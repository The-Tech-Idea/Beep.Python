"""
Projects Routes
"""
import logging
from flask import Blueprint, render_template, request, jsonify, flash, redirect, url_for
from app import db, socketio
from app.models.project import MLProject
from app.models.experiment import Experiment
from app.services.environment_manager import EnvironmentManager
from app.services.ml_service import MLService
from app.services.data_service import DataService
from flask import current_app

from app.utils.constants import HTTP_BAD_REQUEST, HTTP_INTERNAL_SERVER_ERROR
from app.utils.request_validators import error_handler, sanitize_string_input
from app.exceptions.database_exceptions import DatabaseError

logger = logging.getLogger(__name__)

projects_bp = Blueprint('projects', __name__)


def get_environment_manager():
    """Get environment manager instance"""
    return EnvironmentManager()


def get_ml_service():
    """Get ML service instance"""
    from app.services.environment_manager import EnvironmentManager
    env_mgr = EnvironmentManager()
    return MLService(
        projects_folder=current_app.config['PROJECTS_FOLDER'],
        environment_manager=env_mgr
    )


def get_data_service():
    """Get data service instance"""
    # DataService now uses settings by default, but we can still pass config if needed
    return DataService(upload_folder=current_app.config.get('UPLOAD_FOLDER'))


@projects_bp.route('/')
def index():
    """List all projects"""
    projects = MLProject.query.filter_by(status='active').order_by(MLProject.created_at.desc()).all()
    
    # Get Community connection status
    community_config = None
    try:
        from app.services.community_connection_service import CommunityConnectionService
        connection_service = CommunityConnectionService()
        community_config = {
            'url': connection_service.get_community_url(),
            'connected': connection_service.is_connected()
        }
    except:
        pass
    
    return render_template('projects/index.html', projects=projects, community_config=community_config)


@projects_bp.route('/<int:project_id>')
def detail(project_id):
    """Project detail view"""
    project = MLProject.query.get_or_404(project_id)
    experiments = Experiment.query.filter_by(project_id=project_id).order_by(Experiment.created_at.desc()).all()
    
    ml_service = get_ml_service()
    models = ml_service.list_models(project_id)
    
    return render_template('projects/detail.html', 
                         project=project, 
                         experiments=experiments,
                         models=models)


