"""
ML Models Routes - API endpoints for ML model hosting
"""
import os
import time
import pickle
import json
import sys
from pathlib import Path
from flask import Blueprint, request, jsonify, send_file, render_template, session, flash, redirect, url_for
from werkzeug.utils import secure_filename

from app.services.ml_model_service import MLModelService
from app.services.model_validation_service import ModelValidationService
from app.services.model_api_generator import ModelAPIGenerator
from app.models.ml_models import MLModel, MLModelVersion, MLModelAPI, ModelStatus
from app.models.core import User
from app.utils.permissions import login_required, get_current_user


ml_models_bp = Blueprint('ml_models', __name__)


def get_current_user_id():
    """Get current user ID from session"""
    if 'user_id' in session:
        return session['user_id']
    # Fallback for API calls with header
    return request.headers.get('X-User-ID', type=int)


# =============================================================================
# Model Management Endpoints
# =============================================================================

# =============================================================================
# Web UI Routes
# =============================================================================

@ml_models_bp.route('/ml-models')
def index():
    """ML Models Management Dashboard"""
    try:
        # Check ML environment status
        from app.services.ml_model_environment import get_ml_model_environment_manager
        env_mgr = get_ml_model_environment_manager()
        env_status = env_mgr.get_status_info()
        env_ready = env_status.get('status') == 'ready'
        
        if not env_ready:
            flash('ML Environment not ready. Please create the environment first to use ML features.', 'warning')
        
        service = MLModelService()
        
        # Get all models and public models
        all_models, total = service.list_models(limit=100)
        public_models, public_total = service.list_models(is_public=True, limit=20)
        
        # Get stats
        stats = {
            'total_models': total,
            'my_models': total,
            'public_models': public_total,
            'active_models': len([m for m in all_models if m.status == 'active']),
            'validated_models': len([m for m in all_models if m.validation_status == 'passed'])
        }
        
        return render_template('ml_models/index.html',
                              user_models=all_models,
                              public_models=public_models,
                              stats=stats,
                              env_ready=env_ready,
                              env_status=env_status)
    except Exception as e:
        flash(f'Error loading models: {str(e)}', 'danger')
        return render_template('ml_models/index.html',
                              user_models=[],
                              public_models=[],
                              stats={},
                              env_ready=False,
                              env_status={})


@ml_models_bp.route('/ml-models/upload')
def upload_page():
    """Upload model page"""
    return render_template('ml_models/upload.html')


@ml_models_bp.route('/ml-models/settings')
def settings():
    """ML Model Settings - Directory Management and Environment"""
    try:
        from app.services.ml_model_directory_manager import get_ml_model_directory_manager
        dir_mgr = get_ml_model_directory_manager()
        directories = dir_mgr.get_directories()
        
        # Get cache stats
        from app.services.ml_model_cache import get_ml_model_cache
        cache = get_ml_model_cache()
        cache_stats = cache.get_stats()
        
        # Get ML environment status
        from app.services.ml_model_environment import get_ml_model_environment_manager
        env_mgr = get_ml_model_environment_manager()
        env_status = env_mgr.get_status_info()
        
        return render_template('ml_models/settings.html',
                              directories=directories,
                              cache_stats=cache_stats,
                              env_status=env_status)
    except Exception as e:
        flash(f'Error loading settings: {str(e)}', 'danger')
        return render_template('ml_models/settings.html',
                              directories=[],
                              cache_stats={},
                              env_status={})


@ml_models_bp.route('/api/v1/ml-models/settings/directories', methods=['GET'])
def get_directories():
    """Get ML model directories"""
    try:
        from app.services.ml_model_directory_manager import get_ml_model_directory_manager
        dir_mgr = get_ml_model_directory_manager()
        directories = dir_mgr.get_directories()
        
        return jsonify({
            'success': True,
            'data': [d.to_dict() for d in directories]
        })
    except Exception as e:
        return jsonify({'success': False, 'error': str(e)}), 500


