"""
RAG Environment Manager

Manages the single shared virtual environment for all RAG databases.
Handles venv creation, package installation, and database switching.
"""
import os
import sys
import json
import platform
import subprocess
import threading
from pathlib import Path
from typing import Dict, Any, List, Optional, Callable


class RAGEnvironmentManager:
    """Manager for RAG virtual environment"""
    
    def __init__(self):
        self.base_path = Path.home() / '.beep-rag'
        self.venv_path = self.base_path / 'venv'
        self.databases_path = self.base_path / 'databases'
        self.config_path = self.base_path / 'config'
        self.active_db_file = self.config_path / 'active_database.json'
        self.env_config_file = self.config_path / 'environment.json'
        self.is_windows = platform.system() == 'Windows'
        
        # Progress tracking for wizard
        self._installation_progress: Dict[str, Any] = {}
        
        # Ensure directories exist
        self.base_path.mkdir(parents=True, exist_ok=True)
        self.databases_path.mkdir(parents=True, exist_ok=True)
        self.config_path.mkdir(parents=True, exist_ok=True)
        
        # Load or initialize config
        self._load_config()
    
    def _load_config(self):
        """Load environment config from JSON file"""
        if self.env_config_file.exists():
            try:
                with open(self.env_config_file, 'r') as f:
                    self.config = json.load(f)
            except Exception:
                self.config = {'installed': False, 'packages': []}
        else:
            self.config = {'installed': False, 'packages': []}
    
    def _save_config(self):
        """Save environment config to JSON file"""
        try:
            with open(self.env_config_file, 'w') as f:
                json.dump(self.config, f, indent=2)
        except Exception as e:
            print(f"Error saving config: {e}")
    
    def _get_python_executable(self) -> Path:
        """Get platform-specific Python executable from RAG venv"""
        if self.is_windows:
            return self.venv_path / 'Scripts' / 'python.exe'
        else:
            return self.venv_path / 'bin' / 'python3'
    
    def _get_pip_executable(self) -> Path:
        """Get platform-specific pip executable from RAG venv"""
        if self.is_windows:
            return self.venv_path / 'Scripts' / 'pip.exe'
        else:
            return self.venv_path / 'bin' / 'pip'
    
    def is_venv_installed(self) -> bool:
        """Check if RAG venv is installed (from config file)"""
        return self.config.get('installed', False)
    
    def create_venv(self, callback: Optional[Callable[[str], None]] = None) -> bool:
        """
        Create RAG virtual environment
        
        Args:
            callback: Optional callback for progress updates
        
        Returns:
            True if successful, False otherwise
        """
        try:
            if callback:
                callback("Creating RAG virtual environment...")
            
            # Create venv
            subprocess.run(
                [sys.executable, '-m', 'venv', str(self.venv_path)],
                check=True,
                capture_output=True
            )
            
            if callback:
                callback("Virtual environment created successfully")
            
            return True
            
        except Exception as e:
            if callback:
                callback(f"Error creating venv: {e}")
            return False
    
    def install_packages(self, callback: Optional[Callable[[str], None]] = None,
                         install_chromadb: bool = True,
                         install_faiss: bool = True) -> bool:
        """
        Install RAG packages (FAISS, ChromaDB, sentence-transformers)
        
        Args:
            callback: Optional callback for progress updates
            install_chromadb: Whether to install ChromaDB
            install_faiss: Whether to install FAISS
        
        Returns:
            True if successful, False otherwise
        """
        pip_exe = self._get_pip_executable()
        
        # Build package list based on selection
        packages = []
        if install_faiss:
            packages.append(('faiss-cpu', 'FAISS (CPU)'))
        if install_chromadb:
            packages.append(('chromadb', 'ChromaDB'))
        
        # Always install these
        packages.extend([
            ('sentence-transformers', 'Sentence Transformers'),
            ('langchain', 'LangChain'),
            ('langchain-community', 'LangChain Community'),
            ('numpy', 'NumPy')
        ])
        
        python_exe = self._get_python_executable()
        
        try:
            # Try to upgrade pip first (non-fatal if it fails)
            if callback:
                callback("Upgrading pip...")
            
            try:
                # Use python -m pip to avoid Windows locking issues
                subprocess.run(
                    [str(python_exe), '-m', 'pip', 'install', '--upgrade', 'pip'],
                    check=True,
                    capture_output=True,
                    timeout=120
                )
            except Exception as pip_err:
                # Pip upgrade failed, but continue anyway
                if callback:
                    callback(f"Pip upgrade skipped (non-critical): {pip_err}")
            
            # Install each package
            installed_packages = []
            for package, display_name in packages:
                if callback:
                    callback(f"Installing {display_name}...")
                
                # Use python -m pip to avoid Windows locking issues
                subprocess.run(
                    [str(python_exe), '-m', 'pip', 'install', package],
                    check=True,
                    capture_output=True,
                    timeout=300  # 5 minute timeout per package
                )
                
                installed_packages.append({
                    'name': package,
                    'display': display_name,
                    'installed': True
                })
                
                if callback:
                    callback(f"{display_name} installed successfully")
            
            # Update config
            self.config['installed'] = True
            self.config['packages'] = installed_packages
            self._save_config()
            
            return True
            
        except Exception as e:
            if callback:
                callback(f"Error installing packages: {e}")
            return False
    
    def setup_environment_async(self, install_chromadb: bool = True, 
                                 install_faiss: bool = True) -> Dict[str, Any]:
        """
        Setup RAG environment in background thread with progress tracking.
        Used by the wizard.
        
        Args:
            install_chromadb: Whether to install ChromaDB
            install_faiss: Whether to install FAISS
            
        Returns:
            Initial status dict
        """
        self._installation_progress = {
            'status': 'starting',
            'message': 'Preparing installation...',
            'percent': 0,
            'complete': False,
            'success': False,
            'packages': []
        }
        
        def run_setup():
            try:
                python_exe = self._get_python_executable()
                
                # Step 1: Create venv if needed
                self._installation_progress['message'] = 'Creating virtual environment...'
                self._installation_progress['percent'] = 5
                
                if not self.venv_path.exists():
                    subprocess.run(
                        [sys.executable, '-m', 'venv', str(self.venv_path)],
                        check=True,
                        capture_output=True
                    )
                
                # Step 2: Try to upgrade pip (non-fatal if it fails)
                self._installation_progress['message'] = 'Upgrading pip...'
                self._installation_progress['percent'] = 10
                
                try:
                    subprocess.run(
                        [str(python_exe), '-m', 'pip', 'install', '--upgrade', 'pip'],
                        check=True,
                        capture_output=True,
                        timeout=120
                    )
                except Exception:
                    # Pip upgrade failed, continue anyway
                    pass
                
                # Build package list
                packages = []
                if install_faiss:
                    packages.append(('faiss-cpu', 'FAISS (CPU)'))
                if install_chromadb:
                    packages.append(('chromadb', 'ChromaDB'))
                packages.extend([
                    ('sentence-transformers', 'Sentence Transformers'),
                    ('langchain', 'LangChain'),
                    ('langchain-community', 'LangChain Community'),
                    ('numpy', 'NumPy')
                ])
                
                # Calculate progress per package
                total_packages = len(packages)
                progress_per_package = 80 / total_packages  # 10-90%
                
                installed_packages = []
                for i, (package, display_name) in enumerate(packages):
                    self._installation_progress['message'] = f'Installing {display_name}...'
                    self._installation_progress['percent'] = int(10 + (i * progress_per_package))
                    
                    # Use python -m pip to avoid Windows locking issues
                    subprocess.run(
                        [str(python_exe), '-m', 'pip', 'install', package],
                        check=True,
                        capture_output=True,
                        timeout=300  # 5 minute timeout per package
                    )
                    
                    installed_packages.append({
                        'name': package,
                        'display': display_name,
                        'installed': True
                    })
                
                # Step 3: Finalize
                self._installation_progress['message'] = 'Finalizing setup...'
                self._installation_progress['percent'] = 95
                
                # Update config
                self.config['installed'] = True
                self.config['packages'] = installed_packages
                self._save_config()
                
                # Complete
                self._installation_progress['status'] = 'completed'
                self._installation_progress['message'] = 'Installation complete!'
                self._installation_progress['percent'] = 100
                self._installation_progress['complete'] = True
                self._installation_progress['success'] = True
                self._installation_progress['packages'] = installed_packages
                
            except Exception as e:
                self._installation_progress['status'] = 'error'
                self._installation_progress['message'] = f'Error: {str(e)}'
                self._installation_progress['complete'] = True
                self._installation_progress['success'] = False
                self._installation_progress['error'] = str(e)
        
        # Run in background thread
        thread = threading.Thread(target=run_setup)
        thread.daemon = True
        thread.start()
        
        return {
            'success': True,
            'message': 'Installation started',
            'progress_endpoint': '/rag/api/env/progress'
        }
    
    def get_installation_progress(self) -> Dict[str, Any]:
        """Get current installation progress for wizard"""
        return self._installation_progress.copy()
    
    def get_installed_packages(self) -> List[Dict[str, Any]]:
        """Get list of installed packages from config"""
        return self.config.get('packages', [])
    
    def get_active_database(self) -> Optional[str]:
        """Get the currently active database name"""
        if not self.active_db_file.exists():
            return None
        
        try:
            with open(self.active_db_file, 'r') as f:
                data = json.load(f)
                return data.get('active_database')
        except Exception:
            return None
    
    def set_active_database(self, db_name: str) -> bool:
        """Set the active database"""
        try:
            with open(self.active_db_file, 'w') as f:
                json.dump({'active_database': db_name}, f)
            return True
        except Exception:
            return False
    
    def list_databases(self) -> List[Dict[str, Any]]:
        """List all RAG databases"""
        if not self.databases_path.exists():
            return []
        
        active_db = self.get_active_database()
        databases = []
        
        for db_dir in self.databases_path.iterdir():
            if db_dir.is_dir():
                # Determine provider type
                provider = 'unknown'
                if (db_dir / 'index.faiss').exists():
                    provider = 'faiss'
                elif (db_dir / 'chroma.sqlite3').exists():
                    provider = 'chromadb'
                
                # Count documents (approximate)
                doc_count = 0
                metadata_file = db_dir / 'metadata.json'
                if metadata_file.exists():
                    try:
                        with open(metadata_file, 'r') as f:
                            metadata = json.load(f)
                            doc_count = metadata.get('document_count', 0)
                    except Exception:
                        pass
                
                databases.append({
                    'name': db_dir.name,
                    'provider': provider,
                    'path': str(db_dir),
                    'is_active': db_dir.name == active_db,
                    'document_count': doc_count
                })
        
        return databases
    
    def get_venv_status(self) -> Dict[str, Any]:
        """Get comprehensive RAG venv status"""
        installed = self.is_venv_installed()
        
        if not installed:
            return {
                'installed': False,
                'path': str(self.venv_path),
                'packages': [],
                'active_database': None,
                'databases': []
            }
        
        return {
            'installed': True,
            'path': str(self.venv_path),
            'python_exe': str(self._get_python_executable()),
            'packages': self.get_installed_packages(),
            'active_database': self.get_active_database(),
            'databases': self.list_databases(),
            'database_count': len(self.list_databases())
        }


def get_rag_environment_manager() -> RAGEnvironmentManager:
    """Get singleton RAG environment manager instance"""
    if not hasattr(get_rag_environment_manager, '_instance'):
        get_rag_environment_manager._instance = RAGEnvironmentManager()
    return get_rag_environment_manager._instance
