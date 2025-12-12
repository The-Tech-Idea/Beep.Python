"""
Validation utilities
"""
import re
from typing import Optional


def validate_project_name(name: str) -> tuple[bool, Optional[str]]:
    """
    Validate project name
    
    Args:
        name: Project name to validate
        
    Returns:
        Tuple of (is_valid, error_message)
    """
    if not name or not name.strip():
        return False, "Project name cannot be empty"
    
    if len(name) > 200:
        return False, "Project name must be less than 200 characters"
    
    # Allow alphanumeric, spaces, hyphens, underscores
    if not re.match(r'^[a-zA-Z0-9\s\-_]+$', name):
        return False, "Project name can only contain letters, numbers, spaces, hyphens, and underscores"
    
    return True, None


def sanitize_filename(filename: str) -> str:
    """Sanitize filename for safe storage"""
    # Remove path components
    filename = filename.replace('..', '').replace('/', '').replace('\\', '')
    # Remove leading/trailing whitespace
    filename = filename.strip()
    return filename

