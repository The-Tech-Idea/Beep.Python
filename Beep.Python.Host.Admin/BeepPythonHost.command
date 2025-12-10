#!/bin/bash
# Beep Python Host Admin Launcher for macOS
# Uses embedded Python - no virtual environment needed

# Get the directory where this script is located
APP_DIR="$(cd "$(dirname "$0")" && pwd)"
cd "$APP_DIR"

# Colors for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

echo -e "${GREEN}Beep Python Host Admin${NC}"
echo "========================================"

# Use embedded Python (required - no fallback)
if [ -f "python-embedded/bin/python3" ]; then
    PYTHON_EXE="$APP_DIR/python-embedded/bin/python3"
    export PYTHONPATH="$APP_DIR/python-embedded:$APP_DIR/python-embedded/lib/python3.11:$APP_DIR/python-embedded/lib/python3.11/site-packages"
elif [ -f "python-embedded/python" ]; then
    PYTHON_EXE="$APP_DIR/python-embedded/python"
    export PYTHONPATH="$APP_DIR/python-embedded"
else
    echo -e "${RED}ERROR: Embedded Python not found!${NC}"
    echo "Please ensure python-embedded folder exists."
    read -p "Press Enter to exit..."
    exit 1
fi

# Set environment variables
export PYTHONUNBUFFERED=1
export BEEP_PYTHON_HOME="$APP_DIR"
export BEEP_CONFIG_DIR="$APP_DIR/config"
export BEEP_DATA_DIR="$APP_DIR/data"

# Launch the application directly - dependencies are pre-installed
echo -e "${GREEN}Starting Beep Python Host Admin...${NC}"
"$PYTHON_EXE" run.py

# Keep terminal open on error
if [ $? -ne 0 ]; then
    echo -e "${RED}Application exited with error${NC}"
    read -p "Press Enter to exit..."
fi
