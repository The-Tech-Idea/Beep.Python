using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Beep.Python.RuntimeEngine.Infrastructure;

/// <summary>
/// Interface for managing model downloads and caching
/// </summary>
public interface IModelManager
{
    /// <summary>
    /// Get the path to the model cache directory
    /// </summary>
    string ModelCachePath { get; }

    /// <summary>
    /// Download a model from Hugging Face
    /// </summary>
    /// <param name="modelId">Hugging Face model ID</param>
    /// <param name="progress">Progress callback</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Path to the downloaded model</returns>
    Task<string> DownloadModel(string modelId, IProgress<DownloadProgress>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a model is already downloaded
    /// </summary>
    /// <param name="modelId">Model ID to check</param>
    /// <returns>True if model is cached locally</returns>
    Task<bool> IsModelDownloaded(string modelId);

    /// <summary>
    /// Get the local path for a model
    /// </summary>
    /// <param name="modelId">Model ID</param>
    /// <returns>Local path or null if not downloaded</returns>
    Task<string?> GetModelPath(string modelId);

    /// <summary>
    /// Delete a model from the cache
    /// </summary>
    /// <param name="modelId">Model ID to delete</param>
    Task<bool> DeleteModel(string modelId);

    /// <summary>
    /// Get all downloaded models
    /// </summary>
    /// <returns>List of downloaded model IDs</returns>
    Task<IEnumerable<string>> GetDownloadedModels();

    /// <summary>
    /// Verify model integrity
    /// </summary>
    /// <param name="modelId">Model ID to verify</param>
    /// <returns>True if model is valid</returns>
    Task<bool> VerifyModel(string modelId);

    /// <summary>
    /// Get the size of a model in bytes
    /// </summary>
    /// <param name="modelId">Model ID</param>
    /// <returns>Size in bytes or null if unknown</returns>
    Task<long?> GetModelSize(string modelId);

    /// <summary>
    /// Clear the entire model cache
    /// </summary>
    Task ClearCache();

    /// <summary>
    /// Unregister and remove all finetuned models that were registered in models.json
    /// Returns the number of removed entries
    /// </summary>
    Task<int> UnregisterFinetunedModels();

    /// <summary>
    /// Get all available model IDs from models.json
    /// </summary>
    /// <returns>List of available model IDs</returns>
    List<string> GetAvailableModels();

    /// <summary>
    /// Get the HuggingFace model ID for a given model ID
    /// </summary>
    /// <param name="modelId">Short model ID (e.g., "dialogpt-small")</param>
    /// <returns>HuggingFace model ID (e.g., "microsoft/DialoGPT-small") or null if not found</returns>
    string? GetHuggingFaceId(string modelId);

    /// <summary>
    /// Get the provider name for a given model ID
    /// </summary>
    /// <param name="modelId">Short model ID (e.g., "dialogpt-small")</param>
    /// <returns>Provider name (e.g., "dialogpt") or null if not found</returns>
    string? GetProviderName(string modelId);

    /// <summary>
    /// Get full model configuration including GPU settings from models.json
    /// </summary>
    /// <param name="modelId">Model ID</param>
    /// <returns>Model configuration or null if not found</returns>
    ModelConfig? GetModelConfig(string modelId);

    /// <summary>
    /// Get all model configurations from models.json
    /// </summary>
    /// <returns>List of all model configurations</returns>
    List<ModelConfig> GetAllModels();

    /// <summary>
    /// Register a finetuned model in models.json and copy to cache
    /// </summary>
    /// <param name="fineTunedModelPath">Path to the finetuned model directory</param>
    /// <param name="modelId">New model ID to register</param>
    /// <param name="baseModelId">Original model ID this was finetuned from</param>
    /// <param name="description">Description of the finetuned model</param>
    /// <returns>True if registration succeeded</returns>
    Task<bool> RegisterFinetunedModel(string fineTunedModelPath, string modelId, string baseModelId, string? description = null);
}
