# Apply Principal Component Analysis (PCA)
from sklearn.decomposition import PCA
import pandas as pd

# Parameters
n_components = {n_components}
feature_list = {feature_list}

# Apply PCA to the specified features
if 'train_data' in globals():
    # Select features for PCA
    if feature_list:
        features = [f for f in feature_list if f in train_data.columns]
    else:
        features = train_data.select_dtypes(include=['number']).columns.tolist()
    
    if features:
        # Apply PCA to the selected features
        pca = PCA(n_components=n_components)
        principal_components = pca.fit_transform(train_data[features])
        
        # Create DataFrame for the principal components
        pc_columns = [f'PC{i+1}' for i in range(principal_components.shape[1])]
        pc_df = pd.DataFrame(data=principal_components, columns=pc_columns, index=train_data.index)
        
        # Combine with non-reduced features if needed
        if feature_list:
            # Keep non-PCA features
            other_features = [col for col in train_data.columns if col not in features]
            if other_features:
                train_data = pd.concat([train_data[other_features], pc_df], axis=1)
            else:
                train_data = pc_df
        else:
            # Replace all numerical features with PCA components
            non_numerical_features = train_data.select_dtypes(exclude=['number']).columns.tolist()
            if non_numerical_features:
                train_data = pd.concat([train_data[non_numerical_features], pc_df], axis=1)
            else:
                train_data = pc_df
        
        globals()['train_data'] = train_data
        globals()['pca_transformer'] = pca
        
        # Apply same transformation to test_data if it exists
        if 'test_data' in globals() and features:
            test_components = pca.transform(test_data[features])
            test_pc_df = pd.DataFrame(data=test_components, columns=pc_columns, index=test_data.index)
            
            if feature_list:
                other_features = [col for col in test_data.columns if col not in features]
                if other_features:
                    test_data = pd.concat([test_data[other_features], test_pc_df], axis=1)
                else:
                    test_data = test_pc_df
            else:
                non_numerical_features = test_data.select_dtypes(exclude=['number']).columns.tolist()
                if non_numerical_features:
                    test_data = pd.concat([test_data[non_numerical_features], test_pc_df], axis=1)
                else:
                    test_data = test_pc_df
            
            globals()['test_data'] = test_data

elif 'data' in globals():
    # Same logic for data DataFrame
    if feature_list:
        features = [f for f in feature_list if f in data.columns]
    else:
        features = data.select_dtypes(include=['number']).columns.tolist()
    
    if features:
        pca = PCA(n_components=n_components)
        principal_components = pca.fit_transform(data[features])
        
        pc_columns = [f'PC{i+1}' for i in range(principal_components.shape[1])]
        pc_df = pd.DataFrame(data=principal_components, columns=pc_columns, index=data.index)
        
        if feature_list:
            other_features = [col for col in data.columns if col not in features]
            if other_features:
                data = pd.concat([data[other_features], pc_df], axis=1)
            else:
                data = pc_df
        else:
            non_numerical_features = data.select_dtypes(exclude=['number']).columns.tolist()
            if non_numerical_features:
                data = pd.concat([data[non_numerical_features], pc_df], axis=1)
            else:
                data = pc_df
        
        globals()['data'] = data
        globals()['pca_transformer'] = pca