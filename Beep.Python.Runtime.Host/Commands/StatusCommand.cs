using Beep.Python.RuntimeEngine.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace Beep.Python.RuntimeHost.Commands;

public class StatusCommand : ICommand
{
    private readonly IPythonRuntimeManager _runtimeManager;
    private readonly ShellState _state;
    private readonly ILogger<StatusCommand> _logger;

    public StatusCommand(
        IPythonRuntimeManager runtimeManager,
        ShellState state,
        ILogger<StatusCommand> logger)
    {
        _runtimeManager = runtimeManager ?? throw new ArgumentNullException(nameof(runtimeManager));
        _state = state ?? throw new ArgumentNullException(nameof(state));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string Name => "status";
    public string[] Aliases => new[] { "st", "info" };
    public string Description => "Show system status";

    public async Task<bool> ExecuteAsync(IServiceProvider services, string[] args)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey);

        table.AddColumn("[bold cyan]Property[/]");
        table.AddColumn("[bold cyan]Value[/]");

        // Initialization status
        table.AddRow(
            "[yellow]Initialized[/]",
            _state.IsInitialized ? "[green]✓ Yes[/]" : "[red]✗ No[/]"
        );

        // Server status
        if (_state.IsServerRunning)
        {
            table.AddRow("[yellow]Server Status[/]", "[green]✓ Running[/]");
            table.AddRow("[yellow]Server Type[/]", $"[cyan]{_state.CurrentServerType}[/]");
            table.AddRow("[yellow]Endpoint[/]", $"[dim]{_state.CurrentServerEndpoint}[/]");
            table.AddRow("[yellow]Virtual Env[/]", $"[dim]{_state.CurrentVenvPath ?? "N/A"}[/]");
        }
        else
        {
            table.AddRow("[yellow]Server Status[/]", "[dim]Not running[/]");
        }

        // Runtime information using Infrastructure
        try
        {
            if (!_runtimeManager.GetAvailableRuntimes().Any())
            {
                await _runtimeManager.Initialize();
            }
            
            var defaultRuntime = _runtimeManager.GetDefaultRuntime();
            if (defaultRuntime != null)
            {
                table.AddRow("[yellow]Python Runtime[/]", $"[cyan]{defaultRuntime.Name}[/]");
                table.AddRow("[yellow]Python Version[/]", $"[dim]{defaultRuntime.Version}[/]");
                table.AddRow("[yellow]Runtime Path[/]", $"[dim]{defaultRuntime.Path}[/]");
                table.AddRow("[yellow]Runtime Type[/]", $"[dim]{defaultRuntime.Type}[/]");
            }
            
            var runtimes = _runtimeManager.GetAvailableRuntimes();
            table.AddRow("[yellow]Total Runtimes[/]", $"[cyan]{runtimes.Count()}[/]");
        }
        catch (Exception ex)
        {
            table.AddRow("[yellow]Runtime Info[/]", $"[red]Error: {Markup.Escape(ex.Message)}[/]");
        }

        AnsiConsole.Write(table);
        return false;
    }
}
