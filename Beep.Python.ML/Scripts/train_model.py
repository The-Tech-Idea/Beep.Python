# Train machine learning model with comprehensive algorithm support
import pandas as pd
import numpy as np
from sklearn.model_selection import train_test_split

# Parameters
model_id = '{model_id}'
algorithm_module = '{algorithm_module}'
algorithm_name = '{algorithm_name}'
parameters = {parameters}
feature_columns = {feature_columns}
label_column = '{label_column}'

try:
    # Import the algorithm dynamically
    if algorithm_module and algorithm_name:
        if algorithm_module == 'ensemble':
            from sklearn.ensemble import (RandomForestClassifier, RandomForestRegressor, 
                                         GradientBoostingClassifier, GradientBoostingRegressor,
                                         AdaBoostClassifier, AdaBoostRegressor, ExtraTreesClassifier,
                                         BaggingClassifier, VotingClassifier)
        elif algorithm_module == 'linear_model':
            from sklearn.linear_model import (LogisticRegression, LinearRegression, Ridge, 
                                            Lasso, ElasticNet, SGDClassifier, SGDRegressor)
        elif algorithm_module == 'svm':
            from sklearn.svm import SVC, SVR
        elif algorithm_module == 'neighbors':
            from sklearn.neighbors import KNeighborsClassifier, KNeighborsRegressor
        elif algorithm_module == 'tree':
            from sklearn.tree import DecisionTreeClassifier, DecisionTreeRegressor
        elif algorithm_module == 'naive_bayes':
            from sklearn.naive_bayes import GaussianNB, MultinomialNB, BernoulliNB
        elif algorithm_module == 'neural_network':
            from sklearn.neural_network import MLPClassifier, MLPRegressor
        
        # Get the algorithm class
        module = __import__(f'sklearn.{algorithm_module}', fromlist=[algorithm_name])
        algorithm_class = getattr(module, algorithm_name)
        
        # Create model with parameters
        model = algorithm_class(**parameters) if parameters else algorithm_class()
        
    else:
        print("Error: Algorithm module and name must be specified")
        train_model_successful = False
        globals()['train_model_successful'] = train_model_successful
        exit()
    
    # Prepare the data
    if 'train_data' in globals() and label_column in train_data.columns:
        # Use existing train_data
        if feature_columns:
            # Use specific features
            X_train = train_data[feature_columns]
        else:
            # Use all features except label
            feature_cols = [col for col in train_data.columns if col != label_column]
            X_train = train_data[feature_cols]
        
        y_train = train_data[label_column]
        
    elif 'data' in globals() and label_column in data.columns:
        # Split the data if no train_data exists
        if feature_columns:
            X = data[feature_columns]
        else:
            feature_cols = [col for col in data.columns if col != label_column]
            X = data[feature_cols]
        
        y = data[label_column]
        
        # Split data (80/20 by default)
        X_train, X_test, y_train, y_test = train_test_split(
            X, y, test_size=0.2, random_state=42, 
            stratify=y if len(y.unique()) < 50 else None  # Stratify for classification
        )
        
        # Store test data for later use
        test_data = pd.concat([X_test, y_test], axis=1)
        globals()['test_data'] = test_data
        globals()['X_test'] = X_test
        globals()['y_test'] = y_test
        
    else:
        print(f"Error: Required data or label column '{label_column}' not available")
        train_model_successful = False
        globals()['train_model_successful'] = train_model_successful
        exit()
    
    # Handle categorical features by converting to dummy variables
    X_train_processed = pd.get_dummies(X_train)
    X_train_processed = X_train_processed.fillna(X_train_processed.mean())
    
    # Train the model
    print(f"Training {algorithm_name} model...")
    print(f"Training data shape: {X_train_processed.shape}")
    print(f"Target variable shape: {y_train.shape}")
    
    model.fit(X_train_processed, y_train)
    
    # Store the trained model
    if 'models' not in globals():
        models = {}
    models[model_id] = model
    globals()['models'] = models
    
    # Store training data for later use
    globals()['X_train_processed'] = X_train_processed
    globals()['y_train'] = y_train
    
    # Calculate basic training metrics
    train_predictions = model.predict(X_train_processed)
    
    # Calculate accuracy/R2 based on problem type
    if hasattr(model, 'predict_proba') or 'Classifier' in algorithm_name:
        # Classification
        from sklearn.metrics import accuracy_score, classification_report
        train_accuracy = accuracy_score(y_train, train_predictions)
        print(f"Training accuracy: {train_accuracy:.4f}")
        
        # Store classification metrics
        globals()['train_accuracy'] = train_accuracy
        globals()['problem_type'] = 'classification'
        
        try:
            class_report = classification_report(y_train, train_predictions, output_dict=True)
            globals()['train_classification_report'] = class_report
        except:
            pass
            
    else:
        # Regression
        from sklearn.metrics import r2_score, mean_squared_error, mean_absolute_error
        train_r2 = r2_score(y_train, train_predictions)
        train_rmse = np.sqrt(mean_squared_error(y_train, train_predictions))
        train_mae = mean_absolute_error(y_train, train_predictions)
        
        print(f"Training R²: {train_r2:.4f}")
        print(f"Training RMSE: {train_rmse:.4f}")
        print(f"Training MAE: {train_mae:.4f}")
        
        # Store regression metrics
        globals()['train_r2'] = train_r2
        globals()['train_rmse'] = train_rmse
        globals()['train_mae'] = train_mae
        globals()['problem_type'] = 'regression'
    
    # Store training results
    training_results = {
        'model_id': model_id,
        'algorithm': algorithm_name,
        'parameters': parameters,
        'training_samples': len(y_train),
        'features': X_train_processed.columns.tolist(),
        'model': model
    }
    
    globals()['training_results'] = training_results
    
    print(f"Model '{model_id}' trained successfully!")
    print(f"Algorithm: {algorithm_name}")
    print(f"Parameters: {parameters}")
    train_model_successful = True
    
except Exception as e:
    print(f"Error during model training: {str(e)}")
    train_model_successful = False

# Store the result
globals()['train_model_successful'] = train_model_successful