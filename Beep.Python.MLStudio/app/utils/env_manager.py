"""
Environment File Manager
Centralized .env file operations with validation and safety features
"""
import os
import shutil
import logging
from pathlib import Path
from typing import Optional, Dict
from datetime import datetime

logger = logging.getLogger(__name__)


class EnvManager:
    """Manager for .env file operations"""
    
    def __init__(self, env_file_path: str = '.env'):
        """
        Initialize environment file manager
        
        Args:
            env_file_path: Path to .env file
        """
        self.env_file = Path(env_file_path)
        self.backup_dir = Path('instance/backups')
        self.backup_dir.mkdir(parents=True, exist_ok=True)
    
    def read_all(self) -> Dict[str, str]:
        """
        Read all environment variables from .env file
        
        Returns:
            Dictionary of key-value pairs
        """
        if not self.env_file.exists():
            return {}
        
        env_vars = {}
        try:
            with open(self.env_file, 'r', encoding='utf-8') as f:
                for line in f:
                    line = line.strip()
                    # Skip empty lines and comments
                    if not line or line.startswith('#'):
                        continue
                    
                    # Parse key=value
                    if '=' in line:
                        key, value = line.split('=', 1)
                        key = key.strip()
                        value = value.strip()
                        # Remove quotes if present
                        if value.startswith('"') and value.endswith('"'):
                            value = value[1:-1]
                        elif value.startswith("'") and value.endswith("'"):
                            value = value[1:-1]
                        env_vars[key] = value
        except Exception as e:
            logger.error(f"Error reading .env file: {e}")
        
        return env_vars
    
    def get(self, key: str, default: Optional[str] = None) -> Optional[str]:
        """
        Get environment variable value
        
        Args:
            key: Variable name
            default: Default value if not found
            
        Returns:
            Variable value or default
        """
        env_vars = self.read_all()
        return env_vars.get(key, default)
    
    def set(self, key: str, value: str, create_backup: bool = True):
        """
        Set environment variable value
        
        Args:
            key: Variable name
            value: Variable value
            create_backup: Whether to create backup before modifying
        """
        if create_backup:
            self._create_backup()
        
        env_vars = self.read_all()
        env_vars[key] = value
        
        self._write_all(env_vars)
        logger.info(f"Set environment variable: {key}")
    
    def remove(self, key: str, create_backup: bool = True):
        """
        Remove environment variable
        
        Args:
            key: Variable name
            create_backup: Whether to create backup before modifying
        """
        if create_backup:
            self._create_backup()
        
        env_vars = self.read_all()
        if key in env_vars:
            del env_vars[key]
            self._write_all(env_vars)
            logger.info(f"Removed environment variable: {key}")
    
    def set_multiple(self, variables: Dict[str, str], create_backup: bool = True):
        """
        Set multiple environment variables at once
        
        Args:
            variables: Dictionary of key-value pairs
            create_backup: Whether to create backup before modifying
        """
        if create_backup and variables:
            self._create_backup()
        
        env_vars = self.read_all()
        env_vars.update(variables)
        self._write_all(env_vars)
        logger.info(f"Set {len(variables)} environment variables")
    
    def _write_all(self, env_vars: Dict[str, str]):
        """Write all environment variables to file"""
        try:
            # Read existing file to preserve comments and formatting
            existing_lines = []
            existing_vars = set()
            
            if self.env_file.exists():
                with open(self.env_file, 'r', encoding='utf-8') as f:
                    for line in f:
                        stripped = line.strip()
                        if stripped and not stripped.startswith('#'):
                            if '=' in stripped:
                                key = stripped.split('=', 1)[0].strip()
                                existing_vars.add(key)
                                # Update value if key exists in new vars
                                if key in env_vars:
                                    existing_lines.append(f"{key}={env_vars[key]}\n")
                                    continue
                        existing_lines.append(line)
            
            # Add new variables
            for key, value in env_vars.items():
                if key not in existing_vars:
                    existing_lines.append(f"{key}={value}\n")
            
            # Write back
            with open(self.env_file, 'w', encoding='utf-8') as f:
                f.writelines(existing_lines)
                
        except Exception as e:
            logger.error(f"Error writing .env file: {e}")
            raise
    
    def _create_backup(self):
        """Create backup of .env file"""
        if not self.env_file.exists():
            return
        
        try:
            timestamp = datetime.now().strftime('%Y%m%d_%H%M%S')
            backup_file = self.backup_dir / f'.env.backup.{timestamp}'
            shutil.copy2(self.env_file, backup_file)
            logger.debug(f"Created .env backup: {backup_file}")
        except Exception as e:
            logger.warning(f"Failed to create .env backup: {e}")
    
    def exists(self) -> bool:
        """Check if .env file exists"""
        return self.env_file.exists()
    
    def create_from_example(self, example_file: str = '.env.example'):
        """
        Create .env file from example
        
        Args:
            example_file: Path to example file
        """
        example_path = Path(example_file)
        if not example_path.exists():
            logger.warning(f"Example file not found: {example_file}")
            return False
        
        if self.env_file.exists():
            logger.warning(".env file already exists. Not overwriting.")
            return False
        
        try:
            shutil.copy2(example_path, self.env_file)
            logger.info(f"Created .env from {example_file}")
            return True
        except Exception as e:
            logger.error(f"Failed to create .env from example: {e}")
            return False
    
    def validate_key(self, key: str) -> bool:
        """
        Validate environment variable key name
        
        Args:
            key: Variable name to validate
            
        Returns:
            True if valid
        """
        if not key or not isinstance(key, str):
            return False
        
        # Environment variable keys should be alphanumeric with underscores
        return bool(key.replace('_', '').replace('-', '').isalnum() and not key[0].isdigit())


# Singleton instance
_env_manager: Optional[EnvManager] = None


def get_env_manager(env_file_path: str = '.env') -> EnvManager:
    """
    Get or create EnvManager singleton instance
    
    Args:
        env_file_path: Path to .env file
        
    Returns:
        EnvManager instance
    """
    global _env_manager
    
    if _env_manager is None:
        _env_manager = EnvManager(env_file_path)
    
    return _env_manager
