namespace Beep.Python.RuntimeHost.Services;

/// <summary>
/// Partial class containing the HTTP server Python script.
/// </summary>
public partial class PythonServerLauncher
{
    private static string GetHttpServerScript()
    {
        return @"#!/usr/bin/env python3
# Beep.LLM Python HTTP Server
# Provides HTTP API for Python execution from C#

import argparse
import json
import sys
import traceback
from http.server import HTTPServer, BaseHTTPRequestHandler
from urllib.parse import urlparse

# Object handle storage
_handles = {}
_handle_counter = 0

def get_handle_id():
    global _handle_counter
    _handle_counter += 1
    return f'handle_{_handle_counter}'

class PythonAPIHandler(BaseHTTPRequestHandler):
    def log_message(self, format, *args):
        print(f'[HTTP] {format % args}', file=sys.stderr)

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
            path = urlparse(self.path).path
            
            if path == '/import':
                result = self.handle_import(request)
            elif path == '/create':
                result = self.handle_create(request)
            elif path == '/call':
                result = self.handle_call(request)
            elif path == '/getattr':
                result = self.handle_getattr(request)
            elif path == '/setattr':
                result = self.handle_setattr(request)
            elif path == '/eval':
                result = self.handle_eval(request)
            elif path == '/dispose':
                result = self.handle_dispose(request)
            elif path == '/tofloatarray':
                result = self.handle_tofloatarray(request)
            elif path == '/tofloatarray2d':
                result = self.handle_tofloatarray2d(request)
            elif path == '/module_available':
                result = self.handle_module_available(request)
            elif path == '/wrap':
                result = self.handle_wrap(request)
            else:
                result = {'error': f'Unknown endpoint: {path}'}
            
            self.send_json(result)
        except Exception as e:
            self.send_json({'error': str(e), 'traceback': traceback.format_exc()}, 500)

    def handle_import(self, request):
        module_name = request.get('moduleName')
        module = __import__(module_name, fromlist=[''])
        handle_id = get_handle_id()
        _handles[handle_id] = module
        return {'handleId': handle_id}

    def handle_create(self, request):
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

    def handle_call(self, request):
        handle_id = request.get('handleId')
        method_name = request.get('methodName')
        args = request.get('args') or []
        kwargs = request.get('kwargs') or {}
        
        obj = _handles.get(handle_id)
        if not obj:
            return {'error': f'Handle not found: {handle_id}'}
        
        method = getattr(obj, method_name)
        result = method(*args, **kwargs)
        
        # Check if result should be wrapped in a handle
        if hasattr(result, '__dict__') or (hasattr(result, '__iter__') and not isinstance(result, (str, bytes, list, dict))):
            new_handle_id = get_handle_id()
            _handles[new_handle_id] = result
            return {'isHandle': True, 'handleId': new_handle_id, 'typeName': type(result).__name__}
        
        return {'value': result}

    def handle_getattr(self, request):
        handle_id = request.get('handleId')
        attr_name = request.get('attributeName')
        
        obj = _handles.get(handle_id)
        if not obj:
            return {'error': f'Handle not found: {handle_id}'}
        
        value = getattr(obj, attr_name)
        
        # Wrap complex objects
        if hasattr(value, '__dict__') or (hasattr(value, '__iter__') and not isinstance(value, (str, bytes, list, dict))):
            new_handle_id = get_handle_id()
            _handles[new_handle_id] = value
            return {'isHandle': True, 'handleId': new_handle_id, 'typeName': type(value).__name__}
        
        return {'value': value}

    def handle_setattr(self, request):
        handle_id = request.get('handleId')
        attr_name = request.get('attributeName')
        value = request.get('value')
        
        obj = _handles.get(handle_id)
        if not obj:
            return {'error': f'Handle not found: {handle_id}'}
        
        setattr(obj, attr_name, value)
        return {'success': True}

    def handle_eval(self, request):
        expression = request.get('expression')
        locals_dict = request.get('locals') or {}
        
        result = eval(expression, globals(), locals_dict)
        return {'value': result}

    def handle_dispose(self, request):
        handle_id = request.get('handleId')
        if handle_id in _handles:
            del _handles[handle_id]
        return {'success': True}

    def handle_tofloatarray(self, request):
        handle_id = request.get('handleId')
        obj = _handles.get(handle_id)
        if not obj:
            return {'error': f'Handle not found: {handle_id}'}
        
        import numpy as np
        arr = np.array(obj).flatten().tolist()
        return {'data': arr}

    def handle_tofloatarray2d(self, request):
        handle_id = request.get('handleId')
        obj = _handles.get(handle_id)
        if not obj:
            return {'error': f'Handle not found: {handle_id}'}
        
        import numpy as np
        arr = np.array(obj).tolist()
        return {'data': arr}

    def handle_module_available(self, request):
        module_name = request.get('moduleName')
        try:
            __import__(module_name)
            return {'available': True}
        except ImportError:
            return {'available': False}

    def handle_wrap(self, request):
        value = request.get('value')
        handle_id = get_handle_id()
        _handles[handle_id] = value
        return {'handleId': handle_id, 'typeName': type(value).__name__}

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--port', type=int, default=5678)
    args = parser.parse_args()
    
    # Bind to 127.0.0.1 explicitly (localhost can have IPv6 issues)
    server = HTTPServer(('127.0.0.1', args.port), PythonAPIHandler)
    print(f'Python HTTP server started on port {args.port}', flush=True)
    server.serve_forever()

if __name__ == '__main__':
    main()
";
    }
}

