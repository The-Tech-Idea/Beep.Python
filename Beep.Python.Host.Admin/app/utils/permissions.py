"""
Permission Decorators for Route Protection

Provides decorators to protect routes with permission checks.
"""
from functools import wraps
from flask import session, redirect, url_for, flash, jsonify, request
from app.models.core import User
from app.services.auth_service import AuthService


def login_required(f):
    """Require user to be logged in"""
    @wraps(f)
    def decorated_function(*args, **kwargs):
        if 'user_id' not in session:
            if request.is_json:
                return jsonify({'error': 'Authentication required'}), 401
            flash('Please log in to access this page.', 'warning')
            return redirect(url_for('auth.login'))
        return f(*args, **kwargs)
    return decorated_function


def permission_required(permission):
    """
    Require user to have specific permission
    
    Usage:
        @permission_required('llm:view')
        def view_models():
            ...
    """
    def decorator(f):
        @wraps(f)
        def decorated_function(*args, **kwargs):
            if 'user_id' not in session:
                if request.is_json:
                    return jsonify({'error': 'Authentication required'}), 401
                flash('Please log in to access this page.', 'warning')
                return redirect(url_for('auth.login'))
            
            user = User.query.get(session['user_id'])
            if not user or not user.is_active:
                if request.is_json:
                    return jsonify({'error': 'User not active'}), 403
                flash('Your account is not active.', 'danger')
                return redirect(url_for('auth.login'))
            
            if not AuthService.check_permission(user, permission):
                if request.is_json:
                    return jsonify({'error': f'Permission denied: {permission}'}), 403
                flash(f'You do not have permission to access this resource.', 'danger')
                return redirect(url_for('main.index'))
            
            return f(*args, **kwargs)
        return decorated_function
    return decorator


def any_permission_required(*permissions):
    """
    Require user to have any of the specified permissions
    
    Usage:
        @any_permission_required('llm:view', 'llm:load')
        def view_or_load_models():
            ...
    """
    def decorator(f):
        @wraps(f)
        def decorated_function(*args, **kwargs):
            if 'user_id' not in session:
                if request.is_json:
                    return jsonify({'error': 'Authentication required'}), 401
                return redirect(url_for('auth.login'))
            
            user = User.query.get(session['user_id'])
            if not user or not user.is_active:
                if request.is_json:
                    return jsonify({'error': 'User not active'}), 403
                return redirect(url_for('auth.login'))
            
            if not AuthService.check_any_permission(user, list(permissions)):
                if request.is_json:
                    return jsonify({'error': 'Permission denied'}), 403
                flash('You do not have permission to access this resource.', 'danger')
                return redirect(url_for('main.index'))
            
            return f(*args, **kwargs)
        return decorated_function
    return decorator


def admin_required(f):
    """Require user to be admin"""
    @wraps(f)
    def decorated_function(*args, **kwargs):
        if 'user_id' not in session:
            if request.is_json:
                return jsonify({'error': 'Authentication required'}), 401
            return redirect(url_for('auth.login'))
        
        user = User.query.get(session['user_id'])
        if not user or not user.is_active or not user.is_admin:
            if request.is_json:
                return jsonify({'error': 'Admin access required'}), 403
            flash('Admin access required.', 'danger')
            return redirect(url_for('main.index'))
        
        return f(*args, **kwargs)
    return decorated_function


def get_current_user():
    """Get current logged-in user"""
    if 'user_id' in session:
        return User.query.get(session['user_id'])
    return None
