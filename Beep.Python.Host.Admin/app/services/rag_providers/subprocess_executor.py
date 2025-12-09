"""
RAG Subprocess Executor

Executes RAG operations in the isolated RAG virtual environment.
This allows using FAISS/ChromaDB without installing them in the main Flask environment.
"""
import json
import subprocess
import platform
import tempfile
from pathlib import Path
from typing import Any, Dict, Optional
import logging

logger = logging.getLogger(__name__)


class RAGSubprocessExecutor:
    """
    Executes Python code in the RAG virtual environment.
    
    This enables the Flask app to use FAISS/ChromaDB packages that are
    installed in ~/.beep-rag/venv without polluting the main environment.
    """
    
    def __init__(self):
        self.rag_venv = Path.home() / '.beep-rag' / 'venv'
        self.is_windows = platform.system() == 'Windows'
        
    @property
    def python_exe(self) -> Path:
        """Get the Python executable from RAG venv"""
        if self.is_windows:
            return self.rag_venv / 'Scripts' / 'python.exe'
        else:
            return self.rag_venv / 'bin' / 'python3'
    
    @property
    def is_venv_available(self) -> bool:
        """Check if the RAG venv exists"""
        return self.python_exe.exists()
    
    def check_package(self, package_name: str) -> bool:
        """Check if a package is installed in the RAG venv"""
        if not self.is_venv_available:
            return False
        try:
            result = subprocess.run(
                [str(self.python_exe), '-c', f'import {package_name}'],
                capture_output=True,
                timeout=10
            )
            return result.returncode == 0
        except Exception:
            return False
    
    def execute_code(self, code: str, timeout: int = 60) -> Dict[str, Any]:
        """
        Execute Python code in the RAG venv and return the result.
        
        The code should print a JSON result to stdout.
        
        Args:
            code: Python code to execute
            timeout: Timeout in seconds
            
        Returns:
            Dict with 'success', 'result' or 'error' keys
        """
        if not self.is_venv_available:
            return {'success': False, 'error': 'RAG venv not available'}
        
        try:
            result = subprocess.run(
                [str(self.python_exe), '-c', code],
                capture_output=True,
                text=True,
                timeout=timeout
            )
            
            if result.returncode == 0:
                try:
                    output = json.loads(result.stdout.strip())
                    return {'success': True, 'result': output}
                except json.JSONDecodeError:
                    return {'success': True, 'result': result.stdout.strip()}
            else:
                return {
                    'success': False, 
                    'error': result.stderr.strip() or f'Exit code: {result.returncode}'
                }
                
        except subprocess.TimeoutExpired:
            return {'success': False, 'error': f'Timeout after {timeout}s'}
        except Exception as e:
            return {'success': False, 'error': str(e)}
    
    def execute_script(self, script_path: str, args: list = None, 
                       input_data: dict = None, timeout: int = 60) -> Dict[str, Any]:
        """
        Execute a Python script in the RAG venv.
        
        Args:
            script_path: Path to the Python script
            args: Command line arguments
            input_data: Data to pass via stdin as JSON
            timeout: Timeout in seconds
            
        Returns:
            Dict with 'success', 'result' or 'error' keys
        """
        if not self.is_venv_available:
            return {'success': False, 'error': 'RAG venv not available'}
        
        cmd = [str(self.python_exe), script_path]
        if args:
            cmd.extend(args)
        
        try:
            stdin_data = json.dumps(input_data) if input_data else None
            
            result = subprocess.run(
                cmd,
                input=stdin_data,
                capture_output=True,
                text=True,
                timeout=timeout
            )
            
            if result.returncode == 0:
                try:
                    output = json.loads(result.stdout.strip())
                    return {'success': True, 'result': output}
                except json.JSONDecodeError:
                    return {'success': True, 'result': result.stdout.strip()}
            else:
                return {
                    'success': False,
                    'error': result.stderr.strip() or f'Exit code: {result.returncode}'
                }
                
        except subprocess.TimeoutExpired:
            return {'success': False, 'error': f'Timeout after {timeout}s'}
        except Exception as e:
            return {'success': False, 'error': str(e)}


# Global executor instance
_executor: Optional[RAGSubprocessExecutor] = None


def get_rag_executor() -> RAGSubprocessExecutor:
    """Get the global RAG subprocess executor"""
    global _executor
    if _executor is None:
        _executor = RAGSubprocessExecutor()
    return _executor
