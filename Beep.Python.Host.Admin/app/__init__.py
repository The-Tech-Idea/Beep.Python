"""
Beep.Python Host Admin - Professional Python Environment Management Web Application
"""
import os
import sys
from flask import Flask
from flask_cors import CORS
from flask_socketio import SocketIO

# Initialize SocketIO for real-time updates
socketio = SocketIO()


def _is_frozen():
    """Check if running as a PyInstaller frozen executable."""
    return getattr(sys, 'frozen', False) and hasattr(sys, '_MEIPASS')


def _resolve_async_mode():
    """
    Prefer eventlet/gevent if available; otherwise use threading.
    Only return eventlet/gevent when imports succeed.
    For frozen (PyInstaller) builds, default to threading for compatibility.
    """
    allowed = {'eventlet', 'gevent', 'threading', 'gevent_uwsgi'}
    
    # For frozen builds, prefer threading to avoid async driver issues
    if _is_frozen():
        preferred = os.environ.get('ASYNC_MODE', 'threading').strip().lower()
    else:
        preferred = os.environ.get('ASYNC_MODE', 'eventlet').strip().lower()
    
    modes = [preferred] if preferred in allowed else []
    # Default cascade - for frozen builds, threading comes first
    if _is_frozen():
        modes += [m for m in ('threading', 'eventlet', 'gevent') if m not in modes]
    else:
        modes += [m for m in ('eventlet', 'gevent', 'threading') if m not in modes]

    for mode in modes:
        if mode == 'eventlet':
            try:
                import eventlet  # noqa: F401
                # Additional check for eventlet hub availability
                import eventlet.hubs  # noqa: F401
                eventlet.hubs.get_hub()
                return 'eventlet'
            except Exception as e:
                print(f"Warning: eventlet unavailable, trying next async mode: {e}")
        elif mode == 'gevent':
            try:
                import gevent  # noqa: F401
                import gevent.monkey  # noqa: F401
                return 'gevent'
            except Exception as e:
                print(f"Warning: gevent unavailable, trying next async mode: {e}")
        elif mode == 'threading':
            return 'threading'
    return 'threading'


def _get_bundle_path():
    """Get the bundle path for PyInstaller frozen builds."""
    if _is_frozen():
        return sys._MEIPASS
    return os.path.dirname(os.path.dirname(os.path.abspath(__file__)))


