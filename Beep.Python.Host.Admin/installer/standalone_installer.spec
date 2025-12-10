# -*- mode: python ; coding: utf-8 -*-
"""
PyInstaller spec for Beep.Python STANDALONE INSTALLER
======================================================
This creates a SINGLE executable installer that contains:
- The installer wizard (GUI)
- All application files (bundled inside)
- Embedded Python runtime
- Everything needed to install on any machine

The user runs this ONE file, wizard shows, they pick a folder,
and the app gets installed there with all settings stored locally.
"""
from pathlib import Path
import os

block_cipher = None
project_root = Path.cwd()

# The installer script
installer_script = project_root / "installer" / "standalone_installer.py"

# Bundle the SOURCE application files directly (not pre-built exe)
# This includes all necessary source files for the Flask app
app_bundle_source = project_root  # Use project root

# Python embedded folder (for standalone operation)
python_embedded_source = project_root / "python-embedded"

# Collect all app files to bundle inside installer
app_bundle_datas = []

# Bundle individual source folders and files needed for the app
source_items = [
    ("app", "app_bundle/app"),
    ("templates", "app_bundle/templates"),
    ("static", "app_bundle/static"),
    ("config", "app_bundle/config"),
    ("migrations", "app_bundle/migrations"),
    ("assets", "app_bundle/assets"),  # Include icons
    ("run.py", "app_bundle"),
    ("requirements.txt", "app_bundle"),
]

for src_name, dst_name in source_items:
    src_path = project_root / src_name
    if src_path.exists():
        app_bundle_datas.append((str(src_path), dst_name))

# Bundle python-embedded separately (to avoid path length issues)
if python_embedded_source.exists():
    app_bundle_datas.append((str(python_embedded_source), "python_embedded"))

a = Analysis(
    [str(installer_script)],
    pathex=[str(project_root)],
    binaries=[],
    datas=app_bundle_datas,
    hiddenimports=[
        'tkinter',
        'tkinter.ttk',
        'tkinter.filedialog',
        'tkinter.messagebox',
    ],
    hookspath=[],
    hooksconfig={},
    runtime_hooks=[],
    excludes=[],
    win_no_prefer_redirects=False,
    win_private_assemblies=False,
    cipher=block_cipher,
    noarchive=False,
)

pyz = PYZ(a.pure, a.zipped_data, cipher=block_cipher)

# Create a SINGLE FILE executable (onefile mode)
exe = EXE(
    pyz,
    a.scripts,
    a.binaries,
    a.zipfiles,
    a.datas,
    [],
    name="BeepPython-Setup",
    debug=False,
    bootloader_ignore_signals=False,
    strip=False,
    upx=True,
    upx_exclude=[],
    runtime_tmpdir=None,
    console=False,  # No console - GUI only
    disable_windowed_traceback=False,
    argv_emulation=False,
    target_arch=None,
    codesign_identity=None,
    entitlements_file=None,
    icon=str(project_root / "assets" / "icon.ico") if (project_root / "assets" / "icon.ico").exists() else None,
)
