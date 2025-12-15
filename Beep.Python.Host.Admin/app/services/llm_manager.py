"""
LLM Manager Service - Core service for managing Large Language Models

This service provides:
- Model discovery from HuggingFace, Ollama, and other sources
- Model downloading with progress tracking
- Local model storage management
- Model metadata and configuration
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


class ModelSource(Enum):
    """Sources for downloading models"""
    HUGGINGFACE = "huggingface"
    OLLAMA = "ollama"
    LOCAL = "local"
    CUSTOM = "custom"


class ModelFormat(Enum):
    """Supported model formats"""
    GGUF = "gguf"
    GGML = "ggml"
    SAFETENSORS = "safetensors"
    PYTORCH = "pytorch"
    ONNX = "onnx"
    OTHER = "other"


# Model Knowledge Base - descriptions and use cases for known model families
MODEL_KNOWLEDGE_BASE = {
    'qwen': {
        'family': 'Qwen',
        'description': 'Qwen (通义千问) is a series of large language models developed by Alibaba Cloud. Known for excellent multilingual capabilities, especially Chinese and English, with strong coding and reasoning abilities.',
        'use_cases': ['Multilingual chat', 'Code generation', 'Text analysis', 'Translation', 'Creative writing', 'Question answering'],
        'license': 'Apache 2.0 / Qwen License'
    },
    'llama': {
        'family': 'LLaMA',
        'description': 'LLaMA (Large Language Model Meta AI) is Meta\'s open-source foundation model. Highly versatile with excellent performance across diverse tasks. The go-to choice for many applications.',
        'use_cases': ['General chat', 'Content creation', 'Summarization', 'Code assistance', 'Research', 'Document analysis'],
        'license': 'Llama 2/3 Community License'
    },
    'mistral': {
        'family': 'Mistral',
        'description': 'Mistral AI models are known for exceptional efficiency and performance. Mistral 7B rivals larger models while being faster and more resource-efficient.',
        'use_cases': ['Fast inference', 'Chatbots', 'Code completion', 'Text generation', 'Edge deployment'],
        'license': 'Apache 2.0'
    },
    'mixtral': {
        'family': 'Mixtral (MoE)',
        'description': 'Mixtral uses Mixture of Experts (MoE) architecture, activating only relevant experts per token. Provides near-GPT-4 quality with faster inference.',
        'use_cases': ['Complex reasoning', 'Multi-task applications', 'High-quality generation', 'Professional writing'],
        'license': 'Apache 2.0'
    },
    'phi': {
        'family': 'Phi',
        'description': 'Microsoft\'s Phi models are small language models (SLMs) that punch above their weight. Designed for efficiency without sacrificing capability.',
        'use_cases': ['Edge/mobile deployment', 'Resource-constrained environments', 'Quick responses', 'Educational applications'],
        'license': 'MIT'
    },
    'gemma': {
        'family': 'Gemma',
        'description': 'Google\'s Gemma is a lightweight, open model built from Gemini research. Offers strong performance for its size with safety-focused training.',
        'use_cases': ['Safe AI applications', 'Educational tools', 'Content moderation', 'General assistance'],
        'license': 'Gemma Terms of Use'
    },
    'codellama': {
        'family': 'Code Llama',
        'description': 'Meta\'s Code Llama is specialized for code generation and understanding. Trained on code-specific data with support for many programming languages.',
        'use_cases': ['Code generation', 'Code completion', 'Debugging assistance', 'Code explanation', 'Refactoring'],
        'license': 'Llama 2 Community License'
    },
    'deepseek': {
        'family': 'DeepSeek',
        'description': 'DeepSeek models excel at coding and mathematical reasoning. DeepSeek Coder is particularly strong for programming tasks.',
        'use_cases': ['Code generation', 'Math problems', 'Technical writing', 'Algorithm design', 'Data analysis'],
        'license': 'DeepSeek License'
    },
    'starcoder': {
        'family': 'StarCoder',
        'description': 'BigCode\'s StarCoder is trained on permissively licensed code from GitHub. Excellent for code completion and generation with legal clarity.',
        'use_cases': ['Code completion', 'IDE integration', 'Code generation', 'Documentation', 'Enterprise use'],
        'license': 'BigCode OpenRAIL-M'
    },
    'vicuna': {
        'family': 'Vicuna',
        'description': 'Vicuna is fine-tuned from LLaMA on user-shared conversations. Known for good instruction-following and conversational abilities.',
        'use_cases': ['Conversational AI', 'Customer support', 'Interactive assistants', 'Role-playing'],
        'license': 'Non-commercial (LLaMA based)'
    },
    'wizard': {
        'family': 'WizardLM',
        'description': 'WizardLM uses Evol-Instruct for enhanced instruction following. Strong at complex instructions and multi-step reasoning.',
        'use_cases': ['Complex instructions', 'Step-by-step tasks', 'Detailed explanations', 'Educational content'],
        'license': 'Non-commercial'
    },
    'yi': {
        'family': 'Yi',
        'description': '01.AI\'s Yi models offer bilingual (Chinese/English) capabilities with strong reasoning. Competitive with larger models at smaller sizes.',
        'use_cases': ['Bilingual applications', 'Long context tasks', 'Research', 'Analysis'],
        'license': 'Yi License'
    },
    'falcon': {
        'family': 'Falcon',
        'description': 'TII\'s Falcon was trained on high-quality web data. One of the first truly open large language models with permissive licensing.',
        'use_cases': ['General chat', 'Content generation', 'Research', 'Commercial applications'],
        'license': 'Apache 2.0'
    },
    'neural': {
        'family': 'NeuralChat/NeuralHermes',
        'description': 'Intel-optimized models fine-tuned for conversational AI. Designed to run efficiently on Intel hardware.',
        'use_cases': ['Intel-optimized inference', 'Chatbots', 'Customer service', 'Enterprise deployment'],
        'license': 'Apache 2.0'
    },
    'openchat': {
        'family': 'OpenChat',
        'description': 'OpenChat models are fine-tuned with C-RLFT for improved chat capabilities. Known for high quality responses in conversations.',
        'use_cases': ['Chat applications', 'Virtual assistants', 'Customer support', 'Interactive systems'],
        'license': 'Apache 2.0'
    },
    'orca': {
        'family': 'Orca',
        'description': 'Microsoft\'s Orca learns from complex explanation traces. Designed to mimic the reasoning process of larger models.',
        'use_cases': ['Reasoning tasks', 'Explanation generation', 'Educational tools', 'Step-by-step problem solving'],
        'license': 'Research only'
    },
    'tinyllama': {
        'family': 'TinyLlama',
        'description': 'A compact 1.1B model trained on 3 trillion tokens. Surprisingly capable for its small size, ideal for edge deployment.',
        'use_cases': ['Mobile apps', 'IoT devices', 'Quick inference', 'Resource-limited environments'],
        'license': 'Apache 2.0'
    },
    'stable': {
        'family': 'Stable LM',
        'description': 'Stability AI\'s language models balance capability with efficiency. Part of the Stable AI ecosystem.',
        'use_cases': ['Text generation', 'Creative writing', 'General assistance', 'Integration with Stable Diffusion'],
        'license': 'CC BY-SA 4.0'
    },
    'default': {
        'family': 'Unknown',
        'description': 'A quantized large language model in GGUF format, optimized for local inference with llama.cpp.',
        'use_cases': ['Text generation', 'Chat', 'Question answering', 'Content creation'],
        'license': 'Check model source'
    }
}


def get_model_knowledge(model_name: str) -> Dict[str, Any]:
    """Get knowledge base info for a model based on its name"""
    name_lower = model_name.lower()
    
    for key, info in MODEL_KNOWLEDGE_BASE.items():
        if key in name_lower:
            return info
    
    return MODEL_KNOWLEDGE_BASE['default']


class ModelStatus(Enum):
    """Model status"""
    AVAILABLE = "available"      # Ready to use
    DOWNLOADING = "downloading"  # Currently downloading
    PAUSED = "paused"           # Download paused
    CORRUPTED = "corrupted"     # File corrupted
    INCOMPLETE = "incomplete"   # Partial download


@dataclass
class ModelFile:
    """Represents a downloadable model file"""
    filename: str
    size: int  # Size in bytes
    url: str
    sha256: Optional[str] = None
    quantization: Optional[str] = None  # e.g., Q4_K_M, Q5_K_S
    
    def to_dict(self) -> dict:
        return asdict(self)


@dataclass
class ModelInfo:
    """Complete model information"""
    id: str
    name: str
    source: str  # huggingface, ollama, local
    author: Optional[str] = None
    description: Optional[str] = None
    license: Optional[str] = None
    tags: List[str] = field(default_factory=list)
    downloads: int = 0
    likes: int = 0
    model_type: Optional[str] = None  # llama, mistral, phi, etc.
    base_model: Optional[str] = None
    context_length: Optional[int] = None
    parameters: Optional[str] = None  # e.g., "7B", "13B", "70B"
    files: List[ModelFile] = field(default_factory=list)
    created_at: Optional[str] = None
    updated_at: Optional[str] = None
    homepage_url: Optional[str] = None
    
    def to_dict(self) -> dict:
        d = asdict(self)
        d['files'] = [f.to_dict() if isinstance(f, ModelFile) else f for f in self.files]
        return d


@dataclass
class LocalModel:
    """A locally stored model"""
    id: str
    name: str
    path: str
    filename: str
    size: int
    format: str
    source: str
    quantization: Optional[str] = None
    parameters: Optional[str] = None
    context_length: Optional[int] = None
    status: str = "available"
    downloaded_at: Optional[str] = None
    last_used: Optional[str] = None
    metadata: Dict[str, Any] = field(default_factory=dict)
    # Virtual environment fields (NEW)
    venv_name: Optional[str] = None  # Name of associated virtual environment
    gpu_backend: Optional[str] = None  # GPU backend (cuda, metal, vulkan, cpu)
    # Description and use cases
    description: Optional[str] = None
    use_cases: List[str] = field(default_factory=list)
    model_family: Optional[str] = None
    license: Optional[str] = None
    
    def to_dict(self) -> dict:
        return asdict(self)


@dataclass
class DownloadProgress:
    """Download progress information"""
    model_id: str
    filename: str
    total_size: int
    downloaded: int
    speed: float  # bytes per second
    eta: int  # seconds remaining
    status: str  # downloading, paused, completed, failed
    error: Optional[str] = None
    
    @property
    def percent(self) -> float:
        if self.total_size == 0:
            return 0
        return (self.downloaded / self.total_size) * 100
    
    def to_dict(self) -> dict:
        d = asdict(self)
        d['percent'] = self.percent
        return d


class LLMManager:
    """Singleton manager for LLM operations"""
    _instance = None
    _lock = threading.Lock()
    
    def __new__(cls):
        if cls._instance is None:
            with cls._lock:
                if cls._instance is None:
                    cls._instance = super().__new__(cls)
                    cls._instance._initialized = False
        return cls._instance
    
    def __init__(self):
        if self._initialized:
            return
        
        self._initialized = True
        # Use app's own folder - no fallback to user home
        from app.config_manager import get_app_directory
        self.base_path = get_app_directory()
        self.config_path = self.base_path / 'config'
        self.cache_path = self.base_path / 'cache' / 'models'
        
        # Ensure directories exist
        self.config_path.mkdir(parents=True, exist_ok=True)
        self.cache_path.mkdir(parents=True, exist_ok=True)
        
        # Initialize repository manager for multiple directories
        from app.services.repository_manager import get_repository_manager
        self.repo_mgr = get_repository_manager()
        
        # Legacy: Keep models_path for backward compatibility (default directory)
        default_dir = self.repo_mgr.get_directory('default')
        if default_dir:
            self.models_path = Path(default_dir.path)
        else:
            self.models_path = self.base_path / 'models'
            self.models_path.mkdir(parents=True, exist_ok=True)
        
        # Active downloads
        self._downloads: Dict[str, DownloadProgress] = {}
        self._download_threads: Dict[str, threading.Thread] = {}
        self._download_callbacks: Dict[str, List[Callable]] = {}
        self._cancel_flags: Dict[str, bool] = {}
        
        # Load settings
        self._settings = self._load_settings()
    
    def _load_settings(self) -> dict:
        """Load LLM settings from config file"""
        settings_file = self.config_path / 'llm_settings.json'
        if settings_file.exists():
            try:
                with open(settings_file, 'r') as f:
                    return json.load(f)
            except:
                pass
        return {
            'huggingface_token': None,
            'default_format': 'gguf',
            'auto_delete_incomplete': True,
            'max_concurrent_downloads': 2,
            'download_chunk_size': 8192,
        }
    
    def _save_settings(self):
        """Save settings to config file"""
        settings_file = self.config_path / 'llm_settings.json'
        with open(settings_file, 'w') as f:
            json.dump(self._settings, f, indent=2)
    
    def get_settings(self) -> dict:
        """Get current settings"""
        return self._settings.copy()
    
    def update_settings(self, settings: dict):
        """Update settings"""
        self._settings.update(settings)
        self._save_settings()
    
    def set_huggingface_token(self, token: str):
        """Set HuggingFace API token"""
        self._settings['huggingface_token'] = token
        self._save_settings()
    
    def get_huggingface_token(self) -> Optional[str]:
        """Get HuggingFace API token"""
        return self._settings.get('huggingface_token')
    
    # =====================
    # Local Model Management
    # =====================
    
    def get_local_models(self, model_type: Optional[str] = None) -> List[LocalModel]:
        """
        Get all locally downloaded models from all enabled directories
        
        Args:
            model_type: Optional filter by model type ('llm', 'text_to_image', 'speech_to_text', etc.)
                      If None, returns only LLM models for backward compatibility
        """
        # If model_type is specified and not 'llm', get AI service models
        if model_type and model_type != 'llm':
            return self._get_ai_service_models(model_type)
        
        models = []
        all_model_paths = self.repo_mgr.get_all_model_paths()
        
        # Track models by normalized path and filename+size to prevent duplicates
        seen_paths = set()
        seen_by_file = {}  # (filename, size) -> model
        
        # Load from all directories
        for model_path in all_model_paths:
            models_index = model_path / 'index.json'
            
            if models_index.exists():
                try:
                    with open(models_index, 'r') as f:
                        index = json.load(f)
                        for model_data in index.get('models', []):
                            # Verify file still exists
                            model_file_path = Path(model_data.get('path', ''))
                            
                            if not model_file_path.exists():
                                model_data['status'] = 'missing'
                            
                            try:
                                # Normalize path for comparison
                                normalized_path = str(model_file_path.resolve())
                                
                                # Check if we've already seen this exact path
                                if normalized_path in seen_paths:
                                    continue
                                
                                # Also check by filename and size
                                filename = model_data.get('filename', model_file_path.name)
                                size = model_data.get('size', model_file_path.stat().st_size if model_file_path.exists() else 0)
                                key = (filename, size)
                                
                                if key in seen_by_file:
                                    # Duplicate found - keep the one with more complete metadata
                                    existing = seen_by_file[key]
                                    new_model = LocalModel(**model_data)
                                    
                                    # Prefer model with 'local' source over 'huggingface'
                                    # or model with more complete metadata
                                    if (new_model.source == 'local' and existing.source != 'local') or \
                                       (new_model.quantization and not existing.quantization) or \
                                       (len(new_model.metadata or {}) > len(existing.metadata or {})):
                                        # Replace existing with new one
                                        models = [m for m in models if m.id != existing.id]
                                        models.append(new_model)
                                        seen_by_file[key] = new_model
                                        # Update seen_paths
                                        try:
                                            existing_path = str(Path(existing.path).resolve())
                                            if existing_path in seen_paths:
                                                del seen_paths[existing_path]
                                        except:
                                            pass
                                        seen_paths.add(normalized_path)
                                    # Otherwise keep existing, skip new one
                                    continue
                                
                                # New unique model
                                model = LocalModel(**model_data)
                                models.append(model)
                                seen_paths.add(normalized_path)
                                seen_by_file[key] = model
                                
                            except Exception as e:
                                # If path resolution fails, still check by filename+size
                                filename = model_data.get('filename', model_file_path.name)
                                size = model_data.get('size', 0)
                                key = (filename, size)
                                
                                if key not in seen_by_file and str(model_file_path) not in seen_paths:
                                    model = LocalModel(**model_data)
                                    models.append(model)
                                    seen_paths.add(str(model_file_path))
                                    seen_by_file[key] = model
                except Exception as e:
                    print(f"Error loading models index from {model_path}: {e}")
        
        # Also scan for unindexed models in all directories
        self._scan_for_models(models, all_model_paths)
        
        # Merge environment associations from LLM environment manager
        self._merge_env_associations(models)
        
        return models
    
    def _merge_env_associations(self, models: List[LocalModel]):
        """Merge venv associations from the LLM environment manager"""
        try:
            from app.services.llm_environment import get_llm_env_manager
            llm_env_mgr = get_llm_env_manager()
            
            for model in models:
                env_info = llm_env_mgr.get_model_environment(model.id)
                if env_info:
                    model.venv_name = env_info.get('venv_name')
                    model.gpu_backend = env_info.get('gpu_backend')
        except Exception as e:
            print(f"Error merging env associations: {e}")
    
    def _scan_for_models(self, existing_models: List[LocalModel], model_paths: Optional[List[Path]] = None):
        """Scan model directories for unindexed models"""
        if model_paths is None:
            model_paths = self.repo_mgr.get_all_model_paths()
            if not model_paths:
                model_paths = [self.models_path]  # Fallback to legacy path
        
        # Normalize existing paths for comparison (resolve to absolute paths)
        existing_paths_normalized = set()
        existing_by_filename_size = {}  # (filename, size) -> model
        
        for m in existing_models:
            try:
                # Normalize path - resolve to absolute and normalize separators
                path_obj = Path(m.path)
                if path_obj.exists():
                    normalized = str(path_obj.resolve())
                    existing_paths_normalized.add(normalized)
                    # Also index by filename and size to catch duplicates
                    key = (m.filename, m.size)
                    if key not in existing_by_filename_size:
                        existing_by_filename_size[key] = m
            except Exception:
                # If path doesn't exist or can't be resolved, use original path
                existing_paths_normalized.add(str(Path(m.path).resolve()) if Path(m.path).is_absolute() else m.path)
        
        # Look for GGUF files in all directories
        for model_path in model_paths:
            if not model_path.exists():
                continue
            for model_file in model_path.rglob('*.gguf'):
                try:
                    # Normalize the scanned file path
                    normalized_file_path = str(model_file.resolve())
                    
                    # Check if this file is already indexed (by normalized path)
                    if normalized_file_path in existing_paths_normalized:
                        continue
                    
                    # Also check by filename and size to catch duplicates with different path formats
                    file_size = model_file.stat().st_size
                    key = (model_file.name, file_size)
                    if key in existing_by_filename_size:
                        # This file is already indexed, skip it
                        continue
                    
                    # Add unindexed model
                    model = LocalModel(
                        id=hashlib.md5(str(model_file).encode()).hexdigest()[:12],
                        name=model_file.stem,
                        path=str(model_file),
                        filename=model_file.name,
                        size=file_size,
                        format='gguf',
                        source='local',
                        status='available',
                        downloaded_at=datetime.fromtimestamp(
                            model_file.stat().st_mtime
                        ).isoformat()
                    )
                    existing_models.append(model)
                    # Add to our tracking sets to prevent duplicates in this scan
                    existing_paths_normalized.add(normalized_file_path)
                    existing_by_filename_size[key] = model
                except Exception as e:
                    # Skip files that can't be accessed
                    print(f"Error scanning model file {model_file}: {e}")
                    continue
    
    def _save_models_index(self, models: List[LocalModel]):
        """Save models index to appropriate directories, with deduplication"""
        # First deduplicate models
        models = self._deduplicate_models(models)
        
        # Group models by directory
        from app.services.repository_manager import get_repository_manager
        repo_mgr = get_repository_manager()
        all_dirs = repo_mgr.get_all_model_paths()
        
        # Create a mapping of directory to models
        dir_models: Dict[Path, List[LocalModel]] = {}
        
        for model in models:
            model_path = Path(model.path)
            # Find which directory this model belongs to
            for dir_path in all_dirs:
                try:
                    if model_path.is_relative_to(dir_path):
                        if dir_path not in dir_models:
                            dir_models[dir_path] = []
                        dir_models[dir_path].append(model)
                        break
                except ValueError:
                    # Path not relative to this directory
                    continue
        
        # Save index for each directory
        for dir_path, dir_model_list in dir_models.items():
            models_index = dir_path / 'index.json'
            with open(models_index, 'w') as f:
                json.dump({
                    'version': 1,
                    'updated': datetime.now().isoformat(),
                    'models': [m.to_dict() for m in dir_model_list]
                }, f, indent=2)
        
        # Also save to default/legacy location for backward compatibility
        if self.models_path not in dir_models:
            models_index = self.models_path / 'index.json'
            with open(models_index, 'w') as f:
                json.dump({
                    'version': 1,
                    'updated': datetime.now().isoformat(),
                    'models': []
                }, f, indent=2)
    
    def _deduplicate_models(self, models: List[LocalModel]) -> List[LocalModel]:
        """Remove duplicate models, keeping the best version of each"""
        seen_paths = {}
        seen_by_file = {}  # (filename, size) -> model
        deduplicated = []
        
        for model in models:
            model_path = Path(model.path)
            
            try:
                # Normalize path
                normalized_path = str(model_path.resolve())
                
                # Check by normalized path
                if normalized_path in seen_paths:
                    # Duplicate by path - keep the better one
                    existing = seen_paths[normalized_path]
                    if self._is_better_model(model, existing):
                        # Replace existing with new one
                        deduplicated = [m for m in deduplicated if m.id != existing.id]
                        deduplicated.append(model)
                        seen_paths[normalized_path] = model
                        # Update seen_by_file too
                        key = (model.filename, model.size)
                        seen_by_file[key] = model
                    # Otherwise keep existing, skip new one
                    continue
                
                # Check by filename and size
                key = (model.filename, model.size)
                if key in seen_by_file:
                    # Duplicate by filename+size - keep the better one
                    existing = seen_by_file[key]
                    if self._is_better_model(model, existing):
                        # Replace existing with new one
                        deduplicated = [m for m in deduplicated if m.id != existing.id]
                        deduplicated.append(model)
                        seen_by_file[key] = model
                        # Update seen_paths too
                        try:
                            existing_path = str(Path(existing.path).resolve())
                            if existing_path in seen_paths:
                                del seen_paths[existing_path]
                            seen_paths[normalized_path] = model
                        except:
                            seen_paths[normalized_path] = model
                    # Otherwise keep existing, skip new one
                    continue
                
                # New unique model
                deduplicated.append(model)
                seen_paths[normalized_path] = model
                seen_by_file[key] = model
                
            except Exception:
                # If path resolution fails, use original path string
                if model.path not in seen_paths:
                    deduplicated.append(model)
                    seen_paths[model.path] = model
                    key = (model.filename, model.size)
                    if key not in seen_by_file:
                        seen_by_file[key] = model
        
        return deduplicated
    
    def _is_better_model(self, new: LocalModel, existing: LocalModel) -> bool:
        """Determine if new model is better than existing (for deduplication)"""
        # Prefer 'local' source over 'huggingface'
        if new.source == 'local' and existing.source != 'local':
            return True
        if existing.source == 'local' and new.source != 'local':
            return False
        
        # Prefer model with quantization info
        if new.quantization and not existing.quantization:
            return True
        if existing.quantization and not new.quantization:
            return False
        
        # Prefer model with more complete metadata
        new_meta = new.metadata or {}
        existing_meta = existing.metadata or {}
        if len(new_meta) > len(existing_meta):
            return True
        if len(existing_meta) > len(new_meta):
            return False
        
        # Prefer model with parameters info
        if new.parameters and not existing.parameters:
            return True
        if existing.parameters and not new.parameters:
            return False
        
        # If all else equal, prefer the one that was added more recently (keep existing)
        return False
    
    def add_local_model(self, model: LocalModel):
        """Add a model to the index, removing any duplicates"""
        models = self.get_local_models()
        
        # Remove existing with same ID
        models = [m for m in models if m.id != model.id]
        
        # Also remove duplicates by normalized path
        model_path = Path(model.path)
        try:
            normalized_path = str(model_path.resolve())
            models = [m for m in models if str(Path(m.path).resolve()) != normalized_path]
        except Exception:
            # If resolution fails, use original path
            models = [m for m in models if m.path != model.path]
        
        # Also remove duplicates by filename and size
        key = (model.filename, model.size)
        models = [m for m in models if (m.filename, m.size) != key or m.id == model.id]
        
        models.append(model)
        self._save_models_index(models)
    
    def remove_local_model(self, model_id: str, delete_file: bool = True) -> bool:
        """Remove a model from local storage"""
        models = self.get_local_models()
        model = next((m for m in models if m.id == model_id), None)
        
        if not model:
            return False
        
        if delete_file and Path(model.path).exists():
            try:
                os.remove(model.path)
            except Exception as e:
                print(f"Error deleting model file: {e}")
                return False
        
        # Update index
        models = [m for m in models if m.id != model_id]
        self._save_models_index(models)
        return True
    
    def get_model_by_id(self, model_id: str) -> Optional[LocalModel]:
        """Get a specific local model by ID"""
        models = self.get_local_models()
        return next((m for m in models if m.id == model_id), None)
    
    def cleanup_duplicate_models(self) -> Dict[str, Any]:
        """
        Clean up duplicate models from index files.
        This will deduplicate and re-save all model indexes.
        
        Returns:
            Dict with cleanup results
        """
        try:
            # Get all models (this will deduplicate on load)
            models_before = self.get_local_models()
            
            # Deduplicate explicitly
            models_after = self._deduplicate_models(models_before)
            
            # Count duplicates removed
            duplicates_removed = len(models_before) - len(models_after)
            
            if duplicates_removed > 0:
                # Re-save deduplicated models
                self._save_models_index(models_after)
            
            return {
                'success': True,
                'models_before': len(models_before),
                'models_after': len(models_after),
                'duplicates_removed': duplicates_removed
            }
        except Exception as e:
            return {
                'success': False,
                'error': str(e)
            }
    
    # =====================
    # Download Management
    # =====================
    
    def get_active_downloads(self) -> List[DownloadProgress]:
        """Get all active downloads"""
        return list(self._downloads.values())
    
    def get_download_progress(self, model_id: str) -> Optional[DownloadProgress]:
        """Get progress for a specific download"""
        return self._downloads.get(model_id)
    
    def cancel_download(self, model_id: str):
        """Cancel an active download"""
        self._cancel_flags[model_id] = True
    
    def pause_download(self, model_id: str):
        """Pause an active download"""
        if model_id in self._downloads:
            self._downloads[model_id].status = 'paused'
    
    def register_download_callback(self, model_id: str, callback: Callable):
        """Register a callback for download progress updates"""
        if model_id not in self._download_callbacks:
            self._download_callbacks[model_id] = []
        self._download_callbacks[model_id].append(callback)
    
    def _notify_download_progress(self, model_id: str, progress: DownloadProgress):
        """Notify all registered callbacks of progress update"""
        for callback in self._download_callbacks.get(model_id, []):
            try:
                callback(progress)
            except Exception as e:
                print(f"Error in download callback: {e}")
    
    # =====================
    # Statistics
    # =====================
    
    def get_storage_stats(self) -> dict:
        """Get storage statistics"""
        total_size = 0
        model_count = 0
        
        for model in self.get_local_models():
            if model.status == 'available':
                total_size += model.size
                model_count += 1
        
        # Get disk space - ensure path exists first
        try:
            if not self.models_path.exists():
                self.models_path.mkdir(parents=True, exist_ok=True)
            disk_usage = shutil.disk_usage(str(self.models_path))
            disk_free = disk_usage.free
            disk_total = disk_usage.total
        except Exception as e:
            print(f"Error getting disk usage for {self.models_path}: {e}")
            disk_free = 0
            disk_total = 0
        
        # Get primary models path (default directory)
        primary_path = str(self.models_path)
        default_dir = self.repo_mgr.get_directory('default')
        if default_dir:
            primary_path = default_dir.path
        
        return {
            'model_count': model_count,
            'total_size': total_size,
            'total_size_gb': round(total_size / (1024**3), 2),
            'disk_free': disk_free,
            'disk_free_gb': round(disk_free / (1024**3), 2),
            'disk_total': disk_total,
            'disk_total_gb': round(disk_total / (1024**3), 2),
            'models_path': primary_path,
            'directories_count': len(self.repo_mgr.get_directories(enabled_only=True))
        }
    
    def _get_ai_service_models(self, service_type: str) -> List[LocalModel]:
        """
        Get AI service models and convert them to LocalModel format for unified display
        
        Args:
            service_type: AI service type (text_to_image, speech_to_text, etc.)
        """
        try:
            from app.services.ai_service_model_manager import get_ai_service_model_manager
            
            ai_mgr = get_ai_service_model_manager(service_type)
            ai_models = ai_mgr.get_local_models()
            
            # Convert AIServiceLocalModel to LocalModel format
            local_models = []
            for ai_model in ai_models:
                # Create a LocalModel from AIServiceLocalModel
                local_model = LocalModel(
                    id=ai_model.id,
                    name=ai_model.name,
                    path=ai_model.path,
                    filename=ai_model.model_id,  # Use model_id as filename
                    size=ai_model.size,
                    format=ai_model.format,
                    source=ai_model.source,
                    status=ai_model.status,
                    downloaded_at=ai_model.downloaded_at,
                    metadata={
                        'model_id': ai_model.model_id,
                        'service_type': ai_model.service_type,
                        'description': ai_model.description,
                        **ai_model.metadata
                    }
                )
                local_models.append(local_model)
            
            return local_models
        except Exception as e:
            print(f"Error getting AI service models for {service_type}: {e}")
            return []
    
    def get_all_models_by_type(self) -> Dict[str, List[LocalModel]]:
        """
        Get all models grouped by type (llm, text_to_image, speech_to_text, etc.)
        
        Returns:
            Dict mapping model_type -> List[LocalModel]
        """
        result = {
            'llm': self.get_local_models('llm'),
            'text_to_image': self._get_ai_service_models('text_to_image'),
            'speech_to_text': self._get_ai_service_models('speech_to_text'),
            'text_to_speech': self._get_ai_service_models('text_to_speech'),
            'voice_to_voice': self._get_ai_service_models('voice_to_voice')
        }
        return result
    
    def get_storage_stats_by_type(self) -> Dict[str, Dict[str, Any]]:
        """
        Get storage statistics grouped by model type
        
        Returns:
            Dict mapping model_type -> storage stats
        """
        stats = {}
        
        # LLM models
        llm_models = self.get_local_models('llm')
        llm_size = sum(m.size for m in llm_models)
        stats['llm'] = {
            'model_count': len(llm_models),
            'total_size_gb': round(llm_size / (1024 ** 3), 2),
            'total_size_bytes': llm_size
        }
        
        # AI service models
        for service_type in ['text_to_image', 'speech_to_text', 'text_to_speech', 'voice_to_voice']:
            try:
                from app.services.ai_service_model_manager import get_ai_service_model_manager
                ai_mgr = get_ai_service_model_manager(service_type)
                service_stats = ai_mgr.get_storage_stats()
                stats[service_type] = service_stats
            except Exception as e:
                stats[service_type] = {
                    'model_count': 0,
                    'total_size_gb': 0,
                    'total_size_bytes': 0
                }
        
        return stats
