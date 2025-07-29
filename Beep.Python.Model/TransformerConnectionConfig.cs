using System;
using System.Collections.Generic;

namespace Beep.Python.Model
{
    #region Connection Configuration Classes

    /// <summary>
    /// Base class for transformer service connection configuration
    /// </summary>
    public abstract class TransformerConnectionConfig
    {
        /// <summary>
        /// Provider name
        /// </summary>
        public string ProviderName { get; set; } = string.Empty;

        /// <summary>
        /// Connection timeout in seconds
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Maximum retry attempts
        /// </summary>
        public int MaxRetries { get; set; } = 3;

        /// <summary>
        /// Custom headers to include in requests
        /// </summary>
        public Dictionary<string, string> CustomHeaders { get; set; } = new();

        /// <summary>
        /// Additional connection parameters
        /// </summary>
        public Dictionary<string, object> AdditionalParameters { get; set; } = new();

        /// <summary>
        /// Whether to use SSL/TLS
        /// </summary>
        public bool UseSSL { get; set; } = true;

        /// <summary>
        /// Proxy configuration
        /// </summary>
        public ProxyConfig? ProxyConfig { get; set; }
    }

    /// <summary>
    /// OpenAI API connection configuration
    /// </summary>
    public class OpenAIConnectionConfig : TransformerConnectionConfig
    {
        public OpenAIConnectionConfig()
        {
            ProviderName = "OpenAI";
        }

        /// <summary>
        /// OpenAI API key
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// Organization ID (optional)
        /// </summary>
        public string? OrganizationId { get; set; }

        /// <summary>
        /// Project ID (optional)
        /// </summary>
        public string? ProjectId { get; set; }

        /// <summary>
        /// API endpoint URL (default: https://api.openai.com/v1)
        /// </summary>
        public string ApiEndpoint { get; set; } = "https://api.openai.com/v1";

        /// <summary>
        /// API version
        /// </summary>
        public string ApiVersion { get; set; } = "v1";

        /// <summary>
        /// Rate limiting configuration
        /// </summary>
        public RateLimitConfig RateLimit { get; set; } = new();
    }

    /// <summary>
    /// Azure OpenAI connection configuration
    /// </summary>
    public class AzureOpenAIConnectionConfig : TransformerConnectionConfig
    {
        public AzureOpenAIConnectionConfig()
        {
            ProviderName = "Azure OpenAI";
        }

        /// <summary>
        /// Azure OpenAI API key
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// Azure OpenAI endpoint URL
        /// </summary>
        public string Endpoint { get; set; } = string.Empty;

        /// <summary>
        /// API version (e.g., 2024-02-01)
        /// </summary>
        public string ApiVersion { get; set; } = "2024-02-01";

        /// <summary>
        /// Deployment name
        /// </summary>
        public string DeploymentName { get; set; } = string.Empty;

        /// <summary>
        /// Azure subscription ID
        /// </summary>
        public string? SubscriptionId { get; set; }

        /// <summary>
        /// Azure resource group
        /// </summary>
        public string? ResourceGroup { get; set; }

        /// <summary>
        /// Azure Active Directory authentication
        /// </summary>
        public AzureADConfig? AzureAD { get; set; }
    }

    /// <summary>
    /// Google AI connection configuration
    /// </summary>
    public class GoogleAIConnectionConfig : TransformerConnectionConfig
    {
        public GoogleAIConnectionConfig()
        {
            ProviderName = "Google AI";
        }

        /// <summary>
        /// Google AI API key
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// Project ID
        /// </summary>
        public string ProjectId { get; set; } = string.Empty;

        /// <summary>
        /// Service endpoint
        /// </summary>
        public string Endpoint { get; set; } = "https://generativelanguage.googleapis.com";

        /// <summary>
        /// API version
        /// </summary>
        public string ApiVersion { get; set; } = "v1beta";

        /// <summary>
        /// Location/Region
        /// </summary>
        public string Location { get; set; } = "us-central1";

        /// <summary>
        /// Service account configuration
        /// </summary>
        public GoogleServiceAccountConfig? ServiceAccount { get; set; }
    }

