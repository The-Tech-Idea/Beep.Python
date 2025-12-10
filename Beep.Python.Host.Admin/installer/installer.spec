# -*- mode: python ; coding: utf-8 -*-
"""
PyInstaller spec for Beep.Python Installer Wizard
This creates a cross-platform installer executable
"""
from pathlib import Path
import platform

block_cipher = None
project_root = Path.cwd()

# Platform-specific settings
system = platform.system()
exe_name = "BeepPythonSetup"
if system == "Windows":
    exe_name = "BeepPythonSetup.exe"
elif system == "Darwin":
    exe_name = "BeepPythonSetup"
else:
    exe_name = "BeepPythonSetup"

# Include the main application dist folder as data
datas = [
    (project_root / "dist" / "BeepPythonHost", "BeepPythonHost"),
    (project_root / "installer" / "readme_before.txt", "."),
    (project_root / "installer" / "readme_after.txt", "."),
]
datas = [(str(src), dest) for src, dest in datas if Path(src).exists()]

# Also include assets if they exist
assets_dir = project_root / "assets"
if assets_dir.exists():
    datas.append((str(assets_dir), "assets"))

a = Analysis(
    [str(project_root / "installer" / "installer_wizard.py")],
    pathex=[str(project_root)],
    binaries=[],
    datas=datas,
    hiddenimports=[
        "tkinter",
        "tkinter.ttk",
        "tkinter.filedialog",
        "tkinter.messagebox",
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

exe = EXE(
    pyz,
    a.scripts,
    a.binaries,
    a.zipfiles,
    a.datas,
    [],
    name=exe_name,
    debug=False,
    bootloader_ignore_signals=False,
    strip=False,
    upx=True,
    upx_exclude=[],
    runtime_tmpdir=None,
    console=False,  # No console window for GUI installer
    disable_windowed_traceback=False,
    argv_emulation=False,
    target_arch=None,
    codesign_identity=None,
    entitlements_file=None,
    icon=str(project_root / "assets" / "icon.ico") if (project_root / "assets" / "icon.ico").exists() else None,
)

# For macOS, create an app bundle
if system == "Darwin":
    app = BUNDLE(
        exe,
        name="BeepPython Setup.app",
        icon=str(project_root / "assets" / "icon.icns") if (project_root / "assets" / "icon.icns").exists() else None,
        bundle_identifier="com.thetechidea.beeppython.installer",
        info_plist={
            'CFBundleName': 'BeepPython Setup',
            'CFBundleDisplayName': 'BeepPython Setup',
            'CFBundleVersion': '1.0.0',
            'CFBundleShortVersionString': '1.0.0',
            'NSHighResolutionCapable': True,
        },
    )
