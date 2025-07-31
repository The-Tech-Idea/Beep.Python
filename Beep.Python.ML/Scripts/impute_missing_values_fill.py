# Impute missing values with fill method
import pandas as pd

# Fill method
method = '{method}'

if 'train_data' in globals():
    if method == 'ffill':
        train_data = train_data.fillna(method='ffill')
    elif method == 'bfill':
        train_data = train_data.fillna(method='bfill')
    elif method == 'interpolate':
        train_data = train_data.interpolate()

if 'test_data' in globals():
    if method == 'ffill':
        test_data = test_data.fillna(method='ffill')
    elif method == 'bfill':
        test_data = test_data.fillna(method='bfill')
    elif method == 'interpolate':
        test_data = test_data.interpolate()

if 'data' in globals():
    if method == 'ffill':
        data = data.fillna(method='ffill')
    elif method == 'bfill':
        data = data.fillna(method='bfill')
    elif method == 'interpolate':
        data = data.interpolate()

# Store the cleaned data back in the global scope
if 'train_data' in globals():
    globals()['train_data'] = train_data
if 'test_data' in globals():
    globals()['test_data'] = test_data
if 'data' in globals():
    globals()['data'] = data