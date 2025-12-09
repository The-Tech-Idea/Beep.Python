"""
Setup Wizard Routes
API endpoints for the initial setup wizard.
"""
from flask import Blueprint, request, jsonify, render_template, current_app, redirect, url_for
from app.config_manager import config_manager
from app.database import db, get_db_uri
from app.models.core import User
# Import all models to ensure they are registered with SQLAlchemy before create_all()
from app.models import (
    Collection, Document, DataSource, SyncJob, SyncJobRun,
    AccessPrivilege, Role, Group, Setting, AuditLog
)
from sqlalchemy import text

setup_bp = Blueprint('setup', __name__)

@setup_bp.route('/', methods=['GET'])
def index():
    """Render Setup Wizard"""
    if config_manager.is_configured:
        return redirect('/')
    return render_template('setup.html')

@setup_bp.route('/api/validate-db', methods=['POST'])
def validate_db():
    """Validate database connection"""
    try:
        data = request.get_json(silent=True)
        if not data:
            return jsonify({'success': False, 'message': 'No JSON data received'}), 400

        provider = data.get('provider')
        if not provider:
            return jsonify({'success': False, 'message': 'Provider is required'}), 400
        
        # Construct URI - remove 'provider' from data to avoid duplicate argument
        db_params = {k: v for k, v in data.items() if k != 'provider'}
        uri = get_db_uri(provider, **db_params)
        
        if not uri:
            return jsonify({'success': False, 'message': 'Invalid database parameters'}), 400
            
        # Test connection
        from sqlalchemy import create_engine
        engine = create_engine(uri)
        with engine.connect() as conn:
            conn.execute(text("SELECT 1"))
        return jsonify({'success': True, 'message': 'Connection successful!', 'uri': uri})

    except Exception as e:
        print(f"Setup Error: {e}") # Debug log
        return jsonify({'success': False, 'message': f"Server Error: {str(e)}"}), 500

@setup_bp.route('/api/initialize', methods=['POST'])
def initialize():
    """Initialize application"""
    data = request.json
    db_uri = data.get('db_uri')
    admin_user = data.get('username')
    admin_pass = data.get('password') # In production, hash this!
    
    if not db_uri or not admin_user:
        return jsonify({'success': False, 'message': 'Missing required fields'}), 400
        
    try:
        # 1. Update Config
        config_manager.set('SQLALCHEMY_DATABASE_URI', db_uri)
        
        # 2. Initialize DB
        # We need to re-configure the app's db engine dynamically here or rely on next restart
        # For now, let's try to set it on current app
        current_app.config['SQLALCHEMY_DATABASE_URI'] = db_uri
        
        with current_app.app_context():
            db.create_all()
            
            # 3. Create Admin
            if not User.query.filter_by(username=admin_user).first():
                user = User(
                    username=admin_user,
                    password_hash=admin_pass, # TODO: Hash it
                    is_admin=True,
                    display_name="Administrator"
                )
                db.session.add(user)
                db.session.commit()
                
        # 4. Mark as Configured
        config_manager.set('is_configured', True)
        config_manager.set('secret_key', os.urandom(24).hex()) # Set a random secret key
        
        return jsonify({'success': True, 'redirect': '/'})
        
    except Exception as e:
        return jsonify({'success': False, 'message': str(e)}), 500

import os
