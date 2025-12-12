/**
 * Workflow Builder
 * Main workflow builder that uses the node registry
 */

class WorkflowBuilder {
    constructor(canvasId, nodePaletteId, propertiesPanelId) {
        this.canvas = document.getElementById(canvasId);
        this.nodePalette = document.getElementById(nodePaletteId);
        this.propertiesPanel = document.getElementById(propertiesPanelId);
        this.jsPlumbInstance = null;
        this.workflowNodes = [];
        this.workflowEdges = [];
        this.currentNodeId = 0;
        this.currentWorkflowId = null;
        this.selectedNode = null;
        this.autoSaveEnabled = true;
        this.autoSaveTimer = null;
        this.projectId = null; // Will be set from template
    }

    /**
     * Initialize the workflow builder
     */
    async initialize() {
        // Wait for node registry to be populated
        await this.waitForRegistry();
        
        // Initialize jsPlumb
        this.initJsPlumb();
        
        // Build node palette from registry
        this.buildNodePalette();
        
        // Set up event listeners
        this.setupEventListeners();
    }

    /**
     * Wait for node registry to be ready
     */
    async waitForRegistry() {
        let attempts = 0;
        const maxAttempts = 150; // Increased timeout to 15 seconds
        
        while (attempts < maxAttempts) {
            if (typeof nodeRegistry !== 'undefined') {
                const nodeCount = nodeRegistry.getAll().length;
                
                if (attempts % 10 === 0) { // Log every second
                    console.log(`Waiting for nodes... Attempt ${attempts + 1}/${maxAttempts}, Found ${nodeCount} nodes`);
                }
                
                if (nodeCount > 0) {
                    console.log(`✓ Node registry ready with ${nodeCount} nodes`);
                    return;
                }
            } else {
                if (attempts % 10 === 0) {
                    console.log(`Node registry not defined yet... Attempt ${attempts + 1}/${maxAttempts}`);
                }
            }
            
            await new Promise(resolve => setTimeout(resolve, 100));
            attempts++;
        }
        
        // Even if timeout, try to proceed with whatever nodes we have
        const nodeCount = typeof nodeRegistry !== 'undefined' ? nodeRegistry.getAll().length : 0;
        console.warn(`Node registry timeout after ${maxAttempts} attempts. Found ${nodeCount} nodes. Proceeding anyway...`);
        
        if (nodeCount === 0) {
            throw new Error(`No nodes registered after ${maxAttempts} attempts. Please check browser console for errors.`);
        }
    }

    /**
     * Initialize jsPlumb
     */
    initJsPlumb() {
        // Check if jsPlumb is available
        if (typeof jsPlumb === 'undefined') {
            throw new Error('jsPlumb library not loaded. Please ensure jsplumb.min.js is loaded before workflow-builder.js');
        }
        
        // jsPlumb 2.x Community Edition API
        // The library structure: jsPlumb.jsPlumb is the main instance
        try {
            // jsPlumb 2.15.6 Community Edition uses jsPlumb.jsPlumb
            if (jsPlumb.jsPlumb) {
                // Check if getInstance exists
                if (typeof jsPlumb.jsPlumb.getInstance === 'function') {
                    this.jsPlumbInstance = jsPlumb.jsPlumb.getInstance({
                        container: this.canvas
                    });
                } else {
                    // Use jsPlumb.jsPlumb directly (default instance)
                    this.jsPlumbInstance = jsPlumb.jsPlumb;
                }
            } else {
                // Fallback: use jsPlumb directly
                this.jsPlumbInstance = jsPlumb;
            }
            
            // Set container
            if (this.jsPlumbInstance && typeof this.jsPlumbInstance.setContainer === 'function') {
                this.jsPlumbInstance.setContainer(this.canvas);
            }
            
            // Set default styles
            if (this.jsPlumbInstance && typeof this.jsPlumbInstance.importDefaults === 'function') {
                this.jsPlumbInstance.importDefaults({
                    PaintStyle: { stroke: '#0d6efd', strokeWidth: 2 },
                    EndpointStyle: { fill: '#0d6efd', outlineStroke: 'white', outlineWidth: 2 },
                    Connector: ['Flowchart', { stub: [10, 15], gap: 5, cornerRadius: 5, alwaysRespectStubs: true }],
                    Anchor: ['Top', 'Bottom'],
                    ConnectionsDetachable: true,
                    ConnectionOverlays: [
                        ['Arrow', { location: 1, width: 10, length: 10, foldback: 0.8 }]
                    ]
                });
            }
            
            // Enable connections - allow source to target connections
            if (this.jsPlumbInstance) {
                // Make all endpoints draggable for connections
                if (this.jsPlumbInstance.bind) {
                    this.jsPlumbInstance.bind('beforeDrop', (info) => {
                        // Allow connections from source (output) to target (input)
                        return true;
                    });
                    
                    // Enable endpoint dragging
                    this.jsPlumbInstance.bind('endpointClick', (endpoint) => {
                        console.log('Endpoint clicked:', endpoint);
                    });
                }
                
                // Set connection type to allow source->target
                if (typeof this.jsPlumbInstance.setConnectionType === 'function') {
                    this.jsPlumbInstance.setConnectionType('basic', {
                        anchor: 'AutoDefault',
                        paintStyle: { stroke: '#0d6efd', strokeWidth: 2 },
                        hoverPaintStyle: { stroke: '#0056b3', strokeWidth: 3 }
                    });
                }
            }
            
            console.log('✓ jsPlumb initialized successfully');
            
        } catch (error) {
            console.error('Error initializing jsPlumb:', error);
            // Fallback: use default instance
            this.jsPlumbInstance = jsPlumb.jsPlumb || jsPlumb;
            if (this.jsPlumbInstance && typeof this.jsPlumbInstance.setContainer === 'function') {
                this.jsPlumbInstance.setContainer(this.canvas);
            }
        }

        // Setup connection handlers
        // jsPlumb 2.x may not have 'ready' event, so setup handlers directly
        if (this.jsPlumbInstance && this.jsPlumbInstance.bind) {
            // Setup handlers directly (ready event may not exist in Community Edition)
            this.setupConnectionHandlers();
        } else {
            // Setup handlers directly
            this.setupConnectionHandlers();
        }
    }
    
