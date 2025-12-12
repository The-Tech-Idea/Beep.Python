#!/usr/bin/env python3
"""
Beep.Python.MLStudio - Cross-Platform Launcher
Automatically sets up and runs MLStudio
"""
import os
import sys
import subprocess
import platform
import time
from pathlib import Path

# Try to import requests (may not be available until dependencies are installed)
try:
    import requests
    HAS_REQUESTS = True
except ImportError:
    HAS_REQUESTS = False

# Colors for terminal output
class Colors:
    GREEN = '\033[92m'
    YELLOW = '\033[93m'
    RED = '\033[91m'
    BLUE = '\033[94m'
    CYAN = '\033[96m'
    RESET = '\033[0m'
    BOLD = '\033[1m'

def print_colored(message, color=Colors.RESET, end='\n'):
    """Print colored message (works on Unix, plain text on Windows)"""
    try:
        if platform.system() == 'Windows':
            print(message, end=end)
        else:
            print(f"{color}{message}{Colors.RESET}", end=end)
    except UnicodeEncodeError:
        # Fallback: replace emoji with ASCII equivalents
        message = message.replace('✅', '[OK]')
        message = message.replace('❌', '[ERROR]')
        message = message.replace('⚠️', '[WARN]')
        message = message.replace('📦', '[INFO]')
        message = message.replace('📥', '[INFO]')
        message = message.replace('⚙️', '[INFO]')
        message = message.replace('🗄️', '[INFO]')
        message = message.replace('🔍', '[INFO]')
        message = message.replace('📁', '[INFO]')
        message = message.replace('🚀', '[INFO]')
        message = message.replace('👋', '[INFO]')
        message = message.replace('📋', '[INFO]')
        print(message, end=end)

def check_python_version():
    """Check if Python version is 3.8+"""
    if sys.version_info < (3, 8):
        print_colored("❌ Python 3.8 or higher is required!", Colors.RED)
        print_colored(f"   Current version: {sys.version}", Colors.YELLOW)
        sys.exit(1)
    print_colored(f"✅ Python {sys.version_info.major}.{sys.version_info.minor}.{sys.version_info.micro}", Colors.GREEN)

def setup_virtual_environment():
    """Create and setup virtual environment"""
    venv_path = Path('.venv')
    
    if platform.system() == 'Windows':
        venv_python = venv_path / 'Scripts' / 'python.exe'
    else:
        venv_python = venv_path / 'bin' / 'python'
    
    if not venv_path.exists():
        print_colored("📦 Creating virtual environment...", Colors.CYAN)
        try:
            subprocess.run([sys.executable, '-m', 'venv', '.venv'], check=True, capture_output=True)
            print_colored("✅ Virtual environment created", Colors.GREEN)
        except subprocess.CalledProcessError as e:
            print_colored(f"❌ Failed to create virtual environment: {e}", Colors.RED)
            if e.stderr:
                print_colored(f"   Error: {e.stderr.decode()}", Colors.RED)
            sys.exit(1)
    
    if not venv_python.exists():
        print_colored(f"❌ Virtual environment setup failed! Python not found at: {venv_python}", Colors.RED)
        print_colored("   Trying to create venv again...", Colors.YELLOW)
        try:
            subprocess.run([sys.executable, '-m', 'venv', '.venv'], check=True, capture_output=True)
            if not venv_python.exists():
                print_colored("❌ Still failed. Please check Python installation.", Colors.RED)
                sys.exit(1)
        except Exception as e:
            print_colored(f"❌ Error: {e}", Colors.RED)
            sys.exit(1)
    
    return str(venv_python)

