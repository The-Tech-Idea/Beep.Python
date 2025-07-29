# Transformer Service Connection Configuration Guide

This guide demonstrates how to configure connections to various AI service providers for transformer pipelines, including authentication, endpoints, and advanced configuration options.

## Overview

The connection configuration system provides:
- **Secure Authentication**: Support for API keys, OAuth, and other authentication methods
- **Provider-Specific Settings**: Tailored configuration for each AI service provider
- **Enterprise Features**: Proxy support, rate limiting, and monitoring
- **Environment Management**: Integration with virtual environments and sessions
- **Validation**: Built-in validation for connection configurations

## ?? Basic Connection Configuration

### OpenAI Configuration

```csharp
using Beep.Python.Model;
using Beep.Python.AI.Transformers;

// Configure OpenAI connection
var openAIConfig = new OpenAIConnectionConfig
{
    ApiKey = "sk-your-openai-api-key-here",
    OrganizationId = "org-your-organization-id", // Optional
    ProjectId = "proj_your-project-id",          // Optional
    ApiEndpoint = "https://api.openai.com/v1",  // Default
    TimeoutSeconds = 60,
    MaxRetries = 3,
    RateLimit = new RateLimitConfig
    {
        RequestsPerMinute = 60,
        TokensPerMinute = 150000,
        ConcurrentRequests = 5
    }
};

// Create pipeline with connection
var openAIPipeline = TransformerPipelineFactory.CreateOpenAIPipeline(
    pythonRunTimeManager,
    executeManager,
    openAIConfig
);

// Or with session management
var enterpriseOpenAI = TransformerPipelineFactory.CreateOpenAIPipeline(
    pythonRunTimeManager,
    executeManager,
    openAIConfig,
    userSession,
    virtualEnvironment
);
```

### Azure OpenAI Configuration

```csharp
// Configure Azure OpenAI connection
var azureConfig = new AzureOpenAIConnectionConfig
{
    ApiKey = "your-azure-openai-api-key",
    Endpoint = "https://your-resource.openai.azure.com/",
    ApiVersion = "2024-02-01",
    DeploymentName = "gpt-35-turbo",
    SubscriptionId = "your-subscription-id",
    ResourceGroup = "your-resource-group",
    TimeoutSeconds = 90,
    MaxRetries = 5
};

// Optional: Configure Azure AD authentication
azureConfig.AzureAD = new AzureADConfig
{
    TenantId = "your-tenant-id",
    ClientId = "your-client-id",
    ClientSecret = "your-client-secret",
    Scope = "https://cognitiveservices.azure.com/.default"
};

var azurePipeline = TransformerPipelineFactory.CreateAzureOpenAIPipeline(
    pythonRunTimeManager,
    executeManager,
    azureConfig,
    userSession,
    virtualEnvironment
);
```

### Google AI Configuration

```csharp
// Configure Google AI connection
var googleConfig = new GoogleAIConnectionConfig
{
    ApiKey = "your-google-ai-api-key",
    ProjectId = "your-project-id",
    Endpoint = "https://generativelanguage.googleapis.com",
    ApiVersion = "v1beta",
    Location = "us-central1",
    TimeoutSeconds = 45
};

// Optional: Service Account authentication
googleConfig.ServiceAccount = new GoogleServiceAccountConfig
{
    KeyFilePath = @"C:\path\to\service-account-key.json",
    Email = "service-account@your-project.iam.gserviceaccount.com",
    Scopes = { "https://www.googleapis.com/auth/cloud-platform" }
};

var googlePipeline = TransformerPipelineFactory.CreateGoogleAIPipeline(
    pythonRunTimeManager,
    executeManager,
    googleConfig
);
```

### Anthropic Claude Configuration

```csharp
// Configure Anthropic connection
var anthropicConfig = new AnthropicConnectionConfig
{
    ApiKey = "sk-ant-your-anthropic-api-key",
    ApiEndpoint = "https://api.anthropic.com",
    ApiVersion = "2023-06-01",
    BetaFeatures = { "tools-2024-04-04" }, // Enable beta features
    TimeoutSeconds = 120, // Longer timeout for complex reasoning
    MaxRetries = 2
};

var anthropicPipeline = TransformerPipelineFactory.CreateAnthropicPipeline(
    pythonRunTimeManager,
    executeManager,
    anthropicConfig,
    userSession,
    virtualEnvironment
);
```

