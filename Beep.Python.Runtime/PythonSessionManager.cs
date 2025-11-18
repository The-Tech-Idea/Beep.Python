using Beep.Python.Model;
using Python.Runtime;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
//using TheTechIdea.Beep.ConfigUtil;
 
//using TheTechIdea.Beep.Editor;

namespace Beep.Python.RuntimeEngine
{
    /// <summary>
    /// Manages Python sessions for multiple users, providing isolation, concurrency support,
    /// and load balancing across Python environments.
    /// </summary>
    public class PythonSessionManager : IPythonSessionManager, IDisposable
    {
        // Thread-safe collections for concurrent access
        private readonly ConcurrentDictionary<string, StringBuilder> _sessionOutputs = new();
        private readonly SemaphoreSlim _resourceSemaphore;
        private readonly object _sessionsLock = new object();

        // Tracks runtime load to enable balanced assignment
        private readonly ConcurrentDictionary<string, int> _environmentLoadCounter = new();

        // Dependencies
       
        private readonly IPythonRunTimeManager _pythonRunTimeManager;

        // Session cleanup timer
        private Timer _sessionCleanupTimer;

        // Configuration settings
        private readonly SessionManagerConfiguration _configuration;

        /// <summary>
        /// All active and recently terminated sessions.
        /// </summary>
        public List<PythonSessionInfo> Sessions { get; set; } = new();

        /// <summary>
        /// Initializes a new instance of the PythonSessionManager.
        /// </summary>
        /// <param name="beepService">Beep service dependency</param>
        /// <param name="pythonRunTimeManager">Python runtime manager dependency</param>
        /// <param name="configuration">Optional configuration settings</param>
        public PythonSessionManager(
             
            IPythonRunTimeManager pythonRunTimeManager,
            SessionManagerConfiguration configuration = null)
        {
             
            _pythonRunTimeManager = pythonRunTimeManager ?? throw new ArgumentNullException(nameof(pythonRunTimeManager));
            _configuration = configuration ?? new SessionManagerConfiguration();

            // Initialize concurrency control
            _resourceSemaphore = new SemaphoreSlim(_configuration.MaxConcurrentSessions, _configuration.MaxConcurrentSessions);

            // Set up the automatic session cleanup timer
            _sessionCleanupTimer = new Timer(
                CleanupTimerCallback,
                null,
                _configuration.CleanupInterval,
                _configuration.CleanupInterval);
        }

        #region Session Creation and Management

        /// <summary>
        /// Creates a new Python session associated with a specific user and environment.
        /// Uses load balancing to determine the best environment when multiple are available.
        /// </summary>
        /// <param name="username">The username for the session.</param>
        /// <param name="environmentId">The ID of the virtual environment to associate with, or null for auto-selection.</param>
        /// <returns>The newly created session.</returns>
        public PythonSessionInfo CreateSession(string username, string environmentId)
        {
            // Ensure we're not exceeding our concurrency limits
            bool resourceAcquired = _resourceSemaphore.Wait(_configuration.ResourceAcquisitionTimeout);

            if (!resourceAcquired)
            {
                throw new TimeoutException("Failed to acquire session resources - system is at maximum capacity.");
            }

            try
            {
                string actualEnvironmentId = environmentId;

                // If no environment specified, use load balancing to select one
                if (string.IsNullOrEmpty(environmentId) && _configuration.EnableLoadBalancing)
                {
                    actualEnvironmentId = SelectEnvironmentWithLoadBalancing();
                }

                var now = DateTime.UtcNow;
                var session = new PythonSessionInfo
                {
                    Username = username,
                    VirtualEnvironmentId = actualEnvironmentId,
                    StartedAt = now,
                    CreatedAt = now,
                    LastActivityAt = now,
                    SessionName = $"Session_{username}_{DateTime.Now.Ticks}",
                    Status = PythonSessionStatus.Active,

                    // Track containerization status - relevant for Docker environments
                    Metadata = new Dictionary<string, object>
                    {
                        ["IsContainerized"] = IsContainerizedEnvironment(actualEnvironmentId),
                        ["LastActivity"] = now
                    }
                };

                // Register session in our tracking systems
                RegisterSession(session);

                // Initialize output capture
                _sessionOutputs[session.SessionId] = new StringBuilder();

                // Increment the environment load counter for load balancing
                IncrementEnvironmentLoadCounter(actualEnvironmentId);

                return session;
            }
            finally
            {
                // Always release the semaphore if we've acquired it
                if (resourceAcquired)
                {
                    // We'll release in the background to avoid blocking
                    Task.Run(() => _resourceSemaphore.Release());
                }
            }
        }

