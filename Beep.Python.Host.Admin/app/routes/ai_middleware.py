"""
AI Services Middleware API

Unified API for accessing all AI services (LLM, Text-to-Image, TTS, STT, etc.)
Enables service chaining and integration.

Middleware Server Manager:
- Request routing based on keywords/patterns
- Access control for services
- Request filtering and modification
- Service chaining and scheduling
"""
from flask import Blueprint, request, jsonify, render_template
from app.services.ai_service_orchestrator import get_orchestrator, ServiceType
from app.services.middleware_server_manager import (
    get_middleware_server_manager,
    RoutingRule, AccessPolicy, RuleType, ActionType
)
from app.services.api_token_manager import get_api_token_manager
from datetime import datetime
import logging
import uuid

logger = logging.getLogger(__name__)

ai_middleware_bp = Blueprint('ai_middleware', __name__)


@ai_middleware_bp.route('/docs')
def api_docs():
    """API documentation page"""
    return render_template('ai_middleware/api_docs.html')


@ai_middleware_bp.route('/manage')
def api_manage_services():
    """Service management page"""
    from app.services.middleware_config import MiddlewareConfig
    
    service_states = MiddlewareConfig.get_all_service_states()
    
    return render_template('ai_middleware/manage.html', service_states=service_states)


@ai_middleware_bp.route('/rules')
def rules_page():
    """Routing rules management page"""
    return render_template('ai_middleware/rules.html')


@ai_middleware_bp.route('/policies')
def policies_page():
    """Access policies management page"""
    return render_template('ai_middleware/policies.html')


@ai_middleware_bp.route('/api/services', methods=['GET'])
def api_list_services():
    """List all available services"""
    orchestrator = get_orchestrator()
    services = orchestrator.list_available_services()
    
    return jsonify({
        'success': True,
        'services': services,
        'service_info': {
            'llm': {
                'name': 'LLM',
                'description': 'Large Language Models for text generation and chat',
                'methods': ['generate', 'chat']
            },
            'text_to_image': {
                'name': 'Text to Image',
                'description': 'Generate images from text prompts',
                'methods': ['generate_image']
            },
            'text_to_speech': {
                'name': 'Text to Speech',
                'description': 'Convert text to speech',
                'methods': ['generate_speech']
            },
            'speech_to_text': {
                'name': 'Speech to Text',
                'description': 'Transcribe audio to text',
                'methods': ['transcribe_audio']
            },
            'voice_to_voice': {
                'name': 'Voice to Voice',
                'description': 'Voice conversion and cloning',
                'methods': ['convert_voice']
            },
            'document_extraction': {
                'name': 'Document Extraction',
                'description': 'Extract text from documents',
                'methods': ['extract']
            }
        }
    })


@ai_middleware_bp.route('/api/services/status', methods=['GET'])
def api_get_all_status():
    """Get status of all services"""
    from app.services.middleware_config import MiddlewareConfig
    
    orchestrator = get_orchestrator()
    status = orchestrator.get_service_status()
    service_states = MiddlewareConfig.get_all_service_states()
    
    return jsonify({
        'success': True,
        'status': status,
        'service_states': service_states
    })


@ai_middleware_bp.route('/api/services/<service_name>/enable', methods=['POST'])
def api_enable_service(service_name: str):
    """Enable a service"""
    from app.services.middleware_config import MiddlewareConfig
    
    data = request.get_json() or {}
    enabled = data.get('enabled', True)
    
    success = MiddlewareConfig.set_service_enabled(service_name, enabled)
    
    return jsonify({
        'success': success,
        'service': service_name,
        'enabled': enabled
    })


@ai_middleware_bp.route('/api/services/states', methods=['GET'])
def api_get_service_states():
    """Get enable/disable states for all services"""
    from app.services.middleware_config import MiddlewareConfig
    
    states = MiddlewareConfig.get_all_service_states()
    
    return jsonify({
        'success': True,
        'states': states
    })


@ai_middleware_bp.route('/api/services/states', methods=['POST'])
def api_set_service_states():
    """Set enable/disable states for multiple services"""
    from app.services.middleware_config import MiddlewareConfig
    
    data = request.get_json() or {}
    states = data.get('states', {})
    
    results = MiddlewareConfig.set_all_service_states(states)
    
    return jsonify({
        'success': True,
        'results': results
    })


