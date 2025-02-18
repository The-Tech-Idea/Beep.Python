﻿using System;
using System.Collections.Generic;
using Python.Runtime;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.ObjectModel;
using Beep.Python.Model;
using TheTechIdea.Beep.ConfigUtil;
using System.Linq;
using System.Threading.Channels;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Editor;
using System.Diagnostics;
using TheTechIdea.Beep.Container.Services;
using Beep.Python.RuntimeEngine.Services;

namespace Beep.Python.RuntimeEngine
{
    /// <summary>
    /// Manages a Python .NET runtime environment, including initialization, configuration,
    /// and script execution.
    /// </summary>
    public class PythonNetRunTimeManager : IDisposable, IPythonRunTimeManager
    {
        /// <summary>
        /// Initializes a new instance of <see cref="PythonNetRunTimeManager"/> with a specified <see cref="IBeepService"/>.
        /// </summary>
        /// <param name="beepService">Service used for logging, configuration, and editor access.</param>
        public PythonNetRunTimeManager(IBeepService beepService)
        {
            _beepService = beepService;
            DMEditor = beepService.DMEEditor;
            JsonLoader = DMEditor.ConfigEditor.JsonLoader;

            PythonRunTimeDiagnostics.SetFolderNames("x32", "x64");
        }

        private bool _IsInitialized = false;
        private bool disposedValue;
        private readonly IBeepService _beepService;

        private string pythonpath;
        private string configfile;
        private volatile bool _shouldStop = false;

        /// <summary>
        /// Gets the current <see cref="IProgress{PassedArgs}"/> object for reporting progress.
        /// </summary>
        public IProgress<PassedArgs> Progress { get; private set; }

        /// <summary>
        /// Gets or sets the token used for cancellation operations.
        /// </summary>
        public CancellationToken Token { get; set; }

