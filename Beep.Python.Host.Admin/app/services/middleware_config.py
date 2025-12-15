"""
Middleware Configuration

Manages enable/disable state for AI services in the middleware.
"""
import logging
from typing import Dict, Optional
from flask import has_app_context
from app.models.core import Setting

logger = logging.getLogger(__name__)


class MiddlewareConfig:
    """Manages middleware service configuration"""
    
    # Default service states
    DEFAULT_STATES = {
        'llm': True,
        'text_to_image': True,
        'text_to_speech': True,
        'speech_to_text': True,
        'voice_to_voice': True,
        'document_extraction': True,
        'rag': True
    }
    
    @classmethod
    def is_service_enabled(cls, service_name: str) -> bool:
        """Check if a service is enabled"""
        if not has_app_context():
            # Default to enabled if no app context
            return cls.DEFAULT_STATES.get(service_name, True)
        
        try:
            key = f'middleware_service_{service_name}_enabled'
            value = Setting.get(key, None)
            if value is None:
                # Use default
                return cls.DEFAULT_STATES.get(service_name, True)
            return value.lower() in ('true', '1', 'yes', 'on')
        except Exception as e:
            logger.warning(f"Error checking service state for {service_name}: {e}")
            return cls.DEFAULT_STATES.get(service_name, True)
    
    @classmethod
    def set_service_enabled(cls, service_name: str, enabled: bool) -> bool:
        """Enable or disable a service"""
        if not has_app_context():
            logger.warning("Cannot save service state: no app context")
            return False
        
        try:
            key = f'middleware_service_{service_name}_enabled'
            Setting.set(key, 'true' if enabled else 'false',
                       f'Enable/disable {service_name} service in middleware')
            return True
        except Exception as e:
            logger.error(f"Error setting service state for {service_name}: {e}")
            return False
    
    @classmethod
    def get_all_service_states(cls) -> Dict[str, bool]:
        """Get enabled/disabled state for all services"""
        states = {}
        for service_name in cls.DEFAULT_STATES.keys():
            states[service_name] = cls.is_service_enabled(service_name)
        return states
    
    @classmethod
    def set_all_service_states(cls, states: Dict[str, bool]) -> Dict[str, bool]:
        """Set enabled/disabled state for multiple services"""
        results = {}
        for service_name, enabled in states.items():
            results[service_name] = cls.set_service_enabled(service_name, enabled)
        return results
