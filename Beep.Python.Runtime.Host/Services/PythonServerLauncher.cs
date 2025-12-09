using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Environment = System.Environment;
using Beep.Python.RuntimeEngine;

namespace Beep.Python.RuntimeHost.Services;

/// <summary>
/// Manages launching Python server processes for remote backends (HTTP, Pipe, RPC).
/// Each server runs in a specific virtual environment.
/// </summary>
public partial class PythonServerLauncher : IDisposable
{
    private readonly ILogger _logger;
    private readonly string _venvPath;
    private readonly PythonBackendType _backendType;
    private Process? _serverProcess;
    private bool _isRunning;

    /// <summary>
    /// The port for HTTP/RPC backends
    /// </summary>
    public int Port { get; private set; }

    /// <summary>
    /// The pipe name for Pipe backend
    /// </summary>
    public string PipeName { get; private set; }

    /// <summary>
    /// The virtual environment path
    /// </summary>
    public string VenvPath => _venvPath;

    /// <summary>
    /// Whether the server is running
    /// </summary>
    public bool IsRunning => _isRunning && _serverProcess != null && !_serverProcess.HasExited;

    public PythonServerLauncher(string venvPath, PythonBackendType backendType, ILogger logger)
    {
        _venvPath = venvPath;
        _backendType = backendType;
        _logger = logger;
        Port = GetAvailablePort();
        PipeName = $"beep-python-{Guid.NewGuid():N}";
    }

