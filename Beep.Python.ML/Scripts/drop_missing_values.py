# Drop missing values
import pandas as pd

# Axis for dropping missing values
axis = '{axis}'

if 'train_data' in globals():
    if axis == 'rows':
        train_data = train_data.dropna(axis=0)
    elif axis == 'columns':
        train_data = train_data.dropna(axis=1)

if 'test_data' in globals():
    if axis == 'rows':
        test_data = test_data.dropna(axis=0)
    elif axis == 'columns':
        test_data = test_data.dropna(axis=1)

if 'data' in globals():
    if axis == 'rows':
        data = data.dropna(axis=0)
    elif axis == 'columns':
        data = data.dropna(axis=1)

# Store the cleaned data back in the global scope
if 'train_data' in globals():
    globals()['train_data'] = train_data
if 'test_data' in globals():
    globals()['test_data'] = test_data
if 'data' in globals():
    globals()['data'] = data