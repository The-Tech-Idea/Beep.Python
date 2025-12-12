/**
 * Scikit-learn Model Selection Nodes
 * Train/test splitting, cross-validation, and hyperparameter tuning
 */

const SklearnModelSelectionNodes = {
    trainTestSplit: {
        type: 'sklearn_train_test_split',
        name: 'Train/Test Split',
        category: 'sklearn-model-selection',
        icon: 'bi-scissors',
        color: '#e65100',
        description: 'Split arrays or matrices into random train and test subsets',
        defaults: {
            test_size: 0.2,
            train_size: null,
            random_state: 42,
            shuffle: true,
            stratify: null
        },
        properties: [
            BaseNode.createProperty('test_size', 'Test Size', 'number', {
                default: 0.2,
                min: 0,
                max: 1,
                step: 0.01,
                help: 'Proportion of dataset to include in test split'
            }),
            BaseNode.createProperty('random_state', 'Random State', 'number', {
                default: 42,
                help: 'Random seed for reproducibility'
            }),
            BaseNode.createProperty('shuffle', 'Shuffle', 'boolean', {
                default: true,
                help: 'Whether to shuffle before splitting'
            }),
            BaseNode.createProperty('stratify', 'Stratify', 'text', {
                placeholder: 'y or leave empty',
                help: 'Array-like for stratified split (usually y)'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'X';
            const testSize = data.test_size || 0.2;
            const randomState = data.random_state || 42;
            const shuffle = data.shuffle !== false;
            const stratify = data.stratify || 'y';
            
            let code = 'from sklearn.model_selection import train_test_split\n';
            const stratifyParam = stratify ? `, stratify=${stratify}` : '';
            code += `X_train, X_test, y_train, y_test = train_test_split(${inputVar}, y, test_size=${testSize}, random_state=${randomState}, shuffle=${shuffle}${stratifyParam})\n`;
            code += `print(f'Train shape: {X_train.shape}, Test shape: {X_test.shape}')\n`;
            
            if (context) {
                context.setVariable(node.id, 'X_train');
            }
            
            return code;
        }
    },

    kFold: {
        type: 'sklearn_kfold',
        name: 'K-Fold Cross Validation',
        category: 'sklearn-model-selection',
        icon: 'bi-arrow-repeat',
        color: '#2e7d32',
        description: 'K-Fold cross-validator',
        defaults: {
            n_splits: 5,
            shuffle: false,
            random_state: null
        },
        properties: [
            BaseNode.createProperty('n_splits', 'N Splits (K)', 'number', {
                default: 5,
                min: 2,
                max: 20,
                help: 'Number of folds'
            }),
            BaseNode.createProperty('shuffle', 'Shuffle', 'boolean', {
                default: false,
                help: 'Whether to shuffle before splitting'
            }),
            BaseNode.createProperty('random_state', 'Random State', 'number', {
                default: null,
                placeholder: '42 or leave empty'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const nSplits = data.n_splits || 5;
            const shuffle = data.shuffle || false;
            const randomState = data.random_state !== null && data.random_state !== undefined ? data.random_state : null;
            
            let code = 'from sklearn.model_selection import KFold, cross_val_score\n';
            const params = [`n_splits=${nSplits}`, `shuffle=${shuffle}`];
            if (randomState !== null) {
                params.push(`random_state=${randomState}`);
            }
            
            code += `kfold = KFold(${params.join(', ')})\n`;
            code += `# Use with cross_val_score(model, X, y, cv=kfold)\n`;
            
            return code;
        }
    },

    stratifiedKFold: {
        type: 'sklearn_stratified_kfold',
        name: 'Stratified K-Fold',
        category: 'sklearn-model-selection',
        icon: 'bi-layers',
        color: '#0277bd',
        description: 'Stratified K-Fold cross-validator',
        defaults: {
            n_splits: 5,
            shuffle: false,
            random_state: null
        },
        properties: [
            BaseNode.createProperty('n_splits', 'N Splits (K)', 'number', {
                default: 5,
                min: 2,
                max: 20
            }),
            BaseNode.createProperty('shuffle', 'Shuffle', 'boolean', {
                default: false
            }),
            BaseNode.createProperty('random_state', 'Random State', 'number', {
                default: null,
                placeholder: '42 or leave empty'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const nSplits = data.n_splits || 5;
            const shuffle = data.shuffle || false;
            const randomState = data.random_state !== null && data.random_state !== undefined ? data.random_state : null;
            
            let code = 'from sklearn.model_selection import StratifiedKFold, cross_val_score\n';
            const params = [`n_splits=${nSplits}`, `shuffle=${shuffle}`];
            if (randomState !== null) {
                params.push(`random_state=${randomState}`);
            }
            
            code += `skfold = StratifiedKFold(${params.join(', ')})\n`;
            code += `# Use with cross_val_score(model, X, y, cv=skfold)\n`;
            
            return code;
        }
    },

    gridSearchCV: {
        type: 'sklearn_grid_search_cv',
        name: 'Grid Search CV',
        category: 'sklearn-model-selection',
        icon: 'bi-grid-3x3',
        color: '#7b1fa2',
        description: 'Exhaustive search over specified parameter values for an estimator',
        defaults: {
            cv: 5,
            scoring: null,
            n_jobs: -1
        },
        properties: [
            BaseNode.createProperty('cv', 'CV Folds', 'number', {
                default: 5,
                min: 2,
                max: 20,
                help: 'Number of cross-validation folds'
            }),
            BaseNode.createProperty('scoring', 'Scoring', 'select', {
                default: null,
                options: [null, 'accuracy', 'precision', 'recall', 'f1', 'roc_auc', 'neg_mean_squared_error', 'r2'],
                help: 'Scoring metric'
            }),
            BaseNode.createProperty('n_jobs', 'N Jobs', 'number', {
                default: -1,
                help: 'Number of parallel jobs (-1 for all cores)'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const cv = data.cv || 5;
            const scoring = data.scoring || null;
            const nJobs = data.n_jobs !== undefined ? data.n_jobs : -1;
            
            let code = 'from sklearn.model_selection import GridSearchCV\n';
            code += `# Define parameter grid\n`;
            code += `param_grid = {'n_estimators': [50, 100, 200], 'max_depth': [None, 10, 20]}\n`;
            code += `# Example: grid_search = GridSearchCV(estimator, param_grid, cv=${cv}`;
            if (scoring) {
                code += `, scoring='${scoring}'`;
            }
            code += `, n_jobs=${nJobs})\n`;
            code += `# grid_search.fit(X_train, y_train)\n`;
            
            return code;
        }
    },

    randomizedSearchCV: {
        type: 'sklearn_randomized_search_cv',
        name: 'Randomized Search CV',
        category: 'sklearn-model-selection',
        icon: 'bi-shuffle',
        color: '#c2185b',
        description: 'Randomized search on hyper parameters',
        defaults: {
            n_iter: 10,
            cv: 5,
            random_state: 42,
            n_jobs: -1
        },
        properties: [
            BaseNode.createProperty('n_iter', 'N Iterations', 'number', {
                default: 10,
                min: 1,
                max: 100,
                help: 'Number of parameter settings sampled'
            }),
            BaseNode.createProperty('cv', 'CV Folds', 'number', {
                default: 5,
                min: 2,
                max: 20
            }),
            BaseNode.createProperty('random_state', 'Random State', 'number', {
                default: 42
            }),
            BaseNode.createProperty('n_jobs', 'N Jobs', 'number', {
                default: -1
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const nIter = data.n_iter || 10;
            const cv = data.cv || 5;
            const randomState = data.random_state || 42;
            const nJobs = data.n_jobs !== undefined ? data.n_jobs : -1;
            
            let code = 'from sklearn.model_selection import RandomizedSearchCV\n';
            code += `# Define parameter distribution\n`;
            code += `param_dist = {'n_estimators': [50, 100, 200], 'max_depth': [None, 10, 20]}\n`;
            code += `# Example: random_search = RandomizedSearchCV(estimator, param_dist, n_iter=${nIter}, cv=${cv}, random_state=${randomState}, n_jobs=${nJobs})\n`;
            code += `# random_search.fit(X_train, y_train)\n`;
            
            return code;
        }
    }
};

// Register all sklearn model selection nodes
Object.values(SklearnModelSelectionNodes).forEach(nodeDef => {
    BaseNode.validateDefinition(nodeDef);
    nodeRegistry.register(nodeDef);
});

// Export
if (typeof module !== 'undefined' && module.exports) {
    module.exports = SklearnModelSelectionNodes;
}

