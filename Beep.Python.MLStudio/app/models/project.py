"""
ML Project Model
"""
from datetime import datetime
from app import db
import json


class MLProject(db.Model):
    """ML Project model"""
    __tablename__ = 'ml_projects'
    
    id = db.Column(db.Integer, primary_key=True)
    name = db.Column(db.String(200), nullable=False, unique=True)
    description = db.Column(db.Text)
    template = db.Column(db.String(50), default='custom')  # classification, regression, clustering, etc.
    environment_name = db.Column(db.String(200), nullable=False)  # Virtual environment name in Host Admin
    status = db.Column(db.String(50), default='active')  # active, archived, deleted
    created_at = db.Column(db.DateTime, default=datetime.utcnow)
    updated_at = db.Column(db.DateTime, default=datetime.utcnow, onupdate=datetime.utcnow)
    
    # Project configuration
    framework = db.Column(db.String(50))  # scikit-learn, tensorflow, pytorch, xgboost, etc.
    python_version = db.Column(db.String(20))
    
    # Industry module support
    industry_profile = db.Column(db.String(100))  # Industry profile ID (e.g., 'petroleum', 'finance')
    scenario_id = db.Column(db.String(100))  # Scenario within the profile
    industry_config = db.Column(db.Text)  # JSON - Industry-specific configuration
    
    # Relationships
    experiments = db.relationship('Experiment', backref='project', lazy=True, cascade='all, delete-orphan')
    
    @property
    def experiments_count(self):
        """Get count of experiments for this project"""
        return len(self.experiments)
    
    def get_environment_info(self):
        """Get virtual environment information for this project"""
        from app.services.environment_manager import EnvironmentManager
        if not self.environment_name:
            return None
        try:
            env_mgr = EnvironmentManager()
            return env_mgr.get_environment(self.environment_name)
        except Exception:
            return None
    
    def validate_environment_link(self):
        """Validate that the project's virtual environment exists"""
        if not self.environment_name:
            return False, "Project has no environment_name set"
        try:
            from app.services.environment_manager import EnvironmentManager
            env_mgr = EnvironmentManager()
            env = env_mgr.get_environment(self.environment_name)
            if env:
                return True, "Environment exists and is valid"
            else:
                return False, f"Environment '{self.environment_name}' not found"
        except Exception as e:
            return False, f"Error checking environment: {str(e)}"
    
    def to_dict(self):
        """Convert to dictionary"""
        import json
        env_info = self.get_environment_info()
        
        # Parse industry config if present
        industry_config_data = None
        if self.industry_config:
            try:
                industry_config_data = json.loads(self.industry_config)
            except (json.JSONDecodeError, TypeError):
                industry_config_data = self.industry_config
        
        return {
            'id': self.id,
            'name': self.name,
            'description': self.description,
            'template': self.template,
            'environment_name': self.environment_name,
            'environment_exists': env_info is not None,
            'environment_path': env_info.path if env_info else None,
            'python_executable': env_info.python_executable if env_info else None,
            'status': self.status,
            'framework': self.framework,
            'python_version': self.python_version,
            'industry_profile': self.industry_profile,
            'scenario_id': self.scenario_id,
            'industry_config': industry_config_data,
            'created_at': self.created_at.isoformat() if self.created_at else None,
            'updated_at': self.updated_at.isoformat() if self.updated_at else None,
            'experiments_count': len(self.experiments)
        }
    
    def __repr__(self):
        return f'<MLProject {self.name}>'

