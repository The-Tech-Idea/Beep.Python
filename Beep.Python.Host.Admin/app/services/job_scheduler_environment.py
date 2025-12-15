"""
Job Scheduler Environment Manager

Manages a dedicated Python virtual environment for the job scheduler.
Installs APScheduler and other required packages.
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


class EnvStatus(Enum):
    """Environment status"""
    NOT_CREATED = "not_created"
    CREATING = "creating"
    READY = "ready"
    ERROR = "error"


@dataclass
class SchedulerPackage:
    """Package information for scheduler environment"""
    name: str
    pip_name: str
    import_name: str
    description: str
    required: bool = True
    installed: bool = False


# Required packages for job scheduler
SCHEDULER_PACKAGES = {
    'apscheduler': SchedulerPackage(
        name='APScheduler',
        pip_name='apscheduler',
        import_name='apscheduler',
        description='Advanced Python Scheduler - Cross-platform task scheduling',
        required=True
    ),
    'requests': SchedulerPackage(
        name='Requests',
        pip_name='requests',
        import_name='requests',
        description='HTTP library for API calls in scheduled jobs',
        required=True
    )
}


class JobSchedulerEnvironmentManager:
    """Manages the dedicated Python virtual environment for job scheduler"""
    
    def __init__(self):
        # Lazy initialization - don't access database in __init__
        self._base_path = None
        self._env_path = None
        self._config_file = None
        self._base_path_value = None
        self._env_path_value = None
        
        # Status
        self._status = EnvStatus.NOT_CREATED
        self._installed_packages = []
        
        # Platform detection
        self.is_windows = os.name == 'nt'
        
        # Load config (will be loaded lazily)
    
    def _ensure_initialized(self):
        """Lazy initialization - ensure paths are set up"""
        if self._base_path is not None:
            return  # Already initialized
        
        from flask import has_app_context
        from app.models.core import Setting
        
        # Get base path - use database if app context is available, otherwise use default
        if has_app_context():
            try:
                base_path_str = Setting.get('job_scheduler_base_path', 
                                           os.path.join(os.getcwd(), 'data', 'job_scheduler'))
            except Exception:
                # Fallback if database access fails
                base_path_str = os.path.join(os.getcwd(), 'data', 'job_scheduler')
        else:
            # No app context - use default
            base_path_str = os.path.join(os.getcwd(), 'data', 'job_scheduler')
        
        self._base_path = Path(base_path_str)
        self._base_path.mkdir(parents=True, exist_ok=True)
        
        # Get environment path
        if has_app_context():
            try:
                env_path = Setting.get('job_scheduler_env_path', '')
                if env_path:
                    self._env_path = Path(env_path)
                else:
                    # Use providers directory via EnvironmentManager
                    providers_path = self._base_path / 'providers'
                    self._env_path = providers_path / 'job_scheduler'
                    # Save default path
                    try:
                        Setting.set('job_scheduler_env_path', str(self._env_path),
                                   'Job scheduler virtual environment path')
                    except Exception:
                        pass  # Ignore if we can't save
            except Exception:
                # Fallback if database access fails
                providers_path = self._base_path / 'providers'
                self._env_path = providers_path / 'job_scheduler'
        else:
            # No app context - use default
            providers_path = self._base_path / 'providers'
            self._env_path = providers_path / 'job_scheduler'
        
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
        """Load scheduler environment configuration"""
        # Note: This is called from _ensure_initialized(), so don't call it here
        if self._config_file is None:
            return
        if self._config_file.exists():
            try:
                with open(self._config_file, 'r') as f:
                    config = json.load(f)
                    self._installed_packages = config.get('installed_packages', [])
            except Exception as e:
                logger.error(f"Failed to load scheduler env config: {e}")
    
    def _save_config(self):
        """Save scheduler environment configuration"""
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
        
        for pkg_name, pkg_info in SCHEDULER_PACKAGES.items():
            try:
                result = subprocess.run(
                    [str(python_exe), '-c', f'import {pkg_info.import_name}; print("ok")'],
                    capture_output=True,
                    text=True,
                    timeout=10
                )
                pkg_info.installed = result.returncode == 0
                if not pkg_info.installed and result.stderr:
                    logger.debug(f"Package {pkg_name} ({pkg_info.import_name}) import failed: {result.stderr[:200]}")
            except subprocess.TimeoutExpired:
                logger.warning(f"Timeout checking {pkg_name}")
                pkg_info.installed = False
            except Exception as e:
                logger.debug(f"Failed to check {pkg_name}: {e}")
                pkg_info.installed = False
    
    def create_environment(self, progress_callback: Optional[Callable] = None) -> Dict:
        """Create the virtual environment"""
        self._ensure_initialized()
        try:
            self._status = EnvStatus.CREATING
            self._save_config()
            
            if progress_callback:
                progress_callback('creating', 10, 'Creating virtual environment...')
            
            # Check if venv module is available
            try:
                import venv
            except ImportError:
                return {
                    'success': False,
                    'error': 'venv module not available. Python 3.3+ required.'
                }
            
            # Create virtual environment
            if self._env_path.exists():
                logger.info(f"Environment already exists at {self._env_path}")
            else:
                if progress_callback:
                    progress_callback('creating', 20, 'Creating virtual environment directory...')
                
                venv.create(self._env_path, with_pip=True)
                logger.info(f"Created virtual environment at {self._env_path}")
            
            if progress_callback:
                progress_callback('creating', 50, 'Upgrading pip...')
            
            # Upgrade pip
            python_exe = self._get_python_path()
            pip_exe = self._get_pip_path()
            
            if not python_exe.exists():
                return {
                    'success': False,
                    'error': f'Python executable not found at {python_exe}'
                }
            
            try:
                subprocess.run(
                    [str(python_exe), '-m', 'pip', 'install', '--upgrade', 'pip'],
                    check=True,
                    capture_output=True,
                    text=True,
                    timeout=120
                )
            except subprocess.TimeoutExpired:
                logger.warning("pip upgrade timed out, continuing anyway")
            except subprocess.CalledProcessError as e:
                logger.warning(f"pip upgrade failed: {e.stderr}")
            
            self._status = EnvStatus.READY
            self._save_config()
            
            if progress_callback:
                progress_callback('complete', 100, 'Environment created successfully')
            
            return {
                'success': True,
                'message': 'Environment created successfully',
                'env_path': str(self._env_path)
            }
            
        except Exception as e:
            self._status = EnvStatus.ERROR
            self._save_config()
            logger.error(f"Failed to create environment: {e}", exc_info=True)
            return {
                'success': False,
                'error': str(e)
            }
    
    def install_packages(self, package_names: Optional[List[str]] = None, 
                        progress_callback: Optional[Callable] = None) -> Dict:
        """Install required packages"""
        self._ensure_initialized()
        if not self._get_python_path().exists():
            return {
                'success': False,
                'error': 'Environment not created. Create environment first.'
            }
        
        python_exe = self._get_python_path()
        pip_exe = self._get_pip_path()
        
        if not python_exe.exists() or not pip_exe.exists():
            return {
                'success': False,
                'error': 'Python or pip not found in environment'
            }
        
        # Determine which packages to install
        if package_names is None:
            # Install all required packages
            packages_to_install = [pkg.pip_name for pkg in SCHEDULER_PACKAGES.values() if pkg.required]
        else:
            packages_to_install = package_names
        
        installed = []
        failed = []
        
        for pkg_name in packages_to_install:
            try:
                if progress_callback:
                    progress_callback('installing', 0, f'Installing {pkg_name}...')
                
                logger.info(f"Installing {pkg_name}...")
                
                result = subprocess.run(
                    [str(pip_exe), 'install', pkg_name],
                    capture_output=True,
                    text=True,
                    timeout=600  # 10 minutes for large packages
                )
                
                if result.returncode == 0:
                    installed.append(pkg_name)
                    logger.info(f"✓ Installed {pkg_name}")
                    
                    # Verify installation for APScheduler
                    if pkg_name == 'apscheduler':
                        logger.info("Verifying APScheduler import...")
                        verify_result = subprocess.run(
                            [str(python_exe), '-c', 'import apscheduler; print("ok")'],
                            capture_output=True,
                            text=True,
                            timeout=10
                        )
                        if verify_result.returncode != 0:
                            logger.warning(f"APScheduler installed but import failed: {verify_result.stderr[:200]}")
                else:
                    error_msg = result.stderr or result.stdout or 'Unknown error'
                    failed.append({'package': pkg_name, 'error': error_msg[:200]})
                    logger.error(f"✗ Failed to install {pkg_name}: {error_msg[:200]}")
                    
            except subprocess.TimeoutExpired:
                failed.append({'package': pkg_name, 'error': 'Installation timeout (exceeded 10 minutes)'})
                logger.error(f"✗ Timeout installing {pkg_name} (exceeded 10 minutes)")
            except Exception as e:
                failed.append({'package': pkg_name, 'error': str(e)})
                logger.error(f"✗ Error installing {pkg_name}: {e}")
        
        # Update package status
        self._check_installed_packages()
        self._save_config()
        
        if progress_callback:
            progress_callback('complete', 100, f'Installed {len(installed)}/{len(packages_to_install)} packages')
        
        return {
            'success': len(failed) == 0,
            'message': f'Installed {len(installed)}/{len(packages_to_install)} packages',
            'installed': installed,
            'failed': failed,
            'package_status': {name: pkg.installed for name, pkg in SCHEDULER_PACKAGES.items()}
        }
    
    def get_python_executable(self) -> Optional[Path]:
        """Get path to Python executable in environment"""
        self._ensure_initialized()
        python_exe = self._get_python_path()
        if python_exe.exists():
            return python_exe
        return None
    
    def get_status(self) -> Dict:
        """Get environment status"""
        self._ensure_initialized()
        # Always check installed packages to get latest status
        self._check_installed_packages()
        
        installed_count = sum(1 for pkg in SCHEDULER_PACKAGES.values() if pkg.installed)
        required_count = sum(1 for pkg in SCHEDULER_PACKAGES.values() if pkg.required)
        
        return {
            'status': self._status.value,
            'env_path': str(self._env_path),
            'env_exists': self._env_path.exists(),
            'python_exists': self._get_python_path().exists(),
            'packages': {
                name: {
                    'name': pkg.name,
                    'installed': pkg.installed,
                    'required': pkg.required,
                    'description': pkg.description
                }
                for name, pkg in SCHEDULER_PACKAGES.items()
            },
            'installed_count': installed_count,
            'required_count': required_count,
            'all_required_installed': installed_count >= required_count
        }
    
    def get_required_packages(self) -> List[str]:
        """Get list of required package names"""
        return [pkg.pip_name for pkg in SCHEDULER_PACKAGES.values() if pkg.required]


# Singleton instance
_scheduler_env_instance = None


def get_scheduler_env() -> JobSchedulerEnvironmentManager:
    """Get singleton instance of JobSchedulerEnvironmentManager"""
    global _scheduler_env_instance
    if _scheduler_env_instance is None:
        _scheduler_env_instance = JobSchedulerEnvironmentManager()
    return _scheduler_env_instance
