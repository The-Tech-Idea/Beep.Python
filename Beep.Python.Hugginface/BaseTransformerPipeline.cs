using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;
using Beep.Python.Model;

namespace Beep.Python.AI.Transformers
{
    /// <summary>
    /// Abstract base class for all transformer pipeline implementations
    /// Provides common functionality and enforces consistent structure across providers
    /// </summary>
    public abstract class BaseTransformerPipeline : ITransformerPipeLine
    {
        #region Protected Fields
        
        protected bool _isInitialized;
        protected bool _isModelLoaded;
        protected string? _modelName;
        protected TransformerModelSource _modelSource;
        protected TransformerTask _taskType;
        protected string? _device;
        protected Dictionary<string, object> _modelConfig;
        protected TransformerPipelineConfig? _pipelineConfig;
        protected bool _disposed;

        // Python runtime dependencies
        protected readonly IPythonRunTimeManager _pythonRunTimeManager;
        protected readonly IPythonCodeExecuteManager _executeManager;

        #endregion

        #region Properties

        public bool IsInitialized => _isInitialized;
        public bool IsModelLoaded => _isModelLoaded;
        public string ModelName => _modelName ?? string.Empty;
        public TransformerModelSource ModelSource => _modelSource;
        public TransformerTask TaskType => _taskType;
        public string Device => _device ?? "auto";
        public Dictionary<string, object> ModelConfig => _modelConfig;
        public TransformerPipelineConfig? PipelineConfig
        {
            get => _pipelineConfig;
            set => _pipelineConfig = value;
        }

        #endregion

        #region Events

        public event EventHandler<TransformerEventArgs>? ModelLoadingStarted;
        public event EventHandler<TransformerEventArgs>? ModelLoadingCompleted;
        public event EventHandler<TransformerEventArgs>? InferenceStarted;
        public event EventHandler<TransformerEventArgs>? InferenceCompleted;
        public event EventHandler<TransformerErrorEventArgs>? ErrorOccurred;
        public event EventHandler<TransformerProgressEventArgs>? ProgressUpdated;

        #endregion

        #region Constructor

        /// <summary>
        /// Initialize base transformer pipeline
        /// </summary>
        /// <param name="pythonRunTimeManager">Python runtime manager</param>
        /// <param name="executeManager">Python code execution manager</param>
        protected BaseTransformerPipeline(IPythonRunTimeManager pythonRunTimeManager, IPythonCodeExecuteManager executeManager)
        {
            _pythonRunTimeManager = pythonRunTimeManager ?? throw new ArgumentNullException(nameof(pythonRunTimeManager));
            _executeManager = executeManager ?? throw new ArgumentNullException(nameof(executeManager));
            _modelConfig = new Dictionary<string, object>();
            _modelSource = TransformerModelSource.HuggingFace;
            _taskType = TransformerTask.Custom;
        }

        #endregion

        #region Abstract Methods - Must be implemented by derived classes

        /// <summary>
        /// Initialize the pipeline with provider-specific configuration
        /// </summary>
        /// <param name="config">Pipeline configuration</param>
        /// <returns>True if initialization successful</returns>
        public abstract Task<bool> InitializeAsync(TransformerPipelineConfig config);

        /// <summary>
        /// Load a model based on the model information and configuration
        /// Each implementation handles the specific source type (HuggingFace, Local, API, etc.)
        /// </summary>
        /// <param name="modelInfo">Model information including source, name, and path</param>
        /// <param name="taskType">Type of task</param>
        /// <param name="modelConfig">Model configuration</param>
        /// <returns>True if model loaded successfully</returns>
        public abstract Task<bool> LoadModelAsync(TransformerModelInfo modelInfo, TransformerTask taskType, Dictionary<string, object>? modelConfig = null);

        /// <summary>
        /// Perform text generation (provider-specific implementation)
        /// </summary>
        /// <param name="prompt">Input prompt</param>
        /// <param name="parameters">Generation parameters</param>
        /// <returns>Generated text result</returns>
        public abstract Task<TransformerResult<string>> GenerateTextAsync(string prompt, TextGenerationParameters? parameters = null);

        #endregion

        #region Virtual Methods - Can be overridden by derived classes

        /// <summary>
        /// Unload the currently loaded model and free resources
        /// </summary>
        public virtual void UnloadModel()
        {
            if (_isModelLoaded)
            {
                _isModelLoaded = false;
                _modelName = null;
                _modelSource = TransformerModelSource.HuggingFace;
                _taskType = TransformerTask.Custom;
                _modelConfig.Clear();
            }
        }

