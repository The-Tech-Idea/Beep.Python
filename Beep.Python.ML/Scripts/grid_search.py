from sklearn.model_selection import GridSearchCV
from sklearn.{algorithm_module} import {algorithm_name}
import json

try:
    # Get features and target
    X = data.drop('{label_column}', axis=1) if '{label_column}' in data.columns else data
    y = data['{label_column}'] if '{label_column}' in data.columns else None
    
    if y is not None:
        model = {algorithm_name}()
        param_grid = {parameter_grid}
        
        grid_search = GridSearchCV(
            model, 
            param_grid, 
            cv={cv_folds}, 
            scoring='accuracy', 
            n_jobs=-1
        )
        
        grid_search.fit(X, y)
        
        optimization_result = {
            'success': True,
            'best_params': grid_search.best_params_,
            'best_score': float(grid_search.best_score_)
        }
    else:
        optimization_result = {
            'success': False,
            'error': 'No target column found'
        }
        
except Exception as e:
    optimization_result = {
        'success': False,
        'error': str(e)
    }