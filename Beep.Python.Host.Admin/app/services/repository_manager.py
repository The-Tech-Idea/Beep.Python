"""
Repository Manager Service

Manages multiple model repositories and directories for cross-platform model management.
Supports:
- HuggingFace Hub
- Ollama
- Local directories
- Custom repositories
"""
import os
import json
from pathlib import Path
from typing import List, Dict, Any, Optional
from dataclasses import dataclass, asdict
from enum import Enum


class RepositoryType(Enum):
    """Types of model repositories"""
    HUGGINGFACE = "huggingface"
    OLLAMA = "ollama"
    LOCAL = "local"
    CUSTOM = "custom"


@dataclass
class ModelRepository:
    """Represents a model repository configuration"""
    id: str
    name: str
    type: str  # huggingface, ollama, local, custom
    enabled: bool
    url: Optional[str] = None  # For custom repositories
    api_key: Optional[str] = None  # For authenticated repositories
    description: Optional[str] = None
    priority: int = 0  # Search priority (lower = higher priority)
    requires_auth: bool = False  # Whether this repository requires authentication
    auth_required_for: Optional[str] = None  # What requires auth (e.g., "gated models", "private models")
    
    def to_dict(self) -> dict:
        d = asdict(self)
        # Don't expose API keys in dict
        if 'api_key' in d and d['api_key']:
            d['api_key'] = '***' if d['api_key'] else None
        d['has_api_key'] = bool(self.api_key)
        return d
    
    def is_configured(self) -> bool:
        """Check if repository is properly configured (has API key if required)"""
        if self.requires_auth and not self.api_key:
            return False
        return True


@dataclass
class ModelDirectory:
    """Represents a model storage directory"""
    id: str
    name: str
    path: str
    enabled: bool
    max_size_gb: Optional[float] = None  # Optional size limit
    description: Optional[str] = None
    priority: int = 0  # Download priority (lower = higher priority)
    
    def to_dict(self) -> dict:
        return asdict(self)
    
    def get_available_space_gb(self) -> float:
        """Get available space in GB"""
        try:
            import shutil
            # Ensure path exists before checking disk usage
            path = Path(self.path)
            if not path.exists():
                path.mkdir(parents=True, exist_ok=True)
            stat = shutil.disk_usage(str(path))
            return stat.free / (1024 ** 3)
        except Exception as e:
            print(f"Error getting available space for {self.path}: {e}")
            return 0.0
    
    def get_used_space_gb(self) -> float:
        """Get used space in GB (total size of all files in this directory)"""
        try:
            path = Path(self.path)
            if not path.exists():
                return 0.0
            total_size = sum(f.stat().st_size for f in path.rglob('*') if f.is_file())
            return total_size / (1024 ** 3)
        except Exception as e:
            print(f"Error getting used space for {self.path}: {e}")
            return 0.0


