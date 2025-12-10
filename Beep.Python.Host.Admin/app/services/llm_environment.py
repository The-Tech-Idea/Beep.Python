"""
LLM Environment Manager

Manages the LLM inference environment including:
- Tracking which environment has LLM ready
- Installing required packages (llama-cpp-python, etc.)
- Managing model installations per environment
- Model categories with descriptions

This is the central place for LLM environment state.
"""
import os
import sys
import json
import subprocess
import threading
from pathlib import Path
from datetime import datetime
from typing import Optional, List, Dict, Any
from dataclasses import dataclass, field, asdict
from enum import Enum


class LLMEnvStatus(Enum):
    """LLM Environment status"""
    NOT_CREATED = "not_created"
    CREATING = "creating"
    CREATED = "created"
    READY = "ready"          # Has llama-cpp-python installed
    ERROR = "error"


class ModelCategory(Enum):
    """LLM Model categories with use cases"""
    CHAT = "chat"
    INSTRUCT = "instruct"
    CODE = "code"
    REASONING = "reasoning"
    VISION = "vision"
    EMBEDDING = "embedding"
    GENERAL = "general"


# Model category information for UI display
MODEL_CATEGORY_INFO = {
    ModelCategory.CHAT: {
        "name": "Chat / Conversational",
        "icon": "bi-chat-dots",
        "color": "primary",
        "description": "Optimized for natural conversation and dialogue",
        "use_cases": [
            "Customer service chatbots",
            "Personal AI assistants",
            "Interactive Q&A systems",
            "Casual conversation partners"
        ],
        "best_for": "Multi-turn conversations where context and natural flow matter",
        "examples": ["Llama-3-Chat", "Mistral-Instruct", "Qwen-Chat"]
    },
    ModelCategory.INSTRUCT: {
        "name": "Instruction Following",
        "icon": "bi-list-task",
        "color": "success",
        "description": "Designed to follow specific instructions and complete tasks",
        "use_cases": [
            "Task automation",
            "Document processing",
            "Data extraction",
            "Text transformation"
        ],
        "best_for": "Single-turn tasks with clear instructions",
        "examples": ["Llama-3-Instruct", "Mistral-7B-Instruct", "Phi-3-mini-instruct"]
    },
    ModelCategory.CODE: {
        "name": "Code Generation",
        "icon": "bi-code-slash",
        "color": "warning",
        "description": "Specialized for writing, understanding, and debugging code",
        "use_cases": [
            "Code completion",
            "Bug fixing",
            "Code explanation",
            "Code translation between languages",
            "Unit test generation"
        ],
        "best_for": "Programming tasks, IDE integration, code review",
        "examples": ["CodeLlama", "DeepSeek-Coder", "StarCoder2", "Qwen-Coder"]
    },
    ModelCategory.REASONING: {
        "name": "Reasoning / Math",
        "icon": "bi-lightbulb",
        "color": "info",
        "description": "Enhanced logical reasoning and mathematical capabilities",
        "use_cases": [
            "Complex problem solving",
            "Mathematical calculations",
            "Logical deduction",
            "Scientific analysis"
        ],
        "best_for": "Tasks requiring step-by-step reasoning or calculations",
        "examples": ["DeepSeek-R1", "Qwen-Math", "WizardMath"]
    },
    ModelCategory.VISION: {
        "name": "Vision / Multimodal",
        "icon": "bi-eye",
        "color": "danger",
        "description": "Can process images along with text",
        "use_cases": [
            "Image description",
            "Visual Q&A",
            "Document analysis with images",
            "Chart/diagram understanding"
        ],
        "best_for": "Tasks that combine text and image understanding",
        "examples": ["LLaVA", "Qwen-VL", "MiniCPM-V"]
    },
    ModelCategory.EMBEDDING: {
        "name": "Text Embeddings",
        "icon": "bi-vector-pen",
        "color": "secondary",
        "description": "Converts text to numerical vectors for similarity search",
        "use_cases": [
            "Semantic search",
            "Document clustering",
            "RAG retrieval",
            "Similarity matching"
        ],
        "best_for": "RAG systems and semantic search applications",
        "examples": ["all-MiniLM", "nomic-embed", "bge-base"]
    },
    ModelCategory.GENERAL: {
        "name": "General Purpose",
        "icon": "bi-stars",
        "color": "light",
        "description": "Balanced models good for various tasks",
        "use_cases": [
            "General text generation",
            "Summarization",
            "Translation",
            "Mixed workloads"
        ],
        "best_for": "When you need one model for multiple use cases",
        "examples": ["Llama-3", "Mistral", "Phi-3", "Qwen"]
    }
}


