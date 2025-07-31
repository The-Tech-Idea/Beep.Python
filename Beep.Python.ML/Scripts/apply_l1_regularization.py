# Apply L1 regularization for feature selection
from sklearn.linear_model import Lasso
from sklearn.feature_selection import SelectFromModel
import pandas as pd

# Parameters
alpha = {alpha}

# Apply L1 regularization for feature selection
if 'train_data' in globals() and 'label_column' in globals():
    # Prepare features and target
    feature_columns = [col for col in train_data.columns if col != label_column]
    X = train_data[feature_columns]
    y = train_data[label_column]
    
    # Handle categorical features by converting to dummy variables
    X_processed = pd.get_dummies(X)
    X_processed = X_processed.fillna(X_processed.mean())
    
    # Apply Lasso for feature selection
    lasso = Lasso(alpha=alpha)
    
    # Use SelectFromModel to automatically select features
    selector = SelectFromModel(lasso)
    selector.fit(X_processed, y)
    
    # Get selected features
    selected_features = X_processed.columns[selector.get_support()].tolist()
    
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
    
    # Ensure we have at least some features selected
    if not original_selected or original_selected == [label_column]:
        print("Warning: No features selected by L1 regularization. Keeping all features.")
        original_selected = train_data.columns.tolist()
    
    # Update train_data with selected features
    train_data = train_data[original_selected]
    globals()['train_data'] = train_data
    
    # Apply same selection to test_data if it exists
    if 'test_data' in globals():
        test_features = [col for col in original_selected if col in test_data.columns]
        test_data = test_data[test_features]
        globals()['test_data'] = test_data

elif 'data' in globals() and 'label_column' in globals():
    # Same logic for data DataFrame
    feature_columns = [col for col in data.columns if col != label_column]
    X = data[feature_columns]
    y = data[label_column]
    
    X_processed = pd.get_dummies(X)
    X_processed = X_processed.fillna(X_processed.mean())
    
    lasso = Lasso(alpha=alpha)
    selector = SelectFromModel(lasso)
    selector.fit(X_processed, y)
    
    selected_features = X_processed.columns[selector.get_support()].tolist()
    
    original_selected = []
    for feature in data.columns:
        if feature == label_column:
            original_selected.append(feature)
        else:
            dummy_cols = [col for col in selected_features if col.startswith(feature)]
            if dummy_cols or feature in selected_features:
                original_selected.append(feature)
    
    if not original_selected or original_selected == [label_column]:
        original_selected = data.columns.tolist()
    
    data = data[original_selected]
    globals()['data'] = data

else:
    print("Warning: label_column not defined or data not available for L1 regularization.")