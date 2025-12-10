"""
Hardware Acceleration Service

Provides detection and management of hardware acceleration options:
- CUDA (NVIDIA GPUs)
- ROCm (AMD GPUs)
- Vulkan (Cross-platform GPU)
- Metal (Apple Silicon)
- CPU (Fallback)

Also handles llama-cpp-python installation with appropriate backend.
"""
import os
import sys
import json
import shutil
import platform
import subprocess
import threading
import multiprocessing
from pathlib import Path
from typing import Optional, List, Dict, Any, Tuple, Callable
from dataclasses import dataclass, field, asdict
from enum import Enum

# CRITICAL: Initialize freeze_support for subprocess calls in frozen apps
if getattr(sys, 'frozen', False):
    multiprocessing.freeze_support()


class AccelerationType(Enum):
    """Hardware acceleration types"""
    CPU = "cpu"
    CUDA = "cuda"
    ROCM = "rocm"
    VULKAN = "vulkan"
    METAL = "metal"
    OPENBLAS = "openblas"
    CLBLAST = "clblast"


@dataclass
class GPUDevice:
    """Represents a detected GPU device"""
    index: int
    name: str
    vendor: str  # nvidia, amd, intel, apple
    memory_total: int  # bytes
    memory_free: int = 0
    driver_version: str = ""
    compute_capability: str = ""  # For CUDA
    is_available: bool = True
    
    def to_dict(self) -> dict:
        return {
            **asdict(self),
            'memory_total_gb': round(self.memory_total / (1024**3), 2),
            'memory_free_gb': round(self.memory_free / (1024**3), 2)
        }


@dataclass
class AccelerationBackend:
    """Represents an acceleration backend option"""
    type: str
    name: str
    description: str
    available: bool
    install_command: str
    cmake_args: str
    devices: List[GPUDevice] = field(default_factory=list)
    recommended: bool = False
    installed: bool = False
    
    def to_dict(self) -> dict:
        d = asdict(self)
        d['devices'] = [dev.to_dict() for dev in self.devices]
        return d


@dataclass
class HardwareProfile:
    """Complete hardware profile"""
    os_name: str
    os_version: str
    architecture: str
    cpu_name: str
    cpu_cores: int
    ram_total: int
    ram_available: int
    backends: List[AccelerationBackend] = field(default_factory=list)
    active_backend: Optional[str] = None
    llama_cpp_installed: bool = False
    llama_cpp_version: str = ""
    
    def to_dict(self) -> dict:
        return {
            **asdict(self),
            'backends': [b.to_dict() for b in self.backends],
            'ram_total_gb': round(self.ram_total / (1024**3), 2),
            'ram_available_gb': round(self.ram_available / (1024**3), 2)
        }


