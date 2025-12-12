/**
 * Pandas Data Operations Nodes
 * Core pandas data manipulation operations
 */

const PandasDataOperationsNodes = {
    groupBy: {
        type: 'pandas_groupby',
        name: 'Group By',
        category: 'pandas-data-operations',
        icon: 'bi-collection',
        color: '#1976d2',
        description: 'Group DataFrame using a mapper or by a Series of columns',
        defaults: {
            by: '',
            agg_function: 'mean'
        },
        properties: [
            BaseNode.createProperty('by', 'Group By Columns', 'text', {
                required: true,
                placeholder: 'col1, col2',
                help: 'Comma-separated list of column names to group by'
            }),
            BaseNode.createProperty('agg_function', 'Aggregation Function', 'select', {
                default: 'mean',
                options: ['mean', 'sum', 'count', 'min', 'max', 'std', 'median', 'first', 'last'],
                help: 'Aggregation function to apply'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const outputVar = inputVar + '_grouped';
            const by = data.by || '';
            const aggFunc = data.agg_function || 'mean';
            
            if (!by) return `# Group By: No columns specified`;
            
            const byList = by.split(',').map(c => c.trim()).map(c => `'${c}'`).join(', ');
            let code = `${outputVar} = ${inputVar}.groupby([${byList}]).${aggFunc}()\n`;
            code += `print(f'Grouped by {[${byList}]} using ${aggFunc}')\n`;
            code += `print(${outputVar}.head())\n`;
            
            if (context) {
                context.setVariable(node.id, outputVar);
            }
            
            return code;
        }
    },

    merge: {
        type: 'pandas_merge',
        name: 'Merge DataFrames',
        category: 'pandas-data-operations',
        icon: 'bi-arrow-left-right',
        color: '#0277bd',
        description: 'Merge DataFrame objects with a database-style join',
        defaults: {
            how: 'inner',
            on: '',
            left_on: '',
            right_on: '',
            suffixes: ['_x', '_y']
        },
        properties: [
            BaseNode.createProperty('how', 'Join Type', 'select', {
                default: 'inner',
                options: ['inner', 'left', 'right', 'outer', 'cross'],
                help: 'Type of merge to perform'
            }),
            BaseNode.createProperty('on', 'Merge Key', 'text', {
                placeholder: 'key_column',
                help: 'Column name(s) to join on (if same in both DataFrames)'
            }),
            BaseNode.createProperty('left_on', 'Left Key', 'text', {
                placeholder: 'left_column',
                help: 'Column name(s) in left DataFrame'
            }),
            BaseNode.createProperty('right_on', 'Right Key', 'text', {
                placeholder: 'right_column',
                help: 'Column name(s) in right DataFrame'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const outputVar = inputVar + '_merged';
            const how = data.how || 'inner';
            const on = data.on || '';
            const leftOn = data.left_on || '';
            const rightOn = data.right_on || '';
            
            let code = `# Assuming df2 is the second DataFrame\n`;
            code += `# df2 = pd.read_csv('path/to/second_file.csv')\n`;
            
            const params = [`how='${how}'`];
            if (on) {
                params.push(`on='${on}'`);
            } else if (leftOn && rightOn) {
                params.push(`left_on='${leftOn}'`, `right_on='${rightOn}'`);
            }
            
            code += `${outputVar} = ${inputVar}.merge(df2, ${params.join(', ')})\n`;
            code += `print(f'Merged shape: {${outputVar}.shape}')\n`;
            
            if (context) {
                context.setVariable(node.id, outputVar);
            }
            
            return code;
        }
    },

    pivot: {
        type: 'pandas_pivot',
        name: 'Pivot Table',
        category: 'pandas-data-operations',
        icon: 'bi-table',
        color: '#e65100',
        description: 'Create a spreadsheet-style pivot table as a DataFrame',
        defaults: {
            index: '',
            columns: '',
            values: '',
            aggfunc: 'mean'
        },
        properties: [
            BaseNode.createProperty('index', 'Index Column', 'text', {
                required: true,
                placeholder: 'date',
                help: 'Column to use as index'
            }),
            BaseNode.createProperty('columns', 'Columns', 'text', {
                placeholder: 'category',
                help: 'Column to use as columns'
            }),
            BaseNode.createProperty('values', 'Values', 'text', {
                placeholder: 'value',
                help: 'Column to aggregate'
            }),
            BaseNode.createProperty('aggfunc', 'Aggregation Function', 'select', {
                default: 'mean',
                options: ['mean', 'sum', 'count', 'min', 'max', 'std', 'median']
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const outputVar = inputVar + '_pivot';
            const index = data.index || '';
            const columns = data.columns || '';
            const values = data.values || '';
            const aggfunc = data.aggfunc || 'mean';
            
            if (!index) return `# Pivot: Index column required`;
            
            const params = [`index='${index}'`];
            if (columns) params.push(`columns='${columns}'`);
            if (values) params.push(`values='${values}'`);
            params.push(`aggfunc='${aggfunc}'`);
            
            let code = `${outputVar} = ${inputVar}.pivot_table(${params.join(', ')})\n`;
            code += `print(${outputVar}.head())\n`;
            
            if (context) {
                context.setVariable(node.id, outputVar);
            }
            
            return code;
        }
    },

    concat: {
        type: 'pandas_concat',
        name: 'Concatenate',
        category: 'pandas-data-operations',
        icon: 'bi-stack',
        color: '#2e7d32',
        description: 'Concatenate pandas objects along a particular axis',
        defaults: {
            axis: 0,
            ignore_index: false
        },
        properties: [
            BaseNode.createProperty('axis', 'Axis', 'select', {
                default: 0,
                options: [
                    { value: 0, label: 'Rows (0)' },
                    { value: 1, label: 'Columns (1)' }
                ],
                help: 'Axis to concatenate along'
            }),
            BaseNode.createProperty('ignore_index', 'Ignore Index', 'boolean', {
                default: false,
                help: 'If True, do not use the index values'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const outputVar = inputVar + '_concat';
            const axis = data.axis || 0;
            const ignoreIndex = data.ignore_index || false;
            
            let code = `# Assuming df2, df3 are additional DataFrames\n`;
            code += `# df2 = pd.read_csv('path/to/file2.csv')\n`;
            code += `${outputVar} = pd.concat([${inputVar}, df2], axis=${axis}, ignore_index=${ignoreIndex})\n`;
            code += `print(f'Concatenated shape: {${outputVar}.shape}')\n`;
            
            if (context) {
                context.setVariable(node.id, outputVar);
            }
            
            return code;
        }
    },

    sortValues: {
        type: 'pandas_sort_values',
        name: 'Sort Values',
        category: 'pandas-data-operations',
        icon: 'bi-sort-alpha-down',
        color: '#c2185b',
        description: 'Sort by the values along either axis',
        defaults: {
            by: '',
            ascending: true,
            na_position: 'last'
        },
        properties: [
            BaseNode.createProperty('by', 'Sort By Column(s)', 'text', {
                required: true,
                placeholder: 'col1, col2',
                help: 'Comma-separated list of column names'
            }),
            BaseNode.createProperty('ascending', 'Ascending', 'boolean', {
                default: true,
                help: 'Sort ascending vs. descending'
            }),
            BaseNode.createProperty('na_position', 'NA Position', 'select', {
                default: 'last',
                options: ['first', 'last'],
                help: 'Position of NaN values'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const outputVar = inputVar + '_sorted';
            const by = data.by || '';
            const ascending = data.ascending !== false;
            const naPosition = data.na_position || 'last';
            
            if (!by) return `# Sort: No columns specified`;
            
            const byList = by.split(',').map(c => c.trim()).map(c => `'${c}'`).join(', ');
            let code = `${outputVar} = ${inputVar}.sort_values(by=[${byList}], ascending=${ascending}, na_position='${naPosition}')\n`;
            code += `print(f'Sorted by {[${byList}]}')\n`;
            
            if (context) {
                context.setVariable(node.id, outputVar);
            }
            
            return code;
        }
    },

    rename: {
        type: 'pandas_rename',
        name: 'Rename Columns',
        category: 'pandas-data-operations',
        icon: 'bi-pencil',
        color: '#7b1fa2',
        description: 'Rename columns or index labels',
        defaults: {
            columns: '',
            mapper: ''
        },
        properties: [
            BaseNode.createProperty('columns', 'Column Mappings', 'text', {
                required: true,
                placeholder: 'old1:new1, old2:new2',
                help: 'Comma-separated old:new column mappings'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const outputVar = inputVar + '_renamed';
            const columns = data.columns || '';
            
            if (!columns) return `# Rename: No mappings specified`;
            
            const mappings = {};
            columns.split(',').forEach(mapping => {
                const [old, new_] = mapping.split(':').map(s => s.trim());
                if (old && new_) {
                    mappings[old] = new_;
                }
            });
            
            const mapperStr = JSON.stringify(mappings).replace(/"/g, "'");
            let code = `${outputVar} = ${inputVar}.rename(columns=${mapperStr})\n`;
            code += `print(f'Renamed columns: {list(${outputVar}.columns)}')\n`;
            
            if (context) {
                context.setVariable(node.id, outputVar);
            }
            
            return code;
        }
    },

    apply: {
        type: 'pandas_apply',
        name: 'Apply Function',
        category: 'pandas-data-operations',
        icon: 'bi-gear',
        color: '#ff6b6b',
        description: 'Apply a function along an axis of the DataFrame',
        defaults: {
            axis: 0,
            function: 'lambda x: x'
        },
        properties: [
            BaseNode.createProperty('axis', 'Axis', 'select', {
                default: 0,
                options: [
                    { value: 0, label: 'Rows (0)' },
                    { value: 1, label: 'Columns (1)' }
                ]
            }),
            BaseNode.createProperty('function', 'Function', 'text', {
                required: true,
                default: 'lambda x: x',
                placeholder: 'lambda x: x * 2',
                help: 'Python function or lambda expression'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const outputVar = inputVar + '_applied';
            const axis = data.axis || 0;
            const func = data.function || 'lambda x: x';
            
            let code = `${outputVar} = ${inputVar}.apply(${func}, axis=${axis})\n`;
            code += `print(f'Applied function: {${outputVar}.shape}')\n`;
            
            if (context) {
                context.setVariable(node.id, outputVar);
            }
            
            return code;
        }
    }
};

// Register all pandas data operations nodes
Object.values(PandasDataOperationsNodes).forEach(nodeDef => {
    BaseNode.validateDefinition(nodeDef);
    nodeRegistry.register(nodeDef);
});

// Export
if (typeof module !== 'undefined' && module.exports) {
    module.exports = PandasDataOperationsNodes;
}

