# Model comparison across multiple algorithms
from sklearn.model_selection import cross_val_score
from sklearn.metrics import accuracy_score, f1_score, precision_score, recall_score
import pandas as pd
import numpy as np

# Parameters
algorithms = {algorithms}
cv_folds = {cv_folds}
label_column = '{label_column}'

try:
    # Prepare the data
    if 'train_data' in globals() and label_column in train_data.columns:
        # Prepare features and target
        feature_columns = [col for col in train_data.columns if col != label_column]
        X = train_data[feature_columns]
        y = train_data[label_column]
        
        # Handle categorical features by converting to dummy variables
        X_processed = pd.get_dummies(X)
        X_processed = X_processed.fillna(X_processed.mean())
        
    elif 'data' in globals() and label_column in data.columns:
        # Same logic for single data DataFrame
        feature_columns = [col for col in data.columns if col != label_column]
        X = data[feature_columns]
        y = data[label_column]
        
        X_processed = pd.get_dummies(X)
        X_processed = X_processed.fillna(X_processed.mean())
        
    else:
        print(f"Error: Required data or label column '{label_column}' not available")
        model_comparison_successful = False
        globals()['model_comparison_successful'] = model_comparison_successful
        exit()
    
    # Results storage
    comparison_results = {}
    
    # Compare each algorithm
    for algorithm_info in algorithms:
        algorithm_name = algorithm_info.get('name', 'Unknown')
        algorithm_module = algorithm_info.get('module', 'ensemble')
        algorithm_params = algorithm_info.get('params', {})
        
        try:
            # Import the algorithm dynamically
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
            
            # Create model with parameters
            model = algorithm_class(**algorithm_params)
            
            # Perform cross-validation
            try:
                cv_scores = cross_val_score(model, X_processed, y, cv=cv_folds, scoring='accuracy', n_jobs=-1)
                mean_score = cv_scores.mean()
                std_score = cv_scores.std()
                
                # Fit model for additional metrics
                model.fit(X_processed, y)
                y_pred = model.predict(X_processed)
                
                # Calculate additional metrics
                accuracy = accuracy_score(y, y_pred)
                
                try:
                    f1 = f1_score(y, y_pred, average='weighted')
                    precision = precision_score(y, y_pred, average='weighted')
                    recall = recall_score(y, y_pred, average='weighted')
                except:
                    f1 = precision = recall = 0.0
                
                # Store results
                comparison_results[algorithm_name] = {
                    'cv_mean_score': mean_score,
                    'cv_std_score': std_score,
                    'cv_scores': cv_scores.tolist(),
                    'accuracy': accuracy,
                    'f1_score': f1,
                    'precision': precision,
                    'recall': recall,
                    'parameters': algorithm_params,
                    'model': model
                }
                
                print(f"{algorithm_name}: CV Score = {mean_score:.4f} (+/- {std_score * 2:.4f})")
                
            except Exception as cv_error:
                print(f"Error in cross-validation for {algorithm_name}: {str(cv_error)}")
                comparison_results[algorithm_name] = {
                    'error': str(cv_error),
                    'cv_mean_score': 0.0,
                    'cv_std_score': 0.0
                }
                
        except Exception as import_error:
            print(f"Error importing {algorithm_name} from {algorithm_module}: {str(import_error)}")
            comparison_results[algorithm_name] = {
                'error': str(import_error),
                'cv_mean_score': 0.0,
                'cv_std_score': 0.0
            }
    
    # Find best performing algorithm
    best_algorithm = None
    best_score = -1
    
    for alg_name, results in comparison_results.items():
        if 'error' not in results and results['cv_mean_score'] > best_score:
            best_score = results['cv_mean_score']
            best_algorithm = alg_name
    
    # Store results
    globals()['model_comparison_results'] = comparison_results
    globals()['best_algorithm'] = best_algorithm
    globals()['best_cv_score'] = best_score
    
    # Create comparison DataFrame for easy viewing
    comparison_df_data = []
    for alg_name, results in comparison_results.items():
        if 'error' not in results:
            comparison_df_data.append({
                'Algorithm': alg_name,
                'CV_Mean_Score': results['cv_mean_score'],
                'CV_Std_Score': results['cv_std_score'],
                'Accuracy': results.get('accuracy', 0.0),
                'F1_Score': results.get('f1_score', 0.0),
                'Precision': results.get('precision', 0.0),
                'Recall': results.get('recall', 0.0)
            })
    
    if comparison_df_data:
        comparison_df = pd.DataFrame(comparison_df_data)
        comparison_df = comparison_df.sort_values('CV_Mean_Score', ascending=False)
        globals()['model_comparison_df'] = comparison_df
        
        print(f"\nModel Comparison Results:")
        print(f"{'Algorithm':<20} {'CV Score':<12} {'Std Dev':<10} {'Accuracy':<10}")
        print("-" * 60)
        for _, row in comparison_df.head(10).iterrows():
            print(f"{row['Algorithm']:<20} {row['CV_Mean_Score']:<12.4f} {row['CV_Std_Score']:<10.4f} {row['Accuracy']:<10.4f}")
    
    if best_algorithm:
        print(f"\nBest performing algorithm: {best_algorithm} (CV Score: {best_score:.4f})")
        
        # Store the best model
        if 'models' not in globals():
            models = {}
        models['best_model'] = comparison_results[best_algorithm]['model']
        globals()['models'] = models
    
    model_comparison_successful = True
    
except Exception as e:
    print(f"Error during model comparison: {str(e)}")
    model_comparison_successful = False

# Store the result
globals()['model_comparison_successful'] = model_comparison_successful