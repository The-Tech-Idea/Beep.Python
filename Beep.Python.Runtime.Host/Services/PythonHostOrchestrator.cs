using Beep.Python.Model;
using Beep.Python.RuntimeEngine.Infrastructure;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Environment = System.Environment;

using Beep.Python.RuntimeEngine;

namespace Beep.Python.RuntimeHost.Services;

/// <summary>
/// High-level orchestrator that uses PythonHost backend infrastructure.
/// Coordinates PythonHost, PythonRuntimeManager, and VenvManager for Python environment management.
/// </summary>
public class PythonHostOrchestrator : IDisposable
{
    private readonly IPythonHost _pythonHost;
    private readonly IPythonRuntimeManager _runtimeManager;
    private readonly IVenvManager _venvManager;
    private readonly PythonHostRuntimeManager _hostRuntimeManager;
    private readonly PythonHostVirtualEnvManager _hostVenvManager;
    private readonly PythonHostCodeExecuteManager _codeExecuteManager;
    private readonly ILogger<PythonHostOrchestrator> _logger;
    private bool _disposed = false;
    private bool _isInitialized = false;

    public PythonHostOrchestrator(
        IPythonHost pythonHost,
        IPythonRuntimeManager runtimeManager,
        IVenvManager venvManager,
        ILogger<PythonHostOrchestrator> logger,
        ILoggerFactory? loggerFactory = null)
    {
        _pythonHost = pythonHost ?? throw new ArgumentNullException(nameof(pythonHost));
        _runtimeManager = runtimeManager ?? throw new ArgumentNullException(nameof(runtimeManager));
        _venvManager = venvManager ?? throw new ArgumentNullException(nameof(venvManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        loggerFactory ??= Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance;

        // Create sub-managers
        _hostRuntimeManager = new PythonHostRuntimeManager(
            pythonHost,
            runtimeManager,
            venvManager,
            loggerFactory.CreateLogger<PythonHostRuntimeManager>());

        _hostVenvManager = new PythonHostVirtualEnvManager(
            venvManager,
            runtimeManager,
            loggerFactory.CreateLogger<PythonHostVirtualEnvManager>());

        _codeExecuteManager = new PythonHostCodeExecuteManager(
            pythonHost,
            venvManager,
            loggerFactory.CreateLogger<PythonHostCodeExecuteManager>());
    }

    /// <summary>
    /// Gets whether the orchestrator is initialized.
    /// </summary>
    public bool IsInitialized => _isInitialized;

    /// <summary>
    /// Gets the PythonHost instance.
    /// </summary>
    public IPythonHost PythonHost => _pythonHost;

    /// <summary>
    /// Gets the runtime manager.
    /// </summary>
    public PythonHostRuntimeManager RuntimeManager => _hostRuntimeManager;

    /// <summary>
    /// Gets the virtual environment manager.
    /// </summary>
    public PythonHostVirtualEnvManager VirtualEnvManager => _hostVenvManager;

    /// <summary>
    /// Gets the code execution manager.
    /// </summary>
    public PythonHostCodeExecuteManager CodeExecuteManager => _codeExecuteManager;

    /// <summary>
    /// Gets all available Python runtimes.
    /// </summary>
    public IEnumerable<PythonRuntimeInfo> GetAvailableRuntimes()
    {
        return _runtimeManager.GetAvailableRuntimes();
    }

    /// <summary>
    /// Initializes the orchestrator with embedded Python runtime.
    /// </summary>
    public async Task<bool> InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Initializing PythonHostOrchestrator...");

            // Initialize runtime manager
            var initialized = await _hostRuntimeManager.InitializeAsync(cancellationToken);
            if (!initialized)
            {
                _logger.LogError("Failed to initialize runtime manager");
                return false;
            }

            _isInitialized = true;
            _logger.LogInformation("PythonHostOrchestrator initialized successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize PythonHostOrchestrator");
            return false;
        }
    }

    /// <summary>
    /// Creates a provider environment and ensures packages are installed.
    /// </summary>
    public async Task<PythonVirtualEnvironment?> CreateProviderEnvironmentAsync(
        string providerName,
        string? modelId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating provider environment: {Provider}", providerName);
            
            var env = await _hostVenvManager.CreateProviderEnvironmentAsync(
                providerName, 
                modelId, 
                cancellationToken);

            if (env != null)
            {
                _logger.LogInformation("Provider environment created: {Provider} at {Path}", 
                    providerName, env.Path);
            }

            return env;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create provider environment: {Provider}", providerName);
            return null;
        }
    }

    /// <summary>
    /// Executes Python code using PythonHost backend.
    /// </summary>
    public async Task<(bool Success, string Output)> ExecuteCodeAsync(
        string code,
        PythonSessionInfo? session = null,
        int timeoutSeconds = 120,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Create a default session if none provided
            session ??= new PythonSessionInfo
            {
                SessionId = Guid.NewGuid().ToString("N")[..8],
                Username = Environment.UserName,
                StartedAt = DateTime.Now,
                Status = PythonSessionStatus.Active
            };

            return await _codeExecuteManager.ExecuteCodeAsync(
                code, 
                session, 
                timeoutSeconds, 
                null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute code");
            return (false, $"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Executes a Python script file.
    /// </summary>
    public async Task<(bool Success, string Output)> ExecuteScriptAsync(
        string scriptPath,
        PythonSessionInfo? session = null,
        int timeoutSeconds = 300,
        CancellationToken cancellationToken = default)
    {
        try
        {
            session ??= new PythonSessionInfo
            {
                SessionId = Guid.NewGuid().ToString("N")[..8],
                Username = System.Environment.UserName,
                StartedAt = DateTime.Now,
                Status = PythonSessionStatus.Active
            };

            return await _codeExecuteManager.ExecuteScriptFileAsync(
                scriptPath,
                session,
                timeoutSeconds,
                null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute script: {Script}", scriptPath);
            return (false, $"Error: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _codeExecuteManager?.Dispose();
            _hostVenvManager?.Dispose();
            _hostRuntimeManager?.Dispose();
            _disposed = true;
        }
    }
}