@ai_middleware_bp.route('/api/services/<service_type>/<method>', methods=['POST'])
def api_call_service(service_type: str, method: str):
    """
    Call a service method through middleware
    
    POST /api/services/llm/generate
    POST /api/services/text_to_image/generate_image
    etc.
    
    This endpoint now goes through the middleware for routing, access control, and filtering.
    """
    try:
        data = request.get_json() or {}
        
        # Extract request text from various possible fields
        request_text = (
            data.get('request_text') or
            data.get('prompt') or
            data.get('query') or
            (data.get('messages', [{}])[-1].get('content', '') if data.get('messages') else '') or
            ''
        )
        
        # Get user info from request (could be from auth token, session, etc.)
        user_id = data.get('user_id') or request.headers.get('X-User-ID')
        user_role = data.get('user_role') or request.headers.get('X-User-Role')
        
        # Use middleware to process the request
        manager = get_middleware_server_manager()
        result = manager.process_request(
            request_text=request_text,
            service_type=service_type,
            user_id=user_id,
            user_role=user_role,
            method=method,
            **{k: v for k, v in data.items() if k not in ['request_text', 'user_id', 'user_role']}
        )
        
        if result.get('success'):
            return jsonify(result)
        else:
            status_code = 403 if result.get('blocked') else 500
            return jsonify(result), status_code
            
    except Exception as e:
        logger.error(f"Error in service call: {e}", exc_info=True)
        return jsonify({
            'success': False,
            'error': str(e)
        }), 500


@ai_middleware_bp.route('/api/services/chain', methods=['POST'])
def api_chain_services():
    """
    Chain multiple services together
    
    Example request:
    {
        "chain": [
            {
                "service": "llm",
                "method": "generate",
                "args": {
                    "prompt": "Describe a beautiful sunset"
                }
            },
            {
                "service": "text_to_image",
                "method": "generate_image",
                "args": {
                    "prompt": "{step_0.text}",
                    "width": 512,
                    "height": 512
                }
            }
        ]
    }
    """
    try:
        data = request.get_json() or {}
        chain = data.get('chain', [])
        
        if not chain:
            return jsonify({
                'success': False,
                'error': 'Chain is required'
            }), 400
        
        orchestrator = get_orchestrator()
        result = orchestrator.chain_services(chain)
        
        if result.get('success'):
            return jsonify(result)
        else:
            return jsonify(result), 500
            
    except Exception as e:
        logger.error(f"Error in service chain: {e}", exc_info=True)
        return jsonify({
            'success': False,
            'error': str(e)
        }), 500


@ai_middleware_bp.route('/api/services/llm-with-image', methods=['POST'])
def api_llm_with_image():
    """
    LLM chat that can generate images when requested
    
    If LLM response contains image generation request, automatically generate it.
    """
    try:
        data = request.get_json() or {}
        messages = data.get('messages', [])
        model_id = data.get('model_id')
        auto_generate_images = data.get('auto_generate_images', True)
        
        orchestrator = get_orchestrator()
        
        # First, get LLM response
        llm_result = orchestrator.call_service(
            ServiceType.LLM,
            'chat',
            messages=messages,
            model_id=model_id,
            config=data.get('config', {})
        )
        
        if not llm_result.get('success'):
            return jsonify(llm_result), 500
        
        llm_text = llm_result.get('message', '')
        
        # Check if LLM response contains image generation request
        image_prompt = None
        if auto_generate_images:
            # Simple detection: look for phrases like "generate image", "create image", etc.
            import re
            image_patterns = [
                r'generate\s+(?:an\s+)?image\s+(?:of|with|showing)\s+(.+)',
                r'create\s+(?:an\s+)?image\s+(?:of|with|showing)\s+(.+)',
                r'draw\s+(?:an\s+)?image\s+(?:of|with|showing)\s+(.+)',
                r'image\s+(?:of|with|showing)\s+(.+)'
            ]
            
            for pattern in image_patterns:
                match = re.search(pattern, llm_text, re.IGNORECASE)
                if match:
                    image_prompt = match.group(1).strip()
                    break
        
        result = {
            'success': True,
            'llm_response': llm_text,
            'image_generated': False
        }
        
        # Generate image if requested
        if image_prompt:
            image_result = orchestrator.call_service(
                ServiceType.TEXT_TO_IMAGE,
                'generate_image',
                prompt=image_prompt,
                width=512,
                height=512,
                num_inference_steps=50
            )
            
            if image_result.get('success'):
                result['image_generated'] = True
                result['image'] = image_result.get('image')
                result['image_metadata'] = image_result.get('metadata')
        
        return jsonify(result)
        
    except Exception as e:
        logger.error(f"Error in LLM with image: {e}", exc_info=True)
        return jsonify({
            'success': False,
            'error': str(e)
        }), 500


