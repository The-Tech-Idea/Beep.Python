# Random search hyperparameter optimization
from sklearn.model_selection import RandomizedSearchCV
import numpy as np
import pandas as pd

# Parameters
algorithm_module = '{algorithm_module}'
algorithm_name = '{algorithm_name}'
param_distributions = {param_distributions}
cv_folds = {cv_folds}
n_iterations = {n_iterations}
label_column = '{label_column}'
model_id = '{model_id}'

try:
    # Import the algorithm dynamically
    if algorithm_module and algorithm_name:
        if algorithm_module == 'ensemble':
            from sklearn.ensemble import RandomForestClassifier, RandomForestRegressor, GradientBoostingClassifier, GradientBoostingRegressor
            from sklearn.ensemble import AdaBoostClassifier, ExtraTreesClassifier, BaggingClassifier
        elif algorithm_module == 'linear_model':
            from sklearn.linear_model import LogisticRegression, LinearRegression, Ridge, Lasso, ElasticNet
        elif algorithm_module == 'svm':
            from sklearn.svm import SVC, SVR
        elif algorithm_module == 'neighbors':
            from sklearn.neighbors import KNeighborsClassifier, KNeighborsRegressor
        elif algorithm_module == 'tree':
            from sklearn.tree import DecisionTreeClassifier, DecisionTreeRegressor
        elif algorithm_module == 'naive_bayes':
            from sklearn.naive_bayes import GaussianNB, MultinomialNB, BernoulliNB
        
        # Get the algorithm class
        module = __import__(f'sklearn.{algorithm_module}', fromlist=[algorithm_name])
        algorithm_class = getattr(module, algorithm_name)
        
        # Create base estimator
        base_estimator = algorithm_class()
        
    else:
        print("Error: Algorithm module and name must be specified")
        random_search_successful = False
        globals()['random_search_successful'] = random_search_successful
        exit()
    
    # Prepare the data
    if 'train_data' in globals() and label_column in train_data.columns:
        # Prepare features and target
        feature_columns = [col for col in train_data.columns if col != label_column]
        X = train_data[feature_columns]
        y = train_data[label_column]
        
        # Handle categorical features by converting to dummy variables
        X_processed = pd.get_dummies(X)
        X_processed = X_processed.fillna(X_processed.mean())
        
        # Perform random search
        random_search = RandomizedSearchCV(
            estimator=base_estimator,
            param_distributions=param_distributions,
            n_iter=n_iterations,
            cv=cv_folds,
            scoring='accuracy',  # Can be parameterized later
            n_jobs=-1,
            random_state=42,
            verbose=1
        )
        
        # Fit the random search
        random_search.fit(X_processed, y)
        
        # Store results
        best_model = random_search.best_estimator_
        best_params = random_search.best_params_
        best_score = random_search.best_score_
        
        # Store the best model
        if 'models' not in globals():
            models = {}
        models[model_id] = best_model
        globals()['models'] = models
        
        # Store random search results
        globals()['random_search_results'] = {
            'best_params': best_params,
            'best_score': best_score,
            'cv_results': random_search.cv_results_,
            'best_estimator': best_model
        }
        
        print(f"Random search completed for {algorithm_name}")
        print(f"Best score: {best_score:.4f}")
        print(f"Best parameters: {best_params}")
        print(f"Total iterations: {n_iterations}")
        print(f"Cross-validation folds: {cv_folds}")
        
        random_search_successful = True
        
    elif 'data' in globals() and label_column in data.columns:
        # Same logic for single data DataFrame
        feature_columns = [col for col in data.columns if col != label_column]
        X = data[feature_columns]
        y = data[label_column]
        
        X_processed = pd.get_dummies(X)
        X_processed = X_processed.fillna(X_processed.mean())
        
        random_search = RandomizedSearchCV(
            estimator=base_estimator,
            param_distributions=param_distributions,
            n_iter=n_iterations,
            cv=cv_folds,
            scoring='accuracy',
            n_jobs=-1,
            random_state=42,
            verbose=1
        )
        
        random_search.fit(X_processed, y)
        
        best_model = random_search.best_estimator_
        best_params = random_search.best_params_
        best_score = random_search.best_score_
        
        if 'models' not in globals():
            models = {}
        models[model_id] = best_model
        globals()['models'] = models
        
        globals()['random_search_results'] = {
            'best_params': best_params,
            'best_score': best_score,
            'cv_results': random_search.cv_results_,
            'best_estimator': best_model
        }
        
        print(f"Random search completed for {algorithm_name}")
        print(f"Best score: {best_score:.4f}")
        print(f"Best parameters: {best_params}")
        
        random_search_successful = True
    
    else:
        print(f"Error: Required data or label column '{label_column}' not available")
        random_search_successful = False
        
except Exception as e:
    print(f"Error during random search: {str(e)}")
    random_search_successful = False

# Store the result
globals()['random_search_successful'] = random_search_successful