class HardwareService:
    """Service for hardware detection and acceleration management"""
    _instance = None
    _lock = threading.Lock()
    
    def __new__(cls):
        if cls._instance is None:
            with cls._lock:
                if cls._instance is None:
                    cls._instance = super().__new__(cls)
                    cls._instance._initialized = False
        return cls._instance
    
    def __init__(self):
        if self._initialized:
            return
        
        self._initialized = True
        # Use app's own folder - no fallback to user home
        from app.config_manager import get_app_directory
        self.base_path = get_app_directory()
        self.config_path = self.base_path / 'config'
        self.config_path.mkdir(parents=True, exist_ok=True)
        
        self._hardware_profile: Optional[HardwareProfile] = None
        self._active_backend: Optional[str] = None
        
        # Load saved configuration
        self._load_config()
    
    def _load_config(self):
        """Load saved hardware configuration and cached hardware profile"""
        config_file = self.config_path / 'hardware_config.json'
        if config_file.exists():
            try:
                with open(config_file, 'r') as f:
                    config = json.load(f)
                    self._active_backend = config.get('active_backend')
                    # Load cached hardware profile if available
                    if 'hardware_profile' in config:
                        try:
                            profile_data = config['hardware_profile']
                            self._hardware_profile = HardwareProfile(**profile_data)
                        except:
                            pass  # If cached profile is invalid, will re-detect
            except:
                pass
    
    def _save_config(self):
        """Save hardware configuration and cache hardware profile"""
        config_file = self.config_path / 'hardware_config.json'
        config_data = {
            'active_backend': self._active_backend
        }
        # Cache hardware profile if available
        if self._hardware_profile:
            try:
                config_data['hardware_profile'] = self._hardware_profile.to_dict()
            except:
                pass
        with open(config_file, 'w') as f:
            json.dump(config_data, f, indent=2)
    
    def detect_hardware(self, force_refresh: bool = False) -> HardwareProfile:
        """Detect all available hardware and acceleration options"""
        if self._hardware_profile and not force_refresh:
            return self._hardware_profile
        
        import psutil
        
        # Basic system info
        profile = HardwareProfile(
            os_name=platform.system(),
            os_version=platform.version(),
            architecture=platform.machine(),
            cpu_name=self._get_cpu_name(),
            cpu_cores=psutil.cpu_count(logical=True),
            ram_total=psutil.virtual_memory().total,
            ram_available=psutil.virtual_memory().available,
            backends=[],
            active_backend=self._active_backend
        )
        
        # Detect available backends
        profile.backends = self._detect_backends()
        
        # Check llama-cpp-python installation
        profile.llama_cpp_installed, profile.llama_cpp_version = self._check_llama_cpp()
        
        self._hardware_profile = profile
        return profile
    
    def _get_cpu_name(self) -> str:
        """Get CPU name"""
        try:
            if platform.system() == "Windows":
                import winreg
                key = winreg.OpenKey(winreg.HKEY_LOCAL_MACHINE, 
                                     r"HARDWARE\DESCRIPTION\System\CentralProcessor\0")
                return winreg.QueryValueEx(key, "ProcessorNameString")[0].strip()
            elif platform.system() == "Linux":
                with open("/proc/cpuinfo") as f:
                    for line in f:
                        if "model name" in line:
                            return line.split(":")[1].strip()
            elif platform.system() == "Darwin":
                result = subprocess.run(["sysctl", "-n", "machdep.cpu.brand_string"],
                                       capture_output=True, text=True)
                return result.stdout.strip()
        except:
            pass
        return platform.processor() or "Unknown CPU"
    
    def _detect_backends(self) -> List[AccelerationBackend]:
        """Detect all available acceleration backends"""
        backends = []
        
        # CPU (always available)
        backends.append(AccelerationBackend(
            type="cpu",
            name="CPU",
            description="Run on CPU (slowest, but always works)",
            available=True,
            install_command="pip install llama-cpp-python",
            cmake_args="",
            recommended=False,
            installed=self._check_backend_installed("cpu")
        ))
        
        # OpenBLAS (CPU optimized)
        backends.append(AccelerationBackend(
            type="openblas",
            name="OpenBLAS",
            description="CPU with OpenBLAS optimization (faster CPU inference)",
            available=True,
            install_command='CMAKE_ARGS="-DLLAMA_BLAS=ON -DLLAMA_BLAS_VENDOR=OpenBLAS" pip install llama-cpp-python --force-reinstall --no-cache-dir',
            cmake_args="-DLLAMA_BLAS=ON -DLLAMA_BLAS_VENDOR=OpenBLAS",
            recommended=False,
            installed=False
        ))
        
        # CUDA (NVIDIA)
        cuda_available, cuda_devices = self._detect_cuda()
        backends.append(AccelerationBackend(
            type="cuda",
            name="CUDA (NVIDIA)",
            description="NVIDIA GPU acceleration (fastest for NVIDIA GPUs)",
            available=cuda_available,
            install_command='CMAKE_ARGS="-DLLAMA_CUBLAS=on" pip install llama-cpp-python --force-reinstall --no-cache-dir',
            cmake_args="-DLLAMA_CUBLAS=on",
            devices=cuda_devices,
            recommended=cuda_available,
            installed=self._check_backend_installed("cuda")
        ))
        
        # ROCm (AMD)
        rocm_available, rocm_devices = self._detect_rocm()
        backends.append(AccelerationBackend(
            type="rocm",
            name="ROCm (AMD)",
            description="AMD GPU acceleration via ROCm/HIP",
            available=rocm_available,
            install_command='CMAKE_ARGS="-DLLAMA_HIPBLAS=on" pip install llama-cpp-python --force-reinstall --no-cache-dir',
            cmake_args="-DLLAMA_HIPBLAS=on",
            devices=rocm_devices,
            recommended=rocm_available and not cuda_available,
            installed=self._check_backend_installed("rocm")
        ))
        
        # Vulkan (cross-platform)
        vulkan_available, vulkan_devices = self._detect_vulkan()
        backends.append(AccelerationBackend(
            type="vulkan",
            name="Vulkan",
            description="Cross-platform GPU acceleration (works with most GPUs)",
            available=vulkan_available,
            install_command='CMAKE_ARGS="-DLLAMA_VULKAN=on" pip install llama-cpp-python --force-reinstall --no-cache-dir',
            cmake_args="-DLLAMA_VULKAN=on",
            devices=vulkan_devices,
            recommended=vulkan_available and not cuda_available and not rocm_available,
            installed=self._check_backend_installed("vulkan")
        ))
        
        # Metal (Apple Silicon)
        if platform.system() == "Darwin":
            metal_available = self._detect_metal()
            backends.append(AccelerationBackend(
                type="metal",
                name="Metal (Apple)",
                description="Apple Silicon GPU acceleration",
                available=metal_available,
                install_command='CMAKE_ARGS="-DLLAMA_METAL=on" pip install llama-cpp-python --force-reinstall --no-cache-dir',
                cmake_args="-DLLAMA_METAL=on",
                devices=[GPUDevice(
                    index=0,
                    name="Apple Silicon GPU",
                    vendor="apple",
                    memory_total=0,  # Shared memory
                    is_available=metal_available
                )] if metal_available else [],
                recommended=metal_available,
                installed=self._check_backend_installed("metal")
            ))
        
        # CLBlast (OpenCL)
        backends.append(AccelerationBackend(
            type="clblast",
            name="CLBlast (OpenCL)",
            description="OpenCL GPU acceleration (broader GPU support)",
            available=self._detect_opencl(),
            install_command='CMAKE_ARGS="-DLLAMA_CLBLAST=on" pip install llama-cpp-python --force-reinstall --no-cache-dir',
            cmake_args="-DLLAMA_CLBLAST=on",
            recommended=False,
            installed=False
        ))
        
        return backends
    
    def _detect_cuda(self) -> Tuple[bool, List[GPUDevice]]:
        """Detect NVIDIA CUDA GPUs"""
        devices = []
        
        # Method 1: Try nvidia-smi
        try:
            result = subprocess.run(
                ["nvidia-smi", "--query-gpu=index,name,memory.total,memory.free,driver_version,compute_cap", 
                 "--format=csv,noheader,nounits"],
                capture_output=True, text=True, timeout=10
            )
            
            if result.returncode == 0:
                for line in result.stdout.strip().split('\n'):
                    if line.strip():
                        parts = [p.strip() for p in line.split(',')]
                        if len(parts) >= 6:
                            devices.append(GPUDevice(
                                index=int(parts[0]),
                                name=parts[1],
                                vendor="nvidia",
                                memory_total=int(float(parts[2])) * 1024 * 1024,
                                memory_free=int(float(parts[3])) * 1024 * 1024,
                                driver_version=parts[4],
                                compute_capability=parts[5]
                            ))
        except:
            pass
        
        # Method 2: Try torch.cuda
        if not devices:
            try:
                import torch
                if torch.cuda.is_available():
                    for i in range(torch.cuda.device_count()):
                        props = torch.cuda.get_device_properties(i)
                        devices.append(GPUDevice(
                            index=i,
                            name=props.name,
                            vendor="nvidia",
                            memory_total=props.total_memory,
                            compute_capability=f"{props.major}.{props.minor}"
                        ))
            except:
                pass
        
        return len(devices) > 0, devices
    
    def _detect_rocm(self) -> Tuple[bool, List[GPUDevice]]:
        """Detect AMD ROCm GPUs"""
        devices = []
        
        # Try rocm-smi
        try:
            result = subprocess.run(
                ["rocm-smi", "--showproductname", "--showmeminfo", "vram", "--json"],
                capture_output=True, text=True, timeout=10
            )
            
            if result.returncode == 0:
                data = json.loads(result.stdout)
                for card_id, info in data.items():
                    if card_id.startswith("card"):
                        devices.append(GPUDevice(
                            index=int(card_id.replace("card", "")),
                            name=info.get("Product Name", "AMD GPU"),
                            vendor="amd",
                            memory_total=int(info.get("VRAM Total Memory (B)", 0)),
                            memory_free=int(info.get("VRAM Total Used Memory (B)", 0))
                        ))
        except:
            pass
        
        # Fallback: Check for HIP
        if not devices:
            try:
                result = subprocess.run(["hipconfig", "--platform"], 
                                       capture_output=True, text=True, timeout=5)
                if result.returncode == 0 and "amd" in result.stdout.lower():
                    devices.append(GPUDevice(
                        index=0,
                        name="AMD GPU (ROCm)",
                        vendor="amd",
                        memory_total=0
                    ))
            except:
                pass
        
        return len(devices) > 0, devices
    
    def _detect_vulkan(self) -> Tuple[bool, List[GPUDevice]]:
        """Detect Vulkan-capable GPUs"""
        devices = []
        
        # Try vulkaninfo
        try:
            result = subprocess.run(
                ["vulkaninfo", "--summary"],
                capture_output=True, text=True, timeout=10
            )
            
            if result.returncode == 0:
                # Parse vulkaninfo output
                lines = result.stdout.split('\n')
                current_device = None
                
                for line in lines:
                    if "deviceName" in line:
                        name = line.split('=')[1].strip() if '=' in line else "Vulkan GPU"
                        vendor = "unknown"
                        if "nvidia" in name.lower():
                            vendor = "nvidia"
                        elif "amd" in name.lower() or "radeon" in name.lower():
                            vendor = "amd"
                        elif "intel" in name.lower():
                            vendor = "intel"
                        
                        devices.append(GPUDevice(
                            index=len(devices),
                            name=name,
                            vendor=vendor,
                            memory_total=0
                        ))
        except:
            pass
        
        # Fallback: Check if Vulkan SDK is installed
        if not devices:
            vulkan_sdk = os.environ.get("VULKAN_SDK")
            if vulkan_sdk and Path(vulkan_sdk).exists():
                devices.append(GPUDevice(
                    index=0,
                    name="Vulkan GPU (SDK detected)",
                    vendor="unknown",
                    memory_total=0
                ))
        
        return len(devices) > 0, devices
    
    def _detect_metal(self) -> bool:
        """Detect Apple Metal support"""
        if platform.system() != "Darwin":
            return False
        
        try:
            # Check for Metal support
            result = subprocess.run(
                ["system_profiler", "SPDisplaysDataType"],
                capture_output=True, text=True, timeout=10
            )
            return "Metal" in result.stdout
        except:
            pass
        
        # Apple Silicon always has Metal
        return platform.machine() == "arm64"
    
    def _detect_opencl(self) -> bool:
        """Detect OpenCL support"""
        try:
            import pyopencl
            platforms = pyopencl.get_platforms()
            return len(platforms) > 0
        except:
            pass
        
        # Check for OpenCL libraries
        if platform.system() == "Windows":
            return Path("C:/Windows/System32/OpenCL.dll").exists()
        elif platform.system() == "Linux":
            return Path("/usr/lib/libOpenCL.so").exists() or Path("/usr/lib/x86_64-linux-gnu/libOpenCL.so").exists()
        
        return False
    
    def _check_llama_cpp(self) -> Tuple[bool, str]:
        """Check if llama-cpp-python is installed and get version"""
        try:
            result = subprocess.run(
                [sys.executable, "-c", "import llama_cpp; print(llama_cpp.__version__)"],
                capture_output=True, text=True, timeout=10
            )
            if result.returncode == 0:
                return True, result.stdout.strip()
        except:
            pass
        return False, ""
    
    def _check_backend_installed(self, backend_type: str) -> bool:
        """Check if a specific backend is installed"""
        # This is a simplified check - in reality, we'd need to check 
        # which backend llama-cpp-python was compiled with
        if not self._check_llama_cpp()[0]:
            return False
        
        # For now, just mark CPU as always installed if llama-cpp is present
        if backend_type == "cpu":
            return True
        
        # Check based on active backend config
        return self._active_backend == backend_type
    
    def get_active_backend(self) -> Optional[str]:
        """Get the currently active backend"""
        return self._active_backend
    
    def set_active_backend(self, backend_type: str):
        """Set the active backend"""
        self._active_backend = backend_type
        self._save_config()
    
    def install_llama_cpp(self, backend_type: str = "cpu", 
                          venv_path: Optional[str] = None,
                          progress_callback: Optional[Callable[[int, str], None]] = None) -> Tuple[bool, str]:
        """
        Install llama-cpp-python with specified backend
        
        Args:
            backend_type: The backend to compile with (cpu, cuda, rocm, vulkan, metal, etc.)
            venv_path: Path to virtual environment. If None, installs to current Python.
            progress_callback: Optional callback(percent, message) for progress updates
        
        Returns:
            Tuple of (success, message)
        """
        import subprocess
        
        def update_progress(pct: int, msg: str):
            if progress_callback:
                progress_callback(pct, msg)
        
        update_progress(5, "Preparing installation...")
        
        # Determine pip executable
        if venv_path:
            venv_path = Path(venv_path)
            if platform.system() == "Windows":
                pip_exe = venv_path / "Scripts" / "pip.exe"
                python_exe = venv_path / "Scripts" / "python.exe"
            else:
                pip_exe = venv_path / "bin" / "pip"
                python_exe = venv_path / "bin" / "python"
            
            if not pip_exe.exists():
                return False, f"Virtual environment pip not found: {pip_exe}"
        else:
            pip_exe = Path(sys.executable).parent / ("pip.exe" if platform.system() == "Windows" else "pip")
            python_exe = sys.executable
        
        update_progress(10, "Uninstalling existing llama-cpp-python...")
        
        # Uninstall existing version
        try:
            subprocess.run(
                [str(pip_exe), "uninstall", "llama-cpp-python", "-y"],
                capture_output=True, timeout=60
            )
        except:
            pass  # Ignore if not installed
        
        update_progress(20, f"Configuring {backend_type} backend...")
        
        # Get CMAKE_ARGS for the backend
        cmake_args = self._get_cmake_args(backend_type)
        
        # Prepare environment
        env = os.environ.copy()
        if cmake_args:
            env["CMAKE_ARGS"] = cmake_args
        
        update_progress(30, f"Installing llama-cpp-python with {backend_type} support...")
        update_progress(35, "This may take several minutes...")
        
        # Install llama-cpp-python
        try:
            result = subprocess.run(
                [str(pip_exe), "install", "llama-cpp-python", "--force-reinstall", "--no-cache-dir"],
                capture_output=True,
                text=True,
                env=env,
                timeout=1800  # 30 minutes timeout for compilation
            )
            
            if result.returncode == 0:
                update_progress(95, "Verifying installation...")
                
                # Verify installation
                verify = subprocess.run(
                    [str(python_exe), "-c", "import llama_cpp; print(llama_cpp.__version__)"],
                    capture_output=True, text=True, timeout=30
                )
                
                if verify.returncode == 0:
                    version = verify.stdout.strip()
                    self.set_active_backend(backend_type)
                    update_progress(100, f"Successfully installed llama-cpp-python {version}")
                    return True, f"Installed llama-cpp-python {version} with {backend_type} backend"
                else:
                    return False, f"Installation completed but import failed: {verify.stderr}"
            else:
                return False, f"Installation failed: {result.stderr[-500:]}"
                
        except subprocess.TimeoutExpired:
            return False, "Installation timed out after 30 minutes"
        except Exception as e:
            return False, f"Installation error: {str(e)}"
    
    def _get_cmake_args(self, backend_type: str) -> str:
        """Get CMAKE_ARGS for a backend type"""
        cmake_args_map = {
            "cpu": "",
            "cuda": "-DGGML_CUDA=on",  # Use GGML_CUDA (newer llama.cpp versions)
            "rocm": "-DLLAMA_HIPBLAS=on",
            "vulkan": "-DLLAMA_VULKAN=on",
            "metal": "-DLLAMA_METAL=on",
            "openblas": "-DLLAMA_BLAS=ON -DLLAMA_BLAS_VENDOR=OpenBLAS",
            "clblast": "-DLLAMA_CLBLAST=on"
        }
        return cmake_args_map.get(backend_type, "")
    
    def ensure_llama_cpp_installed(self, venv_path: Optional[str] = None,
                                    progress_callback: Optional[Callable[[int, str], None]] = None) -> Tuple[bool, str]:
        """
        Ensure llama-cpp-python is installed, auto-installing if needed
        
        Uses the recommended backend for the current system.
        """
        # Check if already installed
        installed, version = self._check_llama_cpp()
        if installed:
            return True, f"Already installed: {version}"
        
        # Get recommended backend
        recommended = self.get_recommended_backend()
        
        # Install with recommended backend
        return self.install_llama_cpp(recommended, venv_path, progress_callback)
    
    def get_available_venvs(self) -> List[Dict[str, Any]]:
        """Get list of available virtual environments for installation"""
        venvs = []
        seen_paths = set()
        
        def add_venv(venv_path: Path, source: str = "", is_current: bool = False):
            """Helper to add a venv if valid"""
            path_str = str(venv_path.resolve())
            if path_str in seen_paths:
                return
            
            if (venv_path / "Scripts" / "python.exe").exists() or (venv_path / "bin" / "python").exists():
                seen_paths.add(path_str)
                name = venv_path.name
                if source:
                    name = f"{source}/{name}" if name != source else source
                
                venvs.append({
                    'path': path_str,
                    'name': name,
                    'source': source,
                    'is_current': is_current,
                    'has_llama_cpp': self._check_llama_cpp_in_venv(venv_path)
                })
        
        # 1. Check current project's .venv (highest priority)
        # Get the app's directory
        from app.config_manager import get_app_directory
        app_dir = get_app_directory()
        project_venv = app_dir / ".venv"
        if project_venv.exists():
            add_venv(project_venv, "Project", is_current=True)
        
        # Also check parent directories for .venv
        for parent in [app_dir.parent, app_dir.parent.parent]:
            for venv_name in [".venv", "venv"]:
                candidate = parent / venv_name
                if candidate.exists():
                    add_venv(candidate, f"{parent.name}")
        
        # 2. Check BEEP_PYTHON managed environments
        beep_envs = self.base_path / "environments"
        if beep_envs.exists():
            for subdir in beep_envs.iterdir():
                if subdir.is_dir():
                    add_venv(subdir, "Managed")
        
        # 3. Check app's environments directory (no longer looking in user home)
        # All environments should be in app folder
        app_envs = app_dir / "environments"
        if app_envs.exists() and app_envs.is_dir():
            for subdir in app_envs.iterdir():
                if subdir.is_dir():
                    add_venv(subdir, "App Environments")
        
        # 4. Check working directory
        cwd = Path.cwd()
        for venv_name in [".venv", "venv"]:
            candidate = cwd / venv_name
            if candidate.exists():
                add_venv(candidate, "Working Dir")
        
        # Sort: current first, then by name
        venvs.sort(key=lambda x: (not x['is_current'], x['name'].lower()))
        
        return venvs
        
        return venvs
    
    def _check_llama_cpp_in_venv(self, venv_path: Path) -> bool:
        """Check if llama-cpp-python is installed in a virtual environment"""
        if platform.system() == "Windows":
            python_exe = venv_path / "Scripts" / "python.exe"
        else:
            python_exe = venv_path / "bin" / "python"
        
        if not python_exe.exists():
            return False
        
        try:
            result = subprocess.run(
                [str(python_exe), "-c", "import llama_cpp"],
                capture_output=True, timeout=10
            )
            return result.returncode == 0
        except:
            return False
    
    def get_install_command(self, backend_type: str) -> Optional[str]:
        """Get the installation command for a backend"""
        profile = self.detect_hardware()
        for backend in profile.backends:
            if backend.type == backend_type:
                return backend.install_command
        return None
    
    def get_recommended_backend(self) -> Optional[str]:
        """Get the recommended backend for this system"""
        profile = self.detect_hardware()
        for backend in profile.backends:
            if backend.recommended and backend.available:
                return backend.type
        return "cpu"
    
    def get_gpu_layers_recommendation(self, model_size_gb: float, backend_type: str) -> int:
        """Get recommended GPU layers based on model size and available VRAM"""
        profile = self.detect_hardware()
        
        for backend in profile.backends:
            if backend.type == backend_type and backend.devices:
                # Get total available VRAM
                total_vram = sum(d.memory_free or d.memory_total for d in backend.devices)
                total_vram_gb = total_vram / (1024**3)
                
                if total_vram_gb <= 0:
                    return 0
                
                # Rough estimation: each layer uses about model_size/32 of VRAM
                # Leave some headroom for KV cache
                available_for_model = total_vram_gb * 0.8
                
                if available_for_model >= model_size_gb:
                    return -1  # All layers on GPU
                else:
                    # Partial offload
                    ratio = available_for_model / model_size_gb
                    return max(1, int(32 * ratio))  # Assuming ~32 layers average
        
        return 0  # CPU only
    
    def get_inference_settings_for_backend(self, backend_type: str) -> Dict[str, Any]:
        """Get recommended inference settings for a backend"""
        profile = self.detect_hardware()
        
        settings = {
            'n_gpu_layers': 0,
            'n_threads': max(1, profile.cpu_cores - 1),
            'n_batch': 512,
            'use_mmap': True,
            'use_mlock': False
        }
        
        if backend_type == "cpu" or backend_type == "openblas":
            settings['n_threads'] = max(1, profile.cpu_cores - 1)
            settings['n_batch'] = 512
        elif backend_type in ["cuda", "rocm", "vulkan", "metal", "hip", "sycl"] or \
             backend_type.startswith("cuda-") or backend_type.startswith("hip-"):
            settings['n_gpu_layers'] = -1  # All layers on GPU
            settings['n_threads'] = 4  # Less CPU threads needed
            settings['n_batch'] = 1024  # Larger batches on GPU
        
        return settings


