"""
AI Services Environment Manager

Manages dedicated Python virtual environments for various AI services:
- Text-to-Image (Stable Diffusion, DALL-E, etc.)
- Text-to-Speech (pyttsx3, gTTS, etc.)
- Speech-to-Text (Whisper, SpeechRecognition, etc.)
- Voice-to-Voice (voice cloning, conversion, etc.)

Each service can have its own isolated environment.
"""
import os
import json
import subprocess
import logging
from pathlib import Path
from datetime import datetime
from dataclasses import dataclass
from typing import Dict, List, Optional, Callable
from enum import Enum

logger = logging.getLogger(__name__)


class AIServiceType(Enum):
    """Types of AI services"""
    TEXT_TO_IMAGE = "text_to_image"
    TEXT_TO_SPEECH = "text_to_speech"
    SPEECH_TO_TEXT = "speech_to_text"
    VOICE_TO_VOICE = "voice_to_voice"
    IMAGE_TO_TEXT = "image_to_text"  # OCR/Image captioning
    OBJECT_DETECTION = "object_detection"
    TABULAR_TIME_SERIES = "tabular_time_series"  # Tabular classification, regression, time series forecasting


class EnvStatus(Enum):
    """Environment status"""
    NOT_CREATED = "not_created"
    CREATING = "creating"
    READY = "ready"
    ERROR = "error"


@dataclass
class AIServicePackage:
    """Package information for AI service"""
    name: str
    pip_name: str
    import_name: str
    description: str
    required: bool = True
    installed: bool = False


# Package definitions for each service type
SERVICE_PACKAGES = {
    AIServiceType.TEXT_TO_IMAGE: {
        'diffusers': AIServicePackage(
            name='Diffusers',
            pip_name='diffusers>=0.19.0',
            import_name='diffusers',
            description='Hugging Face Diffusers >= 0.19.0 - Stable Diffusion XL, Qwen-Image, and other image generation models',
            required=True
        ),
        'transformers': AIServicePackage(
            name='Transformers',
            pip_name='transformers',
            import_name='transformers',
            description='Hugging Face Transformers - Required for diffusers',
            required=True
        ),
        'torch': AIServicePackage(
            name='PyTorch',
            pip_name='torch',
            import_name='torch',
            description='PyTorch - Deep learning framework',
            required=True
        ),
        'pillow': AIServicePackage(
            name='Pillow',
            pip_name='Pillow',
            import_name='PIL',
            description='Image processing library',
            required=True
        ),
        'accelerate': AIServicePackage(
            name='Accelerate',
            pip_name='accelerate',
            import_name='accelerate',
            description='Hugging Face Accelerate - For faster inference and CPU offloading',
            required=True
        ),
        'safetensors': AIServicePackage(
            name='Safetensors',
            pip_name='safetensors',
            import_name='safetensors',
            description='Safetensors - Safe and fast tensor serialization',
            required=True
        ),
        'invisible_watermark': AIServicePackage(
            name='Invisible Watermark',
            pip_name='invisible-watermark',
            import_name='invisible_watermark',
            description='Invisible Watermark - For Stable Diffusion XL',
            required=True
        ),
        'ultraflux': AIServicePackage(
            name='UltraFlux',
            pip_name='ultraflux',
            import_name='ultraflux',
            description='UltraFlux - High-resolution image generation (4096x4096)',
            required=False
        )
    },
    AIServiceType.TEXT_TO_SPEECH: {
        'pyttsx3': AIServicePackage(
            name='pyttsx3',
            pip_name='pyttsx3',
            import_name='pyttsx3',
            description='Offline text-to-speech (uses system voices)',
            required=False
        ),
        'gtts': AIServicePackage(
            name='gTTS',
            pip_name='gtts',
            import_name='gtts',
            description='Google Text-to-Speech (requires internet)',
            required=False
        ),
        'edge-tts': AIServicePackage(
            name='Edge TTS',
            pip_name='edge-tts',
            import_name='edge_tts',
            description='Microsoft Edge TTS (free, high quality)',
            required=False
        ),
        'coqui-tts': AIServicePackage(
            name='Coqui TTS',
            pip_name='TTS',
            import_name='TTS',
            description='Coqui TTS - High quality neural TTS',
            required=False
        ),
        'pydub': AIServicePackage(
            name='Pydub',
            pip_name='pydub',
            import_name='pydub',
            description='Audio manipulation library',
            required=False
        )
    },
    AIServiceType.SPEECH_TO_TEXT: {
        'whisper': AIServicePackage(
            name='OpenAI Whisper',
            pip_name='openai-whisper',
            import_name='whisper',
            description='OpenAI Whisper - High quality speech recognition',
            required=True
        ),
        'speechrecognition': AIServicePackage(
            name='SpeechRecognition',
            pip_name='SpeechRecognition',
            import_name='speech_recognition',
            description='Speech recognition library (multiple backends)',
            required=False
        ),
        'pydub': AIServicePackage(
            name='Pydub',
            pip_name='pydub',
            import_name='pydub',
            description='Audio manipulation library',
            required=False
        )
    },
    AIServiceType.VOICE_TO_VOICE: {
        'so-vits-svc': AIServicePackage(
            name='so-vits-svc',
            pip_name='so-vits-svc',
            import_name='so_vits_svc',
            description='Voice conversion and cloning',
            required=False
        ),
        'pydub': AIServicePackage(
            name='Pydub',
            pip_name='pydub',
            import_name='pydub',
            description='Audio manipulation library',
            required=True
        ),
        'librosa': AIServicePackage(
            name='Librosa',
            pip_name='librosa',
            import_name='librosa',
            description='Audio analysis library',
            required=False
        )
    },
    AIServiceType.IMAGE_TO_TEXT: {
        'easyocr': AIServicePackage(
            name='EasyOCR',
            pip_name='easyocr',
            import_name='easyocr',
            description='OCR - Optical Character Recognition',
            required=False
        ),
        'pytesseract': AIServicePackage(
            name='PyTesseract',
            pip_name='pytesseract',
            import_name='pytesseract',
            description='Tesseract OCR wrapper',
            required=False
        ),
        'pillow': AIServicePackage(
            name='Pillow',
            pip_name='Pillow',
            import_name='PIL',
            description='Image processing library',
            required=True
        )
    }
}


