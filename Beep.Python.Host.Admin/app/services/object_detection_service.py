"""
Object Detection Service

Detects objects in images using various models (YOLO, DETR, etc.)
"""
import os
import sys
import subprocess
import base64
import io
import json
import logging
import tempfile
from pathlib import Path
from typing import Dict, Any, Optional, List
from datetime import datetime

logger = logging.getLogger(__name__)


class ObjectDetectionService:
    """Service for object detection in images"""
    
    def __init__(self):
        from app.services.ai_services_environment import AIServiceType, get_ai_service_env
        
        self.service_type = AIServiceType.OBJECT_DETECTION
        self.env_mgr = get_ai_service_env(self.service_type)
        logger.info(f"Initialized ObjectDetectionService with environment manager")
    
    def _get_python_executable(self) -> Optional[Path]:
        """Get Python executable from virtual environment"""
        try:
            python_path = self.env_mgr._get_python_path()
            if python_path and python_path.exists():
                logger.info(f"Using Python from virtual environment: {python_path}")
                return python_path
            else:
                logger.warning("Virtual environment Python not found, using system Python")
                return None
        except Exception as e:
            logger.error(f"Error getting Python executable: {e}")
            return None
    
    def detect_objects(self, image_data: bytes, 
                      model_id: Optional[str] = None,
                      confidence_threshold: float = 0.5,
                      max_detections: int = 100) -> Dict[str, Any]:
        """
        Detect objects in an image
        
        Args:
            image_data: Image bytes
            model_id: HuggingFace model ID (e.g., "facebook/detr-resnet-50", "Ultralytics/YOLOv8")
            confidence_threshold: Minimum confidence score (0.0-1.0)
            max_detections: Maximum number of detections to return
        
        Returns:
            Dict with 'success', 'detections' (list of detected objects), 'metadata', 'error'
        """
        try:
            # Get Python executable
            python_exe = self._get_python_executable()
            if not python_exe:
                return {
                    'success': False,
                    'error': 'Python environment not available. Please create the environment first.'
                }
            
            # Get selected model if not provided
            if not model_id:
                from app.services.ai_service_models import AIServiceModelConfig
                model_id = AIServiceModelConfig.get_selected_model('object_detection')
                if not model_id:
                    # Default to a popular model
                    model_id = "facebook/detr-resnet-50"
            
            # Track loaded model (only one per service type)
            from app.services.ai_service_loaded_models import get_ai_service_loaded_tracker
            tracker = get_ai_service_loaded_tracker()
            previous_model = tracker.load_model('object_detection', model_id)
            if previous_model and previous_model != model_id:
                logger.info(f"[ObjectDetection] Switched from {previous_model} to {model_id}")
            
            tracker.update_last_used('object_detection')
            
            # Use subprocess to run in dedicated environment
            return self._detect_via_subprocess(
                image_data, model_id, confidence_threshold, max_detections
            )
            
        except Exception as e:
            logger.error(f"Error in detect_objects: {e}", exc_info=True)
            return {
                'success': False,
                'error': str(e)
            }
    
    def _detect_via_subprocess(self, image_data: bytes,
                              model_id: str,
                              confidence_threshold: float,
                              max_detections: int) -> Dict[str, Any]:
        """Run object detection in subprocess"""
        try:
            python_exe = self._get_python_executable()
            if not python_exe:
                return {
                    'success': False,
                    'error': 'Python executable not found'
                }
            
            # Create temporary script
            script_content = self._get_detection_script()
            script_path = Path(tempfile.gettempdir()) / f"detect_objects_{datetime.now().timestamp()}.py"
            
            try:
                script_path.write_text(script_content, encoding='utf-8')
                
                # Encode image as base64
                img_base64 = base64.b64encode(image_data).decode('utf-8')
                
                # Run detection
                logger.info(f"Starting object detection: model={model_id}, threshold={confidence_threshold}")
                logger.info(f"Using Python from virtual environment: {python_exe}")
                
                result = subprocess.run(
                    [str(python_exe), str(script_path),
                     img_base64, model_id, str(confidence_threshold), str(max_detections)],
                    capture_output=True,
                    text=True,
                    timeout=300  # 5 minute timeout
                )
                
                logger.info(f"Detection completed: returncode={result.returncode}")
                
                if result.returncode == 0:
                    output = result.stdout.strip()
                    try:
                        return json.loads(output)
                    except json.JSONDecodeError:
                        return {
                            'success': False,
                            'error': 'Failed to parse detection result'
                        }
                else:
                    error_msg = result.stderr or result.stdout or 'Unknown error'
                    logger.error(f"Detection failed: {error_msg}")
                    return {
                        'success': False,
                        'error': error_msg[:500]  # Limit error message length
                    }
                    
            finally:
                # Clean up script
                if script_path.exists():
                    try:
                        script_path.unlink()
                    except:
                        pass
                        
        except subprocess.TimeoutExpired:
            logger.error("Object detection timed out")
            return {
                'success': False,
                'error': 'Detection timed out after 5 minutes'
            }
        except Exception as e:
            logger.error(f"Error in _detect_via_subprocess: {e}", exc_info=True)
            return {
                'success': False,
                'error': str(e)
            }
    
    def _get_detection_script(self) -> str:
        """Get Python script for object detection"""
        return '''import sys
import json
import base64
import io
from pathlib import Path

# Get arguments
img_base64 = sys.argv[1]
model_id = sys.argv[2]
confidence_threshold = float(sys.argv[3])
max_detections = int(sys.argv[4])

try:
    from transformers import pipeline, AutoImageProcessor, AutoModelForObjectDetection
    from PIL import Image
    import torch
    
    # Decode image
    img_data = base64.b64decode(img_base64)
    image = Image.open(io.BytesIO(img_data))
    
    # Check if CUDA is available
    device = "cuda" if torch.cuda.is_available() else "cpu"
    
    # Load model and processor
    try:
        # Try to use pipeline first (simpler API)
        detector = pipeline(
            "object-detection",
            model=model_id,
            device=0 if device == "cuda" else -1
        )
        
        # Run detection
        results = detector(image)
        
        # Filter by confidence and limit results
        detections = []
        for result in results:
            if result['score'] >= confidence_threshold:
                detections.append({
                    'label': result['label'],
                    'score': float(result['score']),
                    'box': {
                        'xmin': float(result['box']['xmin']),
                        'ymin': float(result['box']['ymin']),
                        'xmax': float(result['box']['xmax']),
                        'ymax': float(result['box']['ymax'])
                    }
                })
                if len(detections) >= max_detections:
                    break
        
        # Sort by confidence (highest first)
        detections.sort(key=lambda x: x['score'], reverse=True)
        
    except Exception as e:
        # Fallback: use model directly
        processor = AutoImageProcessor.from_pretrained(model_id)
        model = AutoModelForObjectDetection.from_pretrained(model_id)
        model.to(device)
        model.eval()
        
        # Process image
        inputs = processor(images=image, return_tensors="pt")
        inputs = {k: v.to(device) for k, v in inputs.items()}
        
        # Run inference
        with torch.no_grad():
            outputs = model(**inputs)
        
        # Process results
        target_sizes = torch.tensor([image.size[::-1]]).to(device)
        results = processor.post_process_object_detection(
            outputs, 
            target_sizes=target_sizes,
            threshold=confidence_threshold
        )[0]
        
        detections = []
        for score, label, box in zip(results["scores"], results["labels"], results["boxes"]):
            if len(detections) >= max_detections:
                break
            detections.append({
                'label': model.config.id2label[label.item()],
                'score': float(score.item()),
                'box': {
                    'xmin': float(box[0].item()),
                    'ymin': float(box[1].item()),
                    'xmax': float(box[2].item()),
                    'ymax': float(box[3].item())
                }
            })
        
        # Sort by confidence
        detections.sort(key=lambda x: x['score'], reverse=True)
    
    result = {
        "success": True,
        "detections": detections,
        "metadata": {
            "model": model_id,
            "device": device,
            "confidence_threshold": confidence_threshold,
            "total_detections": len(detections)
        }
    }
    print(json.dumps(result))
    
except Exception as e:
    import traceback
    error_result = {
        "success": False,
        "error": str(e),
        "traceback": traceback.format_exc()[:500]
    }
    print(json.dumps(error_result))
'''


# Singleton instance
_object_detection_service = None

def get_object_detection_service() -> ObjectDetectionService:
    """Get singleton instance of ObjectDetectionService"""
    global _object_detection_service
    if _object_detection_service is None:
        _object_detection_service = ObjectDetectionService()
    return _object_detection_service
