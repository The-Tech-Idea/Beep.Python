"""
Llama Server Manager - HTTP API Communication

Manages llama.cpp server processes and provides HTTP API interface.
LM Studio style - communicate with native server via HTTP, not Python subprocess.

Features:
- Start/stop llama-server processes
- Load/unload models via HTTP API
- Health checks and monitoring
- Port management
"""
import os
import sys
import subprocess
import threading
import time
import json
import socket
import signal
from pathlib import Path
from typing import Dict, List, Optional, Any, Generator, Iterator
from dataclasses import dataclass, asdict
from datetime import datetime
import urllib.request
import urllib.error

from app.services.llama_binary_manager import get_llama_binary_manager


# Default server settings
DEFAULT_PORT = 8080
DEFAULT_HOST = "127.0.0.1"
DEFAULT_CONTEXT_SIZE = 4096
DEFAULT_GPU_LAYERS = -1  # -1 = all layers on GPU


@dataclass
class ServerInstance:
    """Represents a running llama-server instance"""
    model_id: str
    model_path: str
    port: int
    host: str
    pid: int
    status: str
    backend: str
    started_at: str
    context_size: int
    gpu_layers: int
    
    def to_dict(self) -> dict:
        return asdict(self)
    
    @property
    def base_url(self) -> str:
        return f"http://{self.host}:{self.port}"


@dataclass  
class ServerConfig:
    """Configuration for starting a server - updated with latest llama.cpp server options"""
    model_path: str
    port: int = DEFAULT_PORT
    host: str = DEFAULT_HOST
    context_size: int = DEFAULT_CONTEXT_SIZE
    gpu_layers: int = DEFAULT_GPU_LAYERS
    threads: int = 0  # 0 = auto
    batch_size: int = 512
    embedding: bool = False
    parallel: int = 1  # Number of parallel requests
    # New options from latest llama.cpp server
    cont_batching: bool = True  # Continuous batching (default in newer versions)
    flash_attn: bool = False  # Flash attention support
    mmproj: Optional[str] = None  # Multimodal projection file for vision models
    ngl: Optional[int] = None  # Alias for gpu_layers
    numa: bool = False  # NUMA support
    rope_freq_base: Optional[float] = None  # RoPE frequency base
    rope_freq_scale: Optional[float] = None  # RoPE frequency scale
    yarn_scale: Optional[float] = None  # YaRN scaling factor
    yarn_alpha: Optional[float] = None  # YaRN alpha parameter
    low_vram: bool = False  # Low VRAM mode
    no_kv_offload: bool = False  # Disable KV cache offloading
    split_mode: Optional[int] = None  # Split mode (1=layer, 2=row)
    main_gpu: Optional[int] = None  # Main GPU for split mode
    tensor_split: Optional[str] = None  # Tensor split across GPUs (e.g., "4,4")
    use_mmap: bool = True  # Use memory mapping
    use_mlock: bool = False  # Lock memory pages
    n_batch: Optional[int] = None  # Batch size for prompt processing
    n_ubatch: Optional[int] = None  # Physical batch size for prompt processing
    n_keep: Optional[int] = None  # Number of tokens to keep from prompt
    logits_all: bool = False  # Return logits for all tokens
    seed: Optional[int] = None  # Random seed
    
    def to_args(self) -> List[str]:
        """Convert config to command line arguments - updated for latest llama.cpp server"""
        args = [
            '--model', str(self.model_path),
            '--host', self.host,
            '--port', str(self.port),
            '--ctx-size', str(self.context_size),
            '--n-gpu-layers', str(self.gpu_layers),
            '--batch-size', str(self.batch_size),
            '--parallel', str(self.parallel)
        ]
        
        if self.threads > 0:
            args.extend(['--threads', str(self.threads)])
        
        if self.embedding:
            args.append('--embedding')
        
        # New options
        if not self.cont_batching:
            args.append('--no-cont-batching')
        
        if self.flash_attn:
            args.append('--flash-attn')
        
        if self.mmproj:
            args.extend(['--mmproj', str(self.mmproj)])
        
        if self.ngl is not None:
            args.extend(['--ngl', str(self.ngl)])
        
        if self.numa:
            args.append('--numa')
        
        if self.rope_freq_base is not None:
            args.extend(['--rope-freq-base', str(self.rope_freq_base)])
        
        if self.rope_freq_scale is not None:
            args.extend(['--rope-freq-scale', str(self.rope_freq_scale)])
        
        if self.yarn_scale is not None:
            args.extend(['--yarn-scale', str(self.yarn_scale)])
        
        if self.yarn_alpha is not None:
            args.extend(['--yarn-alpha', str(self.yarn_alpha)])
        
        if self.low_vram:
            args.append('--low-vram')
        
        if self.no_kv_offload:
            args.append('--no-kv-offload')
        
        if self.split_mode is not None:
            args.extend(['--split-mode', str(self.split_mode)])
        
        if self.main_gpu is not None:
            args.extend(['--main-gpu', str(self.main_gpu)])
        
        if self.tensor_split:
            args.extend(['--tensor-split', self.tensor_split])
        
        if not self.use_mmap:
            args.append('--no-mmap')
        
        if self.use_mlock:
            args.append('--mlock')
        
        if self.n_batch is not None:
            args.extend(['--n-batch', str(self.n_batch)])
        
        if self.n_ubatch is not None:
            args.extend(['--n-ubatch', str(self.n_ubatch)])
        
        if self.n_keep is not None:
            args.extend(['--n-keep', str(self.n_keep)])
        
        if self.logits_all:
            args.append('--logits-all')
        
        if self.seed is not None:
            args.extend(['--seed', str(self.seed)])
        
        return args


