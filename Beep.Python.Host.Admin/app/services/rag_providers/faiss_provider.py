"""
FAISS RAG Provider

Local vector database using Facebook's FAISS library.
Fast, efficient, and works completely offline.

Requirements:
    pip install faiss-cpu  # or faiss-gpu for GPU acceleration
    pip install sentence-transformers  # for embeddings
"""
import os
import sys
import json
import uuid
import pickle
import hashlib
import subprocess
import platform
from pathlib import Path
from typing import Optional, List, Dict, Any
from datetime import datetime

from .base import (
    RAGProvider, RAGProviderType, RAGConfig, RAGContext, RAGQuery,
    Collection, Document
)


def get_rag_venv_python() -> Path:
    """Get the Python executable from the RAG venv"""
    rag_venv = Path.home() / '.beep-rag' / 'venv'
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


class FAISSProvider(RAGProvider):
    """
    FAISS-based RAG Provider for local vector search
    
    Uses sentence-transformers for embeddings and FAISS for similarity search.
    Data is persisted to disk for offline use.
    """
    
    def __init__(self):
        self._config: Optional[RAGConfig] = None
        self._data_path: Path = Path.home() / '.beep-llm' / 'rag_data' / 'faiss'
        self._faiss = None
        self._embedder = None
        self._embedding_model: str = "all-MiniLM-L6-v2"
        self._embedding_dim: int = 384
        
        # In-memory storage
        self._collections: Dict[str, Collection] = {}
        self._documents: Dict[str, Dict[str, Document]] = {}  # collection_id -> {doc_id -> doc}
        self._indices: Dict[str, Any] = {}  # collection_id -> faiss index
        self._doc_id_maps: Dict[str, List[str]] = {}  # collection_id -> [doc_ids in order]
    
    @property
    def provider_type(self) -> RAGProviderType:
        return RAGProviderType.FAISS
    
    @property
    def is_available(self) -> bool:
        """Check if FAISS and sentence-transformers are installed in RAG venv"""
        return check_package_in_rag_venv('faiss') and check_package_in_rag_venv('sentence_transformers')
    
    def get_install_instructions(self) -> str:
        """Get installation instructions"""
        return """
To use FAISS RAG provider, install:
    pip install faiss-cpu sentence-transformers

For GPU acceleration:
    pip install faiss-gpu sentence-transformers
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
            self._data_path = Path(RAGConfig.get_default_data_path()) / 'faiss'
        
        self._data_path.mkdir(parents=True, exist_ok=True)
        
        # Get embedding model from settings
        settings = config.provider_settings
        self._embedding_model = settings.get('embedding_model', 'all-MiniLM-L6-v2')
        
        # Initialize FAISS and embedder
        try:
            import faiss
            from sentence_transformers import SentenceTransformer
            
            self._faiss = faiss
            self._embedder = SentenceTransformer(self._embedding_model)
            self._embedding_dim = self._embedder.get_sentence_embedding_dimension()
            
            # Load existing data
            self._load_data()
            
            return True
            
        except Exception as e:
            print(f"FAISS initialization error: {e}")
            return False
    
    def _load_data(self):
        """Load collections and indices from disk"""
        collections_file = self._data_path / 'collections.json'
        
        if collections_file.exists():
            try:
                with open(collections_file, 'r') as f:
                    data = json.load(f)
                
                for coll_data in data.get('collections', []):
                    coll = Collection(**coll_data)
                    self._collections[coll.id] = coll
                    
                    # Load index and documents for this collection
                    self._load_collection_data(coll.id)
                    
            except Exception as e:
                print(f"Error loading FAISS data: {e}")
    
    def _load_collection_data(self, collection_id: str):
        """Load a collection's index and documents"""
        coll_path = self._data_path / collection_id
        
        # Load documents
        docs_file = coll_path / 'documents.json'
        if docs_file.exists():
            with open(docs_file, 'r') as f:
                docs_data = json.load(f)
            self._documents[collection_id] = {
                d['id']: Document(**d) for d in docs_data
            }
            self._doc_id_maps[collection_id] = list(self._documents[collection_id].keys())
        else:
            self._documents[collection_id] = {}
            self._doc_id_maps[collection_id] = []
        
        # Load FAISS index
        index_file = coll_path / 'index.faiss'
        if index_file.exists():
            self._indices[collection_id] = self._faiss.read_index(str(index_file))
        else:
            # Create empty index
            self._indices[collection_id] = self._faiss.IndexFlatIP(self._embedding_dim)
    
    def _save_data(self):
        """Save collections metadata to disk"""
        collections_file = self._data_path / 'collections.json'
        
        data = {
            'collections': [c.to_dict() for c in self._collections.values()]
        }
        
        with open(collections_file, 'w') as f:
            json.dump(data, f, indent=2)
    
    def _save_collection_data(self, collection_id: str):
        """Save a collection's index and documents"""
        coll_path = self._data_path / collection_id
        coll_path.mkdir(parents=True, exist_ok=True)
        
        # Save documents
        docs = self._documents.get(collection_id, {})
        docs_data = [d.to_dict() for d in docs.values()]
        
        with open(coll_path / 'documents.json', 'w') as f:
            json.dump(docs_data, f, indent=2)
        
        # Save FAISS index
        if collection_id in self._indices:
            self._faiss.write_index(
                self._indices[collection_id],
                str(coll_path / 'index.faiss')
            )
    
    def get_status(self) -> Dict[str, Any]:
        """Get provider status"""
        if not self.is_available:
            return {
                'status': 'unavailable',
                'message': 'FAISS not installed',
                'install': self.get_install_instructions()
            }
        
        return {
            'status': 'ready',
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
        # Local FAISS has no built-in auth
        # You can extend this to check a local permissions file
        return {'allowed': True, 'reason': 'Local provider - no auth required'}
    
    def retrieve_context(self, query: RAGQuery) -> List[RAGContext]:
        """Retrieve context using FAISS similarity search"""
        if not self._embedder or not self._faiss:
            return []
        
        contexts = []
        
        # Determine which collections to search
        collection_ids = query.collection_ids or list(self._collections.keys())
        
        # Embed the query
        query_embedding = self._embedder.encode([query.query])[0]
        
        import numpy as np
        query_vector = np.array([query_embedding]).astype('float32')
        # Normalize for cosine similarity
        self._faiss.normalize_L2(query_vector)
        
        for coll_id in collection_ids:
            if coll_id not in self._indices:
                continue
            
            index = self._indices[coll_id]
            doc_ids = self._doc_id_maps.get(coll_id, [])
            docs = self._documents.get(coll_id, {})
            
            if index.ntotal == 0:
                continue
            
            # Search
            k = min(query.max_results, index.ntotal)
            scores, indices = index.search(query_vector, k)
            
            for score, idx in zip(scores[0], indices[0]):
                if idx < 0 or idx >= len(doc_ids):
                    continue
                
                doc_id = doc_ids[idx]
                doc = docs.get(doc_id)
                
                if not doc:
                    continue
                
                # Convert score to 0-1 range (cosine similarity)
                relevance = float((score + 1) / 2)  # Normalize from [-1,1] to [0,1]
                
                if relevance >= query.min_relevance:
                    contexts.append(RAGContext(
                        id=doc.id,
                        content=doc.content,
                        source=doc.source,
                        relevance_score=relevance,
                        metadata={**doc.metadata, 'collection_id': coll_id}
                    ))
        
        # Sort by relevance and limit
        contexts.sort(key=lambda x: x.relevance_score, reverse=True)
        return contexts[:query.max_results]
    
    def list_collections(self, user_id: Optional[str] = None) -> List[Collection]:
        """List all collections"""
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
        self._documents[collection_id] = {}
        self._doc_id_maps[collection_id] = []
        self._indices[collection_id] = self._faiss.IndexFlatIP(self._embedding_dim)
        
        # Save to disk
        self._save_data()
        (self._data_path / collection_id).mkdir(parents=True, exist_ok=True)
        
        return collection
    
    def delete_collection(self, collection_id: str) -> bool:
        """Delete a collection"""
        if collection_id not in self._collections:
            return False
        
        # Remove from memory
        del self._collections[collection_id]
        self._documents.pop(collection_id, None)
        self._doc_id_maps.pop(collection_id, None)
        self._indices.pop(collection_id, None)
        
        # Remove from disk
        import shutil
        coll_path = self._data_path / collection_id
        if coll_path.exists():
            shutil.rmtree(coll_path)
        
        self._save_data()
        return True
    
    def add_documents(self,
                     documents: List[Document],
                     collection_id: str) -> Dict[str, Any]:
        """Add documents to a collection"""
        if collection_id not in self._collections:
            return {'success': False, 'error': 'Collection not found', 'added': 0}
        
        if not self._embedder:
            return {'success': False, 'error': 'Embedder not initialized', 'added': 0}
        
        import numpy as np
        
        added = 0
        errors = []
        
        # Get embeddings for all documents
        texts = [doc.content for doc in documents]
        embeddings = self._embedder.encode(texts)
        
        for doc, embedding in zip(documents, embeddings):
            try:
                # Generate ID if not provided
                if not doc.id:
                    doc.id = str(uuid.uuid4())[:12]
                
                # Store document
                self._documents[collection_id][doc.id] = doc
                self._doc_id_maps[collection_id].append(doc.id)
                
                # Add to index
                vector = np.array([embedding]).astype('float32')
                self._faiss.normalize_L2(vector)
                self._indices[collection_id].add(vector)
                
                added += 1
                
            except Exception as e:
                errors.append(f"{doc.id}: {str(e)}")
        
        # Update collection doc count
        self._collections[collection_id].doc_count = len(self._documents[collection_id])
        
        # Save to disk
        self._save_collection_data(collection_id)
        self._save_data()
        
        return {
            'success': True,
            'added': added,
            'errors': errors
        }
    
    def delete_documents(self,
                        document_ids: List[str],
                        collection_id: str) -> Dict[str, Any]:
        """Delete documents from a collection"""
        if collection_id not in self._collections:
            return {'success': False, 'error': 'Collection not found', 'deleted': 0}
        
        # FAISS doesn't support deletion easily
        # We need to rebuild the index without deleted docs
        deleted = 0
        docs = self._documents.get(collection_id, {})
        
        for doc_id in document_ids:
            if doc_id in docs:
                del docs[doc_id]
                deleted += 1
        
        if deleted > 0:
            # Rebuild index
            self._rebuild_index(collection_id)
            self._collections[collection_id].doc_count = len(docs)
            self._save_collection_data(collection_id)
            self._save_data()
        
        return {'success': True, 'deleted': deleted}
    
    def _rebuild_index(self, collection_id: str):
        """Rebuild FAISS index for a collection"""
        import numpy as np
        
        docs = self._documents.get(collection_id, {})
        
        # Create new index
        self._indices[collection_id] = self._faiss.IndexFlatIP(self._embedding_dim)
        self._doc_id_maps[collection_id] = []
        
        if not docs:
            return
        
        # Re-embed all documents
        doc_list = list(docs.values())
        texts = [d.content for d in doc_list]
        embeddings = self._embedder.encode(texts)
        
        vectors = np.array(embeddings).astype('float32')
        self._faiss.normalize_L2(vectors)
        
        self._indices[collection_id].add(vectors)
        self._doc_id_maps[collection_id] = [d.id for d in doc_list]
    
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
        docs = self._documents.get(collection_id, {})
        return [docs[ctx.id] for ctx in contexts if ctx.id in docs]
