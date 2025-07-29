using System;
using System.Collections.Generic;

namespace Beep.Python.Model
{
    #region Configuration Classes

    /// <summary>
    /// Configuration for transformer pipeline
    /// </summary>
    public class TransformerPipelineConfig
    {
        /// <summary>
        /// Model name or identifier
        /// </summary>
        public string ModelName { get; set; }

        /// <summary>
        /// Model source type
        /// </summary>
        public TransformerModelSource ModelSource { get; set; }

        /// <summary>
        /// Task type for the pipeline
        /// </summary>
        public TransformerTask TaskType { get; set; }

        /// <summary>
        /// Device to use for inference
        /// </summary>
        public TransformerDevice Device { get; set; } = TransformerDevice.Auto;

        /// <summary>
        /// Model precision
        /// </summary>
        public ModelPrecision Precision { get; set; } = ModelPrecision.Auto;

        /// <summary>
        /// Maximum input length for tokenization
        /// </summary>
        public int MaxInputLength { get; set; } = 512;

        /// <summary>
        /// Batch size for inference
        /// </summary>
        public int BatchSize { get; set; } = 1;

        /// <summary>
        /// Whether to use caching
        /// </summary>
        public bool UseCache { get; set; } = true;

        /// <summary>
        /// Custom model configuration parameters
        /// </summary>
        public Dictionary<string, object> CustomConfig { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Authentication token (for private models)
        /// </summary>
        public string AuthToken { get; set; }

        /// <summary>
        /// Local model path (for local models)
        /// </summary>
        public string LocalModelPath { get; set; }

        /// <summary>
        /// Custom model URL (for custom sources)
        /// </summary>
        public string CustomModelUrl { get; set; }

        /// <summary>
        /// Trust remote code (for HuggingFace models)
        /// </summary>
        public bool TrustRemoteCode { get; set; } = false;

        /// <summary>
        /// Revision/branch of the model to use
        /// </summary>
        public string Revision { get; set; } = "main";
    }

    #endregion

    #region Model Information Classes

    /// <summary>
    /// Information about a transformer model
    /// </summary>
    public class TransformerModelInfo
    {
        /// <summary>
        /// Model name or identifier
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Model display name
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Model description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Model source
        /// </summary>
        public TransformerModelSource Source { get; set; }

        /// <summary>
        /// Supported tasks
        /// </summary>
        public List<TransformerTask> SupportedTasks { get; set; } = new List<TransformerTask>();

        /// <summary>
        /// Model size in parameters (if known)
        /// </summary>
        public long? ParameterCount { get; set; }

        /// <summary>
        /// Model architecture
        /// </summary>
        public string Architecture { get; set; }

        /// <summary>
        /// Language support
        /// </summary>
        public List<string> Languages { get; set; } = new List<string>();

        /// <summary>
        /// Model license
        /// </summary>
        public string License { get; set; }

        /// <summary>
        /// Model URL/path
        /// </summary>
        public string ModelPath { get; set; }

        /// <summary>
        /// Model metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Model creation date
        /// </summary>
        public DateTime? CreatedDate { get; set; }

        /// <summary>
        /// Model version
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Model author/organization
        /// </summary>
        public string Author { get; set; }

        /// <summary>
        /// Whether model requires authentication
        /// </summary>
        public bool RequiresAuth { get; set; }

        /// <summary>
        /// Download count (for public models)
        /// </summary>
        public long? DownloadCount { get; set; }

        /// <summary>
        /// Model tags
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();
    }

    /// <summary>
    /// Result of model validation
    /// </summary>
    public class ModelValidationResult
    {
        /// <summary>
        /// Whether the model is valid
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Validation messages
        /// </summary>
        public List<string> Messages { get; set; } = new List<string>();

        /// <summary>
        /// Validation errors
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// Warnings about the model
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();

        /// <summary>
        /// Compatibility score (0-100)
        /// </summary>
        public int CompatibilityScore { get; set; }

        /// <summary>
        /// Recommended configuration changes
        /// </summary>
        public Dictionary<string, object> RecommendedConfig { get; set; } = new Dictionary<string, object>();
    }

    #endregion

    #region Result Classes

    /// <summary>
    /// Generic transformer result wrapper
    /// </summary>
    /// <typeparam name="T">Result data type</typeparam>
    public class TransformerResult<T>
    {
        /// <summary>
        /// Whether the operation was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Result data
        /// </summary>
        public T Data { get; set; }

        /// <summary>
        /// Error message if operation failed
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Execution time in milliseconds
        /// </summary>
        public long ExecutionTimeMs { get; set; }

        /// <summary>
        /// Model used for inference
        /// </summary>
        public string ModelName { get; set; }

        /// <summary>
        /// Task type performed
        /// </summary>
        public TransformerTask TaskType { get; set; }

        /// <summary>
        /// Additional metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Confidence score (if applicable)
        /// </summary>
        public double? ConfidenceScore { get; set; }

        /// <summary>
        /// Token usage information
        /// </summary>
        public TokenUsage TokenUsage { get; set; }
    }

