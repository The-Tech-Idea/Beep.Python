#!/usr/bin/env python3
"""
App Icon Generator for Beep AI Server
======================================
Generates cross-platform app icons from source Python logo with theme color customization.

Usage:
    python generate_app_icons.py                          # Generate with default colors
    python generate_app_icons.py --orange "#FF8C00"       # Custom orange
    python generate_app_icons.py --navy "#001F4D"         # Custom navy
    python generate_app_icons.py --theme dark             # Use dark theme preset
    
Generates icons for:
- Windows: .ico files (16, 32, 48, 64, 128, 256)
- macOS: .icns file (requires pillow-icns or iconutil)
- Linux: PNG files (16, 32, 48, 64, 128, 256, 512)
- Web: favicon.ico, apple-touch-icon.png, android-chrome
"""

import sys
import argparse
from pathlib import Path

try:
    from PIL import Image, ImageDraw
    import struct
except ImportError:
    print("Error: Pillow library not installed.")
    print("Install with: pip install Pillow")
    sys.exit(1)

# Theme color presets
THEMES = {
    'default': {'orange': '#FF8C00', 'navy': '#001F4D'},
    'light': {'orange': '#FFA500', 'navy': '#1E3A8A'},
    'dark': {'orange': '#D97706', 'navy': '#0F172A'},
    'blue': {'orange': '#3B82F6', 'navy': '#1E40AF'},
    'green': {'orange': '#10B981', 'navy': '#065F46'},
}

# Icon sizes for different platforms
SIZES = {
    'windows': [16, 32, 48, 64, 128, 256],
    'linux': [16, 24, 32, 48, 64, 96, 128, 256, 512],
    'web': [16, 32, 192, 512],
    'mac': [16, 32, 64, 128, 256, 512, 1024],
}


def hex_to_rgb(hex_color):
    """Convert hex color to RGB tuple."""
    hex_color = hex_color.lstrip('#')
    return tuple(int(hex_color[i:i+2], 16) for i in (0, 2, 4))


def recolor_image(img, old_color, new_color, tolerance=30):
    """Replace a color in an image with a new color."""
    img = img.convert('RGBA')
    data = img.getdata()
    
    old_r, old_g, old_b = hex_to_rgb(old_color) if isinstance(old_color, str) else old_color
    new_r, new_g, new_b = hex_to_rgb(new_color) if isinstance(new_color, str) else new_color
    
    new_data = []
    for item in data:
        r, g, b, a = item
        # Check if color is close to the old color
        if (abs(r - old_r) <= tolerance and 
            abs(g - old_g) <= tolerance and 
            abs(b - old_b) <= tolerance and a > 0):
            new_data.append((new_r, new_g, new_b, a))
        else:
            new_data.append(item)
    
    img.putdata(new_data)
    return img


def colorize_monochrome_logo(source_img, color):
    """Colorize a monochrome logo with a single color."""
    img = source_img.convert('RGBA')
    width, height = img.size
    pixels = img.load()
    
    new_r, new_g, new_b = hex_to_rgb(color)
    
    # Create new image
    result = Image.new('RGBA', (width, height), (0, 0, 0, 0))
    result_pixels = result.load()
    
    for y in range(height):
        for x in range(width):
            r, g, b, a = pixels[x, y]
            
            if a > 10:  # Not transparent
                # Apply the new color while preserving alpha
                result_pixels[x, y] = (new_r, new_g, new_b, a)
            else:
                result_pixels[x, y] = (0, 0, 0, 0)
    
    return result


def create_ico(images, output_path):
    """Create a .ico file from multiple images."""
    output_path.parent.mkdir(parents=True, exist_ok=True)
    
    # ICO format: Header + Directory + Images
    with open(output_path, 'wb') as f:
        # ICO header
        f.write(struct.pack('<HHH', 0, 1, len(images)))  # Reserved, Type (1=ICO), Count
        
        offset = 6 + (16 * len(images))  # Header + Directory entries
        
        # Directory entries
        for img in images:
            width, height = img.size
            # Convert to BMP format for ICO
            from io import BytesIO
            bmp_io = BytesIO()
            img.save(bmp_io, format='PNG')
            bmp_data = bmp_io.getvalue()
            
            # Directory entry
            f.write(struct.pack('B', width if width < 256 else 0))  # Width
            f.write(struct.pack('B', height if height < 256 else 0))  # Height
            f.write(struct.pack('B', 0))  # Color palette
            f.write(struct.pack('B', 0))  # Reserved
            f.write(struct.pack('H', 1))  # Color planes
            f.write(struct.pack('H', 32))  # Bits per pixel
            f.write(struct.pack('I', len(bmp_data)))  # Image data size
            f.write(struct.pack('I', offset))  # Offset to image data
            offset += len(bmp_data)
        
        # Image data
        for img in images:
            from io import BytesIO
            bmp_io = BytesIO()
            img.save(bmp_io, format='PNG')
            f.write(bmp_io.getvalue())


