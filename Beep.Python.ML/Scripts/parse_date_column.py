# Parse date column into datetime object
import pandas as pd

# Parameter
column_name = '{column_name}'

# Parse the date column into a datetime object
if 'train_data' in globals() and column_name in train_data.columns:
    train_data[column_name] = pd.to_datetime(train_data[column_name], errors='coerce')

if 'test_data' in globals() and column_name in test_data.columns:
    test_data[column_name] = pd.to_datetime(test_data[column_name], errors='coerce')

if 'data' in globals() and column_name in data.columns:
    data[column_name] = pd.to_datetime(data[column_name], errors='coerce')