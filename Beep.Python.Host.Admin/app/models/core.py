"""
Core Models
User, Settings, and Audit Log definitions.
"""
from datetime import datetime
from typing import Optional
import json
from app.database import db


# Association table for many-to-many relationship between users and groups
user_groups = db.Table('user_groups',
    db.Column('id', db.Integer, primary_key=True),
    db.Column('user_id', db.Integer, db.ForeignKey('users.id'), nullable=False),
    db.Column('group_id', db.Integer, db.ForeignKey('groups.id'), nullable=False),
    db.Column('added_at', db.DateTime, default=datetime.utcnow),
    db.Column('added_by', db.Integer, db.ForeignKey('users.id'), nullable=True)
)


class Role(db.Model):
    """User Roles for RBAC"""
    __tablename__ = 'roles'
    
    id = db.Column(db.Integer, primary_key=True)
    name = db.Column(db.String(50), unique=True, nullable=False)
    description = db.Column(db.String(255))
    permissions = db.Column(db.Text)  # JSON array of permission strings
    is_system = db.Column(db.Boolean, default=False)  # System roles can't be deleted
    created_at = db.Column(db.DateTime, default=datetime.utcnow)
    
    # Relationships
    users = db.relationship('User', backref='role', lazy=True)
    
    def get_permissions(self):
        """Get list of permissions"""
        if self.permissions:
            try:
                return json.loads(self.permissions)
            except:
                return []
        return []
    
    def set_permissions(self, perms: list):
        """Set permissions from list"""
        self.permissions = json.dumps(perms)
    
    def to_dict(self):
        return {
            'id': self.id,
            'name': self.name,
            'description': self.description,
            'permissions': self.get_permissions(),
            'is_system': self.is_system,
            'user_count': len(self.users)
        }


class User(db.Model):
    """Application User"""
    __tablename__ = 'users'

    id = db.Column(db.Integer, primary_key=True)
    username = db.Column(db.String(80), unique=True, nullable=False)
    password_hash = db.Column(db.String(255), nullable=True)
    email = db.Column(db.String(120), unique=True, nullable=True)
    display_name = db.Column(db.String(120), nullable=True)
    is_admin = db.Column(db.Boolean, default=False)
    is_active = db.Column(db.Boolean, default=True)
    created_at = db.Column(db.DateTime, default=datetime.utcnow)
    last_login = db.Column(db.DateTime, nullable=True)
    
    # RBAC fields
    role_id = db.Column(db.Integer, db.ForeignKey('roles.id'), nullable=True)
    
    # Relationships
    audit_logs = db.relationship('AuditLog', backref='user', lazy=True, foreign_keys='AuditLog.user_id')
    groups = db.relationship('Group', secondary=user_groups, 
                            primaryjoin="User.id == user_groups.c.user_id",
                            secondaryjoin="Group.id == user_groups.c.group_id",
                            backref='members', lazy='dynamic')
    api_tokens = db.relationship('APIToken', backref='user', lazy=True, cascade='all, delete-orphan')

    def to_dict(self):
        return {
            'id': self.id,
            'username': self.username,
            'display_name': self.display_name,
            'email': self.email,
            'is_admin': self.is_admin,
            'is_active': self.is_active,
            'created_at': self.created_at.isoformat() if self.created_at else None,
            'role': self.role.to_dict() if self.role else None,
            'groups': [g.name for g in self.groups]
        }
    
    def has_permission(self, permission: str) -> bool:
        """Check if user has a specific permission"""
        if self.is_admin:
            return True
        if self.role:
            return permission in self.role.get_permissions()
        return False


class Group(db.Model):
    """User Groups"""
    __tablename__ = 'groups'
    
    id = db.Column(db.Integer, primary_key=True)
    name = db.Column(db.String(80), unique=True, nullable=False)
    description = db.Column(db.String(255), nullable=True)
    is_active = db.Column(db.Boolean, default=True)
    created_at = db.Column(db.DateTime, default=datetime.utcnow)
    created_by = db.Column(db.Integer, db.ForeignKey('users.id'), nullable=True)
    
    def to_dict(self):
        return {
            'id': self.id,
            'name': self.name,
            'description': self.description,
            'is_active': self.is_active,
            'created_at': self.created_at.isoformat() if self.created_at else None,
            'member_count': self.members.count()
        }


