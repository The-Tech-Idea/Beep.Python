using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Beep.Python.Model;
using Beep.Python.RuntimeEngine.Helpers;
using Python.Runtime;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Container.Services;
using TheTechIdea.Beep.Editor;

namespace Beep.Python.RuntimeEngine.PackageManagement
{
    /// <summary>
    /// Main manager for Python package operations. Coordinates specialized managers for different package-related functions.
    /// </summary>
    public class PythonPackageManager : IPythonPackageManager
    {
        #region Fields
        private readonly IBeepService _beepService;
        private readonly IPythonRunTimeManager _pythonRuntime;
        private readonly IPythonVirtualEnvManager _virtualEnvManager;
        private readonly IPythonSessionManager _sessionManager;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly PackageOperationManager _packageOperations;
        private readonly RequirementsFileManager _requirementsManager;
        private readonly PackageCategoryManager _categoryManager;
        private readonly PackageSetManager _packageSetManager;

        private bool _isBusy;
        private bool _isDisposed;
        private PythonSessionInfo _currentSession;
        private PythonVirtualEnvironment _currentEnvironment;
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
        /// Initializes a new instance of the PythonPackageManager class
        /// </summary>
        public PythonPackageManager(
            IBeepService beepService,
            IPythonRunTimeManager pythonRuntime,
            IPythonVirtualEnvManager virtualEnvManager,
            PythonVirtualEnvironment environment,
            PythonSessionInfo session,
            IPythonSessionManager sessionManager)
        {
            _beepService = beepService ?? throw new ArgumentNullException(nameof(beepService));
            _pythonRuntime = pythonRuntime ?? throw new ArgumentNullException(nameof(pythonRuntime));
            _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
            _virtualEnvManager = virtualEnvManager ?? throw new ArgumentNullException(nameof(virtualEnvManager));
            _currentSession = session ?? throw new ArgumentNullException(nameof(session));
            _currentEnvironment = environment ?? throw new ArgumentNullException(nameof(environment));
            _cancellationTokenSource = new CancellationTokenSource();

            // Create progress reporter
            Progress = new Progress<PassedArgs>(args =>
            {
                if (Editor != null)
                {
                    Editor.AddLogMessage("Package Manager", args.Messege, DateTime.Now, -1, null, Errors.Ok);
                }
            });

            // Initialize specialized managers
            _packageOperations = new PackageOperationManager(beepService, pythonRuntime, virtualEnvManager, Progress);
            _requirementsManager = new RequirementsFileManager(beepService, pythonRuntime, _packageOperations, Progress);
            _categoryManager = new PackageCategoryManager(beepService, _packageOperations, Progress);
            _packageSetManager = new PackageSetManager(beepService, _packageOperations, _requirementsManager, Progress);

            // Initialize unit of work if editor is available
            InitializeUnitOfWork();
        }
        #endregion

        #region Session and Environment Management
        /// <summary>
        /// Sets the active session and environment for package operations
        /// </summary>
        public void SetActiveSessionAndEnvironment(PythonSessionInfo session, PythonVirtualEnvironment environment)
        {
            _currentSession = session ?? throw new ArgumentNullException(nameof(session));
            _currentEnvironment = environment ?? throw new ArgumentNullException(nameof(environment));

            // Refresh packages after changing environment
            RefreshAllPackagesAsync();

            // Re-initialize unit of work with the new environment's packages
            InitializeUnitOfWork();
        }

        private void InitializeUnitOfWork()
        {
            if (Editor != null && _currentEnvironment?.InstalledPackages != null)
            {
                UnitofWork = new UnitofWork<PackageDefinition>(Editor, true, _currentEnvironment.InstalledPackages);
            }
        }

        private bool ValidateSessionAndEnvironment()
        {
            if (_currentSession == null)
            {
                ReportError("No Python session is assigned.");
                return false;
            }

            if (_currentEnvironment == null)
            {
                ReportError("No Python environment is assigned.");
                return false;
            }

            return true;
        }
        #endregion

