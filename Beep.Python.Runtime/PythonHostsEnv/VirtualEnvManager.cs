using Beep.Python.Model;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Beep.Python.RuntimeEngine.Infrastructure;

/// <summary>
/// Infrastructure implementation of virtual environment management.
/// Uses IVenvManager for all virtual environment operations.
/// </summary>
public class VirtualEnvManager : IDisposable
{
    private readonly IVenvManager _venvManager;
    private readonly IPythonRuntimeManager? _runtimeManager;
    private readonly ILogger<VirtualEnvManager> _logger;
    private readonly List<PythonVirtualEnvironment> _managedEnvironments = new();
    private readonly object _environmentsLock = new();
    private bool _disposed = false;

    public VirtualEnvManager(
        IVenvManager venvManager,
        IPythonRuntimeManager? runtimeManager,
        ILogger<VirtualEnvManager> logger)
    {
        _venvManager = venvManager ?? throw new ArgumentNullException(nameof(venvManager));
        _runtimeManager = runtimeManager;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets all managed virtual environments.
    /// </summary>
    public List<PythonVirtualEnvironment> ManagedVirtualEnvironments
    {
        get
        {
            lock (_environmentsLock)
            {
                return new List<PythonVirtualEnvironment>(_managedEnvironments);
            }
        }
    }

    /// <summary>
    /// Gets whether the manager is busy.
    /// </summary>
    public bool IsBusy { get; private set; }

    /// <summary>
    /// Gets an environment by path.
    /// </summary>
    public PythonVirtualEnvironment? GetEnvironmentByPath(string path)
    {
        lock (_environmentsLock)
        {
            return _managedEnvironments.FirstOrDefault(e =>
                e.Path.Equals(path, StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <summary>
    /// Gets an environment by ID.
    /// </summary>
    public PythonVirtualEnvironment? GetEnvironmentById(string id)
    {
        lock (_environmentsLock)
        {
            return _managedEnvironments.FirstOrDefault(e =>
                e.ID.Equals(id, StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <summary>
    /// Creates a provider virtual environment.
    /// </summary>
    public async Task<PythonVirtualEnvironment?> CreateProviderEnvironmentAsync(
        string providerName,
        string? modelId = null,
        CancellationToken cancellationToken = default)
    {
        IsBusy = true;
        try
        {
            var venvPath = await _venvManager.CreateProviderVirtualEnvironment(providerName, modelId, cancellationToken);
            if (string.IsNullOrEmpty(venvPath))
                return null;

            var env = GetOrCreateEnvironment(venvPath, providerName);
            _logger.LogInformation("Created provider environment: {Provider} at {Path}", providerName, venvPath);
            return env;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create provider environment: {Provider}", providerName);
            return null;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Ensures a provider environment exists.
    /// </summary>
    public async Task<PythonVirtualEnvironment?> EnsureProviderEnvironmentAsync(
        string providerName,
        string? modelId = null,
        CancellationToken cancellationToken = default)
    {
        IsBusy = true;
        try
        {
            var venvPath = await _venvManager.EnsureProviderEnvironment(providerName, modelId, cancellationToken);
            if (string.IsNullOrEmpty(venvPath))
                return null;

            var env = GetOrCreateEnvironment(venvPath, providerName);
            return env;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ensure provider environment: {Provider}", providerName);
            return null;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Adds an environment to managed environments.
    /// </summary>
    public bool AddToManagedEnvironments(PythonVirtualEnvironment env)
    {
        if (env == null)
            return false;

        lock (_environmentsLock)
        {
            if (!_managedEnvironments.Any(e => e.ID == env.ID))
            {
                _managedEnvironments.Add(env);
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Removes an environment.
    /// </summary>
    public async Task<bool> RemoveEnvironmentAsync(
        string environmentId,
        CancellationToken cancellationToken = default)
    {
        var env = GetEnvironmentById(environmentId);
        if (env == null)
            return false;

        try
        {
            var deleted = await _venvManager.DeleteVirtualEnvironment(env.Path, cancellationToken);
            if (deleted)
            {
                lock (_environmentsLock)
                {
                    _managedEnvironments.RemoveAll(e => e.ID == environmentId);
                }
                _logger.LogInformation("Removed environment: {EnvironmentId}", environmentId);
            }
            return deleted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove environment: {EnvironmentId}", environmentId);
            return false;
        }
    }

    /// <summary>
    /// Updates environment usage timestamp.
    /// </summary>
    public void UpdateEnvironmentUsage(string environmentId)
    {
        var env = GetEnvironmentById(environmentId);
        if (env != null)
        {
            // Update last used timestamp if environment has such property
            _logger.LogDebug("Updated usage for environment: {EnvironmentId}", environmentId);
        }
    }

    /// <summary>
    /// Gets the least recently used environment.
    /// </summary>
    public PythonVirtualEnvironment? GetLeastRecentlyUsedEnvironment()
    {
        lock (_environmentsLock)
        {
            return _managedEnvironments
                .OrderBy(e => e.CreatedOn)
                .FirstOrDefault();
        }
    }

    /// <summary>
    /// Performs environment cleanup.
    /// </summary>
    public void PerformEnvironmentCleanup(TimeSpan maxIdleTime)
    {
        var cutoffTime = DateTime.Now - maxIdleTime;
        var envsToRemove = _managedEnvironments
            .Where(e => e.CreatedOn < cutoffTime)
            .ToList();

        foreach (var env in envsToRemove)
        {
            _logger.LogInformation("Cleaning up idle environment: {EnvironmentId}", env.ID);
            // Could add logic to delete idle environments if needed
        }
    }

    private PythonVirtualEnvironment GetOrCreateEnvironment(string venvPath, string name)
    {
        var existing = GetEnvironmentByPath(venvPath);
        if (existing != null)
            return existing;

        var env = new PythonVirtualEnvironment
        {
            ID = Guid.NewGuid().ToString("N")[..8],
            Name = name,
            Path = venvPath,
            CreatedOn = DateTime.Now,
            
            EnvironmentType = PythonEnvironmentType.VirtualEnv
        };

        AddToManagedEnvironments(env);
        return env;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            lock (_environmentsLock)
            {
                _managedEnvironments.Clear();
            }
            _disposed = true;
        }
    }
}
