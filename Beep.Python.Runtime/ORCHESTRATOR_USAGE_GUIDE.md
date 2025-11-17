# Python Runtime Orchestrator - Usage Guide

## Overview

The `PythonRuntimeOrchestrator` is the **main entry point** for working with Python in your application. It provides a developer-friendly API that orchestrates all Python management components (runtime, sessions, environments, execution).

## Architecture

```
PythonRuntimeOrchestrator (High-level API)
    ├── PythonNetRunTimeManager (Core runtime)
    ├── PythonSessionManager (Session management)
    ├── PythonVirtualEnvManager (Environment management)
    └── PythonCodeExecuteManager (Code execution)
```

## Key Concepts

### 1. Admin Environment
- **Purpose**: System-level Python environment with management packages
- **Created automatically** during initialization
- **Contains**: pip, setuptools, wheel, virtualenv, and other base packages
- **Used for**: Package installation, environment creation, system operations

### 2. Operating Modes

#### Single-User Mode (`PythonEngineMode.SingleUser`)
- One active environment at a time
- Simple API with "current environment" concept
- Ideal for desktop applications, development tools

#### Multi-User Mode (`PythonEngineMode.MultiUserWithEnvAndScopeAndSession`)
- Isolated environments per user
- Concurrent session management
- Load balancing across environments
- Ideal for web services, multi-tenant applications

## Quick Start

### Basic Initialization (Embedded Python)

```csharp
using Beep.Python.RuntimeEngine;
using Beep.Python.Model;

// Create orchestrator
var orchestrator = new PythonRuntimeOrchestrator(beepService);

// Optional: Set custom working directory for environments
// (automatically set when used in BeepShell)
orchestrator.WorkingDirectory = @"C:\MyWorkspace";

// Initialize with embedded Python (downloads if needed)
var progress = new Progress<string>(msg => Console.WriteLine(msg));
var success = await orchestrator.InitializeWithEmbeddedPythonAsync(
    mode: PythonEngineMode.SingleUser,
    basePackages: null, // Uses defaults: pip, setuptools, wheel, virtualenv
    progress: progress
);

if (success)
{
    Console.WriteLine("Orchestrator ready!");
}
```

### Initialization with Existing Python

```csharp
var orchestrator = new PythonRuntimeOrchestrator(beepService);

// Initialize with existing Python installation
var success = await orchestrator.InitializeWithExistingPythonAsync(
    pythonPath: @"C:\Python39",
    mode: PythonEngineMode.SingleUser,
    progress: progress
);
```

## Single-User Mode Examples

### Example 1: Basic Code Execution

```csharp
// Initialize in single-user mode
var orchestrator = new PythonRuntimeOrchestrator(beepService);
await orchestrator.InitializeWithEmbeddedPythonAsync(
    mode: PythonEngineMode.SingleUser
);

// Execute Python code in default environment
var (success, output) = await orchestrator.ExecuteAsync(@"
print('Hello from Python!')
result = 2 + 2
print(f'2 + 2 = {result}')
");

if (success)
{
    Console.WriteLine($"Output:\n{output}");
}
```

### Example 2: Creating and Switching Environments

```csharp
// Create a data science environment
var dsEnv = await orchestrator.CreateAndSetEnvironmentAsync(
    envName: "data_science",
    packageProfiles: new List<string> { "data-science" }, // numpy, pandas, matplotlib
    progress: progress
);

// Execute in this environment
var (success, output) = await orchestrator.ExecuteAsync(@"
import pandas as pd
import numpy as np

df = pd.DataFrame({'A': np.random.rand(5), 'B': np.random.rand(5)})
print(df)
");

// Create another environment for web scraping
var webEnv = await orchestrator.CreateAndSetEnvironmentAsync(
    envName: "web_scraping",
    packageProfiles: new List<string> { "web" } // requests, beautifulsoup4
);

// Switch back to data science environment
orchestrator.SetCurrentEnvironment(dsEnv.ID);
```

### Example 3: Installing Packages

```csharp
// Get current environment
var currentEnv = orchestrator.CurrentEnvironment;

// Install specific packages
var packages = new List<string> { "requests", "pillow", "openpyxl" };
var installSuccess = await orchestrator.InstallPackagesAsync(
    environmentId: currentEnv.ID,
    packages: packages,
    progress: new Progress<string>(Console.WriteLine)
);

// Now use the packages
if (installSuccess)
{
    var (success, output) = await orchestrator.ExecuteAsync(@"
import requests
response = requests.get('https://api.github.com')
print(f'Status: {response.status_code}')
");
}
```

