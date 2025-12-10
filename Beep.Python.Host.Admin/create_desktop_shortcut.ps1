# Create Desktop Shortcut for Beep Python Host
# Run this script to create a desktop icon that launches the app

$WScriptShell = New-Object -ComObject WScript.Shell

# Get paths
$AppDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$BatchFile = Join-Path $AppDir "BeepPythonHost.bat"
$DesktopPath = [Environment]::GetFolderPath("Desktop")
$ShortcutPath = Join-Path $DesktopPath "Beep Python Host.lnk"

# Check if icon exists
$IconPath = Join-Path $AppDir "static\favicon.ico"
if (-not (Test-Path $IconPath)) {
    # Try alternate icon locations
    $IconPath = Join-Path $AppDir "icon.ico"
    if (-not (Test-Path $IconPath)) {
        Write-Host "Icon file not found. Shortcut will use default icon." -ForegroundColor Yellow
        $IconPath = $null
    }
}

# Create shortcut
$Shortcut = $WScriptShell.CreateShortcut($ShortcutPath)
$Shortcut.TargetPath = $BatchFile
$Shortcut.WorkingDirectory = $AppDir
$Shortcut.Description = "Beep Python Host Admin - Python Environment Manager"
if ($IconPath) {
    $Shortcut.IconLocation = $IconPath
}
$Shortcut.Save()

Write-Host "✓ Desktop shortcut created successfully!" -ForegroundColor Green
Write-Host "  Location: $ShortcutPath" -ForegroundColor Cyan
Write-Host ""
Write-Host "You can now double-click the shortcut to launch Beep Python Host." -ForegroundColor White

# Keep window open
Read-Host "Press Enter to continue"
