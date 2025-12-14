#!/usr/bin/env python3
"""
macOS Package Creation Script
Creates .dmg and/or .pkg packages for macOS
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


def create_dmg(version):
    """Create .dmg disk image"""
    print("[macOS Package] Creating .dmg disk image...")
    
    macos_dist = Path("dist") / "macos"
    if not macos_dist.exists():
        print("[macOS Package] ERROR: macOS build not found. Run build_macos.py first.")
        return False
    
    dmg_name = f"BeepPythonHost-{version}-macOS.dmg"
    dmg_path = Path("dist") / "macos" / dmg_name
    
    # Create temporary directory for DMG contents
    dmg_temp = Path("dist") / "macos" / "dmg_temp"
    if dmg_temp.exists():
        shutil.rmtree(dmg_temp)
    dmg_temp.mkdir(parents=True, exist_ok=True)
    
    # Copy application bundle or files
    app_name = "BeepPythonHost.app"
    app_bundle = dmg_temp / app_name
    
    # Create .app bundle structure
    app_bundle.mkdir()
    contents_dir = app_bundle / "Contents"
    contents_dir.mkdir()
    
    macos_dir = contents_dir / "MacOS"
    macos_dir.mkdir()
    resources_dir = contents_dir / "Resources"
    resources_dir.mkdir()
    
    # Copy executable
    if (macos_dist / "BeepPythonHost" / "BeepPythonHost").exists():
        shutil.copy2(
            macos_dist / "BeepPythonHost" / "BeepPythonHost",
            macos_dir / "BeepPythonHost"
        )
        os.chmod(macos_dir / "BeepPythonHost", 0o755)
    
    # Copy python-embedded
    if (macos_dist / "python-embedded").exists():
        shutil.copytree(
            macos_dist / "python-embedded",
            contents_dir / "Resources" / "python-embedded",
            dirs_exist_ok=True
        )
    
    # Create Info.plist
    info_plist = f"""<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleExecutable</key>
    <string>BeepPythonHost</string>
    <key>CFBundleIdentifier</key>
    <string>com.thetechidea.beep-python-host-admin</string>
    <key>CFBundleName</key>
    <string>Beep.Python Host Admin</string>
    <key>CFBundleVersion</key>
    <string>{version}</string>
    <key>CFBundleShortVersionString</key>
    <string>{version}</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>LSMinimumSystemVersion</key>
    <string>10.13</string>
</dict>
</plist>
"""
    (contents_dir / "Info.plist").write_text(info_plist)
    
    # Create symlink to Applications
    applications_link = dmg_temp / "Applications"
    applications_link.symlink_to("/Applications")
    
    # Try using create-dmg if available
    try:
        result = subprocess.run(
            [
                "create-dmg",
                "--volname", "BeepPythonHost",
                "--window-pos", "200", "120",
                "--window-size", "800", "400",
                "--icon-size", "100",
                "--icon", app_name, "200", "190",
                "--hide-extension", app_name,
                "--app-drop-link", "600", "185",
                str(dmg_path),
                str(dmg_temp)
            ],
            check=False,
            capture_output=True
        )
        if result.returncode == 0:
            print(f"[macOS Package] Created: {dmg_path}")
            shutil.rmtree(dmg_temp)
            return True
    except FileNotFoundError:
        pass
    
    # Fallback to hdiutil
    try:
        # Remove existing DMG
        if dmg_path.exists():
            dmg_path.unlink()
        
        # Create DMG
        result = subprocess.run(
            [
                "hdiutil", "create",
                "-volname", "BeepPythonHost",
                "-srcfolder", str(dmg_temp),
                "-ov",
                "-format", "UDZO",
                str(dmg_path)
            ],
            check=False,
            capture_output=True
        )
        
        if result.returncode == 0:
            print(f"[macOS Package] Created: {dmg_path}")
            shutil.rmtree(dmg_temp)
            return True
        else:
            print(f"[macOS Package] ERROR: hdiutil failed: {result.stderr.decode()}")
            shutil.rmtree(dmg_temp)
            return False
    except Exception as e:
        print(f"[macOS Package] ERROR: Failed to create DMG: {e}")
        if dmg_temp.exists():
            shutil.rmtree(dmg_temp)
        return False


def create_pkg(version):
    """Create .pkg installer"""
    print("[macOS Package] Creating .pkg installer...")
    
    macos_dist = Path("dist") / "macos"
    if not macos_dist.exists():
        print("[macOS Package] ERROR: macOS build not found. Run build_macos.py first.")
        return False
    
    pkg_name = f"BeepPythonHost-{version}-macOS.pkg"
    pkg_path = Path("dist") / "macos" / pkg_name
    
    # Create package root
    pkg_root = Path("dist") / "macos" / "pkg_root"
    if pkg_root.exists():
        shutil.rmtree(pkg_root)
    
    app_dir = pkg_root / "Applications"
    app_dir.mkdir(parents=True, exist_ok=True)
    
    # Copy application (simplified - would need proper .app bundle)
    if (macos_dist / "BeepPythonHost").exists():
        shutil.copytree(
            macos_dist / "BeepPythonHost",
            app_dir / "BeepPythonHost",
            dirs_exist_ok=True
        )
    
    # Create distribution.xml
    dist_xml = f"""<?xml version="1.0" encoding="utf-8"?>
