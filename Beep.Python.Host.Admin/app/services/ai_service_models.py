"""
AI Service Model Configuration

Manages model selection for different AI services.
"""
import logging
from typing import Dict, List, Optional
from flask import has_app_context
from app.models.core import Setting

logger = logging.getLogger(__name__)


# Available models for each service type
AVAILABLE_MODELS = {
    'text_to_image': [
        {
            'id': 'runwayml/stable-diffusion-v1-5',
            'name': 'Stable Diffusion v1.5',
            'description': 'Standard Stable Diffusion model (default)',
            'default': True
        },
        {
            'id': 'stabilityai/stable-diffusion-2-1',
            'name': 'Stable Diffusion 2.1',
            'description': 'Improved version with better quality',
            'default': False
        },
        {
            'id': 'stabilityai/stable-diffusion-xl-base-1.0',
            'name': 'Stable Diffusion XL',
            'description': 'High resolution model (1024x1024)',
            'default': False
        }
    ],
    'speech_to_text': [
        {
            'id': 'tiny',
            'name': 'Whisper Tiny',
            'description': 'Fastest, smallest model (~39M params)',
            'default': False
        },
        {
            'id': 'base',
            'name': 'Whisper Base',
            'description': 'Good balance of speed and accuracy (~74M params)',
            'default': True
        },
        {
            'id': 'small',
            'name': 'Whisper Small',
            'description': 'Better accuracy (~244M params)',
            'default': False
        },
        {
            'id': 'medium',
            'name': 'Whisper Medium',
            'description': 'High accuracy (~769M params)',
            'default': False
        },
        {
            'id': 'large',
            'name': 'Whisper Large',
            'description': 'Best accuracy (~1550M params)',
            'default': False
        }
    ],
    'text_to_speech': [
        {
            'id': 'edge-tts',
            'name': 'Edge TTS (Microsoft)',
            'description': 'High quality, multiple voices, free',
            'default': True
        },
        {
            'id': 'gtts',
            'name': 'Google TTS',
            'description': 'Requires internet, multiple languages',
            'default': False
        },
        {
            'id': 'pyttsx3',
            'name': 'pyttsx3 (Offline)',
            'description': 'Offline, uses system voices',
            'default': False
        }
    ]
}


class AIServiceModelConfig:
    """Manages model selection for AI services"""
    
    @classmethod
    def get_available_models(cls, service_type: str) -> List[Dict]:
        """Get list of available models for a service"""
        return AVAILABLE_MODELS.get(service_type, [])
    
    @classmethod
    def get_selected_model(cls, service_type: str) -> Optional[str]:
        """Get currently selected model for a service"""
        if not has_app_context():
            # Return default model
            models = AVAILABLE_MODELS.get(service_type, [])
            default = next((m for m in models if m.get('default')), None)
            return default.get('id') if default else None
        
        try:
            key = f'ai_service_{service_type}_model'
            model_id = Setting.get(key, None)
            if model_id:
                return model_id
            
            # Return default if no setting
            models = AVAILABLE_MODELS.get(service_type, [])
            default = next((m for m in models if m.get('default')), None)
            return default.get('id') if default else None
        except Exception as e:
            logger.warning(f"Error getting model for {service_type}: {e}")
            models = AVAILABLE_MODELS.get(service_type, [])
            default = next((m for m in models if m.get('default')), None)
            return default.get('id') if default else None
    
    @classmethod
    def set_selected_model(cls, service_type: str, model_id: str) -> bool:
        """Set selected model for a service"""
        if not has_app_context():
            logger.warning("Cannot save model selection: no app context")
            return False
        
        try:
            # Validate model exists
            models = AVAILABLE_MODELS.get(service_type, [])
            if not any(m['id'] == model_id for m in models):
                logger.error(f"Invalid model {model_id} for {service_type}")
                return False
            
            key = f'ai_service_{service_type}_model'
            Setting.set(key, model_id, f'Selected model for {service_type} service')
            return True
        except Exception as e:
            logger.error(f"Error setting model for {service_type}: {e}")
            return False
    
    @classmethod
    def get_model_info(cls, service_type: str, model_id: str) -> Optional[Dict]:
        """Get information about a specific model"""
        models = AVAILABLE_MODELS.get(service_type, [])
        return next((m for m in models if m['id'] == model_id), None)
