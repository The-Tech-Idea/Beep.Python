"""
Beep.Python Host Admin - Standalone Cross-Platform Installer
============================================================
This creates a SINGLE executable installer that:
- Has Python embedded
- Has all application files bundled inside
- Shows a wizard to select install location
- Saves ALL settings in the app folder (portable)
- Works on Windows, macOS, and Linux
"""
import os
import sys
import json
import shutil
import platform
import subprocess
import threading
import zipfile
import tempfile
from pathlib import Path
from datetime import datetime

# Try to import tkinter
try:
    import tkinter as tk
    from tkinter import ttk, filedialog, messagebox
    HAS_TK = True
except ImportError:
    HAS_TK = False


def get_bundle_dir() -> Path:
    """Get directory where bundled files are located"""
    if getattr(sys, 'frozen', False):
        # Running as compiled installer executable - files are in _MEIPASS
        return Path(sys._MEIPASS) / "app_bundle"
    else:
        # Running as script - use project root directory
        return Path(__file__).parent.parent


def get_python_embedded_dir() -> Path:
    """Get directory where embedded Python is located"""
    if getattr(sys, 'frozen', False):
        # Running as compiled executable - files are in _MEIPASS
        return Path(sys._MEIPASS) / "python_embedded"
    else:
        # Running as script - use project folder
        return Path(__file__).parent.parent / "python-embedded"


def get_default_install_path() -> str:
    """Get platform-appropriate default installation path"""
    system = platform.system()
    if system == "Windows":
        # Default to user's home directory for portability
        return str(Path.home() / "BeepPython")
    elif system == "Darwin":
        return str(Path.home() / "BeepPython")
    else:
        return str(Path.home() / "BeepPython")


