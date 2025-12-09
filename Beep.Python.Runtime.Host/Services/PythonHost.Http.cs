using Beep.Python.RuntimeEngine.Infrastructure;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

using Beep.Python.RuntimeEngine;

namespace Beep.Python.RuntimeHost.Services;

/// <summary>
/// HTTP-based Python execution backend.
/// Communicates with a Python HTTP service for remote execution.
/// To use this backend, run a Python HTTP server that implements the expected endpoints.
/// </summary>
public class PythonHostHttp : IPythonHostBackend
{
    private readonly ILogger _logger;
    private readonly HttpClient _httpClient;
    private readonly Dictionary<string, RemoteModuleHandle> _moduleHandles = new();
    private readonly Dictionary<string, RemoteObjectHandle> _objectHandles = new();
    private int _handleCounter = 0;
    private bool _isInitialized;
    private readonly JsonSerializerOptions _jsonOptions;

    public bool IsInitialized => _isInitialized;
    public bool IsGILHeld => _isInitialized; // HTTP doesn't have GIL concept

    public PythonHostHttp(string serviceUrl, ILogger logger, string? apiToken = null)
    {
        _logger = logger;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(serviceUrl),
            Timeout = TimeSpan.FromMinutes(10)
        };

        if (!string.IsNullOrEmpty(apiToken))
        {
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiToken);
        }

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
            // Test connection
            var response = await _httpClient.GetAsync("/health", cancellationToken);
            _isInitialized = response.IsSuccessStatusCode;
            
            if (_isInitialized)
                _logger.LogInformation("HTTP Python backend initialized at {Url}", _httpClient.BaseAddress);
            else
                _logger.LogError("Failed to connect to HTTP Python backend at {Url}", _httpClient.BaseAddress);
            
            return _isInitialized;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize HTTP Python backend");
            return false;
        }
    }

    public IDisposable AcquireGIL()
    {
        // HTTP backend doesn't need GIL - return a no-op disposable
        return NoOpDisposable.Instance;
    }

    public async Task<IPythonModuleHandle?> ImportModuleAsync(string moduleName, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new { moduleName };
            var response = await _httpClient.PostAsJsonAsync("/api/import", request, _jsonOptions, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ImportModuleResponse>(_jsonOptions, cancellationToken);
            if (result?.HandleId == null)
                return null;

            var handle = new RemoteModuleHandle(result.HandleId, moduleName);
            _moduleHandles[result.HandleId] = handle;
            return handle;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import module via HTTP: {Module}", moduleName);
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

            var response = await _httpClient.PostAsJsonAsync("/api/create", request, _jsonOptions, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<CreateObjectResponse>(_jsonOptions, cancellationToken);
            if (result?.HandleId == null)
                return null;

            var handle = new RemoteObjectHandle(result.HandleId, result.TypeName ?? className);
            _objectHandles[result.HandleId] = handle;
            return handle;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create object via HTTP: {Class}", className);
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
                ReturnType = typeof(T).Name
            };

            var response = await _httpClient.PostAsJsonAsync("/api/call", request, _jsonOptions, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<CallMethodResponse<T>>(_jsonOptions, cancellationToken);
            
            // If result is a handle, wrap it
            if (result?.IsHandle == true && result.HandleId != null)
            {
                var objHandle = new RemoteObjectHandle(result.HandleId, result.TypeName ?? "object");
                _objectHandles[result.HandleId] = objHandle;
                return (T)(object)objHandle;
            }

            return result != null ? result.Value : default;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call method via HTTP: {Method}", methodName);
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
                ReturnType = typeof(T).Name
            };

            var response = await _httpClient.PostAsJsonAsync("/api/getattr", request, _jsonOptions, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<GetAttributeResponse<T>>(_jsonOptions, cancellationToken);
            return result != null ? result.Value : default;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get attribute via HTTP: {Attr}", attributeName);
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

            var response = await _httpClient.PostAsJsonAsync("/api/setattr", request, _jsonOptions, cancellationToken);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set attribute via HTTP: {Attr}", attributeName);
        }
    }

    public async Task DisposeHandleAsync(IPythonObjectHandle handle, CancellationToken cancellationToken = default)
    {
        try
        {
            _objectHandles.Remove(handle.HandleId);
            
            var request = new { handleId = handle.HandleId };
            await _httpClient.PostAsJsonAsync("/api/dispose", request, _jsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to dispose handle via HTTP: {HandleId}", handle.HandleId);
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
                ReturnType = typeof(T).Name
            };

            var response = await _httpClient.PostAsJsonAsync("/api/eval", request, _jsonOptions, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<EvaluateResponse<T>>(_jsonOptions, cancellationToken);
            return result != null ? result.Value : default;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to evaluate expression via HTTP");
            return default;
        }
    }

    public async Task<float[]> ToFloatArrayAsync(IPythonObjectHandle handle, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new { handleId = handle.HandleId };
            var response = await _httpClient.PostAsJsonAsync("/api/tofloatarray", request, _jsonOptions, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<float[]>(_jsonOptions, cancellationToken);
            return result ?? Array.Empty<float>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to convert to float array via HTTP");
            return Array.Empty<float>();
        }
    }

    public async Task<float[][]> ToFloatArray2DAsync(IPythonObjectHandle handle, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new { handleId = handle.HandleId };
            var response = await _httpClient.PostAsJsonAsync("/api/tofloatarray2d", request, _jsonOptions, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<float[][]>(_jsonOptions, cancellationToken);
            return result ?? Array.Empty<float[]>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to convert to 2D float array via HTTP");
            return Array.Empty<float[]>();
        }
    }

    public async Task<bool> IsModuleAvailableAsync(string moduleName, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new { moduleName };
            var response = await _httpClient.PostAsJsonAsync("/api/module-available", request, _jsonOptions, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ModuleAvailableResponse>(_jsonOptions, cancellationToken);
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

        // For HTTP backend, we need to send the value to the server and get a handle back
        try
        {
            var request = new { value = result };
            var response = await _httpClient.PostAsJsonAsync("/api/wrap", request, _jsonOptions, cancellationToken);
            response.EnsureSuccessStatusCode();

            var handleResult = await response.Content.ReadFromJsonAsync<CreateObjectResponse>(_jsonOptions, cancellationToken);
            if (handleResult?.HandleId == null)
                throw new InvalidOperationException("Failed to create handle from result");

            var handle = new RemoteObjectHandle(handleResult.HandleId, handleResult.TypeName ?? "object");
            _objectHandles[handleResult.HandleId] = handle;
            return handle;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create handle from result via HTTP");
            throw;
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }

    #region Request/Response DTOs

    private class ImportModuleResponse
    {
        public string? HandleId { get; set; }
    }

    private class CreateObjectRequest
    {
        public string? ModuleHandleId { get; set; }
        public string? ClassName { get; set; }
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
        public string? HandleId { get; set; }
        public string? MethodName { get; set; }
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
        public string? HandleId { get; set; }
        public string? AttributeName { get; set; }
        public string? ReturnType { get; set; }
    }

    private class GetAttributeResponse<T>
    {
        public T? Value { get; set; }
    }

    private class SetAttributeRequest
    {
        public string? HandleId { get; set; }
        public string? AttributeName { get; set; }
        public object? Value { get; set; }
    }

    private class EvaluateRequest
    {
        public string? Expression { get; set; }
        public Dictionary<string, object?>? Locals { get; set; }
        public string? ReturnType { get; set; }
    }

    private class EvaluateResponse<T>
    {
        public T? Value { get; set; }
    }

    private class ModuleAvailableResponse
    {
        public bool Available { get; set; }
    }

    #endregion

    #region Handle Classes

    private class RemoteModuleHandle : IPythonModuleHandle
    {
        public string HandleId { get; }
        public string ModuleName { get; }

        public RemoteModuleHandle(string handleId, string moduleName)
        {
            HandleId = handleId;
            ModuleName = moduleName;
        }

        public void Dispose() { }
    }

    private class RemoteObjectHandle : IPythonObjectHandle
    {
        public string HandleId { get; }
        public string TypeName { get; }
        public bool IsValid => true;

        public RemoteObjectHandle(string handleId, string typeName)
        {
            HandleId = handleId;
            TypeName = typeName;
        }

        public void Dispose() { }
    }

    #endregion
}
