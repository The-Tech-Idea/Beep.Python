"""
Environments Routes - Virtual environment management
"""
from flask import Blueprint, render_template, request, jsonify, flash, redirect, url_for
from app.services.environment_manager import EnvironmentManager
from dataclasses import asdict

environments_bp = Blueprint('environments', __name__)


@environments_bp.route('/')
def index():
    """List all virtual environments"""
    mgr = EnvironmentManager()
    environments = mgr.list_environments()
    return render_template('environments/index.html', environments=environments)


@environments_bp.route('/<env_id>')
def detail(env_id):
    """Environment detail view with packages"""
    mgr = EnvironmentManager()
    environments = mgr.list_environments()
    env = next((e for e in environments if e.id == env_id), None)
    
    if not env:
        flash('Environment not found', 'error')
        return redirect(url_for('environments.index'))
    
    packages = mgr.get_packages(env_id)
    return render_template('environments/detail.html', 
                          environment=env, 
                          packages=packages)


@environments_bp.route('/create', methods=['GET', 'POST'])
def create():
    """Create new virtual environment"""
    if request.method == 'POST':
        name = request.form.get('name')
        packages = request.form.get('packages', '').split('\n')
        packages = [p.strip() for p in packages if p.strip()]
        
        mgr = EnvironmentManager()
        try:
            env = mgr.create_environment(name, packages=packages)
            flash(f'Environment "{name}" created successfully!', 'success')
            return redirect(url_for('environments.detail', env_id=env.id))
        except Exception as e:
            flash(f'Failed to create environment: {str(e)}', 'error')
    
    return render_template('environments/create.html')


@environments_bp.route('/<env_id>/delete', methods=['POST'])
def delete(env_id):
    """Delete virtual environment"""
    mgr = EnvironmentManager()
    try:
        mgr.delete_environment(env_id)
        flash(f'Environment deleted successfully!', 'success')
    except Exception as e:
        flash(f'Failed to delete: {str(e)}', 'error')
    return redirect(url_for('environments.index'))


@environments_bp.route('/<env_id>/packages/install', methods=['POST'])
def install_packages(env_id):
    """Install packages in environment"""
    packages = request.form.get('packages', '').split()
    
    mgr = EnvironmentManager()
    try:
        result = mgr.install_packages(env_id, packages)
        if result['success']:
            flash('Packages installed successfully!', 'success')
        else:
            flash(f'Installation failed: {result["stderr"]}', 'error')
    except Exception as e:
        flash(f'Failed to install: {str(e)}', 'error')
    
    return redirect(url_for('environments.detail', env_id=env_id))


@environments_bp.route('/<env_id>/packages/<package>/uninstall', methods=['POST'])
def uninstall_package(env_id, package):
    """Uninstall a package"""
    mgr = EnvironmentManager()
    try:
        result = mgr.uninstall_package(env_id, package)
        if result['success']:
            flash(f'Package "{package}" uninstalled!', 'success')
        else:
            flash(f'Uninstall failed: {result["stderr"]}', 'error')
    except Exception as e:
        flash(f'Failed to uninstall: {str(e)}', 'error')
    
    return redirect(url_for('environments.detail', env_id=env_id))


@environments_bp.route('/<env_id>/export')
def export_requirements(env_id):
    """Export requirements.txt"""
    from flask import Response
    
    mgr = EnvironmentManager()
    requirements = mgr.export_requirements(env_id)
    
    return Response(
        requirements,
        mimetype='text/plain',
        headers={'Content-Disposition': f'attachment; filename={env_id}_requirements.txt'}
    )
