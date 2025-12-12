#!/bin/bash
# ============================================
# MLStudio - Embedded Python Setup (Linux/macOS)
# ============================================

set -e  # Exit on error

echo "============================================"
echo "MLStudio Embedded Python Setup"
echo "============================================"
echo ""

# Detect OS
OS="$(uname -s)"
case "${OS}" in
    Linux*)     PLATFORM=linux;;
    Darwin*)    PLATFORM=macos;;
    *)          echo "Unsupported OS: ${OS}"; exit 1;;
esac

# Detect architecture
ARCH="$(uname -m)"
case "${ARCH}" in
    x86_64)     ARCH_NAME=x86_64;;
    aarch64)    ARCH_NAME=aarch64;;
    arm64)      ARCH_NAME=aarch64;;
    *)          echo "Unsupported architecture: ${ARCH}"; exit 1;;
esac

echo "Detected: ${PLATFORM} (${ARCH_NAME})"
echo ""

# Check if embedded Python already exists
if [ -d "python-embedded" ] && [ -f "python-embedded/bin/python3" ]; then
    echo "Embedded Python already installed."
    echo "Location: python-embedded/"
    echo ""
    read -p "Do you want to re-download and reinstall? (y/N) " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        echo "Skipping download."
        exit 0
    fi
fi

echo ""
echo "Downloading Python 3.11.7 Standalone..."
echo ""

# Create directory
mkdir -p python-embedded

# Download URLs for different platforms
if [ "$PLATFORM" = "linux" ]; then
    if [ "$ARCH_NAME" = "x86_64" ]; then
        URL="https://github.com/indygreg/python-build-standalone/releases/download/20231002/cpython-3.11.6+20231002-x86_64-unknown-linux-gnu-install_only.tar.gz"
    else
        URL="https://github.com/indygreg/python-build-standalone/releases/download/20231002/cpython-3.11.6+20231002-aarch64-unknown-linux-gnu-install_only.tar.gz"
    fi
elif [ "$PLATFORM" = "macos" ]; then
    if [ "$ARCH_NAME" = "aarch64" ]; then
        URL="https://github.com/indygreg/python-build-standalone/releases/download/20231002/cpython-3.11.6+20231002-aarch64-apple-darwin-install_only.tar.gz"
    else
        URL="https://github.com/indygreg/python-build-standalone/releases/download/20231002/cpython-3.11.6+20231002-x86_64-apple-darwin-install_only.tar.gz"
    fi
fi

echo "Downloading from: ${URL}"
echo "This will download approximately 30-40MB..."
echo ""

# Download and extract
curl -L "${URL}" -o python-embedded.tar.gz

if [ $? -ne 0 ]; then
    echo ""
    echo "ERROR: Failed to download Python!"
    echo "Please check your internet connection."
    exit 1
fi

echo ""
echo "Extracting Python..."
tar -xzf python-embedded.tar.gz -C python-embedded --strip-components=1

# Clean up
rm python-embedded.tar.gz

echo ""
echo "Installing pip..."
python-embedded/bin/python3 -m ensurepip

echo ""
echo "Installing application dependencies..."
python-embedded/bin/python3 -m pip install --upgrade pip
python-embedded/bin/python3 -m pip install -r requirements.txt

# Create protection marker
cat > python-embedded/.mlstudio_protected << EOF
CRITICAL_SYSTEM_COMPONENT
This directory contains the embedded Python runtime required for MLStudio to function.
DO NOT DELETE this directory unless you are uninstalling the application.
Deletion will prevent the application from starting.
EOF

echo ""
echo "============================================"
echo "Setup Complete!"
echo "============================================"
echo ""
echo "Embedded Python installed to: python-embedded/"
echo "Python version: 3.11.6"
echo ""
echo "You can now run MLStudio using ./run.sh or python run_mlstudio.py"
echo ""

