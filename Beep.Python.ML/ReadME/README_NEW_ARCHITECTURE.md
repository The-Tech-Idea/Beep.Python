# Beep.Python.ML - Machine Learning Library

## Architecture Overview

The Beep.Python.ML library has been refactored to follow clean architecture principles:

- **Separation of Concerns**: Python scripts are stored in separate `.py` files
- **Template-Based Approach**: Python scripts use parameter placeholders like `{parameter_name}`
- **Clean C# Code**: No embedded Python strings in C# source code
- **Maintainable Scripts**: Python files can be edited independently

## Directory Structure

```
Beep.Python.ML/
??? Utils/
?   ??? PythonScriptTemplateManager.cs    # File loading & parameter substitution
??? Scripts/                               # Python script files (.py)
?   ??? cross_validation.py
?   ??? grid_search.py
?   ??? random_search.py
?   ??? model_comparison.py
?   ??? comprehensive_evaluation.py
?   ??? training_initialization.py
??? PythonTrainingViewModel.cs
??? PythonMLManager.cs
??? MLDataStructures.cs
```

## How It Works

### 1. Python Script Templates

Python scripts use placeholder syntax:

```python
# cross_validation.py
from sklearn.{algorithm_module} import {algorithm_name}

X = data.drop('{label_column}', axis=1)
y = data['{label_column}']

cv = StratifiedKFold(n_splits={cv_folds})
scores = cross_val_score(model, X, y, cv=cv)
```

### 2. C# Usage

```csharp
var parameters = new Dictionary<string, object>
{
    ["algorithm_module"] = "ensemble",
    ["algorithm_name"] = "RandomForestClassifier",
    ["label_column"] = "target",
    ["cv_folds"] = 5
};

string script = PythonScriptTemplateManager.GetScript("cross_validation", parameters);
```

### 3. Available Scripts

- `training_initialization.py` - Initialize ML environment
- `cross_validation.py` - K-fold cross-validation
- `grid_search.py` - Grid search optimization
- `random_search.py` - Random search optimization  
- `model_comparison.py` - Compare multiple algorithms
- `comprehensive_evaluation.py` - Detailed model evaluation

## Benefits

? Clean separation of Python and C# code
? Maintainable and version-control friendly
? Full IDE support for Python scripts
? Scripts can be tested independently
? Performance optimized with caching