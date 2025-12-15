"""
Document Extraction Routes

Standalone document extraction feature - separate from RAG.
"""
from flask import Blueprint, render_template, request, jsonify
from werkzeug.utils import secure_filename
import logging

logger = logging.getLogger(__name__)

document_extraction_bp = Blueprint('document_extraction', __name__)


@document_extraction_bp.route('/')
def index():
    """Document extraction dashboard"""
    from app.services.document_extraction_environment import get_doc_extraction_env
    from app.services.document_extractor import get_document_extractor
    from app.services.ocr_engine_tracker import get_ocr_engine_tracker
    
    env_mgr = get_doc_extraction_env()
    extractor = get_document_extractor()
    ocr_tracker = get_ocr_engine_tracker()
    
    env_status = env_mgr.get_status()
    extractor_status = extractor.get_status()
    ocr_engines = ocr_tracker.get_all_engines()
    
    return render_template('document_extraction/index.html',
                          env_status=env_status,
                          extractor_status=extractor_status,
                          ocr_engines=ocr_engines)


@document_extraction_bp.route('/api/status', methods=['GET'])
def api_status():
    """Get document extraction environment status"""
    from app.services.document_extraction_environment import get_doc_extraction_env
    from app.services.document_extractor import get_document_extractor
    
    try:
        env_mgr = get_doc_extraction_env()
        extractor = get_document_extractor()
        
        env_status = env_mgr.get_status()
        extractor_status = extractor.get_status()
        
        return jsonify({
            'success': True,
            'environment': env_status,
            'extractor': extractor_status
        })
    except Exception as e:
        logger.error(f"Error in api_status: {e}", exc_info=True)
        return jsonify({
            'success': False,
            'error': str(e)
        }), 500


@document_extraction_bp.route('/api/health', methods=['GET'])
def api_health():
    """Health check endpoint"""
    return jsonify({
        'success': True,
        'status': 'ok',
        'message': 'Document extraction service is running'
    })


@document_extraction_bp.route('/api/create-env', methods=['POST'])
def api_create_env():
    """Create document extraction environment"""
    from app.services.document_extraction_environment import get_doc_extraction_env
    from app.services.task_manager import TaskManager
    
    env_mgr = get_doc_extraction_env()
    task_mgr = TaskManager()
    
    task = task_mgr.create_task(
        name="Create Document Extraction Environment",
        task_type="doc_extraction_setup",
        steps=[
            "Creating virtual environment",
            "Upgrading pip",
            "Environment ready"
        ]
    )
    
    def run_creation():
        try:
            task_mgr.start_task(task.id)
            task_mgr.update_step(task.id, 0, "running", "Creating virtual environment...")
            task_mgr.update_progress(task.id, 10, "Creating virtual environment...")
            
            def progress_callback(step, progress, message):
                if step == 'creating':
                    task_mgr.update_step(task.id, 0, "running", message)
                    task_mgr.update_progress(task.id, progress, message)
                elif step == 'installing':
                    task_mgr.update_step(task.id, 1, "running", message)
                    task_mgr.update_progress(task.id, progress, message)
                elif step == 'complete':
                    task_mgr.update_step(task.id, 2, "completed", message)
                    task_mgr.update_progress(task.id, 100, message)
            
            result = env_mgr.create_environment(progress_callback)
            
            if result.get('success'):
                task_mgr.complete_task(task.id, result)
            else:
                task_mgr.fail_task(task.id, result.get('error', 'Unknown error'))
        except Exception as e:
            task_mgr.fail_task(task.id, str(e))
    
    import threading
    thread = threading.Thread(target=run_creation, daemon=True)
    thread.start()
    
    return jsonify({
        'success': True,
        'task_id': task.id,
        'message': 'Environment creation started'
    })


@document_extraction_bp.route('/api/install-packages', methods=['POST'])
def api_install_packages():
    """Install document extraction packages"""
    from app.services.document_extraction_environment import get_doc_extraction_env
    from app.services.task_manager import TaskManager
    
    data = request.get_json() or {}
    package_names = data.get('packages')  # Optional list, defaults to required packages
    
    env_mgr = get_doc_extraction_env()
    task_mgr = TaskManager()
    
    task = task_mgr.create_task(
        name="Install Document Extraction Packages",
        task_type="doc_extraction_install",
        steps=[f"Installing {len(package_names) if package_names else 'required'} packages"]
    )
    
    def run_installation():
        try:
            task_mgr.start_task(task.id)
            task_mgr.update_progress(task.id, 0, 'Starting package installation...')
            
            def progress_callback(step, progress, message):
                task_mgr.update_progress(task.id, progress, message)
                task_mgr.update_step(task.id, 0, "running", message)
            
            result = env_mgr.install_packages(package_names, progress_callback)
            
            if result.get('success'):
                # Include detailed results in task completion
                task_mgr.complete_task(task.id, {
                    'message': result.get('message', 'Installation completed'),
                    'installed': result.get('installed', []),
                    'failed': result.get('failed', []),
                    'package_status': result.get('package_status', {})
                })
            else:
                # Include failed packages in error message
                error_msg = result.get('error', 'Unknown error')
                if result.get('failed'):
                    failed_pkgs = [f['package'] for f in result.get('failed', [])]
                    error_msg += f". Failed packages: {', '.join(failed_pkgs)}"
                task_mgr.fail_task(task.id, error_msg)
        except Exception as e:
            import traceback
            error_details = traceback.format_exc()
            logger.error(f"Installation thread error: {error_details}")
            task_mgr.fail_task(task.id, f"Installation error: {str(e)}")
    
    import threading
    thread = threading.Thread(target=run_installation, daemon=True)
    thread.start()
    
    return jsonify({
        'success': True,
        'task_id': task.id,
        'message': 'Package installation started'
    })


