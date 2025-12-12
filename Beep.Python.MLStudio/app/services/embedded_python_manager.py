"""
Embedded Python Runtime Manager for MLStudio
Manages embedded Python runtime
"""
import os
import sys
import platform
from pathlib import Path
from typing import Dict, Any, Optional


class EmbeddedPythonManager:
    """Manager for embedded Python runtime"""
    
    def __init__(self, base_path: Optional[Path] = None):
        """Initialize embedded Python manager"""
        if base_path:
            self.base_path = base_path
        else:
            # Use MLStudio's own directory
            self.base_path = Path(__file__).parent.parent.parent
        self.embedded_path = self.base_path / 'python-embedded'
        self.protection_file = self.embedded_path / '.mlstudio_protected'
        self.is_windows = platform.system() == 'Windows'
    
    def _get_python_executable(self) -> Path:
        """Get platform-specific Python executable path"""
        if self.is_windows:
            return self.embedded_path / 'python.exe'
        else:
            return self.embedded_path / 'bin' / 'python3'
    
    def is_embedded_mode(self) -> bool:
        """Check if running in embedded Python mode"""
        return os.environ.get('MLSTUDIO_EMBEDDED_PYTHON') == '1'
    
    def is_embedded_installed(self) -> bool:
        """Check if embedded Python is installed"""
        python_exe = self._get_python_executable()
        return python_exe.exists()
    
    def get_embedded_python(self) -> Optional[str]:
        """Get embedded Python executable path if available"""
        python_exe = self._get_python_executable()
        if python_exe.exists():
            return str(python_exe)
        return None
    
    def get_embedded_info(self) -> Dict[str, Any]:
        """Get information about embedded Python installation"""
        if not self.is_embedded_installed():
            return {
                'installed': False,
                'message': 'Embedded Python not installed'
            }
        
        python_exe = self._get_python_executable()
        
        # Get Python version
        version = 'Unknown'
        try:
            import subprocess
            result = subprocess.run(
                [str(python_exe), '--version'],
                capture_output=True,
                text=True
            )
            version = result.stdout.strip() or result.stderr.strip()
        except:
            pass
        
        # Get directory size
        total_size = 0
        file_count = 0
        try:
            for file in self.embedded_path.rglob('*'):
                if file.is_file():
                    total_size += file.stat().st_size
                    file_count += 1
        except:
            pass
        
        size_mb = total_size / (1024 * 1024)
        
        return {
            'installed': True,
            'path': str(self.embedded_path),
            'python_exe': str(python_exe),
            'version': version,
            'size_mb': round(size_mb, 2),
            'file_count': file_count,
            'currently_using': self.is_embedded_mode()
        }
    
    def verify_integrity(self) -> Dict[str, Any]:
        """Verify embedded Python integrity"""
        if not self.is_embedded_installed():
            return {
                'healthy': False,
                'issues': ['Embedded Python not installed']
            }
        
        issues = []
        
        # Check for python executable
        python_exe = self._get_python_executable()
        if not python_exe.exists():
            exe_name = 'python.exe' if self.is_windows else 'bin/python3'
            issues.append(f'{exe_name} missing')
        
        # Check for pip
        if self.is_windows:
            pip_exe = self.embedded_path / 'Scripts' / 'pip.exe'
        else:
            pip_exe = self.embedded_path / 'bin' / 'pip'
        
        if not pip_exe.exists():
            issues.append('pip not found')
        
        return {
            'healthy': len(issues) == 0,
            'issues': issues
        }


def get_embedded_python_manager() -> EmbeddedPythonManager:
    """Get singleton embedded Python manager instance"""
    if not hasattr(get_embedded_python_manager, '_instance'):
        get_embedded_python_manager._instance = EmbeddedPythonManager()
    return get_embedded_python_manager._instance

