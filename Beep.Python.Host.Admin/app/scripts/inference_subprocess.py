"""
Subprocess Inference Script

This script runs in a model's dedicated virtual environment and handles
inference requests via JSON-based IPC (stdin/stdout communication).

Usage:
    python inference_subprocess.py <model_path> <config_json>
"""
import sys
import json
import traceback
from typing import Dict, Any, Optional


def log_error(message: str):
    """Log error to stderr"""
    print(json.dumps({'error': message}), file=sys.stderr, flush=True)


def send_response(data: Dict[str, Any]):
    """Send JSON response to stdout"""
    print(json.dumps(data), flush=True)


def load_model(model_path: str, config: Dict[str, Any]):
    """Load the LLM model"""
    try:
        from llama_cpp import Llama
        
        n_ctx = config.get('n_ctx', 4096)
        n_gpu_layers = config.get('n_gpu_layers', 0)
        n_threads = config.get('n_threads', 4)
        verbose = config.get('verbose', False)
        
        # Detect chat format from model name
        model_name = model_path.lower()
        chat_format = None
        
        if 'deepseek' in model_name:
            chat_format = 'deepseek'
        elif 'mistral' in model_name or 'mixtral' in model_name:
            chat_format = 'mistral-instruct'
        elif 'llama-3' in model_name or 'llama3' in model_name:
            chat_format = 'llama-3'
        elif 'llama-2' in model_name or 'llama2' in model_name:
            chat_format = 'llama-2'
        elif 'qwen' in model_name:
            chat_format = 'chatml'
        elif 'phi' in model_name:
            chat_format = 'chatml'
        elif 'vicuna' in model_name:
            chat_format = 'vicuna'
        elif 'openchat' in model_name:
            chat_format = 'openchat'
        elif 'zephyr' in model_name:
            chat_format = 'zephyr'
        
        # Allow config to override
        chat_format = config.get('chat_format', chat_format)
        
        model_kwargs = {
            'model_path': model_path,
            'n_ctx': n_ctx,
            'n_gpu_layers': n_gpu_layers,
            'n_threads': n_threads,
            'verbose': verbose
        }
        
        if chat_format:
            model_kwargs['chat_format'] = chat_format
        
        model = Llama(**model_kwargs)
        
        return model
    except ImportError as e:
        log_error(f"llama-cpp-python not installed: {e}")
        return None
    except Exception as e:
        log_error(f"Failed to load model: {e}")
        return None


def handle_completion(model, request: Dict[str, Any]):
    """Handle text completion request"""
    prompt = request.get('prompt', '')
    max_tokens = request.get('max_tokens', 512)
    temperature = request.get('temperature', 0.7)
    top_p = request.get('top_p', 0.95)
    stop = request.get('stop', [])
    stream = request.get('stream', False)
    
    try:
        if stream:
            # Streaming response
            send_response({'type': 'stream_start'})
            
            for output in model(
                prompt,
                max_tokens=max_tokens,
                temperature=temperature,
                top_p=top_p,
                stop=stop,
                stream=True
            ):
                token = output['choices'][0]['text']
                send_response({
                    'type': 'stream_token',
                    'token': token
                })
            
            send_response({'type': 'stream_end'})
        else:
            # Non-streaming response
            output = model(
                prompt,
                max_tokens=max_tokens,
                temperature=temperature,
                top_p=top_p,
                stop=stop
            )
            
            send_response({
                'type': 'completion',
                'text': output['choices'][0]['text'],
                'usage': output.get('usage', {})
            })
    except Exception as e:
        send_response({
            'type': 'error',
            'error': str(e),
            'traceback': traceback.format_exc()
        })


def handle_chat(model, request: Dict[str, Any]):
    """Handle chat completion request"""
    messages = request.get('messages', [])
    max_tokens = request.get('max_tokens', 512)
    temperature = request.get('temperature', 0.7)
    top_p = request.get('top_p', 0.95)
    stream = request.get('stream', False)
    
    try:
        if stream:
            # Streaming response
            send_response({'type': 'stream_start'})
            
            for output in model.create_chat_completion(
                messages=messages,
                max_tokens=max_tokens,
                temperature=temperature,
                top_p=top_p,
                stream=True
            ):
                delta = output['choices'][0]['delta']
                if 'content' in delta:
                    send_response({
                        'type': 'stream_token',
                        'token': delta['content']
                    })
            
            send_response({'type': 'stream_end'})
        else:
            # Non-streaming response
            output = model.create_chat_completion(
                messages=messages,
                max_tokens=max_tokens,
                temperature=temperature,
                top_p=top_p
            )
            
            send_response({
                'type': 'chat_completion',
                'message': output['choices'][0]['message'],
                'usage': output.get('usage', {})
            })
    except Exception as e:
        send_response({
            'type': 'error',
            'error': str(e),
            'traceback': traceback.format_exc()
        })


def handle_unload(model):
    """Handle model unload request"""
    try:
        # Python will garbage collect the model
        del model
        send_response({'type': 'unload_success'})
        return None
    except Exception as e:
        send_response({
            'type': 'error',
            'error': f"Failed to unload model: {e}"
        })
        return model


def main():
    """Main inference loop"""
    if len(sys.argv) < 3:
        log_error("Usage: python inference_subprocess.py <model_path> <config_json>")
        sys.exit(1)
    
    model_path = sys.argv[1]
    config_json = sys.argv[2]
    
    try:
        config = json.loads(config_json)
    except json.JSONDecodeError as e:
        log_error(f"Invalid config JSON: {e}")
        sys.exit(1)
    
    # Load model
    send_response({'type': 'loading', 'message': 'Loading model...'})
    model = load_model(model_path, config)
    
    if model is None:
        send_response({'type': 'error', 'error': 'Failed to load model'})
        sys.exit(1)
    
    send_response({'type': 'ready', 'message': 'Model loaded successfully'})
    
    # Main request loop - read from stdin
    try:
        for line in sys.stdin:
            line = line.strip()
            if not line:
                continue
            
            try:
                request = json.loads(line)
                request_type = request.get('type')
                
                if request_type == 'completion':
                    handle_completion(model, request)
                elif request_type == 'chat':
                    handle_chat(model, request)
                elif request_type == 'unload':
                    model = handle_unload(model)
                    if model is None:
                        break
                elif request_type == 'ping':
                    send_response({'type': 'pong'})
                else:
                    send_response({
                        'type': 'error',
                        'error': f'Unknown request type: {request_type}'
                    })
            except json.JSONDecodeError as e:
                send_response({
                    'type': 'error',
                    'error': f'Invalid JSON request: {e}'
                })
            except Exception as e:
                send_response({
                    'type': 'error',
                    'error': str(e),
                    'traceback': traceback.format_exc()
                })
    except KeyboardInterrupt:
        send_response({'type': 'shutdown', 'message': 'Interrupted'})
    except Exception as e:
        log_error(f"Fatal error in main loop: {e}")
        traceback.print_exc(file=sys.stderr)
    finally:
        if model is not None:
            del model


if __name__ == '__main__':
    main()
