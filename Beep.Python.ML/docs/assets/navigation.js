// Enhanced Navigation Manager for Beep Python ML Documentation
class PythonMLDocumentationNavigationManager {
    constructor() {
        this.currentPage = this.getCurrentPageName();
        this.searchIndex = [];
        this.isInitialized = false;
        
        console.log('?? Initializing Python ML Documentation Navigation...');
    }

    async loadNavigation() {
        if (this.isInitialized) return;
        
        try {
            await this.createNavigationStructure();
            this.setupEventListeners();
            this.setupThemeToggle();
            this.initializeSearch();
            this.setActiveNavigation();
            
            this.isInitialized = true;
            console.log('? Python ML Documentation Navigation loaded successfully');
        } catch (error) {
            console.error('? Failed to load navigation:', error);
            this.createFallbackNavigation();
        }
    }

    getCurrentPageName() {
        const path = window.location.pathname;
        const fileName = path.split('/').pop();
        return fileName.replace('.html', '') || 'index';
    }

    async createNavigationStructure() {
        const sidebar = document.getElementById('sidebar');
        if (!sidebar) {
            console.warn('?? Sidebar element not found');
            return;
        }

        const navigationHTML = this.generateNavigationHTML();
        sidebar.innerHTML = navigationHTML;
        
        // Build search index
        this.buildSearchIndex();
    }

    generateNavigationHTML() {
        return `
            <div class="logo">
                <div class="logo-text">
                    <h2>Python ML Integration</h2>
                    <span class="version">v1.0.0</span>
                </div>
            </div>
            
            <div class="search-container">
                <input type="text" class="search-input" placeholder="Search documentation..." 
                       onkeyup="searchDocs(this.value)" id="searchInput">
            </div>
            
            <nav>
                <ul class="nav-menu" id="navMenu">
                    ${this.generateMenuItems()}
                </ul>
            </nav>
        `;
    }

    generateMenuItems() {
        return `
            <!-- Home -->
            <li><a href="index.html" class="${this.currentPage === 'index' ? 'active' : ''}">
                <i class="bi bi-house"></i> Home
            </a></li>
            
            <!-- Getting Started -->
            <li class="has-submenu">
                <a href="#"><i class="bi bi-rocket"></i> Getting Started</a>
                <ul class="submenu">
                    <li><a href="installation.html">?? Installation</a></li>
                    <li><a href="getting-started.html">?? Quick Start</a></li>
                    <li><a href="examples.html">?? Basic Examples</a></li>
                    <li><a href="advanced-examples.html">?? Advanced Examples</a></li>
                </ul>
            </li>
            
            <!-- Core Components -->
            <li class="has-submenu">
                <a href="#"><i class="bi bi-cpu"></i> Core Components</a>
                <ul class="submenu">
                    <li><a href="PythonMLManager.html">?? PythonMLManager</a></li>
                    <li><a href="algorithm-management.html">?? Algorithm Management</a></li>
                    <li><a href="model-evaluation.html">?? Model Evaluation</a></li>
                    <li><a href="training-workflows.html">?? Training Workflows</a></li>
                </ul>
            </li>
            
            <!-- MVVM ViewModels -->
            <li class="has-submenu">
                <a href="#"><i class="bi bi-laptop"></i> MVVM ViewModels</a>
                <ul class="submenu">
                    <li><a href="PythonMachineLearningViewModel.html">?? ML ViewModel</a></li>
                    <li><a href="PythonTrainingViewModel.html">?? Training ViewModel</a></li>
                    <li><a href="ModelEvaluationGraphsViewModel.html">?? Evaluation Graphs ViewModel</a></li>
                    <li><a href="PythonAlgorithimsViewModel.html">?? Algorithms ViewModel</a></li>
                </ul>
            </li>
            
            <!-- Advanced ML -->
            <li class="has-submenu">
                <a href="#"><i class="bi bi-graph-up"></i> Advanced ML</a>
                <ul class="submenu">
                    <li><a href="automl-integration.html">?? AutoML Integration</a></li>
                    <li><a href="ensemble-methods.html">??? Ensemble Methods</a></li>
                    <li><a href="model-interpretability.html">??? Model Interpretability</a></li>
                    <li><a href="hyperparameter-tuning.html">??? Hyperparameter Tuning</a></li>
                </ul>
            </li>
            
            <!-- Assistant Classes -->
            <li class="has-submenu">
                <a href="#"><i class="bi bi-tools"></i> Assistant Classes</a>
                <ul class="submenu">
                    <li><a href="assistant-classes.html">??? All Assistants Overview</a></li>
                    <li><a href="python-data-preprocessing.html">?? Data Preprocessing</a></li>
                    <li><a href="python-feature-engineering.html">?? Feature Engineering</a></li>
                    <li><a href="python-visualization.html">?? Visualization</a></li>
                    <li><a href="python-time-series.html">?? Time Series</a></li>
                    <li><a href="python-text-processing.html">?? Text Processing</a></li>
                </ul>
            </li>
            
            <!-- Python Scripts -->
            <li class="has-submenu">
                <a href="#"><i class="bi bi-file-code"></i> Python Scripts</a>
                <ul class="submenu">
                    <li><a href="python-scripts.html">?? Template Scripts (101+)</a></li>
                    <li><a href="script-categories.html">?? Script Categories</a></li>
                    <li><a href="custom-scripts.html">??? Custom Scripts</a></li>
                </ul>
            </li>
            
            <!-- API Reference -->
            <li><a href="api-reference.html"><i class="bi bi-code-square"></i> API Reference</a></li>
            
            <!-- Troubleshooting -->
            <li><a href="troubleshooting.html"><i class="bi bi-question-circle"></i> Troubleshooting</a></li>
        `;
    }

