using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Text.Json;
using Beep.Python.Model;

namespace Beep.Python.AI.Transformers
{
    /// <summary>
    /// Cohere transformer pipeline implementation
    /// Handles Cohere language models
    /// </summary>
    public class CohereTransformerPipeline : BaseTransformerPipeline
    {
        /// <summary>
        /// Initialize Cohere transformer pipeline
        /// </summary>
        /// <param name="pythonRunTimeManager">Python runtime manager</param>
        /// <param name="executeManager">Python code execution manager</param>
        public CohereTransformerPipeline(IPythonRunTimeManager pythonRunTimeManager, IPythonCodeExecuteManager executeManager)
            : base(pythonRunTimeManager, executeManager)
        {
            // Cohere-specific initialization
        }

        /// <summary>
        /// Initialize Cohere pipeline with API requirements
        /// </summary>
        public override async Task<bool> InitializeAsync(TransformerPipelineConfig config)
        {
            try
            {
                OnProgressUpdated("Initializing Cohere pipeline...", 0, 100);
                
                // Initialize base pipeline
                _pipelineConfig = config ?? throw new ArgumentNullException(nameof(config));
                
                // Install Cohere-specific packages
                await EnsureCoherePackagesInstalledAsync();
                OnProgressUpdated("Installing Cohere packages...", 50, 100);

                // Import Cohere modules
                await ImportCohereModulesAsync();
                OnProgressUpdated("Importing Cohere modules...", 75, 100);

                _isInitialized = true;
                OnProgressUpdated("Cohere initialization complete", 100, 100);

                return true;
            }
            catch (Exception ex)
            {
                OnErrorOccurred("Failed to initialize Cohere pipeline", ex);
                return false;
            }
        }

        /// <summary>
        /// Load a model based on the model information and configuration
        /// Cohere handles API-based models only
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
                OnProgressUpdated($"Loading Cohere model {modelInfo.Name}...", 0, 100);

                if (!_isInitialized)
                {
                    throw new InvalidOperationException("Pipeline must be initialized before loading models");
                }

                // Cohere only supports API-based models
                if (modelInfo.Source != TransformerModelSource.Cohere)
                {
                    throw new ArgumentException($"Cohere pipeline only supports Cohere models. Received: {modelInfo.Source}");
                }

                // Validate Cohere configuration
                ValidateCohereConfig(modelConfig);

                // Generate Cohere-specific pipeline code
                var pipelineCode = GenerateCoherePipelineCode(modelInfo, taskType, modelConfig);
                OnProgressUpdated("Creating Cohere pipeline...", 50, 100);

                var result = await ExecutePythonCodeAsync(pipelineCode);
                if (!result.Success)
                {
                    throw new InvalidOperationException($"Failed to create Cohere pipeline: {result.ErrorMessage}");
                }

                // Update model state
                UpdateModelState(modelInfo.Name, TransformerModelSource.Cohere, taskType, modelConfig);

                OnProgressUpdated("Cohere model loaded successfully", 100, 100);
                OnModelLoadingCompleted(modelInfo.Name, taskType);