@ai_middleware_bp.route('/api/services/llm-with-tts', methods=['POST'])
def api_llm_with_tts():
    """
    LLM chat that automatically converts response to speech
    """
    try:
        data = request.get_json() or {}
        messages = data.get('messages', [])
        model_id = data.get('model_id')
        generate_speech = data.get('generate_speech', True)
        voice = data.get('voice', 'default')
        speed = data.get('speed', 1.0)
        
        orchestrator = get_orchestrator()
        
        # Get LLM response
        llm_result = orchestrator.call_service(
            ServiceType.LLM,
            'chat',
            messages=messages,
            model_id=model_id,
            config=data.get('config', {})
        )
        
        if not llm_result.get('success'):
            return jsonify(llm_result), 500
        
        llm_text = llm_result.get('message', '')
        
        result = {
            'success': True,
            'llm_response': llm_text,
            'speech_generated': False
        }
        
        # Generate speech if requested
        if generate_speech and llm_text:
            tts_result = orchestrator.call_service(
                ServiceType.TEXT_TO_SPEECH,
                'generate_speech',
                text=llm_text,
                voice=voice,
                speed=speed,
                engine='edge-tts'
            )
            
            if tts_result.get('success'):
                result['speech_generated'] = True
                result['audio'] = tts_result.get('audio')
                result['audio_format'] = tts_result.get('format', 'mp3')
                result['audio_metadata'] = tts_result.get('metadata')
        
        return jsonify(result)
        
    except Exception as e:
        logger.error(f"Error in LLM with TTS: {e}", exc_info=True)
        return jsonify({
            'success': False,
            'error': str(e)
        }), 500


@ai_middleware_bp.route('/api/services/voice-chat', methods=['POST'])
def api_voice_chat():
    """
    Voice-based chat: Speech-to-Text -> LLM -> Text-to-Speech
    
    Input: Audio file
    Output: Audio response
    """
    try:
        if 'audio' not in request.files:
            return jsonify({
                'success': False,
                'error': 'Audio file is required'
            }), 400
        
        audio_file = request.files['audio']
        if not audio_file or not audio_file.filename:
            return jsonify({
                'success': False,
                'error': 'Invalid audio file'
            }), 400
        
        data = request.form.to_dict()
        model_id = data.get('model_id')
        language = data.get('language', 'en')
        voice = data.get('voice', 'default')
        speed = float(data.get('speed', 1.0))
        
        orchestrator = get_orchestrator()
        
        # Step 1: Transcribe audio
        audio_content = audio_file.read()
        stt_result = orchestrator.call_service(
            ServiceType.SPEECH_TO_TEXT,
            'transcribe_audio',
            audio_content=audio_content,
            language=language,
            model_size='base'
        )
        
        if not stt_result.get('success'):
            return jsonify(stt_result), 500
        
        transcribed_text = stt_result.get('text', '')
        
        # Step 2: Get LLM response
        llm_result = orchestrator.call_service(
            ServiceType.LLM,
            'chat',
            messages=[{'role': 'user', 'content': transcribed_text}],
            model_id=model_id,
            config={}
        )
        
        if not llm_result.get('success'):
            return jsonify(llm_result), 500
        
        llm_response = llm_result.get('message', '')
        
        # Step 3: Convert to speech
        tts_result = orchestrator.call_service(
            ServiceType.TEXT_TO_SPEECH,
            'generate_speech',
            text=llm_response,
            voice=voice,
            speed=speed,
            engine='edge-tts'
        )
        
        if not tts_result.get('success'):
            return jsonify(tts_result), 500
        
        return jsonify({
            'success': True,
            'transcribed_text': transcribed_text,
            'llm_response': llm_response,
            'audio': tts_result.get('audio'),
            'audio_format': tts_result.get('format', 'mp3'),
            'metadata': {
                'language': stt_result.get('language'),
                'voice': voice
            }
        })
        
    except Exception as e:
        logger.error(f"Error in voice chat: {e}", exc_info=True)
        return jsonify({
            'success': False,
            'error': str(e)
        }), 500


# =============================================================================
# Middleware Server Manager API - Developer Interface
# =============================================================================

@ai_middleware_bp.route('/api/middleware/status', methods=['GET'])
def api_middleware_status():
    """Get middleware server status"""
    try:
        manager = get_middleware_server_manager()
        status = manager.get_status()
        return jsonify({
            'success': True,
            'status': status
        })
    except Exception as e:
        logger.error(f"Error getting middleware status: {e}", exc_info=True)
        return jsonify({
            'success': False,
            'error': str(e)
        }), 500


@ai_middleware_bp.route('/api/middleware/rules', methods=['GET'])
def api_list_rules():
    """List all routing rules"""
    try:
        manager = get_middleware_server_manager()
        enabled_only = request.args.get('enabled_only', 'false').lower() == 'true'
        rules = manager.get_rules(enabled_only=enabled_only)
        return jsonify({
            'success': True,
            'rules': [r.to_dict() for r in rules]
        })
    except Exception as e:
        logger.error(f"Error listing rules: {e}", exc_info=True)
        return jsonify({
            'success': False,
            'error': str(e)
        }), 500


