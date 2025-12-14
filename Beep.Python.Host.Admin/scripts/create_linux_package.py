#!/usr/bin/env python3
"""
Linux Package Creation Script
Creates .deb, .rpm, and/or AppImage packages for Linux
"""
import os
import sys
import subprocess
import shutil
import argparse
from pathlib import Path
from datetime import datetime


def get_version(version_arg=None):
    """Get version from argument, VERSION file, or git tag"""
    if version_arg:
        return version_arg
    
    version_file = Path(__file__).parent.parent / "VERSION"
    if version_file.exists():
        return version_file.read_text().strip()
    
    return "1.0.0"


def create_deb_package(version):
    """Create .deb package"""
    print("[Linux Package] Creating .deb package...")
    
    linux_dist = Path("dist") / "linux"
    if not linux_dist.exists():
        print("[Linux Package] ERROR: Linux build not found. Run build_linux.py first.")
        return False
    
    # Create package structure
    package_name = "beep-python-host-admin"
    package_dir = Path("dist") / "linux" / f"{package_name}_{version}"
    package_dir.mkdir(parents=True, exist_ok=True)
    
    # DEBIAN control directory
    debian_dir = package_dir / "DEBIAN"
    debian_dir.mkdir(exist_ok=True)
    
    # Control file
    control_content = f"""Package: {package_name}
Version: {version}
Section: utils
Priority: optional
Architecture: amd64
Depends: libc6 (>= 2.17)
Maintainer: The Tech Idea <info@thetechidea.com>
Description: Professional Python Environment & LLM Management System
 Beep.Python Host Admin is a comprehensive web application for managing
 Python environments, Large Language Models (LLMs), RAG systems, and AI infrastructure.
"""
    (debian_dir / "control").write_text(control_content)
    
    # Postinst script
    postinst_content = """#!/bin/bash
# Post-installation script
echo "Beep.Python Host Admin installed successfully!"
"""
    (debian_dir / "postinst").write_text(postinst_content)
    os.chmod(debian_dir / "postinst", 0o755)
    
    # Prerm script
    prerm_content = """#!/bin/bash
# Pre-removal script
echo "Removing Beep.Python Host Admin..."
"""
    (debian_dir / "prerm").write_text(prerm_content)
    os.chmod(debian_dir / "prerm", 0o755)
    
    # Copy application files
    app_dir = package_dir / "opt" / "beep-python"
    app_dir.mkdir(parents=True, exist_ok=True)
    
    # Copy build output
    if (linux_dist / "BeepPythonHost").exists():
        shutil.copytree(linux_dist / "BeepPythonHost", app_dir / "BeepPythonHost", dirs_exist_ok=True)
    if (linux_dist / "python-embedded").exists():
        shutil.copytree(linux_dist / "python-embedded", app_dir / "python-embedded", dirs_exist_ok=True)
    if (linux_dist / "launch_beep_python_admin.sh").exists():
        shutil.copy2(linux_dist / "launch_beep_python_admin.sh", app_dir)
    
    # Create desktop entry
    desktop_dir = package_dir / "usr" / "share" / "applications"
    desktop_dir.mkdir(parents=True, exist_ok=True)
    
    desktop_content = f"""[Desktop Entry]
Name=Beep.Python Host Admin
Comment=Professional Python Environment & LLM Management System
Exec=/opt/beep-python/launch_beep_python_admin.sh
Icon=beep-python
Terminal=false
Type=Application
Categories=Development;Utility;
"""
    (desktop_dir / "beep-python-host-admin.desktop").write_text(desktop_content)
    
    # Create symlink in /usr/local/bin
    bin_dir = package_dir / "usr" / "local" / "bin"
    bin_dir.mkdir(parents=True, exist_ok=True)
    
    # Build .deb
    deb_file = Path("dist") / "linux" / f"{package_name}_{version}_amd64.deb"
    
    try:
        result = subprocess.run(
            ["dpkg-deb", "--build", str(package_dir), str(deb_file)],
            check=False,
            capture_output=True
        )
        if result.returncode == 0:
            print(f"[Linux Package] Created: {deb_file}")
            return True
        else:
            print(f"[Linux Package] WARNING: dpkg-deb failed: {result.stderr.decode()}")
            print("[Linux Package] Install dpkg-dev to create .deb packages")
            return False
    except FileNotFoundError:
        print("[Linux Package] WARNING: dpkg-deb not found. Install dpkg-dev to create .deb packages")
        return False


def create_rpm_package(version):
    """Create .rpm package"""
    print("[Linux Package] Creating .rpm package...")
    
    # This is a simplified version - full RPM creation requires rpmbuild
    print("[Linux Package] RPM package creation requires rpmbuild")
    print("[Linux Package] Skipping RPM creation (not implemented)")
    return False


def create_appimage(version):
    """Create AppImage package"""
    print("[Linux Package] Creating AppImage...")
    
    linux_dist = Path("dist") / "linux"
    if not linux_dist.exists():
        print("[Linux Package] ERROR: Linux build not found. Run build_linux.py first.")
        return False
    
    # AppImage requires appimagetool
    try:
        result = subprocess.run(
            ["which", "appimagetool"],
            capture_output=True,
            check=False
        )
        if result.returncode != 0:
            print("[Linux Package] WARNING: appimagetool not found. Install appimagetool to create AppImage")
            return False
    except:
        print("[Linux Package] WARNING: Cannot check for appimagetool")
        return False
    
    print("[Linux Package] AppImage creation not fully implemented")
    print("[Linux Package] Requires AppDir structure and appimagetool")
    return False


def create_tarball(version):
    """Create .tar.gz archive"""
    print("[Linux Package] Creating .tar.gz archive...")
    
    linux_dist = Path("dist") / "linux"
    if not linux_dist.exists():
        print("[Linux Package] ERROR: Linux build not found. Run build_linux.py first.")
        return False
    
    archive_name = f"beep-python-host-admin-{version}-linux-amd64.tar.gz"
    archive_path = Path("dist") / "linux" / archive_name
    
    # Create archive
    result = subprocess.run(
        ["tar", "-czf", str(archive_path), "-C", str(linux_dist.parent), "linux"],
        check=False
    )
    
    if result.returncode == 0:
        print(f"[Linux Package] Created: {archive_path}")
        return True
    else:
        print("[Linux Package] ERROR: Failed to create tarball")
        return False


def main():
    parser = argparse.ArgumentParser(description="Create Linux packages")
    parser.add_argument("--version", help="Version number (e.g., 1.0.0)")
    parser.add_argument(
        "--format",
        choices=["deb", "rpm", "appimage", "tarball", "all"],
        default="tarball",
        help="Package format to create (default: tarball)"
    )
    args = parser.parse_args()
    
    version = get_version(args.version)
    print(f"[Linux Package] Creating packages for version {version}")
    
    # Change to project root
    project_root = Path(__file__).parent.parent
    os.chdir(project_root)
    
    success = True
    
    if args.format == "all":
        formats = ["deb", "rpm", "appimage", "tarball"]
    else:
        formats = [args.format]
    
    for fmt in formats:
        if fmt == "deb":
            if not create_deb_package(version):
                success = False
        elif fmt == "rpm":
            if not create_rpm_package(version):
                success = False
        elif fmt == "appimage":
            if not create_appimage(version):
                success = False
        elif fmt == "tarball":
            if not create_tarball(version):
                success = False
    
    if success:
        print("[Linux Package] SUCCESS: Package creation complete!")
    else:
        print("[Linux Package] WARNING: Some packages may not have been created")
        # Don't exit with error for warnings


if __name__ == "__main__":
    main()

