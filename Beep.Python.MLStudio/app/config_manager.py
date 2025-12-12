"""
Configuration Manager for MLStudio
Handles loading and saving of application configuration.
"""
import os
import sys
import json
from pathlib import Path
from typing import Dict, Any, Optional


def get_app_directory() -> Path:
    """
    Get the application directory - where the executable is located.
    ALL settings and data are stored HERE.
    """
    # If running as frozen executable (PyInstaller)
    if getattr(sys, 'frozen', False):
        # The executable's directory
        app_dir = Path(sys.executable).parent
        return app_dir
    
    # Running as script - use script's parent directory
    app_dir = Path(__file__).parent.parent
    return app_dir


class ConfigManager:
    """Manages application configuration"""
    _instance = None
    
    def __init__(self):
        self.app_dir = get_app_directory()
        self.config_file = self.app_dir / 'config' / 'mlstudio_config.json'
        self.config_file.parent.mkdir(parents=True, exist_ok=True)
        self._config = {}
        self._load_config()
    
    def _load_config(self):
        """Load configuration from file"""
        if self.config_file.exists():
            try:
                with open(self.config_file, 'r') as f:
                    self._config = json.load(f)
            except:
                self._config = {}
        else:
            self._config = {}
    
    def _save_config(self):
        """Save configuration to file"""
        try:
            with open(self.config_file, 'w') as f:
                json.dump(self._config, f, indent=2)
        except Exception as e:
            print(f"Warning: Could not save config: {e}")
    
    def get(self, key: str, default: Any = None) -> Any:
        """Get configuration value"""
        return self._config.get(key, default)
    
    def set(self, key: str, value: Any):
        """Set configuration value"""
        self._config[key] = value
        self._save_config()
    
    @property
    def db_uri(self) -> Optional[str]:
        """Get database URI"""
        return self.get('database_uri')
    
    @db_uri.setter
    def db_uri(self, value: str):
        """Set database URI"""
        self.set('database_uri', value)


# Singleton instance
_config_manager = None

def get_config_manager() -> ConfigManager:
    """Get singleton config manager instance"""
    global _config_manager
    if _config_manager is None:
        _config_manager = ConfigManager()
    return _config_manager

