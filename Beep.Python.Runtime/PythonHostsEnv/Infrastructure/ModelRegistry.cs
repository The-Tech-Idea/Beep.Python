using System.Collections.Generic;

namespace Beep.Python.RuntimeEngine.Infrastructure;

/// <summary>
/// Registry of available models
/// </summary>
public class ModelRegistry
{
    /// <summary>
    /// List of available models
    /// </summary>
    public List<ModelConfig> Models { get; set; } = new();
}

/// <summary>
/// Model metadata for the registry
/// </summary>
public class ModelMetadata
{
    /// <summary>
    /// Model ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display name
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
    /// Hugging Face model ID
    /// </summary>
    public string HuggingFaceId { get; set; } = string.Empty;

    /// <summary>
    /// Model size (human readable)
    /// </summary>
    public string Size { get; set; } = string.Empty;

    /// <summary>
    /// Required RAM (human readable)
    /// </summary>
    public string RamRequired { get; set; } = string.Empty;

    /// <summary>
    /// Model description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Supported languages
    /// </summary>
    public List<string>? Languages { get; set; }

    /// <summary>
    /// Model tags
    /// </summary>
    public List<string>? Tags { get; set; }

    /// <summary>
    /// License information
    /// </summary>
    public string? License { get; set; }

    /// <summary>
    /// Model URL
    /// </summary>
    public string? Url { get; set; }
}
