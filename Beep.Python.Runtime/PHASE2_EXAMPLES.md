# Phase 2 Features - Usage Examples

## Health Monitoring

### Example 1: Basic Health Monitoring
```csharp
using Beep.Python.RuntimeEngine.Monitoring;

// Create health monitor
var registry = new PythonRuntimeRegistry(beepService);
await registry.InitializeAsync();

var monitor = new PythonHealthMonitor(registry, beepService);

// Start periodic monitoring (every 30 minutes)
monitor.StartMonitoring(intervalMinutes: 30);

// Monitor runs in background...

// Later: stop monitoring
monitor.StopMonitoring();
```

### Example 2: Manual Health Check
```csharp
// Perform immediate health check
var report = await monitor.PerformHealthCheckAsync();

Console.WriteLine($"Overall Health: {report.OverallHealth}");
Console.WriteLine($"Summary: {report.Summary}");

foreach (var check in report.RuntimeChecks)
{
    Console.WriteLine($"\nRuntime: {check.RuntimeName}");
    Console.WriteLine($"  Status: {check.Status}");
    Console.WriteLine($"  Python Version: {check.PythonVersion}");
    
    if (check.Issues.Count > 0)
    {
        Console.WriteLine("  Issues:");
        foreach (var issue in check.Issues)
        {
            Console.WriteLine($"    - {issue}");
        }
    }
}
```

### Example 3: Check Specific Runtime
```csharp
// Get runtime from registry
var runtime = registry.GetRuntime("runtime-id-here");

// Check its health
var healthCheck = await monitor.CheckRuntimeHealthAsync(runtime);

if (healthCheck.IsHealthy)
{
    Console.WriteLine($"✅ {runtime.Name} is healthy");
}
else
{
    Console.WriteLine($"❌ {runtime.Name} has issues:");
    foreach (var issue in healthCheck.Issues)
    {
        Console.WriteLine($"   - {issue}");
    }
}
```

## Integration Helpers

### Example 4: Quick Setup (One Line)
```csharp
using Beep.Python.RuntimeEngine.Integration;

// Setup Python with data science template
var result = await BootstrapIntegration.QuickSetupAsync(
    beepService,
    templateName: "data-science");

if (result.IsSuccessful)
{
    Console.WriteLine($"✅ Environment ready at: {result.EnvironmentPath}");
}
```

### Example 5: Extension Methods
```csharp
using Beep.Python.RuntimeEngine.Integration;

// IBeepService extension method
var result = await beepService.EnsurePythonAsync("machine-learning");

// Or get runtime directly
var runtime = await beepService.GetPythonRuntimeAsync("minimal");
```

### Example 6: Custom Setup with Integration Helper
```csharp
var options = new BootstrapOptions
{
    EnvironmentName = "my-project",
    PackageProfiles = new List<string> { "base", "web" }
};

var result = await BootstrapIntegration.CustomSetupAsync(
    beepService,
    options,
    progress: BootstrapIntegration.CreateConsoleProgress());
```

### Example 7: Setup with Health Monitoring
```csharp
// Setup environment and start monitoring in one call
var (result, monitor) = await BootstrapIntegration.SetupWithMonitoringAsync(
    beepService,
    EnvironmentTemplates.FullStack,
    monitoringIntervalMinutes: 15);

if (result.IsSuccessful)
{
    Console.WriteLine("✅ Environment ready and monitoring started");
    
    // Monitor runs in background...
    
    // Later: stop monitoring
    monitor.StopMonitoring();
}
```

### Example 8: Console Progress Reporter
```csharp
// Create colored console progress reporter
var consoleProgress = BootstrapIntegration.CreateConsoleProgress();

var result = await bootstrapManager.EnsurePythonEnvironmentAsync(
    EnvironmentTemplates.MachineLearning,
    consoleProgress);
```

### Example 9: Log Progress Reporter
```csharp
// Create progress reporter that logs to DMEEditor
var logProgress = BootstrapIntegration.CreateLogProgress(beepService);

var result = await bootstrapManager.EnsurePythonEnvironmentAsync(
    EnvironmentTemplates.DataScience,
    logProgress);
```

### Example 10: Ensure Runtime for Existing Code
```csharp
// Get a PythonRunTime object for use with existing managers
var runtime = await BootstrapIntegration.EnsureRuntimeAsync(
    beepService,
    templateName: "minimal");

// Now use with existing PythonNetRunTimeManager
pythonManager.Initialize(runtime);
```

## Application Integration