@ai_middleware_bp.route('/api/middleware/rules', methods=['POST'])
def api_create_rule():
    """Create a new routing rule"""
    try:
        data = request.get_json() or {}
        
        # Validate required fields
        required = ['name', 'rule_type', 'pattern', 'action']
        for field in required:
            if field not in data:
                return jsonify({
                    'success': False,
                    'error': f'Missing required field: {field}'
                }), 400
        
        # Create rule
        rule = RoutingRule(
            id=data.get('id', str(uuid.uuid4())),
            name=data['name'],
            description=data.get('description', ''),
            rule_type=RuleType(data['rule_type']),
            pattern=data['pattern'],
            action=ActionType(data['action']),
            target_service=data.get('target_service'),
            target_api=data.get('target_api'),
            priority=data.get('priority', 0),
            enabled=data.get('enabled', True),
            conditions=data.get('conditions', {})
        )
        
        manager = get_middleware_server_manager()
        success = manager.add_rule(rule)
        
        if success:
            return jsonify({
                'success': True,
                'rule': rule.to_dict()
            })
        else:
            return jsonify({
                'success': False,
                'error': 'Failed to create rule'
            }), 500
            
    except ValueError as e:
        return jsonify({
            'success': False,
            'error': f'Invalid enum value: {str(e)}'
        }), 400
    except Exception as e:
        logger.error(f"Error creating rule: {e}", exc_info=True)
        return jsonify({
            'success': False,
            'error': str(e)
        }), 500


@ai_middleware_bp.route('/api/middleware/rules/<rule_id>', methods=['PUT'])
def api_update_rule(rule_id: str):
    """Update a routing rule"""
    try:
        data = request.get_json() or {}
        manager = get_middleware_server_manager()
        
        # Get existing rule
        rules = manager.get_rules()
        existing = next((r for r in rules if r.id == rule_id), None)
        
        if not existing:
            return jsonify({
                'success': False,
                'error': 'Rule not found'
            }), 404
        
        # Update fields
        if 'name' in data:
            existing.name = data['name']
        if 'description' in data:
            existing.description = data['description']
        if 'rule_type' in data:
            existing.rule_type = RuleType(data['rule_type'])
        if 'pattern' in data:
            existing.pattern = data['pattern']
        if 'action' in data:
            existing.action = ActionType(data['action'])
        if 'target_service' in data:
            existing.target_service = data['target_service']
        if 'target_api' in data:
            existing.target_api = data['target_api']
        if 'priority' in data:
            existing.priority = data['priority']
        if 'enabled' in data:
            existing.enabled = data['enabled']
        if 'conditions' in data:
            existing.conditions = data['conditions']
        
        existing.updated_at = datetime.now().isoformat()
        
        success = manager.add_rule(existing)  # add_rule handles updates too
        
        if success:
            return jsonify({
                'success': True,
                'rule': existing.to_dict()
            })
        else:
            return jsonify({
                'success': False,
                'error': 'Failed to update rule'
            }), 500
            
    except ValueError as e:
        return jsonify({
            'success': False,
            'error': f'Invalid enum value: {str(e)}'
        }), 400
    except Exception as e:
        logger.error(f"Error updating rule: {e}", exc_info=True)
        return jsonify({
            'success': False,
            'error': str(e)
        }), 500


@ai_middleware_bp.route('/api/middleware/rules/<rule_id>', methods=['DELETE'])
def api_delete_rule(rule_id: str):
    """Delete a routing rule"""
    try:
        manager = get_middleware_server_manager()
        success = manager.remove_rule(rule_id)
        
        if success:
            return jsonify({
                'success': True,
                'message': 'Rule deleted'
            })
        else:
            return jsonify({
                'success': False,
                'error': 'Failed to delete rule'
            }), 500
            
    except Exception as e:
        logger.error(f"Error deleting rule: {e}", exc_info=True)
        return jsonify({
            'success': False,
            'error': str(e)
        }), 500


@ai_middleware_bp.route('/api/middleware/policies', methods=['GET'])
def api_list_policies():
    """List all access policies"""
    try:
        manager = get_middleware_server_manager()
        service = request.args.get('service')
        policies = manager.get_policies(service=service)
        return jsonify({
            'success': True,
            'policies': [p.to_dict() for p in policies]
        })
    except Exception as e:
        logger.error(f"Error listing policies: {e}", exc_info=True)
        return jsonify({
            'success': False,
            'error': str(e)
        }), 500


@ai_middleware_bp.route('/api/middleware/policies', methods=['POST'])
def api_create_policy():
    """Create a new access policy"""
    try:
        data = request.get_json() or {}
        
        # Validate required fields
        required = ['name', 'service']
        for field in required:
            if field not in data:
                return jsonify({
                    'success': False,
                    'error': f'Missing required field: {field}'
                }), 400
        
        # Create policy
        policy = AccessPolicy(
            id=data.get('id', str(uuid.uuid4())),
            name=data['name'],
            description=data.get('description', ''),
            service=data['service'],
            resource=data.get('resource'),
            user_id=data.get('user_id'),
            user_role=data.get('user_role'),
            allowed=data.get('allowed', True),
            conditions=data.get('conditions', {})
        )
        
        manager = get_middleware_server_manager()
        success = manager.add_policy(policy)
        
        if success:
            return jsonify({
                'success': True,
                'policy': policy.to_dict()
            })
        else:
            return jsonify({
                'success': False,
                'error': 'Failed to create policy'
            }), 500
            
    except Exception as e:
        logger.error(f"Error creating policy: {e}", exc_info=True)
        return jsonify({
            'success': False,
            'error': str(e)
        }), 500