        /// <summary>
        /// Provides a GIL (Global Interpreter Lock) context. Throws an exception if Python is not initialized.
        /// </summary>
        /// <returns>A <see cref="Py.GILState"/> object that manages Python GIL acquisition and release.</returns>
        public Py.GILState GIL()
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("Python runtime is not initialized.");
            }
            return Py.GIL();
        }

        /// <summary>
        /// Gets the current Python runtime configuration from <see cref="PythonConfig"/> using <see cref="PythonConfiguration.RunTimeIndex"/>.
        /// </summary>
        public PythonRunTime CurrentRuntimeConfig
        {
            get
            {
                if (PythonConfig.RunTimeIndex >= 0)
                {
                    return PythonConfig.Runtimes[PythonConfig.RunTimeIndex];
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Gets or sets the persistent Python scope (module) that remains loaded across script executions.
        /// </summary>
        public PyModule PersistentScope { get; set; }

        /// <summary>
        /// Creates a new persistent Python scope if one doesn't already exist.
        /// </summary>
        /// <returns>
        /// True if a new scope was created; false if a scope already exists.
        /// </returns>
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

        /// <summary>
        /// Indicates whether Python is fully initialized and ready to execute code.
        /// </summary>
        public bool IsInitialized => GetIsPythonReady();

        /// <summary>
        /// Indicates whether a Python compiler/interpreter is available on the system.
        /// </summary>
        public bool IsCompilerAvailable => GetIsPythonAvailable();

        /// <summary>
        /// Gets or sets an observable collection of output lines captured from Python execution.
        /// </summary>
        public ObservableCollection<string> OutputLines { get; set; } = new ObservableCollection<string>();

        /// <summary>
        /// Indicates whether the runtime manager is currently performing an operation.
        /// </summary>
        public bool IsBusy { get; set; } = false;

        /// <summary>
        /// The Python configuration object, containing a list of runtimes and the currently selected one.
        /// </summary>
        public PythonConfiguration PythonConfig { get; set; } = new PythonConfiguration();

        /// <summary>
        /// Indicates whether the Python configuration is loaded.
        /// </summary>
        public bool IsConfigLoaded
        {
            get { return GetIsConfigLoaded(); }
            set { }
        }

        /// <summary>
        /// Indicates whether a Python script is currently running.
        /// </summary>
        public bool IsRunning { get; set; }

        /// <summary>
        /// Gets or sets the file path of the currently loaded Python script.
        /// </summary>
        public string CurrentFileLoaded { get; set; }

        /// <summary>
        /// Indicates whether the Python path has changed.
        /// </summary>
        public bool IsPathChanged { get; set; } = false;

        /// <summary>
        /// Stores a new path if the Python path has changed.
        /// </summary>
        public string NewPath { get; set; } = null;

        /// <summary>
        /// Gets or sets the global <see cref="IDMEEditor"/> used for logging, messages, etc.
        /// </summary>
        public IDMEEditor DMEditor { get; set; }

        /// <summary>
        /// Gets or sets the JSON loader used for reading/writing configuration files.
        /// </summary>
        public IJsonLoader JsonLoader { get; set; }

        /// <summary>
        /// Gets or sets the binary type (32 or 64-bit).
        /// </summary>
        public BinType32or64 BinType { get; set; } = BinType32or64.p395x32;

        #region "Initialization and Shutdown"

        /// <summary>
        /// Initializes Python for a specific user by creating or loading a virtual environment.
        /// </summary>
        /// <param name="envBasePath">Base path where user environments are stored.</param>
        /// <param name="username">The username for which to create or load a virtual environment.</param>
        /// <returns>True if initialization succeeded; otherwise false.</returns>
        public bool InitializeForUser(string envBasePath, string username)
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

            // Initialize using the newly created or existing env
            return Initialize(userEnvPath);
        }

        /// <summary>
        /// Creates a virtual environment by invoking a Python command line ("python -m venv &lt;envPath&gt;").
        /// </summary>
        /// <param name="envPath">Path where the new virtual environment will be created.</param>
        /// <returns>True if successful; otherwise false.</returns>
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

        /// <summary>
        /// Creates a virtual environment using Python's built-in "venv" module (via Python.NET).
        /// </summary>
        /// <param name="envPath">Path where the new virtual environment will be created.</param>
        /// <returns>True if successful; otherwise false.</returns>
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

                // Acquire the Python Global Interpreter Lock
                using (Py.GIL())
                {
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

        /// <summary>
        /// Initializes the Python environment using the specified virtual environment path or the current runtime config if none is provided.
        /// </summary>
        /// <param name="virtualEnvPath">Path to a Python virtual environment (optional).</param>
        /// <returns>True if initialized successfully; otherwise false.</returns>
        public bool Initialize(string virtualEnvPath = null)
        {
            if (IsBusy) return false;
            IsBusy = true;

            // Use the provided virtual environment path or fall back to the config's BinPath
            string pythonBinPath = virtualEnvPath ?? CurrentRuntimeConfig?.BinPath;
            if (pythonBinPath == null)
            {
                ReportProgress("No Python runtime path found in configuration.", Errors.Failed);
                IsBusy = false; // FIX: Ensure IsBusy is reset
                return false;
            }

            string pythonScriptPath = Path.Combine(pythonBinPath, "Scripts"); // Common for Windows venv
            string pythonPackagePath = Path.Combine(pythonBinPath, "Lib\\site-Packages");

            if (CurrentRuntimeConfig != null && CurrentRuntimeConfig.IsPythonInstalled)
            {
                if (!PythonEngine.IsInitialized)
                {
                    PythonRunTimeDiagnostics.SetAiFolderPath(DMEditor);

                    Environment.SetEnvironmentVariable("PATH", $"{pythonBinPath};{pythonScriptPath};" + Environment.GetEnvironmentVariable("PATH"), EnvironmentVariableTarget.Process);
                    Environment.SetEnvironmentVariable("PYTHONNET_RUNTIME", $"{pythonBinPath}", EnvironmentVariableTarget.Process);
                    Environment.SetEnvironmentVariable("PYTHONNET_PYTHON_RUNTIME", $"{pythonBinPath}", EnvironmentVariableTarget.Process);
                    Environment.SetEnvironmentVariable("PYTHONHOME", pythonBinPath, EnvironmentVariableTarget.Process);
                    Environment.SetEnvironmentVariable("PYTHONPATH", $"{pythonPackagePath};", EnvironmentVariableTarget.Process);

                    try
                    {
                        PassedArgs args = new PassedArgs();
                        ReportProgress("Init. of Python engine");

                        //  Runtime.PythonRuntimePath= CurrentRuntimeConfig.BinPath;
                        Runtime.PythonDLL = Path.Combine(pythonBinPath, Path.GetFileName(CurrentRuntimeConfig.PythonDll));
                        PythonEngine.PythonHome = pythonBinPath;
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
                    // If code runs here, you'd need a return statement.
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

            // FIX: Add a default return false to handle any unhandled paths.
            IsBusy = false;
            return false;
        }

        /// <summary>
        /// Shuts down the Python engine, disposing of any persistent scope if it exists.
        /// </summary>
        /// <returns>An <see cref="IErrorsInfo"/> indicating success or failure.</returns>
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

        #endregion

        #region "Configuration Methods"

        /// <summary>
        /// Determines if Python is ready by checking if the compiler is available and the engine is initialized.
        /// </summary>
        /// <returns>True if Python is initialized and available; otherwise false.</returns>
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

        /// <summary>
        /// Determines if a Python runtime is available based on <see cref="PythonConfig"/>.
        /// </summary>
        /// <returns>True if a runtime is configured and Status; otherwise false.</returns>
        private bool GetIsPythonAvailable()
        {
            if (PythonConfig != null)
            {
                if (PythonConfig.RunTimeIndex > -1)
                {
                    if (!string.IsNullOrEmpty(CurrentRuntimeConfig?.BinPath))
                    {
                        return PythonRunTimeDiagnostics.IsPythonInstalled(CurrentRuntimeConfig.BinPath);
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

        /// <summary>
        /// Checks if the configuration is loaded by verifying <see cref="PythonConfig"/> and its runtimes.
        /// </summary>
        /// <returns>True if config is loaded; otherwise false.</returns>
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

        /// <summary>
        /// Adds specified paths to the current process' PATH environment variable.
        /// </summary>
        /// <param name="paths">Array of paths to add to PATH.</param>
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

        /// <summary>
        /// Initializes Python with a specified home directory and bin type, optionally specifying a site-Packages folder.
        /// </summary>
        /// <param name="pythonhome">Path to the Python home directory or virtual environment.</param>
        /// <param name="binType">Binary type (32 or 64-bit).</param>
        /// <param name="libpath">Relative path to the site-Packages folder (default "lib\\site-Packages").</param>
        /// <returns>True if initialization is successful, otherwise false.</returns>
        public bool Initialize(string pythonhome, BinType32or64 binType, string libpath = @"lib\site-Packages")
        {
            pythonpath = DMEditor.GetPythonDataPath();
            configfile = Path.Combine(pythonpath, "cpython.config");
            IsConfigLoaded = false;
            if (IsBusy) return false;
            // IsBusy = true; (Not set here, presumably to allow repeated calls)

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
                    return false;
                }
            }
            catch (Exception ex)
            {
                DMEditor.AddLogMessage("Beep AI Python", $"{ex.Message} ", DateTime.Now, 0, null, Errors.Failed);
            }
            return false;
        }

        /// <summary>
        /// Picks a Python configuration based on the specified path, adds it to <see cref="PythonConfig.Runtimes"/>, and initializes it.
        /// </summary>
        /// <param name="path">Path to the Python installation or environment.</param>
        /// <returns>True if picked successfully and initialized, otherwise false.</returns>
        public bool PickConfig(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                if (PythonRunTimeDiagnostics.IsPythonInstalled(path))
                {
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

        /// <summary>
        /// Picks a Python runtime from the existing <see cref="PythonConfig.Runtimes"/> and initializes it.
        /// </summary>
        /// <param name="cfg">A <see cref="PythonRunTime"/> instance already in <see cref="PythonConfig.Runtimes"/>.</param>
        /// <returns>True if picked successfully, otherwise false.</returns>
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

        /// <summary>
        /// Sets the runtime path in the current runtime config and re-initializes Python.
        /// </summary>
        /// <param name="runtimepath">Path to the Python runtime.</param>
        /// <param name="binType">Binary type (32 or 64-bit).</param>
        /// <param name="libpath">Relative path to the site-Packages folder (default is "lib\\site-Packages").</param>
        public void SetRuntimePath(string runtimepath, BinType32or64 binType, string libpath = @"lib\site-Packages")
        {
            Initialize(CurrentRuntimeConfig.RuntimePath, binType);
            SaveConfig();
        }

        /// <summary>
        /// Creates or loads the Python config file from disk. If none exists, it creates a default.
        /// </summary>
        public void CreateLoadConfig()
        {
            if (File.Exists(configfile) && !IsConfigLoaded)
            {
                PythonConfig = JsonLoader.DeserializeSingleObject<PythonConfiguration>(configfile);
                IsConfigLoaded = true;
                if (PythonConfig.Runtimes.Count > 0)
                {
                    if (PythonConfig.RunTimeIndex > -1)
                    {
                        // IsCompilerAvailable= PythonRunTimeDiagnostics.IsPythonInstalled(CurrentRuntimeConfig.BinPath);
                    }
                }
            }
            else
            {
                if (PythonConfig.RunTimeIndex < 0)
                {
                    PythonRunTime config = new PythonRunTime
                    {
                        IsPythonInstalled = false,
                        RuntimePath = string.Empty,
                        Message = "No Python Runtime Found"
                    };
                    PythonConfig.Runtimes.Add(config);
                    PythonConfig.RunTimeIndex = -1;

                    JsonLoader.Serialize(configfile, PythonConfig);
                }
                IsConfigLoaded = true;
            }
        }

        /// <summary>
        /// Saves the current <see cref="PythonConfig"/> to disk.
        /// </summary>
        public void SaveConfig()
        {
            string configfile = Path.Combine(pythonpath, "cpython.config");
            if (PythonConfig == null)
            {
                PythonConfig = new PythonConfiguration();
            }
            if (JsonLoader == null)
            {
                JsonLoader = DMEditor.ConfigEditor.JsonLoader;
            }
            JsonLoader.Serialize(configfile, PythonConfig);
            IsConfigLoaded = true;
        }

        #endregion

        #region "Python Run Code"

        /// <summary>
        /// Runs a Python script within the persistent scope with optional parameters.
        /// </summary>
        /// <param name="script">Python script code as a string.</param>
        /// <param name="parameters">Optional parameters (dynamic) to be made available in the script's scope.</param>
        /// <returns>True if the script ran successfully, otherwise false.</returns>
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

        /// <summary>
        /// Runs a Python script with parameters and returns the result asynchronously.
        /// </summary>
        /// <param name="script">The Python script code to run.</param>
        /// <param name="parameters">Optional parameters to be passed to the script.</param>
        /// <returns>A dynamic object representing the result of the script execution.</returns>
        public async Task<dynamic> RunPythonScriptWithResult(string script, dynamic parameters)
        {
            dynamic result = null;
            if (!IsInitialized)
            {
                return result;
            }
            result = await RunPythonCodeAndGetOutput(Progress, script);
            return result;
        }

        /// <summary>
        /// Runs a Python file asynchronously, given a file path, and reports progress to a <see cref="IProgress{PassedArgs}"/>.
        /// </summary>
        /// <param name="file">Path to the Python file.</param>
        /// <param name="progress">Progress reporter for logging and feedback.</param>
        /// <param name="token">Cancellation token to stop execution (if supported).</param>
        /// <returns>An <see cref="IErrorsInfo"/> indicating success or failure.</returns>
        public async Task<IErrorsInfo> RunFile(string file, IProgress<PassedArgs> progress, CancellationToken token)
        {
            DMEditor.ErrorObject.Flag = Errors.Ok;
            if (IsBusy) return DMEditor.ErrorObject;
            IsBusy = true;

            try
            {
                string code = $"{PythonRunTimeDiagnostics.GetPythonExe(CurrentRuntimeConfig.BinPath)} {file}";
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

        /// <summary>
        /// Runs arbitrary Python code asynchronously, reporting progress and allowing cancellation.
        /// </summary>
        /// <param name="code">Python code as a string.</param>
        /// <param name="progress">Progress reporter for logging and feedback.</param>
        /// <param name="token">Cancellation token to stop execution (if supported).</param>
        /// <returns>An <see cref="IErrorsInfo"/> indicating success or failure.</returns>
        public async Task<IErrorsInfo> RunCode(string code, IProgress<PassedArgs> progress, CancellationToken token)
        {
            DMEditor.ErrorObject.Flag = Errors.Ok;
            if (IsBusy) return DMEditor.ErrorObject;
            IsBusy = true;

            try
            {
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

        /// <summary>
        /// Signals the runtime manager to attempt stopping any ongoing Python code execution.
        /// </summary>
        public void Stop()
        {
            _shouldStop = true;
        }

        /// <summary>
        /// Runs a Python command (string) asynchronously, with progress reporting and cancellation.
        /// </summary>
        /// <param name="command">The command string to execute.</param>
        /// <param name="progress">A progress reporter for message feedback.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>A dynamic object containing the result of the execution, if any.</returns>
        public async Task<dynamic> RunCommand(string command, IProgress<PassedArgs> progress, CancellationToken token)
        {
            PyObject pyObject = null;
            DMEditor.ErrorObject.Flag = Errors.Ok;
            if (IsBusy) { return false; }
            IsBusy = true;

            try
            {
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

        /// <summary>
        /// Runs Python code within a wrapper that captures stdout line-by-line and reports it to the provided <see cref="IProgress{PassedArgs}"/>.
        /// </summary>
        /// <param name="progress">Progress reporter for output lines.</param>
        /// <param name="code">Python code as a string.</param>
        /// <returns>The collected output as a single string.</returns>
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
            self.seek(0)      # Reset the buffer pointer

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

            progress.Report(new PassedArgs() { Messege = $"Finished", EventType = "CODEFINISH" });
            IsBusy = false;
            return output;
        }

        #endregion

        #region "Utility Methods"

        /// <summary>
        /// Reports progress messages using <see cref="Progress"/> if available; otherwise uses <see cref="DMEditor"/>.
        /// </summary>
        /// <param name="args">Arguments containing the message and other info.</param>
        private void ReportProgress(PassedArgs args)
        {
            if (Progress != null)
            {
                Progress.Report(args);
            }
        }

        /// <summary>
        /// Overload of <see cref="ReportProgress(PassedArgs)"/> that takes a string message and an error flag.
        /// </summary>
        /// <param name="messege">Message to report.</param>
        /// <param name="flag">Error level (<see cref="Errors"/>).</param>
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
                    PassedArgs args = new PassedArgs
                    {
                        Messege = messege
                    };
                    Progress.Report(args);
                }
                DMEditor.AddLogMessage("Beep AI Python", messege, DateTime.Now, 0, null, flag);
            }
        }

        /// <summary>
        /// Converts a C# dictionary to a Python dictionary object.
        /// </summary>
        /// <param name="dictionary">An <see cref="IDictionary{TKey, TValue}"/> to convert.</param>
        /// <returns>A <see cref="PyObject"/> representing the Python dictionary.</returns>
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

        /// <summary>
        /// Converts an arbitrary C# object to a <see cref="PyObject"/>.
        /// </summary>
        /// <param name="obj">The object to convert.</param>
        /// <returns>A <see cref="PyObject"/> wrapping the provided object.</returns>
        public static PyObject ToPython(object obj)
        {
            using (Py.GIL())
            {
                return PyObject.FromManagedObject(obj);
            }
        }

        /// <summary>
        /// Runs a command line in Python (e.g., pip install) by spawning a subprocess and capturing its output.
        /// </summary>
        /// <param name="progress">A progress reporter to relay output messages.</param>
        /// <param name="commandstring">The command arguments to pass (e.g., "install requests").</param>
        /// <param name="useConda">If true, conda is used instead of pip (not fully implemented here).</param>
        /// <returns>The collected output as a single string.</returns>
        public async Task<string> RunPythonCommandLineAsync(IProgress<PassedArgs> progress, string commandstring, bool useConda = false)
        {
            string customPath = $"{CurrentRuntimeConfig.BinPath.Trim()};{CurrentRuntimeConfig.ScriptPath.Trim()}".Trim();
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
                        command = $" {commandstring}"; // conda usage example
                    }
                    else
                    {
                        command = $" {commandstring}"; // pip / python.exe usage example
                    }

                    progress.Report(new PassedArgs() { Messege = $"Running {command}" });
                    PyObject pyArgs = new PyList();
                    pyArgs.InvokeMethod("extend", command.Split(' ').ToPython());

                    // Prepare an output channel
                    Channel<string> outputChannel = Channel.CreateUnbounded<string>();
                    PyObject outputCallback = PyObject.FromManagedObject((Action<string>)(s =>
                    {
                        outputChannel.Writer.TryWrite(s);
                    }));
                    globalsDict.SetItem("output_callback", outputCallback);

                    // Run the Python code with a timeout
                    int timeoutInSeconds = 120; // Adjust as needed
                    PyObject runWithTimeoutFunc = scope.GetAttr("run_with_timeout");
                    Task pythonTask = Task.Run(() => runWithTimeoutFunc.Invoke(captureOutputFunc, pyArgs, outputCallback, timeoutInSeconds.ToPython()));

                    var outputList = new List<string>();

                    // Asynchronous method to read from the channel
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

                    // Start reading output lines
                    Task readOutputTask = ReadFromChannelAsync();

                    // Wait for the Python task to complete
                    await pythonTask;
                    outputChannel.Writer.Complete();

                    // Wait for the readOutputTask to finish
                    await readOutputTask;

                    output = string.Join("\n", outputList);
                }
            }

            if (output.Length > 0)
            {
                progress.Report(new PassedArgs() { Messege = $"Finished {command}" });
            }
            else
            {
                progress.Report(new PassedArgs() { Messege = $"Finished {command} with error" });
            }
            return output;
        }

        #endregion

        #region "IDisposable Implementation"

        /// <summary>
        /// Protected virtual dispose method for freeing resources.
        /// </summary>
        /// <param name="disposing">Indicates whether the call is from Dispose (true) or finalizer (false).</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    ShutDown();
                    // Dispose managed objects here if needed.
                }
                // Free unmanaged objects (if any) and set large fields to null.

                disposedValue = true;
            }
        }

        /// <summary>
        /// Finalizer. Override only if Dispose(bool disposing) has code to free unmanaged resources.
        /// </summary>
        ~PythonNetRunTimeManager()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        /// <summary>
        /// Public dispose method. Calls the protected dispose pattern method and suppresses finalization.
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
