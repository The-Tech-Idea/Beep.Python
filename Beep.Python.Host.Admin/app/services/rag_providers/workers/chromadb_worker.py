#!/usr/bin/env python
"""
ChromaDB RAG Worker Script

This script runs in the RAG venv and handles ChromaDB operations.
It receives commands via stdin (JSON) and outputs results to stdout (JSON).

Usage:
    echo '{"action": "check"}' | python chromadb_worker.py
"""
import sys
import json
import os
from pathlib import Path


# Global client instance for session persistence
_client = None
_data_path = None


def get_client(data_path: str = None):
    """Get or create ChromaDB client"""
    global _client, _data_path
    
    if data_path:
        _data_path = data_path
    
    if _client is None or data_path != _data_path:
        import chromadb
        from chromadb.config import Settings
        
        persist_dir = _data_path or str(Path.home() / '.beep-rag' / 'data' / 'chromadb')
        Path(persist_dir).mkdir(parents=True, exist_ok=True)
        
        _client = chromadb.PersistentClient(
            path=persist_dir,
            settings=Settings(anonymized_telemetry=False)
        )
        _data_path = persist_dir
    
    return _client


def check_availability():
    """Check if ChromaDB is available"""
    try:
        import chromadb
        from sentence_transformers import SentenceTransformer
        return {
            'available': True, 
            'chromadb_version': chromadb.__version__ if hasattr(chromadb, '__version__') else 'unknown'
        }
    except ImportError as e:
        return {'available': False, 'error': str(e)}


def initialize(data_path: str, embedding_model: str = "all-MiniLM-L6-v2"):
    """Initialize ChromaDB"""
    try:
        client = get_client(data_path)
        
        # Test embedding model
        from sentence_transformers import SentenceTransformer
        embedder = SentenceTransformer(embedding_model)
        dim = embedder.get_sentence_embedding_dimension()
        
        return {
            'success': True,
            'embedding_dim': dim,
            'model': embedding_model,
            'data_path': data_path
        }
    except Exception as e:
        return {'success': False, 'error': str(e)}


def list_collections(data_path: str):
    """List all collections"""
    try:
        client = get_client(data_path)
        collections = client.list_collections()
        
        result = []
        for coll in collections:
            result.append({
                'name': coll.name,
                'count': coll.count(),
                'metadata': coll.metadata or {}
            })
        
        return {'success': True, 'collections': result}
    except Exception as e:
        return {'success': False, 'error': str(e)}


def create_collection(data_path: str, name: str, metadata: dict = None):
    """Create a new collection"""
    try:
        client = get_client(data_path)
        
        # Check if exists
        existing = [c.name for c in client.list_collections()]
        if name in existing:
            return {'success': False, 'error': f'Collection {name} already exists'}
        
        coll = client.create_collection(
            name=name,
            metadata=metadata or {}
        )
        
        return {
            'success': True,
            'name': coll.name,
            'metadata': coll.metadata or {}
        }
    except Exception as e:
        return {'success': False, 'error': str(e)}


def delete_collection(data_path: str, name: str):
    """Delete a collection"""
    try:
        client = get_client(data_path)
        client.delete_collection(name)
        return {'success': True, 'deleted': name}
    except Exception as e:
        return {'success': False, 'error': str(e)}


def add_documents(data_path: str, collection_name: str, 
                  documents: list, ids: list, metadatas: list = None,
                  embedding_model: str = "all-MiniLM-L6-v2"):
    """Add documents to a collection"""
    try:
        from sentence_transformers import SentenceTransformer
        
        client = get_client(data_path)
        coll = client.get_collection(collection_name)
        
        # Generate embeddings
        embedder = SentenceTransformer(embedding_model)
        embeddings = embedder.encode(documents, convert_to_numpy=True).tolist()
        
        # Add to collection
        coll.add(
            documents=documents,
            embeddings=embeddings,
            ids=ids,
            metadatas=metadatas
        )
        
        return {
            'success': True,
            'added': len(documents),
            'collection': collection_name
        }
    except Exception as e:
        return {'success': False, 'error': str(e)}


def query(data_path: str, collection_name: str, query_text: str, 
          top_k: int = 5, embedding_model: str = "all-MiniLM-L6-v2"):
    """Query a collection"""
    try:
        from sentence_transformers import SentenceTransformer
        
        client = get_client(data_path)
        coll = client.get_collection(collection_name)
        
        # Generate query embedding
        embedder = SentenceTransformer(embedding_model)
        query_embedding = embedder.encode([query_text], convert_to_numpy=True).tolist()
        
        # Query
        results = coll.query(
            query_embeddings=query_embedding,
            n_results=top_k,
            include=['documents', 'metadatas', 'distances']
        )
        
        # Format results
        formatted = []
        if results['documents'] and results['documents'][0]:
            for i, doc in enumerate(results['documents'][0]):
                formatted.append({
                    'id': results['ids'][0][i] if results['ids'] else None,
                    'document': doc,
                    'metadata': results['metadatas'][0][i] if results['metadatas'] else {},
                    'distance': results['distances'][0][i] if results['distances'] else 0
                })
        
        return {
            'success': True,
            'results': formatted,
            'count': len(formatted)
        }
    except Exception as e:
        return {'success': False, 'error': str(e)}


def get_collection_info(data_path: str, collection_name: str):
    """Get info about a collection"""
    try:
        client = get_client(data_path)
        coll = client.get_collection(collection_name)
        
        return {
            'success': True,
            'name': coll.name,
            'count': coll.count(),
            'metadata': coll.metadata or {}
        }
    except Exception as e:
        return {'success': False, 'error': str(e)}


def main():
    """Main entry point - reads JSON from stdin"""
    try:
        # Read input
        input_data = json.loads(sys.stdin.read())
        action = input_data.get('action', 'check')
        data_path = input_data.get('data_path', str(Path.home() / '.beep-rag' / 'data' / 'chromadb'))
        embedding_model = input_data.get('embedding_model', 'all-MiniLM-L6-v2')
        
        # Dispatch to action handlers
        if action == 'check':
            result = check_availability()
        elif action == 'initialize':
            result = initialize(data_path, embedding_model)
        elif action == 'list_collections':
            result = list_collections(data_path)
        elif action == 'create_collection':
            result = create_collection(
                data_path,
                input_data.get('name'),
                input_data.get('metadata')
            )
        elif action == 'delete_collection':
            result = delete_collection(data_path, input_data.get('name'))
        elif action == 'add_documents':
            result = add_documents(
                data_path,
                input_data.get('collection_name'),
                input_data.get('documents', []),
                input_data.get('ids', []),
                input_data.get('metadatas'),
                embedding_model
            )
        elif action == 'query':
            result = query(
                data_path,
                input_data.get('collection_name'),
                input_data.get('query_text'),
                input_data.get('top_k', 5),
                embedding_model
            )
        elif action == 'get_collection_info':
            result = get_collection_info(data_path, input_data.get('name'))
        else:
            result = {'success': False, 'error': f'Unknown action: {action}'}
        
        # Output result
        print(json.dumps(result))
        
    except json.JSONDecodeError as e:
        print(json.dumps({'success': False, 'error': f'Invalid JSON input: {e}'}))
    except Exception as e:
        print(json.dumps({'success': False, 'error': str(e)}))


if __name__ == '__main__':
    main()