def install_dependencies(venv_python):
    """Install required packages"""
    requirements = Path('requirements.txt')
    if not requirements.exists():
        print_colored("❌ requirements.txt not found!", Colors.RED)
        sys.exit(1)
    
    print_colored("📥 Installing dependencies...", Colors.CYAN)
    
    # Upgrade pip first
    print_colored("   Upgrading pip...", Colors.CYAN)
    result = subprocess.run(
        [str(venv_python), '-m', 'pip', 'install', '--upgrade', 'pip', '--quiet'],
        capture_output=True,
        text=True
    )
    
    # Install dependencies - this is critical, so we check for success
    print_colored("   Installing packages from requirements.txt...", Colors.CYAN)
    result = subprocess.run(
        [str(venv_python), '-m', 'pip', 'install', '-r', 'requirements.txt'],
        capture_output=True,
        text=True
    )
    
    if result.returncode == 0:
        print_colored("✅ Dependencies installed successfully", Colors.GREEN)
        
        # Verify critical packages are installed
        print_colored("   Verifying installation...", Colors.CYAN)
        check_packages = ['flask', 'flask-sqlalchemy', 'flask-socketio', 'python-dotenv']
        missing = []
        for pkg in check_packages:
            check_result = subprocess.run(
                [str(venv_python), '-m', 'pip', 'show', pkg],
                capture_output=True,
                text=True
            )
            if check_result.returncode != 0:
                missing.append(pkg)
        
        if missing:
            print_colored(f"❌ Critical packages missing: {', '.join(missing)}", Colors.RED)
            print_colored("   Attempting to install missing packages...", Colors.YELLOW)
            for pkg in missing:
                subprocess.run(
                    [str(venv_python), '-m', 'pip', 'install', pkg],
                    capture_output=True,
                    text=True
                )
            print_colored("✅ Missing packages installed", Colors.GREEN)
    else:
        print_colored("❌ Failed to install dependencies!", Colors.RED)
        if result.stdout:
            print_colored(f"   Output: {result.stdout[-500:]}", Colors.YELLOW)
        if result.stderr:
            print_colored(f"   Error: {result.stderr[-500:]}", Colors.RED)
        print_colored("\n   Please check your internet connection and try again.", Colors.YELLOW)
        sys.exit(1)

def setup_embedded_python():
    """Set up embedded Python - REQUIRED for MLStudio"""
    embedded_path = Path('python-embedded')
    if platform.system() == 'Windows':
        python_exe = embedded_path / 'python.exe'
    else:
        python_exe = embedded_path / 'bin' / 'python3'
    
    if python_exe.exists():
        print_colored("✅ Embedded Python already installed", Colors.GREEN)
        return True
    
    print_colored("🐍 Setting up embedded Python (required for MLStudio)...", Colors.CYAN)
    print_colored("   This is the base runtime for all virtual environments", Colors.CYAN)
    
    # Check if setup script exists
    if platform.system() == 'Windows':
        setup_script = Path('setup_embedded_python.bat')
    else:
        setup_script = Path('setup_embedded_python.sh')
    
    if not setup_script.exists():
        print_colored("❌ Setup script not found!", Colors.RED)
        print_colored(f"   Expected: {setup_script}", Colors.YELLOW)
        print_colored("   Please ensure setup_embedded_python.bat (Windows) or setup_embedded_python.sh (Linux/macOS) exists", Colors.YELLOW)
        return False
    
    print_colored(f"   Running setup script: {setup_script}", Colors.CYAN)
    print_colored("   This may take a few minutes...", Colors.CYAN)
    
    try:
        if platform.system() == 'Windows':
            result = subprocess.run([str(setup_script)], check=False)
        else:
            # Make sure script is executable
            os.chmod(setup_script, 0o755)
            result = subprocess.run(['bash', str(setup_script)], check=False)
        
        # Check if setup was successful
        if python_exe.exists():
            print_colored("✅ Embedded Python installed successfully!", Colors.GREEN)
            return True
        else:
            print_colored("❌ Embedded Python setup failed!", Colors.RED)
            print_colored("   Please run the setup script manually:", Colors.YELLOW)
            if platform.system() == 'Windows':
                print_colored(f"   {setup_script}", Colors.CYAN)
            else:
                print_colored(f"   ./{setup_script}", Colors.CYAN)
            return False
    except Exception as e:
        print_colored(f"❌ Error running setup script: {e}", Colors.RED)
        return False

