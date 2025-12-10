"""
Model Recommendation Service

Provides intelligent LLM model recommendations based on:
- Hardware capabilities (GPU, VRAM, RAM, disk space)
- User's intended use case (coding, chat, reasoning, etc.)
- Performance preferences (speed vs quality)
"""
import os
import platform
import subprocess
import psutil
import shutil
from dataclasses import dataclass, field
from typing import List, Dict, Optional, Any
from enum import Enum


class UseCase(str, Enum):
    """LLM use cases"""
    CHAT = "chat"
    CODING = "coding"
    REASONING = "reasoning"
    CREATIVE = "creative"
    GENERAL = "general"


class GPUType(str, Enum):
    """GPU types"""
    NVIDIA = "nvidia"
    AMD = "amd"
    APPLE_SILICON = "apple_silicon"
    INTEL = "intel"
    NONE = "none"


@dataclass
class HardwareProfile:
    """User's hardware capabilities"""
    gpu_type: GPUType
    gpu_name: Optional[str] = None
    vram_gb: float = 0.0
    ram_gb: float = 0.0
    disk_free_gb: float = 0.0
    cpu_cores: int = 0
    cpu_name: Optional[str] = None
    platform: str = ""
    
    def to_dict(self) -> Dict[str, Any]:
        return {
            'gpu_type': self.gpu_type.value,
            'gpu_name': self.gpu_name,
            'vram_gb': round(self.vram_gb, 2),
            'ram_gb': round(self.ram_gb, 2),
            'disk_free_gb': round(self.disk_free_gb, 2),
            'cpu_cores': self.cpu_cores,
            'cpu_name': self.cpu_name,
            'platform': self.platform
        }


@dataclass
class ModelRecommendation:
    """Recommended model with scoring"""
    model_id: str
    model_name: str
    size_gb: float
    quantization: str
    score: float
    speed_rating: int  # 1-5 stars
    quality_rating: int  # 1-5 stars
    fits_vram: bool
    fits_ram: bool
    use_case_match: str
    description: str
    download_url: Optional[str] = None
    
    def to_dict(self) -> Dict[str, Any]:
        return {
            'model_id': self.model_id,
            'model_name': self.model_name,
            'size_gb': round(self.size_gb, 2),
            'quantization': self.quantization,
            'score': round(self.score, 2),
            'speed_rating': self.speed_rating,
            'quality_rating': self.quality_rating,
            'fits_vram': self.fits_vram,
            'fits_ram': self.fits_ram,
            'use_case_match': self.use_case_match,
            'description': self.description,
            'download_url': self.download_url
        }


