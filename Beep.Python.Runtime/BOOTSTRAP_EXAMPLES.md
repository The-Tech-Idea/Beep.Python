# Python Bootstrap System - Usage Examples

This document demonstrates how to use the new Python bootstrap system for one-call environment setup.

## Basic Usage

### Example 1: Minimal Environment Setup
```csharp
using Beep.Python.RuntimeEngine.Infrastructure;
using Beep.Python.RuntimeEngine.Templates;

// Create a minimal Python environment with just essential packages
var bootstrapManager = new PythonBootstrapManager(
    provisioner, registry, packageManager, venvManager, beepService);

var result = await bootstrapManager.EnsurePythonEnvironmentAsync(
    EnvironmentTemplates.Minimal,
    progress: new Progress<BootstrapProgress>(p => 
        Console.WriteLine($"[{p.PercentComplete}%] {p.Message}")));

if (result.IsSuccessful)
{
    Console.WriteLine($"Environment ready at: {result.EnvironmentPath}");
}
```

### Example 2: Data Science Environment
```csharp
// Create a data science environment with numpy, pandas, matplotlib, etc.
var result = await bootstrapManager.EnsurePythonEnvironmentAsync(
    EnvironmentTemplates.DataScience,
    progress: new Progress<BootstrapProgress>(p => 
        Console.WriteLine($"{p.Stage}: {p.Message}")));
```

### Example 3: Machine Learning Environment
```csharp
// Create ML environment with PyTorch and Transformers
var result = await bootstrapManager.EnsurePythonEnvironmentAsync(
    EnvironmentTemplates.MachineLearning);
```

## Custom Configurations

### Example 4: Custom Package Selection
```csharp
var options = new BootstrapOptions
{
    EnsureEmbeddedPython = true,
    CreateVirtualEnvironment = true,
    EnvironmentName = "my-custom-env",
    PackageProfiles = new List<string> { "base", "data-science", "web" },
    SetAsDefault = true
};

var result = await bootstrapManager.EnsurePythonEnvironmentAsync(options);
```

### Example 5: Using Template Builder
```csharp
var customTemplate = EnvironmentTemplates.Custom(
    name: "my-project",
    profiles: new List<string> { "base", "machine-learning" },
    useVirtualEnv: true,
    setAsDefault: false);

var result = await bootstrapManager.EnsurePythonEnvironmentAsync(customTemplate);
```

### Example 6: Reusing Existing Environment
```csharp
var options = new BootstrapOptions
{
    EnsureEmbeddedPython = false,  // Use existing runtime
    CreateVirtualEnvironment = false,  // Don't create venv
    PackageProfiles = new List<string> { "data-science" }  // Just install packages
};

var result = await bootstrapManager.EnsurePythonEnvironmentAsync(options);
```

## Advanced Scenarios

### Example 7: Progress Tracking with Detailed Stages
```csharp
var progress = new Progress<BootstrapProgress>(p =>
{
    switch (p.Stage)
    {
        case BootstrapStage.ProvisioningPython:
            Console.ForegroundColor = ConsoleColor.Yellow;
            break;
        case BootstrapStage.InstallingPackages:
            Console.ForegroundColor = ConsoleColor.Cyan;
            break;
        case BootstrapStage.Complete:
            Console.ForegroundColor = ConsoleColor.Green;
            break;
        case BootstrapStage.Failed:
            Console.ForegroundColor = ConsoleColor.Red;
            break;
    }
    
    Console.WriteLine($"[{p.PercentComplete,3}%] {p.Stage,-20} {p.Message}");
    Console.ResetColor();
});

var result = await bootstrapManager.EnsurePythonEnvironmentAsync(
    EnvironmentTemplates.FullStack, progress);
```

### Example 8: Custom Path Configuration
```csharp
var options = new BootstrapOptions
{
    EmbeddedPythonPath = @"C:\MyApp\Python",
    VirtualEnvironmentPath = @"C:\MyApp\Environments\MainEnv",
    PackageProfiles = new List<string> { "base", "web" }
};

var result = await bootstrapManager.EnsurePythonEnvironmentAsync(options);
```

### Example 9: Cancellation Support
```csharp
var cts = new CancellationTokenSource();
cts.CancelAfter(TimeSpan.FromMinutes(5));  // Timeout after 5 minutes

try
{
    var result = await bootstrapManager.EnsurePythonEnvironmentAsync(
        EnvironmentTemplates.MachineLearning,
        cancellationToken: cts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Bootstrap operation was cancelled");
}
```

## Template Discovery

### Example 10: List Available Templates
```csharp
var templates = EnvironmentTemplates.GetAvailableTemplates();
var descriptions = EnvironmentTemplates.GetTemplateDescriptions();

foreach (var template in templates)
{
    Console.WriteLine($"{template}: {descriptions[template]}");
}
```

### Example 11: Dynamic Template Selection
```csharp
Console.WriteLine("Select template: minimal, data-science, machine-learning, web-development, full-stack");
var templateName = Console.ReadLine();

var template = EnvironmentTemplates.GetTemplate(templateName);
if (template != null)
{
    var result = await bootstrapManager.EnsurePythonEnvironmentAsync(template);
}
else
{
    Console.WriteLine("Invalid template name");
}
```

## Result Handling

### Example 12: Detailed Result Analysis
```csharp
var result = await bootstrapManager.EnsurePythonEnvironmentAsync(
    EnvironmentTemplates.DataScience);

Console.WriteLine($"Success: {result.IsSuccessful}");
Console.WriteLine($"Runtime ID: {result.BaseRuntimeId}");
Console.WriteLine($"Environment Path: {result.EnvironmentPath}");
Console.WriteLine($"Duration: {(result.EndTime - result.StartTime).TotalSeconds:F2}s");

Console.WriteLine("\nInstalled Profiles:");
foreach (var profile in result.InstalledProfiles)
{
    Console.WriteLine($"  - {profile}");
}

Console.WriteLine("\nValidation Messages:");
foreach (var message in result.ValidationMessages)
{
    Console.WriteLine($"  {message}");
}
```

## Integration Examples

### Example 13: Application Startup Integration
```csharp
public class MyApplication
{
    private readonly IPythonBootstrapManager _bootstrapManager;
    
    public async Task InitializeAsync()
    {
        // Ensure Python environment on first run
        var result = await _bootstrapManager.EnsurePythonEnvironmentAsync(
            EnvironmentTemplates.DataScience);
            
        if (!result.IsSuccessful)
        {
            throw new InvalidOperationException(
                "Failed to initialize Python environment");
        }
        
        // Application is now ready to use Python
        Console.WriteLine("Python environment initialized");
    }
}
```

### Example 14: Multi-Environment Management
```csharp
// Create separate environments for different purposes
var devResult = await bootstrapManager.EnsurePythonEnvironmentAsync(
    EnvironmentTemplates.Custom("dev", new List<string> { "base", "data-science" }));

var testResult = await bootstrapManager.EnsurePythonEnvironmentAsync(
    EnvironmentTemplates.Custom("test", new List<string> { "base" }));

var prodResult = await bootstrapManager.EnsurePythonEnvironmentAsync(
    EnvironmentTemplates.Custom("prod", new List<string> { "base", "machine-learning" }));
```

## Configuration Persistence

The bootstrap system automatically persists:
- **Runtime Registry**: `~/.beep-python/runtimes.json`
- **Package Profiles**: `~/.beep-python/package-requirements.json`

These configurations are loaded automatically on subsequent runs, enabling:
- Fast environment discovery
- Cached runtime information
- Reusable package profiles
