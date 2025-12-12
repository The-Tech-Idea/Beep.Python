/**
 * Domain-Specific Feature Engineering Nodes
 * Create domain-specific features
 */

const DomainFeatureNodes = {
    extractDateFeatures: {
        type: 'fe_extract_date_features',
        name: 'Extract Date Features',
        category: 'feature-engineering',
        icon: 'bi-calendar',
        color: '#17a2b8',
        description: 'Extract comprehensive date/time features',
        defaults: {
            column: '',
            features: 'year,month,day,dayofweek,hour,quarter'
        },
        properties: [
            BaseNode.createProperty('column', 'Date Column', 'text', {
                required: true,
                placeholder: 'date_column'
            }),
            BaseNode.createProperty('features', 'Features to Extract', 'text', {
                default: 'year,month,day,dayofweek,hour,quarter',
                placeholder: 'year,month,day,dayofweek,hour,quarter,week',
                help: 'Comma-separated date features'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const column = data.column || '';
            const features = data.features || 'year,month,day,dayofweek,hour,quarter';
            
            if (!column) return `# Extract Date Features: Column required`;
            
            let code = `import pandas as pd\n`;
            code += `${inputVar}['${column}'] = pd.to_datetime(${inputVar}['${column}'])\n`;
            
            const featureList = features.split(',').map(f => f.trim());
            featureList.forEach(feature => {
                if (feature === 'dayofweek') {
                    code += `${inputVar}['${column}_${feature}'] = ${inputVar}['${column}'].dt.dayofweek\n`;
                } else if (feature === 'week') {
                    code += `${inputVar}['${column}_${feature}'] = ${inputVar}['${column}'].dt.isocalendar().week\n`;
                } else {
                    code += `${inputVar}['${column}_${feature}'] = ${inputVar}['${column}'].dt.${feature}\n`;
                }
            });
            
            code += `print(f'Extracted {len(featureList)} date features from {column}')\n`;
            
            return code;
        }
    },

    timeSinceEvent: {
        type: 'fe_time_since_event',
        name: 'Time Since Event',
        category: 'feature-engineering',
        icon: 'bi-clock-history',
        color: '#6f42c1',
        description: 'Calculate time elapsed since a reference date',
        defaults: {
            date_column: '',
            reference_date: 'today',
            unit: 'days'
        },
        properties: [
            BaseNode.createProperty('date_column', 'Date Column', 'text', {
                required: true,
                placeholder: 'event_date'
            }),
            BaseNode.createProperty('reference_date', 'Reference Date', 'text', {
                default: 'today',
                placeholder: 'today or 2024-01-01',
                help: 'Reference date to calculate from'
            }),
            BaseNode.createProperty('unit', 'Time Unit', 'select', {
                default: 'days',
                options: ['days', 'weeks', 'months', 'years'],
                help: 'Unit for time difference'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const dateCol = data.date_column || '';
            const refDate = data.reference_date || 'today';
            const unit = data.unit || 'days';
            
            if (!dateCol) return `# Time Since Event: Date column required`;
            
            let code = `import pandas as pd\n`;
            code += `from datetime import datetime\n`;
            code += `${inputVar}['${dateCol}'] = pd.to_datetime(${inputVar}['${dateCol}'])\n`;
            
            if (refDate === 'today') {
                code += `reference = pd.Timestamp.now()\n`;
            } else {
                code += `reference = pd.to_datetime('${refDate}')\n`;
            }
            
            code += `${inputVar}['time_since_${refDate.replace("-", "_")}'] = (reference - ${inputVar}['${dateCol}']).dt.${unit}\n`;
            code += `print(f'Created time since event feature')\n`;
            
            return code;
        }
    },

    cyclicalEncoding: {
        type: 'fe_cyclical_encoding',
        name: 'Cyclical Encoding',
        category: 'feature-engineering',
        icon: 'bi-arrow-repeat',
        color: '#20c997',
        description: 'Encode cyclical features (hours, days, months) using sin/cos',
        defaults: {
            column: '',
            max_value: null
        },
        properties: [
            BaseNode.createProperty('column', 'Column', 'text', {
                required: true,
                placeholder: 'hour, dayofweek, month'
            }),
            BaseNode.createProperty('max_value', 'Max Value', 'number', {
                default: null,
                placeholder: '24 for hours, 7 for days, 12 for months',
                help: 'Maximum value in the cycle (auto-detect if empty)'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const column = data.column || '';
            const maxVal = data.max_value !== null && data.max_value !== undefined ? data.max_value : null;
            
            if (!column) return `# Cyclical Encoding: Column required`;
            
            let code = `import numpy as np\n`;
            if (maxVal !== null) {
                code += `${inputVar}['${column}_sin'] = np.sin(2 * np.pi * ${inputVar}['${column}'] / ${maxVal})\n`;
                code += `${inputVar}['${column}_cos'] = np.cos(2 * np.pi * ${inputVar}['${column}'] / ${maxVal})\n`;
            } else {
                code += `max_val = ${inputVar}['${column}'].max()\n`;
                code += `${inputVar}['${column}_sin'] = np.sin(2 * np.pi * ${inputVar}['${column}'] / max_val)\n`;
                code += `${inputVar}['${column}_cos'] = np.cos(2 * np.pi * ${inputVar}['${column}'] / max_val)\n`;
            }
            code += `print(f'Created cyclical encoding for {column}')\n`;
            
            return code;
        }
    },

    binning: {
        type: 'fe_binning',
        name: 'Binning/Discretization',
        category: 'feature-engineering',
        icon: 'bi-grid-3x3',
        color: '#fd7e14',
        description: 'Create binned/categorical features from continuous variables',
        defaults: {
            column: '',
            bins: 5,
            strategy: 'uniform'
        },
        properties: [
            BaseNode.createProperty('column', 'Column', 'text', {
                required: true,
                placeholder: 'age'
            }),
            BaseNode.createProperty('bins', 'Number of Bins', 'number', {
                default: 5,
                min: 2,
                max: 50
            }),
            BaseNode.createProperty('strategy', 'Binning Strategy', 'select', {
                default: 'uniform',
                options: ['uniform', 'quantile', 'kmeans'],
                help: 'Strategy for binning'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const column = data.column || '';
            const bins = data.bins || 5;
            const strategy = data.strategy || 'uniform';
            
            if (!column) return `# Binning: Column required`;
            
            let code = '';
            if (strategy === 'uniform') {
                code += `import pandas as pd\n`;
                code += `${inputVar}['${column}_binned'] = pd.cut(${inputVar}['${column}'], bins=${bins})\n`;
            } else if (strategy === 'quantile') {
                code += `import pandas as pd\n`;
                code += `${inputVar}['${column}_binned'] = pd.qcut(${inputVar}['${column}'], q=${bins})\n`;
            } else {
                code += `from sklearn.preprocessing import KBinsDiscretizer\n`;
                code += `discretizer = KBinsDiscretizer(n_bins=${bins}, encode='ordinal', strategy='${strategy}')\n`;
                code += `${inputVar}['${column}_binned'] = discretizer.fit_transform(${inputVar}[['${column}']])\n`;
            }
            
            code += `print(f'Created binned feature: {column}_binned')\n`;
            
            return code;
        }
    },

    targetEncoding: {
        type: 'fe_target_encoding',
        name: 'Target Encoding',
        category: 'feature-engineering',
        icon: 'bi-bullseye',
        color: '#e83e8c',
        description: 'Encode categorical features using target mean',
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
                step: 0.1,
                help: 'Smoothing to prevent overfitting'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const column = data.column || '';
            const targetCol = data.target_column || 'target';
            const smoothing = data.smoothing !== undefined ? data.smoothing : 1.0;
            
            if (!column || !targetCol) return `# Target Encoding: Column and target required`;
            
            let code = `# Target encoding for ${column}\n`;
            code += `global_mean = ${inputVar}['${targetCol}'].mean()\n`;
            code += `category_means = ${inputVar}.groupby('${column}')['${targetCol}'].mean()\n`;
            code += `category_counts = ${inputVar}.groupby('${column}')['${targetCol}'].count()\n`;
            code += `${inputVar}['${column}_target_encoded'] = ${inputVar}['${column}'].map(\n`;
            code += `    lambda x: (category_means[x] * category_counts[x] + global_mean * ${smoothing}) / (category_counts[x] + ${smoothing})\n`;
            code += `)\n`;
            code += `print(f'Created target encoding for {column}')\n`;
            
            return code;
        }
    },

    frequencyEncoding: {
        type: 'fe_frequency_encoding',
        name: 'Frequency Encoding',
        category: 'feature-engineering',
        icon: 'bi-bar-chart',
        color: '#6610f2',
        description: 'Encode categorical features by their frequency',
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
            
            if (!column) return `# Frequency Encoding: Column required`;
            
            let code = `# Frequency encoding for ${column}\n`;
            code += `freq_map = ${inputVar}['${column}'].value_counts().to_dict()\n`;
            code += `${inputVar}['${column}_freq'] = ${inputVar}['${column}'].map(freq_map)\n`;
            code += `print(f'Created frequency encoding for {column}')\n`;
            
            return code;
        }
    }
};

// Register all domain feature nodes
Object.values(DomainFeatureNodes).forEach(nodeDef => {
    BaseNode.validateDefinition(nodeDef);
    nodeRegistry.register(nodeDef);
});

// Export
if (typeof module !== 'undefined' && module.exports) {
    module.exports = DomainFeatureNodes;
}

