using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Beep.Python.Model;

namespace Beep.Python.AI.Transformers
{
    /// <summary>
    /// Factory for creating and configuring multimodal transformer pipelines
    /// Provides simplified creation methods for common multimodal scenarios
    /// </summary>
    public static class MultimodalPipelineFactory
    {
        #region Quick Start Methods

        /// <summary>
        /// Create a multimodal pipeline for creative content generation
        /// Includes text-to-image, text-to-audio, and text generation capabilities
        /// </summary>
        /// <param name="pythonRunTimeManager">Python runtime manager</param>
        /// <param name="executeManager">Python execution manager</param>
        /// <param name="config">Optional configuration</param>
        /// <returns>Configured multimodal pipeline</returns>
        public static async Task<MultimodalTransformerPipeline> CreateCreativeContentPipelineAsync(
            IPythonRunTimeManager pythonRunTimeManager,
            IPythonCodeExecuteManager executeManager,
            MultimodalPipelineConfig? config = null)
        {
            config ??= new MultimodalPipelineConfig
            {
                PreloadPipelines = new List<MultimodalTask>
                {
                    MultimodalTask.TextGeneration,
                    MultimodalTask.TextToImage,
                    MultimodalTask.TextToAudio
                },
                DefaultQuality = "high",
                MaxConcurrentTasks = 2
            };

            var pipeline = new MultimodalTransformerPipeline(pythonRunTimeManager, executeManager);
            await pipeline.InitializeAsync(config);

            return pipeline;
        }

        /// <summary>
        /// Create a multimodal pipeline for content analysis
        /// Includes image-to-text, audio-to-text, and visual QA capabilities
        /// </summary>
        /// <param name="pythonRunTimeManager">Python runtime manager</param>
        /// <param name="executeManager">Python execution manager</param>
        /// <param name="config">Optional configuration</param>
        /// <returns>Configured multimodal pipeline</returns>
        public static async Task<MultimodalTransformerPipeline> CreateContentAnalysisPipelineAsync(
            IPythonRunTimeManager pythonRunTimeManager,
            IPythonCodeExecuteManager executeManager,
            MultimodalPipelineConfig? config = null)
        {
            config ??= new MultimodalPipelineConfig
            {
                PreloadPipelines = new List<MultimodalTask>
                {
                    MultimodalTask.ImageToText,
                    MultimodalTask.AudioToText,
                    MultimodalTask.VisualQuestionAnswering,
                    MultimodalTask.AudioClassification
                },
                DefaultQuality = "high",
                MaxConcurrentTasks = 3
            };

            var pipeline = new MultimodalTransformerPipeline(pythonRunTimeManager, executeManager);
            await pipeline.InitializeAsync(config);

            return pipeline;
        }

        /// <summary>
        /// Create a complete multimedia production pipeline
        /// Includes all major multimodal capabilities for professional content creation
        /// </summary>
        /// <param name="pythonRunTimeManager">Python runtime manager</param>
        /// <param name="executeManager">Python execution manager</param>
        /// <param name="config">Optional configuration</param>
        /// <returns>Configured multimodal pipeline</returns>
        public static async Task<MultimodalTransformerPipeline> CreateMultimediaProductionPipelineAsync(
            IPythonRunTimeManager pythonRunTimeManager,
            IPythonCodeExecuteManager executeManager,
            MultimodalPipelineConfig? config = null)
        {
            config ??= new MultimodalPipelineConfig
            {
                PreloadPipelines = new List<MultimodalTask>
                {
                    MultimodalTask.TextGeneration,
                    MultimodalTask.TextToImage,
                    MultimodalTask.TextToAudio,
                    MultimodalTask.ImageToText,
                    MultimodalTask.AudioToText,
                    MultimodalTask.TextToMusic
                },
                DefaultQuality = "ultra",
                MaxConcurrentTasks = 4,
                EnableCaching = true
            };

            var pipeline = new MultimodalTransformerPipeline(pythonRunTimeManager, executeManager);
            await pipeline.InitializeAsync(config);

            return pipeline;
        }

