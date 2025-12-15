"""
RAG Management Routes

Provides API endpoints for managing RAG (Retrieval-Augmented Generation):
- Select provider (External API, FAISS, ChromaDB)
- Configure provider settings
- Manage collections and documents
- Query context (for testing)
- Manage RAG virtual environment
- Metadata and access control management

Supports three providers:
1. External API - Delegates to your external RAG API
2. FAISS - Local Facebook AI Similarity Search
3. ChromaDB - Local ChromaDB database

Now with SQLite metadata for:
- User/Group management
- Access control (privileges)
- Audit logging
"""
import json
from flask import Blueprint, request, jsonify, render_template

from app.services.rag_service import RAGService, RAGProviderType
from app.services.rag_environment import get_rag_environment, RAGEnvStatus
from app.models.rag_metadata import get_rag_metadata_db, asdict
from app.services.document_extractor import get_document_extractor
from app.database import db


# Create blueprint
rag_bp = Blueprint('rag', __name__, url_prefix='/rag')


# =====================
# Database Migration
# =====================

@rag_bp.route('/api/db/migrate', methods=['POST'])
def api_db_migrate():
    """Create missing database tables for RAG features"""
    try:
        # Import all models to ensure they're registered
        from app.models import (
            Collection, Document, DataSource, SyncJob, SyncJobRun,
            AccessPrivilege, Role, Group, Setting, AuditLog
        )
        
        # Create all tables that don't exist
        db.create_all()
        
        return jsonify({
            'success': True,
            'message': 'Database tables created successfully'
        })
    except Exception as e:
        return jsonify({
            'success': False,
            'error': str(e)
        }), 500


@rag_bp.route('/api/db/check', methods=['GET'])
def api_db_check():
    """Check if all required tables exist"""
    from sqlalchemy import inspect
    
    try:
        inspector = inspect(db.engine)
        existing_tables = inspector.get_table_names()
        
        required_tables = [
            'rag_collections', 'rag_documents', 'rag_data_sources', 
            'rag_sync_jobs', 'rag_sync_job_runs', 'rag_access_privileges'
        ]
        
        missing = [t for t in required_tables if t not in existing_tables]
        
        return jsonify({
            'success': True,
            'existing_tables': existing_tables,
            'required_tables': required_tables,
            'missing_tables': missing,
            'all_tables_exist': len(missing) == 0
        })
    except Exception as e:
        return jsonify({
            'success': False,
            'error': str(e)
        }), 500


def get_rag_service() -> RAGService:
    return RAGService()


def get_rag_env():
    return get_rag_environment()


def get_metadata_db():
    return get_rag_metadata_db()


# =====================
# Web UI Routes
# =====================

@rag_bp.route('/')
def rag_dashboard():
    """RAG management dashboard"""
    from app.services.rag_environment_manager import get_rag_environment_manager
    
    env_mgr = get_rag_environment_manager()
    
    return render_template('rag/dashboard.html',
                          env_status=env_mgr.get_venv_status())


@rag_bp.route('/wizard')
def rag_wizard():
    """RAG Environment Setup Wizard"""
    from app.services.rag_environment_manager import get_rag_environment_manager
    
    env_mgr = get_rag_environment_manager()
    
    return render_template('rag/wizard.html',
                          env_status=env_mgr.get_venv_status())


@rag_bp.route('/configure')
def rag_configure():
    """RAG configuration page"""
    rag_service = get_rag_service()
    settings = rag_service.get_settings()
    providers = rag_service.get_available_providers()
    
    return render_template('rag/configure.html', 
                          settings=settings,
                          providers=providers)


@rag_bp.route('/collections')
def rag_collections():
    """View and manage collections"""
    rag_service = get_rag_service()
    
    if not rag_service.is_enabled():
        return render_template('rag/collections.html', 
                             collections=[],
                             error="RAG not enabled - configure a provider first")
    
    collections = rag_service.list_collections()
    provider = rag_service.get_settings().get('provider_type', 'unknown')
    
    return render_template('rag/collections.html', 
                          collections=collections,
                          provider=provider)


@rag_bp.route('/test')
def rag_test():
    """Test RAG query page"""
    rag_service = get_rag_service()
    settings = rag_service.get_settings()
    collections = []
    
    if rag_service.is_enabled():
        collections = rag_service.list_collections()
    
    return render_template('rag/test.html', 
                          settings=settings,
                          collections=collections)


@rag_bp.route('/local')
def rag_local():
    """Local RAG Management page"""
    rag_service = get_rag_service()
    rag_env = get_rag_env()
    
    settings = rag_service.get_settings()
    env_status = rag_env.get_status_info()
    
    collections = []
    total_docs = 0
    
    if rag_service.is_enabled():
        collections = rag_service.list_collections()
        total_docs = sum(c.get('document_count', 0) for c in collections)
    
    return render_template('rag/local.html',
                          provider=settings.get('provider_type', 'unknown'),
                          data_path=settings.get('data_path'),
                          embedding_model=settings.get('embedding_model', 'all-MiniLM-L6-v2'),
                          collections=collections,
                          total_docs=total_docs,
                          env_status=env_status,
                          rag_enabled=rag_service.is_enabled())


@rag_bp.route('/pipeline')
def rag_pipeline():
    """External Context API Pipeline Management page"""
    rag_service = get_rag_service()
    settings = rag_service.get_settings()
    
    return render_template('rag/pipeline.html', settings=settings)


@rag_bp.route('/environment')
def rag_environment():
    """RAG Environment Management page (separate from local RAG)"""
    rag_env = get_rag_env()
    env_status = rag_env.get_status_info()
    
    return render_template('rag/environment.html', env_status=env_status)


@rag_bp.route('/manage-collections')
def rag_manage_collections():
    """Collections & Documents Management page"""
    rag_service = get_rag_service()
    metadata_db = get_metadata_db()
    
    stats = metadata_db.get_stats()
    settings = rag_service.get_settings()
    
    # Don't call is_enabled() here as it can be slow - let JS check via API
    return render_template('rag/manage_collections.html',
                          provider=settings.get('provider_type', 'unknown'),
                          stats=stats,
                          rag_enabled=True)  # Assume enabled, JS will check


# =====================
# API Routes
# =====================

@rag_bp.route('/api/status', methods=['GET'])
def api_status():
    """Get RAG service status"""
    rag_service = get_rag_service()
    
    settings = rag_service.get_settings()
    providers = rag_service.get_available_providers()
    provider_status = rag_service.get_provider_status()
    cache_stats = rag_service.get_cache_stats()
    
    return jsonify({
        'enabled': rag_service.is_enabled(),
        'settings': settings,
        'providers': providers,
        'provider_status': provider_status,
        'cache': cache_stats
    })


@rag_bp.route('/api/providers', methods=['GET'])
def api_list_providers():
    """List available RAG providers"""
    rag_service = get_rag_service()
    providers = rag_service.get_available_providers()
    
    return jsonify({'providers': providers})


@rag_bp.route('/api/providers/switch', methods=['POST'])
def api_switch_provider():
    """Switch to a different RAG provider"""
    rag_service = get_rag_service()
    
    data = request.get_json()
    if not data or not data.get('provider'):
        return jsonify({'error': 'provider is required'}), 400
    
    try:
        provider_type = RAGProviderType(data['provider'])
        result = rag_service.set_provider(provider_type)
        return jsonify(result)
    except ValueError:
        return jsonify({
            'error': f"Invalid provider: {data['provider']}. Valid: external_api, faiss, chromadb"
        }), 400


@rag_bp.route('/api/configure', methods=['POST'])
def api_configure():
    """Configure RAG service"""
    rag_service = get_rag_service()
    
    data = request.get_json()
    if not data:
        return jsonify({'error': 'JSON body required'}), 400
    
    try:
        result = rag_service.configure(
            enabled=data.get('enabled'),
            provider_type=data.get('provider_type'),
            base_url=data.get('base_url'),
            api_key=data.get('api_key'),
            data_path=data.get('data_path'),
            embedding_model=data.get('embedding_model'),
            **{k: v for k, v in data.items() 
               if k not in ['enabled', 'provider_type', 'base_url', 'api_key', 'data_path', 'embedding_model']}
        )
        return jsonify(result)
        
    except Exception as e:
        return jsonify({'error': str(e)}), 400


@rag_bp.route('/api/test-connection', methods=['POST'])
def api_test_connection():
    """Test connection to external RAG API"""
    rag_service = get_rag_service()
    
    # Allow testing with temporary config
    data = request.get_json() or {}
    
    if data.get('base_url'):
        # Test with provided config (don't save)
        import httpx
        
        try:
            headers = {}
            if data.get('api_key'):
                prefix = data.get('auth_prefix', 'Bearer')
                header_name = data.get('auth_header', 'Authorization')
                headers[header_name] = f"{prefix} {data['api_key']}"
            
            with httpx.Client(timeout=10.0) as client:
                endpoint = data.get('status_endpoint', '/api/rag/status')
                response = client.get(
                    f"{data['base_url'].rstrip('/')}{endpoint}",
                    headers=headers
                )
                
                return jsonify({
                    'success': response.status_code == 200,
                    'status_code': response.status_code,
                    'response': response.json() if response.status_code == 200 else None,
                    'message': 'Connection successful' if response.status_code == 200 else f'HTTP {response.status_code}'
                })
                
        except httpx.ConnectError as e:
            return jsonify({
                'success': False,
                'message': f'Connection failed: Cannot reach {data["base_url"]}'
            })
        except Exception as e:
            return jsonify({
                'success': False,
                'message': str(e)
            })
    else:
        # Test current config
        status = rag_service.get_api_status()
        return jsonify({
            'success': status.get('reachable', False),
            'status': status
        })


