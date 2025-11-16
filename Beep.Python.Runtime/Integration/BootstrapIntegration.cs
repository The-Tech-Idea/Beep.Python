using Beep.Python.Model;
using Beep.Python.RuntimeEngine.Configuration;
using Beep.Python.RuntimeEngine.Infrastructure;
using Beep.Python.RuntimeEngine.Monitoring;
using Beep.Python.RuntimeEngine.Templates;
using System;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Container.Services;

namespace Beep.Python.RuntimeEngine.Integration
{
    /// <summary>
    /// Provides integration helpers for initializing and managing Python environments
    /// with the bootstrap system. Simplifies common integration scenarios.
    /// </summary>
    public static class BootstrapIntegration
    {
        /// <summary>
        /// Creates a fully configured bootstrap manager with all dependencies.
        /// </summary>
        public static PythonBootstrapManager CreateBootstrapManager(
            IBeepService beepService,
            IPythonRunTimeManager pythonRuntime = null,
            IPythonVirtualEnvManager venvManager = null)
        {
            // Create configuration for embedded provisioner
            var embeddedConfig = new EmbeddedPythonConfig
            {
                Version = "3.11.9",
                InstallPath = System.IO.Path.Combine(
                    System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile),
                    ".beep-python",
                    "embedded")
            };

            // Create provisioner
            var provisioner = new PythonEmbeddedProvisioner(beepService, embeddedConfig);

            // Create registry
            var registry = new PythonRuntimeRegistry(beepService);

            // Create package manager
            var packageManager = new PackageRequirementsManager(beepService);

            // Use provided or create new virtual env manager
            venvManager ??= new PythonVirtualEnvManager(beepService, pythonRuntime);

            // Create and return bootstrap manager
            return new PythonBootstrapManager(
                provisioner,
                registry,
                packageManager,
                venvManager,
                beepService);
        }

        /// <summary>
        /// Quick setup using a template - one line initialization.
        /// </summary>
        public static async Task<BootstrapResult> QuickSetupAsync(
            IBeepService beepService,
            string templateName = "data-science",
            IProgress<BootstrapProgress> progress = null,
            CancellationToken cancellationToken = default)
        {
            var bootstrapManager = CreateBootstrapManager(beepService);
            var template = EnvironmentTemplates.GetTemplate(templateName);

            if (template == null)
            {
                throw new ArgumentException($"Template '{templateName}' not found. Available templates: {string.Join(", ", EnvironmentTemplates.GetAvailableTemplates())}");
            }

            return await bootstrapManager.EnsurePythonEnvironmentAsync(template, progress, cancellationToken);
        }

        /// <summary>
        /// Setup with custom options.
        /// </summary>
        public static async Task<BootstrapResult> CustomSetupAsync(
            IBeepService beepService,
            BootstrapOptions options,
            IProgress<BootstrapProgress> progress = null,
            CancellationToken cancellationToken = default)
        {
            var bootstrapManager = CreateBootstrapManager(beepService);
            return await bootstrapManager.EnsurePythonEnvironmentAsync(options, progress, cancellationToken);
        }

        /// <summary>
        /// Initialize with health monitoring enabled.
        /// </summary>
        public static async Task<(BootstrapResult result, IPythonHealthMonitor monitor)> SetupWithMonitoringAsync(
            IBeepService beepService,
            BootstrapOptions options,
            int monitoringIntervalMinutes = 30,
            IProgress<BootstrapProgress> progress = null,
            CancellationToken cancellationToken = default)
        {
            var bootstrapManager = CreateBootstrapManager(beepService);
            var result = await bootstrapManager.EnsurePythonEnvironmentAsync(options, progress, cancellationToken);

            // Create and start health monitor
            var registry = new PythonRuntimeRegistry(beepService);
            await registry.InitializeAsync();

            var monitor = new PythonHealthMonitor(registry, beepService);
            monitor.StartMonitoring(monitoringIntervalMinutes);

            return (result, monitor);
        }

        /// <summary>
        /// Ensures Python environment exists, creating if necessary.
        /// Returns runtime information for use with existing managers.
        /// </summary>
        public static async Task<PythonRunTime> EnsureRuntimeAsync(
            IBeepService beepService,
            string templateName = "minimal",
            CancellationToken cancellationToken = default)
        {
            var result = await QuickSetupAsync(beepService, templateName, cancellationToken: cancellationToken);

            if (!result.IsSuccessful)
            {
                throw new InvalidOperationException($"Failed to ensure Python runtime: {string.Join(", ", result.ValidationMessages)}");
            }

            // Get the runtime registry to retrieve runtime info
            var registry = new PythonRuntimeRegistry(beepService);
            await registry.InitializeAsync();

            var runtime = registry.GetRuntime(result.BaseRuntimeId);
            
            return new PythonRunTime
            {
                RuntimePath = runtime.Path,
                BinPath = runtime.Path
            };
        }

        /// <summary>
        /// Console progress reporter with colored output.
        /// </summary>
        public static IProgress<BootstrapProgress> CreateConsoleProgress()
        {
            return new Progress<BootstrapProgress>(p =>
            {
                var originalColor = Console.ForegroundColor;

                switch (p.Stage)
                {
                    case BootstrapStage.Initializing:
                    case BootstrapStage.InitializingRegistry:
                    case BootstrapStage.LoadingProfiles:
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        break;
                    case BootstrapStage.ProvisioningPython:
                    case BootstrapStage.CreatingVirtualEnv:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        break;
                    case BootstrapStage.InstallingPackages:
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        break;
                    case BootstrapStage.Complete:
                        Console.ForegroundColor = ConsoleColor.Green;
                        break;
                    case BootstrapStage.Failed:
                        Console.ForegroundColor = ConsoleColor.Red;
                        break;
                }

                Console.WriteLine($"[{p.PercentComplete,3}%] {p.Stage,-20} {p.Message}");
                Console.ForegroundColor = originalColor;
            });
        }

        /// <summary>
        /// Creates a simple progress reporter that logs to DMEEditor.
        /// </summary>
        public static IProgress<BootstrapProgress> CreateLogProgress(IBeepService beepService)
        {
            return new Progress<BootstrapProgress>(p =>
            {
                var dmEditor = beepService?.DMEEditor;
                var errorLevel = p.Stage == BootstrapStage.Failed ? Errors.Failed : Errors.Ok;
                
                dmEditor?.AddLogMessage(
                    "Beep.Python.Bootstrap",
                    $"[{p.PercentComplete}%] {p.Stage}: {p.Message}",
                    DateTime.Now,
                    0,
                    null,
                    errorLevel);
            });
        }
    }

    /// <summary>
    /// Extension methods for convenient bootstrap integration.
    /// </summary>
    public static class BootstrapExtensions
    {
        /// <summary>
        /// Extension method for IBeepService to enable quick Python setup.
        /// </summary>
        public static async Task<BootstrapResult> EnsurePythonAsync(
            this IBeepService beepService,
            string template = "minimal",
            IProgress<BootstrapProgress> progress = null)
        {
            return await BootstrapIntegration.QuickSetupAsync(beepService, template, progress);
        }

        /// <summary>
        /// Extension method for IBeepService to get a configured Python runtime.
        /// </summary>
        public static async Task<PythonRunTime> GetPythonRuntimeAsync(
            this IBeepService beepService,
            string template = "minimal")
        {
            return await BootstrapIntegration.EnsureRuntimeAsync(beepService, template);
        }
    }
}
