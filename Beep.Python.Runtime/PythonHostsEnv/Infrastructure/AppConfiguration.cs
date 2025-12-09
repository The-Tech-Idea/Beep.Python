using System.Collections.Generic;

namespace Beep.Python.RuntimeEngine.Infrastructure;

/// <summary>
/// Application configuration
/// </summary>
public class AppConfiguration
{
    /// <summary>
    /// Default model to use
    /// </summary>
    public string DefaultModel { get; set; } = "phi-3.5-mini";

    /// <summary>
    /// Model cache path
    /// </summary>
    public string ModelCachePath { get; set; } = "~/.beep-llm/models";

    /// <summary>
    /// Python runtime path
    /// </summary>
    public string PythonPath { get; set; } = "./python-embed";

    /// <summary>
    /// Maximum memory in GB
    /// </summary>
    public int MaxMemoryGB { get; set; } = 4;

    /// <summary>
    /// Whether to use GPU
    /// </summary>
    public bool UseGPU { get; set; }

    /// <summary>
    /// GPU device index to use (0 = first GPU)
    /// </summary>
    public int GpuDeviceIndex { get; set; } = 0;

    /// <summary>
    /// Maximum GPU memory per model in GB (null for unlimited)
    /// </summary>
    public float? MaxGpuMemoryGB { get; set; }

    /// <summary>
    /// Provider-specific configurations
    /// </summary>
    public Dictionary<string, ProviderConfig>? Providers { get; set; }

    /// <summary>
    /// Logging configuration
    /// </summary>
    public LoggingConfig? Logging { get; set; }

    /// <summary>
    /// Server configuration
    /// </summary>
    public ServerConfig? Server { get; set; }

    /// <summary>
    /// ROCm venv strategy: 'model', 'family', or 'single'
    /// </summary>
    public string RocmVenvStrategy { get; set; } = "model";
    /// <summary>
    /// Enable download progress display in CLI
    /// </summary>
    public bool EnableDownloadProgress { get; set; } = true;

    /// <summary>
    /// Optional Hugging Face token to use if no environment variable is set
    /// </summary>
    public string? HuggingFaceHubToken { get; set; } 

    /// <summary>
    /// Whether to auto-initialize the embedded Python runtime if the configured Python path does not exist
    /// </summary>
    public bool AutoInitializeRuntimeIfMissing { get; set; } = true;

    /// <summary>
    /// ChatML configuration settings
    /// </summary>
    public ChatMLSettings? ChatML { get; set; }
}

/// <summary>
/// ChatML settings in app configuration
/// </summary>
public class ChatMLSettings
{
    /// <summary>
    /// Whether to automatically apply ChatML system prompts from configuration
    /// </summary>
    public bool AutoApply { get; set; } = true;

    /// <summary>
    /// Default ChatML config path (if different from standard location)
    /// </summary>
    public string? ConfigPath { get; set; }
}

/// <summary>
/// Logging configuration
/// </summary>
public class LoggingConfig
{
    /// <summary>
    /// Minimum log level (Debug, Information, Warning, Error)
    /// </summary>
    public string Level { get; set; } = "Information";

    /// <summary>
    /// Log file path
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>
    /// Enable console logging
    /// </summary>
    public bool EnableConsole { get; set; } = true;

    /// <summary>
    /// Enable file logging
    /// </summary>
    public bool EnableFile { get; set; }
}

/// <summary>
/// Server configuration
/// </summary>
public class ServerConfig
{
    /// <summary>
    /// Default port
    /// </summary>
    public int Port { get; set; } = 5000;

    /// <summary>
    /// Host address
    /// </summary>
    public string Host { get; set; } = "localhost";

    /// <summary>
    /// Enable authentication
    /// </summary>
    public bool RequireAuth { get; set; }

    /// <summary>
    /// API key
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Enable CORS
    /// </summary>
    public bool EnableCors { get; set; } = true;

    /// <summary>
    /// Allowed origins for CORS
    /// </summary>
    public List<string>? AllowedOrigins { get; set; }
}
