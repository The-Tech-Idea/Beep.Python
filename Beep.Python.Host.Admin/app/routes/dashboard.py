"""
Dashboard Routes - Main admin dashboard
"""
from flask import Blueprint, render_template
from app.services.runtime_manager import RuntimeManager
from app.services.environment_manager import EnvironmentManager
from app.services.server_manager import ServerManager

dashboard_bp = Blueprint('dashboard', __name__)


@dashboard_bp.route('/')
def index():
    """Main dashboard page"""
    runtime_mgr = RuntimeManager()
    env_mgr = EnvironmentManager()
    server_mgr = ServerManager()
    
    runtimes = runtime_mgr.discover_runtimes()
    environments = env_mgr.list_environments()
    servers = server_mgr.list_servers()
    
    stats = {
        'runtimes_count': len(runtimes),
        'environments_count': len(environments),
        'servers_running': len([s for s in servers if s.status == 'running']),
        'total_packages': sum(e.packages_count for e in environments),
        'total_size_mb': round(sum(e.size_mb for e in environments), 2)
    }
    
    return render_template('dashboard/index.html',
                          stats=stats,
                          runtimes=runtimes[:5],
                          environments=environments[:5],
                          servers=servers[:5])