    /**
     * Setup connection event handlers
     */
    setupConnectionHandlers() {
        if (!this.jsPlumbInstance) return;
        
        // Bind connection events
        const connectionHandler = (conn) => {
            // Extract connection information - handle different jsPlumb API formats
            let sourceId, targetId, sourceUuid, targetUuid;
            
            // Try to get endpoints first (most reliable)
            let sourceEndpoint = null;
            let targetEndpoint = null;
            
            if (conn.endpoints && conn.endpoints.length === 2) {
                sourceEndpoint = conn.endpoints[0];
                targetEndpoint = conn.endpoints[1];
            } else if (conn.getSource && conn.getTarget) {
                sourceEndpoint = conn.getSource();
                targetEndpoint = conn.getTarget();
            }
            
            // Extract from endpoints if available
            if (sourceEndpoint && targetEndpoint) {
                // Get element ID (node ID)
                if (sourceEndpoint.element) {
                    sourceId = sourceEndpoint.element.id || sourceEndpoint.element.getId();
                } else if (sourceEndpoint.getElement) {
                    const el = sourceEndpoint.getElement();
                    sourceId = el ? (el.id || el.getId()) : null;
                }
                
                if (targetEndpoint.element) {
                    targetId = targetEndpoint.element.id || targetEndpoint.element.getId();
                } else if (targetEndpoint.getElement) {
                    const el = targetEndpoint.getElement();
                    targetId = el ? (el.id || el.getId()) : null;
                }
                
                // Get endpoint UUID - safely check for method
                sourceUuid = sourceEndpoint.uuid || (typeof sourceEndpoint.getUuid === 'function' ? sourceEndpoint.getUuid() : null);
                targetUuid = targetEndpoint.uuid || (typeof targetEndpoint.getUuid === 'function' ? targetEndpoint.getUuid() : null);
            }
            
            // Fallback to direct properties
            if (!sourceId || !targetId) {
                if (conn.sourceId && conn.targetId) {
                    sourceId = conn.sourceId;
                    targetId = conn.targetId;
                } else if (conn.source && conn.target) {
                    // Handle both DOM elements and endpoint objects
                    if (conn.source.id) {
                        sourceId = conn.source.id;
                    } else if (typeof conn.source.getId === 'function') {
                        sourceId = conn.source.getId();
                    } else if (conn.source.node) {
                        sourceId = conn.source.node.id || (typeof conn.source.node.getId === 'function' ? conn.source.node.getId() : null);
                    }
                    
                    if (conn.target.id) {
                        targetId = conn.target.id;
                    } else if (typeof conn.target.getId === 'function') {
                        targetId = conn.target.getId();
                    } else if (conn.target.node) {
                        targetId = conn.target.node.id || (typeof conn.target.node.getId === 'function' ? conn.target.node.getId() : null);
                    }
                }
                
                // Safely get UUIDs
                if (!sourceUuid && conn.source) {
                    if (conn.source.uuid) {
                        sourceUuid = conn.source.uuid;
                    } else if (typeof conn.source.getUuid === 'function') {
                        try {
                            sourceUuid = conn.source.getUuid();
                        } catch (e) {
                            // Ignore error, will construct UUID below
                        }
                    }
                }
                if (!targetUuid && conn.target) {
                    if (conn.target.uuid) {
                        targetUuid = conn.target.uuid;
                    } else if (typeof conn.target.getUuid === 'function') {
                        try {
                            targetUuid = conn.target.getUuid();
                        } catch (e) {
                            // Ignore error, will construct UUID below
                        }
                    }
                }
            }
            
            if (!sourceId || !targetId) {
                console.error('Failed to extract connection information:', conn);
                return;
            }
            
            // Ensure we have port UUIDs - construct if missing
            if (!sourceUuid || sourceUuid === sourceId) {
                sourceUuid = `${sourceId}_output_0`;
            }
            if (!targetUuid || targetUuid === targetId) {
                targetUuid = `${targetId}_input_0`;
            }
            
            // Store connection with port information
            const edge = {
                id: `edge_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`,
                source: sourceId,
                target: targetId,
                sourcePort: sourceUuid,
                targetPort: targetUuid
            };
            
            // Check if edge already exists
            const existingEdge = this.workflowEdges.find(e => 
                e.source === sourceId && e.target === targetId &&
                e.sourcePort === edge.sourcePort && e.targetPort === edge.targetPort
            );
            
            if (!existingEdge) {
                this.workflowEdges.push(edge);
                console.log('Connection created:', edge);
                
                // Trigger auto-save
                if (this.autoSaveEnabled) {
                    clearTimeout(this.autoSaveTimer);
                    this.autoSaveTimer = setTimeout(() => {
                        this.autoSave();
                    }, 2000);
                }
            } else {
                console.log('Connection already exists:', edge);
            }
        };
        
        const connectionDetachedHandler = (conn) => {
            // Extract connection information - same logic as connectionHandler
            let sourceId, targetId, sourceUuid, targetUuid;
            
            if (conn.sourceId && conn.targetId) {
                sourceId = conn.sourceId;
                targetId = conn.targetId;
                sourceUuid = conn.sourceUuid || conn.sourceId;
                targetUuid = conn.targetUuid || conn.targetId;
            } else if (conn.source && conn.target) {
                sourceId = conn.source.id || (typeof conn.source.getId === 'function' ? conn.source.getId() : null);
                targetId = conn.target.id || (typeof conn.target.getId === 'function' ? conn.target.getId() : null);
                sourceUuid = conn.source.uuid || (typeof conn.source.getUuid === 'function' ? conn.source.getUuid() : null);
                targetUuid = conn.target.uuid || (typeof conn.target.getUuid === 'function' ? conn.target.getUuid() : null);
            } else if (conn.endpoints && conn.endpoints.length === 2) {
                const srcEp = conn.endpoints[0];
                const tgtEp = conn.endpoints[1];
                sourceId = srcEp.element ? (srcEp.element.id || (typeof srcEp.element.getId === 'function' ? srcEp.element.getId() : null)) : 
                         (typeof srcEp.getElement === 'function' ? (srcEp.getElement().id || srcEp.getElement().getId()) : null);
                targetId = tgtEp.element ? (tgtEp.element.id || (typeof tgtEp.element.getId === 'function' ? tgtEp.element.getId() : null)) : 
                         (typeof tgtEp.getElement === 'function' ? (tgtEp.getElement().id || tgtEp.getElement().getId()) : null);
                sourceUuid = srcEp.uuid || (typeof srcEp.getUuid === 'function' ? srcEp.getUuid() : null);
                targetUuid = tgtEp.uuid || (typeof tgtEp.getUuid === 'function' ? tgtEp.getUuid() : null);
            } else {
                sourceId = conn.sourceId || (conn.source && (conn.source.id || (typeof conn.source.getId === 'function' ? conn.source.getId() : null)));
                targetId = conn.targetId || (conn.target && (conn.target.id || (typeof conn.target.getId === 'function' ? conn.target.getId() : null)));
                sourceUuid = conn.sourceUuid || (conn.source && (conn.source.uuid || (typeof conn.source.getUuid === 'function' ? conn.source.getUuid() : null)));
                targetUuid = conn.targetUuid || (conn.target && (conn.target.uuid || (typeof conn.target.getUuid === 'function' ? conn.target.getUuid() : null)));
            }
            
            if (sourceId && targetId) {
                this.workflowEdges = this.workflowEdges.filter(e => 
                    !(e.source === sourceId && e.target === targetId && 
                      e.sourcePort === (sourceUuid || `${sourceId}_output_0`) && 
                      e.targetPort === (targetUuid || `${targetId}_input_0`))
                );
                console.log('Connection removed');
                
                // Trigger auto-save
                if (this.autoSaveEnabled) {
                    clearTimeout(this.autoSaveTimer);
                    this.autoSaveTimer = setTimeout(() => {
                        this.autoSave();
                    }, 2000);
                }
            }
        };
        
        if (this.jsPlumbInstance.bind) {
            this.jsPlumbInstance.bind('connection', connectionHandler);
            this.jsPlumbInstance.bind('connectionDetached', connectionDetachedHandler);
        } else if (this.jsPlumbInstance.on) {
            this.jsPlumbInstance.on('connection', connectionHandler);
            this.jsPlumbInstance.on('connectionDetached', connectionDetachedHandler);
        }
    }

    /**
     * Build node palette from registry
     */
    buildNodePalette() {
        if (!this.nodePalette) {
            console.error('Node palette element not found');
            return;
        }
        
        const categories = nodeRegistry.getCategories();
        console.log(`Building node palette with ${categories.length} categories`);
        
        if (categories.length === 0) {
            this.nodePalette.innerHTML = `
                <div class="text-center p-4 text-warning">
                    <i class="bi bi-exclamation-triangle" style="font-size: 2rem;"></i>
                    <p class="mt-2">No nodes registered</p>
                    <p class="small text-muted">Please refresh the page.</p>
                </div>
            `;
            return;
        }
        
        let html = '';

        categories.forEach(category => {
            const nodes = nodeRegistry.getByCategory(category);
            if (nodes.length === 0) return;

            const categoryName = this.formatCategoryName(category);
            html += `<div class="node-category" data-category="${category}">`;
            const categoryId = category.replace(/[^a-zA-Z0-9]/g, '_');
            html += `<div class="node-category-header" data-category-toggle="${category}">`;
            html += `<i class="bi bi-chevron-down category-toggle-icon"></i>`;
            html += `<span>${categoryName} (${nodes.length})</span>`;
            html += `</div>`;
            html += `<div class="node-category-content" data-category-content="${category}">`;

            nodes.forEach(nodeDef => {
                html += this.createNodePaletteItem(nodeDef);
            });

            html += `</div></div>`;
        });

        if (html === '') {
            this.nodePalette.innerHTML = `
                <div class="text-center p-4 text-warning">
                    <i class="bi bi-exclamation-triangle" style="font-size: 2rem;"></i>
                    <p class="mt-2">No nodes found in categories</p>
                </div>
            `;
            return;
        }

        this.nodePalette.innerHTML = html;
        this.makeNodesDraggable();
        this.setupCategoryToggles();
        this.setupCategoryClickHandlers();
        console.log('Node palette built successfully');
    }
    