@rag_bp.route('/api/collections', methods=['GET'])
def api_list_collections():
    """List collections - syncs provider with metadata DB"""
    rag_service = get_rag_service()
    metadata_db = get_metadata_db()
    
    if not rag_service.is_enabled():
        # Return collections from metadata DB even if provider is disabled
        collections = metadata_db.list_collections()
        return jsonify({'collections': collections, 'source': 'metadata_db'})
    
    user_id = request.args.get('user_id')
    
    # Get collections from RAG provider
    provider_collections = rag_service.list_collections(user_id=user_id)
    
    # Get collections from metadata DB
    metadata_collections = metadata_db.list_collections()
    metadata_ids = {c.get('collection_id') for c in metadata_collections}
    
    # Sync: Register any provider collections not in metadata DB
    settings = rag_service.get_settings()
    provider_type = settings.get('provider_type', 'faiss')
    
    for coll in provider_collections:
        coll_id = coll.get('id')
        if coll_id and coll_id not in metadata_ids:
            try:
                metadata_db.register_collection(
                    collection_id=coll_id,
                    name=coll.get('name', coll_id),
                    provider=provider_type,
                    description=coll.get('description', ''),
                    is_public=False,
                    metadata=coll.get('metadata', {})
                )
            except Exception as e:
                print(f"Warning: Could not sync collection {coll_id} to metadata: {e}")
    
    # Return merged data with doc counts from provider
    result_collections = []
    for coll in provider_collections:
        coll_id = coll.get('id')
        # Get metadata info if available
        meta_info = next((m for m in metadata_collections if m.get('collection_id') == coll_id), None)
        
        result_collections.append({
            'id': coll_id,
            'name': coll.get('name', coll_id),
            'description': coll.get('description', ''),
            'doc_count': coll.get('doc_count', 0),
            'document_count': meta_info.get('document_count', 0) if meta_info else 0,
            'is_public': meta_info.get('is_public', False) if meta_info else False,
            'metadata': coll.get('metadata', {})
        })
    
    return jsonify({'collections': result_collections, 'source': 'synced'})


@rag_bp.route('/api/query', methods=['POST'])
def api_query_context():
    """Query context from external API (for testing)"""
    rag_service = get_rag_service()
    
    if not rag_service.is_enabled():
        return jsonify({'error': 'RAG not configured'}), 400
    
    data = request.get_json()
    if not data or not data.get('query'):
        return jsonify({'error': 'query is required'}), 400
    
    # Check authorization if user_id provided
    user_id = data.get('user_id')
    if user_id:
        auth_result = rag_service.check_authorization(
            user_id=user_id,
            action='query',
            resource_type='collection',
            resource_id=data.get('collection_id')
        )
        
        if not auth_result.get('allowed', False):
            return jsonify({
                'error': 'Authorization denied',
                'reason': auth_result.get('reason', 'Access denied')
            }), 403
    
    # Retrieve context
    contexts = rag_service.retrieve_context(
        query=data['query'],
        user_id=user_id,
        collection_ids=data.get('collection_ids'),
        max_results=data.get('max_results', 5),
        min_relevance=data.get('min_relevance', 0.0),
        filters=data.get('filters'),
        use_cache=data.get('use_cache', True)
    )
    
    # Format for prompt
    formatted = rag_service.format_context_for_prompt(contexts)
    
    return jsonify({
        'contexts': [c.to_dict() for c in contexts],
        'formatted_prompt': formatted,
        'count': len(contexts)
    })


@rag_bp.route('/api/authorize', methods=['POST'])
def api_check_authorization():
    """Check authorization with external API"""
    rag_service = get_rag_service()
    
    if not rag_service.is_enabled():
        return jsonify({'error': 'RAG not configured'}), 400
    
    data = request.get_json()
    if not data or not data.get('user_id'):
        return jsonify({'error': 'user_id is required'}), 400
    
    result = rag_service.check_authorization(
        user_id=data['user_id'],
        action=data.get('action', 'query'),
        resource_type=data.get('resource_type', 'collection'),
        resource_id=data.get('resource_id')
    )
    
    return jsonify(result)


@rag_bp.route('/api/cache/clear', methods=['POST'])
def api_clear_cache():
    """Clear the context cache"""
    rag_service = get_rag_service()
    rag_service.clear_cache()
    
    return jsonify({
        'success': True,
        'message': 'Cache cleared'
    })


@rag_bp.route('/api/cache/stats', methods=['GET'])
def api_cache_stats():
    """Get cache statistics"""
    rag_service = get_rag_service()
    return jsonify(rag_service.get_cache_stats())


# =====================
# Endpoint Documentation
# =====================

@rag_bp.route('/api/docs', methods=['GET'])
def api_docs():
    """
    Get documentation for external API requirements
    
    Your external API should implement these endpoints:
    """
    return jsonify({
        "description": "External RAG API Requirements",
        "endpoints": {
            "POST /api/rag/authorize": {
                "description": "Check if user is authorized for an action",
                "request": {
                    "user_id": "string - User identifier",
                    "action": "string - Action: query, upload, delete, list",
                    "resource_type": "string - Resource type: collection, document",
                    "resource_id": "string? - Optional specific resource ID"
                },
                "response": {
                    "allowed": "boolean",
                    "reason": "string? - Optional reason for denial",
                    "privileges": "string[]? - Optional list of granted privileges"
                }
            },
            "POST /api/rag/context": {
                "description": "Retrieve relevant context for a query",
                "request": {
                    "query": "string - The query to find context for",
                    "user_id": "string? - Optional user ID for access control",
                    "collection_ids": "string[]? - Optional collection IDs to search",
                    "max_results": "number? - Maximum results (default 5)",
                    "min_relevance": "number? - Minimum relevance 0-1",
                    "filters": "object? - Custom filters"
                },
                "response": {
                    "contexts": [{
                        "id": "string",
                        "content": "string - The context text",
                        "source": "string - Source document/URL",
                        "relevance_score": "number 0-1",
                        "metadata": "object? - Additional metadata"
                    }]
                }
            },
            "GET /api/rag/collections": {
                "description": "List available collections",
                "query_params": {
                    "user_id": "string? - Filter by user access"
                },
                "response": {
                    "collections": [{
                        "id": "string",
                        "name": "string",
                        "doc_count": "number",
                        "description": "string?"
                    }]
                }
            },
            "GET /api/rag/status": {
                "description": "Health check endpoint",
                "response": {
                    "status": "string - 'healthy' or error status",
                    "version": "string?",
                    "...": "any additional info"
                }
            }
        },
        "authentication": {
            "header": "Authorization (configurable)",
            "format": "Bearer {api_key} (configurable)"
        }
    })


# =====================
# Collection Management
# =====================

@rag_bp.route('/api/collections', methods=['POST'])
def api_create_collection():
    """Create a new collection"""
    rag_service = get_rag_service()
    rag_env = get_rag_env()
    
    # Check if environment is ready
    if not rag_env.is_ready:
        return jsonify({
            'error': 'RAG environment not ready. Create environment and install packages first.',
            'env_status': rag_env.status.value
        }), 400
    
    # Try to enable RAG if not enabled
    if not rag_service.is_enabled():
        # Try to initialize with default settings
        settings = rag_service.get_settings()
        provider_type = settings.get('provider_type', 'faiss')
        
        # Configure and enable
        rag_service.configure(
            enabled=True,
            provider_type=provider_type
        )
        
        # Check again
        if not rag_service.is_enabled():
            return jsonify({
                'error': 'RAG could not be enabled. Check that required packages are installed.',
                'hint': 'Install FAISS and sentence-transformers in the RAG environment.'
            }), 400
    
    data = request.get_json()
    if not data or not data.get('name'):
        return jsonify({'error': 'name is required'}), 400
    
    # Create in RAG provider
    result = rag_service.create_collection(
        name=data['name'],
        description=data.get('description', ''),
        metadata=data.get('metadata')
    )
    
    if result.get('success'):
        # Also register in metadata DB for persistence
        metadata_db = get_metadata_db()
        try:
            settings = rag_service.get_settings()
            provider_type = settings.get('provider_type', 'faiss')
            
            metadata_db.register_collection(
                collection_id=result.get('collection', {}).get('id', result.get('id', '')),
                name=data['name'],
                provider=provider_type,
                description=data.get('description', ''),
                is_public=data.get('is_public', False),
                metadata=data.get('metadata')
            )
        except Exception as e:
            print(f"Warning: Could not register collection in metadata: {e}")
        
        return jsonify(result), 201
    else:
        return jsonify(result), 400


