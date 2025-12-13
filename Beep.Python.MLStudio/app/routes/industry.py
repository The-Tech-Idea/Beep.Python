"""
Industry Mode Routes
Handles industry profile selection and industry-specific dashboards
"""
import json
import logging
from flask import Blueprint, render_template, redirect, url_for, request, session, jsonify, flash
from app.industry_profiles import profile_manager
from app.models.project import MLProject as Project
from app.models.industry_scenario import IndustryScenarioProgress
from app import db

logger = logging.getLogger(__name__)

industry_bp = Blueprint('industry', __name__)


@industry_bp.route('/mode')
def select_mode():
    """Display mode selection page"""
    from flask import current_app
    
    # If forced industry mode is set, redirect to that industry dashboard
    forced_industry = current_app.config.get('FORCED_INDUSTRY_MODE')
    if forced_industry:
        profile = profile_manager.get(forced_industry)
        if profile:
            session['industry_mode'] = forced_industry
            session['forced_industry'] = True
            return redirect(url_for('industry.dashboard', profile_id=forced_industry))
    
    profiles = profile_manager.list_all()
    current_mode = session.get('industry_mode', 'advanced')
    return render_template('industry/select_mode.html', 
                         profiles=profiles, 
                         current_mode=current_mode)


@industry_bp.route('/mode/<profile_id>', methods=['POST'])
def set_mode(profile_id):
    """Set the current industry mode"""
    from flask import current_app
    
    # If forced industry mode is set, prevent switching
    forced_industry = current_app.config.get('FORCED_INDUSTRY_MODE')
    if forced_industry:
        if profile_id != forced_industry:
            flash(f'Industry mode is locked to {forced_industry.title()}. Cannot switch.', 'warning')
            return redirect(url_for('industry.dashboard', profile_id=forced_industry))
    
    profile = profile_manager.get(profile_id)
    if not profile:
        flash('Invalid industry profile', 'error')
        return redirect(url_for('industry.select_mode'))
    
    session['industry_mode'] = profile_id
    
    # Redirect to appropriate dashboard
    if profile_id == 'advanced':
        return redirect(url_for('dashboard.index'))
    else:
        return redirect(url_for('industry.dashboard', profile_id=profile_id))


@industry_bp.route('/dashboard/<profile_id>')
def dashboard(profile_id):
    """Display industry-specific dashboard"""
    from flask import current_app
    
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
    resolved_id = industry_aliases.get(profile_id.lower(), profile_id.lower())
    
    profile = profile_manager.get(resolved_id)
    if not profile:
        flash('Invalid industry profile', 'error')
        return redirect(url_for('industry.select_mode'))
    
    # Check if forced industry mode is set
    forced_industry = current_app.config.get('FORCED_INDUSTRY_MODE')
    
    # If forced mode is set, resolve it and check
    if forced_industry:
        forced_resolved = industry_aliases.get(forced_industry.lower(), forced_industry.lower())
        if forced_resolved != resolved_id:
            forced_profile = profile_manager.get(forced_resolved)
            if forced_profile:
                session['industry_mode'] = forced_resolved
                session['forced_industry'] = True
                return redirect(url_for('industry.dashboard', profile_id=forced_resolved))
    
    # Store current mode in session (use resolved_id)
    session['industry_mode'] = resolved_id
    if forced_industry:
        session['forced_industry'] = True
    
    # Get projects filtered by industry profile
    # Show projects that match this industry, or projects with no industry assigned (advanced mode)
    if resolved_id == 'advanced':
        # Advanced mode shows all projects
        projects = Project.query.filter_by(status='active').order_by(Project.created_at.desc()).all()
    else:
        # Industry mode shows only projects for this industry
        projects = Project.query.filter(
            Project.status == 'active',
            Project.industry_profile == resolved_id
        ).order_by(Project.created_at.desc()).all()
    
    # Get all profiles for display (will be filtered in template if forced)
    all_profiles = profile_manager.list_all()
    
    return render_template('industry/dashboard.html',
                         profile=profile,
                         projects=projects,
                         scenarios=profile.scenarios,
                         all_profiles=all_profiles,
                         forced_industry=forced_industry)


