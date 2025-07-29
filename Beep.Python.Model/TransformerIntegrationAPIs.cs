using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Beep.Python.Model
{
    #region API Gateway and Integration

    /// <summary>
    /// API Gateway for transformer services with advanced routing and management
    /// </summary>
    public interface ITransformerApiGateway
    {
        Task<ApiResponse<T>> RouteRequestAsync<T>(ApiRequest request);
        Task<bool> RegisterEndpointAsync(ApiEndpoint endpoint);
        Task<List<ApiEndpoint>> GetAvailableEndpointsAsync();
        Task<ApiMetrics> GetEndpointMetricsAsync(string endpointId);
        Task<bool> ApplyRateLimitingAsync(string endpointId, RateLimitPolicy policy);
        Task<bool> ConfigureLoadBalancingAsync(string endpointId, LoadBalancingStrategy strategy);
        Task<bool> EnableCachingAsync(string endpointId, CachingPolicy policy);
        Task<ApiHealthStatus> GetHealthStatusAsync();
    }

    /// <summary>
    /// API metrics data
    /// </summary>
    public class ApiMetrics
    {
        public string EndpointId { get; set; } = string.Empty;
        public int TotalRequests { get; set; }
        public int SuccessfulRequests { get; set; }
        public int FailedRequests { get; set; }
        public double AverageResponseTime { get; set; }
        public double P95ResponseTime { get; set; }
        public double P99ResponseTime { get; set; }
        public Dictionary<int, int> StatusCodeCounts { get; set; } = new();
        public DateTime LastRequest { get; set; }
        public double RequestsPerSecond { get; set; }
    }

    /// <summary>
    /// Load balancing strategy configuration
    /// </summary>
    public class LoadBalancingStrategy
    {
        public LoadBalancingMethod Method { get; set; } = LoadBalancingMethod.RoundRobin;
        public List<LoadBalancingTarget> Targets { get; set; } = new();
        public HealthCheckConfig HealthCheck { get; set; } = new();
        public int MaxFailures { get; set; } = 3;
        public TimeSpan FailureTimeout { get; set; } = TimeSpan.FromMinutes(5);
    }

    /// <summary>
    /// Load balancing target
    /// </summary>
    public class LoadBalancingTarget
    {
        public string Id { get; set; } = string.Empty;
        public string Endpoint { get; set; } = string.Empty;
        public int Weight { get; set; } = 1;
        public bool IsHealthy { get; set; } = true;
        public DateTime LastHealthCheck { get; set; }
        public int CurrentConnections { get; set; }
    }

    /// <summary>
    /// Health check configuration
    /// </summary>
    public class HealthCheckConfig
    {
        public string Path { get; set; } = "/health";
        public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(30);
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(5);
        public int HealthyThreshold { get; set; } = 2;
        public int UnhealthyThreshold { get; set; } = 3;
    }

    /// <summary>
    /// Caching policy configuration
    /// </summary>
    public class CachingPolicy
    {
        public bool IsEnabled { get; set; } = false;
        public TimeSpan TTL { get; set; } = TimeSpan.FromMinutes(10);
        public CacheScope Scope { get; set; } = CacheScope.User;
        public List<string> VaryByHeaders { get; set; } = new();
        public List<string> VaryByParameters { get; set; } = new();
        public int MaxCacheSize { get; set; } = 1000;
        public CacheEvictionPolicy EvictionPolicy { get; set; } = CacheEvictionPolicy.LRU;
    }

    /// <summary>
    /// API health status
    /// </summary>
    public class ApiHealthStatus
    {
        public HealthStatus Status { get; set; }
        public DateTime LastCheck { get; set; }
        public Dictionary<string, HealthStatus> EndpointStatuses { get; set; } = new();
        public List<string> Issues { get; set; } = new();
        public TimeSpan Uptime { get; set; }
        public double OverallLatency { get; set; }
    }

    /// <summary>
    /// API request wrapper
    /// </summary>
    public class ApiRequest
    {
        public string EndpointId { get; set; } = string.Empty;
        public string Method { get; set; } = "POST";
        public Dictionary<string, string> Headers { get; set; } = new();
        public Dictionary<string, object> Parameters { get; set; } = new();
        public object Body { get; set; } = new();
        public string UserId { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;
        public DateTime RequestTime { get; set; } = DateTime.UtcNow;
        public string ClientInfo { get; set; } = string.Empty;
    }

    /// <summary>
    /// API response wrapper
    /// </summary>
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string? ErrorMessage { get; set; }
        public int StatusCode { get; set; }
        public Dictionary<string, string> Headers { get; set; } = new();
        public TimeSpan ProcessingTime { get; set; }
        public string RequestId { get; set; } = string.Empty;
        public ApiMetadata Metadata { get; set; } = new();
    }

    /// <summary>
    /// API metadata
    /// </summary>
    public class ApiMetadata
    {
        public string Version { get; set; } = "1.0";
        public string ModelVersion { get; set; } = string.Empty;
        public long TokensUsed { get; set; }
        public decimal Cost { get; set; }
        public Dictionary<string, object> AdditionalInfo { get; set; } = new();
    }

    /// <summary>
    /// API endpoint configuration
    /// </summary>
    public class ApiEndpoint
    {
        public string Id { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string Method { get; set; } = "POST";
        public string Description { get; set; } = string.Empty;
        public TransformerModelSource TargetProvider { get; set; }
        public string ModelId { get; set; } = string.Empty;
        public TransformerTask TaskType { get; set; }
        public AuthenticationRequirement Authentication { get; set; } = new();
        public RateLimitPolicy RateLimit { get; set; } = new();
        public CachingPolicy Caching { get; set; } = new();
        public List<string> AllowedOrigins { get; set; } = new();
        public bool IsEnabled { get; set; } = true;
        public Dictionary<string, object> DefaultParameters { get; set; } = new();
    }

    /// <summary>
    /// Authentication requirement configuration
    /// </summary>
    public class AuthenticationRequirement
    {
        public AuthenticationType Type { get; set; } = AuthenticationType.None;
        public List<string> RequiredScopes { get; set; } = new();
        public List<string> AllowedRoles { get; set; } = new();
        public bool RequireHttps { get; set; } = true;
        public Dictionary<string, string> AdditionalSettings { get; set; } = new();
    }

    /// <summary>
    /// Rate limiting policy
    /// </summary>
    public class RateLimitPolicy
    {
        public int RequestsPerMinute { get; set; } = 60;
        public int RequestsPerHour { get; set; } = 1000;
        public int RequestsPerDay { get; set; } = 10000;
        public int ConcurrentRequests { get; set; } = 5;
        public RateLimitScope Scope { get; set; } = RateLimitScope.User;
        public string? CustomKey { get; set; }
        public List<RateLimitExemption> Exemptions { get; set; } = new();
    }

    /// <summary>
    /// Rate limit exemption
    /// </summary>
    public class RateLimitExemption
    {
        public string UserId { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public int? CustomLimit { get; set; }
    }

    public enum LoadBalancingMethod
    {
        RoundRobin,
        WeightedRoundRobin,
        LeastConnections,
        Random,
        IPHash,
        HealthyRandom
    }

    public enum CacheScope
    {
        Global,
        User,
        Session,
        Request
    }

    public enum CacheEvictionPolicy
    {
        LRU,
        LFU,
        FIFO,
        TTL
    }

    public enum AuthenticationType
    {
        None,
        ApiKey,
        Bearer,
        OAuth2,
        JWT,
        Custom
    }

    public enum RateLimitScope
    {
        Global,
        User,
        IP,
        Session,
        Custom
    }

    #endregion

    #region Webhooks and Event System

    /// <summary>
    /// Webhook and event notification system
    /// </summary>
    public interface ITransformerWebhooks
    {
        Task<string> RegisterWebhookAsync(WebhookConfig config);
        Task<bool> UnregisterWebhookAsync(string webhookId);
        Task<List<Webhook>> GetActiveWebhooksAsync();
        Task<bool> TriggerWebhookAsync(string webhookId, WebhookEvent eventData);
        Task<List<WebhookDelivery>> GetDeliveryHistoryAsync(string webhookId);
        Task<bool> RetryFailedDeliveryAsync(string deliveryId);
        Task<WebhookHealth> GetWebhookHealthAsync(string webhookId);
    }

    /// <summary>
    /// Webhook information
    /// </summary>
    public class Webhook
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public List<WebhookEventType> Events { get; set; } = new();
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime LastTriggered { get; set; }
        public int TotalDeliveries { get; set; }
        public int SuccessfulDeliveries { get; set; }
    }

    /// <summary>
    /// Webhook delivery attempt
    /// </summary>
    public class WebhookDelivery
    {
        public string Id { get; set; } = string.Empty;
        public string WebhookId { get; set; } = string.Empty;
        public DateTime AttemptedAt { get; set; }
        public bool Success { get; set; }
        public int StatusCode { get; set; }
        public string Response { get; set; } = string.Empty;
        public TimeSpan Duration { get; set; }
        public int AttemptNumber { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }

    /// <summary>
    /// Webhook health status
    /// </summary>
    public class WebhookHealth
    {
        public string WebhookId { get; set; } = string.Empty;
        public HealthStatus Status { get; set; }
        public double SuccessRate { get; set; }
        public TimeSpan AverageResponseTime { get; set; }
        public int RecentFailures { get; set; }
        public DateTime LastSuccessfulDelivery { get; set; }
        public List<string> RecentErrors { get; set; } = new();
    }

    /// <summary>
    /// Webhook configuration
    /// </summary>
    public class WebhookConfig
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public List<WebhookEventType> Events { get; set; } = new();
        public WebhookAuthentication Authentication { get; set; } = new();
        public int TimeoutSeconds { get; set; } = 30;
        public int MaxRetries { get; set; } = 3;
        public bool IsActive { get; set; } = true;
        public Dictionary<string, string> CustomHeaders { get; set; } = new();
        public string? FilterExpression { get; set; }
    }

    /// <summary>
    /// Webhook authentication configuration
    /// </summary>
    public class WebhookAuthentication
    {
        public WebhookAuthType Type { get; set; } = WebhookAuthType.None;
        public string Secret { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public Dictionary<string, string> Headers { get; set; } = new();
    }

    /// <summary>
    /// Webhook event data
    /// </summary>
    public class WebhookEvent
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public WebhookEventType Type { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Source { get; set; } = string.Empty;
        public object Data { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
        public string Version { get; set; } = "1.0";
    }

    public enum WebhookAuthType
    {
        None,
        Basic,
        Bearer,
        HMAC,
        Custom
    }

    public enum WebhookEventType
    {
        ModelLoaded,
        InferenceCompleted,
        InferenceFailed,
        ModelUnloaded,
        PipelineStarted,
        PipelineStopped,
        AlertTriggered,
        ExperimentCompleted,
        FineTuningCompleted,
        SecurityIncident,
        ComplianceViolation
    }

    #endregion

    #region Third-party Integrations

    /// <summary>
    /// Integration manager for third-party services
    /// </summary>
    public interface ITransformerIntegrations
    {
        Task<bool> RegisterIntegrationAsync(IntegrationConfig config);
        Task<List<Integration>> GetAvailableIntegrationsAsync();
        Task<bool> EnableIntegrationAsync(string integrationId);
        Task<bool> DisableIntegrationAsync(string integrationId);
        Task<IntegrationStatus> GetIntegrationStatusAsync(string integrationId);
        Task<bool> SyncWithIntegrationAsync(string integrationId);
        Task<List<IntegrationLog>> GetIntegrationLogsAsync(string integrationId);
    }

    /// <summary>
    /// Integration information
    /// </summary>
    public class Integration
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public IntegrationType Type { get; set; }
        public bool IsEnabled { get; set; }
        public DateTime LastSync { get; set; }
        public IntegrationStatus Status { get; set; }
        public List<IntegrationCapability> Capabilities { get; set; } = new();
        public string Version { get; set; } = string.Empty;
    }

    /// <summary>
    /// Integration status
    /// </summary>
    public class IntegrationStatus
    {
        public string IntegrationId { get; set; } = string.Empty;
        public HealthStatus Status { get; set; }
        public bool IsConnected { get; set; }
        public DateTime LastCheck { get; set; }
        public List<string> Issues { get; set; } = new();
        public Dictionary<string, object> StatusDetails { get; set; } = new();
    }

    /// <summary>
    /// Integration log entry
    /// </summary>
    public class IntegrationLog
    {
        public string Id { get; set; } = string.Empty;
        public string IntegrationId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public LogLevel Level { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Operation { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string ErrorDetails { get; set; } = string.Empty;
    }

    /// <summary>
    /// Integration configuration
    /// </summary>
    public class IntegrationConfig
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public IntegrationType Type { get; set; }
        public Dictionary<string, string> ConnectionSettings { get; set; } = new();
        public Dictionary<string, object> Configuration { get; set; } = new();
        public List<IntegrationCapability> Capabilities { get; set; } = new();
        public bool IsEnabled { get; set; } = true;
        public IntegrationSyncSettings SyncSettings { get; set; } = new();
    }

    /// <summary>
    /// Integration synchronization settings
    /// </summary>
    public class IntegrationSyncSettings
    {
        public bool AutoSync { get; set; } = true;
        public TimeSpan SyncInterval { get; set; } = TimeSpan.FromMinutes(15);
        public List<string> SyncedDataTypes { get; set; } = new();
        public ConflictResolutionStrategy ConflictResolution { get; set; } = ConflictResolutionStrategy.LastWrite;
        public int MaxRetries { get; set; } = 3;
    }

    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error,
        Critical
    }

    public enum ConflictResolutionStrategy
    {
        LastWrite,
        FirstWrite,
        Merge,
        Manual
    }

    public enum IntegrationType
    {
        MLFlow,
        Weights_And_Biases,
        TensorBoard,
        Neptune,
        Comet,
        Kubeflow,
        Ray,
        Dask,
        Spark,
        Airflow,
        Prefect,
        Kafka,
        Redis,
        Elasticsearch,
        Grafana,
        Prometheus,
        DataDog,
        NewRelic
    }

    public enum IntegrationCapability
    {
        ModelVersioning,
        ExperimentTracking,
        MetricsLogging,
        ArtifactStorage,
        Monitoring,
        Alerting,
        DataPipelines,
        Workflows,
        DistributedComputing,
        Caching,
        Search,
        Visualization
    }

    #endregion

    #region SDK and Client Libraries

    /// <summary>
    /// SDK generator for multiple programming languages
    /// </summary>
    public interface ITransformerSDKGenerator
    {
        Task<string> GenerateSDKAsync(SDKConfig config);
        Task<List<SupportedLanguage>> GetSupportedLanguagesAsync();
        Task<bool> PublishSDKAsync(string sdkId, PublishTarget target);
        Task<SDKDocumentation> GenerateDocumentationAsync(string sdkId);
        Task<List<SDKExample>> GenerateExamplesAsync(string sdkId);
    }

    /// <summary>
    /// Supported programming language
    /// </summary>
    public class SupportedLanguage
    {
        public ProgrammingLanguage Language { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public List<SDKFeature> SupportedFeatures { get; set; } = new();
        public bool IsStable { get; set; }
        public Dictionary<string, string> Templates { get; set; } = new();
    }

    /// <summary>
    /// SDK publish target
    /// </summary>
    public class PublishTarget
    {
        public PublishPlatform Platform { get; set; }
        public string Repository { get; set; } = string.Empty;
        public Dictionary<string, string> Credentials { get; set; } = new();
        public bool AutoPublish { get; set; } = false;
        public string BuildConfiguration { get; set; } = "Release";
    }

    /// <summary>
    /// SDK documentation
    /// </summary>
    public class SDKDocumentation
    {
        public string SDKId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<DocumentationSection> Sections { get; set; } = new();
        public List<ApiReference> ApiReferences { get; set; } = new();
        public List<string> CodeExamples { get; set; } = new();
        public DateTime GeneratedAt { get; set; }
    }

    /// <summary>
    /// Documentation section
    /// </summary>
    public class DocumentationSection
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int Order { get; set; }
        public List<DocumentationSection> Subsections { get; set; } = new();
    }

    /// <summary>
    /// API reference documentation
    /// </summary>
    public class ApiReference
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Signature { get; set; } = string.Empty;
        public List<Parameter> Parameters { get; set; } = new();
        public ReturnType ReturnType { get; set; } = new();
        public List<string> Examples { get; set; } = new();
    }

    /// <summary>
    /// Parameter documentation
    /// </summary>
    public class Parameter
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsRequired { get; set; }
        public string DefaultValue { get; set; } = string.Empty;
    }

    /// <summary>
    /// Return type documentation
    /// </summary>
    public class ReturnType
    {
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> PossibleValues { get; set; } = new();
    }

    /// <summary>
    /// SDK example
    /// </summary>
    public class SDKExample
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public ProgrammingLanguage Language { get; set; }
        public ExampleCategory Category { get; set; }
        public List<string> Tags { get; set; } = new();
    }

    /// <summary>
    /// SDK configuration
    /// </summary>
    public class SDKConfig
    {
        public string Id { get; set; } = string.Empty;
        public ProgrammingLanguage Language { get; set; }
        public string PackageName { get; set; } = string.Empty;
        public string Version { get; set; } = "1.0.0";
        public List<ApiEndpoint> IncludedEndpoints { get; set; } = new();
        public SDKFeatures Features { get; set; } = new();
        public CodeGeneration CodeGeneration { get; set; } = new();
        public Dictionary<string, object> LanguageSpecificSettings { get; set; } = new();
    }

    /// <summary>
    /// Code generation settings
    /// </summary>
    public class CodeGeneration
    {
        public bool GenerateModels { get; set; } = true;
        public bool GenerateClients { get; set; } = true;
        public bool GenerateExceptions { get; set; } = true;
        public bool GenerateValidation { get; set; } = true;
        public bool GenerateDocumentation { get; set; } = true;
        public bool GenerateTests { get; set; } = false;
        public CodeStyle Style { get; set; } = new();
    }

    /// <summary>
    /// Code style settings
    /// </summary>
    public class CodeStyle
    {
        public NamingConvention NamingConvention { get; set; } = NamingConvention.CamelCase;
        public string IndentStyle { get; set; } = "spaces";
        public int IndentSize { get; set; } = 4;
        public bool UseRegions { get; set; } = true;
        public Dictionary<string, string> CustomSettings { get; set; } = new();
    }

    public enum PublishPlatform
    {
        NuGet,
        PyPI,
        NPM,
        Maven,
        GoModules,
        Cargo,
        SwiftPackageManager,
        Composer,
        RubyGems,
        CRAN
    }

    public enum SDKFeature
    {
        Async,
        Retry,
        Batching,
        Caching,
        Streaming,
        Metrics,
        Logging,
        ErrorHandling,
        Validation,
        Authentication,
        RateLimiting,
        Pagination
    }

    public enum ExampleCategory
    {
        QuickStart,
        BasicUsage,
        AdvancedUsage,
        Integration,
        ErrorHandling,
        Performance,
        Security
    }

    public enum NamingConvention
    {
        CamelCase,
        PascalCase,
        SnakeCase,
        KebabCase
    }

    public enum ProgrammingLanguage
    {
        CSharp,
        Python,
        JavaScript,
        TypeScript,
        Java,
        Go,
        Rust,
        Swift,
        Kotlin,
        PHP,
        Ruby,
        R
    }

    /// <summary>
    /// SDK features configuration
    /// </summary>
    public class SDKFeatures
    {
        public bool IncludeAsync { get; set; } = true;
        public bool IncludeRetry { get; set; } = true;
        public bool IncludeBatching { get; set; } = true;
        public bool IncludeCaching { get; set; } = false;
        public bool IncludeStreaming { get; set; } = false;
        public bool IncludeMetrics { get; set; } = false;
        public bool IncludeLogging { get; set; } = true;
        public bool IncludeErrorHandling { get; set; } = true;
        public bool IncludeValidation { get; set; } = true;
    }

    #endregion
}