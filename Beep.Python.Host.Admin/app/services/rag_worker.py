"""
RAG Worker Script
Runs inside the RAG virtual environment (not the main app venv).
Handles FAISS/ChromaDB operations in strict isolation.

Communication:
- Input: JSON from stdin
- Output: JSON to stdout
"""
import sys
import json
import os
import traceback
from pathlib import Path

# Add local directory to path if needed
sys.path.append(os.getcwd())

def get_result(success: bool, data: dict = None, error: str = None) -> dict:
    """Format standard response"""
    res = {'success': success}
    if data:
        res.update(data)
    if error:
        res['error'] = error
    return res

def handle_status():
    """Check installed packages"""
    status = {'faiss': False, 'chromadb': False, 'sentence_transformers': False}
    versions = {}
    
    try:
        import faiss
        status['faiss'] = True
        versions['faiss'] = faiss.__version__
    except ImportError:
        pass

    try:
        import chromadb
        status['chromadb'] = True
        versions['chromadb'] = chromadb.__version__
    except ImportError:
        pass
        
    try:
        import sentence_transformers
        status['sentence_transformers'] = True
        versions['sentence_transformers'] = sentence_transformers.__version__
    except ImportError:
        pass
        
    return get_result(True, {'installed': status, 'versions': versions})

def main():
    try:
        # Read input command from stdin
        if len(sys.argv) > 1 and sys.argv[1].endswith('.json'):
            # Read from file (optional debug mode)
            with open(sys.argv[1], 'r') as f:
                payload = json.load(f)
        else:
            # Read from stdin
            input_str = sys.stdin.read()
            if not input_str.strip():
                return
            payload = json.loads(input_str)
            
        command = payload.get('command')
        params = payload.get('params', {})
        
        if command == 'status':
            response = handle_status()
        elif command == 'echo':
            response = get_result(True, {'echo': params})
        else:
            response = get_result(False, error=f"Unknown command: {command}")
            
        print(json.dumps(response))
        
    except Exception as e:
        print(json.dumps(get_result(False, error=str(e))))

if __name__ == '__main__':
    main()
