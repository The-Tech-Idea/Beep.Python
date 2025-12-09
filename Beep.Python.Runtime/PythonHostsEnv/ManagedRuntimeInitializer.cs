using Microsoft.Extensions.Logging;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Beep.Python.RuntimeEngine.Infrastructure;

/// <summary>
/// Default implementation for IManagedRuntimeInitializer that delegates to IPythonPathResolver to ensure
/// runtime is initialized. Respects IConfigurationManager.AutoInitializeRuntimeIfMissing to decide whether
/// to attempt initialization when the runtime path is missing.
/// </summary>
public class ManagedRuntimeInitializer : IManagedRuntimeInitializer
{
    private readonly IPythonPathResolver _resolver;
    private readonly IConfigurationManager _configurationManager;
    private readonly ILogger<ManagedRuntimeInitializer> _logger;

    public ManagedRuntimeInitializer(IPythonPathResolver resolver, IConfigurationManager configManager, ILogger<ManagedRuntimeInitializer> logger)
    {
        _resolver = resolver;
        _configurationManager = configManager;
        _logger = logger;
    }

    public async Task EnsureRuntimeInitializedAsync(CancellationToken ct = default)
    {
        // If auto initialize is disabled, we don't proactively initialize
        if (!_configurationManager.AutoInitializeRuntimeIfMissing)
        {
            _logger.LogDebug("Auto initialize disabled via configuration");
            return;
        }

        // Ask the resolver to initialize the runtime if needed
        _logger.LogInformation("Checking for managed runtime and initializing if missing");
        try
        {
            await _resolver.EnsureRuntimeInitializedAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Managed runtime initialization failed");
        }
    }
}
