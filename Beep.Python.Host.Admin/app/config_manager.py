"""
Configuration Manager
Handles loading and saving of application configuration.
"""
import os
import json
from pathlib import Path
from typing import Dict, Any, Optional

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
        self._base_path = Path(os.environ.get('BEEP_PYTHON_HOME', 
                                              Path.home() / '.beep-llm'))
        self._config_path = self._base_path / 'config' / 'app_config.json'
        self._config = {}
        
        self.load()
        
    def load(self):
        """Load configuration from file"""
        if self._config_path.exists():
            try:
                with open(self._config_path, 'r') as f:
                    self._config = json.load(f)
            except Exception as e:
                print(f"Error loading config: {e}")
                self._config = {}
        else:
            self._config = {}
            
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
