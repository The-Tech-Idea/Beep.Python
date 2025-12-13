#!/usr/bin/env python3
"""
Beep.Python.Host.Admin - Cross-Platform Launcher
Automatically sets up and runs Host Admin
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
        message = message.replace('🚀', '[INFO]')
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
            sys.exit(1)
    
    if not venv_python.exists():
        print_colored(f"❌ Virtual environment setup failed! Python not found at: {venv_python}", Colors.RED)
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
    subprocess.run([str(venv_python), '-m', 'pip', 'install', '--upgrade', 'pip', '--quiet'],
                   capture_output=True)
    
    # Install dependencies
    result = subprocess.run([str(venv_python), '-m', 'pip', 'install', '-r', 'requirements.txt'],
                           capture_output=True, text=True)
    
    if result.returncode == 0:
        print_colored("✅ Dependencies installed successfully", Colors.GREEN)
    else:
        print_colored("❌ Failed to install dependencies!", Colors.RED)
        if result.stderr:
            print_colored(f"   Error: {result.stderr[-500:]}", Colors.RED)
        sys.exit(1)

def download_embedded_python():
    """Automatically download and install embedded Python if not found"""
    embedded_path = Path('python-embedded')
    if platform.system() == 'Windows':
        python_exe = embedded_path / 'python.exe'
        zip_file = Path('python-embedded.zip')
    else:
        python_exe = embedded_path / 'bin' / 'python3'
        zip_file = Path('python-embedded.tar.gz')
    
    if python_exe.exists():
        print_colored("✅ Embedded Python already installed", Colors.GREEN)
        return True
    
    print_colored("🐍 Embedded Python not found. Downloading automatically...", Colors.CYAN)
    print_colored("   This may take a few minutes...", Colors.CYAN)
    
    try:
        system = platform.system()
        machine = platform.machine().lower()
        
        if system == 'Windows':
            url = 'https://www.python.org/ftp/python/3.11.7/python-3.11.7-embed-amd64.zip'
            print_colored(f"   Downloading from: {url}", Colors.CYAN)
            
            if HAS_REQUESTS:
                response = requests.get(url, stream=True, timeout=30)
                response.raise_for_status()
                embedded_path.mkdir(exist_ok=True)
                with open(zip_file, 'wb') as f:
                    for chunk in response.iter_content(chunk_size=8192):
                        if chunk:
                            f.write(chunk)
            else:
                import urllib.request
                embedded_path.mkdir(exist_ok=True)
                urllib.request.urlretrieve(url, zip_file)
            
            print_colored("   Extracting Python...", Colors.CYAN)
            import zipfile
            with zipfile.ZipFile(zip_file, 'r') as zip_ref:
                zip_ref.extractall(embedded_path)
            zip_file.unlink()
            
            # Configure
            pth_file = embedded_path / 'python311._pth'
            if pth_file.exists():
                content = pth_file.read_text(encoding='utf-8')
                content = content.replace('#import site', 'import site')
                pth_file.write_text(content, encoding='utf-8')
            
            # Install pip
            print_colored("   Installing pip...", Colors.CYAN)
            pip_url = 'https://bootstrap.pypa.io/get-pip.py'
            get_pip = embedded_path / 'get-pip.py'
            
            if HAS_REQUESTS:
                response = requests.get(pip_url, timeout=30)
                get_pip.write_bytes(response.content)
            else:
                import urllib.request
                urllib.request.urlretrieve(pip_url, get_pip)
            
            subprocess.run([str(python_exe), str(get_pip)], check=True, capture_output=True)
            get_pip.unlink()
        else:
            # Linux/macOS
            os_name = 'linux' if system == 'Linux' else 'macos'
            arch = 'aarch64' if 'arm' in machine or 'aarch' in machine else 'x86_64'
            
            if os_name == 'linux':
                url = f'https://github.com/indygreg/python-build-standalone/releases/download/20231002/cpython-3.11.6+20231002-{arch}-unknown-linux-gnu-install_only.tar.gz'
            else:
                url = f'https://github.com/indygreg/python-build-standalone/releases/download/20231002/cpython-3.11.6+20231002-{arch}-apple-darwin-install_only.tar.gz'
            
            print_colored(f"   Downloading from: {url}", Colors.CYAN)
            
            if HAS_REQUESTS:
                response = requests.get(url, stream=True, timeout=60)
                response.raise_for_status()
                embedded_path.mkdir(exist_ok=True)
                with open(zip_file, 'wb') as f:
                    for chunk in response.iter_content(chunk_size=8192):
                        if chunk:
                            f.write(chunk)
            else:
                import urllib.request
                embedded_path.mkdir(exist_ok=True)
                urllib.request.urlretrieve(url, zip_file)
            
            print_colored("   Extracting Python...", Colors.CYAN)
            import tarfile
            with tarfile.open(zip_file, 'r:gz') as tar:
                tar.extractall(embedded_path)
                extracted_dirs = [d for d in embedded_path.iterdir() if d.is_dir()]
                if extracted_dirs:
                    import shutil
                    for item in extracted_dirs[0].iterdir():
                        shutil.move(str(item), str(embedded_path / item.name))
                    extracted_dirs[0].rmdir()
            zip_file.unlink()
            
            print_colored("   Installing pip...", Colors.CYAN)
            subprocess.run([str(python_exe), '-m', 'ensurepip'], check=True, capture_output=True)
        
        # Install requirements
        print_colored("   Installing application dependencies...", Colors.CYAN)
        subprocess.run([str(python_exe), '-m', 'pip', 'install', '--upgrade', 'pip', '--no-warn-script-location'],
                      check=True, capture_output=True)
        subprocess.run([str(python_exe), '-m', 'pip', 'install', '-r', 'requirements.txt', '--no-warn-script-location'],
                      check=True, capture_output=True)
        
        if python_exe.exists():
            print_colored("✅ Embedded Python installed successfully!", Colors.GREEN)
            return True
        return False
    except Exception as e:
        print_colored(f"❌ Error downloading embedded Python: {e}", Colors.RED)
        return False

def setup_embedded_python():
    """Set up embedded Python"""
    embedded_path = Path('python-embedded')
    if platform.system() == 'Windows':
        python_exe = embedded_path / 'python.exe'
    else:
        python_exe = embedded_path / 'bin' / 'python3'
    
    if python_exe.exists():
        return True
    
    if download_embedded_python():
        return True
    
    # Fallback to setup script
    if platform.system() == 'Windows':
        setup_script = Path('setup_embedded_python.bat')
    else:
        setup_script = Path('setup_embedded_python.sh')
    
    if setup_script.exists():
        print_colored("   Trying setup script...", Colors.YELLOW)
        try:
            if platform.system() == 'Windows':
                subprocess.run([str(setup_script)], check=False)
            else:
                os.chmod(setup_script, 0o755)
                subprocess.run(['bash', str(setup_script)], check=False)
            return python_exe.exists()
        except:
            pass
    
    return False

def run_hostadmin(venv_python):
    """Run Host Admin application"""
    print_colored("", Colors.RESET)
    print_colored("=" * 60, Colors.CYAN)
    print_colored("🚀 Starting Beep AI Server", Colors.BOLD + Colors.GREEN)
    print_colored("=" * 60, Colors.CYAN)
    print_colored("", Colors.RESET)
    
    script_dir = Path(__file__).parent.absolute()
    os.chdir(script_dir)
    
    cmd = [str(venv_python), 'run.py']
    
    try:
        process = subprocess.Popen(cmd, cwd=str(script_dir), stdout=subprocess.PIPE,
                                   stderr=subprocess.STDOUT, text=True, bufsize=1)
        for line in process.stdout:
            print(line, end='')
        process.wait()
    except KeyboardInterrupt:
        print_colored("\n\n👋 Host Admin stopped by user", Colors.CYAN)
        if 'process' in locals():
            process.terminate()

def main():
    """Main launcher function"""
    print_colored("", Colors.RESET)
    print_colored("=" * 60, Colors.BLUE)
    print_colored("  Beep AI Server - Setup & Launcher", Colors.BOLD)
    print_colored("=" * 60, Colors.BLUE)
    print_colored("", Colors.RESET)
    
    script_dir = Path(__file__).parent.absolute()
    os.chdir(script_dir)
    
    check_python_version()
    venv_python = setup_virtual_environment()
    
    print_colored("", Colors.RESET)
    print_colored("=" * 60, Colors.CYAN)
    print_colored("📦 Installing Dependencies", Colors.BOLD)
    print_colored("=" * 60, Colors.CYAN)
    install_dependencies(venv_python)
    
    print_colored("", Colors.RESET)
    print_colored("=" * 60, Colors.CYAN)
    print_colored("🐍 Embedded Python Setup", Colors.BOLD)
    print_colored("=" * 60, Colors.CYAN)
    if not setup_embedded_python():
        print_colored("", Colors.RESET)
        print_colored("=" * 60, Colors.RED)
        print_colored("❌ Embedded Python setup failed!", Colors.BOLD + Colors.RED)
        print_colored("=" * 60, Colors.RED)
        print_colored("   Cannot proceed without embedded Python.", Colors.YELLOW)
        print_colored("   Please check the error messages above and try again.", Colors.YELLOW)
        print_colored("", Colors.RESET)
        sys.exit(1)
    
    print_colored("", Colors.RESET)
    print_colored("=" * 60, Colors.GREEN)
    print_colored("✅ All Requirements Met - Starting Host Admin", Colors.BOLD + Colors.GREEN)
    print_colored("=" * 60, Colors.GREEN)
    print_colored("", Colors.RESET)
    run_hostadmin(venv_python)

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

