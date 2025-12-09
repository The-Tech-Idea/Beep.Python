"""
Data Source Connectors for RAG Document Sync
Cross-platform connectors for SQL databases, file systems, and APIs.
"""
import os
import json
import hashlib
import glob
from pathlib import Path
from datetime import datetime
from typing import Dict, List, Any, Optional, Generator
from abc import ABC, abstractmethod


class DataSourceConnector(ABC):
    """Base class for data source connectors"""
    
    def __init__(self, config: Dict[str, Any]):
        self.config = config
        self._connected = False
    
    @abstractmethod
    def connect(self) -> bool:
        """Establish connection to the data source"""
        pass
    
    @abstractmethod
    def disconnect(self):
        """Close connection"""
        pass
    
    @abstractmethod
    def test_connection(self) -> Dict[str, Any]:
        """Test the connection and return status"""
        pass
    
    @abstractmethod
    def fetch_documents(self) -> Generator[Dict[str, Any], None, None]:
        """Fetch documents from the data source as a generator"""
        pass
    
    def get_document_hash(self, content: str) -> str:
        """Generate content hash for change detection"""
        return hashlib.sha256(content.encode()).hexdigest()[:16]


class SQLDatabaseConnector(DataSourceConnector):
    """Connector for SQL databases (SQLite, PostgreSQL, MySQL, SQL Server)"""
    
    def __init__(self, config: Dict[str, Any]):
        super().__init__(config)
        self.connection = None
        self.db_type = config.get('source_type', 'sqlite')
    
    def _get_connection_string(self) -> str:
        """Build connection string based on database type"""
        if self.config.get('connection_string'):
            return self.config['connection_string']
        
        db_type = self.db_type
        host = self.config.get('host', 'localhost')
        port = self.config.get('port')
        database = self.config.get('database', '')
        username = self.config.get('username', '')
        password = self.config.get('password', '')
        
        if db_type == 'sqlite':
            return f"sqlite:///{database}"
        elif db_type == 'postgresql':
            port = port or 5432
            return f"postgresql://{username}:{password}@{host}:{port}/{database}"
        elif db_type == 'mysql':
            port = port or 3306
            return f"mysql+pymysql://{username}:{password}@{host}:{port}/{database}"
        elif db_type == 'mssql':
            port = port or 1433
            return f"mssql+pyodbc://{username}:{password}@{host}:{port}/{database}?driver=ODBC+Driver+17+for+SQL+Server"
        else:
            raise ValueError(f"Unsupported database type: {db_type}")
    
    def connect(self) -> bool:
        """Connect to the database using SQLAlchemy"""
        try:
            from sqlalchemy import create_engine
            conn_str = self._get_connection_string()
            self.engine = create_engine(conn_str)
            self.connection = self.engine.connect()
            self._connected = True
            return True
        except Exception as e:
            print(f"Database connection error: {e}")
            self._connected = False
            return False
    
    def disconnect(self):
        """Close database connection"""
        if self.connection:
            self.connection.close()
            self._connected = False
    
    def test_connection(self) -> Dict[str, Any]:
        """Test the database connection"""
        try:
            if not self._connected:
                self.connect()
            
            from sqlalchemy import text
            result = self.connection.execute(text("SELECT 1"))
            result.fetchone()
            
            return {
                'success': True,
                'message': f'Connected to {self.db_type} database',
                'database': self.config.get('database', 'unknown')
            }
        except Exception as e:
            return {
                'success': False,
                'message': str(e)
            }
        finally:
            self.disconnect()
    
    def fetch_documents(self) -> Generator[Dict[str, Any], None, None]:
        """Fetch documents from SQL query results"""
        query = self.config.get('query')
        if not query:
            return
        
        content_col = self.config.get('content_column', 'content')
        title_col = self.config.get('title_column', 'title')
        id_col = self.config.get('id_column', 'id')
        
        try:
            if not self._connected:
                self.connect()
            
            from sqlalchemy import text
            result = self.connection.execute(text(query))
            columns = result.keys()
            
            for row in result:
                row_dict = dict(zip(columns, row))
                
                # Extract content
                content = str(row_dict.get(content_col, ''))
                if not content:
                    continue
                
                # Build document
                doc_id = str(row_dict.get(id_col, hashlib.md5(content.encode()).hexdigest()[:12]))
                title = str(row_dict.get(title_col, f'Record {doc_id}'))
                
                yield {
                    'id': doc_id,
                    'content': content,
                    'title': title,
                    'source': f"{self.db_type}:{self.config.get('database', 'db')}",
                    'content_hash': self.get_document_hash(content),
                    'metadata': {
                        'source_type': 'database',
                        'database_type': self.db_type,
                        'query': query[:100] + '...' if len(query) > 100 else query,
                        'row_data': {k: str(v)[:200] for k, v in row_dict.items() if k not in [content_col]}
                    }
                }
        except Exception as e:
            print(f"Error fetching documents from database: {e}")
            raise
        finally:
            self.disconnect()


