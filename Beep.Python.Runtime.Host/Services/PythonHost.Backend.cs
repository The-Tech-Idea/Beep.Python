using Beep.Python.RuntimeEngine.Infrastructure;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Beep.Python.RuntimeEngine;

namespace Beep.Python.RuntimeHost.Services;

/// <summary>
/// Partial class containing the Python Execution Abstraction Layer.
/// Delegates all operations to the configured backend (Python.NET, HTTP, Pipe, RPC).
/// </summary>
public partial class PythonHost
{
    private IPythonHostBackend? _backend;
    private PythonBackendType _backendType = PythonBackendType.PythonNet;
    private PythonBackendConfig? _backendConfig;
    private PythonHostPythonNet? _pythonNetBackend;
    private PythonServerLauncher? _serverLauncher;
    
    /// <summary>
    /// Provider-specific backends for different virtual environments.
    /// Key is provider name, value is the backend + server launcher.
    /// </summary>
    private readonly Dictionary<string, (IPythonHostBackend Backend, PythonServerLauncher? Launcher)> _providerBackends = new();

    /// <summary>
    /// Gets the current backend type.
    /// </summary>
    public PythonBackendType BackendType => _backendType;

    /// <summary>
    /// Gets the current backend instance.
    /// </summary>
    public IPythonHostBackend? Backend => _backend;

    /// <summary>
    /// Sets the backend type. Must be called before Initialize().
    /// For remote backends (HTTP, Pipe, RPC), call ConfigureRemoteBackend() instead.
    /// </summary>
    public void SetBackendType(PythonBackendType backendType)
    {
        if (_isInitialized)
            throw new InvalidOperationException("Cannot change backend type after initialization");
        _backendType = backendType;
    }

    /// <summary>
    /// Configures a remote backend (HTTP, Pipe, or RPC).
    /// Must be called before Initialize().
    /// </summary>
    public void ConfigureRemoteBackend(PythonBackendConfig config)
    {
        if (_isInitialized)
            throw new InvalidOperationException("Cannot configure backend after initialization");

        _backendType = config.BackendType;
        _backendConfig = config;
    }

    /// <summary>
    /// Configures a remote backend with specific settings and virtual environment.
    /// </summary>
    public void ConfigureHttpBackend(string? venvPath = null, string baseUrl = "http://localhost:5678")
    {
        ConfigureRemoteBackend(new PythonBackendConfig
        {
            BackendType = PythonBackendType.Http,
            VirtualEnvPath = venvPath,
            HttpBaseUrl = baseUrl,
            AutoStartServer = venvPath != null
        });
    }

    /// <summary>
    /// Configures a pipe backend with specific settings and virtual environment.
    /// </summary>
    public void ConfigurePipeBackend(string? venvPath = null, string pipeName = "beep-python-pipe")
    {
        ConfigureRemoteBackend(new PythonBackendConfig
        {
            BackendType = PythonBackendType.Pipe,
            VirtualEnvPath = venvPath,
            PipeName = pipeName,
            AutoStartServer = venvPath != null
        });
    }

    /// <summary>
    /// Configures an RPC backend with specific settings and virtual environment.
    /// </summary>
    public void ConfigureRpcBackend(string? venvPath = null, string serverAddress = "http://localhost:50051")
    {
        ConfigureRemoteBackend(new PythonBackendConfig
        {
            BackendType = PythonBackendType.Rpc,
            VirtualEnvPath = venvPath,
            RpcAddress = serverAddress,
            AutoStartServer = venvPath != null
        });
    }

    /// <summary>
    /// Gets or creates a backend for a specific provider's virtual environment.
    /// This allows different providers to use different Python environments.
    /// </summary>
    public async Task<IPythonHostBackend?> GetProviderBackendAsync(
        string providerName, 
        string? modelId = null, 
        CancellationToken cancellationToken = default)
    {
        // For Python.NET, we use the main backend (GIL is shared)
        if (_backendType == PythonBackendType.PythonNet)
        {
            return _backend;
        }

        // Check if we already have a backend for this provider
        var key = string.IsNullOrEmpty(modelId) ? providerName : $"{providerName}:{modelId}";
        if (_providerBackends.TryGetValue(key, out var existing))
        {
            return existing.Backend;
        }

        // Get the provider's virtual environment
        var venvPath = await EnsureProviderEnvironment(providerName, modelId, cancellationToken);
        if (venvPath == null)
        {
            _logger.LogError("Failed to get virtual environment for provider: {Provider}", providerName);
            return null;
        }

        // Create and start a new backend for this provider's venv
        var (backend, launcher) = await CreateBackendForVenvAsync(venvPath, cancellationToken);
        if (backend != null)
        {
            _providerBackends[key] = (backend, launcher);
        }
        return backend;
    }