        /// <summary>
        /// Perform text classification (base implementation)
        /// </summary>
        /// <param name="text">Text to classify</param>
        /// <param name="parameters">Classification parameters</param>
        /// <returns>Classification result with scores</returns>
        public virtual async Task<TransformerResult<ClassificationResult>> ClassifyTextAsync(string text, ClassificationParameters? parameters = null)
        {
            return await ExecuteInferenceAsync<ClassificationResult>("text_classification", text, parameters);
        }

        /// <summary>
        /// Perform named entity recognition (base implementation)
        /// </summary>
        /// <param name="text">Text to analyze</param>
        /// <param name="parameters">NER parameters</param>
        /// <returns>Entities found in text</returns>
        public virtual async Task<TransformerResult<List<EntityResult>>> ExtractEntitiesAsync(string text, NERParameters? parameters = null)
        {
            return await ExecuteInferenceAsync<List<EntityResult>>("ner", text, parameters);
        }

        /// <summary>
        /// Perform question answering (base implementation)
        /// </summary>
        /// <param name="question">Question to answer</param>
        /// <param name="context">Context text</param>
        /// <param name="parameters">QA parameters</param>
        /// <returns>Answer with confidence score</returns>
        public virtual async Task<TransformerResult<AnswerResult>> AnswerQuestionAsync(string question, string context, QAParameters? parameters = null)
        {
            var input = new { question, context };
            return await ExecuteInferenceAsync<AnswerResult>("question_answering", input, parameters);
        }

        /// <summary>
        /// Generate text embeddings (base implementation)
        /// </summary>
        /// <param name="texts">Texts to embed</param>
        /// <param name="parameters">Embedding parameters</param>
        /// <returns>Text embeddings</returns>
        public virtual async Task<TransformerResult<List<float[]>>> GetEmbeddingsAsync(List<string> texts, EmbeddingParameters? parameters = null)
        {
            return await ExecuteInferenceAsync<List<float[]>>("feature_extraction", texts, parameters);
        }

        /// <summary>
        /// Perform text summarization (base implementation)
        /// </summary>
        /// <param name="text">Text to summarize</param>
        /// <param name="parameters">Summarization parameters</param>
        /// <returns>Summary text</returns>
        public virtual async Task<TransformerResult<string>> SummarizeTextAsync(string text, SummarizationParameters? parameters = null)
        {
            return await ExecuteInferenceAsync<string>("summarization", text, parameters);
        }

        /// <summary>
        /// Perform language translation (base implementation)
        /// </summary>
        /// <param name="text">Text to translate</param>
        /// <param name="targetLanguage">Target language code</param>
        /// <param name="sourceLanguage">Source language code (optional for auto-detection)</param>
        /// <param name="parameters">Translation parameters</param>
        /// <returns>Translated text</returns>
        public virtual async Task<TransformerResult<string>> TranslateTextAsync(string text, string targetLanguage, string? sourceLanguage = null, TranslationParameters? parameters = null)
        {
            var input = new { text, target_lang = targetLanguage, source_lang = sourceLanguage };
            return await ExecuteInferenceAsync<string>("translation", input, parameters);
        }

        /// <summary>
        /// Perform batch inference for multiple inputs (base implementation)
        /// </summary>
        /// <param name="inputs">List of inputs</param>
        /// <param name="taskType">Type of task to perform</param>
        /// <param name="parameters">Task-specific parameters</param>
        /// <returns>List of results</returns>
        public virtual async Task<List<TransformerResult<object>>> BatchInferenceAsync(List<string> inputs, TransformerTask taskType, object? parameters = null)
        {
            var results = new List<TransformerResult<object>>();
            
            foreach (var input in inputs)
            {
                var result = await InferenceAsync(input, parameters as Dictionary<string, object>);
                results.Add(result);
            }
            
            return results;
        }

        /// <summary>
        /// Generic inference method for custom tasks (base implementation)
        /// </summary>
        /// <param name="inputs">Input data</param>
        /// <param name="parameters">Task parameters</param>
        /// <returns>Raw inference result</returns>
        public virtual async Task<TransformerResult<object>> InferenceAsync(object inputs, Dictionary<string, object>? parameters = null)
        {
            return await ExecuteInferenceAsync<object>("generic", inputs, parameters);
        }

