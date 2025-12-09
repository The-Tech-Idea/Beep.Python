"""
Database Module
Initializes SQLAlchemy and provides connection helpers.
"""
from flask_sqlalchemy import SQLAlchemy
from sqlalchemy.orm import DeclarativeBase

class Base(DeclarativeBase):
    pass

db = SQLAlchemy(model_class=Base)

def init_db(app):
    """Initialize database with application"""
    db.init_app(app)
    
    with app.app_context():
        # Tables will be created via Setup Wizard or manual init
        pass

def get_db_uri(provider, **kwargs):
    """Construct a database URI from parameters"""
    if provider == 'sqlite':
        # Local SQLite
        path = kwargs.get('path', 'beep_data.db')
        return f"sqlite:///{path}"
    
    elif provider == 'postgresql':
        user = kwargs.get('user')
        password = kwargs.get('password')
        host = kwargs.get('host')
        port = kwargs.get('port', 5432)
        dbname = kwargs.get('dbname')
        return f"postgresql://{user}:{password}@{host}:{port}/{dbname}"
        
    elif provider == 'sqlserver':
        # Requires pyodbc and ODBC Driver for SQL Server
        user = kwargs.get('user')
        password = kwargs.get('password')
        host = kwargs.get('host')
        port = kwargs.get('port', 1433)
        dbname = kwargs.get('dbname')
        driver = kwargs.get('driver', 'ODBC Driver 17 for SQL Server')
        return f"mssql+pyodbc://{user}:{password}@{host}:{port}/{dbname}?driver={driver}"
        
    elif provider == 'oracle':
        # Requires cx_Oracle
        user = kwargs.get('user')
        password = kwargs.get('password')
        host = kwargs.get('host')
        port = kwargs.get('port', 1521)
        service = kwargs.get('service')
        return f"oracle+cx_oracle://{user}:{password}@{host}:{port}/?service_name={service}"
        
    return None
