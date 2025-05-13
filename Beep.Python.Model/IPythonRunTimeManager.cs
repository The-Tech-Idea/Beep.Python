using Python.Runtime;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Editor;
using static System.Collections.Specialized.BitVector32;

namespace Beep.Python.Model
{

    /// <summary>
    /// Manages Python sessions and virtual environments, executing code within scoped contexts.
    /// </summary>
    public interface IPythonRunTimeManager : IDisposable
    {
        Dictionary<string, PyModule> SessionScopes { get; }


        /// <summary>Currently active session.</summary>
      //  PythonSessionInfo CurrentSession { get; }

        /// <summary>All managed sessions.</summary>
        ObservableBindingList<PythonSessionInfo> Sessions { get; set; }

        /// <summary>Currently selected Python virtual environment.</summary>
        //PythonVirtualEnvironment CurrentVirtualEnvironment { get; set; }

        /// <summary>All known virtual environments.</summary>
        ObservableBindingList<PythonVirtualEnvironment> ManagedVirtualEnvironments { get; }

        void SaveEnvironments(string filePath);
        void LoadEnvironments(string filePath);

        /// <summary>Acquire the GIL state for thread-safe Python calls.</summary>
        Py.GILState GIL();

        BinType32or64 BinType { get; set; }
        IDMEEditor DMEditor { get; set; }
        ObservableCollection<string> OutputLines { get; set; }
        bool IsBusy { get; set; }
        void Stop();

        /// <summary>Persistent module scope across commands.</summary>
      //  PyModule CurrentPersistentScope { get; set; }

        /// <summary>Creates a new Python scope for the given session and environment.</summary>
        bool CreateScope(PythonSessionInfo session, PythonVirtualEnvironment environment);
        bool CreateScope(PythonSessionInfo session);


      //  PythonConfiguration PythonConfig { get; set; }
    //    PythonRunTime CurrentRuntimeConfig { get; }
   //     string CurrentFileLoaded { get; set; }
        bool IsConfigLoaded { get; }
        bool IsCompilerAvailable { get; }
        bool IsInitialized { get; }
        /// <summary>
        /// Add to IPythonRunTimeManager interface
        /// </summary>

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

        bool PickConfig(string path);
        bool PickConfig(PythonRunTime cfg);
        bool InitializeForUser(string envBasePath, string username);
        bool InitializeForUser(PythonSessionInfo sessionInfo);
        bool RestartWithEnvironment(PythonVirtualEnvironment venv);
        bool CreateVirtualEnvironmentFromDefinition(PythonVirtualEnvironment env);
        bool Initialize(string virtualEnvPath = null);
        bool Initialize(PythonVirtualEnvironment venv);
        bool Initialize(string pythonHome, BinType32or64 binType, string libPath);

        Task<IErrorsInfo> RunCode(PythonSessionInfo session, string code, IProgress<PassedArgs> progress, CancellationToken token);
        Task<dynamic> RunCommand(PythonSessionInfo session, string command, IProgress<PassedArgs> progress, CancellationToken token);
        Task<IErrorsInfo> RunFile(PythonSessionInfo session, string file, IProgress<PassedArgs> progress, CancellationToken token);

        /// <summary>Runs a pip/command-line instruction in the given session/environment.</summary>
        Task<string> RunPythonCommandLineAsync(
            IProgress<PassedArgs> progress,
            string commandString,
            bool useConda,
            PythonSessionInfo session,
            PythonVirtualEnvironment environment);

        /// <summary>Executes Python code and returns stdout.</summary>
        Task<string> RunPythonForUserAsync(
            PythonSessionInfo session,
            string environmentName,
            string code,
        IProgress<PassedArgs> progress);

        bool RunPythonScript(string script, dynamic parameters,PythonSessionInfo session);
        void CreateLoadConfig();
        void SaveConfig();

        void SetRuntimePath(string runtimePath, BinType32or64 binType, string libPath = @"lib\site-packages");
        IErrorsInfo ShutDown();
        IErrorsInfo ShutDownSession(PythonSessionInfo session);
        void CleanupSession(PythonSessionInfo session);
        void PerformSessionCleanup(TimeSpan maxAge);
      
    }
}
