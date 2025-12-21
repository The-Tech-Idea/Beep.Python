"""
REST API Routes
"""
import json
import logging
from dataclasses import asdict
from flask import Blueprint, request, jsonify
from app import db
from app.models.project import MLProject
from app.models.experiment import Experiment
from app.models.workflow import Workflow
from app.models.settings import Settings
from app.services.environment_manager import EnvironmentManager
from app.services.ml_service import MLService
from app.services.data_service import DataService
from flask import current_app
from datetime import datetime

from app.utils.constants import (
    HTTP_OK,
    HTTP_BAD_REQUEST,
    HTTP_NOT_FOUND,
    HTTP_INTERNAL_SERVER_ERROR,
    DIR_PROJECTS
)
from app.utils.request_validators import (
    validate_json_request,
    error_handler,
    sanitize_string_input
)
from app.exceptions.database_exceptions import DatabaseError

logger = logging.getLogger(__name__)

api_bp = Blueprint('api', __name__)


def get_environment_manager():
    """Get environment manager instance"""
    return EnvironmentManager()


def get_ml_service():
    """Get ML service instance"""
    env_mgr = EnvironmentManager()
    return MLService(
        projects_folder=current_app.config['PROJECTS_FOLDER'],
        environment_manager=env_mgr
    )


def get_data_service():
    """Get data service instance"""
    # DataService now uses settings by default
    return DataService(upload_folder=current_app.config.get('UPLOAD_FOLDER'))


@api_bp.route('/health', methods=['GET'])
@error_handler
def health():
    """Health check"""
    from app.services.embedded_python_manager import get_embedded_python_manager
    embedded_mgr = get_embedded_python_manager()
    embedded_available = embedded_mgr.is_embedded_installed()
    
    return jsonify({
        'success': True,
        'status': 'healthy',
        'embedded_python_available': embedded_available
    }), HTTP_OK


# Database Management API
@api_bp.route('/database/status', methods=['GET'])
def database_status():
    """Get database schema status and migration info"""
    try:
        from app.services.database_migration_manager import get_migration_manager
        mgr = get_migration_manager()
        status = mgr.get_schema_status()
        
        return jsonify({
            'success': True,
            'data': status
        })
    except DatabaseError as e:
        logger.error(f"Database error: {e}")
        return jsonify({
            'success': False,
            'error': str(e)
        }), HTTP_INTERNAL_SERVER_ERROR
    except Exception as e:
        logger.error(f"Error getting database status: {e}")
        return jsonify({
            'success': False,
            'error': str(e)
        }), HTTP_INTERNAL_SERVER_ERROR


@api_bp.route('/database/migrate', methods=['POST'])
def run_database_migrations():
    """Run pending database migrations"""
    try:
        from app.services.database_migration_manager import get_migration_manager
        mgr = get_migration_manager()
        applied = mgr.check_and_apply_migrations()
        
        return jsonify({
            'success': True,
            'message': f'Applied {len(applied)} migration(s)' if applied else 'No pending migrations',
            'applied_migrations': applied
        })
    except DatabaseError as e:
        logger.error(f"Database error: {e}")
        return jsonify({
            'success': False,
            'error': str(e)
        }), HTTP_INTERNAL_SERVER_ERROR
    except Exception as e:
        logger.error(f"Error running migrations: {e}")
        return jsonify({
            'success': False,
            'error': str(e)
        }), HTTP_INTERNAL_SERVER_ERROR


# Industry Modules API
@api_bp.route('/industry-modules', methods=['GET'])
def list_industry_modules():
    """List all available industry modules"""
    try:
        from app.industry_modules import get_available_modules
        modules = get_available_modules()
        
        return jsonify({
            'success': True,
            'data': [m.to_dict() for m in modules.values()]
        })
    except Exception as e:
        logger.error(f"Error listing industry modules: {e}")
        return jsonify({
            'success': True,
            'data': []  # Return empty list if modules not available
        })


@api_bp.route('/industry-modules/<module_id>', methods=['GET'])
def get_industry_module(module_id):
    """Get details of a specific industry module"""
    try:
        from app.industry_modules import get_module
        module = get_module(module_id)
        
        if not module:
            return jsonify({
                'success': False,
                'error': f'Module not found: {module_id}'
            }), 404
        
        return jsonify({
            'success': True,
            'data': module.to_dict()
        })
    except Exception as e:
        logger.error(f"Error getting industry module: {e}")
        return jsonify({
            'success': False,
            'error': str(e)
        }), 500


# Projects API
@api_bp.route('/projects', methods=['GET'])
def list_projects():
    """List all projects"""
    projects = MLProject.query.filter_by(status='active').all()
    return jsonify({
        'success': True,
        'data': [p.to_dict() for p in projects]
    })


@api_bp.route('/projects/create-from-competition', methods=['POST'])
@error_handler
@validate_json_request(required_fields=['competition_id'])
def create_project_from_competition():
    """Create a project from a Community competition (joins competition and downloads dataset)"""
    from app.services.community_client import get_community_client
    from app.services.auth_service import AuthService
    from pathlib import Path
    import shutil
    
    try:
        # Get current user
        current_user = AuthService.get_current_user()
        if not current_user:
            return jsonify({'success': False, 'error': 'Authentication required'}), 401
        
        data = request.get_json()
        competition_id = data.get('competition_id')
        project_name = data.get('name')  # Optional, will use competition title if not provided
        project_description = data.get('description')  # Optional, will use competition description if not provided
        
        # Get Community client
        client = get_community_client()
        
        # Get competition details
        competition = client.get_competition_detail(competition_id)
        if competition.get('error') or competition.get('success') is False:
            return jsonify({
                'success': False,
                'error': competition.get('error', 'Competition not found')
            }), 404
        
        # Check if user is participant (join if not)
        user_id = current_user.id
        is_participant = competition.get('is_participant', False)
        
        if not is_participant:
            # Join competition first
            join_result = client.join_competition(competition_id, user_id)
            if join_result.get('success') is False or join_result.get('error'):
                return jsonify({
                    'success': False,
                    'error': join_result.get('error', 'Failed to join competition')
                }), 400
        
        # Generate project name if not provided
        if not project_name:
            # Use competition title, sanitize for filesystem
            project_name = competition.get('title', f'Competition_{competition_id}')
            # Remove invalid characters for project name
            import re
            project_name = re.sub(r'[^\w\s-]', '', project_name).strip()
            project_name = re.sub(r'[-\s]+', '_', project_name)
            # Ensure uniqueness
            base_name = project_name
            counter = 1
            while MLProject.query.filter_by(name=project_name).first():
                project_name = f"{base_name}_{counter}"
                counter += 1
        
        # Generate project description if not provided
        if not project_description:
            project_description = competition.get('description', f'Project based on competition: {competition.get("title", "")}')
        
        # Create project (reuse existing create logic)
        env_mgr = get_environment_manager()
        
        # Verify embedded Python is available
        embedded_python = env_mgr.get_embedded_python()
        if not embedded_python:
            return jsonify({
                'success': False,
                'error': 'Embedded Python is required but not found. Please set up embedded Python first.'
            }), HTTP_BAD_REQUEST
        
        # Determine framework from competition task type or default to scikit-learn
        task_type = competition.get('task_type', 'classification')
        framework = data.get('framework', 'scikit-learn')  # Can be overridden
        
        packages = _get_framework_packages(framework)
        env_name = f"mlstudio_{project_name.lower().replace(' ', '_').replace('-', '_')}"
        
        # Create environment
        try:
            env_info = env_mgr.create_environment(env_name, packages=packages)
        except ValueError as e:
            # Environment might already exist, check if we can use it
            existing_env = env_mgr.get_environment(env_name)
            if not existing_env:
                return jsonify({
                    'success': False,
                    'error': f'Failed to create environment: {str(e)}'
                }), HTTP_BAD_REQUEST
        
        # Create project record
        project = MLProject(
            name=project_name,
            description=project_description,
            template=data.get('template', 'custom'),
            environment_name=env_name,
            framework=framework,
            competition_id=competition_id
        )
        db.session.add(project)
        db.session.flush()  # Flush to get project ID
        
        # Create project structure
        ml_service = get_ml_service()
        ml_service.create_project_structure(project.id, project.name)
        project_path = ml_service.get_project_path(project.id)
        
        # Download training dataset
        training_data_path = competition.get('training_data_path')
        if training_data_path:
            # Determine file extension
            file_ext = Path(training_data_path).suffix or '.csv'
            dataset_save_path = project_path / 'data' / f'training_data{file_ext}'
            
            download_success, download_error = client.download_training_data(competition_id, dataset_save_path)
            
            if not download_success:
                logger.warning(f"Failed to download training data: {download_error}")
                # Continue anyway - project is created, user can upload data manually
                # But log the issue
        
        db.session.commit()
        
        return jsonify({
            'success': True,
            'data': project.to_dict(),
            'message': 'Project created from competition successfully'
        }), 201
        
    except DatabaseError as e:
        db.session.rollback()
        logger.error(f"Database error creating project from competition: {e}")
        return jsonify({'success': False, 'error': str(e)}), HTTP_INTERNAL_SERVER_ERROR
    except Exception as e:
        db.session.rollback()
        import traceback
        logger.error(f"Error creating project from competition: {traceback.format_exc()}")
        return jsonify({'success': False, 'error': str(e)}), HTTP_INTERNAL_SERVER_ERROR


