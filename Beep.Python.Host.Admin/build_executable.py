"""
Platform-aware builder for Beep.Python Host Admin.

Run on the target OS (no cross-compiling):
    python build_executable.py

Options:
    --no-clean   Skip PyInstaller --clean

Outputs land in dist/BeepPythonHost/ with platform-appropriate binaries.
"""
import argparse
import importlib
import platform
import subprocess
import sys
from pathlib import Path


PROJECT_ROOT = Path(__file__).resolve().parent
SPEC_FILE = PROJECT_ROOT / "beep_python_host.spec"


def ensure_pyinstaller():
    """Ensure PyInstaller is available; install if missing."""
    try:
        importlib.import_module("PyInstaller.__main__")
        return
    except ImportError:
        pass

    print("PyInstaller not found; installing locally...")
    subprocess.check_call(
        [sys.executable, "-m", "pip", "install", "pyinstaller"],
        shell=False,
    )


def build(clean: bool):
    if not SPEC_FILE.exists():
        raise FileNotFoundError(f"Spec file not found: {SPEC_FILE}")

    cmd = [sys.executable, "-m", "PyInstaller"]
    if clean:
        cmd.append("--clean")
    cmd.append(str(SPEC_FILE))

    print(f"Building for platform: {platform.system()} ({platform.machine()})")
    print(f"Running: {' '.join(cmd)}")
    subprocess.check_call(cmd, cwd=PROJECT_ROOT, shell=False)


def main():
    parser = argparse.ArgumentParser(description="Build platform-specific executable with PyInstaller.")
    parser.add_argument("--no-clean", action="store_true", help="Skip PyInstaller --clean step")
    args = parser.parse_args()

    ensure_pyinstaller()
    build(clean=not args.no_clean)

    dist_dir = PROJECT_ROOT / "dist" / "BeepPythonHost"
    binary = dist_dir / ("BeepPythonHost.exe" if platform.system() == "Windows" else "BeepPythonHost")
    print("\nBuild complete.")
    print(f"Output folder: {dist_dir}")
    print(f"Launcher: {binary}")
    print("Use the platform launcher scripts in that folder to start the app.")


if __name__ == "__main__":
    main()
