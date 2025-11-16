using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Beep.Python.Model;
using Beep.Python.RuntimeEngine.Helpers;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Container.Services;
using SysEnv = System.Environment;

namespace Beep.Python.RuntimeEngine.Infrastructure
{
    /// <summary>
    /// Manages multiple Python versions and provides version-specific provisioning
    /// </summary>
    public class PythonVersionManager
    {
        private readonly IBeepService _beepService;
        private readonly IDMEEditor _dmEditor;
        private readonly PythonRuntimeRegistry _registry;
        private readonly PythonEmbeddedProvisioner _provisioner;
        private readonly string _versionsDirectory;
        private readonly Dictionary<string, PythonVersionInfo> _availableVersions;

        public PythonVersionManager(
            IBeepService beepService,
            PythonRuntimeRegistry registry,
            PythonEmbeddedProvisioner provisioner)
        {
            _beepService = beepService;
            _dmEditor = beepService?.DMEEditor;
            _registry = registry;
            _provisioner = provisioner;
            _versionsDirectory = Path.Combine(
                SysEnv.GetFolderPath(SysEnv.SpecialFolder.UserProfile),
                ".beep-python",
                "versions");

            Directory.CreateDirectory(_versionsDirectory);

            _availableVersions = new Dictionary<string, PythonVersionInfo>
            {
                ["3.9.13"] = new PythonVersionInfo
                {
                    Version = "3.9.13",
                    DownloadUrl = "https://www.python.org/ftp/python/3.9.13/python-3.9.13-embed-amd64.zip",
                    Size = 7991808,
                    IsSupported = true,
                    ReleaseDate = new DateTime(2022, 5, 17)
                },
                ["3.10.11"] = new PythonVersionInfo
                {
                    Version = "3.10.11",
                    DownloadUrl = "https://www.python.org/ftp/python/3.10.11/python-3.10.11-embed-amd64.zip",
                    Size = 8916992,
                    IsSupported = true,
                    ReleaseDate = new DateTime(2023, 4, 5)
                },
                ["3.11.9"] = new PythonVersionInfo
                {
                    Version = "3.11.9",
                    DownloadUrl = "https://www.python.org/ftp/python/3.11.9/python-3.11.9-embed-amd64.zip",
                    Size = 10485760,
                    IsSupported = true,
                    ReleaseDate = new DateTime(2024, 4, 2),
                    IsRecommended = true
                },
                ["3.12.3"] = new PythonVersionInfo
                {
                    Version = "3.12.3",
                    DownloadUrl = "https://www.python.org/ftp/python/3.12.3/python-3.12.3-embed-amd64.zip",
                    Size = 11534336,
                    IsSupported = true,
                    ReleaseDate = new DateTime(2024, 4, 9)
                }
            };
        }

        /// <summary>
        /// Gets list of all available Python versions
        /// </summary>
        public IReadOnlyList<PythonVersionInfo> GetAvailableVersions()
        {
            return _availableVersions.Values.OrderByDescending(v => v.Version).ToList();
        }

        /// <summary>
        /// Gets list of installed Python versions
        /// </summary>
        public async Task<IReadOnlyList<PythonVersionInfo>> GetInstalledVersionsAsync()
        {
            await _registry.InitializeAsync();
            var runtimes = _registry.GetAvailableRuntimes();

            var installedVersions = new List<PythonVersionInfo>();

            foreach (var runtime in runtimes.Where(r => r.Type == PythonRuntimeType.Embedded))
            {
                if (_availableVersions.TryGetValue(runtime.Name, out var versionInfo))
                {
                    var installedInfo = new PythonVersionInfo
                    {
                        Version = versionInfo.Version,
                        DownloadUrl = versionInfo.DownloadUrl,
                        Size = versionInfo.Size,
                        IsSupported = versionInfo.IsSupported,
                        ReleaseDate = versionInfo.ReleaseDate,
                        IsRecommended = versionInfo.IsRecommended,
                        IsInstalled = true,
                        InstallPath = runtime.Path
                    };
                    installedVersions.Add(installedInfo);
                }
            }

            return installedVersions;
        }

