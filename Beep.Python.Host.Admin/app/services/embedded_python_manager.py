"""
Embedded Python Runtime Manager

Manages embedded Python runtime and provides admin interface
for monitoring and protection.
"""
import os
import sys
import platform
from pathlib import Path
from typing import Dict, Any, Optional
import shutil


class EmbeddedPythonManager:
    """Manager for embedded Python runtime"""
    
    def __init__(self):
        from app.config_manager import get_app_directory
        self.base_path = get_app_directory()
        self.embedded_path = self.base_path / 'python-embedded'
        self.protection_file = self.embedded_path / '.beep_protected'
        self.is_windows = platform.system() == 'Windows'
    
    def _get_python_executable(self) -> Path:
        """Get platform-specific Python executable path"""
        if self.is_windows:
            return self.embedded_path / 'python.exe'
        else:
            return self.embedded_path / 'bin' / 'python3'
    
    def is_embedded_mode(self) -> bool:
        """Check if running in embedded Python mode"""
        return os.environ.get('BEEP_EMBEDDED_PYTHON') == '1'
    
    def is_embedded_installed(self) -> bool:
        """Check if embedded Python is installed"""
        python_exe = self._get_python_executable()
        return python_exe.exists()
    
    def is_protected(self) -> bool:
        """Check if embedded Python has protection marker"""
        return self.protection_file.exists()
    
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
        for file in self.embedded_path.rglob('*'):
            if file.is_file():
                total_size += file.stat().st_size
                file_count += 1
        
        size_mb = total_size / (1024 * 1024)
        
        return {
            'installed': True,
            'protected': self.is_protected(),
            'path': str(self.embedded_path),
            'python_exe': str(python_exe),
            'version': version,
            'size_mb': round(size_mb, 2),
            'file_count': file_count,
            'currently_using': self.is_embedded_mode(),
            'can_delete': False  # Always protected
        }
    
    def get_protection_warning(self) -> str:
        """Get protection warning message"""
        setup_script = 'setup_embedded_python.bat' if self.is_windows else './setup_embedded_python.sh'
        return f"""
⚠️ CRITICAL SYSTEM COMPONENT ⚠️

This is the embedded Python runtime that powers Beep.Python.

Deleting this directory will:
- Prevent the application from starting
- Require re-running {setup_script}
- Not affect your LLM models or data

This directory is PROTECTED and should not be deleted.
"""
    
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
        
        # Check for critical files (platform-specific)
        if self.is_windows:
            critical_files = ['python311.dll', 'python311._pth']
            for file in critical_files:
                if not (self.embedded_path / file).exists():
                    issues.append(f'{file} missing')
            
            # Check for pip (Windows)
            scripts_path = self.embedded_path / 'Scripts'
            if not scripts_path.exists():
                issues.append('Scripts directory missing (pip may not be installed)')
        else:
            # Check for pip (Linux/macOS)
            bin_path = self.embedded_path / 'bin'
            if not bin_path.exists():
                issues.append('bin directory missing')
        
        # Check protection marker
        if not self.is_protected():
            issues.append('Protection marker missing')
        
        setup_script = 'setup_embedded_python.bat' if self.is_windows else './setup_embedded_python.sh'
        
        return {
            'healthy': len(issues) == 0,
            'issues': issues,
            'recommendations': [
                f'Run {setup_script} to repair' if issues else 'No action needed'
            ]
        }
    
    def create_protection_marker(self):
        """Create protection marker file"""
        if not self.embedded_path.exists():
            return False
        
        with open(self.protection_file, 'w') as f:
            f.write("CRITICAL_SYSTEM_COMPONENT\n")
            f.write("This directory contains the embedded Python runtime required for Beep.Python to function.\n")
            f.write("DO NOT DELETE this directory unless you are uninstalling the application.\n")
            f.write("Deletion will prevent the application from starting.\n")
        
        return True
    
    def get_runtime_stats(self) -> Dict[str, Any]:
        """Get current runtime statistics"""
        return {
            'python_version': sys.version,
            'python_executable': sys.executable,
            'is_embedded': self.is_embedded_mode(),
            'embedded_available': self.is_embedded_installed(),
            'platform': sys.platform,
            'prefix': sys.prefix,
            'base_prefix': sys.base_prefix
        }


def get_embedded_python_manager() -> EmbeddedPythonManager:
    """Get singleton embedded Python manager instance"""
    if not hasattr(get_embedded_python_manager, '_instance'):
        get_embedded_python_manager._instance = EmbeddedPythonManager()
    return get_embedded_python_manager._instance
