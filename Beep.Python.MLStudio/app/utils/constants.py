"""
Application Constants
Centralized constants to eliminate magic strings and improve maintainability
"""
from typing import Final

# ==================== Authentication Modes ====================
AUTH_MODE_LOCAL: Final[str] = 'local'
AUTH_MODE_IDENTITY_SERVER: Final[str] = 'identity_server'
AUTH_MODES: Final[tuple] = (AUTH_MODE_LOCAL, AUTH_MODE_IDENTITY_SERVER)

# ==================== Database Providers ====================
DB_PROVIDER_SQLITE: Final[str] = 'sqlite'
DB_PROVIDER_POSTGRESQL: Final[str] = 'postgresql'
DB_PROVIDER_MYSQL: Final[str] = 'mysql'
DB_PROVIDER_MSSQL: Final[str] = 'mssql'
DB_PROVIDER_ORACLE: Final[str] = 'oracle'
DB_PROVIDER_FIREBIRD: Final[str] = 'firebird'

DB_PROVIDERS: Final[tuple] = (
    DB_PROVIDER_SQLITE,
    DB_PROVIDER_POSTGRESQL,
    DB_PROVIDER_MYSQL,
    DB_PROVIDER_MSSQL,
    DB_PROVIDER_ORACLE,
    DB_PROVIDER_FIREBIRD
)

# ==================== Environment Variable Names ====================
ENV_SECRET_KEY: Final[str] = 'SECRET_KEY'
ENV_DEBUG: Final[str] = 'DEBUG'
ENV_FLASK_ENV: Final[str] = 'FLASK_ENV'
ENV_HOST: Final[str] = 'HOST'
ENV_PORT: Final[str] = 'PORT'
ENV_DATABASE_URL: Final[str] = 'DATABASE_URL'
ENV_DATABASE_PROVIDER: Final[str] = 'DATABASE_PROVIDER'
ENV_AUTH_MODE: Final[str] = 'AUTH_MODE'
ENV_IDENTITY_SERVER_URL: Final[str] = 'IDENTITY_SERVER_URL'
ENV_IDENTITY_SERVER_CLIENT_ID: Final[str] = 'IDENTITY_SERVER_CLIENT_ID'
ENV_IDENTITY_SERVER_CLIENT_SECRET: Final[str] = 'IDENTITY_SERVER_CLIENT_SECRET'
ENV_IDENTITY_SERVER_REDIRECT_URIS: Final[str] = 'IDENTITY_SERVER_REDIRECT_URIS'
ENV_JWT_SECRET_KEY: Final[str] = 'JWT_SECRET_KEY'
ENV_JWT_ACCESS_TOKEN_EXPIRES: Final[str] = 'JWT_ACCESS_TOKEN_EXPIRES'
ENV_MAX_UPLOAD_SIZE: Final[str] = 'MAX_UPLOAD_SIZE'
ENV_UPLOAD_FOLDER: Final[str] = 'UPLOAD_FOLDER'
ENV_REDIS_URL: Final[str] = 'REDIS_URL'
ENV_AISERVER_URL: Final[str] = 'AISERVER_URL'
ENV_AISERVER_API_KEY: Final[str] = 'AISERVER_API_KEY'
ENV_COMMUNITY_URL: Final[str] = 'COMMUNITY_URL'
ENV_COMMUNITY_API_KEY: Final[str] = 'COMMUNITY_API_KEY'
ENV_MLSTUDIO_FORCED_INDUSTRY: Final[str] = 'MLSTUDIO_FORCED_INDUSTRY'

# ==================== Default Values ====================
DEFAULT_SECRET_KEY: Final[str] = 'mlstudio-dev-secret-key-change-in-production'
DEFAULT_HOST: Final[str] = '127.0.0.1'
DEFAULT_PORT: Final[int] = 5001
DEFAULT_DATABASE_URL: Final[str] = 'sqlite:///mlstudio.db'
DEFAULT_DATABASE_PROVIDER: Final[str] = DB_PROVIDER_SQLITE
DEFAULT_AUTH_MODE: Final[str] = AUTH_MODE_LOCAL
DEFAULT_MAX_UPLOAD_SIZE_MB: Final[int] = 100
DEFAULT_UPLOAD_FOLDER: Final[str] = 'uploads'
DEFAULT_REDIS_URL: Final[str] = 'memory://'
DEFAULT_AISERVER_URL: Final[str] = 'http://127.0.0.1:5000'
DEFAULT_COMMUNITY_URL: Final[str] = 'http://127.0.0.1:5002'
DEFAULT_JWT_EXPIRES_SECONDS: Final[int] = 3600

