# Apply SMOTE (Synthetic Minority Oversampling Technique)
from imblearn.over_sampling import SMOTE
import pandas as pd

# Parameters
target_column = '{target_column}'
sampling_strategy = {sampling_strategy}

# Perform SMOTE to balance the dataset
if 'data' in globals() and target_column in data.columns:
    smote = SMOTE(sampling_strategy=sampling_strategy, random_state=42)
    X_resampled, y_resampled = smote.fit_resample(data.drop(columns=[target_column]), data[target_column])
    
    # Combine the resampled features and labels back into a DataFrame
    data = pd.concat([pd.DataFrame(X_resampled, columns=data.drop(columns=[target_column]).columns), 
                     pd.Series(y_resampled, name=target_column)], axis=1)
    globals()['data'] = data

if 'train_data' in globals() and target_column in train_data.columns:
    smote = SMOTE(sampling_strategy=sampling_strategy, random_state=42)
    X_resampled, y_resampled = smote.fit_resample(train_data.drop(columns=[target_column]), train_data[target_column])
    
    # Combine the resampled features and labels back into a DataFrame
    train_data = pd.concat([pd.DataFrame(X_resampled, columns=train_data.drop(columns=[target_column]).columns), 
                           pd.Series(y_resampled, name=target_column)], axis=1)
    globals()['train_data'] = train_data