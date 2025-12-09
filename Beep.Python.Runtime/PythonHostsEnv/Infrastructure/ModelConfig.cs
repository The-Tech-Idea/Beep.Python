using System.Collections.Generic;

namespace Beep.Python.RuntimeEngine.Infrastructure;

/// <summary>
/// Type of model (Large Language Model or Small Language Model)
/// </summary>
public enum ModelType
{
    /// <summary>
    /// Large Language Model
    /// </summary>
    LLM,
    
    /// <summary>
    /// Small Language Model
    /// </summary>
    SLM
}

/// <summary>
/// Configuration for an LLM model
/// </summary>
public class ModelConfig
{
    /// <summary>
    /// Unique identifier for the model
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the model
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Provider name (e.g., "phi", "tinyllama", "llama3")
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
    /// Model size (e.g., "7.6GB")
    /// </summary>
    public string Size { get; set; } = string.Empty;

    /// <summary>
    /// Required RAM (e.g., "4GB")
    /// </summary>
    public string RamRequired { get; set; } = string.Empty;

    /// <summary>
    /// Model description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Total parameter count (e.g., "8B")
    /// </summary>
    public string? ParameterSize { get; set; }

    /// <summary>
    /// Recommended quantization profile (e.g., "Q4_K_M")
    /// </summary>
    public string? RecommendedQuantization { get; set; }

    /// <summary>
    /// Approximate VRAM usage string (e.g., "~6GB")
    /// </summary>
    public string? VramUsage { get; set; }

    /// <summary>
    /// Key strengths summary
    /// </summary>
    public string? Strengths { get; set; }

    /// <summary>
    /// Known weaknesses summary
    /// </summary>
    public string? Weaknesses { get; set; }

    /// <summary>
    /// Typical generation speed description
    /// </summary>
    public string? TypicalSpeed { get; set; }

    /// <summary>
    /// Local path to the model (if downloaded)
    /// </summary>
    public string? LocalPath { get; set; }

    /// <summary>
    /// Quantization setting (e.g., "4bit", "8bit", "none")
    /// </summary>
    public string? Quantization { get; set; }

    /// <summary>
    /// Maximum context length
    /// </summary>
    public int MaxContextLength { get; set; } = 2048;

    /// <summary>
    /// Whether to use GPU acceleration
    /// </summary>
    public bool UseGPU { get; set; }

    /// <summary>
    /// Preferred backend to use for GPU execution
    /// </summary>
    public Backend Backend { get; set; } = Backend.Cuda;

    /// <summary>
    /// GPU device index (0 for first GPU, 1 for second, etc.)
    /// </summary>
    public int GpuDeviceIndex { get; set; } = 0;

    /// <summary>
    /// Maximum GPU memory to use in GB (null for unlimited)
    /// </summary>
    public float? MaxGpuMemoryGB { get; set; }

    /// <summary>
    /// GPU memory required in GB (informational)
    /// </summary>
    public string? GpuMemoryRequired { get; set; }

    /// <summary>
    /// Whether this model can be fine-tuned (non-GGUF PyTorch models)
    /// </summary>
    public bool CanFineTune { get; set; }

    /// <summary>
    /// The original transformers model ID to use for fine-tuning (may differ from HuggingFaceId for GGUF models)
    /// </summary>
    public string? FineTuneModelId { get; set; }

    /// <summary>
    /// Additional provider-specific settings
    /// </summary>
    public Dictionary<string, object>? AdditionalSettings { get; set; }
}

/// <summary>
/// GPU backend selection
/// </summary>
public enum Backend
{
    Cuda = 0,
    Rocm = 1,
    Vulkan = 2,
    Cpu = 3
}