### HuggingFace Hub Configuration

```csharp
// Configure HuggingFace connection
var hfConfig = new HuggingFaceConnectionConfig
{
    Token = "hf_your-huggingface-token",        // For private models
    HubEndpoint = "https://huggingface.co",
    InferenceEndpoint = "https://api-inference.huggingface.co",
    UseInferenceAPI = true,                     // Use hosted inference
    CacheDir = @"C:\HuggingFace\Cache",
    OfflineMode = false,
    TimeoutSeconds = 300 // Longer for model downloads
};

var hfPipeline = TransformerPipelineFactory.CreateHuggingFacePipeline(
    pythonRunTimeManager,
    executeManager,
    hfConfig
);
```

## ?? Enterprise Connection Configuration

### Multi-Provider Enterprise Setup

```csharp
// Configure multiple providers for enterprise use
public class EnterpriseAIConfiguration
{
    private readonly TransformerConnectionManager _connectionManager = new();
    
    public void ConfigureAllProviders()
    {
        // Configure OpenAI with enterprise settings
        var openAIConfig = new OpenAIConnectionConfig
        {
            ApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY"),
            OrganizationId = Environment.GetEnvironmentVariable("OPENAI_ORG_ID"),
            TimeoutSeconds = 120,
            MaxRetries = 5,
            RateLimit = new RateLimitConfig
            {
                RequestsPerMinute = 100,
                TokensPerMinute = 500000,
                ConcurrentRequests = 10
            },
            CustomHeaders = new Dictionary<string, string>
            {
                ["User-Agent"] = "Enterprise-AI-Platform/1.0",
                ["X-Request-ID"] = Guid.NewGuid().ToString()
            }
        };

        // Configure Azure with managed identity
        var azureConfig = new AzureOpenAIConnectionConfig
        {
            Endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT"),
            ApiVersion = "2024-02-01",
            TimeoutSeconds = 180,
            AzureAD = new AzureADConfig
            {
                TenantId = Environment.GetEnvironmentVariable("AZURE_TENANT_ID"),
                ClientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID"),
                ClientSecret = Environment.GetEnvironmentVariable("AZURE_CLIENT_SECRET")
            }
        };

        // Configure with proxy for enterprise network
        azureConfig.ProxyConfig = new ProxyConfig
        {
            Host = "proxy.company.com",
            Port = 8080,
            Username = Environment.GetEnvironmentVariable("PROXY_USER"),
            Password = Environment.GetEnvironmentVariable("PROXY_PASS"),
            Type = ProxyType.HTTP
        };

        // Register configurations
        _connectionManager.RegisterConnection(TransformerModelSource.OpenAI, openAIConfig);
        _connectionManager.RegisterConnection(TransformerModelSource.Azure, azureConfig);
        
        // Validate all configurations
        foreach (var provider in _connectionManager.GetConfiguredProviders())
        {
            var validation = _connectionManager.ValidateConnection(provider);
            if (!validation.IsValid)
            {
                throw new InvalidOperationException(
                    $"Invalid configuration for {provider}: {string.Join(", ", validation.Errors)}");
            }
        }
    }
    
    public ITransformerPipeLine CreateUserPipeline(
        TransformerModelSource preferredProvider,
        string username,
        IPythonRunTimeManager runtimeManager,
        IPythonCodeExecuteManager executeManager)
    {
        var connectionConfig = _connectionManager.GetConnection(preferredProvider);
        if (connectionConfig == null)
        {
            throw new InvalidOperationException($"No configuration found for {preferredProvider}");
        }

        return TransformerPipelineFactory.CreateSessionAwarePipelineWithConnection(
            preferredProvider,
            runtimeManager,
            executeManager,
            username,
            connectionConfig
        );
    }
}
```

### Connection Validation and Health Checks

