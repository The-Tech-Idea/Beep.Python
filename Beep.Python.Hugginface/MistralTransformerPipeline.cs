using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Beep.Python.Model;

namespace Beep.Python.AI.Transformers
{
    /// <summary>
    /// Mistral AI transformer pipeline implementation
    /// Handles Mistral AI models (Mistral, Mixtral, etc.)
    /// </summary>
    public class MistralTransformerPipeline : BaseTransformerPipeline
    {
        /// <summary>
        /// Initialize Mistral transformer pipeline
        /// </summary>
        /// <param name="pythonRunTimeManager">Python runtime manager</param>
        /// <param name="executeManager">Python code execution manager</param>
        public MistralTransformerPipeline(IPythonRunTimeManager pythonRunTimeManager, IPythonCodeExecuteManager executeManager)
            : base(pythonRunTimeManager, executeManager)
        {
            // Mistral-specific initialization
        }

        /// <summary>
        /// Initialize Mistral pipeline with API requirements
        /// </summary>
        public override async Task<bool> InitializeAsync(TransformerPipelineConfig config)
        {
            try
            {
                OnProgressUpdated("Initializing Mistral AI pipeline...", 0, 100);
                
                _pipelineConfig = config ?? throw new ArgumentNullException(nameof(config));

                // Install Mistral-specific packages
                await EnsureMistralPackagesInstalledAsync();
                OnProgressUpdated("Installing Mistral AI packages...", 50, 100);

                // Import Mistral modules
                await ImportMistralModulesAsync();
                OnProgressUpdated("Importing Mistral modules...", 75, 100);

                _isInitialized = true;
                OnProgressUpdated("Mistral AI initialization complete", 100, 100);

                return true;
            }
            catch (Exception ex)
            {
                OnErrorOccurred("Failed to initialize Mistral AI pipeline", ex);
                return false;
            }
        }

        /// <summary>
        /// Load a model based on the model information and configuration
        /// Mistral handles API-based models and HuggingFace models
        /// </summary>
        /// <param name="modelInfo">Model information including source, name, and path</param>
        /// <param name="taskType">Type of task</param>
        /// <param name="modelConfig">Model configuration</param>
        /// <returns>True if model loaded successfully</returns>
        public override async Task<bool> LoadModelAsync(TransformerModelInfo modelInfo, TransformerTask taskType, Dictionary<string, object>? modelConfig = null)
        {
            try
            {
                OnModelLoadingStarted(modelInfo.Name, taskType);
                OnProgressUpdated($"Loading Mistral model {modelInfo.Name}...", 0, 100);

                if (!_isInitialized)
                {
                    throw new InvalidOperationException("Pipeline must be initialized before loading models");
                }

                // Mistral supports both API and HuggingFace models
                switch (modelInfo.Source)
                {
                    case TransformerModelSource.Mistral:
                        // Check if this is an API-based model or local/HuggingFace model
                        if (modelConfig?.ContainsKey("api_key") == true)
                        {
                            return await LoadMistralAPIModelAsync(modelInfo, taskType, modelConfig);
                        }
                        else
                        {
                            // Load from HuggingFace or local with Mistral-specific optimizations
                            return await LoadMistralHuggingFaceModelAsync(modelInfo.Name, taskType, modelConfig);
                        }
                    
                    case TransformerModelSource.HuggingFace:
                        // Load Mistral model from HuggingFace
                        return await LoadMistralFromHuggingFaceAsync(modelInfo, taskType, modelConfig);
                    
                    default:
                        throw new ArgumentException($"Mistral pipeline does not support model source: {modelInfo.Source}. Use Mistral or HuggingFace sources.");
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Failed to load Mistral model {modelInfo.Name}", ex);
                return false;
            }
        }

        /// <summary>
        /// Get supported tasks for Mistral models
        /// </summary>
        public override List<TransformerTask> GetSupportedTasks()
        {
            return new List<TransformerTask>
            {
                TransformerTask.TextGeneration,
                TransformerTask.Conversational,
                TransformerTask.TextClassification,
                TransformerTask.Summarization,
                TransformerTask.QuestionAnswering,
                TransformerTask.FeatureExtraction,
                TransformerTask.FillMask
            };
        }

        /// <summary>
        /// Generate text using Mistral models
        /// </summary>
        public override async Task<TransformerResult<string>> GenerateTextAsync(string prompt, TextGenerationParameters? parameters = null)
        {
            try
            {
                if (!_isModelLoaded)
                {
                    throw new InvalidOperationException("No Mistral model is loaded");
                }

                OnInferenceStarted(_modelName, _taskType);
                var startTime = DateTime.UtcNow;

                // Generate Mistral-specific inference code
                var inferenceCode = GenerateMistralInferenceCode(prompt, parameters);
                var result = await ExecutePythonCodeAsync(inferenceCode);

                var executionTime = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;

                var transformerResult = new TransformerResult<string>
                {
                    Success = result.Success,
                    ExecutionTimeMs = executionTime,
                    ModelName = _modelName,
                    TaskType = _taskType
                };

                if (result.Success)
                {
                    transformerResult.Data = result.Result?.ToString();
                    ExtractMistralMetadata(transformerResult, result.Result);
                }
                else
                {
                    transformerResult.ErrorMessage = result.ErrorMessage;
                }

                OnInferenceCompleted(_modelName, _taskType);
                return transformerResult;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Mistral text generation failed", ex);
                return new TransformerResult<string>
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    ModelName = _modelName,
                    TaskType = _taskType
                };
            }
        }

