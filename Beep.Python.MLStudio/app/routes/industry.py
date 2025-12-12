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
    profiles = profile_manager.list_all()
    current_mode = session.get('industry_mode', 'advanced')
    return render_template('industry/select_mode.html', 
                         profiles=profiles, 
                         current_mode=current_mode)


@industry_bp.route('/mode/<profile_id>', methods=['POST'])
def set_mode(profile_id):
    """Set the current industry mode"""
    profile = profile_manager.get(profile_id)
    if not profile:
        flash('Invalid industry profile', 'error')
        return redirect(url_for('industry.select_mode'))
    
    session['industry_mode'] = profile_id
    flash(f'Switched to {profile.name} mode', 'success')
    
    # Redirect to appropriate dashboard
    if profile_id == 'advanced':
        return redirect(url_for('dashboard.index'))
    else:
        return redirect(url_for('industry.dashboard', profile_id=profile_id))


@industry_bp.route('/dashboard/<profile_id>')
def dashboard(profile_id):
    """Display industry-specific dashboard"""
    profile = profile_manager.get(profile_id)
    if not profile:
        flash('Invalid industry profile', 'error')
        return redirect(url_for('industry.select_mode'))
    
    # Store current mode in session
    session['industry_mode'] = profile_id
    
    # Get projects for this user (all projects for now)
    projects = Project.query.order_by(Project.created_at.desc()).all()
    
    return render_template('industry/dashboard.html',
                         profile=profile,
                         projects=projects,
                         scenarios=profile.scenarios)


@industry_bp.route('/scenario/<profile_id>/<scenario_id>')
def scenario_wizard(profile_id, scenario_id):
    """Display scenario wizard for a specific use case"""
    profile = profile_manager.get(profile_id)
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
        return redirect(url_for('industry.dashboard', profile_id=profile_id))
    
    return render_template('industry/scenario_wizard.html',
                         profile=profile,
                         scenario=scenario)


@industry_bp.route('/scenario/<profile_id>/<scenario_id>/start', methods=['POST'])
def start_scenario(profile_id, scenario_id):
    """Start a new project based on a scenario"""
    profile = profile_manager.get(profile_id)
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
        industry_profile=profile_id,
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
        'industry_profile': profile_id,
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