    /// <summary>
    /// Classification result
    /// </summary>
    public class ClassificationResult
    {
        /// <summary>
        /// Predicted label
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// Confidence score
        /// </summary>
        public double Score { get; set; }

        /// <summary>
        /// All class predictions with scores
        /// </summary>
        public List<ClassPrediction> AllPredictions { get; set; } = new List<ClassPrediction>();
    }

    /// <summary>
    /// Individual class prediction
    /// </summary>
    public class ClassPrediction
    {
        /// <summary>
        /// Class label
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// Prediction score
        /// </summary>
        public double Score { get; set; }
    }

    /// <summary>
    /// Named entity result
    /// </summary>
    public class EntityResult
    {
        /// <summary>
        /// Entity text
        /// </summary>
        public string Entity { get; set; }

        /// <summary>
        /// Entity label/type
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// Confidence score
        /// </summary>
        public double Score { get; set; }

        /// <summary>
        /// Start position in text
        /// </summary>
        public int Start { get; set; }

        /// <summary>
        /// End position in text
        /// </summary>
        public int End { get; set; }

        /// <summary>
        /// Additional entity information
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Question answering result
    /// </summary>
    public class AnswerResult
    {
        /// <summary>
        /// Answer text
        /// </summary>
        public string Answer { get; set; }

        /// <summary>
        /// Confidence score
        /// </summary>
        public double Score { get; set; }

        /// <summary>
        /// Start position in context
        /// </summary>
        public int Start { get; set; }

        /// <summary>
        /// End position in context
        /// </summary>
        public int End { get; set; }

        /// <summary>
        /// Context used for answering
        /// </summary>
        public string Context { get; set; }

        /// <summary>
        /// Question that was asked
        /// </summary>
        public string Question { get; set; }
    }

    /// <summary>
    /// Token usage information
    /// </summary>
    public class TokenUsage
    {
        /// <summary>
        /// Number of input tokens
        /// </summary>
        public int InputTokens { get; set; }

        /// <summary>
        /// Number of output tokens
        /// </summary>
        public int OutputTokens { get; set; }

        /// <summary>
        /// Total tokens used
        /// </summary>
        public int TotalTokens => InputTokens + OutputTokens;

        /// <summary>
        /// Number of prompt tokens (alias for InputTokens for API compatibility)
        /// </summary>
        public int PromptTokens 
        { 
            get => InputTokens; 
            set => InputTokens = value; 
        }

        /// <summary>
        /// Number of completion tokens (alias for OutputTokens for API compatibility)
        /// </summary>
        public int CompletionTokens 
        { 
            get => OutputTokens; 
            set => OutputTokens = value; 
        }

        /// <summary>
        /// Estimated cost (if applicable)
        /// </summary>
        public decimal? EstimatedCost { get; set; }
    }

    #endregion

    #region Parameter Classes

    /// <summary>
    /// Base class for task parameters
    /// </summary>
    public abstract class TaskParametersBase
    {
        /// <summary>
        /// Maximum number of tokens to generate
        /// </summary>
        public int? MaxLength { get; set; }

        /// <summary>
        /// Temperature for sampling
        /// </summary>
        public double? Temperature { get; set; }

        /// <summary>
        /// Top-p sampling parameter
        /// </summary>
        public double? TopP { get; set; }

        /// <summary>
        /// Top-k sampling parameter
        /// </summary>
        public int? TopK { get; set; }

        /// <summary>
        /// Random seed for reproducibility
        /// </summary>
        public int? Seed { get; set; }

