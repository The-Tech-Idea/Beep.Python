using Beep.Python.RuntimeEngine.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace Beep.Python.RuntimeHost.Commands;

public class ConfigCommand : ICommand
{
    private readonly IPythonRuntimeManager _runtimeManager;
    private readonly VirtualEnvManager _virtualEnvManager;
    private readonly IVenvManager _venvManager;
    private readonly ShellState _state;
    private readonly ILogger<ConfigCommand> _logger;

    public ConfigCommand(
        IPythonRuntimeManager runtimeManager,
        VirtualEnvManager virtualEnvManager,
        IVenvManager venvManager,
        ShellState state,
        ILogger<ConfigCommand> logger)
    {
        _runtimeManager = runtimeManager ?? throw new ArgumentNullException(nameof(runtimeManager));
        _virtualEnvManager = virtualEnvManager ?? throw new ArgumentNullException(nameof(virtualEnvManager));
        _venvManager = venvManager ?? throw new ArgumentNullException(nameof(venvManager));
        _state = state ?? throw new ArgumentNullException(nameof(state));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string Name => "config";
    public string[] Aliases => new[] { "check", "validate" };
    public string Description => "Check and validate configuration (Python runtime, packages)";

    public async Task<bool> ExecuteAsync(IServiceProvider services, string[] args)
    {
        var rule = new Rule("[bold cyan]Configuration Check[/]");
        AnsiConsole.Write(rule);
        AnsiConsole.MarkupLine("\n[dim]Validating runtime environment and packages using Infrastructure...[/]\n");

        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey);

        table.AddColumn("[bold cyan]Component[/]");
        table.AddColumn("[bold cyan]Status[/]");
        table.AddColumn("[bold cyan]Details[/]");

        bool allValid = true;
        bool canStartServers = true;

        // Check Python Runtime using Infrastructure
        try
        {
            if (!_runtimeManager.GetAvailableRuntimes().Any())
            {
                await _runtimeManager.Initialize();
            }

            var defaultRuntime = _runtimeManager.GetDefaultRuntime();

            if (defaultRuntime != null && defaultRuntime.Status == PythonRuntimeStatus.Ready)
            {
                table.AddRow(
                    "[yellow]Python Runtime[/]",
                    "[green]✓ Ready[/]",
                    $"[dim]{defaultRuntime.Name} - {defaultRuntime.Version} at {defaultRuntime.Path}[/]");
                
                _state.IsInitialized = true;
            }
            else
            {
                table.AddRow(
                    "[yellow]Python Runtime[/]",
                    "[red]✗ Not Found[/]",
                    "[dim]Run 'init' to download and setup Python[/]");
                allValid = false;
                canStartServers = false;
            }
        }
        catch (Exception ex)
        {
            table.AddRow(
                "[yellow]Python Runtime[/]",
                "[red]✗ Error[/]",
                $"[red]{Markup.Escape(ex.Message)}[/]");
            allValid = false;
            canStartServers = false;
        }

        // Check Virtual Environment for servers using Infrastructure
        try
        {
            var env = await _virtualEnvManager.EnsureProviderEnvironmentAsync("runtime-host", null, CancellationToken.None);
            
            if (env != null && !string.IsNullOrEmpty(env.Path) && Directory.Exists(env.Path))
            {
                var pythonExe = OperatingSystem.IsWindows()
                    ? Path.Combine(env.Path, "Scripts", "python.exe")
                    : Path.Combine(env.Path, "bin", "python");
                
                if (File.Exists(pythonExe))
                {
                    table.AddRow(
                        "[yellow]Virtual Environment[/]",
                        "[green]✓ Ready[/]",
                        $"[dim]{env.Path}[/]");
                }
                else
                {
                    table.AddRow(
                        "[yellow]Virtual Environment[/]",
                        "[red]✗ Invalid[/]",
                        "[dim]Python executable not found[/]");
                    allValid = false;
                    canStartServers = false;
                }
            }
            else
            {
                table.AddRow(
                    "[yellow]Virtual Environment[/]",
                    "[yellow]⚠ Not Created[/]",
                    "[dim]Will be created when starting server[/]");
            }
        }
        catch (Exception ex)
        {
            table.AddRow(
                "[yellow]Virtual Environment[/]",
                "[yellow]⚠ Error[/]",
                $"[dim]{Markup.Escape(ex.Message)}[/]");
        }

        // Check Required Packages using Infrastructure
        try
        {
            var env = await _virtualEnvManager.EnsureProviderEnvironmentAsync("runtime-host", null, CancellationToken.None);
            
            if (env != null && !string.IsNullOrEmpty(env.Path) && Directory.Exists(env.Path))
            {
                var pythonExe = OperatingSystem.IsWindows()
                    ? Path.Combine(env.Path, "Scripts", "python.exe")
                    : Path.Combine(env.Path, "bin", "python");
                
                if (File.Exists(pythonExe))
                {
                    var requiredPackages = new[]
                    {
                        "fastapi",
                        "uvicorn",
                        "pydantic",
                        "grpcio",
                        "numpy"
                    };

                    var missingPackages = new List<string>();
                    
                    var verified = await _venvManager.VerifyPackagesInstalled(
                        pythonExe,
                        requiredPackages,
                        "runtime-host",
                        CancellationToken.None);

                    if (verified)
                    {
                        table.AddRow(
                            "[yellow]Required Packages[/]",
                            "[green]✓ Installed[/]",
                            $"[dim]{requiredPackages.Length} packages verified[/]");
                    }
                    else
                    {
                        // Check individual packages
                        foreach (var package in requiredPackages)
                        {
                            var processInfo = new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = pythonExe,
                                Arguments = $"-c \"import {package}\"",
                                RedirectStandardOutput = true,
                                RedirectStandardError = true,
                                UseShellExecute = false,
                                CreateNoWindow = true
                            };

                            using var process = System.Diagnostics.Process.Start(processInfo);
                            if (process != null)
                            {
                                await process.WaitForExitAsync();
                                if (process.ExitCode != 0)
                                {
                                    missingPackages.Add(package);
                                }
                            }
                        }

                        if (missingPackages.Count == 0)
                        {
                            table.AddRow(
                                "[yellow]Required Packages[/]",
                                "[green]✓ Installed[/]",
                                $"[dim]{requiredPackages.Length} packages verified[/]");
                        }
                        else
                        {
                            table.AddRow(
                                "[yellow]Required Packages[/]",
                                "[red]✗ Missing[/]",
                                $"[red]Missing: {string.Join(", ", missingPackages)}[/]");
                            allValid = false;
                            canStartServers = false;
                        }
                    }
                }
                else
                {
                    table.AddRow(
                        "[yellow]Required Packages[/]",
                        "[yellow]⚠ Skipped[/]",
                        "[dim]Virtual environment not ready[/]");
                }
            }
            else
            {
                table.AddRow(
                    "[yellow]Required Packages[/]",
                    "[yellow]⚠ Not Checked[/]",
                    "[dim]Virtual environment not created yet[/]");
            }
        }
        catch (Exception ex)
        {
            table.AddRow(
                "[yellow]Required Packages[/]",
                "[yellow]⚠ Error[/]",
                $"[dim]{Markup.Escape(ex.Message)}[/]");
        }

