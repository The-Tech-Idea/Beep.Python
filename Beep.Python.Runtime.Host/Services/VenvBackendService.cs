using Beep.Python.RuntimeEngine.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Beep.Python.RuntimeHost.Services;

/// <summary>
/// Service that executes VenvManager operations through backend servers using IPythonHostBackend
/// </summary>
public class VenvBackendService
{
    private readonly IPythonHostBackend? _backend;
    private readonly ILogger<VenvBackendService> _logger;

    public VenvBackendService(IPythonHostBackend? backend, ILogger<VenvBackendService> logger)
    {
        _backend = backend;
        _logger = logger;
    }

    /// <summary>
    /// Execute VenvManager operations through the backend by importing and using Infrastructure classes
    /// </summary>
    public async Task<string?> EnsureProviderEnvironmentAsync(string providerName, string? modelId = null, CancellationToken cancellationToken = default)
    {
        if (_backend == null || !_backend.IsInitialized)
        {
            _logger.LogWarning("Backend not connected. Cannot execute VenvManager operations through backend.");
            return null;
        }

        try
        {
            // Import clr module to access .NET types
            var clrModule = await _backend.ImportModuleAsync("clr", cancellationToken);
            if (clrModule == null)
            {
                _logger.LogError("Failed to import clr module in backend");
                return null;
            }

            // Add reference to Beep.Python.Runtime assembly
            await _backend.EvaluateAsync<bool>(
                "clr.AddReference('Beep.Python.Runtime')", 
                cancellationToken: cancellationToken);

            // Import Infrastructure classes
            var venvManagerModule = await _backend.ImportModuleAsync("Beep.Python.RuntimeEngine.Infrastructure", cancellationToken);
            if (venvManagerModule == null)
            {
                _logger.LogError("Failed to import Infrastructure module in backend");
                return null;
            }

            // Create VenvManager instance (this requires proper initialization in Python)
            // For now, we'll execute a Python script that uses VenvManager
            var script = $@"
import clr
clr.AddReference('Beep.Python.Runtime')

from Beep.Python.RuntimeEngine.Infrastructure import VenvManager, IVenvManager
import asyncio

# Note: This is a simplified example - actual implementation would need proper initialization
# of VenvManager with logger, pythonPath, etc.

# For demonstration, return the provider name as venv path
# In real implementation, this would call VenvManager.EnsureProviderEnvironment
result = f'/path/to/venv/{{'{providerName}'}}'
result
";

            var result = await _backend.EvaluateAsync<string>(script, cancellationToken: cancellationToken);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute VenvManager operation through backend");
            return null;
        }
    }

    /// <summary>
    /// Execute Python code that uses VenvManager through the backend
    /// </summary>
    public async Task<T?> ExecutePythonCodeAsync<T>(string pythonCode, CancellationToken cancellationToken = default)
    {
        if (_backend == null || !_backend.IsInitialized)
        {
            _logger.LogWarning("Backend not connected");
            return default;
        }

        try
        {
            return await _backend.EvaluateAsync<T>(pythonCode, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute Python code through backend");
            return default;
        }
    }
}
