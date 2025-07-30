// Beep.Python.DataManagement Documentation Navigation
// Enhanced navigation system with pandas-specific features

document.addEventListener('DOMContentLoaded', function() {
    // Determine the base path for navigation
    const currentPath = window.location.pathname;
    const basePath = currentPath.includes('/api/') || currentPath.includes('/examples/') || currentPath.includes('/configuration/') 
        ? '../' : './';

    // Initialize navigation
    initializeSidebar();
    initializeTheme();
    initializeMobileMenu();
    initializeSearch();
    initializeCodeCopy();
    highlightCurrentPage();
});

function initializeSidebar() {
    const sidebar = document.getElementById('sidebar');
    if (!sidebar) return;

    const currentPath = window.location.pathname;
    const basePath = currentPath.includes('/api/') || currentPath.includes('/examples/') || currentPath.includes('/configuration/') 
        ? '../' : './';

    sidebar.innerHTML = `
        <!-- Sidebar Header -->
        <div class="sidebar-header">
            <i class="bi bi-table text-primary"></i>
            <div>
                <h3>DataManagement</h3>
                <span class="version">v1.0.0</span>
            </div>
        </div>

        <!-- Search -->
        <div class="search-container">
            <input type="text" class="search-input" placeholder="Search documentation..." id="search-input">
        </div>

        <!-- Navigation Menu -->
        <nav class="sidebar-nav">
            <ul class="nav-menu" id="nav-menu">
                <li><a href="${basePath}index.html" id="nav-home"><i class="bi bi-house-fill"></i> Home</a></li>
                <li><a href="${basePath}getting-started.html" id="nav-getting-started"><i class="bi bi-rocket-takeoff"></i> Getting Started</a></li>
                
                <li class="has-submenu" id="nav-core-api">
                    <a href="#"><i class="bi bi-cpu"></i> Core API</a>
                    <ul class="submenu">
                        <li><a href="${basePath}api/PythonPandasManager.html" id="nav-pandas-manager"><i class="bi bi-circle-fill text-primary"></i> PythonPandasManager</a></li>
                        <li><a href="${basePath}api/IPythonPandasManager.html" id="nav-pandas-interface"><i class="bi bi-circle-fill text-info"></i> IPythonPandasManager</a></li>
                    </ul>
                </li>
                
                <li class="has-submenu" id="nav-operations">
                    <a href="#"><i class="bi bi-gear-wide-connected"></i> Data Operations</a>
                    <ul class="submenu">
                        <li><a href="${basePath}api/dataframe-io.html" id="nav-dataframe-io"><i class="bi bi-circle-fill text-io"></i> I/O Operations</a></li>
                        <li><a href="${basePath}api/data-manipulation.html" id="nav-data-manipulation"><i class="bi bi-circle-fill text-transform"></i> Data Manipulation</a></li>
                        <li><a href="${basePath}api/statistical-analysis.html" id="nav-statistical-analysis"><i class="bi bi-circle-fill text-analyze"></i> Statistical Analysis</a></li>
                        <li><a href="${basePath}api/data-cleaning.html" id="nav-data-cleaning"><i class="bi bi-circle-fill text-clean"></i> Data Cleaning</a></li>
                        <li><a href="${basePath}api/time-series.html" id="nav-time-series"><i class="bi bi-circle-fill text-warning"></i> Time Series</a></li>
                        <li><a href="${basePath}api/merging-joining.html" id="nav-merging-joining"><i class="bi bi-circle-fill text-success"></i> Merging & Joining</a></li>
                    </ul>
                </li>
                
                <li class="has-submenu" id="nav-examples">
                    <a href="#"><i class="bi bi-code-square"></i> Examples</a>
                    <ul class="submenu">
                        <li><a href="${basePath}examples/basic-usage.html" id="nav-basic-examples">Basic Usage</a></li>
                        <li><a href="${basePath}examples/data-analysis-pipeline.html" id="nav-analysis-pipeline">Analysis Pipeline</a></li>
                        <li><a href="${basePath}examples/business-intelligence.html" id="nav-business-intelligence">Business Intelligence</a></li>
                        <li><a href="${basePath}examples/etl-workflows.html" id="nav-etl-workflows">ETL Workflows</a></li>
                        <li><a href="${basePath}examples/time-series-analysis.html" id="nav-timeseries-examples">Time Series Analysis</a></li>
                        <li><a href="${basePath}examples/statistical-modeling.html" id="nav-statistical-modeling">Statistical Modeling</a></li>
                    </ul>
                </li>
                
                <li class="has-submenu" id="nav-configuration">
                    <a href="#"><i class="bi bi-gear"></i> Configuration</a>
                    <ul class="submenu">
                        <li><a href="${basePath}configuration/session-management.html" id="nav-session-config">Session Management</a></li>
                        <li><a href="${basePath}configuration/virtual-environments.html" id="nav-venv-config">Virtual Environments</a></li>
                        <li><a href="${basePath}configuration/performance-tuning.html" id="nav-performance-config">Performance Tuning</a></li>
                        <li><a href="${basePath}configuration/security.html" id="nav-security-config">Security & Compliance</a></li>
                    </ul>
                </li>
                
                <li class="has-submenu" id="nav-integration">
                    <a href="#"><i class="bi bi-plugin"></i> Integration</a>
                    <ul class="submenu">
                        <li><a href="${basePath}integration/beep-framework.html" id="nav-beep-integration">BEEP Framework</a></li>
                        <li><a href="${basePath}integration/dependency-injection.html" id="nav-di-integration">Dependency Injection</a></li>
                        <li><a href="${basePath}integration/workflow-steps.html" id="nav-workflow-integration">Workflow Steps</a></li>
                        <li><a href="${basePath}integration/enterprise-deployment.html" id="nav-enterprise-deployment">Enterprise Deployment</a></li>
                    </ul>
                </li>
                
                <li><a href="${basePath}troubleshooting.html" id="nav-troubleshooting"><i class="bi bi-tools"></i> Troubleshooting</a></li>
                <li><a href="${basePath}changelog.html" id="nav-changelog"><i class="bi bi-clock-history"></i> Changelog</a></li>
            </ul>
        </nav>

        <!-- Navigation Footer -->
        <div class="nav-footer">
            <div class="nav-footer-section">
                <h6><i class="bi bi-info-circle"></i> Quick Links</h6>
                <ul class="nav-footer-links">
                    <li><a href="${basePath}api/PythonPandasManager.html">API Reference</a></li>
                    <li><a href="${basePath}examples/data-analysis-pipeline.html">Examples</a></li>
                    <li><a href="https://github.com/The-Tech-Idea/Beep.Python" target="_blank">GitHub</a></li>
                </ul>
            </div>
            
            <div class="nav-footer-section">
                <h6><i class="bi bi-question-circle"></i> Support</h6>
                <ul class="nav-footer-links">
                    <li><a href="${basePath}troubleshooting.html">Troubleshooting</a></li>
                    <li><a href="https://github.com/The-Tech-Idea/Beep.Python/issues" target="_blank">Report Issues</a></li>
                    <li><a href="https://github.com/The-Tech-Idea/Beep.Python/discussions" target="_blank">Discussions</a></li>
                </ul>
            </div>
        </div>
    `;

    // Initialize submenu toggles
    initializeSubmenus();
}

