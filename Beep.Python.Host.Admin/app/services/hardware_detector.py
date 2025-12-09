"""
Cross-Platform Hardware Detection Service

Detects system hardware (GPU, RAM, CPU, Disk) for LLM model recommendations.
Works on Windows, Linux, and macOS.

Usage:
    from app.services.hardware_detector import get_hardware_info, HardwareInfo
    
    # Get cached or fresh hardware info
    hw = get_hardware_info()
    print(f"GPU: {hw.gpu_name}, VRAM: {hw.vram_gb}GB")
"""

import os
import json
import platform
import subprocess
from pathlib import Path
from dataclasses import dataclass, asdict, field
from typing import Optional, Dict, Any
from datetime import datetime

try:
    import psutil
except ImportError:
    psutil = None


@dataclass
class HardwareInfo:
    """Hardware information dataclass"""
    # GPU
    gpu_type: str = 'none'  # nvidia, amd, apple_silicon, intel, none
    gpu_name: Optional[str] = None
    vram_gb: float = 0.0
    
    # Memory
    ram_gb: float = 8.0
    
    # Storage
    disk_free_gb: float = 10.0
    
    # CPU
    cpu_name: str = 'Unknown'
    cpu_cores: int = 4
    
    # Platform
    platform: str = 'Unknown'
    arch: str = 'x64'
    
    # Metadata
    detected_at: str = field(default_factory=lambda: datetime.now().isoformat())
    
    def to_dict(self) -> Dict[str, Any]:
        return asdict(self)
    
    @classmethod
    def from_dict(cls, data: Dict[str, Any]) -> 'HardwareInfo':
        # Filter to only valid fields
        valid_fields = {f.name for f in cls.__dataclass_fields__.values()}
        filtered = {k: v for k, v in data.items() if k in valid_fields}
        return cls(**filtered)
    
    @property
    def can_run_gpu_models(self) -> bool:
        """Check if system can run GPU-accelerated models"""
        return self.gpu_type != 'none' and self.vram_gb >= 4.0
    
    @property
    def recommended_model_size(self) -> str:
        """Get recommended model size based on hardware"""
        if self.vram_gb >= 16:
            return 'large'  # 13B+ models
        elif self.vram_gb >= 8:
            return 'medium'  # 7B models
        elif self.vram_gb >= 4:
            return 'small'  # 3B models
        elif self.ram_gb >= 16:
            return 'small'  # CPU with good RAM
        else:
            return 'tiny'  # 1B models or smaller


