# Standardize data with selected features
from sklearn.preprocessing import StandardScaler

def standardize_data(df, selected_features=None):
    if selected_features is None or len(selected_features) == 0:
        # If no features are selected, standardize all numerical features
        numerical_features = df.select_dtypes(include=['number']).columns
        selected_features = numerical_features
    else:
        # Ensure only the selected numerical features are standardized
        selected_features = [f for f in selected_features if f in df.select_dtypes(include=['number']).columns]
    
    if selected_features:
        scaler = StandardScaler()
        df[selected_features] = scaler.fit_transform(df[selected_features])
    return df

selected_features = {selected_features}

if 'train_data' in globals():
    train_data = standardize_data(train_data, selected_features)
if 'test_data' in globals():
    test_data = standardize_data(test_data, selected_features)  
if 'data' in globals():
    data = standardize_data(data, selected_features)