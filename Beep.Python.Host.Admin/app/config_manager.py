"""
Configuration Manager
Handles loading and saving of application configuration.
Settings are stored ONLY in the application's own folder (portable).
NO FALLBACK to user directories - everything stays in the install folder.
"""
import os
import sys
import json
from pathlib import Path
from typing import Dict, Any, Optional


def get_app_directory() -> Path:
    """
    Get the application directory - where the executable is located.
    ALL settings and data are stored HERE. No fallback to user folders.
    """
    # If running as frozen executable (PyInstaller)
    if getattr(sys, 'frozen', False):
        # The executable's directory - this is where everything is stored
        app_dir = Path(sys.executable).parent
        print(f"[BeepPython] Running as frozen executable. App directory: {app_dir}")
        return app_dir
    
    # Running as script - use script's parent directory
    app_dir = Path(__file__).parent.parent
    print(f"[BeepPython] Running as script. App directory: {app_dir}")
    return app_dir


class ConfigManager:
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
        
        # ALWAYS use the app's own directory - no fallback
        self._base_path = get_app_directory()
        
        # Ensure config directory exists
        config_dir = self._base_path / 'config'
        config_dir.mkdir(parents=True, exist_ok=True)
        
        self._config_path = config_dir / 'app_config.json'
        self._install_settings_path = config_dir / 'app_settings.json'
        self._config = {}
        
        self.load()
        
    @property
    def base_path(self) -> Path:
        """Get the base application path"""
        return self._base_path
        
    @property
    def data_path(self) -> Path:
        """Get the data directory path (inside app folder)"""
        data_dir = self._base_path / 'data'
        data_dir.mkdir(parents=True, exist_ok=True)
        return data_dir
    
    @property
    def logs_path(self) -> Path:
        """Get the logs directory path (inside app folder)"""
        logs_dir = self._base_path / 'logs'
        logs_dir.mkdir(parents=True, exist_ok=True)
        return logs_dir
        
    def load(self):
        """Load configuration from app's config folder"""
        self._config = {}
        
        # First load install settings (from setup wizard)
        if self._install_settings_path.exists():
            try:
                with open(self._install_settings_path, 'r') as f:
                    install_settings = json.load(f)
                    self._config.update(install_settings)
            except Exception as e:
                print(f"Warning: Could not load install settings: {e}")
        
        # Then load/merge main config (user changes)
        if self._config_path.exists():
            try:
                with open(self._config_path, 'r') as f:
                    user_config = json.load(f)
                    self._config.update(user_config)
            except Exception as e:
                print(f"Error loading config: {e}")
            
    def save(self):
        """Save configuration to file"""
        self._config_path.parent.mkdir(parents=True, exist_ok=True)
        try:
            with open(self._config_path, 'w') as f:
                json.dump(self._config, f, indent=4)
        except Exception as e:
            print(f"Error saving config: {e}")
            
    def get(self, key: str, default: Any = None) -> Any:
        """Get configuration value"""
        return self._config.get(key, default)
        
    def set(self, key: str, value: Any):
        """Set configuration value and save"""
        self._config[key] = value
        self.save()
        
    @property
    def is_configured(self) -> bool:
        """Check if application is configured"""
        return self._config.get('is_configured', False)
        
    @property
    def db_uri(self) -> Optional[str]:
        """Get configured database URI"""
        return self._config.get('SQLALCHEMY_DATABASE_URI')

# Global instance
config_manager = ConfigManager()
