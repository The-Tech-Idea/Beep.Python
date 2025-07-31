# Apply variance threshold for feature selection
from sklearn.feature_selection import VarianceThreshold
import pandas as pd

# Parameters
threshold = {threshold}

# Apply Variance Threshold
if 'train_data' in globals():
    # Select only numerical features for variance threshold
    numerical_features = train_data.select_dtypes(include=['number']).columns.tolist()
    
    if numerical_features:
        selector = VarianceThreshold(threshold=threshold)
        selected_features = selector.fit_transform(train_data[numerical_features])
        
        # Get the names of selected features
        selected_feature_names = [numerical_features[i] for i in selector.get_support(indices=True)]
        
        # Keep selected numerical features and all non-numerical features
        non_numerical_features = train_data.select_dtypes(exclude=['number']).columns.tolist()
        all_selected_features = selected_feature_names + non_numerical_features
        
        # Update train_data with selected features
        train_data = train_data[all_selected_features]
        globals()['train_data'] = train_data
        
        # Apply same selection to test_data if it exists
        if 'test_data' in globals():
            test_data = test_data[all_selected_features]
            globals()['test_data'] = test_data

elif 'data' in globals():
    # Select only numerical features for variance threshold
    numerical_features = data.select_dtypes(include=['number']).columns.tolist()
    
    if numerical_features:
        selector = VarianceThreshold(threshold=threshold)
        selected_features = selector.fit_transform(data[numerical_features])
        
        # Get the names of selected features
        selected_feature_names = [numerical_features[i] for i in selector.get_support(indices=True)]
        
        # Keep selected numerical features and all non-numerical features
        non_numerical_features = data.select_dtypes(exclude=['number']).columns.tolist()
        all_selected_features = selected_feature_names + non_numerical_features
        
        # Update data with selected features
        data = data[all_selected_features]
        globals()['data'] = data