# Helper function to get installation scripts
def get_backend_install_script(backend_type: str, use_venv: bool = True) -> str:
    """Generate a complete installation script for a backend"""
    
    backends = {
        "cpu": {
            "windows": "pip install llama-cpp-python",
            "linux": "pip install llama-cpp-python",
            "darwin": "pip install llama-cpp-python"
        },
        "cuda": {
            "windows": '''$env:CMAKE_ARGS="-DGGML_CUDA=on"
pip install llama-cpp-python --force-reinstall --no-cache-dir''',
            "linux": '''CMAKE_ARGS="-DGGML_CUDA=on" pip install llama-cpp-python --force-reinstall --no-cache-dir''',
            "darwin": "# CUDA not supported on macOS"
        },
        "rocm": {
            "windows": "# ROCm not officially supported on Windows",
            "linux": '''CMAKE_ARGS="-DLLAMA_HIPBLAS=on" pip install llama-cpp-python --force-reinstall --no-cache-dir''',
            "darwin": "# ROCm not supported on macOS"
        },
        "vulkan": {
            "windows": '''$env:CMAKE_ARGS="-DLLAMA_VULKAN=on"
pip install llama-cpp-python --force-reinstall --no-cache-dir''',
            "linux": '''CMAKE_ARGS="-DLLAMA_VULKAN=on" pip install llama-cpp-python --force-reinstall --no-cache-dir''',
            "darwin": '''CMAKE_ARGS="-DLLAMA_VULKAN=on" pip install llama-cpp-python --force-reinstall --no-cache-dir'''
        },
        "metal": {
            "windows": "# Metal not supported on Windows",
            "linux": "# Metal not supported on Linux",
            "darwin": '''CMAKE_ARGS="-DLLAMA_METAL=on" pip install llama-cpp-python --force-reinstall --no-cache-dir'''
        },
        "openblas": {
            "windows": '''$env:CMAKE_ARGS="-DLLAMA_BLAS=ON -DLLAMA_BLAS_VENDOR=OpenBLAS"
pip install llama-cpp-python --force-reinstall --no-cache-dir''',
            "linux": '''CMAKE_ARGS="-DLLAMA_BLAS=ON -DLLAMA_BLAS_VENDOR=OpenBLAS" pip install llama-cpp-python --force-reinstall --no-cache-dir''',
            "darwin": '''CMAKE_ARGS="-DLLAMA_BLAS=ON -DLLAMA_BLAS_VENDOR=OpenBLAS" pip install llama-cpp-python --force-reinstall --no-cache-dir'''
        },
        "clblast": {
            "windows": '''$env:CMAKE_ARGS="-DLLAMA_CLBLAST=on"
pip install llama-cpp-python --force-reinstall --no-cache-dir''',
            "linux": '''CMAKE_ARGS="-DLLAMA_CLBLAST=on" pip install llama-cpp-python --force-reinstall --no-cache-dir''',
            "darwin": '''CMAKE_ARGS="-DLLAMA_CLBLAST=on" pip install llama-cpp-python --force-reinstall --no-cache-dir'''
        }
    }
    
    os_key = platform.system().lower()
    if backend_type in backends and os_key in backends[backend_type]:
        return backends[backend_type][os_key]
    
    return f"# Unknown backend: {backend_type}"
