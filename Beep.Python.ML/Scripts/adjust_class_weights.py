# Adjust class weights for imbalanced data
from sklearn.utils.class_weight import compute_class_weight
import numpy as np
import pandas as pd

# Parameters
model_id = '{model_id}'
algorithm_name = '{algorithm_name}'
parameters = {parameters}
feature_columns = {feature_columns}
label_column = '{label_column}'

# Prepare the data
if 'train_data' in globals():
    X = pd.get_dummies(train_data[feature_columns])
    X.fillna(X.mean(), inplace=True)
    Y = train_data[label_column]
    
    # Compute class weights
    unique_classes = np.unique(Y)
    class_weights = compute_class_weight('balanced', classes=unique_classes, y=Y)
    class_weight_dict = dict(zip(unique_classes, class_weights))
    
    # Dynamically import the algorithm
    if algorithm_name == 'LogisticRegression':
        from sklearn.linear_model import LogisticRegression
        model = LogisticRegression(**parameters, class_weight=class_weight_dict)
    elif algorithm_name == 'RandomForestClassifier':
        from sklearn.ensemble import RandomForestClassifier
        model = RandomForestClassifier(**parameters, class_weight=class_weight_dict)
    elif algorithm_name == 'SVC':
        from sklearn.svm import SVC
        model = SVC(**parameters, class_weight=class_weight_dict)
    else:
        # Generic approach for other algorithms
        algorithm_module = __import__('sklearn.ensemble', fromlist=[algorithm_name])
        algorithm_class = getattr(algorithm_module, algorithm_name)
        model = algorithm_class(**parameters, class_weight=class_weight_dict)
    
    # Fit the model
    model.fit(X, Y)
    
    # Store the model
    if 'models' not in globals():
        models = {}
    models[model_id] = model
    globals()['models'] = models