@rag_bp.route('/api/collections/<collection_id>', methods=['GET'])
def api_get_collection(collection_id):
    """Get collection details including documents - syncs with metadata DB"""
    rag_service = get_rag_service()
    metadata_db = get_metadata_db()
    
    # Get collection info from metadata DB
    collection_info = metadata_db.get_collection_by_rag_id(collection_id)
    
    # If not in metadata DB, try to sync from provider
    if not collection_info and rag_service.is_enabled():
        try:
            # Get from provider's list
            provider_collections = rag_service.list_collections()
            provider_coll = next((c for c in provider_collections if c.get('id') == collection_id), None)
            
            if provider_coll:
                # Register in metadata DB
                settings = rag_service.get_settings()
                provider_type = settings.get('provider_type', 'faiss')
                
                metadata_db.register_collection(
                    collection_id=collection_id,
                    name=provider_coll.get('name', collection_id),
                    provider=provider_type,
                    description=provider_coll.get('description', ''),
                    is_public=False,
                    metadata=provider_coll.get('metadata', {})
                )
                collection_info = metadata_db.get_collection_by_rag_id(collection_id)
        except Exception as e:
            print(f"Warning: Could not sync collection from provider: {e}")
    
    if not collection_info:
        return jsonify({'error': 'Collection not found'}), 404
    
    # Get documents from metadata DB
    documents = metadata_db.list_documents(collection_id)
    
    # Get document count from RAG provider if available
    provider_doc_count = 0
    provider_documents = []
    if rag_service.is_enabled():
        try:
            provider_collections = rag_service.list_collections()
            provider_coll = next((c for c in provider_collections if c.get('id') == collection_id), None)
            if provider_coll:
                provider_doc_count = provider_coll.get('doc_count', 0)
            
            # If metadata DB has no documents but provider does, sync them
            if len(documents) == 0 and provider_doc_count > 0:
                provider_documents = rag_service.get_documents(collection_id)
                
                # Register provider documents in metadata DB for persistence
                for doc in provider_documents:
                    try:
                        metadata_db.register_document(
                            document_id=doc.get('document_id') or doc.get('id', ''),
                            collection_id=collection_id,
                            source=doc.get('source', 'unknown'),
                            title=doc.get('title', doc.get('source', 'Untitled')),
                            chunk_count=1,
                            uploaded_by=None,
                            metadata=doc.get('metadata', {})
                        )
                    except Exception as e:
                        print(f"Warning: Could not sync document to metadata: {e}")
                
                # Reload documents after sync
                documents = metadata_db.list_documents(collection_id)
                
                # If still empty, use provider documents directly
                if len(documents) == 0:
                    documents = provider_documents
        except Exception as e:
            print(f"Warning: Could not get provider documents: {e}")
    
    return jsonify({
        'success': True,
        'collection': {
            'id': collection_id,
            'name': collection_info.get('name', collection_id),
            'description': collection_info.get('description', ''),
            'provider': collection_info.get('provider', 'unknown'),
            'document_count': len(documents) or collection_info.get('document_count', 0),
            'doc_count': provider_doc_count or len(documents),
            'is_public': collection_info.get('is_public', False),
            'created_at': collection_info.get('created_at'),
            'metadata': collection_info.get('metadata', {})
        },
        'documents': documents
    })


@rag_bp.route('/api/collections/<collection_id>', methods=['PUT'])
def api_update_collection(collection_id):
    """Update a collection's metadata"""
    metadata_db = get_metadata_db()
    
    data = request.get_json()
    if not data:
        return jsonify({'error': 'JSON body required'}), 400
    
    # Update in metadata DB
    result = metadata_db.update_collection(
        collection_id=collection_id,
        name=data.get('name'),
        description=data.get('description'),
        is_public=data.get('is_public'),
        metadata=data.get('metadata')
    )
    
    if result:
        return jsonify({'success': True, 'collection': result})
    else:
        return jsonify({'error': 'Collection not found or update failed'}), 404


@rag_bp.route('/api/collections/<collection_id>', methods=['DELETE'])
def api_delete_collection(collection_id):
    """Delete a collection"""
    rag_service = get_rag_service()
    metadata_db = get_metadata_db()
    
    if not rag_service.is_enabled():
        return jsonify({'error': 'RAG not enabled'}), 400
    
    # Delete from RAG provider
    result = rag_service.delete_collection(collection_id)
    
    # Also delete from metadata DB (cascades to documents)
    try:
        metadata_db.delete_collection(collection_id)
    except Exception as e:
        print(f"Warning: Could not remove collection from metadata: {e}")
    
    if result.get('success'):
        return jsonify(result)
    else:
        return jsonify(result), 400


# =====================
# Document Management
# =====================

@rag_bp.route('/api/documents/<document_id>', methods=['GET'])
def api_get_document(document_id):
    """Get a single document by ID"""
    metadata_db = get_metadata_db()
    collection_id = request.args.get('collection_id')
    
    if not collection_id:
        return jsonify({'error': 'collection_id is required'}), 400
    
    # Get from metadata DB
    docs = metadata_db.list_documents(collection_id)
    doc = next((d for d in docs if d.get('document_id') == document_id), None)
    
    if doc:
        return jsonify({'success': True, 'document': doc})
    else:
        return jsonify({'error': 'Document not found'}), 404


@rag_bp.route('/api/documents/<document_id>', methods=['DELETE'])
def api_delete_single_document(document_id):
    """Delete a single document"""
    rag_service = get_rag_service()
    metadata_db = get_metadata_db()
    
    collection_id = request.args.get('collection_id')
    if not collection_id:
        return jsonify({'error': 'collection_id is required'}), 400
    
    # Delete from RAG provider
    if rag_service.is_enabled():
        try:
            rag_service.delete_documents([document_id], collection_id)
        except Exception as e:
            print(f"Warning: Could not delete from RAG provider: {e}")
    
    # Delete from metadata DB
    success = metadata_db.delete_document(document_id, collection_id)
    
    if success:
        return jsonify({'success': True, 'deleted': 1})
    else:
        return jsonify({'error': 'Document not found'}), 404


@rag_bp.route('/api/documents/upload', methods=['POST'])
def api_upload_files():
    """Upload files to a collection (supports multiple files)"""
    import hashlib
    from werkzeug.utils import secure_filename
    
    rag_service = get_rag_service()
    metadata_db = get_metadata_db()
    
    # Check if RAG is ready
    if not rag_service.is_enabled():
        return jsonify({'error': 'RAG not enabled. Please complete setup first.'}), 400
    
    collection_id = request.form.get('collection_id')
    if not collection_id:
        return jsonify({'error': 'collection_id is required'}), 400
    
    files = request.files.getlist('files')
    if not files:
        return jsonify({'error': 'No files uploaded'}), 400
    
    # Allowed file types - use extractor's supported formats
    extractor = get_document_extractor()
    ALLOWED_EXTENSIONS = set(extractor.SUPPORTED_EXTENSIONS.keys())
    
    def allowed_file(filename):
        return '.' in filename and filename.rsplit('.', 1)[1].lower() in ALLOWED_EXTENSIONS
    
    def extract_text(file, filename):
        """Extract text from various file types using DocumentExtractor"""
        try:
            content = file.read()
            extractor = get_document_extractor()
            result = extractor.extract(file_content=content, filename=filename)
            
            if result.success:
                return result.text
            else:
                return f"[{filename} - {result.error}]"
        except Exception as e:
            return f"[Error reading file: {str(e)}]"
    
    results = {
        'success': True,
        'uploaded': [],
        'failed': [],
        'total': len(files)
    }
    
    documents = []
    updated_docs = []
    
    for file in files:
        if not file or not file.filename:
            continue
            
        filename = secure_filename(file.filename)
        
        if not allowed_file(filename):
            results['failed'].append({
                'filename': filename,
                'error': f'File type not allowed. Allowed: {", ".join(ALLOWED_EXTENSIONS)}'
            })
            continue
        
        try:
            # Extract text content
            text_content = extract_text(file, filename)
            
            # Compute content hash for deduplication/tracking
            content_hash = hashlib.sha256(text_content.encode()).hexdigest()[:16]
            
            # Check if document with same source already exists (for update)
            existing_doc = metadata_db.get_document_by_source(filename, collection_id)
            is_update = False
            
            if existing_doc:
                # Check if content changed
                if existing_doc.get('content_hash') == content_hash:
                    results['uploaded'].append({
                        'filename': filename,
                        'size': len(text_content),
                        'hash': content_hash,
                        'status': 'skipped',
                        'reason': 'Content unchanged'
                    })
                    continue
                else:
                    is_update = True
                    updated_docs.append({
                        'old_doc_id': existing_doc.get('document_id'),
                        'filename': filename,
                        'content_hash': content_hash
                    })
            
            # Create document
            doc = {
                'content': text_content,
                'source': filename,
                'title': filename,
                'metadata': {
                    'original_filename': file.filename,
                    'content_hash': content_hash,
                    'file_size': len(text_content)
                }
            }
            documents.append(doc)
            
            results['uploaded'].append({
                'filename': filename,
                'size': len(text_content),
                'hash': content_hash,
                'status': 'updated' if is_update else 'new'
            })
            
        except Exception as e:
            results['failed'].append({
                'filename': filename,
                'error': str(e)
            })
    
    # Delete old versions for updated documents
    for update in updated_docs:
        try:
            rag_service.delete_documents([update['old_doc_id']], collection_id)
            metadata_db.delete_document(update['old_doc_id'], collection_id)
        except Exception as e:
            print(f"Warning: Could not delete old document version: {e}")
    
    # Add documents to RAG
    if documents:
        add_result = rag_service.add_documents(
            documents=documents,
            collection_id=collection_id
        )
        
        if add_result.get('success'):
            # Track documents in metadata DB
            for doc in documents:
                try:
                    content_hash = doc.get('metadata', {}).get('content_hash', '')
                    metadata_db.register_document(
                        collection_id=collection_id,
                        document_id=content_hash,
                        source=doc.get('source'),
                        title=doc.get('title'),
                        content_hash=content_hash,
                        chunk_count=1,
                        uploaded_by=None,  # TODO: Add user context
                        metadata=doc.get('metadata', {})
                    )
                except Exception as e:
                    # Log but don't fail
                    print(f"Warning: Could not track document in metadata: {e}")
            
            results['added_to_rag'] = add_result.get('added', len(documents))
            results['updated'] = len(updated_docs)
        else:
            results['rag_error'] = add_result.get('error', 'Failed to add to RAG')
            results['success'] = False
    
    return jsonify(results), 201 if results['success'] else 400


