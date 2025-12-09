using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;

namespace Beep.Python.RuntimeHost.Commands;

public class StopServerCommand : ICommand
{
    public string Name => "stop";
    public string[] Aliases => Array.Empty<string>();
    public string Description => "Stop a running server";

    public Task<bool> ExecuteAsync(IServiceProvider services, string[] args)
    {
        var state = services.GetRequiredService<ShellState>();
        
        if (!state.IsServerRunning)
        {
            AnsiConsole.MarkupLine("[yellow]No server is currently running.[/]");
            return Task.FromResult(false);
        }

        AnsiConsole.MarkupLine($"[yellow]Stopping {state.CurrentServerType} server...[/]");
        // Server cleanup is handled by the StartServerCommand when Ctrl+C is pressed
        // This command is for informational purposes
        AnsiConsole.MarkupLine("[dim]Use Ctrl+C in the server process to stop it.[/]");
        
        return Task.FromResult(false);
    }
}