        /// <summary>
        /// Create a lightweight multimodal pipeline for basic tasks
        /// Optimized for speed and lower resource usage
        /// </summary>
        /// <param name="pythonRunTimeManager">Python runtime manager</param>
        /// <param name="executeManager">Python execution manager</param>
        /// <param name="primaryTask">Primary multimodal task</param>
        /// <returns>Configured multimodal pipeline</returns>
        public static async Task<MultimodalTransformerPipeline> CreateLightweightPipelineAsync(
            IPythonRunTimeManager pythonRunTimeManager,
            IPythonCodeExecuteManager executeManager,
            MultimodalTask primaryTask)
        {
            var config = new MultimodalPipelineConfig
            {
                PreloadPipelines = new List<MultimodalTask> { primaryTask },
                DefaultQuality = "medium",
                MaxConcurrentTasks = 1,
                EnableCaching = false
            };

            var pipeline = new MultimodalTransformerPipeline(pythonRunTimeManager, executeManager);
            await pipeline.InitializeAsync(config);

            return pipeline;
        }

        #endregion

        #region Specialized Pipelines

        /// <summary>
        /// Create a pipeline specifically for story creation with multimedia elements
        /// </summary>
        /// <param name="pythonRunTimeManager">Python runtime manager</param>
        /// <param name="executeManager">Python execution manager</param>
        /// <returns>Story-focused multimodal pipeline</returns>
        public static async Task<MultimodalTransformerPipeline> CreateStorytellingPipelineAsync(
            IPythonRunTimeManager pythonRunTimeManager,
            IPythonCodeExecuteManager executeManager)
        {
            var config = new MultimodalPipelineConfig
            {
                PreloadPipelines = new List<MultimodalTask>
                {
                    MultimodalTask.TextGeneration,
                    MultimodalTask.TextToImage,
                    MultimodalTask.TextToAudio,
                    MultimodalTask.MultimediaStoryGeneration
                },
                GlobalConfig = new Dictionary<string, object>
                {
                    ["story_mode"] = true,
                    ["default_style"] = "cinematic",
                    ["narrative_voice"] = "storyteller"
                },
                DefaultQuality = "high"
            };

            var pipeline = new MultimodalTransformerPipeline(pythonRunTimeManager, executeManager);
            await pipeline.InitializeAsync(config);

            return pipeline;
        }

        /// <summary>
        /// Create a pipeline for educational content creation
        /// </summary>
        /// <param name="pythonRunTimeManager">Python runtime manager</param>
        /// <param name="executeManager">Python execution manager</param>
        /// <returns>Education-focused multimodal pipeline</returns>
        public static async Task<MultimodalTransformerPipeline> CreateEducationalPipelineAsync(
            IPythonRunTimeManager pythonRunTimeManager,
            IPythonCodeExecuteManager executeManager)
        {
            var config = new MultimodalPipelineConfig
            {
                PreloadPipelines = new List<MultimodalTask>
                {
                    MultimodalTask.TextGeneration,
                    MultimodalTask.TextToImage,
                    MultimodalTask.TextToAudio,
                    MultimodalTask.PresentationGeneration
                },
                GlobalConfig = new Dictionary<string, object>
                {
                    ["education_mode"] = true,
                    ["default_style"] = "educational",
                    ["voice_style"] = "teacher",
                    ["target_audience"] = "students"
                },
                DefaultQuality = "high"
            };

            var pipeline = new MultimodalTransformerPipeline(pythonRunTimeManager, executeManager);
            await pipeline.InitializeAsync(config);

            return pipeline;
        }

        /// <summary>
        /// Create a pipeline for accessibility features
        /// Focus on converting between different modalities for accessibility
        /// </summary>
        /// <param name="pythonRunTimeManager">Python runtime manager</param>
        /// <param name="executeManager">Python execution manager</param>
        /// <returns>Accessibility-focused multimodal pipeline</returns>
        public static async Task<MultimodalTransformerPipeline> CreateAccessibilityPipelineAsync(
            IPythonRunTimeManager pythonRunTimeManager,
            IPythonCodeExecuteManager executeManager)
        {
            var config = new MultimodalPipelineConfig
            {
                PreloadPipelines = new List<MultimodalTask>
                {
                    MultimodalTask.ImageToText,
                    MultimodalTask.TextToAudio,
                    MultimodalTask.AudioToText,
                    MultimodalTask.VisualQuestionAnswering
                },
                GlobalConfig = new Dictionary<string, object>
                {
                    ["accessibility_mode"] = true,
                    ["detailed_descriptions"] = true,
                    ["clear_speech"] = true
                },
                DefaultQuality = "high"
            };

            var pipeline = new MultimodalTransformerPipeline(pythonRunTimeManager, executeManager);
            await pipeline.InitializeAsync(config);

            return pipeline;
        }

