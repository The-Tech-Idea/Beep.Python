"""
Custom Exception Classes
"""
from app.exceptions.auth_exceptions import (
    AuthenticationError,
    IdentityServerError,
    TokenValidationError,
    UserAccessDeniedError,
    AuthenticationModeError
)
from app.exceptions.database_exceptions import (
    DatabaseError,
    DatabaseConnectionError,
    DatabaseConfigurationError,
    UnsupportedDatabaseProviderError,
    DatabaseDriverError
)
from app.exceptions.setup_exceptions import (
    SetupError,
    SetupValidationError,
    SetupConfigurationError,
    SetupCompleteError
)
from app.exceptions.community_exceptions import (
    CommunityConnectionError,
    CommunityAuthError,
    CommunityAPIError
)

__all__ = [
    'AuthenticationError',
    'IdentityServerError',
    'TokenValidationError',
    'UserAccessDeniedError',
    'AuthenticationModeError',
    'DatabaseError',
    'DatabaseConnectionError',
    'DatabaseConfigurationError',
    'UnsupportedDatabaseProviderError',
    'DatabaseDriverError',
    'SetupError',
    'SetupValidationError',
    'SetupConfigurationError',
    'SetupCompleteError',
    'CommunityConnectionError',
    'CommunityAuthError',
    'CommunityAPIError'
]
