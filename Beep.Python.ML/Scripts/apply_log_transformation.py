# Apply log transformation
import numpy as np

def apply_log_transformation(df, selected_features=None):
    if selected_features is None or len(selected_features) == 0:
        # If no features are selected, apply to all numerical features
        numerical_features = df.select_dtypes(include=['number']).columns
        selected_features = numerical_features
    else:
        selected_features = [f for f in selected_features if f in df.select_dtypes(include=['number']).columns]
    
    if len(selected_features) > 0:
        # Add small constant to handle zero values, then apply log1p
        df[selected_features] = df[selected_features].apply(lambda x: np.log1p(x.clip(lower=0)))
    return df

selected_features = {selected_features}

if 'train_data' in globals():
    train_data = apply_log_transformation(train_data, selected_features)
if 'test_data' in globals():
    test_data = apply_log_transformation(test_data, selected_features)
if 'data' in globals():
    data = apply_log_transformation(data, selected_features)