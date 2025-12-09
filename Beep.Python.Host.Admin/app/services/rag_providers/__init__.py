"""
RAG Providers - Multiple backend options for RAG

Available Providers:
- ExternalAPIProvider: Delegate to external API (your server)
- FAISSProvider: Local FAISS vector database (in-process, requires faiss in Flask env)
- ChromaDBProvider: Local ChromaDB database (in-process, requires chromadb in Flask env)
- SubprocessFAISSProvider: FAISS via subprocess (uses RAG venv)
- SubprocessChromaDBProvider: ChromaDB via subprocess (uses RAG venv)
"""

from .base import RAGProvider, RAGContext, RAGQuery, RAGConfig
from .external_api import ExternalAPIProvider
from .faiss_provider import FAISSProvider
from .chromadb_provider import ChromaDBProvider
from .faiss_subprocess import SubprocessFAISSProvider
from .chromadb_subprocess import SubprocessChromaDBProvider

__all__ = [
    'RAGProvider',
    'RAGContext', 
    'RAGQuery',
    'RAGConfig',
    'ExternalAPIProvider',
    'FAISSProvider',
    'ChromaDBProvider',
    'SubprocessFAISSProvider',
    'SubprocessChromaDBProvider'
]