    /// <summary>
    /// Creates a backend for a specific virtual environment.
    /// For remote backends, this starts a Python server in that venv.
    /// </summary>
    private async Task<(IPythonHostBackend? Backend, PythonServerLauncher? Launcher)> CreateBackendForVenvAsync(
        string venvPath,
        CancellationToken cancellationToken)
    {
        if (_backendType == PythonBackendType.PythonNet)
        {
            // Python.NET uses the main backend
            return (_backend, null);
        }

        // Start a Python server in the venv
        var launcher = new PythonServerLauncher(venvPath, _backendType, _logger);
        var started = await launcher.StartAsync(cancellationToken);
        
        if (!started)
        {
            _logger.LogError("Failed to start Python server for venv: {VenvPath}", venvPath);
            launcher.Dispose();
            return (null, null);
        }

        // Create the backend connected to this server
        var endpoint = launcher.GetEndpoint();
        IPythonHostBackend backend = _backendType switch
        {
            PythonBackendType.Http => new PythonHostHttp(endpoint, _logger),
            PythonBackendType.Pipe => new PythonHostPipe(endpoint, _logger),
            PythonBackendType.Rpc => new PythonHostRpc(endpoint, _logger),
            _ => throw new InvalidOperationException($"Unknown backend type: {_backendType}")
        };

        var initialized = await backend.InitializeAsync(cancellationToken);
        if (!initialized)
        {
            _logger.LogError("Failed to initialize backend for venv: {VenvPath}", venvPath);
            backend.Dispose();
            launcher.Dispose();
            return (null, null);
        }

        _logger.LogInformation("Created {BackendType} backend for venv: {VenvPath} -> {Endpoint}", 
            _backendType, venvPath, endpoint);
        
        return (backend, launcher);
    }

    /// <summary>
    /// Initializes the backend after Python runtime is initialized.
    /// Called internally by Initialize().
    /// </summary>
    private async Task InitializeBackendAsync(CancellationToken cancellationToken)
    {
        if (_backendType == PythonBackendType.PythonNet)
        {
            // Create Python.NET backend
            _pythonNetBackend = new PythonHostPythonNet(_logger);
            _pythonNetBackend.SetInitialized(true);
            _backend = _pythonNetBackend;
        }
        else if (_backendConfig != null)
        {
            // For remote backends, check if we need to auto-start a server
            if (_backendConfig.AutoStartServer && !string.IsNullOrEmpty(_backendConfig.VirtualEnvPath))
            {
                var (backend, launcher) = await CreateBackendForVenvAsync(_backendConfig.VirtualEnvPath, cancellationToken);
                _backend = backend;
                _serverLauncher = launcher;
            }
            else
            {
                // Connect to existing server
                _backend = PythonBackendFactory.CreateBackend(_backendConfig, _logger);
                var success = await _backend.InitializeAsync(cancellationToken);
                if (!success)
                {
                    _logger.LogWarning("Failed to initialize {BackendType} backend, falling back to Python.NET", _backendType);
                    _backendType = PythonBackendType.PythonNet;
                    _pythonNetBackend = new PythonHostPythonNet(_logger);
                    _pythonNetBackend.SetInitialized(true);
                    _backend = _pythonNetBackend;
                }
            }
        }
        else
        {
            // Default to Python.NET if no config
            _pythonNetBackend = new PythonHostPythonNet(_logger);
            _pythonNetBackend.SetInitialized(true);
            _backend = _pythonNetBackend;
        }
    }