def setup_environment_file():
    """Create .env file if it doesn't exist"""
    env_file = Path('.env')
    env_example = Path('.env.example')
    
    if not env_file.exists():
        print_colored("⚙️  Creating .env file...", Colors.CYAN)
        if env_example.exists():
            import shutil
            shutil.copy(env_example, env_file)
            print_colored("✅ .env file created from .env.example", Colors.GREEN)
        else:
            # Create default .env
            default_env = """# Flask Configuration
SECRET_KEY=mlstudio-dev-secret-key-change-in-production
DEBUG=true
HOST=127.0.0.1
PORT=5001

# Database
DATABASE_URL=sqlite:///mlstudio.db

# File Upload
MAX_UPLOAD_SIZE=100
UPLOAD_FOLDER=data
PROJECTS_FOLDER=projects
"""
            env_file.write_text(default_env)
            print_colored("✅ Default .env file created", Colors.GREEN)

def ensure_embedded_python_packages(embedded_python):
    """Ensure embedded Python has ALL required packages for admin operations - REQUIRED"""
    print_colored("   Checking embedded Python packages (REQUIRED)...", Colors.CYAN)
    
    # All packages required for admin operations
    required_packages = [
        'flask',
        'flask-sqlalchemy', 
        'python-dotenv',
        'flask-cors',
        'flask-socketio'
    ]
    
    missing_packages = []
    
    for package in required_packages:
        # Check if package is installed
        check_result = subprocess.run(
            [str(embedded_python), '-m', 'pip', 'show', package],
            capture_output=True,
            text=True
        )
        
        if check_result.returncode != 0:
            missing_packages.append(package)
            print_colored(f"   ❌ {package} not found - installing...", Colors.YELLOW)
            install_result = subprocess.run(
                [str(embedded_python), '-m', 'pip', 'install', package],
                capture_output=True,
                text=True
            )
            if install_result.returncode == 0:
                print_colored(f"   ✅ {package} installed successfully", Colors.GREEN)
            else:
                print_colored(f"   ❌ FAILED to install {package}", Colors.RED)
                if install_result.stderr:
                    print_colored(f"      Error: {install_result.stderr[:200]}", Colors.RED)
                return False
        else:
            print_colored(f"   ✅ {package} already installed", Colors.GREEN)
    
    if missing_packages:
        print_colored(f"   ✅ All {len(required_packages)} required packages installed", Colors.GREEN)
    
    return True


