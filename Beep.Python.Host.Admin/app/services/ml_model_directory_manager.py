"""
ML Model Directory Manager Service

Manages multiple storage directories for ML models, similar to LLM repository management.
Stores configuration in config/ml_model_directories.json
"""
import os
import json
from pathlib import Path
from typing import List, Dict, Any, Optional
from dataclasses import dataclass, asdict


@dataclass
class MLModelDirectory:
    """Represents an ML model storage directory"""
    id: str
    name: str
    path: str
    enabled: bool
    max_size_gb: Optional[float] = None  # Optional size limit
    description: Optional[str] = None
    priority: int = 0  # Storage priority (lower = higher priority)
    
    def to_dict(self) -> dict:
        return asdict(self)
    
    def get_available_space_gb(self) -> float:
        """Get available space in GB"""
        try:
            import shutil
            path = Path(self.path)
            if not path.exists():
                path.mkdir(parents=True, exist_ok=True)
            stat = shutil.disk_usage(str(path))
            return stat.free / (1024 ** 3)
        except Exception as e:
            print(f"Error getting available space for {self.path}: {e}")
            return 0.0
    
    def get_used_space_gb(self) -> float:
        """Get used space in GB"""
        try:
            path = Path(self.path)
            if not path.exists():
                return 0.0
            total_size = sum(f.stat().st_size for f in path.rglob('*') if f.is_file())
            return total_size / (1024 ** 3)
        except Exception as e:
            print(f"Error getting used space for {self.path}: {e}")
            return 0.0


class MLModelDirectoryManager:
    """Manages ML model storage directories"""
    _instance = None
    
    def __new__(cls):
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
        self.config_path.mkdir(parents=True, exist_ok=True)
        
        self.directories_file = self.config_path / 'ml_model_directories.json'
        
        # Load configurations
        self._directories: List[MLModelDirectory] = []
        self._load_directories()
        
        # Initialize defaults if empty
        if not self._directories:
            self._initialize_default_directories()
    
    def _load_directories(self):
        """Load directories from config file"""
        if self.directories_file.exists():
            try:
                with open(self.directories_file, 'r') as f:
                    data = json.load(f)
                    loaded_dirs = []
                    for dir_data in data.get('directories', []):
                        # Validate and fix paths
                        dir_path = dir_data.get('path', '')
                        if dir_path:
                            path_obj = Path(dir_path)
                            if not path_obj.is_absolute():
                                dir_data['path'] = str(self.base_path / dir_path)
                            elif not path_obj.exists():
                                # Try to reconstruct using relative parts
                                try:
                                    rel_parts = path_obj.parts[-2:] if len(path_obj.parts) > 1 else path_obj.parts[-1:]
                                    new_path = self.base_path.joinpath(*rel_parts)
                                    dir_data['path'] = str(new_path)
                                except:
                                    dir_data['path'] = str(self.base_path / 'ml_models')
                        loaded_dirs.append(MLModelDirectory(**dir_data))
                    self._directories = loaded_dirs
                    
                    # Validate default directory
                    self._validate_default_directory()
            except Exception as e:
                print(f"Error loading ML model directories: {e}")
                self._directories = []
        else:
            self._directories = []
    
    def _validate_default_directory(self):
        """Ensure default directory exists and points to current app location"""
        default_dir = next((d for d in self._directories if d.id == 'default'), None)
        expected_default = self.base_path / 'ml_models'
        
        if default_dir:
            current_path = Path(default_dir.path)
            if current_path != expected_default:
                default_dir.path = str(expected_default)
                expected_default.mkdir(parents=True, exist_ok=True)
                self._save_directories()
        else:
            self._initialize_default_directories()
    
    def _save_directories(self):
        """Save directories to config file"""
        data = {
            'directories': [dir.to_dict() for dir in self._directories]
        }
        with open(self.directories_file, 'w') as f:
            json.dump(data, f, indent=2)
    
    def _initialize_default_directories(self):
        """Initialize default ML model directory"""
        default_dir = MLModelDirectory(
            id='default',
            name='Default ML Models',
            path=str(self.base_path / 'ml_models'),
            enabled=True,
            description='Default ML model storage directory',
            priority=0
        )
        self._directories = [default_dir]
        self._save_directories()
        
        # Ensure directory exists
        Path(default_dir.path).mkdir(parents=True, exist_ok=True)
    
    # =====================
    # Directory Management
    # =====================
    
    def get_directories(self, enabled_only: bool = False) -> List[MLModelDirectory]:
        """Get all ML model directories"""
        dirs = self._directories
        if enabled_only:
            dirs = [d for d in dirs if d.enabled]
        return sorted(dirs, key=lambda x: x.priority)
    
    def get_directory(self, dir_id: str) -> Optional[MLModelDirectory]:
        """Get a specific directory by ID"""
        return next((d for d in self._directories if d.id == dir_id), None)
    
    def add_directory(self, directory: MLModelDirectory) -> bool:
        """Add a new ML model directory"""
        if any(d.id == directory.id for d in self._directories):
            return False  # ID already exists
        
        # Ensure directory exists
        Path(directory.path).mkdir(parents=True, exist_ok=True)
        
        self._directories.append(directory)
        self._save_directories()
        return True
    
    def update_directory(self, dir_id: str, updates: Dict[str, Any]) -> bool:
        """Update a directory"""
        dir_obj = self.get_directory(dir_id)
        if not dir_obj:
            return False
        
        # If path changed, ensure new directory exists
        if 'path' in updates:
            Path(updates['path']).mkdir(parents=True, exist_ok=True)
        
        for key, value in updates.items():
            if hasattr(dir_obj, key):
                setattr(dir_obj, key, value)
        
        self._save_directories()
        return True
    
    def delete_directory(self, dir_id: str) -> bool:
        """Delete a directory (doesn't delete files, just removes from config)"""
        if dir_id == 'default':  # Prevent deleting default
            return False
        
        self._directories = [d for d in self._directories if d.id != dir_id]
        self._save_directories()
        return True
    
    def get_best_directory_for_upload(self, size_gb: float) -> Optional[MLModelDirectory]:
        """Get the best directory for uploading a model of given size"""
        enabled_dirs = self.get_directories(enabled_only=True)
        
        # Filter directories with enough space
        suitable_dirs = []
        for dir_obj in enabled_dirs:
            available = dir_obj.get_available_space_gb()
            if dir_obj.max_size_gb:
                used = dir_obj.get_used_space_gb()
                if used + size_gb > dir_obj.max_size_gb:
                    continue
            
            if available >= size_gb:
                suitable_dirs.append((dir_obj, available))
        
        if not suitable_dirs:
            return None
        
        # Sort by priority, then by available space (descending)
        suitable_dirs.sort(key=lambda x: (x[0].priority, -x[1]))
        return suitable_dirs[0][0]
    
    def get_all_model_paths(self) -> List[Path]:
        """Get all enabled ML model directory paths"""
        return [Path(d.path) for d in self.get_directories(enabled_only=True)]


def get_ml_model_directory_manager() -> MLModelDirectoryManager:
    """Get singleton instance of MLModelDirectoryManager"""
    return MLModelDirectoryManager()

