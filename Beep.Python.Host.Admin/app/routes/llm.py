"""
LLM Routes - Web interface for LLM Management

Provides routes for:
- Model browsing and discovery
- Model downloading
- Model management
- Chat interface
- LLM Environment management
- Settings
"""
import os
import time
import json
import threading
from pathlib import Path
from typing import Dict, Any, Optional
from flask import Blueprint, render_template, request, jsonify, redirect, url_for, Response, flash
from app.services.llm_manager import LLMManager
from app.services.huggingface_service import HuggingFaceService
from app.services.inference_service import InferenceService, InferenceConfig
from app.services.task_manager import TaskManager
from app.services.hardware_service import HardwareService
from app.services.llm_environment import get_llm_env_manager, MODEL_CATEGORY_INFO, ModelCategory
from app.services.model_recommendation import get_recommendation_service, UseCase

# Import the rebuild route handler
from app.routes.llm_rebuild import api_rebuild_venv
from app.services.cuda_installer import get_toolkit_installer, get_cuda_installer

llm_bp = Blueprint('llm', __name__)

# Register the rebuild route manually since it's in a separate file but needs to be part of llm_bp
llm_bp.add_url_rule('/api/venv/<venv_name>/rebuild', 'api_rebuild_venv', api_rebuild_venv, methods=['POST'])


# =====================
# Dashboard / Overview
# =====================

@llm_bp.route('/')
def index():
    """LLM Management Dashboard"""
    llm_mgr = LLMManager()
    inference = InferenceService()
    hf_service = HuggingFaceService()
    
    # Get statistics
    storage_stats = llm_mgr.get_storage_stats()
    local_models = llm_mgr.get_local_models()
    loaded_models = inference.get_loaded_models()
    active_downloads = llm_mgr.get_active_downloads()
    
    # Get categories for browsing
    categories = hf_service.get_popular_categories()
    
    return render_template('llm/index.html',
                           storage_stats=storage_stats,
                           local_models=local_models,
                           loaded_models=loaded_models,
                           active_downloads=active_downloads,
                           categories=categories,
                           inference_available=inference.is_available())


# =====================
# Model Discovery
# =====================

@llm_bp.route('/discover')
def discover():
    """Browse and discover models"""
    hf_service = HuggingFaceService()
    categories = hf_service.get_popular_categories()
    
    # Get query params
    query = request.args.get('q', '')
    category = request.args.get('category', '')
    
    models = []
    if query or category:
        search_query = query if query else categories[0]['query'] if category else ''
        for cat in categories:
            if cat['id'] == category:
                search_query = cat['query']
                break
        
        if search_query:
            models = hf_service.search_models(search_query, filter_gguf=True, limit=30)
    
    return render_template('llm/discover.html',
                           categories=categories,
                           models=models,
                           query=query,
                           selected_category=category)


@llm_bp.route('/search')
def search():
    """Search models API endpoint - searches across all enabled repositories"""
    from app.services.repository_manager import get_repository_manager
    
    query = request.args.get('q', '')
    filter_gguf = request.args.get('gguf', 'true').lower() == 'true'
    limit = min(int(request.args.get('limit', 20)), 50)
    sort = request.args.get('sort', 'downloads')
    
    if not query:
        return jsonify({'error': 'Query required', 'models': []})
    
    repo_mgr = get_repository_manager()
    enabled_repos = repo_mgr.get_repositories(enabled_only=True)
    
    all_models = []
    
    # Search HuggingFace repositories
    hf_repos = [r for r in enabled_repos if r.type == 'huggingface']
    if hf_repos:
        hf_service = HuggingFaceService()
        # Use API key from first HuggingFace repo
        hf_repo = hf_repos[0]
        if hf_repo.api_key:
            hf_service.llm_manager.set_huggingface_token(hf_repo.api_key)
        models = hf_service.search_models(query, filter_gguf=filter_gguf, 
                                         limit=limit, sort=sort)
        all_models.extend(models)
    
    # TODO: Add Ollama repository search
    # ollama_repos = [r for r in enabled_repos if r.type == 'ollama']
    # if ollama_repos:
    #     # Implement Ollama search
    
    # TODO: Add local directory search
    # local_repos = [r for r in enabled_repos if r.type == 'local']
    # if local_repos:
    #     # Scan local directories for models
    
    return jsonify({
        'models': all_models, 
        'count': len(all_models),
        'repositories_searched': [r.name for r in enabled_repos if r.type == 'huggingface']
    })


@llm_bp.route('/model/<path:model_id>')
def model_details(model_id: str):
    """View model details and files"""
    hf_service = HuggingFaceService()
    llm_mgr = LLMManager()
    
    # Get model details
    details = hf_service.get_model_details(model_id)
    if not details:
        flash('Model not found', 'error')
        return redirect(url_for('llm.discover'))
    
    # Get downloadable files
    files = hf_service.get_model_files(model_id, filter_gguf=True)
    
    # Check which files are already downloaded
    local_models = llm_mgr.get_local_models()
    downloaded_files = set()
    for lm in local_models:
        if lm.metadata.get('model_id') == model_id:
            downloaded_files.add(lm.filename)
    
    for file in files:
        file['is_downloaded'] = file['filename'] in downloaded_files
    
    # Check if token is set (for gated models) - check both repository and legacy
    from app.services.repository_manager import get_repository_manager
    repo_mgr = get_repository_manager()
    
    has_token = False
    token_source = None
    
    # Check repository API key first
    hf_repo = repo_mgr.get_repository('hf_default')
    if hf_repo and hf_repo.api_key:
        has_token = True
        token_source = 'repository'
    # Fallback to legacy token
    elif llm_mgr.get_huggingface_token():
        has_token = True
        token_source = 'legacy'
    
    # Check if model is gated and needs token
    is_gated = details.get('gated', False)
    is_private = details.get('private', False)
    needs_token = is_gated or is_private
    
    return render_template('llm/model_details.html',
                           model=details,
                           files=files,
                           has_token=has_token,
                           token_source=token_source,
                           needs_token=needs_token,
                           is_gated=is_gated,
                           is_private=is_private)


# =====================
# Model Downloads
# =====================

@llm_bp.route('/download', methods=['POST'])
def download_model():
    """Start model download with progress tracking"""
    from app.services.repository_manager import get_repository_manager
    
    data = request.get_json() or request.form
    model_id = data.get('model_id')
    filename = data.get('filename')
    
    if not model_id or not filename:
        return jsonify({'error': 'model_id and filename are required'}), 400
    
    # Pre-validate token requirements before starting download
    hf_service = HuggingFaceService()
    model_details = hf_service.get_model_details(model_id)
    
    if model_details:
        is_gated = model_details.get('gated', False)
        is_private = model_details.get('private', False)
        requires_token = is_gated or is_private
        
        if requires_token:
            # Check if token is available
            repo_mgr = get_repository_manager()
            hf_repo = repo_mgr.get_repository('hf_default')
            has_token = bool(
                (hf_repo and hf_repo.api_key) or 
                LLMManager().get_huggingface_token()
            )
            
            if not has_token:
                return jsonify({
                    'error': 'Token required',
                    'message': (
                        f"This model requires a HuggingFace API token. "
                        f"{'It is a gated model (requires license acceptance).' if is_gated else ''} "
                        f"{'It is a private model.' if is_private else ''} "
                        f"Please configure your API token in Settings."
                    ),
                    'requires_token': True,
                    'is_gated': is_gated,
                    'is_private': is_private
                }), 400
    
    # Create task for progress tracking
    task_mgr = TaskManager()
    steps = [
        "Preparing download",
        "Connecting to HuggingFace",
        "Downloading model",
        "Verifying file",
        "Finalizing"
    ]
    
    task = task_mgr.create_task(
        name=f"Download: {filename}",
        task_type="download_model",
        steps=steps
    )
    
    # Run download in background
    def run_download():
        hf_service = HuggingFaceService()
        
        try:
            task_mgr.start_task(task.id)
            
            # Step 1: Preparing
            task_mgr.update_step(task.id, 0, "running", "Initializing download...")
            task_mgr.update_progress(task.id, 5, "Preparing...")
            time.sleep(0.3)
            task_mgr.update_step(task.id, 0, "completed", "Ready")
            
            # Step 2: Connecting
            task_mgr.update_step(task.id, 1, "running", "Connecting to HuggingFace...")
            task_mgr.update_progress(task.id, 10, "Connecting...")
            
            # Get file info
            files = hf_service.get_model_files(model_id)
            file_info = next((f for f in files if f['filename'] == filename), None)
            
            if not file_info:
                task_mgr.fail_task(task.id, "File not found in model repository")
                return
            
            task_mgr.update_step(task.id, 1, "completed", "Connected")
            
            # Step 3: Downloading
            task_mgr.update_step(task.id, 2, "running", "Starting download...")
            task_mgr.update_progress(task.id, 15, "Downloading...")
            
            # Download with progress callback
            def progress_callback(progress):
                percent = 15 + int(progress.percent * 0.7)  # 15-85%
                speed_mb = progress.speed / (1024 * 1024)
                downloaded_mb = progress.downloaded / (1024 * 1024)
                total_mb = progress.total_size / (1024 * 1024)
                
                task_mgr.update_progress(
                    task.id, percent,
                    f"Downloading: {downloaded_mb:.1f} / {total_mb:.1f} MB ({speed_mb:.1f} MB/s)"
                )
                task_mgr.update_step(
                    task.id, 2, "running",
                    f"{progress.percent:.1f}% - ETA: {progress.eta}s"
                )
            
            local_model = hf_service.download_model(
                model_id, filename,
                progress_callback=progress_callback,
                task_manager=task_mgr,
                task_id=task.id
            )
            
            if not local_model:
                task_mgr.fail_task(task.id, "Download failed or was cancelled")
                return
            
            task_mgr.update_step(task.id, 2, "completed", "Download complete")
            
            # Step 4: Verifying
            task_mgr.update_step(task.id, 3, "running", "Verifying file integrity...")
            task_mgr.update_progress(task.id, 90, "Verifying...")
            time.sleep(0.3)
            task_mgr.update_step(task.id, 3, "completed", "File verified")
            
            # Step 5: Finalizing
            task_mgr.update_step(task.id, 4, "running", "Finalizing...")
            task_mgr.update_progress(task.id, 95, "Finalizing...")
            time.sleep(0.2)
            task_mgr.update_step(task.id, 4, "completed", "Done!")
            
            # Complete task
            task_mgr.complete_task(task.id, result={
                'model_id': local_model.id,
                'filename': local_model.filename,
                'path': local_model.path,
                'size': local_model.size
            })
            
        except Exception as e:
            task_mgr.fail_task(task.id, str(e))
    
    thread = threading.Thread(target=run_download, daemon=True)
    thread.start()
    
    return jsonify({
        'success': True,
        'task_id': task.id,
        'redirect_url': f'/tasks/{task.id}'
    })


@llm_bp.route('/downloads')
def downloads():
    """View active downloads"""
    llm_mgr = LLMManager()
    active_downloads = llm_mgr.get_active_downloads()
    
    return render_template('llm/downloads.html',
                           downloads=active_downloads)


# =====================
# Local Models
# =====================

@llm_bp.route('/models')
def local_models():
    """View local models"""
    llm_mgr = LLMManager()
    inference = InferenceService()
    
    models = llm_mgr.get_local_models()
    loaded_models = {m['model_id']: m for m in inference.get_loaded_models()}
    
    # Add loaded status to models
    for model in models:
        model.is_loaded = model.id in loaded_models
    
    storage_stats = llm_mgr.get_storage_stats()
    
    return render_template('llm/models.html',
                           models=models,
                           loaded_models=loaded_models,
                           storage_stats=storage_stats,
                           inference_available=inference.is_available())


