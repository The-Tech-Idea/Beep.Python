using Beep.Python.Model;
using Beep.Python.RuntimeEngine.Configuration;
using Beep.Python.RuntimeEngine.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Container.Model.Data;
using TheTechIdea.Beep.Container.Services;
using TheTechIdea.Beep.Editor;
using SysEnv = System.Environment;

namespace Beep.Python.RuntimeEngine.Infrastructure
{
    #region Supporting Classes and Interfaces

    /// <summary>
    /// Interface for bootstrap manager.
    /// </summary>
    public interface IPythonBootstrapManager
    {
        /// <summary>
        /// Ensures a Python environment is ready with all specified configurations.
        /// </summary>
        Task<BootstrapResult> EnsurePythonEnvironmentAsync(
            BootstrapOptions options,
            IProgress<BootstrapProgress> progress = null,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Options for bootstrapping a Python environment.
    /// </summary>
    public class BootstrapOptions
    {
        /// <summary>
        /// Ensure embedded Python is provisioned if no runtime exists.
        /// </summary>
        public bool EnsureEmbeddedPython { get; set; } = true;

        /// <summary>
        /// Path for embedded Python installation. Defaults to ~/.beep-python/embedded.
        /// </summary>
        public string EmbeddedPythonPath { get; set; }

        /// <summary>
        /// Create a virtual environment for isolation.
        /// </summary>
        public bool CreateVirtualEnvironment { get; set; } = true;

        /// <summary>
        /// Path for virtual environment. Defaults to ~/.beep-python/venvs/{name}.
        /// </summary>
        public string VirtualEnvironmentPath { get; set; }

        /// <summary>
        /// Name for the environment (used in default path if VirtualEnvironmentPath not set).
        /// </summary>
        public string EnvironmentName { get; set; }

        /// <summary>
        /// Package profiles to install (e.g., "base", "data-science", "machine-learning").
        /// </summary>
        public List<string> PackageProfiles { get; set; } = new List<string>();

        /// <summary>
        /// Set the provisioned runtime as the default.
        /// </summary>
        public bool SetAsDefault { get; set; } = true;
    }

    /// <summary>
    /// Result of bootstrap operation.
    /// </summary>
    public class BootstrapResult
    {
        /// <summary>
        /// Whether bootstrap was successful.
        /// </summary>
        public bool IsSuccessful { get; set; }

        /// <summary>
        /// ID of the base Python runtime used or created.
        /// </summary>
        public string BaseRuntimeId { get; set; }

        /// <summary>
        /// Path to the final environment (virtual env or base runtime).
        /// </summary>
        public string EnvironmentPath { get; set; }

        /// <summary>
        /// Package profiles that were installed.
        /// </summary>
        public List<string> InstalledProfiles { get; set; } = new List<string>();

        /// <summary>
        /// Validation messages from verification step.
        /// </summary>
        public List<string> ValidationMessages { get; set; } = new List<string>();

        /// <summary>
        /// Bootstrap start time.
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Bootstrap end time.
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Original bootstrap options.
        /// </summary>
        public BootstrapOptions Options { get; set; }
    }

    /// <summary>
    /// Progress information for bootstrap operation.
    /// </summary>
    public class BootstrapProgress
    {
        /// <summary>
        /// Current bootstrap stage.
        /// </summary>
        public BootstrapStage Stage { get; set; }

        /// <summary>
        /// Percentage complete (0-100).
        /// </summary>
        public int PercentComplete { get; set; }

        /// <summary>
        /// Human-readable progress message.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Timestamp of this progress update.
        /// </summary>
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Stages of bootstrap process.
    /// </summary>
    public enum BootstrapStage
    {
        Initializing,
        InitializingRegistry,
        LoadingProfiles,
        CheckingRuntime,
        ProvisioningPython,
        RegisteringRuntime,
        CreatingVirtualEnv,
        InstallingPackages,
        Verifying,
        Complete,
        Failed
    }

    /// <summary>
    /// Result of environment verification.
    /// </summary>
    public class EnvironmentVerificationResult
    {
        /// <summary>
        /// Whether the environment passed validation.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Validation messages (info, warnings, errors).
        /// </summary>
        public List<string> Messages { get; set; } = new List<string>();
    }

    #endregion
    /// <summary>
    /// Orchestrates one-call Python environment setup by coordinating provisioner, registry, and package installation.
    /// Provides a streamlined API for ensuring Python environments are ready with required packages.
    /// </summary>
    public class PythonBootstrapManager : IPythonBootstrapManager
    {
        private readonly IPythonEmbeddedProvisioner _provisioner;
        private readonly IPythonRuntimeRegistry _registry;
        private readonly IPackageRequirementsManager _packageManager;
        private readonly IPythonVirtualEnvManager _venvManager;
        private readonly IBeepService _beepService;
        private readonly IDMEEditor _dmEditor;

        public PythonBootstrapManager(
            IPythonEmbeddedProvisioner provisioner,
            IPythonRuntimeRegistry registry,
            IPackageRequirementsManager packageManager,
            IPythonVirtualEnvManager venvManager,
            IBeepService beepService)
        {
            _provisioner = provisioner ?? throw new ArgumentNullException(nameof(provisioner));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _packageManager = packageManager ?? throw new ArgumentNullException(nameof(packageManager));
            _venvManager = venvManager ?? throw new ArgumentNullException(nameof(venvManager));
            _beepService = beepService ?? throw new ArgumentNullException(nameof(beepService));
            _dmEditor = beepService.DMEEditor;
        }

        /// <summary>
        /// Ensures a Python environment is ready with all specified configurations.
        /// This is the main one-call API for setting up Python.
        /// </summary>
        /// <param name="options">Bootstrap options specifying desired environment</param>
        /// <param name="progress">Progress reporter</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Bootstrap result with runtime information</returns>
        public async Task<BootstrapResult> EnsurePythonEnvironmentAsync(
            BootstrapOptions options,
            IProgress<BootstrapProgress> progress = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _dmEditor?.AddLogMessage("Beep", "🚀 Starting Python environment bootstrap...", DateTime.Now, 0, null, Errors.Ok);
                ReportProgress(progress, BootstrapStage.Initializing, 0, "Initializing bootstrap process...");

                var result = new BootstrapResult
                {
                    StartTime = DateTime.UtcNow,
                    Options = options
                };

                // Step 1: Initialize registry
                ReportProgress(progress, BootstrapStage.InitializingRegistry, 10, "Initializing runtime registry...");
                await _registry.InitializeAsync();

                // Step 2: Load package profiles
                ReportProgress(progress, BootstrapStage.LoadingProfiles, 20, "Loading package profiles...");
                await _packageManager.LoadProfilesAsync();

                // Step 3: Ensure base runtime exists
                PythonRuntimeInfo baseRuntime = null;
                if (options.EnsureEmbeddedPython)
                {
                    baseRuntime = await EnsureBaseRuntimeAsync(options, progress, cancellationToken);
                    result.BaseRuntimeId = baseRuntime.Id;
                }
                else
                {
                    // Use default runtime from registry
                    baseRuntime = _registry.GetDefaultRuntime();
                    if (baseRuntime == null)
                    {
                        throw new InvalidOperationException("No default Python runtime available. Enable EnsureEmbeddedPython or configure a default runtime.");
                    }
                    result.BaseRuntimeId = baseRuntime.Id;
                }

                // Step 4: Create or use virtual environment
                string targetEnvPath = null;
                if (options.CreateVirtualEnvironment)
                {
                    targetEnvPath = await CreateVirtualEnvironmentAsync(baseRuntime, options, progress, cancellationToken);
                    result.EnvironmentPath = targetEnvPath;
                }
                else
                {
                    // Use base runtime directly
                    targetEnvPath = Path.GetDirectoryName(baseRuntime.Path);
                    result.EnvironmentPath = targetEnvPath;
                }

                // Step 5: Install packages
                if (options.PackageProfiles?.Any() == true)
                {
                    await InstallPackagesAsync(baseRuntime, options.PackageProfiles, progress, cancellationToken);
                    result.InstalledProfiles = new List<string>(options.PackageProfiles);
                }

                // Step 6: Verify installation
                ReportProgress(progress, BootstrapStage.Verifying, 95, "Verifying installation...");
                var verificationResult = await VerifyEnvironmentAsync(baseRuntime, targetEnvPath, options.CreateVirtualEnvironment, cancellationToken);
                result.IsSuccessful = verificationResult.IsValid;
                result.ValidationMessages = verificationResult.Messages;

                result.EndTime = DateTime.UtcNow;
                ReportProgress(progress, BootstrapStage.Complete, 100, "Bootstrap complete!");

                _dmEditor?.AddLogMessage("Beep", $"✅ Bootstrap completed in {(result.EndTime - result.StartTime).TotalSeconds:F2}s", DateTime.Now, 0, null, Errors.Ok);

                return result;
            }
            catch (Exception ex)
            {
                _dmEditor?.AddLogMessage("Beep", $"❌ Bootstrap failed: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                ReportProgress(progress, BootstrapStage.Failed, 0, $"Bootstrap failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Ensures embedded Python is provisioned and registered.
        /// </summary>
        private async Task<PythonRuntimeInfo> EnsureBaseRuntimeAsync(
            BootstrapOptions options,
            IProgress<BootstrapProgress> progress,
            CancellationToken cancellationToken)
        {
            ReportProgress(progress, BootstrapStage.CheckingRuntime, 30, "Checking for embedded Python...");

            // Check if embedded Python already exists
            var existingRuntime = _registry.GetAvailableRuntimes()
                .FirstOrDefault(r => r.Type == PythonRuntimeType.Embedded && r.Status == PythonRuntimeStatus.Ready);

            if (existingRuntime != null)
            {
                _dmEditor?.AddLogMessage("Beep", $"✓ Using existing embedded Python: {existingRuntime.Name}", DateTime.Now, 0, null, Errors.Ok);
                return existingRuntime;
            }

            // Provision new embedded Python
            ReportProgress(progress, BootstrapStage.ProvisioningPython, 40, "Provisioning embedded Python...");

            var provisioningProgress = new Progress<ProvisioningProgress>(p =>
            {
                // Map provisioning progress to bootstrap progress
                var percentage = 40 + (int)(p.Percentage * 0.3); // 40-70%
                ReportProgress(progress, BootstrapStage.ProvisioningPython, percentage, p.Message);
            });

            var provisionedRuntime = await _provisioner.ProvisionEmbeddedPythonAsync(
                null,  // Use default version from config
                provisioningProgress,
                cancellationToken);

            if (provisionedRuntime == null)
            {
                throw new Exception("Failed to provision embedded Python");
            }

            // Register the new runtime
            ReportProgress(progress, BootstrapStage.RegisteringRuntime, 70, "Registering embedded Python runtime...");

            var runtimeId = await _registry.RegisterManagedRuntimeAsync(
                "Embedded Python",
                PythonRuntimeType.Embedded);

            var runtime = _registry.GetRuntime(runtimeId);

            // Set as default if requested
            if (options.SetAsDefault)
            {
                await _registry.SetDefaultRuntimeAsync(runtimeId);
            }

            return runtime;
        }

        /// <summary>
        /// Creates a virtual environment with the specified configuration.
        /// </summary>
        private async Task<string> CreateVirtualEnvironmentAsync(
            PythonRuntimeInfo baseRuntime,
            BootstrapOptions options,
            IProgress<BootstrapProgress> progress,
            CancellationToken cancellationToken)
        {
            ReportProgress(progress, BootstrapStage.CreatingVirtualEnv, 75, "Creating virtual environment...");

            var envPath = options.VirtualEnvironmentPath ?? Path.Combine(
                SysEnv.GetFolderPath(SysEnv.SpecialFolder.UserProfile),
                ".beep-python",
                "venvs",
                options.EnvironmentName ?? $"env_{DateTime.Now:yyyyMMddHHmmss}");

            // Check if virtual environment already exists
            if (Directory.Exists(envPath))
            {
                _dmEditor?.AddLogMessage("Beep", $"✓ Using existing virtual environment: {envPath}", DateTime.Now, 0, null, Errors.Ok);
                return await Task.FromResult(envPath);
            }

            // Create new virtual environment using synchronous method
            return await Task.Run(() =>
            {
                var config = new PythonRunTime { RuntimePath = baseRuntime.Path };
                var success = _venvManager.CreateVirtualEnvironment(config, envPath);
                
                if (!success)
                {
                    throw new Exception($"Failed to create virtual environment at {envPath}");
                }

                _dmEditor?.AddLogMessage("Beep", $"✅ Created virtual environment: {envPath}", DateTime.Now, 0, null, Errors.Ok);
                return envPath;
            }, cancellationToken);
        }

        /// <summary>
        /// Installs packages from specified profiles.
        /// </summary>
        private async Task InstallPackagesAsync(
            PythonRuntimeInfo runtime,
            IEnumerable<string> profileNames,
            IProgress<BootstrapProgress> progress,
            CancellationToken cancellationToken)
        {
            ReportProgress(progress, BootstrapStage.InstallingPackages, 80, "Installing package profiles...");

            var installProgress = new Progress<PackageInstallProgress>(p =>
            {
                var percentage = 80 + (int)((p.Current / (double)p.Total) * 15); // 80-95%
                var message = $"Installing packages: {p.PackageName} ({p.Current}/{p.Total})";
                ReportProgress(progress, BootstrapStage.InstallingPackages, percentage, message);
            });

            var runtimeConfig = new PythonRunTime { RuntimePath = runtime.Path };
            
            await _packageManager.InstallMultipleProfilesAsync(
                profileNames.ToList(),
                runtimeConfig,
                installProgress,
                cancellationToken);

            _dmEditor?.AddLogMessage("Beep", $"✅ Installed {profileNames.Count()} package profile(s)", DateTime.Now, 0, null, Errors.Ok);
        }

        /// <summary>
        /// Verifies that the environment is properly configured.
        /// </summary>
        private async Task<EnvironmentVerificationResult> VerifyEnvironmentAsync(
            PythonRuntimeInfo runtime,
            string environmentPath,
            bool isVirtualEnvironment,
            CancellationToken cancellationToken)
        {
            var result = new EnvironmentVerificationResult
            {
                IsValid = true,
                Messages = new List<string>()
            };

            // Verify Python path exists
            if (!Directory.Exists(runtime.Path) && !File.Exists(runtime.Path))
            {
                result.IsValid = false;
                result.Messages.Add($"Python runtime path not found: {runtime.Path}");
                return result;
            }

            result.Messages.Add($"✓ Python runtime: {runtime.Path}");

            // Verify environment directory structure
            if (!Directory.Exists(environmentPath))
            {
                result.IsValid = false;
                result.Messages.Add($"Environment directory not found: {environmentPath}");
                return result;
            }

            if (isVirtualEnvironment)
            {
                result.Messages.Add($"✓ Virtual environment: {environmentPath}");
            }
            else
            {
                result.Messages.Add($"✓ Base runtime: {environmentPath}");
            }

            return await Task.FromResult(result);
        }

        /// <summary>
        /// Reports progress to the provided progress reporter.
        /// </summary>
        private void ReportProgress(IProgress<BootstrapProgress> progress, BootstrapStage stage, int percentage, string message)
        {
            progress?.Report(new BootstrapProgress
            {
                Stage = stage,
                PercentComplete = percentage,
                Message = message,
                Timestamp = DateTime.UtcNow
            });
        }
    }
}
