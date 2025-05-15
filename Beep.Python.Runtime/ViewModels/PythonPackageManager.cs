using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Beep.Python.Model;
using Beep.Python.RuntimeEngine.Helpers;
using Beep.Python.RuntimeEngine.PackageManagement;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Container.Services;
using TheTechIdea.Beep.Editor;

namespace Beep.Python.RuntimeEngine.ViewModels
{
    /// <summary>
    /// Main Python package management class implementing IPythonPackageManager interface.
    /// Coordinates specialized components for different package management responsibilities.
    /// </summary>
    public class PythonPackageManager : IPythonPackageManager, IDisposable
    {
        #region Fields
        private readonly IBeepService _beepService;
        private readonly IPythonRunTimeManager _pythonRuntime;
        private readonly IPythonVirtualEnvManager _virtualEnvManager;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private bool _isBusy = false;
        private PythonSessionInfo _currentSession;
        private PythonVirtualEnvironment _currentEnvironment;
        private bool _isDisposed = false;

        // Specialized package management components
        private readonly PackageOperationManager _packageOperations;
        private readonly RequirementsFileManager _requirementsManager;
        private readonly PackageCategoryManager _categoryManager;
        private readonly PackageSetManager _packageSetManager;
        #endregion

        #region Properties
        public IDMEEditor Editor => _beepService?.DMEEditor;
        public UnitofWork<PackageDefinition> UnitofWork { get; set; }
        public IProgress<PassedArgs> Progress { get; set; }
        public CancellationToken Token => _cancellationTokenSource.Token;
        public bool IsBusy => _isBusy;
        #endregion

        #region Constructor
        public PythonPackageManager(
            IBeepService beepService,
            IPythonRunTimeManager pythonRuntime,
            IPythonVirtualEnvManager virtualEnvManager,
            PythonVirtualEnvironment environment = null,
            PythonSessionInfo session = null)
        {
            _beepService = beepService ?? throw new ArgumentNullException(nameof(beepService));
            _pythonRuntime = pythonRuntime ?? throw new ArgumentNullException(nameof(pythonRuntime));
            _virtualEnvManager = virtualEnvManager ?? throw new ArgumentNullException(nameof(virtualEnvManager));
            
            // Initialize current session and environment
            _currentSession = session;
            _currentEnvironment = environment;
            
            // Set up progress reporting
            Progress = new Progress<PassedArgs>(args =>
            {
                if (Editor != null)
                {
                    Editor.AddLogMessage("Package Manager", args.Messege, DateTime.Now, -1, null, Errors.Ok);
                }
            });

            // Initialize the specialized components
            _packageOperations = new PackageOperationManager(_beepService, _pythonRuntime, _virtualEnvManager, Progress);
            _requirementsManager = new RequirementsFileManager(_beepService, _pythonRuntime, _packageOperations, Progress);
            _categoryManager = new PackageCategoryManager(_beepService, _packageOperations, Progress);
            _packageSetManager = new PackageSetManager(_beepService, _packageOperations, _requirementsManager, Progress);

            // Initialize UnitOfWork if we have an environment
            if (_currentEnvironment != null)
            {
                InitializeUnitOfWork();
            }
        }
        #endregion

        #region Session and Environment Management
        /// <summary>
        /// Sets the active session and environment for package operations
        /// </summary>
        public void SetActiveSessionAndEnvironment(PythonSessionInfo session, PythonVirtualEnvironment environment)
        {
            if (session == null || environment == null)
                throw new ArgumentNullException(session == null ? nameof(session) : nameof(environment));

            _currentSession = session;
            _currentEnvironment = environment;

            // Reinitialize UnitOfWork
            InitializeUnitOfWork();

            // Refresh packages
            RefreshAllPackagesAsync();
        }

        private void InitializeUnitOfWork()
        {
            // Create a unit of work for managing package data
            if (Editor != null && _currentEnvironment?.InstalledPackages != null)
            {
                UnitofWork = new UnitofWork<PackageDefinition>(Editor, true, _currentEnvironment.InstalledPackages);
            }
        }

        /// <summary>
        /// Validates that we have a valid session and environment
        /// </summary>
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

