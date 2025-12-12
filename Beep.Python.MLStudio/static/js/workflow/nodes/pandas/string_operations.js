/**
 * Pandas String Operations Nodes
 * String manipulation and text processing
 */

const PandasStringOperationsNodes = {
    strLower: {
        type: 'pandas_str_lower',
        name: 'String Lowercase',
        category: 'pandas-string-operations',
        icon: 'bi-type',
        color: '#1976d2',
        description: 'Convert strings in a Series/Index to lowercase',
        defaults: {
            column: '',
            new_column: null
        },
        properties: [
            BaseNode.createProperty('column', 'Column', 'text', {
                required: true,
                placeholder: 'text_column',
                help: 'Column to convert to lowercase'
            }),
            BaseNode.createProperty('new_column', 'New Column Name', 'text', {
                placeholder: 'text_lower or leave empty',
                help: 'Name for new column (empty to overwrite)'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const column = data.column || '';
            const newColumn = data.new_column || null;
            
            if (!column) return `# String Lower: Column required`;
            
            const targetCol = newColumn || column;
            let code = `${inputVar}['${targetCol}'] = ${inputVar}['${column}'].str.lower()\n`;
            code += `print(f'Converted {column} to lowercase')\n`;
            
            return code;
        }
    },

    strUpper: {
        type: 'pandas_str_upper',
        name: 'String Uppercase',
        category: 'pandas-string-operations',
        icon: 'bi-type-h1',
        color: '#0277bd',
        description: 'Convert strings in a Series/Index to uppercase',
        defaults: {
            column: '',
            new_column: null
        },
        properties: [
            BaseNode.createProperty('column', 'Column', 'text', {
                required: true,
                placeholder: 'text_column'
            }),
            BaseNode.createProperty('new_column', 'New Column Name', 'text', {
                placeholder: 'text_upper or leave empty'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const column = data.column || '';
            const newColumn = data.new_column || null;
            
            if (!column) return `# String Upper: Column required`;
            
            const targetCol = newColumn || column;
            let code = `${inputVar}['${targetCol}'] = ${inputVar}['${column}'].str.upper()\n`;
            code += `print(f'Converted {column} to uppercase')\n`;
            
            return code;
        }
    },

    strStrip: {
        type: 'pandas_str_strip',
        name: 'String Strip',
        category: 'pandas-string-operations',
        icon: 'bi-scissors',
        color: '#e65100',
        description: 'Remove leading and trailing characters',
        defaults: {
            column: '',
            to_strip: null,
            new_column: null
        },
        properties: [
            BaseNode.createProperty('column', 'Column', 'text', {
                required: true,
                placeholder: 'text_column'
            }),
            BaseNode.createProperty('to_strip', 'Characters to Strip', 'text', {
                placeholder: 'Leave empty for whitespace',
                help: 'Characters to remove (default: whitespace)'
            }),
            BaseNode.createProperty('new_column', 'New Column Name', 'text', {
                placeholder: 'text_stripped or leave empty'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const column = data.column || '';
            const toStrip = data.to_strip || null;
            const newColumn = data.new_column || null;
            
            if (!column) return `# String Strip: Column required`;
            
            const targetCol = newColumn || column;
            const stripParam = toStrip ? `to_strip='${toStrip}'` : '';
            let code = `${inputVar}['${targetCol}'] = ${inputVar}['${column}'].str.strip(${stripParam})\n`;
            code += `print(f'Stripped {column}')\n`;
            
            return code;
        }
    },

    strReplace: {
        type: 'pandas_str_replace',
        name: 'String Replace',
        category: 'pandas-string-operations',
        icon: 'bi-arrow-repeat',
        color: '#2e7d32',
        description: 'Replace occurrences of pattern/regex in the Series/Index',
        defaults: {
            column: '',
            pat: '',
            repl: '',
            regex: false,
            new_column: null
        },
        properties: [
            BaseNode.createProperty('column', 'Column', 'text', {
                required: true,
                placeholder: 'text_column'
            }),
            BaseNode.createProperty('pat', 'Pattern', 'text', {
                required: true,
                placeholder: 'old_text or regex',
                help: 'String or regex pattern to replace'
            }),
            BaseNode.createProperty('repl', 'Replacement', 'text', {
                required: true,
                placeholder: 'new_text',
                help: 'Replacement string'
            }),
            BaseNode.createProperty('regex', 'Use Regex', 'boolean', {
                default: false,
                help: 'Interpret pattern as regex'
            }),
            BaseNode.createProperty('new_column', 'New Column Name', 'text', {
                placeholder: 'text_replaced or leave empty'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const column = data.column || '';
            const pat = data.pat || '';
            const repl = data.repl || '';
            const regex = data.regex || false;
            const newColumn = data.new_column || null;
            
            if (!column || !pat || !repl) return `# String Replace: Column, pattern, and replacement required`;
            
            const targetCol = newColumn || column;
            let code = `${inputVar}['${targetCol}'] = ${inputVar}['${column}'].str.replace('${pat}', '${repl}', regex=${regex})\n`;
            code += `print(f'Replaced {pat} with {repl} in {column}')\n`;
            
            return code;
        }
    },

    strExtract: {
        type: 'pandas_str_extract',
        name: 'String Extract',
        category: 'pandas-string-operations',
        icon: 'bi-search',
        color: '#c2185b',
        description: 'Extract capture groups in the regex pat as columns',
        defaults: {
            column: '',
            pat: '',
            expand: true
        },
        properties: [
            BaseNode.createProperty('column', 'Column', 'text', {
                required: true,
                placeholder: 'text_column'
            }),
            BaseNode.createProperty('pat', 'Regex Pattern', 'text', {
                required: true,
                placeholder: '(\\d+)',
                help: 'Regular expression pattern with capture groups'
            }),
            BaseNode.createProperty('expand', 'Expand', 'boolean', {
                default: true,
                help: 'Return DataFrame with one column per capture group'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const outputVar = inputVar + '_extracted';
            const column = data.column || '';
            const pat = data.pat || '';
            const expand = data.expand !== false;
            
            if (!column || !pat) return `# String Extract: Column and pattern required`;
            
            let code = `${outputVar} = ${inputVar}['${column}'].str.extract('${pat}', expand=${expand})\n`;
            code += `print(f'Extracted from {column}:')\n`;
            code += `print(${outputVar}.head())\n`;
            
            if (context) {
                context.setVariable(node.id, outputVar);
            }
            
            return code;
        }
    },

    strSplit: {
        type: 'pandas_str_split',
        name: 'String Split',
        category: 'pandas-string-operations',
        icon: 'bi-scissors',
        color: '#7b1fa2',
        description: 'Split strings around given separator/delimiter',
        defaults: {
            column: '',
            pat: ',',
            n: -1,
            expand: false
        },
        properties: [
            BaseNode.createProperty('column', 'Column', 'text', {
                required: true,
                placeholder: 'text_column'
            }),
            BaseNode.createProperty('pat', 'Separator', 'text', {
                default: ',',
                placeholder: ', or |',
                help: 'String or regex to split on'
            }),
            BaseNode.createProperty('n', 'Max Splits', 'number', {
                default: -1,
                min: -1,
                max: 100,
                help: 'Maximum number of splits (-1 for all)'
            }),
            BaseNode.createProperty('expand', 'Expand to Columns', 'boolean', {
                default: false,
                help: 'Expand split strings into separate columns'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const outputVar = inputVar + '_split';
            const column = data.column || '';
            const pat = data.pat || ',';
            const n = data.n !== undefined ? data.n : -1;
            const expand = data.expand || false;
            
            if (!column) return `# String Split: Column required`;
            
            let code = `${outputVar} = ${inputVar}['${column}'].str.split('${pat}', n=${n}, expand=${expand})\n`;
            if (expand) {
                code += `print(f'Split {column} into {${outputVar}.shape[1]} columns')\n`;
            } else {
                code += `print(f'Split {column} into lists')\n`;
            }
            
            if (context) {
                context.setVariable(node.id, outputVar);
            }
            
            return code;
        }
    },

    strContains: {
        type: 'pandas_str_contains',
        name: 'String Contains',
        category: 'pandas-string-operations',
        icon: 'bi-search',
        color: '#ff6b6b',
        description: 'Test if pattern or regex is contained within a string',
        defaults: {
            column: '',
            pat: '',
            case: true,
            regex: true
        },
        properties: [
            BaseNode.createProperty('column', 'Column', 'text', {
                required: true,
                placeholder: 'text_column'
            }),
            BaseNode.createProperty('pat', 'Pattern', 'text', {
                required: true,
                placeholder: 'search_text',
                help: 'Character sequence or regex pattern'
            }),
            BaseNode.createProperty('case', 'Case Sensitive', 'boolean', {
                default: true,
                help: 'If True, case sensitive'
            }),
            BaseNode.createProperty('regex', 'Use Regex', 'boolean', {
                default: true,
                help: 'Interpret pattern as regex'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const outputVar = inputVar + '_filtered';
            const column = data.column || '';
            const pat = data.pat || '';
            const case_ = data.case !== false;
            const regex = data.regex !== false;
            
            if (!column || !pat) return `# String Contains: Column and pattern required`;
            
            let code = `mask = ${inputVar}['${column}'].str.contains('${pat}', case=${case_}, regex=${regex})\n`;
            code += `${outputVar} = ${inputVar}[mask]\n`;
            code += `print(f'Found {${outputVar}.shape[0]} rows containing {pat}')\n`;
            
            if (context) {
                context.setVariable(node.id, outputVar);
            }
            
            return code;
        }
    },

    strLen: {
        type: 'pandas_str_len',
        name: 'String Length',
        category: 'pandas-string-operations',
        icon: 'bi-rulers',
        color: '#4ecdc4',
        description: 'Compute the length of each string in the Series/Index',
        defaults: {
            column: '',
            new_column: null
        },
        properties: [
            BaseNode.createProperty('column', 'Column', 'text', {
                required: true,
                placeholder: 'text_column'
            }),
            BaseNode.createProperty('new_column', 'New Column Name', 'text', {
                default: null,
                placeholder: 'text_length or leave empty',
                help: 'Name for new column with lengths'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const column = data.column || '';
            const newColumn = data.new_column || `${column}_length`;
            
            if (!column) return `# String Length: Column required`;
            
            let code = `${inputVar}['${newColumn}'] = ${inputVar}['${column}'].str.len()\n`;
            code += `print(f'Added {newColumn} column with string lengths')\n`;
            
            return code;
        }
    }
};

// Register all pandas string operations nodes
Object.values(PandasStringOperationsNodes).forEach(nodeDef => {
    BaseNode.validateDefinition(nodeDef);
    nodeRegistry.register(nodeDef);
});

// Export
if (typeof module !== 'undefined' && module.exports) {
    module.exports = PandasStringOperationsNodes;
}

