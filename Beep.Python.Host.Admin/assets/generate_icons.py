#!/usr/bin/env python3
"""
Icon Generator for Beep.Python Host Admin
==========================================
Generates all required icon formats from a source image.

Usage:
    python generate_icons.py <source_image.png>
    
The source image should be at least 1024x1024 pixels for best quality.
"""
import sys
from pathlib import Path

try:
    from PIL import Image
except ImportError:
    print("Error: Pillow library not installed.")
    print("Install with: pip install Pillow")
    sys.exit(1)


def generate_icons(source_path: str, output_dir: str = None):
    """Generate all icon formats from a source image."""
    source = Path(source_path)
    if not source.exists():
        print(f"Error: Source image not found: {source}")
        sys.exit(1)
    
    if output_dir is None:
        output_dir = source.parent
    else:
        output_dir = Path(output_dir)
        output_dir.mkdir(parents=True, exist_ok=True)
    
    # Load source image
    print(f"Loading source image: {source}")
    img = Image.open(source)
    
    # Convert to RGBA if necessary
    if img.mode != 'RGBA':
        img = img.convert('RGBA')
    
    original_size = img.size
    print(f"Source size: {original_size[0]}x{original_size[1]}")
    
    if original_size[0] < 256 or original_size[1] < 256:
        print("Warning: Source image is smaller than 256x256. Quality may be poor.")
    
    # Make it square if it isn't
    if original_size[0] != original_size[1]:
        size = max(original_size)
        new_img = Image.new('RGBA', (size, size), (0, 0, 0, 0))
        offset = ((size - original_size[0]) // 2, (size - original_size[1]) // 2)
        new_img.paste(img, offset)
        img = new_img
        print(f"Padded to square: {size}x{size}")
    
    # =========================================================================
    # Windows .ico file (multi-resolution)
    # =========================================================================
    print("\n[Windows] Generating icon.ico...")
    ico_sizes = [(16, 16), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)]
    ico_path = output_dir / "icon.ico"
    img.save(ico_path, format='ICO', sizes=ico_sizes)
    print(f"  Created: {ico_path}")
    
    # =========================================================================
    # Favicon .ico file (for web)
    # =========================================================================
    print("\n[Web] Generating favicon.ico...")
    favicon_sizes = [(16, 16), (32, 32), (48, 48)]
    favicon_path = output_dir / "favicon.ico"
    img.save(favicon_path, format='ICO', sizes=favicon_sizes)
    print(f"  Created: {favicon_path}")
    
    # Also copy to static folder if it exists
    static_folder = output_dir.parent / "static"
    if static_folder.exists():
        static_favicon = static_folder / "favicon.ico"
        img.save(static_favicon, format='ICO', sizes=favicon_sizes)
        print(f"  Created: {static_favicon}")
    
    # =========================================================================
    # PNG files (various sizes)
    # =========================================================================
    print("\n[PNG] Generating PNG files...")
    png_sizes = [16, 32, 48, 64, 128, 256, 512]
    
    for size in png_sizes:
        resized = img.resize((size, size), Image.LANCZOS)
        png_path = output_dir / f"icon-{size}x{size}.png"
        resized.save(png_path, format='PNG')
        print(f"  Created: {png_path}")
    
    # Main icon.png (256x256 for Linux)
    icon_png = img.resize((256, 256), Image.LANCZOS)
    icon_png_path = output_dir / "icon.png"
    icon_png.save(icon_png_path, format='PNG')
    print(f"  Created: {icon_png_path}")
    
    # =========================================================================
    # Web favicon PNGs
    # =========================================================================
    print("\n[Web] Generating web favicon PNGs...")
    
    if static_folder.exists():
        # favicon-16x16.png
        resized = img.resize((16, 16), Image.LANCZOS)
        resized.save(static_folder / "favicon-16x16.png", format='PNG')
        print(f"  Created: {static_folder / 'favicon-16x16.png'}")
        
        # favicon-32x32.png
        resized = img.resize((32, 32), Image.LANCZOS)
        resized.save(static_folder / "favicon-32x32.png", format='PNG')
        print(f"  Created: {static_folder / 'favicon-32x32.png'}")
        
        # apple-touch-icon.png (180x180)
        resized = img.resize((180, 180), Image.LANCZOS)
        resized.save(static_folder / "apple-touch-icon.png", format='PNG')
        print(f"  Created: {static_folder / 'apple-touch-icon.png'}")
        
        # android-chrome icons
        resized = img.resize((192, 192), Image.LANCZOS)
        resized.save(static_folder / "android-chrome-192x192.png", format='PNG')
        print(f"  Created: {static_folder / 'android-chrome-192x192.png'}")
        
        resized = img.resize((512, 512), Image.LANCZOS)
        resized.save(static_folder / "android-chrome-512x512.png", format='PNG')
        print(f"  Created: {static_folder / 'android-chrome-512x512.png'}")
    
    # =========================================================================
    # Summary
    # =========================================================================
    print("\n" + "=" * 60)
    print("Icon generation complete!")
    print("=" * 60)
    print(f"\nOutput directory: {output_dir}")
    print("\nFiles created:")
    print("  - icon.ico        (Windows app/installer icon)")
    print("  - icon.png        (Linux icon)")
    print("  - favicon.ico     (Web favicon)")
    print("  - icon-NxN.png    (Various sizes)")
    if static_folder.exists():
        print("\nWeb files in /static:")
        print("  - favicon.ico, favicon-16x16.png, favicon-32x32.png")
        print("  - apple-touch-icon.png (iOS)")
        print("  - android-chrome-192x192.png, android-chrome-512x512.png")
    
    print("\nNote: For macOS .icns files, use an online converter or:")
    print("  brew install libicns")
    print("  png2icns icon.icns icon-16x16.png icon-32x32.png icon-128x128.png icon-256x256.png icon-512x512.png")


if __name__ == "__main__":
    if len(sys.argv) < 2:
        print(__doc__)
        print("\nExample:")
        print("  python generate_icons.py logo.png")
        sys.exit(1)
    
    source_image = sys.argv[1]
    output_directory = sys.argv[2] if len(sys.argv) > 2 else None
    
    generate_icons(source_image, output_directory)