@ai_middleware_bp.route('/api/middleware/policies/<policy_id>', methods=['PUT'])
def api_update_policy(policy_id: str):
    """Update an access policy"""
    try:
        data = request.get_json() or {}
        manager = get_middleware_server_manager()
        
        # Get existing policy
        policies = manager.get_policies()
        existing = next((p for p in policies if p.id == policy_id), None)
        
        if not existing:
            return jsonify({
                'success': False,
                'error': 'Policy not found'
            }), 404
        
        # Update fields
        if 'name' in data:
            existing.name = data['name']
        if 'description' in data:
            existing.description = data['description']
        if 'service' in data:
            existing.service = data['service']
        if 'resource' in data:
            existing.resource = data['resource']
        if 'user_id' in data:
            existing.user_id = data['user_id']
        if 'user_role' in data:
            existing.user_role = data['user_role']
        if 'allowed' in data:
            existing.allowed = data['allowed']
        if 'conditions' in data:
            existing.conditions = data['conditions']
        
        existing.updated_at = datetime.now().isoformat()
        
        success = manager.add_policy(existing)  # add_policy handles updates too
        
        if success:
            return jsonify({
                'success': True,
                'policy': existing.to_dict()
            })
        else:
            return jsonify({
                'success': False,
                'error': 'Failed to update policy'
            }), 500
            
    except Exception as e:
        logger.error(f"Error updating policy: {e}", exc_info=True)
        return jsonify({
            'success': False,
            'error': str(e)
        }), 500


@ai_middleware_bp.route('/api/middleware/policies/<policy_id>', methods=['DELETE'])
def api_delete_policy(policy_id: str):
    """Delete an access policy"""
    try:
        manager = get_middleware_server_manager()
        success = manager.remove_policy(policy_id)
        
        if success:
            return jsonify({
                'success': True,
                'message': 'Policy deleted'
            })
        else:
            return jsonify({
                'success': False,
                'error': 'Failed to delete policy'
            }), 500
            
    except Exception as e:
        logger.error(f"Error deleting policy: {e}", exc_info=True)
        return jsonify({
            'success': False,
            'error': str(e)
        }), 500


@ai_middleware_bp.route('/api/middleware/process', methods=['POST'])
def api_process_request():
    """
    Process a request through the middleware
    
    This is the main endpoint for developers to send requests.
    The middleware will handle routing, access control, and filtering.
    
    Example request:
    {
        "request_text": "What is the weather today?",
        "service_type": "llm",
        "user_id": "user123",
        "user_role": "user",
        "messages": [{"role": "user", "content": "What is the weather?"}]
    }
    """
    try:
        data = request.get_json() or {}
        
        request_text = data.get('request_text', '')
        service_type = data.get('service_type', 'llm')
        user_id = data.get('user_id')
        user_role = data.get('user_role')
        
        if not request_text:
            return jsonify({
                'success': False,
                'error': 'request_text is required'
            }), 400
        
        manager = get_middleware_server_manager()
        result = manager.process_request(
            request_text=request_text,
            service_type=service_type,
            user_id=user_id,
            user_role=user_role,
            **{k: v for k, v in data.items() if k not in ['request_text', 'service_type', 'user_id', 'user_role']}
        )
        
        return jsonify(result)
        
    except Exception as e:
        logger.error(f"Error processing request: {e}", exc_info=True)
        return jsonify({
            'success': False,
            'error': str(e)
        }), 500


@ai_middleware_bp.route('/api/middleware/check-access', methods=['POST'])
def api_check_access():
    """Check if a user has access to a service/resource"""
    try:
        data = request.get_json() or {}
        
        service = data.get('service')
        user_id = data.get('user_id')
        user_role = data.get('user_role')
        resource = data.get('resource')
        
        if not service:
            return jsonify({
                'success': False,
                'error': 'service is required'
            }), 400
        
        manager = get_middleware_server_manager()
        allowed, reason = manager.check_access(
            service=service,
            user_id=user_id,
            user_role=user_role,
            resource=resource
        )
        
        return jsonify({
            'success': True,
            'allowed': allowed,
            'reason': reason
        })
        
    except Exception as e:
        logger.error(f"Error checking access: {e}", exc_info=True)
        return jsonify({
            'success': False,
            'error': str(e)
        }), 500


# =============================================================================
# High-Level Service Operations - Direct Methods
# =============================================================================

