"""
Admin Routes for User and Group Management
"""
from flask import Blueprint, render_template, request, jsonify, flash, redirect, url_for
from app.database import db
from app.models.core import User, Role, Group
from app.services.auth_service import AuthService, Permissions
from app.utils.permissions import admin_required, get_current_user
from werkzeug.security import generate_password_hash

admin_bp = Blueprint('admin', __name__, url_prefix='/admin')


# =====================
# User Management
# =====================

@admin_bp.route('/users')
@admin_required
def users():
    """User management page"""
    users = User.query.all()
    roles = Role.query.all()
    groups = Group.query.all()
    
    return render_template('admin/users.html',
                          users=users,
                          roles=roles,
                          groups=groups,
                          current_user=get_current_user())


@admin_bp.route('/api/users', methods=['GET'])
@admin_required
def api_list_users():
    """Get list of all users"""
    users = User.query.all()
    return jsonify([u.to_dict() for u in users])


@admin_bp.route('/api/users', methods=['POST'])
@admin_required
def api_create_user():
    """Create a new user"""
    data = request.get_json()
    
    # Validate required fields
    if not data.get('username'):
        return jsonify({'error': 'Username is required'}), 400
    
    # Check if username exists
    if User.query.filter_by(username=data['username']).first():
        return jsonify({'error': 'Username already exists'}), 400
    
    # Check if email exists
    if data.get('email') and User.query.filter_by(email=data['email']).first():
        return jsonify({'error': 'Email already exists'}), 400
    
    try:
        user = User(
            username=data['username'],
            email=data.get('email'),
            display_name=data.get('display_name'),
            is_admin=data.get('is_admin', False),
            is_active=data.get('is_active', True),
            role_id=data.get('role_id')
        )
        
        # Set password if provided
        if data.get('password'):
            user.password_hash = generate_password_hash(data['password'])
        
        db.session.add(user)
        db.session.commit()
        
        return jsonify(user.to_dict()), 201
    except Exception as e:
        db.session.rollback()
        return jsonify({'error': str(e)}), 500


@admin_bp.route('/api/users/<int:user_id>', methods=['PUT'])
@admin_required
def api_update_user(user_id: int):
    """Update a user"""
    user = User.query.get_or_404(user_id)
    data = request.get_json()
    
    try:
        # Update fields
        if 'email' in data:
            user.email = data['email']
        if 'display_name' in data:
            user.display_name = data['display_name']
        if 'is_admin' in data:
            user.is_admin = data['is_admin']
        if 'is_active' in data:
            user.is_active = data['is_active']
        if 'role_id' in data:
            user.role_id = data['role_id']
        
        # Update password if provided
        if data.get('password'):
            user.password_hash = generate_password_hash(data['password'])
        
        db.session.commit()
        return jsonify(user.to_dict())
    except Exception as e:
        db.session.rollback()
        return jsonify({'error': str(e)}), 500


@admin_bp.route('/api/users/<int:user_id>', methods=['DELETE'])
@admin_required
def api_delete_user(user_id: int):
    """Delete a user"""
    user = User.query.get_or_404(user_id)
    
    # Prevent deleting yourself
    current_user = get_current_user()
    if user.id == current_user.id:
        return jsonify({'error': 'Cannot delete your own account'}), 400
    
    try:
        db.session.delete(user)
        db.session.commit()
        return jsonify({'success': True})
    except Exception as e:
        db.session.rollback()
        return jsonify({'error': str(e)}), 500


# =====================
# Role Management
# =====================

@admin_bp.route('/roles')
@admin_required
def roles():
    """Role management page"""
    roles = Role.query.all()
    all_permissions = Permissions.all_permissions()
    
    return render_template('admin/roles.html',
                          roles=roles,
                          all_permissions=all_permissions,
                          current_user=get_current_user())


@admin_bp.route('/api/roles', methods=['GET'])
@admin_required
def api_list_roles():
    """Get list of all roles"""
    roles = Role.query.all()
    return jsonify([r.to_dict() for r in roles])


@admin_bp.route('/api/roles', methods=['POST'])
@admin_required
def api_create_role():
    """Create a new role"""
    data = request.get_json()
    
    if not data.get('name'):
        return jsonify({'error': 'Role name is required'}), 400
    
    if Role.query.filter_by(name=data['name']).first():
        return jsonify({'error': 'Role name already exists'}), 400
    
    try:
        role = Role(
            name=data['name'],
            description=data.get('description'),
            is_system=False
        )
        role.set_permissions(data.get('permissions', []))
        
        db.session.add(role)
        db.session.commit()
        
        return jsonify(role.to_dict()), 201
    except Exception as e:
        db.session.rollback()
        return jsonify({'error': str(e)}), 500


