"""
Model API Generator - Generates REST API endpoints for ML models
"""
import os
import json
import uuid
from pathlib import Path
from typing import Optional, Dict, Any
from datetime import datetime

from app.database import db
from app.models.ml_models import MLModel, MLModelVersion, MLModelAPI
from app.models.core import AuditLog


class ModelAPIGenerator:
    """Service for generating API endpoints for models"""
    _instance = None
    
    def __new__(cls):
        if cls._instance is None:
            cls._instance = super().__new__(cls)
            cls._instance._initialized = False
        return cls._instance
    
    def __init__(self):
        if self._initialized:
            return
        
        self._initialized = True
    
    def generate_api(self, model_id: str, version_id: str,
                    endpoint_path: Optional[str] = None,
                    rate_limit_per_minute: Optional[int] = None) -> Dict[str, Any]:
        """Generate API endpoint for a model"""
        model = MLModel.query.get(model_id)
        version = MLModelVersion.query.get(version_id)
        
        if not model or not version or version.model_id != model_id:
            raise ValueError("Model or version not found")
        
        # Generate API ID
        api_id = f"api_{uuid.uuid4().hex[:12]}"
        
        # Default endpoint path
        if not endpoint_path:
            endpoint_path = f"/api/v1/ml-models/{model_id}/predict"
        
        # Generate OpenAPI schema
        api_schema = self._generate_openapi_schema(model, version, endpoint_path)
        
        # Create API record
        api = MLModelAPI(
            id=api_id,
            model_id=model_id,
            version_id=version_id,
            endpoint_path=endpoint_path,
            is_active=True,
            api_schema=api_schema,
            rate_limit_per_minute=rate_limit_per_minute
        )
        
        db.session.add(api)
        db.session.commit()
        
        # Log audit
        AuditLog.log(
            action='ml_model_api_generated',
            resource_type='ml_model',
            resource_id=model_id,
            user_id=model.owner_id,
            details={'api_id': api_id, 'endpoint': endpoint_path}
        )
        
        return {
            'api_id': api_id,
            'endpoint': endpoint_path,
            'status': 'active',
            'documentation_url': f'/api/v1/ml-models/{model_id}/docs'
        }
    
    def _generate_openapi_schema(self, model: MLModel, version: MLModelVersion,
                                endpoint_path: str) -> Dict[str, Any]:
        """Generate OpenAPI schema for the model API"""
        schema = {
            'openapi': '3.0.0',
            'info': {
                'title': f'{model.name} API',
                'description': model.description or f'API for {model.name}',
                'version': version.version
            },
            'paths': {
                endpoint_path: {
                    'post': {
                        'summary': f'Predict using {model.name}',
                        'description': f'Make predictions using {model.name} version {version.version}',
                        'requestBody': {
                            'required': True,
                            'content': {
                                'application/json': {
                                    'schema': {
                                        'type': 'object',
                                        'properties': {
                                            'data': {
                                                'type': 'object',
                                                'description': 'Input features for prediction'
                                            }
                                        },
                                        'required': ['data']
                                    }
                                }
                            }
                        },
                        'responses': {
                            '200': {
                                'description': 'Successful prediction',
                                'content': {
                                    'application/json': {
                                        'schema': {
                                            'type': 'object',
                                            'properties': {
                                                'success': {'type': 'boolean'},
                                                'prediction': {
                                                    'type': 'array',
                                                    'items': {'type': 'number'}
                                                },
                                                'model_id': {'type': 'string'},
                                                'version': {'type': 'string'}
                                            }
                                        }
                                    }
                                }
                            },
                            '400': {
                                'description': 'Bad request'
                            },
                            '500': {
                                'description': 'Server error'
                            }
                        }
                    }
                },
                f'{endpoint_path}/batch': {
                    'post': {
                        'summary': f'Batch predict using {model.name}',
                        'description': f'Make batch predictions using {model.name}',
                        'requestBody': {
                            'required': True,
                            'content': {
                                'application/json': {
                                    'schema': {
                                        'type': 'object',
                                        'properties': {
                                            'data': {
                                                'type': 'array',
                                                'items': {'type': 'object'}
                                            }
                                        },
                                        'required': ['data']
                                    }
                                }
                            }
                        },
                        'responses': {
                            '200': {
                                'description': 'Successful batch prediction'
                            }
                        }
                    }
                }
            }
        }
        
        return schema
    
    def get_api_docs(self, model_id: str) -> Optional[Dict[str, Any]]:
        """Get API documentation for a model"""
        api = MLModelAPI.query.filter_by(model_id=model_id, is_active=True).first()
        if not api:
            return None
        
        return api.api_schema
    
    def update_api(self, api_id: str, **kwargs) -> bool:
        """Update API configuration"""
        api = MLModelAPI.query.get(api_id)
        if not api:
            return False
        
        try:
            allowed_fields = ['endpoint_path', 'rate_limit_per_minute', 'is_active']
            for field, value in kwargs.items():
                if field in allowed_fields and hasattr(api, field):
                    setattr(api, field, value)
            
            api.updated_at = datetime.utcnow()
            db.session.commit()
            return True
            
        except Exception as e:
            db.session.rollback()
            raise Exception(f"Failed to update API: {str(e)}")

