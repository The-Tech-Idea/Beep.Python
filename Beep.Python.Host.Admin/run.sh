#!/bin/bash

# Default Port
PORT=5000

# Beep.Python Host Admin - Linux/macOS Startup Script
# Checks for Python, creates venv, installs requirements, and runs the app.

echo "==================================================="
echo "  Beep.Python Host Admin - Startup"
echo "==================================================="

# Function to check for command
command_exists() {
    command -v "$1" >/dev/null 2>&1
}

# Find Python executable
PYTHON_CMD="python3"
if ! command_exists python3; then
    if command_exists python; then
        PYTHON_CMD="python"
    else
        echo "[ERROR] Python 3 not found! Please install python3."
        exit 1
    fi
fi

# Check version (optional, but good practice)
$PYTHON_CMD --version

# Create virtual environment if it doesn't exist
if [ ! -d ".venv" ]; then
    echo "[INFO] Virtual environment not found. Creating one..."
    $PYTHON_CMD -m venv .venv
    if [ $? -ne 0 ]; then
        echo "[ERROR] Failed to create virtual environment. You might need to install 'python3-venv'."
        exit 1
    fi
    echo "[INFO] Virtual environment created."
fi

# Activate environment
source .venv/bin/activate

# Install dependencies
echo "[INFO] Checking dependencies..."
pip install -r requirements.txt
if [ $? -ne 0 ]; then
    echo "[ERROR] Failed to install dependencies."
    exit 1
fi

# Run the application
echo "[INFO] Starting application..."
echo "[INFO] Open your browser to http://127.0.0.1:$PORT"
export PORT=$PORT
python run.py
