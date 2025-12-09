"""
RAG (Retrieval-Augmented Generation) Service

Unified RAG service with multiple provider options:

1. External API Provider
   - Delegates to YOUR external API for document storage and retrieval
   - Supports authorization and privilege checking
   - Best for production with centralized RAG service

2. FAISS Provider  
   - Local vector database using Facebook AI Similarity Search
   - Fast, memory-efficient, offline capable
   - Best for standalone/offline use

3. ChromaDB Provider
   - Local vector database with easy API
   - Feature-rich, persistent storage
   - Best for development and moderate-scale applications
"""
import os
import json
import hashlib
import threading
from pathlib import Path
from datetime import datetime
from typing import Optional, List, Dict, Any
from dataclasses import dataclass, field, asdict
from enum import Enum


class RAGProviderType(Enum):
    """Available RAG provider types"""
    EXTERNAL_API = "external_api"
    FAISS = "faiss"
    CHROMADB = "chromadb"


@dataclass
class RAGContext:
    """Context retrieved from any provider"""
    id: str
    content: str
    source: str
    relevance_score: float = 0.0
    metadata: Dict[str, Any] = field(default_factory=dict)
    
    def to_dict(self) -> dict:
        return asdict(self)


@dataclass 
class RAGQuery:
    """A RAG query for any provider"""
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
class ExternalAPIConfig:
    """Configuration for external RAG API"""
    base_url: str
    api_key: Optional[str] = None
    auth_header: str = "Authorization"
    auth_prefix: str = "Bearer"
    timeout: float = 30.0
    
    # Endpoint paths (customizable)
    context_endpoint: str = "/api/rag/context"
    auth_endpoint: str = "/api/rag/authorize"
    collections_endpoint: str = "/api/rag/collections"
    upload_endpoint: str = "/api/rag/upload"
    status_endpoint: str = "/api/rag/status"
    
    # Context Processing API endpoint (post-RAG, pre-LLM processing)
    context_processing_endpoint: str = "/api/rag/process-context"
    
    def to_dict(self) -> dict:
        d = asdict(self)
        # Mask API key
        if d.get('api_key'):
            d['api_key'] = '***' + d['api_key'][-4:] if len(d['api_key']) > 4 else '***'
        return d


@dataclass
class RAGSettings:
    """
    RAG service settings with pipeline support:
    
    Pipeline Flow:
    1. User Query arrives
    2. Auth API (optional) → Validates user has access
    3. RAG Provider → Retrieves relevant documents from local/external storage
    4. Context API (optional) → Processes/enriches RAG data before LLM
    5. LLM → Generates response with processed context
    6. Response → Sent to user
    """
    enabled: bool = False
    provider_type: RAGProviderType = RAGProviderType.FAISS  # Default to local FAISS
    
    # External API config (used for auth and/or context processing)
    external_api: Optional[ExternalAPIConfig] = None
    
    # Pipeline Step 1: Authorization API (optional)
    # When enabled, calls external auth endpoint before allowing RAG access
    use_auth_api: bool = False
    
    # Pipeline Step 2: Context Processing API (optional)
    # When enabled, sends RAG results to external API for processing before LLM
    # This allows external service to: filter, rerank, summarize, add metadata, etc.
    use_context_api: bool = False
    
    # Local provider config
    data_path: Optional[str] = None  # Path for local data storage
    embedding_model: str = "all-MiniLM-L6-v2"  # Sentence transformer model
    
    # Context injection settings
    max_context_length: int = 4000  # Max chars to inject
    context_template: str = """## Relevant Context

The following information may be helpful for answering the user's question:

{context}

---
"""
    
    # Fallback settings
    fallback_on_api_error: bool = True
    cache_contexts: bool = True
    cache_ttl_seconds: int = 300
    
    def to_dict(self) -> dict:
        return {
            'enabled': self.enabled,
            'provider_type': self.provider_type.value,
            'external_api': self.external_api.to_dict() if self.external_api else None,
            'use_auth_api': self.use_auth_api,
            'use_context_api': self.use_context_api,
            'data_path': self.data_path,
            'embedding_model': self.embedding_model,
            'max_context_length': self.max_context_length,
            'context_template': self.context_template,
            'fallback_on_api_error': self.fallback_on_api_error,
            'cache_contexts': self.cache_contexts,
            'cache_ttl_seconds': self.cache_ttl_seconds
        }