@api_bp.route('/projects', methods=['POST'])
@error_handler
@validate_json_request(required_fields=['name'])
@sanitize_string_input(['name', 'description'])
def create_project():
    """Create new project"""
    data = request.get_json()
    name = data.get('name')
    
    if not name:
        return jsonify({'success': False, 'error': 'Name is required'}), HTTP_BAD_REQUEST
    
    if MLProject.query.filter_by(name=name).first():
        return jsonify({'success': False, 'error': 'Project name already exists'}), HTTP_BAD_REQUEST
    
    env_name = f"mlstudio_{name.lower().replace(' ', '_')}"
    
    try:
        env_mgr = get_environment_manager()
        
        # Verify embedded Python is available
        embedded_python = env_mgr.get_embedded_python()
        if not embedded_python:
            return jsonify({
                'success': False, 
                'error': 'Embedded Python is required but not found. Please set up embedded Python first.'
            }), HTTP_BAD_REQUEST
        
        framework = data.get('framework', 'scikit-learn')
        packages = _get_framework_packages(framework)
        
        # Create environment using EnvironmentManager (uses embedded Python)
        try:
            env_info = env_mgr.create_environment(env_name, packages=packages)
        except RuntimeError as e:
            error_msg = str(e)
            if 'not found' in error_msg.lower():
                return jsonify({
                    'success': False,
                    'error': 'Embedded Python not found. Please set up embedded Python first.'
                }), HTTP_BAD_REQUEST
            else:
                return jsonify({
                    'success': False,
                    'error': f'Failed to create environment: {error_msg}'
                }), HTTP_INTERNAL_SERVER_ERROR
        except ValueError as e:
            return jsonify({
                'success': False,
                'error': f'Environment already exists: {str(e)}'
            }), HTTP_BAD_REQUEST
        except Exception as e:
            import traceback
            logger.error(f"Environment creation error: {traceback.format_exc()}")
            return jsonify({
                'success': False,
                'error': f'Failed to create environment: {str(e)}'
            }), HTTP_INTERNAL_SERVER_ERROR
        
        project = MLProject(
            name=name,
            description=data.get('description', ''),
            template=data.get('template', 'custom'),
            environment_name=env_name,  # CRITICAL: Link project to environment
            framework=framework
        )
        db.session.add(project)
        db.session.commit()
        
        # Verify the link
        env_valid, env_msg = project.validate_environment_link()
        if not env_valid:
            # Rollback if environment link is invalid
            db.session.rollback()
            return jsonify({
                'success': False,
                'error': f'Failed to link project to environment: {env_msg}'
            }), 500
        
        ml_service = get_ml_service()
        ml_service.create_project_structure(project.id, project.name)
        
        return jsonify({
            'success': True,
            'data': project.to_dict()
        }), 201
    except DatabaseError as e:
        db.session.rollback()
        logger.error(f"Database error creating project: {e}")
        return jsonify({'success': False, 'error': str(e)}), HTTP_INTERNAL_SERVER_ERROR
    except Exception as e:
        db.session.rollback()
        import traceback
        logger.error(f"Project creation error: {traceback.format_exc()}")
        return jsonify({'success': False, 'error': str(e)}), HTTP_INTERNAL_SERVER_ERROR


def _get_framework_packages(framework: str) -> list:
    """Get default packages for a framework"""
    base_packages = ['numpy', 'pandas', 'matplotlib', 'seaborn', 'scikit-learn']
    
    framework_packages = {
        'scikit-learn': base_packages,
        'tensorflow': base_packages + ['tensorflow'],
        'pytorch': base_packages + ['torch', 'torchvision'],
        'xgboost': base_packages + ['xgboost'],
        'lightgbm': base_packages + ['lightgbm'],
        'custom': base_packages
    }
    
    return framework_packages.get(framework, base_packages)


@api_bp.route('/projects/<int:project_id>', methods=['GET'])
def get_project(project_id):
    """Get project details"""
    project = MLProject.query.get_or_404(project_id)
    return jsonify({
        'success': True,
        'data': project.to_dict()
    })


@api_bp.route('/projects/<int:project_id>', methods=['DELETE'])
def delete_project(project_id):
    """Delete project (keeps virtual environment for reuse)"""
    project = MLProject.query.get_or_404(project_id)
    env_name = project.environment_name
    
    # Note: Virtual environment is NOT deleted - can be reused or manually deleted from Settings
    
    # Explicitly delete related records first to avoid foreign key issues
    try:
        from app.models.industry_scenario import IndustryScenarioProgress
        IndustryScenarioProgress.query.filter_by(project_id=project_id).delete()
    except Exception:
        pass  # Table might not exist yet
    
    try:
        from app.models.workflow import Workflow
        Workflow.query.filter_by(project_id=project_id).delete()
    except Exception:
        pass  # Table might not exist yet
    
    db.session.delete(project)
    db.session.commit()
    
    return jsonify({
        'success': True,
        'message': f'Project deleted. Virtual environment "{env_name}" was kept.'
    })


# Experiments API
@api_bp.route('/projects/<int:project_id>/experiments', methods=['GET'])
def list_experiments(project_id):
    """List experiments for a project"""
    experiments = Experiment.query.filter_by(project_id=project_id).all()
    return jsonify({
        'success': True,
        'data': [exp.to_dict() for exp in experiments]
    })


@api_bp.route('/experiments/<int:experiment_id>', methods=['GET'])
def get_experiment(experiment_id):
    """Get experiment details"""
    experiment = Experiment.query.get_or_404(experiment_id)
    return jsonify({
        'success': True,
        'data': experiment.to_dict()
    })


