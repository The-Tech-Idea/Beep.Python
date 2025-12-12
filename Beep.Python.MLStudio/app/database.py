"""
Database initialization and models
"""
from app import db
from app.models.project import MLProject
from app.models.experiment import Experiment

def init_database():
    """Initialize database with tables"""
    db.create_all()
    print("Database initialized successfully!")