    /**
     * Setup click handlers for category headers
     */
    setupCategoryClickHandlers() {
        const categoryHeaders = this.nodePalette.querySelectorAll('.node-category-header[data-category-toggle]');
        categoryHeaders.forEach(header => {
            header.addEventListener('click', (e) => {
                e.stopPropagation();
                const category = header.getAttribute('data-category-toggle');
                if (category) {
                    this.toggleCategory(category);
                }
            });
        });
    }
    
    /**
     * Toggle category collapse/expand
     */
    toggleCategory(category) {
        const categoryEl = this.nodePalette.querySelector(`[data-category="${category}"]`);
        if (!categoryEl) return;
        
        const contentEl = categoryEl.querySelector(`[data-category-content="${category}"]`);
        const headerEl = categoryEl.querySelector('.node-category-header');
        const iconEl = headerEl ? headerEl.querySelector('.category-toggle-icon') : null;
        
        if (!contentEl) return;
        
        const isCollapsed = categoryEl.classList.contains('collapsed');
        
        if (isCollapsed) {
            // Expand
            contentEl.style.display = '';
            if (iconEl) {
                iconEl.classList.remove('bi-chevron-right');
                iconEl.classList.add('bi-chevron-down');
            }
            categoryEl.classList.remove('collapsed');
            this.saveCategoryState(category, true);
        } else {
            // Collapse
            contentEl.style.display = 'none';
            if (iconEl) {
                iconEl.classList.remove('bi-chevron-down');
                iconEl.classList.add('bi-chevron-right');
            }
            categoryEl.classList.add('collapsed');
            this.saveCategoryState(category, false);
        }
    }
    
    /**
     * Setup category toggle functionality
     */
    setupCategoryToggles() {
        // Restore saved category states after a brief delay to ensure DOM is ready
        setTimeout(() => {
            const categories = nodeRegistry.getCategories();
            categories.forEach(category => {
                const savedState = this.getCategoryState(category);
                if (savedState === false) {
                    // Category was collapsed, collapse it now
                    const categoryEl = this.nodePalette.querySelector(`[data-category="${category}"]`);
                    if (categoryEl) {
                        const contentEl = categoryEl.querySelector(`[data-category-content="${category}"]`);
                        const iconEl = categoryEl.querySelector('.category-toggle-icon');
                        if (contentEl) {
                            contentEl.style.display = 'none';
                            if (iconEl) {
                                iconEl.classList.remove('bi-chevron-down');
                                iconEl.classList.add('bi-chevron-right');
                            }
                            categoryEl.classList.add('collapsed');
                        }
                    }
                }
            });
        }, 100);
    }
    
    /**
     * Save category state to localStorage
     */
    saveCategoryState(category, isExpanded) {
        try {
            const states = JSON.parse(localStorage.getItem('nodeCategoryStates') || '{}');
            states[category] = isExpanded;
            localStorage.setItem('nodeCategoryStates', JSON.stringify(states));
        } catch (e) {
            console.error('Error saving category state:', e);
        }
    }
    
    /**
     * Get category state from localStorage
     */
    getCategoryState(category) {
        try {
            const states = JSON.parse(localStorage.getItem('nodeCategoryStates') || '{}');
            return states[category] !== false; // Default to expanded (true)
        } catch (e) {
            return true; // Default to expanded
        }
    }

    /**
     * Format category name for display
     */
    formatCategoryName(category) {
        return category
            .split('-')
            .map(word => word.charAt(0).toUpperCase() + word.slice(1))
            .join(' ');
    }

    /**
     * Create node palette item HTML
     */
    createNodePaletteItem(nodeDef) {
        const searchTerms = [nodeDef.name, nodeDef.description, nodeDef.type].join(' ').toLowerCase();
        
        return `
            <div class="node-item" draggable="true" data-node-type="${nodeDef.type}" data-search="${searchTerms}">
                <div class="node-item-icon" style="background: ${this.getColorBackground(nodeDef.color)}; color: ${nodeDef.color};">
                    <i class="bi ${nodeDef.icon}"></i>
                </div>
                <div class="node-item-info">
                    <div class="node-item-name">${nodeDef.name}</div>
                    <div class="node-item-desc">${nodeDef.description}</div>
                </div>
            </div>
        `;
    }

    /**
     * Get color background (light version)
     */
    getColorBackground(color) {
        // Convert hex to rgba with low opacity
        const hex = color.replace('#', '');
        const r = parseInt(hex.substr(0, 2), 16);
        const g = parseInt(hex.substr(2, 2), 16);
        const b = parseInt(hex.substr(4, 2), 16);
        return `rgba(${r}, ${g}, ${b}, 0.1)`;
    }

    /**
     * Make nodes draggable
     */
    makeNodesDraggable() {
        const nodeItems = this.nodePalette.querySelectorAll('.node-item');
        nodeItems.forEach(item => {
            item.addEventListener('dragstart', (e) => {
                e.dataTransfer.setData('text/plain', item.dataset.nodeType);
                item.classList.add('dragging');
            });

            item.addEventListener('dragend', () => {
                item.classList.remove('dragging');
            });
        });
    }

    /**
     * Setup event listeners
     */
    setupEventListeners() {
        // Canvas drop
        this.canvas.addEventListener('dragover', (e) => {
            e.preventDefault();
            e.dataTransfer.dropEffect = 'copy';
        });

        this.canvas.addEventListener('drop', (e) => {
            e.preventDefault();
            const nodeType = e.dataTransfer.getData('text/plain');
            if (!nodeType) return;

            const rect = this.canvas.getBoundingClientRect();
            const x = e.clientX - rect.left;
            const y = e.clientY - rect.top;

            this.createNode(nodeType, x, y);
        });
    }

