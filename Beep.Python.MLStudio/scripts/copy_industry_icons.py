"""
Script to copy industry icons from external icon packs to ML Studio's static directory
Run this script to copy all necessary icons into the app structure
"""
import shutil
from pathlib import Path
import os

# Base paths
APP_ROOT = Path(__file__).parent.parent
ICON_PACKS_ROOT = Path(r'H:\dev\iconPacks\imgs')
STATIC_ICONS_DIR = APP_ROOT / 'static' / 'assets' / 'icons'
STATIC_IMAGES_DIR = APP_ROOT / 'static' / 'assets' / 'images'

# Create static directories if they don't exist
STATIC_ICONS_DIR.mkdir(parents=True, exist_ok=True)
STATIC_IMAGES_DIR.mkdir(parents=True, exist_ok=True)

# Industry icon mappings (same as Community)
INDUSTRY_ICON_SOURCES = {
    'oil_gas': {
        'source_dir': Path(r'H:\dev\iconPacks\oilandgas32mycollection\png'),
        'target_dir': STATIC_ICONS_DIR / 'oil_gas',
        'icons': [
            '007-refinery.png',
            '002-tanker.png',
            '003-oil-tank.png',
            '004-oil-tank-1.png',
            '005-storage-tank.png',
            '011-oil-industry.png',
            '057-oil-platform.png',
            '063-oil-rig.png',
            '066-oil-platform-1.png',
            '067-oil-rig-1.png',
            '069-oil-rig-3.png',
            '001-tank.png',
            '006-tank-1.png',
            '008-oil.png',
            '009-pump.png',
            '010-refinery-1.png',
            '039-oil-well.png',
            '040-oil-well-1.png',
            '041-oil-well-2.png'
        ]
    },
    'finance': {
        'source_dir': ICON_PACKS_ROOT / 'BusinessandFinance',
        'target_dir': STATIC_ICONS_DIR / 'finance',
        'icons': []  # Auto-scan recursively
    },
    'healthcare': {
        'source_dir': ICON_PACKS_ROOT / 'healthcare',
        'target_dir': STATIC_ICONS_DIR / 'healthcare',
        'icons': []  # Auto-scan recursively
    },
    'real_estate': {
        'source_dir': ICON_PACKS_ROOT / 'real-estate',
        'target_dir': STATIC_ICONS_DIR / 'real_estate',
        'icons': []  # Auto-scan recursively
    },
    'retail': {
        'source_dir': ICON_PACKS_ROOT / '1198327-retail',
        'target_dir': STATIC_ICONS_DIR / 'retail',
        'icons': []  # Auto-scan
    },
    'manufacturing': {
        'source_dir': ICON_PACKS_ROOT / '4070563-timing',
        'target_dir': STATIC_ICONS_DIR / 'manufacturing',
        'icons': []  # Auto-scan
    },
    'education': {
        'source_dir': ICON_PACKS_ROOT / '4070563-timing',
        'target_dir': STATIC_ICONS_DIR / 'education',
        'icons': []  # Auto-scan
    },
    'agriculture': {
        'source_dir': ICON_PACKS_ROOT / '4479880-agriculture',
        'target_dir': STATIC_ICONS_DIR / 'agriculture',
        'icons': []  # Auto-scan
    },
    'transportation': {
        'source_dir': ICON_PACKS_ROOT / '1493652-logistics',
        'target_dir': STATIC_ICONS_DIR / 'transportation',
        'icons': []  # Auto-scan
    },
    'energy': {
        'source_dir': ICON_PACKS_ROOT / '4514697-renewable-energy',
        'target_dir': STATIC_ICONS_DIR / 'energy',
        'icons': []  # Auto-scan
    },
    'insurance': {
        'source_dir': ICON_PACKS_ROOT / '2646182-insurance',
        'target_dir': STATIC_ICONS_DIR / 'insurance',
        'icons': []  # Auto-scan
    },
    'telecom': {
        'source_dir': ICON_PACKS_ROOT / '4070563-timing',
        'target_dir': STATIC_ICONS_DIR / 'telecom',
        'icons': []  # Auto-scan
    },
    'media': {
        'source_dir': ICON_PACKS_ROOT / '4070563-timing',
        'target_dir': STATIC_ICONS_DIR / 'media',
        'icons': []  # Auto-scan
    },
    'government': {
        'source_dir': ICON_PACKS_ROOT / '4070563-timing',
        'target_dir': STATIC_ICONS_DIR / 'government',
        'icons': []  # Auto-scan
    },
    'sports': {
        'source_dir': ICON_PACKS_ROOT / '4070563-timing',
        'target_dir': STATIC_ICONS_DIR / 'sports',
        'icons': []  # Auto-scan
    },
    'food_beverage': {
        'source_dir': ICON_PACKS_ROOT / '4070563-timing',
        'target_dir': STATIC_ICONS_DIR / 'food_beverage',
        'icons': []  # Auto-scan
    },
    'general': {
        'source_dir': STATIC_IMAGES_DIR,
        'target_dir': STATIC_ICONS_DIR / 'general',
        'icons': []  # Will use default logos
    }
}


def scan_directory_for_png(directory: Path, max_files: int = 20, recursive: bool = True) -> list:
    """Scan directory for PNG files (recursively if needed)"""
    png_files = []
    if directory.exists():
        try:
            if recursive:
                pattern = '**/*.png'
            else:
                pattern = '*.png'
            
            for file_path in sorted(directory.glob(pattern)):
                if len(png_files) >= max_files:
                    break
                png_files.append(file_path.name)
        except Exception as e:
            print(f"Error scanning {directory}: {e}")
    return png_files