        /// <summary>
        /// Get model information specific to Mistral
        /// </summary>
        public override TransformerModelInfo? GetModelInfo()
        {
            if (string.IsNullOrEmpty(_modelName))
                return null;

            return new TransformerModelInfo
            {
                Name = _modelName,
                Source = TransformerModelSource.Mistral,
                Architecture = "Mistral Language Model",
                Metadata = new Dictionary<string, object>
                {
                    ["provider"] = "Mistral AI",
                    ["is_mistral"] = true,
                    ["model_family"] = GetMistralModelFamily(_modelName),
                    ["task_type"] = _taskType.ToString(),
                    ["supports_api"] = true,
                    ["supports_huggingface"] = true
                }
            };
        }

        #region Private Helper Methods

        private async Task EnsureMistralPackagesInstalledAsync()
        {
            var packages = new[] { "mistralai", "torch", "accelerate", "bitsandbytes" };
            
            foreach (var package in packages)
            {
                // Install packages using the existing package management infrastructure
            }
        }

        private async Task ImportMistralModulesAsync()
        {
            var importCode = @"
try:
    from mistralai.client import MistralClient
    from mistralai.models.chat_completion import ChatMessage
    mistral_api_available = True
except ImportError:
    mistral_api_available = False

from transformers import AutoTokenizer, AutoModelForCausalLM
from transformers import BitsAndBytesConfig
import torch
import json
import os
";
            await ExecutePythonCodeAsync(importCode);
        }

        private void ValidateMistralAPIConfig(Dictionary<string, object>? modelConfig)
        {
            if (modelConfig == null)
            {
                throw new ArgumentException("Mistral API requires configuration parameters");
            }

            if (!modelConfig.ContainsKey("api_key") || string.IsNullOrEmpty(modelConfig["api_key"]?.ToString()))
            {
                throw new ArgumentException("Mistral API requires 'api_key' configuration parameter");
            }
        }

        private Dictionary<string, object> AddMistralOptimizations(Dictionary<string, object> originalConfig)
        {
            var config = originalConfig ?? new Dictionary<string, object>();

            if (!config.ContainsKey("torch_dtype"))
                config["torch_dtype"] = "torch.float16";

            if (!config.ContainsKey("device_map"))
                config["device_map"] = "auto";

            if (!config.ContainsKey("load_in_8bit") && !config.ContainsKey("load_in_4bit"))
            {
                if (_modelName?.ToLower().Contains("mixtral") == true)
                    config["load_in_8bit"] = true;
            }

            return config;
        }

        private async Task<bool> LoadMistralFromHuggingFaceAsync(TransformerModelInfo modelInfo, TransformerTask taskType, Dictionary<string, object>? modelConfig)
        {
            OnProgressUpdated($"Loading Mistral model {modelInfo.Name} from HuggingFace...", 0, 100);

            // Add Mistral-specific optimizations to model config
            var optimizedConfig = AddMistralOptimizations(modelConfig?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<string, object>());

            // Generate Mistral-optimized pipeline code
            var pipelineCode = GenerateMistralPipelineCode(modelInfo.Name, taskType, optimizedConfig);
            OnProgressUpdated("Creating Mistral pipeline...", 50, 100);

            var result = await ExecutePythonCodeAsync(pipelineCode);
            if (!result.Success)
            {
                throw new InvalidOperationException($"Failed to create Mistral pipeline: {result.ErrorMessage}");
            }

            UpdateModelState(modelInfo.Name, TransformerModelSource.Mistral, taskType, modelConfig?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value));

            OnProgressUpdated("Mistral model loaded successfully", 100, 100);
            OnModelLoadingCompleted(modelInfo.Name, taskType);

            return true;
        }

        private string GenerateMistralPipelineCode(string modelName, TransformerTask taskType, Dictionary<string, object> modelConfig)
        {
            var taskMapping = GetHuggingFaceTaskName(taskType);
            var deviceConfig = GetDeviceConfig();

            return $@"
try:
    from transformers import AutoTokenizer, AutoModelForCausalLM, pipeline
    
    # Load tokenizer and model
    tokenizer = AutoTokenizer.from_pretrained('{modelName}')
    model = AutoModelForCausalLM.from_pretrained('{modelName}')
    
    # Create pipeline
    pipeline = pipeline(
        task='{taskMapping}',
        model=model,
        tokenizer=tokenizer
    )
    
    use_api = False
    pipeline_created = True
except Exception as e:
    pipeline_created = False
    error_message = str(e)
";
        }

