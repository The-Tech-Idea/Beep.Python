# Calculate time difference between two date columns
import pandas as pd

# Parameters
start_column = '{start_column}'
end_column = '{end_column}'
new_column_name = '{new_column_name}'

# Calculate time difference between two date columns
if 'train_data' in globals() and start_column in train_data.columns and end_column in train_data.columns:
    train_data[new_column_name] = (pd.to_datetime(train_data[end_column]) - pd.to_datetime(train_data[start_column])).dt.total_seconds()

if 'test_data' in globals() and start_column in test_data.columns and end_column in test_data.columns:
    test_data[new_column_name] = (pd.to_datetime(test_data[end_column]) - pd.to_datetime(test_data[start_column])).dt.total_seconds()

if 'data' in globals() and start_column in data.columns and end_column in data.columns:
    data[new_column_name] = (pd.to_datetime(data[end_column]) - pd.to_datetime(data[start_column])).dt.total_seconds()