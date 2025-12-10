#!/usr/bin/env python
"""
FAISS RAG Worker Script

This script runs in the RAG venv and handles FAISS operations.
It receives commands via stdin (JSON) and outputs results to stdout (JSON).

Usage:
    echo '{"action": "check"}' | python faiss_worker.py
"""
import sys
import json
import os
import pickle
import multiprocessing
from pathlib import Path

# CRITICAL: Required for PyInstaller frozen environments
if __name__ == '__main__':
    multiprocessing.freeze_support()


def check_availability():
    """Check if FAISS is available"""
    try:
        import faiss
        from sentence_transformers import SentenceTransformer
        return {'available': True, 'faiss_version': faiss.__version__ if hasattr(faiss, '__version__') else 'unknown'}
    except ImportError as e:
        return {'available': False, 'error': str(e)}


def initialize(data_path: str, embedding_model: str = "all-MiniLM-L6-v2"):
    """Initialize FAISS and return embedding dimension"""
    try:
        import faiss
        from sentence_transformers import SentenceTransformer
        
        # Create data directory
        Path(data_path).mkdir(parents=True, exist_ok=True)
        
        # Load embedding model
        embedder = SentenceTransformer(embedding_model)
        dim = embedder.get_sentence_embedding_dimension()
        
        return {
            'success': True,
            'embedding_dim': dim,
            'model': embedding_model
        }
    except Exception as e:
        return {'success': False, 'error': str(e)}


def create_embeddings(texts: list, embedding_model: str = "all-MiniLM-L6-v2"):
    """Create embeddings for texts"""
    try:
        from sentence_transformers import SentenceTransformer
        import numpy as np
        
        embedder = SentenceTransformer(embedding_model)
        embeddings = embedder.encode(texts, convert_to_numpy=True)
        
        # Convert to list for JSON serialization
        return {
            'success': True,
            'embeddings': embeddings.tolist(),
            'count': len(texts)
        }
    except Exception as e:
        return {'success': False, 'error': str(e)}


def create_index(embeddings: list, index_path: str):
    """Create FAISS index from embeddings"""
    try:
        import faiss
        import numpy as np
        
        embeddings_np = np.array(embeddings).astype('float32')
        dim = embeddings_np.shape[1]
        
        # Create index (using Flat L2 for simplicity, can be optimized)
        index = faiss.IndexFlatL2(dim)
        index.add(embeddings_np)
        
        # Save index
        faiss.write_index(index, index_path)
        
        return {
            'success': True,
            'dimension': dim,
            'count': index.ntotal,
            'index_path': index_path
        }
    except Exception as e:
        return {'success': False, 'error': str(e)}


def add_to_index(embeddings: list, index_path: str):
    """Add embeddings to existing FAISS index"""
    try:
        import faiss
        import numpy as np
        
        embeddings_np = np.array(embeddings).astype('float32')
        
        # Load or create index
        if os.path.exists(index_path):
            index = faiss.read_index(index_path)
        else:
            dim = embeddings_np.shape[1]
            index = faiss.IndexFlatL2(dim)
        
        # Add embeddings
        index.add(embeddings_np)
        
        # Save index
        faiss.write_index(index, index_path)
        
        return {
            'success': True,
            'count': index.ntotal,
            'added': len(embeddings)
        }
    except Exception as e:
        return {'success': False, 'error': str(e)}


def search(query_embedding: list, index_path: str, top_k: int = 5):
    """Search FAISS index for similar embeddings"""
    try:
        import faiss
        import numpy as np
        
        if not os.path.exists(index_path):
            return {'success': False, 'error': 'Index not found'}
        
        index = faiss.read_index(index_path)
        query_np = np.array([query_embedding]).astype('float32')
        
        distances, indices = index.search(query_np, top_k)
        
        return {
            'success': True,
            'indices': indices[0].tolist(),
            'distances': distances[0].tolist()
        }
    except Exception as e:
        return {'success': False, 'error': str(e)}


def main():
    """Main entry point - reads JSON from stdin"""
    try:
        # Read input
        input_data = json.loads(sys.stdin.read())
        action = input_data.get('action', 'check')
        
        # Dispatch to action handlers
        if action == 'check':
            result = check_availability()
        elif action == 'initialize':
            # data_path MUST be provided by caller - no fallback to user home
            data_path = input_data.get('data_path')
            if not data_path:
                raise ValueError("data_path must be provided for initialize action")
            result = initialize(
                data_path,
                input_data.get('embedding_model', 'all-MiniLM-L6-v2')
            )
        elif action == 'create_embeddings':
            result = create_embeddings(
                input_data.get('texts', []),
                input_data.get('embedding_model', 'all-MiniLM-L6-v2')
            )
        elif action == 'create_index':
            result = create_index(
                input_data.get('embeddings', []),
                input_data.get('index_path')
            )
        elif action == 'add_to_index':
            result = add_to_index(
                input_data.get('embeddings', []),
                input_data.get('index_path')
            )
        elif action == 'search':
            result = search(
                input_data.get('query_embedding', []),
                input_data.get('index_path'),
                input_data.get('top_k', 5)
            )
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
