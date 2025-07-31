# Apply feature hashing
from sklearn.feature_extraction import FeatureHasher
import pandas as pd

def apply_feature_hashing(df, selected_features, n_features=10):
    if len(selected_features) > 0:
        hasher = FeatureHasher(input_type='string', n_features=n_features)
        
        # Convert selected features to string representation
        feature_data = df[selected_features].astype(str)
        
        # Apply feature hashing
        hashed_features = hasher.fit_transform(feature_data.values)
        
        # Create DataFrame from hashed features
        hashed_df = pd.DataFrame(hashed_features.toarray(), 
                                index=df.index,
                                columns=[f'hash_{i}' for i in range(n_features)])
        
        # Drop original features and add hashed features
        df = df.drop(columns=selected_features)
        df = pd.concat([df, hashed_df], axis=1)
    return df

selected_features = {selected_features}
n_features = {n_features}

if 'train_data' in globals():
    train_data = apply_feature_hashing(train_data, selected_features, n_features)
if 'test_data' in globals():
    test_data = apply_feature_hashing(test_data, selected_features, n_features)
if 'data' in globals():
    data = apply_feature_hashing(data, selected_features, n_features)