"""
OpenAI-Compatible Chat API Routes

Provides standard OpenAI API endpoints for LLM chat:
- POST /v1/chat/completions - Chat completions (streaming and non-streaming)
- POST /v1/completions - Text completions
- GET /v1/models - List available models
- GET /v1/models/{model_id} - Get model info

These endpoints follow the OpenAI API specification for compatibility
with existing tools and libraries (langchain, openai-python, etc.)
"""
import json
import time
import uuid
import threading
from datetime import datetime
from typing import Optional, List, Dict, Any, Generator
from flask import Blueprint, request, jsonify, Response, stream_with_context

from app.services.inference_service import InferenceService, InferenceConfig, ChatMessage
from app.services.rag_service import RAGService
from app.services.llm_manager import LLMManager


# Create blueprint
openai_bp = Blueprint('openai', __name__, url_prefix='/v1')


def get_inference_service() -> InferenceService:
    return InferenceService()


def get_rag_service() -> RAGService:
    return RAGService()


def get_llm_manager() -> LLMManager:
    return LLMManager()


def generate_id(prefix: str = "chatcmpl") -> str:
    """Generate OpenAI-style ID"""
    return f"{prefix}-{uuid.uuid4().hex[:24]}"


def get_unix_timestamp() -> int:
    """Get current Unix timestamp"""
    return int(time.time())


def extract_auth_info(req) -> Dict[str, Any]:
    """Extract authentication info from request headers"""
    auth_header = req.headers.get('Authorization', '')
    api_key = None
    
    if auth_header.startswith('Bearer '):
        api_key = auth_header[7:]
    
    return {
        'api_key': api_key,
        'user_id': req.headers.get('X-User-ID'),
        'session_id': req.headers.get('X-Session-ID'),
        'collection_ids': req.headers.get('X-RAG-Collections', '').split(',') if req.headers.get('X-RAG-Collections') else None
    }


# =====================
# Models Endpoints
# =====================

@openai_bp.route('/models', methods=['GET'])
def list_models():
    """
    List available models (OpenAI-compatible)
    
    GET /v1/models
    
    Returns:
        {
            "object": "list",
            "data": [
                {
                    "id": "model-id",
                    "object": "model",
                    "created": 1234567890,
                    "owned_by": "local"
                }
            ]
        }
    """
    llm_manager = get_llm_manager()
    inference = get_inference_service()
    
    models = llm_manager.list_local_models()
    loaded_model_ids = list(inference._loaded_models.keys())
    
    model_list = []
    for model in models:
        model_list.append({
            "id": model.id,
            "object": "model",
            "created": int(datetime.fromisoformat(model.downloaded_at).timestamp()) if model.downloaded_at else 0,
            "owned_by": "local",
            "permission": [],
            "root": model.id,
            "parent": None,
            # Extra info (non-standard)
            "_local": {
                "name": model.name,
                "size_bytes": model.size_bytes,
                "quantization": model.quantization,
                "loaded": model.id in loaded_model_ids,
                "path": model.path
            }
        })
    
    return jsonify({
        "object": "list",
        "data": model_list
    })


@openai_bp.route('/models/<path:model_id>', methods=['GET'])
def get_model(model_id: str):
    """
    Get model details (OpenAI-compatible)
    
    GET /v1/models/{model_id}
    """
    llm_manager = get_llm_manager()
    model = llm_manager.get_model_by_id(model_id)
    
    if not model:
        return jsonify({
            "error": {
                "message": f"Model '{model_id}' not found",
                "type": "invalid_request_error",
                "param": "model",
                "code": "model_not_found"
            }
        }), 404
    
    return jsonify({
        "id": model.id,
        "object": "model",
        "created": int(datetime.fromisoformat(model.downloaded_at).timestamp()) if model.downloaded_at else 0,
        "owned_by": "local",
        "permission": [],
        "root": model.id,
        "parent": None
    })


# =====================
# Chat Completions
# =====================

