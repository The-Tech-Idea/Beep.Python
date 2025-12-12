/**
 * Pandas Data Cleaning Nodes
 * Data cleaning and preprocessing operations
 */

const PandasDataCleaningNodes = {
    dropDuplicates: {
        type: 'pandas_drop_duplicates',
        name: 'Drop Duplicates',
        category: 'pandas-data-cleaning',
        icon: 'bi-backspace-reverse',
        color: '#c62828',
        description: 'Return DataFrame with duplicate rows removed',
        defaults: {
            subset: null,
            keep: 'first',
            inplace: false
        },
        properties: [
            BaseNode.createProperty('subset', 'Subset Columns', 'text', {
                placeholder: 'col1, col2',
                help: 'Comma-separated columns to consider (None for all)'
            }),
            BaseNode.createProperty('keep', 'Keep', 'select', {
                default: 'first',
                options: ['first', 'last', false],
                help: 'Which duplicate to keep'
            }),
            BaseNode.createProperty('inplace', 'Inplace', 'boolean', {
                default: false,
                help: 'Modify DataFrame in place'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const outputVar = data.inplace ? inputVar : inputVar + '_deduped';
            const subset = data.subset || null;
            const keep = data.keep === false ? 'False' : `'${data.keep || 'first'}'`;
            const inplace = data.inplace || false;
            
            const params = [`keep=${keep}`, `inplace=${inplace}`];
            if (subset) {
                const subsetList = subset.split(',').map(c => c.trim()).map(c => `'${c}'`).join(', ');
                params.push(`subset=[${subsetList}]`);
            }
            
            let code = `${outputVar} = ${inputVar}.drop_duplicates(${params.join(', ')})\n`;
            if (!inplace) {
                code += `print(f'Removed duplicates. Shape: {${outputVar}.shape}')\n`;
            }
            
            if (context && !inplace) {
                context.setVariable(node.id, outputVar);
            }
            
            return code;
        }
    },

    dropNA: {
        type: 'pandas_dropna',
        name: 'Drop NA',
        category: 'pandas-data-cleaning',
        icon: 'bi-x-circle',
        color: '#c62828',
        description: 'Remove missing values',
        defaults: {
            axis: 0,
            how: 'any',
            subset: null,
            thresh: null
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
                help: 'Specific columns to check'
            }),
            BaseNode.createProperty('thresh', 'Thresh', 'number', {
                placeholder: 'Leave empty',
                help: 'Minimum number of non-NA values required'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const outputVar = inputVar + '_dropped_na';
            const axis = data.axis || 0;
            const how = data.how || 'any';
            const subset = data.subset || null;
            const thresh = data.thresh !== null && data.thresh !== undefined ? data.thresh : null;
            
            const params = [`axis=${axis}`, `how='${how}'`];
            if (subset) {
                const subsetList = subset.split(',').map(c => c.trim()).map(c => `'${c}'`).join(', ');
                params.push(`subset=[${subsetList}]`);
            }
            if (thresh !== null) {
                params.push(`thresh=${thresh}`);
            }
            
            let code = `${outputVar} = ${inputVar}.dropna(${params.join(', ')})\n`;
            code += `print(f'After dropna: {${outputVar}.shape} (removed {${inputVar}.shape[0] - ${outputVar}.shape[0]} rows)')\n`;
            
            if (context) {
                context.setVariable(node.id, outputVar);
            }
            
            return code;
        }
    },

    fillNA: {
        type: 'pandas_fillna',
        name: 'Fill NA',
        category: 'pandas-data-cleaning',
        icon: 'bi-arrow-down-up',
        color: '#2e7d32',
        description: 'Fill NA/NaN values using the specified method',
        defaults: {
            value: null,
            method: null,
            axis: 0,
            limit: null
        },
        properties: [
            BaseNode.createProperty('method', 'Method', 'select', {
                default: null,
                options: [null, 'ffill', 'bfill', 'pad', 'backfill'],
                help: 'Filling method (None to use value)'
            }),
            BaseNode.createProperty('value', 'Fill Value', 'text', {
                placeholder: '0 or mean',
                help: 'Value to use when method is None'
            }),
            BaseNode.createProperty('axis', 'Axis', 'select', {
                default: 0,
                options: [
                    { value: 0, label: 'Rows (0)' },
                    { value: 1, label: 'Columns (1)' }
                ]
            }),
            BaseNode.createProperty('limit', 'Limit', 'number', {
                placeholder: 'Leave empty',
                help: 'Maximum number of consecutive NaN values to fill'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const outputVar = inputVar + '_filled';
            const method = data.method || null;
            const value = data.value || null;
            const axis = data.axis || 0;
            const limit = data.limit !== null && data.limit !== undefined ? data.limit : null;
            
            const params = [`axis=${axis}`];
            if (method) {
                params.push(`method='${method}'`);
            } else if (value) {
                // Try to parse as number, otherwise use as string
                const numValue = parseFloat(value);
                const fillValue = isNaN(numValue) ? `'${value}'` : value;
                params.push(`value=${fillValue}`);
            }
            if (limit !== null) {
                params.push(`limit=${limit}`);
            }
            
            let code = `${outputVar} = ${inputVar}.fillna(${params.join(', ')})\n`;
            code += `print(f'Filled missing values using ${method || value}')\n`;
            
            if (context) {
                context.setVariable(node.id, outputVar);
            }
            
            return code;
        }
    },

    replace: {
        type: 'pandas_replace',
        name: 'Replace Values',
        category: 'pandas-data-cleaning',
        icon: 'bi-arrow-repeat',
        color: '#e65100',
        description: 'Replace values given in to_replace with value',
        defaults: {
            to_replace: '',
            value: '',
            regex: false
        },
        properties: [
            BaseNode.createProperty('to_replace', 'To Replace', 'text', {
                required: true,
                placeholder: 'old_value or old1,old2',
                help: 'Value(s) to find and replace'
            }),
            BaseNode.createProperty('value', 'Replace With', 'text', {
                required: true,
                placeholder: 'new_value',
                help: 'Value to replace with'
            }),
            BaseNode.createProperty('regex', 'Use Regex', 'boolean', {
                default: false,
                help: 'Interpret to_replace as regex'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const outputVar = inputVar + '_replaced';
            const toReplace = data.to_replace || '';
            const value = data.value || '';
            const regex = data.regex || false;
            
            if (!toReplace || !value) return `# Replace: Missing to_replace or value`;
            
            const params = [`to_replace='${toReplace}'`, `value='${value}'`, `regex=${regex}`];
            let code = `${outputVar} = ${inputVar}.replace(${params.join(', ')})\n`;
            code += `print(f'Replaced {toReplace} with {value}')\n`;
            
            if (context) {
                context.setVariable(node.id, outputVar);
            }
            
            return code;
        }
    },

    clip: {
        type: 'pandas_clip',
        name: 'Clip Values',
        category: 'pandas-data-cleaning',
        icon: 'bi-scissors',
        color: '#0277bd',
        description: 'Trim values at input threshold(s)',
        defaults: {
            lower: null,
            upper: null,
            axis: null
        },
        properties: [
            BaseNode.createProperty('lower', 'Lower Bound', 'number', {
                placeholder: '0',
                help: 'Minimum threshold value'
            }),
            BaseNode.createProperty('upper', 'Upper Bound', 'number', {
                placeholder: '100',
                help: 'Maximum threshold value'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const outputVar = inputVar + '_clipped';
            const lower = data.lower !== null && data.lower !== undefined ? data.lower : null;
            const upper = data.upper !== null && data.upper !== undefined ? data.upper : null;
            
            const params = [];
            if (lower !== null) params.push(`lower=${lower}`);
            if (upper !== null) params.push(`upper=${upper}`);
            
            if (params.length === 0) return `# Clip: Specify at least lower or upper bound`;
            
            let code = `${outputVar} = ${inputVar}.clip(${params.join(', ')})\n`;
            code += `print(f'Clipped values to range [{lower || 'None'}, ${upper || 'None'}]')\n`;
            
            if (context) {
                context.setVariable(node.id, outputVar);
            }
            
            return code;
        }
    }
};

// Register all pandas data cleaning nodes
Object.values(PandasDataCleaningNodes).forEach(nodeDef => {
    BaseNode.validateDefinition(nodeDef);
    nodeRegistry.register(nodeDef);
});

// Export
if (typeof module !== 'undefined' && module.exports) {
    module.exports = PandasDataCleaningNodes;
}

