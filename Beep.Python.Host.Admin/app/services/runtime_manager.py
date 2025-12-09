"""
Python Runtime Manager Service
Manages Python installations and discovery
"""
import os
import sys
import json
import subprocess
import platform
from pathlib import Path
from dataclasses import dataclass, asdict
from typing import List, Optional
import shutil


@dataclass
class PythonRuntime:
    """Represents a Python runtime installation"""
    id: str
    version: str
    path: str
    executable: str
    is_virtual: bool
    is_embedded: bool
    is_active: bool
    architecture: str
    pip_version: Optional[str] = None
    packages_count: int = 0


class RuntimeManager:
    """Manages Python runtime discovery and operations"""
    
    EMBEDDED_PYTHON_VERSION = "3.11.9"
    EMBEDDED_PYTHON_URL = "https://www.python.org/ftp/python/{version}/python-{version}-embed-amd64.zip"
    
    def __init__(self, base_path: Optional[str] = None):
        self.base_path = Path(base_path or os.path.expanduser("~/.beep-llm"))
        self.python_path = self.base_path / "python"
        self.providers_path = self.base_path / "providers"
        self._ensure_directories()
    
    def _ensure_directories(self):
        """Ensure base directories exist"""
        self.base_path.mkdir(parents=True, exist_ok=True)
        self.python_path.mkdir(exist_ok=True)
        self.providers_path.mkdir(exist_ok=True)
    
    def discover_runtimes(self) -> List[PythonRuntime]:
        """Discover all available Python runtimes"""
        runtimes = []
        
        # 1. Check embedded Python
        embedded = self._check_embedded_python()
        if embedded:
            runtimes.append(embedded)
        
        # 2. Check system Python installations
        system_pythons = self._discover_system_pythons()
        runtimes.extend(system_pythons)
        
        # 3. Check virtual environments in providers
        venvs = self._discover_virtual_environments()
        runtimes.extend(venvs)
        
        return runtimes
    
    def _check_embedded_python(self) -> Optional[PythonRuntime]:
        """Check for embedded Python installation"""
        embedded_path = self.python_path / self.EMBEDDED_PYTHON_VERSION
        
        if platform.system() == "Windows":
            executable = embedded_path / "python.exe"
        else:
            executable = embedded_path / "bin" / "python"
        
        if executable.exists():
            version = self._get_python_version(str(executable))
            return PythonRuntime(
                id=f"embedded-{self.EMBEDDED_PYTHON_VERSION}",
                version=version or self.EMBEDDED_PYTHON_VERSION,
                path=str(embedded_path),
                executable=str(executable),
                is_virtual=False,
                is_embedded=True,
                is_active=False,
                architecture="amd64",
                pip_version=self._get_pip_version(str(executable))
            )
        return None
    
    def _discover_system_pythons(self) -> List[PythonRuntime]:
        """Discover system Python installations"""
        runtimes = []
        
        # Common Python locations
        if platform.system() == "Windows":
            search_paths = [
                Path(os.environ.get("LOCALAPPDATA", "")) / "Programs" / "Python",
                Path("C:/Python*"),
                Path("C:/Program Files/Python*"),
            ]
            python_names = ["python.exe", "python3.exe"]
        else:
            search_paths = [
                Path("/usr/bin"),
                Path("/usr/local/bin"),
                Path(os.path.expanduser("~/.pyenv/versions")),
            ]
            python_names = ["python3", "python"]
        
        # Check PATH for python executables
        path_dirs = os.environ.get("PATH", "").split(os.pathsep)
        for path_dir in path_dirs:
            path = Path(path_dir)
            if path.exists():
                for name in python_names:
                    exe = path / name
                    if exe.exists() and exe.is_file():
                        runtime = self._create_runtime_from_exe(str(exe), is_system=True)
                        if runtime and not any(r.executable == runtime.executable for r in runtimes):
                            runtimes.append(runtime)
        
        return runtimes
    
    def _discover_virtual_environments(self) -> List[PythonRuntime]:
        """Discover virtual environments in the providers directory"""
        runtimes = []
        
        if self.providers_path.exists():
            for venv_dir in self.providers_path.iterdir():
                if venv_dir.is_dir():
                    if platform.system() == "Windows":
                        exe = venv_dir / "Scripts" / "python.exe"
                    else:
                        exe = venv_dir / "bin" / "python"
                    
                    if exe.exists():
                        runtime = self._create_runtime_from_exe(str(exe), is_virtual=True)
                        if runtime:
                            runtime.id = f"venv-{venv_dir.name}"
                            runtimes.append(runtime)
        
        return runtimes
    
    def _create_runtime_from_exe(self, executable: str, is_system: bool = False, 
                                  is_virtual: bool = False) -> Optional[PythonRuntime]:
        """Create a PythonRuntime from an executable path"""
        try:
            version = self._get_python_version(executable)
            if not version:
                return None
            
            path = str(Path(executable).parent.parent if is_virtual else Path(executable).parent)
            
            return PythonRuntime(
                id=f"{'venv' if is_virtual else 'system'}-{version}-{hash(executable) % 10000}",
                version=version,
                path=path,
                executable=executable,
                is_virtual=is_virtual,
                is_embedded=False,
                is_active=executable == sys.executable,
                architecture=platform.machine(),
                pip_version=self._get_pip_version(executable)
            )
        except Exception:
            return None
    
    def _get_python_version(self, executable: str) -> Optional[str]:
        """Get Python version from executable"""
        try:
            result = subprocess.run(
                [executable, "--version"],
                capture_output=True,
                text=True,
                timeout=10
            )
            if result.returncode == 0:
                # Output is like "Python 3.11.9"
                return result.stdout.strip().replace("Python ", "")
        except Exception:
            pass
        return None
    
    def _get_pip_version(self, executable: str) -> Optional[str]:
        """Get pip version for a Python installation"""
        try:
            result = subprocess.run(
                [executable, "-m", "pip", "--version"],
                capture_output=True,
                text=True,
                timeout=10
            )
            if result.returncode == 0:
                # Output is like "pip 23.3.1 from ..."
                parts = result.stdout.strip().split()
                if len(parts) >= 2:
                    return parts[1]
        except Exception:
            pass
        return None
    
    def install_embedded_python(self, progress_callback=None) -> bool:
        """Download and install embedded Python"""
        import urllib.request
        import zipfile
        
        version = self.EMBEDDED_PYTHON_VERSION
        target_path = self.python_path / version
        
        if target_path.exists():
            return True
        
        target_path.mkdir(parents=True, exist_ok=True)
        
        # Download URL
        url = self.EMBEDDED_PYTHON_URL.format(version=version)
        zip_path = target_path / "python.zip"
        
        try:
            if progress_callback:
                progress_callback("Downloading Python...", 0)
            
            urllib.request.urlretrieve(url, zip_path)
            
            if progress_callback:
                progress_callback("Extracting Python...", 50)
            
            with zipfile.ZipFile(zip_path, 'r') as zip_ref:
                zip_ref.extractall(target_path)
            
            zip_path.unlink()
            
            # Enable pip by modifying python*._pth
            pth_files = list(target_path.glob("python*._pth"))
            for pth_file in pth_files:
                content = pth_file.read_text()
                content = content.replace("#import site", "import site")
                pth_file.write_text(content)
            
            if progress_callback:
                progress_callback("Installing pip...", 75)
            
            # Install pip
            self._install_pip(str(target_path))
            
            if progress_callback:
                progress_callback("Complete!", 100)
            
            return True
            
        except Exception as e:
            if target_path.exists():
                shutil.rmtree(target_path)
            raise e
    
    def _install_pip(self, python_path: str):
        """Install pip in embedded Python"""
        import urllib.request
        
        python_exe = Path(python_path) / "python.exe"
        get_pip_url = "https://bootstrap.pypa.io/get-pip.py"
        get_pip_path = Path(python_path) / "get-pip.py"
        
        urllib.request.urlretrieve(get_pip_url, get_pip_path)
        
        subprocess.run(
            [str(python_exe), str(get_pip_path)],
            cwd=python_path,
            check=True
        )
        
        get_pip_path.unlink()
    
    def get_runtime_info(self, runtime_id: str) -> Optional[dict]:
        """Get detailed information about a runtime"""
        runtimes = self.discover_runtimes()
        for runtime in runtimes:
            if runtime.id == runtime_id:
                return asdict(runtime)
        return None
    
    def to_dict_list(self, runtimes: List[PythonRuntime]) -> List[dict]:
        """Convert runtimes to dictionary list"""
        return [asdict(r) for r in runtimes]
