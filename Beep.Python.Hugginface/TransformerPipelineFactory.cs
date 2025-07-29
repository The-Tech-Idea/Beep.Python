using System;
using System.Collections.Generic;
using Beep.Python.Model;

namespace Beep.Python.AI.Transformers
{
    /// <summary>
    /// Factory for creating transformer pipeline instances supporting multiple providers
    /// (HuggingFace, OpenAI, Azure, Local models, and custom sources)
    /// </summary>
    public static class TransformerPipelineFactory
    {
        /// <summary>
        /// Create a transformer pipeline based on the specified source
        /// </summary>
        /// <param name="source">Model source type</param>
        /// <param name="pythonRunTimeManager">Python runtime manager</param>
        /// <param name="executeManager">Python code execution manager</param>
        /// <returns>Transformer pipeline instance</returns>
        public static ITransformerPipeLine CreatePipeline(
            TransformerModelSource source,
            IPythonRunTimeManager pythonRunTimeManager,
            IPythonCodeExecuteManager executeManager)
        {
            return source switch
            {
                TransformerModelSource.HuggingFace => new HuggingFaceTransformerPipeline(pythonRunTimeManager, executeManager),
                TransformerModelSource.Local => new LocalTransformerPipeline(pythonRunTimeManager, executeManager),
                TransformerModelSource.OpenAI => new OpenAITransformerPipeline(pythonRunTimeManager, executeManager),
                TransformerModelSource.Azure => new AzureTransformerPipeline(pythonRunTimeManager, executeManager),
                TransformerModelSource.Google => new GoogleTransformerPipeline(pythonRunTimeManager, executeManager),
                TransformerModelSource.Anthropic => new AnthropicTransformerPipeline(pythonRunTimeManager, executeManager),
                TransformerModelSource.Cohere => new CohereTransformerPipeline(pythonRunTimeManager, executeManager),
                TransformerModelSource.Meta => new MetaTransformerPipeline(pythonRunTimeManager, executeManager),
                TransformerModelSource.Mistral => new MistralTransformerPipeline(pythonRunTimeManager, executeManager),
                TransformerModelSource.Custom => new CustomTransformerPipeline(pythonRunTimeManager, executeManager),
                _ => new HuggingFaceTransformerPipeline(pythonRunTimeManager, executeManager) // Default to HuggingFace
            };
        }

        /// <summary>
        /// Create a transformer pipeline with automatic source detection
        /// </summary>
        /// <param name="modelIdentifier">Model name, path, or URL</param>
        /// <param name="pythonRunTimeManager">Python runtime manager</param>
        /// <param name="executeManager">Python code execution manager</param>
        /// <returns>Transformer pipeline instance</returns>
        public static ITransformerPipeLine CreatePipelineAuto(
            string modelIdentifier,
            IPythonRunTimeManager pythonRunTimeManager,
            IPythonCodeExecuteManager executeManager)
        {
            var source = DetectModelSource(modelIdentifier);
            return CreatePipeline(source, pythonRunTimeManager, executeManager);
        }

        /// <summary>
        /// Create a pipeline with explicit configuration
        /// </summary>
        /// <param name="config">Pipeline configuration</param>
        /// <param name="pythonRunTimeManager">Python runtime manager</param>
        /// <param name="executeManager">Python code execution manager</param>
        /// <returns>Configured transformer pipeline instance</returns>
        public static ITransformerPipeLine CreatePipelineWithConfig(
            TransformerPipelineConfig config,
            IPythonRunTimeManager pythonRunTimeManager,
            IPythonCodeExecuteManager executeManager)
        {
            var pipeline = CreatePipeline(config.ModelSource, pythonRunTimeManager, executeManager);
            pipeline.PipelineConfig = config;
            return pipeline;
        }

