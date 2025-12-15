"""
Document Extraction Environment Manager

Manages an isolated virtual environment for document extraction libraries:
- PyMuPDF (PDF)
- python-docx (Word)
- openpyxl (Excel)
- python-pptx (PowerPoint)
- xlrd (old Excel)
- Other document processing libraries

This ensures document extraction packages don't conflict with the main application.
"""
import os
import sys
import json
import subprocess
import threading
import platform
from pathlib import Path
from typing import Optional, List, Dict, Any, Callable
from dataclasses import dataclass, field
from enum import Enum
from datetime import datetime
import logging

logger = logging.getLogger(__name__)


class DocExtEnvStatus(Enum):
    """Document extraction environment status"""
    NOT_CREATED = "not_created"
    CREATING = "creating"
    READY = "ready"
    INSTALLING = "installing"
    ERROR = "error"


@dataclass
class DocExtPackage:
    """Information about a document extraction package"""
    name: str
    pip_name: str
    import_name: str  # The actual module name to import
    description: str
    required: bool = False
    installed: bool = False
    version: Optional[str] = None


# Define available document extraction packages
DOC_EXTRACTION_PACKAGES: Dict[str, DocExtPackage] = {
    'pymupdf': DocExtPackage(
        name='PyMuPDF',
        pip_name='pymupdf',
        import_name='fitz',  # PyMuPDF imports as 'fitz'
        description='Fast PDF text extraction (recommended)',
        required=True
    ),
    'pypdf': DocExtPackage(
        name='PyPDF',
        pip_name='pypdf',
        import_name='pypdf',  # Can also be PyPDF2
        description='Alternative PDF library (fallback)',
        required=False
    ),
    'python-docx': DocExtPackage(
        name='Python-docx',
        pip_name='python-docx',
        import_name='docx',  # python-docx imports as 'docx'
        description='Microsoft Word (.docx) file processing',
        required=True
    ),
    'openpyxl': DocExtPackage(
        name='OpenPyXL',
        pip_name='openpyxl',
        import_name='openpyxl',
        description='Excel (.xlsx) file processing',
        required=True
    ),
    'xlrd': DocExtPackage(
        name='xlrd',
        pip_name='xlrd',
        import_name='xlrd',
        description='Legacy Excel (.xls) file support',
        required=False
    ),
    'python-pptx': DocExtPackage(
        name='Python-pptx',
        pip_name='python-pptx',
        import_name='pptx',  # python-pptx imports as 'pptx'
        description='PowerPoint (.pptx) file processing',
        required=False
    ),
    'unstructured': DocExtPackage(
        name='Unstructured',
        pip_name='unstructured',
        import_name='unstructured',
        description='Advanced document parsing and preprocessing',
        required=False
    ),
    'easyocr': DocExtPackage(
        name='EasyOCR',
        pip_name='easyocr',
        import_name='easyocr',
        description='Easy to use OCR, supports 80+ languages',
        required=False
    ),
    'pytesseract': DocExtPackage(
        name='Tesseract OCR',
        pip_name='pytesseract',
        import_name='pytesseract',
        description='Highly accurate OCR for printed text, supports 100+ languages',
        required=False
    ),
    'paddleocr': DocExtPackage(
        name='PaddleOCR',
        pip_name='paddleocr',
        import_name='paddleocr',
        description='Fast and accurate OCR, excellent for Chinese/English',
        required=False
    ),
    'opencv-python': DocExtPackage(
        name='OpenCV',
        pip_name='opencv-python',
        import_name='cv2',
        description='Image preprocessing to improve OCR accuracy',
        required=False
    ),
    'easyocr': DocExtPackage(
        name='EasyOCR',
        pip_name='easyocr',
        import_name='easyocr',
        description='OCR (Optical Character Recognition) - Pure Python, no external dependencies',
        required=False
    ),
    'pillow': DocExtPackage(
        name='Pillow',
        pip_name='Pillow',
        import_name='PIL',
        description='Image processing library (required for OCR)',
        required=False
    ),
}


