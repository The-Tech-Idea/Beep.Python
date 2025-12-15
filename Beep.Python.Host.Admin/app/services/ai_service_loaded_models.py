"""
AI Service Loaded Models Tracker

Tracks which model is currently loaded/running for each AI service type.
Only one model per service type can be loaded at a time.
"""
import threading
from typing import Dict, Optional
from datetime import datetime
from dataclasses import dataclass, field
import logging

logger = logging.getLogger(__name__)


@dataclass
class LoadedAIServiceModel:
    """Information about a loaded AI service model"""
    service_type: str
    model_id: str
    loaded_at: str
    last_used: str = field(default_factory=lambda: datetime.now().isoformat())
    
    def to_dict(self) -> dict:
        return {
            'service_type': self.service_type,
            'model_id': self.model_id,
            'loaded_at': self.loaded_at,
            'last_used': self.last_used
        }


class AIServiceLoadedModelsTracker:
    """
    Singleton tracker for loaded AI service models.
    Only one model per service type can be loaded at a time.
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
        # Dict mapping service_type -> LoadedAIServiceModel
        self._loaded_models: Dict[str, LoadedAIServiceModel] = {}
        self._tracker_lock = threading.Lock()
    
    def load_model(self, service_type: str, model_id: str) -> Optional[str]:
        """
        Load a model for a service type.
        If another model is already loaded for this service type, it will be unloaded.
        
        Args:
            service_type: Service type (text_to_image, speech_to_text, etc.)
            model_id: Model ID to load
            
        Returns:
            Previously loaded model_id if one was unloaded, None otherwise
        """
        with self._tracker_lock:
            previous_model_id = None
            
            # Check if another model is already loaded for this service type
            if service_type in self._loaded_models:
                previous = self._loaded_models[service_type]
                if previous.model_id != model_id:
                    previous_model_id = previous.model_id
                    logger.info(f"[AIServiceTracker] Unloading previous {service_type} model: {previous_model_id}")
                    # Remove the previous model
                    del self._loaded_models[service_type]
            
            # Load the new model
            self._loaded_models[service_type] = LoadedAIServiceModel(
                service_type=service_type,
                model_id=model_id,
                loaded_at=datetime.now().isoformat()
            )
            logger.info(f"[AIServiceTracker] Loaded {service_type} model: {model_id}")
            
            return previous_model_id
    
    def unload_model(self, service_type: str) -> bool:
        """
        Unload the currently loaded model for a service type.
        
        Args:
            service_type: Service type to unload
            
        Returns:
            True if a model was unloaded, False if none was loaded
        """
        with self._tracker_lock:
            if service_type in self._loaded_models:
                model_id = self._loaded_models[service_type].model_id
                del self._loaded_models[service_type]
                logger.info(f"[AIServiceTracker] Unloaded {service_type} model: {model_id}")
                return True
            return False
    
    def get_loaded_model(self, service_type: str) -> Optional[LoadedAIServiceModel]:
        """
        Get the currently loaded model for a service type.
        
        Args:
            service_type: Service type to check
            
        Returns:
            LoadedAIServiceModel if one is loaded, None otherwise
        """
        with self._tracker_lock:
            return self._loaded_models.get(service_type)
    
    def is_model_loaded(self, service_type: str, model_id: str) -> bool:
        """
        Check if a specific model is currently loaded for a service type.
        
        Args:
            service_type: Service type to check
            model_id: Model ID to check
            
        Returns:
            True if the model is loaded, False otherwise
        """
        with self._tracker_lock:
            loaded = self._loaded_models.get(service_type)
            return loaded is not None and loaded.model_id == model_id
    
    def get_all_loaded_models(self) -> Dict[str, LoadedAIServiceModel]:
        """
        Get all currently loaded models.
        
        Returns:
            Dict mapping service_type -> LoadedAIServiceModel
        """
        with self._tracker_lock:
            return self._loaded_models.copy()
    
    def update_last_used(self, service_type: str):
        """Update the last_used timestamp for the loaded model"""
        with self._tracker_lock:
            if service_type in self._loaded_models:
                self._loaded_models[service_type].last_used = datetime.now().isoformat()


def get_ai_service_loaded_tracker() -> AIServiceLoadedModelsTracker:
    """Get singleton instance of the tracker"""
    return AIServiceLoadedModelsTracker()