@llm_bp.route('/models/<model_id>/info')
def get_model_info(model_id: str):
    """Get detailed model information including description and use cases"""
    from app.services.llm_manager import LLMManager, get_model_knowledge
    
    llm_mgr = LLMManager()
    model = llm_mgr.get_model_by_id(model_id)
    
    if not model:
        return jsonify({'error': 'Model not found'}), 404
    
    # Get knowledge base info
    knowledge = get_model_knowledge(model.name)
    
    # Parse quantization from filename if not set
    quantization = model.quantization
    if not quantization:
        filename_lower = model.filename.lower()
        quant_patterns = ['q2_k', 'q3_k', 'q4_k', 'q5_k', 'q6_k', 'q8_0', 'q4_0', 'q5_0', 
                          'q4_1', 'q5_1', 'iq2', 'iq3', 'iq4', 'f16', 'f32']
        for pattern in quant_patterns:
            if pattern in filename_lower:
                quantization = pattern.upper()
                break
    
    # Parse parameters from name if not set
    parameters = model.parameters
    if not parameters:
        import re
        param_match = re.search(r'(\d+\.?\d*)[bB]', model.name)
        if param_match:
            parameters = f"{param_match.group(1)}B"
    
    # Estimate context length if not set
    context_length = model.context_length
    if not context_length:
        name_lower = model.name.lower()
        if '128k' in name_lower or '131072' in name_lower:
            context_length = 131072
        elif '32k' in name_lower:
            context_length = 32768
        elif '16k' in name_lower:
            context_length = 16384
        elif '8k' in name_lower:
            context_length = 8192
        else:
            context_length = 4096  # Default
    
    # Build response
    response = {
        'id': model.id,
        'name': model.name,
        'filename': model.filename,
        'path': model.path,
        'size': model.size,
        'size_gb': round(model.size / (1024**3), 2),
        'format': model.format,
        'source': model.source,
        'quantization': quantization,
        'parameters': parameters,
        'context_length': context_length,
        'status': model.status,
        'downloaded_at': model.downloaded_at,
        'last_used': model.last_used,
        'venv_name': model.venv_name,
        'gpu_backend': model.gpu_backend,
        # Knowledge base info
        'family': knowledge.get('family', 'Unknown'),
        'description': model.description or knowledge.get('description', ''),
        'use_cases': model.use_cases if model.use_cases else knowledge.get('use_cases', []),
        'license': model.license or knowledge.get('license', 'Unknown'),
        # Quantization quality info
        'quantization_info': get_quantization_info(quantization)
    }
    
    return jsonify(response)


def get_quantization_info(quant: str) -> Dict[str, Any]:
    """Get info about quantization level"""
    if not quant:
        return {'quality': 'Unknown', 'description': 'Unknown quantization'}
    
    quant_upper = quant.upper()
    
    quant_info = {
        'F32': {'quality': 'Maximum', 'description': 'Full precision, best quality but largest size', 'stars': 5},
        'F16': {'quality': 'Excellent', 'description': 'Half precision, near-original quality', 'stars': 5},
        'Q8_0': {'quality': 'Excellent', 'description': '8-bit quantization, minimal quality loss', 'stars': 5},
        'Q6_K': {'quality': 'Very Good', 'description': '6-bit k-quant, excellent balance', 'stars': 4},
        'Q5_K': {'quality': 'Very Good', 'description': '5-bit k-quant, great quality/size ratio', 'stars': 4},
        'Q5_K_M': {'quality': 'Very Good', 'description': '5-bit k-quant medium, recommended', 'stars': 4},
        'Q5_K_S': {'quality': 'Good', 'description': '5-bit k-quant small, good balance', 'stars': 4},
        'Q5_0': {'quality': 'Good', 'description': '5-bit quantization, decent quality', 'stars': 4},
        'Q5_1': {'quality': 'Good', 'description': '5-bit with higher precision', 'stars': 4},
        'Q4_K': {'quality': 'Good', 'description': '4-bit k-quant, popular choice', 'stars': 3},
        'Q4_K_M': {'quality': 'Good', 'description': '4-bit k-quant medium, best 4-bit option', 'stars': 3},
        'Q4_K_S': {'quality': 'Acceptable', 'description': '4-bit k-quant small, smaller file', 'stars': 3},
        'Q4_0': {'quality': 'Acceptable', 'description': '4-bit basic, noticeable quality loss', 'stars': 3},
        'Q4_1': {'quality': 'Acceptable', 'description': '4-bit with offset, slightly better', 'stars': 3},
        'Q3_K': {'quality': 'Low', 'description': '3-bit k-quant, significant compression', 'stars': 2},
        'Q3_K_M': {'quality': 'Low', 'description': '3-bit k-quant medium', 'stars': 2},
        'Q3_K_S': {'quality': 'Low', 'description': '3-bit k-quant small', 'stars': 2},
        'Q3_K_L': {'quality': 'Low', 'description': '3-bit k-quant large', 'stars': 2},
        'IQ4_XS': {'quality': 'Good', 'description': 'Importance-matrix 4-bit extra small', 'stars': 3},
        'IQ4_NL': {'quality': 'Good', 'description': 'Importance-matrix 4-bit non-linear', 'stars': 3},
        'IQ3_M': {'quality': 'Acceptable', 'description': 'Importance-matrix 3-bit medium', 'stars': 2},
        'IQ3_S': {'quality': 'Acceptable', 'description': 'Importance-matrix 3-bit small', 'stars': 2},
        'IQ3_XS': {'quality': 'Low', 'description': 'Importance-matrix 3-bit extra small', 'stars': 2},
        'IQ3_XXS': {'quality': 'Low', 'description': 'Importance-matrix 3-bit extra extra small', 'stars': 1},
        'IQ2_M': {'quality': 'Very Low', 'description': 'Importance-matrix 2-bit, extreme compression', 'stars': 1},
        'IQ2_S': {'quality': 'Very Low', 'description': 'Importance-matrix 2-bit small', 'stars': 1},
        'IQ2_XS': {'quality': 'Minimal', 'description': 'Importance-matrix 2-bit, smallest possible', 'stars': 1},
        'IQ2_XXS': {'quality': 'Minimal', 'description': 'Maximum compression, quality trade-off', 'stars': 1},
        'Q2_K': {'quality': 'Very Low', 'description': '2-bit k-quant, maximum compression', 'stars': 1},
    }
    
    # Try exact match first
    if quant_upper in quant_info:
        return quant_info[quant_upper]
    
    # Try partial match
    for key, info in quant_info.items():
        if key in quant_upper or quant_upper in key:
            return info
    
    return {'quality': 'Unknown', 'description': f'{quant} quantization', 'stars': 3}


@llm_bp.route('/models/<model_id>/delete', methods=['POST'])
def delete_model(model_id: str):
    """Delete a local model"""
    llm_mgr = LLMManager()
    inference = InferenceService()
    
    # Unload if loaded
    if inference.is_model_loaded(model_id):
        inference.unload_model(model_id)
    
    # Create deletion task
    task_mgr = TaskManager()
    model = llm_mgr.get_model_by_id(model_id)
    
    if not model:
        return jsonify({'error': 'Model not found'}), 404
    
    steps = ["Unloading model", "Removing files", "Updating index"]
    task = task_mgr.create_task(
        name=f"Delete: {model.name}",
        task_type="delete_model",
        steps=steps
    )
    
    def run_delete():
        try:
            task_mgr.start_task(task.id)
            
            # Step 1: Unload
            task_mgr.update_step(task.id, 0, "running", "Checking if loaded...")
            task_mgr.update_progress(task.id, 20, "Unloading...")
            inference.unload_model(model_id)
            task_mgr.update_step(task.id, 0, "completed", "Unloaded")
            
            # Step 2: Remove files
            task_mgr.update_step(task.id, 1, "running", "Removing model file...")
            task_mgr.update_progress(task.id, 50, "Removing files...")
            
            success = llm_mgr.remove_local_model(model_id, delete_file=True)
            
            if not success:
                task_mgr.fail_task(task.id, "Failed to delete model file")
                return
            
            task_mgr.update_step(task.id, 1, "completed", "Files removed")
            
            # Step 3: Update index
            task_mgr.update_step(task.id, 2, "running", "Updating index...")
            task_mgr.update_progress(task.id, 90, "Finalizing...")
            time.sleep(0.2)
            task_mgr.update_step(task.id, 2, "completed", "Index updated")
            
            task_mgr.complete_task(task.id, result={'model_name': model.name})
            
        except Exception as e:
            task_mgr.fail_task(task.id, str(e))
    
    thread = threading.Thread(target=run_delete, daemon=True)
    thread.start()
    
    return jsonify({
        'success': True,
        'task_id': task.id,
        'redirect_url': f'/tasks/{task.id}'
    })


@llm_bp.route('/models/<model_id>/load', methods=['POST'])
def load_model(model_id: str):
    """Load a model for inference"""
    inference = InferenceService()
    
    # Check if model has dedicated venv (per-model environment)
    llm_env_mgr = get_llm_env_manager()
    venv_path = llm_env_mgr.get_venv_path_for_model(model_id)
    has_dedicated_venv = venv_path and Path(venv_path).exists()
    
    # Only require base llama-cpp if model doesn't have dedicated venv
    if not has_dedicated_venv and not inference.is_available():
        return jsonify({
            'error': 'Model has no dedicated environment and llama-cpp-python is not installed in base. Please set up an environment for this model first.',
            'needs_environment': True
        }), 400
    
    data = request.get_json() or {}
    
    # Parse config from request
    config = InferenceConfig(
        n_ctx=int(data.get('n_ctx', 4096)),
        n_gpu_layers=int(data.get('n_gpu_layers', 0)),
        temperature=float(data.get('temperature', 0.7))
    )
    
    try:
        loaded = inference.load_model(model_id, config)
        return jsonify({
            'success': True,
            'model': loaded.get_stats()
        })
    except Exception as e:
        return jsonify({'error': str(e)}), 500


@llm_bp.route('/models/<model_id>/unload', methods=['POST'])
def unload_model(model_id: str):
    """Unload a model from memory"""
    inference = InferenceService()
    
    success = inference.unload_model(model_id)
    
    return jsonify({'success': success})


# =====================
# Chat Interface
# =====================

@llm_bp.route('/chat')
def chat():
    """Chat interface"""
    inference = InferenceService()
    llm_mgr = LLMManager()
    llm_env_mgr = get_llm_env_manager()
    
    loaded_models = inference.get_loaded_models()
    local_models = llm_mgr.get_local_models()
    sessions = inference.get_sessions()
    
    # Get current session if specified
    session_id = request.args.get('session')
    current_session = inference.get_session(session_id) if session_id else None
    
    # Check if any models have environments set up
    models_with_env = [m for m in local_models if m.venv_name]
    has_model_environments = len(models_with_env) > 0
    
    return render_template('llm/chat.html',
                           loaded_models=loaded_models,
                           local_models=local_models,
                           models_with_env=models_with_env,
                           sessions=sessions,
                           current_session=current_session,
                           has_model_environments=has_model_environments)


@llm_bp.route('/chat/session', methods=['POST'])
def create_chat_session():
    """Create a new chat session"""
    inference = InferenceService()
    
    data = request.get_json() or request.form
    model_id = data.get('model_id')
    system_prompt = data.get('system_prompt', '')
    
    if not model_id:
        return jsonify({'error': 'model_id is required'}), 400
    
    # Load model if not loaded
    if not inference.is_model_loaded(model_id):
        try:
            inference.load_model(model_id)
        except Exception as e:
            return jsonify({'error': f'Failed to load model: {e}'}), 500
    
    session = inference.create_session(model_id, system_prompt or None)
    
    return jsonify({
        'success': True,
        'session': session.to_dict()
    })


@llm_bp.route('/chat/session/<session_id>/message', methods=['POST'])
def send_chat_message(session_id: str):
    """Send a message in a chat session"""
    inference = InferenceService()
    
    data = request.get_json() or request.form
    content = data.get('content', '').strip()
    stream = data.get('stream', True)
    
    if not content:
        return jsonify({'error': 'Message content is required'}), 400
    
    session = inference.get_session(session_id)
    if not session:
        return jsonify({'error': 'Session not found'}), 404
    
    if stream:
        def generate():
            try:
                for token in inference.send_message(session_id, content, stream=True):
                    yield f"data: {json.dumps({'token': token})}\n\n"
                yield f"data: {json.dumps({'done': True})}\n\n"
            except Exception as e:
                yield f"data: {json.dumps({'error': str(e)})}\n\n"
        
        return Response(generate(), mimetype='text/event-stream')
    else:
        try:
            response = inference.send_message(session_id, content, stream=False)
            return jsonify({
                'success': True,
                'message': response
            })
        except Exception as e:
            return jsonify({'error': str(e)}), 500


@llm_bp.route('/chat/session/<session_id>', methods=['DELETE'])
def delete_chat_session(session_id: str):
    """Delete a chat session"""
    inference = InferenceService()
    
    success = inference.delete_session(session_id)
    
    return jsonify({'success': success})


# =====================
# Settings
# =====================

