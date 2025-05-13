using System;
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
using Beep.Python.RuntimeEngine.Helpers;
using Newtonsoft.Json;

namespace Beep.Python.RuntimeEngine
{
    /// <summary>
    /// Manages a Python .NET runtime environment, including initialization, configuration,
    /// and script execution.
    /// </summary>
    public class PythonNetRunTimeManager : IDisposable, IPythonRunTimeManager
    {
        #region "Fields"

        private bool _IsInitialized = false;
        private bool disposedValue;
        private readonly IBeepService _beepService;
        /// <summary>
        /// Gets the current <see cref="IProgress{PassedArgs}"/> object for reporting progress.
        /// </summary>
        public IProgress<PassedArgs> Progress { get; private set; }

        /// <summary>
        /// Gets or sets the token used for cancellation operations.
        /// </summary>
        public CancellationToken Token { get; set; }

        private string pythonpath;
        private string configfile;
        private volatile bool _shouldStop = false;
        #endregion "Fields"
        #region "Properties"
        #region "Session and Environment"

    //    public PythonSessionInfo CurrentSession { get; private set; }
        public ObservableBindingList<PythonSessionInfo> Sessions { get; set; } = new();
    //    public PythonVirtualEnvironment CurrentVirtualEnvironment { get; set; }
     
        public ObservableBindingList<PythonVirtualEnvironment> ManagedVirtualEnvironments { get;  set; } = new();
        // Dictionary to hold all active scopes, keyed by session or environment ID
        public  Dictionary<string, PyModule> SessionScopes { get; } = new();

        #endregion "Session and Environment"
        #region "Status and Configuration"
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
        #endregion "Status and Configuration"
        #endregion "Properties"
        #region "Constructors"
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


        #endregion "Constructors"

        #region "Scope Management"
        /// <summary>
        /// Gets or sets the persistent Python scope (module) that remains loaded across script executions.
        /// </summary>
      //  public PyModule CurrentPersistentScope { get; set; }

        /// <summary>
        /// Creates a new persistent Python scope if one doesn't already exist.
        /// </summary>
        /// <returns>
        /// True if a new scope was created; false if a scope already exists.
        /// </returns>
        public bool CreateScope(PythonSessionInfo session)
        {

            if (session.VirtualEnvironmentId!=null)
            {
                var venv = ManagedVirtualEnvironments.FirstOrDefault(e => e.ID == session.VirtualEnvironmentId);
                if (venv == null)
                {
                    ReportProgress("No virtual environment found for the given session information.", Errors.Failed);
                    return false;
                }
                return CreateScope(session, venv);
            }
            return false;
        }
        public PyModule GetScope(PythonSessionInfo session)
        {
            if (session == null || !SessionScopes.ContainsKey(session.SessionId))
                return null;

            return SessionScopes[session.SessionId];
        }

        public bool HasScope(PythonSessionInfo session)
        {
            return session != null && SessionScopes.ContainsKey(session.SessionId);
        }

        public bool CreateScope(PythonSessionInfo session, PythonVirtualEnvironment venv)
        {
            if (session == null || venv == null)
                return false;

            if (SessionScopes.ContainsKey(session.SessionId))
                return false;

            using (Py.GIL()) // Always acquire GIL when creating or using scopes
            {
                var scope = Py.CreateScope();
                SessionScopes[session.SessionId] = scope;

                // Inject session and environment metadata into the Python scope
                string setContext = $@"
import os
import sys

os.environ['VIRTUAL_ENV'] = r'{venv.Path}'
sys.prefix = r'{venv.Path}'
sys.exec_prefix = r'{venv.Path}'

username = '{session.Username ?? "unknown"}'
session_id = '{session.SessionId}'
venv_name = '{venv.Name}'
";
                scope.Exec(setContext);
            }

            return true;
        }


        public void ClearScope(string sessionId)
        {
            if (SessionScopes.TryGetValue(sessionId, out var scope))
            {
                scope.Dispose();
                SessionScopes.Remove(sessionId);
            }
        }

        public void ClearAll()
        {
            foreach (var scope in SessionScopes.Values)
            {
                scope.Dispose();
            }
            SessionScopes.Clear();
        }
        #endregion "Scope Management"
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

