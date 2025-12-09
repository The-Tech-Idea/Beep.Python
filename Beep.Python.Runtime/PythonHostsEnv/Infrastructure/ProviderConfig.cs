using System.Collections.Generic;

namespace Beep.Python.RuntimeEngine.Infrastructure;

/// <summary>
/// Configuration specific to provider settings
/// </summary>
public class ProviderConfig
{
    /// <summary>
    /// Provider name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Quantization setting (e.g., "4bit", "8bit", "none")
    /// </summary>
    public string? Quantization { get; set; }

    /// <summary>
    /// Maximum context length
    /// </summary>
    public int MaxContextLength { get; set; } = 2048;

    /// <summary>
    /// Whether to use GPU
    /// </summary>
    public bool UseGPU { get; set; }

    /// <summary>
    /// Device index for multi-GPU systems
    /// </summary>
    public int DeviceIndex { get; set; }

    /// <summary>
    /// Preferred backend for provider environments
    /// </summary>
    public Backend Backend { get; set; } = Backend.Cuda;

    /// <summary>
    /// Provider-specific settings
    /// </summary>
    public Dictionary<string, object>? Settings { get; set; }
}
