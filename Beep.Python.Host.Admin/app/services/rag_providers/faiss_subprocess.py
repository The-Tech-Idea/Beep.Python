"""
Subprocess-based FAISS RAG Provider

Uses the RAG venv to execute FAISS operations via subprocess.
This allows using FAISS without installing it in the Flask environment.
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


class SubprocessFAISSProvider(RAGProvider):
    """
    FAISS-based RAG Provider using subprocess execution.
    
    All FAISS operations run in the RAG venv via subprocess.
    """
    
    def __init__(self):
        self._config: Optional[RAGConfig] = None
        # Use app's own folder
        from app.config_manager import get_app_directory
        app_dir = get_app_directory()
        self._data_path: Path = app_dir / 'rag_data' / 'data' / 'faiss'
        # Use providers directory via EnvironmentManager
        providers_path = app_dir / 'providers'
        self._rag_venv = providers_path / 'rag'
        self._embedding_model: str = "all-MiniLM-L6-v2"
        self._embedding_dim: int = 384
        self._initialized = False
        self._is_windows = platform.system() == 'Windows'
        
        # Worker script path - use app directory for frozen builds
        self._worker_path = app_dir / 'app' / 'services' / 'rag_providers' / 'workers' / 'faiss_worker.py'
        
        # In-memory cache for collections (metadata only)
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
        return RAGProviderType.FAISS
    
    @property
    def is_available(self) -> bool:
        """Check if FAISS is installed in RAG venv (uses config file for fast check)"""
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
                    return packages.get('faiss-cpu', False) and packages.get('sentence-transformers', False)
            except Exception:
                pass
        
        return False
    
    def _execute_worker(self, data: dict, timeout: int = 120) -> dict:
        """Execute the FAISS worker script with the given data"""
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
To use FAISS RAG provider, run the RAG wizard to install packages:
    - faiss-cpu (or faiss-gpu for GPU acceleration)
    - sentence-transformers
"""
    
    def initialize(self, config: RAGConfig) -> bool:
        """Initialize FAISS provider"""
        if not self.is_available:
            return False
        
        self._config = config
        
        # Set data path
        if config.data_path:
            self._data_path = Path(config.data_path) / 'faiss'
        else:
            from app.config_manager import get_app_directory
            self._data_path = get_app_directory() / 'rag_data' / 'data' / 'faiss'
        
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
            self._embedding_dim = result.get('embedding_dim', 384)
            self._initialized = True
            self._load_collections()
            return True
        
        return False
    
    def _load_collections(self):
        """Load collections metadata from disk"""
        collections_file = self._data_path / 'collections.json'
        
        if collections_file.exists():
            try:
                with open(collections_file, 'r') as f:
                    data = json.load(f)
                
                self._collections.clear()
                for coll_data in data.get('collections', []):
                    coll = Collection(**coll_data)
                    
                    # Also count actual documents on disk
                    doc_file = self._data_path / coll.id / 'documents.json'
                    if doc_file.exists():
                        try:
                            with open(doc_file, 'r') as df:
                                docs = json.load(df)
                                coll.doc_count = len(docs) if isinstance(docs, list) else 0
                        except:
                            pass
                    
                    self._collections[coll.id] = coll
            except Exception as e:
                print(f"Error loading collections: {e}")
    
    def _save_collections(self):
        """Save collections metadata to disk"""
        self._data_path.mkdir(parents=True, exist_ok=True)
        collections_file = self._data_path / 'collections.json'
        
        data = {
            'collections': [c.to_dict() for c in self._collections.values()]
        }
        
        with open(collections_file, 'w') as f:
            json.dump(data, f, indent=2)
    
    def get_status(self) -> Dict[str, Any]:
        """Get provider status"""
        if not self.is_available:
            return {
                'status': 'unavailable',
                'message': 'FAISS not installed in RAG venv',
                'install': self.get_install_instructions()
            }
        
        return {
            'status': 'ready' if self._initialized else 'not_initialized',
            'provider': 'faiss',
            'embedding_model': self._embedding_model,
            'embedding_dim': self._embedding_dim,
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
        """Retrieve context using FAISS similarity search"""
        if not self._initialized:
            return []
        
        contexts = []
        collection_ids = query.collection_ids or list(self._collections.keys())
        
        for coll_id in collection_ids:
            if coll_id not in self._collections:
                continue
            
            # Create query embedding
            embed_result = self._execute_worker({
                'action': 'create_embeddings',
                'texts': [query.query],
                'embedding_model': self._embedding_model
            })
            
            if not embed_result.get('success'):
                continue
            
            query_embedding = embed_result['embeddings'][0]
            
            # Search index
            index_path = str(self._data_path / coll_id / 'index.faiss')
            search_result = self._execute_worker({
                'action': 'search',
                'query_embedding': query_embedding,
                'index_path': index_path,
                'top_k': query.max_results
            })
            
            if not search_result.get('success'):
                continue
            
            # Load documents
            docs_file = self._data_path / coll_id / 'documents.json'
            if not docs_file.exists():
                continue
            
            with open(docs_file, 'r') as f:
                docs_data = json.load(f)
            
            doc_map = {d['id']: d for d in docs_data}
            doc_ids = [d['id'] for d in docs_data]
            
            # Map results to documents
            for idx, distance in zip(search_result.get('indices', []), search_result.get('distances', [])):
                if idx < 0 or idx >= len(doc_ids):
                    continue
                
                doc_id = doc_ids[idx]
                doc = doc_map.get(doc_id)
                
                if not doc:
                    continue
                
                # Convert L2 distance to similarity score (0-1)
                relevance = max(0, 1 - (distance / 10))  # Rough normalization
                
                if relevance >= query.min_relevance:
                    contexts.append(RAGContext(
                        id=doc['id'],
                        content=doc['content'],
                        source=doc.get('source', 'unknown'),
                        relevance_score=relevance,
                        metadata={**doc.get('metadata', {}), 'collection_id': coll_id}
                    ))
        
        contexts.sort(key=lambda x: x.relevance_score, reverse=True)
        return contexts[:query.max_results]
    
    def list_collections(self, user_id: Optional[str] = None) -> List[Collection]:
        """List all collections (reloads from disk to get latest data)"""
        # Reload from disk to get latest document counts
        self._load_collections()
        return list(self._collections.values())
    
    def create_collection(self,
                         name: str,
                         description: str = "",
                         metadata: Optional[Dict[str, Any]] = None) -> Collection:
        """Create a new collection"""
        collection_id = str(uuid.uuid4())[:8]
        
        collection = Collection(
            id=collection_id,
            name=name,
            description=description,
            doc_count=0,
            metadata=metadata or {}
        )
        
        self._collections[collection_id] = collection
        
        # Create collection directory
        coll_path = self._data_path / collection_id
        coll_path.mkdir(parents=True, exist_ok=True)
        
        # Initialize empty documents
        with open(coll_path / 'documents.json', 'w') as f:
            json.dump([], f)
        
        self._save_collections()
        return collection
    
    def delete_collection(self, collection_id: str) -> bool:
        """Delete a collection"""
        if collection_id not in self._collections:
            return False
        
        del self._collections[collection_id]
        
        # Remove from disk
        import shutil
        coll_path = self._data_path / collection_id
        if coll_path.exists():
            shutil.rmtree(coll_path)
        
        self._save_collections()
        return True
    
    def get_documents(self, collection_id: str) -> List[Dict[str, Any]]:
        """Get all documents in a collection"""
        coll_path = self._data_path / collection_id
        docs_file = coll_path / 'documents.json'
        
        if not docs_file.exists():
            return []
        
        try:
            with open(docs_file, 'r') as f:
                docs = json.load(f)
            
            # Return as list of dicts
            return [
                {
                    'id': doc.get('id', ''),
                    'document_id': doc.get('id', ''),
                    'title': doc.get('metadata', {}).get('title', doc.get('source', 'Untitled')),
                    'source': doc.get('source', ''),
                    'content': doc.get('content', '')[:200] + '...' if len(doc.get('content', '')) > 200 else doc.get('content', ''),
                    'metadata': doc.get('metadata', {}),
                    'created_at': doc.get('metadata', {}).get('created_at', None)
                }
                for doc in docs
            ]
        except Exception as e:
            print(f"Error loading documents: {e}")
            return []
    
    def add_documents(self,
                     documents: List[Document],
                     collection_id: str) -> Dict[str, Any]:
        """Add documents to a collection"""
        if collection_id not in self._collections:
            return {'success': False, 'error': 'Collection not found', 'added': 0}
        
        coll_path = self._data_path / collection_id
        coll_path.mkdir(parents=True, exist_ok=True)
        
        # Load existing documents
        docs_file = coll_path / 'documents.json'
        if docs_file.exists():
            with open(docs_file, 'r') as f:
                existing_docs = json.load(f)
        else:
            existing_docs = []
        
        # Process new documents
        texts = [doc.content for doc in documents]
        
        # Generate IDs if needed
        for doc in documents:
            if not doc.id:
                doc.id = str(uuid.uuid4())[:12]
        
        # Create embeddings
        embed_result = self._execute_worker({
            'action': 'create_embeddings',
            'texts': texts,
            'embedding_model': self._embedding_model
        }, timeout=300)
        
        if not embed_result.get('success'):
            return {'success': False, 'error': embed_result.get('error', 'Embedding failed'), 'added': 0}
        
        embeddings = embed_result['embeddings']
        
        # Add documents to storage
        for doc in documents:
            existing_docs.append(doc.to_dict())
        
        with open(docs_file, 'w') as f:
            json.dump(existing_docs, f, indent=2)
        
        # Add to FAISS index
        index_path = str(coll_path / 'index.faiss')
        index_result = self._execute_worker({
            'action': 'add_to_index',
            'embeddings': embeddings,
            'index_path': index_path
        })
        
        if not index_result.get('success'):
            return {'success': False, 'error': index_result.get('error', 'Index update failed'), 'added': 0}
        
        # Update collection count
        self._collections[collection_id].doc_count = len(existing_docs)
        self._save_collections()
        
        return {'success': True, 'added': len(documents)}
    
    def delete_documents(self,
                        document_ids: List[str],
                        collection_id: str) -> Dict[str, Any]:
        """Delete documents from a collection"""
        if collection_id not in self._collections:
            return {'success': False, 'error': 'Collection not found', 'deleted': 0}
        
        coll_path = self._data_path / collection_id
        docs_file = coll_path / 'documents.json'
        
        if not docs_file.exists():
            return {'success': False, 'error': 'No documents', 'deleted': 0}
        
        with open(docs_file, 'r') as f:
            docs = json.load(f)
        
        # Filter out deleted documents
        original_count = len(docs)
        docs = [d for d in docs if d['id'] not in document_ids]
        deleted = original_count - len(docs)
        
        with open(docs_file, 'w') as f:
            json.dump(docs, f, indent=2)
        
        # Rebuild index if documents were deleted
        if deleted > 0:
            texts = [d['content'] for d in docs]
            if texts:
                embed_result = self._execute_worker({
                    'action': 'create_embeddings',
                    'texts': texts,
                    'embedding_model': self._embedding_model
                }, timeout=300)
                
                if embed_result.get('success'):
                    index_path = str(coll_path / 'index.faiss')
                    self._execute_worker({
                        'action': 'create_index',
                        'embeddings': embed_result['embeddings'],
                        'index_path': index_path
                    })
            
            self._collections[collection_id].doc_count = len(docs)
            self._save_collections()
        
        return {'success': True, 'deleted': deleted}
    
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
        
        # Convert contexts back to documents
        coll_path = self._data_path / collection_id
        docs_file = coll_path / 'documents.json'
        
        if not docs_file.exists():
            return []
        
        with open(docs_file, 'r') as f:
            docs_data = json.load(f)
        
        doc_map = {d['id']: Document(**d) for d in docs_data}
        
        return [doc_map[ctx.id] for ctx in contexts if ctx.id in doc_map]
