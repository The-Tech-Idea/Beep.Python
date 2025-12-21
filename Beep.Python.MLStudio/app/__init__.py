"""
Beep ML Studio Application Factory
By TheTechIdea
"""
import os
import logging
from flask import Flask, jsonify
from flask_sqlalchemy import SQLAlchemy
from flask_socketio import SocketIO
from flask_cors import CORS
from flask_jwt_extended import JWTManager
from dotenv import load_dotenv
from pathlib import Path

from app.utils.constants import (
    ENV_SECRET_KEY,
    ENV_DATABASE_URL,
    DEFAULT_SECRET_KEY,
    DEFAULT_DATABASE_URL,
    HTTP_INTERNAL_SERVER_ERROR,
    HTTP_NOT_FOUND,
    HTTP_BAD_REQUEST
)

# Load environment variables
load_dotenv()

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)

# Initialize extensions
db = SQLAlchemy()
socketio = SocketIO(cors_allowed_origins="*")
jwt = JWTManager()




def create_app(config_name=None):
    """Application factory"""
    # Get the app root directory (where app/__init__.py is located)
    app_root = Path(__file__).parent.absolute()
    project_root = app_root.parent
    
    # Verify templates directory exists
    templates_dir = project_root / 'templates'
    if not templates_dir.exists():
        raise RuntimeError(f"Templates directory not found: {templates_dir}")
    
    # Set instance_relative_config to False to prevent Flask from using instance folder
    # Explicitly set template folder to project root templates (absolute path)
    # Set instance_path to project_root so Flask doesn't use a separate instance folder
    app = Flask(
        __name__, 
        instance_relative_config=False,
        instance_path=str(project_root),
        template_folder=str(templates_dir.absolute()),
        static_folder=str(project_root / 'static') if (project_root / 'static').exists() else None
    )
    
    # Configuration - use environment variables first, then settings
    app.config['SECRET_KEY'] = os.environ.get(ENV_SECRET_KEY, DEFAULT_SECRET_KEY)
    
    # JWT Configuration
    app.config['JWT_SECRET_KEY'] = os.environ.get('JWT_SECRET_KEY', app.config['SECRET_KEY'])
    app.config['JWT_ACCESS_TOKEN_EXPIRES'] = int(os.environ.get('JWT_ACCESS_TOKEN_EXPIRES', 3600))
    
    # Forced industry mode - set via command-line argument
    app.config['FORCED_INDUSTRY_MODE'] = os.environ.get('MLSTUDIO_FORCED_INDUSTRY', None)
    
    # Database URI - use absolute path to project root (not instance folder)
    db_uri = os.environ.get(ENV_DATABASE_URL, None)
    if not db_uri:
        # Use absolute path to project root (not instance folder)
        project_root = Path(__file__).parent.parent.absolute()
        db_path = project_root / 'mlstudio.db'
        db_uri = f'sqlite:///{db_path}'
    app.config['SQLALCHEMY_DATABASE_URI'] = db_uri
    app.config['SQLALCHEMY_TRACK_MODIFICATIONS'] = False
    
    # Initialize settings manager first
    from app.services.settings_manager import get_settings_manager
    settings_mgr = get_settings_manager()
    
    # Upload folder - use settings
    upload_folder = settings_mgr.get_data_folder()
    if not upload_folder.is_absolute():
        upload_folder = Path('.').resolve() / upload_folder
    upload_folder.mkdir(parents=True, exist_ok=True)
    app.config['UPLOAD_FOLDER'] = str(upload_folder)
    
    # Projects folder - use settings
    projects_folder = settings_mgr.get_projects_folder()
    if not projects_folder.is_absolute():
        projects_folder = Path('.').resolve() / projects_folder
    projects_folder.mkdir(parents=True, exist_ok=True)
    app.config['PROJECTS_FOLDER'] = str(projects_folder)
    
    # Max upload size - use settings
    app.config['MAX_CONTENT_LENGTH'] = settings_mgr.get_max_upload_size()
    
    # Initialize extensions
    db.init_app(app)
    CORS(app)
    socketio.init_app(app)
    jwt.init_app(app)
    
    # Register error handlers for custom exceptions
    from app.exceptions import (
        AuthenticationError,
        IdentityServerError,
        TokenValidationError,
        UserAccessDeniedError,
        DatabaseError,
        DatabaseConnectionError,
        CommunityConnectionError,
        CommunityAuthError,
        CommunityAPIError
    )
    
    @app.errorhandler(AuthenticationError)
    @app.errorhandler(IdentityServerError)
    @app.errorhandler(TokenValidationError)
    def handle_auth_error(e):
        """Handle authentication errors"""
        return jsonify({
            'success': False,
            'error': str(e)
        }), 401
    
    @app.errorhandler(UserAccessDeniedError)
    def handle_access_denied(e):
        """Handle access denied errors"""
        return jsonify({
            'success': False,
            'error': str(e)
        }), 403
    
    @app.errorhandler(DatabaseError)
    @app.errorhandler(DatabaseConnectionError)
    def handle_database_error(e):
        """Handle database errors"""
        return jsonify({
            'success': False,
            'error': str(e)
        }), 500
    
    @app.errorhandler(CommunityConnectionError)
    @app.errorhandler(CommunityAuthError)
    @app.errorhandler(CommunityAPIError)
    def handle_community_error(e):
        """Handle Community connection errors"""
        status_code = getattr(e, 'status_code', 502)
        return jsonify({
            'success': False,
            'error': str(e)
        }), status_code
    
    @app.errorhandler(404)
    def handle_not_found(e):
        """Handle 404 errors"""
        return jsonify({
            'success': False,
            'error': 'Resource not found'
        }), HTTP_NOT_FOUND
    
    @app.errorhandler(500)
    def handle_internal_error(e):
        """Handle 500 errors"""
        logger = logging.getLogger(__name__)
        logger.error(f"Internal server error: {e}", exc_info=True)
        return jsonify({
            'success': False,
            'error': 'Internal server error'
        }), HTTP_INTERNAL_SERVER_ERROR
    
    @app.errorhandler(Exception)
    def handle_generic_error(e):
        """Handle all other exceptions"""
        logger = logging.getLogger(__name__)
        logger.error(f"Unhandled exception: {e}", exc_info=True)
        return jsonify({
            'success': False,
            'error': 'An unexpected error occurred'
        }), HTTP_INTERNAL_SERVER_ERROR
    
    # Register blueprints
    from app.routes.dashboard import dashboard_bp
    from app.routes.projects import projects_bp
    from app.routes.models import models_bp
    from app.routes.experiments import experiments_bp
    from app.routes.api import api_bp
    from app.routes.settings import settings_bp
    from app.routes.industry import industry_bp
    
    app.register_blueprint(dashboard_bp)
    app.register_blueprint(projects_bp, url_prefix='/projects')
    app.register_blueprint(models_bp, url_prefix='/models')
    app.register_blueprint(experiments_bp, url_prefix='/experiments')
    app.register_blueprint(api_bp, url_prefix='/api/v1')
    app.register_blueprint(settings_bp)
    app.register_blueprint(industry_bp, url_prefix='/industry')
    
    # Initialize theme provider
    from app.services.theme_provider import init_theme_provider
    init_theme_provider(app)
    
    # Context processor to make theme, forced_industry and current profile available in all templates
    @app.context_processor
    def inject_theme_and_industry():
        from flask import session
        from app.industry_profiles import profile_manager
        from app.services.theme_provider import get_theme_provider
        
        # Get theme
        theme_provider = get_theme_provider()
        branding = theme_provider.get_branding_for_app("Beep ML Studio")
        
        # Get industry profile
        forced_industry = app.config.get('FORCED_INDUSTRY_MODE')
        current_mode = session.get('industry_mode', 'advanced')
        
        current_profile = None
        if forced_industry:
            # Map aliases
            industry_aliases = {
                'pet': 'petroleum', 'oilandgas': 'petroleum', 'oil': 'petroleum',
                'health': 'healthcare', 'medical': 'healthcare',
                'fin': 'finance', 'mfg': 'manufacturing'
            }
            profile_id = industry_aliases.get(forced_industry.lower(), forced_industry.lower())
            current_profile = profile_manager.get(profile_id)
        elif current_mode and current_mode != 'advanced':
            current_profile = profile_manager.get(current_mode)
        
        return {
            'theme': branding,
            'forced_industry': forced_industry,
            'current_profile': current_profile,
            'current_industry_mode': current_mode
        }
    
    # Create database tables and run migrations
    with app.app_context():
        db.create_all()
        
        # Run automatic migrations for schema upgrades
        # Migration manager automatically uses Flask's database path
        from app.services.database_migration_manager import run_migrations_on_startup
        run_migrations_on_startup()
    
    # Register SocketIO events
    @socketio.on('join_project')
    def on_join_project(data):
        """Join a project room for real-time updates"""
        project_id = data.get('project_id')
        if project_id:
            from flask_socketio import join_room
            room = f'project_{project_id}'
            join_room(room)
            socketio.emit('joined_project', {'project_id': project_id}, room=room)
    
    @socketio.on('leave_project')
    def on_leave_project(data):
        """Leave a project room"""
        project_id = data.get('project_id')
        if project_id:
            from flask_socketio import leave_room
            room = f'project_{project_id}'
            leave_room(room)
    
    return app