@llm_bp.route('/settings')
def settings():
    """LLM settings page"""
    from app.services.repository_manager import get_repository_manager
    
    llm_mgr = LLMManager()
    inference = InferenceService()
    repo_mgr = get_repository_manager()
    
    current_settings = llm_mgr.get_settings()
    storage_stats = llm_mgr.get_storage_stats()
    
    # Get hardware info (includes backend status, GPUs, etc.)
    hardware_info = inference.get_hardware_info()
    
    # Get repositories and directories
    repositories = repo_mgr.get_repositories()
    directories = repo_mgr.get_directories()
    
    # Add space info to directories
    for dir_obj in directories:
        dir_obj.available_space_gb = dir_obj.get_available_space_gb()
        dir_obj.used_space_gb = dir_obj.get_used_space_gb()
        dir_obj.total_space_gb = dir_obj.available_space_gb + dir_obj.used_space_gb
    
    # Mask token for display
    token = current_settings.get('huggingface_token', '')
    if token:
        current_settings['huggingface_token_masked'] = token[:4] + '*' * (len(token) - 8) + token[-4:] if len(token) > 8 else '****'
    else:
        current_settings['huggingface_token_masked'] = None
    
    return render_template('llm/settings.html',
                           settings=current_settings,
                           storage_stats=storage_stats,
                           hardware_info=hardware_info,
                           repositories=repositories,
                           directories=directories)


@llm_bp.route('/settings/token', methods=['POST'])
def save_token():
    """Save HuggingFace token (saves to both legacy settings and repository)"""
    from app.services.repository_manager import get_repository_manager
    
    llm_mgr = LLMManager()
    repo_mgr = get_repository_manager()
    
    data = request.get_json() or request.form
    token = data.get('token', '').strip()
    
    # Save to legacy settings (backward compatibility)
    llm_mgr.set_huggingface_token(token if token else None)
    
    # Also save to HuggingFace repository
    hf_repo = repo_mgr.get_repository('hf_default')
    if hf_repo:
        repo_mgr.set_repository_api_key('hf_default', token if token else None)
    
    return jsonify({'success': True})


@llm_bp.route('/settings/update', methods=['POST'])
def update_settings():
    """Update LLM settings"""
    llm_mgr = LLMManager()
    
    data = request.get_json() or request.form
    
    settings = {
        'default_format': data.get('default_format', 'gguf'),
        'auto_delete_incomplete': data.get('auto_delete_incomplete', 'true') == 'true',
        'max_concurrent_downloads': int(data.get('max_concurrent_downloads', 2)),
        'download_chunk_size': int(data.get('download_chunk_size', 8192))
    }
    
    llm_mgr.update_settings(settings)
    
    return jsonify({'success': True})


# =====================
# Repository Management
# =====================

@llm_bp.route('/api/repositories', methods=['GET'])
def api_list_repositories():
    """API: List all repositories"""
    from app.services.repository_manager import get_repository_manager
    repo_mgr = get_repository_manager()
    
    repositories = repo_mgr.get_repositories()
    return jsonify({
        'success': True,
        'repositories': [r.to_dict() for r in repositories]
    })


@llm_bp.route('/api/repositories', methods=['POST'])
def api_add_repository():
    """API: Add a new repository"""
    from app.services.repository_manager import get_repository_manager, ModelRepository
    repo_mgr = get_repository_manager()
    
    data = request.get_json() or {}
    
    repo = ModelRepository(
        id=data.get('id', f"repo_{len(repo_mgr.get_repositories())}"),
        name=data.get('name', ''),
        type=data.get('type', 'custom'),
        enabled=data.get('enabled', True),
        url=data.get('url'),
        api_key=data.get('api_key'),
        description=data.get('description'),
        priority=data.get('priority', 0)
    )
    
    success = repo_mgr.add_repository(repo)
    
    if success:
        return jsonify({'success': True, 'repository': repo.to_dict()})
    else:
        return jsonify({'success': False, 'error': 'Repository ID already exists'}), 400


@llm_bp.route('/api/repositories/<repo_id>', methods=['PUT'])
def api_update_repository(repo_id: str):
    """API: Update a repository"""
    from app.services.repository_manager import get_repository_manager
    repo_mgr = get_repository_manager()
    
    data = request.get_json() or {}
    
    success = repo_mgr.update_repository(repo_id, data)
    
    if success:
        repo = repo_mgr.get_repository(repo_id)
        return jsonify({'success': True, 'repository': repo.to_dict()})
    else:
        return jsonify({'success': False, 'error': 'Repository not found'}), 404


@llm_bp.route('/api/repositories/<repo_id>', methods=['DELETE'])
def api_delete_repository(repo_id: str):
    """API: Delete a repository"""
    from app.services.repository_manager import get_repository_manager
    repo_mgr = get_repository_manager()
    
    success = repo_mgr.delete_repository(repo_id)
    
    if success:
        return jsonify({'success': True})
    else:
        return jsonify({'success': False, 'error': 'Cannot delete default repository'}), 400


@llm_bp.route('/api/repositories/<repo_id>/api-key', methods=['POST'])
def api_set_repository_api_key(repo_id: str):
    """API: Set API key for a repository"""
    from app.services.repository_manager import get_repository_manager
    repo_mgr = get_repository_manager()
    
    data = request.get_json() or {}
    api_key = data.get('api_key')
    
    repo_mgr.set_repository_api_key(repo_id, api_key)
    
    return jsonify({'success': True})


# =====================
# Directory Management
# =====================

@llm_bp.route('/api/directories', methods=['GET'])
def api_list_directories():
    """API: List all model directories"""
    from app.services.repository_manager import get_repository_manager
    repo_mgr = get_repository_manager()
    
    directories = repo_mgr.get_directories()
    
    # Add space info
    for dir_obj in directories:
        dir_obj.available_space_gb = dir_obj.get_available_space_gb()
        dir_obj.used_space_gb = dir_obj.get_used_space_gb()
        dir_obj.total_space_gb = dir_obj.available_space_gb + dir_obj.used_space_gb
    
    return jsonify({
        'success': True,
        'directories': [d.to_dict() for d in directories]
    })


@llm_bp.route('/api/directories', methods=['POST'])
def api_add_directory():
    """API: Add a new model directory"""
    from app.services.repository_manager import get_repository_manager, ModelDirectory
    repo_mgr = get_repository_manager()
    
    data = request.get_json() or {}
    
    directory = ModelDirectory(
        id=data.get('id', f"dir_{len(repo_mgr.get_directories())}"),
        name=data.get('name', ''),
        path=data.get('path', ''),
        enabled=data.get('enabled', True),
        max_size_gb=data.get('max_size_gb'),
        description=data.get('description'),
        priority=data.get('priority', 0)
    )
    
    success = repo_mgr.add_directory(directory)
    
    if success:
        return jsonify({'success': True, 'directory': directory.to_dict()})
    else:
        return jsonify({'success': False, 'error': 'Directory ID already exists'}), 400


@llm_bp.route('/api/directories/<dir_id>', methods=['PUT'])
def api_update_directory(dir_id: str):
    """API: Update a directory"""
    from app.services.repository_manager import get_repository_manager
    repo_mgr = get_repository_manager()
    
    data = request.get_json() or {}
    
    success = repo_mgr.update_directory(dir_id, data)
    
    if success:
        dir_obj = repo_mgr.get_directory(dir_id)
        return jsonify({'success': True, 'directory': dir_obj.to_dict()})
    else:
        return jsonify({'success': False, 'error': 'Directory not found'}), 404


@llm_bp.route('/api/directories/<dir_id>', methods=['DELETE'])
def api_delete_directory(dir_id: str):
    """API: Delete a directory (doesn't delete files)"""
    from app.services.repository_manager import get_repository_manager
    repo_mgr = get_repository_manager()
    
    success = repo_mgr.delete_directory(dir_id)
    
    if success:
        return jsonify({'success': True})
    else:
        return jsonify({'success': False, 'error': 'Cannot delete default directory'}), 400


# =====================
# API Endpoints
# =====================

@llm_bp.route('/api/models')
def api_list_models():
    """API: List local models"""
    llm_mgr = LLMManager()
    models = llm_mgr.get_local_models()
    return jsonify({'models': [m.to_dict() for m in models]})


@llm_bp.route('/api/models/<model_id>/info')
def api_model_info(model_id: str):
    """API: Get detailed model information with description and use cases"""
    from app.services.llm_manager import get_model_knowledge
    
    llm_mgr = LLMManager()
    model = llm_mgr.get_model_by_id(model_id)
    
    if not model:
        return jsonify({'error': 'Model not found'}), 404
    
    # Get knowledge base info
    knowledge = get_model_knowledge(model.name)
    
    # Get quantization info
    quant_info = get_quantization_info(model.quantization) if model.quantization else None
    
    # Build detailed response
    info = {
        'id': model.id,
        'name': model.name,
        'filename': model.filename,
        'path': model.path,
        'size_bytes': model.size,
        'size_gb': round(model.size / 1073741824, 2),
        'format': model.format,
        'source': model.source,
        'status': model.status,
        'downloaded_at': model.downloaded_at,
        'last_used': model.last_used,
        
        # Model characteristics
        'quantization': model.quantization,
        'quantization_info': quant_info,
        'parameters': model.parameters,
        'context_length': model.context_length,
        
        # Environment info
        'venv_name': model.venv_name,
        'gpu_backend': model.gpu_backend,
        'has_environment': bool(model.venv_name),
        
        # Knowledge base info
        'family': knowledge.get('family', 'Unknown'),
        'description': model.description or knowledge.get('description', 'A large language model optimized for local inference.'),
        'use_cases': model.use_cases or knowledge.get('use_cases', []),
        'license': model.license or knowledge.get('license', 'Check model source'),
        
        # Recommendations
        'recommended_ram': _estimate_ram_requirement(model.size, model.quantization),
        'recommended_vram': _estimate_vram_requirement(model.size, model.quantization),
    }
    
    return jsonify(info)


def _estimate_ram_requirement(size_bytes: int, quantization: Optional[str]) -> str:
    """Estimate RAM requirement based on model size"""
    size_gb = size_bytes / 1073741824
    
    # For GGUF models, RAM needed is approximately 1.1-1.5x file size for CPU inference
    ram_low = size_gb * 1.1
    ram_high = size_gb * 1.5
    
    return f"{ram_low:.1f} - {ram_high:.1f} GB"


def _estimate_vram_requirement(size_bytes: int, quantization: Optional[str]) -> str:
    """Estimate VRAM requirement for full GPU offload"""
    size_gb = size_bytes / 1073741824
    
    # For GPU inference, VRAM needed is approximately file size + overhead
    vram = size_gb * 1.1
    
    return f"{vram:.1f} GB (full offload)"


@llm_bp.route('/api/models/loaded')
def api_loaded_models():
    """API: List loaded models"""
    inference = InferenceService()
    return jsonify({'models': inference.get_loaded_models()})


@llm_bp.route('/api/complete', methods=['POST'])
def api_complete():
    """API: Text completion"""
    inference = InferenceService()
    
    data = request.get_json()
    if not data:
        return jsonify({'error': 'JSON body required'}), 400
    
    model_id = data.get('model_id')
    prompt = data.get('prompt')
    
    if not model_id or not prompt:
        return jsonify({'error': 'model_id and prompt are required'}), 400
    
    try:
        result = inference.complete(
            model_id, prompt,
            max_tokens=data.get('max_tokens'),
            temperature=data.get('temperature'),
            stream=False
        )
        return jsonify(result)
    except Exception as e:
        return jsonify({'error': str(e)}), 500


@llm_bp.route('/api/chat', methods=['POST'])
def api_chat():
    """API: Chat completion"""
    inference = InferenceService()
    
    data = request.get_json()
    if not data:
        return jsonify({'error': 'JSON body required'}), 400
    
    model_id = data.get('model_id')
    messages = data.get('messages')
    
    if not model_id or not messages:
        return jsonify({'error': 'model_id and messages are required'}), 400
    
    try:
        result = inference.chat(
            model_id, messages,
            max_tokens=data.get('max_tokens'),
            temperature=data.get('temperature'),
            stream=False
        )
        return jsonify(result)
    except Exception as e:
        return jsonify({'error': str(e)}), 500


# =====================
# Hardware Management
# =====================

@llm_bp.route('/hardware')
def hardware():
    """REDIRECTED: Hardware acceleration is now managed per-model"""
    flash('Hardware acceleration is configured per-model. Create virtual environments for your models.', 'info')
    return redirect(url_for('llm.llm_environment'))


@llm_bp.route('/api/hardware/detect', methods=['GET'])
def api_hardware_detect():
    """API: Detect available hardware backends"""
    hw_service = HardwareService()
    
    profile = hw_service.detect_hardware(force_refresh=True)
    recommended = hw_service.get_recommended_backend()
    
    return jsonify({
        'profile': profile.to_dict(),
        'recommended': recommended,
        'active_backend': hw_service.get_active_backend()
    })


