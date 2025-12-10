"""
Beep.Python Host Admin - Cross-Platform Installer
A GUI wizard for installing and configuring the application on Windows, macOS, and Linux.
"""
import os
import sys
import json
import shutil
import platform
import subprocess
import threading
from pathlib import Path

# Try to import tkinter (standard library)
try:
    import tkinter as tk
    from tkinter import ttk, filedialog, messagebox
    HAS_TK = True
except ImportError:
    HAS_TK = False


class InstallerConfig:
    """Holds installation configuration"""
    def __init__(self):
        self.install_path = self._get_default_install_path()
        self.data_path = self._get_default_data_path()
        self.server_host = "127.0.0.1"
        self.server_port = 5000
        self.create_shortcut = True
        self.auto_start = False
        self.open_browser = True
    
    def _get_default_install_path(self) -> str:
        system = platform.system()
        if system == "Windows":
            # Use user's local app data (doesn't require admin)
            return str(Path(os.environ.get('LOCALAPPDATA', Path.home() / "AppData" / "Local")) / "Programs" / "BeepPython")
        elif system == "Darwin":  # macOS
            # User's Applications folder (doesn't require admin)
            return str(Path.home() / "Applications" / "BeepPython")
        else:  # Linux
            return str(Path.home() / ".local" / "share" / "beep-python")
    
    def _get_default_data_path(self) -> str:
        system = platform.system()
        if system == "Windows":
            return str(Path(os.environ.get('LOCALAPPDATA', Path.home())) / "BeepPython")
        elif system == "Darwin":  # macOS
            return str(Path.home() / "Library" / "Application Support" / "BeepPython")
        else:  # Linux
            return str(Path(os.environ.get('XDG_DATA_HOME', Path.home() / ".local" / "share")) / "beep-python")
    
    def to_dict(self) -> dict:
        return {
            "install_path": self.install_path,
            "data_path": self.data_path,
            "server_host": self.server_host,
            "server_port": self.server_port,
            "create_shortcut": self.create_shortcut,
            "auto_start": self.auto_start,
            "open_browser": self.open_browser,
            "platform": platform.system(),
            "version": "1.0.0"
        }


class WizardPage(ttk.Frame):
    """Base class for wizard pages"""
    def __init__(self, parent, wizard):
        super().__init__(parent)
        self.wizard = wizard
        self.config = wizard.config
    
    def on_enter(self):
        """Called when page is shown"""
        pass
    
    def on_leave(self) -> bool:
        """Called when leaving page. Return False to prevent navigation."""
        return True
    
    def validate(self) -> bool:
        """Validate page inputs"""
        return True


class WelcomePage(WizardPage):
    """Welcome page with introduction"""
    def __init__(self, parent, wizard):
        super().__init__(parent, wizard)
        self._create_widgets()
    
    def _create_widgets(self):
        # Title
        title = ttk.Label(self, text="Welcome to Beep.Python Host Admin", 
                         font=('Helvetica', 16, 'bold'))
        title.pack(pady=(40, 20))
        
        # Description
        desc_text = """
Beep.Python Host Admin is a professional Python Environment 
and LLM Management System.

This wizard will guide you through the installation process:

  • Select installation directory
  • Configure data storage location  
  • Set up server options
  • Create shortcuts and startup options

Click 'Next' to continue with the installation.
        """
        desc = ttk.Label(self, text=desc_text, justify=tk.LEFT)
        desc.pack(pady=20, padx=40)
        
        # System info
        sys_frame = ttk.LabelFrame(self, text="System Information")
        sys_frame.pack(pady=20, padx=40, fill=tk.X)
        
        ttk.Label(sys_frame, text=f"Operating System: {platform.system()} {platform.release()}").pack(anchor=tk.W, padx=10, pady=2)
        ttk.Label(sys_frame, text=f"Architecture: {platform.machine()}").pack(anchor=tk.W, padx=10, pady=2)
        ttk.Label(sys_frame, text=f"Python Version: {platform.python_version()}").pack(anchor=tk.W, padx=10, pady=(2, 10))