function initializeSubmenus() {
    const submenuItems = document.querySelectorAll('.has-submenu');
    
    submenuItems.forEach(item => {
        const link = item.querySelector('a');
        
        link.addEventListener('click', function(e) {
            e.preventDefault();
            
            // Close other submenus
            submenuItems.forEach(otherItem => {
                if (otherItem !== item) {
                    otherItem.classList.remove('open');
                }
            });
            
            // Toggle current submenu
            item.classList.toggle('open');
        });
    });
}

function highlightCurrentPage() {
    const currentPath = window.location.pathname;
    const currentFile = currentPath.split('/').pop() || 'index.html';
    
    // Remove .html extension for matching
    const currentPage = currentFile.replace('.html', '');
    
    // Find and activate the current page link
    const navLinks = document.querySelectorAll('.nav-menu a');
    
    navLinks.forEach(link => {
        const href = link.getAttribute('href');
        if (href && (href.includes(currentFile) || href.includes(currentPage))) {
            link.classList.add('active');
            
            // Open parent submenu if this is a submenu item
            const parentSubmenu = link.closest('.has-submenu');
            if (parentSubmenu) {
                parentSubmenu.classList.add('open');
            }
        }
    });
}

function initializeTheme() {
    const themeToggle = document.querySelector('.theme-toggle');
    const themeIcon = document.getElementById('theme-icon');
    
    // Load saved theme or default to light
    const savedTheme = localStorage.getItem('datamanagement-docs-theme') || 'light';
    document.documentElement.setAttribute('data-theme', savedTheme);
    updateThemeIcon(savedTheme);
    
    if (themeToggle) {
        themeToggle.addEventListener('click', () => {
            const currentTheme = document.documentElement.getAttribute('data-theme');
            const newTheme = currentTheme === 'dark' ? 'light' : 'dark';
            
            document.documentElement.setAttribute('data-theme', newTheme);
            localStorage.setItem('datamanagement-docs-theme', newTheme);
            updateThemeIcon(newTheme);
        });
    }
    
    function updateThemeIcon(theme) {
        if (themeIcon) {
            themeIcon.className = theme === 'dark' ? 'bi bi-moon-fill' : 'bi bi-sun-fill';
        }
    }
}

