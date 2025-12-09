using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Beep.Python.RuntimeEngine.Infrastructure;
using Microsoft.Extensions.Logging;
using Python.Runtime;

using Beep.Python.RuntimeEngine;

namespace Beep.Python.RuntimeHost.Services;

/// <summary>
/// Python.NET-based backend implementation.
/// Uses in-process Python execution via Python.NET (pythonnet) library.
/// This is the fastest backend but requires Python.NET initialization.
/// </summary>
public class PythonHostPythonNet : IPythonHostBackend
{
    private readonly ILogger _logger;
    private readonly Dictionary<string, PyObject> _moduleHandles = new();
    private readonly Dictionary<string, PyObject> _objectHandles = new();
    private int _handleCounter = 0;
    private bool _isInitialized;

    public bool IsInitialized => _isInitialized;
    public bool IsGILHeld => PythonEngine.IsInitialized;

    public PythonHostPythonNet(ILogger logger)
    {
        _logger = logger;
    }

    public Task<bool> InitializeAsync(CancellationToken cancellationToken = default)
    {
        // Python.NET initialization is handled by PythonHost.Initialize()
        // This backend just checks if it's already initialized
        _isInitialized = PythonEngine.IsInitialized;
        return Task.FromResult(_isInitialized);
    }

    /// <summary>
    /// Sets the initialization state. Called by PythonHost after runtime initialization.
    /// </summary>
    internal void SetInitialized(bool initialized)
    {
        _isInitialized = initialized;
    }

    public IDisposable AcquireGIL()
    {
        if (!_isInitialized)
            throw new InvalidOperationException("Python runtime not initialized");
        return Py.GIL();
    }

    public async Task<IPythonModuleHandle?> ImportModuleAsync(string moduleName, CancellationToken cancellationToken = default)
    {
        if (!_isInitialized)
            throw new InvalidOperationException("Python runtime not initialized");

        try
        {
            return await Task.Run(() =>
            {
                using (Py.GIL())
                {
                    var module = Py.Import(moduleName);
                    var handleId = $"module_{Interlocked.Increment(ref _handleCounter)}";
                    _moduleHandles[handleId] = module;
                    return (IPythonModuleHandle)new PythonNetModuleHandle(handleId, moduleName, module);
                }
            }, cancellationToken);
        }
        catch (PythonException ex)
        {
            _logger.LogError(ex, "Failed to import Python module: {Module}", moduleName);
            return null;
        }
    }

    public async Task<IPythonObjectHandle?> CreateObjectAsync(
        IPythonModuleHandle moduleHandle,
        string className,
        object?[]? args = null,
        Dictionary<string, object?>? kwargs = null,
        CancellationToken cancellationToken = default)
    {
        if (!_isInitialized)
            throw new InvalidOperationException("Python runtime not initialized");

        if (moduleHandle is not PythonNetModuleHandle pyModuleHandle || pyModuleHandle.PyModule == null)
            throw new ArgumentException("Invalid Python module handle");

        try
        {
            return await Task.Run(() =>
            {
                using (Py.GIL())
                {
                    dynamic module = pyModuleHandle.PyModule;
                    dynamic classObj = module.GetAttr(className);

                    // Build Python arguments
                    var pyArgs = new PyList();
                    if (args != null)
                    {
                        foreach (var arg in args)
                        {
                            pyArgs.Append(arg.ToPython());
                        }
                    }

                    // Build Python kwargs
                    var pyKwargs = new PyDict();
                    if (kwargs != null)
                    {
                        foreach (var kvp in kwargs)
                        {
                            if (kvp.Value != null)
                            {
                                pyKwargs[new PyString(kvp.Key)] = kvp.Value.ToPython();
                            }
                        }
                    }

                    // Call constructor with args and kwargs
                    PyObject instance;
                    if (args?.Length > 0 || kwargs?.Count > 0)
                    {
                        var pyArgsArray = pyArgs.As<PyObject[]>() ?? Array.Empty<PyObject>();
                        instance = classObj.Invoke(pyArgsArray, pyKwargs);
                    }
                    else
                    {
                        instance = classObj.Invoke(Array.Empty<PyObject>());
                    }

                    var handleId = $"obj_{Interlocked.Increment(ref _handleCounter)}";
                    _objectHandles[handleId] = instance;

                    // Get type name
                    string typeName = className;
                    try
                    {
                        dynamic typeObj = instance.GetAttr("__class__");
                        typeName = (string)typeObj.__name__;
                    }
                    catch { }

                    return (IPythonObjectHandle)new PythonNetObjectHandle(handleId, typeName, instance);
                }
            }, cancellationToken);
        }
        catch (PythonException ex)
        {
            _logger.LogError(ex, "Failed to create Python object: {Module}.{Class}", moduleHandle.ModuleName, className);
            return null;
        }
    }

