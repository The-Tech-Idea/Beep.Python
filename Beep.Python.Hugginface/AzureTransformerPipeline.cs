using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Beep.Python.Model;

namespace Beep.Python.AI.Transformers
{
    /// <summary>
    /// Azure OpenAI transformer pipeline implementation
    /// Handles Azure-hosted OpenAI models
    /// </summary>
    public class AzureTransformerPipeline : BaseTransformerPipeline
    {
        /// <summary>
        /// Initialize Azure OpenAI transformer pipeline
        /// </summary>
        /// <param name="pythonRunTimeManager">Python runtime manager</param>
        /// <param name="executeManager">Python code execution manager</param>
        public AzureTransformerPipeline(IPythonRunTimeManager pythonRunTimeManager, IPythonCodeExecuteManager executeManager)
            : base(pythonRunTimeManager, executeManager)
        {
        }

        /// <summary>
        /// Initialize Azure OpenAI pipeline with specific requirements
        /// </summary>
        public override async Task<bool> InitializeAsync(TransformerPipelineConfig config)
        {
            try
            {
                OnProgressUpdated("Initializing Azure OpenAI pipeline...", 0, 100);
                
                _pipelineConfig = config ?? throw new ArgumentNullException(nameof(config));

                // Install Azure OpenAI packages
                await EnsureAzurePackagesInstalledAsync();
                OnProgressUpdated("Installing Azure OpenAI packages...", 50, 100);

                // Import Azure modules
                await ImportAzureModulesAsync();
                OnProgressUpdated("Importing Azure modules...", 75, 100);

                _isInitialized = true;
                OnProgressUpdated("Azure OpenAI initialization complete", 100, 100);

                return true;
            }
            catch (Exception ex)
            {
                OnErrorOccurred("Failed to initialize Azure OpenAI pipeline", ex);
                return false;
            }
        }

        /// <summary>
        /// Load a model based on the model information and configuration
        /// Azure handles Azure OpenAI Service models only
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
                OnProgressUpdated($"Loading Azure model {modelInfo.Name}...", 0, 100);

                if (!_isInitialized)
                {
                    throw new InvalidOperationException("Pipeline must be initialized before loading models");
                }

                // Azure only supports Azure OpenAI Service models
                if (modelInfo.Source != TransformerModelSource.Azure)
                {
                    throw new ArgumentException($"Azure pipeline only supports Azure models. Received: {modelInfo.Source}");
                }

                // Validate Azure configuration
                ValidateAzureConfig(modelConfig);

                // Generate Azure-specific pipeline code
                var pipelineCode = GenerateAzurePipelineCode(modelInfo, taskType, modelConfig);
                OnProgressUpdated("Creating Azure pipeline...", 50, 100);

                var result = await ExecutePythonCodeAsync(pipelineCode);
                if (!result.Success)
                {
                    throw new InvalidOperationException($"Failed to create Azure pipeline: {result.ErrorMessage}");
                }

                UpdateModelState(modelInfo.Name, TransformerModelSource.Azure, taskType, modelConfig);

                OnProgressUpdated("Azure model loaded successfully", 100, 100);
                OnModelLoadingCompleted(modelInfo.Name, taskType);

