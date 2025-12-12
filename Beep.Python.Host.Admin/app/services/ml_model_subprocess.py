"""
ML Model Subprocess Executor

Runs model predictions in isolated ML environment using subprocess.
Similar to RAG subprocess executor pattern.
"""
import json
import subprocess
import sys
from pathlib import Path
from typing import Dict, Any, Optional
from app.services.ml_model_environment import get_ml_model_environment_manager


def run_model_prediction_in_env(model_path: str, model_type: str, input_data: dict) -> Dict[str, Any]:
    """
    Run model prediction in isolated ML environment
    
    Args:
        model_path: Path to model file
        model_type: Type of model (sklearn, tensorflow, pytorch, etc.)
        input_data: Input data for prediction
    
    Returns:
        Prediction result dict
    """
    env_mgr = get_ml_model_environment_manager()
    
    if not env_mgr.is_ready:
        return {
            'success': False,
            'error': 'ML environment not ready. Please run environment setup first.',
            'prediction': None
        }
    
    python_path = env_mgr.get_python_executable()
    if not python_path:
        return {
            'success': False,
            'error': 'Python executable not found in ML environment',
            'prediction': None
        }
    
    # Create prediction script
    script = f"""
import sys
import json
import traceback
from pathlib import Path

try:
    model_path = r"{model_path}"
    model_type = "{model_type}"
    input_data = {json.dumps(input_data)}
    
    # Load model based on type
    if model_type == 'sklearn':
        try:
            import joblib
            model = joblib.load(model_path)
        except:
            import pickle
            with open(model_path, 'rb') as f:
                model = pickle.load(f)
    elif model_type == 'tensorflow':
        import tensorflow as tf
        if Path(model_path).suffix == '.h5':
            model = tf.keras.models.load_model(model_path)
        else:
            model = tf.saved_model.load(model_path)
    elif model_type == 'pytorch':
        import torch
        model = torch.load(model_path, map_location='cpu')
        if hasattr(model, 'eval'):
            model.eval()
    elif model_type == 'xgboost':
        import xgboost as xgb
        if Path(model_path).suffix == '.json':
            model = xgb.Booster()
            model.load_model(model_path)
        else:
            import joblib
            model = joblib.load(model_path)
    elif model_type == 'onnx':
        import onnxruntime as ort
        model = ort.InferenceSession(model_path)
    else:
        # Generic
        try:
            import joblib
            model = joblib.load(model_path)
        except:
            import pickle
            with open(model_path, 'rb') as f:
                model = pickle.load(f)
    
    # Prepare input
    import numpy as np
    if isinstance(input_data, dict):
        features = np.array([list(input_data.values())])
    else:
        features = np.array([input_data]) if isinstance(input_data, list) else np.array(input_data)
    
    # Predict
    if model_type == 'sklearn' or hasattr(model, 'predict'):
        prediction = model.predict(features)
        if hasattr(model, 'predict_proba'):
            try:
                probabilities = model.predict_proba(features)[0].tolist()
                result = {{'prediction': probabilities, 'type': 'probabilities'}}
            except:
                result = {{'prediction': prediction.tolist() if hasattr(prediction, 'tolist') else [float(p) for p in prediction], 'type': 'prediction'}}
        else:
            result = {{'prediction': prediction.tolist() if hasattr(prediction, 'tolist') else [float(p) for p in prediction], 'type': 'prediction'}}
    elif model_type == 'tensorflow':
        if hasattr(model, 'predict'):
            prediction = model.predict(features, verbose=0)
        else:
            prediction = model(features)
            if hasattr(prediction, 'numpy'):
                prediction = prediction.numpy()
        result = {{'prediction': prediction[0].tolist() if hasattr(prediction, 'tolist') else [float(p) for p in prediction[0]], 'type': 'prediction'}}
    elif model_type == 'pytorch':
        import torch
        input_tensor = torch.from_numpy(features.astype(np.float32))
        with torch.no_grad():
            prediction = model(input_tensor)
        if hasattr(prediction, 'numpy'):
            pred_list = prediction[0].numpy().tolist()
        elif hasattr(prediction, 'tolist'):
            pred_list = prediction[0].tolist()
        else:
            pred_list = [float(p) for p in prediction[0]]
        result = {{'prediction': pred_list, 'type': 'prediction'}}
    elif model_type == 'xgboost':
        import xgboost as xgb
        if isinstance(model, xgb.Booster):
            dmatrix = xgb.DMatrix(features)
            prediction = model.predict(dmatrix)
        else:
            prediction = model.predict(features)
        if hasattr(model, 'predict_proba'):
            try:
                probabilities = model.predict_proba(features)[0].tolist()
                result = {{'prediction': probabilities, 'type': 'probabilities'}}
            except:
                result = {{'prediction': prediction.tolist() if hasattr(prediction, 'tolist') else [float(p) for p in prediction], 'type': 'prediction'}}
        else:
            result = {{'prediction': prediction.tolist() if hasattr(prediction, 'tolist') else [float(p) for p in prediction], 'type': 'prediction'}}
    elif model_type == 'onnx':
        input_name = model.get_inputs()[0].name
        prediction = model.run(None, {{input_name: features.astype(np.float32)}})[0]
        result = {{'prediction': prediction[0].tolist(), 'type': 'prediction'}}
    else:
        if hasattr(model, 'predict'):
            prediction = model.predict(features)
            result = {{'prediction': prediction.tolist() if hasattr(prediction, 'tolist') else [float(p) for p in prediction], 'type': 'prediction'}}
        else:
            raise ValueError("Model does not have a predict method")
    
    print(json.dumps({{'success': True, **result}}))
    
except ImportError as e:
    print(json.dumps({{'success': False, 'error': f'Missing dependency: {{str(e)}}'}}))
    sys.exit(1)
except Exception as e:
    error_msg = str(e)
    traceback_str = traceback.format_exc()
    print(json.dumps({{'success': False, 'error': error_msg, 'traceback': traceback_str}}))
    sys.exit(1)
"""
    
    try:
        # Run script in ML environment
        result = subprocess.run(
            [python_path, '-c', script],
            capture_output=True,
            text=True,
            timeout=300  # 5 minute timeout
        )
        
        if result.returncode != 0:
            return {
                'success': False,
                'error': result.stderr or 'Prediction failed',
                'prediction': None
            }
        
        # Parse output
        try:
            output = json.loads(result.stdout)
            return output
        except json.JSONDecodeError:
            return {
                'success': False,
                'error': f'Invalid output from prediction: {result.stdout[:200]}',
                'prediction': None
            }
    
    except subprocess.TimeoutExpired:
        return {
            'success': False,
            'error': 'Prediction timeout (exceeded 5 minutes)',
            'prediction': None
        }
    except Exception as e:
        return {
            'success': False,
            'error': str(e),
            'prediction': None
        }

