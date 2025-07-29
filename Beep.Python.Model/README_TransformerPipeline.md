# Transformer Pipeline Interface Documentation

## Overview

The `ITransformerPipeLine` interface provides a unified, general-purpose abstraction for working with transformer models from various sources including HuggingFace, local models, OpenAI, Azure, and other custom sources. This interface is designed to work seamlessly with the existing Beep.Python infrastructure.

## Key Features

- **Multi-Source Support**: Works with HuggingFace Hub, local models, OpenAI, Azure OpenAI, Google, Anthropic, Cohere, Meta, Mistral, and custom model sources
- **Comprehensive Task Coverage**: Supports text generation, classification, NER, Q&A, summarization, translation, embeddings, and more
- **Async Operations**: All operations are asynchronous for better performance and responsiveness
- **Event-Driven**: Provides events for model loading, inference progress, and error handling
- **Flexible Configuration**: Extensive configuration options for different use cases
- **Type-Safe Results**: Strongly-typed result objects with metadata and error information

## Supported Model Sources

- **HuggingFace Hub**: Access to thousands of pre-trained models
- **Local Models**: Load models from local filesystem
- **OpenAI**: Integration with OpenAI's GPT models (GPT-4, GPT-3.5, etc.)
- **Azure OpenAI**: Azure-hosted OpenAI models
- **Google**: Google Cloud AI models (Gemini, PaLM, etc.)
- **Anthropic**: Claude models
- **Cohere**: Cohere language models
- **Meta**: Llama models
- **Mistral**: Mistral AI models
- **Custom**: Support for custom model sources and APIs

## Project Structure

The transformer pipeline implementation is organized in the `Beep.Python.AI.Transformers` namespace:

```
Beep.Python.AI.Transformers/
??? TransformerPipelineFactory.cs        # Main factory for creating pipelines
??? BaseTransformerPipeline.cs          # Base implementation for all providers
??? HuggingFaceTransformerPipeline.cs   # HuggingFace-specific implementation
??? OpenAITransformerPipeline.cs        # OpenAI-specific implementation
??? AzureTransformerPipeline.cs         # Azure OpenAI implementation
??? LocalTransformerPipeline.cs         # Local model implementation
??? CustomTransformerPipeline.cs        # Custom source implementation
```

## Basic Usage

### 1. Create and Initialize Pipeline

```csharp
using Beep.Python.AI.Transformers;

// Create pipeline using factory
var pipeline = TransformerPipelineFactory.CreatePipeline(
    TransformerModelSource.HuggingFace,
    pythonRunTimeManager,
    executeManager
);

// Or use automatic detection
var autoPipeline = TransformerPipelineFactory.CreatePipelineAuto(
    "gpt-3.5-turbo", // Will automatically detect as OpenAI
    pythonRunTimeManager,
    executeManager
);

// Configure pipeline
var config = new TransformerPipelineConfig
{
    ModelName = "bert-base-uncased",
    TaskType = TransformerTask.TextClassification,
    Device = TransformerDevice.Auto,
    MaxInputLength = 512,
    BatchSize = 1
};

// Initialize
await pipeline.InitializeAsync(config);
```

### 2. Load Models from Different Sources

```csharp
// Load HuggingFace model
await pipeline.LoadHuggingFaceModelAsync(
    "distilbert-base-uncased-finetuned-sst-2-english", 
    TransformerTask.SentimentAnalysis
);

// Load local model
await pipeline.LoadLocalModelAsync(
    @"C:\path\to\local\model", 
    TransformerTask.TextGeneration
);

// Load OpenAI model
var openAIModel = new TransformerModelInfo
{
    Name = "gpt-3.5-turbo",
    Source = TransformerModelSource.OpenAI,
    SupportedTasks = { TransformerTask.TextGeneration, TransformerTask.Conversational }
};
await pipeline.LoadCustomModelAsync(openAIModel, TransformerTask.TextGeneration);

// Load Anthropic Claude
var claudeModel = new TransformerModelInfo
{
    Name = "claude-3-sonnet",
    Source = TransformerModelSource.Anthropic,
    SupportedTasks = { TransformerTask.TextGeneration, TransformerTask.Conversational }
};
await pipeline.LoadCustomModelAsync(claudeModel, TransformerTask.TextGeneration);
```