    setupEventListeners() {
        // Setup submenu toggles
        const submenus = document.querySelectorAll('.has-submenu > a');
        submenus.forEach(item => {
            item.addEventListener('click', (e) => {
                e.preventDefault();
                const parent = item.parentElement;
                const isOpen = parent.classList.contains('open');
                
                // Close all submenus
                document.querySelectorAll('.has-submenu.open').forEach(openMenu => {
                    if (openMenu !== parent) {
                        openMenu.classList.remove('open');
                    }
                });
                
                // Toggle current submenu
                parent.classList.toggle('open', !isOpen);
            });
        });

        // Setup keyboard navigation
        document.addEventListener('keydown', (e) => {
            if (e.ctrlKey && e.key === 'k') {
                e.preventDefault();
                const searchInput = document.getElementById('searchInput');
                if (searchInput) {
                    searchInput.focus();
                }
            }
        });
    }

    setupThemeToggle() {
        // Load saved theme
        const savedTheme = localStorage.getItem('beep-python-ml-docs-theme') || 'light';
        document.documentElement.setAttribute('data-theme', savedTheme);
        
        const themeIcon = document.getElementById('theme-icon');
        if (themeIcon) {
            themeIcon.className = savedTheme === 'dark' ? 'bi bi-moon-fill' : 'bi bi-sun-fill';
        }
    }

    initializeSearch() {
        // Enhanced search functionality
        const searchInput = document.getElementById('searchInput');
        if (searchInput) {
            searchInput.addEventListener('input', (e) => {
                this.performSearch(e.target.value);
            });
            
            // Add keyboard shortcuts
            searchInput.addEventListener('keydown', (e) => {
                if (e.key === 'Escape') {
                    e.target.value = '';
                    this.performSearch('');
                    e.target.blur();
                }
            });
        }
    }

    buildSearchIndex() {
        // Build searchable index from navigation and page content
        const navLinks = document.querySelectorAll('.nav-menu a');
        this.searchIndex = Array.from(navLinks).map(link => ({
            title: link.textContent.trim(),
            url: link.href,
            category: this.getCategoryFromLink(link)
        }));
        
        // Add page headings to search index
        const headings = document.querySelectorAll('h2, h3, h4');
        headings.forEach(heading => {
            this.searchIndex.push({
                title: heading.textContent.trim(),
                url: window.location.href + '#' + (heading.id || ''),
                category: 'Content'
            });
        });
    }