@rag_bp.route('/api/documents', methods=['POST'])
def api_add_documents():
    """Add documents to a collection"""
    rag_service = get_rag_service()
    
    if not rag_service.is_enabled():
        return jsonify({'error': 'RAG not enabled'}), 400
    
    data = request.get_json()
    if not data:
        return jsonify({'error': 'JSON body required'}), 400
    
    if not data.get('collection_id'):
        return jsonify({'error': 'collection_id is required'}), 400
    
    if not data.get('documents'):
        return jsonify({'error': 'documents array is required'}), 400
    
    import hashlib
    
    # Add documents to RAG vector store
    result = rag_service.add_documents(
        documents=data['documents'],
        collection_id=data['collection_id']
    )
    
    if result.get('success'):
        # Also register in metadata DB for persistence
        metadata_db = get_metadata_db()
        for doc in data['documents']:
            try:
                # Generate document ID from content hash if not provided
                content = doc.get('content', '')
                content_hash = hashlib.sha256(content.encode()).hexdigest()[:16]
                doc_id = doc.get('id') or content_hash
                
                metadata_db.register_document(
                    document_id=doc_id,
                    collection_id=data['collection_id'],
                    source=doc.get('source', 'manual_input'),
                    title=doc.get('title', doc.get('source', 'Untitled')),
                    chunk_count=1,
                    uploaded_by=None,
                    metadata={
                        'content_hash': content_hash,
                        'content_length': len(content),
                        **doc.get('metadata', {})
                    }
                )
            except Exception as e:
                print(f"Warning: Could not track document in metadata: {e}")
        
        return jsonify(result), 201
    else:
        return jsonify(result), 400


@rag_bp.route('/api/documents/search', methods=['POST'])
def api_search_documents():
    """Search documents in a collection"""
    rag_service = get_rag_service()
    
    if not rag_service.is_enabled():
        return jsonify({'error': 'RAG not enabled'}), 400
    
    data = request.get_json()
    if not data or not data.get('query'):
        return jsonify({'error': 'query is required'}), 400
    
    if not data.get('collection_id'):
        return jsonify({'error': 'collection_id is required'}), 400
    
    documents = rag_service.search_documents(
        query=data['query'],
        collection_id=data['collection_id'],
        limit=data.get('limit', 10)
    )
    
    return jsonify({
        'documents': documents,
        'count': len(documents)
    })


@rag_bp.route('/api/documents', methods=['DELETE'])
def api_delete_documents():
    """Delete documents from a collection"""
    rag_service = get_rag_service()
    metadata_db = get_metadata_db()
    
    if not rag_service.is_enabled():
        return jsonify({'error': 'RAG not enabled'}), 400
    
    data = request.get_json()
    if not data:
        return jsonify({'error': 'JSON body required'}), 400
    
    if not data.get('collection_id'):
        return jsonify({'error': 'collection_id is required'}), 400
    
    if not data.get('document_ids'):
        return jsonify({'error': 'document_ids array is required'}), 400
    
    # Delete from RAG vector store
    result = rag_service.delete_documents(
        document_ids=data['document_ids'],
        collection_id=data['collection_id']
    )
    
    # Also delete from metadata DB
    for doc_id in data['document_ids']:
        try:
            metadata_db.delete_document(doc_id, data['collection_id'])
        except Exception as e:
            print(f"Warning: Could not remove document from metadata: {e}")
    
    if result.get('success'):
        return jsonify(result)
    else:
        return jsonify(result), 400


# =====================
# Local RAG Management API
# =====================

@rag_bp.route('/api/local/configure', methods=['POST'])
def api_local_configure():
    """Configure local RAG provider settings"""
    rag_service = get_rag_service()
    
    data = request.get_json()
    if not data:
        return jsonify({'error': 'JSON body required'}), 400
    
    try:
        result = rag_service.configure(
            provider_type=data.get('provider_type'),
            data_path=data.get('data_path'),
            embedding_model=data.get('embedding_model'),
            enabled=True  # Auto-enable when configuring
        )
        return jsonify(result)
    except Exception as e:
        return jsonify({'error': str(e)}), 400


# =====================
# Pipeline API Management
# =====================

@rag_bp.route('/api/pipeline/configure', methods=['POST'])
def api_pipeline_configure():
    """Configure external API pipeline settings"""
    rag_service = get_rag_service()
    
    data = request.get_json()
    if not data:
        return jsonify({'error': 'JSON body required'}), 400
    
    try:
        result = rag_service.configure(
            use_auth_api=data.get('use_auth_api'),
            use_context_api=data.get('use_context_api'),
            fallback_on_api_error=data.get('fallback_on_api_error'),
            base_url=data.get('base_url'),
            api_key=data.get('api_key'),
            timeout=data.get('timeout'),
            auth_header=data.get('auth_header'),
            auth_prefix=data.get('auth_prefix'),
            auth_endpoint=data.get('auth_endpoint'),
            context_processing_endpoint=data.get('context_processing_endpoint')
        )
        return jsonify(result)
    except Exception as e:
        return jsonify({'error': str(e)}), 400


@rag_bp.route('/api/pipeline/test-auth', methods=['POST'])
def api_test_auth_api():
    """Test the external Auth API endpoint"""
    import httpx
    
    data = request.get_json()
    if not data:
        return jsonify({'error': 'JSON body required'}), 400
    
    base_url = data.get('base_url')
    endpoint = data.get('endpoint', '/api/rag/authorize')
    
    if not base_url:
        return jsonify({'error': 'base_url is required'}), 400
    
    try:
        url = f"{base_url.rstrip('/')}{endpoint}"
        headers = {'Content-Type': 'application/json'}
        
        if data.get('api_key'):
            auth_header = data.get('auth_header', 'Authorization')
            auth_prefix = data.get('auth_prefix', 'Bearer')
            headers[auth_header] = f"{auth_prefix} {data['api_key']}"
        
        payload = {
            'user_id': data.get('user_id', 'test_user'),
            'action': data.get('action', 'query'),
            'resource_type': 'collection'
        }
        
        with httpx.Client(timeout=10.0) as client:
            response = client.post(url, json=payload, headers=headers)
            
            if response.status_code == 200:
                result = response.json()
                return jsonify({
                    'success': True,
                    'allowed': result.get('allowed', False),
                    'response': result
                })
            else:
                return jsonify({
                    'success': False,
                    'error': f"HTTP {response.status_code}",
                    'response': response.text[:500]
                })
                
    except httpx.ConnectError:
        return jsonify({'success': False, 'error': 'Connection failed'})
    except httpx.TimeoutException:
        return jsonify({'success': False, 'error': 'Request timeout'})
    except Exception as e:
        return jsonify({'success': False, 'error': str(e)})


@rag_bp.route('/api/pipeline/test-context', methods=['POST'])
def api_test_context_api():
    """Test the external Context Processing API endpoint"""
    import httpx
    
    data = request.get_json()
    if not data:
        return jsonify({'error': 'JSON body required'}), 400
    
    base_url = data.get('base_url')
    endpoint = data.get('endpoint', '/api/rag/process-context')
    
    if not base_url:
        return jsonify({'error': 'base_url is required'}), 400
    
    try:
        url = f"{base_url.rstrip('/')}{endpoint}"
        headers = {'Content-Type': 'application/json'}
        
        if data.get('api_key'):
            auth_header = data.get('auth_header', 'Authorization')
            auth_prefix = data.get('auth_prefix', 'Bearer')
            headers[auth_header] = f"{auth_prefix} {data['api_key']}"
        
        payload = {
            'query': data.get('query', 'test query'),
            'user_id': data.get('user_id', 'test_user'),
            'contexts': data.get('contexts', []),
            'options': {}
        }
        
        with httpx.Client(timeout=10.0) as client:
            response = client.post(url, json=payload, headers=headers)
            
            if response.status_code == 200:
                result = response.json()
                return jsonify({
                    'success': True,
                    'context_count': len(result.get('contexts', [])),
                    'response': result
                })
            else:
                return jsonify({
                    'success': False,
                    'error': f"HTTP {response.status_code}",
                    'response': response.text[:500]
                })
                
    except httpx.ConnectError:
        return jsonify({'success': False, 'error': 'Connection failed'})
    except httpx.TimeoutException:
        return jsonify({'success': False, 'error': 'Request timeout'})
    except Exception as e:
        return jsonify({'success': False, 'error': str(e)})


