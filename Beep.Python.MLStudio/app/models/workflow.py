"""
Workflow/Pipeline Model
Stores visual ML workflow definitions
"""
from datetime import datetime
from app import db
import json


class Workflow(db.Model):
    """ML Workflow/Pipeline model"""
    __tablename__ = 'workflows'
    
    id = db.Column(db.Integer, primary_key=True)
    project_id = db.Column(db.Integer, db.ForeignKey('ml_projects.id'), nullable=False)
    name = db.Column(db.String(200), nullable=False)
    description = db.Column(db.Text)
    
    # Workflow definition (JSON structure with nodes and edges)
    workflow_data = db.Column(db.Text)  # JSON string containing nodes, edges, and metadata
    
    # Generated Python code from workflow
    generated_code = db.Column(db.Text)
    
    # Status
    status = db.Column(db.String(50), default='draft')  # draft, saved, executed
    
    # Timestamps
    created_at = db.Column(db.DateTime, default=datetime.utcnow)
    updated_at = db.Column(db.DateTime, default=datetime.utcnow, onupdate=datetime.utcnow)
    last_executed_at = db.Column(db.DateTime)
    
    # Relationships - cascade delete when project is deleted
    project = db.relationship('MLProject', backref=db.backref('workflows', cascade='all, delete-orphan'))
    
    def set_workflow_data(self, data_dict):
        """Set workflow data as JSON"""
        self.workflow_data = json.dumps(data_dict)
    
    def get_workflow_data(self):
        """Get workflow data as dictionary"""
        if self.workflow_data:
            return json.loads(self.workflow_data)
        return {
            'nodes': [],
            'edges': [],
            'viewport': {'x': 0, 'y': 0, 'zoom': 1}
        }
    
    def to_dict(self):
        """Convert to dictionary"""
        return {
            'id': self.id,
            'project_id': self.project_id,
            'name': self.name,
            'description': self.description,
            'workflow_data': self.get_workflow_data(),
            'generated_code': self.generated_code,
            'status': self.status,
            'created_at': self.created_at.isoformat() if self.created_at else None,
            'updated_at': self.updated_at.isoformat() if self.updated_at else None,
            'last_executed_at': self.last_executed_at.isoformat() if self.last_executed_at else None
        }
    
    def __repr__(self):
        return f'<Workflow {self.name}>'

