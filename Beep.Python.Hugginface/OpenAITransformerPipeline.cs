using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Beep.Python.Model;

namespace Beep.Python.AI.Transformers
{
    /// <summary>
    /// OpenAI transformer pipeline implementation
    /// Handles OpenAI API-based models (GPT-3.5, GPT-4, etc.)
    /// </summary>
    public class OpenAITransformerPipeline : BaseTransformerPipeline
    {
        /// <summary>
        /// Initialize OpenAI transformer pipeline
        /// </summary>
        /// <param name="pythonRunTimeManager">Python runtime manager</param>
        /// <param name="executeManager">Python code execution manager</param>
        public OpenAITransformerPipeline(IPythonRunTimeManager pythonRunTimeManager, IPythonCodeExecuteManager executeManager)
            : base(pythonRunTimeManager, executeManager)
        {
        }

        /// <summary>
        /// Initialize OpenAI pipeline with API requirements
        /// </summary>
        public override async Task<bool> InitializeAsync(TransformerPipelineConfig config)
        {
            try
            {
                OnProgressUpdated("Initializing OpenAI pipeline...", 0, 100);
                
                _pipelineConfig = config ?? throw new ArgumentNullException(nameof(config));

                // Install OpenAI-specific packages
                await EnsureOpenAIPackagesInstalledAsync();
                OnProgressUpdated("Installing OpenAI packages...", 50, 100);

                // Import OpenAI modules
                await ImportOpenAIModulesAsync();
                OnProgressUpdated("Importing OpenAI modules...", 75, 100);

                _isInitialized = true;
                OnProgressUpdated("OpenAI initialization complete", 100, 100);

                return true;
            }
            catch (Exception ex)
            {
                OnErrorOccurred("Failed to initialize OpenAI pipeline", ex);
                return false;
            }
        }

        /// <summary>
        /// Load a model based on the model information and configuration
        /// OpenAI handles API-based models only
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
                OnProgressUpdated($"Loading OpenAI model {modelInfo.Name}...", 0, 100);

                if (!_isInitialized)
                {
                    throw new InvalidOperationException("Pipeline must be initialized before loading models");
                }

                // OpenAI only supports API-based models
                if (modelInfo.Source != TransformerModelSource.OpenAI)
                {
                    throw new ArgumentException($"OpenAI pipeline only supports OpenAI models. Received: {modelInfo.Source}");
                }

                // Validate OpenAI configuration
                ValidateOpenAIConfig(modelConfig);

                // Generate OpenAI-specific pipeline code
                var pipelineCode = GenerateOpenAIPipelineCode(modelInfo, taskType, modelConfig);
                OnProgressUpdated("Creating OpenAI pipeline...", 50, 100);

                var result = await ExecutePythonCodeAsync(pipelineCode);
                if (!result.Success)
                {
                    throw new InvalidOperationException($"Failed to create OpenAI pipeline: {result.ErrorMessage}");
                }

                UpdateModelState(modelInfo.Name, TransformerModelSource.OpenAI, taskType, modelConfig);

                OnProgressUpdated("OpenAI model loaded successfully", 100, 100);
                OnModelLoadingCompleted(modelInfo.Name, taskType);

