"""
Beep.Python.Host.Admin Models

Data models and database schemas for the admin application.
"""

from .core import (
    User,
    Setting,
    AuditLog,
    Role,
    Group  # Group is defined in core.py for RBAC
)

from .rag_metadata import (
    RAGMetadataDB,
    get_rag_metadata_db,
    CollectionMeta,
    DocumentMeta,
    AccessPrivilege,
    AccessLevel,
    ResourceType,
    Collection,
    Document,
    DataSourceType,
    DataSource,
    SyncJobStatus,
    SyncJob,
    SyncJobRun
)

from .ml_models import (
    ModelType,
    ModelStatus,
    ValidationStatus,
    MLModel,
    MLModelVersion,
    MLModelAPI,
    MLModelUsageLog,
    MLModelValidation,
    MLModelPermission
)

__all__ = [
    # Core models
    'User',
    'Setting',
    'AuditLog',
    'Role',
    'Group',
    # RAG models
    'RAGMetadataDB',
    'get_rag_metadata_db',
    'CollectionMeta',
    'DocumentMeta',
    'AccessPrivilege',
    'AccessLevel',
    'ResourceType',
    'Collection',
    'Document',
    # Data Source models
    'DataSourceType',
    'DataSource',
    'SyncJobStatus',
    'SyncJob',
    'SyncJobRun',
    # ML Model models
    'ModelType',
    'ModelStatus',
    'ValidationStatus',
    'MLModel',
    'MLModelVersion',
    'MLModelAPI',
    'MLModelUsageLog',
    'MLModelValidation',
    'MLModelPermission'
]
