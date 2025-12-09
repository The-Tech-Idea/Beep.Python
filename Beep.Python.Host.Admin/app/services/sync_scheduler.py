"""
RAG Sync Scheduler Service
Cross-platform scheduler using APScheduler for Windows, Mac, and Linux.
Manages scheduled jobs for refreshing RAG documents from various data sources.

This scheduler runs within the RAG environment to use its installed packages.
"""
import os
import sys
import json
import threading
import subprocess
from datetime import datetime, timedelta
from typing import Dict, List, Any, Optional, Callable
from pathlib import Path


class RAGSyncScheduler:
    """
    Cross-platform scheduler for RAG document sync jobs.
    Uses APScheduler's BackgroundScheduler which works on all platforms.
    Runs within the RAG environment to access its installed packages.
    """
    
    def __init__(self, app=None):
        self.app = app
        self._scheduler = None
        self._jobs = {}  # Track our managed jobs
        self._initialized = False
        self._apscheduler_available = False
        self._rag_python = None
        
        # Check if RAG environment is available and has APScheduler
        self._check_rag_environment()
    
    def _check_rag_environment(self):
        """Check if RAG environment has APScheduler installed"""
        try:
            from app.services.rag_environment import RAGEnvironmentManager
            
            rag_env = RAGEnvironmentManager()
            self._rag_python = rag_env.get_env_python()
            
            if self._rag_python and Path(self._rag_python).exists():
                # Check if APScheduler is installed in RAG env
                result = subprocess.run(
                    [self._rag_python, '-c', 'import apscheduler; print("ok")'],
                    capture_output=True,
                    text=True,
                    timeout=10
                )
                self._apscheduler_available = result.returncode == 0 and 'ok' in result.stdout
                
                if self._apscheduler_available:
                    # Import APScheduler from RAG env by adding site-packages to path
                    rag_path = Path(self._rag_python).parent.parent
                    if sys.platform == 'win32':
                        site_packages = rag_path / 'Lib' / 'site-packages'
                    else:
                        # Find python version
                        site_packages = None
                        lib_path = rag_path / 'lib'
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
                            self._apscheduler_available = True
                        except ImportError:
                            self._apscheduler_available = False
                else:
                    print("Warning: APScheduler not installed in RAG environment.")
                    print("Run RAG setup to install required packages including APScheduler.")
            else:
                print("Warning: RAG environment not ready. Scheduler will not be available.")
                print("Please set up the RAG environment first.")
                
        except Exception as e:
            print(f"Warning: Could not check RAG environment: {e}")
            self._apscheduler_available = False
    
    def init_app(self, app):
        """Initialize with Flask app"""
        self.app = app
        
        if not self._apscheduler_available:
            return
        
        try:
            # Create scheduler using APScheduler from RAG environment
            from apscheduler.schedulers.background import BackgroundScheduler
            from apscheduler.jobstores.memory import MemoryJobStore
            
            jobstores = {
                'default': MemoryJobStore()
            }
            
            self._scheduler = BackgroundScheduler(
                jobstores=jobstores,
                timezone='UTC'
            )
            
            self._initialized = True
        except ImportError as e:
            print(f"Warning: Could not initialize scheduler: {e}")
            self._initialized = False
    
    def start(self):
        """Start the scheduler"""
        if not self._initialized or not self._scheduler:
            return False
        
        if not self._scheduler.running:
            self._scheduler.start()
            print("RAG Sync Scheduler started")
            
            # Load and schedule existing jobs from database
            self._load_scheduled_jobs()
        
        return True
    
    def stop(self):
        """Stop the scheduler"""
        if self._scheduler and self._scheduler.running:
            self._scheduler.shutdown(wait=False)
            print("RAG Sync Scheduler stopped")
    
    @property
    def is_available(self) -> bool:
        """Check if scheduler is available"""
        return self._apscheduler_available
    
    @property
    def is_running(self) -> bool:
        """Check if scheduler is running"""
        return self._scheduler and self._scheduler.running
    
    def _load_scheduled_jobs(self):
        """Load active jobs from database and schedule them"""
        if not self.app:
            return
        
        with self.app.app_context():
            try:
                # Check if tables exist first
                from app.database import db
                from sqlalchemy import inspect
                
                inspector = inspect(db.engine)
                tables = inspector.get_table_names()
                
                if 'rag_sync_jobs' not in tables:
                    print("Sync jobs table not found. Run database migration first.")
                    print("Use POST /rag/api/db/migrate to create missing tables.")
                    return
                
                from app.models.rag_metadata import SyncJob
                
                active_jobs = SyncJob.query.filter_by(is_active=True).all()
                
                for job in active_jobs:
                    if job.schedule_type != 'manual':
                        self.schedule_job(job)
                
                print(f"Loaded {len(active_jobs)} sync jobs from database")
            except Exception as e:
                print(f"Error loading scheduled jobs: {e}")
                import traceback
                traceback.print_exc()
    
    def schedule_job(self, sync_job) -> bool:
        """Schedule a sync job"""
        if not self._scheduler or not self._scheduler.running:
            return False
        
        job_id = f"sync_job_{sync_job.id}"
        
        # Remove existing job if any
        self.unschedule_job(sync_job.id)
        
        try:
            from apscheduler.triggers.interval import IntervalTrigger
            from apscheduler.triggers.cron import CronTrigger
            
            schedule_config = sync_job.schedule_config or {}
            
            if sync_job.schedule_type == 'interval':
                # Interval-based scheduling
                interval_minutes = schedule_config.get('interval_minutes', 60)
                trigger = IntervalTrigger(minutes=interval_minutes)
                
            elif sync_job.schedule_type == 'cron':
                # Cron-based scheduling
                cron_expr = schedule_config.get('cron_expression', '0 * * * *')
                parts = cron_expr.split()
                if len(parts) >= 5:
                    trigger = CronTrigger(
                        minute=parts[0],
                        hour=parts[1],
                        day=parts[2],
                        month=parts[3],
                        day_of_week=parts[4]
                    )
                else:
                    print(f"Invalid cron expression: {cron_expr}")
                    return False
            else:
                # Manual or unknown - don't schedule
                return False
            
            # Add job to scheduler
            self._scheduler.add_job(
                self._execute_sync_job,
                trigger=trigger,
                id=job_id,
                name=sync_job.name,
                args=[sync_job.id],
                replace_existing=True
            )
            
            self._jobs[sync_job.id] = job_id
            
            # Update next run time in database
            job = self._scheduler.get_job(job_id)
            if job and job.next_run_time:
                with self.app.app_context():
                    from app.models.rag_metadata import SyncJob
                    from app.database import db
                    
                    db_job = SyncJob.query.get(sync_job.id)
                    if db_job:
                        db_job.next_run_at = job.next_run_time
                        db.session.commit()
            
            return True
            
        except Exception as e:
            print(f"Error scheduling job {sync_job.id}: {e}")
            return False
    
    def unschedule_job(self, job_id: int) -> bool:
        """Remove a job from the scheduler"""
        if not self._scheduler:
            return False
        
        scheduler_job_id = f"sync_job_{job_id}"
        
        try:
            self._scheduler.remove_job(scheduler_job_id)
            if job_id in self._jobs:
                del self._jobs[job_id]
            return True
        except Exception:
            return False
    
    def _execute_sync_job(self, job_id: int):
        """Execute a sync job - runs in the RAG environment"""
        if not self.app:
            return
        
        with self.app.app_context():
            try:
                from app.models.rag_metadata import SyncJob, SyncJobRun, DataSource
                from app.database import db
                from app.services.data_source_connectors import DataSourceFactory
                from datetime import datetime
                
                # Get job and data source
                sync_job = SyncJob.query.get(job_id)
                if not sync_job:
                    print(f"Sync job {job_id} not found")
                    return
                
                data_source = DataSource.query.get(sync_job.data_source_id)
                if not data_source:
                    print(f"Data source {sync_job.data_source_id} not found")
                    return
                
                # Create job run record
                job_run = SyncJobRun(
                    sync_job_id=job_id,
                    status='running',
                    started_at=datetime.utcnow()
                )
                db.session.add(job_run)
                db.session.commit()
                
                # Update job status
                sync_job.status = 'running'
                sync_job.last_run_at = datetime.utcnow()
                db.session.commit()
                
                try:
                    # Create connector and fetch documents
                    connector = DataSourceFactory.create_connector(
                        data_source.source_type,
                        data_source.connection_config
                    )
                    
                    documents_synced = 0
                    errors = []
                    
                    # Connect and fetch documents
                    if connector.connect():
                        for doc in connector.fetch_documents():
                            try:
                                # Add document to RAG collection
                                self._add_document_to_collection(
                                    data_source.collection_id,
                                    doc
                                )
                                documents_synced += 1
                            except Exception as e:
                                errors.append(str(e))
                        
                        connector.disconnect()
                    else:
                        errors.append("Failed to connect to data source")
                    
                    # Update job run record
                    job_run.status = 'completed' if not errors else 'completed_with_errors'
                    job_run.completed_at = datetime.utcnow()
                    job_run.documents_synced = documents_synced
                    job_run.error_message = '; '.join(errors[:5]) if errors else None
                    
                    # Update job status
                    sync_job.status = 'idle'
                    sync_job.documents_synced = (sync_job.documents_synced or 0) + documents_synced
                    
                    db.session.commit()
                    
                    print(f"Sync job {job_id} completed: {documents_synced} documents synced")
                    
                except Exception as e:
                    job_run.status = 'failed'
                    job_run.completed_at = datetime.utcnow()
                    job_run.error_message = str(e)
                    
                    sync_job.status = 'error'
                    sync_job.last_error = str(e)
                    
                    db.session.commit()
                    
                    print(f"Sync job {job_id} failed: {e}")
                    
            except Exception as e:
                print(f"Error executing sync job {job_id}: {e}")
    
    def _add_document_to_collection(self, collection_id: str, doc: Dict[str, Any]):
        """Add a document to a RAG collection using subprocess"""
        if not self._rag_python:
            raise RuntimeError("RAG environment not available")
        
        from app.models.rag_metadata import Collection
        
        collection = Collection.query.filter_by(id=collection_id).first()
        if not collection:
            raise ValueError(f"Collection {collection_id} not found")
        
        # Get the provider and database from the collection
        provider = collection.provider_type
        db_path = collection.database_path
        
        # Prepare the document for the subprocess
        doc_json = json.dumps({
            'collection_id': collection_id,
            'content': doc.get('content', ''),
            'metadata': doc.get('metadata', {}),
            'doc_id': doc.get('id')
        })
        
        # Build the add document script based on provider
        if provider == 'faiss':
            script = self._get_faiss_add_script(collection_id, db_path)
        elif provider == 'chromadb':
            script = self._get_chromadb_add_script(collection_id, db_path)
        else:
            raise ValueError(f"Unsupported provider: {provider}")
        
        result = subprocess.run(
            [self._rag_python, '-c', script, doc_json],
            capture_output=True,
            text=True,
            timeout=60
        )
        
        if result.returncode != 0:
            raise RuntimeError(f"Failed to add document: {result.stderr}")
    
    def _get_faiss_add_script(self, collection_id: str, db_path: str) -> str:
        """Get script for adding document to FAISS"""
        return f'''
import sys
import json
from sentence_transformers import SentenceTransformer
import faiss
import numpy as np
import pickle
from pathlib import Path

doc_data = json.loads(sys.argv[1])
content = doc_data['content']
metadata = doc_data['metadata']
doc_id = doc_data.get('doc_id')

# Load model and index
model = SentenceTransformer('all-MiniLM-L6-v2')
index_path = Path("{db_path}") / "{collection_id}"
index_path.mkdir(parents=True, exist_ok=True)

# Generate embedding
embedding = model.encode([content])[0]

# Load or create index
faiss_index_path = index_path / 'index.faiss'
if faiss_index_path.exists():
    index = faiss.read_index(str(faiss_index_path))
else:
    index = faiss.IndexFlatL2(embedding.shape[0])

# Add to index
index.add(np.array([embedding], dtype=np.float32))

# Save
faiss.write_index(index, str(faiss_index_path))

print(json.dumps({{"success": True}}))
'''
    
    def _get_chromadb_add_script(self, collection_id: str, db_path: str) -> str:
        """Get script for adding document to ChromaDB"""
        return f'''
import sys
import json
import chromadb

doc_data = json.loads(sys.argv[1])
content = doc_data['content']
metadata = doc_data['metadata']
doc_id = doc_data.get('doc_id') or str(hash(content))

client = chromadb.PersistentClient(path="{db_path}")
collection = client.get_or_create_collection("{collection_id}")

collection.add(
    documents=[content],
    metadatas=[metadata],
    ids=[doc_id]
)

print(json.dumps({{"success": True}}))
'''
    
    def run_job_now(self, job_id: int) -> Dict[str, Any]:
        """Manually trigger a sync job"""
        if not self.app:
            return {'success': False, 'error': 'App not initialized'}
        
        # Run in a thread to not block
        thread = threading.Thread(
            target=self._execute_sync_job,
            args=[job_id]
        )
        thread.start()
        
        return {'success': True, 'message': 'Sync job started'}
    
    def get_all_jobs(self) -> List[Dict[str, Any]]:
        """Get all scheduled jobs"""
        if not self._scheduler:
            return []
        
        jobs = []
        for job in self._scheduler.get_jobs():
            jobs.append({
                'id': job.id,
                'name': job.name,
                'next_run': job.next_run_time.isoformat() if job.next_run_time else None
            })
        
        return jobs


# Singleton instance
_sync_scheduler: Optional[RAGSyncScheduler] = None


def get_sync_scheduler() -> RAGSyncScheduler:
    """Get the singleton scheduler instance"""
    global _sync_scheduler
    
    if _sync_scheduler is None:
        _sync_scheduler = RAGSyncScheduler()
    
    return _sync_scheduler


def init_scheduler(app):
    """Initialize the scheduler with Flask app"""
    scheduler = get_sync_scheduler()
    scheduler.init_app(app)
    
    # Start scheduler when app is ready (only if available)
    if scheduler.is_available:
        @app.before_request
        def start_scheduler_once():
            # Remove this handler after first request
            try:
                app.before_request_funcs[None].remove(start_scheduler_once)
            except (ValueError, KeyError):
                pass
            scheduler.start()
    
    return scheduler