        /// <summary>
        /// Selects the most appropriate environment based on current load.
        /// </summary>
        private string SelectEnvironmentWithLoadBalancing()
        {
            var environments = _pythonRunTimeManager.VirtualEnvmanager?.ManagedVirtualEnvironments;

            if (environments == null || environments.Count == 0)
            {
                throw new InvalidOperationException("No Python environments available.");
            }

            // Find the environment with the lowest load
            var leastLoadedEnv = environments
                .OrderBy(e => _environmentLoadCounter.GetValueOrDefault(e.ID, 0))
                .First();

            return leastLoadedEnv.ID;
        }

        /// <summary>
        /// Increments the load counter for an environment.
        /// </summary>
        private void IncrementEnvironmentLoadCounter(string environmentId)
        {
            if (string.IsNullOrEmpty(environmentId))
                return;

            _environmentLoadCounter.AddOrUpdate(
                environmentId,
                1,
                (_, currentCount) => currentCount + 1);
        }

        /// <summary>
        /// Decrements the load counter for an environment.
        /// </summary>
        private void DecrementEnvironmentLoadCounter(string environmentId)
        {
            if (string.IsNullOrEmpty(environmentId))
                return;

            _environmentLoadCounter.AddOrUpdate(
                environmentId,
                0,
                (_, currentCount) => Math.Max(0, currentCount - 1));
        }

        /// <summary>
        /// Determines if an environment is running in a container.
        /// </summary>
        private bool IsContainerizedEnvironment(string environmentId)
        {
            if (string.IsNullOrEmpty(environmentId))
                return false;

            var env = _pythonRunTimeManager.VirtualEnvmanager?.ManagedVirtualEnvironments
                .FirstOrDefault(e => e.ID == environmentId);

            // Check for container markers in path or metadata
            return env?.Path?.Contains("container", StringComparison.OrdinalIgnoreCase) == true ||
                   env?.Path?.Contains("docker", StringComparison.OrdinalIgnoreCase) == true;
        }

        /// <summary>
        /// Checks if a session with the specified ID exists.
        /// </summary>
        /// <param name="sessionId">The session ID to check.</param>
        /// <returns>True if the session exists; otherwise false.</returns>
        public bool HasSession(string sessionId)
        {
            return Sessions.Any(s => s.SessionId == sessionId);
        }

        /// <summary>
        /// Gets a session by its ID.
        /// </summary>
        /// <param name="sessionId">The session ID to retrieve.</param>
        /// <returns>The session if found; otherwise null.</returns>
        public PythonSessionInfo GetSession(string sessionId)
        {
            return Sessions.FirstOrDefault(s => s.SessionId == sessionId);
        }

        /// <summary>
        /// Updates the last activity timestamp for a session.
        /// </summary>
        /// <param name="sessionId">The session ID to update.</param>
        public void UpdateSessionActivity(string sessionId)
        {
            var session = GetSession(sessionId);
            if (session == null)
            {
                return;
            }

            var lastActivity = DateTime.UtcNow;
            session.LastActivityAt = lastActivity;

            if (session.Metadata != null)
            {
                session.Metadata["LastActivity"] = lastActivity;
            }
        }