class AIServicesEnvironmentManager:
    """Manages dedicated environments for AI services"""
    
    def __init__(self, service_type: AIServiceType):
        # Lazy initialization - don't access database in __init__
        self.service_type = service_type
        self._base_path = None
        self._env_path = None
        self._config_file = None
        
        # Status
        self._status = EnvStatus.NOT_CREATED
        self._installed_packages = []
        
        # Platform detection
        self.is_windows = os.name == 'nt'
    
    def _ensure_initialized(self):
        """Lazy initialization - ensure paths are set up"""
        if self._base_path is not None:
            return  # Already initialized
        
        from flask import has_app_context
        from app.models.core import Setting
        
        service_name = self.service_type.value
        
        # Get base path - use database if app context is available, otherwise use default
        if has_app_context():
            try:
                base_path_str = Setting.get(f'ai_service_{service_name}_base_path', 
                                           os.path.join(os.getcwd(), 'data', 'ai_services', service_name))
            except Exception:
                # Fallback if database access fails
                base_path_str = os.path.join(os.getcwd(), 'data', 'ai_services', service_name)
        else:
            # No app context - use default
            base_path_str = os.path.join(os.getcwd(), 'data', 'ai_services', service_name)
        
        self._base_path = Path(base_path_str)
        self._base_path.mkdir(parents=True, exist_ok=True)
        
        # Get environment path
        if has_app_context():
            try:
                env_path = Setting.get(f'ai_service_{service_name}_env_path', '')
                if env_path:
                    self._env_path = Path(env_path)
                else:
                    # Use providers directory via EnvironmentManager
                    providers_path = self._base_path / 'providers'
                    self._env_path = providers_path / f'ai_services_{self._service_type.value}'
                    # Save default path
                    try:
                        Setting.set(f'ai_service_{service_name}_env_path', str(self._env_path),
                                   f'{service_name.replace("_", " ").title()} virtual environment path')
                    except Exception:
                        pass  # Ignore if we can't save
            except Exception:
                # Fallback if database access fails
                providers_path = self._base_path / 'providers'
                self._env_path = providers_path / f'ai_services_{self._service_type.value}'
        else:
            # No app context - use default
            providers_path = self._base_path / 'providers'
            self._env_path = providers_path / f'ai_services_{self._service_type.value}'
        
        # Config file
        self._config_file = self._base_path / 'config.json'
        
        # Load config
        self._load_config()
        
        # Check if environment exists
        if self._env_path.exists() and self._get_python_path().exists():
            self._status = EnvStatus.READY
            self._check_installed_packages()
        else:
            self._status = EnvStatus.NOT_CREATED
    
    def _load_config(self):
        """Load environment configuration"""
        self._ensure_initialized()
        if self._config_file is None or not self._config_file.exists():
            return
        if self._config_file.exists():
            try:
                with open(self._config_file, 'r') as f:
                    config = json.load(f)
                    self._installed_packages = config.get('installed_packages', [])
            except Exception as e:
                logger.error(f"Failed to load {self.service_type.value} env config: {e}")
    
    def _save_config(self):
        """Save environment configuration"""
        self._ensure_initialized()
        self._base_path.mkdir(parents=True, exist_ok=True)
        
        config = {
            'env_path': str(self._env_path),
            'status': self._status.value,
            'installed_packages': self._installed_packages,
            'updated_at': datetime.now().isoformat()
        }
        
        with open(self._config_file, 'w') as f:
            json.dump(config, f, indent=2)
    
    def _get_python_path(self) -> Path:
        """Get path to Python executable in environment"""
        self._ensure_initialized()
        if self.is_windows:
            return self._env_path / 'Scripts' / 'python.exe'
        return self._env_path / 'bin' / 'python'
    
    def _get_pip_path(self) -> Path:
        """Get path to pip in environment"""
        self._ensure_initialized()
        if self.is_windows:
            return self._env_path / 'Scripts' / 'pip.exe'
        return self._env_path / 'bin' / 'pip'
    
    def _check_installed_packages(self):
        """Check which packages are actually installed"""
        self._ensure_initialized()
        if not self._get_python_path().exists():
            return
        
        python_exe = self._get_python_path()
        packages = SERVICE_PACKAGES.get(self.service_type, {})
        
        for pkg_name, pkg_info in packages.items():
            try:
                result = subprocess.run(
                    [str(python_exe), '-c', f'import {pkg_info.import_name}; print("ok")'],
                    capture_output=True,
                    text=True,
                    timeout=30
                )
                pkg_info.installed = result.returncode == 0
            except subprocess.TimeoutExpired:
                logger.warning(f"Timeout checking {pkg_name}")
                pkg_info.installed = False
            except Exception as e:
                logger.debug(f"Failed to check {pkg_name}: {e}")
                pkg_info.installed = False
    
    def create_environment(self, progress_callback: Optional[Callable] = None) -> Dict:
        """Create the virtual environment using EnvironmentManager"""
        self._ensure_initialized()
        try:
            self._status = EnvStatus.CREATING
            self._save_config()
            
            if progress_callback:
                progress_callback('creating', 10, 'Creating virtual environment...')
            
            # Use centralized EnvironmentManager
            from app.services.environment_manager import EnvironmentManager
            from app.config_manager import get_app_directory
            
            env_mgr = EnvironmentManager(base_path=str(get_app_directory()))
            env_name = f'ai_services_{self._service_type.value}'
            
            # Check if environment already exists
            existing_envs = env_mgr.list_environments()
            existing = next((e for e in existing_envs if e.name == env_name), None)
            
            if existing:
                # Environment already exists, use it
                self._env_path = Path(existing.path)
                logger.info(f"Using existing environment at {self._env_path}")
            else:
                # Create new environment using EnvironmentManager
                if progress_callback:
                    progress_callback('creating', 20, 'Creating virtual environment via EnvironmentManager...')
                virtual_env = env_mgr.create_environment(name=env_name)
                self._env_path = Path(virtual_env.path)
                logger.info(f"Created virtual environment at {self._env_path}")
            
            if progress_callback:
                progress_callback('creating', 50, 'Upgrading pip...')
            
            python_exe = self._get_python_path()
            if not python_exe.exists():
                return {'success': False, 'error': f'Python executable not found at {python_exe}'}
            
            try:
                subprocess.run(
                    [str(python_exe), '-m', 'pip', 'install', '--upgrade', 'pip'],
                    check=True,
                    capture_output=True,
                    text=True,
                    timeout=120
                )
            except:
                pass
            
            self._status = EnvStatus.READY
            self._save_config()
            
            if progress_callback:
                progress_callback('complete', 100, 'Environment created successfully')
            
            return {'success': True, 'message': 'Environment created successfully', 'env_path': str(self._env_path)}
            
        except Exception as e:
            self._status = EnvStatus.ERROR
            self._save_config()
            logger.error(f"Failed to create environment: {e}", exc_info=True)
            return {'success': False, 'error': str(e)}
    
    def install_packages(self, package_names: Optional[List[str]] = None, 
                        progress_callback: Optional[Callable] = None) -> Dict:
        """Install required packages"""
        self._ensure_initialized()
        if not self._get_python_path().exists():
            return {'success': False, 'error': 'Environment not created. Create environment first.'}
        
        python_exe = self._get_python_path()
        pip_exe = self._get_pip_path()
        
        if not python_exe.exists() or not pip_exe.exists():
            return {'success': False, 'error': 'Python or pip not found in environment'}
        
        packages = SERVICE_PACKAGES.get(self.service_type, {})
        
        if package_names is None:
            packages_to_install = [pkg.pip_name for pkg in packages.values() if pkg.required]
        else:
            packages_to_install = package_names
        
        installed = []
        failed = []
        total = len(packages_to_install)
        
        logger.info(f"Starting installation of {total} packages using Python: {python_exe}")
        logger.info(f"Using 'python -m pip' to avoid Windows file locking issues")
        
        # Use python -m pip instead of pip.exe directly to avoid Windows file locking issues
        import time
        
        for i, pkg_name in enumerate(packages_to_install):
            max_retries = 3
            retry_delay = 2  # seconds
            installed_successfully = False
            
            for attempt in range(max_retries):
                try:
                    progress_pct = int((i / total) * 90)  # 0-90% for installation, 100% when complete
                    if progress_callback:
                        retry_msg = f" (retry {attempt + 1}/{max_retries})" if attempt > 0 else ""
                        progress_callback('installing', progress_pct, f'Installing {pkg_name} ({i+1}/{total}){retry_msg}...')
                    
                    if attempt > 0:
                        logger.info(f"Retry {attempt + 1}/{max_retries} for {pkg_name} after {retry_delay}s delay...")
                        time.sleep(retry_delay * attempt)  # Exponential backoff
                    
                    logger.info(f"Installing package {i+1}/{total}: {pkg_name}")
                    
                    # Use python -m pip instead of pip.exe to avoid Windows file locking issues
                    result = subprocess.run(
                        [str(python_exe), '-m', 'pip', 'install', pkg_name, '--no-warn-script-location'],
                        capture_output=True,
                        text=True,
                        timeout=1800  # 30 minutes for large packages like torch
                    )
                    
                    if result.returncode == 0:
                        installed.append(pkg_name)
                        logger.info(f"✓ Successfully installed {pkg_name}")
                        installed_successfully = True
                        break  # Success, exit retry loop
                    else:
                        error_msg = result.stderr or result.stdout or 'Unknown error'
                        
                        # Check for Windows file lock error
                        is_file_lock_error = (
                            'WinError 32' in error_msg or 
                            'The process cannot access the file' in error_msg or
                            'being used by another process' in error_msg
                        )
                        
                        if is_file_lock_error and attempt < max_retries - 1:
                            logger.warning(f"File lock error installing {pkg_name}, will retry...")
                            continue  # Retry on file lock error
                        else:
                            # Not a retryable error or out of retries
                            failed.append({'package': pkg_name, 'error': error_msg[:500]})
                            logger.error(f"✗ Failed to install {pkg_name}: {error_msg[:200]}")
                            break  # Exit retry loop
                            
                except subprocess.TimeoutExpired:
                    if attempt < max_retries - 1:
                        logger.warning(f"Timeout installing {pkg_name}, will retry...")
                        continue
                    else:
                        failed.append({'package': pkg_name, 'error': 'Installation timeout (exceeded 30 minutes)'})
                        logger.error(f"✗ Timeout installing {pkg_name}")
                        break
                except Exception as e:
                    error_str = str(e)
                    is_file_lock_error = (
                        'WinError 32' in error_str or 
                        'The process cannot access the file' in error_str or
                        'being used by another process' in error_str
                    )
                    
                    if is_file_lock_error and attempt < max_retries - 1:
                        logger.warning(f"File lock error installing {pkg_name}, will retry...")
                        continue
                    else:
                        failed.append({'package': pkg_name, 'error': error_str[:500]})
                        logger.error(f"✗ Error installing {pkg_name}: {error_str[:200]}")
                        break
            
            # Small delay between packages to avoid conflicts
            if i < total - 1:  # Don't delay after last package
                time.sleep(0.5)
        
        # Verify installations
        logger.info("Verifying installed packages...")
        self._check_installed_packages()
        self._save_config()
        
        if progress_callback:
            progress_callback('complete', 100, f'Installed {len(installed)}/{len(packages_to_install)} packages')
        
        logger.info(f"Installation complete: {len(installed)} installed, {len(failed)} failed")
        
        return {
            'success': len(failed) == 0,
            'message': f'Installed {len(installed)}/{len(packages_to_install)} packages',
            'installed': installed,
            'failed': failed,
            'package_status': {name: pkg.installed for name, pkg in packages.items()}
        }
    
    def get_python_executable(self) -> Optional[Path]:
        """Get path to Python executable in environment"""
        python_exe = self._get_python_path()
        if python_exe.exists():
            return python_exe
        return None
    
    def get_status(self) -> Dict:
        """Get environment status"""
        try:
            self._ensure_initialized()
            
            # Only check packages if environment exists
            if self._env_path and self._env_path.exists() and self._get_python_path().exists():
                self._check_installed_packages()
            else:
                # Reset package status if env doesn't exist
                packages = SERVICE_PACKAGES.get(self.service_type, {})
                for pkg in packages.values():
                    pkg.installed = False
            
            packages = SERVICE_PACKAGES.get(self.service_type, {})
            installed_count = sum(1 for pkg in packages.values() if pkg.installed)
            required_count = sum(1 for pkg in packages.values() if pkg.required)
            
            return {
                'status': self._status.value,
                'env_path': str(self._env_path) if self._env_path else '',
                'env_exists': self._env_path.exists() if self._env_path else False,
                'python_exists': self._get_python_path().exists() if self._env_path else False,
                'packages': {
                    name: {
                        'name': pkg.name,
                        'installed': pkg.installed,
                        'required': pkg.required,
                        'description': pkg.description
                    }
                    for name, pkg in packages.items()
                },
                'installed_count': installed_count,
                'required_count': required_count,
                'all_required_installed': installed_count >= required_count
            }
        except Exception as e:
            logger.error(f"Error getting status for {self.service_type.value}: {e}", exc_info=True)
            # Return safe default status
            packages = SERVICE_PACKAGES.get(self.service_type, {})
            return {
                'status': 'error',
                'env_path': '',
                'env_exists': False,
                'python_exists': False,
                'packages': {
                    name: {
                        'name': pkg.name,
                        'installed': False,
                        'required': pkg.required,
                        'description': pkg.description
                    }
                    for name, pkg in packages.items()
                },
                'installed_count': 0,
                'required_count': sum(1 for pkg in packages.values() if pkg.required),
                'all_required_installed': False,
                'error': str(e)
            }
    
    def get_required_packages(self) -> List[str]:
        """Get list of required package names"""
        packages = SERVICE_PACKAGES.get(self.service_type, {})
        return [pkg.pip_name for pkg in packages.values() if pkg.required]


# Singleton instances
_env_instances = {}


def get_ai_service_env(service_type: AIServiceType) -> AIServicesEnvironmentManager:
    """Get singleton instance for a service type"""
    if service_type not in _env_instances:
        _env_instances[service_type] = AIServicesEnvironmentManager(service_type)
    return _env_instances[service_type]
