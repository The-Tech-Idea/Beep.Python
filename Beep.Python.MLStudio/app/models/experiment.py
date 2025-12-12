"""
Experiment Model
"""
from datetime import datetime
from app import db
import json


class Experiment(db.Model):
    """ML Experiment model"""
    __tablename__ = 'experiments'
    
    id = db.Column(db.Integer, primary_key=True)
    project_id = db.Column(db.Integer, db.ForeignKey('ml_projects.id'), nullable=False)
    name = db.Column(db.String(200), nullable=False)
    description = db.Column(db.Text)
    
    # Model information
    model_type = db.Column(db.String(100))  # RandomForest, NeuralNetwork, etc.
    model_config = db.Column(db.Text)  # JSON string of model parameters
    
    # Training data
    dataset_path = db.Column(db.String(500))
    train_size = db.Column(db.Float)  # Train/test split ratio
    
    # Results
    metrics = db.Column(db.Text)  # JSON string of evaluation metrics
    model_path = db.Column(db.String(500))  # Path to saved model
    
    # Status
    status = db.Column(db.String(50), default='pending')  # pending, running, completed, failed
    error_message = db.Column(db.Text)
    stdout = db.Column(db.Text)  # Standard output from execution
    stderr = db.Column(db.Text)  # Standard error from execution
    
    # Timestamps
    created_at = db.Column(db.DateTime, default=datetime.utcnow)
    started_at = db.Column(db.DateTime)
    completed_at = db.Column(db.DateTime)
    
    def set_model_config(self, config_dict):
        """Set model configuration as JSON"""
        self.model_config = json.dumps(config_dict)
    
    def get_model_config(self):
        """Get model configuration as dictionary"""
        if self.model_config:
            return json.loads(self.model_config)
        return {}
    
    def set_metrics(self, metrics_dict):
        """Set metrics as JSON"""
        self.metrics = json.dumps(metrics_dict)
    
    def get_metrics(self):
        """Get metrics as dictionary"""
        if self.metrics:
            return json.loads(self.metrics)
        return {}
    
    def to_dict(self):
        """Convert to dictionary"""
        return {
            'id': self.id,
            'project_id': self.project_id,
            'name': self.name,
            'description': self.description,
            'model_type': self.model_type,
            'model_config': self.get_model_config(),
            'dataset_path': self.dataset_path,
            'train_size': self.train_size,
            'metrics': self.get_metrics(),
            'model_path': self.model_path,
            'status': self.status,
            'error_message': self.error_message,
            'stdout': self.stdout[:1000] if self.stdout else None,  # Truncate for API responses
            'stderr': self.stderr[:1000] if self.stderr else None,  # Truncate for API responses
            'created_at': self.created_at.isoformat() if self.created_at else None,
            'started_at': self.started_at.isoformat() if self.started_at else None,
            'completed_at': self.completed_at.isoformat() if self.completed_at else None
        }
    
    def __repr__(self):
        return f'<Experiment {self.name}>'