        /// <summary>
        /// Registers a session with the session manager and the associated environment.
        /// </summary>
        /// <param name="session">The session to register.</param>
        public void RegisterSession(PythonSessionInfo session)
        {
            if (session == null)
                return;

            lock (_sessionsLock)
            {
                // Add to session collection if not already present
                if (!Sessions.Any(s => s.SessionId == session.SessionId))
                {
                    Sessions.Add(session);
                }
            }

            // If the session has a virtual environment association, add it to that environment
            if (!string.IsNullOrEmpty(session.VirtualEnvironmentId))
            {
                var environment = _pythonRunTimeManager.VirtualEnvmanager?.ManagedVirtualEnvironments
                    .FirstOrDefault(e => e.ID == session.VirtualEnvironmentId);

                if (environment != null && !environment.Sessions.Any(s => s.SessionId == session.SessionId))
                {
                    environment.AddSession(session);
                }
            }

            // Initialize session output tracking if not already present
            if (!_sessionOutputs.ContainsKey(session.SessionId))
            {
                _sessionOutputs[session.SessionId] = new StringBuilder();
            }
        }

        /// <summary>
        /// Unregisters a session from the session manager and associated environment.
        /// </summary>
        /// <param name="sessionId">The ID of the session to unregister.</param>
        public void UnregisterSession(string sessionId)
        {
            var session = GetSession(sessionId);
            if (session == null)
                return;

            // Remove from environment if associated
            if (!string.IsNullOrEmpty(session.VirtualEnvironmentId))
            {
                var environment = _pythonRunTimeManager.VirtualEnvmanager?.ManagedVirtualEnvironments
                    .FirstOrDefault(e => e.ID == session.VirtualEnvironmentId);

                if (environment != null)
                {
                    var sessionInEnv = environment.Sessions.FirstOrDefault(s => s.SessionId == sessionId);
                    if (sessionInEnv != null)
                    {
                        environment.Sessions.Remove(sessionInEnv);
                    }
                }

                // Update load counter for the environment
                DecrementEnvironmentLoadCounter(session.VirtualEnvironmentId);
            }

            lock (_sessionsLock)
            {
                // Remove from session collection
                var sessionToRemove = Sessions.FirstOrDefault(s => s.SessionId == sessionId);
                if (sessionToRemove != null)
                {
                    Sessions.Remove(sessionToRemove);
                }
            }

            // Clean up output buffer
            _sessionOutputs.TryRemove(sessionId, out _);

            // Release the semaphore to allow another session to be created
            try
            {
                _resourceSemaphore.Release();
            }
            catch (SemaphoreFullException)
            {
                // Ignore if we somehow have more releases than acquisitions
            }
        }

        /// <summary>
        /// Associates a session with a virtual environment.
        /// </summary>
        /// <param name="sessionId">The ID of the session to associate.</param>
        /// <param name="environmentId">The ID of the virtual environment to associate with.</param>
        /// <returns>True if the association was successful; otherwise false.</returns>
        public bool AssociateWithEnvironment(string sessionId, string environmentId)
        {
            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(environmentId))
                return false;

            var session = GetSession(sessionId);
            if (session == null)
                return false;

            var environment = _pythonRunTimeManager.VirtualEnvmanager?.ManagedVirtualEnvironments
                .FirstOrDefault(e => e.ID == environmentId);

            if (environment == null)
                return false;

            // If already associated with an environment, decrement its load counter
            if (!string.IsNullOrEmpty(session.VirtualEnvironmentId))
            {
                DecrementEnvironmentLoadCounter(session.VirtualEnvironmentId);
            }

            // Update the session's environment ID
            session.VirtualEnvironmentId = environmentId;

            // Add session to the environment if not already present
            if (!environment.Sessions.Any(s => s.SessionId == sessionId))
            {
                environment.AddSession(session);
            }

            // Increment the load counter for the new environment
            IncrementEnvironmentLoadCounter(environmentId);

            return true;
        }

