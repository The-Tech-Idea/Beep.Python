"""
Speech to Text Service

Transcribes speech/audio to text using Whisper and other models.
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


class SpeechToTextService:
    """Service for converting speech to text"""
    
    def __init__(self):
        from app.services.ai_services_environment import AIServiceType, get_ai_service_env
        
        self.service_type = AIServiceType.SPEECH_TO_TEXT
        self.env_mgr = get_ai_service_env(self.service_type)
    
    def _get_python_executable(self):
        """Get Python executable from the virtual environment (like other modules)"""
        # Use the environment manager's method directly, like document_extractor does
        python_exe = self.env_mgr._get_python_path()
        if python_exe and python_exe.exists():
            logger.debug(f"Using Python from speech-to-text virtual environment: {python_exe}")
            return python_exe
        
        logger.warning(f"Python executable not found in speech-to-text environment: {python_exe}")
        return None
    
    def transcribe_audio(self, audio_content: bytes, 
                        language: str = "en",
                        model_size: str = "base") -> Dict[str, Any]:
        """
        Transcribe audio to text
        
        IMPORTANT: Only one model per service type can be loaded at a time.
        Loading a new model automatically unloads the previous one.
        
        Args:
            audio_content: Audio file content as bytes
            language: Language code (en, es, fr, etc.) or "auto" for auto-detect
            model_size: Whisper model size (tiny, base, small, medium, large)
        
        Returns:
            Dict with 'success', 'text', 'language', 'metadata', 'error'
        """
        try:
            # Check if virtual environment is ready
            python_exe = self._get_python_executable()
            if not python_exe:
                return {
                    'success': False,
                    'error': 'Speech-to-Text virtual environment not set up. Please create environment and install packages first.'
                }
            
            # Track loaded model (only one per service type)
            from app.services.ai_service_loaded_models import get_ai_service_loaded_tracker
            tracker = get_ai_service_loaded_tracker()
            previous_model = tracker.load_model('speech_to_text', model_size)
            if previous_model and previous_model != model_size:
                logger.info(f"[SpeechToText] Switched from {previous_model} to {model_size}")
            
            tracker.update_last_used('speech_to_text')
            
            return self._transcribe_via_subprocess(audio_content, language, model_size, python_exe)
        except Exception as e:
            logger.error(f"Error transcribing audio: {e}", exc_info=True)
            return {
                'success': False,
                'error': str(e)
            }
    
    def _transcribe_via_subprocess(self, audio_content: bytes,
                                   language: str, model_size: str, python_exe: Path) -> Dict[str, Any]:
        """Transcribe using subprocess with dedicated environment"""
        # Log environment info for debugging
        env_path = self.env_mgr._env_path if hasattr(self.env_mgr, '_env_path') else 'unknown'
        logger.info(f"Speech-to-Text: Using virtual environment at {env_path}")
        logger.info(f"Speech-to-Text: Python executable: {python_exe}")
        
        try:
            # Save audio to temp file
            with tempfile.NamedTemporaryFile(delete=False, suffix='.wav') as audio_file:
                audio_file.write(audio_content)
                audio_path = audio_file.name
            
            script_content = '''import sys
import json
import whisper

audio_path = sys.argv[1]
language = sys.argv[2]
model_size = sys.argv[3]

try:
    # Load Whisper model (will download on first use)
    model = whisper.load_model(model_size)
    
    # Transcribe
    if language == "auto":
        result = model.transcribe(audio_path)
    else:
        result = model.transcribe(audio_path, language=language)
    
    transcription_result = {
        "success": True,
        "text": result["text"].strip(),
        "language": result.get("language", language),
        "metadata": {
            "model_size": model_size,
            "duration": result.get("duration", 0),
            "segments": len(result.get("segments", []))
        }
    }
    print(json.dumps(transcription_result))
    
except Exception as e:
    import traceback
    error_result = {
        "success": False,
        "error": str(e),
        "traceback": traceback.format_exc()[:500]
    }
    print(json.dumps(error_result))
    sys.exit(1)
'''
            
            with tempfile.NamedTemporaryFile(mode='w', suffix='.py', delete=False) as f:
                f.write(script_content)
                script_path = f.name
            
            try:
                result = subprocess.run(
                    [str(python_exe), script_path, audio_path, language, model_size],
                    capture_output=True,
                    text=True,
                    timeout=600  # 10 minutes max (for large audio files)
                )
                
                if result.returncode == 0:
                    output = result.stdout.strip()
                    try:
                        transcription = json.loads(output)
                        return transcription
                    except json.JSONDecodeError:
                        return {
                            'success': False,
                            'error': 'Failed to parse transcription result'
                        }
                else:
                    error_msg = result.stderr or result.stdout or 'Unknown error'
                    return {
                        'success': False,
                        'error': error_msg[:500]
                    }
            finally:
                # Clean up temp files
                try:
                    os.unlink(script_path)
                    os.unlink(audio_path)
                except:
                    pass
                    
        except subprocess.TimeoutExpired:
            return {
                'success': False,
                'error': 'Transcription timeout (exceeded 10 minutes)'
            }
        except Exception as e:
            logger.error(f"Subprocess error: {e}", exc_info=True)
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
            'env_status': env_status
        }


def get_speech_to_text_service() -> SpeechToTextService:
    """Get singleton instance"""
    if not hasattr(get_speech_to_text_service, '_instance'):
        get_speech_to_text_service._instance = SpeechToTextService()
    return get_speech_to_text_service._instance