@ml_models_bp.route('/api/v1/ml-models/settings/directories', methods=['POST'])
def add_directory():
    """Add new ML model directory"""
    try:
        
        from app.services.ml_model_directory_manager import get_ml_model_directory_manager, MLModelDirectory
        dir_mgr = get_ml_model_directory_manager()
        
        data = request.get_json()
        directory = MLModelDirectory(
            id=data.get('id'),
            name=data.get('name'),
            path=data.get('path'),
            enabled=data.get('enabled', True),
            max_size_gb=data.get('max_size_gb'),
            description=data.get('description'),
            priority=data.get('priority', 0)
        )
        
        success = dir_mgr.add_directory(directory)
        if not success:
            return jsonify({'success': False, 'error': 'Directory ID already exists'}), 400
        
        return jsonify({'success': True, 'data': directory.to_dict()})
    except Exception as e:
        return jsonify({'success': False, 'error': str(e)}), 500


@ml_models_bp.route('/api/v1/ml-models/settings/cache/clear', methods=['POST'])
def clear_cache():
    """Clear model cache"""
    try:
        from app.services.ml_model_cache import get_ml_model_cache
        cache = get_ml_model_cache()
        cache.clear()
        
        return jsonify({'success': True, 'message': 'Cache cleared successfully'})
    except Exception as e:
        return jsonify({'success': False, 'error': str(e)}), 500


@ml_models_bp.route('/api/v1/ml-models/settings/environment/setup', methods=['POST'])
def setup_ml_environment():
    """Setup ML model environment"""
    try:
        
        from app.services.ml_model_environment import get_ml_model_environment_manager
        env_mgr = get_ml_model_environment_manager()
        
        data = request.get_json() or {}
        install_optional = data.get('install_optional', False)
        use_gpu = data.get('use_gpu', False)
        
        result = env_mgr.setup_environment(
            install_optional=install_optional,
            use_gpu=use_gpu
        )
        
        return jsonify(result)
    except Exception as e:
        return jsonify({'success': False, 'error': str(e)}), 500


@ml_models_bp.route('/api/v1/ml-models/settings/environment/status', methods=['GET'])
def get_ml_environment_status():
    """Get ML environment status"""
    try:
        from app.services.ml_model_environment import get_ml_model_environment_manager
        env_mgr = get_ml_model_environment_manager()
        status = env_mgr.get_status_info()
        
        return jsonify({'success': True, 'data': status})
    except Exception as e:
        return jsonify({'success': False, 'error': str(e)}), 500


@ml_models_bp.route('/api/v1/ml-models/settings/environment/install-packages', methods=['POST'])
def install_ml_packages():
    """Install packages in ML environment"""
    try:
        
        from app.services.ml_model_environment import get_ml_model_environment_manager
        env_mgr = get_ml_model_environment_manager()
        
        data = request.get_json() or {}
        packages = data.get('packages', [])
        use_gpu = data.get('use_gpu', False)
        
        if not packages:
            # Install required packages
            result = env_mgr.install_packages(use_gpu=use_gpu)
        else:
            result = env_mgr.install_packages(packages=packages, use_gpu=use_gpu)
        
        return jsonify(result)
    except Exception as e:
        return jsonify({'success': False, 'error': str(e)}), 500


@ml_models_bp.route('/ml-models/marketplace')
def marketplace():
    """Model Marketplace - Browse public models"""
    try:
        service = MLModelService()
        
        # Get query parameters
        category = request.args.get('category')
        search = request.args.get('search', '')
        sort_by = request.args.get('sort', 'created_at')  # created_at, name, popularity
        
        # Get public models
        public_models, total = service.list_models(
            is_public=True,
            category=category,
            limit=50
        )
        
        # Filter by search term
        if search:
            search_lower = search.lower()
            public_models = [m for m in public_models 
                            if search_lower in m.name.lower() 
                            or (m.description and search_lower in m.description.lower())
                            or any(search_lower in tag.lower() for tag in (m.tags_list or []))]
        
        # Sort
        if sort_by == 'name':
            public_models.sort(key=lambda x: x.name.lower())
        elif sort_by == 'popularity':
            # Sort by validation status and creation date
            public_models.sort(key=lambda x: (
                x.validation_status == 'passed',
                x.created_at or ''
            ), reverse=True)
        
        # Get categories for filter
        all_models, _ = service.list_models(is_public=True, limit=1000)
        categories = sorted(set([m.category for m in all_models if m.category]))
        
        return render_template('ml_models/marketplace.html',
                              models=public_models,
                              categories=categories,
                              current_category=category,
                              search_term=search,
                              sort_by=sort_by)
    except Exception as e:
        flash(f'Error loading marketplace: {str(e)}', 'danger')
        return render_template('ml_models/marketplace.html',
                              models=[],
                              categories=[])


