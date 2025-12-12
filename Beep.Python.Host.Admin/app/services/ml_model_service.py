"""
ML Model Service - Core service for managing ML models
"""
import os
import json
import hashlib
import shutil
import uuid
import threading
from pathlib import Path
from datetime import datetime
from typing import Optional, List, Dict, Any
from dataclasses import dataclass, asdict

from app.database import db
from app.models.ml_models import (
    MLModel, MLModelVersion, MLModelAPI, MLModelUsageLog,
    MLModelPermission, ModelType, ModelStatus, ValidationStatus
)
from app.models.core import User, AuditLog
from app.config_manager import get_app_directory


@dataclass
class ModelUploadResult:
    """Result of model upload"""
    model_id: str
    version_id: str
    status: str
    message: str
    file_path: str


class MLModelService:
    """Service for managing ML models"""
    _instance = None
    _lock = threading.Lock()
    
    def __new__(cls):
        if cls._instance is None:
            with cls._lock:
                if cls._instance is None:
                    cls._instance = super().__new__(cls)
                    cls._instance._initialized = False
        return cls._instance
    
    def __init__(self):
        if self._initialized:
            return
        
        self._initialized = True
        self.base_path = get_app_directory()
        
        # Initialize directory manager for multiple directories
        from app.services.ml_model_directory_manager import get_ml_model_directory_manager
        self.dir_mgr = get_ml_model_directory_manager()
        
        # Get default directory
        default_dir = self.dir_mgr.get_directory('default')
        if default_dir:
            self.models_path = Path(default_dir.path)
        else:
            self.models_path = self.base_path / 'ml_models'
            self.models_path.mkdir(parents=True, exist_ok=True)
    
    def upload_model(self, 
                    file_data: bytes,
                    filename: str,
                    name: str,
                    owner_id: int,
                    model_type: str = 'sklearn',
                    description: Optional[str] = None,
                    framework: Optional[str] = None,
                    category: Optional[str] = None,
                    tags: Optional[List[str]] = None,
                    is_public: bool = False,
                    requirements: Optional[str] = None,
                    python_version: Optional[str] = None,
                    metadata: Optional[Dict[str, Any]] = None) -> ModelUploadResult:
        """Upload and register a new model"""
        try:
            # Generate IDs
            model_id = f"model_{uuid.uuid4().hex[:12]}"
            version_id = f"version_{uuid.uuid4().hex[:12]}"
            version = "1.0.0"
            
            # Get best directory for upload
            file_size_gb = len(file_data) / (1024 ** 3)
            target_dir = self.dir_mgr.get_best_directory_for_upload(file_size_gb)
            if not target_dir:
                raise Exception("No suitable directory found with enough space")
            
            # Create model directory structure in selected directory
            storage_path = Path(target_dir.path)
            user_dir = storage_path / str(owner_id)
            user_dir.mkdir(parents=True, exist_ok=True)
            
            model_dir = user_dir / model_id
            model_dir.mkdir(parents=True, exist_ok=True)
            
            version_dir = model_dir / version
            version_dir.mkdir(parents=True, exist_ok=True)
            
            # Save model file
            file_path = version_dir / filename
            with open(file_path, 'wb') as f:
                f.write(file_data)
            
            # Calculate file hash
            file_hash = hashlib.sha256(file_data).hexdigest()
            file_size = len(file_data)
            
            # Parse requirements
            requirements_list = []
            if requirements:
                requirements_list = [r.strip() for r in requirements.split('\n') if r.strip()]
            
            # Create model record
            model = MLModel(
                id=model_id,
                name=name,
                description=description,
                model_type=model_type,
                framework=framework,
                version=version,
                current_version_id=version_id,
                owner_id=owner_id,
                is_public=is_public,
                category=category,
                tags_list=tags or [],
                status=ModelStatus.PENDING.value,
                validation_status=ValidationStatus.PENDING.value,
                metadata=metadata or {}
            )
            
            # Create version record
            version_record = MLModelVersion(
                id=version_id,
                model_id=model_id,
                version=version,
                file_path=str(file_path),
                file_size=file_size,
                file_hash=file_hash,
                requirements=requirements_list,
                python_version=python_version,
                is_active=True,
                created_by=owner_id
            )
            
            db.session.add(model)
            db.session.add(version_record)
            db.session.commit()
            
            # Log audit
            AuditLog.log(
                action='ml_model_uploaded',
                resource_type='ml_model',
                resource_id=model_id,
                user_id=owner_id,
                details={'model_name': name, 'version': version}
            )
            
            return ModelUploadResult(
                model_id=model_id,
                version_id=version_id,
                status='pending_validation',
                message='Model uploaded successfully. Validation in progress.',
                file_path=str(file_path)
            )
            
        except Exception as e:
            db.session.rollback()
            raise Exception(f"Failed to upload model: {str(e)}")
    
    def get_model(self, model_id: str) -> Optional[MLModel]:
        """Get model by ID"""
        return MLModel.query.get(model_id)
    
    def list_models(self,
                   owner_id: Optional[int] = None,
                   category: Optional[str] = None,
                   is_public: Optional[bool] = None,
                   status: Optional[str] = None,
                   limit: int = 100,
                   offset: int = 0) -> tuple[List[MLModel], int]:
        """List models with filtering"""
        query = MLModel.query
        
        if owner_id:
            query = query.filter(MLModel.owner_id == owner_id)
        
        if category:
            query = query.filter(MLModel.category == category)
        
        if is_public is not None:
            query = query.filter(MLModel.is_public == is_public)
        
        if status:
            query = query.filter(MLModel.status == status)
        
        total = query.count()
        models = query.order_by(MLModel.created_at.desc()).limit(limit).offset(offset).all()
        
        return models, total
    
    def delete_model(self, model_id: str, user_id: int) -> bool:
        """Delete a model"""
        model = self.get_model(model_id)
        if not model:
            return False
        
        # Check permissions
        if model.owner_id != user_id:
            # Check if user is admin
            user = User.query.get(user_id)
            if not user or not user.is_admin:
                raise PermissionError("You don't have permission to delete this model")
        
        try:
            # Delete files from all directories
            all_paths = self.dir_mgr.get_all_model_paths()
            for storage_path in all_paths:
                user_dir = storage_path / str(model.owner_id)
                model_dir = user_dir / model_id
                if model_dir.exists():
                    shutil.rmtree(model_dir)
                    break  # Model should only exist in one directory
            
            # Delete from database (cascade will handle related records)
            db.session.delete(model)
            db.session.commit()
            
            # Log audit
            AuditLog.log(
                action='ml_model_deleted',
                resource_type='ml_model',
                resource_id=model_id,
                user_id=user_id,
                details={'model_name': model.name}
            )
            
            return True
            
        except Exception as e:
            db.session.rollback()
            raise Exception(f"Failed to delete model: {str(e)}")
    
    def update_model(self, model_id: str, user_id: int, **kwargs) -> bool:
        """Update model metadata"""
        model = self.get_model(model_id)
        if not model:
            return False
        
        # Check permissions
        if model.owner_id != user_id:
            user = User.query.get(user_id)
            if not user or not user.is_admin:
                raise PermissionError("You don't have permission to update this model")
        
        try:
            # Update allowed fields
            allowed_fields = ['name', 'description', 'category', 'is_public', 'tags_list', 'metadata']
            for field, value in kwargs.items():
                if field in allowed_fields and hasattr(model, field):
                    setattr(model, field, value)
            
            model.updated_at = datetime.utcnow()
            db.session.commit()
            
            # Log audit
            AuditLog.log(
                action='ml_model_updated',
                resource_type='ml_model',
                resource_id=model_id,
                user_id=user_id,
                details={'updated_fields': list(kwargs.keys())}
            )
            
            return True
            
        except Exception as e:
            db.session.rollback()
            raise Exception(f"Failed to update model: {str(e)}")
    
    def get_model_versions(self, model_id: str) -> List[MLModelVersion]:
        """Get all versions of a model"""
        return MLModelVersion.query.filter_by(model_id=model_id).order_by(
            MLModelVersion.created_at.desc()
        ).all()
    
    def activate_version(self, model_id: str, version_id: str, user_id: int) -> bool:
        """Activate a specific model version"""
        model = self.get_model(model_id)
        if not model:
            return False
        
        # Check permissions
        if model.owner_id != user_id:
            user = User.query.get(user_id)
            if not user or not user.is_admin:
                raise PermissionError("You don't have permission to activate this version")
        
        try:
            # Deactivate all versions
            MLModelVersion.query.filter_by(model_id=model_id).update({'is_active': False})
            
            # Activate selected version
            version = MLModelVersion.query.get(version_id)
            if version and version.model_id == model_id:
                version.is_active = True
                model.current_version_id = version_id
                model.version = version.version
                db.session.commit()
                
                # Log audit
                AuditLog.log(
                    action='ml_model_version_activated',
                    resource_type='ml_model',
                    resource_id=model_id,
                    user_id=user_id,
                    details={'version_id': version_id, 'version': version.version}
                )
                
                return True
            
            return False
            
        except Exception as e:
            db.session.rollback()
            raise Exception(f"Failed to activate version: {str(e)}")
    
    def check_permission(self, model_id: str, user_id: int, permission_type: str) -> bool:
        """Check if user has permission for a model"""
        model = self.get_model(model_id)
        if not model:
            return False
        
        # Owner has all permissions
        if model.owner_id == user_id:
            return True
        
        # Check if user is admin
        user = User.query.get(user_id)
        if user and user.is_admin:
            return True
        
        # Public models can be read/executed by anyone
        if model.is_public and permission_type in ['read', 'execute']:
            return True
        
        # Check explicit permissions
        permission = MLModelPermission.query.filter_by(
            model_id=model_id,
            permission_type=permission_type
        ).filter(
            (MLModelPermission.user_id == user_id) |
            (MLModelPermission.group_id.in_([g.id for g in user.groups.all()] if user else []))
        ).first()
        
        if permission:
            # Check expiration
            if permission.expires_at and permission.expires_at < datetime.utcnow():
                return False
            return True
        
        return False
    
    def log_usage(self, model_id: str, api_id: Optional[str], user_id: Optional[int],
                 endpoint: str, request_data: Any, response_status: int,
                 response_time_ms: int, error_message: Optional[str] = None,
                 ip_address: Optional[str] = None):
        """Log model API usage"""
        try:
            log = MLModelUsageLog(
                model_id=model_id,
                api_id=api_id,
                user_id=user_id,
                endpoint=endpoint,
                request_data=json.dumps(request_data) if request_data else None,
                response_status=response_status,
                response_time_ms=response_time_ms,
                error_message=error_message,
                ip_address=ip_address
            )
            db.session.add(log)
            db.session.commit()
        except Exception as e:
            # Don't fail on logging errors
            print(f"Failed to log usage: {e}")

