# Create learning curve visualization
from sklearn.model_selection import learning_curve
import matplotlib.pyplot as plt
import numpy as np

# Parameters
model_id = '{model_id}'
image_path = '{image_path}'

try:
    # Check if models dictionary exists and contains the specified model
    if 'models' in globals() and model_id in models:
        model = models[model_id]
        
        if 'train_data' in globals() and 'label_column' in globals():
            # Prepare features and target
            feature_columns = [col for col in train_data.columns if col != label_column]
            X = train_data[feature_columns]
            y = train_data[label_column]
            
            # Handle categorical features by converting to dummy variables
            X_processed = pd.get_dummies(X)
            X_processed = X_processed.fillna(X_processed.mean())
            
            # Generate learning curve
            train_sizes, train_scores, test_scores = learning_curve(
                model, X_processed, y, cv=5, n_jobs=-1, 
                train_sizes=np.linspace(0.1, 1.0, 10)
            )
            
            # Calculate mean and std for train and test scores
            train_scores_mean = np.mean(train_scores, axis=1)
            train_scores_std = np.std(train_scores, axis=1)
            test_scores_mean = np.mean(test_scores, axis=1)
            test_scores_std = np.std(test_scores, axis=1)
            
            # Create the plot
            plt.figure(figsize=(10, 6))
            plt.fill_between(train_sizes, train_scores_mean - train_scores_std,
                           train_scores_mean + train_scores_std, alpha=0.1, color='r')
            plt.fill_between(train_sizes, test_scores_mean - test_scores_std,
                           test_scores_mean + test_scores_std, alpha=0.1, color='g')
            plt.plot(train_sizes, train_scores_mean, 'o-', color='r', label='Training score')
            plt.plot(train_sizes, test_scores_mean, 'o-', color='g', label='Cross-validation score')
            
            plt.title('Learning Curve')
            plt.xlabel('Training Examples')
            plt.ylabel('Score')
            plt.legend(loc='best')
            plt.grid(True, alpha=0.3)
            plt.tight_layout()
            
            # Save the plot
            plt.savefig(image_path, dpi=300, bbox_inches='tight')
            
            # Store results
            globals()['learning_curve_train_sizes'] = train_sizes
            globals()['learning_curve_train_scores'] = train_scores
            globals()['learning_curve_test_scores'] = test_scores
            
            print(f"Learning curve created successfully and saved to: {image_path}")
            learning_curve_created = True
            
        else:
            print("Warning: Required data (train_data, label_column) not available for learning curve")
            learning_curve_created = False
    else:
        print(f"Warning: Model '{model_id}' not found in models dictionary")
        learning_curve_created = False
        
except Exception as e:
    print(f"Error creating learning curve: {str(e)}")
    learning_curve_created = False

# Store the result
globals()['learning_curve_created'] = learning_curve_created