# Required packages for LLM inference
LLM_REQUIRED_PACKAGES = {
    "core": [
        "llama-cpp-python",
        "numpy",
        "huggingface-hub"
    ],
    "optional": [
        "transformers",
        "tokenizers",
        "sentencepiece"
    ]
}

# GPU-specific installation commands using PRE-BUILT wheels (LM Studio style)
# No compilers or CUDA Toolkit needed - just download pre-built binaries
# Source: https://github.com/jllllll/llama-cpp-python-cuBLAS-wheels
GPU_INSTALL_COMMANDS = {
    "cuda": {
        "name": "NVIDIA CUDA (Pre-built)",
        "command": 'pip install llama-cpp-python --prefer-binary --extra-index-url=https://jllllll.github.io/llama-cpp-python-cuBLAS-wheels/AVX2/cu121',
        "env_vars": {},
        "pre_install": None,
        "description": "Pre-built CUDA 12.1 wheel - no compilation needed"
    },
    "cuda11": {
        "name": "NVIDIA CUDA 11 (Pre-built)",
        "command": 'pip install llama-cpp-python --prefer-binary --extra-index-url=https://jllllll.github.io/llama-cpp-python-cuBLAS-wheels/AVX2/cu118',
        "env_vars": {},
        "pre_install": None,
        "description": "Pre-built CUDA 11.8 wheel - no compilation needed"
    },
    "cuda12": {
        "name": "NVIDIA CUDA 12 (Pre-built)",
        "command": 'pip install llama-cpp-python --prefer-binary --extra-index-url=https://jllllll.github.io/llama-cpp-python-cuBLAS-wheels/AVX2/cu121',
        "env_vars": {},
        "pre_install": None,
        "description": "Pre-built CUDA 12.1 wheel - no compilation needed"
    },
    "metal": {
        "name": "Apple Metal (Pre-built)",
        "command": 'pip install llama-cpp-python --prefer-binary --extra-index-url=https://jllllll.github.io/llama-cpp-python-cuBLAS-wheels/metal',
        "env_vars": {},
        "pre_install": None,
        "description": "Pre-built Metal wheel for macOS - no compilation needed"
    },
    "vulkan": {
        "name": "Vulkan (Pre-built)",
        "command": 'pip install llama-cpp-python --prefer-binary --extra-index-url=https://jllllll.github.io/llama-cpp-python-cuBLAS-wheels/AVX2/cpu',
        "env_vars": {},
        "pre_install": None,
        "description": "CPU build with Vulkan support - no compilation needed"
    },
    "rocm": {
        "name": "AMD ROCm (Pre-built)",
        "command": 'pip install llama-cpp-python --prefer-binary --extra-index-url=https://jllllll.github.io/llama-cpp-python-cuBLAS-wheels/rocm',
        "env_vars": {},
        "pre_install": None,
        "description": "Pre-built ROCm wheel for AMD GPUs - no compilation needed"
    },
    "cpu": {
        "name": "CPU Only (Pre-built)",
        "command": 'pip install llama-cpp-python --prefer-binary --extra-index-url=https://jllllll.github.io/llama-cpp-python-cuBLAS-wheels/AVX2/cpu',
        "env_vars": {},
        "pre_install": None,
        "description": "Pre-built CPU-only wheel - no compilation needed"
    }
}


@dataclass
class LLMEnvironment:
    """Represents an LLM environment"""
    name: str
    path: str
    status: str = "not_created"
    has_inference: bool = False
    llama_cpp_version: Optional[str] = None
    gpu_backend: Optional[str] = None
    installed_packages: List[str] = field(default_factory=list)
    models_loaded: List[str] = field(default_factory=list)
    created_at: Optional[str] = None
    last_used: Optional[str] = None
    error_message: Optional[str] = None
    
    def to_dict(self) -> dict:
        return asdict(self)


