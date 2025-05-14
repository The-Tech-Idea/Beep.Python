using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Beep.Python.Model;
using Beep.Python.RuntimeEngine.Helpers;
using Python.Runtime;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Container.Services;
using TheTechIdea.Beep.Editor;
using System.Text;

namespace Beep.Python.RuntimeEngine.ViewModels
{
    public class PythonPackageManager : IPythonPackageManager
    {
        private readonly IBeepService _beepService;
        private readonly IPythonRunTimeManager _pythonRuntime;
        private readonly IPythonSessionManager _sessionManager;
        private IPythonVirtualEnvManager _virtualEnvmanager;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private bool _isBusy = false;
        private PythonSessionInfo _currentSession;
        private PythonVirtualEnvironment _currentEnvironment;
        private bool _isDisposed = false;
        private Dictionary<string, PackageSet> _packageSets;

        public IDMEEditor Editor => _beepService?.DMEEditor;
     
        public UnitofWork<PackageDefinition> UnitofWork { get; set; }
        public IProgress<PassedArgs> Progress { get; set; }
        public CancellationToken Token => _cancellationTokenSource.Token;
        public bool IsBusy => _isBusy;

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
            _virtualEnvmanager = virtualEnvManager ?? throw new ArgumentNullException(nameof(virtualEnvManager));
            _currentSession = session ?? throw new ArgumentNullException(nameof(session));
            _currentEnvironment = environment ?? throw new ArgumentNullException(nameof(environment));
            
            Progress = new Progress<PassedArgs>(args =>
            {
                if (Editor != null)
                {
                    Editor.AddLogMessage("Package Manager", args.Messege, DateTime.Now, -1, null, Errors.Ok);
                }
            });

            InitializeUnitOfWork();
        }

        private void InitializeUnitOfWork()
        {
            // Create a unit of work for managing package data
            if (Editor != null)
            {
                UnitofWork = new UnitofWork<PackageDefinition>(Editor, true, _currentEnvironment.InstalledPackages);
            }
        }

        public void SetActiveSessionAndEnvironment(PythonSessionInfo session, PythonVirtualEnvironment environment)
        {
            _currentSession = session ?? throw new ArgumentNullException(nameof(session));
            _currentEnvironment = environment ?? throw new ArgumentNullException(nameof(environment));

            // Refresh packages in this environment
            RefreshAllPackagesAsync();
        }

        #region IPackageManagerViewModel Implementation