### 3. Perform Inference

```csharp
// Text Classification
var classificationResult = await pipeline.ClassifyTextAsync(
    "I love this product!", 
    new ClassificationParameters { ReturnAllScores = true }
);

if (classificationResult.Success)
{
    Console.WriteLine($"Sentiment: {classificationResult.Data.Label}");
    Console.WriteLine($"Confidence: {classificationResult.Data.Score:P2}");
}

// Text Generation
var generationResult = await pipeline.GenerateTextAsync(
    "The future of AI is",
    new TextGenerationParameters 
    { 
        MaxLength = 100, 
        Temperature = 0.7,
        TopP = 0.9
    }
);

// Named Entity Recognition
var nerResult = await pipeline.ExtractEntitiesAsync(
    "Apple Inc. was founded by Steve Jobs in Cupertino, California.",
    new NERParameters { AggregationStrategy = "simple" }
);

// Question Answering
var qaResult = await pipeline.AnswerQuestionAsync(
    "Who founded Apple?",
    "Apple Inc. was founded by Steve Jobs in Cupertino, California.",
    new QAParameters { TopK = 1 }
);

// Text Summarization
var summaryResult = await pipeline.SummarizeTextAsync(
    "Long text to summarize...",
    new SummarizationParameters { MaxLength = 50, MinLength = 10 }
);

// Translation
var translationResult = await pipeline.TranslateTextAsync(
    "Hello, how are you?",
    "es", // Spanish
    "en"  // From English
);

// Embeddings
var embeddingResult = await pipeline.GetEmbeddingsAsync(
    new List<string> { "Text 1", "Text 2", "Text 3" },
    new EmbeddingParameters { Normalize = true }
);
```

### 4. Batch Processing

```csharp
var inputs = new List<string> 
{ 
    "This is great!", 
    "This is terrible!", 
    "This is okay." 
};

var batchResults = await pipeline.BatchInferenceAsync(
    inputs, 
    TransformerTask.SentimentAnalysis
);

foreach (var result in batchResults)
{
    if (result.Success)
    {
        Console.WriteLine($"Result: {result.Data}");
    }
}
```

### 5. Event Handling

```csharp
// Subscribe to events
pipeline.ModelLoadingStarted += (sender, e) => 
    Console.WriteLine($"Loading model {e.ModelName}...");

pipeline.ModelLoadingCompleted += (sender, e) => 
    Console.WriteLine($"Model {e.ModelName} loaded successfully");

pipeline.InferenceStarted += (sender, e) => 
    Console.WriteLine("Inference started...");

pipeline.InferenceCompleted += (sender, e) => 
    Console.WriteLine($"Inference completed in {e.Data["execution_time"]}ms");

pipeline.ErrorOccurred += (sender, e) => 
    Console.WriteLine($"Error: {e.ErrorMessage}");

pipeline.ProgressUpdated += (sender, e) => 
    Console.WriteLine($"Progress: {e.ProgressPercentage}% - {e.Message}");
```

## Configuration Options

### Pipeline Configuration

```csharp
var config = new TransformerPipelineConfig
{
    ModelName = "model-name",
    ModelSource = TransformerModelSource.HuggingFace,
    TaskType = TransformerTask.TextGeneration,
    Device = TransformerDevice.Auto,      // CPU, CUDA, MPS, Auto
    Precision = ModelPrecision.Auto,      // Full, Half, Int8, Int4, Auto
    MaxInputLength = 512,
    BatchSize = 1,
    UseCache = true,
    TrustRemoteCode = false,
    Revision = "main",
    AuthToken = "your-token",
    CustomConfig = new Dictionary<string, object>
    {
        { "use_fast_tokenizer", true },
        { "low_cpu_mem_usage", true }
    }
};
```

### Task-Specific Parameters

