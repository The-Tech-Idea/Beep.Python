using Beep.Python.RuntimeEngine.Infrastructure;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Beep.Python.RuntimeEngine;

namespace Beep.Python.RuntimeHost.Services;

/// <summary>
/// RPC-based Python execution backend using HTTP/2 transport.
/// Communicates with a Python RPC service for remote or high-performance IPC.
/// Suitable for distributed scenarios or microservices architecture.
/// Note: For full gRPC support, add Grpc.Net.Client NuGet package.
/// </summary>
public class PythonHostRpc : IPythonHostBackend
{
    private readonly ILogger _logger;
    private readonly string _serverAddress;
    private HttpClient? _httpClient;
    private readonly Dictionary<string, RpcObjectHandle> _objectHandles = new();
    private bool _isInitialized;

    public bool IsInitialized => _isInitialized;
    public bool IsGILHeld => _isInitialized;

    public PythonHostRpc(string serverAddress, ILogger logger)
    {
        _serverAddress = serverAddress;
        _logger = logger;
    }

    public async Task<bool> InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Use HTTP/2 for gRPC-like performance without requiring Grpc.Net.Client
            var handler = new SocketsHttpHandler
            {
                EnableMultipleHttp2Connections = true
            };
            _httpClient = new HttpClient(handler) { BaseAddress = new Uri(_serverAddress) };
            
            _logger.LogInformation("Connecting to Python RPC server: {Address}", _serverAddress);
            
            // Verify connection with a ping
            var response = await _httpClient.GetAsync("/health", cancellationToken);
            _isInitialized = response.IsSuccessStatusCode;
            
            if (_isInitialized)
                _logger.LogInformation("RPC Python backend connected: {Address}", _serverAddress);
            else
                _logger.LogError("Failed to verify RPC connection: {Address}", _serverAddress);
            
