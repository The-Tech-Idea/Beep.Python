# -*- mode: python ; coding: utf-8 -*-
"""
PyInstaller spec for Beep.Python Host Admin
- Bundles templates/static/config/instance
- Includes launcher scripts for Windows/Linux
- Note: python-embedded is handled separately by the installer
- Keeps console for visibility of server logs
"""
from pathlib import Path
import sys

block_cipher = None
# In PyInstaller spec context, __file__ may be undefined; use current working directory.
project_root = Path.cwd()

datas = [
    (project_root / "templates", "templates"),
    (project_root / "static", "static"),
    (project_root / "config", "config"),
    (project_root / "instance", "instance"),
    (project_root / "launch_beep_python_admin.cmd", "."),
    (project_root / "launch_beep_python_admin.sh", "."),
]

# Note: python-embedded is NOT bundled here due to path length issues
# It will be copied separately by the standalone installer

datas = [(str(src), dest) for src, dest in datas if src.exists()]

hiddenimports = [
    # SocketIO async drivers - threading is primary for frozen builds
    "engineio.async_drivers.threading",
    "engineio.async_drivers.eventlet",
    # Threading support (primary for PyInstaller)
    "concurrent.futures",
    "queue",
    # Eventlet (optional - may not be available)
    "eventlet",
    "eventlet.green",
    "eventlet.green.threading",
    "eventlet.hubs",
    "eventlet.hubs.hub",
    "eventlet.hubs.selects",
    "eventlet.hubs.poll",
    # Flask and extensions
    "flask",
    "flask.json",
    "flask_cors",
    "flask_sqlalchemy",
    "flask_socketio",
    "flask_restx",
    # SQLAlchemy dialects
    "sqlalchemy.dialects.sqlite",
    "sqlalchemy.pool",
    # Core app modules
    "app",
    "app.routes",
    "app.services",
    "app.models",
    "app.utils",
    "app.config_manager",
    "app.database",
    # Other dependencies
    "jinja2",
    "markupsafe",
    "werkzeug",
    "click",
    "itsdangerous",
    "email.mime.text",
    "email.mime.multipart",
]

a = Analysis(
    ["run.py"],
    pathex=[str(project_root)],
    binaries=[],
    datas=datas,
    hiddenimports=hiddenimports,
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

exe = EXE(
    pyz,
    a.scripts,
    a.binaries,
    a.zipfiles,
    a.datas,
    [],
    name="BeepPythonHost",
    debug=False,
    bootloader_ignore_signals=False,
    strip=False,
    upx=True,
    upx_exclude=[],
    runtime_tmpdir=None,
    console=True,
    disable_windowed_traceback=False,
    argv_emulation=False,
    target_arch=None,
    codesign_identity=None,
    entitlements_file=None,
)

coll = COLLECT(
    exe,
    a.binaries,
    a.zipfiles,
    a.datas,
    strip=False,
    upx=True,
    upx_exclude=[],
    name="BeepPythonHost",
)
