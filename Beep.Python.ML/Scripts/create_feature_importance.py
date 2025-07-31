# Create feature importance visualization
import matplotlib.pyplot as plt
import numpy as np

# Parameters
model_id = '{model_id}'
image_path = '{image_path}'

try:
    # Check if models dictionary exists and contains the specified model
    if 'models' in globals() and model_id in models:
        model = models[model_id]
        
        # Check if the model has feature importances
        if hasattr(model, 'feature_importances_'):
            importances = model.feature_importances_
            
            # Get feature names if available
            if 'feature_names' in globals():
                feature_names = globals()['feature_names']
            elif 'train_data' in globals() and 'label_column' in globals():
                feature_names = [col for col in train_data.columns if col != label_column]
            else:
                feature_names = [f'Feature_{i}' for i in range(len(importances))]
            
            # Ensure we have the right number of feature names
            if len(feature_names) != len(importances):
                feature_names = [f'Feature_{i}' for i in range(len(importances))]
            
            # Sort features by importance
            indices = np.argsort(importances)[::-1]
            
            # Limit to top features for readability
            top_features = min(20, len(importances))
            top_indices = indices[:top_features]
            top_importances = importances[top_indices]
            top_feature_names = [feature_names[i] for i in top_indices]
            
            # Create the plot
            plt.figure(figsize=(12, 8))
            plt.title(f'Feature Importance - Top {top_features} Features')
            plt.bar(range(top_features), top_importances, align='center')
            plt.xticks(range(top_features), top_feature_names, rotation=45, ha='right')
            plt.xlabel('Features')
            plt.ylabel('Importance')
            plt.tight_layout()
            
            # Save the plot
            plt.savefig(image_path, dpi=300, bbox_inches='tight')
            
            # Store results
            globals()['feature_importances'] = importances
            globals()['feature_importance_indices'] = indices
            globals()['top_feature_names'] = top_feature_names
            globals()['top_feature_importances'] = top_importances
            
            print(f"Feature importance plot created successfully")
            print(f"Top 5 important features:")
            for i in range(min(5, top_features)):
                print(f"  {i+1}. {top_feature_names[i]}: {top_importances[i]:.4f}")
            print(f"Plot saved to: {image_path}")
            feature_importance_created = True
            
        else:
            print(f"Warning: Model '{model_id}' does not have feature importances")
            feature_importance_created = False
            
    else:
        print(f"Warning: Model '{model_id}' not found in models dictionary")
        feature_importance_created = False
        
except Exception as e:
    print(f"Error creating feature importance plot: {str(e)}")
    feature_importance_created = False

# Store the result
globals()['feature_importance_created'] = feature_importance_created