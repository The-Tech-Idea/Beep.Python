"""
AI Service Model Manager

Manages local model storage for AI services (Text-to-Image, Speech-to-Text, etc.)
Similar to LLMManager but specialized for AI service models.
"""
import os
import json
import shutil
import hashlib
import threading
from pathlib import Path
from datetime import datetime
from dataclasses import dataclass, field, asdict
from typing import Optional, List, Dict, Any, Callable
from enum import Enum
import logging

logger = logging.getLogger(__name__)


class AIServiceModelSource(Enum):
    """Sources for downloading AI service models"""
    HUGGINGFACE = "huggingface"
    LOCAL = "local"
    CUSTOM = "custom"


@dataclass
class AIServiceLocalModel:
    """A locally stored AI service model"""
    id: str
    name: str
    model_id: str  # HuggingFace model ID (e.g., "runwayml/stable-diffusion-v1-5")
    path: str  # Local path to model files
    service_type: str  # text_to_image, speech_to_text, etc.
    size: int  # Total size in bytes
    format: str  # safetensors, pytorch, etc.
    source: str
    status: str = "available"
    downloaded_at: Optional[str] = None
    last_used: Optional[str] = None
    metadata: Dict[str, Any] = field(default_factory=dict)
    description: Optional[str] = None
    
    def to_dict(self) -> dict:
        return asdict(self)


@dataclass
class AIServiceDownloadProgress:
    """Download progress information"""
    model_id: str
    filename: str
    total_size: int
    downloaded: int
    percentage: float
    speed: float  # bytes per second
    status: str  # downloading, completed, failed, cancelled
    error: Optional[str] = None


