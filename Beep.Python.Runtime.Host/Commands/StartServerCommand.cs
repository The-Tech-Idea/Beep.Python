using Beep.Python.RuntimeEngine;
using Beep.Python.RuntimeEngine.Infrastructure;
using Beep.Python.RuntimeHost;
using Beep.Python.RuntimeHost.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace Beep.Python.RuntimeHost.Commands;

public class StartServerCommand : ICommand
{
    private readonly IPythonRuntimeManager _runtimeManager;
    private readonly VirtualEnvManager _virtualEnvManager;
    private readonly IVenvManager _venvManager;
    private readonly ShellState _state;
    private readonly ILogger<StartServerCommand> _logger;

    public StartServerCommand(
        IPythonRuntimeManager runtimeManager,
        VirtualEnvManager virtualEnvManager,
        IVenvManager venvManager,
        ShellState state,
        ILogger<StartServerCommand> logger)
    {
        _runtimeManager = runtimeManager ?? throw new ArgumentNullException(nameof(runtimeManager));
        _virtualEnvManager = virtualEnvManager ?? throw new ArgumentNullException(nameof(virtualEnvManager));
        _venvManager = venvManager ?? throw new ArgumentNullException(nameof(venvManager));
        _state = state ?? throw new ArgumentNullException(nameof(state));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string Name => "start";
    public string[] Aliases => new[] { "run", "serve" };
    public string Description => "Start a Python server (Http, Pipe, or Rpc)";

    public async Task<bool> ExecuteAsync(IServiceProvider services, string[] args)
    {
        // Validate configuration first using Infrastructure
        var configValid = await ValidateConfigurationAsync();
        if (!configValid)
        {
            AnsiConsole.MarkupLine("\n[yellow]⚠ Configuration check failed. Cannot start server.[/]");
            AnsiConsole.MarkupLine("[dim]Run [cyan]config[/] to see details or [cyan]init[/] to setup Python runtime[/]\n");
            return false;
        }

        // Parse backend type
        PythonBackendType backendType = PythonBackendType.Http;
        string? backendArg = args.Length > 0 ? args[0] : null;
        string? venvArg = args.Length > 1 ? args[1] : null;
        int? portArg = null;
        
        if (args.Length > 1 && int.TryParse(args[1], out var port))
        {
            portArg = port;
        }

        // Interactive selection if no args
        if (string.IsNullOrEmpty(backendArg))
        {
            backendArg = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[cyan]Select server backend:[/]")
                    .AddChoices(new[] { "Http", "Pipe", "Rpc" }));
        }

        if (!Enum.TryParse<PythonBackendType>(backendArg, ignoreCase: true, out backendType))
        {
            AnsiConsole.MarkupLine($"[red]ERROR: Invalid backend type: {backendArg}. Valid values: Http, Pipe, Rpc[/]");
            return false;
        }

        // Get venv path
        if (string.IsNullOrEmpty(venvArg))
        {
            venvArg = AnsiConsole.Prompt(
                new TextPrompt<string?>(
                    "[cyan]Virtual environment path (press Enter to create automatically):[/]")
                    .AllowEmpty());
        }

        // Ensure runtime manager is initialized
        if (!_runtimeManager.GetAvailableRuntimes().Any())
        {
            AnsiConsole.MarkupLine("[dim]Initializing runtime manager...[/]");
            await _runtimeManager.Initialize();
        }
        
        if (!string.IsNullOrEmpty(venvArg) && Directory.Exists(venvArg))
        {
            AnsiConsole.MarkupLine($"[dim]Using existing virtual environment: {venvArg}[/]");
        }
        else
        {
            AnsiConsole.MarkupLine("[dim]Virtual environment will be created using Infrastructure...[/]");
        }

        // Create virtual environment using Infrastructure if needed
        string? venvPath = venvArg;
        if (string.IsNullOrEmpty(venvPath) || !Directory.Exists(venvPath))
        {
            AnsiConsole.MarkupLine("[dim]Creating virtual environment using Infrastructure...[/]");
            var env = await _virtualEnvManager.CreateProviderEnvironmentAsync(
                "runtime-host",
                modelId: null,
                CancellationToken.None);
            
            if (env == null || string.IsNullOrEmpty(env.Path))
            {
                AnsiConsole.MarkupLine("[red]ERROR: Failed to create virtual environment using Infrastructure[/]");
                return false;
            }
            
            venvPath = env.Path;
            AnsiConsole.MarkupLine($"[dim]Virtual environment created at: {venvPath}[/]");
        }

        // Install required packages using Infrastructure
        var pythonExe = OperatingSystem.IsWindows()
            ? Path.Combine(venvPath, "Scripts", "python.exe")
            : Path.Combine(venvPath, "bin", "python");
        
        var scriptsDirectory = Path.Combine(AppContext.BaseDirectory, "python-servers");
        var requirementsPath = Path.Combine(scriptsDirectory, "requirements.txt");
        if (File.Exists(requirementsPath))
        {
            AnsiConsole.MarkupLine("[dim]Installing packages using Infrastructure...[/]");
            var pipArgs = $"-m pip install -r \"{requirementsPath}\"";
            await _venvManager.RunPipCommand(pythonExe, pipArgs, CancellationToken.None);
        }

        // Use Infrastructure PythonServerLauncher directly
        using var launcher = new PythonServerLauncher(venvPath, backendType, _logger);

        AnsiConsole.MarkupLine($"[yellow]Starting {backendType} server using Infrastructure...[/]");
        var started = await launcher.StartAsync();

        if (!started)
        {
            AnsiConsole.MarkupLine("[red]ERROR: Failed to start server[/]");
            return false;
        }

        _state.IsServerRunning = true;
        _state.CurrentServerType = backendType.ToString();
        _state.CurrentServerEndpoint = launcher.GetEndpoint();
        _state.CurrentVenvPath = launcher.VenvPath;

        AnsiConsole.MarkupLine($"[green]✓ Server started successfully![/]");
        AnsiConsole.MarkupLine($"[dim]Endpoint: {_state.CurrentServerEndpoint}[/]");
        AnsiConsole.MarkupLine("[dim]Press Ctrl+C to stop...[/]");

        // Wait for cancellation
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (s, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        try
        {
            await Task.Delay(Timeout.Infinite, cts.Token);
        }
        catch (OperationCanceledException)
        {
            AnsiConsole.MarkupLine("\n[yellow]Stopping server...[/]");
            launcher.Stop();
            _state.IsServerRunning = false;
            AnsiConsole.MarkupLine("[green]✓ Server stopped.[/]");
        }

        return false;
    }

    private async Task<bool> ValidateConfigurationAsync()
    {
        try
        {
            // Check Python runtime
            if (!_runtimeManager.GetAvailableRuntimes().Any())
            {
                await _runtimeManager.Initialize();
            }

            var defaultRuntime = _runtimeManager.GetDefaultRuntime();
            if (defaultRuntime == null || defaultRuntime.Status != PythonRuntimeStatus.Ready)
            {
                AnsiConsole.MarkupLine("[red]✗ Python runtime not found[/]");
                AnsiConsole.MarkupLine("[dim]Run 'init' to download and setup Python[/]");
                return false;
            }

            _state.IsInitialized = true;
            return true;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]✗ Configuration validation error: {Markup.Escape(ex.Message)}[/]");
            return false;
        }
    }
}
