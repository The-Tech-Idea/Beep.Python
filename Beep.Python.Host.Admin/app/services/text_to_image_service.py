"""
Text to Image Service

Generates images from text prompts using Stable Diffusion and other models.
"""
import os
import sys
import subprocess
import base64
import io
import json
import logging
import tempfile
from pathlib import Path
from typing import Dict, Any, Optional, Tuple
from datetime import datetime

logger = logging.getLogger(__name__)


class TextToImageService:
    """Service for generating images from text"""
    
    def __init__(self):
        from app.services.ai_services_environment import AIServiceType, get_ai_service_env
        
        self.service_type = AIServiceType.TEXT_TO_IMAGE
        self.env_mgr = get_ai_service_env(self.service_type)
        self._model_loaded = False
    
    def _get_python_executable(self):
        """Get Python executable from the virtual environment (like other modules)"""
        # Use the environment manager's method directly, like document_extractor does
        python_exe = self.env_mgr._get_python_path()
        if python_exe and python_exe.exists():
            logger.debug(f"Using Python from text-to-image virtual environment: {python_exe}")
            return python_exe
        
        logger.warning(f"Python executable not found in text-to-image environment: {python_exe}")
        return None
    
    def generate_image(self, prompt: str, negative_prompt: str = "", 
                      width: int = 512, height: int = 512, 
                      num_inference_steps: int = 50,
                      guidance_scale: float = 7.5,
                      seed: Optional[int] = None,
                      model_id: Optional[str] = None,
                      use_refiner: bool = False,
                      use_cpu_offload: bool = False,
                      max_sequence_length: int = 512,
                      true_cfg_scale: Optional[float] = None) -> Dict[str, Any]:
        """
        Generate an image from text prompt
        
        Supports:
        - Stable Diffusion 1.5 and earlier
        - Stable Diffusion XL (SDXL) with optional refiner (base+refiner ensemble with 80/20 split)
        - Z-Image-Turbo (fast anime generation with DiT architecture)
        - Animagine XL (anime models with custom LPW pipeline)
        - FLUX.1-dev (Black Forest Labs FLUX.1 [dev] via diffusers FluxPipeline)
        - UltraFlux (high-resolution generation up to 4096x4096)
        - Qwen-Image (Qwen/Qwen-Image with true_cfg_scale parameter)
        - CPU offloading for low VRAM systems (pipe.enable_model_cpu_offload())
        - torch.compile optimization (torch >= 2.0, improves speed by 20-30%)
        - bfloat16 for optimal performance on supported GPUs
        - Custom pipelines (e.g., lpw_stable_diffusion_xl for better prompt handling)
        - FlowMatchEulerDiscreteScheduler for UltraFlux models
        - DiffusionPipeline API for SDXL (recommended by diffusers >= 0.19.0)
        
        IMPORTANT: Only one model per service type can be loaded at a time.
        Loading a new model automatically unloads the previous one.
        
        Args:
            prompt: Text description of the image
            negative_prompt: Things to avoid in the image
            width: Image width in pixels
            height: Image height in pixels
            num_inference_steps: Number of denoising steps (10-100)
            guidance_scale: How closely to follow the prompt (1-20)
            seed: Random seed for reproducibility
            model_id: HuggingFace model ID (e.g., "stabilityai/stable-diffusion-xl-base-1.0", "Owen777/UltraFlux-v1")
            use_refiner: For SDXL, use base+refiner ensemble (80/20 split)
            use_cpu_offload: Enable CPU offloading for low VRAM systems
            max_sequence_length: For UltraFlux, maximum token length for prompt (default: 512)
            true_cfg_scale: For Qwen-Image models, CFG scale parameter (default: 4.0, uses guidance_scale if not provided)
        
        Returns:
            Dict with 'success', 'image' (base64), 'metadata', 'error'
        """
        try:
            # Check if virtual environment is ready
            python_exe = self._get_python_executable()
            if not python_exe:
                return {
                    'success': False,
                    'error': 'Text-to-Image virtual environment not set up. Please create environment and install packages first.'
                }
            
            # Get model ID
            if not model_id:
                from app.services.ai_service_models import AIServiceModelConfig
                model_id = AIServiceModelConfig.get_selected_model('text_to_image') or 'runwayml/stable-diffusion-v1-5'
            
            # Track loaded model (only one per service type)
            from app.services.ai_service_loaded_models import get_ai_service_loaded_tracker
            tracker = get_ai_service_loaded_tracker()
            previous_model = tracker.load_model('text_to_image', model_id)
            if previous_model and previous_model != model_id:
                logger.info(f"[TextToImage] Switched from {previous_model} to {model_id}")
            
            # Check if model is available locally and update last_used
            try:
                from app.services.ai_service_model_manager import get_ai_service_model_manager
                model_mgr = get_ai_service_model_manager('text_to_image')
                local_model = model_mgr.get_model_by_id(model_id)
                
                if local_model:
                    # Update last_used timestamp
                    for model_data in model_mgr._index.get('models', []):
                        if model_data.get('model_id') == model_id:
                            model_data['last_used'] = datetime.now().isoformat()
                            model_mgr._save_index()
                            logger.info(f"Using local model: {model_id}")
                            break
            except Exception as e:
                logger.debug(f"Could not check/update local model: {e}")
            
            # Update tracker last_used
            tracker.update_last_used('text_to_image')
            
            # Use subprocess to run in dedicated environment
            return self._generate_via_subprocess(
                prompt, negative_prompt, width, height, 
                num_inference_steps, guidance_scale, seed, model_id,
                use_refiner, use_cpu_offload, max_sequence_length, true_cfg_scale
            )
        except Exception as e:
            logger.error(f"Error generating image: {e}", exc_info=True)
            return {
                'success': False,
                'error': str(e)
            }
    
    def _generate_via_subprocess(self, prompt: str, negative_prompt: str,
                                width: int, height: int,
                                num_inference_steps: int, guidance_scale: float,
                                seed: Optional[int], model_id: str,
                                use_refiner: bool, use_cpu_offload: bool,
                                max_sequence_length: int = 512,
                                true_cfg_scale: Optional[float] = None) -> Dict[str, Any]:
        """Generate image using subprocess with dedicated environment"""
        import tempfile
        import json
        
        # Timeout for generation (5 minutes - first use may download model)
        timeout = 300
        
        # Get Python executable from virtual environment (like document_extractor pattern)
        python_exe = self._get_python_executable()
        if not python_exe:
            return {
                'success': False,
                'error': 'Python executable not found in text-to-image virtual environment. Please ensure the environment is created and packages are installed.'
            }
        
        # Log environment info for debugging
        env_path = self.env_mgr._env_path if hasattr(self.env_mgr, '_env_path') else 'unknown'
        logger.info(f"Text-to-Image: Using virtual environment at {env_path}")
        logger.info(f"Text-to-Image: Python executable: {python_exe}")
        
        try:
            script_content = '''import sys
import json
import base64
import os
from io import BytesIO

prompt = sys.argv[1]
negative_prompt = sys.argv[2]
width = int(sys.argv[3])
height = int(sys.argv[4])
num_steps = int(sys.argv[5])
guidance_scale = float(sys.argv[6])
seed = sys.argv[7] if sys.argv[7] != "None" else None
model_id = sys.argv[8] if len(sys.argv) > 8 else "runwayml/stable-diffusion-v1-5"
use_refiner = sys.argv[9].lower() == "true" if len(sys.argv) > 9 else False
use_cpu_offload = sys.argv[10].lower() == "true" if len(sys.argv) > 10 else False
max_sequence_length = int(sys.argv[11]) if len(sys.argv) > 11 else 512
true_cfg_scale = float(sys.argv[12]) if len(sys.argv) > 12 and sys.argv[12] != "None" else None

try:
    from diffusers import StableDiffusionPipeline, StableDiffusionXLPipeline, DiffusionPipeline
    import torch
    from PIL import Image
    
    # Try to import ZImagePipeline (for Z-Image-Turbo models)
    try:
        from diffusers import ZImagePipeline
        has_zimage = True
    except ImportError:
        has_zimage = False
        ZImagePipeline = None
    
    # Try to import FluxPipeline from diffusers (for FLUX.1-dev models)
    try:
        from diffusers import FluxPipeline as DiffusersFluxPipeline
        has_flux = True
    except ImportError:
        has_flux = False
        DiffusersFluxPipeline = None
    
    # Try to import UltraFlux pipeline (separate package)
    try:
        from ultraflux.pipeline_flux import FluxPipeline
        from ultraflux.transformer_flux_visionyarn import FluxTransformer2DModel
        from ultraflux.autoencoder_kl import AutoencoderKL
        from diffusers import FlowMatchEulerDiscreteScheduler
        has_ultraflux = True
    except ImportError:
        has_ultraflux = False
        FluxPipeline = None
        FluxTransformer2DModel = None
        AutoencoderKL = None
        FlowMatchEulerDiscreteScheduler = None
    
    # Set environment variable to suppress warnings
    os.environ["TOKENIZERS_PARALLELISM"] = "false"
    
    # Check if CUDA is available
    use_cuda = torch.cuda.is_available()
    device = "cuda" if use_cuda else "cpu"
    
    # Determine dtype - use bfloat16 for FLUX, Qwen, and Z-Image models if supported, otherwise float16
    if use_cuda:
        try:
            # Check if bfloat16 is supported
            test_tensor = torch.tensor([1.0], dtype=torch.bfloat16, device=device)
            # FLUX.1, Qwen-Image, and Z-Image models prefer bfloat16
            dtype = torch.bfloat16
        except:
            dtype = torch.float16
    else:
        dtype = torch.float32
    
    # Load model - use local cache if available
    cache_dir = os.environ.get('HF_HOME', os.path.expanduser('~/.cache/huggingface'))
    
    # Detect model type
    is_qwen = "qwen" in model_id.lower() and "image" in model_id.lower()
    is_flux = ("flux" in model_id.lower() and "ultraflux" not in model_id.lower() and "ultra-flux" not in model_id.lower()) or "black-forest-labs" in model_id.lower()
    is_ultraflux = "ultraflux" in model_id.lower() or "ultra-flux" in model_id.lower()
    is_zimage = "z-image" in model_id.lower() or "zimage" in model_id.lower()
    is_sdxl = "xl" in model_id.lower() or "stable-diffusion-xl" in model_id.lower() or "animagine" in model_id.lower()
    is_turbo = "turbo" in model_id.lower()
    
    # Load appropriate pipeline
    if is_flux and has_flux:
        # FLUX.1-dev pipeline (from diffusers library)
        pipe = DiffusersFluxPipeline.from_pretrained(
            model_id,
            torch_dtype=dtype,
            cache_dir=cache_dir
        )
        
        if use_cpu_offload:
            pipe.enable_model_cpu_offload()
        elif use_cuda:
            pipe = pipe.to(device)
        
        # Optimize with torch.compile if available (torch >= 2.0)
        if use_cuda and hasattr(torch, 'compile'):
            try:
                torch_version = torch.__version__.split('.')
                if len(torch_version) >= 2 and int(torch_version[0]) >= 2:
                    pipe.transformer = torch.compile(pipe.transformer, mode="reduce-overhead", fullgraph=True)
            except:
                pass
        
        generator = None
        if seed:
            generator = torch.Generator(device="cpu")  # FLUX uses CPU generator
            generator.manual_seed(int(seed))
        else:
            generator = torch.Generator(device="cpu")
        
        image = pipe(
            prompt=prompt,
            height=height,
            width=width,
            guidance_scale=guidance_scale,
            num_inference_steps=num_steps,
            max_sequence_length=max_sequence_length,
            generator=generator
        ).images[0]
        
    elif is_ultraflux and has_ultraflux:
        # UltraFlux pipeline (high-resolution generation up to 4096x4096)
        # Load components separately for better control
        local_vae = AutoencoderKL.from_pretrained(
            model_id,
            subfolder="vae",
            torch_dtype=dtype,
            cache_dir=cache_dir
        )
        
        transformer = FluxTransformer2DModel.from_pretrained(
            model_id,
            subfolder="transformer",
            torch_dtype=dtype,
            cache_dir=cache_dir
        )
        
        # Load pipeline with custom components
        pipe = FluxPipeline.from_pretrained(
            model_id,
            vae=local_vae,
            transformer=transformer,
            torch_dtype=dtype,
            cache_dir=cache_dir
        )
        
        # Configure scheduler (FlowMatchEulerDiscreteScheduler)
        from diffusers import FlowMatchEulerDiscreteScheduler
        pipe.scheduler = FlowMatchEulerDiscreteScheduler.from_config(pipe.scheduler.config)
        pipe.scheduler.config.use_dynamic_shifting = False
        pipe.scheduler.config.time_shift = 4
        
        if use_cpu_offload:
            pipe.enable_model_cpu_offload()
        elif use_cuda:
            pipe = pipe.to(device)
        
        generator = None
        if seed:
            generator = torch.Generator(device=device)
            generator.manual_seed(int(seed))
        
        image = pipe(
            prompt=prompt,
            negative_prompt=negative_prompt if negative_prompt else None,
            height=height,
            width=width,
            guidance_scale=guidance_scale,
            num_inference_steps=num_steps,
            max_sequence_length=max_sequence_length,
            generator=generator
        ).images[0]
        
    elif is_zimage and has_zimage:
        # Z-Image-Turbo pipeline (fast anime generation)
        pipe = ZImagePipeline.from_pretrained(
            model_id,
            torch_dtype=dtype,
            low_cpu_mem_usage=False,
            cache_dir=cache_dir
        )
        
        if use_cpu_offload:
            pipe.enable_model_cpu_offload()
        elif use_cuda:
            pipe = pipe.to(device)
        
        # Optional: Set attention backend (Flash Attention)
        # pipe.transformer.set_attention_backend("flash")  # Flash-Attention-2
        # pipe.transformer.set_attention_backend("_flash_3")  # Flash-Attention-3
        
        # Optional: Compile DiT model for faster inference
        # pipe.transformer.compile()
        
        generator = None
        if seed:
            generator = torch.Generator(device=device)
            generator.manual_seed(int(seed))
        
        # Turbo models use guidance_scale=0.0
        guidance_scale_value = 0.0 if is_turbo else guidance_scale
        
        image = pipe(
            prompt=prompt,
            height=height,
            width=width,
            num_inference_steps=num_steps,
            guidance_scale=guidance_scale_value,
            generator=generator
        ).images[0]
        
    elif is_sdxl:
        # Stable Diffusion XL
        if use_refiner:
            # Use base + refiner ensemble
            # Determine base and refiner IDs
            if "refiner" in model_id.lower():
                # User selected refiner, get base from it
                base_id = model_id.replace("-refiner", "-base").replace("_refiner", "_base")
                refiner_id = model_id
            elif "base" in model_id.lower():
                # User selected base, get refiner from it
                base_id = model_id
                refiner_id = model_id.replace("-base", "-refiner").replace("_base", "_refiner")
            else:
                # Generic SDXL model, try standard naming
                base_id = model_id if "base" in model_id.lower() else f"{model_id}-base" if not model_id.endswith("-base") else model_id
                refiner_id = model_id.replace("-base", "-refiner") if "-base" in model_id else f"{model_id}-refiner"
            
            # Load base pipeline (using DiffusionPipeline as recommended)
            base = DiffusionPipeline.from_pretrained(
                base_id,
                torch_dtype=dtype,
                variant="fp16" if use_cuda else None,
                use_safetensors=True,
                cache_dir=cache_dir
            )
            
            if use_cpu_offload:
                base.enable_model_cpu_offload()
            elif use_cuda:
                base = base.to(device)
            
            # Optimize base with torch.compile if available (torch >= 2.0)
            if use_cuda and hasattr(torch, 'compile'):
                try:
                    torch_version = torch.__version__.split('.')
                    if len(torch_version) >= 2 and int(torch_version[0]) >= 2:
                        base.unet = torch.compile(base.unet, mode="reduce-overhead", fullgraph=True)
                except:
                    pass
            
            # Load refiner pipeline (reuse text_encoder_2 and vae from base)
            refiner = DiffusionPipeline.from_pretrained(
                refiner_id,
                text_encoder_2=base.text_encoder_2,
                vae=base.vae,
                torch_dtype=dtype,
                variant="fp16" if use_cuda else None,
                use_safetensors=True,
                cache_dir=cache_dir
            )
            
            if use_cpu_offload:
                refiner.enable_model_cpu_offload()
            elif use_cuda:
                refiner = refiner.to(device)
            
            # Optimize refiner with torch.compile if available (torch >= 2.0)
            if use_cuda and hasattr(torch, 'compile'):
                try:
                    torch_version = torch.__version__.split('.')
                    if len(torch_version) >= 2 and int(torch_version[0]) >= 2:
                        refiner.unet = torch.compile(refiner.unet, mode="reduce-overhead", fullgraph=True)
                except:
                    pass
            
            # Generate with base + refiner ensemble (80/20 split)
            # Define how many steps and what % of steps to be run on each expert
            n_steps = num_steps
            high_noise_frac = 0.8  # 80% on base, 20% on refiner
            
            generator = None
            if seed:
                generator = torch.Generator(device=device)
                generator.manual_seed(int(seed))
            
            # Run base pipeline (80% of denoising)
            image = base(
                prompt=prompt,
                negative_prompt=negative_prompt if negative_prompt else None,
                num_inference_steps=n_steps,
                denoising_end=high_noise_frac,
                output_type="latent",
                generator=generator
            ).images
            
            # Run refiner pipeline (20% of denoising)
            image = refiner(
                prompt=prompt,
                negative_prompt=negative_prompt if negative_prompt else None,
                num_inference_steps=n_steps,
                denoising_start=high_noise_frac,
                image=image,
                generator=generator
            ).images[0]
            
        else:
            # Single SDXL pipeline (base only)
            # Check if model needs custom pipeline (e.g., lpw_stable_diffusion_xl for Animagine)
            use_custom_pipeline = "animagine" in model_id.lower() or "lpw" in model_id.lower()
            
            if use_custom_pipeline:
                # Use StableDiffusionXLPipeline for custom pipelines
                custom_pipeline = "lpw_stable_diffusion_xl"
                pipe = StableDiffusionXLPipeline.from_pretrained(
                    model_id,
                    torch_dtype=dtype,
                    variant="fp16" if use_cuda else None,
                    use_safetensors=True,
                    custom_pipeline=custom_pipeline,
                    add_watermarker=False,
                    cache_dir=cache_dir
                )
            else:
                # Use DiffusionPipeline for standard SDXL (simpler API, recommended by diffusers)
                pipe = DiffusionPipeline.from_pretrained(
                    model_id,
                    torch_dtype=dtype,
                    variant="fp16" if use_cuda else None,
                    use_safetensors=True,
                    cache_dir=cache_dir
                )
            
            if use_cpu_offload:
                pipe.enable_model_cpu_offload()
            elif use_cuda:
                pipe = pipe.to(device)
            
            # Optimize with torch.compile if available (torch >= 2.0)
            # This improves inference speed by 20-30%
            if use_cuda and hasattr(torch, 'compile'):
                try:
                    import torch
                    # Check if torch version is >= 2.0
                    torch_version = torch.__version__.split('.')
                    if len(torch_version) >= 2 and int(torch_version[0]) >= 2:
                        pipe.unet = torch.compile(pipe.unet, mode="reduce-overhead", fullgraph=True)
                except Exception as e:
                    pass  # Fallback if compile fails
            
            generator = None
            if seed:
                generator = torch.Generator(device=device)
                generator.manual_seed(int(seed))
            
            image = pipe(
                prompt=prompt,
                negative_prompt=negative_prompt if negative_prompt else None,
                width=width,
                height=height,
                num_inference_steps=num_steps,
                guidance_scale=guidance_scale,
                generator=generator
            ).images[0]
    
    elif is_qwen:
        # Qwen-Image pipeline (uses DiffusionPipeline with true_cfg_scale)
        pipe = DiffusionPipeline.from_pretrained(
            model_id,
            torch_dtype=dtype,
            cache_dir=cache_dir
        )
        
        if use_cpu_offload:
            pipe.enable_model_cpu_offload()
        elif use_cuda:
            pipe = pipe.to(device)
        
        # Optimize with torch.compile if available (torch >= 2.0)
        if use_cuda and hasattr(torch, 'compile'):
            try:
                torch_version = torch.__version__.split('.')
                if len(torch_version) >= 2 and int(torch_version[0]) >= 2:
                    pipe.unet = torch.compile(pipe.unet, mode="reduce-overhead", fullgraph=True)
            except:
                pass
        
        generator = None
        if seed:
            generator = torch.Generator(device=device)
            generator.manual_seed(int(seed))
        
        # Qwen-Image uses true_cfg_scale instead of guidance_scale
        cfg_scale = true_cfg_scale if true_cfg_scale is not None else guidance_scale
        
        # Prepare pipeline arguments
        pipe_kwargs = {
            "prompt": prompt,
            "negative_prompt": negative_prompt if negative_prompt else " ",
            "width": width,
            "height": height,
            "num_inference_steps": num_steps,
            "true_cfg_scale": cfg_scale,
            "generator": generator
        }
        
        image = pipe(**pipe_kwargs).images[0]
    
    else:
        # Stable Diffusion 1.5 or earlier
        pipe = StableDiffusionPipeline.from_pretrained(
            model_id,
            torch_dtype=dtype,
            safety_checker=None,  # Disable safety checker for faster generation
            requires_safety_checker=False,
            use_safetensors=True,
            cache_dir=cache_dir
        )
        
        if use_cpu_offload:
            pipe.enable_model_cpu_offload()
        elif use_cuda:
            pipe = pipe.to(device)
        
        generator = None
        if seed:
            generator = torch.Generator(device=device)
            generator.manual_seed(int(seed))
        
        image = pipe(
            prompt=prompt,
            negative_prompt=negative_prompt if negative_prompt else None,
            width=width,
            height=height,
            num_inference_steps=num_steps,
            guidance_scale=guidance_scale,
            generator=generator
        ).images[0]
    
    # Convert to base64
    buffered = BytesIO()
    image.save(buffered, format="PNG")
    img_base64 = base64.b64encode(buffered.getvalue()).decode()
    
    result = {
        "success": True,
        "image": img_base64,
        "metadata": {
            "width": width,
            "height": height,
            "steps": num_steps,
            "guidance_scale": guidance_scale if not is_turbo else 0.0,
            "seed": seed,
            "device": device,
            "model": model_id,
            "is_sdxl": is_sdxl,
            "is_zimage": is_zimage,
            "is_ultraflux": is_ultraflux,
            "is_turbo": is_turbo,
            "used_refiner": use_refiner and is_sdxl,
            "used_cpu_offload": use_cpu_offload,
            "max_sequence_length": max_sequence_length if (is_flux or is_ultraflux) else None,
            "true_cfg_scale": true_cfg_scale if is_qwen else None,
            "is_flux": is_flux,
            "is_qwen": is_qwen
        }
    }
    print(json.dumps(result))
    
except Exception as e:
    import traceback
    error_result = {
        "success": False,
        "error": str(e),
        "traceback": traceback.format_exc()[:500]
    }
    print(json.dumps(error_result))
    sys.exit(1)
'''
            
            # Write script to temp file
            with tempfile.NamedTemporaryFile(mode='w', suffix='.py', delete=False) as f:
                f.write(script_content)
                script_path = f.name
            
            try:
                seed_str = str(seed) if seed else "None"
                
                # python_exe already retrieved at start of function
                logger.info(f"Starting image generation: model={model_id}, steps={num_inference_steps}, size={width}x{height}")
                logger.info(f"Using Python from virtual environment: {python_exe}")
                
                true_cfg_scale_str = str(true_cfg_scale) if true_cfg_scale is not None else "None"
                result = subprocess.run(
                    [str(python_exe), script_path,
                     prompt, negative_prompt, str(width), str(height),
                     str(num_inference_steps), str(guidance_scale), seed_str, model_id,
                     str(use_refiner), str(use_cpu_offload), str(max_sequence_length), true_cfg_scale_str],
                    capture_output=True,
                    text=True,
                    timeout=timeout
                )
                
                logger.info(f"Generation completed: returncode={result.returncode}")
                
                if result.returncode == 0:
                    output = result.stdout.strip()
                    try:
                        return json.loads(output)
                    except json.JSONDecodeError:
                        return {
                            'success': False,
                            'error': 'Failed to parse generation result'
                        }
                else:
                    error_msg = result.stderr or result.stdout or 'Unknown error'
                    return {
                        'success': False,
                        'error': error_msg[:500]
                    }
            finally:
                # Clean up temp script
                try:
                    os.unlink(script_path)
                except:
                    pass
                    
        except subprocess.TimeoutExpired:
            logger.error(f"Image generation timeout after {timeout} seconds")
            return {
                'success': False,
                'error': f'Generation timeout (exceeded {timeout//60} minutes). The model may still be downloading on first use, or the generation is taking too long. Try reducing steps or image size.'
            }
        except Exception as e:
            logger.error(f"Subprocess error: {e}", exc_info=True)
            return {
                'success': False,
                'error': str(e)
            }
    
    def get_status(self) -> Dict[str, Any]:
        """Get service status"""
        env_status = self.env_mgr.get_status()
        
        return {
            'environment_ready': env_status['status'] == 'ready',
            'packages_installed': env_status['all_required_installed'],
            'env_status': env_status
        }


def get_text_to_image_service() -> TextToImageService:
    """Get singleton instance"""
    if not hasattr(get_text_to_image_service, '_instance'):
        get_text_to_image_service._instance = TextToImageService()
    return get_text_to_image_service._instance
