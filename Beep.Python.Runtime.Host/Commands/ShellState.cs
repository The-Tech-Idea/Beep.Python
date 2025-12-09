namespace Beep.Python.RuntimeHost.Commands;

/// <summary>
/// Shared state for the console session
/// </summary>
public class ShellState
{
    public bool IsInitialized { get; set; }
    public List<string> CommandHistory { get; } = new();
    public int HistoryIndex { get; set; } = -1;
    
    // Server state
    public string? CurrentServerType { get; set; }
    public string? CurrentServerEndpoint { get; set; }
    public bool IsServerRunning { get; set; }
    public string? CurrentVenvPath { get; set; }
    
    // Whether this process is currently running the interactive shell
    public bool IsInInteractiveShell { get; set; }
}
