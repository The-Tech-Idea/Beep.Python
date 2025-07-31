# Create confusion matrix visualization
from sklearn.metrics import confusion_matrix, classification_report
import matplotlib.pyplot as plt
import seaborn as sns
import numpy as np

try:
    # Check if we have the necessary data
    if 'y_test' in globals() and 'y_pred' in globals():
        # Use existing test data and predictions
        y_true = y_test
        y_predicted = y_pred
    elif 'y_true' in globals() and 'y_pred' in globals():
        # Use directly provided true labels and predictions
        y_true = globals()['y_true']
        y_predicted = globals()['y_pred']
    else:
        print("Warning: Required data (y_true, y_pred) not available for confusion matrix")
        confusion_matrix_created = False
    
    if 'y_true' in locals() and 'y_predicted' in locals():
        # Compute confusion matrix
        cm = confusion_matrix(y_true, y_predicted)
        
        # Get unique class labels
        labels = np.unique(np.concatenate([y_true, y_predicted]))
        
        # Create the plot
        plt.figure(figsize=(8, 6))
        sns.heatmap(cm, annot=True, fmt='d', cmap='Blues', 
                   xticklabels=labels, yticklabels=labels,
                   cbar_kws={'label': 'Count'})
        
        plt.xlabel('Predicted Label')
        plt.ylabel('True Label')
        plt.title('Confusion Matrix')
        plt.tight_layout()
        
        # Save the plot
        plt.savefig('confusion_matrix.png', dpi=300, bbox_inches='tight')
        
        # Calculate additional metrics
        accuracy = np.trace(cm) / np.sum(cm)
        
        # Generate classification report
        try:
            class_report = classification_report(y_true, y_predicted, output_dict=True)
            globals()['classification_report'] = class_report
        except Exception as e:
            print(f"Warning: Could not generate classification report: {str(e)}")
        
        # Store results
        globals()['confusion_matrix'] = cm
        globals()['confusion_matrix_labels'] = labels
        globals()['accuracy'] = accuracy
        
        print(f"Confusion matrix created successfully. Accuracy = {accuracy:.4f}")
        confusion_matrix_created = True
        
        # Print basic statistics
        print(f"\nConfusion Matrix:")
        print(cm)
        print(f"\nAccuracy: {accuracy:.4f}")
        
    else:
        confusion_matrix_created = False
        
except Exception as e:
    print(f"Error creating confusion matrix: {str(e)}")
    confusion_matrix_created = False

# Store the result
globals()['confusion_matrix_created'] = confusion_matrix_created