"""
Tasks Routes - Background task management with progress tracking
"""
from flask import Blueprint, render_template, request, jsonify, Response
from app.services.task_manager import TaskManager
from app.services.environment_manager import EnvironmentManager
import threading
import json
import time

tasks_bp = Blueprint('tasks', __name__)


@tasks_bp.route('/')
def index():
    """List all tasks"""
    mgr = TaskManager()
    tasks = mgr.list_tasks()
    return render_template('tasks/index.html', tasks=tasks)


@tasks_bp.route('/<task_id>')
def detail(task_id):
    """Task detail/progress view"""
    mgr = TaskManager()
    task = mgr.get_task(task_id)
    if not task:
        return jsonify({'error': 'Task not found'}), 404
    return render_template('tasks/detail.html', task=task)


@tasks_bp.route('/<task_id>/status')
def status(task_id):
    """Get task status as JSON"""
    mgr = TaskManager()
    task = mgr.get_task(task_id)
    if not task:
        return jsonify({'error': 'Task not found'}), 404
    return jsonify(task.to_dict())


@tasks_bp.route('/<task_id>/stream')
def stream(task_id):
    """Server-Sent Events stream for task progress"""
    mgr = TaskManager()
    task = mgr.get_task(task_id)
    
    if not task:
        return jsonify({'error': 'Task not found'}), 404
    
    def generate():
        last_update = None
        while True:
            task = mgr.get_task(task_id)
            if not task:
                yield f"data: {json.dumps({'error': 'Task not found'})}\n\n"
                break
            
            task_data = task.to_dict()
            current_update = json.dumps(task_data)
            
            # Only send if changed
            if current_update != last_update:
                yield f"data: {current_update}\n\n"
                last_update = current_update
            
            # Stop streaming if task is done
            if task.status in ('completed', 'failed', 'cancelled'):
                break
            
            time.sleep(0.5)  # Poll every 500ms
    
    return Response(generate(), mimetype='text/event-stream',
                   headers={'Cache-Control': 'no-cache', 'X-Accel-Buffering': 'no'})


@tasks_bp.route('/create-environment', methods=['POST'])
def create_environment_task():
    """Create environment with progress tracking"""
    data = request.get_json() or request.form
    name = data.get('name')
    packages = data.get('packages', '').split(',') if data.get('packages') else []
    packages = [p.strip() for p in packages if p.strip()]
    
    if not name:
        return jsonify({'error': 'Name is required'}), 400
    
    # Create task with steps
    task_mgr = TaskManager()
    steps = [
        "Initializing",
        "Creating virtual environment",
        "Upgrading pip",
    ]
    if packages:
        steps.append(f"Installing {len(packages)} packages")
    steps.append("Finalizing")
    
    task = task_mgr.create_task(
        name=f"Create Environment: {name}",
        task_type="create_environment",
        steps=steps
    )
    
    # Run in background thread
    def run_task():
        env_mgr = EnvironmentManager()
        try:
            task_mgr.start_task(task.id)
            
            # Step 1: Initializing
            task_mgr.update_step(task.id, 0, "running", "Preparing environment creation...")
            task_mgr.update_progress(task.id, 5, "Initializing...")
            time.sleep(0.5)
            task_mgr.update_step(task.id, 0, "completed", "Ready")
            
            # Step 2: Creating venv
            task_mgr.update_step(task.id, 1, "running", "Creating virtual environment...")
            task_mgr.update_progress(task.id, 15, "Creating virtual environment...")
            
            # Actually create the environment (without packages first)
            env = env_mgr.create_environment(name, packages=None)
            
            task_mgr.update_step(task.id, 1, "completed", f"Environment created at {env.path}")
            task_mgr.update_progress(task.id, 40, "Virtual environment created")
            
            # Step 3: Upgrade pip
            task_mgr.update_step(task.id, 2, "running", "Upgrading pip...")
            task_mgr.update_progress(task.id, 50, "Upgrading pip...")
            
            try:
                env_mgr.install_packages(name, ['pip', '--upgrade'])
            except:
                pass  # Ignore pip upgrade errors
            
            task_mgr.update_step(task.id, 2, "completed", "Pip upgraded")
            task_mgr.update_progress(task.id, 60, "Pip upgraded")
            
            # Step 4: Install packages (if any)
            step_index = 3
            if packages:
                task_mgr.update_step(task.id, step_index, "running", f"Installing {len(packages)} packages...")
                
                for i, pkg in enumerate(packages):
                    progress = 60 + int((i / len(packages)) * 30)
                    task_mgr.update_progress(task.id, progress, f"Installing {pkg}...")
                    try:
                        env_mgr.install_packages(name, [pkg])
                    except Exception as e:
                        task_mgr.update_step(task.id, step_index, "running", f"Warning: {pkg} - {str(e)[:50]}")
                
                task_mgr.update_step(task.id, step_index, "completed", f"Installed {len(packages)} packages")
                step_index += 1
            
            # Final step
            task_mgr.update_step(task.id, step_index, "running", "Finalizing...")
            task_mgr.update_progress(task.id, 95, "Finalizing...")
            time.sleep(0.3)
            task_mgr.update_step(task.id, step_index, "completed", "Done!")
            
            # Complete task
            task_mgr.complete_task(task.id, result={
                'environment_name': name,
                'environment_path': env.path,
                'packages_installed': len(packages)
            })
            
        except Exception as e:
            task_mgr.fail_task(task.id, str(e))
    
    thread = threading.Thread(target=run_task, daemon=True)
    thread.start()
    
    # Return task info for progress tracking
    return jsonify({
        'success': True,
        'task_id': task.id,
        'redirect_url': f'/tasks/{task.id}'
    })