@ml_models_bp.route('/ml-models/<model_id>')
def model_detail(model_id: str):
    """Model detail page"""
    try:
        service = MLModelService()
        
        model = service.get_model(model_id)
        if not model:
            flash('Model not found', 'danger')
            return redirect(url_for('ml_models.index'))
        
        # Get versions
        versions = service.get_model_versions(model_id)
        
        # Get validation history
        validation_service = ModelValidationService()
        validations = validation_service.get_validation_history(model_id)
        
        # Get API info
        api = MLModelAPI.query.filter_by(model_id=model_id, is_active=True).first()
        
        # Get usage stats
        from app.models.ml_models import MLModelUsageLog
        usage_logs = MLModelUsageLog.query.filter_by(model_id=model_id).order_by(
            MLModelUsageLog.created_at.desc()
        ).limit(100).all()
        
        return render_template('ml_models/detail.html',
                              model=model,
                              versions=versions,
                              validations=validations,
                              api=api,
                              usage_logs=usage_logs)
    except Exception as e:
        flash(f'Error loading model: {str(e)}', 'danger')
        return redirect(url_for('ml_models.index'))


# =============================================================================
# API Endpoints (with authentication)
# =============================================================================

@ml_models_bp.route('/api/v1/ml-models', methods=['POST'])
def upload_model():
    """Upload a new ML model (accessible from ML Studio and other services)"""
    try:
        service = MLModelService()
        user = get_current_user()
        user_id = user.id if user else 'mlstudio'  # Default user for API uploads
        
        # Get form data
        if 'file' not in request.files:
            return jsonify({'success': False, 'error': 'No file provided'}), 400
        
        file = request.files['file']
        if file.filename == '':
            return jsonify({'success': False, 'error': 'No file selected'}), 400
        
        # Get metadata
        name = request.form.get('name', file.filename)
        description = request.form.get('description')
        model_type = request.form.get('model_type', 'sklearn')
        framework = request.form.get('framework')
        category = request.form.get('category')
        is_public = request.form.get('is_public', 'false').lower() == 'true'
        requirements = request.form.get('requirements')
        python_version = request.form.get('python_version')
        
        # Parse tags
        tags_str = request.form.get('tags', '[]')
        try:
            tags = json.loads(tags_str) if isinstance(tags_str, str) else tags_str
        except:
            tags = []
        
        # Read file data
        file_data = file.read()
        
        # Upload model
        result = service.upload_model(
            file_data=file_data,
            filename=secure_filename(file.filename),
            name=name,
            owner_id=user_id,
            model_type=model_type,
            description=description,
            framework=framework,
            category=category,
            tags=tags,
            is_public=is_public,
            requirements=requirements,
            python_version=python_version
        )
        
        # Start validation in background
        validation_service = ModelValidationService()
        validation_id = validation_service.validate_model(
            result.model_id,
            result.version_id,
            user_id
        )
        
        return jsonify({
            'success': True,
            'model_id': result.model_id,
            'version_id': result.version_id,
            'validation_id': validation_id,
            'status': result.status,
            'message': result.message
        }), 201
        
    except Exception as e:
        return jsonify({'success': False, 'error': str(e)}), 500


@ml_models_bp.route('/api/v1/ml-models', methods=['GET'])
def list_models():
    """List all models with filtering"""
    try:
        service = MLModelService()
        
        # Get query parameters
        owner_id = request.args.get('owner_id', type=int)
        category = request.args.get('category')
        is_public = request.args.get('is_public', type=bool)
        status = request.args.get('status')
        limit = request.args.get('limit', 100, type=int)
        offset = request.args.get('offset', 0, type=int)
        
        models, total = service.list_models(
            owner_id=owner_id,
            category=category,
            is_public=is_public,
            status=status,
            limit=limit,
            offset=offset
        )
        
        # Get API endpoints for each model
        model_list = []
        for model in models:
            model_dict = model.to_dict()
            # Get active API endpoint
            api = MLModelAPI.query.filter_by(model_id=model.id, is_active=True).first()
            if api:
                model_dict['api_endpoint'] = api.endpoint_path
            else:
                model_dict['api_endpoint'] = f'/api/v1/ml-models/{model.id}/predict'
            model_list.append(model_dict)
        
        return jsonify({
            'success': True,
            'data': model_list,
            'total': total,
            'limit': limit,
            'offset': offset
        })
        
    except Exception as e:
        return jsonify({'success': False, 'error': str(e)}), 500


