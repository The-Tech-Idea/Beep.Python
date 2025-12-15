"""
Database Models
"""
from app.models.core import (
    User, Role, Group, Setting, AuditLog, user_groups
)
from app.models.scheduled_jobs import ScheduledJob, JobExecution

# Import RAG metadata models
from app.models.rag_metadata import (
    Collection, Document, AccessPrivilege, DataSource, SyncJob, SyncJobRun
)
_has_rag_models = True

# Import ML models
try:
    from app.models.ml_models import (
        MLModel, MLModelVersion, MLModelAPI, MLModelUsageLog, 
        MLModelValidation, MLModelPermission
    )
    _has_ml_models = True
except (ImportError, AttributeError):
    _has_ml_models = False
    MLModel = None
    MLModelVersion = None
    MLModelAPI = None
    MLModelUsageLog = None
    MLModelValidation = None
    MLModelPermission = None

# Try to import LLM models (may not exist)
try:
    from app.models.llm_models import LocalModel, ModelVersion
    _has_llm_models = True
except (ImportError, AttributeError):
    _has_llm_models = False
    LocalModel = None
    ModelVersion = None

# Try to import AI service models (may not exist)
try:
    from app.models.ai_service_models import AIServiceLocalModel
    _has_ai_service_models = True
except (ImportError, AttributeError):
    _has_ai_service_models = False
    AIServiceLocalModel = None

# Import middleware models if they exist
try:
    from app.models.middleware import RoutingRule, AccessPolicy
    _has_middleware = True
except (ImportError, AttributeError):
    _has_middleware = False
    RoutingRule = None
    AccessPolicy = None

# Build __all__ list
__all__ = [
    'User', 'Role', 'Group', 'Setting', 'AuditLog', 'user_groups',
    'ScheduledJob', 'JobExecution',
]

if _has_rag_models:
    __all__.extend(['Collection', 'Document', 'AccessPrivilege', 'DataSource', 'SyncJob', 'SyncJobRun'])

if _has_ml_models:
    __all__.extend(['MLModel', 'MLModelVersion', 'MLModelAPI', 'MLModelUsageLog', 
                    'MLModelValidation', 'MLModelPermission'])

if _has_llm_models:
    __all__.extend(['LocalModel', 'ModelVersion'])

if _has_ai_service_models:
    __all__.append('AIServiceLocalModel')

if _has_middleware:
    __all__.extend(['RoutingRule', 'AccessPolicy'])
