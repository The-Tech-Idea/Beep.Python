"""
ChromaDB RAG Provider

Local vector database using ChromaDB.
Easy to use, feature-rich, with built-in embedding support.

Requirements:
    pip install chromadb sentence-transformers
"""
import os
import uuid
import subprocess
import platform
from pathlib import Path
from typing import Optional, List, Dict, Any

from .base import (
    RAGProvider, RAGProviderType, RAGConfig, RAGContext, RAGQuery,
    Collection, Document
)


def get_rag_venv_python() -> Path:
    """Get the Python executable from the RAG venv"""
    # Use app's own folder
    from app.config_manager import get_app_directory
    rag_venv = get_app_directory() / 'rag_data' / 'venv'
    if platform.system() == 'Windows':
        return rag_venv / 'Scripts' / 'python.exe'
    else:
        return rag_venv / 'bin' / 'python3'


def check_package_in_rag_venv(package_name: str) -> bool:
    """Check if a package is installed in the RAG venv"""
    python_exe = get_rag_venv_python()
    if not python_exe.exists():
        return False
    try:
        result = subprocess.run(
            [str(python_exe), '-c', f'import {package_name}'],
            capture_output=True,
            timeout=10
        )
        return result.returncode == 0
    except Exception:
        return False


class ChromaDBProvider(RAGProvider):
    """
    ChromaDB-based RAG Provider for local vector search
    
    ChromaDB provides:
    - Persistent storage
    - Built-in embedding functions
    - Metadata filtering
    - Easy API
    """
    
    def __init__(self):
        self._config: Optional[RAGConfig] = None
        # Use app's own folder - no fallback to user home
        from app.config_manager import get_app_directory
        self._data_path: Path = get_app_directory() / 'rag_data' / 'chromadb'
        self._client = None
        self._embedding_function = None
        self._embedding_model: str = "all-MiniLM-L6-v2"
    
    @property
    def provider_type(self) -> RAGProviderType:
        return RAGProviderType.CHROMADB
    
    @property
    def is_available(self) -> bool:
        """Check if ChromaDB is installed in RAG venv"""
        return check_package_in_rag_venv('chromadb')
    
    def get_install_instructions(self) -> str:
        """Get installation instructions"""
        return """
To use ChromaDB RAG provider, install:
    pip install chromadb sentence-transformers
"""
    
    def initialize(self, config: RAGConfig) -> bool:
        """Initialize ChromaDB provider"""
        if not self.is_available:
            return False
        
        self._config = config
        
        # Set data path
        if config.data_path:
            self._data_path = Path(config.data_path) / 'chromadb'
        else:
            self._data_path = Path(RAGConfig.get_default_data_path()) / 'chromadb'
        
        self._data_path.mkdir(parents=True, exist_ok=True)
        
        # Get settings
        settings = config.provider_settings
        self._embedding_model = settings.get('embedding_model', 'all-MiniLM-L6-v2')
        
        try:
            import chromadb
            from chromadb.config import Settings
            
            # Initialize ChromaDB client with persistence
            self._client = chromadb.PersistentClient(
                path=str(self._data_path),
                settings=Settings(
                    anonymized_telemetry=False,
                    allow_reset=True
                )
            )
            
            # Set up embedding function
            try:
                from chromadb.utils import embedding_functions
                self._embedding_function = embedding_functions.SentenceTransformerEmbeddingFunction(
                    model_name=self._embedding_model
                )
            except:
                # Fallback to default
                self._embedding_function = None
            
            return True
            
        except Exception as e:
            print(f"ChromaDB initialization error: {e}")
            return False
    
    def get_status(self) -> Dict[str, Any]:
        """Get provider status"""
        if not self.is_available:
            return {
                'status': 'unavailable',
                'message': 'ChromaDB not installed',
                'install': self.get_install_instructions()
            }
        
        if not self._client:
            return {
                'status': 'not_initialized',
                'message': 'ChromaDB client not initialized'
            }
        
        try:
            collections = self._client.list_collections()
            return {
                'status': 'ready',
                'provider': 'chromadb',
                'embedding_model': self._embedding_model,
                'collections': len(collections),
                'data_path': str(self._data_path)
            }
        except Exception as e:
            return {
                'status': 'error',
                'message': str(e)
            }
    
    def check_authorization(self,
                           user_id: str,
                           action: str,
                           resource_type: str = "collection",
                           resource_id: Optional[str] = None) -> Dict[str, Any]:
        """Local provider - always authorized"""
        return {'allowed': True, 'reason': 'Local provider - no auth required'}
    
    def retrieve_context(self, query: RAGQuery) -> List[RAGContext]:
        """Retrieve context using ChromaDB similarity search"""
        if not self._client:
            return []
        
        contexts = []
        
        # Determine which collections to search
        if query.collection_ids:
            collection_names = query.collection_ids
        else:
            collection_names = [c.name for c in self._client.list_collections()]
        
        for coll_name in collection_names:
            try:
                # Get collection
                collection = self._client.get_collection(
                    name=coll_name,
                    embedding_function=self._embedding_function
                )
                
                if collection.count() == 0:
                    continue
                
                # Build where filter if provided
                where_filter = None
                if query.filters:
                    where_filter = query.filters
                
                # Query
                results = collection.query(
                    query_texts=[query.query],
                    n_results=min(query.max_results, collection.count()),
                    where=where_filter,
                    include=['documents', 'metadatas', 'distances']
                )
                
                # Process results
                if results and results['documents'] and results['documents'][0]:
                    docs = results['documents'][0]
                    metadatas = results['metadatas'][0] if results['metadatas'] else [{}] * len(docs)
                    distances = results['distances'][0] if results['distances'] else [0.0] * len(docs)
                    ids = results['ids'][0] if results['ids'] else [''] * len(docs)
                    
                    for doc, meta, dist, doc_id in zip(docs, metadatas, distances, ids):
                        # Convert distance to similarity score (ChromaDB uses L2 by default)
                        # Lower distance = higher similarity
                        relevance = max(0, 1 - (dist / 2))  # Approximate normalization
                        
                        if relevance >= query.min_relevance:
                            contexts.append(RAGContext(
                                id=doc_id,
                                content=doc,
                                source=meta.get('source', coll_name),
                                relevance_score=relevance,
                                metadata={**meta, 'collection_id': coll_name}
                            ))
                            
            except Exception as e:
                print(f"ChromaDB query error for {coll_name}: {e}")
                continue
        
        # Sort by relevance and limit
        contexts.sort(key=lambda x: x.relevance_score, reverse=True)
        return contexts[:query.max_results]
    
    def list_collections(self, user_id: Optional[str] = None) -> List[Collection]:
        """List all collections"""
        if not self._client:
            return []
        
        try:
            chroma_collections = self._client.list_collections()
            
            collections = []
            for c in chroma_collections:
                coll = self._client.get_collection(name=c.name)
                collections.append(Collection(
                    id=c.name,  # ChromaDB uses name as ID
                    name=c.name,
                    description=c.metadata.get('description', '') if c.metadata else '',
                    doc_count=coll.count(),
                    metadata=c.metadata or {}
                ))
            
            return collections
            
        except Exception as e:
            print(f"ChromaDB list collections error: {e}")
            return []
    
    def create_collection(self,
                         name: str,
                         description: str = "",
                         metadata: Optional[Dict[str, Any]] = None) -> Collection:
        """Create a new collection"""
        if not self._client:
            raise Exception("ChromaDB not initialized")
        
        # Sanitize name for ChromaDB (alphanumeric and underscores only)
        safe_name = ''.join(c if c.isalnum() or c == '_' else '_' for c in name)
        if not safe_name[0].isalpha():
            safe_name = 'c_' + safe_name
        
        meta = metadata or {}
        meta['description'] = description
        
        try:
            self._client.create_collection(
                name=safe_name,
                metadata=meta,
                embedding_function=self._embedding_function
            )
            
            return Collection(
                id=safe_name,
                name=safe_name,
                description=description,
                doc_count=0,
                metadata=meta
            )
            
        except Exception as e:
            raise Exception(f"Failed to create collection: {e}")
    
    def delete_collection(self, collection_id: str) -> bool:
        """Delete a collection"""
        if not self._client:
            return False
        
        try:
            self._client.delete_collection(name=collection_id)
            return True
        except:
            return False
    
    def add_documents(self,
                     documents: List[Document],
                     collection_id: str) -> Dict[str, Any]:
        """Add documents to a collection"""
        if not self._client:
            return {'success': False, 'error': 'ChromaDB not initialized', 'added': 0}
        
        try:
            collection = self._client.get_collection(
                name=collection_id,
                embedding_function=self._embedding_function
            )
            
            # Prepare data
            ids = []
            contents = []
            metadatas = []
            
            for doc in documents:
                doc_id = doc.id or str(uuid.uuid4())[:12]
                ids.append(doc_id)
                contents.append(doc.content)
                metadatas.append({
                    'source': doc.source,
                    **doc.metadata
                })
            
            # Add to collection
            collection.add(
                ids=ids,
                documents=contents,
                metadatas=metadatas
            )
            
            return {
                'success': True,
                'added': len(documents),
                'errors': []
            }
            
        except Exception as e:
            return {
                'success': False,
                'error': str(e),
                'added': 0
            }
    
    def delete_documents(self,
                        document_ids: List[str],
                        collection_id: str) -> Dict[str, Any]:
        """Delete documents from a collection"""
        if not self._client:
            return {'success': False, 'error': 'ChromaDB not initialized', 'deleted': 0}
        
        try:
            collection = self._client.get_collection(name=collection_id)
            collection.delete(ids=document_ids)
            
            return {
                'success': True,
                'deleted': len(document_ids)
            }
            
        except Exception as e:
            return {
                'success': False,
                'error': str(e),
                'deleted': 0
            }
    
    def search_documents(self,
                        query: str,
                        collection_id: str,
                        limit: int = 10) -> List[Document]:
        """Search documents in a collection"""
        rag_query = RAGQuery(
            query=query,
            collection_ids=[collection_id],
            max_results=limit
        )
        
        contexts = self.retrieve_context(rag_query)
        
        # Convert to documents
        return [
            Document(
                id=ctx.id,
                content=ctx.content,
                source=ctx.source,
                metadata=ctx.metadata,
                collection_id=collection_id
            )
            for ctx in contexts
        ]
    
    def get_collection_info(self, collection_id: str) -> Optional[Dict[str, Any]]:
        """Get detailed collection information"""
        if not self._client:
            return None
        
        try:
            collection = self._client.get_collection(
                name=collection_id,
                embedding_function=self._embedding_function
            )
            
            return {
                'id': collection_id,
                'name': collection.name,
                'count': collection.count(),
                'metadata': collection.metadata
            }
            
        except:
            return None