@ml_models_bp.route('/api/v1/ml-models/<model_id>', methods=['GET'])
def get_model(model_id: str):
    """Get model details"""
    try:
        service = MLModelService()
        
        model = service.get_model(model_id)
        if not model:
            return jsonify({'success': False, 'error': 'Model not found'}), 404
        
        model_dict = model.to_dict()
        
        # Get current version
        if model.current_version_id:
            version = MLModelVersion.query.get(model.current_version_id)
            if version:
                model_dict['current_version'] = version.to_dict()
        
        # Get all versions
        versions = service.get_model_versions(model_id)
        model_dict['versions'] = [v.to_dict() for v in versions]
        
        # Get API endpoint
        api = MLModelAPI.query.filter_by(model_id=model_id, is_active=True).first()
        if api:
            model_dict['api_endpoint'] = api.endpoint_path
            model_dict['api_id'] = api.id
        
        # Get usage stats (simplified)
        from app.models.ml_models import MLModelUsageLog
        usage_logs = MLModelUsageLog.query.filter_by(model_id=model_id).all()
        total_calls = len(usage_logs)
        successful_calls = len([l for l in usage_logs if l.response_status == 200])
        avg_response_time = sum([l.response_time_ms or 0 for l in usage_logs]) / total_calls if total_calls > 0 else 0
        
        model_dict['usage_stats'] = {
            'total_calls': total_calls,
            'successful_calls': successful_calls,
            'success_rate': successful_calls / total_calls if total_calls > 0 else 0,
            'avg_response_time_ms': round(avg_response_time, 2)
        }
        
        return jsonify({'success': True, 'data': model_dict})
        
    except Exception as e:
        return jsonify({'success': False, 'error': str(e)}), 500


@ml_models_bp.route('/api/v1/ml-models/<model_id>', methods=['DELETE'])
@login_required
def delete_model(model_id: str):
    """Delete a model"""
    try:
        service = MLModelService()
        user = get_current_user()
        user_id = user.id if user else None
        
        if not user_id:
            return jsonify({'success': False, 'error': 'Authentication required'}), 401
        
        success = service.delete_model(model_id, user_id)
        if not success:
            return jsonify({'success': False, 'error': 'Model not found'}), 404
        
        return jsonify({'success': True, 'message': 'Model deleted successfully'})
        
    except PermissionError as e:
        return jsonify({'success': False, 'error': str(e)}), 403
    except Exception as e:
        return jsonify({'success': False, 'error': str(e)}), 500


@ml_models_bp.route('/api/v1/ml-models/<model_id>', methods=['PATCH'])
@login_required
def update_model(model_id: str):
    """Update model metadata"""
    try:
        service = MLModelService()
        user = get_current_user()
        user_id = user.id if user else None
        
        if not user_id:
            return jsonify({'success': False, 'error': 'Authentication required'}), 401
        
        data = request.get_json() or {}
        
        # Build update dict
        updates = {}
        if 'name' in data:
            updates['name'] = data['name']
        if 'description' in data:
            updates['description'] = data['description']
        if 'category' in data:
            updates['category'] = data['category']
        if 'is_public' in data:
            updates['is_public'] = data['is_public']
        if 'tags' in data:
            updates['tags_list'] = data['tags']
        if 'metadata' in data:
            updates['metadata'] = data['metadata']
        
        success = service.update_model(model_id, user_id, **updates)
        if not success:
            return jsonify({'success': False, 'error': 'Model not found'}), 404
        
        return jsonify({'success': True, 'message': 'Model updated successfully'})
        
    except PermissionError as e:
        return jsonify({'success': False, 'error': str(e)}), 403
    except Exception as e:
        return jsonify({'success': False, 'error': str(e)}), 500


# =============================================================================
# Validation Endpoints
# =============================================================================

@ml_models_bp.route('/api/v1/ml-models/<model_id>/validate', methods=['POST'])
def validate_model(model_id: str):
    """Start validation for a model"""
    try:
        validation_service = ModelValidationService()
        user = get_current_user()
        user_id = user.id if user else None
        
        if not user_id:
            return jsonify({'success': False, 'error': 'Authentication required'}), 401
        
        model = MLModel.query.get(model_id)
        if not model:
            return jsonify({'success': False, 'error': 'Model not found'}), 404
        
        version_id = request.json.get('version_id') if request.json else None
        if not version_id:
            version_id = model.current_version_id
        
        if not version_id:
            return jsonify({'success': False, 'error': 'No version specified'}), 400
        
        validation_id = validation_service.validate_model(model_id, version_id, user_id)
        
        return jsonify({
            'success': True,
            'validation_id': validation_id,
            'status': 'running',
            'message': f'Validation started. Check status at /api/v1/ml-models/{model_id}/validations/{validation_id}'
        })
        
    except Exception as e:
        return jsonify({'success': False, 'error': str(e)}), 500