function initializeMobileMenu() {
    const mobileToggle = document.querySelector('.mobile-menu-toggle');
    const sidebar = document.querySelector('.sidebar');
    
    if (mobileToggle && sidebar) {
        mobileToggle.addEventListener('click', () => {
            sidebar.classList.toggle('open');
        });
        
        // Close sidebar when clicking outside
        document.addEventListener('click', (e) => {
            if (!sidebar.contains(e.target) && !mobileToggle.contains(e.target)) {
                sidebar.classList.remove('open');
            }
        });
    }
}

function initializeSearch() {
    const searchInput = document.getElementById('search-input');
    const navMenu = document.getElementById('nav-menu');
    
    if (!searchInput || !navMenu) return;
    
    // Store original menu items
    const originalItems = Array.from(navMenu.querySelectorAll('li'));
    
    searchInput.addEventListener('input', function() {
        const searchTerm = this.value.toLowerCase().trim();
        
        if (searchTerm === '') {
            // Show all items
            originalItems.forEach(item => {
                item.style.display = '';
            });
            return;
        }
        
        // Filter items based on search term
        originalItems.forEach(item => {
            const text = item.textContent.toLowerCase();
            const shouldShow = text.includes(searchTerm);
            
            item.style.display = shouldShow ? '' : 'none';
            
            // If this is a parent with submenu, show it if any child matches
            if (item.classList.contains('has-submenu') && !shouldShow) {
                const submenuItems = item.querySelectorAll('.submenu li');
                const hasMatchingChild = Array.from(submenuItems).some(child => 
                    child.textContent.toLowerCase().includes(searchTerm)
                );
                
                if (hasMatchingChild) {
                    item.style.display = '';
                    item.classList.add('open'); // Open submenu to show matching items
                }
            }
        });
    });
    
    // Clear search on escape
    searchInput.addEventListener('keydown', function(e) {
        if (e.key === 'Escape') {
            this.value = '';
            this.dispatchEvent(new Event('input'));
            this.blur();
        }
    });
}

