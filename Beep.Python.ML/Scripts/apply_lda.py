# Apply Linear Discriminant Analysis (LDA)
from sklearn.discriminant_analysis import LinearDiscriminantAnalysis as LDA
import pandas as pd

# Parameters
label_column = '{label_column}'
n_components = {n_components}
feature_list = {feature_list}

# Apply LDA to the specified features
if 'train_data' in globals() and label_column in train_data.columns:
    # Select features for LDA
    if feature_list:
        features = [f for f in feature_list if f in train_data.columns and f != label_column]
    else:
        features = [col for col in train_data.select_dtypes(include=['number']).columns if col != label_column]
    
    if features:
        # Separate the features and labels
        X = train_data[features]
        Y = train_data[label_column]
        
        # Apply LDA to the selected features
        lda = LDA(n_components=n_components)
        lda_components = lda.fit_transform(X, Y)
        
        # Create DataFrame for the LDA components
        lda_columns = [f'LD{i+1}' for i in range(lda_components.shape[1])]
        lda_df = pd.DataFrame(data=lda_components, columns=lda_columns, index=train_data.index)
        
        # Combine with non-reduced features if needed
        if feature_list:
            # Keep non-LDA features (including label)
            other_features = [col for col in train_data.columns if col not in features]
            train_data = pd.concat([train_data[other_features], lda_df], axis=1)
        else:
            # Replace all numerical features with LDA components, keep non-numerical and label
            non_numerical_features = [col for col in train_data.columns 
                                    if col not in features and col == label_column or 
                                    train_data[col].dtype == 'object']
            if non_numerical_features:
                train_data = pd.concat([train_data[non_numerical_features], lda_df], axis=1)
            else:
                train_data = pd.concat([train_data[[label_column]], lda_df], axis=1)
        
        globals()['train_data'] = train_data
        globals()['lda_transformer'] = lda
        
        # Apply same transformation to test_data if it exists
        if 'test_data' in globals() and features:
            # Check if test_data has the required features
            available_features = [f for f in features if f in test_data.columns]
            if available_features:
                test_components = lda.transform(test_data[available_features])
                test_lda_df = pd.DataFrame(data=test_components, columns=lda_columns, index=test_data.index)
                
                if feature_list:
                    other_features = [col for col in test_data.columns if col not in available_features]
                    if other_features:
                        test_data = pd.concat([test_data[other_features], test_lda_df], axis=1)
                    else:
                        test_data = test_lda_df
                else:
                    non_numerical_features = [col for col in test_data.columns 
                                            if col not in available_features and 
                                            (col == label_column or test_data[col].dtype == 'object')]
                    if non_numerical_features:
                        test_data = pd.concat([test_data[non_numerical_features], test_lda_df], axis=1)
                    else:
                        test_data = test_lda_df
                
                globals()['test_data'] = test_data

elif 'data' in globals() and label_column in data.columns:
    # Same logic for data DataFrame
    if feature_list:
        features = [f for f in feature_list if f in data.columns and f != label_column]
    else:
        features = [col for col in data.select_dtypes(include=['number']).columns if col != label_column]
    
    if features:
        X = data[features]
        Y = data[label_column]
        
        lda = LDA(n_components=n_components)
        lda_components = lda.fit_transform(X, Y)
        
        lda_columns = [f'LD{i+1}' for i in range(lda_components.shape[1])]
        lda_df = pd.DataFrame(data=lda_components, columns=lda_columns, index=data.index)
        
        if feature_list:
            other_features = [col for col in data.columns if col not in features]
            data = pd.concat([data[other_features], lda_df], axis=1)
        else:
            non_numerical_features = [col for col in data.columns 
                                    if col not in features and col == label_column or 
                                    data[col].dtype == 'object']
            if non_numerical_features:
                data = pd.concat([data[non_numerical_features], lda_df], axis=1)
            else:
                data = pd.concat([data[[label_column]], lda_df], axis=1)
        
        globals()['data'] = data
        globals()['lda_transformer'] = lda

else:
    print(f"Warning: Label column '{label_column}' not found or data not available for LDA.")