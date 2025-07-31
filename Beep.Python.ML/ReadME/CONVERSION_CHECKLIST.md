# Complete Inline Python Conversion Guide for PythonMLManager.cs

## URGENT: ALL Inline Python Code Must Be Converted!

You're absolutely right - there are still several methods in PythonMLManager.cs with inline Python code that need to be converted to use the PythonScriptTemplateManager. Here's the complete list:

## Methods Still Containing Inline Python Code:

### 1. FilterDataToSelectedFeatures() (Lines ~217-240)
```csharp
// CURRENT - HAS INLINE PYTHON:
string script = $@"
# List of selected features
selected_features = [{selectedFeaturesList}]

# Filter train_data, test_data, and data based on selected features
if 'train_data' in globals():
    train_data = train_data[selected_features]
if 'test_data' in globals():
    test_data = test_data[selected_features]
if 'data' in globals():
    data = data[selected_features]

# Update the datasets in the Python scope (if needed)
globals()['train_data'] = train_data if 'train_data' in globals() else None
globals()['test_data'] = test_data if 'test_data' in globals() else None
globals()['data'] = data if 'data' in globals() else None
";

// SHOULD BE:
var parameters = new Dictionary<string, object>
{
    ["selected_features"] = selectedFeatures
};
string script = PythonScriptTemplateManager.GetScript("filter_selected_features", parameters);
```

### 2. LoadData(string filePath, string[] selectedFeatures) (Lines ~244-279)
```csharp
// CURRENT - HAS INLINE PYTHON:
string script = $@"
import pandas as pd

# Load the dataset
data = pd.read_csv('{modifiedFilePath}')
# List of selected features
selected_features = [{selectedFeaturesList}]

# Filter the data based on selected features
data = data[selected_features]

# Get the final list of features after filtering
features = data.columns.tolist()

# Store the filtered data back to the global scope if needed
globals()['data'] = data
";

// SHOULD BE:
var parameters = new Dictionary<string, object>
{
    ["file_path"] = filePath.Replace("\\", "\\\\"),
    ["selected_features"] = selectedFeatures
};
string script = PythonScriptTemplateManager.GetScript("load_data_with_features", parameters);
```

### 3. LoadData(string filePath) (Lines ~281-308)
```csharp
// CURRENT - HAS INLINE PYTHON:
string script = $@"
import pandas as pd

# Load the dataset
data = pd.read_csv('{modifiedFilePath}')

# Get the list of features (column names)
features = data.columns.tolist()

# Store the data in the global scope
globals()['data'] = data
";

// SHOULD BE:
var parameters = new Dictionary<string, object>
{
    ["file_path"] = filePath.Replace("\\", "\\\\")
};
string script = PythonScriptTemplateManager.GetScript("load_data", parameters);
```

### 4. GetStringArrayFromSession() (Lines ~154-172)
```csharp
// CURRENT - HAS INLINE PYTHON:
string script = $@"
import json
if '{variableName}' in globals():
    result_json = json.dumps({variableName})
else:
    result_json = '[]'
";

// SHOULD BE:
var parameters = new Dictionary<string, object>
{
    ["variable_name"] = variableName
};
string script = PythonScriptTemplateManager.GetScript("get_string_array", parameters);
```

### 5. GetFromSessionScope<T>() (Lines ~183-200)
```csharp
// CURRENT - HAS INLINE PYTHON:
string script = $@"
import json
if '{variableName}' in globals():
    if isinstance({variableName}, (list, dict, str, int, float, bool)):
        result_json = json.dumps({variableName})
    else:
        result_json = json.dumps(str({variableName}))
else:
    result_json = 'null'
";

// SHOULD BE:
var parameters = new Dictionary<string, object>
{
    ["variable_name"] = variableName
};
string script = PythonScriptTemplateManager.GetScript("get_from_session_scope", parameters);
```

## Required Python Script Files to Create:

### 1. filter_selected_features.py
```python
# List of selected features
selected_features = {selected_features}

# Filter train_data, test_data, and data based on selected features
if 'train_data' in globals():
    train_data = train_data[selected_features]
if 'test_data' in globals():
    test_data = test_data[selected_features]
if 'data' in globals():
    data = data[selected_features]

# Update the datasets in the Python scope
globals()['train_data'] = train_data if 'train_data' in globals() else None
globals()['test_data'] = test_data if 'test_data' in globals() else None
globals()['data'] = data if 'data' in globals() else None
```

### 2. load_data_with_features.py
```python
import pandas as pd

# Load the dataset
data = pd.read_csv('{file_path}')
# List of selected features
selected_features = {selected_features}

# Filter the data based on selected features
data = data[selected_features]

# Get the final list of features after filtering
features = data.columns.tolist()

# Store the filtered data back to the global scope
globals()['data'] = data
```

### 3. get_string_array.py
```python
import json
if '{variable_name}' in globals():
    result_json = json.dumps({variable_name})
else:
    result_json = '[]'
```

### 4. get_from_session_scope.py
```python
import json
if '{variable_name}' in globals():
    if isinstance({variable_name}, (list, dict, str, int, float, bool)):
        result_json = json.dumps({variable_name})
    else:
        result_json = json.dumps(str({variable_name}))
else:
    result_json = 'null'
```

## Action Items:

1. **Create the 4 missing Python script files** in `Beep.Python.ML/Scripts/`
2. **Replace all 5 methods** in `PythonMLManager.cs` to use the template manager
3. **Verify** the using statement `using Beep.Python.ML.Utils;` is present
4. **Test** all functionality to ensure the conversion works correctly

## Current Status:
- ? ValidateAndPreviewData() - Already converted to use template manager
- ? FilterDataToSelectedFeatures() - STILL HAS INLINE PYTHON
- ? LoadData(string filePath, string[] selectedFeatures) - STILL HAS INLINE PYTHON  
- ? LoadData(string filePath) - STILL HAS INLINE PYTHON
- ? GetStringArrayFromSession() - STILL HAS INLINE PYTHON
- ? GetFromSessionScope<T>() - STILL HAS INLINE PYTHON

**YOU ARE CORRECT - THESE METHODS STILL NEED TO BE CONVERTED!**