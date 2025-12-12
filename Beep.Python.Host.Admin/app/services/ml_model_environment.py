"""
ML Model Environment Manager

Manages a single shared Python virtual environment for ML model inference.
Similar to RAG environment - one shared environment for all ML models.

Uses the existing EnvironmentManager to create and manage the virtual environment.
"""
import os
import sys
import json
import subprocess
import threading
from pathlib import Path
from datetime import datetime
from typing import Optional, List, Dict, Any, Callable
from enum import Enum
from dataclasses import dataclass

from app.services.environment_manager import EnvironmentManager


class MLEnvStatus(Enum):
    """ML Environment status"""
    NOT_CREATED = "not_created"
    CREATING = "creating"
    CREATED = "created"
    READY = "ready"  # Has ML packages installed
    ERROR = "error"


# Common ML packages that should be installed in the ML environment
# Package names without version specifiers to avoid pip parsing issues
ML_REQUIRED_PACKAGES = [
    "numpy",
    "scipy",
    "pandas",
    "scikit-learn",
    "joblib",
]

ML_OPTIONAL_PACKAGES = {
    "tensorflow": "tensorflow",  # GPU support included automatically in TF 2.x
    "pytorch": "torch",
    "xgboost": "xgboost",
    "onnxruntime": "onnxruntime",
    "keras": "keras",
}


@dataclass
class MLEnvironmentInfo:
    """ML Environment information"""
    status: str
    env_path: str
    python_path: str
    exists: bool
    installed_packages: List[str]
    error: Optional[str] = None