    getCategoryFromLink(link) {
        const parentSubmenu = link.closest('.has-submenu');
        if (parentSubmenu) {
            const parentTitle = parentSubmenu.querySelector('> a').textContent.trim();
            return parentTitle;
        }
        return 'Navigation';
    }

    performSearch(query) {
        if (!query || query.length < 2) {
            this.clearSearchResults();
            return;
        }

        const results = this.searchIndex.filter(item => 
            item.title.toLowerCase().includes(query.toLowerCase())
        );

        this.displaySearchResults(results, query);
    }

    displaySearchResults(results, query) {
        // Highlight matching navigation items
        const navLinks = document.querySelectorAll('.nav-menu a');
        navLinks.forEach(link => {
            const listItem = link.closest('li');
            const text = link.textContent.toLowerCase();
            
            if (text.includes(query.toLowerCase())) {
                listItem.style.display = '';
                
                // Expand parent submenu if needed
                const parentSubmenu = listItem.closest('.has-submenu');
                if (parentSubmenu && !parentSubmenu.classList.contains('open')) {
                    parentSubmenu.classList.add('open');
                }
            } else {
                listItem.style.display = 'none';
            }
        });

        console.log(`?? Search results for "${query}":`, results);
    }

    clearSearchResults() {
        // Show all navigation items
        const navItems = document.querySelectorAll('.nav-menu li');
        navItems.forEach(item => {
            item.style.display = '';
        });
        
        // Close expanded submenus
        const openSubmenus = document.querySelectorAll('.has-submenu.open');
        openSubmenus.forEach(submenu => {
            submenu.classList.remove('open');
        });
    }

    setActiveNavigation() {
        // Set active navigation based on current page
        const currentPath = window.location.pathname;
        const navLinks = document.querySelectorAll('.nav-menu a');
        
        navLinks.forEach(link => {
            try {
                const linkPath = new URL(link.href).pathname;
                if (linkPath === currentPath || link.href.includes(this.currentPage + '.html')) {
                    link.classList.add('active');
                    
                    // Expand parent submenu
                    const parentSubmenu = link.closest('.has-submenu');
                    if (parentSubmenu) {
                        parentSubmenu.classList.add('open');
                    }
                }
            } catch (e) {
                // Handle relative URLs
                if (link.getAttribute('href') === this.currentPage + '.html') {
                    link.classList.add('active');
                    
                    const parentSubmenu = link.closest('.has-submenu');
                    if (parentSubmenu) {
                        parentSubmenu.classList.add('open');
                    }
                }
            }
        });
    }

    createFallbackNavigation() {
        console.log('?? Creating fallback navigation...');
        
        const sidebar = document.getElementById('sidebar');
        if (!sidebar) return;
        
        sidebar.innerHTML = `
            <div class="logo">
                <div class="logo-text">
                    <h2>Python ML Documentation</h2>
                    <span class="version">v1.0.0</span>
                </div>
            </div>
            
            <nav>
                <ul class="nav-menu">
                    <li><a href="index.html" class="active"><i class="bi bi-house"></i> Home</a></li>
                    <li><a href="getting-started.html"><i class="bi bi-rocket"></i> Getting Started</a></li>
                    <li><a href="PythonMLManager.html"><i class="bi bi-cpu"></i> ML Manager</a></li>
                    <li><a href="PythonMachineLearningViewModel.html"><i class="bi bi-laptop"></i> ML ViewModel</a></li>
                    <li><a href="ModelEvaluationGraphsViewModel.html"><i class="bi bi-graph-up"></i> Evaluation Graphs</a></li>
                    <li><a href="PythonAlgorithimsViewModel.html"><i class="bi bi-cpu"></i> Algorithms ViewModel</a></li>
                    <li><a href="automl-integration.html"><i class="bi bi-robot"></i> AutoML</a></li>
                    <li><a href="model-interpretability.html"><i class="bi bi-eye"></i> Model Interpretability</a></li>
                    <li><a href="api-reference.html"><i class="bi bi-code-square"></i> API Reference</a></li>
                    <li><a href="troubleshooting.html"><i class="bi bi-question-circle"></i> Troubleshooting</a></li>
                </ul>
            </nav>
        `;
    }
}