class InstallLocationPage(WizardPage):
    """Page for selecting installation directory"""
    def __init__(self, parent, wizard):
        super().__init__(parent, wizard)
        self._create_widgets()
    
    def _create_widgets(self):
        title = ttk.Label(self, text="Installation Location", font=('Helvetica', 14, 'bold'))
        title.pack(pady=(30, 10))
        
        desc = ttk.Label(self, text="Choose where to install Beep.Python Host Admin:")
        desc.pack(pady=(0, 20))
        
        # Install path
        path_frame = ttk.Frame(self)
        path_frame.pack(fill=tk.X, padx=40, pady=10)
        
        ttk.Label(path_frame, text="Installation Directory:").pack(anchor=tk.W)
        
        entry_frame = ttk.Frame(path_frame)
        entry_frame.pack(fill=tk.X, pady=5)
        
        self.install_path_var = tk.StringVar(value=self.config.install_path)
        self.install_entry = ttk.Entry(entry_frame, textvariable=self.install_path_var, width=50)
        self.install_entry.pack(side=tk.LEFT, fill=tk.X, expand=True)
        
        browse_btn = ttk.Button(entry_frame, text="Browse...", command=self._browse_install)
        browse_btn.pack(side=tk.LEFT, padx=(10, 0))
        
        # Space required
        self.space_label = ttk.Label(path_frame, text="Space required: ~150 MB")
        self.space_label.pack(anchor=tk.W, pady=(10, 0))
        
        # Available space
        self.avail_label = ttk.Label(path_frame, text="")
        self.avail_label.pack(anchor=tk.W)
        self._update_space_info()
        
        self.install_path_var.trace_add('write', lambda *args: self._update_space_info())
    
    def _browse_install(self):
        path = filedialog.askdirectory(initialdir=self.install_path_var.get())
        if path:
            self.install_path_var.set(path)
    
    def _update_space_info(self):
        try:
            path = Path(self.install_path_var.get())
            # Find existing parent directory
            check_path = path
            while not check_path.exists() and check_path.parent != check_path:
                check_path = check_path.parent
            
            if check_path.exists():
                if platform.system() == "Windows":
                    import ctypes
                    free_bytes = ctypes.c_ulonglong(0)
                    ctypes.windll.kernel32.GetDiskFreeSpaceExW(
                        ctypes.c_wchar_p(str(check_path)), None, None, ctypes.pointer(free_bytes))
                    free_gb = free_bytes.value / (1024**3)
                else:
                    stat = os.statvfs(check_path)
                    free_gb = (stat.f_bavail * stat.f_frsize) / (1024**3)
                
                self.avail_label.config(text=f"Available space: {free_gb:.1f} GB")
        except Exception:
            self.avail_label.config(text="Available space: Unknown")
    
    def _check_write_permission(self, path_str: str) -> bool:
        """Check if we can write to the given path (or its parent)"""
        path = Path(path_str)
        check_path = path
        while not check_path.exists() and check_path.parent != check_path:
            check_path = check_path.parent
        
        if check_path.exists():
            test_file = check_path / ".beep_write_test"
            try:
                test_file.write_text("test")
                test_file.unlink()
                return True
            except (PermissionError, OSError):
                return False
        return True  # Assume OK if we can't check
    
    def on_leave(self) -> bool:
        self.config.install_path = self.install_path_var.get()
        return True
    
    def validate(self) -> bool:
        path = self.install_path_var.get()
        if not path:
            messagebox.showerror("Error", "Please select an installation directory.")
            return False
        
        # Check write permissions
        if not self._check_write_permission(path):
            result = messagebox.askyesno(
                "Permission Warning",
                f"You may not have permission to write to:\n{path}\n\n"
                f"This typically happens with system folders like 'Program Files'.\n\n"
                f"Recommended: Choose a folder in your user directory, such as:\n"
                f"  • {Path(os.environ.get('LOCALAPPDATA', '')) / 'Programs' / 'BeepPython'}\n\n"
                f"Do you want to continue anyway?\n"
                f"(You may need to run as Administrator)"
            )
            if not result:
                return False
        
        return True


class DataLocationPage(WizardPage):
    """Page for selecting data directory"""
    def __init__(self, parent, wizard):
        super().__init__(parent, wizard)
        self._create_widgets()
    
    def _create_widgets(self):
        title = ttk.Label(self, text="Data Storage Location", font=('Helvetica', 14, 'bold'))
        title.pack(pady=(30, 10))
        
        desc = ttk.Label(self, text="Choose where to store application data (databases, models, logs):")
        desc.pack(pady=(0, 20))
        
        # Data path
        path_frame = ttk.Frame(self)
        path_frame.pack(fill=tk.X, padx=40, pady=10)
        
        ttk.Label(path_frame, text="Data Directory:").pack(anchor=tk.W)
        
        entry_frame = ttk.Frame(path_frame)
        entry_frame.pack(fill=tk.X, pady=5)
        
        self.data_path_var = tk.StringVar(value=self.config.data_path)
        self.data_entry = ttk.Entry(entry_frame, textvariable=self.data_path_var, width=50)
        self.data_entry.pack(side=tk.LEFT, fill=tk.X, expand=True)
        
        browse_btn = ttk.Button(entry_frame, text="Browse...", command=self._browse_data)
        browse_btn.pack(side=tk.LEFT, padx=(10, 0))
        
        # Info about data
        info_frame = ttk.LabelFrame(path_frame, text="Data Storage Info")
        info_frame.pack(fill=tk.X, pady=20)
        
        info_text = """
This directory will contain:
  • SQLite database files
  • Downloaded LLM models (can be several GB each)
  • Configuration files
  • Log files

Tip: Choose a location with plenty of free space if you plan to download large LLM models.
        """
        ttk.Label(info_frame, text=info_text, justify=tk.LEFT).pack(padx=10, pady=10)
    
    def _browse_data(self):
        path = filedialog.askdirectory(initialdir=self.data_path_var.get())
        if path:
            self.data_path_var.set(path)
    
    def on_leave(self) -> bool:
        self.config.data_path = self.data_path_var.get()
        return True
    
    def validate(self) -> bool:
        path = self.data_path_var.get()
        if not path:
            messagebox.showerror("Error", "Please select a data directory.")
            return False
        return True


