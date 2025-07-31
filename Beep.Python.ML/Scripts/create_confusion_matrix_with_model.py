# Create confusion matrix with model
from sklearn.metrics import confusion_matrix, classification_report
import matplotlib.pyplot as plt
import seaborn as sns
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
            
            # Make predictions
            y_pred = model.predict(X_test_processed)
            
        elif 'X_test' in globals() and 'y_test' in globals():
            # Use existing test data
            y_true = y_test
            y_pred = model.predict(X_test)
            
        else:
            print("Warning: No test data available for confusion matrix")
            confusion_matrix_created = False
            globals()['confusion_matrix_created'] = confusion_matrix_created
            exit()
        
        # Compute confusion matrix
        cm = confusion_matrix(y_true, y_pred)
        
        # Get unique class labels
        labels = np.unique(np.concatenate([y_true, y_pred]))
        
        # Create the plot
        plt.figure(figsize=(8, 6))
        sns.heatmap(cm, annot=True, fmt='d', cmap='Blues', 
                   xticklabels=labels, yticklabels=labels,
                   cbar_kws={'label': 'Count'})
        
        plt.xlabel('Predicted Label')
        plt.ylabel('True Label')
        plt.title(f'Confusion Matrix - Model: {model_id}')
        plt.tight_layout()
        
        # Save the plot
        plt.savefig(image_path, dpi=300, bbox_inches='tight')
        
        # Calculate additional metrics
        accuracy = np.trace(cm) / np.sum(cm)
        
        # Generate classification report
        try:
            class_report = classification_report(y_true, y_pred, output_dict=True)
            globals()['classification_report'] = class_report
        except Exception as e:
            print(f"Warning: Could not generate classification report: {str(e)}")
        
        # Store results
        globals()['confusion_matrix'] = cm
        globals()['confusion_matrix_labels'] = labels
        globals()['accuracy'] = accuracy
        globals()['y_true'] = y_true
        globals()['y_pred'] = y_pred
        
        print(f"Confusion matrix created successfully for model '{model_id}'")
        print(f"Accuracy: {accuracy:.4f}")
        print(f"Plot saved to: {image_path}")
        confusion_matrix_created = True
        
    else:
        print(f"Warning: Model '{model_id}' not found in models dictionary")
        confusion_matrix_created = False
        
except Exception as e:
    print(f"Error creating confusion matrix: {str(e)}")
    confusion_matrix_created = False

# Store the result
globals()['confusion_matrix_created'] = confusion_matrix_created