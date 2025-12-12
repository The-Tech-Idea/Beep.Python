/**
 * Workflow Loader
 * Dynamically loads node definitions from separate files
 */

class WorkflowLoader {
    constructor() {
        this.loadedModules = new Set();
    }

    /**
     * Load a node module
     * @param {string} modulePath - Path to the module file
     */
    async loadModule(modulePath) {
        if (this.loadedModules.has(modulePath)) {
            return; // Already loaded
        }

        try {
            // Create script element
            const script = document.createElement('script');
            script.src = modulePath;
            script.type = 'text/javascript';
            
            // Wait for script to load
            await new Promise((resolve, reject) => {
                script.onload = resolve;
                script.onerror = () => reject(new Error(`Failed to load module: ${modulePath}`));
                document.head.appendChild(script);
            });

            this.loadedModules.add(modulePath);
            console.log(`Loaded workflow module: ${modulePath}`);
        } catch (error) {
            console.error(`Error loading module ${modulePath}:`, error);
            throw error;
        }
    }

    /**
     * Load all default node modules
     */
    async loadAllModules() {
        const modules = [
            '/static/js/workflow/nodes/base-node.js',
            '/static/js/workflow/node-registry.js',
            '/static/js/workflow/nodes/data-sources.js',
            '/static/js/workflow/nodes/data-management.js',
            '/static/js/workflow/nodes/preprocessing.js',
            '/static/js/workflow/nodes/algorithms.js',
            '/static/js/workflow/nodes/evaluation.js',
            '/static/js/workflow/nodes/output.js'
        ];

        for (const module of modules) {
            try {
                await this.loadModule(module);
            } catch (error) {
                console.warn(`Could not load ${module}, continuing...`, error);
            }
        }
    }

    /**
     * Load custom node modules from a directory
     * @param {string} directory - Directory containing custom node files
     */
    async loadCustomModules(directory) {
        // This would typically fetch a manifest or list of files
        // For now, it's a placeholder for future extensibility
        console.log(`Loading custom modules from: ${directory}`);
    }
}

// Global loader instance
const workflowLoader = new WorkflowLoader();

// Export
if (typeof module !== 'undefined' && module.exports) {
    module.exports = { WorkflowLoader, workflowLoader };
}