            var env = new PythonVirtualEnvironment
            {
                Name = username,
                Path = userEnvPath,
                PythonVersion = CurrentRuntimeConfig?.PythonVersion ?? "Unknown",
                BaseInterpreterPath = CurrentRuntimeConfig?.BinPath
            };

            if (!Directory.Exists(userEnvPath))
            {
                bool creationSuccess = CreateVirtualEnvironmentFromDefinition(env);
                if (!creationSuccess)
                {
                    return false;
                }
            }
            else
            {
                // If directory exists, make sure it's tracked
                if (!ManagedVirtualEnvironments.Any(e => e.Path == userEnvPath))
                {
                    ManagedVirtualEnvironments.Add(env);
                }
            }

            return Initialize(userEnvPath);
        }

        public bool InitializeForUser(PythonSessionInfo sessionInfo)
        {
            if (sessionInfo == null || string.IsNullOrWhiteSpace(sessionInfo.Username))
            {
                ReportProgress("Invalid session information provided.", Errors.Failed);
                return false;
            }
            if (sessionInfo.VirtualEnvironmentId == null)
            {
                ReportProgress("No virtual environment ID found in session information.", Errors.Failed);
                return false;
            }
            // Get Virtual enviroment form ManagedVirtualEnvironments
            var env = ManagedVirtualEnvironments.FirstOrDefault(e => e.ID == sessionInfo.VirtualEnvironmentId);
            if (env == null) {
                ReportProgress("No virtual environment found for the given session information.", Errors.Failed);
                return false;
            }
            return InitializeForUser(env.Path, sessionInfo.Username);
        }

