/**
 * Feature Interaction Nodes
 * Create interaction features between variables
 */

const FeatureInteractionNodes = {
    multiplyFeatures: {
        type: 'fe_multiply_features',
        name: 'Multiply Features',
        category: 'feature-engineering',
        icon: 'bi-x',
        color: '#1976d2',
        description: 'Create new feature by multiplying two or more features',
        defaults: {
            columns: '',
            new_column: null
        },
        properties: [
            BaseNode.createProperty('columns', 'Columns', 'text', {
                required: true,
                placeholder: 'col1, col2',
                help: 'Comma-separated columns to multiply'
            }),
            BaseNode.createProperty('new_column', 'New Column Name', 'text', {
                placeholder: 'product_feature or leave empty',
                help: 'Name for new feature (auto-generated if empty)'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const columns = data.columns || '';
            const newCol = data.new_column || null;
            
            if (!columns) return `# Multiply Features: Columns required`;
            
            const colList = columns.split(',').map(c => c.trim());
            if (colList.length < 2) return `# Multiply: At least 2 columns required`;
            
            const colName = newCol || `product_${colList.join('_')}`;
            let code = `${inputVar}['${colName}'] = ${colList.map(c => `${inputVar}['${c}']`).join(' * ')}\n`;
            code += `print(f'Created multiplication feature: {colName}')\n`;
            
            return code;
        }
    },

    divideFeatures: {
        type: 'fe_divide_features',
        name: 'Divide Features',
        category: 'feature-engineering',
        icon: 'bi-slash',
        color: '#0277bd',
        description: 'Create new feature by dividing two features',
        defaults: {
            numerator: '',
            denominator: '',
            new_column: null
        },
        properties: [
            BaseNode.createProperty('numerator', 'Numerator Column', 'text', {
                required: true,
                placeholder: 'col1'
            }),
            BaseNode.createProperty('denominator', 'Denominator Column', 'text', {
                required: true,
                placeholder: 'col2'
            }),
            BaseNode.createProperty('new_column', 'New Column Name', 'text', {
                placeholder: 'ratio_feature or leave empty'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const numerator = data.numerator || '';
            const denominator = data.denominator || '';
            const newCol = data.new_column || null;
            
            if (!numerator || !denominator) return `# Divide Features: Both columns required`;
            
            const colName = newCol || `ratio_${numerator}_${denominator}`;
            let code = `${inputVar}['${colName}'] = ${inputVar}['${numerator}'] / ${inputVar}['${denominator}'].replace(0, np.nan)\n`;
            code += `print(f'Created division feature: {colName}')\n`;
            
            return code;
        }
    },

    addFeatures: {
        type: 'fe_add_features',
        name: 'Add Features',
        category: 'feature-engineering',
        icon: 'bi-plus',
        color: '#e65100',
        description: 'Create new feature by adding two or more features',
        defaults: {
            columns: '',
            new_column: null
        },
        properties: [
            BaseNode.createProperty('columns', 'Columns', 'text', {
                required: true,
                placeholder: 'col1, col2, col3'
            }),
            BaseNode.createProperty('new_column', 'New Column Name', 'text', {
                placeholder: 'sum_feature or leave empty'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const columns = data.columns || '';
            const newCol = data.new_column || null;
            
            if (!columns) return `# Add Features: Columns required`;
            
            const colList = columns.split(',').map(c => c.trim());
            const colName = newCol || `sum_${colList.join('_')}`;
            let code = `${inputVar}['${colName}'] = ${colList.map(c => `${inputVar}['${c}']`).join(' + ')}\n`;
            code += `print(f'Created addition feature: {colName}')\n`;
            
            return code;
        }
    },

    subtractFeatures: {
        type: 'fe_subtract_features',
        name: 'Subtract Features',
        category: 'feature-engineering',
        icon: 'bi-dash',
        color: '#2e7d32',
        description: 'Create new feature by subtracting two features',
        defaults: {
            minuend: '',
            subtrahend: '',
            new_column: null
        },
        properties: [
            BaseNode.createProperty('minuend', 'Minuend (First Column)', 'text', {
                required: true,
                placeholder: 'col1'
            }),
            BaseNode.createProperty('subtrahend', 'Subtrahend (Second Column)', 'text', {
                required: true,
                placeholder: 'col2'
            }),
            BaseNode.createProperty('new_column', 'New Column Name', 'text', {
                placeholder: 'diff_feature or leave empty'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const minuend = data.minuend || '';
            const subtrahend = data.subtrahend || '';
            const newCol = data.new_column || null;
            
            if (!minuend || !subtrahend) return `# Subtract Features: Both columns required`;
            
            const colName = newCol || `diff_${minuend}_${subtrahend}`;
            let code = `${inputVar}['${colName}'] = ${inputVar}['${minuend}'] - ${inputVar}['${subtrahend}']\n`;
            code += `print(f'Created subtraction feature: {colName}')\n`;
            
            return code;
        }
    },

    powerFeature: {
        type: 'fe_power_feature',
        name: 'Power Feature',
        category: 'feature-engineering',
        icon: 'bi-arrow-up',
        color: '#c2185b',
        description: 'Create new feature by raising a column to a power',
        defaults: {
            column: '',
            power: 2,
            new_column: null
        },
        properties: [
            BaseNode.createProperty('column', 'Column', 'text', {
                required: true,
                placeholder: 'age'
            }),
            BaseNode.createProperty('power', 'Power', 'number', {
                default: 2,
                min: -10,
                max: 10,
                step: 0.1,
                help: 'Power to raise the column to'
            }),
            BaseNode.createProperty('new_column', 'New Column Name', 'text', {
                placeholder: 'age_squared or leave empty'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const column = data.column || '';
            const power = data.power !== undefined ? data.power : 2;
            const newCol = data.new_column || null;
            
            if (!column) return `# Power Feature: Column required`;
            
            const colName = newCol || `${column}_power${power}`;
            let code = `import numpy as np\n`;
            code += `${inputVar}['${colName}'] = np.power(${inputVar}['${column}'], ${power})\n`;
            code += `print(f'Created power feature: {colName}')\n`;
            
            return code;
        }
    },

    logTransform: {
        type: 'fe_log_transform',
        name: 'Log Transform',
        category: 'feature-engineering',
        icon: 'bi-graph-up',
        color: '#7b1fa2',
        description: 'Apply logarithmic transformation to a feature',
        defaults: {
            column: '',
            log_type: 'natural',
            new_column: null
        },
        properties: [
            BaseNode.createProperty('column', 'Column', 'text', {
                required: true,
                placeholder: 'price'
            }),
            BaseNode.createProperty('log_type', 'Log Type', 'select', {
                default: 'natural',
                options: ['natural', 'log10', 'log2'],
                help: 'Type of logarithm'
            }),
            BaseNode.createProperty('new_column', 'New Column Name', 'text', {
                placeholder: 'log_price or leave empty'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const column = data.column || '';
            const logType = data.log_type || 'natural';
            const newCol = data.new_column || null;
            
            if (!column) return `# Log Transform: Column required`;
            
            const colName = newCol || `log_${column}`;
            let code = `import numpy as np\n`;
            
            if (logType === 'natural') {
                code += `${inputVar}['${colName}'] = np.log(${inputVar}['${column}'] + 1)\n`;
            } else if (logType === 'log10') {
                code += `${inputVar}['${colName}'] = np.log10(${inputVar}['${column}'] + 1)\n`;
            } else {
                code += `${inputVar}['${colName}'] = np.log2(${inputVar}['${column}'] + 1)\n`;
            }
            
            code += `print(f'Created log transform feature: {colName}')\n`;
            
            return code;
        }
    },

    sqrtTransform: {
        type: 'fe_sqrt_transform',
        name: 'Square Root Transform',
        category: 'feature-engineering',
        icon: 'bi-square',
        color: '#ff6b6b',
        description: 'Apply square root transformation to a feature',
        defaults: {
            column: '',
            new_column: null
        },
        properties: [
            BaseNode.createProperty('column', 'Column', 'text', {
                required: true,
                placeholder: 'area'
            }),
            BaseNode.createProperty('new_column', 'New Column Name', 'text', {
                placeholder: 'sqrt_area or leave empty'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const column = data.column || '';
            const newCol = data.new_column || null;
            
            if (!column) return `# Sqrt Transform: Column required`;
            
            const colName = newCol || `sqrt_${column}`;
            let code = `import numpy as np\n`;
            code += `${inputVar}['${colName}'] = np.sqrt(${inputVar}['${column}'])\n`;
            code += `print(f'Created sqrt transform feature: {colName}')\n`;
            
            return code;
        }
    },

    rollingWindow: {
        type: 'fe_rolling_window',
        name: 'Rolling Window Statistics',
        category: 'feature-engineering',
        icon: 'bi-arrow-left-right',
        color: '#4ecdc4',
        description: 'Create rolling window statistics (mean, std, etc.)',
        defaults: {
            column: '',
            window: 5,
            statistic: 'mean'
        },
        properties: [
            BaseNode.createProperty('column', 'Column', 'text', {
                required: true,
                placeholder: 'value'
            }),
            BaseNode.createProperty('window', 'Window Size', 'number', {
                default: 5,
                min: 2,
                max: 100,
                help: 'Size of rolling window'
            }),
            BaseNode.createProperty('statistic', 'Statistic', 'select', {
                default: 'mean',
                options: ['mean', 'std', 'min', 'max', 'median', 'sum'],
                help: 'Statistic to calculate'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const column = data.column || '';
            const window = data.window || 5;
            const statistic = data.statistic || 'mean';
            
            if (!column) return `# Rolling Window: Column required`;
            
            const colName = `${column}_rolling_${statistic}_${window}`;
            let code = `${inputVar}['${colName}'] = ${inputVar}['${column}'].rolling(window=${window}).${statistic}()\n`;
            code += `print(f'Created rolling window feature: {colName}')\n`;
            
            return code;
        }
    }
};

// Register all feature interaction nodes
Object.values(FeatureInteractionNodes).forEach(nodeDef => {
    BaseNode.validateDefinition(nodeDef);
    nodeRegistry.register(nodeDef);
});

// Export
if (typeof module !== 'undefined' && module.exports) {
    module.exports = FeatureInteractionNodes;
}

