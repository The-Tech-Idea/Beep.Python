using Beep.Python.Model;
using Beep.Python.RuntimeEngine.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
//using TheTechIdea.Beep.Addin;
//using TheTechIdea.Beep.ConfigUtil;
 
//using TheTechIdea.Beep.Editor;
using SysEnv = System.Environment;

namespace Beep.Python.RuntimeEngine.Configuration
{
    /// <summary>
    /// Manages package requirement profiles for Python environments.
    /// Supports profile-based package installation and version constraints.
    /// </summary>
    public class PackageRequirementsManager : IPackageRequirementsManager
    {
       
        private readonly string _configPath;
        private PackageRequirementsConfig _config;

        public PackageRequirementsManager(string configPath = null)
        {
           

            // Allow caller (e.g., orchestrator/shell) to control where the
            // package requirements file lives by passing configPath. If not
            // provided, fall back to the current user-profile-based path.
            _configPath = configPath ?? Path.Combine(
                SysEnv.GetFolderPath(SysEnv.SpecialFolder.UserProfile),
                ".beep-python",
                "package-requirements.json");
        }

        /// <summary>
        /// Loads package profiles from configuration file.
        /// </summary>
        public async Task<bool> LoadProfilesAsync(string configPath = null)
        {
            try
            {
                var path = configPath ?? _configPath;

                if (!File.Exists(path))
                {
                   Messaging.AddLogMessage("Beep", "Package requirements config not found, creating default...", DateTime.Now, 0, null, Errors.Ok);
                    await CreateDefaultConfigAsync(path);
                }

                var json = await File.ReadAllTextAsync(path);
                _config = JsonConvert.DeserializeObject<PackageRequirementsConfig>(json);

                if (_config == null)
                {
                   Messaging.AddLogMessage("Beep", "Failed to load package requirements config", DateTime.Now, 0, null, Errors.Failed);
                    return false;
                }

               Messaging.AddLogMessage("Beep", $"Loaded {_config.Profiles.Count} package profile(s)", DateTime.Now, 0, null, Errors.Ok);
                return true;
            }
            catch (Exception ex)
            {
               Messaging.AddLogMessage("Beep", $"Error loading package requirements: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                return false;
            }
        }

        /// <summary>
        /// Gets a specific package profile by name.
        /// </summary>
        public PackageProfile GetProfile(string profileName)
        {
            if (_config == null || string.IsNullOrEmpty(profileName))
                return null;

            return _config.Profiles.ContainsKey(profileName) 
                ? _config.Profiles[profileName] 
                : null;
        }

        /// <summary>
        /// Gets all available profile names.
        /// </summary>
        public List<string> GetAvailableProfiles()
        {
            return _config?.Profiles.Keys.ToList() ?? new List<string>();
        }

        /// <summary>
        /// Installs all packages from a specific profile.
        /// </summary>
        public async Task<bool> InstallProfileAsync(
            string profileName,
            PythonRunTime runtime,
            IProgress<PackageInstallProgress> progress = null,
            CancellationToken cancellationToken = default)
        {
            if (_config == null)
            {
                await LoadProfilesAsync();
            }

            var profile = GetProfile(profileName);
            if (profile == null)
            {
               Messaging.AddLogMessage("Beep", $"Profile '{profileName}' not found", DateTime.Now, 0, null, Errors.Failed);
                return false;
            }

            if (runtime == null || string.IsNullOrEmpty(runtime.RuntimePath))
            {
               Messaging.AddLogMessage("Beep", "Invalid runtime provided", DateTime.Now, 0, null, Errors.Failed);
                return false;
            }

           Messaging.AddLogMessage("Beep", $"Installing profile '{profileName}' with {profile.Packages.Count} package(s)", DateTime.Now, 0, null, Errors.Ok);

            var pythonExe = Path.Combine(runtime.RuntimePath, "python.exe");
            if (!File.Exists(pythonExe))
            {
               Messaging.AddLogMessage("Beep", $"Python executable not found at {pythonExe}", DateTime.Now, 0, null, Errors.Failed);
                return false;
            }

            var packageList = profile.Packages.Select(kvp => 
                string.IsNullOrEmpty(kvp.Value) ? kvp.Key : $"{kvp.Key}{kvp.Value}").ToList();

            var totalPackages = packageList.Count;
            var currentPackage = 0;

            foreach (var package in packageList)
            {
                cancellationToken.ThrowIfCancellationRequested();

                currentPackage++;
                progress?.Report(new PackageInstallProgress
                {
                    PackageName = package,
                    Message = $"Installing {package}...",
                    Current = currentPackage,
                    Total = totalPackages
                });

                var success = await InstallPackageAsync(pythonExe, package, progress);

                if (!success)
                {
                   Messaging.AddLogMessage("Beep", $"Failed to install package: {package}", DateTime.Now, 0, null, Errors.Failed);
                    return false;
                }
            }

           Messaging.AddLogMessage("Beep", $"Profile '{profileName}' installed successfully", DateTime.Now, 0, null, Errors.Ok);
            return true;
        }

        /// <summary>
        /// Installs multiple profiles sequentially.
        /// </summary>
        public async Task<bool> InstallMultipleProfilesAsync(
            List<string> profileNames,
            PythonRunTime runtime,
            IProgress<PackageInstallProgress> progress = null,
            CancellationToken cancellationToken = default)
        {
            if (profileNames == null || !profileNames.Any())
                return true;

            foreach (var profileName in profileNames)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var success = await InstallProfileAsync(profileName, runtime, progress, cancellationToken);
                if (!success)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Installs a single package.
        /// </summary>
        private async Task<bool> InstallPackageAsync(
            string pythonExe,
            string packageSpec,
            IProgress<PackageInstallProgress> progress = null)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = pythonExe,
                        Arguments = $"-m pip install {packageSpec}",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        progress?.Report(new PackageInstallProgress
                        {
                            PackageName = packageSpec,
                            Message = e.Data,
                            Current = 0,
                            Total = 0
                        });
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                await process.WaitForExitAsync();

                return process.ExitCode == 0;
            }
            catch (Exception ex)
            {
               Messaging.AddLogMessage("Beep", $"Error installing package {packageSpec}: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                return false;
            }
        }

        /// <summary>
        /// Creates a default package requirements configuration.
        /// </summary>
        private async Task CreateDefaultConfigAsync(string path)
        {
            var defaultConfig = new PackageRequirementsConfig
            {
                Version = "1.0",
                Profiles = new Dictionary<string, PackageProfile>
                {
                    ["base"] = new PackageProfile
                    {
                        Description = "Essential packages for Python development",
                        Packages = new Dictionary<string, string>
                        {
                            ["pip"] = ">=23.0",
                            ["setuptools"] = ">=68.0",
                            ["wheel"] = ">=0.40"
                        }
                    },
                    ["data-science"] = new PackageProfile
                    {
                        Description = "Common data science packages",
                        Packages = new Dictionary<string, string>
                        {
                            ["numpy"] = ">=1.24.0",
                            ["pandas"] = ">=2.0.0",
                            ["matplotlib"] = ">=3.7.0",
                            ["scipy"] = ">=1.10.0",
                            ["scikit-learn"] = ">=1.3.0"
                        }
                    },
                    ["machine-learning"] = new PackageProfile
                    {
                        Description = "Machine learning frameworks",
                        Packages = new Dictionary<string, string>
                        {
                            ["torch"] = ">=2.0.0",
                            ["transformers"] = ">=4.35.0",
                            ["safetensors"] = ">=0.3.0",
                            ["accelerate"] = ">=0.20.0",
                            ["tokenizers"] = ">=0.13.0"
                        }
                    },
                    ["web"] = new PackageProfile
                    {
                        Description = "Web development packages",
                        Packages = new Dictionary<string, string>
                        {
                            ["flask"] = ">=3.0.0",
                            ["requests"] = ">=2.31.0",
                            ["beautifulsoup4"] = ">=4.12.0"
                        }
                    }
                }
            };

            var json = JsonConvert.SerializeObject(defaultConfig, Formatting.Indented);
            
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllTextAsync(path, json);
            _config = defaultConfig;
        }

        /// <summary>
        /// Adds a new profile or updates an existing one.
        /// </summary>
        public async Task<bool> AddOrUpdateProfileAsync(string profileName, PackageProfile profile)
        {
            if (_config == null)
            {
                await LoadProfilesAsync();
            }

            if (string.IsNullOrEmpty(profileName) || profile == null)
                return false;

            _config.Profiles[profileName] = profile;
            
            return await SaveConfigAsync();
        }

        /// <summary>
        /// Removes a profile.
        /// </summary>
        public async Task<bool> RemoveProfileAsync(string profileName)
        {
            if (_config == null || string.IsNullOrEmpty(profileName))
                return false;

            if (!_config.Profiles.ContainsKey(profileName))
                return false;

            _config.Profiles.Remove(profileName);
            
            return await SaveConfigAsync();
        }

        /// <summary>
        /// Saves the current configuration to disk.
        /// </summary>
        private async Task<bool> SaveConfigAsync()
        {
            try
            {
                var json = JsonConvert.SerializeObject(_config, Formatting.Indented);
                await File.WriteAllTextAsync(_configPath, json);
                return true;
            }
            catch (Exception ex)
            {
               Messaging.AddLogMessage("Beep", $"Failed to save package requirements config: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                return false;
            }
        }
    }

    /// <summary>
    /// Package requirements configuration structure.
    /// </summary>
    public class PackageRequirementsConfig
    {
        public string Version { get; set; }
        public Dictionary<string, PackageProfile> Profiles { get; set; } = new();
    }

    /// <summary>
    /// A package profile containing a set of packages to install.
    /// </summary>
    public class PackageProfile
    {
        public string Description { get; set; }
        public Dictionary<string, string> Packages { get; set; } = new();
    }

    /// <summary>
    /// Progress information for package installation.
    /// </summary>
    public class PackageInstallProgress
    {
        public string PackageName { get; set; }
        public string Message { get; set; }
        public int Current { get; set; }
        public int Total { get; set; }
        public double Percentage => Total > 0 ? (Current * 100.0 / Total) : 0;
    }

    /// <summary>
    /// Interface for package requirements management.
    /// </summary>
    public interface IPackageRequirementsManager
    {
        Task<bool> LoadProfilesAsync(string configPath = null);
        PackageProfile GetProfile(string profileName);
        List<string> GetAvailableProfiles();
        Task<bool> InstallProfileAsync(
            string profileName,
            PythonRunTime runtime,
            IProgress<PackageInstallProgress> progress = null,
            CancellationToken cancellationToken = default);
        Task<bool> InstallMultipleProfilesAsync(
            List<string> profileNames,
            PythonRunTime runtime,
            IProgress<PackageInstallProgress> progress = null,
            CancellationToken cancellationToken = default);
        Task<bool> AddOrUpdateProfileAsync(string profileName, PackageProfile profile);
        Task<bool> RemoveProfileAsync(string profileName);
    }
}
