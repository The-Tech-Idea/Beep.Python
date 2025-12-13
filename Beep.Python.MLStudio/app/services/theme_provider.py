"""
Theme Provider Service
Provides theme configuration from JSON theme files, similar to C# ThemeProvider
"""
import json
import logging
from pathlib import Path
from typing import Optional, Dict, Any
from dataclasses import dataclass, field, asdict
from flask import current_app

logger = logging.getLogger(__name__)


@dataclass
class BrandingConfig:
    """Theme configuration data class matching C# BrandingConfig structure"""
    # App Identity
    AppName: str = "Beep ML Studio"
    AppShortName: str = "ML Studio"
    Tagline: str = "Machine Learning Development Platform"
    Copyright: str = "© 2025 TheTechIdea. All rights reserved."
    Version: str = "1.0.0"
    
    # Logo URLs
    LogoUrl: str = "/static/images/branding/logo-32.png"
    LogoDarkUrl: str = "/static/images/branding/logo-32.png"
    IconUrl: str = "/static/images/branding/logo-32.png"
    FaviconUrl: str = "/static/images/branding/favicon.ico"
    
    # Primary Colors
    PrimaryColor: str = "#00ff88"
    PrimaryHoverColor: str = "#00cc6f"
    PrimaryTransparentColor: str = "rgba(0, 255, 136, 0.1)"
    PrimaryColorDark: str = "#00ff88"
    
    # Secondary Colors
    SecondaryColor: str = "#00d4ff"
    SecondaryColorDark: str = "#00d4ff"
    TertiaryColor: str = "#bd93f9"
    
    # Background Colors
    BackgroundColor: str = "#0d1117"
    BackgroundDarkColor: str = "#010409"
    BackgroundColorDark: str = "#010409"
    SurfaceColor: str = "#161b22"
    SurfaceColorDark: str = "#161b22"
    SurfaceElevatedColor: str = "#21262d"
    
    # Border Colors
    BorderColor: str = "#30363d"
    BorderColorDark: str = "#30363d"
    
    # Text Colors
    TextPrimaryColor: str = "#e6edf3"
    TextPrimaryDark: str = "#e6edf3"
    TextPrimary: str = "#e6edf3"
    TextSecondaryColor: str = "#8b949e"
    TextSecondaryDark: str = "#8b949e"
    TextSecondary: str = "#8b949e"
    TextOnPrimaryColor: str = "#0d1117"
    TextOnPrimary: str = "#0d1117"
    
    # Semantic Colors
    SuccessColor: str = "#00ff88"
    WarningColor: str = "#ffd866"
    ErrorColor: str = "#ff6b9d"
    InfoColor: str = "#00d4ff"
    
    # Typography
    FontFamily: str = "'JetBrains Mono', 'Fira Code', 'Consolas', monospace"
    HeadingFontFamily: str = "'JetBrains Mono', 'Fira Code', monospace"
    FontMonoFamily: str = "'JetBrains Mono', 'Fira Code', 'Consolas', monospace"
    BaseFontSize: str = "14px"
    
    # RTL Support
    RtlFontFamily: Optional[str] = None
    RtlHeadingFontFamily: Optional[str] = None
    SupportRtl: bool = False
    
    # Login/Register
    LoginBackgroundUrl: str = ""
    LoginTitle: str = "$ ssh user@mlstudio"
    LoginWelcomeMessage: str = "$ ssh user@mlstudio"
    LoginSubtitle: str = "Authenticating..."
    RegisterTitle: str = "$ ./create-account"
    RegisterSubtitle: str = "Initializing new user..."
    
    # Theme Settings
    IsDarkModeDefault: bool = True
    DefaultDarkMode: bool = True
    EnableDarkMode: bool = True
    AllowThemeSwitching: bool = False
    ShowVersionInFooter: bool = True
    UseTerminalStyle: bool = True
    ShowSocialLogins: bool = True
    ShowRememberMe: bool = True
    
    # Design System
    BorderRadius: str = "8px"
    BorderRadiusSm: str = "6px"
    BorderRadiusMd: str = "8px"
    BorderRadiusLg: str = "12px"
    BorderRadiusXl: str = "16px"
    
    ShadowSm: str = "0 1px 2px 0 rgba(0, 255, 136, 0.05)"
    ShadowMd: str = "0 4px 6px -1px rgba(0, 255, 136, 0.1)"
    ShadowLg: str = "0 10px 15px -3px rgba(0, 255, 136, 0.1)"
    ShadowXl: str = "0 20px 25px -5px rgba(0, 255, 136, 0.1)"
    
    TransitionDuration: str = "200ms"
    TransitionEasing: str = "ease-out"
    
    # Gray Scale
    Gray50: str = "#f0f6fc"
    Gray100: str = "#c9d1d9"
    Gray200: str = "#b1bac4"
    Gray300: str = "#8b949e"
    Gray400: str = "#6e7681"
    Gray500: str = "#484f58"
    Gray600: str = "#30363d"
    Gray700: str = "#21262d"
    Gray800: str = "#161b22"
    Gray900: str = "#0d1117"
    
    # Social Links
    GitHubUrl: Optional[str] = None
    TwitterUrl: Optional[str] = None
    LinkedInUrl: Optional[str] = None
    DiscordUrl: Optional[str] = None
    YouTubeUrl: Optional[str] = None
    SupportEmail: str = "support@thetechidea.com"
    SupportUrl: str = "/support"
    WebsiteUrl: Optional[str] = None
    
    def synchronize_properties(self):
        """Synchronize duplicate properties (e.g., TextPrimary and TextPrimaryColor)"""
        # Ensure consistency between property variants
        if not hasattr(self, '_synced'):
            self.TextPrimary = self.TextPrimaryColor
            self.TextSecondary = self.TextSecondaryColor
            self.TextOnPrimary = self.TextOnPrimaryColor
            self._synced = True
    
    def to_dict(self) -> Dict[str, Any]:
        """Convert to dictionary for JSON serialization"""
        return asdict(self)
    
    @classmethod
    def from_dict(cls, data: Dict[str, Any]) -> 'BrandingConfig':
        """Create from dictionary"""
        # Filter out None values and create instance
        filtered_data = {k: v for k, v in data.items() if v is not None}
        instance = cls(**filtered_data)
        instance.synchronize_properties()
        return instance


