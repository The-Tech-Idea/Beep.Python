#!/usr/bin/env python3
"""
Unified Build Orchestrator
Detects platform and runs appropriate build script
Can be used locally or in CI/CD
"""
import os
import sys
import subprocess
import platform
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


def detect_platform():
    """Detect the current platform"""
    system = platform.system().lower()
    if system == "windows":
        return "windows"
    elif system == "linux":
        return "linux"
    elif system == "darwin":
        return "macos"
    else:
        return None


def build_for_platform(platform_name, version):
    """Build for a specific platform"""
    script_map = {
        "windows": "build_windows.py",
        "linux": "build_linux.py",
        "macos": "build_macos.py"
    }
    
    script_name = script_map.get(platform_name)
    if not script_name:
        print(f"ERROR: Unknown platform: {platform_name}")
        return False
    
    script_path = Path(__file__).parent / script_name
    if not script_path.exists():
        print(f"ERROR: Build script not found: {script_path}")
        return False
    
    print(f"Building for {platform_name}...")
    result = subprocess.run(
        [sys.executable, str(script_path), "--version", version],
        check=False
    )
    
    return result.returncode == 0


def main():
    parser = argparse.ArgumentParser(
        description="Build Beep.Python for all platforms or current platform"
    )
    parser.add_argument("--version", help="Version number (e.g., 1.0.0)")
    parser.add_argument(
        "--platform",
        choices=["windows", "linux", "macos", "all"],
        help="Platform to build for (default: auto-detect current platform)"
    )
    args = parser.parse_args()
    
    version = get_version(args.version)
    print(f"Building Beep.Python version {version}")
    
    # Determine platform(s) to build
    if args.platform == "all":
        platforms = ["windows", "linux", "macos"]
        print("WARNING: Building for all platforms requires running on each platform separately")
        print("This script will only build for the current platform.")
        current_platform = detect_platform()
        if current_platform:
            platforms = [current_platform]
    elif args.platform:
        platforms = [args.platform]
    else:
        # Auto-detect
        current_platform = detect_platform()
        if not current_platform:
            print("ERROR: Could not detect platform")
            sys.exit(1)
        platforms = [current_platform]
        print(f"Auto-detected platform: {current_platform}")
    
    # Build for each platform
    success = True
    for platform_name in platforms:
        if not build_for_platform(platform_name, version):
            print(f"ERROR: Build failed for {platform_name}")
            success = False
    
    if success:
        print("\nSUCCESS: All builds completed!")
    else:
        print("\nERROR: Some builds failed")
        sys.exit(1)


if __name__ == "__main__":
    main()