# File Management API for browsing project files
@api_bp.route('/projects/<int:project_id>/files', methods=['GET'])
def list_files(project_id):
    """List files and directories in a project path"""
    import os
    
    project = MLProject.query.get_or_404(project_id)
    ml_service = get_ml_service()
    project_path = ml_service.get_project_path(project_id)
    
    # Get optional path parameter for subdirectory browsing
    sub_path = request.args.get('path', '')
    
    # Ensure path is safe (no directory traversal)
    if '..' in sub_path:
        return jsonify({'success': False, 'error': 'Invalid path'}), HTTP_BAD_REQUEST
    
    # Build full path
    if sub_path:
        browse_path = project_path / sub_path
    else:
        browse_path = project_path
    
    files = []
    
    if browse_path.exists() and browse_path.is_dir():
        try:
            for item in browse_path.iterdir():
                # Skip hidden files and __pycache__
                if item.name.startswith('.') or item.name == '__pycache__':
                    continue
                
                file_info = {
                    'name': item.name,
                    'path': str(item.relative_to(project_path)).replace('\\', '/'),
                    'is_directory': item.is_dir(),
                }
                
                if item.is_file():
                    try:
                        stat = item.stat()
                        file_info['size'] = stat.st_size
                        file_info['modified'] = datetime.fromtimestamp(stat.st_mtime).isoformat()
                        
                        # Determine file type based on extension
                        ext = item.suffix.lower()
                        if ext == '.py':
                            file_info['type'] = 'python'
                        elif ext in ['.csv', '.xlsx', '.xls', '.json', '.parquet', '.feather']:
                            file_info['type'] = 'data'
                        elif ext in ['.pkl', '.joblib', '.h5', '.pt', '.pth']:
                            file_info['type'] = 'model'
                        elif ext in ['.png', '.jpg', '.jpeg', '.gif', '.svg']:
                            file_info['type'] = 'image'
                        elif ext in ['.yaml', '.yml', '.toml', '.ini', '.cfg']:
                            file_info['type'] = 'config'
                        elif ext in ['.txt', '.md', '.rst']:
                            file_info['type'] = 'text'
                        else:
                            file_info['type'] = 'file'
                    except Exception as e:
                        file_info['size'] = 0
                        file_info['type'] = 'file'
                else:
                    file_info['type'] = 'directory'
                
                files.append(file_info)
            
            # Sort: directories first, then by name
            files.sort(key=lambda x: (not x['is_directory'], x['name'].lower()))
            
        except PermissionError:
            return jsonify({'success': False, 'error': 'Permission denied'}), 403
        except Exception as e:
            logger.error(f"Error listing files: {e}")
            return jsonify({'success': False, 'error': str(e)}), HTTP_INTERNAL_SERVER_ERROR
    else:
        # Path doesn't exist - might be a new project, return empty list
        pass
    
    return jsonify({
        'success': True,
        'files': files,
        'path': sub_path,
        'project_path': str(project_path)
    })


@api_bp.route('/projects/<int:project_id>/files', methods=['POST'])
@error_handler
@validate_json_request(required_fields=['path'])
def create_file(project_id):
    """Create a new Python file"""
    project = MLProject.query.get_or_404(project_id)
    data = request.get_json()
    
    file_path = data.get('path', '')
    content = data.get('content', '')
    
    if not file_path:
        return jsonify({'success': False, 'error': 'File path is required'}), HTTP_BAD_REQUEST
    
    # Ensure path is relative and safe
    if '..' in file_path or file_path.startswith('/'):
        return jsonify({'success': False, 'error': 'Invalid file path'}), HTTP_BAD_REQUEST
    
    ml_service = get_ml_service()
    project_path = ml_service.get_project_path(project_id)
    full_path = project_path / file_path
    
    # Ensure parent directory exists
    full_path.parent.mkdir(parents=True, exist_ok=True)
    
    # Write file
    try:
        full_path.write_text(content, encoding='utf-8')
        return jsonify({
            'success': True,
            'message': f'File {file_path} created successfully',
            'data': {
                'path': file_path,
                'size': len(content)
            }
        }), 201
    except Exception as e:
        return jsonify({'success': False, 'error': str(e)}), 500


@api_bp.route('/projects/<int:project_id>/upload', methods=['POST'])
def upload_file(project_id):
    """Upload a file to the project"""
    project = MLProject.query.get_or_404(project_id)
    
    if 'file' not in request.files:
        return jsonify({'success': False, 'error': 'No file provided'}), 400
    
    file = request.files['file']
    if file.filename == '':
        return jsonify({'success': False, 'error': 'No file selected'}), 400
    
    destination = request.form.get('destination', 'data')
    
    # Sanitize filename
    import os
    from werkzeug.utils import secure_filename
    filename = secure_filename(file.filename)
    
    if not filename:
        return jsonify({'success': False, 'error': 'Invalid filename'}), HTTP_BAD_REQUEST
    
    ml_service = get_ml_service()
    project_path = ml_service.get_project_path(project_id)
    
    # Build destination path
    if destination:
        dest_dir = project_path / destination
    else:
        dest_dir = project_path
    
    # Ensure destination directory exists
    dest_dir.mkdir(parents=True, exist_ok=True)
    
    # Save file
    file_path = dest_dir / filename
    
    # Handle duplicate filenames
    counter = 1
    original_stem = file_path.stem
    while file_path.exists():
        file_path = dest_dir / f"{original_stem}_{counter}{file_path.suffix}"
        counter += 1
    
    try:
        file.save(str(file_path))
        
        # Return relative path from project root
        relative_path = str(file_path.relative_to(project_path)).replace('\\', '/')
        
        return jsonify({
            'success': True,
            'message': f'File uploaded successfully',
            'path': relative_path,
            'size': os.path.getsize(file_path)
        }), 201
    except Exception as e:
        logger.error(f"Error uploading file: {e}")
        return jsonify({'success': False, 'error': str(e)}), HTTP_INTERNAL_SERVER_ERROR


@api_bp.route('/projects/<int:project_id>/data-columns', methods=['GET'])
def get_data_columns(project_id):
    """Get columns from a data file (CSV, Excel, JSON, Parquet)"""
    import pandas as pd
    
    project = MLProject.query.get_or_404(project_id)
    file_path = request.args.get('file_path', '')
    
    if not file_path:
        return jsonify({'success': False, 'error': 'file_path parameter is required'}), 400
    
    # Ensure path is safe
    if '..' in file_path:
        return jsonify({'success': False, 'error': 'Invalid file path'}), 400
    
    ml_service = get_ml_service()
    project_path = ml_service.get_project_path(project_id)
    full_path = project_path / file_path
    
    if not full_path.exists():
        return jsonify({'success': False, 'error': f'File not found: {file_path}'}), 404
    
    try:
        # Determine file type and read columns
        file_ext = full_path.suffix.lower()
        df = None
        
        if file_ext == '.csv':
            df = pd.read_csv(full_path, nrows=5)  # Only read first 5 rows for speed
        elif file_ext in ['.xlsx', '.xls']:
            df = pd.read_excel(full_path, nrows=5)
        elif file_ext == '.json':
            df = pd.read_json(full_path)
            if len(df) > 5:
                df = df.head(5)
        elif file_ext == '.parquet':
            df = pd.read_parquet(full_path)
            if len(df) > 5:
                df = df.head(5)
        elif file_ext == '.feather':
            df = pd.read_feather(full_path)
            if len(df) > 5:
                df = df.head(5)
        else:
            return jsonify({'success': False, 'error': f'Unsupported file type: {file_ext}'}), 400
        
        # Get column info
        import numpy as np
        columns = []
        for col in df.columns:
            # Convert sample values, handling NaN and None
            sample_values = df[col].head(3).tolist()
            # Replace NaN, None, and numpy/pandas null values with None (which JSON serializes as null)
            sample_values_clean = []
            for val in sample_values:
                try:
                    if pd.isna(val) or val is None:
                        sample_values_clean.append(None)
                    elif isinstance(val, (float, np.floating)) and (np.isnan(val) or np.isinf(val)):
                        sample_values_clean.append(None)
                    else:
                        # Convert to native Python type for JSON serialization
                        if isinstance(val, (np.integer, np.int64, np.int32)):
                            sample_values_clean.append(int(val))
                        elif isinstance(val, (np.floating, np.float64, np.float32)):
                            sample_values_clean.append(float(val))
                        else:
                            sample_values_clean.append(str(val) if val is not None else None)
                except (TypeError, ValueError):
                    sample_values_clean.append(str(val) if val is not None else None)
            
            col_info = {
                'name': str(col),
                'dtype': str(df[col].dtype),
                'sample_values': sample_values_clean,
                'null_count': int(df[col].isnull().sum()),
                'unique_count': int(df[col].nunique()) if len(df) > 0 else 0
            }
            
            # Determine if column is likely a target/label
            col_lower = str(col).lower()
            col_info['is_likely_target'] = any(kw in col_lower for kw in ['target', 'label', 'class', 'y', 'output', 'result'])
            col_info['is_likely_id'] = any(kw in col_lower for kw in ['id', 'index', 'key', 'pk'])
            col_info['is_numeric'] = pd.api.types.is_numeric_dtype(df[col])
            col_info['is_categorical'] = pd.api.types.is_categorical_dtype(df[col]) or (df[col].nunique() < 20 and not pd.api.types.is_numeric_dtype(df[col]))
            
            columns.append(col_info)
        
        return jsonify({
            'success': True,
            'data': {
                'file_path': file_path,
                'row_count': len(df),
                'columns': columns
            }
        })
        
    except Exception as e:
        logger.error(f"Error reading data columns: {e}")
        return jsonify({'success': False, 'error': str(e)}), HTTP_INTERNAL_SERVER_ERROR