### Example 4: Executing Scripts with Variables

```csharp
// Get current session
var session = orchestrator.GetCurrentSession();

// Execute with variables
var variables = new Dictionary<string, object>
{
    ["input_file"] = @"C:\data\input.csv",
    ["output_file"] = @"C:\data\output.csv",
    ["threshold"] = 0.75
};

var result = await orchestrator.ExecuteWithVariablesAsync(
    session: session,
    code: @"
import pandas as pd

# Use variables from C#
df = pd.read_csv(input_file)
filtered = df[df['score'] > threshold]
filtered.to_csv(output_file, index=False)

print(f'Filtered {len(filtered)} rows from {len(df)} total')
",
    variables: variables
);
```

## Multi-User Mode Examples

### Example 5: Multi-User Web Service

```csharp
// Initialize in multi-user mode
var orchestrator = new PythonRuntimeOrchestrator(beepService);
await orchestrator.InitializeWithEmbeddedPythonAsync(
    mode: PythonEngineMode.MultiUserWithEnvAndScopeAndSession
);

// Handle user request
public async Task<string> HandleUserRequest(string username, string pythonCode)
{
    // Get or create session for user (isolated environment)
    var session = orchestrator.GetOrCreateUserSession(username);
    
    // Execute code in user's isolated session
    var (success, output) = await orchestrator.ExecuteForUserAsync(
        username: username,
        code: pythonCode,
        timeout: 120
    );
    
    return output;
}

// Create dedicated environment for a user
var (env, session) = await orchestrator.CreateUserEnvironmentAsync(
    username: "alice@example.com",
    envName: "alice_workspace",
    packageProfiles: new List<string> { "data-science", "ml" }
);
```

### Example 6: Multi-User Concurrent Execution

```csharp
// Handle multiple users concurrently
var users = new[] { "user1", "user2", "user3" };
var tasks = users.Select(async user =>
{
    var code = $@"
import time
print('User {user} starting...')
time.sleep(1)
result = sum(range(1000000))
print(f'User {user} result: {{result}}')
";
    
    var (success, output) = await orchestrator.ExecuteForUserAsync(
        username: user,
        code: code
    );
    
    return (user, output);
});

var results = await Task.WhenAll(tasks);

foreach (var (user, output) in results)
{
    Console.WriteLine($"{user}:\n{output}\n");
}
```

### Example 7: User Session Management

```csharp
// Terminate a user's session when they log out
orchestrator.TerminateUserSession("user1");

// Get diagnostics for monitoring
var diagnostics = orchestrator.GetDiagnostics();
Console.WriteLine($"Active Sessions: {diagnostics["ActiveSessions"]}");
Console.WriteLine($"Total Environments: {diagnostics["TotalEnvironments"]}");

// Perform cleanup periodically
orchestrator.PerformMaintenance(
    sessionMaxAge: TimeSpan.FromHours(12),
    environmentMaxIdleTime: TimeSpan.FromDays(7)
);
```

## Advanced Examples

### Example 8: Executing Python Scripts from Files

```csharp
var session = orchestrator.GetCurrentSession();

var (success, output) = await orchestrator.ExecuteScriptAsync(
    session: session,
    filePath: @"C:\scripts\data_processing.py",
    timeout: 300, // 5 minutes
    progress: progressReporter
);
```

### Example 9: Batch Execution

```csharp
var session = orchestrator.GetCurrentSession();

var commands = new List<string>
{
    "import numpy as np",
    "x = np.array([1, 2, 3, 4, 5])",
    "mean = x.mean()",
    "std = x.std()",
    "print(f'Mean: {mean}, Std: {std}')"
};

var results = await orchestrator.ExecuteBatchAsync(
    session: session,
    commands: commands
);

foreach (var result in results)
{
    Console.WriteLine(result);
}
```

### Example 10: Switching Modes at Runtime

```csharp
// Start in single-user mode
await orchestrator.InitializeWithEmbeddedPythonAsync(
    mode: PythonEngineMode.SingleUser
);

// ... use single-user features ...

// Switch to multi-user mode
var switched = await orchestrator.SwitchModeAsync(
    PythonEngineMode.MultiUserWithEnvAndScopeAndSession
);

if (switched)
{
    // Now can use multi-user features
    var session = orchestrator.GetOrCreateUserSession("newuser");
}
```