        /// <summary>
        /// Get available models for a specific task from the configured source (base implementation)
        /// </summary>
        /// <param name="taskType">Type of task</param>
        /// <param name="source">Model source to search</param>
        /// <returns>List of available models</returns>
        public virtual async Task<List<TransformerModelInfo>> GetAvailableModelsAsync(TransformerTask taskType, TransformerModelSource source = TransformerModelSource.HuggingFace)
        {
            // Default implementation returns empty list - providers should override
            await Task.CompletedTask;
            return new List<TransformerModelInfo>();
        }

        /// <summary>
        /// Validate model compatibility with the pipeline (base implementation)
        /// </summary>
        /// <param name="modelInfo">Model information to validate</param>
        /// <returns>Validation result with details</returns>
        public virtual async Task<ModelValidationResult> ValidateModelAsync(TransformerModelInfo modelInfo)
        {
            try
            {
                await Task.CompletedTask;
                // Basic validation
                var result = new ModelValidationResult
                {
                    IsValid = true,
                    CompatibilityScore = 100
                };

                // Providers can override for specific validation logic
                
                return result;
            }
            catch (Exception ex)
            {
                return new ModelValidationResult
                {
                    IsValid = false,
                    Errors = { ex.Message },
                    CompatibilityScore = 0
                };
            }
        }

        /// <summary>
        /// Get model information and metadata (base implementation)
        /// </summary>
        /// <returns>Current model information</returns>
        public virtual TransformerModelInfo? GetModelInfo()
        {
            if (!_isModelLoaded)
                return null;

            return new TransformerModelInfo
            {
                Name = _modelName ?? string.Empty,
                Source = _modelSource,
                SupportedTasks = GetSupportedTasks(),
                Metadata = new Dictionary<string, object>(_modelConfig)
            };
        }