@industry_bp.route('/scenario/<profile_id>/<scenario_id>')
def scenario_wizard(profile_id, scenario_id):
    """Display scenario wizard for a specific use case"""
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
    resolved_id = industry_aliases.get(profile_id.lower(), profile_id.lower())
    
    profile = profile_manager.get(resolved_id)
    if not profile:
        flash('Invalid industry profile', 'error')
        return redirect(url_for('industry.select_mode'))
    
    # Find the scenario
    scenario = None
    for s in profile.scenarios:
        if s.id == scenario_id:
            scenario = s
            break
    
    if not scenario:
        flash('Scenario not found', 'error')
        return redirect(url_for('industry.dashboard', profile_id=resolved_id))
    
    return render_template('industry/scenario_wizard.html',
                         profile=profile,
                         scenario=scenario)


@industry_bp.route('/scenario/<profile_id>/<scenario_id>/start', methods=['POST'])
def start_scenario(profile_id, scenario_id):
    """Start a new project based on a scenario"""
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
    resolved_id = industry_aliases.get(profile_id.lower(), profile_id.lower())
    
    profile = profile_manager.get(resolved_id)
    if not profile:
        return jsonify({'success': False, 'error': 'Invalid industry profile'})
    
    # Find the scenario
    scenario = None
    for s in profile.scenarios:
        if s.id == scenario_id:
            scenario = s
            break
    
    if not scenario:
        return jsonify({'success': False, 'error': 'Scenario not found'})
    
    # Create project name from scenario
    from datetime import datetime
    import re
    timestamp = datetime.now().strftime('%Y%m%d_%H%M%S')
    project_name = f"{scenario.name} - {timestamp}"
    
    # Generate environment name (sanitized)
    env_name = re.sub(r'[^a-zA-Z0-9_]', '_', scenario.name.lower())
    env_name = f"mlstudio_{env_name}_{timestamp}"
    
    # Prepare industry config as JSON
    industry_config = json.dumps({
        'terminology': profile.terminology,
        'scenario_name': scenario.name,
        'recommended_algorithms': scenario.recommended_algorithms if hasattr(scenario, 'recommended_algorithms') else []
    })
    
    # Create the project with industry info stored in database
    project = Project(
        name=project_name,
        description=f"Created from {profile.name} scenario: {scenario.name}",
        framework='scikit-learn',
        environment_name=env_name,
        industry_profile=resolved_id,  # Use resolved_id to ensure correct industry
        scenario_id=scenario_id,
        industry_config=industry_config
    )
    
    db.session.add(project)
    db.session.commit()
    
    # Create scenario progress tracking record
    try:
        progress = IndustryScenarioProgress(
            project_id=project.id,
            scenario_id=scenario_id,
            current_step=1,
            status='in_progress'
        )
        db.session.add(progress)
        db.session.commit()
    except Exception as e:
        logger.warning(f"Failed to create scenario progress record: {e}")
    
    # Also store in session for quick access
    if 'project_industry_config' not in session:
        session['project_industry_config'] = {}
    session['project_industry_config'][str(project.id)] = {
        'industry_profile': resolved_id,  # Use resolved_id
        'scenario': scenario_id,
        'terminology': profile.terminology
    }
    session.modified = True
    
    # Create project structure
    try:
        from flask import current_app
        from app.services.ml_service import MLService
        from app.services.environment_manager import EnvironmentManager
        env_mgr = EnvironmentManager()
        ml_service = MLService(
            projects_folder=current_app.config['PROJECTS_FOLDER'],
            environment_manager=env_mgr
        )
        ml_service.create_project_structure(project.id, project.name)
    except Exception as e:
        import logging
        logging.getLogger(__name__).warning(f"Failed to create project structure: {e}")
    
    return jsonify({
        'success': True,
        'project_id': project.id,
        'redirect_url': url_for('industry.scenario_step', 
                               project_id=project.id, 
                               step=1)
    })


