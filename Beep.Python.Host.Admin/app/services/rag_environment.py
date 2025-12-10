"""
RAG Environment Manager

Manages an isolated virtual environment for RAG dependencies:
- FAISS (faiss-cpu or faiss-gpu)
- Sentence Transformers
- ChromaDB
- Other RAG-related packages

This ensures RAG packages don't conflict with the main application.
"""
import os
import sys
import json
import subprocess
import threading
from pathlib import Path
from typing import Optional, List, Dict, Any, Callable
from dataclasses import dataclass, field
from enum import Enum
from datetime import datetime
import logging

logger = logging.getLogger(__name__)


class RAGEnvStatus(Enum):
    """RAG environment status"""
    NOT_CREATED = "not_created"
    CREATING = "creating"
    READY = "ready"
    INSTALLING = "installing"
    ERROR = "error"


class RAGProviderChoice(Enum):
    """Available RAG providers"""
    FAISS = "faiss"
    CHROMADB = "chromadb"
    # Future providers can be added here


@dataclass
class RAGProviderInfo:
    """Information about a RAG provider"""
    name: str
    provider_type: RAGProviderChoice
    description: str
    packages: List[str]  # pip package names
    gpu_packages: Optional[List[str]] = None  # GPU alternatives
    features: List[str] = field(default_factory=list)
    
    
# Define RAG providers and their required packages
RAG_PROVIDERS = {
    RAGProviderChoice.FAISS: RAGProviderInfo(
        name="FAISS",
        provider_type=RAGProviderChoice.FAISS,
        description="Facebook AI Similarity Search - Fast, memory-efficient vector search. Best for large-scale similarity search.",
        packages=[
            "faiss-cpu",
            "sentence-transformers",
            "numpy"
        ],
        gpu_packages=[
            "faiss-gpu",
            "sentence-transformers",
            "numpy"
        ],
        features=[
            "Extremely fast similarity search",
            "Memory-efficient for large datasets",
            "Supports billions of vectors",
            "Multiple index types (Flat, IVF, HNSW)",
            "Works offline"
        ]
    ),
    RAGProviderChoice.CHROMADB: RAGProviderInfo(
        name="ChromaDB",
        provider_type=RAGProviderChoice.CHROMADB,
        description="Open-source embedding database with built-in embedding support. Best for ease of use and development.",
        packages=[
            "chromadb",
            "sentence-transformers"
        ],
        gpu_packages=None,  # ChromaDB doesn't have GPU-specific packages
        features=[
            "Built-in embedding functions",
            "Easy-to-use API",
            "Automatic persistence",
            "Metadata filtering",
            "Multi-modal support",
            "Works offline"
        ]
    )
}


@dataclass
class RAGPackage:
    """Information about a RAG package"""
    name: str
    pip_name: str
    description: str
    required: bool = False
    installed: bool = False
    version: Optional[str] = None
    gpu_alternative: Optional[str] = None