@ml_models_bp.route('/api/v1/ml-models/<model_id>/validations/<validation_id>', methods=['GET'])
def get_validation_status(model_id: str, validation_id: str):
    """Get validation status"""
    try:
        validation_service = ModelValidationService()
        
        # Get latest validation report
        version = MLModelVersion.query.filter_by(model_id=model_id).order_by(
            MLModelVersion.validation_date.desc()
        ).first()
        
        if not version or not version.validation_report:
            return jsonify({
                'success': True,
                'data': {
                    'id': validation_id,
                    'status': 'pending',
                    'message': 'Validation in progress'
                }
            })
        
        report = version.validation_report
        if report.get('validation_id') == validation_id:
            return jsonify({'success': True, 'data': report})
        else:
            # Return latest report
            return jsonify({'success': True, 'data': report})
        
    except Exception as e:
        return jsonify({'success': False, 'error': str(e)}), 500


# =============================================================================
# Prediction Endpoints
# =============================================================================

@ml_models_bp.route('/api/v1/ml-models/<model_id>/predict', methods=['POST'])
def predict(model_id: str):
    """Make a prediction using the model (public API for apps)"""
    start_time = time.time()
    user = get_current_user()
    user_id = user.id if user else None
    
    try:
        service = MLModelService()
        
        # Check permissions
        if not service.check_permission(model_id, user_id or 0, 'execute'):
            return jsonify({'success': False, 'error': 'Permission denied'}), 403
        
        model = service.get_model(model_id)
        if not model:
            return jsonify({'success': False, 'error': 'Model not found'}), 404
        
        if model.status != ModelStatus.ACTIVE.value and model.status != ModelStatus.VALIDATED.value:
            return jsonify({'success': False, 'error': 'Model is not active'}), 400
        
        # Get request data
        data = request.get_json()
        if not data or 'data' not in data:
            return jsonify({'success': False, 'error': 'Missing data field'}), 400
        
        input_data = data['data']
        
        # Get active version
        version = MLModelVersion.query.get(model.current_version_id)
        if not version:
            return jsonify({'success': False, 'error': 'No active version found'}), 404
        
        # Run model prediction in isolated ML environment (required)
        try:
            prediction = _run_model_prediction(version, input_data)
        except FileNotFoundError as e:
            response_time = int((time.time() - start_time) * 1000)
            service.log_usage(
                model_id, None, user_id, '/predict', input_data,
                404, response_time, str(e), request.remote_addr
            )
            return jsonify({'success': False, 'error': 'Model file not found. Please contact the model owner.'}), 404
        except ImportError as e:
            response_time = int((time.time() - start_time) * 1000)
            service.log_usage(
                model_id, None, user_id, '/predict', input_data,
                503, response_time, str(e), request.remote_addr
            )
            return jsonify({'success': False, 'error': f'Required library not installed: {str(e)}. Please install the required dependencies.'}), 503
        except ValueError as e:
            response_time = int((time.time() - start_time) * 1000)
            service.log_usage(
                model_id, None, user_id, '/predict', input_data,
                400, response_time, str(e), request.remote_addr
            )
            return jsonify({'success': False, 'error': f'Invalid input data: {str(e)}'}), 400
        except Exception as e:
            response_time = int((time.time() - start_time) * 1000)
            error_msg = str(e)
            # Don't expose internal errors in production
            if 'Prediction failed' not in error_msg:
                error_msg = 'Prediction failed. Please check your input data format and try again.'
            service.log_usage(
                model_id, None, user_id, '/predict', input_data,
                500, response_time, error_msg, request.remote_addr
            )
            return jsonify({'success': False, 'error': error_msg}), 500
        
        response_time = int((time.time() - start_time) * 1000)
        
        # Log usage
        api = MLModelAPI.query.filter_by(model_id=model_id, is_active=True).first()
        service.log_usage(
            model_id, api.id if api else None, user_id, '/predict', input_data,
            200, response_time, None, request.remote_addr
        )
        
        return jsonify({
            'success': True,
            'prediction': prediction,
            'model_id': model_id,
            'version': version.version,
            'inference_time_ms': response_time
        })
        
    except Exception as e:
        response_time = int((time.time() - start_time) * 1000)
        service = MLModelService()
        service.log_usage(
            model_id, None, user_id, '/predict', None,
            500, response_time, str(e), request.remote_addr
        )
        return jsonify({'success': False, 'error': str(e)}), 500


