# Switching GPU Backends for Models

## Can You Switch GPU Backends?

**Yes!** The system uses a **smart environment switching** approach:

## Smart Environment Strategy

Instead of rebuilding, the system creates **separate environments per backend**:

- `llm_model_cuda` - For CUDA backend
- `llm_model_vulkan` - For Vulkan backend  
- `llm_model_rocm` - For ROCm backend
- `llm_model_cpu` - For CPU backend

### How It Works

1. **First switch** (e.g., CUDA → Vulkan):
   - Creates new environment: `llm_model_vulkan`
   - Installs llama-cpp-python with Vulkan support (5-15 min)
   - Switches model association to new environment
   
2. **Subsequent switches** (Vulkan → CUDA → Vulkan):
   - **Instant!** Just switches association to existing environment
   - No recompilation needed
   - Takes seconds instead of minutes

## How to Switch GPU Backends

### Method 1: Smart Switch (Recommended) ⭐

**Via API:**
```bash
POST /llm/api/models/<model_id>/switch-backend
{
  "gpu_backend": "vulkan"
}
```

**What happens:**
- ✅ If environment exists → **Instant switch** (just changes association)
- ✅ If environment doesn't exist → Creates it, then switches
- ✅ Automatically unloads model if running
- ✅ Updates model configuration
- ✅ Returns task ID for progress tracking

**Example Flow:**
```
1. Model on CUDA → Switch to Vulkan
   → Creates llm_model_vulkan (5-15 min)
   → Switches association

2. Switch back to CUDA
   → Instant! (uses existing llm_model_cuda)

3. Switch to Vulkan again
   → Instant! (uses existing llm_model_vulkan)
```

### Method 2: Rebuild Environment (Legacy)

If you want to rebuild an existing environment instead:
```bash
POST /llm/api/venv/<venv_name>/rebuild
{
  "gpu_backend": "vulkan"
}
```

## What Happens During Switch

### First Time (Environment Creation):
1. **Model is unloaded** (if currently loaded)
2. **New environment is created** (e.g., `llm_model_vulkan`)
3. **llama-cpp-python is compiled** with the selected GPU backend (5-15 min)
4. **GPU support is verified** in the new installation
5. **Model association is switched** to the new environment
6. **Model needs to be reloaded** to use the new backend

### Subsequent Switches (Instant):
1. **Model is unloaded** (if currently loaded)
2. **Model association is switched** to existing environment (instant!)
3. **Model needs to be reloaded** to use the new backend

## Important Notes

### ⚠️ Limitations

- **One backend per environment**: Each venv can only have one GPU backend at a time
- **Recompilation required**: Switching takes 5-15 minutes (compilation time)
- **Model must be reloaded**: After rebuild, unload and reload the model

### ✅ Best Practices

1. **Plan your backends**: If you know you'll need multiple backends, create separate environments
2. **Test before switching**: Verify the new backend works with your hardware
3. **Backup important models**: Before major backend changes
4. **Check system toolkits**: Ensure the target GPU toolkit (CUDA/Vulkan/ROCm) is installed

## Example: Switching from CUDA to Vulkan

```python
# Current state: Model using CUDA
# Environment: llm_mistral_7b_cuda

# Step 1: Switch backend (smart - creates if needed)
POST /llm/api/models/<model_id>/switch-backend
{
  "gpu_backend": "vulkan"
}

# Response:
# {
#   "success": true,
#   "task_id": "...",
#   "instant_switch": false,  # First time - creating environment
#   "message": "Creating vulkan environment (first time setup)..."
# }

# Step 2: Wait for task to complete (check /tasks/<task_id>)
# - Creates: llm_mistral_7b_vulkan
# - Installs llama-cpp-python with Vulkan support
# - Switches model association

# Step 3: Reload model
POST /llm/api/models/<model_id>/load
{
  "n_gpu_layers": -1  # Use GPU layers
}

# Model now runs on Vulkan!

# Step 4: Switch back to CUDA (instant!)
POST /llm/api/models/<model_id>/switch-backend
{
  "gpu_backend": "cuda"
}

# Response:
# {
#   "success": true,
#   "task_id": "...",
#   "instant_switch": true,  # Uses existing environment!
#   "message": "Switching to existing cuda environment (instant switch)..."
# }

# Takes seconds instead of minutes!
```

## Environment Naming Convention

The system automatically creates environments with backend suffixes:

- Model: `mistral-7b-instruct-v0.2.Q8_0`
- CUDA environment: `llm_mistral_7b_instruct_v0_2_cuda`
- Vulkan environment: `llm_mistral_7b_instruct_v0_2_vulkan`
- ROCm environment: `llm_mistral_7b_instruct_v0_2_rocm`
- CPU environment: `llm_mistral_7b_instruct_v0_2_cpu`

This allows:
- ✅ Multiple backends per model
- ✅ Instant switching between existing environments
- ✅ No rebuilds needed after initial setup

## Troubleshooting

### "Backend switch failed"
- Check that the target GPU toolkit is installed (CUDA Toolkit, Vulkan SDK, etc.)
- Verify system requirements for the new backend
- Check compilation logs in the task details

### "Model still using old backend"
- **Unload the model** first: `POST /llm/api/models/<model_id>/unload`
- **Reload after rebuild**: `POST /llm/api/models/<model_id>/load`

### "Compilation takes too long"
- GPU backend compilation is normal (5-15 minutes)
- Ensure you have sufficient disk space
- Check that build tools (CMake, compiler) are installed

## Summary

✅ **Yes, you can switch** GPU backends (CUDA ↔ Vulkan ↔ ROCm ↔ CPU)  
✅ **Smart switching** - creates separate environments per backend  
⚡ **First switch**: Creates environment (5-15 min compilation)  
⚡ **Subsequent switches**: Instant! (just changes association)  
🔄 **Model must be reloaded** after switch  

### Benefits of This Approach:

- ⚡ **Fast switching** after initial setup
- 💾 **No rebuilds** - each backend has its own environment
- 🔄 **Easy switching** - just change model association
- 🎯 **Flexible** - can have multiple backends ready for the same model

The system automatically creates environments as needed and switches instantly between existing ones!

