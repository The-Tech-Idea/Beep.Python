"""
Virtual Environment Manager Service
Manages Python virtual environments for ML projects
Adapted from Beep.Python.Host.Admin
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
    """Manages virtual environments for ML projects"""
    
    def __init__(self, base_path: Optional[str] = None):
        """Initialize environment manager"""
        from app.services.settings_manager import get_settings_manager
        settings_mgr = get_settings_manager()
        
        if base_path:
            self.base_path = Path(base_path)
        else:
            # Use settings or default
            self.base_path = settings_mgr.get_base_path()
        
        # Get providers path from settings
        self.providers_path = settings_mgr.get_providers_folder()
        if not self.providers_path.is_absolute():
            self.providers_path = self.base_path / self.providers_path
        self.providers_path.mkdir(parents=True, exist_ok=True)
    
    def get_embedded_python(self) -> Optional[str]:
        """Get embedded Python executable - this is the base for all environments"""
        from app.services.settings_manager import get_settings_manager
        settings_mgr = get_settings_manager()
        
        embedded_path = settings_mgr.get_python_embedded_path()
        if not embedded_path.is_absolute():
            embedded_path = self.base_path / embedded_path
        
        if platform.system() == 'Windows':
            python_exe = embedded_path / 'python.exe'
        else:
            python_exe = embedded_path / 'bin' / 'python3'
        
        if python_exe.exists():
            return str(python_exe)
        return None
    
    def get_python_executable(self, prefer_embedded: bool = True) -> str:
        """
        Get Python executable to use for creating environments.
        REQUIRES embedded Python - no fallback to system Python.
        
        Raises:
            RuntimeError: If embedded Python is not found
        """
        # Embedded Python is REQUIRED - it's the base runtime
        embedded = self.get_embedded_python()
        if embedded:
            return embedded
        
        # No fallback - embedded Python is mandatory
        error_msg = (
            "Embedded Python is required but not found!\n"
            "Please run setup_embedded_python.bat (Windows) or ./setup_embedded_python.sh (Linux/macOS)\n"
            f"Expected location: {self.base_path / 'python-embedded'}"
        )
        logger.error(error_msg)
        raise RuntimeError(error_msg)
    
    def get_environment(self, name: str) -> Optional[VirtualEnvironment]:
        """Get a specific virtual environment by name"""
        venv_path = self.providers_path / name
        if venv_path.exists() and venv_path.is_dir():
            return self._get_environment_info(venv_path)
        return None
    
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
                    try:
                        packages = json.loads(result.stdout)
                        packages_count = len(packages)
                    except:
                        pass
            
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
            logger.error(f"Error getting environment info: {e}")
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
    
    def _ensure_virtualenv_available(self, python_exe: str):
        """
        Ensure virtualenv is available in the Python executable.
        ALWAYS uses virtualenv - NO FALLBACK to venv.
        Installs virtualenv if needed.
        
        Raises:
            RuntimeError: If virtualenv cannot be installed or made available
        """
        # Check if virtualenv is available
        check_virtualenv = subprocess.run(
            [python_exe, "-m", "virtualenv", "--help"],
            capture_output=True,
            text=True,
            timeout=10
        )
        if check_virtualenv.returncode == 0:
            logger.info(f"virtualenv module is available in {python_exe}")
            return
        
        # virtualenv not available - install it
        logger.info(f"Installing virtualenv in {python_exe}...")
        install_result = subprocess.run(
            [python_exe, "-m", "pip", "install", "virtualenv"],
            capture_output=True,
            text=True,
            timeout=120
        )
        
        if install_result.returncode != 0:
            raise RuntimeError(
                f"Failed to install virtualenv in {python_exe}. "
                f"Error: {install_result.stderr[:500]}"
            )
        
        # Verify virtualenv is now available
        check_virtualenv = subprocess.run(
            [python_exe, "-m", "virtualenv", "--help"],
            capture_output=True,
            text=True,
            timeout=10
        )
        if check_virtualenv.returncode != 0:
            raise RuntimeError(
                f"virtualenv installation failed or is not working in {python_exe}. "
                f"Please check the Python installation."
            )
        
        logger.info(f"virtualenv installed and verified in {python_exe}")
    
    def create_environment(self, name: str, python_executable: Optional[str] = None,
                          packages: Optional[List[str]] = None) -> VirtualEnvironment:
        """
        Create a new virtual environment.
        REQUIRES embedded Python as the base runtime (NO FALLBACK).
        
        Raises:
            RuntimeError: If embedded Python is not found and no python_executable provided
            ValueError: If environment creation fails
        """
        # Ensure providers directory exists
        self.providers_path.mkdir(parents=True, exist_ok=True)
        
        venv_path = self.providers_path / name
        
        if venv_path.exists():
            raise ValueError(f"Environment '{name}' already exists")
        
        # Get embedded Python - REQUIRED, NO FALLBACK
        if python_executable:
            python_exe = python_executable
            # Verify it's embedded Python
            if 'python-embedded' not in str(Path(python_exe).parent):
                raise RuntimeError(
                    f"Only embedded Python can be used. Provided: {python_exe}. "
                    f"Please use embedded Python from python-embedded directory."
                )
            logger.info(f"Creating environment '{name}' using provided embedded Python: {python_exe}")
        else:
            # Get embedded Python - REQUIRED, will raise RuntimeError if not found
            python_exe = self.get_python_executable(prefer_embedded=True)
            logger.info(f"Creating environment '{name}' using embedded Python (base runtime): {python_exe}")
        
        # Verify Python executable exists
        if not Path(python_exe).exists():
            raise RuntimeError(f"Python executable not found: {python_exe}")
        
        # Ensure virtualenv is available - ALWAYS use virtualenv, NO FALLBACK
        self._ensure_virtualenv_available(python_exe)
        
        # Create virtual environment using virtualenv (ALWAYS)
        logger.info(f"Creating virtual environment at: {venv_path} using virtualenv")
        result = subprocess.run(
            [python_exe, "-m", "virtualenv", str(venv_path)],
            capture_output=True,
            text=True,
            timeout=300  # 5 minute timeout
        )
        
        if result.returncode != 0:
            error_msg = f"Failed to create virtual environment '{name}'. "
            if result.stderr:
                error_msg += f"Error: {result.stderr[:500]}"
            if result.stdout:
                error_msg += f"Output: {result.stdout[:500]}"
            logger.error(error_msg)
            raise RuntimeError(error_msg)
        
        # Verify environment was created
        if platform.system() == "Windows":
            python_exe_check = venv_path / "Scripts" / "python.exe"
        else:
            python_exe_check = venv_path / "bin" / "python"
        
        if not python_exe_check.exists():
            raise RuntimeError(f"Virtual environment created but Python executable not found at: {python_exe_check}")
        
        logger.info(f"Virtual environment created successfully: {venv_path}")
        
        # Install packages if provided
        install_result = None
        if packages:
            logger.info(f"Installing packages in environment '{name}': {packages}")
            install_result = self.install_packages(name, packages)
            if not install_result.get('success', False):
                logger.warning(f"Some packages may have failed to install: {install_result.get('stderr', '')}")
        
        env = self._get_environment_info(venv_path)
        if not env:
            raise RuntimeError("Failed to get environment information after creation")
        
        # Convert to dict and include installation results
        env_dict = asdict(env)
        if install_result:
            env_dict['install_result'] = install_result
        
        return env_dict
    
    def delete_environment(self, name: str) -> bool:
        """Delete a virtual environment"""
        import shutil
        
        venv_path = self.providers_path / name
        
        if not venv_path.exists():
            raise ValueError(f"Environment '{name}' not found")
        
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
            try:
                pkg_list = json.loads(result.stdout)
                for pkg in pkg_list:
                    packages.append(Package(
                        name=pkg.get('name', ''),
                        version=pkg.get('version', ''),
                        location=pkg.get('location', ''),
                        requires=[],
                        required_by=[]
                    ))
            except:
                pass
        
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
        
        # Install packages in batches to avoid timeout
        # Heavy packages like tensorflow, transformers can take 10+ minutes
        timeout_per_batch = 1800  # 30 minutes per batch
        
        # Split into core and optional packages
        core_packages = [pkg for pkg in packages if pkg not in ['tensorflow', 'transformers', 'torch', 'torchvision']]
        heavy_packages = [pkg for pkg in packages if pkg in ['tensorflow', 'transformers', 'torch', 'torchvision']]
        
        all_results = []
        
        # Install core packages first
        if core_packages:
            logger.info(f"[{name}] Starting installation of {len(core_packages)} core packages...")
            logger.info(f"[{name}] Packages: {', '.join(core_packages[:10])}{'...' if len(core_packages) > 10 else ''}")
            try:
                result = subprocess.run(
                    [str(python_exe), "-m", "pip", "install"] + core_packages,
                    capture_output=True,
                    text=True,
                    timeout=timeout_per_batch
                )
                
                # Log key output lines
                if result.stdout:
                    for line in result.stdout.split('\n'):
                        if any(keyword in line.lower() for keyword in ['collecting', 'downloading', 'installing', 'successfully installed', 'requirement already satisfied']):
                            logger.info(f"[{name}] {line.strip()}")
                
                all_results.append({
                    "batch": "core",
                    "success": result.returncode == 0,
                    "stdout": result.stdout,
                    "stderr": result.stderr
                })
                
                if result.returncode == 0:
                    logger.info(f"[{name}] ✓ Core packages installed successfully")
                else:
                    logger.warning(f"[{name}] ⚠ Core packages installation had errors (return code: {result.returncode})")
            except subprocess.TimeoutExpired:
                logger.error(f"[{name}] ✗ Timeout installing core packages after {timeout_per_batch} seconds")
                all_results.append({
                    "batch": "core",
                    "success": False,
                    "stdout": "",
                    "stderr": f"Timeout installing core packages after {timeout_per_batch} seconds"
                })
        
        # Install heavy packages separately (optional, can fail without breaking everything)
        if heavy_packages:
            logger.info(f"[{name}] Installing {len(heavy_packages)} heavy packages separately...")
            for pkg in heavy_packages:
                logger.info(f"[{name}] Installing {pkg}...")
                try:
                    result = subprocess.run(
                        [str(python_exe), "-m", "pip", "install", pkg],
                        capture_output=True,
                        text=True,
                        timeout=timeout_per_batch
                    )
                    
                    if result.returncode == 0:
                        logger.info(f"[{name}] ✓ {pkg} installed successfully")
                    else:
                        logger.warning(f"[{name}] ⚠ {pkg} installation had errors (non-critical)")
                    
                    all_results.append({
                        "batch": pkg,
                        "success": result.returncode == 0,
                        "stdout": result.stdout,
                        "stderr": result.stderr
                    })
                except subprocess.TimeoutExpired:
                    logger.warning(f"[{name}] ⚠ Timeout installing {pkg} after {timeout_per_batch} seconds (non-critical)")
                    all_results.append({
                        "batch": pkg,
                        "success": False,
                        "stdout": "",
                        "stderr": f"Timeout installing {pkg} after {timeout_per_batch} seconds"
                    })
        
        # Return combined result
        core_success = any(r["success"] for r in all_results if r["batch"] == "core")
        return {
            "success": core_success,  # Success if core packages installed
            "stdout": "\n".join(r["stdout"] for r in all_results),
            "stderr": "\n".join(r["stderr"] for r in all_results),
            "batches": all_results  # Detailed batch results
        }
        
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

