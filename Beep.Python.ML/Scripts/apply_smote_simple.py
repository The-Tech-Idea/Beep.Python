# Apply SMOTE (simplified version)
from imblearn.over_sampling import SMOTE
import pandas as pd

# Parameter
label_column = '{label_column}'

# Apply SMOTE
if 'data' in globals() and label_column in data.columns:
    smote = SMOTE(random_state=42)
    X, Y = smote.fit_resample(data.drop(columns=[label_column]), data[label_column])
    
    # Combine the resampled features and labels back into a DataFrame
    data = pd.concat([pd.DataFrame(X), pd.Series(Y, name=label_column)], axis=1)
    globals()['data'] = data

if 'train_data' in globals() and label_column in train_data.columns:
    smote = SMOTE(random_state=42)
    X, Y = smote.fit_resample(train_data.drop(columns=[label_column]), train_data[label_column])
    
    # Combine the resampled features and labels back into a DataFrame
    train_data = pd.concat([pd.DataFrame(X), pd.Series(Y, name=label_column)], axis=1)
    globals()['train_data'] = train_data