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
using Beep.Python.RuntimeEngine.Helpers;

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
        public IPythonCodeExecuteManager ExecuteManager { get; set; }
        public IPythonSessionManager SessionManager { get; set; }
        public IPythonVirtualEnvManager VirtualEnvmanager { get; set; }
         public Dictionary<string, PyModule> SessionScopes { get; } = new();
        #endregion "Session and Environment"

        #region "Status and Configuration"

        public PythonEngineMode EngineMode { get; set; } = PythonEngineMode.SingleUser;
        /// <summary>
        /// Indicates whether Python is fully initialized and ready to execute code.
        /// </summary>
        public bool IsInitialized => _IsInitialized;

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
        public ObservableBindingList<PythonRunTime> PythonInstallations { get; set; } = new();

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
        public string RequirementsFile { get; private set; }
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

            // Create a VirtualEnvManager instance instead of implementing the functionality directly
            VirtualEnvmanager = new PythonVirtualEnvManager(beepService, this);
        }

        /// <summary>
        /// Provides a GIL (Global Interpreter Lock) context. Throws an exception if Python is not initialized.
        /// </summary>
        /// <returns>A <see cref="Py.GILState"/> object that manages Python GIL acquisition and release.</returns>
        public Py.GILState GIL()
        {
            return Py.GIL();
        }
        #endregion "Constructors"

        #region "Scope Management"
        /// <summary>
        /// Creates a new scope for the specified session.
        /// </summary>
        public bool CreateScope(PythonSessionInfo session)
        {
            if (session.VirtualEnvironmentId != null)
            {
                var venv =VirtualEnvmanager.ManagedVirtualEnvironments.FirstOrDefault(e => e.ID == session.VirtualEnvironmentId);
                if (venv == null)
                {
                    ReportProgress("No virtual environment found for the given session information.", Errors.Failed);
                    return false;
                }
                return CreateScope(session, venv);
            }
            return false;
        }

        /// <summary>
        /// Gets the Python scope associated with a session.
        /// </summary>
        public PyModule GetScope(PythonSessionInfo session)
        {
            if (session == null || !SessionScopes.ContainsKey(session.SessionId))
                return null;

            return SessionScopes[session.SessionId];
        }

        /// <summary>
        /// Checks if a session has an associated Python scope.
        /// </summary>
        public bool HasScope(PythonSessionInfo session)
        {
            return session != null && SessionScopes.ContainsKey(session.SessionId);
        }

        /// <summary>
        /// Creates a new scope for a session within a specific virtual environment.
        /// </summary>
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

        /// <summary>
        /// Clears a session's scope.
        /// </summary>
        public void ClearScope(string sessionId)
        {
            if (SessionScopes.TryGetValue(sessionId, out var scope))
            {
                scope.Dispose();
                SessionScopes.Remove(sessionId);
            }
        }

        /// <summary>
        /// Clears all scopes.
        /// </summary>
        public void ClearAll()
        {
            foreach (var scope in SessionScopes.Values)
            {
                scope.Dispose();
            }
            SessionScopes.Clear();
        }
        #endregion "Scope Management"

        #region "Initialiaqzation and Shutdown"
        public PythonRunTime Initialize(string runtimepath)
        {
            if (string.IsNullOrEmpty(runtimepath))
            {
                ReportProgress("Cannot initialize with null or empty runtime path.", Errors.Failed);
                return null;
            }

            try
            {
                ReportProgress($"Checking Python installation at: {runtimepath}");

                // First check if it exists in PythonInstallations collection
                PythonRunTime runtime = PythonInstallations?.FirstOrDefault(p => 
                    p.BinPath == runtimepath || 
                    p.RuntimePath == runtimepath);
                
                if (runtime != null)
                {
                    ReportProgress($"Found existing Python configuration for: {runtimepath}");
                    return runtime;
                }

                // If not found in existing installations, run diagnostics
                var diagnostics = PythonEnvironmentDiagnostics.RunFullDiagnostics(runtimepath);
                if (diagnostics == null)
                {
                    ReportProgress($"Failed to run Python diagnostics on path: {runtimepath}", Errors.Failed);
                    return null;
                }
                
                // Check if the diagnostics found a valid Python installation
                if (!diagnostics.PythonFound)
                {
                    string errorMessage = diagnostics.Errors.Any() 
                        ? diagnostics.Errors.First() 
                        : "Python not found at the specified path";
                    
                    ReportProgress($"Invalid Python path: {errorMessage}", Errors.Failed);
                    return null;
                }

                // Create a new runtime configuration using the comprehensive method
                runtime = PythonRunTimeDiagnostics.GetPythonConfig(runtimepath);
                
                if (runtime == null)
                {
                    ReportProgress($"Failed to create Python runtime configuration for: {runtimepath}", Errors.Failed);
                    return null;
                }
                
                // Enhance the runtime with additional configuration
                EnhanceRuntimeConfiguration(runtime, diagnostics);

                // Add to known installations if not already there
                if (!PythonInstallations.Any(p => p.ID == runtime.ID))
                {
                    PythonInstallations.Add(runtime);
                    SaveConfig(); // Save the updated configuration
                }
                
                ReportProgress($"Successfully initialized Python runtime: {runtime.PythonVersion} at {runtimepath}", Errors.Ok);
                return runtime;
            }
            catch (Exception ex)
            {
                ReportProgress($"Error initializing Python runtime: {ex.Message}", Errors.Failed);
                return null;
            }
        }

        /// <summary>
        /// Enhances a Python runtime configuration with additional information from diagnostics.
        /// </summary>
        private void EnhanceRuntimeConfiguration(PythonRunTime runtime, PythonDiagnosticsReport diagnostics)
        {
            if (runtime == null || diagnostics == null)
                return;

            // Set package type (Conda or standard Python)
            runtime.PackageType = PythonRunTimeDiagnostics.GetPackageType(runtime.RuntimePath);
            
            // Format Python version nicely
            if (!string.IsNullOrEmpty(diagnostics.PythonVersion))
            {
                runtime.PythonVersion = diagnostics.PythonVersion.Replace("Python ", "").Trim();
            }
            
            // Check and set Conda path if applicable
            string condaExe = PythonRunTimeDiagnostics.IsCondaInstalled(runtime.RuntimePath);
            if (!string.IsNullOrEmpty(condaExe))
            {
                runtime.CondaPath = Path.Combine(runtime.RuntimePath, condaExe);
                runtime.Binary = PythonBinary.Conda;
                ReportProgress($"Conda installation detected: {runtime.CondaPath}");
            }
            else
            {
                runtime.Binary = PythonBinary.Python;
            }
            
            // Try to fix missing or invalid DLL paths
            if (!string.IsNullOrEmpty(runtime.PythonDll) && !File.Exists(runtime.PythonDll))
            {
                string[] candidates;
                
                if (runtime.Binary == PythonBinary.Conda)
                {
                    // Common Conda DLL locations
                    candidates = new[] {
                        Path.Combine(runtime.RuntimePath, Path.GetFileName(runtime.PythonDll)),
                        Path.Combine(runtime.RuntimePath, "Library", "bin", Path.GetFileName(runtime.PythonDll))
                    };
                }
                else
                {
                    // Standard Python DLL locations
                    candidates = new[] {
                        Path.Combine(runtime.RuntimePath, Path.GetFileName(runtime.PythonDll)),
                        Path.Combine(runtime.RuntimePath, "DLLs", Path.GetFileName(runtime.PythonDll))
                    };
                }
                
                string foundDll = candidates.FirstOrDefault(File.Exists);
                if (!string.IsNullOrEmpty(foundDll))
                {
                    runtime.PythonDll = foundDll;
                    ReportProgress($"Found Python DLL at: {foundDll}");
                }
            }
            
            // Set up a default AI folder path if needed
            if (string.IsNullOrEmpty(runtime.AiFolderpath))
            {
                string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                runtime.AiFolderpath = Path.Combine(documentsPath, "AI");
                
                // Create the directory if it doesn't exist
                if (!Directory.Exists(runtime.AiFolderpath))
                {
                    Directory.CreateDirectory(runtime.AiFolderpath);
                }
            }
            
            // Copy installed packages information from diagnostics
            if (diagnostics.InstalledPackages?.Any() == true)
            {
                if (runtime.Packagelist == null)
                {
                    runtime.Packagelist = new ObservableBindingList<PackageDefinition>();
                }
                
                foreach (string packageName in diagnostics.InstalledPackages)
                {
                    if (!runtime.Packagelist.Any(p => p.PackageName == packageName))
                    {
                        runtime.Packagelist.Add(new PackageDefinition
                        {
                            PackageName = packageName,
                            Status = PackageStatus.Installed
                        });
                    }
                }
                
                ReportProgress($"Loaded {diagnostics.InstalledPackages.Count} installed packages");
            }
        }

        /// <summary>
        /// Initializes the Python runtime with appropriate environment based on engine mode.
        /// </summary>
        /// <param name="cfg">Python runtime configuration to use</param>
        /// <param name="virtualEnvPath">Optional path for virtual environment</param>
        /// <param name="envName">Name for the environment (used for both single and multi-user modes)</param>
        /// <param name="mode">The Python engine mode (SingleUser or MultiUserWithEnvAndScopeAndSession)</param>
        /// <returns>True if initialization was successful; otherwise false</returns>
        public bool Initialize(PythonRunTime cfg, string virtualEnvPath, string envName, PythonEngineMode mode)
        {
            if (cfg == null)
            {
                ReportProgress("No Python configuration provided.", Errors.Failed);
                return false;
            }

            try
            {
                // Set the engine mode for the runtime
                EngineMode = mode;

                // Determine base environment path
                string baseEnvPath;
                if (string.IsNullOrWhiteSpace(virtualEnvPath))
                {
                    baseEnvPath = GetPythonEnvironmentsPath();
                }
                else
                {
                    baseEnvPath = Path.GetDirectoryName(virtualEnvPath);
                    if (string.IsNullOrEmpty(baseEnvPath))
                    {
                        // If the path is invalid, use the cross-platform default environment path
                        baseEnvPath = GetPythonEnvironmentsPath();
                    }
                }

                // Ensure the directory exists
                if (!Directory.Exists(baseEnvPath))
                {
                    Directory.CreateDirectory(baseEnvPath);
                }

                // Set default environment name if not provided
                if (string.IsNullOrEmpty(envName))
                {
                    // Use mode-appropriate default names
                    envName = mode == PythonEngineMode.SingleUser ? "SingleUser" : "MultiUser";
                }

                // Determine environment variables
                PythonVirtualEnvironment env;
                string username = Environment.UserName;

                // If a specific virtual environment path is provided, use it
                if (!string.IsNullOrWhiteSpace(virtualEnvPath) && Directory.Exists(virtualEnvPath))
                {
                    env = VirtualEnvmanager.GetEnvironmentByPath(virtualEnvPath);

                    if (env == null)
                    {
                        ReportProgress($"Creating environment at specified path: {virtualEnvPath}");

                        // Create a new environment definition
                        env = new PythonVirtualEnvironment
                        {
                            Name = envName,
                            Path = virtualEnvPath,
                            PythonConfigID = cfg.ID,
                            BaseInterpreterPath = cfg.RuntimePath ?? cfg.BinPath,
                            CreatedOn = DateTime.Now,
                            CreatedBy = username,
                            PythonBinary = cfg.Binary, // Preserve the binary type (Python/Conda)
                            EnvironmentType = PythonEnvironmentType.VirtualEnv
                        };

                        // Try to create if it doesn't exist
                        if (!Directory.Exists(virtualEnvPath) &&
                            !VirtualEnvmanager.CreateVirtualEnvironment(cfg, env))
                        {
                            ReportProgress($"Failed to create environment at: {virtualEnvPath}", Errors.Failed);
                            return false;
                        }

                        // Add to managed environments
                        VirtualEnvmanager.AddToManagedEnvironments(env);
                    }
                }
                else
                {
                    // Create or use mode-specific environment with the provided name
                    string envPath = Path.Combine(baseEnvPath, envName);

                    // Get existing or create new environment
                    env = VirtualEnvmanager.GetEnvironmentByPath(envPath);
                    if (env == null)
                    {
                        ReportProgress($"Creating {(mode == PythonEngineMode.SingleUser ? "single-user" : "multi-user")} environment...");

                        // Create a new environment definition
                        env = new PythonVirtualEnvironment
                        {
                            Name = envName,
                            Path = envPath,
                            PythonConfigID = cfg.ID,
                            BaseInterpreterPath = cfg.RuntimePath ?? cfg.BinPath,
                            CreatedOn = DateTime.Now,
                            // Set creator based on mode
                            CreatedBy = mode == PythonEngineMode.SingleUser ? username : "system",
                            PythonBinary = cfg.Binary, // Preserve the binary type (Python/Conda)
                            EnvironmentType = PythonEnvironmentType.VirtualEnv
                        };

                        // Create the virtual environment
                        if (!VirtualEnvmanager.CreateVirtualEnvironment(cfg, env))
                        {
                            ReportProgress($"Failed to create {(mode == PythonEngineMode.SingleUser ? "single-user" : "multi-user")} environment.", Errors.Failed);
                            return false;
                        }

                        // Add to managed environments
                        VirtualEnvmanager.AddToManagedEnvironments(env);
                    }
                }

                // Now initialize Python with the appropriate environment
                if (env == null)
                {
                    ReportProgress("Failed to obtain a valid Python environment.", Errors.Failed);
                    return false;
                }

                // First initialize the Python engine
                bool pythonInitialized = InitializePythonEngine(cfg, env);
                if (!pythonInitialized)
                {
                    ReportProgress("Failed to initialize Python engine.", Errors.Failed);
                    return false;
                }

                // Now that Python is initialized, create appropriate sessions based on mode
                PythonSessionInfo session = null;

                if (mode == PythonEngineMode.SingleUser)
                {
                    // Create a session for the current user
                    session = CreateSessionForUser(cfg, env, username);
                    if (session == null)
                    {
                        ReportProgress("Failed to create user session after Python initialization.", Errors.Warning);
                        // Not a fatal error, Python is still initialized
                    }
                }
                else if (mode == PythonEngineMode.MultiUserWithEnvAndScopeAndSession)
                {
                    // Create a system session for multi-user mode
                    session = CreateSessionForUser(cfg, env, "system");
                    if (session == null)
                    {
                        ReportProgress("Failed to create system session after Python initialization.", Errors.Warning);
                        // Not a fatal error, Python is still initialized
                    }
                }

                // Python is initialized and we attempted to create sessions as needed
                return true;
            }
            catch (Exception ex)
            {
                ReportProgress($"Error during initialization: {ex.Message}", Errors.Failed);
                return false;
            }
        }
        /// <summary>
        /// Core method for initializing the Python engine with a specific virtual environment.
        /// This is separated from the environment creation logic.
        /// </summary>
        /// <param name="config">Python runtime configuration</param>
        /// <param name="venv">Virtual environment to initialize with</param>
        /// <returns>True if initialization was successful; otherwise false</returns>
        private bool InitializePythonEngine(PythonRunTime config, PythonVirtualEnvironment venv)
        {
            if (IsBusy) return false;
            IsBusy = true;

            try
            {
                // Validate environment
                if (venv == null || string.IsNullOrWhiteSpace(venv.Path))
                {
                    ReportProgress("Invalid virtual environment provided.", Errors.Failed);
                    IsBusy = false;
                    return false;
                }

                if (!Directory.Exists(venv.Path))
                {
                    ReportProgress($"Virtual environment path not found: {venv.Path}", Errors.Failed);
                    IsBusy = false;
                    return false;
                }

                // Determine paths based on platform and binary type (Python/Conda)
                string pythonBinPath = venv.Path;
                string pythonScriptPath;
                string pythonExe;
                string pythonPackagePath;
                string pythonDll;

                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    if (venv.PythonBinary == PythonBinary.Conda)
                    {
                        // Conda environment structure on Windows
                        pythonScriptPath = venv.Path; // Conda typically has executables in the root directory
                        pythonExe = Path.Combine(venv.Path, "python.exe");
                        pythonPackagePath = Path.Combine(venv.Path, "Lib", "site-packages");

                        // Set Conda-specific environment variables
                        Environment.SetEnvironmentVariable("CONDA_PREFIX", venv.Path, EnvironmentVariableTarget.Process);
                        Environment.SetEnvironmentVariable("CONDA_DEFAULT_ENV", venv.Name, EnvironmentVariableTarget.Process);
                    }
                    else
                    {
                        // Standard Python venv structure on Windows
                        pythonScriptPath = Path.Combine(venv.Path, "Scripts");
                        pythonExe = Path.Combine(venv.Path, "python.exe");
                        pythonPackagePath = Path.Combine(venv.Path, "Lib", "site-packages");
                    }
                }
                else
                {
                    // Unix-like systems (Linux/macOS)
                    pythonScriptPath = Path.Combine(venv.Path, "bin");
                    pythonExe = Path.Combine(venv.Path, "bin", "python");

                    if (venv.PythonBinary == PythonBinary.Conda)
                    {
                        // Conda environment structure on Unix
                        // Get Python version from config for more precise path
                        string pythonVersion = !string.IsNullOrEmpty(config.PythonVersion)
                            ? config.PythonVersion.Split(' ')[0]
                            : "3";

                        // Try to format version number as major.minor
                        string versionPrefix = pythonVersion.Contains(".")
                            ? pythonVersion.Substring(0, pythonVersion.LastIndexOf('.'))
                            : pythonVersion;

                        pythonPackagePath = Path.Combine(venv.Path, "lib", $"python{versionPrefix}", "site-packages");

                        // Set Conda-specific environment variables
                        Environment.SetEnvironmentVariable("CONDA_PREFIX", venv.Path, EnvironmentVariableTarget.Process);
                        Environment.SetEnvironmentVariable("CONDA_DEFAULT_ENV", venv.Name, EnvironmentVariableTarget.Process);
                    }
                    else
                    {
                        // Standard Python venv structure on Unix
                        // Get Python lib directory
                        var pythonLibDir = Directory.GetDirectories(Path.Combine(venv.Path, "lib"))
                            .FirstOrDefault(d => d.Contains("python"));

                        // Set the site-packages path
                        pythonPackagePath = pythonLibDir != null
                            ? Path.Combine(pythonLibDir, "site-packages")
                            : Path.Combine(venv.Path, "lib", "python3", "site-packages");
                    }
                }

                // Verify the Python executable exists in the virtual environment
                if (!File.Exists(pythonExe))
                {
                    ReportProgress($"Python executable not found in virtual environment: {pythonExe}", Errors.Failed);
                    IsBusy = false;
                    return false;
                }

                // Check if Python is already initialized
                if (PythonEngine.IsInitialized)
                {
                    // If already initialized with the same environment, no need to reinitialize
                    ReportProgress("Python Already Initialized", Errors.Ok);
                    IsBusy = false;
                    return true;
                }

                // Set up the environment variables
                Environment.SetEnvironmentVariable("VIRTUAL_ENV", venv.Path, EnvironmentVariableTarget.Process);

                // Use platform-specific path separator for PATH
                char pathSeparator = Environment.OSVersion.Platform == PlatformID.Win32NT ? ';' : ':';
                Environment.SetEnvironmentVariable(
                    "PATH",
                    $"{pythonBinPath}{pathSeparator}{pythonScriptPath}{pathSeparator}" + Environment.GetEnvironmentVariable("PATH"),
                    EnvironmentVariableTarget.Process
                );

                Environment.SetEnvironmentVariable("PYTHONNET_RUNTIME", $"{pythonBinPath}", EnvironmentVariableTarget.Process);
                Environment.SetEnvironmentVariable("PYTHONNET_PYTHON_RUNTIME", $"{pythonBinPath}", EnvironmentVariableTarget.Process);
                Environment.SetEnvironmentVariable("PYTHONHOME", pythonBinPath, EnvironmentVariableTarget.Process);
                Environment.SetEnvironmentVariable("PYTHONPATH", $"{pythonPackagePath}", EnvironmentVariableTarget.Process);

                // Initialize Python.NET
                try
                {
                    ReportProgress("Initializing Python engine with virtual environment");

                    // Find the Python DLL
                    if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                    {
                        if (venv.PythonBinary == PythonBinary.Conda)
                        {
                            // Conda on Windows typically places DLLs in different locations
                            pythonDll = Path.Combine(pythonBinPath, Path.GetFileName(config.PythonDll));
                            if (!File.Exists(pythonDll))
                            {
                                // Try Library/bin directory (common in Conda)
                                pythonDll = Path.Combine(pythonBinPath, "Library", "bin", Path.GetFileName(config.PythonDll));
                                if (!File.Exists(pythonDll))
                                {
                                    // Fall back to base interpreter DLL as last resort
                                    pythonDll = config.PythonDll;
                                }
                            }
                        }
                        else
                        {
                            // Standard Python venv
                            pythonDll = Path.Combine(pythonBinPath, Path.GetFileName(config.PythonDll));
                            if (!File.Exists(pythonDll))
                            {
                                pythonDll = config.PythonDll; // Fall back to base interpreter DLL
                            }
                        }
                    }
                    else
                    {
                        // For Linux/macOS, find the appropriate .so file
                        var searchPattern = venv.PythonBinary == PythonBinary.Conda
                            ? "libpython*.so*"  // Conda pattern
                            : "libpython*.so*"; // Standard pattern (same for now, but could be specialized)

                        // For both Conda and standard Python, search in the lib directory
                        var soFiles = Directory.GetFiles(Path.Combine(venv.Path, "lib"), searchPattern, SearchOption.AllDirectories);
                        pythonDll = soFiles.FirstOrDefault() ?? "";
                    }

                    if (string.IsNullOrEmpty(pythonDll) || !File.Exists(pythonDll))
                    {
                        ReportProgress($"Python DLL not found for virtual environment.", Errors.Failed);
                        IsBusy = false;
                        return false;
                    }

                    // Initialize Python.NET
                    Runtime.PythonDLL = pythonDll;
                    PythonEngine.PythonHome = venv.Path;
                    PythonEngine.Initialize();

                    // Use the existing InitializePythonEnvironment method from VirtualEnvManager
                    // This handles the Python environment setup that was previously done inline
                    VirtualEnvmanager.InitializePythonEnvironment(venv);

                    ReportProgress($"Virtual environment ({(venv.PythonBinary == PythonBinary.Conda ? "Conda" : "Python")}) activated successfully", Errors.Ok);
                    _IsInitialized = true;

                    // If this environment has a requirements file, install required packages
                    InstallRequirementsIfNeeded(venv);

                    return true;
                }
                catch (Exception ex)
                {
                    ReportProgress($"Error initializing Python: {ex.Message}", Errors.Failed);
                    return false;
                }
                finally
                {
                    IsBusy = false;
                }
            }
            catch (Exception ex)
            {
                ReportProgress($"Unexpected error during initialization: {ex.Message}", Errors.Failed);
                IsBusy = false;
                return false;
            }
        }
        /// <summary>
        /// Installs packages from a requirements file if one is specified for the environment.
        /// Uses the admin session for package management operations.
        /// </summary>
        private void InstallRequirementsIfNeeded(PythonVirtualEnvironment venv)
        {
            if (venv == null || string.IsNullOrEmpty(venv.RequirementsFile) || !File.Exists(venv.RequirementsFile))
            {
                return;
            }

            ReportProgress($"Installing packages from requirements: {venv.RequirementsFile}");

            // Get the admin session for package management
            var adminSession = VirtualEnvmanager.GetPackageManagementSession(venv);

            if (adminSession == null)
            {
                ReportProgress("Failed to obtain admin session for package installation.", Errors.Warning);
                return;
            }

            // Execute pip install command
            if (ExecuteManager != null)
            {
                try
                {
                    ReportProgress($"Using admin session '{adminSession.SessionId}' for package installation");

                    var installTask = ExecuteManager.RunPythonCommandLineAsync(
                        Progress ?? DMEditor?.progress,
                        $"install -r \"{venv.RequirementsFile}\"",
                        venv.PythonBinary == PythonBinary.Conda,
                        adminSession,
                        venv);

                    installTask.Wait();
                    var result = installTask.Result;

                    if (result != null && (result.Contains("ERROR:") || result.Contains("Error:")))
                    {
                        ReportProgress($"Warning installing packages: {result}", Errors.Warning);
                    }
                    else
                    {
                        ReportProgress("Package installation completed successfully");
                    }
                }
                catch (Exception ex)
                {
                    ReportProgress($"Error installing packages: {ex.Message}", Errors.Warning);
                }
            }
            else
            {
                ReportProgress("ExecuteManager not initialized. Cannot install packages.", Errors.Warning);
            }
        }
        /// Shuts down the Python engine, disposing of any persistent scope and clearing active environment state.
        /// </summary>
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
        /// Maintains the current engine mode during restart.
        /// </summary>
        /// <param name="venv">The virtual environment to initialize with after restart</param>
        /// <returns>True if shutdown and reinitialization were successful; otherwise false</returns>
        public bool RestartWithEnvironment(PythonVirtualEnvironment venv)
        {
            if (venv == null)
            {
                ReportProgress("Cannot restart with null environment", Errors.Failed);
                return false;
            }

            // Remember the current engine mode
            var currentMode = this.EngineMode;

            // Log the restart attempt
            ReportProgress($"Restarting Python engine with environment: {venv.Name}", Errors.Ok);

            // Shut down the current Python instance
            var result = ShutDown();
            if (result.Flag != Errors.Ok)
            {
                ReportProgress("Failed to shut down current Python instance.", Errors.Failed);
                return false;
            }

            // Find the configuration for this environment
            var config = PythonInstallations?.FirstOrDefault(c => c.ID == venv.PythonConfigID);
            if (config == null)
            {
                ReportProgress($"Configuration not found for environment {venv.Name}", Errors.Failed);
                return false;
            }

            // Update environment usage timestamp
            VirtualEnvmanager.UpdateEnvironmentUsage(venv.ID);

            // Initialize with the specified environment and current mode
            return Initialize(config, venv.Path,venv.Name, currentMode);
        }

        #endregion
        #region "Create User Environments"
       
        /// <summary>
        /// Creates a session for a user with a specific environment.
        /// </summary>
        /// <param name="cfg">The Python runtime configuration to use.</param>
        /// <param name="envBasePath">The base path for user environments.</param>
        /// <param name="username">The username to create the environment for.</param>
        /// <param name="envName">Optional name for the environment (defaults to username).</param>
        /// <returns>Session associated with the new environment, or null if creation failed.</returns>
        public PythonSessionInfo CreateSessionForSingleUserMode(PythonRunTime cfg, string envBasePath,
                                                                   string username, string envName = null)
        {
            if (!IsInitialized) return null;
            // Delegate to VirtualEnvManager
            return VirtualEnvmanager.CreateEnvironmentForUser(cfg, envBasePath, username, envName);
        }

        /// <summary>
        /// Creates a session for a user with an existing environment.
        /// </summary>
        /// <param name="cfg">The Python runtime configuration to use.</param>
        /// <param name="env">The virtual environment to use.</param>
        /// <param name="username">The username to create the session for.</param>
        /// <returns>Session associated with the environment, or null if creation failed.</returns>
        public PythonSessionInfo CreateSessionForUser(PythonRunTime cfg, PythonVirtualEnvironment env, string username)
        {
            if (!IsInitialized) return null;
            if (env == null)
                throw new ArgumentNullException(nameof(env));

            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("Username cannot be empty", nameof(username));

            // Create a new session
            var session = new PythonSessionInfo
            {
                Username = username,
                VirtualEnvironmentId = env.ID,
                StartedAt = DateTime.Now,
                SessionName = $"Session_{username}_{DateTime.Now.Ticks}",
                Status = PythonSessionStatus.Active
            };

            // Initialize the environment for this session
            if (!VirtualEnvmanager.CreateEnvForUser(cfg, session))
            {
                // Initialization failed
                return null;
            }

            return session;
        }

        /// <summary>
        /// Gets or creates a single user session for immediate use.
        /// If a session doesn't exist for the current user, one will be created.
        /// </summary>
        /// <returns>A ready-to-use Python session</returns>
        public PythonSessionInfo GetOrCreateDefaultSession()
        {
            string username = Environment.UserName;

            // Check if we already have a session for this user
            var existingSession = SessionManager?.Sessions.FirstOrDefault(s =>
                s.Username == username &&
                s.Status == PythonSessionStatus.Active);

            if (existingSession != null)
            {
                return existingSession;
            }

            // No existing session, create a new one

            // Find a suitable Python runtime
            var runtime = PythonInstallations.FirstOrDefault(p => p.IsPythonInstalled);
            if (runtime == null)
            {
                RefreshPythonInstalltions();
                runtime = PythonInstallations.FirstOrDefault(p => p.IsPythonInstalled);

                if (runtime == null)
                {
                    ReportProgress("No Python installation found on this system", Errors.Failed);
                    return null;
                }
            }

            // Check for an existing environment for this user
            var env = VirtualEnvmanager?.ManagedVirtualEnvironments.FirstOrDefault(e =>
                e.CreatedBy == username);

            if (env != null)
            {
                // Use existing environment
                return CreateSessionForUser(runtime, env, username);
            }
            else
            {
                // Create a new environment
                string defaultEnvPath = Path.Combine(
                    DMEditor.ConfigEditor.ContainerName,
                    "PythonEnvironments");

                return CreateSessionForSingleUserMode(runtime, defaultEnvPath, username);
            }
        }
        #endregion "Create User Environments"

        #region "Configuration Methods"
        /// <summary>
        /// Determines if a Python runtime is available based on runtime config.
        /// </summary>
        private bool GetIsPythonAvailable(PythonRunTime config)
        {
            if (config != null)
            {
                if (!string.IsNullOrEmpty(config?.BinPath))
                {
                    return PythonRunTimeDiagnostics.IsPythonInstalled(config.BinPath);
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
        /// Adds specified paths to the current process' PATH environment variable.
        /// </summary>
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
        /// Creates or loads the Python config file from disk. If none exists, it creates a default.
        /// </summary>
        public void CreateLoadConfig()
        {
            // Implement configuration loading logic
            // This will depend on your specific configuration model
        }

        /// <summary>
        /// Saves the current Python configuration to disk.
        /// </summary>
        public void SaveConfig()
        {
            // Implement configuration saving logic
            // This will depend on your specific configuration model
        }
        #endregion

        #region "Utility Methods"
        /// <summary>
        /// Refreshes the list of Python installations available on the system.
        /// Updates the PythonInstallations property with discovered Python runtimes.
        /// </summary>
        public void RefreshPythonInstalltions()
        {
            try
            {
                ReportProgress("Scanning for Python installations...");

                // Use the diagnostics class to find Python installations
                var diagnosticReports = PythonEnvironmentDiagnostics.LookForPythonInstallations();

                // Clear existing installations or create a new collection if needed
                if (PythonInstallations == null)
                    PythonInstallations = new ObservableBindingList<PythonRunTime>();

                // Store existing IDs to avoid duplicates
                var existingIds = new HashSet<string>(
                    PythonInstallations.Select(p => p.ID),
                    StringComparer.OrdinalIgnoreCase);

                // Convert diagnostic reports to PythonRunTime objects
                foreach (var report in diagnosticReports)
                {
                    if (report.PythonFound && !string.IsNullOrEmpty(report.PythonPath))
                    {
                        // Get installation directory from Python executable path
                        string installDir = Path.GetDirectoryName(report.PythonPath);

                        // Create a new runtime configuration
                        var config = PythonRunTimeDiagnostics.GetPythonConfig(installDir);

                        if (config != null && !existingIds.Contains(config.ID))
                        {
                            // Add extra info from the diagnostic report
                            config.PythonVersion = report.PythonVersion?.Replace("Python ", "").Trim();
                            config.IsPythonInstalled = true;

                            // Check for Conda installation
                            bool isCondaInstallation = report.Warnings.Any(w => w.Contains("Conda"));
                            if (isCondaInstallation)
                            {
                                config.Binary = PythonBinary.Conda;

                                // Search for conda executable in common locations
                                string[] condaCandidates = GetCondaCandidatePaths(installDir);
                                string condaPath = condaCandidates.FirstOrDefault(File.Exists);

                                if (!string.IsNullOrEmpty(condaPath))
                                {
                                    // Set the CondaPath property directly now that it exists
                                    config.CondaPath = condaPath;
                                    ReportProgress($"Found Conda at {condaPath}");
                                }
                            }
                            else
                            {
                                config.Binary = PythonBinary.Python;
                            }

                            // Verify DLL path exists and try to find it if needed
                            if (!string.IsNullOrEmpty(config.PythonDll) && !File.Exists(config.PythonDll))
                            {
                                string foundDll = FindPythonDll(config, installDir);
                                if (!string.IsNullOrEmpty(foundDll))
                                {
                                    config.PythonDll = foundDll;
                                }
                            }

                            // Add to our collection
                            PythonInstallations.Add(config);
                            existingIds.Add(config.ID);

                            string envType = config.Binary == PythonBinary.Conda ? "Conda" : "Python";
                            ReportProgress($"Found {envType} {config.PythonVersion} at {config.BinPath}");
                        }
                    }
                }

                // If no installations found, check standard locations
                if (PythonInstallations.Count == 0)
                {
                    ReportProgress("No Python installations found in PATH. Checking standard locations...");
                    CheckStandardPythonLocations();
                }

                // Save the updated configurations
                SaveConfig();

                ReportProgress($"Found {PythonInstallations.Count} Python installation(s)", Errors.Ok);
            }
            catch (Exception ex)
            {
                ReportProgress($"Error refreshing Python installations: {ex.Message}", Errors.Failed);
            }
        }

        /// <summary>
        /// Checks standard locations for Python installations
        /// </summary>
        private void CheckStandardPythonLocations()
        {
            string[] standardLocations;

            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                // Windows standard locations
                standardLocations = new[] {
            @"C:\Python312",
            @"C:\Python311",
            @"C:\Python310",
            @"C:\Program Files\Python312",
            @"C:\Program Files\Python311",
            @"C:\Program Files\Python310",
            // Anaconda/Conda locations
            @"C:\ProgramData\Anaconda3",
            @"C:\Users\" + Environment.UserName + @"\Anaconda3",
            @"C:\Users\" + Environment.UserName + @"\miniconda3"
        };
            }
            else
            {
                // Unix/Linux/macOS standard locations
                string userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                standardLocations = new[] {
            "/usr/bin",
            "/usr/local/bin",
            "/opt/python3",
            "/opt/anaconda3",
            "/opt/miniconda3",
            Path.Combine(userHome, "anaconda3"),
            Path.Combine(userHome, "miniconda3")
        };
            }

            foreach (string location in standardLocations)
            {
                if (Directory.Exists(location))
                {
                    string pythonExe = null;
                    string condaExe = null;

                    // Find executables based on platform
                    if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                    {
                        pythonExe = Path.Combine(location, "python.exe");
                        condaExe = Path.Combine(location, "Scripts", "conda.exe");

                        if (!File.Exists(condaExe))
                        {
                            condaExe = Path.Combine(location, "condabin", "conda.exe");
                        }
                    }
                    else
                    {
                        pythonExe = Path.Combine(location, "bin", "python");
                        condaExe = Path.Combine(location, "bin", "conda");
                    }

                    if (File.Exists(pythonExe))
                    {
                        var config = PythonRunTimeDiagnostics.GetPythonConfig(Path.GetDirectoryName(pythonExe));

                        if (config != null)
                        {
                            config.IsPythonInstalled = true;
                            bool isConda = File.Exists(condaExe);

                            config.Binary = isConda ? PythonBinary.Conda : PythonBinary.Python;

                            if (isConda)
                            {
                                // Now directly set the CondaPath property
                                config.CondaPath = condaExe;
                                ReportProgress($"Found Conda installation at standard location: {location}");
                            }
                            else
                            {
                                ReportProgress($"Found Python installation at standard location: {location}");
                            }

                            // Add to the collection if not a duplicate
                            if (!PythonInstallations.Any(p => p.ID == config.ID))
                            {
                                PythonInstallations.Add(config);
                            }
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Returns possible locations for the conda executable based on the installation directory.
        /// </summary>
        private string[] GetCondaCandidatePaths(string installDir)
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                return new[] {
            Path.Combine(installDir, "conda.exe"),            // Windows - Same dir as python.exe
            Path.Combine(installDir, "..", "Scripts", "conda.exe"), // Windows - Scripts dir
            Path.Combine(installDir, "..", "condabin", "conda.exe"), // Windows - condabin 
            Path.Combine(installDir, "..", "Library", "bin", "conda.exe") // Windows - Library/bin
        };
            }
            else
            {
                return new[] {
            Path.Combine(installDir, "conda"),                // Unix - Same dir
            Path.Combine(installDir, "..", "bin", "conda"),   // Unix - bin dir
            "/usr/bin/conda",                                // Unix - system dir
            "/usr/local/bin/conda"                          // Unix - local dir
        };
            }
        }

        /// <summary>
        /// Attempts to find the Python DLL in various locations.
        /// </summary>
        private string FindPythonDll(PythonRunTime config, string installDir)
        {
            string dllName = Path.GetFileName(config.PythonDll);

            if (config.Binary == PythonBinary.Conda)
            {
                // Common Conda DLL locations
                string[] dllCandidates = {
            Path.Combine(installDir, dllName),
            Path.Combine(installDir, "Library", "bin", dllName),
            Path.Combine(Path.GetDirectoryName(installDir), "Library", "bin", dllName)
        };

                return dllCandidates.FirstOrDefault(File.Exists);
            }
            else
            {
                // Standard Python DLL locations
                string[] dllCandidates = {
            Path.Combine(installDir, dllName),
            Path.Combine(installDir, "DLLs", dllName)
        };

                return dllCandidates.FirstOrDefault(File.Exists);
            }
        }
        /// <summary>
        /// Gets the appropriate path for storing Python environments based on the platform.
        /// </summary>
        /// <returns>Platform-specific path for Python environments</returns>
        public string GetPythonEnvironmentsPath()
        {
            string baseDir;

            // Determine the appropriate base directory based on the platform
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                // On Windows, use Local AppData
                baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            }
            else if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                // On Linux/macOS, use the home directory with a dot-prefixed directory
                baseDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                return Path.Combine(baseDir, ".beep", "python_environments");
            }
            else
            {
                // Fallback for other platforms
                baseDir = Path.GetTempPath();
            }

            // Construct the path with organization and application name for better organization
            string appDataPath = Path.Combine(baseDir, "TheTechIdea", "Beep", "PythonEnvironments");

            // Create the directory if it doesn't exist
            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory(appDataPath);
            }

            return appDataPath;
        }
        /// <summary>
        /// Reports progress messages using Progress if available; otherwise uses DMEditor.
        /// </summary>
        private void ReportProgress(PassedArgs args)
        {
            if (Progress != null)
            {
                Progress.Report(args);
            }
        }

        /// <summary>
        /// Overload of ReportProgress that takes a string message and an error flag.
        /// </summary>
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
        /// Converts an arbitrary C# object to a PyObject.
        /// </summary>
        public static PyObject ToPython(object obj)
        {
            using (Py.GIL())
            {
                return PyObject.FromManagedObject(obj);
            }
        }
        #endregion

        /// <summary>
        /// Signals the runtime manager to attempt stopping any ongoing Python code execution.
        /// </summary>
        public void Stop()
        {
            _shouldStop = true;
        }

     

        #region "Session Management"
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
        /// </summary>
        public void PerformSessionCleanup(TimeSpan maxAge)
        {
            if (IsBusy)
                return;

            var now = DateTime.Now;
            var sessionsToCleanup = SessionManager.Sessions
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
                    var env = VirtualEnvmanager.ManagedVirtualEnvironments.FirstOrDefault(v =>
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
               SessionManager.Sessions.Remove(session);
            }
        }
       

        #endregion

        #region "IDisposable Implementation"
        /// <summary>
        /// Protected virtual dispose method for freeing resources.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    foreach (var session in SessionManager.Sessions.ToList())
                    {
                        CleanupSession(session);
                    }

                    ShutDown();
                }
                // Free unmanaged objects (if any) and set large fields to null

                disposedValue = true;
            }
        }

        /// <summary>
        /// Finalizer.
        /// </summary>
        ~PythonNetRunTimeManager()
        {
            Dispose(disposing: false);
        }

        /// <summary>
        /// Public dispose method.
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
