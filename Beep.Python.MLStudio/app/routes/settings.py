"""
Settings Routes
"""
from flask import Blueprint, render_template, request, jsonify, flash, redirect, url_for
from app import db
from app.models.settings import Settings
from app.models.project import MLProject
from app.services.settings_manager import get_settings_manager
from app.services.environment_manager import EnvironmentManager
from app.services.community_connection_service import get_community_connection_service
from app.utils.request_validators import (
    validate_json_request,
    sanitize_string_input,
    error_handler
)

settings_bp = Blueprint('settings', __name__, url_prefix='/settings')


@settings_bp.route('/')
def index():
    """Settings page"""
    settings_mgr = get_settings_manager()
    categories = settings_mgr.get_categories()
    
    # Get settings by category
    settings_by_category = {}
    for category in categories:
        settings_by_category[category] = Settings.query.filter_by(category=category).all()
    
    # Get Community connection status
    community_service = get_community_connection_service()
    community_config = community_service.get_connection_config()
    
    return render_template('settings/index.html', 
                         categories=categories,
                         settings_by_category=settings_by_category,
                         community_config=community_config)


@settings_bp.route('/save', methods=['POST'])
def save():
    """Save settings"""
    settings_mgr = get_settings_manager()
    
    try:
        # Get all form data
        for key, value in request.form.items():
            if key.startswith('setting_'):
                setting_key = key.replace('setting_', '')
                settings_mgr.set(setting_key, value)
        
        flash('Settings saved successfully!', 'success')
    except Exception as e:
        flash(f'Error saving settings: {str(e)}', 'error')
    
    return redirect(url_for('settings.index'))


@settings_bp.route('/environments', methods=['GET'])
def list_environments():
    """List all environments with their linked projects"""
    try:
        env_mgr = EnvironmentManager()
        environments = env_mgr.list_environments()
        
        # Get linked projects for each environment
        envs_with_projects = []
        for env in environments:
            # Get all active projects linked to this environment (should be 0 or 1)
            linked_projects = MLProject.query.filter_by(environment_name=env.name, status='active').all()
            linked_project = linked_projects[0] if linked_projects else None
            
            env_dict = {
                'name': env.name,
                'path': env.path,
                'python_version': env.python_version,
                'packages_count': env.packages_count,
                'size_mb': env.size_mb,
                'created_at': env.created_at,
                'linked_project': {
                    'id': linked_project.id,
                    'name': linked_project.name
                } if linked_project else None
            }
            envs_with_projects.append(env_dict)
        
        return jsonify({
            'success': True,
            'data': envs_with_projects
        })
    except Exception as e:
        import logging
        logging.getLogger(__name__).error(f"Error listing environments: {e}", exc_info=True)
        return jsonify({
            'success': False,
            'error': str(e)
        }), 500


@settings_bp.route('/environments/<env_name>/delete', methods=['POST'])
def delete_environment(env_name):
    """Delete an environment, optionally with linked project"""
    try:
        data = request.get_json() or {}
        delete_project = data.get('delete_project', False)
        
        # Check if environment is linked to any projects
        linked_projects = MLProject.query.filter_by(environment_name=env_name, status='active').all()
        linked_project = linked_projects[0] if linked_projects else None
        
        if linked_project and not delete_project:
            return jsonify({
                'success': False,
                'error': f'Environment is linked to project "{linked_project.name}". Check "Delete Project" to delete both.',
                'linked_project': {
                    'id': linked_project.id,
                    'name': linked_project.name
                }
            }), 400
        
        # Delete linked project(s) if requested
        if linked_project and delete_project:
            for project in linked_projects:
                db.session.delete(project)
            db.session.commit()
        
        # Delete environment
        env_mgr = EnvironmentManager()
        success = env_mgr.delete_environment(env_name)
        
        if success:
            project_names = ', '.join([p.name for p in linked_projects]) if linked_projects else ''
            return jsonify({
                'success': True,
                'message': f'Environment "{env_name}" deleted successfully' + (f' along with project(s): {project_names}' if project_names else '')
            })
        else:
            return jsonify({
                'success': False,
                'error': f'Failed to delete environment "{env_name}"'
            }), 500
            
    except ValueError as e:
        # Environment not found
        return jsonify({
            'success': False,
            'error': str(e)
        }), 404
    except Exception as e:
        import logging
        logging.getLogger(__name__).error(f"Error deleting environment {env_name}: {e}", exc_info=True)
        return jsonify({
            'success': False,
            'error': str(e)
        }), 500


@settings_bp.route('/community/configure', methods=['POST'])
@error_handler
@validate_json_request(required_fields=['url'])
@sanitize_string_input(['url', 'api_key'])
def configure_community():
    """Configure Community server connection"""
    try:
        data = request.get_json()
        url = data.get('url')
        api_key = data.get('api_key', '')
        
        community_service = get_community_connection_service()
        success, error = community_service.configure_connection(url, api_key)
        
        if success:
            return jsonify({
                'success': True,
                'message': 'Connected to Community server successfully',
                'config': community_service.get_connection_config()
            })
        else:
            return jsonify({
                'success': False,
                'error': error or 'Failed to connect to Community server'
            }), 400
            
    except Exception as e:
        import logging
        logging.getLogger(__name__).error(f"Error configuring Community connection: {e}", exc_info=True)
        return jsonify({
            'success': False,
            'error': str(e)
        }), 500


@settings_bp.route('/community/test', methods=['POST'])
@error_handler
def test_community_connection():
    """Test Community server connection"""
    try:
        community_service = get_community_connection_service()
        success, error = community_service.test_connection()
        
        if success:
            return jsonify({
                'success': True,
                'message': 'Connection successful',
                'config': community_service.get_connection_config()
            })
        else:
            return jsonify({
                'success': False,
                'error': error or 'Connection test failed'
            }), 400
            
    except Exception as e:
        import logging
        logging.getLogger(__name__).error(f"Error testing Community connection: {e}", exc_info=True)
        return jsonify({
            'success': False,
            'error': str(e)
        }), 500


@settings_bp.route('/community/disconnect', methods=['POST'])
@error_handler
def disconnect_community():
    """Disconnect from Community server"""
    try:
        community_service = get_community_connection_service()
        community_service.disconnect()
        
        return jsonify({
            'success': True,
            'message': 'Disconnected from Community server',
            'config': community_service.get_connection_config()
        })
        
    except Exception as e:
        import logging
        logging.getLogger(__name__).error(f"Error disconnecting from Community: {e}", exc_info=True)
        return jsonify({
            'success': False,
            'error': str(e)
        }), 500

