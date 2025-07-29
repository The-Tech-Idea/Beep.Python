/**
 * Beep.Python.AI.Transformers Documentation - Navigation Manager
 * Handles dynamic navigation loading and active state management
 */

class TransformerNavigationManager {
    constructor() {
        this.currentPage = this.getCurrentPageName();
        this.navigationMapping = this.createNavigationMapping();
    }

    getCurrentPageName() {
        const path = window.location.pathname;
        const filename = path.split('/').pop() || 'index.html';
        return filename.replace('.html', '');
    }

    createNavigationMapping() {
        return {
            // Home
            'index': { 
                activeId: 'nav-home', 
                openSection: null 
            },
            
            // Getting Started
            'installation': { 
                activeId: 'nav-installation', 
                openSection: 'nav-getting-started' 
            },
            'getting-started': { 
                activeId: 'nav-quick-start', 
                openSection: 'nav-getting-started' 
            },
            'configuration': { 
                activeId: 'nav-configuration', 
                openSection: 'nav-getting-started' 
            },
            
            // Core API
            'ITransformerPipeLine': { 
                activeId: 'nav-core-interface', 
                openSection: 'nav-core-api' 
            },
            'BaseTransformerPipeline': { 
                activeId: 'nav-base-pipeline', 
                openSection: 'nav-core-api' 
            },
            'TransformerPipelineFactory': { 
                activeId: 'nav-pipeline-factory', 
                openSection: 'nav-core-api' 
            },
            
            // Provider Pipelines
            'HuggingFaceTransformerPipeline': { 
                activeId: 'nav-huggingface', 
                openSection: 'nav-providers' 
            },
            'OpenAITransformerPipeline': { 
                activeId: 'nav-openai', 
                openSection: 'nav-providers' 
            },
            'AzureTransformerPipeline': { 
                activeId: 'nav-azure', 
                openSection: 'nav-providers' 
            },
            'GoogleTransformerPipeline': { 
                activeId: 'nav-google', 
                openSection: 'nav-providers' 
            },
            'AnthropicTransformerPipeline': { 
                activeId: 'nav-anthropic', 
                openSection: 'nav-providers' 
            },
            'LocalTransformerPipeline': { 
                activeId: 'nav-local', 
                openSection: 'nav-providers' 
            },
            'CustomTransformerPipeline': { 
                activeId: 'nav-custom', 
                openSection: 'nav-providers' 
            },
            
            // Multimodal AI
            'MultimodalTransformerPipeline': { 
                activeId: 'nav-multimodal-pipeline', 
                openSection: 'nav-multimodal' 
            },
            'MultimodalPipelineFactory': { 
                activeId: 'nav-multimodal-factory', 
                openSection: 'nav-multimodal' 
            },
            'multimodal-examples': { 
                activeId: 'nav-multimodal-examples', 
                openSection: 'nav-multimodal' 
            },
            
            // Examples
            'basic-usage': { 
                activeId: 'nav-basic-examples', 
                openSection: 'nav-examples' 
            },
            'multi-provider': { 
                activeId: 'nav-multi-provider', 
                openSection: 'nav-examples' 
            },
            'enterprise': { 
                activeId: 'nav-enterprise-examples', 
                openSection: 'nav-examples' 
            },
            
            // Configuration
            'connection-config': { 
                activeId: 'nav-connection-config', 
                openSection: 'nav-configuration' 
            },
            'security': { 
                activeId: 'nav-security', 
                openSection: 'nav-configuration' 
            },
            'monitoring': { 
                activeId: 'nav-monitoring', 
                openSection: 'nav-configuration' 
            },
            
            // Reference
            'enums': { 
                activeId: 'nav-enums', 
                openSection: 'nav-reference' 
            },
            'data-models': { 
                activeId: 'nav-data-models', 
                openSection: 'nav-reference' 
            },
            'changelog': { 
                activeId: 'nav-changelog', 
                openSection: 'nav-reference' 
            }
        };
    }