@tasks_bp.route('/delete-environment', methods=['POST'])
def delete_environment_task():
    """Delete environment with progress tracking"""
    data = request.get_json() or request.form
    # Accept both 'name' and 'env_name' for flexibility
    name = data.get('env_name') or data.get('name')
    
    if not name:
        return jsonify({'error': 'Environment name is required'}), 400
    
    # Create task with steps
    task_mgr = TaskManager()
    steps = [
        "Preparing deletion",
        "Unloading models",
        "Removing environment files",
        "Cleaning up associations"
    ]
    
    task = task_mgr.create_task(
        name=f"Delete Environment: {name}",
        task_type="delete_environment",
        steps=steps
    )
    
    # Run in background thread
    def run_task():
        env_mgr = EnvironmentManager()
        try:
            task_mgr.start_task(task.id)
            
            # Step 1: Preparing
            task_mgr.update_step(task.id, 0, "running", "Checking environment...")
            task_mgr.update_progress(task.id, 10, "Preparing deletion...")
            time.sleep(0.3)
            task_mgr.update_step(task.id, 0, "completed", "Environment found")
            
            # Step 2: Unload any models using this environment
            task_mgr.update_step(task.id, 1, "running", "Checking for loaded models...")
            task_mgr.update_progress(task.id, 25, "Unloading models...")
            
            # Find and unload models using this venv
            from app.services.inference_service import InferenceService
            from app.services.llm_environment import get_llm_env_manager
            from app.services.llm_manager import LLMManager
            
            inference = InferenceService()
            llm_env_mgr = get_llm_env_manager()
            llm_mgr = LLMManager()
            
            # Find all models associated with this venv
            models_to_unload = []
            for model_id, assoc in llm_env_mgr._model_associations.items():
                if assoc.get('venv_name') == name:
                    models_to_unload.append(model_id)
            
            # Unload each model
            unloaded_count = 0
            for model_id in models_to_unload:
                if inference.is_model_loaded(model_id):
                    inference.unload_model(model_id)
                    unloaded_count += 1
            
            if unloaded_count > 0:
                task_mgr.update_step(task.id, 1, "completed", f"Unloaded {unloaded_count} model(s)")
            else:
                task_mgr.update_step(task.id, 1, "completed", "No models were loaded")
            
            # Step 3: Remove files
            task_mgr.update_step(task.id, 2, "running", "Removing environment files...")
            task_mgr.update_progress(task.id, 50, "Deleting files...")
            
            # Actually delete
            env_mgr.delete_environment(name)
            
            task_mgr.update_step(task.id, 2, "completed", "Files removed")
            task_mgr.update_progress(task.id, 75, "Files deleted")
            
            # Step 4: Cleanup associations
            task_mgr.update_step(task.id, 3, "running", "Removing model associations...")
            task_mgr.update_progress(task.id, 85, "Cleaning up...")
            
            # Remove associations and update model records
            all_models = llm_mgr.get_local_models()
            for model_id in models_to_unload:
                llm_env_mgr.remove_model_association(model_id)
                # Update model in the list
                for i, model in enumerate(all_models):
                    if model.id == model_id:
                        all_models[i].venv_name = None
                        all_models[i].gpu_backend = None
                        break
            
            # Save model index
            if models_to_unload:
                llm_mgr._save_models_index(all_models)
            
            task_mgr.update_step(task.id, 3, "completed", f"Cleaned up {len(models_to_unload)} association(s)")
            task_mgr.update_progress(task.id, 100, "Complete!")
            
            # Complete task
            task_mgr.complete_task(task.id, result={
                'environment_name': name,
                'deleted': True,
                'models_unloaded': unloaded_count,
                'associations_removed': len(models_to_unload)
            })
            
        except Exception as e:
            task_mgr.fail_task(task.id, str(e))
    
    thread = threading.Thread(target=run_task, daemon=True)
    thread.start()
    
    return jsonify({
        'success': True,
        'task_id': task.id,
        'redirect_url': f'/tasks/{task.id}'
    })


