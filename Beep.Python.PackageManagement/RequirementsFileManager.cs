using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Beep.Python.Model;
using Python.Runtime;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Container.Services;

namespace Beep.Python.RuntimeEngine.PackageManagement
{
    /// <summary>
    /// Enhanced requirements file manager with session management, virtual environment support,
    /// and comprehensive requirements file operations for Python package management
    /// </summary>
    public class RequirementsFileManager : IDisposable
    {
        #region Private Fields
        private readonly object _operationLock = new object();
        private volatile bool _isDisposed = false;
        
        private readonly IBeepService _beepService;
        private readonly IPythonRunTimeManager _pythonRuntime;
        private readonly PythonPackageManager _packageManager;
        private readonly IProgress<PassedArgs> _progress;
        
        // Session and Environment management
        private PythonSessionInfo? _configuredSession;
        private PythonVirtualEnvironment? _configuredEnvironment;
        private PyModule? _sessionScope;
        #endregion

        #region Constructor
        public RequirementsFileManager(
            IBeepService beepService,
            IPythonRunTimeManager pythonRuntime,
            PythonPackageManager packageManager,
            IProgress<PassedArgs> progress = null)
        {
            _beepService = beepService ?? throw new ArgumentNullException(nameof(beepService));
            _pythonRuntime = pythonRuntime ?? throw new ArgumentNullException(nameof(pythonRuntime));
            _packageManager = packageManager ?? throw new ArgumentNullException(nameof(packageManager));
            _progress = progress;
        }
        #endregion