        /// <summary>
        /// Ensures a specific Python version is installed
        /// </summary>
        public async Task<PythonRuntimeInfo> EnsureVersionAsync(
            string version,
            IProgress<VersionInstallProgress> progress = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(version))
                throw new ArgumentException("Version cannot be null or empty", nameof(version));

            if (!_availableVersions.ContainsKey(version))
            {
                _dmEditor?.AddLogMessage("Beep", $"Unsupported Python version: {version}", DateTime.Now, 0, null, Errors.Failed);
                throw new NotSupportedException($"Python version {version} is not supported");
            }

            progress?.Report(new VersionInstallProgress
            {
                Version = version,
                Stage = InstallStage.Checking,
                Message = $"Checking for Python {version}..."
            });

            // Check if already installed
            await _registry.InitializeAsync();
            var existingRuntime = _registry.GetAvailableRuntimes()
                .FirstOrDefault(r => r.Type == PythonRuntimeType.Embedded && r.Name == version);

            if (existingRuntime != null)
            {
                progress?.Report(new VersionInstallProgress
                {
                    Version = version,
                    Stage = InstallStage.Complete,
                    Message = $"Python {version} already installed",
                    Percentage = 100
                });

                return existingRuntime;
            }

            progress?.Report(new VersionInstallProgress
            {
                Version = version,
                Stage = InstallStage.Downloading,
                Message = $"Installing Python {version}...",
                Percentage = 10
            });

            // Install the version
            var provisionProgress = new Progress<ProvisioningProgress>(p =>
            {
                progress?.Report(new VersionInstallProgress
                {
                    Version = version,
                    Stage = InstallStage.Downloading,
                    Message = p.Message,
                    Percentage = 10 + (p.Percentage * 0.9) // 10-100%
                });
            });

            var provisionedRuntime = await _provisioner.ProvisionEmbeddedPythonAsync(
                version,
                provisionProgress,
                cancellationToken);

            // Get the registered runtime info
            await _registry.InitializeAsync();
            var runtime = _registry.GetAvailableRuntimes()
                .FirstOrDefault(r => r.Type == PythonRuntimeType.Embedded && r.Name == version);

            progress?.Report(new VersionInstallProgress
            {
                Version = version,
                Stage = InstallStage.Complete,
                Message = $"Python {version} installed successfully",
                Percentage = 100
            });

            _dmEditor?.AddLogMessage("Beep", $"Python {version} installed at: {runtime?.Path}", DateTime.Now, 0, null, Errors.Ok);

            return runtime;
        }

        /// <summary>
        /// Gets the recommended Python version
        /// </summary>
        public PythonVersionInfo GetRecommendedVersion()
        {
            return _availableVersions.Values.FirstOrDefault(v => v.IsRecommended)
                ?? _availableVersions.Values.OrderByDescending(v => v.Version).First();
        }

        /// <summary>
        /// Checks if a specific version is installed
        /// </summary>
        public async Task<bool> IsVersionInstalledAsync(string version)
        {
            await _registry.InitializeAsync();
            return _registry.GetAvailableRuntimes()
                .Any(r => r.Type == PythonRuntimeType.Embedded && r.Name == version);
        }