@projects_bp.route('/create', methods=['GET', 'POST'])
@error_handler
def create():
    """Create new project"""
    if request.method == 'POST':
        name = request.form.get('name', '').strip()
        description = request.form.get('description', '').strip()
        template = request.form.get('template', 'custom')
        env_option = request.form.get('env_option', 'new')  # 'new' or 'existing'
        existing_env_name = request.form.get('existing_env_name', '').strip()
        framework = request.form.get('framework', 'scikit-learn')
        
        if not name:
            flash('Project name is required', 'error')
            return render_template('projects/create.html')
        
        # Check if name already exists
        if MLProject.query.filter_by(name=name).first():
            flash('Project name already exists', 'error')
            return render_template('projects/create.html')
        
        env_mgr = get_environment_manager()
        
        # Verify embedded Python is available
        embedded_python = env_mgr.get_embedded_python()
        if not embedded_python:
            flash('Embedded Python is required but not found. Please set up embedded Python first.', 'error')
            return render_template('projects/create.html')
        
        try:
            # Handle environment selection/creation
            if env_option == 'existing':
                # Use existing environment
                if not existing_env_name:
                    flash('Please select an existing environment', 'error')
                    return render_template('projects/create.html')
                
                # Verify environment exists
                env_info = env_mgr.get_environment(existing_env_name)
                if not env_info:
                    flash(f'Selected environment "{existing_env_name}" not found', 'error')
                    return render_template('projects/create.html')
                
                # Check if environment is already linked to another project
                existing_project = MLProject.query.filter_by(environment_name=existing_env_name).first()
                if existing_project:
                    flash(f'Environment "{existing_env_name}" is already linked to project "{existing_project.name}"', 'error')
                    return render_template('projects/create.html')
                
                env_name = existing_env_name
                # Framework is optional when using existing environment
                
            else:
                # Create new environment
                env_name = f"mlstudio_{name.lower().replace(' ', '_')}"
                packages = _get_framework_packages(framework)
                
                # Create the virtual environment using EnvironmentManager
                try:
                    env_info = env_mgr.create_environment(env_name, packages=packages)
                    
                    # Check if core packages installed successfully (batched installation)
                    if isinstance(env_info, dict) and 'install_result' in env_info:
                        install_result = env_info['install_result']
                        if 'batches' in install_result:
                            core_batch = next((b for b in install_result['batches'] if b['batch'] == 'core'), None)
                            if core_batch and not core_batch['success']:
                                flash(f'Warning: Core packages installation had issues. Project created but some packages may be missing.', 'warning')
                                logger.warning(f"Core packages installation issue: {core_batch.get('stderr', 'Unknown')}")
                            # Check for failed heavy packages (non-critical)
                            failed_heavy = [b for b in install_result['batches'] if b['batch'] != 'core' and not b['success']]
                            if failed_heavy:
                                failed_names = [b['batch'] for b in failed_heavy]
                                flash(f'Note: Some optional packages failed to install: {", ".join(failed_names)}. You can install them later if needed.', 'info')
                        elif not install_result.get('success', False):
                            flash(f'Warning: Some packages may have failed to install. Project created but you may need to install packages manually.', 'warning')
                except RuntimeError as e:
                    error_msg = str(e)
                    if 'not found' in error_msg.lower():
                        flash(f'Failed to create environment: Embedded Python not found. Please set up embedded Python.', 'error')
                    else:
                        flash(f'Failed to create environment: {error_msg}', 'error')
                    return render_template('projects/create.html')
                except ValueError as e:
                    flash(f'Environment already exists: {str(e)}', 'error')
                    return render_template('projects/create.html')
                except Exception as e:
                    flash(f'Failed to create environment: {str(e)}', 'error')
                    import traceback
                    logger.error(f"Environment creation error: {traceback.format_exc()}")
                    return render_template('projects/create.html')
            
            # Create project in database - LINKED to environment
            project = MLProject(
                name=name,
                description=description,
                template=template,
                environment_name=env_name,  # CRITICAL: Link project to environment
                framework=framework if env_option == 'new' else None
            )
            db.session.add(project)
            db.session.commit()
            
            # Verify the link
            env_valid, env_msg = project.validate_environment_link()
            if not env_valid:
                db.session.rollback()
                flash(f'Failed to link project to environment: {env_msg}', 'error')
                return render_template('projects/create.html')
            
            # Create project directory structure
            ml_service = get_ml_service()
            ml_service.create_project_structure(project.id, project.name)
            
            flash(f'Project "{name}" created successfully and linked to environment "{env_name}"!', 'success')
            return redirect(url_for('projects.detail', project_id=project.id))
            
        except Exception as e:
            db.session.rollback()
            import traceback
            logger.error(f"Project creation error: {traceback.format_exc()}")
            flash(f'Failed to create project: {str(e)}', 'error')
            return render_template('projects/create.html')
    
    return render_template('projects/create.html')