        #region Package Management Core Methods
        /// <summary>
        /// Installs a new package in the current environment
        /// </summary>
        public bool InstallNewPackageAsync(string packageName)
        {
            if (string.IsNullOrEmpty(packageName) || _isBusy || !ValidateSessionAndEnvironment())
                return false;

            _isBusy = true;
            try
            {
                var task = _packageOperations.InstallPackageAsync(packageName, _currentEnvironment);
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
        /// Installs or upgrades the pip package manager
        /// </summary>
        public bool InstallPipToolAsync()
        {
            if (_isBusy || !ValidateSessionAndEnvironment())
                return false;

            _isBusy = true;
            try
            {
                var task = _packageOperations.UpgradePackageAsync("pip", _currentEnvironment);
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
                var task = _packageOperations.GetAllPackagesAsync(_currentEnvironment);
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
        /// Refreshes information for a specific package in the current environment
        /// </summary>
        public bool RefreshPackageAsync(string packageName)
        {
            if (string.IsNullOrEmpty(packageName) || _isBusy || !ValidateSessionAndEnvironment())
                return false;

            _isBusy = true;
            try
            {
                var task = _packageOperations.GetPackageInfoAsync(packageName, _currentEnvironment);
                task.Wait();
                var packageInfo = task.Result;

                if (packageInfo != null)
                {
                    // Update or add package in the installed packages list
                    var existingPackage = _currentEnvironment.InstalledPackages.FirstOrDefault(p =>
                        p.PackageName != null &&
                        p.PackageName.Equals(packageName, StringComparison.OrdinalIgnoreCase));

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
                        _currentEnvironment.InstalledPackages.Add(packageInfo);
                    }
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
                var task = _packageOperations.UninstallPackageAsync(packageName, _currentEnvironment);
                task.Wait();
                bool result = task.Result;

                if (result)
                {
                    // Remove the package from our list
                    var packageToRemove = _currentEnvironment.InstalledPackages.FirstOrDefault(p =>
                        p.PackageName != null &&
                        p.PackageName.Equals(packageName, StringComparison.OrdinalIgnoreCase));

                    if (packageToRemove != null)
                    {
                        _currentEnvironment.InstalledPackages.Remove(packageToRemove);
                    }
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
                var refreshTask = _packageOperations.GetAllPackagesAsync(_currentEnvironment);
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

                    var upgradeTask = _packageOperations.UpgradePackageAsync(pkg.PackageName, _currentEnvironment);
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
                var task = _packageOperations.UpgradePackageAsync(packageName, _currentEnvironment);
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
        /// Synchronizes the environment's package list with a source list
        /// </summary>
        private void SynchronizePackages(List<PackageDefinition> sourcePackages)
        {
            if (sourcePackages == null)
                return;

            // Initialize packages collection if needed
            if (_currentEnvironment.InstalledPackages == null)
            {
                _currentEnvironment.InstalledPackages = new ObservableBindingList<PackageDefinition>();
            }
            else
            {
                // Clear existing packages
                _currentEnvironment.InstalledPackages.Clear();
            }

            // Add all packages from source
            foreach (var package in sourcePackages)
            {
                _currentEnvironment.InstalledPackages.Add(package);
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
                var task = _requirementsManager.InstallFromRequirementsFileAsync(filePath, _currentEnvironment);
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
        /// Generates a requirements file from the packages installed in the current environment
        /// </summary>
        public bool GenerateRequirementsFile(string filePath, bool includeVersions = true)
        {
            if (string.IsNullOrEmpty(filePath) || _isBusy || !ValidateSessionAndEnvironment())
                return false;

            _isBusy = true;
            try
            {
                var task = _requirementsManager.GenerateRequirementsFileAsync(filePath, _currentEnvironment, includeVersions);
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
            if (_currentEnvironment?.InstalledPackages == null)
            {
                return new ObservableBindingList<PackageDefinition>();
            }

            var filtered = new ObservableBindingList<PackageDefinition>();
            foreach (var package in _categoryManager.GetPackagesByCategory(_currentEnvironment, category))
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

            _categoryManager.SetPackageCategory(_currentEnvironment, packageName, category);
        }

        /// <summary>
        /// Updates categories for multiple packages at once
        /// </summary>
        public void UpdatePackageCategories(Dictionary<string, PackageCategory> packageCategories)
        {
            if (packageCategories == null || packageCategories.Count == 0 || !ValidateSessionAndEnvironment())
                return;

            _categoryManager.UpdatePackageCategories(_currentEnvironment, packageCategories);
        }

        /// <summary>
        /// Populates common package categories based on known package names
        /// </summary>
        public bool PopulateCommonPackageCategories()
        {
            if (!ValidateSessionAndEnvironment())
                return false;

            return _categoryManager.PopulateCommonPackageCategories(_currentEnvironment);
        }

        /// <summary>
        /// Suggests categories for packages based on their names and descriptions
        /// </summary>
        public async Task<Dictionary<string, PackageCategory>> SuggestCategoriesForPackages(IEnumerable<string> packageNames)
        {
            if (packageNames == null || !packageNames.Any() || !ValidateSessionAndEnvironment())
                return new Dictionary<string, PackageCategory>();

            return await _categoryManager.SuggestCategoriesForPackagesAsync(packageNames, _currentEnvironment);
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
                var task = _packageSetManager.InstallPackageSetAsync(setName, _currentEnvironment);
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
                    setName, _currentEnvironment, description);
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
            if (!_isDisposed)
            {
                if (disposing)
                {
                    // Clean up managed resources
                    _cancellationTokenSource?.Cancel();
                    _cancellationTokenSource?.Dispose();
                    UnitofWork = null;
                }

                _isDisposed = true;
            }
        }
        #endregion
    }
}