class ThemeProvider:
    """Theme provider that loads themes from JSON files"""
    
    def __init__(self, app=None):
        self.app = app
        self._current_theme: Optional[BrandingConfig] = None
        self._theme_cache: Dict[str, BrandingConfig] = {}
        
        if app:
            self.init_app(app)
    
    def init_app(self, app):
        """Initialize theme provider with Flask app"""
        self.app = app
        self._load_default_theme()
    
    def _load_default_theme(self):
        """Load the default terminal theme"""
        try:
            # Try multiple paths for theme file
            possible_paths = [
                Path(self.app.root_path).parent / 'themes' / 'TerminalTheme.json',  # Project root/themes
                Path(self.app.instance_path) / 'themes' / 'TerminalTheme.json',  # Instance/themes
                Path(self.app.root_path) / 'themes' / 'TerminalTheme.json',  # App root/themes
            ]
            
            theme_path = None
            for path in possible_paths:
                if path.exists():
                    theme_path = path
                    break
            
            if theme_path and theme_path.exists():
                self._current_theme = self.load_theme_from_file(theme_path)
                logger.info(f"Loaded theme from {theme_path}")
            else:
                # Use default terminal theme
                self._current_theme = BrandingConfig()
                self._current_theme.synchronize_properties()
                logger.info("Using default terminal theme (TerminalTheme.json not found)")
        except Exception as e:
            logger.error(f"Failed to load theme: {e}")
            # Fallback to default
            self._current_theme = BrandingConfig()
            self._current_theme.synchronize_properties()
    
    def load_theme_from_file(self, theme_path: Path) -> Optional[BrandingConfig]:
        """Load theme from JSON file"""
        try:
            if not theme_path.exists():
                logger.warning(f"Theme file not found: {theme_path}")
                return None
            
            with open(theme_path, 'r', encoding='utf-8') as f:
                data = json.load(f)
            
            theme = BrandingConfig.from_dict(data)
            self._theme_cache[str(theme_path)] = theme
            return theme
        except Exception as e:
            logger.error(f"Failed to load theme from {theme_path}: {e}")
            return None
    
    def get_branding(self) -> BrandingConfig:
        """Get current branding configuration"""
        if self._current_theme is None:
            self._load_default_theme()
        return self._current_theme or BrandingConfig()
    
    def get_branding_for_app(self, app_name: str = None) -> BrandingConfig:
        """Get branding with app-specific overrides"""
        branding = self.get_branding()
        
        # Override app name if provided
        if app_name:
            branding.AppName = app_name
            branding.AppShortName = app_name.split()[0] if ' ' in app_name else app_name
        
        return branding
    
    def set_theme(self, theme_name: str):
        """Load and set a specific theme"""
        try:
            # Try multiple paths
            possible_paths = [
                Path(self.app.root_path).parent / 'themes' / f'{theme_name}Theme.json',
                Path(self.app.instance_path) / 'themes' / f'{theme_name}Theme.json',
                Path(self.app.root_path) / 'themes' / f'{theme_name}Theme.json',
            ]
            
            theme_path = None
            for path in possible_paths:
                if path.exists():
                    theme_path = path
                    break
            
            if theme_path and theme_path.exists():
                self._current_theme = self.load_theme_from_file(theme_path)
                logger.info(f"Switched to theme: {theme_name}")
                return True
            else:
                logger.warning(f"Theme not found: {theme_name}")
                return False
        except Exception as e:
            logger.error(f"Failed to set theme {theme_name}: {e}")
            return False


# Global theme provider instance
_theme_provider: Optional[ThemeProvider] = None


def get_theme_provider() -> ThemeProvider:
    """Get the global theme provider instance"""
    global _theme_provider
    if _theme_provider is None:
        _theme_provider = ThemeProvider(current_app)
    return _theme_provider


def init_theme_provider(app):
    """Initialize theme provider with Flask app"""
    global _theme_provider
    _theme_provider = ThemeProvider(app)
    return _theme_provider

