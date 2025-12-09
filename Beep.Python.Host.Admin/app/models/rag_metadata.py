"""
RAG Metadata Models (SQLAlchemy)
Stores metadata for Collections, Documents, and Access Control.
Uses Group model from core.py for user groups.
"""
from datetime import datetime
from typing import Optional, List, Dict
from enum import Enum
import json
from app.database import db
from app.models.core import User, AuditLog, Group  # Import Group from core

class AccessLevel(str, Enum):
    READ = 'read'
    WRITE = 'write'
    ADMIN = 'admin'

class ResourceType(str, Enum):
    COLLECTION = 'collection'
    DOCUMENT = 'document'

# Group model is now imported from core.py - no duplicate definition needed

class Collection(db.Model):
    """RAG Collection Metadata"""
    __tablename__ = 'collections'

    id = db.Column(db.Integer, primary_key=True)
    collection_id = db.Column(db.String(100), unique=True, nullable=False) # ID in vector DB
    name = db.Column(db.String(100), nullable=False)
    description = db.Column(db.Text, nullable=True)
    provider = db.Column(db.String(50), nullable=False) # e.g., 'chromadb', 'faiss'
    owner_id = db.Column(db.Integer, db.ForeignKey('users.id'), nullable=True)
    document_count = db.Column(db.Integer, default=0)
    is_public = db.Column(db.Boolean, default=False)
    created_at = db.Column(db.DateTime, default=datetime.utcnow)
    updated_at = db.Column(db.DateTime, default=datetime.utcnow, onupdate=datetime.utcnow)
    metadata_json = db.Column(db.Text, default='{}')

    # Relationships
    owner = db.relationship('User', backref='collections')
    documents = db.relationship('Document', backref='collection', lazy='dynamic', cascade='all, delete-orphan')

    @property
    def meta(self):
        return json.loads(self.metadata_json)

    @meta.setter
    def meta(self, value):
        self.metadata_json = json.dumps(value)

    def to_dict(self):
        return {
            'id': self.id,
            'collection_id': self.collection_id,
            'name': self.name,
            'description': self.description,
            'provider': self.provider,
            'owner_id': self.owner_id,
            'document_count': self.document_count,
            'is_public': self.is_public,
            'created_at': self.created_at.isoformat(),
            'metadata': self.meta
        }

class Document(db.Model):
    """RAG Document Metadata"""
    __tablename__ = 'documents'

    id = db.Column(db.Integer, primary_key=True)
    document_id = db.Column(db.String(100), nullable=False) # ID in vector DB
    collection_id = db.Column(db.Integer, db.ForeignKey('collections.id'), nullable=False)
    source = db.Column(db.String(255), nullable=True) # File path or URL
    title = db.Column(db.String(255), nullable=True)
    content_hash = db.Column(db.String(64), nullable=True)
    chunk_count = db.Column(db.Integer, default=0)
    uploaded_by = db.Column(db.Integer, db.ForeignKey('users.id'), nullable=True)
    created_at = db.Column(db.DateTime, default=datetime.utcnow)
    metadata_json = db.Column(db.Text, default='{}')

    # Relationships
    uploader = db.relationship('User', backref='uploaded_documents')

    __table_args__ = (
        db.UniqueConstraint('document_id', 'collection_id', name='uq_doc_col'),
    )

    @property
    def meta(self):
        return json.loads(self.metadata_json)

    @meta.setter
    def meta(self, value):
        self.metadata_json = json.dumps(value)

    def to_dict(self):
        return {
            'id': self.id,
            'document_id': self.document_id,
            'collection_id': self.collection_id,
            'source': self.source,
            'title': self.title,
            'content_hash': self.content_hash,
            'chunk_count': self.chunk_count,
            'uploaded_by': self.uploaded_by,
            'created_at': self.created_at.isoformat() if self.created_at else None,
            'metadata': self.meta
        }

class AccessPrivilege(db.Model):
    """Access Control for RAG Resources"""
    __tablename__ = 'access_privileges'

    id = db.Column(db.Integer, primary_key=True)
    resource_type = db.Column(db.String(50), nullable=False) # 'collection', 'document'
    resource_id = db.Column(db.Integer, nullable=False)
    user_id = db.Column(db.Integer, db.ForeignKey('users.id'), nullable=True)
    group_id = db.Column(db.Integer, nullable=True) # Future proofing for groups
    access_level = db.Column(db.String(20), default='read') # 'read', 'write', 'admin'
    granted_by = db.Column(db.Integer, db.ForeignKey('users.id'), nullable=True)
    created_at = db.Column(db.DateTime, default=datetime.utcnow)
    expires_at = db.Column(db.DateTime, nullable=True)

    # Indexes
    __table_args__ = (
        db.Index('idx_access_resource', 'resource_type', 'resource_id'),
    )

