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
using Newtonsoft.Json;
using Beep.Python.RuntimeEngine.ViewModels;
using System.Text;

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

        #region "Initialization and Shutdown"
       

        /// <summary>
        /// Initializes the Python environment using the specified virtual environment path.
        /// </summary>
        public bool Initialize(PythonRunTime config, string virtualEnvPath) 
        {
            if (IsBusy) return false;
            IsBusy = true;

            try
            {
                // Use the provided virtual environment path or fall back to the config's BinPath
                if (string.IsNullOrWhiteSpace(virtualEnvPath))
                {
                    ReportProgress("No virtual environment path provided.", Errors.Failed);
                    IsBusy = false;
                    return false;
                }

                if (!Directory.Exists(virtualEnvPath))
                {
                    var ret=VirtualEnvmanager.CreateVirtualEnvironment(config, virtualEnvPath);
                    if (ret)
                    {
                        ReportProgress($"Virtual environment created successfully at {virtualEnvPath}", Errors.Ok);
                    }
                    else
                    {
                        ReportProgress($"Failed to create virtual environment at {virtualEnvPath}", Errors.Failed);
                        IsBusy = false;
                        return false;
                    }
                   
                }

                // Determine paths based on platform
                string pythonBinPath = virtualEnvPath;
                string pythonScriptPath;
                string pythonExe;
                string pythonPackagePath;

                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    pythonScriptPath = Path.Combine(virtualEnvPath, "Scripts");
                    pythonExe = Path.Combine(virtualEnvPath, "python.exe");
                    pythonPackagePath = Path.Combine(virtualEnvPath, "Lib", "site-packages");
                }
                else
                {
                    pythonScriptPath = Path.Combine(virtualEnvPath, "bin");
                    pythonExe = Path.Combine(virtualEnvPath, "bin", "python");
                    // Get Python lib directory
                    var pythonLibDir = Directory.GetDirectories(Path.Combine(virtualEnvPath, "lib"))
                        .FirstOrDefault(d => d.Contains("python"));

                    // Set the site-packages path
                    pythonPackagePath = pythonLibDir != null
                        ? Path.Combine(pythonLibDir, "site-packages")
                        : Path.Combine(virtualEnvPath, "lib", "python3", "site-packages");

                }

                // Verify the Python executable exists in the virtual environment
                if (!File.Exists(pythonExe))
                {
                    ReportProgress($"Python executable not found in virtual environment: {pythonExe}", Errors.Failed);
                    IsBusy = false;
                    return false;
                }

                if (config != null && File.Exists(pythonExe))
                {
                    if (!PythonEngine.IsInitialized)
                    {
                        // Set up the environment variables
                        Environment.SetEnvironmentVariable("VIRTUAL_ENV", virtualEnvPath, EnvironmentVariableTarget.Process);
                        Environment.SetEnvironmentVariable("PATH", $"{pythonBinPath};{pythonScriptPath};" + Environment.GetEnvironmentVariable("PATH"), EnvironmentVariableTarget.Process);
                        Environment.SetEnvironmentVariable("PYTHONNET_RUNTIME", $"{pythonBinPath}", EnvironmentVariableTarget.Process);
                        Environment.SetEnvironmentVariable("PYTHONNET_PYTHON_RUNTIME", $"{pythonBinPath}", EnvironmentVariableTarget.Process);
                        Environment.SetEnvironmentVariable("PYTHONHOME", pythonBinPath, EnvironmentVariableTarget.Process);
                        Environment.SetEnvironmentVariable("PYTHONPATH", $"{pythonPackagePath};", EnvironmentVariableTarget.Process);

                        try
                        {
                            ReportProgress("Initializing Python engine with virtual environment");

                            // Find the Python DLL
                            string pythonDll;
                            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                            {
                                pythonDll = Path.Combine(pythonBinPath, Path.GetFileName(config.PythonDll));
                                if (!File.Exists(pythonDll))
                                {
                                    pythonDll = config.PythonDll; // Fall back to base interpreter DLL
                                }
                            }
                            else
                            {
                                // For Linux/macOS, we need to find the appropriate .so file
                                var soFiles = Directory.GetFiles(Path.Combine(virtualEnvPath, "lib"), "libpython*.so*", SearchOption.AllDirectories);
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
                            PythonEngine.PythonHome = virtualEnvPath;
                            PythonEngine.Initialize();

                            // After initialization, setup the virtual environment correctly
                            using (Py.GIL())
                            {
                                string activationCode = $@"
import os
import sys
import site

# Set environment variables
os.environ['VIRTUAL_ENV'] = r'{virtualEnvPath}'

# Ensure virtual environment is properly activated
if r'{pythonPackagePath}' not in sys.path:
    sys.path.insert(0, r'{pythonPackagePath}')

sys.prefix = r'{virtualEnvPath}'
sys.exec_prefix = r'{virtualEnvPath}'

# Print environment details for debugging
print(f'Python {{sys.version}} on {{sys.platform}}')
print(f'Virtual Environment: {{os.environ.get(""VIRTUAL_ENV"")}}')
print(f'sys.prefix: {{sys.prefix}}')
print(f'sys.path: {{sys.path}}')
";
                                var scope = Py.CreateScope();
                                scope.Exec(activationCode);
                            }

                            ReportProgress("Virtual environment activated successfully", Errors.Ok);
                            _IsInitialized = true;
                            IsBusy = false;
                            return true;
                        }
                        catch (Exception ex)
                        {
                            IsBusy = false;
                            ReportProgress($"Error initializing Python: {ex.Message}", Errors.Failed);
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
                    ReportProgress("No valid Python or virtual environment available", Errors.Failed);
                    IsBusy = false;
                    return false;
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
        /// Initializes the Python environment using the provided runtime config and virtual environment.
        /// </summary>
        public bool Initialize(PythonRunTime cfg, PythonVirtualEnvironment venv)
        {
            if (venv == null || string.IsNullOrWhiteSpace(venv.Path))
            {
                ReportProgress("Invalid virtual environment provided.", Errors.Failed);
                return false;
            }

            if (!Directory.Exists(venv.Path))
            {
                // If the environment doesn't exist, try to create it
                ReportProgress($"Virtual environment does not exist. Attempting to create at {venv.Path}...");

                if (!VirtualEnvmanager.CreateVirtualEnvironmentFromDefinition(cfg, venv))
                {
                    ReportProgress("Failed to create virtual environment.", Errors.Failed);
                    return false;
                }

                // Add to managed environments if created successfully and not already there
                if (!VirtualEnvmanager.ManagedVirtualEnvironments.Any(e => e.ID == venv.ID))
                {
                    VirtualEnvmanager.ManagedVirtualEnvironments.Add(venv);
                }
            }

            // Check if requirements file exists and install packages if needed
            if (!string.IsNullOrEmpty(venv.RequirementsFile) && File.Exists(venv.RequirementsFile))
            {
                ReportProgress($"Found requirements file at: {venv.RequirementsFile}");

                // We'll need a temporary initialization to run pip
                bool tempInitialized = !_IsInitialized;
                if (tempInitialized)
                {
                    // Initialize just to install packages
                    if (!Initialize(cfg, venv.Path))
                    {
                        return false;
                    }
                }

                // Create a temporary session for requirements installation
                var tempSession = new PythonSessionInfo
                {
                    SessionName = $"TempSession_{DateTime.Now.Ticks}",
                    VirtualEnvironmentId = venv.ID,
                    Username = "system",
                    StartedAt = DateTime.Now,
                    Status = PythonSessionStatus.Active
                };

                // Install requirements
                ReportProgress("Installing required packages...");

                if (ExecuteManager != null)
                {
                    try
                    {
                        var installTask = ExecuteManager.RunPythonCommandLineAsync(
                            Progress ?? DMEditor?.progress,
                            $"install -r \"{venv.RequirementsFile}\"",
                            venv.PythonBinary == PythonBinary.Conda,
                            tempSession,
                            venv);

                        installTask.Wait();
                        var result = installTask.Result;

                        if (result != null && (result.Contains("ERROR:") || result.Contains("Error:")))
                        {
                            ReportProgress($"Warning installing packages: {result}", Errors.Warning);
                            // Continue anyway, as some packages might have been installed
                        }
                    }
                    catch (Exception ex)
                    {
                        ReportProgress($"Error installing packages: {ex.Message}", Errors.Warning);
                        // Continue with initialization anyway
                    }
                }

                // If we did a temporary initialization for package installation, shut it down
                if (tempInitialized)
                {
                    ShutDown();
                }
            }

            // Now initialize with the virtual environment path
            return Initialize(cfg, venv.Path);
        }

        /// <summary>
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
        /// </summary>
        public bool RestartWithEnvironment(PythonVirtualEnvironment venv)
        {
            // Delegate to the VirtualEnvManager for environment-specific operations
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

            return Initialize(config, venv);
        }
        #endregion
        #region "Create User Environments"
        /// <summary>
        /// Refreshes the list of Python installations available on the system.
        /// Updates the PythonInstallations property with discovered Python runtimes.
        /// </summary>
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

                            // Handle Conda environments
                            if (report.Warnings.Any(w => w.Contains("Conda")))
                            {
                                config.Binary = PythonBinary.Conda;
                            }

                            // Add to our collection
                            PythonInstallations.Add(config);
                            existingIds.Add(config.ID);

                            ReportProgress($"Found Python {config.PythonVersion} at {config.BinPath}");
                        }
                    }
                }

                // Save the updated configurations
                SaveConfig();

                ReportProgress($"Found {diagnosticReports.Count} Python installation(s)", Errors.Ok);
            }
            catch (Exception ex)
            {
                ReportProgress($"Error refreshing Python installations: {ex.Message}", Errors.Failed);
            }
        }


        /// <summary>
        /// Creates a virtual environment for a single user and returns a session connected to it.
        /// </summary>
        /// <param name="cfg">Python runtime configuration</param>
        /// <param name="envBasePath">Base directory for the environment</param>
        /// <param name="username">Username for the session</param>
        /// <param name="envName">Name of the environment (optional, default derived from username)</param>
        /// <returns>A session connected to the created environment</returns>
        public PythonSessionInfo CreateEnvironmentForSingleUserMode(PythonRunTime cfg, string envBasePath, string username, string envName = null)
        {
            // If no configuration provided, try to find one
            if (cfg == null)
            {
                // Refresh installations if needed
                if (PythonInstallations == null || PythonInstallations.Count == 0)
                {
                    RefreshPythonInstalltions();
                }

                // Get the first available installation
                cfg = PythonInstallations.FirstOrDefault(p => p.IsPythonInstalled);

                if (cfg == null)
                {
                    ReportProgress("No Python runtime configuration found on this system", Errors.Failed);
                    return null;
                }
            }

            if (string.IsNullOrWhiteSpace(envBasePath))
            {
                // Use default path if not specified
                envBasePath = Path.Combine(
                    DMEditor.ConfigEditor.ContainerName,
                    "PythonEnvironments");
            }

            // Create environment name if not provided
            if (string.IsNullOrWhiteSpace(envName))
            {
                envName = $"env_{username}_{DateTime.Now.ToString("yyyyMMddHHmmss")}";
            }

            // Get the full path for the environment
            string envPath = Path.Combine(envBasePath, envName);

            try
            {
                // Create the directory if it doesn't exist
                if (!Directory.Exists(envBasePath))
                {
                    Directory.CreateDirectory(envBasePath);
                }

                // Create a virtual environment definition
                var venv = new PythonVirtualEnvironment
                {
                    Name = envName,
                    Path = envPath,
                    PythonConfigID = cfg.ID,
                    BaseInterpreterPath = cfg.BinPath,
                    PythonBinary = PythonBinary.Python, // check if its conda or regualr
                       
                    RequirementsFile = Path.Combine(envPath, "requirements.txt"),
                    CreatedAt = DateTime.Now,
                    CreatedBy = username
                };

                // Create the actual environment
                if (!VirtualEnvmanager.CreateVirtualEnvironmentFromDefinition(cfg, venv))
                {
                    ReportProgress($"Failed to create virtual environment at {envPath}", Errors.Failed);
                    return null;
                }

                // Add to managed environments if created successfully
                VirtualEnvmanager.ManagedVirtualEnvironments.Add(venv);

                // Now create a session for this user and environment
                return CreateSessionForUser(cfg, venv, username);
            }
            catch (Exception ex)
            {
                ReportProgress($"Error creating environment: {ex.Message}", Errors.Failed);
                return null;
            }
        }

        /// <summary>
        /// Creates a Python session for a specific user with an existing virtual environment.
        /// </summary>
        /// <param name="cfg">Python runtime configuration</param>
        /// <param name="env">Virtual environment to use</param>
        /// <param name="username">Username for the session</param>
        /// <returns>The created session info</returns>
        public PythonSessionInfo CreateSessionForUser(PythonRunTime cfg, PythonVirtualEnvironment env, string username)
        {
            if (env == null)
            {
                ReportProgress("No virtual environment provided", Errors.Failed);
                return null;
            }

            if (string.IsNullOrWhiteSpace(username))
            {
                username = Environment.UserName; // Default to current system user
            }

            try
            {
                // Ensure the python runtime is properly initialized for this environment
                if (!IsInitialized)
                {
                    if (!Initialize(cfg, env))
                    {
                        ReportProgress("Failed to initialize Python for the session", Errors.Failed);
                        return null;
                    }
                }

                // Create a new session
                var session = new PythonSessionInfo
                {
                    SessionName = $"Session_{username}_{DateTime.Now.ToString("yyyyMMddHHmmss")}",
                    Username = username,
                    VirtualEnvironmentId = env.ID,
                    StartedAt = DateTime.Now,
                    Status = PythonSessionStatus.Active,
                    Metadata = new Dictionary<string, object>
                    {
                        ["RuntimeMode"] = EngineMode.ToString(),
                        ["PythonConfig"] = cfg.ID,
                        ["EnvironmentPath"] = env.Path
                    }
                };

              
                // Register the session
                SessionManager.RegisterSession(session);

                // Associate the session with the environment
                env.AddSession(session);

                // Create a Python scope for this session
                if (!CreateScope(session, env))
                {
                    ReportProgress("Failed to create Python scope for the session", Errors.Failed);
                    return null;
                }

                ReportProgress($"Created session {session.SessionId} for user {username}", Errors.Ok);
                return session;
            }
            catch (Exception ex)
            {
                ReportProgress($"Error creating session: {ex.Message}", Errors.Failed);
                return null;
            }
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

                return CreateEnvironmentForSingleUserMode(runtime, defaultEnvPath, username);
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