    public async Task<IPythonObjectHandle?> CreateObjectAsync(
        string moduleName,
        string className,
        object?[]? args = null,
        Dictionary<string, object?>? kwargs = null,
        CancellationToken cancellationToken = default)
    {
        if (!_isInitialized)
            throw new InvalidOperationException("Python runtime not initialized");

        try
        {
            return await Task.Run(() =>
            {
                using (Py.GIL())
                {
                    dynamic module = Py.Import(moduleName);
                    dynamic classObj = module.GetAttr(className);

                    // Build Python arguments
                    var pyArgs = new PyList();
                    if (args != null)
                    {
                        foreach (var arg in args)
                        {
                            pyArgs.Append(arg.ToPython());
                        }
                    }

                    // Build Python kwargs
                    var pyKwargs = new PyDict();
                    if (kwargs != null)
                    {
                        foreach (var kvp in kwargs)
                        {
                            if (kvp.Value != null)
                            {
                                pyKwargs[new PyString(kvp.Key)] = kvp.Value.ToPython();
                            }
                        }
                    }

                    // Call constructor with args and kwargs
                    PyObject instance;
                    if (args?.Length > 0 || kwargs?.Count > 0)
                    {
                        var pyArgsArray = pyArgs.As<PyObject[]>() ?? Array.Empty<PyObject>();
                        instance = classObj.Invoke(pyArgsArray, pyKwargs);
                    }
                    else
                    {
                        instance = classObj.Invoke(Array.Empty<PyObject>());
                    }

                    var handleId = $"obj_{Interlocked.Increment(ref _handleCounter)}";
                    _objectHandles[handleId] = instance;

                    // Get type name
                    string typeName = className;
                    try
                    {
                        dynamic typeObj = instance.GetAttr("__class__");
                        typeName = (string)typeObj.__name__;
                    }
                    catch { }

                    return (IPythonObjectHandle)new PythonNetObjectHandle(handleId, typeName, instance);
                }
            }, cancellationToken);
        }
        catch (PythonException ex)
        {
            _logger.LogError(ex, "Failed to create Python object: {Module}.{Class}", moduleName, className);
            return null;
        }
    }

    public async Task<T?> CallMethodAsync<T>(
        IPythonObjectHandle handle,
        string methodName,
        object?[]? args = null,
        Dictionary<string, object?>? kwargs = null,
        CancellationToken cancellationToken = default)
    {
        if (!_isInitialized)
            throw new InvalidOperationException("Python runtime not initialized");

        if (handle is not PythonNetObjectHandle pyHandle || !pyHandle.IsValid)
            throw new ArgumentException("Invalid Python object handle");

        return await Task.Run(() =>
        {
            using (Py.GIL())
            {
                var pyObj = pyHandle.PyObject;
                dynamic method = pyObj.GetAttr(methodName);

                // Build kwargs
                var pyKwargs = new PyDict();
                if (kwargs != null)
                {
                    foreach (var kvp in kwargs)
                    {
                        if (kvp.Value != null)
                        {
                            pyKwargs[new PyString(kvp.Key)] = kvp.Value.ToPython();
                        }
                    }
                }

                // Build args
                var pyArgsList = new List<PyObject>();
                if (args != null)
                {
                    foreach (var arg in args)
                    {
                        pyArgsList.Add(arg.ToPython());
                    }
                }

                // Call method
                PyObject result;
                if (pyArgsList.Count > 0 || kwargs?.Count > 0)
                {
                    result = method.Invoke(pyArgsList.ToArray(), pyKwargs);
                }
                else
                {
                    result = method.Invoke(Array.Empty<PyObject>());
                }

                // Convert result
                return ConvertFromPython<T>(result);
            }
        }, cancellationToken);
    }

