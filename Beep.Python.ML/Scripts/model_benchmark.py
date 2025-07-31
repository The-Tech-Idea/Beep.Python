# Model performance comparison and benchmarking
import pandas as pd
import numpy as np
from sklearn.metrics import accuracy_score, precision_score, recall_score, f1_score, roc_auc_score
from sklearn.metrics import mean_squared_error, mean_absolute_error, r2_score
import time

# Parameters
model_ids = {model_ids}
benchmark_dataset = '{benchmark_dataset}'
performance_metrics = {performance_metrics}

try:
    # Check if models exist
    if 'models' not in globals():
        print("Error: No models dictionary found")
        model_benchmark_successful = False
        globals()['model_benchmark_successful'] = model_benchmark_successful
        exit()
    
    # Validate model IDs
    available_models = [model_id for model_id in model_ids if model_id in models]
    if not available_models:
        print(f"Error: None of the specified models found: {model_ids}")
        model_benchmark_successful = False
        globals()['model_benchmark_successful'] = model_benchmark_successful
        exit()
    
    print(f"Benchmarking {len(available_models)} models...")
    
    # Determine benchmark dataset
    if benchmark_dataset == 'test_data' and 'test_data' in globals():
        X_benchmark = test_data[[col for col in test_data.columns if col != label_column]]
        y_benchmark = test_data[label_column]
        dataset_name = "test_data"
    elif benchmark_dataset == 'validation_data' and 'validation_data' in globals():
        X_benchmark = validation_data[[col for col in validation_data.columns if col != label_column]]
        y_benchmark = validation_data[label_column]
        dataset_name = "validation_data"
    elif 'X_test' in globals() and 'y_test' in globals():
        X_benchmark = X_test
        y_benchmark = y_test
        dataset_name = "X_test, y_test"
    else:
        print("Error: No suitable benchmark dataset found")
        model_benchmark_successful = False
        globals()['model_benchmark_successful'] = model_benchmark_successful
        exit()
    
    print(f"Using benchmark dataset: {dataset_name}")
    print(f"Benchmark samples: {len(y_benchmark)}")
    
    # Prepare benchmark data
    X_benchmark_processed = pd.get_dummies(X_benchmark)
    X_benchmark_processed = X_benchmark_processed.fillna(X_benchmark_processed.mean())
    
    # Initialize benchmark results
    benchmark_results = {
        'dataset_info': {
            'name': dataset_name,
            'samples': len(y_benchmark),
            'features': X_benchmark_processed.shape[1]
        },
        'models': {},
        'comparison_summary': {},
        'rankings': {}
    }
    
    # Detect problem type
    unique_targets = len(np.unique(y_benchmark))
    problem_type = 'classification' if unique_targets <= 50 else 'regression'
    print(f"Problem type: {problem_type}")
    
    # Benchmark each model
    for model_id in available_models:
        print(f"\nBenchmarking model: {model_id}")
        
        model = models[model_id]
        model_results = {
            'model_id': model_id,
            'model_type': type(model).__name__,
            'problem_type': problem_type
        }
        
        try:
            # Align features with training data if available
            if 'X_train_processed' in globals():
                train_columns = X_train_processed.columns
                
                # Add missing columns
                for col in train_columns:
                    if col not in X_benchmark_processed.columns:
                        X_benchmark_processed[col] = 0
                
                # Remove extra columns and reorder
                X_benchmark_aligned = X_benchmark_processed[train_columns]
            else:
                X_benchmark_aligned = X_benchmark_processed
            
            # Measure prediction time
            start_time = time.time()
            y_pred = model.predict(X_benchmark_aligned)
            prediction_time = time.time() - start_time
            
            model_results['prediction_time'] = prediction_time
            model_results['predictions_per_second'] = len(y_benchmark) / prediction_time
            
            # Get probabilities if available
            y_proba = None
            if hasattr(model, 'predict_proba'):
                y_proba = model.predict_proba(X_benchmark_aligned)
            elif hasattr(model, 'decision_function'):
                y_proba = model.decision_function(X_benchmark_aligned)
            
            # Calculate metrics based on problem type
            if problem_type == 'classification':
                # Classification metrics
                metrics = {}
                
                if 'accuracy' in performance_metrics:
                    metrics['accuracy'] = accuracy_score(y_benchmark, y_pred)
                
                if 'precision' in performance_metrics:
                    metrics['precision'] = precision_score(y_benchmark, y_pred, average='weighted', zero_division=0)
                
                if 'recall' in performance_metrics:
                    metrics['recall'] = recall_score(y_benchmark, y_pred, average='weighted', zero_division=0)
                
                if 'f1_score' in performance_metrics:
                    metrics['f1_score'] = f1_score(y_benchmark, y_pred, average='weighted', zero_division=0)
                
                if 'roc_auc' in performance_metrics and y_proba is not None and unique_targets == 2:
                    try:
                        if hasattr(model, 'predict_proba'):
                            metrics['roc_auc'] = roc_auc_score(y_benchmark, y_proba[:, 1])
                        else:
                            metrics['roc_auc'] = roc_auc_score(y_benchmark, y_proba)
                    except:
                        metrics['roc_auc'] = None
                
                model_results['metrics'] = metrics
                
                # Print results
                for metric, value in metrics.items():
                    if value is not None:
                        print(f"  {metric}: {value:.4f}")
                    else:
                        print(f"  {metric}: N/A")
                
            else:
                # Regression metrics
                metrics = {}
                
                if 'mse' in performance_metrics:
                    metrics['mse'] = mean_squared_error(y_benchmark, y_pred)
                
                if 'rmse' in performance_metrics:
                    metrics['rmse'] = np.sqrt(mean_squared_error(y_benchmark, y_pred))
                
                if 'mae' in performance_metrics:
                    metrics['mae'] = mean_absolute_error(y_benchmark, y_pred)
                
                if 'r2_score' in performance_metrics:
                    metrics['r2_score'] = r2_score(y_benchmark, y_pred)
                
                model_results['metrics'] = metrics
                
                # Print results
                for metric, value in metrics.items():
                    print(f"  {metric}: {value:.4f}")
            
            print(f"  Prediction time: {prediction_time:.4f}s")
            print(f"  Predictions/sec: {model_results['predictions_per_second']:.0f}")
            
        except Exception as e:
            print(f"  Error benchmarking model {model_id}: {str(e)}")
            model_results['error'] = str(e)
        
        benchmark_results['models'][model_id] = model_results
    
    # Create comparison summary
    print(f"\n=== Benchmark Summary ===")
    
    # Create comparison DataFrame
    comparison_data = []
    for model_id, results in benchmark_results['models'].items():
        if 'error' not in results:
            row = {'Model': model_id, 'Type': results['model_type']}
            row.update(results['metrics'])
            row['Pred_Time'] = results['prediction_time']
            row['Pred_Per_Sec'] = results['predictions_per_second']
            comparison_data.append(row)
    
    if comparison_data:
        comparison_df = pd.DataFrame(comparison_data)
        benchmark_results['comparison_dataframe'] = comparison_df.to_dict()
        
        print(comparison_df.to_string(index=False, float_format='%.4f'))
        
        # Create rankings
        rankings = {}
        for metric in performance_metrics:
            if metric in comparison_df.columns:
                if metric in ['mse', 'mae']:  # Lower is better
                    ranked = comparison_df.nsmallest(len(comparison_df), metric)
                else:  # Higher is better
                    ranked = comparison_df.nlargest(len(comparison_df), metric)
                
                rankings[metric] = ranked[['Model', metric]].to_dict('records')
        
        # Speed ranking
        speed_ranked = comparison_df.nlargest(len(comparison_df), 'Pred_Per_Sec')
        rankings['speed'] = speed_ranked[['Model', 'Pred_Per_Sec']].to_dict('records')
        
        benchmark_results['rankings'] = rankings
        
        # Print top performers
        print(f"\n=== Top Performers ===")
        for metric, ranking in rankings.items():
            if ranking:
                top_model = ranking[0]
                print(f"{metric.upper()}: {top_model['Model']} ({top_model.get(metric, top_model.get('Pred_Per_Sec', 'N/A')):.4f})")
    
    # Store results
    globals()['benchmark_results'] = benchmark_results
    globals()['comparison_df'] = comparison_df if comparison_data else None
    
    print(f"\nModel benchmarking completed successfully!")
    model_benchmark_successful = True
    
except Exception as e:
    print(f"Error during model benchmarking: {str(e)}")
    model_benchmark_successful = False

# Store the result
globals()['model_benchmark_successful'] = model_benchmark_successful