class RAGService:
    """
    Unified RAG Service with Multiple Provider Support
    
    Supports three provider types:
    
    1. EXTERNAL_API - Delegates to your external RAG API
       - Best for: Production, centralized document management
       - Features: Authorization, privilege checking, document management
       - Requires: External API implementation
    
    2. FAISS - Local Facebook AI Similarity Search
       - Best for: Offline use, fast similarity search
       - Features: Memory-efficient, GPU support, persistent storage
       - Requires: faiss-cpu or faiss-gpu, sentence-transformers
    
    3. CHROMADB - Local ChromaDB database  
       - Best for: Development, moderate scale applications
       - Features: Easy API, metadata filtering, persistent storage
       - Requires: chromadb, sentence-transformers
    
    Usage:
        rag = RAGService()
        rag.set_provider(RAGProviderType.FAISS)
        rag.configure(enabled=True)
        
        # Add documents
        rag.add_documents([...], collection_id="my_docs")
        
        # Retrieve context
        contexts = rag.retrieve_context("What is X?")
    """
    
    _instance = None
    _lock = threading.Lock()
    
    def __new__(cls):
        if cls._instance is None:
            with cls._lock:
                if cls._instance is None:
                    cls._instance = super().__new__(cls)
                    cls._instance._initialized = False
        return cls._instance
    
    def __init__(self):
        if self._initialized:
            return
        
        self._initialized = True
        self._config_path = Path(__file__).parent.parent.parent / "config" / "rag_config.json"
        self._settings = self._load_settings()
        self._context_cache: Dict[str, tuple] = {}  # query_hash -> (contexts, timestamp)
        
        # Provider instances (lazy loaded)
        self._providers: Dict[RAGProviderType, Any] = {}
        self._current_provider = None
        
        # Initialize provider based on settings
        self._init_provider()
    
    def _load_settings(self) -> RAGSettings:
        """Load RAG settings from config file"""
        if self._config_path.exists():
            try:
                with open(self._config_path, 'r') as f:
                    data = json.load(f)
                
                external_api = None
                if data.get('external_api'):
                    external_api = ExternalAPIConfig(**data['external_api'])
                
                provider_type = RAGProviderType.FAISS  # Default
                if data.get('provider_type'):
                    try:
                        provider_type = RAGProviderType(data['provider_type'])
                    except:
                        pass
                
                return RAGSettings(
                    enabled=data.get('enabled', False),
                    provider_type=provider_type,
                    external_api=external_api,
                    data_path=data.get('data_path'),
                    embedding_model=data.get('embedding_model', 'all-MiniLM-L6-v2'),
                    max_context_length=data.get('max_context_length', 4000),
                    context_template=data.get('context_template', RAGSettings.context_template),
                    fallback_on_api_error=data.get('fallback_on_api_error', True),
                    cache_contexts=data.get('cache_contexts', True),
                    cache_ttl_seconds=data.get('cache_ttl_seconds', 300),
                    use_auth_api=data.get('use_auth_api', False),
                    use_context_api=data.get('use_context_api', False)
                )
            except Exception as e:
                print(f"Error loading RAG config: {e}")
        
        return RAGSettings()
    
    def _save_settings(self):
        """Save RAG settings to config file"""
        self._config_path.parent.mkdir(parents=True, exist_ok=True)
        
        data = {
            'enabled': self._settings.enabled,
            'provider_type': self._settings.provider_type.value,
            'max_context_length': self._settings.max_context_length,
            'context_template': self._settings.context_template,
            'fallback_on_api_error': self._settings.fallback_on_api_error,
            'cache_contexts': self._settings.cache_contexts,
            'cache_ttl_seconds': self._settings.cache_ttl_seconds,
            'data_path': self._settings.data_path,
            'embedding_model': self._settings.embedding_model,
            'use_auth_api': self._settings.use_auth_api,
            'use_context_api': self._settings.use_context_api
        }
        
        if self._settings.external_api:
            data['external_api'] = {
                'base_url': self._settings.external_api.base_url,
                'api_key': self._settings.external_api.api_key,
                'auth_header': self._settings.external_api.auth_header,
                'auth_prefix': self._settings.external_api.auth_prefix,
                'timeout': self._settings.external_api.timeout,
                'context_endpoint': self._settings.external_api.context_endpoint,
                'auth_endpoint': self._settings.external_api.auth_endpoint,
                'collections_endpoint': self._settings.external_api.collections_endpoint,
                'upload_endpoint': self._settings.external_api.upload_endpoint,
                'status_endpoint': self._settings.external_api.status_endpoint
            }
        
        with open(self._config_path, 'w') as f:
            json.dump(data, f, indent=2)
    
    def _init_provider(self):
        """Initialize the current provider"""
        from .rag_providers import ExternalAPIProvider, SubprocessFAISSProvider, SubprocessChromaDBProvider
        from .rag_providers.base import RAGConfig
        
        provider_type = self._settings.provider_type
        
        # Create provider if not cached (use subprocess providers for local backends)
        if provider_type not in self._providers:
            if provider_type == RAGProviderType.EXTERNAL_API:
                self._providers[provider_type] = ExternalAPIProvider()
            elif provider_type == RAGProviderType.FAISS:
                # Use subprocess provider to run in RAG venv
                self._providers[provider_type] = SubprocessFAISSProvider()
            elif provider_type == RAGProviderType.CHROMADB:
                # Use subprocess provider to run in RAG venv
                self._providers[provider_type] = SubprocessChromaDBProvider()
        
        self._current_provider = self._providers[provider_type]
        
        # Initialize provider with config
        if self._current_provider and self._settings.enabled:
            config = RAGConfig(
                provider_type=provider_type,
                data_path=self._settings.data_path,
                provider_settings={
                    'embedding_model': self._settings.embedding_model,
                    'external_api': self._settings.external_api
                }
            )
            self._current_provider.initialize(config)
    
    def _get_cache_key(self, query: RAGQuery) -> str:
        """Generate cache key for a query"""
        data = json.dumps(query.to_dict(), sort_keys=True)
        return hashlib.sha256(data.encode()).hexdigest()[:16]
    
    # =====================
    # Provider Management
    # =====================
    
    def get_available_providers(self) -> Dict[str, Dict[str, Any]]:
        """Get all available provider types and their status"""
        from .rag_providers import ExternalAPIProvider, SubprocessFAISSProvider, SubprocessChromaDBProvider
        
        providers = {
            RAGProviderType.EXTERNAL_API: ExternalAPIProvider(),
            RAGProviderType.FAISS: SubprocessFAISSProvider(),
            RAGProviderType.CHROMADB: SubprocessChromaDBProvider()
        }
        
        result = {}
        for ptype, provider in providers.items():
            result[ptype.value] = {
                'available': provider.is_available,
                'install_instructions': provider.get_install_instructions() if not provider.is_available else None,
                'current': ptype == self._settings.provider_type
            }
        
        return result
    
    def set_provider(self, provider_type: RAGProviderType) -> Dict[str, Any]:
        """
        Switch to a different RAG provider
        
        Args:
            provider_type: The provider type to switch to
            
        Returns:
            Status dict with success/error info
        """
        self._settings.provider_type = provider_type
        self._init_provider()
        self._save_settings()
        
        if self._current_provider and not self._current_provider.is_available:
            return {
                'success': False,
                'error': 'Provider not available',
                'install': self._current_provider.get_install_instructions()
            }
        
        return {
            'success': True,
            'provider': provider_type.value,
            'status': self._current_provider.get_status() if self._current_provider else {}
        }
    
    def get_provider_status(self) -> Dict[str, Any]:
        """Get current provider status"""
        if not self._current_provider:
            return {'status': 'no_provider', 'message': 'No provider initialized'}
        
        return self._current_provider.get_status()
    
    # =====================
    # Public API
    # =====================
    
    def is_enabled(self) -> bool:
        """Check if RAG is enabled and provider is available"""
        return (
            self._settings.enabled and 
            self._current_provider is not None and
            self._current_provider.is_available
        )
    
    def get_settings(self) -> dict:
        """Get current RAG settings"""
        return self._settings.to_dict()
    
    def configure(self, 
                  enabled: Optional[bool] = None,
                  provider_type: Optional[str] = None,
                  base_url: Optional[str] = None,
                  api_key: Optional[str] = None,
                  data_path: Optional[str] = None,
                  embedding_model: Optional[str] = None,
                  **kwargs) -> dict:
        """
        Configure RAG service
        
        Args:
            enabled: Enable/disable RAG
            provider_type: Provider type (external_api, faiss, chromadb)
            base_url: External API base URL (for external_api provider)
            api_key: API key for authentication (for external_api provider)
            data_path: Local data storage path (for faiss/chromadb)
            embedding_model: Sentence transformer model name
            **kwargs: Additional settings
        """
        if enabled is not None:
            self._settings.enabled = enabled
        
        if provider_type:
            try:
                self._settings.provider_type = RAGProviderType(provider_type)
            except:
                pass
        
        if data_path:
            self._settings.data_path = data_path
        
        if embedding_model:
            self._settings.embedding_model = embedding_model
        
        if base_url:
            if not self._settings.external_api:
                self._settings.external_api = ExternalAPIConfig(base_url=base_url)
            else:
                self._settings.external_api.base_url = base_url
        
        if api_key:
            if not self._settings.external_api:
                raise ValueError("base_url must be set before api_key")
            self._settings.external_api.api_key = api_key
        
        # Additional settings
        for key, value in kwargs.items():
            if hasattr(self._settings, key):
                setattr(self._settings, key, value)
            elif self._settings.external_api and hasattr(self._settings.external_api, key):
                setattr(self._settings.external_api, key, value)
        
        # Re-initialize provider with new settings
        self._init_provider()
        
        self._save_settings()
        
        return {'success': True, 'settings': self._settings.to_dict()}
    
    # =====================
    # Authorization (Pipeline Step 1)
    # =====================
    
    def _get_external_api_provider(self):
        """Get or create External API provider for auth/context processing"""
        from .rag_providers import ExternalAPIProvider
        from .rag_providers.base import RAGConfig
        
        if RAGProviderType.EXTERNAL_API not in self._providers:
            self._providers[RAGProviderType.EXTERNAL_API] = ExternalAPIProvider()
        
        provider = self._providers[RAGProviderType.EXTERNAL_API]
        
        # Initialize if needed and external_api config exists
        if self._settings.external_api and not provider._initialized:
            config = RAGConfig(
                provider_type=RAGProviderType.EXTERNAL_API,
                data_path=self._settings.data_path,
                provider_settings={
                    'embedding_model': self._settings.embedding_model,
                    'external_api': self._settings.external_api
                }
            )
            provider.initialize(config)
        
        return provider
    
    def check_authorization(self, 
                           user_id: str,
                           action: str,
                           resource_type: str = "collection",
                           resource_id: Optional[str] = None) -> Dict[str, Any]:
        """
        Pipeline Step 1: Check authorization via Auth API
        
        When use_auth_api is True, calls external auth endpoint to validate user access.
        This happens BEFORE retrieving RAG data.
        
        Flow: User Query → [AUTH API] → RAG → Context API → LLM → Response
        
        Args:
            user_id: User identifier
            action: Action being performed (query, upload, delete, list)
            resource_type: Type of resource (collection, document)
            resource_id: Optional specific resource ID
            
        Returns:
            {allowed: bool, reason?: str, privileges?: list}
        """
        if not self.is_enabled():
            return {'allowed': False, 'reason': 'RAG not enabled'}
        
        # If Auth API is not enabled, allow all (no auth check)
        if not self._settings.use_auth_api:
            return {'allowed': True, 'reason': 'Auth API not enabled'}
        
        # Auth API is enabled - must use external API
        if not self._settings.external_api:
            return {'allowed': False, 'reason': 'Auth API enabled but no external API configured'}
        
        auth_provider = self._get_external_api_provider()
        if not auth_provider.is_available:
            if self._settings.fallback_on_api_error:
                return {'allowed': True, 'reason': 'Auth API unavailable, fallback allowed'}
            else:
                return {'allowed': False, 'reason': 'Auth API unavailable'}
        
        return auth_provider.check_authorization(
            user_id=user_id,
            action=action,
            resource_type=resource_type,
            resource_id=resource_id
        )
    
    # =====================
    # Context Retrieval (Pipeline Step 2)
    # =====================
    
    def retrieve_context(self, 
                        query: str,
                        user_id: Optional[str] = None,
                        collection_ids: Optional[List[str]] = None,
                        max_results: int = 5,
                        min_relevance: float = 0.0,
                        filters: Optional[Dict[str, Any]] = None,
                        use_cache: bool = True) -> List[RAGContext]:
        """
        Pipeline Step 2: Retrieve relevant context from RAG storage
        
        Retrieves documents from the configured provider (FAISS, ChromaDB, or External API).
        
        Flow: User Query → Auth API → [RAG RETRIEVAL] → Context API → LLM → Response
        
        Args:
            query: The query/question to find context for
            user_id: Optional user ID for authorization
            collection_ids: Optional list of collection IDs to search
            max_results: Maximum number of contexts to return
            min_relevance: Minimum relevance score (0-1)
            filters: Optional filters to pass to provider
            use_cache: Whether to use cached results
            
        Returns:
            List of RAGContext objects
        """
        if not self.is_enabled():
            return []
        
        if not self._current_provider:
            return []
        
        from .rag_providers.base import RAGQuery as ProviderQuery
        
        rag_query = ProviderQuery(
            query=query,
            user_id=user_id,
            collection_ids=collection_ids or [],
            max_results=max_results,
            min_relevance=min_relevance,
            filters=filters or {}
        )
        
        # Check cache
        if use_cache and self._settings.cache_contexts:
            internal_query = RAGQuery(
                query=query,
                user_id=user_id,
                collection_ids=collection_ids or [],
                max_results=max_results,
                min_relevance=min_relevance,
                filters=filters or {}
            )
            cache_key = self._get_cache_key(internal_query)
            if cache_key in self._context_cache:
                contexts, timestamp = self._context_cache[cache_key]
                age = (datetime.now() - timestamp).total_seconds()
                if age < self._settings.cache_ttl_seconds:
                    return contexts
        
        try:
            # Get contexts from current provider (FAISS, ChromaDB, or External API)
            provider_contexts = self._current_provider.retrieve_context(rag_query)
            
            # Convert to our RAGContext type
            contexts = [
                RAGContext(
                    id=ctx.id,
                    content=ctx.content,
                    source=ctx.source,
                    relevance_score=ctx.relevance_score,
                    metadata=ctx.metadata
                )
                for ctx in provider_contexts
            ]
            
            # Cache results
            if self._settings.cache_contexts:
                internal_query = RAGQuery(
                    query=query,
                    user_id=user_id,
                    collection_ids=collection_ids or [],
                    max_results=max_results,
                    min_relevance=min_relevance,
                    filters=filters or {}
                )
                cache_key = self._get_cache_key(internal_query)
                self._context_cache[cache_key] = (contexts, datetime.now())
            
            return contexts
            
        except Exception as e:
            print(f"Context retrieval error: {e}")
            return []
    
    # =====================
    # Context Processing (Pipeline Step 3)
    # =====================
    
    def process_context(self,
                       contexts: List[RAGContext],
                       query: str,
                       user_id: Optional[str] = None,
                       processing_options: Optional[Dict[str, Any]] = None) -> List[RAGContext]:
        """
        Pipeline Step 3: Process/enrich RAG contexts via Context API before sending to LLM
        
        When use_context_api is True, sends the retrieved RAG data to an external API
        for processing. This allows the external service to:
        - Filter irrelevant contexts
        - Rerank by relevance
        - Summarize long contexts
        - Add additional metadata
        - Inject user-specific information
        - Apply business rules
        
        Flow: User Query → Auth API → RAG → [CONTEXT API] → LLM → Response
        
        Args:
            contexts: List of RAGContext objects from RAG retrieval
            query: The original user query
            user_id: Optional user ID for personalization
            processing_options: Optional dict with processing preferences
            
        Returns:
            Processed list of RAGContext objects (may be modified, filtered, or enriched)
        """
        # If Context API is not enabled, return contexts unchanged
        if not self._settings.use_context_api:
            return contexts
        
        # Context API is enabled - must use external API
        if not self._settings.external_api:
            print("Context API enabled but no external API configured")
            return contexts  # Return unprocessed
        
        try:
            import httpx
            
            # Build request payload
            payload = {
                'query': query,
                'user_id': user_id,
                'contexts': [ctx.to_dict() for ctx in contexts],
                'options': processing_options or {}
            }
            
            # Get endpoint
            endpoint = self._settings.external_api.context_processing_endpoint
            base_url = self._settings.external_api.base_url
            url = f"{base_url.rstrip('/')}{endpoint}"
            
            # Build headers
            headers = {'Content-Type': 'application/json'}
            if self._settings.external_api.api_key:
                auth_header = self._settings.external_api.auth_header
                auth_prefix = self._settings.external_api.auth_prefix
                headers[auth_header] = f"{auth_prefix} {self._settings.external_api.api_key}"
            
            # Call Context Processing API
            with httpx.Client(timeout=self._settings.external_api.timeout) as client:
                response = client.post(url, json=payload, headers=headers)
                
                if response.status_code == 200:
                    data = response.json()
                    
                    # Parse processed contexts from response
                    processed_contexts = []
                    for ctx_data in data.get('contexts', []):
                        processed_contexts.append(RAGContext(
                            id=ctx_data.get('id', ''),
                            content=ctx_data.get('content', ''),
                            source=ctx_data.get('source', ''),
                            relevance_score=ctx_data.get('relevance_score', 0.0),
                            metadata=ctx_data.get('metadata', {})
                        ))
                    
                    return processed_contexts if processed_contexts else contexts
                else:
                    print(f"Context API error: {response.status_code}")
                    if self._settings.fallback_on_api_error:
                        return contexts
                    return []
                    
        except Exception as e:
            print(f"Context processing error: {e}")
            if self._settings.fallback_on_api_error:
                return contexts
            return []
    
    def format_context_for_prompt(self, contexts: List[RAGContext]) -> str:
        """
        Format retrieved contexts for injection into prompt
        
        Args:
            contexts: List of RAGContext objects
            
        Returns:
            Formatted context string
        """
        if not contexts:
            return ""
        
        # Build context text, respecting max length
        context_parts = []
        total_length = 0
        
        for ctx in contexts:
            # Format each context
            part = f"[Source: {ctx.source}]\n{ctx.content}\n"
            
            if total_length + len(part) > self._settings.max_context_length:
                # Truncate if needed
                remaining = self._settings.max_context_length - total_length
                if remaining > 100:
                    part = part[:remaining] + "..."
                    context_parts.append(part)
                break
            
            context_parts.append(part)
            total_length += len(part)
        
        combined_context = "\n".join(context_parts)
        return self._settings.context_template.format(context=combined_context)
    
    def augment_messages(self,
                        messages: List[Dict[str, str]],
                        user_id: Optional[str] = None,
                        collection_ids: Optional[List[str]] = None) -> List[Dict[str, str]]:
        """
        Full RAG Pipeline: Augment chat messages with processed RAG context
        
        Executes the complete pipeline:
        1. Auth API (if enabled) → Validate user access
        2. RAG Retrieval → Get relevant documents  
        3. Context API (if enabled) → Process/enrich contexts
        4. Format and inject into messages for LLM
        
        Flow: User Query → [AUTH API] → [RAG] → [CONTEXT API] → LLM → Response
        
        Args:
            messages: List of chat messages [{role, content}, ...]
            user_id: Optional user ID for authorization
            collection_ids: Optional collection IDs to search
            
        Returns:
            Augmented messages list with RAG context injected
        """
        if not self.is_enabled() or not messages:
            return messages
        
        # Find last user message
        last_user_msg_idx = None
        for i in range(len(messages) - 1, -1, -1):
            if messages[i].get('role') == 'user':
                last_user_msg_idx = i
                break
        
        if last_user_msg_idx is None:
            return messages
        
        user_query = messages[last_user_msg_idx].get('content', '')
        
        # ===== Pipeline Step 1: Auth API =====
        if self._settings.use_auth_api:
            auth_result = self.check_authorization(
                user_id=user_id or 'anonymous',
                action='query',
                resource_type='collection'
            )
            if not auth_result.get('allowed', False):
                print(f"Auth denied: {auth_result.get('reason', 'Unknown')}")
                return messages  # Return without RAG context
        
        # ===== Pipeline Step 2: RAG Retrieval =====
        contexts = self.retrieve_context(
            query=user_query,
            user_id=user_id,
            collection_ids=collection_ids
        )
        
        if not contexts:
            return messages
        
        # ===== Pipeline Step 3: Context API =====
        if self._settings.use_context_api:
            contexts = self.process_context(
                contexts=contexts,
                query=user_query,
                user_id=user_id
            )
        
        if not contexts:
            return messages
        
        # ===== Pipeline Step 4: Format for LLM =====
        context_text = self.format_context_for_prompt(contexts)
        
        # Create augmented messages
        augmented = messages.copy()
        
        # Check if there's already a system message
        has_system = any(m.get('role') == 'system' for m in augmented)
        
        if has_system:
            # Append context to existing system message
            for i, msg in enumerate(augmented):
                if msg.get('role') == 'system':
                    augmented[i] = {
                        'role': 'system',
                        'content': msg['content'] + '\n\n' + context_text
                    }
                    break
        else:
            # Insert new system message with context
            augmented.insert(0, {
                'role': 'system',
                'content': context_text
            })
        
        return augmented
    
    # =====================
    # Collection Management
    # =====================
    
    def list_collections(self, user_id: Optional[str] = None) -> List[Dict[str, Any]]:
        """
        List available collections from current provider
        
        Args:
            user_id: Optional user ID for filtering by access
            
        Returns:
            List of collection info dicts
        """
        if not self.is_enabled():
            return []
        
        if not self._current_provider:
            return []
        
        try:
            collections = self._current_provider.list_collections(user_id)
            return [
                {
                    'id': c.id,
                    'name': c.name,
                    'description': c.description,
                    'doc_count': c.doc_count,
                    'metadata': c.metadata
                }
                for c in collections
            ]
        except Exception as e:
            print(f"List collections error: {e}")
            return []
    
    def create_collection(self,
                         name: str,
                         description: str = "",
                         metadata: Optional[Dict[str, Any]] = None) -> Dict[str, Any]:
        """
        Create a new collection
        
        Args:
            name: Collection name
            description: Optional description
            metadata: Optional metadata
            
        Returns:
            Created collection info or error
        """
        if not self.is_enabled():
            return {'success': False, 'error': 'RAG not enabled'}
        
        if not self._current_provider:
            return {'success': False, 'error': 'No provider initialized'}
        
        try:
            collection = self._current_provider.create_collection(
                name=name,
                description=description,
                metadata=metadata
            )
            return {
                'success': True,
                'collection': {
                    'id': collection.id,
                    'name': collection.name,
                    'description': collection.description
                }
            }
        except Exception as e:
            return {'success': False, 'error': str(e)}
    
    def delete_collection(self, collection_id: str) -> Dict[str, Any]:
        """Delete a collection"""
        if not self.is_enabled():
            return {'success': False, 'error': 'RAG not enabled'}
        
        if not self._current_provider:
            return {'success': False, 'error': 'No provider initialized'}
        
        try:
            success = self._current_provider.delete_collection(collection_id)
            return {'success': success}
        except Exception as e:
            return {'success': False, 'error': str(e)}
    
    # =====================
    # Document Management
    # =====================
    
    def add_documents(self,
                     documents: List[Dict[str, Any]],
                     collection_id: str) -> Dict[str, Any]:
        """
        Add documents to a collection
        
        Args:
            documents: List of documents with {content, source?, metadata?}
            collection_id: Target collection ID
            
        Returns:
            Result with added count
        """
        if not self.is_enabled():
            return {'success': False, 'error': 'RAG not enabled', 'added': 0}
        
        if not self._current_provider:
            return {'success': False, 'error': 'No provider initialized', 'added': 0}
        
        from .rag_providers.base import Document
        
        try:
            doc_objects = [
                Document(
                    id=doc.get('id'),
                    content=doc.get('content', ''),
                    source=doc.get('source', 'unknown'),
                    metadata=doc.get('metadata', {}),
                    collection_id=collection_id
                )
                for doc in documents
            ]
            
            return self._current_provider.add_documents(doc_objects, collection_id)
        except Exception as e:
            return {'success': False, 'error': str(e), 'added': 0}
    
    def delete_documents(self,
                        document_ids: List[str],
                        collection_id: str) -> Dict[str, Any]:
        """Delete documents from a collection"""
        if not self.is_enabled():
            return {'success': False, 'error': 'RAG not enabled', 'deleted': 0}
        
        if not self._current_provider:
            return {'success': False, 'error': 'No provider initialized', 'deleted': 0}
        
        try:
            return self._current_provider.delete_documents(document_ids, collection_id)
        except Exception as e:
            return {'success': False, 'error': str(e), 'deleted': 0}
    
    def get_documents(self, collection_id: str) -> List[Dict[str, Any]]:
        """
        Get all documents in a collection
        
        Args:
            collection_id: Collection ID
            
        Returns:
            List of document dicts
        """
        if not self.is_enabled():
            return []
        
        if not self._current_provider:
            return []
        
        try:
            # Check if provider has get_documents method
            if hasattr(self._current_provider, 'get_documents'):
                return self._current_provider.get_documents(collection_id)
            return []
        except Exception as e:
            print(f"Error getting documents: {e}")
            return []
    
    def search_documents(self,
                        query: str,
                        collection_id: str,
                        limit: int = 10) -> List[Dict[str, Any]]:
        """Search documents in a collection"""
        if not self.is_enabled():
            return []
        
        if not self._current_provider:
            return []
        
        try:
            docs = self._current_provider.search_documents(query, collection_id, limit)
            return [
                {
                    'id': d.id,
                    'content': d.content,
                    'source': d.source,
                    'metadata': d.metadata
                }
                for d in docs
            ]
        except Exception as e:
            print(f"Search documents error: {e}")
            return []
    
    # =====================
    # Status and Cache
    # =====================
    
    def get_api_status(self) -> Dict[str, Any]:
        """
        Get provider status
        
        Returns:
            Provider status info
        """
        return self.get_provider_status()
    
    def clear_cache(self):
        """Clear the context cache"""
        self._context_cache.clear()
    
    def get_cache_stats(self) -> Dict[str, Any]:
        """Get cache statistics"""
        return {
            'entries': len(self._context_cache),
            'enabled': self._settings.cache_contexts,
            'ttl_seconds': self._settings.cache_ttl_seconds
        }