```csharp
// Enterprise connection monitoring
public class ConnectionHealthMonitor
{
    private readonly TransformerConnectionManager _connectionManager;
    private readonly Dictionary<TransformerModelSource, DateTime> _lastHealthCheck = new();
    
    public async Task<bool> ValidateAllConnectionsAsync()
    {
        var providers = _connectionManager.GetConfiguredProviders();
        var tasks = providers.Select(ValidateProviderConnectionAsync);
        var results = await Task.WhenAll(tasks);
        
        return results.All(r => r.IsHealthy);
    }
    
    private async Task<ConnectionHealthResult> ValidateProviderConnectionAsync(TransformerModelSource provider)
    {
        var config = _connectionManager.GetConnection(provider);
        if (config == null)
        {
            return new ConnectionHealthResult 
            { 
                Provider = provider, 
                IsHealthy = false, 
                Error = "No configuration found" 
            };
        }

        try
        {
            // Perform actual health check based on provider type
            var isHealthy = await PerformHealthCheckAsync(provider, config);
            
            _lastHealthCheck[provider] = DateTime.UtcNow;
            
            return new ConnectionHealthResult 
            { 
                Provider = provider, 
                IsHealthy = isHealthy,
                LastChecked = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            return new ConnectionHealthResult 
            { 
                Provider = provider, 
                IsHealthy = false, 
                Error = ex.Message,
                LastChecked = DateTime.UtcNow
            };
        }
    }
    
    private async Task<bool> PerformHealthCheckAsync(TransformerModelSource provider, TransformerConnectionConfig config)
    {
        // Create a minimal test pipeline
        using var pipeline = TransformerPipelineFactory.CreatePipelineWithConnection(
            provider,
            runtimeManager,
            executeManager,
            config
        );

        // Test basic connectivity
        await pipeline.InitializeAsync(new TransformerPipelineConfig
        {
            ModelSource = provider,
            TaskType = TransformerTask.TextGeneration
        });

        return pipeline.IsInitialized;
    }
}

public class ConnectionHealthResult
{
    public TransformerModelSource Provider { get; set; }
    public bool IsHealthy { get; set; }
    public string? Error { get; set; }
    public DateTime LastChecked { get; set; }
}
```

## ?? Security and Authentication

### Secure Credential Management

```csharp
// Secure credential storage and retrieval
public class SecureCredentialManager
{
    public static OpenAIConnectionConfig CreateSecureOpenAIConfig()
    {
        return new OpenAIConnectionConfig
        {
            ApiKey = GetSecureCredential("OPENAI_API_KEY"),
            OrganizationId = GetSecureCredential("OPENAI_ORG_ID"),
            CustomHeaders = new Dictionary<string, string>
            {
                ["X-Source"] = "Enterprise-Platform",
                ["X-Environment"] = GetEnvironmentName()
            },
            RateLimit = GetRateLimitFromPolicy(),
            TimeoutSeconds = GetTimeoutFromPolicy()
        };
    }
    
    public static AzureOpenAIConnectionConfig CreateSecureAzureConfig()
    {
        var config = new AzureOpenAIConnectionConfig
        {
            Endpoint = GetSecureCredential("AZURE_OPENAI_ENDPOINT"),
            ApiVersion = "2024-02-01",
            DeploymentName = GetSecureCredential("AZURE_DEPLOYMENT_NAME")
        };

        // Use managed identity when available
        if (IsManagedIdentityAvailable())
        {
            config.AzureAD = new AzureADConfig
            {
                TenantId = GetSecureCredential("AZURE_TENANT_ID"),
                // Client credentials managed by Azure Key Vault or similar
            };
        }
        else
        {
            config.ApiKey = GetSecureCredential("AZURE_OPENAI_API_KEY");
        }

        return config;
    }
    
    private static string GetSecureCredential(string key)
    {
        // Priority: Key Vault > Environment Variables > Secure Configuration
        return Environment.GetEnvironmentVariable(key) 
               ?? throw new InvalidOperationException($"Credential {key} not found");
    }
    
    private static string GetEnvironmentName()
    {
        return Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
    }
    
    private static RateLimitConfig GetRateLimitFromPolicy()
    {
        return new RateLimitConfig
        {
            RequestsPerMinute = 100,
            TokensPerMinute = 500000,
            ConcurrentRequests = 10,
            Enabled = true
        };
    }
    
    private static int GetTimeoutFromPolicy()
    {
        return int.Parse(Environment.GetEnvironmentVariable("AI_TIMEOUT_SECONDS") ?? "120");
    }
    
    private static bool IsManagedIdentityAvailable()
    {
        // Check if running in Azure with managed identity
        return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MSI_ENDPOINT"));
    }
}
```

