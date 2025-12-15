"""
Voice to Voice Service

Converts voice from one style to another, voice cloning, etc.
"""
import os
import sys
import subprocess
import base64
import logging
from pathlib import Path
from typing import Dict, Any, Optional
import tempfile
import json

logger = logging.getLogger(__name__)


class VoiceToVoiceService:
    """Service for voice conversion and cloning"""
    
    def __init__(self):
        from app.services.ai_services_environment import AIServiceType, get_ai_service_env
        
        self.service_type = AIServiceType.VOICE_TO_VOICE
        self.env_mgr = get_ai_service_env(self.service_type)
    
    def _get_python_executable(self):
        """Get Python executable from the virtual environment (like other modules)"""
        # Use the environment manager's method directly, like document_extractor does
        python_exe = self.env_mgr._get_python_path()
        if python_exe and python_exe.exists():
            logger.debug(f"Using Python from voice-to-voice virtual environment: {python_exe}")
            return python_exe
        
        logger.warning(f"Python executable not found in voice-to-voice environment: {python_exe}")
        return None
    
    def convert_voice(self, source_audio: bytes, 
                     target_voice_type: str = "preset",
                     voice_sample: Optional[bytes] = None,
                     preset_voice: str = "male1") -> Dict[str, Any]:
        """
        Convert voice from source audio
        
        Args:
            source_audio: Source audio file content as bytes
            target_voice_type: "preset" or "clone"
            voice_sample: Sample audio for voice cloning (if target_voice_type is "clone")
            preset_voice: Preset voice name (if target_voice_type is "preset")
        
        Returns:
            Dict with 'success', 'audio' (base64), 'metadata', 'error'
        """
        try:
            # Check if virtual environment is ready
            python_exe = self._get_python_executable()
            if not python_exe:
                return {
                    'success': False,
                    'error': 'Voice-to-Voice virtual environment not set up. Please create environment and install packages first.'
                }
            
            # Log environment info for debugging
            env_path = self.env_mgr._env_path if hasattr(self.env_mgr, '_env_path') else 'unknown'
            logger.info(f"Voice-to-Voice: Using virtual environment at {env_path}")
            logger.info(f"Voice-to-Voice: Python executable: {python_exe}")
            
            # For now, return a placeholder implementation
            # Voice conversion requires more complex setup (so-vits-svc, etc.)
            return {
                'success': False,
                'error': 'Voice-to-Voice conversion is not yet fully implemented. This requires additional model setup.'
            }
            
            # TODO: Implement actual voice conversion
            # This would involve:
            # 1. Loading source audio
            # 2. If cloning: Training/using a voice model from sample
            # 3. If preset: Applying preset voice transformation
            # 4. Generating output audio
            
        except Exception as e:
            logger.error(f"Error converting voice: {e}", exc_info=True)
            return {
                'success': False,
                'error': str(e)
            }
    
    def get_status(self) -> Dict[str, Any]:
        """Get service status"""
        env_status = self.env_mgr.get_status()
        
        return {
            'environment_ready': env_status['status'] == 'ready',
            'packages_installed': env_status['all_required_installed'],
            'env_status': env_status,
            'note': 'Voice-to-Voice conversion requires additional model training/setup'
        }


def get_voice_to_voice_service() -> VoiceToVoiceService:
    """Get singleton instance"""
    if not hasattr(get_voice_to_voice_service, '_instance'):
        get_voice_to_voice_service._instance = VoiceToVoiceService()
    return get_voice_to_voice_service._instance
