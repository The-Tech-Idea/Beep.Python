/**
 * Advanced Aggregation Feature Engineering Nodes
 * Create features through complex aggregations
 */

const AggregationFeatureNodes = {
    groupbyAggregates: {
        type: 'fe_groupby_aggregates',
        name: 'GroupBy Aggregates',
        category: 'feature-engineering',
        icon: 'bi-collection',
        color: '#1976d2',
        description: 'Create aggregated features using groupby operations',
        defaults: {
            group_by: '',
            aggregate_columns: '',
            functions: 'mean,std,min,max'
        },
        properties: [
            BaseNode.createProperty('group_by', 'Group By Column', 'text', {
                required: true,
                placeholder: 'category'
            }),
            BaseNode.createProperty('aggregate_columns', 'Columns to Aggregate', 'text', {
                required: true,
                placeholder: 'col1, col2',
                help: 'Comma-separated columns'
            }),
            BaseNode.createProperty('functions', 'Aggregation Functions', 'text', {
                default: 'mean,std,min,max',
                placeholder: 'mean,std,min,max,median,sum,count',
                help: 'Comma-separated aggregation functions'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const groupBy = data.group_by || '';
            const aggCols = data.aggregate_columns || '';
            const functions = data.functions || 'mean,std,min,max';
            
            if (!groupBy || !aggCols) return `# GroupBy Aggregates: Group by and columns required`;
            
            const colList = aggCols.split(',').map(c => c.trim());
            const funcList = functions.split(',').map(f => f.trim());
            
            let code = `# GroupBy aggregates\n`;
            code += `agg_dict = {}\n`;
            colList.forEach(col => {
                funcList.forEach(func => {
                    code += `agg_dict['${col}'] = ['${func}']\n`;
                });
            });
            code += `grouped = ${inputVar}.groupby('${groupBy}').agg(agg_dict)\n`;
            code += `grouped.columns = ['_'.join(col).strip() for col in grouped.columns.values]\n`;
            code += `${inputVar} = ${inputVar}.merge(grouped, left_on='${groupBy}', right_index=True, suffixes=('', '_grouped'))\n`;
            code += `print(f'Created grouped aggregate features')\n`;
            
            return code;
        }
    },

    cumulativeFeatures: {
        type: 'fe_cumulative_features',
        name: 'Cumulative Features',
        category: 'feature-engineering',
        icon: 'bi-graph-up-arrow',
        color: '#0277bd',
        description: 'Create cumulative sum, mean, max, min features',
        defaults: {
            columns: '',
            functions: 'sum,mean,max,min'
        },
        properties: [
            BaseNode.createProperty('columns', 'Columns', 'text', {
                required: true,
                placeholder: 'col1, col2'
            }),
            BaseNode.createProperty('functions', 'Cumulative Functions', 'text', {
                default: 'sum,mean,max,min',
                placeholder: 'sum,mean,max,min',
                help: 'Comma-separated cumulative functions'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const columns = data.columns || '';
            const functions = data.functions || 'sum,mean,max,min';
            
            if (!columns) return `# Cumulative Features: Columns required`;
            
            const colList = columns.split(',').map(c => c.trim());
            const funcList = functions.split(',').map(f => f.trim());
            
            let code = `# Cumulative features\n`;
            colList.forEach(col => {
                funcList.forEach(func => {
                    code += `${inputVar}['${col}_cum${func}'] = ${inputVar}['${col}'].cum${func}()\n`;
                });
            });
            code += `print(f'Created cumulative features')\n`;
            
            return code;
        }
    },

    expandingWindow: {
        type: 'fe_expanding_window',
        name: 'Expanding Window Statistics',
        category: 'feature-engineering',
        icon: 'bi-arrows-expand',
        color: '#e65100',
        description: 'Create expanding window statistics',
        defaults: {
            column: '',
            statistic: 'mean'
        },
        properties: [
            BaseNode.createProperty('column', 'Column', 'text', {
                required: true,
                placeholder: 'value'
            }),
            BaseNode.createProperty('statistic', 'Statistic', 'select', {
                default: 'mean',
                options: ['mean', 'std', 'min', 'max', 'sum', 'count'],
                help: 'Statistic to calculate'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const column = data.column || '';
            const statistic = data.statistic || 'mean';
            
            if (!column) return `# Expanding Window: Column required`;
            
            const colName = `${column}_expanding_${statistic}`;
            let code = `${inputVar}['${colName}'] = ${inputVar}['${column}'].expanding().${statistic}()\n`;
            code += `print(f'Created expanding window feature: {colName}')\n`;
            
            return code;
        }
    },

    ewmFeatures: {
        type: 'fe_ewm_features',
        name: 'Exponentially Weighted Moving',
        category: 'feature-engineering',
        icon: 'bi-speedometer2',
        color: '#2e7d32',
        description: 'Create exponentially weighted moving average features',
        defaults: {
            column: '',
            span: 5,
            statistic: 'mean'
        },
        properties: [
            BaseNode.createProperty('column', 'Column', 'text', {
                required: true,
                placeholder: 'value'
            }),
            BaseNode.createProperty('span', 'Span', 'number', {
                default: 5,
                min: 2,
                max: 100,
                help: 'Span for exponential weighting'
            }),
            BaseNode.createProperty('statistic', 'Statistic', 'select', {
                default: 'mean',
                options: ['mean', 'std'],
                help: 'Statistic to calculate'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const column = data.column || '';
            const span = data.span || 5;
            const statistic = data.statistic || 'mean';
            
            if (!column) return `# EWM Features: Column required`;
            
            const colName = `${column}_ewm_${statistic}_${span}`;
            let code = `${inputVar}['${colName}'] = ${inputVar}['${column}'].ewm(span=${span}).${statistic}()\n`;
            code += `print(f'Created EWM feature: {colName}')\n`;
            
            return code;
        }
    },

    pivotFeatures: {
        type: 'fe_pivot_features',
        name: 'Pivot Table Features',
        category: 'feature-engineering',
        icon: 'bi-table',
        color: '#c2185b',
        description: 'Create features from pivot table aggregations',
        defaults: {
            index: '',
            columns: '',
            values: '',
            aggfunc: 'mean'
        },
        properties: [
            BaseNode.createProperty('index', 'Index Column', 'text', {
                required: true,
                placeholder: 'id'
            }),
            BaseNode.createProperty('columns', 'Columns Column', 'text', {
                required: true,
                placeholder: 'category'
            }),
            BaseNode.createProperty('values', 'Values Column', 'text', {
                required: true,
                placeholder: 'value'
            }),
            BaseNode.createProperty('aggfunc', 'Aggregation Function', 'select', {
                default: 'mean',
                options: ['mean', 'sum', 'count', 'min', 'max', 'std'],
                help: 'Function to aggregate values'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const index = data.index || '';
            const columns = data.columns || '';
            const values = data.values || '';
            const aggfunc = data.aggfunc || 'mean';
            
            if (!index || !columns || !values) return `# Pivot Features: All parameters required`;
            
            let code = `# Pivot table features\n`;
            code += `pivot = ${inputVar}.pivot_table(index='${index}', columns='${columns}', values='${values}', aggfunc='${aggfunc}')\n`;
            code += `pivot.columns = [f'pivot_{col}_{aggfunc}' for col in pivot.columns]\n`;
            code += `${inputVar} = ${inputVar}.merge(pivot, left_on='${index}', right_index=True)\n`;
            code += `print(f'Created pivot table features')\n`;
            
            return code;
        }
    }
};

// Register all aggregation feature nodes
Object.values(AggregationFeatureNodes).forEach(nodeDef => {
    BaseNode.validateDefinition(nodeDef);
    nodeRegistry.register(nodeDef);
});

// Export
if (typeof module !== 'undefined' && module.exports) {
    module.exports = AggregationFeatureNodes;
}