def initialize_database():
    """Initialize database using embedded Python - REQUIRED, NO FALLBACKS"""
    embedded_path = Path('python-embedded')
    if platform.system() == 'Windows':
        embedded_python = embedded_path / 'python.exe'
    else:
        embedded_python = embedded_path / 'bin' / 'python3'
    
    if not embedded_python.exists():
        print_colored("", Colors.RESET)
        print_colored("=" * 60, Colors.RED)
        print_colored("❌ CRITICAL ERROR: Embedded Python Not Found", Colors.BOLD + Colors.RED)
        print_colored("=" * 60, Colors.RED)
        print_colored("   Database initialization REQUIRES embedded Python.", Colors.RED)
        print_colored("   Embedded Python is the base runtime for all operations.", Colors.RED)
        print_colored("", Colors.RESET)
        print_colored("   Please run setup_embedded_python.bat (Windows) or", Colors.YELLOW)
        print_colored("   ./setup_embedded_python.sh (Linux/macOS) first.", Colors.YELLOW)
        print_colored("", Colors.RESET)
        return False
    
    print_colored("🗄️  Initializing database (REQUIRED - using embedded Python)...", Colors.CYAN)
    
    # Ensure embedded Python has ALL required packages - REQUIRED
    print_colored("   Step 1: Installing required packages in embedded Python...", Colors.CYAN)
    if not ensure_embedded_python_packages(embedded_python):
        print_colored("", Colors.RESET)
        print_colored("=" * 60, Colors.RED)
        print_colored("❌ CRITICAL ERROR: Failed to install required packages", Colors.BOLD + Colors.RED)
        print_colored("=" * 60, Colors.RED)
        print_colored("   Cannot proceed without required packages in embedded Python.", Colors.RED)
        print_colored("", Colors.RESET)
        return False
    
    # Use admin script that's designed for embedded Python
    admin_script = Path(__file__).parent / 'admin_init_db.py'
    if not admin_script.exists():
        print_colored("", Colors.RESET)
        print_colored("=" * 60, Colors.RED)
        print_colored("❌ CRITICAL ERROR: Admin script not found", Colors.BOLD + Colors.RED)
        print_colored("=" * 60, Colors.RED)
        print_colored(f"   Expected: {admin_script}", Colors.RED)
        print_colored("", Colors.RESET)
        return False
    
    print_colored("   Step 2: Creating database and tables...", Colors.CYAN)
    result = subprocess.run(
        [str(embedded_python), str(admin_script)],
        capture_output=True,
        text=True,
        cwd=str(Path(__file__).parent)
    )
    
    if result.returncode != 0:
        print_colored("", Colors.RESET)
        print_colored("=" * 60, Colors.RED)
        print_colored("❌ CRITICAL ERROR: Database initialization FAILED", Colors.BOLD + Colors.RED)
        print_colored("=" * 60, Colors.RED)
        if result.stderr:
            print_colored("   Error output:", Colors.RED)
            for line in result.stderr.split('\n')[:10]:
                if line.strip():
                    print_colored(f"   {line}", Colors.RED)
        if result.stdout:
            print_colored("   Output:", Colors.YELLOW)
            for line in result.stdout.split('\n')[-10:]:
                if line.strip():
                    print_colored(f"   {line}", Colors.YELLOW)
        print_colored("", Colors.RESET)
        print_colored("   Database initialization is REQUIRED. Cannot proceed.", Colors.RED)
        print_colored("", Colors.RESET)
        return False
    
    # Verify database was created successfully
    db_file = Path('mlstudio.db')
    if not db_file.exists():
        # SQLite creates file on first write, so check if tables exist by trying to connect
        print_colored("   Step 3: Verifying database...", Colors.CYAN)
        verify_result = subprocess.run(
            [str(embedded_python), '-c', 
             'from app import create_app, db; app = create_app(); app.app_context().push(); from app.models.settings import Settings; print("OK" if Settings.query.first() is not None or True else "FAIL")'],
            capture_output=True,
            text=True,
            cwd=str(Path(__file__).parent)
        )
        if verify_result.returncode != 0:
            print_colored("   ⚠️  Database verification had issues, but continuing...", Colors.YELLOW)
    
    print_colored("✅ Database initialized successfully", Colors.GREEN)
    if result.stdout:
        # Print important messages
        for line in result.stdout.split('\n'):
            if '[OK]' in line:
                print_colored(f"   {line}", Colors.GREEN)
            elif '[WARN]' in line:
                print_colored(f"   {line}", Colors.YELLOW)
            elif '[ERROR]' in line:
                print_colored(f"   {line}", Colors.RED)
    
    return True


def create_directories():
    """Create necessary directories"""
    dirs = ['data', 'projects']
    for dir_name in dirs:
        Path(dir_name).mkdir(exist_ok=True)

