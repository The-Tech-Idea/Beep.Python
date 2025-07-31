# Target encode categorical features
import pandas as pd

# Parameters
categorical_features = {categorical_features}
label_column = '{label_column}'

# Perform target encoding
for feature in categorical_features:
    if 'train_data' in globals() and feature in train_data.columns and label_column in train_data.columns:
        means = train_data.groupby(feature)[label_column].mean()
        train_data[feature] = train_data[feature].map(means)
        
        # Store the means for test data
        globals()[f'{feature}_means'] = means
        
    if 'test_data' in globals() and feature in test_data.columns:
        if f'{feature}_means' in globals():
            means = globals()[f'{feature}_means']
            test_data[feature] = test_data[feature].map(means).fillna(means.mean())
    
    if 'data' in globals() and feature in data.columns and label_column in data.columns:
        means = data.groupby(feature)[label_column].mean()
        data[feature] = data[feature].map(means)