class FileSystemConnector(DataSourceConnector):
    """Connector for file system (works on Windows, Mac, Linux)"""
    
    # Supported file extensions and their handlers
    SUPPORTED_EXTENSIONS = {
        '.txt': 'text',
        '.md': 'text',
        '.json': 'json',
        '.csv': 'csv',
        '.html': 'html',
        '.xml': 'xml',
        '.py': 'text',
        '.js': 'text',
        '.ts': 'text',
        '.css': 'text',
        '.yaml': 'text',
        '.yml': 'text',
        '.pdf': 'pdf'
    }
    
    def __init__(self, config: Dict[str, Any]):
        super().__init__(config)
        self.base_path = Path(config.get('base_path', '.'))
        self.patterns = config.get('file_patterns', ['*.txt', '*.md'])
        if isinstance(self.patterns, str):
            try:
                self.patterns = json.loads(self.patterns)
            except:
                self.patterns = [self.patterns]
        self.recursive = config.get('recursive', True)
    
    def connect(self) -> bool:
        """Verify base path exists"""
        self._connected = self.base_path.exists()
        return self._connected
    
    def disconnect(self):
        """No persistent connection for file system"""
        self._connected = False
    
    def test_connection(self) -> Dict[str, Any]:
        """Test if the path is accessible"""
        if not self.base_path.exists():
            return {
                'success': False,
                'message': f'Path does not exist: {self.base_path}'
            }
        
        if not self.base_path.is_dir():
            return {
                'success': False,
                'message': f'Path is not a directory: {self.base_path}'
            }
        
        # Count matching files
        file_count = 0
        for pattern in self.patterns:
            if self.recursive:
                file_count += len(list(self.base_path.rglob(pattern)))
            else:
                file_count += len(list(self.base_path.glob(pattern)))
        
        return {
            'success': True,
            'message': f'Found {file_count} matching files',
            'path': str(self.base_path),
            'patterns': self.patterns,
            'file_count': file_count
        }
    
    def _read_file_content(self, file_path: Path) -> Optional[str]:
        """Read content from a file based on its type"""
        ext = file_path.suffix.lower()
        
        if ext == '.pdf':
            return self._read_pdf(file_path)
        elif ext == '.json':
            return self._read_json(file_path)
        elif ext == '.csv':
            return self._read_csv(file_path)
        elif ext in ['.html', '.xml']:
            return self._read_markup(file_path)
        else:
            # Default: read as text
            try:
                return file_path.read_text(encoding='utf-8')
            except UnicodeDecodeError:
                try:
                    return file_path.read_text(encoding='latin-1')
                except:
                    return None
    
    def _read_pdf(self, file_path: Path) -> Optional[str]:
        """Extract text from PDF"""
        try:
            import fitz  # PyMuPDF
            doc = fitz.open(file_path)
            text = ""
            for page in doc:
                text += page.get_text()
            doc.close()
            return text.strip() if text.strip() else f"[PDF: {file_path.name} - No extractable text]"
        except ImportError:
            return f"[PDF: {file_path.name} - PyMuPDF not installed]"
        except Exception as e:
            return f"[PDF: {file_path.name} - Error: {str(e)}]"
    
    def _read_json(self, file_path: Path) -> Optional[str]:
        """Read JSON file as formatted string"""
        try:
            with open(file_path, 'r', encoding='utf-8') as f:
                data = json.load(f)
            return json.dumps(data, indent=2)
        except:
            return file_path.read_text(encoding='utf-8')
    
    def _read_csv(self, file_path: Path) -> Optional[str]:
        """Read CSV file as text"""
        try:
            import csv
            rows = []
            with open(file_path, 'r', encoding='utf-8', newline='') as f:
                reader = csv.reader(f)
                for i, row in enumerate(reader):
                    if i >= 1000:  # Limit rows
                        rows.append("... (truncated)")
                        break
                    rows.append(', '.join(row))
            return '\n'.join(rows)
        except:
            return file_path.read_text(encoding='utf-8')
    
    def _read_markup(self, file_path: Path) -> Optional[str]:
        """Read HTML/XML and optionally strip tags"""
        try:
            from html.parser import HTMLParser
            
            class TextExtractor(HTMLParser):
                def __init__(self):
                    super().__init__()
                    self.text = []
                
                def handle_data(self, data):
                    self.text.append(data.strip())
            
            content = file_path.read_text(encoding='utf-8')
            extractor = TextExtractor()
            extractor.feed(content)
            return ' '.join(filter(None, extractor.text))
        except:
            return file_path.read_text(encoding='utf-8')
    
    def fetch_documents(self) -> Generator[Dict[str, Any], None, None]:
        """Fetch documents from file system"""
        if not self.connect():
            return
        
        processed_files = set()
        
        for pattern in self.patterns:
            if self.recursive:
                files = self.base_path.rglob(pattern)
            else:
                files = self.base_path.glob(pattern)
            
            for file_path in files:
                if not file_path.is_file():
                    continue
                
                # Avoid duplicates
                abs_path = str(file_path.absolute())
                if abs_path in processed_files:
                    continue
                processed_files.add(abs_path)
                
                # Read content
                content = self._read_file_content(file_path)
                if not content:
                    continue
                
                # Get file stats
                stat = file_path.stat()
                
                yield {
                    'id': hashlib.md5(abs_path.encode()).hexdigest()[:12],
                    'content': content,
                    'title': file_path.name,
                    'source': abs_path,
                    'content_hash': self.get_document_hash(content),
                    'metadata': {
                        'source_type': 'file_system',
                        'file_path': abs_path,
                        'file_name': file_path.name,
                        'file_size': stat.st_size,
                        'modified_at': datetime.fromtimestamp(stat.st_mtime).isoformat(),
                        'extension': file_path.suffix.lower()
                    }
                }


