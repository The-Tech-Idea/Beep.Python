"""
Servers Routes - Python server management
"""
from flask import Blueprint, render_template, request, jsonify, flash, redirect, url_for
from app.services.server_manager import ServerManager
from app.services.environment_manager import EnvironmentManager
from app.services.runtime_manager import RuntimeManager
from dataclasses import asdict

servers_bp = Blueprint('servers', __name__)


@servers_bp.route('/')
def index():
    """List all servers"""
    mgr = ServerManager()
    servers = mgr.list_servers()
    return render_template('servers/index.html', servers=servers)


@servers_bp.route('/<server_id>')
def detail(server_id):
    """Server detail view"""
    mgr = ServerManager()
    server = mgr.get_server(server_id)
    
    if not server:
        flash('Server not found', 'error')
        return redirect(url_for('servers.index'))
    
    logs = mgr.get_server_logs(server_id)
    return render_template('servers/detail.html', server=server, logs=logs)


@servers_bp.route('/start', methods=['GET', 'POST'])
def start():
    """Start a new server"""
    env_mgr = EnvironmentManager()
    runtime_mgr = RuntimeManager()
    
    environments = env_mgr.list_environments()
    runtimes = runtime_mgr.discover_runtimes()
    
    # Combine environments and runtimes for selection
    python_sources = []
    for env in environments:
        python_sources.append({
            'path': env.path,
            'name': f"[Env] {env.name}",
            'version': env.python_version,
            'type': 'environment'
        })
    for runtime in runtimes:
        if runtime.is_virtual:
            continue  # Already in environments
        python_sources.append({
            'path': runtime.path,
            'name': f"[Runtime] {runtime.version}",
            'version': runtime.version,
            'type': 'runtime',
            'executable': runtime.executable
        })
    
    if request.method == 'POST':
        name = request.form.get('name')
        venv_path = request.form.get('venv_path')
        backend_type = request.form.get('backend_type', 'http')
        port = request.form.get('port')
        port = int(port) if port else None
        
        if not name or not venv_path:
            flash('Server name and Python path are required', 'error')
            return render_template('servers/start.html', environments=environments, 
                                   python_sources=python_sources)
        
        mgr = ServerManager()
        try:
            server = mgr.start_server(name, venv_path, backend_type, port)
            flash(f'Server "{name}" started successfully on port {server.port}!', 'success')
            return redirect(url_for('servers.detail', server_id=server.id))
        except Exception as e:
            flash(f'Failed to start server: {str(e)}', 'error')
    
    return render_template('servers/start.html', environments=environments, 
                           python_sources=python_sources)


@servers_bp.route('/<server_id>/stop', methods=['POST'])
def stop(server_id):
    """Stop a running server"""
    mgr = ServerManager()
    try:
        mgr.stop_server(server_id)
        flash('Server stopped successfully!', 'success')
    except Exception as e:
        flash(f'Failed to stop server: {str(e)}', 'error')
    return redirect(url_for('servers.index'))


@servers_bp.route('/<server_id>/restart', methods=['POST'])
def restart(server_id):
    """Restart a server"""
    mgr = ServerManager()
    try:
        server = mgr.get_server(server_id)
        if server:
            mgr.stop_server(server_id)
            mgr.start_server(server.name, server.venv_path, server.backend_type)
            flash('Server restarted successfully!', 'success')
    except Exception as e:
        flash(f'Failed to restart server: {str(e)}', 'error')
    return redirect(url_for('servers.index'))
