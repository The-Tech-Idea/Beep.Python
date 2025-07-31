# Handle cyclical time features using sine and cosine transformations
import numpy as np
import pandas as pd

# Parameters
column_name = '{column_name}'
feature_type = '{feature_type}'

# Handle cyclical nature of time features
if 'train_data' in globals() and column_name in train_data.columns:
    max_value = 23 if feature_type == 'hour' else 11 if feature_type == 'month' else 6 if feature_type == 'dayofweek' else None
    if max_value:
        train_data[column_name + '_sin'] = np.sin(2 * np.pi * train_data[column_name] / max_value)
        train_data[column_name + '_cos'] = np.cos(2 * np.pi * train_data[column_name] / max_value)

if 'test_data' in globals() and column_name in test_data.columns:
    max_value = 23 if feature_type == 'hour' else 11 if feature_type == 'month' else 6 if feature_type == 'dayofweek' else None
    if max_value:
        test_data[column_name + '_sin'] = np.sin(2 * np.pi * test_data[column_name] / max_value)
        test_data[column_name + '_cos'] = np.cos(2 * np.pi * test_data[column_name] / max_value)

if 'data' in globals() and column_name in data.columns:
    max_value = 23 if feature_type == 'hour' else 11 if feature_type == 'month' else 6 if feature_type == 'dayofweek' else None
    if max_value:
        data[column_name + '_sin'] = np.sin(2 * np.pi * data[column_name] / max_value)
        data[column_name + '_cos'] = np.cos(2 * np.pi * data[column_name] / max_value)