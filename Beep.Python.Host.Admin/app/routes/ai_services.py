"""
AI Services Routes

Routes for various AI services:
- Text to Image
- Text to Speech
- Speech to Text
- Voice to Voice
"""
from flask import Blueprint, render_template, request, jsonify
from app.services.ai_services_environment import AIServiceType, get_ai_service_env, SERVICE_PACKAGES
from app.services.task_manager import TaskManager
import logging

logger = logging.getLogger(__name__)

ai_services_bp = Blueprint('ai_services', __name__)


def _get_service_info(service_type: AIServiceType):
    """Get service information"""
    service_info = {
        AIServiceType.TEXT_TO_IMAGE: {
            'name': 'Text to Image',
            'icon': 'bi-image',
            'description': 'Generate images from text descriptions using AI models like Stable Diffusion',
            'route': 'text_to_image'
        },
        AIServiceType.TEXT_TO_SPEECH: {
            'name': 'Text to Speech',
            'icon': 'bi-volume-up',
            'description': 'Convert text to natural-sounding speech',
            'route': 'text_to_speech'
        },
        AIServiceType.SPEECH_TO_TEXT: {
            'name': 'Speech to Text',
            'icon': 'bi-mic',
            'description': 'Transcribe speech/audio to text using AI models',
            'route': 'speech_to_text'
        },
        AIServiceType.VOICE_TO_VOICE: {
            'name': 'Voice to Voice',
            'icon': 'bi-arrow-left-right',
            'description': 'Voice conversion, cloning, and transformation',
            'route': 'voice_to_voice'
        },
        AIServiceType.OBJECT_DETECTION: {
            'name': 'Object Detection',
            'icon': 'bi-bounding-box',
            'description': 'Detect and identify objects in images using YOLO, DETR, and other models',
            'route': 'object_detection'
        },
        AIServiceType.TABULAR_TIME_SERIES: {
            'name': 'Tabular & Time Series',
            'icon': 'bi-graph-up',
            'description': 'Time series forecasting, tabular classification, and regression using TimesFM, TTM, Chronos, and other models',
            'route': 'tabular_time_series'
        }
    }
    return service_info.get(service_type, {})


@ai_services_bp.route('/text-to-image')
def text_to_image():
    """Text to Image service page"""
    env_mgr = get_ai_service_env(AIServiceType.TEXT_TO_IMAGE)
    env_status = env_mgr.get_status()
    service_info = _get_service_info(AIServiceType.TEXT_TO_IMAGE)
    
    return render_template('ai_services/text_to_image.html',
                          env_status=env_status,
                          service_info=service_info)


@ai_services_bp.route('/text-to-speech')
def text_to_speech():
    """Text to Speech service page"""
    env_mgr = get_ai_service_env(AIServiceType.TEXT_TO_SPEECH)
    env_status = env_mgr.get_status()
    service_info = _get_service_info(AIServiceType.TEXT_TO_SPEECH)
    
    return render_template('ai_services/text_to_speech.html',
                          env_status=env_status,
                          service_info=service_info)


@ai_services_bp.route('/speech-to-text')
def speech_to_text():
    """Speech to Text service page"""
    env_mgr = get_ai_service_env(AIServiceType.SPEECH_TO_TEXT)
    env_status = env_mgr.get_status()
    service_info = _get_service_info(AIServiceType.SPEECH_TO_TEXT)
    
    return render_template('ai_services/speech_to_text.html',
                          env_status=env_status,
                          service_info=service_info)


@ai_services_bp.route('/voice-to-voice')
def voice_to_voice():
    """Voice to Voice service page"""
    env_mgr = get_ai_service_env(AIServiceType.VOICE_TO_VOICE)
    env_status = env_mgr.get_status()
    service_info = _get_service_info(AIServiceType.VOICE_TO_VOICE)
    
    return render_template('ai_services/voice_to_voice.html',
                          env_status=env_status,
                          service_info=service_info)


@ai_services_bp.route('/object-detection')
def object_detection():
    """Object Detection service page"""
    env_mgr = get_ai_service_env(AIServiceType.OBJECT_DETECTION)
    env_status = env_mgr.get_status()
    service_info = _get_service_info(AIServiceType.OBJECT_DETECTION)
    
    return render_template('ai_services/object_detection.html',
                          env_status=env_status,
                          service_info=service_info)


@ai_services_bp.route('/tabular-time-series')
def tabular_time_series():
    """Tabular & Time Series service page"""
    env_mgr = get_ai_service_env(AIServiceType.TABULAR_TIME_SERIES)
    env_status = env_mgr.get_status()
    service_info = _get_service_info(AIServiceType.TABULAR_TIME_SERIES)
    
    return render_template('ai_services/tabular_time_series.html',
                          env_status=env_status,
                          service_info=service_info)