class APIConnector(DataSourceConnector):
    """Connector for REST APIs"""
    
    def __init__(self, config: Dict[str, Any]):
        super().__init__(config)
        self.url = config.get('api_url', '')
        self.method = config.get('api_method', 'GET').upper()
        self.headers = config.get('api_headers', {})
        if isinstance(self.headers, str):
            try:
                self.headers = json.loads(self.headers)
            except:
                self.headers = {}
        
        self.auth_type = config.get('api_auth_type', 'none')
        self.auth_value = config.get('api_auth_value', '')
    
    def _get_headers(self) -> Dict[str, str]:
        """Build request headers with authentication"""
        headers = dict(self.headers)
        
        if self.auth_type == 'bearer':
            headers['Authorization'] = f'Bearer {self.auth_value}'
        elif self.auth_type == 'api_key':
            headers['X-API-Key'] = self.auth_value
        
        return headers
    
    def connect(self) -> bool:
        """APIs are connectionless, just verify URL"""
        self._connected = bool(self.url)
        return self._connected
    
    def disconnect(self):
        """No persistent connection for APIs"""
        self._connected = False
    
    def test_connection(self) -> Dict[str, Any]:
        """Test the API endpoint"""
        import urllib.request
        import urllib.error
        
        try:
            headers = self._get_headers()
            req = urllib.request.Request(self.url, headers=headers, method='HEAD')
            
            with urllib.request.urlopen(req, timeout=10) as response:
                return {
                    'success': True,
                    'message': f'API reachable (status {response.status})',
                    'url': self.url
                }
        except urllib.error.HTTPError as e:
            # Some APIs don't support HEAD, try GET
            if e.code == 405:
                try:
                    req = urllib.request.Request(self.url, headers=headers, method='GET')
                    with urllib.request.urlopen(req, timeout=10) as response:
                        return {
                            'success': True,
                            'message': f'API reachable (status {response.status})',
                            'url': self.url
                        }
                except Exception as e2:
                    return {'success': False, 'message': str(e2)}
            return {'success': False, 'message': f'HTTP {e.code}: {e.reason}'}
        except Exception as e:
            return {'success': False, 'message': str(e)}
    
    def fetch_documents(self) -> Generator[Dict[str, Any], None, None]:
        """Fetch documents from API response"""
        import urllib.request
        import urllib.error
        
        try:
            headers = self._get_headers()
            headers['Accept'] = 'application/json'
            
            req = urllib.request.Request(self.url, headers=headers, method=self.method)
            
            with urllib.request.urlopen(req, timeout=60) as response:
                data = json.loads(response.read().decode('utf-8'))
            
            # Handle different response structures
            items = []
            if isinstance(data, list):
                items = data
            elif isinstance(data, dict):
                # Try common keys
                for key in ['data', 'results', 'items', 'documents', 'records']:
                    if key in data and isinstance(data[key], list):
                        items = data[key]
                        break
                if not items:
                    # Treat the whole object as one document
                    items = [data]
            
            content_col = self.config.get('content_column', 'content')
            title_col = self.config.get('title_column', 'title')
            id_col = self.config.get('id_column', 'id')
            
            for i, item in enumerate(items):
                if isinstance(item, dict):
                    content = str(item.get(content_col, json.dumps(item)))
                    title = str(item.get(title_col, f'API Record {i+1}'))
                    doc_id = str(item.get(id_col, hashlib.md5(content.encode()).hexdigest()[:12]))
                else:
                    content = str(item)
                    title = f'API Record {i+1}'
                    doc_id = hashlib.md5(content.encode()).hexdigest()[:12]
                
                yield {
                    'id': doc_id,
                    'content': content,
                    'title': title,
                    'source': self.url,
                    'content_hash': self.get_document_hash(content),
                    'metadata': {
                        'source_type': 'api',
                        'api_url': self.url,
                        'record_index': i
                    }
                }
        except Exception as e:
            print(f"Error fetching from API: {e}")
            raise


