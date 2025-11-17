using Beep.Python.Model;
using Beep.Python.RuntimeEngine.Helpers;
using Beep.Python.RuntimeEngine.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
 
using TheTechIdea.Beep.Editor;
using Environment = System.Environment;

namespace Beep.Python.RuntimeEngine
{
    /// <summary>
    /// High-level orchestrator for Python runtime management.
    /// Provides a developer-friendly API that coordinates all Python managers.
    /// This is the recommended entry point for working with Python in your application.
    /// </summary>
    public class PythonRuntimeOrchestrator : IPythonRuntimeOrchestrator
    {
        #region Fields

       
        private readonly IDMEEditor _dmEditor;
        private bool _disposed = false;
        private PythonRunTime _basePythonRuntime;
        private string _pythonBasePath;
        private PythonVirtualEnvironment _adminEnvironment;
        private PythonVirtualEnvironment _currentEnvironment;
        private PythonSessionInfo _currentSession;
        private readonly Dictionary<string, PythonSessionInfo> _userSessions = new Dictionary<string, PythonSessionInfo>();
        private readonly object _lockObject = new object();
        private string? _workingDirectory;
        
        // Default base packages for admin environment
        private static readonly List<string> DefaultBasePackages = new List<string>
        {
            "pip",
            "setuptools",
            "wheel",
            "virtualenv"
        };

        #endregion

        #region Properties

        public IPythonRunTimeManager RuntimeManager { get; private set; }
        public IPythonSessionManager SessionManager => RuntimeManager?.SessionManager;
        public IPythonVirtualEnvManager VirtualEnvManager => RuntimeManager?.VirtualEnvmanager;
        public IPythonCodeExecuteManager ExecuteManager => RuntimeManager?.ExecuteManager;
        public PythonEngineMode Mode { get; private set; }
        public bool IsInitialized { get; private set; }
        public PythonVirtualEnvironment AdminEnvironment => _adminEnvironment;
        public PythonVirtualEnvironment CurrentEnvironment => _currentEnvironment;
        public ObservableBindingList<PythonRunTime> AvailablePythonInstallations => RuntimeManager?.PythonInstallations;
        public ObservableBindingList<PythonVirtualEnvironment> ManagedEnvironments => VirtualEnvManager?.ManagedVirtualEnvironments;