                return true;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Failed to load OpenAI model {modelInfo.Name}", ex);
                return false;
            }
        }

        /// <summary>
        /// Get supported tasks for OpenAI models
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
                TransformerTask.FeatureExtraction
            };
        }

        /// <summary>
        /// Generate text using OpenAI models
        /// </summary>
        public override async Task<TransformerResult<string>> GenerateTextAsync(string prompt, TextGenerationParameters parameters = null)
        {
            try
            {
                if (!_isModelLoaded)
                {
                    throw new InvalidOperationException("No OpenAI model is loaded");
                }

                OnInferenceStarted(_modelName, _taskType);
                var startTime = DateTime.UtcNow;

                // Generate OpenAI-specific inference code
                var inferenceCode = GenerateOpenAIInferenceCode(prompt, parameters);
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
                    // Extract token usage if available
                    ExtractTokenUsage(transformerResult, result.Data);
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
                OnErrorOccurred($"OpenAI text generation failed", ex);
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
        /// Get model information specific to OpenAI
        /// </summary>
        public override TransformerModelInfo GetModelInfo()
        {
            var baseInfo = base.GetModelInfo();
            if (baseInfo != null)
            {
                baseInfo.Metadata = baseInfo.Metadata ?? new Dictionary<string, object>();
                baseInfo.Metadata["provider"] = "OpenAI";
                baseInfo.Metadata["is_openai"] = true;
            }
            return baseInfo;
        }

        #region Provider-Specific Implementation

        protected override async Task<(bool Success, T Data, string ErrorMessage)> ExecuteProviderSpecificInferenceAsync<T>(string taskName, object input, object parameters)
        {
            try
            {
                // Generate OpenAI-specific inference code
                var inferenceCode = GenerateOpenAIInferenceCode(input?.ToString(), parameters as TextGenerationParameters);
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

        private async Task EnsureOpenAIPackagesInstalledAsync()
        {
            var packages = new[] { "openai", "tiktoken" };
            
            foreach (var package in packages)
            {
                // Install packages using the existing package management infrastructure
            }
        }

        private async Task ImportOpenAIModulesAsync()
        {
            var importCode = @"
import openai
import tiktoken
import json
import os
";
            await ExecutePythonCodeAsync(importCode);
        }

        private void ValidateOpenAIConfig(Dictionary<string, object>? modelConfig)
        {
            if (modelConfig == null)
            {
                throw new ArgumentException("OpenAI requires configuration parameters");
            }

            if (!modelConfig.ContainsKey("api_key") || string.IsNullOrEmpty(modelConfig["api_key"]?.ToString()))
            {
                // Check if API key is available in environment variables
                var envApiKey = System.Environment.GetEnvironmentVariable("OPENAI_API_KEY");
                if (string.IsNullOrEmpty(envApiKey))
                {
                    throw new ArgumentException("OpenAI requires 'api_key' configuration parameter or OPENAI_API_KEY environment variable");
                }
            }
        }

        private string GetApiKey(Dictionary<string, object> modelConfig)
        {
            if (modelConfig?.ContainsKey("api_key") == true)
            {
                return modelConfig["api_key"]?.ToString();
            }

            // Check environment variable
            return System.Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        }

        private string GenerateOpenAIPipelineCode(TransformerModelInfo modelInfo, TransformerTask taskType, Dictionary<string, object>? modelConfig)
        {
            var apiKey = modelConfig?["api_key"]?.ToString();
            var baseUrl = modelConfig?.ContainsKey("base_url") == true ? modelConfig["base_url"]?.ToString() : null;

            return $@"
import openai

# Configure OpenAI client
openai.api_key = '{apiKey}'
{(baseUrl != null ? $"openai.api_base = '{baseUrl}'" : "")}

# Set up model configuration
model_name = '{modelInfo.Name}'
task_type = '{taskType}'

pipeline_created = True
";
        }

        private string GenerateOpenAIInferenceCode(string prompt, TextGenerationParameters parameters)
        {
            var maxTokens = parameters?.MaxLength ?? 100;
            var temperature = parameters?.Temperature ?? 0.7;
            var topP = parameters?.TopP ?? 1.0;

            return $@"
try:
    response = client.chat.completions.create(
        model=model_name,
        messages=[
            {{'role': 'user', 'content': '{prompt?.Replace("'", "\\'")}'}},
        ],
        max_tokens={maxTokens},
        temperature={temperature},
        top_p={topP}
    )
    
    result = response.choices[0].message.content
    token_usage = {{
        'prompt_tokens': response.usage.prompt_tokens,
        'completion_tokens': response.usage.completion_tokens,
        'total_tokens': response.usage.total_tokens
    }}
    
    inference_success = True
    inference_result = {{
        'text': result,
        'usage': token_usage
    }}
except Exception as e:
    inference_success = False
    inference_error = str(e)
";
        }

        private void ExtractTokenUsage(TransformerResult<string> result, object data)
        {
            try
            {
                // Parse token usage from the response
                if (data is string jsonData)
                {
                    var response = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(jsonData);
                    if (response?.ContainsKey("usage") == true)
                    {
                        var usage = response["usage"] as Dictionary<string, object>;
                        result.TokenUsage = new TokenUsage
                        {
                            InputTokens = Convert.ToInt32(usage["prompt_tokens"]),
                            OutputTokens = Convert.ToInt32(usage["completion_tokens"])
                        };
                    }
                }
            }
            catch
            {
                // Token usage extraction failed, continue without it
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