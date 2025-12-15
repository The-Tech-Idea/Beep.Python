"""
Text to Speech Service

Converts text to natural-sounding speech using various TTS engines.
"""
import os
import sys
import subprocess
import base64
import io
import logging
from pathlib import Path
from typing import Dict, Any, Optional
import tempfile

logger = logging.getLogger(__name__)


class TextToSpeechService:
    """Service for converting text to speech"""
    
    def __init__(self):
        from app.services.ai_services_environment import AIServiceType, get_ai_service_env
        
        self.service_type = AIServiceType.TEXT_TO_SPEECH
        self.env_mgr = get_ai_service_env(self.service_type)
    
    def _get_python_executable(self):
        """Get Python executable from the virtual environment (like other modules)"""
        # Use the environment manager's method directly, like document_extractor does
        python_exe = self.env_mgr._get_python_path()
        if python_exe and python_exe.exists():
            logger.debug(f"Using Python from text-to-speech virtual environment: {python_exe}")
            return python_exe
        
        logger.warning(f"Python executable not found in text-to-speech environment: {python_exe}")
        return None
    
    def generate_speech(self, text: str, voice: str = "default", 
                       speed: float = 1.0, 
                       engine: str = "edge-tts") -> Dict[str, Any]:
        """
        Generate speech from text
        
        IMPORTANT: Only one model per service type can be loaded at a time.
        Loading a new model automatically unloads the previous one.
        
        Args:
            text: Text to convert
            voice: Voice type/name
            speed: Speech speed multiplier
            engine: TTS engine to use (edge-tts, gtts, pyttsx3, coqui)
        
        Returns:
            Dict with 'success', 'audio' (base64), 'metadata', 'error'
        """
        try:
            # Check if virtual environment is ready
            python_exe = self._get_python_executable()
            if not python_exe:
                return {
                    'success': False,
                    'error': 'Text-to-Speech virtual environment not set up. Please create environment and install packages first.'
                }
            
            # Track loaded model (only one per service type)
            from app.services.ai_service_loaded_models import get_ai_service_loaded_tracker
            tracker = get_ai_service_loaded_tracker()
            previous_model = tracker.load_model('text_to_speech', engine)
            if previous_model and previous_model != engine:
                logger.info(f"[TextToSpeech] Switched from {previous_model} to {engine}")
            
            tracker.update_last_used('text_to_speech')
            
            return self._generate_via_subprocess(text, voice, speed, engine, python_exe)
        except Exception as e:
            logger.error(f"Error generating speech: {e}", exc_info=True)
            return {
                'success': False,
                'error': str(e)
            }
    
    def _generate_via_subprocess(self, text: str, voice: str, 
                                 speed: float, engine: str, python_exe: Path) -> Dict[str, Any]:
        """Generate speech using subprocess with dedicated environment"""
        import json
        
        # Log environment info for debugging
        env_path = self.env_mgr._env_path if hasattr(self.env_mgr, '_env_path') else 'unknown'
        logger.info(f"Text-to-Speech: Using virtual environment at {env_path}")
        logger.info(f"Text-to-Speech: Python executable: {python_exe}")
        
        try:
            script_content = '''import sys
import json
import base64
import os
import tempfile
from pathlib import Path

text = sys.argv[1]
voice = sys.argv[2]
speed = float(sys.argv[3])
engine = sys.argv[4]

try:
    output_file = tempfile.NamedTemporaryFile(delete=False, suffix='.mp3')
    output_path = output_file.name
    output_file.close()
    
    if engine == "edge-tts":
        import asyncio
        import edge_tts
        
        async def generate_tts():
            # List available voices
            voices = await edge_tts.list_voices()
            
            # Select voice based on preference
            selected_voice = None
            if voice == "male":
                for v in voices:
                    if v["Gender"] == "Male" and "en" in v["Locale"]:
                        selected_voice = v["ShortName"]
                        break
            elif voice == "female":
                for v in voices:
                    if v["Gender"] == "Female" and "en" in v["Locale"]:
                        selected_voice = v["ShortName"]
                        break
            else:
                # Default to first English voice
                for v in voices:
                    if "en" in v["Locale"]:
                        selected_voice = v["ShortName"]
                        break
            
            if not selected_voice:
                selected_voice = "en-US-AriaNeural"
            
            # Generate speech
            rate = f"+{int((speed - 1.0) * 100)}%"
            communicate = edge_tts.Communicate(text, selected_voice, rate=rate)
            await communicate.save(output_path)
        
        asyncio.run(generate_tts())
        
    elif engine == "gtts":
        from gtts import gTTS
        tts = gTTS(text=text, lang='en', slow=(speed < 1.0))
        tts.save(output_path)
        
    elif engine == "pyttsx3":
        import pyttsx3
        engine_obj = pyttsx3.init()
        
        # Set voice
        voices = engine_obj.getProperty('voices')
        if voice == "male" and len(voices) > 0:
            for v in voices:
                if "male" in v.name.lower():
                    engine_obj.setProperty('voice', v.id)
                    break
        elif voice == "female" and len(voices) > 0:
            for v in voices:
                if "female" in v.name.lower():
                    engine_obj.setProperty('voice', v.id)
                    break
        
        engine_obj.setProperty('rate', int(200 * speed))
        engine_obj.save_to_file(text, output_path)
        engine_obj.runAndWait()
        
    else:
        raise ValueError(f"Unknown engine: {engine}")
    
    # Read audio file and convert to base64
    with open(output_path, 'rb') as f:
        audio_data = f.read()
    
    audio_base64 = base64.b64encode(audio_data).decode()
    
    # Clean up
    os.unlink(output_path)
    
    result = {
        "success": True,
        "audio": audio_base64,
        "format": "mp3",
        "metadata": {
            "engine": engine,
            "voice": voice,
            "speed": speed,
            "text_length": len(text)
        }
    }
    print(json.dumps(result))
    
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
                    [str(python_exe), script_path, text, voice, str(speed), engine],
                    capture_output=True,
                    text=True,
                    timeout=300  # 5 minutes max
                )
                
                if result.returncode == 0:
                    output = result.stdout.strip()
                    try:
                        return json.loads(output)
                    except json.JSONDecodeError:
                        return {
                            'success': False,
                            'error': 'Failed to parse TTS result'
                        }
                else:
                    error_msg = result.stderr or result.stdout or 'Unknown error'
                    return {
                        'success': False,
                        'error': error_msg[:500]
                    }
            finally:
                try:
                    os.unlink(script_path)
                except:
                    pass
                    
        except subprocess.TimeoutExpired:
            return {
                'success': False,
                'error': 'TTS generation timeout'
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


def get_text_to_speech_service() -> TextToSpeechService:
    """Get singleton instance"""
    if not hasattr(get_text_to_speech_service, '_instance'):
        get_text_to_speech_service._instance = TextToSpeechService()
    return get_text_to_speech_service._instance
