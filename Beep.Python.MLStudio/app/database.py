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

def reset_database():
    """Reset database (drop and recreate all tables) - WARNING: Deletes all data!"""
    db.drop_all()
    db.create_all()
    print("Database reset successfully - all tables dropped and recreated!")