    /**
     * Create a node on the canvas
     * @param {string} type - Node type
     * @param {number} x - X position
     * @param {number} y - Y position
     * @param {string} customId - Optional custom node ID (for loading saved workflows)
     */
    createNode(type, x, y, customId = null) {
        const nodeDef = nodeRegistry.get(type);
        if (!nodeDef) {
            console.error(`Node type not found: ${type}`);
            return;
        }

        // Use custom ID if provided, otherwise generate one
        let nodeId;
        if (customId) {
            nodeId = customId;
            // Update currentNodeId to avoid conflicts
            const match = customId.match(/node_(\d+)/);
            if (match) {
                const idNum = parseInt(match[1]);
                if (idNum >= this.currentNodeId) {
                    this.currentNodeId = idNum + 1;
                }
            }
        } else {
            this.currentNodeId++;
            nodeId = `node_${this.currentNodeId}`;
        }

        // Create node element
        const node = document.createElement('div');
        node.id = nodeId;
        node.className = 'workflow-node';
        node.style.left = x + 'px';
        node.style.top = y + 'px';

        // Get port definitions (default to 1 input, 1 output if not specified)
        const ports = nodeDef.ports || {
            inputs: [{ name: 'input', label: 'Input' }],
            outputs: [{ name: 'output', label: 'Output' }]
        };
        
        // Build ports HTML
        let portsHTML = '<div class="workflow-node-ports">';
        
        // Input ports (on top/left)
        if (ports.inputs && ports.inputs.length > 0) {
            ports.inputs.forEach((port, index) => {
                const portId = `${nodeId}_input_${port.name || index}`;
                portsHTML += `
                    <div class="node-port input" data-port-id="${portId}" data-port-name="${port.name || 'input'}" data-port-type="input" title="${port.label || 'Input'}">
                        <span class="node-port-label">${port.label || 'Input'}</span>
                    </div>
                `;
            });
        }
        
        // Output ports (on bottom/right)
        if (ports.outputs && ports.outputs.length > 0) {
            ports.outputs.forEach((port, index) => {
                const portId = `${nodeId}_output_${port.name || index}`;
                portsHTML += `
                    <div class="node-port output" data-port-id="${portId}" data-port-name="${port.name || 'output'}" data-port-type="output" title="${port.label || 'Output'}">
                        <span class="node-port-label">${port.label || 'Output'}</span>
                    </div>
                `;
            });
        }
        
        portsHTML += '</div>';
        
        // Add data attributes to help with endpoint positioning
        const hasInputs = ports.inputs && ports.inputs.length > 0;
        const hasOutputs = ports.outputs && ports.outputs.length > 0;
        
        node.innerHTML = `
            <div class="workflow-node-header" style="background: linear-gradient(135deg, ${nodeDef.color} 0%, ${this.darkenColor(nodeDef.color)} 100%);">
                <span><i class="bi ${nodeDef.icon}"></i> ${nodeDef.name}</span>
                <button class="btn btn-sm btn-link text-white p-0" onclick="event.stopPropagation(); workflowBuilder.deleteNode('${nodeId}')" style="font-size: 0.75rem; opacity: 0.8;">
                    <i class="bi bi-x-lg"></i>
                </button>
            </div>
            <div class="workflow-node-body">${nodeDef.description}</div>
            ${portsHTML}
        `;
        
        // Set data attributes for CSS positioning
        if (hasInputs) node.setAttribute('data-has-inputs', 'true');
        if (hasOutputs) node.setAttribute('data-has-outputs', 'true');

        this.canvas.appendChild(node);

        // Make draggable (jsPlumb 2.x API)
        if (this.jsPlumbInstance && this.jsPlumbInstance.draggable) {
            try {
                this.jsPlumbInstance.draggable(nodeId, { containment: 'parent' });
            } catch (error) {
                console.error('Error making node draggable:', error);
                // Fallback: use jQuery draggable if available
                if (typeof $ !== 'undefined' && $.fn.draggable) {
                    $(`#${nodeId}`).draggable({ containment: 'parent' });
                }
            }
        }

        // Add endpoints for each port (jsPlumb 2.x API)
        // Wait a bit for DOM to be ready
        setTimeout(() => {
            if (this.jsPlumbInstance && this.jsPlumbInstance.addEndpoint) {
                try {
                    // Add input endpoints (on LEFT side to match port labels)
                    if (ports.inputs && ports.inputs.length > 0) {
                        ports.inputs.forEach((port, index) => {
                            const portId = `${nodeId}_input_${port.name || index}`;
                            const portElement = node.querySelector(`[data-port-id="${portId}"]`);
                            
                            if (portElement) {
                                // Calculate anchor position on LEFT side based on number of inputs
                                let anchorPos;
                                if (ports.inputs.length === 1) {
                                    anchorPos = 'Left';  // Single input on left side
                                } else {
                                    // Distribute inputs along left edge
                                    const ratio = (index + 1) / (ports.inputs.length + 1);
                                    anchorPos = [0, ratio, -1, 0];  // Left side, distributed vertically
                                }
                                
                                this.jsPlumbInstance.addEndpoint(nodeId, {
                                    anchor: anchorPos,
                                    uuid: portId,
                                    maxConnections: -1,  // Allow unlimited connections to same port
                                    isSource: false,  // Input ports are targets only
                                    isTarget: true,
                                    endpoint: ['Dot', { radius: 8 }],
                                    paintStyle: { fill: '#0d6efd', outlineStroke: 'white', outlineWidth: 2 },
                                    hoverPaintStyle: { fill: '#0056b3' },
                                    connectorStyle: { stroke: '#0d6efd', strokeWidth: 2 },
                                    connectorHoverStyle: { stroke: '#0056b3', strokeWidth: 3 }
                                });
                            }
                        });
                    }
                    
                    // Add output endpoints (on RIGHT side to match port labels)
                    if (ports.outputs && ports.outputs.length > 0) {
                        ports.outputs.forEach((port, index) => {
                            const portId = `${nodeId}_output_${port.name || index}`;
                            const portElement = node.querySelector(`[data-port-id="${portId}"]`);
                            
                            if (portElement) {
                                // Calculate anchor position on RIGHT side based on number of outputs
                                let anchorPos;
                                if (ports.outputs.length === 1) {
                                    anchorPos = 'Right';  // Single output on right side
                                } else {
                                    // Distribute outputs along right edge
                                    const ratio = (index + 1) / (ports.outputs.length + 1);
                                    anchorPos = [1, ratio, 1, 0];  // Right side, distributed vertically
                                }
                                
                                this.jsPlumbInstance.addEndpoint(nodeId, {
                                    anchor: anchorPos,
                                    uuid: portId,
                                    maxConnections: -1,  // Allow unlimited connections from same port
                                    isSource: true,   // Output ports are sources
                                    isTarget: false,
                                    endpoint: ['Dot', { radius: 8 }],
                                    paintStyle: { fill: '#28a745', outlineStroke: 'white', outlineWidth: 2 },
                                    hoverPaintStyle: { fill: '#218838' },
                                    connectorStyle: { stroke: '#28a745', strokeWidth: 2 },
                                    connectorHoverStyle: { stroke: '#218838', strokeWidth: 3 }
                                });
                            }
                        });
                    }
                } catch (error) {
                    console.error('Error adding endpoints:', error);
                }
            }
        }, 100);

        // Store node data with port information
        const nodeData = {
            id: nodeId,
            type: type,
            position: { x, y },
            data: { ...nodeDef.defaults },
            ports: {
                inputs: ports.inputs || [],
                outputs: ports.outputs || []
            }
        };
        this.workflowNodes.push(nodeData);
        
        // Trigger auto-save after a delay
        if (this.autoSaveEnabled) {
            clearTimeout(this.autoSaveTimer);
            this.autoSaveTimer = setTimeout(() => {
                this.autoSave();
            }, 2000); // Auto-save 2 seconds after last change
        }

        // Make selectable
        node.addEventListener('click', (e) => {
            if (e.target.closest('.btn')) return;
            this.selectNode(nodeId);
        });
        
        // Add double-click handler for Python code nodes
        let doubleClickTimer = null;
        node.addEventListener('dblclick', (e) => {
            if (e.target.closest('.btn')) return;
            const nodeData = this.workflowNodes.find(n => n.id === nodeId);
            if (nodeData && nodeData.type === 'python_code') {
                this.openCodeEditor(nodeId);
            }
        });
    }

    /**
     * Select a node
     */
    selectNode(nodeId) {
        // Deselect all
        document.querySelectorAll('.workflow-node').forEach(n => n.classList.remove('selected'));
        
        // Select this node
        const nodeEl = document.getElementById(nodeId);
        if (nodeEl) {
            nodeEl.classList.add('selected');
            this.selectedNode = this.workflowNodes.find(n => n.id === nodeId);
            this.showNodeProperties(nodeId);
        }
    }

    /**
     * Show node properties in properties panel
     */
    showNodeProperties(nodeId) {
        const node = this.workflowNodes.find(n => n.id === nodeId);
        if (!node) return;

        const nodeDef = nodeRegistry.get(node.type);
        if (!nodeDef) return;

        const propsContent = document.getElementById('propertiesContent');
        let html = `<h6 class="mb-3">${nodeDef.name}</h6>`;
        html += `<p class="text-muted small mb-3">${nodeDef.description}</p>`;

        // Add "Edit Code" button for Python code nodes
        if (node.type === 'python_code') {
            html += `
                <div class="mb-3">
                    <button class="btn btn-primary btn-sm w-100" onclick="workflowBuilder.openCodeEditor('${nodeId}')">
                        <i class="bi bi-code-slash"></i> Edit Code in Editor
                    </button>
                </div>
            `;
        }

        // Check if node has custom property renderer
        if (nodeDef.hasCustomProperties && typeof nodeDef.renderCustomProperties === 'function') {
            html += nodeDef.renderCustomProperties(nodeId, node.data || {});
        } else {
            // Generate property fields using standard renderer
            nodeDef.properties.forEach(prop => {
                html += this.createPropertyField(prop, nodeId, node.data[prop.key] !== undefined ? node.data[prop.key] : prop.default);
            });
        }

        propsContent.innerHTML = html;
    }

