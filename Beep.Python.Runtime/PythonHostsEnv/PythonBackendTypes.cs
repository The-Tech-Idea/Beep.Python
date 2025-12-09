using System;

namespace Beep.Python.RuntimeEngine;

/// <summary>
/// Defines the available Python host backend types.
/// </summary>
public enum PythonBackendType
{
    /// <summary>
    /// Python.NET - Direct in-process Python execution (fastest, requires Python.NET)
    /// </summary>
    PythonNet,
    
    /// <summary>
    /// HTTP - REST API communication with Python HTTP server
    /// </summary>
    Http,
    
    /// <summary>
    /// Named Pipes - IPC via named pipes (fast local communication)
    /// </summary>
    Pipe,
    
    /// <summary>
    /// gRPC/RPC - High-performance remote procedure calls
    /// </summary>
    Rpc
}

/// <summary>
/// Configuration for Python host backends.
/// </summary>
public class PythonBackendConfig
{
    /// <summary>
    /// The backend type to use.
    /// </summary>
    public PythonBackendType BackendType { get; set; } = PythonBackendType.PythonNet;
    
    /// <summary>
    /// Virtual environment path. Required for all backend types.
    /// The backend will use Python from this venv.
    /// </summary>
    public string? VirtualEnvPath { get; set; }
    
    /// <summary>
    /// Provider name (used to auto-detect virtual environment if VirtualEnvPath is not set).
    /// </summary>
    public string? ProviderName { get; set; }
    
    /// <summary>
    /// Whether to auto-start the Python server for remote backends (HTTP, Pipe, RPC).
    /// Default: true
    /// </summary>
    public bool AutoStartServer { get; set; } = true;
    
    /// <summary>
    /// HTTP server base URL (for Http backend).
    /// If AutoStartServer is true, this is ignored and a dynamic port is used.
    /// Default: "http://localhost:5678"
    /// </summary>
    public string HttpBaseUrl { get; set; } = "http://localhost:5678";
    
    /// <summary>
    /// Named pipe name (for Pipe backend).
    /// If AutoStartServer is true, this is ignored and a unique name is generated.
    /// Default: "beep-python-pipe"
    /// </summary>
    public string PipeName { get; set; } = "beep-python-pipe";
    
    /// <summary>
    /// gRPC server address (for Rpc backend).
    /// If AutoStartServer is true, this is ignored and a dynamic port is used.
    /// Default: "http://localhost:50051"
    /// </summary>
    public string RpcAddress { get; set; } = "http://localhost:50051";
    
    /// <summary>
    /// Python executable path (for PythonNet backend, optional - derived from VirtualEnvPath).
    /// </summary>
    public string? PythonPath { get; set; }
}
