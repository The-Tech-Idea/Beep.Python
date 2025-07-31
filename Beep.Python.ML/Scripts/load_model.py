# Load trained model from file
import pickle
import joblib
import os

# Parameters
model_id = '{model_id}'
file_path = '{file_path}'
load_format = '{load_format}'  # 'pickle', 'joblib', or 'auto'

try:
    # Check if file exists
    if not os.path.exists(file_path):
        print(f"Error: Model file not found at: {file_path}")
        load_model_successful = False
        globals()['load_model_successful'] = load_model_successful
        exit()
    
    # Determine load format
    if load_format == 'auto':
        # Auto-detect from file extension
        if file_path.endswith('.pkl') or file_path.endswith('.pickle'):
            load_format = 'pickle'
        elif file_path.endswith('.joblib'):
            load_format = 'joblib'
        else:
            # Try both formats
            try:
                model = joblib.load(file_path)
                load_format = 'joblib'
            except:
                try:
                    with open(file_path, 'rb') as f:
                        model = pickle.load(f)
                    load_format = 'pickle'
                except:
                    raise ValueError("Could not determine file format. Please specify load_format.")
    
    # Load with specified format (moved outside the nested else)
    if load_format == 'pickle':
        with open(file_path, 'rb') as f:
            model = pickle.load(f)
    elif load_format == 'joblib':
        model = joblib.load(file_path)
    else:
        raise ValueError(f"Unsupported load format: {load_format}")
    
    print(f"Loading model from: {file_path}")
    print(f"Load format: {load_format}")
    print(f"Model type: {type(model).__name__}")
    
    # Store the loaded model
    if 'models' not in globals():
        models = {}
    models[model_id] = model
    globals()['models'] = models
    
    # Try to load metadata if available
    metadata_path = file_path.replace('.pkl', '_metadata.pkl').replace('.joblib', '_metadata.joblib')
    model_metadata = None
    
    if os.path.exists(metadata_path):
        try:
            if load_format == 'pickle':
                with open(metadata_path, 'rb') as f:
                    model_metadata = pickle.load(f)
            else:
                model_metadata = joblib.load(metadata_path)
            
            print(f"Model metadata loaded from: {metadata_path}")
            
            # Restore global variables from metadata
            if isinstance(model_metadata, dict):
                for key, value in model_metadata.items():
                    if key in ['feature_names', 'label_column', 'problem_type', 'n_features']:
                        globals()[key] = value
                
                # Display metadata information
                if 'model_type' in model_metadata:
                    print(f"Original model type: {model_metadata['model_type']}")
                if 'n_features' in model_metadata:
                    print(f"Number of features: {model_metadata['n_features']}")
                if 'problem_type' in model_metadata:
                    print(f"Problem type: {model_metadata['problem_type']}")
                if 'evaluation_results' in model_metadata:
                    eval_results = model_metadata['evaluation_results']
                    if 'accuracy' in eval_results:
                        print(f"Last known accuracy: {eval_results['accuracy']:.4f}")
                    elif 'r2_score' in eval_results:
                        print(f"Last known R² score: {eval_results['r2_score']:.4f}")
            
        except Exception as meta_error:
            print(f"Warning: Could not load metadata: {str(meta_error)}")
    
    # Get model information
    model_info = {
        'model_id': model_id,
        'file_path': file_path,
        'load_format': load_format,
        'model_type': type(model).__name__,
        'model_module': type(model).__module__,
        'file_size': os.path.getsize(file_path),
        'metadata': model_metadata
    }
    
    # Check if model has common attributes
    if hasattr(model, 'feature_importances_'):
        model_info['has_feature_importances'] = True
        print("? Model has feature importances")
    
    if hasattr(model, 'predict_proba'):
        model_info['has_predict_proba'] = True
        print("? Model supports probability predictions")
    
    if hasattr(model, 'classes_'):
        model_info['classes'] = model.classes_.tolist()
        print(f"? Model classes: {model.classes_}")
    
    globals()['load_info'] = model_info
    
    print(f"Model '{model_id}' loaded successfully!")
    load_model_successful = True
    
except Exception as e:
    print(f"Error loading model: {str(e)}")
    load_model_successful = False

# Store the result
globals()['load_model_successful'] = load_model_successful