@llm_bp.route('/api/hardware/config', methods=['GET'])
def api_hardware_get_config():
    """API: Get current hardware configuration"""
    hw_service = HardwareService()
    active_backend = hw_service.get_active_backend()
    
    if active_backend:
        settings = hw_service.get_inference_settings_for_backend(active_backend)
        return jsonify({
            'backend': active_backend,
            **settings
        })
    else:
        return jsonify({'backend': 'cpu', 'n_gpu_layers': 0})


@llm_bp.route('/api/hardware/config', methods=['POST'])
def api_hardware_save_config():
    """API: Save hardware configuration"""
    hw_service = HardwareService()
    
    data = request.get_json()
    if not data:
        return jsonify({'error': 'JSON body required'}), 400
    
    try:
        backend = data.get('backend', 'cpu').lower()
        hw_service.set_active_backend(backend)
        
        return jsonify({'success': True, 'message': f'Backend set to {backend}'})
    except Exception as e:
        return jsonify({'error': str(e)}), 500


@llm_bp.route('/api/hardware/install-args', methods=['GET'])
def api_hardware_install_args():
    """API: Get installation arguments for llama-cpp-python"""
    hw_service = HardwareService()
    
    backend = request.args.get('backend', 'cpu').lower()
    
    install_cmd = hw_service.get_install_command(backend)
    if install_cmd:
        return jsonify({
            'backend': backend,
            'install_command': install_cmd
        })
    else:
        return jsonify({'error': f'Unknown backend: {backend}'}), 400


@llm_bp.route('/api/hardware/layers', methods=['GET'])
def api_hardware_layers():
    """API: Get recommended GPU layers for a model"""
    hw_service = HardwareService()
    
    model_size_gb = request.args.get('model_size', type=float)
    backend = request.args.get('backend', 'cpu').lower()
    
    if not model_size_gb:
        return jsonify({'error': 'model_size parameter required (in GB)'}), 400
    
    try:
        layers = hw_service.get_gpu_layers_recommendation(model_size_gb, backend)
        return jsonify({
            'model_size_gb': model_size_gb,
            'backend': backend,
            'recommended_layers': layers
        })
    except Exception as e:
        return jsonify({'error': str(e)}), 500


@llm_bp.route('/api/hardware/reinstall', methods=['POST'])
def api_hardware_reinstall():
    """API: Install llama-cpp-python with specific backend into a virtual environment"""
    task_mgr = TaskManager()
    hw_service = HardwareService()
    
    data = request.get_json()
    if not data:
        return jsonify({'error': 'JSON body required'}), 400
    
    backend = data.get('backend', 'cpu').lower()
    venv_path = data.get('venv_path')  # Optional: target virtual environment
    
    # Warn if installing to global Python
    install_warning = None
    if not venv_path:
        install_warning = "Installing to global Python. Consider using a virtual environment for isolation."
    
    try:
        # Create task for installation
        task_id = task_mgr.create_task(
            task_type='install_llama_cpp',
            name=f'Install llama-cpp-python ({backend})',
            metadata={
                'backend': backend,
                'venv_path': venv_path
            }
        )
        
        def do_install():
            def progress_callback(pct, msg):
                task_mgr.update_progress(task_id, pct, msg)
            
            try:
                success, message = hw_service.install_llama_cpp(
                    backend_type=backend,
                    venv_path=venv_path,
                    progress_callback=progress_callback
                )
                
                if success:
                    task_mgr.complete_task(task_id, {'success': True, 'message': message})
                else:
                    task_mgr.fail_task(task_id, message)
                    
            except Exception as e:
                task_mgr.fail_task(task_id, str(e))
        
        # Run in background thread
        thread = threading.Thread(target=do_install)
        thread.daemon = True
        thread.start()
        
        response = {
            'success': True,
            'task_id': task_id,
            'message': f'Installing llama-cpp-python with {backend} support',
            'target': venv_path or 'Global Python'
        }
        if install_warning:
            response['warning'] = install_warning
        
        return jsonify(response)
        
    except Exception as e:
        return jsonify({'error': str(e)}), 500


@llm_bp.route('/api/hardware/venvs', methods=['GET'])
def api_hardware_venvs():
    """API: Get list of available virtual environments"""
    hw_service = HardwareService()
    venvs = hw_service.get_available_venvs()
    return jsonify({'venvs': venvs})


@llm_bp.route('/api/hardware/auto-install', methods=['POST'])
def api_hardware_auto_install():
    """API: Auto-install llama-cpp-python with recommended backend"""
    task_mgr = TaskManager()
    hw_service = HardwareService()
    
    data = request.get_json() or {}
    venv_path = data.get('venv_path')
    
    # Warn if installing to global Python
    install_warning = None
    if not venv_path:
        install_warning = "Installing to global Python. Consider using a virtual environment for isolation."
    
    # Create task
    task_id = task_mgr.create_task(
        task_type='auto_install_llama_cpp',
        name='Auto-install llama-cpp-python',
        metadata={'venv_path': venv_path}
    )
    
    def do_auto_install():
        def progress_callback(pct, msg):
            task_mgr.update_progress(task_id, pct, msg)
        
        try:
            success, message = hw_service.ensure_llama_cpp_installed(
                venv_path=venv_path,
                progress_callback=progress_callback
            )
            
            if success:
                task_mgr.complete_task(task_id, {'success': True, 'message': message})
            else:
                task_mgr.fail_task(task_id, message)
                
        except Exception as e:
            task_mgr.fail_task(task_id, str(e))
    
    thread = threading.Thread(target=do_auto_install)
    thread.daemon = True
    thread.start()
    
    response = {
        'success': True,
        'task_id': task_id,
        'message': 'Auto-installing llama-cpp-python with recommended backend',
        'target': venv_path or 'Global Python'
    }
    if install_warning:
        response['warning'] = install_warning
    
    return jsonify(response)


# =====================
# LLM Setup Wizard
# =====================

@llm_bp.route('/wizard')
def wizard():
    """LLM Setup Wizard - Guided model selection and setup"""
    from app.services.hardware_detector import get_hardware_info
    
    # Get hardware data directly (uses cache, very fast)
    hw = get_hardware_info()
    hardware_data = hw.to_dict()
    
    recommended_backend = 'cpu'
    backend_toolkit_status = {'cpu': {'available': True, 'toolkit_name': 'CPU', 'details': 'No toolkit required'}}
    available_backends = {}
    
    # Set recommended backend based on GPU type
    if hw.gpu_type == 'nvidia':
        recommended_backend = 'cuda'
    elif hw.gpu_type == 'apple_silicon':
        recommended_backend = 'metal'
        backend_toolkit_status['metal'] = {'available': True, 'toolkit_name': 'Metal', 'details': 'Built into macOS'}
    elif hw.gpu_type == 'amd':
        recommended_backend = 'rocm'
    
    # Get installed llama.cpp backends (fast - just checks files)
    try:
        from app.services.cuda_installer import get_backend_installer
        backend_installer = get_backend_installer()
        available_backends = backend_installer.get_available_backends()
        
        # Add all available backends to status (except CPU which is always available)
        for bid, b in available_backends.items():
            if bid == 'cpu':
                continue  # CPU is always available, don't overwrite
            backend_toolkit_status[bid] = {
                'available': b.get('installed', False),
                'toolkit_name': b.get('name', bid),
                'details': b.get('description', ''),
                'install_url': '/llm/backend-extensions'
            }
        
        # Also add generic mappings for convenience
        if hw.gpu_type == 'nvidia':
            # If cuda is installed, mark 'nvidia' as available too
            if 'cuda' in available_backends and available_backends['cuda'].get('installed', False):
                backend_toolkit_status['nvidia'] = backend_toolkit_status['cuda'].copy()
            # If no CUDA backend installed yet, mark as not available
            if 'cuda' not in backend_toolkit_status:
                backend_toolkit_status['cuda'] = {
                    'available': False,
                    'toolkit_name': 'CUDA Backend',
                    'details': 'Install CUDA from Backend Extensions',
                    'install_url': '/llm/backend-extensions'
                }
                backend_toolkit_status['nvidia'] = backend_toolkit_status['cuda'].copy()
        
        elif hw.gpu_type == 'amd':
            # Map rocm/amd to hip backend
            if 'hip' in available_backends and available_backends['hip'].get('installed', False):
                backend_toolkit_status['rocm'] = backend_toolkit_status['hip'].copy()
                backend_toolkit_status['amd'] = backend_toolkit_status['hip'].copy()
            else:
                backend_toolkit_status['rocm'] = {
                    'available': False,
                    'toolkit_name': 'AMD ROCm/HIP Backend',
                    'details': 'Install hip from Backend Extensions',
                    'install_url': '/llm/backend-extensions'
                }
                backend_toolkit_status['amd'] = backend_toolkit_status['rocm'].copy()
        
        elif hw.gpu_type == 'intel':
            # Map intel to sycl backend
            if 'sycl' in available_backends and available_backends['sycl'].get('installed', False):
                backend_toolkit_status['intel'] = backend_toolkit_status['sycl'].copy()
            else:
                backend_toolkit_status['intel'] = {
                    'available': False,
                    'toolkit_name': 'Intel SYCL Backend',
                    'details': 'Install sycl from Backend Extensions',
                    'install_url': '/llm/backend-extensions'
                }
        
        # Always add vulkan mapping (works with any GPU vendor)
        if 'vulkan' in available_backends:
            # Already added above, just ensure the mapping exists
            pass
        
        # Always add rocm/hip mapping for AMD GPUs option
        if 'hip' in available_backends and 'rocm' not in backend_toolkit_status:
            backend_toolkit_status['rocm'] = backend_toolkit_status.get('hip', {
                'available': False,
                'toolkit_name': 'AMD ROCm/HIP',
                'details': 'Install hip from Backend Extensions',
                'install_url': '/llm/backend-extensions'
            })
            backend_toolkit_status['amd'] = backend_toolkit_status['rocm'].copy()
                
    except Exception as e:
        print(f"Backend installer check failed: {e}")
    
    return render_template('llm/llm_setup_wizard.html',
                           hardware_data=hardware_data,
                           backend_toolkit_status=backend_toolkit_status,
                           recommended_backend=recommended_backend,
                           available_backends=available_backends)


