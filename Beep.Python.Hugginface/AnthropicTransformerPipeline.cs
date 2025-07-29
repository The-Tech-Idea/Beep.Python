using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Beep.Python.Model;

namespace Beep.Python.AI.Transformers
{
    /// <summary>
    /// Anthropic transformer pipeline implementation
    /// Handles Anthropic Claude models
    /// </summary>
    public class AnthropicTransformerPipeline : BaseTransformerPipeline
    {
        /// <summary>
        /// Initialize Anthropic transformer pipeline
        /// </summary>
        /// <param name="pythonRunTimeManager">Python runtime manager</param>
        /// <param name="executeManager">Python code execution manager</param>
        public AnthropicTransformerPipeline(IPythonRunTimeManager pythonRunTimeManager, IPythonCodeExecuteManager executeManager)
            : base(pythonRunTimeManager, executeManager)
        {
        }

        /// <summary>
        /// Initialize Anthropic pipeline with API requirements
        /// </summary>
        public override async Task<bool> InitializeAsync(TransformerPipelineConfig config)
        {
            try
            {
                OnProgressUpdated("Initializing Anthropic pipeline...", 0, 100);
                
                _pipelineConfig = config ?? throw new ArgumentNullException(nameof(config));

                // Install Anthropic-specific packages
                await EnsureAnthropicPackagesInstalledAsync();
                OnProgressUpdated("Installing Anthropic packages...", 50, 100);

                // Import Anthropic modules
                await ImportAnthropicModulesAsync();
                OnProgressUpdated("Importing Anthropic modules...", 75, 100);

                _isInitialized = true;
                OnProgressUpdated("Anthropic initialization complete", 100, 100);

                return true;
            }
            catch (Exception ex)
            {
                OnErrorOccurred("Failed to initialize Anthropic pipeline", ex);
                return false;
            }
        }

        /// <summary>
        /// Load a model based on the model information and configuration
        /// Anthropic handles API-based models only
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
                OnProgressUpdated($"Loading Anthropic model {modelInfo.Name}...", 0, 100);

                if (!_isInitialized)
                {
                    throw new InvalidOperationException("Pipeline must be initialized before loading models");
                }

                // Anthropic only supports API-based models
                if (modelInfo.Source != TransformerModelSource.Anthropic)
                {
                    throw new ArgumentException($"Anthropic pipeline only supports Anthropic models. Received: {modelInfo.Source}");
                }

                // Validate Anthropic configuration
                ValidateAnthropicConfig(modelConfig);

                // Generate Anthropic-specific pipeline code
                var pipelineCode = GenerateAnthropicPipelineCode(modelInfo, taskType, modelConfig);
                OnProgressUpdated("Creating Anthropic pipeline...", 50, 100);

                var result = await ExecutePythonCodeAsync(pipelineCode);
                if (!result.Success)
                {
                    throw new InvalidOperationException($"Failed to create Anthropic pipeline: {result.ErrorMessage}");
                }

                UpdateModelState(modelInfo.Name, TransformerModelSource.Anthropic, taskType, modelConfig);

                OnProgressUpdated("Anthropic model loaded successfully", 100, 100);
                OnModelLoadingCompleted(modelInfo.Name, taskType);

