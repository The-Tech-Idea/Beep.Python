using System;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea;
using TheTechIdea.Beep;
using TheTechIdea.Util;

namespace Beep.Python.Model
{
    public interface IPythonRunTimeManager
    {
       
        BinType32or64 BinType { get; set; }
        IDMEEditor DMEditor { get; }
        ObservableCollection<string> OutputLines { get; set; }
        bool IsBusy { get; set; }
        object PersistentScope { get; set; }
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
        
        bool Initialize();
        bool Initialize(string pythonhome,  BinType32or64 binType, string libpath);

        Task<PackageDefinition> FindPackageUpdate(string packageName, IProgress<PassedArgs> progress, CancellationToken token);
        Task<bool> InstallPackage(string packageName, IProgress<PassedArgs> progress, CancellationToken token);
        Task<bool> RemovePackage(string packageName, IProgress<PassedArgs> progress, CancellationToken token);
        Task<bool> UpdatePackage(string packageName, IProgress<PassedArgs> progress, CancellationToken token);
        bool IsPackageInstalled(string packageName, IProgress<PassedArgs> progress, CancellationToken token);
        Task<bool> RefreshInstalledPackagesList(IProgress<PassedArgs> progress, CancellationToken token);
        Task<bool> RefreshInstalledPackage(string packagename, IProgress<PassedArgs> progress, CancellationToken token);
        Task<bool> InstallPIP(IProgress<PassedArgs> progress, CancellationToken token);
      //  void RunPIP(string Command, string Commandpath);
        Task<IErrorsInfo> RunCode(string code, IProgress<PassedArgs> progress, CancellationToken token);
        dynamic RunCommand(string command, IProgress<PassedArgs> progress, CancellationToken token);
        Task<IErrorsInfo> RunFile(string file, IProgress<PassedArgs> progress, CancellationToken token);
        void CreateLoadConfig();
        void SaveConfig( );
        void SetRuntimePath(string runtimepath, BinType32or64 binType, string libpath = @"lib\site-packages");
        IErrorsInfo ShutDown();
        //bool IsRunTimeFound(string path);
    }
}