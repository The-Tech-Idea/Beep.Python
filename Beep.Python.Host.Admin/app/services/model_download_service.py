"""
Unified Model Download Service

Supports downloading models from multiple sources:
- HuggingFace Hub
- Direct URLs (GitHub releases, direct downloads)
- Civitai (for Stable Diffusion models)
- ModelScope (Chinese alternative)
- Custom repositories
"""
import os
import re
import json
import hashlib
import requests
import logging
from pathlib import Path
from typing import Optional, Dict, Any, Callable, List
from dataclasses import dataclass
from enum import Enum
from urllib.parse import urlparse, parse_qs

logger = logging.getLogger(__name__)


class ModelSource(Enum):
    """Supported model sources"""
    HUGGINGFACE = "huggingface"
    DIRECT_URL = "direct_url"
    CIVITAI = "civitai"
    MODELSCOPE = "modelscope"
    GITHUB_RELEASE = "github_release"
    CUSTOM = "custom"


@dataclass
class DownloadResult:
    """Result of a model download"""
    success: bool
    model_id: str
    source: ModelSource
    local_path: Optional[Path] = None
    error: Optional[str] = None
    metadata: Optional[Dict[str, Any]] = None


class ModelDownloadService:
    """Unified service for downloading models from multiple sources"""
    
    def __init__(self):
        self.session = requests.Session()
        self.session.headers.update({
            'User-Agent': 'Beep.Python.Host.Admin/1.0'
        })
    
    def detect_source(self, model_id_or_url: str) -> ModelSource:
        """
        Detect the source type from model ID or URL
        
        Args:
            model_id_or_url: Model ID (e.g., "user/model") or URL
            
        Returns:
            Detected ModelSource
        """
        # Check if it's a URL
        if model_id_or_url.startswith(('http://', 'https://')):
            url = model_id_or_url.lower()
            
            # Civitai
            if 'civitai.com' in url or 'civitai' in url:
                return ModelSource.CIVITAI
            
            # GitHub releases
            if 'github.com' in url and ('/releases/' in url or '/release/' in url):
                return ModelSource.GITHUB_RELEASE
            
            # ModelScope
            if 'modelscope.cn' in url or 'modelscope' in url:
                return ModelSource.MODELSCOPE
            
            # Direct URL
            return ModelSource.DIRECT_URL
        
        # Check if it looks like HuggingFace format (user/model)
        if '/' in model_id_or_url and not model_id_or_url.startswith('http'):
            # Could be HuggingFace or ModelScope format
            # Default to HuggingFace, but can be overridden
            return ModelSource.HUGGINGFACE
        
        # Default to HuggingFace
        return ModelSource.HUGGINGFACE
    
    def download_from_huggingface(self, 
                                  model_id: str,
                                  filename: Optional[str] = None,
                                  progress_callback: Optional[Callable] = None,
                                  token: Optional[str] = None) -> DownloadResult:
        """Download from HuggingFace Hub"""
        try:
            from app.services.huggingface_service import HuggingFaceService
            from huggingface_hub import snapshot_download, hf_hub_download
            
            hf_service = HuggingFaceService()
            if token:
                hf_service.llm_manager.set_huggingface_token(token)
            
            # If filename specified, download single file
            if filename:
                local_path = hf_hub_download(
                    repo_id=model_id,
                    filename=filename,
                    token=token,
                    resume_download=True
                )
                return DownloadResult(
                    success=True,
                    model_id=model_id,
                    source=ModelSource.HUGGINGFACE,
                    local_path=Path(local_path),
                    metadata={'filename': filename}
                )
            else:
                # Download entire model
                local_dir = snapshot_download(
                    repo_id=model_id,
                    token=token,
                    resume_download=True
                )
                return DownloadResult(
                    success=True,
                    model_id=model_id,
                    source=ModelSource.HUGGINGFACE,
                    local_path=Path(local_dir),
                    metadata={'type': 'full_model'}
                )
        except Exception as e:
            logger.error(f"Error downloading from HuggingFace: {e}", exc_info=True)
            return DownloadResult(
                success=False,
                model_id=model_id,
                source=ModelSource.HUGGINGFACE,
                error=str(e)
            )
    
    def download_from_direct_url(self,
                                 url: str,
                                 save_path: Path,
                                 progress_callback: Optional[Callable] = None,
                                 headers: Optional[Dict[str, str]] = None) -> DownloadResult:
        """Download from direct URL"""
        try:
            save_path.parent.mkdir(parents=True, exist_ok=True)
            
            # Prepare headers
            request_headers = self.session.headers.copy()
            if headers:
                request_headers.update(headers)
            
            # Start download with streaming
            response = self.session.get(url, headers=request_headers, stream=True, timeout=30)
            response.raise_for_status()
            
            # Get file size
            total_size = int(response.headers.get('content-length', 0))
            downloaded = 0
            
            # Download file
            with open(save_path, 'wb') as f:
                for chunk in response.iter_content(chunk_size=8192):
                    if chunk:
                        f.write(chunk)
                        downloaded += len(chunk)
                        
                        # Update progress
                        if progress_callback and total_size > 0:
                            progress = (downloaded / total_size) * 100
                            progress_callback({
                                'downloaded': downloaded,
                                'total': total_size,
                                'percentage': progress,
                                'status': 'downloading'
                            })
            
            return DownloadResult(
                success=True,
                model_id=url,
                source=ModelSource.DIRECT_URL,
                local_path=save_path,
                metadata={'url': url, 'size': downloaded}
            )
        except Exception as e:
            logger.error(f"Error downloading from URL: {e}", exc_info=True)
            return DownloadResult(
                success=False,
                model_id=url,
                source=ModelSource.DIRECT_URL,
                error=str(e)
            )
    
    def download_from_civitai(self,
                             model_id_or_url: str,
                             save_path: Path,
                             progress_callback: Optional[Callable] = None,
                             api_key: Optional[str] = None) -> DownloadResult:
        """Download from Civitai"""
        try:
            # Civitai API endpoint
            civitai_api = "https://civitai.com/api/v1"
            
            # Extract model ID from URL if needed
            if model_id_or_url.startswith('http'):
                # Extract ID from URL like https://civitai.com/models/12345
                match = re.search(r'/models/(\d+)', model_id_or_url)
                model_id = match.group(1) if match else None
            else:
                model_id = model_id_or_url
            
            if not model_id:
                return DownloadResult(
                    success=False,
                    model_id=model_id_or_url,
                    source=ModelSource.CIVITAI,
                    error="Could not extract model ID from URL"
                )
            
            # Get model details
            headers = {}
            if api_key:
                headers['Authorization'] = f'Bearer {api_key}'
            
            model_response = self.session.get(
                f"{civitai_api}/models/{model_id}",
                headers=headers
            )
            model_response.raise_for_status()
            model_data = model_response.json()
            
            # Get the latest model version
            model_versions = model_data.get('modelVersions', [])
            if not model_versions:
                return DownloadResult(
                    success=False,
                    model_id=model_id,
                    source=ModelSource.CIVITAI,
                    error="No model versions found"
                )
            
            latest_version = model_versions[0]
            files = latest_version.get('files', [])
            
            if not files:
                return DownloadResult(
                    success=False,
                    model_id=model_id,
                    source=ModelSource.CIVITAI,
                    error="No files found for this model"
                )
            
            # Download the main model file (usually the largest)
            main_file = max(files, key=lambda f: f.get('sizeKB', 0))
            download_url = main_file.get('downloadUrl')
            
            if not download_url:
                return DownloadResult(
                    success=False,
                    model_id=model_id,
                    source=ModelSource.CIVITAI,
                    error="No download URL found"
                )
            
            # Download the file
            result = self.download_from_direct_url(
                download_url,
                save_path,
                progress_callback,
                headers=headers
            )
            
            if result.success:
                result.metadata = {
                    'civitai_model_id': model_id,
                    'model_name': model_data.get('name'),
                    'version': latest_version.get('name'),
                    'file_name': main_file.get('name')
                }
            
            return result
            
        except Exception as e:
            logger.error(f"Error downloading from Civitai: {e}", exc_info=True)
            return DownloadResult(
                success=False,
                model_id=model_id_or_url,
                source=ModelSource.CIVITAI,
                error=str(e)
            )
    
    def download_from_github_release(self,
                                    url: str,
                                    save_path: Path,
                                    progress_callback: Optional[Callable] = None,
                                    token: Optional[str] = None) -> DownloadResult:
        """Download from GitHub release"""
        try:
            # Extract repo and release info from URL
            # Format: https://github.com/user/repo/releases/tag/v1.0.0
            # or: https://github.com/user/repo/releases/download/v1.0.0/file.zip
            match = re.search(r'github\.com/([^/]+)/([^/]+)/releases/(?:download|tag)/([^/]+)(?:/(.+))?', url)
            
            if not match:
                # Try direct asset URL
                return self.download_from_direct_url(url, save_path, progress_callback)
            
            repo_owner, repo_name, release_tag, filename = match.groups()
            
            # If filename not in URL, get latest release assets
            if not filename:
                headers = {}
                if token:
                    headers['Authorization'] = f'token {token}'
                
                release_url = f"https://api.github.com/repos/{repo_owner}/{repo_name}/releases/tags/{release_tag}"
                release_response = self.session.get(release_url, headers=headers)
                release_response.raise_for_status()
                release_data = release_response.json()
                
                assets = release_data.get('assets', [])
                if not assets:
                    return DownloadResult(
                        success=False,
                        model_id=url,
                        source=ModelSource.GITHUB_RELEASE,
                        error="No assets found in release"
                    )
                
                # Download the first asset (or largest)
                asset = max(assets, key=lambda a: a.get('size', 0))
                download_url = asset['browser_download_url']
                filename = asset['name']
            else:
                # Construct download URL
                download_url = f"https://github.com/{repo_owner}/{repo_name}/releases/download/{release_tag}/{filename}"
            
            # Download the file
            headers = {}
            if token:
                headers['Authorization'] = f'token {token}'
            
            result = self.download_from_direct_url(
                download_url,
                save_path,
                progress_callback,
                headers=headers
            )
            
            if result.success:
                result.metadata = {
                    'github_repo': f"{repo_owner}/{repo_name}",
                    'release_tag': release_tag,
                    'filename': filename
                }
            
            return result
            
        except Exception as e:
            logger.error(f"Error downloading from GitHub: {e}", exc_info=True)
            return DownloadResult(
                success=False,
                model_id=url,
                source=ModelSource.GITHUB_RELEASE,
                error=str(e)
            )
    
    def download_from_modelscope(self,
                                 model_id: str,
                                 save_path: Path,
                                 progress_callback: Optional[Callable] = None,
                                 token: Optional[str] = None) -> DownloadResult:
        """Download from ModelScope (Chinese alternative to HuggingFace)"""
        try:
            # ModelScope uses similar API to HuggingFace
            # Try using modelscope library if available
            try:
                from modelscope import snapshot_download
                
                local_dir = snapshot_download(
                    model_id=model_id,
                    cache_dir=str(save_path.parent),
                    token=token
                )
                
                return DownloadResult(
                    success=True,
                    model_id=model_id,
                    source=ModelSource.MODELSCOPE,
                    local_path=Path(local_dir),
                    metadata={'type': 'full_model'}
                )
            except ImportError:
                # Fallback: use API directly
                modelscope_api = "https://www.modelscope.cn/api/v1"
                
                # Get model info
                headers = {}
                if token:
                    headers['Authorization'] = f'Bearer {token}'
                
                model_response = self.session.get(
                    f"{modelscope_api}/models/{model_id}",
                    headers=headers
                )
                model_response.raise_for_status()
                model_data = model_response.json()
                
                # Download files (similar to HuggingFace structure)
                # This is a simplified version - full implementation would need
                # to handle ModelScope's specific API structure
                return DownloadResult(
                    success=False,
                    model_id=model_id,
                    source=ModelSource.MODELSCOPE,
                    error="ModelScope library not installed. Install with: pip install modelscope"
                )
                
        except Exception as e:
            logger.error(f"Error downloading from ModelScope: {e}", exc_info=True)
            return DownloadResult(
                success=False,
                model_id=model_id,
                source=ModelSource.MODELSCOPE,
                error=str(e)
            )
    
    def download(self,
                model_id_or_url: str,
                save_path: Optional[Path] = None,
                source: Optional[ModelSource] = None,
                progress_callback: Optional[Callable] = None,
                **kwargs) -> DownloadResult:
        """
        Unified download method that automatically detects source
        
        Args:
            model_id_or_url: Model ID or URL
            save_path: Where to save the file (optional, auto-generated if not provided)
            source: Force a specific source (optional, auto-detected if not provided)
            progress_callback: Callback for progress updates
            **kwargs: Additional parameters (token, api_key, headers, etc.)
        
        Returns:
            DownloadResult with success status and details
        """
        # Detect source if not provided
        if not source:
            source = self.detect_source(model_id_or_url)
        
        # Generate save path if not provided
        if not save_path:
            from app.services.repository_manager import get_repository_manager
            repo_mgr = get_repository_manager()
            target_dir = repo_mgr.get_best_directory_for_download(5.0)  # Default 5GB estimate
            if target_dir:
                save_path = Path(target_dir.path)
            else:
                save_path = Path.home() / '.cache' / 'models'
            save_path.mkdir(parents=True, exist_ok=True)
            
            # Generate filename from model_id_or_url
            safe_name = model_id_or_url.replace('/', '_').replace(':', '_')
            save_path = save_path / safe_name
        
        # Route to appropriate download method
        if source == ModelSource.HUGGINGFACE:
            return self.download_from_huggingface(
                model_id_or_url,
                filename=kwargs.get('filename'),
                progress_callback=progress_callback,
                token=kwargs.get('token')
            )
        elif source == ModelSource.DIRECT_URL:
            return self.download_from_direct_url(
                model_id_or_url,
                save_path,
                progress_callback,
                headers=kwargs.get('headers')
            )
        elif source == ModelSource.CIVITAI:
            return self.download_from_civitai(
                model_id_or_url,
                save_path,
                progress_callback,
                api_key=kwargs.get('api_key')
            )
        elif source == ModelSource.GITHUB_RELEASE:
            return self.download_from_github_release(
                model_id_or_url,
                save_path,
                progress_callback,
                token=kwargs.get('token')
            )
        elif source == ModelSource.MODELSCOPE:
            return self.download_from_modelscope(
                model_id_or_url,
                save_path,
                progress_callback,
                token=kwargs.get('token')
            )
        else:
            return DownloadResult(
                success=False,
                model_id=model_id_or_url,
                source=source,
                error=f"Unsupported source: {source}"
            )


# Singleton instance
_download_service = None

def get_model_download_service() -> ModelDownloadService:
    """Get singleton instance of ModelDownloadService"""
    global _download_service
    if _download_service is None:
        _download_service = ModelDownloadService()
    return _download_service