# ==========================================
# Compatibility Layer (Adapter)
# ==========================================

def asdict(obj):
    """Legacy helper"""
    if hasattr(obj, 'to_dict'):
        return obj.to_dict()
    return {}

class RAGMetadataDB:
    """
    Adapter to make SQLAlchemy models look like the old SQLite/JSON wrapper.
    Used by app/routes/rag.py and services.
    """
    _instance = None
    
    def __new__(cls):
        if cls._instance is None:
            cls._instance = super().__new__(cls)
        return cls._instance

    def __init__(self):
        pass

    def get_stats(self):
        """Get global stats"""
        try:
            return {
                'collections': Collection.query.count(),
                'documents': Document.query.count(),
                'users': User.query.count()
            }
        except:
            return {'collections': 0, 'documents': 0, 'users': 0}

    def list_collections(self, provider: Optional[str] = None, user_id: Optional[int] = None) -> List[Dict]:
        """List collections"""
        query = Collection.query
        if provider:
            query = query.filter_by(provider=provider)
        if user_id:
            # Simple header check for now, real auth logic would be complex
            # Assuming 'owner_id' check or public
            query = query.filter((Collection.owner_id == user_id) | (Collection.is_public == True))
        
        return [c.to_dict() for c in query.all()]

    def register_collection(self, collection_id: str, name: str, provider: str, 
                          description: str = "", owner_id: Optional[int] = None, 
                          is_public: bool = False, metadata: Optional[Dict] = None) -> Optional[Dict]:
        """Register collection"""
        try:
            col = Collection(
                collection_id=collection_id,
                name=name,
                provider=provider,
                description=description,
                owner_id=owner_id,
                is_public=is_public
            )
            col.meta = metadata or {}
            db.session.add(col)
            db.session.commit()
            return col.to_dict()
        except Exception as e:
            db.session.rollback()
            print(f"Error registering collection: {e}")
            return None

    def delete_collection(self, collection_id: str) -> bool:
        """Delete collection by RAG ID"""
        try:
            col = Collection.query.filter_by(collection_id=collection_id).first()
            if col:
                db.session.delete(col)
                db.session.commit()
                return True
            return False
        except Exception:
            db.session.rollback()
            return False

    def get_collection_by_rag_id(self, collection_id: str) -> Optional[Dict]:
        try:
            col = Collection.query.filter_by(collection_id=collection_id).first()
            return col.to_dict() if col else None
        except:
            return None

    def update_collection(self, collection_id: str, 
                         name: str = None, description: str = None,
                         is_public: bool = None, metadata: Dict = None) -> Optional[Dict]:
        """Update collection metadata"""
        try:
            col = Collection.query.filter_by(collection_id=collection_id).first()
            if not col:
                return None
            
            if name is not None:
                col.name = name
            if description is not None:
                col.description = description
            if is_public is not None:
                col.is_public = is_public
            if metadata is not None:
                col.meta = metadata
            
            db.session.commit()
            return col.to_dict()
        except Exception as e:
            db.session.rollback()
            print(f"Error updating collection: {e}")
            return None

    def register_document(self, document_id: str, collection_id: str,
                        source: str = "", title: str = "",
                        chunk_count: int = 0, uploaded_by: Optional[int] = None,
                        metadata: Optional[Dict] = None,
                        content_hash: str = None) -> Optional[Dict]:
        """Register document"""
        try:
            # Need to find integer ID for collection
            col = Collection.query.filter_by(collection_id=collection_id).first()
            if not col:
                return None
            
            # Extract content_hash from metadata if not provided directly
            if content_hash is None and metadata:
                content_hash = metadata.get('content_hash')

            doc = Document(
                document_id=document_id,
                collection_id=col.id,
                source=source,
                title=title,
                content_hash=content_hash,
                chunk_count=chunk_count,
                uploaded_by=uploaded_by
            )
            doc.meta = metadata or {}
            db.session.add(doc)
            
            # Update count
            col.document_count += 1
            
            db.session.commit()
            return doc.to_dict()
        except Exception as e:
            db.session.rollback()
            print(f"Error registering document: {e}")
            return None

    def delete_document(self, document_id: str, collection_id: str) -> bool:
        """Delete document"""
        try:
            col = Collection.query.filter_by(collection_id=collection_id).first()
            if not col:
                return False
                
            doc = Document.query.filter_by(document_id=document_id, collection_id=col.id).first()
            if doc:
                db.session.delete(doc)
                col.document_count = max(0, col.document_count - 1)
                db.session.commit()
                return True
            return False
        except:
            db.session.rollback()
            return False
    
    def get_document_by_source(self, source: str, collection_id: str) -> Optional[Dict]:
        """Get document by source (filename) for update detection"""
        try:
            col = Collection.query.filter_by(collection_id=collection_id).first()
            if not col:
                return None
            doc = Document.query.filter_by(source=source, collection_id=col.id).first()
            return doc.to_dict() if doc else None
        except:
            return None
    
    def get_document_by_hash(self, content_hash: str, collection_id: str) -> Optional[Dict]:
        """Get document by content hash for deduplication"""
        try:
            col = Collection.query.filter_by(collection_id=collection_id).first()
            if not col:
                return None
            doc = Document.query.filter_by(content_hash=content_hash, collection_id=col.id).first()
            return doc.to_dict() if doc else None
        except:
            return None
    
    def update_document(self, document_id: str, collection_id: str,
                       source: str = None, title: str = None,
                       content_hash: str = None, chunk_count: int = None,
                       metadata: Dict = None) -> Optional[Dict]:
        """Update an existing document"""
        try:
            col = Collection.query.filter_by(collection_id=collection_id).first()
            if not col:
                return None
            
            doc = Document.query.filter_by(document_id=document_id, collection_id=col.id).first()
            if not doc:
                return None
            
            if source is not None:
                doc.source = source
            if title is not None:
                doc.title = title
            if content_hash is not None:
                doc.content_hash = content_hash
            if chunk_count is not None:
                doc.chunk_count = chunk_count
            if metadata is not None:
                doc.meta = metadata
            
            db.session.commit()
            return doc.to_dict()
        except Exception as e:
            db.session.rollback()
            print(f"Error updating document: {e}")
            return None
            
    def list_documents(self, collection_id: str) -> List[Dict]:
        """List documents"""
        try:
            col = Collection.query.filter_by(collection_id=collection_id).first()
            if not col:
                return []
            return [d.to_dict() for d in col.documents]
        except:
            return []
    
    # =====================
    # User Management
    # =====================
    
    def list_users(self, active_only: bool = True) -> List[User]:
        """List all users"""
        try:
            query = User.query
            if active_only:
                query = query.filter_by(is_active=True)
            return query.all()
        except:
            return []
    
    def get_user(self, user_id: int) -> Optional[User]:
        """Get user by ID"""
        try:
            return User.query.get(user_id)
        except:
            return None
    
    def create_user(self, username: str, display_name: str = "", 
                   email: str = None, is_admin: bool = False,
                   metadata: Dict = None) -> Optional[User]:
        """Create a new user"""
        try:
            # Check if username exists
            if User.query.filter_by(username=username).first():
                return None
            
            user = User(
                username=username,
                display_name=display_name or username,
                email=email,
                is_admin=is_admin
            )
            db.session.add(user)
            db.session.commit()
            return user
        except Exception as e:
            db.session.rollback()
            print(f"Error creating user: {e}")
            return None
    
    def delete_user(self, user_id: int) -> bool:
        """Delete a user"""
        try:
            user = User.query.get(user_id)
            if user:
                db.session.delete(user)
                db.session.commit()
                return True
            return False
        except:
            db.session.rollback()
            return False
    
    # =====================
    # Group Management
    # =====================
    
    def list_groups(self, active_only: bool = True) -> List[Group]:
        """List all groups"""
        try:
            query = Group.query
            if active_only:
                query = query.filter_by(is_active=True)
            return query.all()
        except:
            return []
    
    def get_group(self, group_id: int) -> Optional[Group]:
        """Get group by ID"""
        try:
            return Group.query.get(group_id)
        except:
            return None
    
    def create_group(self, name: str, description: str = "",
                    metadata: Dict = None) -> Optional[Group]:
        """Create a new group"""
        try:
            # Check if name exists
            if Group.query.filter_by(name=name).first():
                return None
            
            group = Group(
                name=name,
                description=description
            )
            db.session.add(group)
            db.session.commit()
            return group
        except Exception as e:
            db.session.rollback()
            print(f"Error creating group: {e}")
            return None
    
    def add_user_to_group(self, user_id: int, group_id: int) -> bool:
        """Add a user to a group"""
        try:
            user = User.query.get(user_id)
            group = Group.query.get(group_id)
            if user and group:
                if user not in group.members:
                    group.members.append(user)
                    db.session.commit()
                return True
            return False
        except:
            db.session.rollback()
            return False
    
    def remove_user_from_group(self, user_id: int, group_id: int) -> bool:
        """Remove a user from a group"""
        try:
            user = User.query.get(user_id)
            group = Group.query.get(group_id)
            if user and group and user in group.members:
                group.members.remove(user)
                db.session.commit()
                return True
            return False
        except:
            db.session.rollback()
            return False
    
    # =====================
    # Access Control
    # =====================
    
    def get_resource_access(self, resource_type: str, resource_id: int) -> List[Dict]:
        """Get access privileges for a resource"""
        try:
            privileges = AccessPrivilege.query.filter_by(
                resource_type=resource_type,
                resource_id=resource_id
            ).all()
            
            result = []
            for p in privileges:
                entry = {
                    'id': p.id,
                    'access_level': p.access_level,
                    'created_at': p.created_at.isoformat() if p.created_at else None
                }
                if p.user_id:
                    user = User.query.get(p.user_id)
                    entry['user_id'] = p.user_id
                    entry['user_name'] = user.username if user else None
                if p.group_id:
                    group = Group.query.get(p.group_id)
                    entry['group_id'] = p.group_id
                    entry['group_name'] = group.name if group else None
                result.append(entry)
            return result
        except:
            return []
    
    def grant_access(self, resource_type: str, resource_id: int,
                    user_id: int = None, group_id: int = None,
                    access_level: str = 'read', granted_by: int = None) -> bool:
        """Grant access to a resource"""
        try:
            privilege = AccessPrivilege(
                resource_type=resource_type,
                resource_id=resource_id,
                user_id=user_id,
                group_id=group_id,
                access_level=access_level,
                granted_by=granted_by
            )
            db.session.add(privilege)
            db.session.commit()
            return True
        except:
            db.session.rollback()
            return False
    
    def revoke_access(self, privilege_id: int) -> bool:
        """Revoke an access privilege"""
        try:
            privilege = AccessPrivilege.query.get(privilege_id)
            if privilege:
                db.session.delete(privilege)
                db.session.commit()
                return True
            return False
        except:
            db.session.rollback()
            return False
    
    # =====================
    # Audit Log
    # =====================
    
    def get_audit_log(self, resource_type: str = None, resource_id: int = None,
                     user_id: int = None, limit: int = 100) -> List[Dict]:
        """Get audit log entries"""
        try:
            from app.models.core import AuditLog
            query = AuditLog.query
            
            if resource_type:
                query = query.filter_by(resource_type=resource_type)
            if resource_id:
                query = query.filter_by(resource_id=str(resource_id))
            if user_id:
                query = query.filter_by(user_id=user_id)
            
            entries = query.order_by(AuditLog.created_at.desc()).limit(limit).all()
            return [{
                'id': e.id,
                'action': e.action,
                'resource_type': e.resource_type,
                'resource_id': e.resource_id,
                'user_id': e.user_id,
                'details': e.details,
                'ip_address': e.ip_address,
                'created_at': e.created_at.isoformat() if e.created_at else None
            } for e in entries]
        except:
            return []

