/**
 * Data Validation Nodes
 * Great Expectations and schema validation
 */

const DataValidationNodes = {
    validateSchema: {
        type: 'validation_schema',
        name: 'Validate Schema',
        category: 'data-validation',
        icon: 'bi-shield-check',
        color: '#28a745',
        description: 'Validate DataFrame schema using pandas',
        defaults: {
            expected_columns: '',
            expected_dtypes: ''
        },
        properties: [
            BaseNode.createProperty('expected_columns', 'Expected Columns', 'text', {
                required: true,
                placeholder: 'col1, col2, col3',
                help: 'Comma-separated list of required columns'
            }),
            BaseNode.createProperty('expected_dtypes', 'Expected Data Types', 'text', {
                placeholder: 'col1:int64, col2:float64',
                help: 'Comma-separated column:type pairs'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const expectedCols = data.expected_columns || '';
            const expectedDtypes = data.expected_dtypes || '';
            
            if (!expectedCols) return `# Schema Validation: Expected columns required`;
            
            let code = `# Schema validation\n`;
            const colList = expectedCols.split(',').map(c => c.trim());
            code += `expected_columns = [${colList.map(c => `'${c}'`).join(', ')}]\n`;
            code += `missing_cols = set(expected_columns) - set(${inputVar}.columns)\n`;
            code += `if missing_cols:\n`;
            code += `    raise ValueError(f'Missing columns: {missing_cols}')\n`;
            code += `print('✓ All required columns present')\n`;
            
            if (expectedDtypes) {
                code += `\n# Data type validation\n`;
                const dtypePairs = expectedDtypes.split(',').map(p => p.trim());
                dtypePairs.forEach(pair => {
                    const [col, dtype] = pair.split(':').map(s => s.trim());
                    code += `if ${inputVar}['${col}'].dtype != '${dtype}':\n`;
                    code += `    raise TypeError(f'Column {col} expected {dtype}, got {${inputVar}["${col}"].dtype}')\n`;
                });
                code += `print('✓ All data types correct')\n`;
            }
            
            return code;
        }
    },

    validateRange: {
        type: 'validation_range',
        name: 'Validate Value Range',
        category: 'data-validation',
        icon: 'bi-sliders',
        color: '#17a2b8',
        description: 'Validate that values are within expected range',
        defaults: {
            column: '',
            min_value: null,
            max_value: null
        },
        properties: [
            BaseNode.createProperty('column', 'Column', 'text', {
                required: true,
                placeholder: 'age'
            }),
            BaseNode.createProperty('min_value', 'Min Value', 'number', {
                placeholder: '0',
                help: 'Minimum allowed value'
            }),
            BaseNode.createProperty('max_value', 'Max Value', 'number', {
                placeholder: '100',
                help: 'Maximum allowed value'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const column = data.column || '';
            const minVal = data.min_value !== null && data.min_value !== undefined ? data.min_value : null;
            const maxVal = data.max_value !== null && data.max_value !== undefined ? data.max_value : null;
            
            if (!column) return `# Range Validation: Column required`;
            
            let code = `# Range validation for ${column}\n`;
            if (minVal !== null) {
                code += `out_of_range_min = ${inputVar}[${inputVar}['${column}'] < ${minVal}]\n`;
                code += `if len(out_of_range_min) > 0:\n`;
                code += `    print(f'Warning: {len(out_of_range_min)} values below minimum {minVal}')\n`;
            }
            if (maxVal !== null) {
                code += `out_of_range_max = ${inputVar}[${inputVar}['${column}'] > ${maxVal}]\n`;
                code += `if len(out_of_range_max) > 0:\n`;
                code += `    print(f'Warning: {len(out_of_range_max)} values above maximum {maxVal}')\n`;
            }
            code += `print('✓ Range validation complete')\n`;
            
            return code;
        }
    },

    validateNotNull: {
        type: 'validation_not_null',
        name: 'Validate Not Null',
        category: 'data-validation',
        icon: 'bi-x-circle',
        color: '#dc3545',
        description: 'Validate that required columns have no null values',
        defaults: {
            columns: ''
        },
        properties: [
            BaseNode.createProperty('columns', 'Columns', 'text', {
                required: true,
                placeholder: 'id, name, email',
                help: 'Comma-separated columns that must not be null'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const columns = data.columns || '';
            
            if (!columns) return `# Not Null Validation: Columns required`;
            
            let code = `# Not null validation\n`;
            const colList = columns.split(',').map(c => c.trim());
            colList.forEach(col => {
                code += `null_count = ${inputVar}['${col}'].isnull().sum()\n`;
                code += `if null_count > 0:\n`;
                code += `    raise ValueError(f'Column {col} has {null_count} null values')\n`;
            });
            code += `print('✓ All required columns are not null')\n`;
            
            return code;
        }
    }
};

// Register all validation nodes
Object.values(DataValidationNodes).forEach(nodeDef => {
    BaseNode.validateDefinition(nodeDef);
    nodeRegistry.register(nodeDef);
});

// Export
if (typeof module !== 'undefined' && module.exports) {
    module.exports = DataValidationNodes;
}

