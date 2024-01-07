using System;
using System.Collections.Generic;
using TheTechIdea.Beep;
using Python.Runtime;
using TheTechIdea.Util;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using TheTechIdea;
using System.Collections.ObjectModel;
using Beep.Python.Model;
using TheTechIdea.Beep.ConfigUtil;
using System.Linq;

namespace Beep.Python.RuntimeEngine
{
    public class PythonNetRunTimeManager : IDisposable, IPythonRunTimeManager
    {
        public PythonNetRunTimeManager(IDMEEditor dMEditor, IJsonLoader jsonLoader, IProgress<PassedArgs> progress,
        CancellationToken token) // @"W:\Cpython\p395x32"
        {
            DMEditor = dMEditor;
            JsonLoader = jsonLoader;
            Progress = progress;
            Token = token;
            PythonRunTimeDiagnostics.SetFolderNames("x32", "x64");

        }
        IProgress<PassedArgs> Progress;
        CancellationToken Token;
        bool _IsInitialized = false;
        private bool disposedValue;
        public PythonRunTime CurrentRuntimeConfig
        {
            get
            {
                if (PythonConfig.RunTimeIndex >= 0)
                {
                    return PythonConfig.Runtimes[PythonConfig.RunTimeIndex];
                }
                else
                    return null;

            }
        }
          