class InstallerWizard(tk.Tk):
    """Main installer wizard window"""
    
    def __init__(self):
        super().__init__()
        
        self.title("Beep.Python Host Admin - Setup")
        self.geometry("650x580")
        self.minsize(650, 580)
        self.resizable(True, True)
        
        # Center window
        self.update_idletasks()
        x = (self.winfo_screenwidth() - 650) // 2
        y = (self.winfo_screenheight() - 580) // 2
        self.geometry(f"+{x}+{y}")
        
        # Installation settings
        self.install_path = tk.StringVar(value=get_default_install_path())
        self.server_port = tk.StringVar(value="5000")
        self.server_host = tk.StringVar(value="127.0.0.1")
        self.create_shortcut = tk.BooleanVar(value=True)
        self.launch_after = tk.BooleanVar(value=True)
        self.open_browser = tk.BooleanVar(value=True)
        
        # Pages
        self.current_page = 0
        self.pages = []
        
        self._create_ui()
        self._show_page(0)
    
    def _create_ui(self):
        # Main container
        self.main_frame = ttk.Frame(self)
        self.main_frame.pack(fill=tk.BOTH, expand=True)
        
        # Page container
        self.page_container = ttk.Frame(self.main_frame)
        self.page_container.pack(fill=tk.BOTH, expand=True, padx=20, pady=10)
        
        # Create pages
        self.pages = [
            self._create_welcome_page(),
            self._create_location_page(),
            self._create_config_page(),
            self._create_summary_page(),
            self._create_progress_page(),
            self._create_complete_page(),
        ]
        
        # Separator
        ttk.Separator(self.main_frame, orient=tk.HORIZONTAL).pack(fill=tk.X, side=tk.BOTTOM, pady=(10, 0))
        
        # Button bar
        btn_frame = ttk.Frame(self.main_frame)
        btn_frame.pack(fill=tk.X, side=tk.BOTTOM, pady=10, padx=20)
        
        self.cancel_btn = ttk.Button(btn_frame, text="Cancel", command=self._on_cancel)
        self.cancel_btn.pack(side=tk.LEFT)
        
        self.next_btn = ttk.Button(btn_frame, text="Next >", command=self._on_next)
        self.next_btn.pack(side=tk.RIGHT)
        
        self.back_btn = ttk.Button(btn_frame, text="< Back", command=self._on_back)
        self.back_btn.pack(side=tk.RIGHT, padx=(0, 10))
    
    def _create_welcome_page(self) -> ttk.Frame:
        page = ttk.Frame(self.page_container)
        
        ttk.Label(page, text="Welcome to Beep.Python Host Admin", 
                 font=('Helvetica', 16, 'bold')).pack(pady=(30, 20))
        
        desc = """
This wizard will install Beep.Python Host Admin on your computer.

Beep.Python is a professional Python Environment and LLM Management 
System that allows you to:

  • Manage Python virtual environments
  • Discover and download LLM models
  • Run local inference with GPU acceleration
  • Manage RAG pipelines

The application is fully portable - all settings and data will be 
stored in the installation folder you choose.

Click 'Next' to continue.
        """
        ttk.Label(page, text=desc, justify=tk.LEFT).pack(pady=10, padx=20)
        
        # System info
        info_frame = ttk.LabelFrame(page, text="System Information")
        info_frame.pack(pady=20, padx=20, fill=tk.X)
        ttk.Label(info_frame, text=f"OS: {platform.system()} {platform.release()}").pack(anchor=tk.W, padx=10, pady=2)
        ttk.Label(info_frame, text=f"Architecture: {platform.machine()}").pack(anchor=tk.W, padx=10, pady=(2, 10))
        
        return page
    
    def _create_location_page(self) -> ttk.Frame:
        page = ttk.Frame(self.page_container)
        
        ttk.Label(page, text="Choose Installation Location", 
                 font=('Helvetica', 14, 'bold')).pack(pady=(30, 10))
        
        ttk.Label(page, text="Select where to install Beep.Python Host Admin.\n"
                           "All application files, settings, and data will be stored here.",
                 justify=tk.CENTER).pack(pady=(0, 20))
        
        # Path selection
        path_frame = ttk.Frame(page)
        path_frame.pack(fill=tk.X, padx=40, pady=10)
        
        ttk.Label(path_frame, text="Installation Folder:").pack(anchor=tk.W)
        
        entry_frame = ttk.Frame(path_frame)
        entry_frame.pack(fill=tk.X, pady=5)
        
        ttk.Entry(entry_frame, textvariable=self.install_path, width=50).pack(side=tk.LEFT, fill=tk.X, expand=True)
        ttk.Button(entry_frame, text="Browse...", command=self._browse_location).pack(side=tk.LEFT, padx=(10, 0))
        
        # Info
        info_frame = ttk.LabelFrame(page, text="Portable Installation")
        info_frame.pack(pady=20, padx=40, fill=tk.X)
        
        info_text = """
This is a PORTABLE installation:
  • All settings saved in: [Install Folder]/config/
  • All data stored in: [Install Folder]/data/
  • You can move the entire folder to another location
  • No registry entries or system files modified
        """
        ttk.Label(info_frame, text=info_text, justify=tk.LEFT).pack(padx=10, pady=10)
        
        return page
    
    def _create_config_page(self) -> ttk.Frame:
        page = ttk.Frame(self.page_container)
        
        ttk.Label(page, text="Server Configuration", 
                 font=('Helvetica', 14, 'bold')).pack(pady=(30, 10))
        
        ttk.Label(page, text="Configure the web server settings:").pack(pady=(0, 20))
        
        # Server settings
        settings_frame = ttk.Frame(page)
        settings_frame.pack(fill=tk.X, padx=40, pady=10)
        
        # Port
        port_frame = ttk.Frame(settings_frame)
        port_frame.pack(fill=tk.X, pady=5)
        ttk.Label(port_frame, text="Server Port:", width=15).pack(side=tk.LEFT)
        ttk.Entry(port_frame, textvariable=self.server_port, width=10).pack(side=tk.LEFT)
        ttk.Label(port_frame, text="  (default: 5000)").pack(side=tk.LEFT)
        
        # Host
        host_frame = ttk.Frame(settings_frame)
        host_frame.pack(fill=tk.X, pady=5)
        ttk.Label(host_frame, text="Server Host:", width=15).pack(side=tk.LEFT)
        host_combo = ttk.Combobox(host_frame, textvariable=self.server_host, width=15,
                                  values=["127.0.0.1", "0.0.0.0", "localhost"])
        host_combo.pack(side=tk.LEFT)
        
        # Options
        options_frame = ttk.LabelFrame(settings_frame, text="Options")
        options_frame.pack(fill=tk.X, pady=20)
        
        ttk.Checkbutton(options_frame, text="Create desktop shortcut", 
                       variable=self.create_shortcut).pack(anchor=tk.W, padx=10, pady=5)
        ttk.Checkbutton(options_frame, text="Launch application after installation", 
                       variable=self.launch_after).pack(anchor=tk.W, padx=10, pady=2)
        ttk.Checkbutton(options_frame, text="Open browser automatically when app starts", 
                       variable=self.open_browser).pack(anchor=tk.W, padx=10, pady=(2, 10))
        
        return page
    
    def _create_summary_page(self) -> ttk.Frame:
        page = ttk.Frame(self.page_container)
        
        ttk.Label(page, text="Ready to Install", 
                 font=('Helvetica', 14, 'bold')).pack(pady=(30, 10))
        
        ttk.Label(page, text="Review your settings and click 'Install' to begin.").pack(pady=(0, 20))
        
        # Summary
        self.summary_text = tk.Text(page, height=15, width=60, state=tk.DISABLED)
        self.summary_text.pack(padx=40, pady=10)
        
        return page
    
    def _create_progress_page(self) -> ttk.Frame:
        page = ttk.Frame(self.page_container)
        
        ttk.Label(page, text="Installing...", 
                 font=('Helvetica', 14, 'bold')).pack(pady=(30, 10))
        
        self.progress_status = ttk.Label(page, text="Preparing installation...")
        self.progress_status.pack(pady=10)
        
        self.progress_bar = ttk.Progressbar(page, length=400, mode='determinate')
        self.progress_bar.pack(pady=20)
        
        # Log
        log_frame = ttk.LabelFrame(page, text="Installation Log")
        log_frame.pack(fill=tk.BOTH, expand=True, padx=40, pady=10)
        
        self.log_text = tk.Text(log_frame, height=10, width=60, state=tk.DISABLED)
        scrollbar = ttk.Scrollbar(log_frame, orient=tk.VERTICAL, command=self.log_text.yview)
        self.log_text.configure(yscrollcommand=scrollbar.set)
        self.log_text.pack(side=tk.LEFT, fill=tk.BOTH, expand=True)
        scrollbar.pack(side=tk.RIGHT, fill=tk.Y)
        
        return page
    
    def _create_complete_page(self) -> ttk.Frame:
        page = ttk.Frame(self.page_container)
        
        ttk.Label(page, text="Installation Complete!", 
                 font=('Helvetica', 16, 'bold')).pack(pady=(40, 20))
        
        ttk.Label(page, text="Beep.Python Host Admin has been successfully installed.",
                 font=('Helvetica', 11)).pack(pady=10)
        
        # Info
        self.complete_info = ttk.Label(page, text="", justify=tk.LEFT)
        self.complete_info.pack(pady=20, padx=40)
        
        # Launch options
        options_frame = ttk.LabelFrame(page, text="Launch Options")
        options_frame.pack(pady=20, padx=40, fill=tk.X)
        
        self.launch_now_var = tk.BooleanVar(value=True)
        ttk.Checkbutton(options_frame, text="Launch Beep.Python Host Admin now",
                       variable=self.launch_now_var).pack(anchor=tk.W, padx=10, pady=5)
        
        self.open_browser_var = tk.BooleanVar(value=True)
        ttk.Checkbutton(options_frame, text="Open web browser",
                       variable=self.open_browser_var).pack(anchor=tk.W, padx=10, pady=(2, 10))
        
        return page
    
    def _browse_location(self):
        path = filedialog.askdirectory(initialdir=self.install_path.get())
        if path:
            self.install_path.set(path)
    
    def _show_page(self, index: int):
        # Hide all pages
        for page in self.pages:
            page.pack_forget()
        
        # Show current page
        self.pages[index].pack(fill=tk.BOTH, expand=True)
        
        # Update buttons
        self.back_btn.config(state=tk.NORMAL if index > 0 else tk.DISABLED)
        
        if index == 3:  # Summary page
            self._update_summary()
            self.next_btn.config(text="Install")
        elif index == 4:  # Progress page
            self.next_btn.config(state=tk.DISABLED)
            self.back_btn.config(state=tk.DISABLED)
            self.cancel_btn.config(state=tk.DISABLED)
            self.after(100, self._run_installation)
        elif index == 5:  # Complete page
            self._update_complete_info()
            self.next_btn.config(text="Finish", state=tk.NORMAL)
            self.back_btn.config(state=tk.DISABLED)
            self.cancel_btn.config(state=tk.DISABLED)
        else:
            self.next_btn.config(text="Next >")
    
    def _update_summary(self):
        summary = f"""Installation Summary:

Installation Folder:
  {self.install_path.get()}

Server Configuration:
  Host: {self.server_host.get()}
  Port: {self.server_port.get()}

Options:
  Create desktop shortcut: {'Yes' if self.create_shortcut.get() else 'No'}
  Launch after install: {'Yes' if self.launch_after.get() else 'No'}
  Auto-open browser: {'Yes' if self.open_browser.get() else 'No'}

Files will be installed:
  Application: [Install Folder]/BeepPythonHost.exe
  Configuration: [Install Folder]/config/
  Data: [Install Folder]/data/
  Logs: [Install Folder]/logs/
"""
        self.summary_text.config(state=tk.NORMAL)
        self.summary_text.delete(1.0, tk.END)
        self.summary_text.insert(tk.END, summary)
        self.summary_text.config(state=tk.DISABLED)
    
    def _update_complete_info(self):
        info = f"""Installation Location: {self.install_path.get()}

Access URL: http://{self.server_host.get()}:{self.server_port.get()}

To start the application:
  • Use the desktop shortcut, or
  • Run BeepPythonHost.exe from the installation folder

All settings are stored in the installation folder.
You can move the entire folder to make it portable.
"""
        self.complete_info.config(text=info)
    
    def _on_back(self):
        if self.current_page > 0:
            self.current_page -= 1
            self._show_page(self.current_page)
    
    def _on_next(self):
        # Validate current page
        if self.current_page == 1:  # Location page
            path = self.install_path.get()
            if not path:
                messagebox.showerror("Error", "Please select an installation folder.")
                return
        
        if self.current_page == 2:  # Config page
            try:
                port = int(self.server_port.get())
                if port < 1 or port > 65535:
                    raise ValueError()
            except ValueError:
                messagebox.showerror("Error", "Please enter a valid port number (1-65535).")
                return
        
        if self.current_page < len(self.pages) - 1:
            self.current_page += 1
            self._show_page(self.current_page)
        else:
            # Finish - launch if selected
            if self.launch_now_var.get():
                self._launch_app()
            self.destroy()
    
    def _on_cancel(self):
        if messagebox.askyesno("Cancel", "Are you sure you want to cancel the installation?"):
            self.destroy()
    
    def _log(self, message: str):
        self.log_text.config(state=tk.NORMAL)
        self.log_text.insert(tk.END, message + "\n")
        self.log_text.see(tk.END)
        self.log_text.config(state=tk.DISABLED)
        self.update_idletasks()
    
    def _set_progress(self, value: int, status: str = None):
        self.progress_bar['value'] = value
        if status:
            self.progress_status.config(text=status)
        self.update_idletasks()
    
    def _run_installation(self):
        thread = threading.Thread(target=self._install, daemon=True)
        thread.start()
        self._check_install_complete()
    
    def _check_install_complete(self):
        if hasattr(self, 'install_done') and self.install_done:
            if hasattr(self, 'install_error') and self.install_error:
                messagebox.showerror("Installation Failed", f"An error occurred:\n{self.install_error}")
                self.cancel_btn.config(state=tk.NORMAL)
                self.back_btn.config(state=tk.NORMAL)
            else:
                self.current_page += 1
                self._show_page(self.current_page)
        else:
            self.after(100, self._check_install_complete)
    
    def _install(self):
        try:
            install_path = Path(self.install_path.get())
            source_dir = get_bundle_dir()
            
            # Step 1: Create directories
            self._set_progress(5, "Creating directories...")
            self._log("Creating installation directories...")
            
            install_path.mkdir(parents=True, exist_ok=True)
            (install_path / "config").mkdir(exist_ok=True)
            (install_path / "data").mkdir(exist_ok=True)
            (install_path / "logs").mkdir(exist_ok=True)
            (install_path / "python").mkdir(exist_ok=True)
            (install_path / "providers").mkdir(exist_ok=True)
            (install_path / "servers").mkdir(exist_ok=True)
            (install_path / "models").mkdir(exist_ok=True)  # LLM models directory
            (install_path / "cache").mkdir(exist_ok=True)   # Cache directory
            (install_path / "cache" / "models").mkdir(exist_ok=True)  # Models cache
            (install_path / "rag_data").mkdir(exist_ok=True)  # RAG data directory
            (install_path / "environments").mkdir(exist_ok=True)  # Virtual environments
            
            self._log(f"  Created: {install_path}")
            
            # Step 2: Copy application files
            self._set_progress(10, "Copying application files...")
            self._log("Copying application files...")
            
            # Debug: Show source directory info
            self._log(f"  Source directory: {source_dir}")
            self._log(f"  Source exists: {source_dir.exists()}")
            if source_dir.exists():
                items_list = list(source_dir.iterdir())
                self._log(f"  Items in source: {[i.name for i in items_list]}")
            
            if source_dir.exists():
                # Copy ALL files and directories from source
                total_items = sum(1 for _ in source_dir.iterdir())
                copied = 0
                
                # Skip these directories/files (build artifacts, caches, etc.)
                skip_items = {
                    '__pycache__', '.git', '.venv', 'venv', 'dist', 'build', 
                    '.pytest_cache', '.mypy_cache', '*.pyc', '*.pyo',
                    'node_modules', '.dependencies_installed'
                }
                
                for item in source_dir.iterdir():
                    # Skip build artifacts and caches
                    if item.name in skip_items or item.name.startswith('.'):
                        if item.name not in ['.gitignore']:  # Keep .gitignore
                            self._log(f"  Skipping: {item.name}")
                            continue
                    
                    src = item
                    dst = install_path / item.name
                    
                    try:
                        if src.is_dir():
                            if dst.exists():
                                shutil.rmtree(dst)
                            shutil.copytree(src, dst, ignore=shutil.ignore_patterns(
                                '__pycache__', '*.pyc', '*.pyo', '.git', '.pytest_cache'
                            ))
                        else:
                            shutil.copy2(src, dst)
                        
                        copied += 1
                        progress = 10 + int((copied / max(total_items, 1)) * 40)
                        self._set_progress(progress)
                        self._log(f"  Copied: {item.name}")
                    except Exception as e:
                        self._log(f"  Error copying {item.name}: {e}")
            else:
                self._log(f"  Warning: Source not found at {source_dir}")
                self._log("  Running in development mode - skipping file copy")
            
            # Step 2b: Copy embedded Python runtime
            self._set_progress(55, "Copying embedded Python...")
            self._log("Copying embedded Python runtime...")
            
            python_embedded_src = get_python_embedded_dir()
            python_embedded_dst = install_path / "python-embedded"
            
            if python_embedded_src.exists():
                if python_embedded_dst.exists():
                    shutil.rmtree(python_embedded_dst)
                shutil.copytree(python_embedded_src, python_embedded_dst)
                self._log(f"  Copied: python-embedded ({python_embedded_src})")
            else:
                self._log(f"  Note: Embedded Python not found at {python_embedded_src}")
                self._log("  The app will use system Python if available")
            
            # Step 3: Write configuration (IN THE APP FOLDER)
            self._set_progress(65, "Writing configuration...")
            self._log("Writing configuration...")
            
            # Write install settings (app_settings.json)
            install_settings = {
                "server_host": self.server_host.get(),
                "server_port": int(self.server_port.get()),
                "open_browser": self.open_browser.get(),
                "version": "1.0.0",
                "install_date": str(datetime.now().isoformat()),
            }
            
            config_file = install_path / "config" / "app_settings.json"
            with open(config_file, 'w') as f:
                json.dump(install_settings, f, indent=2)
            
            self._log(f"  Saved: {config_file}")
            
            # IMPORTANT: Reset app_config.json to force setup wizard on first run
            # Set is_configured=False so the setup wizard will show
            app_config = {
                "is_configured": False,
                "setup_required": True,
                "fresh_install": True
            }
            
            app_config_file = install_path / "config" / "app_config.json"
            with open(app_config_file, 'w') as f:
                json.dump(app_config, f, indent=2)
            
            self._log(f"  Saved: {app_config_file} (setup wizard will show on first run)")
            
            # Write model directories config with correct absolute paths
            model_dirs_config = {
                "directories": [
                    {
                        "id": "default",
                        "name": "Default Models",
                        "path": str(install_path / "models"),
                        "enabled": True,
                        "max_size_gb": None,
                        "description": "Default model storage directory",
                        "priority": 0
                    }
                ]
            }
            
            model_dirs_file = install_path / "config" / "model_directories.json"
            with open(model_dirs_file, 'w') as f:
                json.dump(model_dirs_config, f, indent=2)
            
            self._log(f"  Saved: {model_dirs_file}")
            
            # Write repositories config
            repos_config = {
                "repositories": [
                    {
                        "id": "hf_default",
                        "name": "HuggingFace Hub",
                        "type": "huggingface",
                        "enabled": True,
                        "url": None,
                        "api_key": None,
                        "description": "Default HuggingFace repository. Token required for gated/private models.",
                        "priority": 0,
                        "requires_auth": True,
                        "auth_required_for": "gated and private models"
                    },
                    {
                        "id": "ollama_default",
                        "name": "Ollama",
                        "type": "ollama",
                        "enabled": False,
                        "url": None,
                        "api_key": None,
                        "description": "Ollama model repository",
                        "priority": 1,
                        "requires_auth": False,
                        "auth_required_for": None
                    }
                ]
            }
            
            repos_file = install_path / "config" / "repositories.json"
            with open(repos_file, 'w') as f:
                json.dump(repos_config, f, indent=2)
            
            # Step 4: Create launcher script that reads local config
            self._set_progress(75, "Creating launcher...")
            self._log("Creating launcher script...")
            
            self._create_launcher(install_path)
            
            # Step 5: Create shortcuts
            self._set_progress(85, "Creating shortcuts...")
            if self.create_shortcut.get():
                self._log("Creating shortcuts...")
                self._create_shortcuts(install_path)
            else:
                self._log("Skipping shortcut creation")
            
            # Step 6: Finalize
            self._set_progress(100, "Installation complete!")
            self._log("")
            self._log("Installation completed successfully!")
            
            self.install_done = True
            
        except Exception as e:
            self.install_error = str(e)
            self.install_done = True
            self._log(f"ERROR: {e}")
    
    def _create_launcher(self, install_path: Path):
        """Create launcher scripts that use embedded Python (NO virtual environment)"""
        system = platform.system()
        
        # Windows batch launcher - uses embedded Python directly
        launcher_bat = install_path / "BeepPythonHost.bat"
        launcher_bat_content = f'''@echo off
REM Beep Python Host Admin Launcher
REM Uses embedded Python - no virtual environment needed

setlocal enabledelayedexpansion

set "APP_DIR=%~dp0"
cd /d "%APP_DIR%"

REM Use embedded Python (required - no fallback)
if exist "python-embedded\\python.exe" (
    set "PYTHON_EXE=%APP_DIR%python-embedded\\python.exe"
    set "PYTHONPATH=%APP_DIR%python-embedded;%APP_DIR%python-embedded\\Lib;%APP_DIR%python-embedded\\Lib\\site-packages"
) else (
    echo ERROR: Embedded Python not found!
    echo Please ensure python-embedded folder exists.
    pause
    exit /b 1
)

REM Set environment variables
set PYTHONUNBUFFERED=1
set BEEP_PYTHON_HOME=%APP_DIR%
set BEEP_CONFIG_DIR=%APP_DIR%config
set BEEP_DATA_DIR=%APP_DIR%data
set HOST={self.server_host.get()}
set PORT={self.server_port.get()}

REM Launch the application - dependencies pre-installed in embedded Python
echo Starting Beep Python Host Admin...
"%PYTHON_EXE%" run.py

if errorlevel 1 (
    echo.
    echo Application exited with error
    pause
)

endlocal
'''
        launcher_bat.write_text(launcher_bat_content, encoding='utf-8')
        self._log(f"  Created: BeepPythonHost.bat")
        
        # Windows silent launcher (VBScript)
        launcher_vbs = install_path / "BeepPythonHost_Silent.vbs"
        launcher_vbs_content = """' Beep Python Host Admin - Silent Launcher
' Launches the application without showing console window

Set objShell = CreateObject("WScript.Shell")
Set objFSO = CreateObject("Scripting.FileSystemObject")

' Get script directory
strScriptPath = objFSO.GetParentFolderName(WScript.ScriptFullName)

' Launch batch file hidden
objShell.Run Chr(34) & strScriptPath & Chr(92) & "BeepPythonHost.bat" & Chr(34), 0, False

Set objShell = Nothing
Set objFSO = Nothing
"""
        launcher_vbs.write_text(launcher_vbs_content, encoding='utf-8')
        self._log(f"  Created: BeepPythonHost_Silent.vbs (hidden console)")
        
        # Linux/macOS shell launcher - uses embedded Python directly
        launcher_sh = install_path / "BeepPythonHost.sh"
        launcher_sh_content = f'''#!/bin/bash
# Beep Python Host Admin Launcher
# Uses embedded Python - no virtual environment needed

APP_DIR="$(cd "$(dirname "${{BASH_SOURCE[0]}}")" && pwd)"
cd "$APP_DIR"

GREEN='\\033[0;32m'
RED='\\033[0;31m'
NC='\\033[0m'

echo -e "${{GREEN}}Beep Python Host Admin${{NC}}"
echo "========================================"

# Use embedded Python (required - no fallback)
if [ -f "python-embedded/bin/python3" ]; then
    PYTHON_EXE="$APP_DIR/python-embedded/bin/python3"
    export PYTHONPATH="$APP_DIR/python-embedded:$APP_DIR/python-embedded/lib/python3.11:$APP_DIR/python-embedded/lib/python3.11/site-packages"
elif [ -f "python-embedded/python" ]; then
    PYTHON_EXE="$APP_DIR/python-embedded/python"
    export PYTHONPATH="$APP_DIR/python-embedded"
else
    echo -e "${{RED}}ERROR: Embedded Python not found!${{NC}}"
    echo "Please ensure python-embedded folder exists."
    read -p "Press Enter to exit..."
    exit 1
fi

export PYTHONUNBUFFERED=1
export BEEP_PYTHON_HOME="$APP_DIR"
export BEEP_CONFIG_DIR="$APP_DIR/config"
export BEEP_DATA_DIR="$APP_DIR/data"
export HOST={self.server_host.get()}
export PORT={self.server_port.get()}

echo -e "${{GREEN}}Starting Beep Python Host Admin...${{NC}}"
"$PYTHON_EXE" run.py

if [ $? -ne 0 ]; then
    echo -e "${{RED}}Application exited with error${{NC}}"
    read -p "Press Enter to continue..."
fi
'''
        launcher_sh.write_text(launcher_sh_content, encoding='utf-8')
        if system != "Windows":
            os.chmod(launcher_sh, 0o755)
        self._log(f"  Created: BeepPythonHost.sh")
        
        # macOS .command file
        launcher_command = install_path / "BeepPythonHost.command"
        launcher_command.write_text(launcher_sh_content, encoding='utf-8')
        if system != "Windows":
            os.chmod(launcher_command, 0o755)
        self._log(f"  Created: BeepPythonHost.command")
        
        # macOS silent launcher
        if system == "Darwin":
            launcher_scpt = install_path / "BeepPythonHost_Silent.scpt"
            launcher_scpt_content = '''-- Beep Python Host Admin - Silent Launcher for macOS
tell application "Finder"
    set scriptPath to POSIX path of (container of (path to me) as alias)
end tell
do shell script "cd " & quoted form of scriptPath & " && nohup ./BeepPythonHost.sh > /dev/null 2>&1 &"
'''
            launcher_scpt.write_text(launcher_scpt_content, encoding='utf-8')
            self._log(f"  Created: BeepPythonHost_Silent.scpt")
        
        # Windows desktop shortcut helper (single file)
        if system == "Windows":
            shortcut_bat = install_path / "create_desktop_shortcut.bat"
            shortcut_bat_content = '''@echo off
REM Create Desktop Shortcut for Beep Python Host
echo Creating desktop shortcut...

set "APP_DIR=%~dp0"
set "VBS_FILE=%APP_DIR%BeepPythonHost_Silent.vbs"
set "ICON_FILE=%APP_DIR%assets\\icon.ico"

REM Check if icon exists and include it
if exist "%ICON_FILE%" (
    powershell -ExecutionPolicy Bypass -Command "$ws = New-Object -ComObject WScript.Shell; $s = $ws.CreateShortcut([Environment]::GetFolderPath('Desktop') + '\\Beep Python Host.lnk'); $s.TargetPath = '%VBS_FILE%'; $s.WorkingDirectory = '%APP_DIR%'; $s.Description = 'Beep Python Host Admin'; $s.IconLocation = '%ICON_FILE%'; $s.Save(); Write-Host 'Desktop shortcut created with icon!' -ForegroundColor Green"
) else (
    powershell -ExecutionPolicy Bypass -Command "$ws = New-Object -ComObject WScript.Shell; $s = $ws.CreateShortcut([Environment]::GetFolderPath('Desktop') + '\\Beep Python Host.lnk'); $s.TargetPath = '%VBS_FILE%'; $s.WorkingDirectory = '%APP_DIR%'; $s.Description = 'Beep Python Host Admin'; $s.Save(); Write-Host 'Desktop shortcut created!' -ForegroundColor Green"
)

echo.
echo Shortcut created on Desktop!
pause
'''
            shortcut_bat.write_text(shortcut_bat_content, encoding='utf-8')
            self._log(f"  Created: create_desktop_shortcut.bat")
    
    def _create_shortcuts(self, install_path: Path):
        """Create desktop shortcuts by running the batch file"""
        system = platform.system()
        
        if system == "Windows":
            try:
                shortcut_bat = install_path / "create_desktop_shortcut.bat"
                
                if shortcut_bat.exists():
                    self._log(f"  Running: {shortcut_bat}")
                    
                    # Run the batch file to create shortcut
                    result = subprocess.run(
                        ["cmd", "/c", str(shortcut_bat)],
                        cwd=str(install_path),
                        capture_output=True, text=True, timeout=30
                    )
                    
                    # Check if shortcut was created
                    desktop = Path(os.environ.get('USERPROFILE', '')) / "Desktop"
                    shortcut_path = desktop / "Beep Python Host.lnk"
                    
                    if shortcut_path.exists():
                        self._log(f"  Created desktop shortcut successfully!")
                    else:
                        self._log(f"  Batch output: {result.stdout}")
                        if result.stderr:
                            self._log(f"  Batch errors: {result.stderr}")
                        self._log(f"  Note: Run 'create_desktop_shortcut.bat' manually")
                else:
                    self._log(f"  Warning: create_desktop_shortcut.bat not found")
                    self._log(f"  Creating shortcut directly...")
                    
                    # Fallback: create directly with PowerShell
                    desktop = Path(os.environ.get('USERPROFILE', '')) / "Desktop"
                    launcher_path = install_path / "BeepPythonHost_Silent.vbs"
                    shortcut_path = desktop / "Beep Python Host.lnk"
                    icon_path = install_path / "assets" / "icon.ico"
                    
                    icon_cmd = ""
                    if icon_path.exists():
                        icon_cmd = f"$s.IconLocation = '{icon_path}';"
                    
                    ps_cmd = f"$ws = New-Object -ComObject WScript.Shell; $s = $ws.CreateShortcut('{shortcut_path}'); $s.TargetPath = '{launcher_path}'; $s.WorkingDirectory = '{install_path}'; {icon_cmd} $s.Save()"
                    
                    subprocess.run(
                        ["powershell", "-ExecutionPolicy", "Bypass", "-Command", ps_cmd],
                        capture_output=True, timeout=30
                    )
                    
                    if shortcut_path.exists():
                        self._log(f"  Created desktop shortcut!")
                    else:
                        self._log(f"  Failed to create shortcut")
                    
            except Exception as e:
                self._log(f"  Shortcut creation error: {e}")
        
        elif system == "Darwin":
            # macOS - create .command file on desktop
            try:
                desktop = Path.home() / "Desktop"
                command_file = desktop / "Beep Python Host.command"
                launcher_path = install_path / "BeepPythonHost.command"
                command_file.write_text(f'''#!/bin/bash
cd "{install_path}"
./BeepPythonHost.command
''')
                os.chmod(command_file, 0o755)
                self._log(f"  Created: {command_file}")
            except Exception as e:
                self._log(f"  Note: You can double-click BeepPythonHost.command to launch")
        
        else:
            # Linux - create .desktop file
            try:
                desktop = Path.home() / "Desktop"
                desktop_file = desktop / "beeppython.desktop"
                launcher_path = install_path / "BeepPythonHost.sh"
                desktop_file.write_text(f'''[Desktop Entry]
Type=Application
Name=Beep Python Host Admin
Comment=Python Environment Manager
Exec={launcher_path}
Path={install_path}
Terminal=false
Categories=Development;
''')
                os.chmod(desktop_file, 0o755)
                self._log(f"  Created: {desktop_file}")
            except Exception as e:
                self._log(f"  Note: You can run ./BeepPythonHost.sh to launch")
    
    def _launch_app(self):
        """Launch the installed application"""
        install_path = Path(self.install_path.get())
        system = platform.system()
        
        try:
            if system == "Windows":
                launcher = install_path / "BeepPythonHost.bat"
                if launcher.exists():
                    subprocess.Popen([str(launcher)], cwd=str(install_path), 
                                   creationflags=subprocess.CREATE_NEW_CONSOLE)
                else:
                    self._log("Launcher not found")
            else:
                launcher = install_path / "BeepPythonHost.sh"
                if launcher.exists():
                    subprocess.Popen([str(launcher)], cwd=str(install_path),
                                   start_new_session=True)
                else:
                    self._log("Launcher not found")
            
            # Open browser if requested
            if self.open_browser.get():
                import webbrowser
                import time
                def open_delayed():
                    time.sleep(2)
                    webbrowser.open(f"http://localhost:{self.server_port.get()}")
                threading.Thread(target=open_delayed, daemon=True).start()
                
        except Exception as e:
            messagebox.showwarning("Warning", f"Could not launch application:\n{e}")