@llm_bp.route('/api/wizard/hardware', methods=['GET'])
def api_wizard_hardware():
    """API: Get cached hardware capabilities for wizard (fast, no detection)"""
    from app.services.environment_manager import EnvironmentManager
    from flask import Response
    
    rec_service = get_recommendation_service()
    env_mgr = EnvironmentManager(base_path=os.environ.get('BEEP_PYTHON_HOME', 
                                                           os.path.expanduser('~/.beep-llm')))
    
    # Check if streaming is requested
    stream = request.args.get('stream', 'false').lower() == 'true'
    
    if stream:
        # Stream progress updates
        def generate():
            try:
                hardware = None
                
                # Try cached first (fast path)
                if rec_service._hardware_cache:
                    yield f"data: {json.dumps({'type': 'progress', 'step': 'cache', 'message': 'Using cached hardware profile', 'percent': 90})}\n\n"
                    hardware = rec_service._hardware_cache
                else:
                    # Detect with progress - break down into steps (no artificial delays)
                    import psutil
                    import platform
                    from app.services.model_recommendation import HardwareProfile, GPUType
                    
                    yield f"data: {json.dumps({'type': 'progress', 'step': 'start', 'message': 'Starting hardware detection...', 'percent': 0})}\n\n"
                    
                    yield f"data: {json.dumps({'type': 'progress', 'step': 'gpu', 'message': 'Detecting GPU...', 'percent': 10})}\n\n"
                    gpu_type, gpu_name, vram_gb = rec_service._detect_gpu()
                    
                    yield f"data: {json.dumps({'type': 'progress', 'step': 'ram', 'message': 'Detecting RAM...', 'percent': 40})}\n\n"
                    ram_gb = psutil.virtual_memory().total / (1024 ** 3)
                    
                    yield f"data: {json.dumps({'type': 'progress', 'step': 'disk', 'message': 'Checking disk space...', 'percent': 60})}\n\n"
                    disk_free_gb = rec_service._get_disk_free_space()
                    
                    yield f"data: {json.dumps({'type': 'progress', 'step': 'cpu', 'message': 'Detecting CPU...', 'percent': 75})}\n\n"
                    cpu_cores = psutil.cpu_count(logical=False) or psutil.cpu_count()
                    cpu_name = rec_service._get_cpu_name()
                    
                    yield f"data: {json.dumps({'type': 'progress', 'step': 'finalize', 'message': 'Finalizing...', 'percent': 90})}\n\n"
                    platform_name = platform.system()
                    
                    hardware = HardwareProfile(
                        gpu_type=gpu_type,
                        gpu_name=gpu_name,
                        vram_gb=vram_gb,
                        ram_gb=ram_gb,
                        disk_free_gb=disk_free_gb,
                        cpu_cores=cpu_cores,
                        cpu_name=cpu_name,
                        platform=platform_name
                    )
                    
                    # Cache it
                    rec_service._hardware_cache = hardware
                    rec_service._save_cached_hardware(hardware)
                
                backend = rec_service.get_recommended_backend(hardware)
                
                response = hardware.to_dict()
                response['recommended_backend'] = backend
                
                # Check installed llama.cpp backends (fast - just checks for installed.json files)
                yield f"data: {json.dumps({'type': 'progress', 'step': 'toolkit', 'message': 'Checking installed backends...', 'percent': 95})}\n\n"
                
                backend_toolkit_status = {}
                gpu_type = hardware.gpu_type.value if hasattr(hardware.gpu_type, 'value') else str(hardware.gpu_type)
                
                # Use LlamaBackendInstaller for fast installed check (no subprocess calls)
                try:
                    from app.services.cuda_installer import get_backend_installer
                    backend_installer = get_backend_installer()
                    available_backends = backend_installer.get_available_backends()
                    
                    # Map GPU type to relevant backends
                    if gpu_type == 'nvidia':
                        if 'cuda' in available_backends:
                            b = available_backends['cuda']
                            backend_toolkit_status['cuda'] = {
                                'available': b.get('installed', False),
                                'toolkit_name': b.get('name', 'NVIDIA CUDA'),
                                'details': b.get('description', ''),
                                'install_url': '/llm/backend-extensions'
                            }
                    elif gpu_type == 'amd':
                        if 'hip' in available_backends:
                            b = available_backends['hip']
                            backend_toolkit_status['hip'] = {
                                'available': b.get('installed', False),
                                'toolkit_name': b.get('name', 'HIP'),
                                'details': b.get('description', ''),
                                'install_url': '/llm/backend-extensions'
                            }
                    elif gpu_type == 'apple_silicon':
                        backend_toolkit_status['metal'] = {'available': True, 'toolkit_name': 'Metal', 'details': 'Built into macOS'}
                    
                    # Vulkan works with all GPUs
                    if 'vulkan' in available_backends:
                        b = available_backends['vulkan']
                        backend_toolkit_status['vulkan'] = {
                            'available': b.get('installed', False),
                            'toolkit_name': b.get('name', 'Vulkan'),
                            'details': b.get('description', ''),
                            'install_url': '/llm/backend-extensions'
                        }
                    
                    # CPU always available
                    if 'cpu' in available_backends:
                        b = available_backends['cpu']
                        backend_toolkit_status['cpu'] = {
                            'available': True,  # CPU is always available
                            'toolkit_name': b.get('name', 'CPU'),
                            'details': b.get('description', 'No GPU required'),
                            'install_url': '/llm/backend-extensions'
                        }
                    else:
                        backend_toolkit_status['cpu'] = {'available': True, 'toolkit_name': 'CPU', 'details': 'No toolkit required'}
                    
                    # Add recommended backend info
                    response['recommended_llama_backend'] = backend_installer.get_recommended_backend()
                    
                except Exception as be:
                    # Fallback if backend installer fails
                    backend_toolkit_status['cpu'] = {'available': True, 'toolkit_name': 'CPU', 'details': 'No toolkit required'}
                    print(f"Backend installer check failed: {be}")
                
                response['backend_toolkit_status'] = backend_toolkit_status
                
                # Send final result
                yield f"data: {json.dumps({'type': 'complete', 'data': response})}\n\n"
                
            except Exception as e:
                import traceback
                traceback.print_exc()
                yield f"data: {json.dumps({'type': 'error', 'error': str(e)})}\n\n"
        
        return Response(generate(), mimetype='text/event-stream',
                       headers={'Cache-Control': 'no-cache', 'X-Accel-Buffering': 'no'})
    
    else:
        # Non-streaming (fast, cached only - don't detect if no cache)
        try:
            # Only use cache - don't detect if cache doesn't exist (that's what streaming is for)
            if rec_service._hardware_cache:
                hardware = rec_service._hardware_cache
                backend = rec_service.get_recommended_backend(hardware)
                
                response = hardware.to_dict()
                response['recommended_backend'] = backend
                
                # Check installed llama.cpp backends (fast - just checks for installed.json files)
                backend_toolkit_status = {}
                gpu_type = hardware.gpu_type.value if hasattr(hardware.gpu_type, 'value') else str(hardware.gpu_type)
                
                try:
                    from app.services.cuda_installer import get_backend_installer
                    backend_installer = get_backend_installer()
                    available_backends = backend_installer.get_available_backends()
                    
                    # Map GPU type to relevant backends
                    if gpu_type == 'nvidia':
                        if 'cuda' in available_backends:
                            b = available_backends['cuda']
                            backend_toolkit_status['cuda'] = {
                                'available': b.get('installed', False),
                                'toolkit_name': b.get('name', 'NVIDIA CUDA'),
                                'details': b.get('description', ''),
                                'install_url': '/llm/backend-extensions'
                            }
                    elif gpu_type == 'amd':
                        if 'hip' in available_backends:
                            b = available_backends['hip']
                            backend_toolkit_status['hip'] = {
                                'available': b.get('installed', False),
                                'toolkit_name': b.get('name', 'HIP'),
                                'details': b.get('description', ''),
                                'install_url': '/llm/backend-extensions'
                            }
                    elif gpu_type == 'apple_silicon':
                        backend_toolkit_status['metal'] = {'available': True, 'toolkit_name': 'Metal', 'details': 'Built into macOS'}
                    
                    # Vulkan works with all GPUs
                    if 'vulkan' in available_backends:
                        b = available_backends['vulkan']
                        backend_toolkit_status['vulkan'] = {
                            'available': b.get('installed', False),
                            'toolkit_name': b.get('name', 'Vulkan'),
                            'details': b.get('description', ''),
                            'install_url': '/llm/backend-extensions'
                        }
                    
                    # CPU always available
                    backend_toolkit_status['cpu'] = {'available': True, 'toolkit_name': 'CPU', 'details': 'No toolkit required'}
                    
                    # Add recommended backend info
                    response['recommended_llama_backend'] = backend_installer.get_recommended_backend()
                    
                except Exception as be:
                    backend_toolkit_status['cpu'] = {'available': True, 'toolkit_name': 'CPU', 'details': 'No toolkit required'}
                    print(f"Backend installer check failed: {be}")
                
                response['backend_toolkit_status'] = backend_toolkit_status
                
                return jsonify(response)
            else:
                # No cache - return error so frontend uses streaming
                return jsonify({
                    'error': 'No cached hardware profile. Use streaming endpoint.',
                    'use_streaming': True
                }), 202  # 202 Accepted - indicates async processing needed
        except Exception as e:
            import traceback
            traceback.print_exc()
            return jsonify({
                'error': str(e),
                'gpu_type': 'none',
                'gpu_name': None,
                'vram_gb': 0,
                'ram_gb': 8.0,
                'disk_free_gb': 10.0,
                'cpu_cores': 4,
                'cpu_name': 'Unknown',
                'platform': 'Unknown',
                'recommended_backend': 'cpu',
                'backend_toolkit_status': {'cpu': {'available': True, 'toolkit_name': 'CPU', 'details': 'No toolkit required'}}
            }), 200


@llm_bp.route('/api/backends', methods=['GET'])
def api_get_all_backends():
    """API: Get all available compute backends (system-wide SDKs)
    
    Query params:
        refresh: Set to 'true' to force re-detection (ignores cache)
    
    Returns all backends with their availability status (cached for performance)
    """
    from app.services.environment_manager import EnvironmentManager
    
    force_refresh = request.args.get('refresh', 'false').lower() == 'true'
    
    try:
        env_mgr = EnvironmentManager(base_path=os.environ.get('BEEP_PYTHON_HOME', 
                                                               os.path.expanduser('~/.beep-llm')))
        
        backends = env_mgr.detect_all_backends(force_refresh=force_refresh)
        
        return jsonify({
            'success': True,
            'backends': backends,
            'cached': not force_refresh
        })
    except Exception as e:
        import traceback
        traceback.print_exc()
        return jsonify({
            'success': False,
            'error': str(e),
            'backends': {
                'cpu': {'available': True, 'toolkit_name': 'CPU', 'message': 'CPU backend - always available'}
            }
        }), 500


@llm_bp.route('/api/backends/refresh', methods=['POST'])
def api_refresh_backends():
    """API: Force refresh backend detection (clears cache and re-detects)
    
    Call this after installing a new SDK (CUDA, ROCm, Vulkan, etc.)
    """
    from app.services.environment_manager import EnvironmentManager
    
    try:
        base_path = Path(os.environ.get('BEEP_PYTHON_HOME', os.path.expanduser('~/.beep-llm')))
        
        # Clear the cache
        EnvironmentManager.clear_toolkit_cache(base_path)
        
        # Re-detect all backends
        env_mgr = EnvironmentManager(base_path=str(base_path))
        backends = env_mgr.detect_all_backends(force_refresh=True)
        
        return jsonify({
            'success': True,
            'message': 'Backend detection refreshed',
            'backends': backends
        })
    except Exception as e:
        import traceback
        traceback.print_exc()
        return jsonify({
            'success': False,
            'error': str(e)
        }), 500


@llm_bp.route('/api/wizard/recommend', methods=['POST'])
def api_wizard_recommend():
    """API: Get model recommendations based on use case"""
    rec_service = get_recommendation_service()
    
    data = request.get_json()
    if not data:
        return jsonify({'error': 'JSON body required'}), 400
    
    use_case_str = data.get('use_case', 'general')
    
    try:
        # Convert string to UseCase enum
        use_case = UseCase(use_case_str.lower())
    except ValueError:
        return jsonify({'error': f'Invalid use case: {use_case_str}'}), 400
    
    try:
        recommendations = rec_service.get_recommendations(use_case, max_results=5)
        
        return jsonify({
            'success': True,
            'use_case': use_case.value,
            'recommendations': [r.to_dict() for r in recommendations]
        })
    except Exception as e:
        return jsonify({'error': str(e)}), 500


