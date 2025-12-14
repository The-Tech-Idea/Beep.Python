#!/usr/bin/env python3
"""
Linux Build Script
Builds Beep.Python Host Admin for Linux using PyInstaller
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
    """Setup Linux embedded Python"""
    print("[Linux Build] Setting up embedded Python...")
    
    embedded_dir = Path("python-embedded")
    if embedded_dir.exists() and (embedded_dir / "bin" / "python3").exists():
        print("[Linux Build] Embedded Python already exists")
        return True
    
    # Run setup script
    setup_script = Path("setup_embedded_python.sh")
    if not setup_script.exists():
        print(f"[Linux Build] ERROR: {setup_script} not found!")
        return False
    
    # Make executable
    os.chmod(setup_script, 0o755)
    
    # Run in non-interactive mode for CI
    env = os.environ.copy()
    env["CI"] = "1"
    
    result = subprocess.run(
        [str(setup_script)],
        env=env,
        check=False
    )
    
    if result.returncode != 0:
        print("[Linux Build] ERROR: Failed to setup embedded Python")
        return False
    
    return True


def install_dependencies():
    """Install Python dependencies"""
    print("[Linux Build] Installing dependencies...")
    
    embedded_python = Path("python-embedded") / "bin" / "python3"
    if not embedded_python.exists():
        print("[Linux Build] ERROR: Embedded Python not found!")
        return False
    
    # Install PyInstaller if not already installed
    result = subprocess.run(
        [str(embedded_python), "-m", "pip", "install", "pyinstaller"],
        check=False
    )
    if result.returncode != 0:
        print("[Linux Build] WARNING: Failed to install PyInstaller with embedded Python")
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
    print("[Linux Build] Building with PyInstaller...")
    
    # Clean previous builds
    build_dir = Path("build")
    dist_dir = Path("dist")
    if build_dir.exists():
        shutil.rmtree(build_dir)
    if dist_dir.exists():
        shutil.rmtree(dist_dir)
    
    # Use embedded Python if available, otherwise system Python
    python_exe = Path("python-embedded") / "bin" / "python3"
    if not python_exe.exists():
        python_exe = Path(sys.executable)
    
    # Run PyInstaller
    spec_file = Path("beep_python_host.spec")
    if not spec_file.exists():
        print(f"[Linux Build] ERROR: {spec_file} not found!")
        return False
    
    result = subprocess.run(
        [str(python_exe), "-m", "PyInstaller", str(spec_file), "--clean", "--noconfirm"],
        check=False
    )
    
    if result.returncode != 0:
        print("[Linux Build] ERROR: PyInstaller build failed")
        return False
    
    # Move build output to platform-specific directory
    linux_dist = Path("dist") / "linux"
    linux_dist.mkdir(parents=True, exist_ok=True)
    
    # Copy PyInstaller output
    pyinstaller_dist = Path("dist") / "BeepPythonHost"
    if pyinstaller_dist.exists():
        shutil.copytree(pyinstaller_dist, linux_dist / "BeepPythonHost", dirs_exist_ok=True)
    
    # Copy python-embedded
    embedded_dir = Path("python-embedded")
    if embedded_dir.exists():
        shutil.copytree(embedded_dir, linux_dist / "python-embedded", dirs_exist_ok=True)
    
    # Copy launcher scripts
    launcher = Path("launch_beep_python_admin.sh")
    if launcher.exists():
        shutil.copy2(launcher, linux_dist)
        os.chmod(linux_dist / launcher.name, 0o755)
    
    print(f"[Linux Build] Build complete! Output: {linux_dist}")
    return True


def main():
    parser = argparse.ArgumentParser(description="Build Beep.Python for Linux")
    parser.add_argument("--version", help="Version number (e.g., 1.0.0)")
    args = parser.parse_args()
    
    version = get_version(args.version)
    print(f"[Linux Build] Building version {version}")
    
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
    
    print("[Linux Build] SUCCESS: Linux build complete!")


if __name__ == "__main__":
    main()

