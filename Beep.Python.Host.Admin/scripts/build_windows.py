#!/usr/bin/env python3
"""
Windows Build Script
Builds Beep.Python Host Admin for Windows using PyInstaller
"""
import os
import sys
import subprocess
import shutil
import argparse
from pathlib import Path


def get_version(version_arg=None):
    """Get version from argument, VERSION file, or git tag"""
    if version_arg:
        return version_arg
    
    # Try VERSION file
    version_file = Path(__file__).parent.parent / "VERSION"
    if version_file.exists():
        return version_file.read_text().strip()
    
    # Try git tag
    try:
        result = subprocess.run(
            ["git", "describe", "--tags", "--abbrev=0"],
            capture_output=True,
            text=True,
            check=True
        )
        version = result.stdout.strip().lstrip('v')
        return version
    except:
        pass
    
    return "1.0.0"  # Default


def setup_embedded_python():
    """Setup Windows embedded Python"""
    print("[Windows Build] Setting up embedded Python...")
    
    embedded_dir = Path("python-embedded")
    if embedded_dir.exists() and (embedded_dir / "python.exe").exists():
        print("[Windows Build] Embedded Python already exists")
        return True
    
    # Run setup script
    setup_script = Path("setup_embedded_python.bat")
    if not setup_script.exists():
        print(f"[Windows Build] ERROR: {setup_script} not found!")
        return False
    
    result = subprocess.run([str(setup_script)], shell=True, check=False)
    if result.returncode != 0:
        print("[Windows Build] ERROR: Failed to setup embedded Python")
        return False
    
    return True


def install_dependencies():
    """Install Python dependencies"""
    print("[Windows Build] Installing dependencies...")
    
    embedded_python = Path("python-embedded") / "python.exe"
    if not embedded_python.exists():
        print("[Windows Build] ERROR: Embedded Python not found!")
        return False
    
    # Install PyInstaller if not already installed
    result = subprocess.run(
        [str(embedded_python), "-m", "pip", "install", "pyinstaller"],
        check=False
    )
    if result.returncode != 0:
        print("[Windows Build] WARNING: Failed to install PyInstaller")
        # Try with system Python
        result = subprocess.run(
            [sys.executable, "-m", "pip", "install", "pyinstaller"],
            check=False
        )
        if result.returncode != 0:
            return False
    
    return True


def build_with_pyinstaller(version):
    """Build executable with PyInstaller"""
    print("[Windows Build] Building with PyInstaller...")
    
    # Clean previous builds
    build_dir = Path("build")
    dist_dir = Path("dist")
    if build_dir.exists():
        shutil.rmtree(build_dir)
    if dist_dir.exists():
        shutil.rmtree(dist_dir)
    
    # Use embedded Python if available, otherwise system Python
    python_exe = Path("python-embedded") / "python.exe"
    if not python_exe.exists():
        python_exe = Path(sys.executable)
    
    # Run PyInstaller
    spec_file = Path("beep_python_host.spec")
    if not spec_file.exists():
        print(f"[Windows Build] ERROR: {spec_file} not found!")
        return False
    
    result = subprocess.run(
        [str(python_exe), "-m", "PyInstaller", str(spec_file), "--clean", "--noconfirm"],
        check=False
    )
    
    if result.returncode != 0:
        print("[Windows Build] ERROR: PyInstaller build failed")
        return False
    
    # Move build output to platform-specific directory
    windows_dist = Path("dist") / "windows"
    windows_dist.mkdir(parents=True, exist_ok=True)
    
    # Copy PyInstaller output
    pyinstaller_dist = Path("dist") / "BeepPythonHost"
    if pyinstaller_dist.exists():
        shutil.copytree(pyinstaller_dist, windows_dist / "BeepPythonHost", dirs_exist_ok=True)
    
    # Copy python-embedded
    embedded_dir = Path("python-embedded")
    if embedded_dir.exists():
        shutil.copytree(embedded_dir, windows_dist / "python-embedded", dirs_exist_ok=True)
    
    # Copy launcher scripts
    launcher = Path("launch_beep_python_admin.cmd")
    if launcher.exists():
        shutil.copy2(launcher, windows_dist)
    
    print(f"[Windows Build] Build complete! Output: {windows_dist}")
    return True


def main():
    parser = argparse.ArgumentParser(description="Build Beep.Python for Windows")
    parser.add_argument("--version", help="Version number (e.g., 1.0.0)")
    args = parser.parse_args()
    
    version = get_version(args.version)
    print(f"[Windows Build] Building version {version}")
    
    # Change to project root
    project_root = Path(__file__).parent.parent
    os.chdir(project_root)
    
    # Build steps
    if not setup_embedded_python():
        sys.exit(1)
    
    if not install_dependencies():
        sys.exit(1)
    
    if not build_with_pyinstaller(version):
        sys.exit(1)
    
    print("[Windows Build] SUCCESS: Windows build complete!")


if __name__ == "__main__":
    main()

