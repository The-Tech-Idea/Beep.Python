using Python.Runtime;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;

namespace Beep.Python.Model
{
    /// <summary>
    /// Manages Python sessions and virtual environments, executing code within scoped contexts.
    /// </summary>
    public interface IPythonRunTimeManager : IDisposable
    {
        IPythonSessionManager SessionManager { get; set; }
        IPythonVirtualEnvManager VirtualEnvmanager { get; set; }
        Dictionary<string, PyModule> SessionScopes { get; }

        /// <summary>All known Python runtime configurations.</summary>
        ObservableBindingList<PythonRunTime> PythonConfigs { get; set; }

        void SaveEnvironments(string filePath);
        void LoadEnvironments(string filePath);

        /// <summary>Acquire the GIL state for thread-safe Python calls.</summary>
        Py.GILState GIL();
        IDMEEditor DMEditor { get; set; }
        ObservableCollection<string> OutputLines { get; set; }
        bool IsBusy { get; set; }
        void Stop();

        /// <summary>Creates a new Python scope for the given session and environment.</summary>
        bool CreateScope(PythonSessionInfo session, PythonVirtualEnvironment environment);

        /// <summary>Creates a new Python scope for the given session using its associated environment.</summary>
        bool CreateScope(PythonSessionInfo session);

        /// <summary>
        /// Checks if a session has an associated Python scope.
        /// </summary>
        /// <param name="session">The session to check for a scope.</param>
        /// <returns>True if the session has a scope; false otherwise.</returns>
        bool HasScope(PythonSessionInfo session);

        /// <summary>
        /// Gets the Python scope associated with a session.
        /// </summary>
        /// <param name="session">The session to get the scope for.</param>
        /// <returns>The session's Python scope, or null if none exists.</returns>
        PyModule GetScope(PythonSessionInfo session);

        /// <summary>
        /// Restarts the Python runtime with a new virtual environment.
        /// </summary>
        bool RestartWithEnvironment(PythonVirtualEnvironment venv);

        /// <summary>
        /// Initializes Python with the specified runtime configuration and optional virtual environment path.
        /// </summary>
        bool Initialize(PythonRunTime cfg, string virtualEnvPath = null);

        /// <summary>
        /// Initializes Python with the specified runtime configuration and virtual environment.
        /// </summary>
        bool Initialize(PythonRunTime cfg, PythonVirtualEnvironment venv);

        /// <summary>
        /// Initializes Python with the specified home directory and library path.
        /// </summary>
        bool Initialize(string pythonHome, string libPath);

        /// <summary>
        /// Runs a Python script within a session's scope and returns the result.
        /// </summary>
        dynamic RunPythonScriptWithResult(PythonSessionInfo session, string script, Dictionary<string, object> variables);

        /// <summary>
        /// Runs Python code asynchronously within a session.
        /// </summary>
        Task<IErrorsInfo> RunCode(PythonSessionInfo session, string code, IProgress<PassedArgs> progress, CancellationToken token);

        /// <summary>
        /// Runs a Python command asynchronously within a session.
        /// </summary>
        Task<dynamic> RunCommand(PythonSessionInfo session, string command, IProgress<PassedArgs> progress, CancellationToken token);

        /// <summary>
        /// Runs a Python file asynchronously within a session.
        /// </summary>
        Task<IErrorsInfo> RunFile(PythonSessionInfo session, string file, IProgress<PassedArgs> progress, CancellationToken token);

        /// <summary>
        /// Runs a pip/command-line instruction in the given session/environment.
        /// </summary>
        Task<string> RunPythonCommandLineAsync(
            IProgress<PassedArgs> progress,
            string commandString,
            bool useConda,
            PythonSessionInfo session,
            PythonVirtualEnvironment environment);

        /// <summary>
        /// Executes Python code for a specific user and returns stdout.
        /// </summary>
        Task<string> RunPythonForUserAsync(
            PythonSessionInfo session,
            string environmentName,
            string code,
            IProgress<PassedArgs> progress);

        /// <summary>
        /// Runs Python code and captures output.
        /// </summary>
        Task<string> RunPythonCodeAndGetOutput(IProgress<PassedArgs> progress, string code, PythonSessionInfo session = null);

        /// <summary>
        /// Runs a Python script within a session's scope.
        /// </summary>
        bool RunPythonScript(string script, dynamic parameters, PythonSessionInfo session);

        /// <summary>
        /// Creates or loads the Python configuration.
        /// </summary>
        void CreateLoadConfig();

        /// <summary>
        /// Saves the current Python configuration.
        /// </summary>
        void SaveConfig();

        /// <summary>
        /// Shuts down the Python runtime.
        /// </summary>
        IErrorsInfo ShutDown();

        /// <summary>
        /// Shuts down a specific session.
        /// </summary>
        IErrorsInfo ShutDownSession(PythonSessionInfo session);

        /// <summary>
        /// Cleans up resources associated with a session.
        /// </summary>
        void CleanupSession(PythonSessionInfo session);

        /// <summary>
        /// Performs cleanup of stale sessions.
        /// </summary>
        void PerformSessionCleanup(TimeSpan maxAge);

        #region Concurrency Support

        /// <summary>
        /// Executes a Python operation with concurrency control.
        /// </summary>
        /// <typeparam name="T">The return type of the operation.</typeparam>
        /// <param name="session">The session to execute the operation in.</param>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="timeout">Optional timeout for the operation.</param>
        /// <returns>The result of the operation.</returns>
        Task<T> ExecuteWithConcurrencyControlAsync<T>(PythonSessionInfo session, Func<Task<T>> operation, TimeSpan? timeout = null);

        /// <summary>
        /// Gets the current load statistics for the Python runtime.
        /// </summary>
        /// <returns>Dictionary of load statistics.</returns>
        Dictionary<string, object> GetRuntimeMetrics();

        #endregion
    }
}