@api_bp.route('/projects/<int:project_id>/data-preview', methods=['GET'])
def get_data_preview(project_id):
    """Get a preview of data from a file"""
    import pandas as pd
    
    project = MLProject.query.get_or_404(project_id)
    file_path = request.args.get('file_path', '')
    n_rows = request.args.get('rows', 10, type=int)
    
    if not file_path:
        return jsonify({'success': False, 'error': 'file_path parameter is required'}), 400
    
    # Ensure path is safe
    if '..' in file_path:
        return jsonify({'success': False, 'error': 'Invalid file path'}), 400
    
    ml_service = get_ml_service()
    project_path = ml_service.get_project_path(project_id)
    full_path = project_path / file_path
    
    if not full_path.exists():
        return jsonify({'success': False, 'error': f'File not found: {file_path}'}), 404
    
    try:
        file_ext = full_path.suffix.lower()
        df = None
        
        if file_ext == '.csv':
            df = pd.read_csv(full_path, nrows=n_rows)
        elif file_ext in ['.xlsx', '.xls']:
            df = pd.read_excel(full_path, nrows=n_rows)
        elif file_ext == '.json':
            df = pd.read_json(full_path)
            df = df.head(n_rows)
        elif file_ext == '.parquet':
            df = pd.read_parquet(full_path)
            df = df.head(n_rows)
        else:
            return jsonify({'success': False, 'error': f'Unsupported file type: {file_ext}'}), HTTP_BAD_REQUEST
        
        return jsonify({
            'success': True,
            'data': {
                'columns': list(df.columns),
                'rows': df.values.tolist(),
                'dtypes': {col: str(dtype) for col, dtype in df.dtypes.items()}
            }
        }), HTTP_OK
        
    except Exception as e:
        logger.error(f"Error previewing data: {e}")
        return jsonify({'success': False, 'error': str(e)}), HTTP_INTERNAL_SERVER_ERROR


@api_bp.route('/projects/<int:project_id>/files/<path:file_path>', methods=['GET'])
def get_file(project_id, file_path):
    """Get file content"""
    project = MLProject.query.get_or_404(project_id)
    
    # Ensure path is safe
    if '..' in file_path or file_path.startswith('/'):
        return jsonify({'success': False, 'error': 'Invalid file path'}), 400
    
    ml_service = get_ml_service()
    project_path = ml_service.get_project_path(project_id)
    full_path = project_path / file_path
    
    if not full_path.exists():
        return jsonify({'success': False, 'error': 'File not found'}), 404
    
    try:
        content = full_path.read_text(encoding='utf-8')
        return jsonify({
            'success': True,
            'data': {
                'path': file_path,
                'content': content,
                'size': len(content)
            }
        })
    except Exception as e:
        logger.error(f"Error getting file: {e}")
        return jsonify({'success': False, 'error': str(e)}), HTTP_INTERNAL_SERVER_ERROR


@api_bp.route('/projects/<int:project_id>/files/<path:file_path>', methods=['PUT'])
@error_handler
@validate_json_request()
def update_file(project_id, file_path):
    """Update file content"""
    project = MLProject.query.get_or_404(project_id)
    data = request.get_json()
    
    # Ensure path is safe
    if '..' in file_path or file_path.startswith('/'):
        return jsonify({'success': False, 'error': 'Invalid file path'}), HTTP_BAD_REQUEST
    
    content = data.get('content', '')
    
    ml_service = get_ml_service()
    project_path = ml_service.get_project_path(project_id)
    full_path = project_path / file_path
    
    if not full_path.exists():
        return jsonify({'success': False, 'error': 'File not found'}), HTTP_NOT_FOUND
    
    try:
        content = data.get('content', '')
        full_path.write_text(content, encoding='utf-8')
        return jsonify({
            'success': True,
            'message': f'File {file_path} updated successfully',
            'data': {
                'path': file_path,
                'size': len(content)
            }
        }), HTTP_OK
    except Exception as e:
        logger.error(f"Error updating file: {e}")
        return jsonify({'success': False, 'error': str(e)}), HTTP_INTERNAL_SERVER_ERROR


@api_bp.route('/projects/<int:project_id>/files/<path:file_path>', methods=['DELETE'])
def delete_file(project_id, file_path):
    """Delete a file"""
    project = MLProject.query.get_or_404(project_id)
    
    # Ensure path is safe
    if '..' in file_path or file_path.startswith('/'):
        return jsonify({'success': False, 'error': 'Invalid file path'}), HTTP_BAD_REQUEST
    
    ml_service = get_ml_service()
    project_path = ml_service.get_project_path(project_id)
    full_path = project_path / file_path
    
    if not full_path.exists():
        return jsonify({'success': False, 'error': 'File not found'}), HTTP_NOT_FOUND
    
    try:
        full_path.unlink()
        return jsonify({
            'success': True,
            'message': f'File {file_path} deleted successfully'
        }), HTTP_OK
    except Exception as e:
        logger.error(f"Error deleting file: {e}")
        return jsonify({'success': False, 'error': str(e)}), HTTP_INTERNAL_SERVER_ERROR


@api_bp.route('/projects/<int:project_id>/templates', methods=['GET'])
def get_code_templates(project_id):
    """Get code templates for the project"""
    project = MLProject.query.get_or_404(project_id)
    
    templates = {
        'train_basic': {
            'name': 'Basic Training Script',
            'content': '''# Basic Training Script
import pandas as pd
from sklearn.model_selection import train_test_split
from sklearn.ensemble import RandomForestClassifier
from sklearn.metrics import accuracy_score, classification_report
import pickle
import os

# Load data
data_path = 'data/your_dataset.csv'
if os.path.exists(data_path):
    df = pd.read_csv(data_path)
    print(f"Loaded dataset with {len(df)} rows and {len(df.columns)} columns")
    
    # Preprocess data
    # X = df.drop('target', axis=1)
    # y = df['target']
    
    # Split data
    # X_train, X_test, y_train, y_test = train_test_split(X, y, test_size=0.2, random_state=42)
    
    # Train model
    # model = RandomForestClassifier(n_estimators=100, random_state=42)
    # model.fit(X_train, y_train)
    
    # Evaluate
    # y_pred = model.predict(X_test)
    # accuracy = accuracy_score(y_test, y_pred)
    # print(f"Accuracy: {accuracy:.4f}")
    # print(classification_report(y_test, y_pred))
    
    # Save model
    # os.makedirs('models', exist_ok=True)
    # with open('models/model.pkl', 'wb') as f:
    #     pickle.dump(model, f)
    # print("Model saved successfully!")
else:
    print(f"Dataset not found at {data_path}")
    print("Please upload a dataset first.")
'''
        },
        'train_regression': {
            'name': 'Regression Training Script',
            'content': '''# Regression Training Script
import pandas as pd
from sklearn.model_selection import train_test_split
from sklearn.ensemble import RandomForestRegressor
from sklearn.metrics import mean_squared_error, r2_score
import pickle
import os
import numpy as np

# Load data
data_path = 'data/your_dataset.csv'
if os.path.exists(data_path):
    df = pd.read_csv(data_path)
    print(f"Loaded dataset with {len(df)} rows")
    
    # Preprocess data
    # X = df.drop('target', axis=1)
    # y = df['target']
    
    # Split data
    # X_train, X_test, y_train, y_test = train_test_split(X, y, test_size=0.2, random_state=42)
    
    # Train model
    # model = RandomForestRegressor(n_estimators=100, random_state=42)
    # model.fit(X_train, y_train)
    
    # Evaluate
    # y_pred = model.predict(X_test)
    # mse = mean_squared_error(y_test, y_pred)
    # r2 = r2_score(y_test, y_pred)
    # print(f"MSE: {mse:.4f}")
    # print(f"R² Score: {r2:.4f}")
    
    # Save model
    # os.makedirs('models', exist_ok=True)
    # with open('models/model.pkl', 'wb') as f:
    #     pickle.dump(model, f)
    # print("Model saved successfully!")
else:
    print(f"Dataset not found at {data_path}")
'''
        },
        'preprocess': {
            'name': 'Data Preprocessing Script',
            'content': '''# Data Preprocessing Script
import pandas as pd
import numpy as np
from sklearn.preprocessing import StandardScaler, LabelEncoder
from sklearn.model_selection import train_test_split

# Load data
data_path = 'data/your_dataset.csv'
df = pd.read_csv(data_path)
print(f"Original shape: {df.shape}")

# Handle missing values
# df = df.dropna()  # or df.fillna(df.mean())

# Encode categorical variables
# label_encoders = {}
# for col in df.select_dtypes(include=['object']).columns:
#     le = LabelEncoder()
#     df[col] = le.fit_transform(df[col])
#     label_encoders[col] = le

# Feature scaling
# scaler = StandardScaler()
# numeric_cols = df.select_dtypes(include=[np.number]).columns
# df[numeric_cols] = scaler.fit_transform(df[numeric_cols])

# Save processed data
# df.to_csv('data/processed_data.csv', index=False)
# print(f"Processed shape: {df.shape}")
# print("Preprocessing complete!")
'''
        }
    }
    
    # Add framework-specific templates
    if project.framework == 'tensorflow':
        templates['train_tensorflow'] = {
            'name': 'TensorFlow/Keras Training',
            'content': '''# TensorFlow/Keras Training Script
import pandas as pd
import numpy as np
from sklearn.model_selection import train_test_split
from tensorflow import keras
from tensorflow.keras import layers
import pickle
import os

# Load data
data_path = 'data/your_dataset.csv'
if os.path.exists(data_path):
    df = pd.read_csv(data_path)
    print(f"Loaded dataset with {len(df)} rows")
    
    # Preprocess and split
    # X = df.drop('target', axis=1).values
    # y = df['target'].values
    # X_train, X_test, y_train, y_test = train_test_split(X, y, test_size=0.2, random_state=42)
    
    # Build model
    # model = keras.Sequential([
    #     layers.Dense(64, activation='relu', input_shape=(X_train.shape[1],)),
    #     layers.Dense(32, activation='relu'),
    #     layers.Dense(1, activation='sigmoid')
    # ])
    # model.compile(optimizer='adam', loss='binary_crossentropy', metrics=['accuracy'])
    
    # Train
    # history = model.fit(X_train, y_train, epochs=10, batch_size=32, validation_split=0.2)
    
    # Evaluate
    # loss, accuracy = model.evaluate(X_test, y_test)
    # print(f"Test Accuracy: {accuracy:.4f}")
    
    # Save model
    # os.makedirs('models', exist_ok=True)
    # model.save('models/model.h5')
    # print("Model saved successfully!")
else:
    print(f"Dataset not found at {data_path}")
'''
        }
    
    return jsonify({
        'success': True,
        'data': templates
    })


