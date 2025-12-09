"""
Backend Extension Service

Manages llama.cpp backend engines and frameworks similar to LM Studio.
Tracks installations, versions, and compatibility.
"""
import os
import platform
import subprocess
import json
from pathlib import Path
from typing import List, Dict, Any, Optional, Tuple
from dataclasses import dataclass, asdict
from enum import Enum

from app.services.environment_manager import EnvironmentManager
from app.services.hardware_service import HardwareService


class ExtensionType(Enum):
    """Extension type"""
    ENGINE = "Engine"
    FRAMEWORK = "Framework"


@dataclass
class BackendExtension:
    """Represents a backend extension (engine or framework)"""
    id: str
    name: str
    type: str  # "Engine" or "Framework"
    version: str
    description: str
    backend: str  # "cuda", "vulkan", "rocm", "metal", "cpu", "cuda12"
    platform: str  # "Windows", "Linux", "Darwin", "All"
    compatible: bool  # Is compatible with current system
    installed: bool  # Is installed in any environment
    installed_version: Optional[str] = None
    installed_environments: List[str] = None  # List of venv names where it's installed
    release_notes: Optional[str] = None
    
    def __post_init__(self):
        if self.installed_environments is None:
            self.installed_environments = []
    
    def to_dict(self) -> dict:
        return {
            **asdict(self),
            'is_latest': self.installed and self.installed_version == self.version
        }