# Define available RAG packages
RAG_PACKAGES: Dict[str, RAGPackage] = {
    # Core RAG dependencies
    'faiss-cpu': RAGPackage(
        name='FAISS CPU',
        pip_name='faiss-cpu',
        description='Facebook AI Similarity Search (CPU version)',
        required=True
    ),
    'faiss-gpu': RAGPackage(
        name='FAISS GPU',
        pip_name='faiss-gpu',
        description='Facebook AI Similarity Search (GPU version)',
        required=False
    ),
    'chromadb': RAGPackage(
        name='ChromaDB',
        pip_name='chromadb',
        description='Open-source embedding database',
        required=True
    ),
    'sentence-transformers': RAGPackage(
        name='Sentence Transformers',
        pip_name='sentence-transformers',
        description='Sentence and text embeddings using BERT & Co.',
        required=True
    ),
    'numpy': RAGPackage(
        name='NumPy',
        pip_name='numpy',
        description='Fundamental package for scientific computing',
        required=True
    ),
    # Scheduling (for sync jobs - cross-platform Windows/Mac/Linux)
    'apscheduler': RAGPackage(
        name='APScheduler',
        pip_name='apscheduler',
        description='Advanced Python Scheduler for document sync jobs',
        required=True
    ),
    # Database connectors (optional - for data source sync)
    'sqlalchemy': RAGPackage(
        name='SQLAlchemy',
        pip_name='sqlalchemy',
        description='SQL toolkit and Object-Relational Mapping',
        required=True
    ),
    'pymysql': RAGPackage(
        name='PyMySQL',
        pip_name='pymysql',
        description='Pure Python MySQL client',
        required=False
    ),
    'psycopg2-binary': RAGPackage(
        name='Psycopg2',
        pip_name='psycopg2-binary',
        description='PostgreSQL adapter for Python',
        required=False
    ),
    'pymongo': RAGPackage(
        name='PyMongo',
        pip_name='pymongo',
        description='MongoDB driver for Python',
        required=False
    ),
    # Document processing (optional)
    'pypdf': RAGPackage(
        name='PyPDF',
        pip_name='pypdf',
        description='PDF processing library',
        required=False
    ),
    'python-docx': RAGPackage(
        name='Python-docx',
        pip_name='python-docx',
        description='Create and update Microsoft Word files',
        required=False
    ),
    'unstructured': RAGPackage(
        name='Unstructured',
        pip_name='unstructured',
        description='Pre-processing for documents, images, and audio',
        required=False
    ),
    # LangChain and LlamaIndex (optional)
    'langchain': RAGPackage(
        name='LangChain',
        pip_name='langchain',
        description='Building applications with LLMs through composability',
        required=False
    ),
    'llama-index': RAGPackage(
        name='LlamaIndex',
        pip_name='llama-index',
        description='Data framework for LLM applications',
        required=False
    ),
}


