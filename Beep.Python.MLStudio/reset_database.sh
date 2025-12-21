#!/bin/bash
# Reset MLStudio Database - Linux/macOS Shell Script

echo ""
echo "========================================"
echo "  MLStudio Database Reset Script"
echo "========================================"
echo ""

# Check if Python is available
if ! command -v python3 &> /dev/null; then
    if ! command -v python &> /dev/null; then
        echo "[ERROR] Python is not installed or not in PATH"
        echo "Please install Python or add it to your PATH"
        exit 1
    else
        PYTHON_CMD=python
    fi
else
    PYTHON_CMD=python3
fi

# Check if we should skip confirmation
if [ "$1" = "--force" ] || [ "$1" = "--yes" ]; then
    FORCE_FLAG="--force"
else
    echo "WARNING: This will delete ALL data in the database!"
    echo "All tables will be dropped and recreated from scratch."
    echo ""
    read -p "Are you sure you want to continue? (yes/no): " confirm
    if [ "$confirm" != "yes" ] && [ "$confirm" != "y" ]; then
        echo "Database reset cancelled."
        exit 0
    fi
fi

echo ""
echo "Resetting database..."
$PYTHON_CMD reset_database.py $FORCE_FLAG "$@"

if [ $? -ne 0 ]; then
    echo ""
    echo "[ERROR] Database reset failed!"
    exit 1
fi

echo ""
echo "Database reset completed successfully!"