### Example 11: Environment Discovery and Management

```csharp
// Refresh Python installations
orchestrator.RefreshPythonInstallations();

// List available installations
foreach (var installation in orchestrator.AvailablePythonInstallations)
{
    Console.WriteLine($"Python {installation.Version} at {installation.RootPath}");
}

// List managed environments
foreach (var env in orchestrator.ManagedEnvironments)
{
    Console.WriteLine($"Environment: {env.Name}");
    Console.WriteLine($"  Path: {env.Path}");
    Console.WriteLine($"  Created by: {env.CreatedBy}");
    Console.WriteLine($"  Active: {env.IsActive}");
    Console.WriteLine($"  Sessions: {env.Sessions.Count}");
}

// Get specific environment
var env = orchestrator.GetEnvironment("data_science");
if (env != null)
{
    Console.WriteLine($"Found environment: {env.Name}");
}

// Delete environment
var deleted = orchestrator.DeleteEnvironment("old_env");
```

## Best Practices

### 1. Working Directory
```csharp
// ✅ Good: Set working directory for organized environment storage
orchestrator.WorkingDirectory = @"C:\MyProject";
await orchestrator.InitializeWithEmbeddedPythonAsync();
// Environments will be created in C:\MyProject\python_environments\

// ✅ Automatic: BeepShell extension sets this automatically
// to the shell directory

// ⚠️ Default: If not set, uses Python runtime path
```

### 2. Initialization
```csharp
// ✅ Good: Use embedded Python for consistent deployment
await orchestrator.InitializeWithEmbeddedPythonAsync();

// ⚠️ Use existing Python only when you control the environment
await orchestrator.InitializeWithExistingPythonAsync(knownPythonPath);
```

### 2. Error Handling
```csharp
// ✅ Good: Always check initialization result
var success = await orchestrator.InitializeWithEmbeddedPythonAsync();
if (!success)
{
    logger.LogError("Failed to initialize Python orchestrator");
    return;
}

// ✅ Good: Check execution results
var (success, output) = await orchestrator.ExecuteAsync(code);
if (!success)
{
    logger.LogError($"Python execution failed: {output}");
}
```

### 3. Resource Management
```csharp
// ✅ Good: Use 'using' pattern
using (var orchestrator = new PythonRuntimeOrchestrator(beepService))
{
    await orchestrator.InitializeWithEmbeddedPythonAsync();
    
    // ... use orchestrator ...
    
} // Automatically cleans up

// ✅ Good: Periodic maintenance
Timer maintenanceTimer = new Timer(_ => 
{
    orchestrator.PerformMaintenance();
}, null, TimeSpan.FromHours(1), TimeSpan.FromHours(1));
```

### 4. Progress Reporting
```csharp
// ✅ Good: Provide progress feedback for long operations
var progress = new Progress<string>(msg => 
{
    logger.LogInformation(msg);
    StatusBar.Text = msg;
});

await orchestrator.InitializeWithEmbeddedPythonAsync(progress: progress);
```

### 5. Mode Selection
```csharp
// ✅ Single-user mode for:
// - Desktop applications
// - Development tools
// - Single-user CLIs

// ✅ Multi-user mode for:
// - Web services
// - Multi-tenant applications
// - Concurrent processing systems
```

## Complete Application Example

```csharp
using Beep.Python.RuntimeEngine;
using Beep.Python.Model;
using Microsoft.Extensions.DependencyInjection;

public class PythonService
{
    private readonly IPythonRuntimeOrchestrator _orchestrator;
    private readonly ILogger<PythonService> _logger;
    
    public PythonService(  ILogger<PythonService> logger)
    {
        _orchestrator = new PythonRuntimeOrchestrator(beepService);
        _logger = logger;
    }
    
    public async Task<bool> InitializeAsync()
    {
        try
        {
            var progress = new Progress<string>(msg => _logger.LogInformation(msg));
            
            var success = await _orchestrator.InitializeWithEmbeddedPythonAsync(
                mode: PythonEngineMode.SingleUser,
                progress: progress
            );
            
            if (success)
            {
                // Create default environment with common packages
                await _orchestrator.CreateAndSetEnvironmentAsync(
                    "default",
                    packageProfiles: new List<string> { "base", "data-science" },
                    progress: progress
                );
            }
            
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Python service");
            return false;
        }
    }
    
    public async Task<string> ExecutePythonAsync(string code)
    {
        try
        {
            var (success, output) = await _orchestrator.ExecuteAsync(code, timeout: 60);
            return success ? output : $"Error: {output}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Python execution error");
            return $"Exception: {ex.Message}";
        }
    }
    
    public void Dispose()
    {
        _orchestrator?.Dispose();
    }
}

// Dependency Injection setup
services.AddSingleton<PythonService>();

// Usage
var pythonService = serviceProvider.GetRequiredService<PythonService>();
await pythonService.InitializeAsync();

var result = await pythonService.ExecutePythonAsync(@"
import pandas as pd
df = pd.DataFrame({'A': [1, 2, 3], 'B': [4, 5, 6]})
print(df.describe())
");

Console.WriteLine(result);
```

