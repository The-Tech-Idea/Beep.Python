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
using System.Threading.Channels;
using System.Net;


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
        public PythonNetRunTimeManager(IDMEEditor dMEditor) // @"W:\Cpython\p395x32"
        {
            DMEditor = dMEditor;
            JsonLoader = dMEditor.ConfigEditor.JsonLoader;
            PythonRunTimeDiagnostics.SetFolderNames("x32", "x64");

        }
        public PythonNetRunTimeManager() // @"W:\Cpython\p395x32"
        {
            
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
        public object PersistentScope { get; set; }
        public bool CreateScope()
        {
            if (PersistentScope == null)
            {
                PersistentScope = Py.CreateScope();
                return true;
            }
            else
            {
                return false;
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
        public IDMEEditor DMEditor { get; set; }
        public IJsonLoader JsonLoader { get; set; }
        public BinType32or64 BinType { get; set; } = BinType32or64.p395x32;
      
     
        #region "Initialization and Shutdown"
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
                    Environment.SetEnvironmentVariable("PYTHONPATH", $"{CurrentRuntimeConfig.Packageinstallpath};", EnvironmentVariableTarget.Process);
                    try
                    {
                        PassedArgs args = new PassedArgs();
                        ReportProgress("Init. of Python engine");


                        //  Runtime.PythonRuntimePath= CurrentRuntimeConfig.BinPath;
                        Runtime.PythonDLL = CurrentRuntimeConfig.PythonDll;
                        PythonEngine.PythonHome = CurrentRuntimeConfig.BinPath;
                        //       PythonEngine.PythonPath = CurrentRuntimeConfig.Packageinstallpath;
                        PythonEngine.Initialize();



                        ReportProgress("Finished Init. of Python engine");

                        IsBusy = false;
                        _IsInitialized = true;

                        ReportProgress("Python Initialized Successfully", Errors.Ok);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        IsBusy = false;
                        ReportProgress(ex.Message, Errors.Failed);
                        return false;
                    }
                }
                else
                {
                    IsBusy = false;
                    ReportProgress("Python Already Initialized", Errors.Ok);
                    return true;
                }


            }
            else
            {
                ReportProgress("No Python Available", Errors.Failed);
                IsBusy = false;
                return false;
            }
            IsBusy = false;
        }
        public IErrorsInfo ShutDown()
        {
            ErrorsInfo er = new ErrorsInfo();
            er.Flag = Errors.Ok;
            if (IsBusy) return er;
            IsBusy = true;

            try
            {
                if (PersistentScope != null)
                {
                    PyModule a = (PyModule)PersistentScope;
                    a.Dispose();
                }

                PythonEngine.Shutdown();
                _IsInitialized = false;
            }
            catch (Exception ex)
            {
                er.Ex = ex;
                er.Flag = Errors.Failed;
                er.Message = ex.Message;

            }
            IsBusy = false;
            return er;

        }
        #endregion "Initialization and Shutdown"
        #region "Configuration Methods"
        private bool GetIsPythonReady()
        {
            if (IsCompilerAvailable)
            {
                if (PythonEngine.IsInitialized)
                {
                    _IsInitialized = true;
                    return true;
                }
                else
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
                if (PythonConfig.RunTimeIndex > -1)
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
            }
            else
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
            }
            else
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
            if (IsBusy) return false;
            // IsBusy = true;
            try
            {
                if (PythonRunTimeDiagnostics.IsPythonInstalled(pythonhome))
                {
                    PythonRunTime cfg;
                    int idx = PythonConfig.Runtimes.FindIndex(p => p.BinPath.Equals(pythonhome, StringComparison.InvariantCultureIgnoreCase));
                    if (idx == -1)
                    {
                        cfg = new PythonRunTime();
                        cfg = PythonRunTimeDiagnostics.GetPythonConfig(pythonhome);
                        PythonConfig.Runtimes.Add(cfg);
                        idx = PythonConfig.Runtimes.IndexOf(cfg);
                    }
                    cfg = PythonConfig.Runtimes[idx];
                    PythonConfig.RunTimeIndex = idx;

                    return Initialize();
                }
                else
                {
                    //         DMEditor.AddLogMessage("Beep AI Python", "No Python Available", DateTime.Now, 0, null, Errors.Failed);
                    //  IsBusy = false;
                    return false;
                }
            }
            catch (Exception ex)
            {
                DMEditor.AddLogMessage("Beep AI Python", $"{ex.Message} ", DateTime.Now, 0, null, Errors.Failed);
                //  IsBusy = false;
            }
            return false;

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
                }
                else return false;
            }
            else return false;
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
        public void SetRuntimePath(string runtimepath, BinType32or64 binType, string libpath = @"lib\site-packages")
        {

            Initialize(CurrentRuntimeConfig.RuntimePath, binType);
            SaveConfig();


        }
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
            if(JsonLoader== null)
            {
                JsonLoader = DMEditor.ConfigEditor.JsonLoader;
            }
            JsonLoader.Serialize(configfile, PythonConfig);
            IsConfigLoaded = true;


        }
        #endregion "Configuration Methods"
        #region "Python Run Code"
        public async Task<IErrorsInfo> RunFile(string file, IProgress<PassedArgs> progress, CancellationToken token)
        {
            DMEditor.ErrorObject.Flag = Errors.Ok;
            if (IsBusy) return DMEditor.ErrorObject;
            IsBusy = true;
            try
            {


                string code = $"{PythonRunTimeDiagnostics.GetPythonExe(CurrentRuntimeConfig.BinPath)} {file}";// File.ReadAllText(file); // Get the python file as raw text

               await RunPythonCodeAndGetOutput(progress, code);
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

               await RunPythonCodeAndGetOutput(progress, code);


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
        public async Task<dynamic> RunCommand(string command, IProgress<PassedArgs> progress, CancellationToken token)
        {
            PyObject pyObject = null;
            DMEditor.ErrorObject.Flag = Errors.Ok;
            try
            {
                if (IsBusy) { return false; }
                IsBusy = true;

               await RunPythonCodeAndGetOutput(progress, command);


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
        public async Task<string> RunPythonCodeAndGetOutput(IProgress<PassedArgs> progress, string code)
        {
            string wrappedPythonCode = $@"
import sys
import io
import clr

class CustomStringIO(io.StringIO):
    def write(self, s):
        super().write(s)
        output = self.getvalue()
        if output.strip():
            OutputHandler(output.strip())
            self.truncate(0)  # Clear the internal buffer
            self.seek(0)  # Reset the buffer pointer

def capture_output(code, globals_dict):
    original_stdout = sys.stdout
    sys.stdout = CustomStringIO()

    try:
        exec(code, dict(globals_dict))
    finally:
        sys.stdout = original_stdout
";
            bool isImage = false;
            string output = "";

            using (Py.GIL())
            {
                using (PyModule scope = Py.CreateScope())
                {


                    Action<string> OutputHandler = line =>
                    {
                        // runTimeManager.OutputLines.Add(line);
                        progress.Report(new PassedArgs() { Messege = line });
                        Console.WriteLine(line);
                    };
                    scope.Set(nameof(OutputHandler), OutputHandler);

                    scope.Exec(wrappedPythonCode);
                    PyObject captureOutputFunc = scope.GetAttr("capture_output");
                    Dictionary<string, object> globalsDict = new Dictionary<string, object>();

                    PyObject pyCode = code.ToPython();
                    PyObject pyGlobalsDict = globalsDict.ToPython();
                    PyObject result = captureOutputFunc.Invoke(pyCode, pyGlobalsDict);
                    if (result is PyObject pyObj)
                    {
                        var pyObjType = pyObj.GetPythonType();
                        var pyObjTypeName = pyObjType.ToString();


                    }
                }
            }

            IsBusy = false;
            return output;
        }

        #endregion "Python Run Code"
      
        #region "Utility Methods"
        private void ReportProgress(PassedArgs args)
        {
            if (Progress != null)
            {
                Progress.Report(args);
            }
        }
        private void ReportProgress(string messege, Errors flag = Errors.Ok)
        {
            if (Progress != null)
            {
                PassedArgs args = new PassedArgs();
                args.Messege = messege;
                Progress.Report(args);
            }
            else if (DMEditor != null)
            {
                if (DMEditor.progress != null)
                {
                    Progress = DMEditor.progress;
                    PassedArgs args = new PassedArgs();
                    args.Messege = messege;
                    Progress.Report(args);
                }
                DMEditor.AddLogMessage("Beep AI Python", messege, DateTime.Now, 0, null, flag);
            }
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
        #endregion "Utility Methods"
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
