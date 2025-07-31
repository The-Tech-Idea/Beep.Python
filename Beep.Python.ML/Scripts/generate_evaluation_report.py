# Generate comprehensive evaluation report
from sklearn.metrics import classification_report, confusion_matrix, roc_curve, auc, precision_recall_curve, average_precision_score
import matplotlib.pyplot as plt
import seaborn as sns
import numpy as np
import os
from datetime import datetime

# Parameters
model_id = '{model_id}'
output_html_path = '{output_html_path}'

try:
    # Check if models dictionary exists and contains the specified model
    if 'models' in globals() and model_id in models:
        model = models[model_id]
        
        # Check if we have test data
        if 'test_data' in globals() and 'label_column' in globals():
            # Prepare test data
            feature_columns = [col for col in test_data.columns if col != label_column]
            X_test = test_data[feature_columns]
            y_true = test_data[label_column]
            
            # Handle categorical features
            X_test_processed = pd.get_dummies(X_test)
            X_test_processed = X_test_processed.fillna(X_test_processed.mean())
            
            # Make predictions
            y_pred = model.predict(X_test_processed)
            
            # Get probabilities if available
            y_score = None
            if hasattr(model, 'predict_proba'):
                y_score = model.predict_proba(X_test_processed)[:, 1]
            elif hasattr(model, 'decision_function'):
                y_score = model.decision_function(X_test_processed)
                
        elif 'X_test' in globals() and 'y_test' in globals():
            # Use existing test data
            y_true = y_test
            y_pred = model.predict(X_test)
            
            y_score = None
            if hasattr(model, 'predict_proba'):
                y_score = model.predict_proba(X_test)[:, 1]
            elif hasattr(model, 'decision_function'):
                y_score = model.decision_function(X_test)
                
        else:
            print("Warning: No test data available for evaluation report")
            report_generated = False
            globals()['report_generated'] = report_generated
            exit()
        
        # Create output directory
        output_dir = os.path.dirname(output_html_path)
        if output_dir and not os.path.exists(output_dir):
            os.makedirs(output_dir)
        
        # Generate plots
        plot_paths = {}
        
        # 1. Confusion Matrix
        cm = confusion_matrix(y_true, y_pred)
        plt.figure(figsize=(8, 6))
        sns.heatmap(cm, annot=True, fmt='d', cmap='Blues')
        plt.title(f'Confusion Matrix - {model_id}')
        plt.ylabel('True Label')
        plt.xlabel('Predicted Label')
        cm_path = os.path.join(output_dir, f'{model_id}_confusion_matrix.png')
        plt.savefig(cm_path, dpi=300, bbox_inches='tight')
        plt.close()
        plot_paths['confusion_matrix'] = os.path.basename(cm_path)
        
        # 2. ROC Curve (if probabilities available)
        if y_score is not None:
            fpr, tpr, _ = roc_curve(y_true, y_score)
            roc_auc = auc(fpr, tpr)
            
            plt.figure(figsize=(8, 6))
            plt.plot(fpr, tpr, color='darkorange', lw=2, label=f'ROC curve (AUC = {roc_auc:.2f})')
            plt.plot([0, 1], [0, 1], color='navy', lw=2, linestyle='--')
            plt.xlim([0.0, 1.0])
            plt.ylim([0.0, 1.05])
            plt.xlabel('False Positive Rate')
            plt.ylabel('True Positive Rate')
            plt.title(f'ROC Curve - {model_id}')
            plt.legend(loc="lower right")
            plt.grid(True, alpha=0.3)
            roc_path = os.path.join(output_dir, f'{model_id}_roc_curve.png')
            plt.savefig(roc_path, dpi=300, bbox_inches='tight')
            plt.close()
            plot_paths['roc_curve'] = os.path.basename(roc_path)
            
            # 3. Precision-Recall Curve
            precision, recall, _ = precision_recall_curve(y_true, y_score)
            avg_precision = average_precision_score(y_true, y_score)
            
            plt.figure(figsize=(8, 6))
            plt.plot(recall, precision, color='b', lw=2, label=f'PR curve (AP = {avg_precision:.2f})')
            plt.xlabel('Recall')
            plt.ylabel('Precision')
            plt.title(f'Precision-Recall Curve - {model_id}')
            plt.legend(loc="lower left")
            plt.grid(True, alpha=0.3)
            pr_path = os.path.join(output_dir, f'{model_id}_precision_recall.png')
            plt.savefig(pr_path, dpi=300, bbox_inches='tight')
            plt.close()
            plot_paths['precision_recall'] = os.path.basename(pr_path)
        
        # 4. Feature Importance (if available)
        if hasattr(model, 'feature_importances_'):
            importances = model.feature_importances_
            feature_names = X_test_processed.columns if 'X_test_processed' in locals() else [f'Feature_{i}' for i in range(len(importances))]
            
            # Sort by importance
            indices = np.argsort(importances)[::-1]
            top_n = min(20, len(importances))
            
            plt.figure(figsize=(12, 8))
            plt.title(f'Top {top_n} Feature Importances - {model_id}')
            plt.bar(range(top_n), importances[indices[:top_n]])
            plt.xticks(range(top_n), [feature_names[i] for i in indices[:top_n]], rotation=45, ha='right')
            plt.tight_layout()
            fi_path = os.path.join(output_dir, f'{model_id}_feature_importance.png')
            plt.savefig(fi_path, dpi=300, bbox_inches='tight')
            plt.close()
            plot_paths['feature_importance'] = os.path.basename(fi_path)
        
        # Generate classification report
        class_report = classification_report(y_true, y_pred, output_dict=True)
        accuracy = class_report['accuracy']
        
        # Create HTML report
        html_content = f"""
<!DOCTYPE html>
<html>
<head>
    <title>Model Evaluation Report - {model_id}</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 40px; }}
        .header {{ background-color: #f0f0f0; padding: 20px; border-radius: 5px; }}
        .metrics {{ display: flex; justify-content: space-around; margin: 20px 0; }}
        .metric {{ text-align: center; padding: 10px; background-color: #e8f4f8; border-radius: 5px; }}
        .plot {{ text-align: center; margin: 20px 0; }}
        .plot img {{ max-width: 100%; height: auto; }}
        table {{ border-collapse: collapse; width: 100%; margin: 20px 0; }}
        th, td {{ border: 1px solid #ddd; padding: 8px; text-align: left; }}
        th {{ background-color: #f2f2f2; }}
    </style>
</head>
<body>
    <div class="header">
        <h1>Model Evaluation Report</h1>
        <p><strong>Model ID:</strong> {model_id}</p>
        <p><strong>Generated:</strong> {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}</p>
        <p><strong>Test Samples:</strong> {len(y_true)}</p>
    </div>
    
    <div class="metrics">
        <div class="metric">
            <h3>Accuracy</h3>
            <h2>{accuracy:.4f}</h2>
        </div>
        """
        
        if y_score is not None:
            html_content += f"""
        <div class="metric">
            <h3>AUC</h3>
            <h2>{roc_auc:.4f}</h2>
        </div>
        <div class="metric">
            <h3>Avg Precision</h3>
            <h2>{avg_precision:.4f}</h2>
        </div>
            """
        
        html_content += """
    </div>
    """
        
        # Add plots
        for plot_name, plot_file in plot_paths.items():
            html_content += f"""
    <div class="plot">
        <h2>{plot_name.replace('_', ' ').title()}</h2>
        <img src="{plot_file}" alt="{plot_name}">
    </div>
            """
        
        # Add classification report table
        html_content += """
    <h2>Classification Report</h2>
    <table>
        <tr><th>Class</th><th>Precision</th><th>Recall</th><th>F1-Score</th><th>Support</th></tr>
        """
        
        for class_name, metrics in class_report.items():
            if class_name not in ['accuracy', 'macro avg', 'weighted avg']:
                html_content += f"""
        <tr>
            <td>{class_name}</td>
            <td>{metrics['precision']:.4f}</td>
            <td>{metrics['recall']:.4f}</td>
            <td>{metrics['f1-score']:.4f}</td>
            <td>{metrics['support']}</td>
        </tr>
                """
        
        # Add averages
        for avg_type in ['macro avg', 'weighted avg']:
            if avg_type in class_report:
                metrics = class_report[avg_type]
                html_content += f"""
        <tr style="background-color: #f9f9f9;">
            <td><strong>{avg_type}</strong></td>
            <td><strong>{metrics['precision']:.4f}</strong></td>
            <td><strong>{metrics['recall']:.4f}</strong></td>
            <td><strong>{metrics['f1-score']:.4f}</strong></td>
            <td><strong>{metrics['support']}</strong></td>
        </tr>
                """
        
        html_content += """
    </table>
</body>
</html>
        """
        
        # Save HTML report
        with open(output_html_path, 'w', encoding='utf-8') as f:
            f.write(html_content)
        
        # Store results
        globals()['evaluation_report'] = {
            'model_id': model_id,
            'accuracy': accuracy,
            'classification_report': class_report,
            'confusion_matrix': cm.tolist(),
            'plot_paths': plot_paths,
            'html_path': output_html_path
        }
        
        if y_score is not None:
            globals()['evaluation_report']['auc'] = roc_auc
            globals()['evaluation_report']['avg_precision'] = avg_precision
        
        print(f"Comprehensive evaluation report generated for model '{model_id}'")
        print(f"Accuracy: {accuracy:.4f}")
        if y_score is not None:
            print(f"AUC: {roc_auc:.4f}")
            print(f"Average Precision: {avg_precision:.4f}")
        print(f"Report saved to: {output_html_path}")
        report_generated = True
        
    else:
        print(f"Warning: Model '{model_id}' not found in models dictionary")
        report_generated = False
        
except Exception as e:
    print(f"Error generating evaluation report: {str(e)}")
    report_generated = False

# Store the result
globals()['report_generated'] = report_generated