    /**
     * Create property field HTML
     */
    createPropertyField(prop, nodeId, value) {
        const valueStr = value !== null && value !== undefined ? value : '';
        const required = prop.required ? 'required' : '';
        const help = prop.help ? `<small class="form-text text-muted">${prop.help}</small>` : '';

        if (prop.type === 'boolean') {
            const checked = value === true ? 'checked' : '';
            return `
                <div class="mb-3">
                    <div class="form-check">
                        <input class="form-check-input" type="checkbox" id="prop_${prop.key}" ${checked} 
                               onchange="workflowBuilder.updateNodeProperty('${nodeId}', '${prop.key}', this.checked)">
                        <label class="form-check-label small" for="prop_${prop.key}">${prop.label}</label>
                    </div>
                    ${help}
                </div>
            `;
        } else if (prop.type === 'select') {
            const options = prop.options.map(opt => {
                const optValue = typeof opt === 'object' ? opt.value : opt;
                const optLabel = typeof opt === 'object' ? opt.label : opt;
                const selected = optValue === value ? 'selected' : '';
                return `<option value="${optValue}" ${selected}>${optLabel}</option>`;
            }).join('');
            
            return `
                <div class="mb-3">
                    <label class="form-label small">${prop.label}</label>
                    <select class="form-select form-select-sm" id="prop_${prop.key}" ${required}
                            onchange="workflowBuilder.updateNodeProperty('${nodeId}', '${prop.key}', this.value)">
                        ${options}
                    </select>
                    ${help}
                </div>
            `;
        } else if (prop.type === 'number') {
            const min = prop.min !== undefined ? `min="${prop.min}"` : '';
            const max = prop.max !== undefined ? `max="${prop.max}"` : '';
            const step = prop.step !== undefined ? `step="${prop.step}"` : '';
            
            return `
                <div class="mb-3">
                    <label class="form-label small">${prop.label}</label>
                    <input type="number" class="form-control form-control-sm" id="prop_${prop.key}" 
                           value="${valueStr}" ${required} ${min} ${max} ${step}
                           onchange="workflowBuilder.updateNodeProperty('${nodeId}', '${prop.key}', ${prop.step ? 'parseFloat' : 'parseInt'}(this.value))">
                    ${help}
                </div>
            `;
        } else if (prop.type === 'file' || prop.type === 'filepath') {
            // File path input with browse button
            const fileFilter = prop.fileFilter || '*';
            const fileType = prop.fileType || 'any'; // 'input', 'output', 'any'
            return `
                <div class="mb-3">
                    <label class="form-label small">${prop.label}</label>
                    <div class="input-group input-group-sm">
                        <input type="text" class="form-control form-control-sm" id="prop_${prop.key}" 
                               value="${valueStr}" ${required} placeholder="${prop.placeholder || 'Select or enter file path...'}"
                               onchange="workflowBuilder.updateNodeProperty('${nodeId}', '${prop.key}', this.value)">
                        <button class="btn btn-outline-secondary" type="button" 
                                onclick="workflowBuilder.openFileBrowser('${nodeId}', '${prop.key}', '${fileFilter}', '${fileType}')"
                                title="Browse files">
                            <i class="bi bi-folder2-open"></i>
                        </button>
                    </div>
                    ${help}
                </div>
            `;
        } else if (prop.type === 'directory' || prop.type === 'folder') {
            // Directory path input with browse button
            return `
                <div class="mb-3">
                    <label class="form-label small">${prop.label}</label>
                    <div class="input-group input-group-sm">
                        <input type="text" class="form-control form-control-sm" id="prop_${prop.key}" 
                               value="${valueStr}" ${required} placeholder="${prop.placeholder || 'Select or enter directory path...'}"
                               onchange="workflowBuilder.updateNodeProperty('${nodeId}', '${prop.key}', this.value)">
                        <button class="btn btn-outline-secondary" type="button" 
                                onclick="workflowBuilder.openDirectoryBrowser('${nodeId}', '${prop.key}')"
                                title="Browse directories">
                            <i class="bi bi-folder"></i>
                        </button>
                    </div>
                    ${help}
                </div>
            `;
        } else if (prop.type === 'textarea' || prop.type === 'multiline') {
            // Multi-line text input
            const rows = prop.rows || 3;
            return `
                <div class="mb-3">
                    <label class="form-label small">${prop.label}</label>
                    <textarea class="form-control form-control-sm" id="prop_${prop.key}" 
                              rows="${rows}" ${required} placeholder="${prop.placeholder || ''}"
                              onchange="workflowBuilder.updateNodeProperty('${nodeId}', '${prop.key}', this.value)">${valueStr}</textarea>
                    ${help}
                </div>
            `;
        } else if (prop.type === 'columns' || prop.type === 'column_select') {
            // Column selector (requires data context)
            return `
                <div class="mb-3">
                    <label class="form-label small">${prop.label}</label>
                    <div class="input-group input-group-sm">
                        <input type="text" class="form-control form-control-sm" id="prop_${prop.key}" 
                               value="${valueStr}" ${required} placeholder="${prop.placeholder || 'Enter column name(s)...'}"
                               onchange="workflowBuilder.updateNodeProperty('${nodeId}', '${prop.key}', this.value)">
                        <button class="btn btn-outline-secondary" type="button" 
                                onclick="workflowBuilder.showColumnSelector('${nodeId}', '${prop.key}', ${prop.multiple || false})"
                                title="Select columns">
                            <i class="bi bi-list-columns"></i>
                        </button>
                    </div>
                    ${help}
                </div>
            `;
        } else {
            // Check if this is likely a file path based on key name
            const isFilePath = prop.key.toLowerCase().includes('path') || 
                               prop.key.toLowerCase().includes('file') ||
                               prop.key.toLowerCase().includes('output') ||
                               prop.key.toLowerCase().includes('input') ||
                               prop.key.toLowerCase().includes('save') ||
                               prop.key.toLowerCase().includes('load');
            
            if (isFilePath) {
                // Auto-detect file path and add browse button
                return `
                    <div class="mb-3">
                        <label class="form-label small">${prop.label}</label>
                        <div class="input-group input-group-sm">
                            <input type="text" class="form-control form-control-sm" id="prop_${prop.key}" 
                                   value="${valueStr}" ${required} placeholder="${prop.placeholder || ''}"
                                   onchange="workflowBuilder.updateNodeProperty('${nodeId}', '${prop.key}', this.value)">
                            <button class="btn btn-outline-secondary" type="button" 
                                    onclick="workflowBuilder.openFileBrowser('${nodeId}', '${prop.key}', '*', 'any')"
                                    title="Browse files">
                                <i class="bi bi-folder2-open"></i>
                            </button>
                        </div>
                        ${help}
                    </div>
                `;
            }
            
            return `
                <div class="mb-3">
                    <label class="form-label small">${prop.label}</label>
                    <input type="text" class="form-control form-control-sm" id="prop_${prop.key}" 
                           value="${valueStr}" ${required} placeholder="${prop.placeholder || ''}"
                           onchange="workflowBuilder.updateNodeProperty('${nodeId}', '${prop.key}', this.value)">
                    ${help}
                </div>
            `;
        }
    }
    
    /**
     * Open file browser modal
     */
    openFileBrowser(nodeId, propertyKey, fileFilter = '*', fileType = 'any') {
        this.currentBrowseNodeId = nodeId;
        this.currentBrowsePropertyKey = propertyKey;
        this.currentBrowseFileFilter = fileFilter;
        
        // Show file browser modal
        if (typeof showFileBrowserModal === 'function') {
            showFileBrowserModal(fileFilter, fileType, (selectedPath) => {
                if (selectedPath) {
                    this.updateNodeProperty(nodeId, propertyKey, selectedPath);
                    document.getElementById(`prop_${propertyKey}`).value = selectedPath;
                }
            });
        } else {
            // Fallback: use simple prompt if modal not available
            const currentValue = document.getElementById(`prop_${propertyKey}`)?.value || '';
            const newPath = prompt('Enter file path:', currentValue);
            if (newPath !== null) {
                this.updateNodeProperty(nodeId, propertyKey, newPath);
                document.getElementById(`prop_${propertyKey}`).value = newPath;
            }
        }
    }
    