@ml_models_bp.route('/api/v1/ml-models/<model_id>/predict/batch', methods=['POST'])
def predict_batch(model_id: str):
    """Make batch predictions (public API for apps)"""
    start_time = time.time()
    user = get_current_user()
    user_id = user.id if user else None
    
    try:
        service = MLModelService()
        
        # Check permissions
        if not service.check_permission(model_id, user_id or 0, 'execute'):
            return jsonify({'success': False, 'error': 'Permission denied'}), 403
        
        model = service.get_model(model_id)
        if not model:
            return jsonify({'success': False, 'error': 'Model not found'}), 404
        
        # Get request data
        data = request.get_json()
        if not data or 'data' not in data:
            return jsonify({'success': False, 'error': 'Missing data field'}), 400
        
        input_data_list = data['data']
        if not isinstance(input_data_list, list):
            return jsonify({'success': False, 'error': 'Data must be a list'}), 400
        
        # Get active version
        version = MLModelVersion.query.get(model.current_version_id)
        if not version:
            return jsonify({'success': False, 'error': 'No active version found'}), 404
        
        # Run batch predictions
        predictions = []
        for input_data in input_data_list:
            try:
                pred = _run_model_prediction(version, input_data)
                predictions.append(pred)
            except Exception as e:
                predictions.append({'error': str(e)})
        
        response_time = int((time.time() - start_time) * 1000)
        
        # Log usage
        api = MLModelAPI.query.filter_by(model_id=model_id, is_active=True).first()
        service.log_usage(
            model_id, api.id if api else None, user_id, '/predict/batch', 
            {'batch_size': len(input_data_list)},
            200, response_time, None, request.remote_addr
        )
        
        return jsonify({
            'success': True,
            'predictions': predictions,
            'model_id': model_id,
            'version': version.version,
            'total_inference_time_ms': response_time
        })
        
    except Exception as e:
        return jsonify({'success': False, 'error': str(e)}), 500


def _run_model_prediction(version: MLModelVersion, input_data: dict):
    """
    Run model prediction - REQUIRES isolated ML environment.
    Environment will be auto-created if it doesn't exist.
    """
    file_path = Path(version.file_path)
    
    if not file_path.exists():
        raise FileNotFoundError(f"Model file not found: {file_path}")
    
    model = MLModel.query.get(version.model_id)
    if not model:
        raise ValueError("Model not found")
    
    model_type = model.model_type.lower()
    
    # Get ML environment manager
    from app.services.ml_model_environment import get_ml_model_environment_manager
    env_mgr = get_ml_model_environment_manager()
    
    # Ensure environment exists - auto-create if needed
    if not env_mgr.is_ready:
        # Try to create environment if it doesn't exist (auto-install packages)
        if env_mgr.status.value == 'not_created':
            create_result = env_mgr.create_environment(auto_install_packages=True)
            if not create_result.get('success'):
                raise RuntimeError(
                    f"ML environment creation failed. Error: {create_result.get('error', 'Unknown error')}. "
                    f"Please set up the environment manually via Settings → ML Models → Environment Setup."
                )
            # Check if ready after creation
            if env_mgr.is_ready:
                pass  # Success, continue
            elif create_result.get('warning'):
                # Environment created but packages had issues
                raise RuntimeError(
                    f"ML environment created but package installation had issues: {create_result.get('warning')}. "
                    f"Please complete setup via Settings → ML Models → Environment Setup."
                )
            else:
                raise RuntimeError(
                    "ML environment created but not ready. Please complete setup via Settings → ML Models → Environment Setup."
                )
        
        # Try to install required packages if environment exists but packages not installed
        elif env_mgr.status.value == 'created':
            install_result = env_mgr.install_packages()
            if not install_result.get('success'):
                raise RuntimeError(
                    f"ML environment packages not installed. Error: {install_result.get('error', 'Unknown error')}. "
                    f"Please set up the environment via Settings → ML Models → Environment Setup."
                )
            # Check again if ready
            if not env_mgr.is_ready:
                raise RuntimeError(
                    "ML environment packages installed but environment not ready. "
                    "Please check Settings → ML Models → Environment Setup."
                )
        
        # Final check
        if not env_mgr.is_ready:
            raise RuntimeError(
                "ML environment is not ready. Please set up the environment via Settings → ML Models → Environment Setup. "
                "This ensures models run in isolation from the main application."
            )
    
    # Use isolated environment via subprocess (REQUIRED - no fallback)
    from app.services.ml_model_subprocess import run_model_prediction_in_env
    result = run_model_prediction_in_env(
        str(file_path),
        model_type,
        input_data
    )
    
    if not result.get('success'):
        error_msg = result.get('error', 'Unknown error')
        raise RuntimeError(f"Prediction failed in isolated environment: {error_msg}")
    
    return result.get('prediction')


