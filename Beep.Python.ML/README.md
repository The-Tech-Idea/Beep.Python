# 🤖 Beep.Python.ML - Enterprise Machine Learning Integration

[![Documentation](https://img.shields.io/badge/docs-available-blue)](docs/index.html)

> **World-Class Machine Learning Integration** - Complete template-based architecture with 101+ Python scripts, 13 specialized assistant classes, and enterprise-grade capabilities for .NET 6, 7, 8, and 9.


The **Beep.Python.ML** framework has achieved **100% template-based architecture** with zero inline Python code, comprehensive assistant classes, and enterprise-grade capabilities across .NET 6, 7, 8, and 9.

### 🏆 **Achievement Status**

| Component | Status | Quality | Completeness |  
|-----------|--------|---------|--------------|
| **Core Interface** | ✅ Clean | **Excellent** | **100%** |
| **Python Scripts** | ✅ 101+ Created | **Excellent** | **100%** |  
| **Assistant Classes** | ✅ 13 Classes | **Excellent** | **100%** |
| **Template Manager** | ✅ Working | **Excellent** | **100%** |
| **Session Management** | ✅ Complete | **Excellent** | **100%** |
| **Multi-Framework** | ✅ .NET 6-9 | **Excellent** | **100%** |
| **Build Status** | ✅ Success | **Excellent** | **100%** |

---

## 🚀 **Key Features**


<PackageReference Include="Beep.Python.ML" Version="1.0.0" />
```

> **Multi-Framework Support**: Beep.Python.ML supports **.NET 6, 7, 8, and 9** with the same API across all versions.

---

## 🏗️ **Clean Architecture Overview**

The framework follows a clean, modular architecture with clear separation of concerns:

```
┌─────────────────────────────────────────────────────────────────────┐
│                     🎯 Core ML Manager                              │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │           PythonMLManager (Interface)                       │   │
│  │  • Core ML operations                                       │   │
│  │  • Session management                                       │   │  
│  │  • Data loading & validation                               │   │
│  │  • Model training & evaluation                             │   │
│  │  • Async operations support                                │   │
│  └─────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────┘
┌─────────────────────────────────────────────────────────────────────┐
│                🛠️ 13 Specialized Assistant Classes                  │
│  ┌─────────────────┬─────────────────┬─────────────────────────────┐ │
│  │ DataPreprocessing│ FeatureEngineer.│ CategoricalEncoding        │ │
│  │  • Template-based approach                                  │   │
│  │  • Parameter substitution                                   │   │
│  │  • Cached for performance                                   │   │
│  │  • Easy to maintain & version control                       │   │
│  └─────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────┘
```

---

## 🚀 **Quick Start**

### Basic ML Workflow with Assistant Classes

```csharp
using Beep.Python.ML;
using Beep.Python.Model;
using TheTechIdea.Beep.Container.Services;

// Initialize the ML manager with session
var beepService = new BeepService();
var pythonRuntime = new PythonNetRunTimeManager(beepService);
var sessionInfo = new PythonSessionInfo { Username = "user1" };

var mlManager = new PythonMLManager(beepService, pythonRuntime, sessionInfo);

// Configure session for user
    // Categorical encoding
    var categoricalFeatures = new[] { "category", "type" };
    manager.CategoricalEncoding.OneHotEncode(categoricalFeatures);
    
    // Feature engineering
    manager.FeatureEngineering.GeneratePolynomialFeatures(degree: 2);
    
    // Data cleaning
    manager.DataCleaning.RemoveOutliers(zThreshold: 3.0);
}

// Train model
var modelId = Guid.NewGuid().ToString();
var parameters = new Dictionary<string, object>
{
                    parameters, selectedFeatures, "target");

// Evaluate model
var scores = mlManager.GetModelClassificationScore(modelId);
Console.WriteLine($"Accuracy: {scores.Item1:F4}, F1-Score: {scores.Item2:F4}");

// Make predictions
var predictions = mlManager.PredictClassification(selectedFeatures);
Console.WriteLine($"Predictions: {string.Join(", ", predictions)}");
// Advanced workflow using multiple assistant classes
public async Task RunAdvancedMLWorkflow()
{
    var mlManager = new PythonMLManager(beepService, pythonRuntime, sessionInfo);
    mlManager.ConfigureMLSessionForUser("advanced_user");

    // Load and prepare data
    var features = await mlManager.LoadDataAsync("customer_data.csv");
    
    // Advanced preprocessing pipeline
    if (mlManager is PythonMLManager manager)
    {
        manager.FeatureSelection.ApplyVarianceThreshold(threshold: 0.01);
        manager.FeatureSelection.ApplyCorrelationThreshold(threshold: 0.95);
        
        // Feature engineering
        
        // Handle imbalanced data
        manager.ImbalancedData.ApplySMOTE("churn", samplingStrategy: 1.0f);
        manager.Visualization.CreateROC(modelId, "roc_curve.png");
        manager.Visualization.CreateFeatureImportance(modelId, "feature_importance.png");
        
        // Data splitting and export
        var splitPaths = manager.Utilities.SplitData("processed_data.csv", 0.2f, 
                                                   "train.csv", "test.csv", "validation.csv", 
                                                   "customer_id", "churn");

## 🛠️ **13 Specialized Assistant Classes**

The framework includes 13 specialized assistant classes, each handling a specific domain of machine learning operations:


|-----------------|--------|----------------|
| **🗄️ DataPreprocessing** | Data Cleaning | Scaling, normalization, missing values |
| **📅 DateTimeProcessing** | Temporal Data | Date extraction, cyclical features |
| **⚖️ ImbalancedData** | Class Balance | SMOTE, under/oversampling |
| **📈 TimeSeries** | Time Series | Forecasting, augmentation |
| **🧹 DataCleaning** | Data Quality | Outliers, duplicates, standardization |
| **📉 DimensionalityReduction** | Feature Space | PCA, LDA |
| **📊 Visualization** | Charts & Plots | ROC, confusion matrix, importance |
| **🔧 Utilities** | Support Operations | Data splitting, export, utilities |

```csharp
// Cast to implementation to access assistants
    manager.DataPreprocessing.StandardizeData();
    
    // Feature engineering
    manager.FeatureEngineering.GeneratePolynomialFeatures(degree: 2);
    manager.FeatureEngineering.ApplyLogTransformation();
    
    // Advanced analysis
    manager.CrossValidation.PerformCrossValidation("model1", 5);
    manager.Visualization.CreateROC("model1", "roc_curve.png");
}
```

---

## 📄 **Template-Based Python Scripts**

The framework includes **101+ Python scripts** covering all major ML operations:

### Specialized Operations
- `time_series_augmentation.py`, `model_benchmark.py`, `comprehensive_evaluation.py`
- `create_roc.py`, `create_confusion_matrix.py`, `create_feature_importance.py`

### Template System Benefits

- **🎯 Zero Inline Python**: All Python code is in separate, maintainable files
- **🔄 Parameter Substitution**: Dynamic parameter replacement using `{parameter_name}` syntax
- **📦 Version Control Friendly**: Python scripts can be versioned independently  
- **👥 Team Collaboration**: Python developers can work on scripts independently
- **⚡ Performance**: Scripts are cached for optimal execution speed
- **🧪 Testable**: Each script can be tested independently

---

## 🎭 **MVVM Architecture Support**


### PythonMachineLearningViewModel
```csharp
public class PythonMachineLearningViewModel : PythonBaseViewModel
{
    // Observable Properties
    public ObservableCollection<MLAlgorithmInfo> AvailableAlgorithms { get; set; }
    public MLAlgorithmInfo SelectedAlgorithm { get; set; }
    public ObservableCollection<ModelInfo> TrainedModels { get; set; }
    
    public async Task TrainModelAsync()
    {
        var progressReporter = new Progress<TrainingProgress>(progress =>
        
        await MLManager.TrainModelAsync(SelectedAlgorithm, progressReporter);
    }
}
    public ObservableCollection<TuningResult> TuningResults { get; set; }
    
    // Real-time monitoring
    public void OnTrainingEpochCompleted(TrainingEpoch epoch)
    {
        TrainingHistory.Add(epoch);
        UpdateLearningCurves(epoch);
    }
}
```

---

var autoMLConfig = new AutoMLConfiguration
{
    TaskType = MLTaskType.Classification,
    MetricToOptimize = "f1_score",
    TimeLimit = TimeSpan.FromHours(2),
    MaxTrials = 100
};

var autoMLResult = await mlManager.RunAutoMLAsync(autoMLConfig);
```

### Ensemble Methods
```csharp
// Advanced ensemble techniques
var votingClassifier = mlManager.CreateEnsemble("VotingClassifier", new[]
{
    ("RandomForest", mlManager.GetAlgorithm("RandomForestClassifier")),
    ("GradientBoosting", mlManager.GetAlgorithm("GradientBoostingClassifier")),
    ("SVC", mlManager.GetAlgorithm("SupportVectorClassifier"))
}, votingType: "soft");
```

### Model Interpretability
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

# Beep.Python.ML - Machine Learning Library

## ?? Overview

**Beep.Python.ML** is a comprehensive machine learning library for .NET that provides seamless integration with Python-based ML frameworks. The library has been completely refactored to follow clean architecture principles with **separate Python script files** and **no embedded Python code in C#**.

## ??? Architecture

### Key Design Principles

1. **Separation of Concerns**: Python scripts are stored in separate `.py` files
2. **Template-Based Approach**: Python scripts use parameter placeholders for dynamic values
3. **Clean C# Code**: No embedded Python strings in C# source code
4. **Maintainable Scripts**: Python files can be edited independently
5. **Version Control Friendly**: Python scripts are tracked separately

### Directory Structure

```
Beep.Python.ML/
??? Utils/
?   ??? PythonScriptTemplateManager.cs    # File loading & parameter substitution
??? Scripts/                               # Python script files
?   ??? cross_validation.py               # Cross-validation logic
?   ??? grid_search.py                     # Grid search optimization
?   ??? random_search.py                  # Random search optimization
?   ??? model_comparison.py               # Algorithm comparison
?   ??? comprehensive_evaluation.py       # Model evaluation
?   ??? training_initialization.py        # Environment setup
??? PythonTrainingViewModel.cs             # Main training view model
??? PythonMLManager.cs                     # ML operations manager
??? MLDataStructures.cs                    # Data structures and enums
??? docs/                                  # Documentation
```

## ?? How It Works

### 1. Python Script Template Manager

The `PythonScriptTemplateManager` utility class handles:

- **File Loading**: Loads Python scripts from the `Scripts/` directory
- **Parameter Substitution**: Replaces `{parameter_name}` placeholders with actual values
- **Caching**: Caches loaded scripts for performance
- **Type Formatting**: Converts C# types to Python-compatible formats

```csharp
// Example usage
var parameters = new Dictionary<string, object>
{
    ["algorithm_module"] = "ensemble",
    ["algorithm_name"] = "RandomForestClassifier", 
    ["label_column"] = "target",
    ["cv_folds"] = 5
};

var shapValues = await shapExplainer.ExplainInstanceAsync(testInstance);

var limeExplainer = await mlManager.CreateLIMEExplainerAsync(modelName);
var limeExplanation = await limeExplainer.ExplainInstanceAsync(testInstance);
```

---

## 📊 **Comprehensive Examples**

### Customer Churn Prediction
```csharp
public async Task CustomerChurnPrediction()
{
    var mlManager = new PythonMLManager(beepService);
    await mlManager.InitializeAsync();

    // Load customer data
    await mlManager.LoadDataAsync("customer_churn.csv");
    
    // Data preparation
    await mlManager.PrepareDataAsync(new DataPreparationOptions
    {
        TargetColumn = "Churn",
        RandomState = 42,
        HandleMissingValues = true,
        ScaleFeatures = true
    });

    // Feature engineering
    await mlManager.EncodeCategoricalFeaturesAsync(EncodingMethod.OneHot);
    
    // Train multiple models
        mlManager.GetAlgorithm("XGBClassifier"),
        mlManager.GetAlgorithm("LogisticRegression")
    };

    var results = await mlManager.TrainMultipleModelsAsync(algorithms);
    var bestModel = results.OrderByDescending(r => r.ValidationScore).First();

    // Hyperparameter tuning
    var paramGrid = new Dictionary<string, object[]>
        ["max_depth"] = new object[] { 5, 10, 15 },
        ["learning_rate"] = new object[] { 0.1, 0.2, 0.3 }
    };

    var tuningResult = await mlManager.GridSearchAsync(bestModel.Algorithm, paramGrid);
    
    var evaluation = await mlManager.EvaluateModelAsync(tuningResult.BestModelName);
    
    Console.WriteLine($"Final Model Performance:");
    Console.WriteLine($"Accuracy: {evaluation.Accuracy:F4}");
    Console.WriteLine($"Precision: {evaluation.Precision:F4}");
    Console.WriteLine($"Recall: {evaluation.Recall:F4}");
    Console.WriteLine($"F1-Score: {evaluation.F1Score:F4}");
    Console.WriteLine($"AUC: {evaluation.AUC:F4}");
}
```

---

## ✨ **Best Practices**

### Architecture Best Practices
- **Use Assistant Classes**: Leverage specialized assistants for domain-specific operations
- **Session Management**: Always configure sessions for multi-user environments
- **Template-Based**: All Python operations use template scripts for maintainability
2. **Create** a feature branch
3. **Implement** your enhancement with tests  
4. **Update** documentation as needed
5. **Submit** a pull request

### Development Setup
```bash
git clone https://github.com/The-Tech-Idea/Beep.Python.git
cd Beep.Python/Beep.Python.ML
dotnet restore
dotnet build
```

---

## 📜 **License**

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

- 📧 **Email**: support@thetechidea.net  
- 🌐 **Website**: [The Tech Idea](https://thetechidea.net)
- 📚 **Documentation**: [Complete Docs](docs/index.html)
- 🐛 **Issues**: [GitHub Issues](https://github.com/The-Tech-Idea/Beep.Python/issues)
---

## 🙏 **Acknowledgments**

- **Python.NET**: For seamless Python-.NET integration
- **scikit-learn**: For comprehensive ML algorithms  
- **Pandas**: For powerful data manipulation capabilities
- **NumPy**: For efficient numerical computing
- **The .NET Community**: For continuous support and feedback  

---

<div align="center">

**🎉 Beep.Python.ML - World-Class Enterprise Machine Learning Integration for .NET**

We welcome contributions to enhance the ML capabilities:

1. **Fork** the repository
    cancellationToken);

if (result.Success)
{
    Console.WriteLine($"Training completed in {result.Duration.TotalMinutes:F2} minutes");
    Console.WriteLine($"Best CV Score: {result.OptimizationResult.BestScore:F4}");
}
```

### Cross-Validation

```csharp
// Perform 10-fold cross-validation
var cvResult = await trainingVM.PerformCrossValidationAsync(
    cvFolds: 10,
    scoringMetric: "f1_weighted");

if (cvResult.Success)
{
    Console.WriteLine($"CV Mean: {cvResult.MeanScore:F4} (±{cvResult.StdScore:F4})");
}
```

### Hyperparameter Optimization

```csharp
// Define parameter grid
var paramGrid = new Dictionary<string, object[]>
{

*Built with ❤️ by [The Tech Idea](https://thetechidea.net)*

[![Build Status](https://img.shields.io/badge/build-passing-brightgreen)](https://github.com/The-Tech-Idea/Beep.Python)
[![Documentation](https://img.shields.io/badge/docs-comprehensive-blue)](docs/index.html)

</div>    paramGrid, 
    SearchType.GridSearch, 
    cvFolds: 5);

if (optResult.Success)
{
    Console.WriteLine($"Best Score: {optResult.BestScore:F4}");
    Console.WriteLine("Best Parameters:");
    foreach (var param in optResult.BestParams)
    {
        Console.WriteLine($"  {param.Key}: {param.Value}");
    }
}
```

### Algorithm Comparison

```csharp
// Compare multiple algorithms
var algorithms = new[]
{
    MachineLearningAlgorithm.RandomForestClassifier,
    MachineLearningAlgorithm.LogisticRegression,
    MachineLearningAlgorithm.SVC
};

var comparison = await trainingVM.CompareAlgorithmsAsync(algorithms, cvFolds: 5);

if (comparison.Success)
{
    Console.WriteLine($"Best Algorithm: {comparison.BestAlgorithm}");
    foreach (var result in comparison.AlgorithmResults)
    {
        Console.WriteLine($"{result.Algorithm}: {result.Metrics.CVMean:F4} (±{result.Metrics.CVStd:F4})");
    }
}
```

## ??? Customization

### Creating Custom Python Scripts

1. Create a new `.py` file in the `Scripts/` directory
2. Use `{parameter_name}` placeholders for dynamic values
3. Follow the result format conventions (return dictionaries with `success` field)

```python
# custom_analysis.py
import pandas as pd
import numpy as np

try:
    # Custom analysis logic
    result = perform_custom_analysis(data, '{analysis_type}', {parameters})
    
    custom_result = {
        'success': True,
        'analysis_type': '{analysis_type}',
        'results': result
    }
    
except Exception as e:
    custom_result = {
        'success': False,
        'error': str(e)
    }
```

### Using Custom Scripts in C#

```csharp
var parameters = new Dictionary<string, object>
{
    ["analysis_type"] = "correlation",
    ["parameters"] = new Dictionary<string, object>
    {
        ["method"] = "pearson",
        ["threshold"] = 0.5
    }
};

string script = PythonScriptTemplateManager.GetScript("custom_analysis", parameters);
bool success = await ExecuteInSessionAsync(script, cancellationToken);

var result = GetFromSessionScope<Dictionary<string, object>>("custom_result");
```

## ?? Benefits of This Architecture

### ? Advantages

1. **Clean Separation**: No Python code mixed with C#
2. **Maintainable**: Python scripts can be edited independently
3. **Version Control**: Clear tracking of script changes
4. **IDE Support**: Full Python syntax highlighting and IntelliSense
5. **Testing**: Python scripts can be tested independently
6. **Collaboration**: Python developers can work on scripts directly
7. **Reusability**: Scripts can be shared across projects
8. **Performance**: Script caching reduces file I/O

### ?? Use Cases

- **Machine Learning Pipelines**: Automated training workflows
- **Data Science**: Exploratory data analysis and modeling
- **Model Deployment**: Production ML model serving
- **Research**: Experimental algorithm development
- **Education**: Teaching ML concepts with practical examples

## ?? Error Handling

The library provides comprehensive error handling:

```csharp
try
{
    string script = PythonScriptTemplateManager.GetScript("non_existent_script");
}
catch (FileNotFoundException ex)
{
    Console.WriteLine($"Script not found: {ex.Message}");
}

// Check if script exists before loading
if (PythonScriptTemplateManager.ScriptExists("custom_script"))
{
    string script = PythonScriptTemplateManager.GetScript("custom_script", parameters);
}
```

## ?? Performance Considerations

- **Script Caching**: Loaded scripts are cached in memory
- **Lazy Loading**: Scripts are only loaded when requested
- **Parameter Formatting**: Efficient type conversion
- **Async Operations**: Non-blocking execution

## ?? Development Workflow

1. **Design**: Plan your ML workflow and required scripts
2. **Create Scripts**: Write Python scripts with parameter placeholders
3. **Implement C#**: Use PythonScriptTemplateManager in your C# code
4. **Test**: Test individual scripts and integration
5. **Deploy**: Package scripts with your application

This architecture provides a clean, maintainable, and scalable approach to integrating Python ML capabilities into .NET applications while keeping concerns properly separated.