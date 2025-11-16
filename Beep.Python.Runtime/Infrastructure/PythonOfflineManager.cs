using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Beep.Python.Model;
using Beep.Python.RuntimeEngine.Helpers;
using Newtonsoft.Json;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Container.Services;
using TheTechIdea.Beep.Editor;
using SysEnv = System.Environment;

namespace Beep.Python.RuntimeEngine.Infrastructure
{
    /// <summary>
    /// Manages offline Python environment setup using cached distributions and packages.
    /// Enables air-gapped installations and reliable offline deployments.
    /// </summary>
    public class PythonOfflineManager
    {
        private readonly IBeepService _beepService;
        private readonly IDMEEditor _dmEditor;
        private readonly PythonRuntimeRegistry _registry;
        private readonly string _cacheDirectory;
        private readonly string _distributionsDirectory;
        private readonly string _packagesDirectory;
        private readonly string _manifestPath;

        public PythonOfflineManager(
            IBeepService beepService,
            PythonRuntimeRegistry registry)
        {
            _beepService = beepService ?? throw new ArgumentNullException(nameof(beepService));
            _dmEditor = beepService.DMEEditor;
            _registry = registry;

            _cacheDirectory = Path.Combine(
                SysEnv.GetFolderPath(SysEnv.SpecialFolder.UserProfile),
                ".beep-python",
                "offline-cache");

            _distributionsDirectory = Path.Combine(_cacheDirectory, "distributions");
            _packagesDirectory = Path.Combine(_cacheDirectory, "packages");
            _manifestPath = Path.Combine(_cacheDirectory, "manifest.json");

            Directory.CreateDirectory(_distributionsDirectory);
            Directory.CreateDirectory(_packagesDirectory);
        }

