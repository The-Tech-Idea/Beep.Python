"""
Dashboard Routes
"""
from flask import Blueprint, render_template, session, redirect, url_for, current_app
from app import db
from app.models.project import MLProject
from app.industry_profiles import profile_manager
from app.services.community_connection_service import get_community_connection_service
from app.services.auth_service import AuthService
import logging

logger = logging.getLogger(__name__)

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
    
    # Get Community connection status and user participation data
    community_service = get_community_connection_service()
    community_config = community_service.get_connection_config()
    community_data = None
    
    if community_config.get('connected'):
        # Get current user
        current_user = AuthService.get_current_user()
        if current_user:
            try:
                client = community_service.get_client()
                if client:
                    # Get user participation data from Community
                    # Note: This assumes the user has the same ID in Community
                    # In a real scenario, we might need to map ML Studio user to Community user
                    user_competitions_resp = client.get_user_competitions(current_user.id)
                    user_rankings_resp = client.get_user_rankings(current_user.id)
                    user_submissions_resp = client.get_user_submissions(current_user.id)
                    user_activity_resp = client.get_user_activity(current_user.id, limit=10)
                    user_stats_resp = client.get_user_stats(current_user.id)
                    
                    # Extract data if successful
                    competitions_list = user_competitions_resp.get('data', []) if user_competitions_resp.get('success') else []
                    rankings_list = user_rankings_resp.get('data', []) if user_rankings_resp.get('success') else []
                    submissions_list = user_submissions_resp.get('data', []) if user_submissions_resp.get('success') else []
                    activity_list = user_activity_resp.get('data', []) if user_activity_resp.get('success') else []
                    stats_dict = user_stats_resp.get('data', {}) if user_stats_resp.get('success') else {}
                    
                    # Create a mapping of competition_id -> ranking data
                    rankings_map = {r.get('competition_id'): r for r in rankings_list}
                    
                    # Create a mapping of competition_id -> submissions list
                    submissions_map = {}
                    for sub in submissions_list:
                        comp_id = sub.get('competition_id')
                        if comp_id not in submissions_map:
                            submissions_map[comp_id] = []
                        submissions_map[comp_id].append(sub)
                    
                    # Enrich competitions with detailed data, rankings, and submissions
                    enriched_competitions = []
                    for comp in competitions_list:
                        comp_id = comp.get('id')
                        
                        # Try to fetch detailed competition info
                        try:
                            comp_detail_resp = client.get_competition_detail(comp_id)
                            if comp_detail_resp.get('success'):
                                # Merge detailed info
                                comp.update(comp_detail_resp)
                        except Exception as e:
                            logger.warning(f"Could not fetch details for competition {comp_id}: {e}")
                        
                        # Add ranking data if available
                        if comp_id in rankings_map:
                            comp['user_ranking'] = rankings_map[comp_id]
                            comp['rank'] = rankings_map[comp_id].get('rank')
                            comp['best_score'] = rankings_map[comp_id].get('score')
                        
                        # Add submissions for this competition
                        comp['submissions'] = submissions_map.get(comp_id, [])
                        comp['submission_count'] = len(comp['submissions'])
                        
                        # Find best submission score
                        if comp['submissions']:
                            scores = [s.get('score') for s in comp['submissions'] if s.get('score') is not None]
                            if scores:
                                comp['best_submission_score'] = max(scores)
                            else:
                                comp['best_submission_score'] = None
                        else:
                            comp['best_submission_score'] = None
                        
                        enriched_competitions.append(comp)
                    
                    # Get community URL for links
                    community_url = community_config.get('url', 'http://127.0.0.1:5001')
                    
                    community_data = {
                        'competitions': enriched_competitions,
                        'rankings': rankings_list,
                        'submissions': submissions_list,
                        'activity': activity_list,
                        'stats': stats_dict,
                        'community_url': community_url
                    }
            except Exception as e:
                logger.error(f"Error fetching Community data: {e}", exc_info=True)
                community_data = None
    
    return render_template('dashboard/index.html', 
                         projects=projects,
                         profiles=profiles,
                         current_mode=current_mode,
                         current_profile=current_profile,
                         forced_industry=forced_industry,
                         community_config=community_config,
                         community_data=community_data)

