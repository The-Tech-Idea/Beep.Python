# Frequency encode categorical features
import pandas as pd

# Parameters
categorical_features = {categorical_features}

# Perform frequency encoding
for feature in categorical_features:
    if 'train_data' in globals() and feature in train_data.columns:
        freq = train_data[feature].value_counts(normalize=True)
        train_data[feature] = train_data[feature].map(freq)
        
        # Store the frequency mapping for test data
        globals()[f'{feature}_freq'] = freq
        
    if 'test_data' in globals() and feature in test_data.columns:
        if f'{feature}_freq' in globals():
            freq = globals()[f'{feature}_freq']
            test_data[feature] = test_data[feature].map(freq).fillna(0)
    
    if 'data' in globals() and feature in data.columns:
        freq = data[feature].value_counts(normalize=True)
        data[feature] = data[feature].map(freq)