class MLModelEnvironmentManager:
    """
    Manages a single shared Python virtual environment for ML model inference.
    
    Uses EnvironmentManager to create and manage the virtual environment.
    All ML models run in this shared environment to avoid dependency conflicts.
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
        # Use app's own folder
        from app.config_manager import get_app_directory
        self._base_path = get_app_directory()
        self._env_name = "ml_models_env"  # Name of the virtual environment
        self._config_file = self._base_path / 'config' / 'ml_model_environment.json'
        self._config_file.parent.mkdir(parents=True, exist_ok=True)
        
        # Initialize environment manager
        self._env_manager = EnvironmentManager(base_path=str(self._base_path))
        
        # Status tracking
        self._status = MLEnvStatus.NOT_CREATED
        self._error_message: Optional[str] = None
        self._installed_packages: List[str] = []
        self._install_progress: Dict[str, Any] = {}
        
        # Check existing environment
        self._check_environment()
    
    def _check_environment(self):
        """Check if ML environment exists and is valid"""
        try:
            environments = self._env_manager.list_environments()
            ml_env = next((e for e in environments if e.name == self._env_name), None)
            
            if ml_env:
                self._status = MLEnvStatus.CREATED
                # Check if it has required packages
                self._load_config()
                if self._installed_packages:
                    self._status = MLEnvStatus.READY
            else:
                self._status = MLEnvStatus.NOT_CREATED
        except Exception as e:
            self._status = MLEnvStatus.ERROR
            self._error_message = str(e)
    
    def _get_env_path(self) -> Optional[Path]:
        """Get path to ML environment"""
        try:
            environments = self._env_manager.list_environments()
            ml_env = next((e for e in environments if e.name == self._env_name), None)
            if ml_env:
                return Path(ml_env.path)
        except:
            pass
        return None
    
    def _get_python_path(self) -> Optional[Path]:
        """Get path to Python executable in ML env"""
        env_path = self._get_env_path()
        if not env_path:
            return None
        
        if sys.platform == 'win32':
            return env_path / 'Scripts' / 'python.exe'
        return env_path / 'bin' / 'python'
    
    def _get_pip_path(self) -> Optional[Path]:
        """Get path to pip in ML env"""
        env_path = self._get_env_path()
        if not env_path:
            return None
        
        if sys.platform == 'win32':
            return env_path / 'Scripts' / 'pip.exe'
        return env_path / 'bin' / 'pip'
    
    def _load_config(self):
        """Load ML environment configuration"""
        if self._config_file.exists():
            try:
                with open(self._config_file, 'r') as f:
                    config = json.load(f)
                    self._installed_packages = config.get('installed_packages', [])
            except Exception as e:
                print(f"Failed to load ML env config: {e}")
    
    def _save_config(self):
        """Save ML environment configuration"""
        config = {
            'env_name': self._env_name,
            'status': self._status.value,
            'installed_packages': self._installed_packages,
            'updated_at': datetime.utcnow().isoformat()
        }
        
        with open(self._config_file, 'w') as f:
            json.dump(config, f, indent=2)
    
    @property
    def status(self) -> MLEnvStatus:
        return self._status
    
    @property
    def is_ready(self) -> bool:
        return self._status == MLEnvStatus.READY
    
    @property
    def env_path(self) -> Optional[Path]:
        return self._get_env_path()
    
    @property
    def python_path(self) -> Optional[Path]:
        return self._get_python_path()
    
    @property
    def error_message(self) -> Optional[str]:
        return self._error_message
    
    def get_status_info(self) -> Dict[str, Any]:
        """Get detailed status information"""
        env_path = self._get_env_path()
        python_path = self._get_python_path()
        
        return {
            'status': self._status.value,
            'env_name': self._env_name,
            'env_path': str(env_path) if env_path else None,
            'python_path': str(python_path) if python_path else None,
            'exists': python_path.exists() if python_path else False,
            'error': self._error_message,
            'installed_packages': self._installed_packages,
            'progress': self._install_progress
        }
    
    def create_environment(self, auto_install_packages: bool = True) -> Dict[str, Any]:
        """
        Create the ML model environment using EnvironmentManager
        
        Args:
            auto_install_packages: If True, automatically install required packages after creation
        
        Returns:
            Status dict with success/error info
        """
        if self._status == MLEnvStatus.CREATING:
            return {'success': False, 'error': 'Environment creation already in progress'}
        
        if self._status == MLEnvStatus.READY:
            return {'success': True, 'message': 'Environment already exists and is ready'}
        
        try:
            self._status = MLEnvStatus.CREATING
            self._error_message = None
            
            # Check if environment already exists
            environments = self._env_manager.list_environments()
            existing_env = next((e for e in environments if e.name == self._env_name), None)
            
            if existing_env:
                self._status = MLEnvStatus.CREATED
                # Auto-install packages if requested
                if auto_install_packages:
                    install_result = self.install_packages()
                    if install_result.get('success'):
                        return {'success': True, 'message': 'Environment exists and packages installed'}
                    else:
                        return {'success': True, 'message': 'Environment exists but package installation had issues', 'warning': install_result.get('error')}
                return {'success': True, 'message': 'Environment already exists'}
            
            # Create environment using EnvironmentManager
            self._install_progress = {'step': 'creating', 'progress': 10, 'message': 'Creating virtual environment...'}
            
            result = self._env_manager.create_environment(name=self._env_name)
            
            if result:
                self._status = MLEnvStatus.CREATED
                self._save_config()
                
                # Auto-install required packages if requested
                if auto_install_packages:
                    install_result = self.install_packages()
                    if install_result.get('success'):
                        return {'success': True, 'message': 'Environment created and packages installed successfully'}
                    else:
                        return {
                            'success': True,
                            'message': 'Environment created but package installation had issues',
                            'warning': install_result.get('error')
                        }
                
                return {'success': True, 'message': 'Environment created successfully'}
            else:
                self._status = MLEnvStatus.ERROR
                self._error_message = 'Failed to create environment'
                return {'success': False, 'error': 'Failed to create environment'}
        
        except Exception as e:
            self._status = MLEnvStatus.ERROR
            self._error_message = str(e)
            return {'success': False, 'error': str(e)}
    
    def install_packages(self, packages: Optional[List[str]] = None, use_gpu: bool = False) -> Dict[str, Any]:
        """
        Install packages in the ML environment
        
        Args:
            packages: List of package names to install. If None, installs required packages.
            use_gpu: Whether to install GPU versions of packages (TensorFlow, PyTorch)
        
        Returns:
            Status dict with success/error info
        """
        if self._status == MLEnvStatus.NOT_CREATED:
            # Try to create environment first
            create_result = self.create_environment()
            if not create_result.get('success'):
                return create_result
        
        if self._status != MLEnvStatus.CREATED and self._status != MLEnvStatus.READY:
            return {'success': False, 'error': f'Environment not ready. Status: {self._status.value}'}
        
        try:
            env_path = self._get_env_path()
            if not env_path:
                return {'success': False, 'error': 'Environment path not found'}
            
            # Determine packages to install
            if packages is None:
                packages_to_install = ML_REQUIRED_PACKAGES.copy()
            else:
                packages_to_install = packages.copy()
            
            # Note: tensorflow-gpu is deprecated. TensorFlow 2.x includes GPU support automatically
            # PyTorch GPU support requires specific installation from pytorch.org
            # For now, we install CPU versions which work everywhere
            
            self._install_progress = {'step': 'installing', 'progress': 20, 'message': f'Installing {len(packages_to_install)} packages...'}
            
            # Install packages using EnvironmentManager
            result = self._env_manager.install_packages(self._env_name, packages_to_install)
            
            if result.get('success'):
                # Update installed packages list
                self._installed_packages.extend(packages_to_install)
                self._status = MLEnvStatus.READY
                self._save_config()
                return {
                    'success': True,
                    'message': f'Successfully installed {len(packages_to_install)} packages',
                    'packages': packages_to_install
                }
            else:
                return {
                    'success': False,
                    'error': result.get('stderr', 'Package installation failed'),
                    'stdout': result.get('stdout', '')
                }
        
        except Exception as e:
            self._status = MLEnvStatus.ERROR
            self._error_message = str(e)
            return {'success': False, 'error': str(e)}
    
    def setup_environment(self, install_optional: bool = False, use_gpu: bool = False) -> Dict[str, Any]:
        """
        Complete setup: create environment and install required packages
        
        Args:
            install_optional: Install optional packages (TensorFlow, PyTorch, etc.)
            use_gpu: Use GPU versions where available
        
        Returns:
            Status dict with success/error info
        """
        # Step 1: Create environment
        create_result = self.create_environment()
        if not create_result.get('success'):
            return create_result
        
        # Step 2: Install required packages
        install_result = self.install_packages(use_gpu=use_gpu)
        if not install_result.get('success'):
            return install_result
        
        # Step 3: Install optional packages if requested
        if install_optional:
            optional_packages = []
            for pkg_name, pkg_spec in ML_OPTIONAL_PACKAGES.items():
                optional_packages.append(pkg_spec)
            
            if optional_packages:
                optional_result = self.install_packages(packages=optional_packages, use_gpu=use_gpu)
                if not optional_result.get('success'):
                    # Optional packages failure is not critical
                    print(f"Warning: Failed to install some optional packages: {optional_result.get('error')}")
        
        return {
            'success': True,
            'message': 'ML environment setup complete',
            'status': self._status.value,
            'installed_packages': self._installed_packages
        }
    
    def install_model_dependencies(self, requirements: List[str]) -> Dict[str, Any]:
        """
        Install dependencies for a specific model
        
        Args:
            requirements: List of package requirements (e.g., ['scikit-learn==1.3.0', 'pandas>=2.0.0'])
        
        Returns:
            Status dict with success/error info
        """
        return self.install_packages(packages=requirements)
    
    def get_python_executable(self) -> Optional[str]:
        """Get path to Python executable in ML environment"""
        python_path = self._get_python_path()
        return str(python_path) if python_path and python_path.exists() else None
    
    def run_in_environment(self, script: str, args: List[str] = None) -> Dict[str, Any]:
        """
        Run a Python script in the ML environment using subprocess
        
        Args:
            script: Python script code or file path
            args: Additional arguments to pass
        
        Returns:
            Result dict with stdout, stderr, returncode
        """
        python_path = self.get_python_executable()
        if not python_path:
            return {
                'success': False,
                'error': 'ML environment not ready. Please run setup first.',
                'stdout': '',
                'stderr': 'Environment not found',
                'returncode': -1
            }
        
        try:
            if Path(script).exists():
                # Run as file
                cmd = [python_path, script]
            else:
                # Run as code string
                cmd = [python_path, '-c', script]
            
            if args:
                cmd.extend(args)
            
            result = subprocess.run(
                cmd,
                capture_output=True,
                text=True,
                timeout=300  # 5 minute timeout
            )
            
            return {
                'success': result.returncode == 0,
                'stdout': result.stdout,
                'stderr': result.stderr,
                'returncode': result.returncode
            }
        
        except subprocess.TimeoutExpired:
            return {
                'success': False,
                'error': 'Execution timeout',
                'stdout': '',
                'stderr': 'Process exceeded 5 minute timeout',
                'returncode': -1
            }
        except Exception as e:
            return {
                'success': False,
                'error': str(e),
                'stdout': '',
                'stderr': str(e),
                'returncode': -1
            }


def get_ml_model_environment_manager() -> MLModelEnvironmentManager:
    """Get singleton instance of MLModelEnvironmentManager"""
    return MLModelEnvironmentManager()