@industry_bp.route('/project/<int:project_id>/step/<int:step>')
def scenario_step(project_id, step):
    """Display a specific step in the scenario wizard"""
    project = Project.query.get_or_404(project_id)
    
    # Get industry profile and scenario - first from session, then from database
    project_industry_config = session.get('project_industry_config', {})
    extra_data = project_industry_config.get(str(project_id), {})
    profile_id = extra_data.get('industry_profile')
    scenario_id = extra_data.get('scenario')
    
    # Fall back to database if not in session
    if not profile_id and project.industry_profile:
        profile_id = project.industry_profile
        scenario_id = project.scenario_id
        # Rebuild session cache from database
        if profile_id:
            profile_obj = profile_manager.get(profile_id)
            if profile_obj and 'project_industry_config' not in session:
                session['project_industry_config'] = {}
            if profile_obj:
                session['project_industry_config'][str(project_id)] = {
                    'industry_profile': profile_id,
                    'scenario': scenario_id,
                    'terminology': profile_obj.terminology
                }
                session.modified = True
    
    if not profile_id:
        profile_id = 'advanced'
    
    profile = profile_manager.get(profile_id)
    if not profile:
        flash('Invalid industry profile', 'error')
        return redirect(url_for('projects.detail', project_id=project_id))
    
    # Find scenario
    scenario = None
    for s in profile.scenarios:
        if s.id == scenario_id:
            scenario = s
            break
    
    if not scenario:
        flash('Scenario not found', 'error')
        return redirect(url_for('projects.detail', project_id=project_id))
    
    # Validate step
    if step < 1 or step > len(scenario.steps):
        flash('Invalid step', 'error')
        return redirect(url_for('projects.detail', project_id=project_id))
    
    current_step = scenario.steps[step - 1]
    
    # Get or create progress tracking
    progress = IndustryScenarioProgress.query.filter_by(
        project_id=project_id,
        scenario_id=scenario_id
    ).first()
    
    if progress:
        # Update current step
        progress.current_step = step
        db.session.commit()
    
    return render_template('industry/scenario_step.html',
                         profile=profile,
                         scenario=scenario,
                         project=project,
                         step=step,
                         current_step=current_step,
                         total_steps=len(scenario.steps),
                         progress=progress)


# API endpoints for AJAX
@industry_bp.route('/api/profiles')
def api_list_profiles():
    """API: List all industry profiles"""
    profiles = profile_manager.list_all()
    return jsonify({
        'success': True,
        'profiles': [p.to_dict() for p in profiles]
    })


@industry_bp.route('/api/profile/<profile_id>')
def api_get_profile(profile_id):
    """API: Get a specific profile"""
    profile = profile_manager.get(profile_id)
    if not profile:
        return jsonify({'success': False, 'error': 'Profile not found'}), 404
    
    return jsonify({
        'success': True,
        'profile': profile.to_dict()
    })


@industry_bp.route('/api/scenarios/<profile_id>')
def api_list_scenarios(profile_id):
    """API: List scenarios for a profile"""
    scenarios = profile_manager.get_scenarios(profile_id)
    return jsonify({
        'success': True,
        'scenarios': [s.to_dict() for s in scenarios]
    })


@industry_bp.route('/sample-data/<filename>')
def download_sample_data(filename: str):
    """Download sample data file for a scenario"""
    from flask import send_from_directory, abort
    from pathlib import Path
    
    # Security: Only allow CSV files from samples directory
    if not filename.endswith('.csv'):
        abort(400, 'Only CSV files are allowed')
    
    # Prevent directory traversal
    if '..' in filename or '/' in filename or '\\' in filename:
        abort(400, 'Invalid filename')
    
    samples_dir = Path(__file__).parent.parent.parent / 'static' / 'data' / 'samples'
    file_path = samples_dir / filename
    
    if not file_path.exists() or not file_path.is_file():
        abort(404, 'Sample data file not found')
    
    return send_from_directory(str(samples_dir), filename, as_attachment=True)

