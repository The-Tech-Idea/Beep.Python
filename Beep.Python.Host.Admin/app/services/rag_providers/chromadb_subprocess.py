"""
Subprocess-based ChromaDB RAG Provider

Uses the RAG venv to execute ChromaDB operations via subprocess.
This allows using ChromaDB without installing it in the Flask environment.
"""
import os
import json
import uuid
import subprocess
import platform
from pathlib import Path
from typing import Optional, List, Dict, Any
from datetime import datetime

from .base import (
    RAGProvider, RAGProviderType, RAGConfig, RAGContext, RAGQuery,
    Collection, Document
)


class SubprocessChromaDBProvider(RAGProvider):
    """
    ChromaDB-based RAG Provider using subprocess execution.
    
    All ChromaDB operations run in the RAG venv via subprocess.
    """
    
    def __init__(self):
        self._config: Optional[RAGConfig] = None
        self._data_path: Path = Path.home() / '.beep-rag' / 'data' / 'chromadb'
        self._rag_venv = Path.home() / '.beep-rag' / 'venv'
        self._embedding_model: str = "all-MiniLM-L6-v2"
        self._initialized = False
        self._is_windows = platform.system() == 'Windows'
        
        # Worker script path
        self._worker_path = Path(__file__).parent / 'workers' / 'chromadb_worker.py'
        
        # In-memory cache for collections
        self._collections: Dict[str, Collection] = {}
    
    @property
    def python_exe(self) -> Path:
        """Get RAG venv Python executable"""
        if self._is_windows:
            return self._rag_venv / 'Scripts' / 'python.exe'
        else:
            return self._rag_venv / 'bin' / 'python3'
    
    @property
    def provider_type(self) -> RAGProviderType:
        return RAGProviderType.CHROMADB
    
    @property
    def is_available(self) -> bool:
        """Check if ChromaDB is installed in RAG venv (uses config file for fast check)"""
        # Check if venv exists first (fast check)
        if not self.python_exe.exists():
            return False
        
        # Check the RAG environment config file (fast file read)
        config_file = self._rag_venv.parent / 'config' / 'environment.json'
        if config_file.exists():
            try:
                with open(config_file, 'r') as f:
                    config = json.load(f)
                
                if config.get('installed'):
                    packages = {p.get('name'): p.get('installed', False) for p in config.get('packages', [])}
                    return packages.get('chromadb', False)
            except Exception:
                pass
        
        return False
    
    def _execute_worker(self, data: dict, timeout: int = 120) -> dict:
        """Execute the ChromaDB worker script with the given data"""
        if not self.python_exe.exists():
            return {'success': False, 'error': 'RAG venv not found'}
        
        try:
            result = subprocess.run(
                [str(self.python_exe), str(self._worker_path)],
                input=json.dumps(data),
                capture_output=True,
                text=True,
                timeout=timeout
            )
            
            if result.returncode == 0:
                try:
                    return json.loads(result.stdout.strip())
                except json.JSONDecodeError:
                    return {'success': False, 'error': f'Invalid JSON: {result.stdout}'}
            else:
                return {'success': False, 'error': result.stderr.strip() or f'Exit code: {result.returncode}'}
                
        except subprocess.TimeoutExpired:
            return {'success': False, 'error': f'Timeout after {timeout}s'}
        except Exception as e:
            return {'success': False, 'error': str(e)}
    
    def get_install_instructions(self) -> str:
        """Get installation instructions"""
        return """
To use ChromaDB RAG provider, run the RAG wizard to install packages:
    - chromadb
    - sentence-transformers
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
            self._data_path = Path.home() / '.beep-rag' / 'data' / 'chromadb'
        
        self._data_path.mkdir(parents=True, exist_ok=True)
        
        # Get embedding model from settings
        settings = config.provider_settings
        self._embedding_model = settings.get('embedding_model', 'all-MiniLM-L6-v2')
        
        # Initialize via worker
        result = self._execute_worker({
            'action': 'initialize',
            'data_path': str(self._data_path),
            'embedding_model': self._embedding_model
        })
        
        if result.get('success'):
            self._initialized = True
            self._sync_collections()
            return True
        
        return False
    
    def _sync_collections(self):
        """Sync collections from ChromaDB"""
        result = self._execute_worker({
            'action': 'list_collections',
            'data_path': str(self._data_path)
        })
        
        if result.get('success'):
            self._collections = {}
            for coll in result.get('collections', []):
                collection = Collection(
                    id=coll['name'],  # ChromaDB uses name as ID
                    name=coll['name'],
                    description=coll.get('metadata', {}).get('description', ''),
                    doc_count=coll.get('count', 0),
                    metadata=coll.get('metadata', {})
                )
                self._collections[coll['name']] = collection
    
    def get_status(self) -> Dict[str, Any]:
        """Get provider status"""
        if not self.is_available:
            return {
                'status': 'unavailable',
                'message': 'ChromaDB not installed in RAG venv',
                'install': self.get_install_instructions()
            }
        
        return {
            'status': 'ready' if self._initialized else 'not_initialized',
            'provider': 'chromadb',
            'embedding_model': self._embedding_model,
            'collections': len(self._collections),
            'data_path': str(self._data_path)
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
        if not self._initialized:
            return []
        
        contexts = []
        collection_ids = query.collection_ids or list(self._collections.keys())
        
        for coll_id in collection_ids:
            if coll_id not in self._collections:
                continue
            
            result = self._execute_worker({
                'action': 'query',
                'data_path': str(self._data_path),
                'collection_name': coll_id,
                'query_text': query.query,
                'top_k': query.max_results,
                'embedding_model': self._embedding_model
            })
            
            if not result.get('success'):
                continue
            
            for item in result.get('results', []):
                # Convert distance to similarity score (ChromaDB uses L2 distance)
                distance = item.get('distance', 0)
                relevance = max(0, 1 - (distance / 2))  # Rough normalization
                
                if relevance >= query.min_relevance:
                    contexts.append(RAGContext(
                        id=item.get('id', ''),
                        content=item.get('document', ''),
                        source=item.get('metadata', {}).get('source', 'unknown'),
                        relevance_score=relevance,
                        metadata={**item.get('metadata', {}), 'collection_id': coll_id}
                    ))
        
        contexts.sort(key=lambda x: x.relevance_score, reverse=True)
        return contexts[:query.max_results]
    
    def list_collections(self, user_id: Optional[str] = None) -> List[Collection]:
        """List all collections"""
        self._sync_collections()
        return list(self._collections.values())
    
    def create_collection(self,
                         name: str,
                         description: str = "",
                         metadata: Optional[Dict[str, Any]] = None) -> Collection:
        """Create a new collection"""
        meta = metadata or {}
        meta['description'] = description
        
        result = self._execute_worker({
            'action': 'create_collection',
            'data_path': str(self._data_path),
            'name': name,
            'metadata': meta
        })
        
        if result.get('success'):
            collection = Collection(
                id=name,
                name=name,
                description=description,
                doc_count=0,
                metadata=meta
            )
            self._collections[name] = collection
            return collection
        else:
            raise ValueError(result.get('error', 'Failed to create collection'))
    
    def delete_collection(self, collection_id: str) -> bool:
        """Delete a collection"""
        result = self._execute_worker({
            'action': 'delete_collection',
            'data_path': str(self._data_path),
            'name': collection_id
        })
        
        if result.get('success'):
            self._collections.pop(collection_id, None)
            return True
        return False
    
    def add_documents(self,
                     documents: List[Document],
                     collection_id: str) -> Dict[str, Any]:
        """Add documents to a collection"""
        if collection_id not in self._collections:
            return {'success': False, 'error': 'Collection not found', 'added': 0}
        
        # Prepare data for worker
        doc_texts = []
        doc_ids = []
        doc_metas = []
        
        for doc in documents:
            if not doc.id:
                doc.id = str(uuid.uuid4())[:12]
            
            doc_texts.append(doc.content)
            doc_ids.append(doc.id)
            doc_metas.append({
                'source': doc.source,
                **doc.metadata
            })
        
        result = self._execute_worker({
            'action': 'add_documents',
            'data_path': str(self._data_path),
            'collection_name': collection_id,
            'documents': doc_texts,
            'ids': doc_ids,
            'metadatas': doc_metas,
            'embedding_model': self._embedding_model
        }, timeout=300)
        
        if result.get('success'):
            # Update collection count
            self._sync_collections()
            return {'success': True, 'added': result.get('added', len(documents))}
        
        return {'success': False, 'error': result.get('error', 'Failed to add documents'), 'added': 0}
    
    def delete_documents(self,
                        document_ids: List[str],
                        collection_id: str) -> Dict[str, Any]:
        """Delete documents from a collection"""
        # ChromaDB worker needs delete_documents action
        # For now, this is not implemented in the worker
        return {'success': False, 'error': 'Delete not implemented for ChromaDB', 'deleted': 0}
    
    def search_documents(self,
                        query: str,
                        collection_id: str,
                        limit: int = 10) -> List[Document]:
        """Search documents (uses vector search)"""
        rag_query = RAGQuery(
            query=query,
            collection_ids=[collection_id],
            max_results=limit
        )
        
        contexts = self.retrieve_context(rag_query)
        
        # Convert contexts to documents
        return [
            Document(
                id=ctx.id,
                content=ctx.content,
                source=ctx.source,
                metadata=ctx.metadata
            )
            for ctx in contexts
        ]