# Note: Direct model loading functions removed - all models now run in isolated ML environment
# See app/services/ml_model_subprocess.py for model loading in isolated environment




# =============================================================================
# API Generation Endpoints
# =============================================================================

@ml_models_bp.route('/api/v1/ml-models/<model_id>/generate-api', methods=['POST'])
def generate_api(model_id: str):
    """Generate API endpoint for a model"""
    try:
        api_generator = ModelAPIGenerator()
        user = get_current_user()
        user_id = user.id if user else None
        
        if not user_id:
            return jsonify({'success': False, 'error': 'Authentication required'}), 401
        
        model = MLModel.query.get(model_id)
        if not model:
            return jsonify({'success': False, 'error': 'Model not found'}), 404
        
        # Check permissions
        service = MLModelService()
        if not service.check_permission(model_id, user_id, 'admin'):
            return jsonify({'success': False, 'error': 'Permission denied'}), 403
        
        data = request.get_json() or {}
        version_id = data.get('version_id') or model.current_version_id
        endpoint_path = data.get('endpoint_path')
        rate_limit = data.get('rate_limit_per_minute')
        
        result = api_generator.generate_api(
            model_id, version_id, endpoint_path, rate_limit
        )
        
        return jsonify({'success': True, **result})
        
    except Exception as e:
        return jsonify({'success': False, 'error': str(e)}), 500


@ml_models_bp.route('/api/v1/ml-models/<model_id>/docs', methods=['GET'])
def get_api_docs(model_id: str):
    """Get API documentation (OpenAPI schema)"""
    try:
        api_generator = ModelAPIGenerator()
        docs = api_generator.get_api_docs(model_id)
        
        if not docs:
            return jsonify({'success': False, 'error': 'API documentation not found'}), 404
        
        return jsonify(docs)
        
    except Exception as e:
        return jsonify({'success': False, 'error': str(e)}), 500


# =============================================================================
# ML Environment Management Routes (using EnvironmentManager)
# =============================================================================

@ml_models_bp.route('/ml-models/environment')
def environment_page():
    """ML Environment Management Page"""
    return render_template('ml_models/environment.html')


@ml_models_bp.route('/api/v1/ml-models/env/status', methods=['GET'])
def get_env_status():
    """Get ML environment status"""
    try:
        from app.services.ml_model_environment import get_ml_model_environment_manager
        env_mgr = get_ml_model_environment_manager()
        status = env_mgr.get_status_info()
        return jsonify({'success': True, 'data': status})
    except Exception as e:
        return jsonify({'success': False, 'error': str(e)}), 500


@ml_models_bp.route('/api/v1/ml-models/env/create', methods=['POST'])
def create_ml_env():
    """Create ML environment using EnvironmentManager"""
    try:
        from app.services.ml_model_environment import get_ml_model_environment_manager
        env_mgr = get_ml_model_environment_manager()
        
        # Create environment without auto-installing packages
        result = env_mgr.create_environment(auto_install_packages=False)
        return jsonify(result)
    except Exception as e:
        return jsonify({'success': False, 'error': str(e)}), 500


