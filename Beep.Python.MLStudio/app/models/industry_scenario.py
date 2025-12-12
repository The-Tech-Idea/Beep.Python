"""
Industry Scenario Progress Model
Tracks progress through industry-specific ML scenarios/workflows
"""
from datetime import datetime
from app import db
import json


class IndustryScenarioProgress(db.Model):
    """
    Tracks a project's progress through an industry scenario workflow.
    
    This allows users to:
    - Resume scenarios where they left off
    - Store data collected at each step
    - Track completion status
    """
    __tablename__ = 'industry_scenario_progress'
    
    id = db.Column(db.Integer, primary_key=True)
    project_id = db.Column(db.Integer, db.ForeignKey('ml_projects.id'), nullable=False)
    scenario_id = db.Column(db.String(100), nullable=False)
    
    # Progress tracking
    current_step = db.Column(db.Integer, default=1)
    completed_steps = db.Column(db.Text)  # JSON array of completed step numbers
    
    # Data collected at each step (JSON object keyed by step number)
    step_data = db.Column(db.Text)
    
    # Status: in_progress, completed, abandoned
    status = db.Column(db.String(50), default='in_progress')
    
    # Timestamps
    created_at = db.Column(db.DateTime, default=datetime.utcnow)
    updated_at = db.Column(db.DateTime, default=datetime.utcnow, onupdate=datetime.utcnow)
    
    # Relationships - cascade delete when project is deleted
    project = db.relationship('MLProject', backref=db.backref('scenario_progress', cascade='all, delete-orphan'))
    
    def get_completed_steps(self):
        """Get list of completed step numbers"""
        if self.completed_steps:
            try:
                return json.loads(self.completed_steps)
            except (json.JSONDecodeError, TypeError):
                return []
        return []
    
    def set_completed_steps(self, steps):
        """Set completed steps list"""
        self.completed_steps = json.dumps(steps)
    
    def mark_step_complete(self, step_number):
        """Mark a specific step as complete"""
        completed = self.get_completed_steps()
        if step_number not in completed:
            completed.append(step_number)
            completed.sort()
            self.set_completed_steps(completed)
    
    def get_step_data(self):
        """Get data collected at each step"""
        if self.step_data:
            try:
                return json.loads(self.step_data)
            except (json.JSONDecodeError, TypeError):
                return {}
        return {}
    
    def set_step_data(self, data):
        """Set step data dictionary"""
        self.step_data = json.dumps(data)
    
    def save_step_data(self, step_number, data):
        """Save data for a specific step"""
        all_data = self.get_step_data()
        all_data[str(step_number)] = data
        self.set_step_data(all_data)
    
    def get_data_for_step(self, step_number):
        """Get data saved for a specific step"""
        all_data = self.get_step_data()
        return all_data.get(str(step_number), {})
    
    def is_step_complete(self, step_number):
        """Check if a specific step is complete"""
        return step_number in self.get_completed_steps()
    
    def to_dict(self):
        """Convert to dictionary"""
        return {
            'id': self.id,
            'project_id': self.project_id,
            'scenario_id': self.scenario_id,
            'current_step': self.current_step,
            'completed_steps': self.get_completed_steps(),
            'step_data': self.get_step_data(),
            'status': self.status,
            'created_at': self.created_at.isoformat() if self.created_at else None,
            'updated_at': self.updated_at.isoformat() if self.updated_at else None
        }
    
    def __repr__(self):
        return f'<IndustryScenarioProgress project={self.project_id} scenario={self.scenario_id}>'

