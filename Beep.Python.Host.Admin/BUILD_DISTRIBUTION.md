# Cross-Platform Distribution Guide

This document describes how to build and distribute Beep.Python Host Admin for Windows, Linux, and macOS.

## Overview

The build system uses:
- **PyInstaller** for creating platform-specific executables
- **Platform-specific embedded Python** for each target platform
- **CI/CD (GitHub Actions)** for automated builds
- **Platform-specific installers** (.exe, .deb/.rpm/.tar.gz, .dmg/.pkg)

## Quick Start

### Automated Build (Recommended)

1. **Create a version tag:**
   ```bash
   git tag v1.0.0
   git push origin v1.0.0
   ```

2. **GitHub Actions will automatically:**
   - Build for Windows, Linux, and macOS
   - Create platform-specific installers
   - Upload artifacts to GitHub Releases

### Manual Build

#### Windows
```bash
# Setup embedded Python
setup_embedded_python.bat

# Build
python scripts/build_windows.py --version 1.0.0

# Create installer (requires Inno Setup)
python scripts/create_windows_installer.py --version 1.0.0
```

#### Linux
```bash
# Setup embedded Python
chmod +x setup_embedded_python.sh
./setup_embedded_python.sh

# Build
python scripts/build_linux.py --version 1.0.0

# Create packages
python scripts/create_linux_package.py --version 1.0.0 --format tarball
python scripts/create_linux_package.py --version 1.0.0 --format deb
```

#### macOS
```bash
# Setup embedded Python
chmod +x setup_embedded_python.sh
./setup_embedded_python.sh

# Build
python scripts/build_macos.py --version 1.0.0

# Create packages
python scripts/create_macos_package.py --version 1.0.0 --format dmg
python scripts/create_macos_package.py --version 1.0.0 --format pkg
```

## Directory Structure

```
project/
├── .github/
│   └── workflows/
│       └── build-release.yml    # CI/CD workflow
├── scripts/
│   ├── build_windows.py          # Windows build script
│   ├── build_linux.py            # Linux build script
│   ├── build_macos.py            # macOS build script
│   ├── build_all_platforms.py   # Unified orchestrator
│   ├── create_windows_installer.py
│   ├── create_linux_package.py
│   └── create_macos_package.py
├── python-embedded/              # Platform-specific (auto-downloaded)
├── dist/
│   ├── windows/                  # Windows build output
│   ├── linux/                    # Linux build output
│   └── macos/                    # macOS build output
└── VERSION                       # Version file
```

## Version Management

Version is determined in priority order:
1. `--version` command-line argument
2. `VERSION` file in project root
3. Git tag (e.g., `v1.0.0`)
4. Default: `1.0.0`

To update version:
```bash
echo "1.0.1" > VERSION
```

## Platform-Specific Details

### Windows

**Requirements:**
- Inno Setup 5 or 6 (for installer creation)
- Download from: https://jrsoftware.org/isdl.php

**Output:**
- `dist/windows/BeepPythonHost/` - Application files
- `dist/windows/python-embedded/` - Embedded Python
- `dist/windows/BeepPythonHost-Setup-{version}.exe` - Installer

### Linux

**Requirements:**
- `dpkg-dev` (for .deb packages): `sudo apt-get install dpkg-dev`
- `rpm` (for .rpm packages): `sudo apt-get install rpm`
- `appimagetool` (for AppImage): Install from AppImageKit

**Output:**
- `dist/linux/BeepPythonHost/` - Application files
- `dist/linux/python-embedded/` - Embedded Python
- `dist/linux/beep-python-host-admin_{version}_amd64.deb` - Debian package
- `dist/linux/beep-python-host-admin-{version}-linux-amd64.tar.gz` - Tarball

### macOS

**Requirements:**
- `hdiutil` (built-in, for .dmg)
- `pkgbuild` and `productbuild` (built-in, for .pkg)
- `create-dmg` (optional, better DMG): `brew install create-dmg`

**Output:**
- `dist/macos/BeepPythonHost/` - Application files
- `dist/macos/python-embedded/` - Embedded Python
- `dist/macos/BeepPythonHost-{version}-macOS.dmg` - Disk image
- `dist/macos/BeepPythonHost-{version}-macOS.pkg` - Installer package

## CI/CD Workflow

The GitHub Actions workflow (`.github/workflows/build-release.yml`) triggers on:
- **Version tags**: `v*.*.*` (e.g., `v1.0.0`)
- **Manual dispatch**: Via GitHub Actions UI

**Workflow Steps:**
1. Checkout code
2. Setup embedded Python (platform-specific)
3. Install build dependencies
4. Build with PyInstaller
5. Create platform-specific installer/package
6. Upload artifacts to GitHub Releases

## Troubleshooting

### Build Fails

1. **Check embedded Python:**
   - Ensure `python-embedded` directory exists
   - Run `setup_embedded_python.{bat|sh}` if missing

2. **Check dependencies:**
   - Ensure PyInstaller is installed
   - Check platform-specific requirements (Inno Setup, dpkg-dev, etc.)

3. **Check version:**
   - Ensure `VERSION` file exists or use `--version` argument

### Installer Creation Fails

**Windows:**
- Ensure Inno Setup is installed
- Check `installer/beep_python_installer.iss` exists

**Linux:**
- Install required tools: `sudo apt-get install dpkg-dev rpm`
- For AppImage, install `appimagetool`

**macOS:**
- `hdiutil` and `pkgbuild` are built-in
- For better DMG, install `create-dmg` via Homebrew

### CI/CD Issues

1. **Check workflow file:**
   - Ensure `.github/workflows/build-release.yml` is correct
   - Check YAML syntax

2. **Check permissions:**
   - Ensure GitHub Actions has permission to create releases
   - Check `GITHUB_TOKEN` is available

3. **Check artifacts:**
   - Artifacts are uploaded to GitHub Actions
   - Releases are created automatically on tag push

## Best Practices

1. **Version Tagging:**
   - Use semantic versioning: `v1.0.0`, `v1.0.1`, `v2.0.0`
   - Tag before pushing: `git tag v1.0.0 && git push origin v1.0.0`

2. **Testing:**
   - Test builds locally before tagging
   - Test installers on clean systems

3. **Documentation:**
   - Update `VERSION` file for each release
   - Update release notes in GitHub Releases

4. **Security:**
   - Code sign Windows/macOS installers (optional but recommended)
   - Notarize macOS packages (required for distribution outside App Store)

## Next Steps

- Set up code signing for Windows/macOS
- Configure notarization for macOS
- Set up package repository uploads (Chocolatey, Homebrew, etc.)
- Add automated testing to CI/CD pipeline

