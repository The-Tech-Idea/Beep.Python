"""
Industry Modules Package
Provides specialized ML workflows for different industries
"""

from .base_module import IndustryModule, ModuleRegistry

# Global module registry
module_registry = ModuleRegistry()

def get_available_modules():
    """Get list of all available industry modules"""
    return module_registry.get_all_modules()

def get_module(module_id: str):
    """Get a specific module by ID"""
    return module_registry.get_module(module_id)

def register_module(module: IndustryModule):
    """Register a new industry module"""
    module_registry.register(module)

# Auto-discover and register modules
def _auto_register_modules():
    """Automatically discover and register all industry modules"""
    import importlib
    import os
    from pathlib import Path
    
    modules_dir = Path(__file__).parent
    
    for item in modules_dir.iterdir():
        if item.is_dir() and not item.name.startswith('_'):
            try:
                # Try to import the module
                module = importlib.import_module(f'.{item.name}', package='app.industry_modules')
                if hasattr(module, 'register'):
                    module.register(module_registry)
            except ImportError as e:
                print(f"Warning: Could not load industry module '{item.name}': {e}")

# Register modules on import
_auto_register_modules()