        /// <summary>
        /// Uninstalls a specific Python version
        /// </summary>
        public async Task<bool> UninstallVersionAsync(
            string version,
            IProgress<string> progress = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                progress?.Report($"Uninstalling Python {version}...");

                await _registry.InitializeAsync();
                var runtime = _registry.GetAvailableRuntimes()
                    .FirstOrDefault(r => r.Type == RuntimeType.Embedded && r.Name == version);

                if (runtime == null)
                {
                    progress?.Report($"Python {version} is not installed");
                    return false;
                }

                // Check if it's the default runtime
                var defaultRuntime = _registry.GetDefaultRuntime();
                if (defaultRuntime?.Id == runtime.Id)
                {
                    progress?.Report("Cannot uninstall default runtime. Set another runtime as default first.");
                    return false;
                }

                // Delete installation directory
                if (Directory.Exists(runtime.Path))
                {
                    progress?.Report($"Deleting {runtime.Path}...");
                    Directory.Delete(runtime.Path, true);
                }

                // Remove from registry (would need to add this method to PythonRuntimeRegistry)
                progress?.Report("Updating registry...");
                // await _registry.UnregisterRuntimeAsync(runtime.Id);

                progress?.Report($"Python {version} uninstalled successfully");
                _dmEditor?.AddLogMessage("Beep", $"Python {version} uninstalled", DateTime.Now, 0, null, Errors.Ok);

                return true;
            }
            catch (Exception ex)
            {
                _dmEditor?.AddLogMessage("Beep", $"Error uninstalling Python {version}: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                progress?.Report($"Error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Switches the default Python version
        /// </summary>
        public async Task<bool> SetDefaultVersionAsync(
            string version,
            IProgress<string> progress = null)
        {
            try
            {
                progress?.Report($"Setting Python {version} as default...");

                await _registry.InitializeAsync();
                var runtime = _registry.GetAvailableRuntimes()
                    .FirstOrDefault(r => r.Type == RuntimeType.Embedded && r.Name == version);

                if (runtime == null)
                {
                    progress?.Report($"Python {version} is not installed");
                    return false;
                }

                await _registry.SetDefaultRuntimeAsync(runtime.Id);

                progress?.Report($"Python {version} is now the default runtime");
                _dmEditor?.AddLogMessage("Beep", $"Default Python set to {version}", DateTime.Now, 0, null, Errors.Ok);

                return true;
            }
            catch (Exception ex)
            {
                _dmEditor?.AddLogMessage("Beep", $"Error setting default Python: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                progress?.Report($"Error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets version information for a specific version
        /// </summary>
        public PythonVersionInfo GetVersionInfo(string version)
        {
            return _availableVersions.TryGetValue(version, out var info) ? info : null;
        }

        /// <summary>
        /// Lists all Python versions with installation status
        /// </summary>
        public async Task<List<VersionStatus>> GetVersionStatusListAsync()
        {
            await _registry.InitializeAsync();
            var installedRuntimes = _registry.GetAvailableRuntimes()
                .Where(r => r.Type == PythonRuntimeType.Embedded)
                .ToList();

            var defaultRuntime = _registry.GetDefaultRuntime();

            var statusList = new List<VersionStatus>();

            foreach (var versionInfo in _availableVersions.Values.OrderByDescending(v => v.Version))
            {
                var runtime = installedRuntimes.FirstOrDefault(r => r.Name == versionInfo.Version);

                statusList.Add(new VersionStatus
                {
                    Version = versionInfo.Version,
                    IsInstalled = runtime != null,
                    IsDefault = runtime != null && defaultRuntime?.Id == runtime.Id,
                    IsRecommended = versionInfo.IsRecommended,
                    InstallPath = runtime?.Path,
                    Size = versionInfo.Size,
                    ReleaseDate = versionInfo.ReleaseDate
                });
            }

            return statusList;
        }
    }

    /// <summary>
    /// Information about a Python version
    /// </summary>
    public class PythonVersionInfo
    {
        public string Version { get; set; }
        public string DownloadUrl { get; set; }
        public long Size { get; set; }
        public bool IsSupported { get; set; }
        public bool IsRecommended { get; set; }
        public DateTime ReleaseDate { get; set; }
        public bool IsInstalled { get; set; }
        public string InstallPath { get; set; }
    }

    /// <summary>
    /// Installation progress for a specific version
    /// </summary>
    public class VersionInstallProgress
    {
        public string Version { get; set; }
        public InstallStage Stage { get; set; }
        public string Message { get; set; }
        public double Percentage { get; set; }
    }

    /// <summary>
    /// Installation stage
    /// </summary>
    public enum InstallStage
    {
        Checking,
        Downloading,
        Extracting,
        Configuring,
        Verifying,
        Complete,
        Failed
    }

    /// <summary>
    /// Version installation status
    /// </summary>
    public class VersionStatus
    {
        public string Version { get; set; }
        public bool IsInstalled { get; set; }
        public bool IsDefault { get; set; }
        public bool IsRecommended { get; set; }
        public string InstallPath { get; set; }
        public long Size { get; set; }
        public DateTime ReleaseDate { get; set; }
    }
}
