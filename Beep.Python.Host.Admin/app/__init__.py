"""
Beep.Python Host Admin - Professional Python Environment Management Web Application
"""
import os
from flask import Flask
from flask_cors import CORS
from flask_socketio import SocketIO

# Initialize SocketIO for real-time updates
socketio = SocketIO()

def create_app(config_name=None):
    """Application factory pattern"""
    app = Flask(__name__, 
                template_folder='../templates',
                static_folder='../static')
    
    # Configuration
    from app.config_manager import config_manager
    app.config['SECRET_KEY'] = config_manager.get('secret_key', os.environ.get('SECRET_KEY', 'beep-python-admin-secret-key'))
    app.config['BEEP_PYTHON_HOME'] = os.environ.get('BEEP_PYTHON_HOME', 
                                                     os.path.expanduser('~/.beep-llm'))
    
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
            # Create any missing tables
            db.create_all()
        except Exception as e:
            print(f"Warning: Could not auto-create tables: {e}")

    # Enable CORS
    CORS(app)
    
    # Initialize SocketIO with the app
    socketio.init_app(app, cors_allowed_origins="*", async_mode='eventlet')
    
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
    
    app.register_blueprint(setup_bp, url_prefix='/setup')
    app.register_blueprint(dashboard_bp)
    app.register_blueprint(runtimes_bp, url_prefix='/runtimes')
    app.register_blueprint(environments_bp, url_prefix='/environments')
    app.register_blueprint(packages_bp, url_prefix='/packages')
    app.register_blueprint(servers_bp, url_prefix='/servers')
    app.register_blueprint(tasks_bp, url_prefix='/tasks')
    app.register_blueprint(llm_bp, url_prefix='/llm')
    app.register_blueprint(backend_ext_bp, url_prefix='/llm/backend-extensions')
    app.register_blueprint(openai_bp)  # /v1/... OpenAI-compatible API
    app.register_blueprint(rag_bp)      # /rag/... RAG management
    app.register_blueprint(api_bp, url_prefix='/api/v1')
    
    # Initialize RAG Sync Scheduler (cross-platform: Windows, Mac, Linux)
    from app.services.sync_scheduler import init_scheduler
    init_scheduler(app)
    
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
