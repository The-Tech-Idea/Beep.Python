# Create precision-recall curve visualization
from sklearn.metrics import precision_recall_curve, average_precision_score
import matplotlib.pyplot as plt

# Parameters
model_id = '{model_id}'
image_path = '{image_path}'

try:
    # Check if models dictionary exists and contains the specified model
    if 'models' in globals() and model_id in models:
        model = models[model_id]
        
        # Check if we have the necessary data
        if 'X_test' in globals() and 'y_test' in globals():
            # Use existing test data
            y_true = y_test
            y_score = model.predict_proba(X_test)[:, 1]  # Assuming binary classification
        elif 'test_data' in globals() and 'label_column' in globals():
            # Prepare test data
            feature_columns = [col for col in test_data.columns if col != label_column]
            X_test = test_data[feature_columns]
            y_true = test_data[label_column]
            
            # Handle categorical features
            X_test_processed = pd.get_dummies(X_test)
            X_test_processed = X_test_processed.fillna(X_test_processed.mean())
            
            # Get prediction probabilities
            if hasattr(model, 'predict_proba'):
                y_score = model.predict_proba(X_test_processed)[:, 1]
            elif hasattr(model, 'decision_function'):
                y_score = model.decision_function(X_test_processed)
            else:
                print("Warning: Model does not support probability prediction")
                precision_recall_created = False
                globals()['precision_recall_created'] = precision_recall_created
                exit()
        else:
            print("Warning: Required test data not available for precision-recall curve")
            precision_recall_created = False
            globals()['precision_recall_created'] = precision_recall_created
            exit()
        
        # Calculate precision-recall curve
        precision, recall, thresholds = precision_recall_curve(y_true, y_score)
        average_precision = average_precision_score(y_true, y_score)
        
        # Create the plot
        plt.figure(figsize=(8, 6))
        plt.plot(recall, precision, color='b', lw=2, 
                label=f'Precision-Recall curve (AP = {average_precision:.2f})')
        plt.xlabel('Recall')
        plt.ylabel('Precision')
        plt.title('Precision-Recall Curve')
        plt.legend(loc='lower left')
        plt.grid(True, alpha=0.3)
        plt.xlim([0.0, 1.0])
        plt.ylim([0.0, 1.05])
        plt.tight_layout()
        
        # Save the plot
        plt.savefig(image_path, dpi=300, bbox_inches='tight')
        
        # Store results
        globals()['precision_recall_precision'] = precision
        globals()['precision_recall_recall'] = recall
        globals()['precision_recall_thresholds'] = thresholds
        globals()['average_precision'] = average_precision
        
        print(f"Precision-Recall curve created successfully. AP = {average_precision:.4f}")
        print(f"Curve saved to: {image_path}")
        precision_recall_created = True
        
    else:
        print(f"Warning: Model '{model_id}' not found in models dictionary")
        precision_recall_created = False
        
except Exception as e:
    print(f"Error creating precision-recall curve: {str(e)}")
    precision_recall_created = False

# Store the result
globals()['precision_recall_created'] = precision_recall_created