    getNavigationHTML() {
        return `
        <!-- Beep.Python.AI.Transformers - Shared Navigation Component -->
        <div class="logo">
            <div class="logo-text">
                <h2><i class="bi bi-robot"></i> AI Transformers</h2>
                <span class="version">v1.0.0</span>
            </div>
        </div>

        <!-- Search -->
        <div class="search-container">
            <input type="text" class="search-input" placeholder="Search documentation..." onkeyup="searchDocs(this.value)">
        </div>

        <nav>
            <ul class="nav-menu">
                <li><a href="index.html" id="nav-home"><i class="bi bi-house"></i> Home</a></li>
                
                <li class="has-submenu" id="nav-getting-started">
                    <a href="#"><i class="bi bi-rocket"></i> Getting Started</a>
                    <ul class="submenu">
                        <li><a href="installation.html" id="nav-installation">Installation</a></li>
                        <li><a href="getting-started.html" id="nav-quick-start">Quick Start Guide</a></li>
                        <li><a href="configuration.html" id="nav-configuration">Configuration</a></li>
                    </ul>
                </li>
                
                <li class="has-submenu" id="nav-core-api">
                    <a href="#"><i class="bi bi-layers"></i> Core API</a>
                    <ul class="submenu">
                        <li><a href="api/ITransformerPipeLine.html" id="nav-core-interface">ITransformerPipeLine</a></li>
                        <li><a href="api/BaseTransformerPipeline.html" id="nav-base-pipeline">BaseTransformerPipeline</a></li>
                        <li><a href="api/TransformerPipelineFactory.html" id="nav-pipeline-factory">TransformerPipelineFactory</a></li>
                    </ul>
                </li>
                
                <li class="has-submenu" id="nav-providers">
                    <a href="#"><i class="bi bi-cloud"></i> AI Providers</a>
                    <ul class="submenu">
                        <li><a href="api/HuggingFaceTransformerPipeline.html" id="nav-huggingface">HuggingFace</a></li>
                        <li><a href="api/OpenAITransformerPipeline.html" id="nav-openai">OpenAI</a></li>
                        <li><a href="api/AzureTransformerPipeline.html" id="nav-azure">Azure OpenAI</a></li>
                        <li><a href="api/GoogleTransformerPipeline.html" id="nav-google">Google AI</a></li>
                        <li><a href="api/AnthropicTransformerPipeline.html" id="nav-anthropic">Anthropic</a></li>
                        <li><a href="api/LocalTransformerPipeline.html" id="nav-local">Local Models</a></li>
                        <li><a href="api/CustomTransformerPipeline.html" id="nav-custom">Custom Providers</a></li>
                    </ul>
                </li>
                
                <li class="has-submenu" id="nav-multimodal">
                    <a href="#"><i class="bi bi-collection"></i> Multimodal AI</a>
                    <ul class="submenu">
                        <li><a href="api/MultimodalTransformerPipeline.html" id="nav-multimodal-pipeline">Multimodal Pipeline</a></li>
                        <li><a href="api/MultimodalPipelineFactory.html" id="nav-multimodal-factory">Multimodal Factory</a></li>
                        <li><a href="examples/multimodal-examples.html" id="nav-multimodal-examples">Multimodal Examples</a></li>
                    </ul>
                </li>
                
                <li class="has-submenu" id="nav-examples">
                    <a href="#"><i class="bi bi-code-square"></i> Examples</a>
                    <ul class="submenu">
                        <li><a href="examples/basic-usage.html" id="nav-basic-examples">Basic Usage</a></li>
                        <li><a href="examples/multi-provider.html" id="nav-multi-provider">Multi-Provider Setup</a></li>
                        <li><a href="examples/enterprise.html" id="nav-enterprise-examples">Enterprise Deployment</a></li>
                    </ul>
                </li>
                
                <li class="has-submenu" id="nav-configuration">
                    <a href="#"><i class="bi bi-gear"></i> Configuration</a>
                    <ul class="submenu">
                        <li><a href="configuration/connection-config.html" id="nav-connection-config">Connection Setup</a></li>
                        <li><a href="configuration/security.html" id="nav-security">Security & Compliance</a></li>
                        <li><a href="configuration/monitoring.html" id="nav-monitoring">Monitoring & Analytics</a></li>
                    </ul>
                </li>
                
                <li class="has-submenu" id="nav-reference">
                    <a href="#"><i class="bi bi-book"></i> Reference</a>
                    <ul class="submenu">
                        <li><a href="reference/enums.html" id="nav-enums">Enumerations</a></li>
                        <li><a href="reference/data-models.html" id="nav-data-models">Data Models</a></li>
                        <li><a href="reference/changelog.html" id="nav-changelog">Changelog</a></li>
                    </ul>
                </li>
            </ul>
        </nav>
        `;
    }

