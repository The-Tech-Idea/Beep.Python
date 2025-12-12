"""
ML Models Database Models
Stores metadata for hosted ML models, versions, APIs, usage logs, and permissions.
"""
from datetime import datetime
from typing import Optional, Dict, Any
from enum import Enum
import json
from app.database import db
from app.models.core import User, Group, AuditLog


class ModelType(str, Enum):
    """Supported model types"""
    SKLEARN = 'sklearn'
    TENSORFLOW = 'tensorflow'
    PYTORCH = 'pytorch'
    XGBOOST = 'xgboost'
    ONNX = 'onnx'
    CUSTOM = 'custom'


class ModelStatus(str, Enum):
    """Model status"""
    PENDING = 'pending'
    VALIDATED = 'validated'
    ACTIVE = 'active'
    DEPRECATED = 'deprecated'
    DELETED = 'deleted'


class ValidationStatus(str, Enum):
    """Validation status"""
    PENDING = 'pending'
    PASSED = 'passed'
    FAILED = 'failed'
    WARNING = 'warning'


class MLModel(db.Model):
    """ML Model Metadata"""
    __tablename__ = 'ml_models'

    id = db.Column(db.String(50), primary_key=True)
    name = db.Column(db.String(255), nullable=False)
    description = db.Column(db.Text, nullable=True)
    model_type = db.Column(db.String(50), nullable=False)  # sklearn, tensorflow, pytorch, etc.
    framework = db.Column(db.String(50), nullable=True)
    version = db.Column(db.String(20), default='1.0.0')
    current_version_id = db.Column(db.String(50), nullable=True)
    owner_id = db.Column(db.Integer, db.ForeignKey('users.id'), nullable=False)
    is_public = db.Column(db.Boolean, default=False)
    category = db.Column(db.String(100), nullable=True)  # classification, regression, nlp, cv, etc.
    tags = db.Column(db.Text, nullable=True)  # JSON array
    status = db.Column(db.String(20), default='pending')
    validation_status = db.Column(db.String(20), nullable=True)
    created_at = db.Column(db.DateTime, default=datetime.utcnow)
    updated_at = db.Column(db.DateTime, default=datetime.utcnow, onupdate=datetime.utcnow)
    metadata_json = db.Column(db.Text, default='{}')

    # Relationships
    owner = db.relationship('User', backref='ml_models')
    versions = db.relationship('MLModelVersion', backref='model', lazy='dynamic', cascade='all, delete-orphan')
    apis = db.relationship('MLModelAPI', backref='model', lazy='dynamic', cascade='all, delete-orphan')
    permissions = db.relationship('MLModelPermission', backref='model', lazy='dynamic', cascade='all, delete-orphan')

    @property
    def tags_list(self):
        """Get tags as list"""
        if self.tags:
            try:
                return json.loads(self.tags)
            except:
                return []
        return []

    @tags_list.setter
    def tags_list(self, value):
        """Set tags from list"""
        self.tags = json.dumps(value) if value else None

    @property
    def model_metadata(self):
        """Get metadata as dict"""
        if self.metadata_json:
            try:
                return json.loads(self.metadata_json)
            except:
                return {}
        return {}

    @model_metadata.setter
    def model_metadata(self, value):
        """Set metadata from dict"""
        self.metadata_json = json.dumps(value) if value else '{}'

    def to_dict(self):
        """Convert to dictionary"""
        return {
            'id': self.id,
            'name': self.name,
            'description': self.description,
            'model_type': self.model_type,
            'framework': self.framework,
            'version': self.version,
            'current_version_id': self.current_version_id,
            'owner_id': self.owner_id,
            'owner': self.owner.to_dict() if self.owner else None,
            'is_public': self.is_public,
            'category': self.category,
            'tags': self.tags_list,
            'status': self.status,
            'validation_status': self.validation_status,
            'created_at': self.created_at.isoformat() if self.created_at else None,
            'updated_at': self.updated_at.isoformat() if self.updated_at else None,
            'metadata': self.model_metadata
        }


