using System.Collections.Generic;
using System.Threading.Tasks;

namespace Beep.Python.RuntimeEngine.Infrastructure;

/// <summary>
/// Interface for managing application configuration
/// </summary>
public interface IConfigurationManager
{
    /// <summary>
    /// Get the default model ID
    /// </summary>
    string DefaultModel { get; }

    /// <summary>
    /// Get the model cache path
    /// </summary>
    string ModelCachePath { get; }

    /// <summary>
    /// Get the Python path
    /// </summary>
    string? PythonPath { get; }

    /// <summary>
    /// Get maximum memory in GB
    /// </summary>
    int MaxMemoryGB { get; }

    /// <summary>
    /// Whether to use GPU
    /// </summary>
    bool UseGPU { get; }
    /// <summary>
    /// Whether to auto-initialize the embedded Python runtime if the configured Python path does not exist
    /// </summary>
    bool AutoInitializeRuntimeIfMissing { get; }
    /// <summary>
    /// ROCm venv strategy: 'model', 'family', or 'single'
    /// </summary>
    string RocmVenvStrategy { get; }
    /// <summary>
    /// Whether to show download progress in the CLI
    /// </summary>
    bool EnableDownloadProgress { get; }

    /// <summary>
    /// Load configuration from file
    /// </summary>
    Task<bool> LoadConfiguration(string? configPath = null);

    /// <summary>
    /// Save current configuration to file
    /// </summary>
    Task<bool> SaveConfiguration(string? configPath = null);

    /// <summary>
    /// Get configuration value by key
    /// </summary>
    /// <typeparam name="T">Type of the value</typeparam>
    /// <param name="key">Configuration key</param>
    /// <returns>Configuration value or default</returns>
    T? GetValue<T>(string key);

    /// <summary>
    /// Set configuration value
    /// </summary>
    /// <typeparam name="T">Type of the value</typeparam>
    /// <param name="key">Configuration key</param>
    /// <param name="value">Value to set</param>
    void SetValue<T>(string key, T value);

    /// <summary>
    /// Get provider configuration
    /// </summary>
    /// <param name="providerName">Provider name</param>
    /// <returns>Provider configuration or null</returns>
    ProviderConfig? GetProviderConfig(string providerName);

    /// <summary>
    /// Get all available models from configuration
    /// </summary>
    /// <returns>List of model configurations</returns>
    IEnumerable<ModelConfig> GetAvailableModels();

    /// <summary>
    /// Get specific model configuration
    /// </summary>
    /// <param name="modelId">Model ID</param>
    /// <returns>Model configuration or null</returns>
    ModelConfig? GetModelConfig(string modelId);

    /// <summary>
    /// Reset configuration to defaults
    /// </summary>
    Task ResetToDefaults();
}
