using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using Beep.Python.Model;

namespace Beep.Python.AI.Transformers
{
    /// <summary>
    /// Local model transformer pipeline implementation
    /// Handles models stored locally on the filesystem
    /// </summary>
    public class LocalTransformerPipeline : BaseTransformerPipeline
    {
        /// <summary>
        /// Initialize Local transformer pipeline
        /// </summary>
        /// <param name="pythonRunTimeManager">Python runtime manager</param>
        /// <param name="executeManager">Python code execution manager</param>
        public LocalTransformerPipeline(IPythonRunTimeManager pythonRunTimeManager, IPythonCodeExecuteManager executeManager)
            : base(pythonRunTimeManager, executeManager)
        {
        }

        /// <summary>
        /// Initialize local pipeline (uses HuggingFace transformers library for local models)
        /// </summary>
        public override async Task<bool> InitializeAsync(TransformerPipelineConfig config)
        {
            try
            {
                OnProgressUpdated("Initializing Local pipeline...", 0, 100);
                
                _pipelineConfig = config ?? throw new ArgumentNullException(nameof(config));

                // Install required packages for local models
                await EnsureLocalPackagesInstalledAsync();
                OnProgressUpdated("Installing required packages...", 50, 100);

                // Import required modules
                await ImportLocalModulesAsync();
                OnProgressUpdated("Importing required modules...", 75, 100);

                _isInitialized = true;
                OnProgressUpdated("Local initialization complete", 100, 100);

                return true;
            }
            catch (Exception ex)
            {
                OnErrorOccurred("Failed to initialize local pipeline", ex);
                return false;
            }
        }

        /// <summary>
        /// Load a model based on the model information and configuration
        /// Local pipeline handles local and HuggingFace models stored locally
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
                OnProgressUpdated($"Loading model {modelInfo.Name}...", 0, 100);

                if (!_isInitialized)
                {
                    throw new InvalidOperationException("Pipeline must be initialized before loading models");
                }

                // Local pipeline can handle local models and treat HuggingFace models as local paths
                switch (modelInfo.Source)
                {
                    case TransformerModelSource.Local:
                        return await LoadLocalPathModelAsync(modelInfo, taskType, modelConfig);
                    
                    case TransformerModelSource.HuggingFace:
                        // For local pipeline, treat as local path
                        return await LoadLocalPathModelAsync(modelInfo, taskType, modelConfig);
                    
                    default:
                        throw new ArgumentException($"Local pipeline does not support model source: {modelInfo.Source}. Use Local or HuggingFace sources.");
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Failed to load model {modelInfo.Name}", ex);
                return false;
            }
        }

        private async Task<bool> LoadLocalPathModelAsync(TransformerModelInfo modelInfo, TransformerTask taskType, Dictionary<string, object>? modelConfig)
        {
            var modelPath = modelInfo.ModelPath ?? modelInfo.Name;
            OnProgressUpdated($"Loading local model from {modelPath}...", 0, 100);

            // Validate the local path
            if (string.IsNullOrWhiteSpace(modelPath))
            {
                throw new ArgumentException("Model path cannot be null or empty");
            }

            // Check if the path exists (for actual local paths)
            if (modelInfo.Source == TransformerModelSource.Local && !IsValidLocalPath(modelPath))
            {
                throw new FileNotFoundException($"Model path does not exist: {modelPath}");
            }

            // Enhanced model configuration with validation
            var enhancedConfig = PrepareLocalModelConfig(modelConfig, taskType);

            // Generate local model pipeline code
            var pipelineCode = GenerateLocalPipelineCode(modelPath, taskType, enhancedConfig);
            OnProgressUpdated("Creating local pipeline...", 50, 100);

            var result = await ExecutePythonCodeAsync(pipelineCode);
            if (!result.Success)
            {
                throw new InvalidOperationException($"Failed to create local pipeline: {result.ErrorMessage}");
            }

            UpdateModelState(modelPath, TransformerModelSource.Local, taskType, modelConfig);

            OnProgressUpdated("Local model loaded successfully", 100, 100);
            OnModelLoadingCompleted(modelPath, taskType);

            return true;
        }

        /// <summary>
        /// Generate text using local models
        /// </summary>
        public override async Task<TransformerResult<string>> GenerateTextAsync(string prompt, TextGenerationParameters parameters = null)
        {
            return await ExecuteInferenceAsync<string>("text_generation", prompt, parameters);
        }

        /// <summary>
        /// Get supported tasks for local models
        /// </summary>
        public override List<TransformerTask> GetSupportedTasks()
        {
            // Local models can support all tasks depending on the model
            return new List<TransformerTask>
            {
                TransformerTask.TextGeneration,
                TransformerTask.TextClassification,
                TransformerTask.NamedEntityRecognition,
                TransformerTask.QuestionAnswering,
                TransformerTask.Summarization,
                TransformerTask.Translation,
                TransformerTask.FeatureExtraction,
                TransformerTask.SentimentAnalysis,
                TransformerTask.FillMask,
                TransformerTask.ZeroShotClassification,
                TransformerTask.Conversational
            };
        }

        /// <summary>
        /// Get model information specific to local models
        /// </summary>
        public override TransformerModelInfo GetModelInfo()
        {
            var baseInfo = base.GetModelInfo();
            if (baseInfo != null)
            {
                baseInfo.Metadata = baseInfo.Metadata ?? new Dictionary<string, object>();
                baseInfo.Metadata["is_local"] = true;
                baseInfo.Metadata["model_path"] = ModelName;
            }
            return baseInfo;
        }

