#!/bin/bash
# Beep.Python Host Admin - Linux/macOS Launcher
# This script automatically sets up and runs Host Admin

# Get script directory
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
cd "$SCRIPT_DIR"

echo ""
echo "============================================================"
echo "  Beep AI Server - Linux/macOS Launcher"
echo "============================================================"
echo ""

# Check if Python is available
if ! command -v python3 &> /dev/null; then
    echo "[ERROR] Python 3 is not installed or not in PATH"
    echo "Please install Python 3.8 or higher"
    exit 1
fi

# Make script executable
chmod +x run_hostadmin.py

# Run the Python launcher
python3 run_hostadmin.py

if [ $? -ne 0 ]; then
    echo ""
    echo "[ERROR] Failed to start Host Admin"
    exit 1
fi
