"""
Setup wizard-related exceptions
"""


class SetupError(Exception):
    """Base exception for setup errors"""
    pass


class SetupValidationError(SetupError):
    """Exception raised when setup validation fails"""
    
    def __init__(self, message: str, field: str = None):
        super().__init__(message)
        self.field = field


class SetupConfigurationError(SetupError):
    """Exception raised when setup configuration is invalid"""
    
    def __init__(self, message: str, step: str = None):
        super().__init__(message)
        self.step = step


class SetupCompleteError(SetupError):
    """Exception raised when setup completion fails"""
    
    def __init__(self, message: str, original_error: Exception = None):
        super().__init__(message)
        self.original_error = original_error
