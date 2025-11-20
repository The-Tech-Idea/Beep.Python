# Phase 3: Advanced Features - Examples

This document provides comprehensive examples for Phase 3 features of the Python Bootstrap System.

## Table of Contents
1. [Python Version Management](#python-version-management)
2. [Offline Mode](#offline-mode)
3. [Advanced Diagnostics](#advanced-diagnostics)
4. [Complete Integration Scenarios](#complete-integration-scenarios)

---

## Python Version Management

### Example 1: List Available Python Versions
```csharp
var versionManager = new PythonVersionManager(
    beepService,
    registry,
    provisioner);

// Get all available versions
var availableVersions = versionManager.GetAvailableVersions();

Console.WriteLine("Available Python Versions:");
foreach (var version in availableVersions)
{
    var marker = version.IsRecommended ? "⭐" : "  ";
    var status = version.IsSupported ? "Supported" : "Not Supported";
    Console.WriteLine($"{marker} Python {version.Version} - {status}");
    Console.WriteLine($"   Released: {version.ReleaseDate:yyyy-MM-dd}");
    Console.WriteLine($"   Size: {version.Size / 1024.0 / 1024.0:F2} MB");
}

// Output:
// Available Python Versions:
// ⭐ Python 3.11.9 - Supported
//    Released: 2024-04-02
//    Size: 10.00 MB
//   Python 3.12.3 - Supported
//    Released: 2024-04-09
//    Size: 11.00 MB
```

### Example 2: Install Specific Python Version
```csharp
var progress = new Progress<VersionInstallProgress>(p =>
{
    Console.WriteLine($"[{p.Percentage:F0}%] {p.Stage}: {p.Message}");
});

// Install Python 3.12.3
var runtime = await versionManager.EnsureVersionAsync(
    "3.12.3",
    progress);

Console.WriteLine($"Python {runtime.Name} installed at: {runtime.Path}");

// Output:
// [10%] Checking: Checking for Python 3.12.3...
// [20%] Downloading: Installing Python 3.12.3...
// [50%] Downloading: Downloading Python distribution...
// [80%] Configuring: Configuring Python environment...
// [100%] Complete: Python 3.12.3 installed successfully
// Python 3.12.3 installed at: C:\Users\username\.beep-python\embedded
```

### Example 3: Check Installation Status
```csharp
var statusList = await versionManager.GetVersionStatusListAsync();

Console.WriteLine("Python Version Status:");
Console.WriteLine("┌─────────────┬───────────┬─────────┬─────────────┐");
Console.WriteLine("│ Version     │ Installed │ Default │ Recommended │");
Console.WriteLine("├─────────────┼───────────┼─────────┼─────────────┤");

foreach (var status in statusList)
{
    var installed = status.IsInstalled ? "✓" : "✗";
    var isDefault = status.IsDefault ? "✓" : " ";
    var recommended = status.IsRecommended ? "⭐" : " ";
    
    Console.WriteLine($"│ {status.Version,-11} │ {installed,9} │ {isDefault,7} │ {recommended,11} │");
}

Console.WriteLine("└─────────────┴───────────┴─────────┴─────────────┘");
```

### Example 4: Switch Default Python Version
```csharp
var progress = new Progress<string>(msg => Console.WriteLine(msg));

// Set Python 3.11.9 as default
var success = await versionManager.SetDefaultVersionAsync(
    "3.11.9",
    progress);

if (success)
{
    Console.WriteLine("Default Python version updated!");
}

// Output:
// Setting Python 3.11.9 as default...
// Python 3.11.9 is now the default runtime
// Default Python version updated!
```

### Example 5: Uninstall Python Version
```csharp
var progress = new Progress<string>(msg => Console.WriteLine(msg));

// Uninstall Python 3.10.11
var success = await versionManager.UninstallVersionAsync(
    "3.10.11",
    progress);

if (success)
{
    Console.WriteLine("Python version uninstalled successfully");
}

// Output:
// Uninstalling Python 3.10.11...
// Deleting C:\Users\username\.beep-python\versions\3.10.11...
// Updating registry...
// Python 3.10.11 uninstalled successfully
```

### Example 6: Get Installed Versions Only
```csharp
var installedVersions = await versionManager.GetInstalledVersionsAsync();

Console.WriteLine($"Found {installedVersions.Count} installed Python version(s):");

foreach (var version in installedVersions)
{
    Console.WriteLine($"  • Python {version.Version}");
    Console.WriteLine($"    Location: {version.InstallPath}");
    Console.WriteLine($"    Size: {new FileInfo(version.InstallPath).Length / 1024.0 / 1024.0:F2} MB");
}
```

---

## Offline Mode

### Example 7: Create Offline Package
```csharp
var offlineManager = new PythonOfflineManager(beepService, registry);

var options = new OfflinePackageOptions
{
    PythonVersion = "3.11.9",
    Packages = new List<string> { "numpy", "pandas", "matplotlib" },
    PackageProfiles = new List<string> { "data-science" },
    OutputPath = @"C:\deploy\python-offline.zip"
};

var progress = new Progress<OfflineProgress>(p =>
{
    Console.WriteLine($"[{p.Percentage:F0}%] {p.Stage}: {p.Message}");
});

var packagePath = await offlineManager.CreateOfflinePackageAsync(
    options,
    progress);

Console.WriteLine($"Offline package created: {packagePath}");

// Output:
// [5%] Preparing: Preparing offline package...
// [20%] CopyingPython: Copying Python 3.11.9 distribution...
// [40%] DownloadingPackages: Downloading packages...
// [70%] CreatingManifest: Creating manifest...
// [85%] Packaging: Creating archive...
// [100%] Complete: Offline package created: C:\deploy\python-offline.zip
```

### Example 8: Install from Offline Package
```csharp
var offlineManager = new PythonOfflineManager(beepService, registry);

var progress = new Progress<OfflineProgress>(p =>
{
    Console.WriteLine($"[{p.Percentage:F0}%] {p.Message}");
});

var runtime = await offlineManager.InstallFromOfflinePackageAsync(
    @"C:\deploy\python-offline.zip",
    progress);

Console.WriteLine($"Python installed from offline package!");
Console.WriteLine($"Runtime ID: {runtime.Id}");
Console.WriteLine($"Path: {runtime.Path}");

// Output:
// [10%] Extracting offline package...
// [30%] Installing Python 3.11.9...
// [60%] Installing packages...
// [90%] Registering runtime...
// [100%] Installation complete
// Python installed from offline package!
```

### Example 9: Cache Python Distribution
```csharp
var offlineManager = new PythonOfflineManager(beepService, registry);

// Cache a Python distribution for offline use
var cachePath = await offlineManager.CachePythonDistributionAsync(
    "3.11.9",
    @"C:\downloads\python-3.11.9-embed-amd64.zip");

Console.WriteLine($"Python distribution cached at: {cachePath}");

// Check if cached
var isCached = offlineManager.IsPythonVersionCached("3.11.9");
Console.WriteLine($"Python 3.11.9 cached: {isCached}");

// Output:
// Python distribution cached at: C:\Users\username\.beep-python\offline-cache\distributions\python-3.11.9-embed-amd64.zip
// Python 3.11.9 cached: True
```

### Example 10: List Cached Distributions
```csharp
var offlineManager = new PythonOfflineManager(beepService, registry);

var cachedDistributions = offlineManager.GetCachedDistributions();

Console.WriteLine("Cached Python Distributions:");
foreach (var dist in cachedDistributions)
{
    Console.WriteLine($"  • Python {dist.Version}");
    Console.WriteLine($"    Size: {dist.Size / 1024.0 / 1024.0:F2} MB");
    Console.WriteLine($"    Cached: {dist.CachedAt:yyyy-MM-dd HH:mm:ss}");
    Console.WriteLine($"    Path: {dist.Path}");
}
```

### Example 11: Clear Offline Cache
```csharp
var offlineManager = new PythonOfflineManager(beepService, registry);

// Clear packages but keep distributions
var success = await offlineManager.ClearCacheAsync(keepDistributions: true);

Console.WriteLine(success 
    ? "Package cache cleared (distributions preserved)"
    : "Failed to clear cache");

// Clear everything
success = await offlineManager.ClearCacheAsync(keepDistributions: false);

Console.WriteLine(success 
    ? "All cache cleared"
    : "Failed to clear cache");
```

---

## Advanced Diagnostics

### Example 12: Run Comprehensive Diagnostics
```csharp
var diagnostics = new PythonAdvancedDiagnostics(beepService);

await registry.InitializeAsync();
var runtime = registry.GetDefaultRuntime();

var report = await diagnostics.RunComprehensiveDiagnosticsAsync(runtime);

Console.WriteLine($"Diagnostic Report for {report.RuntimeName}");
Console.WriteLine($"Duration: {report.Duration.TotalSeconds:F2}s");
Console.WriteLine();

// Basic diagnostics
Console.WriteLine("Basic Diagnostics:");
Console.WriteLine($"  Python Found: {report.BasicDiagnostics.PythonFound}");
Console.WriteLine($"  Python Version: {report.BasicDiagnostics.PythonVersion}");
Console.WriteLine($"  Pip Found: {report.BasicDiagnostics.PipFound}");
Console.WriteLine($"  Can Execute Code: {report.BasicDiagnostics.CanExecuteCode}");

// DLL dependencies
Console.WriteLine("\nDLL Dependencies:");
Console.WriteLine($"  Healthy: {report.DllDependencies.IsHealthy}");
Console.WriteLine($"  Missing Critical: {report.DllDependencies.MissingCritical.Count}");
Console.WriteLine($"  Missing Optional: {report.DllDependencies.MissingOptional.Count}");

// Performance
Console.WriteLine("\nPerformance Benchmarks:");
Console.WriteLine($"  Simple Arithmetic: {report.PerformanceBenchmarks.SimpleArithmeticMs:F2}ms");
Console.WriteLine($"  List Comprehension: {report.PerformanceBenchmarks.ListComprehensionMs:F2}ms");
Console.WriteLine($"  Overall Score: {report.PerformanceBenchmarks.OverallScore:F2}/100");

// Security
Console.WriteLine("\nSecurity Analysis:");
Console.WriteLine($"  SSL Support: {report.SecurityAnalysis.HasSslSupport}");
Console.WriteLine($"  Pip Available: {report.SecurityAnalysis.HasPip}");
Console.WriteLine($"  Security Level: {report.SecurityAnalysis.OverallSecurityLevel}");

// Disk usage
Console.WriteLine("\nDisk Usage:");
Console.WriteLine($"  Total Size: {report.DiskUsage.TotalSizeMB:F2} MB");
Console.WriteLine($"  Total Files: {report.DiskUsage.TotalFiles}");
```

### Example 13: Check DLL Dependencies
```csharp
var diagnostics = new PythonAdvancedDiagnostics(beepService);
var runtime = registry.GetDefaultRuntime();

var dllReport = await diagnostics.CheckDllDependenciesAsync(runtime.Path);

Console.WriteLine("DLL Dependency Check:");
Console.WriteLine($"Overall Health: {(dllReport.IsHealthy ? "✓ Healthy" : "✗ Issues Found")}");
Console.WriteLine();

// Show missing critical DLLs
if (dllReport.MissingCritical.Any())
{
    Console.WriteLine("Missing Critical DLLs:");
    foreach (var dll in dllReport.MissingCritical)
    {
        Console.WriteLine($"  ✗ {dll}");
    }
}

// Show all dependencies
Console.WriteLine("\nAll Dependencies:");
foreach (var dep in dllReport.Dependencies)
{
    var status = dep.Found ? "✓" : "✗";
    var required = dep.IsRequired ? "[REQUIRED]" : "[OPTIONAL]";
    Console.WriteLine($"  {status} {dep.Name} {required}");
    Console.WriteLine($"     {dep.Description}");
    if (dep.Found)
    {
        Console.WriteLine($"     Size: {dep.Size / 1024.0:F2} KB");
    }
}
```

### Example 14: Analyze Installed Packages
```csharp
var diagnostics = new PythonAdvancedDiagnostics(beepService);
var runtime = registry.GetDefaultRuntime();

var packageReport = await diagnostics.AnalyzeInstalledPackagesAsync(runtime.Path);

Console.WriteLine($"Package Analysis:");
Console.WriteLine($"Total Packages: {packageReport.TotalPackages}");

if (packageReport.OutdatedPackages.Any())
{
    Console.WriteLine($"\nOutdated Packages ({packageReport.OutdatedPackages.Count}):");
    foreach (var pkg in packageReport.OutdatedPackages)
    {
        Console.WriteLine($"  • {pkg}");
    }
}

Console.WriteLine("\nInstalled Packages:");
foreach (var pkg in packageReport.Packages)
{
    Console.WriteLine($"  • {pkg.Name} {pkg.Version}");
}
```

### Example 15: Performance Benchmarks
```csharp
var diagnostics = new PythonAdvancedDiagnostics(beepService);
var runtime = registry.GetDefaultRuntime();

var perfReport = await diagnostics.RunPerformanceBenchmarksAsync(runtime.Path);

Console.WriteLine("Performance Benchmarks:");
Console.WriteLine($"┌────────────────────────┬──────────────┐");
Console.WriteLine($"│ Benchmark              │ Time (ms)    │");
Console.WriteLine($"├────────────────────────┼──────────────┤");
Console.WriteLine($"│ Simple Arithmetic      │ {perfReport.SimpleArithmeticMs,12:F2} │");
Console.WriteLine($"│ List Comprehension     │ {perfReport.ListComprehensionMs,12:F2} │");
Console.WriteLine($"│ String Operations      │ {perfReport.StringOperationsMs,12:F2} │");
Console.WriteLine($"│ Import Time            │ {perfReport.ImportTimeMs,12:F2} │");
Console.WriteLine($"└────────────────────────┴──────────────┘");
Console.WriteLine($"\nOverall Performance Score: {perfReport.OverallScore:F2}/100");
```

### Example 16: Check Python.NET Compatibility
```csharp
var diagnostics = new PythonAdvancedDiagnostics(beepService);
var runtime = registry.GetDefaultRuntime();

var compatReport = await diagnostics.CheckPythonNetCompatibilityAsync(runtime.Path);

Console.WriteLine("Python.NET Compatibility Check:");
Console.WriteLine($"Python Version: {compatReport.PythonVersion}");
Console.WriteLine($"Compatible: {(compatReport.IsCompatible ? "✓ Yes" : "✗ No")}");
Console.WriteLine($"Architecture: {compatReport.Architecture}");
Console.WriteLine($"Has Required Modules: {compatReport.HasRequiredModules}");

Console.WriteLine("\nNotes:");
foreach (var note in compatReport.CompatibilityNotes)
{
    Console.WriteLine($"  • {note}");
}
```

### Example 17: Security Analysis
```csharp
var diagnostics = new PythonAdvancedDiagnostics(beepService);
var runtime = registry.GetDefaultRuntime();

var secReport = await diagnostics.PerformSecurityAnalysisAsync(runtime.Path);

Console.WriteLine("Security Analysis:");
Console.WriteLine($"Overall Security Level: {secReport.OverallSecurityLevel}");
Console.WriteLine();

Console.WriteLine("Checks:");
Console.WriteLine($"  Directory Readable: {(secReport.IsReadable ? "✓" : "✗")}");
Console.WriteLine($"  Directory Writable: {(secReport.IsWritable ? "✓" : "✗")}");
Console.WriteLine($"  SSL/TLS Support: {(secReport.HasSslSupport ? "✓" : "✗")}");
Console.WriteLine($"  Pip Available: {(secReport.HasPip ? "✓" : "✗")}");

if (secReport.SuspiciousFiles.Any())
{
    Console.WriteLine($"\n⚠ Suspicious Files Found ({secReport.SuspiciousFiles.Count}):");
    foreach (var file in secReport.SuspiciousFiles)
    {
        Console.WriteLine($"  • {file}");
    }
}
```

### Example 18: Disk Usage Analysis
```csharp
var diagnostics = new PythonAdvancedDiagnostics(beepService);
var runtime = registry.GetDefaultRuntime();

var diskReport = diagnostics.AnalyzeDiskUsage(runtime.Path);

Console.WriteLine($"Disk Usage Report for: {diskReport.RootPath}");
Console.WriteLine($"Total Size: {diskReport.TotalSizeMB:F2} MB");
Console.WriteLine($"Total Files: {diskReport.TotalFiles}");
Console.WriteLine();

Console.WriteLine("Breakdown by Directory:");
var sortedUsage = diskReport.SubdirectoryUsage
    .OrderByDescending(x => x.Value)
    .Take(10);

foreach (var dir in sortedUsage)
{
    var sizeMB = dir.Value / (1024.0 * 1024.0);
    var percentage = (dir.Value * 100.0) / diskReport.TotalSizeBytes;
    Console.WriteLine($"  {dir.Key,-30} {sizeMB,8:F2} MB ({percentage:F1}%)");
}
```

---

## Complete Integration Scenarios

### Example 19: Multi-Version Development Environment
```csharp
// Setup multiple Python versions for development
var versionManager = new PythonVersionManager(beepService, registry, provisioner);
var bootstrapManager = new PythonBootstrapManager(beepService, registry, 
    provisioner, packageManager, venvManager);

// Install Python 3.11.9 for production
Console.WriteLine("Setting up production environment (Python 3.11.9)...");
var prodRuntime = await versionManager.EnsureVersionAsync("3.11.9");
await versionManager.SetDefaultVersionAsync("3.11.9");

// Install Python 3.12.3 for testing new features
Console.WriteLine("Setting up test environment (Python 3.12.3)...");
var testRuntime = await versionManager.EnsureVersionAsync("3.12.3");

// Create production environment with data science packages
var prodResult = await bootstrapManager.EnsurePythonEnvironmentAsync(
    EnvironmentTemplates.DataScience);

Console.WriteLine($"Production environment ready: {prodResult.IsSuccessful}");
Console.WriteLine($"Test runtime available: {testRuntime.Name}");
```

### Example 20: Air-Gapped Deployment Workflow
```csharp
// Step 1: On connected machine, create offline package
var offlineManager = new PythonOfflineManager(beepService, registry);

var packageOptions = new OfflinePackageOptions
{
    PythonVersion = "3.11.9",
    PackageProfiles = new List<string> { "data-science", "machine-learning" },
    OutputPath = @"D:\deployment\python-package.zip"
};

var packagePath = await offlineManager.CreateOfflinePackageAsync(packageOptions);
Console.WriteLine($"Offline package ready for deployment: {packagePath}");

// Step 2: Transfer to air-gapped machine and install
// (on air-gapped machine)
var runtime = await offlineManager.InstallFromOfflinePackageAsync(
    @"D:\deployment\python-package.zip");

Console.WriteLine($"Python installed offline: {runtime.Path}");

// Step 3: Verify installation
var diagnostics = new PythonAdvancedDiagnostics(beepService);
var report = await diagnostics.RunComprehensiveDiagnosticsAsync(runtime);

Console.WriteLine($"Installation verified: {report.IsSuccessful}");
```

### Example 21: Complete Health Monitoring Setup
```csharp
// Setup with version management, monitoring, and diagnostics
var versionManager = new PythonVersionManager(beepService, registry, provisioner);
var healthMonitor = new PythonHealthMonitor(registry, beepService);
var diagnostics = new PythonAdvancedDiagnostics(beepService);

// Ensure Python is installed
var runtime = await versionManager.EnsureVersionAsync("3.11.9");

// Start periodic health monitoring (every 30 minutes)
healthMonitor.StartMonitoring(intervalMinutes: 30);

// Run initial comprehensive diagnostics
var report = await diagnostics.RunComprehensiveDiagnosticsAsync(runtime);

if (!report.IsSuccessful || !report.DllDependencies.IsHealthy)
{
    Console.WriteLine("⚠ Issues detected, running detailed diagnostics...");
    
    // Check specific issues
    var dllReport = await diagnostics.CheckDllDependenciesAsync(runtime.Path);
    var perfReport = await diagnostics.RunPerformanceBenchmarksAsync(runtime.Path);
    
    // Report problems
    foreach (var missing in dllReport.MissingCritical)
    {
        Console.WriteLine($"✗ Missing critical DLL: {missing}");
    }
    
    if (perfReport.OverallScore < 50)
    {
        Console.WriteLine($"⚠ Low performance score: {perfReport.OverallScore:F2}");
    }
}
else
{
    Console.WriteLine("✓ All systems healthy");
}
```

---

## Best Practices

### Version Management
- Always check installation status before installing
- Use recommended versions for production
- Keep at least one fallback version installed
- Test on non-default versions before switching

### Offline Mode
- Cache distributions on build servers
- Include all required package profiles
- Verify offline packages before deployment
- Document package contents in manifest

### Diagnostics
- Run comprehensive diagnostics after installation
- Schedule regular health checks
- Monitor performance metrics over time
- Keep diagnostic reports for troubleshooting

### Integration
- Combine health monitoring with version management
- Use offline mode for air-gapped environments
- Run diagnostics before production deployment
- Maintain audit trail of installations and checks
