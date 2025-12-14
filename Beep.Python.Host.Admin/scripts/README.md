# Build Scripts

This directory contains scripts for building Beep.Python Host Admin for different platforms.

## Overview

The build system supports automated cross-platform builds via CI/CD (GitHub Actions) and local builds.

## Scripts

### Platform Build Scripts

- **`build_windows.py`** - Builds for Windows using PyInstaller
- **`build_linux.py`** - Builds for Linux using PyInstaller
- **`build_macos.py`** - Builds for macOS using PyInstaller
- **`build_all_platforms.py`** - Unified orchestrator (auto-detects platform)

### Packaging Scripts

- **`create_windows_installer.py`** - Creates Windows installer (.exe) using Inno Setup
- **`create_linux_package.py`** - Creates Linux packages (.deb, .rpm, .tar.gz, AppImage)
- **`create_macos_package.py`** - Creates macOS packages (.dmg, .pkg)

## Usage

### Local Build

Build for current platform:
```bash
python scripts/build_all_platforms.py --version 1.0.0
```

Build for specific platform:
```bash
python scripts/build_windows.py --version 1.0.0
python scripts/build_linux.py --version 1.0.0
python scripts/build_macos.py --version 1.0.0
```

### Create Installers/Packages

After building, create platform-specific installers:

**Windows:**
```bash
python scripts/create_windows_installer.py --version 1.0.0
```

**Linux:**
```bash
python scripts/create_linux_package.py --version 1.0.0 --format tarball
python scripts/create_linux_package.py --version 1.0.0 --format deb
python scripts/create_linux_package.py --version 1.0.0 --format all
```

**macOS:**
```bash
python scripts/create_macos_package.py --version 1.0.0 --format dmg
python scripts/create_macos_package.py --version 1.0.0 --format pkg
python scripts/create_macos_package.py --version 1.0.0 --format all
```

## Version Management

Version is determined in this order:
1. `--version` command-line argument
2. `VERSION` file in project root
3. Git tag (if available)
4. Default: `1.0.0`

## CI/CD

The GitHub Actions workflow (`.github/workflows/build-release.yml`) automatically:
- Builds for all platforms on version tags (`v*.*.*`)
- Creates platform-specific installers
- Uploads artifacts to GitHub Releases

Trigger by:
- Pushing a version tag: `git tag v1.0.0 && git push origin v1.0.0`
- Manual workflow dispatch in GitHub Actions

## Requirements

### All Platforms
- Python 3.11+
- PyInstaller
- Platform-specific embedded Python (auto-downloaded)

### Windows
- Inno Setup (for installer creation)
- Available at: https://jrsoftware.org/isdl.php

### Linux
- `dpkg-deb` (for .deb packages) - install `dpkg-dev`
- `rpmbuild` (for .rpm packages) - install `rpm`
- `appimagetool` (for AppImage) - install from AppImageKit

### macOS
- `hdiutil` (built-in, for .dmg)
- `pkgbuild` and `productbuild` (built-in, for .pkg)
- `create-dmg` (optional, better DMG creation) - install via Homebrew

## Output

Build outputs are placed in:
- `dist/windows/` - Windows build and installer
- `dist/linux/` - Linux build and packages
- `dist/macos/` - macOS build and packages

## Notes

- The `python-embedded` directory is platform-specific and is automatically downloaded during build
- Build scripts clean previous builds before starting
- All scripts support CI/CD environments (non-interactive mode)

