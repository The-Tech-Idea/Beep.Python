using Spectre.Console;
using Microsoft.Extensions.DependencyInjection;

namespace Beep.Python.RuntimeHost.Commands;

public class HelpCommand : ICommand
{
    public string Name => "help";
    public string[] Aliases => new[] { "?", "h" };
    public string Description => "Show help information";

    public Task<bool> ExecuteAsync(IServiceProvider services, string[] args)
    {
        var table = new Table();
        table.Border(TableBorder.Rounded);
        table.AddColumn("[bold cyan]Command[/]");
        table.AddColumn("[bold cyan]Description[/]");

        table.AddRow("[green]init[/]", "Initialize Python runtime environment (downloads embedded Python)");
        table.AddEmptyRow();

        table.AddRow("[bold]Server Management[/]", "");
        table.AddRow("[blue]start[/] [dim](backend) (venv) (port)[/]", "Start a Python server (Http, Pipe, or Rpc)");
        table.AddRow("[blue]stop[/]", "Stop a running server");
        table.AddRow("[blue]status[/] | [blue]st[/]", "Show system status");
        table.AddEmptyRow();

        table.AddRow("[bold]Information[/]", "");
        table.AddRow("[cyan]list[/] | [cyan]runtimes[/]", "List available Python runtimes");
        table.AddRow("[cyan]help[/] | [cyan]?[/]", "Show this help");
        table.AddEmptyRow();

        table.AddRow("[dim]clear[/] | [dim]cls[/]", "Clear screen");
        table.AddRow("[dim]exit[/] | [dim]quit[/] | [dim]q[/]", "Exit console");
        table.AddRow("[dim]menu[/] | [dim]m[/]", "Show interactive menu");

        AnsiConsole.Write(table);

        AnsiConsole.MarkupLine("\n[bold]Examples:[/]");
        AnsiConsole.MarkupLine("  [dim]runtime-host>[/] [cyan]init[/]");
        AnsiConsole.MarkupLine("  [dim]runtime-host>[/] [cyan]start Http[/]");
        AnsiConsole.MarkupLine("  [dim]runtime-host>[/] [cyan]start Pipe --venv C:\\path\\to\\venv[/]");
        AnsiConsole.MarkupLine("  [dim]runtime-host>[/] [cyan]start Rpc --port 50051[/]");
        AnsiConsole.MarkupLine("  [dim]runtime-host>[/] [cyan]list[/]");
        AnsiConsole.MarkupLine("  [dim]runtime-host>[/] [cyan]status[/]");

        return Task.FromResult(false);
    }
}