class HardwareDetector:
    """
    Cross-platform hardware detection.
    Caches results to disk for fast subsequent access.
    """
    
    _instance: Optional['HardwareDetector'] = None
    _hardware_cache: Optional[HardwareInfo] = None
    
    def __new__(cls, base_path: Optional[str] = None):
        """Singleton pattern for consistent caching"""
        if cls._instance is None:
            cls._instance = super().__new__(cls)
            cls._instance._initialized = False
        return cls._instance
    
    def __init__(self, base_path: Optional[str] = None):
        if self._initialized:
            return
        
        self._initialized = True
        self.system = platform.system()  # Windows, Linux, Darwin
        self.machine = platform.machine().lower()  # x86_64, arm64, etc.
        
        # Determine base path for cache
        if base_path:
            self.base_path = Path(base_path)
        else:
            self.base_path = Path(os.environ.get('BEEP_PYTHON_HOME', 
                                                  os.path.expanduser('~/.beep-llm')))
        
        self.cache_file = self.base_path / 'config' / 'hardware_info.json'
        
        # Load cached hardware on init
        self._load_cache()
    
    def _load_cache(self) -> None:
        """Load hardware info from disk cache"""
        if self.cache_file.exists():
            try:
                with open(self.cache_file, 'r') as f:
                    data = json.load(f)
                HardwareDetector._hardware_cache = HardwareInfo.from_dict(data)
            except Exception as e:
                print(f"Failed to load hardware cache: {e}")
    
    def _save_cache(self, hw: HardwareInfo) -> None:
        """Save hardware info to disk cache"""
        try:
            self.cache_file.parent.mkdir(parents=True, exist_ok=True)
            with open(self.cache_file, 'w') as f:
                json.dump(hw.to_dict(), f, indent=2)
        except Exception as e:
            print(f"Failed to save hardware cache: {e}")
    
    def get_hardware(self, force_refresh: bool = False) -> HardwareInfo:
        """
        Get hardware information.
        Uses cache if available, otherwise detects fresh.
        
        Args:
            force_refresh: Force re-detection even if cache exists
            
        Returns:
            HardwareInfo with system hardware details
        """
        if not force_refresh and HardwareDetector._hardware_cache:
            return HardwareDetector._hardware_cache
        
        hw = self._detect_all()
        HardwareDetector._hardware_cache = hw
        self._save_cache(hw)
        return hw
    
    def _detect_all(self) -> HardwareInfo:
        """Detect all hardware components"""
        hw = HardwareInfo(
            platform=self.system,
            arch=self._get_arch()
        )
        
        # Detect RAM (fast, no subprocess)
        hw.ram_gb = self._detect_ram()
        
        # Detect disk space (fast, no subprocess)
        hw.disk_free_gb = self._detect_disk()
        
        # Detect CPU (fast, no subprocess)
        hw.cpu_name, hw.cpu_cores = self._detect_cpu()
        
        # Detect GPU (may use subprocess, but with short timeout)
        hw.gpu_type, hw.gpu_name, hw.vram_gb = self._detect_gpu()
        
        hw.detected_at = datetime.now().isoformat()
        return hw
    
    def _get_arch(self) -> str:
        """Get system architecture"""
        if self.machine in ('x86_64', 'amd64'):
            return 'x64'
        elif self.machine in ('aarch64', 'arm64'):
            return 'arm64'
        return self.machine
    
    def _detect_ram(self) -> float:
        """Detect total RAM in GB"""
        if psutil:
            return round(psutil.virtual_memory().total / (1024 ** 3), 1)
        
        # Fallback for systems without psutil
        try:
            if self.system == 'Linux':
                with open('/proc/meminfo', 'r') as f:
                    for line in f:
                        if line.startswith('MemTotal:'):
                            kb = int(line.split()[1])
                            return round(kb / (1024 ** 2), 1)
            elif self.system == 'Darwin':
                result = subprocess.run(['sysctl', '-n', 'hw.memsize'],
                                        capture_output=True, text=True, timeout=2)
                if result.returncode == 0:
                    return round(int(result.stdout.strip()) / (1024 ** 3), 1)
        except:
            pass
        return 8.0  # Default
    
    def _detect_disk(self) -> float:
        """Detect free disk space in GB"""
        if psutil:
            try:
                if self.system == 'Windows':
                    return round(psutil.disk_usage('C:').free / (1024 ** 3), 1)
                else:
                    return round(psutil.disk_usage('/').free / (1024 ** 3), 1)
            except:
                pass
        
        # Fallback
        try:
            if self.system != 'Windows':
                stat = os.statvfs('/')
                return round((stat.f_bavail * stat.f_frsize) / (1024 ** 3), 1)
        except:
            pass
        return 10.0  # Default
    
    def _detect_cpu(self) -> tuple:
        """Detect CPU name and core count"""
        cores = 4
        name = 'Unknown'
        
        if psutil:
            cores = psutil.cpu_count(logical=False) or psutil.cpu_count() or 4
        
        # Try to get CPU name
        try:
            if self.system == 'Windows':
                # Use platform module (no subprocess)
                name = platform.processor() or 'Unknown'
            elif self.system == 'Linux':
                with open('/proc/cpuinfo', 'r') as f:
                    for line in f:
                        if line.startswith('model name'):
                            name = line.split(':')[1].strip()
                            break
            elif self.system == 'Darwin':
                result = subprocess.run(['sysctl', '-n', 'machdep.cpu.brand_string'],
                                        capture_output=True, text=True, timeout=2)
                if result.returncode == 0:
                    name = result.stdout.strip()
        except:
            pass
        
        return name, cores
    
    def _detect_gpu(self) -> tuple:
        """
        Detect GPU type, name, and VRAM.
        Returns: (gpu_type, gpu_name, vram_gb)
        """
        # Check for Apple Silicon first (no subprocess needed)
        if self.system == 'Darwin' and self.machine == 'arm64':
            # Apple Silicon uses unified memory
            ram = self._detect_ram()
            return 'apple_silicon', 'Apple Silicon', round(ram * 0.6, 1)
        
        # Try NVIDIA (most common for ML)
        nvidia_result = self._detect_nvidia()
        if nvidia_result[0]:
            return nvidia_result
        
        # Try AMD on Linux
        if self.system == 'Linux':
            amd_result = self._detect_amd_linux()
            if amd_result[0]:
                return amd_result
        
        # Try AMD on Windows (via DirectX/WMI would be complex, skip for now)
        
        return 'none', None, 0.0
    
    def _detect_nvidia(self) -> tuple:
        """Detect NVIDIA GPU using nvidia-smi"""
        try:
            # Use CREATE_NO_WINDOW on Windows to prevent console popup
            kwargs = {'capture_output': True, 'text': True, 'timeout': 2}
            if self.system == 'Windows':
                kwargs['creationflags'] = subprocess.CREATE_NO_WINDOW
            
            result = subprocess.run(
                ['nvidia-smi', '--query-gpu=name,memory.total', '--format=csv,noheader,nounits'],
                **kwargs
            )
            
            if result.returncode == 0 and result.stdout.strip():
                lines = result.stdout.strip().split('\n')
                parts = lines[0].split(',')
                gpu_name = parts[0].strip()
                vram_gb = 0.0
                if len(parts) >= 2:
                    try:
                        vram_gb = round(float(parts[1].strip()) / 1024, 1)
                    except ValueError:
                        pass
                return 'nvidia', gpu_name, vram_gb
        except (FileNotFoundError, subprocess.TimeoutExpired, Exception):
            pass
        
        return None, None, 0.0
    
    def _detect_amd_linux(self) -> tuple:
        """Detect AMD GPU on Linux using lspci"""
        try:
            result = subprocess.run(['lspci'], capture_output=True, text=True, timeout=2)
            if result.returncode == 0:
                for line in result.stdout.split('\n'):
                    if 'VGA' in line or 'Display' in line:
                        if 'AMD' in line or 'Radeon' in line:
                            # Extract GPU name from lspci output
                            parts = line.split(':')
                            if len(parts) >= 3:
                                gpu_name = parts[2].strip()
                            else:
                                gpu_name = 'AMD GPU'
                            # VRAM detection for AMD is complex, return 0
                            return 'amd', gpu_name, 0.0
        except (FileNotFoundError, subprocess.TimeoutExpired):
            pass
        
        return None, None, 0.0
    
    def get_recommended_backend(self) -> str:
        """Get recommended compute backend based on detected hardware"""
        hw = self.get_hardware()
        
        if hw.gpu_type == 'nvidia':
            return 'cuda'
        elif hw.gpu_type == 'apple_silicon':
            return 'metal'
        elif hw.gpu_type == 'amd':
            return 'rocm'
        else:
            return 'cpu'
    
    def clear_cache(self) -> None:
        """Clear cached hardware info"""
        HardwareDetector._hardware_cache = None
        if self.cache_file.exists():
            try:
                self.cache_file.unlink()
            except:
                pass


# Module-level singleton accessor
_detector: Optional[HardwareDetector] = None


def get_hardware_detector(base_path: Optional[str] = None) -> HardwareDetector:
    """Get the singleton hardware detector instance"""
    global _detector
    if _detector is None:
        _detector = HardwareDetector(base_path)
    return _detector


def get_hardware_info(force_refresh: bool = False) -> HardwareInfo:
    """
    Convenience function to get hardware info.
    
    Usage:
        hw = get_hardware_info()
        print(f"GPU: {hw.gpu_name}, RAM: {hw.ram_gb}GB")
    """
    return get_hardware_detector().get_hardware(force_refresh)


def detect_hardware_at_startup(base_path: Optional[str] = None) -> HardwareInfo:
    """
    Detect hardware at application startup and cache results.
    Call this once during app initialization.
    
    Returns:
        HardwareInfo with detected hardware
    """
    detector = get_hardware_detector(base_path)
    # Only detect if no cache exists
    if HardwareDetector._hardware_cache is None:
        return detector.get_hardware(force_refresh=True)
    return detector.get_hardware(force_refresh=False)