    /**
     * Open directory browser modal
     */
    openDirectoryBrowser(nodeId, propertyKey) {
        this.currentBrowseNodeId = nodeId;
        this.currentBrowsePropertyKey = propertyKey;
        
        // Show directory browser modal
        if (typeof showDirectoryBrowserModal === 'function') {
            showDirectoryBrowserModal((selectedPath) => {
                if (selectedPath) {
                    this.updateNodeProperty(nodeId, propertyKey, selectedPath);
                    document.getElementById(`prop_${propertyKey}`).value = selectedPath;
                }
            });
        } else {
            // Fallback: use simple prompt if modal not available
            const currentValue = document.getElementById(`prop_${propertyKey}`)?.value || '';
            const newPath = prompt('Enter directory path:', currentValue);
            if (newPath !== null) {
                this.updateNodeProperty(nodeId, propertyKey, newPath);
                document.getElementById(`prop_${propertyKey}`).value = newPath;
            }
        }
    }
    
    /**
     * Show column selector (for data-aware properties)
     */
    showColumnSelector(nodeId, propertyKey, multiple = false) {
        if (typeof showColumnSelectorModal === 'function') {
            showColumnSelectorModal(nodeId, propertyKey, multiple, (selectedColumns) => {
                if (selectedColumns) {
                    const value = multiple ? selectedColumns.join(', ') : selectedColumns;
                    this.updateNodeProperty(nodeId, propertyKey, value);
                    document.getElementById(`prop_${propertyKey}`).value = value;
                }
            });
        } else {
            // Fallback
            const currentValue = document.getElementById(`prop_${propertyKey}`)?.value || '';
            const newValue = prompt('Enter column name(s):', currentValue);
            if (newValue !== null) {
                this.updateNodeProperty(nodeId, propertyKey, newValue);
                document.getElementById(`prop_${propertyKey}`).value = newValue;
            }
        }
    }

    /**
     * Update node property
     */
    updateNodeProperty(nodeId, key, value) {
        const node = this.workflowNodes.find(n => n.id === nodeId);
        if (node) {
            if (!node.data) node.data = {};
            node.data[key] = value;
            
            // Trigger auto-save
            if (this.autoSaveEnabled) {
                clearTimeout(this.autoSaveTimer);
                this.autoSaveTimer = setTimeout(() => {
                    this.autoSave();
                }, 2000);
            }
        }
    }

    /**
     * Delete a node
     */
    deleteNode(nodeId) {
        const node = document.getElementById(nodeId);
        if (node) {
            if (this.jsPlumbInstance && this.jsPlumbInstance.removeAllEndpoints) {
                this.jsPlumbInstance.removeAllEndpoints(nodeId);
            }
            if (this.jsPlumbInstance && this.jsPlumbInstance.remove) {
                this.jsPlumbInstance.remove(nodeId);
            }
            node.remove();
            this.workflowNodes = this.workflowNodes.filter(n => n.id !== nodeId);
            this.workflowEdges = this.workflowEdges.filter(e => e.source !== nodeId && e.target !== nodeId);
            
            if (this.selectedNode && this.selectedNode.id === nodeId) {
                this.selectedNode = null;
                document.getElementById('propertiesContent').innerHTML = '<p class="text-muted text-center mt-4">Select a node to edit its properties</p>';
            }
        }
    }

    /**
     * Darken color for gradient
     */
    darkenColor(color) {
        const hex = color.replace('#', '');
        const r = Math.max(0, parseInt(hex.substr(0, 2), 16) - 30);
        const g = Math.max(0, parseInt(hex.substr(2, 2), 16) - 30);
        const b = Math.max(0, parseInt(hex.substr(4, 2), 16) - 30);
        return `rgb(${r}, ${g}, ${b})`;
    }

    /**
     * Get current zoom level from canvas transform
     */
    getCurrentZoom() {
        if (!this.canvas) return 1;
        const transform = this.canvas.style.transform || '';
        const match = transform.match(/scale\(([\d.]+)\)/);
        return match ? parseFloat(match[1]) : 1;
    }

    /**
     * Get workflow data
     */
    getWorkflowData() {
        const currentZoom = this.getCurrentZoom();
        const panX = window.zoomState?.panX || 0;
        const panY = window.zoomState?.panY || 0;
        
        const nodes = this.workflowNodes.map(node => {
            const el = document.getElementById(node.id);
            if (!el) {
                // Fallback if element not found
                return {
                    id: node.id,
                    type: node.type,
                    position: node.position || { x: 0, y: 0 },
                    data: node.data
                };
            }
            
            // Get actual CSS left/top values (not affected by transform)
            const left = parseFloat(el.style.left) || 0;
            const top = parseFloat(el.style.top) || 0;
            
            // Account for pan offset and save unscaled positions
            return {
                id: node.id,
                type: node.type,
                position: {
                    x: (left - panX) / currentZoom,
                    y: (top - panY) / currentZoom
                },
                data: node.data
            };
        });

        return {
            nodes,
            edges: this.workflowEdges,
            viewport: { 
                x: panX, 
                y: panY, 
                zoom: currentZoom 
            }
        };
    }

    /**
     * Code generation context
     */
    createCodeContext() {
        const variableMap = new Map();
        
        return {
            getInputVariable: (node) => {
                // Find the source node connected to this node
                const edge = this.workflowEdges.find(e => e.target === node.id);
                if (edge) {
                    return variableMap.get(edge.source) || 'df';
                }
                return 'df'; // Default
            },
            
            getOutputVariable: (node) => {
                return variableMap.get(node.id) || 'df';
            },
            
            setVariable: (nodeId, varName) => {
                variableMap.set(nodeId, varName);
            },
            
            projectFramework: 'scikit-learn', // Could be passed from project
            workflowData: this.getWorkflowData()
        };
    }

