/**
 * Workflow Code Generator
 * Generates Python code from workflow using node definitions
 */

class WorkflowCodeGenerator {
    constructor(workflowData, nodeRegistry) {
        this.workflowData = workflowData;
        this.nodeRegistry = nodeRegistry;
        this.nodes = workflowData.nodes || [];
        this.edges = workflowData.edges || [];
        this.variableMap = {}; // Maps node IDs to variable names
        this.executionOrder = [];
        this.generatedCode = '';
        this.imports = new Set();
    }

    /**
     * Generate Python code from workflow
     */
    generate() {
        if (this.nodes.length === 0) {
            return '# Empty workflow - add nodes to generate code\n';
        }

        // Build dependency graph and determine execution order
        this.executionOrder = this._topologicalSort();
        
        if (this.executionOrder.length === 0) {
            return '# Error: Could not determine execution order. Check for circular dependencies.\n';
        }

        // Generate code for each node in order
        let code = '';
        
        // Generate imports
        code += this._generateImports() + '\n\n';
        
        // Generate code for each node
        for (const nodeId of this.executionOrder) {
            const node = this.nodes.find(n => n.id === nodeId);
            if (!node) continue;
            
            const nodeCode = this._generateNodeCode(node);
            if (nodeCode) {
                code += nodeCode + '\n\n';
            }
        }
        
        return code;
    }

    /**
     * Generate imports based on nodes
     */
    _generateImports() {
        const imports = new Set();
        
        // Always include these
        imports.add('import pandas as pd');
        imports.add('import numpy as np');
        imports.add('import os');
        
        // Collect imports from nodes
        for (const node of this.nodes) {
            const nodeDef = this.nodeRegistry.get(node.type);
            if (nodeDef && nodeDef.imports) {
                if (typeof nodeDef.imports === 'function') {
                    const nodeImports = nodeDef.imports(node);
                    if (Array.isArray(nodeImports)) {
                        nodeImports.forEach(imp => imports.add(imp));
                    } else if (nodeImports) {
                        imports.add(nodeImports);
                    }
                } else if (Array.isArray(nodeDef.imports)) {
                    nodeDef.imports.forEach(imp => imports.add(imp));
                } else if (nodeDef.imports) {
                    imports.add(nodeDef.imports);
                }
            }
        }
        
        // Add common imports based on node types
        const nodeTypes = this.nodes.map(n => n.type);
        if (nodeTypes.some(t => t.includes('split') || t.includes('train_test'))) {
            imports.add('from sklearn.model_selection import train_test_split');
        }
        if (nodeTypes.some(t => t.includes('scale') || t.includes('scaler'))) {
            imports.add('from sklearn.preprocessing import StandardScaler');
        }
        if (nodeTypes.some(t => t.includes('classifier'))) {
            imports.add('from sklearn.ensemble import RandomForestClassifier');
            imports.add('from sklearn.metrics import accuracy_score, classification_report');
        }
        if (nodeTypes.some(t => t.includes('regressor'))) {
            imports.add('from sklearn.ensemble import RandomForestRegressor');
            imports.add('from sklearn.metrics import mean_squared_error, r2_score');
        }
        
        return Array.from(imports).sort().join('\n');
    }

    /**
     * Generate code for a single node using its definition
     */
    _generateNodeCode(node) {
        const nodeDef = this.nodeRegistry.get(node.type);
        
        if (!nodeDef) {
            return `# Unknown node type: ${node.type}\n`;
        }
        
        if (!nodeDef.generateCode) {
            return `# Node ${nodeDef.name || node.type} has no code generator\n`;
        }
        
        // Create context for code generation
        const context = {
            getInputVariable: (targetNode) => this._getInputVariable(targetNode),
            getInputVariables: (targetNode) => this._getInputVariables(targetNode),
            setVariable: (nodeId, varName) => {
                this.variableMap[nodeId] = varName;
            },
            getVariable: (nodeId) => this.variableMap[nodeId] || null
        };
        
        try {
            // Call the node's generateCode function
            let code = nodeDef.generateCode(node, context);
            
            // Replace placeholder variables
            code = this._replacePlaceholders(code, node);
            
            // Track output variable if not already set
            if (!this.variableMap[node.id]) {
                const outputVar = this._getDefaultVariableName(node);
                if (outputVar) {
                    this.variableMap[node.id] = outputVar;
                }
            }
            
            return code;
        } catch (error) {
            console.error(`Error generating code for node ${node.id} (${node.type}):`, error);
            return `# Error generating code for ${nodeDef.name || node.type}: ${error.message}\n`;
        }
    }

    /**
     * Get input variable for a node (single input)
     */
    _getInputVariable(node) {
        const inputs = this._getInputVariables(node);
        // Return the first input, or a default
        if (inputs && Object.keys(inputs).length > 0) {
            return Object.values(inputs)[0];
        }
        
        // Check if this node has a default input variable
        if (node.type === 'start') {
            return null; // Start node has no input
        }
        
        // Special handling: if this is a scale node and the source is a split node, use X_train
        const incomingEdges = this.edges.filter(e => e.target === node.id);
        if (incomingEdges.length > 0) {
            const sourceNode = this.nodes.find(n => n.id === incomingEdges[0].source);
            if (sourceNode && (sourceNode.type.includes('split') || sourceNode.type.includes('train_test'))) {
                // Scale node receiving input from split node should use X_train
                return 'X_train';
            }
        }
        
        // Default fallback
        return 'df';
    }

