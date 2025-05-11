using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Beep.Python.Model;
using Beep.Python.RuntimeEngine.ViewModels;
using Newtonsoft.Json;
using TheTechIdea.Beep.Container.Services;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Addin;

namespace Beep.Python.RuntimeEngine
{
    public class PythonPackageManager : PythonBaseViewModel
    {
        /// <summary>
        /// The Python virtual environment in which all package operations will run.
        /// </summary>
        public PythonVirtualEnvironment Environment { get; private set; }

        /// <summary>
        /// The Python session associated with the current scope.
        /// </summary>
        public PythonSessionInfo Session { get; private set; }

        /// <summary>
        /// Constructs a new PackageManager tied to a specific session and environment.
        /// </summary>
        public PythonPackageManager(
            IBeepService beepService,
            IPythonRunTimeManager pythonRuntimeManager,
            PythonSessionInfo session,
            PythonVirtualEnvironment environment)
            : base(beepService, pythonRuntimeManager)
        {
            Session = session ?? throw new ArgumentNullException(nameof(session));
            Environment = environment ?? throw new ArgumentNullException(nameof(environment));
        }

        /// <summary>
        /// Ensures that both Session and Environment are valid, initializes the environment if needed,
        /// and creates a scoped Python execution context for the session.
        /// </summary>
        private bool ValidateSessionAndEnvironment()
        {
            if (Session == null)
            {
                ReportProgress("Python session is not assigned.", Errors.Failed);
                return false;
            }

            if (Environment == null)
            {
                ReportProgress("Python environment is not assigned.", Errors.Failed);
                return false;
            }

            // Ensure the virtual environment is initialized
            if (!PythonRuntime.Initialize(Environment))
            {
                ReportProgress("Failed to initialize Python environment.", Errors.Failed);
                return false;
            }

            // Ensure there's a scope for this session in the given environment
            if (!PythonRuntime.CreateScope(Session, Environment))
            {
                ReportProgress("Failed to create session scope in environment.", Errors.Failed);
                return false;
            }

            return true;
        }

        #region Install / Uninstall / Upgrade



        /// <summary>
        /// Installs a Python package and handles errors
        /// </summary>
        public async Task<PackageOperationResult> InstallPackageAsync(string packageName, bool useConda = false)
        {
            if (string.IsNullOrWhiteSpace(packageName))
            {
                return new PackageOperationResult(false, "Package name cannot be empty.", packageName);
            }

            if (!ValidateSessionAndEnvironment())
            {
                return new PackageOperationResult(false, "Environment validation failed.", packageName);
            }

            try
            {
                string command;

                if (useConda)
                {
                    command = $"conda install {packageName} -y";
                }
                else
                {
                    command = $"pip install {packageName}";
                }

                var output = await PythonRuntime.RunPythonCommandLineAsync(Progress, command, useConda, Session, Environment);

                return ParseInstallationOutput(output, packageName, command);
            }
            catch (Exception ex)
            {
                ReportProgress($"Exception installing package {packageName}: {ex.Message}", Errors.Failed);
                return new PackageOperationResult(false, $"Installation failed with error: {ex.Message}", packageName);
            }
        }

        /// <summary>
        /// Installs a specific version of a Python package
        /// </summary>
        public async Task<PackageOperationResult> InstallPackageAsync(string packageName, string version, bool useConda = false)
        {
            if (string.IsNullOrWhiteSpace(packageName))
            {
                return new PackageOperationResult(false, "Package name cannot be empty.", packageName);
            }

            if (string.IsNullOrWhiteSpace(version))
            {
                return new PackageOperationResult(false, "Version cannot be empty.", packageName);
            }

            if (!ValidateSessionAndEnvironment())
            {
                return new PackageOperationResult(false, "Environment validation failed.", packageName);
            }

            try
            {
                string command;

                if (useConda)
                {
                    // Conda uses a single equals sign for version specification
                    command = $"conda install {packageName}={version} -y";
                }
                else
                {
                    // Pip uses double equals
                    command = $"pip install {packageName}=={version}";
                }

                var output = await PythonRuntime.RunPythonCommandLineAsync(Progress, command, useConda, Session, Environment);

                return ParseInstallationOutput(output, packageName, command);
            }
            catch (Exception ex)
            {
                ReportProgress($"Exception installing package {packageName} version {version}: {ex.Message}", Errors.Failed);
                return new PackageOperationResult(false, $"Installation failed with error: {ex.Message}", packageName);
            }
        }


