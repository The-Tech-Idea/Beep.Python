# MinMax scale data with selected features
from sklearn.preprocessing import MinMaxScaler

def min_max_scale_data(df, feature_range=(0, 1), selected_features=None):
    if selected_features is None or len(selected_features) == 0:
        # If no features are selected, scale all numerical features
        numerical_features = df.select_dtypes(include=['number']).columns
        selected_features = numerical_features
    else:
        # Ensure only the selected numerical features are scaled
        selected_features = [f for f in selected_features if f in df.select_dtypes(include=['number']).columns]
    
    if selected_features:
        scaler = MinMaxScaler(feature_range=feature_range)
        df[selected_features] = scaler.fit_transform(df[selected_features])
    return df

selected_features = {selected_features}
feature_range_min = {feature_range_min}
feature_range_max = {feature_range_max}

if 'train_data' in globals():
    train_data = min_max_scale_data(train_data, feature_range=(feature_range_min, feature_range_max), selected_features=selected_features)
if 'test_data' in globals():
    test_data = min_max_scale_data(test_data, feature_range=(feature_range_min, feature_range_max), selected_features=selected_features)
if 'data' in globals():
    data = min_max_scale_data(data, feature_range=(feature_range_min, feature_range_max), selected_features=selected_features)