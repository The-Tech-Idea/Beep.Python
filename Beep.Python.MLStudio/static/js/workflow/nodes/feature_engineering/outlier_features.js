/**
 * Outlier Detection Feature Engineering Nodes
 * Create features based on outlier detection
 */

const OutlierFeatureNodes = {
    iqrOutlierFeatures: {
        type: 'fe_iqr_outlier',
        name: 'IQR Outlier Features',
        category: 'feature-engineering',
        icon: 'bi-exclamation-triangle',
        color: '#dc3545',
        description: 'Create features indicating outliers using IQR method',
        defaults: {
            columns: '',
            multiplier: 1.5
        },
        properties: [
            BaseNode.createProperty('columns', 'Columns', 'text', {
                required: true,
                placeholder: 'col1, col2'
            }),
            BaseNode.createProperty('multiplier', 'IQR Multiplier', 'number', {
                default: 1.5,
                min: 0.5,
                max: 5,
                step: 0.1,
                help: 'Multiplier for IQR (1.5 = standard, 3 = extreme outliers)'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const columns = data.columns || '';
            const multiplier = data.multiplier !== undefined ? data.multiplier : 1.5;
            
            if (!columns) return `# IQR Outlier: Columns required`;
            
            const colList = columns.split(',').map(c => c.trim());
            let code = `import numpy as np\n`;
            code += `# IQR outlier detection\n`;
            
            colList.forEach(col => {
                code += `Q1 = ${inputVar}['${col}'].quantile(0.25)\n`;
                code += `Q3 = ${inputVar}['${col}'].quantile(0.75)\n`;
                code += `IQR = Q3 - Q1\n`;
                code += `lower_bound = Q1 - ${multiplier} * IQR\n`;
                code += `upper_bound = Q3 + ${multiplier} * IQR\n`;
                code += `${inputVar}['${col}_is_outlier'] = (${inputVar}['${col}'] < lower_bound) | (${inputVar}['${col}'] > upper_bound)\n`;
                code += `${inputVar}['${col}_outlier_score'] = np.where(${inputVar}['${col}'] < lower_bound, (lower_bound - ${inputVar}['${col}']) / IQR,\n`;
                code += `                                    np.where(${inputVar}['${col}'] > upper_bound, (${inputVar}['${col}'] - upper_bound) / IQR, 0))\n`;
            });
            
            code += `print(f'Created IQR outlier features')\n`;
            
            return code;
        }
    },

    zscoreOutlierFeatures: {
        type: 'fe_zscore_outlier',
        name: 'Z-Score Outlier Features',
        category: 'feature-engineering',
        icon: 'bi-exclamation-circle',
        color: '#fd7e14',
        description: 'Create features indicating outliers using Z-score method',
        defaults: {
            columns: '',
            threshold: 3.0
        },
        properties: [
            BaseNode.createProperty('columns', 'Columns', 'text', {
                required: true,
                placeholder: 'col1, col2'
            }),
            BaseNode.createProperty('threshold', 'Z-Score Threshold', 'number', {
                default: 3.0,
                min: 1,
                max: 5,
                step: 0.1,
                help: 'Z-score threshold for outlier detection'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const columns = data.columns || '';
            const threshold = data.threshold !== undefined ? data.threshold : 3.0;
            
            if (!columns) return `# Z-Score Outlier: Columns required`;
            
            const colList = columns.split(',').map(c => c.trim());
            let code = `import numpy as np\n`;
            code += `from scipy import stats\n`;
            code += `# Z-score outlier detection\n`;
            
            colList.forEach(col => {
                code += `z_scores = np.abs(stats.zscore(${inputVar}['${col}']))\n`;
                code += `${inputVar}['${col}_is_outlier'] = z_scores > ${threshold}\n`;
                code += `${inputVar}['${col}_zscore'] = z_scores\n`;
            });
            
            code += `print(f'Created Z-score outlier features')\n`;
            
            return code;
        }
    },

    isolationForestFeatures: {
        type: 'fe_isolation_forest',
        name: 'Isolation Forest Outlier',
        category: 'feature-engineering',
        icon: 'bi-tree',
        color: '#20c997',
        description: 'Create outlier features using Isolation Forest',
        defaults: {
            columns: '',
            contamination: 0.1
        },
        properties: [
            BaseNode.createProperty('columns', 'Feature Columns', 'text', {
                required: true,
                placeholder: 'col1, col2, col3'
            }),
            BaseNode.createProperty('contamination', 'Contamination', 'number', {
                default: 0.1,
                min: 0.01,
                max: 0.5,
                step: 0.01,
                help: 'Expected proportion of outliers'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const columns = data.columns || '';
            const contamination = data.contamination !== undefined ? data.contamination : 0.1;
            
            if (!columns) return `# Isolation Forest: Columns required`;
            
            const colList = columns.split(',').map(c => c.trim());
            
            let code = 'from sklearn.ensemble import IsolationForest\n';
            code += `# Isolation Forest outlier detection\n`;
            code += `X_outlier = ${inputVar}[${colList.map(c => `'${c}'`).join(', ')}].values\n`;
            code += `iso_forest = IsolationForest(contamination=${contamination}, random_state=42)\n`;
            code += `${inputVar}['isolation_forest_outlier'] = iso_forest.fit_predict(X_outlier)\n`;
            code += `${inputVar}['isolation_forest_outlier'] = (${inputVar}['isolation_forest_outlier'] == -1).astype(int)\n`;
            code += `${inputVar}['isolation_forest_score'] = iso_forest.score_samples(X_outlier)\n`;
            code += `print(f'Created Isolation Forest outlier features')\n`;
            
            return code;
        }
    },

    localOutlierFactor: {
        type: 'fe_lof_outlier',
        name: 'Local Outlier Factor',
        category: 'feature-engineering',
        icon: 'bi-geo-alt',
        color: '#6f42c1',
        description: 'Create outlier features using Local Outlier Factor',
        defaults: {
            columns: '',
            n_neighbors: 20,
            contamination: 0.1
        },
        properties: [
            BaseNode.createProperty('columns', 'Feature Columns', 'text', {
                required: true,
                placeholder: 'col1, col2'
            }),
            BaseNode.createProperty('n_neighbors', 'N Neighbors', 'number', {
                default: 20,
                min: 2,
                max: 100
            }),
            BaseNode.createProperty('contamination', 'Contamination', 'number', {
                default: 0.1,
                min: 0.01,
                max: 0.5,
                step: 0.01
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const columns = data.columns || '';
            const nNeighbors = data.n_neighbors || 20;
            const contamination = data.contamination !== undefined ? data.contamination : 0.1;
            
            if (!columns) return `# LOF: Columns required`;
            
            const colList = columns.split(',').map(c => c.trim());
            
            let code = 'from sklearn.neighbors import LocalOutlierFactor\n';
            code += `# Local Outlier Factor\n`;
            code += `X_outlier = ${inputVar}[${colList.map(c => `'${c}'`).join(', ')}].values\n`;
            code += `lof = LocalOutlierFactor(n_neighbors=${nNeighbors}, contamination=${contamination})\n`;
            code += `${inputVar}['lof_outlier'] = lof.fit_predict(X_outlier)\n`;
            code += `${inputVar}['lof_outlier'] = (${inputVar}['lof_outlier'] == -1).astype(int)\n`;
            code += `${inputVar}['lof_score'] = -lof.negative_outlier_factor_\n`;
            code += `print(f'Created LOF outlier features')\n`;
            
            return code;
        }
    }
};

// Register all outlier feature nodes
Object.values(OutlierFeatureNodes).forEach(nodeDef => {
    BaseNode.validateDefinition(nodeDef);
    nodeRegistry.register(nodeDef);
});

// Export
if (typeof module !== 'undefined' && module.exports) {
    module.exports = OutlierFeatureNodes;
}