class CSVConnector(FileSystemConnector):
    """Special connector for CSV files with column mapping"""
    
    def __init__(self, config: Dict[str, Any]):
        super().__init__(config)
        self.content_col = config.get('content_column')
        self.title_col = config.get('title_column')
        self.id_col = config.get('id_column')
    
    def fetch_documents(self) -> Generator[Dict[str, Any], None, None]:
        """Fetch documents from CSV with column mapping"""
        import csv
        
        if not self.connect():
            return
        
        # If no column mapping, use parent implementation
        if not self.content_col:
            yield from super().fetch_documents()
            return
        
        for pattern in self.patterns:
            if self.recursive:
                files = self.base_path.rglob(pattern)
            else:
                files = self.base_path.glob(pattern)
            
            for file_path in files:
                if not file_path.is_file() or file_path.suffix.lower() != '.csv':
                    continue
                
                try:
                    with open(file_path, 'r', encoding='utf-8', newline='') as f:
                        reader = csv.DictReader(f)
                        
                        for i, row in enumerate(reader):
                            content = row.get(self.content_col, '')
                            if not content:
                                continue
                            
                            title = row.get(self.title_col, f'{file_path.name} Row {i+1}')
                            doc_id = row.get(self.id_col) or hashlib.md5(content.encode()).hexdigest()[:12]
                            
                            yield {
                                'id': str(doc_id),
                                'content': content,
                                'title': title,
                                'source': f'{file_path.name}:row{i+1}',
                                'content_hash': self.get_document_hash(content),
                                'metadata': {
                                    'source_type': 'csv',
                                    'file_path': str(file_path.absolute()),
                                    'row_index': i,
                                    'row_data': {k: str(v)[:100] for k, v in row.items() if k != self.content_col}
                                }
                            }
                except Exception as e:
                    print(f"Error reading CSV {file_path}: {e}")


def get_connector(config: Dict[str, Any]) -> DataSourceConnector:
    """Factory function to get the appropriate connector"""
    source_type = config.get('source_type', 'file_system')
    
    if source_type in ['sqlite', 'postgresql', 'mysql', 'mssql']:
        return SQLDatabaseConnector(config)
    elif source_type == 'file_system':
        return FileSystemConnector(config)
    elif source_type == 'api':
        return APIConnector(config)
    elif source_type == 'csv':
        return CSVConnector(config)
    else:
        raise ValueError(f"Unsupported source type: {source_type}")
