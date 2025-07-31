# Evaluate machine learning model
import pandas as pd
import numpy as np
from sklearn.metrics import (accuracy_score, precision_score, recall_score, f1_score,
                           classification_report, confusion_matrix, roc_auc_score,
                           mean_squared_error, mean_absolute_error, r2_score)

# Parameters
model_id = '{model_id}'
use_test_data = {use_test_data}

try:
    # Check if models dictionary exists and contains the specified model
    if 'models' not in globals() or model_id not in models:
        print(f"Error: Model '{model_id}' not found in models dictionary")
        evaluate_model_successful = False
        globals()['evaluate_model_successful'] = evaluate_model_successful
        exit()
    
    model = models[model_id]
    
    # Determine which data to use for evaluation
    if use_test_data and 'test_data' in globals() and 'label_column' in globals():
        # Use test data
        feature_columns = [col for col in test_data.columns if col != label_column]
        X_eval = test_data[feature_columns]
        y_true = test_data[label_column]
        eval_type = "Test"
        
    elif use_test_data and 'X_test' in globals() and 'y_test' in globals():
        # Use existing test split
        X_eval = X_test
        y_true = y_test
        eval_type = "Test"
        
    elif 'train_data' in globals() and 'label_column' in globals():
        # Use training data for evaluation
        feature_columns = [col for col in train_data.columns if col != label_column]
        X_eval = train_data[feature_columns]
        y_true = train_data[label_column]
        eval_type = "Training"
        
    elif 'X_train_processed' in globals() and 'y_train' in globals():
        # Use processed training data
        X_eval = X_train_processed
        y_true = y_train
        eval_type = "Training"
        
    else:
        print("Error: No suitable data available for model evaluation")
        evaluate_model_successful = False
        globals()['evaluate_model_successful'] = evaluate_model_successful
        exit()
    
    # Process evaluation data (handle categorical features)
    if not isinstance(X_eval, pd.DataFrame) or 'X_train_processed' not in globals():
        X_eval_processed = pd.get_dummies(X_eval)
        X_eval_processed = X_eval_processed.fillna(X_eval_processed.mean())
    else:
        # Align with training data columns if available
        if 'X_train_processed' in globals():
            # Ensure test data has same columns as training data
            train_columns = X_train_processed.columns
            X_eval_processed = pd.get_dummies(X_eval)
            
            # Add missing columns
            for col in train_columns:
                if col not in X_eval_processed.columns:
                    X_eval_processed[col] = 0
            
            # Remove extra columns and reorder
            X_eval_processed = X_eval_processed[train_columns]
            X_eval_processed = X_eval_processed.fillna(X_eval_processed.mean())
        else:
            X_eval_processed = pd.get_dummies(X_eval)
            X_eval_processed = X_eval_processed.fillna(X_eval_processed.mean())
    
    # Make predictions
    y_pred = model.predict(X_eval_processed)
    
    # Get probabilities if available
    y_proba = None
    if hasattr(model, 'predict_proba'):
        y_proba = model.predict_proba(X_eval_processed)
    elif hasattr(model, 'decision_function'):
        y_proba = model.decision_function(X_eval_processed)
    
    # Determine problem type and calculate appropriate metrics
    problem_type = globals().get('problem_type', 'classification')
    
    # Try to auto-detect if not available
    if problem_type not in ['classification', 'regression']:
        unique_targets = len(np.unique(y_true))
        problem_type = 'classification' if unique_targets <= 50 else 'regression'
    
    evaluation_results = {
        'model_id': model_id,
        'eval_type': eval_type,
        'problem_type': problem_type,
        'n_samples': len(y_true),
        'predictions': y_pred.tolist()
    }
    
    print(f"\n=== {eval_type} Evaluation Results for Model '{model_id}' ===")
    print(f"Problem Type: {problem_type}")
    print(f"Evaluation Samples: {len(y_true)}")
    
    if problem_type == 'classification':
        # Classification metrics
        accuracy = accuracy_score(y_true, y_pred)
        
        try:
            precision = precision_score(y_true, y_pred, average='weighted')
            recall = recall_score(y_true, y_pred, average='weighted')
            f1 = f1_score(y_true, y_pred, average='weighted')
        except:
            precision = recall = f1 = 0.0
        
        evaluation_results.update({
            'accuracy': accuracy,
            'precision': precision,
            'recall': recall,
            'f1_score': f1
        })
        
        print(f"Accuracy: {accuracy:.4f}")
        print(f"Precision: {precision:.4f}")
        print(f"Recall: {recall:.4f}")
        print(f"F1-Score: {f1:.4f}")
        
        # Confusion Matrix
        cm = confusion_matrix(y_true, y_pred)
        evaluation_results['confusion_matrix'] = cm.tolist()
        
        print(f"\nConfusion Matrix:")
        print(cm)
        
        # ROC AUC for binary classification
        if y_proba is not None and len(np.unique(y_true)) == 2:
            try:
                if hasattr(model, 'predict_proba') and y_proba.shape[1] == 2:
                    roc_auc = roc_auc_score(y_true, y_proba[:, 1])
                else:
                    roc_auc = roc_auc_score(y_true, y_proba)
                evaluation_results['roc_auc'] = roc_auc
                print(f"ROC AUC: {roc_auc:.4f}")
            except:
                print("ROC AUC: Could not calculate")
        
        # Classification Report
        try:
            class_report = classification_report(y_true, y_pred, output_dict=True)
            evaluation_results['classification_report'] = class_report
            print(f"\nDetailed Classification Report:")
            print(classification_report(y_true, y_pred))
        except Exception as e:
            print(f"Could not generate classification report: {str(e)}")
    
    else:
        # Regression metrics
        mse = mean_squared_error(y_true, y_pred)
        rmse = np.sqrt(mse)
        mae = mean_absolute_error(y_true, y_pred)
        r2 = r2_score(y_true, y_pred)
        
        evaluation_results.update({
            'mse': mse,
            'rmse': rmse,
            'mae': mae,
            'r2_score': r2
        })
        
        print(f"Mean Squared Error (MSE): {mse:.4f}")
        print(f"Root Mean Squared Error (RMSE): {rmse:.4f}")
        print(f"Mean Absolute Error (MAE): {mae:.4f}")
        print(f"R² Score: {r2:.4f}")
        
        # Additional regression metrics
        if len(y_true) > 0:
            mean_target = np.mean(y_true)
            evaluation_results['mean_target'] = mean_target
            
            # Percentage errors
            mape = np.mean(np.abs((y_true - y_pred) / y_true)) * 100
            evaluation_results['mape'] = mape
            print(f"Mean Absolute Percentage Error (MAPE): {mape:.2f}%")
    
    # Store predictions and true values for further analysis
    evaluation_results.update({
        'y_true': y_true.tolist() if hasattr(y_true, 'tolist') else y_true,
        'y_pred': y_pred.tolist() if hasattr(y_pred, 'tolist') else y_pred
    })
    
    if y_proba is not None:
        if hasattr(y_proba, 'tolist'):
            evaluation_results['y_proba'] = y_proba.tolist()
        else:
            evaluation_results['y_proba'] = y_proba
    
    # Store results in global scope
    globals()['evaluation_results'] = evaluation_results
    globals()['y_pred'] = y_pred
    globals()['y_true'] = y_true
    
    if y_proba is not None:
        globals()['y_proba'] = y_proba
    
    print(f"\nEvaluation completed successfully!")
    evaluate_model_successful = True
    
except Exception as e:
    print(f"Error during model evaluation: {str(e)}")
    evaluate_model_successful = False

# Store the result
globals()['evaluate_model_successful'] = evaluate_model_successful