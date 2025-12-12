/**
 * Statistical Feature Engineering Nodes
 * Create statistical and aggregative features
 */

const StatisticalFeatureNodes = {
    statisticalAggregates: {
        type: 'fe_statistical_aggregates',
        name: 'Statistical Aggregates',
        category: 'feature-engineering',
        icon: 'bi-calculator',
        color: '#1976d2',
        description: 'Create statistical aggregate features (mean, std, min, max)',
        defaults: {
            columns: '',
            group_by: null
        },
        properties: [
            BaseNode.createProperty('columns', 'Columns', 'text', {
                required: true,
                placeholder: 'col1, col2, col3',
                help: 'Comma-separated columns to aggregate'
            }),
            BaseNode.createProperty('group_by', 'Group By', 'text', {
                placeholder: 'category or leave empty',
                help: 'Optional column to group by'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const columns = data.columns || '';
            const groupBy = data.group_by || null;
            
            if (!columns) return `# Statistical Aggregates: Columns required`;
            
            const colList = columns.split(',').map(c => c.trim());
            let code = `# Statistical aggregates\n`;
            
            if (groupBy) {
                code += `agg_stats = ${inputVar}.groupby('${groupBy}')[${colList.map(c => `'${c}'`).join(', ')}].agg(['mean', 'std', 'min', 'max'])\n`;
                code += `print(f'Created grouped statistical features')\n`;
            } else {
                colList.forEach(col => {
                    code += `${inputVar}['${col}_mean'] = ${inputVar}['${col}'].mean()\n`;
                    code += `${inputVar}['${col}_std'] = ${inputVar}['${col}'].std()\n`;
                    code += `${inputVar}['${col}_min'] = ${inputVar}['${col}'].min()\n`;
                    code += `${inputVar}['${col}_max'] = ${inputVar}['${col}'].max()\n`;
                });
                code += `print(f'Created statistical features for {len(colList)} columns')\n`;
            }
            
            return code;
        }
    },

    percentileFeatures: {
        type: 'fe_percentile_features',
        name: 'Percentile Features',
        category: 'feature-engineering',
        icon: 'bi-percent',
        color: '#0277bd',
        description: 'Create percentile-based features',
        defaults: {
            column: '',
            percentiles: '25,50,75'
        },
        properties: [
            BaseNode.createProperty('column', 'Column', 'text', {
                required: true,
                placeholder: 'score'
            }),
            BaseNode.createProperty('percentiles', 'Percentiles', 'text', {
                default: '25,50,75',
                placeholder: '25,50,75,90',
                help: 'Comma-separated percentiles to calculate'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const column = data.column || '';
            const percentiles = data.percentiles || '25,50,75';
            
            if (!column) return `# Percentile Features: Column required`;
            
            const percList = percentiles.split(',').map(p => p.trim());
            let code = `import numpy as np\n`;
            code += `# Percentile features for ${column}\n`;
            
            percList.forEach(perc => {
                const percVal = parseFloat(perc);
                code += `${inputVar}['${column}_p${perc}'] = np.percentile(${inputVar}['${column}'], ${percVal})\n`;
            });
            
            code += `print(f'Created percentile features for {column}')\n`;
            
            return code;
        }
    },

    zScore: {
        type: 'fe_zscore',
        name: 'Z-Score Normalization',
        category: 'feature-engineering',
        icon: 'bi-arrow-down-up',
        color: '#e65100',
        description: 'Create z-score normalized features',
        defaults: {
            columns: ''
        },
        properties: [
            BaseNode.createProperty('columns', 'Columns', 'text', {
                required: true,
                placeholder: 'col1, col2',
                help: 'Comma-separated columns to normalize'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const columns = data.columns || '';
            
            if (!columns) return `# Z-Score: Columns required`;
            
            const colList = columns.split(',').map(c => c.trim());
            let code = `import numpy as np\n`;
            code += `# Z-score normalization\n`;
            
            colList.forEach(col => {
                code += `mean_${col} = ${inputVar}['${col}'].mean()\n`;
                code += `std_${col} = ${inputVar}['${col}'].std()\n`;
                code += `${inputVar}['${col}_zscore'] = (${inputVar}['${col}'] - mean_${col}) / std_${col}\n`;
            });
            
            code += `print(f'Created z-score features for {len(colList)} columns')\n`;
            
            return code;
        }
    },

    rankFeatures: {
        type: 'fe_rank_features',
        name: 'Rank Features',
        category: 'feature-engineering',
        icon: 'bi-list-ol',
        color: '#2e7d32',
        description: 'Create rank-based features',
        defaults: {
            columns: '',
            method: 'average'
        },
        properties: [
            BaseNode.createProperty('columns', 'Columns', 'text', {
                required: true,
                placeholder: 'col1, col2'
            }),
            BaseNode.createProperty('method', 'Ranking Method', 'select', {
                default: 'average',
                options: ['average', 'min', 'max', 'first', 'dense'],
                help: 'Method for ranking ties'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const columns = data.columns || '';
            const method = data.method || 'average';
            
            if (!columns) return `# Rank Features: Columns required`;
            
            const colList = columns.split(',').map(c => c.trim());
            let code = `# Rank features\n`;
            
            colList.forEach(col => {
                code += `${inputVar}['${col}_rank'] = ${inputVar}['${col}'].rank(method='${method}')\n`;
            });
            
            code += `print(f'Created rank features for {len(colList)} columns')\n`;
            
            return code;
        }
    },

    lagFeatures: {
        type: 'fe_lag_features',
        name: 'Lag Features',
        category: 'feature-engineering',
        icon: 'bi-arrow-left',
        color: '#c2185b',
        description: 'Create lagged features (shift values by periods)',
        defaults: {
            column: '',
            periods: '1,2,3'
        },
        properties: [
            BaseNode.createProperty('column', 'Column', 'text', {
                required: true,
                placeholder: 'value'
            }),
            BaseNode.createProperty('periods', 'Lag Periods', 'text', {
                default: '1,2,3',
                placeholder: '1,2,3,7',
                help: 'Comma-separated periods to lag'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const column = data.column || '';
            const periods = data.periods || '1,2,3';
            
            if (!column) return `# Lag Features: Column required`;
            
            const periodList = periods.split(',').map(p => p.trim());
            let code = `# Lag features for ${column}\n`;
            
            periodList.forEach(period => {
                code += `${inputVar}['${column}_lag${period}'] = ${inputVar}['${column}'].shift(${period})\n`;
            });
            
            code += `print(f'Created lag features for {column}')\n`;
            
            return code;
        }
    },

    differenceFeatures: {
        type: 'fe_difference_features',
        name: 'Difference Features',
        category: 'feature-engineering',
        icon: 'bi-arrow-down-up',
        color: '#7b1fa2',
        description: 'Create difference features (change between periods)',
        defaults: {
            column: '',
            periods: 1
        },
        properties: [
            BaseNode.createProperty('column', 'Column', 'text', {
                required: true,
                placeholder: 'value'
            }),
            BaseNode.createProperty('periods', 'Periods', 'number', {
                default: 1,
                min: 1,
                max: 100,
                help: 'Number of periods to difference'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const column = data.column || '';
            const periods = data.periods || 1;
            
            if (!column) return `# Difference Features: Column required`;
            
            const colName = `${column}_diff${periods}`;
            let code = `${inputVar}['${colName}'] = ${inputVar}['${column}'].diff(periods=${periods})\n`;
            code += `print(f'Created difference feature: {colName}')\n`;
            
            return code;
        }
    },

    pctChange: {
        type: 'fe_pct_change',
        name: 'Percentage Change',
        category: 'feature-engineering',
        icon: 'bi-percent',
        color: '#ff6b6b',
        description: 'Create percentage change features',
        defaults: {
            column: '',
            periods: 1
        },
        properties: [
            BaseNode.createProperty('column', 'Column', 'text', {
                required: true,
                placeholder: 'price'
            }),
            BaseNode.createProperty('periods', 'Periods', 'number', {
                default: 1,
                min: 1,
                max: 100
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const column = data.column || '';
            const periods = data.periods || 1;
            
            if (!column) return `# Percentage Change: Column required`;
            
            const colName = `${column}_pct_change${periods}`;
            let code = `${inputVar}['${colName}'] = ${inputVar}['${column}'].pct_change(periods=${periods})\n`;
            code += `print(f'Created percentage change feature: {colName}')\n`;
            
            return code;
        }
    }
};

// Register all statistical feature nodes
Object.values(StatisticalFeatureNodes).forEach(nodeDef => {
    BaseNode.validateDefinition(nodeDef);
    nodeRegistry.register(nodeDef);
});

// Export
if (typeof module !== 'undefined' && module.exports) {
    module.exports = StatisticalFeatureNodes;
}