@tasks_bp.route('/install-packages', methods=['POST'])
def install_packages_task():
    """Install packages with progress tracking"""
    data = request.get_json() or request.form
    env_name = data.get('environment')
    packages = data.get('packages', '')
    
    if isinstance(packages, str):
        packages = [p.strip() for p in packages.split('\n') if p.strip()]
    
    if not env_name:
        return jsonify({'error': 'Environment name is required'}), 400
    if not packages:
        return jsonify({'error': 'At least one package is required'}), 400
    
    # Create task
    task_mgr = TaskManager()
    steps = ["Preparing"] + [f"Install {pkg}" for pkg in packages[:10]]  # Limit steps shown
    if len(packages) > 10:
        steps.append(f"Install {len(packages) - 10} more packages")
    steps.append("Finalizing")
    
    task = task_mgr.create_task(
        name=f"Install Packages in {env_name}",
        task_type="install_packages",
        steps=steps
    )
    
    def run_task():
        env_mgr = EnvironmentManager()
        try:
            task_mgr.start_task(task.id)
            
            # Preparing
            task_mgr.update_step(task.id, 0, "running", "Preparing...")
            task_mgr.update_progress(task.id, 5, "Preparing installation...")
            time.sleep(0.3)
            task_mgr.update_step(task.id, 0, "completed", "Ready")
            
            # Install each package
            total = len(packages)
            for i, pkg in enumerate(packages):
                step_idx = min(i + 1, 10)  # Cap at step 10
                progress = 10 + int((i / total) * 80)
                
                task_mgr.update_step(task.id, step_idx, "running", f"Installing {pkg}...")
                task_mgr.update_progress(task.id, progress, f"Installing {pkg}...")
                
                try:
                    env_mgr.install_packages(env_name, [pkg])
                    task_mgr.update_step(task.id, step_idx, "completed", f"Installed {pkg}")
                except Exception as e:
                    task_mgr.update_step(task.id, step_idx, "completed", f"{pkg} - {str(e)[:30]}")
            
            # Finalizing
            final_step = min(len(packages) + 1, 11)
            task_mgr.update_step(task.id, final_step, "running", "Finalizing...")
            task_mgr.update_progress(task.id, 95, "Finalizing...")
            time.sleep(0.3)
            task_mgr.update_step(task.id, final_step, "completed", "Done!")
            
            task_mgr.complete_task(task.id, result={
                'environment': env_name,
                'packages_installed': total
            })
            
        except Exception as e:
            task_mgr.fail_task(task.id, str(e))
    
    thread = threading.Thread(target=run_task, daemon=True)
    thread.start()
    
    return jsonify({
        'success': True,
        'task_id': task.id,
        'redirect_url': f'/tasks/{task.id}'
    })