## Diagnostics and Monitoring

```csharp
// Get current state
var diagnostics = orchestrator.GetDiagnostics();

Console.WriteLine($"Initialized: {diagnostics["IsInitialized"]}");
Console.WriteLine($"Mode: {diagnostics["Mode"]}");
Console.WriteLine($"Admin Environment: {diagnostics["AdminEnvironment"]}");
Console.WriteLine($"Current Environment: {diagnostics["CurrentEnvironment"]}");
Console.WriteLine($"Total Environments: {diagnostics["TotalEnvironments"]}");
Console.WriteLine($"Total Sessions: {diagnostics["TotalSessions"]}");
Console.WriteLine($"Active Sessions: {diagnostics["ActiveSessions"]}");

// Access underlying managers for advanced operations
var runtimeManager = orchestrator.RuntimeManager;
var sessionManager = orchestrator.SessionManager;
var envManager = orchestrator.VirtualEnvManager;
var executeManager = orchestrator.ExecuteManager;
```

## Migration from Direct Manager Usage

### Before (Old Way)
```csharp
var runtimeManager = new PythonNetRunTimeManager(beepService);
var runtime = runtimeManager.Initialize(pythonPath);
var success = runtimeManager.Initialize(runtime, envPath, "myenv", PythonEngineMode.SingleUser);
var session = runtimeManager.CreateSessionForSingleUserMode(runtime, basePath, "user", "env");
var executeManager = runtimeManager.ExecuteManager;
var (success, output) = await executeManager.ExecuteCodeAsync(code, session);
```

### After (New Way with Orchestrator)
```csharp
var orchestrator = new PythonRuntimeOrchestrator(beepService);
await orchestrator.InitializeWithExistingPythonAsync(pythonPath);
var (success, output) = await orchestrator.ExecuteAsync(code);
```

## Troubleshooting

### Issue: Initialization Fails
```csharp
// Check diagnostics
var diagnostics = orchestrator.GetDiagnostics();
if (!(bool)diagnostics["IsInitialized"])
{
    // Try refreshing Python installations
    orchestrator.RefreshPythonInstallations();
    
    // Check available installations
    foreach (var install in orchestrator.AvailablePythonInstallations)
    {
        Console.WriteLine($"Found: {install.RootPath}");
    }
}
```

### Issue: Environment Not Found
```csharp
// List all environments
foreach (var env in orchestrator.ManagedEnvironments)
{
    Console.WriteLine($"{env.ID}: {env.Name} at {env.Path}");
}

// Try getting by name or ID
var env = orchestrator.GetEnvironment("myenv");
if (env == null)
{
    // Create if doesn't exist
    env = await orchestrator.CreateEnvironmentAsync("myenv");
}
```

### Issue: Code Execution Timeout
```csharp
// Increase timeout for long-running code
var (success, output) = await orchestrator.ExecuteAsync(
    code,
    timeout: 600 // 10 minutes
);
```

## Summary

The `PythonRuntimeOrchestrator` simplifies Python integration by:

1. **Automatic admin environment setup** - Handles all system-level operations
2. **Mode-aware API** - Different methods for single vs multi-user scenarios
3. **Simplified initialization** - One call to get everything running
4. **Environment management** - Easy create, switch, and delete operations
5. **Session management** - Automatic user isolation in multi-user mode
6. **Integrated execution** - Simple API for running Python code
7. **Resource cleanup** - Automatic disposal and maintenance

Use the orchestrator as your **primary interface** to the Python runtime system!
