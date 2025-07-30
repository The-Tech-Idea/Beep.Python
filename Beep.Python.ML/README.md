# Beep.Python.ML - Enhanced Machine Learning Integration

![License](https://img.shields.io/badge/license-MIT-blue.svg)
![.NET](https://img.shields.io/badge/.NET-6%7C7%7C8%7C9-purple.svg)
![Version](https://img.shields.io/badge/version-1.0.44-green.svg)

Enhanced Python machine learning integration for .NET applications with session management, virtual environment support, and comprehensive ML operations.

## ?? What's New in v1.0.44

The Beep.Python.ML project has been significantly refactored and optimized to provide better integration with the enhanced Python runtime system:

- **Enhanced Session Management**: Full support for multi-user session isolation and management
- **Virtual Environment Integration**: Seamless integration with Python virtual environments
- **Async Operations**: Comprehensive async/await support for better performance
- **Improved Architecture**: Streamlined dependencies and better separation of concerns
- **Interface Enhancement**: Extended IPythonMLManager with session management capabilities

## ?? Key Features

### Session Management
- **Multi-User Support**: Isolated sessions for concurrent users
- **Session Configuration**: Easy session setup with username-based configuration
- **Virtual Environment Binding**: Automatic association with appropriate Python environments
- **Session State Management**: Proper session lifecycle management

### Enhanced ML Operations
- **Data Loading & Validation**: Session-aware data loading with validation
- **Model Training**: Asynchronous model training with progress reporting
- **Feature Engineering**: Comprehensive feature preprocessing capabilities
- **Model Evaluation**: Advanced model evaluation and scoring
- **Visualization**: Rich visualization and reporting capabilities

### Async Operations
- **Non-Blocking Operations**: All ML operations support async execution
- **Cancellation Support**: Proper cancellation token implementation
- **Progress Reporting**: Real-time progress updates for long-running operations
- **Error Handling**: Comprehensive error handling and recovery

## ?? Installation

### NuGet Package Manager
```powershell
Install-Package Beep.Python.ML -Version 1.0.44
```

### .NET CLI
```bash
dotnet add package Beep.Python.ML --version 1.0.44
```

### Package Reference
```xml
<PackageReference Include="Beep.Python.ML" Version="1.0.44" />
```

## ?? Quick Start

### Basic Setup with Session Management

```csharp
using Beep.Python.ML;
using Beep.Python.Model;
using TheTechIdea.Beep.Container.Services;

// Initialize services
var beepService = new BeepService();
var pythonRuntime = new PythonNetRunTimeManager(beepService);
var virtualEnvManager = new PythonVirtualEnvManager(beepService, pythonRuntime);
var sessionManager = new PythonSessionManager(beepService, pythonRuntime);

// Create session info
var sessionInfo = new PythonSessionInfo
{
    Username = "data-scientist",
    SessionName = "ML-Session"
};

// Create ML manager with session support
var mlManager = new PythonMLManager(
    beepService, 
    pythonRuntime, 
    virtualEnvManager, 
    sessionManager, 
    sessionInfo);

// Configure session for user
bool configured = mlManager.ConfigureMLSessionForUser("john.doe", "ml-environment");
if (!configured)
{
    throw new InvalidOperationException("Failed to configure ML session");
}
```

### Data Loading and Preprocessing

```csharp
// Load and validate data
string[] features = await mlManager.LoadDataAsync("data/training.csv");

// Preview data for validation
string[] previewColumns = mlManager.ValidateAndPreviewData("data/training.csv", 10);

// Apply data preprocessing
mlManager.StandardizeData(features);
mlManager.OneHotEncode(new[] { "category_column" });
mlManager.ImputeMissingValues(strategy: "mean");
```

### Model Training and Evaluation

```csharp
// Configure model parameters
var parameters = new Dictionary<string, object>
{
    { "n_estimators", 100 },
    { "max_depth", 10 },
    { "random_state", 42 }
};

// Train model asynchronously
bool success = await mlManager.TrainModelAsync(
    "my-model", 
    MachineLearningAlgorithm.RandomForestClassifier,
    parameters,
    features,
    "target_column");

if (success)
{
    // Evaluate model performance
    var scores = await mlManager.GetModelClassificationScoreAsync("my-model");
    Console.WriteLine($"Accuracy: {scores.Item1}, F1-Score: {scores.Item2}");
    
    // Generate evaluation report
    mlManager.GenerateEvaluationReport("my-model", "reports/model_evaluation.html");
}
```

### Advanced Features

```csharp
// Feature engineering
mlManager.GeneratePolynomialFeatures(features, degree: 2);
mlManager.ApplyPCA(nComponents: 10, featureList: features);

// Handle imbalanced data
mlManager.ApplySMOTE("target_column", samplingStrategy: 1.0f);

// Text processing (if applicable)
mlManager.ApplyTFIDFVectorization("text_column", maxFeatures: 1000);
mlManager.RemoveStopwords("text_column");

// Cross-validation
mlManager.PerformCrossValidation("my-model", numFolds: 5);
```

## ??? Architecture

### Enhanced Components

1. **PythonMLManager**: Core ML operations with session management
2. **PythonBaseViewModel**: Enhanced base class with session support
3. **IPythonMLManager**: Extended interface with async operations
4. **Session Integration**: Seamless integration with Python runtime sessions

### Key Improvements

- **Session Isolation**: Each user gets isolated Python sessions
- **Resource Management**: Proper cleanup and disposal patterns
- **Error Handling**: Comprehensive error handling and recovery
- **Performance**: Async operations for better responsiveness
- **Scalability**: Support for concurrent users and operations

## ?? Migration Guide

### From Previous Versions

The enhanced version maintains backward compatibility while adding new session management capabilities:

```csharp
// Old approach (still supported)
var mlManager = new PythonMLManager(beepService, pythonRuntime, sessionInfo);

// New enhanced approach (recommended)
var mlManager = new PythonMLManager(
    beepService, 
    pythonRuntime, 
    virtualEnvManager, 
    sessionManager, 
    sessionInfo);

// Configure session (new requirement)
mlManager.ConfigureMLSessionForUser("username");
```

## ?? Testing

The project includes comprehensive testing support:

```csharp
// Unit testing with session management
[Test]
public async Task TestMLOperationsWithSession()
{
    var mlManager = CreateTestMLManager();
    mlManager.ConfigureMLSessionForUser("test-user");
    
    var features = await mlManager.LoadDataAsync("test-data.csv");
    Assert.IsNotNull(features);
    Assert.IsTrue(features.Length > 0);
}
```

## ?? Requirements

- **.NET 6.0+**: Multi-targeting support for .NET 6, 7, 8, and 9
- **Python 3.8+**: Compatible Python installation
- **Required Packages**: pandas, numpy, scikit-learn, matplotlib
- **Memory**: Minimum 4GB RAM for ML operations
- **Storage**: Adequate space for datasets and models

## ?? Contributing

We welcome contributions to enhance the ML capabilities:

1. **Fork** the repository
2. **Create** a feature branch
3. **Implement** your enhancement with tests
4. **Submit** a pull request

## ?? Documentation

- [API Reference](./docs/api-reference.md)
- [Getting Started Guide](./docs/getting-started.md)
- [Session Management](./docs/session-management.md)
- [Advanced Features](./docs/advanced-features.md)

## ??? License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## ?? Support

For support and questions:
- ?? Email: support@thetechidea.net
- ?? Website: [The Tech Idea](https://thetechidea.net)
- ?? Documentation: [Complete Documentation](https://docs.thetechidea.net)

---

**Beep.Python.ML** - Enhanced Machine Learning Integration for .NET
© 2024 The Tech Idea. All rights reserved.