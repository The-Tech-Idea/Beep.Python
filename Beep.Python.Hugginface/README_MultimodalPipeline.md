# Multimodal Transformer Pipeline Examples

This document demonstrates how to use the new **MultimodalTransformerPipeline** for advanced cross-modal AI tasks, with special emphasis on **enterprise multi-user environments**.

## Overview

The `MultimodalTransformerPipeline` is a high-level orchestrator that coordinates multiple specialized transformer pipelines to enable complex multimodal AI capabilities like:

- **Text-to-Image**: Generate images from text descriptions
- **Text-to-Audio**: Create speech and music from text
- **Image-to-Text**: Generate captions and descriptions from images
- **Audio-to-Text**: Transcribe speech and analyze audio
- **Complex Workflows**: Create complete multimedia stories and presentations

## ?? Enterprise Multi-User Usage (Recommended)

### 1. Enterprise Setup with Pre-Existing Sessions

```csharp
using Beep.Python.AI.Transformers;

// In an enterprise environment, sessions are typically managed centrally
// Get the session manager and create/retrieve user sessions
var sessionManager = pythonRunTimeManager.SessionManager;
var envManager = pythonRunTimeManager.VirtualEnvmanager;

// Option 1: Use existing session (recommended for web applications)
var existingSession = sessionManager.GetSession("user123_session_id");
var virtualEnv = envManager.GetEnvironmentById(existingSession.VirtualEnvironmentId);

var enterprisePipeline = await MultimodalPipelineFactory.CreateEnterpriseMultiUserPipelineAsync(
    pythonRunTimeManager,
    executeManager,
    existingSession,
    virtualEnv
);

// Option 2: Create session-aware pipeline (auto-manages sessions)
var sessionAwarePipeline = await MultimodalPipelineFactory.CreateSessionAwarePipelineAsync(
    pythonRunTimeManager,
    executeManager,
    username: "alice",
    environmentId: null // Auto-select best environment
);

// Now use the pipeline with proper session isolation
var imageResult = await enterprisePipeline.GenerateImageFromTextAsync(
    "A corporate office with modern design",
    new TextToImageParameters { Quality = "high", Style = "professional" }
);
```

### 2. Multi-User Environment with Dedicated Virtual Environments

```csharp
// Create dedicated environments for different user types
var dataScientistEnv = envManager.GetEnvironmentByPath(@"C:\Envs\DataScience");
var designerEnv = envManager.GetEnvironmentByPath(@"C:\Envs\CreativeDesign");
var developerEnv = envManager.GetEnvironmentByPath(@"C:\Envs\Development");

// Create pipelines for different user roles
var dataScientistPipeline = await MultimodalPipelineFactory.CreateEnvironmentSpecificPipelineAsync(
    pythonRunTimeManager,
    executeManager,
    dataScientistEnv,
    "alice_data_scientist"
);

var designerPipeline = await MultimodalPipelineFactory.CreateEnvironmentSpecificPipelineAsync(
    pythonRunTimeManager,
    executeManager,
    designerEnv,
    "bob_designer"
);

// Each pipeline is isolated and uses the appropriate environment
var technicalDiagram = await dataScientistPipeline.GenerateImageFromTextAsync(
    "A scientific data visualization chart showing neural network architecture"
);

var artisticConcept = await designerPipeline.GenerateImageFromTextAsync(
    "A creative artistic interpretation of data flowing through networks"
);
```

### 3. Session Configuration and Management

```csharp
// Manual session configuration for maximum control
var pipeline = new MultimodalTransformerPipeline(pythonRunTimeManager, executeManager);

// Configure with specific session and environment
var configured = pipeline.ConfigureSession(userSession, userVirtualEnvironment);
if (configured)
{
    await pipeline.InitializeAsync(new MultimodalPipelineConfig
    {
        GlobalConfig = new Dictionary<string, object>
        {
            ["enterprise_mode"] = true,
            ["user_isolation"] = true,
            ["security_level"] = "high"
        }
    });
    
    // Check current configuration
    var currentSession = pipeline.GetConfiguredSession();
    var currentEnv = pipeline.GetConfiguredVirtualEnvironment();
    
    Console.WriteLine($"Using session: {currentSession?.SessionId}");
    Console.WriteLine($"User: {currentSession?.Username}");
    Console.WriteLine($"Environment: {currentEnv?.Name}");
}
```

## ??? Single-User Development Usage

### Quick Start for Development

```csharp
// For development and testing - creates temporary sessions automatically
var devPipeline = await MultimodalPipelineFactory.CreateCreativeContentPipelineAsync(
    pythonRunTimeManager, 
    executeManager
);

// This will use temporary sessions (not recommended for production)
var result = await devPipeline.GenerateImageFromTextAsync("A concept art of a futuristic city");
```

## ?? Web Application Integration

### ASP.NET Core Example