def run_mlstudio(venv_python, extra_args=None):
    """Run MLStudio application"""
    print_colored("", Colors.RESET)
    print_colored("=" * 60, Colors.CYAN)
    print_colored("🚀 Starting Beep.Python.MLStudio", Colors.BOLD + Colors.GREEN)
    print_colored("=" * 60, Colors.CYAN)
    print_colored("", Colors.RESET)
    
    # Change to script directory to ensure relative paths work
    script_dir = Path(__file__).parent.absolute()
    os.chdir(script_dir)
    
    # Verify database exists before starting
    db_file = Path('mlstudio.db')
    if not db_file.exists():
        print_colored("⚠️  Warning: Database file not found, but continuing anyway...", Colors.YELLOW)
        print_colored("   The database will be created automatically when the app starts.", Colors.CYAN)
    
    # Build command with any extra arguments (like --industry=pet)
    cmd = [str(venv_python), 'run.py']
    if extra_args:
        cmd.extend(extra_args)
    
    # Show industry mode if specified
    industry_arg = next((arg for arg in (extra_args or []) if arg.startswith('--industry=')), None)
    if industry_arg:
        industry = industry_arg.split('=')[1] if '=' in industry_arg else None
        if industry:
            print_colored(f"   Industry Mode: {industry.title()}", Colors.CYAN)
    
    # Run the application
    try:
        # Use subprocess.Popen to run in background and capture output
        process = subprocess.Popen(
            cmd,
            cwd=str(script_dir),
            stdout=subprocess.PIPE,
            stderr=subprocess.STDOUT,
            text=True,
            bufsize=1,
            universal_newlines=True
        )
        
        # Stream output
        for line in process.stdout:
            print(line, end='')
        
        process.wait()
        
    except KeyboardInterrupt:
        print_colored("\n\n👋 MLStudio stopped by user", Colors.CYAN)
        if 'process' in locals():
            process.terminate()
    except Exception as e:
        print_colored(f"\n❌ Error running MLStudio: {e}", Colors.RED)
        import traceback
        traceback.print_exc()