# Workflow/Pipeline API
@api_bp.route('/projects/<int:project_id>/workflows', methods=['GET'])
def list_workflows(project_id):
    """List all workflows for a project"""
    project = MLProject.query.get_or_404(project_id)
    workflows = Workflow.query.filter_by(project_id=project_id).order_by(Workflow.updated_at.desc()).all()
    return jsonify({
        'success': True,
        'data': [w.to_dict() for w in workflows]
    })


@api_bp.route('/projects/<int:project_id>/workflows', methods=['POST'])
@error_handler
@validate_json_request()
def create_workflow(project_id):
    """Create a new workflow"""
    project = MLProject.query.get_or_404(project_id)
    data = request.get_json()
    
    name = data.get('name', 'Untitled Workflow')
    description = data.get('description', '')
    workflow_data = data.get('workflow_data', {'nodes': [], 'edges': []})
    
    workflow = Workflow(
        project_id=project_id,
        name=name,
        description=description
    )
    workflow.set_workflow_data(workflow_data)
    
    # Use frontend-generated code if provided, otherwise generate on backend
    generated_code = data.get('generated_code')
    if not generated_code:
        from app.services.workflow_service import WorkflowService
        workflow_service = WorkflowService(projects_folder=current_app.config['PROJECTS_FOLDER'])
        generated_code = workflow_service.generate_code_from_workflow(workflow_data, project.framework or 'scikit-learn')
    workflow.generated_code = generated_code
    
    db.session.add(workflow)
    db.session.commit()
    
    return jsonify({
        'success': True,
        'data': workflow.to_dict()
    }), 201


@api_bp.route('/workflows/<int:workflow_id>', methods=['GET'])
def get_workflow(workflow_id):
    """Get workflow details"""
    workflow = Workflow.query.get_or_404(workflow_id)
    return jsonify({
        'success': True,
        'data': workflow.to_dict()
    })


@api_bp.route('/workflows/<int:workflow_id>', methods=['PUT'])
@error_handler
@validate_json_request()
def update_workflow(workflow_id):
    """Update workflow"""
    workflow = Workflow.query.get_or_404(workflow_id)
    data = request.get_json()
    project = workflow.project
    
    if 'name' in data:
        workflow.name = data['name']
    if 'description' in data:
        workflow.description = data['description']
    if 'workflow_data' in data:
        workflow.set_workflow_data(data['workflow_data'])
        
        # Update project context from workflow nodes
        try:
            from app.services.project_context import get_project_context
            from app.services.workflow_service import WorkflowService
            workflow_service = WorkflowService(projects_folder=current_app.config['PROJECTS_FOLDER'])
            project_path = workflow_service.get_project_path(project.id)
            context = get_project_context(project_path)
            
            # Extract configuration from workflow nodes
            _update_context_from_workflow(context, data['workflow_data'])
        except Exception as e:
            logger.warning(f"Failed to update project context: {e}")
        
        # Use frontend-generated code if provided, otherwise generate on backend
        generated_code = data.get('generated_code')
        if not generated_code:
            from app.services.workflow_service import WorkflowService
            workflow_service = WorkflowService(projects_folder=current_app.config['PROJECTS_FOLDER'])
            generated_code = workflow_service.generate_code_from_workflow(
                data['workflow_data'], 
                project.framework or 'scikit-learn',
                project_id=project.id
            )
        workflow.generated_code = generated_code
    
    workflow.status = data.get('status', workflow.status)
    db.session.commit()
    
    return jsonify({
        'success': True,
        'data': workflow.to_dict()
    })


def _update_context_from_workflow(context, workflow_data):
    """Extract and save configuration from workflow nodes to project context"""
    nodes = workflow_data.get('nodes', [])
    
    for node in nodes:
        node_type = node.get('type', '')
        node_data = node.get('data', {})
        
        # Load CSV node - extract data source info
        if node_type == 'data_load_csv':
            file_path = node_data.get('file_path')
            if file_path:
                context.set_data_source(
                    file_path=file_path,
                    delimiter=node_data.get('delimiter', ','),
                    has_header=node_data.get('header', True)
                )
        
        # Select Features/Target node - extract column selection
        elif node_type == 'preprocess_select_features_target':
            target = node_data.get('target_column')
            features = node_data.get('feature_columns', [])
            if target:
                context.set_target(target)
            if features:
                if isinstance(features, str):
                    features = [f.strip() for f in features.split(',') if f.strip()]
                context.set_features(features)
        
        # Split node - extract split configuration
        elif node_type == 'preprocess_split':
            context.set_split_config(
                test_size=node_data.get('test_size', 0.2),
                random_state=node_data.get('random_state', 42),
                shuffle=node_data.get('shuffle', True),
                stratify=node_data.get('stratify', True)
            )
        
        # Auto Data Prep node - extract preprocessing settings
        elif node_type == 'auto_data_prep':
            context.set_preprocessing({
                'handle_missing': {
                    'enabled': True,
                    'strategy': node_data.get('handle_missing', 'fill_median')
                },
                'encode_categorical': {
                    'enabled': node_data.get('encode_categoricals', True),
                    'method': 'label'
                },
                'drop_high_cardinality': {
                    'enabled': node_data.get('drop_high_cardinality', True),
                    'threshold': node_data.get('max_categories', 10)
                }
            })
        
        # Algorithm nodes - extract model configuration  
        elif 'classifier' in node_type or 'regressor' in node_type:
            model_type = 'classifier' if 'classifier' in node_type else 'regressor'
            algorithm = node_type.replace('algo_', '')
            context.set_model_config(
                model_type=model_type,
                algorithm=algorithm,
                params=node_data
            )


