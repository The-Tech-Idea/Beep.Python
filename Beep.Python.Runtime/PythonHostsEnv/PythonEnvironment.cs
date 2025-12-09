using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Environment = System.Environment;

namespace Beep.Python.RuntimeEngine.Infrastructure;

/// <summary>
/// Manages Python environment setup and dependencies
/// </summary>
public class PythonEnvironment : IPythonEnvironment
{
    private readonly ILogger<PythonEnvironment> _logger;
    private string? _pythonVersion;
    private string? _pythonExecutablePath;
    private bool _isInitialized;

    public string? PythonVersion => _pythonVersion;
    public string? PythonExecutablePath => _pythonExecutablePath;
    public bool IsInitialized => _isInitialized;

    public PythonEnvironment(ILogger<PythonEnvironment> logger)
    {
        _logger = logger;
    }

    public async Task<bool> Setup(string? pythonPath = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Setting up Python environment...");

            // Find Python executable
            _pythonExecutablePath = await FindPythonExecutable(pythonPath);

            if (_pythonExecutablePath == null)
            {
                _logger.LogError("Python executable not found");
                return false;
            }

            // Get Python version
            _pythonVersion = await GetPythonVersion();

            _isInitialized = true;
            _logger.LogInformation("Python environment setup complete. Version: {Version}", _pythonVersion);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to setup Python environment");
            return false;
        }
    }

    public async Task<bool> InstallDependencies(
        string requirementsPath,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Installing dependencies from: {Path}", requirementsPath);
            progress?.Report("Installing Python dependencies...");

            if (!File.Exists(requirementsPath))
            {
                _logger.LogError("Requirements file not found: {Path}", requirementsPath);
                return false;
            }

            var processInfo = new ProcessStartInfo
            {
                FileName = _pythonExecutablePath ?? "python",
                Arguments = $"-m pip install -r \"{requirementsPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process != null)
            {
                // Read output and report progress
                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        progress?.Report(e.Data);
                        _logger.LogDebug(e.Data);
                    }
                };

                process.BeginOutputReadLine();
                await process.WaitForExitAsync(cancellationToken);

                if (process.ExitCode != 0)
                {
                    var error = await process.StandardError.ReadToEndAsync(cancellationToken);
                    _logger.LogError("Failed to install dependencies: {Error}", error);
                    return false;
                }
            }

            progress?.Report("Dependencies installed successfully");
            _logger.LogInformation("Dependencies installed successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install dependencies");
            return false;
        }
    }

    public async Task<bool> IsPackageInstalled(string packageName)
    {
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = _pythonExecutablePath ?? "python",
                Arguments = $"-m pip show {packageName}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process != null)
            {
                await process.WaitForExitAsync();
                return process.ExitCode == 0;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    public async Task<string?> GetPackageVersion(string packageName)
    {
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = _pythonExecutablePath ?? "python",
                Arguments = $"-m pip show {packageName}",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process != null)
            {
                var output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode == 0)
                {
                    // Parse version from output
                    var lines = output.Split('\n');
                    var versionLine = lines.FirstOrDefault(l => l.StartsWith("Version:"));
                    if (versionLine != null)
                    {
                        return versionLine.Split(':')[1].Trim();
                    }
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> VerifyEnvironment()
    {
        try
        {
            if (!_isInitialized)
                return false;

            // Verify Python is accessible
            var version = await GetPythonVersion();
            return !string.IsNullOrEmpty(version);
        }
        catch
        {
            return false;
        }
    }

    public async Task<Dictionary<string, string>> GetInstalledPackages()
    {
        var packages = new Dictionary<string, string>();

        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = _pythonExecutablePath ?? "python",
                Arguments = "-m pip list --format=json",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process != null)
            {
                var output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode == 0)
                {
                    // Parse JSON output (simplified)
                    var lines = output.Split('\n');
                    foreach (var line in lines)
                    {
                        if (line.Contains("\"name\"") && line.Contains("\"version\""))
                        {
                            // Basic JSON parsing
                            var parts = line.Split(',');
                            if (parts.Length >= 2)
                            {
                                var name = parts[0].Split(':')[1].Trim(' ', '"', '{');
                                var version = parts[1].Split(':')[1].Trim(' ', '"', '}');
                                packages[name] = version;
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get installed packages");
        }

        return packages;
    }

    public Task Cleanup()
    {
        _isInitialized = false;
        return Task.CompletedTask;
    }

    private async Task<string?> FindPythonExecutable(string? pythonPath)
    {
        var possiblePaths = new List<string>();

        if (!string.IsNullOrEmpty(pythonPath))
        {
            possiblePaths.Add(Path.Combine(pythonPath, "python.exe"));
            possiblePaths.Add(Path.Combine(pythonPath, "python"));
        }
        else
        {
            // If no path is provided, check the dedicated embedded location first
            var embeddedPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".beep-llm", "python", "python.exe");
            if (File.Exists(embeddedPath))
            {
                _logger.LogInformation("Found Python at: {Path}", embeddedPath);
                return embeddedPath;
            }
        }

        possiblePaths.AddRange(new[]
        {
            "python.exe",
            "python",
            @"C:\Python311\python.exe",
            @"C:\Python310\python.exe",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Python", "Python311", "python.exe")
        });

        foreach (var path in possiblePaths)
        {
            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = path,
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(processInfo);
                if (process != null)
                {
                    await process.WaitForExitAsync();
                    if (process.ExitCode == 0)
                    {
                        _logger.LogInformation("Found Python at: {Path}", path);
                        return path;
                    }
                }
            }
            catch
            {
                continue;
            }
        }

        return null;
    }

    private async Task<string?> GetPythonVersion()
    {
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = _pythonExecutablePath ?? "python",
                Arguments = "--version",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process != null)
            {
                var output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();
                return output.Trim();
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}