        /// <summary>
        /// Removes a Python package and handles errors
        /// </summary>
        public async Task<PackageOperationResult> RemovePackageAsync(string packageName, bool useConda = false)
        {
            if (string.IsNullOrWhiteSpace(packageName))
            {
                return new PackageOperationResult(false, "Package name cannot be empty.", packageName);
            }

            if (!ValidateSessionAndEnvironment())
            {
                return new PackageOperationResult(false, "Environment validation failed.", packageName);
            }

            try
            {
                string command;

                if (useConda)
                {
                    command = $"conda remove {packageName} -y";
                }
                else
                {
                    command = $"pip uninstall -y {packageName}";
                }

                var output = await PythonRuntime.RunPythonCommandLineAsync(Progress, command, useConda, Session, Environment);

                // Check if uninstall was successful
                if ((useConda && (output.Contains("successfully removed") || output.Contains("packages found"))) ||
                    (!useConda && (output.Contains("Successfully uninstalled") || output.Contains("not installed"))))
                {
                    ReportProgress($"Successfully removed package: {packageName}", Errors.Ok);
                    return new PackageOperationResult(true, "Package removed successfully", packageName)
                    {
                        Details = output,
                        CommandExecuted = command
                    };
                }
                else
                {
                    ReportProgress($"Failed to remove package: {packageName}. Details: {output}", Errors.Failed);
                    return new PackageOperationResult(false, "Package removal failed", packageName)
                    {
                        Details = output,
                        CommandExecuted = command
                    };
                }
            }
            catch (Exception ex)
            {
                ReportProgress($"Exception removing package {packageName}: {ex.Message}", Errors.Failed);
                return new PackageOperationResult(false, $"Removal failed with error: {ex.Message}", packageName);
            }
        }

        /// <summary>
        /// Upgrades a Python package to the latest version
        /// </summary>
        public async Task<PackageOperationResult> UpgradePackageAsync(string packageName, bool useConda = false)
        {
            if (string.IsNullOrWhiteSpace(packageName))
            {
                return new PackageOperationResult(false, "Package name cannot be empty.", packageName);
            }

            if (!ValidateSessionAndEnvironment())
            {
                return new PackageOperationResult(false, "Environment validation failed.", packageName);
            }

            try
            {
                string command;

                if (useConda)
                {
                    command = $"conda update {packageName} -y";
                }
                else
                {
                    command = $"pip install --upgrade {packageName}";
                }

                var output = await PythonRuntime.RunPythonCommandLineAsync(Progress, command, useConda, Session, Environment);

                // Parse output based on the package manager used
                bool isSuccess = false;
                string message = "";

                if (useConda)
                {
                    isSuccess = output.Contains("successfully") ||
                               output.Contains("All requested packages already installed") ||
                               output.Contains("All requested packages already updated");

                    if (!isSuccess)
                    {
                        var errorMatch = Regex.Match(output, @"(Error:|PackagesNotFoundError:)([^\n]*)", RegexOptions.IgnoreCase);
                        message = errorMatch.Success ?
                            $"Conda upgrade failed: {errorMatch.Groups[2].Value.Trim()}" :
                            "Conda upgrade failed with an unknown error.";
                    }
                    else
                    {
                        message = "Package successfully updated with conda";
                    }
                }
                else
                {
                    // Use the existing pip parsing logic
                    var result = ParseInstallationOutput(output, packageName, command);
                    isSuccess = result.Success;
                    message = result.Message;

                    // Early return since we're reusing the existing parse method
                    return result;
                }

                ReportProgress(message, isSuccess ? Errors.Ok : Errors.Failed);
                return new PackageOperationResult(isSuccess, message, packageName)
                {
                    Details = output,
                    CommandExecuted = command
                };
            }
            catch (Exception ex)
            {
                ReportProgress($"Exception upgrading package {packageName}: {ex.Message}", Errors.Failed);
                return new PackageOperationResult(false, $"Upgrade failed with error: {ex.Message}", packageName);
            }
        }