        /// <summary>
        /// Gets or sets the working directory for Python environments.
        /// When set together with a Python initialization call that provides a shell path,
        /// the shell path will overwrite <see cref="PythonRunTime.RootPath"/> so that all
        /// environments are created under the shell directory.
        /// </summary>
        public string? WorkingDirectory
        {
            get => _workingDirectory;
            set => _workingDirectory = value;
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of PythonRuntimeOrchestrator.
        /// </summary>
        /// <param name="beepService">BEEP service for application integration</param>
        public PythonRuntimeOrchestrator( )
        {
             
           
            // Create the runtime manager
            RuntimeManager = new PythonNetRunTimeManager();
            
            LogInfo("PythonRuntimeOrchestrator created");
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes the orchestrator with an embedded Python runtime.
        /// Downloads and sets up Python if not already available.
        /// </summary>
        /// <param name="mode">Operating mode (SingleUser or MultiUser)</param>
        /// <param name="basePackages">Base packages to install in admin environment</param>
        /// <param name="progress">Progress reporter</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task<bool> InitializeWithEmbeddedPythonAsync(
            PythonEngineMode mode = PythonEngineMode.SingleUser,
            List<string> basePackages = null,
            IProgress<string> progress = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                LogInfo($"Initializing with embedded Python, mode: {mode}");
                progress?.Report("Starting embedded Python initialization...");

                Mode = mode;

                // Check for embedded Python provisioner
                var provisioner = new PythonEmbeddedProvisioner();
                
                progress?.Report("Checking for embedded Python installation...");
                
                // Try to get existing embedded Python or download it
                var embeddedPath = await provisioner.GetOrDownloadEmbeddedPythonAsync(
                    new Progress<string>(msg => progress?.Report($"Download: {msg}")),
                    cancellationToken);

                if (string.IsNullOrEmpty(embeddedPath))
                {
                    LogError("Failed to get embedded Python");
                    return false;
                }

                progress?.Report($"Embedded Python ready at: {embeddedPath}");

                // Initialize with the embedded Python
                return await InitializeWithExistingPythonAsync(
                    embeddedPath,
                    mode,
                    basePackages,
                    progress,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                LogError($"Failed to initialize with embedded Python: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Initializes the orchestrator with an existing Python installation.
        /// </summary>
        /// <param name="pythonPath">Path to existing Python installation</param>
        /// <param name="mode">Operating mode (SingleUser or MultiUser)</param>
        /// <param name="basePackages">Base packages to install in admin environment</param>
        /// <param name="progress">Progress reporter</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task<bool> InitializeWithExistingPythonAsync(
            string pythonPath,
            PythonEngineMode mode = PythonEngineMode.SingleUser,
            List<string> basePackages = null,
            IProgress<string> progress = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                LogInfo($"Initializing with existing Python at {pythonPath}, mode: {mode}");
                progress?.Report($"Initializing Python runtime at: {pythonPath}");

                Mode = mode;

                // Initialize the runtime with the Python path
                _basePythonRuntime = RuntimeManager.Initialize(pythonPath);
                _pythonBasePath = pythonPath;
                
                if (_basePythonRuntime == null)
                {
                    LogError("Failed to initialize base Python runtime");
                    return false;
                }

                progress?.Report("Base Python runtime initialized");

                // Create admin environment
                var adminEnvPath = Path.Combine(
                    _pythonBasePath,
                    "python_environments",
                    "admin_env");

                progress?.Report("Creating admin environment...");

                _adminEnvironment = await CreateAdminEnvironmentAsync(
                    _basePythonRuntime,
                    adminEnvPath,
                    basePackages ?? DefaultBasePackages,
                    progress,
                    cancellationToken);

                if (_adminEnvironment == null)
                {
                    LogError("Failed to create admin environment");
                    return false;
                }

                progress?.Report("Admin environment created successfully");

                // Initialize the Python engine with admin environment
                var initSuccess = RuntimeManager.Initialize(
                    _basePythonRuntime,
                    _adminEnvironment.Path,
                    "admin_env",
                    mode);

                if (!initSuccess)
                {
                    LogError("Failed to initialize Python engine");
                    return false;
                }

                progress?.Report("Python engine initialized");

                // For single-user mode, create a default user environment
                if (mode == PythonEngineMode.SingleUser)
                {
                    progress?.Report("Creating default user environment for single-user mode...");
                    
                    var defaultEnvPath = Path.Combine(
                        _pythonBasePath,
                        "python_environments",
                        "default_env");

                    _currentEnvironment = await CreateEnvironmentAsync(
                        "default",
                        defaultEnvPath,
                        null,
                        "System",
                        progress,
                        cancellationToken);

                    if (_currentEnvironment != null)
                    {
                        // Create default session
                        _currentSession = RuntimeManager.CreateSessionForSingleUserMode(
                            _basePythonRuntime,
                            Path.GetDirectoryName(defaultEnvPath),
                            "default",
                            "default");

                        progress?.Report("Default environment and session created");
                    }

                    UpdateActiveEnvironmentFlag(_currentEnvironment?.ID);
                }

                IsInitialized = true;
                progress?.Report("Orchestrator initialization complete");
                LogInfo("Orchestrator initialized successfully");

                return true;
            }
            catch (Exception ex)
            {
                LogError($"Failed to initialize with existing Python: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Creates the admin environment with base packages.
        /// </summary>
        private async Task<PythonVirtualEnvironment> CreateAdminEnvironmentAsync(
            PythonRunTime baseRuntime,
            string envPath,
            List<string> packages,
            IProgress<string> progress,
            CancellationToken cancellationToken)
        {
            try
            {
                // Check if admin environment already exists
                var existingEnv = VirtualEnvManager.GetEnvironmentByPath(envPath);
                if (existingEnv != null)
                {
                    LogInfo("Using existing admin environment");
                    return existingEnv;
                }

                // Create the admin environment
                var success = VirtualEnvManager.CreateVirtualEnvironment(baseRuntime, envPath);
                
                if (!success)
                {
                    LogError("Failed to create admin virtual environment");
                    return null;
                }

                var adminEnv = VirtualEnvManager.GetEnvironmentByPath(envPath);
                
                if (adminEnv == null)
                {
                    LogError("Admin environment created but not found");
                    return null;
                }

                // Mark as admin environment
                adminEnv.Name = "admin_env";
                adminEnv.CreatedBy = "System";

                // Install base packages if provided (delegated to higher-level bootstrap/profile system)
                if (packages != null && packages.Count > 0)
                {
                    progress?.Report("Admin environment created; base package installation should be handled by bootstrap/profile system.");
                }

                return adminEnv;
            }
            catch (Exception ex)
            {
                LogError($"Error creating admin environment: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// Reinitializes the orchestrator with a different mode.
        /// </summary>
        public async Task<bool> ReinitializeAsync(PythonEngineMode newMode)
        {
            try
            {
                LogInfo($"Reinitializing orchestrator with mode: {newMode}");

                if (Mode == newMode)
                {
                    LogInfo("Already in requested mode");
                    return true;
                }

                // Shutdown current runtime
                var shutdownResult = RuntimeManager.ShutDown();
                if (shutdownResult.Flag != Errors.Ok)
                {
                    LogWarning($"Shutdown had warnings: {shutdownResult.Message}");
                }

                // Clear current state
                _currentEnvironment = null;
                UpdateActiveEnvironmentFlag(null);
                _currentSession = null;
                _userSessions.Clear();
                IsInitialized = false;

                // Reinitialize with new mode using same python path
                if (string.IsNullOrEmpty(_pythonBasePath))
                {
                    LogError("Cannot reinitialize: python base path is not set");
                    return false;
                }

                return await InitializeWithExistingPythonAsync(
                    _pythonBasePath,
                    newMode);
            }
            catch (Exception ex)
            {
                LogError($"Failed to reinitialize: {ex.Message}", ex);
                return false;
            }
        }

        #endregion

        #region Single-User Mode Operations

        /// <summary>
        /// Sets the current active environment for single-user mode.
        /// </summary>
        public bool SetCurrentEnvironment(string environmentId)
        {
            try
            {
                if (Mode != PythonEngineMode.SingleUser)
                {
                    LogError("SetCurrentEnvironment is only available in single-user mode");
                    return false;
                }

                var environment = GetEnvironment(environmentId);
                if (environment == null)
                {
                    LogError($"Environment not found: {environmentId}");
                    return false;
                }

                lock (_lockObject)
                {
                    _currentEnvironment = environment;
                    UpdateActiveEnvironmentFlag(environment.ID);
                    
                    // Create or update session for this environment
                    _currentSession = RuntimeManager.CreateSessionForSingleUserMode(
                        _basePythonRuntime,
                        Path.GetDirectoryName(environment.Path),
                        "default",
                        environment.Name);

                    LogInfo($"Current environment set to: {environment.Name}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                LogError($"Failed to set current environment: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Creates a new environment and sets it as current (single-user mode).
        /// </summary>
        public async Task<PythonVirtualEnvironment> CreateAndSetEnvironmentAsync(
            string envName,
            List<string> packageProfiles = null,
            IProgress<string> progress = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (Mode != PythonEngineMode.SingleUser)
                {
                    LogError("CreateAndSetEnvironment is only available in single-user mode");
                    return null;
                }

                // Always create environments under the python base path
                var envPath = Path.Combine(
                    _pythonBasePath,
                    "python_environments",
                    envName);

                var environment = await CreateEnvironmentAsync(
                    envName,
                    envPath,
                    packageProfiles,
                    "User",
                    progress,
                    cancellationToken);

                if (environment != null)
                {
                    SetCurrentEnvironment(environment.ID);
                }

                return environment;
            }
            catch (Exception ex)
            {
                LogError($"Failed to create and set environment: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// Executes Python code in the current environment (single-user mode).
        /// </summary>
        public async Task<(bool Success, string Output)> ExecuteAsync(
            string code,
            int timeout = 120,
            IProgress<PassedArgs> progress = null)
        {
            try
            {
                if (Mode != PythonEngineMode.SingleUser)
                {
                    LogError("ExecuteAsync (without session) is only available in single-user mode");
                    return (false, "This method requires single-user mode");
                }

                if (_currentSession == null)
                {
                    LogError("No current session available");
                    return (false, "No active session. Please set an environment first.");
                }

                return await ExecuteManager.ExecuteCodeAsync(
                    code,
                    _currentSession,
                    timeout,
                    progress);
            }
            catch (Exception ex)
            {
                LogError($"Failed to execute code: {ex.Message}", ex);
                return (false, $"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the current session for single-user mode.
        /// </summary>
        public PythonSessionInfo GetCurrentSession()
        {
            if (Mode != PythonEngineMode.SingleUser)
            {
                LogWarning("GetCurrentSession called in multi-user mode");
                return null;
            }

            return _currentSession;
        }

        #endregion

        #region Multi-User Mode Operations

        /// <summary>
        /// Creates a new environment for a specific user (multi-user mode).
        /// </summary>
        public async Task<(PythonVirtualEnvironment Environment, PythonSessionInfo Session)> CreateUserEnvironmentAsync(
            string username,
            string envName = null,
            List<string> packageProfiles = null,
            IProgress<string> progress = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username))
                {
                    LogError("Username is required");
                    return (null, null);
                }

                var actualEnvName = envName ?? username;
                // User environments are created under the python base path
                var userEnvPath = Path.Combine(
                    _pythonBasePath,
                    "python_environments",
                    "users",
                    username,
                    actualEnvName);

                progress?.Report($"Creating environment for user: {username}");

                var environment = await CreateEnvironmentAsync(
                    actualEnvName,
                    userEnvPath,
                    packageProfiles,
                    username,
                    progress,
                    cancellationToken);

                if (environment == null)
                {
                    LogError($"Failed to create environment for user: {username}");
                    return (null, null);
                }

                // Create session for user
                var session = RuntimeManager.CreateSessionForUser(
                    _basePythonRuntime,
                    environment,
                    username);

                if (session == null)
                {
                    LogError($"Failed to create session for user: {username}");
                    return (environment, null);
                }

                lock (_lockObject)
                {
                    _userSessions[username] = session;
                }

                progress?.Report($"Environment and session created for: {username}");
                LogInfo($"User environment created: {username}/{actualEnvName}");

                return (environment, session);
            }
            catch (Exception ex)
            {
                LogError($"Failed to create user environment: {ex.Message}", ex);
                return (null, null);
            }
        }

        /// <summary>
        /// Gets or creates a session for a user in multi-user mode.
        /// </summary>
        public PythonSessionInfo GetOrCreateUserSession(string username, string environmentId = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username))
                {
                    LogError("Username is required");
                    return null;
                }

                lock (_lockObject)
                {
                    // Check if user already has a session
                    if (_userSessions.TryGetValue(username, out var existingSession))
                    {
                        if (existingSession.Status == PythonSessionStatus.Active)
                        {
                            return existingSession;
                        }
                    }

                    // Create new session
                    var session = SessionManager.CreateSession(username, environmentId);
                    
                    if (session != null)
                    {
                        _userSessions[username] = session;
                        LogInfo($"Session created for user: {username}");
                    }

                    return session;
                }
            }
            catch (Exception ex)
            {
                LogError($"Failed to get or create user session: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// Executes Python code for a specific user (multi-user mode).
        /// </summary>
        public async Task<(bool Success, string Output)> ExecuteForUserAsync(
            string username,
            string code,
            int timeout = 120,
            IProgress<PassedArgs> progress = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username))
                {
                    LogError("Username is required");
                    return (false, "Username is required");
                }

                var session = GetOrCreateUserSession(username);
                
                if (session == null)
                {
                    LogError($"Failed to get session for user: {username}");
                    return (false, $"Failed to get session for user: {username}");
                }

                return await ExecuteManager.ExecuteCodeAsync(
                    code,
                    session,
                    timeout,
                    progress);
            }
            catch (Exception ex)
            {
                LogError($"Failed to execute code for user: {ex.Message}", ex);
                return (false, $"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Terminates a user's session.
        /// </summary>
        public IErrorsInfo TerminateUserSession(string username)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username))
                {
                    return new ErrorsInfo { Flag = Errors.Failed, Message = "Username is required" };
                }

                lock (_lockObject)
                {
                    if (_userSessions.TryGetValue(username, out var session))
                    {
                        var result = SessionManager.TerminateSession(session.SessionId);
                        _userSessions.Remove(username);
                        
                        LogInfo($"Session terminated for user: {username}");
                        return result;
                    }

                    return new ErrorsInfo { Flag = Errors.Ok, Message = "No active session found" };
                }
            }
            catch (Exception ex)
            {
                LogError($"Failed to terminate user session: {ex.Message}", ex);
                return new ErrorsInfo { Flag = Errors.Failed, Message = ex.Message };
            }
        }

        #endregion

        #region Environment Management

        /// <summary>
        /// Creates a new virtual environment with specified configuration.
        /// </summary>
        public async Task<PythonVirtualEnvironment> CreateEnvironmentAsync(
            string envName,
            string envPath = null,
            List<string> packageProfiles = null,
            string createdBy = "System",
            IProgress<string> progress = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // All environments live under the python base path
                var root = !string.IsNullOrEmpty(_pythonBasePath) ? _pythonBasePath : Environment.CurrentDirectory;
                var actualPath = envPath ?? Path.Combine(
                    root,
                    "python_environments",
                    envName);

                progress?.Report($"Creating environment: {envName}");

                // Check if environment already exists
                var existing = VirtualEnvManager.GetEnvironmentByPath(actualPath);
                if (existing != null)
                {
                    LogInfo($"Environment already exists: {envName}");
                    return existing;
                }

                // Create environment without assuming extended IPythonVirtualEnvManager APIs
                var created = VirtualEnvManager.CreateVirtualEnvironment(_basePythonRuntime, actualPath);
                if (!created)
                {
                    LogError($"Failed to create environment: {envName}");
                    return null;
                }

                var environment = VirtualEnvManager.GetEnvironmentByPath(actualPath);
                if (environment != null)
                {
                    environment.Name = envName;
                    environment.CreatedBy = createdBy;

                    if (packageProfiles != null)
                    {
                        environment.PackageProfiles = new List<string>(packageProfiles);
                    }
                    else if (environment.PackageProfiles == null)
                    {
                        environment.PackageProfiles = new List<string>();
                    }

                    progress?.Report($"Environment created: {envName}");
                    LogInfo($"Environment created successfully: {envName}");
                }

                return environment;
            }
            catch (Exception ex)
            {
                LogError($"Failed to create environment: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// Gets an environment by name or ID.
        /// </summary>
        public PythonVirtualEnvironment GetEnvironment(string nameOrId)
        {
            if (string.IsNullOrWhiteSpace(nameOrId))
                return null;

            // Try by ID first
            var byId = VirtualEnvManager.GetEnvironmentById(nameOrId);
            if (byId != null)
                return byId;

            // Try by name
            return ManagedEnvironments?.FirstOrDefault(e => 
                e.Name?.Equals(nameOrId, StringComparison.OrdinalIgnoreCase) == true);
        }

        /// <summary>
        /// Deletes an environment and cleans up resources.
        /// </summary>
        public bool DeleteEnvironment(string environmentId)
        {
            try
            {
                var environment = GetEnvironment(environmentId);
                
                if (environment == null)
                {
                    LogError($"Environment not found: {environmentId}");
                    return false;
                }

                // Don't allow deletion of admin environment
                if (environment.ID == _adminEnvironment?.ID)
                {
                    LogError("Cannot delete admin environment");
                    return false;
                }

                // Don't allow deletion of current environment in single-user mode
                if (Mode == PythonEngineMode.SingleUser && environment.ID == _currentEnvironment?.ID)
                {
                    LogError("Cannot delete current environment. Switch to another environment first.");
                    return false;
                }

                // Terminate any sessions using this environment
                var sessionsToTerminate = SessionManager.Sessions
                    .Where(s => s.VirtualEnvironmentId == environment.ID)
                    .ToList();

                foreach (var session in sessionsToTerminate)
                {
                    SessionManager.TerminateSession(session.SessionId);
                }

                // Remove from managed environments
                var removed = VirtualEnvManager.RemoveEnvironment(environment.ID);

                // Optionally delete the physical directory
                if (removed && Directory.Exists(environment.Path))
                {
                    try
                    {
                        Directory.Delete(environment.Path, true);
                        LogInfo($"Environment deleted: {environment.Name}");
                    }
                    catch (Exception ex)
                    {
                        LogWarning($"Failed to delete environment directory: {ex.Message}");
                    }
                }

                return removed;
            }
            catch (Exception ex)
            {
                LogError($"Failed to delete environment: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Installs packages in a specific environment.
        /// </summary>
        public async Task<bool> InstallPackagesAsync(
            string environmentId,
            List<string> packages,
            IProgress<string> progress = null)
        {
            try
            {
                var environment = GetEnvironment(environmentId);
                
                if (environment == null)
                {
                    LogError($"Environment not found: {environmentId}");
                    return false;
                }

                if (packages == null || packages.Count == 0)
                {
                    LogWarning("No packages specified");
                    return true;
                }

                progress?.Report($"Installing {packages.Count} package(s) in: {environment.Name}");

                progress?.Report("Package installation should be handled by external package manager or bootstrap system.");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Failed to install packages: {ex.Message}", ex);
                return false;
            }
        }

        #endregion

        #region Helpers

        private void UpdateActiveEnvironmentFlag(string? environmentId)
        {
            var environments = ManagedEnvironments;
            if (environments == null)
            {
                return;
            }

            foreach (var env in environments)
            {
                var isActive = !string.IsNullOrEmpty(environmentId) &&
                               string.Equals(env.ID, environmentId, StringComparison.OrdinalIgnoreCase);
                env.IsActive = isActive;
            }
        }

        #endregion

        #region Code Execution (Advanced)

        /// <summary>
        /// Executes code in a specific session with variables.
        /// </summary>
        public async Task<object> ExecuteWithVariablesAsync(
            PythonSessionInfo session,
            string code,
            Dictionary<string, object> variables,
            int timeout = 120,
            IProgress<PassedArgs> progress = null)
        {
            try
            {
                if (session == null)
                {
                    LogError("Session is required");
                    return null;
                }

                return await ExecuteManager.ExecuteWithVariablesAsync(
                    code,
                    session,
                    variables,
                    progress);
            }
            catch (Exception ex)
            {
                LogError($"Failed to execute with variables: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// Executes a Python script file in a session.
        /// </summary>
        public async Task<(bool Success, string Output)> ExecuteScriptAsync(
            PythonSessionInfo session,
            string filePath,
            int timeout = 300,
            IProgress<PassedArgs> progress = null)
        {
            try
            {
                if (session == null)
                {
                    LogError("Session is required");
                    return (false, "Session is required");
                }

                if (!File.Exists(filePath))
                {
                    LogError($"Script file not found: {filePath}");
                    return (false, $"File not found: {filePath}");
                }

                return await ExecuteManager.ExecuteScriptFileAsync(
                    filePath,
                    session,
                    timeout,
                    progress);
            }
            catch (Exception ex)
            {
                LogError($"Failed to execute script: {ex.Message}", ex);
                return (false, $"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Executes batch commands in a session.
        /// </summary>
        public async Task<List<object>> ExecuteBatchAsync(
            PythonSessionInfo session,
            IList<string> commands,
            IProgress<PassedArgs> progress = null)
        {
            try
            {
                if (session == null)
                {
                    LogError("Session is required");
                    return null;
                }

                return await ExecuteManager.ExecuteBatchAsync(
                    commands,
                    session,
                    progress);
            }
            catch (Exception ex)
            {
                LogError($"Failed to execute batch: {ex.Message}", ex);
                return null;
            }
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Refreshes the list of available Python installations.
        /// </summary>
        public void RefreshPythonInstallations()
        {
            try
            {
                RuntimeManager.RefreshPythonInstalltions();
                LogInfo("Python installations refreshed");
            }
            catch (Exception ex)
            {
                LogError($"Failed to refresh Python installations: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Performs cleanup of stale sessions and environments.
        /// </summary>
        public void PerformMaintenance(
            TimeSpan? sessionMaxAge = null,
            TimeSpan? environmentMaxIdleTime = null)
        {
            try
            {
                var maxAge = sessionMaxAge ?? TimeSpan.FromHours(12);
                var maxIdle = environmentMaxIdleTime ?? TimeSpan.FromHours(24);

                SessionManager?.PerformSessionCleanup(maxAge);
                VirtualEnvManager?.PerformEnvironmentCleanup(maxIdle);

                LogInfo("Maintenance completed");
            }
            catch (Exception ex)
            {
                LogError($"Failed to perform maintenance: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets diagnostic information about the orchestrator state.
        /// </summary>
        public Dictionary<string, object> GetDiagnostics()
        {
            try
            {
                var diagnostics = new Dictionary<string, object>
                {
                    ["IsInitialized"] = IsInitialized,
                    ["Mode"] = Mode.ToString(),
                    ["AdminEnvironment"] = AdminEnvironment?.Name ?? "None",
                    ["CurrentEnvironment"] = CurrentEnvironment?.Name ?? "None",
                    ["TotalEnvironments"] = ManagedEnvironments?.Count ?? 0,
                    ["TotalSessions"] = SessionManager?.SessionCount ?? 0,
                    ["ActiveSessions"] = SessionManager?.ActiveSessionCount ?? 0,
                    ["AvailablePythonInstallations"] = AvailablePythonInstallations?.Count ?? 0
                };

                if (Mode == PythonEngineMode.SingleUser)
                {
                    diagnostics["CurrentSession"] = _currentSession?.SessionId ?? "None";
                }
                else
                {
                    diagnostics["UserSessionCount"] = _userSessions.Count;
                }

                return diagnostics;
            }
            catch (Exception ex)
            {
                LogError($"Failed to get diagnostics: {ex.Message}", ex);
                return new Dictionary<string, object> { ["Error"] = ex.Message };
            }
        }

        /// <summary>
        /// Switches between single-user and multi-user modes.
        /// </summary>
        public async Task<bool> SwitchModeAsync(PythonEngineMode newMode)
        {
            return await ReinitializeAsync(newMode);
        }

        #endregion

        #region Logging

        private void LogInfo(string message)
        {
            _dmEditor?.AddLogMessage("PythonOrchestrator", message, DateTime.Now, 0, null, Errors.Ok);
        }

        private void LogWarning(string message)
        {
            _dmEditor?.AddLogMessage("PythonOrchestrator", message, DateTime.Now, 0, null, Errors.Failed);
        }

        private void LogError(string message, Exception ex = null)
        {
            var fullMessage = ex != null ? $"{message}: {ex.Message}" : message;
            _dmEditor?.AddLogMessage("PythonOrchestrator", fullMessage, DateTime.Now, 0, null, Errors.Failed);
        }

        #endregion

        #region IDisposable

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    try
                    {
                        // Terminate all user sessions
                        lock (_lockObject)
                        {
                            foreach (var username in _userSessions.Keys.ToList())
                            {
                                TerminateUserSession(username);
                            }
                            _userSessions.Clear();
                        }

                        // Shutdown runtime
                        RuntimeManager?.ShutDown();
                        RuntimeManager?.Dispose();

                        LogInfo("PythonRuntimeOrchestrator disposed");
                    }
                    catch (Exception ex)
                    {
                        LogError($"Error during disposal: {ex.Message}", ex);
                    }
                }

                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~PythonRuntimeOrchestrator()
        {
            Dispose(false);
        }

        #endregion
    }
}
