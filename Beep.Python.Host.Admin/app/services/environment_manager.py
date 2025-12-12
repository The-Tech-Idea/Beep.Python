"""
Virtual Environment Manager Service
Manages Python virtual environments
"""
import os
import subprocess
import json
import logging
from pathlib import Path
from dataclasses import dataclass, asdict
from typing import List, Optional, Dict, Any
import platform

logger = logging.getLogger(__name__)


@dataclass
class VirtualEnvironment:
    """Represents a Python virtual environment"""
    id: str
    name: str
    path: str
    python_version: str
    python_executable: str
    is_active: bool
    packages_count: int
    size_mb: float
    created_at: Optional[str] = None
    base_python: Optional[str] = None


@dataclass
class Package:
    """Represents an installed Python package"""
    name: str
    version: str
    location: str
    requires: List[str]
    required_by: List[str]


class EnvironmentManager:
    """Manages virtual environments"""
    
    def __init__(self, base_path: Optional[str] = None):
        # Use app's own folder - no fallback to user home
        if base_path:
            self.base_path = Path(base_path)
        else:
            from app.config_manager import get_app_directory
            self.base_path = get_app_directory()
        self.providers_path = self.base_path / "providers"
        self.providers_path.mkdir(parents=True, exist_ok=True)
    
    def list_environments(self) -> List[VirtualEnvironment]:
        """List all virtual environments"""
        environments = []
        
        if self.providers_path.exists():
            for venv_dir in self.providers_path.iterdir():
                if venv_dir.is_dir():
                    env = self._get_environment_info(venv_dir)
                    if env:
                        environments.append(env)
        
        return environments
    
    def _get_environment_info(self, venv_path: Path) -> Optional[VirtualEnvironment]:
        """Get information about a virtual environment"""
        if platform.system() == "Windows":
            python_exe = venv_path / "Scripts" / "python.exe"
            pip_exe = venv_path / "Scripts" / "pip.exe"
        else:
            python_exe = venv_path / "bin" / "python"
            pip_exe = venv_path / "bin" / "pip"
        
        if not python_exe.exists():
            return None
        
        try:
            # Get Python version
            result = subprocess.run(
                [str(python_exe), "--version"],
                capture_output=True,
                text=True,
                timeout=10
            )
            version = result.stdout.strip().replace("Python ", "") if result.returncode == 0 else "Unknown"
            
            # Get package count
            packages_count = 0
            if pip_exe.exists():
                result = subprocess.run(
                    [str(python_exe), "-m", "pip", "list", "--format=json"],
                    capture_output=True,
                    text=True,
                    timeout=30
                )
                if result.returncode == 0:
                    packages = json.loads(result.stdout)
                    packages_count = len(packages)
            
            # Calculate size
            size_mb = self._get_directory_size(venv_path) / (1024 * 1024)
            
            # Get creation time
            created_at = None
            try:
                stat = venv_path.stat()
                from datetime import datetime
                created_at = datetime.fromtimestamp(stat.st_ctime).isoformat()
            except:
                pass
            
            return VirtualEnvironment(
                id=venv_path.name,
                name=venv_path.name,
                path=str(venv_path),
                python_version=version,
                python_executable=str(python_exe),
                is_active=False,
                packages_count=packages_count,
                size_mb=round(size_mb, 2),
                created_at=created_at
            )
            
        except Exception as e:
            print(f"Error getting environment info: {e}")
            return None
    
    def _get_directory_size(self, path: Path) -> int:
        """Get total size of a directory in bytes"""
        total = 0
        try:
            for entry in path.rglob('*'):
                if entry.is_file():
                    total += entry.stat().st_size
        except:
            pass
        return total
    
    def create_environment(self, name: str, python_executable: Optional[str] = None,
                          packages: Optional[List[str]] = None) -> VirtualEnvironment:
        """Create a new virtual environment"""
        venv_path = self.providers_path / name
        
        if venv_path.exists():
            raise ValueError(f"Environment '{name}' already exists")
        
        # Use provided Python or default
        python_exe = python_executable or "python"
        
        # Create virtual environment
        subprocess.run(
            [python_exe, "-m", "venv", str(venv_path)],
            check=True
        )
        
        # Install packages if provided
        if packages:
            self.install_packages(name, packages)
        
        env = self._get_environment_info(venv_path)
        if not env:
            raise RuntimeError("Failed to create environment")
        
        return env
    
    def delete_environment(self, name: str) -> bool:
        """Delete a virtual environment"""
        import shutil
        
        venv_path = self.providers_path / name
        
        if not venv_path.exists():
            raise ValueError(f"Environment '{name}' not found")
        
        # Clean up LLM environment associations
        try:
            from app.services.llm_environment import get_llm_env_manager
            llm_env_mgr = get_llm_env_manager()
            llm_env_mgr.remove_environment_associations(name)
        except Exception as e:
            print(f"Warning: Failed to clean up LLM environment associations: {e}")
        
        shutil.rmtree(venv_path)
        return True
    
    def get_packages(self, name: str) -> List[Package]:
        """Get list of installed packages in an environment"""
        venv_path = self.providers_path / name
        
        if platform.system() == "Windows":
            python_exe = venv_path / "Scripts" / "python.exe"
        else:
            python_exe = venv_path / "bin" / "python"
        
        if not python_exe.exists():
            raise ValueError(f"Environment '{name}' not found")
        
        # Get package list with details
        result = subprocess.run(
            [str(python_exe), "-m", "pip", "list", "--format=json"],
            capture_output=True,
            text=True,
            timeout=60
        )
        
        packages = []
        if result.returncode == 0:
            pkg_list = json.loads(result.stdout)
            for pkg in pkg_list:
                packages.append(Package(
                    name=pkg.get('name', ''),
                    version=pkg.get('version', ''),
                    location=pkg.get('location', ''),
                    requires=[],
                    required_by=[]
                ))
        
        return packages
    
    def install_packages(self, name: str, packages: List[str]) -> dict:
        """Install packages in an environment"""
        venv_path = self.providers_path / name
        
        if platform.system() == "Windows":
            python_exe = venv_path / "Scripts" / "python.exe"
        else:
            python_exe = venv_path / "bin" / "python"
        
        if not python_exe.exists():
            raise ValueError(f"Environment '{name}' not found")
        
        # First upgrade pip, setuptools, and wheel to avoid installation issues
        try:
            subprocess.run(
                [str(python_exe), "-m", "pip", "install", "--upgrade", "pip", "setuptools", "wheel"],
                capture_output=True,
                text=True,
                timeout=120
            )
        except Exception as e:
            # Pip upgrade failed but continue with package installation
            logger.warning(f"Failed to upgrade pip: {e}")
        
        result = subprocess.run(
            [str(python_exe), "-m", "pip", "install"] + packages,
            capture_output=True,
            text=True,
            timeout=1800  # 30 minutes timeout for very large packages like TensorFlow
        )
        
        return {
            "success": result.returncode == 0,
            "stdout": result.stdout,
            "stderr": result.stderr
        }
    
    def uninstall_package(self, name: str, package: str) -> dict:
        """Uninstall a package from an environment"""
        venv_path = self.providers_path / name
        
        if platform.system() == "Windows":
            python_exe = venv_path / "Scripts" / "python.exe"
        else:
            python_exe = venv_path / "bin" / "python"
        
        if not python_exe.exists():
            raise ValueError(f"Environment '{name}' not found")
        
        result = subprocess.run(
            [str(python_exe), "-m", "pip", "uninstall", "-y", package],
            capture_output=True,
            text=True,
            timeout=60
        )
        
        return {
            "success": result.returncode == 0,
            "stdout": result.stdout,
            "stderr": result.stderr
        }
    
    def install_llama_cpp_python(self, name: str, gpu_backend: str = 'cpu') -> dict:
        """
        Install llama-cpp-python with GPU support.
        
        For GPU support, llama-cpp-python must be compiled from source with
        the appropriate CMAKE arguments.
        
        Args:
            name: Virtual environment name
            gpu_backend: One of 'cpu', 'cuda', 'rocm', 'metal', 'vulkan'
        
        Returns:
            dict with success, stdout, stderr
        """
        venv_path = self.providers_path / name
        
        if platform.system() == "Windows":
            python_exe = venv_path / "Scripts" / "python.exe"
        else:
            python_exe = venv_path / "bin" / "python"
        
        if not python_exe.exists():
            raise ValueError(f"Environment '{name}' not found")
        
        # Set up environment variables for CMAKE
        env = os.environ.copy()
        
        # Configure CMAKE args based on backend
        cmake_args = []
        
        # Track CUDA detection for later use
        nvcc_found = False
        nvidia_gpu_present = False
        cuda_warning = None  # Warning message for CUDA detection issues
        
        if gpu_backend == 'cuda' or gpu_backend == 'nvidia' or gpu_backend == 'cuda12':
            # CUDA support (NVIDIA)
            # Newer llama.cpp versions use GGML_CUDA, older versions use LLAMA_CUBLAS
            # We'll try GGML_CUDA first (newer), and if that fails, the error will indicate
            # which flag to use. For now, use GGML_CUDA as it's the current standard.
            cmake_args = ["-DGGML_CUDA=on"]
            env['CMAKE_ARGS'] = ' '.join(cmake_args)
            env['FORCE_CMAKE'] = '1'
            
            # Find and configure CUDA Toolkit paths
            cuda_path = None
            cuda_bin = None
            
            # Method 1: Check CUDA_PATH environment variable
            if 'CUDA_PATH' in os.environ:
                cuda_path = os.environ['CUDA_PATH']
                cuda_bin = Path(cuda_path) / 'bin'
                env['CUDA_PATH'] = cuda_path
                if cuda_bin.exists():
                    # Add CUDA bin to PATH for CMAKE to find nvcc
                    current_path = env.get('PATH', '')
                    env['PATH'] = str(cuda_bin) + os.pathsep + current_path
            
            # Method 2: Check common installation paths (Windows)
            if not cuda_path and platform.system() == 'Windows':
                common_paths = [
                    Path('C:/Program Files/NVIDIA GPU Computing Toolkit/CUDA'),
                    Path('C:/Program Files (x86)/NVIDIA GPU Computing Toolkit/CUDA'),
                ]
                for base_path in common_paths:
                    if base_path.exists():
                        # Find latest CUDA version
                        versions = sorted([d for d in base_path.iterdir() if d.is_dir() and d.name.startswith('v')], reverse=True)
                        if versions:
                            cuda_path = str(versions[0])
                            cuda_bin = versions[0] / 'bin'
                            env['CUDA_PATH'] = cuda_path
                            if cuda_bin.exists():
                                current_path = env.get('PATH', '')
                                env['PATH'] = str(cuda_bin) + os.pathsep + current_path
                            break
            
            # Method 3: Check common Linux installation paths
            if not cuda_path and platform.system() == 'Linux':
                common_linux_paths = [
                    Path('/usr/local/cuda'),
                    Path('/opt/cuda'),
                    Path('/usr/local/cuda-12'),
                    Path('/usr/local/cuda-11'),
                ]
                for cuda_install_path in common_linux_paths:
                    if cuda_install_path.exists():
                        cuda_path = str(cuda_install_path)
                        cuda_bin = cuda_install_path / 'bin'
                        env['CUDA_PATH'] = cuda_path
                        if cuda_bin.exists():
                            current_path = env.get('PATH', '')
                            env['PATH'] = str(cuda_bin) + os.pathsep + current_path
                        break
            
            # Set additional CMAKE variables to help it find CUDA
            if cuda_path:
                # CMAKE can use CUDA_PATH to find CUDA
                env['CUDA_PATH'] = cuda_path
                # Also set CMAKE_PREFIX_PATH as fallback
                cuda_lib = Path(cuda_path) / 'lib64'
                if cuda_lib.exists():
                    cmake_prefix = env.get('CMAKE_PREFIX_PATH', '')
                    if cmake_prefix:
                        env['CMAKE_PREFIX_PATH'] = f"{cuda_path}{os.pathsep}{cmake_prefix}"
                    else:
                        env['CMAKE_PREFIX_PATH'] = cuda_path
            
            # Verify nvcc is accessible (either in PATH or we found it)
            nvcc_found = False
            nvcc_path = None
            
            # First, try to find nvcc in the detected CUDA path
            if cuda_path:
                nvcc_exe = 'nvcc.exe' if platform.system() == 'Windows' else 'nvcc'
                potential_nvcc = Path(cuda_path) / 'bin' / nvcc_exe
                if potential_nvcc.exists():
                    nvcc_path = str(potential_nvcc)
                    nvcc_found = True
            
            # If not found in path, try running nvcc with updated PATH
            if not nvcc_found:
                try:
                    nvcc_check = subprocess.run(
                        ['nvcc', '--version'],
                        capture_output=True,
                        text=True,
                        timeout=5,
                        env=env  # Use env with updated PATH
                    )
                    if nvcc_check.returncode == 0:
                        nvcc_found = True
                        # Extract nvcc path from output or find it
                        if platform.system() == 'Windows':
                            nvcc_path = str(Path(cuda_path) / 'bin' / 'nvcc.exe') if cuda_path else None
                        else:
                            nvcc_path = str(Path(cuda_path) / 'bin' / 'nvcc') if cuda_path else None
                except (FileNotFoundError, subprocess.TimeoutExpired):
                    pass
            
            # Verify CUDA libraries exist (even if nvcc not found, libraries might be there)
            cuda_libs_found = False
            if cuda_path:
                cuda_lib_path = Path(cuda_path) / ('lib' if platform.system() == 'Windows' else 'lib64')
                # Check for common CUDA libraries
                cuda_libs = ['cudart', 'cublas', 'curand']
                for lib_name in cuda_libs:
                    if platform.system() == 'Windows':
                        lib_file = cuda_lib_path / f"{lib_name}*.dll"
                    else:
                        lib_file = cuda_lib_path / f"lib{lib_name}.so*"
                    
                    # Check if any matching files exist
                    if list(cuda_lib_path.glob(lib_file.name if platform.system() == 'Windows' else f"lib{lib_name}.so*")):
                        cuda_libs_found = True
                        break
            
            # Also check if nvidia-smi works (indicates GPU/driver present)
            nvidia_gpu_present = False
            try:
                nvidia_check = subprocess.run(
                    ['nvidia-smi'],
                    capture_output=True,
                    text=True,
                    timeout=5
                )
                if nvidia_check.returncode == 0:
                    nvidia_gpu_present = True
            except (FileNotFoundError, subprocess.TimeoutExpired):
                pass
            
            # Determine if we can proceed with CUDA build
            can_build_cuda = False
            if cuda_path and (nvcc_found or cuda_libs_found):
                # We have CUDA path and either nvcc or libraries - CMAKE should be able to find it
                can_build_cuda = True
            elif nvidia_gpu_present and cuda_path:
                # GPU present and CUDA path found - CMAKE might still find it
                can_build_cuda = True
                cuda_warning = (
                    f"CUDA Toolkit found at {cuda_path}, but nvcc compiler not directly accessible. "
                    "CMAKE will attempt to auto-detect CUDA. If build fails, ensure CUDA Toolkit is fully installed."
                )
            elif nvidia_gpu_present and not cuda_path:
                # GPU present but no CUDA path found
                can_build_cuda = False
                cuda_warning = (
                    "NVIDIA GPU detected, but CUDA Toolkit not found. "
                    "Please install CUDA Toolkit from https://developer.nvidia.com/cuda-downloads "
                    "or set CUDA_PATH environment variable."
                )
            
            if not can_build_cuda:
                error_details = []
                if not nvidia_gpu_present:
                    error_details.append("  - No NVIDIA GPU detected")
                if not cuda_path:
                    error_details.append("  - CUDA Toolkit not found")
                    if platform.system() == 'Windows':
                        error_details.append("    Expected locations:")
                        error_details.append("    - C:/Program Files/NVIDIA GPU Computing Toolkit/CUDA/v12.x/")
                        error_details.append("    - Set CUDA_PATH environment variable")
                    else:
                        error_details.append("    Expected locations:")
                        error_details.append("    - /usr/local/cuda/")
                        error_details.append("    - Set CUDA_PATH environment variable")
                if not nvcc_found and not cuda_libs_found:
                    error_details.append("  - CUDA compiler (nvcc) or libraries not found")
                
                # Provide automatic installation option (LM Studio style)
                auto_install_available = True
                try:
                    from app.services.cuda_installer import get_toolkit_installer
                    installer = get_toolkit_installer()
                    download_url = installer.get_download_url()
                    auto_install_available = download_url is not None
                except:
                    auto_install_available = False
                
                error_msg = "CUDA Toolkit not properly configured.\n\n" + \
                           "System Requirements:\n" + \
                           "\n".join(error_details)
                
                if auto_install_available:
                    error_msg += "\n\n" + \
                                "🔄 AUTOMATIC INSTALLATION AVAILABLE (LM Studio style):\n" + \
                                "  You can install CUDA Toolkit automatically:\n" + \
                                "  1. Use the 'Install CUDA Toolkit' button in the wizard\n" + \
                                "  2. Or call /llm/api/cuda/auto-install API endpoint\n" + \
                                "  3. The installer will download and launch automatically\n" + \
                                "  4. Follow the installation wizard\n" + \
                                "  5. Restart the application after installation\n\n" + \
                                "Manual Installation (Alternative):\n"
                else:
                    error_msg += "\n\nInstallation Steps:\n"
                
                error_msg += "  1. Download CUDA Toolkit from https://developer.nvidia.com/cuda-downloads\n" + \
                            "  2. Install CUDA Toolkit (system-level installation)\n" + \
                            "  3. Set CUDA_PATH environment variable to CUDA installation directory\n" + \
                            (f"     (e.g., C:/Program Files/NVIDIA GPU Computing Toolkit/CUDA/v12.x)\n" if platform.system() == 'Windows' else
                             f"     (e.g., /usr/local/cuda)\n") + \
                            "  4. Restart the application\n\n" + \
                            "Note: Python packages (nvidia-cuda-runtime-cu12, etc.) will be installed automatically in the venv."
                
                return {
                    "success": False,
                    "stdout": "",
                    "stderr": error_msg,
                    "gpu_backend": gpu_backend,
                    "cmake_args": cmake_args,
                    "gpu_verified": False,
                    "warning": cuda_warning or "CUDA Toolkit not found - cannot build with CUDA support",
                    "required_toolkit": "CUDA Toolkit (system-level)",
                    "cuda_path_found": cuda_path is not None,
                    "nvcc_found": nvcc_found,
                    "cuda_libs_found": cuda_libs_found,
                    "auto_install_available": auto_install_available,
                    "auto_install_endpoint": "/llm/api/cuda/auto-install" if auto_install_available else None
                }
        elif gpu_backend == 'rocm' or gpu_backend == 'amd':
            # ROCm support (AMD)
            cmake_args = ["-DLLAMA_HIPBLAS=on"]
            env['CMAKE_ARGS'] = ' '.join(cmake_args)
            env['FORCE_CMAKE'] = '1'
            
            # Configure ROCm paths (typically /opt/rocm on Linux)
            rocm_path = os.environ.get('ROCM_PATH', '/opt/rocm')
            if Path(rocm_path).exists():
                env['ROCM_PATH'] = rocm_path
                # Add ROCm bin to PATH
                rocm_bin = Path(rocm_path) / 'bin'
                if rocm_bin.exists():
                    current_path = env.get('PATH', '')
                    env['PATH'] = str(rocm_bin) + os.pathsep + current_path
                
                # Set ROCm compiler paths
                rocm_llvm = Path(rocm_path) / 'llvm' / 'bin'
                if (rocm_llvm / 'clang').exists():
                    env['CC'] = str(rocm_llvm / 'clang')
                    env['CXX'] = str(rocm_llvm / 'clang++')
            
            # Check if ROCm is available
            rocm_found = False
            try:
                rocm_check = subprocess.run(
                    ['rocm-smi', '--version'],
                    capture_output=True,
                    text=True,
                    timeout=5,
                    env=env  # Use env with updated PATH
                )
                if rocm_check.returncode == 0:
                    rocm_found = True
            except (FileNotFoundError, subprocess.TimeoutExpired):
                pass
            
            if not rocm_found:
                # ROCm requires manual installation (package manager on Linux)
                try:
                    from app.services.cuda_installer import get_toolkit_installer
                    installer = get_toolkit_installer()
                    rocm_info = installer.get_rocm_install_instructions()
                except:
                    rocm_info = {'manual_install': True}
                
                error_msg = "ROCm SDK not found. Please install ROCm SDK.\n\n" + \
                           "System Requirements:\n" + \
                           "  - ROCm SDK (system-level installation required)\n" + \
                           "  - On Linux, ROCm is typically installed to /opt/rocm\n" + \
                           "  - Ensure rocm-smi is in PATH or set ROCM_PATH environment variable\n" + \
                           "  - Compatible AMD GPU required\n" + \
                           "  - Linux only (ROCm not officially supported on Windows)\n\n"
                
                if rocm_info.get('manual_install'):
                    error_msg += "Installation Instructions:\n" + \
                                "  1. Follow AMD ROCm installation guide: https://rocm.docs.amd.com/\n" + \
                                "  2. Install ROCm packages via package manager (apt/yum)\n" + \
                                "  3. Set ROCM_PATH environment variable (defaults to /opt/rocm)\n" + \
                                "  4. Verify installation: rocm-smi --version\n" + \
                                "  5. Restart the application\n\n"
                
                error_msg += "Note: ROCm does not require Python packages - all libraries are system-level."
                
                return {
                    "success": False,
                    "stdout": "",
                    "stderr": error_msg,
                    "gpu_backend": gpu_backend,
                    "cmake_args": cmake_args,
                    "gpu_verified": False,
                    "warning": "ROCm SDK not found - cannot build with ROCm support",
                    "required_toolkit": "ROCm SDK (system-level)",
                    "auto_install_available": False,
                    "manual_install": True,
                    "install_url": "https://rocm.docs.amd.com/projects/install-on-linux/en/latest/"
                }
        elif gpu_backend == 'metal':
            # Metal support (Apple Silicon)
            cmake_args = ["-DLLAMA_METAL=on"]
            env['CMAKE_ARGS'] = ' '.join(cmake_args)
            env['FORCE_CMAKE'] = '1'
            # Metal is only available on macOS
            if platform.system() != 'Darwin':
                return {
                    "success": False,
                    "stdout": "",
                    "stderr": "Metal is only supported on macOS (Apple Silicon).",
                    "gpu_backend": gpu_backend,
                    "cmake_args": cmake_args,
                    "gpu_verified": False,
                    "warning": "Metal not supported on this platform"
                }
        elif gpu_backend == 'vulkan':
            # Vulkan support
            cmake_args = ["-DLLAMA_VULKAN=on"]
            env['CMAKE_ARGS'] = ' '.join(cmake_args)
            env['FORCE_CMAKE'] = '1'
            
            # Configure Vulkan SDK paths
            vulkan_sdk = os.environ.get('VULKAN_SDK')
            if vulkan_sdk:
                env['VULKAN_SDK'] = vulkan_sdk
                # Add Vulkan SDK bin to PATH
                vulkan_bin = Path(vulkan_sdk) / 'bin'
                if vulkan_bin.exists():
                    current_path = env.get('PATH', '')
                    env['PATH'] = str(vulkan_bin) + os.pathsep + current_path
            
            # Check if Vulkan SDK is available
            vulkan_found = False
            try:
                vulkan_check = subprocess.run(
                    ['vulkaninfo', '--summary'],
                    capture_output=True,
                    text=True,
                    timeout=5,
                    env=env  # Use env with updated PATH
                )
                if vulkan_check.returncode == 0:
                    vulkan_found = True
            except (FileNotFoundError, subprocess.TimeoutExpired):
                pass
            
            if not vulkan_found and not vulkan_sdk:
                # Check if automatic installation is available
                auto_install_available = False
                try:
                    from app.services.cuda_installer import get_toolkit_installer
                    installer = get_toolkit_installer()
                    vulkan_url = installer.get_vulkan_download_url()
                    auto_install_available = vulkan_url is not None
                except:
                    pass
                
                error_msg = "Vulkan SDK not found. Please install Vulkan SDK.\n\n" + \
                           "System Requirements:\n" + \
                           "  - Vulkan SDK (system-level installation required)\n" + \
                           "  - Vulkan-compatible GPU with up-to-date drivers\n" + \
                           "  - After installation, set VULKAN_SDK environment variable\n" + \
                           "  - Or add Vulkan SDK bin directory to PATH\n\n"
                
                if auto_install_available:
                    error_msg += "🔄 AUTOMATIC INSTALLATION AVAILABLE:\n" + \
                                "  You can install Vulkan SDK automatically:\n" + \
                                "  1. Use the 'Install Vulkan SDK' button in the wizard\n" + \
                                "  2. Or call /llm/api/toolkit/install API endpoint\n" + \
                                "  3. The installer will download and launch automatically\n" + \
                                "  4. Follow the installation wizard\n" + \
                                "  5. Restart the application after installation\n\n" + \
                                "Manual Installation (Alternative):\n"
                else:
                    error_msg += "Installation Steps:\n"
                
                error_msg += "  1. Download Vulkan SDK from https://vulkan.lunarg.com/sdk/home\n" + \
                            "  2. Install Vulkan SDK (system-level installation)\n" + \
                            "  3. Set VULKAN_SDK environment variable\n" + \
                            "  4. Restart the application\n\n" + \
                            "Note: Vulkan does not require Python packages - all libraries are system-level."
                
                return {
                    "success": False,
                    "stdout": "",
                    "stderr": error_msg,
                    "gpu_backend": gpu_backend,
                    "cmake_args": cmake_args,
                    "gpu_verified": False,
                    "warning": "Vulkan SDK not found - cannot build with Vulkan support",
                    "required_toolkit": "Vulkan SDK (system-level)",
                    "auto_install_available": auto_install_available,
                    "auto_install_endpoint": "/llm/api/toolkit/install" if auto_install_available else None
                }
        elif gpu_backend == 'openblas':
            # OpenBLAS CPU optimization
            cmake_args = ["-DLLAMA_BLAS=ON", "-DLLAMA_BLAS_VENDOR=OpenBLAS"]
            env['CMAKE_ARGS'] = ' '.join(cmake_args)
            env['FORCE_CMAKE'] = '1'
        # else: cpu - no special args needed
        
        # Install backend-specific Python packages in the venv for runtime support
        # Note: System-level toolkits (CUDA Toolkit, ROCm SDK, Vulkan SDK) must be installed separately
        
        if gpu_backend in ['cuda', 'nvidia', 'cuda12'] and (nvcc_found or nvidia_gpu_present):
            # CUDA: Install Python packages that provide CUDA runtime libraries
            # System requirement: CUDA Toolkit must be installed (system-level)
            try:
                # Try to detect CUDA version to install matching Python packages
                cuda_version = None
                if 'CUDA_PATH' in env:
                    cuda_path_str = env['CUDA_PATH']
                    # Extract version from path (e.g., "C:/Program Files/.../CUDA/v12.8")
                    import re
                    version_match = re.search(r'v?(\d+)\.(\d+)', cuda_path_str)
                    if version_match:
                        major, minor = version_match.groups()
                        cuda_version = f"{major}.{minor}"
                
                # Install CUDA Python packages (these provide runtime libraries)
                # Use cu12 for CUDA 12.x, cu11 for CUDA 11.x, etc.
                cuda_pkg_suffix = "cu12"  # Default to CUDA 12
                if cuda_version:
                    major_ver = int(cuda_version.split('.')[0])
                    if major_ver == 11:
                        cuda_pkg_suffix = "cu11"
                    elif major_ver == 12:
                        cuda_pkg_suffix = "cu12"
                
                # Install CUDA runtime packages (non-critical, continue if they fail)
                cuda_packages = [
                    f"nvidia-cuda-runtime-{cuda_pkg_suffix}",
                    f"nvidia-cublas-{cuda_pkg_suffix}",
                ]
                
                for pkg in cuda_packages:
                    try:
                        subprocess.run(
                            [str(python_exe), "-m", "pip", "install", pkg, "--quiet"],
                            capture_output=True,
                            text=True,
                            timeout=120,
                            env=env
                        )
                    except (subprocess.TimeoutExpired, Exception):
                        # Non-critical - continue even if some packages fail
                        pass
            except Exception:
                # Non-critical - continue with installation
                pass
        
        elif gpu_backend == 'rocm' or gpu_backend == 'amd':
            # ROCm: No Python packages needed - all libraries are system-level
            # System requirement: ROCm SDK must be installed (system-level, typically /opt/rocm on Linux)
            # ROCm provides system libraries, not Python packages
            pass
        
        elif gpu_backend == 'vulkan':
            # Vulkan: No Python packages needed - all libraries are system-level
            # System requirement: Vulkan SDK must be installed (system-level)
            # Vulkan provides system libraries, not Python packages
            pass
        
        elif gpu_backend == 'metal':
            # Metal: No additional packages needed - built into macOS
            # System requirement: macOS with Apple Silicon (M1/M2/M3, etc.)
            pass
        
        # First uninstall existing llama-cpp-python if present
        subprocess.run(
            [str(python_exe), "-m", "pip", "uninstall", "-y", "llama-cpp-python"],
            capture_output=True,
            text=True,
            timeout=60
        )
        
        # Install with --no-cache-dir to force rebuild
        cmd = [str(python_exe), "-m", "pip", "install", "--no-cache-dir", "--force-reinstall", "llama-cpp-python"]
        
        result = subprocess.run(
            cmd,
            capture_output=True,
            text=True,
            timeout=900,  # 15 minutes - compilation can take time
            env=env
        )
        
        # Verify GPU support if installation succeeded and GPU backend was requested
        gpu_verified = False
        if result.returncode == 0 and gpu_backend in ['cuda', 'nvidia', 'rocm', 'metal', 'vulkan']:
            # Normalize backend name for verification
            verify_backend = gpu_backend
            if gpu_backend == 'nvidia':
                verify_backend = 'cuda'
            
            # Check if GPU support is actually present
            verify_cmd = [
                str(python_exe), "-c",
                f"import llama_cpp; print(hasattr(llama_cpp, 'llama_backend_{verify_backend}'))"
            ]
            verify_result = subprocess.run(
                verify_cmd,
                capture_output=True,
                text=True,
                timeout=10
            )
            if verify_result.returncode == 0 and 'True' in verify_result.stdout:
                gpu_verified = True
            else:
                # Try alternative checks for specific backends
                if gpu_backend in ['cuda', 'nvidia']:
                    # Try both cuda and cublas
                    for alt_backend in ['cuda', 'cublas']:
                        verify_cmd2 = [
                            str(python_exe), "-c",
                            f"import llama_cpp; print(hasattr(llama_cpp, 'llama_backend_{alt_backend}'))"
                        ]
                        verify_result2 = subprocess.run(
                            verify_cmd2,
                            capture_output=True,
                            text=True,
                            timeout=10
                        )
                        if verify_result2.returncode == 0 and 'True' in verify_result2.stdout:
                            gpu_verified = True
                            break
                elif gpu_backend == 'rocm':
                    # Try hipblas as alternative
                    verify_cmd2 = [
                        str(python_exe), "-c",
                        "import llama_cpp; print(hasattr(llama_cpp, 'llama_backend_hipblas'))"
                    ]
                    verify_result2 = subprocess.run(
                        verify_cmd2,
                        capture_output=True,
                        text=True,
                        timeout=10
                    )
                    if verify_result2.returncode == 0 and 'True' in verify_result2.stdout:
                        gpu_verified = True
        
        # Build warning message
        warning_msg = None
        if not gpu_verified and gpu_backend != 'cpu':
            warning_msg = "Installation succeeded but GPU support not detected"
        elif cuda_warning and gpu_backend in ['cuda', 'nvidia', 'cuda12']:
            # Include CUDA detection warning if present
            warning_msg = cuda_warning
        
        return {
            "success": result.returncode == 0,
            "stdout": result.stdout,
            "stderr": result.stderr,
            "gpu_backend": gpu_backend,
            "cmake_args": cmake_args,
            "gpu_verified": gpu_verified,
            "warning": warning_msg
        }
    
    # Class-level cache for toolkit availability (system-wide SDKs don't change often)
    _toolkit_cache: Dict[str, Dict[str, Any]] = {}
    _toolkit_cache_loaded: bool = False
    _all_backends: List[str] = ['cuda', 'rocm', 'vulkan', 'metal', 'openblas', 'cpu']
    
    @classmethod
    def _load_toolkit_cache(cls, base_path: Path):
        """Load system-wide toolkit cache from disk"""
        if cls._toolkit_cache_loaded:
            return
        
        import json
        cache_file = base_path / 'config' / 'toolkit_cache.json'
        if cache_file.exists():
            try:
                with open(cache_file, 'r') as f:
                    cls._toolkit_cache = json.load(f)
            except:
                pass
        cls._toolkit_cache_loaded = True
    
    @classmethod
    def _save_toolkit_cache(cls, base_path: Path):
        """Save system-wide toolkit cache to disk"""
        import json
        cache_file = base_path / 'config' / 'toolkit_cache.json'
        cache_file.parent.mkdir(parents=True, exist_ok=True)
        try:
            with open(cache_file, 'w') as f:
                json.dump(cls._toolkit_cache, f, indent=2)
        except:
            pass
    
    @classmethod
    def clear_toolkit_cache(cls, base_path: Path = None):
        """Clear toolkit cache (call when SDK is installed/uninstalled)"""
        cls._toolkit_cache = {}
        cls._toolkit_cache_loaded = False
        if base_path:
            cache_file = base_path / 'config' / 'toolkit_cache.json'
            if cache_file.exists():
                try:
                    cache_file.unlink()
                except:
                    pass
    
    def detect_all_backends(self, force_refresh: bool = False) -> Dict[str, Dict[str, Any]]:
        """
        Detect all available GPU/compute backends on the system at once.
        This is more efficient than checking each backend individually.
        
        Args:
            force_refresh: Force re-detection even if cached
            
        Returns:
            Dict mapping backend name to availability info
        """
        # Load cache from disk
        EnvironmentManager._load_toolkit_cache(self.base_path)
        
        # If cache has all backends and not forcing refresh, return it
        if not force_refresh and len(EnvironmentManager._toolkit_cache) >= len(EnvironmentManager._all_backends):
            return EnvironmentManager._toolkit_cache.copy()
        
        # Detect all backends
        results = {}
        for backend in EnvironmentManager._all_backends:
            # Skip if already cached and not forcing
            if not force_refresh and backend in EnvironmentManager._toolkit_cache:
                results[backend] = EnvironmentManager._toolkit_cache[backend]
            else:
                results[backend] = self._detect_single_backend(backend)
        
        # Update cache
        EnvironmentManager._toolkit_cache = results
        EnvironmentManager._save_toolkit_cache(self.base_path)
        
        return results
    
    def _detect_single_backend(self, backend: str) -> Dict[str, Any]:
        """Detect a single backend's availability (internal, no caching)"""
        result = {
            "available": False,
            "toolkit_name": "",
            "message": "",
            "install_url": "",
            "details": ""
        }
        
        if backend in ['cuda', 'nvidia', 'cuda12']:
            result["toolkit_name"] = "CUDA Toolkit"
            result["install_url"] = "https://developer.nvidia.com/cuda-downloads"
            
            nvcc_found = False
            cuda_path = None
            
            # Check CUDA_PATH environment variable
            if 'CUDA_PATH' in os.environ:
                cuda_path = os.environ['CUDA_PATH']
                nvcc_path = Path(cuda_path) / 'bin' / ('nvcc.exe' if platform.system() == 'Windows' else 'nvcc')
                if nvcc_path.exists():
                    nvcc_found = True
            
            # Check common Windows paths
            if not nvcc_found and platform.system() == 'Windows':
                common_paths = [
                    Path('C:/Program Files/NVIDIA GPU Computing Toolkit/CUDA'),
                    Path('C:/Program Files (x86)/NVIDIA GPU Computing Toolkit/CUDA'),
                ]
                for base_path in common_paths:
                    if base_path.exists():
                        versions = sorted([d for d in base_path.iterdir() if d.is_dir() and d.name.startswith('v')], reverse=True)
                        if versions:
                            nvcc_path = versions[0] / 'bin' / 'nvcc.exe'
                            if nvcc_path.exists():
                                nvcc_found = True
                                cuda_path = str(versions[0])
                                break
            
            # Check nvcc in PATH
            if not nvcc_found:
                try:
                    subprocess.run(['nvcc', '--version'], capture_output=True, timeout=2)
                    nvcc_found = True
                except (FileNotFoundError, subprocess.TimeoutExpired):
                    pass
            
            # Check nvidia-smi (indicates GPU present)
            nvidia_gpu_present = False
            try:
                subprocess.run(['nvidia-smi'], capture_output=True, timeout=2)
                nvidia_gpu_present = True
            except (FileNotFoundError, subprocess.TimeoutExpired):
                pass
            
            if nvcc_found:
                result["available"] = True
                result["message"] = f"CUDA Toolkit found" + (f" at {cuda_path}" if cuda_path else "")
                result["details"] = "CUDA Toolkit is installed and ready to use."
            elif nvidia_gpu_present:
                result["available"] = False
                result["message"] = "NVIDIA GPU detected but CUDA Toolkit not found"
                result["details"] = "Install CUDA Toolkit from NVIDIA. Python packages will be installed automatically."
            else:
                result["available"] = False
                result["message"] = "CUDA Toolkit not found"
                result["details"] = "Install CUDA Toolkit from NVIDIA to use CUDA acceleration."
                
        elif backend == 'rocm' or backend == 'amd':
            result["toolkit_name"] = "ROCm SDK"
            result["install_url"] = "https://rocm.docs.amd.com/"
            
            rocm_path = os.environ.get('ROCM_PATH', '/opt/rocm')
            rocm_found = False
            
            try:
                subprocess.run(['rocm-smi', '--version'], capture_output=True, timeout=2)
                rocm_found = True
            except (FileNotFoundError, subprocess.TimeoutExpired):
                if Path(rocm_path).exists():
                    rocm_smi = Path(rocm_path) / 'bin' / 'rocm-smi'
                    if rocm_smi.exists():
                        rocm_found = True
            
            if rocm_found:
                result["available"] = True
                result["message"] = f"ROCm SDK found" + (f" at {rocm_path}" if rocm_path != '/opt/rocm' else " (default location)")
                result["details"] = "ROCm SDK is installed and ready to use for AMD GPUs."
            else:
                result["available"] = False
                result["message"] = "ROCm SDK not found"
                result["details"] = "Install ROCm SDK from AMD. On Linux, typically installed to /opt/rocm."
                
        elif backend == 'vulkan':
            result["toolkit_name"] = "Vulkan SDK"
            result["install_url"] = "https://vulkan.lunarg.com/sdk/home"
            
            vulkan_sdk = os.environ.get('VULKAN_SDK')
            vulkan_found = False
            
            try:
                subprocess.run(['vulkaninfo', '--summary'], capture_output=True, timeout=2)
                vulkan_found = True
            except (FileNotFoundError, subprocess.TimeoutExpired):
                if vulkan_sdk and Path(vulkan_sdk).exists():
                    vulkan_found = True
            
            if vulkan_found:
                result["available"] = True
                result["message"] = f"Vulkan SDK found" + (f" at {vulkan_sdk}" if vulkan_sdk else "")
                result["details"] = "Vulkan SDK is installed. Works with NVIDIA, AMD, and Intel GPUs."
            else:
                result["available"] = False
                result["message"] = "Vulkan SDK not found"
                result["details"] = "Install Vulkan SDK from LunarG. Set VULKAN_SDK environment variable after installation."
                
        elif backend == 'metal':
            result["toolkit_name"] = "Metal (Built-in)"
            result["install_url"] = ""
            
            if platform.system() == 'Darwin':
                result["available"] = True
                result["message"] = "Metal is available (built into macOS)"
                result["details"] = "No additional installation needed for Apple Silicon."
            else:
                result["available"] = False
                result["message"] = "Metal not available"
                result["details"] = "Metal is only available on macOS with Apple Silicon."
                
        elif backend == 'openblas':
            result["toolkit_name"] = "OpenBLAS"
            result["install_url"] = "https://www.openblas.net/"
            
            openblas_found = False
            
            # Check common locations
            if platform.system() == 'Windows':
                # On Windows, usually installed via conda or vcpkg
                openblas_paths = [
                    Path(os.environ.get('CONDA_PREFIX', '')) / 'Library' / 'lib' / 'openblas.lib',
                    Path('C:/vcpkg/installed/x64-windows/lib/openblas.lib'),
                ]
                for p in openblas_paths:
                    if p.exists():
                        openblas_found = True
                        break
            else:
                # On Linux/Mac, check for library
                try:
                    result_proc = subprocess.run(['ldconfig', '-p'], capture_output=True, text=True, timeout=2)
                    if 'openblas' in result_proc.stdout.lower():
                        openblas_found = True
                except (FileNotFoundError, subprocess.TimeoutExpired):
                    # Check common paths
                    openblas_paths = [
                        Path('/usr/lib/libopenblas.so'),
                        Path('/usr/lib/x86_64-linux-gnu/libopenblas.so'),
                        Path('/opt/homebrew/lib/libopenblas.dylib'),
                    ]
                    for p in openblas_paths:
                        if p.exists():
                            openblas_found = True
                            break
            
            if openblas_found:
                result["available"] = True
                result["message"] = "OpenBLAS found"
                result["details"] = "OpenBLAS is available for optimized CPU computation."
            else:
                result["available"] = False
                result["message"] = "OpenBLAS not found (optional)"
                result["details"] = "OpenBLAS is optional. CPU backend will use default BLAS."
                
        elif backend == 'cpu':
            result["available"] = True
            result["toolkit_name"] = "CPU (No toolkit required)"
            result["message"] = "CPU backend - always available"
            result["details"] = "CPU backend works without any additional toolkits."
        
        return result
    
    def check_backend_toolkit_availability(self, backend: str, force_refresh: bool = False) -> Dict[str, Any]:
        """
        Check if the required toolkit/SDK is installed for a backend.
        Uses disk-based caching since SDKs are system-wide installations.
        
        Args:
            backend: The backend to check (cuda, rocm, vulkan, metal, cpu)
            force_refresh: Force re-detection even if cached
        
        Returns:
            dict with:
                - available: bool - Whether toolkit is installed
                - toolkit_name: str - Name of required toolkit
                - message: str - Status message
                - install_url: str - URL to download toolkit
                - details: str - Additional details
        """
        # Load cache from disk on first call
        EnvironmentManager._load_toolkit_cache(self.base_path)
        
        # Normalize backend key
        cache_key = backend.lower()
        if cache_key == 'nvidia':
            cache_key = 'cuda'
        elif cache_key == 'amd':
            cache_key = 'rocm'
        
        # Check cache first (system-wide SDKs don't change often)
        if not force_refresh and cache_key in EnvironmentManager._toolkit_cache:
            return EnvironmentManager._toolkit_cache[cache_key]
        
        # Detect this backend
        result = self._detect_single_backend(cache_key)
        
        # Cache the result to disk (system-wide SDKs persist across restarts)
        EnvironmentManager._toolkit_cache[cache_key] = result
        EnvironmentManager._save_toolkit_cache(self.base_path)
        
        return result
    
    def upgrade_package(self, name: str, package: str) -> dict:
        """Upgrade a package in an environment"""
        return self.install_packages(name, ["--upgrade", package])
    
    def export_requirements(self, name: str) -> str:
        """Export requirements.txt for an environment"""
        venv_path = self.providers_path / name
        
        if platform.system() == "Windows":
            python_exe = venv_path / "Scripts" / "python.exe"
        else:
            python_exe = venv_path / "bin" / "python"
        
        if not python_exe.exists():
            raise ValueError(f"Environment '{name}' not found")
        
        result = subprocess.run(
            [str(python_exe), "-m", "pip", "freeze"],
            capture_output=True,
            text=True,
            timeout=60
        )
        
        return result.stdout if result.returncode == 0 else ""
    
    def import_requirements(self, name: str, requirements: str) -> dict:
        """Import requirements into an environment"""
        import tempfile
        
        venv_path = self.providers_path / name
        
        if platform.system() == "Windows":
            python_exe = venv_path / "Scripts" / "python.exe"
        else:
            python_exe = venv_path / "bin" / "python"
        
        if not python_exe.exists():
            raise ValueError(f"Environment '{name}' not found")
        
        # Write requirements to temp file
        with tempfile.NamedTemporaryFile(mode='w', suffix='.txt', delete=False) as f:
            f.write(requirements)
            req_file = f.name
        
        try:
            result = subprocess.run(
                [str(python_exe), "-m", "pip", "install", "-r", req_file],
                capture_output=True,
                text=True,
                timeout=600
            )
            
            return {
                "success": result.returncode == 0,
                "stdout": result.stdout,
                "stderr": result.stderr
            }
        finally:
            os.unlink(req_file)
    
    def to_dict_list(self, environments: List[VirtualEnvironment]) -> List[dict]:
        """Convert environments to dictionary list"""
        return [asdict(e) for e in environments]
