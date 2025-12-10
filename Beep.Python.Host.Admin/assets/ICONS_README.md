# Beep.Python Icon Assets

## Required Icon Files

Place your icon files in this folder with the following names:

### Windows
- **`icon.ico`** - Multi-resolution Windows icon
  - Must contain: 16x16, 32x32, 48x48, 64x64, 128x128, 256x256 pixels
  - Used for: Installer, desktop shortcut, taskbar

### macOS  
- **`icon.icns`** - Apple icon format
  - Must contain: 16x16, 32x32, 128x128, 256x256, 512x512, 1024x1024 pixels
  - Used for: App bundle, Dock, Finder

### Linux
- **`icon.png`** - PNG format (256x256 or 512x512 recommended)
  - Used for: Desktop files, application menu

### Web/Favicon (place in /static folder)
- **`favicon.ico`** - 16x16, 32x32, 48x48 (multi-resolution)
- **`favicon-16x16.png`** - 16x16 pixels
- **`favicon-32x32.png`** - 32x32 pixels
- **`apple-touch-icon.png`** - 180x180 pixels
- **`android-chrome-192x192.png`** - 192x192 pixels
- **`android-chrome-512x512.png`** - 512x512 pixels

## How to Create Icons

### Option 1: Online Tools (Easiest)
1. Create a 1024x1024 PNG image (your logo)
2. Use these free tools:
   - **Windows .ico**: https://icoconvert.com/ or https://convertico.com/
   - **macOS .icns**: https://cloudconvert.com/png-to-icns
   - **Favicon set**: https://realfavicongenerator.net/

### Option 2: Using ImageMagick (Command Line)
```bash
# Install ImageMagick first

# Create Windows .ico (from 256x256 PNG)
magick icon-256.png -define icon:auto-resize=256,128,64,48,32,16 icon.ico

# Create multiple PNG sizes
magick icon-1024.png -resize 512x512 icon-512.png
magick icon-1024.png -resize 256x256 icon-256.png
magick icon-1024.png -resize 128x128 icon-128.png
magick icon-1024.png -resize 64x64 icon-64.png
magick icon-1024.png -resize 48x48 icon-48.png
magick icon-1024.png -resize 32x32 icon-32.png
magick icon-1024.png -resize 16x16 icon-16.png
```

### Option 3: Using Python (pillow)
```python
from PIL import Image

# Load your source image (should be 1024x1024 or larger)
img = Image.open('logo.png')

# Create Windows .ico
sizes = [(16,16), (32,32), (48,48), (64,64), (128,128), (256,256)]
img.save('icon.ico', sizes=sizes)

# Create favicon
img.save('favicon.ico', sizes=[(16,16), (32,32), (48,48)])

# Create PNGs
for size in [16, 32, 48, 64, 128, 256, 512]:
    resized = img.resize((size, size), Image.LANCZOS)
    resized.save(f'icon-{size}x{size}.png')
```

## Icon Design Guidelines

- Use a **square** image (1:1 aspect ratio)
- Keep the design **simple** - it must be recognizable at 16x16
- Use **high contrast** colors
- Leave some **padding** around the edges (about 10%)
- Avoid **fine details** that won't be visible at small sizes
- Test at **16x16** to ensure readability

## Current Status

After adding icons, run the installer build again:
```bash
python -m PyInstaller installer/standalone_installer.spec --noconfirm
```

The installer will automatically use `assets/icon.ico` for the Windows executable.
