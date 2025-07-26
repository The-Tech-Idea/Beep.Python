/**
 * Beep.Python.Runtime.PythonNet Documentation - Navigation Manager
 * Handles dynamic navigation loading and active state management
 */

class NavigationManager {
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
            'examples': { 
                activeId: 'nav-examples', 
                openSection: 'nav-getting-started' 
            },
            
            // Core Classes
            'PythonNetRunTimeManager': { 
                activeId: 'nav-runtime-manager', 
                openSection: 'nav-core-classes' 
            },
            'PythonSessionManager': { 
                activeId: 'nav-session-manager', 
                openSection: 'nav-core-classes' 
            },
            'PythonCodeExecuteManager': { 
                activeId: 'nav-execute-manager', 
                openSection: 'nav-core-classes' 
            },
            
            // Manager Classes
            'PythonVirtualEnvManager': { 
                activeId: 'nav-virtualenv-manager', 
                openSection: 'nav-manager-classes' 
            },
            'PythonPackageManager': { 
                activeId: 'nav-package-manager', 
                openSection: 'nav-manager-classes' 
            },
            'PythonMLManager': { 
                activeId: 'nav-ml-manager', 
                openSection: 'nav-manager-classes' 
            },
            'PythonPandasManager': { 
                activeId: 'nav-pandas-manager', 
                openSection: 'nav-manager-classes' 
            },
            'PythonPlotManager': { 
                activeId: 'nav-plot-manager', 
                openSection: 'nav-manager-classes' 
            },
            
            // Helper Classes
            'helper-classes': { 
                activeId: 'nav-helper-classes', 
                openSection: 'nav-helper-utilities' 
            },
            