class BackendExtensionService:
    """Service for managing backend extensions"""
    
    # Latest llama-cpp-python version (update as needed)
    # Format matches llama-cpp-python versioning (e.g., "0.2.0")
    LATEST_VERSION = "0.2.0"  # Update this to match latest llama-cpp-python version
    
    def __init__(self):
        self.base_path = Path(os.environ.get('BEEP_PYTHON_HOME', 
                                             os.path.expanduser('~/.beep-llm')))
        self.env_mgr = EnvironmentManager(base_path=str(self.base_path))
        self.hardware_service = HardwareService()
        self._platform = self._get_platform_name()
        
        # Pre-populate toolkit cache on first access (runs once, then cached to disk)
        self._ensure_toolkit_cache()
    
    def _ensure_toolkit_cache(self):
        """Ensure toolkit cache is populated (runs full detection if needed)"""
        # This will detect all backends and cache to disk if not already cached
        self.env_mgr.detect_all_backends(force_refresh=False)
    
    def _get_platform_name(self) -> str:
        """Get platform name for filtering"""
        system = platform.system()
        if system == "Windows":
            return "Windows"
        elif system == "Linux":
            return "Linux"
        elif system == "Darwin":
            return "Darwin"
        return "All"
    
    def get_available_extensions(self) -> List[BackendExtension]:
        """Get all available backend extensions"""
        extensions = []
        
        # Get toolkit availability from shared cache (fast, disk-cached)
        toolkit_status = self.env_mgr.detect_all_backends(force_refresh=False)
        
        # Map toolkit availability to backend flags
        cuda_available = toolkit_status.get('cuda', {}).get('available', False)
        vulkan_available = toolkit_status.get('vulkan', {}).get('available', False)
        rocm_available = toolkit_status.get('rocm', {}).get('available', False)
        metal_available = toolkit_status.get('metal', {}).get('available', False)
        openblas_available = toolkit_status.get('openblas', {}).get('available', False)
        
        # Get installed extensions
        installed_exts = self._get_installed_extensions()
        
        # Define available engines
        engines = [
            {
                "id": "llama-cpp-cpu",
                "name": "CPU llama.cpp",
                "backend": "cpu",
                "description": "CPU-only llama.cpp engine",
                "platform": "All",
                "compatible": True  # Always compatible
            },
            {
                "id": "llama-cpp-openblas",
                "name": "OpenBLAS llama.cpp",
                "backend": "openblas",
                "description": "CPU with OpenBLAS optimization (faster CPU inference)",
                "platform": "All",
                "compatible": openblas_available
            },
            {
                "id": "llama-cpp-cuda",
                "name": "CUDA llama.cpp",
                "backend": "cuda",
                "description": "Nvidia CUDA accelerated llama.cpp engine",
                "platform": "All",  # Windows & Linux
                "compatible": cuda_available
            },
            {
                "id": "llama-cpp-cuda12",
                "name": "CUDA 12 llama.cpp",
                "backend": "cuda12",
                "description": "Nvidia CUDA 12.x accelerated llama.cpp engine",
                "platform": "All",  # Windows & Linux
                "compatible": cuda_available
            },
            {
                "id": "llama-cpp-vulkan",
                "name": "Vulkan llama.cpp",
                "backend": "vulkan",
                "description": "Vulkan accelerated llama.cpp engine (works with most GPUs)",
                "platform": "All",  # Cross-platform
                "compatible": vulkan_available
            },
            {
                "id": "llama-cpp-rocm",
                "name": "ROCm llama.cpp",
                "backend": "rocm",
                "description": "AMD ROCm accelerated llama.cpp engine",
                "platform": "Linux",
                "compatible": rocm_available
            },
            {
                "id": "llama-cpp-metal",
                "name": "Metal llama.cpp",
                "backend": "metal",
                "description": "Apple Metal accelerated llama.cpp engine",
                "platform": "Darwin",
                "compatible": metal_available
            }
        ]
        
        # Create extension objects
        for engine in engines:
            # Check if platform matches
            if engine["platform"] != "All" and engine["platform"] != self._platform:
                continue
            
            ext_id = engine["id"]
            installed_info = installed_exts.get(ext_id, {})
            
            extension = BackendExtension(
                id=ext_id,
                name=f"{engine['name']} ({self._platform})",
                type=ExtensionType.ENGINE.value,
                version=self.LATEST_VERSION,
                description=engine["description"],
                backend=engine["backend"],
                platform=engine["platform"],
                compatible=engine["compatible"],
                installed=installed_info.get("installed", False),
                installed_version=installed_info.get("version"),
                installed_environments=installed_info.get("environments", [])
            )
            extensions.append(extension)
        
        # Add frameworks (can be added later)
        # frameworks = [...]
        
        return extensions
    
    def _get_installed_extensions(self) -> Dict[str, Dict[str, Any]]:
        """Check which extensions are installed in environments"""
        installed = {}
        
        # Get all environments
        environments = self.env_mgr.list_environments()
        
        # Check each environment for llama-cpp-python
        for env in environments:
            if platform.system() == "Windows":
                python_exe = Path(env.python_executable)
            else:
                python_exe = Path(env.python_executable)
            
            if not python_exe.exists():
                continue
            
            # Check for llama-cpp-python
            has_llama, version, backend = self._check_llama_cpp(python_exe)
            
            if has_llama and backend:
                # Map backend to extension ID
                ext_id = self._backend_to_extension_id(backend)
                
                if ext_id:
                    if ext_id not in installed:
                        installed[ext_id] = {
                            "installed": True,
                            "version": version,
                            "environments": []
                        }
                    
                    installed[ext_id]["environments"].append(env.name)
        
        return installed
    
    def _backend_to_extension_id(self, backend: str) -> Optional[str]:
        """Map backend name to extension ID"""
        mapping = {
            "cpu": "llama-cpp-cpu",
            "cuda": "llama-cpp-cuda",
            "cuda12": "llama-cpp-cuda12",
            "vulkan": "llama-cpp-vulkan",
            "rocm": "llama-cpp-rocm",
            "metal": "llama-cpp-metal"
        }
        return mapping.get(backend.lower())
    
    def _check_llama_cpp(self, python_executable: Path) -> Tuple[bool, Optional[str], Optional[str]]:
        """Check if llama-cpp-python is installed and detect backend"""
        try:
            # Check version
            result = subprocess.run(
                [str(python_executable), '-c', 
                 'import llama_cpp; print(llama_cpp.__version__)'],
                capture_output=True,
                text=True,
                timeout=5
            )
            
            if result.returncode != 0:
                return False, None, None
            
            version = result.stdout.strip()
            
            # Detect backend
            backend = self._detect_backend(python_executable)
            
            return True, version, backend
            
        except Exception:
            return False, None, None
    
    def _detect_backend(self, python_executable: Path) -> Optional[str]:
        """Detect GPU backend from llama-cpp-python installation"""
        try:
            # Try to import and check for backend attributes
            check_code = """
import llama_cpp
backend = None
if hasattr(llama_cpp, 'llama_backend_cublas'):
    backend = 'cuda'
elif hasattr(llama_cpp, 'llama_backend_hipblas'):
    backend = 'rocm'
elif hasattr(llama_cpp, 'llama_backend_vulkan'):
    backend = 'vulkan'
elif hasattr(llama_cpp, 'llama_backend_metal'):
    backend = 'metal'
else:
    backend = 'cpu'
print(backend)
"""
            result = subprocess.run(
                [str(python_executable), '-c', check_code],
                capture_output=True,
                text=True,
                timeout=5
            )
            
            if result.returncode == 0:
                return result.stdout.strip()
            
        except Exception:
            pass
        
        return "cpu"  # Default to CPU
    
    def get_compatible_extensions(self) -> List[BackendExtension]:
        """Get extensions compatible with current system"""
        all_exts = self.get_available_extensions()
        return [ext for ext in all_exts if ext.compatible]
    
    def get_installed_extensions(self) -> List[BackendExtension]:
        """Get only installed extensions"""
        all_exts = self.get_available_extensions()
        return [ext for ext in all_exts if ext.installed]
    
    def get_engines(self) -> List[BackendExtension]:
        """Get only engine extensions"""
        all_exts = self.get_available_extensions()
        return [ext for ext in all_exts if ext.type == ExtensionType.ENGINE.value]
    
    def get_frameworks(self) -> List[BackendExtension]:
        """Get only framework extensions"""
        all_exts = self.get_available_extensions()
        return [ext for ext in all_exts if ext.type == ExtensionType.FRAMEWORK.value]
    
    def install_extension(self, extension_id: str, venv_name: Optional[str] = None, model_id: Optional[str] = None) -> Dict[str, Any]:
        """
        Install an extension in a virtual environment
        
        Note: This should be called when setting up a model's environment, not to create
        standalone backend environments. LM Studio doesn't create separate environments per backend.
        
        Args:
            extension_id: The extension to install
            venv_name: Existing virtual environment name (required)
            model_id: Optional model ID to associate with
        """
        # Get extension info
        extensions = self.get_available_extensions()
        extension = next((e for e in extensions if e.id == extension_id), None)
        
        if not extension:
            return {"success": False, "error": f"Extension {extension_id} not found"}
        
        if not extension.compatible:
            return {"success": False, "error": f"Extension {extension_id} is not compatible with this system"}
        
        # venv_name is required - we don't create generic backend environments
        if not venv_name:
            return {
                "success": False,
                "error": "venv_name is required. Backend extensions should be installed in model-specific environments, not standalone backend environments."
            }
        
        # Check if venv exists
        environments = self.env_mgr.list_environments()
        venv_exists = any(e.name == venv_name for e in environments)
        
        if not venv_exists:
            return {
                "success": False,
                "error": f"Environment {venv_name} does not exist. Create the environment first, then install the backend extension."
            }
        
        # Install llama-cpp-python with the appropriate backend
        backend = extension.backend
        if backend == "cuda12":
            backend = "cuda"  # Use CUDA for CUDA 12
        
        result = self.env_mgr.install_llama_cpp_python(venv_name, backend)
        
        if result.get("success"):
            # If model_id provided, associate it
            if model_id:
                from app.services.llm_environment import get_llm_env_manager
                llm_env_mgr = get_llm_env_manager()
                llm_env_mgr.associate_model_with_env(model_id, venv_name, backend)
            
            return {
                "success": True,
                "venv_name": venv_name,
                "message": f"Successfully installed {extension.name} in {venv_name}"
            }
        else:
            return {
                "success": False,
                "error": result.get("stderr", "Installation failed")
            }
    
    def uninstall_extension(self, extension_id: str, venv_name: str) -> Dict[str, Any]:
        """Uninstall an extension from a virtual environment"""
        # Get environment
        environments = self.env_mgr.list_environments()
        env = next((e for e in environments if e.name == venv_name), None)
        
        if not env:
            return {"success": False, "error": f"Environment {venv_name} not found"}
        
        # Uninstall llama-cpp-python
        if platform.system() == "Windows":
            python_exe = Path(env.python_executable)
        else:
            python_exe = Path(env.python_executable)
        
        try:
            result = subprocess.run(
                [str(python_exe), "-m", "pip", "uninstall", "llama-cpp-python", "-y"],
                capture_output=True,
                text=True,
                timeout=60
            )
            
            if result.returncode == 0:
                return {
                    "success": True,
                    "message": f"Successfully uninstalled {extension_id} from {venv_name}"
                }
            else:
                return {
                    "success": False,
                    "error": result.stderr
                }
        except Exception as e:
            return {"success": False, "error": str(e)}


def get_backend_extension_service() -> BackendExtensionService:
    """Get singleton instance of BackendExtensionService"""
    if not hasattr(get_backend_extension_service, '_instance'):
        get_backend_extension_service._instance = BackendExtensionService()
    return get_backend_extension_service._instance

