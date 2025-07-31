# Extract date/time components
import pandas as pd

# Parameter
column_name = '{column_name}'

# Extract components from the date/time column
if 'train_data' in globals() and column_name in train_data.columns:
    train_data[column_name + '_year'] = pd.to_datetime(train_data[column_name]).dt.year
    train_data[column_name + '_month'] = pd.to_datetime(train_data[column_name]).dt.month
    train_data[column_name + '_day'] = pd.to_datetime(train_data[column_name]).dt.day
    train_data[column_name + '_hour'] = pd.to_datetime(train_data[column_name]).dt.hour
    train_data[column_name + '_minute'] = pd.to_datetime(train_data[column_name]).dt.minute
    train_data[column_name + '_dayofweek'] = pd.to_datetime(train_data[column_name]).dt.dayofweek

if 'test_data' in globals() and column_name in test_data.columns:
    test_data[column_name + '_year'] = pd.to_datetime(test_data[column_name]).dt.year
    test_data[column_name + '_month'] = pd.to_datetime(test_data[column_name]).dt.month
    test_data[column_name + '_day'] = pd.to_datetime(test_data[column_name]).dt.day
    test_data[column_name + '_hour'] = pd.to_datetime(test_data[column_name]).dt.hour
    test_data[column_name + '_minute'] = pd.to_datetime(test_data[column_name]).dt.minute
    test_data[column_name + '_dayofweek'] = pd.to_datetime(test_data[column_name]).dt.dayofweek

if 'data' in globals() and column_name in data.columns:
    data[column_name + '_year'] = pd.to_datetime(data[column_name]).dt.year
    data[column_name + '_month'] = pd.to_datetime(data[column_name]).dt.month
    data[column_name + '_day'] = pd.to_datetime(data[column_name]).dt.day
    data[column_name + '_hour'] = pd.to_datetime(data[column_name]).dt.hour
    data[column_name + '_minute'] = pd.to_datetime(data[column_name]).dt.minute
    data[column_name + '_dayofweek'] = pd.to_datetime(data[column_name]).dt.dayofweek