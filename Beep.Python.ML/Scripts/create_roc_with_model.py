# Create ROC curve with model
from sklearn.metrics import roc_curve, auc
import matplotlib.pyplot as plt
import numpy as np

# Parameters
model_id = '{model_id}'
image_path = '{image_path}'

try:
    # Check if models dictionary exists and contains the specified model
    if 'models' in globals() and model_id in models:
        model = models[model_id]
        
        # Check if we have test data and can make predictions
        if 'test_data' in globals() and 'label_column' in globals():
            # Prepare test data
            feature_columns = [col for col in test_data.columns if col != label_column]
            X_test = test_data[feature_columns]
            y_true = test_data[label_column]
            
            # Handle categorical features
            X_test_processed = pd.get_dummies(X_test)
            X_test_processed = X_test_processed.fillna(X_test_processed.mean())
            
            # Get prediction probabilities
            if hasattr(model, 'predict_proba'):
                y_score = model.predict_proba(X_test_processed)[:, 1]  # Binary classification
            elif hasattr(model, 'decision_function'):
                y_score = model.decision_function(X_test_processed)
            else:
                print("Warning: Model does not support probability prediction")
                roc_created = False
                globals()['roc_created'] = roc_created
                exit()
                
        elif 'X_test' in globals() and 'y_test' in globals():
            # Use existing test data
            y_true = y_test
            if hasattr(model, 'predict_proba'):
                y_score = model.predict_proba(X_test)[:, 1]
            elif hasattr(model, 'decision_function'):
                y_score = model.decision_function(X_test)
            else:
                print("Warning: Model does not support probability prediction")
                roc_created = False
                globals()['roc_created'] = roc_created
                exit()
                
        else:
            print("Warning: No test data available for ROC curve")
            roc_created = False
            globals()['roc_created'] = roc_created
            exit()
        
        # Compute ROC curve and AUC
        fpr, tpr, thresholds = roc_curve(y_true, y_score)
        roc_auc = auc(fpr, tpr)
        
        # Create the plot
        plt.figure(figsize=(8, 6))
        plt.plot(fpr, tpr, color='darkorange', lw=2, 
                label=f'ROC curve (AUC = {roc_auc:.2f})')
        plt.plot([0, 1], [0, 1], color='navy', lw=2, linestyle='--', 
                label='Random classifier')
        
        plt.xlim([0.0, 1.0])
        plt.ylim([0.0, 1.05])
        plt.xlabel('False Positive Rate')
        plt.ylabel('True Positive Rate')
        plt.title(f'ROC Curve - Model: {model_id}')
        plt.legend(loc="lower right")
        plt.grid(True, alpha=0.3)
        plt.tight_layout()
        
        # Save the plot
        plt.savefig(image_path, dpi=300, bbox_inches='tight')
        
        # Store results
        globals()['roc_fpr'] = fpr
        globals()['roc_tpr'] = tpr
        globals()['roc_auc'] = roc_auc
        globals()['roc_thresholds'] = thresholds
        globals()['y_true'] = y_true
        globals()['y_score'] = y_score
        
        print(f"ROC curve created successfully for model '{model_id}'")
        print(f"AUC = {roc_auc:.4f}")
        print(f"Plot saved to: {image_path}")
        roc_created = True
        
    else:
        print(f"Warning: Model '{model_id}' not found in models dictionary")
        roc_created = False
        
except Exception as e:
    print(f"Error creating ROC curve: {str(e)}")
    roc_created = False

# Store the result
globals()['roc_created'] = roc_created