                return true;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Failed to load Cohere model {modelInfo.Name}", ex);
                return false;
            }
        }

        /// <summary>
        /// Get supported tasks for Cohere models
        /// </summary>
        public override List<TransformerTask> GetSupportedTasks()
        {
            return new List<TransformerTask>
            {
                TransformerTask.TextGeneration,
                TransformerTask.TextClassification,
                TransformerTask.Summarization,
                TransformerTask.FeatureExtraction,
                TransformerTask.SimilarityComparison,
                TransformerTask.Conversational
            };
        }

        /// <summary>
        /// Generate text using Cohere models
        /// </summary>
        public override async Task<TransformerResult<string>> GenerateTextAsync(string prompt, TextGenerationParameters? parameters = null)
        {
            try
            {
                if (!_isModelLoaded)
                {
                    throw new InvalidOperationException("No Cohere model is loaded");
                }

                OnInferenceStarted(_modelName, _taskType);
                var startTime = DateTime.UtcNow;

                // Generate Cohere-specific inference code
                var inferenceCode = GenerateCohereInferenceCode(prompt, parameters);
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
                    // Extract Cohere-specific metadata
                    ExtractCohereMetadata(transformerResult, result.Result);
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
                OnErrorOccurred($"Cohere text generation failed", ex);
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
        /// Get embeddings using Cohere models
        /// </summary>
        public override async Task<TransformerResult<List<float[]>>> GetEmbeddingsAsync(List<string> texts, EmbeddingParameters? parameters = null)
        {
            try
            {
                if (!_isModelLoaded)
                {
                    throw new InvalidOperationException("No Cohere model is loaded");
                }

                OnInferenceStarted(_modelName, TransformerTask.FeatureExtraction);
                var startTime = DateTime.UtcNow;

                // Generate Cohere embedding code
                var embeddingCode = GenerateCohereEmbeddingCode(texts, parameters);
                var result = await ExecutePythonCodeAsync(embeddingCode);

                var executionTime = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;

                var transformerResult = new TransformerResult<List<float[]>>
                {
                    Success = result.Success,
                    ExecutionTimeMs = executionTime,
                    ModelName = _modelName,
                    TaskType = TransformerTask.FeatureExtraction
                };

                if (result.Success)
                {
                    // Parse embeddings from result
                    transformerResult.Data = ParseEmbeddings(result.Result?.ToString());
                }
                else
                {
                    transformerResult.ErrorMessage = result.ErrorMessage;
                }

                OnInferenceCompleted(_modelName, TransformerTask.FeatureExtraction);
                return transformerResult;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Cohere embedding generation failed", ex);
                return new TransformerResult<List<float[]>>
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    ModelName = _modelName,
                    TaskType = TransformerTask.FeatureExtraction
                };
            }
        }

        /// <summary>
        /// Get model information specific to Cohere
        /// </summary>
        public override TransformerModelInfo? GetModelInfo()
        {
            if (string.IsNullOrEmpty(_modelName))
                return null;

            return new TransformerModelInfo
            {
                Name = _modelName,
                Source = TransformerModelSource.Cohere,
                Architecture = "Cohere Language Model",
                Metadata = new Dictionary<string, object>
                {
                    ["provider"] = "Cohere",
                    ["is_cohere"] = true,
                    ["task_type"] = _taskType.ToString(),
                    ["is_api_based"] = true
                }
            };
        }

        #region Private Helper Methods

        private async Task EnsureCoherePackagesInstalledAsync()
        {
            var packages = new[] { "cohere" };
            
            foreach (var package in packages)
            {
                // Install packages using the existing package management infrastructure
                // Implementation would depend on the existing IPythonPackageManager
            }
        }

        private async Task ImportCohereModulesAsync()
        {
            var importCode = @"
import cohere
import json
import os
import numpy as np
";
            await ExecutePythonCodeAsync(importCode);
        }

        private void ValidateCohereConfig(Dictionary<string, object>? modelConfig)
        {
            if (modelConfig == null)
            {
                throw new ArgumentException("Cohere requires configuration parameters");
            }

            if (!modelConfig.ContainsKey("api_key") || string.IsNullOrEmpty(modelConfig["api_key"]?.ToString()))
            {
                throw new ArgumentException("Cohere requires 'api_key' configuration parameter");
            }
        }

        private string GenerateCoherePipelineCode(TransformerModelInfo modelInfo, TransformerTask taskType, Dictionary<string, object>? modelConfig)
        {
            var apiKey = modelConfig?["api_key"]?.ToString();
            var baseUrl = modelConfig?.ContainsKey("base_url") == true ? modelConfig["base_url"]?.ToString() : null;

            return $@"
import cohere

# Configure Cohere client
co = cohere.Client(api_key='{apiKey}'{(baseUrl != null ? $", base_url='{baseUrl}'" : "")})

# Set up model configuration
model_name = '{modelInfo.Name}'
task_type = '{taskType}'

pipeline_created = True
";
        }

        private string GenerateCohereInferenceCode(string prompt, TextGenerationParameters? parameters)
        {
            var maxTokens = parameters?.MaxLength ?? 100;
            var temperature = parameters?.Temperature ?? 0.7;
            var topP = parameters?.TopP ?? 1.0;
            var topK = parameters?.TopK ?? 0;
            var stopSequences = parameters?.StopSequences?.Count > 0 
                ? $"[{string.Join(", ", parameters.StopSequences.Select(s => $"'{s}'"))}]" 
                : "None";

            return $@"
try:
    response = co.generate(
        model=model_name,
        prompt='{prompt.Replace("'", "\\'")}',
        max_tokens={maxTokens},
        temperature={temperature},
        p={topP},
        k={topK},
        stop_sequences={stopSequences},
        return_likelihoods='NONE'
    )
    
    result = response.generations[0].text
    
    # Calculate token usage (Cohere provides token counts in some responses)
    prompt_tokens = len('{prompt.Replace("'", "\\'")}') // 4  # Rough estimation
    completion_tokens = len(result) // 4  # Rough estimation
    
    inference_success = True
    inference_result = {{
        'text': result,
        'usage': {{
            'prompt_tokens': prompt_tokens,
            'completion_tokens': completion_tokens,
            'total_tokens': prompt_tokens + completion_tokens
        }},
        'cohere_metadata': {{
            'model': model_name,
            'id': response.generations[0].id if hasattr(response.generations[0], 'id') else None,
            'finish_reason': response.generations[0].finish_reason if hasattr(response.generations[0], 'finish_reason') else None
        }}
    }}
except Exception as e:
    inference_success = False
    inference_error = str(e)
";
        }

        private string GenerateCohereEmbeddingCode(List<string> texts, EmbeddingParameters? parameters)
        {
            var inputType = parameters?.InputType ?? "search_document";
            var truncate = parameters?.Truncate ?? "END";
            var textsJson = System.Text.Json.JsonSerializer.Serialize(texts);

            return $@"
try:
    texts = {textsJson.Replace("\"", "'")}
    
    response = co.embed(
        texts=texts,
        model=model_name,
        input_type='{inputType}',
        truncate='{truncate}'
    )
    
    embeddings = response.embeddings
    
    inference_success = True
    inference_result = {{
        'embeddings': embeddings,
        'cohere_metadata': {{
            'model': model_name,
            'id': response.id if hasattr(response, 'id') else None,
            'num_texts': len(texts),
            'embedding_dim': len(embeddings[0]) if embeddings else 0
        }}
    }}
except Exception as e:
    inference_success = False
    inference_error = str(e)
";
        }

        private List<float[]> ParseEmbeddings(string? jsonResult)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(jsonResult))
                    return new List<float[]>();

                var result = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonResult);
                
                if (result?.ContainsKey("embeddings") == true)
                {
                    var embeddingsData = result["embeddings"];
                    if (embeddingsData is JsonElement element && element.ValueKind == JsonValueKind.Array)
                    {
                        var embeddings = new List<float[]>();
                        foreach (var item in element.EnumerateArray())
                        {
                            if (item.ValueKind == JsonValueKind.Array)
                            {
                                var embedding = new List<float>();
                                foreach (var value in item.EnumerateArray())
                                {
                                    if (value.TryGetSingle(out float floatValue))
                                    {
                                        embedding.Add(floatValue);
                                    }
                                }
                                embeddings.Add(embedding.ToArray());
                            }
                        }
                        return embeddings;
                    }
                }

                return new List<float[]>();
            }
            catch
            {
                return new List<float[]>();
            }
        }

        private void ExtractCohereMetadata(TransformerResult<string> result, object? data)
        {
            try
            {
                if (data is string jsonData)
                {
                    var response = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(jsonData);
                    
                    // Extract Cohere-specific metadata
                    if (response != null && response.ContainsKey("cohere_metadata"))
                    {
                        var metadata = response["cohere_metadata"];
                        if (metadata is JsonElement element)
                        {
                            result.Metadata = result.Metadata ?? new Dictionary<string, object>();
                            foreach (var property in element.EnumerateObject())
                            {
                                result.Metadata[property.Name] = property.Value.ToString();
                            }
                        }
                    }
                }
            }
            catch
            {
                // Ignore errors in metadata extraction
            }
        }

        #endregion
    }
}