```csharp
// Text Generation
var genParams = new TextGenerationParameters
{
    MaxLength = 100,
    MinLength = 10,
    Temperature = 0.7,
    TopP = 0.9,
    TopK = 50,
    NumReturn = 1,
    DoSample = true,
    EarlyStopping = false,
    RepetitionPenalty = 1.0,
    LengthPenalty = 1.0,
    StopSequences = { "\n", "." }
};

// Classification
var classParams = new ClassificationParameters
{
    ReturnAllScores = true,
    Function = "softmax",
    CandidateLabels = { "positive", "negative", "neutral" }
};

// NER
var nerParams = new NERParameters
{
    AggregationStrategy = "simple",
    IgnoreLabels = false,
    ScoreThreshold = 0.5
};
```

## Model Information and Validation

```csharp
// Get model information
var modelInfo = pipeline.GetModelInfo();
Console.WriteLine($"Model: {modelInfo.Name}");
Console.WriteLine($"Architecture: {modelInfo.Architecture}");
Console.WriteLine($"Parameters: {modelInfo.ParameterCount:N0}");

// Validate model compatibility
var validation = await pipeline.ValidateModelAsync(modelInfo);
if (validation.IsValid)
{
    Console.WriteLine($"Model is compatible (Score: {validation.CompatibilityScore}/100)");
}
else
{
    Console.WriteLine("Model validation failed:");
    validation.Errors.ForEach(Console.WriteLine);
}

// Get supported tasks
var supportedTasks = pipeline.GetSupportedTasks();
Console.WriteLine($"Supported tasks: {string.Join(", ", supportedTasks)}");
```

## Advanced Features

### Warm-up Models

```csharp
// Warm up model to reduce first inference latency
await pipeline.WarmUpAsync("Sample input for warmup");
```

### Custom Model Sources

```csharp
var customModel = new TransformerModelInfo
{
    Name = "custom-model",
    Source = TransformerModelSource.Custom,
    ModelPath = "https://custom-api.com/model",
    Architecture = "custom-architecture",
    Metadata = { { "api_key", "your-api-key" } }
};

await pipeline.LoadCustomModelAsync(customModel, TransformerTask.TextGeneration);
```

### Model Management

```csharp
// Get available models
var availableModels = await pipeline.GetAvailableModelsAsync(
    TransformerTask.TextClassification, 
    TransformerModelSource.HuggingFace
);

// Update configuration
var newConfig = new TransformerPipelineConfig { BatchSize = 4 };
pipeline.UpdateConfiguration(newConfig);

// Unload model to free memory
pipeline.UnloadModel();
```

## Error Handling

```csharp
try
{
    var result = await pipeline.GenerateTextAsync("Hello world");
    
    if (!result.Success)
    {
        Console.WriteLine($"Inference failed: {result.ErrorMessage}");
        return;
    }
    
    Console.WriteLine($"Generated text: {result.Data}");
    Console.WriteLine($"Execution time: {result.ExecutionTimeMs}ms");
    
    if (result.TokenUsage != null)
    {
        Console.WriteLine($"Tokens used: {result.TokenUsage.TotalTokens}");
        Console.WriteLine($"Estimated cost: ${result.TokenUsage.EstimatedCost:F4}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Exception occurred: {ex.Message}");
}
```

## Best Practices

1. **Initialize Once**: Initialize the pipeline once and reuse it for multiple inferences
2. **Use Appropriate Batch Sizes**: Adjust batch size based on available memory and performance requirements
3. **Handle Events**: Subscribe to events for better user experience and debugging
4. **Validate Models**: Always validate models before loading in production
5. **Warm-up Models**: Use warm-up for better first inference performance
6. **Resource Management**: Dispose of pipelines when done to free resources
7. **Error Handling**: Always check result.Success before using result.Data
8. **Token Monitoring**: Monitor token usage for cost-aware applications

## Integration with Existing Infrastructure

The transformer pipeline integrates seamlessly with the existing Beep.Python infrastructure:

- Uses `IPythonRunTimeManager` for Python runtime management
- Leverages `IPythonCodeExecuteManager` for code execution
- Compatible with existing package management and virtual environments
- Supports the established event and error handling patterns