@openai_bp.route('/chat/completions', methods=['POST'])
def chat_completions():
    """
    Create chat completion (OpenAI-compatible)
    
    POST /v1/chat/completions
    
    Request body:
        {
            "model": "model-id",
            "messages": [
                {"role": "system", "content": "..."},
                {"role": "user", "content": "..."}
            ],
            "temperature": 0.7,
            "max_tokens": 2048,
            "stream": false,
            "top_p": 0.95,
            "stop": ["\\n"],
            
            // RAG options (custom extension)
            "rag_enabled": true,
            "rag_collections": ["collection-id"]
        }
    
    Returns:
        {
            "id": "chatcmpl-...",
            "object": "chat.completion",
            "created": 1234567890,
            "model": "model-id",
            "choices": [
                {
                    "index": 0,
                    "message": {
                        "role": "assistant",
                        "content": "..."
                    },
                    "finish_reason": "stop"
                }
            ],
            "usage": {
                "prompt_tokens": 10,
                "completion_tokens": 20,
                "total_tokens": 30
            }
        }
    """
    inference = get_inference_service()
    rag_service = get_rag_service()
    
    # Check if inference is available
    if not inference.is_available():
        return jsonify({
            "error": {
                "message": "LLM inference not available. Install llama-cpp-python.",
                "type": "service_unavailable",
                "code": "inference_unavailable"
            }
        }), 503
    
    # Parse request
    data = request.get_json()
    if not data:
        return jsonify({
            "error": {
                "message": "Request body is required",
                "type": "invalid_request_error",
                "code": "missing_body"
            }
        }), 400
    
    model_id = data.get('model')
    messages = data.get('messages', [])
    stream = data.get('stream', False)
    
    if not model_id:
        return jsonify({
            "error": {
                "message": "model is required",
                "type": "invalid_request_error",
                "param": "model",
                "code": "missing_model"
            }
        }), 400
    
    if not messages:
        return jsonify({
            "error": {
                "message": "messages is required",
                "type": "invalid_request_error",
                "param": "messages",
                "code": "missing_messages"
            }
        }), 400
    
    # Extract auth info
    auth_info = extract_auth_info(request)
    
    # RAG augmentation
    rag_enabled = data.get('rag_enabled', True)  # Enable by default if configured
    rag_collections = data.get('rag_collections') or auth_info.get('collection_ids')
    
    if rag_enabled and rag_service.is_enabled():
        # Check authorization if user_id provided
        user_id = auth_info.get('user_id')
        if user_id:
            auth_result = rag_service.check_authorization(
                user_id=user_id,
                action='query',
                resource_type='collection'
            )
            if not auth_result.get('allowed', False):
                # RAG denied, continue without context
                pass
            else:
                # Augment messages with RAG context
                messages = rag_service.augment_messages(
                    messages=messages,
                    user_id=user_id,
                    collection_ids=rag_collections
                )
        else:
            # No auth required, try to augment
            messages = rag_service.augment_messages(
                messages=messages,
                collection_ids=rag_collections
            )
    
    # Load model if not already loaded
    try:
        # Build config from request
        config = InferenceConfig(
            temperature=data.get('temperature', 0.7),
            top_p=data.get('top_p', 0.95),
            max_tokens=data.get('max_tokens', 2048),
            stop_sequences=data.get('stop', [])
        )
        
        # Merge with model-specific settings if provided
        if data.get('n_ctx'):
            config.n_ctx = data['n_ctx']
        if data.get('n_gpu_layers') is not None:
            config.n_gpu_layers = data['n_gpu_layers']
        
        loaded = inference.load_model(model_id, config)
        
    except ValueError as e:
        return jsonify({
            "error": {
                "message": str(e),
                "type": "invalid_request_error",
                "param": "model",
                "code": "model_not_found"
            }
        }), 404
    except Exception as e:
        return jsonify({
            "error": {
                "message": f"Failed to load model: {e}",
                "type": "server_error",
                "code": "model_load_failed"
            }
        }), 500
    
    # Convert messages to ChatMessage format
    chat_messages = [
        ChatMessage(role=m['role'], content=m['content'])
        for m in messages
    ]
    
    # Generate response
    request_id = generate_id()
    created = get_unix_timestamp()
    
    if stream:
        # Streaming response
        return Response(
            stream_with_context(stream_chat_response(
                inference, loaded, chat_messages, config, request_id, model_id, created
            )),
            mimetype='text/event-stream',
            headers={
                'Cache-Control': 'no-cache',
                'Connection': 'keep-alive',
                'X-Accel-Buffering': 'no'
            }
        )
    else:
        # Non-streaming response
        try:
            result = inference.chat_completion(
                model_id=model_id,
                messages=chat_messages,
                temperature=config.temperature,
                top_p=config.top_p,
                max_tokens=config.max_tokens,
                stop=config.stop_sequences
            )
            
            return jsonify({
                "id": request_id,
                "object": "chat.completion",
                "created": created,
                "model": model_id,
                "choices": [
                    {
                        "index": 0,
                        "message": {
                            "role": "assistant",
                            "content": result.get('content', '')
                        },
                        "finish_reason": result.get('finish_reason', 'stop')
                    }
                ],
                "usage": {
                    "prompt_tokens": result.get('prompt_tokens', 0),
                    "completion_tokens": result.get('completion_tokens', 0),
                    "total_tokens": result.get('total_tokens', 0)
                }
            })
            
        except Exception as e:
            return jsonify({
                "error": {
                    "message": str(e),
                    "type": "server_error",
                    "code": "inference_error"
                }
            }), 500


