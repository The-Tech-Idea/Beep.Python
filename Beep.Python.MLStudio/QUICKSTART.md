# Quick Start Guide - Beep.Python.MLStudio

## One-Command Setup & Run

### Windows
```cmd
run.bat
```
Or double-click `run.bat` in File Explorer

### Linux/macOS
```bash
chmod +x run.sh
./run.sh
```

### macOS (Double-Click)
Double-click `run.command` in Finder

### Cross-Platform
```bash
python run_mlstudio.py
```

---

## What Happens Automatically

When you run the launcher, it will:

1. ✅ **Check Python version** (requires 3.8+)
2. ✅ **Create virtual environment** (`.venv` folder)
3. ✅ **Install dependencies** (from `requirements.txt`)
4. ✅ **Create `.env` file** (if missing, with defaults)
5. ✅ **Create directories** (`data/`, `projects/`)
6. ✅ **Initialize database** (if needed)
7. ✅ **Find Host Admin** (searches common locations)
8. ✅ **Check if Host Admin is running** (on port 5000)
9. ✅ **Optionally start Host Admin** (if found but not running - asks for approval)
10. ✅ **Start MLStudio** (opens browser at http://127.0.0.1:5001)

---

## First Time Setup

### Prerequisites
- **Python 3.8+** installed and in PATH
- **Beep.Python.Host.Admin** in a sibling directory (recommended structure below)

### Recommended Project Structure

```
Beep.Python/
├── Beep.Python.Host.Admin/     ← Host Admin (sibling directory)
│   ├── run.py
│   └── ...
└── Beep.Python.MLStudio/        ← MLStudio (this project)
    ├── run_mlstudio.py
    └── ...
```

The launcher will automatically find Host Admin if it's in:
- `../Beep.Python.Host.Admin` (sibling directory - most common)
- `./Beep.Python.Host.Admin` (subdirectory)
- `~/Beep.Python.Host.Admin` (home directory)

---

## Using MLStudio

### 1. Create a Project
1. Open http://127.0.0.1:5001 in your browser
2. Click "New Project"
3. Enter project name and select framework (scikit-learn, TensorFlow, PyTorch, etc.)
4. MLStudio automatically creates a virtual environment via Host Admin
5. Start coding your ML model!

### 2. Upload Data
1. Go to your project
2. Click "Upload Dataset"
3. Select CSV, JSON, or Excel file
4. Data is validated and ready to use

### 3. Train a Model
1. Write your training script (or use the example template)
2. Click "Train"
3. Monitor progress in real-time
4. View results and metrics

### 4. Compare Models
1. Train multiple models with different parameters
2. View comparison dashboard
3. Select best model
4. Export for deployment

---

## Troubleshooting

### "Python not found"
- Install Python 3.8+ from https://www.python.org/
- On Windows, check "Add Python to PATH" during installation
- Verify: `python --version` or `python3 --version`

### "Host Admin not found"
- Ensure Host Admin is in a sibling directory: `../Beep.Python.Host.Admin`
- Or start Host Admin manually before running MLStudio
- The launcher will show you where it's looking

### "Host Admin not running"
- The launcher will offer to start it automatically (answer 'y')
- Or start it manually:
  ```bash
  cd ../Beep.Python.Host.Admin
  python run.py
  ```

### "Port already in use"
- Change `PORT` in `.env` file to a different port (e.g., 5002)
- Or stop the process using port 5001

### "Cannot connect to Host Admin"
- Ensure Host Admin is running: http://127.0.0.1:5000/api/v1/health
- Check `HOST_ADMIN_URL` in `.env` file
- Verify firewall isn't blocking the connection

---

## Configuration

The launcher creates a `.env` file automatically. You can edit it:

```bash
# Host Admin URL (default: http://127.0.0.1:5000)
HOST_ADMIN_URL=http://127.0.0.1:5000

# MLStudio port (default: 5001)
PORT=5001

# Other settings...
```

---

## Next Steps

- Read the [README.md](README.md) for full documentation
- Check [INTEGRATION.md](INTEGRATION.md) for Host Admin integration details
- Explore the web interface at http://127.0.0.1:5001

---

**That's it!** Just run one command and you're ready to build ML models! 🚀
