# Comprehensive model evaluation with detailed metrics and visualizations
from sklearn.metrics import (classification_report, confusion_matrix, roc_curve, auc, 
                            precision_recall_curve, average_precision_score, accuracy_score,
                            mean_squared_error, mean_absolute_error, r2_score)
from sklearn.model_selection import learning_curve, validation_curve
import matplotlib.pyplot as plt
import seaborn as sns
import pandas as pd
import numpy as np
import os

# Parameters
model_id = '{model_id}'
output_dir = '{output_dir}'
problem_type = '{problem_type}'  # 'classification' or 'regression'

try:
    # Check if models dictionary exists and contains the specified model
    if 'models' not in globals() or model_id not in models:
        print(f"Error: Model '{model_id}' not found in models dictionary")
        comprehensive_evaluation_successful = False
        globals()['comprehensive_evaluation_successful'] = comprehensive_evaluation_successful
        exit()
    
    model = models[model_id]
    
    # Prepare data
    if 'test_data' in globals() and 'label_column' in globals():
        # Use test data
        feature_columns = [col for col in test_data.columns if col != label_column]
        X_test = test_data[feature_columns]
        y_true = test_data[label_column]
        
        # Handle categorical features
        X_test_processed = pd.get_dummies(X_test)
        X_test_processed = X_test_processed.fillna(X_test_processed.mean())
        
    elif 'X_test' in globals() and 'y_test' in globals():
        # Use existing test data
        X_test_processed = X_test
        y_true = y_test
        
    else:
        print("Error: No test data available for comprehensive evaluation")
        comprehensive_evaluation_successful = False
        globals()['comprehensive_evaluation_successful'] = comprehensive_evaluation_successful
        exit()
    
    # Create output directory
    if output_dir and output_dir != 'None':
        os.makedirs(output_dir, exist_ok=True)
    else:
        output_dir = '.'
    
    # Make predictions
    y_pred = model.predict(X_test_processed)
    
    # Get probabilities if available (for classification)
    y_score = None
    if problem_type == 'classification' and hasattr(model, 'predict_proba'):
        y_proba = model.predict_proba(X_test_processed)
        if y_proba.shape[1] == 2:  # Binary classification
            y_score = y_proba[:, 1]
    elif problem_type == 'classification' and hasattr(model, 'decision_function'):
        y_score = model.decision_function(X_test_processed)
    
    # Initialize evaluation results
    evaluation_results = {
        'model_id': model_id,
        'problem_type': problem_type,
        'test_samples': len(y_true),
        'predictions': y_pred.tolist() if hasattr(y_pred, 'tolist') else y_pred,
        'true_values': y_true.tolist() if hasattr(y_true, 'tolist') else y_true
    }
    
    # Classification metrics
    if problem_type == 'classification':
        accuracy = accuracy_score(y_true, y_pred)
        evaluation_results['accuracy'] = accuracy
        
        # Classification report
        class_report = classification_report(y_true, y_pred, output_dict=True)
        evaluation_results['classification_report'] = class_report
        
        # Confusion matrix
        cm = confusion_matrix(y_true, y_pred)
        evaluation_results['confusion_matrix'] = cm.tolist()
        
        # Plot confusion matrix
        plt.figure(figsize=(8, 6))
        sns.heatmap(cm, annot=True, fmt='d', cmap='Blues')
        plt.title(f'Confusion Matrix - {model_id}')
        plt.ylabel('True Label')
        plt.xlabel('Predicted Label')
        cm_path = os.path.join(output_dir, f'{model_id}_confusion_matrix.png')
        plt.savefig(cm_path, dpi=300, bbox_inches='tight')
        plt.close()
        evaluation_results['confusion_matrix_plot'] = cm_path
        
        # ROC curve and AUC (if probabilities available)
        if y_score is not None:
            fpr, tpr, _ = roc_curve(y_true, y_score)
            roc_auc = auc(fpr, tpr)
            evaluation_results['roc_auc'] = roc_auc
            
            # Plot ROC curve
            plt.figure(figsize=(8, 6))
            plt.plot(fpr, tpr, color='darkorange', lw=2, label=f'ROC curve (AUC = {roc_auc:.2f})')
            plt.plot([0, 1], [0, 1], color='navy', lw=2, linestyle='--')
            plt.xlim([0.0, 1.0])
            plt.ylim([0.0, 1.05])
            plt.xlabel('False Positive Rate')
            plt.ylabel('True Positive Rate')
            plt.title(f'ROC Curve - {model_id}')
            plt.legend(loc="lower right")
            plt.grid(True, alpha=0.3)
            roc_path = os.path.join(output_dir, f'{model_id}_roc_curve.png')
            plt.savefig(roc_path, dpi=300, bbox_inches='tight')
            plt.close()
            evaluation_results['roc_curve_plot'] = roc_path
            
            # Precision-Recall curve
            precision, recall, _ = precision_recall_curve(y_true, y_score)
            avg_precision = average_precision_score(y_true, y_score)
            evaluation_results['avg_precision'] = avg_precision
            
            # Plot PR curve
            plt.figure(figsize=(8, 6))
            plt.plot(recall, precision, color='b', lw=2, label=f'PR curve (AP = {avg_precision:.2f})')
            plt.xlabel('Recall')
            plt.ylabel('Precision')
            plt.title(f'Precision-Recall Curve - {model_id}')
            plt.legend(loc="lower left")
            plt.grid(True, alpha=0.3)
            pr_path = os.path.join(output_dir, f'{model_id}_precision_recall.png')
            plt.savefig(pr_path, dpi=300, bbox_inches='tight')
            plt.close()
            evaluation_results['pr_curve_plot'] = pr_path
        
        print(f"Classification Evaluation Completed for {model_id}")
        print(f"Accuracy: {accuracy:.4f}")
        if y_score is not None:
            print(f"ROC AUC: {roc_auc:.4f}")
            print(f"Average Precision: {avg_precision:.4f}")
    
    # Regression metrics
    elif problem_type == 'regression':
        mse = mean_squared_error(y_true, y_pred)
        mae = mean_absolute_error(y_true, y_pred)
        r2 = r2_score(y_true, y_pred)
        rmse = np.sqrt(mse)
        
        evaluation_results.update({
            'mse': mse,
            'mae': mae,
            'r2_score': r2,
            'rmse': rmse
        })
        
        # Plot actual vs predicted
        plt.figure(figsize=(8, 6))
        plt.scatter(y_true, y_pred, alpha=0.5)
        plt.plot([y_true.min(), y_true.max()], [y_true.min(), y_true.max()], 'r--', lw=2)
        plt.xlabel('Actual Values')
        plt.ylabel('Predicted Values')
        plt.title(f'Actual vs Predicted - {model_id}')
        plt.grid(True, alpha=0.3)
        scatter_path = os.path.join(output_dir, f'{model_id}_actual_vs_predicted.png')
        plt.savefig(scatter_path, dpi=300, bbox_inches='tight')
        plt.close()
        evaluation_results['actual_vs_predicted_plot'] = scatter_path
        
        # Residuals plot
        residuals = y_true - y_pred
        plt.figure(figsize=(8, 6))
        plt.scatter(y_pred, residuals, alpha=0.5)
        plt.axhline(y=0, color='r', linestyle='--')
        plt.xlabel('Predicted Values')
        plt.ylabel('Residuals')
        plt.title(f'Residuals Plot - {model_id}')
        plt.grid(True, alpha=0.3)
        residuals_path = os.path.join(output_dir, f'{model_id}_residuals.png')
        plt.savefig(residuals_path, dpi=300, bbox_inches='tight')
        plt.close()
        evaluation_results['residuals_plot'] = residuals_path
        
        print(f"Regression Evaluation Completed for {model_id}")
        print(f"MSE: {mse:.4f}")
        print(f"MAE: {mae:.4f}")
        print(f"R² Score: {r2:.4f}")
        print(f"RMSE: {rmse:.4f}")
    
    # Feature importance (if available)
    if hasattr(model, 'feature_importances_'):
        importances = model.feature_importances_
        if 'train_data' in globals() and 'label_column' in globals():
            feature_names = [col for col in train_data.columns if col != label_column]
        else:
            feature_names = [f'Feature_{i}' for i in range(len(importances))]
        
        # Sort by importance
        indices = np.argsort(importances)[::-1]
        top_n = min(20, len(importances))
        
        # Plot feature importance
        plt.figure(figsize=(12, 8))
        plt.title(f'Top {top_n} Feature Importances - {model_id}')
        plt.bar(range(top_n), importances[indices[:top_n]])
        plt.xticks(range(top_n), [feature_names[i] for i in indices[:top_n]], rotation=45, ha='right')
        plt.tight_layout()
        fi_path = os.path.join(output_dir, f'{model_id}_feature_importance.png')
        plt.savefig(fi_path, dpi=300, bbox_inches='tight')
        plt.close()
        
        evaluation_results['feature_importance_plot'] = fi_path
        evaluation_results['feature_importances'] = importances[indices[:top_n]].tolist()
        evaluation_results['top_features'] = [feature_names[i] for i in indices[:top_n]]
    
    # Learning curve (if training data available)
    if 'train_data' in globals() and 'label_column' in globals():
        try:
            feature_columns = [col for col in train_data.columns if col != label_column]
            X_train = train_data[feature_columns]
            y_train = train_data[label_column]
            
            X_train_processed = pd.get_dummies(X_train)
            X_train_processed = X_train_processed.fillna(X_train_processed.mean())
            
            train_sizes, train_scores, val_scores = learning_curve(
                model, X_train_processed, y_train, cv=5, n_jobs=-1,
                train_sizes=np.linspace(0.1, 1.0, 10)
            )
            
            # Plot learning curve
            plt.figure(figsize=(10, 6))
            train_mean = np.mean(train_scores, axis=1)
            train_std = np.std(train_scores, axis=1)
            val_mean = np.mean(val_scores, axis=1)
            val_std = np.std(val_scores, axis=1)
            
            plt.fill_between(train_sizes, train_mean - train_std, train_mean + train_std, alpha=0.1, color='r')
            plt.fill_between(train_sizes, val_mean - val_std, val_mean + val_std, alpha=0.1, color='g')
            plt.plot(train_sizes, train_mean, 'o-', color='r', label='Training score')
            plt.plot(train_sizes, val_mean, 'o-', color='g', label='Cross-validation score')
            
            plt.title(f'Learning Curve - {model_id}')
            plt.xlabel('Training Examples')
            plt.ylabel('Score')
            plt.legend(loc='best')
            plt.grid(True, alpha=0.3)
            lc_path = os.path.join(output_dir, f'{model_id}_learning_curve.png')
            plt.savefig(lc_path, dpi=300, bbox_inches='tight')
            plt.close()
            
            evaluation_results['learning_curve_plot'] = lc_path
            
        except Exception as lc_error:
            print(f"Warning: Could not generate learning curve: {str(lc_error)}")
    
    # Store results
    globals()['comprehensive_evaluation_results'] = evaluation_results
    
    print(f"\nComprehensive evaluation completed for model '{model_id}'")
    print(f"Results saved to: {output_dir}")
    print(f"Total metrics calculated: {len(evaluation_results)}")
    
    comprehensive_evaluation_successful = True
    
except Exception as e:
    print(f"Error during comprehensive evaluation: {str(e)}")
    comprehensive_evaluation_successful = False

# Store the result
globals()['comprehensive_evaluation_successful'] = comprehensive_evaluation_successful