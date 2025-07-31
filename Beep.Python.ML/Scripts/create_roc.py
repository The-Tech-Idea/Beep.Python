# Create ROC curve visualization
from sklearn.metrics import roc_curve, auc
import matplotlib.pyplot as plt
import numpy as np

try:
    # Check if we have the necessary data
    if 'y_test' in globals() and 'y_pred_proba' in globals():
        # Use existing test data and predictions
        y_true = y_test
        y_score = y_pred_proba[:, 1] if len(y_pred_proba.shape) > 1 else y_pred_proba
    elif 'y_true' in globals() and 'y_score' in globals():
        # Use directly provided true labels and scores
        y_true = globals()['y_true']
        y_score = globals()['y_score']
    else:
        print("Warning: Required data (y_true, y_score) not available for ROC curve")
        roc_created = False
    
    if 'y_true' in locals() and 'y_score' in locals():
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
        plt.title('Receiver Operating Characteristic (ROC) Curve')
        plt.legend(loc="lower right")
        plt.grid(True, alpha=0.3)
        
        # Save or display the plot
        plt.tight_layout()
        plt.savefig('roc_curve.png', dpi=300, bbox_inches='tight')
        
        # Store results
        globals()['roc_fpr'] = fpr
        globals()['roc_tpr'] = tpr
        globals()['roc_auc'] = roc_auc
        globals()['roc_thresholds'] = thresholds
        
        print(f"ROC curve created successfully. AUC = {roc_auc:.4f}")
        roc_created = True
    else:
        roc_created = False
        
except Exception as e:
    print(f"Error creating ROC curve: {str(e)}")
    roc_created = False

# Store the result
globals()['roc_created'] = roc_created