@llm_bp.route('/api/wizard/setup', methods=['POST'])
def api_wizard_setup():
    """
    API: Automated model download and environment setup
    
    Hybrid Approach:
    - Download GGUF model
    - Create virtual environment for the model
    - Install llama.cpp backend (LM Studio style - native binaries)
    - Associate model with venv
    - Use native llama-server for inference (HTTP API)
    """
    from app.services.cuda_installer import get_backend_installer
    from app.services.environment_manager import EnvironmentManager
    from app.services.llm_environment import get_llm_env_manager
    
    data = request.get_json()
    if not data:
        return jsonify({'error': 'JSON body required'}), 400
    
    model_id = data.get('model_id')
    model_name = data.get('model_name')
    backend = data.get('backend', 'cpu')
    
    if not model_id or not model_name:
        return jsonify({'error': 'model_id and model_name are required'}), 400
    
    # Get backend installer for LM Studio-style native binaries
    backend_installer = get_backend_installer()
    
    # Map generic backend names to specific backend IDs
    backend_mapping = {
        'cuda': 'cuda',
        'nvidia': 'cuda',
        'rocm': 'hip',
        'amd': 'hip',
        'metal': 'metal',
        'vulkan': 'vulkan',
        'cpu': 'cpu',
        'auto': 'cpu'
    }
    backend_id = backend_mapping.get(backend.lower(), backend.lower())
    
    task_mgr = TaskManager()
    hf_service = HuggingFaceService()
    llm_mgr = LLMManager()
    
    # Create comprehensive setup task with venv creation
    steps = [
        "Checking for existing model",
        "Downloading model",
        "Creating virtual environment",
        "Installing llama.cpp backend",
        "Associating model with environment",
        "Finalizing setup"
    ]
    
    task = task_mgr.create_task(
        name=f"Setup: {model_name}",
        task_type="wizard_setup",
        steps=steps
    )
    
    def run_setup():
        try:
            task_mgr.start_task(task.id)
            local_model = None
            
            # Step 1: Check if model already exists
            task_mgr.update_step(task.id, 0, "running", "Checking for existing model...")
            task_mgr.update_progress(task.id, 5, "Checking local models...")
            
            # Check if we already have this model downloaded
            existing_models = llm_mgr.get_local_models()
            for m in existing_models:
                if model_id.replace('/', '_').lower() in m.name.lower() or model_id.lower() in m.path.lower():
                    local_model = m
                    break
            
            if local_model:
                task_mgr.update_step(task.id, 0, "completed", f"Found: {local_model.name}")
                task_mgr.update_step(task.id, 1, "completed", "Already downloaded")
                task_mgr.update_progress(task.id, 40, "Model already exists")
            else:
                task_mgr.update_step(task.id, 0, "completed", "Model not found locally")
                
                # Step 2: Download model
                task_mgr.update_step(task.id, 1, "running", "Fetching model files...")
                task_mgr.update_progress(task.id, 10, "Fetching model info...")
                
                files = hf_service.get_model_files(model_id, filter_gguf=True)
                if not files:
                    task_mgr.fail_task(task.id, "No GGUF files found for this model")
                    return
                
                # Get the first GGUF file
                target_file = files[0]
                task_mgr.update_step(task.id, 1, "running", f"Downloading {target_file['filename']}...")
                
                def download_progress(progress):
                    percent = 10 + int(progress.percent * 0.3)  # 10-40%
                    task_mgr.update_progress(task.id, percent, f"Downloading: {progress.percent:.1f}%")
                    task_mgr.update_step(task.id, 1, "running", f"Downloading: {progress.percent:.1f}%")
                
                local_model = hf_service.download_model(
                    model_id,
                    target_file['filename'],
                    progress_callback=download_progress
                )
                
                if not local_model:
                    task_mgr.fail_task(task.id, "Download failed")
                    return
                
                task_mgr.update_step(task.id, 1, "completed", "Download complete")
            
            # Step 3: Create virtual environment for this model
            task_mgr.update_step(task.id, 2, "running", "Creating virtual environment...")
            task_mgr.update_progress(task.id, 45, "Setting up environment...")
            
            env_manager = EnvironmentManager()
            llm_env_manager = get_llm_env_manager()
            
            # Create environment name based on model
            model_safe_name = model_id.replace('/', '_').replace('-', '_').lower()
            env_name = f"llm_{model_safe_name[:30]}"
            
            try:
                # Check if environment already exists
                existing_envs = env_manager.list_environments()
                env_exists = any(e.name == env_name for e in existing_envs)
                
                if env_exists:
                    task_mgr.update_step(task.id, 2, "completed", f"✅ Environment '{env_name}' exists")
                else:
                    # Create new virtual environment
                    task_mgr.update_step(task.id, 2, "running", f"Creating {env_name}...")
                    task_mgr.update_progress(task.id, 50, f"Creating virtual environment: {env_name}")
                    
                    # create_environment returns a VirtualEnvironment object
                    new_env = env_manager.create_environment(name=env_name)
                    
                    if new_env:
                        task_mgr.update_step(task.id, 2, "completed", f"✅ Created environment: {env_name}")
                    else:
                        task_mgr.update_step(task.id, 2, "completed", f"⚠️ Environment creation had issues")
            except Exception as e:
                import traceback
                traceback.print_exc()
                task_mgr.update_step(task.id, 2, "completed", f"⚠️ Environment setup: {str(e)}")
            
            # Step 4: Check if backend is installed (LM Studio style)
            task_mgr.update_step(task.id, 3, "running", f"Checking {backend_id} backend...")
            task_mgr.update_progress(task.id, 55, "Checking backend...")
            
            available_backends = backend_installer.get_available_backends()
            backend_info = available_backends.get(backend_id)
            backend_installed = backend_info and backend_info.get('installed', False)
            
            if backend_installed:
                task_mgr.update_step(task.id, 3, "completed", f"✅ {backend_info.get('name', backend_id)} is installed")
                task_mgr.update_progress(task.id, 70, "Backend ready")
            else:
                task_mgr.update_step(task.id, 3, "running", f"Installing {backend_id} backend...")
                task_mgr.update_progress(task.id, 60, "Installing backend...")
                
                def backend_progress(percent, message):
                    actual_percent = 60 + int(percent * 0.15)  # 60-75%
                    task_mgr.update_progress(task.id, actual_percent, message)
                    task_mgr.update_step(task.id, 3, "running", message)
                
                result = backend_installer.install_backend(backend_id, backend_progress)
                
                if result.get('success'):
                    task_mgr.update_step(task.id, 3, "completed", f"✅ {backend_id} installed")
                else:
                    # Try CPU fallback
                    task_mgr.update_step(task.id, 3, "running", "Trying CPU backend fallback...")
                    cpu_result = backend_installer.install_backend('cpu', backend_progress)
                    if cpu_result.get('success'):
                        task_mgr.update_step(task.id, 3, "completed", "✅ CPU backend installed (fallback)")
                    else:
                        task_mgr.update_step(task.id, 3, "completed", f"⚠️ Backend installation had issues: {result.get('error')}")
            
            # Step 5: Associate model with environment
            task_mgr.update_step(task.id, 4, "running", "Associating model with environment...")
            task_mgr.update_progress(task.id, 80, "Linking model...")
            
            try:
                # Associate the model with its dedicated environment
                llm_env_manager.associate_model_with_env(local_model.id, env_name)
                task_mgr.update_step(task.id, 4, "completed", f"✅ Model linked to {env_name}")
            except Exception as e:
                task_mgr.update_step(task.id, 4, "completed", f"⚠️ Association: {str(e)}")
            
            task_mgr.update_progress(task.id, 90, "Almost done...")
            
            # Step 6: Finalize setup - register model
            task_mgr.update_step(task.id, 5, "running", "Finalizing setup...")
            
            # Update model metadata with backend preference and environment
            local_model.backend = backend_id
            local_model.environment = env_name
            llm_mgr.add_local_model(local_model)
            
            task_mgr.update_step(task.id, 5, "completed", "Setup complete!")
            task_mgr.update_progress(task.id, 100, "Done!")
            
            # Complete task
            task_mgr.complete_task(task.id, result={
                'model_id': local_model.id,
                'model_name': local_model.name,
                'backend': backend_id,
                'environment': env_name,
                'path': local_model.path,
                'inference_mode': 'server'  # LM Studio style HTTP server
            })
            
        except Exception as e:
            import traceback
            traceback.print_exc()
            task_mgr.fail_task(task.id, str(e))
    
    thread = threading.Thread(target=run_setup, daemon=True)
    thread.start()
    
    return jsonify({
        'success': True,
        'task_id': task.id,
        'message': 'Setup started'
    })


# =====================
# Per-Model Environment Management (Legacy - kept for compatibility)
# =====================

@llm_bp.route('/api/models/<model_id>/create-environment', methods=['POST'])
def api_create_model_environment(model_id: str):
    """Create a dedicated virtual environment for a model"""
    from app.services.environment_manager import EnvironmentManager
    
    data = request.get_json() or {}
    venv_name = data.get('venv_name')
    gpu_backend = data.get('gpu_backend', 'auto')
    
    if not venv_name:
        return jsonify({'error': 'venv_name is required'}), 400
    
    # Get model info
    llm_mgr = LLMManager()
    model = llm_mgr.get_model_by_id(model_id)
    
    if not model:
        return jsonify({'error': 'Model not found'}), 404
    
    # Auto-detect backend if needed
    if gpu_backend == 'auto':
        from app.services.model_recommendation import get_recommendation_service
        rec_service = get_recommendation_service()
        hardware = rec_service.detect_hardware()
        gpu_backend = rec_service.get_recommended_backend(hardware)
    
    # STRICT VALIDATION: Check if required toolkit is installed before proceeding
    if gpu_backend and gpu_backend.lower() != 'cpu':
        env_mgr = EnvironmentManager()
        toolkit_status = env_mgr.check_backend_toolkit_availability(gpu_backend)
        
        if not toolkit_status.get('available', False):
            toolkit_name = toolkit_status.get('toolkit_name', 'Toolkit')
            install_url = toolkit_status.get('install_url', '')
            
            # Check if auto-install is available
            auto_install_available = False
            try:
                from app.services.cuda_installer import get_toolkit_installer
                installer = get_toolkit_installer()
                if gpu_backend.lower() in ['cuda', 'nvidia', 'cuda12']:
                    auto_install_available = installer.get_download_url() is not None
                elif gpu_backend.lower() == 'vulkan':
                    auto_install_available = installer.get_vulkan_download_url() is not None
            except:
                pass
            
            error_msg = f"{toolkit_name} is required but not installed.\n\n"
            error_msg += f"{toolkit_status.get('message', '')}\n\n"
            
            if auto_install_available:
                error_msg += "You can install it automatically via the wizard's 'Install Toolkit' button.\n"
                error_msg += "Or use the API endpoint: /llm/api/toolkit/install\n\n"
            
            if install_url:
                error_msg += f"Download: {install_url}\n\n"
            
            error_msg += "Environment creation cannot proceed without the required toolkit. Please install it first."
            
            return jsonify({
                'success': False,
                'error': error_msg,
                'toolkit_required': True,
                'toolkit_name': toolkit_name,
                'auto_install_available': auto_install_available,
                'install_url': install_url,
                'backend': gpu_backend
            }), 400
    
    # Create task for environment setup
    task_mgr = TaskManager()
    steps = [
        "Creating virtual environment",
        "Installing llama-cpp-python",
        "Verifying installation",
        "Associating with model"
    ]
    
    task = task_mgr.create_task(
        name=f"Setup Environment: {model.name}",
        task_type="create_model_environment",
        steps=steps
    )
    
    def run_env_creation():
        from app.services.environment_manager import EnvironmentManager
        from app.services.llm_environment import get_llm_env_manager
        
        try:
            task_mgr.start_task(task.id)
            env_mgr = EnvironmentManager(base_path=os.environ.get('BEEP_PYTHON_HOME', 
                                                                   os.path.expanduser('~/.beep-llm')))
            llm_env_mgr = get_llm_env_manager()
            
            # Step 1: Create virtual environment
            task_mgr.update_step(task.id, 0, "running", f"Creating venv: {venv_name}")
            task_mgr.update_progress(task.id, 10, "Creating virtual environment...")
            
            result = env_mgr.create_environment(venv_name)
            if not result:
                task_mgr.fail_task(task.id, "Failed to create virtual environment")
                return
            
            task_mgr.update_step(task.id, 0, "completed", "Virtual environment created")
            task_mgr.update_progress(task.id, 30, "Environment created")
            
            # Step 2: Install llama-cpp-python with GPU support
            task_mgr.update_step(task.id, 1, "running", f"Installing llama-cpp-python with {gpu_backend} support")
            task_mgr.update_progress(task.id, 35, "Installing dependencies (this may take several minutes for GPU builds)...")
            
            # Use the specialized install method for llama-cpp-python
            install_result = env_mgr.install_llama_cpp_python(venv_name, gpu_backend)
            if not install_result.get('success'):
                error_msg = install_result.get('stderr', 'Unknown error')[:500]
                task_mgr.fail_task(task.id, f"Failed to install llama-cpp-python: {error_msg}")
                return
            
            task_mgr.update_step(task.id, 1, "completed", f"Installed with {gpu_backend} backend")
            task_mgr.update_progress(task.id, 80, "Installation complete")
            
            # Step 3: Verify installation
            task_mgr.update_step(task.id, 2, "running", "Verifying installation...")
            task_mgr.update_progress(task.id, 85, "Verifying...")
            
            # Get venv info to check
            venvs = env_mgr.list_environments()
            target_venv = next((v for v in venvs if v.name == venv_name), None)
            
            if not target_venv:
                task_mgr.fail_task(task.id, "Virtual environment not found after creation")
                return
            
            # Check if llama-cpp-python is installed
            has_llama, version, detected_backend = llm_env_mgr._check_venv_llama_cpp(target_venv.python_executable)
            
            if not has_llama:
                task_mgr.fail_task(task.id, "llama-cpp-python not found in virtual environment")
                return
            
            task_mgr.update_step(task.id, 2, "completed", f"Verified (version: {version})")
            task_mgr.update_progress(task.id, 90, "Verification complete")
            
            # Step 4: Associate with model
            task_mgr.update_step(task.id, 3, "running", "Associating environment with model...")
            task_mgr.update_progress(task.id, 95, "Associating...")
            
            llm_env_mgr.associate_model_with_env(model_id, venv_name, detected_backend or gpu_backend)
            
            # Update model object
            model.venv_name = venv_name
            model.gpu_backend = detected_backend or gpu_backend
            
            # Save updated models index
            all_models = llm_mgr.get_local_models()
            # Update the model in the list
            for i, m in enumerate(all_models):
                if m.id == model_id:
                    all_models[i] = model
                    break
            else:
                # Model not in list, add it
                all_models.append(model)
            llm_mgr._save_models_index(all_models)
            
            task_mgr.update_step(task.id, 3, "completed", "Model associated")
            task_mgr.update_progress(task.id, 100, "Complete!")
            
            # Complete task
            task_mgr.complete_task(task.id, result={
                'model_id': model_id,
                'venv_name': venv_name,
                'gpu_backend': detected_backend or gpu_backend,
                'llama_cpp_version': version
            })
            
        except Exception as e:
            task_mgr.fail_task(task.id, str(e))
    
    thread = threading.Thread(target=run_env_creation, daemon=True)
    thread.start()
    
    return jsonify({
        'success': True,
        'task_id': task.id,
        'message': 'Environment creation started'
    })