    /// <summary>
    /// Starts the Python server in the configured virtual environment.
    /// </summary>
    public async Task<bool> StartAsync(CancellationToken cancellationToken = default)
    {
        if (_isRunning) return true;

        var pythonExe = GetPythonExecutable();
        if (!File.Exists(pythonExe))
        {
            _logger.LogError("Python executable not found: {Path}", pythonExe);
            return false;
        }

        // Get the server script path
        var serverScript = GetServerScript();
        _logger.LogDebug("Starting Python server: {Exe} {Script} --port {Port}", pythonExe, serverScript, Port);

        var startInfo = new ProcessStartInfo
        {
            FileName = pythonExe,
            Arguments = GetServerArguments(serverScript),
            WorkingDirectory = Path.GetDirectoryName(serverScript) ?? _venvPath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        // Set virtual environment
        startInfo.Environment["VIRTUAL_ENV"] = _venvPath;
        var scriptsPath = Path.Combine(_venvPath, OperatingSystem.IsWindows() ? "Scripts" : "bin");
        startInfo.Environment["PATH"] = $"{scriptsPath}{Path.PathSeparator}{Environment.GetEnvironmentVariable("PATH")}";
        // Ensure Python doesn't buffer output
        startInfo.Environment["PYTHONUNBUFFERED"] = "1";

        try
        {
            _serverProcess = new Process { StartInfo = startInfo };
            
            string? lastError = null;
            string? lastOutput = null;
            
            _serverProcess.OutputDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    lastOutput = e.Data;
                    _logger.LogDebug("[Python] {Output}", e.Data);
                }
            };
            _serverProcess.ErrorDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    lastError = e.Data;
                    // Python's http.server logs requests to stderr, so check for actual errors
                    if (e.Data.Contains("Error") || e.Data.Contains("Exception") || e.Data.Contains("Traceback"))
                    {
                        _logger.LogWarning("[Python Error] {Error}", e.Data);
                    }
                    else
                    {
                        _logger.LogDebug("[Python] {Output}", e.Data);
                    }
                }
            };

            _serverProcess.Start();
            _serverProcess.BeginOutputReadLine();
            _serverProcess.BeginErrorReadLine();

            _logger.LogDebug("Python process started, PID: {ProcessId}", _serverProcess.Id);

            // Give the process a moment to start or fail
            await Task.Delay(1000, cancellationToken);
            
            if (_serverProcess.HasExited)
            {
                _logger.LogError("Python server process exited immediately with code: {ExitCode}", _serverProcess.ExitCode);
                return false;
            }

            // Wait for server to be ready
            _isRunning = await WaitForServerReadyAsync(cancellationToken);

            if (_isRunning)
            {
                _logger.LogInformation("Python {BackendType} server started on {Endpoint} using venv: {VenvPath}",
                    _backendType, GetEndpoint(), _venvPath);
            }
            else
            {
                _logger.LogError("Python server failed to start within timeout");
                Stop();
            }

            return _isRunning;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start Python server");
            return false;
        }
    }

    /// <summary>
    /// Stops the Python server.
    /// </summary>
    public void Stop()
    {
        if (_serverProcess != null)
        {
            try
            {
                if (!_serverProcess.HasExited)
                {
                    _serverProcess.Kill(entireProcessTree: true);
                    _serverProcess.WaitForExit(5000);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error stopping Python server");
            }
            finally
            {
                _serverProcess.Dispose();
                _serverProcess = null;
                _isRunning = false;
            }
        }
    }

    /// <summary>
    /// Gets the connection endpoint for the running server.
    /// </summary>
    public string GetEndpoint()
    {
        return _backendType switch
        {
            PythonBackendType.Http => $"http://localhost:{Port}",
            PythonBackendType.Rpc => $"http://localhost:{Port}",
            PythonBackendType.Pipe => PipeName,
            _ => throw new InvalidOperationException($"No endpoint for backend type: {_backendType}")
        };
    }

    private string GetPythonExecutable()
    {
        if (OperatingSystem.IsWindows())
            return Path.Combine(_venvPath, "Scripts", "python.exe");
        else
            return Path.Combine(_venvPath, "bin", "python");
    }

    private string GetServerScript()
    {
        var scriptDir = Path.Combine(AppContext.BaseDirectory, "python-servers");
        Directory.CreateDirectory(scriptDir);

        var scriptName = _backendType switch
        {
            PythonBackendType.Http => "http_server.py",
            PythonBackendType.Pipe => "pipe_server.py",
            PythonBackendType.Rpc => "rpc_server.py",
            _ => throw new InvalidOperationException()
        };

        var scriptPath = Path.Combine(scriptDir, scriptName);

        // Create the server script if it doesn't exist
        if (!File.Exists(scriptPath))
        {
            var scriptContent = _backendType switch
            {
                PythonBackendType.Http => GetHttpServerScript(),
                PythonBackendType.Pipe => GetPipeServerScript(),
                PythonBackendType.Rpc => GetRpcServerScript(),
                _ => throw new InvalidOperationException()
            };
            File.WriteAllText(scriptPath, scriptContent);
        }

        return scriptPath;
    }

    private string GetServerArguments(string scriptPath)
    {
        return _backendType switch
        {
            PythonBackendType.Http => $"\"{scriptPath}\" --port {Port}",
            PythonBackendType.Rpc => $"\"{scriptPath}\" --port {Port}",
            PythonBackendType.Pipe => $"\"{scriptPath}\" --pipe-name {PipeName}",
            _ => throw new InvalidOperationException()
        };
    }

    private async Task<bool> WaitForServerReadyAsync(CancellationToken cancellationToken)
    {
        var timeout = TimeSpan.FromSeconds(30);
        var startTime = DateTime.UtcNow;

        // Create a single HttpClient for all attempts (more efficient)
        using var handler = new HttpClientHandler();
        using var client = new HttpClient(handler) 
        { 
            Timeout = TimeSpan.FromSeconds(5) 
        };

        while (DateTime.UtcNow - startTime < timeout)
        {
            if (cancellationToken.IsCancellationRequested)
                return false;

            if (_serverProcess == null || _serverProcess.HasExited)
            {
                _logger.LogError("Python server process exited unexpectedly");
                return false;
            }

            try
            {
                if (_backendType == PythonBackendType.Http || _backendType == PythonBackendType.Rpc)
                {
                    // Use 127.0.0.1 to avoid DNS resolution issues with localhost
                    var url = $"http://127.0.0.1:{Port}/health";
                    var response = await client.GetAsync(url, cancellationToken);
                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogDebug("Python server health check passed");
                        return true;
                    }
                }
                else if (_backendType == PythonBackendType.Pipe)
                {
                    // For pipe, try to connect briefly
                    var pipePath = $@"\\.\pipe\{PipeName}";
                    if (File.Exists(pipePath))
                        return true;
                }
            }
            catch (HttpRequestException)
            {
                // Server not ready yet, retry
            }
            catch (TaskCanceledException)
            {
                // Request timed out, retry
            }
            catch (Exception ex)
            {
                _logger.LogDebug("Health check error: {Error}", ex.Message);
            }

            await Task.Delay(500, cancellationToken);
        }

        _logger.LogWarning("Timeout waiting for Python server to be ready");
        return false;
    }

    private static int GetAvailablePort()
    {
        var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    public void Dispose()
    {
        Stop();
    }
}

