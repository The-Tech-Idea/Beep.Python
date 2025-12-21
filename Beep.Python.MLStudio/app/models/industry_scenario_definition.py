"""
Industry Scenario Definition Model - Store scenario definitions (use cases, dataset ideas, competition ideas)
Note: This is different from IndustryScenarioProgress which tracks user progress through scenarios
"""
from app import db
from datetime import datetime
from sqlalchemy import Index
import json


class IndustryScenarioDefinition(db.Model):
    """Model for industry scenario definitions (use cases, dataset ideas, competition ideas)"""
    __tablename__ = 'industry_scenario_definitions'
    
    id = db.Column(db.Integer, primary_key=True)
    industry = db.Column(db.String(50), nullable=False, index=True)
    scenario_type = db.Column(db.String(20), nullable=False)  # 'use_case', 'dataset_idea', 'competition_idea'
    title = db.Column(db.String(200), nullable=False)
    description = db.Column(db.Text, nullable=False)
    details = db.Column(db.Text, nullable=True)  # JSON as text (some DBs don't support JSON natively)
    icon_name = db.Column(db.String(255), nullable=True)
    priority = db.Column(db.Integer, default=0)
    is_active = db.Column(db.Boolean, default=True)
    created_at = db.Column(db.DateTime, default=datetime.utcnow)
    updated_at = db.Column(db.DateTime, default=datetime.utcnow, onupdate=datetime.utcnow)
    
    # Index for faster queries
    __table_args__ = (
        Index('idx_industry_type', 'industry', 'scenario_type'),
        Index('idx_industry_priority', 'industry', 'priority'),
    )
    
    def get_details_dict(self):
        """Get details as dictionary"""
        if self.details:
            try:
                return json.loads(self.details)
            except (json.JSONDecodeError, TypeError):
                return {}
        return {}
    
    def set_details_dict(self, data: dict):
        """Set details from dictionary"""
        self.details = json.dumps(data) if data else None
    
    def to_dict(self):
        """Convert to dictionary"""
        return {
            'id': self.id,
            'industry': self.industry,
            'scenario_type': self.scenario_type,
            'title': self.title,
            'description': self.description,
            'details': self.get_details_dict(),
            'icon_name': self.icon_name,
            'priority': self.priority,
            'is_active': self.is_active,
            'created_at': self.created_at.isoformat() if self.created_at else None,
            'updated_at': self.updated_at.isoformat() if self.updated_at else None
        }
    
    def __repr__(self):
        return f'<IndustryScenarioDefinition {self.id}: {self.industry} - {self.scenario_type} - {self.title}>'
