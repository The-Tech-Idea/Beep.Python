using Beep.Python.RuntimeEngine.Infrastructure;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Beep.Python.RuntimeEngine;

namespace Beep.Python.RuntimeHost.Services;

/// <summary>
/// Runtime manager that uses PythonHost and PythonRuntimeManager from the new infrastructure.
/// Provides runtime management using the copied Python environment handling.
/// </summary>
public class PythonHostRuntimeManager : IDisposable
{
    private readonly IPythonHost _pythonHost;
    private readonly IPythonRuntimeManager _runtimeManager;
    private readonly IVenvManager? _venvManager;
    private readonly ILogger<PythonHostRuntimeManager> _logger;
    private bool _disposed = false;

    public PythonHostRuntimeManager(
        IPythonHost pythonHost,
        IPythonRuntimeManager runtimeManager,
        IVenvManager? venvManager,
        ILogger<PythonHostRuntimeManager> logger)
    {
        _pythonHost = pythonHost ?? throw new ArgumentNullException(nameof(pythonHost));
        _runtimeManager = runtimeManager ?? throw new ArgumentNullException(nameof(runtimeManager));
        _venvManager = venvManager;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Initializes the runtime manager using PythonRuntimeManager.
    /// </summary>
    public async Task<bool> InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Initializing PythonHostRuntimeManager...");

            // Initialize the runtime manager
            var initialized = await _runtimeManager.Initialize();
            if (!initialized)
            {
                _logger.LogError("Failed to initialize PythonRuntimeManager");
                return false;
            }

            // Initialize PythonHost if not already initialized
            if (!_pythonHost.IsInitialized)
            {
                var defaultRuntime = _runtimeManager.GetDefaultRuntime();
                if (defaultRuntime != null)
                {
                    var pythonExe = Path.Combine(defaultRuntime.Path, "python.exe");
                    var hostInitialized = await _pythonHost.Initialize(pythonExe, cancellationToken);
                    if (!hostInitialized)
                    {
                        _logger.LogWarning("Failed to initialize PythonHost, but continuing...");
                    }
                }
            }

            _logger.LogInformation("PythonHostRuntimeManager initialized successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize PythonHostRuntimeManager");
            return false;
        }
    }

    /// <summary>
    /// Gets all available Python runtimes.
    /// </summary>
    public IEnumerable<PythonRuntimeInfo> GetAvailableRuntimes()
    {
        return _runtimeManager.GetAvailableRuntimes();
    }

    /// <summary>
    /// Gets the default runtime.
    /// </summary>
    public PythonRuntimeInfo? GetDefaultRuntime()
    {
        return _runtimeManager.GetDefaultRuntime();
    }

    /// <summary>
    /// Sets the default runtime.
    /// </summary>
    public async Task<bool> SetDefaultRuntimeAsync(string runtimeId, CancellationToken cancellationToken = default)
    {
        return await _runtimeManager.SetDefaultRuntime(runtimeId);
    }

    /// <summary>
    /// Creates a new managed runtime.
    /// </summary>
    public async Task<string> CreateManagedRuntimeAsync(
        string name,
        PythonRuntimeType type = PythonRuntimeType.Embedded,
        CancellationToken cancellationToken = default)
    {
        return await _runtimeManager.CreateManagedRuntime(name, type);
    }

    /// <summary>
    /// Gets Python executable for a specific runtime.
    /// </summary>
    public async Task<string?> GetPythonExecutableAsync(string runtimeId, CancellationToken cancellationToken = default)
    {
        var runtime = _runtimeManager.GetRuntime(runtimeId);
        if (runtime == null)
            return null;

        var pythonExe = Path.Combine(runtime.Path, "python.exe");
        return File.Exists(pythonExe) ? pythonExe : null;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _pythonHost?.Shutdown();
            _disposed = true;
        }
    }
}
