using Beep.Python.Model;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Beep.Python.RuntimeEngine.Infrastructure;

/// <summary>
/// Infrastructure implementation of session management.
/// Manages Python execution sessions with concurrency control and load balancing.
/// </summary>
public class SessionManager : ISessionManager, IDisposable
{
    private readonly ILogger<SessionManager> _logger;
    private readonly IVenvManager? _venvManager;
    private readonly SemaphoreSlim _resourceSemaphore;
    private readonly ConcurrentDictionary<string, StringBuilder> _sessionOutputs = new();
    private readonly ConcurrentDictionary<string, int> _environmentLoadCounter = new();
    private readonly object _sessionsLock = new();
    private readonly int _maxConcurrentSessions;
    private bool _disposed = false;

    public SessionManager(
        ILogger<SessionManager> logger,
        IVenvManager? venvManager = null,
        int maxConcurrentSessions = 100)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _venvManager = venvManager;
        _maxConcurrentSessions = maxConcurrentSessions;
        _resourceSemaphore = new SemaphoreSlim(maxConcurrentSessions, maxConcurrentSessions);
    }

    public List<PythonSessionInfo> Sessions { get; } = new();

    public int SessionCount => Sessions.Count;

    public int ActiveSessionCount => Sessions.Count(s => s.Status == PythonSessionStatus.Active);

    /// <summary>
    /// Creates a new Python session.
    /// </summary>
    public PythonSessionInfo CreateSession(string username, string? environmentId = null)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username cannot be empty", nameof(username));

        if (!_resourceSemaphore.Wait(TimeSpan.FromSeconds(5)))
        {
            throw new TimeoutException("Failed to acquire session resources - system is at maximum capacity");
        }

        try
        {
            var now = DateTime.UtcNow;
            var session = new PythonSessionInfo
            {
                SessionId = Guid.NewGuid().ToString("N")[..8],
                Username = username,
                VirtualEnvironmentId = environmentId,
                StartedAt = now,
                CreatedAt = now,
                LastActivityAt = now,
                SessionName = $"Session_{username}_{DateTime.Now.Ticks}",
                Status = PythonSessionStatus.Active,
                Metadata = new Dictionary<string, object>
                {
                    ["LastActivity"] = now
                }
            };

            RegisterSession(session);
            _sessionOutputs[session.SessionId] = new StringBuilder();

            if (!string.IsNullOrEmpty(environmentId))
            {
                IncrementEnvironmentLoadCounter(environmentId);
            }

            _logger.LogInformation("Created session {SessionId} for user {Username}", session.SessionId, username);
            return session;
        }
        finally
        {
            Task.Run(() => _resourceSemaphore.Release());
        }
    }

    public PythonSessionInfo? GetSession(string sessionId)
    {
        if (string.IsNullOrEmpty(sessionId))
            return null;

        lock (_sessionsLock)
        {
            return Sessions.FirstOrDefault(s => s.SessionId == sessionId);
        }
    }

    public bool HasSession(string sessionId)
    {
        return Sessions.Any(s => s.SessionId == sessionId);
    }

    public void RegisterSession(PythonSessionInfo session)
    {
        if (session == null)
            return;

        lock (_sessionsLock)
        {
            if (!Sessions.Any(s => s.SessionId == session.SessionId))
            {
                Sessions.Add(session);
            }
        }

        _logger.LogDebug("Registered session {SessionId}", session.SessionId);
    }

    public void UnregisterSession(string sessionId)
    {
        if (string.IsNullOrEmpty(sessionId))
            return;

        lock (_sessionsLock)
        {
            var session = Sessions.FirstOrDefault(s => s.SessionId == sessionId);
            if (session != null)
            {
                Sessions.Remove(session);
                _sessionOutputs.TryRemove(sessionId, out _);
                
                if (!string.IsNullOrEmpty(session.VirtualEnvironmentId))
                {
                    DecrementEnvironmentLoadCounter(session.VirtualEnvironmentId);
                }
            }
        }

        _logger.LogDebug("Unregistered session {SessionId}", sessionId);
    }

    public void UpdateSessionActivity(string sessionId)
    {
        var session = GetSession(sessionId);
        if (session != null)
        {
            session.LastActivityAt = DateTime.UtcNow;
            if (session.Metadata != null)
            {
                session.Metadata["LastActivity"] = session.LastActivityAt;
            }
        }
    }

    public PassedParameters TerminateSession(string sessionId)
    {
        var session = GetSession(sessionId);
        if (session == null)
        {
            return new PassedParameters { Flag = Errors.Failed, Message = $"Session {sessionId} not found" };
        }

        try
        {
            session.Status = PythonSessionStatus.Terminated;
            session.EndedAt = DateTime.UtcNow;
            CleanupSession(session);
            UnregisterSession(sessionId);

            _logger.LogInformation("Terminated session {SessionId}", sessionId);
            return new PassedParameters { Flag = Errors.Ok, Message = "Session terminated successfully" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error terminating session {SessionId}", sessionId);
            return new PassedParameters { Flag = Errors.Failed, Message = ex.Message };
        }
    }

    public void CleanupSession(PythonSessionInfo session)
    {
        if (session == null)
            return;

        _sessionOutputs.TryRemove(session.SessionId, out _);
    }

    public void PerformSessionCleanup(TimeSpan maxAge)
    {
        var cutoffTime = DateTime.UtcNow - maxAge;
        var sessionsToClean = Sessions
            .Where(s => s.Status == PythonSessionStatus.Terminated && 
                       (s.EndedAt ?? s.LastActivityAt) < cutoffTime)
            .ToList();

        foreach (var session in sessionsToClean)
        {
            UnregisterSession(session.SessionId);
            _logger.LogDebug("Cleaned up old session {SessionId}", session.SessionId);
        }
    }

    public Task<string> GetSessionOutput(string sessionId)
    {
        if (_sessionOutputs.TryGetValue(sessionId, out var output))
        {
            return Task.FromResult(output.ToString());
        }
        return Task.FromResult(string.Empty);
    }

    public void AppendSessionOutput(string sessionId, string output)
    {
        if (_sessionOutputs.TryGetValue(sessionId, out var sb))
        {
            sb.AppendLine(output);
        }
    }

    public void ClearSessionOutput(string sessionId)
    {
        if (_sessionOutputs.TryGetValue(sessionId, out var sb))
        {
            sb.Clear();
        }
    }

    public bool AssociateWithEnvironment(string sessionId, string environmentId)
    {
        var session = GetSession(sessionId);
        if (session != null)
        {
            session.VirtualEnvironmentId = environmentId;
            return true;
        }
        return false;
    }

    public Dictionary<string, object> GetMetrics()
    {
        return new Dictionary<string, object>
        {
            ["TotalSessions"] = SessionCount,
            ["ActiveSessions"] = ActiveSessionCount,
            ["MaxConcurrentSessions"] = _maxConcurrentSessions
        };
    }

    public async Task<bool> ExecuteWithConcurrencyControlAsync(string sessionId, Func<Task> action)
    {
        if (!_resourceSemaphore.Wait(TimeSpan.FromSeconds(5)))
            return false;

        try
        {
            await action();
            return true;
        }
        catch
        {
            return false;
        }
        finally
        {
            _resourceSemaphore.Release();
        }
    }

    private void IncrementEnvironmentLoadCounter(string environmentId)
    {
        if (string.IsNullOrEmpty(environmentId))
            return;

        _environmentLoadCounter.AddOrUpdate(
            environmentId,
            1,
            (_, current) => current + 1);
    }

    private void DecrementEnvironmentLoadCounter(string environmentId)
    {
        if (string.IsNullOrEmpty(environmentId))
            return;

        _environmentLoadCounter.AddOrUpdate(
            environmentId,
            0,
            (_, current) => Math.Max(0, current - 1));
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _resourceSemaphore?.Dispose();
            Sessions.Clear();
            _sessionOutputs.Clear();
            _environmentLoadCounter.Clear();
            _disposed = true;
        }
    }
}