```csharp
// In Startup.cs or Program.cs
services.AddSingleton<IPythonRunTimeManager, PythonNetRunTimeManager>();
services.AddSingleton<IPythonCodeExecuteManager, PythonCodeExecuteManager>();
services.AddScoped<MultimodalTransformerPipeline>();

// In Controller
[ApiController]
[Route("api/[controller]")]
public class MultimodalController : ControllerBase
{
    private readonly IPythonRunTimeManager _runtimeManager;
    private readonly IPythonCodeExecuteManager _executeManager;

    public MultimodalController(
        IPythonRunTimeManager runtimeManager,
        IPythonCodeExecuteManager executeManager)
    {
        _runtimeManager = runtimeManager;
        _executeManager = executeManager;
    }

    [HttpPost("generate-image")]
    public async Task<IActionResult> GenerateImage([FromBody] ImageGenerationRequest request)
    {
        // Get user session from HTTP context or JWT token
        var userId = User.Identity.Name;
        var sessionId = HttpContext.Session.GetString("PythonSessionId");
        
        PythonSessionInfo userSession;
        
        if (string.IsNullOrEmpty(sessionId))
        {
            // Create new session for user
            userSession = _runtimeManager.SessionManager.CreateSession(userId, null);
            HttpContext.Session.SetString("PythonSessionId", userSession.SessionId);
        }
        else
        {
            // Reuse existing session
            userSession = _runtimeManager.SessionManager.GetSession(sessionId);
        }

        if (userSession == null)
        {
            return BadRequest("Failed to create or retrieve user session");
        }

        try
        {
            // Get virtual environment for the session
            var virtualEnv = _runtimeManager.VirtualEnvmanager.GetEnvironmentById(userSession.VirtualEnvironmentId);
            
            // Create enterprise pipeline
            var pipeline = await MultimodalPipelineFactory.CreateEnterpriseMultiUserPipelineAsync(
                _runtimeManager,
                _executeManager,
                userSession,
                virtualEnv
            );

            // Generate image
            var result = await pipeline.GenerateImageFromTextAsync(
                request.Prompt,
                new TextToImageParameters 
                { 
                    Width = request.Width,
                    Height = request.Height,
                    Quality = request.Quality
                }
            );

            if (result.Success)
            {
                return Ok(new { ImageData = result.Data.ImageData, Format = result.Data.Format });
            }
            else
            {
                return BadRequest(result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error generating image: {ex.Message}");
        }
    }
}
```

## ??? Security and Isolation Best Practices

### 1. Environment Isolation

```csharp
// Create isolated environments for different security levels
var highSecurityEnv = await envManager.CreateEnvironmentForUser(
    pythonConfig,
    @"C:\SecureEnvs\HighSec",
    "security_user"
);

var standardEnv = await envManager.CreateEnvironmentForUser(
    pythonConfig,
    @"C:\StandardEnvs\Standard",
    "standard_user"
);

// Use appropriate environment based on user security clearance
var pipeline = await MultimodalPipelineFactory.CreateEnvironmentSpecificPipelineAsync(
    pythonRunTimeManager,
    executeManager,
    userSecurityLevel == "high" ? highSecurityEnv : standardEnv,
    username
);
```

### 2. Session Lifecycle Management

```csharp
// Proper session lifecycle in a web application
public class SessionManagedMultimodalService : IDisposable
{
    private readonly Dictionary<string, MultimodalTransformerPipeline> _userPipelines = new();
    private readonly IPythonRunTimeManager _runtimeManager;
    private readonly IPythonCodeExecuteManager _executeManager;

    public async Task<MultimodalTransformerPipeline> GetUserPipelineAsync(string userId)
    {
        if (_userPipelines.TryGetValue(userId, out var existingPipeline))
        {
            // Verify session is still active
            var session = existingPipeline.GetConfiguredSession();
            if (session?.Status == PythonSessionStatus.Active)
            {
                return existingPipeline;
            }
            else
            {
                // Clean up inactive pipeline
                existingPipeline.Dispose();
                _userPipelines.Remove(userId);
            }
        }

        // Create new pipeline for user
        var newPipeline = await MultimodalPipelineFactory.CreateSessionAwarePipelineAsync(
            _runtimeManager,
            _executeManager,
            userId
        );

        _userPipelines[userId] = newPipeline;
        return newPipeline;
    }

    public void Dispose()
    {
        foreach (var pipeline in _userPipelines.Values)
        {
            pipeline.Dispose();
        }
        _userPipelines.Clear();
    }
}
```

## ?? Advanced Configuration

### Custom Virtual Environment Setup

```csharp
// Create specialized environments for different AI workloads
var imageGenEnv = new PythonVirtualEnvironment
{
    Name = "ImageGeneration",
    Path = @"C:\AIEnvs\ImageGen",
    EnvironmentType = PythonEnvironmentType.Conda,
    PythonBinary = PythonBinary.Conda
};

// Create the environment with specific packages
var created = envManager.CreateVirtualEnvironment(pythonConfig, imageGenEnv);
if (created)
{
    // Initialize with AI-specific packages
    envManager.InitializePythonEnvironment(imageGenEnv);
    
    // Create pipeline for this specialized environment
    var aiPipeline = await MultimodalPipelineFactory.CreateEnvironmentSpecificPipelineAsync(
        pythonRunTimeManager,
        executeManager,
        imageGenEnv,
        "ai_artist"
    );
}
```

## ?? Benefits of Proper Session Management

### ? **Multi-User Support**
- **True Isolation**: Each user has their own Python session and virtual environment
- **Concurrent Execution**: Multiple users can run AI tasks simultaneously
- **Resource Management**: Proper cleanup and resource allocation

### ? **Enterprise Features**
- **Session Persistence**: Sessions can be reused across multiple requests
- **Load Balancing**: Automatic distribution across available environments
- **Security**: User isolation prevents code and data leakage

### ? **Production Ready**
- **Error Handling**: Robust error handling and recovery
- **Monitoring**: Session status tracking and health monitoring
- **Scalability**: Designed for high-throughput scenarios

### ? **Best Practices Implementation**
- **No Session Creation in Pipelines**: Sessions are managed externally
- **Virtual Environment Reuse**: Efficient environment utilization
- **Proper Cleanup**: Automatic resource management and cleanup

This approach ensures that your multimodal AI applications are **production-ready**, **secure**, and **scalable** for enterprise environments! ??