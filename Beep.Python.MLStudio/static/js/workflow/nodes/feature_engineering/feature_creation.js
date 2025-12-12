/**
 * Feature Engineering Nodes
 * Create new features from existing data
 */

const FeatureEngineeringNodes = {
    polynomialFeatures: {
        type: 'sklearn_polynomial_features',
        name: 'Polynomial Features',
        category: 'feature-engineering',
        icon: 'bi-diagram-3',
        color: '#1976d2',
        description: 'Generate polynomial and interaction features',
        defaults: {
            degree: 2,
            interaction_only: false,
            include_bias: true
        },
        properties: [
            BaseNode.createProperty('degree', 'Degree', 'number', {
                default: 2,
                min: 1,
                max: 5,
                help: 'Degree of polynomial features'
            }),
            BaseNode.createProperty('interaction_only', 'Interaction Only', 'boolean', {
                default: false,
                help: 'Only include interaction features'
            }),
            BaseNode.createProperty('include_bias', 'Include Bias', 'boolean', {
                default: true,
                help: 'Include bias (intercept) term'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'X';
            const outputVar = inputVar + '_poly';
            const degree = data.degree || 2;
            const interactionOnly = data.interaction_only || false;
            const includeBias = data.include_bias !== false;
            
            let code = 'from sklearn.preprocessing import PolynomialFeatures\n';
            code += `poly = PolynomialFeatures(degree=${degree}, interaction_only=${interactionOnly}, include_bias=${includeBias})\n`;
            code += `${outputVar} = poly.fit_transform(${inputVar})\n`;
            code += `print(f'Polynomial features: {${inputVar}.shape[1]} -> {${outputVar}.shape[1]}')\n`;
            
            if (context) {
                context.setVariable(node.id, outputVar);
            }
            
            return code;
        }
    },

    createInteraction: {
        type: 'pandas_create_interaction',
        name: 'Create Interaction Features',
        category: 'feature-engineering',
        icon: 'bi-arrow-left-right',
        color: '#0277bd',
        description: 'Create interaction features (product of columns)',
        defaults: {
            columns: '',
            operation: 'multiply'
        },
        properties: [
            BaseNode.createProperty('columns', 'Columns', 'text', {
                required: true,
                placeholder: 'col1, col2',
                help: 'Comma-separated columns to interact'
            }),
            BaseNode.createProperty('operation', 'Operation', 'select', {
                default: 'multiply',
                options: ['multiply', 'add', 'subtract', 'divide'],
                help: 'Mathematical operation'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const columns = data.columns || '';
            const operation = data.operation || 'multiply';
            
            if (!columns) return `# Interaction: Columns required`;
            
            const colList = columns.split(',').map(c => c.trim());
            if (colList.length < 2) return `# Interaction: At least 2 columns required`;
            
            let code = '';
            const ops = {
                'multiply': '*',
                'add': '+',
                'subtract': '-',
                'divide': '/'
            };
            const op = ops[operation] || '*';
            
            const newColName = `interaction_${colList.join('_')}`;
            code += `${inputVar}['${newColName}'] = ${inputVar}['${colList[0]}'] ${op} ${inputVar}['${colList[1]}']\n`;
            code += `print(f'Created interaction feature: {newColName}')\n`;
            
            return code;
        }
    },

    createBinned: {
        type: 'pandas_create_binned',
        name: 'Create Binned Feature',
        category: 'feature-engineering',
        icon: 'bi-scissors',
        color: '#e65100',
        description: 'Create binned/categorical feature from continuous variable',
        defaults: {
            column: '',
            bins: 5,
            labels: null
        },
        properties: [
            BaseNode.createProperty('column', 'Column', 'text', {
                required: true,
                placeholder: 'age'
            }),
            BaseNode.createProperty('bins', 'Number of Bins', 'number', {
                default: 5,
                min: 2,
                max: 20
            }),
            BaseNode.createProperty('labels', 'Labels', 'text', {
                placeholder: 'Low,Medium,High',
                help: 'Comma-separated labels'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const column = data.column || '';
            const bins = data.bins || 5;
            const labels = data.labels || null;
            
            if (!column) return `# Binned Feature: Column required`;
            
            const params = [`bins=${bins}`];
            if (labels) {
                const labelList = labels.split(',').map(l => l.trim()).map(l => `'${l}'`).join(', ');
                params.push(`labels=[${labelList}]`);
            }
            
            let code = `import pandas as pd\n`;
            code += `${inputVar}['${column}_binned'] = pd.cut(${inputVar}['${column}'], ${params.join(', ')})\n`;
            code += `print(f'Created binned feature: {column}_binned')\n`;
            
            return code;
        }
    },

    extractDateTime: {
        type: 'pandas_extract_datetime',
        name: 'Extract DateTime Features',
        category: 'feature-engineering',
        icon: 'bi-calendar',
        color: '#2e7d32',
        description: 'Extract features from datetime column',
        defaults: {
            column: '',
            features: 'year,month,day'
        },
        properties: [
            BaseNode.createProperty('column', 'DateTime Column', 'text', {
                required: true,
                placeholder: 'date_column'
            }),
            BaseNode.createProperty('features', 'Features to Extract', 'text', {
                default: 'year,month,day',
                placeholder: 'year,month,day,dayofweek,hour',
                help: 'Comma-separated: year, month, day, dayofweek, hour, etc.'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const column = data.column || '';
            const features = data.features || 'year,month,day';
            
            if (!column) return `# DateTime Extract: Column required`;
            
            let code = `import pandas as pd\n`;
            code += `${inputVar}['${column}'] = pd.to_datetime(${inputVar}['${column}'])\n`;
            
            const featureList = features.split(',').map(f => f.trim());
            featureList.forEach(feature => {
                code += `${inputVar}['${column}_${feature}'] = ${inputVar}['${column}'].dt.${feature}\n`;
            });
            
            code += `print(f'Extracted {len(featureList)} datetime features from {column}')\n`;
            
            return code;
        }
    }
};

// Register all feature engineering nodes
Object.values(FeatureEngineeringNodes).forEach(nodeDef => {
    BaseNode.validateDefinition(nodeDef);
    nodeRegistry.register(nodeDef);
});

// Export
if (typeof module !== 'undefined' && module.exports) {
    module.exports = FeatureEngineeringNodes;
}

