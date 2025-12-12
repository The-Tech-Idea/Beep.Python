/**
 * Node Registration Helper
 * Ensures all nodes are registered properly with error handling
 */

(function() {
    'use strict';
    
    // Wait for dependencies
    function waitForDependencies(callback, maxAttempts = 50) {
        let attempts = 0;
        const check = () => {
            if (typeof BaseNode !== 'undefined' && typeof nodeRegistry !== 'undefined') {
                callback();
            } else if (attempts < maxAttempts) {
                attempts++;
                setTimeout(check, 100);
            } else {
                console.error('Dependencies not available after', maxAttempts, 'attempts');
            }
        };
        check();
    }
    
    // Register nodes safely
    function registerNodes(nodeObject, nodeTypeName) {
        waitForDependencies(() => {
            try {
                let count = 0;
                Object.values(nodeObject).forEach(nodeDef => {
                    try {
                        if (BaseNode && BaseNode.validateDefinition) {
                            BaseNode.validateDefinition(nodeDef);
                        }
                        if (nodeRegistry && nodeRegistry.register) {
                            nodeRegistry.register(nodeDef);
                            count++;
                        }
                    } catch (error) {
                        console.error(`Failed to register node ${nodeDef.type || 'unknown'}:`, error);
                    }
                });
                if (count > 0) {
                    console.log(`✓ Registered ${count} ${nodeTypeName} nodes`);
                }
            } catch (error) {
                console.error(`Error registering ${nodeTypeName} nodes:`, error);
            }
        });
    }
    
    // Make registerNodes available globally for node files
    window.registerNodes = registerNodes;
    
    // Log when dependencies are ready
    waitForDependencies(() => {
        console.log('Node registration dependencies ready');
        console.log('BaseNode:', typeof BaseNode !== 'undefined' ? 'available' : 'missing');
        console.log('nodeRegistry:', typeof nodeRegistry !== 'undefined' ? 'available' : 'missing');
    });
})();

