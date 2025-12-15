"""
Job Scheduler Routes

Unified job scheduler for LLM, RAG, Document Extraction, and other modules.
"""
from flask import Blueprint, render_template, request, jsonify
from app.database import db
from app.models.scheduled_jobs import ScheduledJob, JobExecution, JobModule, JobType, ScheduleType, JobStatus
from app.services.job_scheduler import get_job_scheduler
from datetime import datetime
import json
import logging

logger = logging.getLogger(__name__)

job_scheduler_bp = Blueprint('job_scheduler', __name__)


@job_scheduler_bp.route('/')
def index():
    """Job scheduler dashboard"""
    from app.services.job_scheduler_environment import get_scheduler_env
    
    scheduler = get_job_scheduler()
    scheduler_status = scheduler.get_scheduler_status()
    env_mgr = get_scheduler_env()
    env_status = env_mgr.get_status()
    
    return render_template('job_scheduler/index.html',
                          scheduler_status=scheduler_status,
                          env_status=env_status)


@job_scheduler_bp.route('/api/jobs', methods=['GET'])
def api_list_jobs():
    """List all scheduled jobs"""
    try:
        jobs = ScheduledJob.query.order_by(ScheduledJob.created_at.desc()).all()
        
        scheduler = get_job_scheduler()
        jobs_data = []
        for job in jobs:
            job_dict = job.to_dict()
            # Get scheduler status
            scheduler_status = scheduler.get_job_status(job.id)
            job_dict['scheduler_status'] = scheduler_status
            jobs_data.append(job_dict)
        
        return jsonify({
            'success': True,
            'jobs': jobs_data
        })
    except Exception as e:
        return jsonify({'success': False, 'error': str(e)}), 500


@job_scheduler_bp.route('/api/jobs', methods=['POST'])
def api_create_job():
    """Create a new scheduled job"""
    try:
        data = request.get_json()
        if not data:
            return jsonify({'success': False, 'error': 'JSON body required'}), 400
        
        # Validate required fields
        if not data.get('name'):
            return jsonify({'success': False, 'error': 'name is required'}), 400
        if not data.get('module'):
            return jsonify({'success': False, 'error': 'module is required'}), 400
        if not data.get('job_type'):
            return jsonify({'success': False, 'error': 'job_type is required'}), 400
        
        # Create job
        job = ScheduledJob(
            name=data['name'],
            description=data.get('description', ''),
            module=data['module'],
            job_type=data['job_type'],
            function_name=data.get('function_name'),
            api_endpoint=data.get('api_endpoint'),
            script_path=data.get('script_path'),
            parameters=data.get('parameters', {}),
            schedule_type=data.get('schedule_type', 'manual'),
            schedule_config=data.get('schedule_config', {}),
            is_active=data.get('is_active', True),
            retry_enabled=data.get('retry_enabled', False),
            max_retries=data.get('max_retries', 3),
            retry_delay_seconds=data.get('retry_delay_seconds', 60),
            retry_backoff=data.get('retry_backoff', True),
            failover_enabled=data.get('failover_enabled', False)
        )
        
        db.session.add(job)
        db.session.commit()
        
        # Schedule if active and not manual
        if job.is_active and job.schedule_type != 'manual':
            scheduler = get_job_scheduler()
            scheduler.schedule_job(job)
        
        return jsonify({
            'success': True,
            'job': job.to_dict()
        }), 201
        
    except Exception as e:
        db.session.rollback()
        return jsonify({'success': False, 'error': str(e)}), 500


@job_scheduler_bp.route('/api/jobs/<int:job_id>', methods=['GET'])
def api_get_job(job_id):
    """Get a specific job"""
    job = ScheduledJob.query.get(job_id)
    if not job:
        return jsonify({'success': False, 'error': 'Job not found'}), 404
    
    scheduler = get_job_scheduler()
    job_dict = job.to_dict()
    job_dict['scheduler_status'] = scheduler.get_job_status(job_id)
    
    return jsonify({
        'success': True,
        'job': job_dict
    })