@ai_middleware_bp.route('/api/middleware/chat', methods=['POST'])
def api_chat():
    """
    Chat with LLM through middleware
    
    Example:
    {
        "messages": [{"role": "user", "content": "Hello"}],
        "model_id": "llama-2-7b",
        "user_id": "user123",
        "user_role": "user"
    }
    """
    try:
        data = request.get_json() or {}
        messages = data.get('messages', [])
        model_id = data.get('model_id')
        user_id = data.get('user_id')
        user_role = data.get('user_role')
        
        if not messages:
            return jsonify({
                'success': False,
                'error': 'messages is required'
            }), 400
        
        manager = get_middleware_server_manager()
        result = manager.chat(
            messages=messages,
            model_id=model_id,
            user_id=user_id,
            user_role=user_role,
            **{k: v for k, v in data.items() if k not in ['messages', 'model_id', 'user_id', 'user_role']}
        )
        
        return jsonify(result)
        
    except Exception as e:
        logger.error(f"Error in chat: {e}", exc_info=True)
        return jsonify({
            'success': False,
            'error': str(e)
        }), 500


@ai_middleware_bp.route('/api/middleware/rag/query', methods=['POST'])
def api_rag_query():
    """
    Query RAG through middleware
    
    Example:
    {
        "query": "What is machine learning?",
        "collection_id": "docs",
        "user_id": "user123",
        "user_role": "user",
        "max_results": 5
    }
    """
    try:
        data = request.get_json() or {}
        query = data.get('query')
        collection_id = data.get('collection_id')
        user_id = data.get('user_id')
        user_role = data.get('user_role')
        max_results = data.get('max_results', 5)
        
        if not query:
            return jsonify({
                'success': False,
                'error': 'query is required'
            }), 400
        
        manager = get_middleware_server_manager()
        result = manager.rag_query(
            query=query,
            collection_id=collection_id,
            user_id=user_id,
            user_role=user_role,
            max_results=max_results,
            **{k: v for k, v in data.items() if k not in ['query', 'collection_id', 'user_id', 'user_role', 'max_results']}
        )
        
        return jsonify(result)
        
    except Exception as e:
        logger.error(f"Error in RAG query: {e}", exc_info=True)
        return jsonify({
            'success': False,
            'error': str(e)
        }), 500


@ai_middleware_bp.route('/api/middleware/rag/documents', methods=['POST'])
def api_rag_add_documents():
    """
    Add documents to RAG collection through middleware
    
    Example:
    {
        "documents": [
            {"content": "Document text", "source": "file.pdf", "metadata": {}}
        ],
        "collection_id": "docs",
        "user_id": "user123",
        "user_role": "user"
    }
    """
    try:
        data = request.get_json() or {}
        documents = data.get('documents', [])
        collection_id = data.get('collection_id')
        user_id = data.get('user_id')
        user_role = data.get('user_role')
        
        if not documents:
            return jsonify({
                'success': False,
                'error': 'documents is required'
            }), 400
        
        if not collection_id:
            return jsonify({
                'success': False,
                'error': 'collection_id is required'
            }), 400
        
        manager = get_middleware_server_manager()
        result = manager.rag_add_documents(
            documents=documents,
            collection_id=collection_id,
            user_id=user_id,
            user_role=user_role
        )
        
        return jsonify(result)
        
    except Exception as e:
        logger.error(f"Error adding RAG documents: {e}", exc_info=True)
        return jsonify({
            'success': False,
            'error': str(e)
        }), 500


@ai_middleware_bp.route('/api/middleware/rag/documents', methods=['DELETE'])
def api_rag_remove_documents():
    """
    Remove documents from RAG collection through middleware
    
    Example:
    {
        "document_ids": ["doc1", "doc2"],
        "collection_id": "docs",
        "user_id": "user123",
        "user_role": "user"
    }
    """
    try:
        data = request.get_json() or {}
        document_ids = data.get('document_ids', [])
        collection_id = data.get('collection_id')
        user_id = data.get('user_id')
        user_role = data.get('user_role')
        
        if not document_ids:
            return jsonify({
                'success': False,
                'error': 'document_ids is required'
            }), 400
        
        if not collection_id:
            return jsonify({
                'success': False,
                'error': 'collection_id is required'
            }), 400
        
        manager = get_middleware_server_manager()
        result = manager.rag_remove_documents(
            document_ids=document_ids,
            collection_id=collection_id,
            user_id=user_id,
            user_role=user_role
        )
        
        return jsonify(result)
        
    except Exception as e:
        logger.error(f"Error removing RAG documents: {e}", exc_info=True)
        return jsonify({
            'success': False,
            'error': str(e)
        }), 500


@ai_middleware_bp.route('/api/middleware/extract', methods=['POST'])
def api_extract_document():
    """
    Extract text from document through middleware
    
    Example:
    POST with multipart/form-data:
    - file: document file
    - user_id: user123
    - user_role: user
    """
    try:
        if 'file' not in request.files:
            return jsonify({
                'success': False,
                'error': 'file is required'
            }), 400
        
        file = request.files['file']
        if not file or not file.filename:
            return jsonify({
                'success': False,
                'error': 'Invalid file'
            }), 400
        
        user_id = request.form.get('user_id')
        user_role = request.form.get('user_role')
        
        file_content = file.read()
        filename = file.filename
        
        manager = get_middleware_server_manager()
        result = manager.extract_document(
            file_content=file_content,
            filename=filename,
            user_id=user_id,
            user_role=user_role
        )
        
        return jsonify(result)
        
    except Exception as e:
        logger.error(f"Error extracting document: {e}", exc_info=True)
        return jsonify({
            'success': False,
            'error': str(e)
        }), 500


