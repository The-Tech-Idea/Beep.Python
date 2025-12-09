"""
Packages Routes - Package search and management
"""
from flask import Blueprint, render_template, request, jsonify
import httpx

packages_bp = Blueprint('packages', __name__)


@packages_bp.route('/')
def index():
    """Package search page"""
    return render_template('packages/index.html')


@packages_bp.route('/search')
def search():
    """Search PyPI for packages"""
    query = request.args.get('q', '')
    
    if not query:
        return render_template('packages/search_results.html', packages=[], query='')
    
    try:
        # Search PyPI
        response = httpx.get(
            f'https://pypi.org/search/',
            params={'q': query},
            headers={'Accept': 'application/json'},
            timeout=10
        )
        
        # PyPI doesn't have a JSON search API, so we'll use the simple API
        # For now, return a simple search using the JSON API for specific packages
        packages = []
        
        # Try to get package info directly
        try:
            pkg_response = httpx.get(
                f'https://pypi.org/pypi/{query}/json',
                timeout=10
            )
            if pkg_response.status_code == 200:
                data = pkg_response.json()
                packages.append({
                    'name': data['info']['name'],
                    'version': data['info']['version'],
                    'summary': data['info']['summary'],
                    'author': data['info']['author'],
                    'homepage': data['info']['home_page']
                })
        except:
            pass
        
        return render_template('packages/search_results.html', 
                              packages=packages, 
                              query=query)
    except Exception as e:
        return render_template('packages/search_results.html', 
                              packages=[], 
                              query=query,
                              error=str(e))


@packages_bp.route('/info/<package_name>')
def info(package_name):
    """Get package details from PyPI"""
    try:
        response = httpx.get(
            f'https://pypi.org/pypi/{package_name}/json',
            timeout=10
        )
        
        if response.status_code == 200:
            data = response.json()
            package = {
                'name': data['info']['name'],
                'version': data['info']['version'],
                'summary': data['info']['summary'],
                'description': data['info']['description'],
                'author': data['info']['author'],
                'author_email': data['info']['author_email'],
                'license': data['info']['license'],
                'homepage': data['info']['home_page'],
                'project_url': data['info']['project_url'],
                'requires_python': data['info']['requires_python'],
                'requires_dist': data['info']['requires_dist'] or [],
                'releases': list(data['releases'].keys())[-10:]  # Last 10 versions
            }
            return render_template('packages/info.html', package=package)
        else:
            return render_template('packages/not_found.html', package_name=package_name)
    except Exception as e:
        return render_template('packages/error.html', error=str(e))
