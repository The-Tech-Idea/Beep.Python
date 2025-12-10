"""
Migration Helper for LLM Models

Helps migrate existing models to use dedicated virtual environments.
Provides bulk operations and health checks.
"""
import os
from typing import List, Dict, Any, Optional
from pathlib import Path
from datetime import datetime

from app.services.llm_manager import LLMManager
from app.services.llm_environment import get_llm_env_manager
from app.services.environment_manager import EnvironmentManager
from app.services.model_recommendation import get_recommendation_service


class ModelMigrationHelper:
    """Helper for migrating models to per-model venvs"""
    
    def __init__(self):
        self.llm_manager = LLMManager()
        self.llm_env_mgr = get_llm_env_manager()
        # Use app's own folder - no fallback to user home
        from app.config_manager import get_app_directory
        self.env_mgr = EnvironmentManager(
            base_path=str(get_app_directory())
        )
        self.rec_service = get_recommendation_service()
    
    def get_migration_status(self) -> Dict[str, Any]:
        """
        Get overall migration status
        
        Returns:
            Dict with counts and lists of models by status
        """
        all_models = self.llm_manager.get_local_models()
        
        models_with_venv = []
        models_without_venv = []
        models_with_broken_venv = []
        
        for model in all_models:
            if model.venv_name:
                # Check if venv exists and is healthy
                venv_path = self.llm_env_mgr.get_venv_path_for_model(model.id)
                if venv_path and Path(venv_path).exists():
                    # Check if llama-cpp-python is installed
                    venv_info = next(
                        (v for v in self.env_mgr.list_environments() 
                         if v.path == venv_path),
                        None
                    )
                    if venv_info:
                        has_llama, version, backend = self.llm_env_mgr._check_venv_llama_cpp(
                            venv_info.python_executable
                        )
                        if has_llama:
                            models_with_venv.append({
                                'model': model,
                                'venv_name': model.venv_name,
                                'venv_path': venv_path,
                                'gpu_backend': model.gpu_backend,
                                'llama_version': version,
                                'status': 'healthy'
                            })
                        else:
                            models_with_broken_venv.append({
                                'model': model,
                                'venv_name': model.venv_name,
                                'venv_path': venv_path,
                                'issue': 'llama-cpp-python not installed'
                            })
                    else:
                        models_with_broken_venv.append({
                            'model': model,
                            'venv_name': model.venv_name,
                            'venv_path': venv_path,
                            'issue': 'venv not found in environment manager'
                        })
                else:
                    models_with_broken_venv.append({
                        'model': model,
                        'venv_name': model.venv_name,
                        'issue': 'venv path does not exist'
                    })
            else:
                models_without_venv.append(model)
        
        return {
            'total_models': len(all_models),
            'with_venv': len(models_with_venv),
            'without_venv': len(models_without_venv),
            'broken_venv': len(models_with_broken_venv),
            'migration_needed': len(models_without_venv) > 0 or len(models_with_broken_venv) > 0,
            'models_with_venv': models_with_venv,
            'models_without_venv': models_without_venv,
            'models_with_broken_venv': models_with_broken_venv
        }
    
    def get_recommended_backend_for_model(self, model_id: str) -> str:
        """Get recommended GPU backend for a model"""
        hardware = self.rec_service.detect_hardware()
        return self.rec_service.get_recommended_backend(hardware)
    
    def generate_migration_plan(self) -> Dict[str, Any]:
        """
        Generate a migration plan for all models without venvs
        
        Returns:
            Dict with migration plan details
        """
        status = self.get_migration_status()
        
        if not status['migration_needed']:
            return {
                'needed': False,
                'message': 'All models have healthy virtual environments'
            }
        
        # Get recommended backend
        recommended_backend = self.get_recommended_backend_for_model('')
        
        migration_items = []
        
        # Models without venv
        for model in status['models_without_venv']:
            venv_name = self._generate_venv_name(model.name)
            migration_items.append({
                'model_id': model.id,
                'model_name': model.name,
                'action': 'create_venv',
                'venv_name': venv_name,
                'gpu_backend': recommended_backend,
                'estimated_time_minutes': 5
            })
        
        # Models with broken venv
        for item in status['models_with_broken_venv']:
            model = item['model']
            migration_items.append({
                'model_id': model.id,
                'model_name': model.name,
                'action': 'repair_venv',
                'venv_name': model.venv_name,
                'gpu_backend': model.gpu_backend or recommended_backend,
                'issue': item['issue'],
                'estimated_time_minutes': 3
            })
        
        total_time = sum(item['estimated_time_minutes'] for item in migration_items)
        
        return {
            'needed': True,
            'total_models': len(migration_items),
            'total_estimated_minutes': total_time,
            'recommended_backend': recommended_backend,
            'items': migration_items
        }
    
    def _generate_venv_name(self, model_name: str) -> str:
        """Generate venv name from model name"""
        return 'llm-' + model_name.lower()\
            .replace(' ', '-')\
            .replace('_', '-')\
            .replace('[', '')\
            .replace(']', '')\
            .replace('(', '')\
            .replace(')', '')[:50]
    
    def check_venv_health(self, model_id: str) -> Dict[str, Any]:
        """
        Check health of a model's virtual environment
        
        Returns:
            Dict with health status and details
        """
        model = self.llm_manager.get_model_by_id(model_id)
        if not model:
            return {'healthy': False, 'error': 'Model not found'}
        
        if not model.venv_name:
            return {
                'healthy': False,
                'has_venv': False,
                'message': 'Model has no virtual environment assigned'
            }
        
        venv_path = self.llm_env_mgr.get_venv_path_for_model(model_id)
        
        if not venv_path or not Path(venv_path).exists():
            return {
                'healthy': False,
                'has_venv': True,
                'venv_name': model.venv_name,
                'issue': 'Virtual environment path does not exist',
                'venv_path': venv_path
            }
        
        # Get venv info
        venv_info = next(
            (v for v in self.env_mgr.list_environments() if v.path == venv_path),
            None
        )
        
        if not venv_info:
            return {
                'healthy': False,
                'has_venv': True,
                'venv_name': model.venv_name,
                'issue': 'Virtual environment not found in environment manager',
                'venv_path': venv_path
            }
        
        # Check llama-cpp-python
        has_llama, version, backend = self.llm_env_mgr._check_venv_llama_cpp(
            venv_info.python_executable
        )
        
        if not has_llama:
            return {
                'healthy': False,
                'has_venv': True,
                'venv_name': model.venv_name,
                'venv_path': venv_path,
                'issue': 'llama-cpp-python not installed in virtual environment',
                'python_version': venv_info.python_version
            }
        
        # All checks passed
        return {
            'healthy': True,
            'has_venv': True,
            'venv_name': model.venv_name,
            'venv_path': venv_path,
            'python_version': venv_info.python_version,
            'llama_cpp_version': version,
            'gpu_backend': backend,
            'size_mb': venv_info.size_mb,
            'packages_count': venv_info.packages_count
        }
    
    def get_orphaned_venvs(self) -> List[Dict[str, Any]]:
        """
        Find virtual environments that are not associated with any model
        
        Returns:
            List of orphaned venv info
        """
        # Get all LLM-capable venvs
        llm_venvs = self.llm_env_mgr.get_llm_capable_environments()
        
        # Get all models
        all_models = self.llm_manager.get_local_models()
        model_venv_names = {m.venv_name for m in all_models if m.venv_name}
        
        orphaned = []
        for venv in llm_venvs:
            if venv['name'] not in model_venv_names:
                orphaned.append({
                    'venv_name': venv['name'],
                    'venv_path': venv['path'],
                    'llama_cpp_version': venv['llama_cpp_version'],
                    'gpu_backend': venv['gpu_backend'],
                    'size_mb': venv['size_mb'],
                    'created_at': venv.get('created_at'),
                    'can_delete': True
                })
        
        return orphaned
    
    def cleanup_orphaned_venvs(self) -> Dict[str, Any]:
        """
        Delete all orphaned virtual environments
        
        Returns:
            Dict with cleanup results
        """
        orphaned = self.get_orphaned_venvs()
        
        if not orphaned:
            return {
                'success': True,
                'deleted_count': 0,
                'message': 'No orphaned environments found'
            }
        
        deleted = []
        failed = []
        
        for venv in orphaned:
            try:
                success = self.env_mgr.delete_environment(venv['venv_name'])
                if success:
                    deleted.append(venv['venv_name'])
                else:
                    failed.append({
                        'venv_name': venv['venv_name'],
                        'error': 'Delete operation returned False'
                    })
            except Exception as e:
                failed.append({
                    'venv_name': venv['venv_name'],
                    'error': str(e)
                })
        
        return {
            'success': len(failed) == 0,
            'deleted_count': len(deleted),
            'failed_count': len(failed),
            'deleted': deleted,
            'failed': failed
        }


def get_migration_helper() -> ModelMigrationHelper:
    """Get singleton migration helper instance"""
    if not hasattr(get_migration_helper, '_instance'):
        get_migration_helper._instance = ModelMigrationHelper()
    return get_migration_helper._instance