        /// <summary>
        /// Update pipeline configuration (base implementation)
        /// </summary>
        /// <param name="config">New configuration</param>
        /// <returns>True if update successful</returns>
        public virtual bool UpdateConfiguration(TransformerPipelineConfig config)
        {
            try
            {
                _pipelineConfig = config;
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get supported task types for current model (base implementation)
        /// </summary>
        /// <returns>List of supported tasks</returns>
        public virtual List<TransformerTask> GetSupportedTasks()
        {
            // Default implementation - providers should override
            return new List<TransformerTask> { _taskType };
        }

        /// <summary>
        /// Warm up the model (base implementation)
        /// </summary>
        /// <param name="sampleInput">Sample input for warming up</param>
        /// <returns>True if warmup successful</returns>
        public virtual async Task<bool> WarmUpAsync(string? sampleInput = null)
        {
            try
            {
                if (!_isModelLoaded)
                    return false;

                sampleInput ??= "This is a warmup input.";
                
                // Execute a simple inference to warm up the model
                switch (_taskType)
                {
                    case TransformerTask.TextGeneration:
                        await GenerateTextAsync(sampleInput);
                        break;
                    case TransformerTask.TextClassification:
                        await ClassifyTextAsync(sampleInput);
                        break;
                    default:
                        await InferenceAsync(sampleInput, null);
                        break;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Session and Environment Management

        /// <summary>
        /// Configure the pipeline to use a specific Python session and virtual environment
        /// This is the recommended approach for multi-user environments
        /// </summary>
        /// <param name="session">Pre-existing Python session to use for execution</param>
        /// <param name="virtualEnvironment">Virtual environment associated with the session</param>
        /// <returns>True if configuration successful</returns>
        public virtual bool ConfigureSession(PythonSessionInfo session, PythonVirtualEnvironment virtualEnvironment)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));
            
            if (virtualEnvironment == null)
                throw new ArgumentNullException(nameof(virtualEnvironment));

            // Validate that session is associated with the environment
            if (session.VirtualEnvironmentId != virtualEnvironment.ID)
            {
                throw new ArgumentException("Session must be associated with the provided virtual environment");
            }

            // Validate session is active
            if (session.Status != PythonSessionStatus.Active)
            {
                throw new ArgumentException("Session must be in Active status");
            }

            // Store session information in model config for use by execution methods
            _modelConfig["__session"] = session;
            _modelConfig["__virtual_environment"] = virtualEnvironment;
            _modelConfig["__user"] = session.Username;
            _modelConfig["__session_id"] = session.SessionId;

            return true;
        }

        /// <summary>
        /// Get the currently configured session, if any
        /// </summary>
        /// <returns>The configured Python session, or null if not configured</returns>
        public virtual PythonSessionInfo? GetConfiguredSession()
        {
            return _modelConfig.TryGetValue("__session", out var session) ? session as PythonSessionInfo : null;
        }

        /// <summary>
        /// Get the currently configured virtual environment, if any
        /// </summary>
        /// <returns>The configured virtual environment, or null if not configured</returns>
        public virtual PythonVirtualEnvironment? GetConfiguredVirtualEnvironment()
        {
            return _modelConfig.TryGetValue("__virtual_environment", out var env) ? env as PythonVirtualEnvironment : null;
        }

        #endregion

        #region Connection Configuration

        /// <summary>
        /// Connection configuration for the transformer service provider
        /// </summary>
        protected TransformerConnectionConfig? _connectionConfig;

        /// <summary>
        /// Connection manager for managing provider connections
        /// </summary>
        protected static readonly TransformerConnectionManager _connectionManager = new();

        /// <summary>
        /// Configure connection for the transformer service provider
        /// </summary>
        /// <param name="connectionConfig">Connection configuration</param>
        /// <returns>True if configuration successful</returns>
        public virtual bool ConfigureConnection(TransformerConnectionConfig connectionConfig)
        {
            if (connectionConfig == null)
                throw new ArgumentNullException(nameof(connectionConfig));

            // Validate connection configuration
            var validation = ValidateConnectionConfig(connectionConfig);
            if (!validation.IsValid)
            {
                throw new ArgumentException($"Invalid connection configuration: {string.Join(", ", validation.Errors)}");
            }

            _connectionConfig = connectionConfig;
            
            // Register with global connection manager
            _connectionManager.RegisterConnection(_modelSource, connectionConfig);

            // Store connection info in model config for Python code generation
            _modelConfig["__connection_config"] = connectionConfig;
            _modelConfig["__provider_name"] = connectionConfig.ProviderName;

            return true;
        }

        /// <summary>
        /// Get the currently configured connection
        /// </summary>
        /// <returns>Connection configuration or null if not configured</returns>
        public virtual TransformerConnectionConfig? GetConnectionConfig()
        {
            return _connectionConfig;
        }

        /// <summary>
        /// Check if connection is configured and valid
        /// </summary>
        /// <returns>True if connection is properly configured</returns>
        public virtual bool IsConnectionConfigured()
        {
            if (_connectionConfig == null) return false;
            
            var validation = ValidateConnectionConfig(_connectionConfig);
            return validation.IsValid;
        }

        /// <summary>
        /// Validate connection configuration for the specific provider
        /// </summary>
        /// <param name="config">Connection configuration to validate</param>
        /// <returns>Validation result</returns>
        protected virtual ConnectionValidationResult ValidateConnectionConfig(TransformerConnectionConfig config)
        {
            var result = new ConnectionValidationResult { IsValid = true };

            // Basic validation
            if (config.TimeoutSeconds <= 0)
                result.Errors.Add("Timeout must be greater than 0");
            
            if (config.MaxRetries < 0)
                result.Errors.Add("Max retries cannot be negative");

            // Provider-specific validation will be handled by derived classes
            return result;
        }

        /// <summary>
        /// Generate authentication headers for API calls
        /// </summary>
        /// <returns>Dictionary of authentication headers</returns>
        protected virtual Dictionary<string, string> GetAuthenticationHeaders()
        {
            var headers = new Dictionary<string, string>();

            if (_connectionConfig == null) return headers;

            // Add custom headers
            foreach (var header in _connectionConfig.CustomHeaders)
            {
                headers[header.Key] = header.Value;
            }

            // Provider-specific authentication headers will be added by derived classes
            return headers;
        }

        /// <summary>
        /// Get API endpoint URL for the provider
        /// </summary>
        /// <returns>API endpoint URL</returns>
        protected virtual string GetApiEndpoint()
        {
            return _connectionConfig switch
            {
                OpenAIConnectionConfig openAI => openAI.ApiEndpoint,
                AzureOpenAIConnectionConfig azure => azure.Endpoint,
                GoogleAIConnectionConfig google => google.Endpoint,
                AnthropicConnectionConfig anthropic => anthropic.ApiEndpoint,
                CohereConnectionConfig cohere => cohere.ApiEndpoint,
                MetaConnectionConfig meta => meta.ApiEndpoint,
                MistralConnectionConfig mistral => mistral.ApiEndpoint,
                HuggingFaceConnectionConfig hf => hf.InferenceEndpoint,
                CustomConnectionConfig custom => custom.ApiEndpoint,
                _ => string.Empty
            };
        }

        #endregion

        #region Protected Helper Methods

        /// <summary>
        /// Execute inference with common error handling and event management
        /// </summary>
        /// <typeparam name="T">Result type</typeparam>
        /// <param name="taskName">Task name</param>
        /// <param name="input">Input data</param>
        /// <param name="parameters">Parameters</param>
        /// <returns>Transformer result</returns>
        protected virtual async Task<TransformerResult<T>> ExecuteInferenceAsync<T>(string taskName, object input, object? parameters)
        {
            try
            {
                OnInferenceStarted(_modelName ?? string.Empty, _taskType);
                var startTime = DateTime.UtcNow;

                if (!_isModelLoaded)
                {
                    throw new InvalidOperationException("No model is loaded");
                }

                // Providers should override this for their specific implementation
                var result = await ExecuteProviderSpecificInferenceAsync<T>(taskName, input, parameters);
                
                var executionTime = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;

                var transformerResult = new TransformerResult<T>
                {
                    Success = result.Success,
                    ExecutionTimeMs = executionTime,
                    ModelName = _modelName ?? string.Empty,
                    TaskType = _taskType,
                    Data = result.Data,
                    ErrorMessage = result.ErrorMessage
                };

                OnInferenceCompleted(_modelName ?? string.Empty, _taskType);
                return transformerResult;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Inference failed for task {taskName}", ex);
                return new TransformerResult<T>
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    ModelName = _modelName ?? string.Empty,
                    TaskType = _taskType
                };
            }
        }

        /// <summary>
        /// Execute provider-specific inference logic (must be implemented by providers)
        /// </summary>
        /// <typeparam name="T">Result type</typeparam>
        /// <param name="taskName">Task name</param>
        /// <param name="input">Input data</param>
        /// <param name="parameters">Parameters</param>
        /// <returns>Raw result</returns>
        protected virtual async Task<(bool Success, T Data, string? ErrorMessage)> ExecuteProviderSpecificInferenceAsync<T>(string taskName, object input, object? parameters)
        {
            // Default implementation - providers should override
            await Task.CompletedTask;
            throw new NotImplementedException("Provider must implement ExecuteProviderSpecificInferenceAsync");
        }

        /// <summary>
        /// Execute Python code asynchronously (shared functionality)
        /// Uses configured session if available, otherwise creates a temporary one
        /// </summary>
        /// <param name="pythonCode">Python code to execute</param>
        /// <param name="session">Python session to use (optional - overrides configured session)</param>
        /// <returns>Execution result</returns>
        protected async Task<(bool Success, string? Result, string? ErrorMessage)> ExecutePythonCodeAsync(string pythonCode, PythonSessionInfo? session = null)
        {
            try
            {
                // Use provided session, then configured session, then create temporary one
                session ??= GetConfiguredSession();
                
                if (session != null)
                {
                    // Use the pre-configured or provided session (recommended for multi-user)
                    var result = await _executeManager.ExecuteCodeAsync(pythonCode, session);
                    return (result.Success, result.Output, result.Success ? null : result.Output);
                }
                else
                {
                    // Fallback: Create a temporary session (not recommended for production)
                    // This should mainly be used for testing or single-user scenarios
                    var tempSession = CreateTemporarySession();
                    var result = await _executeManager.ExecuteCodeAsync(pythonCode, tempSession);
                    return (result.Success, result.Output, result.Success ? null : result.Output);
                }
            }
            catch (Exception ex)
            {
                return (false, null, ex.Message);
            }
        }

        /// <summary>
        /// Creates a temporary session for fallback scenarios
        /// This is not recommended for production multi-user environments
        /// </summary>
        /// <returns>Temporary Python session</returns>
        protected virtual PythonSessionInfo CreateTemporarySession()
        {
            var currentUser = System.Environment.UserName;
            
            return new PythonSessionInfo
            {
                SessionId = Guid.NewGuid().ToString(),
                Username = currentUser,
                SessionName = $"TransformerTemp_{currentUser}_{DateTime.Now.Ticks}",
                Status = PythonSessionStatus.Active,
                StartedAt = DateTime.Now
            };
        }

        /// <summary>
        /// Parse inference result from JSON (shared functionality)
        /// </summary>
        /// <typeparam name="T">Result type</typeparam>
        /// <param name="jsonResult">JSON result from Python</param>
        /// <returns>Parsed result</returns>
        protected (bool success, T? data, string? error) ParseInferenceResult<T>(string jsonResult)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(jsonResult))
                {
                    return (false, default(T), "Empty result received");
                }

