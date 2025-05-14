using Beep.Python.Model;
using Beep.Python.RuntimeEngine.Helpers;
using Newtonsoft.Json;
using Python.Runtime;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Container.Model;
using TheTechIdea.Beep.Container.Services;
using TheTechIdea.Beep.Editor;

namespace Beep.Python.RuntimeEngine
{
    /// <summary>
    /// Manages Python virtual environments, including creation, initialization, and maintenance.
    /// Centralizes all virtual environment functionality to avoid duplication.
    /// </summary>
    public class PythonVirtualEnvManager : IPythonVirtualEnvManager, IDisposable
    {
        private bool disposedValue;
        private readonly IBeepService _beepservice;
        private readonly IPythonRunTimeManager _pythonRuntime;
        private readonly object _environmentsLock = new object();
        private readonly ConcurrentDictionary<string, DateTime> _lastEnvironmentUsage = new ConcurrentDictionary<string, DateTime>();
        private const string ADMIN_SESSION_USERNAME = "admin";
        private const string ADMIN_SESSION_NAME = "AdminManagementSession";
        private PythonSessionInfo _adminSession;

        /// <summary>
        /// Gets or sets whether the manager is currently busy with an operation.
        /// </summary>
        public bool IsBusy { get; private set; }

        /// <summary>
        /// Collection of all managed virtual environments.
        /// </summary>
        public ObservableBindingList<PythonVirtualEnvironment> ManagedVirtualEnvironments { get; set; } = new();

        /// <summary>
        /// Initializes a new instance of PythonVirtualEnvManager.
        /// </summary>
        /// <param name="beepservice">The BEEP service for accessing application services.</param>
        /// <param name="pythonRuntimeManager">The Python runtime manager.</param>
        public PythonVirtualEnvManager(IBeepService beepservice, IPythonRunTimeManager pythonRuntimeManager)
        {
            _beepservice = beepservice ?? throw new ArgumentNullException(nameof(beepservice));
            _pythonRuntime = pythonRuntimeManager ?? throw new ArgumentNullException(nameof(pythonRuntimeManager));
            IsBusy = false;
        }

        #region Environment Creation
        /// <summary>
        /// Gets an active admin session for package management operations.
        /// </summary>
        /// <param name="env">The environment to get an admin session for</param>
        /// <returns>The admin session for package management</returns>
        public PythonSessionInfo GetPackageManagementSession(PythonVirtualEnvironment env)
        {
            return GetAdminSession(env);
        }

        /// <summary>
        /// Gets or creates the admin session used for environment management operations.
        /// </summary>
        /// <returns>The admin session</returns>
        private PythonSessionInfo GetAdminSession(PythonVirtualEnvironment env)
        {
            // If we already have an admin session for this environment, use it
            if (_adminSession != null &&
                _adminSession.VirtualEnvironmentId == env.ID &&
                _adminSession.Status == PythonSessionStatus.Active)
            {
                return _adminSession;
            }

            // Check if the environment already has an admin session
            var existingAdminSession = env.Sessions.FirstOrDefault(s =>
                s.Username == ADMIN_SESSION_USERNAME &&
                s.Status == PythonSessionStatus.Active);

            if (existingAdminSession != null)
            {
                _adminSession = existingAdminSession;
                return _adminSession;
            }

            // Create a new admin session
            _adminSession = new PythonSessionInfo
            {
                Username = ADMIN_SESSION_USERNAME,
                SessionName = ADMIN_SESSION_NAME,
                VirtualEnvironmentId = env.ID,
                StartedAt = DateTime.Now,
                Status = PythonSessionStatus.Active
            };

            // Associate with environment
            env.AddSession(_adminSession);

            // Add to global sessions if not already there
            if (_pythonRuntime.SessionManager != null &&
                !_pythonRuntime.SessionManager.Sessions.Any(s => s.SessionId == _adminSession.SessionId))
            {
                _pythonRuntime.SessionManager.Sessions.Add(_adminSession);
            }

            // Create a scope for this session if needed
            if (!_pythonRuntime.HasScope(_adminSession))
            {
                _pythonRuntime.CreateScope(_adminSession, env);
            }

            LogInfo($"Created admin session {_adminSession.SessionId} for environment {env.Name}");
            return _adminSession;
        }