            // Advanced Topics
            'virtual-environments': { 
                activeId: 'nav-virtual-env', 
                openSection: 'nav-advanced-topics' 
            },
            'multi-user-sessions': { 
                activeId: 'nav-multi-user', 
                openSection: 'nav-advanced-topics' 
            },
            'package-management': { 
                activeId: 'nav-packages', 
                openSection: 'nav-advanced-topics' 
            },
            'api-reference': { 
                activeId: 'nav-api', 
                openSection: null 
            }
        };
    }

    getNavigationHTML() {
        return `
        <!-- Beep.Python.Runtime.PythonNet - Shared Navigation Component -->
        <div class="logo">
            <div class="logo-text">
                <h2>Beep Python Runtime</h2>
                <span class="version">v1.0.43</span>
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
                        <li><a href="getting-started.html" id="nav-quick-start">Quick Start Tutorial</a></li>
                        <li><a href="examples.html" id="nav-examples">Examples</a></li>
                    </ul>
                </li>
                <li class="has-submenu" id="nav-core-classes">
                    <a href="#"><i class="bi bi-layers"></i> Core Classes</a>
                    <ul class="submenu">
                        <li><a href="PythonNetRunTimeManager.html" id="nav-runtime-manager">PythonNetRunTimeManager</a></li>
                        <li><a href="PythonSessionManager.html" id="nav-session-manager">PythonSessionManager</a></li>
                        <li><a href="PythonCodeExecuteManager.html" id="nav-execute-manager">PythonCodeExecuteManager</a></li>
                    </ul>
                </li>
                <li class="has-submenu" id="nav-manager-classes">
                    <a href="#"><i class="bi bi-gear"></i> Manager Classes</a>
                    <ul class="submenu">
                        <li><a href="PythonVirtualEnvManager.html" id="nav-virtualenv-manager">PythonVirtualEnvManager</a></li>
                        <li><a href="PythonPackageManager.html" id="nav-package-manager">PythonPackageManager</a></li>
                        <li><a href="PythonMLManager.html" id="nav-ml-manager">PythonMLManager</a></li>
                        <li><a href="PythonPandasManager.html" id="nav-pandas-manager">PythonPandasManager</a></li>
                        <li><a href="PythonPlotManager.html" id="nav-plot-manager">PythonPlotManager</a></li>
                    </ul>
                </li>
                <li class="has-submenu" id="nav-helper-utilities">
                    <a href="#"><i class="bi bi-tools"></i> Helper Classes & Utilities</a>
                    <ul class="submenu">
                        <li><a href="helper-classes.html" id="nav-helper-classes">Diagnostics & Utilities</a></li>
                    </ul>
                </li>
                <li class="has-submenu" id="nav-advanced-topics">
                    <a href="#"><i class="bi bi-cpu"></i> Advanced Topics</a>
                    <ul class="submenu">
                        <li><a href="virtual-environments.html" id="nav-virtual-env">Virtual Environments</a></li>
                        <li><a href="multi-user-sessions.html" id="nav-multi-user">Multi-User Sessions</a></li>
                        <li><a href="package-management.html" id="nav-packages">Package Management</a></li>
                    </ul>
                </li>
                <li><a href="api-reference.html" id="nav-api"><i class="bi bi-code-square"></i> API Reference</a></li>
            </ul>
        </nav>
        `;
    }

    async loadNavigation() {
        try {
            // Get navigation HTML from embedded function (no fetch needed - avoids CORS)
            const navigationHtml = this.getNavigationHTML();
            
            // Insert navigation into sidebar
            const sidebar = document.getElementById('sidebar');
            if (sidebar) {
                sidebar.innerHTML = navigationHtml;
                
                // Set up navigation after loading
                this.setupNavigation();
                console.log('?? Navigation loaded successfully');
            } else {
                console.error('?? Sidebar element not found');
            }
        } catch (error) {
            console.error('?? Error loading navigation:', error);
            // Fallback: Show error message in sidebar
            const sidebar = document.getElementById('sidebar');
            if (sidebar) {
                sidebar.innerHTML = `
                    <div class="navigation-error">
                        <h3>Navigation Error</h3>
                        <p>Failed to load navigation. Please refresh the page.</p>
                        <p>Error: ${error.message}</p>
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
        // Search functionality is handled by the global searchDocs function
        const searchInput = document.querySelector('.search-input');
        if (searchInput) {
            searchInput.addEventListener('input', function(e) {
                if (typeof searchDocs === 'function') {
                    searchDocs(e.target.value);
                }
            });
        }
    }

    setupThemeToggle() {
        // Load saved theme
        const savedTheme = localStorage.getItem('theme') || 'light';
        document.documentElement.setAttribute('data-theme', savedTheme);
        
        const themeIcon = document.getElementById('theme-icon');
        if (themeIcon) {
            themeIcon.className = savedTheme === 'dark' ? 'bi bi-moon-fill' : 'bi bi-sun-fill';
        }
    }
}

// Global navigation manager instance
let navigationManager;

// Initialize navigation when DOM is loaded
document.addEventListener('DOMContentLoaded', async function() {
    console.log('?? Initializing Beep Python Runtime documentation navigation...');
    
    try {
        // Create navigation manager
        navigationManager = new NavigationManager();
        
        // Load navigation
        await navigationManager.loadNavigation();
        
        console.log('? Beep Python Runtime documentation navigation initialized successfully');
    } catch (error) {
        console.error('? Failed to initialize navigation:', error);
    }
});

// Global theme toggle function (called by theme toggle button)
function toggleTheme() {
    const currentTheme = document.documentElement.getAttribute('data-theme');
    const newTheme = currentTheme === 'dark' ? 'light' : 'dark';
    
    document.documentElement.setAttribute('data-theme', newTheme);
    localStorage.setItem('theme', newTheme);
    
    // Update theme icon
    const themeIcon = document.getElementById('theme-icon');
    if (themeIcon) {
        themeIcon.className = newTheme === 'dark' ? 'bi bi-moon-fill' : 'bi bi-sun-fill';
    }
}

// Global sidebar toggle function (called by mobile menu button)
function toggleSidebar() {
    const sidebar = document.getElementById('sidebar');
    if (sidebar) {
        sidebar.classList.toggle('open');
    }
}

// Global search function (called by search input)
function searchDocs(query = '') {
    const links = document.querySelectorAll('.nav-menu a');
    const lowerQuery = query.toLowerCase();
    
    links.forEach(link => {
        const text = link.textContent.toLowerCase();
        const listItem = link.closest('li');
        
        if (text.includes(lowerQuery) || lowerQuery === '') {
            if (listItem) listItem.style.display = '';
        } else {
            if (listItem) listItem.style.display = 'none';
        }
    });
}

// Global code copy functionality
function setupCodeCopy() {
    const codeBlocks = document.querySelectorAll('pre code');
    
    codeBlocks.forEach(block => {
        const button = document.createElement('button');
        button.textContent = 'Copy';
        button.style.cssText = `
            position: absolute;
            top: 8px;
            right: 8px;
            background: var(--color-brand-primary);
            color: white;
            border: none;
            border-radius: 4px;
            padding: 4px 8px;
            font-size: 12px;
            cursor: pointer;
            opacity: 0;
            transition: opacity 0.3s ease;
        `;
        
        const pre = block.parentElement;
        pre.style.position = 'relative';
        pre.appendChild(button);
        
        pre.addEventListener('mouseenter', () => {
            button.style.opacity = '1';
        });
        
        pre.addEventListener('mouseleave', () => {
            button.style.opacity = '0';
        });
        
        button.addEventListener('click', () => {
            navigator.clipboard.writeText(block.textContent).then(() => {
                button.textContent = 'Copied!';
                setTimeout(() => {
                    button.textContent = 'Copy';
                }, 2000);
            });
        });
    });
}

// Initialize code copy functionality when navigation loads
document.addEventListener('DOMContentLoaded', function() {
    // Add a small delay to ensure content is loaded
    setTimeout(setupCodeCopy, 100);
});

// Export for use in modules (if needed)
if (typeof module !== 'undefined' && module.exports) {
    module.exports = { NavigationManager, toggleTheme, toggleSidebar, searchDocs, setupCodeCopy };
}