@api_bp.route('/workflows/<int:workflow_id>', methods=['DELETE'])
def delete_workflow(workflow_id):
    """Delete workflow"""
    workflow = Workflow.query.get_or_404(workflow_id)
    db.session.delete(workflow)
    db.session.commit()
    
    return jsonify({
        'success': True,
        'message': 'Workflow deleted successfully'
    })


@api_bp.route('/workflows/<int:workflow_id>/generate-code', methods=['POST'])
def generate_workflow_code(workflow_id):
    """Generate Python code from workflow (uses frontend-generated code if provided)"""
    workflow = Workflow.query.get_or_404(workflow_id)
    data = request.get_json()
    
    workflow_data = data.get('workflow_data', workflow.get_workflow_data())
    project = workflow.project
    
    # Update project context from workflow nodes
    try:
        from app.services.project_context import get_project_context
        from app.services.workflow_service import WorkflowService
        workflow_service = WorkflowService(projects_folder=current_app.config['PROJECTS_FOLDER'])
        project_path = workflow_service.get_project_path(project.id)
        context = get_project_context(project_path)
        _update_context_from_workflow(context, workflow_data)
    except Exception as e:
        logger.warning(f"Failed to update project context: {e}")
    
    # Use frontend-generated code if provided, otherwise generate on backend
    generated_code = data.get('generated_code')
    if not generated_code:
        from app.services.workflow_service import WorkflowService
        workflow_service = WorkflowService(projects_folder=current_app.config['PROJECTS_FOLDER'])
        generated_code = workflow_service.generate_code_from_workflow(
            workflow_data, 
            project.framework or 'scikit-learn',
            project_id=project.id
        )
    
    # Update workflow
    workflow.generated_code = generated_code
    workflow.set_workflow_data(workflow_data)
    db.session.commit()
    
    return jsonify({
        'success': True,
        'code': generated_code,
        'data': workflow.to_dict()
    })


@api_bp.route('/environments', methods=['GET'])
def list_environments():
    """List all available virtual environments"""
    try:
        from app.services.environment_manager import EnvironmentManager
        env_mgr = EnvironmentManager()
        environments = env_mgr.list_environments()
        
        # Get list of environments already linked to projects
        linked_envs = {p.environment_name for p in MLProject.query.all() if p.environment_name}
        
        # Filter out environments that are already linked
        available_envs = [env for env in environments if env.name not in linked_envs]
        
        return jsonify({
            'success': True,
            'data': [asdict(env) for env in available_envs]
        })
    except Exception as e:
        logger.error(f"Error listing environments: {e}")
        import traceback
        logger.error(traceback.format_exc())
        return jsonify({
            'success': False,
            'error': str(e)
        }), 500


@api_bp.route('/workflows/<int:workflow_id>/execute', methods=['POST'])
def execute_workflow(workflow_id):
    """Execute workflow by running generated code"""
    from flask import current_app
    
    try:
        workflow = Workflow.query.get_or_404(workflow_id)
        
        if not workflow.generated_code:
            return jsonify({
                'success': False,
                'error': 'No generated code available. Please generate code first.'
            }), HTTP_BAD_REQUEST
        
        # Save generated code to a file and execute it
        from app.services.ml_service import MLService
        from app.services.environment_manager import EnvironmentManager
        from app.services.workflow_service import WorkflowService
        
        env_mgr = EnvironmentManager()
        ml_service = MLService(
            projects_folder=current_app.config['PROJECTS_FOLDER'],
            environment_manager=env_mgr
        )
        
        project = workflow.project
        
        # Validate project has environment linked
        if not project.environment_name:
            return jsonify({
                'success': False,
                'error': f'Project "{project.name}" is not linked to a virtual environment. Please recreate the project.'
            }), HTTP_BAD_REQUEST
        
        # Check if environment exists
        env_exists, env_message = project.validate_environment_link()
        if not env_exists:
            return jsonify({
                'success': False,
                'error': f'Project environment not found: {env_message}. Please recreate the project.'
            }), HTTP_BAD_REQUEST
        
        # Extract required packages from workflow code and install them
        workflow_service = WorkflowService(projects_folder=current_app.config['PROJECTS_FOLDER'])
        required_packages = workflow_service.extract_required_packages(workflow.generated_code)
        
        if required_packages:
            logger.info(f"Installing required packages for workflow '{workflow.name}': {required_packages}")
            try:
                install_result = env_mgr.install_packages(project.environment_name, list(required_packages))
                if not install_result.get('success', False):
                    logger.warning(f"Some packages may have failed to install: {install_result.get('stderr', '')}")
                    # Continue anyway - packages might already be installed
            except Exception as e:
                logger.warning(f"Error installing packages: {e}. Continuing execution...")
        
        script_path = f'scripts/workflow_{workflow_id}.py'
        
        # Save code to file
        project_path = ml_service.get_project_path(project.id)
        script_full_path = project_path / script_path
        script_full_path.parent.mkdir(parents=True, exist_ok=True)
        script_full_path.write_text(workflow.generated_code, encoding='utf-8')
        
        # Create experiment
        from app.models.experiment import Experiment
        from datetime import datetime
        experiment = Experiment(
            project_id=project.id,
            name=f'Workflow: {workflow.name}',
            description=f'Executed from visual workflow',
            model_type='Workflow Pipeline',
            status='running'
        )
        experiment.started_at = datetime.utcnow()
        db.session.add(experiment)
        db.session.commit()
        
        # Extract all values BEFORE background thread to avoid lazy loading issues
        _project_id = project.id
        _experiment_id = experiment.id
        _workflow_id = workflow.id
        _script_path = script_path
        _env_name = project.environment_name
        
        # Get the Flask app for context in background thread (MUST be done while in request context)
        from flask import current_app
        _app = current_app._get_current_object()
        
        # Execute in background (similar to model training)
        import threading
        def execute_async(app_instance, project_id, experiment_id, workflow_id, script_path, env_name):
            """Execute workflow in background thread"""
            success = False
            error_message = None
            result = None
            
            try:
                with app_instance.app_context():
                    logger.info(f"Starting workflow execution for experiment {experiment_id}")
                    
                    # Recreate MLService within app context
                    from app.services.ml_service import MLService
                    from app.services.environment_manager import EnvironmentManager
                    env_mgr = EnvironmentManager()
                    ml_service = MLService(
                        projects_folder=app_instance.config['PROJECTS_FOLDER'],
                        environment_manager=env_mgr
                    )
                    
                    # Verify script exists
                    from pathlib import Path
                    project_path = ml_service.get_project_path(project_id)
                    script_full_path = project_path / script_path
                    if not script_full_path.exists():
                        raise FileNotFoundError(f"Script not found: {script_path}")
                    
                    logger.info(f"Executing script: {script_full_path}")
                    
                    # Execute the workflow script
                    result = ml_service.train_model(
                        project_id=project_id,
                        experiment_id=experiment_id,
                        script_path=script_path,
                        env_name=env_name
                    )
                    
                    success = result.get('success', False)
                    if not success:
                        error_message = result.get('stderr', result.get('stdout', 'Execution failed'))
                        logger.warning(f"Workflow execution failed: {error_message}")
                    else:
                        logger.info(f"Workflow execution completed successfully")
                        
            except Exception as e:
                logger.error(f"Workflow execution error: {e}", exc_info=True)
                success = False
                error_message = f"Execution error: {str(e)}"
            
            # ALWAYS update status, even if execution failed
            try:
                with app_instance.app_context():
                    from app.models.experiment import Experiment
                    from app.models.workflow import Workflow
                    from datetime import datetime
                    from app import db
                    
                    exp = Experiment.query.get(experiment_id)
                    wf = Workflow.query.get(workflow_id)
                    
                    if exp:
                        exp.status = 'completed' if success else 'failed'
                        exp.completed_at = datetime.utcnow()
                        
                        # Save stdout and stderr
                        if result:
                            max_length = 100000  # 100KB limit
                            exp.stdout = (result.get('stdout', '') or '')[:max_length]
                            exp.stderr = (result.get('stderr', '') or '')[:max_length]
                            
                            # Parse metrics from stdout if successful
                            if success and exp.stdout:
                                from app.services.metrics_parser import MetricsParser
                                parser = MetricsParser()
                                parsed_metrics = parser.parse_metrics(exp.stdout)
                                if parsed_metrics:
                                    exp.set_metrics(parsed_metrics)
                        
                        if error_message:
                            # Truncate error message if too long
                            max_error_length = 10000
                            exp.error_message = error_message[:max_error_length] if len(error_message) > max_error_length else error_message
                        
                        db.session.commit()
                        logger.info(f"Experiment {experiment_id} status updated to '{exp.status}'")
                    else:
                        logger.error(f"Experiment {experiment_id} not found for status update")
                    
                    if wf:
                        wf.last_executed_at = datetime.utcnow()
                        db.session.commit()
                    
            except Exception as e:
                logger.error(f"CRITICAL: Error updating experiment status: {e}", exc_info=True)
                # Try to update status directly as a last resort
                try:
                    with app_instance.app_context():
                        from app.models.experiment import Experiment
                        from datetime import datetime
                        from app import db
                        exp = Experiment.query.get(experiment_id)
                        if exp:
                            exp.status = 'failed'
                            exp.completed_at = datetime.utcnow()
                            exp.error_message = f"Status update error: {str(e)}"
                            db.session.commit()
                except:
                    logger.error(f"Failed to update experiment status even as fallback")
            
            # Emit WebSocket event
            try:
                with app_instance.app_context():
                    from app import socketio
                    socketio.emit('workflow_complete', {
                        'workflow_id': workflow_id,
                        'experiment_id': experiment_id,
                        'success': success,
                        'error': error_message
                    }, room=f'project_{project_id}')
                    logger.info(f"WebSocket event emitted for experiment {experiment_id}")
            except Exception as e:
                logger.error(f"Error emitting WebSocket event: {e}", exc_info=True)
        
        thread = threading.Thread(
            target=execute_async, 
            args=(_app, _project_id, _experiment_id, _workflow_id, _script_path, _env_name),
            daemon=True
        )
        thread.start()
        
        return jsonify({
            'success': True,
            'experiment_id': experiment.id,
            'message': 'Workflow execution started'
        })
    
    except Exception as e:
        logger.error(f"Error executing workflow {workflow_id}: {e}", exc_info=True)
        return jsonify({
            'success': False,
            'error': f'Failed to execute workflow: {str(e)}'
        }), 500


