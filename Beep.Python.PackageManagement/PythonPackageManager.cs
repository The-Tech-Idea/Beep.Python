using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Beep.Python.Model;
using Beep.Python.RuntimeEngine.Helpers;
using Python.Runtime;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Container.Services;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.UOW;

namespace Beep.Python.RuntimeEngine.PackageManagement
{
    /// <summary>
    /// Enhanced Python package manager with proper session management, virtual environment support,
    /// error handling, and async operations for comprehensive package management operations.
    /// Consolidates all package operations without external operation manager dependencies.
    /// </summary>
    public class PythonPackageManager : IPythonPackageManager
    {
        #region Private Fields
        private readonly object _operationLock = new object();
        private volatile bool _isDisposed = false;
        private readonly CancellationTokenSource _cancellationTokenSource;

        // Python runtime dependencies
        private readonly IBeepService _beepService;
        private readonly IPythonRunTimeManager _pythonRuntime;
        private readonly IPythonVirtualEnvManager _virtualEnvManager;
        private readonly IPythonSessionManager _sessionManager;
        private readonly HttpClient _httpClient;

        // Session and Environment management
        private PythonSessionInfo? _configuredSession;
        private PythonVirtualEnvironment? _configuredVirtualEnvironment;
        private PyModule? _sessionScope;

        // Specialized managers (removed PackageOperationManager dependency)
        private readonly RequirementsFileManager _requirementsManager;
        private readonly PackageCategoryManager _categoryManager;
        private readonly PackageSetManager _packageSetManager;

        private bool _isBusy;
        #endregion

        #region Properties
        /// <summary>
        /// Gets the DME editor instance for logging and configuration access
        /// </summary>
        public IDMEEditor Editor => _beepService.DMEEditor;

        /// <summary>
        /// Gets or sets the unit of work for tracking package changes
        /// </summary>
        public UnitofWork<PackageDefinition> UnitofWork { get; set; }

        /// <summary>
        /// Gets or sets the progress reporter for operations
        /// </summary>
        public IProgress<PassedArgs> Progress { get; set; }

        /// <summary>
        /// Gets the cancellation token for stopping operations
        /// </summary>
        public CancellationToken Token => _cancellationTokenSource.Token;

        /// <summary>
        /// Gets whether the manager is currently performing an operation
        /// </summary>
        public bool IsBusy => _isBusy;
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the PythonPackageManager class with enhanced session management
        /// </summary>
        public PythonPackageManager(
            IBeepService beepService,
            IPythonRunTimeManager pythonRuntime,
            IPythonVirtualEnvManager virtualEnvManager,
            IPythonSessionManager sessionManager)
        {
            _beepService = beepService ?? throw new ArgumentNullException(nameof(beepService));
            _pythonRuntime = pythonRuntime ?? throw new ArgumentNullException(nameof(pythonRuntime));
            _virtualEnvManager = virtualEnvManager ?? throw new ArgumentNullException(nameof(virtualEnvManager));
            _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
            _cancellationTokenSource = new CancellationTokenSource();

            // Initialize HTTP client for online package checks
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };

            // Create progress reporter
            Progress = new Progress<PassedArgs>(args =>
            {
                if (Editor != null)
                {
                    Editor.AddLogMessage("Package Manager", args.Messege, DateTime.Now, -1, null, Errors.Ok);
                }
            });

            // NOTE: Removing PackageOperationManager dependency - all operations are now handled directly
            // Initialize specialized managers without the redundant PackageOperationManager
            _requirementsManager = new RequirementsFileManager(beepService, pythonRuntime, this, Progress);
            _categoryManager = new PackageCategoryManager(beepService, this, Progress);
            _packageSetManager = new PackageSetManager(beepService, this, _requirementsManager, Progress);
        }
        #endregion

        #region Session and Environment Configuration
        /// <summary>
        /// Configure the package manager to use a specific Python session and virtual environment
        /// This is the recommended approach for multi-user environments
        /// </summary>
        /// <param name="session">Pre-existing Python session to use for execution</param>
        /// <param name="virtualEnvironment">Virtual environment associated with the session</param>
        /// <returns>True if configuration successful</returns>
        public bool ConfigureSession(PythonSessionInfo session, PythonVirtualEnvironment virtualEnvironment)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            if (virtualEnvironment == null)
                throw new ArgumentNullException(nameof(virtualEnvironment));

            // Validate that session is associated with the environment
            if (session.VirtualEnvironmentId != virtualEnvironment.ID)
            {
                throw new ArgumentException("Session must be associated with the provided virtual environment");
            }

            // Validate session is active
            if (session.Status != PythonSessionStatus.Active)
            {
                throw new ArgumentException("Session must be in Active status");
            }

            _configuredSession = session;
            _configuredVirtualEnvironment = virtualEnvironment;

            // Get or create the session scope
            if (_pythonRuntime.HasScope(session))
            {
                _sessionScope = _pythonRuntime.GetScope(session);
            }
            else
            {
                if (_pythonRuntime.CreateScope(session, virtualEnvironment))
                {
                    _sessionScope = _pythonRuntime.GetScope(session);
                }
                else
                {
                    throw new InvalidOperationException("Failed to create Python scope for session");
                }
            }

            // Configure specialized managers with the session
            _requirementsManager.ConfigureSession(session, virtualEnvironment);
            _categoryManager.ConfigureSession(session, virtualEnvironment);

