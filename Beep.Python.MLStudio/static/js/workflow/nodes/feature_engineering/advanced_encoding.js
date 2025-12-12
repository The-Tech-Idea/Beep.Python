/**
 * Advanced Encoding Feature Engineering Nodes
 * Advanced categorical encoding techniques
 */

const AdvancedEncodingNodes = {
    hashEncoding: {
        type: 'fe_hash_encoding',
        name: 'Hash Encoding',
        category: 'feature-engineering',
        icon: 'bi-hash',
        color: '#1976d2',
        description: 'Encode high-cardinality categoricals using feature hashing',
        defaults: {
            column: '',
            n_features: 10
        },
        properties: [
            BaseNode.createProperty('column', 'Categorical Column', 'text', {
                required: true,
                placeholder: 'category'
            }),
            BaseNode.createProperty('n_features', 'Number of Features', 'number', {
                default: 10,
                min: 2,
                max: 100,
                help: 'Number of hash features to create'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const column = data.column || '';
            const nFeatures = data.n_features || 10;
            
            if (!column) return `# Hash Encoding: Column required`;
            
            let code = 'from sklearn.feature_extraction import FeatureHasher\n';
            code += `# Hash encoding for ${column}\n`;
            code += `hasher = FeatureHasher(n_features=${nFeatures}, input_type='string')\n`;
            code += `hash_features = hasher.transform(${inputVar}['${column}'].astype(str).values.reshape(-1, 1))\n`;
            code += `hash_df = pd.DataFrame(hash_features.toarray(), columns=[f'${column}_hash_{i}' for i in range(${nFeatures})])\n`;
            code += `${inputVar} = pd.concat([${inputVar}, hash_df], axis=1)\n`;
            code += `print(f'Created {nFeatures} hash features for {column}')\n`;
            
            return code;
        }
    },

    countEncoding: {
        type: 'fe_count_encoding',
        name: 'Count Encoding',
        category: 'feature-engineering',
        icon: 'bi-123',
        color: '#0277bd',
        description: 'Encode categoricals by their count/occurrence frequency',
        defaults: {
            column: ''
        },
        properties: [
            BaseNode.createProperty('column', 'Categorical Column', 'text', {
                required: true,
                placeholder: 'category'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const column = data.column || '';
            
            if (!column) return `# Count Encoding: Column required`;
            
            let code = `# Count encoding for ${column}\n`;
            code += `count_map = ${inputVar}['${column}'].value_counts().to_dict()\n`;
            code += `${inputVar}['${column}_count'] = ${inputVar}['${column}'].map(count_map)\n`;
            code += `print(f'Created count encoding for {column}')\n`;
            
            return code;
        }
    },

    meanEncoding: {
        type: 'fe_mean_encoding',
        name: 'Mean Encoding',
        category: 'feature-engineering',
        icon: 'bi-calculator',
        color: '#e65100',
        description: 'Encode categoricals using target mean (similar to target encoding)',
        defaults: {
            column: '',
            target_column: 'target',
            smoothing: 1.0
        },
        properties: [
            BaseNode.createProperty('column', 'Categorical Column', 'text', {
                required: true,
                placeholder: 'category'
            }),
            BaseNode.createProperty('target_column', 'Target Column', 'text', {
                required: true,
                default: 'target',
                placeholder: 'target'
            }),
            BaseNode.createProperty('smoothing', 'Smoothing Factor', 'number', {
                default: 1.0,
                min: 0,
                max: 10,
                step: 0.1
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const column = data.column || '';
            const targetCol = data.target_column || 'target';
            const smoothing = data.smoothing !== undefined ? data.smoothing : 1.0;
            
            if (!column || !targetCol) return `# Mean Encoding: Column and target required`;
            
            let code = `# Mean encoding for ${column}\n`;
            code += `global_mean = ${inputVar}['${targetCol}'].mean()\n`;
            code += `category_means = ${inputVar}.groupby('${column}')['${targetCol}'].mean()\n`;
            code += `category_counts = ${inputVar}.groupby('${column}')['${targetCol}'].count()\n`;
            code += `${inputVar}['${column}_mean_encoded'] = ${inputVar}['${column}'].map(\n`;
            code += `    lambda x: (category_means[x] * category_counts[x] + global_mean * ${smoothing}) / (category_counts[x] + ${smoothing})\n`;
            code += `)\n`;
            code += `print(f'Created mean encoding for {column}')\n`;
            
            return code;
        }
    },

    weightOfEvidence: {
        type: 'fe_weight_of_evidence',
        name: 'Weight of Evidence',
        category: 'feature-engineering',
        icon: 'bi-clipboard-data',
        color: '#2e7d32',
        description: 'Calculate Weight of Evidence for binary classification',
        defaults: {
            column: '',
            target_column: 'target'
        },
        properties: [
            BaseNode.createProperty('column', 'Categorical Column', 'text', {
                required: true,
                placeholder: 'category'
            }),
            BaseNode.createProperty('target_column', 'Target Column', 'text', {
                required: true,
                default: 'target',
                placeholder: 'target'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const column = data.column || '';
            const targetCol = data.target_column || 'target';
            
            if (!column || !targetCol) return `# Weight of Evidence: Column and target required`;
            
            let code = `import numpy as np\n`;
            code += `# Weight of Evidence for ${column}\n`;
            code += `# Calculate WoE = ln((% of non-events) / (% of events))\n`;
            code += `total_events = (${inputVar}['${targetCol}'] == 1).sum()\n`;
            code += `total_non_events = (${inputVar}['${targetCol}'] == 0).sum()\n`;
            code += `woe_dict = {}\n`;
            code += `for category in ${inputVar}['${column}'].unique():\n`;
            code += `    cat_data = ${inputVar}[${inputVar}['${column}'] == category]\n`;
            code += `    events = (cat_data['${targetCol}'] == 1).sum()\n`;
            code += `    non_events = (cat_data['${targetCol}'] == 0).sum()\n`;
            code += `    pct_non_events = non_events / total_non_events if total_non_events > 0 else 0.0001\n`;
            code += `    pct_events = events / total_events if total_events > 0 else 0.0001\n`;
            code += `    woe_dict[category] = np.log(pct_non_events / pct_events)\n`;
            code += `${inputVar}['${column}_woe'] = ${inputVar}['${column}'].map(woe_dict)\n`;
            code += `print(f'Created Weight of Evidence for {column}')\n`;
            
            return code;
        }
    },

    leaveOneOutEncoding: {
        type: 'fe_leave_one_out',
        name: 'Leave-One-Out Encoding',
        category: 'feature-engineering',
        icon: 'bi-arrow-left-right',
        color: '#c2185b',
        description: 'Encode using target mean excluding current row (prevents overfitting)',
        defaults: {
            column: '',
            target_column: 'target'
        },
        properties: [
            BaseNode.createProperty('column', 'Categorical Column', 'text', {
                required: true,
                placeholder: 'category'
            }),
            BaseNode.createProperty('target_column', 'Target Column', 'text', {
                required: true,
                default: 'target',
                placeholder: 'target'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const column = data.column || '';
            const targetCol = data.target_column || 'target';
            
            if (!column || !targetCol) return `# Leave-One-Out: Column and target required`;
            
            let code = `# Leave-One-Out encoding for ${column}\n`;
            code += `global_mean = ${inputVar}['${targetCol}'].mean()\n`;
            code += `${inputVar}['${column}_loo'] = ${inputVar}.apply(\n`;
            code += `    lambda row: (${inputVar}[(${inputVar}['${column}'] == row['${column}']) & (${inputVar}.index != row.name)]['${targetCol}'].mean()\n`;
            code += `                if len(${inputVar}[(${inputVar}['${column}'] == row['${column}']) & (${inputVar}.index != row.name)]) > 0\n`;
            code += `                else global_mean), axis=1\n`;
            code += `)\n`;
            code += `print(f'Created Leave-One-Out encoding for {column}')\n`;
            
            return code;
        }
    },

    catBoostEncoding: {
        type: 'fe_catboost_encoding',
        name: 'CatBoost Encoding',
        category: 'feature-engineering',
        icon: 'bi-box',
        color: '#7b1fa2',
        description: 'Encode using CatBoost-style target encoding',
        defaults: {
            column: '',
            target_column: 'target',
            a: 1.0
        },
        properties: [
            BaseNode.createProperty('column', 'Categorical Column', 'text', {
                required: true,
                placeholder: 'category'
            }),
            BaseNode.createProperty('target_column', 'Target Column', 'text', {
                required: true,
                default: 'target',
                placeholder: 'target'
            }),
            BaseNode.createProperty('a', 'Smoothing Parameter', 'number', {
                default: 1.0,
                min: 0.1,
                max: 10,
                step: 0.1,
                help: 'Smoothing parameter (higher = more smoothing)'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const column = data.column || '';
            const targetCol = data.target_column || 'target';
            const a = data.a !== undefined ? data.a : 1.0;
            
            if (!column || !targetCol) return `# CatBoost Encoding: Column and target required`;
            
            let code = `# CatBoost encoding for ${column}\n`;
            code += `global_mean = ${inputVar}['${targetCol}'].mean()\n`;
            code += `category_stats = ${inputVar}.groupby('${column}').agg({\n`;
            code += `    '${targetCol}': ['sum', 'count']\n`;
            code += `}).reset_index()\n`;
            code += `category_stats.columns = ['${column}', 'sum', 'count']\n`;
            code += `${inputVar} = ${inputVar}.merge(category_stats, on='${column}', how='left')\n`;
            code += `${inputVar}['${column}_catboost'] = (${inputVar}['sum'] + global_mean * ${a}) / (${inputVar}['count'] + ${a})\n`;
            code += `${inputVar} = ${inputVar}.drop(columns=['sum', 'count'])\n`;
            code += `print(f'Created CatBoost encoding for {column}')\n`;
            
            return code;
        }
    }
};

// Register all advanced encoding nodes
Object.values(AdvancedEncodingNodes).forEach(nodeDef => {
    BaseNode.validateDefinition(nodeDef);
    nodeRegistry.register(nodeDef);
});

// Export
if (typeof module !== 'undefined' && module.exports) {
    module.exports = AdvancedEncodingNodes;
}

