"""
Models Routes - ML Model operations
"""
from flask import Blueprint, render_template, request, jsonify, flash, redirect, url_for
from app import db, socketio
from app.models.project import MLProject
from app.models.experiment import Experiment
from app.services.ml_service import MLService
from app.services.environment_manager import EnvironmentManager
from flask import current_app
import threading

models_bp = Blueprint('models', __name__)


def get_ml_service():
    """Get ML service instance"""
    env_mgr = EnvironmentManager()
    return MLService(
        projects_folder=current_app.config['PROJECTS_FOLDER'],
        environment_manager=env_mgr
    )


@models_bp.route('/<int:project_id>/train', methods=['POST'])
def train(project_id):
    """Train a model"""
    project = MLProject.query.get_or_404(project_id)
    data = request.get_json()
    
    script_path = data.get('script_path', 'scripts/train.py')
    
    # Create experiment record
    experiment = Experiment(
        project_id=project_id,
        name=data.get('name', 'Training Run'),
        description=data.get('description', ''),
        model_type=data.get('model_type', 'Unknown'),
        status='running'
    )
    experiment.set_model_config(data.get('config', {}))
    from datetime import datetime
    experiment.started_at = datetime.utcnow()
    db.session.add(experiment)
    db.session.commit()
    
    # Train model in background
    def train_async():
        ml_service = get_ml_service()
        result = ml_service.train_model(
            project_id=project_id,
            experiment_id=experiment.id,
            script_path=script_path,
            env_name=project.environment_name
        )
        
        # Update experiment
        experiment.status = 'completed' if result['success'] else 'failed'
        experiment.completed_at = datetime.utcnow()
        if not result['success']:
            experiment.error_message = result['stderr']
        
        # Try to extract metrics from stdout (simplified)
        if result['success']:
            # In a real implementation, you'd parse the output for metrics
            experiment.set_metrics({'status': 'completed'})
        
        db.session.commit()
        
        # Emit WebSocket event with more details
        socketio.emit('training_complete', {
            'experiment_id': experiment.id,
            'success': result['success'],
            'stdout': result.get('stdout', '')[:500],  # First 500 chars
            'stderr': result.get('stderr', '')[:500]
        }, room=f'project_{project_id}')
        
        # Also emit progress updates during training (if needed)
        if result['success']:
            socketio.emit('training_progress', {
                'experiment_id': experiment.id,
                'message': 'Training completed successfully!'
            }, room=f'project_{project_id}')
        else:
            socketio.emit('training_progress', {
                'experiment_id': experiment.id,
                'message': f'Training failed: {result.get("stderr", "Unknown error")[:200]}'
            }, room=f'project_{project_id}')
    
    thread = threading.Thread(target=train_async, daemon=True)
    thread.start()
    
    return jsonify({
        'success': True,
        'experiment_id': experiment.id,
        'message': 'Training started'
    })


@models_bp.route('/<int:project_id>/list')
def list_models(project_id):
    """List all models in a project"""
    project = MLProject.query.get_or_404(project_id)
    ml_service = get_ml_service()
    models = ml_service.list_models(project_id)
    
    return jsonify({
        'success': True,
        'models': models
    })


@models_bp.route('/<int:experiment_id>/submit-to-competition', methods=['POST'])
def submit_to_competition(experiment_id):
    """Submit a trained model to a Community competition"""
    from app.services.community_client import get_community_client
    from app.services.community_connection_service import CommunityConnectionService
    from app.services.auth_service import AuthService
    from pathlib import Path
    
    experiment = Experiment.query.get_or_404(experiment_id)
    project = MLProject.query.get_or_404(experiment.project_id)
    
    # Get current user
    current_user = AuthService.get_current_user()
    if not current_user:
        return jsonify({'success': False, 'error': 'Authentication required'}), 401
    
    # Get request data
    data = request.get_json()
    competition_id = data.get('competition_id')
    if not competition_id:
        return jsonify({'success': False, 'error': 'competition_id is required'}), 400
    
    # Get model file path from experiment
    project_folder = Path(current_app.config['PROJECTS_FOLDER']) / str(project.id)
    model_file_path = None
    
    # Check if experiment has model_path field (stored path)
    if experiment.model_path:
        model_file_path = Path(experiment.model_path)
        if not model_file_path.is_absolute():
            model_file_path = project_folder / model_file_path
    
    # If not found, try common locations
    if not model_file_path or not model_file_path.exists():
        possible_model_paths = [
            project_folder / 'models' / f'model_{experiment_id}.pkl',
            project_folder / 'models' / f'experiment_{experiment_id}.pkl',
            project_folder / 'outputs' / f'model_{experiment_id}.pkl',
            project_folder / 'outputs' / f'experiment_{experiment_id}.pkl',
            project_folder / f'model_{experiment_id}.pkl',
        ]
        
        for path in possible_model_paths:
            if path.exists():
                model_file_path = path
                break
    
    if not model_file_path or not model_file_path.exists():
        return jsonify({
            'success': False,
            'error': f'Model file not found for experiment {experiment_id}. Please ensure the model was saved during training.'
        }), 404
    
    # Get Community connection info and user ID
    # Note: In production, you'd map MLStudio users to Community users
    # For now, we'll use the MLStudio user ID and let Community handle authentication via API key
    # The API key should be configured in Community connection settings
    from app.services.community_connection_service import CommunityConnectionService
    connection_service = CommunityConnectionService()
    
    # Use current user ID as community user ID (assuming same user system)
    # In a production system with separate user databases, you'd map via connection service
    community_user_id = current_user.id
    
    # Get Community client
    client = get_community_client()
    
    # Prepare model metadata
    model_name = data.get('model_name', f"{project.name}_experiment_{experiment_id}")
    model_type = experiment.model_type or data.get('model_type')
    metrics = experiment.get_metrics() if hasattr(experiment, 'get_metrics') else data.get('metrics')
    
    # Submit model to competition
    result = client.submit_model_to_competition(
        competition_id=competition_id,
        model_file_path=str(model_file_path),
        model_name=model_name,
        user_id=community_user_id,
        model_type=model_type,
        framework=data.get('framework'),
        metrics=metrics,
        description=data.get('description', experiment.description),
        input_schema=data.get('input_schema'),
        output_schema=data.get('output_schema'),
        mlstudio_source_id=str(experiment_id)
    )
    
    # Return result (client already formatted the response)
    if result.get('success'):
        return jsonify(result), 200
    else:
        status_code = 400 if result.get('validation_failed') else 500
        return jsonify(result), status_code