        #region Session and Environment Configuration
        /// <summary>
        /// Configure the requirements manager to use a specific session and environment
        /// </summary>
        /// <param name="session">Python session to use for operations</param>
        /// <param name="environment">Virtual environment for requirements operations</param>
        /// <returns>True if configuration successful</returns>
        public bool ConfigureSession(PythonSessionInfo session, PythonVirtualEnvironment environment)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));
            
            if (environment == null)
                throw new ArgumentNullException(nameof(environment));

            _configuredSession = session;
            _configuredEnvironment = environment;
            
            // Configure the package operations manager with the same session
            if (_packageManager is PythonPackageManager packageOpManager)
            {
                packageOpManager.ConfigureSession(session, environment);
            }

            // Get or create the session scope
            if (_pythonRuntime.HasScope(session))
            {
                _sessionScope = _pythonRuntime.GetScope(session);
            }
            else
            {
                if (_pythonRuntime.CreateScope(session, environment))
                {
                    _sessionScope = _pythonRuntime.GetScope(session);
                }
                else
                {
                    throw new InvalidOperationException("Failed to create Python scope for session");
                }
            }

            return true;
        }

        /// <summary>
        /// Check if session is properly configured
        /// </summary>
        /// <returns>True if session and environment are configured</returns>
        public bool IsSessionConfigured()
        {
            return _configuredSession != null && _configuredEnvironment != null && _sessionScope != null;
        }

        private bool ValidateSessionAndEnvironment()
        {
            if (!IsSessionConfigured())
            {
                ReportError("Session and environment must be configured before performing requirements operations.");
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
                throw new ObjectDisposedException(nameof(RequirementsFileManager));

            if (!IsSessionConfigured())
                throw new InvalidOperationException("Session must be configured before executing requirements operations");

            lock (_operationLock)
            {
                try
                {
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
        /// Executes code safely within the session context and returns a result
        /// </summary>
        /// <typeparam name="T">Type of result to return</typeparam>
        /// <param name="func">Function to execute in session</param>
        /// <returns>Result of the function</returns>
        private T ExecuteInSession<T>(Func<T> func)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(RequirementsFileManager));

            if (!IsSessionConfigured())
                throw new InvalidOperationException("Session must be configured before executing requirements operations");

            lock (_operationLock)
            {
                try
                {
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

        #region Requirements File Operations
        /// <summary>
        /// Installs packages from a requirements file with enhanced session support
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
                ReportProgress($"Reading requirements file: {filePath}");
                
                // Read and parse the requirements file
                var requirements = await ReadRequirementsFileAsync(filePath);
                if (requirements.Count == 0)
                {
                    ReportProgress("No packages found in requirements file.");
                    return false;
                }

                // Temporarily disable auto-updates during batch operation
                bool originalAutoUpdate = environment.AutoUpdateRequirements;
                environment.AutoUpdateRequirements = false;

                // Install each package with session support
                bool success = true;
                int totalPackages = requirements.Count;
                int current = 0;

                ReportProgress($"Installing {totalPackages} packages from requirements file...");

                foreach (var package in requirements)
                {
                    current++;
                    string packageSpec = package.Key + (string.IsNullOrEmpty(package.Value) ? "" : package.Value);

                    ReportProgress($"Installing {packageSpec} ({current}/{totalPackages})");
                    bool installResult = await _packageManager.InstallPackageAsync(packageSpec, environment);

                    if (!installResult)
                    {
                        ReportError($"Failed to install {packageSpec}");
                        success = false;
                        // Continue with other packages instead of stopping
                    }
                    else
                    {
                        ReportProgress($"Successfully installed {packageSpec}");
                    }

                    // Small delay to allow UI updates
                    await Task.Delay(10);
                }

                // Restore original auto-update setting
                environment.AutoUpdateRequirements = originalAutoUpdate;

                // Set the requirements file path in the environment
                if (success && string.IsNullOrEmpty(environment.RequirementsFile))
                {
                    environment.RequirementsFile = filePath;
                    environment.RequirementsLastUpdated = DateTime.Now;
                }

                string resultMessage = success 
                    ? "All packages installed successfully." 
                    : "Some packages failed to install.";
                
                ReportProgress($"Completed installing packages from requirements file. {resultMessage}");
                return success;
            }
            catch (Exception ex)
            {
                ReportError($"Error installing packages from requirements file: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Installs packages from a requirements file asynchronously with session support
        /// </summary>
        /// <param name="filePath">Path to the requirements file</param>
        /// <param name="environment">Target environment</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successful</returns>
        public async Task<bool> InstallFromRequirementsFileWithSessionAsync(
            string filePath, 
            PythonVirtualEnvironment environment, 
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath) || environment == null)
            {
                ReportError($"Invalid requirements file path: {filePath}");
                return false;
            }

            try
            {
                // Ensure session is configured for this operation
                if (!IsSessionConfigured())
                {
                    // If we have a configured environment but no session, we need one
                    if (_configuredEnvironment != null)
                    {
                        environment = _configuredEnvironment;
                    }
                    
                    ReportError("Session must be configured before performing requirements operations with session support");
                    return false;
                }

                return await InstallFromRequirementsFileAsync(filePath, environment);
            }
            catch (OperationCanceledException)
            {
                ReportProgress("Requirements installation was cancelled.");
                return false;
            }
            catch (Exception ex)
            {
                ReportError($"Error installing packages from requirements file with session: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Generates a requirements file from an environment's installed packages with enhanced session support
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
                ReportProgress($"Generating requirements file from environment: {environment.Name}");
                
                // Ensure we have the latest package data
                var packages = await _packageManager.GetAllPackagesAsync(environment);
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

                // Build the requirements file content with enhanced metadata
                StringBuilder content = new StringBuilder();
                content.AppendLine($"# Requirements for {environment.Name}");
                content.AppendLine($"# Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                content.AppendLine($"# Python version: {environment.PythonVersion}");
                content.AppendLine($"# Environment path: {environment.Path}");
                content.AppendLine($"# Package count: {packages.Count}");
                content.AppendLine();

                // Group packages by category if available
                var categorizedPackages = packages
                    .Where(p => !string.IsNullOrEmpty(p.PackageName))
                    .GroupBy(p => p.Category)
                    .OrderBy(g => g.Key.ToString());

                foreach (var categoryGroup in categorizedPackages)
                {
                    if (categoryGroup.Key != PackageCategory.Uncategorized)
                    {
                        content.AppendLine($"# {categoryGroup.Key} packages");
                    }

                    foreach (var package in categoryGroup.OrderBy(p => p.PackageName))
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

                    if (categoryGroup.Key != PackageCategory.Uncategorized)
                    {
                        content.AppendLine();
                    }
                }

                // Write the file atomically
                string tempFile = filePath + ".tmp";
                await File.WriteAllTextAsync(tempFile, content.ToString());
                File.Move(tempFile, filePath, true);

                // Update the environment's requirements file information
                environment.RequirementsFile = filePath;
                environment.RequirementsLastUpdated = DateTime.Now;

                ReportProgress($"Generated requirements file at {filePath} with {packages.Count} packages");
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
                ReportProgress($"Updating environment {environment.Name} from requirements file");
                
                // Install packages from the requirements file
                bool result = await InstallFromRequirementsFileAsync(environment.RequirementsFile, environment);
                
                if (result)
                {
                    environment.RequirementsLastUpdated = DateTime.Now;
                    ReportProgress($"Successfully updated environment {environment.Name}");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                ReportError($"Error updating environment from requirements file: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Validates a requirements file for syntax and package availability
        /// </summary>
        /// <param name="filePath">Path to the requirements file</param>
        /// <returns>Validation result with any errors found</returns>
        public async Task<RequirementsValidationResult> ValidateRequirementsFileAsync(string filePath)
        {
            var result = new RequirementsValidationResult { IsValid = true, Errors = new List<string>() };

            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                result.IsValid = false;
                result.Errors.Add("Requirements file does not exist");
                return result;
            }

            try
            {
                var requirements = await ReadRequirementsFileAsync(filePath);
                
                if (requirements.Count == 0)
                {
                    result.Errors.Add("No valid package requirements found in file");
                }

                // Validate each requirement
                foreach (var requirement in requirements)
                {
                    if (string.IsNullOrWhiteSpace(requirement.Key))
                    {
                        result.Errors.Add("Found empty package name");
                        continue;
                    }

                    // Basic package name validation
                    if (!IsValidPackageName(requirement.Key))
                    {
                        result.Errors.Add($"Invalid package name: {requirement.Key}");
                    }

                    // Validate version specifier if present
                    if (!string.IsNullOrEmpty(requirement.Value) && !IsValidVersionSpecifier(requirement.Value))
                    {
                        result.Errors.Add($"Invalid version specifier for {requirement.Key}: {requirement.Value}");
                    }
                }

                result.IsValid = result.Errors.Count == 0;
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Errors.Add($"Error validating requirements file: {ex.Message}");
            }

            return result;
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
                    System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData),
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
        #endregion

        #region Helper Methods
        /// <summary>
        /// Reads package requirements from a requirements.txt file asynchronously
        /// </summary>
        /// <param name="filePath">Path to the requirements file</param>
        /// <returns>Dictionary of package name to version constraint</returns>
        public async Task<Dictionary<string, string>> ReadRequirementsFileAsync(string filePath)
        {
            var requirements = new Dictionary<string, string>();

            try
            {
                var lines = await File.ReadAllLinesAsync(filePath);
                
                foreach (var line in lines)
                {
                    string trimmedLine = line.Trim();

                    // Skip comments and empty lines
                    if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith("#"))
                        continue;

                    // Skip options like -r, -e, etc.
                    if (trimmedLine.StartsWith("-"))
                        continue;

                    // Parse package specs (supports format like: package==1.0.0)
                    string packageName;
                    string version = string.Empty;

                    // Common requirement formats: package==1.0.0, package>=1.0.0, etc.
                    int specifierIndex = trimmedLine.IndexOfAny(new[] { '=', '>', '<', '~', '!' });
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

        /// <summary>
        /// Reads package requirements from a requirements.txt file synchronously (legacy compatibility)
        /// </summary>
        /// <param name="filePath">Path to the requirements file</param>
        /// <returns>Dictionary of package name to version constraint</returns>
        public Dictionary<string, string> ReadRequirementsFile(string filePath)
        {
            return ReadRequirementsFileAsync(filePath).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Validates if a string is a valid Python package name
        /// </summary>
        private bool IsValidPackageName(string packageName)
        {
            if (string.IsNullOrWhiteSpace(packageName))
                return false;

            // Basic validation: can contain letters, numbers, hyphens, underscores, dots
            return System.Text.RegularExpressions.Regex.IsMatch(packageName, @"^[a-zA-Z0-9._-]+$");
        }

        /// <summary>
        /// Validates if a string is a valid version specifier
        /// </summary>
        private bool IsValidVersionSpecifier(string versionSpec)
        {
            if (string.IsNullOrWhiteSpace(versionSpec))
                return false;

            // Basic validation for common version specifiers
            return System.Text.RegularExpressions.Regex.IsMatch(versionSpec, @"^[=><~!]+[\d\w\.\-+]+.*$");
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

        #region IDisposable Implementation
        /// <summary>
        /// Disposes resources used by the requirements file manager
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
                    // Clean up any resources
                }
                catch (Exception ex)
                {
                    // Log disposal errors but don't throw
                    Console.WriteLine($"Warning during RequirementsFileManager disposal: {ex.Message}");
                }
                finally
                {
                    _isDisposed = true;
                }
            }
        }
        #endregion
    }

    /// <summary>
    /// Represents the result of validating a requirements file
    /// </summary>
    public class RequirementsValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }
}