def stream_chat_response(inference: InferenceService, 
                        loaded_model,
                        messages: List[ChatMessage],
                        config: InferenceConfig,
                        request_id: str,
                        model_id: str,
                        created: int) -> Generator[str, None, None]:
    """
    Generate streaming chat response in SSE format
    """
    try:
        # Use streaming inference
        for chunk in inference.chat_completion_stream(
            model_id=model_id,
            messages=messages,
            temperature=config.temperature,
            top_p=config.top_p,
            max_tokens=config.max_tokens,
            stop=config.stop_sequences
        ):
            if chunk.get('content'):
                data = {
                    "id": request_id,
                    "object": "chat.completion.chunk",
                    "created": created,
                    "model": model_id,
                    "choices": [
                        {
                            "index": 0,
                            "delta": {
                                "content": chunk['content']
                            },
                            "finish_reason": None
                        }
                    ]
                }
                yield f"data: {json.dumps(data)}\n\n"
        
        # Send final chunk with finish_reason
        final_data = {
            "id": request_id,
            "object": "chat.completion.chunk",
            "created": created,
            "model": model_id,
            "choices": [
                {
                    "index": 0,
                    "delta": {},
                    "finish_reason": "stop"
                }
            ]
        }
        yield f"data: {json.dumps(final_data)}\n\n"
        yield "data: [DONE]\n\n"
        
    except Exception as e:
        error_data = {
            "error": {
                "message": str(e),
                "type": "server_error",
                "code": "stream_error"
            }
        }
        yield f"data: {json.dumps(error_data)}\n\n"


# =====================
# Text Completions (Legacy)
# =====================

@openai_bp.route('/completions', methods=['POST'])
def completions():
    """
    Create text completion (OpenAI-compatible, legacy endpoint)
    
    POST /v1/completions
    
    Request body:
        {
            "model": "model-id",
            "prompt": "...",
            "max_tokens": 2048,
            "temperature": 0.7,
            "stream": false
        }
    """
    inference = get_inference_service()
    
    if not inference.is_available():
        return jsonify({
            "error": {
                "message": "LLM inference not available",
                "type": "service_unavailable",
                "code": "inference_unavailable"
            }
        }), 503
    
    data = request.get_json()
    if not data:
        return jsonify({"error": {"message": "Request body required"}}), 400
    
    model_id = data.get('model')
    prompt = data.get('prompt', '')
    stream = data.get('stream', False)
    
    if not model_id:
        return jsonify({"error": {"message": "model is required"}}), 400
    
    try:
        config = InferenceConfig(
            temperature=data.get('temperature', 0.7),
            top_p=data.get('top_p', 0.95),
            max_tokens=data.get('max_tokens', 2048),
            stop_sequences=data.get('stop', [])
        )
        
        inference.load_model(model_id, config)
        
        request_id = generate_id("cmpl")
        created = get_unix_timestamp()
        
        if stream:
            return Response(
                stream_with_context(stream_completion_response(
                    inference, model_id, prompt, config, request_id, created
                )),
                mimetype='text/event-stream'
            )
        else:
            result = inference.text_completion(
                model_id=model_id,
                prompt=prompt,
                temperature=config.temperature,
                top_p=config.top_p,
                max_tokens=config.max_tokens,
                stop=config.stop_sequences
            )
            
            return jsonify({
                "id": request_id,
                "object": "text_completion",
                "created": created,
                "model": model_id,
                "choices": [
                    {
                        "text": result.get('text', ''),
                        "index": 0,
                        "logprobs": None,
                        "finish_reason": result.get('finish_reason', 'stop')
                    }
                ],
                "usage": {
                    "prompt_tokens": result.get('prompt_tokens', 0),
                    "completion_tokens": result.get('completion_tokens', 0),
                    "total_tokens": result.get('total_tokens', 0)
                }
            })
            
    except Exception as e:
        return jsonify({"error": {"message": str(e)}}), 500


def stream_completion_response(inference: InferenceService,
                               model_id: str,
                               prompt: str,
                               config: InferenceConfig,
                               request_id: str,
                               created: int) -> Generator[str, None, None]:
    """Generate streaming text completion response"""
    try:
        for chunk in inference.text_completion_stream(
            model_id=model_id,
            prompt=prompt,
            temperature=config.temperature,
            top_p=config.top_p,
            max_tokens=config.max_tokens,
            stop=config.stop_sequences
        ):
            if chunk.get('text'):
                data = {
                    "id": request_id,
                    "object": "text_completion",
                    "created": created,
                    "model": model_id,
                    "choices": [
                        {
                            "text": chunk['text'],
                            "index": 0,
                            "finish_reason": None
                        }
                    ]
                }
                yield f"data: {json.dumps(data)}\n\n"
        
        yield "data: [DONE]\n\n"
        
    except Exception as e:
        yield f"data: {json.dumps({'error': str(e)})}\n\n"


# =====================
# Health Check
# =====================

@openai_bp.route('/health', methods=['GET'])
def health():
    """Health check endpoint"""
    inference = get_inference_service()
    rag_service = get_rag_service()
    
    return jsonify({
        "status": "healthy",
        "inference_available": inference.is_available(),
        "loaded_models": len(inference._loaded_models),
        "rag_enabled": rag_service.is_enabled(),
        "timestamp": datetime.now().isoformat()
    })