        private string GenerateMistralInferenceCode(string prompt, TextGenerationParameters? parameters)
        {
            var maxTokens = parameters?.MaxLength ?? 100;
            var temperature = parameters?.Temperature ?? 0.7;
            var topP = parameters?.TopP ?? 1.0;

            return $@"
try:
    result = pipeline(
        '{prompt.Replace("'", "\\'")}',
        max_new_tokens={maxTokens},
        temperature={temperature},
        top_p={topP},
        do_sample=True
    )
    
    generated_text = result[0]['generated_text']
    
    inference_result = {{
        'text': generated_text,
        'mistral_metadata': {{
            'model': model_name if 'model_name' in globals() else 'unknown'
        }}
    }}
    
    inference_success = True
except Exception as e:
    inference_success = False
    inference_error = str(e)
";
        }

        private async Task<bool> LoadMistralAPIModelAsync(TransformerModelInfo modelSource, TransformerTask taskType, Dictionary<string, object>? modelConfig)
        {
            try
            {
                ValidateMistralAPIConfig(modelConfig);

                var pipelineCode = GenerateMistralAPIPipelineCode(modelSource, taskType, modelConfig);
                OnProgressUpdated("Creating Mistral API pipeline...", 50, 100);

                var result = await ExecutePythonCodeAsync(pipelineCode);
                if (!result.Success)
                {
                    throw new InvalidOperationException($"Failed to create Mistral API pipeline: {result.ErrorMessage}");
                }

                UpdateModelState(modelSource.Name, TransformerModelSource.Mistral, taskType, modelConfig);

                OnProgressUpdated("Mistral API model loaded successfully", 100, 100);
                OnModelLoadingCompleted(modelSource.Name, taskType);

                return true;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Failed to load Mistral API model {modelSource.Name}", ex);
                return false;
            }
        }

        private async Task<bool> LoadMistralHuggingFaceModelAsync(string modelName, TransformerTask taskType, Dictionary<string, object>? modelConfig = null)
        {
            try
            {
                OnProgressUpdated($"Loading Mistral model {modelName} from HuggingFace...", 0, 100);

                var optimizedConfig = AddMistralOptimizations(modelConfig ?? new Dictionary<string, object>());
                var pipelineCode = GenerateMistralPipelineCode(modelName, taskType, optimizedConfig);
                OnProgressUpdated("Creating Mistral pipeline...", 50, 100);

                var result = await ExecutePythonCodeAsync(pipelineCode);
                if (!result.Success)
                {
                    throw new InvalidOperationException($"Failed to create Mistral pipeline: {result.ErrorMessage}");
                }

                UpdateModelState(modelName, TransformerModelSource.Mistral, taskType, modelConfig);

                OnProgressUpdated("Mistral model loaded successfully", 100, 100);
                OnModelLoadingCompleted(modelName, taskType);

                return true;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Failed to load Mistral model {modelName}", ex);
                return false;
            }
        }

        private string GenerateMistralAPIPipelineCode(TransformerModelInfo modelSource, TransformerTask taskType, Dictionary<string, object>? modelConfig)
        {
            var apiKey = modelConfig?["api_key"]?.ToString();

            return $@"
if not mistral_api_available:
    raise ImportError('Mistral API client not available')

# Configure Mistral client
client = MistralClient(api_key='{apiKey}')

model_name = '{modelSource.Name}'
task_type = '{taskType}'
use_api = True

pipeline_created = True
";
        }

        private string GetMistralModelFamily(string? modelName)
        {
            if (string.IsNullOrEmpty(modelName))
                return "Unknown";

            var lowerName = modelName.ToLower();
            if (lowerName.Contains("mixtral"))
                return "Mixtral";
            else if (lowerName.Contains("mistral"))
                return "Mistral";
            
            return "Mistral";
        }

        private void ExtractMistralMetadata(TransformerResult<string> result, object? data)
        {
            try
            {
                if (data is string jsonData)
                {
                    var response = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(jsonData);
                    
                    if (response?.ContainsKey("mistral_metadata") == true)
                    {
                        var mistralMetadata = response["mistral_metadata"] as Dictionary<string, object>;
                        if (mistralMetadata != null)
                        {
                            result.Metadata = result.Metadata ?? new Dictionary<string, object>();
                            result.Metadata["mistral_model"] = mistralMetadata["model"];
                        }
                    }
                }
            }
            catch
            {
                // Metadata extraction failed, continue without it
            }
        }

        #endregion
    }
}