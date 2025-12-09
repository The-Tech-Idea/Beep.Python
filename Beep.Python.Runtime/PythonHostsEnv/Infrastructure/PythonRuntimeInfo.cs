using System;
using System.Collections.Generic;

namespace Beep.Python.RuntimeEngine.Infrastructure;

/// <summary>
/// Type of Python runtime
/// </summary>
public enum PythonRuntimeType
{
    /// <summary>
    /// System-installed Python
    /// </summary>
    System,
    
    /// <summary>
    /// Embedded Python distribution managed by Beep.LLM
    /// </summary>
    Embedded,
    
    /// <summary>
    /// Conda environment
    /// </summary>
    Conda,
    
    /// <summary>
    /// Virtual environment
    /// </summary>
    VirtualEnv,
    
    /// <summary>
    /// Custom/other type
    /// </summary>
    Custom,

    /// <summary>
    /// Unknown runtime type
    /// </summary>
    Unknown
}

/// <summary>
/// Information about a Python runtime environment
/// </summary>
public class PythonRuntimeInfo
{
    /// <summary>
    /// Unique runtime identifier
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the runtime
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Type of runtime
    /// </summary>
    public PythonRuntimeType Type { get; set; }

    /// <summary>
    /// Path to the runtime directory
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Python version
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Whether this runtime is managed by Beep.LLM
    /// </summary>
    public bool IsManaged { get; set; }

    /// <summary>
    /// Current status of the runtime
    /// </summary>
    public PythonRuntimeStatus Status { get; set; }

    /// <summary>
    /// When the runtime was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the runtime was last initialized
    /// </summary>
    public DateTime? LastInitialized { get; set; }

    /// <summary>
    /// When the runtime was last used
    /// </summary>
    public DateTime? LastUsed { get; set; }

    /// <summary>
    /// Last error message if any
    /// </summary>
    public string? LastError { get; set; }

    /// <summary>
    /// Installed packages and their versions
    /// </summary>
    public Dictionary<string, string> InstalledPackages { get; set; } = new();

    /// <summary>
    /// Associated LLM engines using this runtime
    /// </summary>
    public List<string> AssociatedEngines { get; set; } = new();

    /// <summary>
    /// Warning messages
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Error messages
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Runtime-specific configuration
    /// </summary>
    public Dictionary<string, object> Configuration { get; set; } = new();
}


/// <summary>
/// Status of a Python runtime
/// </summary>
public enum PythonRuntimeStatus
{
    /// <summary>
    /// Runtime not yet initialized
    /// </summary>
    NotInitialized,
    
    /// <summary>
    /// Runtime is being initialized
    /// </summary>
    Initializing,
    
    /// <summary>
    /// Runtime is ready for use
    /// </summary>
    Ready,
    
    /// <summary>
    /// Runtime is currently in use
    /// </summary>
    InUse,
    
    /// <summary>
    /// Runtime has an error
    /// </summary>
    Error,
    
    /// <summary>
    /// Runtime is unavailable
    /// </summary>
    Unavailable,

    /// <summary>
    /// Runtime is being updated
    /// </summary>
    Updating
}

/// <summary>
/// Configuration for Python Runtime Manager
/// </summary>
public class PythonRuntimeManagerConfig
{
    /// <summary>
    /// ID of the default runtime
    /// </summary>
    public string? DefaultRuntimeId { get; set; }

    /// <summary>
    /// List of all known runtimes
    /// </summary>
    public List<PythonRuntimeInfo> Runtimes { get; set; } = new();
}