@ml_models_bp.route('/api/v1/ml-models/env/recreate', methods=['POST'])
def recreate_ml_env():
    """Recreate ML environment"""
    try:
        from app.services.ml_model_environment import get_ml_model_environment_manager, MLEnvStatus
        from app.services.environment_manager import EnvironmentManager
        
        env_mgr = get_ml_model_environment_manager()
        
        # Delete existing environment
        try:
            base_env_mgr = EnvironmentManager()
            base_env_mgr.delete_environment(env_mgr._env_name)
        except:
            pass
        
        # Reset state
        env_mgr._status = MLEnvStatus.NOT_CREATED
        env_mgr._installed_packages = []
        env_mgr._error_message = None
        
        # Create new environment
        result = env_mgr.create_environment(auto_install_packages=False)
        return jsonify(result)
    except Exception as e:
        return jsonify({'success': False, 'error': str(e)}), 500


@ml_models_bp.route('/api/v1/ml-models/env/delete', methods=['DELETE'])
def delete_ml_env():
    """Delete ML environment"""
    try:
        from app.services.ml_model_environment import get_ml_model_environment_manager, MLEnvStatus
        from app.services.environment_manager import EnvironmentManager
        
        env_mgr = get_ml_model_environment_manager()
        
        # Delete using EnvironmentManager
        base_env_mgr = EnvironmentManager()
        result = base_env_mgr.delete_environment(env_mgr._env_name)
        
        if result:
            # Reset state
            env_mgr._status = MLEnvStatus.NOT_CREATED
            env_mgr._installed_packages = []
            env_mgr._error_message = None
            env_mgr._save_config()
            return jsonify({'success': True, 'message': 'Environment deleted successfully'})
        else:
            return jsonify({'success': False, 'error': 'Failed to delete environment'}), 500
    except Exception as e:
        return jsonify({'success': False, 'error': str(e)}), 500


@ml_models_bp.route('/api/v1/ml-models/env/install-required', methods=['POST'])
def install_required_packages():
    """Install required ML packages"""
    try:
        from app.services.ml_model_environment import get_ml_model_environment_manager
        env_mgr = get_ml_model_environment_manager()
        
        # Install required packages (numpy, scipy, pandas, scikit-learn, joblib)
        result = env_mgr.install_packages()
        return jsonify(result)
    except Exception as e:
        return jsonify({'success': False, 'error': str(e)}), 500


@ml_models_bp.route('/api/v1/ml-models/env/install-framework', methods=['POST'])
def install_framework():
    """Install a specific ML framework"""
    try:
        from app.services.ml_model_environment import get_ml_model_environment_manager, ML_OPTIONAL_PACKAGES
        env_mgr = get_ml_model_environment_manager()
        
        data = request.get_json() or {}
        framework = data.get('framework', '').lower()
        use_gpu = data.get('use_gpu', False)
        
        # Map framework names to packages
        # Note: tensorflow-gpu is deprecated - modern tensorflow includes GPU support automatically
        framework_packages = {
            'sklearn': ['scikit-learn'],
            'scikit-learn': ['scikit-learn'],
            'tensorflow': ['tensorflow'],  # GPU support included automatically in TF 2.x
            'pytorch': ['torch', 'torchvision'],
            'xgboost': ['xgboost'],
            'onnxruntime': ['onnxruntime'],
            'lightgbm': ['lightgbm'],
            'joblib': ['joblib'],
            'keras': ['keras'],
        }
        
        packages = framework_packages.get(framework)
        if not packages:
            return jsonify({'success': False, 'error': f'Unknown framework: {framework}'}), 400
        
        result = env_mgr.install_packages(packages=packages, use_gpu=use_gpu)
        return jsonify(result)
    except Exception as e:
        return jsonify({'success': False, 'error': str(e)}), 500


@ml_models_bp.route('/api/v1/ml-models/env/packages', methods=['GET'])
def get_env_packages():
    """Get installed packages in ML environment"""
    try:
        from app.services.ml_model_environment import get_ml_model_environment_manager
        env_mgr = get_ml_model_environment_manager()
        
        # Get packages using pip list in the environment
        python_path = env_mgr.get_python_executable()
        if not python_path:
            return jsonify({'success': True, 'packages': []})
        
        import subprocess
        try:
            result = subprocess.run(
                [python_path, '-m', 'pip', 'list', '--format=json'],
                capture_output=True,
                text=True,
                timeout=30
            )
            if result.returncode == 0:
                packages = json.loads(result.stdout)
                return jsonify({'success': True, 'packages': packages})
        except:
            pass
        
        # Fallback to stored packages
        status = env_mgr.get_status_info()
        return jsonify({'success': True, 'packages': status.get('installed_packages', [])})
    except Exception as e:
        return jsonify({'success': False, 'error': str(e)}), 500