"""
One-shot builder for Beep.Python Host Admin.

Usage:
    python build_bundle.py

Behavior:
- Ensures PyInstaller is available (installs locally if missing).
- Runs PyInstaller against beep_python_host.spec with --clean.
- Leaves output in dist/BeepPythonHost/.
"""
import importlib
import subprocess
import sys
from pathlib import Path


def ensure_pyinstaller():
    """Ensure PyInstaller is installed; install if missing."""
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


def run_pyinstaller(project_root: Path):
    """Run PyInstaller with the repo spec file."""
    spec_path = project_root / "beep_python_host.spec"
    if not spec_path.exists():
        raise FileNotFoundError(f"Spec file not found at {spec_path}")

    cmd = [sys.executable, "-m", "PyInstaller", "--clean", str(spec_path)]
    print(f"Running: {' '.join(cmd)}")
    subprocess.check_call(cmd, cwd=project_root, shell=False)


def main():
    project_root = Path(__file__).resolve().parent
    ensure_pyinstaller()
    run_pyinstaller(project_root)
    dist_dir = project_root / "dist" / "BeepPythonHost"
    print(f"\nBuild complete. Output folder: {dist_dir}")
    print("Use the launcher scripts inside that folder to start the app.")


if __name__ == "__main__":
    main()