        /// <summary>
        /// Get supported model sources
        /// </summary>
        /// <returns>List of supported sources</returns>
        public static List<TransformerModelSource> GetSupportedSources()
        {
            return new List<TransformerModelSource>
            {
                TransformerModelSource.HuggingFace,
                TransformerModelSource.Local,
                TransformerModelSource.OpenAI,
                TransformerModelSource.Azure,
                TransformerModelSource.Google,
                TransformerModelSource.Anthropic,
                TransformerModelSource.Cohere,
                TransformerModelSource.Meta,
                TransformerModelSource.Mistral,
                TransformerModelSource.Custom
            };
        }

        /// <summary>
        /// Check if a model source is supported
        /// </summary>
        /// <param name="source">Model source to check</param>
        /// <returns>True if supported</returns>
        public static bool IsSourceSupported(TransformerModelSource source)
        {
            return GetSupportedSources().Contains(source);
        }

        /// <summary>
        /// Get recommended model source for a specific task
        /// </summary>
        /// <param name="taskType">Type of transformer task</param>
        /// <returns>Recommended model source</returns>
        public static TransformerModelSource GetRecommendedSource(TransformerTask taskType)
        {
            return taskType switch
            {
                TransformerTask.TextGeneration => TransformerModelSource.HuggingFace,
                TransformerTask.Conversational => TransformerModelSource.OpenAI,
                TransformerTask.TextClassification => TransformerModelSource.HuggingFace,
                TransformerTask.NamedEntityRecognition => TransformerModelSource.HuggingFace,
                TransformerTask.QuestionAnswering => TransformerModelSource.HuggingFace,
                TransformerTask.Summarization => TransformerModelSource.HuggingFace,
                TransformerTask.Translation => TransformerModelSource.HuggingFace,
                TransformerTask.FeatureExtraction => TransformerModelSource.HuggingFace,
                _ => TransformerModelSource.HuggingFace
            };
        }

        private static TransformerModelSource DetectModelSource(string modelIdentifier)
        {
            if (string.IsNullOrWhiteSpace(modelIdentifier))
                return TransformerModelSource.HuggingFace;

            // Check if it's a local path
            if (System.IO.Path.IsPathRooted(modelIdentifier) || 
                System.IO.Directory.Exists(modelIdentifier) || 
                System.IO.File.Exists(modelIdentifier))
            {
                return TransformerModelSource.Local;
            }

            // Check for OpenAI models
            if (modelIdentifier.StartsWith("gpt-") || 
                modelIdentifier.StartsWith("text-") || 
                modelIdentifier.StartsWith("davinci") ||
                modelIdentifier.StartsWith("curie") ||
                modelIdentifier.StartsWith("babbage") ||
                modelIdentifier.StartsWith("ada") ||
                modelIdentifier.StartsWith("o1-"))
            {
                return TransformerModelSource.OpenAI;
            }

            // Check for Anthropic Claude models
            if (modelIdentifier.StartsWith("claude-"))
            {
                return TransformerModelSource.Anthropic;
            }

            // Check for Google models
            if (modelIdentifier.StartsWith("gemini-") || 
                modelIdentifier.StartsWith("palm-") ||
                modelIdentifier.StartsWith("bard-"))
            {
                return TransformerModelSource.Google;
            }

            // Check for Meta models
            if (modelIdentifier.StartsWith("llama-") ||
                modelIdentifier.StartsWith("codellama-"))
            {
                return TransformerModelSource.Meta;
            }

            // Check for Mistral models
            if (modelIdentifier.StartsWith("mistral-") ||
                modelIdentifier.StartsWith("mixtral-"))
            {
                return TransformerModelSource.Mistral;
            }

            // Check for Cohere models
            if (modelIdentifier.StartsWith("command-") ||
                modelIdentifier.StartsWith("embed-"))
            {
                return TransformerModelSource.Cohere;
            }

            // Check for custom URLs
            if (modelIdentifier.StartsWith("http://") || modelIdentifier.StartsWith("https://"))
            {
                return TransformerModelSource.Custom;
            }

            // Default to HuggingFace
            return TransformerModelSource.HuggingFace;
        }