def create_app(config_name=None):
    """Application factory pattern"""
    # Resolve template and static paths for both normal and frozen builds
    bundle_path = _get_bundle_path()
    template_path = os.path.join(bundle_path, 'templates')
    static_path = os.path.join(bundle_path, 'static')
    
    # Fall back to relative paths if absolute paths don't exist
    if not os.path.exists(template_path):
        template_path = '../templates'
    if not os.path.exists(static_path):
        static_path = '../static'
    
    app = Flask(__name__, 
                template_folder=template_path,
                static_folder=static_path)
    
    # Configuration - ALL paths use app's own folder (portable/standalone)
    from app.config_manager import config_manager, get_app_directory
    app.config['SECRET_KEY'] = config_manager.get('secret_key', 'beep-python-admin-secret-key')
    
    # BEEP_PYTHON_HOME is NOW the app's own folder - no user home fallback
    app_dir = get_app_directory()
    app.config['BEEP_PYTHON_HOME'] = str(app_dir / 'data')
    
    # Database Config
    if config_manager.db_uri:
        app.config['SQLALCHEMY_DATABASE_URI'] = config_manager.db_uri
    else:
        # Fallback for initial load (will be set by wizard)
        app.config['SQLALCHEMY_DATABASE_URI'] = 'sqlite:///:memory:' # Dummy to prevent error on startup before config
    
    app.config['SQLALCHEMY_TRACK_MODIFICATIONS'] = False

    # Initialize Extensions
    from app.database import init_db
    init_db(app)
    
    # Auto-create missing database tables (for new models)
    from app.database import db
    with app.app_context():
        try:
            # Import all models to ensure they're registered
            from app.models.rag_metadata import Collection, Document, AccessPrivilege, DataSource, SyncJob, SyncJobRun
            from app.models.ml_models import MLModel, MLModelVersion, MLModelAPI, MLModelUsageLog, MLModelValidation, MLModelPermission
            from app.models.scheduled_jobs import ScheduledJob, JobExecution
            # Import middleware models (may not exist before migration)
            try:
                from app.models.middleware import RoutingRule, AccessPolicy
            except ImportError:
                pass  # Models will be created by migration script
            # Create any missing tables
            db.create_all()
        except Exception as e:
            print(f"Warning: Could not auto-create tables: {e}")

    # Enable CORS
    CORS(app)
    
    # Initialize SocketIO with the app
    socketio.init_app(app, cors_allowed_origins="*", async_mode=_resolve_async_mode())
    
    # Register blueprints
    from app.routes.dashboard import dashboard_bp
    from app.routes.runtimes import runtimes_bp
    from app.routes.environments import environments_bp
    from app.routes.packages import packages_bp
    from app.routes.servers import servers_bp
    from app.routes.api import api_bp
    from app.routes.tasks import tasks_bp
    from app.routes.llm import llm_bp
    from app.routes.openai_api import openai_bp
    from app.routes.rag import rag_bp
    from app.routes.setup import setup_bp
    from app.routes.backend_extensions import backend_ext_bp
    from app.routes.ml_models import ml_models_bp
    from app.routes.document_extraction import document_extraction_bp
    from app.routes.job_scheduler import job_scheduler_bp
    from app.routes.ai_services import ai_services_bp
    from app.routes.ai_middleware import ai_middleware_bp
    from app.routes.llm_integration import llm_integration_bp
    
    app.register_blueprint(setup_bp, url_prefix='/setup')
    app.register_blueprint(dashboard_bp)
    app.register_blueprint(runtimes_bp, url_prefix='/runtimes')
    app.register_blueprint(environments_bp, url_prefix='/environments')
    app.register_blueprint(packages_bp, url_prefix='/packages')
    app.register_blueprint(servers_bp, url_prefix='/servers')
    app.register_blueprint(tasks_bp, url_prefix='/tasks')
    app.register_blueprint(llm_bp, url_prefix='/llm')
    app.register_blueprint(backend_ext_bp, url_prefix='/llm/backend-extensions')
    app.register_blueprint(llm_integration_bp, url_prefix='/llm')  # LLM integration routes
    app.register_blueprint(openai_bp)  # /v1/... OpenAI-compatible API
    app.register_blueprint(rag_bp)      # /rag/... RAG management
    app.register_blueprint(document_extraction_bp, url_prefix='/document-extraction')  # Document extraction
    app.register_blueprint(job_scheduler_bp, url_prefix='/job-scheduler')  # Job/Task Scheduler
    app.register_blueprint(ai_services_bp, url_prefix='/ai-services')  # AI Services (Text-to-Image, TTS, STT, etc.)
    app.register_blueprint(ai_middleware_bp, url_prefix='/ai-middleware')  # Unified AI Services Middleware
    app.register_blueprint(ml_models_bp)  # /ml-models (web) and /api/v1/ml-models (API)
    app.register_blueprint(api_bp, url_prefix='/api/v1')
    
    # Initialize RAG Sync Scheduler (cross-platform: Windows, Mac, Linux)
    from app.services.sync_scheduler import init_scheduler
    init_scheduler(app)
    
    # Initialize Unified Job Scheduler
    from app.services.job_scheduler import get_job_scheduler
    job_scheduler = get_job_scheduler()
    job_scheduler.init_app(app)
    
    # Pre-detect hardware at startup (cache for fast access)
    # This runs once when app starts, uses cached data if available
    with app.app_context():
        try:
            from app.services.hardware_detector import detect_hardware_at_startup
            hw = detect_hardware_at_startup()
            print(f"Hardware: {hw.gpu_name or 'No GPU'}, {hw.ram_gb}GB RAM, {hw.cpu_cores} cores")
        except Exception as e:
            print(f"Warning: Could not detect hardware at startup: {e}")
    
    # Setup Enforcer Middleware
    from flask import request, redirect, url_for
    
    @app.before_request
    def check_setup():
        if not config_manager.is_configured:
            # Allow static files and setup routes
            if request.endpoint and (
                'setup' in request.endpoint or 
                'static' in request.endpoint
            ):
                return None
            return redirect(url_for('setup.index'))
            
    return app
