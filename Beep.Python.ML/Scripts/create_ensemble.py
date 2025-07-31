# Ensemble model creation and stacking
import pandas as pd
import numpy as np
from sklearn.ensemble import VotingClassifier, VotingRegressor
from sklearn.model_selection import cross_val_score

# Parameters
base_model_ids = {base_model_ids}
ensemble_method = '{ensemble_method}'  # 'voting', 'stacking', 'weighted'
ensemble_model_id = '{ensemble_model_id}'
voting_strategy = '{voting_strategy}'  # 'hard', 'soft' for classification
weights = {weights}

try:
    # Check if models exist
    if 'models' not in globals():
        print("Error: No models dictionary found")
        ensemble_successful = False
        globals()['ensemble_successful'] = ensemble_successful
        exit()
    
    # Validate base model IDs
    available_models = [(model_id, models[model_id]) for model_id in base_model_ids if model_id in models]
    if len(available_models) < 2:
        print(f"Error: Need at least 2 base models. Found: {len(available_models)}")
        ensemble_successful = False
        globals()['ensemble_successful'] = ensemble_successful
        exit()
    
    print(f"Creating ensemble from {len(available_models)} base models...")
    for model_id, model in available_models:
        print(f"  - {model_id}: {type(model).__name__}")
    
    # Determine problem type
    problem_type = globals().get('problem_type', 'classification')
    if problem_type not in ['classification', 'regression']:
        # Try to detect from first model
        first_model = available_models[0][1]
        if hasattr(first_model, 'predict_proba') or 'Classifier' in type(first_model).__name__:
            problem_type = 'classification'
        else:
            problem_type = 'regression'
    
    print(f"Problem type: {problem_type}")
    print(f"Ensemble method: {ensemble_method}")
    
    # Prepare training data for ensemble
    if 'train_data' in globals() and 'label_column' in globals():
        feature_columns = [col for col in train_data.columns if col != label_column]
        X_train = train_data[feature_columns]
        y_train = train_data[label_column]
        
        # Process features
        X_train_processed = pd.get_dummies(X_train)
        X_train_processed = X_train_processed.fillna(X_train_processed.mean())
        
    elif 'X_train_processed' in globals() and 'y_train' in globals():
        X_train_processed = globals()['X_train_processed']
        y_train = globals()['y_train']
        
    else:
        print("Error: No training data available for ensemble")
        ensemble_successful = False
        globals()['ensemble_successful'] = ensemble_successful
        exit()
    
    # Create ensemble model based on method
    if ensemble_method == 'voting':
        # Voting ensemble
        estimators = [(model_id, model) for model_id, model in available_models]
        
        if problem_type == 'classification':
            ensemble_model = VotingClassifier(
                estimators=estimators,
                voting=voting_strategy if voting_strategy in ['hard', 'soft'] else 'soft',
                weights=weights if weights else None
            )
        else:
            ensemble_model = VotingRegressor(
                estimators=estimators,
                weights=weights if weights else None
            )
        
        print(f"Created voting ensemble with {len(estimators)} estimators")
        if weights:
            print(f"Using weights: {weights}")
        
    elif ensemble_method == 'stacking':
        # Stacking ensemble (simplified version)
        from sklearn.ensemble import StackingClassifier, StackingRegressor
        from sklearn.linear_model import LogisticRegression, LinearRegression
        
        estimators = [(model_id, model) for model_id, model in available_models]
        
        if problem_type == 'classification':
            final_estimator = LogisticRegression()
            ensemble_model = StackingClassifier(
                estimators=estimators,
                final_estimator=final_estimator,
                cv=5
            )
        else:
            final_estimator = LinearRegression()
            ensemble_model = StackingRegressor(
                estimators=estimators,
                final_estimator=final_estimator,
                cv=5
            )
        
        print(f"Created stacking ensemble with {len(estimators)} base estimators")
        print(f"Final estimator: {type(final_estimator).__name__}")
        
    elif ensemble_method == 'weighted':
        # Custom weighted ensemble (manual implementation)
        class WeightedEnsemble:
            def __init__(self, models, weights, problem_type):
                self.models = models
                self.weights = np.array(weights) if weights else np.ones(len(models)) / len(models)
                self.problem_type = problem_type
                self.is_fitted = False
            
            def fit(self, X, y):
                # Base models should already be fitted
                self.is_fitted = True
                return self
            
            def predict(self, X):
                if not self.is_fitted:
                    raise ValueError("Ensemble not fitted")
                
                predictions = []
                for (model_id, model), weight in zip(self.models, self.weights):
                    pred = model.predict(X)
                    predictions.append(pred * weight)
                
                if self.problem_type == 'classification':
                    # For classification, use weighted voting
                    weighted_preds = np.sum(predictions, axis=0)
                    return np.round(weighted_preds).astype(int)
                else:
                    # For regression, use weighted average
                    return np.sum(predictions, axis=0)
            
            def predict_proba(self, X):
                if self.problem_type != 'classification':
                    raise ValueError("predict_proba only available for classification")
                
                probabilities = []
                for (model_id, model), weight in zip(self.models, self.weights):
                    if hasattr(model, 'predict_proba'):
                        proba = model.predict_proba(X)
                        probabilities.append(proba * weight)
                
                if probabilities:
                    return np.sum(probabilities, axis=0)
                else:
                    raise ValueError("No base models support predict_proba")
        
        # Normalize weights
        if not weights:
            weights = [1.0] * len(available_models)
        weights = np.array(weights) / np.sum(weights)
        
        ensemble_model = WeightedEnsemble(available_models, weights, problem_type)
        
        print(f"Created weighted ensemble with normalized weights: {weights}")
    
    else:
        raise ValueError(f"Unknown ensemble method: {ensemble_method}")
    
    # Fit the ensemble model
    print("Fitting ensemble model...")
    ensemble_model.fit(X_train_processed, y_train)
    
    # Evaluate ensemble performance
    print("Evaluating ensemble performance...")
    
    if problem_type == 'classification':
        cv_scores = cross_val_score(ensemble_model, X_train_processed, y_train, cv=5, scoring='accuracy')
        print(f"Cross-validation accuracy: {cv_scores.mean():.4f} (+/- {cv_scores.std() * 2:.4f})")
    else:
        cv_scores = cross_val_score(ensemble_model, X_train_processed, y_train, cv=5, scoring='r2')
        print(f"Cross-validation R²: {cv_scores.mean():.4f} (+/- {cv_scores.std() * 2:.4f})")
    
    # Store the ensemble model
    models[ensemble_model_id] = ensemble_model
    globals()['models'] = models
    
    # Store ensemble information
    ensemble_info = {
        'ensemble_model_id': ensemble_model_id,
        'ensemble_method': ensemble_method,
        'base_models': base_model_ids,
        'problem_type': problem_type,
        'cv_scores': cv_scores.tolist(),
        'cv_mean': cv_scores.mean(),
        'cv_std': cv_scores.std(),
        'weights': weights.tolist() if hasattr(weights, 'tolist') else weights
    }
    
    if ensemble_method == 'voting':
        ensemble_info['voting_strategy'] = voting_strategy
    
    globals()['ensemble_info'] = ensemble_info
    
    print(f"\nEnsemble model '{ensemble_model_id}' created successfully!")
    print(f"Base models: {base_model_ids}")
    print(f"Method: {ensemble_method}")
    print(f"Performance: {cv_scores.mean():.4f}")
    
    ensemble_successful = True
    
except Exception as e:
    print(f"Error creating ensemble: {str(e)}")
    ensemble_successful = False

# Store the result
globals()['ensemble_successful'] = ensemble_successful