## ?? Web Application Integration

### ASP.NET Core Configuration

```csharp
// Startup.cs or Program.cs
public void ConfigureServices(IServiceCollection services)
{
    // Register connection configurations
    services.Configure<OpenAIConnectionConfig>(Configuration.GetSection("OpenAI"));
    services.Configure<AzureOpenAIConnectionConfig>(Configuration.GetSection("AzureOpenAI"));
    services.Configure<AnthropicConnectionConfig>(Configuration.GetSection("Anthropic"));
    
    // Register transformer services
    services.AddSingleton<IPythonRunTimeManager, PythonNetRunTimeManager>();
    services.AddSingleton<IPythonCodeExecuteManager, PythonCodeExecuteManager>();
    services.AddSingleton<TransformerConnectionManager>();
    services.AddScoped<ITransformerService, TransformerService>();
}

// appsettings.json configuration
{
  "OpenAI": {
    "ApiKey": "sk-your-api-key",
    "OrganizationId": "org-your-org",
    "TimeoutSeconds": 60,
    "MaxRetries": 3,
    "RateLimit": {
      "RequestsPerMinute": 60,
      "TokensPerMinute": 150000,
      "ConcurrentRequests": 5
    }
  },
  "AzureOpenAI": {
    "Endpoint": "https://your-resource.openai.azure.com/",
    "ApiKey": "your-azure-key",
    "ApiVersion": "2024-02-01",
    "DeploymentName": "gpt-35-turbo",
    "TimeoutSeconds": 90
  },
  "Anthropic": {
    "ApiKey": "sk-ant-your-key",
    "TimeoutSeconds": 120,
    "MaxRetries": 2
  }
}

// Service implementation
public interface ITransformerService
{
    Task<string> GenerateTextAsync(string prompt, TransformerModelSource provider, string userId);
    Task<bool> ValidateConnectionAsync(TransformerModelSource provider);
}

public class TransformerService : ITransformerService
{
    private readonly IOptionsMonitor<OpenAIConnectionConfig> _openAIConfig;
    private readonly IOptionsMonitor<AzureOpenAIConnectionConfig> _azureConfig;
    private readonly IOptionsMonitor<AnthropicConnectionConfig> _anthropicConfig;
    private readonly IPythonRunTimeManager _runtimeManager;
    private readonly IPythonCodeExecuteManager _executeManager;
    private readonly Dictionary<string, ITransformerPipeLine> _userPipelines = new();

    public TransformerService(
        IOptionsMonitor<OpenAIConnectionConfig> openAIConfig,
        IOptionsMonitor<AzureOpenAIConnectionConfig> azureConfig,
        IOptionsMonitor<AnthropicConnectionConfig> anthropicConfig,
        IPythonRunTimeManager runtimeManager,
        IPythonCodeExecuteManager executeManager)
    {
        _openAIConfig = openAIConfig;
        _azureConfig = azureConfig;
        _anthropicConfig = anthropicConfig;
        _runtimeManager = runtimeManager;
        _executeManager = executeManager;
    }

    public async Task<string> GenerateTextAsync(string prompt, TransformerModelSource provider, string userId)
    {
        var pipeline = await GetOrCreateUserPipelineAsync(provider, userId);
        
        var result = await pipeline.GenerateTextAsync(prompt);
        return result.Success ? result.Data : $"Error: {result.ErrorMessage}";
    }

    public async Task<bool> ValidateConnectionAsync(TransformerModelSource provider)
    {
        var connectionConfig = GetConnectionConfig(provider);
        if (connectionConfig == null) return false;

        try
        {
            using var testPipeline = TransformerPipelineFactory.CreatePipelineWithConnection(
                provider,
                _runtimeManager,
                _executeManager,
                connectionConfig
            );

            await testPipeline.InitializeAsync(new TransformerPipelineConfig
            {
                ModelSource = provider,
                TaskType = TransformerTask.TextGeneration
            });

            return testPipeline.IsInitialized;
        }
        catch
        {
            return false;
        }
    }

    private async Task<ITransformerPipeLine> GetOrCreateUserPipelineAsync(TransformerModelSource provider, string userId)
    {
        var key = $"{provider}_{userId}";
        
        if (_userPipelines.TryGetValue(key, out var existingPipeline))
        {
            if (existingPipeline is BaseTransformerPipeline basePipeline)
            {
                var session = basePipeline.GetConfiguredSession();
                if (session?.Status == PythonSessionStatus.Active)
                {
                    return existingPipeline;
                }
            }
            
            existingPipeline.Dispose();
            _userPipelines.Remove(key);
        }

        var connectionConfig = GetConnectionConfig(provider);
        if (connectionConfig == null)
        {
            throw new InvalidOperationException($"No connection configuration for {provider}");
        }

        var newPipeline = TransformerPipelineFactory.CreateSessionAwarePipelineWithConnection(
            provider,
            _runtimeManager,
            _executeManager,
            userId,
            connectionConfig
        );

        _userPipelines[key] = newPipeline;
        return newPipeline;
    }

    private TransformerConnectionConfig? GetConnectionConfig(TransformerModelSource provider)
    {
        return provider switch
        {
            TransformerModelSource.OpenAI => _openAIConfig.CurrentValue,
            TransformerModelSource.Azure => _azureConfig.CurrentValue,
            TransformerModelSource.Anthropic => _anthropicConfig.CurrentValue,
            _ => null
        };
    }
}
```