        /// <summary>
        /// Parses pip command output to determine success or failure
        /// </summary>
        private PackageOperationResult ParseInstallationOutput(string output, string packageName, string command)
        {
            if (string.IsNullOrWhiteSpace(output))
            {
                ReportProgress($"No output received from pip for package: {packageName}", Errors.Warning);
                return new PackageOperationResult(false, "No output received from pip command", packageName)
                {
                    CommandExecuted = command
                };
            }

            // Possible success indicators
            if (output.Contains("Successfully installed") ||
                output.Contains("Requirement already satisfied"))
            {
                ReportProgress($"Successfully installed/upgraded package: {packageName}", Errors.Ok);
                return new PackageOperationResult(true, "Package operation completed successfully", packageName)
                {
                    Details = output,
                    CommandExecuted = command
                };
            }

            // Look for common error patterns
            if (output.Contains("ERROR:") ||
                output.Contains("Error:") ||
                output.Contains("Could not find a version that satisfies the requirement") ||
                output.Contains("No matching distribution found"))
            {
                // Extract specific error message using regex
                var errorMatch = Regex.Match(output, @"ERROR:.*?:(.*?)(?=\r\n|\n|$)", RegexOptions.Singleline);
                var errorMessage = errorMatch.Success ? errorMatch.Groups[1].Value.Trim() : "Unknown error";

                ReportProgress($"Failed to install package: {packageName}. Error: {errorMessage}", Errors.Failed);
                return new PackageOperationResult(false, $"Installation failed: {errorMessage}", packageName)
                {
                    Details = output,
                    CommandExecuted = command
                };
            }

            // If we can't determine success or failure
            ReportProgress($"Ambiguous result installing package: {packageName}. Please check the output.", Errors.Warning);
            return new PackageOperationResult(false, "Could not determine if operation was successful", packageName)
            {
                Details = output,
                CommandExecuted = command
            };
        }

        #endregion
        #region Querying Installed Packages

        /// <summary>
        /// Gets the installed version of a given package.
        /// </summary>
        public async Task<string> GetPackageVersionAsync(string packageName, bool useConda = false)
        {
            if (!ValidateSessionAndEnvironment()) return null;

            try
            {
                string code;

                if (useConda)
                {
                    code = $@"
import subprocess
import json
try:
    # For conda, we need to use 'conda list' to get package info
    output = subprocess.check_output(['conda', 'list', '{packageName}', '--json'], text=True)
    packages = json.loads(output)
    if packages:
        for pkg in packages:
            if pkg.get('name', '').lower() == '{packageName}'.lower():
                print(pkg.get('version', 'not found'))
                break
        else:
            print('not found: package not in environment')
    else:
        print('not found: empty result')
except Exception as e:
    print(f'not found: {{str(e)}}')";
                }
                else
                {
                    code = $@"
import pkg_resources
try:
    version = pkg_resources.get_distribution('{packageName}').version
    print(version)
except Exception as e:
    print(f'not found: {{str(e)}}')";
                }

                var output = await PythonRuntime.RunPythonForUserAsync(
                    Session,
                    Environment.Name,
                    code,
                    Progress);

                return output?.Trim();
            }
            catch (Exception ex)
            {
                ReportProgress($"Error getting package version for {packageName}: {ex.Message}", Errors.Failed);
                return null;
            }
        }

        /// <summary>
        /// Writes the output of package list to a requirements file.
        /// </summary>
        public async Task<bool> FreezePackagesAsync(string outputPath, bool useConda = false)
        {
            if (!ValidateSessionAndEnvironment()) return false;

            try
            {
                string code;

                if (useConda)
                {
                    code = $@"
import subprocess
import os

try:
    # Create directory if it doesn't exist
    os.makedirs(os.path.dirname(r'{outputPath}'), exist_ok=True)
    
    with open(r'{outputPath}', 'w') as f:
        # For conda, we export the environment
        result = subprocess.run(['conda', 'list', '--export'], capture_output=True, text=True)
        if result.returncode == 0:
            f.write(result.stdout)
            print('SUCCESS: Conda environment exported to ' + r'{outputPath}')
        else:
            print(f'ERROR: Conda export failed: {{result.stderr}}')
except Exception as e:
    print(f'ERROR: {{str(e)}}')";
                }
                else
                {
                    code = $@"
import subprocess
import os

try:
    # Create directory if it doesn't exist
    os.makedirs(os.path.dirname(r'{outputPath}'), exist_ok=True)
    
    with open(r'{outputPath}', 'w') as f:
        subprocess.run(['pip', 'freeze'], stdout=f, check=True)
    print('SUCCESS: Requirements written to ' + r'{outputPath}')
except Exception as e:
    print(f'ERROR: {{str(e)}}')";
                }

                var output = await PythonRuntime.RunPythonForUserAsync(
                    Session,
                    Environment.Name,
                    code,
                    Progress);

                if (output != null && output.Contains("SUCCESS:"))
                {
                    ReportProgress($"Successfully saved package list to {outputPath}", Errors.Ok);
                    return true;
                }
                else
                {
                    ReportProgress($"Failed to save package list: {output}", Errors.Failed);
                    return false;
                }
            }
            catch (Exception ex)
            {
                ReportProgress($"Error saving package list: {ex.Message}", Errors.Failed);
                return false;
            }
        }