    /// <summary>
    /// Anthropic Claude connection configuration
    /// </summary>
    public class AnthropicConnectionConfig : TransformerConnectionConfig
    {
        public AnthropicConnectionConfig()
        {
            ProviderName = "Anthropic";
        }

        /// <summary>
        /// Anthropic API key
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// API endpoint URL
        /// </summary>
        public string ApiEndpoint { get; set; } = "https://api.anthropic.com";

        /// <summary>
        /// API version
        /// </summary>
        public string ApiVersion { get; set; } = "2023-06-01";

        /// <summary>
        /// Beta features to enable
        /// </summary>
        public List<string> BetaFeatures { get; set; } = new();
    }

    /// <summary>
    /// Cohere connection configuration
    /// </summary>
    public class CohereConnectionConfig : TransformerConnectionConfig
    {
        public CohereConnectionConfig()
        {
            ProviderName = "Cohere";
        }

        /// <summary>
        /// Cohere API key
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// API endpoint URL
        /// </summary>
        public string ApiEndpoint { get; set; } = "https://api.cohere.ai";

        /// <summary>
        /// API version
        /// </summary>
        public string ApiVersion { get; set; } = "v1";

        /// <summary>
        /// Client name for tracking
        /// </summary>
        public string ClientName { get; set; } = "Beep.Python.AI.Transformers";
    }

    /// <summary>
    /// Meta (Llama) connection configuration
    /// </summary>
    public class MetaConnectionConfig : TransformerConnectionConfig
    {
        public MetaConnectionConfig()
        {
            ProviderName = "Meta";
        }

        /// <summary>
        /// Access token
        /// </summary>
        public string AccessToken { get; set; } = string.Empty;

        /// <summary>
        /// API endpoint URL
        /// </summary>
        public string ApiEndpoint { get; set; } = "https://api.llama-api.com";

        /// <summary>
        /// Model provider (e.g., "together", "fireworks", "replicate")
        /// </summary>
        public string ModelProvider { get; set; } = "together";

        /// <summary>
        /// Provider-specific configuration
        /// </summary>
        public Dictionary<string, object> ProviderConfig { get; set; } = new();
    }

    /// <summary>
    /// Mistral AI connection configuration
    /// </summary>
    public class MistralConnectionConfig : TransformerConnectionConfig
    {
        public MistralConnectionConfig()
        {
            ProviderName = "Mistral";
        }

        /// <summary>
        /// Mistral API key
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// API endpoint URL
        /// </summary>
        public string ApiEndpoint { get; set; } = "https://api.mistral.ai";

        /// <summary>
        /// API version
        /// </summary>
        public string ApiVersion { get; set; } = "v1";
    }

    /// <summary>
    /// HuggingFace Hub connection configuration
    /// </summary>
    public class HuggingFaceConnectionConfig : TransformerConnectionConfig
    {
        public HuggingFaceConnectionConfig()
        {
            ProviderName = "HuggingFace";
        }

        /// <summary>
        /// HuggingFace token for private models
        /// </summary>
        public string? Token { get; set; }

        /// <summary>
        /// Hub endpoint URL
        /// </summary>
        public string HubEndpoint { get; set; } = "https://huggingface.co";

        /// <summary>
        /// Inference API endpoint
        /// </summary>
        public string InferenceEndpoint { get; set; } = "https://api-inference.huggingface.co";

        /// <summary>
        /// Whether to use inference API or local models
        /// </summary>
        public bool UseInferenceAPI { get; set; } = false;

        /// <summary>
        /// Cache directory for downloaded models
        /// </summary>
        public string? CacheDir { get; set; }

        /// <summary>
        /// Offline mode - only use cached models
        /// </summary>
        public bool OfflineMode { get; set; } = false;
    }

    /// <summary>
    /// Custom API connection configuration
    /// </summary>
    public class CustomConnectionConfig : TransformerConnectionConfig
    {
        public CustomConnectionConfig()
        {
            ProviderName = "Custom";
        }

