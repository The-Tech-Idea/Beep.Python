/**
 * Base Node Interface
 * Standard interface that all nodes must implement
 */

class BaseNode {
    /**
     * Node definition structure
     * All nodes must follow this interface
     */
    static getDefinition() {
        return {
            type: '',              // Unique identifier (e.g., 'data_load_csv')
            name: '',             // Display name (e.g., 'Load CSV')
            category: '',         // Category (e.g., 'data-sources', 'algorithms')
            icon: '',             // Bootstrap icon class (e.g., 'bi-file-earmark-spreadsheet')
            color: '',            // Color code (e.g., '#1976d2')
            description: '',      // Short description
            
            // Default configuration values
            defaults: {},
            
            // Property definitions for the properties panel
            properties: [],
            
            // Port definitions for multiple inputs/outputs
            // If not specified, defaults to 1 input and 1 output
            ports: {
                inputs: [{ name: 'input', label: 'Input' }],  // Array of { name, label, optional }
                outputs: [{ name: 'output', label: 'Output' }] // Array of { name, label }
            },
            
            // Code generation function
            generateCode: null,   // Function(node, context) => string
            
            // Validation function
            validate: null        // Function(node) => { valid: boolean, errors: [] }
        };
    }

    /**
     * Validate node definition
     */
    static validateDefinition(def) {
        const required = ['type', 'name', 'category', 'icon', 'description', 'defaults', 'properties'];
        const missing = required.filter(field => !(field in def));
        
        if (missing.length > 0) {
            throw new Error(`Node definition missing required fields: ${missing.join(', ')}`);
        }

        // Validate properties structure
        if (!Array.isArray(def.properties)) {
            throw new Error('Properties must be an array');
        }

        for (const prop of def.properties) {
            if (!prop.key || !prop.label || !prop.type) {
                throw new Error('Property definition must have key, label, and type');
            }
        }

        return true;
    }

    /**
     * Create property definition
     */
    static createProperty(key, label, type, options = {}) {
        return {
            key,
            label,
            type,  // 'text', 'number', 'select', 'boolean', 'array', 'json'
            default: options.default || null,
            required: options.required || false,
            placeholder: options.placeholder || '',
            help: options.help || '',
            min: options.min,
            max: options.max,
            step: options.step,
            options: options.options || null,  // For select type
            ...options
        };
    }
}

// Export
if (typeof module !== 'undefined' && module.exports) {
    module.exports = BaseNode;
}

