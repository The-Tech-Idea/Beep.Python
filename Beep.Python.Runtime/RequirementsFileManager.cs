using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;

namespace Beep.Python.Model
{
    /// <summary>
    /// Utility class for managing Python requirements.txt files
    /// </summary>
    public static class RequirementsFileManager
    {
        /// <summary>
        /// Updates a requirements file with the current installed packages
        /// </summary>
        /// <param name="environment">The Python environment to generate requirements for</param>
        /// <param name="requirementsFilePath">Path to the requirements file (uses environment.RequirementsFile if null)</param>
        /// <param name="includeVersions">Whether to include version constraints</param>
        /// <returns>True if the update was successful</returns>
        public static bool UpdateRequirementsFile(PythonVirtualEnvironment environment, string requirementsFilePath = null, bool includeVersions = true)
        {
            if (environment == null)
                return false;

            string filePath = requirementsFilePath ?? environment.RequirementsFile;

            // If no path specified and no default in environment, create one
            if (string.IsNullOrEmpty(filePath))
            {
                filePath = Path.Combine(environment.Path, "requirements.txt");
                environment.RequirementsFile = filePath;
            }

            try
            {
                // Build requirements file content
                StringBuilder content = new StringBuilder();

                // Add header
                content.AppendLine($"# Requirements for {environment.Name}");
                content.AppendLine($"# Generated: {DateTime.Now}");
                content.AppendLine($"# Python version: {environment.PythonVersion}");
                content.AppendLine();

                // Add installed packages with versions if available
                if (environment.InstalledPackages != null)
                {
                    foreach (var package in environment.InstalledPackages.OrderBy(p => p.PackageName))
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

                // Add additional requirements if any
                if (environment.AdditionalRequirements != null && environment.AdditionalRequirements.Count > 0)
                {
                    content.AppendLine();
                    content.AppendLine("# Additional requirements");
                    foreach (var req in environment.AdditionalRequirements)
                    {
                        content.AppendLine(req);
                    }
                }

                // Write the file
                File.WriteAllText(filePath, content.ToString());

                // Update timestamp
                environment.RequirementsLastUpdated = DateTime.Now;

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating requirements file: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Reads packages from a requirements file
        /// </summary>
        /// <param name="filePath">Path to the requirements file</param>
        /// <returns>Dictionary of package names and version constraints</returns>
        public static Dictionary<string, string> ReadRequirementsFile(string filePath)
        {
            var requirements = new Dictionary<string, string>();

            if (!File.Exists(filePath))
                return requirements;

            try
            {
                foreach (var line in File.ReadAllLines(filePath))
                {
                    // Skip comments and empty lines
                    string trimmedLine = line.Trim();
                    if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith("#"))
                        continue;

                    // Parse package specs
                    string packageName;
                    string version = string.Empty;

                    // Handle various requirement formats:
                    // package==1.0.0
                    // package>=1.0.0
                    // package<=1.0.0
                    // package~=1.0.0
                    // package
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

                    if (!string.IsNullOrEmpty(packageName) && !requirements.ContainsKey(packageName))
                    {
                        requirements.Add(packageName, version);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading requirements file: {ex.Message}");
            }

            return requirements;
        }

        /// <summary>
        /// Updates a requirements file with a specific package change
        /// </summary>
        /// <param name="requirementsFilePath">Path to the requirements file</param>
        /// <param name="packageName">Name of the package being changed</param>
        /// <param name="version">Version of the package, or null if removing</param>
        /// <param name="operation">Operation type: "add", "remove", or "update"</param>
        /// <returns>True if the update was successful</returns>
        public static bool UpdatePackageInRequirementsFile(string requirementsFilePath, string packageName,
            string version, string operation)
        {
            if (string.IsNullOrEmpty(requirementsFilePath) || string.IsNullOrEmpty(packageName))
                return false;

            try
            {
                // Create directory if it doesn't exist
                var directory = Path.GetDirectoryName(requirementsFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Load existing requirements
                var requirements = File.Exists(requirementsFilePath)
                    ? ReadRequirementsFile(requirementsFilePath)
                    : new Dictionary<string, string>();

                // Update based on operation
                switch (operation.ToLower())
                {
                    case "add":
                    case "update":
                        requirements[packageName] = !string.IsNullOrEmpty(version) ? $"=={version}" : string.Empty;
                        break;

                    case "remove":
                        if (requirements.ContainsKey(packageName))
                        {
                            requirements.Remove(packageName);
                        }
                        break;
                }

                // Write updated requirements
                StringBuilder content = new StringBuilder();
                content.AppendLine($"# Requirements file updated on {DateTime.Now}");
                content.AppendLine();

                foreach (var package in requirements.OrderBy(p => p.Key))
                {
                    if (string.IsNullOrEmpty(package.Value))
                    {
                        content.AppendLine(package.Key);
                    }
                    else
                    {
                        content.AppendLine($"{package.Key}{package.Value}");
                    }
                }

                File.WriteAllText(requirementsFilePath, content.ToString());
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating package in requirements file: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Generates a requirements file from a package set
        /// </summary>
        /// <param name="packageSet">The package set to use</param>
        /// <param name="outputPath">Path where to save the requirements file</param>
        /// <param name="includeVersions">Whether to include version constraints</param>
        /// <returns>True if successful</returns>
        public static bool GenerateRequirementsFromPackageSet(PackageSet packageSet, string outputPath, bool includeVersions = true)
        {
            if (packageSet == null || string.IsNullOrEmpty(outputPath))
                return false;

            try
            {
                string content = packageSet.ToRequirementsText(includeVersions);
                File.WriteAllText(outputPath, content);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating requirements from package set: {ex.Message}");
                return false;
            }
        }
    }
}