function initializeCodeCopy() {
    // Add copy buttons to code blocks
    const codeBlocks = document.querySelectorAll('pre code');
    
    codeBlocks.forEach((block, index) => {
        const pre = block.parentElement;
        if (!pre) return;
        
        // Create copy button
        const copyButton = document.createElement('button');
        copyButton.className = 'copy-button btn btn-sm btn-outline-secondary';
        copyButton.innerHTML = '<i class="bi bi-clipboard"></i>';
        copyButton.title = 'Copy code';
        
        // Position the button
        pre.style.position = 'relative';
        copyButton.style.position = 'absolute';
        copyButton.style.top = '8px';
        copyButton.style.right = '8px';
        copyButton.style.opacity = '0';
        copyButton.style.transition = 'opacity 0.3s ease';
        
        pre.appendChild(copyButton);
        
        // Show button on hover
        pre.addEventListener('mouseenter', () => {
            copyButton.style.opacity = '1';
        });
        
        pre.addEventListener('mouseleave', () => {
            copyButton.style.opacity = '0';
        });
        
        // Copy functionality
        copyButton.addEventListener('click', async () => {
            try {
                await navigator.clipboard.writeText(block.textContent);
                
                // Provide feedback
                const originalHTML = copyButton.innerHTML;
                copyButton.innerHTML = '<i class="bi bi-check"></i>';
                copyButton.classList.add('btn-success');
                copyButton.classList.remove('btn-outline-secondary');
                
                setTimeout(() => {
                    copyButton.innerHTML = originalHTML;
                    copyButton.classList.remove('btn-success');
                    copyButton.classList.add('btn-outline-secondary');
                }, 1000);
                
            } catch (err) {
                console.error('Failed to copy code:', err);
                
                // Fallback for older browsers
                const textArea = document.createElement('textarea');
                textArea.value = block.textContent;
                document.body.appendChild(textArea);
                textArea.focus();
                textArea.select();
                document.execCommand('copy');
                document.body.removeChild(textArea);
                
                // Show feedback
                copyButton.innerHTML = '<i class="bi bi-check"></i>';
                setTimeout(() => {
                    copyButton.innerHTML = '<i class="bi bi-clipboard"></i>';
                }, 1000);
            }
        });
    });
}

// Export functions for external use
window.toggleSidebar = function() {
    const sidebar = document.querySelector('.sidebar');
    if (sidebar) {
        sidebar.classList.toggle('open');
    }
};

window.toggleTheme = function() {
    const currentTheme = document.documentElement.getAttribute('data-theme');
    const newTheme = currentTheme === 'dark' ? 'light' : 'dark';
    
    document.documentElement.setAttribute('data-theme', newTheme);
    localStorage.setItem('datamanagement-docs-theme', newTheme);
    
    const themeIcon = document.getElementById('theme-icon');
    if (themeIcon) {
        themeIcon.className = newTheme === 'dark' ? 'bi bi-moon-fill' : 'bi bi-sun-fill';
    }
};

// Smooth scrolling for anchor links
document.addEventListener('click', function(e) {
    if (e.target.tagName === 'A' && e.target.getAttribute('href').startsWith('#')) {
        e.preventDefault();
        const targetId = e.target.getAttribute('href').substring(1);
        const targetElement = document.getElementById(targetId);
        
        if (targetElement) {
            targetElement.scrollIntoView({
                behavior: 'smooth'
            });
        }
    }
});

// Add animation classes on scroll
const observerOptions = {
    threshold: 0.1,
    rootMargin: '0px 0px -50px 0px'
};

const observer = new IntersectionObserver((entries) => {
    entries.forEach(entry => {
        if (entry.isIntersecting) {
            entry.target.style.animation = 'fadeIn 0.6s ease-out';
        }
    });
}, observerOptions);

// Observe all sections when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    const sections = document.querySelectorAll('.section');
    sections.forEach(section => observer.observe(section));
});