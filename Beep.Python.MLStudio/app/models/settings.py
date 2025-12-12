"""
Settings Model
Stores application-wide configuration settings
"""
from datetime import datetime
from app import db
import json


class Settings(db.Model):
    """Application settings model"""
    __tablename__ = 'settings'
    
    id = db.Column(db.Integer, primary_key=True)
    key = db.Column(db.String(200), nullable=False, unique=True)
    category = db.Column(db.String(100), nullable=False)  # general, paths, environment, ml, ui
    value = db.Column(db.Text)  # JSON string for complex values
    value_type = db.Column(db.String(50), default='string')  # string, number, boolean, json, path
    description = db.Column(db.Text)
    is_encrypted = db.Column(db.Boolean, default=False)
    
    # Timestamps
    created_at = db.Column(db.DateTime, default=datetime.utcnow)
    updated_at = db.Column(db.DateTime, default=datetime.utcnow, onupdate=datetime.utcnow)
    
    def get_value(self):
        """Get setting value with proper type conversion"""
        if self.value is None:
            return None
        
        if self.value_type == 'boolean':
            return self.value.lower() in ('true', '1', 'yes', 'on')
        elif self.value_type == 'number':
            try:
                if '.' in self.value:
                    return float(self.value)
                return int(self.value)
            except ValueError:
                return self.value
        elif self.value_type == 'json':
            try:
                return json.loads(self.value)
            except:
                return self.value
        else:
            return self.value
    
    def set_value(self, value):
        """Set setting value with proper serialization"""
        if self.value_type == 'boolean':
            self.value = str(bool(value)).lower()
        elif self.value_type == 'number':
            self.value = str(value)
        elif self.value_type == 'json':
            self.value = json.dumps(value) if not isinstance(value, str) else value
        else:
            self.value = str(value)
    
    def to_dict(self):
        """Convert to dictionary"""
        return {
            'id': self.id,
            'key': self.key,
            'category': self.category,
            'value': self.get_value(),
            'value_type': self.value_type,
            'description': self.description,
            'is_encrypted': self.is_encrypted,
            'created_at': self.created_at.isoformat() if self.created_at else None,
            'updated_at': self.updated_at.isoformat() if self.updated_at else None
        }
    
    @staticmethod
    def get_default_settings():
        """Get default settings configuration"""
        return [
            # General Settings
            {'key': 'app_name', 'category': 'general', 'value': 'Beep ML Studio', 'value_type': 'string', 'description': 'Application name'},
            {'key': 'company_name', 'category': 'general', 'value': 'TheTechIdea', 'value_type': 'string', 'description': 'Company name'},
            {'key': 'app_version', 'category': 'general', 'value': '1.0.0', 'value_type': 'string', 'description': 'Application version'},
            {'key': 'debug_mode', 'category': 'general', 'value': 'false', 'value_type': 'boolean', 'description': 'Enable debug mode'},
            {'key': 'log_level', 'category': 'general', 'value': 'INFO', 'value_type': 'string', 'description': 'Logging level (DEBUG, INFO, WARNING, ERROR)'},
            
            # Path Settings
            {'key': 'base_path', 'category': 'paths', 'value': '.', 'value_type': 'path', 'description': 'Base application path'},
            {'key': 'projects_folder', 'category': 'paths', 'value': 'projects', 'value_type': 'path', 'description': 'Projects directory'},
            {'key': 'data_folder', 'category': 'paths', 'value': 'data', 'value_type': 'path', 'description': 'Data uploads directory'},
            {'key': 'models_folder', 'category': 'paths', 'value': 'models', 'value_type': 'path', 'description': 'Saved models directory'},
            {'key': 'providers_folder', 'category': 'paths', 'value': 'providers', 'value_type': 'path', 'description': 'Virtual environments directory'},
            {'key': 'python_embedded_path', 'category': 'paths', 'value': 'python-embedded', 'value_type': 'path', 'description': 'Embedded Python installation path'},
            
            # Environment Settings
            {'key': 'environment_manager_type', 'category': 'environment', 'value': 'local', 'value_type': 'string', 'description': 'Environment manager type (local, host_admin)'},
            {'key': 'host_admin_url', 'category': 'environment', 'value': 'http://127.0.0.1:5000', 'value_type': 'string', 'description': 'Host Admin API URL'},
            {'key': 'host_admin_api_key', 'category': 'environment', 'value': '', 'value_type': 'string', 'description': 'Host Admin API key (optional)', 'is_encrypted': True},
            {'key': 'default_python_version', 'category': 'environment', 'value': '3.11', 'value_type': 'string', 'description': 'Default Python version for new environments'},
            {'key': 'auto_create_environments', 'category': 'environment', 'value': 'true', 'value_type': 'boolean', 'description': 'Automatically create environments for new projects'},
            
            # ML Settings
            {'key': 'default_framework', 'category': 'ml', 'value': 'scikit-learn', 'value_type': 'string', 'description': 'Default ML framework'},
            {'key': 'max_upload_size_mb', 'category': 'ml', 'value': '100', 'value_type': 'number', 'description': 'Maximum file upload size in MB'},
            {'key': 'default_train_test_split', 'category': 'ml', 'value': '0.2', 'value_type': 'number', 'description': 'Default train/test split ratio'},
            {'key': 'default_random_state', 'category': 'ml', 'value': '42', 'value_type': 'number', 'description': 'Default random state for reproducibility'},
            {'key': 'auto_save_models', 'category': 'ml', 'value': 'true', 'value_type': 'boolean', 'description': 'Automatically save trained models'},
            
            # UI Settings
            {'key': 'theme', 'category': 'ui', 'value': 'light', 'value_type': 'string', 'description': 'UI theme (light, dark)'},
            {'key': 'items_per_page', 'category': 'ui', 'value': '20', 'value_type': 'number', 'description': 'Number of items per page in lists'},
            {'key': 'auto_refresh_interval', 'category': 'ui', 'value': '30', 'value_type': 'number', 'description': 'Auto-refresh interval in seconds'},
            {'key': 'show_advanced_options', 'category': 'ui', 'value': 'false', 'value_type': 'boolean', 'description': 'Show advanced options in UI'},
        ]
    
    def __repr__(self):
        return f'<Settings {self.key}>'