            // Initialize package management environment for this session
            InitializePackageEnvironment();

            // Initialize unit of work with the environment's packages
            InitializeUnitOfWork();

            return true;
        }

        /// <summary>
        /// Configure session using username and optional environment ID
        /// This method will create or reuse a session for the specified user
        /// </summary>
        /// <param name="username">Username for session creation</param>
        /// <param name="environmentId">Specific environment ID, or null for auto-selection</param>
        /// <returns>True if configuration successful</returns>
        public bool ConfigureSessionForUser(string username, string? environmentId = null)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("Username cannot be null or empty", nameof(username));

            if (_sessionManager == null)
                throw new InvalidOperationException("Session manager is not available");

            // Create or get existing session for the user
            var session = _sessionManager.CreateSession(username, environmentId);
            if (session == null)
            {
                throw new InvalidOperationException($"Failed to create session for user: {username}");
            }

            // Get the virtual environment for this session
            var virtualEnvironment = _virtualEnvManager?.GetEnvironmentById(session.VirtualEnvironmentId);
            if (virtualEnvironment == null)
            {
                throw new InvalidOperationException($"Virtual environment not found for session: {session.SessionId}");
            }

            return ConfigureSession(session, virtualEnvironment);
        }

        /// <summary>
        /// Legacy constructor compatibility - sets the active session and environment for package operations
        /// Use ConfigureSession or ConfigureSessionForUser for better session management
        /// </summary>
        public void SetActiveSessionAndEnvironment(PythonSessionInfo session, PythonVirtualEnvironment environment)
        {
            ConfigureSession(session, environment);
        }

        /// <summary>
        /// Get the currently configured session, if any
        /// </summary>
        /// <returns>The configured Python session, or null if not configured</returns>
        public PythonSessionInfo? GetConfiguredSession()
        {
            return _configuredSession;
        }

        /// <summary>
        /// Get the currently configured virtual environment, if any
        /// </summary>
        /// <returns>The configured virtual environment, or null if not configured</returns>
        public PythonVirtualEnvironment? GetConfiguredVirtualEnvironment()
        {
            return _configuredVirtualEnvironment;
        }

        /// <summary>
        /// Check if session is properly configured
        /// </summary>
        /// <returns>True if session and environment are configured</returns>
        public bool IsSessionConfigured()
        {
            return _configuredSession != null && _configuredVirtualEnvironment != null && _sessionScope != null;
        }

        private void InitializePackageEnvironment()
        {
            if (!IsSessionConfigured())
                throw new InvalidOperationException("Session must be configured before initializing package environment");

            try
            {
                ExecuteInSession(() =>
                {
                    // Essential package management imports with error handling
                    string initScript = @"
import sys
import subprocess
import os
import traceback
try:
    import pip
    print('Package management environment initialized successfully')
except ImportError as e:
    print(f'Package management initialization warning: {e}')
except Exception as e:
    print(f'Package management initialization error: {e}')
    traceback.print_exc()
";
                    _sessionScope!.Exec(initScript);
                });
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to initialize package management environment: {ex.Message}", ex);
            }
        }

        private void InitializeUnitOfWork()
        {
            if (Editor != null && _configuredVirtualEnvironment?.InstalledPackages != null)
            {
                UnitofWork = new UnitofWork<PackageDefinition>(Editor, true, _configuredVirtualEnvironment.InstalledPackages);
            }
        }

        private bool ValidateSessionAndEnvironment()
        {
            if (!IsSessionConfigured())
            {
                ReportError("Session and environment must be configured before performing package operations.");
                return false;
            }

            return true;
        }
        #endregion

        #region Core Helper Methods
        /// <summary>
        /// Executes code safely within the session context without manual GIL management
        /// </summary>
        /// <param name="action">Action to execute in session</param>
        private void ExecuteInSession(Action action)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(PythonPackageManager));

            if (!IsSessionConfigured())
                throw new InvalidOperationException("Session must be configured before executing package operations");

            lock (_operationLock)
            {
                try
                {
                    // Let the runtime manager handle GIL management through the session scope
                    action();
                }
                catch (PythonException pythonEx)
                {
                    throw new InvalidOperationException($"Python execution error: {pythonEx.Message}", pythonEx);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Session execution error: {ex.Message}", ex);
                }
            }
        }

        /// <summary>
        /// Executes code safely within the session context and returns a result without manual GIL management
        /// </summary>
        /// <typeparam name="T">Type of result to return</typeparam>
        /// <param name="func">Function to execute in session</param>
        /// <returns>Result of the function</returns>
        private T ExecuteInSession<T>(Func<T> func)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(PythonPackageManager));

            if (!IsSessionConfigured())
                throw new InvalidOperationException("Session must be configured before executing package operations");

            lock (_operationLock)
            {
                try
                {
                    // Let the runtime manager handle GIL management through the session scope
                    return func();
                }
                catch (PythonException pythonEx)
                {
                    throw new InvalidOperationException($"Python execution error: {pythonEx.Message}", pythonEx);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Session execution error: {ex.Message}", ex);
                }
            }
        }
        #endregion

        #region Core Package Management Operations (Consolidated from PackageOperationManager)
        /// <summary>
        /// Runs a package management command in the configured environment
        /// </summary>
        public async Task<string> RunPackageCommandAsync(
            string command,
            PackageAction action,
            PythonVirtualEnvironment environment,
            bool useConda = false)
        {
            if (environment == null)
            {
                ReportError("No environment specified for package operation");
                return string.Empty;
            }

            try
            {
                // Use configured session or get package management session
                var session = _configuredSession ?? _virtualEnvManager.GetPackageManagementSession(environment);
                if (session == null)
                {
                    ReportError("Failed to obtain session for package management");
                    return string.Empty;
                }

                // Build the command 
                string packageCommand = BuildPackageCommand(command, action, useConda);

                // Execute the command
                var result = await _pythonRuntime.ExecuteManager.RunPythonCommandLineAsync(
                    Progress,
                    packageCommand,
                    useConda || environment.PythonBinary == PythonBinary.Conda,
                    session,
                    environment);

                return result ?? string.Empty;
            }
            catch (Exception ex)
            {
                ReportError($"Error executing package command '{command}': {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Installs a package in the specified environment with session support
        /// </summary>
        public async Task<bool> InstallPackageAsync(string packageName, PythonVirtualEnvironment environment)
        {
            if (string.IsNullOrEmpty(packageName))
            {
                ReportError("Package name cannot be empty");
                return false;
            }

            try
            {
                ReportProgress($"Installing package: {packageName}");

                string result = await RunPackageCommandAsync(
                    packageName,
                    PackageAction.Install,
                    environment,
                    environment.PythonBinary == PythonBinary.Conda);

                bool success = !string.IsNullOrEmpty(result) && !result.Contains("ERROR:");

                if (success)
                {
                    ReportProgress($"Successfully installed package: {packageName}");
                }
                else
                {
                    ReportError($"Failed to install package: {packageName}");
                }

                return success;
            }
            catch (Exception ex)
            {
                ReportError($"Failed to install package {packageName}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Uninstalls a package from the specified environment with session support
        /// </summary>
        public async Task<bool> UninstallPackageAsync(string packageName, PythonVirtualEnvironment environment)
        {
            if (string.IsNullOrEmpty(packageName))
            {
                ReportError("Package name cannot be empty");
                return false;
            }

            try
            {
                ReportProgress($"Uninstalling package: {packageName}");

                string result = await RunPackageCommandAsync(
                    packageName,
                    PackageAction.Remove,
                    environment,
                    environment.PythonBinary == PythonBinary.Conda);

                bool success = !string.IsNullOrEmpty(result) && !result.Contains("ERROR:");

                if (success)
                {
                    ReportProgress($"Successfully uninstalled package: {packageName}");
                }
                else
                {
                    ReportError($"Failed to uninstall package: {packageName}");
                }

                return success;
            }
            catch (Exception ex)
            {
                ReportError($"Failed to uninstall package {packageName}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Upgrades a package in the specified environment with session support
        /// </summary>
        public async Task<bool> UpgradePackageAsync(string packageName, PythonVirtualEnvironment environment)
        {
            if (string.IsNullOrEmpty(packageName))
            {
                ReportError("Package name cannot be empty");
                return false;
            }

            try
            {
                ReportProgress($"Upgrading package: {packageName}");

                // Special case for pip itself
                PackageAction action = packageName.Equals("pip", StringComparison.OrdinalIgnoreCase)
                    ? PackageAction.UpgradePackager
                    : PackageAction.Update;

                string result = await RunPackageCommandAsync(
                    packageName,
                    action,
                    environment,
                    environment.PythonBinary == PythonBinary.Conda);

                bool success = !string.IsNullOrEmpty(result) && !result.Contains("ERROR:");

                if (success)
                {
                    ReportProgress($"Successfully upgraded package: {packageName}");
                }
                else
                {
                    ReportError($"Failed to upgrade package: {packageName}");
                }

                return success;
            }
            catch (Exception ex)
            {
                ReportError($"Failed to upgrade package {packageName}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets information about an installed package with session support
        /// </summary>
        public async Task<PackageDefinition> GetPackageInfoAsync(string packageName, PythonVirtualEnvironment environment)
        {
            if (string.IsNullOrEmpty(packageName) || environment == null)
            {
                return null;
            }

            try
            {
                // Use configured session or get package management session
                var session = _configuredSession ?? _virtualEnvManager.GetPackageManagementSession(environment);
                if (session == null)
                {
                    ReportError("Failed to obtain session for package info");
                    return null;
                }

                // Get package info using session-aware execution
                var packageInfo = await ExecutePackageInfoScriptAsync(packageName, session);
                if (packageInfo != null)
                {
                    // Check online for latest version
                    var onlinePackage = await CheckIfPackageExistsAsync(packageName);

                    packageInfo.Updateversion = onlinePackage?.Version ?? packageInfo.Version;
                    packageInfo.Buttondisplay = DetermineButtonDisplay(packageInfo.Version, onlinePackage?.Version);
                }

                return packageInfo;
            }
            catch (Exception ex)
            {
                ReportError($"Failed to get package info for {packageName}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets information about all installed packages with session support
        /// </summary>
        public async Task<List<PackageDefinition>> GetAllPackagesAsync(PythonVirtualEnvironment environment)
        {
            var packages = new List<PackageDefinition>();

            if (environment == null)
            {
                ReportError("Environment cannot be null");
                return packages;
            }

            try
            {
                // Use configured session or get package management session
                var session = _configuredSession ?? _virtualEnvManager.GetPackageManagementSession(environment);
                if (session == null)
                {
                    ReportError("Failed to obtain session for package listing");
                    return packages;
                }

                ReportProgress("Retrieving installed packages...");

                // Get all packages using session-aware execution
                var packageList = await ExecutePackageListScriptAsync(session);
                if (packageList != null && packageList.Any())
                {
                    bool isInternetAvailable = PythonRunTimeDiagnostics.CheckNet();
                    ReportProgress($"Found {packageList.Count} packages. Checking for updates...");

                    // Process packages in batches for better performance
                    int batchSize = 10;
                    for (int i = 0; i < packageList.Count; i += batchSize)
                    {
                        var batch = packageList.Skip(i).Take(batchSize).ToList();

                        foreach (var packageInfo in batch)
                        {
                            if (!string.IsNullOrEmpty(packageInfo.PackageName))
                            {
                                ReportProgress($"Processing package {packageInfo.PackageName} ({i + 1}/{packageList.Count})");

                                // Check online for latest version if internet is available
                                if (isInternetAvailable)
                                {
                                    var onlinePackage = await CheckIfPackageExistsAsync(packageInfo.PackageName);
                                    if (onlinePackage != null)
                                    {
                                        packageInfo.Updateversion = onlinePackage.Version;
                                        packageInfo.Buttondisplay = DetermineButtonDisplay(packageInfo.Version, onlinePackage.Version);
                                    }
                                }

                                packages.Add(packageInfo);
                            }
                        }

                        // Allow UI to update between batches
                        await Task.Delay(1);
                    }
                }

                ReportProgress($"Completed package retrieval. Found {packages.Count} packages.");
            }
            catch (Exception ex)
            {
                ReportError($"Error getting packages: {ex.Message}");
            }

            return packages;
        }

        /// <summary>
        /// Checks the PyPI repository for information about a package
        /// </summary>
        public async Task<PackageDefinition> CheckIfPackageExistsAsync(string packageName)
        {
            if (string.IsNullOrEmpty(packageName))
                return null;

            try
            {
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
                {
                    HttpResponseMessage response = await _httpClient.GetAsync(
                        $"https://pypi.org/pypi/{packageName}/json",
                        cts.Token);

                    if (response.IsSuccessStatusCode)
                    {
                        string jsonResponse = await response.Content.ReadAsStringAsync();
                        dynamic packageData = Newtonsoft.Json.JsonConvert.DeserializeObject(jsonResponse);

                        PackageDefinition packageInfo = new PackageDefinition
                        {
                            PackageName = packageName,
                            Version = packageData.info.version,
                            Description = packageData.info.summary ?? packageData.info.description,
                            Status = PackageStatus.Available
                        };

                        return packageInfo;
                    }
                }
            }
            catch (HttpRequestException)
            {
                // Connection error - be silent
            }
            catch (TaskCanceledException)
            {
                // Timeout - be silent
            }
            catch (Exception ex)
            {
                ReportError($"Error checking package {packageName}: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Executes package info script using the configured session
        /// </summary>
        private async Task<PackageDefinition> ExecutePackageInfoScriptAsync(string packageName, PythonSessionInfo session)
        {
            var packageInfoScript = $@"
import json
import importlib.metadata

try:
    dist = importlib.metadata.distribution('{packageName}')
    package_info = {{
        'name': dist.metadata['Name'],
        'version': dist.version,
        'summary': dist.metadata.get('Summary', ''),
        'location': str(dist.locate_file(''))
    }}
    print(json.dumps(package_info))
except importlib.metadata.PackageNotFoundError:
    print(json.dumps({{'error': 'Package not found'}}))
except Exception as e:
    print(json.dumps({{'error': str(e)}}))
";

            try
            {
                var output = await _pythonRuntime.ExecuteManager.RunPythonCodeAndGetOutput(
                    Progress,
                    packageInfoScript,
                    session);

                if (!string.IsNullOrEmpty(output) && !output.Contains("error"))
                {
                    var packageInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(output);
                    if (packageInfo != null && packageInfo.ContainsKey("name"))
                    {
                        return new PackageDefinition
                        {
                            PackageName = packageInfo["name"],
                            Version = packageInfo["version"],
                            Description = packageInfo.ContainsKey("summary") ? packageInfo["summary"] : "",
                            Installpath = packageInfo.ContainsKey("location") ? packageInfo["location"] : "",
                            Status = PackageStatus.Installed
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                ReportError($"Error executing package info script for {packageName}: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Executes package list script using the configured session
        /// </summary>
        private async Task<List<PackageDefinition>> ExecutePackageListScriptAsync(PythonSessionInfo session)
        {
            var packageListScript = @"
import json
import importlib.metadata

try:
    packages = []
    for dist in importlib.metadata.distributions():
        try:
            package = {
                'name': dist.metadata['Name'],
                'version': dist.version,
                'summary': dist.metadata.get('Summary', ''),
                'location': str(dist.locate_file(''))
            }
            packages.append(package)
        except (KeyError, Exception):
            pass
    
    print(json.dumps(packages))
except Exception as e:
    print(json.dumps({'error': str(e)}))
";

            try
            {
                var output = await _pythonRuntime.ExecuteManager.RunPythonCodeAndGetOutput(
                    Progress,
                    packageListScript,
                    session);

                if (!string.IsNullOrEmpty(output) && !output.Contains("error"))
                {
                    var packageList = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(output);
                    if (packageList != null)
                    {
                        return packageList.Where(p => p.ContainsKey("name") && p.ContainsKey("version"))
                            .Select(p => new PackageDefinition
                            {
                                PackageName = p["name"],
                                Version = p["version"],
                                Description = p.ContainsKey("summary") ? p["summary"] : "",
                                Installpath = p.ContainsKey("location") ? p["location"] : "",
                                Status = PackageStatus.Installed,
                                Buttondisplay = "Status"
                            }).ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                ReportError($"Error executing package list script: {ex.Message}");
            }

            return new List<PackageDefinition>();
        }

        /// <summary>
        /// Builds a package management command for pip or conda
        /// </summary>
        private string BuildPackageCommand(string packageName, PackageAction action, bool useConda)
        {
            if (useConda)
            {
                switch (action)
                {
                    case PackageAction.Install:
                        return $"install -c conda-forge {packageName}";
                    case PackageAction.Remove:
                        return $"remove {packageName}";
                    case PackageAction.Update:
                        return $"update {packageName}";
                    case PackageAction.UpgradePackager:
                        return $"update conda";
                    default:
                        return packageName;
                }
            }
            else
            {
                switch (action)
                {
                    case PackageAction.Install:
                        return $"install -U {packageName}";
                    case PackageAction.Remove:
                        return $"uninstall -y {packageName}";
                    case PackageAction.Update:
                        return $"install --upgrade {packageName}";
                    case PackageAction.UpgradePackager:
                        return $"install --upgrade pip";
                    default:
                        return packageName;
                }
            }
        }

        /// <summary>
        /// Determines if a package needs an update based on version comparison
        /// </summary>
        private string DetermineButtonDisplay(string currentVersion, string onlineVersion)
        {
            if (string.IsNullOrEmpty(onlineVersion))
                return "Status";

            try
            {
                if (Version.TryParse(currentVersion, out var current) &&
                    Version.TryParse(onlineVersion, out var online))
                {
                    return online > current ? "Update" : "Status";
                }
            }
            catch
            {
                // Ignore parsing errors
            }

            return "Status";
        }
        #endregion

        #region Package Management Core Methods (Public Interface)
        /// <summary>
        /// Installs a new package in the current environment with enhanced session support
        /// </summary>
        public bool InstallNewPackageAsync(string packageName)
        {
            if (string.IsNullOrEmpty(packageName) || _isBusy || !ValidateSessionAndEnvironment())
                return false;

            _isBusy = true;
            try
            {
                var task = InstallPackageAsync(packageName, _configuredVirtualEnvironment!);
                task.Wait();
                bool result = task.Result;

                if (result)
                {
                    // If installation was successful, refresh package information
                    RefreshPackageAsync(packageName);
                }

                return result;
            }
            catch (Exception ex)
            {
                ReportError($"Failed to install package {packageName}: {ex.Message}");
                return false;
            }
            finally
            {
                _isBusy = false;
            }
        }

        /// <summary>
        /// Installs a new package asynchronously with session support
        /// </summary>
        public async Task<bool> InstallNewPackageWithSessionAsync(string packageName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(packageName) || _isBusy || !ValidateSessionAndEnvironment())
                return false;

            _isBusy = true;
            try
            {
                bool result = await InstallPackageAsync(packageName, _configuredVirtualEnvironment!);

                if (result)
                {
                    // If installation was successful, refresh package information
                    await RefreshPackageWithSessionAsync(packageName, cancellationToken);
                }

                return result;
            }
            catch (Exception ex)
            {
                ReportError($"Failed to install package {packageName}: {ex.Message}");
                return false;
            }
            finally
            {
                _isBusy = false;
            }
        }

        /// <summary>
        /// Installs or upgrades the pip package manager
        /// </summary>
        public bool InstallPipToolAsync()
        {
            if (_isBusy || !ValidateSessionAndEnvironment())
                return false;

            _isBusy = true;
            try
            {
                var task = UpgradePackageAsync("pip", _configuredVirtualEnvironment!);
                task.Wait();
                return task.Result;
            }
            catch (Exception ex)
            {
                ReportError($"Failed to install/upgrade pip tool: {ex.Message}");
                return false;
            }
            finally
            {
                _isBusy = false;
            }
        }

        /// <summary>
        /// Refreshes information for all packages in the current environment
        /// </summary>
        public bool RefreshAllPackagesAsync()
        {
            if (_isBusy || !ValidateSessionAndEnvironment())
                return false;

            _isBusy = true;
            try
            {
                var task = GetAllPackagesAsync(_configuredVirtualEnvironment!);
                task.Wait();
                var packages = task.Result;

                if (packages != null)
                {
                    SynchronizePackages(packages);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                ReportError($"Failed to refresh packages: {ex.Message}");
                return false;
            }
            finally
            {
                _isBusy = false;
            }
        }

        /// <summary>
        /// Refreshes information for all packages asynchronously with session support
        /// </summary>
        public async Task<bool> RefreshAllPackagesWithSessionAsync(CancellationToken cancellationToken = default)
        {
            if (_isBusy || !ValidateSessionAndEnvironment())
                return false;

            _isBusy = true;
            try
            {
                var packages = await GetAllPackagesAsync(_configuredVirtualEnvironment!);

                if (packages != null)
                {
                    SynchronizePackages(packages);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                ReportError($"Failed to refresh packages: {ex.Message}");
                return false;
            }
            finally
            {
                _isBusy = false;
            }
        }

        /// <summary>
        /// Refreshes information for a specific package in the current environment
        /// </summary>
        public bool RefreshPackageAsync(string packageName)
        {
            if (string.IsNullOrEmpty(packageName) || _isBusy || !ValidateSessionAndEnvironment())
                return false;

            _isBusy = true;
            try
            {
                var task = GetPackageInfoAsync(packageName, _configuredVirtualEnvironment!);
                task.Wait();
                var packageInfo = task.Result;

                if (packageInfo != null)
                {
                    UpdatePackageInEnvironment(packageInfo);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                ReportError($"Failed to refresh package {packageName}: {ex.Message}");
                return false;
            }
            finally
            {
                _isBusy = false;
            }
        }

        /// <summary>
        /// Refreshes information for a specific package asynchronously with session support
        /// </summary>
        public async Task<bool> RefreshPackageWithSessionAsync(string packageName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(packageName) || _isBusy || !ValidateSessionAndEnvironment())
                return false;

            _isBusy = true;
            try
            {
                var packageInfo = await GetPackageInfoAsync(packageName, _configuredVirtualEnvironment!);

                if (packageInfo != null)
                {
                    UpdatePackageInEnvironment(packageInfo);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                ReportError($"Failed to refresh package {packageName}: {ex.Message}");
                return false;
            }
            finally
            {
                _isBusy = false;
            }
        }

        /// <summary>
        /// Uninstalls a package from the current environment
        /// </summary>
        public bool UnInstallPackageAsync(string packageName)
        {
            if (string.IsNullOrEmpty(packageName) || _isBusy || !ValidateSessionAndEnvironment())
                return false;

            _isBusy = true;
            try
            {
                var task = UninstallPackageAsync(packageName, _configuredVirtualEnvironment!);
                task.Wait();
                bool result = task.Result;

                if (result)
                {
                    RemovePackageFromEnvironment(packageName);
                }

                return result;
            }
            catch (Exception ex)
            {
                ReportError($"Failed to uninstall package {packageName}: {ex.Message}");
                return false;
            }
            finally
            {
                _isBusy = false;
            }
        }

        /// <summary>
        /// Uninstalls a package asynchronously with session support
        /// </summary>
        public async Task<bool> UnInstallPackageWithSessionAsync(string packageName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(packageName) || _isBusy || !ValidateSessionAndEnvironment())
                return false;

            _isBusy = true;
            try
            {
                bool result = await UninstallPackageAsync(packageName, _configuredVirtualEnvironment!);

                if (result)
                {
                    RemovePackageFromEnvironment(packageName);
                }

                return result;
            }
            catch (Exception ex)
            {
                ReportError($"Failed to uninstall package {packageName}: {ex.Message}");
                return false;
            }
            finally
            {
                _isBusy = false;
            }
        }

        /// <summary>
        /// Upgrades all packages in the current environment
        /// </summary>
        public bool UpgradeAllPackagesAsync()
        {
            if (_isBusy || !ValidateSessionAndEnvironment())
                return false;

            _isBusy = true;
            try
            {
                // First refresh packages to get current info
                var refreshTask = GetAllPackagesAsync(_configuredVirtualEnvironment!);
                refreshTask.Wait();
                var packages = refreshTask.Result;

                if (packages == null)
                {
                    return false;
                }

                // Find packages that need updates
                var packagesToUpdate = packages
                    .Where(p => p.Buttondisplay == "Update" ||
                              (p.Updateversion != null && p.Version != null &&
                               p.Updateversion != p.Version))
                    .ToList();

                if (packagesToUpdate.Count == 0)
                {
                    ReportProgress("No packages need upgrading.");
                    return true;
                }

                // Upgrade each package
                bool allSucceeded = true;
                for (int i = 0; i < packagesToUpdate.Count; i++)
                {
                    var pkg = packagesToUpdate[i];
                    ReportProgress($"Upgrading {pkg.PackageName} ({i + 1}/{packagesToUpdate.Count}) from {pkg.Version} to {pkg.Updateversion}");

                    var upgradeTask = UpgradePackageAsync(pkg.PackageName, _configuredVirtualEnvironment!);
                    upgradeTask.Wait();

                    if (!upgradeTask.Result)
                    {
                        allSucceeded = false;
                        ReportError($"Failed to upgrade {pkg.PackageName}");
                    }
                }

                // Final refresh to confirm upgrades
                RefreshAllPackagesAsync();
                return allSucceeded;
            }
            catch (Exception ex)
            {
                ReportError($"Failed to upgrade all packages: {ex.Message}");
                return false;
            }
            finally
            {
                _isBusy = false;
            }
        }

        /// <summary>
        /// Upgrades a specific package in the current environment
        /// </summary>
        public bool UpgradePackageAsync(string packageName)
        {
            if (string.IsNullOrEmpty(packageName) || _isBusy || !ValidateSessionAndEnvironment())
                return false;

            _isBusy = true;
            try
            {
                var task = UpgradePackageAsync(packageName, _configuredVirtualEnvironment!);
                task.Wait();
                bool result = task.Result;

                if (result)
                {
                    // Refresh the package information
                    RefreshPackageAsync(packageName);
                }

                return result;
            }
            catch (Exception ex)
            {
                ReportError($"Failed to upgrade package {packageName}: {ex.Message}");
                return false;
            }
            finally
            {
                _isBusy = false;
            }
        }

        /// <summary>
        /// Helper method to update or add package in the environment's installed packages list
        /// </summary>
        private void UpdatePackageInEnvironment(PackageDefinition packageInfo)
        {
            if (_configuredVirtualEnvironment?.InstalledPackages == null)
                return;

            var existingPackage = _configuredVirtualEnvironment.InstalledPackages.FirstOrDefault(p =>
                p.PackageName != null &&
                p.PackageName.Equals(packageInfo.PackageName, StringComparison.OrdinalIgnoreCase));

            if (existingPackage != null)
            {
                // Update existing package properties
                existingPackage.Version = packageInfo.Version;
                existingPackage.Updateversion = packageInfo.Updateversion;
                existingPackage.Status = packageInfo.Status;
                existingPackage.Buttondisplay = packageInfo.Buttondisplay;
                existingPackage.Description = packageInfo.Description;
                existingPackage.Installpath = packageInfo.Installpath;
            }
            else
            {
                // Add new package
                _configuredVirtualEnvironment.InstalledPackages.Add(packageInfo);
            }
        }

        /// <summary>
        /// Helper method to remove package from the environment's installed packages list
        /// </summary>
        private void RemovePackageFromEnvironment(string packageName)
        {
            if (_configuredVirtualEnvironment?.InstalledPackages == null)
                return;

            var packageToRemove = _configuredVirtualEnvironment.InstalledPackages.FirstOrDefault(p =>
                p.PackageName != null &&
                p.PackageName.Equals(packageName, StringComparison.OrdinalIgnoreCase));

            if (packageToRemove != null)
            {
                _configuredVirtualEnvironment.InstalledPackages.Remove(packageToRemove);
            }
        }

        /// <summary>
        /// Synchronizes the environment's package list with a source list
        /// </summary>
        private void SynchronizePackages(List<PackageDefinition> sourcePackages)
        {
            if (sourcePackages == null || _configuredVirtualEnvironment == null)
                return;

            // Initialize packages collection if needed
            if (_configuredVirtualEnvironment.InstalledPackages == null)
            {
                _configuredVirtualEnvironment.InstalledPackages = new ObservableBindingList<PackageDefinition>();
            }
            else
            {
                // Clear existing packages
                _configuredVirtualEnvironment.InstalledPackages.Clear();
            }

            // Add all packages from source
            foreach (var package in sourcePackages)
            {
                _configuredVirtualEnvironment.InstalledPackages.Add(package);
            }

            // Re-initialize unit of work with updated packages
            InitializeUnitOfWork();
        }
        #endregion

        #region Requirements File Management
        /// <summary>
        /// Installs packages from a requirements file into the current environment
        /// </summary>
        public bool InstallFromRequirementsFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath) || _isBusy || !ValidateSessionAndEnvironment())
                return false;

            _isBusy = true;
            try
            {
                var task = _requirementsManager.InstallFromRequirementsFileAsync(filePath, _configuredVirtualEnvironment!);
                task.Wait();
                return task.Result;
            }
            catch (Exception ex)
            {
                ReportError($"Error installing packages from requirements file: {ex.Message}");
                return false;
            }
            finally
            {
                _isBusy = false;
            }
        }

        /// <summary>
        /// Installs packages from a requirements file asynchronously with session support
        /// </summary>
        public async Task<bool> InstallFromRequirementsFileWithSessionAsync(string filePath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath) || _isBusy || !ValidateSessionAndEnvironment())
                return false;

            _isBusy = true;
            try
            {
                return await _requirementsManager.InstallFromRequirementsFileAsync(filePath, _configuredVirtualEnvironment!);
            }
            catch (Exception ex)
            {
                ReportError($"Error installing packages from requirements file: {ex.Message}");
                return false;
            }
            finally
            {
                _isBusy = false;
            }
        }

        /// <summary>
        /// Generates a requirements file from the packages installed in the current environment
        /// </summary>
        public bool GenerateRequirementsFile(string filePath, bool includeVersions = true)
        {
            if (string.IsNullOrEmpty(filePath) || _isBusy || !ValidateSessionAndEnvironment())
                return false;

            _isBusy = true;
            try
            {
                var task = _requirementsManager.GenerateRequirementsFileAsync(filePath, _configuredVirtualEnvironment!, includeVersions);
                task.Wait();
                return task.Result;
            }
            catch (Exception ex)
            {
                ReportError($"Error generating requirements file: {ex.Message}");
                return false;
            }
            finally
            {
                _isBusy = false;
            }
        }

        /// <summary>
        /// Updates a virtual environment with packages from its requirements file
        /// </summary>
        public bool UpdateVirtualEnvironmentWithRequirementsFile(PythonVirtualEnvironment environment)
        {
            if (environment == null || _isBusy)
                return false;

            _isBusy = true;
            try
            {
                var task = _requirementsManager.UpdateEnvironmentWithRequirementsFileAsync(environment);
                task.Wait();
                return task.Result;
            }
            catch (Exception ex)
            {
                ReportError($"Error updating environment from requirements file: {ex.Message}");
                return false;
            }
            finally
            {
                _isBusy = false;
            }
        }
        #endregion

        #region Package Category Management
        /// <summary>
        /// Gets packages in a specific category from the current environment
        /// </summary>
        public ObservableBindingList<PackageDefinition> GetPackagesByCategory(PackageCategory category)
        {
            if (_configuredVirtualEnvironment?.InstalledPackages == null)
            {
                return new ObservableBindingList<PackageDefinition>();
            }

            var filtered = new ObservableBindingList<PackageDefinition>();
            foreach (var package in _categoryManager.GetPackagesByCategory(_configuredVirtualEnvironment, category))
            {
                filtered.Add(package);
            }

            return filtered;
        }

        /// <summary>
        /// Sets the category for a specific package in the current environment
        /// </summary>
        public void SetPackageCategory(string packageName, PackageCategory category)
        {
            if (string.IsNullOrEmpty(packageName) || !ValidateSessionAndEnvironment())
                return;

            _categoryManager.SetPackageCategory(_configuredVirtualEnvironment!, packageName, category);
        }

        /// <summary>
        /// Updates categories for multiple packages at once
        /// </summary>
        public void UpdatePackageCategories(Dictionary<string, PackageCategory> packageCategories)
        {
            if (packageCategories == null || packageCategories.Count == 0 || !ValidateSessionAndEnvironment())
                return;

            _categoryManager.UpdatePackageCategories(_configuredVirtualEnvironment!, packageCategories);
        }

        /// <summary>
        /// Populates common package categories based on known package names
        /// </summary>
        public bool PopulateCommonPackageCategories()
        {
            if (!ValidateSessionAndEnvironment())
                return false;

            return _categoryManager.PopulateCommonPackageCategories(_configuredVirtualEnvironment!);
        }

        /// <summary>
        /// Suggests categories for packages based on their names and descriptions
        /// </summary>
        public async Task<Dictionary<string, PackageCategory>> SuggestCategoriesForPackages(IEnumerable<string> packageNames)
        {
            if (packageNames == null || !packageNames.Any() || !ValidateSessionAndEnvironment())
                return new Dictionary<string, PackageCategory>();

            return await _categoryManager.SuggestCategoriesForPackagesAsync(packageNames, _configuredVirtualEnvironment!);
        }
        #endregion

        #region Package Set Management
        /// <summary>
        /// Installs all packages from a predefined package set
        /// </summary>
        public bool InstallPackageSet(string setName)
        {
            if (string.IsNullOrEmpty(setName) || _isBusy || !ValidateSessionAndEnvironment())
                return false;

            _isBusy = true;
            try
            {
                var task = _packageSetManager.InstallPackageSetAsync(setName, _configuredVirtualEnvironment!);
                task.Wait();

                // Refresh packages after installation
                if (task.Result)
                {
                    RefreshAllPackagesAsync();
                }

                return task.Result;
            }
            catch (Exception ex)
            {
                ReportError($"Error installing package set: {ex.Message}");
                return false;
            }
            finally
            {
                _isBusy = false;
            }
        }

        /// <summary>
        /// Gets a dictionary of available package set names and their package lists
        /// </summary>
        public Dictionary<string, List<PackageDefinition>> GetAvailablePackageSets()
        {
            return _packageSetManager.GetAvailablePackageSets();
        }

        /// <summary>
        /// Creates a new package set from the currently installed packages
        /// </summary>
        public bool SavePackageSetFromCurrentEnvironment(string setName, string description = "")
        {
            if (string.IsNullOrEmpty(setName) || !ValidateSessionAndEnvironment())
                return false;

            _isBusy = true;
            try
            {
                var task = _packageSetManager.SavePackageSetFromEnvironmentAsync(
                    setName, _configuredVirtualEnvironment!, description);
                task.Wait();
                return task.Result;
            }
            catch (Exception ex)
            {
                ReportError($"Error saving package set: {ex.Message}");
                return false;
            }
            finally
            {
                _isBusy = false;
            }
        }
        #endregion

        #region Logging and Error Handling
        private void ReportProgress(string message)
        {
            Progress?.Report(new PassedArgs
            {
                Messege = message,
                EventType = "Info"
            });

            // Also log to editor if available
            Editor?.AddLogMessage("Package Manager", message, DateTime.Now, -1, null, Errors.Ok);
        }

        private void ReportError(string message)
        {
            Progress?.Report(new PassedArgs
            {
                Messege = message,
                EventType = "Error",
                Flag = Errors.Failed
            });

            // Also log to editor if available
            Editor?.AddLogMessage("Package Manager", message, DateTime.Now, -1, null, Errors.Failed);
        }
        #endregion

        #region IDisposable Implementation
        /// <summary>
        /// Disposes resources used by the package manager
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Protected implementation of Dispose pattern
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed && disposing)
            {
                try
                {
                    _cancellationTokenSource?.Cancel();
                    _cancellationTokenSource?.Dispose();
                    _httpClient?.Dispose();
                    UnitofWork = null;
                }
                catch (Exception ex)
                {
                    // Log disposal errors but don't throw
                    Console.WriteLine($"Warning during disposal: {ex.Message}");
                }
                finally
                {
                    _isDisposed = true;
                }
            }
        }
        #endregion
    }
}
