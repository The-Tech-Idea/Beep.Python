using Spectre.Console;
using Microsoft.Extensions.DependencyInjection;

namespace Beep.Python.RuntimeHost.Commands;

public class MainMenuCommand : ICommand
{
    private readonly ShellState _state;
    private readonly IServiceProvider _services;

    public string Name => "menu";
    public string[] Aliases => new[] { "m" };
    public string Description => "Show interactive menu";

    public MainMenuCommand(ShellState state, IServiceProvider services)
    {
        _state = state;
        _services = services;
    }

    public async Task<bool> ExecuteAsync(IServiceProvider services, string[] args)
    {
        AnsiConsole.Clear();
        var rule = new Rule("[bold cyan]Beep.Python.Runtime.Host - Interactive Menu[/]");
        rule.Centered();
        AnsiConsole.Write(rule);
        AnsiConsole.WriteLine();

        while (true)
        {
            // Build status panel
            var serverStatus = _state.IsServerRunning 
                ? $"[green]{_state.CurrentServerType} - {_state.CurrentServerEndpoint}[/]"
                : "[dim]Not running[/]";
            
            var initStatus = _state.IsInitialized ? "[green]✓ Initialized[/]" : "[yellow]Not initialized[/]";

            var panel = new Panel(
                $"[cyan]Initialization:[/] {initStatus}\n" +
                $"[cyan]Server:[/] {serverStatus}")
            {
                Header = new PanelHeader("[bold]Current State[/]"),
                Border = BoxBorder.Rounded,
                Padding = new Padding(1, 0)
            };
            AnsiConsole.Write(panel);
            AnsiConsole.WriteLine();

            var commandRegistry = _services.GetRequiredService<CommandRegistry>();
            var commands = commandRegistry.GetAllCommands()
                .Where(c => c.Name != "menu" && c.Name != "exit")
                .ToList();

            var choices = commands.Select(c => 
            {
                var label = c.Name switch
                {
                    "init" => "🧰 Initialize Python Runtime",
                    "start" => "▶ Start Server",
                    "stop" => "⏹ Stop Server",
                    "status" => "📊 Status",
                    "list" => "📋 List Runtimes",
                    "help" => "❓ Help",
                    "clear" => "🧹 Clear Screen",
                    _ => c.Description
                };
                return (label, command: c);
            }).ToList();

            choices.Add(("🚪 Exit", new ExitCommand()));

            var choiceText = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold cyan]Main Menu - Choose an action[/]")
                    .PageSize(12)
                    .HighlightStyle(new Style(Color.Cyan1))
                    .AddChoices(choices.Select(c => c.label))
            );

            var selected = choices.FirstOrDefault(c => c.label == choiceText);

            if (selected.command == null)
                continue;

            try
            {
                AnsiConsole.Clear();
                var shouldExit = await selected.command.ExecuteAsync(_services, Array.Empty<string>());
                
                if (shouldExit)
                    return true;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"\n[red]Error:[/] {Markup.Escape(ex.Message)}\n");
            }

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[dim]Press any key to return to the menu...[/]");
            Console.ReadKey(true);
            AnsiConsole.Clear();
        }
    }
}