@ai_middleware_bp.route('/api/middleware/jobs', methods=['POST'])
def api_create_job():
    """
    Create a scheduled job through middleware
    
    Example:
    {
        "job_name": "Daily RAG Sync",
        "job_type": "rag",
        "schedule": "0 0 * * *",
        "function_name": "sync_rag_collection",
        "function_args": {"collection_id": "docs"},
        "user_id": "user123",
        "user_role": "admin"
    }
    """
    try:
        data = request.get_json() or {}
        
        required = ['job_name', 'job_type', 'schedule', 'function_name']
        for field in required:
            if field not in data:
                return jsonify({
                    'success': False,
                    'error': f'{field} is required'
                }), 400
        
        manager = get_middleware_server_manager()
        result = manager.create_job(
            job_name=data['job_name'],
            job_type=data['job_type'],
            schedule=data['schedule'],
            function_name=data['function_name'],
            function_args=data.get('function_args', {}),
            user_id=data.get('user_id'),
            user_role=data.get('user_role')
        )
        
        return jsonify(result)
        
    except Exception as e:
        logger.error(f"Error creating job: {e}", exc_info=True)
        return jsonify({
            'success': False,
            'error': str(e)
        }), 500


@ai_middleware_bp.route('/api/middleware/jobs/<job_id>', methods=['GET'])
def api_check_job(job_id: str):
    """
    Check job status through middleware
    
    Example:
    GET /api/middleware/jobs/123?user_id=user123&user_role=admin
    """
    try:
        user_id = request.args.get('user_id')
        user_role = request.args.get('user_role')
        
        manager = get_middleware_server_manager()
        result = manager.check_job(
            job_id=job_id,
            user_id=user_id,
            user_role=user_role
        )
        
        return jsonify(result)
        
    except Exception as e:
        logger.error(f"Error checking job: {e}", exc_info=True)
        return jsonify({
            'success': False,
            'error': str(e)
        }), 500


# =============================================================================
# User Management & API Tokens
# =============================================================================

@ai_middleware_bp.route('/users')
def users_page():
    """User management page"""
    from app.models.core import User, Role
    from app.database import db
    
    users = User.query.all()
    roles = Role.query.all()
    
    return render_template('ai_middleware/users.html', users=users, roles=roles)


@ai_middleware_bp.route('/api/users', methods=['GET'])
def api_list_users():
    """List all users"""
    try:
        from app.models.core import User
        from app.database import db
        
        users = User.query.all()
        return jsonify({
            'success': True,
            'users': [user.to_dict() for user in users]
        })
    except Exception as e:
        logger.error(f"Error listing users: {e}", exc_info=True)
        return jsonify({
            'success': False,
            'error': str(e)
        }), 500


@ai_middleware_bp.route('/api/users', methods=['POST'])
def api_create_user():
    """Create a new user"""
    try:
        from app.models.core import User, Role
        from app.database import db
        import hashlib
        
        data = request.get_json() or {}
        
        required = ['username']
        for field in required:
            if field not in data:
                return jsonify({
                    'success': False,
                    'error': f'{field} is required'
                }), 400
        
        # Check if username exists
        if User.query.filter_by(username=data['username']).first():
            return jsonify({
                'success': False,
                'error': 'Username already exists'
            }), 400
        
        # Create user
        user = User(
            username=data['username'],
            email=data.get('email'),
            display_name=data.get('display_name', data['username']),
            is_admin=data.get('is_admin', False),
            is_active=data.get('is_active', True)
        )
        
        # Set password if provided
        if data.get('password'):
            password_hash = hashlib.sha256(data['password'].encode()).hexdigest()
            user.password_hash = password_hash
        
        # Set role if provided
        if data.get('role_id'):
            role = db.session.get(Role, data['role_id'])
            if role:
                user.role_id = role.id
        
        db.session.add(user)
        db.session.commit()
        
        return jsonify({
            'success': True,
            'user': user.to_dict()
        })
        
    except Exception as e:
        logger.error(f"Error creating user: {e}", exc_info=True)
        db.session.rollback()
        return jsonify({
            'success': False,
            'error': str(e)
        }), 500


