using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Beep.Python.Model;
using Beep.Python.RuntimeEngine.Helpers;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Container.Services;

namespace Beep.Python.RuntimeEngine.PackageManagement
{
    /// <summary>
    /// Handles core Python package management operations like install, uninstall, and update.
    /// </summary>
    public class PackageOperationManager
    {
        private readonly IBeepService _beepService;
        private readonly IPythonRunTimeManager _pythonRuntime;
        private readonly IPythonVirtualEnvManager _virtualEnvManager;
        private readonly HttpClient _httpClient;
        private readonly IProgress<PassedArgs> _progress;

        public PackageOperationManager(
            IBeepService beepService,
            IPythonRunTimeManager pythonRuntime,
            IPythonVirtualEnvManager virtualEnvManager,
            IProgress<PassedArgs> progress = null)
        {
            _beepService = beepService ?? throw new ArgumentNullException(nameof(beepService));
            _pythonRuntime = pythonRuntime ?? throw new ArgumentNullException(nameof(pythonRuntime));
            _virtualEnvManager = virtualEnvManager ?? throw new ArgumentNullException(nameof(virtualEnvManager));
            _progress = progress;
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        /// <summary>
        /// Runs a package management command in the specified environment using pip or conda
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
                // Get an admin session for package management
                var adminSession = _virtualEnvManager.GetPackageManagementSession(environment);
                if (adminSession == null)
                {
                    ReportError("Failed to obtain admin session for package management");
                    return string.Empty;
                }

                // Build the command 
                string packageCommand = BuildPackageCommand(command, action, useConda);

                // Execute the command
                var result = await _pythonRuntime.ExecuteManager.RunPythonCommandLineAsync(
                    _progress,
                    packageCommand,
                    useConda || environment.PythonBinary == PythonBinary.Conda,
                    adminSession,
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
        /// Installs a package in the specified environment
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
                string result = await RunPackageCommandAsync(
                    packageName, 
                    PackageAction.Install, 
                    environment, 
                    environment.PythonBinary == PythonBinary.Conda);

                return !string.IsNullOrEmpty(result) && !result.Contains("ERROR:");
            }
            catch (Exception ex)
            {
                ReportError($"Failed to install package {packageName}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Uninstalls a package from the specified environment
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
                string result = await RunPackageCommandAsync(
                    packageName, 
                    PackageAction.Remove, 
                    environment, 
                    environment.PythonBinary == PythonBinary.Conda);

                return !string.IsNullOrEmpty(result) && !result.Contains("ERROR:");
            }
            catch (Exception ex)
            {
                ReportError($"Failed to uninstall package {packageName}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Upgrades a package in the specified environment
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
                // Special case for pip itself
                PackageAction action = packageName.Equals("pip", StringComparison.OrdinalIgnoreCase)
                    ? PackageAction.UpgradePackager
                    : PackageAction.Update;

                string result = await RunPackageCommandAsync(
                    packageName, 
                    action, 
                    environment,
                    environment.PythonBinary == PythonBinary.Conda);

                return !string.IsNullOrEmpty(result) && !result.Contains("ERROR:");
            }
            catch (Exception ex)
            {
                ReportError($"Failed to upgrade package {packageName}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets information about an installed package (version, description, etc.)
        /// </summary>
        public async Task<PackageDefinition> GetPackageInfoAsync(string packageName, PythonVirtualEnvironment environment)
        {
            if (string.IsNullOrEmpty(packageName) || environment == null)
            {
                return null;
            }

            try
            {
                // Get admin session 
                var adminSession = _virtualEnvManager.GetPackageManagementSession(environment);
                if (adminSession == null)
                {
                    ReportError("Failed to obtain admin session for package info");
                    return null;
                }

                // Python code to get package info
                string packageInfoScript = $@"
import json
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
    except (importlib.metadata.PackageNotFoundError, KeyError) as e:
        print(json.dumps({{'error': str(e)}}))

    # Convert to JSON
    result = json.dumps(package_info if package_info else {{}})
    print(result)
except Exception as e:
    print(json.dumps({{'error': str(e)}}))
";

                // Execute the script
                var output = await _pythonRuntime.ExecuteManager.RunPythonCodeAndGetOutput(
                    _progress,
                    packageInfoScript,
                    adminSession);

                if (string.IsNullOrEmpty(output) || output.Contains("null") || output.Contains("error"))
                {
                    return null;
                }

                try
                {
                    // Parse the JSON output
                    var packageInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(output);
                    if (packageInfo != null && packageInfo.ContainsKey("name"))
                    {
                        // Check online for latest version
                        var onlinePackage = await CheckIfPackageExistsAsync(packageName);

                        // Create package definition
                        var package = new PackageDefinition
                        {
                            PackageName = packageInfo["name"],
                            Version = packageInfo["version"],
                            Updateversion = onlinePackage?.Version ?? packageInfo["version"],
                            Description = packageInfo.ContainsKey("summary") ? packageInfo["summary"] : "",
                            Installpath = packageInfo.ContainsKey("location") ? packageInfo["location"] : "",
                            Status = PackageStatus.Installed,
                            Buttondisplay = DetermineButtonDisplay(packageInfo["version"], onlinePackage?.Version)
                        };

                        return package;
                    }
                }
                catch (Exception ex)
                {
                    ReportError($"Error parsing package info for {packageName}: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                ReportError($"Failed to get package info for {packageName}: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Gets information about all installed packages in an environment
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
                // Get admin session
                var adminSession = _virtualEnvManager.GetPackageManagementSession(environment);
                if (adminSession == null)
                {
                    ReportError("Failed to obtain admin session for package listing");
                    return packages;
                }

                // Python code to get all installed packages
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
            pass
    
    # Convert to JSON
    result = json.dumps(packages)
    print(result)
except Exception as e:
    print(json.dumps({'error': str(e)}))
";

                // Execute the script
                var output = await _pythonRuntime.ExecuteManager.RunPythonCodeAndGetOutput(
                    _progress,
                    packageListScript,
                    adminSession);

                if (string.IsNullOrEmpty(output) || output.Contains("error"))
                {
                    ReportError("Failed to get package list from Python environment");
                    return packages;
                }

                // Parse the JSON output
                var packageList = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(output);
                if (packageList != null)
                {
                    bool isInternetAvailable = PythonRunTimeDiagnostics.CheckNet();

                    // Process packages in batches
                    int batchSize = 10;
                    for (int i = 0; i < packageList.Count; i += batchSize)
                    {
                        var batch = packageList.Skip(i).Take(batchSize).ToList();
                        
                        foreach (var packageInfo in batch)
                        {
                            if (packageInfo.ContainsKey("name") && packageInfo.ContainsKey("version"))
                            {
                                string packageName = packageInfo["name"];
                                string packageVersion = packageInfo["version"];

                                ReportProgress($"Processing package {packageName} ({i + 1}/{packageList.Count})");

                                // Check online for latest version if internet is available
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
                                    Buttondisplay = DetermineButtonDisplay(packageVersion, onlinePackage?.Version)
                                };

                                packages.Add(packageDef);
                            }
                        }

                        // Allow UI to update between batches
                        await Task.Delay(1);
                    }
                }
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
                            Description = packageData.info.description,
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

        #region Helper Methods

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

        private void ReportProgress(string message)
        {
            _progress?.Report(new PassedArgs { Messege = message });
            
            // Log to editor if available
            _beepService.DMEEditor?.AddLogMessage("Package Manager", message, DateTime.Now, -1, null, Errors.Ok);
        }

        private void ReportError(string message)
        {
            _progress?.Report(new PassedArgs
            {
                Messege = message,
                EventType = "Error",
                Flag = Errors.Failed
            });

            // Log to editor
            _beepService.DMEEditor?.AddLogMessage("Package Manager", message, DateTime.Now, -1, null, Errors.Failed);
        }

        #endregion
    }
}