@document_extraction_bp.route('/api/extract', methods=['POST'])
def api_extract():
    """Extract text from uploaded document - synchronous for single file"""
    from app.services.document_extractor import get_document_extractor
    from app.services.document_extraction_environment import get_doc_extraction_env
    import json
    import base64
    from datetime import datetime
    
    try:
        # Validate request
        if 'file' not in request.files:
            logger.warning("Extract request missing file")
            return jsonify({'success': False, 'error': 'No file provided'}), 400
        
        file = request.files['file']
        if not file or not file.filename:
            logger.warning("Extract request with invalid file")
            return jsonify({'success': False, 'error': 'Invalid file'}), 400
        
        filename = secure_filename(file.filename)
        logger.info(f"Extracting text from: {filename}")
        
        # Read file content
        try:
            file_content = file.read()
            if not file_content:
                return jsonify({'success': False, 'error': 'File is empty'}), 400
        except Exception as e:
            logger.error(f"Error reading file {filename}: {e}")
            return jsonify({'success': False, 'error': f'Error reading file: {str(e)}'}), 400
        
        # Check environment status
        try:
            env_mgr = get_doc_extraction_env()
            env_status = env_mgr.get_status()
            if env_status.get('status') != 'ready':
                logger.warning(f"Environment not ready: {env_status.get('status')}")
                return jsonify({
                    'success': False,
                    'error': f"Document extraction environment is not ready. Status: {env_status.get('status')}. Please set up the environment first."
                }), 503
        except Exception as e:
            logger.error(f"Error checking environment status: {e}")
            return jsonify({
                'success': False,
                'error': f'Error checking environment: {str(e)}'
            }), 500
        
        # Get extractor and extract
        try:
            extractor = get_document_extractor()
            # Refresh library checks to ensure latest status (especially after package installation)
            extractor.refresh_libraries()
            
            logger.info(f"Starting extraction for {filename}")
            result = extractor.extract(file_content=file_content, filename=filename)
            
            if not result:
                logger.error(f"Extractor returned None for {filename}")
                return jsonify({'success': False, 'error': 'Extraction returned no result'}), 500
            
            logger.info(f"Extraction {'successful' if result.success else 'failed'} for {filename}")
            if not result.success:
                logger.warning(f"Extraction error for {filename}: {result.error}")
        except Exception as e:
            logger.error(f"Error during extraction for {filename}: {e}", exc_info=True)
            return jsonify({
                'success': False,
                'text': '',
                'metadata': {},
                'error': f'Extraction error: {str(e)}'
            }), 500
        
        # Optionally save extracted text to storage path
        save_to_file = request.form.get('save', 'false').lower() == 'true'
        saved_path = None
        if save_to_file and result.success:
            try:
                storage_path = env_mgr.get_storage_path()
                
                # Create filename with timestamp
                from pathlib import Path
                file_stem = Path(filename).stem
                timestamp = datetime.now().strftime('%Y%m%d_%H%M%S')
                output_filename = f"{file_stem}_{timestamp}.txt"
                saved_path = storage_path / output_filename
                
                with open(saved_path, 'w', encoding='utf-8') as f:
                    f.write(result.text)
                saved_path = str(saved_path)
                logger.info(f"Saved extracted text to: {saved_path}")
            except Exception as e:
                logger.error(f"Failed to save extracted text: {e}")
                # Don't fail the request if save fails
        
        return jsonify({
            'success': result.success,
            'text': result.text or '',
            'metadata': result.metadata or {},
            'error': result.error,
            'saved_path': saved_path
        })
    except Exception as e:
        logger.error(f"Exception in extract endpoint: {e}", exc_info=True)
        import traceback
        error_trace = traceback.format_exc()
        logger.error(f"Full traceback:\n{error_trace}")
        return jsonify({
            'success': False,
            'text': '',
            'metadata': {},
            'error': f'Server error: {str(e)}'
        }), 500