class ServerConfigPage(WizardPage):
    """Page for server configuration"""
    def __init__(self, parent, wizard):
        super().__init__(parent, wizard)
        self._create_widgets()
    
    def _create_widgets(self):
        title = ttk.Label(self, text="Server Configuration", font=('Helvetica', 14, 'bold'))
        title.pack(pady=(30, 10))
        
        desc = ttk.Label(self, text="Configure the web server settings:")
        desc.pack(pady=(0, 20))
        
        # Server settings frame
        settings_frame = ttk.Frame(self)
        settings_frame.pack(fill=tk.X, padx=40, pady=10)
        
        # Host
        host_frame = ttk.Frame(settings_frame)
        host_frame.pack(fill=tk.X, pady=5)
        ttk.Label(host_frame, text="Server Host:", width=15).pack(side=tk.LEFT)
        self.host_var = tk.StringVar(value=self.config.server_host)
        host_combo = ttk.Combobox(host_frame, textvariable=self.host_var, width=20,
                                  values=["127.0.0.1", "0.0.0.0", "localhost"])
        host_combo.pack(side=tk.LEFT)
        ttk.Label(host_frame, text="  (127.0.0.1 = local only, 0.0.0.0 = network access)").pack(side=tk.LEFT)
        
        # Port
        port_frame = ttk.Frame(settings_frame)
        port_frame.pack(fill=tk.X, pady=5)
        ttk.Label(port_frame, text="Server Port:", width=15).pack(side=tk.LEFT)
        self.port_var = tk.StringVar(value=str(self.config.server_port))
        port_entry = ttk.Entry(port_frame, textvariable=self.port_var, width=10)
        port_entry.pack(side=tk.LEFT)
        ttk.Label(port_frame, text="  (default: 5000)").pack(side=tk.LEFT)
        
        # Options frame
        options_frame = ttk.LabelFrame(settings_frame, text="Options")
        options_frame.pack(fill=tk.X, pady=20)
        
        self.browser_var = tk.BooleanVar(value=self.config.open_browser)
        ttk.Checkbutton(options_frame, text="Open browser automatically when server starts",
                       variable=self.browser_var).pack(anchor=tk.W, padx=10, pady=5)
        
        # Access URL preview
        self.url_label = ttk.Label(settings_frame, text="", font=('Helvetica', 10))
        self.url_label.pack(pady=10)
        self._update_url()
        
        self.host_var.trace_add('write', lambda *args: self._update_url())
        self.port_var.trace_add('write', lambda *args: self._update_url())
    
    def _update_url(self):
        host = self.host_var.get()
        port = self.port_var.get()
        display_host = "localhost" if host in ("127.0.0.1", "0.0.0.0") else host
        self.url_label.config(text=f"Access URL: http://{display_host}:{port}")
    
    def on_leave(self) -> bool:
        self.config.server_host = self.host_var.get()
        try:
            self.config.server_port = int(self.port_var.get())
        except ValueError:
            self.config.server_port = 5000
        self.config.open_browser = self.browser_var.get()
        return True
    
    def validate(self) -> bool:
        try:
            port = int(self.port_var.get())
            if port < 1 or port > 65535:
                raise ValueError()
        except ValueError:
            messagebox.showerror("Error", "Please enter a valid port number (1-65535).")
            return False
        return True


class ShortcutsPage(WizardPage):
    """Page for shortcuts and startup options"""
    def __init__(self, parent, wizard):
        super().__init__(parent, wizard)
        self._create_widgets()
    
    def _create_widgets(self):
        title = ttk.Label(self, text="Shortcuts & Startup", font=('Helvetica', 14, 'bold'))
        title.pack(pady=(30, 10))
        
        desc = ttk.Label(self, text="Configure shortcuts and startup options:")
        desc.pack(pady=(0, 20))
        
        options_frame = ttk.Frame(self)
        options_frame.pack(fill=tk.X, padx=40, pady=10)
        
        # Shortcuts
        self.shortcut_var = tk.BooleanVar(value=self.config.create_shortcut)
        shortcut_text = self._get_shortcut_text()
        ttk.Checkbutton(options_frame, text=shortcut_text,
                       variable=self.shortcut_var).pack(anchor=tk.W, pady=5)
        
        # Auto-start
        self.autostart_var = tk.BooleanVar(value=self.config.auto_start)
        ttk.Checkbutton(options_frame, text="Start Beep.Python automatically when you log in",
                       variable=self.autostart_var).pack(anchor=tk.W, pady=5)
        
        # Platform-specific notes
        notes_frame = ttk.LabelFrame(options_frame, text="Platform Notes")
        notes_frame.pack(fill=tk.X, pady=20)
        
        notes = self._get_platform_notes()
        ttk.Label(notes_frame, text=notes, justify=tk.LEFT).pack(padx=10, pady=10)
    
    def _get_shortcut_text(self) -> str:
        system = platform.system()
        if system == "Windows":
            return "Create Desktop shortcut and Start Menu entry"
        elif system == "Darwin":
            return "Add to Applications folder and Dock"
        else:
            return "Create desktop entry (.desktop file)"
    
    def _get_platform_notes(self) -> str:
        system = platform.system()
        if system == "Windows":
            return "Shortcuts will be created in the Start Menu and optionally on the Desktop."
        elif system == "Darwin":
            return "The application will be available in Launchpad and can be added to the Dock."
        else:
            return "A .desktop file will be created for your desktop environment."
    
    def on_leave(self) -> bool:
        self.config.create_shortcut = self.shortcut_var.get()
        self.config.auto_start = self.autostart_var.get()
        return True