# Get experiment status
@api_bp.route('/experiments/<int:experiment_id>/status', methods=['GET'])
def get_experiment_status(experiment_id):
    """Get current status of an experiment"""
    from app.models.experiment import Experiment
    from datetime import datetime, timedelta
    
    experiment = Experiment.query.get_or_404(experiment_id)
    
    # Auto-mark as failed if running for more than 2 hours
    if experiment.status == 'running' and experiment.started_at:
        running_time = datetime.utcnow() - experiment.started_at
        if running_time > timedelta(hours=2):
            experiment.status = 'failed'
            experiment.completed_at = datetime.utcnow()
            experiment.error_message = 'Experiment timed out (running for more than 2 hours)'
            db.session.commit()
    
    return jsonify({
        'success': True,
        'status': experiment.status,
        'completed_at': experiment.completed_at.isoformat() if experiment.completed_at else None,
        'error_message': experiment.error_message,
        'stdout': experiment.stdout[-2000:] if experiment.stdout else None,  # Last 2KB
        'stderr': experiment.stderr[-2000:] if experiment.stderr else None,  # Last 2KB
        'experiment_id': experiment.id,
        'experiment_url': f'/experiments/{experiment.id}'
    })


# Cleanup stuck experiments
@api_bp.route('/experiments/cleanup-stuck', methods=['POST'])
def cleanup_stuck_experiments():
    """Mark experiments that have been running for too long as failed"""
    from app.models.experiment import Experiment
    from datetime import datetime, timedelta
    
    # Find experiments running for more than 1 hour
    cutoff_time = datetime.utcnow() - timedelta(hours=1)
    stuck_experiments = Experiment.query.filter(
        Experiment.status == 'running',
        Experiment.started_at < cutoff_time
    ).all()
    
    count = 0
    for exp in stuck_experiments:
        exp.status = 'failed'
        exp.completed_at = datetime.utcnow()
        exp.error_message = 'Experiment timed out (marked as failed by cleanup)'
        count += 1
    
    db.session.commit()
    
    return jsonify({
        'success': True,
        'message': f'Marked {count} stuck experiment(s) as failed'
    })


# Cancel/Update stuck experiment
@api_bp.route('/experiments/<int:experiment_id>/cancel', methods=['POST'])
def cancel_experiment(experiment_id):
    """Cancel or mark a stuck experiment as failed"""
    from app.models.experiment import Experiment
    
    experiment = Experiment.query.get_or_404(experiment_id)
    
    if experiment.status == 'running':
        experiment.status = 'failed'
        experiment.error_message = 'Cancelled by user'
        experiment.completed_at = datetime.utcnow()
        db.session.commit()
        
        return jsonify({
            'success': True,
            'message': 'Experiment cancelled'
        })
    else:
        return jsonify({
            'success': False,
            'error': f'Experiment is not running (status: {experiment.status})'
        })


# Settings API
@api_bp.route('/settings', methods=['GET'])
def list_settings():
    """List all settings, optionally filtered by category"""
    category = request.args.get('category')
    
    from app.services.settings_manager import get_settings_manager
    settings_mgr = get_settings_manager()
    
    if category:
        settings = settings_mgr.get_by_category(category)
    else:
        settings = settings_mgr.get_all()
    
    # Get full setting objects for detailed info
    query = Settings.query
    if category:
        query = query.filter_by(category=category)
    setting_objects = query.all()
    
    return jsonify({
        'success': True,
        'data': [s.to_dict() for s in setting_objects],
        'values': settings
    })


@api_bp.route('/settings/categories', methods=['GET'])
def list_categories():
    """List all setting categories"""
    from app.services.settings_manager import get_settings_manager
    settings_mgr = get_settings_manager()
    categories = settings_mgr.get_categories()
    
    return jsonify({
        'success': True,
        'data': categories
    })


@api_bp.route('/settings/<key>', methods=['GET'])
def get_setting(key):
    """Get a specific setting"""
    setting = Settings.query.filter_by(key=key).first_or_404()
    return jsonify({
        'success': True,
        'data': setting.to_dict()
    })


@api_bp.route('/settings/<key>', methods=['PUT'])
@error_handler
@validate_json_request()
def update_setting(key):
    """Update a setting"""
    data = request.get_json()
    
    setting = Settings.query.filter_by(key=key).first_or_404()
    
    if 'value' in data:
        setting.set_value(data['value'])
    if 'description' in data:
        setting.description = data['description']
    if 'category' in data:
        setting.category = data['category']
    
    db.session.commit()
    
    # Clear cache
    from app.services.settings_manager import get_settings_manager
    settings_mgr = get_settings_manager()
    settings_mgr._settings_cache.clear()
    
    return jsonify({
        'success': True,
        'data': setting.to_dict()
    })


@api_bp.route('/settings', methods=['POST'])
@error_handler
@validate_json_request(required_fields=['key'])
@sanitize_string_input(['key', 'description', 'category'])
def create_setting():
    """Create a new setting"""
    data = request.get_json()
    
    key = data.get('key')
    if not key:
        return jsonify({'success': False, 'error': 'Key is required'}), HTTP_BAD_REQUEST
    
    if Settings.query.filter_by(key=key).first():
        return jsonify({'success': False, 'error': 'Setting already exists'}), HTTP_BAD_REQUEST
    
    setting = Settings(
        key=key,
        category=data.get('category', 'general'),
        value_type=data.get('value_type', 'string'),
        description=data.get('description', ''),
        is_encrypted=data.get('is_encrypted', False)
    )
    setting.set_value(data.get('value', ''))
    
    db.session.add(setting)
    db.session.commit()
    
    return jsonify({
        'success': True,
        'data': setting.to_dict()
    }), 201


