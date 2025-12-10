"""
Backend Extensions Routes

Manages backend engines and frameworks (similar to LM Studio)
"""
import threading
from flask import Blueprint, render_template, request, jsonify
from app.services.backend_extension_service import get_backend_extension_service
from app.services.task_manager import TaskManager

backend_ext_bp = Blueprint('backend_extensions', __name__)


@backend_ext_bp.route('/')
def index():
    """Backend Extensions Manager UI"""
    # Check if we should auto-install a toolkit from query parameter
    install_backend = request.args.get('install')
    return render_template('llm/backend_extensions.html', install_backend=install_backend)


@backend_ext_bp.route('/api/extensions', methods=['GET'])
def api_list_extensions():
    """Get all available extensions"""
    service = get_backend_extension_service()
    
    # Get filter parameters
    filter_type = request.args.get('type')  # 'all', 'engines', 'frameworks'
    filter_installed = request.args.get('installed')  # 'true', 'false', 'all'
    filter_compatible = request.args.get('compatible')  # 'true', 'false', 'all'
    
    if filter_type == 'engines':
        extensions = service.get_engines()
    elif filter_type == 'frameworks':
        extensions = service.get_frameworks()
    else:
        extensions = service.get_available_extensions()
    
    # Filter by installed
    if filter_installed == 'true':
        extensions = [e for e in extensions if e.installed]
    elif filter_installed == 'false':
        extensions = [e for e in extensions if not e.installed]
    
    # Filter by compatible
    if filter_compatible == 'true':
        extensions = [e for e in extensions if e.compatible]
    elif filter_compatible == 'false':
        extensions = [e for e in extensions if not e.compatible]
    
    return jsonify({
        'success': True,
        'extensions': [ext.to_dict() for ext in extensions]
    })


@backend_ext_bp.route('/api/extensions/compatible', methods=['GET'])
def api_compatible_extensions():
    """Get compatible extensions (installed)"""
    service = get_backend_extension_service()
    extensions = service.get_compatible_extensions()
    
    return jsonify({
        'success': True,
        'extensions': [ext.to_dict() for ext in extensions]
    })


@backend_ext_bp.route('/api/extensions/installed', methods=['GET'])
def api_installed_extensions():
    """Get installed extensions"""
    service = get_backend_extension_service()
    extensions = service.get_installed_extensions()
    
    return jsonify({
        'success': True,
        'extensions': [ext.to_dict() for ext in extensions]
    })


@backend_ext_bp.route('/api/extensions/<extension_id>/install', methods=['POST'])
def api_install_extension(extension_id: str):
    """
    Install an extension in an existing virtual environment
    
    Note: Backend extensions should be installed in model-specific environments,
    not standalone backend environments. This matches LM Studio's approach where
    backends are part of the model's runtime environment.
    """
    data = request.get_json() or {}
    venv_name = data.get('venv_name')
    model_id = data.get('model_id')
    
    if not venv_name:
        return jsonify({
            'success': False,
            'error': 'venv_name is required. Backend extensions must be installed in an existing model environment. Create the environment first via the model setup wizard.'
        }), 400
    
    service = get_backend_extension_service()
    task_mgr = TaskManager()
    
    # Get extension info
    extensions = service.get_available_extensions()
    extension = next((e for e in extensions if e.id == extension_id), None)
    
    if not extension:
        return jsonify({'success': False, 'error': 'Extension not found'}), 404
    
    # Create task
    task = task_mgr.create_task(
        name=f"Install {extension.name}",
        task_type="install_extension",
        steps=[
            "Preparing environment",
            f"Installing {extension.name}",
            "Verifying installation"
        ]
    )
    
    def run_install():
        try:
            task_mgr.start_task(task.id)
            
            # Step 1: Prepare
            task_mgr.update_step(task.id, 0, "running", "Preparing environment...")
            task_mgr.update_progress(task.id, 10, "Preparing...")
            
            # Step 2: Install
            task_mgr.update_step(task.id, 1, "running", f"Installing {extension.name}...")
            task_mgr.update_progress(task.id, 20, "This may take several minutes...")
            
            result = service.install_extension(extension_id, venv_name, model_id)
            
            if not result.get('success'):
                task_mgr.fail_task(task.id, result.get('error', 'Installation failed'))
                return
            
            task_mgr.update_step(task.id, 1, "completed", f"Installed {extension.name}")
            task_mgr.update_progress(task.id, 90, "Installation complete")
            
            # Step 3: Verify
            task_mgr.update_step(task.id, 2, "running", "Verifying installation...")
            
            # Refresh extensions to check installation
            service._get_installed_extensions.cache_clear() if hasattr(service._get_installed_extensions, 'cache_clear') else None
            
            task_mgr.update_step(task.id, 2, "completed", "Verification complete")
            task_mgr.update_progress(task.id, 100, "Complete!")
            
            task_mgr.complete_task(task.id, result={
                'extension_id': extension_id,
                'extension_name': extension.name,
                'venv_name': result.get('venv_name'),
                'message': result.get('message', 'Installation successful')
            })
            
        except Exception as e:
            import traceback
            traceback.print_exc()
            task_mgr.fail_task(task.id, str(e))
    
    thread = threading.Thread(target=run_install, daemon=True)
    thread.start()
    
    return jsonify({
        'success': True,
        'task_id': task.id,
        'message': f'Installing {extension.name}...',
        'redirect_url': f'/tasks/{task.id}'
    })