class LLMEnvironmentManager:
    """
    Singleton manager for LLM environments.
    
    Tracks which environments have LLM capabilities and manages package installation.
    """
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
        self._base_path = get_app_directory()
        self._env_path = self._base_path / 'llm_env'
        self._config_path = self._base_path / 'config'
        
        self._config_path.mkdir(parents=True, exist_ok=True)
        
        # Current active environment
        self._active_env: Optional[LLMEnvironment] = None
        self._status = LLMEnvStatus.NOT_CREATED
        self._installation_progress: Dict[str, Any] = {}
        
        # Model-to-venv associations
        self._model_associations: Dict[str, Dict[str, Any]] = {}  # model_id -> {venv_name, gpu_backend, ...}
        
        # Load saved state
        self._load_state()
        self._load_model_associations()
        
        # Check current environment on startup
        self._check_current_environment()
    
    def _get_config_file(self) -> Path:
        return self._config_path / 'llm_environment.json'
    
    def _get_associations_file(self) -> Path:
        return self._config_path / 'model_environments.json'
    
    def _load_model_associations(self):
        """Load model-to-venv associations"""
        assoc_file = self._get_associations_file()
        if assoc_file.exists():
            try:
                with open(assoc_file, 'r') as f:
                    data = json.load(f)
                    self._model_associations = data.get('model_environments', {})
                
                # Clean up invalid associations on load
                self.cleanup_invalid_associations()
            except Exception as e:
                print(f"Error loading model associations: {e}")
                self._model_associations = {}

    def cleanup_invalid_associations(self):
        """Remove associations for environments that no longer exist"""
        try:
            from app.services.environment_manager import EnvironmentManager
            env_mgr = EnvironmentManager(base_path=str(self._base_path))
            
            # Get list of existing environment names
            existing_envs = {env.name for env in env_mgr.list_environments()}
            
            # Find invalid associations
            invalid_models = []
            for model_id, assoc in self._model_associations.items():
                venv_name = assoc.get('venv_name')
                if venv_name and venv_name not in existing_envs:
                    invalid_models.append(model_id)
            
            # Remove invalid associations
            if invalid_models:
                for model_id in invalid_models:
                    del self._model_associations[model_id]
                
                self._save_model_associations()
                print(f"Cleaned up {len(invalid_models)} invalid model associations")
                
        except Exception as e:
            print(f"Error cleaning up associations: {e}")
    
    def _save_model_associations(self):
        """Save model-to-venv associations"""
        assoc_file = self._get_associations_file()
        data = {
            'version': 1,
            'updated': datetime.now().isoformat(),
            'model_environments': self._model_associations
        }
        with open(assoc_file, 'w') as f:
            json.dump(data, f, indent=2)

    def remove_environment_associations(self, venv_name: str):
        """Remove all model associations for a specific environment"""
        # Find models associated with this venv
        models_to_update = []
        for model_id, assoc in self._model_associations.items():
            if assoc.get('venv_name') == venv_name:
                models_to_update.append(model_id)
        
        # Remove associations
        if models_to_update:
            for model_id in models_to_update:
                del self._model_associations[model_id]
            
            self._save_model_associations()
            print(f"Removed associations for environment '{venv_name}' from {len(models_to_update)} models")
    
    def _load_state(self):
        """Load saved environment state"""
        config_file = self._get_config_file()
        if config_file.exists():
            try:
                with open(config_file, 'r') as f:
                    data = json.load(f)
                    if data.get('active_env'):
                        self._active_env = LLMEnvironment(**data['active_env'])
            except Exception as e:
                print(f"Error loading LLM environment state: {e}")
    
    def _save_state(self):
        """Save environment state"""
        config_file = self._get_config_file()
        data = {
            'version': 1,
            'updated': datetime.now().isoformat(),
            'active_env': self._active_env.to_dict() if self._active_env else None
        }
        with open(config_file, 'w') as f:
            json.dump(data, f, indent=2)
    
    def _check_current_environment(self):
        """Check if current Python environment has LLM capabilities"""
        try:
            from llama_cpp import Llama
            import llama_cpp
            
            # Current environment has llama-cpp-python
            version = getattr(llama_cpp, '__version__', 'unknown')
            
            if self._active_env is None:
                self._active_env = LLMEnvironment(
                    name="current",
                    path=sys.executable,
                    status="ready",
                    has_inference=True,
                    llama_cpp_version=version,
                    created_at=datetime.now().isoformat()
                )
            else:
                self._active_env.has_inference = True
                self._active_env.llama_cpp_version = version
                self._active_env.status = "ready"
            
            self._status = LLMEnvStatus.READY
            self._save_state()
            
        except ImportError:
            self._status = LLMEnvStatus.CREATED if self._env_path.exists() else LLMEnvStatus.NOT_CREATED
            if self._active_env:
                self._active_env.has_inference = False
                self._active_env.status = str(self._status.value)
    
    # =====================
    # Environment Management
    # =====================
    
    def get_status(self) -> Dict[str, Any]:
        """Get current LLM environment status in format expected by UI"""
        # Check package installations
        llama_info = self.check_package('llama-cpp-python')
        torch_info = self.check_package('torch')
        transformers_info = self.check_package('transformers')
        
        # Determine GPU backend
        gpu_backend = self._detect_gpu_backend()
        gpu_info = self._get_gpu_info()
        
        return {
            'status': self._status.value,
            'llama_cpp_installed': llama_info.get('installed', False),
            'llama_cpp_version': llama_info.get('version'),
            'torch_installed': torch_info.get('installed', False),
            'torch_version': torch_info.get('version'),
            'transformers_installed': transformers_info.get('installed', False),
            'transformers_version': transformers_info.get('version'),
            'gpu_backend': gpu_backend,
            'gpu_info': gpu_info,
            'inference_ready': self._status == LLMEnvStatus.READY,
            'has_inference': self._status == LLMEnvStatus.READY,
            'active_env': self._active_env.to_dict() if self._active_env else None,
            'env_path': str(self._env_path),
            'required_packages': LLM_REQUIRED_PACKAGES,
            'gpu_options': {k: v['name'] for k, v in GPU_INSTALL_COMMANDS.items()}
        }
    
    def _detect_gpu_backend(self) -> str:
        """Detect current GPU backend in use"""
        # Check if CUDA is available via torch
        try:
            import torch
            if torch.cuda.is_available():
                return 'cuda'
            if hasattr(torch.backends, 'mps') and torch.backends.mps.is_available():
                return 'metal'
        except ImportError:
            pass
        
        # Check environment variables
        cmake_args = os.environ.get('CMAKE_ARGS', '')
        if 'CUDA' in cmake_args:
            return 'cuda'
        if 'METAL' in cmake_args:
            return 'metal'
        if 'VULKAN' in cmake_args:
            return 'vulkan'
        
        return 'cpu'
    
    def _get_gpu_info(self) -> str:
        """Get GPU information string"""
        try:
            import torch
            if torch.cuda.is_available():
                return torch.cuda.get_device_name(0)
            if hasattr(torch.backends, 'mps') and torch.backends.mps.is_available():
                return 'Apple Silicon (MPS)'
        except ImportError:
            pass
        
        # Try nvidia-smi for non-torch detection
        try:
            result = subprocess.run(['nvidia-smi', '--query-gpu=name', '--format=csv,noheader,nounits'],
                                  capture_output=True, text=True)
            if result.returncode == 0:
                return result.stdout.strip().split('\n')[0]
        except:
            pass
        
        return 'CPU'
    
    def get_installation_options(self) -> Dict[str, Any]:
        """Get available installation options with descriptions"""
        return {
            'gpu_backends': GPU_INSTALL_COMMANDS,
            'required_packages': LLM_REQUIRED_PACKAGES,
            'recommended': self._detect_recommended_backend()
        }
    
    def _detect_recommended_backend(self) -> str:
        """Detect recommended GPU backend based on system"""
        # Check for NVIDIA GPU
        try:
            result = subprocess.run(['nvidia-smi'], capture_output=True, text=True)
            if result.returncode == 0:
                return 'cuda'
        except:
            pass
        
        # Check for AMD GPU (ROCm)
        try:
            result = subprocess.run(['rocm-smi'], capture_output=True, text=True)
            if result.returncode == 0:
                return 'rocm'
        except:
            pass

        # Check for Apple Silicon
        import platform
        if platform.system() == 'Darwin' and platform.machine() == 'arm64':
            return 'metal'
        
        # Check for Vulkan (generic fallback for GPUs)
        try:
            result = subprocess.run(['vulkaninfo'], capture_output=True, text=True)
            if result.returncode == 0:
                return 'vulkan'
        except:
            pass
        
        # Default to CPU
        return 'cpu'
    
    # =====================
    # Virtual Environment Management (NEW)
    # =====================
    
    def get_llm_capable_environments(self) -> List[Dict[str, Any]]:
        """
        Get all virtual environments that have llama-cpp-python installed
        
        Returns list of dicts with venv info and LLM capabilities
        """
        from app.services.environment_manager import EnvironmentManager
        
        env_mgr = EnvironmentManager(base_path=str(self._base_path))
        all_venvs = env_mgr.list_environments()
        
        llm_venvs = []
        for venv in all_venvs:
            # Check if this venv has llama-cpp-python
            has_llama, version, backend = self._check_venv_llama_cpp(venv.python_executable)
            
            if has_llama:
                # Check which models use this venv
                models_using = [
                    model_id for model_id, assoc in self._model_associations.items()
                    if assoc.get('venv_name') == venv.name
                ]
                
                llm_venvs.append({
                    'name': venv.name,
                    'path': venv.path,
                    'python_version': venv.python_version,
                    'python_executable': venv.python_executable,
                    'llama_cpp_version': version,
                    'gpu_backend': backend,
                    'models_using': models_using,
                    'models_count': len(models_using),
                    'created_at': venv.created_at,
                    'size_mb': venv.size_mb
                })
        
        return llm_venvs
    
    def _check_venv_llama_cpp(self, python_executable: str) -> tuple[bool, Optional[str], Optional[str]]:
        """
        Check if a venv has llama-cpp-python installed
        
        Returns: (has_llama, version, gpu_backend)
        """
        try:
            # Run python -c "import llama_cpp; print(llama_cpp.__version__)"
            result = subprocess.run(
                [python_executable, '-c', 
                 'import llama_cpp; print(llama_cpp.__version__)'],
                capture_output=True,
                text=True,
                timeout=5
            )
            
            if result.returncode == 0:
                version = result.stdout.strip()
                
                # Try to detect GPU backend
                backend = self._detect_venv_backend(python_executable)
                
                return True, version, backend
            else:
                return False, None, None
                
        except (subprocess.TimeoutExpired, FileNotFoundError, Exception):
            return False, None, None
    
    def _detect_venv_backend(self, python_executable: str) -> str:
        """Detect GPU backend for a venv's llama-cpp-python installation"""
        try:
            # Check for CUDA/CUBLAS (try both)
            for backend_check in ['cublas', 'cuda']:
                result = subprocess.run(
                    [python_executable, '-c',
                     f'import llama_cpp; print(hasattr(llama_cpp, "llama_backend_{backend_check}"))'],
                    capture_output=True,
                    text=True,
                    timeout=5
                )
                if result.returncode == 0 and 'True' in result.stdout:
                    return 'cuda'
            
            # Check for ROCm/HIPBLAS
            result = subprocess.run(
                [python_executable, '-c',
                 'import llama_cpp; print(hasattr(llama_cpp, "llama_backend_hipblas"))'],
                capture_output=True,
                text=True,
                timeout=5
            )
            if result.returncode == 0 and 'True' in result.stdout:
                return 'rocm'
            
            # Check for Vulkan
            result = subprocess.run(
                [python_executable, '-c',
                 'import llama_cpp; print(hasattr(llama_cpp, "llama_backend_vulkan"))'],
                capture_output=True,
                text=True,
                timeout=5
            )
            if result.returncode == 0 and 'True' in result.stdout:
                return 'vulkan'
            
            # Check for Metal (Apple Silicon)
            result = subprocess.run(
                [python_executable, '-c',
                 'import llama_cpp; print(hasattr(llama_cpp, "llama_backend_metal"))'],
                capture_output=True,
                text=True,
                timeout=5
            )
            if result.returncode == 0 and 'True' in result.stdout:
                return 'metal'
            
            # Default to CPU
            return 'cpu'
            
        except Exception:
            return 'cpu'
    
    def associate_model_with_env(self, model_id: str, venv_name: str, gpu_backend: str = 'cpu'):
        """
        Associate a model with a specific virtual environment
        
        Args:
            model_id: ID of the model
            venv_name: Name of the virtual environment
            gpu_backend: GPU backend used (cuda, metal, vulkan, cpu)
        """
        self._model_associations[model_id] = {
            'venv_name': venv_name,
            'gpu_backend': gpu_backend,
            'associated_at': datetime.now().isoformat()
        }
        self._save_model_associations()
    
    def get_model_environment(self, model_id: str) -> Optional[Dict[str, Any]]:
        """
        Get the virtual environment associated with a model
        
        Returns dict with venv_name, gpu_backend, etc. or None
        """
        return self._model_associations.get(model_id)
    
    def remove_model_association(self, model_id: str):
        """Remove model-to-venv association"""
        if model_id in self._model_associations:
            del self._model_associations[model_id]
            self._save_model_associations()
    
    def get_venv_path_for_model(self, model_id: str) -> Optional[str]:
        """Get the full path to the venv for a model"""
        assoc = self.get_model_environment(model_id)
        if not assoc:
            return None
        
        venv_name = assoc.get('venv_name')
        if not venv_name:
            return None
        
        # Get venv path from EnvironmentManager
        from app.services.environment_manager import EnvironmentManager
        env_mgr = EnvironmentManager(base_path=str(self._base_path))
        
        venvs = env_mgr.list_environments()
        for venv in venvs:
            if venv.name == venv_name:
                return venv.path
        
        return None
    
    def install_llm_packages(self, gpu_backend: str = 'cpu', 
                             use_existing_env: bool = True,
                             callback: Optional[callable] = None) -> Dict[str, Any]:
        """
        Install required LLM packages.
        
        Args:
            gpu_backend: 'cuda', 'metal', 'vulkan', or 'cpu'
            use_existing_env: If True, install in current env; if False, create new venv
            callback: Optional progress callback function
            
        Returns:
            Installation result
        """
        if gpu_backend not in GPU_INSTALL_COMMANDS:
            return {'success': False, 'error': f'Invalid backend: {gpu_backend}'}
        
        install_config = GPU_INSTALL_COMMANDS[gpu_backend]
        
        self._installation_progress = {
            'status': 'starting',
            'step': 'Preparing installation',
            'percent': 0,
            'backend': gpu_backend
        }
        
        def run_installation():
            try:
                # Step 1: Skip pre-install - pre-built wheels don't need cmake/ninja
                self._installation_progress['step'] = 'Preparing to download pre-built backend'
                self._installation_progress['percent'] = 10
                
                # Pre-built wheels don't need pre-install steps
                # if install_config.get('pre_install'):
                #     self._run_pip_command(install_config['pre_install'])
                
                # Step 2: No environment variables needed for pre-built wheels
                self._installation_progress['step'] = 'Downloading pre-built llama-cpp-python'
                self._installation_progress['percent'] = 20
                
                # Step 3: Install llama-cpp-python from pre-built wheel
                self._installation_progress['step'] = f"Installing {install_config.get('name', 'llama-cpp-python')}"
                self._installation_progress['percent'] = 30
                
                # Use shell=True for complex pip commands with extra-index-url
                result = subprocess.run(
                    install_config['command'],
                    capture_output=True,
                    text=True,
                    shell=True
                )
                
                if result.returncode != 0:
                    raise Exception(f"Installation failed: {result.stderr}")
                
                # Step 4: Install other required packages
                self._installation_progress['step'] = 'Installing additional packages'
                self._installation_progress['percent'] = 70
                
                for pkg in LLM_REQUIRED_PACKAGES['core'][1:]:  # Skip llama-cpp-python
                    self._run_pip_command(f'pip install {pkg}')
                
                # Step 5: Verify installation
                self._installation_progress['step'] = 'Verifying installation'
                self._installation_progress['percent'] = 90
                
                # Refresh environment check
                self._check_current_environment()
                
                self._installation_progress['status'] = 'completed'
                self._installation_progress['step'] = 'Installation complete'
                self._installation_progress['percent'] = 100
                
                if self._active_env:
                    self._active_env.gpu_backend = gpu_backend
                    self._save_state()
                
                if callback:
                    callback({'success': True, 'message': 'LLM packages installed successfully'})
                    
            except Exception as e:
                self._installation_progress['status'] = 'error'
                self._installation_progress['error'] = str(e)
                if callback:
                    callback({'success': False, 'error': str(e)})
        
        # Run in background thread
        thread = threading.Thread(target=run_installation)
        thread.daemon = True
        thread.start()
        
        return {
            'success': True,
            'message': 'Installation started',
            'progress_endpoint': '/llm/api/env/progress'
        }
    
    def _run_pip_command(self, command: str) -> subprocess.CompletedProcess:
        """Run a pip command"""
        return subprocess.run(
            command.split(),
            capture_output=True,
            text=True,
            check=True
        )
    
    def get_installation_progress(self) -> Dict[str, Any]:
        """Get current installation progress in format expected by UI"""
        progress = self._installation_progress.copy()
        
        # Add complete and success flags based on status
        if progress.get('status') == 'completed':
            progress['complete'] = True
            progress['success'] = True
        elif progress.get('status') == 'error':
            progress['complete'] = True
            progress['success'] = False
        else:
            progress['complete'] = False
            progress['success'] = False
        
        return progress
    
    def check_package(self, package_name: str) -> Dict[str, Any]:
        """Check if a package is installed"""
        try:
            result = subprocess.run(
                [sys.executable, '-m', 'pip', 'show', package_name],
                capture_output=True,
                text=True
            )
            if result.returncode == 0:
                # Parse version from output
                for line in result.stdout.split('\n'):
                    if line.startswith('Version:'):
                        version = line.split(':', 1)[1].strip()
                        return {'installed': True, 'version': version}
            return {'installed': False}
        except:
            return {'installed': False}
    
    def get_installed_packages(self) -> List[Dict[str, str]]:
        """Get list of relevant installed packages"""
        packages = []
        check_packages = LLM_REQUIRED_PACKAGES['core'] + LLM_REQUIRED_PACKAGES['optional']
        
        for pkg in check_packages:
            info = self.check_package(pkg)
            packages.append({
                'name': pkg,
                'installed': info.get('installed', False),
                'version': info.get('version'),
                'required': pkg in LLM_REQUIRED_PACKAGES['core']
            })
        
        return packages
    
    # =====================
    # Model Categories
    # =====================
    
    def get_model_categories(self) -> Dict[str, Dict]:
        """Get all model categories with their info"""
        return {cat.value: info for cat, info in MODEL_CATEGORY_INFO.items()}
    
    def get_category_info(self, category: str) -> Optional[Dict]:
        """Get info for a specific category"""
        try:
            cat = ModelCategory(category)
            return MODEL_CATEGORY_INFO.get(cat)
        except ValueError:
            return None
    
    def detect_model_category(self, model_name: str, model_id: str = "") -> str:
        """Detect model category from name/ID"""
        name_lower = (model_name + model_id).lower()
        
        # Check for specific patterns
        if any(x in name_lower for x in ['code', 'coder', 'starcoder', 'codellama', 'deepseek-coder']):
            return ModelCategory.CODE.value
        
        if any(x in name_lower for x in ['chat', 'conversational']):
            return ModelCategory.CHAT.value
        
        if any(x in name_lower for x in ['instruct', 'instruction']):
            return ModelCategory.INSTRUCT.value
        
        if any(x in name_lower for x in ['math', 'reasoning', 'deepseek-r1']):
            return ModelCategory.REASONING.value
        
        if any(x in name_lower for x in ['vision', 'vl', 'llava', 'multimodal']):
            return ModelCategory.VISION.value
        
        if any(x in name_lower for x in ['embed', 'minilm', 'bge', 'nomic']):
            return ModelCategory.EMBEDDING.value
        
        return ModelCategory.GENERAL.value


# Singleton accessor
def get_llm_env_manager() -> LLMEnvironmentManager:
    """Get the LLM environment manager singleton"""
    return LLMEnvironmentManager()
