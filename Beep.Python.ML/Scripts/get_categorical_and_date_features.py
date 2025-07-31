# Get categorical and date features from selected features
import pandas as pd

# Parameters
selected_features = {selected_features}

# Initialize the feature lists
categorical_features = []
date_features = []

# Get the dtypes of the selected features
if 'data' in globals():
    dtypes = data[selected_features].dtypes
    
    # Filter and get the categorical features
    categorical_features = dtypes[dtypes == 'object'].index.tolist()
    
    # Filter and get the date features (datetime64 or object with date parsing required)
    date_features = dtypes[dtypes == 'datetime64[ns]'].index.tolist()
    
elif 'train_data' in globals():
    dtypes = train_data[selected_features].dtypes
    
    # Filter and get the categorical features
    categorical_features = dtypes[dtypes == 'object'].index.tolist()
    
    # Filter and get the date features (datetime64 or object with date parsing required)
    date_features = dtypes[dtypes == 'datetime64[ns]'].index.tolist()

# Ensure the lists are initialized even if empty
if 'categorical_features' not in globals():
    categorical_features = []

if 'date_features' not in globals():
    date_features = []

# Store the results
globals()['categorical_features'] = categorical_features
globals()['date_features'] = date_features