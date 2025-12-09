using Beep.Python.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Beep.Python.RuntimeEngine.Infrastructure;

/// <summary>
/// Interface for managing Python execution sessions.
/// Provides session creation, tracking, and lifecycle management.
/// </summary>
public interface ISessionManager : IDisposable
{
    /// <summary>
    /// Collection of all active sessions.
    /// </summary>
    List<PythonSessionInfo> Sessions { get; }

    /// <summary>
    /// Creates a new Python session.
    /// </summary>
    PythonSessionInfo CreateSession(string username, string? environmentId = null);

    /// <summary>
    /// Gets a session by its ID.
    /// </summary>
    PythonSessionInfo? GetSession(string sessionId);

    /// <summary>
    /// Checks if a session exists.
    /// </summary>
    bool HasSession(string sessionId);

    /// <summary>
    /// Registers a session with the manager.
    /// </summary>
    void RegisterSession(PythonSessionInfo session);

    /// <summary>
    /// Unregisters a session from the manager.
    /// </summary>
    void UnregisterSession(string sessionId);

    /// <summary>
    /// Updates the last activity timestamp for a session.
    /// </summary>
    void UpdateSessionActivity(string sessionId);

    /// <summary>
    /// Terminates a session.
    /// </summary>
    PassedParameters TerminateSession(string sessionId);

    /// <summary>
    /// Cleans up resources for a session.
    /// </summary>
    void CleanupSession(PythonSessionInfo session);

    /// <summary>
    /// Performs cleanup of old sessions.
    /// </summary>
    void PerformSessionCleanup(TimeSpan maxAge);

    /// <summary>
    /// Gets session output.
    /// </summary>
    Task<string> GetSessionOutput(string sessionId);

    /// <summary>
    /// Appends output to a session.
    /// </summary>
    void AppendSessionOutput(string sessionId, string output);

    /// <summary>
    /// Clears session output.
    /// </summary>
    void ClearSessionOutput(string sessionId);

    /// <summary>
    /// Associates a session with a virtual environment.
    /// </summary>
    bool AssociateWithEnvironment(string sessionId, string environmentId);

    /// <summary>
    /// Gets current session count.
    /// </summary>
    int SessionCount { get; }

    /// <summary>
    /// Gets active session count.
    /// </summary>
    int ActiveSessionCount { get; }

    /// <summary>
    /// Gets usage metrics.
    /// </summary>
    Dictionary<string, object> GetMetrics();

    /// <summary>
    /// Executes action with concurrency control.
    /// </summary>
    Task<bool> ExecuteWithConcurrencyControlAsync(string sessionId, Func<Task> action);
}