        /// <summary>
        /// Custom parameters
        /// </summary>
        public Dictionary<string, object> CustomParameters { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Parameters for text generation
    /// </summary>
    public class TextGenerationParameters : TaskParametersBase
    {
        /// <summary>
        /// Number of sequences to generate
        /// </summary>
        public int NumReturn { get; set; } = 1;

        /// <summary>
        /// Whether to return full text or just new tokens
        /// </summary>
        public bool ReturnFullText { get; set; } = true;

        /// <summary>
        /// Whether to clean up tokenization spaces
        /// </summary>
        public bool CleanUpTokenizationSpaces { get; set; } = true;

        /// <summary>
        /// Prefix to add to the prompt
        /// </summary>
        public string Prefix { get; set; }

        /// <summary>
        /// Stop sequences
        /// </summary>
        public List<string> StopSequences { get; set; } = new List<string>();

        /// <summary>
        /// Whether to use sampling
        /// </summary>
        public bool DoSample { get; set; } = true;

        /// <summary>
        /// Early stopping criteria
        /// </summary>
        public bool EarlyStopping { get; set; } = false;

        /// <summary>
        /// Repetition penalty
        /// </summary>
        public double? RepetitionPenalty { get; set; }

        /// <summary>
        /// Length penalty
        /// </summary>
        public double? LengthPenalty { get; set; }
    }

    /// <summary>
    /// Parameters for text classification
    /// </summary>
    public class ClassificationParameters : TaskParametersBase
    {
        /// <summary>
        /// Return all scores instead of just top prediction
        /// </summary>
        public bool ReturnAllScores { get; set; } = false;

        /// <summary>
        /// Function to apply to logits
        /// </summary>
        public string Function { get; set; } = "softmax";

        /// <summary>
        /// Custom labels for classification
        /// </summary>
        public List<string> CandidateLabels { get; set; } = new List<string>();
    }

    /// <summary>
    /// Parameters for Named Entity Recognition
    /// </summary>
    public class NERParameters : TaskParametersBase
    {
        /// <summary>
        /// Aggregation strategy for sub-word tokens
        /// </summary>
        public string AggregationStrategy { get; set; } = "simple";

        /// <summary>
        /// Whether to ignore labels
        /// </summary>
        public bool IgnoreLabels { get; set; } = false;

        /// <summary>
        /// Minimum score threshold
        /// </summary>
        public double? ScoreThreshold { get; set; }
    }

    /// <summary>
    /// Parameters for Question Answering
    /// </summary>
    public class QAParameters : TaskParametersBase
    {
        /// <summary>
        /// Number of best answers to return
        /// </summary>
        public int TopK { get; set; } = 1;

        /// <summary>
        /// Maximum answer length
        /// </summary>
        public int MaxAnswerLength { get; set; } = 15;

        /// <summary>
        /// Minimum score threshold
        /// </summary>
        public double? ScoreThreshold { get; set; }

        /// <summary>
        /// Handle impossible answers
        /// </summary>
        public bool HandleImpossibleAnswer { get; set; } = false;
    }

    /// <summary>
    /// Parameters for embedding generation
    /// </summary>
    public class EmbeddingParameters : TaskParametersBase
    {
        /// <summary>
        /// Whether to normalize embeddings
        /// </summary>
        public bool Normalize { get; set; } = true;

        /// <summary>
        /// Pooling strategy
        /// </summary>
        public string PoolingStrategy { get; set; } = "mean";

        /// <summary>
        /// Whether to return attention mask
        /// </summary>
        public bool ReturnAttentionMask { get; set; } = false;

        /// <summary>
        /// Input type for embedding (for API compatibility)
        /// </summary>
        public string? InputType { get; set; }

        /// <summary>
        /// Truncation strategy for long inputs
        /// </summary>
        public string? Truncate { get; set; }
    }

    /// <summary>
    /// Parameters for summarization
    /// </summary>
    public class SummarizationParameters : TaskParametersBase
    {
        /// <summary>
        /// Minimum length of summary
        /// </summary>
        public int? MinLength { get; set; }

        /// <summary>
        /// Maximum length of summary
        /// </summary>
        public new int? MaxLength { get; set; }

        /// <summary>
        /// Whether to clean up tokenization spaces
        /// </summary>
        public bool CleanUpTokenizationSpaces { get; set; } = true;

        /// <summary>
        /// Truncation strategy
        /// </summary>
        public string Truncation { get; set; } = "longest_first";
    }

    /// <summary>
    /// Parameters for translation
    /// </summary>
    public class TranslationParameters : TaskParametersBase
    {
        /// <summary>
        /// Source language (if not auto-detected)
        /// </summary>
        public string SourceLanguage { get; set; }

        /// <summary>
        /// Target language
        /// </summary>
        public string TargetLanguage { get; set; }

        /// <summary>
        /// Whether to clean up tokenization spaces
        /// </summary>
        public bool CleanUpTokenizationSpaces { get; set; } = true;

        /// <summary>
        /// Maximum length of translation
        /// </summary>
        public int? MaxLength { get; set; }

        /// <summary>
        /// Number of beams for beam search
        /// </summary>
        public int? NumBeams { get; set; }
    }

    #endregion

    #region Event Classes

    /// <summary>
    /// Base event arguments for transformer events
    /// </summary>
    public class TransformerEventArgs : EventArgs
    {
        /// <summary>
        /// Model name
        /// </summary>
        public string ModelName { get; set; }

        /// <summary>
        /// Task type
        /// </summary>
        public TransformerTask TaskType { get; set; }

        /// <summary>
        /// Timestamp of the event
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Additional event data
        /// </summary>
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Error event arguments
    /// </summary>
    public class TransformerErrorEventArgs : TransformerEventArgs
    {
        /// <summary>
        /// Error message
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Exception details
        /// </summary>
        public Exception Exception { get; set; }

        /// <summary>
        /// Error code
        /// </summary>
        public string ErrorCode { get; set; }
    }

    /// <summary>
    /// Progress event arguments
    /// </summary>
    public class TransformerProgressEventArgs : TransformerEventArgs
    {
        /// <summary>
        /// Progress percentage (0-100)
        /// </summary>
        public int ProgressPercentage { get; set; }

        /// <summary>
        /// Progress message
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Current step
        /// </summary>
        public int CurrentStep { get; set; }

        /// <summary>
        /// Total steps
        /// </summary>
        public int TotalSteps { get; set; }
    }

    #endregion
}