@ai_middleware_bp.route('/api/users/<user_id>', methods=['PUT'])
def api_update_user(user_id: int):
    """Update a user"""
    try:
        from app.models.core import User, Role
        from app.database import db
        import hashlib
        
        user = db.session.get(User, user_id)
        if not user:
            return jsonify({
                'success': False,
                'error': 'User not found'
            }), 404
        
        data = request.get_json() or {}
        
        # Update fields
        if 'email' in data:
            user.email = data['email']
        if 'display_name' in data:
            user.display_name = data['display_name']
        if 'is_admin' in data:
            user.is_admin = data['is_admin']
        if 'is_active' in data:
            user.is_active = data['is_active']
        if 'password' in data and data['password']:
            password_hash = hashlib.sha256(data['password'].encode()).hexdigest()
            user.password_hash = password_hash
        if 'role_id' in data:
            if data['role_id']:
                role = db.session.get(Role, data['role_id'])
                if role:
                    user.role_id = role.id
            else:
                user.role_id = None
        
        db.session.commit()
        
        return jsonify({
            'success': True,
            'user': user.to_dict()
        })
        
    except Exception as e:
        logger.error(f"Error updating user: {e}", exc_info=True)
        db.session.rollback()
        return jsonify({
            'success': False,
            'error': str(e)
        }), 500


@ai_middleware_bp.route('/api/users/<user_id>', methods=['DELETE'])
def api_delete_user(user_id: int):
    """Delete a user"""
    try:
        from app.models.core import User
        from app.database import db
        
        user = db.session.get(User, user_id)
        if not user:
            return jsonify({
                'success': False,
                'error': 'User not found'
            }), 404
        
        # Revoke all tokens first
        token_mgr = get_api_token_manager()
        token_mgr.revoke_all_user_tokens(user_id)
        
        db.session.delete(user)
        db.session.commit()
        
        return jsonify({
            'success': True,
            'message': 'User deleted successfully'
        })
        
    except Exception as e:
        logger.error(f"Error deleting user: {e}", exc_info=True)
        db.session.rollback()
        return jsonify({
            'success': False,
            'error': str(e)
        }), 500


@ai_middleware_bp.route('/api/users/<user_id>/tokens', methods=['GET'])
def api_list_user_tokens(user_id: int):
    """List all API tokens for a user"""
    try:
        token_mgr = get_api_token_manager()
        tokens = token_mgr.get_user_tokens(user_id)
        
        return jsonify({
            'success': True,
            'tokens': [token.to_dict(include_token=False) for token in tokens]
        })
    except Exception as e:
        logger.error(f"Error listing tokens: {e}", exc_info=True)
        return jsonify({
            'success': False,
            'error': str(e)
        }), 500


@ai_middleware_bp.route('/api/users/<user_id>/tokens', methods=['POST'])
def api_create_token(user_id: int):
    """Create a new API token for a user"""
    try:
        data = request.get_json() or {}
        
        name = data.get('name')
        expires_in_days = data.get('expires_in_days')
        scopes = data.get('scopes', [])
        
        token_mgr = get_api_token_manager()
        result = token_mgr.create_token(
            user_id=user_id,
            name=name,
            expires_in_days=expires_in_days,
            scopes=scopes
        )
        
        return jsonify({
            'success': True,
            'token': result['token'],  # Plain token (only shown once!)
            'token_record': result['token_record'].to_dict(include_token=False),
            'user': result['user'].to_dict(),
            'warning': 'Save this token now! It will not be shown again.'
        })
        
    except ValueError as e:
        return jsonify({
            'success': False,
            'error': str(e)
        }), 400
    except Exception as e:
        logger.error(f"Error creating token: {e}", exc_info=True)
        return jsonify({
            'success': False,
            'error': str(e)
        }), 500


@ai_middleware_bp.route('/api/tokens/<token_id>', methods=['DELETE'])
def api_revoke_token(token_id: int):
    """Revoke an API token"""
    try:
        token_mgr = get_api_token_manager()
        success = token_mgr.revoke_token(token_id)
        
        if success:
            return jsonify({
                'success': True,
                'message': 'Token revoked successfully'
            })
        else:
            return jsonify({
                'success': False,
                'error': 'Token not found'
            }), 404
            
    except Exception as e:
        logger.error(f"Error revoking token: {e}", exc_info=True)
        return jsonify({
            'success': False,
            'error': str(e)
        }), 500


@ai_middleware_bp.route('/api/tokens/validate', methods=['POST'])
def api_validate_token():
    """Validate an API token"""
    try:
        data = request.get_json() or {}
        token = data.get('token')
        
        if not token:
            return jsonify({
                'success': False,
                'error': 'token is required'
            }), 400
        
        token_mgr = get_api_token_manager()
        user_info = token_mgr.validate_token(token)
        
        if user_info:
            return jsonify({
                'success': True,
                'valid': True,
                'user': user_info
            })
        else:
            return jsonify({
                'success': True,
                'valid': False,
                'error': 'Invalid or expired token'
            })
            
    except Exception as e:
        logger.error(f"Error validating token: {e}", exc_info=True)
        return jsonify({
            'success': False,
            'error': str(e)
        }), 500
