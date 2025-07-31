# Binary encode categorical features
try:
    import category_encoders as ce
    binary_encoder_available = True
except ImportError:
    binary_encoder_available = False
    print("Warning: category_encoders not installed. Please install it using: pip install category_encoders")

import pandas as pd

# Parameters
categorical_features = {categorical_features}

if binary_encoder_available:
    # Perform binary encoding
    encoder = ce.BinaryEncoder(cols=categorical_features)
    
    if 'train_data' in globals():
        train_data = encoder.fit_transform(train_data)
        
    if 'test_data' in globals():
        test_data = encoder.transform(test_data)
        
    if 'data' in globals():
        data = encoder.fit_transform(data)
    
    # Store the encoder in the Python persistent scope if needed
    globals()['binary_encoder'] = encoder
else:
    # Fallback to one-hot encoding if binary encoder is not available
    if 'train_data' in globals():
        train_data = pd.get_dummies(train_data, columns=categorical_features, drop_first=False)
        
    if 'test_data' in globals():
        test_data = pd.get_dummies(test_data, columns=categorical_features, drop_first=False)
        if 'train_data' in globals():
            test_data = test_data.reindex(columns=train_data.columns, fill_value=0)
            
    if 'data' in globals():
        data = pd.get_dummies(data, columns=categorical_features, drop_first=False)