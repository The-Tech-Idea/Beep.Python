"""
Base RAG Provider Interface

Defines the abstract interface that all RAG providers must implement.
"""
import os
from abc import ABC, abstractmethod
from dataclasses import dataclass, field, asdict
from typing import Optional, List, Dict, Any
from enum import Enum
from pathlib import Path


class RAGProviderType(Enum):
    """Available RAG provider types"""
    EXTERNAL_API = "external_api"
    FAISS = "faiss"
    CHROMADB = "chromadb"


@dataclass
class RAGContext:
    """Context retrieved from RAG system"""
    id: str
    content: str
    source: str
    relevance_score: float = 0.0
    metadata: Dict[str, Any] = field(default_factory=dict)
    
    def to_dict(self) -> dict:
        return asdict(self)


@dataclass
class RAGQuery:
    """A RAG query"""
    query: str
    user_id: Optional[str] = None
    session_id: Optional[str] = None
    collection_ids: List[str] = field(default_factory=list)
    max_results: int = 5
    min_relevance: float = 0.0
    filters: Dict[str, Any] = field(default_factory=dict)
    
    def to_dict(self) -> dict:
        return asdict(self)


@dataclass
class RAGConfig:
    """Base RAG configuration"""
    provider_type: str = "external_api"
    enabled: bool = False
    
    # Context settings
    max_context_length: int = 4000
    context_template: str = """## Relevant Context

The following information may be helpful:

{context}

---
"""
    
    # Cache settings
    cache_enabled: bool = True
    cache_ttl_seconds: int = 300
    
    # Data path for local providers
    data_path: str = ""
    
    # Provider-specific settings
    provider_settings: Dict[str, Any] = field(default_factory=dict)
    
    def to_dict(self) -> dict:
        return asdict(self)
    
    @classmethod
    def get_default_data_path(cls) -> str:
        """Get default data path for RAG storage"""
        # Use app's own folder - no fallback to user home
        from app.config_manager import get_app_directory
        return str(get_app_directory() / 'rag_data')


@dataclass
class Collection:
    """A document collection"""
    id: str
    name: str
    description: str = ""
    doc_count: int = 0
    metadata: Dict[str, Any] = field(default_factory=dict)
    
    def to_dict(self) -> dict:
        return asdict(self)


@dataclass
class Document:
    """A document to be indexed"""
    id: str
    content: str
    source: str
    metadata: Dict[str, Any] = field(default_factory=dict)
    collection_id: Optional[str] = None
    
    def to_dict(self) -> dict:
        return asdict(self)


class RAGProvider(ABC):
    """
    Abstract base class for RAG providers
    
    All RAG providers (External API, FAISS, ChromaDB) must implement this interface.
    """
    
    @property
    @abstractmethod
    def provider_type(self) -> RAGProviderType:
        """Return the provider type"""
        pass
    
    @property
    @abstractmethod
    def is_available(self) -> bool:
        """Check if the provider is available (dependencies installed)"""
        pass
    
    @abstractmethod
    def get_install_instructions(self) -> str:
        """Get instructions for installing required dependencies"""
        pass
    
    @abstractmethod
    def initialize(self, config: RAGConfig) -> bool:
        """
        Initialize the provider with configuration
        
        Args:
            config: RAG configuration
            
        Returns:
            True if initialization successful
        """
        pass
    
    @abstractmethod
    def get_status(self) -> Dict[str, Any]:
        """
        Get provider status
        
        Returns:
            Status dict with keys: status, message, details
        """
        pass
    
    # =====================
    # Authorization
    # =====================
    
    @abstractmethod
    def check_authorization(self,
                           user_id: str,
                           action: str,
                           resource_type: str = "collection",
                           resource_id: Optional[str] = None) -> Dict[str, Any]:
        """
        Check if user is authorized for an action
        
        Args:
            user_id: User identifier
            action: Action (query, upload, delete, list)
            resource_type: Resource type (collection, document)
            resource_id: Optional specific resource ID
            
        Returns:
            {allowed: bool, reason?: str, privileges?: list}
        """
        pass
    
    # =====================
    # Context Retrieval
    # =====================
    
    @abstractmethod
    def retrieve_context(self, query: RAGQuery) -> List[RAGContext]:
        """
        Retrieve relevant context for a query
        
        Args:
            query: RAG query with search parameters
            
        Returns:
            List of relevant contexts
        """
        pass
    
    # =====================
    # Collection Management
    # =====================
    
    @abstractmethod
    def list_collections(self, user_id: Optional[str] = None) -> List[Collection]:
        """
        List available collections
        
        Args:
            user_id: Optional user ID for filtering
            
        Returns:
            List of collections
        """
        pass
    
    @abstractmethod
    def create_collection(self, 
                         name: str, 
                         description: str = "",
                         metadata: Optional[Dict[str, Any]] = None) -> Collection:
        """
        Create a new collection
        
        Args:
            name: Collection name
            description: Collection description
            metadata: Optional metadata
            
        Returns:
            Created collection
        """
        pass
    
    @abstractmethod
    def delete_collection(self, collection_id: str) -> bool:
        """
        Delete a collection
        
        Args:
            collection_id: Collection ID to delete
            
        Returns:
            True if deleted
        """
        pass
    
    # =====================
    # Document Management
    # =====================
    
    @abstractmethod
    def add_documents(self,
                     documents: List[Document],
                     collection_id: str) -> Dict[str, Any]:
        """
        Add documents to a collection
        
        Args:
            documents: List of documents to add
            collection_id: Target collection ID
            
        Returns:
            {success: bool, added: int, errors: list}
        """
        pass
    
    @abstractmethod
    def delete_documents(self,
                        document_ids: List[str],
                        collection_id: str) -> Dict[str, Any]:
        """
        Delete documents from a collection
        
        Args:
            document_ids: List of document IDs to delete
            collection_id: Collection ID
            
        Returns:
            {success: bool, deleted: int}
        """
        pass
    
    @abstractmethod
    def search_documents(self,
                        query: str,
                        collection_id: str,
                        limit: int = 10) -> List[Document]:
        """
        Search documents in a collection (text search, not vector)
        
        Args:
            query: Search query
            collection_id: Collection ID
            limit: Max results
            
        Returns:
            List of matching documents
        """
        pass
