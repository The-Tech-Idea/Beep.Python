from sklearn.preprocessing import StandardScaler

# Standardize the data
scaler = StandardScaler()

# Get numerical features
if 'numerical_features' not in globals():
    numerical_features = data.select_dtypes(include=['number']).columns.tolist()

# Apply standardization
data[numerical_features] = scaler.fit_transform(data[numerical_features])

# Store the standardized data back in the global scope
globals()['data'] = data