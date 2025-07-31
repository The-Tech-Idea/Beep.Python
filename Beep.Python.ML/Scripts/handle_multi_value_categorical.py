# Handle multi-value categorical features
import pandas as pd

def handle_multi_value_categorical_features(data, feature_list):
    for feature in feature_list:
        # Split the multi-value feature into individual values
        split_features = data[feature].str.split(',', expand=True)
        
        # Get unique values across the entire column to create dummy variables
        unique_values = pd.unique(split_features.values.ravel('K'))
        unique_values = [val for val in unique_values if val is not None]
        
        # For each unique value, create a binary column
        for value in unique_values:
            if value is not None and value != '':
                data[f'{feature}_{value}'] = split_features.apply(lambda row: int(value in row.values), axis=1)
        
        # Drop the original multi-value feature column
        data = data.drop(columns=[feature])
    
    return data

# List of features with multiple values
multi_value_features = {multi_value_features}

# Process the multi-value features for train_data, test_data, and predict_data if they exist
if 'train_data' in globals():
    train_data = handle_multi_value_categorical_features(train_data, multi_value_features)
    globals()['train_data'] = train_data

if 'test_data' in globals():
    test_data = handle_multi_value_categorical_features(test_data, multi_value_features)
    globals()['test_data'] = test_data

if 'data' in globals():
    data = handle_multi_value_categorical_features(data, multi_value_features)
    globals()['data'] = data