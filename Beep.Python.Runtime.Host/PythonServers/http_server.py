#!/usr/bin/env python3
"""
Beep.Python Runtime HTTP Server
Provides FastAPI-based HTTP API for Python execution from C#
"""

import argparse
import json
import sys
import traceback
from typing import Any, Dict, Optional
from fastapi import FastAPI, HTTPException
from fastapi.responses import JSONResponse
from pydantic import BaseModel
import uvicorn

# Object handle storage
_handles: Dict[str, Any] = {}
_handle_counter = 0

def get_handle_id() -> str:
    global _handle_counter
    _handle_counter += 1
    return f'handle_{_handle_counter}'

app = FastAPI(title="Beep.Python Runtime HTTP Server", version="1.0.0")

# Request/Response Models
class ImportModuleRequest(BaseModel):
    moduleName: str

class ImportModuleResponse(BaseModel):
    handleId: str

class CreateObjectRequest(BaseModel):
    moduleHandleId: str
    className: str
    args: list = []
    kwargs: dict = {}

class CreateObjectResponse(BaseModel):
    handleId: str
    typeName: str

class CallMethodRequest(BaseModel):
    handleId: str
    methodName: str
    args: list = []
    kwargs: dict = {}

class CallMethodResponse(BaseModel):
    value: Optional[Any] = None
    isHandle: bool = False
    handleId: Optional[str] = None
    typeName: Optional[str] = None

class GetAttributeRequest(BaseModel):
    handleId: str
    attributeName: str

class SetAttributeRequest(BaseModel):
    handleId: str
    attributeName: str
    value: Any

class EvaluateRequest(BaseModel):
    expression: str
    locals: dict = {}

class DisposeRequest(BaseModel):
    handleId: str

class ToFloatArrayRequest(BaseModel):
    handleId: str

class ModuleAvailableRequest(BaseModel):
    moduleName: str

class WrapRequest(BaseModel):
    value: Any

@app.get("/health")
async def health():
    """Health check endpoint"""
    return {"status": "ok"}

@app.post("/import", response_model=ImportModuleResponse)
async def import_module(request: ImportModuleRequest):
    """Import a Python module"""
    try:
        module = __import__(request.moduleName, fromlist=[''])
        handle_id = get_handle_id()
        _handles[handle_id] = module
        return ImportModuleResponse(handleId=handle_id)
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

@app.post("/create")
async def create_object(request: CreateObjectRequest):
    """Create an object from a module class"""
    try:
        module = _handles.get(request.moduleHandleId)
        if not module:
            raise HTTPException(status_code=404, detail=f"Module handle not found: {request.moduleHandleId}")
        
        cls = getattr(module, request.className)
        instance = cls(*request.args, **request.kwargs)
        
        handle_id = get_handle_id()
        _handles[handle_id] = instance
        return {"handleId": handle_id, "typeName": type(instance).__name__}
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

@app.post("/call")
async def call_method(request: CallMethodRequest):
    """Call a method on an object"""
    try:
        obj = _handles.get(request.handleId)
        if not obj:
            raise HTTPException(status_code=404, detail=f"Handle not found: {request.handleId}")
        
        method = getattr(obj, request.methodName)
        result = method(*request.args, **request.kwargs)
        
        # Check if result should be wrapped in a handle
        if hasattr(result, '__dict__') or (hasattr(result, '__iter__') and not isinstance(result, (str, bytes, list, dict, tuple))):
            new_handle_id = get_handle_id()
            _handles[new_handle_id] = result
            return {"isHandle": True, "handleId": new_handle_id, "typeName": type(result).__name__}
        
        return {"value": result}
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"{str(e)}\n{traceback.format_exc()}")

@app.post("/getattr")
async def get_attribute(request: GetAttributeRequest):
    """Get an attribute from an object"""
    try:
        obj = _handles.get(request.handleId)
        if not obj:
            raise HTTPException(status_code=404, detail=f"Handle not found: {request.handleId}")
        
        value = getattr(obj, request.attributeName)
        
        # Wrap complex objects
        if hasattr(value, '__dict__') or (hasattr(value, '__iter__') and not isinstance(value, (str, bytes, list, dict, tuple))):
            new_handle_id = get_handle_id()
            _handles[new_handle_id] = value
            return {"isHandle": True, "handleId": new_handle_id, "typeName": type(value).__name__}
        
        return {"value": value}
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

@app.post("/setattr")
async def set_attribute(request: SetAttributeRequest):
    """Set an attribute on an object"""
    try:
        obj = _handles.get(request.handleId)
        if not obj:
            raise HTTPException(status_code=404, detail=f"Handle not found: {request.handleId}")
        
        setattr(obj, request.attributeName, request.value)
        return {"success": True}
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

@app.post("/eval")
async def evaluate(request: EvaluateRequest):
    """Evaluate a Python expression"""
    try:
        result = eval(request.expression, globals(), request.locals)
        return {"value": result}
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"{str(e)}\n{traceback.format_exc()}")

@app.post("/dispose")
async def dispose(request: DisposeRequest):
    """Dispose of a handle"""
    try:
        if request.handleId in _handles:
            del _handles[request.handleId]
        return {"success": True}
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

@app.post("/tofloatarray")
async def to_float_array(request: ToFloatArrayRequest):
    """Convert object to float array"""
    try:
        obj = _handles.get(request.handleId)
        if not obj:
            raise HTTPException(status_code=404, detail=f"Handle not found: {request.handleId}")
        
        import numpy as np
        arr = np.array(obj).flatten().tolist()
        return {"data": arr}
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

@app.post("/tofloatarray2d")
async def to_float_array_2d(request: ToFloatArrayRequest):
    """Convert object to 2D float array"""
    try:
        obj = _handles.get(request.handleId)
        if not obj:
            raise HTTPException(status_code=404, detail=f"Handle not found: {request.handleId}")
        
        import numpy as np
        arr = np.array(obj).tolist()
        return {"data": arr}
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

@app.post("/module_available")
async def module_available(request: ModuleAvailableRequest):
    """Check if a module is available"""
    try:
        __import__(request.moduleName)
        return {"available": True}
    except ImportError:
        return {"available": False}
    except Exception as e:
        return {"available": False, "error": str(e)}

@app.post("/wrap")
async def wrap_value(request: WrapRequest):
    """Wrap a value in a handle"""
    try:
        handle_id = get_handle_id()
        _handles[handle_id] = request.value
        return {"handleId": handle_id, "typeName": type(request.value).__name__}
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

@app.exception_handler(Exception)
async def global_exception_handler(request, exc):
    """Global exception handler"""
    return JSONResponse(
        status_code=500,
        content={
            "error": str(exc),
            "traceback": traceback.format_exc(),
            "path": str(request.url)
        }
    )

def main():
    parser = argparse.ArgumentParser(description="Beep.Python Runtime HTTP Server")
    parser.add_argument("--host", type=str, default="0.0.0.0", help="Host to bind to")
    parser.add_argument("--port", type=int, default=5678, help="Port to bind to")
    parser.add_argument("--workers", type=int, default=1, help="Number of worker processes")
    args = parser.parse_args()
    
    print(f"Python HTTP server starting on {args.host}:{args.port}", flush=True)
    
    uvicorn.run(
        app,
        host=args.host,
        port=args.port,
        workers=args.workers,
        log_level="info"
    )

if __name__ == '__main__':
    main()
