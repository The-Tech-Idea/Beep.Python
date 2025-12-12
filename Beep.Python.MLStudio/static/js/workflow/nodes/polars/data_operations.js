/**
 * Polars Data Operations Nodes
 * Fast DataFrame operations using Polars
 */

const PolarsDataOperationsNodes = {
    readCSV: {
        type: 'polars_read_csv',
        name: 'Read CSV (Polars)',
        category: 'polars-data-operations',
        icon: 'bi-file-earmark-spreadsheet',
        color: '#1976d2',
        description: 'Read CSV file using Polars (faster than pandas)',
        defaults: {
            file_path: 'data/your_dataset.csv',
            variable_name: 'df',
            separator: ',',
            has_header: true
        },
        properties: [
            BaseNode.createProperty('file_path', 'File Path', 'text', {
                required: true,
                placeholder: 'data/dataset.csv'
            }),
            BaseNode.createProperty('variable_name', 'Variable Name', 'text', {
                default: 'df'
            }),
            BaseNode.createProperty('separator', 'Separator', 'text', {
                default: ',',
                help: 'Field delimiter'
            }),
            BaseNode.createProperty('has_header', 'Has Header', 'boolean', {
                default: true
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const varName = data.variable_name || 'df';
            const filePath = data.file_path || 'data/your_dataset.csv';
            const separator = data.separator || ',';
            const hasHeader = data.has_header !== false;
            
            let code = 'import polars as pl\n';
            code += `${varName} = pl.read_csv('${filePath}', separator='${separator}', has_header=${hasHeader})\n`;
            code += `print(f'Loaded dataset: {${varName}.shape}')\n`;
            
            if (context) {
                context.setVariable(node.id, varName);
            }
            
            return code;
        }
    },

    readParquet: {
        type: 'polars_read_parquet',
        name: 'Read Parquet (Polars)',
        category: 'polars-data-operations',
        icon: 'bi-file-earmark-binary',
        color: '#0277bd',
        description: 'Read Parquet file using Polars',
        defaults: {
            file_path: 'data/your_dataset.parquet',
            variable_name: 'df'
        },
        properties: [
            BaseNode.createProperty('file_path', 'File Path', 'text', {
                required: true,
                placeholder: 'data/dataset.parquet'
            }),
            BaseNode.createProperty('variable_name', 'Variable Name', 'text', {
                default: 'df'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const varName = data.variable_name || 'df';
            const filePath = data.file_path || 'data/your_dataset.parquet';
            
            let code = 'import polars as pl\n';
            code += `${varName} = pl.read_parquet('${filePath}')\n`;
            code += `print(f'Loaded dataset: {${varName}.shape}')\n`;
            
            if (context) {
                context.setVariable(node.id, varName);
            }
            
            return code;
        }
    },

    filter: {
        type: 'polars_filter',
        name: 'Filter (Polars)',
        category: 'polars-data-operations',
        icon: 'bi-funnel',
        color: '#e65100',
        description: 'Filter rows using a boolean expression',
        defaults: {
            condition: ''
        },
        properties: [
            BaseNode.createProperty('condition', 'Filter Condition', 'text', {
                required: true,
                placeholder: 'pl.col("age") > 18',
                help: 'Polars expression (e.g., pl.col("age") > 18)'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const outputVar = inputVar + '_filtered';
            const condition = data.condition || '';
            
            if (!condition) return `# Filter: Condition required`;
            
            let code = 'import polars as pl\n';
            code += `${outputVar} = ${inputVar}.filter(${condition})\n`;
            code += `print(f'Filtered shape: {${outputVar}.shape}')\n`;
            
            if (context) {
                context.setVariable(node.id, outputVar);
            }
            
            return code;
        }
    },

    select: {
        type: 'polars_select',
        name: 'Select Columns (Polars)',
        category: 'polars-data-operations',
        icon: 'bi-list-check',
        color: '#2e7d32',
        description: 'Select columns from DataFrame',
        defaults: {
            columns: ''
        },
        properties: [
            BaseNode.createProperty('columns', 'Columns', 'text', {
                required: true,
                placeholder: 'col1, col2 or pl.col("col1"), pl.col("col2")',
                help: 'Comma-separated column names or Polars expressions'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const outputVar = inputVar + '_selected';
            const columns = data.columns || '';
            
            if (!columns) return `# Select: Columns required`;
            
            let code = 'import polars as pl\n';
            // Check if it's already a Polars expression or just column names
            if (columns.includes('pl.col')) {
                code += `${outputVar} = ${inputVar}.select([${columns}])\n`;
            } else {
                const colList = columns.split(',').map(c => c.trim()).map(c => `pl.col("${c}")`).join(', ');
                code += `${outputVar} = ${inputVar}.select([${colList}])\n`;
            }
            code += `print(f'Selected columns. Shape: {${outputVar}.shape}')\n`;
            
            if (context) {
                context.setVariable(node.id, outputVar);
            }
            
            return code;
        }
    },

    groupBy: {
        type: 'polars_groupby',
        name: 'Group By (Polars)',
        category: 'polars-data-operations',
        icon: 'bi-collection',
        color: '#c2185b',
        description: 'Group DataFrame and apply aggregation',
        defaults: {
            by: '',
            agg: 'mean'
        },
        properties: [
            BaseNode.createProperty('by', 'Group By Columns', 'text', {
                required: true,
                placeholder: 'col1, col2',
                help: 'Comma-separated column names'
            }),
            BaseNode.createProperty('agg', 'Aggregation', 'select', {
                default: 'mean',
                options: ['mean', 'sum', 'count', 'min', 'max', 'std', 'median', 'first', 'last'],
                help: 'Aggregation function'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const outputVar = inputVar + '_grouped';
            const by = data.by || '';
            const agg = data.agg || 'mean';
            
            if (!by) return `# Group By: Columns required`;
            
            let code = 'import polars as pl\n';
            const byList = by.split(',').map(c => c.trim()).map(c => `"${c}"`).join(', ');
            code += `${outputVar} = ${inputVar}.group_by([${byList}]).agg(pl.col("*").${agg}())\n`;
            code += `print(f'Grouped by {[${byList}]}')\n`;
            
            if (context) {
                context.setVariable(node.id, outputVar);
            }
            
            return code;
        }
    },

    join: {
        type: 'polars_join',
        name: 'Join (Polars)',
        category: 'polars-data-operations',
        icon: 'bi-arrow-left-right',
        color: '#7b1fa2',
        description: 'Join two DataFrames',
        defaults: {
            how: 'inner',
            on: '',
            left_on: '',
            right_on: ''
        },
        properties: [
            BaseNode.createProperty('how', 'Join Type', 'select', {
                default: 'inner',
                options: ['inner', 'left', 'right', 'outer', 'anti', 'semi']
            }),
            BaseNode.createProperty('on', 'Join Key', 'text', {
                placeholder: 'key_column',
                help: 'Column name if same in both DataFrames'
            }),
            BaseNode.createProperty('left_on', 'Left Key', 'text', {
                placeholder: 'left_column'
            }),
            BaseNode.createProperty('right_on', 'Right Key', 'text', {
                placeholder: 'right_column'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const outputVar = inputVar + '_joined';
            const how = data.how || 'inner';
            const on = data.on || '';
            const leftOn = data.left_on || '';
            const rightOn = data.right_on || '';
            
            let code = 'import polars as pl\n';
            code += `# Assuming df2 is the second DataFrame\n`;
            code += `# df2 = pl.read_csv('path/to/second_file.csv')\n`;
            
            const params = [`how='${how}'`];
            if (on) {
                params.push(`on='${on}'`);
            } else if (leftOn && rightOn) {
                params.push(`left_on='${leftOn}'`, `right_on='${rightOn}'`);
            }
            
            code += `${outputVar} = ${inputVar}.join(df2, ${params.join(', ')})\n`;
            code += `print(f'Joined shape: {${outputVar}.shape}')\n`;
            
            if (context) {
                context.setVariable(node.id, outputVar);
            }
            
            return code;
        }
    }
};

// Register all polars nodes
Object.values(PolarsDataOperationsNodes).forEach(nodeDef => {
    BaseNode.validateDefinition(nodeDef);
    nodeRegistry.register(nodeDef);
});

// Export
if (typeof module !== 'undefined' && module.exports) {
    module.exports = PolarsDataOperationsNodes;
}

