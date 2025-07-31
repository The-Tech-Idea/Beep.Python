# Apply tree-based feature selection
import matplotlib.pyplot as plt
import numpy as np

# Parameters
model_id = '{model_id}'

try:
    # Check if models dictionary exists and contains the specified model
    if 'models' in globals() and model_id in models:
        model = models[model_id]
        
        # Check if the model has feature importances
        if hasattr(model, 'feature_importances_'):
            importances = model.feature_importances_
            
            # Get feature names if available
            if 'train_data' in globals() and 'label_column' in globals():
                feature_names = [col for col in train_data.columns if col != label_column]
            elif 'feature_names' in globals():
                feature_names = globals()['feature_names']
            else:
                feature_names = [f'Feature_{i}' for i in range(len(importances))]
            
            # Ensure we have the right number of feature names
            if len(feature_names) != len(importances):
                feature_names = [f'Feature_{i}' for i in range(len(importances))]
            
            # Sort features by importance
            indices = np.argsort(importances)[::-1]
            
            # Select features above a certain threshold or top N features
            # Method 1: Use features above mean importance
            mean_importance = np.mean(importances)
            selected_indices = [i for i in range(len(importances)) if importances[i] > mean_importance]
            
            # Method 2: Ensure we have at least some features (top 50% if mean method gives too few)
            if len(selected_indices) < max(1, len(importances) // 4):
                n_select = max(1, len(importances) // 2)
                selected_indices = indices[:n_select].tolist()
            
            selected_features = [feature_names[i] for i in selected_indices]
            selected_importances = [importances[i] for i in selected_indices]
            
            # Create visualization
            plt.figure(figsize=(12, 8))
            
            # Plot all features (top 20 for readability)
            top_n = min(20, len(importances))
            top_indices = indices[:top_n]
            top_names = [feature_names[i] for i in top_indices]
            top_importances = [importances[i] for i in top_indices]
            
            bars = plt.bar(range(top_n), top_importances, align='center')
            
            # Highlight selected features
            for i, idx in enumerate(top_indices):
                if idx in selected_indices:
                    bars[i].set_color('orange')
                else:
                    bars[i].set_color('lightblue')
            
            plt.title(f'Tree-based Feature Importance - Model: {model_id}')
            plt.xlabel('Features')
            plt.ylabel('Importance')
            plt.xticks(range(top_n), top_names, rotation=45, ha='right')
            
            # Add legend
            import matplotlib.patches as mpatches
            selected_patch = mpatches.Patch(color='orange', label='Selected Features')
            not_selected_patch = mpatches.Patch(color='lightblue', label='Not Selected')
            plt.legend(handles=[selected_patch, not_selected_patch])
            
            plt.tight_layout()
            plt.savefig('tree_feature_selection.png', dpi=300, bbox_inches='tight')
            plt.show()
            
            # Apply feature selection to data
            if 'train_data' in globals():
                # Keep selected features plus label column
                cols_to_keep = selected_features.copy()
                if 'label_column' in globals() and label_column in train_data.columns:
                    cols_to_keep.append(label_column)
                
                # Filter to only existing columns
                cols_to_keep = [col for col in cols_to_keep if col in train_data.columns]
                train_data = train_data[cols_to_keep]
                globals()['train_data'] = train_data
                
                # Apply same selection to test_data if it exists
                if 'test_data' in globals():
                    test_cols = [col for col in cols_to_keep if col in test_data.columns]
                    test_data = test_data[test_cols]
                    globals()['test_data'] = test_data
            
            elif 'data' in globals():
                # Apply to single data DataFrame
                cols_to_keep = selected_features.copy()
                if 'label_column' in globals() and label_column in data.columns:
                    cols_to_keep.append(label_column)
                
                cols_to_keep = [col for col in cols_to_keep if col in data.columns]
                data = data[cols_to_keep]
                globals()['data'] = data
            
            # Store results
            globals()['tree_feature_importances'] = importances
            globals()['tree_feature_names'] = feature_names
            globals()['tree_selected_features'] = selected_features
            globals()['tree_selected_importances'] = selected_importances
            globals()['tree_selection_threshold'] = mean_importance
            
            print(f"Tree-based feature selection completed for model '{model_id}'")
            print(f"Original features: {len(feature_names)}")
            print(f"Selected features: {len(selected_features)}")
            print(f"Selection threshold (mean importance): {mean_importance:.4f}")
            print(f"Selected features: {selected_features[:10]}...")  # Show first 10
            
            tree_selection_successful = True
            
        else:
            print(f"Warning: Model '{model_id}' does not have feature importances (not a tree-based model)")
            tree_selection_successful = False
            
    else:
        print(f"Warning: Model '{model_id}' not found in models dictionary")
        tree_selection_successful = False
        
except Exception as e:
    print(f"Error in tree-based feature selection: {str(e)}")
    tree_selection_successful = False

# Store the result
globals()['tree_selection_successful'] = tree_selection_successful