# Perform cross-validation on a trained model
from sklearn.model_selection import cross_val_score
import numpy as np

# Parameters
model_id = '{model_id}'
num_folds = {num_folds}

# Check if models dictionary exists and contains the specified model
if 'models' in globals() and model_id in models:
    model = models[model_id]
    
    if 'train_data' in globals() and 'label_column' in globals():
        # Prepare features and target
        feature_columns = [col for col in train_data.columns if col != label_column]
        X = train_data[feature_columns]
        y = train_data[label_column]
        
        # Handle categorical features by converting to dummy variables
        X_processed = pd.get_dummies(X)
        X_processed = X_processed.fillna(X_processed.mean())
        
        try:
            # Perform cross-validation
            scores = cross_val_score(model, X_processed, y, cv=num_folds, n_jobs=-1, scoring='accuracy')
            
            # Calculate statistics
            avg_score = scores.mean()
            std_score = scores.std()
            min_score = scores.min()
            max_score = scores.max()
            
            # Store results in global scope
            cv_results = {
                'scores': scores.tolist(),
                'avg_score': avg_score,
                'std_score': std_score,
                'min_score': min_score,
                'max_score': max_score,
                'num_folds': num_folds
            }
            
            globals()['cv_results'] = cv_results
            globals()['avg_cross_val_score'] = avg_score
            globals()['std_cross_val_score'] = std_score
            
            print(f"Cross-validation completed:")
            print(f"Average Score: {avg_score:.4f}")
            print(f"Standard Deviation: {std_score:.4f}")
            print(f"Score Range: {min_score:.4f} - {max_score:.4f}")
            
        except Exception as e:
            print(f"Error during cross-validation: {str(e)}")
            # Try with different scoring metric for regression models
            try:
                scores = cross_val_score(model, X_processed, y, cv=num_folds, n_jobs=-1, scoring='neg_mean_squared_error')
                avg_score = scores.mean()
                std_score = scores.std()
                
                cv_results = {
                    'scores': scores.tolist(),
                    'avg_score': avg_score,
                    'std_score': std_score,
                    'scoring': 'neg_mean_squared_error',
                    'num_folds': num_folds
                }
                
                globals()['cv_results'] = cv_results
                globals()['avg_cross_val_score'] = avg_score
                globals()['std_cross_val_score'] = std_score
                
                print(f"Cross-validation completed with MSE scoring:")
                print(f"Average MSE Score: {avg_score:.4f}")
                print(f"Standard Deviation: {std_score:.4f}")
                
            except Exception as e2:
                print(f"Cross-validation failed: {str(e2)}")
                globals()['cv_results'] = {'error': str(e2)}

    elif 'data' in globals() and 'label_column' in globals():
        # Same logic for data DataFrame
        feature_columns = [col for col in data.columns if col != label_column]
        X = data[feature_columns]
        y = data[label_column]
        
        X_processed = pd.get_dummies(X)
        X_processed = X_processed.fillna(X_processed.mean())
        
        try:
            scores = cross_val_score(model, X_processed, y, cv=num_folds, n_jobs=-1, scoring='accuracy')
            avg_score = scores.mean()
            std_score = scores.std()
            
            cv_results = {
                'scores': scores.tolist(),
                'avg_score': avg_score,
                'std_score': std_score,
                'num_folds': num_folds
            }
            
            globals()['cv_results'] = cv_results
            globals()['avg_cross_val_score'] = avg_score
            globals()['std_cross_val_score'] = std_score
            
        except Exception as e:
            try:
                scores = cross_val_score(model, X_processed, y, cv=num_folds, n_jobs=-1, scoring='neg_mean_squared_error')
                avg_score = scores.mean()
                std_score = scores.std()
                
                cv_results = {
                    'scores': scores.tolist(),
                    'avg_score': avg_score,
                    'std_score': std_score,
                    'scoring': 'neg_mean_squared_error',
                    'num_folds': num_folds
                }
                
                globals()['cv_results'] = cv_results
                globals()['avg_cross_val_score'] = avg_score
                globals()['std_cross_val_score'] = std_score
                
            except Exception as e2:
                print(f"Cross-validation failed: {str(e2)}")
                globals()['cv_results'] = {'error': str(e2)}
    else:
        print("Warning: Required data or label_column not available for cross-validation.")
        
else:
    print(f"Warning: Model '{model_id}' not found in models dictionary.")