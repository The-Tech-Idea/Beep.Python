"""
Llama.cpp Binary Manager - LM Studio Style

This module provides a unified interface to the LlamaBackendInstaller
from cuda_installer.py. It downloads and manages pre-built llama.cpp 
server binaries.

No Python compilation needed - just download and run native executables.

Supports:
- Windows x64 (CUDA 12/13, Vulkan, SYCL, HIP, CPU)
- Linux x64 (Vulkan, CPU)
- macOS (Metal ARM64, x64)
"""
import os
from pathlib import Path
from typing import Dict, List, Optional, Any
from dataclasses import dataclass, asdict

# Import the existing backend installer
from app.services.cuda_installer import get_backend_installer, LlamaBackendInstaller


@dataclass
class BackendInfo:
    """Information about an available or installed backend"""
    id: str
    name: str
    description: str
    size: str
    requires_gpu: bool
    installed: bool
    installed_version: Optional[str] = None
    install_path: Optional[str] = None
    compatible: bool = True
    
    def to_dict(self) -> dict:
        return asdict(self)


class LlamaBinaryManager:
    """
    Manages pre-built llama.cpp server binaries.
    
    LM Studio style - downloads native executables, no Python compilation.
    
    This is a wrapper around LlamaBackendInstaller from cuda_installer.py
    to provide a consistent interface for the inference service.
    """
    _instance = None
    
    def __new__(cls):
        if cls._instance is None:
            cls._instance = super().__new__(cls)
            cls._instance._initialized = False
        return cls._instance
    
    def __init__(self):
        if self._initialized:
            return
        
        # Use the existing backend installer
        self._installer: LlamaBackendInstaller = get_backend_installer()
        
        self._initialized = True
    
    @property
    def backends_path(self) -> Path:
        """Get the backends installation directory"""
        return self._installer.backends_dir
    
    def get_platform(self) -> str:
        """Get current platform name"""
        return self._installer.platform
    
    def get_arch(self) -> str:
        """Get current architecture"""
        return self._installer.arch
    
    def get_available_backends(self) -> Dict[str, BackendInfo]:
        """Get all available backends for current platform"""
        raw_backends = self._installer.get_available_backends()
        
        backends = {}
        for backend_id, info in raw_backends.items():
            backends[backend_id] = BackendInfo(
                id=backend_id,
                name=info.get('name', backend_id),
                description=info.get('description', ''),
                size=info.get('size', 'Unknown'),
                requires_gpu=info.get('requires_gpu', False),
                installed=info.get('installed', False),
                installed_version=info.get('installed_version'),
                install_path=info.get('install_path'),
                compatible=True
            )
        
        return backends
    
    def get_installed_backends(self) -> List[BackendInfo]:
        """Get list of installed backends"""
        installed = self._installer.get_installed_backends()
        
        return [
            BackendInfo(
                id=info.get('id', 'unknown'),
                name=info.get('name', info.get('id', 'unknown')),
                description=info.get('description', ''),
                size=info.get('size', 'Unknown'),
                requires_gpu=info.get('requires_gpu', False),
                installed=True,
                installed_version=info.get('version'),
                install_path=info.get('path'),
                compatible=True
            )
            for info in installed
        ]
    
    def get_active_backend(self) -> Optional[BackendInfo]:
        """Get the currently active/preferred backend"""
        installed = self.get_installed_backends()
        if installed:
            # Prefer GPU backends over CPU
            for backend in installed:
                if backend.requires_gpu:
                    return backend
            # Fall back to first installed
            return installed[0]
        return None
    
    def download_backend(self, backend_id: str, 
                         progress_callback: Optional[callable] = None) -> Dict[str, Any]:
        """
        Download and install a backend
        
        Args:
            backend_id: Backend to download (e.g., 'cuda-12', 'vulkan', 'cpu')
            progress_callback: Optional callback(percent, message)
            
        Returns:
            Result dict with success, message, path
        """
        return self._installer.install_backend(backend_id, progress_callback)
    
    def uninstall_backend(self, backend_id: str) -> Dict[str, Any]:
        """Uninstall a backend"""
        return self._installer.uninstall_backend(backend_id)
    
    def get_server_executable(self, backend_id: Optional[str] = None) -> Optional[Path]:
        """Get path to llama-server executable for a backend"""
        if not backend_id:
            active = self.get_active_backend()
            if not active:
                return None
            backend_id = active.id
        
        backend_dir = self.backends_path / backend_id
        
        if not backend_dir.exists():
            return None
        
        # Look for llama-server or llama-server.exe
        if self._installer.is_windows:
            executable = backend_dir / 'bin' / 'llama-server.exe'
            if not executable.exists():
                executable = backend_dir / 'llama-server.exe'
        else:
            executable = backend_dir / 'bin' / 'llama-server'
            if not executable.exists():
                executable = backend_dir / 'llama-server'
        
        # Also check in root of extracted folder
        if not executable.exists():
            for file in backend_dir.rglob('llama-server*'):
                if file.is_file() and (file.suffix == '' or file.suffix == '.exe'):
                    return file
        
        if executable.exists():
            return executable
        
        return None
    
    def get_recommended_backend(self) -> str:
        """Get recommended backend for current system"""
        # Check available backends and recommend the best one
        available = self.get_available_backends()
        
        # Priority order based on platform
        if self._installer.is_windows:
            priority = ['cuda', 'vulkan', 'hip', 'sycl', 'cpu']
        elif self._installer.is_macos:
            priority = ['metal', 'cpu']
        else:
            priority = ['vulkan', 'cpu']
        
        for backend in priority:
            if backend in available:
                return backend
        
        return 'cpu'


# Singleton accessor
_manager_instance = None

def get_llama_binary_manager() -> LlamaBinaryManager:
    """Get the singleton LlamaBinaryManager instance"""
    global _manager_instance
    if _manager_instance is None:
        _manager_instance = LlamaBinaryManager()
    return _manager_instance