class MLModelVersion(db.Model):
    """ML Model Version"""
    __tablename__ = 'ml_model_versions'

    id = db.Column(db.String(50), primary_key=True)
    model_id = db.Column(db.String(50), db.ForeignKey('ml_models.id'), nullable=False)
    version = db.Column(db.String(20), nullable=False)
    file_path = db.Column(db.Text, nullable=False)
    file_size = db.Column(db.BigInteger, nullable=True)
    file_hash = db.Column(db.String(64), nullable=True)  # SHA-256
    requirements_json = db.Column(db.Text, nullable=True)  # JSON array
    python_version = db.Column(db.String(20), nullable=True)
    validation_report_json = db.Column(db.Text, nullable=True)
    validation_status = db.Column(db.String(20), nullable=True)
    validation_date = db.Column(db.DateTime, nullable=True)
    is_active = db.Column(db.Boolean, default=False)
    created_at = db.Column(db.DateTime, default=datetime.utcnow)
    created_by = db.Column(db.Integer, db.ForeignKey('users.id'), nullable=True)
    notes = db.Column(db.Text, nullable=True)

    # Relationships
    creator = db.relationship('User', backref='created_model_versions')

    __table_args__ = (
        db.UniqueConstraint('model_id', 'version', name='uq_model_version'),
    )

    @property
    def requirements(self):
        """Get requirements as list"""
        if self.requirements_json:
            try:
                return json.loads(self.requirements_json)
            except:
                return []
        return []

    @requirements.setter
    def requirements(self, value):
        """Set requirements from list"""
        self.requirements_json = json.dumps(value) if value else None

    @property
    def validation_report(self):
        """Get validation report as dict"""
        if self.validation_report_json:
            try:
                return json.loads(self.validation_report_json)
            except:
                return {}
        return {}

    @validation_report.setter
    def validation_report(self, value):
        """Set validation report from dict"""
        self.validation_report_json = json.dumps(value) if value else None

    def to_dict(self):
        """Convert to dictionary"""
        return {
            'id': self.id,
            'model_id': self.model_id,
            'version': self.version,
            'file_path': self.file_path,
            'file_size': self.file_size,
            'file_hash': self.file_hash,
            'requirements': self.requirements,
            'python_version': self.python_version,
            'validation_report': self.validation_report,
            'validation_status': self.validation_status,
            'validation_date': self.validation_date.isoformat() if self.validation_date else None,
            'is_active': self.is_active,
            'created_at': self.created_at.isoformat() if self.created_at else None,
            'created_by': self.created_by,
            'notes': self.notes
        }


class MLModelAPI(db.Model):
    """ML Model API Endpoint"""
    __tablename__ = 'ml_model_apis'

    id = db.Column(db.String(50), primary_key=True)
    model_id = db.Column(db.String(50), db.ForeignKey('ml_models.id'), nullable=False)
    version_id = db.Column(db.String(50), db.ForeignKey('ml_model_versions.id'), nullable=False)
    endpoint_path = db.Column(db.String(255), nullable=False)
    server_id = db.Column(db.String(50), nullable=True)  # Reference to ServerManager server
    port = db.Column(db.Integer, nullable=True)
    is_active = db.Column(db.Boolean, default=False)
    api_schema_json = db.Column(db.Text, nullable=True)  # OpenAPI schema
    rate_limit_per_minute = db.Column(db.Integer, nullable=True)
    created_at = db.Column(db.DateTime, default=datetime.utcnow)
    updated_at = db.Column(db.DateTime, default=datetime.utcnow, onupdate=datetime.utcnow)

    # Relationships
    model_version = db.relationship('MLModelVersion', backref='apis')

    @property
    def api_schema(self):
        """Get API schema as dict"""
        if self.api_schema_json:
            try:
                return json.loads(self.api_schema_json)
            except:
                return {}
        return {}

    @api_schema.setter
    def api_schema(self, value):
        """Set API schema from dict"""
        self.api_schema_json = json.dumps(value) if value else None

    def to_dict(self):
        """Convert to dictionary"""
        return {
            'id': self.id,
            'model_id': self.model_id,
            'version_id': self.version_id,
            'endpoint_path': self.endpoint_path,
            'server_id': self.server_id,
            'port': self.port,
            'is_active': self.is_active,
            'api_schema': self.api_schema,
            'rate_limit_per_minute': self.rate_limit_per_minute,
            'created_at': self.created_at.isoformat() if self.created_at else None,
            'updated_at': self.updated_at.isoformat() if self.updated_at else None
        }


