using System;
using Beep.Python.RuntimeEngine;
using Beep.Python.RuntimeEngine.Infrastructure;
using Microsoft.Extensions.Logging;
using Environment = System.Environment;

namespace Beep.Python.RuntimeHost.Services;

/// <summary>
/// Factory for creating Python host backends.
/// Makes it easy to switch between Python.NET, HTTP, Pipe, and RPC backends.
/// </summary>
public static class PythonBackendFactory
{
    /// <summary>
    /// Creates a Python host backend based on the specified configuration.
    /// </summary>
    /// <param name="config">Backend configuration</param>
    /// <param name="logger">Logger instance</param>
    /// <returns>Configured Python host backend</returns>
    public static IPythonHostBackend CreateBackend(PythonBackendConfig config, ILogger logger)
    {
        return config.BackendType switch
        {
            PythonBackendType.Http => new PythonHostHttp(config.HttpBaseUrl, logger),
            PythonBackendType.Pipe => new PythonHostPipe(config.PipeName, logger),
            PythonBackendType.Rpc => new PythonHostRpc(config.RpcAddress, logger),
            PythonBackendType.PythonNet => throw new NotSupportedException(
                "Python.NET backend should use PythonHost directly, not via factory. " +
                "This is because Python.NET requires additional initialization that is specific to the runtime."),
            _ => throw new ArgumentException($"Unknown backend type: {config.BackendType}")
        };
    }

    /// <summary>
    /// Creates a Python host backend based on the specified backend type with default settings.
    /// </summary>
    /// <param name="backendType">Backend type</param>
    /// <param name="logger">Logger instance</param>
    /// <returns>Configured Python host backend</returns>
    public static IPythonHostBackend CreateBackend(PythonBackendType backendType, ILogger logger)
    {
        return CreateBackend(new PythonBackendConfig { BackendType = backendType }, logger);
    }

    /// <summary>
    /// Creates an HTTP backend with the specified base URL.
    /// </summary>
    public static PythonHostHttp CreateHttpBackend(string baseUrl, ILogger logger)
    {
        return new PythonHostHttp(baseUrl, logger);
    }

    /// <summary>
    /// Creates a Pipe backend with the specified pipe name.
    /// </summary>
    public static PythonHostPipe CreatePipeBackend(string pipeName, ILogger logger)
    {
        return new PythonHostPipe(pipeName, logger);
    }

    /// <summary>
    /// Creates an RPC backend with the specified server address.
    /// </summary>
    public static PythonHostRpc CreateRpcBackend(string serverAddress, ILogger logger)
    {
        return new PythonHostRpc(serverAddress, logger);
    }
}

/// <summary>
/// Extension methods for Python backend configuration.
/// </summary>
public static class PythonBackendExtensions
{
    /// <summary>
    /// Configures the backend from environment variables.
    /// PYTHON_BACKEND_TYPE: "pythonnet", "http", "pipe", "rpc"
    /// PYTHON_HTTP_URL: HTTP server URL
    /// PYTHON_PIPE_NAME: Named pipe name
    /// PYTHON_RPC_ADDRESS: gRPC server address
    /// </summary>
    public static PythonBackendConfig ConfigureFromEnvironment(this PythonBackendConfig config)
    {
        var backendTypeStr = Environment.GetEnvironmentVariable("PYTHON_BACKEND_TYPE");
        if (!string.IsNullOrEmpty(backendTypeStr))
        {
            config.BackendType = backendTypeStr.ToLowerInvariant() switch
            {
                "pythonnet" => PythonBackendType.PythonNet,
                "http" => PythonBackendType.Http,
                "pipe" => PythonBackendType.Pipe,
                "rpc" => PythonBackendType.Rpc,
                _ => config.BackendType
            };
        }

        var httpUrl = Environment.GetEnvironmentVariable("PYTHON_HTTP_URL");
        if (!string.IsNullOrEmpty(httpUrl))
            config.HttpBaseUrl = httpUrl;

        var pipeName = Environment.GetEnvironmentVariable("PYTHON_PIPE_NAME");
        if (!string.IsNullOrEmpty(pipeName))
            config.PipeName = pipeName;

        var rpcAddress = Environment.GetEnvironmentVariable("PYTHON_RPC_ADDRESS");
        if (!string.IsNullOrEmpty(rpcAddress))
            config.RpcAddress = rpcAddress;

        return config;
    }
}