@rag_bp.route('/api/pipeline/test-full', methods=['POST'])
def api_test_full_pipeline():
    """Test the full RAG pipeline: Auth API → RAG → Context API"""
    rag_service = get_rag_service()
    
    data = request.get_json()
    if not data or not data.get('query'):
        return jsonify({'error': 'query is required'}), 400
    
    query = data['query']
    user_id = data.get('user_id', 'test_user')
    settings = rag_service.get_settings()
    
    results = {
        'query': query,
        'user_id': user_id,
        'pipeline_steps': [],
        'success': True
    }
    
    # Step 1: Auth API (if enabled)
    if settings.get('use_auth_api'):
        auth_result = rag_service.check_authorization(
            user_id=user_id,
            action='query',
            resource_type='collection'
        )
        results['pipeline_steps'].append({
            'step': 1,
            'name': 'Auth API',
            'status': 'success' if auth_result.get('allowed') else 'denied',
            'result': auth_result
        })
        
        if not auth_result.get('allowed'):
            results['success'] = False
            results['error'] = 'Authorization denied'
            return jsonify(results)
    else:
        results['pipeline_steps'].append({
            'step': 1,
            'name': 'Auth API',
            'status': 'skipped',
            'result': 'Not enabled'
        })
    
    # Step 2: RAG Retrieval
    contexts = rag_service.retrieve_context(
        query=query,
        user_id=user_id
    )
    results['pipeline_steps'].append({
        'step': 2,
        'name': 'RAG Retrieval',
        'status': 'success' if contexts else 'no_results',
        'result': {
            'contexts_found': len(contexts),
            'contexts': [c.to_dict() for c in contexts[:3]]  # First 3 for display
        }
    })
    
    # Step 3: Context API (if enabled)
    if settings.get('use_context_api') and contexts:
        processed = rag_service.process_context(
            contexts=contexts,
            query=query,
            user_id=user_id
        )
        results['pipeline_steps'].append({
            'step': 3,
            'name': 'Context API',
            'status': 'success' if processed else 'error',
            'result': {
                'contexts_processed': len(processed),
                'contexts': [c.to_dict() for c in processed[:3]]
            }
        })
        contexts = processed
    else:
        results['pipeline_steps'].append({
            'step': 3,
            'name': 'Context API',
            'status': 'skipped',
            'result': 'Not enabled' if not settings.get('use_context_api') else 'No contexts to process'
        })
    
    # Final formatted context for LLM
    if contexts:
        results['formatted_context'] = rag_service.format_context_for_prompt(contexts)
    
    return jsonify(results)


# =====================
# RAG Environment Management API
# =====================


@rag_bp.route('/api/env/status', methods=['GET'])
def api_rag_env_status():
    """Get RAG environment status"""
    from app.services.rag_environment_manager import get_rag_environment_manager
    env_mgr = get_rag_environment_manager()
    return jsonify(env_mgr.get_venv_status())


@rag_bp.route('/api/env/setup', methods=['POST'])
def api_rag_env_setup():
    """Setup RAG environment (create venv and install packages) - async for wizard"""
    from app.services.rag_environment_manager import get_rag_environment_manager
    
    env_mgr = get_rag_environment_manager()
    
    # Check if already installed
    if env_mgr.is_venv_installed():
        return jsonify({
            'success': False,
            'error': 'RAG environment already installed'
        }), 400
    
    # Get options from request
    data = request.get_json() or {}
    install_chromadb = data.get('install_chromadb', True)
    install_faiss = data.get('install_faiss', True)
    
    # Start async setup (for wizard progress tracking)
    result = env_mgr.setup_environment_async(
        install_chromadb=install_chromadb,
        install_faiss=install_faiss
    )
    
    return jsonify(result)


@rag_bp.route('/api/env/progress', methods=['GET'])
def api_rag_env_progress():
    """Get RAG environment installation progress"""
    from app.services.rag_environment_manager import get_rag_environment_manager
    
    env_mgr = get_rag_environment_manager()
    return jsonify(env_mgr.get_installation_progress())


@rag_bp.route('/api/database/switch', methods=['POST'])
def api_switch_database():
    """Switch active RAG database"""
    from app.services.rag_environment_manager import get_rag_environment_manager
    
    data = request.get_json()
    db_name = data.get('database')
    
    if not db_name:
        return jsonify({'success': False, 'error': 'Database name required'}), 400
    
    env_mgr = get_rag_environment_manager()
    
    if env_mgr.set_active_database(db_name):
        return jsonify({
            'success': True,
            'message': f'Switched to database: {db_name}'
        })
    else:
        return jsonify({
            'success': False,
            'error': 'Failed to switch database'
        }), 500


@rag_bp.route('/api/databases', methods=['GET'])
def api_list_databases():
    """List all RAG databases"""
    from app.services.rag_environment_manager import get_rag_environment_manager
    
    env_mgr = get_rag_environment_manager()
    databases = env_mgr.list_databases()
    

# Legacy routes removed or replaced by new implementation
    
    result = rag_env.install_packages(packages=packages, use_gpu=use_gpu)
    
    if result.get('success'):
        return jsonify(result)
    return jsonify(result), 400


@rag_bp.route('/api/env/install/<package_name>', methods=['POST'])
def api_env_install_package(package_name):
    """Install a single package in RAG environment"""
    rag_env = get_rag_env()
    
    data = request.get_json() or {}
    use_gpu = data.get('use_gpu', False)
    
    result = rag_env.install_package(package_name, use_gpu=use_gpu)
    
    if result.get('success'):
        return jsonify(result)
    return jsonify(result), 400


@rag_bp.route('/api/env/reinstall/<package_name>', methods=['POST'])
def api_env_reinstall_package(package_name):
    """Reinstall (force upgrade) a package in RAG environment"""
    rag_env = get_rag_env()
    
    data = request.get_json() or {}
    use_gpu = data.get('use_gpu', False)
    
    # First uninstall, then install
    rag_env.uninstall_package(package_name)
    result = rag_env.install_package(package_name, use_gpu=use_gpu)
    
    if result.get('success'):
        return jsonify({
            'success': True,
            'message': f'Package {package_name} reinstalled successfully',
            'details': result
        })
    return jsonify(result), 400


@rag_bp.route('/api/env/install-scheduler', methods=['POST'])
def api_env_install_scheduler():
    """Install APScheduler in RAG environment for sync job scheduling"""
    rag_env = get_rag_env()
    
    if not rag_env.is_ready:
        return jsonify({
            'success': False,
            'error': 'RAG environment not ready. Please set up the RAG environment first.'
        }), 400
    
    # Run installation in background thread for faster response
    import threading
    
    def install_in_background():
        try:
            result = rag_env.install_package('apscheduler')
            if result.get('success'):
                # Reinitialize the scheduler
                from app.services.sync_scheduler import get_sync_scheduler
                scheduler = get_sync_scheduler()
                scheduler._check_rag_environment()
        except Exception as e:
            print(f"Background APScheduler install failed: {e}")
    
    thread = threading.Thread(target=install_in_background, daemon=True)
    thread.start()
    
    return jsonify({
        'success': True,
        'message': 'APScheduler installation started. This may take 1-2 minutes.',
        'status': 'installing'
    })


@rag_bp.route('/api/env/check-package/<package_name>', methods=['GET'])
def api_env_check_package_installed(package_name):
    """Check if a package is installed in RAG environment"""
    rag_env = get_rag_env()
    
    packages = rag_env.get_installed_packages()
    package_names = [p.get('name', '').lower() for p in packages]
    
    installed = package_name.lower() in package_names
    
    return jsonify({
        'package': package_name,
        'installed': installed
    })


@rag_bp.route('/api/env/uninstall/<package_name>', methods=['DELETE'])
def api_env_uninstall_package(package_name):
    """Uninstall a package from RAG environment"""
    rag_env = get_rag_env()
    
    result = rag_env.uninstall_package(package_name)
    
    if result.get('success'):
        return jsonify(result)
    return jsonify(result), 400


@rag_bp.route('/api/env/packages', methods=['GET'])
def api_env_list_packages():
    """List installed packages in RAG environment"""
    rag_env = get_rag_env()
    
    packages = rag_env.get_installed_packages()
    return jsonify({'packages': packages})


@rag_bp.route('/api/env/check/<package_name>', methods=['GET'])
def api_env_check_package(package_name):
    """Check if a package is installed and importable"""
    rag_env = get_rag_env()
    
    installed = rag_env.check_package(package_name)
    return jsonify({
        'package': package_name,
        'installed': installed
    })


