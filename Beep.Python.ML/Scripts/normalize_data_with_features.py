# Normalize data with selected features
from sklearn.preprocessing import Normalizer

def normalize_data(df, norm='l2', selected_features=None):
    if selected_features is None or len(selected_features) == 0:
        # If no features are selected, normalize all numerical features
        numerical_features = df.select_dtypes(include=['number']).columns
        selected_features = numerical_features
    else:
        # Ensure only the selected numerical features are normalized
        selected_features = [f for f in selected_features if f in df.select_dtypes(include=['number']).columns]
    
    if selected_features:
        normalizer = Normalizer(norm=norm)
        df[selected_features] = normalizer.fit_transform(df[selected_features])
    return df

selected_features = {selected_features}
norm = '{norm}'

if 'train_data' in globals():
    train_data = normalize_data(train_data, norm, selected_features)
if 'test_data' in globals():
    test_data = normalize_data(test_data, norm, selected_features)
if 'data' in globals():
    data = normalize_data(data, norm, selected_features)