class SummaryPage(WizardPage):
    """Summary page before installation"""
    def __init__(self, parent, wizard):
        super().__init__(parent, wizard)
        self._create_widgets()
    
    def _create_widgets(self):
        title = ttk.Label(self, text="Ready to Install", font=('Helvetica', 14, 'bold'))
        title.pack(pady=(30, 10))
        
        desc = ttk.Label(self, text="Review your settings and click 'Install' to begin:")
        desc.pack(pady=(0, 20))
        
        # Summary frame
        self.summary_frame = ttk.Frame(self)
        self.summary_frame.pack(fill=tk.BOTH, expand=True, padx=40, pady=10)
        
        # Will be populated in on_enter
        self.summary_text = tk.Text(self.summary_frame, height=15, width=60, state=tk.DISABLED)
        self.summary_text.pack(fill=tk.BOTH, expand=True)
    
    def on_enter(self):
        summary = f"""
Installation Summary
====================

Installation Directory:
  {self.config.install_path}

Data Directory:
  {self.config.data_path}

Server Configuration:
  Host: {self.config.server_host}
  Port: {self.config.server_port}

Options:
  Create shortcuts: {'Yes' if self.config.create_shortcut else 'No'}
  Auto-start: {'Yes' if self.config.auto_start else 'No'}
  Open browser on start: {'Yes' if self.config.open_browser else 'No'}

Platform: {platform.system()} {platform.release()}

Click 'Install' to begin the installation.
        """
        self.summary_text.config(state=tk.NORMAL)
        self.summary_text.delete(1.0, tk.END)
        self.summary_text.insert(tk.END, summary)
        self.summary_text.config(state=tk.DISABLED)


class InstallProgressPage(WizardPage):
    """Installation progress page"""
    def __init__(self, parent, wizard):
        super().__init__(parent, wizard)
        self._create_widgets()
        self.install_complete = False
        self.install_error = None
    
    def _create_widgets(self):
        title = ttk.Label(self, text="Installing...", font=('Helvetica', 14, 'bold'))
        title.pack(pady=(30, 10))
        
        self.status_label = ttk.Label(self, text="Preparing installation...")
        self.status_label.pack(pady=10)
        
        self.progress = ttk.Progressbar(self, length=400, mode='determinate')
        self.progress.pack(pady=20, padx=40)
        
        # Log frame
        log_frame = ttk.LabelFrame(self, text="Installation Log")
        log_frame.pack(fill=tk.BOTH, expand=True, padx=40, pady=10)
        
        self.log_text = tk.Text(log_frame, height=10, width=60, state=tk.DISABLED)
        scrollbar = ttk.Scrollbar(log_frame, orient=tk.VERTICAL, command=self.log_text.yview)
        self.log_text.configure(yscrollcommand=scrollbar.set)
        
        self.log_text.pack(side=tk.LEFT, fill=tk.BOTH, expand=True)
        scrollbar.pack(side=tk.RIGHT, fill=tk.Y)
    
    def log(self, message: str):
        self.log_text.config(state=tk.NORMAL)
        self.log_text.insert(tk.END, message + "\n")
        self.log_text.see(tk.END)
        self.log_text.config(state=tk.DISABLED)
        self.update_idletasks()
    
    def on_enter(self):
        self.wizard.next_btn.config(state=tk.DISABLED)
        self.wizard.back_btn.config(state=tk.DISABLED)
        self.after(100, self._run_installation)
    
    def _run_installation(self):
        thread = threading.Thread(target=self._install, daemon=True)
        thread.start()
        self._check_install_complete()
    
    def _check_install_complete(self):
        if self.install_complete:
            if self.install_error:
                messagebox.showerror("Installation Failed", f"An error occurred:\n{self.install_error}")
                self.wizard.back_btn.config(state=tk.NORMAL)
            else:
                self.status_label.config(text="Installation complete!")
                self.wizard.next_btn.config(state=tk.NORMAL, text="Finish")
        else:
            self.after(100, self._check_install_complete)
    
    def _install(self):
        try:
            installer = Installer(self.config, self.log, self._set_progress)
            installer.install()
            self.install_complete = True
        except Exception as e:
            self.install_error = str(e)
            self.install_complete = True
    
    def _set_progress(self, value: int):
        self.progress['value'] = value
        self.update_idletasks()


