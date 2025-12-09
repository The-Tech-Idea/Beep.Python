namespace Beep.Python.RuntimeHost.Commands;

/// <summary>
/// Base interface for all console commands
/// </summary>
public interface ICommand
{
    /// <summary>
    /// Command name (e.g., "init", "start", "status")
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Command aliases (e.g., ["i"] for init)
    /// </summary>
    string[] Aliases { get; }

    /// <summary>
    /// Short description of the command
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Execute the command interactively
    /// </summary>
    Task<bool> ExecuteAsync(IServiceProvider services, string[] args);
}
