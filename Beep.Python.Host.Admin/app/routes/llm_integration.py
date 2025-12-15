"""
LLM Integration Routes

Routes for integrating LLM with other AI services (Text-to-Image, TTS, STT, etc.)
"""
from flask import Blueprint, request, jsonify
from app.services.ai_service_orchestrator import get_orchestrator, ServiceType
from app.services.inference_service import InferenceService
import logging

logger = logging.getLogger(__name__)

llm_integration_bp = Blueprint('llm_integration', __name__)


@llm_integration_bp.route('/api/chat/enhanced', methods=['POST'])
def api_enhanced_chat():
    """
    Enhanced LLM chat with integrated AI services
    
    Supports:
    - Automatic image generation when LLM requests it
    - Automatic TTS for responses
    - Voice input (STT)
    - Service chaining
    
    Request body:
    {
        "messages": [...],
        "model_id": "...",
        "options": {
            "auto_generate_images": true,
            "auto_generate_speech": false,
            "voice": "default",
            "speed": 1.0
        }
    }
    """
    try:
        from app.services.inference_service import InferenceService
        
        data = request.get_json() or {}
        messages = data.get('messages', [])
        model_id = data.get('model_id')
        options = data.get('options', {})
        
        auto_images = options.get('auto_generate_images', False)
        auto_speech = options.get('auto_generate_speech', False)
        voice = options.get('voice', 'default')
        speed = options.get('speed', 1.0)
        
        orchestrator = get_orchestrator()
        inference = InferenceService()
        
        # Get LLM response
        if not model_id:
            loaded = inference.get_loaded_models()
            if not loaded:
                return jsonify({
                    'success': False,
                    'error': 'No model loaded'
                }), 400
            model_id = loaded[0].get('id') or loaded[0].get('name')
        
        # Use inference service directly for chat
        config = data.get('config', {})
        config.update({
            'temperature': options.get('temperature', 0.7),
            'max_tokens': options.get('max_tokens', 2048)
        })
        
        try:
            llm_response = inference.chat(
                model_id=model_id,
                messages=messages,
                max_tokens=config.get('max_tokens'),
                temperature=config.get('temperature'),
                stream=False
            )
            llm_text = llm_response.get('message', '') or llm_response.get('text', '')
        except Exception as e:
            logger.error(f"LLM chat error: {e}", exc_info=True)
            return jsonify({
                'success': False,
                'error': f'LLM error: {str(e)}'
            }), 500
        
        result = {
            'success': True,
            'message': llm_text,
            'model_id': model_id,
            'images': [],
            'audio': None
        }
        
        # Check for image generation requests
        if auto_images:
            image_prompts = _extract_image_requests(llm_text)
            for img_prompt in image_prompts:
                img_result = orchestrator.call_service(
                    ServiceType.TEXT_TO_IMAGE,
                    'generate_image',
                    prompt=img_prompt,
                    width=512,
                    height=512,
                    num_inference_steps=50
                )
                if img_result.get('success'):
                    result['images'].append({
                        'image': img_result.get('image'),
                        'prompt': img_prompt,
                        'metadata': img_result.get('metadata')
                    })
        
        # Generate speech if requested
        if auto_speech and llm_text:
            tts_result = orchestrator.call_service(
                ServiceType.TEXT_TO_SPEECH,
                'generate_speech',
                text=llm_text,
                voice=voice,
                speed=speed,
                engine='edge-tts'
            )
            if tts_result.get('success'):
                result['audio'] = tts_result.get('audio')
                result['audio_format'] = tts_result.get('format', 'mp3')
        
        return jsonify(result)
        
    except Exception as e:
        logger.error(f"Error in enhanced chat: {e}", exc_info=True)
        return jsonify({
            'success': False,
            'error': str(e)
        }), 500


@llm_integration_bp.route('/api/chat/voice', methods=['POST'])
def api_voice_chat():
    """
    Voice-based chat: Audio input -> STT -> LLM -> TTS -> Audio output
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
        conversation_history = data.get('conversation_history', '[]')
        
        try:
            import json
            history = json.loads(conversation_history) if conversation_history else []
        except:
            history = []
        
        orchestrator = get_orchestrator()
        
        # Step 1: Transcribe
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
        
        # Step 2: LLM chat
        messages = history + [{'role': 'user', 'content': transcribed_text}]
        
        llm_result = orchestrator.call_service(
            ServiceType.LLM,
            'chat',
            messages=messages,
            model_id=model_id,
            config={}
        )
        
        if not llm_result.get('success'):
            return jsonify(llm_result), 500
        
        llm_response = llm_result.get('message', '')
        
        # Step 3: TTS
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


def _extract_image_requests(text: str) -> list:
    """Extract image generation requests from text"""
    import re
    
    patterns = [
        r'generate\s+(?:an\s+)?image\s+(?:of|with|showing|depicting)\s+(.+?)(?:\.|$|,)',
        r'create\s+(?:an\s+)?image\s+(?:of|with|showing|depicting)\s+(.+?)(?:\.|$|,)',
        r'draw\s+(?:an\s+)?image\s+(?:of|with|showing|depicting)\s+(.+?)(?:\.|$|,)',
        r'image\s+(?:of|with|showing|depicting)\s+(.+?)(?:\.|$|,)',
        r'picture\s+(?:of|with|showing|depicting)\s+(.+?)(?:\.|$|,)'
    ]
    
    prompts = []
    for pattern in patterns:
        matches = re.finditer(pattern, text, re.IGNORECASE)
        for match in matches:
            prompt = match.group(1).strip()
            if prompt and len(prompt) > 5:  # Minimum prompt length
                prompts.append(prompt)
    
    return prompts


@llm_integration_bp.route('/api/services/status', methods=['GET'])
def api_services_status():
    """Get status of all integrated services"""
    orchestrator = get_orchestrator()
    status = orchestrator.get_service_status()
    
    return jsonify({
        'success': True,
        'services': status,
        'available_services': orchestrator.list_available_services()
    })