@rag_bp.route('/api/env/delete', methods=['DELETE'])
def api_env_delete():
    """Delete RAG virtual environment"""
    rag_env = get_rag_env()
    
    result = rag_env.delete_environment()
    
    if result.get('success'):
        return jsonify(result)
    return jsonify(result), 400


@rag_bp.route('/api/env/run', methods=['POST'])
def api_env_run_script():
    """Run a Python script in RAG environment"""
    rag_env = get_rag_env()
    
    data = request.get_json()
    if not data or not data.get('script'):
        return jsonify({'error': 'script is required'}), 400
    
    result = rag_env.run_in_env(
        script=data['script'],
        timeout=data.get('timeout', 30)
    )
    
    return jsonify(result)


# =====================
# Backup & Restore API
# =====================

@rag_bp.route('/api/backup/list', methods=['GET'])
def api_list_backups():
    """List available RAG data backups"""
    rag_env = get_rag_env()
    backups = rag_env.list_backups()
    return jsonify({'backups': backups})


@rag_bp.route('/api/backup/create', methods=['POST'])
def api_create_backup():
    """Create a backup of RAG data"""
    rag_env = get_rag_env()
    
    data = request.get_json() or {}
    name = data.get('name')
    
    result = rag_env.create_backup(name=name)
    
    if result.get('success'):
        return jsonify(result), 201
    return jsonify(result), 400


@rag_bp.route('/api/backup/restore/<backup_name>', methods=['POST'])
def api_restore_backup(backup_name):
    """Restore RAG data from a backup"""
    rag_env = get_rag_env()
    
    data = request.get_json() or {}
    overwrite = data.get('overwrite', False)
    
    result = rag_env.restore_backup(backup_name, overwrite=overwrite)
    
    if result.get('success'):
        return jsonify(result)
    return jsonify(result), 400


@rag_bp.route('/api/backup/<backup_name>', methods=['DELETE'])
def api_delete_backup(backup_name):
    """Delete a backup"""
    rag_env = get_rag_env()
    
    result = rag_env.delete_backup(backup_name)
    
    if result.get('success'):
        return jsonify(result)
    return jsonify(result), 400


@rag_bp.route('/api/data/clear', methods=['POST'])
def api_clear_data():
    """Clear all RAG data (with optional backup)"""
    rag_env = get_rag_env()
    
    data = request.get_json() or {}
    backup_first = data.get('backup_first', True)
    
    result = rag_env.clear_data(backup_first=backup_first)
    
    if result.get('success'):
        return jsonify(result)
    return jsonify(result), 400


@rag_bp.route('/api/data/export', methods=['POST'])
def api_export_data():
    """Export RAG data to a zip file"""
    rag_env = get_rag_env()
    
    data = request.get_json() or {}
    export_path = data.get('path')
    
    if not export_path:
        # Use default path in backups folder
        from datetime import datetime
        export_path = str(rag_env.get_backup_path() / f"export_{datetime.now().strftime('%Y%m%d_%H%M%S')}.zip")
    
    result = rag_env.export_data(export_path)
    
    if result.get('success'):
        return jsonify(result)
    return jsonify(result), 400


@rag_bp.route('/api/data/import', methods=['POST'])
def api_import_data():
    """Import RAG data from a zip file"""
    rag_env = get_rag_env()
    
    data = request.get_json()
    if not data or not data.get('path'):
        return jsonify({'error': 'path is required'}), 400
    
    overwrite = data.get('overwrite', False)
    
    result = rag_env.import_data(data['path'], overwrite=overwrite)
    
    if result.get('success'):
        return jsonify(result)
    return jsonify(result), 400


# =====================
# Initialize/Enable RAG
# =====================

@rag_bp.route('/api/initialize', methods=['POST'])
def api_initialize_rag():
    """Initialize RAG with provider settings - enables RAG and creates first collection"""
    rag_service = get_rag_service()
    rag_env = get_rag_env()
    
    # Check environment is ready
    if not rag_env.is_ready:
        return jsonify({'error': 'RAG environment not ready. Create and install packages first.'}), 400
    
    data = request.get_json() or {}
    
    # Configure RAG
    provider_type = data.get('provider_type', 'faiss')
    
    result = rag_service.configure(
        enabled=True,
        provider_type=provider_type,
        data_path=data.get('data_path'),
        embedding_model=data.get('embedding_model', 'all-MiniLM-L6-v2')
    )
    
    return jsonify({
        'success': True,
        'message': 'RAG initialized',
        'settings': rag_service.get_settings()
    })


# =====================
# Provider Setup API
# =====================

@rag_bp.route('/api/env/setup-provider', methods=['POST'])
def api_setup_provider():
    """Setup a specific RAG provider (installs required packages)"""
    rag_env = get_rag_env()
    
    data = request.get_json() or {}
    provider = data.get('provider', '').lower()
    use_gpu = data.get('use_gpu', False)
    
    if provider not in ['faiss', 'chromadb']:
        return jsonify({'error': 'Invalid provider. Must be faiss or chromadb'}), 400
    
    result = rag_env.setup_provider(provider, use_gpu=use_gpu)
    
    if result.get('success'):
        return jsonify(result)
    return jsonify(result), 400


@rag_bp.route('/api/env/recreate', methods=['POST'])
def api_env_recreate():
    """Recreate RAG environment (delete and create fresh)"""
    rag_env = get_rag_env()
    
    # Delete existing
    rag_env.delete_environment()
    
    # Create new
    result = rag_env.create_environment()
    
    if result.get('success'):
        return jsonify(result)
    return jsonify(result), 400


# =====================
# Metadata API - Users
# =====================

@rag_bp.route('/api/metadata/users', methods=['GET'])
def api_list_users():
    """List all users"""
    db = get_metadata_db()
    active_only = request.args.get('active', 'true').lower() == 'true'
    
    users = db.list_users(active_only=active_only)
    return jsonify({'users': [asdict(u) for u in users]})


@rag_bp.route('/api/metadata/users', methods=['POST'])
def api_create_user():
    """Create a new user"""
    db = get_metadata_db()
    
    data = request.get_json()
    if not data or not data.get('username'):
        return jsonify({'error': 'username is required'}), 400
    
    user = db.create_user(
        username=data['username'],
        display_name=data.get('display_name', ''),
        email=data.get('email'),
        is_admin=data.get('is_admin', False),
        metadata=data.get('metadata')
    )
    
    if user:
        return jsonify({'success': True, 'user': asdict(user)}), 201
    return jsonify({'error': 'User already exists'}), 400


@rag_bp.route('/api/metadata/users/<int:user_id>', methods=['GET'])
def api_get_user(user_id):
    """Get a user by ID"""
    db = get_metadata_db()
    user = db.get_user(user_id)
    
    if user:
        return jsonify({'user': asdict(user)})
    return jsonify({'error': 'User not found'}), 404


@rag_bp.route('/api/metadata/users/<int:user_id>', methods=['DELETE'])
def api_delete_user(user_id):
    """Delete a user"""
    db = get_metadata_db()
    
    if db.delete_user(user_id):
        return jsonify({'success': True})
    return jsonify({'error': 'User not found'}), 404


# =====================
# Metadata API - Groups
# =====================

@rag_bp.route('/api/metadata/groups', methods=['GET'])
def api_list_groups():
    """List all groups"""
    db = get_metadata_db()
    active_only = request.args.get('active', 'true').lower() == 'true'
    
    groups = db.list_groups(active_only=active_only)
    return jsonify({'groups': [asdict(g) for g in groups]})


@rag_bp.route('/api/metadata/groups', methods=['POST'])
def api_create_group():
    """Create a new group"""
    db = get_metadata_db()
    
    data = request.get_json()
    if not data or not data.get('name'):
        return jsonify({'error': 'name is required'}), 400
    
    group = db.create_group(
        name=data['name'],
        description=data.get('description', ''),
        metadata=data.get('metadata')
    )
    
    if group:
        return jsonify({'success': True, 'group': asdict(group)}), 201
    return jsonify({'error': 'Group already exists'}), 400


@rag_bp.route('/api/metadata/groups/<int:group_id>/members', methods=['POST'])
def api_add_user_to_group(group_id):
    """Add a user to a group"""
    db = get_metadata_db()
    
    data = request.get_json()
    if not data or not data.get('user_id'):
        return jsonify({'error': 'user_id is required'}), 400
    
    if db.add_user_to_group(data['user_id'], group_id):
        return jsonify({'success': True})
    return jsonify({'error': 'Failed to add user to group'}), 400


@rag_bp.route('/api/metadata/groups/<int:group_id>/members/<int:user_id>', methods=['DELETE'])
def api_remove_user_from_group(group_id, user_id):
    """Remove a user from a group"""
    db = get_metadata_db()
    
    if db.remove_user_from_group(user_id, group_id):
        return jsonify({'success': True})
    return jsonify({'error': 'User not in group'}), 404


# =====================
# Metadata API - Collection Access
# =====================

