# Normalize data
from sklearn.preprocessing import Normalizer

# Normalize the data
norm = '{norm}'
scaler = Normalizer(norm=norm)

# Get numerical features
if 'train_data' in globals():
    numerical_features = train_data.select_dtypes(include=['number']).columns.tolist()
    if numerical_features:
        train_data[numerical_features] = scaler.fit_transform(train_data[numerical_features])

if 'test_data' in globals():
    numerical_features = test_data.select_dtypes(include=['number']).columns.tolist()
    if numerical_features:
        test_data[numerical_features] = scaler.transform(test_data[numerical_features])

if 'data' in globals():
    numerical_features = data.select_dtypes(include=['number']).columns.tolist()
    if numerical_features:
        data[numerical_features] = scaler.fit_transform(data[numerical_features])

# Store the normalized data back in the global scope
if 'train_data' in globals():
    globals()['train_data'] = train_data
if 'test_data' in globals():
    globals()['test_data'] = test_data
if 'data' in globals():
    globals()['data'] = data