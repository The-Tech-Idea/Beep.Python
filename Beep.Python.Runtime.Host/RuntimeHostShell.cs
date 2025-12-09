using Beep.Python.RuntimeHost.Commands;
using Beep.Python.RuntimeEngine.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;

namespace Beep.Python.RuntimeHost;

/// <summary>
/// Interactive shell for Beep.Python.Runtime.Host using Spectre.Console
/// </summary>
class RuntimeHostShell
{
    private readonly IServiceProvider _services;
    private readonly CommandRegistry _commandRegistry;
    private readonly ShellState _state;

    public RuntimeHostShell(IServiceProvider services)
    {
        _services = services;
        _commandRegistry = services.GetRequiredService<CommandRegistry>();
        _state = services.GetRequiredService<ShellState>();
    }

    public async Task Run()
    {
        await InitializeShellContext();
        _state.IsInInteractiveShell = true;
        ShowWelcome();

        try
        {
            while (true)
            {
                var input = ReadLineWithHistory();

                if (string.IsNullOrEmpty(input))
                    continue;

                // Add to history if it's not a duplicate of the last command
                if (_state.CommandHistory.Count == 0 || _state.CommandHistory[^1] != input)
                {
                    _state.CommandHistory.Add(input);
                }
                _state.HistoryIndex = -1; // Reset history navigation

                var parts = ParseCommand(input);
                if (parts.Length == 0) continue;

                var commandName = parts[0].ToLower();
                var args = parts.Skip(1).ToArray();

                try
                {
                    var command = _commandRegistry.GetCommand(commandName);
                    if (command == null)
                    {
                        AnsiConsole.MarkupLine($"[red]Unknown command:[/] {Markup.Escape(commandName)}");
                        AnsiConsole.MarkupLine("[dim]Type 'help' for available commands or 'menu' for interactive menu[/]");
                        continue;
                    }

                    var shouldExit = await command.ExecuteAsync(_services, args);
                    if (shouldExit)
                        break;
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
                }
            }

            AnsiConsole.MarkupLine("\n[yellow]Goodbye![/]");
        }
        finally
        {
            _state.IsInInteractiveShell = false;
        }
    }

    private void ShowWelcome()
    {
        AnsiConsole.Clear();
        
        var rule = new Rule("[bold blue]Beep.Python.Runtime.Host[/]");
        rule.Centered();
        AnsiConsole.Write(rule);
        
        AnsiConsole.MarkupLine("\n[dim]Python Server Host for Runtime Infrastructure[/]");
        AnsiConsole.MarkupLine("[dim]Type [cyan]help[/] for available commands or [cyan]menu[/] for interactive menu[/]\n");
    }