        /// <summary>
        /// Core method for creating virtual environments that centralizes all creation logic.
        /// All public virtual environment creation methods should use this method.
        /// </summary>
        /// <param name="config">Python runtime configuration to use</param>
        /// <param name="envPath">Path where the virtual environment should be created</param>
        /// <param name="envName">Optional name for the environment (defaults to directory name)</param>
        /// <param name="createdBy">User who created the environment (defaults to "System")</param>
        /// <returns>The created or existing environment, or null if creation failed</returns>
        private PythonVirtualEnvironment CreateVirtualEnvironmentCore(
            PythonRunTime config,
            string envPath,
            string envName = null,
            string createdBy = "System")
        {
            if (config == null)
            {
                LogError("No Python configuration provided.");
                return null;
            }

            if (string.IsNullOrWhiteSpace(envPath))
            {
                LogError("Environment path cannot be empty.");
                return null;
            }

            // Check if environment already exists by path
            var existingEnv = GetEnvironmentByPath(envPath);
            if (existingEnv != null)
            {
                LogInfo($"Virtual environment already exists at {envPath}.");
                return existingEnv;
            }

            // Verify Python installation before creating env
            var report = PythonEnvironmentDiagnostics.RunFullDiagnostics(config.BinPath);
            if (!report.PythonFound || !report.CanExecuteCode)
            {
                LogError("Invalid Python installation. Cannot create virtual environment.");
                return null;
            }

            try
            {
                // Ensure the parent directory exists
                Directory.CreateDirectory(Path.GetDirectoryName(envPath));

                // Create environment definition
                envName = envName ?? Path.GetFileName(envPath);

                var env = new PythonVirtualEnvironment
                {
                    Name = envName,
                    Path = envPath,
                    PythonConfigID = config.ID,
                    BaseInterpreterPath = config.RuntimePath ?? config.BinPath,
                    CreatedOn = DateTime.Now,
                    CreatedBy = createdBy,
                    EnvironmentType = PythonEnvironmentType.VirtualEnv
                };

                // Get or create the admin session
                var session = GetAdminSession(env);
                // Associate session with environment
               

                // Add to global sessions collection
                if (_pythonRuntime.SessionManager != null &&
                    !_pythonRuntime.SessionManager.Sessions.Any(s => s.SessionId == session.SessionId))
                {
                    _pythonRuntime.SessionManager.Sessions.Add(session);
                }

                // Use the Python executable from the configuration
                string pythonExe = PythonRunTimeDiagnostics.GetPythonExe(config.BinPath);
                if (string.IsNullOrEmpty(pythonExe) || !File.Exists(pythonExe))
                {
                    LogError($"Python executable not found at {config.BinPath}");
                    session.EndedAt = DateTime.Now;
                    session.WasSuccessful = false;
                    session.Notes = "Failed: Python executable not found";
                    return null;
                }

                // Skip create if directory already exists
                if (Directory.Exists(envPath))
                {
                    LogInfo($"Directory for environment already exists at {envPath}. Using existing environment.");
                    session.EndedAt = DateTime.Now;
                    session.WasSuccessful = true;
                    session.Notes = $"Using existing directory at {envPath}";

                    // Add environment to managed environments
                    AddToManagedEnvironments(env);
                    return env;
                }

                // Create the virtual environment using Python's venv module
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = pythonExe,
                        Arguments = $"-m venv \"{envPath}\" --copies --clear",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                // Update session info
                session.EndedAt = DateTime.Now;

                if (process.ExitCode == 0)
                {
                    session.WasSuccessful = true;
                    session.Notes = $"Created virtual environment at {envPath}";
                    LogInfo($"Virtual environment created at: {envPath}");

                    // Add environment to managed environments
                    AddToManagedEnvironments(env);

                    // Track usage time
                    UpdateEnvironmentUsage(env.ID);

                    return env;
                }
                else
                {
                    session.WasSuccessful = false;
                    session.Notes = $"Failed: {error}";
                    LogError($"Failed to create virtual environment: {error}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                LogError($"Error creating virtual environment: {ex.Message}");
                return null;
            }
        }
        /// <summary>
        /// Creates a virtual environment with the specified configuration and environment.
        /// </summary>
        public bool CreateVirtualEnvironment(PythonRunTime cfg, PythonVirtualEnvironment env)
        {
            if (env == null)
            {
                LogError("Environment definition cannot be null.");
                return false;
            }

            var createdEnv = CreateVirtualEnvironmentCore(
                cfg,
                env.Path,
                env.Name,
                env.CreatedBy ?? "System");

            return createdEnv != null;
        }

        /// <summary>
        /// Creates a virtual environment at the specified path.
        /// </summary>
        public bool CreateVirtualEnvironment(PythonRunTime config, string envPath)
        {
            var createdEnv = CreateVirtualEnvironmentCore(config, envPath);
            return createdEnv != null;
        }

        /// <summary>
        /// Creates a virtual environment for a specific user with a session.
        /// </summary>
        public bool CreateEnvForUser(PythonRunTime cfg, PythonSessionInfo sessionInfo)
        {
            if (sessionInfo == null)
            {
                LogError("Session information cannot be null.");
                return false;
            }

            // Determine the base path for environments
            string baseEnvPath = Path.Combine(
                _beepservice.DMEEditor.ConfigEditor.ConfigPath,
                "PythonEnvironments");

            // Create an environment based on the username in the session
            var envName = string.IsNullOrEmpty(sessionInfo.Username) ?
                $"User_{DateTime.Now.Ticks}" :
                sessionInfo.Username;

            string envPath = Path.Combine(baseEnvPath, envName);

            // Create the environment
            var env = CreateVirtualEnvironmentCore(cfg, envPath, envName, sessionInfo.Username ?? "System");
            if (env == null)
            {
                return false;
            }

            // Update the session's environment ID
            sessionInfo.VirtualEnvironmentId = env.ID;

            // Associate the session with the environment
            env.AddSession(sessionInfo);

            // Add to global sessions if not already there
            if (_pythonRuntime.SessionManager != null &&
                !_pythonRuntime.SessionManager.Sessions.Any(s => s.SessionId == sessionInfo.SessionId))
            {
                _pythonRuntime.SessionManager.Sessions.Add(sessionInfo);
            }

            return true;
        }

        /// <summary>
        /// Creates a virtual environment for a user.
        /// </summary>
        public bool CreateEnvForUser(PythonRunTime config, string envBasePath, string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                LogError("Username cannot be empty.");
                return false;
            }

            string envPath = Path.Combine(envBasePath, username);
            var env = CreateVirtualEnvironmentCore(config, envPath, username, username);
            return env != null;
        }

        /// <summary>
        /// Creates a virtual environment for a user with a specified name.
        /// </summary>
        public PythonSessionInfo CreateEnvironmentForUser(PythonRunTime config, string envBasePath,
                                                      string username, string envName = null)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                LogError("Username cannot be empty.");
                return null;
            }

