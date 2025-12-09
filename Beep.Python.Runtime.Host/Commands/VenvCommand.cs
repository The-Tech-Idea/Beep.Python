using Beep.Python.RuntimeEngine.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace Beep.Python.RuntimeHost.Commands;

/// <summary>
/// Command to manage virtual environments using Infrastructure VenvManager
/// </summary>
public class VenvCommand : ICommand
{
    private readonly IPythonRuntimeManager _runtimeManager;
    private readonly VirtualEnvManager _virtualEnvManager;
    private readonly IVenvManager _venvManager;
    private readonly ILogger<VenvCommand> _logger;

    public VenvCommand(
        IPythonRuntimeManager runtimeManager,
        VirtualEnvManager virtualEnvManager,
        IVenvManager venvManager,
        ILogger<VenvCommand> logger)
    {
        _runtimeManager = runtimeManager ?? throw new ArgumentNullException(nameof(runtimeManager));
        _virtualEnvManager = virtualEnvManager ?? throw new ArgumentNullException(nameof(virtualEnvManager));
        _venvManager = venvManager ?? throw new ArgumentNullException(nameof(venvManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string Name => "venv";
    public string[] Aliases => new[] { "virtualenv", "env" };
    public string Description => "Manage virtual environments (create, list, delete, admin)";

    public async Task<bool> ExecuteAsync(IServiceProvider services, string[] args)
    {
        if (args.Length == 0)
        {
            return await ListEnvironmentsAsync();
        }

        var action = args[0].ToLowerInvariant();
        
        return action switch
        {
            "create" => await CreateEnvironmentAsync(args.Skip(1).ToArray()),
            "list" => await ListEnvironmentsAsync(),
            "delete" => await DeleteEnvironmentAsync(args.Skip(1).ToArray()),
            "admin" => await SetupAdminEnvironmentAsync(),
            "status" => await ShowEnvironmentStatusAsync(args.Skip(1).ToArray()),
            _ => await ShowHelpAsync()
        };
    }

    private async Task<bool> ListEnvironmentsAsync()
    {
        var rule = new Rule("[bold cyan]Virtual Environments[/]");
        AnsiConsole.Write(rule);
        AnsiConsole.MarkupLine("\n[dim]Listing virtual environments using Infrastructure...[/]\n");

        try
        {
            // Ensure runtime is initialized
            if (!_runtimeManager.GetAvailableRuntimes().Any())
            {
                await _runtimeManager.Initialize();
            }

            var table = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(Color.Grey);

            table.AddColumn("[bold cyan]Name[/]");
            table.AddColumn("[bold cyan]Path[/]");
            table.AddColumn("[bold cyan]Status[/]");

            // List known environments
            var environments = new[]
            {
                ("runtime-host", "Server runtime environment"),
                ("admin", "Administrative environment")
            };

            foreach (var (envName, description) in environments)
            {
                var envPath = _venvManager.GetRegisteredEnvironmentPath(envName);
                
                if (envPath != null && Directory.Exists(envPath))
                {
                    var pythonExe = OperatingSystem.IsWindows()
                        ? Path.Combine(envPath, "Scripts", "python.exe")
                        : Path.Combine(envPath, "bin", "python");
                    
                    var status = File.Exists(pythonExe) ? "[green]✓ Ready[/]" : "[red]✗ Invalid[/]";
                    table.AddRow(
                        $"[yellow]{envName}[/]",
                        $"[dim]{envPath}[/]",
                        status);
                }
                else
                {
                    table.AddRow(
                        $"[yellow]{envName}[/]",
                        "[dim]Not created[/]",
                        "[yellow]⚠ Not Created[/]");
                }
            }

            AnsiConsole.Write(table);
            AnsiConsole.WriteLine();
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]✗ Error listing environments: {Markup.Escape(ex.Message)}[/]");
            _logger.LogError(ex, "Failed to list virtual environments");
        }

        return false;
    }

    private async Task<bool> CreateEnvironmentAsync(string[] args)
    {
        if (args.Length == 0)
        {
            AnsiConsole.MarkupLine("[red]ERROR: Environment name required[/]");
            AnsiConsole.MarkupLine("[dim]Usage: venv create <name> [modelId][/]");
            return false;
        }

        var envName = args[0];
        var modelId = args.Length > 1 ? args[1] : null;

        var rule = new Rule($"[bold green]Create Virtual Environment: {envName}[/]");
        AnsiConsole.Write(rule);
        AnsiConsole.WriteLine();

        return await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync($"[yellow]Creating virtual environment '{envName}' using Infrastructure...[/]", async ctx =>
            {
                try
                {
                    // Ensure runtime is initialized
                    if (!_runtimeManager.GetAvailableRuntimes().Any())
                    {
                        ctx.Status("Initializing runtime manager...");
                        await _runtimeManager.Initialize();
                    }

                    // Create environment using Infrastructure
                    ctx.Status($"Creating virtual environment '{envName}'...");
                    var env = await _virtualEnvManager.CreateProviderEnvironmentAsync(envName, modelId, CancellationToken.None);

                    if (env != null && !string.IsNullOrEmpty(env.Path))
                    {
                        AnsiConsole.MarkupLine("[green]✓ Virtual environment created[/]");
                        AnsiConsole.MarkupLine($"  [dim]Path: {env.Path}[/]");
                        AnsiConsole.WriteLine();
                        return false;
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("[red]✗ Failed to create virtual environment[/]");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]✗ Error: {Markup.Escape(ex.Message)}[/]");
                    _logger.LogError(ex, "Failed to create virtual environment");
                    return false;
                }
            });
    }

    private async Task<bool> DeleteEnvironmentAsync(string[] args)
    {
        if (args.Length == 0)
        {
            AnsiConsole.MarkupLine("[red]ERROR: Environment name required[/]");
            AnsiConsole.MarkupLine("[dim]Usage: venv delete <name>[/]");
            return false;
        }

        var envName = args[0];
        var envPath = _venvManager.GetRegisteredEnvironmentPath(envName);

        if (envPath == null || !Directory.Exists(envPath))
        {
            AnsiConsole.MarkupLine($"[yellow]⚠ Environment '{envName}' not found[/]");
            return false;
        }

        if (!AnsiConsole.Confirm($"[red]Are you sure you want to delete environment '{envName}' at {envPath}?[/]", false))
        {
            return false;
        }

        var rule = new Rule($"[bold red]Delete Virtual Environment: {envName}[/]");
        AnsiConsole.Write(rule);
        AnsiConsole.WriteLine();

        return await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync($"[yellow]Deleting virtual environment '{envName}'...[/]", async ctx =>
            {
                try
                {
                    var deleted = await _venvManager.DeleteVirtualEnvironment(envPath, CancellationToken.None);
                    
                    if (deleted)
                    {
                        AnsiConsole.MarkupLine("[green]✓ Virtual environment deleted[/]");
                        return false;
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("[red]✗ Failed to delete virtual environment[/]");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]✗ Error: {Markup.Escape(ex.Message)}[/]");
                    _logger.LogError(ex, "Failed to delete virtual environment");
                    return false;
                }
            });
    }

    private async Task<bool> SetupAdminEnvironmentAsync()
    {
        var rule = new Rule("[bold green]Setup Admin Virtual Environment[/]");
        AnsiConsole.Write(rule);
        AnsiConsole.MarkupLine("\n[dim]Creating admin virtual environment for management operations...[/]\n");

        return await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("[yellow]Setting up admin environment using Infrastructure...[/]", async ctx =>
            {
                try
                {
                    // Ensure runtime is initialized
                    if (!_runtimeManager.GetAvailableRuntimes().Any())
                    {
                        ctx.Status("Initializing runtime manager...");
                        await _runtimeManager.Initialize();
                    }

                    // Create admin environment using Infrastructure
                    ctx.Status("Creating admin virtual environment...");
                    var env = await _virtualEnvManager.CreateProviderEnvironmentAsync("admin", null, CancellationToken.None);

                    if (env == null || string.IsNullOrEmpty(env.Path))
                    {
                        AnsiConsole.MarkupLine("[red]✗ Failed to create admin environment[/]");
                        return false;
                    }

                    AnsiConsole.MarkupLine("[green]✓ Admin environment created[/]");
                    AnsiConsole.MarkupLine($"  [dim]Path: {env.Path}[/]");

                    // Install admin packages
                    ctx.Status("Installing admin packages...");
                    var pythonExe = OperatingSystem.IsWindows()
                        ? Path.Combine(env.Path, "Scripts", "python.exe")
                        : Path.Combine(env.Path, "bin", "python");

                    // Install common admin/management packages
                    var adminPackages = new Dictionary<string, string>
                    {
                        { "pip", "" },
                        { "setuptools", "" },
                        { "wheel", "" }
                    };

                    await _venvManager.InstallProviderPackagesInVenv("admin", pythonExe, adminPackages, CancellationToken.None);

                    AnsiConsole.MarkupLine("[green]✓ Admin packages installed[/]");
                    AnsiConsole.WriteLine();

                    AnsiConsole.Write(new Panel("[green]Admin environment ready![/]")
                    {
                        Border = BoxBorder.Rounded,
                        BorderStyle = new Style(Color.Green)
                    });

                    return false;
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]✗ Error: {Markup.Escape(ex.Message)}[/]");
                    _logger.LogError(ex, "Failed to setup admin environment");
                    return false;
                }
            });
    }

    private async Task<bool> ShowEnvironmentStatusAsync(string[] args)
    {
        if (args.Length == 0)
        {
            AnsiConsole.MarkupLine("[red]ERROR: Environment name required[/]");
            AnsiConsole.MarkupLine("[dim]Usage: venv status <name>[/]");
            return false;
        }

        var envName = args[0];
        var envPath = _venvManager.GetRegisteredEnvironmentPath(envName);

        if (envPath == null || !Directory.Exists(envPath))
        {
            AnsiConsole.MarkupLine($"[yellow]⚠ Environment '{envName}' not found[/]");
            return false;
        }

        var rule = new Rule($"[bold cyan]Environment Status: {envName}[/]");
        AnsiConsole.Write(rule);
        AnsiConsole.WriteLine();

        var pythonExe = OperatingSystem.IsWindows()
            ? Path.Combine(envPath, "Scripts", "python.exe")
            : Path.Combine(envPath, "bin", "python");

        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey);

        table.AddColumn("[bold cyan]Property[/]");
        table.AddColumn("[bold cyan]Value[/]");

        table.AddRow("[yellow]Name[/]", envName);
        table.AddRow("[yellow]Path[/]", $"[dim]{envPath}[/]");
        table.AddRow("[yellow]Python Executable[/]", File.Exists(pythonExe) ? "[green]✓ Found[/]" : "[red]✗ Not Found[/]");

        if (File.Exists(pythonExe))
        {
            // Get Python version
            try
            {
                var processInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = pythonExe,
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = System.Diagnostics.Process.Start(processInfo);
                if (process != null)
                {
                    var version = await process.StandardOutput.ReadToEndAsync();
                    await process.WaitForExitAsync();
                    table.AddRow("[yellow]Python Version[/]", $"[dim]{version.Trim()}[/]");
                }
            }
            catch { }
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();

        return false;
    }

    private Task<bool> ShowHelpAsync()
    {
        AnsiConsole.MarkupLine("[bold cyan]Virtual Environment Management[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[yellow]Usage:[/]");
        AnsiConsole.MarkupLine("  [cyan]venv list[/]                    - List all virtual environments");
        AnsiConsole.MarkupLine("  [cyan]venv create <name> [modelId][/] - Create a new virtual environment");
        AnsiConsole.MarkupLine("  [cyan]venv delete <name>[/]           - Delete a virtual environment");
        AnsiConsole.MarkupLine("  [cyan]venv admin[/]                   - Setup admin virtual environment");
        AnsiConsole.MarkupLine("  [cyan]venv status <name>[/]           - Show environment status");
        AnsiConsole.WriteLine();
        return Task.FromResult(false);
    }
}
