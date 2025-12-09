"""
REST API Routes - JSON API for programmatic access
"""
from flask import Blueprint, request, jsonify
from app.services.runtime_manager import RuntimeManager
from app.services.environment_manager import EnvironmentManager
from app.services.server_manager import ServerManager
from dataclasses import asdict

api_bp = Blueprint('api', __name__)


# =============================================================================
# Runtimes API
# =============================================================================

@api_bp.route('/runtimes', methods=['GET'])
def list_runtimes():
    """List all Python runtimes"""
    mgr = RuntimeManager()
    runtimes = mgr.discover_runtimes()
    return jsonify({
        'success': True,
        'data': mgr.to_dict_list(runtimes)
    })


@api_bp.route('/runtimes/<runtime_id>', methods=['GET'])
def get_runtime(runtime_id):
    """Get runtime details"""
    mgr = RuntimeManager()
    runtime = mgr.get_runtime_info(runtime_id)
    if runtime:
        return jsonify({'success': True, 'data': runtime})
    return jsonify({'success': False, 'error': 'Runtime not found'}), 404


@api_bp.route('/runtimes/install-embedded', methods=['POST'])
def install_embedded():
    """Install embedded Python"""
    mgr = RuntimeManager()
    try:
        mgr.install_embedded_python()
        return jsonify({'success': True, 'message': 'Embedded Python installed'})
    except Exception as e:
        return jsonify({'success': False, 'error': str(e)}), 500


# =============================================================================
# Environments API
# =============================================================================

@api_bp.route('/environments', methods=['GET'])
def list_environments():
    """List all virtual environments"""
    mgr = EnvironmentManager()
    environments = mgr.list_environments()
    return jsonify({
        'success': True,
        'data': mgr.to_dict_list(environments)
    })


@api_bp.route('/environments', methods=['POST'])
def create_environment():
    """Create a new virtual environment"""
    data = request.get_json()
    name = data.get('name')
    packages = data.get('packages', [])
    python_executable = data.get('python_executable')
    
    if not name:
        return jsonify({'success': False, 'error': 'Name is required'}), 400
    
    mgr = EnvironmentManager()
    try:
        env = mgr.create_environment(name, python_executable, packages)
        return jsonify({'success': True, 'data': asdict(env)}), 201
    except Exception as e:
        return jsonify({'success': False, 'error': str(e)}), 500


@api_bp.route('/environments/<env_id>', methods=['GET'])
def get_environment(env_id):
    """Get environment details"""
    mgr = EnvironmentManager()
    environments = mgr.list_environments()
    env = next((e for e in environments if e.id == env_id), None)
    
    if env:
        return jsonify({'success': True, 'data': asdict(env)})
    return jsonify({'success': False, 'error': 'Environment not found'}), 404


@api_bp.route('/environments/<env_id>', methods=['DELETE'])
def delete_environment(env_id):
    """Delete a virtual environment"""
    mgr = EnvironmentManager()
    try:
        mgr.delete_environment(env_id)
        return jsonify({'success': True, 'message': 'Environment deleted'})
    except Exception as e:
        return jsonify({'success': False, 'error': str(e)}), 500


@api_bp.route('/environments/<env_id>/packages', methods=['GET'])
def list_packages(env_id):
    """List packages in environment"""
    mgr = EnvironmentManager()
    try:
        packages = mgr.get_packages(env_id)
        return jsonify({
            'success': True,
            'data': [asdict(p) for p in packages]
        })
    except Exception as e:
        return jsonify({'success': False, 'error': str(e)}), 500


@api_bp.route('/environments/<env_id>/packages', methods=['POST'])
def install_packages(env_id):
    """Install packages in environment"""
    data = request.get_json()
    packages = data.get('packages', [])
    
    if not packages:
        return jsonify({'success': False, 'error': 'Packages list is required'}), 400
    
    mgr = EnvironmentManager()
    try:
        result = mgr.install_packages(env_id, packages)
        return jsonify({
            'success': result['success'],
            'stdout': result['stdout'],
            'stderr': result['stderr']
        })
    except Exception as e:
        return jsonify({'success': False, 'error': str(e)}), 500