        #region Enterprise and Multi-User Factory Methods

        /// <summary>
        /// Create a transformer pipeline for enterprise multi-user environment
        /// Uses pre-existing session and virtual environment for proper isolation
        /// </summary>
        /// <param name="source">Model source type</param>
        /// <param name="pythonRunTimeManager">Python runtime manager</param>
        /// <param name="executeManager">Python execution manager</param>
        /// <param name="session">Pre-existing Python session</param>
        /// <param name="virtualEnvironment">Virtual environment for the session</param>
        /// <param name="config">Optional configuration</param>
        /// <returns>Configured transformer pipeline for enterprise use</returns>
        public static ITransformerPipeLine CreateEnterpriseMultiUserPipeline(
            TransformerModelSource source,
            IPythonRunTimeManager pythonRunTimeManager,
            IPythonCodeExecuteManager executeManager,
            PythonSessionInfo session,
            PythonVirtualEnvironment virtualEnvironment,
            TransformerPipelineConfig? config = null)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));
            
            if (virtualEnvironment == null)
                throw new ArgumentNullException(nameof(virtualEnvironment));

            var pipeline = CreatePipeline(source, pythonRunTimeManager, executeManager);
            
            // Configure with the provided session and environment
            if (pipeline is BaseTransformerPipeline basePipeline)
            {
                var sessionConfigured = basePipeline.ConfigureSession(session, virtualEnvironment);
                if (!sessionConfigured)
                {
                    throw new InvalidOperationException("Failed to configure pipeline with provided session");
                }
            }

            // Apply configuration if provided
            if (config != null)
            {
                pipeline.PipelineConfig = config;
            }

            return pipeline;
        }

        /// <summary>
        /// Create a session-aware pipeline using the session manager
        /// Automatically creates or reuses a session for the specified user
        /// </summary>
        /// <param name="source">Model source type</param>
        /// <param name="pythonRunTimeManager">Python runtime manager</param>
        /// <param name="executeManager">Python execution manager</param>
        /// <param name="username">Username for session creation</param>
        /// <param name="environmentId">Specific environment ID, or null for auto-selection</param>
        /// <param name="config">Optional configuration</param>
        /// <returns>Configured transformer pipeline with managed session</returns>
        public static ITransformerPipeLine CreateSessionAwarePipeline(
            TransformerModelSource source,
            IPythonRunTimeManager pythonRunTimeManager,
            IPythonCodeExecuteManager executeManager,
            string username,
            string? environmentId = null,
            TransformerPipelineConfig? config = null)
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
            return CreateEnterpriseMultiUserPipeline(
                source,
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
        /// <param name="source">Model source type</param>
        /// <param name="pythonRunTimeManager">Python runtime manager</param>
        /// <param name="executeManager">Python execution manager</param>
        /// <param name="virtualEnvironment">Target virtual environment</param>
        /// <param name="username">Username for session creation</param>
        /// <param name="config">Optional configuration</param>
        /// <returns>Configured transformer pipeline</returns>
        public static ITransformerPipeLine CreateEnvironmentSpecificPipeline(
            TransformerModelSource source,
            IPythonRunTimeManager pythonRunTimeManager,
            IPythonCodeExecuteManager executeManager,
            PythonVirtualEnvironment virtualEnvironment,
            string username,
            TransformerPipelineConfig? config = null)
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

            return CreateEnterpriseMultiUserPipeline(
                source,
                pythonRunTimeManager, 
                executeManager, 
                session, 
                virtualEnvironment, 
                config);
        }

        /// <summary>
        /// Create pipeline with automatic model source detection for enterprise use
        /// </summary>
        /// <param name="modelIdentifier">Model name, path, or URL</param>
        /// <param name="pythonRunTimeManager">Python runtime manager</param>
        /// <param name="executeManager">Python execution manager</param>
        /// <param name="session">Pre-existing Python session</param>
        /// <param name="virtualEnvironment">Virtual environment for the session</param>
        /// <param name="config">Optional configuration</param>
        /// <returns>Configured transformer pipeline</returns>
        public static ITransformerPipeLine CreateEnterpriseAutoDetectPipeline(
            string modelIdentifier,
            IPythonRunTimeManager pythonRunTimeManager,
            IPythonCodeExecuteManager executeManager,
            PythonSessionInfo session,
            PythonVirtualEnvironment virtualEnvironment,
            TransformerPipelineConfig? config = null)
        {
            var source = DetectModelSource(modelIdentifier);
            return CreateEnterpriseMultiUserPipeline(
                source,
                pythonRunTimeManager,
                executeManager,
                session,
                virtualEnvironment,
                config);
        }

        #endregion

        #region Connection-Aware Factory Methods

        /// <summary>
        /// Create a transformer pipeline with connection configuration
        /// </summary>
        /// <param name="source">Model source type</param>
        /// <param name="pythonRunTimeManager">Python runtime manager</param>
        /// <param name="executeManager">Python execution manager</param>
        /// <param name="connectionConfig">Connection configuration for the provider</param>
        /// <param name="pipelineConfig">Optional pipeline configuration</param>
        /// <returns>Configured transformer pipeline</returns>
        public static ITransformerPipeLine CreatePipelineWithConnection(
            TransformerModelSource source,
            IPythonRunTimeManager pythonRunTimeManager,
            IPythonCodeExecuteManager executeManager,
            TransformerConnectionConfig connectionConfig,
            TransformerPipelineConfig? pipelineConfig = null)
        {
            var pipeline = CreatePipeline(source, pythonRunTimeManager, executeManager);
            
            // Configure connection
            if (pipeline is BaseTransformerPipeline basePipeline)
            {
                basePipeline.ConfigureConnection(connectionConfig);
            }

            // Apply pipeline configuration if provided
            if (pipelineConfig != null)
            {
                pipeline.PipelineConfig = pipelineConfig;
            }

            return pipeline;
        }

        /// <summary>
        /// Create enterprise pipeline with connection and session configuration
        /// </summary>
        /// <param name="source">Model source type</param>
        /// <param name="pythonRunTimeManager">Python runtime manager</param>
        /// <param name="executeManager">Python execution manager</param>
        /// <param name="session">Pre-existing Python session</param>
        /// <param name="virtualEnvironment">Virtual environment for the session</param>
        /// <param name="connectionConfig">Connection configuration for the provider</param>
        /// <param name="pipelineConfig">Optional pipeline configuration</param>
        /// <returns>Fully configured enterprise transformer pipeline</returns>
        public static ITransformerPipeLine CreateEnterpriseMultiUserPipelineWithConnection(
            TransformerModelSource source,
            IPythonRunTimeManager pythonRunTimeManager,
            IPythonCodeExecuteManager executeManager,
            PythonSessionInfo session,
            PythonVirtualEnvironment virtualEnvironment,
            TransformerConnectionConfig connectionConfig,
            TransformerPipelineConfig? pipelineConfig = null)
        {
            var pipeline = CreateEnterpriseMultiUserPipeline(
                source,
                pythonRunTimeManager,
                executeManager,
                session,
                virtualEnvironment,
                pipelineConfig);

            // Configure connection
            if (pipeline is BaseTransformerPipeline basePipeline)
            {
                basePipeline.ConfigureConnection(connectionConfig);
            }

            return pipeline;
        }

        /// <summary>
        /// Create session-aware pipeline with connection configuration
        /// </summary>
        /// <param name="source">Model source type</param>
        /// <param name="pythonRunTimeManager">Python runtime manager</param>
        /// <param name="executeManager">Python execution manager</param>
        /// <param name="username">Username for session creation</param>
        /// <param name="connectionConfig">Connection configuration for the provider</param>
        /// <param name="environmentId">Specific environment ID, or null for auto-selection</param>
        /// <param name="pipelineConfig">Optional pipeline configuration</param>
        /// <returns>Configured transformer pipeline with managed session and connection</returns>
        public static ITransformerPipeLine CreateSessionAwarePipelineWithConnection(
            TransformerModelSource source,
            IPythonRunTimeManager pythonRunTimeManager,
            IPythonCodeExecuteManager executeManager,
            string username,
            TransformerConnectionConfig connectionConfig,
            string? environmentId = null,
            TransformerPipelineConfig? pipelineConfig = null)
        {
            var pipeline = CreateSessionAwarePipeline(
                source,
                pythonRunTimeManager,
                executeManager,
                username,
                environmentId,
                pipelineConfig);

            // Configure connection
            if (pipeline is BaseTransformerPipeline basePipeline)
            {
                basePipeline.ConfigureConnection(connectionConfig);
            }

            return pipeline;
        }

        #endregion

        #region Provider-Specific Factory Methods

        /// <summary>
        /// Create OpenAI pipeline with connection configuration
        /// </summary>
        /// <param name="pythonRunTimeManager">Python runtime manager</param>
        /// <param name="executeManager">Python execution manager</param>
        /// <param name="openAIConfig">OpenAI connection configuration</param>
        /// <param name="session">Optional Python session</param>
        /// <param name="virtualEnvironment">Optional virtual environment</param>
        /// <returns>Configured OpenAI pipeline</returns>
        public static ITransformerPipeLine CreateOpenAIPipeline(
            IPythonRunTimeManager pythonRunTimeManager,
            IPythonCodeExecuteManager executeManager,
            OpenAIConnectionConfig openAIConfig,
            PythonSessionInfo? session = null,
            PythonVirtualEnvironment? virtualEnvironment = null)
        {
            if (session != null && virtualEnvironment != null)
            {
                return CreateEnterpriseMultiUserPipelineWithConnection(
                    TransformerModelSource.OpenAI,
                    pythonRunTimeManager,
                    executeManager,
                    session,
                    virtualEnvironment,
                    openAIConfig);
            }
            else
            {
                return CreatePipelineWithConnection(
                    TransformerModelSource.OpenAI,
                    pythonRunTimeManager,
                    executeManager,
                    openAIConfig);
            }
        }

        /// <summary>
        /// Create Azure OpenAI pipeline with connection configuration
        /// </summary>
        /// <param name="pythonRunTimeManager">Python runtime manager</param>
        /// <param name="executeManager">Python execution manager</param>
        /// <param name="azureConfig">Azure OpenAI connection configuration</param>
        /// <param name="session">Optional Python session</param>
        /// <param name="virtualEnvironment">Optional virtual environment</param>
        /// <returns>Configured Azure OpenAI pipeline</returns>
        public static ITransformerPipeLine CreateAzureOpenAIPipeline(
            IPythonRunTimeManager pythonRunTimeManager,
            IPythonCodeExecuteManager executeManager,
            AzureOpenAIConnectionConfig azureConfig,
            PythonSessionInfo? session = null,
            PythonVirtualEnvironment? virtualEnvironment = null)
        {
            if (session != null && virtualEnvironment != null)
            {
                return CreateEnterpriseMultiUserPipelineWithConnection(
                    TransformerModelSource.Azure,
                    pythonRunTimeManager,
                    executeManager,
                    session,
                    virtualEnvironment,
                    azureConfig);
            }
            else
            {
                return CreatePipelineWithConnection(
                    TransformerModelSource.Azure,
                    pythonRunTimeManager,
                    executeManager,
                    azureConfig);
            }
        }

        /// <summary>
        /// Create Google AI pipeline with connection configuration
        /// </summary>
        /// <param name="pythonRunTimeManager">Python runtime manager</param>
        /// <param name="executeManager">Python execution manager</param>
        /// <param name="googleConfig">Google AI connection configuration</param>
        /// <param name="session">Optional Python session</param>
        /// <param name="virtualEnvironment">Optional virtual environment</param>
        /// <returns>Configured Google AI pipeline</returns>
        public static ITransformerPipeLine CreateGoogleAIPipeline(
            IPythonRunTimeManager pythonRunTimeManager,
            IPythonCodeExecuteManager executeManager,
            GoogleAIConnectionConfig googleConfig,
            PythonSessionInfo? session = null,
            PythonVirtualEnvironment? virtualEnvironment = null)
        {
            if (session != null && virtualEnvironment != null)
            {
                return CreateEnterpriseMultiUserPipelineWithConnection(
                    TransformerModelSource.Google,
                    pythonRunTimeManager,
                    executeManager,
                    session,
                    virtualEnvironment,
                    googleConfig);
            }
            else
            {
                return CreatePipelineWithConnection(
                    TransformerModelSource.Google,
                    pythonRunTimeManager,
                    executeManager,
                    googleConfig);
            }
        }

        /// <summary>
        /// Create Anthropic pipeline with connection configuration
        /// </summary>
        /// <param name="pythonRunTimeManager">Python runtime manager</param>
        /// <param name="executeManager">Python execution manager</param>
        /// <param name="anthropicConfig">Anthropic connection configuration</param>
        /// <param name="session">Optional Python session</param>
        /// <param name="virtualEnvironment">Optional virtual environment</param>
        /// <returns>Configured Anthropic pipeline</returns>
        public static ITransformerPipeLine CreateAnthropicPipeline(
            IPythonRunTimeManager pythonRunTimeManager,
            IPythonCodeExecuteManager executeManager,
            AnthropicConnectionConfig anthropicConfig,
            PythonSessionInfo? session = null,
            PythonVirtualEnvironment? virtualEnvironment = null)
        {
            if (session != null && virtualEnvironment != null)
            {
                return CreateEnterpriseMultiUserPipelineWithConnection(
                    TransformerModelSource.Anthropic,
                    pythonRunTimeManager,
                    executeManager,
                    session,
                    virtualEnvironment,
                    anthropicConfig);
            }
            else
            {
                return CreatePipelineWithConnection(
                    TransformerModelSource.Anthropic,
                    pythonRunTimeManager,
                    executeManager,
                    anthropicConfig);
            }
        }

        /// <summary>
        /// Create HuggingFace pipeline with connection configuration
        /// </summary>
        /// <param name="pythonRunTimeManager">Python runtime manager</param>
        /// <param name="executeManager">Python execution manager</param>
        /// <param name="hfConfig">HuggingFace connection configuration</param>
        /// <param name="session">Optional Python session</param>
        /// <param name="virtualEnvironment">Optional virtual environment</param>
        /// <returns>Configured HuggingFace pipeline</returns>
        public static ITransformerPipeLine CreateHuggingFacePipeline(
            IPythonRunTimeManager pythonRunTimeManager,
            IPythonCodeExecuteManager executeManager,
            HuggingFaceConnectionConfig hfConfig,
            PythonSessionInfo? session = null,
            PythonVirtualEnvironment? virtualEnvironment = null)
        {
            if (session != null && virtualEnvironment != null)
            {
                return CreateEnterpriseMultiUserPipelineWithConnection(
                    TransformerModelSource.HuggingFace,
                    pythonRunTimeManager,
                    executeManager,
                    session,
                    virtualEnvironment,
                    hfConfig);
            }
            else
            {
                return CreatePipelineWithConnection(
                    TransformerModelSource.HuggingFace,
                    pythonRunTimeManager,
                    executeManager,
                    hfConfig);
            }
        }

        #endregion
    }
}