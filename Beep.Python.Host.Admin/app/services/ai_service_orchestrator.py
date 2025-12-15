"""
AI Service Orchestrator

Unified middleware for orchestrating all AI services (LLM, Text-to-Image, TTS, STT, etc.)
Enables service chaining and integration.
"""
import logging
from typing import Dict, Any, Optional, List, Callable
from enum import Enum

logger = logging.getLogger(__name__)


class ServiceType(Enum):
    """Available service types"""
    LLM = "llm"
    TEXT_TO_IMAGE = "text_to_image"
    TEXT_TO_SPEECH = "text_to_speech"
    SPEECH_TO_TEXT = "speech_to_text"
    VOICE_TO_VOICE = "voice_to_voice"
    OBJECT_DETECTION = "object_detection"
    TABULAR_TIME_SERIES = "tabular_time_series"
    DOCUMENT_EXTRACTION = "document_extraction"
    RAG = "rag"


class AIServiceOrchestrator:
    """
    Orchestrates multiple AI services together.
    Enables service chaining and unified API access.
    """
    
    def __init__(self):
        self._services = {}
        self._initialize_services()
    
    def _initialize_services(self):
        """Initialize all available services"""
        try:
            from app.services.text_to_image_service import get_text_to_image_service
            self._services[ServiceType.TEXT_TO_IMAGE] = get_text_to_image_service()
        except Exception as e:
            logger.warning(f"Text-to-Image service not available: {e}")
        
        try:
            from app.services.text_to_speech_service import get_text_to_speech_service
            self._services[ServiceType.TEXT_TO_SPEECH] = get_text_to_speech_service()
        except Exception as e:
            logger.warning(f"Text-to-Speech service not available: {e}")
        
        try:
            from app.services.speech_to_text_service import get_speech_to_text_service
            self._services[ServiceType.SPEECH_TO_TEXT] = get_speech_to_text_service()
        except Exception as e:
            logger.warning(f"Speech-to-Text service not available: {e}")
        
        try:
            from app.services.voice_to_voice_service import get_voice_to_voice_service
            self._services[ServiceType.VOICE_TO_VOICE] = get_voice_to_voice_service()
        except Exception as e:
            logger.warning(f"Voice-to-Voice service not available: {e}")
        
        try:
            from app.services.object_detection_service import get_object_detection_service
            self._services[ServiceType.OBJECT_DETECTION] = get_object_detection_service()
        except Exception as e:
            logger.warning(f"Object Detection service not available: {e}")
        
        try:
            from app.services.tabular_time_series_service import get_tabular_time_series_service
            self._services[ServiceType.TABULAR_TIME_SERIES] = get_tabular_time_series_service()
        except Exception as e:
            logger.warning(f"Tabular & Time Series service not available: {e}")
        
        try:
            from app.services.llm_manager import LLMManager
            from app.services.inference_service import InferenceService
            self._services[ServiceType.LLM] = {
                'manager': LLMManager(),
                'inference': InferenceService()
            }
        except Exception as e:
            logger.warning(f"LLM service not available: {e}")
        
        try:
            from app.services.document_extractor import get_document_extractor
            self._services[ServiceType.DOCUMENT_EXTRACTION] = get_document_extractor()
        except Exception as e:
            logger.warning(f"Document Extraction service not available: {e}")
    
    def call_service(self, service_type: ServiceType, method: str, **kwargs) -> Dict[str, Any]:
        """
        Call a service method
        
        Args:
            service_type: Type of service to call
            method: Method name to call
            **kwargs: Arguments to pass to the method
        
        Returns:
            Service response dict
        """
        # Check if service is enabled
        from app.services.middleware_config import MiddlewareConfig
        if not MiddlewareConfig.is_service_enabled(service_type.value):
            return {
                'success': False,
                'error': f'Service {service_type.value} is disabled'
            }
        
        if service_type not in self._services:
            return {
                'success': False,
                'error': f'Service {service_type.value} not available'
            }
        
        service = self._services[service_type]
        
        try:
            if service_type == ServiceType.LLM:
                # LLM has manager and inference
                if method == 'generate':
                    return self._llm_generate(**kwargs)
                elif method == 'chat':
                    return self._llm_chat(**kwargs)
                else:
                    return {'success': False, 'error': f'Unknown LLM method: {method}'}
            else:
                # Other services have direct methods
                if hasattr(service, method):
                    func = getattr(service, method)
                    return func(**kwargs)
                else:
                    return {
                        'success': False,
                        'error': f'Method {method} not found on {service_type.value}'
                    }
        except Exception as e:
            logger.error(f"Error calling {service_type.value}.{method}: {e}", exc_info=True)
            return {
                'success': False,
                'error': str(e)
            }
    
    def _llm_generate(self, prompt: str, model_id: Optional[str] = None, **kwargs) -> Dict[str, Any]:
        """Generate text using LLM"""
        try:
            llm_data = self._services[ServiceType.LLM]
            inference = llm_data['inference']
            
            # Get or load model
            if model_id:
                # Load specific model
                models = llm_data['manager'].get_local_models()
                model = next((m for m in models if m.get('id') == model_id or m.get('name') == model_id), None)
                if not model:
                    return {'success': False, 'error': f'Model {model_id} not found'}
            else:
                # Use currently loaded model or default
                loaded = inference.get_loaded_models()
                if not loaded:
                    return {'success': False, 'error': 'No model loaded. Please load a model first.'}
                model = loaded[0]
            
            # Generate
            config = kwargs.get('config', {})
            result = inference.generate(
                model_id=model.get('id') or model.get('name'),
                prompt=prompt,
                config=config
            )
            
            return {
                'success': True,
                'text': result.get('text', ''),
                'metadata': result.get('metadata', {})
            }
        except Exception as e:
            logger.error(f"LLM generation error: {e}", exc_info=True)
            return {'success': False, 'error': str(e)}
    
    def _llm_chat(self, messages: List[Dict], model_id: Optional[str] = None, **kwargs) -> Dict[str, Any]:
        """Chat with LLM"""
        try:
            llm_data = self._services[ServiceType.LLM]
            inference = llm_data['inference']
            
            # Use model_id directly or get from loaded models
            if not model_id:
                loaded = inference.get_loaded_models()
                if not loaded:
                    return {'success': False, 'error': 'No model loaded'}
                model_id = loaded[0].get('id') or loaded[0].get('name')
            
            config = kwargs.get('config', {})
            result = inference.chat(
                model_id=model_id,
                messages=messages,
                max_tokens=config.get('max_tokens'),
                temperature=config.get('temperature'),
                stream=False
            )
            
            return {
                'success': True,
                'message': result.get('message', '') or result.get('text', ''),
                'metadata': result.get('metadata', {})
            }
        except Exception as e:
            logger.error(f"LLM chat error: {e}", exc_info=True)
            return {'success': False, 'error': str(e)}
    
    def chain_services(self, chain: List[Dict[str, Any]]) -> Dict[str, Any]:
        """
        Chain multiple services together
        
        Args:
            chain: List of service calls, each with 'service', 'method', and 'args'
                  Can reference previous results with {step_N.field}
        
        Returns:
            Combined results from all steps
        """
        results = []
        
        for i, step in enumerate(chain):
            service_type_str = step.get('service')
            method = step.get('method')
            args = step.get('args', {})
            
            try:
                service_type = ServiceType(service_type_str)
            except ValueError:
                return {
                    'success': False,
                    'error': f'Invalid service type: {service_type_str}',
                    'step': i
                }
            
            # Replace placeholders with previous results
            args_str = str(args)
            for j in range(i):
                result_key = f'step_{j}'
                if result_key in args_str:
                    prev_result = results[j]
                    # Simple placeholder replacement
                    import json
                    args_json = json.dumps(args)
                    for key, value in prev_result.items():
                        placeholder = f'{{step_{j}.{key}}}'
                        if placeholder in args_json:
                            if isinstance(value, (dict, list)):
                                args_json = args_json.replace(placeholder, json.dumps(value))
                            else:
                                args_json = args_json.replace(placeholder, json.dumps(str(value)))
                    args = json.loads(args_json)
            
            # Call service
            result = self.call_service(service_type, method, **args)
            results.append(result)
            
            # Stop chain on error
            if not result.get('success'):
                return {
                    'success': False,
                    'error': f'Chain failed at step {i}: {result.get("error")}',
                    'step': i,
                    'results': results
                }
        
        return {
            'success': True,
            'results': results,
            'final_result': results[-1] if results else None
        }
    
    def get_service_status(self, service_type: Optional[ServiceType] = None) -> Dict[str, Any]:
        """Get status of service(s)"""
        if service_type:
            if service_type in self._services:
                service = self._services[service_type]
                if hasattr(service, 'get_status'):
                    return service.get_status()
                return {'available': True}
            return {'available': False}
        
        # Get all services status
        status = {}
        for svc_type in ServiceType:
            status[svc_type.value] = self.get_service_status(svc_type)
        
        return status
    
    def list_available_services(self) -> List[str]:
        """List all available services"""
        return [svc_type.value for svc_type in self._services.keys()]


# Singleton instance
_orchestrator_instance = None


def get_orchestrator() -> AIServiceOrchestrator:
    """Get singleton instance of orchestrator"""
    global _orchestrator_instance
    if _orchestrator_instance is None:
        _orchestrator_instance = AIServiceOrchestrator()
    return _orchestrator_instance