class DocumentExtractionEnvironmentManager:
    """
    Manages an isolated Python virtual environment for document extraction operations.
    
    The document extraction environment is separate from the main app to:
    1. Avoid dependency conflicts
    2. Keep extraction libraries isolated
    3. Enable independent updates
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
        
        # Load paths from settings, or use defaults
        from app.models.core import Setting
        
        # Environment path - use providers directory via EnvironmentManager
        providers_path = self._base_path / 'providers'
        env_path_setting = Setting.get('doc_extraction_env_path')
        if env_path_setting:
            self._env_path = Path(env_path_setting)
        else:
            self._env_path = providers_path / 'document_extraction'
            # Save default to settings
            Setting.set('doc_extraction_env_path', str(self._env_path), 
                       'Document extraction virtual environment path')
        
        # Extracted documents storage path
        extracted_docs_path_setting = Setting.get('doc_extraction_storage_path')
        if extracted_docs_path_setting:
            self._storage_path = Path(extracted_docs_path_setting)
        else:
            self._storage_path = self._base_path / 'extracted_documents'
            self._storage_path.mkdir(parents=True, exist_ok=True)
            # Save default to settings
            Setting.set('doc_extraction_storage_path', str(self._storage_path),
                       'Path where extracted document text files are stored')
        
        self._config_file = self._base_path / 'doc_extraction_env_config.json'
        self._status = DocExtEnvStatus.NOT_CREATED
        self._error_message: Optional[str] = None
        self._install_progress: Dict[str, Any] = {}
        self._progress_callbacks: List[Callable] = []
        self._installed_packages: List[str] = []
        self.is_windows = platform.system() == 'Windows'
        
        # Check existing environment
        self._check_environment()
    
    def _check_environment(self):
        """Check if document extraction environment exists and is valid"""
        if self._get_python_path().exists():
            self._status = DocExtEnvStatus.READY
            self._load_config()
            self._check_installed_packages()
        else:
            self._status = DocExtEnvStatus.NOT_CREATED
    
    def _get_python_path(self) -> Path:
        """Get path to Python executable in document extraction env"""
        if self.is_windows:
            return self._env_path / 'Scripts' / 'python.exe'
        return self._env_path / 'bin' / 'python'
    
    def _get_pip_path(self) -> Path:
        """Get path to pip in document extraction env"""
        if self.is_windows:
            return self._env_path / 'Scripts' / 'pip.exe'
        return self._env_path / 'bin' / 'pip'
    
    def _load_config(self):
        """Load document extraction environment configuration"""
        if self._config_file.exists():
            try:
                with open(self._config_file, 'r') as f:
                    config = json.load(f)
                    self._installed_packages = config.get('installed_packages', [])
            except Exception as e:
                logger.error(f"Failed to load doc extraction env config: {e}")
    
    def _save_config(self):
        """Save document extraction environment configuration"""
        self._base_path.mkdir(parents=True, exist_ok=True)
        
        config = {
            'env_path': str(self._env_path),
            'status': self._status.value,
            'installed_packages': self._installed_packages,
            'updated_at': datetime.now().isoformat()
        }
        
        with open(self._config_file, 'w') as f:
            json.dump(config, f, indent=2)
    
    def _check_installed_packages(self):
        """Check which packages are actually installed"""
        if not self._get_python_path().exists():
            return
        
        python_exe = self._get_python_path()
        
        for pkg_name, pkg_info in DOC_EXTRACTION_PACKAGES.items():
            try:
                # Use the correct import name for each package
                import_name = pkg_info.import_name
                
                # Special case for pypdf - check both pypdf and PyPDF2
                if pkg_name == 'pypdf':
                    result1 = subprocess.run(
                        [str(python_exe), '-c', 'import pypdf; print("ok")'],
                        capture_output=True,
                        text=True,
                        timeout=5
                    )
                    result2 = subprocess.run(
                        [str(python_exe), '-c', 'import PyPDF2; print("ok")'],
                        capture_output=True,
                        text=True,
                        timeout=5
                    )
                    pkg_info.installed = result1.returncode == 0 or result2.returncode == 0
                # Special case for easyocr - it can take time to import on first run
                elif pkg_name == 'easyocr':
                    result = subprocess.run(
                        [str(python_exe), '-c', 'import easyocr; print("ok")'],
                        capture_output=True,
                        text=True,
                        timeout=30  # Give it more time, first import can be slow
                    )
                    pkg_info.installed = result.returncode == 0
                    if result.returncode != 0:
                        logger.debug(f"EasyOCR import failed: {result.stderr[:200]}")
                # Special case for Pillow - imports as PIL
                elif pkg_name == 'pillow':
                    result = subprocess.run(
                        [str(python_exe), '-c', 'from PIL import Image; print("ok")'],
                        capture_output=True,
                        text=True,
                        timeout=5
                    )
                    pkg_info.installed = result.returncode == 0
                # Special case for pytesseract - check if tesseract binary is also available
                elif pkg_name == 'pytesseract':
                    result = subprocess.run(
                        [str(python_exe), '-c', 'import pytesseract; print("ok")'],
                        capture_output=True,
                        text=True,
                        timeout=10
                    )
                    pkg_info.installed = result.returncode == 0
                # Special case for paddleocr - can take time to import
                elif pkg_name == 'paddleocr':
                    result = subprocess.run(
                        [str(python_exe), '-c', 'from paddleocr import PaddleOCR; print("ok")'],
                        capture_output=True,
                        text=True,
                        timeout=30
                    )
                    pkg_info.installed = result.returncode == 0
                # Special case for opencv-python - imports as cv2
                elif pkg_name == 'opencv-python':
                    result = subprocess.run(
                        [str(python_exe), '-c', 'import cv2; print("ok")'],
                        capture_output=True,
                        text=True,
                        timeout=5
                    )
                    pkg_info.installed = result.returncode == 0
                else:
                    result = subprocess.run(
                        [str(python_exe), '-c', f'import {import_name}; print("ok")'],
                        capture_output=True,
                        text=True,
                        timeout=5
                    )
                    pkg_info.installed = result.returncode == 0
                    if not pkg_info.installed and result.stderr:
                        logger.debug(f"Package {pkg_name} ({import_name}) import failed: {result.stderr[:200]}")
            except subprocess.TimeoutExpired:
                logger.warning(f"Timeout checking {pkg_name}")
                pkg_info.installed = False
            except Exception as e:
                logger.debug(f"Failed to check {pkg_name}: {e}")
                pkg_info.installed = False
    
    def _notify_progress(self, step: str, progress: float, message: str):
        """Notify progress callbacks"""
        self._install_progress = {
            'step': step,
            'progress': progress,
            'message': message
        }
        for callback in self._progress_callbacks:
            try:
                callback(step, progress, message)
            except Exception as e:
                logger.error(f"Progress callback error: {e}")
    
    def register_progress_callback(self, callback: Callable[[str, float, str], None]):
        """Register a callback for installation progress"""
        if callback not in self._progress_callbacks:
            self._progress_callbacks.append(callback)
    
    def get_status(self) -> Dict[str, Any]:
        """Get current environment status"""
        return {
            'status': self._status.value,
            'env_path': str(self._env_path),
            'storage_path': str(self._storage_path),
            'python_path': str(self._get_python_path()) if self._get_python_path().exists() else None,
            'installed_packages': self._installed_packages,
            'error': self._error_message,
            'progress': self._install_progress,
            'packages': {
                name: {
                    'name': pkg.name,
                    'pip_name': pkg.pip_name,
                    'description': pkg.description,
                    'required': pkg.required,
                    'installed': pkg.installed
                }
                for name, pkg in DOC_EXTRACTION_PACKAGES.items()
            }
        }
    
    def create_environment(self, progress_callback: Optional[Callable] = None) -> Dict[str, Any]:
        """Create the document extraction virtual environment using EnvironmentManager"""
        if self._status == DocExtEnvStatus.CREATING or self._status == DocExtEnvStatus.INSTALLING:
            return {'success': False, 'error': 'Environment creation already in progress'}
        
        if progress_callback:
            self.register_progress_callback(progress_callback)
        
        self._status = DocExtEnvStatus.CREATING
        self._error_message = None
        
        try:
            self._notify_progress('creating', 10, 'Creating virtual environment...')
            
            # Use centralized EnvironmentManager
            from app.services.environment_manager import EnvironmentManager
            from app.config_manager import get_app_directory
            
            env_mgr = EnvironmentManager(base_path=str(get_app_directory()))
            env_name = 'document_extraction'
            
            # Check if environment already exists
            existing_envs = env_mgr.list_environments()
            existing = next((e for e in existing_envs if e.name == env_name), None)
            
            if existing:
                # Environment already exists, use it
                self._env_path = Path(existing.path)
                self._notify_progress('creating', 50, 'Using existing environment')
            else:
                # Create new environment using EnvironmentManager
                self._notify_progress('creating', 20, 'Creating virtual environment via EnvironmentManager...')
                virtual_env = env_mgr.create_environment(name=env_name)
                self._env_path = Path(virtual_env.path)
                self._notify_progress('creating', 50, 'Virtual environment created')
            
            # Upgrade pip
            self._notify_progress('installing', 60, 'Upgrading pip...')
            pip_exe = self._get_pip_path()
            subprocess.run(
                [str(pip_exe), 'install', '--upgrade', 'pip', '--quiet'],
                capture_output=True,
                timeout=60
            )
            
            self._status = DocExtEnvStatus.READY
            self._save_config()
            self._notify_progress('complete', 100, 'Environment ready')
            
            return {'success': True, 'message': 'Environment created successfully'}
            
        except Exception as e:
            self._status = DocExtEnvStatus.ERROR
            self._error_message = str(e)
            self._save_config()
            self._notify_progress('error', 0, f'Error: {str(e)}')
            return {'success': False, 'error': str(e)}
    
    def install_packages(self, package_names: Optional[List[str]] = None,
                        progress_callback: Optional[Callable] = None) -> Dict[str, Any]:
        """Install document extraction packages"""
        if not self._get_python_path().exists():
            error_msg = f'Environment not created. Create it first. Python path: {self._get_python_path()}'
            logger.error(error_msg)
            return {'success': False, 'error': error_msg}
        
        pip_exe = self._get_pip_path()
        if not pip_exe.exists():
            error_msg = f'pip not found in environment. Path: {pip_exe}'
            logger.error(error_msg)
            return {'success': False, 'error': error_msg}
        
        if progress_callback:
            self.register_progress_callback(progress_callback)
        
        self._status = DocExtEnvStatus.INSTALLING
        self._error_message = None
        
        try:
            logger.info(f"Starting package installation. Pip: {pip_exe}, Packages: {package_names}")
            
            # Determine which packages to install
            if package_names is None:
                # Install required packages by default
                package_names = [pkg.pip_name for pkg in DOC_EXTRACTION_PACKAGES.values() if pkg.required]
            
            # Parse package names (remove comments if present)
            clean_package_names = []
            for pkg in package_names:
                # Remove comments (everything after #)
                pkg_clean = pkg.split('#')[0].strip()
                if pkg_clean:
                    clean_package_names.append(pkg_clean)
            
            package_names = clean_package_names
            
            if not package_names:
                return {'success': False, 'error': 'No packages specified for installation'}
            
            logger.info(f"Installing {len(package_names)} packages: {', '.join(package_names)}")
            
            total = len(package_names)
            installed = []
            failed = []
            
            for i, pkg_name in enumerate(package_names):
                self._notify_progress('installing', int((i / total) * 90), f'Installing {pkg_name}...')
                logger.info(f"Installing package {i+1}/{total}: {pkg_name}")
                
                try:
                    # Use --no-warn-script-location to reduce noise, but capture errors
                    # Don't use --quiet for better error visibility, but redirect to stderr
                    result = subprocess.run(
                        [str(pip_exe), 'install', pkg_name, '--no-warn-script-location'],
                        capture_output=True,
                        text=True,
                        timeout=600  # 10 minutes for large packages like easyocr
                    )
                    
                    if result.returncode == 0:
                        installed.append(pkg_name)
                        if pkg_name not in self._installed_packages:
                            self._installed_packages.append(pkg_name)
                        logger.info(f"✓ Successfully installed {pkg_name}")
                        
                        # For easyocr, verify it can actually be imported
                        if pkg_name == 'easyocr':
                            logger.info("Verifying EasyOCR import...")
                            verify_result = subprocess.run(
                                [str(self._get_python_path()), '-c', 'import easyocr; print("ok")'],
                                capture_output=True,
                                text=True,
                                timeout=30
                            )
                            if verify_result.returncode != 0:
                                logger.warning(f"EasyOCR installed but import failed: {verify_result.stderr[:200]}")
                    else:
                        error_msg = result.stderr or result.stdout or 'Unknown error'
                        # Extract meaningful error from pip output
                        if 'ERROR:' in error_msg:
                            error_lines = [line for line in error_msg.split('\n') if 'ERROR:' in line or 'error:' in line.lower()]
                            if error_lines:
                                error_msg = '\n'.join(error_lines[:3])  # First 3 error lines
                        error_msg = error_msg[:500]  # Limit error message length
                        failed.append({'package': pkg_name, 'error': error_msg})
                        logger.error(f"✗ Failed to install {pkg_name}")
                        logger.error(f"  Error: {error_msg}")
                except subprocess.TimeoutExpired:
                    failed.append({'package': pkg_name, 'error': 'Installation timeout (exceeded 10 minutes)'})
                    logger.error(f"✗ Timeout installing {pkg_name} (exceeded 10 minutes)")
                except Exception as e:
                    error_msg = str(e)[:500]
                    failed.append({'package': pkg_name, 'error': error_msg})
                    logger.error(f"✗ Exception installing {pkg_name}: {error_msg}")
                    import traceback
                    logger.debug(f"Traceback: {traceback.format_exc()}")
            
            # Re-check installed packages after installation
            self._check_installed_packages()
            
            self._status = DocExtEnvStatus.READY
            self._save_config()
            
            if len(failed) > 0:
                error_summary = f"Failed to install {len(failed)}/{total} packages: " + ", ".join([f['package'] for f in failed])
                self._notify_progress('complete', 100, error_summary)
                logger.warning(error_summary)
            else:
                self._notify_progress('complete', 100, f'Successfully installed {len(installed)}/{total} packages')
            
            return {
                'success': len(failed) == 0,
                'installed': installed,
                'failed': failed,
                'message': f'Installed {len(installed)}/{total} packages' + (f', {len(failed)} failed' if failed else ''),
                'package_status': {
                    name: pkg.installed 
                    for name, pkg in DOC_EXTRACTION_PACKAGES.items()
                }
            }
            
        except Exception as e:
            self._status = DocExtEnvStatus.ERROR
            self._error_message = str(e)
            self._save_config()
            return {'success': False, 'error': str(e)}
    
    def install_all_packages(self, progress_callback: Optional[Callable] = None) -> Dict[str, Any]:
        """Install all recommended packages"""
        all_packages = [pkg.pip_name for pkg in DOC_EXTRACTION_PACKAGES.values()]
        return self.install_packages(all_packages, progress_callback)
    
    def get_python_executable(self) -> Optional[Path]:
        """Get path to Python executable in the environment"""
        python_path = self._get_python_path()
        return python_path if python_path.exists() else None
    
    def get_storage_path(self) -> Path:
        """Get path where extracted documents are stored"""
        return self._storage_path
    
    def set_storage_path(self, path: str):
        """Update storage path setting"""
        from app.models.core import Setting
        self._storage_path = Path(path)
        self._storage_path.mkdir(parents=True, exist_ok=True)
        Setting.set('doc_extraction_storage_path', str(self._storage_path),
                   'Path where extracted document text files are stored')
    
    def delete_environment(self) -> Dict[str, Any]:
        """Delete the document extraction environment"""
        try:
            import shutil
            if self._env_path.exists():
                shutil.rmtree(self._env_path)
            
            self._status = DocExtEnvStatus.NOT_CREATED
            self._installed_packages = []
            self._error_message = None
            
            if self._config_file.exists():
                self._config_file.unlink()
            
            return {'success': True, 'message': 'Environment deleted'}
        except Exception as e:
            return {'success': False, 'error': str(e)}


def get_doc_extraction_env() -> DocumentExtractionEnvironmentManager:
    """Get singleton instance of DocumentExtractionEnvironmentManager"""
    return DocumentExtractionEnvironmentManager()
