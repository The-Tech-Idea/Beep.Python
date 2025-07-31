# Label encode categorical features
from sklearn.preprocessing import LabelEncoder
import pandas as pd

# Parameters
categorical_features = {categorical_features}

label_encoders = {}

# Perform label encoding
for feature in categorical_features:
    le = LabelEncoder()
    if 'train_data' in globals() and feature in train_data.columns:
        train_data[feature] = le.fit_transform(train_data[feature].astype(str))
        label_encoders[feature] = le
        
    if 'test_data' in globals() and feature in test_data.columns:
        if feature in label_encoders:
            # Handle unseen categories
            unique_values = set(le.classes_)
            test_data[feature] = test_data[feature].apply(lambda x: x if x in unique_values else 'unknown')
            test_data[feature] = le.transform(test_data[feature].astype(str))
        else:
            test_data[feature] = le.fit_transform(test_data[feature].astype(str))
            label_encoders[feature] = le
    
    if 'data' in globals() and feature in data.columns:
        data[feature] = le.fit_transform(data[feature].astype(str))
        label_encoders[feature] = le

# Store the label encoders in the Python persistent scope if needed
globals()['label_encoders'] = label_encoders