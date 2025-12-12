/**
 * Algorithm Nodes
 * Machine learning algorithm nodes with configurable parameters
 */

// Helper function to generate standard training code pattern
function generateTrainingCode(node, context, algorithmImport, algorithmClass, paramStr) {
    let code = `# ${algorithmClass} Node (${node.id})\n`;
    code += `# Input: X_train_scaled or X_train (from previous node)\n`;
    code += `# Output: model (trained ${algorithmClass})\n\n`;
    code += `${algorithmImport}\n`;
    code += 'import pandas as pd\n';
    code += 'import numpy as np\n\n';
    
    // Use scaled data with fallback logic
    code += '# Get training data from pipeline\n';
    code += 'try:\n';
    code += '    X_train_input = X_train_scaled\n';
    code += '    X_test_input = X_test_scaled\n';
    code += "    print('Using scaled data for training')\n";
    code += 'except NameError:\n';
    code += '    X_train_input = X_train\n';
    code += '    X_test_input = X_test\n';
    code += "    print('Using unscaled data for training')\n\n";
    
    code += '# Ensure only numeric columns are used\n';
    code += 'if isinstance(X_train_input, pd.DataFrame):\n';
    code += '    numeric_cols = X_train_input.select_dtypes(include=[np.number]).columns.tolist()\n';
    code += '    if len(numeric_cols) < X_train_input.shape[1]:\n';
    code += "        print(f'Dropping {X_train_input.shape[1] - len(numeric_cols)} non-numeric columns')\n";
    code += '        X_train_input = X_train_input[numeric_cols]\n';
    code += '        X_test_input = X_test_input[numeric_cols]\n\n';
    
    code += `model = ${algorithmClass}(${paramStr})\n`;
    code += 'model.fit(X_train_input, y_train)\n';
    code += `print(f'${algorithmClass} trained: {X_train_input.shape[0]} samples, {X_train_input.shape[1]} features')\n`;
    
    // Register output in context
    if (context && context.setVariable) {
        context.setVariable(node.id, 'model');
    }
    
    return code;
}