class MLModelUsageLog(db.Model):
    """ML Model Usage Log"""
    __tablename__ = 'ml_model_usage_logs'

    id = db.Column(db.Integer, primary_key=True)
    model_id = db.Column(db.String(50), db.ForeignKey('ml_models.id'), nullable=False)
    api_id = db.Column(db.String(50), db.ForeignKey('ml_model_apis.id'), nullable=True)
    user_id = db.Column(db.Integer, db.ForeignKey('users.id'), nullable=True)
    endpoint = db.Column(db.String(255), nullable=True)
    request_data = db.Column(db.Text, nullable=True)  # JSON
    response_status = db.Column(db.Integer, nullable=True)
    response_time_ms = db.Column(db.Integer, nullable=True)
    error_message = db.Column(db.Text, nullable=True)
    ip_address = db.Column(db.String(45), nullable=True)
    created_at = db.Column(db.DateTime, default=datetime.utcnow)

    # Relationships
    user = db.relationship('User', backref='ml_model_usage_logs')
    api = db.relationship('MLModelAPI', backref='usage_logs')

    def to_dict(self):
        """Convert to dictionary"""
        return {
            'id': self.id,
            'model_id': self.model_id,
            'api_id': self.api_id,
            'user_id': self.user_id,
            'endpoint': self.endpoint,
            'request_data': json.loads(self.request_data) if self.request_data else None,
            'response_status': self.response_status,
            'response_time_ms': self.response_time_ms,
            'error_message': self.error_message,
            'ip_address': self.ip_address,
            'created_at': self.created_at.isoformat() if self.created_at else None
        }


class MLModelValidation(db.Model):
    """ML Model Validation Record"""
    __tablename__ = 'ml_model_validations'

    id = db.Column(db.Integer, primary_key=True)
    model_id = db.Column(db.String(50), db.ForeignKey('ml_models.id'), nullable=False)
    version_id = db.Column(db.String(50), db.ForeignKey('ml_model_versions.id'), nullable=False)
    validation_type = db.Column(db.String(50), nullable=True)  # format, functionality, performance, security
    status = db.Column(db.String(20), nullable=False)  # passed, failed, warning
    details_json = db.Column(db.Text, nullable=True)  # JSON validation details
    executed_by = db.Column(db.Integer, db.ForeignKey('users.id'), nullable=True)
    executed_at = db.Column(db.DateTime, default=datetime.utcnow)
    execution_time_ms = db.Column(db.Integer, nullable=True)

    # Relationships
    executor = db.relationship('User', backref='ml_model_validations')
    model_version = db.relationship('MLModelVersion', backref='validations')

    @property
    def details(self):
        """Get details as dict"""
        if self.details_json:
            try:
                return json.loads(self.details_json)
            except:
                return {}
        return {}

    @details.setter
    def details(self, value):
        """Set details from dict"""
        self.details_json = json.dumps(value) if value else None

    def to_dict(self):
        """Convert to dictionary"""
        return {
            'id': self.id,
            'model_id': self.model_id,
            'version_id': self.version_id,
            'validation_type': self.validation_type,
            'status': self.status,
            'details': self.details,
            'executed_by': self.executed_by,
            'executed_at': self.executed_at.isoformat() if self.executed_at else None,
            'execution_time_ms': self.execution_time_ms
        }


class MLModelPermission(db.Model):
    """ML Model Permission"""
    __tablename__ = 'ml_model_permissions'

    id = db.Column(db.Integer, primary_key=True)
    model_id = db.Column(db.String(50), db.ForeignKey('ml_models.id'), nullable=False)
    user_id = db.Column(db.Integer, db.ForeignKey('users.id'), nullable=True)
    group_id = db.Column(db.Integer, db.ForeignKey('groups.id'), nullable=True)
    permission_type = db.Column(db.String(20), nullable=False)  # read, execute, admin
    granted_by = db.Column(db.Integer, db.ForeignKey('users.id'), nullable=True)
    granted_at = db.Column(db.DateTime, default=datetime.utcnow)
    expires_at = db.Column(db.DateTime, nullable=True)

    # Relationships
    user = db.relationship('User', foreign_keys=[user_id], backref='ml_model_permissions')
    group = db.relationship('Group', backref='ml_model_permissions')
    granter = db.relationship('User', foreign_keys=[granted_by])

    def to_dict(self):
        """Convert to dictionary"""
        return {
            'id': self.id,
            'model_id': self.model_id,
            'user_id': self.user_id,
            'group_id': self.group_id,
            'permission_type': self.permission_type,
            'granted_by': self.granted_by,
            'granted_at': self.granted_at.isoformat() if self.granted_at else None,
            'expires_at': self.expires_at.isoformat() if self.expires_at else None
        }

