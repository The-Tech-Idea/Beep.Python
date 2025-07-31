# Apply random undersampling
from imblearn.under_sampling import RandomUnderSampler
import pandas as pd

# Parameters
target_column = '{target_column}'
sampling_strategy = {sampling_strategy}

# Perform random undersampling to balance the dataset
if 'data' in globals() and target_column in data.columns:
    rus = RandomUnderSampler(sampling_strategy=sampling_strategy, random_state=42)
    X_resampled, y_resampled = rus.fit_resample(data.drop(columns=[target_column]), data[target_column])
    
    # Combine the resampled features and labels back into a DataFrame
    data = pd.concat([pd.DataFrame(X_resampled, columns=data.drop(columns=[target_column]).columns), 
                     pd.Series(y_resampled, name=target_column)], axis=1)
    globals()['data'] = data

if 'train_data' in globals() and target_column in train_data.columns:
    rus = RandomUnderSampler(sampling_strategy=sampling_strategy, random_state=42)
    X_resampled, y_resampled = rus.fit_resample(train_data.drop(columns=[target_column]), train_data[target_column])
    
    # Combine the resampled features and labels back into a DataFrame
    train_data = pd.concat([pd.DataFrame(X_resampled, columns=train_data.drop(columns=[target_column]).columns), 
                           pd.Series(y_resampled, name=target_column)], axis=1)
    globals()['train_data'] = train_data