        #endregion

        #region Enterprise and Multi-User Methods

        /// <summary>
        /// Create a multimodal pipeline for enterprise multi-user environment
        /// Uses pre-existing session and virtual environment for proper isolation
        /// </summary>
        /// <param name="pythonRunTimeManager">Python runtime manager</param>
        /// <param name="executeManager">Python execution manager</param>
        /// <param name="session">Pre-existing Python session</param>
        /// <param name="virtualEnvironment">Virtual environment for the session</param>
        /// <param name="config">Optional configuration</param>
        /// <returns>Configured multimodal pipeline for enterprise use</returns>
        public static async Task<MultimodalTransformerPipeline> CreateEnterpriseMultiUserPipelineAsync(
            IPythonRunTimeManager pythonRunTimeManager,
            IPythonCodeExecuteManager executeManager,
            PythonSessionInfo session,
            PythonVirtualEnvironment virtualEnvironment,
            MultimodalPipelineConfig? config = null)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));
            
            if (virtualEnvironment == null)
                throw new ArgumentNullException(nameof(virtualEnvironment));

            config ??= new MultimodalPipelineConfig
            {
                PreloadPipelines = new List<MultimodalTask>
                {
                    MultimodalTask.TextGeneration,
                    MultimodalTask.TextToImage,
                    MultimodalTask.ImageToText,
                    MultimodalTask.AudioToText
                },
                DefaultQuality = "high",
                MaxConcurrentTasks = 3,
                EnableCaching = true,
                GlobalConfig = new Dictionary<string, object>
                {
                    ["enterprise_mode"] = true,
                    ["user"] = session.Username,
                    ["session_isolation"] = true
                }
            };

            var pipeline = new MultimodalTransformerPipeline(pythonRunTimeManager, executeManager);
            
            // Configure with the provided session and environment
            var sessionConfigured = pipeline.ConfigureSession(session, virtualEnvironment);
            if (!sessionConfigured)
            {
                throw new InvalidOperationException("Failed to configure pipeline with provided session");
            }

            // Initialize the pipeline
            var initialized = await pipeline.InitializeAsync(config);
            if (!initialized)
            {
                throw new InvalidOperationException("Failed to initialize multimodal pipeline");
            }

            return pipeline;
        }

        /// <summary>
        /// Create a session-aware pipeline using the session manager
        /// Automatically creates or reuses a session for the specified user
        /// </summary>
        /// <param name="pythonRunTimeManager">Python runtime manager</param>
        /// <param name="executeManager">Python execution manager</param>
        /// <param name="username">Username for session creation</param>
        /// <param name="environmentId">Specific environment ID, or null for auto-selection</param>
        /// <param name="config">Optional configuration</param>
        /// <returns>Configured multimodal pipeline with managed session</returns>
        public static async Task<MultimodalTransformerPipeline> CreateSessionAwarePipelineAsync(
            IPythonRunTimeManager pythonRunTimeManager,
            IPythonCodeExecuteManager executeManager,
            string username,
            string? environmentId = null,
            MultimodalPipelineConfig? config = null)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("Username cannot be null or empty", nameof(username));

            if (pythonRunTimeManager.SessionManager == null)
                throw new InvalidOperationException("Session manager is not available");

            // Create or get existing session for the user
            var session = pythonRunTimeManager.SessionManager.CreateSession(username, environmentId);
            if (session == null)
            {
                throw new InvalidOperationException($"Failed to create session for user: {username}");
            }

            // Get the virtual environment for this session
            var virtualEnvironment = pythonRunTimeManager.VirtualEnvmanager?.GetEnvironmentById(session.VirtualEnvironmentId);
            if (virtualEnvironment == null)
            {
                throw new InvalidOperationException($"Virtual environment not found for session: {session.SessionId}");
            }

            // Use the enterprise method with the managed session
            return await CreateEnterpriseMultiUserPipelineAsync(
                pythonRunTimeManager, 
                executeManager, 
                session, 
                virtualEnvironment, 
                config);
        }

        /// <summary>
        /// Create a pipeline for a specific virtual environment
        /// Useful when you want to use a pre-configured environment
        /// </summary>
        /// <param name="pythonRunTimeManager">Python runtime manager</param>
        /// <param name="executeManager">Python execution manager</param>
        /// <param name="virtualEnvironment">Target virtual environment</param>
        /// <param name="username">Username for session creation</param>
        /// <param name="config">Optional configuration</param>
        /// <returns>Configured multimodal pipeline</returns>
        public static async Task<MultimodalTransformerPipeline> CreateEnvironmentSpecificPipelineAsync(
            IPythonRunTimeManager pythonRunTimeManager,
            IPythonCodeExecuteManager executeManager,
            PythonVirtualEnvironment virtualEnvironment,
            string username,
            MultimodalPipelineConfig? config = null)
        {
            if (virtualEnvironment == null)
                throw new ArgumentNullException(nameof(virtualEnvironment));
            
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("Username cannot be null or empty", nameof(username));

            if (pythonRunTimeManager.SessionManager == null)
                throw new InvalidOperationException("Session manager is not available");

            // Create a session for this specific environment
            var session = pythonRunTimeManager.SessionManager.CreateSession(username, virtualEnvironment.ID);
            if (session == null)
            {
                throw new InvalidOperationException($"Failed to create session for environment: {virtualEnvironment.Name}");
            }

            return await CreateEnterpriseMultiUserPipelineAsync(
                pythonRunTimeManager, 
                executeManager, 
                session, 
                virtualEnvironment, 
                config);
        }

        #endregion

        #region Model Configuration Helpers

        /// <summary>
        /// Get default model configurations for specific multimodal tasks
        /// </summary>
        /// <param name="task">Multimodal task</param>
        /// <param name="quality">Quality level (low, medium, high, ultra)</param>
        /// <returns>Recommended model information</returns>
        public static TransformerModelInfo GetRecommendedModel(MultimodalTask task, string quality = "high")
        {
            return task switch
            {
                MultimodalTask.TextToImage => GetTextToImageModel(quality),
                MultimodalTask.TextToAudio => GetTextToAudioModel(quality),
                MultimodalTask.ImageToText => GetImageToTextModel(quality),
                MultimodalTask.AudioToText => GetAudioToTextModel(quality),
                MultimodalTask.TextGeneration => GetTextGenerationModel(quality),
                MultimodalTask.VisualQuestionAnswering => GetVisualQAModel(quality),
                MultimodalTask.AudioClassification => GetAudioClassificationModel(quality),
                MultimodalTask.TextToMusic => GetTextToMusicModel(quality),
                _ => GetDefaultModel(quality)
            };
        }

        /// <summary>
        /// Get default parameters for specific multimodal tasks
        /// </summary>
        /// <param name="task">Multimodal task</param>
        /// <param name="quality">Quality level</param>
        /// <returns>Default parameters object</returns>
        public static object GetDefaultParameters(MultimodalTask task, string quality = "high")
        {
            return task switch
            {
                MultimodalTask.TextToImage => new TextToImageParameters 
                { 
                    Quality = quality, 
                    NumInferenceSteps = quality == "ultra" ? 100 : 50 
                },
                MultimodalTask.TextToAudio => new TextToAudioParameters 
                { 
                    Quality = quality,
                    SampleRate = quality == "ultra" ? 44100 : 22050
                },
                MultimodalTask.ImageToText => new ImageToTextParameters 
                { 
                    Style = "descriptive",
                    DetailedAnalysis = quality is "high" or "ultra"
                },
                MultimodalTask.AudioToText => new SpeechToTextParameters 
                { 
                    IncludeTimestamps = quality is "high" or "ultra"
                },
                _ => new object()
            };
        }

        #endregion

        #region Private Helper Methods

        private static TransformerModelInfo GetTextToImageModel(string quality)
        {
            return quality switch
            {
                "ultra" => new TransformerModelInfo
                {
                    Name = "stabilityai/stable-diffusion-xl-base-1.0",
                    Source = TransformerModelSource.HuggingFace,
                    Architecture = "Stable Diffusion XL",
                    SupportedTasks = { TransformerTask.Custom }
                },
                "high" => new TransformerModelInfo
                {
                    Name = "runwayml/stable-diffusion-v1-5",
                    Source = TransformerModelSource.HuggingFace,
                    Architecture = "Stable Diffusion",
                    SupportedTasks = { TransformerTask.Custom }
                },
                _ => new TransformerModelInfo
                {
                    Name = "CompVis/stable-diffusion-v1-4",
                    Source = TransformerModelSource.HuggingFace,
                    Architecture = "Stable Diffusion",
                    SupportedTasks = { TransformerTask.Custom }
                }
            };
        }

        private static TransformerModelInfo GetTextToAudioModel(string quality)
        {
            return quality switch
            {
                "ultra" => new TransformerModelInfo
                {
                    Name = "microsoft/speecht5_tts",
                    Source = TransformerModelSource.HuggingFace,
                    Architecture = "SpeechT5",
                    SupportedTasks = { TransformerTask.TextToSpeech }
                },
                _ => new TransformerModelInfo
                {
                    Name = "microsoft/speecht5_tts",
                    Source = TransformerModelSource.HuggingFace,
                    Architecture = "SpeechT5",
                    SupportedTasks = { TransformerTask.TextToSpeech }
                }
            };
        }

        private static TransformerModelInfo GetImageToTextModel(string quality)
        {
            return quality switch
            {
                "ultra" => new TransformerModelInfo
                {
                    Name = "Salesforce/blip2-opt-6.7b",
                    Source = TransformerModelSource.HuggingFace,
                    Architecture = "BLIP-2",
                    SupportedTasks = { TransformerTask.ImageCaptioning }
                },
                _ => new TransformerModelInfo
                {
                    Name = "Salesforce/blip-image-captioning-base",
                    Source = TransformerModelSource.HuggingFace,
                    Architecture = "BLIP",
                    SupportedTasks = { TransformerTask.ImageCaptioning }
                }
            };
        }

        private static TransformerModelInfo GetAudioToTextModel(string quality)
        {
            return new TransformerModelInfo
            {
                Name = "openai/whisper-large-v3",
                Source = TransformerModelSource.HuggingFace,
                Architecture = "Whisper",
                SupportedTasks = { TransformerTask.AutomaticSpeechRecognition }
            };
        }

        private static TransformerModelInfo GetTextGenerationModel(string quality)
        {
            return quality switch
            {
                "ultra" => new TransformerModelInfo
                {
                    Name = "gpt-4",
                    Source = TransformerModelSource.OpenAI,
                    Architecture = "GPT-4",
                    SupportedTasks = { TransformerTask.TextGeneration }
                },
                "high" => new TransformerModelInfo
                {
                    Name = "gpt-3.5-turbo",
                    Source = TransformerModelSource.OpenAI,
                    Architecture = "GPT-3.5",
                    SupportedTasks = { TransformerTask.TextGeneration }
                },
                _ => new TransformerModelInfo
                {
                    Name = "microsoft/DialoGPT-large",
                    Source = TransformerModelSource.HuggingFace,
                    Architecture = "DialoGPT",
                    SupportedTasks = { TransformerTask.TextGeneration }
                }
            };
        }

        private static TransformerModelInfo GetVisualQAModel(string quality)
        {
            return new TransformerModelInfo
            {
                Name = "Salesforce/blip-vqa-base",
                Source = TransformerModelSource.HuggingFace,
                Architecture = "BLIP VQA",
                SupportedTasks = { TransformerTask.VisualQuestionAnswering }
            };
        }

        private static TransformerModelInfo GetAudioClassificationModel(string quality)
        {
            return new TransformerModelInfo
            {
                Name = "facebook/wav2vec2-base-960h",
                Source = TransformerModelSource.HuggingFace,
                Architecture = "Wav2Vec2",
                SupportedTasks = { TransformerTask.AudioClassification }
            };
        }

        private static TransformerModelInfo GetTextToMusicModel(string quality)
        {
            return new TransformerModelInfo
            {
                Name = "facebook/musicgen-small",
                Source = TransformerModelSource.HuggingFace,
                Architecture = "MusicGen",
                SupportedTasks = { TransformerTask.Custom }
            };
        }

        private static TransformerModelInfo GetDefaultModel(string quality)
        {
            return new TransformerModelInfo
            {
                Name = "bert-base-uncased",
                Source = TransformerModelSource.HuggingFace,
                Architecture = "BERT",
                SupportedTasks = { TransformerTask.Custom }
            };
        }

        #endregion
    }
}