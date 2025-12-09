using Beep.Python.Model;
using Beep.Python.RuntimeEngine.Infrastructure;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Beep.Python.RuntimeEngine;

namespace Beep.Python.RuntimeHost.Services;

/// <summary>
/// Virtual environment manager that uses VenvManager from the new infrastructure.
/// Provides virtual environment management using the copied Python environment handling.
/// </summary>
public class PythonHostVirtualEnvManager : IDisposable
{
    private readonly IVenvManager _venvManager;
    private readonly IPythonRuntimeManager? _runtimeManager;
    private readonly ILogger<PythonHostVirtualEnvManager> _logger;
    private readonly List<PythonVirtualEnvironment> _managedEnvironments = new();
    private bool _disposed = false;

    public PythonHostVirtualEnvManager(
        IVenvManager venvManager,
        IPythonRuntimeManager? runtimeManager,
        ILogger<PythonHostVirtualEnvManager> logger)
    {
        _venvManager = venvManager ?? throw new ArgumentNullException(nameof(venvManager));
        _runtimeManager = runtimeManager;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets all managed virtual environments.
    /// </summary>
    public List<PythonVirtualEnvironment> ManagedVirtualEnvironments => _managedEnvironments;

    /// <summary>
    /// Ensures a provider environment exists using VenvManager.
    /// </summary>
    public async Task<PythonVirtualEnvironment?> EnsureProviderEnvironmentAsync(
        string providerName,
        string? modelId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var venvPath = await _venvManager.EnsureProviderEnvironment(providerName, modelId, cancellationToken);
            if (string.IsNullOrEmpty(venvPath))
                return null;

            // Convert to PythonVirtualEnvironment
            var env = GetOrCreateEnvironment(venvPath, providerName);
            return env;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ensure provider environment: {Provider}", providerName);
            return null;
        }
    }

    /// <summary>
    /// Creates a virtual environment for a provider.
    /// </summary>
    public async Task<PythonVirtualEnvironment?> CreateProviderEnvironmentAsync(
        string providerName,
        string? modelId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var venvPath = await _venvManager.CreateProviderVirtualEnvironment(providerName, modelId, cancellationToken);
            if (string.IsNullOrEmpty(venvPath))
                return null;

            var env = GetOrCreateEnvironment(venvPath, providerName);
            return env;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create provider environment: {Provider}", providerName);
            return null;
        }
    }

    /// <summary>
    /// Gets an environment by path.
    /// </summary>
    public PythonVirtualEnvironment? GetEnvironmentByPath(string path)
    {
        return _managedEnvironments.FirstOrDefault(e => 
            e.Path.Equals(path, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets an environment by ID.
    /// </summary>
    public PythonVirtualEnvironment? GetEnvironmentById(string id)
    {
        return _managedEnvironments.FirstOrDefault(e => 
            e.ID.Equals(id, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets or creates a PythonVirtualEnvironment from a venv path.
    /// </summary>
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
            
        };

        _managedEnvironments.Add(env);
        return env;
    }

    /// <summary>
    /// Deletes a virtual environment.
    /// </summary>
    public async Task<bool> DeleteEnvironmentAsync(
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
                _managedEnvironments.RemoveAll(e => e.ID == environmentId);
                _logger.LogInformation("Deleted environment: {EnvironmentId}", environmentId);
            }
            return deleted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete environment: {EnvironmentId}", environmentId);
            return false;
        }
    }

    /// <summary>
    /// Gets provider package status.
    /// </summary>
    public async Task<List<ProviderPackageInfo>> GetProviderPackageStatusAsync(
        string providerName,
        CancellationToken cancellationToken = default)
    {
        return await _venvManager.GetProviderPackageStatus(providerName, cancellationToken);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _managedEnvironments.Clear();
            _disposed = true;
        }
    }
}
