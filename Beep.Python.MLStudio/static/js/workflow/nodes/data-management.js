/**
 * Data Management Nodes
 * Nodes for data cleaning and manipulation
 */

const DataManagementNodes = {
    selectColumns: {
        type: 'data_select_columns',
        name: 'Select Columns',
        category: 'data-management',
        icon: 'bi-list-check',
        color: '#e65100',
        description: 'Select specific columns/features',
        defaults: {
            columns: '',
            keep: true
        },
        properties: [
            BaseNode.createProperty('columns', 'Columns', 'text', {
                required: true,
                placeholder: 'col1, col2, col3',
                help: 'Comma-separated list of column names'
            }),
            BaseNode.createProperty('keep', 'Keep Selected', 'boolean', {
                default: true,
                help: 'If true, keep selected columns; if false, drop them'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const columns = data.columns || '';
            const keep = data.keep !== false;
            const inputVar = context.getInputVariable(node) || 'df';
            const outputVar = inputVar + '_selected';
            
            if (!columns) return `# Select Columns: No columns specified`;
            
            const colList = columns.split(',').map(c => c.trim()).map(c => `'${c}'`).join(', ');
            
            let code = `# Select Columns Node (${node.id})\n`;
            code += `# Input: ${inputVar}\n`;
            code += `# Output: ${outputVar}\n\n`;
            code += keep 
                ? `${outputVar} = ${inputVar}[[${colList}]].copy()\n`
                : `${outputVar} = ${inputVar}.drop(columns=[${colList}]).copy()\n`;
            code += `print(f'Selected columns: {${outputVar}.shape}')\n`;
            
            // Register output in context
            context.setVariable(node.id, outputVar);
            
            return code;
        }
    },

    dropNA: {
        type: 'data_drop_na',
        name: 'Drop Missing Values',
        category: 'data-management',
        icon: 'bi-x-circle',
        color: '#c62828',
        description: 'Remove rows with NaN/null values',
        defaults: {
            axis: 0,
            how: 'any',
            subset: null
        },
        properties: [
            BaseNode.createProperty('axis', 'Axis', 'select', {
                default: 0,
                options: [
                    { value: 0, label: 'Rows (0)' },
                    { value: 1, label: 'Columns (1)' }
                ]
            }),
            BaseNode.createProperty('how', 'How', 'select', {
                default: 'any',
                options: ['any', 'all'],
                help: 'Drop if any/all values are NA'
            }),
            BaseNode.createProperty('subset', 'Subset Columns', 'text', {
                placeholder: 'col1, col2',
                help: 'Optional: specific columns to check (comma-separated)'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context.getInputVariable(node) || 'df';
            const outputVar = inputVar + '_clean';
            const axis = data.axis || 0;
            const how = data.how || 'any';
            const subset = data.subset ? `subset=[${data.subset.split(',').map(c => `'${c.trim()}'`).join(', ')}]` : '';
            
            let code = `# Drop NA Node (${node.id})\n`;
            code += `# Input: ${inputVar}\n`;
            code += `# Output: ${outputVar}\n\n`;
            code += `${outputVar} = ${inputVar}.dropna(axis=${axis}, how='${how}'${subset ? ', ' + subset : ''}).copy()\n`;
            code += `print(f'Dropped NA: {${inputVar}.shape} -> {${outputVar}.shape}')\n`;
            
            // Register output in context
            context.setVariable(node.id, outputVar);
            
            return code;
        }
    },

    fillNA: {
        type: 'data_fill_na',
        name: 'Fill Missing Values',
        category: 'data-management',
        icon: 'bi-arrow-down-up',
        color: '#2e7d32',
        description: 'Impute missing values',
        defaults: {
            method: 'mean',
            value: null,
            axis: 0
        },
        properties: [
            BaseNode.createProperty('method', 'Method', 'select', {
                default: 'mean',
                options: ['mean', 'median', 'mode', 'forward', 'backward', 'value'],
                help: 'Method to fill missing values'
            }),
            BaseNode.createProperty('value', 'Fill Value', 'text', {
                placeholder: '0 or leave empty',
                help: 'Value to use when method is "value"'
            }),
            BaseNode.createProperty('axis', 'Axis', 'select', {
                default: 0,
                options: [
                    { value: 0, label: 'Rows (0)' },
                    { value: 1, label: 'Columns (1)' }
                ]
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context.getInputVariable(node) || 'df';
            const outputVar = inputVar + '_filled';
            const method = data.method || 'mean';
            const value = data.value;
            const axis = data.axis || 0;
            
            let code = `# Fill NA Node (${node.id})\n`;
            code += `# Input: ${inputVar}\n`;
            code += `# Output: ${outputVar}\n\n`;
            
            if (method === 'value' && value) {
                code += `${outputVar} = ${inputVar}.fillna(${value}, axis=${axis}).copy()\n`;
            } else if (['mean', 'median', 'mode'].includes(method)) {
                code += `${outputVar} = ${inputVar}.fillna(${inputVar}.${method}(), axis=${axis}).copy()\n`;
            } else {
                code += `${outputVar} = ${inputVar}.fillna(method='${method}', axis=${axis}).copy()\n`;
            }
            code += `print(f'Filled NA values: {${outputVar}.shape}')\n`;
            
            // Register output in context
            context.setVariable(node.id, outputVar);
            
            return code;
        }
    },

    removeDuplicates: {
        type: 'data_remove_duplicates',
        name: 'Remove Duplicates',
        category: 'data-management',
        icon: 'bi-backspace-reverse',
        color: '#c2185b',
        description: 'Remove duplicate rows',
        defaults: {
            subset: null,
            keep: 'first'
        },
        properties: [
            BaseNode.createProperty('subset', 'Subset Columns', 'text', {
                placeholder: 'col1, col2',
                help: 'Optional: specific columns to check (comma-separated)'
            }),
            BaseNode.createProperty('keep', 'Keep', 'select', {
                default: 'first',
                options: ['first', 'last', false],
                help: 'Which duplicate to keep'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context.getInputVariable(node) || 'df';
            const outputVar = inputVar + '_dedup';
            const subset = data.subset ? `subset=[${data.subset.split(',').map(c => `'${c.trim()}'`).join(', ')}]` : '';
            const keep = data.keep === false ? 'False' : `'${data.keep || 'first'}'`;
            
            let code = `# Remove Duplicates Node (${node.id})\n`;
            code += `# Input: ${inputVar}\n`;
            code += `# Output: ${outputVar}\n\n`;
            code += `${outputVar} = ${inputVar}.drop_duplicates(${subset ? subset + ', ' : ''}keep=${keep}).copy()\n`;
            code += `print(f'Removed duplicates: {${inputVar}.shape} -> {${outputVar}.shape}')\n`;
            
            // Register output in context
            context.setVariable(node.id, outputVar);
            
            return code;
        }
    },

    filter: {
        type: 'data_filter',
        name: 'Filter Rows',
        category: 'data-management',
        icon: 'bi-funnel',
        color: '#0277bd',
        description: 'Filter data by condition',
        defaults: {
            condition: '',
            column: ''
        },
        properties: [
            BaseNode.createProperty('column', 'Column Name', 'text', {
                required: true,
                placeholder: 'age',
                help: 'Column to filter on'
            }),
            BaseNode.createProperty('condition', 'Condition', 'text', {
                required: true,
                placeholder: '> 18',
                help: 'Filter condition (e.g., "> 18", "== \'yes\'")'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context.getInputVariable(node) || 'df';
            const outputVar = inputVar + '_filtered';
            const column = data.column || '';
            const condition = data.condition || '';
            
            if (!column || !condition) return `# Filter: Missing column or condition`;
            
            let code = `# Filter Rows Node (${node.id})\n`;
            code += `# Input: ${inputVar}\n`;
            code += `# Output: ${outputVar}\n\n`;
            code += `${outputVar} = ${inputVar}[${inputVar}['${column}'] ${condition}].copy()\n`;
            code += `print(f'Filtered: {${inputVar}.shape} -> {${outputVar}.shape}')\n`;
            
            // Register output in context
            context.setVariable(node.id, outputVar);
            
            return code;
        }
    }
};

// Register all data management nodes (safe registration)
if (typeof registerNodesSafely !== 'undefined') {
    registerNodesSafely(DataManagementNodes, 'data management');
} else if (typeof BaseNode !== 'undefined' && typeof nodeRegistry !== 'undefined') {
    Object.values(DataManagementNodes).forEach(nodeDef => {
        try {
            BaseNode.validateDefinition(nodeDef);
            nodeRegistry.register(nodeDef);
        } catch (error) {
            console.error(`Failed to register node ${nodeDef.type}:`, error);
        }
    });
    console.log(`Registered ${Object.keys(DataManagementNodes).length} data management nodes`);
} else {
    console.warn('Dependencies not ready for data-management.js, will retry');
    setTimeout(() => {
        if (typeof BaseNode !== 'undefined' && typeof nodeRegistry !== 'undefined') {
            Object.values(DataManagementNodes).forEach(nodeDef => {
                try {
                    BaseNode.validateDefinition(nodeDef);
                    nodeRegistry.register(nodeDef);
                } catch (error) {
                    console.error(`Failed to register node ${nodeDef.type}:`, error);
                }
            });
        }
    }, 500);
}

// Export
if (typeof module !== 'undefined' && module.exports) {
    module.exports = DataManagementNodes;
}

