namespace Beep.Python.RuntimeHost.Services;

/// <summary>
/// Partial class containing the Named Pipe server Python script.
/// </summary>
public partial class PythonServerLauncher
{
    private static string GetPipeServerScript()
    {
        return @"#!/usr/bin/env python3
# Beep.LLM Python Pipe Server
# Provides Named Pipe IPC for Python execution from C#

import argparse
import json
import sys
import traceback

# Object handle storage
_handles = {}
_handle_counter = 0

def get_handle_id():
    global _handle_counter
    _handle_counter += 1
    return f'handle_{_handle_counter}'

def handle_command(command, payload):
    try:
        if command == 'ping':
            return {'status': 'ok'}
        elif command == 'import':
            module_name = payload.get('moduleName')
            module = __import__(module_name, fromlist=[''])
            handle_id = get_handle_id()
            _handles[handle_id] = module
            return {'handleId': handle_id}
        elif command == 'create':
            module_handle_id = payload.get('moduleHandleId')
            class_name = payload.get('className')
            args = payload.get('args') or []
            kwargs = payload.get('kwargs') or {}
            
            module = _handles.get(module_handle_id)
            if not module:
                return {'error': f'Module handle not found: {module_handle_id}'}
            
            cls = getattr(module, class_name)
            instance = cls(*args, **kwargs)
            
            handle_id = get_handle_id()
            _handles[handle_id] = instance
            return {'handleId': handle_id, 'typeName': type(instance).__name__}
        elif command == 'call':
            handle_id = payload.get('handleId')
            method_name = payload.get('methodName')
            args = payload.get('args') or []
            kwargs = payload.get('kwargs') or {}
            
            obj = _handles.get(handle_id)
            if not obj:
                return {'error': f'Handle not found: {handle_id}'}
            
            method = getattr(obj, method_name)
            result = method(*args, **kwargs)
            
            if hasattr(result, '__dict__') or (hasattr(result, '__iter__') and not isinstance(result, (str, bytes, list, dict))):
                new_handle_id = get_handle_id()
                _handles[new_handle_id] = result
                return {'isHandle': True, 'handleId': new_handle_id, 'typeName': type(result).__name__}
            return {'value': result}
        elif command == 'getattr':
            handle_id = payload.get('handleId')
            attr_name = payload.get('attributeName')
            obj = _handles.get(handle_id)
            if not obj:
                return {'error': f'Handle not found: {handle_id}'}
            value = getattr(obj, attr_name)
            if hasattr(value, '__dict__'):
                new_handle_id = get_handle_id()
                _handles[new_handle_id] = value
                return {'isHandle': True, 'handleId': new_handle_id, 'typeName': type(value).__name__}
            return {'value': value}
        elif command == 'setattr':
            handle_id = payload.get('handleId')
            attr_name = payload.get('attributeName')
            value = payload.get('value')
            obj = _handles.get(handle_id)
            if not obj:
                return {'error': f'Handle not found: {handle_id}'}
            setattr(obj, attr_name, value)
            return {'success': True}
        elif command == 'eval':
            expression = payload.get('expression')
            locals_dict = payload.get('locals') or {}
            result = eval(expression, globals(), locals_dict)
            return {'value': result}
        elif command == 'dispose':
            handle_id = payload.get('handleId')
            if handle_id in _handles:
                del _handles[handle_id]
            return {'success': True}
        elif command == 'tofloatarray':
            handle_id = payload.get('handleId')
            obj = _handles.get(handle_id)
            if not obj:
                return {'error': f'Handle not found: {handle_id}'}
            import numpy as np
            arr = np.array(obj).flatten().tolist()
            return {'data': arr}
        elif command == 'tofloatarray2d':
            handle_id = payload.get('handleId')
            obj = _handles.get(handle_id)
            if not obj:
                return {'error': f'Handle not found: {handle_id}'}
            import numpy as np
            arr = np.array(obj).tolist()
            return {'data': arr}
        elif command == 'module_available':
            module_name = payload.get('moduleName')
            try:
                __import__(module_name)
                return {'available': True}
            except ImportError:
                return {'available': False}
        elif command == 'wrap':
            value = payload.get('value')
            handle_id = get_handle_id()
            _handles[handle_id] = value
            return {'handleId': handle_id, 'typeName': type(value).__name__}
        else:
            return {'error': f'Unknown command: {command}'}
    except Exception as e:
        return {'error': str(e), 'traceback': traceback.format_exc()}


def run_windows_pipe_server(pipe_name):
    import win32pipe
    import win32file
    import pywintypes
    
    pipe_path = f'\\\\.\\pipe\\{pipe_name}'
    print(f'Python Pipe server starting on {pipe_path}', flush=True)
    
    while True:
        pipe = win32pipe.CreateNamedPipe(
            pipe_path,
            win32pipe.PIPE_ACCESS_DUPLEX,
            win32pipe.PIPE_TYPE_MESSAGE | win32pipe.PIPE_READMODE_MESSAGE | win32pipe.PIPE_WAIT,
            1, 65536, 65536, 0, None
        )
        
        try:
            win32pipe.ConnectNamedPipe(pipe, None)
            print('Client connected', flush=True)
            
            while True:
                try:
                    result, data = win32file.ReadFile(pipe, 65536)
                    if not data:
                        break
                    
                    request = json.loads(data.decode('utf-8'))
                    command = request.get('command', '')
                    payload = request.get('payload', {})
                    
                    response = handle_command(command, payload)
                    response_bytes = json.dumps(response).encode('utf-8') + b'\n'
                    win32file.WriteFile(pipe, response_bytes)
                except pywintypes.error as e:
                    if e.args[0] == 109:  # Broken pipe
                        break
                    raise
        except Exception as e:
            print(f'Pipe error: {e}', file=sys.stderr)
        finally:
            win32file.CloseHandle(pipe)


def run_unix_socket_server(pipe_name):
    import socket
    import os
    
    socket_path = f'/tmp/{pipe_name}.sock'
    
    # Remove existing socket
    if os.path.exists(socket_path):
        os.unlink(socket_path)
    
    server = socket.socket(socket.AF_UNIX, socket.SOCK_STREAM)
    server.bind(socket_path)
    server.listen(1)
    
    print(f'Python Pipe server started on {socket_path}', flush=True)
    
    while True:
        conn, _ = server.accept()
        try:
            buffer = b''
            while True:
                data = conn.recv(65536)
                if not data:
                    break
                
                buffer += data
                while b'\n' in buffer:
                    line, buffer = buffer.split(b'\n', 1)
                    request = json.loads(line.decode('utf-8'))
                    command = request.get('command', '')
                    payload = request.get('payload', {})
                    
                    response = handle_command(command, payload)
                    response_bytes = json.dumps(response).encode('utf-8') + b'\n'
                    conn.sendall(response_bytes)
        except Exception as e:
            print(f'Socket error: {e}', file=sys.stderr)
        finally:
            conn.close()


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--pipe-name', default='beep-python-pipe')
    args = parser.parse_args()
    
    if sys.platform == 'win32':
        run_windows_pipe_server(args.pipe_name)
    else:
        run_unix_socket_server(args.pipe_name)

if __name__ == '__main__':
    main()
";
    }
}

