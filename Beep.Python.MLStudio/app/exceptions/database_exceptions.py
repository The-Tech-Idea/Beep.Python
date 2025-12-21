"""
Database-related exceptions
"""


class DatabaseError(Exception):
    """Base exception for database errors"""
    pass


class DatabaseConnectionError(DatabaseError):
    """Exception raised when database connection fails"""
    
    def __init__(self, message: str, connection_string: str = None, original_error: Exception = None):
        super().__init__(message)
        self.connection_string = connection_string
        self.original_error = original_error


class DatabaseConfigurationError(DatabaseError):
    """Exception raised when database configuration is invalid"""
    
    def __init__(self, message: str, provider: str = None):
        super().__init__(message)
        self.provider = provider


class UnsupportedDatabaseProviderError(DatabaseError):
    """Exception raised when an unsupported database provider is used"""
    
    def __init__(self, provider: str, supported_providers: list = None):
        message = f"Unsupported database provider: {provider}"
        if supported_providers:
            message += f". Supported providers: {', '.join(supported_providers)}"
        super().__init__(message)
        self.provider = provider
        self.supported_providers = supported_providers


class DatabaseDriverError(DatabaseError):
    """Exception raised when database driver is not installed or unavailable"""
    
    def __init__(self, message: str, driver_name: str = None):
        super().__init__(message)
        self.driver_name = driver_name