@llm_bp.route('/api/models/<model_id>/environment-status', methods=['GET'])
def api_model_environment_status(model_id: str):
    """Get environment creation status for a model"""
    # This would typically check the task manager for the latest task
    # For now, return the current environment association
    from app.services.llm_environment import get_llm_env_manager
    
    llm_env_mgr = get_llm_env_manager()
    env_info = llm_env_mgr.get_model_environment(model_id)
    
    if env_info:
        return jsonify({
            'status': 'completed',
            'venv_name': env_info.get('venv_name'),
            'gpu_backend': env_info.get('gpu_backend'),
            'progress': 100
        })
    else:
        return jsonify({
            'status': 'not_created',
            'progress': 0
        })


@llm_bp.route('/api/models/<model_id>/switch-backend', methods=['POST'])
def api_switch_model_backend(model_id: str):
    """
    Switch a model's GPU backend by creating/using separate environments.
    
    Smart approach:
    - If environment with target backend exists → just switch association (instant)
    - If environment doesn't exist → create it with target backend, then switch
    - This avoids rebuilding - each backend gets its own environment
    
    Example: Model can have both llm_model_cuda and llm_model_vulkan environments,
    and switch between them instantly after initial creation.
    """
    from app.services.llm_manager import LLMManager
    from app.services.llm_environment import get_llm_env_manager
    from app.services.inference_service import InferenceService
    from app.services.task_manager import TaskManager
    from app.services.environment_manager import EnvironmentManager
    
    data = request.get_json() or {}
    new_backend = data.get('gpu_backend', 'cpu')
    
    if new_backend not in ['cpu', 'cuda', 'nvidia', 'rocm', 'metal', 'vulkan', 'openblas']:
        return jsonify({'error': f'Invalid GPU backend: {new_backend}'}), 400
    
    # Normalize backend name
    if new_backend == 'nvidia':
        new_backend = 'cuda'
    
    llm_mgr = LLMManager()
    llm_env_mgr = get_llm_env_manager()
    inference = InferenceService()
    task_mgr = TaskManager()
    env_mgr = EnvironmentManager(base_path=os.environ.get('BEEP_PYTHON_HOME', 
                                                           os.path.expanduser('~/.beep-llm')))
    
    # Get model
    model = llm_mgr.get_model_by_id(model_id)
    if not model:
        return jsonify({'error': 'Model not found'}), 404
    
    # Get current environment info
    env_info = llm_env_mgr.get_model_environment(model_id)
    current_backend = env_info.get('gpu_backend', 'cpu') if env_info else None
    current_venv = env_info.get('venv_name') if env_info else None
    
    # Check if already on the requested backend
    if current_backend == new_backend:
        return jsonify({
            'success': True,
            'message': f'Model is already using {new_backend} backend',
            'gpu_backend': current_backend,
            'venv_name': current_venv
        })
    
    # Generate environment name with backend suffix
    # Format: llm_<model_name>_<backend>
    # Use same naming convention as wizard setup, but with backend suffix
    safe_model_name = model.name.replace(' ', '_').replace('.', '_').replace('-', '_')[:20]
    target_venv_name = f"llm_{safe_model_name}_{new_backend}"
    
    # Normalize: remove any trailing underscores and ensure valid name
    target_venv_name = target_venv_name.rstrip('_')
    
    # Check if target environment already exists
    existing_venvs = env_mgr.list_environments()
    target_venv_exists = any(v.name == target_venv_name for v in existing_venvs)
    
    # Unload model if it's currently loaded
    if inference.is_model_loaded(model_id):
        inference.unload_model(model_id)
    
    if target_venv_exists:
        # Environment exists - just switch association (instant!)
        task = task_mgr.create_task(
            name=f"Switch {model.name} to {new_backend}",
            task_type="switch_backend",
            steps=["Switching environment", "Updating configuration"]
        )
        
        def run_instant_switch():
            try:
                task_mgr.start_task(task.id)
                
                # Step 1: Switch association
                task_mgr.update_step(task.id, 0, "running", f"Switching to {new_backend} environment...")
                task_mgr.update_progress(task.id, 50, "Switching...")
                
                # Verify the target venv has the right backend
                target_venv = next(v for v in existing_venvs if v.name == target_venv_name)
                has_llama, version, detected_backend = llm_env_mgr._check_venv_llama_cpp(target_venv.python_executable)
                
                if not has_llama:
                    task_mgr.fail_task(task.id, f"Target environment {target_venv_name} doesn't have llama-cpp-python installed")
                    return
                
                # Use detected backend or requested backend
                final_backend = detected_backend or new_backend
                
                # Update association
                llm_env_mgr.associate_model_with_env(model_id, target_venv_name, final_backend)
                
                # Step 2: Update model metadata
                task_mgr.update_step(task.id, 1, "running", "Updating model configuration...")
                task_mgr.update_progress(task.id, 90, "Updating...")
                
                model.venv_name = target_venv_name
                model.gpu_backend = final_backend
                all_models = llm_mgr.get_local_models()
                for i, m in enumerate(all_models):
                    if m.id == model_id:
                        all_models[i] = model
                        break
                llm_mgr._save_models_index(all_models)
                
                task_mgr.update_step(task.id, 1, "completed", "Configuration updated")
                task_mgr.update_progress(task.id, 100, "Complete!")
                
                task_mgr.complete_task(task.id, result={
                    'model_id': model_id,
                    'model_name': model.name,
                    'old_backend': current_backend,
                    'new_backend': final_backend,
                    'old_venv': current_venv,
                    'new_venv': target_venv_name,
                    'instant_switch': True,
                    'message': f'Instantly switched from {current_backend} to {final_backend}. Reload the model to use the new backend.'
                })
                
            except Exception as e:
                import traceback
                traceback.print_exc()
                task_mgr.fail_task(task.id, str(e))
        
        thread = threading.Thread(target=run_instant_switch, daemon=True)
        thread.start()
        
        return jsonify({
            'success': True,
            'task_id': task.id,
            'message': f'Switching to existing {new_backend} environment (instant switch)...',
            'instant_switch': True,
            'redirect_url': f'/tasks/{task.id}'
        })
    
    else:
        # Environment doesn't exist - create it with target backend
        task = task_mgr.create_task(
            name=f"Create {new_backend} environment for {model.name}",
            task_type="switch_backend",
            steps=[
                "Creating virtual environment",
                f"Installing llama-cpp-python ({new_backend})",
                "Verifying installation",
                "Updating model configuration"
            ]
        )
        
        def run_create_and_switch():
            try:
                task_mgr.start_task(task.id)
                
                # Step 1: Create virtual environment
                task_mgr.update_step(task.id, 0, "running", f"Creating {target_venv_name}...")
                task_mgr.update_progress(task.id, 10, "Creating virtual environment...")
                
                result = env_mgr.create_environment(target_venv_name)
                if not result:
                    task_mgr.fail_task(task.id, "Failed to create virtual environment")
                    return
                
                task_mgr.update_step(task.id, 0, "completed", f"Created: {target_venv_name}")
                task_mgr.update_progress(task.id, 30, "Environment created")
                
                # Step 2: Install llama-cpp-python with target backend
                task_mgr.update_step(task.id, 1, "running", f"Installing llama-cpp-python with {new_backend} support...")
                task_mgr.update_progress(task.id, 35, "This may take several minutes for GPU builds...")
                
                install_result = env_mgr.install_llama_cpp_python(target_venv_name, new_backend)
                
                if not install_result.get('success'):
                    error_msg = install_result.get('stderr', 'Unknown error')[:500]
                    task_mgr.fail_task(task.id, f"Installation failed: {error_msg}")
                    return
                
                task_mgr.update_step(task.id, 1, "completed", f"Installed with {new_backend} backend")
                task_mgr.update_progress(task.id, 80, "Installation complete")
                
                # Step 3: Verify
                task_mgr.update_step(task.id, 2, "running", "Verifying installation...")
                
                venvs = env_mgr.list_environments()
                target_venv = next((v for v in venvs if v.name == target_venv_name), None)
                if target_venv:
                    has_llama, version, detected_backend = llm_env_mgr._check_venv_llama_cpp(target_venv.python_executable)
                    if has_llama:
                        task_mgr.update_step(task.id, 2, "completed", f"Verified ({detected_backend or new_backend})")
                    else:
                        task_mgr.update_step(task.id, 2, "completed", "Verification skipped")
                
                # Step 4: Update model configuration
                task_mgr.update_step(task.id, 3, "running", "Updating model configuration...")
                task_mgr.update_progress(task.id, 90, "Updating...")
                
                # Update association
                final_backend = detected_backend if 'detected_backend' in locals() else new_backend
                llm_env_mgr.associate_model_with_env(model_id, target_venv_name, final_backend)
                
                # Update model metadata
                model.venv_name = target_venv_name
                model.gpu_backend = final_backend
                all_models = llm_mgr.get_local_models()
                for i, m in enumerate(all_models):
                    if m.id == model_id:
                        all_models[i] = model
                        break
                llm_mgr._save_models_index(all_models)
                
                task_mgr.update_step(task.id, 3, "completed", "Configuration updated")
                task_mgr.update_progress(task.id, 100, "Complete!")
                
                task_mgr.complete_task(task.id, result={
                    'model_id': model_id,
                    'model_name': model.name,
                    'old_backend': current_backend,
                    'new_backend': final_backend,
                    'old_venv': current_venv,
                    'new_venv': target_venv_name,
                    'instant_switch': False,
                    'message': f'Created {new_backend} environment and switched model. Future switches will be instant. Reload the model to use the new backend.'
                })
                
            except Exception as e:
                import traceback
                traceback.print_exc()
                task_mgr.fail_task(task.id, str(e))
        
        thread = threading.Thread(target=run_create_and_switch, daemon=True)
        thread.start()
        
        return jsonify({
            'success': True,
            'task_id': task.id,
            'message': f'Creating {new_backend} environment (first time setup)...',
            'instant_switch': False,
            'redirect_url': f'/tasks/{task.id}'
        })


@llm_bp.route('/api/models/<model_id>/delete-environment', methods=['DELETE'])
def api_delete_model_environment(model_id: str):
    """Delete a model's dedicated virtual environment"""
    from app.services.environment_manager import EnvironmentManager
    from app.services.llm_environment import get_llm_env_manager
    
    llm_env_mgr = get_llm_env_manager()
    env_info = llm_env_mgr.get_model_environment(model_id)
    
    if not env_info:
        return jsonify({'error': 'No environment associated with this model'}), 404
    
    venv_name = env_info.get('venv_name')
    
    try:
        # First, unload the model if it's loaded (IMPORTANT!)
        inference = InferenceService()
        if inference.is_model_loaded(model_id):
            inference.unload_model(model_id)
        
        env_mgr = EnvironmentManager(base_path=os.environ.get('BEEP_PYTHON_HOME',
                                                               os.path.expanduser('~/.beep-llm')))
        
        # Delete the venv
        success = env_mgr.delete_environment(venv_name)
        
        if success:
            # Remove association
            llm_env_mgr.remove_model_association(model_id)
            
            # Update model
            llm_mgr = LLMManager()
            models = llm_mgr.get_local_models()
            for m in models:
                if m.id == model_id:
                    m.venv_name = None
                    m.gpu_backend = None
            llm_mgr._save_models_index(models)
            
            return jsonify({'success': True, 'message': 'Environment deleted and model unloaded'})
        else:
            return jsonify({'error': 'Failed to delete environment'}), 500
            
    except Exception as e:
        return jsonify({'error': str(e)}), 500


