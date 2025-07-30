# Beep.Python.PackageManagement

![License](https://img.shields.io/badge/license-MIT-blue.svg)
![.NET](https://img.shields.io/badge/.NET-6%7C7%7C8%7C9-purple.svg)
![Version](https://img.shields.io/badge/version-1.0.16-green.svg)

Enterprise Python package management system for .NET applications with enhanced session management, virtual environment integration, and comprehensive package lifecycle management.

## ?? What's New in v1.0.16

The architecture has been significantly refactored to provide a more streamlined and efficient package management experience:

- **Consolidated Architecture**: Removed `PackageOperationManager` and consolidated all functionality into `PythonPackageManager`
- **Enhanced Session Management**: Improved multi-user session support with proper isolation
- **Async/Await Support**: Full asynchronous operations with cancellation token support
- **Streamlined Dependencies**: Simplified dependency injection and initialization
- **Better Error Handling**: Comprehensive error handling and progress reporting

## ?? Key Features

- **Unified Package Management**: All operations consolidated in a single, powerful manager
- **Session-Aware Operations**: Full support for multi-user environments with session isolation
- **Requirements File Management**: Advanced parsing and management of requirements files
- **Intelligent Package Categorization**: Auto-categorization of packages by functionality
- **Package Sets**: Predefined collections for common use cases
- **Virtual Environment Integration**: Seamless integration with Python virtual environments
- **Progress Reporting**: Real-time progress updates for long-running operations
- **Async Operations**: Non-blocking operations with cancellation support

## ?? Installation

### NuGet Package Manager
```powershell
Install-Package Beep.Python.PackageManagement -Version 1.0.16
```

### .NET CLI
```bash
dotnet add package Beep.Python.PackageManagement --version 1.0.16
```

### Package Reference
```xml
<PackageReference Include="Beep.Python.PackageManagement" Version="1.0.16" />
```

## ?? Quick Start

### Basic Setup

```csharp
using Beep.Python.PackageManagement;
using Beep.Python.Model;
using TheTechIdea.Beep.Container.Services;

// Initialize services
var beepService = new BeepService();
var pythonRuntime = new PythonNetRunTimeManager(beepService);
var virtualEnvManager = new PythonVirtualEnvManager(beepService, pythonRuntime);
var sessionManager = new PythonSessionManager(beepService, pythonRuntime);

// Create package manager
var packageManager = new PythonPackageManager(
    beepService, 
    pythonRuntime, 
    virtualEnvManager, 
    sessionManager);

// Configure session for a user
bool configured = packageManager.ConfigureSessionForUser("john.doe", "data-science-env");
if (!configured)
{
    throw new InvalidOperationException("Failed to configure session");
}
```

### Install Packages

```csharp
// Install packages with async support
using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));

// Install essential data science packages
bool pandasInstalled = await packageManager.InstallNewPackageWithSessionAsync("pandas", cts.Token);
bool numpyInstalled = await packageManager.InstallNewPackageWithSessionAsync("numpy", cts.Token);
bool sklearnInstalled = await packageManager.InstallNewPackageWithSessionAsync("scikit-learn", cts.Token);

// Auto-categorize packages
packageManager.PopulateCommonPackageCategories();

// Get packages by category
var dataPackages = packageManager.GetPackagesByCategory(PackageCategory.DataScience);
var mlPackages = packageManager.GetPackagesByCategory(PackageCategory.MachineLearning);
```

### Requirements File Management

```csharp
// Install from requirements file
bool installed = await packageManager.InstallFromRequirementsFileWithSessionAsync(
    "requirements.txt", 
    cts.Token);

// Generate requirements file from current environment
bool generated = packageManager.GenerateRequirementsFile(
    "generated-requirements.txt", 
    includeVersions: true);
```

## ??? Architecture Components

### PythonPackageManager
The main package management class with consolidated functionality:
- Session management and configuration
- Package installation, update, and removal
- Package information retrieval
- Virtual environment integration
- Progress reporting and error handling

### PackageCategoryManager
Organizes packages into logical categories:
- Machine Learning
- Data Science
- Web Development
- Database
- Graphics and Visualization
- And many more...

### RequirementsFileManager
Enhanced requirements file operations:
- Parsing and validation
- Generation from environments
- Installation with session support
- Dependency management

### PackageSetManager
Manages predefined package collections:
- Common package sets for specific use cases
- Custom set creation from environments
- Bulk installation operations

## ?? Package Categories

The system automatically categorizes packages into logical groups:

- **MachineLearning**: TensorFlow, PyTorch, Scikit-learn, XGBoost
- **DataScience**: Pandas, NumPy, SciPy, Matplotlib, Jupyter
- **WebDevelopment**: Django, Flask, FastAPI, Requests
- **Database**: SQLAlchemy, PyMongo, PostgreSQL adapters
- **Graphics**: Pillow, OpenCV, Matplotlib, Plotly
- **UserInterface**: PyQt, Kivy, Tkinter, Streamlit
- **Testing**: Pytest, Unittest, Coverage tools
- **Utilities**: Click, Rich, Loguru, Python-dotenv
- **Security**: Cryptography, PyJWT, Authlib
- **VectorDB**: Pinecone, Weaviate, ChromaDB, Qdrant
- **Embedding**: Sentence-transformers, OpenAI, Cohere
- **Scientific**: SciPy, SymPy, AstroPy, BioPython

## ?? API Reference

### Session Configuration

```csharp
// Configure with user and environment
bool configured = packageManager.ConfigureSessionForUser("username", "env-id");

// Configure with existing session objects
bool configured = packageManager.ConfigureSession(session, environment);

// Check configuration status
bool isConfigured = packageManager.IsSessionConfigured();
var currentSession = packageManager.GetConfiguredSession();
var currentEnvironment = packageManager.GetConfiguredVirtualEnvironment();
```

### Package Operations

```csharp
// Asynchronous operations (recommended)
await packageManager.InstallNewPackageWithSessionAsync("package-name", cancellationToken);
await packageManager.RefreshAllPackagesWithSessionAsync(cancellationToken);
await packageManager.UnInstallPackageWithSessionAsync("package-name", cancellationToken);

// Synchronous operations (legacy support)
bool installed = packageManager.InstallNewPackageAsync("package-name");
bool refreshed = packageManager.RefreshAllPackagesAsync();
bool uninstalled = packageManager.UnInstallPackageAsync("package-name");
```

### Category Management

```csharp
// Auto-categorize packages
bool categorized = packageManager.PopulateCommonPackageCategories();

// Get packages by category
var mlPackages = packageManager.GetPackagesByCategory(PackageCategory.MachineLearning);

// Manual category assignment
packageManager.SetPackageCategory("custom-package", PackageCategory.Utilities);

// Bulk category updates
var categoryUpdates = new Dictionary<string, PackageCategory>
{
    ["tensorflow"] = PackageCategory.MachineLearning,
    ["pandas"] = PackageCategory.DataScience
};
packageManager.UpdatePackageCategories(categoryUpdates);
```

## ??? Error Handling

```csharp
try
{
    // Check session configuration
    if (!packageManager.IsSessionConfigured())
    {
        throw new InvalidOperationException("Session must be configured");
    }

    // Check if busy
    if (packageManager.IsBusy)
    {
        Console.WriteLine("Package manager is busy, waiting...");
        await Task.Delay(1000);
    }

    // Perform operations with timeout
    using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
    bool success = await packageManager.InstallNewPackageWithSessionAsync("package", cts.Token);
    
    if (!success)
    {
        throw new InvalidOperationException("Installation failed");
    }
}
catch (OperationCanceledException)
{
    Console.WriteLine("Operation was cancelled due to timeout");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
```

## ?? Best Practices

1. **Always configure sessions** before performing package operations
2. **Use async methods** with cancellation tokens for better responsiveness
3. **Implement progress reporting** for long-running operations
4. **Handle exceptions** appropriately and check operation results
5. **Use package categories** for better organization
6. **Leverage package sets** for common scenarios
7. **Dispose of resources** properly when done

## ?? Dependencies

- **Beep.Python.Model**: Core data models and interfaces
- **Beep.Python.Runtime.PythonNet**: Python runtime management
- **TheTechIdea.Beep.DataManagementEngine**: Base framework
- **PythonNet**: Python.NET integration
- **Newtonsoft.Json**: JSON serialization

## ?? Documentation

- [Getting Started Guide](docs/getting-started.html)
- [API Reference](docs/api-reference.html)
- [Architecture Overview](docs/index.html)

## ?? Requirements

- **.NET 6, 7, 8, or 9**
- **Python 3.8 or higher**
- **Windows, macOS, or Linux**

## ?? Contributing

Contributions are welcome! Please read our contributing guidelines and submit pull requests to help improve the library.

## ?? License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## ????? Support

For questions, issues, or feature requests:
- Create an issue on GitHub
- Contact The Tech Idea team
- Check the documentation for detailed examples

---

**Beep.Python.PackageManagement** - Enterprise Python package management for .NET applications.

*© 2024 The Tech Idea - Supporting .NET 6, 7, 8, and 9*