const AlgorithmNodes = {
    // Classification Algorithms
    randomForestClassifier: {
        type: 'algo_random_forest_classifier',
        name: 'Random Forest',
        category: 'algorithms-classification',
        icon: 'bi-diagram-3',
        color: '#1976d2',
        description: 'Random Forest Classifier',
        imports: 'from sklearn.ensemble import RandomForestClassifier',
        defaults: {
            n_estimators: 100,
            max_depth: null,
            min_samples_split: 2,
            min_samples_leaf: 1,
            random_state: 42,
            n_jobs: -1
        },
        properties: [
            BaseNode.createProperty('n_estimators', 'N Estimators', 'number', {
                default: 100,
                min: 1,
                max: 1000,
                help: 'Number of trees in the forest'
            }),
            BaseNode.createProperty('max_depth', 'Max Depth', 'number', {
                default: null,
                min: 1,
                help: 'Maximum depth of trees (null for unlimited)'
            }),
            BaseNode.createProperty('min_samples_split', 'Min Samples Split', 'number', {
                default: 2,
                min: 2,
                help: 'Minimum samples required to split a node'
            }),
            BaseNode.createProperty('min_samples_leaf', 'Min Samples Leaf', 'number', {
                default: 1,
                min: 1,
                help: 'Minimum samples required at a leaf node'
            }),
            BaseNode.createProperty('random_state', 'Random State', 'number', {
                default: 42,
                help: 'Random seed for reproducibility'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const params = [];
            
            if (data.n_estimators) params.push(`n_estimators=${data.n_estimators}`);
            if (data.max_depth !== null && data.max_depth !== undefined) params.push(`max_depth=${data.max_depth}`);
            if (data.min_samples_split) params.push(`min_samples_split=${data.min_samples_split}`);
            if (data.min_samples_leaf) params.push(`min_samples_leaf=${data.min_samples_leaf}`);
            if (data.random_state !== null && data.random_state !== undefined) params.push(`random_state=${data.random_state}`);
            
            const paramStr = params.length > 0 ? params.join(', ') : '';
            
            // Get input from previous node via context
            const inputVar = context.getInputVariable ? context.getInputVariable(node) : 'X_train_scaled';
            
            let code = `# Random Forest Classifier Node (${node.id})\n`;
            code += `# Input: ${inputVar} (from previous node)\n`;
            code += `# Output: model (trained classifier)\n\n`;
            code += 'from sklearn.ensemble import RandomForestClassifier\n';
            code += 'import pandas as pd\n';
            code += 'import numpy as np\n\n';
            
            // Use the input from context, with fallback logic
            code += `# Get training data from pipeline\n`;
            code += 'try:\n';
            code += '    X_train_input = X_train_scaled  # Prefer scaled data\n';
            code += '    X_test_input = X_test_scaled\n';
            code += "    print('Using scaled data for training')\n";
            code += 'except NameError:\n';
            code += '    X_train_input = X_train  # Fall back to unscaled\n';
            code += '    X_test_input = X_test\n';
            code += "    print('Using unscaled data for training')\n\n";
            
            // Ensure only numeric data is used
            code += '# Ensure only numeric columns are used\n';
            code += 'if isinstance(X_train_input, pd.DataFrame):\n';
            code += '    numeric_cols = X_train_input.select_dtypes(include=[np.number]).columns.tolist()\n';
            code += '    if len(numeric_cols) < X_train_input.shape[1]:\n';
            code += "        print(f'Dropping {X_train_input.shape[1] - len(numeric_cols)} non-numeric columns')\n";
            code += '        X_train_input = X_train_input[numeric_cols]\n';
            code += '        X_test_input = X_test_input[numeric_cols]\n\n';
            
            code += `model = RandomForestClassifier(${paramStr})\n`;
            code += 'model.fit(X_train_input, y_train)\n';
            code += "print(f'Model trained: {X_train_input.shape[0]} samples, {X_train_input.shape[1]} features')\n";
            
            // Register output in context for downstream nodes
            context.setVariable(node.id, 'model');
            
            return code;
        }
    },

    decisionTreeClassifier: {
        type: 'algo_decision_tree_classifier',
        name: 'Decision Tree',
        category: 'algorithms-classification',
        icon: 'bi-diagram-2',
        color: '#0277bd',
        description: 'Decision Tree Classifier',
        defaults: {
            max_depth: null,
            min_samples_split: 2,
            min_samples_leaf: 1,
            random_state: 42
        },
        properties: [
            BaseNode.createProperty('max_depth', 'Max Depth', 'number', {
                default: null,
                min: 1,
                help: 'Maximum depth of the tree'
            }),
            BaseNode.createProperty('min_samples_split', 'Min Samples Split', 'number', {
                default: 2,
                min: 2
            }),
            BaseNode.createProperty('min_samples_leaf', 'Min Samples Leaf', 'number', {
                default: 1,
                min: 1
            }),
            BaseNode.createProperty('random_state', 'Random State', 'number', {
                default: 42
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const params = [];
            
            if (data.max_depth !== null && data.max_depth !== undefined) params.push(`max_depth=${data.max_depth}`);
            if (data.min_samples_split) params.push(`min_samples_split=${data.min_samples_split}`);
            if (data.min_samples_leaf) params.push(`min_samples_leaf=${data.min_samples_leaf}`);
            if (data.random_state !== null && data.random_state !== undefined) params.push(`random_state=${data.random_state}`);
            
            const paramStr = params.length > 0 ? params.join(', ') : '';
            
            let code = `# Decision Tree Classifier Node (${node.id})\n`;
            code += `# Input: X_train_scaled or X_train (from previous node)\n`;
            code += `# Output: model (trained classifier)\n\n`;
            code += 'from sklearn.tree import DecisionTreeClassifier\n';
            code += 'import pandas as pd\n';
            code += 'import numpy as np\n\n';
            
            // Use scaled data with fallback logic
            code += '# Get training data from pipeline\n';
            code += 'try:\n';
            code += '    X_train_input = X_train_scaled\n';
            code += '    X_test_input = X_test_scaled\n';
            code += "    print('Using scaled data for training')\n";
            code += 'except NameError:\n';
            code += '    X_train_input = X_train\n';
            code += '    X_test_input = X_test\n';
            code += "    print('Using unscaled data for training')\n\n";
            
            code += '# Ensure only numeric columns are used\n';
            code += 'if isinstance(X_train_input, pd.DataFrame):\n';
            code += '    numeric_cols = X_train_input.select_dtypes(include=[np.number]).columns.tolist()\n';
            code += '    if len(numeric_cols) < X_train_input.shape[1]:\n';
            code += "        print(f'Dropping {X_train_input.shape[1] - len(numeric_cols)} non-numeric columns')\n";
            code += '        X_train_input = X_train_input[numeric_cols]\n';
            code += '        X_test_input = X_test_input[numeric_cols]\n\n';
            
            code += `model = DecisionTreeClassifier(${paramStr})\n`;
            code += 'model.fit(X_train_input, y_train)\n';
            code += "print(f'Model trained: {X_train_input.shape[0]} samples, {X_train_input.shape[1]} features')\n";
            
            // Register output in context
            if (context && context.setVariable) {
                context.setVariable(node.id, 'model');
            }
            
            return code;
        }
    },

    svmClassifier: {
        type: 'algo_svm_classifier',
        name: 'SVM',
        category: 'algorithms-classification',
        icon: 'bi-shield-check',
        color: '#e65100',
        description: 'Support Vector Machine',
        defaults: {
            C: 1.0,
            kernel: 'rbf',
            gamma: 'scale',
            random_state: 42
        },
        properties: [
            BaseNode.createProperty('C', 'C (Regularization)', 'number', {
                default: 1.0,
                min: 0.01,
                max: 100,
                step: 0.1,
                help: 'Regularization parameter'
            }),
            BaseNode.createProperty('kernel', 'Kernel', 'select', {
                default: 'rbf',
                options: ['linear', 'poly', 'rbf', 'sigmoid']
            }),
            BaseNode.createProperty('gamma', 'Gamma', 'select', {
                default: 'scale',
                options: ['scale', 'auto'],
                help: 'Kernel coefficient'
            }),
            BaseNode.createProperty('random_state', 'Random State', 'number', {
                default: 42
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const params = [];
            
            if (data.C) params.push(`C=${data.C}`);
            if (data.kernel) params.push(`kernel='${data.kernel}'`);
            if (data.gamma) params.push(`gamma='${data.gamma}'`);
            if (data.random_state !== null && data.random_state !== undefined) params.push(`random_state=${data.random_state}`);
            
            const paramStr = params.join(', ');
            return generateTrainingCode(node, context, 'from sklearn.svm import SVC', 'SVC', paramStr);
        }
    },

    logisticRegression: {
        type: 'algo_logistic_regression',
        name: 'Logistic Regression',
        category: 'algorithms-classification',
        icon: 'bi-graph-up-arrow',
        color: '#2e7d32',
        description: 'Logistic Regression Classifier',
        defaults: {
            C: 1.0,
            penalty: 'l2',
            solver: 'lbfgs',
            max_iter: 100,
            random_state: 42
        },
        properties: [
            BaseNode.createProperty('C', 'C (Inverse Regularization)', 'number', {
                default: 1.0,
                min: 0.01,
                max: 100,
                step: 0.1
            }),
            BaseNode.createProperty('penalty', 'Penalty', 'select', {
                default: 'l2',
                options: ['l1', 'l2', 'elasticnet', 'none']
            }),
            BaseNode.createProperty('solver', 'Solver', 'select', {
                default: 'lbfgs',
                options: ['lbfgs', 'liblinear', 'newton-cg', 'sag', 'saga']
            }),
            BaseNode.createProperty('max_iter', 'Max Iterations', 'number', {
                default: 100,
                min: 1,
                max: 10000
            }),
            BaseNode.createProperty('random_state', 'Random State', 'number', {
                default: 42
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const params = [];
            
            if (data.C) params.push(`C=${data.C}`);
            if (data.penalty) params.push(`penalty='${data.penalty}'`);
            if (data.solver) params.push(`solver='${data.solver}'`);
            if (data.max_iter) params.push(`max_iter=${data.max_iter}`);
            if (data.random_state !== null && data.random_state !== undefined) params.push(`random_state=${data.random_state}`);
            
            const paramStr = params.join(', ');
            return generateTrainingCode(node, context, 'from sklearn.linear_model import LogisticRegression', 'LogisticRegression', paramStr);
        }
    },

    knnClassifier: {
        type: 'algo_knn_classifier',
        name: 'K-Nearest Neighbors',
        category: 'algorithms-classification',
        icon: 'bi-people',
        color: '#c2185b',
        description: 'KNN Classifier',
        defaults: {
            n_neighbors: 5,
            weights: 'uniform',
            algorithm: 'auto'
        },
        properties: [
            BaseNode.createProperty('n_neighbors', 'N Neighbors', 'number', {
                default: 5,
                min: 1,
                max: 100,
                help: 'Number of neighbors to use'
            }),
            BaseNode.createProperty('weights', 'Weights', 'select', {
                default: 'uniform',
                options: ['uniform', 'distance']
            }),
            BaseNode.createProperty('algorithm', 'Algorithm', 'select', {
                default: 'auto',
                options: ['auto', 'ball_tree', 'kd_tree', 'brute']
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const params = [];
            
            if (data.n_neighbors) params.push(`n_neighbors=${data.n_neighbors}`);
            if (data.weights) params.push(`weights='${data.weights}'`);
            if (data.algorithm) params.push(`algorithm='${data.algorithm}'`);
            
            const paramStr = params.join(', ');
            return generateTrainingCode(node, context, 'from sklearn.neighbors import KNeighborsClassifier', 'KNeighborsClassifier', paramStr);
        }
    },

    naiveBayes: {
        type: 'algo_naive_bayes',
        name: 'Naive Bayes',
        category: 'algorithms-classification',
        icon: 'bi-lightning',
        color: '#7b1fa2',
        description: 'Naive Bayes Classifier',
        defaults: {
            alpha: 1.0,
            fit_prior: true
        },
        properties: [
            BaseNode.createProperty('alpha', 'Alpha (Smoothing)', 'number', {
                default: 1.0,
                min: 0,
                max: 10,
                step: 0.1,
                help: 'Additive smoothing parameter'
            }),
            BaseNode.createProperty('fit_prior', 'Fit Prior', 'boolean', {
                default: true,
                help: 'Whether to learn class prior probabilities'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const params = [];
            
            if (data.alpha !== null && data.alpha !== undefined) params.push(`alpha=${data.alpha}`);
            if (data.fit_prior !== null && data.fit_prior !== undefined) params.push(`fit_prior=${data.fit_prior}`);
            
            const paramStr = params.join(', ');
            
            // Use GaussianNB for numeric data (MultinomialNB requires non-negative features)
            return generateTrainingCode(node, context, 'from sklearn.naive_bayes import GaussianNB', 'GaussianNB', paramStr);
        }
    },

    // Regression Algorithms
    randomForestRegressor: {
        type: 'algo_random_forest_regressor',
        name: 'Random Forest',
        category: 'algorithms-regression',
        icon: 'bi-graph-up',
        color: '#1976d2',
        description: 'Random Forest Regressor',
        defaults: {
            n_estimators: 100,
            max_depth: null,
            min_samples_split: 2,
            min_samples_leaf: 1,
            random_state: 42,
            n_jobs: -1
        },
        properties: [
            BaseNode.createProperty('n_estimators', 'N Estimators', 'number', {
                default: 100,
                min: 1,
                max: 1000
            }),
            BaseNode.createProperty('max_depth', 'Max Depth', 'number', {
                default: null,
                min: 1
            }),
            BaseNode.createProperty('min_samples_split', 'Min Samples Split', 'number', {
                default: 2,
                min: 2
            }),
            BaseNode.createProperty('min_samples_leaf', 'Min Samples Leaf', 'number', {
                default: 1,
                min: 1
            }),
            BaseNode.createProperty('random_state', 'Random State', 'number', {
                default: 42
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const params = [];
            
            if (data.n_estimators) params.push(`n_estimators=${data.n_estimators}`);
            if (data.max_depth !== null && data.max_depth !== undefined) params.push(`max_depth=${data.max_depth}`);
            if (data.min_samples_split) params.push(`min_samples_split=${data.min_samples_split}`);
            if (data.min_samples_leaf) params.push(`min_samples_leaf=${data.min_samples_leaf}`);
            if (data.random_state !== null && data.random_state !== undefined) params.push(`random_state=${data.random_state}`);
            
            const paramStr = params.join(', ');
            return generateTrainingCode(node, context, 'from sklearn.ensemble import RandomForestRegressor', 'RandomForestRegressor', paramStr);
        }
    },

    linearRegression: {
        type: 'algo_linear_regression',
        name: 'Linear Regression',
        category: 'algorithms-regression',
        icon: 'bi-graph-up-arrow',
        color: '#2e7d32',
        description: 'Linear Regression',
        defaults: {
            fit_intercept: true,
            normalize: false,
            n_jobs: null
        },
        properties: [
            BaseNode.createProperty('fit_intercept', 'Fit Intercept', 'boolean', {
                default: true,
                help: 'Whether to calculate the intercept'
            }),
            BaseNode.createProperty('normalize', 'Normalize', 'boolean', {
                default: false,
                help: 'Whether to normalize features'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const params = [];
            
            if (data.fit_intercept !== null && data.fit_intercept !== undefined) params.push(`fit_intercept=${data.fit_intercept}`);
            
            const paramStr = params.join(', ');
            return generateTrainingCode(node, context, 'from sklearn.linear_model import LinearRegression', 'LinearRegression', paramStr);
        }
    },

    ridgeRegression: {
        type: 'algo_ridge_regression',
        name: 'Ridge Regression',
        category: 'algorithms-regression',
        icon: 'bi-graph-up',
        color: '#0277bd',
        description: 'Ridge Regression (L2)',
        defaults: {
            alpha: 1.0,
            fit_intercept: true,
            normalize: false,
            solver: 'auto'
        },
        properties: [
            BaseNode.createProperty('alpha', 'Alpha (Regularization)', 'number', {
                default: 1.0,
                min: 0,
                max: 100,
                step: 0.1,
                help: 'Regularization strength'
            }),
            BaseNode.createProperty('fit_intercept', 'Fit Intercept', 'boolean', {
                default: true
            }),
            BaseNode.createProperty('normalize', 'Normalize', 'boolean', {
                default: false
            }),
            BaseNode.createProperty('solver', 'Solver', 'select', {
                default: 'auto',
                options: ['auto', 'svd', 'cholesky', 'lsqr', 'sparse_cg', 'sag', 'saga']
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context.getInputVariable(node);
            const params = [];
            
            if (data.alpha) params.push(`alpha=${data.alpha}`);
            if (data.fit_intercept !== null && data.fit_intercept !== undefined) params.push(`fit_intercept=${data.fit_intercept}`);
            if (data.normalize !== null && data.normalize !== undefined) params.push(`normalize=${data.normalize}`);
            if (data.solver) params.push(`solver='${data.solver}'`);
            
            const paramStr = params.length > 0 ? ', ' + params.join(', ') : '';
            const yVar = 'y_train';
            
            let code = 'from sklearn.linear_model import Ridge\n';
            code += `model = Ridge(${paramStr})\n`;
            code += `model.fit(${inputVar}, ${yVar})\n`;
            code += `print(f'Ridge trained on {${inputVar}.shape[0]} samples')\n`;
            
            if (context) {
                context.setVariable(node.id, 'model');
            }
            
            return code;
        }
    },

    lassoRegression: {
        type: 'algo_lasso_regression',
        name: 'Lasso Regression',
        category: 'algorithms-regression',
        icon: 'bi-graph-up',
        color: '#e65100',
        description: 'Lasso Regression (L1)',
        defaults: {
            alpha: 1.0,
            fit_intercept: true,
            normalize: false,
            max_iter: 1000
        },
        properties: [
            BaseNode.createProperty('alpha', 'Alpha (Regularization)', 'number', {
                default: 1.0,
                min: 0,
                max: 100,
                step: 0.1
            }),
            BaseNode.createProperty('fit_intercept', 'Fit Intercept', 'boolean', {
                default: true
            }),
            BaseNode.createProperty('normalize', 'Normalize', 'boolean', {
                default: false
            }),
            BaseNode.createProperty('max_iter', 'Max Iterations', 'number', {
                default: 1000,
                min: 1,
                max: 10000
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context.getInputVariable(node);
            const params = [];
            
            if (data.alpha) params.push(`alpha=${data.alpha}`);
            if (data.fit_intercept !== null && data.fit_intercept !== undefined) params.push(`fit_intercept=${data.fit_intercept}`);
            if (data.normalize !== null && data.normalize !== undefined) params.push(`normalize=${data.normalize}`);
            if (data.max_iter) params.push(`max_iter=${data.max_iter}`);
            
            const paramStr = params.length > 0 ? ', ' + params.join(', ') : '';
            const yVar = 'y_train';
            
            let code = 'from sklearn.linear_model import Lasso\n';
            code += `model = Lasso(${paramStr})\n`;
            code += `model.fit(${inputVar}, ${yVar})\n`;
            code += `print(f'Lasso trained on {${inputVar}.shape[0]} samples')\n`;
            
            if (context) {
                context.setVariable(node.id, 'model');
            }
            
            return code;
        }
    },

    svmRegressor: {
        type: 'algo_svm_regressor',
        name: 'SVM Regressor',
        category: 'algorithms-regression',
        icon: 'bi-shield-check',
        color: '#c2185b',
        description: 'Support Vector Regression',
        defaults: {
            C: 1.0,
            kernel: 'rbf',
            gamma: 'scale',
            epsilon: 0.1
        },
        properties: [
            BaseNode.createProperty('C', 'C (Regularization)', 'number', {
                default: 1.0,
                min: 0.01,
                max: 100,
                step: 0.1
            }),
            BaseNode.createProperty('kernel', 'Kernel', 'select', {
                default: 'rbf',
                options: ['linear', 'poly', 'rbf', 'sigmoid']
            }),
            BaseNode.createProperty('gamma', 'Gamma', 'select', {
                default: 'scale',
                options: ['scale', 'auto']
            }),
            BaseNode.createProperty('epsilon', 'Epsilon', 'number', {
                default: 0.1,
                min: 0,
                max: 1,
                step: 0.01,
                help: 'Epsilon in the epsilon-SVR model'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context.getInputVariable(node);
            const params = [];
            
            if (data.C) params.push(`C=${data.C}`);
            if (data.kernel) params.push(`kernel='${data.kernel}'`);
            if (data.gamma) params.push(`gamma='${data.gamma}'`);
            if (data.epsilon) params.push(`epsilon=${data.epsilon}`);
            
            const paramStr = params.length > 0 ? ', ' + params.join(', ') : '';
            const yVar = 'y_train';
            
            let code = 'from sklearn.svm import SVR\n';
            code += `model = SVR(${paramStr})\n`;
            code += `model.fit(${inputVar}, ${yVar})\n`;
            code += `print(f'SVR trained on {${inputVar}.shape[0]} samples')\n`;
            
            if (context) {
                context.setVariable(node.id, 'model');
            }
            
            return code;
        }
    }
};

// Register all algorithm nodes (safe registration)
if (typeof registerNodesSafely !== 'undefined') {
    registerNodesSafely(AlgorithmNodes, 'algorithm');
} else if (typeof BaseNode !== 'undefined' && typeof nodeRegistry !== 'undefined') {
    Object.values(AlgorithmNodes).forEach(nodeDef => {
        try {
            BaseNode.validateDefinition(nodeDef);
            nodeRegistry.register(nodeDef);
        } catch (error) {
            console.error(`Failed to register node ${nodeDef.type}:`, error);
        }
    });
} else {
    console.warn('Dependencies not ready for algorithms.js');
}

// Export
if (typeof module !== 'undefined' && module.exports) {
    module.exports = AlgorithmNodes;
}