def generate_icons(source_path, output_dir, theme='default', color=None):
    """Generate all app icons from source image."""
    source = Path(source_path)
    output_dir = Path(output_dir)
    
    if not source.exists():
        print(f"Error: Source image not found: {source}")
        return False
    
    # Load source image
    print(f"Loading source: {source}")
    source_img = Image.open(source)
    
    # Convert to RGBA
    if source_img.mode != 'RGBA':
        source_img = source_img.convert('RGBA')
    
    # Get theme color
    if theme in THEMES:
        color = color or THEMES[theme]['orange']
    else:
        color = color or THEMES['default']['orange']
    
    print(f"Icon color: {color}")
    
    # Colorize the monochrome logo
    print("Applying color to monochrome logo...")
    themed_img = colorize_monochrome_logo(source_img, color)
    
    # Create output directory
    output_dir.mkdir(parents=True, exist_ok=True)
    
    # Save themed source
    themed_source = output_dir / 'python_logo_themed.png'
    themed_img.save(themed_source)
    print(f"✓ Saved themed source: {themed_source}")
    
    # Generate PNG files for all sizes
    all_sizes = sorted(set(SIZES['windows'] + SIZES['linux'] + SIZES['web']))
    png_images = {}
    
    for size in all_sizes:
        img = themed_img.copy()
        img.thumbnail((size, size), Image.Resampling.LANCZOS)
        
        # Create new image with exact size (centered)
        final_img = Image.new('RGBA', (size, size), (0, 0, 0, 0))
        offset = ((size - img.size[0]) // 2, (size - img.size[1]) // 2)
        final_img.paste(img, offset)
        
        output_path = output_dir / f'icon-{size}x{size}.png'
        final_img.save(output_path)
        png_images[size] = final_img
        print(f"✓ Generated: {output_path.name}")
    
    # Generate Windows .ico files
    print("\nGenerating Windows icons...")
    ico_sizes = [png_images[s] for s in SIZES['windows'] if s in png_images]
    ico_path = output_dir / 'icon.ico'
    create_ico(ico_sizes, ico_path)
    print(f"✓ Generated: {ico_path.name}")
    
    # Generate favicon.ico (16, 32, 48)
    favicon_sizes = [png_images[s] for s in [16, 32, 48] if s in png_images]
    favicon_path = output_dir / 'favicon.ico'
    create_ico(favicon_sizes, favicon_path)
    print(f"✓ Generated: {favicon_path.name}")
    
    # Generate web icons
    print("\nGenerating web icons...")
    if 192 in png_images:
        android_chrome = output_dir / 'android-chrome-192x192.png'
        png_images[192].save(android_chrome)
        print(f"✓ Generated: {android_chrome.name}")
    
    if 512 in png_images:
        android_chrome = output_dir / 'android-chrome-512x512.png'
        png_images[512].save(android_chrome)
        print(f"✓ Generated: {android_chrome.name}")
    
    # Apple touch icon (180x180)
    apple_img = themed_img.copy()
    apple_img.thumbnail((180, 180), Image.Resampling.LANCZOS)
    apple_icon = Image.new('RGBA', (180, 180), (0, 0, 0, 0))
    offset = ((180 - apple_img.size[0]) // 2, (180 - apple_img.size[1]) // 2)
    apple_icon.paste(apple_img, offset)
    apple_path = output_dir / 'apple-touch-icon.png'
    apple_icon.save(apple_path)
    print(f"✓ Generated: {apple_path.name}")
    
    print(f"\n✅ All icons generated successfully in: {output_dir}")
    print(f"   Color: {color}")
    
    return True


def main():
    parser = argparse.ArgumentParser(description='Generate cross-platform app icons - recolor monochrome images')
    parser.add_argument('source', nargs='?', default='python_logo.png', help='Source image path')
    parser.add_argument('-o', '--output', default='../static', help='Output directory')
    parser.add_argument('-t', '--theme', choices=list(THEMES.keys()), default='default', 
                        help='Color theme preset (uses orange color from theme)')
    parser.add_argument('-c', '--color', help='Custom color (hex) - e.g., #FF8C00')
    
    args = parser.parse_args()
    
    # Resolve paths
    script_dir = Path(__file__).parent
    source_path = script_dir / args.source
    output_dir = script_dir / args.output
    
    print(f"""
╔════════════════════════════════════════════════════════╗
║      Beep AI Server - Icon Generator                   ║
║      Recolor Monochrome Images to Any Color            ║
╚════════════════════════════════════════════════════════╝
    """)
    
    success = generate_icons(source_path, output_dir, args.theme, args.color)
    
    sys.exit(0 if success else 1)


if __name__ == '__main__':
    main()