    private string[] ParseCommand(string input)
    {
        var parts = new List<string>();
        var current = "";
        var inQuotes = false;

        foreach (var ch in input)
        {
            if (ch == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (ch == ' ' && !inQuotes)
            {
                if (!string.IsNullOrEmpty(current))
                {
                    parts.Add(current);
                    current = "";
                }
            }
            else
            {
                current += ch;
            }
        }

        if (!string.IsNullOrEmpty(current))
            parts.Add(current);

        return parts.ToArray();
    }

    private async Task InitializeShellContext()
    {
        try
        {
            // Initialize runtime manager and get default runtime using Infrastructure
            var runtimeManager = _services.GetRequiredService<IPythonRuntimeManager>();
            
            if (!runtimeManager.GetAvailableRuntimes().Any())
            {
                await runtimeManager.Initialize();
            }

            var defaultRuntime = runtimeManager.GetDefaultRuntime();
            if (defaultRuntime != null && defaultRuntime.Status == PythonRuntimeStatus.Ready)
            {
                _state.IsInitialized = true;
            }
        }
        catch (Exception ex)
        {
            // Don't let initialization errors crash the shell
            AnsiConsole.MarkupLine($"[yellow]Warning: Could not initialize shell context: {ex.Message}[/]");
        }
    }

    private string ReadLineWithHistory()
    {
        AnsiConsole.Markup("[bold cyan]runtime-host>[/] ");
        var promptLeft = Console.CursorLeft;
        var promptTop = Console.CursorTop;
        
        var input = new System.Text.StringBuilder();
        var cursorPosition = 0;

        while (true)
        {
            var key = Console.ReadKey(intercept: true);

            switch (key.Key)
            {
                case ConsoleKey.Enter:
                    Console.WriteLine();
                    return input.ToString().Trim();

                case ConsoleKey.UpArrow:
                    if (_state.CommandHistory.Count > 0)
                    {
                        if (_state.HistoryIndex == -1)
                        {
                            _state.HistoryIndex = _state.CommandHistory.Count - 1;
                        }
                        else if (_state.HistoryIndex > 0)
                        {
                            _state.HistoryIndex--;
                        }

                        ClearCurrentInput(promptLeft, promptTop, input.Length);
                        input.Clear();
                        input.Append(_state.CommandHistory[_state.HistoryIndex]);
                        cursorPosition = input.Length;
                        Console.Write(input.ToString());
                    }
                    break;

                case ConsoleKey.DownArrow:
                    if (_state.HistoryIndex != -1)
                    {
                        if (_state.HistoryIndex < _state.CommandHistory.Count - 1)
                        {
                            _state.HistoryIndex++;
                            ClearCurrentInput(promptLeft, promptTop, input.Length);
                            input.Clear();
                            input.Append(_state.CommandHistory[_state.HistoryIndex]);
                            cursorPosition = input.Length;
                            Console.Write(input.ToString());
                        }
                        else
                        {
                            _state.HistoryIndex = -1;
                            ClearCurrentInput(promptLeft, promptTop, input.Length);
                            input.Clear();
                            cursorPosition = 0;
                        }
                    }
                    break;

                case ConsoleKey.LeftArrow:
                    if (cursorPosition > 0)
                    {
                        cursorPosition--;
                        Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                    }
                    break;

                case ConsoleKey.RightArrow:
                    if (cursorPosition < input.Length)
                    {
                        cursorPosition++;
                        Console.SetCursorPosition(Console.CursorLeft + 1, Console.CursorTop);
                    }
                    break;

                case ConsoleKey.Backspace:
                    if (cursorPosition > 0)
                    {
                        input.Remove(cursorPosition - 1, 1);
                        cursorPosition--;
                        RedrawInput(promptLeft, promptTop, input.ToString(), cursorPosition);
                    }
                    break;

                case ConsoleKey.Delete:
                    if (cursorPosition < input.Length)
                    {
                        input.Remove(cursorPosition, 1);
                        RedrawInput(promptLeft, promptTop, input.ToString(), cursorPosition);
                    }
                    break;

                default:
                    if (!char.IsControl(key.KeyChar))
                    {
                        input.Insert(cursorPosition, key.KeyChar);
                        cursorPosition++;
                        RedrawInput(promptLeft, promptTop, input.ToString(), cursorPosition);
                    }
                    break;
            }
        }
    }

    private void ClearCurrentInput(int promptLeft, int promptTop, int length)
    {
        try
        {
            Console.SetCursorPosition(promptLeft, promptTop);
            int bufferWidth = Console.BufferWidth;
            int maxClear = Math.Min(length, bufferWidth - promptLeft - 1);
            
            if (maxClear > 0)
            {
                Console.Write(new string(' ', maxClear));
            }
            
            Console.SetCursorPosition(promptLeft, promptTop);
        }
        catch (ArgumentOutOfRangeException)
        {
            Console.SetCursorPosition(0, Console.CursorTop);
        }
    }

    private void RedrawInput(int promptLeft, int promptTop, string text, int cursorPos)
    {
        try
        {
            int bufferWidth = Console.BufferWidth;
            int availableWidth = bufferWidth - promptLeft - 1;
            
            string displayText = text;
            if (text.Length > availableWidth)
            {
                if (cursorPos > availableWidth - 10)
                {
                    displayText = "..." + text.Substring(text.Length - availableWidth + 3);
                    cursorPos = Math.Min(cursorPos, availableWidth - 1);
                }
                else
                {
                    displayText = text.Substring(0, availableWidth - 3) + "...";
                }
            }
            
            Console.SetCursorPosition(promptLeft, promptTop);
            Console.Write(displayText);
            Console.Write(' ');
            
            int targetPos = promptLeft + cursorPos;
            if (targetPos >= bufferWidth)
            {
                targetPos = bufferWidth - 1;
            }
            
            Console.SetCursorPosition(targetPos, promptTop);
        }
        catch (ArgumentOutOfRangeException)
        {
            Console.Write(text);
        }
    }
}
