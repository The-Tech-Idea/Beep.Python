"""
Python Server Manager Service
Manages Python HTTP/RPC servers
"""
import os
import subprocess
import signal
import psutil
import socket
from pathlib import Path
from dataclasses import dataclass, asdict
from typing import List, Optional, Dict
import platform
import threading
import time


@dataclass
class PythonServer:
    """Represents a running Python server"""
    id: str
    name: str
    port: int
    pid: int
    status: str  # running, stopped, error
    backend_type: str  # http, rpc, pipe
    venv_path: str
    endpoint: str
    uptime_seconds: float
    memory_mb: float
    cpu_percent: float


class ServerManager:
    """Manages Python server processes"""
    
    def __init__(self, base_path: Optional[str] = None):
        # Use app's own folder - no fallback to user home
        if base_path:
            self.base_path = Path(base_path)
        else:
            from app.config_manager import get_app_directory
            self.base_path = get_app_directory()
        self.servers_path = self.base_path / "servers"
        self.servers_path.mkdir(parents=True, exist_ok=True)
        self._servers: Dict[str, dict] = {}
        self._load_servers()
    
    def _load_servers(self):
        """Load server state from disk"""
        state_file = self.servers_path / "servers.json"
        if state_file.exists():
            import json
            try:
                with open(state_file) as f:
                    self._servers = json.load(f)
            except:
                self._servers = {}
    
    def _save_servers(self):
        """Save server state to disk"""
        import json
        state_file = self.servers_path / "servers.json"
        with open(state_file, 'w') as f:
            json.dump(self._servers, f, indent=2)
    
    def list_servers(self) -> List[PythonServer]:
        """List all servers with their current status"""
        servers = []
        
        for server_id, server_info in self._servers.items():
            pid = server_info.get('pid')
            status = "stopped"
            uptime = 0
            memory_mb = 0
            cpu_percent = 0
            
            if pid:
                try:
                    proc = psutil.Process(pid)
                    if proc.is_running():
                        status = "running"
                        uptime = time.time() - proc.create_time()
                        memory_mb = proc.memory_info().rss / (1024 * 1024)
                        cpu_percent = proc.cpu_percent(interval=0.1)
                except psutil.NoSuchProcess:
                    status = "stopped"
            
            servers.append(PythonServer(
                id=server_id,
                name=server_info.get('name', server_id),
                port=server_info.get('port', 0),
                pid=pid or 0,
                status=status,
                backend_type=server_info.get('backend_type', 'http'),
                venv_path=server_info.get('venv_path', ''),
                endpoint=f"http://127.0.0.1:{server_info.get('port', 0)}",
                uptime_seconds=uptime,
                memory_mb=round(memory_mb, 2),
                cpu_percent=round(cpu_percent, 2)
            ))
        
        return servers
    
    def get_available_port(self) -> int:
        """Get an available port"""
        with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
            s.bind(('127.0.0.1', 0))
            return s.getsockname()[1]
    
    def start_server(self, name: str, venv_path: str, backend_type: str = "http",
                    port: Optional[int] = None) -> PythonServer:
        """Start a new Python server"""
        
        # Get port
        port = port or self.get_available_port()
        
        # Get Python executable - try multiple locations
        python_exe = None
        path = Path(venv_path)
        
        # Check if it's a virtual environment (has Scripts/bin folder)
        if platform.system() == "Windows":
            venv_exe = path / "Scripts" / "python.exe"
            # Also check if it's just a folder containing python.exe directly
            direct_exe = path / "python.exe"
        else:
            venv_exe = path / "bin" / "python"
            direct_exe = path / "python"
        
        if venv_exe.exists():
            python_exe = venv_exe
        elif direct_exe.exists():
            python_exe = direct_exe
        else:
            # Maybe it's a system python path (e.g., C:\Users\...\Programs\Python\Python313)
            if platform.system() == "Windows":
                system_exe = path / "python.exe"
            else:
                system_exe = path / "python3"
            if system_exe.exists():
                python_exe = system_exe
        
        if not python_exe or not python_exe.exists():
            raise ValueError(f"Python executable not found in: {venv_path}")
        
        # Create server script
        script_path = self._create_server_script(backend_type)
        
        # Start server process
        log_file = self.servers_path / f"{name}-{port}.log"
        log_handle = open(log_file, 'w')
        
        try:
            creationflags = subprocess.CREATE_NO_WINDOW if platform.system() == "Windows" else 0
            process = subprocess.Popen(
                [str(python_exe), str(script_path), "--port", str(port)],
                stdout=log_handle,
                stderr=subprocess.STDOUT,
                creationflags=creationflags
            )
        except Exception as e:
            log_handle.close()
            raise RuntimeError(f"Failed to start Python process: {e}")
        
        server_id = f"{name}-{port}"
        
        # Store server info
        self._servers[server_id] = {
            "name": name,
            "port": port,
            "pid": process.pid,
            "backend_type": backend_type,
            "venv_path": venv_path,
            "started_at": time.time()
        }
        self._save_servers()
        
        # Wait for server to be ready
        time.sleep(1)  # Give process time to start
        
        if not self._wait_for_server(port, timeout=15):
            # Read log for error details
            error_msg = "Server failed to start"
            try:
                with open(log_file, 'r') as f:
                    log_content = f.read()
                if log_content:
                    error_msg = f"Server failed to start. Log: {log_content[:500]}"
            except:
                pass
            self.stop_server(server_id)
            raise RuntimeError(error_msg)
        
        return self.get_server(server_id)
    
    def _wait_for_server(self, port: int, timeout: int = 30) -> bool:
        """Wait for server to be ready"""
        import httpx
        
        start_time = time.time()
        while time.time() - start_time < timeout:
            try:
                response = httpx.get(f"http://127.0.0.1:{port}/health", timeout=2)
                if response.status_code == 200:
                    return True
            except:
                pass
            time.sleep(0.5)
        
        return False
    
    def _create_server_script(self, backend_type: str) -> Path:
        """Create or get the server script"""
        script_path = self.servers_path / f"{backend_type}_server.py"
        
        if not script_path.exists():
            if backend_type == "http":
                script_content = self._get_http_server_script()
            elif backend_type == "rpc":
                script_content = self._get_rpc_server_script()
            else:
                raise ValueError(f"Unknown backend type: {backend_type}")
            
            script_path.write_text(script_content)
        
        return script_path
    
    def _get_http_server_script(self) -> str:
        """Get the HTTP server Python script"""
        return '''#!/usr/bin/env python3
"""Beep.Python HTTP Server"""
import argparse
import json
import sys
import traceback
from http.server import HTTPServer, BaseHTTPRequestHandler

_handles = {}
_handle_counter = 0

def get_handle_id():
    global _handle_counter
    _handle_counter += 1
    return f'handle_{_handle_counter}'

class PythonAPIHandler(BaseHTTPRequestHandler):
    def log_message(self, format, *args):
        pass  # Suppress logging
    
    def send_json(self, data, status=200):
        response = json.dumps(data).encode('utf-8')
        self.send_response(status)
        self.send_header('Content-Type', 'application/json')
        self.send_header('Content-Length', len(response))
        self.end_headers()
        self.wfile.write(response)
    
    def do_GET(self):
        if self.path == '/health':
            self.send_json({'status': 'ok'})
        elif self.path == '/info':
            import platform
            self.send_json({
                'python_version': sys.version,
                'platform': platform.platform(),
                'handles': len(_handles)
            })
        else:
            self.send_json({'error': 'Not found'}, 404)
    
    def do_POST(self):
        content_length = int(self.headers.get('Content-Length', 0))
        body = self.rfile.read(content_length).decode('utf-8')
        
        try:
            request = json.loads(body) if body else {}
            path = self.path
            
            if path == '/exec':
                result = self.handle_exec(request)
            elif path == '/import':
                result = self.handle_import(request)
            elif path == '/eval':
                result = self.handle_eval(request)
            else:
                result = {'error': f'Unknown endpoint: {path}'}
            
            self.send_json(result)
        except Exception as e:
            self.send_json({'error': str(e), 'traceback': traceback.format_exc()}, 500)
    
    def handle_exec(self, request):
        code = request.get('code', '')
        globals_dict = {}
        locals_dict = {}
        exec(code, globals_dict, locals_dict)
        return {'success': True, 'locals': {k: str(v) for k, v in locals_dict.items()}}
    
    def handle_import(self, request):
        module_name = request.get('module')
        module = __import__(module_name, fromlist=[''])
        handle_id = get_handle_id()
        _handles[handle_id] = module
        return {'handleId': handle_id}
    
    def handle_eval(self, request):
        expr = request.get('expression', '')
        result = eval(expr)
        return {'result': str(result), 'type': type(result).__name__}

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--port', type=int, default=5678)
    args = parser.parse_args()
    
    server = HTTPServer(('127.0.0.1', args.port), PythonAPIHandler)
    print(f'Python HTTP server started on port {args.port}', flush=True)
    server.serve_forever()

if __name__ == '__main__':
    main()
'''
    
    def _get_rpc_server_script(self) -> str:
        """Get the RPC server Python script"""
        return '''#!/usr/bin/env python3
"""Beep.Python RPC Server using JSON-RPC"""
import argparse
import json
from http.server import HTTPServer, BaseHTTPRequestHandler

class RPCHandler(BaseHTTPRequestHandler):
    def log_message(self, format, *args):
        pass
    
    def do_GET(self):
        if self.path == '/health':
            self.send_response(200)
            self.send_header('Content-Type', 'application/json')
            self.end_headers()
            self.wfile.write(json.dumps({'status': 'ok'}).encode())
    
    def do_POST(self):
        content_length = int(self.headers.get('Content-Length', 0))
        body = self.rfile.read(content_length).decode('utf-8')
        
        try:
            request = json.loads(body)
            method = request.get('method')
            params = request.get('params', {})
            
            result = self.dispatch(method, params)
            
            response = {
                'jsonrpc': '2.0',
                'result': result,
                'id': request.get('id')
            }
        except Exception as e:
            response = {
                'jsonrpc': '2.0',
                'error': {'code': -32000, 'message': str(e)},
                'id': request.get('id') if 'request' in dir() else None
            }
        
        self.send_response(200)
        self.send_header('Content-Type', 'application/json')
        self.end_headers()
        self.wfile.write(json.dumps(response).encode())
    
    def dispatch(self, method, params):
        if method == 'exec':
            exec(params.get('code', ''))
            return {'success': True}
        elif method == 'eval':
            return {'result': str(eval(params.get('expression', '')))}
        else:
            raise ValueError(f'Unknown method: {method}')

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--port', type=int, default=5678)
    args = parser.parse_args()
    
    server = HTTPServer(('127.0.0.1', args.port), RPCHandler)
    print(f'Python RPC server started on port {args.port}', flush=True)
    server.serve_forever()

if __name__ == '__main__':
    main()
'''
    
    def stop_server(self, server_id: str) -> bool:
        """Stop a running server"""
        if server_id not in self._servers:
            raise ValueError(f"Server '{server_id}' not found")
        
        server_info = self._servers[server_id]
        pid = server_info.get('pid')
        
        if pid:
            try:
                proc = psutil.Process(pid)
                proc.terminate()
                proc.wait(timeout=5)
            except psutil.NoSuchProcess:
                pass
            except psutil.TimeoutExpired:
                try:
                    proc.kill()
                except:
                    pass
        
        del self._servers[server_id]
        self._save_servers()
        return True
    
    def get_server(self, server_id: str) -> Optional[PythonServer]:
        """Get server by ID"""
        servers = self.list_servers()
        for server in servers:
            if server.id == server_id:
                return server
        return None
    
    def get_server_logs(self, server_id: str, lines: int = 100) -> List[str]:
        """Get server logs"""
        log_file = self.servers_path / f"{server_id}.log"
        if log_file.exists():
            with open(log_file) as f:
                return f.readlines()[-lines:]
        return []
    
    def to_dict_list(self, servers: List[PythonServer]) -> List[dict]:
        """Convert servers to dictionary list"""
        return [asdict(s) for s in servers]
