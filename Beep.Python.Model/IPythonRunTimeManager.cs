using Python.Runtime;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

 
 

namespace Beep.Python.Model
{
    /// <summary>
    /// Manages Python sessions and virtual environments, executing code within scoped contexts.
    /// </summary>
    public interface IPythonRunTimeManager : IDisposable
    {
        IPythonCodeExecuteManager ExecuteManager { get; set; }
        IPythonSessionManager SessionManager { get; set; }
        IPythonVirtualEnvManager VirtualEnvmanager { get; set; }
        Dictionary<string, PyModule> SessionScopes { get; }

        /// <summary>All known Python runtime configurations.</summary>
        List<PythonRunTime> PythonInstallations { get; set; }

        

        /// <summary>Acquire the GIL state for thread-safe Python calls.</summary>
        Py.GILState GIL();
        
        ObservableCollection<string> OutputLines { get; set; }
        bool IsBusy { get; set; }
        bool IsInitialized { get; }
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
        bool Initialize(PythonRunTime cfg, string virtualEnvPath, string envName, PythonEngineMode mode );

        PythonRunTime Initialize(string runtimepath);
        
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
        PassedParameters ShutDown();

        /// <summary>
        /// Shuts down a specific session.
        /// </summary>
        PassedParameters ShutDownSession(PythonSessionInfo session);

        /// <summary>
        /// Cleans up resources associated with a session.
        /// </summary>
        void CleanupSession(PythonSessionInfo session);

        /// <summary>
        /// Performs cleanup of stale sessions.
        /// </summary>
        void PerformSessionCleanup(TimeSpan maxAge);

        /// <summary>
        /// Set Operating Mode for Python engine.
        /// <see cref="PythonEngineMode"/>"/>
        ///     
        PythonEngineMode EngineMode { get; set; }

        /// <summary>
        /// Function to refresh the Python environment runtime installed on the system.
        /// and store the information in the config file. using PythonConfig.json
        /// PythonConfig Property will be updated with the new information.
        /// <see cref="PythonRunTime"/> "/>
        /// 
        void RefreshPythonInstalltions();
        PythonSessionInfo CreateSessionForSingleUserMode(PythonRunTime cfg, string envBasePath, string username, string envName);
        PythonSessionInfo CreateSessionForUser(PythonRunTime cfg, PythonVirtualEnvironment env, string username);
    }
    public enum PythonEngineMode
    {
        SingleUser,
        MultiUserWithEnvAndScopeAndSession
    }
}
