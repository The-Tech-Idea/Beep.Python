# Impute missing values with custom value
import pandas as pd

# Custom value for imputation
custom_value = {custom_value}

if 'train_data' in globals():
    train_data = train_data.fillna(custom_value)

if 'test_data' in globals():
    test_data = test_data.fillna(custom_value)

if 'data' in globals():
    data = data.fillna(custom_value)

# Store the cleaned data back in the global scope
if 'train_data' in globals():
    globals()['train_data'] = train_data
if 'test_data' in globals():
    globals()['test_data'] = test_data
if 'data' in globals():
    globals()['data'] = data