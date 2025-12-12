/**
 * Pandas Data Inspection Nodes
 * Data exploration, statistics, and information
 */

const PandasDataInspectionNodes = {
    describe: {
        type: 'pandas_describe',
        name: 'Describe (Statistics)',
        category: 'pandas-data-inspection',
        icon: 'bi-bar-chart',
        color: '#1976d2',
        description: 'Generate descriptive statistics',
        defaults: {
            include: 'all',
            percentiles: [0.25, 0.5, 0.75]
        },
        properties: [
            BaseNode.createProperty('include', 'Include', 'select', {
                default: 'all',
                options: ['all', 'number', 'object'],
                help: 'Data types to include'
            }),
            BaseNode.createProperty('percentiles', 'Percentiles', 'text', {
                default: '0.25, 0.5, 0.75',
                placeholder: '0.25, 0.5, 0.75',
                help: 'Comma-separated percentiles to include'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const include = data.include || 'all';
            const percentiles = data.percentiles || '0.25, 0.5, 0.75';
            
            const params = [];
            if (include !== 'all') {
                params.push(`include='${include}'`);
            }
            if (percentiles) {
                const percList = percentiles.split(',').map(p => p.trim()).join(', ');
                params.push(`percentiles=[${percList}]`);
            }
            
            let code = `stats = ${inputVar}.describe(${params.length > 0 ? params.join(', ') : ''})\n`;
            code += `print(stats)\n`;
            
            return code;
        }
    },

    info: {
        type: 'pandas_info',
        name: 'DataFrame Info',
        category: 'pandas-data-inspection',
        icon: 'bi-info-circle',
        color: '#0277bd',
        description: 'Print a concise summary of a DataFrame',
        defaults: {
            verbose: null,
            memory_usage: null
        },
        properties: [
            BaseNode.createProperty('verbose', 'Verbose', 'boolean', {
                default: null,
                help: 'Print full summary (None for auto)'
            }),
            BaseNode.createProperty('memory_usage', 'Memory Usage', 'boolean', {
                default: null,
                help: 'Display memory usage (None for auto)'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const verbose = data.verbose !== null && data.verbose !== undefined ? data.verbose : null;
            const memoryUsage = data.memory_usage !== null && data.memory_usage !== undefined ? data.memory_usage : null;
            
            const params = [];
            if (verbose !== null) params.push(`verbose=${verbose}`);
            if (memoryUsage !== null) params.push(`memory_usage=${memoryUsage}`);
            
            let code = `${inputVar}.info(${params.length > 0 ? params.join(', ') : ''})\n`;
            
            return code;
        }
    },

    valueCounts: {
        type: 'pandas_value_counts',
        name: 'Value Counts',
        category: 'pandas-data-inspection',
        icon: 'bi-list-ol',
        color: '#e65100',
        description: 'Return a Series containing counts of unique values',
        defaults: {
            column: '',
            normalize: false,
            ascending: false,
            dropna: true
        },
        properties: [
            BaseNode.createProperty('column', 'Column', 'text', {
                required: true,
                placeholder: 'category',
                help: 'Column to count values'
            }),
            BaseNode.createProperty('normalize', 'Normalize', 'boolean', {
                default: false,
                help: 'Return proportions rather than counts'
            }),
            BaseNode.createProperty('ascending', 'Ascending', 'boolean', {
                default: false,
                help: 'Sort in ascending order'
            }),
            BaseNode.createProperty('dropna', 'Drop NA', 'boolean', {
                default: true,
                help: 'Exclude NA/null values'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const column = data.column || '';
            const normalize = data.normalize || false;
            const ascending = data.ascending || false;
            const dropna = data.dropna !== false;
            
            if (!column) return `# Value Counts: Column required`;
            
            const params = [`normalize=${normalize}`, `ascending=${ascending}`, `dropna=${dropna}`];
            let code = `value_counts = ${inputVar}['${column}'].value_counts(${params.join(', ')})\n`;
            code += `print(value_counts)\n`;
            
            return code;
        }
    },

    unique: {
        type: 'pandas_unique',
        name: 'Unique Values',
        category: 'pandas-data-inspection',
        icon: 'bi-star',
        color: '#2e7d32',
        description: 'Return unique values in a Series',
        defaults: {
            column: ''
        },
        properties: [
            BaseNode.createProperty('column', 'Column', 'text', {
                required: true,
                placeholder: 'category',
                help: 'Column to get unique values from'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const column = data.column || '';
            
            if (!column) return `# Unique: Column required`;
            
            let code = `unique_values = ${inputVar}['${column}'].unique()\n`;
            code += `print(f'Unique values in {column}: {unique_values}')\n`;
            code += `print(f'Count: {len(unique_values)}')\n`;
            
            return code;
        }
    },

    nunique: {
        type: 'pandas_nunique',
        name: 'Number of Unique',
        category: 'pandas-data-inspection',
        icon: 'bi-123',
        color: '#c2185b',
        description: 'Return number of unique elements in the object',
        defaults: {
            column: '',
            dropna: true
        },
        properties: [
            BaseNode.createProperty('column', 'Column', 'text', {
                placeholder: 'category or leave empty for all',
                help: 'Column name (empty for all columns)'
            }),
            BaseNode.createProperty('dropna', 'Drop NA', 'boolean', {
                default: true,
                help: 'Exclude NA/null values'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const column = data.column || '';
            const dropna = data.dropna !== false;
            
            let code = '';
            if (column) {
                code += `n_unique = ${inputVar}['${column}'].nunique(dropna=${dropna})\n`;
                code += `print(f'Number of unique values in {column}: {n_unique}')\n`;
            } else {
                code += `n_unique = ${inputVar}.nunique(dropna=${dropna})\n`;
                code += `print('Number of unique values per column:')\n`;
                code += `print(n_unique)\n`;
            }
            
            return code;
        }
    },

    head: {
        type: 'pandas_head',
        name: 'Head (First N Rows)',
        category: 'pandas-data-inspection',
        icon: 'bi-arrow-up',
        color: '#7b1fa2',
        description: 'Return the first n rows',
        defaults: {
            n: 5
        },
        properties: [
            BaseNode.createProperty('n', 'Number of Rows', 'number', {
                default: 5,
                min: 1,
                max: 100,
                help: 'Number of rows to return'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const n = data.n || 5;
            
            let code = `print('First ${n} rows:')\n`;
            code += `print(${inputVar}.head(${n}))\n`;
            
            return code;
        }
    },

    tail: {
        type: 'pandas_tail',
        name: 'Tail (Last N Rows)',
        category: 'pandas-data-inspection',
        icon: 'bi-arrow-down',
        color: '#ff6b6b',
        description: 'Return the last n rows',
        defaults: {
            n: 5
        },
        properties: [
            BaseNode.createProperty('n', 'Number of Rows', 'number', {
                default: 5,
                min: 1,
                max: 100
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const n = data.n || 5;
            
            let code = `print('Last ${n} rows:')\n`;
            code += `print(${inputVar}.tail(${n}))\n`;
            
            return code;
        }
    },

    sample: {
        type: 'pandas_sample',
        name: 'Sample Rows',
        category: 'pandas-data-inspection',
        icon: 'bi-shuffle',
        color: '#4ecdc4',
        description: 'Return a random sample of items from an axis',
        defaults: {
            n: 10,
            frac: null,
            random_state: null,
            replace: false
        },
        properties: [
            BaseNode.createProperty('n', 'Number of Rows', 'number', {
                default: 10,
                min: 1,
                max: 10000,
                help: 'Number of items to return (use if frac is empty)'
            }),
            BaseNode.createProperty('frac', 'Fraction', 'number', {
                default: null,
                placeholder: '0.1 or leave empty',
                min: 0,
                max: 1,
                step: 0.01,
                help: 'Fraction of items to return (overrides n)'
            }),
            BaseNode.createProperty('random_state', 'Random State', 'number', {
                default: null,
                placeholder: '42 or leave empty',
                help: 'Random seed'
            }),
            BaseNode.createProperty('replace', 'With Replacement', 'boolean', {
                default: false,
                help: 'Sample with replacement'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const outputVar = inputVar + '_sample';
            const n = data.n || 10;
            const frac = data.frac !== null && data.frac !== undefined ? data.frac : null;
            const randomState = data.random_state !== null && data.random_state !== undefined ? data.random_state : null;
            const replace = data.replace || false;
            
            const params = [`replace=${replace}`];
            if (frac !== null) {
                params.push(`frac=${frac}`);
            } else {
                params.push(`n=${n}`);
            }
            if (randomState !== null) {
                params.push(`random_state=${randomState}`);
            }
            
            let code = `${outputVar} = ${inputVar}.sample(${params.join(', ')})\n`;
            code += `print(f'Sampled {${outputVar}.shape[0]} rows')\n`;
            code += `print(${outputVar})\n`;
            
            if (context) {
                context.setVariable(node.id, outputVar);
            }
            
            return code;
        }
    },

    query: {
        type: 'pandas_query',
        name: 'Query (Filter)',
        category: 'pandas-data-inspection',
        icon: 'bi-funnel',
        color: '#0277bd',
        description: 'Query the columns of a DataFrame with a boolean expression',
        defaults: {
            expr: ''
        },
        properties: [
            BaseNode.createProperty('expr', 'Query Expression', 'text', {
                required: true,
                placeholder: 'age > 18 and category == "A"',
                help: 'Boolean expression to filter rows'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const outputVar = inputVar + '_filtered';
            const expr = data.expr || '';
            
            if (!expr) return `# Query: Expression required`;
            
            let code = `${outputVar} = ${inputVar}.query('${expr}')\n`;
            code += `print(f'Filtered shape: {${outputVar}.shape} (from {${inputVar}.shape[0]} rows)')\n`;
            
            if (context) {
                context.setVariable(node.id, outputVar);
            }
            
            return code;
        }
    },

    shape: {
        type: 'pandas_shape',
        name: 'Get Shape',
        category: 'pandas-data-inspection',
        icon: 'bi-aspect-ratio',
        color: '#e65100',
        description: 'Return the shape of the DataFrame',
        defaults: {},
        properties: [],
        generateCode: (node, context) => {
            const inputVar = context ? context.getInputVariable(node) : 'df';
            
            let code = `shape = ${inputVar}.shape\n`;
            code += `print(f'DataFrame shape: {shape[0]} rows × {shape[1]} columns')\n`;
            
            return code;
        }
    }
};

// Register all pandas data inspection nodes
Object.values(PandasDataInspectionNodes).forEach(nodeDef => {
    BaseNode.validateDefinition(nodeDef);
    nodeRegistry.register(nodeDef);
});

// Export
if (typeof module !== 'undefined' && module.exports) {
    module.exports = PandasDataInspectionNodes;
}

