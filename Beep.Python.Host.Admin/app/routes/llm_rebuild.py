"""
LLM Rebuild Route - Handle environment rebuild with GPU backend
"""
import os
import threading
from flask import request, jsonify
from app.services.task_manager import TaskManager
from app.services.llm_manager import LLMManager


def api_rebuild_venv(venv_name: str):
    """Rebuild a virtual environment with a specific backend"""
    data = request.get_json()
    if not data:
        return jsonify({'error': 'JSON body required'}), 400
    
    gpu_backend = data.get('gpu_backend', 'cpu')
    
    task_mgr = TaskManager()
    
    steps = [
        "Preparing environment",
        f"Installing llama-cpp-python ({gpu_backend})",
        "Verifying installation",
        "Updating model associations"
    ]
    
    task = task_mgr.create_task(
        name=f"Rebuild Environment: {venv_name}",
        task_type="rebuild_environment",
        steps=steps
    )
    
    def run_rebuild():
        from app.services.environment_manager import EnvironmentManager
        from app.services.llm_environment import get_llm_env_manager
        
        try:
            task_mgr.start_task(task.id)
            env_mgr = EnvironmentManager(base_path=os.environ.get('BEEP_PYTHON_HOME', 
                                                                   os.path.expanduser('~/.beep-llm')))
            llm_env_mgr = get_llm_env_manager()
            
            # Step 1: Prepare
            task_mgr.update_step(task.id, 0, "running", "Checking environment...")
            task_mgr.update_progress(task.id, 10, "Checking environment...")
            
            # Verify venv exists
            venvs = env_mgr.list_environments()
            if not any(v.name == venv_name for v in venvs):
                task_mgr.fail_task(task.id, f"Environment {venv_name} not found")
                return
            
            task_mgr.update_step(task.id, 0, "completed", "Environment ready")
            
            # Step 2: Install
            task_mgr.update_step(task.id, 1, "running", f"Installing with {gpu_backend} support...")
            task_mgr.update_progress(task.id, 20, "Installing dependencies (this may take several minutes)...")
            
            # Use specialized installer
            install_result = env_mgr.install_llama_cpp_python(venv_name, gpu_backend)
            
            if not install_result.get('success'):
                error_msg = install_result.get('stderr', 'Unknown error')[:500]
                task_mgr.fail_task(task.id, f"Installation failed: {error_msg}")
                return
            
            task_mgr.update_step(task.id, 1, "completed", "Installation successful")
            task_mgr.update_progress(task.id, 80, "Installation complete")
            
            # Step 3: Verify
            task_mgr.update_step(task.id, 2, "running", "Verifying installation...")
            
            # Get python executable path
            target_venv = next(v for v in venvs if v.name == venv_name)
            has_llama, version, detected_backend = llm_env_mgr._check_venv_llama_cpp(target_venv.python_executable)
            
            if not has_llama:
                task_mgr.fail_task(task.id, "Verification failed: llama-cpp-python not found")
                return
            
            task_mgr.update_step(task.id, 2, "completed", f"Verified ({detected_backend or 'cpu'})")
            
            # Step 4: Update associations
            task_mgr.update_step(task.id, 3, "running", "Updating model associations...")
            
            # Unload any currently loaded models using this venv (they need to reload with new GPU settings)
            from app.services.inference_service import InferenceService
            inference = InferenceService()
            models_unloaded = 0
            
            # Find models using this venv
            models_updated = 0
            llm_mgr = LLMManager()
            all_models = llm_mgr.get_local_models()
            models_to_save = []
            
            for model in all_models:
                if model.venv_name == venv_name:
                    # Unload model if it's currently loaded (needs reload with new GPU backend)
                    if inference.is_model_loaded(model.id):
                        inference.unload_model(model.id)
                        models_unloaded += 1
                    
                    # Update association
                    llm_env_mgr.associate_model_with_env(model.id, venv_name, detected_backend or gpu_backend)
                    # Update model metadata
                    model.gpu_backend = detected_backend or gpu_backend
                    models_updated += 1
                models_to_save.append(model)
            
            if models_updated > 0:
                llm_mgr._save_models_index(models_to_save)
            
            task_mgr.update_step(task.id, 3, "completed", f"Updated {models_updated} models")
            task_mgr.update_progress(task.id, 100, "Rebuild complete!")
            
            task_mgr.complete_task(task.id, result={
                'venv_name': venv_name,
                'gpu_backend': detected_backend or gpu_backend,
                'models_updated': models_updated,
                'models_unloaded': models_unloaded,
                'message': f'Rebuild complete! {models_updated} models updated. {models_unloaded} models were unloaded and need to be reloaded to use the new GPU backend.'
            })
            
        except Exception as e:
            task_mgr.fail_task(task.id, str(e))
    
    thread = threading.Thread(target=run_rebuild, daemon=True)
    thread.start()
    
    return jsonify({
        'success': True,
        'task_id': task.id,
        'message': 'Rebuild started'
    })
    thread = threading.Thread(target=run_rebuild, daemon=True)
    thread.start()
    
    return jsonify({
        'success': True,
        'task_id': task.id,
        'message': 'Rebuild started'
    })
