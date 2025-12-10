"""
Model Inference Service

Provides local model inference using native llama.cpp server (LM Studio style).
Supports:
- Loading and unloading models via HTTP API
- Chat completions (OpenAI-compatible)
- Text completions
- Streaming responses
- Context management

Architecture:
- Uses pre-built llama.cpp server binaries (no Python compilation)
- Communicates via HTTP API (OpenAI-compatible endpoints)
- Supports CUDA, Vulkan, Metal, ROCm, CPU backends
"""
import os
import sys
import json
import threading
import queue
import multiprocessing
from pathlib import Path
from datetime import datetime
from typing import Optional, List, Dict, Any, Generator, Callable
from dataclasses import dataclass, field, asdict

# CRITICAL: Initialize freeze_support for subprocess calls in frozen apps
if getattr(sys, 'frozen', False):
    multiprocessing.freeze_support()

from .llm_manager import LLMManager, LocalModel
from .hardware_service import HardwareService
from .llama_server_manager import get_llama_server_manager, ServerConfig
from .llama_binary_manager import get_llama_binary_manager


@dataclass
class ChatMessage:
    """A chat message"""
    role: str  # system, user, assistant
    content: str
    timestamp: Optional[str] = None
    
    def to_dict(self) -> dict:
        return asdict(self)


@dataclass
class ChatSession:
    """A chat session with a model"""
    id: str
    model_id: str
    messages: List[ChatMessage] = field(default_factory=list)
    created_at: str = field(default_factory=lambda: datetime.now().isoformat())
    last_activity: str = field(default_factory=lambda: datetime.now().isoformat())
    system_prompt: Optional[str] = None
    
    def to_dict(self) -> dict:
        return {
            'id': self.id,
            'model_id': self.model_id,
            'messages': [m.to_dict() for m in self.messages],
            'created_at': self.created_at,
            'last_activity': self.last_activity,
            'system_prompt': self.system_prompt
        }


@dataclass
class InferenceConfig:
    """Configuration for model inference"""
    n_ctx: int = 4096           # Context length
    n_batch: int = 512          # Batch size
    n_threads: int = 0          # 0 = auto
    n_gpu_layers: int = -1      # Default to -1 (all layers) to ensure GPU usage if available
    temperature: float = 0.7
    top_p: float = 0.95
    top_k: int = 40
    repeat_penalty: float = 1.1
    max_tokens: int = 2048
    stop_sequences: List[str] = field(default_factory=list)
    
    def to_dict(self) -> dict:
        return asdict(self)


class LoadedModel:
    """Wrapper for a loaded model"""
    
    def __init__(self, local_model: LocalModel, llm: Any, config: InferenceConfig, is_subprocess: bool = False):
        self.local_model = local_model
        self.llm = llm  # Server instance info or legacy Llama/SubprocessModel
        self.config = config
        self.loaded_at = datetime.now()
        self.request_count = 0
        self.total_tokens_generated = 0
        self._lock = threading.Lock()
        self.is_subprocess = is_subprocess  # True if using subprocess model (legacy)
        self.is_server = False  # True if using HTTP server (LM Studio style)
        self.server_info = None  # Server info when using HTTP API
    
    def get_stats(self) -> dict:
        return {
            'model_id': self.local_model.id,
            'model_name': self.local_model.name,
            'loaded_at': self.loaded_at.isoformat(),
            'uptime_seconds': (datetime.now() - self.loaded_at).total_seconds(),
            'request_count': self.request_count,
            'total_tokens_generated': self.total_tokens_generated,
            'config': self.config.to_dict(),
            'inference_mode': 'server' if self.is_server else ('subprocess' if self.is_subprocess else 'direct')
        }


