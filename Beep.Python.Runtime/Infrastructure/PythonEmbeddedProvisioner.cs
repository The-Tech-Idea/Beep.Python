using Beep.Python.Model;
using Beep.Python.RuntimeEngine.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
//using TheTechIdea.Beep.ConfigUtil;
 
//using TheTechIdea.Beep.Editor;
using Environment = System.Environment;

namespace Beep.Python.RuntimeEngine.Infrastructure
{
    /// <summary>
    /// Manages downloading, installing, and configuring embedded Python distributions.
    /// Provides zero-configuration Python setup for applications.
    /// </summary>
    public class PythonEmbeddedProvisioner : IPythonEmbeddedProvisioner
    {
       
       
        private readonly EmbeddedPythonConfig _config;

        public PythonEmbeddedProvisioner(  EmbeddedPythonConfig config = null)
        {
             
         
            _config = config ?? new EmbeddedPythonConfig();
        }

        /// <summary>
        /// Convenience wrapper for orchestrator: provision embedded Python (or reuse
        /// existing) and return the installation root path. This mirrors the
        /// "GetOrDownloadEmbeddedPythonAsync" usage expected by the orchestrator.
        /// </summary>
        /// <param name="progress">Optional textual progress reporter.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Root path of the embedded Python installation.</returns>
        public async Task<string> GetOrDownloadEmbeddedPythonAsync(
            IProgress<string> progress = null,
            CancellationToken cancellationToken = default)
        {
            var provisioningProgress = progress != null
                ? new Progress<ProvisioningProgress>(p =>
                    progress.Report($"{p.Phase}: {p.Message}"))
                : null;

            var runtime = await ProvisionEmbeddedPythonAsync(
                null,
                provisioningProgress,
                cancellationToken);

            // Fallback to configured install path if runtime does not expose a
            // dedicated root/path property.
            return runtime?.RuntimePath ?? _config.InstallPath;
        }