        /// <summary>
        /// Creates a virtual environment using the current runtime config and the provided virtual environment definition.
        /// </summary>
        /// <param name="env">The virtual environment metadata (name, path, etc.).</param>
        /// <returns>True if successful; otherwise false.</returns>
        public bool CreateVirtualEnvironmentFromDefinition(PythonVirtualEnvironment env)
        {
            if (env == null || string.IsNullOrWhiteSpace(env.Path))
            {
                ReportProgress("Invalid environment definition.", Errors.Failed);
                return false;
            }

            if (CurrentRuntimeConfig == null || string.IsNullOrWhiteSpace(CurrentRuntimeConfig.BinPath))
            {
                ReportProgress("No active Python runtime is set.", Errors.Failed);
                return false;
            }

            if (Directory.Exists(env.Path))
            {
                ReportProgress($"Virtual environment already exists at {env.Path}", Errors.Ok);
                return true;
            }

            try
            {
                string pythonExe = Path.Combine(CurrentRuntimeConfig.BinPath, "python.exe");

                var process = new Process()
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = pythonExe,
                        Arguments = $"-m venv \"{env.Path}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    ReportProgress($"Virtual environment created at: {env.Path}", Errors.Ok);

                    // Register environment with metadata
                    env.BaseInterpreterPath = CurrentRuntimeConfig.BinPath;
                    env.PythonVersion = CurrentRuntimeConfig.PythonVersion;

                    ManagedVirtualEnvironments.Add(env);
                    return true;
                }
                else
                {
                    ReportProgress($"Error creating virtual environment: {error}", Errors.Failed);
                    return false;
                }
            }
            catch (Exception ex)
            {
                ReportProgress($"Exception: {ex.Message}", Errors.Failed);
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
        /// Initializes the Python environment using the provided PythonVirtualEnvironment definition.
        /// </summary>
        /// <param name="venv">The virtual environment to initialize.</param>
        /// <returns>True if initialized successfully; otherwise false.</returns>
        /// <summary>
        /// Initializes the Python environment using the provided PythonVirtualEnvironment definition.
        /// </summary>
        /// <param name="venv">The virtual environment to initialize.</param>
        /// <returns>True if initialized successfully; otherwise false.</returns>
        public bool Initialize(PythonVirtualEnvironment venv)
        {
            if (IsBusy) return false;
            IsBusy = true;

            if (venv == null || string.IsNullOrWhiteSpace(venv.Path))
            {
                ReportProgress("Invalid virtual environment provided.", Errors.Failed);
                IsBusy = false;
                return false;
            }

            // Create a new session if none exists
        

            string pythonBinPath = venv.Path;
            string pythonExe = Path.Combine(pythonBinPath, "Scripts", "python.exe");
            if (!File.Exists(pythonExe))
            {
                ReportProgress($"python.exe not found in virtual environment at {pythonExe}", Errors.Failed);
                IsBusy = false;
                return false;
            }

            string pythonScriptPath = Path.Combine(pythonBinPath, "Scripts");
            string pythonPackagePath = Path.Combine(pythonBinPath, "Lib", "site-packages");

            try
            {
                if (!PythonEngine.IsInitialized)
                {
                    PythonRunTimeDiagnostics.SetAiFolderPath(DMEditor);

                    Environment.SetEnvironmentVariable("PATH", $"{pythonBinPath};{pythonScriptPath};" + Environment.GetEnvironmentVariable("PATH"), EnvironmentVariableTarget.Process);
                    Environment.SetEnvironmentVariable("PYTHONNET_RUNTIME", pythonBinPath, EnvironmentVariableTarget.Process);
                    Environment.SetEnvironmentVariable("PYTHONNET_PYTHON_RUNTIME", pythonBinPath, EnvironmentVariableTarget.Process);
                    Environment.SetEnvironmentVariable("PYTHONHOME", pythonBinPath, EnvironmentVariableTarget.Process);
                    Environment.SetEnvironmentVariable("PYTHONPATH", $"{pythonPackagePath};", EnvironmentVariableTarget.Process);

                    ReportProgress("Initializing Python engine...");

                    // Auto-detect DLL if needed
                    string pythonDll = null;
                    if (!string.IsNullOrWhiteSpace(venv.PythonVersion))
                    {
                        var versionDigits = venv.PythonVersion.Replace(".", "");
                        var expectedDll = Path.Combine(pythonBinPath, $"python{versionDigits}.dll");
                        if (File.Exists(expectedDll))
                        {
                            pythonDll = expectedDll;
                        }
                    }

                    if (pythonDll == null)
                    {
                        var dllCandidates = Directory.GetFiles(pythonBinPath, "python*.dll");
                        pythonDll = dllCandidates.OrderByDescending(f => f).FirstOrDefault();
                    }

                    if (string.IsNullOrEmpty(pythonDll) || !File.Exists(pythonDll))
                    {
                        ReportProgress("Python DLL not found in the virtual environment.", Errors.Failed);
                        return false;
                    }

                    Runtime.PythonDLL = pythonDll;
                    PythonEngine.PythonHome = pythonBinPath;
                    PythonEngine.Initialize();

                    ReportProgress("Python engine initialized.");
                    _IsInitialized = true;
               

                    return true;
                }
                else
                {
                    //CurrentSession.WasSuccessful = true;
                    //CurrentSession.EndedAt = DateTime.Now;

                    ReportProgress("Python engine already initialized.", Errors.Ok);
                    return true;
                }
            }
            catch (Exception ex)
            {
                //CurrentSession.WasSuccessful = false;
                //CurrentSession.Notes = ex.Message;
                //CurrentSession.EndedAt = DateTime.Now;

                ReportProgress($"Initialization error: {ex.Message}", Errors.Failed);
                return false;
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Runs Python code with 

        /// <summary>
        /// Shuts down the Python engine, disposing of any persistent scope and clearing active environment state.
        /// </summary>
        /// <returns>An <see cref="IErrorsInfo"/> indicating success or failure.</returns>
        public IErrorsInfo ShutDown()
        {
            var er = new ErrorsInfo { Flag = Errors.Ok };
            if (IsBusy) return er;
            IsBusy = true;

            try
            {
                foreach (var scope in SessionScopes.Values)
                {
                    scope.Dispose();
                }
                SessionScopes.Clear();

                if (PythonEngine.IsInitialized)
                {
                    PythonEngine.Shutdown();
                }

                _IsInitialized = false;

                //// ✅ Finalize session tracking
                //if (CurrentSession != null)
                //{
                //    CurrentSession.EndedAt = DateTime.Now;
                //    CurrentSession.Notes = "Session ended via shutdown.";
                //}

                ReportProgress("Python engine shut down.", Errors.Ok);
            }
            catch (Exception ex)
            {
                er.Ex = ex;
                er.Flag = Errors.Failed;
                er.Message = ex.Message;
                ReportProgress($"Shutdown error: {ex.Message}", Errors.Failed);
            }
            finally
            {
                IsBusy = false;
        
            }

            return er;
        }

        /// <summary>
        /// Shuts down the current Python engine and reinitializes it with a new virtual environment.
        /// </summary>
        /// <param name="venv">The new virtual environment to initialize.</param>
        /// <returns>True if successful; otherwise false.</returns>
        public bool RestartWithEnvironment(PythonVirtualEnvironment venv)
        {
            var result = ShutDown();
            if (result.Flag != Errors.Ok)
            {
                ReportProgress("Failed to shut down current Python instance.", Errors.Failed);
                return false;
            }

         
            return Initialize(venv);
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
        public async Task<string> RunPythonForUserAsync(PythonSessionInfo session, string username, string code, IProgress<PassedArgs> progress = null)
        {
            if (session == null)
            {
                ReportProgress("Session object is required.", Errors.Failed);
                return null;
            }

            // 🔍 Try to find an existing venv by session.VirtualEnvironmentId
            var venv = ManagedVirtualEnvironments.FirstOrDefault(v =>
                v.ID.Equals(session.VirtualEnvironmentId, StringComparison.OrdinalIgnoreCase));

            // 🔁 Fallback: Try to find by username if not linked by ID
            if (venv == null)
            {
                venv = ManagedVirtualEnvironments.FirstOrDefault(v =>
                    v.Name.Equals(username, StringComparison.OrdinalIgnoreCase));
            }

            if (venv == null)
            {
                ReportProgress($"No virtual environment found for user '{username}' or session.", Errors.Failed);
                session.Notes = "Virtual environment not found.";
                session.WasSuccessful = false;
                session.EndedAt = DateTime.Now;
                return null;
            }

            // ✅ Track session info
            session.StartedAt = DateTime.Now;
            session.Username = username;
            session.VirtualEnvironmentId = venv.ID;

            // Check if the session already exists in the virtual environment's Sessions collection
            if (!venv.Sessions.Any(s => s.SessionId == session.SessionId))
            {
                venv.AddSession(session);
            }

            // Check if the session already exists in the global Sessions collection
            if (!Sessions.Any(s => s.SessionId == session.SessionId))
            {
                Sessions.Add(session);
            }

            // Set current environment/session
      

            // 🔧 Initialize engine
            if (!Initialize(venv))
            {
                session.Notes = "Environment initialization failed.";
                session.WasSuccessful = false;
                session.EndedAt = DateTime.Now;
                return null;
            }

            try
            {
                string output = await RunPythonCodeAndGetOutput(progress ?? DMEditor.progress, code,session);
                session.Notes = "Execution succeeded.";
                session.WasSuccessful = true;
                session.EndedAt = DateTime.Now;
                return output;
            }
            catch (Exception ex)
            {
                session.Notes = $"Execution failed: {ex.Message}";
                session.WasSuccessful = false;
                session.EndedAt = DateTime.Now;
                ReportProgress(session.Notes, Errors.Failed);
                return null;
            }
        }

        /// <summary>
        /// Runs a command line in Python (e.g., pip install) by spawning a subprocess and capturing its output.
        /// </summary>
        /// <param name="progress">A progress reporter to relay output messages.</param>
        /// <param name="commandString">The command arguments to pass (e.g., "install requests").</param>
        /// <param name="useConda">If true, conda is used instead of pip.</param>
        /// <param name="session">The session to use for execution. If null, uses the current session.</param>
        /// <param name="environment">The environment to use. If null, determined from session or current environment.</param>
        /// <returns>The collected output as a single string.</returns>
        public async Task<string> RunPythonCommandLineAsync(
            IProgress<PassedArgs> progress,
            string commandString,
            bool useConda = false,
            PythonSessionInfo session = null,
            PythonVirtualEnvironment environment = null)
        {
       
            // Use provided environment or find from session
            if (environment == null && session != null)
            {
                environment = ManagedVirtualEnvironments.FirstOrDefault(v =>
                    v.ID.Equals(session?.VirtualEnvironmentId, StringComparison.OrdinalIgnoreCase));
            }

          
            if (environment == null)
            {
                ReportProgress("No virtual environment specified or available for command execution.", Errors.Failed);
                return null;
            }

            // Ensure the session is properly tracked
            if (session != null)
            {
                if (!Sessions.Any(s => s.SessionId == session.SessionId))
                {
                    Sessions.Add(session);
                }

                if (!environment.Sessions.Any(s => s.SessionId == session.SessionId))
                {
                    environment.AddSession(session);
                }

            }

            // Get the proper environment paths
            string customPath = environment.Path;
            string scriptPath = Path.Combine(environment.Path, "Scripts");
            string modifiedFilePath = $"{customPath};{scriptPath}".Replace("\\", "\\\\");

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
                // Determine the scope to use
                PyModule scope = null;
                if (session != null)
                {
                    scope = GetScope(session);
                    if (scope == null && environment != null)
                    {
                        CreateScope(session, environment);
                        scope = GetScope(session);
                    }
                }

                // Fall back to a new temporary scope if needed
                if (scope == null)
                {
                    scope = Py.CreateScope();
                }

                PyObject globalsDict = scope.GetAttr("__dict__");

                scope.Exec(wrappedPythonCode);

                // Set the custom_path from C# and call set_custom_path function in Python
                PyObject setCustomPathFunc = scope.GetAttr("set_custom_path");
                setCustomPathFunc.Invoke(modifiedFilePath.ToPython());

                PyObject captureOutputFunc = scope.GetAttr("run_pip_and_capture_output");

                if (useConda)
                {
                    command = $" {commandString}"; // conda usage example
                }
                else
                {
                    command = $" {commandString}"; // pip / python.exe usage example
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

            if (output.Length > 0)
            {
                progress.Report(new PassedArgs() { Messege = $"Finished {command}" });
            }
            else
            {
                progress.Report(new PassedArgs() { Messege = $"Finished {command} with error" });
            }

            // Update session status
            if (session != null)
            {
                session.Notes = $"Executed command: {commandString}";
                // Don't end the session here, just update the notes
            }

            return output;
        }

        /// <summary>
        /// Runs a Python script within the persistent scope with optional parameters.
        /// </summary>
        /// <param name="script">Python script code as a string.</param>
        /// <param name="parameters">Optional parameters (dynamic) to be made available in the script's scope.</param>
        /// <returns>True if the script ran successfully, otherwise false.</returns>
        public virtual bool RunPythonScript(string script, dynamic parameters,PythonSessionInfo session)
        {
            bool retval = false;
            if (!IsInitialized)
            {
                return retval;
            }

            try
            {

                SessionScopes[session.SessionId].Set(nameof(parameters), parameters);
                SessionScopes[session.SessionId].Exec(script);
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
        public async Task<dynamic> RunPythonScriptWithResult(string script, dynamic parameters,PythonSessionInfo session)
        {
            dynamic result = null;
            if (!IsInitialized)
            {
                return result;
            }
            result = await RunPythonCodeAndGetOutput(Progress, script,session);
            return result;
        }

        /// <summary>
        /// Runs a Python file asynchronously, given a file path, and reports progress to a <see cref="IProgress{PassedArgs}"/>.
        /// </summary>
        /// <param name="session">The session context to execute within.</param>
        /// <param name="file">Path to the Python file.</param>
        /// <param name="progress">Progress reporter for logging and feedback.</param>
        /// <param name="token">Cancellation token to stop execution (if supported).</param>
        /// <returns>An <see cref="IErrorsInfo"/> indicating success or failure.</returns>
        public async Task<IErrorsInfo> RunFile(PythonSessionInfo session, string file, IProgress<PassedArgs> progress, CancellationToken token)
        {
            DMEditor.ErrorObject.Flag = Errors.Ok;
            if (IsBusy) return DMEditor.ErrorObject;
            IsBusy = true;

            try
            {
                // Use the provided session or fall back to current session if null
                if (session != null)
                {
                    // Find the environment associated with this session
                    var venv = ManagedVirtualEnvironments.FirstOrDefault(v =>
                        v.ID.Equals(session.VirtualEnvironmentId, StringComparison.OrdinalIgnoreCase));

                    if (venv != null)
                    {
                        // Check if the session should be tracked in collections
                        if (!venv.Sessions.Any(s => s.SessionId == session.SessionId))
                        {
                            venv.AddSession(session);
                        }

                        if (!Sessions.Any(s => s.SessionId == session.SessionId))
                        {
                            Sessions.Add(session);
                        }

                      

                        // Ensure environment is initialized
                        if (!Initialize(venv))
                        {
                            ReportProgress("Failed to initialize environment for file execution.", Errors.Failed);
                            IsBusy = false;
                            return DMEditor.ErrorObject;
                        }
                        string code = $"{venv.BaseInterpreterPath} {file}";
                        await RunPythonCodeAndGetOutput(progress, code, session);
                    }
                }

               
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
        /// <param name="session">The session context to execute within.</param>
        /// <param name="code">Python code as a string.</param>
        /// <param name="progress">Progress reporter for logging and feedback.</param>
        /// <param name="token">Cancellation token to stop execution (if supported).</param>
        /// <returns>An <see cref="IErrorsInfo"/> indicating success or failure.</returns>
        public async Task<IErrorsInfo> RunCode(PythonSessionInfo session, string code, IProgress<PassedArgs> progress, CancellationToken token)
        {
            DMEditor.ErrorObject.Flag = Errors.Ok;
            if (IsBusy) return DMEditor.ErrorObject;
            IsBusy = true;

            try
            {
                // Use the provided session or fall back to current session if null
                if (session != null)
                {
                    // Find the environment associated with this session
                    var venv = ManagedVirtualEnvironments.FirstOrDefault(v =>
                        v.ID.Equals(session.VirtualEnvironmentId, StringComparison.OrdinalIgnoreCase));

                    if (venv != null)
                    {
                        // Check if the session should be tracked in collections
                        if (!venv.Sessions.Any(s => s.SessionId == session.SessionId))
                        {
                            venv.AddSession(session);
                        }

                        if (!Sessions.Any(s => s.SessionId == session.SessionId))
                        {
                            Sessions.Add(session);
                        }

                       

                        // Ensure environment is initialized
                        if (!Initialize(venv))
                        {
                            ReportProgress("Failed to initialize environment for code execution.", Errors.Failed);
                            IsBusy = false;
                            return DMEditor.ErrorObject;
                        }
                    }
                }

                // Execute code with session-specific scope if available
                await RunPythonCodeAndGetOutput(progress, code, session);
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
        /// Runs a Python command (string) asynchronously, with progress reporting and cancellation.
        /// </summary>
        /// <param name="session">The session context to execute within.</param>
        /// <param name="command">The command string to execute.</param>
        /// <param name="progress">A progress reporter for message feedback.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>A dynamic object containing the result of the execution, if any.</returns>
        public async Task<dynamic> RunCommand(PythonSessionInfo session, string command, IProgress<PassedArgs> progress, CancellationToken token)
        {
            PyObject pyObject = null;
            DMEditor.ErrorObject.Flag = Errors.Ok;
            if (IsBusy) { return false; }
            IsBusy = true;

            try
            {
                // Use the provided session or fall back to current session if null
                if (session != null)
                {
                    // Find the environment associated with this session
                    var venv = ManagedVirtualEnvironments.FirstOrDefault(v =>
                        v.ID.Equals(session.VirtualEnvironmentId, StringComparison.OrdinalIgnoreCase));

                    if (venv != null)
                    {
                        // Check if the session should be tracked in collections
                        if (!venv.Sessions.Any(s => s.SessionId == session.SessionId))
                        {
                            venv.AddSession(session);
                        }

                        if (!Sessions.Any(s => s.SessionId == session.SessionId))
                        {
                            Sessions.Add(session);
                        }

                    

                        // Ensure environment is initialized
                        if (!Initialize(venv))
                        {
                            ReportProgress("Failed to initialize environment for command execution.", Errors.Failed);
                            IsBusy = false;
                            return null;
                        }
                    }
                }

                // Execute command with session-specific scope if available
                await RunPythonCodeAndGetOutput(progress, command, session);
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
        /// <param name="session">The session in which to execute the code. If null, uses the persistent scope.</param>
        /// <returns>The collected output as a single string.</returns>
        public async Task<string> RunPythonCodeAndGetOutput(IProgress<PassedArgs> progress, string code, PythonSessionInfo session = null)
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
            string output = "";
            PyModule scope = null;

            try
            {
                // Determine which scope to use based on the session
                if (session != null)
                {
                    // Try to get the session's scope if it exists
                    scope = GetScope(session);

                    // If no scope exists for the session, create one if we have an environment
                    if (scope == null)
                    {
                        var venv = ManagedVirtualEnvironments.FirstOrDefault(v =>
                            v.ID.Equals(session.VirtualEnvironmentId, StringComparison.OrdinalIgnoreCase));

                        if (venv != null)
                        {
                            CreateScope(session, venv);
                            scope = GetScope(session);
                        }
                    }
                }

            
                Action<string> OutputHandler = line =>
                {
                    progress.Report(new PassedArgs() { Messege = line });
                    output += line + "\n";
                };
                Func<bool> ShouldStop = () => _shouldStop;

                scope.Set("output_handler", OutputHandler);
                scope.Set("should_stop", ShouldStop);
                scope.Exec(wrappedPythonCode);

                PyObject captureOutputFunc = scope.GetAttr("capture_output");
                Dictionary<string, object> globalsDict = new Dictionary<string, object>();

                using (PyObject pyCode = new PyString(code))
                using (PyObject pyGlobalsDict = globalsDict.ToPython())
                using (PyObject pyOutputHandler = scope.Get("output_handler"))
                using (PyObject pyShouldStop = scope.Get("should_stop"))
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
        /// <summary>
        /// Signals the runtime manager to attempt stopping any ongoing Python code execution.
        /// </summary>
        public void Stop()
        {
            _shouldStop = true;
        }

        public void SaveEnvironments(string filePath)
        {
            var json = JsonConvert.SerializeObject(ManagedVirtualEnvironments, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }

        public void LoadEnvironments(string filePath)
        {
            if (File.Exists(filePath))
            {
                var json = File.ReadAllText(filePath);
                var list = JsonConvert.DeserializeObject<ObservableBindingList<PythonVirtualEnvironment>>(json);
                ManagedVirtualEnvironments = list ?? new ObservableBindingList<PythonVirtualEnvironment>();
            }
        }

        #region "IDisposable Implementation"
        /// <summary>
        /// Updates the session management to properly dispose of Python scopes when sessions end or are removed.
        /// </summary>
        public void CleanupSession(PythonSessionInfo session)
        {
            if (session == null)
                return;

            // Clean up the session's scope if it exists
            if (HasScope(session))
            {
                ClearScope(session.SessionId);
            }

          
        }

        /// <summary>
        /// Overload of ShutDown that allows specifying which session to shut down.
        /// </summary>
        public IErrorsInfo ShutDownSession(PythonSessionInfo session)
        {
            var er = new ErrorsInfo { Flag = Errors.Ok };
            if (session == null)
                return er;

            try
            {
                // Mark session as ended
                session.Status = PythonSessionStatus.Terminated;
                session.EndedAt = DateTime.Now;
                session.Notes = "Session explicitly terminated";

                // Clean up the session's scope
                CleanupSession(session);

               
            }
            catch (Exception ex)
            {
                er.Ex = ex;
                er.Flag = Errors.Failed;
                er.Message = ex.Message;
                ReportProgress($"Session shutdown error: {ex.Message}", Errors.Failed);
            }

            return er;
        }
        /// <summary>
        /// Performs cleanup of stale sessions and their associated scopes.
        /// This can be called periodically to prevent memory leaks.
        /// </summary>
        public void PerformSessionCleanup(TimeSpan maxAge)
        {
            if (IsBusy)
                return;

            var now = DateTime.Now;
            var sessionsToCleanup = Sessions
                .Where(s => s.Status == PythonSessionStatus.Terminated ||
                          (s.EndedAt.HasValue && (now - s.EndedAt.Value) > maxAge) ||
                          (!s.EndedAt.HasValue && (now - s.StartedAt) > maxAge))
                .ToList();

            foreach (var session in sessionsToCleanup)
            {
                ReportProgress($"Cleaning up stale session: {session.SessionId}", Errors.Ok);
                CleanupSession(session);

                // Remove from environment's sessions collection
                if (!string.IsNullOrEmpty(session.VirtualEnvironmentId))
                {
                    var env = ManagedVirtualEnvironments.FirstOrDefault(v =>
                        v.ID.Equals(session.VirtualEnvironmentId, StringComparison.OrdinalIgnoreCase));

                    if (env != null)
                    {
                        var sessionInEnv = env.Sessions.FirstOrDefault(s => s.SessionId == session.SessionId);
                        if (sessionInEnv != null)
                        {
                            env.Sessions.Remove(sessionInEnv);
                        }
                    }
                }

                // Remove from global sessions collection
                Sessions.Remove(session);
            }
        }

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
                    foreach (var session in Sessions.ToList())
                    {
                        CleanupSession(session);
                    }

                   
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