            envName = envName ?? username;
            string userEnvPath = Path.Combine(envBasePath, envName);

            // Create or get existing environment
            var env = CreateVirtualEnvironmentCore(config, userEnvPath, envName, username);
            if (env == null)
            {
                LogError($"Failed to create environment for user {username}");
                return null;
            }

            // Create a new session for this user
            var session = new PythonSessionInfo
            {
                Username = username,
                VirtualEnvironmentId = env.ID,
                StartedAt = DateTime.Now,
                SessionName = $"Session_{username}_{DateTime.Now.Ticks}",
                Status = PythonSessionStatus.Active
            };

            // Associate session with environment
            env.AddSession(session);

            // Add to global sessions collection
            if (_pythonRuntime.SessionManager != null)
            {
                _pythonRuntime.SessionManager.Sessions.Add(session);
            }

            // Update environment usage timestamp
            UpdateEnvironmentUsage(env.ID);

            return session;
        }

        #endregion

        #region Environment Initialization


        /// <summary>
        /// Initializes the Python runtime asynchronously for a specific environment.
        /// </summary>
        /// <param name="env">The virtual environment to initialize.</param>
        public void InitializePythonEnvironment(PythonVirtualEnvironment env)
        {
            if (env == null)
            {
                LogError("Cannot initialize Python environment: environment is null.");
                return;
            }

            Task.Run(() =>
            {
                try
                {
                    // Find the configuration for this environment
                    var config = _pythonRuntime.PythonInstallations.FirstOrDefault(c => c.ID == env.PythonConfigID);
                    if (config == null)
                    {
                        LogError($"Configuration not found for environment {env.Name}");
                        return;
                    }

                    // Determine engine mode using the centralized helper method
                    PythonEngineMode mode = DetermineEngineMode(env);

                    // Initialize with the appropriate engine mode
                    _pythonRuntime.Initialize(config, env.Path, mode);

                    // Create a session for this initialization if needed
                    if (env.Sessions.Count == 0)
                    {
                        // Get or create the admin session
                        var session = GetAdminSession(env);
                      

                        if (_pythonRuntime.SessionManager != null &&
                            !_pythonRuntime.SessionManager.Sessions.Any(s => s.SessionId == session.SessionId))
                        {
                            _pythonRuntime.SessionManager.Sessions.Add(session);
                        }

                        if (!_pythonRuntime.HasScope(session))
                        {
                            _pythonRuntime.CreateScope(session, env);
                        }
                    }

                    // Track the last time this environment was used
                    UpdateEnvironmentUsage(env.ID);
                }
                catch (Exception ex)
                {
                    LogError($"Error initializing Python environment: {ex.Message}");
                }
            });
        }