        /// <summary>
        /// Provisions an embedded Python runtime with full setup including pip.
        /// </summary>
        public async Task<PythonRunTime> ProvisionEmbeddedPythonAsync(
            string version = null,
            IProgress<ProvisioningProgress> progress = null,
            CancellationToken cancellationToken = default)
        {
            version = version ?? _config.Version;
            var installPath = _config.InstallPath;

            try
            {
               Messaging.AddLogMessage("Beep", $"Starting embedded Python {version} provisioning", DateTime.Now, 0, null, Errors.Ok);

                // Step 1: Check if already installed
                if (await VerifyEmbeddedInstallationAsync(installPath))
                {
                    progress?.Report(new ProvisioningProgress
                    {
                        Phase = "Verification",
                        Message = "Embedded Python already installed and verified",
                        Percentage = 100
                    });

                    return PythonEnvironmentDiagnostics.GetPythonRunTime(installPath);
                }

                // Step 2: Download Python embedded distribution
                progress?.Report(new ProvisioningProgress
                {
                    Phase = "Download",
                    Message = $"Downloading Python {version} embedded distribution...",
                    Percentage = 0
                });

                Console.WriteLine();
                Console.WriteLine($"📥 Step 1/5: Downloading Python {version} embedded distribution...");

                var zipPath = await DownloadEmbeddedPythonAsync(version, installPath, progress, cancellationToken);

                var downloadSize = new FileInfo(zipPath).Length / (1024.0 * 1024.0);
                Console.WriteLine($"✅ Python downloaded successfully ({downloadSize:F2} MB)");

                cancellationToken.ThrowIfCancellationRequested();

                // Step 3: Extract distribution
                progress?.Report(new ProvisioningProgress
                {
                    Phase = "Extraction",
                    Message = "Extracting Python files...",
                    Percentage = 40
                });

                Console.WriteLine("📦 Step 2/5: Extracting Python files...");

                await ExtractEmbeddedPythonAsync(zipPath, installPath, cancellationToken);

                Console.WriteLine("✅ Extraction complete");

                cancellationToken.ThrowIfCancellationRequested();

                // Step 4: Configure site-packages support
                progress?.Report(new ProvisioningProgress
                {
                    Phase = "Configuration",
                    Message = "Configuring site-packages support...",
                    Percentage = 60
                });

                Console.WriteLine("🔧 Step 3/5: Configuring site-packages...");

                ConfigureSitePackages(installPath);

                Console.WriteLine("✅ Configuration complete");

                cancellationToken.ThrowIfCancellationRequested();

                // Step 5: Install pip
                progress?.Report(new ProvisioningProgress
                {
                    Phase = "Pip Installation",
                    Message = "Installing pip...",
                    Percentage = 75
                });

                Console.WriteLine("📥 Step 4/5: Installing pip...");

                await SetupPipAsync(installPath, new Progress<string>(msg => 
                    progress?.Report(new ProvisioningProgress
                    {
                        Phase = "Pip Installation",
                        Message = msg,
                        Percentage = 80
                    })));

                Console.WriteLine("✅ pip installed and upgraded");

                cancellationToken.ThrowIfCancellationRequested();

                // Step 6: Verify installation
                progress?.Report(new ProvisioningProgress
                {
                    Phase = "Verification",
                    Message = "Verifying installation...",
                    Percentage = 95
                });

                Console.WriteLine("✅ Step 5/5: Verifying installation...");

                var isValid = await VerifyEmbeddedInstallationAsync(installPath);

                if (!isValid)
                {
                    throw new InvalidOperationException("Embedded Python installation verification failed");
                }

                Console.WriteLine("✅ Python environment ready!");

                progress?.Report(new ProvisioningProgress
                {
                    Phase = "Complete",
                    Message = "Embedded Python provisioned successfully",
                    Percentage = 100
                });

               Messaging.AddLogMessage("Beep", $"Embedded Python {version} provisioned successfully at {installPath}", DateTime.Now, 0, null, Errors.Ok);

                return PythonEnvironmentDiagnostics.GetPythonRunTime(installPath);
            }
            catch (OperationCanceledException)
            {
               Messaging.AddLogMessage("Beep", "Embedded Python provisioning cancelled", DateTime.Now, 0, null, Errors.Failed);
                
                // Cleanup partial installation
                if (Directory.Exists(installPath))
                {
                    try { Directory.Delete(installPath, true); } catch { }
                }

                throw;
            }
            catch (Exception ex)
            {
               Messaging.AddLogMessage("Beep", $"Failed to provision embedded Python: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                
                // Cleanup partial installation
                if (Directory.Exists(installPath))
                {
                    try { Directory.Delete(installPath, true); } catch { }
                }

                throw;
            }
        }

        /// <summary>
        /// Downloads the embedded Python distribution from python.org.
        /// </summary>
        private async Task<string> DownloadEmbeddedPythonAsync(
            string version,
            string installPath,
            IProgress<ProvisioningProgress> progress,
            CancellationToken cancellationToken)
        {
            Directory.CreateDirectory(installPath);

            var downloadUrl = string.Format(_config.DownloadUrlTemplate ?? 
                "https://www.python.org/ftp/python/{0}/python-{0}-embed-amd64.zip", version);
            
            var zipPath = Path.Combine(installPath, "python.zip");

            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromMinutes(10);

            using var response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? 0;
            var bytesDownloaded = 0L;

            using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var fileStream = File.Create(zipPath);

            var buffer = new byte[8192];
            int bytesRead;

            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                bytesDownloaded += bytesRead;

                if (totalBytes > 0)
                {
                    var percentage = (bytesDownloaded * 100.0) / totalBytes;
                    progress?.Report(new ProvisioningProgress
                    {
                        Phase = "Download",
                        Message = $"Downloading... {percentage:F1}%",
                        Percentage = percentage * 0.4, // Download is 40% of total process
                        BytesDownloaded = bytesDownloaded,
                        TotalBytes = totalBytes
                    });
                }
            }

            return zipPath;
        }

        /// <summary>
        /// Extracts the embedded Python distribution.
        /// </summary>
        private async Task ExtractEmbeddedPythonAsync(string zipPath, string installPath, CancellationToken cancellationToken)
        {
            await Task.Run(() =>
            {
                ZipFile.ExtractToDirectory(zipPath, installPath);
                File.Delete(zipPath);
            }, cancellationToken);
        }