    /**
     * Get input variables for a node (multiple inputs by port)
     */
    _getInputVariables(node) {
        const inputs = {};
        
        // Find all edges that connect to this node
        const incomingEdges = this.edges.filter(e => e.target === node.id);
        
        for (const edge of incomingEdges) {
            const sourceNode = this.nodes.find(n => n.id === edge.source);
            if (!sourceNode) continue;
            
            // Get the variable name from the source node
            // Check if source node has multiple outputs (like select_features_target)
            const sourcePort = edge.sourcePort || 'output';
            let sourceVar;
            
            if (sourceNode.type === 'preprocess_select_features_target') {
                // This node outputs both X and y
                if (sourcePort === 'features') {
                    sourceVar = 'X';
                } else if (sourcePort === 'target') {
                    sourceVar = 'y';
                } else {
                    sourceVar = this.variableMap[edge.source] || this._getDefaultVariableName(sourceNode);
                }
            } else if (sourceNode.type.includes('split') || sourceNode.type.includes('train_test')) {
                // Split node outputs X_train, X_test, y_train, y_test
                // Default output port should map to X_train for scale/train nodes
                if (sourcePort === 'output' || !sourcePort) {
                    sourceVar = 'X_train'; // Default to X_train for scale/train nodes
                } else {
                    // Could have specific ports for X_train, X_test, etc. in the future
                    sourceVar = this.variableMap[edge.source] || 'X_train';
                }
            } else {
                // Regular single output node
                sourceVar = this.variableMap[edge.source] || this._getDefaultVariableName(sourceNode);
            }
            
            // Map by target port name
            const portName = edge.targetPort || 'input';
            inputs[portName] = sourceVar;
        }
        
        return inputs;
    }

    /**
     * Get default variable name for a node
     */
    _getDefaultVariableName(node) {
        const nodeDef = this.nodeRegistry.get(node.type);
        
        if (node.type === 'start') {
            return null;
        }
        if (node.type.includes('load') || node.type.includes('data_load')) {
            const varName = 'df';
            this.variableMap[node.id] = varName;
            return varName;
        }
        if (node.type.includes('select_features_target')) {
            // This node creates X and y - track both
            this.variableMap[node.id + '_features'] = 'X';
            this.variableMap[node.id + '_target'] = 'y';
            this.variableMap[node.id] = 'X'; // Default output
            return 'X';
        }
        if (node.type.includes('split') || node.type.includes('train_test')) {
            // Split creates X_train, X_test, y_train, y_test
            // Default to X_train for the main output
            const varName = 'X_train';
            this.variableMap[node.id] = varName;
            return varName;
        }
        if (node.type.includes('scale') || node.type.includes('scaler')) {
            // Scaling modifies X_train and X_test in place
            return 'X_train';
        }
        if (node.type.includes('classifier') || node.type.includes('regressor')) {
            const varName = 'model';
            this.variableMap[node.id] = varName;
            return varName;
        }
        if (node.type.includes('evaluate')) {
            const varName = 'metrics';
            this.variableMap[node.id] = varName;
            return varName;
        }
        
        // Generic fallback
        const varName = `result_${node.id.slice(-6)}`;
        this.variableMap[node.id] = varName;
        return varName;
    }

    /**
     * Replace placeholder variables in generated code
     */
    _replacePlaceholders(code, node) {
        // Replace ${input} with actual input variable
        const inputVar = this._getInputVariable(node);
        if (inputVar) {
            code = code.replace(/\$\{input\}/g, inputVar);
            code = code.replace(/\{input\}/g, inputVar);
        }
        
        // Replace ${output} with node's output variable
        const outputVar = this.variableMap[node.id] || this._getDefaultVariableName(node);
        if (outputVar) {
            code = code.replace(/\$\{output\}/g, outputVar);
            code = code.replace(/\{output\}/g, outputVar);
        }
        
        return code;
    }

    /**
     * Topological sort to determine execution order
     */
    _topologicalSort() {
        // Build dependency graph
        const graph = {};
        const inDegree = {};
        
        // Initialize
        for (const node of this.nodes) {
            graph[node.id] = [];
            inDegree[node.id] = 0;
        }
        
        // Build graph from edges
        for (const edge of this.edges) {
            if (graph[edge.source] && graph[edge.target]) {
                graph[edge.source].push(edge.target);
                inDegree[edge.target]++;
            }
        }
        
        // Find start nodes (nodes with no incoming edges)
        const queue = [];
        for (const node of this.nodes) {
            if (inDegree[node.id] === 0) {
                queue.push(node.id);
            }
        }
        
        // If no start nodes, try to find 'start' type nodes
        if (queue.length === 0) {
            const startNodes = this.nodes.filter(n => n.type === 'start');
            if (startNodes.length > 0) {
                queue.push(...startNodes.map(n => n.id));
            } else {
                // Use first node as start
                if (this.nodes.length > 0) {
                    queue.push(this.nodes[0].id);
                }
            }
        }
        
        // Topological sort
        const result = [];
        while (queue.length > 0) {
            const current = queue.shift();
            result.push(current);
            
            // Update in-degrees of neighbors
            for (const neighbor of graph[current] || []) {
                inDegree[neighbor]--;
                if (inDegree[neighbor] === 0) {
                    queue.push(neighbor);
                }
            }
        }
        
        // Check for cycles
        if (result.length !== this.nodes.length) {
            console.warn('Possible cycle detected in workflow graph');
            // Add remaining nodes
            for (const node of this.nodes) {
                if (!result.includes(node.id)) {
                    result.push(node.id);
                }
            }
        }
        
        return result;
    }
}

// Export
if (typeof module !== 'undefined' && module.exports) {
    module.exports = WorkflowCodeGenerator;
}

