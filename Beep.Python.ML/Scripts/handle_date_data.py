# Handle date data features
import pandas as pd

# List of date features
date_features = {date_features}

# Ensure all date features are in datetime format
for feature in date_features:
    if 'data' in globals():
        data[feature] = pd.to_datetime(data[feature], errors='coerce')
    if 'train_data' in globals():
        train_data[feature] = pd.to_datetime(train_data[feature], errors='coerce')
    if 'test_data' in globals():
        test_data[feature] = pd.to_datetime(test_data[feature], errors='coerce')

# Extract components like year, month, day
for feature in date_features:
    if 'data' in globals():
        data[feature + '_year'] = data[feature].dt.year
        data[feature + '_month'] = data[feature].dt.month  
        data[feature + '_day'] = data[feature].dt.day
    if 'train_data' in globals():
        train_data[feature + '_year'] = train_data[feature].dt.year
        train_data[feature + '_month'] = train_data[feature].dt.month
        train_data[feature + '_day'] = train_data[feature].dt.day
    if 'test_data' in globals():
        test_data[feature + '_year'] = test_data[feature].dt.year
        test_data[feature + '_month'] = test_data[feature].dt.month
        test_data[feature + '_day'] = test_data[feature].dt.day

# Convert to timestamp
for feature in date_features:
    if 'data' in globals():
        data[feature + '_timestamp'] = data[feature].apply(lambda x: x.timestamp() if pd.notnull(x) else None)
    if 'train_data' in globals():
        train_data[feature + '_timestamp'] = train_data[feature].apply(lambda x: x.timestamp() if pd.notnull(x) else None)
    if 'test_data' in globals():
        test_data[feature + '_timestamp'] = test_data[feature].apply(lambda x: x.timestamp() if pd.notnull(x) else None)

# Drop the original date columns
if 'data' in globals():
    data.drop(columns=date_features, inplace=True)
if 'train_data' in globals():
    train_data.drop(columns=date_features, inplace=True)
if 'test_data' in globals():
    test_data.drop(columns=date_features, inplace=True)