class Installer:
    """Performs the actual installation"""
    def __init__(self, config: InstallerConfig, log_func, progress_func):
        self.config = config
        self.log = log_func
        self.set_progress = progress_func
        self.source_dir = self._get_source_dir()
    
    def _get_source_dir(self) -> Path:
        """Get the source directory (where bundled files are)"""
        if getattr(sys, 'frozen', False):
            return Path(sys._MEIPASS)
        else:
            # Development mode - use dist folder
            return Path(__file__).parent.parent / "dist" / "BeepPythonHost"
    
    def install(self):
        # First check if we have write permissions
        self._check_permissions()
        
        steps = [
            (10, "Creating directories...", self._create_directories),
            (30, "Copying application files...", self._copy_files),
            (60, "Writing configuration...", self._write_config),
            (75, "Creating shortcuts...", self._create_shortcuts),
            (90, "Setting up auto-start...", self._setup_autostart),
            (100, "Finalizing installation...", self._finalize),
        ]
        
        for progress, message, func in steps:
            self.log(message)
            self.set_progress(progress)
            func()
        
        self.log("Installation completed successfully!")
    
    def _check_permissions(self):
        """Check if we have write permissions to the target directories"""
        install_path = Path(self.config.install_path)
        data_path = Path(self.config.data_path)
        
        for path, name in [(install_path, "installation"), (data_path, "data")]:
            # Find the first existing parent directory
            check_path = path
            while not check_path.exists() and check_path.parent != check_path:
                check_path = check_path.parent
            
            # Check if we can write to this directory
            if check_path.exists():
                test_file = check_path / ".beep_write_test"
                try:
                    test_file.write_text("test")
                    test_file.unlink()
                except PermissionError:
                    raise PermissionError(
                        f"Cannot write to {name} directory: '{path}'\n\n"
                        f"Please choose a different location or run the installer with administrator privileges."
                    )
                except Exception as e:
                    self.log(f"  Warning: Could not verify write permissions for {path}: {e}")
    
    def _create_directories(self):
        try:
            Path(self.config.install_path).mkdir(parents=True, exist_ok=True)
        except PermissionError:
            raise PermissionError(
                f"Access denied: Cannot create installation directory '{self.config.install_path}'\n\n"
                f"Try selecting a different folder (e.g., in your user directory) or run as administrator."
            )
        
        try:
            Path(self.config.data_path).mkdir(parents=True, exist_ok=True)
            (Path(self.config.data_path) / "logs").mkdir(exist_ok=True)
            (Path(self.config.data_path) / "models").mkdir(exist_ok=True)
            (Path(self.config.data_path) / "config").mkdir(exist_ok=True)
        except PermissionError:
            raise PermissionError(
                f"Access denied: Cannot create data directory '{self.config.data_path}'\n\n"
                f"Try selecting a different folder (e.g., in your user directory)."
            )
    
    def _copy_files(self):
        if not self.source_dir.exists():
            self.log(f"Warning: Source directory not found at {self.source_dir}")
            self.log("Skipping file copy (development mode)")
            return
        
        dest = Path(self.config.install_path)
        
        # Copy all files from source
        for item in self.source_dir.iterdir():
            src_path = self.source_dir / item.name
            dst_path = dest / item.name
            
            if src_path.is_dir():
                if dst_path.exists():
                    shutil.rmtree(dst_path)
                shutil.copytree(src_path, dst_path)
            else:
                shutil.copy2(src_path, dst_path)
            
            self.log(f"  Copied: {item.name}")
    
    def _write_config(self):
        config_path = Path(self.config.data_path) / "config" / "install_config.json"
        config_data = self.config.to_dict()
        
        with open(config_path, 'w') as f:
            json.dump(config_data, f, indent=2)
        
        self.log(f"  Configuration saved to: {config_path}")
        
        # Set environment variable
        self._set_env_variable("BEEP_PYTHON_HOME", self.config.data_path)
    
    def _set_env_variable(self, name: str, value: str):
        system = platform.system()
        
        if system == "Windows":
            try:
                import winreg
                key = winreg.OpenKey(winreg.HKEY_CURRENT_USER, "Environment", 0, winreg.KEY_SET_VALUE)
                winreg.SetValueEx(key, name, 0, winreg.REG_SZ, value)
                winreg.CloseKey(key)
                self.log(f"  Set environment variable: {name}")
            except Exception as e:
                self.log(f"  Warning: Could not set environment variable: {e}")
        else:
            # For Unix, add to shell profile
            shell_profile = self._get_shell_profile()
            if shell_profile and shell_profile.exists():
                line = f'\nexport {name}="{value}"\n'
                with open(shell_profile, 'a') as f:
                    f.write(line)
                self.log(f"  Added {name} to {shell_profile}")
    
    def _get_shell_profile(self) -> Path:
        home = Path.home()
        for profile in [".zshrc", ".bashrc", ".profile"]:
            path = home / profile
            if path.exists():
                return path
        return home / ".profile"
    
    def _create_shortcuts(self):
        if not self.config.create_shortcut:
            self.log("  Skipping shortcut creation")
            return
        
        system = platform.system()
        
        if system == "Windows":
            self._create_windows_shortcuts()
        elif system == "Darwin":
            self._create_macos_shortcuts()
        else:
            self._create_linux_shortcuts()
    
    def _create_windows_shortcuts(self):
        try:
            # Get paths
            exe_path = Path(self.config.install_path) / "BeepPythonHost.exe"
            
            if not exe_path.exists():
                self.log(f"  Warning: Executable not found at {exe_path}, skipping shortcuts")
                return
            
            # Create Start Menu folder
            start_menu = Path(os.environ.get('APPDATA', '')) / "Microsoft" / "Windows" / "Start Menu" / "Programs" / "BeepPython"
            start_menu.mkdir(parents=True, exist_ok=True)
            
            # Desktop path
            desktop = Path(os.environ.get('USERPROFILE', '')) / "Desktop"
            
            shortcuts_created = []
            
            # Create shortcuts using PowerShell
            for name, location in [("Beep.Python Host Admin", start_menu), ("Beep.Python Host Admin", desktop)]:
                shortcut_path = location / f"{name}.lnk"
                
                # PowerShell command to create shortcut
                ps_command = f'''
$WshShell = New-Object -ComObject WScript.Shell
$Shortcut = $WshShell.CreateShortcut("{shortcut_path}")
$Shortcut.TargetPath = "{exe_path}"
$Shortcut.WorkingDirectory = "{self.config.install_path}"
$Shortcut.Description = "Beep.Python Host Admin - Python Environment & LLM Management"
$Shortcut.Save()
'''
                result = subprocess.run(
                    ["powershell", "-ExecutionPolicy", "Bypass", "-Command", ps_command], 
                    capture_output=True, 
                    text=True
                )
                
                if result.returncode == 0 and shortcut_path.exists():
                    self.log(f"  Created shortcut: {shortcut_path}")
                    shortcuts_created.append(str(shortcut_path))
                else:
                    self.log(f"  Warning: Failed to create shortcut at {shortcut_path}")
                    if result.stderr:
                        self.log(f"    Error: {result.stderr.strip()}")
            
            if not shortcuts_created:
                self.log("  Warning: No shortcuts were created. You may need to create them manually.")
                
        except Exception as e:
            self.log(f"  Warning: Could not create shortcuts: {e}")
    
    def _create_macos_shortcuts(self):
        # Create .app bundle structure
        app_path = Path("/Applications/BeepPython.app")
        contents = app_path / "Contents"
        macos = contents / "MacOS"
        resources = contents / "Resources"
        
        try:
            macos.mkdir(parents=True, exist_ok=True)
            resources.mkdir(parents=True, exist_ok=True)
            
            # Create Info.plist
            plist_content = '''<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleExecutable</key>
    <string>BeepPython</string>
    <key>CFBundleIdentifier</key>
    <string>com.thetechidea.beeppython</string>
    <key>CFBundleName</key>
    <string>BeepPython</string>
    <key>CFBundleVersion</key>
    <string>1.0.0</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
</dict>
</plist>'''
            (contents / "Info.plist").write_text(plist_content)
            
            # Create launcher script
            launcher = f'''#!/bin/bash
cd "{self.config.install_path}"
./BeepPythonHost
'''
            launcher_path = macos / "BeepPython"
            launcher_path.write_text(launcher)
            launcher_path.chmod(0o755)
            
            self.log(f"  Created macOS app bundle: {app_path}")
        except Exception as e:
            self.log(f"  Warning: Could not create app bundle: {e}")
    
    def _create_linux_shortcuts(self):
        # Create .desktop file
        desktop_entry = f'''[Desktop Entry]
Type=Application
Name=BeepPython Host Admin
Comment=Python Environment and LLM Management System
Exec={self.config.install_path}/BeepPythonHost
Icon={self.config.install_path}/icon.png
Terminal=false
Categories=Development;
'''
        try:
            # User applications directory
            apps_dir = Path.home() / ".local" / "share" / "applications"
            apps_dir.mkdir(parents=True, exist_ok=True)
            
            desktop_file = apps_dir / "beeppython.desktop"
            desktop_file.write_text(desktop_entry)
            desktop_file.chmod(0o755)
            
            self.log(f"  Created desktop entry: {desktop_file}")
            
            # Also create on desktop if it exists
            desktop_dir = Path.home() / "Desktop"
            if desktop_dir.exists():
                desktop_shortcut = desktop_dir / "BeepPython.desktop"
                desktop_shortcut.write_text(desktop_entry)
                desktop_shortcut.chmod(0o755)
                self.log(f"  Created desktop shortcut: {desktop_shortcut}")
                
        except Exception as e:
            self.log(f"  Warning: Could not create desktop entry: {e}")
    
    def _setup_autostart(self):
        if not self.config.auto_start:
            self.log("  Skipping auto-start setup")
            return
        
        system = platform.system()
        
        try:
            if system == "Windows":
                # Add to registry Run key
                import winreg
                key = winreg.OpenKey(winreg.HKEY_CURRENT_USER, 
                                    r"Software\Microsoft\Windows\CurrentVersion\Run",
                                    0, winreg.KEY_SET_VALUE)
                exe_path = Path(self.config.install_path) / "BeepPythonHost.exe"
                winreg.SetValueEx(key, "BeepPython", 0, winreg.REG_SZ, f'"{exe_path}" --minimized')
                winreg.CloseKey(key)
                self.log("  Added to Windows startup")
                
            elif system == "Darwin":
                # Create LaunchAgent
                launch_agents = Path.home() / "Library" / "LaunchAgents"
                launch_agents.mkdir(parents=True, exist_ok=True)
                
                plist = f'''<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>Label</key>
    <string>com.thetechidea.beeppython</string>
    <key>ProgramArguments</key>
    <array>
        <string>{self.config.install_path}/BeepPythonHost</string>
    </array>
    <key>RunAtLoad</key>
    <true/>
</dict>
</plist>'''
                (launch_agents / "com.thetechidea.beeppython.plist").write_text(plist)
                self.log("  Created LaunchAgent for auto-start")
                
            else:
                # Linux: Create autostart entry
                autostart_dir = Path.home() / ".config" / "autostart"
                autostart_dir.mkdir(parents=True, exist_ok=True)
                
                desktop_entry = f'''[Desktop Entry]
Type=Application
Name=BeepPython Host Admin
Exec={self.config.install_path}/BeepPythonHost --minimized
Hidden=false
NoDisplay=false
X-GNOME-Autostart-enabled=true
'''
                (autostart_dir / "beeppython.desktop").write_text(desktop_entry)
                self.log("  Created autostart entry")
                
        except Exception as e:
            self.log(f"  Warning: Could not set up auto-start: {e}")
    
    def _finalize(self):
        self.log("  Installation finalized")
        
        # Return the config for post-install actions
        return self.config