class ModelRecommendationService:
    """Service for hardware detection and model recommendations"""
    
    def __init__(self):
        self._hardware_cache: Optional[HardwareProfile] = None
        self._load_cached_hardware()
    
    def _load_cached_hardware(self):
        """Load cached hardware profile from disk"""
        import json
        from pathlib import Path
        
        # Use app's own folder - no fallback to user home
        from app.config_manager import get_app_directory
        cache_file = get_app_directory() / 'config' / 'hardware_cache.json'
        if cache_file.exists():
            try:
                with open(cache_file, 'r') as f:
                    data = json.load(f)
                    # Convert gpu_type string to GPUType enum
                    if 'gpu_type' in data and isinstance(data['gpu_type'], str):
                        data['gpu_type'] = GPUType(data['gpu_type'])
                    self._hardware_cache = HardwareProfile(**data)
            except:
                pass  # If cache is invalid, will re-detect
    
    def _save_cached_hardware(self, profile: HardwareProfile):
        """Save hardware profile to disk cache"""
        import json
        from pathlib import Path
        from dataclasses import asdict
        
        # Use app's own folder - no fallback to user home
        from app.config_manager import get_app_directory
        cache_file = get_app_directory() / 'config' / 'hardware_cache.json'
        cache_file.parent.mkdir(parents=True, exist_ok=True)
        try:
            with open(cache_file, 'w') as f:
                json.dump(asdict(profile), f, indent=2)
        except:
            pass  # Non-critical if save fails
    
    def detect_hardware(self, force_refresh: bool = False, progress_callback=None) -> HardwareProfile:
        """Detect user's hardware capabilities (uses cache if available)
        
        Args:
            force_refresh: If True, force re-detection even if cache exists
            progress_callback: Optional callback function(step, message, percent) for progress updates
        """
        if self._hardware_cache and not force_refresh:
            if progress_callback:
                progress_callback("complete", "Using cached hardware profile", 100)
            return self._hardware_cache
        
        if progress_callback:
            progress_callback("start", "Starting hardware detection...", 0)
        
        # Detect GPU
        if progress_callback:
            progress_callback("gpu", "Detecting GPU...", 10)
        gpu_type, gpu_name, vram_gb = self._detect_gpu()
        
        # Detect RAM
        if progress_callback:
            progress_callback("ram", "Detecting RAM...", 30)
        ram_gb = psutil.virtual_memory().total / (1024 ** 3)
        
        # Detect disk space
        if progress_callback:
            progress_callback("disk", "Checking disk space...", 50)
        disk_free_gb = self._get_disk_free_space()
        
        # Detect CPU
        if progress_callback:
            progress_callback("cpu", "Detecting CPU...", 70)
        cpu_cores = psutil.cpu_count(logical=False) or psutil.cpu_count()
        cpu_name = self._get_cpu_name()
        
        # Platform
        if progress_callback:
            progress_callback("platform", "Detecting platform...", 85)
        platform_name = platform.system()
        
        if progress_callback:
            progress_callback("finalize", "Finalizing hardware profile...", 90)
        
        profile = HardwareProfile(
            gpu_type=gpu_type,
            gpu_name=gpu_name,
            vram_gb=vram_gb,
            ram_gb=ram_gb,
            disk_free_gb=disk_free_gb,
            cpu_cores=cpu_cores,
            cpu_name=cpu_name,
            platform=platform_name
        )
        
        self._hardware_cache = profile
        # Save to cache for fast access next time
        self._save_cached_hardware(profile)
        
        if progress_callback:
            progress_callback("complete", "Hardware detection complete!", 100)
        
        return profile
    
    def _detect_gpu(self) -> tuple[GPUType, Optional[str], float]:
        """Detect GPU type, name, and VRAM"""
        system = platform.system()
        
        # Try NVIDIA first (most common for ML) - single nvidia-smi call for both check and info
        gpu_name, vram_gb = self._get_nvidia_info_fast()
        if gpu_name:
            return GPUType.NVIDIA, gpu_name, vram_gb
        
        # Check for Apple Silicon
        if system == "Darwin":
            if platform.machine() == "arm64":
                # Apple Silicon (M1/M2/M3)
                gpu_name = self._get_apple_silicon_name()
                # Unified memory, use 60% of RAM as estimate for GPU
                vram_gb = psutil.virtual_memory().total / (1024 ** 3) * 0.6
                return GPUType.APPLE_SILICON, gpu_name, vram_gb
        
        # Check for AMD GPU
        if self._has_amd_gpu():
            gpu_name = self._get_amd_gpu_name()
            vram_gb = 0.0  # Harder to detect AMD VRAM
            return GPUType.AMD, gpu_name, vram_gb
        
        # No discrete GPU found
        return GPUType.NONE, None, 0.0
    
    def _get_nvidia_info_fast(self) -> tuple[Optional[str], float]:
        """Get NVIDIA GPU name and VRAM in a single call (fast, no double-check)"""
        try:
            result = subprocess.run(
                ['nvidia-smi', '--query-gpu=name,memory.total', '--format=csv,noheader,nounits'],
                capture_output=True,
                text=True,
                timeout=2,  # Short timeout
                creationflags=subprocess.CREATE_NO_WINDOW if platform.system() == 'Windows' else 0
            )
            
            if result.returncode == 0 and result.stdout.strip():
                lines = result.stdout.strip().split('\n')
                if lines:
                    parts = lines[0].split(',')
                    if len(parts) >= 2:
                        gpu_name = parts[0].strip()
                        try:
                            vram_mb = float(parts[1].strip())
                            return gpu_name, vram_mb / 1024
                        except ValueError:
                            return gpu_name, 0.0
                    elif len(parts) == 1:
                        return parts[0].strip(), 0.0
            return None, 0.0
        except (FileNotFoundError, subprocess.TimeoutExpired, Exception):
            return None, 0.0
    
    def _has_nvidia_gpu(self) -> bool:
        """Check if NVIDIA GPU is present"""
        try:
            result = subprocess.run(
                ['nvidia-smi', '--query-gpu=name', '--format=csv,noheader'],
                capture_output=True,
                text=True,
                timeout=2  # Reduced timeout to prevent hanging
            )
            return result.returncode == 0 and result.stdout.strip()
        except (FileNotFoundError, subprocess.TimeoutExpired):
            return False
    
    def _get_nvidia_info(self) -> tuple[Optional[str], float]:
        """Get NVIDIA GPU name and VRAM"""
        try:
            # Use single nvidia-smi call to get both name and memory (faster, less blocking)
            result = subprocess.run(
                ['nvidia-smi', '--query-gpu=name,memory.total', '--format=csv,noheader,nounits'],
                capture_output=True,
                text=True,
                timeout=3  # Reduced timeout
            )
            
            if result.returncode == 0 and result.stdout.strip():
                lines = result.stdout.strip().split('\n')
                if lines:
                    parts = lines[0].split(',')
                    if len(parts) >= 2:
                        gpu_name = parts[0].strip()
                        try:
                            vram_mb = float(parts[1].strip())
                            vram_gb = vram_mb / 1024
                            return gpu_name, vram_gb
                        except ValueError:
                            return gpu_name, 0.0
                    elif len(parts) == 1:
                        return parts[0].strip(), 0.0
            
            return None, 0.0
        except (FileNotFoundError, subprocess.TimeoutExpired, ValueError, Exception):
            return None, 0.0
    
    def _has_amd_gpu(self) -> bool:
        """Check if AMD GPU is present"""
        system = platform.system()
        if system == "Linux":
            try:
                result = subprocess.run(
                    ['lspci'],
                    capture_output=True,
                    text=True,
                    timeout=2  # Reduced from 5 to 2
                )
                return 'AMD' in result.stdout or 'Radeon' in result.stdout
            except (FileNotFoundError, subprocess.TimeoutExpired):
                return False
        return False
    
    def _get_amd_gpu_name(self) -> Optional[str]:
        """Get AMD GPU name"""
        try:
            result = subprocess.run(
                ['lspci'],
                capture_output=True,
                text=True,
                timeout=2  # Reduced from 5 to 2
            )
            for line in result.stdout.split('\n'):
                if 'AMD' in line or 'Radeon' in line:
                    # Extract GPU name
                    parts = line.split(':')
                    if len(parts) >= 3:
                        return parts[2].strip()
            return "AMD GPU"
        except (FileNotFoundError, subprocess.TimeoutExpired):
            return None
    
    def _get_apple_silicon_name(self) -> str:
        """Get Apple Silicon chip name - try platform first, subprocess only if needed"""
        try:
            # Try platform.processor() first (no subprocess)
            cpu_name = platform.processor()
            if cpu_name and ('Apple' in cpu_name or 'M1' in cpu_name or 'M2' in cpu_name or 'M3' in cpu_name):
                return cpu_name
            
            # Fallback to subprocess only if platform doesn't work
            result = subprocess.run(
                ['sysctl', '-n', 'machdep.cpu.brand_string'],
                capture_output=True,
                text=True,
                timeout=2
            )
            if result.returncode == 0:
                return result.stdout.strip()
            return "Apple Silicon"
        except (FileNotFoundError, subprocess.TimeoutExpired, Exception):
            return "Apple Silicon"
    
    def _get_disk_free_space(self) -> float:
        """Get free disk space in GB for LLM storage directory"""
        # Use app's own folder
        from app.config_manager import get_app_directory
        llm_dir = get_app_directory()
        
        try:
            # Ensure directory exists
            if not llm_dir.exists():
                llm_dir.mkdir(parents=True, exist_ok=True)
            stat = shutil.disk_usage(str(llm_dir))
            return stat.free / (1024 ** 3)
        except Exception:
            return 0.0
    
    def _get_cpu_name(self) -> Optional[str]:
        """Get CPU name - using Python libraries first, subprocess only as fallback"""
        # Try platform.processor() first - works on all platforms, no subprocess!
        try:
            cpu_name = platform.processor()
            if cpu_name and cpu_name.strip() and cpu_name.strip() != '':
                return cpu_name.strip()
        except:
            pass
        
        # Platform-specific fallbacks (no subprocess for Linux)
        system = platform.system()
        
        try:
            if system == "Linux":
                # Read /proc/cpuinfo directly (no subprocess needed!)
                try:
                    with open('/proc/cpuinfo', 'r') as f:
                        for line in f:
                            if 'model name' in line:
                                return line.split(':')[1].strip()
                except:
                    pass
            
            # Windows and macOS: Only use subprocess as last resort
            # (platform.processor() usually works, but try subprocess if it didn't)
            if system == "Windows":
                try:
                    result = subprocess.run(
                        ['wmic', 'cpu', 'get', 'name'],
                        capture_output=True,
                        text=True,
                        timeout=2  # Short timeout
                    )
                    if result.returncode == 0:
                        lines = result.stdout.strip().split('\n')
                        if len(lines) > 1:
                            return lines[1].strip()
                except:
                    pass
            
            elif system == "Darwin":  # macOS
                try:
                    result = subprocess.run(
                        ['sysctl', '-n', 'machdep.cpu.brand_string'],
                        capture_output=True,
                        text=True,
                        timeout=2  # Short timeout
                    )
                    if result.returncode == 0:
                        return result.stdout.strip()
                except:
                    pass
        except Exception:
            pass
        
        # Final fallback
        return platform.machine() or "Unknown CPU"
    
    def get_recommended_backend(self, hardware: HardwareProfile) -> str:
        """Get recommended GPU backend based on hardware"""
        if hardware.gpu_type == GPUType.NVIDIA:
            return "cuda"
        elif hardware.gpu_type == GPUType.APPLE_SILICON:
            return "metal"
        elif hardware.gpu_type == GPUType.AMD:
            return "vulkan"
        else:
            return "cpu"
    
    def calculate_model_score(
        self,
        model_size_gb: float,
        quantization: str,
        use_case: UseCase,
        model_categories: List[str],
        hardware: HardwareProfile,
        downloads: int = 0
    ) -> float:
        """
        Calculate recommendation score for a model
        
        Scoring weights:
        - Hardware fit: 40%
        - Use case match: 30%
        - Performance tier: 20%
        - Popularity: 10%
        """
        score = 0.0
        
        # Hardware fit (40 points)
        if hardware.vram_gb > 0 and model_size_gb <= hardware.vram_gb:
            score += 40  # Perfect fit in VRAM
        elif model_size_gb <= hardware.ram_gb * 0.5:
            score += 30  # Fits in RAM with headroom
        elif model_size_gb <= hardware.ram_gb * 0.8:
            score += 20  # Fits in RAM (tight)
        else:
            score += 0   # Too large
        
        # Use case match (30 points)
        if use_case.value in model_categories:
            score += 30
        elif any(cat in model_categories for cat in ['general', 'instruct']):
            score += 15  # General models can work for any use case
        
        # Performance tier based on quantization (20 points)
        quant_scores = {
            'Q8_0': 20, 'Q6_K': 19, 'Q5_K_M': 18, 'Q5_K_S': 17,
            'Q4_K_M': 15, 'Q4_K_S': 14, 'Q4_0': 13,
            'Q3_K_M': 11, 'Q3_K_S': 10, 'Q2_K': 8
        }
        score += quant_scores.get(quantization, 10)
        
        # Popularity/reliability (10 points)
        score += min(downloads / 100000, 10)
        
        return score
    
    def get_recommendations(
        self,
        use_case: UseCase,
        hardware: Optional[HardwareProfile] = None,
        max_results: int = 5
    ) -> List[ModelRecommendation]:
        """
        Get model recommendations based on use case and hardware
        
        Args:
            use_case: User's intended use case
            hardware: Hardware profile (auto-detected if not provided)
            max_results: Maximum number of recommendations to return
        
        Returns:
            List of recommended models, sorted by score (highest first)
        """
        if hardware is None:
            hardware = self.detect_hardware()
        
        # Get curated model database
        models = self._get_model_database(use_case)
        
        # Score each model
        recommendations = []
        for model in models:
            score = self.calculate_model_score(
                model_size_gb=model['size_gb'],
                quantization=model['quantization'],
                use_case=use_case,
                model_categories=model['categories'],
                hardware=hardware,
                downloads=model.get('downloads', 0)
            )
            
            # Only recommend models that fit in hardware
            if score < 10:  # Too low score means doesn't fit
                continue
            
            # Determine speed and quality ratings
            speed_rating = self._get_speed_rating(model['size_gb'], model['quantization'])
            quality_rating = self._get_quality_rating(model['quantization'])
            
            recommendation = ModelRecommendation(
                model_id=model['id'],
                model_name=model['name'],
                size_gb=model['size_gb'],
                quantization=model['quantization'],
                score=score,
                speed_rating=speed_rating,
                quality_rating=quality_rating,
                fits_vram=hardware.vram_gb > 0 and model['size_gb'] <= hardware.vram_gb,
                fits_ram=model['size_gb'] <= hardware.ram_gb * 0.8,
                use_case_match=use_case.value,
                description=model['description'],
                download_url=model.get('download_url')
            )
            recommendations.append(recommendation)
        
        # Sort by score (highest first) and return top N
        recommendations.sort(key=lambda x: x.score, reverse=True)
        return recommendations[:max_results]
    
    def _get_speed_rating(self, size_gb: float, quantization: str) -> int:
        """Get speed rating (1-5 stars) based on model size and quantization"""
        # Smaller models and lower quantization = faster
        if size_gb < 3:
            base = 5
        elif size_gb < 5:
            base = 4
        elif size_gb < 8:
            base = 3
        elif size_gb < 12:
            base = 2
        else:
            base = 1
        
        # Adjust for quantization
        if quantization in ['Q2_K', 'Q3_K_S']:
            base = min(5, base + 1)
        elif quantization in ['Q8_0', 'Q6_K']:
            base = max(1, base - 1)
        
        return base
    
    def _get_quality_rating(self, quantization: str) -> int:
        """Get quality rating (1-5 stars) based on quantization"""
        quality_map = {
            'Q8_0': 5, 'Q6_K': 5,
            'Q5_K_M': 4, 'Q5_K_S': 4,
            'Q4_K_M': 3, 'Q4_K_S': 3, 'Q4_0': 3,
            'Q3_K_M': 2, 'Q3_K_S': 2,
            'Q2_K': 1
        }
        return quality_map.get(quantization, 3)
    
    def _get_model_database(self, use_case: UseCase) -> List[Dict[str, Any]]:
        """
        Get curated model database filtered by use case
        
        This is a curated list of popular, well-tested models.
        In production, this could be fetched from HuggingFace API or a database.
        """
        # Curated model database
        all_models = [
            # Coding models
            {
                'id': 'TheBloke/CodeLlama-13B-Instruct-GGUF',
                'name': 'CodeLlama 13B Instruct',
                'size_gb': 7.8,
                'quantization': 'Q4_K_M',
                'categories': ['coding', 'instruct'],
                'description': 'Excellent for Python, JavaScript, and general programming tasks',
                'downloads': 500000,
                'download_url': 'https://huggingface.co/TheBloke/CodeLlama-13B-Instruct-GGUF'
            },
            {
                'id': 'TheBloke/deepseek-coder-6.7B-instruct-GGUF',
                'name': 'DeepSeek Coder 6.7B',
                'size_gb': 4.8,
                'quantization': 'Q5_K_M',
                'categories': ['coding', 'instruct'],
                'description': 'Fast code completion and debugging, great for real-time assistance',
                'downloads': 300000,
                'download_url': 'https://huggingface.co/TheBloke/deepseek-coder-6.7B-instruct-GGUF'
            },
            {
                'id': 'TheBloke/CodeLlama-7B-Instruct-GGUF',
                'name': 'CodeLlama 7B Instruct',
                'size_gb': 4.1,
                'quantization': 'Q4_K_M',
                'categories': ['coding', 'instruct'],
                'description': 'Lightweight coding assistant, perfect for laptops',
                'downloads': 450000,
                'download_url': 'https://huggingface.co/TheBloke/CodeLlama-7B-Instruct-GGUF'
            },
            
            # Chat models
            {
                'id': 'TheBloke/Llama-2-13B-chat-GGUF',
                'name': 'Llama 2 13B Chat',
                'size_gb': 7.3,
                'quantization': 'Q4_K_M',
                'categories': ['chat', 'general'],
                'description': 'Natural conversation and Q&A, well-balanced performance',
                'downloads': 800000,
                'download_url': 'https://huggingface.co/TheBloke/Llama-2-13B-chat-GGUF'
            },
            {
                'id': 'TheBloke/Mistral-7B-Instruct-v0.2-GGUF',
                'name': 'Mistral 7B Instruct v0.2',
                'size_gb': 4.4,
                'quantization': 'Q5_K_M',
                'categories': ['chat', 'instruct', 'general'],
                'description': 'Versatile model for chat and instruction following',
                'downloads': 900000,
                'download_url': 'https://huggingface.co/TheBloke/Mistral-7B-Instruct-v0.2-GGUF'
            },
            
            # Reasoning models
            {
                'id': 'TheBloke/Qwen1.5-14B-Chat-GGUF',
                'name': 'Qwen 1.5 14B Chat',
                'size_gb': 8.5,
                'quantization': 'Q4_K_M',
                'categories': ['reasoning', 'chat', 'general'],
                'description': 'Strong reasoning and problem-solving capabilities',
                'downloads': 250000,
                'download_url': 'https://huggingface.co/TheBloke/Qwen1.5-14B-Chat-GGUF'
            },
            
            # Creative models
            {
                'id': 'TheBloke/Nous-Hermes-2-Mistral-7B-DPO-GGUF',
                'name': 'Nous Hermes 2 Mistral 7B',
                'size_gb': 4.4,
                'quantization': 'Q5_K_M',
                'categories': ['creative', 'chat', 'general'],
                'description': 'Creative writing, storytelling, and content generation',
                'downloads': 350000,
                'download_url': 'https://huggingface.co/TheBloke/Nous-Hermes-2-Mistral-7B-DPO-GGUF'
            },
            
            # General purpose
            {
                'id': 'TheBloke/Llama-2-7B-Chat-GGUF',
                'name': 'Llama 2 7B Chat',
                'size_gb': 4.1,
                'quantization': 'Q4_K_M',
                'categories': ['general', 'chat'],
                'description': 'Lightweight all-purpose assistant, great for beginners',
                'downloads': 1000000,
                'download_url': 'https://huggingface.co/TheBloke/Llama-2-7B-Chat-GGUF'
            },
            {
                'id': 'TheBloke/Phi-3-mini-4k-instruct-GGUF',
                'name': 'Phi-3 Mini 4K Instruct',
                'size_gb': 2.4,
                'quantization': 'Q4_K_M',
                'categories': ['general', 'instruct'],
                'description': 'Tiny but capable, runs on almost any hardware',
                'downloads': 400000,
                'download_url': 'https://huggingface.co/microsoft/Phi-3-mini-4k-instruct-gguf'
            },
        ]
        
        # Filter by use case
        if use_case == UseCase.GENERAL:
            # Return all models for general use case
            return all_models
        else:
            # Filter models that match the use case
            filtered = [m for m in all_models if use_case.value in m['categories']]
            # If no exact matches, include general models
            if not filtered:
                filtered = [m for m in all_models if 'general' in m['categories']]
            return filtered


# Singleton instance
_recommendation_service_instance: Optional[ModelRecommendationService] = None


def get_recommendation_service() -> ModelRecommendationService:
    """Get singleton instance of recommendation service (cached for performance)"""
    global _recommendation_service_instance
    if _recommendation_service_instance is None:
        _recommendation_service_instance = ModelRecommendationService()
    return _recommendation_service_instance