        /// <summary>
        /// API endpoint URL
        /// </summary>
        public string ApiEndpoint { get; set; } = string.Empty;

        /// <summary>
        /// Authentication method
        /// </summary>
        public AuthenticationMethod AuthMethod { get; set; } = AuthenticationMethod.ApiKey;

        /// <summary>
        /// Authentication credentials
        /// </summary>
        public Dictionary<string, string> Credentials { get; set; } = new();

        /// <summary>
        /// Custom request format
        /// </summary>
        public RequestFormat RequestFormat { get; set; } = RequestFormat.JSON;

        /// <summary>
        /// Custom response parser
        /// </summary>
        public string? ResponseParser { get; set; }
    }

    #endregion

    #region Supporting Configuration Classes

    /// <summary>
    /// Proxy configuration
    /// </summary>
    public class ProxyConfig
    {
        /// <summary>
        /// Proxy host
        /// </summary>
        public string Host { get; set; } = string.Empty;

        /// <summary>
        /// Proxy port
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Proxy username
        /// </summary>
        public string? Username { get; set; }

        /// <summary>
        /// Proxy password
        /// </summary>
        public string? Password { get; set; }

        /// <summary>
        /// Proxy type
        /// </summary>
        public ProxyType Type { get; set; } = ProxyType.HTTP;
    }

    /// <summary>
    /// Rate limiting configuration
    /// </summary>
    public class RateLimitConfig
    {
        /// <summary>
        /// Requests per minute
        /// </summary>
        public int RequestsPerMinute { get; set; } = 60;

        /// <summary>
        /// Tokens per minute
        /// </summary>
        public int TokensPerMinute { get; set; } = 150000;

        /// <summary>
        /// Concurrent requests limit
        /// </summary>
        public int ConcurrentRequests { get; set; } = 10;

        /// <summary>
        /// Enable rate limiting
        /// </summary>
        public bool Enabled { get; set; } = true;
    }

    /// <summary>
    /// Azure Active Directory configuration
    /// </summary>
    public class AzureADConfig
    {
        /// <summary>
        /// Tenant ID
        /// </summary>
        public string TenantId { get; set; } = string.Empty;

        /// <summary>
        /// Client ID
        /// </summary>
        public string ClientId { get; set; } = string.Empty;

        /// <summary>
        /// Client secret
        /// </summary>
        public string ClientSecret { get; set; } = string.Empty;

        /// <summary>
        /// Authentication scope
        /// </summary>
        public string Scope { get; set; } = "https://cognitiveservices.azure.com/.default";
    }

    /// <summary>
    /// Google Service Account configuration
    /// </summary>
    public class GoogleServiceAccountConfig
    {
        /// <summary>
        /// Service account key file path
        /// </summary>
        public string KeyFilePath { get; set; } = string.Empty;

        /// <summary>
        /// Service account email
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Private key
        /// </summary>
        public string PrivateKey { get; set; } = string.Empty;

        /// <summary>
        /// Scopes
        /// </summary>
        public List<string> Scopes { get; set; } = new();
    }

    #endregion

    #region Enums

    /// <summary>
    /// Authentication methods
    /// </summary>
    public enum AuthenticationMethod
    {
        None,
        ApiKey,
        Bearer,
        Basic,
        OAuth2,
        Custom
    }

    /// <summary>
    /// Request formats
    /// </summary>
    public enum RequestFormat
    {
        JSON,
        XML,
        FormData,
        Custom
    }

    /// <summary>
    /// Proxy types
    /// </summary>
    public enum ProxyType
    {
        HTTP,
        HTTPS,
        SOCKS4,
        SOCKS5
    }

    #endregion

    #region Connection Manager

    /// <summary>
    /// Manages transformer service connections and configurations
    /// </summary>
    public class TransformerConnectionManager
    {
        private readonly Dictionary<TransformerModelSource, TransformerConnectionConfig> _connections = new();
        