@job_scheduler_bp.route('/api/jobs/<int:job_id>', methods=['PUT'])
def api_update_job(job_id):
    """Update a scheduled job"""
    job = ScheduledJob.query.get(job_id)
    if not job:
        return jsonify({'success': False, 'error': 'Job not found'}), 404
    
    try:
        data = request.get_json()
        if not data:
            return jsonify({'success': False, 'error': 'JSON body required'}), 400
        
        # Update fields
        if 'name' in data:
            job.name = data['name']
        if 'description' in data:
            job.description = data.get('description')
        if 'function_name' in data:
            job.function_name = data.get('function_name')
        if 'api_endpoint' in data:
            job.api_endpoint = data.get('api_endpoint')
        if 'script_path' in data:
            job.script_path = data.get('script_path')
        if 'parameters' in data:
            job.parameters = data.get('parameters', {})
        if 'schedule_type' in data:
            job.schedule_type = data['schedule_type']
        if 'schedule_config' in data:
            job.schedule_config = data.get('schedule_config', {})
        if 'is_active' in data:
            job.is_active = data['is_active']
        if 'retry_enabled' in data:
            job.retry_enabled = data['retry_enabled']
        if 'max_retries' in data:
            job.max_retries = data['max_retries']
        if 'retry_delay_seconds' in data:
            job.retry_delay_seconds = data['retry_delay_seconds']
        if 'retry_backoff' in data:
            job.retry_backoff = data['retry_backoff']
        if 'failover_enabled' in data:
            job.failover_enabled = data['failover_enabled']
        
        job.updated_at = datetime.utcnow()
        db.session.commit()
        
        # Reschedule if needed
        scheduler = get_job_scheduler()
        scheduler.unschedule_job(job_id)
        if job.is_active and job.schedule_type != 'manual':
            scheduler.schedule_job(job)
        
        return jsonify({
            'success': True,
            'job': job.to_dict()
        })
        
    except Exception as e:
        db.session.rollback()
        return jsonify({'success': False, 'error': str(e)}), 500


@job_scheduler_bp.route('/api/jobs/<int:job_id>', methods=['DELETE'])
def api_delete_job(job_id):
    """Delete a scheduled job"""
    job = ScheduledJob.query.get(job_id)
    if not job:
        return jsonify({'success': False, 'error': 'Job not found'}), 404
    
    try:
        scheduler = get_job_scheduler()
        scheduler.unschedule_job(job_id)
        
        db.session.delete(job)
        db.session.commit()
        
        return jsonify({
            'success': True,
            'message': 'Job deleted'
        })
    except Exception as e:
        db.session.rollback()
        return jsonify({'success': False, 'error': str(e)}), 500


@job_scheduler_bp.route('/api/jobs/<int:job_id>/run', methods=['POST'])
def api_run_job(job_id):
    """Manually trigger a job to run now"""
    job = ScheduledJob.query.get(job_id)
    if not job:
        return jsonify({'success': False, 'error': 'Job not found'}), 404
    
    try:
        scheduler = get_job_scheduler()
        result = scheduler.run_job_now(job_id)
        
        if result.get('success'):
            return jsonify(result)
        else:
            return jsonify(result), 400
    except Exception as e:
        logger.error(f"Error running job {job_id}: {e}", exc_info=True)
        return jsonify({'success': False, 'error': str(e)}), 500


@job_scheduler_bp.route('/api/jobs/<int:job_id>/stop', methods=['POST'])
def api_stop_job(job_id):
    """Stop a running job"""
    job = ScheduledJob.query.get(job_id)
    if not job:
        return jsonify({'success': False, 'error': 'Job not found'}), 404
    
    try:
        scheduler = get_job_scheduler()
        result = scheduler.stop_job(job_id)
        
        if result.get('success'):
            return jsonify(result)
        else:
            return jsonify(result), 400
    except Exception as e:
        logger.error(f"Error stopping job {job_id}: {e}", exc_info=True)
        return jsonify({'success': False, 'error': str(e)}), 500


@job_scheduler_bp.route('/api/jobs/<int:job_id>/status', methods=['GET'])
def api_get_job_execution_status(job_id):
    """Get current execution status of a job"""
    execution_id = request.args.get('execution_id', type=int)
    
    try:
        scheduler = get_job_scheduler()
        result = scheduler.get_job_execution_status(job_id, execution_id)
        
        if result.get('success'):
            return jsonify(result)
        else:
            return jsonify(result), 404
    except Exception as e:
        logger.error(f"Error getting job status {job_id}: {e}", exc_info=True)
        return jsonify({'success': False, 'error': str(e)}), 500


