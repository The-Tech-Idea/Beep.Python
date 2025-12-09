"""
HuggingFace Integration Service

Provides integration with HuggingFace Hub for:
- Model search and discovery
- Model metadata retrieval
- Model file downloading with authentication
- GGUF file detection and quantization info
"""
import os
import re
import time
import json
import hashlib
import threading
import requests
from pathlib import Path
from datetime import datetime
from typing import Optional, List, Dict, Any, Callable, Generator
from dataclasses import dataclass, field

from .llm_manager import (
    LLMManager, ModelInfo, ModelFile, LocalModel, 
    DownloadProgress, ModelSource, ModelFormat
)


# HuggingFace API endpoints
HF_API_BASE = "https://huggingface.co/api"
HF_MODEL_API = f"{HF_API_BASE}/models"
HF_DOWNLOAD_BASE = "https://huggingface.co"


@dataclass
class HFSearchResult:
    """HuggingFace search result"""
    id: str
    author: str
    model_id: str
    downloads: int
    likes: int
    tags: List[str]
    pipeline_tag: Optional[str]
    last_modified: str
    private: bool = False
    gated: bool = False
    
    def to_dict(self) -> dict:
        return {
            'id': self.id,
            'author': self.author,
            'model_id': self.model_id,
            'downloads': self.downloads,
            'likes': self.likes,
            'tags': self.tags,
            'pipeline_tag': self.pipeline_tag,
            'last_modified': self.last_modified,
            'private': self.private,
            'gated': self.gated
        }