    /**
     * Load workflow data
     */
    loadWorkflowData(workflowData) {
        // Clear existing
        this.clearCanvas();
        
        if (!workflowData || !workflowData.nodes || workflowData.nodes.length === 0) {
            // Create default start node if no workflow data or empty nodes
            console.log('No workflow data or empty nodes, creating default start node');
            this.createDefaultStartNode();
            return;
        }

        // Restore viewport (zoom and pan) if saved
        if (workflowData.viewport) {
            const viewport = workflowData.viewport;
            if (viewport.zoom && typeof window.setZoom === 'function') {
                window.setZoom(viewport.zoom);
            }
            if (window.zoomState && (viewport.x !== undefined || viewport.y !== undefined)) {
                window.zoomState.panX = viewport.x || 0;
                window.zoomState.panY = viewport.y || 0;
                const canvas = this.canvas;
                if (canvas) {
                    canvas.style.transform = `translate(${window.zoomState.panX}px, ${window.zoomState.panY}px) scale(${window.zoomState.scale || 1})`;
                }
            }
        }

        // Create a mapping from old node IDs to new node IDs (in case IDs changed)
        const nodeIdMap = new Map();
        
        // Get current zoom for position restoration
        const currentZoom = this.getCurrentZoom();
        const panX = window.zoomState?.panX || 0;
        const panY = window.zoomState?.panY || 0;
        
        // Load nodes and create mapping - preserve original IDs
        workflowData.nodes.forEach(nodeData => {
            const savedId = nodeData.id;
            // Restore position accounting for zoom and pan
            const restoredX = (nodeData.position.x * currentZoom) + panX;
            const restoredY = (nodeData.position.y * currentZoom) + panY;
            // Create node with the saved ID and restored position
            this.createNode(nodeData.type, restoredX, restoredY, savedId);
            // Find the created node
            const newNode = this.workflowNodes.find(n => n.id === savedId);
            if (newNode) {
                // Update node data
                if (nodeData.data) {
                    newNode.data = { ...newNode.data, ...nodeData.data };
                }
                // Map old ID to current ID
                nodeIdMap.set(savedId, savedId);
            }
        });

        // Load edges after a delay to ensure nodes are rendered and endpoints are created
        setTimeout(() => {
            if (workflowData.edges && workflowData.edges.length > 0 && this.jsPlumbInstance) {
                let loadedCount = 0;
                workflowData.edges.forEach((edge, index) => {
                    // Stagger connections slightly to avoid race conditions
                    setTimeout(() => {
                        try {
                            // Map old node IDs to current node IDs
                            const sourceNodeId = nodeIdMap.get(edge.source) || edge.source;
                            const targetNodeId = nodeIdMap.get(edge.target) || edge.target;
                            const sourcePort = edge.sourcePort || `${sourceNodeId}_output_0`;
                            const targetPort = edge.targetPort || `${targetNodeId}_input_0`;
                            
                            // Verify nodes exist
                            const sourceNode = document.getElementById(sourceNodeId);
                            const targetNode = document.getElementById(targetNodeId);
                            
                            if (!sourceNode || !targetNode) {
                                console.warn(`Cannot connect: nodes not found`, { 
                                    sourceNodeId, 
                                    targetNodeId,
                                    sourceNode: !!sourceNode,
                                    targetNode: !!targetNode,
                                    availableNodes: Array.from(document.querySelectorAll('.workflow-node')).map(n => n.id)
                                });
                                // Skip this edge - node doesn't exist (might have been deleted)
                                return;
                            }
                            
                            // Verify nodes are in workflowNodes array
                            const sourceNodeData = this.workflowNodes.find(n => n.id === sourceNodeId);
                            const targetNodeData = this.workflowNodes.find(n => n.id === targetNodeId);
                            
                            if (!sourceNodeData || !targetNodeData) {
                                console.warn(`Cannot connect: nodes not in workflowNodes array`, {
                                    sourceNodeId,
                                    targetNodeId,
                                    sourceInArray: !!sourceNodeData,
                                    targetInArray: !!targetNodeData
                                });
                                return;
                            }
                            
                            // Connect using jsPlumb - get endpoints first
                            if (this.jsPlumbInstance && this.jsPlumbInstance.connect) {
                                try {
                                    // Get all endpoints for both nodes
                                    const sourceEndpoints = this.jsPlumbInstance.getEndpoints(sourceNodeId) || [];
                                    const targetEndpoints = this.jsPlumbInstance.getEndpoints(targetNodeId) || [];
                                    
                                    // Find matching endpoints by UUID
                                    let sourceEndpoint = sourceEndpoints.find(ep => {
                                        const epUuid = ep.uuid || (typeof ep.getUuid === 'function' ? ep.getUuid() : null);
                                        return epUuid === sourcePort || epUuid === `${sourceNodeId}_output_0` || epUuid === `${sourceNodeId}_output`;
                                    });
                                    
                                    let targetEndpoint = targetEndpoints.find(ep => {
                                        const epUuid = ep.uuid || (typeof ep.getUuid === 'function' ? ep.getUuid() : null);
                                        return epUuid === targetPort || epUuid === `${targetNodeId}_input_0` || epUuid === `${targetNodeId}_input`;
                                    });
                                    
                                    // Fallback: find first output/input endpoint if UUID match failed
                                    if (!sourceEndpoint) {
                                        sourceEndpoint = sourceEndpoints.find(ep => ep.isSource === true) || sourceEndpoints[0];
                                    }
                                    if (!targetEndpoint) {
                                        targetEndpoint = targetEndpoints.find(ep => ep.isTarget === true) || targetEndpoints[0];
                                    }
                                    
                                    // Try connecting by endpoints first (most reliable)
                                    if (sourceEndpoint && targetEndpoint) {
                                        const connection = this.jsPlumbInstance.connect({
                                            source: sourceEndpoint,
                                            target: targetEndpoint,
                                            paintStyle: { stroke: '#0d6efd', strokeWidth: 2 },
                                            hoverPaintStyle: { stroke: '#0a58ca', strokeWidth: 3 },
                                            endpoint: ['Dot', { radius: 8 }],
                                            overlays: [
                                                ['Arrow', { location: 1, width: 10, length: 10, foldback: 0.8 }]
                                            ]
                                        });
                                        
                                        if (connection) {
                                            loadedCount++;
                                            // Silent success - no console log to reduce noise
                                        } else {
                                            throw new Error('Connection returned null');
                                        }
                                    } else {
                                        // Fallback: connect by node IDs with anchors (endpoints not ready yet)
                                        const fallbackConn = this.jsPlumbInstance.connect({
                                            source: sourceNodeId,
                                            target: targetNodeId,
                                            anchors: ['Right', 'Left'],  // Right for output, Left for input
                                            paintStyle: { stroke: '#0d6efd', strokeWidth: 2 },
                                            endpoint: ['Dot', { radius: 8 }],
                                            overlays: [
                                                ['Arrow', { location: 1, width: 10, length: 10, foldback: 0.8 }]
                                            ]
                                        });
                                        if (fallbackConn) {
                                            loadedCount++;
                                            // Silent success - connection works via fallback
                                        } else {
                                            console.warn(`Failed to connect: ${sourceNodeId} -> ${targetNodeId} (endpoints: ${sourceEndpoints.length} source, ${targetEndpoints.length} target)`);
                                        }
                                    }
                                } catch (connError) {
                                    console.error('Error connecting:', connError, { sourcePort, targetPort, sourceNodeId, targetNodeId });
                                    // Try fallback connection
                                    try {
                                        const fallbackConn = this.jsPlumbInstance.connect({
                                            source: sourceNodeId,
                                            target: targetNodeId,
                                            anchors: ['Right', 'Left'],
                                            paintStyle: { stroke: '#0d6efd', strokeWidth: 2 }
                                        });
                                        if (fallbackConn) {
                                            loadedCount++;
                                            console.log(`✓ Connected (error fallback): ${sourceNodeId} -> ${targetNodeId}`);
                                        }
                                    } catch (fallbackError) {
                                        console.error('Fallback connection also failed:', fallbackError);
                                    }
                                }
                            }
                        } catch (error) {
                            console.error('Error loading edge:', error, edge);
                        }
                    }, index * 50); // Stagger connections by 50ms each
                });
                
                // Update workflowEdges array after all connections are attempted
                // Map old node IDs to new node IDs in edges and filter out edges to non-existent nodes
                setTimeout(() => {
                    const validNodeIds = new Set(this.workflowNodes.map(n => n.id));
                    this.workflowEdges = (workflowData.edges || [])
                        .map(edge => ({
                            ...edge,
                            source: nodeIdMap.get(edge.source) || edge.source,
                            target: nodeIdMap.get(edge.target) || edge.target
                        }))
                        .filter(edge => {
                            // Only keep edges where both source and target nodes exist
                            return validNodeIds.has(edge.source) && validNodeIds.has(edge.target);
                        });
                    console.log(`Loaded ${loadedCount} of ${workflowData.edges.length} connections (filtered to ${this.workflowEdges.length} valid edges)`);
                }, workflowData.edges.length * 50 + 500);
            } else {
                this.workflowEdges = workflowData.edges || [];
            }
        }, 800); // Increased delay to ensure endpoints are ready
    }
    
    /**
     * Create default start node
     */
    createDefaultStartNode() {
        // Wait a bit to ensure node registry is ready
        setTimeout(() => {
            // Create start node at center-top of canvas
            const canvasRect = this.canvas.getBoundingClientRect();
            const x = (canvasRect.width || 800) / 2 - 100;
            const y = 50;
            
            console.log('Creating default start node at:', x, y);
            
            // Check if start node type exists
            if (typeof nodeRegistry === 'undefined') {
                console.warn('Node registry not ready, retrying in 500ms...');
                setTimeout(() => this.createDefaultStartNode(), 500);
                return;
            }
            
            const startNodeDef = nodeRegistry.get('start');
            if (!startNodeDef) {
                console.warn('Start node type not found in registry. Available types:', 
                    nodeRegistry.getAll().map(n => n.type));
                // Retry after a delay - but limit retries
                if (!this._startNodeRetries) {
                    this._startNodeRetries = 0;
                }
                if (this._startNodeRetries < 5) {
                    this._startNodeRetries++;
                    console.log(`Retrying start node creation (attempt ${this._startNodeRetries}/5)...`);
                    setTimeout(() => this.createDefaultStartNode(), 500);
                } else {
                    console.error('Failed to create start node after 5 attempts. Creating manually...');
                    // Create a basic start node manually if registration failed
                    this._createManualStartNode(x, y);
                }
                return;
            }
            
            try {
                this.createNode('start', x, y);
                console.log('✓ Default start node created successfully');
                this._startNodeRetries = 0; // Reset on success
            } catch (error) {
                console.error('Error creating start node:', error);
                // Fallback to manual creation
                this._createManualStartNode(x, y);
            }
        }, 100);
    }
    