    async loadNavigation() {
        try {
            // Get navigation HTML from embedded function (no fetch needed - avoids CORS)
            const navigationHtml = this.getNavigationHTML();
            
            // Try both possible sidebar selectors for maximum compatibility
            let sidebar = document.querySelector('#sidebar');
            if (!sidebar) {
                sidebar = document.querySelector('.sidebar');
            }
            if (!sidebar) {
                sidebar = document.querySelector('.documentation-sidebar');
            }
            
            if (sidebar) {
                // Insert navigation into sidebar
                sidebar.innerHTML = navigationHtml;
                
                // Set up navigation after loading
                this.setupNavigation();
                console.log('? Transformer navigation loaded successfully');
            } else {
                console.error('? Sidebar element not found with any selector');
            }
        } catch (error) {
            console.error('? Error loading navigation:', error);
            // Fallback: Show error message in sidebar
            let sidebar = document.querySelector('#sidebar') || 
                         document.querySelector('.sidebar') || 
                         document.querySelector('.documentation-sidebar');
            if (sidebar) {
                sidebar.innerHTML = `
                    <div class="navigation-error p-4">
                        <h5>Navigation Error</h5>
                        <p>Failed to load navigation. Please refresh the page.</p>
                        <p class="text-muted small">Error: ${error.message}</p>
                    </div>
                `;
            }
        }
    }

    setupNavigation() {
        // Set active states based on current page
        this.setActiveStates();
        
        // Set up submenu toggles
        this.setupSubmenuToggles();
        
        // Set up search functionality
        this.setupSearch();
        
        // Set up theme toggle (if not already set up)
        this.setupThemeToggle();
    }

    setActiveStates() {
        const mapping = this.navigationMapping[this.currentPage];
        
        if (mapping) {
            // Set active link
            if (mapping.activeId) {
                const activeElement = document.getElementById(mapping.activeId);
                if (activeElement) {
                    activeElement.classList.add('active');
                }
            }
            
            // Open parent section
            if (mapping.openSection) {
                const sectionElement = document.getElementById(mapping.openSection);
                if (sectionElement) {
                    sectionElement.classList.add('open');
                }
            }
        }
        
        console.log(`?? Set active state for page: ${this.currentPage}`);
    }

    setupSubmenuToggles() {
        const submenus = document.querySelectorAll('.has-submenu > a');
        
        submenus.forEach(item => {
            item.addEventListener('click', function(e) {
                e.preventDefault();
                const parent = this.parentElement;
                parent.classList.toggle('open');
            });
        });
    }

    setupSearch() {
        // Enhanced search functionality
        const searchInput = document.querySelector('.search-input');
        if (searchInput) {
            searchInput.addEventListener('input', function(e) {
                if (typeof searchDocs === 'function') {
                    searchDocs(e.target.value);
                }
            });
            
            // Add keyboard shortcuts
            searchInput.addEventListener('keydown', function(e) {
                if (e.key === 'Escape') {
                    this.value = '';
                    searchDocs('');
                    this.blur();
                }
            });
        }
    }

