/**
 * Python Code Node
 * Allows users to write custom Python code in the workflow
 */

const PythonCodeNode = {
    python_code: {
        type: 'python_code',
        name: 'Python Code',
        category: 'custom',
        icon: 'bi-code-slash',
        color: '#6f42c1',
        description: 'Write custom Python code for your workflow',
        ports: {
            inputs: [
                { name: 'input', label: 'Input', optional: true }
            ],
            outputs: [
                { name: 'output', label: 'Output' }
            ]
        },
        defaults: {
            code: '# Write your Python code here\n# Access input data via the "input" variable\nresult = input if "input" in locals() else None\nprint("Custom code executed")'
        },
        properties: [
            BaseNode.createProperty('code', 'Python Code', 'textarea', {
                required: true,
                default: '# Write your Python code here\n# Access input data via the "input" variable\nresult = input if "input" in locals() else None\nprint("Custom code executed")',
                placeholder: '# Write your Python code here',
                help: 'Write custom Python code. Use the "Edit Code" button to open the full editor.',
                rows: 5
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const code = data.code || '# No code provided';
            const inputVar = context ? context.getInputVariable(node) : null;
            const outputVar = context ? context.getOutputVariable(node) : 'result';
            
            let generatedCode = `# Custom Python Code Block\n`;
            if (inputVar) {
                generatedCode += `# Input from previous node: ${inputVar}\n`;
                generatedCode += `input = ${inputVar}\n\n`;
            }
            generatedCode += `${code}\n\n`;
            if (inputVar) {
                generatedCode += `# Output variable: ${outputVar}\n`;
                generatedCode += `if 'result' not in locals():\n`;
                generatedCode += `    ${outputVar} = ${inputVar}  # Default: pass through input\n`;
                generatedCode += `else:\n`;
                generatedCode += `    ${outputVar} = result\n`;
            } else {
                generatedCode += `# Output variable: ${outputVar}\n`;
                generatedCode += `if 'result' not in locals():\n`;
                generatedCode += `    ${outputVar} = None\n`;
                generatedCode += `else:\n`;
                generatedCode += `    ${outputVar} = result\n`;
            }
            
            if (context) {
                context.setVariable(node.id, outputVar);
            }
            
            return generatedCode;
        }
    }
};

// Register Python code node
if (typeof registerNodesSafely !== 'undefined') {
    registerNodesSafely(PythonCodeNode, 'python code');
} else if (typeof BaseNode !== 'undefined' && typeof nodeRegistry !== 'undefined') {
    Object.values(PythonCodeNode).forEach(nodeDef => {
        try {
            BaseNode.validateDefinition(nodeDef);
            nodeRegistry.register(nodeDef);
            console.log('Registered Python code node');
        } catch (error) {
            console.error(`Failed to register Python code node:`, error);
        }
    });
} else {
    console.warn('Dependencies not ready for python-code.js, will retry');
    setTimeout(() => {
        if (typeof BaseNode !== 'undefined' && typeof nodeRegistry !== 'undefined') {
            Object.values(PythonCodeNode).forEach(nodeDef => {
                try {
                    BaseNode.validateDefinition(nodeDef);
                    nodeRegistry.register(nodeDef);
                } catch (error) {
                    console.error(`Failed to register Python code node:`, error);
                }
            });
        }
    }, 500);
}

// Export
if (typeof module !== 'undefined' && module.exports) {
    module.exports = PythonCodeNode;
}