            return _isInitialized;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to Python RPC server: {Address}", _serverAddress);
            return false;
        }
    }

    /// <summary>
    /// Sends an RPC call to the Python server using HTTP/2 transport.
    /// Uses JSON serialization for simplicity - can be replaced with protobuf for better performance.
    /// </summary>
    private async Task<TResponse?> CallServiceAsync<TResponse>(
        string serviceName,
        string methodName,
        object request,
        CancellationToken cancellationToken)
    {
        if (_httpClient == null)
            throw new InvalidOperationException("RPC client not initialized");

        try
        {
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"/rpc/{serviceName}/{methodName}", content, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<TResponse>(responseJson);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RPC call failed: {Service}.{Method}", serviceName, methodName);
            throw;
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
            var request = new ImportModuleRequest { ModuleName = moduleName };
            var result = await CallServiceAsync<ImportModuleResponse>("PythonService", "ImportModule", request, cancellationToken);
            
            if (result?.HandleId == null)
                return null;

            return new RpcModuleHandle(result.HandleId, moduleName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import module via gRPC: {Module}", moduleName);
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
            var request = new CreateObjectRequest
            {
                ModuleHandleId = moduleHandle.HandleId,
                ClassName = className,
                Args = args,
                Kwargs = kwargs
            };

            var result = await CallServiceAsync<CreateObjectResponse>("PythonService", "CreateObject", request, cancellationToken);
            
            if (result?.HandleId == null)
                return null;

            var handle = new RpcObjectHandle(result.HandleId, result.TypeName ?? className);
            _objectHandles[result.HandleId] = handle;
            return handle;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create object via gRPC: {Class}", className);
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
            var request = new CallMethodRequest
            {
                HandleId = handle.HandleId,
                MethodName = methodName,
                Args = args,
                Kwargs = kwargs,
                ReturnType = typeof(T).FullName
            };

            var result = await CallServiceAsync<CallMethodResponse<T>>("PythonService", "CallMethod", request, cancellationToken);
            
            if (result == null)
                return default;

            if (result.IsHandle && result.HandleId != null)
            {
                var objHandle = new RpcObjectHandle(result.HandleId, result.TypeName ?? "object");
                _objectHandles[result.HandleId] = objHandle;
                return (T)(object)objHandle;
            }

            return result.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call method via gRPC: {Method}", methodName);
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
            var request = new GetAttributeRequest
            {
                HandleId = handle.HandleId,
                AttributeName = attributeName,
                ReturnType = typeof(T).FullName
            };

            var result = await CallServiceAsync<GetAttributeResponse<T>>("PythonService", "GetAttribute", request, cancellationToken);
            return result != null ? result.Value : default;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get attribute via gRPC: {Attr}", attributeName);
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
            var request = new SetAttributeRequest
            {
                HandleId = handle.HandleId,
                AttributeName = attributeName,
                Value = value
            };

            await CallServiceAsync<EmptyResponse>("PythonService", "SetAttribute", request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set attribute via gRPC: {Attr}", attributeName);
        }
    }

    public async Task DisposeHandleAsync(IPythonObjectHandle handle, CancellationToken cancellationToken = default)
    {
        try
        {
            _objectHandles.Remove(handle.HandleId);
            var request = new DisposeRequest { HandleId = handle.HandleId };
            await CallServiceAsync<EmptyResponse>("PythonService", "DisposeHandle", request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to dispose handle via gRPC: {HandleId}", handle.HandleId);
        }
    }

    public async Task<T?> EvaluateAsync<T>(
        string expression,
        Dictionary<string, object?>? locals = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new EvaluateRequest
            {
                Expression = expression,
                Locals = locals,
                ReturnType = typeof(T).FullName
            };

            var result = await CallServiceAsync<EvaluateResponse<T>>("PythonService", "Evaluate", request, cancellationToken);
            return result != null ? result.Value : default;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to evaluate expression via gRPC");
            return default;
        }
    }

    public async Task<float[]> ToFloatArrayAsync(IPythonObjectHandle handle, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new ToArrayRequest { HandleId = handle.HandleId };
            var result = await CallServiceAsync<ToFloatArrayResponse>("PythonService", "ToFloatArray", request, cancellationToken);
            return result?.Data ?? Array.Empty<float>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to convert to float array via gRPC");
            return Array.Empty<float>();
        }
    }

    public async Task<float[][]> ToFloatArray2DAsync(IPythonObjectHandle handle, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new ToArrayRequest { HandleId = handle.HandleId };
            var result = await CallServiceAsync<ToFloatArray2DResponse>("PythonService", "ToFloatArray2D", request, cancellationToken);
            return result?.Data ?? Array.Empty<float[]>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to convert to 2D float array via gRPC");
            return Array.Empty<float[]>();
        }
    }

    public async Task<bool> IsModuleAvailableAsync(string moduleName, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new ModuleCheckRequest { ModuleName = moduleName };
            var result = await CallServiceAsync<ModuleCheckResponse>("PythonService", "IsModuleAvailable", request, cancellationToken);
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
            var request = new WrapValueRequest { Value = result };
            var wrapResult = await CallServiceAsync<CreateObjectResponse>("PythonService", "WrapValue", request, cancellationToken);
            
            if (wrapResult?.HandleId == null)
                throw new InvalidOperationException("Failed to create handle from result");

            var handle = new RpcObjectHandle(wrapResult.HandleId, wrapResult.TypeName ?? "object");
            _objectHandles[wrapResult.HandleId] = handle;
            return handle;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create handle from result via gRPC");
            throw;
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }

    #region Protocol DTOs (would be replaced by protobuf-generated classes in production)

    private class ImportModuleRequest
    {
        public string ModuleName { get; set; } = "";
    }

    private class ImportModuleResponse
    {
        public string? HandleId { get; set; }
    }

    private class CreateObjectRequest
    {
        public string? ModuleHandleId { get; set; }
        public string ClassName { get; set; } = "";
        public object?[]? Args { get; set; }
        public Dictionary<string, object?>? Kwargs { get; set; }
    }

    private class CreateObjectResponse
    {
        public string? HandleId { get; set; }
        public string? TypeName { get; set; }
    }

    private class CallMethodRequest
    {
        public string HandleId { get; set; } = "";
        public string MethodName { get; set; } = "";
        public object?[]? Args { get; set; }
        public Dictionary<string, object?>? Kwargs { get; set; }
        public string? ReturnType { get; set; }
    }

    private class CallMethodResponse<T>
    {
        public T? Value { get; set; }
        public bool IsHandle { get; set; }
        public string? HandleId { get; set; }
        public string? TypeName { get; set; }
    }

    private class GetAttributeRequest
    {
        public string HandleId { get; set; } = "";
        public string AttributeName { get; set; } = "";
        public string? ReturnType { get; set; }
    }

    private class GetAttributeResponse<T>
    {
        public T? Value { get; set; }
    }

    private class SetAttributeRequest
    {
        public string HandleId { get; set; } = "";
        public string AttributeName { get; set; } = "";
        public object? Value { get; set; }
    }

    private class DisposeRequest
    {
        public string HandleId { get; set; } = "";
    }

    private class EvaluateRequest
    {
        public string Expression { get; set; } = "";
        public Dictionary<string, object?>? Locals { get; set; }
        public string? ReturnType { get; set; }
    }

    private class EvaluateResponse<T>
    {
        public T? Value { get; set; }
    }

    private class ToArrayRequest
    {
        public string HandleId { get; set; } = "";
    }

    private class ToFloatArrayResponse
    {
        public float[]? Data { get; set; }
    }

    private class ToFloatArray2DResponse
    {
        public float[][]? Data { get; set; }
    }

    private class ModuleCheckRequest
    {
        public string ModuleName { get; set; } = "";
    }

    private class ModuleCheckResponse
    {
        public bool Available { get; set; }
    }

    private class WrapValueRequest
    {
        public object? Value { get; set; }
    }

    private class EmptyResponse
    {
    }

    #endregion

    #region Handle Classes

    private class RpcModuleHandle : IPythonModuleHandle
    {
        public string HandleId { get; }
        public string ModuleName { get; }

        public RpcModuleHandle(string handleId, string moduleName)
        {
            HandleId = handleId;
            ModuleName = moduleName;
        }

        public void Dispose() { }
    }

    private class RpcObjectHandle : IPythonObjectHandle
    {
        public string HandleId { get; }
        public string TypeName { get; }
        public bool IsValid => true;

        public RpcObjectHandle(string handleId, string typeName)
        {
            HandleId = handleId;
            TypeName = typeName;
        }

        public void Dispose() { }
    }

    #endregion
}
