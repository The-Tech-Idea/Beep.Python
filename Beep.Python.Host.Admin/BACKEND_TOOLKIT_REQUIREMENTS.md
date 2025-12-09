# Backend Toolkit Requirements

This document outlines the system-level toolkit requirements for each GPU backend.

## Overview

Each GPU backend requires a **system-level toolkit/SDK** to be installed before compilation. Python packages are installed automatically in the virtual environment, but the system toolkits must be installed separately.

## Backend Requirements

### CUDA (NVIDIA)

**System Toolkit Required:** CUDA Toolkit  
**Download:** https://developer.nvidia.com/cuda-downloads

**Requirements:**
- ✅ CUDA Toolkit (system-level installation)
- ✅ NVIDIA GPU with CUDA support
- ✅ CUDA_PATH environment variable set OR CUDA bin in PATH
- ✅ Python packages installed automatically: `nvidia-cuda-runtime-cu12`, `nvidia-cublas-cu12`

**Installation:**
1. Download and install CUDA Toolkit from NVIDIA
2. Set `CUDA_PATH` environment variable (or ensure CUDA bin is in PATH)
3. System will automatically detect and install Python packages during venv setup

**Common Paths:**
- Windows: `C:/Program Files/NVIDIA GPU Computing Toolkit/CUDA/v12.x/`
- Linux: `/usr/local/cuda/`
- macOS: Not supported

---

### ROCm (AMD)

**System Toolkit Required:** ROCm SDK  
**Download:** https://rocm.docs.amd.com/

**Requirements:**
- ✅ ROCm SDK (system-level installation)
- ✅ Compatible AMD GPU
- ✅ Linux (ROCm not officially supported on Windows)
- ✅ ROCM_PATH environment variable set (defaults to `/opt/rocm`) OR rocm-smi in PATH
- ❌ No Python packages needed (all libraries are system-level)

**Installation:**
1. Install ROCm SDK following AMD's installation guide
2. Set `ROCM_PATH` environment variable (defaults to `/opt/rocm` on Linux)
3. Ensure `rocm-smi` is accessible in PATH

**Common Paths:**
- Linux: `/opt/rocm/`
- Windows: Not officially supported
- macOS: Not supported

---

### Vulkan (Cross-Platform)

**System Toolkit Required:** Vulkan SDK  
**Download:** https://vulkan.lunarg.com/sdk/home

**Requirements:**
- ✅ Vulkan SDK (system-level installation)
- ✅ Vulkan-compatible GPU with up-to-date drivers
- ✅ VULKAN_SDK environment variable set OR vulkaninfo in PATH
- ❌ No Python packages needed (all libraries are system-level)

**Installation:**
1. Download and install Vulkan SDK from LunarG
2. Set `VULKAN_SDK` environment variable
3. Ensure `vulkaninfo` is accessible in PATH

**Common Paths:**
- Windows: `C:/VulkanSDK/x.x.x.x/`
- Linux: `/usr/local/VulkanSDK/x.x.x.x/`
- macOS: `/usr/local/VulkanSDK/x.x.x.x/`

---

### Metal (Apple Silicon)

**System Toolkit Required:** None (built into macOS)  
**Download:** N/A

**Requirements:**
- ✅ macOS with Apple Silicon (M1, M2, M3, etc.)
- ✅ No additional installation needed
- ❌ No Python packages needed

**Installation:**
- No installation required - Metal is built into macOS

---

## Summary Table

| Backend | System Toolkit | Python Packages | Platform Support |
|---------|---------------|-----------------|------------------|
| **CUDA** | CUDA Toolkit | ✅ Auto-installed | Windows, Linux |
| **ROCm** | ROCm SDK | ❌ Not needed | Linux only |
| **Vulkan** | Vulkan SDK | ❌ Not needed | Windows, Linux, macOS |
| **Metal** | Built-in | ❌ Not needed | macOS only |
| **CPU** | None | ❌ Not needed | All platforms |

## Installation Flow

1. **Install System Toolkit First** (CUDA Toolkit, ROCm SDK, or Vulkan SDK)
2. **Set Environment Variables** (CUDA_PATH, ROCM_PATH, VULKAN_SDK)
3. **Create Model Environment** via Setup Wizard
4. **System Auto-Detects Toolkit** and installs backend
5. **Python Packages Installed** (for CUDA only)

## Troubleshooting

### CUDA Toolkit Not Found
- Verify CUDA Toolkit is installed
- Check `CUDA_PATH` environment variable
- Ensure CUDA bin directory is in PATH
- Run `nvcc --version` to verify installation

### ROCm SDK Not Found
- Verify ROCm SDK is installed (typically `/opt/rocm`)
- Check `ROCM_PATH` environment variable
- Ensure `rocm-smi` is in PATH
- Run `rocm-smi --version` to verify installation

### Vulkan SDK Not Found
- Verify Vulkan SDK is installed
- Check `VULKAN_SDK` environment variable
- Ensure `vulkaninfo` is in PATH
- Run `vulkaninfo --summary` to verify installation

## Notes

- **System toolkits are required** - Python packages alone are not sufficient
- **CUDA is the only backend** that requires Python packages (for runtime libraries)
- **ROCm and Vulkan** use system libraries only
- **Metal** requires no additional installation (built into macOS)
- All toolkits must be installed **before** creating the model environment