class InferenceService:
    """
    Service for model inference using LM Studio-style HTTP API
    
    Uses native llama.cpp server binaries for inference.
    Falls back to legacy subprocess/direct mode if server not available.
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
        self.llm_manager = LLMManager()
        self.hardware_service = HardwareService()
        self._loaded_models: Dict[str, LoadedModel] = {}
        self._chat_sessions: Dict[str, ChatSession] = {}
        self._llama_cpp_available = self._check_llama_cpp()
        
        # LM Studio-style server managers - initialize BEFORE _load_default_config
        self._server_manager = get_llama_server_manager()
        self._binary_manager = get_llama_binary_manager()
        
        # Now load config (uses _binary_manager)
        self._default_config = self._load_default_config()
        
        # Fresh start - no models loaded
        print("[InferenceService] Ready - no models loaded (fresh start)")
    
    def _check_llama_cpp(self) -> bool:
        """Check if llama-cpp-python is available (legacy mode)"""
        try:
            from llama_cpp import Llama
            return True
        except ImportError:
            return False
    
    def _check_server_available(self) -> bool:
        """Check if LM Studio-style server is available"""
        return self._binary_manager.get_server_executable() is not None
    
    def _load_default_config(self) -> InferenceConfig:
        """Load default inference config from hardware settings"""
        # First check for installed backend from binary manager (LM Studio style)
        active_server_backend = self._binary_manager.get_active_backend()
        if active_server_backend:
            backend_type = active_server_backend.id  # e.g., "cuda", "vulkan", "hip"
            settings = self.hardware_service.get_inference_settings_for_backend(backend_type)
            print(f"[InferenceService] Using backend {backend_type}, gpu_layers={settings.get('n_gpu_layers')}")
            return InferenceConfig(
                n_ctx=settings.get('n_ctx', 4096),
                n_batch=settings.get('n_batch', 512),
                n_threads=settings.get('n_threads', 0),
                n_gpu_layers=settings.get('n_gpu_layers', -1)  # Default to all GPU layers
            )
        
        # Fall back to hardware service
        active_backend = self.hardware_service.get_active_backend()
        
        if active_backend:
            settings = self.hardware_service.get_inference_settings_for_backend(active_backend)
            return InferenceConfig(
                n_ctx=settings.get('n_ctx', 4096),
                n_batch=settings.get('n_batch', 512),
                n_threads=settings.get('n_threads', 0),
                n_gpu_layers=settings.get('n_gpu_layers', -1)  # Default to all GPU layers
            )
        
        # Default config with GPU layers = -1 (all on GPU if available)
        print("[InferenceService] No backend detected, using default config with n_gpu_layers=-1")
        return InferenceConfig(n_gpu_layers=-1)
    
    def reload_hardware_config(self):
        """Reload hardware configuration (call after config changes)"""
        self._default_config = self._load_default_config()
    
    def get_hardware_info(self) -> dict:
        """Get current hardware configuration info"""
        profile = self.hardware_service.detect_hardware()
        active_backend = self.hardware_service.get_active_backend()
        
        # Get installed backends from binary manager
        installed_backends = self._binary_manager.get_installed_backends()
        
        gpus = []
        for backend in profile.backends:
            for device in backend.devices:
                gpus.append({
                    'name': device.name,
                    'vendor': device.vendor,
                    'memory_gb': device.memory_total / (1024**3) if device.memory_total else 0
                })
        
        # Server availability (LM Studio style)
        server_available = self._check_server_available()
        active_server_backend = self._binary_manager.get_active_backend()
        
        return {
            'current_backend': active_backend or 'cpu',
            'available_backends': [b.type for b in profile.backends if b.available],
            'gpus': gpus,
            'llama_cpp_available': self._llama_cpp_available,
            'llama_cpp_installed': profile.llama_cpp_installed,
            'llama_cpp_version': profile.llama_cpp_version,
            # LM Studio-style server info
            'server_available': server_available,
            'server_backend': active_server_backend.id if active_server_backend else None,
            'installed_backends': [b.to_dict() for b in installed_backends],
            'inference_mode': 'server' if server_available else ('legacy' if self._llama_cpp_available else 'none')
        }
    
    def is_available(self) -> bool:
        """Check if inference is available (server or legacy mode)"""
        return self._check_server_available() or self._llama_cpp_available
    
    def get_requirements_info(self) -> dict:
        """Get info about what's needed for inference"""
        available_backends = self._binary_manager.get_available_backends()
        recommended = self._binary_manager.get_recommended_backend()
        
        return {
            'server_available': self._check_server_available(),
            'llama_cpp_available': self._llama_cpp_available,
            'recommended_backend': recommended,
            'available_backends': {k: v.to_dict() for k, v in available_backends.items()},
            'models_path': str(self.llm_manager.models_path),
            'setup_instructions': 'Install a backend from the GPU Backends tab to enable inference.'
        }
    
    # =====================
    # Model Loading
    # =====================
    
    def load_model(self, model_id: str, config: Optional[InferenceConfig] = None) -> Optional[LoadedModel]:
        """
        Load a model for inference using LM Studio-style HTTP server
        
        Architecture (LM Studio style - NO FALLBACKS):
        1. Check if backend is installed
        2. Start llama-server for the model
        3. Communicate via HTTP API
        
        Args:
            model_id: Local model ID
            config: Optional inference configuration
        """
        # Check if already loaded
        if model_id in self._loaded_models:
            return self._loaded_models[model_id]
        
        # Get model info
        model = self.llm_manager.get_model_by_id(model_id)
        if not model:
            raise ValueError(f"Model not found: {model_id}")
        
        if not Path(model.path).exists():
            raise FileNotFoundError(f"Model file not found: {model.path}")
        
        config = config or self._default_config
        
        # Check if backend is installed
        if not self._check_server_available():
            raise RuntimeError(
                "No llama.cpp backend installed. Please install a backend from the GPU Backends tab."
            )
        
        # LM Studio-style: Start native llama-server with HTTP API
        server_config = ServerConfig(
            model_path=model.path,
            context_size=config.n_ctx,
            gpu_layers=config.n_gpu_layers,
            threads=config.n_threads,
            batch_size=config.n_batch
        )
        
        # Start server for this model
        result = self._server_manager.start_server(
            model_id=model_id,
            model_path=model.path,
            config=server_config
        )
        
        if not result.get('success'):
            error_msg = result.get('error', 'Unknown error')
            raise RuntimeError(f"Failed to start llama-server: {error_msg}")
        
        # Create loaded model wrapper
        loaded = LoadedModel(model, None, config)
        loaded.is_server = True
        loaded.server_info = result.get('server')
        self._loaded_models[model_id] = loaded
        
        # Update last used
        model.last_used = datetime.now().isoformat()
        self.llm_manager.add_local_model(model)
        
        print(f"[InferenceService] Model {model_id} loaded via HTTP server on port {loaded.server_info.get('port')}")
        return loaded
    
    def unload_model(self, model_id: str) -> bool:
        """Unload a model from memory"""
        if model_id not in self._loaded_models:
            return False
        
        loaded = self._loaded_models[model_id]
        
        # Stop server if using HTTP mode
        if loaded.is_server:
            self._server_manager.stop_server(model_id)
        # Shutdown subprocess if applicable
        elif loaded.is_subprocess:
            loaded.llm.shutdown()
        
        if loaded.llm:
            del loaded.llm
        del self._loaded_models[model_id]
        
        return True
    
    def get_loaded_models(self) -> List[Dict[str, Any]]:
        """Get list of currently loaded models"""
        return [m.get_stats() for m in self._loaded_models.values()]
    
    def is_model_loaded(self, model_id: str) -> bool:
        """Check if a model is loaded"""
        return model_id in self._loaded_models
    
    # =====================
    # Text Completion
    # =====================
    
    def complete(self, model_id: str, prompt: str, 
                 max_tokens: Optional[int] = None,
                 temperature: Optional[float] = None,
                 stream: bool = False) -> Any:
        """
        Generate text completion
        
        Args:
            model_id: Local model ID (must be loaded)
            prompt: The prompt to complete
            max_tokens: Maximum tokens to generate
            temperature: Sampling temperature
            stream: Whether to stream the response
        """
        if model_id not in self._loaded_models:
            raise ValueError(f"Model not loaded: {model_id}")
        
        loaded = self._loaded_models[model_id]
        
        with loaded._lock:
            config = loaded.config
            
            params = {
                'max_tokens': max_tokens or config.max_tokens,
                'temperature': temperature or config.temperature,
                'top_p': config.top_p,
                'top_k': config.top_k,
                'repeat_penalty': config.repeat_penalty,
                'stream': stream
            }
            
            if config.stop_sequences:
                params['stop'] = config.stop_sequences
            
            loaded.request_count += 1
            
            if stream:
                return self._stream_completion(loaded, prompt, params)
            
            # === HTTP Server Mode (LM Studio style) ===
            if loaded.is_server:
                result = self._server_manager.generate_completion(
                    model_id=model_id,
                    prompt=prompt,
                    max_tokens=params['max_tokens'],
                    temperature=params['temperature'],
                    top_p=params['top_p'],
                    stop=params.get('stop'),
                    stream=False
                )
                
                if result.get('success'):
                    api_result = result.get('result', {})
                    loaded.total_tokens_generated += api_result.get('usage', {}).get('completion_tokens', 0)
                    return api_result
                else:
                    raise RuntimeError(f"Completion failed: {result.get('error')}")
            
            # === Legacy subprocess mode ===
            elif loaded.is_subprocess:
                text = loaded.llm.complete(
                    prompt=prompt,
                    max_tokens=params['max_tokens'],
                    temperature=params['temperature'],
                    stream=False
                )
                result = {
                    'choices': [{'text': text, 'finish_reason': 'stop'}],
                    'usage': {'completion_tokens': len(text.split())}
                }
                loaded.total_tokens_generated += result.get('usage', {}).get('completion_tokens', 0)
                return result
            
            # === Direct llama-cpp-python mode ===
            else:
                result = loaded.llm(prompt, **params)
                loaded.total_tokens_generated += result.get('usage', {}).get('completion_tokens', 0)
                return result
    
    def _stream_completion(self, loaded: LoadedModel, prompt: str, 
                           params: dict) -> Generator[str, None, None]:
        """Stream completion tokens"""
        if loaded.is_subprocess:
            # Use subprocess model streaming
            for token in loaded.llm.complete(
                prompt=prompt,
                max_tokens=params['max_tokens'],
                temperature=params['temperature'],
                stream=True
            ):
                loaded.total_tokens_generated += 1
                yield token
        else:
            # Use direct model streaming
            for output in loaded.llm(prompt, **params):
                if 'choices' in output and len(output['choices']) > 0:
                    token = output['choices'][0].get('text', '')
                    if token:
                        loaded.total_tokens_generated += 1
                        yield token
    
    # =====================
    # Chat Completion
    # =====================
    
    def chat(self, model_id: str, messages: List[Dict[str, str]],
             max_tokens: Optional[int] = None,
             temperature: Optional[float] = None,
             stream: bool = False) -> Any:
        """
        Generate chat completion
        
        Args:
            model_id: Local model ID (must be loaded)
            messages: List of chat messages [{"role": "user", "content": "..."}]
            max_tokens: Maximum tokens to generate
            temperature: Sampling temperature
            stream: Whether to stream the response
        """
        if model_id not in self._loaded_models:
            raise ValueError(f"Model not loaded: {model_id}")
        
        loaded = self._loaded_models[model_id]
        
        with loaded._lock:
            config = loaded.config
            
            params = {
                'max_tokens': max_tokens or config.max_tokens,
                'temperature': temperature or config.temperature,
                'top_p': config.top_p,
                'top_k': config.top_k,
                'repeat_penalty': config.repeat_penalty,
                'stream': stream
            }
            
            if config.stop_sequences:
                params['stop'] = config.stop_sequences
            
            loaded.request_count += 1
            
            if stream:
                return self._stream_chat(loaded, messages, params)
            
            # === HTTP Server Mode (LM Studio style) ===
            if loaded.is_server:
                result = self._server_manager.chat_completion(
                    model_id=model_id,
                    messages=messages,
                    max_tokens=params['max_tokens'],
                    temperature=params['temperature'],
                    top_p=params['top_p'],
                    stream=False
                )
                
                if result.get('success'):
                    api_result = result.get('result', {})
                    loaded.total_tokens_generated += api_result.get('usage', {}).get('completion_tokens', 0)
                    return api_result
                else:
                    raise RuntimeError(f"Chat completion failed: {result.get('error')}")
            
            # === Legacy subprocess mode ===
            elif loaded.is_subprocess:
                # Use subprocess model
                content = loaded.llm.chat(
                    messages=messages,
                    max_tokens=params['max_tokens'],
                    temperature=params['temperature'],
                    stream=False
                )
                # Format as llama-cpp-python response
                result = {
                    'choices': [{'message': {'role': 'assistant', 'content': content}, 'finish_reason': 'stop'}],
                    'usage': {'completion_tokens': len(content.split())}  # Approximate
                }
                loaded.total_tokens_generated += result.get('usage', {}).get('completion_tokens', 0)
                return result
            
            # === Direct llama-cpp-python mode ===
            else:
                # Use direct model
                result = loaded.llm.create_chat_completion(messages=messages, **params)
                loaded.total_tokens_generated += result.get('usage', {}).get('completion_tokens', 0)
                return result
    
    def _stream_chat(self, loaded: LoadedModel, messages: List[Dict[str, str]], 
                     params: dict) -> Generator[str, None, None]:
        """Stream chat completion tokens"""
        # === HTTP Server Mode (LM Studio style) ===
        if loaded.is_server:
            # For now, use non-streaming and yield the full response
            # TODO: Implement proper SSE streaming from llama-server
            result = self._server_manager.chat_completion(
                model_id=loaded.local_model.id,
                messages=messages,
                max_tokens=params['max_tokens'],
                temperature=params['temperature'],
                top_p=params['top_p'],
                stream=False
            )
            
            if result.get('success'):
                api_result = result.get('result', {})
                if 'choices' in api_result and len(api_result['choices']) > 0:
                    content = api_result['choices'][0].get('message', {}).get('content', '')
                    loaded.total_tokens_generated += api_result.get('usage', {}).get('completion_tokens', 0)
                    # Yield word by word to simulate streaming
                    for word in content.split(' '):
                        yield word + ' '
            else:
                yield f"Error: {result.get('error', 'Unknown error')}"
        
        # === Legacy subprocess mode ===
        elif loaded.is_subprocess:
            # Use subprocess model streaming
            for token in loaded.llm.chat(
                messages=messages,
                max_tokens=params['max_tokens'],
                temperature=params['temperature'],
                stream=True
            ):
                loaded.total_tokens_generated += 1
                yield token
        
        # === Direct llama-cpp-python mode ===
        else:
            # Use direct model streaming
            for output in loaded.llm.create_chat_completion(messages=messages, **params):
                if 'choices' in output and len(output['choices']) > 0:
                    delta = output['choices'][0].get('delta', {})
                    content = delta.get('content', '')
                    if content:
                        loaded.total_tokens_generated += 1
                        yield content
    
    # =====================
    # Chat Sessions
    # =====================
    
    def create_session(self, model_id: str, system_prompt: Optional[str] = None) -> ChatSession:
        """Create a new chat session"""
        import uuid
        
        session_id = str(uuid.uuid4())[:8]
        session = ChatSession(
            id=session_id,
            model_id=model_id,
            system_prompt=system_prompt
        )
        
        if system_prompt:
            session.messages.append(ChatMessage(
                role="system",
                content=system_prompt,
                timestamp=datetime.now().isoformat()
            ))
        
        self._chat_sessions[session_id] = session
        return session
    
    def get_session(self, session_id: str) -> Optional[ChatSession]:
        """Get a chat session"""
        return self._chat_sessions.get(session_id)
    
    def get_sessions(self) -> List[ChatSession]:
        """Get all chat sessions"""
        return list(self._chat_sessions.values())
    
    def delete_session(self, session_id: str) -> bool:
        """Delete a chat session"""
        if session_id in self._chat_sessions:
            del self._chat_sessions[session_id]
            return True
        return False
    
    def send_message(self, session_id: str, content: str, 
                     stream: bool = False) -> Any:
        """
        Send a message in a chat session
        
        Args:
            session_id: Session ID
            content: Message content
            stream: Whether to stream the response
        """
        session = self.get_session(session_id)
        if not session:
            raise ValueError(f"Session not found: {session_id}")
        
        # Add user message
        user_message = ChatMessage(
            role="user",
            content=content,
            timestamp=datetime.now().isoformat()
        )
        session.messages.append(user_message)
        session.last_activity = datetime.now().isoformat()
        
        # Prepare messages for API
        messages = [{"role": m.role, "content": m.content} for m in session.messages]
        
        if stream:
            return self._stream_session_response(session, messages)
        else:
            response = self.chat(session.model_id, messages, stream=False)
            
            # Extract assistant message
            if 'choices' in response and len(response['choices']) > 0:
                assistant_content = response['choices'][0].get('message', {}).get('content', '')
                
                assistant_message = ChatMessage(
                    role="assistant",
                    content=assistant_content,
                    timestamp=datetime.now().isoformat()
                )
                session.messages.append(assistant_message)
                
                return assistant_message.to_dict()
            
            return None
    
    def _stream_session_response(self, session: ChatSession, 
                                  messages: List[Dict[str, str]]) -> Generator[str, None, None]:
        """Stream session response and collect full message"""
        full_response = []
        
        for token in self.chat(session.model_id, messages, stream=True):
            full_response.append(token)
            yield token
        
        # Save complete message to session
        assistant_message = ChatMessage(
            role="assistant",
            content="".join(full_response),
            timestamp=datetime.now().isoformat()
        )
        session.messages.append(assistant_message)
    
    # =====================
    # Configuration
    # =====================
    
    def get_default_config(self) -> InferenceConfig:
        """Get default inference configuration"""
        return self._default_config
    
    def set_default_config(self, config: InferenceConfig):
        """Set default inference configuration"""
        self._default_config = config
    
    def get_gpu_info(self) -> dict:
        """Get GPU information if available"""
        info = {
            'cuda_available': False,
            'metal_available': False,
            'gpus': []
        }
        
        try:
            import torch
            if torch.cuda.is_available():
                info['cuda_available'] = True
                for i in range(torch.cuda.device_count()):
                    info['gpus'].append({
                        'index': i,
                        'name': torch.cuda.get_device_name(i),
                        'memory': torch.cuda.get_device_properties(i).total_memory
                    })
        except ImportError:
            pass
        
        try:
            import platform
            if platform.system() == 'Darwin':
                # Check for Metal support on macOS
                import subprocess
                result = subprocess.run(['system_profiler', 'SPDisplaysDataType'], 
                                       capture_output=True, text=True)
                if 'Metal' in result.stdout:
                    info['metal_available'] = True
        except:
            pass
        
        return info
    
    # =====================
    # OpenAI-Compatible API Methods
    # =====================
    
    def chat_completion(self, model_id: str, 
                        messages: List[ChatMessage],
                        temperature: float = 0.7,
                        top_p: float = 0.95,
                        max_tokens: int = 2048,
                        stop: Optional[List[str]] = None) -> Dict[str, Any]:
        """
        OpenAI-compatible chat completion (non-streaming)
        
        Args:
            model_id: Local model ID
            messages: List of ChatMessage objects
            temperature: Sampling temperature
            top_p: Top-p sampling
            max_tokens: Maximum tokens to generate
            stop: Stop sequences
            
        Returns:
            Dict with content, finish_reason, and token counts
        """
        # Ensure model is loaded
        if model_id not in self._loaded_models:
            self.load_model(model_id)
        
        loaded = self._loaded_models[model_id]
        
        # Convert ChatMessage to dict format
        msg_dicts = [{"role": m.role, "content": m.content} for m in messages]
        
        with loaded._lock:
            loaded.request_count += 1
            
            # === HTTP Server Mode (LM Studio style) ===
            if loaded.is_server:
                result = self._server_manager.chat_completion(
                    model_id=model_id,
                    messages=msg_dicts,
                    max_tokens=max_tokens,
                    temperature=temperature,
                    top_p=top_p,
                    stream=False
                )
                
                if result.get('success'):
                    api_result = result.get('result', {})
                    usage = api_result.get('usage', {})
                    loaded.total_tokens_generated += usage.get('completion_tokens', 0)
                    
                    # Extract response
                    content = ""
                    finish_reason = "stop"
                    
                    if 'choices' in api_result and len(api_result['choices']) > 0:
                        choice = api_result['choices'][0]
                        content = choice.get('message', {}).get('content', '')
                        finish_reason = choice.get('finish_reason', 'stop')
                    
                    return {
                        'content': content,
                        'finish_reason': finish_reason,
                        'prompt_tokens': usage.get('prompt_tokens', 0),
                        'completion_tokens': usage.get('completion_tokens', 0),
                        'total_tokens': usage.get('total_tokens', 0)
                    }
                else:
                    raise RuntimeError(f"Chat completion failed: {result.get('error')}")
            
            # === Legacy/Direct Mode ===
            params = {
                'max_tokens': max_tokens,
                'temperature': temperature,
                'top_p': top_p,
                'stream': False
            }
            if stop:
                params['stop'] = stop
            
            result = loaded.llm.create_chat_completion(messages=msg_dicts, **params)
            
            usage = result.get('usage', {})
            loaded.total_tokens_generated += usage.get('completion_tokens', 0)
            
            # Extract response
            content = ""
            finish_reason = "stop"
            
            if 'choices' in result and len(result['choices']) > 0:
                choice = result['choices'][0]
                content = choice.get('message', {}).get('content', '')
                finish_reason = choice.get('finish_reason', 'stop')
            
            return {
                'content': content,
                'finish_reason': finish_reason,
                'prompt_tokens': usage.get('prompt_tokens', 0),
                'completion_tokens': usage.get('completion_tokens', 0),
                'total_tokens': usage.get('total_tokens', 0)
            }
    
    def chat_completion_stream(self, model_id: str,
                               messages: List[ChatMessage],
                               temperature: float = 0.7,
                               top_p: float = 0.95,
                               max_tokens: int = 2048,
                               stop: Optional[List[str]] = None) -> Generator[Dict[str, Any], None, None]:
        """
        OpenAI-compatible chat completion (streaming)
        
        Args:
            model_id: Local model ID
            messages: List of ChatMessage objects
            temperature: Sampling temperature
            top_p: Top-p sampling
            max_tokens: Maximum tokens to generate
            stop: Stop sequences
            
        Yields:
            Dict with content chunk
        """
        # Ensure model is loaded
        if model_id not in self._loaded_models:
            self.load_model(model_id)
        
        loaded = self._loaded_models[model_id]
        
        # Convert ChatMessage to dict format
        msg_dicts = [{"role": m.role, "content": m.content} for m in messages]
        
        with loaded._lock:
            params = {
                'max_tokens': max_tokens,
                'temperature': temperature,
                'top_p': top_p,
                'stream': True
            }
            if stop:
                params['stop'] = stop
            
            loaded.request_count += 1
            
            for chunk in loaded.llm.create_chat_completion(messages=msg_dicts, **params):
                if 'choices' in chunk and len(chunk['choices']) > 0:
                    delta = chunk['choices'][0].get('delta', {})
                    content = delta.get('content', '')
                    if content:
                        loaded.total_tokens_generated += 1
                        yield {'content': content}
    
    def text_completion(self, model_id: str,
                        prompt: str,
                        temperature: float = 0.7,
                        top_p: float = 0.95,
                        max_tokens: int = 2048,
                        stop: Optional[List[str]] = None) -> Dict[str, Any]:
        """
        OpenAI-compatible text completion (non-streaming)
        
        Args:
            model_id: Local model ID
            prompt: Text prompt
            temperature: Sampling temperature
            top_p: Top-p sampling
            max_tokens: Maximum tokens to generate
            stop: Stop sequences
            
        Returns:
            Dict with text, finish_reason, and token counts
        """
        # Ensure model is loaded
        if model_id not in self._loaded_models:
            self.load_model(model_id)
        
        loaded = self._loaded_models[model_id]
        
        with loaded._lock:
            params = {
                'max_tokens': max_tokens,
                'temperature': temperature,
                'top_p': top_p,
                'stream': False
            }
            if stop:
                params['stop'] = stop
            
            loaded.request_count += 1
            
            result = loaded.llm(prompt, **params)
            
            usage = result.get('usage', {})
            loaded.total_tokens_generated += usage.get('completion_tokens', 0)
            
            # Extract response
            text = ""
            finish_reason = "stop"
            
            if 'choices' in result and len(result['choices']) > 0:
                choice = result['choices'][0]
                text = choice.get('text', '')
                finish_reason = choice.get('finish_reason', 'stop')
            
            return {
                'text': text,
                'finish_reason': finish_reason,
                'prompt_tokens': usage.get('prompt_tokens', 0),
                'completion_tokens': usage.get('completion_tokens', 0),
                'total_tokens': usage.get('total_tokens', 0)
            }
    
    def text_completion_stream(self, model_id: str,
                               prompt: str,
                               temperature: float = 0.7,
                               top_p: float = 0.95,
                               max_tokens: int = 2048,
                               stop: Optional[List[str]] = None) -> Generator[Dict[str, Any], None, None]:
        """
        OpenAI-compatible text completion (streaming)
        
        Args:
            model_id: Local model ID
            prompt: Text prompt
            temperature: Sampling temperature
            top_p: Top-p sampling
            max_tokens: Maximum tokens to generate
            stop: Stop sequences
            
        Yields:
            Dict with text chunk
        """
        # Ensure model is loaded
        if model_id not in self._loaded_models:
            self.load_model(model_id)
        
        loaded = self._loaded_models[model_id]
        
        with loaded._lock:
            params = {
                'max_tokens': max_tokens,
                'temperature': temperature,
                'top_p': top_p,
                'stream': True
            }
            if stop:
                params['stop'] = stop
            
            loaded.request_count += 1
            
            for chunk in loaded.llm(prompt, **params):
                if 'choices' in chunk and len(chunk['choices']) > 0:
                    text = chunk['choices'][0].get('text', '')
                    if text:
                        loaded.total_tokens_generated += 1
                        yield {'text': text}