// Global navigation manager instance
let pythonMLNavigationManager;

// Initialize navigation when DOM is loaded
document.addEventListener('DOMContentLoaded', async function() {
    console.log('?? Initializing Beep Python ML documentation navigation...');
    
    try {
        // Create navigation manager
        pythonMLNavigationManager = new PythonMLDocumentationNavigationManager();
        
        // Load navigation
        await pythonMLNavigationManager.loadNavigation();
        
        // Initialize other features
        initializeCodeHighlighting();
        initializeTooltips();
        initializeMobileMenu();
        
        console.log('? Python ML documentation navigation initialized successfully');
    } catch (error) {
        console.error('? Failed to initialize navigation:', error);
    }
});

// Global theme toggle function
function toggleTheme() {
    const currentTheme = document.documentElement.getAttribute('data-theme');
    const newTheme = currentTheme === 'dark' ? 'light' : 'dark';
    
    document.documentElement.setAttribute('data-theme', newTheme);
    localStorage.setItem('beep-python-ml-docs-theme', newTheme);
    
    // Update theme icon
    const themeIcon = document.getElementById('theme-icon');
    if (themeIcon) {
        themeIcon.className = newTheme === 'dark' ? 'bi bi-moon-fill' : 'bi bi-sun-fill';
    }
    
    console.log(`?? Theme changed to: ${newTheme}`);
}

// Global sidebar toggle function
function toggleSidebar() {
    const sidebar = document.querySelector('#sidebar') || 
                   document.querySelector('.sidebar');
    
    if (sidebar) {
        sidebar.classList.toggle('mobile-open');
        console.log('?? Mobile sidebar toggled');
    }
}

// Enhanced search function
function searchDocs(query = '') {
    if (pythonMLNavigationManager && pythonMLNavigationManager.isInitialized) {
        pythonMLNavigationManager.performSearch(query);
    } else {
        // Fallback search
        basicSearch(query);
    }
}