        #region Provider-Specific Implementation

        protected override async Task<(bool Success, T Data, string ErrorMessage)> ExecuteProviderSpecificInferenceAsync<T>(string taskName, object input, object parameters)
        {
            try
            {
                // Use HuggingFace transformers library for local models
                var inferenceCode = GenerateLocalInferenceCode(taskName, input, parameters);
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

        private async Task EnsureLocalPackagesInstalledAsync()
        {
            var packages = new[] { "transformers", "torch", "tokenizers" };
            
            foreach (var package in packages)
            {
                // Install packages using the existing package management infrastructure
            }
        }

        private async Task ImportLocalModulesAsync()
        {
            var importCode = @"
from transformers import pipeline, AutoTokenizer, AutoModel
import torch
import json
import os
";
            await ExecutePythonCodeAsync(importCode);
        }

        private bool IsValidLocalPath(string modelPath)
        {
            try
            {
                // Check if it's a valid file path
                if (File.Exists(modelPath))
                    return true;
                
                // Check if it's a valid directory path
                if (Directory.Exists(modelPath))
                    return true;
                
                // Check if it contains expected model files
                if (Directory.Exists(modelPath))
                {
                    var modelFiles = new[] { "config.json", "pytorch_model.bin", "model.safetensors", "tf_model.h5" };
                    return modelFiles.Any(file => File.Exists(Path.Combine(modelPath, file)));
                }
                
                return false;
            }
            catch
            {
                return false;
            }
        }

        private Dictionary<string, object> PrepareLocalModelConfig(Dictionary<string, object>? modelConfig, TransformerTask taskType)
        {
            var config = modelConfig ?? new Dictionary<string, object>();
            
            // Add default configurations for local models
            if (!config.ContainsKey("torch_dtype"))
                config["torch_dtype"] = "torch.float16";
            
            if (!config.ContainsKey("device_map"))
                config["device_map"] = "auto";
            
            if (!config.ContainsKey("trust_remote_code"))
                config["trust_remote_code"] = false;
            
            // Add task-specific configurations
            switch (taskType)
            {
                case TransformerTask.TextGeneration:
                    if (!config.ContainsKey("do_sample"))
                        config["do_sample"] = true;
                    if (!config.ContainsKey("max_new_tokens"))
                        config["max_new_tokens"] = 100;
                    break;
                
                case TransformerTask.TextClassification:
                    if (!config.ContainsKey("return_all_scores"))
                        config["return_all_scores"] = false;
                    break;
                
                case TransformerTask.FeatureExtraction:
                    if (!config.ContainsKey("return_tensors"))
                        config["return_tensors"] = "pt";
                    break;
            }
            
            return config;
        }

        private string GenerateLocalPipelineCode(string modelPath, TransformerTask taskType, Dictionary<string, object> enhancedConfig)
        {
            var taskMapping = GetHuggingFaceTaskName(taskType);
            var deviceConfig = GetDeviceConfig();
            var configJson = System.Text.Json.JsonSerializer.Serialize(enhancedConfig);

            return $@"
try:
    import os
    from transformers import pipeline, AutoTokenizer, AutoModel
    import torch
    
    # Verify model path exists
    model_path = '{modelPath}'
    if not os.path.exists(model_path):
        raise FileNotFoundError(f'Model path does not exist: {{model_path}}')
    
    # Create pipeline for local model
    pipeline = pipeline(
        task='{taskMapping}',
        model=model_path,
        device={deviceConfig},
        **{configJson.Replace("\"", "'")}
    )
    
    model_name = model_path
    pipeline_created = True
    
except Exception as e:
    pipeline_created = False
    error_message = str(e)
";
        }

        private string GenerateLocalModelPipelineCode(string modelPath, TransformerTask taskType, Dictionary<string, object> modelConfig)
        {
            var taskMapping = GetHuggingFaceTaskName(taskType);
            var deviceConfig = GetDeviceConfig();
            var configJson = System.Text.Json.JsonSerializer.Serialize(modelConfig ?? new Dictionary<string, object>());

            return $@"
try:
    pipeline = pipeline(
        task='{taskMapping}',
        model='{modelPath}',
        device={deviceConfig},
        **{configJson.Replace("\"", "'")}
    )
    pipeline_created = True
except Exception as e:
    pipeline_created = False
    error_message = str(e)
";
        }

        private string GenerateLocalInferenceCode(string taskName, object input, object parameters)
        {
            var inputJson = System.Text.Json.JsonSerializer.Serialize(input);
            var parametersJson = System.Text.Json.JsonSerializer.Serialize(parameters ?? new object());

            return $@"
try:
    input_data = {inputJson.Replace("\"", "'")}
    parameters = {parametersJson.Replace("\"", "'")}
    
    result = pipeline(input_data, **parameters)
    inference_success = True
    inference_result = result
except Exception as e:
    inference_success = False
    inference_error = str(e)
";
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

        private string GetHuggingFaceTaskName(TransformerTask taskType)
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

        private string GetDeviceConfig()
        {
            return _pipelineConfig?.Device switch
            {
                TransformerDevice.CPU => "\"cpu\"",
                TransformerDevice.CUDA => "0 if torch.cuda.is_available() else \"cpu\"",
                TransformerDevice.Auto => "0 if torch.cuda.is_available() else \"cpu\"",
                _ => "\"cpu\""
            };
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