<installer-gui-script minSpecVersion="1">
    <title>Beep.Python Host Admin {version}</title>
    <organization>com.thetechidea</organization>
    <domains enable_localSystem="true"/>
    <options customize="never" require-scripts="false" rootVolumeOnly="true"/>
    <pkg-ref id="com.thetechidea.beep-python-host-admin"/>
    <options customize="never" require-scripts="false"/>
    <choices-outline>
        <line choice="com.thetechidea.beep-python-host-admin"/>
    </choices-outline>
    <choice id="com.thetechidea.beep-python-host-admin"/>
    <pkg-ref id="com.thetechidea.beep-python-host-admin" version="{version}" onConclusion="none">BeepPythonHost.pkg</pkg-ref>
</installer-gui-script>
"""
    (pkg_root / "distribution.xml").write_text(dist_xml)
    
    # Build package using pkgbuild
    try:
        # First create component package
        component_pkg = Path("dist") / "macos" / "BeepPythonHost.pkg"
        result = subprocess.run(
            [
                "pkgbuild",
                "--root", str(pkg_root),
                "--identifier", "com.thetechidea.beep-python-host-admin",
                "--version", version,
                "--install-location", "/",
                str(component_pkg)
            ],
            check=False,
            capture_output=True
        )
        
        if result.returncode != 0:
            print(f"[macOS Package] ERROR: pkgbuild failed: {result.stderr.decode()}")
            return False
        
        # Create product archive
        result = subprocess.run(
            [
                "productbuild",
                "--distribution", str(pkg_root / "distribution.xml"),
                "--package-path", str(component_pkg.parent),
                "--resources", str(pkg_root),
                str(pkg_path)
            ],
            check=False,
            capture_output=True
        )
        
        if result.returncode == 0:
            print(f"[macOS Package] Created: {pkg_path}")
            if component_pkg.exists():
                component_pkg.unlink()
            return True
        else:
            print(f"[macOS Package] ERROR: productbuild failed: {result.stderr.decode()}")
            return False
    except FileNotFoundError:
        print("[macOS Package] ERROR: pkgbuild or productbuild not found")
        return False


def main():
    parser = argparse.ArgumentParser(description="Create macOS packages")
    parser.add_argument("--version", help="Version number (e.g., 1.0.0)")
    parser.add_argument(
        "--format",
        choices=["dmg", "pkg", "all"],
        default="dmg",
        help="Package format to create (default: dmg)"
    )
    args = parser.parse_args()
    
    version = get_version(args.version)
    print(f"[macOS Package] Creating packages for version {version}")
    
    # Change to project root
    project_root = Path(__file__).parent.parent
    os.chdir(project_root)
    
    success = True
    
    if args.format == "all":
        formats = ["dmg", "pkg"]
    else:
        formats = [args.format]
    
    for fmt in formats:
        if fmt == "dmg":
            if not create_dmg(version):
                success = False
        elif fmt == "pkg":
            if not create_pkg(version):
                success = False
    
    if success:
        print("[macOS Package] SUCCESS: Package creation complete!")
    else:
        print("[macOS Package] WARNING: Some packages may not have been created")


if __name__ == "__main__":
    main()