    /// <summary>
    /// Disposes all provider backends and their server launchers.
    /// </summary>
    private void DisposeBackends()
    {
        foreach (var (backend, launcher) in _providerBackends.Values)
        {
            backend.Dispose();
            launcher?.Dispose();
        }
        _providerBackends.Clear();
        
        _serverLauncher?.Dispose();
        _backend?.Dispose();
    }

    /// <inheritdoc />
    public bool IsGILHeld => _backend?.IsGILHeld ?? false;

    /// <inheritdoc />
    public IDisposable AcquireGIL()
    {
        EnsureBackend();
        return _backend!.AcquireGIL();
    }

    /// <inheritdoc />
    public async Task<IPythonModuleHandle?> ImportModuleAsync(string moduleName, CancellationToken cancellationToken = default)
    {
        EnsureBackend();
        return await _backend!.ImportModuleAsync(moduleName, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IPythonObjectHandle?> CreateObjectAsync(
        IPythonModuleHandle moduleHandle,
        string className,
        object?[]? args = null,
        Dictionary<string, object?>? kwargs = null,
        CancellationToken cancellationToken = default)
    {
        EnsureBackend();
        return await _backend!.CreateObjectAsync(moduleHandle, className, args, kwargs, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IPythonObjectHandle?> CreateObjectAsync(
        string moduleName,
        string className,
        object?[]? args = null,
        Dictionary<string, object?>? kwargs = null,
        CancellationToken cancellationToken = default)
    {
        EnsureBackend();
        return await _backend!.CreateObjectAsync(moduleName, className, args, kwargs, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<T?> CallMethodAsync<T>(
        IPythonObjectHandle handle,
        string methodName,
        object?[]? args = null,
        Dictionary<string, object?>? kwargs = null,
        CancellationToken cancellationToken = default)
    {
        EnsureBackend();
        return await _backend!.CallMethodAsync<T>(handle, methodName, args, kwargs, cancellationToken);
    }

    /// <inheritdoc />
    public async Task CallMethodAsync(
        IPythonObjectHandle handle,
        string methodName,
        object?[]? args = null,
        Dictionary<string, object?>? kwargs = null,
        CancellationToken cancellationToken = default)
    {
        EnsureBackend();
        await _backend!.CallMethodAsync(handle, methodName, args, kwargs, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<T?> GetAttributeAsync<T>(
        IPythonObjectHandle handle,
        string attributeName,
        CancellationToken cancellationToken = default)
    {
        EnsureBackend();
        return await _backend!.GetAttributeAsync<T>(handle, attributeName, cancellationToken);
    }

    /// <inheritdoc />
    public async Task SetAttributeAsync(
        IPythonObjectHandle handle,
        string attributeName,
        object? value,
        CancellationToken cancellationToken = default)
    {
        EnsureBackend();
        await _backend!.SetAttributeAsync(handle, attributeName, value, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DisposeHandleAsync(IPythonObjectHandle handle, CancellationToken cancellationToken = default)
    {
        if (_backend != null)
        {
            await _backend.DisposeHandleAsync(handle, cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task<T?> EvaluateAsync<T>(
        string expression,
        Dictionary<string, object?>? locals = null,
        CancellationToken cancellationToken = default)
    {
        EnsureBackend();
        return await _backend!.EvaluateAsync<T>(expression, locals, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<float[]> ToFloatArrayAsync(IPythonObjectHandle handle, CancellationToken cancellationToken = default)
    {
        EnsureBackend();
        return await _backend!.ToFloatArrayAsync(handle, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<float[][]> ToFloatArray2DAsync(IPythonObjectHandle handle, CancellationToken cancellationToken = default)
    {
        EnsureBackend();
        return await _backend!.ToFloatArray2DAsync(handle, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> IsModuleAvailableAsync(string moduleName, CancellationToken cancellationToken = default)
    {
        if (_backend == null) return false;
        return await _backend.IsModuleAvailableAsync(moduleName, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IPythonObjectHandle> CreateObjectHandleFromResultAsync(
        object result,
        CancellationToken cancellationToken = default)
    {
        EnsureBackend();
        return await _backend!.CreateObjectHandleFromResultAsync(result, cancellationToken);
    }

    private void EnsureBackend()
    {
        if (_backend == null)
            throw new InvalidOperationException("Python backend not initialized. Call Initialize() first.");
    }
}
