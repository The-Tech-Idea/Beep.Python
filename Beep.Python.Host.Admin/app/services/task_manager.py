"""
Task Manager Service
Manages background tasks with progress tracking
"""
import uuid
import threading
import time
from dataclasses import dataclass, field, asdict
from typing import Dict, List, Optional, Callable, Any
from enum import Enum
from datetime import datetime


class TaskStatus(Enum):
    PENDING = "pending"
    RUNNING = "running"
    COMPLETED = "completed"
    FAILED = "failed"
    CANCELLED = "cancelled"


@dataclass
class TaskStep:
    """Represents a step in a task"""
    name: str
    status: str = "pending"  # pending, running, completed, failed
    message: str = ""
    started_at: Optional[str] = None
    completed_at: Optional[str] = None


@dataclass
class Task:
    """Represents a background task"""
    id: str
    name: str
    task_type: str
    status: str = "pending"
    progress: int = 0
    message: str = ""
    steps: List[TaskStep] = field(default_factory=list)
    result: Any = None
    error: Optional[str] = None
    created_at: str = field(default_factory=lambda: datetime.now().isoformat())
    started_at: Optional[str] = None
    completed_at: Optional[str] = None
    
    def to_dict(self) -> dict:
        return {
            "id": self.id,
            "name": self.name,
            "task_type": self.task_type,
            "status": self.status,
            "progress": self.progress,
            "message": self.message,
            "steps": [{"name": s.name, "status": s.status, "message": s.message} for s in self.steps],
            "result": self.result,
            "error": self.error,
            "created_at": self.created_at,
            "started_at": self.started_at,
            "completed_at": self.completed_at
        }


class TaskManager:
    """Manages background tasks with progress tracking"""
    
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
        self._tasks: Dict[str, Task] = {}
        self._callbacks: Dict[str, List[Callable]] = {}
        self._initialized = True
    
    def create_task(self, name: str, task_type: str, steps: List[str] = None) -> Task:
        """Create a new task"""
        task_id = str(uuid.uuid4())[:8]
        task = Task(
            id=task_id,
            name=name,
            task_type=task_type,
            steps=[TaskStep(name=s) for s in (steps or [])]
        )
        self._tasks[task_id] = task
        return task
    
    def get_task(self, task_id: str) -> Optional[Task]:
        """Get a task by ID"""
        return self._tasks.get(task_id)
    
    def list_tasks(self, limit: int = 50) -> List[Task]:
        """List recent tasks"""
        tasks = list(self._tasks.values())
        tasks.sort(key=lambda t: t.created_at, reverse=True)
        return tasks[:limit]
    
    def start_task(self, task_id: str):
        """Mark a task as started"""
        task = self._tasks.get(task_id)
        if task:
            task.status = "running"
            task.started_at = datetime.now().isoformat()
            self._notify(task_id)
    
    def update_progress(self, task_id: str, progress: int, message: str = ""):
        """Update task progress (0-100)"""
        task = self._tasks.get(task_id)
        if task:
            task.progress = min(100, max(0, progress))
            if message:
                task.message = message
            self._notify(task_id)
    
    def update_step(self, task_id: str, step_index: int, status: str, message: str = ""):
        """Update a specific step"""
        task = self._tasks.get(task_id)
        if task and 0 <= step_index < len(task.steps):
            step = task.steps[step_index]
            step.status = status
            step.message = message
            if status == "running":
                step.started_at = datetime.now().isoformat()
            elif status in ("completed", "failed"):
                step.completed_at = datetime.now().isoformat()
            self._notify(task_id)
    
    def complete_task(self, task_id: str, result: Any = None):
        """Mark a task as completed"""
        task = self._tasks.get(task_id)
        if task:
            task.status = "completed"
            task.progress = 100
            task.result = result
            task.completed_at = datetime.now().isoformat()
            self._notify(task_id)
    
    def fail_task(self, task_id: str, error: str):
        """Mark a task as failed"""
        task = self._tasks.get(task_id)
        if task:
            task.status = "failed"
            task.error = error
            task.completed_at = datetime.now().isoformat()
            self._notify(task_id)
    
    def add_callback(self, task_id: str, callback: Callable):
        """Add a callback for task updates"""
        if task_id not in self._callbacks:
            self._callbacks[task_id] = []
        self._callbacks[task_id].append(callback)
    
    def remove_callback(self, task_id: str, callback: Callable):
        """Remove a callback"""
        if task_id in self._callbacks:
            self._callbacks[task_id] = [c for c in self._callbacks[task_id] if c != callback]
    
    def _notify(self, task_id: str):
        """Notify all callbacks for a task"""
        task = self._tasks.get(task_id)
        if task and task_id in self._callbacks:
            for callback in self._callbacks[task_id]:
                try:
                    callback(task)
                except Exception as e:
                    print(f"Callback error: {e}")
    
    def cleanup_old_tasks(self, max_age_hours: int = 24):
        """Remove old completed tasks"""
        cutoff = datetime.now().timestamp() - (max_age_hours * 3600)
        to_remove = []
        for task_id, task in self._tasks.items():
            if task.status in ("completed", "failed", "cancelled"):
                try:
                    task_time = datetime.fromisoformat(task.completed_at or task.created_at).timestamp()
                    if task_time < cutoff:
                        to_remove.append(task_id)
                except:
                    pass
        for task_id in to_remove:
            del self._tasks[task_id]
            if task_id in self._callbacks:
                del self._callbacks[task_id]
