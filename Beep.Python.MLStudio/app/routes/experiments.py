"""
Experiments Routes
"""
from datetime import datetime
from flask import Blueprint, render_template, request, jsonify
from app import db
from app.models.project import MLProject
from app.models.experiment import Experiment

experiments_bp = Blueprint('experiments', __name__)


@experiments_bp.route('/<int:experiment_id>')
def detail(experiment_id):
    """Experiment detail view"""
    experiment = Experiment.query.get_or_404(experiment_id)
    project = MLProject.query.get_or_404(experiment.project_id)
    
    # Calculate running duration if running
    running_minutes = 0
    if experiment.status == 'running' and experiment.started_at:
        running_minutes = (datetime.utcnow() - experiment.started_at).total_seconds() / 60
    
    return render_template('experiments/detail.html', 
                         experiment=experiment, 
                         project=project,
                         running_minutes=running_minutes,
                         now=datetime.utcnow())


@experiments_bp.route('/<int:project_id>/list')
def list_experiments(project_id):
    """List all experiments for a project"""
    project = MLProject.query.get_or_404(project_id)
    experiments = Experiment.query.filter_by(project_id=project_id).order_by(Experiment.created_at.desc()).all()
    
    return jsonify({
        'success': True,
        'experiments': [exp.to_dict() for exp in experiments]
    })