class LlamaServerManager:
    """
    Manages llama.cpp server processes and HTTP API communication.
    
    LM Studio approach:
    1. Start native llama-server executable
    2. Load model into server
    3. Communicate via HTTP API (OpenAI-compatible)
    4. No Python venv needed for inference
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
        
        # Import binary manager
        from app.services.llama_binary_manager import get_llama_binary_manager
        self._binary_manager = get_llama_binary_manager()
        
        # Track running server instances
        self._servers: Dict[str, ServerInstance] = {}
        self._processes: Dict[str, subprocess.Popen] = {}
        
        # Port tracking
        self._used_ports: set = set()
        self._port_range = range(8080, 8180)
        
        # State file for persistence - use app's own folder
        from app.config_manager import get_app_directory
        self._base_path = get_app_directory()
        self._state_file = self._base_path / 'server_state.json'
        
        self._initialized = True
        
        # On startup, kill any orphaned servers from previous session
        self._cleanup_orphaned_servers()
    
    def _cleanup_orphaned_servers(self):
        """Kill any orphaned llama-server processes from previous session"""
        if not self._state_file.exists():
            return
        
        try:
            with open(self._state_file, 'r') as f:
                state = json.load(f)
            
            servers_data = state.get('servers', {})
            
            for model_id, server_data in servers_data.items():
                pid = server_data.get('pid')
                port = server_data.get('port')
                
                # Try to kill the old process
                if pid:
                    try:
                        if sys.platform == 'win32':
                            subprocess.run(['taskkill', '/F', '/PID', str(pid)], 
                                         capture_output=True, timeout=5)
                        else:
                            os.kill(pid, signal.SIGTERM)
                        print(f"[LlamaServerManager] Killed orphaned server (PID {pid}) for {model_id}")
                    except Exception:
                        pass  # Process already dead
            
            # Clear the state file - fresh start
            self._state_file.unlink(missing_ok=True)
            print("[LlamaServerManager] Cleared previous server state - fresh start")
            
        except Exception as e:
            print(f"[LlamaServerManager] Error cleaning up orphaned servers: {e}")
    
    def _find_free_port(self) -> int:
        """Find an available port"""
        for port in self._port_range:
            if port in self._used_ports:
                continue
            try:
                with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
                    s.bind((DEFAULT_HOST, port))
                    return port
            except OSError:
                continue
        raise RuntimeError("No available ports in range")
    
    def start_server(self, model_id: str, model_path: str, 
                     config: Optional[ServerConfig] = None,
                     backend_id: Optional[str] = None) -> Dict[str, Any]:
        """
        Start a llama-server for a model
        
        Args:
            model_id: Unique identifier for this model instance
            model_path: Path to the GGUF model file
            config: Optional server configuration
            backend_id: Optional backend to use (default: active backend)
            
        Returns:
            Result dict with success, server info, or error
        """
        # Check if server already running for this model
        if model_id in self._servers:
            server = self._servers[model_id]
            if self._is_server_healthy(server):
                return {
                    'success': True,
                    'message': 'Server already running',
                    'server': server.to_dict()
                }
            else:
                # Server died, clean up
                self._cleanup_server(model_id)
        
        # Get executable
        executable = self._binary_manager.get_server_executable(backend_id)
        if not executable:
            return {
                'success': False,
                'error': 'No backend installed. Please install a backend first.'
            }
        
        # Validate model path
        model_path = Path(model_path)
        if not model_path.exists():
            return {
                'success': False,
                'error': f'Model file not found: {model_path}'
            }
        
        # Create config if not provided
        if config is None:
            config = ServerConfig(model_path=str(model_path))
        else:
            config.model_path = str(model_path)
        
        # Find a free port
        try:
            port = self._find_free_port()
            config.port = port
        except RuntimeError as e:
            return {'success': False, 'error': str(e)}
        
        # Build command
        cmd = [str(executable)] + config.to_args()
        
        # Start server process
        try:
            # Prepare environment (for CUDA, etc.)
            env = os.environ.copy()
            backend = self._binary_manager.get_active_backend()
            if backend and 'cuda' in backend.id:
                # Ensure CUDA libs are findable
                backend_path = Path(backend.install_path) if backend.install_path else None
                if backend_path and sys.platform == 'win32':
                    env['PATH'] = str(backend_path) + ';' + env.get('PATH', '')
                elif backend_path:
                    env['LD_LIBRARY_PATH'] = str(backend_path) + ':' + env.get('LD_LIBRARY_PATH', '')
            
            # Start the process
            process = subprocess.Popen(
                cmd,
                stdout=subprocess.PIPE,
                stderr=subprocess.PIPE,
                env=env,
                creationflags=subprocess.CREATE_NO_WINDOW if sys.platform == 'win32' else 0
            )
            
            # Wait for server to become ready
            ready = self._wait_for_server(config.host, config.port, timeout=60)
            
            if not ready:
                process.terminate()
                stderr = process.stderr.read().decode() if process.stderr else ""
                return {
                    'success': False,
                    'error': f'Server failed to start within timeout. Stderr: {stderr[:500]}'
                }
            
            # Create server instance
            server = ServerInstance(
                model_id=model_id,
                model_path=str(model_path),
                port=config.port,
                host=config.host,
                pid=process.pid,
                status='running',
                backend=backend.id if backend else 'unknown',
                started_at=datetime.now().isoformat(),
                context_size=config.context_size,
                gpu_layers=config.gpu_layers
            )
            
            # Track server
            self._servers[model_id] = server
            self._processes[model_id] = process
            self._used_ports.add(config.port)
            
            # Save state
            self._save_state()
            
            return {
                'success': True,
                'message': 'Server started successfully',
                'server': server.to_dict()
            }
            
        except Exception as e:
            return {'success': False, 'error': str(e)}
    
    def _wait_for_server(self, host: str, port: int, timeout: int = 60) -> bool:
        """Wait for server to become ready"""
        url = f"http://{host}:{port}/health"
        start_time = time.time()
        
        while time.time() - start_time < timeout:
            try:
                req = urllib.request.Request(url, method='GET')
                with urllib.request.urlopen(req, timeout=2) as resp:
                    if resp.status == 200:
                        return True
            except:
                pass
            time.sleep(0.5)
        
        return False
    
    def _is_server_healthy(self, server: ServerInstance) -> bool:
        """Check if a server is healthy"""
        try:
            url = f"{server.base_url}/health"
            req = urllib.request.Request(url, method='GET')
            with urllib.request.urlopen(req, timeout=5) as resp:
                return resp.status == 200
        except:
            return False
    
    def stop_server(self, model_id: str) -> Dict[str, Any]:
        """Stop a running server and kill the process"""
        if model_id not in self._servers:
            return {'success': False, 'error': 'Server not found'}
        
        server = self._servers[model_id]
        
        try:
            # First try the tracked process
            process = self._processes.get(model_id)
            if process:
                process.terminate()
                try:
                    process.wait(timeout=10)
                except subprocess.TimeoutExpired:
                    process.kill()
            
            # Also try to kill by PID (in case process reference lost)
            if server.pid:
                try:
                    if sys.platform == 'win32':
                        subprocess.run(['taskkill', '/F', '/PID', str(server.pid)], 
                                     capture_output=True, timeout=5)
                    else:
                        os.kill(server.pid, signal.SIGKILL)
                except Exception:
                    pass  # Process already dead
            
            self._cleanup_server(model_id)
            
            print(f"[LlamaServerManager] Stopped server for {model_id}")
            return {'success': True, 'message': 'Server stopped'}
            
        except Exception as e:
            return {'success': False, 'error': str(e)}
    
    def _cleanup_server(self, model_id: str):
        """Clean up server tracking"""
        if model_id in self._servers:
            server = self._servers[model_id]
            self._used_ports.discard(server.port)
            del self._servers[model_id]
        
        if model_id in self._processes:
            del self._processes[model_id]
        
        self._save_state()
    
    def get_server(self, model_id: str) -> Optional[ServerInstance]:
        """Get a server instance by model ID"""
        return self._servers.get(model_id)
    
    def get_running_servers(self) -> List[ServerInstance]:
        """Get all running servers"""
        # Check health of all servers
        healthy = []
        for model_id, server in list(self._servers.items()):
            if self._is_server_healthy(server):
                server.status = 'running'
                healthy.append(server)
            else:
                # Server died
                self._cleanup_server(model_id)
        return healthy
    
    def stop_all_servers(self) -> Dict[str, Any]:
        """Stop all running servers"""
        results = {}
        for model_id in list(self._servers.keys()):
            results[model_id] = self.stop_server(model_id)
        return {'success': True, 'results': results}
    
    # ============== HTTP API Methods ==============
    
    def generate_completion(self, model_id: str, prompt: str,
                           max_tokens: int = 256,
                           temperature: float = 0.7,
                           top_p: float = 0.9,
                           top_k: Optional[int] = None,
                           repeat_penalty: Optional[float] = None,
                           frequency_penalty: Optional[float] = None,
                           presence_penalty: Optional[float] = None,
                           mirostat: Optional[int] = None,
                           mirostat_tau: Optional[float] = None,
                           mirostat_eta: Optional[float] = None,
                           seed: Optional[int] = None,
                           stop: Optional[List[str]] = None,
                           stream: bool = False) -> Dict[str, Any]:
        """
        Generate text completion via HTTP API with extended parameters
        
        Uses OpenAI-compatible /v1/completions endpoint
        Supports latest llama.cpp server parameters
        """
        server = self._servers.get(model_id)
        if not server:
            return {'success': False, 'error': 'Server not running'}
        
        url = f"{server.base_url}/v1/completions"
        
        payload = {
            'prompt': prompt,
            'max_tokens': max_tokens,
            'temperature': temperature,
            'top_p': top_p,
            'stream': stream
        }
        
        # Add extended parameters if provided
        if top_k is not None:
            payload['top_k'] = top_k
        if repeat_penalty is not None:
            payload['repeat_penalty'] = repeat_penalty
        if frequency_penalty is not None:
            payload['frequency_penalty'] = frequency_penalty
        if presence_penalty is not None:
            payload['presence_penalty'] = presence_penalty
        if mirostat is not None:
            payload['mirostat'] = mirostat
        if mirostat_tau is not None:
            payload['mirostat_tau'] = mirostat_tau
        if mirostat_eta is not None:
            payload['mirostat_eta'] = mirostat_eta
        if seed is not None:
            payload['seed'] = seed
        if stop:
            payload['stop'] = stop
        
        try:
            data = json.dumps(payload).encode('utf-8')
            req = urllib.request.Request(
                url,
                data=data,
                headers={'Content-Type': 'application/json'},
                method='POST'
            )
            
            with urllib.request.urlopen(req, timeout=120) as resp:
                result = json.loads(resp.read().decode())
                return {
                    'success': True,
                    'result': result
                }
                
        except urllib.error.HTTPError as e:
            error_body = e.read().decode() if e.fp else str(e)
            return {'success': False, 'error': f'HTTP {e.code}: {error_body}'}
        except Exception as e:
            return {'success': False, 'error': str(e)}
    
    def chat_completion(self, model_id: str, messages: List[Dict[str, str]],
                        max_tokens: int = 256,
                        temperature: float = 0.7,
                        top_p: float = 0.9,
                        top_k: Optional[int] = None,
                        repeat_penalty: Optional[float] = None,
                        frequency_penalty: Optional[float] = None,
                        presence_penalty: Optional[float] = None,
                        mirostat: Optional[int] = None,
                        mirostat_tau: Optional[float] = None,
                        mirostat_eta: Optional[float] = None,
                        seed: Optional[int] = None,
                        stop: Optional[List[str]] = None,
                        stream: bool = False) -> Dict[str, Any]:
        """
        Generate chat completion via HTTP API with extended parameters
        
        Uses OpenAI-compatible /v1/chat/completions endpoint
        Supports latest llama.cpp server parameters
        """
        server = self._servers.get(model_id)
        if not server:
            return {'success': False, 'error': 'Server not running'}
        
        url = f"{server.base_url}/v1/chat/completions"
        
        payload = {
            'messages': messages,
            'max_tokens': max_tokens,
            'temperature': temperature,
            'top_p': top_p,
            'stream': stream
        }
        
        # Add extended parameters if provided
        if top_k is not None:
            payload['top_k'] = top_k
        if repeat_penalty is not None:
            payload['repeat_penalty'] = repeat_penalty
        if frequency_penalty is not None:
            payload['frequency_penalty'] = frequency_penalty
        if presence_penalty is not None:
            payload['presence_penalty'] = presence_penalty
        if mirostat is not None:
            payload['mirostat'] = mirostat
        if mirostat_tau is not None:
            payload['mirostat_tau'] = mirostat_tau
        if mirostat_eta is not None:
            payload['mirostat_eta'] = mirostat_eta
        if seed is not None:
            payload['seed'] = seed
        if stop:
            payload['stop'] = stop
        
        try:
            data = json.dumps(payload).encode('utf-8')
            req = urllib.request.Request(
                url,
                data=data,
                headers={'Content-Type': 'application/json'},
                method='POST'
            )
            
            with urllib.request.urlopen(req, timeout=120) as resp:
                result = json.loads(resp.read().decode())
                return {
                    'success': True,
                    'result': result
                }
                
        except urllib.error.HTTPError as e:
            error_body = e.read().decode() if e.fp else str(e)
            return {'success': False, 'error': f'HTTP {e.code}: {error_body}'}
        except Exception as e:
            return {'success': False, 'error': str(e)}
    
    def chat_completion_stream(self, model_id: str, messages: List[Dict[str, str]],
                               max_tokens: int = 256,
                               temperature: float = 0.7,
                               top_p: float = 0.9,
                               top_k: Optional[int] = None,
                               repeat_penalty: Optional[float] = None,
                               stop: Optional[List[str]] = None) -> Generator[Dict[str, Any], None, None]:
        """
        Stream chat completion responses
        
        Yields chunks of the response as they're generated
        """
        server = self._servers.get(model_id)
        if not server:
            yield {'success': False, 'error': 'Server not running'}
            return
        
        url = f"{server.base_url}/v1/chat/completions"
        
        payload = {
            'messages': messages,
            'max_tokens': max_tokens,
            'temperature': temperature,
            'top_p': top_p,
            'stream': True
        }
        
        if top_k is not None:
            payload['top_k'] = top_k
        if repeat_penalty is not None:
            payload['repeat_penalty'] = repeat_penalty
        if stop:
            payload['stop'] = stop
        
        try:
            import urllib.request
            data = json.dumps(payload).encode('utf-8')
            req = urllib.request.Request(
                url,
                data=data,
                headers={'Content-Type': 'application/json'},
                method='POST'
            )
            
            with urllib.request.urlopen(req, timeout=300) as resp:
                buffer = ""
                for line in resp:
                    line = line.decode('utf-8')
                    buffer += line
                    
                    # Process complete lines
                    while '\n' in buffer:
                        line, buffer = buffer.split('\n', 1)
                        line = line.strip()
                        
                        if line.startswith('data: '):
                            data_str = line[6:]  # Remove 'data: ' prefix
                            if data_str == '[DONE]':
                                return
                            try:
                                chunk = json.loads(data_str)
                                yield {'success': True, 'chunk': chunk}
                            except json.JSONDecodeError:
                                continue
                                
        except Exception as e:
            yield {'success': False, 'error': str(e)}
    
    def get_embeddings(self, model_id: str, input_text: str) -> Dict[str, Any]:
        """
        Get embeddings via HTTP API
        
        Requires server started with --embedding flag
        """
        server = self._servers.get(model_id)
        if not server:
            return {'success': False, 'error': 'Server not running'}
        
        url = f"{server.base_url}/v1/embeddings"
        
        payload = {
            'input': input_text
        }
        
        try:
            data = json.dumps(payload).encode('utf-8')
            req = urllib.request.Request(
                url,
                data=data,
                headers={'Content-Type': 'application/json'},
                method='POST'
            )
            
            with urllib.request.urlopen(req, timeout=60) as resp:
                result = json.loads(resp.read().decode())
                return {
                    'success': True,
                    'result': result
                }
                
        except urllib.error.HTTPError as e:
            error_body = e.read().decode() if e.fp else str(e)
            return {'success': False, 'error': f'HTTP {e.code}: {error_body}'}
        except Exception as e:
            return {'success': False, 'error': str(e)}
    
    def list_models(self, model_id: str) -> Dict[str, Any]:
        """
        List models available on the server
        
        Uses /v1/models endpoint (OpenAI-compatible)
        """
        server = self._servers.get(model_id)
        if not server:
            return {'success': False, 'error': 'Server not running'}
        
        try:
            url = f"{server.base_url}/v1/models"
            req = urllib.request.Request(url, method='GET')
            with urllib.request.urlopen(req, timeout=5) as resp:
                result = json.loads(resp.read().decode())
                return {
                    'success': True,
                    'models': result.get('data', [])
                }
        except Exception as e:
            return {'success': False, 'error': str(e)}
    
    def get_server_info(self, model_id: str) -> Dict[str, Any]:
        """
        Get detailed server information
        
        Uses /info endpoint (llama.cpp specific)
        """
        server = self._servers.get(model_id)
        if not server:
            return {'success': False, 'error': 'Server not running'}
        
        try:
            url = f"{server.base_url}/info"
            req = urllib.request.Request(url, method='GET')
            with urllib.request.urlopen(req, timeout=5) as resp:
                info = json.loads(resp.read().decode())
                return {
                    'success': True,
                    'info': info
                }
        except:
            # Fall back to health endpoint
            return self.get_server_health(model_id)
    
    def get_server_health(self, model_id: str) -> Dict[str, Any]:
        """Get server health status with detailed information"""
        server = self._servers.get(model_id)
        if not server:
            return {'success': False, 'error': 'Server not running'}
        
        try:
            url = f"{server.base_url}/health"
            req = urllib.request.Request(url, method='GET')
            with urllib.request.urlopen(req, timeout=5) as resp:
                health = json.loads(resp.read().decode())
                return {
                    'success': True,
                    'health': health,
                    'server': server.to_dict()
                }
        except Exception as e:
            return {'success': False, 'error': str(e)}
    
    def count_tokens(self, model_id: str, text: str) -> Dict[str, Any]:
        """
        Count tokens in text
        
        Uses /tokenize endpoint (if available) or estimates
        """
        server = self._servers.get(model_id)
        if not server:
            return {'success': False, 'error': 'Server not running'}
        
        # Try /tokenize endpoint (if available in newer versions)
        try:
            url = f"{server.base_url}/tokenize"
            payload = {'content': text}
            data = json.dumps(payload).encode('utf-8')
            req = urllib.request.Request(
                url,
                data=data,
                headers={'Content-Type': 'application/json'},
                method='POST'
            )
            with urllib.request.urlopen(req, timeout=10) as resp:
                result = json.loads(resp.read().decode())
                return {
                    'success': True,
                    'tokens': result.get('tokens', []),
                    'count': len(result.get('tokens', []))
                }
        except:
            # Fallback: estimate tokens (rough approximation: 1 token ≈ 4 characters)
            estimated = len(text) // 4
            return {
                'success': True,
                'count': estimated,
                'estimated': True
            }
    
    def get_server_metrics(self, model_id: str) -> Dict[str, Any]:
        """Get server metrics and stats in Prometheus format"""
        server = self._servers.get(model_id)
        if not server:
            return {'success': False, 'error': 'Server not running'}
        
        # Try /metrics endpoint
        try:
            url = f"{server.base_url}/metrics"
            req = urllib.request.Request(url, method='GET')
            with urllib.request.urlopen(req, timeout=5) as resp:
                # Metrics are in Prometheus format
                metrics_text = resp.read().decode()
                return {
                    'success': True,
                    'metrics': metrics_text,
                    'format': 'prometheus'
                }
        except:
            pass
        
        # Fall back to /health
        return self.get_server_health(model_id)
    
    def _save_state(self):
        """Save server state to file"""
        try:
            state = {
                'servers': {k: v.to_dict() for k, v in self._servers.items()}
            }
            with open(self._state_file, 'w') as f:
                json.dump(state, f, indent=2)
        except:
            pass
    
    def anthropic_chat_completion(self, model_id: str, messages: List[Dict[str, str]],
                                  max_tokens: int = 256,
                                  temperature: float = 0.7,
                                  top_p: float = 0.9,
                                  stream: bool = False) -> Dict[str, Any]:
        """
        Anthropic Messages API compatible chat completion
        
        Uses /v1/chat/completions with Anthropic-style parameters
        """
        # Anthropic API uses similar structure but different parameter names
        # Map to OpenAI format for llama.cpp server
        return self.chat_completion(
            model_id=model_id,
            messages=messages,
            max_tokens=max_tokens,
            temperature=temperature,
            top_p=top_p,
            stream=stream
        )
    
    def reload_model(self, model_id: str, model_path: Optional[str] = None) -> Dict[str, Any]:
        """
        Reload a model without restarting the server
        
        Uses /v1/models/{model}/reload endpoint (if available)
        """
        server = self._servers.get(model_id)
        if not server:
            return {'success': False, 'error': 'Server not running'}
        
        # Try reload endpoint (newer llama.cpp versions)
        try:
            url = f"{server.base_url}/v1/models/{model_id}/reload"
            if model_path:
                payload = {'model_path': model_path}
                data = json.dumps(payload).encode('utf-8')
                req = urllib.request.Request(
                    url,
                    data=data,
                    headers={'Content-Type': 'application/json'},
                    method='POST'
                )
            else:
                req = urllib.request.Request(url, method='POST')
            
            with urllib.request.urlopen(req, timeout=60) as resp:
                result = json.loads(resp.read().decode())
                return {
                    'success': True,
                    'result': result
                }
        except:
            # Fallback: stop and restart server
            self.stop_server(model_id)
            if model_path:
                config = ServerConfig(model_path=model_path)
                return self.start_server(model_id, model_path, config)
            else:
                return {'success': False, 'error': 'Model path required for reload'}
    
    def cleanup_on_shutdown(self):
        """Clean up all servers on application shutdown"""
        self.stop_all_servers()


# Singleton accessor
_manager_instance = None

def get_llama_server_manager() -> LlamaServerManager:
    """Get the singleton LlamaServerManager instance"""
    global _manager_instance
    if _manager_instance is None:
        _manager_instance = LlamaServerManager()
    return _manager_instance