def run_cli():
    """Command-line fallback installer"""
    print("=" * 60)
    print("Beep.Python Host Admin - Installer")
    print("=" * 60)
    print()
    
    default_path = get_default_install_path()
    print(f"Installation folder [{default_path}]: ", end="")
    install_path = input().strip() or default_path
    
    print(f"Server port [5000]: ", end="")
    port = input().strip() or "5000"
    
    print()
    print(f"Installing to: {install_path}")
    print(f"Server port: {port}")
    print()
    print("Continue? [Y/n]: ", end="")
    
    if input().strip().lower() not in ('', 'y', 'yes'):
        print("Cancelled.")
        return
    
    # Simple installation
    install_path = Path(install_path)
    source_dir = get_bundle_dir()
    
    print("Creating directories...")
    install_path.mkdir(parents=True, exist_ok=True)
    (install_path / "config").mkdir(exist_ok=True)
    (install_path / "data").mkdir(exist_ok=True)
    (install_path / "logs").mkdir(exist_ok=True)
    
    print("Copying files...")
    if source_dir.exists():
        for item in source_dir.iterdir():
            src = source_dir / item.name
            dst = install_path / item.name
            if src.is_dir():
                if dst.exists():
                    shutil.rmtree(dst)
                shutil.copytree(src, dst)
            else:
                shutil.copy2(src, dst)
            print(f"  {item.name}")
    
    print("Writing config...")
    config = {"server_port": int(port), "server_host": "127.0.0.1"}
    with open(install_path / "config" / "app_settings.json", 'w') as f:
        json.dump(config, f, indent=2)
    
    print()
    print("Installation complete!")
    print(f"Run: {install_path / 'BeepPythonHost.exe'}")


def main():
    if HAS_TK:
        try:
            app = InstallerWizard()
            app.mainloop()
        except Exception as e:
            print(f"GUI Error: {e}")
            run_cli()
    else:
        run_cli()


if __name__ == "__main__":
    main()