    /**
     * Create a manual start node if registration failed
     */
    _createManualStartNode(x, y) {
        console.log('Creating manual start node...');
        try {
            // Create a basic node structure manually
            const nodeId = `node_${this.currentNodeId++}`;
            const nodeElement = document.createElement('div');
            nodeElement.className = 'workflow-node';
            nodeElement.id = nodeId;
            nodeElement.style.left = x + 'px';
            nodeElement.style.top = y + 'px';
            nodeElement.innerHTML = `
                <div class="node-header" style="background: #28a745;">
                    <i class="bi bi-play-circle-fill"></i>
                    <span>Start</span>
                    <button class="node-delete" onclick="workflowBuilder.deleteNode('${nodeId}')">×</button>
                </div>
                <div class="node-body">
                    <div class="node-description">Workflow entry point</div>
                </div>
                <div class="workflow-node-ports">
                    <div class="node-ports-output">
                        <div class="node-port" data-port="output" data-type="output">
                            <span class="port-label">Start</span>
                        </div>
                    </div>
                </div>
            `;
            
            this.canvas.appendChild(nodeElement);
            
            // Add to workflow nodes
            this.workflowNodes.push({
                id: nodeId,
                type: 'start',
                position: { x, y },
                data: { message: 'Workflow started' }
            });
            
            // Make draggable
            this._makeNodeDraggable(nodeElement);
            
            // Add jsPlumb endpoint
            if (this.jsPlumbInstance) {
                this.jsPlumbInstance.addEndpoint(nodeElement, {
                    anchor: 'Right',
                    isSource: true,
                    isTarget: false,
                    maxConnections: -1,
                    endpoint: 'Dot',
                    paintStyle: { fill: '#28a745', radius: 6 },
                    hoverPaintStyle: { fill: '#1e7e34', radius: 8 }
                });
            }
            
            console.log('✓ Manual start node created');
        } catch (error) {
            console.error('Failed to create manual start node:', error);
        }
    }

    /**
     * Clear canvas
     */
    clearCanvas() {
        this.workflowNodes.forEach(node => {
                if (this.jsPlumbInstance && this.jsPlumbInstance.removeAllEndpoints) {
                    this.jsPlumbInstance.removeAllEndpoints(node.id);
                }
                if (this.jsPlumbInstance && this.jsPlumbInstance.remove) {
                    this.jsPlumbInstance.remove(node.id);
                }
            document.getElementById(node.id)?.remove();
        });
        this.workflowNodes = [];
        this.workflowEdges = [];
        this.selectedNode = null;
    }
    
    /**
     * Auto-save workflow
     */
    autoSave() {
        if (!this.projectId) {
            console.warn('Cannot auto-save: projectId not set');
            return;
        }
        
        const workflowData = this.getWorkflowData();
        
        // Only save if there are nodes
        if (workflowData.nodes.length === 0) {
            return;
        }
        
        const url = this.currentWorkflowId 
            ? `/api/v1/workflows/${this.currentWorkflowId}`
            : `/api/v1/projects/${this.projectId}/workflows`;
        const method = this.currentWorkflowId ? 'PUT' : 'POST';
        
        const body = {
            name: this.currentWorkflowId ? undefined : 'Auto-saved Workflow',
            workflow_data: workflowData
        };
        
        // Remove undefined fields
        Object.keys(body).forEach(key => body[key] === undefined && delete body[key]);
        
        fetch(url, {
            method: method,
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(body)
        })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                if (!this.currentWorkflowId && data.data) {
                    this.currentWorkflowId = data.data.id;
                }
                console.log('Workflow auto-saved');
            }
        })
        .catch(error => {
            console.error('Error auto-saving workflow:', error);
        });
    }
    
    /**
     * Set project ID for auto-save
     */
    setProjectId(projectId) {
        this.projectId = projectId;
    }
    
    /**
     * Auto-arrange nodes in a hierarchical layout
     */
    autoArrange() {
        if (this.workflowNodes.length === 0) {
            return;
        }

        // Build dependency graph and assign layers
        const nodeLayers = new Map(); // nodeId -> layer number
        const inDegree = new Map(); // nodeId -> number of incoming edges
        const children = new Map(); // nodeId -> array of child node IDs
        
        // Initialize
        this.workflowNodes.forEach(node => {
            nodeLayers.set(node.id, -1);
            inDegree.set(node.id, 0);
            children.set(node.id, []);
        });
        
        // Build graph from edges
        this.workflowEdges.forEach(edge => {
            const sourceId = edge.source;
            const targetId = edge.target;
            
            // Update in-degree
            inDegree.set(targetId, (inDegree.get(targetId) || 0) + 1);
            
            // Add to children
            if (!children.has(sourceId)) {
                children.set(sourceId, []);
            }
            children.get(sourceId).push(targetId);
        });
        
        // Find start nodes (nodes with no incoming edges)
        const startNodes = Array.from(inDegree.entries())
            .filter(([nodeId, degree]) => degree === 0)
            .map(([nodeId]) => nodeId);
        
        // If no start nodes, use all nodes as potential starts
        if (startNodes.length === 0) {
            startNodes.push(...this.workflowNodes.map(n => n.id));
        }
        
        // BFS to assign layers
        const queue = [...startNodes.map(id => ({ id, layer: 0 }))];
        const visited = new Set();
        
        while (queue.length > 0) {
            const { id, layer } = queue.shift();
            if (visited.has(id)) continue;
            
            visited.add(id);
            nodeLayers.set(id, layer);
            
            // Process children
            const nodeChildren = children.get(id) || [];
            nodeChildren.forEach(childId => {
                if (!visited.has(childId)) {
                    queue.push({ id: childId, layer: layer + 1 });
                }
            });
        }
        
        // Assign layer 0 to unvisited nodes
        this.workflowNodes.forEach(node => {
            if (nodeLayers.get(node.id) === -1) {
                nodeLayers.set(node.id, 0);
            }
        });
        
        // Group nodes by layer
        const layers = new Map();
        nodeLayers.forEach((layer, nodeId) => {
            if (!layers.has(layer)) {
                layers.set(layer, []);
            }
            layers.get(layer).push(nodeId);
        });
        
        // Layout parameters
        const NODE_WIDTH = 200;
        const NODE_HEIGHT = 150;
        const HORIZONTAL_SPACING = 250;
        const VERTICAL_SPACING = 200;
        const START_X = 100;
        const START_Y = 100;
        
        // Arrange nodes layer by layer
        const maxLayer = Math.max(...Array.from(layers.keys()));
        layers.forEach((nodeIds, layer) => {
            const x = START_X + (layer * HORIZONTAL_SPACING);
            const layerHeight = nodeIds.length * VERTICAL_SPACING;
            const startY = START_Y + (maxLayer > 0 ? (maxLayer - layer) * 50 : 0);
            
            nodeIds.forEach((nodeId, index) => {
                const y = startY + (index * VERTICAL_SPACING);
                const nodeEl = document.getElementById(nodeId);
                if (nodeEl) {
                    nodeEl.style.left = x + 'px';
                    nodeEl.style.top = y + 'px';
                    
                    // Update node position in memory
                    const node = this.workflowNodes.find(n => n.id === nodeId);
                    if (node) {
                        node.position = { x, y };
                    }
                    
                    // Repaint jsPlumb connections
                    if (this.jsPlumbInstance && this.jsPlumbInstance.repaint) {
                        setTimeout(() => {
                            this.jsPlumbInstance.repaint(nodeId);
                        }, 10);
                    }
                }
            });
        });
        
        // Repaint all connections
        if (this.jsPlumbInstance && this.jsPlumbInstance.repaintEverything) {
            setTimeout(() => {
                this.jsPlumbInstance.repaintEverything();
            }, 100);
        }
        
        // Trigger auto-save
        if (this.autoSaveEnabled) {
            clearTimeout(this.autoSaveTimer);
            this.autoSaveTimer = setTimeout(() => {
                this.autoSave();
            }, 2000);
        }
        
        console.log('Nodes auto-arranged');
    }

    /**
     * Enable/disable auto-save
     */
    setAutoSave(enabled) {
        this.autoSaveEnabled = enabled;
    }
}

// Export
if (typeof module !== 'undefined' && module.exports) {
    module.exports = { WorkflowBuilder };
}

// Note: workflowBuilder instance is created in the template, not here
// to avoid duplicate declaration errors

