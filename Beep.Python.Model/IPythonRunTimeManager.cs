using Python.Runtime;
using System;
using System.Collections.ObjectModel;

using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Editor;


namespace Beep.Python.Model
{
    public interface IPythonRunTimeManager:IDisposable
    {
         PythonSessionInfo CurrentSession { get;  }

         List<PythonSessionInfo> Sessions { get; set; }
         PythonVirtualEnvironment CurrentVirtualEnvironment { get; set; }

         List<PythonVirtualEnvironment> ManagedVirtualEnvironments { get; }
      
        Py.GILState GIL();
        BinType32or64 BinType { get; set; }
        IDMEEditor DMEditor { get; set; }
       
        ObservableCollection<string> OutputLines { get; set; }
        bool IsBusy { get; set; }
        void Stop();
        PyModule CurrentPersistentScope { get; set; }
        bool CreateScope();
        PythonConfiguration PythonConfig { get; set; }
     
        PythonRunTime CurrentRuntimeConfig { get;  }
        string CurrentFileLoaded { get; set; }
        bool IsConfigLoaded { get;  }
        bool IsCompilerAvailable { get;  }
        bool IsInitialized { get;  }
        void Dispose();
         bool PickConfig(string path);
         bool PickConfig(PythonRunTime cfg);
        bool InitializeForUser(string envBasePath, string username);
        bool RestartWithEnvironment(PythonVirtualEnvironment venv);
        bool CreateVirtualEnvironmentFromDefinition(PythonVirtualEnvironment env);
        bool Initialize(string virtualEnvPath = null);
        bool Initialize(PythonVirtualEnvironment venv);
        bool Initialize(string pythonhome,  BinType32or64 binType, string libpath);
        Task<IErrorsInfo> RunCode(string code, IProgress<PassedArgs> progress, CancellationToken token);
        Task<dynamic> RunCommand(string command, IProgress<PassedArgs> progress, CancellationToken token);
        Task<IErrorsInfo> RunFile(string file, IProgress<PassedArgs> progress, CancellationToken token);
        Task<string> RunPythonCommandLineAsync(  IProgress<PassedArgs> progress, string commandstring, bool useConda = false);
        bool RunPythonScript(string script, dynamic parameters);
        void CreateLoadConfig();
        void SaveConfig( );
        void SetRuntimePath(string runtimepath, BinType32or64 binType, string libpath = @"lib\site-Packages");
        IErrorsInfo ShutDown();
       
    }
}