        /// <summary>
        /// Retrieves the list of installed packages and returns as objects.
        /// </summary>
        public async Task<List<PackageDefinition>> GetPackageListAsync(bool useConda = false)
        {
            if (!ValidateSessionAndEnvironment()) return new List<PackageDefinition>();

            try
            {
                string code;

                if (useConda)
                {
                    code = @"
import subprocess
import json
try:
    output = subprocess.check_output(['conda', 'list', '--json'], text=True)
    print(output)
except Exception as e:
    print(f'{{""error"": ""Failed to list conda packages: {str(e)}""}}')";
                }
                else
                {
                    code = @"
import subprocess
import json
try:
    output = subprocess.check_output(['pip', 'list', '--format=json'], text=True)
    print(output)
except Exception as e:
    print(f'{{""error"": ""Failed to list pip packages: {str(e)}""}}')";
                }

                var jsonOutput = await PythonRuntime.RunPythonForUserAsync(
                    Session,
                    Environment.Name,
                    code,
                    Progress);

                if (string.IsNullOrWhiteSpace(jsonOutput))
                {
                    ReportProgress("No output received from package list command", Errors.Warning);
                    return new List<PackageDefinition>();
                }

                if (jsonOutput.Contains("\"error\""))
                {
                    var error = JsonConvert.DeserializeAnonymousType(jsonOutput, new { error = "" });
                    ReportProgress($"Error getting package list: {error?.error}", Errors.Failed);
                    return new List<PackageDefinition>();
                }

                try
                {
                    if (useConda)
                    {
                        // Conda JSON format is different from pip
                        // Example format: [{"base_url":"https://repo.anaconda.com/pkgs/main","build_number":0,"build_string":"py39h06a4308_0",...}]
                        var condaPackages = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(jsonOutput);
                        return condaPackages.Select(item => new PackageDefinition
                        {
                            PackageTitle = item["name"]?.ToString(),
                            PackageName = item["name"]?.ToString(),
                            Version = item["version"]?.ToString(),
                            // You can add more properties specific to conda here if needed
                            Status = PackageStatus.Installed,
                        }).ToList();
                    }
                    else
                    {
                        // Standard pip format
                        var pipPackages = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(jsonOutput);
                        return pipPackages.Select(item => new PackageDefinition
                        {
                            PackageTitle = item["name"],
                            PackageName = item["name"],
                            Version = item["version"],
                            Status = PackageStatus.Installed
                        }).ToList();
                    }
                }
                catch (JsonException jsonEx)
                {
                    ReportProgress($"Failed to parse package list JSON: {jsonEx.Message}", Errors.Failed);
                    return new List<PackageDefinition>();
                }
            }
            catch (Exception ex)
            {
                ReportProgress($"Error getting package list: {ex.Message}", Errors.Failed);
                return new List<PackageDefinition>();
            }
        }

        /// <summary>
        /// Checks if a package is installed
        /// </summary>
        /// <param name="packageName">The name of the package to check</param>
        /// <param name="useConda">Whether to check in conda environment</param>
        /// <returns>True if the package is installed, false otherwise</returns>
        public async Task<bool> IsPackageInstalledAsync(string packageName, bool useConda = false)
        {
            var version = await GetPackageVersionAsync(packageName, useConda);
            return version != null && !version.StartsWith("not found");
        }