## Performance Considerations

- **Device Selection**: Use GPU when available for better performance
- **Model Precision**: Use lower precision (Int8, Int4) for faster inference with minimal accuracy loss
- **Batch Processing**: Process multiple inputs in batches for better throughput
- **Caching**: Enable caching for repeated inferences with similar inputs
- **Model Size**: Choose appropriate model size based on accuracy vs. speed requirements

## Extensibility

The interface is designed for extensibility:

- Add new model sources by implementing the interface
- Support new task types by extending the TransformerTask enum
- Add custom parameters by extending the parameter classes
- Create specialized pipelines for specific use cases

This interface provides a solid foundation for building sophisticated AI applications with transformer models while maintaining flexibility, extensibility, and ease of use across multiple providers.

# Transformer Pipeline with Enterprise Session Management

This document demonstrates how to use transformer pipelines with **enterprise-grade session management** and **virtual environment isolation** for multi-user environments.

## Overview

All transformer pipeline classes now support:
- **Pre-existing session management** for multi-user environments
- **Virtual environment isolation** for security and resource management
- **Enterprise-grade configuration** with proper session handling
- **Backward compatibility** for single-user scenarios

## ?? Enterprise Multi-User Usage (Recommended)

### 1. Enterprise Setup with Session Management

```csharp
using Beep.Python.AI.Transformers;

// Get session manager and environment manager
var sessionManager = pythonRunTimeManager.SessionManager;
var envManager = pythonRunTimeManager.VirtualEnvmanager;

// Option 1: Use existing session (recommended for web applications)
var existingSession = sessionManager.GetSession("user123_session_id");
var virtualEnv = envManager.GetEnvironmentById(existingSession.VirtualEnvironmentId);

var enterprisePipeline = TransformerPipelineFactory.CreateEnterpriseMultiUserPipeline(
    TransformerModelSource.HuggingFace,
    pythonRunTimeManager,
    executeManager,
    existingSession,
    virtualEnv
);

// Option 2: Create session-aware pipeline (auto-manages sessions)
var sessionAwarePipeline = TransformerPipelineFactory.CreateSessionAwarePipeline(
    TransformerModelSource.OpenAI,
    pythonRunTimeManager,
    executeManager,
    username: "alice",
    environmentId: null // Auto-select best environment
);

// Now use the pipeline with proper session isolation
await enterprisePipeline.InitializeAsync(new TransformerPipelineConfig
{
    ModelSource = TransformerModelSource.HuggingFace,
    TaskType = TransformerTask.TextGeneration,
    Device = TransformerDevice.CUDA
});

await enterprisePipeline.LoadModelAsync(new TransformerModelInfo
{
    Name = "microsoft/DialoGPT-medium",
    Source = TransformerModelSource.HuggingFace
}, TransformerTask.TextGeneration);

var result = await enterprisePipeline.GenerateTextAsync("Hello, how are you?");
```

### 2. Multi-User Environment with Dedicated Virtual Environments

```csharp
// Create dedicated environments for different user types
var dataScientistEnv = envManager.GetEnvironmentByPath(@"C:\Envs\DataScience");
var developerEnv = envManager.GetEnvironmentByPath(@"C:\Envs\Development");
var testingEnv = envManager.GetEnvironmentByPath(@"C:\Envs\Testing");

// Create pipelines for different user roles with different providers
var dataScientistPipeline = TransformerPipelineFactory.CreateEnvironmentSpecificPipeline(
    TransformerModelSource.HuggingFace,  // Use HuggingFace for research
    pythonRunTimeManager,
    executeManager,
    dataScientistEnv,
    "alice_data_scientist"
);

var developerPipeline = TransformerPipelineFactory.CreateEnvironmentSpecificPipeline(
    TransformerModelSource.OpenAI,       // Use OpenAI for development
    pythonRunTimeManager,
    executeManager,
    developerEnv,
    "bob_developer"
);

var testingPipeline = TransformerPipelineFactory.CreateEnvironmentSpecificPipeline(
    TransformerModelSource.Local,        // Use local models for testing
    pythonRunTimeManager,
    executeManager,
    testingEnv,
    "charlie_tester"
);

// Each pipeline is isolated and uses the appropriate environment
await dataScientistPipeline.LoadModelAsync(new TransformerModelInfo
{
    Name = "bert-base-uncased",
    Source = TransformerModelSource.HuggingFace
}, TransformerTask.TextClassification);

await developerPipeline.LoadModelAsync(new TransformerModelInfo
{
    Name = "gpt-3.5-turbo",
    Source = TransformerModelSource.OpenAI
}, TransformerTask.TextGeneration);

await testingPipeline.LoadModelAsync(new TransformerModelInfo
{
    Name = @"C:\Models\local-model",
    Source = TransformerModelSource.Local
}, TransformerTask.Summarization);
```