class APIToken(db.Model):
    """API Token for user authentication"""
    __tablename__ = 'api_tokens'
    
    id = db.Column(db.Integer, primary_key=True)
    user_id = db.Column(db.Integer, db.ForeignKey('users.id'), nullable=False)
    token = db.Column(db.String(255), unique=True, nullable=False, index=True)
    name = db.Column(db.String(200), nullable=True)  # Token name/description
    expires_at = db.Column(db.DateTime, nullable=True)  # Optional expiration
    last_used_at = db.Column(db.DateTime, nullable=True)
    created_at = db.Column(db.DateTime, default=datetime.utcnow)
    is_active = db.Column(db.Boolean, default=True)
    
    # Permissions/scopes (JSON array)
    scopes = db.Column(db.Text, default='[]')  # JSON array of allowed scopes
    
    def get_scopes(self):
        """Get scopes as list"""
        try:
            return json.loads(self.scopes) if self.scopes else []
        except:
            return []
    
    def set_scopes(self, scope_list):
        """Set scopes from list"""
        self.scopes = json.dumps(scope_list) if scope_list else '[]'
    
    def is_expired(self):
        """Check if token is expired"""
        if not self.expires_at:
            return False
        return datetime.utcnow() > self.expires_at
    
    def is_valid(self):
        """Check if token is valid (active and not expired)"""
        return self.is_active and not self.is_expired()
    
    def to_dict(self, include_token=False):
        """Convert to dictionary"""
        return {
            'id': self.id,
            'user_id': self.user_id,
            'token': self.token if include_token else '***' + self.token[-8:] if self.token else None,
            'name': self.name,
            'expires_at': self.expires_at.isoformat() if self.expires_at else None,
            'last_used_at': self.last_used_at.isoformat() if self.last_used_at else None,
            'created_at': self.created_at.isoformat() if self.created_at else None,
            'is_active': self.is_active,
            'is_expired': self.is_expired(),
            'scopes': self.get_scopes()
        }


class Setting(db.Model):
    """Application Settings (Key-Value Store)"""
    __tablename__ = 'settings'

    key = db.Column(db.String(100), primary_key=True)
    value = db.Column(db.Text, nullable=True)
    description = db.Column(db.String(255), nullable=True)
    is_encrypted = db.Column(db.Boolean, default=False)
    updated_at = db.Column(db.DateTime, default=datetime.utcnow, onupdate=datetime.utcnow)

    @classmethod
    def get(cls, key, default=None):
        setting = cls.query.get(key)
        if setting:
            return setting.value
        return default

    @classmethod
    def set(cls, key, value, description=None):
        setting = cls.query.get(key)
        if not setting:
            setting = cls(key=key)
            db.session.add(setting)
        setting.value = str(value)
        if description:
            setting.description = description
        db.session.commit()


class AuditLog(db.Model):
    """System Audit Log"""
    __tablename__ = 'audit_log'

    id = db.Column(db.Integer, primary_key=True)
    action = db.Column(db.String(50), nullable=False)
    resource_type = db.Column(db.String(50), nullable=False)
    resource_id = db.Column(db.String(100), nullable=True)
    user_id = db.Column(db.Integer, db.ForeignKey('users.id'), nullable=True)
    details = db.Column(db.Text, nullable=True)
    ip_address = db.Column(db.String(45), nullable=True)
    created_at = db.Column(db.DateTime, default=datetime.utcnow)

    @classmethod
    def log(cls, action, resource_type, resource_id=None, user_id=None, details=None, ip_address=None):
        entry = cls(
            action=action,
            resource_type=resource_type,
            resource_id=str(resource_id) if resource_id else None,
            user_id=user_id,
            details=details,
            ip_address=ip_address
        )
        db.session.add(entry)
        db.session.commit()
