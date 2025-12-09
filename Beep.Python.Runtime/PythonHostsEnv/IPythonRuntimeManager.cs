using System.Collections.Generic;
using System.Threading.Tasks;

namespace Beep.Python.RuntimeEngine.Infrastructure;

/// <summary>
/// Interface for managing Python runtime environments
/// </summary>
public interface IPythonRuntimeManager
{
    /// <summary>
    /// Initialize the runtime manager
    /// </summary>
    Task<bool> Initialize();

    /// <summary>
    /// Get all available Python runtimes
    /// </summary>
    IEnumerable<PythonRuntimeInfo> GetAvailableRuntimes();

    /// <summary>
    /// Get a specific runtime by ID
    /// </summary>
    PythonRuntimeInfo? GetRuntime(string runtimeId);

    /// <summary>
    /// Get the default runtime
    /// </summary>
    PythonRuntimeInfo? GetDefaultRuntime();

    /// <summary>
    /// Set the default runtime
    /// </summary>
    Task<bool> SetDefaultRuntime(string runtimeId);

    /// <summary>
    /// Create a new managed runtime
    /// </summary>
    Task<string> CreateManagedRuntime(string name, PythonRuntimeType type = PythonRuntimeType.Embedded);

    /// <summary>
    /// Delete a managed runtime
    /// </summary>
    Task<bool> DeleteRuntime(string runtimeId);

    /// <summary>
    /// Initialize a specific runtime
    /// </summary>
    Task<bool> InitializeRuntime(string runtimeId);

    /// <summary>
    /// Install packages in a runtime
    /// </summary>
    Task<bool> InstallPackages(string runtimeId, IEnumerable<string> packages);
}
