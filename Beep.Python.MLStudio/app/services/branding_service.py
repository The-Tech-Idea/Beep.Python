"""
Branding Service - White-label branding and industry-specific themes for ML Studio
Compatible with Community platform branding for consistency
"""
import json
import os
import shutil
from pathlib import Path
from typing import Dict, Optional, List
from dataclasses import dataclass, asdict


@dataclass
class BrandingConfig:
    """Branding configuration for white-label customization"""
    company_name: str = "Beep ML Studio"
    app_name: str = "Beep ML Studio"
    app_short_name: str = "ML Studio"
    tagline: str = "Machine Learning Development Platform"
    copyright_text: str = "© 2025 TheTechIdea. All rights reserved."
    version: str = "1.0.0"
    logo_url: str = "/static/images/branding/logo-32.png"
    logo_dark_url: str = "/static/images/branding/logo-32.png"
    icon_url: str = "/static/images/branding/logo-32.png"
    favicon_url: str = "/static/images/branding/favicon.ico"
    industry: str = "general"
    theme_name: str = "default"
    primary_color: str = "#00ff88"
    secondary_color: str = "#00d4ff"
    accent_color: str = "#bd93f9"
    background_color: str = "#0d1117"
    text_color: str = "#c9d1d9"
    header_background: str = "#161b22"
    header_text: str = "#c9d1d9"
    success_color: str = "#00ff88"
    warning_color: str = "#ffa500"
    error_color: str = "#e74c3c"
    info_color: str = "#00d4ff"
    support_email: str = "support@thetechidea.com"
    support_url: str = "/help"
    website_url: str = ""
    show_powered_by: bool = True
    allow_theme_switching: bool = True
    enable_dark_mode: bool = True
    
    def to_dict(self) -> Dict:
        """Convert to dictionary"""
        return asdict(self)
    
    @classmethod
    def from_dict(cls, data: Dict) -> 'BrandingConfig':
        """Create from dictionary"""
        return cls(**{k: v for k, v in data.items() if hasattr(cls, k)})


