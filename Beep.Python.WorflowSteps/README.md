# ?? Beep.Python.WorflowSteps Documentation

Welcome to the Beep.Python.WorflowSteps documentation! This library provides a comprehensive collection of predefined workflow steps for Python integration in .NET applications.

## ?? Table of Contents

- [Overview](#overview)
- [Features](#features)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Available Steps](#available-steps)
- [Custom Steps](#custom-steps)
- [Step Categories](#step-categories)
- [Advanced Configuration](#advanced-configuration)
- [Best Practices](#best-practices)

## ?? Overview

Beep.Python.WorflowSteps provides a rich library of predefined workflow steps that can be easily integrated into Python workflows. These steps cover common operations like data processing, machine learning, file operations, and system integrations.

### Key Features

- ? **50+ Predefined Steps:** Comprehensive library of ready-to-use workflow steps
- ?? **Composable Design:** Steps can be easily combined and chained together
- ?? **Data Processing:** Built-in steps for common data operations
- ?? **ML Integration:** Machine learning workflow steps
- ?? **Enterprise Ready:** Security, monitoring, and governance features
- ??? **Configurable:** Flexible configuration and parameterization
- ?? **Performance Optimized:** Efficient execution with resource management

## ?? Installation

### Package Manager Console
```powershell
Install-Package Beep.Python.WorflowSteps
```

### .NET CLI
```bash
dotnet add package Beep.Python.WorflowSteps
```

### Package Reference
```xml
<PackageReference Include="Beep.Python.WorflowSteps" Version="1.0.0" />
```

## ?? Quick Start

```csharp
using Beep.Python.WorflowSteps;
using Beep.Python.WorkFlows;

// Create workflow
var workflow = new PythonWorkflow("Data Analysis Pipeline");

// Add predefined steps
workflow.AddStep(new LoadCsvStep
{
    Name = "Load Data",
    FilePath = "data/sales_data.csv",
    OutputVariable = "sales_df"
});

workflow.AddStep(new DataCleaningStep
{
    Name = "Clean Data",
    InputVariable = "sales_df",
    Operations = new[]
    {
        CleaningOperation.RemoveNulls,
        CleaningOperation.RemoveDuplicates,
        CleaningOperation.StandardizeText
    },
    OutputVariable = "clean_df"
});

workflow.AddStep(new StatisticalAnalysisStep
{
    Name = "Analyze Data",
    InputVariable = "clean_df",
    Operations = new[]
    {
        AnalysisOperation.DescriptiveStats,
        AnalysisOperation.CorrelationMatrix,
        AnalysisOperation.OutlierDetection
    },
    OutputVariable = "analysis_results"
});

workflow.AddStep(new SaveResultsStep
{
    Name = "Save Results",
    InputVariable = "analysis_results",
    OutputPath = "results/analysis_report.json"
});

// Execute workflow
var engine = new PythonWorkflowEngine(beepService);
var result = await engine.ExecuteAsync(workflow);
```

## ?? Available Steps

### Data Processing Steps

#### LoadCsvStep
```csharp
var loadStep = new LoadCsvStep
{
    Name = "Load Sales Data",
    FilePath = "data/sales.csv",
    Delimiter = ",",
    HeaderRow = true,
    DataTypes = new Dictionary<string, string>
    {
        ["date"] = "datetime",
        ["amount"] = "float64",
        ["category"] = "string"
    },
    OutputVariable = "sales_data"
};
```

#### DataFilterStep
```csharp
var filterStep = new DataFilterStep
{
    Name = "Filter Recent Sales",
    InputVariable = "sales_data",
    FilterConditions = new[]
    {
        "date >= '2023-01-01'",
        "amount > 0",
        "category.notnull()"
    },
    OutputVariable = "filtered_data"
};
```

#### DataAggregationStep
```csharp
var aggregateStep = new DataAggregationStep
{
    Name = "Monthly Sales Summary",
    InputVariable = "filtered_data",
    GroupByColumns = new[] { "category", "month" },
    AggregationRules = new Dictionary<string, string>
    {
        ["amount"] = "sum",
        ["transaction_id"] = "count",
        ["customer_id"] = "nunique"
    },
    OutputVariable = "monthly_summary"
};
```

### Machine Learning Steps

#### ModelTrainingStep
```csharp
var trainingStep = new ModelTrainingStep
{
    Name = "Train Prediction Model",
    InputVariable = "training_data",
    Algorithm = "RandomForestRegressor",
    TargetColumn = "target",
    FeatureColumns = new[] { "feature1", "feature2", "feature3" },
    HyperParameters = new Dictionary<string, object>
    {
        ["n_estimators"] = 100,
        ["max_depth"] = 10,
        ["random_state"] = 42
    },
    ValidationSplit = 0.2,
    OutputVariable = "trained_model"
};
```

#### ModelEvaluationStep
```csharp
var evaluationStep = new ModelEvaluationStep
{
    Name = "Evaluate Model",
    ModelVariable = "trained_model",
    TestDataVariable = "test_data",
    Metrics = new[]
    {
        ModelMetric.Accuracy,
        ModelMetric.Precision,
        ModelMetric.Recall,
        ModelMetric.F1Score,
        ModelMetric.AUC
    },
    OutputVariable = "evaluation_results"
};
```

#### ModelPredictionStep
```csharp
var predictionStep = new ModelPredictionStep
{
    Name = "Generate Predictions",
    ModelVariable = "trained_model",
    InputDataVariable = "new_data",
    PredictionType = PredictionType.Probability,
    OutputVariable = "predictions"
};
```

### File Operations Steps

#### FileDownloadStep
```csharp
var downloadStep = new FileDownloadStep
{
    Name = "Download Dataset",
    Url = "https://example.com/dataset.csv",
    LocalPath = "data/downloaded_dataset.csv",
    Authentication = new WebAuthentication
    {
        Type = AuthenticationType.ApiKey,
        ApiKey = "your-api-key"
    },
    RetryCount = 3,
    TimeoutSeconds = 30
};
```

#### FileCompressionStep
```csharp
var compressionStep = new FileCompressionStep
{
    Name = "Compress Results",
    InputFiles = new[] { "results/*.csv", "results/*.json" },
    OutputFile = "archive/results.zip",
    CompressionLevel = CompressionLevel.Maximum,
    DeleteOriginals = false
};
```

### Database Steps

#### DatabaseQueryStep
```csharp
var queryStep = new DatabaseQueryStep
{
    Name = "Load Customer Data",
    ConnectionString = "Server=localhost;Database=Sales;Trusted_Connection=true;",
    Query = @"
        SELECT customer_id, name, email, registration_date
        FROM customers 
        WHERE status = 'active' 
        AND registration_date >= @start_date",
    Parameters = new Dictionary<string, object>
    {
        ["start_date"] = DateTime.Now.AddDays(-30)
    },
    OutputVariable = "customer_data"
};
```

#### DatabaseInsertStep
```csharp
var insertStep = new DatabaseInsertStep
{
    Name = "Save Predictions",
    ConnectionString = connectionString,
    TableName = "predictions",
    InputVariable = "prediction_results",
    BatchSize = 1000,
    OnConflict = ConflictResolution.Update
};
```

### API Integration Steps

#### RestApiCallStep
```csharp
var apiStep = new RestApiCallStep
{
    Name = "Fetch Weather Data",
    Url = "https://api.weather.com/v1/current",
    Method = HttpMethod.Get,
    Headers = new Dictionary<string, string>
    {
        ["Authorization"] = "Bearer {api_token}",
        ["Content-Type"] = "application/json"
    },
    QueryParameters = new Dictionary<string, string>
    {
        ["city"] = "New York",
        ["units"] = "metric"
    },
    OutputVariable = "weather_data",
    RetryPolicy = new RetryPolicy
    {
        MaxRetries = 3,
        DelayBetweenRetries = TimeSpan.FromSeconds(5)
    }
};
```

### Visualization Steps

#### ChartGenerationStep
```csharp
var chartStep = new ChartGenerationStep
{
    Name = "Generate Sales Chart",
    InputVariable = "sales_summary",
    ChartType = ChartType.BarChart,
    XColumn = "month",
    YColumn = "total_sales",
    Title = "Monthly Sales Trends",
    OutputPath = "charts/monthly_sales.png",
    ChartOptions = new ChartOptions
    {
        Width = 1200,
        Height = 800,
        ShowLegend = true,
        ColorScheme = "viridis"
    }
};
```

## ?? Custom Steps

### Creating Custom Steps

```csharp
public class CustomDataProcessingStep : BaseWorkflowStep
{
    public string InputVariable { get; set; }
    public string OutputVariable { get; set; }
    public ProcessingOptions Options { get; set; }

    public override async Task<StepExecutionResult> ExecuteAsync(WorkflowExecutionContext context)
    {
        try
        {
            // Get input data
            var inputData = context.GetVariable(InputVariable);
            
            // Generate Python script
            var script = GenerateProcessingScript(inputData, Options);
            
            // Execute Python code
            var pythonEngine = context.GetPythonEngine();
            var result = await pythonEngine.ExecuteAsync(script);
            
            // Store result
            context.SetVariable(OutputVariable, result.Output);
            
            return StepExecutionResult.Success(result.Output);
        }
        catch (Exception ex)
        {
            return StepExecutionResult.Failed(ex.Message, ex);
        }
    }

    private string GenerateProcessingScript(object inputData, ProcessingOptions options)
    {
        return $@"
import pandas as pd
import numpy as np

# Load input data
data = {SerializeInput(inputData)}

# Apply custom processing
processed_data = data.copy()

# Custom transformations based on options
if {options.NormalizeValues}:
    processed_data = (processed_data - processed_data.mean()) / processed_data.std()

if {options.RemoveOutliers}:
    Q1 = processed_data.quantile(0.25)
    Q3 = processed_data.quantile(0.75)
    IQR = Q3 - Q1
    processed_data = processed_data[~((processed_data < (Q1 - 1.5 * IQR)) | (processed_data > (Q3 + 1.5 * IQR))).any(axis=1)]

result = processed_data.to_dict('records')
";
    }
}
```

### Step Registration

```csharp
// Register custom step
StepRegistry.RegisterStep<CustomDataProcessingStep>("CustomProcessing");

// Use in workflow
workflow.AddStep(new CustomDataProcessingStep
{
    Name = "Custom Data Processing",
    InputVariable = "raw_data",
    OutputVariable = "processed_data",
    Options = new ProcessingOptions
    {
        NormalizeValues = true,
        RemoveOutliers = true
    }
});
```

## ?? Step Categories

### Data Operations
- **LoadCsvStep** - Load data from CSV files
- **LoadExcelStep** - Load data from Excel files  
- **LoadJsonStep** - Load data from JSON files
- **SaveCsvStep** - Save data to CSV format
- **DataFilterStep** - Filter data based on conditions
- **DataSortStep** - Sort data by columns
- **DataGroupStep** - Group data and aggregate
- **DataJoinStep** - Join multiple datasets
- **DataValidationStep** - Validate data quality

### Machine Learning
- **ModelTrainingStep** - Train ML models
- **ModelEvaluationStep** - Evaluate model performance
- **ModelPredictionStep** - Generate predictions
- **FeatureEngineeringStep** - Create new features
- **ModelComparisonStep** - Compare multiple models
- **HyperparameterTuningStep** - Optimize model parameters
- **CrossValidationStep** - Perform cross-validation

### Statistical Analysis
- **DescriptiveStatsStep** - Calculate descriptive statistics
- **CorrelationAnalysisStep** - Compute correlations
- **OutlierDetectionStep** - Detect data outliers
- **TrendAnalysisStep** - Analyze trends over time
- **DistributionAnalysisStep** - Analyze data distributions

### File & I/O Operations
- **FileDownloadStep** - Download files from URLs
- **FileUploadStep** - Upload files to servers
- **FileCompressionStep** - Compress/decompress files
- **DirectoryOperationsStep** - Manage directories
- **FileConversionStep** - Convert between file formats

### Database Operations
- **DatabaseQueryStep** - Execute SQL queries
- **DatabaseInsertStep** - Insert data into tables
- **DatabaseUpdateStep** - Update existing records
- **DatabaseDeleteStep** - Delete records
- **StoredProcedureStep** - Execute stored procedures

### API Integration
- **RestApiCallStep** - Make REST API calls
- **GraphQLQueryStep** - Execute GraphQL queries
- **SoapServiceStep** - Call SOAP web services
- **WebhookStep** - Handle webhook requests

### Visualization
- **ChartGenerationStep** - Create charts and graphs
- **DashboardStep** - Generate dashboards
- **ReportGenerationStep** - Create PDF/HTML reports
- **ImageProcessingStep** - Process and manipulate images

## ?? Advanced Configuration

### Step Dependencies
```csharp
// Configure step dependencies
var step1 = new LoadCsvStep { Name = "Load Data" };
var step2 = new DataCleaningStep { Name = "Clean Data" };
var step3 = new ModelTrainingStep { Name = "Train Model" };

// Set dependencies
step2.Dependencies.Add(step1);
step3.Dependencies.Add(step2);

// Workflow engine will respect dependencies
workflow.AddSteps(step1, step2, step3);
```

### Conditional Execution
```csharp
var conditionalStep = new ConditionalStep
{
    Name = "Conditional Processing",
    Condition = "len(input_data) > 1000",
    TrueStep = new ModelTrainingStep { Name = "Train Model" },
    FalseStep = new SimpleAnalysisStep { Name = "Basic Analysis" }
};
```

### Parallel Execution
```csharp
var parallelStep = new ParallelExecutionStep
{
    Name = "Parallel Analysis",
    Steps = new[]
    {
        new StatisticalAnalysisStep { Name = "Stats Analysis" },
        new VisualizationStep { Name = "Generate Charts" },
        new ModelTrainingStep { Name = "Train Model" }
    },
    MaxConcurrency = 3,
    FailOnAnyFailure = false
};
```

### Error Handling
```csharp
var robustStep = new DataProcessingStep
{
    Name = "Robust Processing",
    ErrorHandling = new ErrorHandlingOptions
    {
        RetryCount = 3,
        RetryDelay = TimeSpan.FromSeconds(5),
        OnError = ErrorAction.Continue,
        FallbackStep = new BasicProcessingStep()
    }
};
```

## ? Best Practices

### Step Design
- **Single Responsibility:** Each step should have a single, well-defined purpose
- **Reusability:** Design steps to be reusable across different workflows
- **Configuration:** Make steps configurable through properties and options
- **Validation:** Validate inputs and configurations before execution
- **Error Handling:** Implement robust error handling and recovery

### Performance Optimization
- **Resource Management:** Properly manage memory and system resources
- **Caching:** Cache expensive operations where appropriate
- **Batch Processing:** Use batch processing for large datasets
- **Parallel Execution:** Leverage parallel execution for independent operations

### Security Considerations
- **Input Sanitization:** Sanitize all user inputs and parameters
- **Credential Management:** Securely handle credentials and sensitive data
- **Access Control:** Implement proper access control for step execution
- **Audit Logging:** Log step execution for security audits

### Testing
- **Unit Testing:** Create unit tests for each step
- **Integration Testing:** Test step integration within workflows
- **Mock Data:** Use mock data for testing scenarios
- **Performance Testing:** Test step performance with realistic data volumes

## ?? API Reference

### Base Classes
- `BaseWorkflowStep` - Base class for all workflow steps
- `DataProcessingStep` - Base for data processing operations
- `MLStep` - Base for machine learning operations
- `IOStep` - Base for input/output operations

### Configuration Classes
- `StepConfiguration` - Base step configuration
- `DataConfiguration` - Data-related configurations
- `ModelConfiguration` - ML model configurations
- `ConnectionConfiguration` - Database/API connection settings

### Result Classes
- `StepExecutionResult` - Step execution results
- `DataResult` - Data operation results
- `ModelResult` - ML operation results
- `ValidationResult` - Data validation results

## ?? Related Documentation

- [Beep.Python.WorkFlows](../Beep.Python.WorkFlows/) - Workflow orchestration engine
- [Beep.Python.Runtime](../Beep.Python.Runtime/) - Python runtime management
- [Beep.Python.ML](../Beep.Python.ML/) - Machine learning integration

## ?? Support

For support and questions:
- ?? Email: support@thetechidea.net
- ?? Website: [The Tech Idea](https://thetechidea.net)
- ?? Documentation: [Complete Documentation](https://docs.thetechidea.net)

---

**Beep.Python.WorflowSteps** - Predefined Workflow Steps for Python Integration
© 2024 The Tech Idea. All rights reserved.