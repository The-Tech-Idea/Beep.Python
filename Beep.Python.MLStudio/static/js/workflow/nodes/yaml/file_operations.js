/**
 * YAML File Operations Nodes
 * YAML file reading and writing
 */

const YAMLFileOperationsNodes = {
    readYAML: {
        type: 'yaml_read',
        name: 'Read YAML',
        category: 'yaml-file-operations',
        icon: 'bi-file-earmark-code',
        color: '#c2185b',
        description: 'Read YAML file',
        defaults: {
            file_path: 'config.yaml',
            variable_name: 'config'
        },
        properties: [
            BaseNode.createProperty('file_path', 'File Path', 'text', {
                required: true,
                placeholder: 'config.yaml'
            }),
            BaseNode.createProperty('variable_name', 'Variable Name', 'text', {
                default: 'config'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const varName = data.variable_name || 'config';
            const filePath = data.file_path || 'config.yaml';
            
            let code = 'import yaml\n';
            code += `with open('${filePath}', 'r') as f:\n`;
            code += `    ${varName} = yaml.safe_load(f)\n`;
            code += `print(f'Loaded YAML from {filePath}')\n`;
            code += `print(${varName})\n`;
            
            if (context) {
                context.setVariable(node.id, varName);
            }
            
            return code;
        }
    },

    writeYAML: {
        type: 'yaml_write',
        name: 'Write YAML',
        category: 'yaml-file-operations',
        icon: 'bi-file-earmark-code',
        color: '#7b1fa2',
        description: 'Write data to YAML file',
        defaults: {
            file_path: 'output.yaml',
            data_variable: 'data'
        },
        properties: [
            BaseNode.createProperty('file_path', 'File Path', 'text', {
                required: true,
                placeholder: 'output.yaml'
            }),
            BaseNode.createProperty('data_variable', 'Data Variable', 'text', {
                default: 'data',
                help: 'Variable name containing data to write'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const filePath = data.file_path || 'output.yaml';
            const dataVar = data.data_variable || 'data';
            
            let code = 'import yaml\n';
            code += `with open('${filePath}', 'w') as f:\n`;
            code += `    yaml.dump(${dataVar}, f, default_flow_style=False)\n`;
            code += `print(f'Written YAML to {filePath}')\n`;
            
            return code;
        }
    }
};

// Register all YAML nodes
Object.values(YAMLFileOperationsNodes).forEach(nodeDef => {
    BaseNode.validateDefinition(nodeDef);
    nodeRegistry.register(nodeDef);
});

// Export
if (typeof module !== 'undefined' && module.exports) {
    module.exports = YAMLFileOperationsNodes;
}