@admin_bp.route('/api/roles/<int:role_id>', methods=['PUT'])
@admin_required
def api_update_role(role_id: int):
    """Update a role"""
    role = Role.query.get_or_404(role_id)
    data = request.get_json()
    
    # Prevent editing system roles
    if role.is_system:
        return jsonify({'error': 'Cannot edit system roles'}), 400
    
    try:
        if 'name' in data:
            role.name = data['name']
        if 'description' in data:
            role.description = data['description']
        if 'permissions' in data:
            role.set_permissions(data['permissions'])
        
        db.session.commit()
        return jsonify(role.to_dict())
    except Exception as e:
        db.session.rollback()
        return jsonify({'error': str(e)}), 500


@admin_bp.route('/api/roles/<int:role_id>', methods=['DELETE'])
@admin_required
def api_delete_role(role_id: int):
    """Delete a role"""
    role = Role.query.get_or_404(role_id)
    
    if role.is_system:
        return jsonify({'error': 'Cannot delete system roles'}), 400
    
    if len(role.users) > 0:
        return jsonify({'error': f'Cannot delete role with {len(role.users)} assigned users'}), 400
    
    try:
        db.session.delete(role)
        db.session.commit()
        return jsonify({'success': True})
    except Exception as e:
        db.session.rollback()
        return jsonify({'error': str(e)}), 500


# =====================
# Embedded Python Management
# =====================

@admin_bp.route('/system')
@admin_required
def system():
    """System management page including embedded Python"""
    from app.services.embedded_python_manager import get_embedded_python_manager
    
    py_manager = get_embedded_python_manager()
    embedded_info = py_manager.get_embedded_info()
    runtime_stats = py_manager.get_runtime_stats()
    integrity = py_manager.verify_integrity()
    
    return render_template('admin/system.html',
                          embedded_info=embedded_info,
                          runtime_stats=runtime_stats,
                          integrity=integrity,
                          protection_warning=py_manager.get_protection_warning(),
                          current_user=get_current_user())


@admin_bp.route('/api/system/embedded-python', methods=['GET'])
@admin_required
def api_embedded_python_info():
    """Get embedded Python information"""
    from app.services.embedded_python_manager import get_embedded_python_manager
    
    py_manager = get_embedded_python_manager()
    return jsonify({
        'embedded_info': py_manager.get_embedded_info(),
        'runtime_stats': py_manager.get_runtime_stats(),
        'integrity': py_manager.verify_integrity()
    })


@admin_bp.route('/api/system/embedded-python/verify', methods=['POST'])
@admin_required
def api_verify_embedded_python():
    """Verify embedded Python integrity"""
    from app.services.embedded_python_manager import get_embedded_python_manager
    
    py_manager = get_embedded_python_manager()
    integrity = py_manager.verify_integrity()
    
    # Recreate protection marker if missing
    if not py_manager.is_protected():
        py_manager.create_protection_marker()
        integrity = py_manager.verify_integrity()
    
    return jsonify(integrity)


# =====================
# Group Management
# =====================

@admin_bp.route('/groups')
@admin_required
def groups():
    """Group management page"""
    groups = Group.query.all()
    users = User.query.all()
    
    return render_template('admin/groups.html',
                          groups=groups,
                          users=users,
                          current_user=get_current_user())


@admin_bp.route('/api/groups', methods=['GET'])
@admin_required
def api_list_groups():
    """Get list of all groups"""
    groups = Group.query.all()
    return jsonify([g.to_dict() for g in groups])


@admin_bp.route('/api/groups', methods=['POST'])
@admin_required
def api_create_group():
    """Create a new group"""
    data = request.get_json()
    
    if not data.get('name'):
        return jsonify({'error': 'Group name is required'}), 400
    
    if Group.query.filter_by(name=data['name']).first():
        return jsonify({'error': 'Group name already exists'}), 400
    
    try:
        current_user = get_current_user()
        group = Group(
            name=data['name'],
            description=data.get('description'),
            created_by=current_user.id
        )
        
        db.session.add(group)
        db.session.commit()
        
        return jsonify(group.to_dict()), 201
    except Exception as e:
        db.session.rollback()
        return jsonify({'error': str(e)}), 500


@admin_bp.route('/api/groups/<int:group_id>/members', methods=['POST'])
@admin_required
def api_add_group_member(group_id: int):
    """Add user to group"""
    group = Group.query.get_or_404(group_id)
    data = request.get_json()
    
    user_id = data.get('user_id')
    if not user_id:
        return jsonify({'error': 'User ID is required'}), 400
    
    user = User.query.get_or_404(user_id)
    
    try:
        if user not in group.members:
            group.members.append(user)
            db.session.commit()
        
        return jsonify({'success': True})
    except Exception as e:
        db.session.rollback()
        return jsonify({'error': str(e)}), 500


@admin_bp.route('/api/groups/<int:group_id>/members/<int:user_id>', methods=['DELETE'])
@admin_required
def api_remove_group_member(group_id: int, user_id: int):
    """Remove user from group"""
    group = Group.query.get_or_404(group_id)
    user = User.query.get_or_404(user_id)
    
    try:
        if user in group.members:
            group.members.remove(user)
            db.session.commit()
        
        return jsonify({'success': True})
    except Exception as e:
        db.session.rollback()
        return jsonify({'error': str(e)}), 500
