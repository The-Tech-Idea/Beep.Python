#!/usr/bin/env python3
"""
Beep.Python Host Admin
Professional Python Environment Management Web Application

Run with: python run.py
Or: flask run
"""
import os
import sys

# Add the app directory to the path
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from app import create_app, socketio

app = create_app()

if __name__ == '__main__':
    host = os.environ.get('HOST', '127.0.0.1')
    port = int(os.environ.get('PORT', 5000))
    debug = os.environ.get('DEBUG', 'true').lower() == 'true'
    
    print(f"""
╔══════════════════════════════════════════════════════════════╗
║           Beep.Python Host Admin                             ║
║           Python Environment Management                      ║
╠══════════════════════════════════════════════════════════════╣
║  Server running at: http://{host}:{port:<5}                      ║
║  Debug mode: {'ON ' if debug else 'OFF'}                                            ║
╚══════════════════════════════════════════════════════════════╝
    """)
    
    socketio.run(app, host=host, port=port, debug=debug)
