using System;
using System.Collections.Generic;
using System.Threading.Tasks;
 
 

namespace Beep.Python.Model
{
    /// <summary>
    /// Manages Python sessions for multiple users, providing isolation, concurrency support,
    /// and load balancing across Python environments.
    /// </summary>
    public interface IPythonSessionManager : IDisposable
    {
        /// <summary>
        /// Collection of all sessions managed by this session manager.
        /// </summary>
        List<PythonSessionInfo> Sessions { get; }

        /// <summary>
        /// Creates a new Python session associated with a specific user and environment.
        /// </summary>
        /// <param name="username">The username for the session.</param>
        /// <param name="environmentId">The ID of the virtual environment to associate with, or null for auto-selection.</param>
        /// <returns>The newly created session.</returns>
        PythonSessionInfo CreateSession(string username, string environmentId);

        /// <summary>
        /// Checks if a session with the specified ID exists.
        /// </summary>
        /// <param name="sessionId">The session ID to check.</param>
        /// <returns>True if the session exists; otherwise false.</returns>
        bool HasSession(string sessionId);

        /// <summary>
        /// Gets a session by its ID.
        /// </summary>
        /// <param name="sessionId">The session ID to retrieve.</param>
        /// <returns>The session if found; otherwise null.</returns>
        PythonSessionInfo GetSession(string sessionId);

        /// <summary>
        /// Registers a session with the session manager and the associated environment.
        /// </summary>
        /// <param name="session">The session to register.</param>
        void RegisterSession(PythonSessionInfo session);

        /// <summary>
        /// Unregisters a session from the session manager and associated environment.
        /// </summary>
        /// <param name="sessionId">The ID of the session to unregister.</param>
        void UnregisterSession(string sessionId);

        /// <summary>
        /// Associates a session with a virtual environment.
        /// </summary>
        /// <param name="sessionId">The ID of the session to associate.</param>
        /// <param name="environmentId">The ID of the virtual environment to associate with.</param>
        /// <returns>True if the association was successful; otherwise false.</returns>
        bool AssociateWithEnvironment(string sessionId, string environmentId);

        /// <summary>
        /// Cleans up resources associated with a session without terminating it.
        /// </summary>
        /// <param name="session">The session to clean up.</param>
        void CleanupSession(PythonSessionInfo session);

        /// <summary>
        /// Terminates a session, cleaning up all resources and marking it as terminated.
        /// </summary>
        /// <param name="sessionId">The ID of the session to terminate.</param>
        /// <returns>Error information, if any.</returns>
        PassedParameters TerminateSession(string sessionId);

        /// <summary>
        /// Performs cleanup of sessions older than the specified maximum age.
        /// </summary>
        /// <param name="maxAge">Maximum age of sessions to keep.</param>
        void PerformSessionCleanup(TimeSpan maxAge);

        /// <summary>
        /// Gets the captured output from a session.
        /// </summary>
        /// <param name="sessionId">The session ID to get output for.</param>
        /// <returns>The captured output as a string.</returns>
        Task<string> GetSessionOutput(string sessionId);

        /// <summary>
        /// Appends output to a session's captured output buffer.
        /// </summary>
        /// <param name="sessionId">The session ID to append output for.</param>
        /// <param name="output">The output to append.</param>
        void AppendSessionOutput(string sessionId, string output);

        /// <summary>
        /// Clears a session's captured output buffer.
        /// </summary>
        /// <param name="sessionId">The session ID to clear output for.</param>
        void ClearSessionOutput(string sessionId);

        /// <summary>
        /// Updates the last activity timestamp for a session.
        /// </summary>
        /// <param name="sessionId">The session ID to update.</param>
        void UpdateSessionActivity(string sessionId);

        /// <summary>
        /// Gets the current session count.
        /// </summary>
        int SessionCount { get; }

        /// <summary>
        /// Gets the number of active sessions.
        /// </summary>
        int ActiveSessionCount { get; }

        /// <summary>
        /// Gets usage metrics for session management.
        /// </summary>
        /// <returns>A dictionary of metrics</returns>
        Dictionary<string, object> GetMetrics();

        /// <summary>
        /// Executes the given action with proper concurrency control.
        /// </summary>
        /// <param name="sessionId">The session ID to execute for.</param>
        /// <param name="action">The action to execute.</param>
        /// <returns>True if the action was executed; otherwise false.</returns>
        Task<bool> ExecuteWithConcurrencyControlAsync(string sessionId, Func<Task> action);
    }
}
