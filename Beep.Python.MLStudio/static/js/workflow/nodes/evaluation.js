/**
 * Evaluation Nodes
 * Model evaluation and metrics calculation
 */

const EvaluationNodes = {
    calculateMetrics: {
        type: 'evaluate_metrics',
        name: 'Calculate Metrics',
        category: 'evaluation',
        icon: 'bi-bar-chart',
        color: '#0277bd',
        description: 'Calculate accuracy, MSE, R²',
        defaults: {
            metrics: ['accuracy', 'precision', 'recall', 'f1']
        },
        properties: [
            BaseNode.createProperty('metrics', 'Metrics', 'text', {
                default: 'accuracy, precision, recall, f1',
                placeholder: 'accuracy, precision, recall, f1',
                help: 'Comma-separated list of metrics to calculate'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const metrics = (data.metrics || 'accuracy, precision, recall, f1').split(',').map(m => m.trim());
            
            // Get model from context (from connected algorithm node)
            const inputVar = context.getInputVariable ? context.getInputVariable(node) : 'model';
            const modelVar = (inputVar && inputVar !== 'undefined') ? inputVar : 'model';
            
            let code = `# Calculate Metrics Node (${node.id})\n`;
            code += `# Input: ${modelVar} (trained model from algorithm node)\n`;
            code += `# Output: Calculated metrics printed to console\n\n`;
            
            code += 'from sklearn.metrics import ';
            const imports = [];
            if (metrics.includes('accuracy')) imports.push('accuracy_score');
            if (metrics.includes('precision')) imports.push('precision_score');
            if (metrics.includes('recall')) imports.push('recall_score');
            if (metrics.includes('f1')) imports.push('f1_score');
            if (metrics.includes('mse') || metrics.includes('mean_squared_error')) imports.push('mean_squared_error');
            if (metrics.includes('r2') || metrics.includes('r2_score')) imports.push('r2_score');
            
            code += imports.join(', ') + '\n';
            code += 'import pandas as pd\n';
            code += 'import numpy as np\n\n';
            
            // Check if model exists
            code += '# Verify model is available (requires Algorithm node connected before this node)\n';
            code += `if '${modelVar}' not in dir() or ${modelVar} is None:\n`;
            code += `    raise ValueError("ERROR: No model found! You must add an Algorithm node (e.g., Random Forest Classifier) before Calculate Metrics. The model variable '${modelVar}' is not defined.")\n\n`;
            
            // Use the same data that was used for training
            code += '# Use the same data format as training\n';
            code += 'try:\n';
            code += '    X_test_eval = X_test_input  # Use the data that model was trained on\n';
            code += 'except NameError:\n';
            code += '    try:\n';
            code += '        X_test_eval = X_test_scaled  # Use scaled test data if available\n';
            code += '    except NameError:\n';
            code += '        X_test_eval = X_test  # Fallback to original test data\n';
            code += '        # Filter to numeric columns if needed\n';
            code += '        if isinstance(X_test_eval, pd.DataFrame):\n';
            code += '            numeric_cols = X_test_eval.select_dtypes(include=[np.number]).columns.tolist()\n';
            code += '            X_test_eval = X_test_eval[numeric_cols]\n\n';
            
            code += '# Make predictions using the trained model\n';
            code += `y_pred = ${modelVar}.predict(X_test_eval)\n`;
            code += 'print(f"Predictions made on {len(y_pred)} test samples")\n\n';
            
            if (metrics.includes('accuracy')) {
                code += 'accuracy = accuracy_score(y_test, y_pred)\n';
                code += 'print(f"Accuracy: {accuracy:.4f}")\n';
            }
            if (metrics.includes('precision')) {
                code += 'precision = precision_score(y_test, y_pred, average="weighted", zero_division=0)\n';
                code += 'print(f"Precision: {precision:.4f}")\n';
            }
            if (metrics.includes('recall')) {
                code += 'recall = recall_score(y_test, y_pred, average="weighted", zero_division=0)\n';
                code += 'print(f"Recall: {recall:.4f}")\n';
            }
            if (metrics.includes('f1')) {
                code += 'f1 = f1_score(y_test, y_pred, average="weighted", zero_division=0)\n';
                code += 'print(f"F1-Score: {f1:.4f}")\n';
            }
            if (metrics.includes('mse') || metrics.includes('mean_squared_error')) {
                code += 'mse = mean_squared_error(y_test, y_pred)\n';
                code += 'print(f"MSE: {mse:.4f}")\n';
            }
            if (metrics.includes('r2') || metrics.includes('r2_score')) {
                code += 'r2 = r2_score(y_test, y_pred)\n';
                code += 'print(f"R^2: {r2:.4f}")\n';
            }
            
            if (context) {
                context.setVariable(node.id, 'metrics');
            }
            
            return code;
        }
    },

    confusionMatrix: {
        type: 'evaluate_confusion_matrix',
        name: 'Confusion Matrix',
        category: 'evaluation',
        icon: 'bi-grid-3x3',
        color: '#e65100',
        description: 'Generate confusion matrix',
        defaults: {
            normalize: null,
            labels: null
        },
        properties: [
            BaseNode.createProperty('normalize', 'Normalize', 'select', {
                default: null,
                options: [null, 'true', 'pred', 'all'],
                help: 'Normalize confusion matrix'
            }),
            BaseNode.createProperty('labels', 'Labels', 'text', {
                placeholder: 'Leave empty for auto',
                help: 'List of labels (comma-separated)'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context.getInputVariable ? context.getInputVariable(node) : 'model';
            const modelVar = (inputVar && inputVar !== 'undefined') ? inputVar : 'model';
            const normalize = data.normalize;
            const labels = data.labels;
            
            let code = `# Confusion Matrix Node (${node.id})\n`;
            code += `# Input: ${modelVar} (trained model from algorithm node)\n`;
            code += `# Output: cm (confusion matrix)\n\n`;
            code += 'from sklearn.metrics import confusion_matrix, ConfusionMatrixDisplay\n';
            code += 'import matplotlib.pyplot as plt\n\n';
            
            // Check if model exists
            code += '# Verify model is available\n';
            code += `if '${modelVar}' not in dir() or ${modelVar} is None:\n`;
            code += `    raise ValueError("ERROR: No model found! Add an Algorithm node before Confusion Matrix.")\n\n`;
            
            code += '# Get test data from pipeline\n';
            code += 'try:\n';
            code += '    X_test_eval = X_test_input  # Use what model was trained on\n';
            code += 'except NameError:\n';
            code += '    try:\n';
            code += '        X_test_eval = X_test_scaled\n';
            code += '    except NameError:\n';
            code += '        X_test_eval = X_test\n\n';
            
            code += `y_pred = ${modelVar}.predict(X_test_eval)\n`;
            
            const params = [];
            if (normalize) params.push(`normalize='${normalize}'`);
            if (labels) {
                const labelList = labels.split(',').map(l => l.trim()).map(l => `'${l}'`).join(', ');
                params.push(`labels=[${labelList}]`);
            }
            
            const paramStr = params.length > 0 ? ', ' + params.join(', ') : '';
            code += `cm = confusion_matrix(y_test, y_pred${paramStr})\n`;
            code += 'print(f"Confusion Matrix:\\n{cm}")\n';
            
            // Register output
            context.setVariable(node.id, 'cm');
            
            return code;
        }
    },

    crossValidation: {
        type: 'evaluate_cross_validation',
        name: 'Cross Validation',
        category: 'evaluation',
        icon: 'bi-arrow-repeat',
        color: '#2e7d32',
        description: 'K-fold cross validation',
        defaults: {
            cv: 5,
            scoring: null
        },
        properties: [
            BaseNode.createProperty('cv', 'K-Folds', 'number', {
                default: 5,
                min: 2,
                max: 20,
                help: 'Number of folds'
            }),
            BaseNode.createProperty('scoring', 'Scoring', 'select', {
                default: null,
                options: [null, 'accuracy', 'precision', 'recall', 'f1', 'roc_auc', 'neg_mean_squared_error', 'r2'],
                help: 'Scoring metric'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context.getInputVariable ? context.getInputVariable(node) : 'model';
            const modelVar = (inputVar && inputVar !== 'undefined') ? inputVar : 'model';
            const cv = data.cv || 5;
            const scoring = data.scoring;
            
            let code = `# Cross Validation Node (${node.id})\n`;
            code += `# Input: ${modelVar} (trained model from algorithm node)\n`;
            code += `# Output: cv_scores (cross-validation scores)\n\n`;
            code += 'from sklearn.model_selection import cross_val_score\n\n';
            
            // Check if model exists
            code += '# Verify model is available\n';
            code += `if '${modelVar}' not in dir() or ${modelVar} is None:\n`;
            code += `    raise ValueError("ERROR: No model found! Add an Algorithm node before Cross Validation.")\n\n`;
            
            code += '# Get training data from pipeline\n';
            code += 'try:\n';
            code += '    X_cv = X_train_input\n';
            code += 'except NameError:\n';
            code += '    try:\n';
            code += '        X_cv = X_train_scaled\n';
            code += '    except NameError:\n';
            code += '        X_cv = X_train\n\n';
            
            const scoringParam = scoring ? `, scoring='${scoring}'` : '';
            code += `cv_scores = cross_val_score(${modelVar}, X_cv, y_train, cv=${cv}${scoringParam})\n`;
            code += 'print(f"Cross-validation scores: {cv_scores}")\n';
            code += 'print(f"Mean: {cv_scores.mean():.4f} (+/- {cv_scores.std() * 2:.4f})")\n';
            
            // Register output
            context.setVariable(node.id, 'cv_scores');
            
            return code;
        }
    }
};

// Register all evaluation nodes
Object.values(EvaluationNodes).forEach(nodeDef => {
    BaseNode.validateDefinition(nodeDef);
    nodeRegistry.register(nodeDef);
});

// Export
if (typeof module !== 'undefined' && module.exports) {
    module.exports = EvaluationNodes;
}

