# Beep.Python.ML - Refactoring Guide

## Overview
This document outlines the complete refactoring of Beep.Python.ML to eliminate inline Python code and use separate `.py` script files with parameter substitution.

## Architecture Changes

### Before (Inline Python):
```csharp
string script = $@"
import pandas as pd
data = pd.read_csv('{filePath}')
features = data.columns.tolist()
";
```

### After (Template-based):
```csharp
var parameters = new Dictionary<string, object>
{
    ["file_path"] = filePath
};
string script = PythonScriptTemplateManager.GetScript("load_data", parameters);
```

## Required Python Scripts

### Core Data Operations
1. **load_data.py** - Load CSV data and return features
2. **validate_and_preview_data.py** - Data validation and preview
3. **filter_selected_features.py** - Filter data to selected features
4. **load_data_with_features.py** - Load data with specific features

### Data Preprocessing Scripts
5. **handle_missing_values.py** - Various missing value strategies
6. **scale_data.py** - Data scaling (Standard, MinMax, Robust)
7. **encode_categorical.py** - Categorical encoding methods
8. **split_data.py** - Train/test/validation splits

### Machine Learning Scripts
9. **train_model.py** - Train ML models
10. **evaluate_model.py** - Model evaluation
11. **predict_model.py** - Make predictions
12. **save_load_model.py** - Model persistence

### Advanced ML Scripts (Already exist)
13. **cross_validation.py** - Cross-validation
14. **grid_search.py** - Grid search optimization
15. **random_search.py** - Random search optimization
16. **model_comparison.py** - Algorithm comparison
17. **comprehensive_evaluation.py** - Detailed evaluation

## Implementation Steps

### Step 1: Create All Python Script Files
Create 17+ Python script files in the `Scripts/` directory.

### Step 2: Update PythonMLManager.cs
Replace all inline Python with template manager calls:

```csharp
// Add using statement
using Beep.Python.ML.Utils;

// Replace inline Python methods
public string[] LoadData(string filePath)
{
    var parameters = new Dictionary<string, object>
    {
        ["file_path"] = filePath.Replace("\\", "\\\\")
    };
    
    string script = PythonScriptTemplateManager.GetScript("load_data", parameters);
    ExecuteInSession(script);
    return GetStringArrayFromSession("features");
}
```

### Step 3: Update PythonTrainingViewModel.cs
Ensure all ML operations use the template approach.

### Step 4: Testing and Validation
Test all functionality to ensure the refactoring is successful.

## Benefits

? **Clean Code**: No mixed Python/C# syntax
? **Maintainable**: Python scripts are separate files  
? **Version Control**: Clear tracking of script changes
? **IDE Support**: Full Python syntax highlighting
? **Testable**: Scripts can be tested independently
? **Reusable**: Scripts can be shared across projects

## Current Status

- ? Template Manager created
- ? Core ML scripts exist (cross_validation, grid_search, etc.)
- ?? Need to create data operation scripts
- ?? Need to update PythonMLManager methods
- ?? Need comprehensive testing

## Next Actions

1. Create all missing Python script files
2. Update all methods in PythonMLManager.cs
3. Test the refactored implementation
4. Update documentation