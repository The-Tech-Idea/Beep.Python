using Beep.Python.RuntimeEngine;
using Beep.Python.RuntimeEngine.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Beep.Python.RuntimeHost.Services;

/// <summary>
/// Service that manages backend client connections (HTTP, Pipe, RPC) and provides access to IPythonHostBackend
/// </summary>
public class BackendClientService : IDisposable
{
    private readonly ILogger<BackendClientService> _logger;
    private IPythonHostBackend? _currentBackend;
    private PythonServerLauncher? _serverLauncher;
    private PythonBackendType? _currentBackendType;
    private string? _currentEndpoint;

    public BackendClientService(ILogger<BackendClientService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get the current backend client, or null if no backend is connected
    /// </summary>
    public IPythonHostBackend? CurrentBackend => _currentBackend;

    /// <summary>
    /// Get the current backend type
    /// </summary>
    public PythonBackendType? CurrentBackendType => _currentBackendType;

    /// <summary>
    /// Get the current endpoint
    /// </summary>
    public string? CurrentEndpoint => _currentEndpoint;

    /// <summary>
    /// Check if a backend is connected and initialized
    /// </summary>
    public bool IsConnected => _currentBackend != null && _currentBackend.IsInitialized;

    /// <summary>
    /// Start a backend server and connect to it
    /// </summary>
    public async Task<bool> StartBackendAsync(
        PythonBackendType backendType,
        string venvPath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Stop existing backend if any
            await StopBackendAsync();

            // Start server
            _serverLauncher = new PythonServerLauncher(venvPath, backendType, _logger);
            var started = await _serverLauncher.StartAsync(cancellationToken);
            
            if (!started)
            {
                _logger.LogError("Failed to start {BackendType} server", backendType);
                return false;
            }

            _currentEndpoint = _serverLauncher.GetEndpoint();
            _currentBackendType = backendType;

            // Create backend client connected to the server
            _currentBackend = backendType switch
            {
                PythonBackendType.Http => PythonBackendFactory.CreateHttpBackend(_currentEndpoint, _logger),
                PythonBackendType.Pipe => PythonBackendFactory.CreatePipeBackend(_serverLauncher.PipeName, _logger),
                PythonBackendType.Rpc => PythonBackendFactory.CreateRpcBackend(_currentEndpoint, _logger),
                _ => throw new NotSupportedException($"Backend type {backendType} not supported for client connection")
            };

            // Initialize the backend client
            var initialized = await _currentBackend.InitializeAsync(cancellationToken);
            if (!initialized)
            {
                _logger.LogError("Failed to initialize {BackendType} backend client", backendType);
                await StopBackendAsync();
                return false;
            }

            _logger.LogInformation("Connected to {BackendType} backend at {Endpoint}", backendType, _currentEndpoint);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start backend");
            await StopBackendAsync();
            return false;
        }
    }

    /// <summary>
    /// Stop the current backend server and disconnect
    /// </summary>
    public async Task StopBackendAsync()
    {
        if (_currentBackend != null)
        {
            try
            {
                _currentBackend.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error disposing backend client");
            }
            _currentBackend = null;
        }

        if (_serverLauncher != null)
        {
            _serverLauncher.Stop();
            _serverLauncher.Dispose();
            _serverLauncher = null;
        }

        _currentBackendType = null;
        _currentEndpoint = null;
    }

    /// <summary>
    /// Connect to an existing backend server (without starting it)
    /// </summary>
    public async Task<bool> ConnectToBackendAsync(
        PythonBackendType backendType,
        string endpoint,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await StopBackendAsync();

            _currentBackendType = backendType;
            _currentEndpoint = endpoint;

            // Create backend client
            _currentBackend = backendType switch
            {
                PythonBackendType.Http => PythonBackendFactory.CreateHttpBackend(endpoint, _logger),
                PythonBackendType.Pipe => PythonBackendFactory.CreatePipeBackend(endpoint, _logger),
                PythonBackendType.Rpc => PythonBackendFactory.CreateRpcBackend(endpoint, _logger),
                _ => throw new NotSupportedException($"Backend type {backendType} not supported")
            };

            var initialized = await _currentBackend.InitializeAsync(cancellationToken);
            if (!initialized)
            {
                _logger.LogError("Failed to connect to {BackendType} backend at {Endpoint}", backendType, endpoint);
                await StopBackendAsync();
                return false;
            }

            _logger.LogInformation("Connected to existing {BackendType} backend at {Endpoint}", backendType, endpoint);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to backend");
            await StopBackendAsync();
            return false;
        }
    }

    public void Dispose()
    {
        StopBackendAsync().GetAwaiter().GetResult();
    }
}
