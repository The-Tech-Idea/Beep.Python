"""
Subprocess Model Wrapper

Manages a model running in a subprocess (in its dedicated virtual environment).
Handles communication via JSON over stdin/stdout.
"""
import os
import json
import subprocess
import threading
import queue
from typing import Optional, Dict, Any, Iterator, Callable
from pathlib import Path
import time


class SubprocessModel:
    """
    Wrapper for a model running in a subprocess
    
    Communicates with the subprocess via JSON messages over stdin/stdout.
    """
    
    def __init__(self, model_id: str, model_path: str, venv_path: str, config: Dict[str, Any]):
        """
        Initialize subprocess model
        
        Args:
            model_id: Unique identifier for the model
            model_path: Path to the model file
            venv_path: Path to the virtual environment
            config: Model configuration (n_ctx, n_gpu_layers, etc.)
        """
        self.model_id = model_id
        self.model_path = model_path
        self.venv_path = venv_path
        self.config = config
        
        self.process: Optional[subprocess.Popen] = None
        self.response_queue = queue.Queue()
        self.reader_thread: Optional[threading.Thread] = None
        self.is_ready = False
        self.is_shutdown = False
        
        # Statistics
        self.start_time = time.time()
        self.request_count = 0
        self.total_tokens = 0
    
    def start(self) -> bool:
        """
        Start the subprocess and load the model
        
        Returns:
            True if successfully started, False otherwise
        """
        try:
            # Get Python executable from venv
            if os.name == 'nt':  # Windows
                python_exe = os.path.join(self.venv_path, 'Scripts', 'python.exe')
            else:  # Unix-like
                python_exe = os.path.join(self.venv_path, 'bin', 'python')
            
            if not os.path.exists(python_exe):
                raise FileNotFoundError(f"Python executable not found: {python_exe}")
            
            # Get inference script path
            from app.config_manager import get_app_directory
            script_dir = get_app_directory() / 'app' / 'scripts'
            script_path = script_dir / 'inference_subprocess.py'
            
            if not script_path.exists():
                raise FileNotFoundError(f"Inference script not found: {script_path}")
            
            # Prepare config JSON
            config_json = json.dumps(self.config)
            
            # Start subprocess
            self.process = subprocess.Popen(
                [python_exe, str(script_path), self.model_path, config_json],
                stdin=subprocess.PIPE,
                stdout=subprocess.PIPE,
                stderr=subprocess.PIPE,
                text=True,
                bufsize=1  # Line buffered
            )
            
            # Start reader thread
            self.reader_thread = threading.Thread(target=self._read_responses, daemon=True)
            self.reader_thread.start()
            
            # Wait for model to be ready
            timeout = 60  # 60 seconds timeout
            start = time.time()
            
            while time.time() - start < timeout:
                try:
                    response = self.response_queue.get(timeout=1)
                    if response.get('type') == 'ready':
                        self.is_ready = True
                        return True
                    elif response.get('type') == 'error':
                        raise Exception(f"Model loading failed: {response.get('error')}")
                except queue.Empty:
                    # Check if process is still alive
                    if self.process.poll() is not None:
                        stderr = self.process.stderr.read() if self.process.stderr else ''
                        raise Exception(f"Subprocess died during startup. Stderr: {stderr}")
            
            raise TimeoutError("Model loading timed out")
            
        except Exception as e:
            self.shutdown()
            raise Exception(f"Failed to start subprocess model: {e}")
    
    def _read_responses(self):
        """Read responses from subprocess stdout (runs in background thread)"""
        try:
            while not self.is_shutdown and self.process and self.process.poll() is None:
                line = self.process.stdout.readline()
                if not line:
                    break
                
                line = line.strip()
                if not line:
                    continue
                
                try:
                    response = json.loads(line)
                    self.response_queue.put(response)
                except json.JSONDecodeError as e:
                    print(f"Failed to parse response: {line}, error: {e}")
        except Exception as e:
            print(f"Error in reader thread: {e}")
    
    def _send_request(self, request: Dict[str, Any]):
        """Send a request to the subprocess"""
        if not self.process or not self.is_ready:
            raise Exception("Model not ready")
        
        request_json = json.dumps(request) + '\n'
        self.process.stdin.write(request_json)
        self.process.stdin.flush()
    
    def complete(self, prompt: str, max_tokens: int = 512, temperature: float = 0.7,
                 stream: bool = False, **kwargs) -> Any:
        """
        Generate text completion
        
        Args:
            prompt: Input prompt
            max_tokens: Maximum tokens to generate
            temperature: Sampling temperature
            stream: Whether to stream the response
            **kwargs: Additional parameters
        
        Returns:
            Completion text (if stream=False) or iterator of tokens (if stream=True)
        """
        self.request_count += 1
        
        request = {
            'type': 'completion',
            'prompt': prompt,
            'max_tokens': max_tokens,
            'temperature': temperature,
            'stream': stream,
            **kwargs
        }
        
        self._send_request(request)
        
        if stream:
            return self._stream_response()
        else:
            response = self._wait_for_response('completion')
            self.total_tokens += response.get('usage', {}).get('total_tokens', 0)
            return response.get('text', '')
    
    def chat(self, messages: list, max_tokens: int = 512, temperature: float = 0.7,
             stream: bool = False, **kwargs) -> Any:
        """
        Generate chat completion
        
        Args:
            messages: List of message dicts with 'role' and 'content'
            max_tokens: Maximum tokens to generate
            temperature: Sampling temperature
            stream: Whether to stream the response
            **kwargs: Additional parameters
        
        Returns:
            Chat message (if stream=False) or iterator of tokens (if stream=True)
        """
        self.request_count += 1
        
        request = {
            'type': 'chat',
            'messages': messages,
            'max_tokens': max_tokens,
            'temperature': temperature,
            'stream': stream,
            **kwargs
        }
        
        self._send_request(request)
        
        if stream:
            return self._stream_response()
        else:
            response = self._wait_for_response('chat_completion')
            self.total_tokens += response.get('usage', {}).get('total_tokens', 0)
            return response.get('message', {}).get('content', '')
    
    def _wait_for_response(self, expected_type: str, timeout: float = 120) -> Dict[str, Any]:
        """Wait for a specific response type"""
        start = time.time()
        
        while time.time() - start < timeout:
            try:
                response = self.response_queue.get(timeout=1)
                
                if response.get('type') == 'error':
                    raise Exception(f"Inference error: {response.get('error')}")
                
                if response.get('type') == expected_type:
                    return response
                    
            except queue.Empty:
                # Check if process is still alive
                if self.process.poll() is not None:
                    raise Exception("Subprocess died unexpectedly")
        
        raise TimeoutError(f"Timeout waiting for response type: {expected_type}")
    
    def _stream_response(self) -> Iterator[str]:
        """Stream tokens from the subprocess"""
        # Wait for stream_start
        response = self._wait_for_response('stream_start', timeout=10)
        
        # Stream tokens
        while True:
            try:
                response = self.response_queue.get(timeout=120)
                
                if response.get('type') == 'error':
                    raise Exception(f"Streaming error: {response.get('error')}")
                
                if response.get('type') == 'stream_end':
                    break
                
                if response.get('type') == 'stream_token':
                    token = response.get('token', '')
                    self.total_tokens += 1
                    yield token
                    
            except queue.Empty:
                if self.process.poll() is not None:
                    raise Exception("Subprocess died during streaming")
                raise TimeoutError("Streaming timeout")
    
    def ping(self) -> bool:
        """Check if subprocess is responsive"""
        try:
            self._send_request({'type': 'ping'})
            response = self._wait_for_response('pong', timeout=5)
            return True
        except:
            return False
    
    def get_stats(self) -> Dict[str, Any]:
        """Get model statistics"""
        return {
            'model_id': self.model_id,
            'uptime_seconds': time.time() - self.start_time,
            'request_count': self.request_count,
            'total_tokens': self.total_tokens,
            'is_ready': self.is_ready,
            'is_alive': self.process is not None and self.process.poll() is None
        }
    
    def shutdown(self):
        """Shutdown the subprocess"""
        self.is_shutdown = True
        
        if self.process:
            try:
                # Try graceful shutdown
                self._send_request({'type': 'unload'})
                self.process.wait(timeout=5)
            except:
                # Force kill if graceful shutdown fails
                self.process.kill()
                self.process.wait()
            
            self.process = None
        
        self.is_ready = False
    
    def __del__(self):
        """Cleanup on deletion"""
        self.shutdown()
