using System.Collections.Generic;

namespace Beep.Python.RuntimeEngine.Infrastructure;

/// <summary>
/// Information about a loaded model
/// </summary>
public class ModelInfo
{
    /// <summary>
    /// Model ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Model name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Provider name
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// Type of model (LLM or SLM)
    /// </summary>
    public ModelType ModelType { get; set; } = ModelType.SLM;

    /// <summary>
    /// Model size in bytes
    /// </summary>
    public long SizeInBytes { get; set; }

    /// <summary>
    /// Number of parameters
    /// </summary>
    public string? ParameterCount { get; set; }

    /// <summary>
    /// Maximum context length
    /// </summary>
    public int MaxContextLength { get; set; }

    /// <summary>
    /// Whether the model is currently loaded
    /// </summary>
    public bool IsLoaded { get; set; }

    /// <summary>
    /// Current quantization level
    /// </summary>
    public string? Quantization { get; set; }

    /// <summary>
    /// Whether GPU is being used
    /// </summary>
    public bool UsingGPU { get; set; }

    /// <summary>
    /// Memory usage in bytes
    /// </summary>
    public long MemoryUsageBytes { get; set; }

    /// <summary>
    /// Model capabilities
    /// </summary>
    public ModelCapabilities? Capabilities { get; set; }

    /// <summary>
    /// Additional metadata
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Capabilities supported by the model
/// </summary>
public class ModelCapabilities
{
    /// <summary>
    /// Supports chat/conversation
    /// </summary>
    public bool SupportsChat { get; set; }

    /// <summary>
    /// Supports function calling
    /// </summary>
    public bool SupportsFunctionCalling { get; set; }

    /// <summary>
    /// Supports streaming responses
    /// </summary>
    public bool SupportsStreaming { get; set; }

    /// <summary>
    /// Supports system prompts
    /// </summary>
    public bool SupportsSystemPrompt { get; set; }

    /// <summary>
    /// Supported languages
    /// </summary>
    public List<string>? SupportedLanguages { get; set; }
}
