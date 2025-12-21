"""
Community server connection-related exceptions
"""


class CommunityConnectionError(Exception):
    """Base exception for Community server connection errors"""
    
    def __init__(self, message: str, url: str = None, original_error: Exception = None):
        super().__init__(message)
        self.url = url
        self.original_error = original_error


class CommunityAuthError(CommunityConnectionError):
    """Exception raised when Community server authentication fails"""
    
    def __init__(self, message: str = "Community authentication failed", api_key: str = None):
        super().__init__(message)
        self.api_key = api_key


class CommunityAPIError(CommunityConnectionError):
    """Exception raised when Community API returns an error"""
    
    def __init__(self, message: str, endpoint: str = None, status_code: int = None, response_data: dict = None):
        super().__init__(message)
        self.endpoint = endpoint
        self.status_code = status_code
        self.response_data = response_data