def find_file_recursive(directory: Path, filename: str) -> Path:
    """Find a file recursively in directory and subdirectories"""
    exact_match = directory / filename
    if exact_match.exists():
        return exact_match
    
    filename_lower = filename.lower()
    for file_path in directory.rglob('*.png'):
        if file_path.name.lower() == filename_lower:
            return file_path
    
    return None


def copy_icons(industry: str, config: dict) -> int:
    """Copy icons for a specific industry"""
    source_dir = config['source_dir']
    target_dir = config['target_dir']
    icons = config['icons']
    
    # Handle multiple possible source directories
    possible_dirs = [source_dir]
    
    if industry == 'retail':
        possible_dirs.extend([
            ICON_PACKS_ROOT / '1326494-retail',
            ICON_PACKS_ROOT / '4293054-retail',
            ICON_PACKS_ROOT / '4379529-supermarket',
            ICON_PACKS_ROOT / '4536642-sales',
            ICON_PACKS_ROOT / '4543149-online-shopping',
            ICON_PACKS_ROOT / '4564267-ecommerce'
        ])
    elif industry == 'agriculture':
        possible_dirs.extend([
            ICON_PACKS_ROOT / '4584950-farming',
            ICON_PACKS_ROOT / '4590727-smart-farm',
            ICON_PACKS_ROOT / '4752022-irrigation'
        ])
    elif industry == 'transportation':
        possible_dirs.extend([
            ICON_PACKS_ROOT / '4320169-logistic-delivery',
            ICON_PACKS_ROOT / '4598554-logistics',
            ICON_PACKS_ROOT / '4693426-aviation',
            ICON_PACKS_ROOT / '4549850-travel'
        ])
    elif industry == 'energy':
        possible_dirs.extend([
            ICON_PACKS_ROOT / '4545243-nuclear-energy',
            ICON_PACKS_ROOT / '4549377-battery',
            ICON_PACKS_ROOT / '4815152-sustainable-energy',
            ICON_PACKS_ROOT / '6629098-smart-meters'
        ])
    elif industry == 'insurance':
        possible_dirs.extend([
            ICON_PACKS_ROOT / '4482096-health-insurance',
            ICON_PACKS_ROOT / '4485938-insurance',
            ICON_PACKS_ROOT / '4599211-insurance'
        ])
    elif industry == 'real_estate':
        possible_dirs.extend([
            ICON_PACKS_ROOT / '1011798-real-estate',
            ICON_PACKS_ROOT / '9676753-real-estate'
        ])
    
    # Find first existing directory
    source_dir = None
    for possible_dir in possible_dirs:
        if possible_dir.exists():
            source_dir = possible_dir
            break
    
    if not source_dir or not source_dir.exists():
        print(f"[WARNING] Source directory not found for {industry}: {possible_dirs[0]}")
        return 0
    
    target_dir.mkdir(parents=True, exist_ok=True)
    
    if not icons:
        icons = scan_directory_for_png(source_dir, max_files=20, recursive=True)
        print(f"   Auto-scanned {len(icons)} icons from {source_dir.name}")
    
    copied_count = 0
    not_found = []
    
    for icon_name in icons:
        source_file = find_file_recursive(source_dir, icon_name)
        
        if not source_file or not source_file.exists():
            not_found.append(icon_name)
            continue
        
        target_file = target_dir / icon_name
        
        try:
            shutil.copy2(source_file, target_file)
            copied_count += 1
            print(f"   [OK] Copied: {icon_name}")
        except Exception as e:
            print(f"   [ERROR] Failed to copy {icon_name}: {e}")
    
    if not_found:
        print(f"   [WARNING] Not found ({len(not_found)}): {', '.join(not_found[:5])}")
        if len(not_found) > 5:
            print(f"      ... and {len(not_found) - 5} more")
    
    return copied_count


def copy_default_images():
    """Copy default logo/images if they exist"""
    default_logo_source = Path(r'C:\Users\f_ald\OneDrive\SimpleInfoapps\LogoGFx\SimpleInfoApps.png')
    if default_logo_source.exists():
        STATIC_IMAGES_DIR.mkdir(parents=True, exist_ok=True)
        target = STATIC_IMAGES_DIR / 'SimpleInfoApps.png'
        shutil.copy2(default_logo_source, target)
        print(f"[OK] Copied default logo: SimpleInfoApps.png")
        return True
    return False


def main():
    """Main function to copy all industry icons"""
    print("=" * 60)
    print("Copying Industry Icons to ML Studio")
    print("=" * 60)
    print()
    
    print("1. Copying default images...")
    copy_default_images()
    print()
    
    total_copied = 0
    for industry, config in INDUSTRY_ICON_SOURCES.items():
        print(f"2. Copying {industry.replace('_', ' ').title()} icons...")
        copied = copy_icons(industry, config)
        total_copied += copied
        print(f"   => Copied {copied} icons\n")
    
    print("=" * 60)
    print(f"[SUCCESS] Complete! Copied {total_copied} icons total")
    print("=" * 60)
    print()
    print(f"Icons are now in: {STATIC_ICONS_DIR}")


if __name__ == '__main__':
    main()