@api_bp.route('/settings/<key>', methods=['DELETE'])
def delete_setting(key):
    """Delete a setting"""
    setting = Settings.query.filter_by(key=key).first_or_404()
    db.session.delete(setting)
    db.session.commit()
    
    # Clear cache
    from app.services.settings_manager import get_settings_manager
    settings_mgr = get_settings_manager()
    settings_mgr._settings_cache.clear()
    
    return jsonify({
        'success': True,
        'message': 'Setting deleted'
    })


@api_bp.route('/settings/reset', methods=['POST'])
def reset_settings():
    """Reset settings to defaults"""
    data = request.get_json()
    category = data.get('category') if data else None
    
    from app.services.settings_manager import get_settings_manager
    settings_mgr = get_settings_manager()
    
    success = settings_mgr.reset_to_defaults(category=category)
    
    return jsonify({
        'success': success,
        'message': 'Settings reset to defaults'
    })


# =============================================================================
# ML Server Integration API - Publish models to Beep AI Server
# =============================================================================

@api_bp.route('/ml-server/status', methods=['GET'])
def ml_server_status():
    """Check ML Server (Host Admin) connection status"""
    try:
        from app.services.ml_server_client import get_ml_server_client
        from app.services.settings_manager import get_settings_manager
        
        settings = get_settings_manager()
        server_url = settings.get('host_admin_url', 'http://127.0.0.1:5000')
        
        client = get_ml_server_client(server_url)
        is_connected = client.health_check()
        
        server_info = {}
        if is_connected:
            server_info = client.get_server_info()
        
        return jsonify({
            'success': True,
            'connected': is_connected,
            'server_url': server_url,
            'server_info': server_info
        })
    except Exception as e:
        return jsonify({
            'success': False,
            'connected': False,
            'error': str(e)
        })


@api_bp.route('/ml-server/models', methods=['GET'])
def ml_server_list_models():
    """List models on the ML Server"""
    try:
        from app.services.ml_server_client import get_ml_server_client
        from app.services.settings_manager import get_settings_manager
        
        settings = get_settings_manager()
        server_url = settings.get('host_admin_url', 'http://127.0.0.1:5000')
        
        client = get_ml_server_client(server_url)
        result = client.list_models()
        
        return jsonify(result)
    except Exception as e:
        return jsonify({'success': False, 'error': str(e)})


@api_bp.route('/projects/<int:project_id>/publish', methods=['POST'])
def publish_project_model(project_id):
    """
    Publish a trained model from a project to the ML Server
    
    Expected JSON body:
    {
        "model_file": "model.pkl",  // relative path in project
        "name": "My Model",
        "description": "Model description",
        "model_type": "sklearn",
        "framework": "scikit-learn",
        "category": "classification",
        "tags": ["tag1", "tag2"],
        "is_public": true
    }
    """
    try:
        from app.services.ml_server_client import get_ml_server_client
        from app.services.settings_manager import get_settings_manager
        from pathlib import Path
        
        project = MLProject.query.get_or_404(project_id)
        data = request.get_json() or {}
        
        # Get model file path
        model_file = data.get('model_file', 'model.pkl')
        project_path = Path(project.path) if project.path else Path('projects') / project.name
        model_path = project_path / 'models' / model_file
        
        # Try alternate locations
        if not model_path.exists():
            model_path = project_path / model_file
        if not model_path.exists():
            # Look for any .pkl or .joblib file in the project
            for ext in ['*.pkl', '*.joblib', '*.h5', '*.onnx']:
                found = list(project_path.rglob(ext))
                if found:
                    model_path = found[0]
                    break
        
        if not model_path.exists():
            return jsonify({
                'success': False,
                'error': f'Model file not found. Looked in: {project_path}'
            }), HTTP_NOT_FOUND
        
        # Get server connection
        settings = get_settings_manager()
        server_url = settings.get('host_admin_url', 'http://127.0.0.1:5000')
        api_key = settings.get('host_admin_api_key')
        
        client = get_ml_server_client(server_url, api_key)
        
        # Check server is available
        if not client.health_check():
            return jsonify({
                'success': False,
                'error': f'Cannot connect to ML Server at {server_url}. Is Beep AI Server running?'
            }), 503
        
        # Publish the model
        result = client.publish_model(
            model_file_path=str(model_path),
            name=data.get('name', project.name),
            model_type=data.get('model_type', 'sklearn'),
            framework=data.get('framework', project.ml_framework),
            description=data.get('description', project.description),
            category=data.get('category'),
            tags=data.get('tags', []),
            is_public=data.get('is_public', True),
            requirements=data.get('requirements'),
            python_version=data.get('python_version')
        )
        
        if result.success:
            # Update project with published model info
            project.industry_config = project.industry_config or '{}'
            try:
                config = json.loads(project.industry_config) if isinstance(project.industry_config, str) else project.industry_config
            except:
                config = {}
            
            config['published_model'] = {
                'model_id': result.model_id,
                'version_id': result.version_id,
                'api_endpoint': result.api_endpoint,
                'server_url': server_url,
                'published_at': datetime.utcnow().isoformat()
            }
            project.industry_config = json.dumps(config)
            db.session.commit()
            
            return jsonify({
                'success': True,
                'model_id': result.model_id,
                'version_id': result.version_id,
                'api_endpoint': result.api_endpoint,
                'message': result.message
            })
        else:
            return jsonify({
                'success': False,
                'error': result.error
            }), HTTP_BAD_REQUEST
            
    except Exception as e:
        logger.error(f"Error publishing model: {e}")
        return jsonify({'success': False, 'error': str(e)}), HTTP_INTERNAL_SERVER_ERROR


@api_bp.route('/community/competitions', methods=['GET'])
def community_competitions():
    """Get list of competitions from Community platform"""
    try:
        from app.services.community_client import get_community_client
        
        active_only = request.args.get('active_only', 'true').lower() == 'true'
        
        client = get_community_client()
        result = client.get_competitions(active_only=active_only)
        
        return jsonify(result)
    except Exception as e:
        logger.error(f"Error getting competitions: {e}")
        return jsonify({
            'success': False,
            'error': str(e),
            'competitions': []
        })


@api_bp.route('/community/competitions/<int:competition_id>/join', methods=['POST'])
def join_competition(competition_id):
    """Join a competition in Community platform"""
    try:
        from app.services.community_client import get_community_client
        from app.services.auth_service import AuthService
        
        # Get current user
        current_user = AuthService.get_current_user()
        if not current_user:
            return jsonify({'success': False, 'error': 'Authentication required'}), 401
        
        # Use current user ID (assuming same user system)
        user_id = current_user.id
        
        client = get_community_client()
        result = client.join_competition(competition_id, user_id)
        
        if result.get('success') is False or result.get('error'):
            status_code = 400 if result.get('error') else 500
            return jsonify(result), status_code
        
        return jsonify({
            'success': True,
            'message': 'Successfully joined competition',
            'participant': result.get('participant')
        }), 200
        
    except Exception as e:
        logger.error(f"Error joining competition: {e}")
        return jsonify({
            'success': False,
            'error': str(e)
        }), 500


@api_bp.route('/ml-server/predict', methods=['POST'])
def ml_server_predict():
    """
    Make a prediction using a model on the ML Server
    
    Expected JSON body:
    {
        "model_id": "uuid-of-model",
        "data": {"feature1": 1.0, "feature2": 2.0}
    }
    """
    try:
        from app.services.ml_server_client import get_ml_server_client
        from app.services.settings_manager import get_settings_manager
        
        data = request.get_json()
        if not data:
            return jsonify({'success': False, 'error': 'No data provided'}), HTTP_BAD_REQUEST
        
        model_id = data.get('model_id')
        input_data = data.get('data')
        
        if not model_id:
            return jsonify({'success': False, 'error': 'model_id is required'}), HTTP_BAD_REQUEST
        if not input_data:
            return jsonify({'success': False, 'error': 'data is required'}), HTTP_BAD_REQUEST
        
        settings = get_settings_manager()
        server_url = settings.get('host_admin_url', 'http://127.0.0.1:5000')
        
        client = get_ml_server_client(server_url)
        result = client.predict(model_id, input_data)
        
        return jsonify(result)
    except Exception as e:
        logger.error(f"Error making prediction: {e}")
        return jsonify({'success': False, 'error': str(e)}), HTTP_INTERNAL_SERVER_ERROR
