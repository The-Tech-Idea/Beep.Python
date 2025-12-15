"""
Curated Models for AI Services

Provides a curated list of well-known, tested models for each AI service type.
These models are guaranteed to work with the service and are recommended for users.
"""
from typing import Dict, List, Any
from dataclasses import dataclass


@dataclass
class CuratedModel:
    """A curated model recommendation"""
    model_id: str
    name: str
    description: str
    size_gb: float
    source: str = "huggingface"
    recommended: bool = True
    tags: List[str] = None
    
    def __post_init__(self):
        if self.tags is None:
            self.tags = []


class AIServiceCuratedModels:
    """Curated model lists for each AI service type"""
    
    TEXT_TO_IMAGE_MODELS = [
        CuratedModel(
            model_id="stabilityai/stable-diffusion-3-medium-diffusers",
            name="Stable Diffusion 3 Medium",
            description="Top choice for quality and ecosystem. High quality, strong typography, large ecosystem, flexible. Best overall balance of quality and performance. ⚠️ Gated model: Requires accepting license on HuggingFace and API token.",
            size_gb=5.0,
            recommended=True,
            tags=["sd3", "high-quality", "typography", "recommended", "gated"]
        ),
        CuratedModel(
            model_id="DeepFloyd/IF-I-XL-v1.0",
            name="DeepFloyd IF",
            description="Pixel-based model with excellent photorealism and text rendering. Great for understanding complex prompts and rendering readable text in images.",
            size_gb=7.0,
            recommended=True,
            tags=["deepfloyd", "photorealism", "text-rendering", "recommended"]
        ),
        CuratedModel(
            model_id="stabilityai/stable-diffusion-xl-base-1.0",
            name="Stable Diffusion XL (SDXL)",
            description="Evolution of SD with significantly better image quality. Great foundation for fine-tuning and LoRAs. Excellent for artistic control with ControlNet support.",
            size_gb=6.9,
            recommended=True,
            tags=["sdxl", "high-quality", "artistic", "controlnet", "recommended"]
        ),
        CuratedModel(
            model_id="black-forest-labs/FLUX.1-pro",
            name="FLUX.1.1 Pro",
            description="Focuses on speed and high resolution (2K). Ideal for rapid iteration. Excellent for fast generation with high-quality results.",
            size_gb=24.0,
            tags=["flux", "fast", "2k", "high-resolution"]
        ),
        CuratedModel(
            model_id="runwayml/stable-diffusion-v1-5",
            name="Stable Diffusion 1.5",
            description="Perfect for beginners. Fast, reliable, widely supported with extensive tutorials and LoRAs. Great starting point with vast resources available.",
            size_gb=4.0,
            tags=["sd1.5", "beginner-friendly", "fast", "extensive-resources"]
        ),
        CuratedModel(
            model_id="prompthero/openjourney-v4",
            name="OpenJourney v4",
            description="Fine-tuned to create art in the Midjourney aesthetic. Popular for artistic, stylized images with that distinctive Midjourney look.",
            size_gb=4.0,
            tags=["openjourney", "midjourney-style", "artistic"]
        ),
        CuratedModel(
            model_id="black-forest-labs/FLUX.1-dev",
            name="FLUX.1 [dev]",
            description="State-of-the-art image generation. Excellent prompt following and image quality. Development version with latest features.",
            size_gb=23.0,
            tags=["flux", "high-quality", "latest", "dev"]
        ),
        CuratedModel(
            model_id="Owen777/UltraFlux-v1",
            name="UltraFlux v1",
            description="Ultra-high resolution (up to 4096x4096). Perfect for detailed, large images. Very fast inference for ultra-HD generation.",
            size_gb=12.0,
            tags=["ultraflux", "ultra-hd", "4096x4096", "fast"]
        ),
    ]
    
    SPEECH_TO_TEXT_MODELS = [
        # Top Models for General Use & Accuracy
        CuratedModel(
            model_id="openai/whisper-large-v3",
            name="Whisper Large v3",
            description="Excellent multilingual performance across 99+ languages. Industry standard for accuracy. Best for production use with high accuracy requirements.",
            size_gb=3.0,
            recommended=True,
            tags=["whisper", "high-accuracy", "multilingual", "99-languages", "recommended"]
        ),
        CuratedModel(
            model_id="nvidia/Canary-1.1B",
            name="Canary Qwen 2.5B (NVIDIA)",
            description="Extremely high English accuracy, released in 2025. Apache 2.0 license. Best for English-only transcription with maximum accuracy.",
            size_gb=2.5,
            recommended=True,
            tags=["canary", "nvidia", "english", "high-accuracy", "2025", "recommended"]
        ),
        CuratedModel(
            model_id="ibm-granite/granite-speech-3.3-en",
            name="IBM Granite Speech 3.3",
            description="Enterprise-grade English STT with translation support. Known for low WER (Word Error Rate). Excellent for professional transcription.",
            size_gb=1.5,
            recommended=True,
            tags=["granite", "ibm", "enterprise", "low-wer", "translation", "recommended"]
        ),
        # Models for Speed & Efficiency
        CuratedModel(
            model_id="distil-whisper/distil-large-v3",
            name="Distil-Whisper Large v3",
            description="Faster version of Whisper Large v3. 6x faster inference with minimal accuracy loss. Ideal for high-throughput needs.",
            size_gb=1.5,
            tags=["distil-whisper", "fast", "high-throughput", "speed"]
        ),
        CuratedModel(
            model_id="alphacep/vosk-model-en-us-0.22",
            name="Vosk (English)",
            description="Lightweight toolkit based on Kaldi. Ideal for mobile and edge devices. Fast, efficient, and perfect for resource-constrained environments.",
            size_gb=0.2,
            tags=["vosk", "lightweight", "mobile", "edge", "kaldi"]
        ),
        # Specialized & Framework Tools
        CuratedModel(
            model_id="m-bain/whisperx",
            name="WhisperX",
            description="Extends Whisper with word-level timestamps and speaker diarization. Perfect for meeting transcription and multi-speaker scenarios.",
            size_gb=3.0,
            tags=["whisperx", "timestamps", "speaker-diarization", "meetings"]
        ),
        CuratedModel(
            model_id="facebook/wav2vec2-base-960h",
            name="Wav2Vec2 Base",
            description="Meta AI's self-supervised learning model. Strong for fine-tuning. Good foundation for custom STT models.",
            size_gb=0.3,
            tags=["wav2vec", "meta", "self-supervised", "fine-tuning"]
        ),
        CuratedModel(
            model_id="nvidia/nemo-asr",
            name="NVIDIA NeMo ASR",
            description="Collection of PyTorch-based models (Conformer, Citrinet) for various architectures. Flexible framework for different use cases.",
            size_gb=1.0,
            tags=["nemo", "nvidia", "pytorch", "conformer", "citrinet", "framework"]
        ),
        # Additional Whisper variants for different needs
        CuratedModel(
            model_id="openai/whisper-medium",
            name="Whisper Medium",
            description="Good balance of speed and accuracy. Recommended for general use when Large v3 is too slow or resource-intensive.",
            size_gb=0.8,
            tags=["whisper", "balanced", "general-use"]
        ),
        CuratedModel(
            model_id="openai/whisper-base",
            name="Whisper Base",
            description="Fast and lightweight. Good for quick transcriptions when accuracy requirements are moderate.",
            size_gb=0.3,
            tags=["whisper", "fast", "lightweight"]
        ),
    ]
    
    TEXT_TO_SPEECH_MODELS = [
        # Top Models for Quality & Voice Cloning
        CuratedModel(
            model_id="coqui/XTTS-v2",
            name="XTTS v2 (Coqui)",
            description="Enhanced voice cloning with just 6-second audio clip. Supports 17+ languages including English, Spanish, French, German. High-quality multilingual TTS with excellent voice similarity.",
            size_gb=1.7,
            recommended=True,
            tags=["xtts", "voice-cloning", "multilingual", "17-languages", "recommended"]
        ),
        CuratedModel(
            model_id="suno/bark",
            name="Bark (Suno AI)",
            description="Highly expressive TTS that can generate speech, music, sound effects, and non-verbal sounds. Excellent for creative applications and expressive voice synthesis.",
            size_gb=1.0,
            recommended=True,
            tags=["bark", "expressive", "music", "sound-effects", "creative", "recommended"]
        ),
        CuratedModel(
            model_id="microsoft/speecht5_tts",
            name="SpeechT5 (Microsoft)",
            description="Neural TTS with natural-sounding speech and good prosody. Well-balanced for general use. Good quality and speed balance.",
            size_gb=0.5,
            recommended=True,
            tags=["speecht5", "microsoft", "natural", "balanced", "recommended"]
        ),
        # Multilingual & Massively Multilingual
        CuratedModel(
            model_id="facebook/mms-tts",
            name="MMS TTS (Meta)",
            description="Massively Multilingual Speech supporting 1100+ languages. Best for applications requiring support for many languages. Excellent for global applications.",
            size_gb=2.0,
            tags=["mms", "multilingual", "1100-languages", "meta", "global"]
        ),
        # Fast & Efficient Models
        CuratedModel(
            model_id="rhasspy/piper",
            name="Piper TTS",
            description="Fast, lightweight, and efficient TTS. Perfect for real-time applications and resource-constrained environments. Low latency with good quality.",
            size_gb=0.1,
            tags=["piper", "fast", "lightweight", "real-time", "low-latency"]
        ),
        CuratedModel(
            model_id="microsoft/edge-tts",
            name="Edge-TTS (Microsoft)",
            description="Microsoft's cloud-based TTS with many voices. Free to use, supports multiple languages and voices. Good for web applications.",
            size_gb=0.0,
            tags=["edge-tts", "microsoft", "cloud", "many-voices", "web"]
        ),
        # Advanced & Research Models
        CuratedModel(
            model_id="facebook/voxpopuli",
            name="VoxPopuli",
            description="Large-scale multilingual TTS trained on 23 European languages. Good for European language support with high quality.",
            size_gb=1.5,
            tags=["voxpopuli", "multilingual", "european", "23-languages"]
        ),
        CuratedModel(
            model_id="espeak-ng/espeak-ng",
            name="eSpeak NG",
            description="Compact, open-source TTS supporting 100+ languages. Very lightweight and fast. Good for basic TTS needs and embedded systems.",
            size_gb=0.05,
            tags=["espeak", "lightweight", "100-languages", "embedded", "compact"]
        ),
    ]
    
    OBJECT_DETECTION_MODELS = [
        # Latest & Best Performance Models
        CuratedModel(
            model_id="ultralytics/yolov12",
            name="YOLOv12",
            description="Latest YOLO model (Feb 2025) with attention mechanisms. YOLOv12-N achieves 40.6% mAP with 1.64ms latency on T4 GPU. Outperforms YOLOv10-N and YOLOv11-N. Best overall performance.",
            size_gb=0.3,
            recommended=True,
            tags=["yolov12", "latest", "2025", "attention", "high-accuracy", "recommended"]
        ),
        CuratedModel(
            model_id="THU-MIG/yolov10",
            name="YOLOv10",
            description="End-to-end detection without NMS. YOLOv10-S is 1.8x faster than RT-DETR-R18 with similar accuracy. Consistent dual assignment approach for reduced latency.",
            size_gb=0.25,
            recommended=True,
            tags=["yolov10", "end-to-end", "no-nms", "fast", "recommended"]
        ),
        CuratedModel(
            model_id="WongKinYiu/yolov9",
            name="YOLOv9",
            description="Programmable Gradient Information (PGI) and GELAN architecture. YOLOv9-E achieves 55.6% mAP at 102 FPS. Excellent for real-time high-accuracy detection.",
            size_gb=0.4,
            recommended=True,
            tags=["yolov9", "pgi", "gelan", "high-accuracy", "real-time", "recommended"]
        ),
        # Real-Time & Speed Optimized
        CuratedModel(
            model_id="ultralytics/yolov11",
            name="YOLOv11",
            description="Enhanced feature extraction with 22% fewer parameters than YOLOv8m. Better accuracy with improved efficiency. Good balance for production use.",
            size_gb=0.3,
            tags=["yolov11", "efficient", "ultralytics", "balanced"]
        ),
        CuratedModel(
            model_id="ultralytics/yolov8n",
            name="YOLOv8 Nano",
            description="Ultra-fast object detection. Perfect for real-time applications and edge devices. Minimal resource requirements.",
            size_gb=0.1,
            tags=["yolov8", "nano", "fast", "real-time", "edge"]
        ),
        CuratedModel(
            model_id="ultralytics/yolov8s",
            name="YOLOv8 Small",
            description="Better accuracy than nano while maintaining speed. Good balance for general object detection tasks.",
            size_gb=0.2,
            tags=["yolov8", "small", "balanced", "general-use"]
        ),
        # Transformer-Based Models
        CuratedModel(
            model_id="PaddlePaddle/rt-detr",
            name="RT-DETRv3",
            description="Real-time end-to-end transformer detector. RT-DETRv3-R18 achieves 48.1% AP with hierarchical dense positive supervision. Best transformer-based real-time detector.",
            size_gb=0.35,
            tags=["rt-detr", "transformer", "real-time", "end-to-end"]
        ),
        CuratedModel(
            model_id="facebook/detr-resnet-50",
            name="DETR ResNet-50",
            description="Facebook's Detection Transformer. Good balance of speed and accuracy. Classic transformer-based detection model.",
            size_gb=0.2,
            tags=["detr", "transformer", "balanced", "facebook"]
        ),
        CuratedModel(
            model_id="facebook/detr-resnet-101",
            name="DETR ResNet-101",
            description="Higher accuracy DETR model. Better for complex scenes requiring detailed detection.",
            size_gb=0.3,
            tags=["detr", "transformer", "high-accuracy", "complex-scenes"]
        ),
        # Specialized Models
        CuratedModel(
            model_id="skywork/yolo-world",
            name="YOLO-World",
            description="Open-vocabulary detection. Identify arbitrary objects described in natural language without retraining. Achieves 35.4% zero-shot AP. Perfect for flexible object detection.",
            size_gb=0.5,
            tags=["yolo-world", "open-vocabulary", "zero-shot", "flexible"]
        ),
    ]
    
    TABULAR_TIME_SERIES_MODELS = [
        # Top Models for Accuracy & Performance
        CuratedModel(
            model_id="autogluon/chronos-bolt-base",
            name="Chronos-Bolt Base",
            description="Best accuracy Chronos-Bolt model. 250x faster than original Chronos with better accuracy. Trained on 100B+ time series observations. Recommended for production forecasting.",
            size_gb=1.2,
            recommended=True,
            tags=["chronos-bolt", "autogluon", "high-accuracy", "ultra-fast", "recommended"]
        ),
        CuratedModel(
            model_id="google/timesfm-2.5-200m-pytorch",
            name="TimesFM 2.5 (200M)",
            description="Google's improved time series foundation model with better accuracy. Excellent for general forecasting tasks. Recommended for most use cases.",
            size_gb=0.8,
            recommended=True,
            tags=["timesfm", "google", "foundation-model", "recommended"]
        ),
        CuratedModel(
            model_id="amazon/chronos-t5-base",
            name="Chronos T5 Base",
            description="Amazon's zero-shot time series forecasting. Better accuracy than small variant. Good for production use with excellent zero-shot performance.",
            size_gb=0.8,
            recommended=True,
            tags=["chronos", "amazon", "zero-shot", "production", "recommended"]
        ),
        # Fast & Efficient Models
        CuratedModel(
            model_id="autogluon/chronos-bolt-small",
            name="Chronos-Bolt Small",
            description="Ultra-fast Chronos variant. 250x faster than original Chronos with better accuracy. Perfect for rapid iteration and real-time forecasting.",
            size_gb=0.5,
            tags=["chronos-bolt", "autogluon", "ultra-fast", "real-time"]
        ),
        CuratedModel(
            model_id="autogluon/chronos-bolt-mini",
            name="Chronos-Bolt Mini",
            description="Smaller, faster variant. Good for resource-constrained environments while maintaining good accuracy.",
            size_gb=0.3,
            tags=["chronos-bolt", "autogluon", "mini", "lightweight"]
        ),
        CuratedModel(
            model_id="amazon/chronos-t5-small",
            name="Chronos T5 Small",
            description="Amazon's zero-shot time series forecasting. Fast and efficient. Good for quick forecasts and testing.",
            size_gb=0.3,
            tags=["chronos", "amazon", "zero-shot", "fast"]
        ),
        # Foundation Models
        CuratedModel(
            model_id="google/timesfm-1.0-200m",
            name="TimesFM 1.0 (200M)",
            description="Google's time series foundation model. Good for general forecasting tasks. Foundation for understanding time series patterns.",
            size_gb=0.8,
            tags=["timesfm", "google", "foundation-model", "general"]
        ),
        CuratedModel(
            model_id="google/timesfm-2.0-500m-pytorch",
            name="TimesFM 2.0 (500M)",
            description="Larger TimesFM model with improved capacity. Better for complex time series with longer contexts.",
            size_gb=2.0,
            tags=["timesfm", "google", "large", "complex"]
        ),
        # IBM Granite Models
        CuratedModel(
            model_id="ibm-granite/granite-timeseries-ttm-r1",
            name="Granite TTM R1",
            description="IBM's TinyTimeMixer model. Efficient for resource-constrained environments. Good balance of speed and accuracy.",
            size_gb=0.2,
            tags=["granite", "ibm", "ttm", "efficient"]
        ),
        CuratedModel(
            model_id="ibm-granite/granite-timeseries-ttm-r2",
            name="Granite TTM R2",
            description="Improved version of Granite TTM. Better accuracy with similar efficiency. Enhanced for production use.",
            size_gb=0.25,
            tags=["granite", "ibm", "ttm", "improved"]
        ),
    ]
    
    @classmethod
    def get_curated_models(cls, service_type: str) -> List[CuratedModel]:
        """Get curated models for a service type"""
        service_map = {
            'text_to_image': cls.TEXT_TO_IMAGE_MODELS,
            'speech_to_text': cls.SPEECH_TO_TEXT_MODELS,
            'text_to_speech': cls.TEXT_TO_SPEECH_MODELS,
            'object_detection': cls.OBJECT_DETECTION_MODELS,
            'tabular_time_series': cls.TABULAR_TIME_SERIES_MODELS,
        }
        return service_map.get(service_type, [])
    
    @classmethod
    def get_curated_models_dict(cls, service_type: str) -> List[Dict[str, Any]]:
        """Get curated models as dictionaries for JSON serialization"""
        models = cls.get_curated_models(service_type)
        return [
            {
                'model_id': m.model_id,
                'name': m.name,
                'description': m.description,
                'size_gb': m.size_gb,
                'source': m.source,
                'recommended': m.recommended,
                'tags': m.tags
            }
            for m in models
        ]
