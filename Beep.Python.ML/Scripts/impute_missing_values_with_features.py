# Impute missing values with specific features and strategy
from sklearn.impute import SimpleImputer
import pandas as pd

# Feature list and strategy
feature_list = {feature_list}
strategy = '{strategy}'

# Select features to impute
features_to_impute = feature_list if feature_list else []

if 'train_data' in globals():
    if features_to_impute:
        imputer = SimpleImputer(strategy=strategy)
        train_data[features_to_impute] = imputer.fit_transform(train_data[features_to_impute])
    else:
        # Apply to all numerical features if no specific features provided
        numerical_features = train_data.select_dtypes(include=['number']).columns.tolist()
        if numerical_features:
            imputer = SimpleImputer(strategy=strategy)
            train_data[numerical_features] = imputer.fit_transform(train_data[numerical_features])

if 'test_data' in globals():
    if features_to_impute:
        test_data[features_to_impute] = imputer.transform(test_data[features_to_impute])
    else:
        # Apply to all numerical features if no specific features provided
        numerical_features = test_data.select_dtypes(include=['number']).columns.tolist()
        if numerical_features:
            test_data[numerical_features] = imputer.transform(test_data[numerical_features])

if 'data' in globals():
    if features_to_impute:
        data[features_to_impute] = imputer.fit_transform(data[features_to_impute])
    else:
        # Apply to all numerical features if no specific features provided
        numerical_features = data.select_dtypes(include=['number']).columns.tolist()
        if numerical_features:
            data[numerical_features] = imputer.fit_transform(data[numerical_features])

# Store the cleaned data back in the global scope
if 'train_data' in globals():
    globals()['train_data'] = train_data
if 'test_data' in globals():
    globals()['test_data'] = test_data
if 'data' in globals():
    globals()['data'] = data