### 3. Session Configuration for Different Providers

```csharp
// Manual session configuration for maximum control across different providers
var providers = new[]
{
    TransformerModelSource.HuggingFace,
    TransformerModelSource.OpenAI,
    TransformerModelSource.Azure,
    TransformerModelSource.Anthropic,
    TransformerModelSource.Google
};

var userPipelines = new Dictionary<TransformerModelSource, ITransformerPipeLine>();

foreach (var provider in providers)
{
    var pipeline = TransformerPipelineFactory.CreatePipeline(provider, pythonRunTimeManager, executeManager);
    
    // Configure with specific session and environment
    if (pipeline is BaseTransformerPipeline basePipeline)
    {
        var configured = basePipeline.ConfigureSession(userSession, userVirtualEnvironment);
        if (configured)
        {
            await pipeline.InitializeAsync(new TransformerPipelineConfig
            {
                ModelSource = provider,
                TaskType = TransformerTask.TextGeneration,
                CustomConfig = new Dictionary<string, object>
                {
                    ["enterprise_mode"] = true,
                    ["user_isolation"] = true,
                    ["security_level"] = "high"
                }
            });
            
            userPipelines[provider] = pipeline;
            
            // Check current configuration
            var currentSession = basePipeline.GetConfiguredSession();
            var currentEnv = basePipeline.GetConfiguredVirtualEnvironment();
            
            Console.WriteLine($"Provider: {provider}");
            Console.WriteLine($"Session: {currentSession?.SessionId}");
            Console.WriteLine($"User: {currentSession?.Username}");
            Console.WriteLine($"Environment: {currentEnv?.Name}");
        }
    }
}
```

## ?? Web Application Integration

### ASP.NET Core Example with Multiple Providers