    public async Task CallMethodAsync(
        IPythonObjectHandle handle,
        string methodName,
        object?[]? args = null,
        Dictionary<string, object?>? kwargs = null,
        CancellationToken cancellationToken = default)
    {
        await CallMethodAsync<object>(handle, methodName, args, kwargs, cancellationToken);
    }

    public async Task<T?> GetAttributeAsync<T>(
        IPythonObjectHandle handle,
        string attributeName,
        CancellationToken cancellationToken = default)
    {
        if (!_isInitialized)
            throw new InvalidOperationException("Python runtime not initialized");

        if (handle is not PythonNetObjectHandle pyHandle || !pyHandle.IsValid)
            throw new ArgumentException("Invalid Python object handle");

        return await Task.Run(() =>
        {
            using (Py.GIL())
            {
                var pyObj = pyHandle.PyObject;
                PyObject attr = pyObj.GetAttr(attributeName);
                return ConvertFromPython<T>(attr);
            }
        }, cancellationToken);
    }

    public async Task SetAttributeAsync(
        IPythonObjectHandle handle,
        string attributeName,
        object? value,
        CancellationToken cancellationToken = default)
    {
        if (!_isInitialized)
            throw new InvalidOperationException("Python runtime not initialized");

        if (handle is not PythonNetObjectHandle pyHandle || !pyHandle.IsValid)
            throw new ArgumentException("Invalid Python object handle");

        await Task.Run(() =>
        {
            using (Py.GIL())
            {
                var pyObj = pyHandle.PyObject;
                pyObj.SetAttr(attributeName, value.ToPython());
            }
        }, cancellationToken);
    }

    public Task DisposeHandleAsync(IPythonObjectHandle handle, CancellationToken cancellationToken = default)
    {
        if (handle is PythonNetObjectHandle pyHandle)
        {
            _objectHandles.Remove(pyHandle.HandleId);
            pyHandle.Dispose();
        }
        return Task.CompletedTask;
    }

    public async Task<T?> EvaluateAsync<T>(
        string expression,
        Dictionary<string, object?>? locals = null,
        CancellationToken cancellationToken = default)
    {
        if (!_isInitialized)
            throw new InvalidOperationException("Python runtime not initialized");

        return await Task.Run(() =>
        {
            using (Py.GIL())
            {
                using var scope = Py.CreateScope();

                // Set local variables
                if (locals != null)
                {
                    foreach (var kvp in locals)
                    {
                        scope.Set(kvp.Key, kvp.Value.ToPython());
                    }
                }

                var result = scope.Eval(expression);
                return ConvertFromPython<T>(result);
            }
        }, cancellationToken);
    }

    public async Task<float[]> ToFloatArrayAsync(IPythonObjectHandle handle, CancellationToken cancellationToken = default)
    {
        if (handle is not PythonNetObjectHandle pyHandle || !pyHandle.IsValid)
            throw new ArgumentException("Invalid Python object handle");

        return await Task.Run(() =>
        {
            using (Py.GIL())
            {
                var pyArray = pyHandle.PyObject;
                using var np = Py.Import("numpy");
                dynamic flattened = ((dynamic)pyArray).flatten();
                dynamic pyList = flattened.tolist();

                var result = new List<float>();
                foreach (var item in pyList)
                {
                    result.Add((float)item);
                }

                return result.ToArray();
            }
        }, cancellationToken);
    }

    public async Task<float[][]> ToFloatArray2DAsync(IPythonObjectHandle handle, CancellationToken cancellationToken = default)
    {
        if (handle is not PythonNetObjectHandle pyHandle || !pyHandle.IsValid)
            throw new ArgumentException("Invalid Python object handle");

        return await Task.Run(() =>
        {
            using (Py.GIL())
            {
                var pyArray = pyHandle.PyObject;
                using var np = Py.Import("numpy");
                dynamic pyList = ((dynamic)pyArray).tolist();

                var result = new List<float[]>();
                foreach (var row in pyList)
                {
                    var rowList = new List<float>();
                    foreach (var item in row)
                    {
                        rowList.Add((float)item);
                    }
                    result.Add(rowList.ToArray());
                }

                return result.ToArray();
            }
        }, cancellationToken);
    }