function basicSearch(query) {
    const links = document.querySelectorAll('.nav-menu a');
    const lowerQuery = query.toLowerCase();
    
    links.forEach(link => {
        const text = link.textContent.toLowerCase();
        const listItem = link.closest('li');
        
        if (text.includes(lowerQuery) || lowerQuery === '') {
            if (listItem) {
                listItem.style.display = '';
                
                // Show parent sections
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
    if (typeof Prism !== 'undefined') {
        // Add copy buttons to code blocks
        document.querySelectorAll('pre code').forEach((block) => {
            addCopyButton(block);
            addLanguageLabel(block);
        });
        
        console.log('?? Code highlighting initialized');
    }
}

function addCopyButton(codeBlock) {
    const pre = codeBlock.parentElement;
    if (pre.querySelector('.copy-button')) return; // Already has copy button
    
    const copyButton = document.createElement('button');
    copyButton.className = 'copy-button';
    copyButton.innerHTML = '<i class="bi bi-clipboard"></i>';
    copyButton.title = 'Copy code';
    
    copyButton.addEventListener('click', async () => {
        try {
            await navigator.clipboard.writeText(codeBlock.textContent);
            copyButton.innerHTML = '<i class="bi bi-check"></i>';
            copyButton.style.color = '#28a745';
            
            setTimeout(() => {
                copyButton.innerHTML = '<i class="bi bi-clipboard"></i>';
                copyButton.style.color = '';
            }, 2000);
        } catch (err) {
            console.error('Failed to copy code:', err);
        }
    });
    
    pre.style.position = 'relative';
    pre.appendChild(copyButton);
}

function addLanguageLabel(codeBlock) {
    const className = codeBlock.className;
    const language = className.match(/language-(\w+)/);
    
    if (language) {
        const pre = codeBlock.parentElement;
        if (pre.querySelector('.language-label')) return; // Already has label
        
        const label = document.createElement('div');
        label.className = 'language-label';
        label.textContent = language[1].toUpperCase();
        
        pre.style.position = 'relative';
        pre.appendChild(label);
    }
}

// Initialize tooltips
function initializeTooltips() {
    // Add tooltips to navigation items
    const navLinks = document.querySelectorAll('.nav-menu a');
    navLinks.forEach(link => {
        if (!link.title && link.textContent.trim()) {
            link.title = link.textContent.trim();
        }
    });
    
    console.log('?? Tooltips initialized');
}

// Initialize mobile menu
function initializeMobileMenu() {
    const mobileToggle = document.querySelector('.mobile-menu-toggle');
    if (mobileToggle) {
        mobileToggle.addEventListener('click', toggleSidebar);
        console.log('?? Mobile menu initialized');
    }
}

// Smooth scrolling for anchor links
document.addEventListener('click', function(e) {
    const target = e.target.closest('a[href^="#"]');
    if (target) {
        e.preventDefault();
        const id = target.getAttribute('href').substring(1);
        const element = document.getElementById(id);
        
        if (element) {
            element.scrollIntoView({
                behavior: 'smooth',
                block: 'start'
            });
        }
    }
});

// Add CSS for copy buttons, language labels, and enhanced navigation
const additionalStyles = `
<style>
/* Copy button styles */
.copy-button {
    position: absolute;
    top: 10px;
    right: 10px;
    background: rgba(0,0,0,0.7);
    color: white;
    border: none;
    border-radius: 4px;
    padding: 5px 8px;
    cursor: pointer;
    font-size: 12px;
    opacity: 0;
    transition: opacity 0.3s ease;
    z-index: 10;
}

pre:hover .copy-button {
    opacity: 1;
}

/* Language label styles */
.language-label {
    position: absolute;
    top: 10px;
    left: 10px;
    background: rgba(0,123,255,0.8);
    color: white;
    padding: 2px 8px;
    border-radius: 3px;
    font-size: 11px;
    font-weight: 600;
    text-transform: uppercase;
    z-index: 10;
}

/* Enhanced navigation styles */
.nav-menu .has-submenu > a::after {
    content: '?';
    float: right;
    font-size: 12px;
    transition: transform 0.3s ease;
}

.nav-menu .has-submenu.open > a::after {
    transform: rotate(90deg);
}

.nav-menu .submenu {
    max-height: 0;
    overflow: hidden;
    transition: max-height 0.3s ease;
}

.nav-menu .has-submenu.open .submenu {
    max-height: 500px;
}

.nav-menu .submenu li a {
    padding-left: 40px;
    font-size: 14px;
}

.nav-menu a.active {
    background-color: var(--primary-color, #007bff);
    color: white;
    border-radius: 4px;
}

/* Search suggestions */
.search-suggestions {
    position: absolute;
    top: 100%;
    left: 0;
    right: 0;
    background: var(--bg-color, white);
    border: 1px solid var(--border-color, #ddd);
    border-radius: 0 0 8px 8px;
    max-height: 300px;
    overflow-y: auto;
    z-index: 1000;
    box-shadow: 0 4px 12px rgba(0,0,0,0.15);
}

/* Mobile responsive adjustments */
@media (max-width: 768px) {
    .sidebar {
        transform: translateX(-100%);
        transition: transform 0.3s ease;
    }
    
    .sidebar.mobile-open {
        transform: translateX(0);
    }
}
</style>
`;

// Inject additional styles
document.head.insertAdjacentHTML('beforeend', additionalStyles);

console.log('?? Beep Python ML Documentation Navigation System Loaded');