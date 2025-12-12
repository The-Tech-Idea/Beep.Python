/**
 * Start Node
 * Default entry point for ML workflows
 */

// Prevent duplicate declaration
if (typeof StartNode === 'undefined') {
    var StartNode = {
        start: {
            type: 'start',
            name: 'Start',
            category: 'system',
            icon: 'bi-play-circle-fill',
            color: '#28a745',
            description: 'Workflow entry point - Start your ML pipeline here',
            ports: {
                inputs: [],  // No inputs - this is the start
                outputs: [
                    { name: 'output', label: 'Start' }
                ]
            },
            defaults: {
                message: 'Workflow started'
            },
            properties: [
                BaseNode.createProperty('message', 'Start Message', 'text', {
                    default: 'Workflow started',
                    placeholder: 'Workflow started',
                    help: 'Message to display when workflow starts'
                })
            ],
            generateCode: (node, context) => {
                const data = node.data || {};
                const message = data.message || 'Workflow started';
                
                let code = `# ML Workflow Pipeline\n`;
                code += `# ${message}\n`;
                code += `print('${message}')\n`;
                code += `print('=' * 50)\n\n`;
                
                if (context) {
                    context.setVariable(node.id, 'start');
                }
                
                return code;
            }
        }
    };
}

// Register start node (safe registration)
if (typeof registerNodesSafely !== 'undefined') {
    registerNodesSafely(StartNode, 'start');
} else if (typeof BaseNode !== 'undefined' && typeof nodeRegistry !== 'undefined') {
    Object.values(StartNode).forEach(nodeDef => {
        try {
            BaseNode.validateDefinition(nodeDef);
            nodeRegistry.register(nodeDef);
        } catch (error) {
            console.error(`Failed to register start node:`, error);
        }
    });
    console.log('Registered start node');
} else {
    console.warn('Dependencies not ready for start-node.js, will retry');
    setTimeout(() => {
        if (typeof BaseNode !== 'undefined' && typeof nodeRegistry !== 'undefined') {
            Object.values(StartNode).forEach(nodeDef => {
                try {
                    BaseNode.validateDefinition(nodeDef);
                    nodeRegistry.register(nodeDef);
                } catch (error) {
                    console.error(`Failed to register start node:`, error);
                }
            });
        }
    }, 500);
}

// Export
if (typeof module !== 'undefined' && module.exports) {
    module.exports = StartNode;
}
