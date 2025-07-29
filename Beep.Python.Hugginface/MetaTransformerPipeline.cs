using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Beep.Python.Model;

namespace Beep.Python.AI.Transformers
{
    /// <summary>
    /// Meta transformer pipeline implementation
    /// Handles Meta AI models (Llama, Code Llama, etc.)
    /// </summary>
    public class MetaTransformerPipeline : BaseTransformerPipeline
    {
        /// <summary>
        /// Initialize Meta transformer pipeline
        /// </summary>
        /// <param name="pythonRunTimeManager">Python runtime manager</param>
        /// <param name="executeManager">Python code execution manager</param>
        public MetaTransformerPipeline(IPythonRunTimeManager pythonRunTimeManager, IPythonCodeExecuteManager executeManager)
            : base(pythonRunTimeManager, executeManager)
        {
            // Meta-specific initialization
        }

        /// <summary>
        /// Initialize Meta pipeline with specific requirements
        /// </summary>
        public override async Task<bool> InitializeAsync(TransformerPipelineConfig config)
        {
            try
            {
                OnProgressUpdated("Initializing Meta AI pipeline...", 0, 100);
                
                // Initialize base pipeline
                _pipelineConfig = config ?? throw new ArgumentNullException(nameof(config));

                // Install Meta-specific packages
                await EnsureMetaPackagesInstalledAsync();
                OnProgressUpdated("Installing Meta AI packages...", 50, 100);

                // Import Meta modules
                await ImportMetaModulesAsync();
                OnProgressUpdated("Importing Meta modules...", 75, 100);

                _isInitialized = true;
                OnProgressUpdated("Meta AI initialization complete", 100, 100);

                return true;
            }
            catch (Exception ex)
            {
                OnErrorOccurred("Failed to initialize Meta AI pipeline", ex);
                return false;
            }
        }

        /// <summary>
        /// Load a model based on the model information and configuration
        /// Meta handles both HuggingFace and local models, routing based on source
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
                OnProgressUpdated($"Loading Meta model {modelInfo.Name}...", 0, 100);

                if (!_isInitialized)
                {
                    throw new InvalidOperationException("Pipeline must be initialized before loading models");
                }

                // Meta supports multiple sources - route based on source
                switch (modelInfo.Source)
                {
                    case TransformerModelSource.Meta:
                    case TransformerModelSource.HuggingFace:
                        return await LoadMetaFromHuggingFaceAsync(modelInfo, taskType, modelConfig);
                    
                    case TransformerModelSource.Local:
                        return await LoadMetaLocalModelAsync(modelInfo, taskType, modelConfig);
                    
                    default:
                        throw new ArgumentException($"Meta pipeline does not support model source: {modelInfo.Source}");
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Failed to load Meta model {modelInfo.Name}", ex);
                return false;
            }
        }

        private async Task<bool> LoadMetaFromHuggingFaceAsync(TransformerModelInfo modelInfo, TransformerTask taskType, Dictionary<string, object>? modelConfig)
        {
            OnProgressUpdated($"Loading Meta model {modelInfo.Name} from HuggingFace...", 0, 100);

            // Add Meta-specific optimizations to model config
            var optimizedConfig = AddMetaOptimizations(modelConfig?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<string, object>());

            // Generate Meta-optimized pipeline code
            var pipelineCode = GenerateMetaPipelineCode(modelInfo.Name, taskType, optimizedConfig);
            OnProgressUpdated("Creating Meta pipeline...", 50, 100);

            var result = await ExecutePythonCodeAsync(pipelineCode);
            if (!result.Success)
            {
                throw new InvalidOperationException($"Failed to create Meta pipeline: {result.ErrorMessage}");
            }

            UpdateModelState(modelInfo.Name, TransformerModelSource.Meta, taskType, modelConfig?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value));

            OnProgressUpdated("Meta model loaded successfully", 100, 100);
            OnModelLoadingCompleted(modelInfo.Name, taskType);