# Singleton accessor for legacy code
def get_rag_metadata_db():
    return RAGMetadataDB()

# Legacy Aliases
CollectionMeta = Collection
DocumentMeta = Document


# =====================
# Data Source Models for RAG Sync
# =====================

class DataSourceType(str, Enum):
    """Types of data sources for RAG sync"""
    SQLITE = 'sqlite'
    POSTGRESQL = 'postgresql'
    MYSQL = 'mysql'
    MSSQL = 'mssql'
    FILE_SYSTEM = 'file_system'
    API = 'api'
    CSV = 'csv'


class SyncJobStatus(str, Enum):
    """Status of sync jobs"""
    PENDING = 'pending'
    RUNNING = 'running'
    COMPLETED = 'completed'
    FAILED = 'failed'
    CANCELLED = 'cancelled'


class DataSource(db.Model):
    """Data source configuration for RAG document sync"""
    __tablename__ = 'rag_data_sources'
    
    id = db.Column(db.Integer, primary_key=True)
    name = db.Column(db.String(100), nullable=False)
    source_type = db.Column(db.String(50), nullable=False)  # DataSourceType enum value
    description = db.Column(db.Text, nullable=True)
    
    # Connection settings (encrypted in production)
    connection_string = db.Column(db.Text, nullable=True)  # For databases
    host = db.Column(db.String(255), nullable=True)
    port = db.Column(db.Integer, nullable=True)
    database = db.Column(db.String(255), nullable=True)
    username = db.Column(db.String(255), nullable=True)
    password = db.Column(db.Text, nullable=True)  # Should be encrypted
    
    # For file system sources
    base_path = db.Column(db.Text, nullable=True)
    file_patterns = db.Column(db.Text, nullable=True)  # JSON array: ["*.txt", "*.md"]
    recursive = db.Column(db.Boolean, default=True)
    
    # For API sources
    api_url = db.Column(db.Text, nullable=True)
    api_method = db.Column(db.String(10), default='GET')
    api_headers = db.Column(db.Text, nullable=True)  # JSON object
    api_auth_type = db.Column(db.String(50), nullable=True)  # none, basic, bearer, api_key
    api_auth_value = db.Column(db.Text, nullable=True)  # Token or key
    
    # Query/extraction settings
    query = db.Column(db.Text, nullable=True)  # SQL query or JSONPath for APIs
    content_column = db.Column(db.String(100), nullable=True)  # Column to use as content
    title_column = db.Column(db.String(100), nullable=True)  # Column to use as title
    id_column = db.Column(db.String(100), nullable=True)  # Column for unique ID
    
    # Target collection
    target_collection_id = db.Column(db.String(100), nullable=True)
    auto_create_collection = db.Column(db.Boolean, default=True)
    
    # Metadata
    is_active = db.Column(db.Boolean, default=True)
    created_at = db.Column(db.DateTime, default=datetime.utcnow)
    updated_at = db.Column(db.DateTime, default=datetime.utcnow, onupdate=datetime.utcnow)
    created_by = db.Column(db.Integer, db.ForeignKey('users.id'), nullable=True)
    settings_json = db.Column(db.Text, default='{}')  # Additional settings
    
    # Relationships
    creator = db.relationship('User', backref='data_sources')
    sync_jobs = db.relationship('SyncJob', backref='data_source', lazy='dynamic', cascade='all, delete-orphan')
    
    @property
    def settings(self):
        try:
            return json.loads(self.settings_json or '{}')
        except:
            return {}
    
    @settings.setter
    def settings(self, value):
        self.settings_json = json.dumps(value)
    
    @property
    def file_pattern_list(self):
        try:
            return json.loads(self.file_patterns or '[]')
        except:
            return []
    
    @file_pattern_list.setter
    def file_pattern_list(self, value):
        self.file_patterns = json.dumps(value)
    
    def to_dict(self, include_credentials=False):
        result = {
            'id': self.id,
            'name': self.name,
            'source_type': self.source_type,
            'description': self.description,
            'host': self.host,
            'port': self.port,
            'database': self.database,
            'base_path': self.base_path,
            'file_patterns': self.file_pattern_list,
            'recursive': self.recursive,
            'api_url': self.api_url,
            'api_method': self.api_method,
            'api_auth_type': self.api_auth_type,
            'query': self.query,
            'content_column': self.content_column,
            'title_column': self.title_column,
            'id_column': self.id_column,
            'target_collection_id': self.target_collection_id,
            'auto_create_collection': self.auto_create_collection,
            'is_active': self.is_active,
            'created_at': self.created_at.isoformat() if self.created_at else None,
            'updated_at': self.updated_at.isoformat() if self.updated_at else None,
            'settings': self.settings
        }
        if include_credentials:
            result['username'] = self.username
            result['connection_string'] = self.connection_string
        return result