# ==================== Timeouts and Retries ====================
HTTP_TIMEOUT: Final[int] = 30
HTTP_CONNECT_TIMEOUT: Final[int] = 10
DB_CONNECTION_TIMEOUT: Final[int] = 10
MAX_RETRIES: Final[int] = 3

# ==================== File Paths ====================
ENV_FILE: Final[str] = '.env'
ENV_EXAMPLE_FILE: Final[str] = '.env.example'
SETUP_COMPLETE_FILE: Final[str] = 'instance/setup_complete.json'
REQUIREMENTS_FILE: Final[str] = 'requirements.txt'
EMBEDDED_PYTHON_PATH: Final[str] = 'python-embedded'
VENV_PATH: Final[str] = '.venv'

# ==================== Directory Names ====================
DIR_UPLOADS: Final[str] = 'uploads'
DIR_PROJECTS: Final[str] = 'projects'
DIR_MODELS: Final[str] = 'models'
DIR_INSTANCE: Final[str] = 'instance'

# ==================== Error Messages ====================
# Authentication Errors
ERROR_AUTH_MODE_NOT_ENABLED: Final[str] = "Authentication mode not enabled"
ERROR_LOCAL_LOGIN_NOT_AVAILABLE: Final[str] = "Local login not available in Identity Server mode. Use OAuth2/OIDC login."
ERROR_IDENTITY_SERVER_NOT_CONFIGURED: Final[str] = "Identity Server client not configured"
ERROR_TOKEN_VALIDATION_FAILED: Final[str] = "Token validation failed"
ERROR_USER_INFO_RETRIEVAL_FAILED: Final[str] = "Failed to retrieve user information"
ERROR_EMAIL_NOT_FOUND: Final[str] = "Email not found in user info"
ERROR_USERNAME_NOT_FOUND: Final[str] = "Username not found in user info"

# Database Errors
ERROR_DB_CONNECTION_STRING_EMPTY: Final[str] = "Connection string cannot be empty"
ERROR_DB_PROVIDER_UNSUPPORTED: Final[str] = "Unsupported database provider"
ERROR_DB_CONNECTION_FAILED: Final[str] = "Database connection failed"
ERROR_DB_DRIVER_NOT_INSTALLED: Final[str] = "Database driver not installed"

# Setup Errors
ERROR_SETUP_COMPLETE_FAILED: Final[str] = "Failed to complete setup"
ERROR_INVALID_AUTH_MODE: Final[str] = "Invalid authentication mode"
ERROR_IDENTITY_SERVER_REQUIRED: Final[str] = "Identity Server URL and Client ID are required"
ERROR_CANNOT_CONNECT_IDENTITY_SERVER: Final[str] = "Cannot connect to Identity Server"

# Community Connection Errors
ERROR_COMMUNITY_CONNECTION_FAILED: Final[str] = "Cannot connect to Community server"
ERROR_COMMUNITY_AUTH_FAILED: Final[str] = "Community authentication failed"
ERROR_COMMUNITY_API_ERROR: Final[str] = "Community API error"
ERROR_COMMUNITY_NOT_CONFIGURED: Final[str] = "Community server not configured"

# ==================== HTTP Status Messages ====================
MSG_UNAUTHORIZED: Final[str] = 'Unauthorized - Invalid or expired token'
MSG_FORBIDDEN: Final[str] = 'Forbidden - Access denied'
MSG_CONNECTION_ERROR: Final[str] = 'Cannot connect to Identity Server. Please check the URL.'
MSG_TIMEOUT_ERROR: Final[str] = 'Request to Identity Server timed out'

# ==================== Success Messages ====================
MSG_ADMIN_CREATED: Final[str] = 'Admin user created successfully'
MSG_DATABASE_CONFIGURED: Final[str] = 'Database configured successfully'
MSG_IDENTITY_SERVER_CONFIGURED: Final[str] = 'Identity Server authentication configured successfully'
MSG_LOCAL_AUTH_CONFIGURED: Final[str] = 'Local JWT authentication mode configured'
MSG_SETUP_COMPLETE: Final[str] = 'Setup completed successfully!'
MSG_CONNECTION_SUCCESS: Final[str] = 'Connection successful'
MSG_COMMUNITY_CONNECTED: Final[str] = 'Connected to Community server successfully'