def _get_framework_packages(framework: str) -> list:
    """Get default packages for a framework"""
    # Core data management libraries - ALWAYS included
    base_packages = [
        'numpy',           # Numerical computing
        'pandas',          # Data manipulation and analysis
        'matplotlib',      # Plotting
        'seaborn',         # Statistical visualization
        'scikit-learn',    # Machine learning
        'openpyxl',        # Excel file support for pandas
        'xlrd',            # Excel file reading
        'pyarrow',         # Fast data interchange (for parquet, etc.)
    ]
    
    # Optional but recommended data management libraries
    recommended_packages = [
        'polars',          # Fast DataFrame library (alternative to pandas)
        'dask',            # Parallel computing for larger datasets
        'pyyaml',          # YAML file support
        'lxml',            # XML/HTML parsing
        'html5lib',        # HTML parsing
        'beautifulsoup4',  # Web scraping and HTML parsing
        'requests',        # HTTP library for web scraping (used with BeautifulSoup)
        # Time series
        'statsmodels',     # Time series analysis (ARIMA, seasonal decomposition)
        'pmdarima',        # Auto ARIMA
        # Deep learning
        'tensorflow',      # TensorFlow/Keras (optional, can be added per project)
        # Data validation
        'great-expectations',  # Data validation framework
        # Text processing and NLP
        'textblob',            # Text processing and sentiment analysis
        'vaderSentiment',      # Sentiment analysis (alternative to TextBlob)
        # Statistical and scientific computing
        'scipy',               # Scientific computing (for outlier detection, stats)
        # Database
        'sqlalchemy',      # SQL toolkit and ORM
        'pymysql',         # MySQL connector
        'psycopg2-binary', # PostgreSQL connector
        'pymongo',         # MongoDB connector
        'redis',           # Redis connector
        # Cloud Storage
        'boto3',           # AWS S3
        'google-cloud-storage',  # Google Cloud Storage
        'azure-storage-blob',    # Azure Blob Storage
        'gspread',         # Google Sheets
        'oauth2client',    # Google API authentication
        'google-cloud-bigquery', # BigQuery
        # File Formats
        'pyarrow',         # Already included, but needed for Parquet/Arrow
        # Dimensionality Reduction
        'umap-learn',      # UMAP for dimensionality reduction
        'fastparquet',     # Alternative Parquet engine
        'pyreadstat',      # SPSS, SAS, Stata files
        'fastavro',        # Avro file format
        'Pillow',          # Image processing (PIL)
        # NLP
        'nltk',            # Natural language processing
        'gensim',           # Topic modeling and word embeddings
        'transformers',     # Hugging Face transformers (optional, large)
    ]
    
    framework_packages = {
        'scikit-learn': base_packages,
        'tensorflow': base_packages + ['tensorflow'],
        'pytorch': base_packages + ['torch', 'torchvision'],
        'xgboost': base_packages + ['xgboost'],
        'lightgbm': base_packages + ['lightgbm'],
        'custom': base_packages
    }
    
    # Get framework-specific packages
    selected_packages = framework_packages.get(framework, base_packages)
    
    # Only include essential recommended packages initially
    # Heavy packages (tensorflow, transformers) should be installed on-demand
    essential_recommended = [
        'polars', 'dask', 'pyyaml', 'lxml', 'html5lib', 'beautifulsoup4', 'requests',
        'statsmodels', 'pmdarima', 'scipy', 'sqlalchemy', 'pyarrow', 'Pillow'
    ]
    
    # Add framework-specific heavy packages only if explicitly selected
    if framework == 'tensorflow':
        essential_recommended.append('tensorflow')
    elif framework == 'pytorch':
        essential_recommended.extend(['torch', 'torchvision'])
    
    # Skip heavy optional packages for initial install:
    # - great-expectations (can be installed later if needed)
    # - textblob, vaderSentiment (install on-demand for NLP projects)
    # - transformers (very large, install on-demand)
    # - nltk, gensim (install on-demand for NLP)
    # - Database connectors (install on-demand when needed)
    # - Cloud storage (install on-demand when needed)
    
    return selected_packages + essential_recommended


@projects_bp.route('/<int:project_id>/delete', methods=['POST'])
def delete(project_id):
    """Delete project (keeps virtual environment for reuse)"""
    project = MLProject.query.get_or_404(project_id)
    env_name = project.environment_name
    
    # Note: Virtual environment is NOT deleted - can be reused or manually deleted from Settings
    
    # Explicitly delete related records first to avoid foreign key issues
    try:
        from app.models.industry_scenario import IndustryScenarioProgress
        IndustryScenarioProgress.query.filter_by(project_id=project_id).delete()
    except Exception:
        pass  # Table might not exist yet
    
    try:
        from app.models.workflow import Workflow
        Workflow.query.filter_by(project_id=project_id).delete()
    except Exception:
        pass  # Table might not exist yet
    
    # Delete project (cascade will delete experiments)
    db.session.delete(project)
    db.session.commit()
    
    flash(f'Project deleted successfully! Virtual environment "{env_name}" was kept and can be reused or deleted from Settings.', 'success')
    return redirect(url_for('projects.index'))


@projects_bp.route('/<int:project_id>/upload', methods=['POST'])
def upload_dataset(project_id):
    """Upload dataset to project"""
    project = MLProject.query.get_or_404(project_id)
    
    if 'file' not in request.files:
        flash('No file provided', 'error')
        return redirect(url_for('projects.detail', project_id=project_id))
    
    file = request.files['file']
    if file.filename == '':
        flash('No file selected', 'error')
        return redirect(url_for('projects.detail', project_id=project_id))
    
    data_service = get_data_service()
    
    if not data_service.is_allowed_file(file.filename):
        flash('Invalid file type. Allowed: CSV, JSON, Excel', 'error')
        return redirect(url_for('projects.detail', project_id=project_id))
    
    try:
        file_path = data_service.save_uploaded_file(file, project_id)
        flash(f'Dataset uploaded successfully: {file.filename}', 'success')
    except Exception as e:
        flash(f'Upload failed: {str(e)}', 'error')
    
    return redirect(url_for('projects.detail', project_id=project_id))

