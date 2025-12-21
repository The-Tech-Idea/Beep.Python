"""
Request Validation Utilities
Reusable validation decorators and helpers for Flask routes
"""
from functools import wraps
from flask import request, jsonify
from typing import Callable, Dict, List, Optional, Any
import re
import logging

logger = logging.getLogger(__name__)


class ValidationError(Exception):
    """Exception raised when request validation fails"""
    def __init__(self, message: str, field: str = None):
        super().__init__(message)
        self.field = field
        self.message = message


def validate_email(email: str) -> bool:
    """Validate email format"""
    if not email:
        return False
    pattern = r'^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$'
    return bool(re.match(pattern, email))


def validate_username(username: str) -> bool:
    """Validate username format (3-20 chars, alphanumeric and underscores)"""
    if not username:
        return False
    if len(username) < 3 or len(username) > 20:
        return False
    return bool(re.match(r'^[a-zA-Z0-9_]+$', username))


def validate_password(password: str) -> tuple[bool, Optional[str]]:
    """Validate password (min 8 chars)"""
    if not password:
        return False, "Password cannot be empty"
    if len(password) < 8:
        return False, "Password must be at least 8 characters"
    return True, None


def validate_json_request(required_fields: Optional[List[str]] = None):
    """
    Decorator to validate JSON request and required fields
    
    Args:
        required_fields: List of required field names
        
    Example:
        @validate_json_request(required_fields=['username', 'email', 'password'])
        def my_route():
            data = request.get_json()
            # data is guaranteed to exist and have required fields
    """
    def decorator(func: Callable) -> Callable:
        @wraps(func)
        def wrapper(*args, **kwargs):
            if not request.is_json:
                return jsonify({
                    'success': False,
                    'message': 'Request must be JSON'
                }), 400
            
            data = request.get_json()
            if not data:
                return jsonify({
                    'success': False,
                    'message': 'Request body is required'
                }), 400
            
            if required_fields:
                missing_fields = [field for field in required_fields if field not in data or not data[field]]
                if missing_fields:
                    return jsonify({
                        'success': False,
                        'message': f'Missing required fields: {", ".join(missing_fields)}'
                    }), 400
            
            return func(*args, **kwargs)
        return wrapper
    return decorator


def validate_field_types(field_types: Dict[str, type]):
    """
    Decorator to validate field types in JSON request
    
    Args:
        field_types: Dictionary mapping field names to expected types
        
    Example:
        @validate_field_types({'age': int, 'name': str, 'active': bool})
        def my_route():
            data = request.get_json()
    """
    def decorator(func: Callable) -> Callable:
        @wraps(func)
        def wrapper(*args, **kwargs):
            data = request.get_json() or {}
            
            for field, expected_type in field_types.items():
                if field in data and data[field] is not None:
                    if not isinstance(data[field], expected_type):
                        try:
                            # Try to convert
                            if expected_type == int:
                                data[field] = int(data[field])
                            elif expected_type == float:
                                data[field] = float(data[field])
                            elif expected_type == bool:
                                data[field] = str(data[field]).lower() in ('true', '1', 'yes', 'on')
                            else:
                                raise ValueError(f"Cannot convert {field} to {expected_type.__name__}")
                        except (ValueError, TypeError):
                            return jsonify({
                                'success': False,
                                'message': f'Field "{field}" must be of type {expected_type.__name__}'
                            }), 400
            
            return func(*args, **kwargs)
        return wrapper
    return decorator