        #endregion

        #region Session Output Management

        /// <summary>
        /// Gets the captured output from a session.
        /// </summary>
        /// <param name="sessionId">The session ID to get output for.</param>
        /// <returns>The captured output as a string.</returns>
        public Task<string> GetSessionOutput(string sessionId)
        {
            if (_sessionOutputs.TryGetValue(sessionId, out var output))
            {
                return Task.FromResult(output.ToString());
            }

            return Task.FromResult(string.Empty);
        }

        /// <summary>
        /// Appends output to a session's captured output buffer.
        /// </summary>
        /// <param name="sessionId">The session ID to append output for.</param>
        /// <param name="output">The output to append.</param>
        public void AppendSessionOutput(string sessionId, string output)
        {
            if (_sessionOutputs.TryGetValue(sessionId, out var buffer))
            {
                buffer.AppendLine(output);

                // Update last activity timestamp
                UpdateSessionActivity(sessionId);
            }
        }

        /// <summary>
        /// Clears a session's captured output buffer.
        /// </summary>
        /// <param name="sessionId">The session ID to clear output for.</param>
        public void ClearSessionOutput(string sessionId)
        {
            if (_sessionOutputs.TryGetValue(sessionId, out var buffer))
            {
                buffer.Clear();
            }
        }

        #endregion

        #region Session Cleanup and Maintenance

        /// <summary>
        /// Cleans up resources associated with a session without terminating it.
        /// </summary>
        /// <param name="session">The session to clean up.</param>
        public void CleanupSession(PythonSessionInfo session)
        {
            if (session == null)
                return;

            // Clean up the session's PyScope if it exists
            if (_pythonRunTimeManager.HasScope(session))
            {
                // We don't want to directly remove the scope here, just clean it up
                _pythonRunTimeManager.CleanupSession(session);
            }
        }

        /// <summary>
        /// Terminates a session, cleaning up all resources and marking it as terminated.
        /// </summary>
        /// <param name="sessionId">The ID of the session to terminate.</param>
        /// <returns>Error information, if any.</returns>
        public PassedParameters TerminateSession(string sessionId)
        {
            var er = new PassedParameters { Flag = Errors.Ok };
            var session = GetSession(sessionId);

            if (session == null)
            {
                er.Flag = Errors.Failed;
                er.Message = $"Session with ID {sessionId} not found.";
                return er;
            }

            try
            {
                // Mark session as terminated
                session.Status = PythonSessionStatus.Terminated;
                session.EndedAt = DateTime.Now;

                // Clean up the session's resources
                _pythonRunTimeManager.ShutDownSession(session);

                // If configured to unregister immediately, do so
                if (_configuration.UnregisterTerminatedSessionsImmediately)
                {
                    UnregisterSession(sessionId);
                }
            }
            catch (Exception ex)
            {
                er.Flag = Errors.Failed;
                er.Message = $"Error terminating session: {ex.Message}";
                er.Ex = ex;
            }

            return er;
        }

        /// <summary>
        /// Timer callback method to check for and clean up stale sessions.
        /// </summary>
        private void CleanupTimerCallback(object state)
        {
            try
            {
                PerformSessionCleanup(_configuration.SessionMaxAge);
            }
            catch (Exception ex)
            {
                // Log the error but don't let exceptions escape the timer callback
                Console.WriteLine($"Session cleanup error: {ex.Message}");
            }
        }