### Example 11: Application Startup with Monitoring
```csharp
public class MyApplication
{
    private IPythonHealthMonitor _healthMonitor;
    private BootstrapResult _bootstrapResult;
    
    public async Task StartupAsync()
    {
        // Quick setup with monitoring
        var (result, monitor) = await BootstrapIntegration.SetupWithMonitoringAsync(
            _beepService,
            EnvironmentTemplates.DataScience,
            monitoringIntervalMinutes: 30,
            progress: BootstrapIntegration.CreateLogProgress(_beepService));
        
        _bootstrapResult = result;
        _healthMonitor = monitor;
        
        if (!result.IsSuccessful)
        {
            throw new InvalidOperationException(
                "Failed to initialize Python: " + 
                string.Join(", ", result.ValidationMessages));
        }
        
        Console.WriteLine("Application ready!");
    }
    
    public async Task ShutdownAsync()
    {
        _healthMonitor?.StopMonitoring();
        _healthMonitor?.Dispose();
    }
}
```

### Example 12: Health-Based Automatic Recovery
```csharp
public class ResilientPythonManager
{
    private readonly IPythonHealthMonitor _monitor;
   
    
    public async Task<bool> EnsureHealthyRuntimeAsync()
    {
        var report = await _monitor.PerformHealthCheckAsync();
        
        // If any runtime is unhealthy, attempt recovery
        foreach (var check in report.RuntimeChecks)
        {
            if (check.Status == HealthStatus.Unhealthy)
            {
                Console.WriteLine($"⚠ Runtime {check.RuntimeName} is unhealthy, attempting recovery...");
                
                // Re-provision
                var result = await BootstrapIntegration.QuickSetupAsync(
                    _beepService,
                    "minimal");
                
                if (result.IsSuccessful)
                {
                    Console.WriteLine($"✅ Recovery successful");
                    return true;
                }
            }
        }
        
        return report.OverallHealth == HealthStatus.Healthy;
    }
}
```

### Example 13: Scheduled Health Reporting
```csharp
public class HealthReporter
{
    private readonly IPythonHealthMonitor _monitor;
    private Timer _reportTimer;
    
    public void StartReporting(int reportIntervalHours = 24)
    {
        _reportTimer = new Timer(
            async _ => await GenerateHealthReportAsync(),
            null,
            TimeSpan.Zero,
            TimeSpan.FromHours(reportIntervalHours));
    }
    
    private async Task GenerateHealthReportAsync()
    {
        var report = await _monitor.PerformHealthCheckAsync();
        
        var reportText = $@"
Python Environment Health Report
Generated: {report.Timestamp:yyyy-MM-dd HH:mm:ss}
Overall Status: {report.OverallHealth}
Summary: {report.Summary}

Runtime Details:
{string.Join("\n", report.RuntimeChecks.Select(c => 
    $"  - {c.RuntimeName}: {c.Status} {(c.IsHealthy ? "✅" : "❌")}"))}
";
        
        Console.WriteLine(reportText);
        // Could also email, log to file, send to monitoring service, etc.
    }
}
```

## Best Practices

### Practice 1: Always Use Progress Reporting for Long Operations
```csharp
// ❌ Bad - no visibility
var result = await bootstrapManager.EnsurePythonEnvironmentAsync(options);

// ✅ Good - user sees progress
var progress = BootstrapIntegration.CreateConsoleProgress();
var result = await bootstrapManager.EnsurePythonEnvironmentAsync(options, progress);
```

### Practice 2: Enable Health Monitoring for Production
```csharp
// Development: Simple setup
var result = await beepService.EnsurePythonAsync("minimal");

// Production: Setup with monitoring
var (result, monitor) = await BootstrapIntegration.SetupWithMonitoringAsync(
    beepService,
    EnvironmentTemplates.DataScience,
    monitoringIntervalMinutes: 60);
```

### Practice 3: Handle Bootstrap Failures Gracefully
```csharp
try
{
    var result = await bootstrapManager.EnsurePythonEnvironmentAsync(options);
    
    if (!result.IsSuccessful)
    {
        // Log validation messages
        foreach (var message in result.ValidationMessages)
        {
            Console.WriteLine($"Validation issue: {message}");
        }
        
        // Fallback to system Python or show error to user
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Bootstrap failed: {ex.Message}");
    // Implement fallback strategy
}
```

### Practice 4: Reuse Bootstrap Manager
```csharp
// ❌ Bad - creates new instance every time
public async Task SetupEnvironmentAsync()
{
    var manager = BootstrapIntegration.CreateBootstrapManager(beepService);
    await manager.EnsurePythonEnvironmentAsync(options);
}

// ✅ Good - reuse instance
private readonly PythonBootstrapManager _bootstrapManager;

public MyClass(IBeepService beepService)
{
    _bootstrapManager = BootstrapIntegration.CreateBootstrapManager(beepService);
}

public async Task SetupEnvironmentAsync()
{
    await _bootstrapManager.EnsurePythonEnvironmentAsync(options);
}
```
