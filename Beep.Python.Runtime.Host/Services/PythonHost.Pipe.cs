using Beep.Python.RuntimeEngine.Infrastructure;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

using Beep.Python.RuntimeEngine;

namespace Beep.Python.RuntimeHost.Services;

/// <summary>
/// Named Pipe-based Python execution backend.
/// Communicates with a Python process via named pipes for IPC.
/// This is faster than HTTP for local communication.
/// </summary>
public class PythonHostPipe : IPythonHostBackend
{
    private readonly ILogger _logger;
    private readonly string _pipeName;
    private NamedPipeClientStream? _pipeClient;
    private StreamReader? _reader;
    private StreamWriter? _writer;
    private readonly Dictionary<string, PipeObjectHandle> _objectHandles = new();
    private int _handleCounter = 0;
    private bool _isInitialized;
    private readonly SemaphoreSlim _pipeLock = new(1, 1);
    private readonly JsonSerializerOptions _jsonOptions;

    public bool IsInitialized => _isInitialized;
    public bool IsGILHeld => _isInitialized;

    public PythonHostPipe(string pipeName, ILogger logger)
    {
        _pipeName = pipeName;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    public async Task<bool> InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _pipeClient = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            
            _logger.LogInformation("Connecting to Python pipe server: {PipeName}", _pipeName);
            await _pipeClient.ConnectAsync(30000, cancellationToken); // 30 second timeout
            
            _reader = new StreamReader(_pipeClient, Encoding.UTF8);
            _writer = new StreamWriter(_pipeClient, Encoding.UTF8) { AutoFlush = true };
            
            // Send ping to verify connection
            var response = await SendCommandAsync<PingResponse>("ping", new { }, cancellationToken);
            _isInitialized = response?.Status == "ok";
            
            if (_isInitialized)
                _logger.LogInformation("Pipe Python backend connected: {PipeName}", _pipeName);
            else
                _logger.LogError("Failed to verify pipe connection: {PipeName}", _pipeName);
            
            return _isInitialized;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to Python pipe server: {PipeName}", _pipeName);
            return false;
        }
    }

    private async Task<T?> SendCommandAsync<T>(string command, object payload, CancellationToken cancellationToken)
    {
        await _pipeLock.WaitAsync(cancellationToken);
        try
        {
            if (_writer == null || _reader == null)
                throw new InvalidOperationException("Pipe not connected");

            var request = new PipeRequest { Command = command, Payload = payload };
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            
            await _writer.WriteLineAsync(json);
            
            var responseLine = await _reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrEmpty(responseLine))
                return default;

            var response = JsonSerializer.Deserialize<PipeResponse<T>>(responseLine, _jsonOptions);
            
            if (response?.Error != null)
            {
                _logger.LogError("Pipe command error: {Error}", response.Error);
                throw new InvalidOperationException(response.Error);
            }
            
            return response != null ? response.Result : default;
        }
        finally
        {
            _pipeLock.Release();
        }
    }

    public IDisposable AcquireGIL()
    {
        return NoOpDisposable.Instance;
    }

    public async Task<IPythonModuleHandle?> ImportModuleAsync(string moduleName, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await SendCommandAsync<ImportResult>("import", new { moduleName }, cancellationToken);
            if (result?.HandleId == null)
                return null;

            return new PipeModuleHandle(result.HandleId, moduleName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import module via pipe: {Module}", moduleName);
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
        try
        {
            var payload = new
            {
                moduleHandleId = moduleHandle.HandleId,
                className,
                args,
                kwargs
            };

            var result = await SendCommandAsync<CreateResult>("create", payload, cancellationToken);
            if (result?.HandleId == null)
                return null;

            var handle = new PipeObjectHandle(result.HandleId, result.TypeName ?? className);
            _objectHandles[result.HandleId] = handle;
            return handle;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create object via pipe: {Class}", className);
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
        var module = await ImportModuleAsync(moduleName, cancellationToken);
        if (module == null) return null;
        return await CreateObjectAsync(module, className, args, kwargs, cancellationToken);
    }

    public async Task<T?> CallMethodAsync<T>(
        IPythonObjectHandle handle,
        string methodName,
        object?[]? args = null,
        Dictionary<string, object?>? kwargs = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new
            {
                handleId = handle.HandleId,
                methodName,
                args,
                kwargs,
                returnType = typeof(T).Name
            };

            var result = await SendCommandAsync<CallResult<T>>("call", payload, cancellationToken);
            
            if (result?.IsHandle == true && result.HandleId != null)
            {
                var objHandle = new PipeObjectHandle(result.HandleId, result.TypeName ?? "object");
                _objectHandles[result.HandleId] = objHandle;
                return (T)(object)objHandle;
            }

            return result != null ? result.Value : default;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call method via pipe: {Method}", methodName);
            return default;
        }
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
        try
        {
            var payload = new
            {
                handleId = handle.HandleId,
                attributeName,
                returnType = typeof(T).Name
            };

            var result = await SendCommandAsync<AttributeResult<T>>("getattr", payload, cancellationToken);
            return result != null ? result.Value : default;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get attribute via pipe: {Attr}", attributeName);
            return default;
        }
    }

    public async Task SetAttributeAsync(
        IPythonObjectHandle handle,
        string attributeName,
        object? value,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new
            {
                handleId = handle.HandleId,
                attributeName,
                value
            };

            await SendCommandAsync<object>("setattr", payload, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set attribute via pipe: {Attr}", attributeName);
        }
    }

    public async Task DisposeHandleAsync(IPythonObjectHandle handle, CancellationToken cancellationToken = default)
    {
        try
        {
            _objectHandles.Remove(handle.HandleId);
            await SendCommandAsync<object>("dispose", new { handleId = handle.HandleId }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to dispose handle via pipe: {HandleId}", handle.HandleId);
        }
    }

    public async Task<T?> EvaluateAsync<T>(
        string expression,
        Dictionary<string, object?>? locals = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new
            {
                expression,
                locals,
                returnType = typeof(T).Name
            };

            var result = await SendCommandAsync<EvalResult<T>>("eval", payload, cancellationToken);
            return result != null ? result.Value : default;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to evaluate expression via pipe");
            return default;
        }
    }

    public async Task<float[]> ToFloatArrayAsync(IPythonObjectHandle handle, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await SendCommandAsync<float[]>("tofloatarray", new { handleId = handle.HandleId }, cancellationToken);
            return result ?? Array.Empty<float>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to convert to float array via pipe");
            return Array.Empty<float>();
        }
    }

    public async Task<float[][]> ToFloatArray2DAsync(IPythonObjectHandle handle, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await SendCommandAsync<float[][]>("tofloatarray2d", new { handleId = handle.HandleId }, cancellationToken);
            return result ?? Array.Empty<float[]>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to convert to 2D float array via pipe");
            return Array.Empty<float[]>();
        }
    }

    public async Task<bool> IsModuleAvailableAsync(string moduleName, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await SendCommandAsync<ModuleCheckResult>("module_available", new { moduleName }, cancellationToken);
            return result?.Available ?? false;
        }
        catch
        {
            return false;
        }
    }

    public async Task<IPythonObjectHandle> CreateObjectHandleFromResultAsync(
        object result,
        CancellationToken cancellationToken = default)
    {
        if (result is IPythonObjectHandle existingHandle)
            return existingHandle;

        try
        {
            var wrapResult = await SendCommandAsync<CreateResult>("wrap", new { value = result }, cancellationToken);
            if (wrapResult?.HandleId == null)
                throw new InvalidOperationException("Failed to create handle from result");

            var handle = new PipeObjectHandle(wrapResult.HandleId, wrapResult.TypeName ?? "object");
            _objectHandles[wrapResult.HandleId] = handle;
            return handle;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create handle from result via pipe");
            throw;
        }
    }

    public void Dispose()
    {
        _writer?.Dispose();
        _reader?.Dispose();
        _pipeClient?.Dispose();
        _pipeLock.Dispose();
    }

    #region Protocol DTOs

    private class PipeRequest
    {
        public string Command { get; set; } = "";
        public object? Payload { get; set; }
    }

    private class PipeResponse<T>
    {
        public T? Result { get; set; }
        public string? Error { get; set; }
    }

    private class PingResponse
    {
        public string? Status { get; set; }
    }

    private class ImportResult
    {
        public string? HandleId { get; set; }
    }

    private class CreateResult
    {
        public string? HandleId { get; set; }
        public string? TypeName { get; set; }
    }

    private class CallResult<T>
    {
        public T? Value { get; set; }
        public bool IsHandle { get; set; }
        public string? HandleId { get; set; }
        public string? TypeName { get; set; }
    }

    private class AttributeResult<T>
    {
        public T? Value { get; set; }
    }

    private class EvalResult<T>
    {
        public T? Value { get; set; }
    }

    private class ModuleCheckResult
    {
        public bool Available { get; set; }
    }

    #endregion

    #region Handle Classes

    private class PipeModuleHandle : IPythonModuleHandle
    {
        public string HandleId { get; }
        public string ModuleName { get; }

        public PipeModuleHandle(string handleId, string moduleName)
        {
            HandleId = handleId;
            ModuleName = moduleName;
        }

        public void Dispose() { }
    }

    private class PipeObjectHandle : IPythonObjectHandle
    {
        public string HandleId { get; }
        public string TypeName { get; }
        public bool IsValid => true;

        public PipeObjectHandle(string handleId, string typeName)
        {
            HandleId = handleId;
            TypeName = typeName;
        }

        public void Dispose() { }
    }

    #endregion
}