def sanitize_string_input(fields: List[str], max_length: Optional[int] = None, strip: bool = True):
    """
    Decorator to sanitize string input fields
    
    Args:
        fields: List of field names to sanitize
        max_length: Optional maximum length
        strip: Whether to strip whitespace
        
    Example:
        @sanitize_string_input(['username', 'email'], max_length=100)
        def my_route():
            data = request.get_json()
    """
    def decorator(func: Callable) -> Callable:
        @wraps(func)
        def wrapper(*args, **kwargs):
            data = request.get_json() or {}
            
            for field in fields:
                if field in data and isinstance(data[field], str):
                    if strip:
                        data[field] = data[field].strip()
                    if max_length and len(data[field]) > max_length:
                        data[field] = data[field][:max_length]
                    # Convert empty strings to None
                    if not data[field]:
                        data[field] = None
            
            return func(*args, **kwargs)
        return wrapper
    return decorator


def validate_email_field(field_name: str = 'email'):
    """
    Decorator to validate email field
    
    Args:
        field_name: Name of the email field
        
    Example:
        @validate_email_field('email')
        def my_route():
            data = request.get_json()
    """
    def decorator(func: Callable) -> Callable:
        @wraps(func)
        def wrapper(*args, **kwargs):
            data = request.get_json() or {}
            
            if field_name in data and data[field_name]:
                email = data[field_name].strip().lower()
                if not validate_email(email):
                    return jsonify({
                        'success': False,
                        'message': f'Invalid email format for field "{field_name}"'
                    }), 400
                data[field_name] = email
            
            return func(*args, **kwargs)
        return wrapper
    return decorator


def validate_username_field(field_name: str = 'username'):
    """
    Decorator to validate username field
    
    Args:
        field_name: Name of the username field
        
    Example:
        @validate_username_field('username')
        def my_route():
            data = request.get_json()
    """
    def decorator(func: Callable) -> Callable:
        @wraps(func)
        def wrapper(*args, **kwargs):
            data = request.get_json() or {}
            
            if field_name in data and data[field_name]:
                username = data[field_name].strip()
                if not validate_username(username):
                    return jsonify({
                        'success': False,
                        'message': f'Invalid username format for field "{field_name}". Use 3-20 characters, alphanumeric and underscores only'
                    }), 400
                data[field_name] = username
            
            return func(*args, **kwargs)
        return wrapper
    return decorator


def validate_password_field(field_name: str = 'password'):
    """
    Decorator to validate password field
    
    Args:
        field_name: Name of the password field
        
    Example:
        @validate_password_field('password')
        def my_route():
            data = request.get_json()
    """
    def decorator(func: Callable) -> Callable:
        @wraps(func)
        def wrapper(*args, **kwargs):
            data = request.get_json() or {}
            
            if field_name in data and data[field_name]:
                password = data[field_name]
                is_valid, error_msg = validate_password(password)
                if not is_valid:
                    return jsonify({
                        'success': False,
                        'message': error_msg or f'Invalid password for field "{field_name}"'
                    }), 400
            
            return func(*args, **kwargs)
        return wrapper
    return decorator


def error_handler(func: Callable) -> Callable:
    """
    Decorator to handle exceptions and return consistent error responses
    
    Example:
        @error_handler
        def my_route():
            # Any exceptions will be caught and returned as JSON
    """
    @wraps(func)
    def wrapper(*args, **kwargs):
        try:
            return func(*args, **kwargs)
        except ValidationError as e:
            logger.warning(f"Validation error: {e.message}")
            return jsonify({
                'success': False,
                'message': e.message,
                'field': e.field
            }), 400
        except Exception as e:
            logger.error(f"Unexpected error in {func.__name__}: {e}", exc_info=True)
            return jsonify({
                'success': False,
                'message': 'An unexpected error occurred'
            }), 500
    return wrapper


def combine_validators(*decorators):
    """
    Combine multiple validation decorators
    
    Example:
        @combine_validators(
            validate_json_request(['username', 'email', 'password']),
            sanitize_string_input(['username', 'email']),
            validate_email_field('email'),
            validate_username_field('username'),
            validate_password_field('password')
        )
        def my_route():
            pass
    """
    def decorator(func: Callable) -> Callable:
        for dec in reversed(decorators):
            func = dec(func)
        return func
    return decorator
