using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Beep.Python.RuntimeEngine.Infrastructure;

/// <summary>
/// Interface for managing embedded Python runtime
/// </summary>
public interface IPythonHost
{
    /// <summary>
    /// Indicates whether Python runtime is initialized
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    /// Path to the Python runtime
    /// </summary>
    string? PythonPath { get; }

    /// <summary>
    /// Initialize the Python runtime
    /// </summary>
    /// <param name="pythonPath">Optional custom Python path</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<bool> Initialize(string? pythonPath = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute a Python script
    /// </summary>
    /// <param name="scriptPath">Path to the Python script</param>
    /// <param name="arguments">Script arguments</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Script execution result</returns>
    Task<PythonExecutionResult> ExecuteScript(string scriptPath, Dictionary<string, object>? arguments = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute Python code directly
    /// </summary>
    /// <param name="code">Python code to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Execution result</returns>
    Task<PythonExecutionResult> ExecuteCode(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Install Python packages
    /// </summary>
    /// <param name="packages">Package names to install</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<bool> InstallPackages(IEnumerable<string> packages, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ensure required packages are installed on demand
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<bool> EnsurePackagesInstalledOnDemand(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get Python executable for a specific provider
    /// </summary>
    Task<string?> GetProviderExecutable(string providerName, string? modelId = null, CancellationToken cancellationToken = default);
    
    Task<string?> GetProviderBackend(string providerName, string? modelId = null, CancellationToken cancellationToken = default);
    
    Task<string?> PrepareProviderEnvironment(string providerName, string? modelId = null, bool forceRecreate = false, CancellationToken cancellationToken = default);
    
    Task<bool> RemoveProviderEnvironment(string providerName, string? modelId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get default Python executable for backward compatibility
    /// </summary>
    string GetDefaultExecutable();

    /// <summary>
    /// Get Python executable for the currently active provider/engine environment
    /// </summary>
    Task<string?> GetCurrentExecutable(CancellationToken cancellationToken = default);

    /// <summary>
    /// Ensure provider environment exists with all required packages installed
    /// </summary>
    Task<string?> EnsureProviderEnvironment(string providerName, string? modelId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Currently active provider name
    /// </summary>
    string? CurrentProvider { get; }

    /// <summary>
    /// Currently active provider's model id
    /// </summary>
    string? CurrentProviderModelId { get; }

    /// <summary>
    /// Path to the currently active provider virtual environment
    /// </summary>
    string? CurrentProviderVenvPath { get; }

    /// <summary>
    /// Get package installation status for a specific provider
    /// </summary>
    Task<List<ProviderPackageInfo>> GetProviderPackageStatus(string providerName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Shutdown the Python runtime
    /// </summary>
    Task Shutdown();

    // ==========================================================================
    // Python Execution Abstraction Layer
    // ==========================================================================

    /// <summary>
    /// Import a Python module and return a handle
    /// </summary>
    Task<IPythonModuleHandle?> ImportModuleAsync(string moduleName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a Python object instance from a class using a module handle
    /// </summary>
    Task<IPythonObjectHandle?> CreateObjectAsync(
        IPythonModuleHandle moduleHandle,
        string className,
        object?[]? args = null,
        Dictionary<string, object?>? kwargs = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a Python object instance from a class
    /// </summary>
    Task<IPythonObjectHandle?> CreateObjectAsync(
        string moduleName, 
        string className, 
        object[]? args = null, 
        Dictionary<string, object>? kwargs = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Call a method on a Python object handle
    /// </summary>
    Task<T?> CallMethodAsync<T>(
        IPythonObjectHandle handle, 
        string methodName, 
        object?[]? args = null, 
        Dictionary<string, object?>? kwargs = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Call a method on a Python object handle (no return value)
    /// </summary>
    Task CallMethodAsync(
        IPythonObjectHandle handle, 
        string methodName, 
        object?[]? args = null, 
        Dictionary<string, object?>? kwargs = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get an attribute from a Python object
    /// </summary>
    Task<T?> GetAttributeAsync<T>(IPythonObjectHandle handle, string attributeName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Set an attribute on a Python object
    /// </summary>
    Task SetAttributeAsync(IPythonObjectHandle handle, string attributeName, object? value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Dispose a Python object handle
    /// </summary>
    Task DisposeHandleAsync(IPythonObjectHandle handle, CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute a Python expression and return the result
    /// </summary>
    Task<T?> EvaluateAsync<T>(string expression, Dictionary<string, object?>? locals = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a Python module/package is available
    /// </summary>
    Task<bool> IsModuleAvailableAsync(string moduleName, CancellationToken cancellationToken = default);
}

/// <summary>
/// Handle to a Python module for abstracted operations
/// </summary>
public interface IPythonModuleHandle : IDisposable
{
    /// <summary>
    /// Module name
    /// </summary>
    string ModuleName { get; }

    /// <summary>
    /// Unique handle identifier
    /// </summary>
    string HandleId { get; }
}

/// <summary>
/// Handle to a Python object for abstracted operations
/// </summary>
public interface IPythonObjectHandle : IDisposable
{
    /// <summary>
    /// Unique handle identifier
    /// </summary>
    string HandleId { get; }

    /// <summary>
    /// Type name of the Python object
    /// </summary>
    string TypeName { get; }

    /// <summary>
    /// Whether the handle is still valid
    /// </summary>
    bool IsValid { get; }
}