@api_bp.route('/environments/<env_id>/packages/<package>', methods=['DELETE'])
def uninstall_package(env_id, package):
    """Uninstall a package from environment"""
    mgr = EnvironmentManager()
    try:
        result = mgr.uninstall_package(env_id, package)
        return jsonify({
            'success': result['success'],
            'stdout': result['stdout'],
            'stderr': result['stderr']
        })
    except Exception as e:
        return jsonify({'success': False, 'error': str(e)}), 500


@api_bp.route('/environments/<env_id>/requirements', methods=['GET'])
def export_requirements(env_id):
    """Export requirements.txt"""
    mgr = EnvironmentManager()
    try:
        requirements = mgr.export_requirements(env_id)
        return jsonify({'success': True, 'data': requirements})
    except Exception as e:
        return jsonify({'success': False, 'error': str(e)}), 500


@api_bp.route('/environments/<env_id>/requirements', methods=['POST'])
def import_requirements(env_id):
    """Import requirements.txt"""
    data = request.get_json()
    requirements = data.get('requirements', '')
    
    mgr = EnvironmentManager()
    try:
        result = mgr.import_requirements(env_id, requirements)
        return jsonify({
            'success': result['success'],
            'stdout': result['stdout'],
            'stderr': result['stderr']
        })
    except Exception as e:
        return jsonify({'success': False, 'error': str(e)}), 500


# =============================================================================
# Servers API
# =============================================================================

@api_bp.route('/servers', methods=['GET'])
def list_servers():
    """List all servers"""
    mgr = ServerManager()
    servers = mgr.list_servers()
    return jsonify({
        'success': True,
        'data': mgr.to_dict_list(servers)
    })


@api_bp.route('/servers', methods=['POST'])
def start_server():
    """Start a new server"""
    data = request.get_json()
    name = data.get('name')
    venv_path = data.get('venv_path')
    backend_type = data.get('backend_type', 'http')
    port = data.get('port')
    
    if not name or not venv_path:
        return jsonify({'success': False, 'error': 'Name and venv_path are required'}), 400
    
    mgr = ServerManager()
    try:
        server = mgr.start_server(name, venv_path, backend_type, port)
        return jsonify({'success': True, 'data': asdict(server)}), 201
    except Exception as e:
        return jsonify({'success': False, 'error': str(e)}), 500


@api_bp.route('/servers/<server_id>', methods=['GET'])
def get_server(server_id):
    """Get server details"""
    mgr = ServerManager()
    server = mgr.get_server(server_id)
    if server:
        return jsonify({'success': True, 'data': asdict(server)})
    return jsonify({'success': False, 'error': 'Server not found'}), 404


@api_bp.route('/servers/<server_id>', methods=['DELETE'])
def stop_server(server_id):
    """Stop a server"""
    mgr = ServerManager()
    try:
        mgr.stop_server(server_id)
        return jsonify({'success': True, 'message': 'Server stopped'})
    except Exception as e:
        return jsonify({'success': False, 'error': str(e)}), 500


@api_bp.route('/servers/<server_id>/logs', methods=['GET'])
def get_server_logs(server_id):
    """Get server logs"""
    lines = request.args.get('lines', 100, type=int)
    mgr = ServerManager()
    logs = mgr.get_server_logs(server_id, lines)
    return jsonify({'success': True, 'data': logs})


# =============================================================================
# Health & Info
# =============================================================================

@api_bp.route('/health', methods=['GET'])
def health():
    """Health check endpoint"""
    return jsonify({'status': 'ok'})


@api_bp.route('/info', methods=['GET'])
def info():
    """System information"""
    import platform
    import sys
    import psutil
    
    return jsonify({
        'success': True,
        'data': {
            'python_version': sys.version,
            'platform': platform.platform(),
            'architecture': platform.machine(),
            'cpu_count': psutil.cpu_count(),
            'memory_total_gb': round(psutil.virtual_memory().total / (1024**3), 2),
            'memory_available_gb': round(psutil.virtual_memory().available / (1024**3), 2)
        }
    })