# ==================== Database Connection String Prefixes ====================
DB_PREFIX_SQLITE: Final[str] = 'sqlite://'
DB_PREFIX_POSTGRESQL: Final[str] = 'postgresql://'
DB_PREFIX_POSTGRES: Final[str] = 'postgres://'
DB_PREFIX_MYSQL: Final[str] = 'mysql'
DB_PREFIX_MYSQL_PYMYSQL: Final[str] = 'mysql+pymysql://'
DB_PREFIX_MARIADB: Final[str] = 'mariadb://'
DB_PREFIX_MSSQL: Final[str] = 'mssql://'
DB_PREFIX_MSSQL_PYODBC: Final[str] = 'mssql+pyodbc://'
DB_PREFIX_ORACLE: Final[str] = 'oracle://'
DB_PREFIX_FIREBIRD: Final[str] = 'firebird://'

# ==================== Database Default Ports ====================
DB_PORT_POSTGRESQL: Final[int] = 5432
DB_PORT_MYSQL: Final[int] = 3306
DB_PORT_MSSQL: Final[int] = 1433
DB_PORT_ORACLE: Final[int] = 1521
DB_PORT_FIREBIRD: Final[int] = 3050

# ==================== Python Embedded Configuration ====================
PYTHON_VERSION_MIN: Final[tuple] = (3, 8)
PYTHON_EMBEDDED_VERSION: Final[str] = '3.11.7'
PYTHON_EMBEDDED_BUILD_VERSION: Final[str] = '3.11.6+20231002'

# URLs for embedded Python downloads
PYTHON_EMBEDDED_URL_WINDOWS: Final[str] = 'https://www.python.org/ftp/python/3.11.7/python-3.11.7-embed-amd64.zip'
PYTHON_EMBEDDED_URL_LINUX_X64: Final[str] = 'https://github.com/indygreg/python-build-standalone/releases/download/20231002/cpython-3.11.6+20231002-x86_64-unknown-linux-gnu-install_only.tar.gz'
PYTHON_EMBEDDED_URL_LINUX_ARM64: Final[str] = 'https://github.com/indygreg/python-build-standalone/releases/download/20231002/cpython-3.11.6+20231002-aarch64-unknown-linux-gnu-install_only.tar.gz'
PYTHON_EMBEDDED_URL_MACOS_X64: Final[str] = 'https://github.com/indygreg/python-build-standalone/releases/download/20231002/cpython-3.11.6+20231002-x86_64-apple-darwin-install_only.tar.gz'
PYTHON_EMBEDDED_URL_MACOS_ARM64: Final[str] = 'https://github.com/indygreg/python-build-standalone/releases/download/20231002/cpython-3.11.6+20231002-aarch64-apple-darwin-install_only.tar.gz'
PIP_INSTALL_URL: Final[str] = 'https://bootstrap.pypa.io/get-pip.py'

# ==================== Identity Server Endpoints ====================
ID_SERVER_ENDPOINT_HEALTH: Final[str] = '/api/health'
ID_SERVER_ENDPOINT_TOKEN_VALIDATE: Final[str] = '/api/token/validate'
ID_SERVER_ENDPOINT_USER_ACCESS_CHECK: Final[str] = '/api/useraccess/check'
ID_SERVER_ENDPOINT_USER_ROLE: Final[str] = '/api/useraccess/role'
ID_SERVER_ENDPOINT_USER_APPLICATIONS: Final[str] = '/api/useraccess/applications'
ID_SERVER_ENDPOINT_TOKEN: Final[str] = '/connect/token'
ID_SERVER_ENDPOINT_USERINFO: Final[str] = '/connect/userinfo'

# ==================== Community API Endpoints ====================
COMMUNITY_ENDPOINT_HEALTH: Final[str] = '/api/v1/health'
COMMUNITY_ENDPOINT_USER_COMPETITIONS: Final[str] = '/api/v1/users/{user_id}/competitions'
COMMUNITY_ENDPOINT_USER_SUBMISSIONS: Final[str] = '/api/v1/users/{user_id}/submissions'
COMMUNITY_ENDPOINT_USER_RANKINGS: Final[str] = '/api/v1/users/{user_id}/rankings'
COMMUNITY_ENDPOINT_USER_ACTIVITY: Final[str] = '/api/v1/users/{user_id}/activity'
COMMUNITY_ENDPOINT_USER_STATS: Final[str] = '/api/v1/users/{user_id}/competitions/stats'

# ==================== HTTP Status Codes ====================
HTTP_OK: Final[int] = 200
HTTP_BAD_REQUEST: Final[int] = 400
HTTP_UNAUTHORIZED: Final[int] = 401
HTTP_FORBIDDEN: Final[int] = 403
HTTP_NOT_FOUND: Final[int] = 404
HTTP_INTERNAL_SERVER_ERROR: Final[int] = 500
