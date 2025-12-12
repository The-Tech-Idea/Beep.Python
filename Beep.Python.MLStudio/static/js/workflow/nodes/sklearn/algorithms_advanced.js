/**
 * Advanced Scikit-learn Algorithm Nodes
 * Additional ML algorithms with full parameter configuration
 */

const SklearnAdvancedAlgorithmsNodes = {
    gradientBoostingClassifier: {
        type: 'sklearn_gradient_boosting_classifier',
        name: 'Gradient Boosting',
        category: 'algorithms-classification',
        icon: 'bi-diagram-3',
        color: '#1976d2',
        description: 'Gradient Boosting Classifier',
        defaults: {
            n_estimators: 100,
            learning_rate: 0.1,
            max_depth: 3,
            min_samples_split: 2,
            min_samples_leaf: 1,
            random_state: 42
        },
        properties: [
            BaseNode.createProperty('n_estimators', 'N Estimators', 'number', {
                default: 100,
                min: 1,
                max: 1000
            }),
            BaseNode.createProperty('learning_rate', 'Learning Rate', 'number', {
                default: 0.1,
                min: 0.01,
                max: 1,
                step: 0.01
            }),
            BaseNode.createProperty('max_depth', 'Max Depth', 'number', {
                default: 3,
                min: 1,
                max: 20
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
            const inputVar = context ? context.getInputVariable(node) : 'X_train';
            const params = [];
            
            if (data.n_estimators) params.push(`n_estimators=${data.n_estimators}`);
            if (data.learning_rate) params.push(`learning_rate=${data.learning_rate}`);
            if (data.max_depth) params.push(`max_depth=${data.max_depth}`);
            if (data.min_samples_split) params.push(`min_samples_split=${data.min_samples_split}`);
            if (data.min_samples_leaf) params.push(`min_samples_leaf=${data.min_samples_leaf}`);
            if (data.random_state !== null && data.random_state !== undefined) params.push(`random_state=${data.random_state}`);
            
            const paramStr = params.length > 0 ? ', ' + params.join(', ') : '';
            return `from sklearn.ensemble import GradientBoostingClassifier\nmodel = GradientBoostingClassifier(${paramStr})\nmodel.fit(${inputVar}, y_train)`;
        }
    },

    adaBoostClassifier: {
        type: 'sklearn_adaboost_classifier',
        name: 'AdaBoost',
        category: 'algorithms-classification',
        icon: 'bi-lightning-charge',
        color: '#ff6b6b',
        description: 'AdaBoost Classifier',
        defaults: {
            n_estimators: 50,
            learning_rate: 1.0,
            random_state: 42
        },
        properties: [
            BaseNode.createProperty('n_estimators', 'N Estimators', 'number', {
                default: 50,
                min: 1,
                max: 500
            }),
            BaseNode.createProperty('learning_rate', 'Learning Rate', 'number', {
                default: 1.0,
                min: 0.01,
                max: 2,
                step: 0.1
            }),
            BaseNode.createProperty('random_state', 'Random State', 'number', {
                default: 42
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'X_train';
            const params = [];
            
            if (data.n_estimators) params.push(`n_estimators=${data.n_estimators}`);
            if (data.learning_rate) params.push(`learning_rate=${data.learning_rate}`);
            if (data.random_state !== null && data.random_state !== undefined) params.push(`random_state=${data.random_state}`);
            
            const paramStr = params.length > 0 ? ', ' + params.join(', ') : '';
            return `from sklearn.ensemble import AdaBoostClassifier\nmodel = AdaBoostClassifier(${paramStr})\nmodel.fit(${inputVar}, y_train)`;
        }
    },

    xgboostClassifier: {
        type: 'sklearn_xgboost_classifier',
        name: 'XGBoost',
        category: 'algorithms-classification',
        icon: 'bi-speedometer2',
        color: '#28a745',
        description: 'XGBoost Classifier (requires xgboost package)',
        defaults: {
            n_estimators: 100,
            max_depth: 6,
            learning_rate: 0.1,
            random_state: 42
        },
        properties: [
            BaseNode.createProperty('n_estimators', 'N Estimators', 'number', {
                default: 100,
                min: 1,
                max: 1000
            }),
            BaseNode.createProperty('max_depth', 'Max Depth', 'number', {
                default: 6,
                min: 1,
                max: 20
            }),
            BaseNode.createProperty('learning_rate', 'Learning Rate', 'number', {
                default: 0.1,
                min: 0.01,
                max: 1,
                step: 0.01
            }),
            BaseNode.createProperty('random_state', 'Random State', 'number', {
                default: 42
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'X_train';
            const params = [];
            
            if (data.n_estimators) params.push(`n_estimators=${data.n_estimators}`);
            if (data.max_depth) params.push(`max_depth=${data.max_depth}`);
            if (data.learning_rate) params.push(`learning_rate=${data.learning_rate}`);
            if (data.random_state !== null && data.random_state !== undefined) params.push(`random_state=${data.random_state}`);
            
            const paramStr = params.length > 0 ? ', ' + params.join(', ') : '';
            return `from xgboost import XGBClassifier\nmodel = XGBClassifier(${paramStr})\nmodel.fit(${inputVar}, y_train)`;
        }
    },

    lightgbmClassifier: {
        type: 'sklearn_lightgbm_classifier',
        name: 'LightGBM',
        category: 'algorithms-classification',
        icon: 'bi-lightning',
        color: '#ffc107',
        description: 'LightGBM Classifier (requires lightgbm package)',
        defaults: {
            n_estimators: 100,
            max_depth: -1,
            learning_rate: 0.1,
            random_state: 42
        },
        properties: [
            BaseNode.createProperty('n_estimators', 'N Estimators', 'number', {
                default: 100,
                min: 1,
                max: 1000
            }),
            BaseNode.createProperty('max_depth', 'Max Depth', 'number', {
                default: -1,
                min: -1,
                max: 20,
                help: '-1 for no limit'
            }),
            BaseNode.createProperty('learning_rate', 'Learning Rate', 'number', {
                default: 0.1,
                min: 0.01,
                max: 1,
                step: 0.01
            }),
            BaseNode.createProperty('random_state', 'Random State', 'number', {
                default: 42
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'X_train';
            const params = [];
            
            if (data.n_estimators) params.push(`n_estimators=${data.n_estimators}`);
            if (data.max_depth !== undefined) params.push(`max_depth=${data.max_depth}`);
            if (data.learning_rate) params.push(`learning_rate=${data.learning_rate}`);
            if (data.random_state !== null && data.random_state !== undefined) params.push(`random_state=${data.random_state}`);
            
            const paramStr = params.length > 0 ? ', ' + params.join(', ') : '';
            return `from lightgbm import LGBMClassifier\nmodel = LGBMClassifier(${paramStr})\nmodel.fit(${inputVar}, y_train)`;
        }
    },

    elasticNet: {
        type: 'sklearn_elastic_net',
        name: 'Elastic Net',
        category: 'algorithms-regression',
        icon: 'bi-graph-up',
        color: '#17a2b8',
        description: 'Elastic Net Regression (L1 + L2 regularization)',
        defaults: {
            alpha: 1.0,
            l1_ratio: 0.5,
            fit_intercept: true,
            max_iter: 1000,
            random_state: 42
        },
        properties: [
            BaseNode.createProperty('alpha', 'Alpha (Regularization)', 'number', {
                default: 1.0,
                min: 0,
                max: 100,
                step: 0.1
            }),
            BaseNode.createProperty('l1_ratio', 'L1 Ratio', 'number', {
                default: 0.5,
                min: 0,
                max: 1,
                step: 0.1,
                help: '0 = Ridge, 1 = Lasso'
            }),
            BaseNode.createProperty('fit_intercept', 'Fit Intercept', 'boolean', {
                default: true
            }),
            BaseNode.createProperty('max_iter', 'Max Iterations', 'number', {
                default: 1000,
                min: 1,
                max: 10000
            }),
            BaseNode.createProperty('random_state', 'Random State', 'number', {
                default: 42
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'X_train';
            const params = [];
            
            if (data.alpha) params.push(`alpha=${data.alpha}`);
            if (data.l1_ratio !== undefined) params.push(`l1_ratio=${data.l1_ratio}`);
            if (data.fit_intercept !== null && data.fit_intercept !== undefined) params.push(`fit_intercept=${data.fit_intercept}`);
            if (data.max_iter) params.push(`max_iter=${data.max_iter}`);
            if (data.random_state !== null && data.random_state !== undefined) params.push(`random_state=${data.random_state}`);
            
            const paramStr = params.length > 0 ? ', ' + params.join(', ') : '';
            return `from sklearn.linear_model import ElasticNet\nmodel = ElasticNet(${paramStr})\nmodel.fit(${inputVar}, y_train)`;
        }
    }
};

// Register all advanced sklearn algorithm nodes
Object.values(SklearnAdvancedAlgorithmsNodes).forEach(nodeDef => {
    BaseNode.validateDefinition(nodeDef);
    nodeRegistry.register(nodeDef);
});

// Export
if (typeof module !== 'undefined' && module.exports) {
    module.exports = SklearnAdvancedAlgorithmsNodes;
}

