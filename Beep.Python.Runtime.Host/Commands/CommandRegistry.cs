using Microsoft.Extensions.DependencyInjection;

namespace Beep.Python.RuntimeHost.Commands;

/// <summary>
/// Registry for discovering and executing console commands
/// </summary>
public class CommandRegistry
{
    private readonly Dictionary<string, ICommand> _commands = new();
    private readonly Dictionary<string, string> _aliases = new();

    public CommandRegistry(IServiceProvider services)
    {
        // Register all commands via ActivatorUtilities to allow DI constructor injection
        RegisterCommand((ICommand)ActivatorUtilities.CreateInstance(services, typeof(InitCommand)));
        RegisterCommand((ICommand)ActivatorUtilities.CreateInstance(services, typeof(ConfigCommand)));
        RegisterCommand((ICommand)ActivatorUtilities.CreateInstance(services, typeof(StartServerCommand)));
        RegisterCommand((ICommand)ActivatorUtilities.CreateInstance(services, typeof(StopServerCommand)));
        RegisterCommand((ICommand)ActivatorUtilities.CreateInstance(services, typeof(StatusCommand)));
        RegisterCommand((ICommand)ActivatorUtilities.CreateInstance(services, typeof(ListRuntimesCommand)));
        RegisterCommand((ICommand)ActivatorUtilities.CreateInstance(services, typeof(VenvCommand)));
        RegisterCommand((ICommand)ActivatorUtilities.CreateInstance(services, typeof(HelpCommand)));
        RegisterCommand((ICommand)ActivatorUtilities.CreateInstance(services, typeof(ClearCommand)));
        RegisterCommand((ICommand)ActivatorUtilities.CreateInstance(services, typeof(ExitCommand)));
        RegisterCommand((ICommand)ActivatorUtilities.CreateInstance(services, typeof(MainMenuCommand)));
    }

    private void RegisterCommand(ICommand command)
    {
        _commands[command.Name.ToLower()] = command;
        
        foreach (var alias in command.Aliases)
        {
            _aliases[alias.ToLower()] = command.Name.ToLower();
        }
    }

    public ICommand? GetCommand(string commandName)
    {
        var key = commandName.ToLower();
        
        // Check if it's an alias first
        if (_aliases.TryGetValue(key, out var actualName))
        {
            key = actualName;
        }

        // Return the command
        _commands.TryGetValue(key, out var command);
        return command;
    }

    public IEnumerable<ICommand> GetAllCommands()
    {
        return _commands.Values;
    }
}