        #region IPythonPackageManager Implementation - Package Operations
        /// <summary>
        /// Installs a new Python package
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
                    // If installation was successful, refresh package info
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
        /// Updates pip tool
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
                ReportError($"Failed to install/upgrade pip: {ex.Message}");
                return false;
            }
            finally
            {
                _isBusy = false;
            }
        }

        /// <summary>
        /// Refreshes information about all installed packages
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
                    // Update environment's package list
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
        /// Refreshes information about a specific package
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
                    // Update package in environment
                    var existingPackage = _currentEnvironment.InstalledPackages.FirstOrDefault(p =>
                        p.PackageName != null &&
                        p.PackageName.Equals(packageName, StringComparison.OrdinalIgnoreCase));

                    if (existingPackage != null)
                    {
                        // Update properties of existing package
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
        /// Uninstalls a Python package
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
        /// Upgrades all packages that have updates available
        /// </summary>
        public bool UpgradeAllPackagesAsync()
        {
            if (_isBusy || !ValidateSessionAndEnvironment())
                return false;

            _isBusy = true;
            try
            {
                // Refresh packages first
                RefreshAllPackagesAsync();

                // Find packages that need updates
                var packagesToUpdate = _currentEnvironment.InstalledPackages
                    .Where(p => p.Buttondisplay == "Update" ||
                               (p.Updateversion != null && p.Version != null &&
                                p.Updateversion != p.Version))
                    .ToList();

                if (packagesToUpdate.Count == 0)
                {
                    ReportProgress("No packages need upgrading.");
                    return true;
                }

                // Keep track of any failures
                bool anyFailures = false;

                // Upgrade each package
                for (int i = 0; i < packagesToUpdate.Count; i++)
                {
                    var pkg = packagesToUpdate[i];

                    ReportProgress($"Upgrading {pkg.PackageName} ({i + 1}/{packagesToUpdate.Count}) from {pkg.Version} to {pkg.Updateversion}");
                    
                    var task = _packageOperations.UpgradePackageAsync(pkg.PackageName, _currentEnvironment);
                    task.Wait();
                    bool result = task.Result;
                    
                    if (!result)
                    {
                        anyFailures = true;
                        ReportError($"Failed to upgrade {pkg.PackageName}");
                    }

                    // Refresh the package info after upgrade
                    RefreshPackageAsync(pkg.PackageName);
                }

                return !anyFailures;
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
        /// Upgrades a specific package
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
                    // Refresh the package in our list
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
        #endregion

        #region IPythonPackageManager Implementation - Requirements Files
        /// <summary>
        /// Installs packages from a requirements file
        /// </summary>
        public bool InstallFromRequirementsFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || _isBusy || !ValidateSessionAndEnvironment())
                return false;

            _isBusy = true;
            try
            {
                var task = _requirementsManager.InstallFromRequirementsFileAsync(filePath, _currentEnvironment);
                task.Wait();
                bool result = task.Result;

                if (result)
                {
                    // Refresh packages after installation
                    RefreshAllPackagesAsync();
                }

                return result;
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
        /// Generates a requirements file from the current environment
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
        /// Updates a virtual environment with packages from a requirements file
        /// </summary>
        public bool UpdateVirtualEnvironmentWithRequirementsFile(PythonVirtualEnvironment environment)
        {
            if (environment == null || _isBusy)
                return false;

            _isBusy = true;
            try
            {
                // Store current environment
                var originalEnvironment = _currentEnvironment;

                try
                {
                    // Set the target environment as current
                    _currentEnvironment = environment;

                    // Update the environment with its requirements file
                    var task = _requirementsManager.UpdateEnvironmentWithRequirementsFileAsync(environment);
                    task.Wait();
                    return task.Result;
                }
                finally
                {
                    // Restore original environment if needed
                    if (originalEnvironment != null && originalEnvironment.ID != environment.ID)
                    {
                        _currentEnvironment = originalEnvironment;
                    }
                }
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

        #region IPythonPackageManager Implementation - Package Categories
        /// <summary>
        /// Gets packages in a specific category
        /// </summary>
        public ObservableBindingList<PackageDefinition> GetPackagesByCategory(PackageCategory category)
        {
            var packages = _categoryManager.GetPackagesByCategory(_currentEnvironment, category);
            var result = new ObservableBindingList<PackageDefinition>();

            foreach (var package in packages)
            {
                result.Add(package);
            }

            return result;
        }

        /// <summary>
        /// Sets the category for a specific package
        /// </summary>
        public void SetPackageCategory(string packageName, PackageCategory category)
        {
            if (!ValidateSessionAndEnvironment())
                return;

            _categoryManager.SetPackageCategory(_currentEnvironment, packageName, category);
        }

        /// <summary>
        /// Updates categories for multiple packages at once
        /// </summary>
        public void UpdatePackageCategories(Dictionary<string, PackageCategory> packageCategories)
        {
            if (!ValidateSessionAndEnvironment())
                return;

            _categoryManager.UpdatePackageCategories(_currentEnvironment, packageCategories);
        }

        /// <summary>
        /// Populates common package categories based on known package sets
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
            if (!ValidateSessionAndEnvironment())
                return new Dictionary<string, PackageCategory>();

            return await _categoryManager.SuggestCategoriesForPackagesAsync(packageNames, _currentEnvironment);
        }
        #endregion

        #region IPythonPackageManager Implementation - Package Sets
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
                bool result = task.Result;

                if (result)
                {
                    // Refresh packages after installation
                    RefreshAllPackagesAsync();
                }

                return result;
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
        /// Gets a dictionary of available package sets
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
            if (string.IsNullOrEmpty(setName) || _isBusy || !ValidateSessionAndEnvironment())
                return false;

            _isBusy = true;
            try
            {
                var task = _packageSetManager.SavePackageSetFromEnvironmentAsync(setName, _currentEnvironment, description);
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

        #region Helper Methods
        /// <summary>
        /// Updates the environment's package list with new package data
        /// </summary>
        private void SynchronizePackages(List<PackageDefinition> packages)
        {
            if (_currentEnvironment?.InstalledPackages == null || packages == null)
                return;

            // Clear existing packages
            _currentEnvironment.InstalledPackages.Clear();

            // Add all packages from source
            foreach (var package in packages)
            {
                _currentEnvironment.InstalledPackages.Add(package);
            }

            // Reinitialize UnitOfWork if needed
            if (UnitofWork == null)
            {
                InitializeUnitOfWork();
            }
        }

        private void ReportProgress(string message)
        {
            Progress?.Report(new PassedArgs { Messege = message });
            
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

            // Also log to editor
            Editor?.AddLogMessage("Package Manager", message, DateTime.Now, -1, null, Errors.Failed);
        }
        #endregion

        #region IDisposable Implementation
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

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