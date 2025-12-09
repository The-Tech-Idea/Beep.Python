namespace Beep.Python.RuntimeHost.Services;

/// <summary>
/// Partial class containing the RPC server Python script.
/// </summary>
public partial class PythonServerLauncher
{
    private static string GetRpcServerScript()
    {
        return @"#!/usr/bin/env python3
# Beep.LLM Python RPC Server
# Provides HTTP/2 RPC for Python execution from C#

import argparse
import json
import sys
import traceback
from http.server import HTTPServer, BaseHTTPRequestHandler

# Object handle storage
_handles = {}
_handle_counter = 0

def get_handle_id():
    global _handle_counter
    _handle_counter += 1
    return f'handle_{_handle_counter}'

class RPCHandler(BaseHTTPRequestHandler):
    def log_message(self, format, *args):
        print(f'[RPC] {format % args}', file=sys.stderr)

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
        else:
            self.send_json({'error': 'Not found'}, 404)

    def do_POST(self):
        content_length = int(self.headers.get('Content-Length', 0))
        body = self.rfile.read(content_length).decode('utf-8')
        
        try:
            request = json.loads(body) if body else {}
            
            # Parse RPC path: /rpc/ServiceName/MethodName
            parts = self.path.strip('/').split('/')
            if len(parts) >= 3 and parts[0] == 'rpc':
                service = parts[1]
                method = parts[2]
                result = self.handle_rpc(service, method, request)
            else:
                result = {'error': f'Invalid RPC path: {self.path}'}
            
            self.send_json(result)
        except Exception as e:
            self.send_json({'error': str(e), 'traceback': traceback.format_exc()}, 500)

    def handle_rpc(self, service, method, request):
        if service != 'PythonService':
            return {'error': f'Unknown service: {service}'}
        
        try:
            if method == 'ImportModule':
                module_name = request.get('moduleName')
                module = __import__(module_name, fromlist=[''])
                handle_id = get_handle_id()
                _handles[handle_id] = module
                return {'handleId': handle_id}
            
            elif method == 'CreateObject':
                module_handle_id = request.get('moduleHandleId')
                class_name = request.get('className')
                args = request.get('args') or []
                kwargs = request.get('kwargs') or {}
                
                module = _handles.get(module_handle_id)
                if not module:
                    return {'error': f'Module handle not found: {module_handle_id}'}
                
                cls = getattr(module, class_name)
                instance = cls(*args, **kwargs)
                
                handle_id = get_handle_id()
                _handles[handle_id] = instance
                return {'handleId': handle_id, 'typeName': type(instance).__name__}
            
            elif method == 'CallMethod':
                handle_id = request.get('handleId')
                method_name = request.get('methodName')
                args = request.get('args') or []
                kwargs = request.get('kwargs') or {}
                
                obj = _handles.get(handle_id)
                if not obj:
                    return {'error': f'Handle not found: {handle_id}'}
                
                m = getattr(obj, method_name)
                result = m(*args, **kwargs)
                
                if hasattr(result, '__dict__') or (hasattr(result, '__iter__') and not isinstance(result, (str, bytes, list, dict))):
                    new_id = get_handle_id()
                    _handles[new_id] = result
                    return {'isHandle': True, 'handleId': new_id, 'typeName': type(result).__name__}
                return {'value': result}
            
            elif method == 'GetAttribute':
                handle_id = request.get('handleId')
                attr_name = request.get('attributeName')
                
                obj = _handles.get(handle_id)
                if not obj:
                    return {'error': f'Handle not found: {handle_id}'}
                
                value = getattr(obj, attr_name)
                if hasattr(value, '__dict__'):
                    new_id = get_handle_id()
                    _handles[new_id] = value
                    return {'isHandle': True, 'handleId': new_id, 'typeName': type(value).__name__}
                return {'value': value}
            
            elif method == 'SetAttribute':
                handle_id = request.get('handleId')
                attr_name = request.get('attributeName')
                value = request.get('value')
                
                obj = _handles.get(handle_id)
                if not obj:
                    return {'error': f'Handle not found: {handle_id}'}
                
                setattr(obj, attr_name, value)
                return {'success': True}
            
            elif method == 'Evaluate':
                expression = request.get('expression')
                locals_dict = request.get('locals') or {}
                result = eval(expression, globals(), locals_dict)
                return {'value': result}
            
            elif method == 'DisposeHandle':
                handle_id = request.get('handleId')
                if handle_id in _handles:
                    del _handles[handle_id]
                return {'success': True}
            
            elif method == 'ToFloatArray':
                handle_id = request.get('handleId')
                obj = _handles.get(handle_id)
                if not obj:
                    return {'error': f'Handle not found: {handle_id}'}
                import numpy as np
                arr = np.array(obj).flatten().tolist()
                return {'data': arr}
            
            elif method == 'ToFloatArray2D':
                handle_id = request.get('handleId')
                obj = _handles.get(handle_id)
                if not obj:
                    return {'error': f'Handle not found: {handle_id}'}
                import numpy as np
                arr = np.array(obj).tolist()
                return {'data': arr}
            
            elif method == 'IsModuleAvailable':
                try:
                    __import__(request.get('moduleName'))
                    return {'available': True}
                except ImportError:
                    return {'available': False}
            
            elif method == 'WrapValue':
                value = request.get('value')
                handle_id = get_handle_id()
                _handles[handle_id] = value
                return {'handleId': handle_id, 'typeName': type(value).__name__}
            
            else:
                return {'error': f'Unknown method: {method}'}
        
        except Exception as e:
            return {'error': str(e), 'traceback': traceback.format_exc()}

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--port', type=int, default=50051)
    args = parser.parse_args()
    
    server = HTTPServer(('localhost', args.port), RPCHandler)
    print(f'Python RPC server started on port {args.port}', flush=True)
    server.serve_forever()

if __name__ == '__main__':
    main()
";
    }
}

