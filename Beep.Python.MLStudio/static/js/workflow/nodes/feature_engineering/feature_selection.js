/**
 * Feature Selection Nodes
 * Select and filter features based on various criteria
 */

const FeatureSelectionNodes = {
    selectByImportance: {
        type: 'fe_select_by_importance',
        name: 'Select by Feature Importance',
        category: 'feature-engineering',
        icon: 'bi-star',
        color: '#ffc107',
        description: 'Select top features based on importance scores',
        defaults: {
            n_features: 10,
            importance_method: 'random_forest'
        },
        properties: [
            BaseNode.createProperty('n_features', 'Number of Features', 'number', {
                default: 10,
                min: 1,
                max: 1000,
                help: 'Number of top features to select'
            }),
            BaseNode.createProperty('importance_method', 'Importance Method', 'select', {
                default: 'random_forest',
                options: ['random_forest', 'mutual_info', 'chi2', 'f_test'],
                help: 'Method to calculate feature importance'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'X';
            const outputVar = inputVar + '_selected';
            const nFeatures = data.n_features || 10;
            const method = data.importance_method || 'random_forest';
            
            let code = '';
            if (method === 'random_forest') {
                code += 'from sklearn.ensemble import RandomForestClassifier\n';
                code += `model = RandomForestClassifier(n_estimators=100, random_state=42)\n`;
                code += `model.fit(${inputVar}, y)\n`;
                code += `importances = model.feature_importances_\n`;
                code += `indices = np.argsort(importances)[::-1][:${nFeatures}]\n`;
                code += `${outputVar} = ${inputVar}[:, indices]\n`;
            } else if (method === 'mutual_info') {
                code += 'from sklearn.feature_selection import SelectKBest, mutual_info_classif\n';
                code += `selector = SelectKBest(mutual_info_classif, k=${nFeatures})\n`;
                code += `${outputVar} = selector.fit_transform(${inputVar}, y)\n`;
            } else if (method === 'chi2') {
                code += 'from sklearn.feature_selection import SelectKBest, chi2\n';
                code += `selector = SelectKBest(chi2, k=${nFeatures})\n`;
                code += `${outputVar} = selector.fit_transform(${inputVar}, y)\n`;
            }
            
            code += `print(f'Selected top {nFeatures} features')\n`;
            
            if (context) {
                context.setVariable(node.id, outputVar);
            }
            
            return code;
        }
    },

    removeLowVariance: {
        type: 'fe_remove_low_variance',
        name: 'Remove Low Variance',
        category: 'feature-engineering',
        icon: 'bi-x-circle',
        color: '#dc3545',
        description: 'Remove features with low variance',
        defaults: {
            threshold: 0.0
        },
        properties: [
            BaseNode.createProperty('threshold', 'Variance Threshold', 'number', {
                default: 0.0,
                min: 0,
                max: 1,
                step: 0.01,
                help: 'Features with variance below this will be removed'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'X';
            const outputVar = inputVar + '_filtered';
            const threshold = data.threshold !== undefined ? data.threshold : 0.0;
            
            let code = 'from sklearn.feature_selection import VarianceThreshold\n';
            code += `selector = VarianceThreshold(threshold=${threshold})\n`;
            code += `${outputVar} = selector.fit_transform(${inputVar})\n`;
            code += `print(f'Removed low variance features: {${inputVar}.shape[1]} -> {${outputVar}.shape[1]}')\n`;
            
            if (context) {
                context.setVariable(node.id, outputVar);
            }
            
            return code;
        }
    },

    removeCorrelated: {
        type: 'fe_remove_correlated',
        name: 'Remove Correlated Features',
        category: 'feature-engineering',
        icon: 'bi-link-45deg',
        color: '#6c757d',
        description: 'Remove highly correlated features',
        defaults: {
            threshold: 0.95
        },
        properties: [
            BaseNode.createProperty('threshold', 'Correlation Threshold', 'number', {
                default: 0.95,
                min: 0,
                max: 1,
                step: 0.01,
                help: 'Remove features with correlation above this value'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const outputVar = inputVar + '_filtered';
            const threshold = data.threshold !== undefined ? data.threshold : 0.95;
            
            let code = `import numpy as np\n`;
            code += `# Remove highly correlated features\n`;
            code += `corr_matrix = ${inputVar}.select_dtypes(include=[np.number]).corr().abs()\n`;
            code += `upper_tri = corr_matrix.where(np.triu(np.ones(corr_matrix.shape), k=1).astype(bool))\n`;
            code += `to_drop = [column for column in upper_tri.columns if any(upper_tri[column] > ${threshold})]\n`;
            code += `${outputVar} = ${inputVar}.drop(columns=to_drop)\n`;
            code += `print(f'Removed {len(to_drop)} highly correlated features')\n`;
            
            if (context) {
                context.setVariable(node.id, outputVar);
            }
            
            return code;
        }
    },

    selectByPValue: {
        type: 'fe_select_by_pvalue',
        name: 'Select by P-Value',
        category: 'feature-engineering',
        icon: 'bi-check-circle',
        color: '#28a745',
        description: 'Select features based on statistical significance (p-value)',
        defaults: {
            alpha: 0.05
        },
        properties: [
            BaseNode.createProperty('alpha', 'Alpha (Significance Level)', 'number', {
                default: 0.05,
                min: 0.001,
                max: 0.1,
                step: 0.001,
                help: 'Features with p-value below this will be selected'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'X';
            const outputVar = inputVar + '_selected';
            const alpha = data.alpha !== undefined ? data.alpha : 0.05;
            
            let code = 'from sklearn.feature_selection import f_classif, SelectFdr\n';
            code += `selector = SelectFdr(f_classif, alpha=${alpha})\n`;
            code += `${outputVar} = selector.fit_transform(${inputVar}, y)\n`;
            code += `print(f'Selected features with p-value < {alpha}')\n`;
            code += `print(f'Selected {${outputVar}.shape[1]} out of {${inputVar}.shape[1]} features')\n`;
            
            if (context) {
                context.setVariable(node.id, outputVar);
            }
            
            return code;
        }
    }
};

// Register all feature selection nodes
Object.values(FeatureSelectionNodes).forEach(nodeDef => {
    BaseNode.validateDefinition(nodeDef);
    nodeRegistry.register(nodeDef);
});

// Export
if (typeof module !== 'undefined' && module.exports) {
    module.exports = FeatureSelectionNodes;
}