# Model Discovery and Management Pages
@ai_services_bp.route('/text-to-image/models')
def text_to_image_models():
    """View and manage text-to-image models"""
    return redirect(url_for('ai_services.service_models', service_type='text-to-image'))


@ai_services_bp.route('/text-to-image/discover')
def text_to_image_discover():
    """Discover and download text-to-image models"""
    return redirect(url_for('ai_services.service_discover', service_type='text-to-image'))


@ai_services_bp.route('/<service_type>/models')
def service_models(service_type: str):
    """View and manage models for a specific AI service"""
    try:
        # Convert route name to enum value
        service_type_normalized = service_type.replace('-', '_')
        
        try:
            service_type_enum = AIServiceType(service_type_normalized)
        except ValueError:
            return render_template('ai_services/error.html', 
                                 error=f'Invalid service type: {service_type}'), 400
        
        service_info = _get_service_info(service_type_enum)
        
        # Get local models for this service
        from app.services.ai_service_model_manager import get_ai_service_model_manager
        model_mgr = get_ai_service_model_manager(service_type_normalized)
        
        try:
            local_models = model_mgr.get_local_models()
        except Exception as e:
            logger.error(f"Error getting local models: {e}")
            local_models = []
        
        try:
            storage_stats = model_mgr.get_storage_stats()
        except Exception as e:
            logger.error(f"Error getting storage stats: {e}")
            storage_stats = {'model_count': 0, 'total_size_gb': 0}
        
        return render_template('ai_services/models.html',
                              service_type=service_type,
                              service_type_normalized=service_type_normalized,
                              service_info=service_info,
                              models=local_models,
                              storage_stats=storage_stats)
    except Exception as e:
        logger.error(f"Error loading models page for {service_type}: {e}", exc_info=True)
        return render_template('ai_services/error.html', error=str(e)), 500


@ai_services_bp.route('/<service_type>/discover')
def service_discover(service_type: str):
    """Discover and download models for a specific AI service"""
    try:
        # Convert route name to enum value
        service_type_normalized = service_type.replace('-', '_')
        
        try:
            service_type_enum = AIServiceType(service_type_normalized)
        except ValueError:
            return render_template('ai_services/error.html', 
                                 error=f'Invalid service type: {service_type}'), 400
        
        service_info = _get_service_info(service_type_enum)
        
        # Get HuggingFace service for model discovery
        from app.services.huggingface_service import HuggingFaceService
        hf_service = HuggingFaceService()
        
        # Get popular categories for this service type
        if service_type_normalized == 'text_to_image':
            search_query = 'stable-diffusion'
            categories = [
                {'id': 'stable-diffusion', 'name': 'Stable Diffusion', 'query': 'stable-diffusion'},
                {'id': 'diffusion', 'name': 'Diffusion Models', 'query': 'diffusion'},
                {'id': 'image-generation', 'name': 'Image Generation', 'query': 'image-generation'}
            ]
        else:
            categories = []
            search_query = ''
        
        # Get query params
        query = request.args.get('q', search_query)
        category = request.args.get('category', '')
        
        models = []
        if query or category:
            search_query = query if query else (categories[0]['query'] if categories and category else '')
            for cat in categories:
                if cat['id'] == category:
                    search_query = cat['query']
                    break
            
            if search_query:
                try:
                    # Search for models (not just GGUF, but all model types)
                    models = hf_service.search_models(search_query, filter_gguf=False, limit=50)
                except Exception as e:
                    logger.error(f"Error searching models: {e}")
                    models = []
        
        return render_template('ai_services/discover.html',
                              service_type=service_type,
                              service_type_normalized=service_type_normalized,
                              service_info=service_info,
                              categories=categories,
                              models=models,
                              query=query,
                              selected_category=category)
    except Exception as e:
        logger.error(f"Error loading discover page for {service_type}: {e}", exc_info=True)
        return render_template('ai_services/error.html', error=str(e)), 500


# Generic API routes for all services
@ai_services_bp.route('/api/<service_type>/status', methods=['GET'])
def api_get_status(service_type: str):
    """Get service environment status"""
    try:
        # Convert route name (text-to-image or text_to_image) to enum value (text_to_image)
        service_type_normalized = service_type.replace('-', '_')
        
        logger.info(f"Getting status for service type: {service_type} (normalized: {service_type_normalized})")
        
        try:
            service_type_enum = AIServiceType(service_type_normalized)
        except ValueError as ve:
            logger.error(f"Invalid service type: {service_type} (normalized: {service_type_normalized}). Available: {[e.value for e in AIServiceType]}")
            return jsonify({
                'success': False, 
                'error': f'Invalid service type: {service_type}. Available: {", ".join([e.value for e in AIServiceType])}'
            }), 400
        
        env_mgr = get_ai_service_env(service_type_enum)
        env_status = env_mgr.get_status()
        
        logger.info(f"Status retrieved successfully for {service_type}: {env_status.get('status')}")
        
        return jsonify({
            'success': True,
            'environment': env_status
        })
    except ValueError as ve:
        logger.error(f"ValueError getting status for {service_type}: {ve}", exc_info=True)
        return jsonify({'success': False, 'error': f'Invalid service type: {service_type}'}), 400
    except Exception as e:
        logger.error(f"Error getting status for {service_type}: {e}", exc_info=True)
        import traceback
        logger.error(f"Traceback: {traceback.format_exc()}")
        return jsonify({'success': False, 'error': str(e)}), 500