        /// <summary>
        /// Gets information about an installed package including dependencies
        /// </summary>
        /// <param name="packageName">Name of the package</param>
        /// <param name="useConda">Whether to use conda</param>
        /// <returns>Detailed package information as a string</returns>
        public async Task<string> GetPackageInfoAsync(string packageName, bool useConda = false)
        {
            if (!ValidateSessionAndEnvironment()) return null;

            try
            {
                string code;

                if (useConda)
                {
                    code = $@"
import subprocess
import json

try:
    # Get package info from conda
    output = subprocess.check_output(['conda', 'list', '{packageName}', '--verbose'], text=True)
    print(output)
except Exception as e:
    print(f'Error getting package info: {{str(e)}}')";
                }
                else
                {
                    code = $@"
import subprocess
try:
    # Get package info from pip
    output = subprocess.check_output(['pip', 'show', '{packageName}'], text=True)
    print(output)
except Exception as e:
    print(f'Error getting package info: {{str(e)}}')";
                }

                var output = await PythonRuntime.RunPythonForUserAsync(
                    Session,
                    Environment.Name,
                    code,
                    Progress);

                if (output != null && !output.Contains("Error getting package info"))
                {
                    return output;
                }
                else
                {
                    ReportProgress($"Failed to get info for package {packageName}: {output}", Errors.Warning);
                    return null;
                }
            }
            catch (Exception ex)
            {
                ReportProgress($"Error getting package info: {ex.Message}", Errors.Failed);
                return null;
            }
        }

        /// <summary>
        /// Installs packages from a requirements file
        /// </summary>
        /// <param name="requirementsFilePath">Path to requirements.txt file</param>
        /// <param name="useConda">Whether to use conda</param>
        /// <returns>Result of the operation</returns>
        public async Task<PackageOperationResult> InstallFromRequirementsFileAsync(string requirementsFilePath, bool useConda = false)
        {
            if (string.IsNullOrWhiteSpace(requirementsFilePath))
            {
                return new PackageOperationResult(false, "Requirements file path cannot be empty");
            }

            if (!ValidateSessionAndEnvironment())
            {
                return new PackageOperationResult(false, "Environment validation failed");
            }

            try
            {
                string command;

                if (useConda)
                {
                    // For conda, we use --file option but it works differently from pip
                    command = $"conda install --file \"{requirementsFilePath}\" -y";
                }
                else
                {
                    command = $"pip install -r \"{requirementsFilePath}\"";
                }

                var output = await PythonRuntime.RunPythonCommandLineAsync(Progress, command, useConda, Session, Environment);

                // Determine if the installation was successful
                bool isSuccess = useConda
                    ? !output.Contains("ERROR") && !output.Contains("FAILED")
                    : output.Contains("Successfully installed") || output.Contains("Requirement already satisfied");

                if (isSuccess)
                {
                    ReportProgress($"Successfully installed packages from {requirementsFilePath}", Errors.Ok);
                    return new PackageOperationResult(true, "Packages installed successfully")
                    {
                        Details = output,
                        CommandExecuted = command
                    };
                }
                else
                {
                    string errorDetails = ExtractErrorMessage(output, useConda);
                    ReportProgress($"Failed to install packages from {requirementsFilePath}: {errorDetails}", Errors.Failed);
                    return new PackageOperationResult(false, $"Installation failed: {errorDetails}")
                    {
                        Details = output,
                        CommandExecuted = command
                    };
                }
            }
            catch (Exception ex)
            {
                ReportProgress($"Exception installing packages from file: {ex.Message}", Errors.Failed);
                return new PackageOperationResult(false, $"Installation failed with error: {ex.Message}");
            }
        }

        /// <summary>
        /// Extracts the error message from pip or conda output
        /// </summary>
        private string ExtractErrorMessage(string output, bool isConda)
        {
            if (string.IsNullOrWhiteSpace(output))
                return "No output received";

            if (isConda)
            {
                var errorMatch = Regex.Match(output, @"(Error:|PackagesNotFoundError:|CondaError:)([^\n]*)", RegexOptions.IgnoreCase);
                return errorMatch.Success ? errorMatch.Groups[2].Value.Trim() : "Unknown conda error";
            }
            else
            {
                var errorMatch = Regex.Match(output, @"ERROR:.*?:(.*?)(?=\r\n|\n|$)", RegexOptions.Singleline);
                return errorMatch.Success ? errorMatch.Groups[1].Value.Trim() : "Unknown pip error";
            }
        }

        #endregion

        /// <summary>
        /// Reports progress with a message and error status
        /// </summary>
        /// <param name="message">The message to report</param>
        /// <param name="errorStatus">The error status (default: Ok)</param>
        protected void ReportProgress(string message, Errors errorStatus = Errors.Ok)
        {
            if (Progress != null)
            {
                Progress.Report(new PassedArgs
                {
                    Messege = message,
                    EventType = errorStatus == Errors.Ok ? "Info" : "Error"
                });

                // Also log to editor if available
                if (Editor != null)
                {
                    Editor.AddLogMessage("Python Package Manager",
                        message,
                        DateTime.Now,
                        -1,
                        null,
                        errorStatus);
                }
            }
        }
    }
}
