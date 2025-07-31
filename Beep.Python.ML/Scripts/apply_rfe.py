# Apply Recursive Feature Elimination (RFE)
from sklearn.feature_selection import RFE
import pandas as pd

# Parameters
model_id = '{model_id}'
n_features_to_select = {n_features_to_select}

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
        
        # Apply RFE
        selector = RFE(model, n_features_to_select=n_features_to_select, step=1)
        selector = selector.fit(X_processed, y)
        
        # Get selected features
        selected_features = X_processed.columns[selector.get_support()].tolist()
        
        # Keep the label column if it was part of the original train_data
        if label_column in train_data.columns:
            selected_features = selected_features + [label_column]
        
        # Update train_data with selected features
        # Map back from processed features to original features
        original_selected = []
        for feature in train_data.columns:
            if feature == label_column:
                original_selected.append(feature)
            else:
                # Check if any of the dummy variables for this feature were selected
                dummy_cols = [col for col in selected_features if col.startswith(feature)]
                if dummy_cols or feature in selected_features:
                    original_selected.append(feature)
        
        train_data = train_data[original_selected]
        globals()['train_data'] = train_data
        
        # Apply same selection to test_data if it exists
        if 'test_data' in globals():
            test_features = [col for col in original_selected if col in test_data.columns]
            test_data = test_data[test_features]
            globals()['test_data'] = test_data
            
else:
    print(f"Warning: Model '{model_id}' not found in models dictionary or models dictionary does not exist.")