@ai_services_bp.route('/api/<service_type>/create-env', methods=['POST'])
def api_create_env(service_type: str):
    """Create service environment"""
    try:
        # Convert route name (text-to-image) to enum value (text_to_image)
        service_type_normalized = service_type.replace('-', '_')
        
        try:
            service_type_enum = AIServiceType(service_type_normalized)
        except ValueError:
            service_type_enum = AIServiceType(service_type)
        env_mgr = get_ai_service_env(service_type_enum)
        task_mgr = TaskManager()
        
        service_info = _get_service_info(service_type_enum)
        service_name = service_info.get('name', service_type.replace('_', ' ').title())
        
        task = task_mgr.create_task(
            name=f"Create {service_name} Environment",
            task_type=f"ai_service_{service_type}_setup",
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
    except ValueError:
        return jsonify({'success': False, 'error': f'Invalid service type: {service_type}'}), 400
    except Exception as e:
        logger.error(f"Error creating environment for {service_type}: {e}", exc_info=True)
        return jsonify({'success': False, 'error': str(e)}), 500


@ai_services_bp.route('/api/<service_type>/install-packages', methods=['POST'])
def api_install_packages(service_type: str):
    """Install service packages"""
    try:
        # Convert route name (text-to-image) to enum value (text_to_image)
        service_type_normalized = service_type.replace('-', '_')
        
        try:
            service_type_enum = AIServiceType(service_type_normalized)
        except ValueError:
            service_type_enum = AIServiceType(service_type)
        env_mgr = get_ai_service_env(service_type_enum)
        task_mgr = TaskManager()
        
        data = request.get_json() or {}
        package_names = data.get('packages')
        
        # If packages is null or empty, install all required packages
        if package_names is None or (isinstance(package_names, list) and len(package_names) == 0):
            package_names = None  # Will trigger installation of required packages
        
        service_info = _get_service_info(service_type_enum)
        service_name = service_info.get('name', service_type.replace('_', ' ').title())
        
        # Determine package count for task description
        if package_names is None:
            packages = SERVICE_PACKAGES.get(service_type_enum, {})
            required_count = len([pkg for pkg in packages.values() if pkg.required])
            package_count = f"{required_count} required"
        else:
            package_count = len(package_names)
        
        task = task_mgr.create_task(
            name=f"Install {service_name} Packages",
            task_type=f"ai_service_{service_type}_install",
            steps=[f"Installing {package_count} packages"]
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
                    task_mgr.complete_task(task.id, {
                        'message': result.get('message', 'Installation completed'),
                        'installed': result.get('installed', []),
                        'failed': result.get('failed', []),
                        'package_status': result.get('package_status', {})
                    })
                else:
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
    except ValueError:
        return jsonify({'success': False, 'error': f'Invalid service type: {service_type}'}), 400
    except Exception as e:
        logger.error(f"Error installing packages for {service_type}: {e}", exc_info=True)
        return jsonify({'success': False, 'error': str(e)}), 500


# Text to Image API
@ai_services_bp.route('/api/text-to-image/generate', methods=['POST'])
def api_generate_image():
    """Generate image from text prompt (async with task tracking)"""
    try:
        from app.services.text_to_image_service import get_text_to_image_service
        from app.services.task_manager import TaskManager
        
        data = request.get_json() or {}
        prompt = data.get('prompt', '').strip()
        
        if not prompt:
            return jsonify({'success': False, 'error': 'Prompt is required'}), 400
        
        negative_prompt = data.get('negative_prompt', '').strip()
        image_size = data.get('image_size', '512x512')
        num_steps = data.get('num_steps', 50)
        guidance_scale = data.get('guidance_scale', 7.5)
        seed = data.get('seed')
        use_refiner = data.get('use_refiner', False)  # For SDXL base+refiner ensemble
        use_cpu_offload = data.get('use_cpu_offload', False)  # For low VRAM systems
        max_sequence_length = data.get('max_sequence_length', 512)  # For UltraFlux models
        true_cfg_scale = data.get('true_cfg_scale')  # For Qwen-Image models
        
        # Parse image size
        try:
            width, height = map(int, image_size.split('x'))
        except:
            width, height = 512, 512
        
        model_id = data.get('model_id')
        
        # Save model selection if provided
        if model_id:
            from app.services.ai_service_models import AIServiceModelConfig
            AIServiceModelConfig.set_selected_model('text_to_image', model_id)
        
        # Create task for async generation
        task_mgr = TaskManager()
        task = task_mgr.create_task(
            name=f"Generate Image: {prompt[:50]}...",
            task_type="text_to_image_generation",
            steps=[
                "Initializing model",
                "Generating image",
                "Processing result"
            ]
        )
        
        def run_generation():
            try:
                task_mgr.start_task(task.id)
                task_mgr.update_step(task.id, 0, "running", "Initializing model and loading weights...")
                task_mgr.update_progress(task.id, 10, "Initializing model...")
                
                service = get_text_to_image_service()
                
                task_mgr.update_step(task.id, 1, "running", f"Generating image with {num_steps} steps...")
                task_mgr.update_progress(task.id, 30, "Generating image...")
                
                result = service.generate_image(
                    prompt=prompt,
                    negative_prompt=negative_prompt,
                    width=width,
                    height=height,
                    num_inference_steps=num_steps,
                    guidance_scale=guidance_scale,
                    seed=seed,
                    model_id=model_id,
                    use_refiner=use_refiner,
                    use_cpu_offload=use_cpu_offload,
                    max_sequence_length=max_sequence_length,
                    true_cfg_scale=true_cfg_scale
                )
                
                if result.get('success'):
                    task_mgr.update_step(task.id, 2, "completed", "Image generated successfully")
                    task_mgr.update_progress(task.id, 100, "Complete")
                    task_mgr.complete_task(task.id, result)
                else:
                    error_msg = result.get('error', 'Unknown error')
                    task_mgr.fail_task(task.id, error_msg)
                    
            except Exception as e:
                import traceback
                error_details = traceback.format_exc()
                logger.error(f"Image generation error: {error_details}")
                task_mgr.fail_task(task.id, str(e))
        
        import threading
        thread = threading.Thread(target=run_generation, daemon=True)
        thread.start()
        
        return jsonify({
            'success': True,
            'task_id': task.id,
            'message': 'Image generation started'
        })
            
    except Exception as e:
        logger.error(f"Error starting image generation: {e}", exc_info=True)
        return jsonify({'success': False, 'error': str(e)}), 500


# Text to Speech API
@ai_services_bp.route('/api/text-to-speech/generate', methods=['POST'])
def api_generate_speech():
    """Generate speech from text"""
    try:
        from app.services.text_to_speech_service import get_text_to_speech_service
        
        data = request.get_json() or {}
        text = data.get('text', '').strip()
        
        if not text:
            return jsonify({'success': False, 'error': 'Text is required'}), 400
        
        voice = data.get('voice', 'default')
        speed = float(data.get('speed', 1.0))
        engine = data.get('engine', 'edge-tts')
        
        service = get_text_to_speech_service()
        result = service.generate_speech(
            text=text,
            voice=voice,
            speed=speed,
            engine=engine
        )
        
        if result.get('success'):
            return jsonify(result)
        else:
            return jsonify(result), 500
            
    except Exception as e:
        logger.error(f"Error in speech generation: {e}", exc_info=True)
        return jsonify({'success': False, 'error': str(e)}), 500


# Speech to Text API
@ai_services_bp.route('/api/speech-to-text/transcribe', methods=['POST'])
def api_transcribe_audio():
    """Transcribe audio to text"""
    try:
        from app.services.speech_to_text_service import get_speech_to_text_service
        
        if 'audio' not in request.files:
            return jsonify({'success': False, 'error': 'Audio file is required'}), 400
        
        audio_file = request.files['audio']
        if not audio_file or not audio_file.filename:
            return jsonify({'success': False, 'error': 'Invalid audio file'}), 400
        
        language = request.form.get('language', 'en')
        model_size = request.form.get('model_size', 'base')
        
        audio_content = audio_file.read()
        if not audio_content:
            return jsonify({'success': False, 'error': 'Audio file is empty'}), 400
        
        service = get_speech_to_text_service()
        result = service.transcribe_audio(
            audio_content=audio_content,
            language=language if language != 'auto' else 'auto',
            model_size=model_size
        )
        
        if result.get('success'):
            return jsonify(result)
        else:
            return jsonify(result), 500
            
    except Exception as e:
        logger.error(f"Error in audio transcription: {e}", exc_info=True)
        return jsonify({'success': False, 'error': str(e)}), 500


# Tabular & Time Series API
@ai_services_bp.route('/api/tabular-time-series/forecast', methods=['POST'])
def api_forecast():
    """Perform time series forecasting or tabular prediction"""
    try:
        from app.services.tabular_time_series_service import get_tabular_time_series_service
        from app.services.task_manager import TaskManager
        
        data = request.get_json() or {}
        
        # Get data (can be CSV, JSON, or array)
        time_series_data = data.get('data')  # List of time series
        model_id = data.get('model_id')
        forecast_horizon = int(data.get('forecast_horizon', 12))
        context_length = data.get('context_length')
        frequency = data.get('frequency')  # H, D, M, etc.
        task_type = data.get('task_type', 'time_series_forecasting')
        
        if not time_series_data:
            return jsonify({'success': False, 'error': 'Time series data is required'}), 400
        
        # Save model selection if provided
        if model_id:
            from app.services.ai_service_models import AIServiceModelConfig
            AIServiceModelConfig.set_selected_model('tabular_time_series', model_id)
        
        # Create task for async forecasting
        task_mgr = TaskManager()
        task = task_mgr.create_task(
            name="Time Series Forecast",
            task_type="tabular_time_series_forecast",
            steps=[
                "Loading model",
                "Processing data",
                "Generating forecast"
            ]
        )
        
        def run_forecast():
            try:
                task_mgr.start_task(task.id)
                task_mgr.update_step(task.id, 0, "running", "Loading model...")
                task_mgr.update_progress(task.id, 10, "Loading model...")
                
                service = get_tabular_time_series_service()
                
                task_mgr.update_step(task.id, 1, "running", "Processing time series data...")
                task_mgr.update_progress(task.id, 30, "Processing data...")
                
                result = service.forecast(
                    data=time_series_data,
                    model_id=model_id,
                    forecast_horizon=forecast_horizon,
                    context_length=context_length,
                    frequency=frequency,
                    task_type=task_type
                )
                
                if result.get('success'):
                    num_series = len(result.get('forecasts', []))
                    task_mgr.update_step(task.id, 2, "completed", f"Forecasted {num_series} time series")
                    task_mgr.update_progress(task.id, 100, "Complete")
                    task_mgr.complete_task(task.id, result)
                else:
                    error_msg = result.get('error', 'Unknown error')
                    task_mgr.fail_task(task.id, error_msg)
                    
            except Exception as e:
                import traceback
                error_details = traceback.format_exc()
                logger.error(f"Forecast error: {error_details}")
                task_mgr.fail_task(task.id, str(e))
        
        import threading
        thread = threading.Thread(target=run_forecast, daemon=True)
        thread.start()
        
        return jsonify({
            'success': True,
            'task_id': task.id,
            'message': 'Forecast started'
        })
            
    except Exception as e:
        logger.error(f"Error starting forecast: {e}", exc_info=True)
        return jsonify({'success': False, 'error': str(e)}), 500


# Object Detection API
@ai_services_bp.route('/api/object-detection/detect', methods=['POST'])
def api_detect_objects():
    """Detect objects in an image"""
    try:
        from app.services.object_detection_service import get_object_detection_service
        from app.services.task_manager import TaskManager
        
        if 'image' not in request.files:
            return jsonify({'success': False, 'error': 'Image file is required'}), 400
        
        image_file = request.files['image']
        if not image_file or not image_file.filename:
            return jsonify({'success': False, 'error': 'Invalid image file'}), 400
        
        image_content = image_file.read()
        if not image_content:
            return jsonify({'success': False, 'error': 'Image file is empty'}), 400
        
        data = request.form
        model_id = data.get('model_id')
        confidence_threshold = float(data.get('confidence_threshold', 0.5))
        max_detections = int(data.get('max_detections', 100))
        
        # Save model selection if provided
        if model_id:
            from app.services.ai_service_models import AIServiceModelConfig
            AIServiceModelConfig.set_selected_model('object_detection', model_id)
        
        # Create task for async detection
        task_mgr = TaskManager()
        task = task_mgr.create_task(
            name="Object Detection",
            task_type="object_detection",
            steps=[
                "Loading model",
                "Detecting objects",
                "Processing results"
            ]
        )
        
        def run_detection():
            try:
                task_mgr.start_task(task.id)
                task_mgr.update_step(task.id, 0, "running", "Loading model...")
                task_mgr.update_progress(task.id, 10, "Loading model...")
                
                service = get_object_detection_service()
                
                task_mgr.update_step(task.id, 1, "running", "Detecting objects in image...")
                task_mgr.update_progress(task.id, 50, "Detecting objects...")
                
                result = service.detect_objects(
                    image_data=image_content,
                    model_id=model_id,
                    confidence_threshold=confidence_threshold,
                    max_detections=max_detections
                )
                
                if result.get('success'):
                    task_mgr.update_step(task.id, 2, "completed", f"Found {len(result.get('detections', []))} objects")
                    task_mgr.update_progress(task.id, 100, "Complete")
                    task_mgr.complete_task(task.id, result)
                else:
                    error_msg = result.get('error', 'Unknown error')
                    task_mgr.fail_task(task.id, error_msg)
                    
            except Exception as e:
                import traceback
                error_details = traceback.format_exc()
                logger.error(f"Object detection error: {error_details}")
                task_mgr.fail_task(task.id, str(e))
        
        import threading
        thread = threading.Thread(target=run_detection, daemon=True)
        thread.start()
        
        return jsonify({
            'success': True,
            'task_id': task.id,
            'message': 'Object detection started'
        })
            
    except Exception as e:
        logger.error(f"Error starting object detection: {e}", exc_info=True)
        return jsonify({'success': False, 'error': str(e)}), 500


# Voice to Voice API
@ai_services_bp.route('/api/voice-to-voice/convert', methods=['POST'])
def api_convert_voice():
    """Convert voice from source audio"""
    try:
        from app.services.voice_to_voice_service import get_voice_to_voice_service
        
        if 'source_audio' not in request.files:
            return jsonify({'success': False, 'error': 'Source audio file is required'}), 400
        
        source_file = request.files['source_audio']
        if not source_file or not source_file.filename:
            return jsonify({'success': False, 'error': 'Invalid source audio file'}), 400
        
        target_voice_type = request.form.get('target_voice_type', 'preset')
        preset_voice = request.form.get('preset_voice', 'male1')
        
        source_audio = source_file.read()
        if not source_audio:
            return jsonify({'success': False, 'error': 'Source audio file is empty'}), 400
        
        voice_sample = None
        if target_voice_type == 'clone' and 'voice_sample' in request.files:
            sample_file = request.files['voice_sample']
            if sample_file and sample_file.filename:
                voice_sample = sample_file.read()
        
        service = get_voice_to_voice_service()
        result = service.convert_voice(
            source_audio=source_audio,
            target_voice_type=target_voice_type,
            voice_sample=voice_sample,
            preset_voice=preset_voice
        )
        
        if result.get('success'):
            return jsonify(result)
        else:
            return jsonify(result), 500
            
    except Exception as e:
        logger.error(f"Error in voice conversion: {e}", exc_info=True)
        return jsonify({'success': False, 'error': str(e)}), 500


# Model Management API
@ai_services_bp.route('/api/<service_type>/models', methods=['GET'])
def api_get_models(service_type: str):
    """Get available models for a service - only returns locally downloaded models"""
    try:
        from app.services.ai_service_models import AIServiceModelConfig
        from app.services.ai_service_model_manager import get_ai_service_model_manager
        from app.services.llm_manager import LLMManager
        
        # Convert route name to enum value
        service_type_normalized = service_type.replace('-', '_')
        
        try:
            service_type_enum = AIServiceType(service_type_normalized)
        except ValueError:
            return jsonify({'success': False, 'error': f'Invalid service type: {service_type}'}), 400
        
        # Get selected model
        selected_model = AIServiceModelConfig.get_selected_model(service_type_normalized)
        
        models = []
        
        # ONLY get models from AIServiceModelManager - this is the single source of truth for AI service models
        # Models downloaded through unified system also use AIServiceModelManager
        try:
            model_mgr = get_ai_service_model_manager(service_type_normalized)
            local_models = model_mgr.get_local_models()
            
            # Verify each model actually exists on disk before including it
            for local_model in local_models:
                # Check if model path exists
                model_path = Path(local_model.path)
                if not model_path.exists():
                    logger.warning(f"Model path does not exist: {local_model.path}, skipping")
                    continue
                
                # Verify it's actually a downloaded model (has files)
                if model_path.is_dir():
                    # Check if directory has model files
                    model_files = list(model_path.rglob('*.safetensors')) + \
                                 list(model_path.rglob('*.bin')) + \
                                 list(model_path.rglob('*.ckpt')) + \
                                 list(model_path.rglob('*.pt')) + \
                                 list(model_path.rglob('*.pth'))
                    if not model_files:
                        logger.warning(f"Model directory has no model files: {local_model.path}, skipping")
                        continue
                elif not model_path.is_file():
                    logger.warning(f"Model path is neither file nor directory: {local_model.path}, skipping")
                    continue
                
                # Calculate actual size
                if model_path.is_dir():
                    actual_size = sum(f.stat().st_size for f in model_path.rglob('*') if f.is_file())
                else:
                    actual_size = model_path.stat().st_size
                
                size_gb = actual_size / (1024 ** 3) if actual_size > 0 else 0
                
                model_dict = {
                    'id': local_model.model_id,  # HuggingFace model ID
                    'name': local_model.name or local_model.model_id,
                    'description': f'Downloaded model ({size_gb:.2f} GB)' if size_gb > 0 else 'Downloaded model',
                    'default': False,
                    'selected': (local_model.model_id == selected_model),
                    'local': True,
                    'size_gb': size_gb
                }
                models.append(model_dict)
        except Exception as e:
            logger.error(f"Error getting models from AIServiceModelManager for {service_type}: {e}", exc_info=True)
        
        # If no models downloaded, return empty list (don't show hardcoded models)
        # The UI should prompt user to download models
        
        return jsonify({
            'success': True,
            'models': models
        })
    except Exception as e:
        logger.error(f"Error getting models for {service_type}: {e}", exc_info=True)
        return jsonify({'success': False, 'error': str(e)}), 500


@ai_services_bp.route('/api/<service_type>/models', methods=['POST'])
def api_set_model(service_type: str):
    """Set selected model for a service"""
    try:
        from app.services.ai_service_models import AIServiceModelConfig
        
        # Convert route name to enum value
        service_type_normalized = service_type.replace('-', '_')
        
        try:
            service_type_enum = AIServiceType(service_type_normalized)
        except ValueError:
            return jsonify({'success': False, 'error': f'Invalid service type: {service_type}'}), 400
        
        data = request.get_json() or {}
        model_id = data.get('model_id')
        
        if not model_id:
            return jsonify({'success': False, 'error': 'model_id is required'}), 400
        
        success = AIServiceModelConfig.set_selected_model(service_type_normalized, model_id)
        
        if success:
            return jsonify({'success': True, 'message': 'Model selected successfully'})
        else:
            return jsonify({'success': False, 'error': 'Failed to set model'}), 400
            
    except Exception as e:
        logger.error(f"Error setting model for {service_type}: {e}", exc_info=True)
        return jsonify({'success': False, 'error': str(e)}), 500


@ai_services_bp.route('/api/<service_type>/models/download', methods=['POST'])
def api_download_model(service_type: str):
    """Download a model for a service"""
    try:
        from app.services.task_manager import TaskManager
        from app.services.ai_service_model_manager import get_ai_service_model_manager
        
        # Convert route name to enum value
        service_type_normalized = service_type.replace('-', '_')
        
        try:
            service_type_enum = AIServiceType(service_type_normalized)
        except ValueError:
            return jsonify({'success': False, 'error': f'Invalid service type: {service_type}'}), 400
        
        data = request.get_json() or {}
        model_id = data.get('model_id')
        
        if not model_id:
            return jsonify({'success': False, 'error': 'model_id is required'}), 400
        
        service_info = _get_service_info(service_type_enum)
        service_name = service_info.get('name', service_type.replace('_', ' ').title())
        
        # Get model manager
        model_mgr = get_ai_service_model_manager(service_type_normalized)
        
        # Check if model already exists
        existing_model = model_mgr.get_model_by_id(model_id)
        if existing_model:
            return jsonify({
                'success': True,
                'message': f'Model {model_id} already exists locally',
                'model': existing_model.to_dict()
            })
        
        # Create task for progress tracking
        task_mgr = TaskManager()
        task = task_mgr.create_task(
            name=f"Download {service_name} Model: {model_id}",
            task_type=f"ai_service_{service_type}_model_download",
            steps=[
                "Verifying model",
                "Downloading model files",
                "Model ready"
            ]
        )
        
        def run_download():
            try:
                task_mgr.start_task(task.id)
                task_mgr.update_step(task.id, 0, "running", f"Verifying model {model_id}...")
                task_mgr.update_progress(task.id, 10, f"Verifying model {model_id}...")
                
                # Download model (download_model handles task updates internally)
                task_mgr.update_step(task.id, 1, "running", f"Downloading {model_id}...")
                task_mgr.update_progress(task.id, 20, f"Downloading {model_id}...")
                
                # download_model runs in a thread and updates task via task_manager parameter
                model = model_mgr.download_model(
                    model_id=model_id,
                    progress_callback=None,  # We use task_manager instead
                    task_manager=task_mgr,
                    task_id=task.id
                )
                
                # Note: download_model runs async, task status will be updated by the download thread
                # If model is None, it means download started in background
                # The task will be completed/failed by the download thread
                
            except Exception as e:
                import traceback
                error_details = traceback.format_exc()
                logger.error(f"Download error: {error_details}")
                task_mgr.fail_task(task.id, str(e))
        
        import threading
        thread = threading.Thread(target=run_download, daemon=True)
        thread.start()
        
        return jsonify({
            'success': True,
            'task_id': task.id,
            'message': 'Model download started'
        })
        
    except Exception as e:
        logger.error(f"Error downloading model for {service_type}: {e}", exc_info=True)
        return jsonify({'success': False, 'error': str(e)}), 500


@ai_services_bp.route('/api/<service_type>/models/select', methods=['POST'])
def api_select_model(service_type: str):
    """Select a model for use"""
    try:
        from app.services.ai_service_models import AIServiceModelConfig
        from app.services.ai_service_model_manager import get_ai_service_model_manager
        
        # Convert route name to enum value
        service_type_normalized = service_type.replace('-', '_')
        
        try:
            service_type_enum = AIServiceType(service_type_normalized)
        except ValueError:
            return jsonify({'success': False, 'error': f'Invalid service type: {service_type}'}), 400
        
        data = request.get_json() or {}
        model_id = data.get('model_id')  # This could be local model ID or HuggingFace model ID
        
        if not model_id:
            return jsonify({'success': False, 'error': 'model_id is required'}), 400
        
        # Check if it's a local model ID (short hash) or HuggingFace model ID
        model_mgr = get_ai_service_model_manager(service_type_normalized)
        local_model = model_mgr.get_model_by_id(model_id)
        
        if local_model:
            # Use the HuggingFace model ID from local model
            hf_model_id = local_model.model_id
        else:
            # Assume it's a HuggingFace model ID
            hf_model_id = model_id
        
        success = AIServiceModelConfig.set_selected_model(service_type_normalized, hf_model_id)
        
        if success:
            return jsonify({'success': True, 'message': 'Model selected successfully'})
        else:
            return jsonify({'success': False, 'error': 'Failed to select model'}), 400
            
    except Exception as e:
        logger.error(f"Error selecting model for {service_type}: {e}", exc_info=True)
        return jsonify({'success': False, 'error': str(e)}), 500


@ai_services_bp.route('/api/<service_type>/models/<model_id>', methods=['DELETE'])
def api_delete_model(service_type: str, model_id: str):
    """Delete a local model"""
    try:
        from app.services.ai_service_model_manager import get_ai_service_model_manager
        
        # Convert route name to enum value
        service_type_normalized = service_type.replace('-', '_')
        
        try:
            service_type_enum = AIServiceType(service_type_normalized)
        except ValueError:
            return jsonify({'success': False, 'error': f'Invalid service type: {service_type}'}), 400
        
        model_mgr = get_ai_service_model_manager(service_type_normalized)
        success = model_mgr.delete_model(model_id)
        
        if success:
            return jsonify({'success': True, 'message': 'Model deleted successfully'})
        else:
            return jsonify({'success': False, 'error': 'Model not found or could not be deleted'}), 404
            
    except Exception as e:
        logger.error(f"Error deleting model for {service_type}: {e}", exc_info=True)
        return jsonify({'success': False, 'error': str(e)}), 500


@ai_services_bp.route('/api/<service_type>/curated-models', methods=['GET'])
def api_get_curated_models(service_type: str):
    """Get curated/recommended models for a service type"""
    try:
        from app.services.ai_service_curated_models import AIServiceCuratedModels
        
        # Convert route name to service type
        service_type_normalized = service_type.replace('-', '_')
        
        curated_models = AIServiceCuratedModels.get_curated_models_dict(service_type_normalized)
        
        return jsonify({
            'success': True,
            'models': curated_models
        })
    except Exception as e:
        logger.error(f"Error getting curated models for {service_type}: {e}", exc_info=True)
        return jsonify({'success': False, 'error': str(e)}), 500


@ai_services_bp.route('/api/<service_type>/loaded', methods=['GET'])
def api_get_loaded_model(service_type: str):
    """Get currently loaded model for a service type"""
    try:
        from app.services.ai_service_loaded_models import get_ai_service_loaded_tracker
        
        # Convert route name to enum value
        service_type_normalized = service_type.replace('-', '_')
        
        tracker = get_ai_service_loaded_tracker()
        loaded_model = tracker.get_loaded_model(service_type_normalized)
        
        if loaded_model:
            return jsonify({
                'success': True,
                'model': loaded_model.to_dict()
            })
        else:
            return jsonify({
                'success': True,
                'model': None
            })
    except Exception as e:
        logger.error(f"Error getting loaded model for {service_type}: {e}", exc_info=True)
        return jsonify({'success': False, 'error': str(e)}), 500


@ai_services_bp.route('/api/loaded', methods=['GET'])
def api_get_all_loaded_models():
    """Get all currently loaded models across all AI services"""
    try:
        from app.services.ai_service_loaded_models import get_ai_service_loaded_tracker
        
        tracker = get_ai_service_loaded_tracker()
        all_loaded = tracker.get_all_loaded_models()
        
        # Convert to dict format
        loaded_dict = {
            service_type: model.to_dict()
            for service_type, model in all_loaded.items()
        }
        
        return jsonify({
            'success': True,
            'loaded_models': loaded_dict
        })
    except Exception as e:
        logger.error(f"Error getting all loaded models: {e}", exc_info=True)
        return jsonify({'success': False, 'error': str(e)}), 500
