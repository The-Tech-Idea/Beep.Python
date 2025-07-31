# Impute missing values using various strategies
from sklearn.impute import SimpleImputer
import pandas as pd

# Strategy for imputation
strategy = '{strategy}'

# Get numerical features for the strategy
if strategy in ['mean', 'median']:
    # For numerical strategies, select only numerical columns
    if 'train_data' in globals():
        numerical_features = train_data.select_dtypes(include=['number']).columns.tolist()
        imputer = SimpleImputer(strategy=strategy)
        train_data[numerical_features] = imputer.fit_transform(train_data[numerical_features])
    
    if 'test_data' in globals():
        numerical_features = test_data.select_dtypes(include=['number']).columns.tolist()
        test_data[numerical_features] = imputer.transform(test_data[numerical_features])
    
    if 'data' in globals():
        numerical_features = data.select_dtypes(include=['number']).columns.tolist()
        data[numerical_features] = imputer.fit_transform(data[numerical_features])

elif strategy == 'most_frequent':
    # For categorical strategies, can apply to all columns
    if 'train_data' in globals():
        imputer = SimpleImputer(strategy=strategy)
        train_data = pd.DataFrame(imputer.fit_transform(train_data), columns=train_data.columns)
    
    if 'test_data' in globals():
        test_data = pd.DataFrame(imputer.transform(test_data), columns=test_data.columns)
    
    if 'data' in globals():
        data = pd.DataFrame(imputer.fit_transform(data), columns=data.columns)

# Store the cleaned data back in the global scope
if 'train_data' in globals():
    globals()['train_data'] = train_data
if 'test_data' in globals():
    globals()['test_data'] = test_data
if 'data' in globals():
    globals()['data'] = data