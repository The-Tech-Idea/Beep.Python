# Beep.Python.ML Refactoring Complete Guide

## Current State Analysis

The Beep.Python.ML project currently has **extensive inline Python code** in `PythonMLManager.cs` that needs to be moved to separate `.py` files. Here are the key methods that contain inline Python:

### Methods with Inline Python Code:

1. **ValidateAndPreviewData()** - Data validation and preview
2. **FilterDataToSelectedFeatures()** - Feature filtering  
3. **LoadData()** (2 overloads) - Data loading operations
4. **Training methods** - Model training operations
5. **Preprocessing methods** - Data preprocessing operations

## Solution Architecture

### Template Manager Approach
- ? **PythonScriptTemplateManager** exists and works correctly
- ? Uses `{parameter_name}` placeholders for dynamic values
- ? Handles proper Python type formatting
- ? Caches scripts for performance

### Required Python Scripts

#### Core Data Operations
```python
# load_data.py
import pandas as pd
data = pd.read_csv('{file_path}')
features = data.columns.tolist()
globals()['data'] = data

# validate_and_preview_data.py  
import pandas as pd
preview_data = pd.read_csv('{file_path}', nrows={num_rows})
preview_columns = preview_data.columns.tolist()
preview_missing_values = preview_data.isnull().sum().tolist()
preview_data_types = preview_data.dtypes.apply(lambda X: X.name).tolist()

# filter_selected_features.py
selected_features = {selected_features}
if 'train_data' in globals():
    train_data = train_data[selected_features]
if 'test_data' in globals():
    test_data = test_data[selected_features]  
if 'data' in globals():
    data = data[selected_features]

# load_data_with_features.py
import pandas as pd
data = pd.read_csv('{file_path}')
selected_features = {selected_features}
data = data[selected_features]
features = data.columns.tolist()
globals()['data'] = data
```

## Implementation Guide

### Step 1: Create Missing Python Scripts
Create all necessary `.py` files in `Beep.Python.ML/Scripts/` directory:

1. `load_data.py`
2. `validate_and_preview_data.py` ? (exists)
3. `filter_selected_features.py`
4. `load_data_with_features.py`
5. `split_data.py`
6. `preprocess_data.py`
7. `train_model.py`
8. `evaluate_model.py`
9. And more...

### Step 2: Update PythonMLManager.cs Methods

#### Before (Inline Python):
```csharp
string script = $@"
import pandas as pd
data = pd.read_csv('{modifiedFilePath}')
features = data.columns.tolist()
globals()['data'] = data
";
```

#### After (Template Manager):
```csharp
using Beep.Python.ML.Utils; // Add this using statement

var parameters = new Dictionary<string, object>
{
    ["file_path"] = filePath.Replace("\\", "\\\\")
};

string script = PythonScriptTemplateManager.GetScript("load_data", parameters);
```

### Step 3: Update All Methods
Replace inline Python in these methods:

```csharp
// Data loading methods
public string[] LoadData(string filePath)
public string[] LoadData(string filePath, string[] selectedFeatures)
public string[] ValidateAndPreviewData(string filePath, int numRows = 5)
public void FilterDataToSelectedFeatures(string[] selectedFeatures)

// Preprocessing methods (all the stub methods need real implementations)
public void ImputeMissingValues(string strategy = "mean")
public void StandardizeData()
public void MinMaxScaleData(double featureRangeMin = 0.0, double featureRangeMax = 1.0)
public void OneHotEncode(string[] categoricalFeatures)
// ... and many more
```

## Benefits of This Approach

### ? Advantages
1. **Clean Separation**: No Python code mixed with C#
2. **Maintainable**: Python scripts can be edited by Python developers
3. **Version Control**: Clear tracking of script changes
4. **IDE Support**: Full Python syntax highlighting and IntelliSense
5. **Testable**: Python scripts can be unit tested independently
6. **Reusable**: Scripts can be shared across different C# classes
7. **Performance**: Script caching reduces file I/O overhead

### ?? Implementation Status

#### ? Completed
- PythonScriptTemplateManager utility class
- Core ML scripts (cross_validation.py, grid_search.py, etc.)
- Template parameter substitution system
- Script caching mechanism

#### ?? In Progress  
- Creating all required Python script files
- Updating PythonMLManager.cs methods
- Adding using statement for template manager

#### ? Todo
- Update PythonTrainingViewModel.cs (already partially done)
- Comprehensive testing of all methods
- Performance validation
- Documentation updates

## Example Usage

### Data Loading
```csharp
// Old way (inline Python)
string script = $@"
import pandas as pd
data = pd.read_csv('{filePath}')
features = data.columns.tolist()
";

// New way (template manager)
var parameters = new Dictionary<string, object> { ["file_path"] = filePath };
string script = PythonScriptTemplateManager.GetScript("load_data", parameters);
```

### Cross-Validation (Already Implemented)
```csharp
var scriptParams = new Dictionary<string, object>
{
    ["algorithm_module"] = "ensemble",
    ["algorithm_name"] = "RandomForestClassifier",
    ["cv_folds"] = 5,
    ["label_column"] = "target"
};

string script = PythonScriptTemplateManager.GetScript("cross_validation", scriptParams);
```

## Next Steps

1. **Create All Python Scripts**: Generate all required `.py` files
2. **Add Using Statement**: Add `using Beep.Python.ML.Utils;` to PythonMLManager.cs
3. **Update Methods**: Replace all inline Python with template manager calls
4. **Test Thoroughly**: Ensure all functionality works correctly
5. **Update Documentation**: Document the new approach

This refactoring will result in a much cleaner, more maintainable, and professional codebase that follows best practices for mixed-language development.