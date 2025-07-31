# Robust scale data with selected features
from sklearn.preprocessing import RobustScaler

def robust_scale_data(df, selected_features=None):
    if selected_features is None or len(selected_features) == 0:
        # If no features are selected, scale all numerical features
        numerical_features = df.select_dtypes(include=['number']).columns
        selected_features = numerical_features
    else:
        # Ensure only the selected numerical features are scaled
        selected_features = [f for f in selected_features if f in df.select_dtypes(include=['number']).columns]
    
    if selected_features:
        scaler = RobustScaler()
        df[selected_features] = scaler.fit_transform(df[selected_features])
    return df

selected_features = {selected_features}

if 'train_data' in globals():
    train_data = robust_scale_data(train_data, selected_features)
if 'test_data' in globals():
    test_data = robust_scale_data(test_data, selected_features)
if 'data' in globals():
    data = robust_scale_data(data, selected_features)