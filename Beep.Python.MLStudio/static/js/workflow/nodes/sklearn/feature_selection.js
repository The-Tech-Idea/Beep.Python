/**
 * Scikit-learn Feature Selection Nodes
 * Feature selection and dimensionality reduction
 */

const SklearnFeatureSelectionNodes = {
    selectKBest: {
        type: 'sklearn_select_k_best',
        name: 'Select K Best',
        category: 'sklearn-feature-selection',
        icon: 'bi-star',
        color: '#1976d2',
        description: 'Select features according to the k highest scores',
        defaults: {
            k: 10,
            score_func: 'f_classif'
        },
        properties: [
            BaseNode.createProperty('k', 'K (Number of Features)', 'number', {
                default: 10,
                min: 1,
                max: 1000,
                help: 'Number of top features to select'
            }),
            BaseNode.createProperty('score_func', 'Score Function', 'select', {
                default: 'f_classif',
                options: ['f_classif', 'mutual_info_classif', 'chi2', 'f_regression', 'mutual_info_regression'],
                help: 'Function to compute feature scores'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'X';
            const outputVar = inputVar + '_selected';
            const k = data.k || 10;
            const scoreFunc = data.score_func || 'f_classif';
            
            let code = 'from sklearn.feature_selection import SelectKBest, ';
            if (scoreFunc.includes('classif')) {
                code += scoreFunc.replace('_classif', '') + '_classif\n';
            } else {
                code += scoreFunc.replace('_regression', '') + '_regression\n';
            }
            
            code += `selector = SelectKBest(k=${k})\n`;
            code += `${outputVar} = selector.fit_transform(${inputVar}, y)\n`;
            code += `print(f'Selected {${outputVar}.shape[1]} features out of {${inputVar}.shape[1]}')\n`;
            
            if (context) {
                context.setVariable(node.id, outputVar);
            }
            
            return code;
        }
    },

    selectPercentile: {
        type: 'sklearn_select_percentile',
        name: 'Select Percentile',
        category: 'sklearn-feature-selection',
        icon: 'bi-percent',
        color: '#0277bd',
        description: 'Select features according to a percentile of the highest scores',
        defaults: {
            percentile: 10,
            score_func: 'f_classif'
        },
        properties: [
            BaseNode.createProperty('percentile', 'Percentile', 'number', {
                default: 10,
                min: 1,
                max: 100,
                help: 'Percent of features to keep'
            }),
            BaseNode.createProperty('score_func', 'Score Function', 'select', {
                default: 'f_classif',
                options: ['f_classif', 'mutual_info_classif', 'chi2', 'f_regression', 'mutual_info_regression']
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'X';
            const outputVar = inputVar + '_selected';
            const percentile = data.percentile || 10;
            const scoreFunc = data.score_func || 'f_classif';
            
            let code = 'from sklearn.feature_selection import SelectPercentile, ';
            if (scoreFunc.includes('classif')) {
                code += scoreFunc.replace('_classif', '') + '_classif\n';
            } else {
                code += scoreFunc.replace('_regression', '') + '_regression\n';
            }
            
            code += `selector = SelectPercentile(percentile=${percentile})\n`;
            code += `${outputVar} = selector.fit_transform(${inputVar}, y)\n`;
            
            if (context) {
                context.setVariable(node.id, outputVar);
            }
            
            return code;
        }
    },

    rfe: {
        type: 'sklearn_rfe',
        name: 'Recursive Feature Elimination',
        category: 'sklearn-feature-selection',
        icon: 'bi-arrow-down',
        color: '#e65100',
        description: 'Feature ranking with recursive feature elimination',
        defaults: {
            n_features_to_select: null,
            step: 1
        },
        properties: [
            BaseNode.createProperty('n_features_to_select', 'N Features to Select', 'number', {
                default: null,
                placeholder: 'Leave empty for auto',
                help: 'Number of features to select (None for 50% of features)'
            }),
            BaseNode.createProperty('step', 'Step', 'number', {
                default: 1,
                min: 1,
                max: 10,
                help: 'Number of features to remove at each iteration'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'X';
            const outputVar = inputVar + '_selected';
            const nFeatures = data.n_features_to_select !== null && data.n_features_to_select !== undefined ? data.n_features_to_select : null;
            const step = data.step || 1;
            
            let code = 'from sklearn.feature_selection import RFE\n';
            code += 'from sklearn.ensemble import RandomForestClassifier\n';
            code += `estimator = RandomForestClassifier(n_estimators=100, random_state=42)\n`;
            
            const params = [`estimator=estimator`, `step=${step}`];
            if (nFeatures !== null) {
                params.push(`n_features_to_select=${nFeatures}`);
            }
            
            code += `selector = RFE(${params.join(', ')})\n`;
            code += `${outputVar} = selector.fit_transform(${inputVar}, y)\n`;
            
            if (context) {
                context.setVariable(node.id, outputVar);
            }
            
            return code;
        }
    },

    varianceThreshold: {
        type: 'sklearn_variance_threshold',
        name: 'Variance Threshold',
        category: 'sklearn-feature-selection',
        icon: 'bi-graph-up',
        color: '#2e7d32',
        description: 'Feature selector that removes all low-variance features',
        defaults: {
            threshold: 0.0
        },
        properties: [
            BaseNode.createProperty('threshold', 'Threshold', 'number', {
                default: 0.0,
                min: 0,
                step: 0.01,
                help: 'Features with variance lower than this will be removed'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'X';
            const outputVar = inputVar + '_selected';
            const threshold = data.threshold !== undefined ? data.threshold : 0.0;
            
            let code = 'from sklearn.feature_selection import VarianceThreshold\n';
            code += `selector = VarianceThreshold(threshold=${threshold})\n`;
            code += `${outputVar} = selector.fit_transform(${inputVar})\n`;
            code += `print(f'Removed {${inputVar}.shape[1] - ${outputVar}.shape[1]} low-variance features')\n`;
            
            if (context) {
                context.setVariable(node.id, outputVar);
            }
            
            return code;
        }
    }
};

// Register all sklearn feature selection nodes
Object.values(SklearnFeatureSelectionNodes).forEach(nodeDef => {
    BaseNode.validateDefinition(nodeDef);
    nodeRegistry.register(nodeDef);
});

// Export
if (typeof module !== 'undefined' && module.exports) {
    module.exports = SklearnFeatureSelectionNodes;
}

