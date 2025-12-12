"""
Dashboard Routes
"""
from flask import Blueprint, render_template, session, redirect, url_for, current_app
from app import db
from app.models.project import MLProject
from app.industry_profiles import profile_manager

dashboard_bp = Blueprint('dashboard', __name__)


@dashboard_bp.route('/')
def index():
    """Main dashboard with industry mode selection"""
    # Check if forced industry mode is set
    forced_industry = current_app.config.get('FORCED_INDUSTRY_MODE')
    
    if forced_industry:
        # Map aliases to actual profile IDs
        industry_aliases = {
            'pet': 'petroleum',
            'oilandgas': 'petroleum',
            'oil': 'petroleum',
            'health': 'healthcare',
            'medical': 'healthcare',
            'finance': 'finance',
            'fin': 'finance',
            'manufacturing': 'manufacturing',
            'mfg': 'manufacturing'
        }
        
        # Resolve alias if needed
        profile_id = industry_aliases.get(forced_industry.lower(), forced_industry.lower())
        
        # Redirect to industry dashboard and set session
        profile = profile_manager.get(profile_id)
        if profile:
            session['industry_mode'] = profile_id
            session['forced_industry'] = True  # Mark as forced
            return redirect(url_for('industry.dashboard', profile_id=profile_id))
        else:
            # Invalid industry, show error but continue
            from flask import flash
            available = [p.id for p in profile_manager.list_all()]
            flash(f'Invalid industry mode: {forced_industry}. Available: {", ".join(available)}', 'warning')
    
    projects = MLProject.query.filter_by(status='active').order_by(MLProject.created_at.desc()).all()
    
    # Get industry profiles for the mode selector
    profiles = profile_manager.list_all()
    current_mode = session.get('industry_mode', 'advanced')
    current_profile = profile_manager.get(current_mode)
    
    return render_template('dashboard/index.html', 
                         projects=projects,
                         profiles=profiles,
                         current_mode=current_mode,
                         current_profile=current_profile,
                         forced_industry=forced_industry)