class SyncJob(db.Model):
    """Scheduled sync job for refreshing RAG documents"""
    __tablename__ = 'rag_sync_jobs'
    
    id = db.Column(db.Integer, primary_key=True)
    name = db.Column(db.String(100), nullable=False)
    description = db.Column(db.Text, nullable=True)
    
    # Source configuration
    data_source_id = db.Column(db.Integer, db.ForeignKey('rag_data_sources.id'), nullable=True)
    
    # For file-based jobs without a data source
    source_type = db.Column(db.String(50), nullable=True)  # file_system, data_source
    file_path = db.Column(db.Text, nullable=True)  # For single file or directory
    file_patterns = db.Column(db.Text, nullable=True)  # JSON array
    recursive = db.Column(db.Boolean, default=True)
    
    # Target collection
    collection_id = db.Column(db.String(100), nullable=False)
    
    # Schedule (cron-like)
    schedule_type = db.Column(db.String(20), default='manual')  # manual, interval, cron
    interval_minutes = db.Column(db.Integer, nullable=True)  # For interval type
    cron_expression = db.Column(db.String(100), nullable=True)  # For cron type
    
    # Sync behavior
    sync_mode = db.Column(db.String(20), default='incremental')  # full, incremental
    delete_missing = db.Column(db.Boolean, default=False)  # Delete docs not in source
    
    # Status
    is_active = db.Column(db.Boolean, default=True)
    last_run_at = db.Column(db.DateTime, nullable=True)
    last_status = db.Column(db.String(20), nullable=True)  # SyncJobStatus enum value
    last_error = db.Column(db.Text, nullable=True)
    last_doc_count = db.Column(db.Integer, default=0)
    next_run_at = db.Column(db.DateTime, nullable=True)
    
    # Metadata
    created_at = db.Column(db.DateTime, default=datetime.utcnow)
    updated_at = db.Column(db.DateTime, default=datetime.utcnow, onupdate=datetime.utcnow)
    created_by = db.Column(db.Integer, db.ForeignKey('users.id'), nullable=True)
    settings_json = db.Column(db.Text, default='{}')
    
    # Relationships
    creator = db.relationship('User', backref='sync_jobs')
    runs = db.relationship('SyncJobRun', backref='job', lazy='dynamic', cascade='all, delete-orphan')
    
    @property
    def settings(self):
        try:
            return json.loads(self.settings_json or '{}')
        except:
            return {}
    
    @settings.setter
    def settings(self, value):
        self.settings_json = json.dumps(value)
    
    @property
    def file_pattern_list(self):
        try:
            return json.loads(self.file_patterns or '[]')
        except:
            return []
    
    def to_dict(self):
        return {
            'id': self.id,
            'name': self.name,
            'description': self.description,
            'data_source_id': self.data_source_id,
            'data_source_name': self.data_source.name if self.data_source else None,
            'source_type': self.source_type,
            'file_path': self.file_path,
            'file_patterns': self.file_pattern_list,
            'recursive': self.recursive,
            'collection_id': self.collection_id,
            'schedule_type': self.schedule_type,
            'interval_minutes': self.interval_minutes,
            'cron_expression': self.cron_expression,
            'sync_mode': self.sync_mode,
            'delete_missing': self.delete_missing,
            'is_active': self.is_active,
            'last_run_at': self.last_run_at.isoformat() if self.last_run_at else None,
            'last_status': self.last_status,
            'last_error': self.last_error,
            'last_doc_count': self.last_doc_count,
            'next_run_at': self.next_run_at.isoformat() if self.next_run_at else None,
            'created_at': self.created_at.isoformat() if self.created_at else None,
            'settings': self.settings
        }