        #endregion


        #region Environment Management

        /// <summary>
        /// Gets a virtual environment by its path.
        /// </summary>
        /// <param name="path">The path of the environment to find.</param>
        /// <returns>The environment if found; otherwise null.</returns>
        public PythonVirtualEnvironment GetEnvironmentByPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;

            return ManagedVirtualEnvironments.FirstOrDefault(e =>
                e.Path.Equals(path, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets a virtual environment by its ID.
        /// </summary>
        /// <param name="id">The ID of the environment to find.</param>
        /// <returns>The environment if found; otherwise null.</returns>
        public PythonVirtualEnvironment GetEnvironmentById(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;

            return ManagedVirtualEnvironments.FirstOrDefault(e => e.ID == id);
        }

        /// <summary>
        /// Adds an environment to the managed environments collection if not already present.
        /// </summary>
        /// <param name="env">The environment to add.</param>
        /// <returns>True if the environment was added; false if it already existed.</returns>
        public bool AddToManagedEnvironments(PythonVirtualEnvironment env)
        {
            if (env == null)
                throw new ArgumentNullException(nameof(env));

            lock (_environmentsLock)
            {
                // Check if environment already exists by path
                var existingByPath = GetEnvironmentByPath(env.Path);
                if (existingByPath != null)
                    return false;

                // Check if environment already exists by ID
                var existingById = GetEnvironmentById(env.ID);
                if (existingById != null)
                    return false;

                // Add the environment
                ManagedVirtualEnvironments.Add(env);
                return true;
            }
        }

        /// <summary>
        /// Removes an environment from the managed environments collection.
        /// </summary>
        /// <param name="environmentId">The ID of the environment to remove.</param>
        /// <returns>True if the environment was removed; otherwise false.</returns>
        public bool RemoveEnvironment(string environmentId)
        {
            if (string.IsNullOrWhiteSpace(environmentId))
                return false;

            lock (_environmentsLock)
            {
                var env = GetEnvironmentById(environmentId);
                if (env == null)
                    return false;

                // Remove all sessions for this environment
                ShutDown(env);

                return ManagedVirtualEnvironments.Remove(env);
            }
        }

        /// <summary>
        /// Updates the last usage time for an environment.
        /// </summary>
        /// <param name="environmentId">The ID of the environment to update.</param>
        public void UpdateEnvironmentUsage(string environmentId)
        {
            if (!string.IsNullOrWhiteSpace(environmentId))
            {
                _lastEnvironmentUsage[environmentId] = DateTime.Now;
            }
        }

        /// <summary>
        /// Gets the least recently used environment.
        /// </summary>
        /// <returns>The least recently used environment, or null if no environments exist.</returns>
        public PythonVirtualEnvironment GetLeastRecentlyUsedEnvironment()
        {
            lock (_environmentsLock)
            {
                if (ManagedVirtualEnvironments.Count == 0)
                    return null;

                // Find environment with oldest last usage time or no usage time
                var leastRecentlyUsedId = _lastEnvironmentUsage
                    .OrderBy(x => x.Value)
                    .Select(x => x.Key)
                    .FirstOrDefault();

                if (leastRecentlyUsedId != null)
                {
                    return GetEnvironmentById(leastRecentlyUsedId);
                }

                // If no usage data, return first environment
                return ManagedVirtualEnvironments.FirstOrDefault();
            }
        }

        /// <summary>
        /// Performs cleanup of environments that haven't been used for a specified time.
        /// </summary>
        /// <param name="maxIdleTime">Maximum time an environment can be idle before cleanup.</param>
        public void PerformEnvironmentCleanup(TimeSpan maxIdleTime)
        {
            var now = DateTime.Now;
            var envsToCleanup = new List<PythonVirtualEnvironment>();

            lock (_environmentsLock)
            {
                foreach (var env in ManagedVirtualEnvironments)
                {
                    // Check if this environment has any active sessions
                    bool hasActiveSessions = env.Sessions.Any(s => s.Status == PythonSessionStatus.Active);

                    // Check if environment has been idle for too long
                    if (_lastEnvironmentUsage.TryGetValue(env.ID, out var lastUsage))
                    {
                        if (now - lastUsage > maxIdleTime && !hasActiveSessions)
                        {
                            envsToCleanup.Add(env);
                        }
                    }
                    else if (!hasActiveSessions)
                    {
                        // If no usage data and no active sessions, consider for cleanup
                        envsToCleanup.Add(env);
                    }
                }
            }

            // Clean up each identified environment
            foreach (var env in envsToCleanup)
            {
                LogInfo($"Cleaning up idle environment: {env.Name}");
                ShutDown(env);
            }
        }
        /// <summary>
        /// Determines the appropriate Python engine mode for an environment based on its name and creator.
        /// Ensures consistent mode determination across all environment operations.
        /// </summary>
        /// <param name="env">The virtual environment to determine the mode for</param>
        /// <returns>The appropriate engine mode</returns>
        private PythonEngineMode DetermineEngineMode(PythonVirtualEnvironment env)
        {
            if (env == null)
                return PythonEngineMode.SingleUser; // Default to single user mode

            // Single user mode conditions
            if (env.Name.Equals("SingleUser", StringComparison.OrdinalIgnoreCase) ||
                env.CreatedBy == Environment.UserName)
            {
                return PythonEngineMode.SingleUser;
            }
            // Multi-user mode conditions
            else if (env.Name.Equals("MultiUser", StringComparison.OrdinalIgnoreCase) ||
                     env.CreatedBy == "system")
            {
                return PythonEngineMode.MultiUserWithEnvAndScopeAndSession;
            }
            // Determine based on session count
            else
            {
                return env.Sessions.Count > 1
                    ? PythonEngineMode.MultiUserWithEnvAndScopeAndSession
                    : PythonEngineMode.SingleUser;
            }
        }

        #endregion

        #region Shutdown and Cleanup

        /// <summary>
        /// Shuts down the Python runtime for a specific environment.
        /// </summary>
        /// <param name="env">The environment to shut down.</param>
        /// <returns>Error information, if any.</returns>
        public IErrorsInfo ShutDown(PythonVirtualEnvironment env)
        {
            ErrorsInfo er = new ErrorsInfo { Flag = Errors.Ok };
            if (IsBusy) return er;

            try
            {
                // Create a list of sessions to terminate, excluding admin
                var sessionsToTerminate = env.Sessions
                    .Where(s => s.Username != ADMIN_SESSION_USERNAME)
                    .ToList();

                // Close all regular sessions for this environment
                foreach (var session in sessionsToTerminate)
                {
                    if (_pythonRuntime.HasScope(session))
                    {
                        // Clean up the session's scope
                        _pythonRuntime.CleanupSession(session);
                    }

                    session.EndedAt = DateTime.Now;
                    session.Status = PythonSessionStatus.Terminated;
                }

                // Handle admin session last, but don't terminate it unless we're closing everything
                if (_adminSession != null && _adminSession.VirtualEnvironmentId == env.ID)
                {
                    // Mark admin session as requiring refresh rather than terminating
                    _adminSession.Notes = "Environment shutdown - refresh needed";
                }

                // If there are no active sessions, we can shut down the runtime
                if (_pythonRuntime.SessionManager != null &&
                    !_pythonRuntime.SessionManager.Sessions.Any(s =>
                        s.Status == PythonSessionStatus.Active && s.VirtualEnvironmentId != env.ID))
                {
                    _pythonRuntime.ShutDown();

                    // Now terminate admin session if it exists
                    if (_adminSession != null)
                    {
                        _adminSession.EndedAt = DateTime.Now;
                        _adminSession.Status = PythonSessionStatus.Terminated;
                        _adminSession = null;
                    }
                }
            }
            catch (Exception ex)
            {
                er.Flag = Errors.Failed;
                er.Message = ex.Message;
                er.Ex = ex;
                LogError($"Error shutting down environment {env.Name}: {ex.Message}");
            }

            return er;
        }


        #endregion

        #region Persistence

        /// <summary>
        /// Saves managed environments to a file.
        /// </summary>
        /// <param name="filePath">The file path to save to.</param>
        public void SaveEnvironments(string filePath)
        {
            try
            {
                var json = JsonConvert.SerializeObject(ManagedVirtualEnvironments, Formatting.Indented);
                File.WriteAllText(filePath, json);
                LogInfo($"Environments saved to {filePath}");
            }
            catch (Exception ex)
            {
                LogError($"Error saving environments: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads managed environments from a file.
        /// </summary>
        /// <param name="filePath">The file path to load from.</param>
        public void LoadEnvironments(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    var json = File.ReadAllText(filePath);
                    var loadedEnvironments = JsonConvert.DeserializeObject<ObservableBindingList<PythonVirtualEnvironment>>(json);

                    if (loadedEnvironments != null)
                    {
                        lock (_environmentsLock)
                        {
                            ManagedVirtualEnvironments = loadedEnvironments;
                        }
                        LogInfo($"Loaded {loadedEnvironments.Count} environments from {filePath}");
                    }
                    else
                    {
                        LogWarning($"Failed to deserialize environments from {filePath}");
                    }
                }
                else
                {
                    LogWarning($"Environment file not found: {filePath}");
                }
            }
            catch (Exception ex)
            {
                LogError($"Error loading environments: {ex.Message}");
            }
        }


        #endregion

        #region Logging

        private void LogInfo(string message)
        {
            _beepservice.DMEEditor?.AddLogMessage("PythonVirtualEnvManager", message, DateTime.Now, -1, null, Errors.Ok);
        }

        private void LogWarning(string message)
        {
            _beepservice.DMEEditor?.AddLogMessage("PythonVirtualEnvManager", message, DateTime.Now, -1, null, Errors.Warning);
        }

        private void LogError(string message)
        {
            _beepservice.DMEEditor?.AddLogMessage("PythonVirtualEnvManager", message, DateTime.Now, -1, null, Errors.Failed);
        }

        #endregion

        #region IDisposable

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // Clean up managed resources
                    foreach (var env in ManagedVirtualEnvironments.ToList())
                    {
                        try
                        {
                            ShutDown(env);
                        }
                        catch
                        {
                            // Ignore errors during disposal
                        }
                    }
                    // Ensure admin session is terminated
                    if (_adminSession != null)
                    {
                        _adminSession.EndedAt = DateTime.Now;
                        _adminSession.Status = PythonSessionStatus.Terminated;
                        _adminSession = null;
                    }
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
}
