using Beep.Python.RuntimeHost;
using Beep.Python.RuntimeHost.Commands;
using Beep.Python.RuntimeHost.Services;
using Beep.Python.RuntimeEngine;
using Beep.Python.RuntimeEngine.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Spectre.Console;

// Create host for dependency injection
var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // Register Infrastructure services
        services.AddSingleton<IPythonRuntimeManager, PythonRuntimeManager>();
        
        // Register IVenvManager using VenvManager from Infrastructure
        // VenvManager needs pythonPath which we'll get from IPythonRuntimeManager after initialization
        services.AddSingleton<IVenvManager>(sp =>
        {
            var runtimeManager = sp.GetRequiredService<IPythonRuntimeManager>();
            var logger = sp.GetRequiredService<ILogger<VenvManager>>();
            
            // Get Python path from default runtime or use default location
            // Note: RuntimeManager may not be initialized yet, so we use a default path
            // The actual path will be resolved when VenvManager creates environments
            var defaultBeepPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".beep-llm", "python");
            var pythonPath = Environment.GetEnvironmentVariable("BEEP_PYTHON_PATH") ?? defaultBeepPath;
            
            logger.LogDebug("IVenvManager factory selected python: {Path}", pythonPath);
            
            // VenvManager constructor: (ILogger<VenvManager>, string pythonPath, IModelCatalog? modelCatalog = null, IConfigurationManager? configurationManager = null)
            // modelCatalog and configurationManager are optional - VenvManager creates a new ModelCatalog if null
            return new VenvManager(logger, pythonPath, modelCatalog: null, configurationManager: null);
        });
        
        // Register VirtualEnvManager from Infrastructure
        services.AddSingleton<VirtualEnvManager>(sp =>
        {
            var venvManager = sp.GetRequiredService<IVenvManager>();
            var runtimeManager = sp.GetRequiredService<IPythonRuntimeManager>();
            var logger = sp.GetRequiredService<ILogger<VirtualEnvManager>>();
            return new VirtualEnvManager(venvManager, runtimeManager, logger);
        });
        
        // Register shell state
        services.AddSingleton<ShellState>();

        // Register backend client service for managing backend connections
        services.AddSingleton<BackendClientService>();

        // Register VenvBackendService for executing VenvManager operations through backend
        services.AddScoped<VenvBackendService>(sp =>
        {
            var backendService = sp.GetRequiredService<BackendClientService>();
            var logger = sp.GetRequiredService<ILogger<VenvBackendService>>();
            return new VenvBackendService(backendService.CurrentBackend, logger);
        });

        // Register command registry
        services.AddSingleton<CommandRegistry>();

        // Configure logging - quieter for shell mode
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Warning);
        });
    })
    .Build();

await host.StartAsync();

try
{
    // Check and setup Python environment before starting shell
    var pythonSetupSuccess = await EnsurePythonEnvironment(host.Services);
    if (!pythonSetupSuccess)
    {
        AnsiConsole.MarkupLine("[red]Failed to setup Python environment. Exiting...[/]");
        return;
    }

    // Start interactive shell
    var shell = new RuntimeHostShell(host.Services);
    await shell.Run();
}
finally
{
    await host.StopAsync();
    host.Dispose();
}

static async Task<bool> EnsurePythonEnvironment(IServiceProvider services)
{
    var runtimeManager = services.GetRequiredService<IPythonRuntimeManager>();
    
    await runtimeManager.Initialize();
    
    var defaultRuntime = runtimeManager.GetDefaultRuntime();
    if (defaultRuntime != null && defaultRuntime.Status == PythonRuntimeStatus.Ready)
    {
        // Python is already set up
        return true;
    }

    // Python doesn't exist - prompt for installation
    AnsiConsole.MarkupLine("[yellow]⚠ Python runtime not found[/]");
    AnsiConsole.MarkupLine("[dim]Beep.Python.Runtime.Host requires an embedded Python environment to run.[/]\n");

    if (!AnsiConsole.Confirm("[cyan]Would you like to install it now?[/]", true))
    {
        AnsiConsole.MarkupLine("[yellow]Python environment is required. You can install it later using the 'init' command.[/]");
        return false;
    }

    // Install Python environment through Infrastructure
    return await InstallPythonEnvironment(services);
}

static async Task<bool> InstallPythonEnvironment(IServiceProvider services)
{
    try
    {
        var runtimeManager = services.GetRequiredService<IPythonRuntimeManager>();
        
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold cyan]Setting up Embedded Python Environment[/]");
        AnsiConsole.MarkupLine("[dim]━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[yellow]This will:[/]");
        AnsiConsole.MarkupLine("  • Download Python 3.11.9 embedded (~25 MB)");
        AnsiConsole.MarkupLine("  • Install pip package manager");
        AnsiConsole.MarkupLine("  • Install virtualenv for isolated environments");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]Estimated time: 2-5 minutes (depending on internet speed)[/]");
        AnsiConsole.WriteLine();
        
        if (!AnsiConsole.Confirm("[cyan]Continue with installation?[/]", true))
        {
            AnsiConsole.MarkupLine("[yellow]Installation cancelled.[/]");
            return false;
        }
        
        AnsiConsole.WriteLine();
        
        // Initialize runtime manager (loads existing configs)
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("[yellow]Initializing runtime manager...[/]", async ctx =>
            {
                await runtimeManager.Initialize();
            });
        AnsiConsole.MarkupLine("[green]✓ Runtime manager initialized[/]");
        
        // Check if embedded runtime was created during initialization
        var defaultRuntime = runtimeManager.GetDefaultRuntime();
        if (defaultRuntime != null && defaultRuntime.Type == PythonRuntimeType.Embedded && defaultRuntime.Status == PythonRuntimeStatus.Ready)
        {
            AnsiConsole.MarkupLine("[green]✓ Python environment ready[/]");
            AnsiConsole.MarkupLine($"  [dim]Location: {defaultRuntime.Path}[/]");
            return true;
        }
        
        // Create managed runtime entry
        var runtimeId = await runtimeManager.CreateManagedRuntime("Default-Embedded", PythonRuntimeType.Embedded);
        
        // Initialize runtime (this will download and setup Python)
        var initSuccess = await runtimeManager.InitializeRuntime(runtimeId);
        
        if (initSuccess)
        {
            var runtime = runtimeManager.GetRuntime(runtimeId);
            if (runtime != null)
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[green bold]✓ Python environment setup completed![/]");
                AnsiConsole.MarkupLine($"  [dim]Location: {runtime.Path}[/]");
                AnsiConsole.WriteLine();
                return true;
            }
        }
        else
        {
            AnsiConsole.MarkupLine("[red]✗ Failed to initialize Python runtime[/]");
            return false;
        }
    }
    catch (Exception ex)
    {
        AnsiConsole.MarkupLine($"[red]✗ Error: {Markup.Escape(ex.Message)}[/]");
        return false;
    }
    
    return false;
}
