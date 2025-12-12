/**
 * Pandas Data Transformation Nodes
 * Data reshaping, transformation, and type conversion
 */

const PandasDataTransformationNodes = {
    melt: {
        type: 'pandas_melt',
        name: 'Melt (Unpivot)',
        category: 'pandas-data-transformation',
        icon: 'bi-arrow-down-up',
        color: '#1976d2',
        description: 'Unpivot a DataFrame from wide to long format',
        defaults: {
            id_vars: '',
            value_vars: '',
            var_name: 'variable',
            value_name: 'value'
        },
        properties: [
            BaseNode.createProperty('id_vars', 'ID Variables', 'text', {
                placeholder: 'col1, col2',
                help: 'Columns to use as identifier variables'
            }),
            BaseNode.createProperty('value_vars', 'Value Variables', 'text', {
                placeholder: 'col3, col4',
                help: 'Columns to unpivot (empty for all except id_vars)'
            }),
            BaseNode.createProperty('var_name', 'Variable Name', 'text', {
                default: 'variable',
                help: 'Name for the variable column'
            }),
            BaseNode.createProperty('value_name', 'Value Name', 'text', {
                default: 'value',
                help: 'Name for the value column'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const outputVar = inputVar + '_melted';
            const idVars = data.id_vars || null;
            const valueVars = data.value_vars || null;
            const varName = data.var_name || 'variable';
            const valueName = data.value_name || 'value';
            
            const params = [`var_name='${varName}'`, `value_name='${valueName}'`];
            if (idVars) {
                const idList = idVars.split(',').map(c => c.trim()).map(c => `'${c}'`).join(', ');
                params.push(`id_vars=[${idList}]`);
            }
            if (valueVars) {
                const valList = valueVars.split(',').map(c => c.trim()).map(c => `'${c}'`).join(', ');
                params.push(`value_vars=[${valList}]`);
            }
            
            let code = `${outputVar} = pd.melt(${inputVar}, ${params.join(', ')})\n`;
            code += `print(f'Melted DataFrame shape: {${outputVar}.shape}')\n`;
            
            if (context) {
                context.setVariable(node.id, outputVar);
            }
            
            return code;
        }
    },

    pivotTable: {
        type: 'pandas_pivot_table',
        name: 'Pivot Table',
        category: 'pandas-data-transformation',
        icon: 'bi-table',
        color: '#0277bd',
        description: 'Create a spreadsheet-style pivot table with aggregation',
        defaults: {
            index: '',
            columns: '',
            values: '',
            aggfunc: 'mean',
            fill_value: null,
            margins: false
        },
        properties: [
            BaseNode.createProperty('index', 'Index', 'text', {
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
                help: 'Column(s) to aggregate'
            }),
            BaseNode.createProperty('aggfunc', 'Aggregation Function', 'select', {
                default: 'mean',
                options: ['mean', 'sum', 'count', 'min', 'max', 'std', 'median', 'first', 'last']
            }),
            BaseNode.createProperty('fill_value', 'Fill Value', 'text', {
                placeholder: '0 or leave empty',
                help: 'Value to replace missing values with'
            }),
            BaseNode.createProperty('margins', 'Add Margins', 'boolean', {
                default: false,
                help: 'Add row/column margins (subtotals)'
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
            const fillValue = data.fill_value || null;
            const margins = data.margins || false;
            
            if (!index) return `# Pivot Table: Index required`;
            
            const params = [`index='${index}'`, `aggfunc='${aggfunc}'`, `margins=${margins}`];
            if (columns) params.push(`columns='${columns}'`);
            if (values) params.push(`values='${values}'`);
            if (fillValue !== null) {
                const numVal = parseFloat(fillValue);
                const fillVal = isNaN(numVal) ? `'${fillValue}'` : fillValue;
                params.push(`fill_value=${fillVal}`);
            }
            
            let code = `${outputVar} = pd.pivot_table(${inputVar}, ${params.join(', ')})\n`;
            code += `print(${outputVar}.head())\n`;
            
            if (context) {
                context.setVariable(node.id, outputVar);
            }
            
            return code;
        }
    },

    crosstab: {
        type: 'pandas_crosstab',
        name: 'Cross Tabulation',
        category: 'pandas-data-transformation',
        icon: 'bi-grid-3x3',
        color: '#e65100',
        description: 'Compute a simple cross-tabulation of two (or more) factors',
        defaults: {
            index: '',
            columns: '',
            normalize: false,
            margins: false
        },
        properties: [
            BaseNode.createProperty('index', 'Index Column', 'text', {
                required: true,
                placeholder: 'row_category',
                help: 'Values to group by in rows'
            }),
            BaseNode.createProperty('columns', 'Columns', 'text', {
                required: true,
                placeholder: 'col_category',
                help: 'Values to group by in columns'
            }),
            BaseNode.createProperty('normalize', 'Normalize', 'select', {
                default: false,
                options: [false, 'index', 'columns', 'all'],
                help: 'Normalize by dividing all values by the sum of values'
            }),
            BaseNode.createProperty('margins', 'Add Margins', 'boolean', {
                default: false
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const outputVar = inputVar + '_crosstab';
            const index = data.index || '';
            const columns = data.columns || '';
            const normalize = data.normalize || false;
            const margins = data.margins || false;
            
            if (!index || !columns) return `# Crosstab: Index and columns required`;
            
            const params = [`index=${inputVar}['${index}']`, `columns=${inputVar}['${columns}']`, `margins=${margins}`];
            if (normalize) {
                const normVal = normalize === true ? 'True' : `'${normalize}'`;
                params.push(`normalize=${normVal}`);
            }
            
            let code = `${outputVar} = pd.crosstab(${params.join(', ')})\n`;
            code += `print(${outputVar})\n`;
            
            if (context) {
                context.setVariable(node.id, outputVar);
            }
            
            return code;
        }
    },

    getDummies: {
        type: 'pandas_get_dummies',
        name: 'Get Dummies (One-Hot)',
        category: 'pandas-data-transformation',
        icon: 'bi-grid-3x3',
        color: '#7b1fa2',
        description: 'Convert categorical variable into dummy/indicator variables',
        defaults: {
            columns: '',
            prefix: null,
            drop_first: false
        },
        properties: [
            BaseNode.createProperty('columns', 'Columns', 'text', {
                required: true,
                placeholder: 'col1, col2',
                help: 'Comma-separated column names to encode'
            }),
            BaseNode.createProperty('prefix', 'Prefix', 'text', {
                placeholder: 'category_ or leave empty',
                help: 'String to append to column names'
            }),
            BaseNode.createProperty('drop_first', 'Drop First', 'boolean', {
                default: false,
                help: 'Remove first level to avoid multicollinearity'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const outputVar = inputVar + '_encoded';
            const columns = data.columns || '';
            const prefix = data.prefix || null;
            const dropFirst = data.drop_first || false;
            
            if (!columns) return `# Get Dummies: Columns required`;
            
            const colList = columns.split(',').map(c => c.trim()).map(c => `'${c}'`).join(', ');
            const params = [`columns=[${colList}]`, `drop_first=${dropFirst}`];
            if (prefix) {
                params.push(`prefix='${prefix}'`);
            }
            
            let code = `${outputVar} = pd.get_dummies(${inputVar}, ${params.join(', ')})\n`;
            code += `print(f'One-hot encoded. New shape: {${outputVar}.shape}')\n`;
            
            if (context) {
                context.setVariable(node.id, outputVar);
            }
            
            return code;
        }
    },

    cut: {
        type: 'pandas_cut',
        name: 'Cut (Binning)',
        category: 'pandas-data-transformation',
        icon: 'bi-scissors',
        color: '#2e7d32',
        description: 'Bin values into discrete intervals',
        defaults: {
            column: '',
            bins: 5,
            labels: null,
            right: true
        },
        properties: [
            BaseNode.createProperty('column', 'Column', 'text', {
                required: true,
                placeholder: 'age',
                help: 'Column to bin'
            }),
            BaseNode.createProperty('bins', 'Number of Bins', 'number', {
                default: 5,
                min: 2,
                max: 100,
                help: 'Number of bins to create'
            }),
            BaseNode.createProperty('labels', 'Labels', 'text', {
                placeholder: 'Low,Medium,High',
                help: 'Comma-separated labels for bins'
            }),
            BaseNode.createProperty('right', 'Right Closed', 'boolean', {
                default: true,
                help: 'Indicates whether bins include the rightmost edge'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const outputVar = inputVar;
            const column = data.column || '';
            const bins = data.bins || 5;
            const labels = data.labels || null;
            const right = data.right !== false;
            
            if (!column) return `# Cut: Column required`;
            
            const params = [`bins=${bins}`, `right=${right}`];
            if (labels) {
                const labelList = labels.split(',').map(l => l.trim()).map(l => `'${l}'`).join(', ');
                params.push(`labels=[${labelList}]`);
            }
            
            let code = `${outputVar}['${column}_binned'] = pd.cut(${inputVar}['${column}'], ${params.join(', ')})\n`;
            code += `print(f'Binned {column} into {bins} categories')\n`;
            
            return code;
        }
    },

    qcut: {
        type: 'pandas_qcut',
        name: 'Quantile Cut',
        category: 'pandas-data-transformation',
        icon: 'bi-percent',
        color: '#c2185b',
        description: 'Quantile-based discretization function',
        defaults: {
            column: '',
            q: 4,
            labels: null
        },
        properties: [
            BaseNode.createProperty('column', 'Column', 'text', {
                required: true,
                placeholder: 'score',
                help: 'Column to bin by quantiles'
            }),
            BaseNode.createProperty('q', 'Number of Quantiles', 'number', {
                default: 4,
                min: 2,
                max: 100,
                help: 'Number of quantiles (e.g., 4 for quartiles)'
            }),
            BaseNode.createProperty('labels', 'Labels', 'text', {
                placeholder: 'Q1,Q2,Q3,Q4',
                help: 'Comma-separated labels for quantiles'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const outputVar = inputVar;
            const column = data.column || '';
            const q = data.q || 4;
            const labels = data.labels || null;
            
            if (!column) return `# QCut: Column required`;
            
            const params = [`q=${q}`];
            if (labels) {
                const labelList = labels.split(',').map(l => l.trim()).map(l => `'${l}'`).join(', ');
                params.push(`labels=[${labelList}]`);
            }
            
            let code = `${outputVar}['${column}_quantile'] = pd.qcut(${inputVar}['${column}'], ${params.join(', ')})\n`;
            code += `print(f'Quantile-binned {column} into {q} groups')\n`;
            
            return code;
        }
    },

    astype: {
        type: 'pandas_astype',
        name: 'Convert Data Types',
        category: 'pandas-data-transformation',
        icon: 'bi-arrow-left-right',
        color: '#ff6b6b',
        description: 'Cast a pandas object to a specified dtype',
        defaults: {
            dtype: 'float64',
            columns: ''
        },
        properties: [
            BaseNode.createProperty('columns', 'Columns', 'text', {
                required: true,
                placeholder: 'col1, col2',
                help: 'Comma-separated columns to convert'
            }),
            BaseNode.createProperty('dtype', 'Data Type', 'select', {
                default: 'float64',
                options: ['int64', 'float64', 'str', 'bool', 'datetime64', 'category'],
                help: 'Target data type'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const outputVar = inputVar + '_converted';
            const columns = data.columns || '';
            const dtype = data.dtype || 'float64';
            
            if (!columns) return `# Astype: Columns required`;
            
            const colList = columns.split(',').map(c => c.trim());
            let code = `${outputVar} = ${inputVar}.copy()\n`;
            
            if (colList.length === 1) {
                code += `${outputVar}['${colList[0]}'] = ${outputVar}['${colList[0]}'].astype('${dtype}')\n`;
            } else {
                const colDict = colList.map(c => `'${c}': '${dtype}'`).join(', ');
                code += `${outputVar} = ${outputVar}.astype({${colDict}})\n`;
            }
            
            code += `print(f'Converted {columns} to {dtype}')\n`;
            
            if (context) {
                context.setVariable(node.id, outputVar);
            }
            
            return code;
        }
    },

    transpose: {
        type: 'pandas_transpose',
        name: 'Transpose',
        category: 'pandas-data-transformation',
        icon: 'bi-arrow-down-up',
        color: '#4ecdc4',
        description: 'Transpose index and columns',
        defaults: {},
        properties: [],
        generateCode: (node, context) => {
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const outputVar = inputVar + '_transposed';
            
            let code = `${outputVar} = ${inputVar}.T\n`;
            code += `print(f'Transposed: {${inputVar}.shape} -> {${outputVar}.shape}')\n`;
            
            if (context) {
                context.setVariable(node.id, outputVar);
            }
            
            return code;
        }
    },

    stack: {
        type: 'pandas_stack',
        name: 'Stack',
        category: 'pandas-data-transformation',
        icon: 'bi-stack',
        color: '#0277bd',
        description: 'Stack the prescribed level(s) from columns to index',
        defaults: {
            level: -1,
            dropna: true
        },
        properties: [
            BaseNode.createProperty('dropna', 'Drop NA', 'boolean', {
                default: true,
                help: 'Whether to drop rows with missing values'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const outputVar = inputVar + '_stacked';
            const dropna = data.dropna !== false;
            
            let code = `${outputVar} = ${inputVar}.stack(dropna=${dropna})\n`;
            code += `print(f'Stacked shape: {${outputVar}.shape}')\n`;
            
            if (context) {
                context.setVariable(node.id, outputVar);
            }
            
            return code;
        }
    },

    unstack: {
        type: 'pandas_unstack',
        name: 'Unstack',
        category: 'pandas-data-transformation',
        icon: 'bi-layers',
        color: '#e65100',
        description: 'Pivot a level of the (necessarily hierarchical) index labels',
        defaults: {
            level: -1,
            fill_value: null
        },
        properties: [
            BaseNode.createProperty('level', 'Level', 'number', {
                default: -1,
                help: 'Level(s) to unstack (default: -1, last level)'
            }),
            BaseNode.createProperty('fill_value', 'Fill Value', 'text', {
                placeholder: '0 or leave empty',
                help: 'Replace NaN with this value'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const outputVar = inputVar + '_unstacked';
            const level = data.level !== undefined ? data.level : -1;
            const fillValue = data.fill_value || null;
            
            const params = [`level=${level}`];
            if (fillValue !== null) {
                const numVal = parseFloat(fillValue);
                const fillVal = isNaN(numVal) ? `'${fillValue}'` : fillValue;
                params.push(`fill_value=${fillVal}`);
            }
            
            let code = `${outputVar} = ${inputVar}.unstack(${params.join(', ')})\n`;
            code += `print(f'Unstacked shape: {${outputVar}.shape}')\n`;
            
            if (context) {
                context.setVariable(node.id, outputVar);
            }
            
            return code;
        }
    }
};

// Register all pandas data transformation nodes
Object.values(PandasDataTransformationNodes).forEach(nodeDef => {
    BaseNode.validateDefinition(nodeDef);
    nodeRegistry.register(nodeDef);
});

// Export
if (typeof module !== 'undefined' && module.exports) {
    module.exports = PandasDataTransformationNodes;
}

