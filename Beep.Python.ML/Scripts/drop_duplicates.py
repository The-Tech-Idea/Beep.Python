# Drop duplicate rows
import pandas as pd

# Parameters
feature_list = {feature_list}

# Determine subset of columns to check for duplicates
subset_columns = feature_list if feature_list else None

# Drop duplicates based on specified features or all columns
if 'train_data' in globals():
    train_data = train_data.drop_duplicates(subset=subset_columns)
    globals()['train_data'] = train_data

if 'test_data' in globals():
    test_data = test_data.drop_duplicates(subset=subset_columns)
    globals()['test_data'] = test_data

if 'data' in globals():
    data = data.drop_duplicates(subset=subset_columns)
    globals()['data'] = data