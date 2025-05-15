using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Beep.Python.Model;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Container.Services;

namespace Beep.Python.RuntimeEngine.PackageManagement
{
    /// <summary>
    /// Manages operations related to Python requirements files (requirements.txt)
    /// </summary>
    public class RequirementsFileManager
    {
        private readonly IBeepService _beepService;
        private readonly IPythonRunTimeManager _pythonRuntime;
        private readonly PackageOperationManager _packageOperations;
        private readonly IProgress<PassedArgs> _progress;

        public RequirementsFileManager(
            IBeepService beepService,
            IPythonRunTimeManager pythonRuntime,
            PackageOperationManager packageOperations,
            IProgress<PassedArgs> progress = null)
        {
            _beepService = beepService ?? throw new ArgumentNullException(nameof(beepService));
            _pythonRuntime = pythonRuntime ?? throw new ArgumentNullException(nameof(pythonRuntime));
            _packageOperations = packageOperations ?? throw new ArgumentNullException(nameof(packageOperations));
            _progress = progress;
        }

        /// <summary>
        /// Installs packages from a requirements file
        /// </summary>
        /// <param name="filePath">Path to the requirements file</param>
        /// <param name="environment">Target environment</param>
        /// <returns>True if successful</returns>
        public async Task<bool> InstallFromRequirementsFileAsync(string filePath, PythonVirtualEnvironment environment)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath) || environment == null)
            {
                ReportError($"Invalid requirements file path: {filePath}");
                return false;
            }

            try
            {
                // Read and parse the requirements file
                var requirements = ReadRequirementsFile(filePath);
                if (requirements.Count == 0)
                {
                    ReportProgress("No packages found in requirements file.");
                    return false;
                }

                // Temporarily disable auto-updates during batch operation
                bool originalAutoUpdate = environment.AutoUpdateRequirements;
                environment.AutoUpdateRequirements = false;

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
                    bool installResult = await _packageOperations.InstallPackageAsync(packageSpec, environment);

                    if (!installResult)
                    {
                        ReportError($"Failed to install {packageSpec}");
                        success = false;
                    }
                }

                // Restore original auto-update setting
                environment.AutoUpdateRequirements = originalAutoUpdate;

                // Set the requirements file path in the environment
                if (success && string.IsNullOrEmpty(environment.RequirementsFile))
                {
                    environment.RequirementsFile = filePath;
                    environment.RequirementsLastUpdated = DateTime.Now;
                }

                ReportProgress($"Completed installing packages from requirements file. {(success ? "All packages installed successfully." : "Some packages failed to install.")}");
                return success;
            }
            catch (Exception ex)
            {
                ReportError($"Error installing packages from requirements file: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Generates a requirements file from an environment's installed packages
        /// </summary>
        /// <param name="filePath">Output file path</param>
        /// <param name="environment">Source environment</param>
        /// <param name="includeVersions">Whether to include version constraints</param>
        /// <returns>True if successful</returns>
        public async Task<bool> GenerateRequirementsFileAsync(
            string filePath, 
            PythonVirtualEnvironment environment, 
            bool includeVersions = true)
        {
            if (string.IsNullOrEmpty(filePath) || environment == null)
            {
                ReportError("Invalid requirements file path or environment");
                return false;
            }

            try
            {
                // Ensure we have the latest package data
                var packages = await _packageOperations.GetAllPackagesAsync(environment);
                if (packages == null || packages.Count == 0)
                {
                    ReportError("No packages found in environment");
                    return false;
                }

                // Create directory if it doesn't exist
                string directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Build the requirements file content
                StringBuilder content = new StringBuilder();
                content.AppendLine($"# Requirements for {environment.Name}");
                content.AppendLine($"# Generated: {DateTime.Now}");
                content.AppendLine($"# Python version: {environment.PythonVersion}");
                content.AppendLine();

                // Add package entries
                foreach (var package in packages.OrderBy(p => p.PackageName))
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

                // Write the file
                File.WriteAllText(filePath, content.ToString());

                // Update the environment's requirements file information
                environment.RequirementsFile = filePath;
                environment.RequirementsLastUpdated = DateTime.Now;

                ReportProgress($"Generated requirements file at {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                ReportError($"Error generating requirements file: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Updates a virtual environment with packages from its associated requirements file
        /// </summary>
        /// <param name="environment">The environment to update</param>
        /// <returns>True if successful</returns>
        public async Task<bool> UpdateEnvironmentWithRequirementsFileAsync(PythonVirtualEnvironment environment)
        {
            if (environment == null)
            {
                ReportError("Environment cannot be null");
                return false;
            }

            if (string.IsNullOrEmpty(environment.RequirementsFile) || !File.Exists(environment.RequirementsFile))
            {
                ReportError($"Requirements file not found for environment {environment.Name}");
                return false;
            }

            try
            {
                // Install packages from the requirements file
                return await InstallFromRequirementsFileAsync(environment.RequirementsFile, environment);
            }
            catch (Exception ex)
            {
                ReportError($"Error updating environment from requirements file: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets the default path for storing requirements files
        /// </summary>
        public string GetDefaultRequirementsDirectory()
        {
            string configPath = _beepService.DMEEditor?.ConfigEditor?.ConfigPath;
            if (string.IsNullOrEmpty(configPath))
            {
                // Fallback to a standard location
                configPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Beep");
            }

            string requirementsDir = Path.Combine(configPath, "PythonRequirements");
            
            // Ensure directory exists
            if (!Directory.Exists(requirementsDir))
            {
                Directory.CreateDirectory(requirementsDir);
            }

            return requirementsDir;
        }

        #region Helper Methods

        /// <summary>
        /// Reads package requirements from a requirements.txt file
        /// </summary>
        /// <param name="filePath">Path to the requirements file</param>
        /// <returns>Dictionary of package name to version constraint</returns>
        public Dictionary<string, string> ReadRequirementsFile(string filePath)
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

        private void ReportProgress(string message)
        {
            _progress?.Report(new PassedArgs { Messege = message });
            
            // Log to editor if available
            _beepService.DMEEditor?.AddLogMessage("Requirements Manager", message, DateTime.Now, -1, null, Errors.Ok);
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
            _beepService.DMEEditor?.AddLogMessage("Requirements Manager", message, DateTime.Now, -1, null, Errors.Failed);
        }

        #endregion
    }
}