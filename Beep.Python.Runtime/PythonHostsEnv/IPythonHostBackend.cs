using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Beep.Python.RuntimeEngine.Infrastructure;

/// <summary>
/// A no-operation disposable for backends that don't require GIL management.
/// </summary>
public sealed class NoOpDisposable : IDisposable
{
    /// <summary>
    /// Singleton instance for efficient reuse.
    /// </summary>
    public static readonly NoOpDisposable Instance = new();

    private NoOpDisposable() { }

    /// <inheritdoc />
    public void Dispose() { }
}

/// <summary>
/// Interface for Python host backends (Python.NET, HTTP, Pipe, RPC)
/// </summary>
public interface IPythonHostBackend : IDisposable
{
    bool IsInitialized { get; }
    bool IsGILHeld { get; }
    
    Task<bool> InitializeAsync(CancellationToken cancellationToken = default);
    IDisposable AcquireGIL();
    
    Task<IPythonModuleHandle?> ImportModuleAsync(string moduleName, CancellationToken cancellationToken = default);
    
    Task<IPythonObjectHandle?> CreateObjectAsync(
        IPythonModuleHandle moduleHandle,
        string className,
        object?[]? args = null,
        Dictionary<string, object?>? kwargs = null,
        CancellationToken cancellationToken = default);
    
    Task<IPythonObjectHandle?> CreateObjectAsync(
        string moduleName,
        string className,
        object?[]? args = null,
        Dictionary<string, object?>? kwargs = null,
        CancellationToken cancellationToken = default);
    
    Task<T?> CallMethodAsync<T>(
        IPythonObjectHandle handle,
        string methodName,
        object?[]? args = null,
        Dictionary<string, object?>? kwargs = null,
        CancellationToken cancellationToken = default);
    
    Task CallMethodAsync(
        IPythonObjectHandle handle,
        string methodName,
        object?[]? args = null,
        Dictionary<string, object?>? kwargs = null,
        CancellationToken cancellationToken = default);
    
    Task<T?> GetAttributeAsync<T>(IPythonObjectHandle handle, string attributeName, CancellationToken cancellationToken = default);
    Task SetAttributeAsync(IPythonObjectHandle handle, string attributeName, object? value, CancellationToken cancellationToken = default);
    Task DisposeHandleAsync(IPythonObjectHandle handle, CancellationToken cancellationToken = default);
    
    Task<T?> EvaluateAsync<T>(string expression, Dictionary<string, object?>? locals = null, CancellationToken cancellationToken = default);
    
    Task<float[]> ToFloatArrayAsync(IPythonObjectHandle handle, CancellationToken cancellationToken = default);
    Task<float[][]> ToFloatArray2DAsync(IPythonObjectHandle handle, CancellationToken cancellationToken = default);
    
    Task<bool> IsModuleAvailableAsync(string moduleName, CancellationToken cancellationToken = default);
    Task<IPythonObjectHandle> CreateObjectHandleFromResultAsync(object result, CancellationToken cancellationToken = default);
}
