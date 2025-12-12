/**
 * Data Source Nodes
 * Nodes for loading data from various sources
 */

const DataSourceNodes = {
    loadCSV: {
        type: 'data_load_csv',
        name: 'Load CSV',
        category: 'data-sources',
        icon: 'bi-file-earmark-spreadsheet',
        color: '#1976d2',
        description: 'Load data from CSV file',
        defaults: {
            file_path: 'data/your_dataset.csv',
            variable_name: 'df',
            delimiter: ',',
            header: true,
            encoding: 'utf-8'
        },
        properties: [
            BaseNode.createProperty('file_path', 'File Path', 'file', {
                required: true,
                placeholder: 'data/dataset.csv',
                help: 'Path to the CSV file',
                fileFilter: '.csv',
                fileType: 'input'
            }),
            BaseNode.createProperty('variable_name', 'Variable Name', 'text', {
                default: 'df',
                help: 'Name of the variable to store the loaded data'
            }),
            BaseNode.createProperty('delimiter', 'Delimiter', 'text', {
                default: ',',
                help: 'Field delimiter (comma, semicolon, tab, etc.)'
            }),
            BaseNode.createProperty('header', 'Has Header', 'boolean', {
                default: true,
                help: 'Whether the CSV file has a header row'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const varName = data.variable_name || 'df';
            const filePath = data.file_path || 'data/your_dataset.csv';
            const delimiter = data.delimiter || ',';
            const header = data.header !== false;
            
            let code = `# Load CSV Node (${node.id})\n`;
            code += `# Input: None (data source)\n`;
            code += `# Output: ${varName} (DataFrame)\n\n`;
            code += `${varName} = pd.read_csv('${filePath}', delimiter='${delimiter}', header=${header ? '0' : 'None'})\n`;
            code += `print(f'Loaded {${varName}.shape[0]} rows, {${varName}.shape[1]} columns from ${filePath}')\n`;
            
            // Register output in context for downstream nodes
            context.setVariable(node.id, varName);
            
            return code;
        }
    },

    loadJSON: {
        type: 'data_load_json',
        name: 'Load JSON',
        category: 'data-sources',
        icon: 'bi-file-earmark-code',
        color: '#0277bd',
        description: 'Load data from JSON file',
        defaults: {
            file_path: 'data/your_dataset.json',
            variable_name: 'df',
            orient: 'records'
        },
        properties: [
            BaseNode.createProperty('file_path', 'File Path', 'file', {
                required: true,
                placeholder: 'data/dataset.json',
                fileFilter: '.json',
                fileType: 'input'
            }),
            BaseNode.createProperty('variable_name', 'Variable Name', 'text', {
                default: 'df'
            }),
            BaseNode.createProperty('orient', 'Orientation', 'select', {
                default: 'records',
                options: ['records', 'index', 'values', 'table', 'split']
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const varName = data.variable_name || 'df';
            const filePath = data.file_path || 'data/your_dataset.json';
            const orient = data.orient || 'records';
            
            let code = `# Load JSON Node (${node.id})\n`;
            code += `# Input: None (data source)\n`;
            code += `# Output: ${varName} (DataFrame)\n\n`;
            code += `${varName} = pd.read_json('${filePath}', orient='${orient}')\n`;
            code += `print(f'Loaded {${varName}.shape[0]} rows, {${varName}.shape[1]} columns from ${filePath}')\n`;
            
            // Register output in context
            context.setVariable(node.id, varName);
            
            return code;
        }
    },

    loadExcel: {
        type: 'data_load_excel',
        name: 'Load Excel',
        category: 'data-sources',
        icon: 'bi-file-earmark-excel',
        color: '#7b1fa2',
        description: 'Load data from Excel file',
        defaults: {
            file_path: 'data/your_dataset.xlsx',
            variable_name: 'df',
            sheet_name: 0
        },
        properties: [
            BaseNode.createProperty('file_path', 'File Path', 'file', {
                required: true,
                placeholder: 'data/dataset.xlsx',
                fileFilter: '.xlsx,.xls',
                fileType: 'input'
            }),
            BaseNode.createProperty('variable_name', 'Variable Name', 'text', {
                default: 'df'
            }),
            BaseNode.createProperty('sheet_name', 'Sheet Name/Index', 'text', {
                default: '0',
                help: 'Sheet name or index (0 for first sheet)'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const varName = data.variable_name || 'df';
            const filePath = data.file_path || 'data/your_dataset.xlsx';
            const sheetName = data.sheet_name || 0;
            const sheetParam = isNaN(sheetName) ? `'${sheetName}'` : sheetName;
            
            let code = `# Load Excel Node (${node.id})\n`;
            code += `# Input: None (data source)\n`;
            code += `# Output: ${varName} (DataFrame)\n\n`;
            code += `${varName} = pd.read_excel('${filePath}', sheet_name=${sheetParam})\n`;
            code += `print(f'Loaded {${varName}.shape[0]} rows, {${varName}.shape[1]} columns from ${filePath}')\n`;
            
            // Register output in context
            context.setVariable(node.id, varName);
            
            return code;
        }
    }
};

// Register all data source nodes (safe registration)
if (typeof registerNodesSafely !== 'undefined') {
    registerNodesSafely(DataSourceNodes, 'data source');
} else if (typeof BaseNode !== 'undefined' && typeof nodeRegistry !== 'undefined') {
    Object.values(DataSourceNodes).forEach(nodeDef => {
        try {
            BaseNode.validateDefinition(nodeDef);
            nodeRegistry.register(nodeDef);
        } catch (error) {
            console.error(`Failed to register node ${nodeDef.type}:`, error);
        }
    });
    console.log(`Registered ${Object.keys(DataSourceNodes).length} data source nodes`);
} else {
    console.warn('Dependencies not ready for data-sources.js, will retry');
    setTimeout(() => {
        if (typeof BaseNode !== 'undefined' && typeof nodeRegistry !== 'undefined') {
            Object.values(DataSourceNodes).forEach(nodeDef => {
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
    module.exports = DataSourceNodes;
}