        // Check Server Scripts
        var scriptsDir = Path.Combine(AppContext.BaseDirectory, "python-servers");
        var httpScript = Path.Combine(scriptsDir, "http_server.py");
        var pipeScript = Path.Combine(scriptsDir, "pipe_server.py");
        var rpcScript = Path.Combine(scriptsDir, "rpc_server.py");

        var scriptsExist = File.Exists(httpScript) && File.Exists(pipeScript) && File.Exists(rpcScript);
        
        if (scriptsExist)
        {
            table.AddRow(
                "[yellow]Server Scripts[/]",
                "[green]✓ Ready[/]",
                "[dim]All server scripts available[/]");
        }
        else
        {
            table.AddRow(
                "[yellow]Server Scripts[/]",
                "[yellow]⚠ Missing[/]",
                "[dim]Scripts will be extracted on first server start[/]");
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();

        if (allValid && canStartServers)
        {
            AnsiConsole.Write(new Panel("[green]✓ Configuration is valid - servers can be started[/]")
            {
                Border = BoxBorder.Rounded,
                BorderStyle = new Style(Color.Green)
            });
            _state.IsInitialized = true;
        }
        else
        {
            AnsiConsole.Write(new Panel("[yellow]⚠ Configuration issues found - fix before starting servers[/]")
            {
                Border = BoxBorder.Rounded,
                BorderStyle = new Style(Color.Yellow)
            });
            
            if (!_state.IsInitialized)
            {
                AnsiConsole.MarkupLine("\n[dim]Use [cyan]init[/] to setup Python runtime[/]");
            }
            
            if (!canStartServers)
            {
                AnsiConsole.MarkupLine("[dim]Cannot start servers until configuration is valid[/]");
            }
        }

        return false;
    }
}