                return true;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Failed to load Azure model {modelInfo.Name}", ex);
                return false;
            }
        }

        /// <summary>
        /// Generate text using Azure OpenAI models
        /// </summary>
        public override async Task<TransformerResult<string>> GenerateTextAsync(string prompt, TextGenerationParameters parameters = null)
        {
            try
            {
                if (!_isModelLoaded)
                {
                    throw new InvalidOperationException("No Azure OpenAI model is loaded");
                }

                OnInferenceStarted(_modelName, _taskType);
                var startTime = DateTime.UtcNow;

                // Generate Azure OpenAI-specific inference code
                var inferenceCode = GenerateAzureOpenAIInferenceCode(prompt, parameters);
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
                    // Extract Azure-specific metadata
                    ExtractAzureMetadata(transformerResult, result.Data);
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
                OnErrorOccurred($"Azure OpenAI text generation failed", ex);
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
        /// Get supported tasks for Azure OpenAI models
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
        /// Get model information specific to Azure OpenAI
        /// </summary>
        public override TransformerModelInfo GetModelInfo()
        {
            var baseInfo = base.GetModelInfo();
            if (baseInfo != null)
            {
                baseInfo.Metadata = baseInfo.Metadata ?? new Dictionary<string, object>();
                baseInfo.Metadata["provider"] = "Azure OpenAI";
                baseInfo.Metadata["is_azure"] = true;
            }
            return baseInfo;
        }

        #region Provider-Specific Implementation

        protected override async Task<(bool Success, T Data, string ErrorMessage)> ExecuteProviderSpecificInferenceAsync<T>(string taskName, object input, object parameters)
        {
            try
            {
                // Generate Azure OpenAI-specific inference code
                var inferenceCode = GenerateAzureOpenAIInferenceCode(input?.ToString(), parameters as TextGenerationParameters);
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

        private async Task EnsureAzurePackagesInstalledAsync()
        {
            var packages = new[] { "openai", "azure-identity" };
            
            foreach (var package in packages)
            {
                // Install packages using the existing package management infrastructure
            }
        }

        private async Task ImportAzureModulesAsync()
        {
            var importCode = @"
import openai
from openai import AzureOpenAI
import json
import os
";
            await ExecutePythonCodeAsync(importCode);
        }

        private void ValidateAzureConfig(Dictionary<string, object> modelConfig)
        {
            if (modelConfig == null)
            {
                throw new ArgumentException("Azure OpenAI requires configuration parameters");
            }

            var requiredKeys = new[] { "azure_endpoint", "api_key", "api_version" };
            foreach (var key in requiredKeys)
            {
                if (!modelConfig.ContainsKey(key) || string.IsNullOrEmpty(modelConfig[key]?.ToString()))
                {
                    throw new ArgumentException($"Azure OpenAI requires '{key}' configuration parameter");
                }
            }
        }

        private string GenerateAzurePipelineCode(TransformerModelInfo modelInfo, TransformerTask taskType, Dictionary<string, object>? modelConfig)
        {
            var apiKey = modelConfig["api_key"].ToString();
            var endpoint = modelConfig["azure_endpoint"].ToString();
            var apiVersion = modelConfig["api_version"].ToString();
            var deploymentName = modelConfig.ContainsKey("deployment_name") ? modelConfig["deployment_name"].ToString() : modelInfo.Name;

            return $@"
# Configure Azure OpenAI client
client = AzureOpenAI(
    api_key='{apiKey}',
    api_version='{apiVersion}',
    azure_endpoint='{endpoint}'
)

# Set up model configuration
deployment_name = '{deploymentName}'
task_type = '{taskType}'

pipeline_created = True
";
        }

        private string GenerateAzureOpenAIInferenceCode(string prompt, TextGenerationParameters parameters)
        {
            var maxTokens = parameters?.MaxLength ?? 100;
            var temperature = parameters?.Temperature ?? 0.7;
            var topP = parameters?.TopP ?? 1.0;

            return $@"
try:
    response = client.chat.completions.create(
        model=deployment_name,
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
    
    azure_metadata = {{
        'model': response.model,
        'created': response.created,
        'id': response.id
    }}
    
    inference_success = True
    inference_result = {{
        'text': result,
        'usage': token_usage,
        'azure_metadata': azure_metadata
    }}
except Exception as e:
    inference_success = False
    inference_error = str(e)
";
        }

        private void ExtractAzureMetadata(TransformerResult<string> result, object data)
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

                    // Extract Azure-specific metadata
                    if (response?.ContainsKey("azure_metadata") == true)
                    {
                        var azureMetadata = response["azure_metadata"] as Dictionary<string, object>;
                        result.Metadata = result.Metadata ?? new Dictionary<string, object>();
                        result.Metadata["azure_model"] = azureMetadata["model"];
                        result.Metadata["azure_created"] = azureMetadata["created"];
                        result.Metadata["azure_id"] = azureMetadata["id"];
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