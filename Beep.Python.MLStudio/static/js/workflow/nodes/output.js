/**
 * Output Nodes
 * Model saving and result export
 */

const OutputNodes = {
    saveModel: {
        type: 'save_model',
        name: 'Save Model',
        category: 'output',
        icon: 'bi-save',
        color: '#7b1fa2',
        description: 'Save trained model to disk',
        defaults: {
            file_path: 'models/model.pkl',
            format: 'pickle'
        },
        properties: [
            BaseNode.createProperty('file_path', 'Output Path', 'file', {
                required: true,
                default: 'models/model.pkl',
                placeholder: 'models/model.pkl',
                help: 'Path to save the model',
                fileFilter: '.pkl,.joblib',
                fileType: 'output'
            }),
            BaseNode.createProperty('format', 'Format', 'select', {
                default: 'pickle',
                options: ['pickle', 'joblib'],
                help: 'Model serialization format'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context.getInputVariable(node) || 'model';
            const filePath = data.file_path || 'models/model.pkl';
            const format = data.format || 'pickle';
            
            let code = `# Save Model Node (${node.id})\n`;
            code += `# Input: ${inputVar} (model from previous node)\n`;
            code += `# Output: Saved model file\n\n`;
            code += 'import os\n';
            code += `os.makedirs(os.path.dirname('${filePath}') or '.', exist_ok=True)\n\n`;
            
            if (format === 'joblib') {
                code += 'import joblib\n';
                code += `joblib.dump(model, '${filePath}')\n`;
            } else {
                code += 'import pickle\n';
                code += `with open('${filePath}', 'wb') as f:\n`;
                code += '    pickle.dump(model, f)\n';
            }
            
            code += `print(f'Model saved to: ${filePath}')\n`;
            
            // Register output
            context.setVariable(node.id, `'${filePath}'`);
            
            return code;
        }
    },

    exportResults: {
        type: 'export_results',
        name: 'Export Results',
        category: 'output',
        icon: 'bi-download',
        color: '#1976d2',
        description: 'Export results to file',
        defaults: {
            file_path: 'results/results.csv',
            format: 'csv'
        },
        properties: [
            BaseNode.createProperty('file_path', 'Output Path', 'file', {
                required: true,
                default: 'results/results.csv',
                placeholder: 'results/results.csv',
                fileFilter: '.csv,.json,.xlsx',
                fileType: 'output'
            }),
            BaseNode.createProperty('format', 'Format', 'select', {
                default: 'csv',
                options: ['csv', 'json', 'excel'],
                help: 'Export file format'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context.getInputVariable(node) || 'model';
            const filePath = data.file_path || 'results/results.csv';
            const format = data.format || 'csv';
            
            let code = `# Export Results Node (${node.id})\n`;
            code += `# Input: ${inputVar} (model from previous node)\n`;
            code += `# Output: Results file\n\n`;
            code += 'import os\n';
            code += `os.makedirs(os.path.dirname('${filePath}') or '.', exist_ok=True)\n\n`;
            
            code += '# Get test data from pipeline\n';
            code += 'try:\n';
            code += '    X_test_pred = X_test_input\n';
            code += 'except NameError:\n';
            code += '    try:\n';
            code += '        X_test_pred = X_test_scaled\n';
            code += '    except NameError:\n';
            code += '        X_test_pred = X_test\n\n';
            
            code += 'y_pred = model.predict(X_test_pred)\n';
            code += 'results = pd.DataFrame({\n';
            code += '    "y_actual": y_test,\n';
            code += '    "y_predicted": y_pred\n';
            code += '})\n\n';
            
            if (format === 'csv') {
                code += `results.to_csv('${filePath}', index=False)\n`;
            } else if (format === 'json') {
                code += `results.to_json('${filePath}', orient='records')\n`;
            } else if (format === 'excel') {
                code += `results.to_excel('${filePath}', index=False)\n`;
            }
            
            code += `print(f'Results exported to: ${filePath} ({len(results)} rows)')\n`;
            
            // Register output
            context.setVariable(node.id, 'results');
            
            return code;
        }
    }
};

// Register all output nodes
Object.values(OutputNodes).forEach(nodeDef => {
    BaseNode.validateDefinition(nodeDef);
    nodeRegistry.register(nodeDef);
});

// Export
if (typeof module !== 'undefined' && module.exports) {
    module.exports = OutputNodes;
}