    setupThemeToggle() {
        // Load saved theme
        const savedTheme = localStorage.getItem('transformer-docs-theme') || 'light';
        document.documentElement.setAttribute('data-theme', savedTheme);
        
        const themeIcon = document.getElementById('theme-icon');
        if (themeIcon) {
            themeIcon.className = savedTheme === 'dark' ? 'bi bi-moon-fill' : 'bi bi-sun-fill';
        }
    }
}

// Global navigation manager instance
let transformerNavigationManager;

// Initialize navigation when DOM is loaded
document.addEventListener('DOMContentLoaded', async function() {
    console.log('?? Initializing Beep.Python.AI.Transformers documentation navigation...');
    
    try {
        // Create navigation manager
        transformerNavigationManager = new TransformerNavigationManager();
        
        // Load navigation
        await transformerNavigationManager.loadNavigation();
        
        // Initialize other features
        initializeCodeHighlighting();
        initializeSearchFeatures();
        initializeTooltips();
        initializeMobileMenu();
        
        console.log('? Transformer documentation navigation initialized successfully');
    } catch (error) {
        console.error('? Failed to initialize navigation:', error);
    }
});

// Global theme toggle function (called by theme toggle button)
function toggleTheme() {
    const currentTheme = document.documentElement.getAttribute('data-theme');
    const newTheme = currentTheme === 'dark' ? 'light' : 'dark';
    
    document.documentElement.setAttribute('data-theme', newTheme);
    localStorage.setItem('transformer-docs-theme', newTheme);
    
    // Update theme icon
    const themeIcon = document.getElementById('theme-icon');
    if (themeIcon) {
        themeIcon.className = newTheme === 'dark' ? 'bi bi-moon-fill' : 'bi bi-sun-fill';
    }
}

// Global sidebar toggle function (called by mobile menu button)
function toggleSidebar() {
    // Try multiple sidebar selectors for compatibility
    let sidebar = document.querySelector('#sidebar') || 
                 document.querySelector('.sidebar') || 
                 document.querySelector('.documentation-sidebar');
    
    if (sidebar) {
        sidebar.classList.toggle('open');
        sidebar.classList.toggle('mobile-open');
    }
}

// Enhanced search function
function searchDocs(query = '') {
    const links = document.querySelectorAll('.nav-menu a');
    const lowerQuery = query.toLowerCase();
    
    links.forEach(link => {
        const text = link.textContent.toLowerCase();
        const listItem = link.closest('li');
        
        if (text.includes(lowerQuery) || lowerQuery === '') {
            if (listItem) {
                listItem.style.display = '';
                // Also show parent sections
                const parentSubmenu = listItem.closest('.has-submenu');
                if (parentSubmenu) {
                    parentSubmenu.style.display = '';
                    if (lowerQuery !== '') {
                        parentSubmenu.classList.add('open');
                    }
                }
            }
        } else {
            if (listItem) listItem.style.display = 'none';
        }
    });
}

// Enhanced code highlighting functionality
function initializeCodeHighlighting() {
    document.querySelectorAll('pre code').forEach((block) => {
        addCopyButton(block);
        addLanguageLabel(block);
        
        if (block.textContent.split('\n').length > 5) {
            addLineNumbers(block);
        }
    });
}

