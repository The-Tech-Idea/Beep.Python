# Remove outliers using Z-score method
import pandas as pd
import numpy as np

# Parameters
feature_list = {feature_list}
z_threshold = {z_threshold}

# Select features to check for outliers
if not feature_list:
    # If no specific features provided, use all numerical features
    if 'data' in globals():
        features = data.select_dtypes(include=['number']).columns.tolist()
    elif 'train_data' in globals():
        features = train_data.select_dtypes(include=['number']).columns.tolist()
    else:
        features = []
else:
    features = feature_list

# Remove outliers based on Z-score
if features:
    if 'train_data' in globals():
        # Calculate Z-scores
        z_scores = np.abs((train_data[features] - train_data[features].mean()) / train_data[features].std())
        # Keep rows where all features have Z-score below threshold
        train_data = train_data[(z_scores < z_threshold).all(axis=1)]
        globals()['train_data'] = train_data

    if 'test_data' in globals():
        # Use training data statistics for test data
        if 'train_data' in globals():
            z_scores = np.abs((test_data[features] - train_data[features].mean()) / train_data[features].std())
        else:
            z_scores = np.abs((test_data[features] - test_data[features].mean()) / test_data[features].std())
        test_data = test_data[(z_scores < z_threshold).all(axis=1)]
        globals()['test_data'] = test_data

    if 'data' in globals():
        z_scores = np.abs((data[features] - data[features].mean()) / data[features].std())
        data = data[(z_scores < z_threshold).all(axis=1)]
        globals()['data'] = data