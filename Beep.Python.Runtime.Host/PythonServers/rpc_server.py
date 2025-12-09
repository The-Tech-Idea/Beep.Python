#!/usr/bin/env python3
"""
Beep.Python Runtime gRPC Server
Provides gRPC API for Python execution from C#
"""

import argparse
import json
import sys
import traceback
from concurrent import futures
from typing import Any, Dict

try:
    import grpc
    from grpc_reflection.v1alpha import reflection
except ImportError:
    print("ERROR: grpcio is required. Install with: pip install grpcio grpcio-tools", file=sys.stderr, flush=True)
    sys.exit(1)

# Try to import generated protobuf code
try:
    import python_service_pb2
    import python_service_pb2_grpc
except ImportError:
    print("ERROR: Generated protobuf files not found. Run: python -m grpc_tools.protoc -I. --python_out=. --grpc_python_out=. python_service.proto", file=sys.stderr, flush=True)
    sys.exit(1)

# Object handle storage
_handles: Dict[str, Any] = {}
_handle_counter = 0

def get_handle_id() -> str:
    global _handle_counter
    _handle_counter += 1
    return f'handle_{_handle_counter}'

class PythonServiceServicer(python_service_pb2_grpc.PythonServiceServicer):
    """Implementation of PythonService gRPC service"""
    
    def Health(self, request, context):
        """Health check"""
        return python_service_pb2.HealthResponse(status="ok")
    
    def ImportModule(self, request, context):
        """Import a Python module"""
        try:
            module = __import__(request.module_name, fromlist=[''])
            handle_id = get_handle_id()
            _handles[handle_id] = module
            return python_service_pb2.ImportModuleResponse(handle_id=handle_id)
        except Exception as e:
            context.set_code(grpc.StatusCode.INTERNAL)
            context.set_details(f"{str(e)}\n{traceback.format_exc()}")
            return python_service_pb2.ImportModuleResponse()
    
    def CreateObject(self, request, context):
        """Create an object from a module class"""
        try:
            module = _handles.get(request.module_handle_id)
            if not module:
                context.set_code(grpc.StatusCode.NOT_FOUND)
                context.set_details(f"Module handle not found: {request.module_handle_id}")
                return python_service_pb2.CreateObjectResponse()
            
            args = json.loads(request.args_json) if request.args_json else []
            kwargs = json.loads(request.kwargs_json) if request.kwargs_json else {}
            
            cls = getattr(module, request.class_name)
            instance = cls(*args, **kwargs)
            
            handle_id = get_handle_id()
            _handles[handle_id] = instance
            return python_service_pb2.CreateObjectResponse(
                handle_id=handle_id,
                type_name=type(instance).__name__
            )
        except Exception as e:
            context.set_code(grpc.StatusCode.INTERNAL)
            context.set_details(f"{str(e)}\n{traceback.format_exc()}")
            return python_service_pb2.CreateObjectResponse()
    
    def CallMethod(self, request, context):
        """Call a method on an object"""
        try:
            obj = _handles.get(request.handle_id)
            if not obj:
                context.set_code(grpc.StatusCode.NOT_FOUND)
                context.set_details(f"Handle not found: {request.handle_id}")
                return python_service_pb2.CallMethodResponse()
            
            args = json.loads(request.args_json) if request.args_json else []
            kwargs = json.loads(request.kwargs_json) if request.kwargs_json else {}
            
            method = getattr(obj, request.method_name)
            result = method(*args, **kwargs)
            
            # Check if result should be wrapped in a handle
            if hasattr(result, '__dict__') or (hasattr(result, '__iter__') and not isinstance(result, (str, bytes, list, dict, tuple))):
                new_handle_id = get_handle_id()
                _handles[new_handle_id] = result
                return python_service_pb2.CallMethodResponse(
                    handle=python_service_pb2.HandleResult(
                        handle_id=new_handle_id,
                        type_name=type(result).__name__
                    )
                )
            
            return python_service_pb2.CallMethodResponse(
                value_json=json.dumps(result, default=str)
            )
        except Exception as e:
            context.set_code(grpc.StatusCode.INTERNAL)
            context.set_details(f"{str(e)}\n{traceback.format_exc()}")
            return python_service_pb2.CallMethodResponse()
    
    def GetAttribute(self, request, context):
        """Get an attribute from an object"""
        try:
            obj = _handles.get(request.handle_id)
            if not obj:
                context.set_code(grpc.StatusCode.NOT_FOUND)
                context.set_details(f"Handle not found: {request.handle_id}")
                return python_service_pb2.GetAttributeResponse()
            
            value = getattr(obj, request.attribute_name)
            
            # Wrap complex objects
            if hasattr(value, '__dict__') or (hasattr(value, '__iter__') and not isinstance(value, (str, bytes, list, dict, tuple))):
                new_handle_id = get_handle_id()
                _handles[new_handle_id] = value
                return python_service_pb2.GetAttributeResponse(
                    handle=python_service_pb2.HandleResult(
                        handle_id=new_handle_id,
                        type_name=type(value).__name__
                    )
                )
            
            return python_service_pb2.GetAttributeResponse(
                value_json=json.dumps(value, default=str)
            )
        except Exception as e:
            context.set_code(grpc.StatusCode.INTERNAL)
            context.set_details(f"{str(e)}\n{traceback.format_exc()}")
            return python_service_pb2.GetAttributeResponse()
    
    def SetAttribute(self, request, context):
        """Set an attribute on an object"""
        try:
            obj = _handles.get(request.handle_id)
            if not obj:
                context.set_code(grpc.StatusCode.NOT_FOUND)
                context.set_details(f"Handle not found: {request.handle_id}")
                return python_service_pb2.SetAttributeResponse(success=False)
            
            value = json.loads(request.value_json) if request.value_json else None
            setattr(obj, request.attribute_name, value)
            return python_service_pb2.SetAttributeResponse(success=True)
        except Exception as e:
            context.set_code(grpc.StatusCode.INTERNAL)
            context.set_details(f"{str(e)}\n{traceback.format_exc()}")
            return python_service_pb2.SetAttributeResponse(success=False)
    
    def Evaluate(self, request, context):
        """Evaluate a Python expression"""
        try:
            locals_dict = json.loads(request.locals_json) if request.locals_json else {}
            result = eval(request.expression, globals(), locals_dict)
            return python_service_pb2.EvaluateResponse(
                value_json=json.dumps(result, default=str)
            )
        except Exception as e:
            context.set_code(grpc.StatusCode.INTERNAL)
            context.set_details(f"{str(e)}\n{traceback.format_exc()}")
            return python_service_pb2.EvaluateResponse()
    
    def DisposeHandle(self, request, context):
        """Dispose of a handle"""
        try:
            if request.handle_id in _handles:
                del _handles[request.handle_id]
            return python_service_pb2.DisposeHandleResponse(success=True)
        except Exception as e:
            context.set_code(grpc.StatusCode.INTERNAL)
            context.set_details(str(e))
            return python_service_pb2.DisposeHandleResponse(success=False)
    
    def ToFloatArray(self, request, context):
        """Convert object to float array"""
        try:
            obj = _handles.get(request.handle_id)
            if not obj:
                context.set_code(grpc.StatusCode.NOT_FOUND)
                context.set_details(f"Handle not found: {request.handle_id}")
                return python_service_pb2.ToFloatArrayResponse()
            
            import numpy as np
            arr = np.array(obj).flatten().tolist()
            return python_service_pb2.ToFloatArrayResponse(data=arr)
        except Exception as e:
            context.set_code(grpc.StatusCode.INTERNAL)
            context.set_details(f"{str(e)}\n{traceback.format_exc()}")
            return python_service_pb2.ToFloatArrayResponse()
    
    def ToFloatArray2D(self, request, context):
        """Convert object to 2D float array"""
        try:
            obj = _handles.get(request.handle_id)
            if not obj:
                context.set_code(grpc.StatusCode.NOT_FOUND)
                context.set_details(f"Handle not found: {request.handle_id}")
                return python_service_pb2.ToFloatArray2DResponse()
            
            import numpy as np
            arr = np.array(obj).tolist()
            
            result = python_service_pb2.ToFloatArray2DResponse()
            for row in arr:
                float_array = python_service_pb2.FloatArray()
                float_array.values.extend(row)
                result.data.append(float_array)
            
            return result
        except Exception as e:
            context.set_code(grpc.StatusCode.INTERNAL)  
            context.set_details(f"{str(e)}\n{traceback.format_exc()}")
            return python_service_pb2.ToFloatArray2DResponse()
    
    def IsModuleAvailable(self, request, context):
        """Check if a module is available"""
        try:
            __import__(request.module_name)
            return python_service_pb2.IsModuleAvailableResponse(available=True)
        except ImportError:
            return python_service_pb2.IsModuleAvailableResponse(available=False)
        except Exception as e:
            context.set_code(grpc.StatusCode.INTERNAL)
            context.set_details(str(e))
            return python_service_pb2.IsModuleAvailableResponse(available=False)
    
    def WrapValue(self, request, context):
        """Wrap a value in a handle"""
        try:
            value = json.loads(request.value_json) if request.value_json else None
            handle_id = get_handle_id()
            _handles[handle_id] = value
            return python_service_pb2.WrapValueResponse(
                handle_id=handle_id,
                type_name=type(value).__name__
            )
        except Exception as e:
            context.set_code(grpc.StatusCode.INTERNAL)
            context.set_details(f"{str(e)}\n{traceback.format_exc()}")
            return python_service_pb2.WrapValueResponse()


def serve(port: int = 50051):
    """Start the gRPC server"""
    server = grpc.server(futures.ThreadPoolExecutor(max_workers=10))
    python_service_pb2_grpc.add_PythonServiceServicer_to_server(
        PythonServiceServicer(), server
    )
    
    # Enable reflection for debugging
    SERVICE_NAMES = (
        python_service_pb2.DESCRIPTOR.services_by_name['PythonService'].full_name,
        reflection.SERVICE_NAME,
    )
    reflection.enable_server_reflection(SERVICE_NAMES, server)
    
    listen_addr = f'[::]:{port}'
    server.add_insecure_port(listen_addr)
    server.start()
    
    print(f'Python gRPC server started on port {port}', flush=True)
    
    try:
        server.wait_for_termination()
    except KeyboardInterrupt:
        print("Shutting down server...", flush=True)
        server.stop(0)


def main():
    parser = argparse.ArgumentParser(description="Beep.Python Runtime gRPC Server")
    parser.add_argument("--port", type=int, default=50051, help="Port to bind to")
    args = parser.parse_args()
    
    serve(args.port)


if __name__ == '__main__':
    main()
