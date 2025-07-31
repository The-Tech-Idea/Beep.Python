# Python Scripts Needed for PythonMLManager.cs Refactoring

## Current Inline Python Code Locations

I can see the following methods in PythonMLManager.cs that still contain inline Python code:

### 1. FilterDataToSelectedFeatures() - Lines ~217-240
**Current inline Python:**
```python
# List of selected features
selected_features = [{selectedFeaturesList}]

# Filter train_data, test_data, and data based on selected features
if 'train_data' in globals():
    train_data = train_data[selected_features]
if 'test_data' in globals():
    test_data = test_data[selected_features]
if 'data' in globals():
    data = data[selected_features]
```

**Needs:** `filter_selected_features.py`

### 2. LoadData(string filePath, string[] selectedFeatures) - Lines ~244-279
**Current inline Python:**
```python
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
```

**Needs:** `load_data_with_features.py`

### 3. LoadData(string filePath) - Lines ~281-308
**Current inline Python:**
```python
import pandas as pd

# Load the dataset
data = pd.read_csv('{modifiedFilePath}')

# Get the list of features (column names)
features = data.columns.tolist()

# Store the data in the global scope
globals()['data'] = data
```

**Already has:** `load_data.py` ?

### 4. GetStringArrayFromSession() helper - Lines ~154-172
**Current inline Python:**
```python
import json
if '{variableName}' in globals():
    result_json = json.dumps({variableName})
else:
    result_json = '[]'
```

**Needs:** `get_string_array.py`

### 5. GetFromSessionScope() helper - Lines ~183-200
**Current inline Python:**
```python
import json
if '{variableName}' in globals():
    if isinstance({variableName}, (list, dict, str, int, float, bool)):
        result_json = json.dumps({variableName})
    else:
        result_json = json.dumps(str({variableName}))
else:
    result_json = 'null'
```

**Needs:** `get_from_session_scope.py`

## Required Python Script Files

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

## Updated C# Methods

### FilterDataToSelectedFeatures() - UPDATED
```csharp
public void FilterDataToSelectedFeatures(string[] selectedFeatures)
{
    if (!IsInitialized)
    {
        throw new InvalidOperationException("The Python environment is not initialized.");
    }

    // Use template manager instead of inline Python
    var parameters = new Dictionary<string, object>
    {
        ["selected_features"] = selectedFeatures
    };

    string script = PythonScriptTemplateManager.GetScript("filter_selected_features", parameters);

    // Execute the Python script using session
    ExecuteInSession(script);
}
```

### LoadData(string filePath, string[] selectedFeatures) - UPDATED
```csharp
public string[] LoadData(string filePath, string[] selectedFeatures)
{
    if (!IsInitialized)
    {
        return null;
    }

    // Use template manager instead of inline Python
    var parameters = new Dictionary<string, object>
    {
        ["file_path"] = filePath.Replace("\\", "\\\\"),
        ["selected_features"] = selectedFeatures
    };

    string script = PythonScriptTemplateManager.GetScript("load_data_with_features", parameters);

    if (ExecuteInSession(script))
    {
        IsDataLoaded = true;
        DataFilePath = filePath;
    }
    else
    {
        IsDataLoaded = false;
    }

    return GetStringArrayFromSession("features");
}
```

### LoadData(string filePath) - UPDATED
```csharp
public string[] LoadData(string filePath)
{
    if (!IsInitialized)
    {
        return null;
    }

    // Use template manager instead of inline Python
    var parameters = new Dictionary<string, object>
    {
        ["file_path"] = filePath.Replace("\\", "\\\\")
    };

    string script = PythonScriptTemplateManager.GetScript("load_data", parameters);

    if (ExecuteInSession(script))
    {
        IsDataLoaded = true;
        DataFilePath = filePath;
    }
    else
    {
        IsDataLoaded = false;
    }

    return GetStringArrayFromSession("features");
}
```

### GetStringArrayFromSession() - UPDATED
```csharp
private string[] GetStringArrayFromSession(string variableName)
{
    if (!IsInitialized || SessionInfo == null)
        return Array.Empty<string>();

    try
    {
        var parameters = new Dictionary<string, object>
        {
            ["variable_name"] = variableName
        };

        string script = PythonScriptTemplateManager.GetScript("get_string_array", parameters);
        ExecuteInSession(script);
        
        var jsonResult = PythonRuntime.ExecuteManager.RunPythonCodeAndGetOutput(null, "result_json", SessionInfo);
        
        if (!string.IsNullOrEmpty(jsonResult?.ToString()))
        {
            var cleanJson = jsonResult.ToString().Trim('"').Replace("\\\"", "\"");
            var result = System.Text.Json.JsonSerializer.Deserialize<string[]>(cleanJson);
            return result ?? Array.Empty<string>();
        }
    }
    catch (Exception)
    {
        // Return empty array on any error
    }

    return Array.Empty<string>();
}
```

### GetFromSessionScope<T>() - UPDATED
```csharp
private T GetFromSessionScope<T>(string variableName, T defaultValue = default(T))
{
    if (!IsInitialized || SessionInfo == null)
        return defaultValue;

    try
    {
        var parameters = new Dictionary<string, object>
        {
            ["variable_name"] = variableName
        };

        string script = PythonScriptTemplateManager.GetScript("get_from_session_scope", parameters);
        ExecuteInSession(script);
        
        var jsonResult = PythonRuntime.ExecuteManager.RunPythonCodeAndGetOutput(null, "result_json", SessionInfo);
        
        if (!string.IsNullOrEmpty(jsonResult?.ToString()))
        {
            var cleanJson = jsonResult.ToString().Trim('"').Replace("\\\"", "\"");
            if (cleanJson != "null")
            {
                var result = System.Text.Json.JsonSerializer.Deserialize<T>(cleanJson);
                return result;
            }
        }
    }
    catch (Exception)
    {
        // Return default value on any error
    }

    return defaultValue;
}
```

## Summary

**TOTAL METHODS WITH INLINE PYTHON TO REPLACE: 5**

1. ? ValidateAndPreviewData() - Already updated to use template manager
2. ?? FilterDataToSelectedFeatures() - Needs update 
3. ?? LoadData(string filePath, string[] selectedFeatures) - Needs update
4. ?? LoadData(string filePath) - Needs update  
5. ?? GetStringArrayFromSession() - Needs update
6. ?? GetFromSessionScope<T>() - Needs update

**REQUIRED PYTHON SCRIPTS TO CREATE: 4**

1. ? load_data.py - Already exists
2. ? validate_and_preview_data.py - Already exists  
3. ?? filter_selected_features.py - Need to create
4. ?? load_data_with_features.py - Need to create
5. ?? get_string_array.py - Need to create
6. ?? get_from_session_scope.py - Need to create

This is the complete list of ALL inline Python code that needs to be converted in PythonMLManager.cs!