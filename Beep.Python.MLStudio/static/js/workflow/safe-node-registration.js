/**
 * Safe Node Registration Wrapper
 * Ensures all nodes register properly even if loaded out of order
 */

(function() {
    'use strict';
    
    // Queue for nodes that try to register before dependencies are ready
    const registrationQueue = [];
    let dependenciesReady = false;
    
    // Check if dependencies are ready
    function checkDependencies() {
        return typeof BaseNode !== 'undefined' && 
               typeof nodeRegistry !== 'undefined' &&
               typeof BaseNode.validateDefinition === 'function' &&
               typeof nodeRegistry.register === 'function';
    }
    
    // Process queued registrations
    function processQueue() {
        if (!checkDependencies()) return;
        
        dependenciesReady = true;
        console.log('Processing queued node registrations...');
        
        registrationQueue.forEach(({ nodes, name }) => {
            try {
                let count = 0;
                Object.values(nodes).forEach(nodeDef => {
                    try {
                        BaseNode.validateDefinition(nodeDef);
                        nodeRegistry.register(nodeDef);
                        count++;
                    } catch (error) {
                        console.error(`Failed to register node ${nodeDef.type || 'unknown'}:`, error);
                    }
                });
                if (count > 0) {
                    console.log(`✓ Registered ${count} ${name} nodes`);
                }
            } catch (error) {
                console.error(`Error registering ${name} nodes:`, error);
            }
        });
        
        registrationQueue.length = 0; // Clear queue
    }
    
    // Safe registration function
    window.registerNodesSafely = function(nodes, name) {
        if (checkDependencies()) {
            // Dependencies ready, register immediately
            let count = 0;
            Object.values(nodes).forEach(nodeDef => {
                try {
                    BaseNode.validateDefinition(nodeDef);
                    nodeRegistry.register(nodeDef);
                    count++;
                } catch (error) {
                    console.error(`Failed to register node ${nodeDef.type || 'unknown'}:`, error);
                }
            });
            if (count > 0) {
                console.log(`✓ Registered ${count} ${name} nodes`);
            }
        } else {
            // Queue for later
            registrationQueue.push({ nodes, name });
            console.log(`Queued ${name} nodes for registration (${Object.keys(nodes).length} nodes)`);
        }
    };
    
    // Check periodically and process queue
    let checkInterval = setInterval(() => {
        if (checkDependencies() && !dependenciesReady) {
            processQueue();
            clearInterval(checkInterval);
        }
    }, 100);
    
    // Also check on window load
    window.addEventListener('load', () => {
        if (checkDependencies() && registrationQueue.length > 0) {
            processQueue();
        }
    });
    
    // Stop checking after 10 seconds
    setTimeout(() => {
        clearInterval(checkInterval);
        if (registrationQueue.length > 0) {
            console.warn(`Still have ${registrationQueue.length} node groups queued after timeout`);
        }
    }, 10000);
})();