```csharp
// In Startup.cs or Program.cs
services.AddSingleton<IPythonRunTimeManager, PythonNetRunTimeManager>();
services.AddSingleton<IPythonCodeExecuteManager, PythonCodeExecuteManager>();
services.AddScoped<TransformerService>();

// Transformer service for web applications
public class TransformerService
{
    private readonly IPythonRunTimeManager _runtimeManager;
    private readonly IPythonCodeExecuteManager _executeManager;
    private readonly Dictionary<string, ITransformerPipeLine> _userPipelines = new();

    public TransformerService(
        IPythonRunTimeManager runtimeManager,
        IPythonCodeExecuteManager executeManager)
    {
        _runtimeManager = runtimeManager;
        _executeManager = executeManager;
    }

    public async Task<ITransformerPipeLine> GetUserPipelineAsync(
        string userId, 
        TransformerModelSource preferredSource = TransformerModelSource.HuggingFace)
    {
        if (_userPipelines.TryGetValue(userId, out var existingPipeline))
        {
            // Check if session is still active
            if (existingPipeline is BaseTransformerPipeline basePipeline)
            {
                var session = basePipeline.GetConfiguredSession();
                if (session?.Status == PythonSessionStatus.Active)
                {
                    return existingPipeline;
                }
            }
            
            // Clean up inactive pipeline
            existingPipeline.Dispose();
            _userPipelines.Remove(userId);
        }

        // Create new pipeline for user
        var newPipeline = TransformerPipelineFactory.CreateSessionAwarePipeline(
            preferredSource,
            _runtimeManager,
            _executeManager,
            userId
        );

        _userPipelines[userId] = newPipeline;
        return newPipeline;
    }

    public async Task<string> GenerateTextForUserAsync(string userId, string prompt, TransformerModelSource? preferredSource = null)
    {
        var source = preferredSource ?? TransformerModelSource.HuggingFace;
        var pipeline = await GetUserPipelineAsync(userId, source);
        
        // Ensure pipeline is ready
        if (!pipeline.IsInitialized)
        {
            await pipeline.InitializeAsync(new TransformerPipelineConfig
            {
                ModelSource = source,
                TaskType = TransformerTask.TextGeneration
            });
        }

        if (!pipeline.IsModelLoaded)
        {
            var defaultModel = GetDefaultModelForSource(source);
            await pipeline.LoadModelAsync(defaultModel, TransformerTask.TextGeneration);
        }

        var result = await pipeline.GenerateTextAsync(prompt);
        return result.Data ?? "Generation failed";
    }

    private TransformerModelInfo GetDefaultModelForSource(TransformerModelSource source)
    {
        return source switch
        {
            TransformerModelSource.HuggingFace => new TransformerModelInfo 
            { 
                Name = "microsoft/DialoGPT-medium", 
                Source = TransformerModelSource.HuggingFace 
            },
            TransformerModelSource.OpenAI => new TransformerModelInfo 
            { 
                Name = "gpt-3.5-turbo", 
                Source = TransformerModelSource.OpenAI 
            },
            TransformerModelSource.Anthropic => new TransformerModelInfo 
            { 
                Name = "claude-3-sonnet-20240229", 
                Source = TransformerModelSource.Anthropic 
            },
            _ => new TransformerModelInfo 
            { 
                Name = "microsoft/DialoGPT-medium", 
                Source = TransformerModelSource.HuggingFace 
            }
        };
    }
}

// In Controller
[ApiController]
[Route("api/[controller]")]
public class TransformerController : ControllerBase
{
    private readonly TransformerService _transformerService;

    public TransformerController(TransformerService transformerService)
    {
        _transformerService = transformerService;
    }

    [HttpPost("generate")]
    public async Task<IActionResult> GenerateText([FromBody] GenerationRequest request)
    {
        try
        {
            var userId = User.Identity?.Name ?? HttpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
            
            var result = await _transformerService.GenerateTextForUserAsync(
                userId, 
                request.Prompt, 
                request.PreferredProvider);
            
            return Ok(new { Text = result, UserId = userId });
        }
        catch (Exception ex)
        {
            return BadRequest($"Error generating text: {ex.Message}");
        }
    }
}

public class GenerationRequest
{
    public string Prompt { get; set; } = string.Empty;
    public TransformerModelSource? PreferredProvider { get; set; }
}
```

## ??? Security and Isolation Best Practices

### 1. Provider-Specific Environment Isolation

```csharp
// Create isolated environments for different providers and security levels
var secureEnvironments = new Dictionary<TransformerModelSource, PythonVirtualEnvironment>
{
    [TransformerModelSource.HuggingFace] = await envManager.CreateEnvironmentForUser(
        pythonConfig, @"C:\SecureEnvs\HuggingFace", "hf_user"
    ),
    [TransformerModelSource.OpenAI] = await envManager.CreateEnvironmentForUser(
        pythonConfig, @"C:\SecureEnvs\OpenAI", "openai_user"
    ),
    [TransformerModelSource.Local] = await envManager.CreateEnvironmentForUser(
        pythonConfig, @"C:\SecureEnvs\Local", "local_user"
    )
};

// Create pipelines with provider-specific isolation
var isolatedPipelines = new Dictionary<TransformerModelSource, ITransformerPipeLine>();

foreach (var kvp in secureEnvironments)
{
    var provider = kvp.Key;
    var environment = kvp.Value;
    
    var pipeline = TransformerPipelineFactory.CreateEnvironmentSpecificPipeline(
        provider,
        pythonRunTimeManager,
        executeManager,
        environment,
        $"secure_{provider.ToString().ToLower()}_user"
    );
    
    isolatedPipelines[provider] = pipeline;
}
```

