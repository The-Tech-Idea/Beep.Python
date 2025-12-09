"""
GPU Backend Runtime Installer Service - LM Studio Style

Downloads prebuilt llama.cpp binaries with GPU support already bundled.
This is exactly how LM Studio does it:
- Download prebuilt binaries from llama.cpp GitHub releases
- All CUDA/Vulkan/Metal libraries are BUNDLED in the download
- No separate SDK installation required
- No admin privileges needed
- No system restart required

Supported Backends (from llama.cpp releases):
- Windows x64 CPU
- Windows x64 CUDA 12 (includes cudart DLLs ~373MB)
- Windows x64 CUDA 13 (includes cudart DLLs ~384MB)
- Windows x64 Vulkan
- Windows x64 SYCL (Intel)
- Windows x64 HIP (AMD ROCm)
- Windows arm64 CPU
- Linux x64 CPU
- Linux x64 Vulkan
- macOS arm64 (Metal built-in)
- macOS x64 (Metal built-in)
"""
import os
import platform
import subprocess
import requests
import zipfile
import tarfile
import shutil
import json
from pathlib import Path
from typing import Dict, Any, Optional, Callable, List
from datetime import datetime


class LlamaBackendInstaller:
    """
    Service for installing llama.cpp prebuilt backends - LM Studio style.
    Downloads complete binaries with all GPU libraries bundled.
    """
    
    # GitHub API for llama.cpp releases
    LLAMA_CPP_RELEASES_API = 'https://api.github.com/repos/ggml-org/llama.cpp/releases/latest'
    LLAMA_CPP_RELEASES_URL = 'https://github.com/ggml-org/llama.cpp/releases/download'
    
    # Backend asset patterns for each platform
    # Format: {platform}_{arch}_{backend} -> asset filename pattern
    # These match EXACTLY the GitHub release asset names
    # For 'cuda' backend, we find the latest CUDA version dynamically from release assets
    BACKEND_ASSETS = {
        'Windows': {
            'x64': {
                'cpu': 'llama-{version}-bin-win-cpu-x64.zip',
                'cuda': 'llama-{version}-bin-win-cuda-{cuda_version}-x64.zip',  # cuda_version auto-detected
                'vulkan': 'llama-{version}-bin-win-vulkan-x64.zip',
                'sycl': 'llama-{version}-bin-win-sycl-x64.zip',
                'hip': 'llama-{version}-bin-win-hip-radeon-x64.zip',
            },
            'arm64': {
                'cpu': 'llama-{version}-bin-win-cpu-arm64.zip',
                'opencl-adreno': 'llama-{version}-bin-win-opencl-adreno-arm64.zip',
            }
        },
        'Linux': {
            'x64': {
                'cpu': 'llama-{version}-bin-ubuntu-x64.zip',
                'vulkan': 'llama-{version}-bin-ubuntu-vulkan-x64.zip',
            },
            's390x': {
                'cpu': 'llama-{version}-bin-ubuntu-s390x.zip',
            }
        },
        'Darwin': {  # macOS
            'arm64': {
                'metal': 'llama-{version}-bin-macos-arm64.zip',  # Metal is default
            },
            'x64': {
                'metal': 'llama-{version}-bin-macos-x64.zip',  # Metal is default
            }
        }
    }
    
    # CUDA runtime assets (bundled with CUDA binaries) - version auto-detected
    CUDART_ASSETS = {
        'cuda': 'cudart-llama-bin-win-cuda-{cuda_version}-x64.zip',
    }
    
    # Backend display info
    BACKEND_INFO = {
        'cpu': {
            'name': 'CPU (OpenBLAS)',
            'description': 'CPU-only inference with OpenBLAS optimization',
            'size': '~17 MB',
            'requires_gpu': False,
        },
        'cuda': {
            'name': 'NVIDIA CUDA',
            'description': 'GPU acceleration for NVIDIA GPUs (auto-selects latest CUDA version)',
            'size': '~200 MB (includes runtime)',
            'requires_gpu': True,
            'gpu_vendor': 'nvidia',
            'min_driver': 520,
        },
        'vulkan': {
            'name': 'Vulkan',
            'description': 'Cross-platform GPU (NVIDIA, AMD, Intel)',
            'size': '~32 MB',
            'requires_gpu': True,
        },
        'hip': {
            'name': 'AMD ROCm/HIP (Radeon)',
            'description': 'GPU acceleration for AMD Radeon GPUs',
            'size': '~340 MB',
            'requires_gpu': True,
            'gpu_vendor': 'amd',
        },
        'sycl': {
            'name': 'Intel SYCL/OneAPI',
            'description': 'GPU acceleration for Intel Arc/Xe GPUs',
            'size': '~106 MB',
            'requires_gpu': True,
            'gpu_vendor': 'intel',
        },
        'metal': {
            'name': 'Apple Metal',
            'description': 'GPU acceleration for Apple Silicon (M1/M2/M3)',
            'size': '~14 MB',
            'requires_gpu': True,
            'gpu_vendor': 'apple',
        },
        'opencl-adreno': {
            'name': 'OpenCL Adreno (Windows ARM)',
            'description': 'GPU acceleration for Qualcomm Adreno GPUs',
            'size': '~14 MB',
            'requires_gpu': True,
            'gpu_vendor': 'qualcomm',
        },
    }
    
    def __init__(self, base_path: Optional[str] = None):
        self.platform = platform.system()
        self.arch = self._get_arch()
        self.is_windows = self.platform == 'Windows'
        self.is_linux = self.platform == 'Linux'
        self.is_macos = self.platform == 'Darwin'
        
        # Local installation directory (no admin required)
        if base_path:
            self.base_path = Path(base_path)
        else:
            self.base_path = Path.home() / '.beep-llm'
        
        self.backends_dir = self.base_path / 'backends'
        self.download_dir = self.base_path / 'downloads'
        self.cache_file = self.base_path / 'backends_cache.json'
        
        # Ensure directories exist
        for d in [self.backends_dir, self.download_dir]:
            d.mkdir(parents=True, exist_ok=True)
        
        # Cache for release info
        self._release_cache = None
        self._release_cache_time = None
    
    def _get_arch(self) -> str:
        """Get system architecture"""
        machine = platform.machine().lower()
        if machine in ('x86_64', 'amd64'):
            return 'x64'
        elif machine in ('aarch64', 'arm64'):
            return 'arm64'
        elif machine == 's390x':
            return 's390x'
        return 'x64'  # Default
    
    def _get_latest_release(self, force_refresh: bool = False) -> Optional[Dict]:
        """Get latest llama.cpp release info from GitHub"""
        # Use cache if recent (5 minutes)
        if not force_refresh and self._release_cache and self._release_cache_time:
            age = (datetime.now() - self._release_cache_time).total_seconds()
            if age < 300:  # 5 minutes
                return self._release_cache
        
        try:
            headers = {'Accept': 'application/vnd.github.v3+json'}
            response = requests.get(self.LLAMA_CPP_RELEASES_API, headers=headers, timeout=10)
            response.raise_for_status()
            self._release_cache = response.json()
            self._release_cache_time = datetime.now()
            return self._release_cache
        except Exception as e:
            print(f"Failed to fetch release info: {e}")
            return None
    
    def _find_latest_cuda_version(self, assets: Dict[str, Any]) -> Optional[str]:
        """
        Find the latest CUDA version from release assets.
        Looks for patterns like 'win-cuda-13.1-x64' or 'win-cuda-12.4-x64'
        Returns the version string like '13.1' or '12.4'
        """
        import re
        cuda_versions = []
        
        for asset_name in assets.keys():
            # Match patterns like: win-cuda-12.4-x64 or win-cuda-13.1-x64
            match = re.search(r'win-cuda-(\d+\.\d+)-x64', asset_name)
            if match:
                version_str = match.group(1)
                try:
                    # Parse as tuple for proper numeric sorting (13.1 > 12.4)
                    major, minor = version_str.split('.')
                    cuda_versions.append((int(major), int(minor), version_str))
                except:
                    pass
        
        if cuda_versions:
            # Sort by major then minor version, get the highest
            cuda_versions.sort(reverse=True)
            return cuda_versions[0][2]  # Return the version string
        
        return None

    def get_available_backends(self) -> Dict[str, Any]:
        """Get list of available backends for this platform"""
        available = {}
        
        platform_backends = self.BACKEND_ASSETS.get(self.platform, {})
        arch_backends = platform_backends.get(self.arch, {})
        
        for backend_id, asset_pattern in arch_backends.items():
            backend_info = self.BACKEND_INFO.get(backend_id, {})
            installed = self.check_backend_installed(backend_id)
            
            available[backend_id] = {
                'id': backend_id,
                'name': backend_info.get('name', backend_id),
                'description': backend_info.get('description', ''),
                'size': backend_info.get('size', 'Unknown'),
                'requires_gpu': backend_info.get('requires_gpu', False),
                'installed': installed.get('installed', False),
                'installed_version': installed.get('version'),
                'install_path': installed.get('path'),
            }
        
        return available
    
    def check_backend_installed(self, backend_id: str) -> Dict[str, Any]:
        """Check if a backend is installed"""
        backend_dir = self.backends_dir / backend_id
        marker_file = backend_dir / 'installed.json'
        
        if marker_file.exists():
            try:
                with open(marker_file, 'r') as f:
                    info = json.load(f)
                return {
                    'installed': True,
                    'version': info.get('version'),
                    'path': str(backend_dir),
                    'installed_date': info.get('installed_date'),
                }
            except:
                pass
        
        return {'installed': False}
    
    def get_installed_backends(self) -> List[Dict[str, Any]]:
        """Get list of all installed backends"""
        installed = []
        if self.backends_dir.exists():
            for backend_dir in self.backends_dir.iterdir():
                if backend_dir.is_dir():
                    marker = backend_dir / 'installed.json'
                    if marker.exists():
                        try:
                            with open(marker, 'r') as f:
                                info = json.load(f)
                            info['id'] = backend_dir.name
                            info['path'] = str(backend_dir)
                            installed.append(info)
                        except:
                            pass
        return installed
    
    def install_backend(
        self, 
        backend_id: str, 
        progress_callback: Optional[Callable] = None
    ) -> Dict[str, Any]:
        """
        Install a backend by downloading prebuilt llama.cpp binaries.
        This includes all required runtime libraries (CUDA, Vulkan, etc.)
        """
        # Validate backend
        platform_backends = self.BACKEND_ASSETS.get(self.platform, {})
        arch_backends = platform_backends.get(self.arch, {})
        
        if backend_id not in arch_backends:
            return {
                'success': False,
                'error': f'Backend {backend_id} not available for {self.platform}/{self.arch}'
            }
        
        asset_pattern = arch_backends[backend_id]
        
        if progress_callback:
            progress_callback(5, 'Fetching release information...')
        
        # Get latest release
        release = self._get_latest_release()
        if not release:
            return {'success': False, 'error': 'Failed to fetch release information'}
        
        version = release.get('tag_name', 'unknown')
        assets = {a['name']: a for a in release.get('assets', [])}
        
        # For CUDA backend, find the latest CUDA version dynamically
        cuda_version = None
        if backend_id == 'cuda':
            cuda_version = self._find_latest_cuda_version(assets)
            if not cuda_version:
                return {'success': False, 'error': 'No CUDA binaries found in release'}
            if progress_callback:
                progress_callback(8, f'Found latest CUDA version: {cuda_version}')
        
        # Find the correct asset - substitute both version and cuda_version
        asset_name = asset_pattern.format(version=version, cuda_version=cuda_version or '')
        
        # Try alternate naming patterns
        possible_names = [
            asset_name,
            asset_name.replace('-x64', '-x86_64'),
            asset_name.replace('.zip', '.tar.gz'),
        ]
        
        download_asset = None
        for name in possible_names:
            if name in assets:
                download_asset = assets[name]
                break
        
        # Also search by partial match for non-CUDA backends
        if not download_asset and backend_id != 'cuda':
            for asset_file, asset_info in assets.items():
                if backend_id.replace('-', '') in asset_file.lower().replace('-', ''):
                    if self.platform.lower()[:3] in asset_file.lower() or 'win' in asset_file.lower():
                        download_asset = asset_info
                        break
        
        if not download_asset:
            # List available for debugging
            available_assets = list(assets.keys())
            return {
                'success': False, 
                'error': f'Asset not found for {backend_id}. Looking for: {asset_name}',
                'available_assets': available_assets[:10]
            }
        
        download_url = download_asset['browser_download_url']
        download_name = download_asset['name']
        download_size = download_asset.get('size', 0)
        
        if progress_callback:
            size_mb = download_size / 1024 / 1024
            progress_callback(10, f'Downloading {download_name} ({size_mb:.1f} MB)...')
        
        # Download
        download_path = self.download_dir / download_name
        
        try:
            if not self._download_file(download_url, download_path, download_size, progress_callback):
                return {'success': False, 'error': 'Download failed'}
            
            if progress_callback:
                progress_callback(70, 'Extracting files...')
            
            # Extract
            backend_dir = self.backends_dir / backend_id
            if backend_dir.exists():
                shutil.rmtree(backend_dir, ignore_errors=True)
            backend_dir.mkdir(parents=True, exist_ok=True)
            
            if not self._extract_archive(download_path, backend_dir):
                return {'success': False, 'error': 'Extraction failed'}
            
            # For CUDA backend on Windows, also download cudart package
            if self.is_windows and backend_id == 'cuda' and cuda_version:
                if progress_callback:
                    progress_callback(80, f'Downloading CUDA {cuda_version} runtime libraries...')
                
                cudart_pattern = self.CUDART_ASSETS.get('cuda')
                if cudart_pattern:
                    cudart_name = cudart_pattern.format(cuda_version=cuda_version)
                    if cudart_name in assets:
                        cudart_asset = assets[cudart_name]
                        cudart_path = self.download_dir / cudart_name
                        
                        if not cudart_path.exists() or cudart_path.stat().st_size != cudart_asset.get('size', 0):
                            self._download_file(
                                cudart_asset['browser_download_url'],
                                cudart_path,
                                cudart_asset.get('size', 0),
                                None  # No callback for this one
                            )
                        
                        if cudart_path.exists():
                            if progress_callback:
                                progress_callback(85, 'Extracting CUDA runtime...')
                            self._extract_archive(cudart_path, backend_dir)
            
            if progress_callback:
                progress_callback(90, 'Configuring environment...')
            
            # Write marker file - include cuda_version if applicable
            marker_file = backend_dir / 'installed.json'
            install_info = {
                'version': version,
                'backend_id': backend_id,
                'cuda_version': cuda_version,  # Track which CUDA version was installed
                'asset_name': download_name,
                'installed_date': datetime.now().isoformat(),
                'platform': self.platform,
                'arch': self.arch,
            }
            with open(marker_file, 'w') as f:
                json.dump(install_info, f, indent=2)
            
            # Add to PATH for current process
            backend_path_str = str(backend_dir)
            self._add_to_path(backend_path_str)
            
            # Find subdirectories that might contain binaries
            for subdir in backend_dir.iterdir():
                if subdir.is_dir():
                    self._add_to_path(str(subdir))
                    # Check for bin subdirectory
                    bin_dir = subdir / 'bin'
                    if bin_dir.exists():
                        self._add_to_path(str(bin_dir))
            
            if progress_callback:
                progress_callback(100, f'{backend_id} installed successfully!')
            
            return {
                'success': True,
                'message': f'{backend_id} backend installed successfully',
                'version': version,
                'path': str(backend_dir),
                'requires_admin': False,
                'requires_restart': False,
                'silent': True,
            }
            
        except Exception as e:
            import traceback
            traceback.print_exc()
            return {'success': False, 'error': str(e)}
    
    def _add_to_path(self, path_str: str):
        """Add directory to PATH for current process"""
        if self.is_windows:
            current = os.environ.get('PATH', '')
            if path_str not in current:
                os.environ['PATH'] = path_str + os.pathsep + current
        else:
            current = os.environ.get('LD_LIBRARY_PATH', '')
            if path_str not in current:
                os.environ['LD_LIBRARY_PATH'] = path_str + os.pathsep + current
    
    def _download_file(
        self, 
        url: str, 
        dest_path: Path, 
        total_size: int = 0,
        progress_callback: Optional[Callable] = None
    ) -> bool:
        """Download file with progress"""
        try:
            response = requests.get(url, stream=True, timeout=600, allow_redirects=True)
            response.raise_for_status()
            
            if total_size == 0:
                total_size = int(response.headers.get('content-length', 0))
            
            downloaded = 0
            with open(dest_path, 'wb') as f:
                for chunk in response.iter_content(chunk_size=131072):  # 128KB chunks
                    if chunk:
                        f.write(chunk)
                        downloaded += len(chunk)
                        
                        if progress_callback and total_size > 0:
                            # Scale progress from 10-70%
                            percent = 10 + int((downloaded / total_size) * 60)
                            mb_done = downloaded / 1024 / 1024
                            mb_total = total_size / 1024 / 1024
                            progress_callback(percent, f'Downloading: {mb_done:.1f} / {mb_total:.1f} MB')
            
            return True
        except Exception as e:
            print(f"Download error: {e}")
            return False
    
    def _extract_archive(self, archive_path: Path, dest_dir: Path) -> bool:
        """Extract zip or tar archive"""
        try:
            if archive_path.suffix == '.zip':
                with zipfile.ZipFile(archive_path, 'r') as zf:
                    zf.extractall(dest_dir)
            elif archive_path.name.endswith('.tar.gz'):
                with tarfile.open(archive_path, 'r:gz') as tf:
                    tf.extractall(dest_dir)
            elif archive_path.name.endswith('.tar.xz'):
                with tarfile.open(archive_path, 'r:xz') as tf:
                    tf.extractall(dest_dir)
            else:
                # Try zip first
                try:
                    with zipfile.ZipFile(archive_path, 'r') as zf:
                        zf.extractall(dest_dir)
                except:
                    return False
            return True
        except Exception as e:
            print(f"Extraction error: {e}")
            return False
    
    def uninstall_backend(self, backend_id: str) -> Dict[str, Any]:
        """Uninstall a backend"""
        backend_dir = self.backends_dir / backend_id
        
        if not backend_dir.exists():
            return {'success': False, 'error': f'Backend {backend_id} not installed'}
        
        try:
            shutil.rmtree(backend_dir)
            return {
                'success': True,
                'message': f'{backend_id} uninstalled successfully'
            }
        except Exception as e:
            return {'success': False, 'error': str(e)}
    
    def check_for_updates(self) -> Dict[str, Any]:
        """Check for updates to installed backends"""
        release = self._get_latest_release(force_refresh=True)
        if not release:
            return {'success': False, 'error': 'Failed to fetch release info'}
        
        latest_version = release.get('tag_name', 'unknown')
        installed = self.get_installed_backends()
        
        updates_available = []
        for backend in installed:
            if backend.get('version') != latest_version:
                updates_available.append({
                    'id': backend.get('id'),
                    'current_version': backend.get('version'),
                    'latest_version': latest_version,
                })
        
        return {
            'success': True,
            'latest_version': latest_version,
            'updates_available': updates_available,
            'installed_backends': installed,
        }
    
    def get_backend_path(self, backend_id: str) -> Optional[str]:
        """Get installation path for a backend"""
        backend_dir = self.backends_dir / backend_id
        if backend_dir.exists():
            return str(backend_dir)
        return None
    
    def get_recommended_backend(self) -> str:
        """Get recommended backend based on system hardware"""
        # Check for NVIDIA GPU
        try:
            result = subprocess.run(
                ['nvidia-smi', '--query-gpu=driver_version', '--format=csv,noheader'],
                capture_output=True, text=True, timeout=5
            )
            if result.returncode == 0:
                driver_version = result.stdout.strip().split('.')[0]
                driver_major = int(driver_version) if driver_version.isdigit() else 0
                if driver_major >= 520:
                    return 'cuda'  # Unified CUDA backend
                else:
                    return 'vulkan'
        except:
            pass
        
        # Check for AMD GPU on Linux
        if self.is_linux:
            try:
                result = subprocess.run(['rocm-smi'], capture_output=True, timeout=5)
                if result.returncode == 0:
                    return 'hip'
            except:
                pass
        
        # Check for Apple Silicon
        if self.is_macos and self.arch == 'arm64':
            return 'metal'
        
        # Check for Vulkan
        try:
            result = subprocess.run(['vulkaninfo', '--summary'], capture_output=True, timeout=5)
            if result.returncode == 0:
                return 'vulkan'
        except:
            pass
        
        # Default to CPU
        return 'cpu'


