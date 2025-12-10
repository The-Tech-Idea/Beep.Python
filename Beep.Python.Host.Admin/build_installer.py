"""
Build script for creating cross-platform installer
Run: python build_installer.py
"""
import subprocess
import sys
import platform
from pathlib import Path


def main():
    project_root = Path(__file__).parent
    
    print("=" * 60)
    print("Beep.Python Installer Builder")
    print("=" * 60)
    print(f"Platform: {platform.system()} {platform.machine()}")
    print()
    
    # Step 1: Build the main application first
    print("[1/2] Building main application...")
    spec_file = project_root / "beep_python_host.spec"
    
    if not spec_file.exists():
        print(f"ERROR: Main spec file not found: {spec_file}")
        return 1
    
    result = subprocess.run(
        [sys.executable, "-m", "PyInstaller", "--clean", "--noconfirm", str(spec_file)],
        cwd=project_root
    )
    
    if result.returncode != 0:
        print("ERROR: Failed to build main application")
        return 1
    
    print()
    print("[2/2] Building installer wizard...")
    
    # Step 2: Build the installer
    installer_spec = project_root / "installer" / "installer.spec"
    
    if not installer_spec.exists():
        print(f"ERROR: Installer spec file not found: {installer_spec}")
        return 1
    
    result = subprocess.run(
        [sys.executable, "-m", "PyInstaller", "--clean", "--noconfirm", str(installer_spec)],
        cwd=project_root
    )
    
    if result.returncode != 0:
        print("ERROR: Failed to build installer")
        return 1
    
    # Output location
    dist_dir = project_root / "dist"
    system = platform.system()
    
    if system == "Windows":
        installer_name = "BeepPythonSetup.exe"
    elif system == "Darwin":
        installer_name = "BeepPython Setup.app"
    else:
        installer_name = "BeepPythonSetup"
    
    installer_path = dist_dir / installer_name
    
    print()
    print("=" * 60)
    print("Build Complete!")
    print("=" * 60)
    print(f"Installer: {installer_path}")
    print()
    print("This installer can be distributed to users.")
    print("It includes:")
    print("  - Setup wizard with GUI")
    print("  - All application files")
    print("  - Configuration options")
    print()
    
    return 0


if __name__ == "__main__":
    sys.exit(main())