class RepositoryManager:
    """Manages model repositories and directories"""
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
        
        self.repositories_file = self.config_path / 'repositories.json'
        self.directories_file = self.config_path / 'model_directories.json'
        
        # Load configurations
        self._repositories: List[ModelRepository] = []
        self._directories: List[ModelDirectory] = []
        self._load_repositories()
        self._load_directories()
        
        # Initialize defaults if empty
        if not self._repositories:
            self._initialize_default_repositories()
        if not self._directories:
            self._initialize_default_directories()
    
    def _load_repositories(self):
        """Load repositories from config file"""
        if self.repositories_file.exists():
            try:
                with open(self.repositories_file, 'r') as f:
                    data = json.load(f)
                    self._repositories = []
                    for repo_data in data.get('repositories', []):
                        # Filter out computed fields that aren't part of the dataclass
                        repo_data_clean = {k: v for k, v in repo_data.items() 
                                         if k not in ['has_api_key']}
                        
                        # Ensure required fields have defaults if missing
                        required_fields = {
                            'id': repo_data_clean.get('id', 'unknown'),
                            'name': repo_data_clean.get('name', 'Unknown'),
                            'type': repo_data_clean.get('type', 'custom'),
                            'enabled': repo_data_clean.get('enabled', True)
                        }
                        repo_data_clean.update(required_fields)
                        
                        try:
                            self._repositories.append(ModelRepository(**repo_data_clean))
                        except Exception as e:
                            print(f"Error loading repository {repo_data.get('id', 'unknown')}: {e}")
                            continue
            except Exception as e:
                print(f"Error loading repositories: {e}")
                self._repositories = []
        else:
            self._repositories = []
    
    def _load_directories(self):
        """Load directories from config file"""
        if self.directories_file.exists():
            try:
                with open(self.directories_file, 'r') as f:
                    data = json.load(f)
                    loaded_dirs = []
                    for dir_data in data.get('directories', []):
                        # Validate and fix paths - convert relative to absolute based on current base_path
                        dir_path = dir_data.get('path', '')
                        if dir_path:
                            path_obj = Path(dir_path)
                            # Check if it's a relative path stored or absolute path that doesn't match current app location
                            if not path_obj.is_absolute():
                                # Convert relative path to absolute using current base_path
                                dir_data['path'] = str(self.base_path / dir_path)
                            elif not path_obj.exists():
                                # Absolute path doesn't exist - likely from different installation
                                # Try to reconstruct using the relative part
                                try:
                                    # Get just the last parts of the path (e.g., 'models' or 'data/models')
                                    rel_parts = path_obj.parts[-2:] if len(path_obj.parts) > 1 else path_obj.parts[-1:]
                                    new_path = self.base_path.joinpath(*rel_parts)
                                    dir_data['path'] = str(new_path)
                                except:
                                    dir_data['path'] = str(self.base_path / 'models')
                        loaded_dirs.append(ModelDirectory(**dir_data))
                    self._directories = loaded_dirs
                    
                    # Validate that default directory exists and points to correct location
                    self._validate_default_directory()
            except Exception as e:
                print(f"Error loading directories: {e}")
                self._directories = []
        else:
            self._directories = []
    
    def _validate_default_directory(self):
        """Ensure default directory exists and points to current app location"""
        default_dir = next((d for d in self._directories if d.id == 'default'), None)
        expected_default = self.base_path / 'models'
        
        if default_dir:
            current_path = Path(default_dir.path)
            # If default doesn't point to current app's models folder, fix it
            if current_path != expected_default:
                default_dir.path = str(expected_default)
                expected_default.mkdir(parents=True, exist_ok=True)
                self._save_directories()
        else:
            # No default directory found, create one
            self._initialize_default_directories()
    
    def _save_repositories(self):
        """Save repositories to config file"""
        data = {
            'repositories': [repo.to_dict() for repo in self._repositories]
        }
        with open(self.repositories_file, 'w') as f:
            json.dump(data, f, indent=2)
    
    def _save_directories(self):
        """Save directories to config file"""
        data = {
            'directories': [dir.to_dict() for dir in self._directories]
        }
        with open(self.directories_file, 'w') as f:
            json.dump(data, f, indent=2)
    
    def _initialize_default_repositories(self):
        """Initialize default repositories"""
        default_repos = [
            ModelRepository(
                id='hf_default',
                name='HuggingFace Hub',
                type='huggingface',
                enabled=True,
                description='Default HuggingFace repository. Token required for gated/private models.',
                priority=0,
                requires_auth=True,  # Token needed for gated models
                auth_required_for='gated and private models'
            ),
            ModelRepository(
                id='ollama_default',
                name='Ollama',
                type='ollama',
                enabled=False,  # Disabled by default, user can enable
                description='Ollama model repository',
                priority=1,
                requires_auth=False
            )
        ]
        self._repositories = default_repos
        self._save_repositories()
    
    def _initialize_default_directories(self):
        """Initialize default model directories"""
        default_dir = ModelDirectory(
            id='default',
            name='Default Models',
            path=str(self.base_path / 'models'),
            enabled=True,
            description='Default model storage directory',
            priority=0
        )
        self._directories = [default_dir]
        self._save_directories()
        
        # Ensure directory exists
        Path(default_dir.path).mkdir(parents=True, exist_ok=True)
    
    # =====================
    # Repository Management
    # =====================
    
    def get_repositories(self, enabled_only: bool = False) -> List[ModelRepository]:
        """Get all repositories"""
        repos = self._repositories
        if enabled_only:
            repos = [r for r in repos if r.enabled]
        return sorted(repos, key=lambda x: x.priority)
    
    def get_repository(self, repo_id: str) -> Optional[ModelRepository]:
        """Get a specific repository by ID"""
        return next((r for r in self._repositories if r.id == repo_id), None)
    
    def add_repository(self, repository: ModelRepository) -> bool:
        """Add a new repository"""
        if any(r.id == repository.id for r in self._repositories):
            return False  # ID already exists
        
        self._repositories.append(repository)
        self._save_repositories()
        return True
    
    def update_repository(self, repo_id: str, updates: Dict[str, Any]) -> bool:
        """Update a repository"""
        repo = self.get_repository(repo_id)
        if not repo:
            return False
        
        # Special handling for enabling repositories that require auth
        if 'enabled' in updates and updates['enabled'] and repo.requires_auth and not repo.api_key:
            # Warn but allow - user can set token later
            pass
        
        for key, value in updates.items():
            if hasattr(repo, key) and key != 'api_key':  # api_key handled separately
                setattr(repo, key, value)
        
        self._save_repositories()
        return True
    
    def delete_repository(self, repo_id: str) -> bool:
        """Delete a repository"""
        if repo_id in ['hf_default']:  # Prevent deleting default
            return False
        
        self._repositories = [r for r in self._repositories if r.id != repo_id]
        self._save_repositories()
        return True
    
    def set_repository_api_key(self, repo_id: str, api_key: Optional[str]):
        """Set API key for a repository (securely)"""
        repo = self.get_repository(repo_id)
        if repo:
            repo.api_key = api_key
            self._save_repositories()
    
    def get_repository_api_key(self, repo_id: str) -> Optional[str]:
        """Get API key for a repository"""
        repo = self.get_repository(repo_id)
        return repo.api_key if repo else None
    
    # =====================
    # Directory Management
    # =====================
    
    def get_directories(self, enabled_only: bool = False) -> List[ModelDirectory]:
        """Get all model directories"""
        dirs = self._directories
        if enabled_only:
            dirs = [d for d in dirs if d.enabled]
        return sorted(dirs, key=lambda x: x.priority)
    
    def get_directory(self, dir_id: str) -> Optional[ModelDirectory]:
        """Get a specific directory by ID"""
        return next((d for d in self._directories if d.id == dir_id), None)
    
    def add_directory(self, directory: ModelDirectory) -> bool:
        """Add a new model directory"""
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
    
    def get_best_directory_for_download(self, size_gb: float) -> Optional[ModelDirectory]:
        """Get the best directory for downloading a model of given size"""
        enabled_dirs = self.get_directories(enabled_only=True)
        
        # Filter directories with enough space
        suitable_dirs = []
        for dir_obj in enabled_dirs:
            available = dir_obj.get_available_space_gb()
            if dir_obj.max_size_gb:
                # Check if adding this model would exceed limit
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
        """Get all enabled model directory paths"""
        return [Path(d.path) for d in self.get_directories(enabled_only=True)]


def get_repository_manager() -> RepositoryManager:
    """Get singleton instance of RepositoryManager"""
    return RepositoryManager()

