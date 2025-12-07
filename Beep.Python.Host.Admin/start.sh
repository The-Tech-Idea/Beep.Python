#!/bin/bash
# ============================================
# Beep.Python - Application Launcher (Linux/macOS)
# ============================================

# Check for embedded Python
if [ -f "python-embedded/bin/python3" ]; then
    echo "Using embedded Python..."
    PYTHON_EXE="python-embedded/bin/python3"
    export BEEP_EMBEDDED_PYTHON=1
else
    echo "Embedded Python not found. Checking system Python..."
    if ! command -v python3 &> /dev/null; then
        echo ""
        echo "ERROR: Python not found!"
        echo ""
        echo "Please run ./setup_embedded_python.sh first to install embedded Python."
        echo ""
        exit 1
    fi
    PYTHON_EXE="python3"
fi

echo "Starting Beep.Python LLM Management..."
echo ""

# Run the application
$PYTHON_EXE app.py

if [ $? -ne 0 ]; then
    echo ""
    echo "Application exited with error code: $?"
    read -p "Press Enter to continue..."
fi