@document_extraction_bp.route('/api/ocr-engines', methods=['GET'])
def api_get_ocr_engines():
    """Get available OCR engines and their status"""
    from app.services.ocr_engine_tracker import get_ocr_engine_tracker, OCREngineType
    from app.services.document_extractor import get_document_extractor
    from app.services.document_extraction_environment import get_doc_extraction_env
    
    try:
        ocr_tracker = get_ocr_engine_tracker()
        extractor = get_document_extractor()
        env_mgr = get_doc_extraction_env()
        
        # Refresh library checks to get latest status
        extractor.refresh_libraries()
        
        # Also check from environment manager's package list
        env_mgr._check_installed_packages()
        
        # Check which engines are installed
        engines = ocr_tracker.get_all_engines()
        libraries = extractor._libraries_available
        
        # Update installed status from both sources
        for engine_type, engine_info in ocr_tracker.AVAILABLE_ENGINES.items():
            if engine_type == OCREngineType.EASYOCR:
                # Check both extractor and env_mgr
                installed = libraries.get('easyocr', False)
                if not installed and 'easyocr' in env_mgr.DOC_EXTRACTION_PACKAGES:
                    installed = env_mgr.DOC_EXTRACTION_PACKAGES['easyocr'].installed
                engines[engine_type.value]['installed'] = installed
            elif engine_type == OCREngineType.TESSERACT:
                installed = libraries.get('tesseract', False)
                if not installed and 'pytesseract' in env_mgr.DOC_EXTRACTION_PACKAGES:
                    installed = env_mgr.DOC_EXTRACTION_PACKAGES['pytesseract'].installed
                engines[engine_type.value]['installed'] = installed
            elif engine_type == OCREngineType.PADDLEOCR:
                installed = libraries.get('paddleocr', False)
                if not installed and 'paddleocr' in env_mgr.DOC_EXTRACTION_PACKAGES:
                    installed = env_mgr.DOC_EXTRACTION_PACKAGES['paddleocr'].installed
                engines[engine_type.value]['installed'] = installed
        
        active_engine = ocr_tracker.get_active_engine()
        
        return jsonify({
            'success': True,
            'engines': engines,
            'active_engine': active_engine.value if active_engine else None
        })
    except Exception as e:
        logger.error(f"Error getting OCR engines: {e}", exc_info=True)
        return jsonify({
            'success': False,
            'error': str(e)
        }), 500


@document_extraction_bp.route('/api/ocr-engines/<engine_type>', methods=['POST'])
def api_activate_ocr_engine(engine_type: str):
    """Activate an OCR engine"""
    from app.services.ocr_engine_tracker import get_ocr_engine_tracker, OCREngineType
    
    try:
        ocr_tracker = get_ocr_engine_tracker()
        
        # Validate engine type
        try:
            engine_enum = OCREngineType(engine_type)
        except ValueError:
            return jsonify({
                'success': False,
                'error': f'Invalid OCR engine type: {engine_type}'
            }), 400
        
        # Check if engine is installed
        engine_info = ocr_tracker.get_engine_info(engine_enum)
        if not engine_info:
            return jsonify({
                'success': False,
                'error': f'OCR engine not found: {engine_type}'
            }), 404
        
        # Activate the engine
        previous_engine = ocr_tracker.activate_engine(engine_enum)
        
        return jsonify({
            'success': True,
            'active_engine': engine_type,
            'previous_engine': previous_engine.value if previous_engine else None,
            'message': f'OCR engine {engine_info.name} activated'
        })
    except Exception as e:
        logger.error(f"Error activating OCR engine: {e}", exc_info=True)
        return jsonify({
            'success': False,
            'error': str(e)
        }), 500


@document_extraction_bp.route('/api/settings', methods=['GET'])
def api_get_settings():
    """Get document extraction settings"""
    from app.models.core import Setting
    
    settings = {
        'env_path': Setting.get('doc_extraction_env_path', ''),
        'storage_path': Setting.get('doc_extraction_storage_path', '')
    }
    
    return jsonify({
        'success': True,
        'settings': settings
    })


@document_extraction_bp.route('/api/settings', methods=['POST'])
def api_update_settings():
    """Update document extraction settings"""
    from app.models.core import Setting
    from app.services.document_extraction_environment import get_doc_extraction_env
    from pathlib import Path
    
    data = request.get_json() or {}
    
    if 'storage_path' in data:
        storage_path = data['storage_path']
        try:
            path_obj = Path(storage_path)
            path_obj.mkdir(parents=True, exist_ok=True)
            env_mgr = get_doc_extraction_env()
            env_mgr.set_storage_path(storage_path)
        except Exception as e:
            return jsonify({'success': False, 'error': f'Invalid path: {str(e)}'}), 400
    
    if 'env_path' in data:
        env_path = data['env_path']
        Setting.set('doc_extraction_env_path', env_path,
                   'Document extraction virtual environment path')
    
    return jsonify({
        'success': True,
        'message': 'Settings updated'
    })