        /// <summary>
        /// Configures the Python installation to support site-packages.
        /// </summary>
        private void ConfigureSitePackages(string installPath)
        {
            // Find the ._pth file (e.g., python311._pth)
            var pthFiles = Directory.GetFiles(installPath, "python*._pth");

            if (pthFiles.Length == 0)
            {
                throw new InvalidOperationException("Could not find Python ._pth configuration file");
            }

            var pthFile = pthFiles[0];
            var lines = new List<string>(File.ReadAllLines(pthFile));

            // Add site-packages support if not already present
            if (!lines.Any(l => l.Trim().Equals("Lib", StringComparison.OrdinalIgnoreCase)))
            {
                lines.Add("Lib");
            }

            if (!lines.Any(l => l.Trim().Equals("Lib\\site-packages", StringComparison.OrdinalIgnoreCase)))
            {
                lines.Add("Lib\\site-packages");
            }

            // Uncomment or add "import site"
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].TrimStart().StartsWith("#import site"))
                {
                    lines[i] = "import site";
                }
            }

            if (!lines.Any(l => l.Trim().Equals("import site", StringComparison.OrdinalIgnoreCase)))
            {
                lines.Add("import site");
            }

            File.WriteAllLines(pthFile, lines);

            // Create Lib and site-packages directories
            var libDir = Path.Combine(installPath, "Lib");
            var sitePackagesDir = Path.Combine(libDir, "site-packages");

            Directory.CreateDirectory(libDir);
            Directory.CreateDirectory(sitePackagesDir);
        }

        /// <summary>
        /// Sets up pip in the embedded Python installation.
        /// </summary>
        public async Task<bool> SetupPipAsync(string pythonPath, IProgress<string> progress = null)
        {
            try
            {
                var pythonExe = Path.Combine(pythonPath, "python.exe");

                if (!File.Exists(pythonExe))
                {
                    throw new FileNotFoundException($"Python executable not found at {pythonExe}");
                }

                // Download get-pip.py
                progress?.Report("Downloading get-pip.py...");

                using var client = new HttpClient();
                var getPipScript = await client.GetStringAsync("https://bootstrap.pypa.io/get-pip.py");
                var getPipPath = Path.Combine(pythonPath, "get-pip.py");
                await File.WriteAllTextAsync(getPipPath, getPipScript);

                // Run get-pip.py
                progress?.Report("Running get-pip.py...");

                var pipProcess = Process.Start(new ProcessStartInfo
                {
                    FileName = pythonExe,
                    Arguments = $"\"{getPipPath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = pythonPath
                });

                if (pipProcess != null)
                {
                    await pipProcess.WaitForExitAsync();

                    if (pipProcess.ExitCode != 0)
                    {
                        var error = await pipProcess.StandardError.ReadToEndAsync();
                        throw new InvalidOperationException($"pip installation failed: {error}");
                    }
                }

                // Clean up get-pip.py
                File.Delete(getPipPath);

                // Upgrade pip and install base packages
                if (_config.AutoUpgradePip)
                {
                    foreach (var package in _config.BasePackages)
                    {
                        progress?.Report($"Upgrading {package}...");

                        var upgradeProcess = Process.Start(new ProcessStartInfo
                        {
                            FileName = pythonExe,
                            Arguments = $"-m pip install --upgrade {package}",
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            WorkingDirectory = pythonPath
                        });

                        if (upgradeProcess != null)
                        {
                            await upgradeProcess.WaitForExitAsync();
                        }
                    }
                }

                progress?.Report("pip setup complete");
                return true;
            }
            catch (Exception ex)
            {
               Messaging.AddLogMessage("Beep", $"Failed to setup pip: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                return false;
            }
        }

        /// <summary>
        /// Verifies that the embedded Python installation is functional.
        /// </summary>
        public async Task<bool> VerifyEmbeddedInstallationAsync(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                    return false;

                var pythonExe = Path.Combine(path, "python.exe");
                if (!File.Exists(pythonExe))
                    return false;

                // Run diagnostics
                var diagnostics = await Task.Run(() => 
                    PythonEnvironmentDiagnostics.RunFullDiagnostics(path));

                return diagnostics.PythonFound && 
                       diagnostics.CanExecuteCode && 
                       diagnostics.PipFound;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Configuration for embedded Python provisioning.
    /// </summary>
    public class EmbeddedPythonConfig
    {
        public string Version { get; set; } = "3.11.9";
        
        public string DownloadUrlTemplate { get; set; } = 
            "https://www.python.org/ftp/python/{0}/python-{0}-embed-amd64.zip";
        
        public string InstallPath { get; set; } = 
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), 
                         ".beep-python", "embedded");
        
        public bool AutoUpgradePip { get; set; } = true;
        
        public string[] BasePackages { get; set; } = new[] { "pip", "setuptools", "wheel" };
    }

    /// <summary>
    /// Progress information for provisioning operations.
    /// </summary>
    public class ProvisioningProgress
    {
        public string Phase { get; set; }
        public string Message { get; set; }
        public double Percentage { get; set; }
        public long BytesDownloaded { get; set; }
        public long TotalBytes { get; set; }
    }

    /// <summary>
    /// Interface for embedded Python provisioning.
    /// </summary>
    public interface IPythonEmbeddedProvisioner
    {
        Task<PythonRunTime> ProvisionEmbeddedPythonAsync(
            string version = null,
            IProgress<ProvisioningProgress> progress = null,
            CancellationToken cancellationToken = default);

        Task<bool> SetupPipAsync(string pythonPath, IProgress<string> progress = null);

        Task<bool> VerifyEmbeddedInstallationAsync(string path);
    }
}
