"""
Dashboard Routes
"""
from flask import Blueprint, render_template, session
from app import db
from app.models.project import MLProject
from app.industry_profiles import profile_manager

dashboard_bp = Blueprint('dashboard', __name__)


@dashboard_bp.route('/')
def index():
    """Main dashboard with industry mode selection"""
    projects = MLProject.query.filter_by(status='active').order_by(MLProject.created_at.desc()).all()
    
    # Get industry profiles for the mode selector
    profiles = profile_manager.list_all()
    current_mode = session.get('industry_mode', 'advanced')
    current_profile = profile_manager.get(current_mode)
    
    return render_template('dashboard/index.html', 
                         projects=projects,
                         profiles=profiles,
                         current_mode=current_mode,
                         current_profile=current_profile)

