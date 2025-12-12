"""
ML Model Cache Service - Caches loaded models for performance
"""
import threading
import time
from typing import Optional, Dict, Any
from pathlib import Path
from datetime import datetime, timedelta


class ModelCacheEntry:
    """Cache entry for a loaded model"""
    def __init__(self, model_obj: Any, model_id: str, version_id: str, file_path: str):
        self.model_obj = model_obj
        self.model_id = model_id
        self.version_id = version_id
        self.file_path = file_path
        self.loaded_at = datetime.utcnow()
        self.last_used = datetime.utcnow()
        self.use_count = 0
        self.lock = threading.Lock()
    
    def touch(self):
        """Update last used time"""
        self.last_used = datetime.utcnow()
        self.use_count += 1
    
    def is_expired(self, max_age_minutes: int = 60) -> bool:
        """Check if cache entry is expired"""
        age = datetime.utcnow() - self.last_used
        return age > timedelta(minutes=max_age_minutes)


class MLModelCache:
    """Singleton cache for loaded ML models"""
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
        self._cache: Dict[str, ModelCacheEntry] = {}
        self._cache_lock = threading.Lock()
        self.max_cache_size = 10  # Maximum number of models to cache
        self.max_age_minutes = 60  # Cache expiration time
    
    def get(self, model_id: str, version_id: str) -> Optional[Any]:
        """Get cached model"""
        cache_key = f"{model_id}:{version_id}"
        
        with self._cache_lock:
            entry = self._cache.get(cache_key)
            if entry:
                # Check if expired
                if entry.is_expired(self.max_age_minutes):
                    del self._cache[cache_key]
                    return None
                
                # Check if file still exists and hasn't changed
                if not Path(entry.file_path).exists():
                    del self._cache[cache_key]
                    return None
                
                entry.touch()
                return entry.model_obj
        
        return None
    
    def put(self, model_id: str, version_id: str, file_path: str, model_obj: Any):
        """Cache a model"""
        cache_key = f"{model_id}:{version_id}"
        
        with self._cache_lock:
            # If cache is full, remove least recently used
            if len(self._cache) >= self.max_cache_size and cache_key not in self._cache:
                self._evict_lru()
            
            entry = ModelCacheEntry(model_obj, model_id, version_id, file_path)
            self._cache[cache_key] = entry
    
    def remove(self, model_id: str, version_id: Optional[str] = None):
        """Remove model from cache"""
        with self._cache_lock:
            if version_id:
                cache_key = f"{model_id}:{version_id}"
                self._cache.pop(cache_key, None)
            else:
                # Remove all versions of this model
                keys_to_remove = [k for k in self._cache.keys() if k.startswith(f"{model_id}:")]
                for key in keys_to_remove:
                    del self._cache[key]
    
    def clear(self):
        """Clear all cached models"""
        with self._cache_lock:
            self._cache.clear()
    
    def _evict_lru(self):
        """Evict least recently used entry"""
        if not self._cache:
            return
        
        lru_key = min(self._cache.keys(), 
                     key=lambda k: self._cache[k].last_used)
        del self._cache[lru_key]
    
    def get_stats(self) -> Dict[str, Any]:
        """Get cache statistics"""
        with self._cache_lock:
            return {
                'size': len(self._cache),
                'max_size': self.max_cache_size,
                'entries': [
                    {
                        'model_id': entry.model_id,
                        'version_id': entry.version_id,
                        'use_count': entry.use_count,
                        'last_used': entry.last_used.isoformat(),
                        'age_minutes': (datetime.utcnow() - entry.last_used).total_seconds() / 60
                    }
                    for entry in self._cache.values()
                ]
            }


def get_ml_model_cache() -> MLModelCache:
    """Get singleton instance of MLModelCache"""
    return MLModelCache()