            return true;
        }

        private async Task<bool> LoadMetaLocalModelAsync(TransformerModelInfo modelInfo, TransformerTask taskType, Dictionary<string, object>? modelConfig)
        {
            OnProgressUpdated($"Loading local Meta model {modelInfo.ModelPath ?? modelInfo.Name}...", 0, 100);

            // Add Meta-specific optimizations to model config
            var optimizedConfig = AddMetaOptimizations(modelConfig ?? new Dictionary<string, object>());

            // Generate Meta-optimized pipeline code for local model
            var pipelineCode = GenerateMetaPipelineCode(modelInfo.ModelPath ?? modelInfo.Name, taskType, optimizedConfig);
            OnProgressUpdated("Creating Meta local pipeline...", 50, 100);

            var result = await ExecutePythonCodeAsync(pipelineCode);
            if (!result.Success)
            {
                throw new InvalidOperationException($"Failed to create Meta pipeline: {result.ErrorMessage}");
            }

            UpdateModelState(modelInfo.ModelPath ?? modelInfo.Name, TransformerModelSource.Meta, taskType, modelConfig);

            OnProgressUpdated("Meta local model loaded successfully", 100, 100);
            OnModelLoadingCompleted(modelInfo.ModelPath ?? modelInfo.Name, taskType);

            return true;
        }

        /// <summary>
        /// Get supported tasks for Meta models
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
        /// Generate text using Meta models with optimizations
        /// </summary>
        public override async Task<TransformerResult<string>> GenerateTextAsync(string prompt, TextGenerationParameters? parameters = null)
        {
            try
            {
                if (!_isModelLoaded)
                {
                    throw new InvalidOperationException("No Meta model is loaded");
                }

                OnInferenceStarted(_modelName, _taskType);
                var startTime = DateTime.UtcNow;

                // Generate Meta-optimized inference code
                var inferenceCode = GenerateMetaInferenceCode(prompt, parameters);
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
                    // Extract Meta-specific metadata
                    ExtractMetaMetadata(transformerResult, result.Result);
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
                OnErrorOccurred($"Meta text generation failed", ex);
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
        /// Get model information specific to Meta
        /// </summary>
        public override TransformerModelInfo? GetModelInfo()
        {
            if (string.IsNullOrEmpty(_modelName))
                return null;

            return new TransformerModelInfo
            {
                Name = _modelName,
                Source = TransformerModelSource.Meta,
                Architecture = "Meta Llama",
                Metadata = new Dictionary<string, object>
                {
                    ["provider"] = "Meta",
                    ["is_meta"] = true,
                    ["model_family"] = GetModelFamily(_modelName),
                    ["task_type"] = _taskType.ToString(),
                    ["supports_huggingface"] = true,
                    ["supports_local"] = true
                }
            };
        }

        #region Private Helper Methods

        private async Task EnsureMetaPackagesInstalledAsync()
        {
            var packages = new[] { "torch", "accelerate", "bitsandbytes", "sentencepiece" };
            
            foreach (var package in packages)
            {
                // Install packages using the existing package management infrastructure
                // Implementation would depend on the existing IPythonPackageManager
            }
        }

        private async Task ImportMetaModulesAsync()
        {
            var importCode = @"
from transformers import LlamaTokenizer, LlamaForCausalLM, AutoTokenizer, AutoModelForCausalLM
from transformers import BitsAndBytesConfig
import torch
import json
import os
";
            await ExecutePythonCodeAsync(importCode);
        }

        private bool IsLocalPath(string path)
        {
            return !string.IsNullOrEmpty(path) && 
                   (System.IO.Path.IsPathRooted(path) || 
                    System.IO.Directory.Exists(path) || 
                    System.IO.File.Exists(path));
        }

        private Dictionary<string, object> AddMetaOptimizations(Dictionary<string, object> originalConfig)
        {
            var config = originalConfig ?? new Dictionary<string, object>();

            // Add default optimizations for Meta models
            if (!config.ContainsKey("torch_dtype"))
            {
                config["torch_dtype"] = "torch.float16";
            }

            if (!config.ContainsKey("device_map"))
            {
                config["device_map"] = "auto";
            }

            // Add quantization for large models if not specified
            if (!config.ContainsKey("load_in_8bit") && !config.ContainsKey("load_in_4bit"))
            {
                config["load_in_8bit"] = true;
            }

            return config;
        }

        private string GenerateMetaPipelineCode(string modelName, TransformerTask taskType, Dictionary<string, object> modelConfig)
        {
            var taskMapping = GetHuggingFaceTaskName(taskType);
            var deviceConfig = GetDeviceConfig();
            
            // Handle quantization configuration
            var quantizationConfig = "";
            if (modelConfig.ContainsKey("load_in_8bit") && Convert.ToBoolean(modelConfig["load_in_8bit"]))
            {
                quantizationConfig = @"
quantization_config = BitsAndBytesConfig(
    load_in_8bit=True,
    llm_int8_threshold=6.0,
    llm_int8_has_fp16_weight=False,
)";
            }
            else if (modelConfig.ContainsKey("load_in_4bit") && Convert.ToBoolean(modelConfig["load_in_4bit"]))
            {
                quantizationConfig = @"
quantization_config = BitsAndBytesConfig(
    load_in_4bit=True,
    bnb_4bit_compute_dtype=torch.float16,
    bnb_4bit_quant_type='nf4',
    bnb_4bit_use_double_quant=True,
)";
            }

            var torchDtype = modelConfig.ContainsKey("torch_dtype") ? modelConfig["torch_dtype"]?.ToString() : "torch.float16";
            var deviceMap = modelConfig.ContainsKey("device_map") ? modelConfig["device_map"]?.ToString() : "auto";

            return $@"
try:
    {quantizationConfig}
    
    # Load tokenizer
    tokenizer = AutoTokenizer.from_pretrained('{modelName}')
    
    # Load model with optimizations
    model_kwargs = {{
        'torch_dtype': {torchDtype},
        'device_map': '{deviceMap}',
        'trust_remote_code': True
    }}
    
    {(quantizationConfig != "" ? "model_kwargs['quantization_config'] = quantization_config" : "")}
    
    model = AutoModelForCausalLM.from_pretrained('{modelName}', **model_kwargs)
    
    # Create pipeline
    pipeline = pipeline(
        task='{taskMapping}',
        model=model,
        tokenizer=tokenizer,
        device_map='{deviceMap}'
    )
    
    pipeline_created = True
except Exception as e:
    pipeline_created = False
    error_message = str(e)
";
        }

        private string GenerateMetaInferenceCode(string prompt, TextGenerationParameters? parameters)
        {
            var maxTokens = parameters?.MaxLength ?? 100;
            var temperature = parameters?.Temperature ?? 0.7;
            var topP = parameters?.TopP ?? 0.9;
            var topK = parameters?.TopK ?? 50;
            var doSample = parameters?.DoSample ?? true;
            var repeatPenalty = parameters?.RepetitionPenalty ?? 1.0;

            // Format prompt for Llama models
            var formattedPrompt = FormatLlamaPrompt(prompt);

            return $@"
try:
    # Format input for Meta/Llama models
    formatted_prompt = '{formattedPrompt.Replace("'", "\\'")}'

    # Generate with Meta-optimized parameters
    result = pipeline(
        formatted_prompt,
        max_new_tokens={maxTokens},
        temperature={temperature},
        top_p={topP},
        top_k={topK},
        do_sample={doSample.ToString().ToLower()},
        repetition_penalty={repeatPenalty},
        pad_token_id=tokenizer.eos_token_id,
        return_full_text=False
    )
    
    generated_text = result[0]['generated_text']
    
    # Estimate token usage
    input_tokens = len(tokenizer.encode(formatted_prompt))
    output_tokens = len(tokenizer.encode(generated_text))
    
    inference_success = True
    inference_result = {{
        'text': generated_text,
        'usage': {{
            'prompt_tokens': input_tokens,
            'completion_tokens': output_tokens,
            'total_tokens': input_tokens + output_tokens
        }},
        'meta_metadata': {{
            'model': '{_modelName}',
            'model_family': '{GetModelFamily(_modelName)}',
            'formatted_prompt': formatted_prompt
        }}
    }}
except Exception as e:
    inference_success = False
    inference_error = str(e)
";
        }

        private string FormatLlamaPrompt(string prompt)
        {
            // Basic Llama prompt formatting
            // For chat models, you might want to use specific templates
            if (_modelName?.ToLower().Contains("chat") == true || _modelName?.ToLower().Contains("instruct") == true)
            {
                return $"<s>[INST] {prompt} [/INST]";
            }
            return prompt;
        }

        private string GetModelFamily(string? modelName)
        {
            if (string.IsNullOrEmpty(modelName))
                return "Unknown";

            var lowerName = modelName.ToLower();
            if (lowerName.Contains("llama"))
                return "Llama";
            else if (lowerName.Contains("codellama"))
                return "Code Llama";
            else if (lowerName.Contains("alpaca"))
                return "Alpaca";
            else if (lowerName.Contains("vicuna"))
                return "Vicuna";
            
            return "Meta";
        }

        private void ExtractMetaMetadata(TransformerResult<string> result, object? data)
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
                        if (usage != null)
                        {
                            result.TokenUsage = new TokenUsage
                            {
                                PromptTokens = Convert.ToInt32(usage["prompt_tokens"]),
                                CompletionTokens = Convert.ToInt32(usage["completion_tokens"])
                                // TotalTokens is calculated automatically from PromptTokens + CompletionTokens
                            };
                        }
                    }

                    // Extract Meta-specific metadata
                    if (response?.ContainsKey("meta_metadata") == true)
                    {
                        var metaMetadata = response["meta_metadata"] as Dictionary<string, object>;
                        if (metaMetadata != null)
                        {
                            result.Metadata = result.Metadata ?? new Dictionary<string, object>();
                            result.Metadata["meta_model"] = metaMetadata["model"];
                            result.Metadata["meta_model_family"] = metaMetadata["model_family"];
                            result.Metadata["meta_formatted_prompt"] = metaMetadata["formatted_prompt"];
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