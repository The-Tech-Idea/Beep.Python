# Beep.Python.MLStudio

> A user-friendly environment for creating and testing Machine Learning models

[![Python](https://img.shields.io/badge/Python-3.8+-blue.svg)](https://www.python.org/)
[![Flask](https://img.shields.io/badge/Flask-3.0+-green.svg)](https://flask.palletsprojects.com/)
[![License](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

**Beep.Python.MLStudio** is a professional web application for creating, training, and testing Machine Learning models in an intuitive, user-friendly environment. It uses **embedded Python** as the base runtime for all virtual environments, ensuring isolated and reproducible ML projects.

> **⚠️ IMPORTANT: You do NOT need to manually run `cd`, `pip install`, `python init_database.py`, or start Host Admin separately. Just run `run.bat` (Windows) or `./run.sh` (Linux/macOS) and everything happens automatically!**

---

## 🚀 Quick Start (One Command!)

### ⚡ Just Run One Command - That's It!

**No manual setup needed!** The launcher handles everything automatically.

#### Windows
```cmd
run.bat
```
Or double-click `run.bat` in File Explorer

#### Linux/macOS
```bash
./run.sh
```

#### macOS (Double-Click)
Double-click `run.command` in Finder

#### Cross-Platform (Python)
```bash
python run_mlstudio.py
```

### What Happens Automatically

The launcher automatically:
- ✅ Checks Python version (requires 3.8+)
- ✅ **Sets up embedded Python** (base runtime for all environments)
- ✅ Creates virtual environment (`.venv`)
- ✅ Installs all dependencies (`pip install -r requirements.txt`)
- ✅ Creates `.env` configuration file (with defaults)
- ✅ Creates necessary directories (`data/`, `projects/`)
- ✅ Initializes database (`python init_database.py`)
- ✅ Optionally starts Host Admin (if you approve)
- ✅ Starts MLStudio (opens browser at http://127.0.0.1:5001)

**No manual steps required!** Everything is automated.

### Prerequisites
- **Python 3.8+** installed (the launcher will check)
- **Beep.Python.Host.Admin** in a sibling directory (the launcher will find it)

### About Host Admin Integration

**No code copying needed!** MLStudio communicates with Host Admin via HTTP API. The launcher will:
1. **Search for Host Admin** in common locations (sibling directory, etc.)
2. **Check if it's running** (on port 5000)
3. **Optionally start it** if you approve (when prompted)

See [INTEGRATION.md](INTEGRATION.md) for details on how the integration works.

---

## 📋 Requirements

- **Python**: 3.8 or higher
- **Beep.Python.Host.Admin**: Should be in a sibling directory (the launcher will find and optionally start it)

---

## 🎯 Features

### 🎯 ML Project Management
- **Project Creation**: Create isolated ML projects with dedicated virtual environments
- **Environment Integration**: Seamless integration with Beep.Python.Host.Admin for environment management
- **Project Templates**: Pre-configured templates for common ML tasks (Classification, Regression, Clustering, etc.)
- **Version Control**: Track model versions and experiments

### 🤖 Model Development
- **Interactive Notebooks**: Create and edit Jupyter-style notebooks
- **Model Training**: Train models with real-time progress tracking
- **Model Evaluation**: Comprehensive evaluation metrics and visualizations
- **Model Comparison**: Compare multiple models side-by-side

### 📊 Data Management
- **Data Upload**: Upload datasets (CSV, JSON, Excel)
- **Data Preview**: Interactive data preview and statistics
- **Data Preprocessing**: Built-in preprocessing tools
- **Data Visualization**: Interactive charts and plots

### 🔧 ML Framework Support
- **Scikit-learn**: Full support for scikit-learn models
- **TensorFlow/Keras**: Deep learning with TensorFlow
- **PyTorch**: PyTorch neural networks
- **XGBoost**: Gradient boosting models
- **Custom Models**: Support for any Python ML library

### 🎨 User-Friendly Interface
- **Modern UI**: Clean, intuitive Bootstrap 5 interface
- **Real-time Updates**: WebSocket support for live progress
- **Code Editor**: Syntax-highlighted code editor
- **Visualizations**: Interactive charts with Plotly

---

## 📁 Project Structure

```
Beep.Python.MLStudio/
├── app/                          # Application code
│   ├── __init__.py              # Application factory
│   ├── config.py                # Configuration management
│   ├── database.py              # Database initialization
│   ├── models/                  # SQLAlchemy models
│   │   ├── __init__.py
│   │   ├── project.py           # ML Project model
│   │   └── experiment.py        # Experiment model
│   ├── routes/                  # Flask blueprints
│   │   ├── __init__.py
│   │   ├── dashboard.py         # Main dashboard
│   │   ├── projects.py          # Project management
│   │   ├── models.py            # ML model operations
│   │   ├── experiments.py       # Experiment tracking
│   │   └── api.py               # REST API endpoints
│   ├── services/               # Business logic
│   │   ├── __init__.py
│   │   ├── environment_manager.py # Virtual environment management
│   │   ├── embedded_python_manager.py # Embedded Python runtime
│   │   ├── ml_service.py        # ML model operations
│   │   ├── data_service.py      # Data management
│   │   └── notebook_service.py  # Notebook execution
│   └── utils/                  # Utility functions
│       ├── __init__.py
│       └── validators.py
├── templates/                   # Jinja2 HTML templates
├── static/                      # Static files (CSS, JS, images)
├── data/                        # Uploaded datasets
├── projects/                    # ML project files
├── run_mlstudio.py            # Cross-platform launcher
├── run.bat                     # Windows launcher
├── run.sh                      # Linux/macOS launcher
├── run.command                 # macOS double-click launcher
├── run.py                      # Application entry point
├── init_database.py            # Database initialization
├── requirements.txt            # Python dependencies
└── README.md                   # This file
```

---

## 🔧 Configuration

The launcher automatically creates a `.env` file on first run. You can edit it to customize:

```bash
# Flask Configuration
SECRET_KEY=your-secret-key-here
DEBUG=true
HOST=127.0.0.1
PORT=5001

# Database
DATABASE_URL=sqlite:///mlstudio.db

# File Upload
MAX_UPLOAD_SIZE=100  # MB
UPLOAD_FOLDER=data
PROJECTS_FOLDER=projects
```

## 🐍 Embedded Python (Base Runtime)

**Embedded Python is the foundation of MLStudio** - it's the base runtime used to create all virtual environments.

### Why Embedded Python?
- **Base Runtime**: All virtual environments are created from embedded Python
- **Isolated**: No conflicts with system Python installations
- **Pre-configured**: Flask and core dependencies ready to use
- **Portable**: Everything in one directory - fully self-contained

### Setup

The launcher (`run_mlstudio.py`) will automatically set up embedded Python on first run. You can also set it up manually:

- **Windows**: Run `setup_embedded_python.bat`
- **Linux/macOS**: Run `./setup_embedded_python.sh`

**Note**: Embedded Python is **REQUIRED**. MLStudio will not run without it. The launcher will automatically set it up on first run, or you can set it up manually using the scripts above.

---

## 📡 API Reference

### REST API

All REST API endpoints are under `/api/v1/`:

#### Projects
- `GET /api/v1/projects` - List all projects
- `POST /api/v1/projects` - Create new project
- `GET /api/v1/projects/{id}` - Get project details
- `PUT /api/v1/projects/{id}` - Update project
- `DELETE /api/v1/projects/{id}` - Delete project

#### Models
- `POST /api/v1/projects/{id}/models/train` - Train a model
- `GET /api/v1/projects/{id}/models` - List models
- `POST /api/v1/projects/{id}/models/predict` - Make predictions
- `GET /api/v1/projects/{id}/models/{model_id}/evaluate` - Evaluate model

#### Experiments
- `GET /api/v1/projects/{id}/experiments` - List experiments
- `POST /api/v1/projects/{id}/experiments` - Create experiment
- `GET /api/v1/experiments/{id}` - Get experiment details

---

## 🎯 Usage Workflow

### Step 1: Start MLStudio (One Command!)

```bash
# Windows
run.bat

# Linux/macOS  
./run.sh

# Or cross-platform
python run_mlstudio.py
```

**That's all you need!** The launcher handles:
- ✅ Virtual environment setup
- ✅ Dependency installation
- ✅ Database initialization
- ✅ Configuration file creation
- ✅ Host Admin detection and startup
- ✅ MLStudio launch

**No manual steps like `cd`, `pip install`, `python init_database.py`, etc. - it's all automatic!**

### Step 2: Create a New ML Project
1. Open http://127.0.0.1:5001 in your browser
2. Click "New Project"
3. Enter project name and select framework
4. MLStudio automatically creates a virtual environment via Host Admin
5. Start coding your ML model

### 3. Train a Model
1. Open your project
2. Upload or select your dataset
3. Write training code or use the visual builder
4. Click "Train" and monitor progress in real-time
5. View evaluation metrics and visualizations

### 4. Compare Models
1. Train multiple models with different parameters
2. View comparison dashboard
3. Select best model based on metrics
4. Export model for deployment

---

## 🔒 Security

- Session-based authentication
- Project-level access control
- Secure file upload validation
- Environment isolation via embedded Python and virtual environments

---

## 🛠️ Troubleshooting

### "Environment creation failed"
- Ensure Python is installed and accessible (or set up embedded Python)
- Verify disk space is available
- Check that the `providers` directory is writable

### "Embedded Python not found" or "Embedded Python is required"
- **This is a required component** - MLStudio cannot run without embedded Python
- Run `setup_embedded_python.bat` (Windows) or `./setup_embedded_python.sh` (Linux/macOS)
- The launcher will automatically set it up on first run
- If setup fails, check your internet connection and try running the setup script manually

### Port Already in Use
- Change `PORT` in `.env` to a different port (e.g., 5002)
- Or stop the process using port 5001

### Python Not Found
- Ensure Python 3.8+ is installed
- Add Python to your system PATH
- On Windows, check "Add Python to PATH" during installation

---

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

---

## 📝 License

This project is licensed under the MIT License - see the LICENSE file for details.

---

## 🙏 Acknowledgments

- **Beep.Python.Host.Admin** - Environment management
- **Flask** - Web framework
- **Scikit-learn** - ML library
- **Bootstrap** - UI framework

---

**Made with ❤️ for the ML community**

---

**Version**: 1.0.0  
**Last Updated**: 2024
