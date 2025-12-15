"""
OCR Engine Tracker

Tracks which OCR engine is currently selected/active for document extraction.
Only one OCR engine can be active at a time.
"""
import threading
from typing import Optional
from datetime import datetime
from dataclasses import dataclass, field
from enum import Enum
import logging

logger = logging.getLogger(__name__)


class OCREngineType(Enum):
    """Available OCR engine types"""
    EASYOCR = "easyocr"
    TESSERACT = "tesseract"
    PADDLEOCR = "paddleocr"


@dataclass
class OCREngineInfo:
    """Information about an OCR engine"""
    engine_type: OCREngineType
    name: str
    description: str
    package_name: str
    installed: bool = False
    active: bool = False
    activated_at: Optional[str] = None
    
    def to_dict(self) -> dict:
        return {
            'engine_type': self.engine_type.value,
            'name': self.name,
            'description': self.description,
            'package_name': self.package_name,
            'installed': self.installed,
            'active': self.active,
            'activated_at': self.activated_at
        }


class OCREngineTracker:
    """
    Singleton tracker for OCR engine selection.
    Only one OCR engine can be active at a time.
    """
    _instance = None
    _lock = threading.Lock()
    
    # Available OCR engines
    AVAILABLE_ENGINES = {
        OCREngineType.EASYOCR: OCREngineInfo(
            engine_type=OCREngineType.EASYOCR,
            name="EasyOCR",
            description="Easy to use, supports 80+ languages, good for general purpose OCR",
            package_name="easyocr"
        ),
        OCREngineType.TESSERACT: OCREngineInfo(
            engine_type=OCREngineType.TESSERACT,
            name="Tesseract OCR",
            description="Highly accurate for printed text, industry standard, supports 100+ languages",
            package_name="pytesseract"
        ),
        OCREngineType.PADDLEOCR: OCREngineInfo(
            engine_type=OCREngineType.PADDLEOCR,
            name="PaddleOCR",
            description="Fast and accurate, excellent for Chinese/English, supports 80+ languages",
            package_name="paddleocr"
        ),
    }
    
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
        self._active_engine: Optional[OCREngineType] = None
        self._tracker_lock = threading.Lock()
    
    def activate_engine(self, engine_type: OCREngineType) -> Optional[OCREngineType]:
        """
        Activate an OCR engine.
        If another engine is already active, it will be deactivated.
        
        Args:
            engine_type: OCR engine type to activate
            
        Returns:
            Previously active engine type if one was deactivated, None otherwise
        """
        with self._tracker_lock:
            previous_engine = None
            
            # Check if another engine is already active
            if self._active_engine is not None and self._active_engine != engine_type:
                previous_engine = self._active_engine
                logger.info(f"[OCREngineTracker] Deactivating previous OCR engine: {previous_engine.value}")
                # Update the previous engine's active status
                if previous_engine in self.AVAILABLE_ENGINES:
                    self.AVAILABLE_ENGINES[previous_engine].active = False
                    self.AVAILABLE_ENGINES[previous_engine].activated_at = None
            
            # Activate the new engine
            self._active_engine = engine_type
            if engine_type in self.AVAILABLE_ENGINES:
                self.AVAILABLE_ENGINES[engine_type].active = True
                self.AVAILABLE_ENGINES[engine_type].activated_at = datetime.now().isoformat()
            logger.info(f"[OCREngineTracker] Activated OCR engine: {engine_type.value}")
            
            return previous_engine
    
    def deactivate_engine(self) -> bool:
        """
        Deactivate the currently active OCR engine.
        
        Returns:
            True if an engine was deactivated, False if none was active
        """
        with self._tracker_lock:
            if self._active_engine is not None:
                engine_type = self._active_engine
                if engine_type in self.AVAILABLE_ENGINES:
                    self.AVAILABLE_ENGINES[engine_type].active = False
                    self.AVAILABLE_ENGINES[engine_type].activated_at = None
                self._active_engine = None
                logger.info(f"[OCREngineTracker] Deactivated OCR engine: {engine_type.value}")
                return True
            return False
    
    def get_active_engine(self) -> Optional[OCREngineType]:
        """
        Get the currently active OCR engine.
        
        Returns:
            OCREngineType if one is active, None otherwise
        """
        with self._tracker_lock:
            return self._active_engine
    
    def is_engine_active(self, engine_type: OCREngineType) -> bool:
        """
        Check if a specific OCR engine is currently active.
        
        Args:
            engine_type: OCR engine type to check
            
        Returns:
            True if the engine is active, False otherwise
        """
        with self._tracker_lock:
            return self._active_engine == engine_type
    
    def get_engine_info(self, engine_type: OCREngineType) -> Optional[OCREngineInfo]:
        """
        Get information about an OCR engine.
        
        Args:
            engine_type: OCR engine type
            
        Returns:
            OCREngineInfo if found, None otherwise
        """
        return self.AVAILABLE_ENGINES.get(engine_type)
    
    def get_all_engines(self) -> dict:
        """
        Get all available OCR engines with their status.
        
        Returns:
            Dict mapping engine_type.value -> OCREngineInfo dict
        """
        with self._tracker_lock:
            return {
                engine_type.value: engine_info.to_dict()
                for engine_type, engine_info in self.AVAILABLE_ENGINES.items()
            }


def get_ocr_engine_tracker() -> OCREngineTracker:
    """Get singleton instance of the OCR engine tracker"""
    return OCREngineTracker()