        public bool IsInitialized => GetIsPythonReady();
        public bool IsCompilerAvailable => GetIsPythonAvailable();
        public ObservableCollection<string> OutputLines { get; set; } = new ObservableCollection<string>();
        public bool IsBusy { get; set; } = false;
        public IPIPManager PIPManager { get; set; }
        public PythonConfiguration PythonConfig { get; set; } = new PythonConfiguration();
        public bool IsConfigLoaded { get {return  GetIsConfigLoaded(); } set { } } 
        public bool IsRunning { get; set; }
        public string CurrentFileLoaded { get; set; }
        public bool IsPathChanged { get; set; } = false;
        public string NewPath { get; set; } = null;
        public IDMEEditor DMEditor { get; }
        public IJsonLoader JsonLoader { get; }
        public BinType32or64 BinType { get; set; } = BinType32or64.p395x32;
        private bool GetIsPythonReady()
        {
            if (IsCompilerAvailable)
            {
                if (PythonEngine.IsInitialized)
                {
                    _IsInitialized = true;
                    return true;
                }else
                    return false;
            }
            else
            {
                return false;
            }
            
        }
        private bool GetIsPythonAvailable()
        {
            if (PythonConfig != null)
            {
                if (PythonConfig.RunTimeIndex >-1)
                {
                    if (!string.IsNullOrEmpty(CurrentRuntimeConfig.BinPath))
                    {
                        if (PythonRunTimeDiagnostics.IsPythonInstalled(CurrentRuntimeConfig.BinPath))
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                        return false;
                   
                }
                else
                {
                    return false;
                }
            }else
            {
                return false;
            }
        }
        private bool GetIsConfigLoaded()
        {
            if (PythonConfig != null)
            {
                if (PythonConfig.Runtimes != null)
                {
                    if (PythonConfig.RunTimeIndex > -1)
                    {
                       
                            return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }else
            {
                return false;
            }
        }
        public static void AddEnvPath(params string[] paths)
        {
            var envPaths = Environment.GetEnvironmentVariable("PATH").Split(Path.PathSeparator).ToList();
            foreach (var path in paths)
            {
                if (path.Length > 0 && !envPaths.Contains(path))
                {
                    envPaths.Insert(0, path);
                }
            }
            Environment.SetEnvironmentVariable("PATH", string.Join(Path.PathSeparator.ToString(), envPaths), EnvironmentVariableTarget.Process);
        }
        public bool Initialize(string pythonhome, BinType32or64 binType, string libpath = @"lib\site-packages")
        {
            if(IsBusy) return false;
            IsBusy = true;
            if (PythonRunTimeDiagnostics.IsPythonInstalled(pythonhome))
            {
                int idx= PythonConfig.Runtimes.FindIndex(p => p.BinPath.Equals(pythonhome,StringComparison.InvariantCultureIgnoreCase));
                PythonRunTime cfg= PythonConfig.Runtimes[idx];
                PythonConfig.RunTimeIndex = idx;
               
               return Initialize();
            }
            else
            {
                DMEditor.AddLogMessage("Beep AI Python", "No Python Available", DateTime.Now, 0, null, Errors.Failed);
                IsBusy = false;
                return false;
            }
            
        }
        public bool Initialize()
        {
           
            if (IsBusy) return false;
            IsBusy = true;
            if (CurrentRuntimeConfig.IsPythonInstalled)
            {
                if (!PythonEngine.IsInitialized)
                {
                  
                    PythonRunTimeDiagnostics.SetAiFolderPath(DMEditor);
                    Environment.SetEnvironmentVariable("PATH", $"{CurrentRuntimeConfig.BinPath};{CurrentRuntimeConfig.ScriptPath};" + Environment.GetEnvironmentVariable("PATH"), EnvironmentVariableTarget.Process);
                    Environment.SetEnvironmentVariable("PYTHONNET_RUNTIME", $"{CurrentRuntimeConfig.BinPath}", EnvironmentVariableTarget.Process);
                    Environment.SetEnvironmentVariable("PYTHONNET_PYTHON_RUNTIME", $"{CurrentRuntimeConfig.BinPath}", EnvironmentVariableTarget.Process);
                    Environment.SetEnvironmentVariable("PYTHONHOME", CurrentRuntimeConfig.BinPath, EnvironmentVariableTarget.Process);
                    Environment.SetEnvironmentVariable("PYTHONPATH", $"{ CurrentRuntimeConfig.Packageinstallpath};", EnvironmentVariableTarget.Process);
                    try
                    {
                        PassedArgs args = new PassedArgs();
                        args.Messege = "Init. of Python engine";
                        DMEditor.progress.Report(args);
                        Runtime.PythonDLL = CurrentRuntimeConfig.PythonDll;
                        PythonEngine.Initialize();
                        PythonEngine.PythonHome = CurrentRuntimeConfig.BinPath;
                        PythonEngine.PythonPath = CurrentRuntimeConfig.Packageinstallpath;
                    
                        args.Messege = "Finished Init. of Python engine";
                        DMEditor.progress.Report(args);
                        IsBusy = false;
                        _IsInitialized = true;
                       
                        DMEditor.AddLogMessage("Beep AI Python", "Python Initialized Successfully", DateTime.Now, 0, null, Errors.Ok);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        IsBusy = false;
                        DMEditor.AddLogMessage("Beep AI Python", ex.Message, DateTime.Now, 0, null, Errors.Failed);
                        return false;
                    }
                }
                else
                {
                    IsBusy = false;
                    DMEditor.AddLogMessage("Beep AI Python", "Python Already Initialized", DateTime.Now, 0, null, Errors.Ok);
                    return true;
                }
                    

            }
            else
            {
                DMEditor.AddLogMessage("Beep AI Python", "No Python Available", DateTime.Now, 0, null, Errors.Failed);
                IsBusy = false;
                return false;
            }
            IsBusy = false;
        }
        public bool PickConfig(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                if (PythonRunTimeDiagnostics.IsPythonInstalled(path))
                {
                    //IsCompilerAvailable = true;
                    PythonRunTime cfg = PythonRunTimeDiagnostics.GetPythonConfig(path);
                    PythonConfig.Runtimes.Add(cfg);
                    int idx = PythonConfig.Runtimes.IndexOf(cfg);
                    if (idx == -1)
                    {
                        return false;
                    }
                    else
                    {
                        PythonConfig.RunTimeIndex = idx;
                        SaveConfig();
                        Initialize();
                        return true;
                    }
                }else return false;
            }else return false;
        }
        public bool PickConfig(PythonRunTime cfg)
        {

            int idx = PythonConfig.Runtimes.IndexOf(cfg);
            if (idx == -1)
            {
                return false;
            }
            else
            {
                PythonConfig.RunTimeIndex = idx;
                SaveConfig();
                Initialize();
                return true;
            }

        }
        public IErrorsInfo ShutDown()
        {
            if (IsBusy) return DMEditor.ErrorObject; 
            IsBusy = true;
            DMEditor.ErrorObject.Flag = Errors.Ok;
            try
            {
                PythonEngine.Shutdown();
                //IsInitialized = false;
            }
            catch (Exception ex)
            {
                DMEditor.AddLogMessage("Beep AI Python", ex.Message, DateTime.Now, 0, null, Errors.Failed);
            }
            IsBusy = false;
            return DMEditor.ErrorObject;

        }
        public async Task<IErrorsInfo> RunFile(string file, IProgress<PassedArgs> progress, CancellationToken token)
        {
            DMEditor.ErrorObject.Flag = Errors.Ok;
            if (IsBusy) return DMEditor.ErrorObject;
            IsBusy = true;
            try
            {


                string code = $"{PythonRunTimeDiagnostics.GetPythonExe(CurrentRuntimeConfig.BinPath)} {file}";// File.ReadAllText(file); // Get the python file as raw text

                PythonNetManager.RunPythonCodeAndGetOutput(this,progress,code);
                IsBusy = false;
            }
            catch (Exception ex)
            {
                IsBusy = false;
                DMEditor.AddLogMessage("Beep AI Python", ex.Message, DateTime.Now, 0, null, Errors.Failed);
            }
            IsBusy = false;
            return DMEditor.ErrorObject;

        }
        public async Task<IErrorsInfo> RunCode(string code, IProgress<PassedArgs> progress, CancellationToken token)
        {
            DMEditor.ErrorObject.Flag = Errors.Ok;

            try
            {
               // PythonNetManager.RunPythonCodeAndGetOutput(this,progress,code);
                PythonNetManager.RunPythonCodeAndGetOutput(this, progress, code);
                // RunPythonCodeAndGetOutput(code);
                IsBusy = false;
            }
            catch (Exception ex)
            {
                IsBusy = false;
               // Py.GIL().Dispose();
                DMEditor.AddLogMessage("Beep AI Python", ex.Message, DateTime.Now, 0, null, Errors.Failed);
            }
            IsBusy = false;
            return DMEditor.ErrorObject;

        }
        public dynamic RunCommand(string command, IProgress<PassedArgs> progress, CancellationToken token)
        {
            PyObject pyObject = null;
            DMEditor.ErrorObject.Flag = Errors.Ok;
            try
            {
                 if (IsBusy) { return false; }
                    IsBusy = true;
                PythonNetManager.RunPythonCodeAndGetOutput(this,progress,command);
                IsBusy = false;
            }
            catch (Exception ex)
            {
                IsBusy = false;
                DMEditor.AddLogMessage("Beep AI Python", ex.Message, DateTime.Now, 0, null, Errors.Failed);
            }
            IsBusy = false;
            return pyObject;

        }
        public async Task<PackageDefinition> FindPackageUpdate(string packageName, IProgress<PassedArgs> progress, CancellationToken token)
        {
            bool IsInternetAvailabe = PythonRunTimeDiagnostics.CheckNet();
            PackageDefinition retval = null;
            DMEditor.ErrorObject.Flag = Errors.Ok;
            if (IsInternetAvailabe)
            {
                retval = await PythonRunTimeDiagnostics.CheckIfPackageExistsAsync(packageName);
            }
           
            PackageDefinition installedpackage = null;
            bool isInstalled = false;
            int idx = -1;
            if (IsBusy) return null;
            IsBusy = true;

            try
            {
                // Create a new Python scope
                using (Py.GIL())
                {
                    // dynamic pip = Py.Import("pip");

                    //string packageName = "requests"; // Replace with the name of the package you are interested in


                    // Check if an update is available
                    bool isUpdateAvailable = false;
                    if (retval != null)
                    {

                         await PythonNetManager.listpackagesAsync(this, progress, token).ConfigureAwait(true);



                        installedpackage = CurrentRuntimeConfig.Packagelist.Where(p => p.packagename.Equals(packageName, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                        idx = CurrentRuntimeConfig.Packagelist.IndexOf(installedpackage);
                        isUpdateAvailable = (new Version(retval.updateversion) > new Version(installedpackage.version));
                    }

                    // Print the result
                    if (isUpdateAvailable)
                    {
                        isInstalled = true;
                        installedpackage.updateversion = retval.updateversion;
                        installedpackage.buttondisplay = "Update";
                        CurrentRuntimeConfig.Packagelist[idx] = installedpackage;
                        //OutputLines.Add($"An update to {packageName} is available ({retval.updateversion}).");
                        progress.Report(new PassedArgs() { Messege = $"An update to {packageName} is available ({retval.updateversion})" });
                        Console.WriteLine($"An update to {packageName} is available ({retval.updateversion}).");
                    }
                    else
                    {
                        isInstalled = false;
                        installedpackage.buttondisplay = "Installed";
                        installedpackage.updateversion = installedpackage.version;
                        progress.Report(new PassedArgs() { Messege = $"No update to {packageName} is available." });
                        Console.WriteLine($"No update to {packageName} is available.");
                    }
                }

            }
            catch (Exception ex)
            {
                isInstalled = false;
                DMEditor.AddLogMessage("Beep AI Python", ex.Message, DateTime.Now, 0, null, Errors.Failed);
            }
            IsBusy = false;
            return installedpackage;
        }
        public  bool IsPackageInstalled(string packageName, IProgress<PassedArgs> progress, CancellationToken token)
        {
            DMEditor.ErrorObject.Flag = Errors.Ok;
            bool isInstalled = false;
           
            try
            {
                // Create a new Python scope
                // Check if a package is installed and capture output
                using (Py.GIL())
                {
                    dynamic scope = Py.CreateScope();
                    // string packageName = "numpy"; // Replace with the name of the package you want to check
                    string code = @"
        import subprocess
        output = subprocess.check_output(['pip', 'list'])
        print(output.decode('utf-8'))
    ";
                     PythonEngine.Exec(code, scope);
                    string output = scope.get("__builtins__").get("print")?.ToString();
                    // Console.WriteLine(output);

                    isInstalled = output.Contains(packageName);
                    string outputmessage = $"Package '{packageName}' is {(isInstalled ? "installed" : "not installed")}";
                    Console.WriteLine(outputmessage);
                   // OutputLines.Add(outputmessage);
                    isInstalled = true;
                }

            }
            catch (Exception ex)
            {
                IsBusy = false;
                isInstalled = false;
                DMEditor.AddLogMessage("Beep AI Python", ex.Message, DateTime.Now, 0, null, Errors.Failed);
            }
            IsBusy = false;
            return isInstalled;
        }
        public async Task<bool> InstallPackage(string packageName, IProgress<PassedArgs> progress, CancellationToken token)
        {
            DMEditor.ErrorObject.Flag = Errors.Ok;
         
          

            try
                {if(IsBusy) { return false; }
                IsBusy = true;

                await PythonNetManager.RunPackageManagerAsync(this, progress, packageName, PackageAction.Install, PythonRunTimeDiagnostics.GetPackageType(CurrentRuntimeConfig.BinPath) == PackageType.conda ).ConfigureAwait(false);
                IsBusy = false;
            }
            catch (Exception ex)
            {
                IsBusy = false;
                DMEditor.AddLogMessage("Beep AI Python", ex.Message, DateTime.Now, 0, null, Errors.Failed);
            }
            IsBusy = false;
            return true;
        }
        public async Task<bool> RemovePackage(string packageName, IProgress<PassedArgs> progress, CancellationToken token)
        {
            DMEditor.ErrorObject.Flag = Errors.Ok;

            bool isInstalled = false;
            
            try
            {
                if(IsBusy) { return false; }    
                
                await PythonNetManager.RunPackageManagerAsync(this, progress, packageName, PackageAction.Remove, false).ConfigureAwait(false);
                IsBusy = false;
            }
            catch (Exception ex)
            {
                IsBusy = false;
                isInstalled = false;
                DMEditor.AddLogMessage("Beep AI Python", ex.Message, DateTime.Now, 0, null, Errors.Failed);
            }
            IsBusy = false;
            return isInstalled;
        }
        public async Task<bool> UpdatePackage(string packageName, IProgress<PassedArgs> progress, CancellationToken token)
        {
            DMEditor.ErrorObject.Flag = Errors.Ok;
          
            bool isInstalled = false;
            try
            {
               
                    if (packageName.Equals("pip", StringComparison.CurrentCultureIgnoreCase))
                    {
                        await PythonNetManager.RunPackageManagerAsync(this, progress, packageName, PackageAction.UpgradePackager, PythonRunTimeDiagnostics.GetPackageType(CurrentRuntimeConfig.BinPath) == PackageType.conda).ConfigureAwait(false);
                }
                    else await PythonNetManager.RunPackageManagerAsync(this, progress, packageName, PackageAction.Update, PythonRunTimeDiagnostics.GetPackageType(CurrentRuntimeConfig.BinPath) == PackageType.conda).ConfigureAwait(false);


                IsBusy = false;

            }
            catch (Exception ex)
            {
                IsBusy = false;
                isInstalled = false;
                DMEditor.AddLogMessage("Beep AI Python", ex.Message, DateTime.Now, 0, null, Errors.Failed);
            }
            IsBusy = false;
            return isInstalled;
        }
        public async Task<bool> RefreshInstalledPackagesList(IProgress<PassedArgs> progress, CancellationToken token)
        {
           
            try
            {
                var retval=await PythonNetManager.listpackagesAsync(this,progress, token).ConfigureAwait(true);
                
                IsBusy = false;
                return true;
            }
            catch (Exception ex)
            {
                IsBusy = false;
                DMEditor.AddLogMessage("Beep AI Python", ex.Message, DateTime.Now, 0, null, Errors.Failed);
                return false;
            }
           
        }
        public async Task<bool> RefreshInstalledPackage(string packagename,IProgress<PassedArgs> progress, CancellationToken token)
        {
           
            try
            {
                var retval = await PythonNetManager.listpackagesAsync(this, progress, token).ConfigureAwait(true);
                IsBusy = false;
                return true;
            }
            catch (Exception ex)
            {
                IsBusy = false;
                DMEditor.AddLogMessage("Beep AI Python", ex.Message, DateTime.Now, 0, null, Errors.Failed);
                return false;
            }
          
        }
        public async Task<bool> InstallPIP(IProgress<PassedArgs> progress, CancellationToken token)
        {
            try
            {
                var retval = await PythonNetManager.InstallPIP(this, progress, token).ConfigureAwait(true);
                IsBusy = false;
                return true;
            }
            catch (Exception ex)
            {
                IsBusy = false;
                DMEditor.AddLogMessage("Beep AI Python", ex.Message, DateTime.Now, 0, null, Errors.Failed);
                return false;
            }

        }
        #region "Configuration File"
        public void CreateLoadConfig()
        {
            string configfile = Path.Combine(DMEditor.ConfigEditor.ConfigPath, "cpython.config");
            if (File.Exists(configfile) && !IsConfigLoaded)
            {
                PythonConfig = JsonLoader.DeserializeSingleObject<PythonConfiguration>(configfile);

                IsConfigLoaded = true;
                if (PythonConfig.Runtimes.Count > 0)
                {
                    if(PythonConfig.RunTimeIndex >-1)
                    {
                        //IsCompilerAvailable= PythonRunTimeDiagnostics.IsPythonInstalled(CurrentRuntimeConfig.BinPath);
                    }
                }
               
            }
            else
            {
                if (PythonConfig.RunTimeIndex <0)
                {
                    PythonRunTime config = new PythonRunTime();
                    config.IsPythonInstalled = false;
                    config.RuntimePath = string.Empty;
                    config.Message = "No Python Runtime Found";
                    PythonConfig.Runtimes.Add(config);
                    PythonConfig.RunTimeIndex = -1;

                    JsonLoader.Serialize(configfile, PythonConfig);
                }
                // IsCompilerAvailable = false;
                IsConfigLoaded=true;
            }
        }
        public void SaveConfig()
        {
            string configfile = Path.Combine(DMEditor.ConfigEditor.ConfigPath, "cpython.config");
            if (PythonConfig == null)
            {
                PythonConfig = new PythonConfiguration();
            }

            JsonLoader.Serialize(configfile, PythonConfig);
            IsConfigLoaded = true;


        }
        #endregion
      
        public void SetRuntimePath(string runtimepath, BinType32or64 binType, string libpath = @"lib\site-packages")
        {
           
            Initialize(CurrentRuntimeConfig.RuntimePath, binType);
            SaveConfig();
         

        }

        
        public static PyObject ToPython(IDictionary<string, object> dictionary)
        {
            using (Py.GIL())
            {
                var pyDict = new PyDict();
                foreach (var kvp in dictionary)
                {
                    PyObject key = new PyString(kvp.Key);
                    PyObject value = kvp.Value.ToPython();
                    pyDict.SetItem(key, value);
                    key.Dispose();
                    value.Dispose();
                }
                return pyDict;
            }
        }
        public static PyObject ToPython(object obj)
        {
            using (Py.GIL())
            {
                return PyObject.FromManagedObject(obj);
            }
        }
      
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    ShutDown();
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~PythonNetRunTimeManager()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