        /// <summary>
        /// Performs cleanup of sessions older than the specified maximum age.
        /// </summary>
        /// <param name="maxAge">Maximum age of sessions to keep.</param>
        public void PerformSessionCleanup(TimeSpan maxAge)
        {
            if (_pythonRunTimeManager == null)
                return;

            var now = DateTime.Now;
            List<PythonSessionInfo> sessionsToCleanup;

            // Safely get a snapshot of sessions to avoid collection modification issues
            lock (_sessionsLock)
            {
                sessionsToCleanup = Sessions.ToList();
            }

            var sessionsToRemove = new List<PythonSessionInfo>();

            foreach (var session in sessionsToCleanup)
            {
                bool shouldCleanup = false;
                bool shouldTerminate = false;

                // For terminated sessions, check against end time
                if (session.Status == PythonSessionStatus.Terminated && session.EndedAt.HasValue)
                {
                    shouldCleanup = (now - session.EndedAt.Value) > maxAge;
                }
                // For active sessions, check against last activity time
                else if (session.Status == PythonSessionStatus.Active)
                {
                    // Get last activity time from metadata or fall back to start time
                    DateTime lastActivity = session.StartedAt;
                    if (session.Metadata != null && session.Metadata.ContainsKey("LastActivity") &&
                        session.Metadata["LastActivity"] is DateTime lastActivityTime)
                    {
                        lastActivity = lastActivityTime;
                    }

                    var inactivityTime = now - lastActivity;
                    shouldTerminate = inactivityTime > _configuration.SessionInactivityTimeout;

                    // Mark active but stale sessions as terminated
                    if (shouldTerminate)
                    {
                        session.Status = PythonSessionStatus.Terminated;
                        session.EndedAt = now;
                        session.Notes = "Session terminated due to inactivity.";

                        // Clean up PyScope and other resources
                        _pythonRunTimeManager.CleanupSession(session);

                        // After termination, check if it should be cleaned up immediately
                        shouldCleanup = _configuration.UnregisterTerminatedSessionsImmediately;
                    }
                }

                if (shouldCleanup)
                {
                    sessionsToRemove.Add(session);
                }
            }

            // Process removals outside the loop to avoid concurrent modification issues
            foreach (var session in sessionsToRemove)
            {
                UnregisterSession(session.SessionId);
            }
        }

        /// <summary>
        /// Performs maintenance of sessions based on specified criteria.
        /// </summary>
        /// <param name="maxInactiveTime">Maximum time a session can be inactive before cleanup.</param>
        /// <param name="terminatedSessionRetention">How long to keep terminated sessions before removal.</param>
        public void PerformSessionMaintenance(TimeSpan maxInactiveTime, TimeSpan terminatedSessionRetention)
        {
            var now = DateTime.Now;
            List<PythonSessionInfo> sessionsToCheck;

            // Safely get a snapshot of sessions
            lock (_sessionsLock)
            {
                sessionsToCheck = Sessions.ToList();
            }

            var sessionsToCleanup = new List<PythonSessionInfo>();

            foreach (var session in sessionsToCheck)
            {
                // Clean up terminated sessions that have expired their retention period
                if (session.Status == PythonSessionStatus.Terminated &&
                    session.EndedAt.HasValue &&
                    (now - session.EndedAt.Value) > terminatedSessionRetention)
                {
                    sessionsToCleanup.Add(session);
                }
                // Clean up stale active sessions
                else if (session.Status == PythonSessionStatus.Active)
                {
                    // Get last activity time from metadata or fall back to start time
                    DateTime lastActivity = session.StartedAt;
                    if (session.Metadata != null && session.Metadata.ContainsKey("LastActivity") &&
                        session.Metadata["LastActivity"] is DateTime lastActivityTime)
                    {
                        lastActivity = lastActivityTime;
                    }

                    if ((now - lastActivity) > maxInactiveTime)
                    {
                        // Mark as terminated
                        session.Status = PythonSessionStatus.Terminated;
                        session.EndedAt = now;
                        session.Notes = "Session terminated due to inactivity.";

                        // Clean up resources
                        _pythonRunTimeManager.CleanupSession(session);
                    }
                }
            }

            // Remove sessions marked for cleanup
            foreach (var session in sessionsToCleanup)
            {
                UnregisterSession(session.SessionId);
            }
        }

        #endregion

        #region Concurrency and Scaling Support

        /// <summary>
        /// Gets the current session count.
        /// </summary>
        public int SessionCount => Sessions.Count;