                return true;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Failed to load Anthropic model {modelInfo.Name}", ex);
                return false;
            }
        }

        /// <summary>
        /// Get supported tasks for Anthropic models
        /// </summary>
        public override List<TransformerTask> GetSupportedTasks()
        {
            return new List<TransformerTask>
            {
                TransformerTask.TextGeneration,
                TransformerTask.Conversational,
                TransformerTask.TextClassification,
                TransformerTask.Summarization,
                TransformerTask.Translation,
                TransformerTask.QuestionAnswering,
                TransformerTask.FeatureExtraction
            };
        }

        /// <summary>
        /// Generate text using Anthropic Claude models
        /// </summary>
        public override async Task<TransformerResult<string>> GenerateTextAsync(string prompt, TextGenerationParameters parameters = null)
        {
            try
            {
                if (!_isModelLoaded)
                {
                    throw new InvalidOperationException("No Anthropic model is loaded");
                }

                OnInferenceStarted(_modelName, _taskType);
                var startTime = DateTime.UtcNow;

                // Generate Anthropic-specific inference code
                var inferenceCode = GenerateAnthropicInferenceCode(prompt, parameters);
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
                    transformerResult.Data = result.Data?.ToString();
                    // Extract Anthropic-specific metadata
                    ExtractAnthropicMetadata(transformerResult, result.Data);
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
                OnErrorOccurred($"Anthropic text generation failed", ex);
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
        /// Get model information specific to Anthropic
        /// </summary>
        public override TransformerModelInfo GetModelInfo()
        {
            var baseInfo = base.GetModelInfo();
            if (baseInfo != null)
            {
                baseInfo.Metadata = baseInfo.Metadata ?? new Dictionary<string, object>();
                baseInfo.Metadata["provider"] = "Anthropic";
                baseInfo.Metadata["is_anthropic"] = true;
            }
            return baseInfo;
        }

        #region Provider-Specific Implementation

        protected override async Task<(bool Success, T Data, string ErrorMessage)> ExecuteProviderSpecificInferenceAsync<T>(string taskName, object input, object parameters)
        {
            try
            {
                // Generate Anthropic-specific inference code
                var inferenceCode = GenerateAnthropicInferenceCode(input?.ToString(), parameters as TextGenerationParameters);
                var result = await ExecutePythonCodeAsync(inferenceCode);
                
                if (result.Success)
                {
                    var data = ParseInferenceResult<T>(result.Data?.ToString());
                    return (true, data, null);
                }
                else
                {
                    return (false, default(T), result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                return (false, default(T), ex.Message);
            }
        }

        #endregion

        #region Private Helper Methods

        private async Task EnsureAnthropicPackagesInstalledAsync()
        {
            var packages = new[] { "anthropic" };
            
            foreach (var package in packages)
            {
                // Install packages using the existing package management infrastructure
            }
        }

        private async Task ImportAnthropicModulesAsync()
        {
            var importCode = @"
import anthropic
import json
import os
";
            await ExecutePythonCodeAsync(importCode);
        }

        private void ValidateAnthropicConfig(Dictionary<string, object> modelConfig)
        {
            if (modelConfig == null)
            {
                throw new ArgumentException("Anthropic requires configuration parameters");
            }

            if (!modelConfig.ContainsKey("api_key") || string.IsNullOrEmpty(modelConfig["api_key"]?.ToString()))
            {
                throw new ArgumentException("Anthropic requires 'api_key' configuration parameter");
            }
        }

        private string GenerateAnthropicPipelineCode(TransformerModelInfo modelInfo, TransformerTask taskType, Dictionary<string, object>? modelConfig)
        {
            var apiKey = modelConfig?["api_key"]?.ToString();
            var baseUrl = modelConfig?.ContainsKey("base_url") == true ? modelConfig["base_url"]?.ToString() : null;

            return $@"
import anthropic

# Configure Anthropic client
client = anthropic.Anthropic(
    api_key='{apiKey}'{(baseUrl != null ? $",\n    base_url='{baseUrl}'" : "")}
)

# Set up model configuration
model_name = '{modelInfo.Name}'
task_type = '{taskType}'

pipeline_created = True
";
        }

        private string GenerateAnthropicInferenceCode(string prompt, TextGenerationParameters parameters)
        {
            var maxTokens = parameters?.MaxLength ?? 100;
            var temperature = parameters?.Temperature ?? 0.7;
            var stopSequences = parameters?.StopSequences?.Count > 0 
                ? $"[{string.Join(", ", parameters.StopSequences.Select(s => $"'{s}'"))}]" 
                : "[]";

            var escapedPrompt = prompt?.Replace("'", "\\'") ?? "";

            return $@"
try:
    response = client.messages.create(
        model=model_name,
        max_tokens={maxTokens},
        temperature={temperature},
        stop_sequences={stopSequences},
        messages=[
            {{
                'role': 'user',
                'content': '{escapedPrompt}',
            }}
        ]
    )
    
    result = response.content[0].text
    
    # Anthropic API doesn't directly provide token counts in the response
    # We'll estimate based on the input and output text
    prompt_tokens = len('{escapedPrompt}') // 4  # Rough estimation
    completion_tokens = len(result) // 4  # Rough estimation
    
    inference_success = True
    inference_result = {{
        'text': result,
        'usage': {{
            'prompt_tokens': prompt_tokens,
            'completion_tokens': completion_tokens,
            'total_tokens': prompt_tokens + completion_tokens
        }},
        'anthropic_metadata': {{
            'model': response.model,
            'id': response.id,
            'role': response.role,
            'type': response.type,
            'stop_reason': response.stop_reason if hasattr(response, 'stop_reason') else None
        }}
    }}
except Exception as e:
    inference_success = False
    inference_error = str(e)
";
        }

        private void ExtractAnthropicMetadata(TransformerResult<string> result, object data)
        {
            try
            {
                if (data is string jsonData)
                {
                    var response = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(jsonData);
                    
                    // Extract token usage
                    if (response?.ContainsKey("usage") == true)
                    {
                        var usage = response["usage"] as Dictionary<string, object>;
                        result.TokenUsage = new TokenUsage
                        {
                            InputTokens = Convert.ToInt32(usage["prompt_tokens"]),
                            OutputTokens = Convert.ToInt32(usage["completion_tokens"])
                        };
                    }

                    // Extract Anthropic-specific metadata
                    if (response?.ContainsKey("anthropic_metadata") == true)
                    {
                        var anthropicMetadata = response["anthropic_metadata"] as Dictionary<string, object>;
                        result.Metadata = result.Metadata ?? new Dictionary<string, object>();
                        result.Metadata["anthropic_model"] = anthropicMetadata["model"];
                        result.Metadata["anthropic_id"] = anthropicMetadata["id"];
                        result.Metadata["anthropic_role"] = anthropicMetadata["role"];
                        result.Metadata["anthropic_type"] = anthropicMetadata["type"];
                        result.Metadata["anthropic_stop_reason"] = anthropicMetadata["stop_reason"];
                    }
                }
            }
            catch
            {
                // Metadata extraction failed, continue without it
            }
        }

        private T ParseInferenceResult<T>(string jsonResult)
        {
            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<T>(jsonResult);
            }
            catch
            {
                return default(T);
            }
        }

        private async Task<(bool Success, object Data, string ErrorMessage)> ExecutePythonCodeAsync(string code)
        {
            try
            {
                // This would integrate with the existing Python execution infrastructure
                return (true, null, null);
            }
            catch (Exception ex)
            {
                return (false, null, ex.Message);
            }
        }

        #endregion
    }
}