### 2. Session Lifecycle Management for All Providers

```csharp
// Proper session lifecycle management for enterprise transformer usage
public class EnterpriseTransformerManager : IDisposable
{
    private readonly Dictionary<string, Dictionary<TransformerModelSource, ITransformerPipeLine>> _userProviderPipelines = new();
    private readonly IPythonRunTimeManager _runtimeManager;
    private readonly IPythonCodeExecuteManager _executeManager;

    public async Task<ITransformerPipeLine> GetUserPipelineAsync(string userId, TransformerModelSource source)
    {
        if (!_userProviderPipelines.ContainsKey(userId))
        {
            _userProviderPipelines[userId] = new Dictionary<TransformerModelSource, ITransformerPipeLine>();
        }

        var userPipelines = _userProviderPipelines[userId];
        
        if (userPipelines.TryGetValue(source, out var existingPipeline))
        {
            // Verify session is still active
            if (existingPipeline is BaseTransformerPipeline basePipeline)
            {
                var session = basePipeline.GetConfiguredSession();
                if (session?.Status == PythonSessionStatus.Active)
                {
                    return existingPipeline;
                }
                else
                {
                    // Clean up inactive pipeline
                    existingPipeline.Dispose();
                    userPipelines.Remove(source);
                }
            }
        }

        // Create new pipeline for user and provider
        var newPipeline = TransformerPipelineFactory.CreateSessionAwarePipeline(
            source,
            _runtimeManager,
            _executeManager,
            userId
        );

        userPipelines[source] = newPipeline;
        return newPipeline;
    }

    public async Task<T> ExecuteWithProviderAsync<T>(
        string userId, 
        TransformerModelSource source, 
        Func<ITransformerPipeLine, Task<T>> operation)
    {
        var pipeline = await GetUserPipelineAsync(userId, source);
        return await operation(pipeline);
    }

    public void Dispose()
    {
        foreach (var userPipelines in _userProviderPipelines.Values)
        {
            foreach (var pipeline in userPipelines.Values)
            {
                pipeline.Dispose();
            }
        }
        _userProviderPipelines.Clear();
    }
}
```

## ??? Single-User Development Usage

### Quick Start for Development (Backward Compatible)

```csharp
// For development and testing - automatically creates temporary sessions
var devPipeline = TransformerPipelineFactory.CreatePipeline(
    TransformerModelSource.HuggingFace,
    pythonRunTimeManager, 
    executeManager
);

await devPipeline.InitializeAsync(new TransformerPipelineConfig
{
    ModelSource = TransformerModelSource.HuggingFace,
    TaskType = TransformerTask.TextGeneration
});

// This will use temporary sessions (not recommended for production)
var result = await devPipeline.GenerateTextAsync("Hello world!");
```

## ?? Benefits of Session Management in Transformer Pipelines

### ? **Multi-User Support**
- **True Isolation**: Each user has their own Python session and virtual environment
- **Provider Flexibility**: Different users can use different AI providers simultaneously
- **Concurrent Execution**: Multiple users can run transformer tasks concurrently

### ? **Enterprise Features**
- **Session Persistence**: Sessions can be reused across multiple transformer operations
- **Load Balancing**: Automatic distribution across available environments
- **Provider Security**: Isolation between different AI provider environments

### ? **Production Ready**
- **Error Handling**: Robust error handling and session recovery
- **Monitoring**: Session status tracking and provider health monitoring
- **Scalability**: Designed for high-throughput enterprise transformer scenarios

### ? **Provider Agnostic**
- **Unified Interface**: Same session management across all transformer providers
- **Flexible Configuration**: Easy switching between HuggingFace, OpenAI, Azure, etc.
- **Resource Management**: Efficient resource utilization across providers

This approach ensures that your transformer AI applications are **production-ready**, **secure**, **scalable**, and work seamlessly across **all AI providers**! ??