class RAGEnvironmentManager:
    """
    Manages an isolated Python virtual environment for RAG operations.
    
    The RAG environment is separate from the main app to:
    1. Avoid dependency conflicts (especially with PyTorch, FAISS)
    2. Allow GPU/CPU package selection
    3. Enable independent updates of RAG components
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
        self._env_path = self._base_path / 'rag_env'
        self._config_file = self._base_path / 'rag_env_config.json'
        self._status = RAGEnvStatus.NOT_CREATED
        self._error_message: Optional[str] = None
        self._install_progress: Dict[str, Any] = {}
        self._progress_callbacks: List[Callable] = []
        self._current_provider: Optional[RAGProviderChoice] = None
        self._installed_packages: List[str] = []
        
        # Check existing environment
        self._check_environment()
    
    def _check_environment(self):
        """Check if RAG environment exists and is valid"""
        if self._get_python_path().exists():
            self._status = RAGEnvStatus.READY
            self._load_config()
        else:
            self._status = RAGEnvStatus.NOT_CREATED
    
    def _get_python_path(self) -> Path:
        """Get path to Python executable in RAG env"""
        if sys.platform == 'win32':
            return self._env_path / 'Scripts' / 'python.exe'
        return self._env_path / 'bin' / 'python'
    
    def _get_pip_path(self) -> Path:
        """Get path to pip in RAG env"""
        if sys.platform == 'win32':
            return self._env_path / 'Scripts' / 'pip.exe'
        return self._env_path / 'bin' / 'pip'
    
    def _load_config(self):
        """Load RAG environment configuration"""
        if self._config_file.exists():
            try:
                with open(self._config_file, 'r') as f:
                    config = json.load(f)
                    self._installed_packages = config.get('installed_packages', [])
                    provider_str = config.get('provider')
                    if provider_str:
                        try:
                            self._current_provider = RAGProviderChoice(provider_str)
                        except ValueError:
                            pass
            except Exception as e:
                logger.error(f"Failed to load RAG env config: {e}")
    
    def _save_config(self):
        """Save RAG environment configuration"""
        self._base_path.mkdir(parents=True, exist_ok=True)
        
        config = {
            'env_path': str(self._env_path),
            'status': self._status.value,
            'provider': self._current_provider.value if self._current_provider else None,
            'installed_packages': self._installed_packages,
            'updated_at': datetime.now().isoformat()
        }
        
        with open(self._config_file, 'w') as f:
            json.dump(config, f, indent=2)
    
    def _notify_progress(self, step: str, progress: float, message: str):
        """Notify progress callbacks"""
        self._install_progress = {
            'step': step,
            'progress': progress,
            'message': message
        }
        for callback in self._progress_callbacks:
            try:
                callback(self._install_progress)
            except Exception as e:
                logger.error(f"Progress callback error: {e}")
    
    def add_progress_callback(self, callback: Callable):
        """Add a progress callback"""
        self._progress_callbacks.append(callback)
    
    def remove_progress_callback(self, callback: Callable):
        """Remove a progress callback"""
        if callback in self._progress_callbacks:
            self._progress_callbacks.remove(callback)
    
    @property
    def status(self) -> RAGEnvStatus:
        return self._status
    
    @property
    def is_ready(self) -> bool:
        return self._status == RAGEnvStatus.READY
    
    @property
    def env_path(self) -> Path:
        return self._env_path
    
    @property
    def error_message(self) -> Optional[str]:
        return self._error_message
    
    @property
    def current_provider(self) -> Optional[RAGProviderChoice]:
        return self._current_provider
    
    def get_available_providers(self) -> List[Dict[str, Any]]:
        """Get list of available RAG providers with their info"""
        providers = []
        for provider_type, info in RAG_PROVIDERS.items():
            providers.append({
                'id': provider_type.value,
                'name': info.name,
                'description': info.description,
                'features': info.features,
                'packages': info.packages,
                'has_gpu': info.gpu_packages is not None
            })
        return providers
    
    def get_status_info(self) -> Dict[str, Any]:
        """Get detailed status information"""
        return {
            'status': self._status.value,
            'env_path': str(self._env_path),
            'python_path': str(self._get_python_path()),
            'exists': self._get_python_path().exists(),
            'error': self._error_message,
            'progress': self._install_progress,
            'current_provider': self._current_provider.value if self._current_provider else None,
            'provider_info': RAG_PROVIDERS[self._current_provider].name if self._current_provider else None,
            'installed_packages': self._installed_packages,
            'available_providers': self.get_available_providers()
        }
    
    def setup_provider(self, provider: str, use_gpu: bool = False) -> Dict[str, Any]:
        """
        Set up RAG with a specific provider - creates environment and installs all required packages.
        
        This is the main method users should call. It handles everything:
        1. Creates virtual environment (if needed)
        2. Installs all required packages for the chosen provider
        3. Configures the environment
        
        Args:
            provider: Provider ID ('faiss' or 'chromadb')
            use_gpu: Use GPU-accelerated packages where available
        
        Returns:
            Status dict with success/error info
        """
        # Validate provider
        try:
            provider_choice = RAGProviderChoice(provider)
        except ValueError:
            return {'success': False, 'error': f'Unknown provider: {provider}. Available: faiss, chromadb'}
        
        provider_info = RAG_PROVIDERS[provider_choice]
        
        if self._status == RAGEnvStatus.CREATING or self._status == RAGEnvStatus.INSTALLING:
            return {'success': False, 'error': 'Setup already in progress'}
        
        self._error_message = None
        
        try:
            # Step 1: Create environment if needed
            if not self._get_python_path().exists():
                self._status = RAGEnvStatus.CREATING
                self._notify_progress('create_venv', 0.1, 'Creating virtual environment...')
                
                self._env_path.mkdir(parents=True, exist_ok=True)
                
                result = subprocess.run(
                    [sys.executable, '-m', 'venv', str(self._env_path)],
                    capture_output=True,
                    text=True
                )
                
                if result.returncode != 0:
                    raise Exception(f"Failed to create venv: {result.stderr}")
                
                # Upgrade pip
                self._notify_progress('upgrade_pip', 0.15, 'Upgrading pip...')
                pip_path = str(self._get_pip_path())
                subprocess.run(
                    [pip_path, 'install', '--upgrade', 'pip'],
                    capture_output=True,
                    text=True
                )
            
            # Step 2: Install packages for the provider
            self._status = RAGEnvStatus.INSTALLING
            packages = provider_info.gpu_packages if (use_gpu and provider_info.gpu_packages) else provider_info.packages
            
            total_packages = len(packages)
            installed = []
            
            for i, package in enumerate(packages):
                progress = 0.2 + (0.7 * (i / total_packages))
                self._notify_progress(f'install_{package}', progress, f'Installing {package}...')
                
                logger.info(f"Installing {package}...")
                pip_path = str(self._get_pip_path())
                
                result = subprocess.run(
                    [pip_path, 'install', package],
                    capture_output=True,
                    text=True
                )
                
                if result.returncode != 0:
                    logger.error(f"Failed to install {package}: {result.stderr}")
                    raise Exception(f"Failed to install {package}: {result.stderr[:200]}")
                
                installed.append(package)
            
            # Success
            self._status = RAGEnvStatus.READY
            self._current_provider = provider_choice
            self._installed_packages = installed
            self._save_config()
            
            self._notify_progress('done', 1.0, f'{provider_info.name} setup complete!')
            
            return {
                'success': True,
                'provider': provider_choice.value,
                'provider_name': provider_info.name,
                'packages_installed': installed,
                'message': f'{provider_info.name} RAG provider is ready!'
            }
            
        except Exception as e:
            self._status = RAGEnvStatus.ERROR
            self._error_message = str(e)
            logger.error(f"Failed to setup provider {provider}: {e}")
            return {'success': False, 'error': str(e)}
    
    def create_environment(self, use_gpu: bool = False) -> Dict[str, Any]:
        """
        Create the RAG virtual environment
        
        Args:
            use_gpu: If True, install GPU versions of packages (faiss-gpu, etc.)
        
        Returns:
            Status dict with success/error info
        """
        if self._status == RAGEnvStatus.CREATING:
            return {'success': False, 'error': 'Environment creation already in progress'}
        
        self._status = RAGEnvStatus.CREATING
        self._error_message = None
        
        try:
            self._notify_progress('create_venv', 0.1, 'Creating virtual environment...')
            
            # Create virtual environment
            self._env_path.mkdir(parents=True, exist_ok=True)
            
            result = subprocess.run(
                [sys.executable, '-m', 'venv', str(self._env_path)],
                capture_output=True,
                text=True
            )
            
            if result.returncode != 0:
                raise Exception(f"Failed to create venv: {result.stderr}")
            
            self._notify_progress('upgrade_pip', 0.2, 'Upgrading pip...')
            
            # Upgrade pip
            pip_path = str(self._get_pip_path())
            subprocess.run(
                [pip_path, 'install', '--upgrade', 'pip'],
                capture_output=True,
                text=True
            )
            
            self._status = RAGEnvStatus.READY
            self._save_config()
            
            self._notify_progress('done', 1.0, 'Environment created successfully')
            
            return {
                'success': True,
                'env_path': str(self._env_path),
                'message': 'RAG environment created. Install packages next.'
            }
            
        except Exception as e:
            self._status = RAGEnvStatus.ERROR
            self._error_message = str(e)
            logger.error(f"Failed to create RAG environment: {e}")
            return {'success': False, 'error': str(e)}
    
    def install_packages(self, 
                         packages: Optional[List[str]] = None,
                         use_gpu: bool = False) -> Dict[str, Any]:
        """
        Install RAG packages into the virtual environment
        
        Args:
            packages: List of package names to install (None = all required)
            use_gpu: Use GPU versions where available
        
        Returns:
            Status dict with installation results
        """
        if not self._get_python_path().exists():
            return {'success': False, 'error': 'RAG environment not created. Create it first.'}
        
        if self._status == RAGEnvStatus.INSTALLING:
            return {'success': False, 'error': 'Installation already in progress'}
        
        self._status = RAGEnvStatus.INSTALLING
        pip_path = str(self._get_pip_path())
        
        # Determine which packages to install
        if packages is None:
            packages = [name for name, pkg in RAG_PACKAGES.items() if pkg.required]
        
        results = {}
        total = len(packages)
        
        try:
            for i, pkg_name in enumerate(packages):
                if pkg_name not in RAG_PACKAGES:
                    results[pkg_name] = {'success': False, 'error': 'Unknown package'}
                    continue
                
                pkg = RAG_PACKAGES[pkg_name]
                pip_name = pkg.pip_name
                
                # Use GPU alternative if requested and available
                if use_gpu and pkg.gpu_alternative:
                    pip_name = pkg.gpu_alternative
                
                progress = (i + 0.5) / total
                self._notify_progress(
                    f'install_{pkg_name}',
                    progress,
                    f'Installing {pkg.name}...'
                )
                
                logger.info(f"Installing {pip_name}...")
                
                result = subprocess.run(
                    [pip_path, 'install', pip_name],
                    capture_output=True,
                    text=True
                )
                
                if result.returncode == 0:
                    pkg.installed = True
                    # Try to get version
                    version_result = subprocess.run(
                        [pip_path, 'show', pip_name],
                        capture_output=True,
                        text=True
                    )
                    if version_result.returncode == 0:
                        for line in version_result.stdout.split('\n'):
                            if line.startswith('Version:'):
                                pkg.version = line.split(':')[1].strip()
                                break
                    
                    results[pkg_name] = {
                        'success': True,
                        'version': pkg.version
                    }
                else:
                    pkg.installed = False
                    results[pkg_name] = {
                        'success': False,
                        'error': result.stderr[:500] if result.stderr else 'Installation failed'
                    }
            
            self._status = RAGEnvStatus.READY
            self._save_config()
            
            self._notify_progress('done', 1.0, 'Installation complete')
            
            # Check overall success
            all_required_installed = all(
                RAG_PACKAGES[name].installed 
                for name in packages 
                if name in RAG_PACKAGES and RAG_PACKAGES[name].required
            )
            
            return {
                'success': all_required_installed,
                'results': results,
                'message': 'All required packages installed' if all_required_installed else 'Some packages failed to install'
            }
            
        except Exception as e:
            self._status = RAGEnvStatus.ERROR
            self._error_message = str(e)
            logger.error(f"Package installation failed: {e}")
            return {'success': False, 'error': str(e)}
    
    def install_package(self, package_name: str, use_gpu: bool = False) -> Dict[str, Any]:
        """Install a single package"""
        return self.install_packages([package_name], use_gpu)
    
    def uninstall_package(self, package_name: str) -> Dict[str, Any]:
        """Uninstall a package from RAG environment"""
        if not self._get_python_path().exists():
            return {'success': False, 'error': 'RAG environment not created'}
        
        if package_name not in RAG_PACKAGES:
            return {'success': False, 'error': 'Unknown package'}
        
        pkg = RAG_PACKAGES[package_name]
        pip_path = str(self._get_pip_path())
        
        result = subprocess.run(
            [pip_path, 'uninstall', '-y', pkg.pip_name],
            capture_output=True,
            text=True
        )
        
        if result.returncode == 0:
            pkg.installed = False
            pkg.version = None
            self._save_config()
            return {'success': True}
        
        return {'success': False, 'error': result.stderr}
    
    def get_installed_packages(self) -> List[Dict[str, Any]]:
        """Get list of installed packages in RAG environment"""
        if not self._get_python_path().exists():
            return []
        
        pip_path = str(self._get_pip_path())
        
        result = subprocess.run(
            [pip_path, 'list', '--format=json'],
            capture_output=True,
            text=True
        )
        
        if result.returncode == 0:
            try:
                return json.loads(result.stdout)
            except:
                pass
        
        return []
    
    def check_package(self, package_name: str) -> bool:
        """Check if a package is installed and importable"""
        if not self._get_python_path().exists():
            return False
        
        python_path = str(self._get_python_path())
        
        # Map package name to import name
        import_map = {
            'faiss': 'faiss',
            'sentence_transformers': 'sentence_transformers',
            'chromadb': 'chromadb',
            'numpy': 'numpy',
            'torch': 'torch'
        }
        
        import_name = import_map.get(package_name, package_name)
        
        result = subprocess.run(
            [python_path, '-c', f'import {import_name}'],
            capture_output=True,
            text=True
        )
        
        return result.returncode == 0
    
    def run_in_env(self, 
                   script: str, 
                   args: Optional[List[str]] = None,
                   timeout: Optional[float] = None) -> Dict[str, Any]:
        """
        Run a Python script in the RAG environment
        
        Args:
            script: Python code to execute
            args: Command line arguments
            timeout: Timeout in seconds
        
        Returns:
            Dict with stdout, stderr, returncode
        """
        if not self._get_python_path().exists():
            return {
                'success': False,
                'error': 'RAG environment not created',
                'stdout': '',
                'stderr': '',
                'returncode': -1
            }
        
        python_path = str(self._get_python_path())
        
        cmd = [python_path, '-c', script]
        if args:
            cmd.extend(args)
        
        try:
            result = subprocess.run(
                cmd,
                capture_output=True,
                text=True,
                timeout=timeout
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
                'error': 'Script execution timed out',
                'stdout': '',
                'stderr': '',
                'returncode': -1
            }
        except Exception as e:
            return {
                'success': False,
                'error': str(e),
                'stdout': '',
                'stderr': '',
                'returncode': -1
            }
    
    def delete_environment(self) -> Dict[str, Any]:
        """Delete the RAG virtual environment"""
        import shutil
        
        if not self._env_path.exists():
            return {'success': True, 'message': 'Environment does not exist'}
        
        try:
            shutil.rmtree(self._env_path)
            self._status = RAGEnvStatus.NOT_CREATED
            
            # Reset package status
            for pkg in RAG_PACKAGES.values():
                pkg.installed = False
                pkg.version = None
            
            self._save_config()
            
            return {'success': True, 'message': 'RAG environment deleted'}
        except Exception as e:
            return {'success': False, 'error': str(e)}
    
    def get_env_python(self) -> Optional[str]:
        """Get path to Python executable in RAG env, or None if not ready"""
        if self.is_ready and self._get_python_path().exists():
            return str(self._get_python_path())
        return None
    
    # =====================
    # Backup & Restore
    # =====================
    
    def get_data_path(self) -> Path:
        """Get the RAG data storage path"""
        return self._base_path / 'rag_data'
    
    def get_backup_path(self) -> Path:
        """Get the RAG backup storage path"""
        return self._base_path / 'rag_backups'
    
    def list_backups(self) -> List[Dict[str, Any]]:
        """List available RAG data backups"""
        backup_path = self.get_backup_path()
        if not backup_path.exists():
            return []
        
        backups = []
        for item in backup_path.iterdir():
            if item.is_dir() and item.name.startswith('backup_'):
                # Parse backup info
                try:
                    info_file = item / 'backup_info.json'
                    if info_file.exists():
                        with open(info_file, 'r') as f:
                            info = json.load(f)
                    else:
                        info = {'name': item.name}
                    
                    info['path'] = str(item)
                    info['size'] = sum(f.stat().st_size for f in item.rglob('*') if f.is_file())
                    backups.append(info)
                except Exception as e:
                    logger.error(f"Error reading backup {item}: {e}")
        
        # Sort by date, newest first
        backups.sort(key=lambda x: x.get('created_at', ''), reverse=True)
        return backups
    
    def create_backup(self, name: Optional[str] = None) -> Dict[str, Any]:
        """
        Create a backup of RAG data (collections, documents, indices)
        
        Args:
            name: Optional backup name (default: timestamp-based)
        
        Returns:
            Status dict with backup info
        """
        import shutil
        from datetime import datetime
        
        data_path = self.get_data_path()
        if not data_path.exists():
            return {'success': False, 'error': 'No RAG data to backup'}
        
        backup_path = self.get_backup_path()
        backup_path.mkdir(parents=True, exist_ok=True)
        
        # Generate backup name
        timestamp = datetime.now().strftime('%Y%m%d_%H%M%S')
        backup_name = name or f"backup_{timestamp}"
        backup_dir = backup_path / backup_name
        
        if backup_dir.exists():
            return {'success': False, 'error': f'Backup {backup_name} already exists'}
        
        try:
            # Copy all RAG data
            shutil.copytree(data_path, backup_dir)
            
            # Create backup info file
            info = {
                'name': backup_name,
                'created_at': datetime.now().isoformat(),
                'source_path': str(data_path),
                'files_count': sum(1 for _ in backup_dir.rglob('*') if _.is_file()),
                'size_bytes': sum(f.stat().st_size for f in backup_dir.rglob('*') if f.is_file())
            }
            
            with open(backup_dir / 'backup_info.json', 'w') as f:
                json.dump(info, f, indent=2)
            
            return {
                'success': True,
                'backup_name': backup_name,
                'backup_path': str(backup_dir),
                'info': info
            }
        except Exception as e:
            # Cleanup on failure
            if backup_dir.exists():
                shutil.rmtree(backup_dir, ignore_errors=True)
            return {'success': False, 'error': str(e)}
    
    def restore_backup(self, backup_name: str, overwrite: bool = False) -> Dict[str, Any]:
        """
        Restore RAG data from a backup
        
        Args:
            backup_name: Name of the backup to restore
            overwrite: If True, overwrite existing data
        
        Returns:
            Status dict
        """
        import shutil
        
        backup_path = self.get_backup_path() / backup_name
        if not backup_path.exists():
            return {'success': False, 'error': f'Backup {backup_name} not found'}
        
        data_path = self.get_data_path()
        
        if data_path.exists() and not overwrite:
            return {
                'success': False, 
                'error': 'RAG data already exists. Set overwrite=True to replace.'
            }
        
        try:
            # Remove existing data if overwriting
            if data_path.exists():
                shutil.rmtree(data_path)
            
            # Copy backup to data path (exclude backup_info.json)
            shutil.copytree(
                backup_path, 
                data_path,
                ignore=shutil.ignore_patterns('backup_info.json')
            )
            
            return {
                'success': True,
                'message': f'Restored from backup {backup_name}',
                'restored_path': str(data_path)
            }
        except Exception as e:
            return {'success': False, 'error': str(e)}
    
    def delete_backup(self, backup_name: str) -> Dict[str, Any]:
        """Delete a backup"""
        import shutil
        
        backup_path = self.get_backup_path() / backup_name
        if not backup_path.exists():
            return {'success': False, 'error': f'Backup {backup_name} not found'}
        
        try:
            shutil.rmtree(backup_path)
            return {'success': True, 'message': f'Deleted backup {backup_name}'}
        except Exception as e:
            return {'success': False, 'error': str(e)}
    
    def export_data(self, export_path: str) -> Dict[str, Any]:
        """
        Export RAG data to a zip file
        
        Args:
            export_path: Path for the zip file
        
        Returns:
            Status dict
        """
        import shutil
        
        data_path = self.get_data_path()
        if not data_path.exists():
            return {'success': False, 'error': 'No RAG data to export'}
        
        try:
            # Create zip archive
            export_file = Path(export_path)
            if not export_file.suffix:
                export_file = export_file.with_suffix('.zip')
            
            shutil.make_archive(
                str(export_file.with_suffix('')),
                'zip',
                data_path
            )
            
            return {
                'success': True,
                'export_path': str(export_file),
                'size_bytes': export_file.stat().st_size
            }
        except Exception as e:
            return {'success': False, 'error': str(e)}
    
    def import_data(self, import_path: str, overwrite: bool = False) -> Dict[str, Any]:
        """
        Import RAG data from a zip file
        
        Args:
            import_path: Path to the zip file
            overwrite: If True, overwrite existing data
        
        Returns:
            Status dict
        """
        import shutil
        import zipfile
        
        import_file = Path(import_path)
        if not import_file.exists():
            return {'success': False, 'error': f'Import file not found: {import_path}'}
        
        if not zipfile.is_zipfile(import_file):
            return {'success': False, 'error': 'Invalid zip file'}
        
        data_path = self.get_data_path()
        
        if data_path.exists() and not overwrite:
            return {
                'success': False,
                'error': 'RAG data already exists. Set overwrite=True to replace.'
            }
        
        try:
            # Remove existing data if overwriting
            if data_path.exists():
                shutil.rmtree(data_path)
            
            data_path.mkdir(parents=True, exist_ok=True)
            
            # Extract zip
            with zipfile.ZipFile(import_file, 'r') as zf:
                zf.extractall(data_path)
            
            return {
                'success': True,
                'message': 'RAG data imported successfully',
                'data_path': str(data_path)
            }
        except Exception as e:
            return {'success': False, 'error': str(e)}
    
    def clear_data(self, backup_first: bool = True) -> Dict[str, Any]:
        """
        Clear all RAG data (collections, documents, indices)
        
        Args:
            backup_first: Create a backup before clearing
        
        Returns:
            Status dict
        """
        import shutil
        
        data_path = self.get_data_path()
        if not data_path.exists():
            return {'success': True, 'message': 'No data to clear'}
        
        backup_info = None
        if backup_first:
            backup_result = self.create_backup(name=f"pre_clear_{datetime.now().strftime('%Y%m%d_%H%M%S')}")
            if backup_result.get('success'):
                backup_info = backup_result.get('info')
            else:
                return {
                    'success': False,
                    'error': f"Failed to create backup: {backup_result.get('error')}"
                }
        
        try:
            shutil.rmtree(data_path)
            data_path.mkdir(parents=True, exist_ok=True)
            
            return {
                'success': True,
                'message': 'RAG data cleared',
                'backup': backup_info
            }
        except Exception as e:
            return {'success': False, 'error': str(e)}


# Singleton instance
def get_rag_environment() -> RAGEnvironmentManager:
    """Get the RAG environment manager singleton"""
    return RAGEnvironmentManager()
