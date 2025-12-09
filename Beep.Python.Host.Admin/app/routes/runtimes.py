"""
Runtimes Routes - Python runtime management
"""
from flask import Blueprint, render_template, request, jsonify, flash, redirect, url_for
from app.services.runtime_manager import RuntimeManager

runtimes_bp = Blueprint('runtimes', __name__)


@runtimes_bp.route('/')
def index():
    """List all Python runtimes"""
    mgr = RuntimeManager()
    runtimes = mgr.discover_runtimes()
    return render_template('runtimes/index.html', runtimes=runtimes)


@runtimes_bp.route('/<runtime_id>')
def detail(runtime_id):
    """Runtime detail view"""
    mgr = RuntimeManager()
    runtime = mgr.get_runtime_info(runtime_id)
    if not runtime:
        flash('Runtime not found', 'error')
        return redirect(url_for('runtimes.index'))
    return render_template('runtimes/detail.html', runtime=runtime)


@runtimes_bp.route('/install-embedded', methods=['POST'])
def install_embedded():
    """Install embedded Python"""
    mgr = RuntimeManager()
    try:
        mgr.install_embedded_python()
        flash('Embedded Python installed successfully!', 'success')
    except Exception as e:
        flash(f'Failed to install: {str(e)}', 'error')
    return redirect(url_for('runtimes.index'))
