# ML Model Environment Design

## Overview

The ML Model Hosting system uses a **single shared Python virtual environment** for all ML models, similar to how the RAG system works. This approach provides isolation from the main application while being simpler to manage than per-model environments.

## Architecture Decision

### Why Single Shared Environment?

**Advantages:**
- ✅ **Simpler Management**: One environment to maintain instead of many
- ✅ **Shared Dependencies**: ML models often share common packages (numpy, pandas, scikit-learn)
- ✅ **Less Overhead**: No need to create/duplicate environments for each model
- ✅ **Consistent**: All models run in the same environment, reducing compatibility issues
- ✅ **Easier Updates**: Update packages once for all models

**Disadvantages:**
- ⚠️ **Dependency Conflicts**: If two models require incompatible package versions, this could be an issue
- ⚠️ **Less Isolation**: Models share the same environment (but still isolated from main app)

### Comparison with Other Systems

| System | Environment Strategy | Rationale |
|--------|---------------------|-----------|
| **RAG** | Single shared environment (`rag_env`) | All RAG operations use similar dependencies (FAISS, ChromaDB, sentence-transformers) |
| **LLM** | Per-model OR shared environment | LLM models can have very different requirements (different llama-cpp-python builds) |
| **ML Models** | **Single shared environment** (`ml_models_env`) | ML models typically share common dependencies (numpy, scikit-learn, etc.) |

## Implementation

### Components

1. **MLModelEnvironmentManager** (`app/services/ml_model_environment.py`)
   - Uses existing `EnvironmentManager` to create/manage the virtual environment
   - Manages package installation
   - Tracks environment status
   - Stores configuration in `config/ml_model_environment.json`

2. **ML Model Subprocess Executor** (`app/services/ml_model_subprocess.py`)
   - Runs model predictions in isolated environment via subprocess
   - Handles all model types (sklearn, tensorflow, pytorch, xgboost, onnx)
   - Returns predictions as JSON

3. **Integration in Routes** (`app/routes/ml_models.py`)
   - Prediction routes try to use isolated environment first
   - Falls back to direct loading if environment not ready
   - Supports both modes for backward compatibility

### Environment Location

- **Path**: `{app_directory}/providers/ml_models_env/`
- **Created by**: `EnvironmentManager` (existing service)
- **Config**: `{app_directory}/config/ml_model_environment.json`

### Default Packages

**Required Packages** (always installed):
- `numpy>=1.20.0`
- `scipy>=1.7.0`
- `pandas>=1.3.0`
- `scikit-learn>=1.0.0`
- `joblib>=1.0.0`

**Optional Packages** (installed on demand):
- `tensorflow>=2.8.0` (or `tensorflow-gpu` for GPU)
- `torch>=1.11.0` (PyTorch)
- `xgboost>=1.5.0`
- `onnxruntime>=1.10.0`
- `keras>=2.8.0`

## Usage Flow

### 1. Environment Setup

```python
from app.services.ml_model_environment import get_ml_model_environment_manager

env_mgr = get_ml_model_environment_manager()

# Setup environment (creates venv and installs required packages)
result = env_mgr.setup_environment(
    install_optional=True,  # Install TensorFlow, PyTorch, etc.
    use_gpu=False           # Use GPU versions
)
```

### 2. Model Prediction

When a prediction is requested:

1. **Check if ML environment is ready**
2. **If ready**: Run prediction in isolated environment via subprocess
3. **If not ready**: Fall back to direct loading (backward compatibility)

```python
# In _run_model_prediction()
if env_mgr.is_ready:
    # Use isolated environment
    result = run_model_prediction_in_env(model_path, model_type, input_data)
else:
    # Fallback to direct loading
    model_obj = load_model_directly()
    prediction = model_obj.predict(input_data)
```

### 3. Model-Specific Dependencies

When a model is uploaded with `requirements.txt`, those packages are installed in the shared environment:

```python
# During model upload
if model_requirements:
    env_mgr.install_model_dependencies(model_requirements)
```

## Settings UI

The settings page (`/ml-models/settings`) shows:
- **Environment Status**: Ready, Created, Not Created, Error
- **Environment Path**: Location of the virtual environment
- **Installed Packages**: List of installed packages
- **Setup Button**: Create/setup environment (admin only)
- **Install Packages**: Install additional packages

## API Endpoints

- `POST /api/v1/ml-models/settings/environment/setup` - Setup environment
- `GET /api/v1/ml-models/settings/environment/status` - Get status
- `POST /api/v1/ml-models/settings/environment/install-packages` - Install packages

## Benefits

1. **Isolation**: ML models run in separate environment from main app (REQUIRED - no fallback)
2. **Auto-Setup**: Environment is automatically created when needed
3. **Dependency Management**: Centralized package management
4. **Security**: Models can't affect main application
5. **Flexibility**: Can install model-specific dependencies as needed
6. **Performance**: Shared environment means packages loaded once
7. **Compatibility**: Uses existing `EnvironmentManager` infrastructure

## Future Enhancements

If needed, we could add:
- **Per-Model Environments**: For models with conflicting dependencies
- **Environment Templates**: Pre-configured environments for different ML frameworks
- **Automatic Dependency Resolution**: Auto-install packages from model requirements
- **Environment Versioning**: Track environment changes over time

## Current Implementation Status

✅ **Implemented:**
- Single shared ML environment manager
- Uses existing `EnvironmentManager`
- Subprocess executor for isolated predictions (REQUIRED - no fallback)
- Settings UI for environment management
- Automatic environment creation when needed
- Package installation support

✅ **Ready to Use:**
- Environment setup via UI or API
- Auto-creation when predictions are requested
- Model predictions in isolated environment (enforced)
- Dependency management

---

**Design Pattern**: Single Shared Environment (like RAG)  
**Implementation**: Uses existing `EnvironmentManager`  
**Location**: `{app_dir}/providers/ml_models_env/`  
**Status**: ✅ Complete and Integrated