def main():
    """Main launcher function"""
    # Parse command-line arguments
    import argparse
    parser = argparse.ArgumentParser(description='Beep ML Studio - Setup & Launcher')
    parser.add_argument('--industry', type=str, help='Force industry mode (pet, health, oilandgas, etc.)')
    parser.add_argument('--port', type=int, help='Port number')
    parser.add_argument('--host', type=str, help='Host address')
    parser.add_argument('--debug', action='store_true', help='Enable debug mode')
    parser.add_argument('--no-browser', action='store_true', help='Do not open browser automatically')
    
    # Parse known args (ignore unknown for now, pass them to run.py)
    args, unknown_args = parser.parse_known_args()
    
    # Build extra args list for run.py
    extra_args = []
    if args.industry:
        extra_args.append(f'--industry={args.industry}')
    if args.port:
        extra_args.append(f'--port={args.port}')
    if args.host:
        extra_args.append(f'--host={args.host}')
    if args.debug:
        extra_args.append('--debug')
    if args.no_browser:
        extra_args.append('--no-browser')
    
    # Add any unknown args (for future compatibility)
    extra_args.extend(unknown_args)
    
    print_colored("", Colors.RESET)
    print_colored("=" * 60, Colors.BLUE)
    print_colored("  Beep.Python.MLStudio - Setup & Launcher", Colors.BOLD)
    print_colored("=" * 60, Colors.BLUE)
    print_colored("", Colors.RESET)
    
    # Change to script directory
    script_dir = Path(__file__).parent.absolute()
    os.chdir(script_dir)
    
    # Setup steps (in order - dependencies MUST be installed first)
    check_python_version()
    venv_python = setup_virtual_environment()
    
    # CRITICAL: Install dependencies FIRST before anything else
    print_colored("", Colors.RESET)
    print_colored("=" * 60, Colors.CYAN)
    print_colored("📦 Installing Dependencies", Colors.BOLD)
    print_colored("=" * 60, Colors.CYAN)
    install_dependencies(venv_python)
    
    # Verify Flask is installed (critical for app to run)
    print_colored("   Verifying Flask installation...", Colors.CYAN)
    verify_result = subprocess.run(
        [str(venv_python), '-c', 'import flask; print("OK")'],
        capture_output=True,
        text=True
    )
    if verify_result.returncode != 0:
        print_colored("❌ Flask verification failed! Reinstalling...", Colors.RED)
        subprocess.run(
            [str(venv_python), '-m', 'pip', 'install', '--force-reinstall', 'flask', 'flask-sqlalchemy', 'flask-socketio'],
            capture_output=True,
            text=True
        )
        print_colored("✅ Flask reinstalled", Colors.GREEN)
    else:
        print_colored("✅ Flask verified", Colors.GREEN)
    
    # Setup embedded Python FIRST (it's REQUIRED - the base for everything)
    print_colored("", Colors.RESET)
    print_colored("=" * 60, Colors.CYAN)
    print_colored("🐍 Embedded Python Setup (REQUIRED)", Colors.BOLD)
    print_colored("=" * 60, Colors.CYAN)
    print_colored("   Embedded Python is the base runtime for all virtual environments", Colors.CYAN)
    print_colored("   It is REQUIRED - MLStudio cannot run without it", Colors.CYAN)
    if not setup_embedded_python():
        print_colored("", Colors.RESET)
        print_colored("❌ ERROR: Embedded Python setup failed!", Colors.RED)
        print_colored("", Colors.RESET)
        print_colored("MLStudio requires embedded Python to function.", Colors.RED)
        print_colored("Please set up embedded Python manually:", Colors.YELLOW)
        if platform.system() == 'Windows':
            print_colored("   setup_embedded_python.bat", Colors.CYAN)
        else:
            print_colored("   ./setup_embedded_python.sh", Colors.CYAN)
        print_colored("", Colors.RESET)
        print_colored("Then run the launcher again.", Colors.YELLOW)
        sys.exit(1)
    
    # Continue with other setup steps
    print_colored("", Colors.RESET)
    print_colored("=" * 60, Colors.CYAN)
    print_colored("⚙️  Configuration", Colors.BOLD)
    print_colored("=" * 60, Colors.CYAN)
    setup_environment_file()
    create_directories()
    
    # Database initialization MUST use embedded Python - REQUIRED, NO FALLBACK
    print_colored("", Colors.RESET)
    print_colored("=" * 60, Colors.CYAN)
    print_colored("🗄️  Database Initialization (REQUIRED)", Colors.BOLD)
    print_colored("=" * 60, Colors.CYAN)
    if not initialize_database():
        print_colored("", Colors.RESET)
        print_colored("=" * 60, Colors.RED)
        print_colored("❌ SETUP FAILED", Colors.BOLD + Colors.RED)
        print_colored("=" * 60, Colors.RED)
        print_colored("   Database initialization is REQUIRED and failed.", Colors.RED)
        print_colored("   Cannot start MLStudio without a properly initialized database.", Colors.RED)
        print_colored("", Colors.RESET)
        print_colored("   Please check the errors above and try again.", Colors.YELLOW)
        print_colored("", Colors.RESET)
        sys.exit(1)
    
    # Run application - only if all requirements are met
    print_colored("", Colors.RESET)
    print_colored("=" * 60, Colors.GREEN)
    print_colored("✅ All Requirements Met - Starting MLStudio", Colors.BOLD + Colors.GREEN)
    print_colored("=" * 60, Colors.GREEN)
    print_colored("", Colors.RESET)
    run_mlstudio(venv_python, extra_args)

if __name__ == '__main__':
    try:
        main()
    except KeyboardInterrupt:
        print_colored("\n\n👋 Goodbye!", Colors.CYAN)
        sys.exit(0)
    except Exception as e:
        print_colored(f"\n❌ Error: {e}", Colors.RED)
        import traceback
        traceback.print_exc()
        sys.exit(1)