@rag_bp.route('/api/collections/<collection_id>/access', methods=['GET'])
def api_get_collection_access(collection_id):
    """Get access privileges for a collection"""
    db = get_metadata_db()
    
    # Get collection from metadata
    collection = db.get_collection_by_rag_id(collection_id)
    if not collection:
        return jsonify({'access': []})
    
    # collection is a dict, get the 'id' key
    access = db.get_resource_access('collection', collection.get('id'))
    return jsonify({'access': access})


@rag_bp.route('/api/collections/<collection_id>/access', methods=['POST'])
def api_grant_collection_access(collection_id):
    """Grant access to a collection"""
    db = get_metadata_db()
    
    data = request.get_json()
    if not data:
        return jsonify({'error': 'Request body required'}), 400
    
    user_id = data.get('user_id')
    group_id = data.get('group_id')
    
    if not user_id and not group_id:
        return jsonify({'error': 'user_id or group_id required'}), 400
    
    # Get collection from metadata
    collection = db.get_collection_by_rag_id(collection_id)
    if not collection:
        return jsonify({'error': 'Collection not found in metadata'}), 404
    
    if db.grant_access(
        resource_type='collection',
        resource_id=collection.get('id'),
        access_level=data.get('access_level', 'read'),
        user_id=int(user_id) if user_id else None,
        group_id=int(group_id) if group_id else None,
        granted_by=data.get('granted_by')
    ):
        return jsonify({'success': True})
    return jsonify({'error': 'Failed to grant access'}), 400


@rag_bp.route('/api/collections/<collection_id>/access', methods=['DELETE'])
def api_revoke_collection_access(collection_id):
    """Revoke access to a collection"""
    db = get_metadata_db()
    
    data = request.get_json()
    if not data:
        return jsonify({'error': 'Request body required'}), 400
    
    # Get collection from metadata
    collection = db.get_collection_by_rag_id(collection_id)
    if not collection:
        return jsonify({'error': 'Collection not found'}), 404
    
    privilege_id = data.get('privilege_id')
    if not privilege_id:
        return jsonify({'error': 'privilege_id required'}), 400
    
    if db.revoke_access(privilege_id=int(privilege_id)):
        return jsonify({'success': True})
    return jsonify({'error': 'Access rule not found'}), 404


# =====================
# Metadata API - Stats & Audit
# =====================

@rag_bp.route('/api/metadata/stats', methods=['GET'])
def api_metadata_stats():
    """Get metadata statistics"""
    db = get_metadata_db()
    return jsonify(db.get_stats())


@rag_bp.route('/api/metadata/audit', methods=['GET'])
def api_audit_log():
    """Get audit log entries"""
    db = get_metadata_db()
    
    resource_type = request.args.get('resource_type')
    resource_id = request.args.get('resource_id')
    user_id = request.args.get('user_id')
    limit = request.args.get('limit', 100, type=int)
    
    logs = db.get_audit_log(
        resource_type=resource_type,
        resource_id=resource_id,
        user_id=int(user_id) if user_id else None,
        limit=limit
    )
    
    return jsonify({'logs': [asdict(log) for log in logs]})


# =====================
# Sync Metadata with RAG Operations
# =====================

def sync_collection_to_metadata(collection_id: str, name: str, provider: str,
                                 description: str = "", owner_id: int = None,
                                 is_public: bool = False):
    """Register or update a collection in metadata DB"""
    db = get_metadata_db()
    
    existing = db.get_collection_by_rag_id(collection_id)
    if not existing:
        db.register_collection(
            collection_id=collection_id,
            name=name,
            provider=provider,
            description=description,
            owner_id=owner_id,
            is_public=is_public
        )


def sync_document_to_metadata(document_id: str, collection_id: str,
                               source: str = "", title: str = "",
                               chunk_count: int = 0, uploaded_by: int = None):
    """Register a document in metadata DB"""
    db = get_metadata_db()
    db.register_document(
        document_id=document_id,
        collection_id=collection_id,
        source=source,
        title=title,
        chunk_count=chunk_count,
        uploaded_by=uploaded_by
    )


def remove_collection_from_metadata(collection_id: str):
    """Remove a collection from metadata DB"""
    db = get_metadata_db()
    db.delete_collection(collection_id)


# =====================
# Data Source API
# =====================

@rag_bp.route('/api/data-sources', methods=['GET'])
def api_list_data_sources():
    """List all data sources"""
    from app.models.rag_metadata import DataSource
    
    try:
        sources = DataSource.query.order_by(DataSource.created_at.desc()).all()
        return jsonify({
            'success': True,
            'data_sources': [s.to_dict() for s in sources]
        })
    except Exception as e:
        return jsonify({'success': False, 'error': str(e)}), 500


@rag_bp.route('/api/data-sources', methods=['POST'])
def api_create_data_source():
    """Create a new data source"""
    from app.database import db
    from app.models.rag_metadata import DataSource
    
    data = request.get_json()
    if not data:
        return jsonify({'error': 'JSON body required'}), 400
    
    if not data.get('name'):
        return jsonify({'error': 'name is required'}), 400
    
    if not data.get('source_type'):
        return jsonify({'error': 'source_type is required'}), 400
    
    try:
        source = DataSource(
            name=data['name'],
            source_type=data['source_type'],
            description=data.get('description', ''),
            connection_string=data.get('connection_string'),
            host=data.get('host'),
            port=data.get('port'),
            database=data.get('database'),
            username=data.get('username'),
            password=data.get('password'),
            base_path=data.get('base_path'),
            file_patterns=json.dumps(data.get('file_patterns', [])),
            recursive=data.get('recursive', True),
            api_url=data.get('api_url'),
            api_method=data.get('api_method', 'GET'),
            api_headers=json.dumps(data.get('api_headers', {})) if data.get('api_headers') else None,
            api_auth_type=data.get('api_auth_type'),
            api_auth_value=data.get('api_auth_value'),
            query=data.get('query'),
            content_column=data.get('content_column'),
            title_column=data.get('title_column'),
            id_column=data.get('id_column'),
            target_collection_id=data.get('target_collection_id'),
            auto_create_collection=data.get('auto_create_collection', True),
            is_active=data.get('is_active', True)
        )
        
        if data.get('settings'):
            source.settings = data['settings']
        
        db.session.add(source)
        db.session.commit()
        
        return jsonify({
            'success': True,
            'data_source': source.to_dict(),
            'id': source.id
        }), 201
        
    except Exception as e:
        db.session.rollback()
        return jsonify({'success': False, 'error': str(e)}), 500


@rag_bp.route('/api/data-sources/<int:source_id>', methods=['GET'])
def api_get_data_source(source_id):
    """Get a specific data source"""
    from app.models.rag_metadata import DataSource
    
    source = DataSource.query.get(source_id)
    if not source:
        return jsonify({'error': 'Data source not found'}), 404
    
    return jsonify({
        'success': True,
        'data_source': source.to_dict()
    })


@rag_bp.route('/api/data-sources/<int:source_id>', methods=['PUT'])
def api_update_data_source(source_id):
    """Update a data source"""
    from app.database import db
    from app.models.rag_metadata import DataSource
    
    source = DataSource.query.get(source_id)
    if not source:
        return jsonify({'error': 'Data source not found'}), 404
    
    data = request.get_json()
    if not data:
        return jsonify({'error': 'JSON body required'}), 400
    
    try:
        # Update fields
        for field in ['name', 'description', 'source_type', 'connection_string', 
                     'host', 'port', 'database', 'username', 'password',
                     'base_path', 'recursive', 'api_url', 'api_method',
                     'api_auth_type', 'api_auth_value', 'query',
                     'content_column', 'title_column', 'id_column',
                     'target_collection_id', 'auto_create_collection', 'is_active']:
            if field in data:
                setattr(source, field, data[field])
        
        if 'file_patterns' in data:
            source.file_patterns = json.dumps(data['file_patterns'])
        
        if 'api_headers' in data:
            source.api_headers = json.dumps(data['api_headers']) if data['api_headers'] else None
        
        if 'settings' in data:
            source.settings = data['settings']
        
        db.session.commit()
        
        return jsonify({
            'success': True,
            'data_source': source.to_dict()
        })
        
    except Exception as e:
        db.session.rollback()
        return jsonify({'success': False, 'error': str(e)}), 500


@rag_bp.route('/api/data-sources/<int:source_id>', methods=['DELETE'])
def api_delete_data_source(source_id):
    """Delete a data source"""
    from app.database import db
    from app.models.rag_metadata import DataSource
    
    source = DataSource.query.get(source_id)
    if not source:
        return jsonify({'error': 'Data source not found'}), 404
    
    try:
        db.session.delete(source)
        db.session.commit()
        
        return jsonify({
            'success': True,
            'message': f'Data source {source_id} deleted'
        })
    except Exception as e:
        db.session.rollback()
        return jsonify({'success': False, 'error': str(e)}), 500


@rag_bp.route('/api/data-sources/<int:source_id>/test', methods=['POST'])
def api_test_data_source(source_id):
    """Test a data source connection"""
    from app.models.rag_metadata import DataSource
    from app.services.data_source_connectors import get_connector
    
    source = DataSource.query.get(source_id)
    if not source:
        return jsonify({'error': 'Data source not found'}), 404
    
    try:
        config = source.to_dict(include_credentials=True)
        config['password'] = source.password
        
        connector = get_connector(config)
        result = connector.test_connection()
        
        return jsonify({
            'success': result.get('success', False),
            'result': result
        })
    except Exception as e:
        return jsonify({
            'success': False,
            'result': {'success': False, 'message': str(e)}
        })


