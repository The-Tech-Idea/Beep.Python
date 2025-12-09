using Beep.Python.Model;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Environment = System.Environment;

namespace Beep.Python.RuntimeEngine.Infrastructure;

/// <summary>
/// High-level orchestrator for Python runtime management in Infrastructure.
/// Coordinates PythonHost, RuntimeManager, VenvManager, and SessionManager.
/// Provides a unified API for Python operations using Infrastructure classes.
/// </summary>
public class PythonRuntimeOrchestrator : IDisposable
{
    private readonly IPythonHost _pythonHost;
    private readonly IPythonRuntimeManager _runtimeManager;
    private readonly IVenvManager _venvManager;
    private readonly SessionManager _sessionManager;
    private readonly VirtualEnvManager _virtualEnvManager;
    private readonly ILogger<PythonRuntimeOrchestrator> _logger;
    private bool _disposed = false;
    private bool _isInitialized = false;

    public PythonRuntimeOrchestrator(
        IPythonHost pythonHost,
        IPythonRuntimeManager runtimeManager,
        IVenvManager venvManager,
        ILogger<PythonRuntimeOrchestrator> logger,
        ILoggerFactory? loggerFactory = null)
    {
        _pythonHost = pythonHost ?? throw new ArgumentNullException(nameof(pythonHost));
        _runtimeManager = runtimeManager ?? throw new ArgumentNullException(nameof(runtimeManager));
        _venvManager = venvManager ?? throw new ArgumentNullException(nameof(venvManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        loggerFactory ??= Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance;

        // Create Infrastructure managers
        _sessionManager = new SessionManager(
            loggerFactory.CreateLogger<SessionManager>(),
            venvManager);

        _virtualEnvManager = new VirtualEnvManager(
            venvManager,
            runtimeManager,
            loggerFactory.CreateLogger<VirtualEnvManager>());
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
    public IPythonRuntimeManager RuntimeManager => _runtimeManager;

    /// <summary>
    /// Gets the session manager.
    /// </summary>
    public ISessionManager SessionManager => _sessionManager;

    /// <summary>
    /// Gets the virtual environment manager.
    /// </summary>
    public VirtualEnvManager VirtualEnvManager => _virtualEnvManager;

    /// <summary>
    /// Gets all available Python runtimes.
    /// </summary>
    public IEnumerable<PythonRuntimeInfo> GetAvailableRuntimes()
    {
        return _runtimeManager.GetAvailableRuntimes();
    }

    /// <summary>
    /// Initializes the orchestrator.
    /// </summary>
    public async Task<bool> InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Initializing PythonRuntimeOrchestrator...");

            // Initialize runtime manager
            var initialized = await _runtimeManager.Initialize();
            if (!initialized)
            {
                _logger.LogError("Failed to initialize runtime manager");
                return false;
            }

            // Initialize PythonHost if not already initialized
            if (!_pythonHost.IsInitialized)
            {
                var defaultRuntime = _runtimeManager.GetDefaultRuntime();
                if (defaultRuntime != null)
                {
                    var pythonExe = System.IO.Path.Combine(defaultRuntime.Path, "python.exe");
                    var hostInitialized = await _pythonHost.Initialize(pythonExe, cancellationToken);
                    if (!hostInitialized)
                    {
                        _logger.LogWarning("Failed to initialize PythonHost, but continuing...");
                    }
                }
            }

            _isInitialized = true;
            _logger.LogInformation("PythonRuntimeOrchestrator initialized successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize PythonRuntimeOrchestrator");
            return false;
        }
    }

    /// <summary>
    /// Creates a session for a user.
    /// </summary>
    public PythonSessionInfo CreateSession(string username, string? environmentId = null)
    {
        return _sessionManager.CreateSession(username, environmentId);
    }

    /// <summary>
    /// Creates a provider environment.
    /// </summary>
    public async Task<PythonVirtualEnvironment?> CreateProviderEnvironmentAsync(
        string providerName,
        string? modelId = null,
        CancellationToken cancellationToken = default)
    {
        return await _virtualEnvManager.CreateProviderEnvironmentAsync(providerName, modelId, cancellationToken);
    }

    /// <summary>
    /// Executes Python code.
    /// </summary>
    public async Task<(bool Success, string Output)> ExecuteCodeAsync(
        string code,
        PythonSessionInfo? session = null,
        int timeoutSeconds = 120,
        CancellationToken cancellationToken = default)
    {
        if (session == null)
        {
            session = CreateSession(Environment.UserName);
        }

        try
        {
            if (!_pythonHost.IsInitialized)
            {
                return (false, "PythonHost not initialized");
            }

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

            var result = await _pythonHost.ExecuteCode(code, cts.Token);
            return (result.Success, result.Output ?? result.Error ?? string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute code");
            return (false, $"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets managed virtual environments.
    /// </summary>
    public List<PythonVirtualEnvironment> GetManagedEnvironments()
    {
        return _virtualEnvManager.ManagedVirtualEnvironments;
    }

    /// <summary>
    /// Gets active sessions.
    /// </summary>
    public List<PythonSessionInfo> GetActiveSessions()
    {
        return _sessionManager.Sessions.Where(s => s.Status == PythonSessionStatus.Active).ToList();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _sessionManager?.Dispose();
            _virtualEnvManager?.Dispose();
            _disposed = true;
        }
    }
}
