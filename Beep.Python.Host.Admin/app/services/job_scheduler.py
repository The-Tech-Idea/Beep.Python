"""
Unified Job Scheduler Service

Manages scheduled jobs for all modules (LLM, RAG, Document Extraction, etc.)
Uses APScheduler for cross-platform scheduling.
"""
import os
import sys
import json
import threading
import subprocess
import importlib
from datetime import datetime, timedelta
from typing import Dict, List, Any, Optional, Callable
from pathlib import Path
import logging

logger = logging.getLogger(__name__)


class JobScheduler:
    """
    Unified job scheduler for all modules.
    Can schedule functions for LLM, RAG, Document Extraction, and other modules.
    """
    
    _instance = None
    _lock = threading.Lock()
    
    def __new__(cls):
        if cls._instance is None:
            with cls._lock:
                if cls._instance is None:
                    cls._instance = super().__new__(cls)
                    cls._instance._initialized = False
        return cls._instance
    
    def __init__(self):
        if self._initialized:
            return
        
        self._initialized = True
        self._scheduler = None
        self._jobs = {}  # job_id -> scheduler_job_id
        self._running_jobs = {}  # job_id -> execution_thread
        self._job_locks = {}  # job_id -> threading.Lock
        self._apscheduler_available = False
        self._scheduler_python = None  # Python executable from dedicated env
        self._app = None
        
        # Try to initialize APScheduler
        self._init_scheduler()
    
    def _init_scheduler(self):
        """Initialize APScheduler from dedicated environment"""
        try:
            # Try to use dedicated environment first
            from app.services.job_scheduler_environment import get_scheduler_env
            env_mgr = get_scheduler_env()
            python_exe = env_mgr.get_python_executable()
            
            if python_exe and python_exe.exists():
                # Check if APScheduler is installed in dedicated env
                result = subprocess.run(
                    [str(python_exe), '-c', 'import apscheduler; print("ok")'],
                    capture_output=True,
                    text=True,
                    timeout=10
                )
                
                if result.returncode == 0:
                    # Import APScheduler by adding environment's site-packages to path
                    import sys
                    env_path = python_exe.parent.parent
                    if sys.platform == 'win32':
                        site_packages = env_path / 'Lib' / 'site-packages'
                    else:
                        # Find python version
                        site_packages = None
                        lib_path = env_path / 'lib'
                        if lib_path.exists():
                            for p in lib_path.iterdir():
                                if p.name.startswith('python'):
                                    site_packages = p / 'site-packages'
                                    break
                    
                    if site_packages and site_packages.exists():
                        if str(site_packages) not in sys.path:
                            sys.path.insert(0, str(site_packages))
                        
                        # Now try to import APScheduler
                        try:
                            from apscheduler.schedulers.background import BackgroundScheduler
                            from apscheduler.triggers.interval import IntervalTrigger
                            from apscheduler.triggers.cron import CronTrigger
                            from apscheduler.triggers.date import DateTrigger
                            
                            self._scheduler = BackgroundScheduler()
                            self._apscheduler_available = True
                            self._scheduler_python = python_exe
                            logger.info(f"APScheduler initialized from dedicated environment: {python_exe}")
                            return
                        except ImportError:
                            pass
            
            # Fallback: try to import from main environment
            from apscheduler.schedulers.background import BackgroundScheduler
            from apscheduler.triggers.interval import IntervalTrigger
            from apscheduler.triggers.cron import CronTrigger
            from apscheduler.triggers.date import DateTrigger
            
            self._scheduler = BackgroundScheduler()
            self._apscheduler_available = True
            self._scheduler_python = None
            logger.info("APScheduler initialized from main environment")
        except ImportError as e:
            logger.warning(f"APScheduler not available: {e}. Setup scheduler environment first.")
            self._apscheduler_available = False
            self._scheduler_python = None
    
    def init_app(self, app):
        """Initialize with Flask app"""
        self._app = app
        
        # Try to initialize scheduler (will check dedicated environment)
        if not self._apscheduler_available:
            self._init_scheduler()
        
        if not self._apscheduler_available:
            logger.warning("Scheduler not available - APScheduler not installed. Setup scheduler environment first.")
            return
        
        try:
            # Start scheduler
            if not self._scheduler.running:
                self._scheduler.start()
                logger.info("Job scheduler started")
            
            # Load and schedule existing jobs
            self._load_and_schedule_jobs()
        except Exception as e:
            logger.error(f"Failed to start scheduler: {e}")
    
    def reinitialize(self):
        """Re-initialize scheduler (useful after installing packages)"""
        if self._scheduler and self._scheduler.running:
            try:
                self._scheduler.shutdown()
            except:
                pass
        
        self._scheduler = None
        self._apscheduler_available = False
        self._init_scheduler()
        
        if self._apscheduler_available and self._app:
            try:
                if not self._scheduler.running:
                    self._scheduler.start()
                    logger.info("Job scheduler reinitialized and started")
                self._load_and_schedule_jobs()
            except Exception as e:
                logger.error(f"Failed to restart scheduler: {e}")
    
    def _load_and_schedule_jobs(self):
        """Load active jobs from database and schedule them"""
        if not self._app:
            return
        
        try:
            from app.models.scheduled_jobs import ScheduledJob
            
            with self._app.app_context():
                active_jobs = ScheduledJob.query.filter_by(is_active=True).all()
                
                for job in active_jobs:
                    try:
                        self.schedule_job(job)
                    except Exception as e:
                        logger.error(f"Failed to schedule job {job.id}: {e}")
        except Exception as e:
            logger.error(f"Failed to load jobs: {e}")
    
    def schedule_job(self, job) -> bool:
        """Schedule a job"""
        if not self._apscheduler_available or not self._scheduler:
            return False
        
        if not self._scheduler.running:
            try:
                self._scheduler.start()
            except:
                pass
        
        job_id = f"job_{job.id}"
        
        # Remove existing job if any
        self.unschedule_job(job.id)
        
        try:
            from apscheduler.triggers.interval import IntervalTrigger
            from apscheduler.triggers.cron import CronTrigger
            from apscheduler.triggers.date import DateTrigger
            
            schedule_config = job.schedule_config
            
            # Determine trigger based on schedule type
            if job.schedule_type == 'once':
                # Run once at specific time
                run_date = schedule_config.get('run_date')
                if run_date:
                    try:
                        if isinstance(run_date, str):
                            run_date = datetime.fromisoformat(run_date)
                        trigger = DateTrigger(run_date=run_date)
                    except Exception as e:
                        logger.error(f"Invalid run_date for job {job.id}: {e}")
                        return False
                else:
                    logger.warning(f"Job {job.id} has 'once' schedule but no run_date")
                    return False
                    
            elif job.schedule_type == 'interval':
                # Interval-based scheduling
                interval_seconds = schedule_config.get('interval_seconds', 3600)
                trigger = IntervalTrigger(seconds=interval_seconds)
                
            elif job.schedule_type == 'cron':
                # Cron-based scheduling
                cron_expr = schedule_config.get('cron_expression', '0 * * * *')
                parts = cron_expr.split()
                if len(parts) >= 5:
                    trigger = CronTrigger(
                        minute=parts[0] if parts[0] != '*' else None,
                        hour=parts[1] if parts[1] != '*' else None,
                        day=parts[2] if parts[2] != '*' else None,
                        month=parts[3] if parts[3] != '*' else None,
                        day_of_week=parts[4] if parts[4] != '*' else None
                    )
                else:
                    logger.error(f"Invalid cron expression for job {job.id}: {cron_expr}")
                    return False
            else:
                # Manual - don't schedule
                return False
            
            # Add job to scheduler
            self._scheduler.add_job(
                self._execute_job,
                trigger=trigger,
                id=job_id,
                name=job.name,
                args=[job.id],
                replace_existing=True
            )
            
            self._jobs[job.id] = job_id
            
            # Update next run time
            scheduled_job = self._scheduler.get_job(job_id)
            if scheduled_job and scheduled_job.next_run_time and self._app:
                from app.database import db
                with self._app.app_context():
                    job.next_run_at = scheduled_job.next_run_time
                    db.session.commit()
            
            logger.info(f"Scheduled job {job.id}: {job.name}")
            return True
            
        except Exception as e:
            logger.error(f"Failed to schedule job {job.id}: {e}")
            return False
    
    def unschedule_job(self, job_id: int):
        """Unschedule a job"""
        if not self._scheduler:
            return
        
        scheduler_job_id = self._jobs.get(job_id)
        if scheduler_job_id:
            try:
                self._scheduler.remove_job(scheduler_job_id)
                del self._jobs[job_id]
                logger.info(f"Unscheduled job {job_id}")
            except Exception as e:
                logger.warning(f"Failed to unschedule job {job_id}: {e}")
    
    def _execute_job(self, job_id: int):
        """Execute a scheduled job"""
        if not self._app:
            logger.error("Cannot execute job - Flask app not initialized")
            return
        
        try:
            from app.models.scheduled_jobs import ScheduledJob, JobExecution, JobStatus
            from app.database import db
            
            with self._app.app_context():
                job = ScheduledJob.query.get(job_id)
                if not job:
                    logger.error(f"Job {job_id} not found")
                    return
                
                if not job.is_active:
                    logger.info(f"Job {job_id} is not active, skipping")
                    return
                
                # Create execution record
                execution = JobExecution(
                    job_id=job_id,
                    status=JobStatus.RUNNING.value,
                    started_at=datetime.utcnow()
                )
                db.session.add(execution)
                db.session.commit()
                
                start_time = datetime.utcnow()
                
                try:
                    # Execute based on job type
                    result = self._run_job_function(job)
                    
                    # Update execution
                    execution.completed_at = datetime.utcnow()
                    execution.status = JobStatus.SUCCESS.value
                    execution.duration_seconds = (execution.completed_at - start_time).total_seconds()
                    execution.result_json = json.dumps(result) if result else None
                    
                    # Update job
                    job.last_run_at = start_time
                    job.last_status = JobStatus.SUCCESS.value
                    job.last_result = json.dumps(result) if result else None
                    job.last_error = None
                    job.run_count += 1
                    job.success_count += 1
                    
                    # Calculate next run time
                    if job.schedule_type != 'once' and self._scheduler:
                        scheduled_job = self._scheduler.get_job(f"job_{job_id}")
                        if scheduled_job and scheduled_job.next_run_time:
                            job.next_run_at = scheduled_job.next_run_time
                    
                    db.session.commit()
                    logger.info(f"Job {job_id} executed successfully")
                    
                except Exception as e:
                    error_msg = str(e)
                    logger.error(f"Job {job_id} execution failed: {error_msg}")
                    
                    # Update execution
                    execution.completed_at = datetime.utcnow()
                    execution.status = JobStatus.FAILED.value
                    execution.duration_seconds = (execution.completed_at - start_time).total_seconds()
                    execution.error_message = error_msg
                    
                    # Update job
                    job.last_run_at = start_time
                    job.last_status = JobStatus.FAILED.value
                    job.last_error = error_msg
                    job.run_count += 1
                    job.failure_count += 1
                    
                    db.session.commit()
                    
        except Exception as e:
            logger.error(f"Error executing job {job_id}: {e}", exc_info=True)
    
    def _run_job_function(self, job) -> Any:
        """Execute the actual job function"""
        try:
            # Execute based on job configuration
            if job.function_name:
                # Execute Python function
                return self._execute_python_function(job)
            elif job.api_endpoint:
                # Call API endpoint
                return self._execute_api_call(job)
            elif job.script_path:
                # Execute script
                return self._execute_script(job)
            else:
                raise ValueError("Job has no function_name, api_endpoint, or script_path")
        except Exception as e:
            logger.error(f"Error running job function: {e}")
            raise
    
    def _execute_python_function(self, job) -> Any:
        """Execute a Python function"""
        try:
            # Parse function name (module.function or just function)
            if '.' in job.function_name:
                module_name, func_name = job.function_name.rsplit('.', 1)
                module = importlib.import_module(module_name)
                func = getattr(module, func_name)
            else:
                # Try to find in common modules
                func = self._find_function(job.function_name, job.module)
            
            if not func:
                raise ValueError(f"Function {job.function_name} not found")
            
            # Call function with parameters
            params = job.parameters
            if callable(func):
                return func(**params) if params else func()
            else:
                raise ValueError(f"{job.function_name} is not callable")
                
        except Exception as e:
            logger.error(f"Error executing Python function: {e}")
            raise
    
    def _find_function(self, func_name: str, module: str) -> Optional[Callable]:
        """Find function in module-specific locations"""
        try:
            if module == 'llm':
                from app.services.llm_manager import LLMManager
                # Add LLM-specific function mappings
                pass
            elif module == 'rag':
                from app.services.rag_service import get_rag_service
                # Add RAG-specific function mappings
                pass
            elif module == 'document_extraction':
                from app.services.document_extractor import get_document_extractor
                # Add document extraction function mappings
                pass
            
            # Try to import from app.services
            service_module = f"app.services.{module}_service"
            try:
                module_obj = importlib.import_module(service_module)
                return getattr(module_obj, func_name, None)
            except:
                pass
            
            return None
        except Exception as e:
            logger.error(f"Error finding function: {e}")
            return None
    
    def _execute_api_call(self, job) -> Any:
        """Execute an API call"""
        import requests
        
        try:
            url = job.api_endpoint
            if not url.startswith('http'):
                # Relative URL - construct full URL
                url = f"http://localhost:5000{url}" if not url.startswith('/') else f"http://localhost:5000{url}"
            
            method = job.parameters.get('method', 'POST')
            headers = job.parameters.get('headers', {})
            data = job.parameters.get('data', {})
            
            response = requests.request(method, url, json=data, headers=headers, timeout=300)
            response.raise_for_status()
            
            return response.json()
        except Exception as e:
            logger.error(f"Error executing API call: {e}")
            raise
    
    def _execute_script(self, job) -> Any:
        """Execute a script file"""
        script_path = Path(job.script_path)
        if not script_path.exists():
            raise FileNotFoundError(f"Script not found: {script_path}")
        
        # Determine Python executable based on module
        python_exe = sys.executable
        if job.module == 'rag':
            from app.services.rag_environment import RAGEnvironmentManager
            rag_env = RAGEnvironmentManager()
            rag_python = rag_env.get_env_python()
            if rag_python and rag_python.exists():
                python_exe = str(rag_python)
        elif job.module == 'document_extraction':
            from app.services.document_extraction_environment import get_doc_extraction_env
            doc_env = get_doc_extraction_env()
            doc_python = doc_env.get_python_executable()
            if doc_python and doc_python.exists():
                python_exe = str(doc_python)
        
        try:
            result = subprocess.run(
                [python_exe, str(script_path)],
                capture_output=True,
                text=True,
                timeout=3600,  # 1 hour max
                cwd=str(script_path.parent)
            )
            
            if result.returncode != 0:
                raise RuntimeError(f"Script failed: {result.stderr}")
            
            return {
                'stdout': result.stdout,
                'stderr': result.stderr,
                'returncode': result.returncode
            }
        except Exception as e:
            logger.error(f"Error executing script: {e}")
            raise
    
    def run_job_now(self, job_id: int) -> Dict[str, Any]:
        """Manually trigger a job to run now. Returns execution info."""
        try:
            from app.models.scheduled_jobs import ScheduledJob, JobExecution, JobStatus
            from app.database import db
            
            with self._app.app_context():
                job = ScheduledJob.query.get(job_id)
                if not job:
                    return {'success': False, 'error': 'Job not found'}
                
                # Check if already running
                if job.is_running:
                    return {
                        'success': False,
                        'error': 'Job is already running',
                        'execution_id': job.current_execution_id
                    }
                
                # Create execution record immediately
                execution = JobExecution(
                    job_id=job_id,
                    status=JobStatus.RUNNING.value,
                    started_at=datetime.utcnow()
                )
                db.session.add(execution)
                db.session.commit()
                
                # Run in background thread
                thread = threading.Thread(target=self._execute_job, args=[job_id, 0], daemon=True)
                thread.start()
                self._running_jobs[job_id] = thread
                
                return {
                    'success': True,
                    'message': 'Job execution started',
                    'execution_id': execution.id,
                    'job_id': job_id
                }
        except Exception as e:
            logger.error(f"Error running job now: {e}")
            return {'success': False, 'error': str(e)}
    
    def stop_job(self, job_id: int) -> Dict[str, Any]:
        """Stop a running job"""
        try:
            from app.models.scheduled_jobs import ScheduledJob, JobExecution, JobStatus
            from app.database import db
            
            with self._app.app_context():
                job = ScheduledJob.query.get(job_id)
                if not job:
                    return {'success': False, 'error': 'Job not found'}
                
                if not job.is_running:
                    return {'success': False, 'error': 'Job is not currently running'}
                
                # Cancel the running thread (if possible)
                thread = self._running_jobs.get(job_id)
                if thread and thread.is_alive():
                    # Note: Python threads can't be forcefully stopped, but we can mark it
                    # The job will check is_running status periodically
                    pass
                
                # Mark job as stopped
                job.is_running = False
                
                # Update current execution
                if job.current_execution_id:
                    execution = JobExecution.query.get(job.current_execution_id)
                    if execution and execution.status == JobStatus.RUNNING.value:
                        execution.status = JobStatus.CANCELLED.value
                        execution.completed_at = datetime.utcnow()
                        execution.error_message = 'Job stopped by user'
                        execution.duration_seconds = (execution.completed_at - execution.started_at).total_seconds() if execution.started_at else None
                
                job.current_execution_id = None
                db.session.commit()
                
                # Remove from running jobs
                self._running_jobs.pop(job_id, None)
                
                return {
                    'success': True,
                    'message': 'Job stop requested',
                    'job_id': job_id
                }
        except Exception as e:
            logger.error(f"Error stopping job: {e}")
            return {'success': False, 'error': str(e)}
    
    def get_job_execution_status(self, job_id: int, execution_id: int = None) -> Dict[str, Any]:
        """Get current execution status of a job"""
        try:
            from app.models.scheduled_jobs import ScheduledJob, JobExecution
            from app.database import db
            
            with self._app.app_context():
                job = ScheduledJob.query.get(job_id)
                if not job:
                    return {'success': False, 'error': 'Job not found'}
                
                execution = None
                if execution_id:
                    execution = JobExecution.query.get(execution_id)
                elif job.current_execution_id:
                    execution = JobExecution.query.get(job.current_execution_id)
                
                result = {
                    'success': True,
                    'job_id': job_id,
                    'is_running': job.is_running,
                    'last_status': job.last_status,
                    'retry_count': job.retry_count,
                    'max_retries': job.max_retries if job.retry_enabled else 0
                }
                
                if execution:
                    result['execution'] = execution.to_dict()
                
                return result
        except Exception as e:
            logger.error(f"Error getting job execution status: {e}")
            return {'success': False, 'error': str(e)}
    
    def get_job_status(self, job_id: int) -> Dict[str, Any]:
        """Get status of a scheduled job"""
        if not self._scheduler:
            return {'scheduler_available': False}
        
        scheduler_job_id = self._jobs.get(job_id)
        if scheduler_job_id:
            job = self._scheduler.get_job(scheduler_job_id)
            if job:
                return {
                    'scheduler_available': True,
                    'scheduled': True,
                    'next_run_time': job.next_run_time.isoformat() if job.next_run_time else None,
                    'job_state': str(job)
                }
        
        return {
            'scheduler_available': True,
            'scheduled': False
        }
    
    def get_scheduler_status(self) -> Dict[str, Any]:
        """Get overall scheduler status"""
        return {
            'available': self._apscheduler_available,
            'running': self._scheduler.running if self._scheduler else False,
            'job_count': len(self._jobs)
        }


def get_job_scheduler() -> JobScheduler:
    """Get singleton instance of JobScheduler"""
    return JobScheduler()
