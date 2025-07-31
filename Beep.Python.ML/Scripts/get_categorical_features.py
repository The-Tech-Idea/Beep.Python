# Get categorical features from selected features
import pandas as pd

# Parameters
selected_features = {selected_features}

# Get the dtypes of the selected features
if 'data' in globals():
    dtypes = data[selected_features].dtypes
    # Filter and get the categorical features
    categorical_features = dtypes[dtypes == 'object'].index.tolist()
elif 'train_data' in globals():
    dtypes = train_data[selected_features].dtypes
    # Filter and get the categorical features
    categorical_features = dtypes[dtypes == 'object'].index.tolist()
else:
    categorical_features = []

# Store the result
globals()['categorical_features'] = categorical_features