class CompletionPage(WizardPage):
    """Completion page with launch options"""
    def __init__(self, parent, wizard):
        super().__init__(parent, wizard)
        self._create_widgets()
    
    def _create_widgets(self):
        title = ttk.Label(self, text="Installation Complete!", font=('Helvetica', 16, 'bold'))
        title.pack(pady=(40, 20))
        
        # Success message
        success_frame = ttk.Frame(self)
        success_frame.pack(pady=20, padx=40, fill=tk.X)
        
        ttk.Label(success_frame, text="✓ Beep.Python Host Admin has been successfully installed!", 
                  font=('Helvetica', 11)).pack(anchor=tk.W)
        
        # Install info
        info_frame = ttk.LabelFrame(self, text="Installation Details")
        info_frame.pack(pady=20, padx=40, fill=tk.X)
        
        self.install_path_label = ttk.Label(info_frame, text="")
        self.install_path_label.pack(anchor=tk.W, padx=10, pady=2)
        
        self.data_path_label = ttk.Label(info_frame, text="")
        self.data_path_label.pack(anchor=tk.W, padx=10, pady=2)
        
        self.url_label = ttk.Label(info_frame, text="")
        self.url_label.pack(anchor=tk.W, padx=10, pady=(2, 10))
        
        # Launch options
        options_frame = ttk.LabelFrame(self, text="Launch Options")
        options_frame.pack(pady=20, padx=40, fill=tk.X)
        
        self.launch_var = tk.BooleanVar(value=True)
        ttk.Checkbutton(options_frame, text="Launch Beep.Python Host Admin now",
                       variable=self.launch_var).pack(anchor=tk.W, padx=10, pady=5)
        
        self.browser_var = tk.BooleanVar(value=True)
        ttk.Checkbutton(options_frame, text="Open web browser to http://localhost:5000",
                       variable=self.browser_var).pack(anchor=tk.W, padx=10, pady=(0, 10))
        
        # Note
        note = ttk.Label(self, text="Click 'Finish' to complete the setup.", 
                        font=('Helvetica', 10, 'italic'))
        note.pack(pady=20)
    
    def on_enter(self):
        self.install_path_label.config(text=f"Installation folder: {self.config.install_path}")
        self.data_path_label.config(text=f"Data folder: {self.config.data_path}")
        self.url_label.config(text=f"Access URL: http://{self.config.server_host}:{self.config.server_port}")
        self.wizard.next_btn.config(text="Finish")
        self.wizard.back_btn.config(state=tk.DISABLED)
    
    def on_leave(self) -> bool:
        # Launch the application if selected
        if self.launch_var.get():
            self._launch_application()
        return True
    
    def _launch_application(self):
        exe_path = Path(self.config.install_path) / ("BeepPythonHost.exe" if platform.system() == "Windows" else "BeepPythonHost")
        
        if not exe_path.exists():
            messagebox.showwarning("Warning", f"Application not found at:\n{exe_path}\n\nPlease launch manually.")
            return
        
        try:
            if platform.system() == "Windows":
                # Use subprocess.Popen to launch without waiting
                subprocess.Popen([str(exe_path)], 
                               cwd=str(self.config.install_path),
                               creationflags=subprocess.CREATE_NEW_CONSOLE)
            else:
                subprocess.Popen([str(exe_path)], 
                               cwd=str(self.config.install_path),
                               start_new_session=True)
            
            # Open browser if requested
            if self.browser_var.get():
                import webbrowser
                url = f"http://{self.config.server_host}:{self.config.server_port}"
                # Wait a moment for server to start
                self.after(2000, lambda: webbrowser.open(url))
                
        except Exception as e:
            messagebox.showwarning("Warning", f"Could not launch application:\n{e}\n\nPlease launch manually.")


