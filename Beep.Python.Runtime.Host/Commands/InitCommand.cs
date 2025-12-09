using Beep.Python.RuntimeEngine.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace Beep.Python.RuntimeHost.Commands;

public class InitCommand : ICommand
{
    private readonly IPythonRuntimeManager _runtimeManager;
    private readonly ShellState _state;
    private readonly ILogger<InitCommand> _logger;

    public InitCommand(
        IPythonRuntimeManager runtimeManager,
        ShellState state,
        ILogger<InitCommand> logger)
    {
        _runtimeManager = runtimeManager ?? throw new ArgumentNullException(nameof(runtimeManager));
        _state = state ?? throw new ArgumentNullException(nameof(state));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string Name => "init";
    public string[] Aliases => new[] { "i" };
    public string Description => "Initialize Python runtime environment (downloads embedded Python)";

    public async Task<bool> ExecuteAsync(IServiceProvider services, string[] args)
    {
        var rule = new Rule("[bold green]Python Runtime Initialization[/]");
        AnsiConsole.Write(rule);
        AnsiConsole.MarkupLine("\n[dim]Setting up Python runtime environment for servers using Infrastructure...[/]\n");

        if (_state.IsInitialized)
        {
            AnsiConsole.MarkupLine("[green]✓ Environment already initialized[/]");
            
            if (!AnsiConsole.Confirm("[yellow]Re-initialize? This will verify the Python installation.[/]", false))
            {
                return false;
            }
        }

        return await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("[yellow]Setting up Python runtime...[/]", async ctx =>
            {
                try
                {
                    ctx.Status("Initializing runtime manager using Infrastructure...");
                    await _runtimeManager.Initialize();

                    var defaultRuntime = _runtimeManager.GetDefaultRuntime();
                    if (defaultRuntime == null || defaultRuntime.Status != PythonRuntimeStatus.Ready)
                    {
                        // Create embedded runtime
                        ctx.Status("Creating embedded Python runtime using Infrastructure...");
                        AnsiConsole.MarkupLine("[dim]Creating embedded Python runtime entry...[/]");
                        
                        var runtimeId = await _runtimeManager.CreateManagedRuntime("Default-Embedded", PythonRuntimeType.Embedded);
                        
                        // Initialize runtime (this will download and setup Python)
                        ctx.Status("Downloading and setting up Python using Infrastructure...");
                        var initSuccess = await _runtimeManager.InitializeRuntime(runtimeId);
                        
                        if (!initSuccess)
                        {
                            AnsiConsole.MarkupLine("[red]✗ Failed to initialize Python runtime[/]");
                            return false;
                        }
                        
                        defaultRuntime = _runtimeManager.GetRuntime(runtimeId);
                    }
                    
                    if (defaultRuntime == null)
                    {
                        AnsiConsole.MarkupLine("[red]✗ No Python runtime available[/]");
                        return false;
                    }

                    AnsiConsole.MarkupLine("[green]✓ Python runtime ready[/]");
                    AnsiConsole.MarkupLine($"  [dim]Location: {defaultRuntime.Path}[/]");
                    AnsiConsole.MarkupLine($"  [dim]Version: {defaultRuntime.Version}[/]");
                    
                    _state.IsInitialized = true;
                    
                    AnsiConsole.WriteLine();
                    AnsiConsole.Write(new Panel("[green]Initialization Complete![/]")
                    {
                        Border = BoxBorder.Rounded,
                        BorderStyle = new Style(Color.Green)
                    });
                    
                    return false;
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]✗ Error during initialization: {Markup.Escape(ex.Message)}[/]");
                    _logger.LogError(ex, "Failed to initialize Python runtime");
                    return false;
                }
            });
    }
}