        /// <summary>
        /// Register connection configuration for a provider
        /// </summary>
        /// <param name="source">Provider source</param>
        /// <param name="config">Connection configuration</param>
        public void RegisterConnection(TransformerModelSource source, TransformerConnectionConfig config)
        {
            _connections[source] = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// Get connection configuration for a provider
        /// </summary>
        /// <param name="source">Provider source</param>
        /// <returns>Connection configuration or null if not found</returns>
        public TransformerConnectionConfig? GetConnection(TransformerModelSource source)
        {
            return _connections.TryGetValue(source, out var config) ? config : null;
        }

        /// <summary>
        /// Check if connection is configured for a provider
        /// </summary>
        /// <param name="source">Provider source</param>
        /// <returns>True if configured</returns>
        public bool IsConfigured(TransformerModelSource source)
        {
            return _connections.ContainsKey(source);
        }

        /// <summary>
        /// Remove connection configuration
        /// </summary>
        /// <param name="source">Provider source</param>
        /// <returns>True if removed</returns>
        public bool RemoveConnection(TransformerModelSource source)
        {
            return _connections.Remove(source);
        }

        /// <summary>
        /// Get all configured providers
        /// </summary>
        /// <returns>List of configured providers</returns>
        public List<TransformerModelSource> GetConfiguredProviders()
        {
            return new List<TransformerModelSource>(_connections.Keys);
        }

        /// <summary>
        /// Validate connection configuration
        /// </summary>
        /// <param name="source">Provider source</param>
        /// <returns>Validation result</returns>
        public ConnectionValidationResult ValidateConnection(TransformerModelSource source)
        {
            if (!_connections.TryGetValue(source, out var config))
            {
                return new ConnectionValidationResult
                {
                    IsValid = false,
                    Errors = { $"No configuration found for {source}" }
                };
            }

            return ValidateConnectionConfig(config);
        }

        private ConnectionValidationResult ValidateConnectionConfig(TransformerConnectionConfig config)
        {
            var result = new ConnectionValidationResult { IsValid = true };

            switch (config)
            {
                case OpenAIConnectionConfig openAI:
                    if (string.IsNullOrEmpty(openAI.ApiKey))
                        result.Errors.Add("OpenAI API key is required");
                    break;

                case AzureOpenAIConnectionConfig azure:
                    if (string.IsNullOrEmpty(azure.ApiKey))
                        result.Errors.Add("Azure OpenAI API key is required");
                    if (string.IsNullOrEmpty(azure.Endpoint))
                        result.Errors.Add("Azure OpenAI endpoint is required");
                    break;

                case GoogleAIConnectionConfig google:
                    if (string.IsNullOrEmpty(google.ApiKey) && google.ServiceAccount == null)
                        result.Errors.Add("Google AI API key or service account is required");
                    break;

                case AnthropicConnectionConfig anthropic:
                    if (string.IsNullOrEmpty(anthropic.ApiKey))
                        result.Errors.Add("Anthropic API key is required");
                    break;

                case CohereConnectionConfig cohere:
                    if (string.IsNullOrEmpty(cohere.ApiKey))
                        result.Errors.Add("Cohere API key is required");
                    break;

                case MetaConnectionConfig meta:
                    if (string.IsNullOrEmpty(meta.AccessToken))
                        result.Errors.Add("Meta access token is required");
                    break;

                case MistralConnectionConfig mistral:
                    if (string.IsNullOrEmpty(mistral.ApiKey))
                        result.Errors.Add("Mistral API key is required");
                    break;

                case CustomConnectionConfig custom:
                    if (string.IsNullOrEmpty(custom.ApiEndpoint))
                        result.Errors.Add("Custom API endpoint is required");
                    break;
            }

            result.IsValid = result.Errors.Count == 0;
            return result;
        }
    }

    /// <summary>
    /// Connection validation result
    /// </summary>
    public class ConnectionValidationResult
    {
        /// <summary>
        /// Whether the connection configuration is valid
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Validation errors
        /// </summary>
        public List<string> Errors { get; set; } = new();

        /// <summary>
        /// Validation warnings
        /// </summary>
        public List<string> Warnings { get; set; } = new();
    }

    #endregion
}