class InstallerWizard(tk.Tk):
    """Main wizard window"""
    def __init__(self):
        super().__init__()
        
        self.title("Beep.Python Host Admin - Setup")
        self.geometry("650x550")
        self.resizable(False, False)
        
        # Center window
        self.update_idletasks()
        x = (self.winfo_screenwidth() - 650) // 2
        y = (self.winfo_screenheight() - 550) // 2
        self.geometry(f"+{x}+{y}")
        
        self.config = InstallerConfig()
        self.current_page = 0
        
        self._create_widgets()
        self._create_pages()
        self._show_page(0)
    
    def _create_widgets(self):
        # Main container
        self.main_frame = ttk.Frame(self)
        self.main_frame.pack(fill=tk.BOTH, expand=True)
        
        # Page container
        self.page_container = ttk.Frame(self.main_frame)
        self.page_container.pack(fill=tk.BOTH, expand=True)
        
        # Button bar
        btn_frame = ttk.Frame(self.main_frame)
        btn_frame.pack(fill=tk.X, side=tk.BOTTOM, pady=10, padx=10)
        
        ttk.Separator(self.main_frame, orient=tk.HORIZONTAL).pack(fill=tk.X, side=tk.BOTTOM)
        
        self.cancel_btn = ttk.Button(btn_frame, text="Cancel", command=self._on_cancel)
        self.cancel_btn.pack(side=tk.LEFT)
        
        self.next_btn = ttk.Button(btn_frame, text="Next >", command=self._on_next)
        self.next_btn.pack(side=tk.RIGHT)
        
        self.back_btn = ttk.Button(btn_frame, text="< Back", command=self._on_back)
        self.back_btn.pack(side=tk.RIGHT, padx=(0, 10))
    
    def _create_pages(self):
        self.pages = [
            WelcomePage(self.page_container, self),
            InstallLocationPage(self.page_container, self),
            DataLocationPage(self.page_container, self),
            ServerConfigPage(self.page_container, self),
            ShortcutsPage(self.page_container, self),
            SummaryPage(self.page_container, self),
            InstallProgressPage(self.page_container, self),
            CompletionPage(self.page_container, self),
        ]
    
    def _show_page(self, index: int):
        # Hide all pages
        for page in self.pages:
            page.pack_forget()
        
        # Show current page
        self.pages[index].pack(fill=tk.BOTH, expand=True)
        self.pages[index].on_enter()
        
        # Update buttons
        self.back_btn.config(state=tk.NORMAL if index > 0 else tk.DISABLED)
        
        # Get page type
        page = self.pages[index]
        
        if isinstance(page, SummaryPage):
            self.next_btn.config(text="Install")
        elif isinstance(page, InstallProgressPage):
            # Buttons controlled by the progress page
            pass
        elif isinstance(page, CompletionPage):
            self.next_btn.config(text="Finish")
            self.back_btn.config(state=tk.DISABLED)
        else:
            self.next_btn.config(text="Next >")
    
    def _on_back(self):
        if self.current_page > 0:
            self.pages[self.current_page].on_leave()
            self.current_page -= 1
            self._show_page(self.current_page)
    
    def _on_next(self):
        page = self.pages[self.current_page]
        
        if not page.validate():
            return
        
        if not page.on_leave():
            return
        
        if self.current_page < len(self.pages) - 1:
            self.current_page += 1
            self._show_page(self.current_page)
        else:
            # Finish
            self.destroy()
    
    def _on_cancel(self):
        if messagebox.askyesno("Cancel Installation", "Are you sure you want to cancel the installation?"):
            self.destroy()