class BrandingService:
    """Service for managing branding and white-label customization"""
    
    INDUSTRY_THEMES = {
        'oil_gas': {
            'industry': 'oil_gas',
            'company_name': 'Oil & Gas ML Platform',
            'app_name': 'Oil & Gas ML Studio',
            'app_short_name': 'ML Studio',
            'primary_color': '#d4af37',
            'secondary_color': '#8b4513',
            'accent_color': '#2c5aa0',
            'background_color': '#0d1117',
            'text_color': '#c9d1d9',
            'header_background': '#161b22',
            'header_text': '#c9d1d9',
            'tagline': 'ML Solutions for Oil & Gas Industry',
            'logo_url': '/static/assets/icons/007-refinery.png',
            'theme_name': 'oil_gas'
        },
        'finance': {
            'industry': 'finance',
            'company_name': 'Financial ML Platform',
            'app_name': 'Finance ML Studio',
            'app_short_name': 'ML Studio',
            'primary_color': '#1e8449',
            'secondary_color': '#154360',
            'accent_color': '#d4af37',
            'background_color': '#0d1117',
            'text_color': '#c9d1d9',
            'header_background': '#161b22',
            'header_text': '#c9d1d9',
            'tagline': 'ML Solutions for Financial Services',
            'logo_url': '/static/assets/images/SimpleInfoApps.png',
            'theme_name': 'finance'
        },
        'healthcare': {
            'industry': 'healthcare',
            'company_name': 'Healthcare ML Platform',
            'app_name': 'Healthcare ML Studio',
            'app_short_name': 'ML Studio',
            'primary_color': '#0066cc',
            'secondary_color': '#00a86b',
            'accent_color': '#ff6b6b',
            'background_color': '#0d1117',
            'text_color': '#c9d1d9',
            'header_background': '#161b22',
            'header_text': '#c9d1d9',
            'tagline': 'ML Solutions for Healthcare & Medical Research',
            'logo_url': '/static/assets/icons/branding/healthcare.png',
            'theme_name': 'healthcare'
        },
        'real_estate': {
            'industry': 'real_estate',
            'company_name': 'Real Estate ML Platform',
            'app_name': 'Real Estate ML Studio',
            'app_short_name': 'ML Studio',
            'primary_color': '#8b4513',
            'secondary_color': '#daa520',
            'accent_color': '#4169e1',
            'background_color': '#0d1117',
            'text_color': '#c9d1d9',
            'header_background': '#161b22',
            'header_text': '#c9d1d9',
            'tagline': 'ML Solutions for Real Estate & Property Management',
            'logo_url': '/static/assets/icons/branding/real-estate.png',
            'theme_name': 'real_estate'
        },
        'retail': {
            'industry': 'retail',
            'company_name': 'Retail & E-Commerce ML Platform',
            'app_name': 'Retail ML Studio',
            'app_short_name': 'ML Studio',
            'primary_color': '#FF6B35',
            'secondary_color': '#C41E3A',
            'accent_color': '#2E86AB',
            'background_color': '#0d1117',
            'text_color': '#c9d1d9',
            'header_background': '#161b22',
            'header_text': '#c9d1d9',
            'tagline': 'ML Solutions for Retail & E-Commerce',
            'logo_url': '/static/assets/icons/retail/001-purchase.png',
            'theme_name': 'retail'
        },
        'manufacturing': {
            'industry': 'manufacturing',
            'company_name': 'Manufacturing ML Platform',
            'app_name': 'Manufacturing ML Studio',
            'app_short_name': 'ML Studio',
            'primary_color': '#4A5568',
            'secondary_color': '#3182CE',
            'accent_color': '#F56565',
            'background_color': '#0d1117',
            'text_color': '#c9d1d9',
            'header_background': '#161b22',
            'header_text': '#c9d1d9',
            'tagline': 'ML Solutions for Smart Manufacturing & Industry 4.0',
            'logo_url': '/static/assets/icons/manufacturing/002-gear.png',
            'theme_name': 'manufacturing'
        },
        'education': {
            'industry': 'education',
            'company_name': 'Education ML Platform',
            'app_name': 'Education ML Studio',
            'app_short_name': 'ML Studio',
            'primary_color': '#2563EB',
            'secondary_color': '#059669',
            'accent_color': '#7C3AED',
            'background_color': '#0d1117',
            'text_color': '#c9d1d9',
            'header_background': '#161b22',
            'header_text': '#c9d1d9',
            'tagline': 'ML Solutions for Education & Learning Analytics',
            'logo_url': '/static/assets/icons/education/008-folder.png',
            'theme_name': 'education'
        },
        'agriculture': {
            'industry': 'agriculture',
            'company_name': 'Agriculture ML Platform',
            'app_name': 'Agriculture ML Studio',
            'app_short_name': 'ML Studio',
            'primary_color': '#48BB78',
            'secondary_color': '#744210',
            'accent_color': '#F6E05E',
            'background_color': '#0d1117',
            'text_color': '#c9d1d9',
            'header_background': '#161b22',
            'header_text': '#c9d1d9',
            'tagline': 'ML Solutions for Precision Agriculture & Smart Farming',
            'logo_url': '/static/assets/icons/agriculture/002-tractor.png',
            'theme_name': 'agriculture'
        },
        'transportation': {
            'industry': 'transportation',
            'company_name': 'Transportation & Logistics ML Platform',
            'app_name': 'Transportation ML Studio',
            'app_short_name': 'ML Studio',
            'primary_color': '#2563EB',
            'secondary_color': '#059669',
            'accent_color': '#EA580C',
            'background_color': '#0d1117',
            'text_color': '#c9d1d9',
            'header_background': '#161b22',
            'header_text': '#c9d1d9',
            'tagline': 'ML Solutions for Transportation & Logistics',
            'logo_url': '/static/assets/icons/transportation/011-delivery.png',
            'theme_name': 'transportation'
        },
        'energy': {
            'industry': 'energy',
            'company_name': 'Energy & Utilities ML Platform',
            'app_name': 'Energy ML Studio',
            'app_short_name': 'ML Studio',
            'primary_color': '#FCD34D',
            'secondary_color': '#10B981',
            'accent_color': '#3B82F6',
            'background_color': '#0d1117',
            'text_color': '#c9d1d9',
            'header_background': '#161b22',
            'header_text': '#c9d1d9',
            'tagline': 'ML Solutions for Energy & Utilities Management',
            'logo_url': '/static/assets/icons/energy/005-battery.png',
            'theme_name': 'energy'
        },
        'insurance': {
            'industry': 'insurance',
            'company_name': 'Insurance ML Platform',
            'app_name': 'Insurance ML Studio',
            'app_short_name': 'ML Studio',
            'primary_color': '#1E3A8A',
            'secondary_color': '#047857',
            'accent_color': '#DC2626',
            'background_color': '#0d1117',
            'text_color': '#c9d1d9',
            'header_background': '#161b22',
            'header_text': '#c9d1d9',
            'tagline': 'ML Solutions for Insurance & Risk Management',
            'logo_url': '/static/assets/icons/insurance/016-lifebuoy.png',
            'theme_name': 'insurance'
        },
        'telecom': {
            'industry': 'telecom',
            'company_name': 'Telecommunications ML Platform',
            'app_name': 'Telecom ML Studio',
            'app_short_name': 'ML Studio',
            'primary_color': '#1E40AF',
            'secondary_color': '#7C3AED',
            'accent_color': '#059669',
            'background_color': '#0d1117',
            'text_color': '#c9d1d9',
            'header_background': '#161b22',
            'header_text': '#c9d1d9',
            'tagline': 'ML Solutions for Telecommunications',
            'logo_url': '/static/assets/icons/telecom/009-internet.png',
            'theme_name': 'telecom'
        },
        'media': {
            'industry': 'media',
            'company_name': 'Media & Entertainment ML Platform',
            'app_name': 'Media ML Studio',
            'app_short_name': 'ML Studio',
            'primary_color': '#9333EA',
            'secondary_color': '#EC4899',
            'accent_color': '#F97316',
            'background_color': '#0d1117',
            'text_color': '#c9d1d9',
            'header_background': '#161b22',
            'header_text': '#c9d1d9',
            'tagline': 'ML Solutions for Media & Entertainment',
            'logo_url': '/static/assets/icons/media/019-envelope.png',
            'theme_name': 'media'
        },
        'government': {
            'industry': 'government',
            'company_name': 'Government ML Platform',
            'app_name': 'Government ML Studio',
            'app_short_name': 'ML Studio',
            'primary_color': '#1E40AF',
            'secondary_color': '#DC2626',
            'accent_color': '#4B5563',
            'background_color': '#0d1117',
            'text_color': '#c9d1d9',
            'header_background': '#161b22',
            'header_text': '#c9d1d9',
            'tagline': 'ML Solutions for Government & Public Services',
            'logo_url': '/static/assets/icons/government/014-clipboard.png',
            'theme_name': 'government'
        },
        'sports': {
            'industry': 'sports',
            'company_name': 'Sports Analytics ML Platform',
            'app_name': 'Sports ML Studio',
            'app_short_name': 'ML Studio',
            'primary_color': '#EF4444',
            'secondary_color': '#10B981',
            'accent_color': '#F59E0B',
            'background_color': '#0d1117',
            'text_color': '#c9d1d9',
            'header_background': '#161b22',
            'header_text': '#c9d1d9',
            'tagline': 'ML Solutions for Sports Analytics & Performance',
            'logo_url': '/static/assets/icons/sports/006-target.png',
            'theme_name': 'sports'
        },
        'food_beverage': {
            'industry': 'food_beverage',
            'company_name': 'Food & Beverage ML Platform',
            'app_name': 'Food & Beverage ML Studio',
            'app_short_name': 'ML Studio',
            'primary_color': '#EF4444',
            'secondary_color': '#10B981',
            'accent_color': '#F59E0B',
            'background_color': '#0d1117',
            'text_color': '#c9d1d9',
            'header_background': '#161b22',
            'header_text': '#c9d1d9',
            'tagline': 'ML Solutions for Food & Beverage Industry',
            'logo_url': '/static/assets/icons/food_beverage/003-calendar.png',
            'theme_name': 'food_beverage'
        },
        'general': {
            'industry': 'general',
            'company_name': 'Beep ML Studio',
            'app_name': 'Beep ML Studio',
            'app_short_name': 'ML Studio',
            'primary_color': '#00ff88',
            'secondary_color': '#00d4ff',
            'accent_color': '#bd93f9',
            'background_color': '#0d1117',
            'text_color': '#c9d1d9',
            'header_background': '#161b22',
            'header_text': '#c9d1d9',
            'tagline': 'Machine Learning Development Platform',
            'logo_url': '/static/images/branding/logo-32.png',
            'theme_name': 'default'
        }
    }
    
    @staticmethod
    def get_branding_config() -> BrandingConfig:
        """Get current branding configuration"""
        config_file = Path('instance') / 'branding.json'
        
        if config_file.exists():
            try:
                with open(config_file, 'r') as f:
                    data = json.load(f)
                    return BrandingConfig.from_dict(data)
            except Exception:
                pass
        
        return BrandingConfig()
    
    @staticmethod
    def save_branding_config(config: BrandingConfig):
        """Save branding configuration"""
        config_file = Path('instance') / 'branding.json'
        config_file.parent.mkdir(exist_ok=True)
        
        with open(config_file, 'w') as f:
            json.dump(config.to_dict(), f, indent=2)
    
    @staticmethod
    def get_industry_theme(industry: str) -> Dict:
        """Get industry-specific theme preset"""
        return BrandingService.INDUSTRY_THEMES.get(industry, BrandingService.INDUSTRY_THEMES['general'])
    
    @staticmethod
    def get_available_industries() -> List[Dict]:
        """Get list of available industry presets"""
        industries = []
        for key, theme in BrandingService.INDUSTRY_THEMES.items():
            industries.append({
                'id': key,
                'name': theme.get('company_name', key.replace('_', ' ').title()),
                'tagline': theme.get('tagline', ''),
                'primary_color': theme.get('primary_color', '#00ff88')
            })
        return industries
