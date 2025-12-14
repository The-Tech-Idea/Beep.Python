#!/usr/bin/env python3
"""
Windows Installer Creation Script
Creates Windows installer using Inno Setup
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
    
    version_file = Path(__file__).parent.parent / "VERSION"
    if version_file.exists():
        return version_file.read_text().strip()
    
    return "1.0.0"


def find_inno_setup():
    """Find Inno Setup compiler"""
    # Common installation paths
    paths = [
        r"C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
        r"C:\Program Files\Inno Setup 6\ISCC.exe",
        r"C:\Program Files (x86)\Inno Setup 5\ISCC.exe",
        r"C:\Program Files\Inno Setup 5\ISCC.exe",
    ]
    
    for path in paths:
        if Path(path).exists():
            return path
    
    # Try in PATH
    try:
        result = subprocess.run(
            ["where", "iscc"],
            capture_output=True,
            text=True,
            check=False
        )
        if result.returncode == 0:
            return result.stdout.strip().split('\n')[0]
    except:
        pass
    
    return None


def create_installer(version):
    """Create Windows installer"""
    print("[Windows Installer] Creating Windows installer...")
    
    windows_dist = Path("dist") / "windows"
    if not windows_dist.exists():
        print("[Windows Installer] ERROR: Windows build not found. Run build_windows.py first.")
        return False
    
    # Find Inno Setup
    iscc_path = find_inno_setup()
    if not iscc_path:
        print("[Windows Installer] ERROR: Inno Setup not found!")
        print("[Windows Installer] Please install Inno Setup from https://jrsoftware.org/isdl.php")
        return False
    
    # Update installer script with version
    iss_file = Path("installer") / "beep_python_installer.iss"
    if not iss_file.exists():
        print(f"[Windows Installer] ERROR: {iss_file} not found!")
        return False
    
    # Read and update version
    iss_content = iss_file.read_text()
    # Replace version in script
    lines = iss_content.split('\n')
    updated_lines = []
    for line in lines:
        if line.startswith('#define MyAppVersion'):
            updated_lines.append(f'#define MyAppVersion "{version}"')
        else:
            updated_lines.append(line)
    
    # Write temporary ISS file
    temp_iss = Path("dist") / "windows" / "beep_python_installer_temp.iss"
    temp_iss.parent.mkdir(parents=True, exist_ok=True)
    temp_iss.write_text('\n'.join(updated_lines))
    
    # Update output directory in ISS to point to dist/windows
    # This is a simplified approach - in production, you'd want to modify the ISS file properly
    print(f"[Windows Installer] Using Inno Setup: {iscc_path}")
    print(f"[Windows Installer] Compiling installer script...")
    
    # Run Inno Setup compiler
    result = subprocess.run(
        [iscc_path, str(iss_file)],
        check=False,
        cwd=Path("installer")
    )
    
    # Clean up temp file
    if temp_iss.exists():
        temp_iss.unlink()
    
    if result.returncode == 0:
        # Check for output file
        installer_output = Path("dist") / "installer" / f"BeepPythonHost-Setup-{version}.exe"
        if installer_output.exists():
            # Move to windows dist
            windows_installer = Path("dist") / "windows" / installer_output.name
            shutil.move(installer_output, windows_installer)
            print(f"[Windows Installer] Created: {windows_installer}")
            return True
        else:
            print("[Windows Installer] WARNING: Installer compiled but output file not found")
            print(f"[Windows Installer] Expected: {installer_output}")
            return False
    else:
        print("[Windows Installer] ERROR: Inno Setup compilation failed")
        return False


def main():
    parser = argparse.ArgumentParser(description="Create Windows installer")
    parser.add_argument("--version", help="Version number (e.g., 1.0.0)")
    args = parser.parse_args()
    
    version = get_version(args.version)
    print(f"[Windows Installer] Creating installer for version {version}")
    
    # Change to project root
    project_root = Path(__file__).parent.parent
    os.chdir(project_root)
    
    if create_installer(version):
        print("[Windows Installer] SUCCESS: Installer creation complete!")
    else:
        print("[Windows Installer] ERROR: Installer creation failed")
        sys.exit(1)


if __name__ == "__main__":
    main()

