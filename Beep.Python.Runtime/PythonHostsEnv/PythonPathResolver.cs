using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Beep.Python.RuntimeEngine.Infrastructure;

public class PythonPathResolver : IPythonPathResolver
{
    private readonly IPythonRuntimeManager _runtimeManager;
    private readonly ILogger<PythonPathResolver> _logger;

    public PythonPathResolver(IPythonRuntimeManager runtimeManager, ILogger<PythonPathResolver> logger)
    {
        _runtimeManager = runtimeManager;
        _logger = logger;
    }

    public string? GetDefaultRuntimePath()
    {
        try
        {
            var runtimePath = _runtimeManager.GetDefaultRuntime()?.Path;
            if (!string.IsNullOrEmpty(runtimePath)) return runtimePath;

            // Fallback to default Beep.LLM embedded runtime location (user profile)
            var defaultBeepPath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile), ".beep-llm", "python");
            return defaultBeepPath;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "GetDefaultRuntimePath failed");
            return null;
        }
    }

    public async Task EnsureRuntimeInitializedAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _runtimeManager.Initialize();
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to initialize IPythonRuntimeManager via resolver");
        }
    }
}