class SyncJobRun(db.Model):
    """History of sync job executions"""
    __tablename__ = 'rag_sync_job_runs'
    
    id = db.Column(db.Integer, primary_key=True)
    job_id = db.Column(db.Integer, db.ForeignKey('rag_sync_jobs.id'), nullable=False)
    
    started_at = db.Column(db.DateTime, default=datetime.utcnow)
    completed_at = db.Column(db.DateTime, nullable=True)
    status = db.Column(db.String(20), default='running')  # SyncJobStatus enum value
    
    # Results
    documents_added = db.Column(db.Integer, default=0)
    documents_updated = db.Column(db.Integer, default=0)
    documents_deleted = db.Column(db.Integer, default=0)
    documents_failed = db.Column(db.Integer, default=0)
    
    error_message = db.Column(db.Text, nullable=True)
    log = db.Column(db.Text, nullable=True)  # Detailed log
    
    def to_dict(self):
        return {
            'id': self.id,
            'job_id': self.job_id,
            'started_at': self.started_at.isoformat() if self.started_at else None,
            'completed_at': self.completed_at.isoformat() if self.completed_at else None,
            'status': self.status,
            'documents_added': self.documents_added,
            'documents_updated': self.documents_updated,
            'documents_deleted': self.documents_deleted,
            'documents_failed': self.documents_failed,
            'error_message': self.error_message,
            'duration_seconds': (self.completed_at - self.started_at).total_seconds() if self.completed_at and self.started_at else None
        }