## ?? Advanced Configuration Features

### Dynamic Configuration Updates

```csharp
// Hot-reload configuration changes
public class DynamicConnectionManager
{
    private readonly IOptionsMonitor<OpenAIConnectionConfig> _openAIMonitor;
    private IDisposable? _changeToken;

    public DynamicConnectionManager(IOptionsMonitor<OpenAIConnectionConfig> openAIMonitor)
    {
        _openAIMonitor = openAIMonitor;
        
        // Listen for configuration changes
        _changeToken = _openAIMonitor.OnChange(OnConfigurationChanged);
    }

    private void OnConfigurationChanged(OpenAIConnectionConfig newConfig)
    {
        // Update existing pipelines with new configuration
        UpdateAllPipelinesWithNewConfig(newConfig);
        
        // Validate new configuration
        var validation = ValidateConnectionConfig(newConfig);
        if (!validation.IsValid)
        {
            LogConfigurationError(validation.Errors);
        }
    }
}
```

### Load Balancing Across Providers

```csharp
// Load balancing between multiple AI providers
public class LoadBalancedTransformerService
{
    private readonly Dictionary<TransformerModelSource, (ITransformerPipeLine Pipeline, int Weight)> _providers = new();
    
    public void ConfigureProviders()
    {
        // Configure multiple providers with different weights
        var openAI = TransformerPipelineFactory.CreateOpenAIPipeline(
            runtimeManager, executeManager, openAIConfig);
        var azure = TransformerPipelineFactory.CreateAzureOpenAIPipeline(
            runtimeManager, executeManager, azureConfig);
        var anthropic = TransformerPipelineFactory.CreateAnthropicPipeline(
            runtimeManager, executeManager, anthropicConfig);

        _providers[TransformerModelSource.OpenAI] = (openAI, 40);      // 40% weight
        _providers[TransformerModelSource.Azure] = (azure, 35);        // 35% weight  
        _providers[TransformerModelSource.Anthropic] = (anthropic, 25); // 25% weight
    }
    
    public async Task<string> GenerateWithLoadBalancingAsync(string prompt)
    {
        var selectedProvider = SelectProviderByWeight();
        var pipeline = _providers[selectedProvider].Pipeline;
        
        var result = await pipeline.GenerateTextAsync(prompt);
        return result.Data ?? "Generation failed";
    }
    
    private TransformerModelSource SelectProviderByWeight()
    {
        var random = new Random().Next(1, 101);
        var cumulative = 0;
        
        foreach (var kvp in _providers)
        {
            cumulative += kvp.Value.Weight;
            if (random <= cumulative)
            {
                return kvp.Key;
            }
        }
        
        return _providers.Keys.First(); // Fallback
    }
}
```

This comprehensive connection configuration system ensures that your transformer pipelines are **enterprise-ready**, **secure**, **scalable**, and can connect to **any AI service provider** with proper authentication and configuration management! ????