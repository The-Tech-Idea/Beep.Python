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

using System.Net.Http;
using System.Diagnostics;
using TheTechIdea.Beep.Container.Services;
using static System.Formats.Asn1.AsnWriter;


namespace Beep.Python.RuntimeEngine
{
    public class PythonNetRunTimeManager : IDisposable, IPythonRunTimeManager
    {
        public PythonNetRunTimeManager(IBeepService beepService) // @"W:\Cpython\p395x32"
        {
            _beepService = beepService;
            DMEditor= beepService.DMEEditor;
            JsonLoader = DMEditor.ConfigEditor.JsonLoader;
            
            PythonRunTimeDiagnostics.SetFolderNames("x32", "x64");

        }
        IProgress<PassedArgs> Progress;
        CancellationToken Token;
        bool _IsInitialized = false;
        private bool disposedValue;
        private readonly IBeepService _beepService;
   

        public Py.GILState GIL()
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("Python runtime is not initialized.");
            }

            return Py.GIL();
        }
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
        public PyModule PersistentScope { get; set; }
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
      //  public IPIPManager PIPManager { get; set; }
      //  public IPackageManagerViewModel PackageManager { get; set; }
        public PythonConfiguration PythonConfig { get; set; } = new PythonConfiguration();
        public bool IsConfigLoaded { get {return  GetIsConfigLoaded(); } set { } } 
        public bool IsRunning { get; set; }
        public string CurrentFileLoaded { get; set; }
        public bool IsPathChanged { get; set; } = false;
        public string NewPath { get; set; } = null;
        public IDMEEditor DMEditor { get; set; }
        public IJsonLoader JsonLoader { get; set; }

        private string pythonpath;

        public BinType32or64 BinType { get; set; } = BinType32or64.p395x32;

        string configfile;
        #region "Initialization and Shutdown"
        public bool InitializeForUser(string envBasePath,string username)
        {
            
            string userEnvPath = Path.Combine(envBasePath, username);

            if (!Directory.Exists(userEnvPath))
            {
                // Create the virtual environment if it does not exist
                bool creationSuccess = CreateVirtualEnvironment(userEnvPath);
                if (!creationSuccess)
                {
                    return false;
                }
            }

            return Initialize(userEnvPath);  // Call to the modified Initialize method with the path to the virtual environment
        }
        public bool CreateVirtualEnvironmentFromCommand(string envPath)
        {
            if (Directory.Exists(envPath))
            {
                Console.WriteLine("Virtual environment already exists.");
                return true; // No need to create if it already exists
            }

            try
            {
                // Command to create virtual environment
                var process = new Process()
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "python", // Ensure this points to the global/system Python executable
                        Arguments = $"-m venv {envPath}",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string err = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    Console.WriteLine($"Virtual environment created at: {envPath}");
                    return true;
                }
                else
                {
                    Console.WriteLine($"Failed to create virtual environment: {err}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception occurred: {ex.Message}");
                return false;
            }
        }
        public bool CreateVirtualEnvironment(string envPath)
        {
            if (Directory.Exists(envPath))
            {
                Console.WriteLine("Virtual environment already exists.");
                return false;
            }

            try
            {
                // Ensure the directory exists
                Directory.CreateDirectory(envPath);

                using (Py.GIL())  // Acquire the Python Global Interpreter Lock
                {
                    // Import the required Python module
                    dynamic venv = Py.Import("venv");

                    // Create the virtual environment
                    venv.create(envPath, with_pip: true);
                }

                Console.WriteLine($"Virtual environment created at: {envPath}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to create virtual environment: {ex.Message}");
                return false;
            }
        }
        public bool Initialize(string virtualEnvPath = null)
        {
        
            if (IsBusy) return false;
            IsBusy = true;
            // Use the provided virtual environment path or fall back to a default
            string pythonBinPath = virtualEnvPath ?? CurrentRuntimeConfig.BinPath;
            string pythonScriptPath = Path.Combine(pythonBinPath, "Scripts"); // Common for Windows virtual environments
            string pythonPackagePath = Path.Combine(pythonBinPath, "Lib\\site-packages");
            if (CurrentRuntimeConfig.IsPythonInstalled)
            {
                if (!PythonEngine.IsInitialized)
                {

                    PythonRunTimeDiagnostics.SetAiFolderPath(DMEditor);
                    Environment.SetEnvironmentVariable("PATH", $"{pythonBinPath};{pythonScriptPath};" + Environment.GetEnvironmentVariable("PATH"), EnvironmentVariableTarget.Process);//CurrentRuntimeConfig.ScriptPath
                    Environment.SetEnvironmentVariable("PYTHONNET_RUNTIME", $"{pythonBinPath}", EnvironmentVariableTarget.Process);
                    Environment.SetEnvironmentVariable("PYTHONNET_PYTHON_RUNTIME", $"{pythonBinPath}", EnvironmentVariableTarget.Process);
                    Environment.SetEnvironmentVariable("PYTHONHOME", pythonBinPath , EnvironmentVariableTarget.Process);//CurrentRuntimeConfig.BinPath
                    Environment.SetEnvironmentVariable("PYTHONPATH", $"{pythonPackagePath};", EnvironmentVariableTarget.Process); //CurrentRuntimeConfig.Packageinstallpath
                    try
                    {
                        PassedArgs args = new PassedArgs();
                        ReportProgress("Init. of Python engine");


                        //  Runtime.PythonRuntimePath= CurrentRuntimeConfig.BinPath;
                        Runtime.PythonDLL = Path.Combine(pythonBinPath,Path.GetFileName(CurrentRuntimeConfig.PythonDll));
                        PythonEngine.PythonHome = pythonBinPath;//CurrentRuntimeConfig.BinPath;
                        //       PythonEngine.PythonPath = CurrentRuntimeConfig.Packageinstallpath;
                        PythonEngine.Initialize();

                        //PackageManager = new PackageManagerViewModel(this);
                        //PackageManager.Editor = DMEditor;
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
            pythonpath = DMEditor.GetPythonDataPath();
            configfile = Path.Combine(pythonpath, "cpython.config");
            IsConfigLoaded = false;
            if (IsBusy) return false;
            // IsBusy = true;
            try
            {
                PythonRunTime cfg = new PythonRunTime();
                if (PythonRunTimeDiagnostics.IsPythonInstalled(pythonhome))
                {
                    
                    int idx = PythonConfig.Runtimes.FindIndex(p => p.BinPath.Equals(pythonhome, StringComparison.InvariantCultureIgnoreCase));
                    if (idx == -1)
                    {
                        if (File.Exists(configfile) && !IsConfigLoaded)
                        {
                            PythonConfig = JsonLoader.DeserializeSingleObject<PythonConfiguration>(configfile);
                            IsConfigLoaded = true;
                            cfg = PythonConfig.Runtimes[PythonConfig.RunTimeIndex];
                        }
                        else
                        {
                           
                            cfg = PythonRunTimeDiagnostics.GetPythonConfig(pythonhome);

                            PythonConfig.Runtimes.Add(cfg);
                           
                        }
                        
                    }
                    idx = PythonConfig.Runtimes.IndexOf(cfg);
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
            string configfile = Path.Combine(pythonpath, "cpython.config");
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
        public virtual bool RunPythonScript(string script, dynamic parameters)
        {
            bool retval = false;
            if (!IsInitialized)
            {
                return retval;
            }
           
            try
            {

               
                    if (parameters != null)
                    {
                        PersistentScope.Set(nameof(parameters), parameters);
                    }
                    PersistentScope.Exec(script);
                    retval = true;
             
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing Python script: {ex.Message}");
                return false;
            }


            return retval;
        }
        public async Task<dynamic> RunPythonScriptWithResult(string script, dynamic parameters)
        {
            dynamic result = null;
         
            if (!IsInitialized)
            {
                return result;
            }
            result= await RunPythonCodeAndGetOutput(Progress, script);
           // using (Py.GIL()) // Acquire the Python Global Interpreter Lock
           //{
           //     result = PersistentScope.Exec(script); // Execute the script in the persistent scope
           // }

            return result;
        }
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
        private volatile bool _shouldStop = false;

        public void Stop()
        {
            _shouldStop = true;
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
            string wrappedPythonCode = @"
import sys
import io

class CustomStringIO(io.StringIO):
    def __init__(self, output_handler, should_stop):
        super().__init__()
        self.output_handler = output_handler
        self.should_stop = should_stop

    def write(self, s):
        super().write(s)
        output = self.getvalue()
        if output.strip():
            self.output_handler(output.strip())
            self.truncate(0)  # Clear the internal buffer
            self.seek(0)  # Reset the buffer pointer

def capture_output(code, globals_dict, output_handler, should_stop):
    original_stdout = sys.stdout
    sys.stdout = CustomStringIO(output_handler, should_stop)

    try:
        exec(code, dict(globals_dict))
        if should_stop():
            raise KeyboardInterrupt
    finally:
        sys.stdout = original_stdout
";
            bool isImage = false;
            string output = "";
            try
            {
                //  using (Py.GIL())
                //   {
                //using (PyModule scope = Py.CreateScope())
                //{


                Action<string> OutputHandler = line =>
                {
                    progress.Report(new PassedArgs() { Messege = line });
                    output += line + "\n";
                };
                Func<bool> ShouldStop = () => _shouldStop;
                PersistentScope.Set("output_handler", OutputHandler);
                PersistentScope.Set("should_stop", ShouldStop);
                PersistentScope.Exec(wrappedPythonCode);
                PyObject captureOutputFunc = PersistentScope.GetAttr("capture_output");
                Dictionary<string, object> globalsDict = new Dictionary<string, object>();
                using (PyObject pyCode = new PyString(code))
                using (PyObject pyGlobalsDict = globalsDict.ToPython())
                using (PyObject pyOutputHandler = PersistentScope.Get("output_handler"))
                using (PyObject pyShouldStop = PersistentScope.Get("should_stop"))
                {
                    captureOutputFunc.Invoke(pyCode, pyGlobalsDict, pyOutputHandler, pyShouldStop);
                }
                //      }
                //   }
            }
            catch (PythonException ex)
            {
                // Handle Python exceptions
                progress.Report(new PassedArgs() { Messege = $"Python error: {ex.Message}" });
                output += $"Python error: {ex.Message}\n";
            }
            catch (Exception ex)
            {
                // Handle general exceptions
                progress.Report(new PassedArgs() { Messege = $"Error: {ex.Message}" });
                output += $"Error: {ex.Message}\n";
            }

            progress.Report(new PassedArgs() { Messege = $"Finished", EventType="CODEFINISH" });
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
        #region "Package Manager"
        public async Task<string> RunPackageManagerAsync(IProgress<PassedArgs> progress, string packageName, PackageAction packageAction, bool useConda = false)
        {
            string customPath = $"{ CurrentRuntimeConfig.BinPath.Trim()};{ CurrentRuntimeConfig.ScriptPath.Trim()}".Trim();
            string modifiedFilePath = customPath.Replace("\\", "\\\\");
            string output = "";
            string command = "";
            string wrappedPythonCode = $@"
import os
import subprocess
import threading
import queue

def set_custom_path(custom_path):
    # Modify the PATH environment variable
    os.environ[""PATH""] = '{modifiedFilePath}' + os.pathsep + os.environ[""PATH""]

def run_pip_and_capture_output(args, output_callback):
    def enqueue_output(stream, queue):
        for line in iter(stream.readline, b''):
            queue.put(line.decode('utf-8').strip())
        stream.close()

    process = subprocess.Popen(args, stdout=subprocess.PIPE, stderr=subprocess.PIPE)

    stdout_queue = queue.Queue()
    stderr_queue = queue.Queue()

    stdout_thread = threading.Thread(target=enqueue_output, args=(process.stdout, stdout_queue))
    stderr_thread = threading.Thread(target=enqueue_output, args=(process.stderr, stderr_queue))

    stdout_thread.start()
    stderr_thread.start()

    while process.poll() is None or not stdout_queue.empty() or not stderr_queue.empty():
        while not stdout_queue.empty():
            line = stdout_queue.get_nowait()
            output_callback(line)

        while not stderr_queue.empty():
            line = stderr_queue.get_nowait()
            output_callback(line)

    stdout_thread.join()
    stderr_thread.join()
    process.communicate()

def run_with_timeout(func, args, output_callback, timeout):
    try:
        func(args, output_callback)
    except Exception as e:
        output_callback(str(e))
";

            using (Py.GIL())
            {
                using (PyModule scope = Py.CreateScope())
                {
                    PyObject globalsDict = scope.GetAttr("__dict__");


                    scope.Exec(wrappedPythonCode);
                    // Set the custom_path from C# and call set_custom_path function in Python

                    PyObject setCustomPathFunc = scope.GetAttr("set_custom_path");
                    setCustomPathFunc.Invoke(modifiedFilePath.ToPython());

                    PyObject captureOutputFunc = scope.GetAttr("run_pip_and_capture_output");



                    if (useConda)
                    {
                        switch (packageAction)
                        {
                            case PackageAction.Install:
                                command = $"conda install -c conda-forge {packageName}";
                                break;
                            case PackageAction.Remove:
                                command = $"conda remove {packageName}";
                                break;
                            case PackageAction.Update:
                                command = $"conda update {packageName}";
                                break;
                            case PackageAction.UpgradePackager:
                                command = $"conda update conda";
                                break;

                            case PackageAction.InstallPackager:
                                command = $"conda {packageName}";
                                break;
                            default:
                                break;
                        }
                    }
                    else
                    {
                        switch (packageAction)
                        {
                            case PackageAction.Install:
                                command = $"pip install -U {packageName}";
                                break;
                            case PackageAction.Remove:
                                command = $"pip uninstall  {packageName}";
                                break;
                            case PackageAction.Update:
                                command = $"pip install --upgrade {packageName}";
                                break;
                            case PackageAction.UpgradePackager:
                                command = $"python.exe -m pip install --upgrade pip";
                                break;
                            case PackageAction.InstallPackager:
                                command = $"python.exe {packageName}";
                                break;
                            default:
                                break;
                        }

                    }
                    progress.Report(new PassedArgs() { Messege = $"Running {command}" });
                    //runTimeManager.OutputLines.Add($"Running {command}");
                    PyObject pyArgs = new PyList();

                    pyArgs.InvokeMethod("extend", command.Split(' ').ToPython());


                    // Set the output_callback function in Python
                    Channel<string> outputChannel = Channel.CreateUnbounded<string>();
                    PyObject outputCallback = PyObject.FromManagedObject((Action<string>)(s => {
                        outputChannel.Writer.TryWrite(s);
                    }));
                    globalsDict.SetItem("output_callback", outputCallback);

                    // Run the Python code with a timeout
                    int timeoutInSeconds = 120; // Adjust this value as needed
                    PyObject runWithTimeoutFunc = scope.GetAttr("run_with_timeout");
                    Task pythonTask = Task.Run(() => runWithTimeoutFunc.Invoke(captureOutputFunc, pyArgs, outputCallback, timeoutInSeconds.ToPython()));

                    var outputList = new List<string>();
                    // Create an async method to read from the channel
                    async Task ReadFromChannelAsync()
                    {
                        while (await outputChannel.Reader.WaitToReadAsync())
                        {
                            if (outputChannel.Reader.TryRead(out var line))
                            {
                                outputList.Add(line);
                                progress.Report(new PassedArgs() { Messege = line });
                                Console.WriteLine(line);
                            }
                        }

                    }

                    // Process the output lines asynchronously
                    Task readOutputTask = ReadFromChannelAsync();

                    // Wait for the Python task to complete and close the channel writer
                    await pythonTask;
                    outputChannel.Writer.Complete();

                    // Wait for the readOutputTask to complete
                    await readOutputTask;


                    output = string.Join("\n", outputList);
                }
            }
            if (output.Length > 0)
            {
                progress.Report(new PassedArgs() { Messege = $"Finished {command}" });
            }
            else
                progress.Report(new PassedArgs() { Messege = $"Finished {command} eith error" });
            return output;
        }
        public bool listpackagesAsync(IProgress<PassedArgs> _progress, CancellationToken token, bool useConda = false, string packagename = null)
        {
            if (IsBusy) return false;
            IsBusy = true;
            int i = 0;
            if (_progress != null)
            {
                Progress = _progress;
            }
            try
            {
                bool checkall = true;
                if (!string.IsNullOrEmpty(packagename))
                {
                    checkall = false;
                }

                //           runTimeManager._pythonRuntimeManager.CurrentRuntimeConfig.Packagelist = new List<PackageDefinition>();
                using (var gil =  GIL())
                {

                    dynamic pkgResources = Py.Import("importlib.metadata");
                    dynamic workingSet = pkgResources.distributions();
                    int count =  CurrentRuntimeConfig.Packagelist.Count;
                    int j = 1;
                    foreach (dynamic pkg in workingSet)
                    {
                        i++;
                        string packageName = pkg.metadata["Name"];
                        string packageVersion = pkg.version.ToString();
                        string line = $"Checking Package {packageName}: {packageVersion}";
                        Console.WriteLine(line);
                        // runTimeManager.OutputLines.Add(line);
                        Progress.Report(new PassedArgs() { Messege = line, ParameterInt1 = j, ParameterInt2 = count });
                        bool IsInternetAvailabe = PythonRunTimeDiagnostics.CheckNet();
                        PackageDefinition onlinepk = new PackageDefinition();
                        if (!string.IsNullOrEmpty(packageVersion))
                        {
                            if (checkall)
                            {
                                if (IsInternetAvailabe)
                                {
                                    onlinepk = PythonRunTimeDiagnostics.CheckIfPackageExistsAsync(packageName).Result;
                                }


                                PackageDefinition package =  CurrentRuntimeConfig.Packagelist.Where(p => p.packagename != null && p.packagename.Equals(packageName, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                                if (package != null)
                                {
                                    int idx =  CurrentRuntimeConfig.Packagelist.IndexOf(package);
                                    if (onlinepk != null)
                                    {
                                        package.updateversion = onlinepk.version;
                                    }
                                    package.installed = true;
                                    package.buttondisplay = "Installed";

                                    if (onlinepk != null)
                                    {
                                         CurrentRuntimeConfig.Packagelist[idx].updateversion = onlinepk.updateversion;
                                        line = $"Package {packageName}: {packageVersion} found with version {onlinepk.updateversion}";
                                    }
                                    else
                                    {
                                         CurrentRuntimeConfig.Packagelist[idx].updateversion = "Not Found";
                                        line = $"Package {packageName}: {"Not Found"}";
                                    }

                                    Console.WriteLine(line);
                                    // runTimeManager.OutputLines.Add(line);
                                    Progress.Report(new PassedArgs() { Messege = line });
                                }
                                else
                                {
                                    PackageDefinition packagelist = new PackageDefinition();
                                    packagelist.packagename = packageName;
                                    packagelist.version = packageVersion;
                                    packagelist.updateversion = packageVersion;
                                    packagelist.installed = true;
                                    packagelist.buttondisplay = "Added";
                                     CurrentRuntimeConfig.Packagelist.Add(packagelist);
                                    line = $"Added new Package {packagelist}: {packagelist.version}";
                                    Console.WriteLine(line);
                                    //  runTimeManager.OutputLines.Add(line);
                                    Progress.Report(new PassedArgs() { Messege = line });
                                }

                            }
                            else
                            {
                                if ( CurrentRuntimeConfig.Packagelist.Any(p => p.packagename != null && p.packagename.Equals(packageName, StringComparison.InvariantCultureIgnoreCase)))
                                {
                                    PackageDefinition package =  CurrentRuntimeConfig.Packagelist.Where(p => p.packagename != null && p.packagename.Equals(packageName, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                                    int idx =  CurrentRuntimeConfig.Packagelist.IndexOf(package);
                                    package.version = packageVersion;
                                    package.updateversion = packageVersion;
                                    package.installed = true;
                                    package.buttondisplay = "Installed";
                                    if (IsInternetAvailabe)
                                    {
                                        onlinepk = PythonRunTimeDiagnostics.CheckIfPackageExistsAsync(packageName).Result;
                                    }
                                    if (onlinepk != null)
                                    {
                                         CurrentRuntimeConfig.Packagelist[idx].updateversion = onlinepk.updateversion;
                                        package.updateversion = onlinepk.version;
                                        line = $"Package {packageName}: {packageVersion} found with version {onlinepk.updateversion}";
                                    }
                                    else
                                    {
                                         CurrentRuntimeConfig.Packagelist[idx].updateversion = "Not Found";
                                        line = $"Package {packageName}: {"Not Found"}";
                                    }

                                    Console.WriteLine(line);
                                    // runTimeManager.OutputLines.Add(line);
                                    Progress.Report(new PassedArgs() { Messege = line });
                                }
                            }


                        }
                        else Console.WriteLine($" empty {packageName}: {packageVersion}");

                        j++;
                    }


                }
                if (i == 0)
                {
                    Progress.Report(new PassedArgs() { Messege = "No Packages Found" });
                     CurrentRuntimeConfig.Packagelist = new List<PackageDefinition>();
                }
                IsBusy = false;
            }
            catch (Exception ex)
            {
                IsBusy = false;
                Console.WriteLine("Error: in Listing Packages");
            }
            IsBusy = false;
            return IsBusy;
        }
        public bool listpackages( bool useConda = false, string packagename = null)
        {
            if (IsBusy) return false;
            IsBusy = true;
            int i = 0;
            
            try
            {
                bool checkall = true;
                if (!string.IsNullOrEmpty(packagename))
                {
                    checkall = false;
                }
                string script = @"
import importlib.metadata
result = [{'name': pkg.metadata['Name'], 'version': pkg.version} for pkg in importlib.metadata.distributions()]
result";

                // Execute the script and get the result
                dynamic packages = RunPythonScriptWithResult(script, null).Result;

                if (packages != null)
                {
                    int j = 1;
                    int count = packages.Count;
                    foreach (var pkg in packages)
                    {
                        string packageName = pkg.name;
                        string packageVersion = pkg.version;
                        string line = $"Checking Package {packageName}: {packageVersion}";
                        Console.WriteLine(line);
                       // _progress?.Report(new PassedArgs() { Messege = line });

                        Console.WriteLine(line);
                        // runTimeManager.OutputLines.Add(line);
                        Progress.Report(new PassedArgs() { Messege = line, ParameterInt1 = j, ParameterInt2 = count });
                        bool IsInternetAvailabe = PythonRunTimeDiagnostics.CheckNet();
                        PackageDefinition onlinepk = new PackageDefinition();
                        if (!string.IsNullOrEmpty(packageVersion))
                        {
                            if (checkall)
                            {
                                if (IsInternetAvailabe)
                                {
                                    onlinepk = PythonRunTimeDiagnostics.CheckIfPackageExistsAsync(packageName).Result;
                                }


                                PackageDefinition package =  CurrentRuntimeConfig.Packagelist.Where(p => p.packagename != null && p.packagename.Equals(packageName, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                                if (package != null)
                                {
                                    int idx =  CurrentRuntimeConfig.Packagelist.IndexOf(package);
                                    if (onlinepk != null)
                                    {
                                        package.updateversion = onlinepk.version;
                                    }
                                    package.installed = true;
                                    package.buttondisplay = "Installed";

                                    if (onlinepk != null)
                                    {
                                         CurrentRuntimeConfig.Packagelist[idx].updateversion = onlinepk.updateversion;
                                        line = $"Package {packageName}: {packageVersion} found with version {onlinepk.updateversion}";
                                    }
                                    else
                                    {
                                         CurrentRuntimeConfig.Packagelist[idx].updateversion = "Not Found";
                                        line = $"Package {packageName}: {"Not Found"}";
                                    }

                                    Console.WriteLine(line);
                                    // runTimeManager.OutputLines.Add(line);
                                    Progress.Report(new PassedArgs() { Messege = line });
                                }
                                else
                                {
                                    PackageDefinition packagelist = new PackageDefinition();
                                    packagelist.packagename = packageName;
                                    packagelist.version = packageVersion;
                                    packagelist.updateversion = packageVersion;
                                    packagelist.installed = true;
                                    packagelist.buttondisplay = "Added";
                                     CurrentRuntimeConfig.Packagelist.Add(packagelist);
                                    line = $"Added new Package {packagelist}: {packagelist.version}";
                                    Console.WriteLine(line);
                                    //  runTimeManager.OutputLines.Add(line);
                                    Progress.Report(new PassedArgs() { Messege = line });
                                }

                            }
                            else
                            {
                                if ( CurrentRuntimeConfig.Packagelist.Any(p => p.packagename != null && p.packagename.Equals(packageName, StringComparison.InvariantCultureIgnoreCase)))
                                {
                                    PackageDefinition package =  CurrentRuntimeConfig.Packagelist.Where(p => p.packagename != null && p.packagename.Equals(packageName, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                                    int idx =  CurrentRuntimeConfig.Packagelist.IndexOf(package);
                                    package.version = packageVersion;
                                    package.updateversion = packageVersion;
                                    package.installed = true;
                                    package.buttondisplay = "Installed";
                                    if (IsInternetAvailabe)
                                    {
                                        onlinepk = PythonRunTimeDiagnostics.CheckIfPackageExistsAsync(packageName).Result;
                                    }
                                    if (onlinepk != null)
                                    {
                                         CurrentRuntimeConfig.Packagelist[idx].updateversion = onlinepk.updateversion;
                                        package.updateversion = onlinepk.version;
                                        line = $"Package {packageName}: {packageVersion} found with version {onlinepk.updateversion}";
                                    }
                                    else
                                    {
                                         CurrentRuntimeConfig.Packagelist[idx].updateversion = "Not Found";
                                        line = $"Package {packageName}: {"Not Found"}";
                                    }

                                    Console.WriteLine(line);
                                    // runTimeManager.OutputLines.Add(line);
                                    Progress.Report(new PassedArgs() { Messege = line });
                                }
                            }


                        }
                        else Console.WriteLine($" empty {packageName}: {packageVersion}");

                        // Add logic to update  CurrentRuntimeConfig.Packagelist or other necessary operations
                    }
                }
                //           runTimeManager._pythonRuntimeManager.CurrentRuntimeConfig.Packagelist = new List<PackageDefinition>();
                using (var gil =  GIL())
                {

                    dynamic pkgResources = Py.Import("importlib.metadata");
                    dynamic workingSet = pkgResources.distributions();
                    int count =  CurrentRuntimeConfig.Packagelist.Count;
                    int j = 1;
                    foreach (dynamic pkg in workingSet)
                    {
                        i++;

                        j++;
                    }


                }
                if (i == 0)
                {
                    Progress.Report(new PassedArgs() { Messege = "No Packages Found" });
                     CurrentRuntimeConfig.Packagelist = new List<PackageDefinition>();
                }
                IsBusy = false;
            }
            catch (Exception ex)
            {
                IsBusy = false;
                Console.WriteLine("Error: in Listing Packages");
            }
            IsBusy = false;
            return IsBusy;
        }
        public bool InstallPIP(IProgress<PassedArgs> progress, CancellationToken token)
        {

            bool pipInstall = true;
            if (IsBusy) return false;
            IsBusy = true;
            try
            {// Execute Python code and capture its output
             // Download the pip installer script
                string url = "https://bootstrap.pypa.io/get-pip.py";
                string scriptPath = Path.Combine(Path.GetTempPath(), "get-pip.py");
                WebClient client = new WebClient();
                client.DownloadFile(url, scriptPath);

                // Install pip
                //using (Py.GIL())
                //{
                //    using (PyModule scope = Py.CreateScope())
                //    {
                //        string code = File.ReadAllText(scriptPath);
                //       //unPythonCodeAndGetOutput(runTimeManager,progress,code);
                //    }


                //}
                RunPackageManagerAsync(progress, scriptPath, PackageAction.InstallPackager, PythonRunTimeDiagnostics.GetPackageType( CurrentRuntimeConfig.BinPath) == PackageType.conda);
                IsBusy = false;
                // Delete the installer script
                File.Delete(scriptPath);

            }
            catch (Exception ex)
            {
                IsBusy = false;
                pipInstall = false;
               DMEditor.AddLogMessage("Beep AI Python", ex.Message, DateTime.Now, 0, null, Errors.Failed);
            }
            IsBusy = false;
            return pipInstall;
        }
        public async Task<PackageDefinition> FindPackageUpdate(string packageName, IProgress<PassedArgs> progress, CancellationToken token)
        {
            bool IsInternetAvailabe = PythonRunTimeDiagnostics.CheckNet();
            PackageDefinition retval = null;
           DMEditor.ErrorObject.Flag = Errors.Ok;
            if (IsInternetAvailabe)
            {
                retval = await CheckIfPackageExistsAsync(packageName);
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

                        listpackagesAsync(progress, token);



                        installedpackage =  CurrentRuntimeConfig.Packagelist.Where(p => p.packagename.Equals(packageName, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                        idx =  CurrentRuntimeConfig.Packagelist.IndexOf(installedpackage);
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
        public bool IsPackageInstalled(string packageName, IProgress<PassedArgs> progress, CancellationToken token)
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
        public bool InstallPackage(string packageName, IProgress<PassedArgs> progress, CancellationToken token)
        {
           DMEditor.ErrorObject.Flag = Errors.Ok;



            try
            {
                if (IsBusy) { return false; }
                IsBusy = true;
                RunPackageManagerAsync(progress, packageName, PackageAction.Install, PythonRunTimeDiagnostics.GetPackageType( CurrentRuntimeConfig.BinPath) == PackageType.conda);


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
        public bool RemovePackage(string packageName, IProgress<PassedArgs> progress, CancellationToken token)
        {
           DMEditor.ErrorObject.Flag = Errors.Ok;

            bool isInstalled = false;

            try
            {
                if (IsBusy) { return false; }
                RunPackageManagerAsync(progress, packageName, PackageAction.Remove, false);
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
        public bool UpdatePackage(string packageName, IProgress<PassedArgs> progress, CancellationToken token)
        {
           DMEditor.ErrorObject.Flag = Errors.Ok;

            bool isInstalled = false;
            try
            {
                if (packageName.Equals("pip", StringComparison.CurrentCultureIgnoreCase))
                {
                    RunPackageManagerAsync(progress, packageName, PackageAction.UpgradePackager, PythonRunTimeDiagnostics.GetPackageType( CurrentRuntimeConfig.BinPath) == PackageType.conda);
                }
                else RunPackageManagerAsync(progress, packageName, PackageAction.Update, PythonRunTimeDiagnostics.GetPackageType( CurrentRuntimeConfig.BinPath) == PackageType.conda);


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
        public bool RefreshInstalledPackagesList(IProgress<PassedArgs> progress, CancellationToken token)
        {

            try
            {
                var retval = listpackagesAsync(progress,token );
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
        public bool RefreshInstalledPackage(string packagename, IProgress<PassedArgs> progress, CancellationToken token)
        {

            try
            {
                var retval = listpackagesAsync(progress, token);
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
        public async Task<PackageDefinition> CheckIfPackageExistsAsync(string packageName)
        {
            using HttpClient httpClient = new HttpClient();
            HttpResponseMessage response;

            using CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)); // set timeout to 30 seconds
            try
            {
                response = await httpClient.GetAsync($"https://pypi.org/pypi/{packageName}/json", cts.Token).ConfigureAwait(false);
            }
            catch (HttpRequestException)
            {
                // Network error, API not available, etc.
                Console.WriteLine("An error occurred while checking the package. Please try again later.");
                return null;
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine($"The request to '{packageName}' timed out.");
                return null;
            }

            // If the response status code is OK (200), the package exists
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                try
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    dynamic packageData = Newtonsoft.Json.JsonConvert.DeserializeObject(jsonResponse);
                    string latestVersion = packageData.info.version;
                    string description = packageData.info.description;

                    PackageDefinition packageInfo = new PackageDefinition
                    {
                        packagename = packageName,
                        version = latestVersion,
                        description = description
                    };

                    return packageInfo;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error while parsing package data for '{packageName}': {ex.Message}");
                    return null;
                }
            }
            else
            {
                Console.WriteLine($"The package '{packageName}' does not exist on PyPI.");
                return null;
            }
        }
        #endregion "Package Manager"
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
