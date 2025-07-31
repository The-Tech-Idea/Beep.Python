# Handle missing dates
import pandas as pd

# Parameters
column_name = '{column_name}'
method = '{method}'
fill_value = '{fill_value}'

# Handle missing dates
if 'train_data' in globals() and column_name in train_data.columns:
    if method == 'fill' and fill_value != 'None':
        train_data[column_name] = train_data[column_name].fillna(fill_value)
    elif method == 'interpolate':
        train_data[column_name] = pd.to_datetime(train_data[column_name]).interpolate()

if 'test_data' in globals() and column_name in test_data.columns:
    if method == 'fill' and fill_value != 'None':
        test_data[column_name] = test_data[column_name].fillna(fill_value)
    elif method == 'interpolate':
        test_data[column_name] = pd.to_datetime(test_data[column_name]).interpolate()

if 'data' in globals() and column_name in data.columns:
    if method == 'fill' and fill_value != 'None':
        data[column_name] = data[column_name].fillna(fill_value)
    elif method == 'interpolate':
        data[column_name] = pd.to_datetime(data[column_name]).interpolate()