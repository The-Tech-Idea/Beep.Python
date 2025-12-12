/**
 * Node Registry - Central registry for all workflow nodes
 * Provides a standard interface for node types
 */

class NodeRegistry {
    constructor() {
        this.nodes = new Map();
        this.categories = new Map();
    }

    /**
     * Register a node type
     * @param {Object} nodeDef - Node definition with standard interface
     */
    register(nodeDef) {
        if (!nodeDef.type || !nodeDef.name || !nodeDef.category) {
            throw new Error('Node definition must have type, name, and category');
        }

        // Validate standard interface
        const required = ['type', 'name', 'category', 'icon', 'description', 'defaults', 'properties'];
        for (const field of required) {
            if (!(field in nodeDef)) {
                throw new Error(`Node definition missing required field: ${field}`);
            }
        }

        this.nodes.set(nodeDef.type, nodeDef);
        
        // Add to category
        if (!this.categories.has(nodeDef.category)) {
            this.categories.set(nodeDef.category, []);
        }
        this.categories.get(nodeDef.category).push(nodeDef.type);
    }

    /**
     * Get node definition by type
     */
    get(type) {
        return this.nodes.get(type);
    }

    /**
     * Get all nodes in a category
     */
    getByCategory(category) {
        const nodeTypes = this.categories.get(category) || [];
        return nodeTypes.map(type => this.nodes.get(type));
    }

    /**
     * Get all categories
     */
    getCategories() {
        return Array.from(this.categories.keys());
    }

    /**
     * Get all registered nodes
     */
    getAll() {
        return Array.from(this.nodes.values());
    }

    /**
     * Check if node type is registered
     */
    has(type) {
        return this.nodes.has(type);
    }
}

// Global registry instance
const nodeRegistry = new NodeRegistry();

// Export for use in other modules
if (typeof module !== 'undefined' && module.exports) {
    module.exports = { NodeRegistry, nodeRegistry };
}

