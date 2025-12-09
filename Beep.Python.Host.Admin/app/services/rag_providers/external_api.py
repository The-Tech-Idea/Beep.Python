"""
External API RAG Provider

Delegates all RAG operations to your external API server.
Your API handles document storage, embeddings, retrieval, and authorization.
"""
import httpx
from typing import Optional, List, Dict, Any
from .base import (
    RAGProvider, RAGProviderType, RAGConfig, RAGContext, RAGQuery,
    Collection, Document
)


class ExternalAPIProvider(RAGProvider):
    """
    RAG Provider that delegates to an external API
    
    Your external API should implement these endpoints:
    
    POST /api/rag/authorize     - Check user authorization
    POST /api/rag/context       - Retrieve context for query
    GET  /api/rag/collections   - List collections
    POST /api/rag/collections   - Create collection
    DELETE /api/rag/collections/{id} - Delete collection
    POST /api/rag/documents     - Add documents
    DELETE /api/rag/documents   - Delete documents
    GET  /api/rag/search        - Search documents
    GET  /api/rag/status        - Health check
    """
    
    def __init__(self):
        self._config: Optional[RAGConfig] = None
        self._client: Optional[httpx.Client] = None
        self._base_url: str = ""
        self._api_key: Optional[str] = None
        self._auth_header: str = "Authorization"
        self._auth_prefix: str = "Bearer"
        self._timeout: float = 30.0
        self._initialized: bool = False
        
        # Custom endpoints
        self._endpoints = {
            'status': '/api/rag/status',
            'authorize': '/api/rag/authorize',
            'context': '/api/rag/context',
            'collections': '/api/rag/collections',
            'documents': '/api/rag/documents',
            'search': '/api/rag/search'
        }
    
    @property
    def provider_type(self) -> RAGProviderType:
        return RAGProviderType.EXTERNAL_API
    
    @property
    def is_available(self) -> bool:
        """Always available - uses httpx which is a dependency"""
        return True
    
    def get_install_instructions(self) -> str:
        """External API provider requires no additional installation"""
        return "No additional packages required. Configure your external API endpoint."
    
    def initialize(self, config: RAGConfig) -> bool:
        """Initialize with configuration"""
        self._config = config
        
        settings = config.provider_settings
        
        # Check if external_api config is passed
        external_api = settings.get('external_api')
        if external_api:
            # Extract from ExternalAPIConfig object or dict
            if hasattr(external_api, 'base_url'):
                self._base_url = external_api.base_url or ''
                self._api_key = external_api.api_key
                self._auth_header = external_api.auth_header or 'Authorization'
                self._auth_prefix = external_api.auth_prefix or 'Bearer'
                self._timeout = external_api.timeout or 30.0
                
                # Custom endpoints
                if hasattr(external_api, 'context_endpoint'):
                    self._endpoints['context'] = external_api.context_endpoint
                if hasattr(external_api, 'auth_endpoint'):
                    self._endpoints['authorize'] = external_api.auth_endpoint
                if hasattr(external_api, 'collections_endpoint'):
                    self._endpoints['collections'] = external_api.collections_endpoint
                if hasattr(external_api, 'status_endpoint'):
                    self._endpoints['status'] = external_api.status_endpoint
            elif isinstance(external_api, dict):
                self._base_url = external_api.get('base_url', '')
                self._api_key = external_api.get('api_key')
                self._auth_header = external_api.get('auth_header', 'Authorization')
                self._auth_prefix = external_api.get('auth_prefix', 'Bearer')
                self._timeout = external_api.get('timeout', 30.0)
        else:
            # Legacy: direct settings
            self._base_url = settings.get('base_url', '')
            self._api_key = settings.get('api_key')
            self._auth_header = settings.get('auth_header', 'Authorization')
            self._auth_prefix = settings.get('auth_prefix', 'Bearer')
            self._timeout = settings.get('timeout', 30.0)
        
        # Close existing client
        if self._client and not self._client.is_closed:
            self._client.close()
        
        self._client = None
        self._initialized = bool(self._base_url)
        return self._initialized
    
    def _get_client(self) -> httpx.Client:
        """Get or create HTTP client"""
        if self._client is None or self._client.is_closed:
            headers = {'Content-Type': 'application/json'}
            if self._api_key:
                headers[self._auth_header] = f"{self._auth_prefix} {self._api_key}"
            
            self._client = httpx.Client(
                base_url=self._base_url,
                headers=headers,
                timeout=self._timeout
            )
        return self._client
    
    def get_status(self) -> Dict[str, Any]:
        """Check external API status"""
        if not self._base_url:
            return {'status': 'not_configured', 'message': 'Base URL not set'}
        
        try:
            client = self._get_client()
            response = client.get(self._endpoints['status'], timeout=5.0)
            
            if response.status_code == 200:
                data = response.json()
                data['reachable'] = True
                data['status'] = 'connected'
                return data
            else:
                return {
                    'status': 'error',
                    'reachable': True,
                    'http_status': response.status_code
                }
        except httpx.ConnectError:
            return {'status': 'unreachable', 'reachable': False}
        except httpx.TimeoutException:
            return {'status': 'timeout', 'reachable': False}
        except Exception as e:
            return {'status': 'error', 'message': str(e)}
    
    def check_authorization(self,
                           user_id: str,
                           action: str,
                           resource_type: str = "collection",
                           resource_id: Optional[str] = None) -> Dict[str, Any]:
        """Check authorization with external API"""
        try:
            client = self._get_client()
            response = client.post(
                self._endpoints['authorize'],
                json={
                    'user_id': user_id,
                    'action': action,
                    'resource_type': resource_type,
                    'resource_id': resource_id
                }
            )
            
            if response.status_code == 200:
                return response.json()
            return {'allowed': False, 'reason': f'HTTP {response.status_code}'}
            
        except Exception as e:
            # Fail open if configured
            if self._config and self._config.provider_settings.get('fallback_on_error', True):
                return {'allowed': True, 'reason': 'Fallback - auth unavailable'}
            return {'allowed': False, 'reason': str(e)}
    
    def retrieve_context(self, query: RAGQuery) -> List[RAGContext]:
        """Retrieve context from external API"""
        try:
            client = self._get_client()
            response = client.post(
                self._endpoints['context'],
                json=query.to_dict()
            )
            
            if response.status_code == 200:
                data = response.json()
                return [
                    RAGContext(
                        id=ctx.get('id', ''),
                        content=ctx.get('content', ''),
                        source=ctx.get('source', 'unknown'),
                        relevance_score=ctx.get('relevance_score', 0.0),
                        metadata=ctx.get('metadata', {})
                    )
                    for ctx in data.get('contexts', [])
                ]
            return []
            
        except Exception as e:
            print(f"External API context error: {e}")
            return []
    
    def list_collections(self, user_id: Optional[str] = None) -> List[Collection]:
        """List collections from external API"""
        try:
            client = self._get_client()
            params = {'user_id': user_id} if user_id else {}
            response = client.get(self._endpoints['collections'], params=params)
            
            if response.status_code == 200:
                return [
                    Collection(
                        id=c.get('id', ''),
                        name=c.get('name', ''),
                        description=c.get('description', ''),
                        doc_count=c.get('doc_count', 0),
                        metadata=c.get('metadata', {})
                    )
                    for c in response.json().get('collections', [])
                ]
            return []
            
        except Exception as e:
            print(f"External API collections error: {e}")
            return []
    
    def create_collection(self,
                         name: str,
                         description: str = "",
                         metadata: Optional[Dict[str, Any]] = None) -> Collection:
        """Create collection via external API"""
        try:
            client = self._get_client()
            response = client.post(
                self._endpoints['collections'],
                json={
                    'name': name,
                    'description': description,
                    'metadata': metadata or {}
                }
            )
            
            if response.status_code in (200, 201):
                data = response.json()
                return Collection(
                    id=data.get('id', ''),
                    name=data.get('name', name),
                    description=data.get('description', description),
                    doc_count=0,
                    metadata=data.get('metadata', {})
                )
            raise Exception(f"Create failed: HTTP {response.status_code}")
            
        except Exception as e:
            raise Exception(f"External API error: {e}")
    
    def delete_collection(self, collection_id: str) -> bool:
        """Delete collection via external API"""
        try:
            client = self._get_client()
            response = client.delete(f"{self._endpoints['collections']}/{collection_id}")
            return response.status_code in (200, 204)
        except:
            return False
    
    def add_documents(self,
                     documents: List[Document],
                     collection_id: str) -> Dict[str, Any]:
        """Add documents via external API"""
        try:
            client = self._get_client()
            response = client.post(
                self._endpoints['documents'],
                json={
                    'collection_id': collection_id,
                    'documents': [d.to_dict() for d in documents]
                }
            )
            
            if response.status_code in (200, 201):
                return response.json()
            return {'success': False, 'error': f'HTTP {response.status_code}'}
            
        except Exception as e:
            return {'success': False, 'error': str(e)}
    
    def delete_documents(self,
                        document_ids: List[str],
                        collection_id: str) -> Dict[str, Any]:
        """Delete documents via external API"""
        try:
            client = self._get_client()
            response = client.request(
                'DELETE',
                self._endpoints['documents'],
                json={
                    'collection_id': collection_id,
                    'document_ids': document_ids
                }
            )
            
            if response.status_code in (200, 204):
                return {'success': True, 'deleted': len(document_ids)}
            return {'success': False, 'error': f'HTTP {response.status_code}'}
            
        except Exception as e:
            return {'success': False, 'error': str(e)}
    
    def search_documents(self,
                        query: str,
                        collection_id: str,
                        limit: int = 10) -> List[Document]:
        """Search documents via external API"""
        try:
            client = self._get_client()
            response = client.get(
                self._endpoints['search'],
                params={
                    'query': query,
                    'collection_id': collection_id,
                    'limit': limit
                }
            )
            
            if response.status_code == 200:
                return [
                    Document(
                        id=d.get('id', ''),
                        content=d.get('content', ''),
                        source=d.get('source', ''),
                        metadata=d.get('metadata', {}),
                        collection_id=collection_id
                    )
                    for d in response.json().get('documents', [])
                ]
            return []
            
        except Exception as e:
            print(f"External API search error: {e}")
            return []
