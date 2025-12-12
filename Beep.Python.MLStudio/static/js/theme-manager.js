/**
 * MLStudio Theme Manager
 * Centralized theme management with localStorage persistence
 */

class ThemeManager {
    constructor() {
        this.themes = ['light', 'dark', 'blue', 'green', 'purple', 'orange'];
        this.currentTheme = this.loadTheme();
        this.init();
    }

    init() {
        // Apply saved theme on load
        this.applyTheme(this.currentTheme);
        
        // Listen for system theme changes
        if (window.matchMedia) {
            const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');
            mediaQuery.addEventListener('change', (e) => {
                if (this.currentTheme === 'auto') {
                    this.applyTheme(e.matches ? 'dark' : 'light');
                }
            });
        }
    }

    loadTheme() {
        // Try to load from localStorage
        const saved = localStorage.getItem('mlstudio-theme');
        if (saved && this.themes.includes(saved)) {
            return saved;
        }
        
        // Default to system preference or light
        if (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) {
            return 'dark';
        }
        
        return 'light';
    }

    saveTheme(theme) {
        localStorage.setItem('mlstudio-theme', theme);
    }

    applyTheme(theme) {
        if (!this.themes.includes(theme)) {
            console.warn(`Unknown theme: ${theme}`);
            return;
        }

        // Remove all theme attributes
        this.themes.forEach(t => {
            document.documentElement.removeAttribute(`data-theme-${t}`);
        });
        
        // Apply new theme
        document.documentElement.setAttribute('data-theme', theme);
        this.currentTheme = theme;
        this.saveTheme(theme);
        
        // Dispatch custom event for theme change
        window.dispatchEvent(new CustomEvent('themechange', { 
            detail: { theme: theme } 
        }));
        
        // Update theme switcher UI if it exists
        this.updateThemeSwitcherUI();
    }

    getCurrentTheme() {
        return this.currentTheme;
    }

    getAvailableThemes() {
        return this.themes;
    }

    updateThemeSwitcherUI() {
        // Update dropdown/select if it exists
        const themeSelect = document.getElementById('theme-select');
        if (themeSelect) {
            themeSelect.value = this.currentTheme;
        }
        
        // Update button text if it exists
        const themeBtn = document.getElementById('theme-switcher-btn');
        if (themeBtn) {
            const icon = this.getThemeIcon(this.currentTheme);
            themeBtn.innerHTML = `${icon} <span class="d-none d-md-inline">${this.capitalize(this.currentTheme)}</span>`;
        }
    }

    getThemeIcon(theme) {
        const icons = {
            'light': '<i class="bi bi-sun"></i>',
            'dark': '<i class="bi bi-moon"></i>',
            'blue': '<i class="bi bi-palette"></i>',
            'green': '<i class="bi bi-palette"></i>',
            'purple': '<i class="bi bi-palette"></i>',
            'orange': '<i class="bi bi-palette"></i>'
        };
        return icons[theme] || icons.light;
    }

    capitalize(str) {
        return str.charAt(0).toUpperCase() + str.slice(1);
    }

    // Create theme switcher dropdown HTML
    createThemeSwitcherHTML() {
        return `
            <div class="dropdown theme-switcher">
                <button class="btn theme-switcher-btn dropdown-toggle" type="button" id="theme-switcher-btn" data-bs-toggle="dropdown" aria-expanded="false">
                    ${this.getThemeIcon(this.currentTheme)}
                    <span class="d-none d-md-inline">${this.capitalize(this.currentTheme)}</span>
                </button>
                <ul class="dropdown-menu dropdown-menu-end" aria-labelledby="theme-switcher-btn">
                    ${this.themes.map(theme => `
                        <li>
                            <a class="dropdown-item ${theme === this.currentTheme ? 'active' : ''}" href="#" data-theme="${theme}">
                                ${this.getThemeIcon(theme)} ${this.capitalize(theme)}
                                ${theme === this.currentTheme ? '<i class="bi bi-check float-end"></i>' : ''}
                            </a>
                        </li>
                    `).join('')}
                </ul>
            </div>
        `;
    }
}

// Initialize theme manager
const themeManager = new ThemeManager();

// Make it globally available
window.themeManager = themeManager;

// Handle theme selection clicks
document.addEventListener('DOMContentLoaded', () => {
    // Listen for theme selection in dropdown
    document.addEventListener('click', (e) => {
        // Don't handle theme clicks if it's a form submit button or inside a form
        if (e.target.closest('form') || e.target.closest('[data-form-submit]') || e.target.type === 'submit') {
            return;
        }
        
        // Don't interfere with modals, checkboxes, radio buttons, or other interactive elements
        if (e.target.closest('.modal') || 
            e.target.type === 'checkbox' || 
            e.target.type === 'radio' ||
            e.target.closest('input[type="checkbox"]') ||
            e.target.closest('input[type="radio"]') ||
            e.target.closest('.form-check') ||
            e.target.closest('.list-group-item') ||
            e.target.closest('.btn') ||
            e.target.closest('button') ||
            e.target.closest('.input-group')) {
            return;
        }
        
        if (e.target.closest('[data-theme]')) {
            e.preventDefault();
            const theme = e.target.closest('[data-theme]').getAttribute('data-theme');
            themeManager.applyTheme(theme);
        }
    });
    
    // Update UI on theme change
    window.addEventListener('themechange', (e) => {
        console.log('Theme changed to:', e.detail.theme);
    });
});