@backend_ext_bp.route('/api/extensions/<extension_id>/uninstall', methods=['POST'])
def api_uninstall_extension(extension_id: str):
    """Uninstall an extension from an environment"""
    data = request.get_json() or {}
    venv_name = data.get('venv_name')
    
    if not venv_name:
        return jsonify({'success': False, 'error': 'venv_name is required'}), 400
    
    service = get_backend_extension_service()
    task_mgr = TaskManager()
    
    # Get extension info
    extensions = service.get_available_extensions()
    extension = next((e for e in extensions if e.id == extension_id), None)
    
    if not extension:
        return jsonify({'success': False, 'error': 'Extension not found'}), 404
    
    # Create task
    task = task_mgr.create_task(
        name=f"Uninstall {extension.name}",
        task_type="uninstall_extension",
        steps=[
            f"Uninstalling {extension.name} from {venv_name}",
            "Verifying removal"
        ]
    )
    
    def run_uninstall():
        try:
            task_mgr.start_task(task.id)
            
            # Step 1: Uninstall
            task_mgr.update_step(task.id, 0, "running", f"Uninstalling {extension.name}...")
            task_mgr.update_progress(task.id, 50, "Uninstalling...")
            
            result = service.uninstall_extension(extension_id, venv_name)
            
            if not result.get('success'):
                task_mgr.fail_task(task.id, result.get('error', 'Uninstallation failed'))
                return
            
            task_mgr.update_step(task.id, 0, "completed", f"Uninstalled {extension.name}")
            task_mgr.update_progress(task.id, 90, "Uninstallation complete")
            
            # Step 2: Verify
            task_mgr.update_step(task.id, 1, "running", "Verifying removal...")
            task_mgr.update_step(task.id, 1, "completed", "Verification complete")
            task_mgr.update_progress(task.id, 100, "Complete!")
            
            task_mgr.complete_task(task.id, result={
                'extension_id': extension_id,
                'extension_name': extension.name,
                'venv_name': venv_name,
                'message': result.get('message', 'Uninstallation successful')
            })
            
        except Exception as e:
            import traceback
            traceback.print_exc()
            task_mgr.fail_task(task.id, str(e))
    
    thread = threading.Thread(target=run_uninstall, daemon=True)
    thread.start()
    
    return jsonify({
        'success': True,
        'task_id': task.id,
        'message': f'Uninstalling {extension.name}...',
        'redirect_url': f'/tasks/{task.id}'
    })


@backend_ext_bp.route('/api/extensions/refresh', methods=['POST'])
def api_refresh_extensions():
    """Refresh extension status (re-scan environments and re-detect SDKs)"""
    import os
    from pathlib import Path
    from app.services.environment_manager import EnvironmentManager
    from app.config_manager import get_app_directory
    
    service = get_backend_extension_service()
    
    # Clear the toolkit cache and re-detect all SDKs
    base_path = get_app_directory()
    EnvironmentManager.clear_toolkit_cache(base_path)
    
    # Re-detect all backends (will populate fresh cache)
    env_mgr = EnvironmentManager(base_path=str(base_path))
    toolkit_status = env_mgr.detect_all_backends(force_refresh=True)
    
    # Get updated extensions
    extensions = service.get_available_extensions()
    
    return jsonify({
        'success': True,
        'message': 'Extensions and SDK detection refreshed',
        'toolkit_status': toolkit_status,
        'extensions': [ext.to_dict() for ext in extensions]
    })