function addCopyButton(codeBlock) {
    const pre = codeBlock.parentElement;
    const button = document.createElement('button');
    button.className = 'copy-button btn btn-sm btn-outline-secondary position-absolute';
    button.innerHTML = '<i class="bi bi-clipboard"></i>';
    button.title = 'Copy code';
    button.style.cssText = 'top: 8px; right: 8px; z-index: 10;';
    
    button.addEventListener('click', async () => {
        try {
            await navigator.clipboard.writeText(codeBlock.textContent);
            button.innerHTML = '<i class="bi bi-check"></i>';
            button.classList.add('btn-success');
            button.classList.remove('btn-outline-secondary');
            
            setTimeout(() => {
                button.innerHTML = '<i class="bi bi-clipboard"></i>';
                button.classList.remove('btn-success');
                button.classList.add('btn-outline-secondary');
            }, 2000);
        } catch (err) {
            console.error('Failed to copy code:', err);
        }
    });
    
    pre.style.position = 'relative';
    pre.appendChild(button);
}

function addLanguageLabel(codeBlock) {
    const language = codeBlock.className.match(/language-(\w+)/);
    if (language) {
        const pre = codeBlock.parentElement;
        const label = document.createElement('span');
        label.className = 'language-label badge bg-secondary position-absolute';
        label.textContent = language[1].toUpperCase();
        label.style.cssText = 'top: 8px; left: 8px; font-size: 0.7rem;';
        pre.appendChild(label);
    }
}

function addLineNumbers(codeBlock) {
    const lines = codeBlock.textContent.split('\n');
    const lineNumbers = document.createElement('div');
    lineNumbers.className = 'line-numbers';
    lineNumbers.style.cssText = `
        position: absolute;
        left: 0;
        top: 0;
        bottom: 0;
        width: 2.5rem;
        background: rgba(0,0,0,0.05);
        border-right: 1px solid rgba(0,0,0,0.1);
        padding: 1rem 0.5rem;
        font-family: monospace;
        font-size: 0.8rem;
        line-height: 1.45;
        color: #666;
        user-select: none;
    `;
    
    lines.forEach((_, index) => {
        if (index < lines.length - 1) { // Skip last empty line
            const lineNumber = document.createElement('div');
            lineNumber.textContent = index + 1;
            lineNumbers.appendChild(lineNumber);
        }
    });
    
    const pre = codeBlock.parentElement;
    pre.style.position = 'relative';
    pre.insertBefore(lineNumbers, codeBlock);
    codeBlock.style.paddingLeft = '3rem';
}

// Search features initialization
function initializeSearchFeatures() {
    // Keyboard shortcuts
    document.addEventListener('keydown', function(e) {
        // Ctrl/Cmd + K for search
        if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
            e.preventDefault();
            focusSearch();
        }
    });
}

function focusSearch() {
    const searchInput = document.querySelector('.search-input');
    if (searchInput) {
        searchInput.focus();
        searchInput.select();
    }
}

// Tooltips initialization
function initializeTooltips() {
    // Initialize Bootstrap tooltips if available
    if (typeof bootstrap !== 'undefined' && bootstrap.Tooltip) {
        const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
        tooltipTriggerList.map(function (tooltipTriggerEl) {
            return new bootstrap.Tooltip(tooltipTriggerEl);
        });
    }
}

// Mobile menu initialization
function initializeMobileMenu() {
    // Mobile menu functionality is handled by the main navigation structure
    // Additional mobile-specific features can be added here
    
    // Close mobile menu when clicking outside
    document.addEventListener('click', (e) => {
        const sidebar = document.querySelector('#sidebar') || 
                       document.querySelector('.sidebar') || 
                       document.querySelector('.documentation-sidebar');
        const isMobile = window.innerWidth < 768;
        
        if (isMobile && sidebar && !sidebar.contains(e.target)) {
            sidebar.classList.remove('open');
            sidebar.classList.remove('mobile-open');
        }
    });
}

// Export for use in modules (if needed)
if (typeof module !== 'undefined' && module.exports) {
    module.exports = { 
        TransformerNavigationManager, 
        toggleTheme, 
        toggleSidebar, 
        searchDocs, 
        initializeCodeHighlighting,
        initializeSearchFeatures,
        initializeTooltips,
        initializeMobileMenu
    };
}