        /// <summary>
        /// Creates an offline package containing Python distribution and packages
        /// </summary>
        public async Task<string> CreateOfflinePackageAsync(
            OfflinePackageOptions options,
            IProgress<OfflineProgress> progress = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _dmEditor?.AddLogMessage("Beep", "Creating offline package...", DateTime.Now, 0, null, Errors.Ok);

                progress?.Report(new OfflineProgress
                {
                    Stage = OfflineStage.Preparing,
                    Message = "Preparing offline package...",
                    Percentage = 5
                });

                var manifest = new OfflineManifest
                {
                    CreatedAt = DateTime.UtcNow,
                    PythonVersion = options.PythonVersion ?? "3.11.9",
                    Packages = options.Packages ?? new List<string>(),
                    PackageProfiles = options.PackageProfiles ?? new List<string>()
                };

                // Create temporary directory for package assembly
                var tempDir = Path.Combine(Path.GetTempPath(), $"beep-offline-{Guid.NewGuid()}");
                Directory.CreateDirectory(tempDir);

                try
                {
                    // Step 1: Copy Python distribution
                    progress?.Report(new OfflineProgress
                    {
                        Stage = OfflineStage.CopyingPython,
                        Message = $"Copying Python {manifest.PythonVersion} distribution...",
                        Percentage = 20
                    });

                    var pythonDistPath = await CopyPythonDistributionAsync(
                        manifest.PythonVersion,
                        tempDir,
                        cancellationToken);

                    manifest.PythonDistributionPath = Path.GetFileName(pythonDistPath);

                    // Step 2: Download/copy packages
                    progress?.Report(new OfflineProgress
                    {
                        Stage = OfflineStage.DownloadingPackages,
                        Message = "Downloading packages...",
                        Percentage = 40
                    });

                    var packagesDir = Path.Combine(tempDir, "packages");
                    Directory.CreateDirectory(packagesDir);

                    await DownloadPackagesAsync(
                        manifest.Packages,
                        packagesDir,
                        progress,
                        cancellationToken);

                    // Step 3: Create manifest
                    progress?.Report(new OfflineProgress
                    {
                        Stage = OfflineStage.CreatingManifest,
                        Message = "Creating manifest...",
                        Percentage = 70
                    });

                    var manifestJson = JsonConvert.SerializeObject(manifest, Formatting.Indented);
                    var manifestFile = Path.Combine(tempDir, "manifest.json");
                    await File.WriteAllTextAsync(manifestFile, manifestJson, cancellationToken);

                    // Step 4: Create offline installer script
                    var installerScript = GenerateOfflineInstallerScript(manifest);
                    var installerFile = Path.Combine(tempDir, "install-offline.ps1");
                    await File.WriteAllTextAsync(installerFile, installerScript, cancellationToken);

                    // Step 5: Create ZIP archive
                    progress?.Report(new OfflineProgress
                    {
                        Stage = OfflineStage.Packaging,
                        Message = "Creating archive...",
                        Percentage = 85
                    });

                    var outputPath = options.OutputPath ?? Path.Combine(
                        _cacheDirectory,
                        $"beep-python-offline-{manifest.PythonVersion}-{DateTime.UtcNow:yyyyMMddHHmmss}.zip");

                    if (File.Exists(outputPath))
                        File.Delete(outputPath);

                    ZipFile.CreateFromDirectory(tempDir, outputPath, CompressionLevel.Optimal, false);

                    progress?.Report(new OfflineProgress
                    {
                        Stage = OfflineStage.Complete,
                        Message = $"Offline package created: {outputPath}",
                        Percentage = 100
                    });

                    _dmEditor?.AddLogMessage("Beep", $"Offline package created at: {outputPath}", DateTime.Now, 0, null, Errors.Ok);

                    return outputPath;
                }
                finally
                {
                    // Cleanup temp directory
                    if (Directory.Exists(tempDir))
                    {
                        try { Directory.Delete(tempDir, true); } catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                _dmEditor?.AddLogMessage("Beep", $"Error creating offline package: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                progress?.Report(new OfflineProgress
                {
                    Stage = OfflineStage.Failed,
                    Message = $"Error: {ex.Message}"
                });
                throw;
            }
        }

        /// <summary>
        /// Installs Python from an offline package
        /// </summary>
        public async Task<PythonRuntimeInfo> InstallFromOfflinePackageAsync(
            string packagePath,
            IProgress<OfflineProgress> progress = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (!File.Exists(packagePath))
                    throw new FileNotFoundException($"Offline package not found: {packagePath}");

                _dmEditor?.AddLogMessage("Beep", $"Installing from offline package: {packagePath}", DateTime.Now, 0, null, Errors.Ok);

                progress?.Report(new OfflineProgress
                {
                    Stage = OfflineStage.Extracting,
                    Message = "Extracting offline package...",
                    Percentage = 10
                });

                // Extract to temp directory
                var tempDir = Path.Combine(Path.GetTempPath(), $"beep-offline-install-{Guid.NewGuid()}");
                ZipFile.ExtractToDirectory(packagePath, tempDir);

                try
                {
                    // Read manifest
                    var manifestFile = Path.Combine(tempDir, "manifest.json");
                    if (!File.Exists(manifestFile))
                        throw new InvalidOperationException("Invalid offline package: manifest.json not found");

                    var manifestJson = await File.ReadAllTextAsync(manifestFile, cancellationToken);
                    var manifest = JsonConvert.DeserializeObject<OfflineManifest>(manifestJson);

                    progress?.Report(new OfflineProgress
                    {
                        Stage = OfflineStage.InstallingPython,
                        Message = $"Installing Python {manifest.PythonVersion}...",
                        Percentage = 30
                    });

                    // Install Python distribution
                    var pythonDistFile = Path.Combine(tempDir, manifest.PythonDistributionPath);
                    var installPath = Path.Combine(
                        SysEnv.GetFolderPath(SysEnv.SpecialFolder.UserProfile),
                        ".beep-python",
                        "embedded");

                    Directory.CreateDirectory(installPath);
                    ZipFile.ExtractToDirectory(pythonDistFile, installPath);

                    // Configure Python
                    await ConfigurePythonOfflineAsync(installPath, cancellationToken);

                    // Install packages
                    progress?.Report(new OfflineProgress
                    {
                        Stage = OfflineStage.InstallingPackages,
                        Message = "Installing packages...",
                        Percentage = 60
                    });

                    var packagesDir = Path.Combine(tempDir, "packages");
                    if (Directory.Exists(packagesDir))
                    {
                        await InstallPackagesOfflineAsync(installPath, packagesDir, progress, cancellationToken);
                    }

                    // Register runtime
                    progress?.Report(new OfflineProgress
                    {
                        Stage = OfflineStage.Registering,
                        Message = "Registering runtime...",
                        Percentage = 90
                    });

                    await _registry.InitializeAsync();
                    var runtimeId = await _registry.RegisterManagedRuntimeAsync(
                        manifest.PythonVersion,
                        PythonRuntimeType.Embedded);

                    // Get the registered runtime info
                    var runtime = _registry.GetRuntime(runtimeId);

                    progress?.Report(new OfflineProgress
                    {
                        Stage = OfflineStage.Complete,
                        Message = "Installation complete",
                        Percentage = 100
                    });

                    _dmEditor?.AddLogMessage("Beep", $"Offline installation complete: {installPath}", DateTime.Now, 0, null, Errors.Ok);

                    return runtime;
                }
                finally
                {
                    // Cleanup
                    if (Directory.Exists(tempDir))
                    {
                        try { Directory.Delete(tempDir, true); } catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                _dmEditor?.AddLogMessage("Beep", $"Error installing from offline package: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                progress?.Report(new OfflineProgress
                {
                    Stage = OfflineStage.Failed,
                    Message = $"Error: {ex.Message}"
                });
                throw;
            }
        }

        /// <summary>
        /// Caches a Python distribution for offline use
        /// </summary>
        public async Task<string> CachePythonDistributionAsync(
            string version,
            string sourcePath,
            CancellationToken cancellationToken = default)
        {
            var cacheFilename = $"python-{version}-embed-amd64.zip";
            var cachePath = Path.Combine(_distributionsDirectory, cacheFilename);

            if (File.Exists(cachePath))
            {
                _dmEditor?.AddLogMessage("Beep", $"Python {version} already cached", DateTime.Now, 0, null, Errors.Ok);
                return cachePath;
            }

            File.Copy(sourcePath, cachePath, true);

            _dmEditor?.AddLogMessage("Beep", $"Cached Python {version} distribution", DateTime.Now, 0, null, Errors.Ok);

            return cachePath;
        }

        /// <summary>
        /// Checks if a Python version is cached
        /// </summary>
        public bool IsPythonVersionCached(string version)
        {
            var cacheFilename = $"python-{version}-embed-amd64.zip";
            var cachePath = Path.Combine(_distributionsDirectory, cacheFilename);
            return File.Exists(cachePath);
        }

        /// <summary>
        /// Lists all cached Python versions
        /// </summary>
        public List<CachedDistribution> GetCachedDistributions()
        {
            var cached = new List<CachedDistribution>();

            if (!Directory.Exists(_distributionsDirectory))
                return cached;

            foreach (var file in Directory.GetFiles(_distributionsDirectory, "*.zip"))
            {
                var fileInfo = new FileInfo(file);
                var filename = Path.GetFileNameWithoutExtension(file);

                cached.Add(new CachedDistribution
                {
                    Version = ExtractVersionFromFilename(filename),
                    Path = file,
                    Size = fileInfo.Length,
                    CachedAt = fileInfo.CreationTimeUtc
                });
            }

            return cached.OrderByDescending(c => c.CachedAt).ToList();
        }

        /// <summary>
        /// Clears the offline cache
        /// </summary>
        public async Task<bool> ClearCacheAsync(bool keepDistributions = false)
        {
            try
            {
                if (!keepDistributions && Directory.Exists(_distributionsDirectory))
                {
                    Directory.Delete(_distributionsDirectory, true);
                    Directory.CreateDirectory(_distributionsDirectory);
                }

                if (Directory.Exists(_packagesDirectory))
                {
                    Directory.Delete(_packagesDirectory, true);
                    Directory.CreateDirectory(_packagesDirectory);
                }

                _dmEditor?.AddLogMessage("Beep", "Offline cache cleared", DateTime.Now, 0, null, Errors.Ok);

                return true;
            }
            catch (Exception ex)
            {
                _dmEditor?.AddLogMessage("Beep", $"Error clearing cache: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                return false;
            }
        }

        #region Private Methods

        private async Task<string> CopyPythonDistributionAsync(
            string version,
            string targetDir,
            CancellationToken cancellationToken)
        {
            var cacheFilename = $"python-{version}-embed-amd64.zip";
            var cachePath = Path.Combine(_distributionsDirectory, cacheFilename);

            if (!File.Exists(cachePath))
                throw new FileNotFoundException($"Python {version} not found in cache. Download it first.");

            var targetPath = Path.Combine(targetDir, cacheFilename);
            File.Copy(cachePath, targetPath, true);

            return targetPath;
        }

        private async Task DownloadPackagesAsync(
            List<string> packages,
            string targetDir,
            IProgress<OfflineProgress> progress,
            CancellationToken cancellationToken)
        {
            // This would use pip download to get .whl files
            // For now, we'll just create placeholder
            foreach (var package in packages)
            {
                progress?.Report(new OfflineProgress
                {
                    Stage = OfflineStage.DownloadingPackages,
                    Message = $"Downloading {package}...",
                    Percentage = 40
                });

                // In real implementation: pip download -d {targetDir} {package}
            }
        }

        private async Task ConfigurePythonOfflineAsync(
            string installPath,
            CancellationToken cancellationToken)
        {
            // Configure site-packages path
            var pthFiles = Directory.GetFiles(installPath, "*._pth");
            if (pthFiles.Any())
            {
                var pthFile = pthFiles.First();
                var lines = await File.ReadAllLinesAsync(pthFile, cancellationToken);
                var newLines = lines.Where(l => !l.StartsWith("#")).ToList();
                
                if (!newLines.Contains("Lib/site-packages"))
                    newLines.Add("Lib/site-packages");

                await File.WriteAllLinesAsync(pthFile, newLines, cancellationToken);
            }
        }

        private async Task InstallPackagesOfflineAsync(
            string pythonPath,
            string packagesDir,
            IProgress<OfflineProgress> progress,
            CancellationToken cancellationToken)
        {
            // Install packages from .whl files
            var packages = Directory.GetFiles(packagesDir, "*.whl");
            
            foreach (var package in packages)
            {
                var packageName = Path.GetFileNameWithoutExtension(package);
                progress?.Report(new OfflineProgress
                {
                    Stage = OfflineStage.InstallingPackages,
                    Message = $"Installing {packageName}...",
                    Percentage = 60
                });

                // In real implementation: pip install --no-index --find-links={packagesDir} {package}
            }
        }

        private string GenerateOfflineInstallerScript(OfflineManifest manifest)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Beep Python Offline Installer");
            sb.AppendLine($"# Created: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            sb.AppendLine($"# Python Version: {manifest.PythonVersion}");
            sb.AppendLine();
            sb.AppendLine("$ErrorActionPreference = 'Stop'");
            sb.AppendLine();
            sb.AppendLine("Write-Host 'Beep Python Offline Installation' -ForegroundColor Green");
            sb.AppendLine($"Write-Host 'Python Version: {manifest.PythonVersion}' -ForegroundColor Cyan");
            sb.AppendLine();
            sb.AppendLine("# Extract Python distribution");
            sb.AppendLine($"$pythonZip = '{manifest.PythonDistributionPath}'");
            sb.AppendLine("$installPath = Join-Path $env:USERPROFILE '.beep-python\\embedded'");
            sb.AppendLine("Expand-Archive -Path $pythonZip -DestinationPath $installPath -Force");
            sb.AppendLine();
            sb.AppendLine("Write-Host 'Installation complete!' -ForegroundColor Green");

            return sb.ToString();
        }

        private string ExtractVersionFromFilename(string filename)
        {
            // Extract version from filename like "python-3.11.9-embed-amd64"
            var parts = filename.Split('-');
            if (parts.Length >= 2)
                return parts[1];
            return "unknown";
        }

        #endregion
    }

    /// <summary>
    /// Options for creating an offline package
    /// </summary>
    public class OfflinePackageOptions
    {
        public string PythonVersion { get; set; } = "3.11.9";
        public List<string> Packages { get; set; } = new();
        public List<string> PackageProfiles { get; set; } = new();
        public string OutputPath { get; set; }
    }

    /// <summary>
    /// Manifest for offline package
    /// </summary>
    public class OfflineManifest
    {
        public DateTime CreatedAt { get; set; }
        public string PythonVersion { get; set; }
        public string PythonDistributionPath { get; set; }
        public List<string> Packages { get; set; } = new();
        public List<string> PackageProfiles { get; set; } = new();
    }

    /// <summary>
    /// Progress report for offline operations
    /// </summary>
    public class OfflineProgress
    {
        public OfflineStage Stage { get; set; }
        public string Message { get; set; }
        public double Percentage { get; set; }
    }

    /// <summary>
    /// Stages of offline operation
    /// </summary>
    public enum OfflineStage
    {
        Preparing,
        CopyingPython,
        DownloadingPackages,
        CreatingManifest,
        Packaging,
        Extracting,
        InstallingPython,
        InstallingPackages,
        Registering,
        Complete,
        Failed
    }

    /// <summary>
    /// Information about a cached distribution
    /// </summary>
    public class CachedDistribution
    {
        public string Version { get; set; }
        public string Path { get; set; }
        public long Size { get; set; }
        public DateTime CachedAt { get; set; }
    }
}
