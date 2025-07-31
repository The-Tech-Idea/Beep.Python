# Apply correlation threshold for feature selection
import numpy as np
import pandas as pd

# Parameters
threshold = {threshold}

# Apply correlation threshold
if 'train_data' in globals():
    # Select only numerical features for correlation analysis
    numerical_features = train_data.select_dtypes(include=['number']).columns.tolist()
    
    if len(numerical_features) > 1:
        # Compute the correlation matrix
        corr_matrix = train_data[numerical_features].corr().abs()
        
        # Select upper triangle of correlation matrix
        upper = corr_matrix.where(np.triu(np.ones(corr_matrix.shape), k=1).astype(bool))
        
        # Find features with correlation greater than the threshold
        to_drop = [column for column in upper.columns if any(upper[column] > threshold)]
        
        # Drop highly correlated features
        features_to_keep = [col for col in train_data.columns if col not in to_drop]
        train_data = train_data[features_to_keep]
        globals()['train_data'] = train_data
        
        # Apply same selection to test_data if it exists
        if 'test_data' in globals():
            test_data = test_data[features_to_keep]
            globals()['test_data'] = test_data

elif 'data' in globals():
    # Select only numerical features for correlation analysis
    numerical_features = data.select_dtypes(include=['number']).columns.tolist()
    
    if len(numerical_features) > 1:
        # Compute the correlation matrix
        corr_matrix = data[numerical_features].corr().abs()
        
        # Select upper triangle of correlation matrix
        upper = corr_matrix.where(np.triu(np.ones(corr_matrix.shape), k=1).astype(bool))
        
        # Find features with correlation greater than the threshold
        to_drop = [column for column in upper.columns if any(upper[column] > threshold)]
        
        # Drop highly correlated features
        features_to_keep = [col for col in data.columns if col not in to_drop]
        data = data[features_to_keep]
        globals()['data'] = data