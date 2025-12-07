#!/bin/bash
# ============================================
# Beep.Python - Create Distribution Package (Linux/macOS)
# ============================================

set -e

echo "============================================"
echo "Beep.Python Distribution Builder"
echo "============================================"
echo ""
echo "This will create a complete distribution package"
echo "with embedded Python included."
echo ""

# Check if we're in the right directory
if [ ! -d "app" ]; then
    echo "ERROR: app directory not found!"
    echo "Please run this script from the Beep.Python.Host.Admin directory."
    exit 1
fi

# Create distribution directory
DIST_DIR="Beep.Python-Distribution"
if [ -d "$DIST_DIR" ]; then
    echo "Removing old distribution..."
    rm -rf "$DIST_DIR"
fi

echo "Creating distribution directory..."
mkdir -p "$DIST_DIR"

# Step 1: Download and setup embedded Python if not already present
echo ""
echo "[1/5] Setting up embedded Python..."

if [ ! -f "python-embedded/bin/python3" ]; then
    echo "Embedded Python not found. Running setup..."
    bash setup_embedded_python.sh
    if [ $? -ne 0 ]; then
        echo "ERROR: Failed to setup embedded Python!"
        exit 1
    fi
else
    echo "Embedded Python already present."
fi

# Step 2: Copy application files
echo ""
echo "[2/5] Copying application files..."

cp -r app "$DIST_DIR/"
cp -r templates "$DIST_DIR/"
[ -d "static" ] && cp -r static "$DIST_DIR/"
cp requirements.txt "$DIST_DIR/"
[ -f "app.py" ] && cp app.py "$DIST_DIR/"
[ -f "run.py" ] && cp run.py "$DIST_DIR/"

echo "Application files copied."

# Step 3: Copy embedded Python
echo ""
echo "[3/5] Copying embedded Python (~50MB)..."

cp -r python-embedded "$DIST_DIR/"

echo "Embedded Python copied."

# Step 4: Create launcher script
echo ""
echo "[4/5] Creating launcher script..."

cat > "$DIST_DIR/start.sh" << 'EOF'
#!/bin/bash
echo "Starting Beep.Python..."
echo ""

# Set embedded Python flag
export BEEP_EMBEDDED_PYTHON=1

# Run application
python-embedded/bin/python3 app.py

if [ $? -ne 0 ]; then
    echo ""
    echo "Application exited with error."
    read -p "Press Enter to continue..."
fi
EOF

chmod +x "$DIST_DIR/start.sh"

# Create README
cat > "$DIST_DIR/README.txt" << EOF
============================================
Beep.Python LLM Management System
============================================

QUICK START:

1. Run: ./start.sh
2. Wait for browser to open
3. Start managing your LLM models!

NO PYTHON INSTALLATION REQUIRED!
Python is already included in this package.

FEATURES:
- Intelligent LLM model discovery and setup
- Per-model virtual environments
- GPU backend support (CUDA, Metal, Vulkan)
- Subprocess-isolated inference
- Role-based access control
- Migration and health tools

SYSTEM REQUIREMENTS:
- Linux or macOS
- 4GB RAM minimum (8GB+ recommended)
- 10GB free disk space (for models)

IMPORTANT:
The "python-embedded" folder contains the Python runtime.
DO NOT DELETE this folder!

For support and documentation, visit:
[Your website/repository URL]
EOF

echo "Launcher script created."

# Step 5: Create tarball
echo ""
echo "[5/5] Creating tarball..."

TAR_NAME="Beep.Python-Portable-v1.0.tar.gz"

tar -czf "$TAR_NAME" "$DIST_DIR"

if [ $? -ne 0 ]; then
    echo "WARNING: Failed to create tarball."
    echo "You can manually compress the $DIST_DIR folder."
else
    echo "Tarball created: $TAR_NAME"
fi

# Calculate size
DIST_SIZE=$(du -sh "$DIST_DIR" | cut -f1)

echo ""
echo "============================================"
echo "Distribution Package Created!"
echo "============================================"
echo ""
echo "Location: $DIST_DIR/"
echo "Tarball: $TAR_NAME"
echo "Package Size: $DIST_SIZE"
echo ""
echo "CONTENTS:"
echo "- Application code"
echo "- Embedded Python 3.11.6"
echo "- All dependencies pre-installed"
echo "- Ready to run (no setup needed)"
echo ""
echo "DISTRIBUTION:"
echo "1. Share the tarball with users"
echo "2. Users extract: tar -xzf $TAR_NAME"
echo "3. Users run: cd $DIST_DIR && ./start.sh"
echo "4. That's it!"
echo ""
echo "============================================"
echo ""

read -p "Do you want to open the distribution folder? (y/N) " -n 1 -r
echo
if [[ $REPLY =~ ^[Yy]$ ]]; then
    if command -v xdg-open &> /dev/null; then
        xdg-open "$DIST_DIR"
    elif command -v open &> /dev/null; then
        open "$DIST_DIR"
    else
        echo "Please manually open: $DIST_DIR"
    fi
fi