@rag_bp.route('/api/data-sources/<int:source_id>/preview', methods=['POST'])
def api_preview_data_source(source_id):
    """Preview documents from a data source (limited to 5)"""
    from app.models.rag_metadata import DataSource
    from app.services.data_source_connectors import get_connector
    
    source = DataSource.query.get(source_id)
    if not source:
        return jsonify({'error': 'Data source not found'}), 404
    
    try:
        config = source.to_dict(include_credentials=True)
        config['password'] = source.password
        
        connector = get_connector(config)
        
        preview_docs = []
        for i, doc in enumerate(connector.fetch_documents()):
            if i >= 5:  # Limit preview
                break
            preview_docs.append({
                'id': doc.get('id'),
                'title': doc.get('title'),
                'source': doc.get('source'),
                'content_preview': doc.get('content', '')[:500] + '...' if len(doc.get('content', '')) > 500 else doc.get('content', ''),
                'metadata': doc.get('metadata', {})
            })
        
        return jsonify({
            'success': True,
            'documents': preview_docs,
            'count': len(preview_docs)
        })
    except Exception as e:
        return jsonify({
            'success': False,
            'error': str(e)
        }), 500


# =====================
# Sync Jobs API
# =====================

@rag_bp.route('/api/sync-jobs', methods=['GET'])
def api_list_sync_jobs():
    """List all sync jobs"""
    from app.models.rag_metadata import SyncJob
    
    try:
        jobs = SyncJob.query.order_by(SyncJob.created_at.desc()).all()
        return jsonify({
            'success': True,
            'sync_jobs': [j.to_dict() for j in jobs]
        })
    except Exception as e:
        return jsonify({'success': False, 'error': str(e)}), 500


@rag_bp.route('/api/sync-jobs', methods=['POST'])
def api_create_sync_job():
    """Create a new sync job"""
    from app.database import db
    from app.models.rag_metadata import SyncJob
    from app.services.sync_scheduler import get_sync_scheduler
    
    data = request.get_json()
    if not data:
        return jsonify({'error': 'JSON body required'}), 400
    
    if not data.get('name'):
        return jsonify({'error': 'name is required'}), 400
    
    if not data.get('collection_id'):
        return jsonify({'error': 'collection_id is required'}), 400
    
    try:
        job = SyncJob(
            name=data['name'],
            description=data.get('description', ''),
            data_source_id=data.get('data_source_id'),
            source_type=data.get('source_type'),
            file_path=data.get('file_path'),
            file_patterns=json.dumps(data.get('file_patterns', [])),
            recursive=data.get('recursive', True),
            collection_id=data['collection_id'],
            schedule_type=data.get('schedule_type', 'manual'),
            interval_minutes=data.get('interval_minutes'),
            cron_expression=data.get('cron_expression'),
            sync_mode=data.get('sync_mode', 'incremental'),
            delete_missing=data.get('delete_missing', False),
            is_active=data.get('is_active', True)
        )
        
        if data.get('settings'):
            job.settings = data['settings']
        
        db.session.add(job)
        db.session.commit()
        
        # Schedule if not manual
        if job.schedule_type != 'manual' and job.is_active:
            scheduler = get_sync_scheduler()
            scheduler.schedule_job(job)
        
        return jsonify({
            'success': True,
            'sync_job': job.to_dict(),
            'id': job.id
        }), 201
        
    except Exception as e:
        db.session.rollback()
        return jsonify({'success': False, 'error': str(e)}), 500


@rag_bp.route('/api/sync-jobs/<int:job_id>', methods=['GET'])
def api_get_sync_job(job_id):
    """Get a specific sync job"""
    from app.models.rag_metadata import SyncJob
    from app.services.sync_scheduler import get_sync_scheduler
    
    job = SyncJob.query.get(job_id)
    if not job:
        return jsonify({'error': 'Sync job not found'}), 404
    
    # Get scheduler status
    scheduler = get_sync_scheduler()
    scheduler_status = scheduler.get_job_status(job_id)
    
    result = job.to_dict()
    result['scheduler_status'] = scheduler_status
    
    return jsonify({
        'success': True,
        'sync_job': result
    })


@rag_bp.route('/api/sync-jobs/<int:job_id>', methods=['PUT'])
def api_update_sync_job(job_id):
    """Update a sync job"""
    from app.database import db
    from app.models.rag_metadata import SyncJob
    from app.services.sync_scheduler import get_sync_scheduler
    
    job = SyncJob.query.get(job_id)
    if not job:
        return jsonify({'error': 'Sync job not found'}), 404
    
    data = request.get_json()
    if not data:
        return jsonify({'error': 'JSON body required'}), 400
    
    try:
        # Track if schedule changed
        schedule_changed = False
        old_schedule_type = job.schedule_type
        
        # Update fields
        for field in ['name', 'description', 'data_source_id', 'source_type',
                     'file_path', 'recursive', 'collection_id', 'schedule_type',
                     'interval_minutes', 'cron_expression', 'sync_mode',
                     'delete_missing', 'is_active']:
            if field in data:
                if field in ['schedule_type', 'interval_minutes', 'cron_expression', 'is_active']:
                    if getattr(job, field) != data[field]:
                        schedule_changed = True
                setattr(job, field, data[field])
        
        if 'file_patterns' in data:
            job.file_patterns = json.dumps(data['file_patterns'])
        
        if 'settings' in data:
            job.settings = data['settings']
        
        db.session.commit()
        
        # Update scheduler if needed
        if schedule_changed:
            scheduler = get_sync_scheduler()
            if job.is_active and job.schedule_type != 'manual':
                scheduler.schedule_job(job)
            else:
                scheduler.unschedule_job(job_id)
        
        return jsonify({
            'success': True,
            'sync_job': job.to_dict()
        })
        
    except Exception as e:
        db.session.rollback()
        return jsonify({'success': False, 'error': str(e)}), 500


@rag_bp.route('/api/sync-jobs/<int:job_id>', methods=['DELETE'])
def api_delete_sync_job(job_id):
    """Delete a sync job"""
    from app.database import db
    from app.models.rag_metadata import SyncJob
    from app.services.sync_scheduler import get_sync_scheduler
    
    job = SyncJob.query.get(job_id)
    if not job:
        return jsonify({'error': 'Sync job not found'}), 404
    
    try:
        # Unschedule first
        scheduler = get_sync_scheduler()
        scheduler.unschedule_job(job_id)
        
        db.session.delete(job)
        db.session.commit()
        
        return jsonify({
            'success': True,
            'message': f'Sync job {job_id} deleted'
        })
    except Exception as e:
        db.session.rollback()
        return jsonify({'success': False, 'error': str(e)}), 500


@rag_bp.route('/api/sync-jobs/<int:job_id>/run', methods=['POST'])
def api_run_sync_job(job_id):
    """Run a sync job immediately"""
    from app.models.rag_metadata import SyncJob
    from app.services.sync_scheduler import get_sync_scheduler
    
    job = SyncJob.query.get(job_id)
    if not job:
        return jsonify({'error': 'Sync job not found'}), 404
    
    try:
        scheduler = get_sync_scheduler()
        result = scheduler.run_job_now(job_id)
        
        return jsonify({
            'success': result.get('success', False),
            'result': result
        })
    except Exception as e:
        return jsonify({
            'success': False,
            'error': str(e)
        }), 500


@rag_bp.route('/api/sync-jobs/<int:job_id>/runs', methods=['GET'])
def api_get_sync_job_runs(job_id):
    """Get run history for a sync job"""
    from app.models.rag_metadata import SyncJob, SyncJobRun
    
    job = SyncJob.query.get(job_id)
    if not job:
        return jsonify({'error': 'Sync job not found'}), 404
    
    limit = request.args.get('limit', 20, type=int)
    
    runs = SyncJobRun.query.filter_by(job_id=job_id)\
        .order_by(SyncJobRun.started_at.desc())\
        .limit(limit)\
        .all()
    
    return jsonify({
        'success': True,
        'runs': [r.to_dict() for r in runs]
    })


@rag_bp.route('/api/scheduler/status', methods=['GET'])
def api_scheduler_status():
    """Get scheduler status"""
    from app.services.sync_scheduler import get_sync_scheduler
    
    scheduler = get_sync_scheduler()
    
    return jsonify({
        'available': scheduler.is_available,
        'running': scheduler.is_running,
        'scheduled_jobs': scheduler.get_all_jobs() if scheduler.is_running else []
    })


# =====================
# Web UI Routes for Data Sources and Sync Jobs
# =====================

@rag_bp.route('/data-sources')
def data_sources_page():
    """Data sources management page"""
    return render_template('rag/data_sources.html')


@rag_bp.route('/sync-jobs')
def sync_jobs_page():
    """Sync jobs management page"""
    return render_template('rag/sync_jobs.html')


