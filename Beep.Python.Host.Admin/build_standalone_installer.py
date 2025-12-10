"""
Build Standalone Installer for Beep.Python Host Admin
======================================================
This script:
1. Builds the main application (BeepPythonHost.exe)
2. Bundles it into a single installer executable (BeepPython-Setup.exe)

The resulting BeepPython-Setup.exe is a SINGLE FILE that:
- Contains everything needed
- Shows a wizard to pick install location
- Installs app with all settings stored in install folder
- Works on any machine without Python installed
"""
import subprocess
import sys
import shutil
from pathlib import Path


def main():
    project_root = Path(__file__).parent
    
    print("=" * 60)
    print("Building Beep.Python Standalone Installer")
    print("=" * 60)
    print()
    
    # Step 1: Build the main application
    print("[1/3] Building main application...")
    print("-" * 40)
    
    app_spec = project_root / "beep_python_host.spec"
    if not app_spec.exists():
        print(f"ERROR: {app_spec} not found!")
        return 1
    
    result = subprocess.run(
        [sys.executable, "-m", "PyInstaller", "--clean", "--noconfirm", str(app_spec)],
        cwd=str(project_root)
    )
    
    if result.returncode != 0:
        print("ERROR: Failed to build main application!")
        return 1
    
    # Verify app was built
    app_dir = project_root / "dist" / "BeepPythonHost"
    if not app_dir.exists():
        print(f"ERROR: App not found at {app_dir}")
        return 1
    
    print(f"Main application built: {app_dir}")
    print()
    
    # Step 2: Build the standalone installer
    print("[2/3] Building standalone installer...")
    print("-" * 40)
    
    installer_spec = project_root / "installer" / "standalone_installer.spec"
    if not installer_spec.exists():
        print(f"ERROR: {installer_spec} not found!")
        return 1
    
    result = subprocess.run(
        [sys.executable, "-m", "PyInstaller", "--clean", "--noconfirm", str(installer_spec)],
        cwd=str(project_root)
    )
    
    if result.returncode != 0:
        print("ERROR: Failed to build installer!")
        return 1
    
    # Step 3: Move installer to dist folder
    print()
    print("[3/3] Finalizing...")
    print("-" * 40)
    
    installer_exe = project_root / "dist" / "BeepPython-Setup.exe"
    if not installer_exe.exists():
        # Check if it's in a different location
        for possible in [
            project_root / "dist" / "BeepPython-Setup" / "BeepPython-Setup.exe",
        ]:
            if possible.exists():
                shutil.copy2(possible, installer_exe)
                break
    
    if installer_exe.exists():
        size_mb = installer_exe.stat().st_size / (1024 * 1024)
        print()
        print("=" * 60)
        print("BUILD SUCCESSFUL!")
        print("=" * 60)
        print()
        print(f"Standalone Installer: {installer_exe}")
        print(f"Size: {size_mb:.1f} MB")
        print()
        print("This single file contains everything needed to install")
        print("Beep.Python Host Admin on any Windows machine.")
        print()
        print("To distribute:")
        print(f"  1. Copy '{installer_exe.name}' to any machine")
        print("  2. Run it - wizard will guide installation")
        print("  3. All settings stored in chosen install folder")
        return 0
    else:
        print("WARNING: Installer exe not found at expected location")
        print("Check the dist folder for the output.")
        return 1


if __name__ == "__main__":
    sys.exit(main())