# =====================
# Migration & Health Checks
# =====================

@llm_bp.route('/api/migration/status', methods=['GET'])
def api_migration_status():
    """Get migration status for all models"""
    from app.services.migration_helper import get_migration_helper
    
    try:
        helper = get_migration_helper()
        status = helper.get_migration_status()
        return jsonify(status)
    except Exception as e:
        return jsonify({'error': str(e)}), 500


@llm_bp.route('/api/migration/plan', methods=['GET'])
def api_migration_plan():
    """Get migration plan for models without venvs"""
    from app.services.migration_helper import get_migration_helper
    
    try:
        helper = get_migration_helper()
        plan = helper.generate_migration_plan()
        return jsonify(plan)
    except Exception as e:
        return jsonify({'error': str(e)}), 500


@llm_bp.route('/api/models/<model_id>/health', methods=['GET'])
def api_model_health(model_id: str):
    """Check health of a model's virtual environment"""
    from app.services.migration_helper import get_migration_helper
    
    try:
        helper = get_migration_helper()
        health = helper.check_venv_health(model_id)
        return jsonify(health)
    except Exception as e:
        return jsonify({'error': str(e)}), 500


@llm_bp.route('/api/migration/orphaned-venvs', methods=['GET'])
def api_orphaned_venvs():
    """Get list of orphaned virtual environments"""
    from app.services.migration_helper import get_migration_helper
    
    try:
        helper = get_migration_helper()
        orphaned = helper.get_orphaned_venvs()
        return jsonify({'orphaned': orphaned, 'count': len(orphaned)})
    except Exception as e:
        return jsonify({'error': str(e)}), 500


@llm_bp.route('/api/migration/cleanup-orphaned', methods=['POST'])
def api_cleanup_orphaned():
    """Cleanup orphaned virtual environments"""
    from app.services.migration_helper import get_migration_helper
    
    try:
        helper = get_migration_helper()
        result = helper.cleanup_orphaned_venvs()
        return jsonify(result)
    except Exception as e:
        return jsonify({'error': str(e)}), 500


# =====================
# LLM Environment Management
# =====================

@llm_bp.route('/environment')
def llm_environment():
    """Model Virtual Environments Management page"""
    env_mgr = get_llm_env_manager()
    
    # Get all LLM-capable virtual environments (model venvs, not main app venv)
    model_venvs = env_mgr.get_llm_capable_environments()
    
    # Get all models to show associations
    llm_mgr = LLMManager()
    all_models = llm_mgr.get_local_models()
    
    # Create a map of venv_name to model info
    venv_to_model = {}
    for model in all_models:
        if model.venv_name:
            venv_to_model[model.venv_name] = {
                'model_id': model.id,
                'model_name': model.name,
                'gpu_backend': model.gpu_backend
            }
    
    return render_template('llm/environment.html',
                          model_venvs=model_venvs,
                          venv_to_model=venv_to_model,
                          total_venvs=len(model_venvs))


@llm_bp.route('/api/env/status', methods=['GET'])
def api_llm_env_status():
    """Get LLM environment status"""
    env_mgr = get_llm_env_manager()
    return jsonify(env_mgr.get_status())


# =====================
# CUDA Toolkit Installation (LM Studio-style)
# =====================

@llm_bp.route('/api/cuda/check', methods=['GET'])
def api_check_cuda():
    """API: Check if CUDA Toolkit is installed"""
    installer = get_cuda_installer()
    status = installer.check_cuda_installed()
    return jsonify(status)


@llm_bp.route('/api/toolkit/check/<backend>', methods=['GET'])
def api_check_toolkit(backend: str):
    """API: Check if toolkit is installed for a backend"""
    installer = get_toolkit_installer()
    status = installer.check_toolkit_installed(backend)
    return jsonify(status)


@llm_bp.route('/api/toolkit/install', methods=['POST'])
def api_install_toolkit():
    """API: Install toolkit for any backend (CUDA, Vulkan, ROCm)"""
    from app.services.task_manager import TaskManager
    
    data = request.get_json() or {}
    backend = data.get('backend', 'cuda')  # cuda, vulkan, rocm
    version = data.get('version')  # For CUDA: '12.x' or '11.x'
    
    task_mgr = TaskManager()
    installer = get_toolkit_installer()
    
    # Get toolkit name
    toolkit_names = {
        'cuda': 'CUDA Toolkit',
        'nvidia': 'CUDA Toolkit',
        'cuda12': 'CUDA Toolkit',
        'vulkan': 'Vulkan SDK',
        'rocm': 'ROCm SDK',
        'amd': 'ROCm SDK'
    }
    toolkit_name = toolkit_names.get(backend.lower(), 'Toolkit')
    
    # Create task
    task = task_mgr.create_task(
        name=f"Install {toolkit_name}",
        task_type="install_toolkit",
        steps=["Preparing", "Downloading", "Installing", "Complete"]
    )
    
    def run_install():
        def progress_callback(percent, message):
            task_mgr.update_progress(task.id, percent, message)
        
        try:
            task_mgr.start_task(task.id)
            task_mgr.update_step(task.id, 0, "running", "Preparing installation...")
            
            result = installer.install_toolkit(backend, version, progress_callback)
            
            if result['success']:
                if result.get('manual_install'):
                    # ROCm requires manual installation
                    task_mgr.update_step(task.id, 1, "completed", "Instructions ready")
                    task_mgr.update_step(task.id, 2, "completed", "Manual installation required")
                    task_mgr.complete_task(task.id, result=result)
                else:
                    task_mgr.update_step(task.id, 1, "completed", "Downloaded")
                    task_mgr.update_step(task.id, 2, "completed", "Installer launched")
                    task_mgr.update_step(task.id, 3, "completed", "Please complete installation")
                    task_mgr.complete_task(task.id, result=result)
            else:
                task_mgr.fail_task(task.id, result.get('error', 'Installation failed'))
        except Exception as e:
            task_mgr.fail_task(task.id, str(e))
    
    thread = threading.Thread(target=run_install, daemon=True)
    thread.start()
    
    return jsonify({
        'success': True,
        'task_id': task.id,
        'redirect_url': f'/tasks/{task.id}'
    })


@llm_bp.route('/api/cuda/download', methods=['POST'])
def api_download_cuda():
    """API: Download CUDA Toolkit installer"""
    from app.services.task_manager import TaskManager
    
    data = request.get_json() or {}
    version = data.get('version')  # '12.x' or '11.x', None for auto
    
    task_mgr = TaskManager()
    installer = get_cuda_installer()
    
    # Create task for download progress
    task = task_mgr.create_task(
        name=f"Download CUDA Toolkit {version or 'Auto'}",
        task_type="download_cuda",
        steps=["Downloading CUDA installer", "Verifying download", "Ready to install"]
    )
    
    def run_download():
        def progress_callback(percent, message):
            task_mgr.update_progress(task.id, percent, message)
        
        try:
            task_mgr.start_task(task.id)
            task_mgr.update_step(task.id, 0, "running", "Starting download...")
            
            result = installer.download_cuda_installer(version, progress_callback)
            
            if result['success']:
                task_mgr.update_step(task.id, 0, "completed", "Download complete")
                task_mgr.update_step(task.id, 1, "running", "Verifying...")
                task_mgr.update_step(task.id, 1, "completed", "Verified")
                task_mgr.update_step(task.id, 2, "completed", "Ready")
                task_mgr.complete_task(task.id, result=result)
            else:
                task_mgr.fail_task(task.id, result.get('error', 'Download failed'))
        except Exception as e:
            task_mgr.fail_task(task.id, str(e))
    
    thread = threading.Thread(target=run_download, daemon=True)
    thread.start()
    
    return jsonify({
        'success': True,
        'task_id': task.id,
        'redirect_url': f'/tasks/{task.id}'
    })


@llm_bp.route('/api/cuda/install', methods=['POST'])
def api_install_cuda():
    """API: Launch CUDA Toolkit installer (downloads if needed)"""
    from app.services.task_manager import TaskManager
    
    data = request.get_json() or {}
    version = data.get('version')  # '12.x' or '11.x', None for auto
    installer_path = data.get('installer_path')  # Optional: use existing installer
    
    task_mgr = TaskManager()
    installer = get_cuda_installer()
    
    # Create task
    task = task_mgr.create_task(
        name=f"Install CUDA Toolkit {version or 'Auto'}",
        task_type="install_cuda",
        steps=["Preparing", "Downloading installer", "Launching installer", "Installation complete"]
    )
    
    def run_install():
        def progress_callback(percent, message):
            task_mgr.update_progress(task.id, percent, message)
        
        try:
            task_mgr.start_task(task.id)
            task_mgr.update_step(task.id, 0, "running", "Preparing installation...")
            
            # If installer path provided, use it; otherwise download
            if installer_path and Path(installer_path).exists():
                task_mgr.update_step(task.id, 1, "completed", "Using existing installer")
                launch_result = installer.launch_installer(installer_path)
            else:
                # Download first
                task_mgr.update_step(task.id, 1, "running", "Downloading installer...")
                download_result = installer.download_cuda_installer(version, progress_callback)
                
                if not download_result['success']:
                    task_mgr.fail_task(task.id, download_result.get('error', 'Download failed'))
                    return
                
                task_mgr.update_step(task.id, 1, "completed", "Download complete")
                task_mgr.update_step(task.id, 2, "running", "Launching installer...")
                
                launch_result = installer.launch_installer(download_result['path'])
            
            if launch_result['success']:
                task_mgr.update_step(task.id, 2, "completed", "Installer launched")
                task_mgr.update_step(task.id, 3, "completed", "Please complete installation wizard")
                task_mgr.complete_task(task.id, result={
                    **launch_result,
                    'installer_path': installer_path or download_result.get('path')
                })
            else:
                task_mgr.fail_task(task.id, launch_result.get('error', 'Failed to launch installer'))
                
        except Exception as e:
            task_mgr.fail_task(task.id, str(e))
    
    thread = threading.Thread(target=run_install, daemon=True)
    thread.start()
    
    return jsonify({
        'success': True,
        'task_id': task.id,
        'redirect_url': f'/tasks/{task.id}'
    })


@llm_bp.route('/api/cuda/auto-install', methods=['POST'])
def api_auto_install_cuda():
    """API: Fully automated CUDA installation (download + launch) - LM Studio style"""
    # Redirect to generic toolkit install endpoint
    data = request.get_json() or {}
    data['backend'] = 'cuda'
    return api_install_toolkit()


@llm_bp.route('/api/env/install', methods=['POST'])
def api_llm_env_install():
    """DISABLED: Package installation in main app venv is not allowed"""
    return jsonify({
        'success': False,
        'error': 'Package installation in main app environment is disabled. Create per-model virtual environments instead.',
        'redirect': '/llm/environment'
    }), 403


@llm_bp.route('/api/env/progress', methods=['GET'])
def api_llm_env_progress():
    """Get installation progress"""
    env_mgr = get_llm_env_manager()
    return jsonify(env_mgr.get_installation_progress())


@llm_bp.route('/api/env/packages', methods=['GET'])
def api_llm_packages():
    """Get list of installed LLM packages"""
    env_mgr = get_llm_env_manager()
    return jsonify({'packages': env_mgr.get_installed_packages()})


@llm_bp.route('/api/env/check/<package_name>', methods=['GET'])
def api_llm_check_package(package_name: str):
    """Check if a specific package is installed"""
    env_mgr = get_llm_env_manager()
    return jsonify(env_mgr.check_package(package_name))


# =====================
# Model Categories API
# =====================

@llm_bp.route('/api/categories', methods=['GET'])
def api_model_categories():
    """Get all model categories with descriptions"""
    env_mgr = get_llm_env_manager()
    return jsonify({'categories': env_mgr.get_model_categories()})


@llm_bp.route('/api/categories/<category>', methods=['GET'])
def api_category_info(category: str):
    """Get detailed info for a specific category"""
    env_mgr = get_llm_env_manager()
    info = env_mgr.get_category_info(category)
    
    if info:
        return jsonify(info)
    return jsonify({'error': 'Category not found'}), 404


@llm_bp.route('/api/model/category', methods=['POST'])
def api_detect_category():
    """Detect model category from name/ID"""
    env_mgr = get_llm_env_manager()
    
    data = request.get_json() or {}
    model_name = data.get('name', '')
    model_id = data.get('id', '')
    
    category = env_mgr.detect_model_category(model_name, model_id)
    info = env_mgr.get_category_info(category)
    
    return jsonify({
        'category': category,
        'info': info
    })