# Backward compatibility aliases
class RuntimeLibraryInstaller(LlamaBackendInstaller):
    """Alias for backward compatibility"""
    pass


class ToolkitInstaller(LlamaBackendInstaller):
    """Alias for backward compatibility"""
    
    def install_cuda_runtime(self, version: Optional[str] = None, progress_callback: Optional[Callable] = None):
        """Backward compatible CUDA install"""
        return self.install_backend('cuda', progress_callback)
    
    def install_vulkan_runtime(self, progress_callback: Optional[Callable] = None):
        """Backward compatible Vulkan install"""
        return self.install_backend('vulkan', progress_callback)
    
    def install_rocm_runtime(self, progress_callback: Optional[Callable] = None):
        """Backward compatible ROCm install"""
        if self.is_windows:
            return self.install_backend('hip', progress_callback)
        return {'success': False, 'error': 'ROCm only available on Linux', 'manual_install': True}
    
    def install_openblas_runtime(self, progress_callback: Optional[Callable] = None):
        """Backward compatible OpenBLAS install (uses CPU backend)"""
        return self.install_backend('cpu', progress_callback)
    
    def check_cuda_runtime_installed(self, version: Optional[str] = None):
        """Backward compatible check"""
        result = self.check_backend_installed('cuda')
        return {'installed': result.get('installed', False), **result}
    
    def check_vulkan_installed(self):
        """Backward compatible check"""
        result = self.check_backend_installed('vulkan')
        return {'installed': result.get('installed', False), **result}
    
    def check_toolkit_installed(self, backend: str) -> Dict[str, Any]:
        """Check if a toolkit/backend is installed"""
        backend_map = {
            'cuda': 'cuda',
            'nvidia': 'cuda',
            'cuda12': 'cuda',
            'cuda-12': 'cuda',
            'cuda13': 'cuda',
            'cuda-13': 'cuda',
            'vulkan': 'vulkan',
            'rocm': 'hip',
            'amd': 'hip',
            'hip': 'hip',
            'metal': 'metal',
            'sycl': 'sycl',
            'intel': 'sycl',
            'cpu': 'cpu',
            'openblas': 'cpu',
        }
        backend_id = backend_map.get(backend.lower(), backend.lower())
        result = self.check_backend_installed(backend_id)
        return {
            'installed': result.get('installed', False),
            'available': result.get('installed', False),
            'version': result.get('version'),
            'path': result.get('path'),
            'backend': backend_id,
        }
    
    def install_toolkit(self, backend: str, version: Optional[str] = None, progress_callback: Optional[Callable] = None) -> Dict[str, Any]:
        """
        Install a toolkit/backend - LM Studio style (prebuilt binaries).
        This is the main entry point for installing any backend.
        """
        backend_map = {
            'cuda': 'cuda-12',
            'nvidia': 'cuda-12',
            'cuda12': 'cuda-12',
            'cuda-12': 'cuda-12',
            'cuda13': 'cuda-13',
            'cuda-13': 'cuda-13',
            'cuda11': 'cuda-12',  # Fallback to 12, 11 not in prebuilt
            'vulkan': 'vulkan',
            'rocm': 'hip',
            'amd': 'hip',
            'hip': 'hip',
            'metal': 'metal',
            'sycl': 'sycl',
            'intel': 'sycl',
            'cpu': 'cpu',
            'openblas': 'cpu',
        }
        
        backend_id = backend_map.get(backend.lower(), backend.lower())
        
        # Override version if specified
        if version:
            if '12' in version:
                backend_id = 'cuda-12'
            elif '13' in version:
                backend_id = 'cuda-13'
        
        return self.install_backend(backend_id, progress_callback)
    
    def download_cuda_installer(self, version: Optional[str] = None, progress_callback: Optional[Callable] = None) -> Dict[str, Any]:
        """Backward compatible - now just installs the CUDA backend"""
        return self.install_toolkit('cuda', version, progress_callback)
    
    def run_cuda_installer(self, version: Optional[str] = None, silent: bool = True) -> Dict[str, Any]:
        """Backward compatible - now just installs the CUDA backend"""
        return self.install_toolkit('cuda', version)


# Singleton instances
_toolkit_installer: Optional[ToolkitInstaller] = None
_cuda_installer: Optional[ToolkitInstaller] = None
_backend_installer: Optional[LlamaBackendInstaller] = None


def get_toolkit_installer() -> ToolkitInstaller:
    """Get singleton ToolkitInstaller instance"""
    global _toolkit_installer
    if _toolkit_installer is None:
        _toolkit_installer = ToolkitInstaller()
    return _toolkit_installer


def get_cuda_installer() -> ToolkitInstaller:
    """Get singleton cuda installer (same as toolkit installer)"""
    global _cuda_installer
    if _cuda_installer is None:
        _cuda_installer = ToolkitInstaller()
    return _cuda_installer


def get_backend_installer() -> LlamaBackendInstaller:
    """Get singleton LlamaBackendInstaller instance"""
    global _backend_installer
    if _backend_installer is None:
        _backend_installer = LlamaBackendInstaller()
    return _backend_installer