class HuggingFaceService:
    """Service for HuggingFace Hub integration"""
    
    # Common GGUF quantization patterns
    QUANT_PATTERNS = [
        (r'[._-]Q2[._-]K[._-]?S?', 'Q2_K_S'),
        (r'[._-]Q2[._-]K[._-]?M?', 'Q2_K_M'),
        (r'[._-]Q3[._-]K[._-]S', 'Q3_K_S'),
        (r'[._-]Q3[._-]K[._-]M', 'Q3_K_M'),
        (r'[._-]Q3[._-]K[._-]L', 'Q3_K_L'),
        (r'[._-]Q4[._-]0', 'Q4_0'),
        (r'[._-]Q4[._-]1', 'Q4_1'),
        (r'[._-]Q4[._-]K[._-]S', 'Q4_K_S'),
        (r'[._-]Q4[._-]K[._-]M', 'Q4_K_M'),
        (r'[._-]Q5[._-]0', 'Q5_0'),
        (r'[._-]Q5[._-]1', 'Q5_1'),
        (r'[._-]Q5[._-]K[._-]S', 'Q5_K_S'),
        (r'[._-]Q5[._-]K[._-]M', 'Q5_K_M'),
        (r'[._-]Q6[._-]K', 'Q6_K'),
        (r'[._-]Q8[._-]0', 'Q8_0'),
        (r'[._-]F16', 'F16'),
        (r'[._-]F32', 'F32'),
        (r'[._-]IQ1', 'IQ1'),
        (r'[._-]IQ2', 'IQ2'),
        (r'[._-]IQ3', 'IQ3'),
        (r'[._-]IQ4', 'IQ4'),
    ]
    
    # Model size patterns
    SIZE_PATTERNS = [
        (r'(\d+)[._-]?[Bb](?:[._-]|$)', lambda m: m.group(1) + 'B'),
        (r'[._-](\d+)b[._-]', lambda m: m.group(1) + 'B'),
    ]
    
    def __init__(self):
        self.llm_manager = LLMManager()
        self.session = requests.Session()
        self.session.headers.update({
            'User-Agent': 'Beep.Python.Host.Admin/1.0'
        })
    
    def _get_auth_headers(self) -> dict:
        """Get authorization headers if token is available"""
        # Check repository manager for API keys first
        try:
            from app.services.repository_manager import get_repository_manager
            repo_mgr = get_repository_manager()
            
            # Get HuggingFace repository API key
            hf_repo = repo_mgr.get_repository('hf_default')
            if hf_repo and hf_repo.api_key:
                return {'Authorization': f'Bearer {hf_repo.api_key}'}
        except:
            pass
        
        # Fallback to LLM manager token (legacy)
        token = self.llm_manager.get_huggingface_token()
        if token:
            return {'Authorization': f'Bearer {token}'}
        return {}
    
    def _extract_quantization(self, filename: str) -> Optional[str]:
        """Extract quantization level from filename"""
        for pattern, quant in self.QUANT_PATTERNS:
            if re.search(pattern, filename, re.IGNORECASE):
                return quant
        return None
    
    def _extract_model_size(self, text: str) -> Optional[str]:
        """Extract model size (e.g., 7B, 13B) from text"""
        for pattern, extractor in self.SIZE_PATTERNS:
            match = re.search(pattern, text, re.IGNORECASE)
            if match:
                return extractor(match)
        return None
    
    def search_models(self, 
                      query: str, 
                      filter_gguf: bool = True,
                      limit: int = 20,
                      sort: str = "downloads") -> List[Dict[str, Any]]:
        """
        Search for models on HuggingFace
        
        Args:
            query: Search query
            filter_gguf: Only show models with GGUF files
            limit: Maximum results
            sort: Sort by (downloads, likes, modified)
        """
        params = {
            'search': query,
            'limit': limit,
            'sort': sort,
            'direction': '-1',
            'full': 'true'
        }
        
        if filter_gguf:
            # Search specifically in GGUF-related models
            params['filter'] = 'gguf'
        
        try:
            response = self.session.get(
                HF_MODEL_API,
                params=params,
                headers=self._get_auth_headers(),
                timeout=30
            )
            response.raise_for_status()
            models = response.json()
            
            results = []
            for model in models:
                result = {
                    'id': model.get('id', ''),
                    'modelId': model.get('modelId', model.get('id', '')),
                    'author': model.get('author', model.get('id', '').split('/')[0] if '/' in model.get('id', '') else ''),
                    'downloads': model.get('downloads', 0),
                    'likes': model.get('likes', 0),
                    'tags': model.get('tags', []),
                    'pipeline_tag': model.get('pipeline_tag'),
                    'lastModified': model.get('lastModified', ''),
                    'private': model.get('private', False),
                    'gated': model.get('gated', False),
                    'description': model.get('description', '')[:200] if model.get('description') else '',
                    'source': 'huggingface'
                }
                
                # Try to extract model size from tags or model ID
                for tag in model.get('tags', []):
                    size = self._extract_model_size(tag)
                    if size:
                        result['parameters'] = size
                        break
                
                if not result.get('parameters'):
                    result['parameters'] = self._extract_model_size(model.get('id', ''))
                
                results.append(result)
            
            return results
            
        except requests.RequestException as e:
            print(f"Error searching HuggingFace: {e}")
            return []
    
    def get_model_details(self, model_id: str) -> Optional[Dict[str, Any]]:
        """
        Get detailed information about a specific model
        
        Args:
            model_id: Full model ID (e.g., "TheBloke/Llama-2-7B-GGUF")
        """
        try:
            response = self.session.get(
                f"{HF_MODEL_API}/{model_id}",
                headers=self._get_auth_headers(),
                timeout=30
            )
            response.raise_for_status()
            model = response.json()
            
            # Get README/description
            card_data = model.get('cardData', {})
            
            details = {
                'id': model.get('id', model_id),
                'modelId': model.get('modelId', model_id),
                'author': model.get('author', ''),
                'sha': model.get('sha', ''),
                'downloads': model.get('downloads', 0),
                'likes': model.get('likes', 0),
                'tags': model.get('tags', []),
                'pipeline_tag': model.get('pipeline_tag'),
                'library_name': model.get('library_name'),
                'lastModified': model.get('lastModified', ''),
                'private': model.get('private', False),
                'gated': model.get('gated', False),
                'disabled': model.get('disabled', False),
                'description': card_data.get('description', ''),
                'license': card_data.get('license', model.get('license', '')),
                'language': card_data.get('language', []),
                'base_model': card_data.get('base_model', ''),
                'model_type': card_data.get('model_type', ''),
                'source': 'huggingface',
                'url': f"https://huggingface.co/{model_id}",
                'files': []
            }
            
            # Extract parameters
            details['parameters'] = self._extract_model_size(model_id)
            for tag in model.get('tags', []):
                if not details['parameters']:
                    details['parameters'] = self._extract_model_size(tag)
            
            return details
            
        except requests.RequestException as e:
            print(f"Error getting model details: {e}")
            return None
    
    def get_model_files(self, model_id: str, filter_gguf: bool = True) -> List[Dict[str, Any]]:
        """
        Get list of files in a model repository
        
        Args:
            model_id: Full model ID
            filter_gguf: Only return GGUF files
        """
        try:
            response = self.session.get(
                f"{HF_API_BASE}/models/{model_id}/tree/main",
                headers=self._get_auth_headers(),
                timeout=30
            )
            response.raise_for_status()
            tree = response.json()
            
            files = []
            for item in tree:
                if item.get('type') != 'file':
                    continue
                
                filename = item.get('path', '')
                
                # Filter by extension
                if filter_gguf and not filename.lower().endswith('.gguf'):
                    continue
                
                # Determine format
                ext = Path(filename).suffix.lower()
                format_map = {
                    '.gguf': 'gguf',
                    '.ggml': 'ggml',
                    '.safetensors': 'safetensors',
                    '.bin': 'pytorch',
                    '.onnx': 'onnx'
                }
                file_format = format_map.get(ext, 'other')
                
                file_info = {
                    'filename': filename,
                    'size': item.get('size', 0),
                    'format': file_format,
                    'lfs': item.get('lfs') is not None,
                    'sha256': item.get('lfs', {}).get('sha256') if item.get('lfs') else None,
                    'download_url': f"{HF_DOWNLOAD_BASE}/{model_id}/resolve/main/{filename}",
                    'quantization': self._extract_quantization(filename)
                }
                
                # Estimate quality based on quantization
                quant = file_info['quantization']
                if quant:
                    if quant.startswith('Q8') or quant.startswith('F'):
                        file_info['quality'] = 'high'
                    elif quant.startswith('Q5') or quant.startswith('Q6'):
                        file_info['quality'] = 'medium-high'
                    elif quant.startswith('Q4'):
                        file_info['quality'] = 'medium'
                    elif quant.startswith('Q3'):
                        file_info['quality'] = 'medium-low'
                    else:
                        file_info['quality'] = 'low'
                
                files.append(file_info)
            
            # Sort by quality (higher quant = better quality but larger)
            quant_order = {'Q8_0': 10, 'F16': 11, 'F32': 12, 'Q6_K': 9, 
                          'Q5_K_M': 8, 'Q5_K_S': 7, 'Q5_1': 7, 'Q5_0': 6,
                          'Q4_K_M': 5, 'Q4_K_S': 4, 'Q4_1': 4, 'Q4_0': 3,
                          'Q3_K_L': 2, 'Q3_K_M': 2, 'Q3_K_S': 1,
                          'Q2_K_M': 0, 'Q2_K_S': 0}
            
            files.sort(key=lambda x: quant_order.get(x.get('quantization', ''), -1), reverse=True)
            
            return files
            
        except requests.RequestException as e:
            print(f"Error getting model files: {e}")
            return []
    
    def download_model(self, 
                       model_id: str, 
                       filename: str,
                       progress_callback: Optional[Callable[[DownloadProgress], None]] = None,
                       task_manager=None,
                       task_id: str = None) -> Optional[LocalModel]:
        """
        Download a model file from HuggingFace
        
        Args:
            model_id: Full model ID (e.g., "TheBloke/Llama-2-7B-GGUF")
            filename: Specific file to download
            progress_callback: Callback for progress updates
            task_manager: Optional TaskManager for progress tracking
            task_id: Optional task ID for progress tracking
        """
        # First, check if model requires authentication
        model_details = self.get_model_details(model_id)
        if not model_details:
            raise ValueError(f"Model {model_id} not found or inaccessible")
        
        is_gated = model_details.get('gated', False)
        is_private = model_details.get('private', False)
        requires_token = is_gated or is_private
        
        # Check if token is available
        has_token = bool(self._get_auth_headers().get('Authorization'))
        
        if requires_token and not has_token:
            error_msg = (
                f"This model requires a HuggingFace API token to download. "
                f"{'It is a gated model (requires license acceptance).' if is_gated else ''} "
                f"{'It is a private model.' if is_private else ''} "
                f"Please configure your API token in Settings > Repositories > HuggingFace Hub."
            )
            
            # Update progress if callback available
            if progress_callback:
                download_id = hashlib.md5(f"{model_id}/{filename}".encode()).hexdigest()[:12]
                progress = DownloadProgress(
                    model_id=download_id,
                    filename=filename,
                    total_size=0,
                    downloaded=0,
                    speed=0,
                    eta=0,
                    status='error',
                    error=error_msg
                )
                progress_callback(progress)
            
            # Update task if available
            if task_manager and task_id:
                task_manager.update_progress(task_id, 0, f"Error: {error_msg}")
                task_manager.complete_task(task_id, success=False, error=error_msg)
            
            raise ValueError(error_msg)
        
        # Construct download URL
        download_url = f"{HF_DOWNLOAD_BASE}/{model_id}/resolve/main/{filename}"
        
        # Get file info first to determine size
        files = self.get_model_files(model_id)
        file_info = next((f for f in files if f['filename'] == filename), None)
        
        # Determine save path using repository manager
        from app.services.repository_manager import get_repository_manager
        repo_mgr = get_repository_manager()
        
        safe_model_id = model_id.replace('/', '_')
        
        # Get file size estimate for directory selection
        file_size_gb = file_info.get('size', 0) / (1024 ** 3) if file_info else 5.0  # Default 5GB estimate
        
        # Get best directory for download
        target_dir = repo_mgr.get_best_directory_for_download(file_size_gb)
        if not target_dir:
            # Fallback to default directory
            target_dir = repo_mgr.get_directory('default')
            if not target_dir:
                # Legacy fallback
                save_dir = self.llm_manager.models_path / safe_model_id
            else:
                save_dir = Path(target_dir.path) / safe_model_id
        else:
            save_dir = Path(target_dir.path) / safe_model_id
        
        save_dir.mkdir(parents=True, exist_ok=True)
        save_path = save_dir / filename
        
        total_size = file_info.get('size', 0) if file_info else 0
        expected_sha256 = file_info.get('sha256') if file_info else None
        quantization = file_info.get('quantization') if file_info else self._extract_quantization(filename)
        
        # Setup progress tracking
        download_id = hashlib.md5(f"{model_id}/{filename}".encode()).hexdigest()[:12]
        progress = DownloadProgress(
            model_id=download_id,
            filename=filename,
            total_size=total_size,
            downloaded=0,
            speed=0,
            eta=0,
            status='downloading'
        )
        
        self.llm_manager._downloads[download_id] = progress
        
        # Check for partial download (resume support)
        downloaded_bytes = 0
        if save_path.exists():
            downloaded_bytes = save_path.stat().st_size
            if downloaded_bytes >= total_size and total_size > 0:
                # Already complete
                progress.downloaded = total_size
                progress.status = 'completed'
                return self._create_local_model(model_id, filename, save_path, 
                                                quantization, total_size)
        
        try:
            headers = self._get_auth_headers()
            if downloaded_bytes > 0:
                headers['Range'] = f'bytes={downloaded_bytes}-'
            
            response = self.session.get(
                download_url,
                headers=headers,
                stream=True,
                timeout=30
            )
            
            # Handle authentication errors specifically
            if response.status_code == 401:
                error_msg = (
                    "Authentication failed. Your HuggingFace API token may be invalid or expired. "
                    "Please check your token in Settings > Repositories > HuggingFace Hub."
                )
                progress.status = 'error'
                progress.error = error_msg
                if progress_callback:
                    progress_callback(progress)
                if task_manager and task_id:
                    task_manager.complete_task(task_id, success=False, error=error_msg)
                raise ValueError(error_msg)
            
            if response.status_code == 403:
                error_msg = (
                    "Access forbidden. This model may be gated (requires license acceptance) or private. "
                    "Please ensure you have: "
                    "1. Accepted the model's license agreement on HuggingFace.co, "
                    "2. Configured a valid API token with appropriate permissions."
                )
                progress.status = 'error'
                progress.error = error_msg
                if progress_callback:
                    progress_callback(progress)
                if task_manager and task_id:
                    task_manager.complete_task(task_id, success=False, error=error_msg)
                raise ValueError(error_msg)
            
            response.raise_for_status()
            
            # Update total size from Content-Length if not known
            if total_size == 0:
                content_length = response.headers.get('Content-Length')
                if content_length:
                    total_size = int(content_length) + downloaded_bytes
                    progress.total_size = total_size
            
            # Open file for writing (append if resuming)
            mode = 'ab' if downloaded_bytes > 0 else 'wb'
            chunk_size = self.llm_manager.get_settings().get('download_chunk_size', 8192)
            
            start_time = time.time()
            last_update_time = start_time
            bytes_since_last_update = 0
            
            with open(save_path, mode) as f:
                for chunk in response.iter_content(chunk_size=chunk_size):
                    # Check for cancellation
                    if self.llm_manager._cancel_flags.get(download_id, False):
                        progress.status = 'cancelled'
                        if progress_callback:
                            progress_callback(progress)
                        return None
                    
                    if chunk:
                        f.write(chunk)
                        downloaded_bytes += len(chunk)
                        bytes_since_last_update += len(chunk)
                        
                        # Update progress periodically
                        current_time = time.time()
                        if current_time - last_update_time >= 0.5:  # Update every 0.5s
                            elapsed = current_time - last_update_time
                            speed = bytes_since_last_update / elapsed if elapsed > 0 else 0
                            
                            remaining = total_size - downloaded_bytes
                            eta = int(remaining / speed) if speed > 0 else 0
                            
                            progress.downloaded = downloaded_bytes
                            progress.speed = speed
                            progress.eta = eta
                            
                            if progress_callback:
                                progress_callback(progress)
                            
                            if task_manager and task_id:
                                percent = int((downloaded_bytes / total_size) * 100) if total_size > 0 else 0
                                speed_mb = speed / (1024 * 1024)
                                task_manager.update_progress(
                                    task_id, percent,
                                    f"Downloading: {percent}% ({speed_mb:.1f} MB/s)"
                                )
                            
                            last_update_time = current_time
                            bytes_since_last_update = 0
            
            # Verify download
            if expected_sha256:
                actual_sha256 = self._calculate_sha256(save_path)
                if actual_sha256 != expected_sha256:
                    progress.status = 'corrupted'
                    progress.error = 'SHA256 mismatch'
                    if progress_callback:
                        progress_callback(progress)
                    return None
            
            progress.status = 'completed'
            progress.downloaded = total_size
            if progress_callback:
                progress_callback(progress)
            
            # Create local model entry
            return self._create_local_model(model_id, filename, save_path, 
                                            quantization, total_size)
            
        except requests.RequestException as e:
            progress.status = 'failed'
            progress.error = str(e)
            if progress_callback:
                progress_callback(progress)
            return None
        finally:
            # Cleanup
            if download_id in self.llm_manager._cancel_flags:
                del self.llm_manager._cancel_flags[download_id]
    
    def _create_local_model(self, model_id: str, filename: str, 
                            save_path: Path, quantization: Optional[str],
                            size: int) -> LocalModel:
        """Create and register a local model entry"""
        local_model = LocalModel(
            id=hashlib.md5(f"{model_id}/{filename}".encode()).hexdigest()[:12],
            name=filename.replace('.gguf', ''),
            path=str(save_path),
            filename=filename,
            size=size,
            format='gguf' if filename.endswith('.gguf') else 'other',
            source='huggingface',
            quantization=quantization,
            parameters=self._extract_model_size(model_id) or self._extract_model_size(filename),
            status='available',
            downloaded_at=datetime.now().isoformat(),
            metadata={
                'model_id': model_id,
                'source_url': f"https://huggingface.co/{model_id}"
            }
        )
        
        # Add to index
        self.llm_manager.add_local_model(local_model)
        
        return local_model
    
    def _calculate_sha256(self, file_path: Path) -> str:
        """Calculate SHA256 hash of a file"""
        sha256 = hashlib.sha256()
        with open(file_path, 'rb') as f:
            for chunk in iter(lambda: f.read(8192), b''):
                sha256.update(chunk)
        return sha256.hexdigest()
    
    def get_trending_models(self, limit: int = 10) -> List[Dict[str, Any]]:
        """Get trending GGUF models"""
        return self.search_models("", filter_gguf=True, limit=limit, sort="downloads")
    
    def get_popular_categories(self) -> List[Dict[str, Any]]:
        """Get popular model categories for browsing"""
        return [
            {
                'id': 'chat',
                'name': 'Chat & Conversation',
                'query': 'chat gguf',
                'icon': 'bi-chat-dots',
                'description': 'Models optimized for conversational AI'
            },
            {
                'id': 'coding',
                'name': 'Code Generation',
                'query': 'code gguf',
                'icon': 'bi-code-square',
                'description': 'Models for code completion and generation'
            },
            {
                'id': 'instruct',
                'name': 'Instruction Following',
                'query': 'instruct gguf',
                'icon': 'bi-list-task',
                'description': 'Models that follow instructions accurately'
            },
            {
                'id': 'llama',
                'name': 'Llama Models',
                'query': 'llama gguf',
                'icon': 'bi-stars',
                'description': 'Meta Llama family of models'
            },
            {
                'id': 'mistral',
                'name': 'Mistral Models',
                'query': 'mistral gguf',
                'icon': 'bi-wind',
                'description': 'Mistral AI models'
            },
            {
                'id': 'phi',
                'name': 'Microsoft Phi',
                'query': 'phi gguf',
                'icon': 'bi-microsoft',
                'description': 'Microsoft Phi small language models'
            },
            {
                'id': 'qwen',
                'name': 'Qwen Models',
                'query': 'qwen gguf',
                'icon': 'bi-globe-asia-australia',
                'description': 'Alibaba Qwen models'
            },
            {
                'id': 'gemma',
                'name': 'Google Gemma',
                'query': 'gemma gguf',
                'icon': 'bi-google',
                'description': 'Google Gemma models'
            }
        ]