class AIServiceModelManager:
    """Manages local model storage for AI services"""
    _instances: Dict[str, 'AIServiceModelManager'] = {}
    _lock = threading.Lock()
    
    def __new__(cls, service_type: str):
        if service_type not in cls._instances:
            with cls._lock:
                if service_type not in cls._instances:
                    instance = super().__new__(cls)
                    instance._initialized = False
                    instance._service_type = service_type
                    cls._instances[service_type] = instance
        return cls._instances[service_type]
    
    def __init__(self, service_type: str):
        if self._initialized:
            return
        
        self._initialized = True
        self.service_type = service_type
        
        # Use app's own folder
        from app.config_manager import get_app_directory
        self.base_path = get_app_directory()
        self.models_path = self.base_path / 'ai_models' / service_type
        
        # Ensure directory exists
        self.models_path.mkdir(parents=True, exist_ok=True)
        
        # Index file for tracking models
        self.index_file = self.models_path / 'index.json'
        
        # Active downloads
        self._downloads: Dict[str, AIServiceDownloadProgress] = {}
        self._download_threads: Dict[str, threading.Thread] = {}
        self._download_callbacks: Dict[str, List[Callable]] = {}
        self._cancel_flags: Dict[str, bool] = {}
        
        # Load index
        self._load_index()
    
    def _load_index(self):
        """Load model index from file"""
        if self.index_file.exists():
            try:
                with open(self.index_file, 'r') as f:
                    self._index = json.load(f)
            except Exception as e:
                logger.error(f"Error loading model index: {e}")
                self._index = {'models': []}
        else:
            self._index = {'models': []}
    
    def _save_index(self):
        """Save model index to file"""
        try:
            with open(self.index_file, 'w') as f:
                json.dump(self._index, f, indent=2)
        except Exception as e:
            logger.error(f"Error saving model index: {e}")
    
    def get_local_models(self) -> List[AIServiceLocalModel]:
        """Get all locally stored models for this service"""
        models = []
        valid_models = []
        
        for model_data in self._index.get('models', []):
            # Verify model still exists and has actual files
            model_path = Path(model_data.get('path', ''))
            
            # Check if path exists
            if not model_path.exists():
                # Model path doesn't exist - skip it (don't add to valid_models)
                logger.debug(f"Model path does not exist, skipping: {model_path}")
                continue
            
            # Verify it has actual model files (not just an empty directory)
            has_model_files = False
            if model_path.is_dir():
                # Check for common model file extensions
                model_extensions = {'.safetensors', '.bin', '.ckpt', '.pt', '.pth', '.onnx', '.pkl'}
                for ext in model_extensions:
                    if list(model_path.rglob(f'*{ext}')):
                        has_model_files = True
                        break
            elif model_path.is_file():
                # Single file model
                has_model_files = True
            
            if has_model_files:
                try:
                    model = AIServiceLocalModel(**model_data)
                    models.append(model)
                    valid_models.append(model_data)
                except Exception as e:
                    logger.warning(f"Error creating model from data: {e}")
                    continue
            else:
                logger.debug(f"Model path has no model files, skipping: {model_path}")
        
        # Update index to only include valid models
        if len(valid_models) != len(self._index.get('models', [])):
            self._index['models'] = valid_models
            self._save_index()
        
        # Scan for new models in the directory
        self._scan_for_models(models)
        
        return models
    
    def _scan_for_models(self, existing_models: List[AIServiceLocalModel]):
        """Scan models directory for unindexed models"""
        existing_paths = {m.path for m in existing_models}
        
        # Look for model directories (HuggingFace cache structure)
        # HuggingFace typically stores models in: models--{org}--{model_name}/
        for item in self.models_path.iterdir():
            if item.is_dir() and item.name.startswith('models--'):
                # This is a HuggingFace cached model
                model_id = item.name.replace('models--', '').replace('--', '/')
                
                # Check if already indexed
                if any(m.model_id == model_id for m in existing_models):
                    continue
                
                # Calculate total size
                total_size = sum(f.stat().st_size for f in item.rglob('*') if f.is_file())
                
                if total_size > 0:
                    model = AIServiceLocalModel(
                        id=hashlib.md5(model_id.encode()).hexdigest()[:12],
                        name=model_id.split('/')[-1],
                        model_id=model_id,
                        path=str(item),
                        service_type=self.service_type,
                        size=total_size,
                        format='huggingface',
                        source='huggingface',
                        status='available',
                        downloaded_at=datetime.fromtimestamp(item.stat().st_mtime).isoformat()
                    )
                    existing_models.append(model)
                    self._index['models'].append(model.to_dict())
        
        self._save_index()
    
    def get_storage_stats(self) -> Dict[str, Any]:
        """Get storage statistics"""
        models = self.get_local_models()
        total_size = sum(m.size for m in models)
        
        return {
            'model_count': len(models),
            'total_size_gb': round(total_size / (1024 ** 3), 2),
            'total_size_bytes': total_size
        }
    
    def download_model(self, 
                      model_id: str,
                      progress_callback: Optional[Callable[[AIServiceDownloadProgress], None]] = None,
                      task_manager=None,
                      task_id: str = None) -> Optional[AIServiceLocalModel]:
        """
        Download a model from HuggingFace
        
        For AI services, models are typically downloaded via huggingface_hub
        and cached automatically. We track them in our index.
        """
        from app.services.huggingface_service import HuggingFaceService
        from huggingface_hub import snapshot_download
        
        hf_service = HuggingFaceService()
        
        # Check if model already exists
        existing_models = self.get_local_models()
        for model in existing_models:
            if model.model_id == model_id:
                logger.info(f"Model {model_id} already exists locally")
                return model
        
        # Create download progress
        progress = AIServiceDownloadProgress(
            model_id=model_id,
            filename=model_id,
            total_size=0,
            downloaded=0,
            percentage=0.0,
            speed=0.0,
            status='downloading'
        )
        
        download_key = f"{self.service_type}_{model_id}"
        self._downloads[download_key] = progress
        
        if progress_callback:
            if download_key not in self._download_callbacks:
                self._download_callbacks[download_key] = []
            self._download_callbacks[download_key].append(progress_callback)
        
        # Note: huggingface_hub's snapshot_download doesn't provide granular progress
        # We'll update progress at key stages
        def update_task_progress(percent: int, message: str):
            if task_manager and task_id:
                try:
                    task_manager.update_progress(task_id, percent, message)
                except Exception as e:
                    logger.debug(f"Task progress update error: {e}")
            
            for callback in self._download_callbacks.get(download_key, []):
                try:
                    callback(progress)
                except Exception as e:
                    logger.error(f"Progress callback error: {e}")
        
        def run_download():
            try:
                progress.status = 'downloading'
                
                # Get HuggingFace token if available
                token = None
                try:
                    from app.services.repository_manager import get_repository_manager
                    repo_mgr = get_repository_manager()
                    hf_repo = repo_mgr.get_repository('hf_default')
                    if hf_repo and hf_repo.api_key:
                        token = hf_repo.api_key
                except:
                    pass
                
                # Download model using huggingface_hub
                # This will cache it in the default HuggingFace cache
                try:
                    from huggingface_hub import snapshot_download
                except ImportError:
                    error_msg = "huggingface_hub not installed. Please install it in the AI service environment."
                    logger.error(error_msg)
                    progress.status = 'failed'
                    progress.error = error_msg
                    if task_manager and task_id:
                        task_manager.fail_task(task_id, error_msg)
                    return None
                
                # Default HuggingFace cache location
                cache_dir = Path.home() / '.cache' / 'huggingface' / 'hub'
                
                logger.info(f"Downloading model {model_id} to HuggingFace cache...")
                
                update_task_progress(30, f"Downloading {model_id} from HuggingFace...")
                
                # Use snapshot_download to get the full model
                # This downloads the entire repository and caches it locally
                # The cache is persistent - models won't re-download unless cache is cleared
                try:
                    local_dir = snapshot_download(
                        repo_id=model_id,
                        token=token,
                        cache_dir=str(cache_dir),
                        resume_download=True,  # Resume if download was interrupted
                        ignore_patterns=["*.md", "*.txt", "*.json"]  # Skip documentation files to speed up download
                    )
                except Exception as download_error:
                    # Re-raise to be caught by outer exception handler for better error messages
                    error_str = str(download_error)
                    # Check if it's a gated model error
                    if "403" in error_str or "gated" in error_str.lower() or "restricted" in error_str.lower():
                        if not token:
                            raise Exception(
                                f"Gated model requires HuggingFace API token. "
                                f"Please configure your token in Settings. "
                                f"Visit https://huggingface.co/{model_id} to request access."
                            )
                        else:
                            raise Exception(
                                f"Access denied to gated model. "
                                f"You may need to accept the license agreement on HuggingFace. "
                                f"Visit https://huggingface.co/{model_id} to request access. "
                                f"Original error: {error_str}"
                            )
                    raise
                
                update_task_progress(80, f"Processing downloaded model...")
                
                # Calculate size
                total_size = sum(f.stat().st_size for f in Path(local_dir).rglob('*') if f.is_file())
                
                # Reference the HuggingFace cache location in our index
                # We don't copy or symlink - just track where it is
                model = AIServiceLocalModel(
                    id=hashlib.md5(model_id.encode()).hexdigest()[:12],
                    name=model_id.split('/')[-1],
                    model_id=model_id,
                    path=str(local_dir),  # Reference to HuggingFace cache
                    service_type=self.service_type,
                    size=total_size,
                    format='huggingface',
                    source='huggingface',
                    status='available',
                    downloaded_at=datetime.now().isoformat()
                )
                
                # Check if already in index
                existing_ids = {m.get('id') for m in self._index.get('models', [])}
                if model.id not in existing_ids:
                    self._index['models'].append(model.to_dict())
                    self._save_index()
                
                progress.status = 'completed'
                progress.percentage = 100.0
                
                update_task_progress(100, f"Model {model_id} ready")
                if task_manager and task_id:
                    task_manager.complete_task(task_id, model.to_dict())
                
                logger.info(f"Model {model_id} downloaded successfully to {local_dir}")
                return model
                
            except Exception as e:
                import traceback
                error_msg = str(e)
                error_details = traceback.format_exc()
                logger.error(f"Error downloading model {model_id}: {error_details}")
                
                # Check for gated model errors (403)
                if "403" in error_msg or "gated" in error_msg.lower() or "restricted" in error_msg.lower() or "authorized" in error_msg.lower():
                    # Provide helpful message for gated models
                    improved_error = (
                        f"This model is gated and requires:\n"
                        f"1. Accepting the license agreement on HuggingFace\n"
                        f"2. A valid HuggingFace API token configured in settings\n"
                        f"3. Access authorization from the model owner\n\n"
                        f"Visit https://huggingface.co/{model_id} to request access.\n\n"
                        f"Original error: {error_msg}"
                    )
                    error_msg = improved_error
                elif "401" in error_msg or "unauthorized" in error_msg.lower():
                    # Token issue
                    improved_error = (
                        f"Authentication failed. Please check your HuggingFace API token in settings.\n\n"
                        f"Original error: {error_msg}"
                    )
                    error_msg = improved_error
                
                progress.status = 'failed'
                progress.error = error_msg
                
                if task_manager and task_id:
                    task_manager.fail_task(task_id, error_msg)
                
                return None
        
        # Run download in thread
        thread = threading.Thread(target=run_download, daemon=True)
        thread.start()
        self._download_threads[download_key] = thread
        
        # Return immediately, download happens in background
        return None
    
    def get_active_downloads(self) -> List[AIServiceDownloadProgress]:
        """Get list of active downloads"""
        return [p for p in self._downloads.values() if p.status == 'downloading']
    
    def cancel_download(self, model_id: str) -> bool:
        """Cancel an active download"""
        download_key = f"{self.service_type}_{model_id}"
        if download_key in self._downloads:
            self._cancel_flags[download_key] = True
            self._downloads[download_key].status = 'cancelled'
            return True
        return False
    
    def delete_model(self, model_id: str) -> bool:
        """Delete a local model"""
        models = self.get_local_models()
        model_to_delete = next((m for m in models if m.id == model_id), None)
        
        if not model_to_delete:
            return False
        
        try:
            model_path = Path(model_to_delete.path)
            
            # If it's a symlink, just remove the symlink
            if model_path.is_symlink():
                model_path.unlink()
            # If it's a directory in our models path, remove it
            elif model_path.parent == self.models_path:
                shutil.rmtree(model_path)
            # Otherwise, it's in HuggingFace cache - just remove from index
            # (don't delete from cache as other apps might use it)
            
            # Remove from index
            self._index['models'] = [m for m in self._index['models'] if m.get('id') != model_id]
            self._save_index()
            
            logger.info(f"Deleted model {model_to_delete.model_id}")
            return True
            
        except Exception as e:
            logger.error(f"Error deleting model {model_id}: {e}")
            return False
    
    def get_model_by_id(self, model_id: str) -> Optional[AIServiceLocalModel]:
        """Get a model by its HuggingFace model ID"""
        models = self.get_local_models()
        return next((m for m in models if m.model_id == model_id), None)
    
    def get_model_path(self, model_id: str) -> Optional[Path]:
        """Get local path to a model by HuggingFace model ID"""
        model = self.get_model_by_id(model_id)
        if model:
            path = Path(model.path)
            # Check if path exists (could be HuggingFace cache or our directory)
            if path.exists():
                return path
            # Also check HuggingFace default cache location
            cache_path = Path.home() / '.cache' / 'huggingface' / 'hub' / f"models--{model_id.replace('/', '--')}"
            if cache_path.exists():
                return cache_path
        return None


def get_ai_service_model_manager(service_type: str) -> AIServiceModelManager:
    """Get model manager instance for a service type"""
    return AIServiceModelManager(service_type)
