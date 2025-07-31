# Apply Balanced Random Forest
from imblearn.ensemble import BalancedRandomForestClassifier
import pandas as pd

# Parameters
target_column = '{target_column}'
n_estimators = {n_estimators}

# Apply Balanced Random Forest for feature selection
if 'train_data' in globals() and target_column in train_data.columns:
    model = BalancedRandomForestClassifier(n_estimators=n_estimators, random_state=42)
    X = pd.get_dummies(train_data.drop(columns=[target_column]))
    y = train_data[target_column]
    model.fit(X, y)
    
    # Store the model
    if 'models' not in globals():
        models = {}
    models[f'BalancedRandomForest_{target_column}'] = model
    globals()['models'] = models

elif 'data' in globals() and target_column in data.columns:
    model = BalancedRandomForestClassifier(n_estimators=n_estimators, random_state=42)
    X = pd.get_dummies(data.drop(columns=[target_column]))
    y = data[target_column]
    model.fit(X, y)
    
    # Store the model
    if 'models' not in globals():
        models = {}
    models[f'BalancedRandomForest_{target_column}'] = model
    globals()['models'] = models