using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Beep.Python.Model
{
    /// <summary>
    /// General interface for transformer pipelines supporting various sources (HuggingFace, local, custom)
    /// </summary>
    public interface ITransformerPipeLine : IDisposable
    {
        #region Properties
        
        /// <summary>
        /// Indicates if the pipeline is initialized and ready for use
        /// </summary>
        bool IsInitialized { get; }
        
        /// <summary>
        /// Indicates if a model is currently loaded
        /// </summary>
        bool IsModelLoaded { get; }
        
        /// <summary>
        /// The name/identifier of the currently loaded model
        /// </summary>
        string ModelName { get; }
        
        /// <summary>
        /// The source of the model (HuggingFace, Local, Custom, etc.)
        /// </summary>
        TransformerModelSource ModelSource { get; }
        
        /// <summary>
        /// The type of transformer task this pipeline handles
        /// </summary>
        TransformerTask TaskType { get; }
        
        /// <summary>
        /// Device being used for inference (CPU, GPU, etc.)
        /// </summary>
        string Device { get; }
        
        /// <summary>
        /// Model configuration parameters
        /// </summary>
        Dictionary<string, object> ModelConfig { get; }
        
        /// <summary>
        /// Pipeline-specific configuration
        /// </summary>
        TransformerPipelineConfig PipelineConfig { get; set; }
        
        #endregion

        #region Model Management
        
        /// <summary>
        /// Initialize the pipeline with specified configuration
        /// </summary>
        /// <param name="config">Pipeline configuration</param>
        /// <returns>True if initialization successful</returns>
        Task<bool> InitializeAsync(TransformerPipelineConfig config);
        
        /// <summary>
        /// Load a model based on the model information and configuration
        /// Each implementation handles the specific source type (HuggingFace, Local, API, etc.)
        /// </summary>
        /// <param name="modelInfo">Model information including source, name, and path</param>
        /// <param name="taskType">Type of task</param>
        /// <param name="modelConfig">Optional model configuration</param>
        /// <returns>True if model loaded successfully</returns>
        Task<bool> LoadModelAsync(TransformerModelInfo modelInfo, TransformerTask taskType, Dictionary<string, object> modelConfig = null);
        
        /// <summary>
        /// Unload the currently loaded model and free resources
        /// </summary>
        void UnloadModel();
        
        #endregion

        #region Inference Methods
        
        /// <summary>
        /// Perform text generation
        /// </summary>
        /// <param name="prompt">Input prompt</param>
        /// <param name="parameters">Generation parameters</param>
        /// <returns>Generated text result</returns>
        Task<TransformerResult<string>> GenerateTextAsync(string prompt, TextGenerationParameters parameters = null);
        
        /// <summary>
        /// Perform text classification
        /// </summary>
        /// <param name="text">Text to classify</param>
        /// <param name="parameters">Classification parameters</param>
        /// <returns>Classification result with scores</returns>
        Task<TransformerResult<ClassificationResult>> ClassifyTextAsync(string text, ClassificationParameters parameters = null);
        
        /// <summary>
        /// Perform named entity recognition
        /// </summary>
        /// <param name="text">Text to analyze</param>
        /// <param name="parameters">NER parameters</param>
        /// <returns>Entities found in text</returns>
        Task<TransformerResult<List<EntityResult>>> ExtractEntitiesAsync(string text, NERParameters parameters = null);
        
        /// <summary>
        /// Perform question answering
        /// </summary>
        /// <param name="question">Question to answer</param>
        /// <param name="context">Context text</param>
        /// <param name="parameters">QA parameters</param>
        /// <returns>Answer with confidence score</returns>
        Task<TransformerResult<AnswerResult>> AnswerQuestionAsync(string question, string context, QAParameters parameters = null);
        
        /// <summary>
        /// Generate text embeddings
        /// </summary>
        /// <param name="texts">Texts to embed</param>
        /// <param name="parameters">Embedding parameters</param>
        /// <returns>Text embeddings</returns>
        Task<TransformerResult<List<float[]>>> GetEmbeddingsAsync(List<string> texts, EmbeddingParameters parameters = null);
        
        /// <summary>
        /// Perform text summarization
        /// </summary>
        /// <param name="text">Text to summarize</param>
        /// <param name="parameters">Summarization parameters</param>
        /// <returns>Summary text</returns>
        Task<TransformerResult<string>> SummarizeTextAsync(string text, SummarizationParameters parameters = null);
        
        /// <summary>
        /// Perform language translation
        /// </summary>
        /// <param name="text">Text to translate</param>
        /// <param name="targetLanguage">Target language code</param>
        /// <param name="sourceLanguage">Source language code (optional for auto-detection)</param>
        /// <param name="parameters">Translation parameters</param>
        /// <returns>Translated text</returns>
        Task<TransformerResult<string>> TranslateTextAsync(string text, string targetLanguage, string sourceLanguage = null, TranslationParameters parameters = null);
        
        /// <summary>
        /// Perform batch inference for multiple inputs
        /// </summary>
        /// <param name="inputs">List of inputs</param>
        /// <param name="taskType">Type of task to perform</param>
        /// <param name="parameters">Task-specific parameters</param>
        /// <returns>List of results</returns>
        Task<List<TransformerResult<object>>> BatchInferenceAsync(List<string> inputs, TransformerTask taskType, object parameters = null);
        
        /// <summary>
        /// Generic inference method for custom tasks
        /// </summary>
        /// <param name="inputs">Input data</param>
        /// <param name="parameters">Task parameters</param>
        /// <returns>Raw inference result</returns>
        Task<TransformerResult<object>> InferenceAsync(object inputs, Dictionary<string, object> parameters = null);
        
        #endregion

        #region Configuration and Utilities
        
        /// <summary>
        /// Get available models for a specific task from the configured source
        /// </summary>
        /// <param name="taskType">Type of task</param>
        /// <param name="source">Model source to search</param>
        /// <returns>List of available models</returns>
        Task<List<TransformerModelInfo>> GetAvailableModelsAsync(TransformerTask taskType, TransformerModelSource source = TransformerModelSource.HuggingFace);
        
        /// <summary>
        /// Validate model compatibility with the pipeline
        /// </summary>
        /// <param name="modelInfo">Model information to validate</param>
        /// <returns>Validation result with details</returns>
        Task<ModelValidationResult> ValidateModelAsync(TransformerModelInfo modelInfo);
        
        /// <summary>
        /// Get model information and metadata
        /// </summary>
        /// <returns>Current model information</returns>
        TransformerModelInfo GetModelInfo();
        
        /// <summary>
        /// Update pipeline configuration
        /// </summary>
        /// <param name="config">New configuration</param>
        /// <returns>True if update successful</returns>
        bool UpdateConfiguration(TransformerPipelineConfig config);
        
        /// <summary>
        /// Get supported task types for current model
        /// </summary>
        /// <returns>List of supported tasks</returns>
        List<TransformerTask> GetSupportedTasks();
        
        /// <summary>
        /// Warm up the model (useful for reducing first inference latency)
        /// </summary>
        /// <param name="sampleInput">Sample input for warming up</param>
        /// <returns>True if warmup successful</returns>
        Task<bool> WarmUpAsync(string sampleInput = null);
        
        #endregion

        #region Events
        
        /// <summary>
        /// Event fired when model loading starts
        /// </summary>
        event EventHandler<TransformerEventArgs> ModelLoadingStarted;
        
        /// <summary>
        /// Event fired when model loading completes
        /// </summary>
        event EventHandler<TransformerEventArgs> ModelLoadingCompleted;
        
        /// <summary>
        /// Event fired when inference starts
        /// </summary>
        event EventHandler<TransformerEventArgs> InferenceStarted;
        
        /// <summary>
        /// Event fired when inference completes
        /// </summary>
        event EventHandler<TransformerEventArgs> InferenceCompleted;
        
        /// <summary>
        /// Event fired when an error occurs
        /// </summary>
        event EventHandler<TransformerErrorEventArgs> ErrorOccurred;
        
        /// <summary>
        /// Event fired for progress updates during long operations
        /// </summary>
        event EventHandler<TransformerProgressEventArgs> ProgressUpdated;
        
        #endregion
    }
}