@job_scheduler_bp.route('/api/jobs/<int:job_id>/executions', methods=['GET'])
def api_get_job_executions(job_id):
    """Get execution history for a job"""
    job = ScheduledJob.query.get(job_id)
    if not job:
        return jsonify({'success': False, 'error': 'Job not found'}), 404
    
    try:
        limit = request.args.get('limit', 50, type=int)
        executions = JobExecution.query.filter_by(job_id=job_id)\
            .order_by(JobExecution.started_at.desc())\
            .limit(limit)\
            .all()
        
        return jsonify({
            'success': True,
            'executions': [ex.to_dict() for ex in executions]
        })
    except Exception as e:
        return jsonify({'success': False, 'error': str(e)}), 500


@job_scheduler_bp.route('/api/modules', methods=['GET'])
def api_get_modules():
    """Get available modules and their job types"""
    return jsonify({
        'success': True,
        'modules': {
            'llm': {
                'name': 'LLM Management',
                'job_types': [
                    {'value': 'llm_inference', 'label': 'LLM Inference'},
                    {'value': 'llm_batch_process', 'label': 'Batch Processing'},
                    {'value': 'llm_model_update', 'label': 'Model Update'}
                ]
            },
            'rag': {
                'name': 'RAG',
                'job_types': [
                    {'value': 'rag_sync', 'label': 'RAG Sync'},
                    {'value': 'rag_index', 'label': 'Index Documents'},
                    {'value': 'rag_cleanup', 'label': 'Cleanup'}
                ]
            },
            'document_extraction': {
                'name': 'Document Extraction',
                'job_types': [
                    {'value': 'doc_extract_batch', 'label': 'Batch Extraction'},
                    {'value': 'doc_extract_folder', 'label': 'Extract Folder'}
                ]
            },
            'ml_models': {
                'name': 'ML Models',
                'job_types': [
                    {'value': 'ml_model_train', 'label': 'Train Model'},
                    {'value': 'ml_model_validate', 'label': 'Validate Model'}
                ]
            },
            'system': {
                'name': 'System',
                'job_types': [
                    {'value': 'system_backup', 'label': 'Backup'},
                    {'value': 'system_cleanup', 'label': 'Cleanup'}
                ]
            },
            'custom': {
                'name': 'Custom',
                'job_types': [
                    {'value': 'custom_script', 'label': 'Custom Script'},
                    {'value': 'custom_api', 'label': 'Custom API'}
                ]
            }
        }
    })


@job_scheduler_bp.route('/api/status', methods=['GET'])
def api_get_scheduler_status():
    """Get scheduler status"""
    from app.services.job_scheduler_environment import get_scheduler_env
    
    scheduler = get_job_scheduler()
    status = scheduler.get_scheduler_status()
    env_mgr = get_scheduler_env()
    env_status = env_mgr.get_status()
    
    return jsonify({
        'success': True,
        'status': status,
        'environment': env_status
    })


@job_scheduler_bp.route('/api/create-env', methods=['POST'])
def api_create_env():
    """Create scheduler environment"""
    from app.services.job_scheduler_environment import get_scheduler_env
    from app.services.task_manager import TaskManager
    
    env_mgr = get_scheduler_env()
    task_mgr = TaskManager()
    
    task = task_mgr.create_task(
        name="Create Job Scheduler Environment",
        task_type="scheduler_setup",
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


@job_scheduler_bp.route('/api/install-packages', methods=['POST'])
def api_install_packages():
    """Install scheduler packages"""
    from app.services.job_scheduler_environment import get_scheduler_env
    from app.services.task_manager import TaskManager
    
    data = request.get_json() or {}
    package_names = data.get('packages')  # Optional list, defaults to required packages
    
    env_mgr = get_scheduler_env()
    task_mgr = TaskManager()
    
    task = task_mgr.create_task(
        name="Install Scheduler Packages",
        task_type="scheduler_install",
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
                # Re-initialize scheduler if APScheduler was installed
                if 'apscheduler' in result.get('installed', []):
                    from app.services.job_scheduler import get_job_scheduler
                    scheduler = get_job_scheduler()
                    scheduler.reinitialize()
                
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
