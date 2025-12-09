using Spectre.Console;

namespace Beep.Python.RuntimeHost.Commands;

public class ClearCommand : ICommand
{
    public string Name => "clear";
    public string[] Aliases => new[] { "cls" };
    public string Description => "Clear the screen";

    public Task<bool> ExecuteAsync(IServiceProvider services, string[] args)
    {
        AnsiConsole.Clear();
        return Task.FromResult(false);
    }
}