        /// <summary>
        /// Gets the number of active sessions.
        /// </summary>
        public int ActiveSessionCount => Sessions.Count(s => s.Status == PythonSessionStatus.Active);

        /// <summary>
        /// Gets usage metrics for session management.
        /// </summary>
        /// <returns>A dictionary of metrics</returns>
        public Dictionary<string, object> GetMetrics()
        {
            return new Dictionary<string, object>
            {
                ["TotalSessionCount"] = SessionCount,
                ["ActiveSessionCount"] = ActiveSessionCount,
                ["AvailableConcurrencySlotsCount"] = _resourceSemaphore.CurrentCount,
                ["EnvironmentLoadCounters"] = new Dictionary<string, int>(_environmentLoadCounter),
                ["SessionsPerUser"] = Sessions
                    .GroupBy(s => s.Username)
                    .ToDictionary(g => g.Key, g => g.Count())
            };
        }

        /// <summary>
        /// Executes the given action with proper concurrency control.
        /// </summary>
        /// <param name="sessionId">The session ID to execute for.</param>
        /// <param name="action">The action to execute.</param>
        /// <returns>True if the action was executed; otherwise false.</returns>
        public async Task<bool> ExecuteWithConcurrencyControlAsync(string sessionId, Func<Task> action)
        {
            var session = GetSession(sessionId);
            if (session == null)
                return false;

            try
            {
                // No need to wait for semaphore since we already have a session
                await action();

                // Update activity timestamp
                UpdateSessionActivity(sessionId);
                return true;
            }
            catch (Exception ex)
            {
                // Log errors
                Console.WriteLine($"Error executing action for session {sessionId}: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region IDisposable Implementation

        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // Stop the cleanup timer
                    _sessionCleanupTimer?.Change(Timeout.Infinite, Timeout.Infinite);
                    _sessionCleanupTimer?.Dispose();
                    _sessionCleanupTimer = null;

                    // Release the semaphore
                    _resourceSemaphore?.Dispose();

                    // Clean up managed resources
                    try
                    {
                        // Make a copy of sessions to avoid modification during enumeration
                        List<PythonSessionInfo> sessionsToTerminate;
                        lock (_sessionsLock)
                        {
                            sessionsToTerminate = Sessions.ToList();
                        }

                        foreach (var session in sessionsToTerminate)
                        {
                            try
                            {
                                TerminateSession(session.SessionId);
                            }
                            catch
                            {
                                // Ignore errors during disposal
                            }
                        }
                    }
                    catch
                    {
                        // Ignore errors during disposal
                    }

                    _sessionOutputs.Clear();
                    _environmentLoadCounter.Clear();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }

    /// <summary>
    /// Configuration options for the Python session manager.
    /// </summary>
    public class SessionManagerConfiguration
    {
        /// <summary>
        /// Maximum number of concurrent sessions allowed.
        /// </summary>
        public int MaxConcurrentSessions { get; set; } = System.Environment.ProcessorCount * 2;

        /// <summary>
        /// How often to run the session cleanup process.
        /// </summary>
        public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Maximum age of a session before it's eligible for cleanup.
        /// </summary>
        public TimeSpan SessionMaxAge { get; set; } = TimeSpan.FromHours(12);

        /// <summary>
        /// Timeout for session resource acquisition.
        /// </summary>
        public TimeSpan ResourceAcquisitionTimeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Time after which an inactive session will be terminated.
        /// </summary>
        public TimeSpan SessionInactivityTimeout { get; set; } = TimeSpan.FromMinutes(30);

        /// <summary>
        /// Whether to immediately unregister sessions after termination.
        /// </summary>
        public bool UnregisterTerminatedSessionsImmediately { get; set; } = false;

        /// <summary>
        /// Whether to use load balancing when selecting environments.
        /// </summary>
        public bool EnableLoadBalancing { get; set; } = true;
    }
}
