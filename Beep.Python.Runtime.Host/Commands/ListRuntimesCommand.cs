using Beep.Python.RuntimeEngine.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace Beep.Python.RuntimeHost.Commands;

public class ListRuntimesCommand : ICommand
{
    private readonly IPythonRuntimeManager _runtimeManager;
    private readonly ILogger<ListRuntimesCommand> _logger;

    public ListRuntimesCommand(
        IPythonRuntimeManager runtimeManager,
        ILogger<ListRuntimesCommand> logger)
    {
        _runtimeManager = runtimeManager ?? throw new ArgumentNullException(nameof(runtimeManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string Name => "list";
    public string[] Aliases => new[] { "runtimes", "ls" };
    public string Description => "List available Python runtimes";

    public async Task<bool> ExecuteAsync(IServiceProvider services, string[] args)
    {
        if (!_runtimeManager.GetAvailableRuntimes().Any())
        {
            await _runtimeManager.Initialize();
        }

        var runtimes = _runtimeManager.GetAvailableRuntimes().ToList();
        
        if (runtimes.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No runtimes found. Run [green]init[/] to create one.[/]");
            return false;
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey);

        table.AddColumn("[bold cyan]Name[/]");
        table.AddColumn("[bold cyan]Version[/]");
        table.AddColumn("[bold cyan]Type[/]");
        table.AddColumn("[bold cyan]Status[/]");
        table.AddColumn("[bold cyan]Path[/]");

        foreach (var runtime in runtimes)
        {
            var statusColor = runtime.Status == PythonRuntimeStatus.Ready ? "green" : "yellow";
            var statusText = runtime.Status == PythonRuntimeStatus.Ready ? "✓ Ready" : runtime.Status.ToString();
            
            table.AddRow(
                $"[cyan]{Markup.Escape(runtime.Name)}[/]",
                $"[dim]{Markup.Escape(runtime.Version)}[/]",
                $"[dim]{runtime.Type}[/]",
                $"[{statusColor}]{statusText}[/]",
                $"[dim]{Markup.Escape(runtime.Path)}[/]"
            );
        }

        AnsiConsole.MarkupLine($"[bold]Available Python Runtimes ({runtimes.Count}):[/]\n");
        AnsiConsole.Write(table);
        
        return false;
    }
}
