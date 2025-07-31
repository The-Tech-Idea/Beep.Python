# Make predictions using trained model
import pandas as pd
import numpy as np

# Parameters
model_id = '{model_id}'
data_source = '{data_source}'  # 'test_data', 'new_data', or 'file'
file_path = '{file_path}'
feature_columns = {feature_columns}

try:
    # Check if models dictionary exists and contains the specified model
    if 'models' not in globals() or model_id not in models:
        print(f"Error: Model '{model_id}' not found in models dictionary")
        predict_model_successful = False
        globals()['predict_model_successful'] = predict_model_successful
        exit()
    
    model = models[model_id]
    
    # Determine data source and prepare prediction data
    if data_source == 'test_data' and 'test_data' in globals():
        # Use existing test data
        if 'label_column' in globals():
            predict_features = [col for col in test_data.columns if col != label_column]
        else:
            predict_features = feature_columns if feature_columns else test_data.columns.tolist()
        
        X_predict = test_data[predict_features]
        data_info = f"test_data ({len(X_predict)} samples)"
        
    elif data_source == 'new_data' and 'predict_data' in globals():
        # Use predict_data DataFrame
        predict_features = feature_columns if feature_columns else predict_data.columns.tolist()
        X_predict = predict_data[predict_features]
        data_info = f"predict_data ({len(X_predict)} samples)"
        
    elif data_source == 'file' and file_path and file_path != 'None':
        # Load data from file
        try:
            file_data = pd.read_csv(file_path)
            predict_features = feature_columns if feature_columns else file_data.columns.tolist()
            X_predict = file_data[predict_features]
            data_info = f"file: {file_path} ({len(X_predict)} samples)"
            
            # Store loaded data for reference
            globals()['predict_data'] = file_data
            
        except FileNotFoundError:
            print(f"Error: File not found at: {file_path}")
            predict_model_successful = False
            globals()['predict_model_successful'] = predict_model_successful
            exit()
        except Exception as e:
            print(f"Error loading file: {str(e)}")
            predict_model_successful = False
            globals()['predict_model_successful'] = predict_model_successful
            exit()
            
    elif 'data' in globals():
        # Fallback to main data DataFrame
        if 'label_column' in globals() and label_column in data.columns:
            predict_features = [col for col in data.columns if col != label_column]
        else:
            predict_features = feature_columns if feature_columns else data.columns.tolist()
        
        X_predict = data[predict_features]
        data_info = f"data ({len(X_predict)} samples)"
        
    else:
        print("Error: No suitable data available for prediction")
        predict_model_successful = False
        globals()['predict_model_successful'] = predict_model_successful
        exit()
    
    # Process prediction data (handle categorical features)
    X_predict_processed = pd.get_dummies(X_predict)
    
    # Align with training data columns if available
    if 'X_train_processed' in globals():
        train_columns = X_train_processed.columns
        
        # Add missing columns with zero values
        for col in train_columns:
            if col not in X_predict_processed.columns:
                X_predict_processed[col] = 0
        
        # Remove extra columns and reorder to match training data
        X_predict_processed = X_predict_processed[train_columns]
    
    # Fill missing values
    X_predict_processed = X_predict_processed.fillna(X_predict_processed.mean())
    
    print(f"Making predictions using model '{model_id}'")
    print(f"Data source: {data_info}")
    print(f"Features: {X_predict_processed.shape[1]}")
    print(f"Samples: {X_predict_processed.shape[0]}")
    
    # Make predictions
    predictions = model.predict(X_predict_processed)
    
    # Get prediction probabilities if available
    prediction_probabilities = None
    if hasattr(model, 'predict_proba'):
        prediction_probabilities = model.predict_proba(X_predict_processed)
        print(f"Prediction probabilities available: Yes")
    elif hasattr(model, 'decision_function'):
        prediction_probabilities = model.decision_function(X_predict_processed)
        print(f"Decision function scores available: Yes")
    else:
        print(f"Prediction probabilities available: No")
    
    # Store predictions
    globals()['predictions'] = predictions
    globals()['X_predict'] = X_predict
    globals()['X_predict_processed'] = X_predict_processed
    
    if prediction_probabilities is not None:
        globals()['prediction_probabilities'] = prediction_probabilities
    
    # Create prediction results DataFrame
    prediction_results = X_predict.copy()
    prediction_results['prediction'] = predictions
    
    if prediction_probabilities is not None:
        if hasattr(model, 'predict_proba') and prediction_probabilities.shape[1] > 1:
            # Multi-class probabilities
            classes = model.classes_ if hasattr(model, 'classes_') else range(prediction_probabilities.shape[1])
            for i, class_label in enumerate(classes):
                prediction_results[f'probability_class_{class_label}'] = prediction_probabilities[:, i]
        else:
            # Single probability or decision function
            prediction_results['prediction_score'] = prediction_probabilities.flatten() if hasattr(prediction_probabilities, 'flatten') else prediction_probabilities
    
    globals()['prediction_results'] = prediction_results
    
    # Display prediction summary
    print(f"\nPrediction Summary:")
    if hasattr(predictions, 'dtype') and predictions.dtype in ['int64', 'int32', 'object']:
        # Classification predictions
        unique_preds, counts = np.unique(predictions, return_counts=True)
        for pred, count in zip(unique_preds, counts):
            percentage = (count / len(predictions)) * 100
            print(f"  Class {pred}: {count} samples ({percentage:.1f}%)")
    else:
        # Regression predictions
        print(f"  Mean prediction: {np.mean(predictions):.4f}")
        print(f"  Std deviation: {np.std(predictions):.4f}")
        print(f"  Min prediction: {np.min(predictions):.4f}")
        print(f"  Max prediction: {np.max(predictions):.4f}")
    
    print(f"\nPredictions completed successfully!")
    print(f"Results stored in 'predictions' and 'prediction_results' variables")
    
    predict_model_successful = True
    
except Exception as e:
    print(f"Error during prediction: {str(e)}")
    predict_model_successful = False

# Store the result
globals()['predict_model_successful'] = predict_model_successful