        public bool InstallNewPackageAsync(string packageName)
        {
            if (string.IsNullOrEmpty(packageName) || _isBusy || !ValidateSessionAndEnvironment())
                return false;

            _isBusy = true;
            try
            {
               

                var result = InstallNewPackageAsync(packageName);

                if (result)
                {
                    // If installation was successful, refresh package list
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

        public bool InstallPipToolAsync()
        {
            if (_isBusy || !ValidateSessionAndEnvironment())
                return false;

            _isBusy = true;
            try
            {
             

                return InstallPipToolAsync();
            }
            catch (Exception ex)
            {
                ReportError($"Failed to install pip tool: {ex.Message}");
                return false;
            }
            finally
            {
                _isBusy = false;
            }
        }

        public bool RefreshAllPackagesAsync()
        {
            if (_isBusy || !ValidateSessionAndEnvironment())
                return false;

            _isBusy = true;
            try
            {
                

                bool result = RefreshAllPackagesAsync();
                if (result)
                {
                    // Update our package list from the package manager
                    SynchronizePackages(_currentEnvironment.InstalledPackages);
                }

                return result;
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

        public bool RefreshPackageAsync(string packageName)
        {
            if (string.IsNullOrEmpty(packageName) || _isBusy || !ValidateSessionAndEnvironment())
                return false;

            _isBusy = true;
            try
            {
               

                bool result = RefreshPackageAsync(packageName);
                if (result)
                {
                    // Update the specific package in our list
                    var updatedPackage = _currentEnvironment.InstalledPackages.FirstOrDefault(p =>
                        p.PackageName != null &&
                        p.PackageName.Equals(packageName, StringComparison.OrdinalIgnoreCase));

                    if (updatedPackage != null)
                    {
                        var existingPackage = _currentEnvironment.InstalledPackages.FirstOrDefault(p =>
                            p.PackageName != null &&
                            p.PackageName.Equals(packageName, StringComparison.OrdinalIgnoreCase));

                        if (existingPackage != null)
                        {
                            // Update properties of existing package
                            existingPackage.Version = updatedPackage.Version;
                            existingPackage.Updateversion = updatedPackage.Updateversion;
                            existingPackage.Status = updatedPackage.Status;
                            existingPackage.Buttondisplay = updatedPackage.Buttondisplay;
                            existingPackage.Description = updatedPackage.Description;
                        }
                        else
                        {
                            // Add new package
                            _currentEnvironment.InstalledPackages.Add(updatedPackage);
                        }
                    }
                }

                return result;
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

        public bool UnInstallPackageAsync(string packageName)
        {
            if (string.IsNullOrEmpty(packageName) || _isBusy || !ValidateSessionAndEnvironment())
                return false;

            _isBusy = true;
            try
            {
              

                bool result = UnInstallPackageAsync(packageName);
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

        public bool UpgradeAllPackagesAsync()
        {
            if (_isBusy || !ValidateSessionAndEnvironment())
                return false;

            _isBusy = true;
            try
            {
               

                bool result = UpgradeAllPackagesAsync();
                if (result)
                {
                    // Refresh our package list
                    RefreshAllPackagesAsync();
                }

                return result;
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

        public bool UpgradePackageAsync(string packageName)
        {
            if (string.IsNullOrEmpty(packageName) || _isBusy || !ValidateSessionAndEnvironment())
                return false;

            _isBusy = true;
            try
            {
               

                bool result = UpgradePackageAsync(packageName);
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
        #region Package Manager
        /// <summary>
        /// Uses a dedicated admin session to perform package management operations
        /// for consistent package management across environments.
        /// </summary>
        private async Task<string> RunPackageOperationWithAdminSessionAsync(
            string packageName,
            PackageAction action,
            PythonVirtualEnvironment environment = null)
        {
            // Use the specified environment or fall back to the current one
            var env = environment ?? _currentEnvironment;
            if (env == null)
            {
                ReportError("No environment specified for package operation.");
                return string.Empty;
            }

            try
            {
                // Get admin session specifically for package management
                var adminSession = _virtualEnvmanager.GetPackageManagementSession(env);
                if (adminSession == null)
                {
                    ReportError("Failed to obtain admin session for package management.");
                    return string.Empty;
                }

                // Update session references if needed
                if (_currentSession == null || _currentSession.Status != PythonSessionStatus.Active)
                {
                    _currentSession = adminSession;
                }

                // Use the existing RunPackageManagerAsync but with the admin session
                return await RunPackageManagerAsync(
                    Progress,
                    packageName,
                    action,
                    env,
                    env.PythonBinary == PythonBinary.Conda);
            }
            catch (Exception ex)
            {
                ReportError($"Error during package operation: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Installs a package in the specified environment using the admin session.
        /// </summary>
        /// <param name="packageName">Name of the package to install</param>
        /// <param name="environment">Target environment (null for current environment)</param>
        /// <returns>True if successful</returns>
        public async Task<bool> InstallPackageAsync(string packageName, PythonVirtualEnvironment environment = null)
        {
            if (string.IsNullOrEmpty(packageName) || _isBusy)
                return false;

            _isBusy = true;
            try
            {
                // Use admin session for consistent package management
                var result = await RunPackageOperationWithAdminSessionAsync(
                    packageName,
                    PackageAction.Install,
                    environment);

                // Refresh package information if successful
                if (!string.IsNullOrEmpty(result) && !result.Contains("ERROR:"))
                {
                    await RefreshPackageAsync(packageName, environment);
                    return true;
                }
                return false;
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
        /// Uninstalls a package from the specified environment using the admin session.
        /// </summary>
        /// <param name="packageName">Name of the package to uninstall</param>
        /// <param name="environment">Target environment (null for current environment)</param>
        /// <returns>True if successful</returns>
        public async Task<bool> UninstallPackageAsync(string packageName, PythonVirtualEnvironment environment = null)
        {
            if (string.IsNullOrEmpty(packageName) || _isBusy)
                return false;

            _isBusy = true;
            try
            {
                // Use admin session for consistent package management
                var result = await RunPackageOperationWithAdminSessionAsync(
                    packageName,
                    PackageAction.Remove,
                    environment);

                // Update package list if successful
                if (!string.IsNullOrEmpty(result) && !result.Contains("ERROR:"))
                {
                    // Remove from package list
                    var packageToRemove = _currentEnvironment.InstalledPackages.FirstOrDefault(p => p.PackageName != null &&
                                                                 p.PackageName.Equals(packageName, StringComparison.OrdinalIgnoreCase));
                    if (packageToRemove != null)
                    {
                        _currentEnvironment.InstalledPackages.Remove(packageToRemove);
                    }
                    return true;
                }
                return false;
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
        /// Upgrades a package in the specified environment using the admin session.
        /// </summary>
        /// <param name="packageName">Name of the package to upgrade</param>
        /// <param name="environment">Target environment (null for current environment)</param>
        /// <returns>True if successful</returns>
        public async Task<bool> UpgradePackageAsync(string packageName, PythonVirtualEnvironment environment = null)
        {
            if (string.IsNullOrEmpty(packageName) || _isBusy)
                return false;

            _isBusy = true;
            try
            {
                // Determine if this is pip itself
                var action = packageName.Equals("pip", StringComparison.OrdinalIgnoreCase)
                    ? PackageAction.UpgradePackager
                    : PackageAction.Update;

                // Use admin session for consistent package management
                var result = await RunPackageOperationWithAdminSessionAsync(
                    packageName,
                    action,
                    environment);

                // Refresh package information if successful
                if (!string.IsNullOrEmpty(result) && !result.Contains("ERROR:"))
                {
                    await RefreshPackageAsync(packageName, environment);
                    return true;
                }
                return false;
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
        /// Refreshes package information for a specific package using the admin session.
        /// </summary>
        /// <param name="packageName">Name of the package to refresh</param>
        /// <param name="environment">Target environment (null for current environment)</param>
        /// <returns>True if successful</returns>
        public async Task<bool> RefreshPackageAsync(string packageName, PythonVirtualEnvironment environment = null)
        {
            if (string.IsNullOrEmpty(packageName) || _isBusy)
                return false;

            _isBusy = true;
            try
            {
                // Use the environment specified or fall back to current
                var env = environment ?? _currentEnvironment;
                if (env == null)
                {
                    ReportError("No environment specified for package refresh.");
                    return false;
                }

                // Get admin session
                var adminSession = _virtualEnvmanager.GetPackageManagementSession(env);
                if (adminSession == null)
                {
                    ReportError("Failed to obtain admin session for package refresh.");
                    return false;
                }

                // Create the Python code to get package info
                string packageInfoScript = $@"
import json
import pkg_resources
import importlib.metadata

try:
    # Get package metadata
    package_info = None
    try:
        # Try importlib.metadata first (Python 3.8+)
        dist = importlib.metadata.distribution('{packageName}')
        package_info = {{
            'name': dist.metadata['Name'],
            'version': dist.version,
            'summary': dist.metadata.get('Summary', ''),
            'location': str(dist.locate_file(''))
        }}
    except (importlib.metadata.PackageNotFoundError, KeyError):
        # Fall back to pkg_resources
        try:
            dist = pkg_resources.get_distribution('{packageName}')
            package_info = {{
                'name': dist.project_name,
                'version': dist.version,
                'summary': dist.get_metadata('METADATA') if dist.has_metadata('METADATA') else '',
                'location': dist.location
            }}
        except pkg_resources.DistributionNotFound:
            pass

    # Convert to JSON
    result = json.dumps(package_info if package_info else {{}})
    print(result)
except Exception as e:
    print(json.dumps({{'error': str(e)}}))
";

                // Execute the script using the admin session
                var output = await _pythonRuntime.ExecuteManager.RunPythonCodeAndGetOutput(
                    Progress,
                    packageInfoScript,
                    adminSession);

                if (string.IsNullOrEmpty(output) || output.Contains("null") || output.Contains("error"))
                {
                    // Package not found
                    return false;
                }

                try
                {
                    // Parse the JSON output
                    var packageInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(output);

                    if (packageInfo != null && packageInfo.ContainsKey("name"))
                    {
                        // Check online for latest version
                        var onlinePackage = await CheckIfPackageExistsAsync(packageName);

                        // Create or update package definition
                        var existingPackage = _currentEnvironment.InstalledPackages.FirstOrDefault(p =>
                            p.PackageName != null &&
                            p.PackageName.Equals(packageName, StringComparison.OrdinalIgnoreCase));

                        if (existingPackage != null)
                        {
                            // Update existing package
                            existingPackage.Version = packageInfo["version"];
                            existingPackage.Updateversion = onlinePackage?.Version ?? packageInfo["version"];
                            existingPackage.Description = packageInfo.ContainsKey("summary") ? packageInfo["summary"] : "";
                            existingPackage.Installpath = packageInfo.ContainsKey("location") ? packageInfo["location"] : "";
                            existingPackage.Status = PackageStatus.Installed;

                            // Set button display based on version comparison
                            if (onlinePackage != null &&
                                Version.TryParse(onlinePackage.Version, out var onlineVersion) &&
                                Version.TryParse(packageInfo["version"], out var currentVersion) &&
                                onlineVersion > currentVersion)
                            {
                                existingPackage.Buttondisplay = "Update";
                            }
                            else
                            {
                                existingPackage.Buttondisplay = "Status";
                            }
                        }
                        else
                        {
                            // Add new package
                            var newPackage = new PackageDefinition
                            {
                                PackageName = packageInfo["name"],
                                Version = packageInfo["version"],
                                Updateversion = onlinePackage?.Version ?? packageInfo["version"],
                                Description = packageInfo.ContainsKey("summary") ? packageInfo["summary"] : "",
                                Installpath = packageInfo.ContainsKey("location") ? packageInfo["location"] : "",
                                Status = PackageStatus.Installed,
                                Buttondisplay = onlinePackage != null &&
                                                Version.TryParse(onlinePackage.Version, out var onlineVer) &&
                                                Version.TryParse(packageInfo["version"], out var currentVer) &&
                                                onlineVer > currentVer ? "Update" : "Status"
                            };

                            _currentEnvironment.InstalledPackages.Add(newPackage);

                            // Also update environment's package list if this is the current environment
                            if (env == _currentEnvironment && env.InstalledPackages != null)
                            {
                                env.InstalledPackages.Add(newPackage);
                            }
                        }

                        return true;
                    }
                }
                catch (Exception ex)
                {
                    ReportError($"Error parsing package info: {ex.Message}");
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
        /// Refreshes all packages in the specified environment using the admin session.
        /// </summary>
        /// <param name="environment">Target environment (null for current environment)</param>
        /// <returns>True if successful</returns>
        public async Task<bool> RefreshAllPackagesAsync(PythonVirtualEnvironment environment = null)
        {
            if (_isBusy)
                return false;

            _isBusy = true;
            try
            {
                // Use the environment specified or fall back to current
                var env = environment ?? _currentEnvironment;
                if (env == null)
                {
                    ReportError("No environment specified for package refresh.");
                    return false;
                }

                // Get admin session
                var adminSession = _virtualEnvmanager.GetPackageManagementSession(env);
                if (adminSession == null)
                {
                    ReportError("Failed to obtain admin session for package refresh.");
                    return false;
                }

                // Script to get all installed packages
                string packageListScript = @"
import json
import importlib.metadata

try:
    # Get all installed packages
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
        except (KeyError, Exception) as e:
            print(f'Error with package {dist}: {e}')
    
    # Convert to JSON
    result = json.dumps(packages)
    print(result)
except Exception as e:
    print(json.dumps({'error': str(e)}))
";

                // Execute the script using the admin session
                var output = await _pythonRuntime.ExecuteManager.RunPythonCodeAndGetOutput(
                    Progress,
                    packageListScript,
                    adminSession);

                if (string.IsNullOrEmpty(output) || output.Contains("error"))
                {
                    ReportError("Failed to get package list from Python environment.");
                    return false;
                }

                try
                {
                    // Parse the JSON output
                    var packageList = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(output);

                    if (packageList != null)
                    {
                        // Clear current packages
                        _currentEnvironment.InstalledPackages.Clear();

                        // Initialize environment's package list if needed
                        if (env.InstalledPackages == null)
                        {
                            env.InstalledPackages = new ObservableBindingList<PackageDefinition>();
                        }
                        else
                        {
                            env.InstalledPackages.Clear();
                        }

                        // Process packages in smaller batches to avoid overwhelming the UI
                        int batchSize = 10;
                        bool isInternetAvailable = PythonRunTimeDiagnostics.CheckNet();

                        for (int i = 0; i < packageList.Count; i += batchSize)
                        {
                            var batch = packageList.Skip(i).Take(batchSize).ToList();

                            foreach (var packageInfo in batch)
                            {
                                if (packageInfo.ContainsKey("name") && packageInfo.ContainsKey("version"))
                                {
                                    string packageName = packageInfo["name"];
                                    string packageVersion = packageInfo["version"];

                                    // Report progress
                                    Progress?.Report(new PassedArgs
                                    {
                                        Messege = $"Processing package {packageName} ({i + 1}/{packageList.Count})",
                                        ParameterInt1 = i + 1,
                                        ParameterInt2 = packageList.Count
                                    });

                                    // Check online for latest version
                                    PackageDefinition onlinePackage = null;
                                    if (isInternetAvailable)
                                    {
                                        onlinePackage = await CheckIfPackageExistsAsync(packageName);
                                    }

                                    // Create package definition
                                    var packageDef = new PackageDefinition
                                    {
                                        PackageName = packageName,
                                        Version = packageVersion,
                                        Updateversion = onlinePackage?.Version ?? packageVersion,
                                        Description = packageInfo.ContainsKey("summary") ? packageInfo["summary"] : "",
                                        Installpath = packageInfo.ContainsKey("location") ? packageInfo["location"] : "",
                                        Status = PackageStatus.Installed,
                                        Buttondisplay = onlinePackage != null &&
                                                       Version.TryParse(onlinePackage.Version, out var onlineVer) &&
                                                       Version.TryParse(packageVersion, out var currentVer) &&
                                                       onlineVer > currentVer ? "Update" : "Status"
                                    };

                                    // Add to package lists
                                    _currentEnvironment.InstalledPackages.Add(packageDef);
                                    env.InstalledPackages.Add(packageDef);
                                }
                            }

                            // Allow UI to update between batches
                            await Task.Delay(1);
                        }

                        return true;
                    }
                }
                catch (Exception ex)
                {
                    ReportError($"Error processing package list: {ex.Message}");
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
        /// Upgrades all packages in the specified environment that have newer versions available.
        /// </summary>
        /// <param name="environment">Target environment (null for current environment)</param>
        /// <returns>True if all upgrades completed successfully</returns>
        public async Task<bool> UpgradeAllPackagesAsync(PythonVirtualEnvironment environment = null)
        {
            if (_isBusy)
                return false;

            _isBusy = true;
            try
            {
                // Use the environment specified or fall back to current
                var env = environment ?? _currentEnvironment;
                if (env == null)
                {
                    ReportError("No environment specified for package upgrades.");
                    return false;
                }

                // Refresh packages first to get latest information
                await RefreshAllPackagesAsync(env);

                // Find packages that need updates
                var packagesToUpdate = _currentEnvironment.InstalledPackages
                    .Where(p => p.Buttondisplay == "Update" ||
                               (p.Updateversion != null && p.Version != null &&
                                p.Updateversion != p.Version))
                    .ToList();

                if (packagesToUpdate.Count == 0)
                {
                    Progress?.Report(new PassedArgs { Messege = "No packages need upgrading." });
                    return true;
                }

                // Keep track of any failures
                bool anyFailures = false;

                // Upgrade each package
                for (int i = 0; i < packagesToUpdate.Count; i++)
                {
                    var pkg = packagesToUpdate[i];

                    Progress?.Report(new PassedArgs
                    {
                        Messege = $"Upgrading {pkg.PackageName} ({i + 1}/{packagesToUpdate.Count}) from {pkg.Version} to {pkg.Updateversion}",
                        ParameterInt1 = i + 1,
                        ParameterInt2 = packagesToUpdate.Count
                    });

                    bool result = await UpgradePackageAsync(pkg.PackageName, env);
                    if (!result)
                    {
                        anyFailures = true;
                        ReportError($"Failed to upgrade {pkg.PackageName}");
                    }

                    // Allow UI to update between operations
                    await Task.Delay(1);
                }

                // Final refresh to confirm upgrades
                await RefreshAllPackagesAsync(env);

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
        /// Installs pip or upgrades it if already installed.
        /// </summary>
        /// <param name="environment">Target environment (null for current environment)</param>
        /// <returns>True if successful</returns>
        public async Task<bool> InstallOrUpgradePipAsync(PythonVirtualEnvironment environment = null)
        {
            if (_isBusy)
                return false;

            _isBusy = true;
            try
            {
                // Use admin session for consistent package management
                var result = await RunPackageOperationWithAdminSessionAsync(
                    "pip",
                    PackageAction.UpgradePackager,
                    environment);

                return !string.IsNullOrEmpty(result) && !result.Contains("ERROR:");
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

        public async Task<string> RunPackageManagerAsync(IProgress<PassedArgs> progress, string packageName, PackageAction packageAction, PythonVirtualEnvironment environment = null, bool useConda = false)
        {
            // Determine the environment to use
            PythonVirtualEnvironment envToUse = environment ?? _currentEnvironment;

            if (envToUse == null)
            {
                ReportError("No environment specified for package operation.");
                return string.Empty;
            }

            // Get the correct paths for this environment
            string environmentPath = envToUse.Path;
            string binPath = Path.Combine(environmentPath, "Scripts"); // Windows
            if (!Directory.Exists(binPath))
            {
                binPath = Path.Combine(environmentPath, "bin"); // Linux/macOS
            }

            string customPath = $"{binPath}".Trim();
            string modifiedFilePath = customPath.Replace("\\", "\\\\");
            string output = "";
            string command = "";

            // Python code for running package commands
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

            // Use the session scope if available
            PyModule scope = null;
            if (_currentSession != null)
            {
                // Get the scope from the PythonRunTimeManager
                scope = _pythonRuntime.GetScope(_currentSession);
            }

            // If no scope is available, use a new temporary scope
            if (scope == null)
            {
                using (Py.GIL())
                {
                    scope = Py.CreateScope();
                }
            }

            using (Py.GIL())
            {
                PyObject globalsDict = scope.GetAttr("__dict__");
                scope.Exec(wrappedPythonCode);

                // Set the custom_path from C# and call set_custom_path function in Python
                PyObject setCustomPathFunc = scope.GetAttr("set_custom_path");
                setCustomPathFunc.Invoke(modifiedFilePath.ToPython());

                PyObject captureOutputFunc = scope.GetAttr("run_pip_and_capture_output");

                // Use conda if the environment is conda-based
                bool useCondaCommand = useConda || envToUse.PythonBinary == PythonBinary.Conda;

                if (useCondaCommand)
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
                    // Determine the python executable name
                    string pythonExe = "python";
                    if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                    {
                        pythonExe = "python.exe";
                    }

                    switch (packageAction)
                    {
                        case PackageAction.Install:
                            command = $"pip install -U {packageName}";
                            break;
                        case PackageAction.Remove:
                            command = $"pip uninstall -y {packageName}";
                            break;
                        case PackageAction.Update:
                            command = $"pip install --upgrade {packageName}";
                            break;
                        case PackageAction.UpgradePackager:
                            command = $"{pythonExe} -m pip install --upgrade pip";
                            break;
                        case PackageAction.InstallPackager:
                            command = $"{pythonExe} {packageName}";
                            break;
                        default:
                            break;
                    }
                }

                Progress?.Report(new PassedArgs() { Messege = $"Running {command}" });
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
                            progress?.Report(new PassedArgs() { Messege = line });
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

            if (output.Length > 0)
            {
                progress?.Report(new PassedArgs() { Messege = $"Finished {command}" });

                // Update the packages for the environment if needed
                if (envToUse.InstalledPackages != null && packageAction != PackageAction.InstallPackager)
                {
                    // Refresh the package list after installation/removal/update
                    await RefreshPackagesForEnvironmentAsync(envToUse, progress, _cancellationTokenSource.Token);
                }
            }
            else
            {
                progress?.Report(new PassedArgs() { Messege = $"Finished {command} with error" });
            }

            return output;
        }

        private async Task<bool> RefreshPackagesForEnvironmentAsync(PythonVirtualEnvironment environment, IProgress<PassedArgs> progress, CancellationToken token)
        {
            if (environment == null || _isBusy)
                return false;

            _isBusy = true;

            try
            {
                // Make sure environment has a package list
                if (environment.InstalledPackages == null)
                {
                    environment.InstalledPackages = new ObservableBindingList<PackageDefinition>();
                }

                // Use Python to get the installed packages
                using (Py.GIL())
                {
                    // Create a session for this operation if needed
                    if(_currentSession == null)
                    {
                        _currentSession = environment.GetLastSession();
                        if (_currentSession == null)
                        {
                            _currentSession = new PythonSessionInfo
                            {
                                SessionName = $"PackageRefresh_{DateTime.Now.Ticks}",
                                VirtualEnvironmentId = environment.ID,
                                StartedAt = DateTime.Now
                            };
                            environment.AddSession(_currentSession);
                        }
                    }
                

                    // Create a scope for the environment if needed
                    PyModule scope = _pythonRuntime.GetScope(_currentSession);
                    if (scope == null)
                    {
                        _pythonRuntime.CreateScope(_currentSession, environment);
                        scope = _pythonRuntime.GetScope(_currentSession);
                    }

                    if (scope != null)
                    {
                        // Script to get the package list
                        string packageListScript = @"
import importlib.metadata
import json

# Get all installed packages with versions
packages = []
for dist in importlib.metadata.distributions():
    try:
        packages.append({
            'name': dist.metadata['Name'],
            'version': dist.version,
            'summary': dist.metadata.get('Summary', '')
        })
    except Exception as e:
        print(f'Error with package {dist}: {e}')
        
# Convert to JSON string
json_packages = json.dumps(packages)
";
                      
                          scope.Exec(packageListScript);

                        // Get the JSON result
                        scope.Exec("print(json_packages)");

                        // To capture the printed output, we need to use RunPythonCodeAndGetOutput
                        string output = await _pythonRuntime.ExecuteManager.RunPythonCodeAndGetOutput(
                            progress ?? Progress,
                            "print(json_packages)",
                            _currentSession);

                        if (!string.IsNullOrEmpty(output))
                        {
                            // Parse the JSON output
                            List<Dictionary<string, string>> packages =
                                Newtonsoft.Json.JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(output);

                            if (packages != null)
                            {
                                // Clear current packages
                                environment.InstalledPackages.Clear();

                                // Process each package
                                int count = packages.Count;
                                int i = 0;
                                bool isInternetAvailable = PythonRunTimeDiagnostics.CheckNet();

                                foreach (var pkg in packages)
                                {
                                    i++;
                                    string packageName = pkg["name"];
                                    string packageVersion = pkg["version"];
                                    string summary = pkg.ContainsKey("summary") ? pkg["summary"] : "";

                                    progress?.Report(new PassedArgs
                                    {
                                        Messege = $"Processing {packageName} {packageVersion}",
                                        ParameterInt1 = i,
                                        ParameterInt2 = count
                                    });

                                    // Check online for updates if internet is available
                                    PackageDefinition onlinePkg = null;
                                    if (isInternetAvailable)
                                    {
                                        onlinePkg = await CheckIfPackageExistsAsync(packageName);
                                    }

                                    // Create a new package definition
                                    var packageDef = new PackageDefinition
                                    {
                                        PackageName = packageName,
                                        Version = packageVersion,
                                        Updateversion = onlinePkg?.Version ?? packageVersion,
                                        Status = PackageStatus.Installed,
                                        Description = summary,
                                        Buttondisplay = onlinePkg != null &&
                                                       new Version(onlinePkg.Version) > new Version(packageVersion)
                                                       ? "Update" : "Status"
                                    };

                                    environment.InstalledPackages.Add(packageDef);
                                }

                                // Update our main package list if this is the current environment
                                if (environment == _currentEnvironment)
                                {
                                    SynchronizePackages(environment.InstalledPackages);
                                }

                                return true;
                            }
                        }
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                ReportError($"Failed to refresh packages for environment {environment.Name}: {ex.Message}");
                return false;
            }
            finally
            {
                _isBusy = false;
            }
        }

        public async Task<bool> ListpackagesAsync(IProgress<PassedArgs> _progress, CancellationToken token, bool useConda = false, string packagename = null)
        {
            if (_isBusy)
                return false;

            _isBusy = true;

            try
            {
                if (_progress != null)
                {
                    Progress = _progress;
                }

                // Use the current environment's packages if available
                if (_currentEnvironment != null && _currentEnvironment.InstalledPackages != null)
                {
                    // Refresh packages in the current environment
                    var refreshTask = RefreshPackagesForEnvironmentAsync(_currentEnvironment, Progress, token);
                    refreshTask.Wait();

                    // Synchronize with our main package list
                    SynchronizePackages(_currentEnvironment.InstalledPackages);

                    _isBusy = false;
                    return true;
                }
                else
                {
                    // Legacy behavior - fall back to the runtime config's package list
                    int i = 0;
                    bool checkall = true;
                    if (!string.IsNullOrEmpty(packagename))
                    {
                        checkall = false;
                    }

                    using (var gil = _pythonRuntime.GIL())
                    {
                        dynamic pkgResources = Py.Import("importlib.metadata");
                        dynamic workingSet = pkgResources.distributions();

                        // Create a package list if needed
                        if (_currentEnvironment.InstalledPackages == null)
                        {
                            _currentEnvironment.InstalledPackages = new ObservableBindingList<PackageDefinition>();
                        }

                        int count = _currentEnvironment.InstalledPackages.Count;
                        int j = 1;
                        bool isInternetAvailable = PythonRunTimeDiagnostics.CheckNet();

                        foreach (dynamic pkg in workingSet)
                        {
                            i++;
                            string packageName = pkg.metadata["Name"];
                            string packageVersion = pkg.version.ToString();
                            string line = $"Checking Package {packageName}: {packageVersion}";

                            Progress?.Report(new PassedArgs
                            {
                                Messege = line,
                                ParameterInt1 = j,
                                ParameterInt2 = count
                            });

                            PackageDefinition onlinepk = null;
                            if (!string.IsNullOrEmpty(packageVersion))
                            {
                                if (checkall)
                                {
                                    if (isInternetAvailable)
                                    {
                                        onlinepk = await CheckIfPackageExistsAsync(packageName);
                                    }

                                    // Find existing package or create new one
                                    PackageDefinition package = _currentEnvironment.InstalledPackages
                                        .FirstOrDefault(p => p.PackageName != null &&
                                                           p.PackageName.Equals(packageName, StringComparison.InvariantCultureIgnoreCase));

                                    if (package != null)
                                    {
                                        // Update existing package
                                        int idx = _currentEnvironment.InstalledPackages.IndexOf(package);
                                        if (onlinepk != null)
                                        {
                                            package.Updateversion = onlinepk.Version;
                                        }
                                        package.Status = PackageStatus.Installed;
                                        package.Buttondisplay = "Status";

                                        if (onlinepk != null)
                                        {
                                            _currentEnvironment.InstalledPackages[idx].Updateversion = onlinepk.Updateversion;
                                            line = $"Package {packageName}: {packageVersion} found with Version {onlinepk.Updateversion}";
                                        }
                                        else
                                        {
                                            _currentEnvironment.InstalledPackages[idx].Updateversion = "Not Found";
                                            line = $"Package {packageName}: {"Not Found"}";
                                        }

                                        Progress?.Report(new PassedArgs { Messege = line });
                                    }
                                    else
                                    {
                                        // Add new package
                                        PackageDefinition packagelist = new PackageDefinition
                                        {
                                            PackageName = packageName,
                                            Version = packageVersion,
                                            Updateversion = packageVersion,
                                            Status = PackageStatus.Installed,
                                            Buttondisplay = "Added"
                                        };

                                        _currentEnvironment.InstalledPackages.Add(packagelist);
                                        line = $"Added new Package {packagelist}: {packagelist.Version}";
                                        Progress?.Report(new PassedArgs { Messege = line });
                                    }
                                }
                                else if (!string.IsNullOrEmpty(packagename) &&
                                        packageName.Equals(packagename, StringComparison.InvariantCultureIgnoreCase))
                                {
                                    // Find matching package
                                    PackageDefinition package = _currentEnvironment.InstalledPackages
                                        .FirstOrDefault(p => p.PackageName != null &&
                                                           p.PackageName.Equals(packageName, StringComparison.InvariantCultureIgnoreCase));

                                    if (package != null)
                                    {
                                        // Update specific package
                                        int idx = _currentEnvironment.InstalledPackages.IndexOf(package);
                                        package.Version = packageVersion;
                                        package.Updateversion = packageVersion;
                                        package.Status = PackageStatus.Installed;
                                        package.Buttondisplay = "Status";

                                        if (isInternetAvailable)
                                        {
                                            onlinepk = await CheckIfPackageExistsAsync(packageName);
                                        }

                                        if (onlinepk != null)
                                        {
                                            _currentEnvironment.InstalledPackages[idx].Updateversion = onlinepk.Updateversion;
                                            package.Updateversion = onlinepk.Version;
                                            line = $"Package {packageName}: {packageVersion} found with Version {onlinepk.Updateversion}";
                                        }
                                        else
                                        {
                                            _currentEnvironment.InstalledPackages[idx].Updateversion = "Not Found";
                                            line = $"Package {packageName}: {"Not Found"}";
                                        }

                                        Progress?.Report(new PassedArgs { Messege = line });
                                    }
                                }
                            }

                            j++;
                        }

                        if (i == 0)
                        {
                            Progress?.Report(new PassedArgs { Messege = "No _currentEnvironment.InstalledPackages Found" });
                            _currentEnvironment.InstalledPackages = new ObservableBindingList<PackageDefinition>();
                        }

                        _currentEnvironment.InstalledPackages = _currentEnvironment.InstalledPackages;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                ReportError($"Error listing packages: {ex.Message}");
                return false;
            }
            finally
            {
                _isBusy = false;
            }
        }

        public async Task<PackageDefinition> CheckIfPackageExistsAsync(string packageName)
        {
            using (HttpClient httpClient = new HttpClient())
            {
                try
                {
                    using (CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
                    {
                        HttpResponseMessage response = await httpClient.GetAsync(
                            $"https://pypi.org/pypi/{packageName}/json",
                            cts.Token).ConfigureAwait(false);

                        if (response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            string jsonResponse = await response.Content.ReadAsStringAsync();
                            dynamic packageData = Newtonsoft.Json.JsonConvert.DeserializeObject(jsonResponse);

                            PackageDefinition packageInfo = new PackageDefinition
                            {
                                PackageName = packageName,
                                Version = packageData.info.version,
                                Description = packageData.info.description
                            };

                            return packageInfo;
                        }
                    }
                }
                catch (HttpRequestException)
                {
                    Console.WriteLine("An error occurred while checking the package. Please try again later.");
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine($"The request to '{packageName}' timed out.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error while parsing package data for '{packageName}': {ex.Message}");
                }
            }

            return null;
        }

        // New methods for working with virtual environments
        public async Task<bool> InstallPackageInEnvironmentAsync(string packageName, PythonVirtualEnvironment environment = null)
        {
            if (string.IsNullOrEmpty(packageName) || _isBusy)
                return false;

            environment = environment ?? _currentEnvironment;
            if (environment == null)
            {
                ReportError("No environment specified for package installation.");
                return false;
            }

            _isBusy = true;

            try
            {
                await RunPackageManagerAsync(
                    Progress,
                    packageName,
                    PackageAction.Install,
                    environment,
                    environment.PythonBinary == PythonBinary.Conda);

                return true;
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

        public async Task<bool> UninstallPackageFromEnvironmentAsync(string packageName, PythonVirtualEnvironment environment = null)
        {
            if (string.IsNullOrEmpty(packageName) || _isBusy)
                return false;

            environment = environment ?? _currentEnvironment;
            if (environment == null)
            {
                ReportError("No environment specified for package uninstallation.");
                return false;
            }

            _isBusy = true;

            try
            {
                await RunPackageManagerAsync(
                    Progress,
                    packageName,
                    PackageAction.Remove,
                    environment,
                    environment.PythonBinary == PythonBinary.Conda);

                return true;
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

        public async Task<bool> UpgradePackageInEnvironmentAsync(string packageName, PythonVirtualEnvironment environment = null)
        {
            if (string.IsNullOrEmpty(packageName) || _isBusy)
                return false;

            environment = environment ?? _currentEnvironment;
            if (environment == null)
            {
                ReportError("No environment specified for package upgrade.");
                return false;
            }

            _isBusy = true;

            try
            {
                PackageAction action = packageName.Equals("pip", StringComparison.OrdinalIgnoreCase)
                    ? PackageAction.UpgradePackager
                    : PackageAction.Update;

                await RunPackageManagerAsync(
                    Progress,
                    packageName,
                    action,
                    environment,
                    environment.PythonBinary == PythonBinary.Conda);

                return true;
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

        public async Task<bool> UpgradeAllPackagesInEnvironmentAsync(PythonVirtualEnvironment environment = null)
        {
            if (_isBusy)
                return false;

            environment = environment ?? _currentEnvironment;
            if (environment == null)
            {
                ReportError("No environment specified for upgrading all packages.");
                return false;
            }

            _isBusy = true;

            try
            {
                // First refresh package list to get the latest information
                await RefreshPackagesForEnvironmentAsync(environment, Progress, _cancellationTokenSource.Token);

                // Find packages that need updates
                var packagesToUpdate = environment.InstalledPackages
                    .Where(p => p.Buttondisplay == "Update" ||
                              (p.Updateversion != null && p.Version != null &&
                               p.Updateversion != p.Version))
                    .ToList();

                if (packagesToUpdate.Count == 0)
                {
                    Progress?.Report(new PassedArgs { Messege = "No packages to upgrade." });
                    return true;
                }

                // Upgrade each package
                foreach (var package in packagesToUpdate)
                {
                    Progress?.Report(new PassedArgs { Messege = $"Upgrading {package.PackageName} from {package.Version} to {package.Updateversion}" });

                    await UpgradePackageInEnvironmentAsync(package.PackageName, environment);
                }

                // Final refresh to confirm upgrades
                await RefreshPackagesForEnvironmentAsync(environment, Progress, _cancellationTokenSource.Token);

                return true;
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

        // Initialize helper for some methods
        private void Init()
        {
            // This is a stub for compatibility with legacy code
            // No initialization is needed since we're using the environment directly
        }
        #endregion "Package Manager"
        #region Requirements File Support

        /// <summary>
        /// Installs packages from a requirements file
        /// </summary>
        /// <param name="filePath">Path to the requirements file</param>
        /// <returns>True if installation was successful</returns>
        public bool InstallFromRequirementsFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath) || _isBusy ||
                !ValidateSessionAndEnvironment())
                return false;

            _isBusy = true;
            try
            {
                // Read the requirements file
                var requirements = ReadRequirementsFile(filePath);
                if (requirements.Count == 0)
                {
                    ReportProgress("No packages found in requirements file.");
                    return false;
                }

                // Temporarily disable auto-updates during batch operation
                bool originalAutoUpdate = _currentEnvironment.AutoUpdateRequirements;
                _currentEnvironment.AutoUpdateRequirements = false;

                // Install each package
                bool success = true;
                int totalPackages = requirements.Count;
                int current = 0;

                ReportProgress($"Installing {totalPackages} packages from requirements file...");

                foreach (var package in requirements)
                {
                    current++;
                    string packageSpec = package.Key + (string.IsNullOrEmpty(package.Value) ? "" : package.Value);

                    ReportProgress($"Installing {packageSpec} ({current}/{totalPackages})");
                    bool installResult = InstallNewPackageAsync(packageSpec);

                    if (!installResult)
                    {
                        ReportError($"Failed to install {packageSpec}");
                        success = false;
                    }
                }

                // Restore original auto-update setting
                _currentEnvironment.AutoUpdateRequirements = originalAutoUpdate;

                // Set the requirements file path in the environment
                if (success && string.IsNullOrEmpty(_currentEnvironment.RequirementsFile))
                {
                    _currentEnvironment.RequirementsFile = filePath;
                }

                // Refresh packages after installation
                RefreshAllPackagesAsync();

                ReportProgress($"Completed installing packages from requirements file. {(success ? "All packages installed successfully." : "Some packages failed to install.")}");
                return success;
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
        /// <param name="filePath">Output file path</param>
        /// <param name="includeVersions">Whether to include version constraints</param>
        /// <returns>True if successful</returns>
        public bool GenerateRequirementsFile(string filePath, bool includeVersions = true)
        {
            if (string.IsNullOrEmpty(filePath) || _isBusy || !ValidateSessionAndEnvironment())
                return false;

            _isBusy = true;
            try
            {
                // Ensure we have the latest package data
                RefreshAllPackagesAsync();

                // Create directory if it doesn't exist
                string directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Build the requirements file content
                StringBuilder content = new StringBuilder();
                content.AppendLine($"# Requirements for {_currentEnvironment.Name}");
                content.AppendLine($"# Generated: {DateTime.Now}");
                content.AppendLine($"# Python version: {_currentEnvironment.PythonVersion}");
                content.AppendLine();

                // Add package entries
                if (_currentEnvironment.InstalledPackages != null)
                {
                    foreach (var package in _currentEnvironment.InstalledPackages.OrderBy(p => p.PackageName))
                    {
                        if (!string.IsNullOrEmpty(package.PackageName))
                        {
                            if (includeVersions && !string.IsNullOrEmpty(package.Version))
                            {
                                content.AppendLine($"{package.PackageName}=={package.Version}");
                            }
                            else
                            {
                                content.AppendLine(package.PackageName);
                            }
                        }
                    }
                }

                // Write the file
                File.WriteAllText(filePath, content.ToString());

                // Update the environment's requirements file information
                _currentEnvironment.RequirementsFile = filePath;
                _currentEnvironment.RequirementsLastUpdated = DateTime.Now;

                ReportProgress($"Generated requirements file at {filePath}");
                return true;
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
        /// <param name="environment">The environment to update</param>
        /// <returns>True if successful</returns>
        public bool UpdateVirtualEnvironmentWithRequirementsFile(PythonVirtualEnvironment environment)
        {
            if (environment == null || _isBusy)
                return false;

            if (string.IsNullOrEmpty(environment.RequirementsFile) || !File.Exists(environment.RequirementsFile))
            {
                ReportError($"Requirements file not found for environment {environment.Name}");
                return false;
            }

            // Store current environment
            var originalEnvironment = _currentEnvironment;

            try
            {
                // Set the target environment as current
                SetActiveEnvironment(environment);

                // Install packages from the requirements file
                return InstallFromRequirementsFile(environment.RequirementsFile);
            }
            finally
            {
                // Restore original environment if needed
                if (originalEnvironment != null && originalEnvironment.ID != environment.ID)
                {
                    SetActiveEnvironment(originalEnvironment);
                }
            }
        }

        /// <summary>
        /// Reads package requirements from a requirements.txt file
        /// </summary>
        private Dictionary<string, string> ReadRequirementsFile(string filePath)
        {
            var requirements = new Dictionary<string, string>();

            try
            {
                foreach (var line in File.ReadAllLines(filePath))
                {
                    string trimmedLine = line.Trim();

                    // Skip comments and empty lines
                    if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith("#"))
                        continue;

                    // Parse package specs (supports format like: package==1.0.0)
                    string packageName;
                    string version = string.Empty;

                    // Common requirement formats: package==1.0.0, package>=1.0.0, etc.
                    int specifierIndex = trimmedLine.IndexOfAny(new[] { '=', '>', '<', '~' });
                    if (specifierIndex > 0)
                    {
                        packageName = trimmedLine.Substring(0, specifierIndex).Trim();
                        version = trimmedLine.Substring(specifierIndex).Trim();
                    }
                    else
                    {
                        packageName = trimmedLine;
                    }

                    // Add to requirements if not already present
                    if (!string.IsNullOrEmpty(packageName) && !requirements.ContainsKey(packageName))
                    {
                        requirements.Add(packageName, version);
                    }
                }
            }
            catch (Exception ex)
            {
                ReportError($"Error reading requirements file: {ex.Message}");
            }

            return requirements;
        }

        #endregion
        #region Package Category Management

        /// <summary>
        /// Gets packages in a specific category
        /// </summary>
        /// <param name="category">The category to filter by</param>
        /// <returns>List of packages in the category</returns>
        public ObservableBindingList<PackageDefinition> GetPackagesByCategory(PackageCategory category)
        {
            var filtered = new ObservableBindingList<PackageDefinition>();

            if (_currentEnvironment.InstalledPackages == null || _currentEnvironment.InstalledPackages.Count == 0)
            {
                return filtered;
            }

            foreach (var package in _currentEnvironment.InstalledPackages)
            {
                if (package.Category == category)
                {
                    filtered.Add(package);
                }
            }

            return filtered;
        }

        /// <summary>
        /// Sets the category for a specific package
        /// </summary>
        /// <param name="packageName">Name of the package</param>
        /// <param name="category">Category to assign</param>
        public void SetPackageCategory(string packageName, PackageCategory category)
        {
            if (string.IsNullOrEmpty(packageName) || _currentEnvironment.InstalledPackages == null)
                return;

            var package = _currentEnvironment.InstalledPackages.FirstOrDefault(p =>
                p.PackageName != null &&
                p.PackageName.Equals(packageName, StringComparison.OrdinalIgnoreCase));

            if (package != null)
            {
                package.Category = category;

                // If this package is in the current environment's installed packages, update there too
                if (_currentEnvironment?.InstalledPackages != null)
                {
                    var envPackage = _currentEnvironment.InstalledPackages.FirstOrDefault(p =>
                        p.PackageName != null &&
                        p.PackageName.Equals(packageName, StringComparison.OrdinalIgnoreCase));

                    if (envPackage != null)
                    {
                        envPackage.Category = category;
                    }
                }
            }
        }

        /// <summary>
        /// Updates categories for multiple packages at once
        /// </summary>
        /// <param name="packageCategories">Dictionary of package names and categories</param>
        public void UpdatePackageCategories(Dictionary<string, PackageCategory> packageCategories)
        {
            if (packageCategories == null || packageCategories.Count == 0 || _currentEnvironment.InstalledPackages == null)
                return;

            foreach (var kvp in packageCategories)
            {
                SetPackageCategory(kvp.Key, kvp.Value);
            }
        }

        /// <summary>
        /// Populates common package categories based on known package sets
        /// </summary>
        /// <returns>True if any packages were categorized</returns>
        public bool PopulateCommonPackageCategories()
        {
            if (_currentEnvironment.InstalledPackages == null || _currentEnvironment.InstalledPackages.Count == 0)
                return false;

            int categorizedCount = 0;

            // Initialize package sets if needed
            if (_packageSets == null)
            {
                _packageSets = PredefinedPackageSets.AllSets;
            }

            // Create category mapping from predefined sets
            var packageToCategory = new Dictionary<string, PackageCategory>(StringComparer.OrdinalIgnoreCase);
            foreach (var set in _packageSets.Values)
            {
                foreach (var package in set.Packages)
                {
                    if (!packageToCategory.ContainsKey(package))
                    {
                        packageToCategory[package] = set.Category;
                    }
                }
            }

            // Apply categories to packages
            foreach (var package in _currentEnvironment.InstalledPackages)
            {
                if (package.Category == PackageCategory.Uncategorized &&
                    packageToCategory.TryGetValue(package.PackageName, out var category))
                {
                    package.Category = category;
                    categorizedCount++;
                }
            }

            return categorizedCount > 0;
        }

        /// <summary>
        /// Suggests categories for packages based on their names and descriptions
        /// </summary>
        /// <param name="packageNames">List of package names to categorize</param>
        /// <returns>Dictionary mapping package names to suggested categories</returns>
        public async Task<Dictionary<string, PackageCategory>> SuggestCategoriesForPackages(IEnumerable<string> packageNames)
        {
            var suggestions = new Dictionary<string, PackageCategory>();

            if (packageNames == null || !packageNames.Any())
                return suggestions;

            try
            {
                // First use a simple keyword-based approach for common packages
                var keywordMap = new Dictionary<string, PackageCategory>
                {
                    { "numpy", PackageCategory.Math },
                    { "scipy", PackageCategory.Scientific },
                    { "pandas", PackageCategory.DataScience },
                    { "matplotlib", PackageCategory.Graphics },
                    { "scikit-learn", PackageCategory.MachineLearning },
                    { "tensorflow", PackageCategory.MachineLearning },
                    { "torch", PackageCategory.MachineLearning },
                    { "flask", PackageCategory.WebDevelopment },
                    { "django", PackageCategory.WebDevelopment },
                    { "pytest", PackageCategory.Testing },
                    { "opencv", PackageCategory.Graphics },
                    { "pillow", PackageCategory.Graphics },
                    { "sql", PackageCategory.Database },
                    { "db", PackageCategory.Database },
                    { "mongo", PackageCategory.Database },
                    { "redis", PackageCategory.Database },
                    { "requests", PackageCategory.Networking },
                    { "http", PackageCategory.Networking },
                    { "web", PackageCategory.WebDevelopment },
                    { "crypto", PackageCategory.Security },
                    { "secure", PackageCategory.Security },
                    { "test", PackageCategory.Testing },
                    { "doc", PackageCategory.Documentation },
                    { "ui", PackageCategory.UserInterface },
                    { "gui", PackageCategory.UserInterface },
                    { "audio", PackageCategory.AudioVideo },
                    { "video", PackageCategory.AudioVideo },
                    { "util", PackageCategory.Utilities },
                    { "helper", PackageCategory.Utilities },
                    { "file", PackageCategory.FileProcessing },
                    { "excel", PackageCategory.FileProcessing },
                    { "pdf", PackageCategory.FileProcessing },
                    { "lint", PackageCategory.DevTools },
                    { "debug", PackageCategory.DevTools }
                };

                // For each package, lookup additional info if available
                foreach (var packageName in packageNames)
                {
                    // First try simple keyword matching
                    bool found = false;
                    foreach (var keyword in keywordMap.Keys)
                    {
                        if (packageName.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            suggestions[packageName] = keywordMap[keyword];
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        // If a package is in our environment with a non-Uncategorized category, use that
                        var existingPackage = _currentEnvironment.InstalledPackages.FirstOrDefault(p =>
                            p.PackageName != null &&
                            p.PackageName.Equals(packageName, StringComparison.OrdinalIgnoreCase) &&
                            p.Category != PackageCategory.Uncategorized);

                        if (existingPackage != null)
                        {
                            suggestions[packageName] = existingPackage.Category;
                        }
                        else
                        {
                            // Check if it's in any predefined package set
                            foreach (var set in PredefinedPackageSets.AllSets.Values)
                            {
                                if (set.Packages.Any(p => p.Equals(packageName, StringComparison.OrdinalIgnoreCase)))
                                {
                                    suggestions[packageName] = set.Category;
                                    found = true;
                                    break;
                                }
                            }

                            // As a last resort, try to look up package info online
                            if (!found)
                            {
                                var onlineInfo = await CheckIfPackageExistsAsync(packageName);
                                if (onlineInfo != null && !string.IsNullOrEmpty(onlineInfo.Description))
                                {
                                    // Use description to suggest a category
                                    suggestions[packageName] = SuggestCategoryFromDescription(onlineInfo.Description);
                                }
                                else
                                {
                                    // Default to Uncategorized
                                    suggestions[packageName] = PackageCategory.Uncategorized;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ReportError($"Error suggesting categories: {ex.Message}");
            }

            return suggestions;
        }

        /// <summary>
        /// Suggests a category based on package description text
        /// </summary>
        private PackageCategory SuggestCategoryFromDescription(string description)
        {
            if (string.IsNullOrEmpty(description))
                return PackageCategory.Uncategorized;

            // Simple keyword-based categorization from description
            description = description.ToLowerInvariant();

            // Define keyword sets for each category
            var categoryKeywords = new Dictionary<PackageCategory, string[]> {
                { PackageCategory.Graphics, new[] { "image", "graphic", "plot", "chart", "visualization", "draw", "render" } },
                { PackageCategory.MachineLearning, new[] { "machine learning", "ml", "classifier", "deep learning", "neural network", "ai", "artificial intelligence", "prediction" } },
                { PackageCategory.DataScience, new[] { "data science", "analysis", "analytics", "dataframe", "dataset", "statistical", "statistics" } },
                { PackageCategory.WebDevelopment, new[] { "web", "http", "html", "css", "javascript", "api", "rest", "flask", "django" } },
                { PackageCategory.DevTools, new[] { "development", "debug", "linting", "ide", "editor", "build", "compilation", "refactor" } },
                { PackageCategory.Database, new[] { "database", "sql", "db", "orm", "query", "storage", "repository", "mongo", "redis", "postgres" } },
                { PackageCategory.Networking, new[] { "network", "http", "socket", "protocol", "client", "request", "internet", "url", "uri" } },
                { PackageCategory.Security, new[] { "security", "crypto", "encryption", "hash", "auth", "authentication", "authorization", "permission" } },
                { PackageCategory.Testing, new[] { "test", "unittest", "pytest", "mock", "fixture", "assertion", "quality assurance", "qa" } },
                { PackageCategory.Utilities, new[] { "utility", "helper", "tool", "common", "convenience" } },
                { PackageCategory.Scientific, new[] { "scientific", "science", "research", "experiment", "academic", "physics", "chemistry", "biology", "simulation" } },
                { PackageCategory.Math, new[] { "math", "matrix", "vector", "numerical", "algebra", "calculus", "arithmetic", "algorithm" } },
                { PackageCategory.UserInterface, new[] { "ui", "gui", "interface", "widget", "window", "dialog", "form", "input" } },
                { PackageCategory.AudioVideo, new[] { "audio", "video", "sound", "media", "stream", "codec", "player", "multimedia" } },
                { PackageCategory.Documentation, new[] { "documentation", "doc", "sphinx", "help", "manual", "reference" } },
                { PackageCategory.FileProcessing, new[] { "file", "io", "csv", "excel", "pdf", "format", "parser", "serialization", "deserialization" } }
            };

            // Count keyword matches for each category
            var scores = new Dictionary<PackageCategory, int>();
            foreach (var category in categoryKeywords.Keys)
            {
                scores[category] = 0;
                foreach (var keyword in categoryKeywords[category])
                {
                    if (description.Contains(keyword))
                    {
                        scores[category]++;
                    }
                }
            }

            // Return the category with the highest score, or Uncategorized if no matches
            return scores.OrderByDescending(s => s.Value).FirstOrDefault().Value > 0
                ? scores.OrderByDescending(s => s.Value).First().Key
                : PackageCategory.Uncategorized;
        }

        #endregion
        #region Package Set Management

        /// <summary>
        /// Installs all packages from a predefined package set
        /// </summary>
        /// <param name="setName">Name of the package set to install</param>
        /// <returns>True if all packages were installed successfully</returns>
        public bool InstallPackageSet(string setName)
        {
            if (string.IsNullOrEmpty(setName) || _isBusy || !ValidateSessionAndEnvironment())
                return false;

            _isBusy = true;
            try
            {
                // Initialize package sets if needed
                if (_packageSets == null)
                {
                    _packageSets = PredefinedPackageSets.AllSets;
                }

                // Find the requested package set
                if (!_packageSets.TryGetValue(setName.ToLowerInvariant(), out var packageSet))
                {
                    ReportError($"Package set '{setName}' not found.");
                    return false;
                }

                // Temporarily disable auto-updates during batch operation
                bool originalAutoUpdate = _currentEnvironment.AutoUpdateRequirements;
                _currentEnvironment.AutoUpdateRequirements = false;

                // Install packages from the set
                bool success = true;
                int totalPackages = packageSet.Packages.Count;
                int current = 0;

                ReportProgress($"Installing {totalPackages} packages from set '{packageSet.Name}'...");

                foreach (var packageName in packageSet.Packages)
                {
                    current++;
                    string packageSpec = packageName;

                    // Add version constraint if specified
                    if (packageSet.Versions.TryGetValue(packageName, out var version) && !string.IsNullOrEmpty(version))
                    {
                        packageSpec = $"{packageName}{version}";
                    }

                    ReportProgress($"Installing {packageSpec} ({current}/{totalPackages})");
                    bool installResult = InstallNewPackageAsync(packageSpec);

                    if (!installResult)
                    {
                        ReportError($"Failed to install {packageSpec}");
                        success = false;
                    }
                }

                // Restore original auto-update setting
                _currentEnvironment.AutoUpdateRequirements = originalAutoUpdate;

                // If auto-update is enabled, update the requirements file
                if (originalAutoUpdate && !string.IsNullOrEmpty(_currentEnvironment.RequirementsFile))
                {
                    GenerateRequirementsFile(_currentEnvironment.RequirementsFile);
                }

                // Refresh packages after installation
                RefreshAllPackagesAsync();

                ReportProgress($"Completed installing package set '{packageSet.Name}'. {(success ? "All packages installed successfully." : "Some packages failed to install.")}");
                return success;
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
        /// <returns>Dictionary of set names and package lists</returns>
        public Dictionary<string, List<string>> GetAvailablePackageSets()
        {
            // Initialize package sets if needed
            if (_packageSets == null)
            {
                _packageSets = PredefinedPackageSets.AllSets;
            }

            var results = new Dictionary<string, List<string>>();

            foreach (var kvp in _packageSets)
            {
                results[kvp.Key] = kvp.Value.Packages.ToList();
            }

            return results;
        }

        /// <summary>
        /// Creates a new package set from the currently installed packages
        /// </summary>
        /// <param name="setName">Name for the new package set</param>
        /// <param name="description">Description for the package set</param>
        /// <returns>True if the set was created successfully</returns>
        public bool SavePackageSetFromCurrentEnvironment(string setName, string description = "")
        {
            if (string.IsNullOrEmpty(setName) || !ValidateSessionAndEnvironment())
                return false;

            try
            {
                // Ensure we have the latest package data
                RefreshAllPackagesAsync();

                if (_currentEnvironment.InstalledPackages == null || _currentEnvironment.InstalledPackages.Count == 0)
                {
                    ReportError("No packages found in current environment to save as a package set.");
                    return false;
                }

                // Determine the dominant category in the current packages
                var categoryCount = _currentEnvironment.InstalledPackages
                    .GroupBy(p => p.Category)
                    .Select(g => new { Category = g.Key, Count = g.Count() })
                    .OrderByDescending(g => g.Count)
                    .ToList();

                var dominantCategory = categoryCount.First().Category;

                // Create the package set
                var packageSet = new PackageSet
                {
                    Name = setName,
                    Description = string.IsNullOrEmpty(description)
                        ? $"Package set created from {_currentEnvironment.Name} on {DateTime.Now}"
                        : description,
                    Category = dominantCategory,
                    Packages = _currentEnvironment.InstalledPackages.Select(p => p.PackageName).ToList(),
                    Versions = _currentEnvironment.InstalledPackages
                        .Where(p => !string.IsNullOrEmpty(p.Version))
                        .ToDictionary(
                            p => p.PackageName,
                            p => $"=={p.Version}"
                        )
                };

                // Save to a file if requirements directory exists
                string requirementsDir = Path.Combine(
                    _beepService.DMEEditor.ConfigEditor.ConfigPath,
                    "PythonRequirements");

                if (!Directory.Exists(requirementsDir))
                {
                    Directory.CreateDirectory(requirementsDir);
                }

                string outputPath = Path.Combine(requirementsDir, $"{setName.Replace(" ", "_")}.txt");

                // Generate the requirements file content
                string content = packageSet.ToRequirementsText(true);
                File.WriteAllText(outputPath, content);

                ReportProgress($"Saved package set '{setName}' with {packageSet.Packages.Count} packages to {outputPath}");
                return true;
            }
            catch (Exception ex)
            {
                ReportError($"Error saving package set: {ex.Message}");
                return false;
            }
        }

        #endregion
        #region Helper Methods

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

        private void SynchronizePackages(ObservableBindingList<PackageDefinition> sourcePackages)
        {
            if (sourcePackages == null)
                return;

            // Clear existing packages
            _currentEnvironment.InstalledPackages.Clear();

            // Add all packages from source
            foreach (var package in sourcePackages)
            {
                _currentEnvironment.InstalledPackages.Add(package);
            }
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
        #region Helper Methods

        /// <summary>
        /// Sets the active environment and session for package operations
        /// </summary>
        /// <param name="environment">The environment to activate</param>
        /// <param name="session">Optional session to use (null for admin session)</param>
        public void SetActiveEnvironment(PythonVirtualEnvironment environment, PythonSessionInfo session = null)
        {
            if (environment == null)
                return;

            _currentEnvironment = environment;

            if (session != null)
            {
                _currentSession = session;
            }
            else
            {
                // Use admin session from virtual env manager
                _currentSession = _virtualEnvmanager.GetPackageManagementSession(environment);
            }

            // Initialize requirements file if not set
            if (string.IsNullOrEmpty(environment.RequirementsFile) && !string.IsNullOrEmpty(environment.Path))
            {
                string defaultPath = Path.Combine(environment.Path, "requirements.txt");
                if (File.Exists(defaultPath))
                {
                    environment.RequirementsFile = defaultPath;
                    environment.RequirementsLastUpdated = File.GetLastWriteTime(defaultPath);
                }
            }

            // Refresh packages in this environment
            RefreshAllPackagesAsync();
        }

      

        private void ReportProgress(string message)
        {
            Progress?.Report(new PassedArgs
            {
                Messege = message,
                EventType = "Info"
            });
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

                // Clean up unmanaged resources

                _isDisposed = true;
            }
        }

        #endregion
    }
}
