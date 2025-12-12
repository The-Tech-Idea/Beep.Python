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