    public async Task<bool> IsModuleAvailableAsync(string moduleName, CancellationToken cancellationToken = default)
    {
        if (!_isInitialized)
            return false;

        return await Task.Run(() =>
        {
            try
            {
                using (Py.GIL())
                {
                    Py.Import(moduleName);
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }, cancellationToken);
    }

    public async Task<IPythonObjectHandle> CreateObjectHandleFromResultAsync(
        object result,
        CancellationToken cancellationToken = default)
    {
        if (!_isInitialized)
            throw new InvalidOperationException("Python runtime not initialized");

        // If it's already a handle, return it
        if (result is IPythonObjectHandle existingHandle)
            return existingHandle;

        return await Task.Run(() =>
        {
            using (Py.GIL())
            {
                PyObject pyObj;
                if (result is PyObject pyResult)
                {
                    pyObj = pyResult;
                }
                else
                {
                    pyObj = result.ToPython();
                }

                var handleId = $"obj_{Interlocked.Increment(ref _handleCounter)}";
                _objectHandles[handleId] = pyObj;

                string typeName = "unknown";
                try
                {
                    dynamic typeObj = pyObj.GetAttr("__class__");
                    typeName = (string)typeObj.__name__;
                }
                catch { }

                return (IPythonObjectHandle)new PythonNetObjectHandle(handleId, typeName, pyObj);
            }
        }, cancellationToken);
    }

    public void Dispose()
    {
        // Don't shutdown Python.NET here - PythonHost manages that
        _moduleHandles.Clear();
        _objectHandles.Clear();
    }

    private T? ConvertFromPython<T>(PyObject pyObj)
    {
        if (pyObj == null || pyObj.IsNone())
            return default;

        var targetType = typeof(T);

        // Handle primitive types
        if (targetType == typeof(string))
            return (T)(object)pyObj.ToString()!;
        if (targetType == typeof(int))
            return (T)(object)(int)pyObj.As<long>();
        if (targetType == typeof(long))
            return (T)(object)pyObj.As<long>();
        if (targetType == typeof(float))
            return (T)(object)(float)pyObj.As<double>();
        if (targetType == typeof(double))
            return (T)(object)pyObj.As<double>();
        if (targetType == typeof(bool))
            return (T)(object)pyObj.As<bool>();

        // Handle object type - wrap in handle
        if (targetType == typeof(IPythonObjectHandle) || targetType == typeof(object))
        {
            var handleId = $"obj_{Interlocked.Increment(ref _handleCounter)}";
            _objectHandles[handleId] = pyObj;
            string typeName = "unknown";
            try
            {
                dynamic typeObj = pyObj.GetAttr("__class__");
                typeName = (string)typeObj.__name__;
            }
            catch { }
            return (T)(object)new PythonNetObjectHandle(handleId, typeName, pyObj);
        }

        // Try to convert to target type
        try
        {
            return pyObj.As<T>();
        }
        catch
        {
            return default;
        }
    }
}

#region Handle Classes for Python.NET Backend

/// <summary>
/// Internal handle implementation for Python modules (Python.NET backend)
/// </summary>
internal class PythonNetModuleHandle : IPythonModuleHandle
{
    private PyObject? _pyModule;
    private bool _disposed;

    public string HandleId { get; }
    public string ModuleName { get; }
    internal PyObject? PyModule => _pyModule;

    public PythonNetModuleHandle(string handleId, string moduleName, PyObject pyModule)
    {
        HandleId = handleId;
        ModuleName = moduleName;
        _pyModule = pyModule;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _pyModule = null;
            _disposed = true;
        }
    }
}

/// <summary>
/// Internal handle implementation for Python objects (Python.NET backend)
/// </summary>
internal class PythonNetObjectHandle : IPythonObjectHandle
{
    private PyObject? _pyObject;
    private bool _disposed;

    public string HandleId { get; }
    public string TypeName { get; }
    public bool IsValid => !_disposed && _pyObject != null;
    internal PyObject PyObject => _pyObject ?? throw new ObjectDisposedException(nameof(PythonNetObjectHandle));

    public PythonNetObjectHandle(string handleId, string typeName, PyObject pyObject)
    {
        HandleId = handleId;
        TypeName = typeName;
        _pyObject = pyObject;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _pyObject = null;
            _disposed = true;
        }
    }
}

#endregion