@backend_ext_bp.route('/api/extensions/check-updates', methods=['GET'])
def api_check_updates():
    """
    Check for available toolkit and extension updates.
    LM Studio style - uses prebuilt llama.cpp backends with bundled libraries.
    Checks all supported backends: CUDA 12/13, Vulkan, HIP, SYCL, Metal, CPU
    """
    import platform
    from app.services.cuda_installer import get_backend_installer
    
    current_platform = platform.system()
    backend_installer = get_backend_installer()
    
    # Get available and installed backends from LlamaBackendInstaller
    available_backends = backend_installer.get_available_backends()
    update_info = backend_installer.check_for_updates()
    
    installed = []
    updates = []
    
    # Process each available backend
    for backend_id, info in available_backends.items():
        backend_info = backend_installer.BACKEND_INFO.get(backend_id, {})
        
        if info.get('installed'):
            installed.append({
                'id': backend_id,
                'name': info.get('name', backend_id),
                'version': info.get('installed_version', 'Installed'),
                'icon': _get_backend_icon(backend_id),
                'status': 'up-to-date',
                'description': info.get('description', ''),
                'path': info.get('install_path'),
            })
        else:
            # Not installed - show as available
            updates.append({
                'id': backend_id,
                'name': info.get('name', backend_id),
                'type': 'backend',
                'current_version': None,
                'latest_version': update_info.get('latest_version', 'Latest'),
                'description': info.get('description', ''),
                'size': info.get('size', 'Unknown'),
                'auto_install': True,  # All backends support silent install now
                'requires_gpu': info.get('requires_gpu', False),
            })
    
    # Check for updates to installed backends
    for update in update_info.get('updates_available', []):
        # Find in installed list and mark as update available
        for item in installed:
            if item['id'] == update['id']:
                item['status'] = 'update-available'
                item['latest_version'] = update['latest_version']
                break
    
    return jsonify({
        'success': True,
        'installed': installed,
        'updates': updates,
        'platform': current_platform,
        'latest_version': update_info.get('latest_version'),
        'checked_at': __import__('datetime').datetime.now().isoformat()
    })


def _get_backend_icon(backend_id: str) -> str:
    """Get Bootstrap icon name for a backend"""
    icons = {
        'cpu': 'cpu',
        'cuda': 'gpu-card',
        'vulkan': 'controller',
        'hip': 'gpu-card',
        'sycl': 'cpu-fill',
        'metal': 'apple',
    }
    return icons.get(backend_id, 'box')


@backend_ext_bp.route('/api/backends', methods=['GET'])
def api_list_backends():
    """Get all available llama.cpp backends for this platform"""
    from app.services.cuda_installer import get_backend_installer
    
    installer = get_backend_installer()
    backends = installer.get_available_backends()
    
    return jsonify({
        'success': True,
        'backends': backends,
        'platform': installer.platform,
        'arch': installer.arch,
        'recommended': installer.get_recommended_backend(),
    })


@backend_ext_bp.route('/api/backends/<backend_id>/install', methods=['POST'])
def api_install_backend(backend_id: str):
    """
    Install a llama.cpp backend (CUDA, Vulkan, HIP, etc.)
    This downloads prebuilt binaries with all required libraries bundled.
    Silent installation - no admin required, no restart needed.
    """
    from app.services.cuda_installer import get_backend_installer
    from app.services.task_manager import TaskManager
    
    installer = get_backend_installer()
    task_mgr = TaskManager()
    
    # Get backend info
    backend_info = installer.BACKEND_INFO.get(backend_id, {})
    backend_name = backend_info.get('name', backend_id)
    
    # Create task
    task = task_mgr.create_task(
        name=f"Install {backend_name}",
        task_type="install_backend",
        steps=["Fetching release info", "Downloading", "Extracting", "Configuring"]
    )
    
    def run_install():
        def progress_callback(percent, message):
            task_mgr.update_progress(task.id, percent, message)
        
        try:
            task_mgr.start_task(task.id)
            task_mgr.update_step(task.id, 0, "running", "Fetching latest release...")
            
            result = installer.install_backend(backend_id, progress_callback)
            
            if result.get('success'):
                task_mgr.update_step(task.id, 0, "completed", "Release found")
                task_mgr.update_step(task.id, 1, "completed", "Downloaded")
                task_mgr.update_step(task.id, 2, "completed", "Extracted")
                task_mgr.update_step(task.id, 3, "completed", "Configured")
                task_mgr.complete_task(task.id, result=result)
            else:
                task_mgr.fail_task(task.id, result.get('error', 'Installation failed'))
        except Exception as e:
            import traceback
            traceback.print_exc()
            task_mgr.fail_task(task.id, str(e))
    
    thread = threading.Thread(target=run_install, daemon=True)
    thread.start()
    
    return jsonify({
        'success': True,
        'task_id': task.id,
        'message': f'Installing {backend_name}...',
        'redirect_url': f'/tasks/{task.id}'
    })


@backend_ext_bp.route('/api/backends/<backend_id>/uninstall', methods=['POST'])
def api_uninstall_backend(backend_id: str):
    """Uninstall a backend"""
    from app.services.cuda_installer import get_backend_installer
    
    installer = get_backend_installer()
    result = installer.uninstall_backend(backend_id)
    
    return jsonify(result)


@backend_ext_bp.route('/api/backends/installed', methods=['GET'])
def api_installed_backends():
    """Get list of installed backends"""
    from app.services.cuda_installer import get_backend_installer
    
    installer = get_backend_installer()
    installed = installer.get_installed_backends()
    
    return jsonify({
        'success': True,
        'backends': installed,
    })

