using Spectre.Console;

namespace Beep.Python.RuntimeHost.Commands;

public class ExitCommand : ICommand
{
    public string Name => "exit";
    public string[] Aliases => new[] { "quit", "q" };
    public string Description => "Exit the console";

    public Task<bool> ExecuteAsync(IServiceProvider services, string[] args)
    {
        AnsiConsole.MarkupLine("[yellow]Goodbye![/]");
        return Task.FromResult(true); // Exit the application
    }
}
