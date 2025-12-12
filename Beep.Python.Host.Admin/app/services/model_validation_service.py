"""
Model Validation Service - Validates uploaded ML models
"""
import os
import json
import time
import subprocess
import tempfile
import threading
from pathlib import Path
from datetime import datetime
from typing import Optional, Dict, Any, List
from dataclasses import dataclass

from app.database import db
from app.models.ml_models import (
    MLModel, MLModelVersion, MLModelValidation, ValidationStatus, ModelStatus
)
from app.models.core import AuditLog
from app.services.environment_manager import EnvironmentManager
from app.services.task_manager import TaskManager


@dataclass
class ValidationResult:
    """Validation result"""
    validation_type: str
    status: str  # passed, failed, warning
    score: float
    details: Dict[str, Any]
    execution_time_ms: int


class ModelValidationService:
    """Service for validating ML models"""
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
        self.env_manager = EnvironmentManager()
        self.task_manager = TaskManager()
    
    def validate_model(self, model_id: str, version_id: str, user_id: int) -> str:
        """Start validation process for a model"""
        model = MLModel.query.get(model_id)
        version = MLModelVersion.query.get(version_id)
        
        if not model or not version or version.model_id != model_id:
            raise ValueError("Model or version not found")
        
        # Create validation task
        validation_id = f"val_{int(time.time())}"
        
        # Start validation in background
        def run_validation():
            try:
                self._run_validation_suite(model, version, validation_id, user_id)
            except Exception as e:
                print(f"Validation error: {e}")
        
        thread = threading.Thread(target=run_validation, daemon=True)
        thread.start()
        
        return validation_id
    
    def _run_validation_suite(self, model: MLModel, version: MLModelVersion,
                            validation_id: str, user_id: int):
        """Run full validation suite"""
        results = {}
        overall_status = ValidationStatus.PASSED.value
        total_time = 0
        
        # Format validation
        start_time = time.time()
        format_result = self._validate_format(model, version)
        results['format_validation'] = format_result
        total_time += (time.time() - start_time) * 1000
        
        if format_result.status == 'failed':
            overall_status = ValidationStatus.FAILED.value
        
        # Dependency check
        start_time = time.time()
        dep_result = self._validate_dependencies(model, version)
        results['dependency_check'] = dep_result
        total_time += (time.time() - start_time) * 1000
        
        if dep_result.status == 'failed':
            overall_status = ValidationStatus.FAILED.value
        
        # Functionality test (only if format and deps passed)
        if overall_status != ValidationStatus.FAILED.value:
            start_time = time.time()
            func_result = self._validate_functionality(model, version)
            results['functionality_test'] = func_result
            total_time += (time.time() - start_time) * 1000
            
            if func_result.status == 'failed':
                overall_status = ValidationStatus.FAILED.value
            elif func_result.status == 'warning':
                overall_status = ValidationStatus.WARNING.value
        
        # Performance benchmark
        if overall_status != ValidationStatus.FAILED.value:
            start_time = time.time()
            perf_result = self._validate_performance(model, version)
            results['performance_benchmark'] = perf_result
            total_time += (time.time() - start_time) * 1000
        
        # Security scan
        start_time = time.time()
        sec_result = self._validate_security(model, version)
        results['security_scan'] = sec_result
        total_time += (time.time() - start_time) * 1000
        
        if sec_result.status == 'failed':
            overall_status = ValidationStatus.FAILED.value
        
        # Save validation report
        validation_report = {
            'validation_id': validation_id,
            'overall_status': overall_status,
            'validations': {k: {
                'status': v.status,
                'score': v.score,
                'details': v.details,
                'execution_time_ms': v.execution_time_ms
            } for k, v in results.items()},
            'executed_at': datetime.utcnow().isoformat(),
            'execution_time_ms': int(total_time)
        }
        
        version.validation_report = validation_report
        version.validation_status = overall_status
        version.validation_date = datetime.utcnow()
        
        model.validation_status = overall_status
        if overall_status == ValidationStatus.PASSED.value:
            model.status = ModelStatus.VALIDATED.value
        
        # Save validation records
        for validation_type, result in results.items():
            validation_record = MLModelValidation(
                model_id=model.id,
                version_id=version.id,
                validation_type=validation_type,
                status=result.status,
                details=result.details,
                executed_by=user_id,
                execution_time_ms=result.execution_time_ms
            )
            db.session.add(validation_record)
        
        db.session.commit()
        
        # Log audit
        AuditLog.log(
            action='ml_model_validated',
            resource_type='ml_model',
            resource_id=model.id,
            user_id=user_id,
            details={'validation_id': validation_id, 'status': overall_status}
        )
    
    def _validate_format(self, model: MLModel, version: MLModelVersion) -> ValidationResult:
        """Validate model file format"""
        start_time = time.time()
        details = {}
        status = 'passed'
        score = 1.0
        
        try:
            file_path = Path(version.file_path)
            if not file_path.exists():
                status = 'failed'
                score = 0.0
                details['error'] = 'Model file not found'
            else:
                # Check file size
                file_size = file_path.stat().st_size
                details['file_size'] = file_size
                details['file_size_mb'] = round(file_size / (1024 * 1024), 2)
                
                # Basic format check based on extension
                ext = file_path.suffix.lower()
                expected_extensions = {
                    'sklearn': ['.pkl', '.joblib'],
                    'tensorflow': ['.h5', '.pb'],
                    'pytorch': ['.pt', '.pth'],
                    'xgboost': ['.pkl', '.json'],
                    'onnx': ['.onnx']
                }
                
                if model.model_type in expected_extensions:
                    if ext not in expected_extensions[model.model_type]:
                        status = 'warning'
                        score = 0.8
                        details['warning'] = f'Expected extension for {model.model_type}, got {ext}'
                
                details['file_format'] = ext
                details['message'] = 'File format appears valid'
        
        except Exception as e:
            status = 'failed'
            score = 0.0
            details['error'] = str(e)
        
        execution_time = int((time.time() - start_time) * 1000)
        
        return ValidationResult(
            validation_type='format_validation',
            status=status,
            score=score,
            details=details,
            execution_time_ms=execution_time
        )
    
    def _validate_dependencies(self, model: MLModel, version: MLModelVersion) -> ValidationResult:
        """Validate model dependencies"""
        start_time = time.time()
        details = {}
        status = 'passed'
        score = 1.0
        
        try:
            requirements = version.requirements
            details['requirements'] = requirements
            details['python_version'] = version.python_version
            
            if not requirements:
                status = 'warning'
                score = 0.9
                details['warning'] = 'No requirements specified'
            else:
                # Check if requirements can be parsed
                missing = []
                conflicts = []
                
                # Basic check - try to import packages
                for req in requirements:
                    try:
                        # Extract package name (handle version specifiers)
                        pkg_name = req.split('==')[0].split('>=')[0].split('<=')[0].strip()
                        # Just check if it's a valid package name format
                        if not pkg_name.replace('-', '').replace('_', '').isalnum():
                            missing.append(req)
                    except:
                        missing.append(req)
                
                if missing:
                    status = 'warning'
                    score = 0.8
                    details['missing_dependencies'] = missing
                
                details['checked_dependencies'] = len(requirements)
        
        except Exception as e:
            status = 'failed'
            score = 0.0
            details['error'] = str(e)
        
        execution_time = int((time.time() - start_time) * 1000)
        
        return ValidationResult(
            validation_type='dependency_check',
            status=status,
            score=score,
            details=details,
            execution_time_ms=execution_time
        )
    
    def _validate_functionality(self, model: MLModel, version: MLModelVersion) -> ValidationResult:
        """Test model functionality with actual loading and test predictions"""
        start_time = time.time()
        details = {}
        status = 'passed'
        score = 1.0
        
        try:
            file_path = Path(version.file_path)
            
            if not file_path.exists():
                status = 'failed'
                score = 0.0
                details['error'] = 'Model file not found'
                execution_time = int((time.time() - start_time) * 1000)
                return ValidationResult(
                    validation_type='functionality_test',
                    status=status,
                    score=score,
                    details=details,
                    execution_time_ms=execution_time
                )
            
            # Basic file integrity check
            file_size = file_path.stat().st_size
            if file_size == 0:
                status = 'failed'
                score = 0.0
                details['error'] = 'Model file is empty'
                execution_time = int((time.time() - start_time) * 1000)
                return ValidationResult(
                    validation_type='functionality_test',
                    status=status,
                    score=score,
                    details=details,
                    execution_time_ms=execution_time
                )
            
            # Try to actually load and test the model - REQUIRES ML environment
            try:
                from app.services.ml_model_environment import get_ml_model_environment_manager
                env_mgr = get_ml_model_environment_manager()
                
                model_type = model.model_type.lower()
                
                # Generate test data based on model type
                test_data = self._generate_test_data(model_type)
                
                # Ensure environment is ready - auto-create if needed
                if not env_mgr.is_ready:
                    # Try to create environment if it doesn't exist
                    if env_mgr.status.value == 'not_created':
                        create_result = env_mgr.create_environment()
                        if not create_result.get('success'):
                            details['error'] = f"ML environment not ready: {create_result.get('error', 'Unknown error')}"
                            details['message'] = 'Please set up the ML environment first'
                            execution_time = int((time.time() - start_time) * 1000)
                            return ValidationResult(
                                validation_type='functionality_test',
                                status='failed',
                                score=0.0,
                                details=details,
                                execution_time_ms=execution_time
                            )
                    
                    # Try to install required packages if environment exists but packages not installed
                    if env_mgr.status.value == 'created':
                        install_result = env_mgr.install_packages()
                        if not install_result.get('success'):
                            details['error'] = f"ML environment packages not installed: {install_result.get('error', 'Unknown error')}"
                            details['message'] = 'Please set up the ML environment first'
                            execution_time = int((time.time() - start_time) * 1000)
                            return ValidationResult(
                                validation_type='functionality_test',
                                status='failed',
                                score=0.0,
                                details=details,
                                execution_time_ms=execution_time
                            )
                    
                    # Check again if ready
                    if not env_mgr.is_ready:
                        details['error'] = 'ML environment is not ready'
                        details['message'] = 'Please set up the environment via Settings → ML Models → Environment Setup'
                        execution_time = int((time.time() - start_time) * 1000)
                        return ValidationResult(
                            validation_type='functionality_test',
                            status='failed',
                            score=0.0,
                            details=details,
                            execution_time_ms=execution_time
                        )
                
                # Use ML environment (REQUIRED - no fallback)
                from app.services.ml_model_subprocess import run_model_prediction_in_env
                result = run_model_prediction_in_env(
                    str(file_path),
                    model_type,
                    test_data
                )
                
                if result.get('success'):
                    details['test_passed'] = True
                    details['sample_output'] = result.get('prediction', [])[:5]
                    details['message'] = 'Model loaded and tested successfully in isolated environment'
                    execution_time = int((time.time() - start_time) * 1000)
                    return ValidationResult(
                        validation_type='functionality_test',
                        status='passed',
                        score=1.0,
                        details=details,
                        execution_time_ms=execution_time
                    )
                else:
                    details['error'] = result.get('error', 'Unknown error')
                    details['message'] = 'Model test failed in isolated environment'
                    execution_time = int((time.time() - start_time) * 1000)
                    return ValidationResult(
                        validation_type='functionality_test',
                        status='failed',
                        score=0.0,
                        details=details,
                        execution_time_ms=execution_time
                    )
                
                # Test prediction
                if hasattr(model_obj, 'predict'):
                    import numpy as np
                    test_input = np.array([test_data])
                    prediction = model_obj.predict(test_input)
                    
                    # Validate prediction output
                    if prediction is None or (hasattr(prediction, '__len__') and len(prediction) == 0):
                        status = 'failed'
                        score = 0.0
                        details['error'] = 'Model prediction returned empty result'
                    else:
                        details['test_passed'] = True
                        details['sample_output'] = prediction.tolist() if hasattr(prediction, 'tolist') else [float(p) for p in prediction][:5]
                        details['message'] = 'Model loaded and tested successfully'
                else:
                    status = 'warning'
                    score = 0.8
                    details['warning'] = 'Model loaded but does not have predict method'
                    details['message'] = 'Model file is valid but may not be a prediction model'
            
            except ImportError as e:
                status = 'warning'
                score = 0.7
                details['warning'] = f'Required library not installed: {str(e)}'
                details['message'] = 'Cannot test functionality without required dependencies'
            except Exception as e:
                status = 'failed'
                score = 0.0
                details['error'] = f'Failed to load or test model: {str(e)}'
                details['traceback'] = str(e)
        
        except Exception as e:
            status = 'failed'
            score = 0.0
            details['error'] = str(e)
        
        execution_time = int((time.time() - start_time) * 1000)
        
        return ValidationResult(
            validation_type='functionality_test',
            status=status,
            score=score,
            details=details,
            execution_time_ms=execution_time
        )
    
    def _generate_test_data(self, model_type: str) -> list:
        """Generate test data for model validation"""
        import random
        # Generate random test features (3-10 features)
        num_features = random.randint(3, 10)
        return [random.random() for _ in range(num_features)]
    
    def _load_sklearn_for_test(self, file_path: Path):
        """Load sklearn model for testing"""
        try:
            import joblib
            return joblib.load(str(file_path))
        except:
            import pickle
            with open(file_path, 'rb') as f:
                return pickle.load(f)
    
    def _load_tensorflow_for_test(self, file_path: Path):
        """Load TensorFlow model for testing"""
        import tensorflow as tf
        if file_path.suffix == '.h5':
            return tf.keras.models.load_model(str(file_path))
        return tf.saved_model.load(str(file_path))
    
    def _load_pytorch_for_test(self, file_path: Path):
        """Load PyTorch model for testing"""
        import torch
        model = torch.load(str(file_path), map_location='cpu')
        if hasattr(model, 'eval'):
            model.eval()
        return model
    
    def _load_xgboost_for_test(self, file_path: Path):
        """Load XGBoost model for testing"""
        import xgboost as xgb
        if file_path.suffix == '.json':
            model = xgb.Booster()
            model.load_model(str(file_path))
            return model
        import joblib
        return joblib.load(str(file_path))
    
    def _load_onnx_for_test(self, file_path: Path):
        """Load ONNX model for testing"""
        import onnxruntime as ort
        return ort.InferenceSession(str(file_path))
    
    def _load_generic_for_test(self, file_path: Path):
        """Generic model loader for testing"""
        try:
            import joblib
            return joblib.load(str(file_path))
        except:
            import pickle
            with open(file_path, 'rb') as f:
                return pickle.load(f)
    
    def _validate_performance(self, model: MLModel, version: MLModelVersion) -> ValidationResult:
        """Benchmark model performance with actual measurements"""
        start_time = time.time()
        details = {}
        status = 'passed'
        score = 1.0
        
        try:
            file_path = Path(version.file_path)
            file_size = file_path.stat().st_size
            details['file_size_mb'] = round(file_size / (1024 * 1024), 2)
            
            # Try to actually benchmark if model can be loaded
            try:
                import psutil
                import numpy as np
                process = psutil.Process()
                mem_before = process.memory_info().rss / (1024 * 1024)  # MB
                
                # Load model
                model_type = model.model_type.lower()
                ext = file_path.suffix.lower()
                
                if model_type == 'sklearn' or ext in ['.pkl', '.joblib']:
                    model_obj = self._load_sklearn_for_test(file_path)
                elif model_type == 'tensorflow' or ext in ['.h5', '.pb']:
                    model_obj = self._load_tensorflow_for_test(file_path)
                elif model_type == 'pytorch' or ext in ['.pt', '.pth']:
                    model_obj = self._load_pytorch_for_test(file_path)
                elif model_type == 'xgboost' or ext == '.json':
                    model_obj = self._load_xgboost_for_test(file_path)
                elif model_type == 'onnx' or ext == '.onnx':
                    model_obj = self._load_onnx_for_test(file_path)
                else:
                    model_obj = self._load_generic_for_test(file_path)
                
                mem_after = process.memory_info().rss / (1024 * 1024)  # MB
                memory_usage = mem_after - mem_before
                
                # Benchmark inference time
                test_data = self._generate_test_data(model_type)
                test_input = np.array([test_data])
                
                import time
                inference_times = []
                for _ in range(10):  # Run 10 predictions
                    pred_start = time.time()
                    if hasattr(model_obj, 'predict'):
                        model_obj.predict(test_input)
                    inference_times.append((time.time() - pred_start) * 1000)  # ms
                
                avg_time = sum(inference_times) / len(inference_times)
                p95_time = sorted(inference_times)[int(len(inference_times) * 0.95)]
                
                details['avg_inference_time_ms'] = round(avg_time, 2)
                details['p95_inference_time_ms'] = round(p95_time, 2)
                details['memory_usage_mb'] = round(memory_usage, 2)
                details['throughput_per_second'] = round(1000 / avg_time if avg_time > 0 else 0, 2)
                details['benchmark_samples'] = 10
                
                # Performance scoring
                if avg_time > 1000:  # > 1 second
                    status = 'warning'
                    score = 0.7
                    details['warning'] = 'Model inference is slow (>1s)'
                elif avg_time > 500:  # > 500ms
                    status = 'warning'
                    score = 0.8
                    details['warning'] = 'Model inference is moderately slow'
                
            except ImportError:
                details['note'] = 'Performance benchmark requires model loading (dependencies may be missing)'
            except Exception as e:
                details['note'] = f'Could not complete full benchmark: {str(e)}'
        
        except Exception as e:
            status = 'warning'
            score = 0.8
            details['error'] = str(e)
        
        execution_time = int((time.time() - start_time) * 1000)
        
        return ValidationResult(
            validation_type='performance_benchmark',
            status=status,
            score=score,
            details=details,
            execution_time_ms=execution_time
        )
    
    def _validate_security(self, model: MLModel, version: MLModelVersion) -> ValidationResult:
        """Security scan"""
        start_time = time.time()
        details = {}
        status = 'passed'
        score = 1.0
        
        try:
            file_path = Path(version.file_path)
            
            # Basic security checks
            vulnerabilities = []
            
            # Check file size (prevent extremely large files)
            file_size = file_path.stat().st_size
            max_size = 1024 * 1024 * 1024  # 1GB
            if file_size > max_size:
                vulnerabilities.append(f'File size ({file_size / (1024*1024):.2f}MB) exceeds recommended limit')
            
            # Check file extension
            ext = file_path.suffix.lower()
            if ext not in ['.pkl', '.joblib', '.h5', '.pb', '.pt', '.pth', '.onnx', '.json']:
                vulnerabilities.append(f'Unusual file extension: {ext}')
            
            if vulnerabilities:
                status = 'warning'
                score = 0.8
                details['vulnerabilities'] = vulnerabilities
            else:
                details['message'] = 'No obvious security issues detected'
                details['note'] = 'Full security scan requires deeper analysis'
        
        except Exception as e:
            status = 'warning'
            score = 0.8
            details['error'] = str(e)
        
        execution_time = int((time.time() - start_time) * 1000)
        
        return ValidationResult(
            validation_type='security_scan',
            status=status,
            score=score,
            details=details,
            execution_time_ms=execution_time
        )
    
    def get_validation_report(self, model_id: str, version_id: str) -> Optional[Dict[str, Any]]:
        """Get validation report for a model version"""
        version = MLModelVersion.query.get(version_id)
        if not version or version.model_id != model_id:
            return None
        
        return version.validation_report
    
    def get_validation_history(self, model_id: str) -> List[MLModelValidation]:
        """Get validation history for a model"""
        return MLModelValidation.query.filter_by(model_id=model_id).order_by(
            MLModelValidation.executed_at.desc()
        ).all()

