# Apply NearMiss undersampling
from imblearn.under_sampling import NearMiss
import pandas as pd

# Parameters
target_column = '{target_column}'
version = {version}

# Perform NearMiss to balance the dataset
if 'data' in globals() and target_column in data.columns:
    nm = NearMiss(version=version)
    X_resampled, y_resampled = nm.fit_resample(data.drop(columns=[target_column]), data[target_column])
    
    # Combine the resampled features and labels back into a DataFrame
    data = pd.concat([pd.DataFrame(X_resampled, columns=data.drop(columns=[target_column]).columns), 
                     pd.Series(y_resampled, name=target_column)], axis=1)
    globals()['data'] = data

if 'train_data' in globals() and target_column in train_data.columns:
    nm = NearMiss(version=version)
    X_resampled, y_resampled = nm.fit_resample(train_data.drop(columns=[target_column]), train_data[target_column])
    
    # Combine the resampled features and labels back into a DataFrame
    train_data = pd.concat([pd.DataFrame(X_resampled, columns=train_data.drop(columns=[target_column]).columns), 
                           pd.Series(y_resampled, name=target_column)], axis=1)
    globals()['train_data'] = train_data