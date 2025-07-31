# Save trained model to file
import pickle
import joblib
import os

# Parameters
model_id = '{model_id}'
file_path = '{file_path}'
save_format = '{save_format}'  # 'pickle', 'joblib', or 'auto'

try:
    # Check if models dictionary exists and contains the specified model
    if 'models' not in globals() or model_id not in models:
        print(f"Error: Model '{model_id}' not found in models dictionary")
        save_model_successful = False
        globals()['save_model_successful'] = save_model_successful
        exit()
    
    model = models[model_id]
    
    # Create directory if it doesn't exist
    output_dir = os.path.dirname(file_path)
    if output_dir and not os.path.exists(output_dir):
        os.makedirs(output_dir)
        print(f"Created directory: {output_dir}")
    
    # Determine save format
    if save_format == 'auto':
        # Auto-detect from file extension
        if file_path.endswith('.pkl') or file_path.endswith('.pickle'):
            save_format = 'pickle'
        elif file_path.endswith('.joblib'):
            save_format = 'joblib'
        else:
            save_format = 'joblib'  # Default to joblib
            if not file_path.endswith('.joblib'):
                file_path += '.joblib'
    
    print(f"Saving model '{model_id}' to: {file_path}")
    print(f"Save format: {save_format}")
    
    # Save the model
    if save_format == 'pickle':
        with open(file_path, 'wb') as f:
            pickle.dump(model, f)
    elif save_format == 'joblib':
        joblib.dump(model, file_path)
    else:
        raise ValueError(f"Unsupported save format: {save_format}")
    
    # Save additional model metadata if available
    metadata_path = file_path.replace('.pkl', '_metadata.pkl').replace('.joblib', '_metadata.joblib')
    
    model_metadata = {
        'model_id': model_id,
        'model_type': type(model).__name__,
        'model_module': type(model).__module__
    }
    
    # Add training results if available
    if 'training_results' in globals():
        model_metadata.update(globals()['training_results'])
    
    # Add evaluation results if available
    if 'evaluation_results' in globals():
        model_metadata['evaluation_results'] = globals()['evaluation_results']
    
    # Add feature information if available
    if 'X_train_processed' in globals():
        model_metadata['feature_names'] = X_train_processed.columns.tolist()
        model_metadata['n_features'] = len(X_train_processed.columns)
    
    if 'label_column' in globals():
        model_metadata['label_column'] = label_column
    
    if 'problem_type' in globals():
        model_metadata['problem_type'] = problem_type
    
    # Save metadata
    try:
        if save_format == 'pickle':
            with open(metadata_path, 'wb') as f:
                pickle.dump(model_metadata, f)
        else:
            joblib.dump(model_metadata, metadata_path)
        print(f"Model metadata saved to: {metadata_path}")
    except Exception as meta_error:
        print(f"Warning: Could not save metadata: {str(meta_error)}")
    
    # Verify the saved file
    file_size = os.path.getsize(file_path)
    print(f"Model saved successfully!")
    print(f"File size: {file_size / 1024:.2f} KB")
    
    # Store save information
    save_info = {
        'model_id': model_id,
        'file_path': file_path,
        'save_format': save_format,
        'file_size': file_size,
        'metadata_path': metadata_path if os.path.exists(metadata_path) else None
    }
    
    globals()['save_info'] = save_info
    save_model_successful = True
    
except Exception as e:
    print(f"Error saving model: {str(e)}")
    save_model_successful = False

# Store the result
globals()['save_model_successful'] = save_model_successful