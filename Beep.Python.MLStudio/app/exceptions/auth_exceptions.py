"""
Authentication-related exceptions
"""


class AuthenticationError(Exception):
    """Base exception for authentication errors"""
    pass


class IdentityServerError(AuthenticationError):
    """Exception raised when Identity Server operations fail"""
    
    def __init__(self, message: str, original_error: Exception = None):
        super().__init__(message)
        self.original_error = original_error


class TokenValidationError(AuthenticationError):
    """Exception raised when token validation fails"""
    
    def __init__(self, message: str = "Token validation failed", token_type: str = None):
        super().__init__(message)
        self.token_type = token_type


class UserAccessDeniedError(AuthenticationError):
    """Exception raised when user access is denied"""
    
    def __init__(self, message: str = "Access denied", reason: str = None):
        super().__init__(message)
        self.reason = reason


class AuthenticationModeError(AuthenticationError):
    """Exception raised when authentication mode is invalid or not configured"""
    
    def __init__(self, message: str, mode: str = None):
        super().__init__(message)
        self.mode = mode
