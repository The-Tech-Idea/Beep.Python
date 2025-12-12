/**
 * Scikit-learn Imputation Nodes
 * Missing value imputation and handling
 */

const SklearnImputationNodes = {
    simpleImputer: {
        type: 'sklearn_simple_imputer',
        name: 'Simple Imputer',
        category: 'sklearn-imputation',
        icon: 'bi-arrow-down-up',
        color: '#2e7d32',
        description: 'Imputation transformer for completing missing values',
        defaults: {
            strategy: 'mean',
            missing_values: 'nan',
            fill_value: null,
            copy: true
        },
        properties: [
            BaseNode.createProperty('strategy', 'Strategy', 'select', {
                default: 'mean',
                options: ['mean', 'median', 'most_frequent', 'constant'],
                help: 'Imputation strategy'
            }),
            BaseNode.createProperty('fill_value', 'Fill Value', 'text', {
                placeholder: 'Leave empty for auto',
                help: 'Value to use when strategy is "constant"'
            }),
            BaseNode.createProperty('copy', 'Copy', 'boolean', {
                default: true
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'X';
            const outputVar = inputVar + '_imputed';
            const strategy = data.strategy || 'mean';
            const fillValue = data.fill_value;
            const copy = data.copy !== false;
            
            let code = 'from sklearn.impute import SimpleImputer\n';
            const params = [`strategy='${strategy}'`, `copy=${copy}`];
            if (fillValue) {
                params.push(`fill_value=${fillValue}`);
            }
            
            code += `imputer = SimpleImputer(${params.join(', ')})\n`;
            code += `${outputVar} = imputer.fit_transform(${inputVar})\n`;
            code += `print(f'Imputed using strategy: {strategy}')\n`;
            
            if (context) {
                context.setVariable(node.id, outputVar);
            }
            
            return code;
        }
    },

    kNNImputer: {
        type: 'sklearn_knn_imputer',
        name: 'KNN Imputer',
        category: 'sklearn-imputation',
        icon: 'bi-people',
        color: '#1976d2',
        description: 'Imputation for completing missing values using k-Nearest Neighbors',
        defaults: {
            n_neighbors: 5,
            weights: 'uniform',
            metric: 'nan_euclidean'
        },
        properties: [
            BaseNode.createProperty('n_neighbors', 'N Neighbors', 'number', {
                default: 5,
                min: 1,
                max: 20,
                help: 'Number of neighboring samples to use'
            }),
            BaseNode.createProperty('weights', 'Weights', 'select', {
                default: 'uniform',
                options: ['uniform', 'distance'],
                help: 'Weight function used in prediction'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'X';
            const outputVar = inputVar + '_imputed';
            const nNeighbors = data.n_neighbors || 5;
            const weights = data.weights || 'uniform';
            
            let code = 'from sklearn.impute import KNNImputer\n';
            code += `imputer = KNNImputer(n_neighbors=${nNeighbors}, weights='${weights}')\n`;
            code += `${outputVar} = imputer.fit_transform(${inputVar})\n`;
            
            if (context) {
                context.setVariable(node.id, outputVar);
            }
            
            return code;
        }
    },

    iterativeImputer: {
        type: 'sklearn_iterative_imputer',
        name: 'Iterative Imputer',
        category: 'sklearn-imputation',
        icon: 'bi-arrow-repeat',
        color: '#e65100',
        description: 'Multivariate imputer that estimates each feature from all the others',
        defaults: {
            estimator: null,
            missing_values: 'nan',
            max_iter: 10,
            random_state: null
        },
        properties: [
            BaseNode.createProperty('max_iter', 'Max Iterations', 'number', {
                default: 10,
                min: 1,
                max: 100,
                help: 'Maximum number of imputation rounds'
            }),
            BaseNode.createProperty('random_state', 'Random State', 'number', {
                default: null,
                placeholder: '42 or leave empty',
                help: 'Random seed for reproducibility'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'X';
            const outputVar = inputVar + '_imputed';
            const maxIter = data.max_iter || 10;
            const randomState = data.random_state !== null && data.random_state !== undefined ? data.random_state : null;
            
            let code = 'from sklearn.impute import IterativeImputer\n';
            const params = [`max_iter=${maxIter}`];
            if (randomState !== null) {
                params.push(`random_state=${randomState}`);
            }
            
            code += `imputer = IterativeImputer(${params.join(', ')})\n`;
            code += `${outputVar} = imputer.fit_transform(${inputVar})\n`;
            
            if (context) {
                context.setVariable(node.id, outputVar);
            }
            
            return code;
        }
    }
};

// Register all sklearn imputation nodes
Object.values(SklearnImputationNodes).forEach(nodeDef => {
    BaseNode.validateDefinition(nodeDef);
    nodeRegistry.register(nodeDef);
});

// Export
if (typeof module !== 'undefined' && module.exports) {
    module.exports = SklearnImputationNodes;
}