                var result = JsonSerializer.Deserialize<T>(jsonResult);
                return (true, result, null);
            }
            catch (Exception ex)
            {
                return (false, default(T), $"Failed to parse result: {ex.Message}");
            }
        }

        /// <summary>
        /// Get HuggingFace task name from TransformerTask enum (shared functionality)
        /// </summary>
        /// <param name="taskType">Task type</param>
        /// <returns>HuggingFace task name</returns>
        protected string GetHuggingFaceTaskName(TransformerTask taskType)
        {
            return taskType switch
            {
                TransformerTask.TextGeneration => "text-generation",
                TransformerTask.TextClassification => "text-classification",
                TransformerTask.NamedEntityRecognition => "ner",
                TransformerTask.QuestionAnswering => "question-answering",
                TransformerTask.Summarization => "summarization",
                TransformerTask.Translation => "translation",
                TransformerTask.FeatureExtraction => "feature-extraction",
                TransformerTask.SentimentAnalysis => "sentiment-analysis",
                TransformerTask.ZeroShotClassification => "zero-shot-classification",
                TransformerTask.FillMask => "fill-mask",
                _ => "text-generation"
            };
        }

        /// <summary>
        /// Get device configuration string (shared functionality)
        /// </summary>
        /// <returns>Device configuration</returns>
        protected string GetDeviceConfig()
        {
            if (_pipelineConfig?.Device == TransformerDevice.CPU)
                return "-1";
            else if (_pipelineConfig?.Device == TransformerDevice.CUDA)
                return "0";
            else if (_pipelineConfig?.Device == TransformerDevice.MPS)
                return "mps";
            else
                return "auto";
        }

        /// <summary>
        /// Update model state after successful loading
        /// </summary>
        /// <param name="modelName">Model name</param>
        /// <param name="modelSource">Model source</param>
        /// <param name="taskType">Task type</param>
        /// <param name="modelConfig">Model configuration</param>
        protected virtual void UpdateModelState(string modelName, TransformerModelSource modelSource, TransformerTask taskType, Dictionary<string, object>? modelConfig)
        {
            _modelName = modelName;
            _modelSource = modelSource;
            _taskType = taskType;
            _modelConfig = modelConfig ?? new Dictionary<string, object>();
            _device = _pipelineConfig?.Device.ToString().ToLower() ?? "auto";
            _isModelLoaded = true;
        }

        #endregion

        #region Event Handlers

        protected virtual void OnModelLoadingStarted(string modelName, TransformerTask taskType)
        {
            ModelLoadingStarted?.Invoke(this, new TransformerEventArgs
            {
                ModelName = modelName,
                TaskType = taskType
            });
        }

        protected virtual void OnModelLoadingCompleted(string modelName, TransformerTask taskType)
        {
            ModelLoadingCompleted?.Invoke(this, new TransformerEventArgs
            {
                ModelName = modelName,
                TaskType = taskType
            });
        }

        protected virtual void OnInferenceStarted(string modelName, TransformerTask taskType)
        {
            InferenceStarted?.Invoke(this, new TransformerEventArgs
            {
                ModelName = modelName,
                TaskType = taskType
            });
        }

        protected virtual void OnInferenceCompleted(string modelName, TransformerTask taskType)
        {
            InferenceCompleted?.Invoke(this, new TransformerEventArgs
            {
                ModelName = modelName,
                TaskType = taskType
            });
        }

        protected virtual void OnErrorOccurred(string message, Exception exception)
        {
            ErrorOccurred?.Invoke(this, new TransformerErrorEventArgs
            {
                ErrorMessage = message,
                Exception = exception,
                ModelName = _modelName ?? string.Empty,
                TaskType = _taskType
            });
        }

        protected virtual void OnProgressUpdated(string message, int current, int total)
        {
            ProgressUpdated?.Invoke(this, new TransformerProgressEventArgs
            {
                Message = message,
                CurrentStep = current,
                TotalSteps = total,
                ProgressPercentage = total > 0 ? (current * 100) / total : 0,
                ModelName = _modelName ?? string.Empty,
                TaskType = _taskType
            });
        }

        #endregion

        #region Disposal

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    UnloadModel();
                }
                _disposed = true;
            }
        }

        #endregion
    }
}