def run_cli_installer():
    """Fallback CLI installer when GUI is not available"""
    print("=" * 60)
    print("Beep.Python Host Admin - Setup")
    print("=" * 60)
    print()
    print("GUI not available. Running command-line installer.")
    print()
    
    config = InstallerConfig()
    
    # Get installation path
    print(f"Installation Directory [{config.install_path}]: ", end="")
    user_input = input().strip()
    if user_input:
        config.install_path = user_input
    
    # Get data path
    print(f"Data Directory [{config.data_path}]: ", end="")
    user_input = input().strip()
    if user_input:
        config.data_path = user_input
    
    # Get port
    print(f"Server Port [{config.server_port}]: ", end="")
    user_input = input().strip()
    if user_input:
        try:
            config.server_port = int(user_input)
        except ValueError:
            pass
    
    # Confirm
    print()
    print("Installation Summary:")
    print(f"  Install Path: {config.install_path}")
    print(f"  Data Path: {config.data_path}")
    print(f"  Server Port: {config.server_port}")
    print()
    print("Proceed with installation? [Y/n]: ", end="")
    
    if input().strip().lower() not in ('', 'y', 'yes'):
        print("Installation cancelled.")
        return
    
    print()
    print("Installing...")
    
    def log(msg):
        print(msg)
    
    def progress(val):
        pass
    
    try:
        installer = Installer(config, log, progress)
        installer.install()
        print()
        print("Installation completed successfully!")
    except Exception as e:
        print(f"Installation failed: {e}")


def main():
    if HAS_TK:
        try:
            app = InstallerWizard()
            app.mainloop()
        except Exception as e:
            print(f"GUI error: {e}")
            run_cli_installer()
    else:
        run_cli_installer()


if __name__ == "__main__":
    main()
