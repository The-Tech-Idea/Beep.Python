using System;
using System.Collections.Generic;
using Beep.Python.Model;

namespace Beep.Python.AI.Transformers
{
    #region Multimodal Enums

    /// <summary>
    /// Types of multimodal transformer tasks
    /// </summary>
    public enum MultimodalTask
    {
        /// <summary>
        /// Generate images from text descriptions
        /// </summary>
        TextToImage,

        /// <summary>
        /// Generate audio/speech from text
        /// </summary>
        TextToAudio,

        /// <summary>
        /// Generate music from text descriptions
        /// </summary>
        TextToMusic,

        /// <summary>
        /// Generate text descriptions from images
        /// </summary>
        ImageToText,

        /// <summary>
        /// Answer questions about images
        /// </summary>
        VisualQuestionAnswering,

        /// <summary>
        /// Convert speech/audio to text
        /// </summary>
        AudioToText,

        /// <summary>
        /// Classify audio content
        /// </summary>
        AudioClassification,

        /// <summary>
        /// Standard text generation
        /// </summary>
        TextGeneration,

        /// <summary>
        /// Create complete multimedia stories
        /// </summary>
        MultimediaStoryGeneration,

        /// <summary>
        /// Create multimedia presentations
        /// </summary>
        PresentationGeneration,

        /// <summary>
        /// Convert text to video
        /// </summary>
        TextToVideo,

        /// <summary>
        /// Generate text from video content
        /// </summary>
        VideoToText,

        /// <summary>
        /// Interactive multimedia chat
        /// </summary>
        MultimodalChat
    }

    #endregion

    #region Configuration Classes

    /// <summary>
    /// Configuration for multimodal pipeline
    /// </summary>
    public class MultimodalPipelineConfig
    {
        /// <summary>
        /// Global configuration settings applied to all pipelines
        /// </summary>
        public Dictionary<string, object>? GlobalConfig { get; set; }

        /// <summary>
        /// List of pipelines to preload during initialization
        /// </summary>
        public List<MultimodalTask>? PreloadPipelines { get; set; }

        /// <summary>
        /// Maximum concurrent tasks
        /// </summary>
        public int MaxConcurrentTasks { get; set; } = 3;

        /// <summary>
        /// Enable caching for frequently used models
        /// </summary>
        public bool EnableCaching { get; set; } = true;

        /// <summary>
        /// Default quality settings
        /// </summary>
        public string DefaultQuality { get; set; } = "high";

        /// <summary>
        /// Default device for processing
        /// </summary>
        public string DefaultDevice { get; set; } = "auto";
    }

    #endregion

    #region Parameter Classes

    /// <summary>
    /// Parameters for text-to-image generation
    /// </summary>
    public class TextToImageParameters
    {
        /// <summary>
        /// Image width in pixels
        /// </summary>
        public int Width { get; set; } = 512;

        /// <summary>
        /// Image height in pixels
        /// </summary>
        public int Height { get; set; } = 512;

        /// <summary>
        /// Number of images to generate
        /// </summary>
        public int NumImages { get; set; } = 1;

        /// <summary>
        /// Artistic style to apply
        /// </summary>
        public string Style { get; set; } = "realistic";

        /// <summary>
        /// Image quality (low, medium, high, ultra)
        /// </summary>
        public string Quality { get; set; } = "high";

        /// <summary>
        /// Guidance scale for prompt adherence
        /// </summary>
        public float GuidanceScale { get; set; } = 7.5f;

        /// <summary>
        /// Number of inference steps
        /// </summary>
        public int NumInferenceSteps { get; set; } = 50;

        /// <summary>
        /// Random seed for reproducibility
        /// </summary>
        public int? Seed { get; set; }

        /// <summary>
        /// Negative prompt (what to avoid)
        /// </summary>
        public string? NegativePrompt { get; set; }
    }

    /// <summary>
    /// Parameters for text-to-audio generation
    /// </summary>
    public class TextToAudioParameters
    {
        /// <summary>
        /// Voice type/style
        /// </summary>
        public string Voice { get; set; } = "neutral";

        /// <summary>
        /// Speaking speed (0.5 to 2.0)
        /// </summary>
        public float Speed { get; set; } = 1.0f;

        /// <summary>
        /// Audio quality
        /// </summary>
        public string Quality { get; set; } = "high";

        /// <summary>
        /// Output format (wav, mp3, etc.)
        /// </summary>
        public string Format { get; set; } = "wav";

        /// <summary>
        /// Sample rate
        /// </summary>
        public int SampleRate { get; set; } = 22050;

        /// <summary>
        /// Emotion/tone to apply
        /// </summary>
        public string? Emotion { get; set; }
    }

    /// <summary>
    /// Parameters for music generation
    /// </summary>
    public class MusicGenerationParameters
    {
        /// <summary>
        /// Duration in seconds
        /// </summary>
        public float Duration { get; set; } = 30.0f;

        /// <summary>
        /// Musical genre
        /// </summary>
        public string Genre { get; set; } = "ambient";

        /// <summary>
        /// Tempo/BPM
        /// </summary>
        public int? Tempo { get; set; }

        /// <summary>
        /// Musical key
        /// </summary>
        public string? Key { get; set; }

        /// <summary>
        /// Instruments to include
        /// </summary>
        public List<string>? Instruments { get; set; }

        /// <summary>
        /// Mood/emotion
        /// </summary>
        public string Mood { get; set; } = "neutral";
    }

    /// <summary>
    /// Parameters for image-to-text generation
    /// </summary>
    public class ImageToTextParameters
    {
        /// <summary>
        /// Maximum caption length
        /// </summary>
        public int MaxLength { get; set; } = 100;

        /// <summary>
        /// Caption style (descriptive, creative, technical)
        /// </summary>
        public string Style { get; set; } = "descriptive";

        /// <summary>
        /// Include detailed analysis
        /// </summary>
        public bool DetailedAnalysis { get; set; } = false;

        /// <summary>
        /// Focus areas for description
        /// </summary>
        public List<string>? FocusAreas { get; set; }
    }

    /// <summary>
    /// Parameters for visual question answering
    /// </summary>
    public class VisualQAParameters
    {
        /// <summary>
        /// Maximum answer length
        /// </summary>
        public int MaxAnswerLength { get; set; } = 50;

        /// <summary>
        /// Answer style (brief, detailed, technical)
        /// </summary>
        public string AnswerStyle { get; set; } = "brief";

        /// <summary>
        /// Include confidence score
        /// </summary>
        public bool IncludeConfidence { get; set; } = true;
    }

    /// <summary>
    /// Parameters for speech-to-text conversion
    /// </summary>
    public class SpeechToTextParameters
    {
        /// <summary>
        /// Expected language (auto-detect if null)
        /// </summary>
        public string? Language { get; set; }

        /// <summary>
        /// Include timestamps
        /// </summary>
        public bool IncludeTimestamps { get; set; } = false;

        /// <summary>
        /// Include speaker identification
        /// </summary>
        public bool IncludeSpeakerID { get; set; } = false;

        /// <summary>
        /// Audio quality hint
        /// </summary>
        public string AudioQuality { get; set; } = "auto";
    }

    /// <summary>
    /// Parameters for audio classification
    /// </summary>
    public class AudioClassificationParameters
    {
        /// <summary>
        /// Classification categories
        /// </summary>
        public List<string>? Categories { get; set; }

        /// <summary>
        /// Return top K results
        /// </summary>
        public int TopK { get; set; } = 5;

        /// <summary>
        /// Include confidence scores
        /// </summary>
        public bool IncludeScores { get; set; } = true;
    }

    /// <summary>
    /// Parameters for story generation
    /// </summary>
    public class StoryGenerationParameters
    {
        /// <summary>
        /// Maximum words in story
        /// </summary>
        public int MaxWords { get; set; } = 500;

        /// <summary>
        /// Story genre
        /// </summary>
        public string Genre { get; set; } = "general";

        /// <summary>
        /// Target audience
        /// </summary>
        public string Audience { get; set; } = "general";

        /// <summary>
        /// Image style for scenes
        /// </summary>
        public string ImageStyle { get; set; } = "cinematic";

        /// <summary>
        /// Image quality
        /// </summary>
        public string ImageQuality { get; set; } = "high";

        /// <summary>
        /// Narration voice
        /// </summary>
        public string NarrationVoice { get; set; } = "neutral";

        /// <summary>
        /// Narration speed
        /// </summary>
        public float NarrationSpeed { get; set; } = 1.0f;

        /// <summary>
        /// Number of scenes to illustrate
        /// </summary>
        public int MaxScenes { get; set; } = 5;
    }

    /// <summary>
    /// Parameters for presentation generation
    /// </summary>
    public class PresentationGenerationParameters
    {
        /// <summary>
        /// Number of slides
        /// </summary>
        public int NumSlides { get; set; } = 7;

        /// <summary>
        /// Presentation style
        /// </summary>
        public string Style { get; set; } = "professional";

        /// <summary>
        /// Target audience
        /// </summary>
        public string Audience { get; set; } = "general";

        /// <summary>
        /// Include images for slides
        /// </summary>
        public bool IncludeImages { get; set; } = true;

        /// <summary>
        /// Include narration
        /// </summary>
        public bool IncludeNarration { get; set; } = true;

        /// <summary>
        /// Presentation duration in minutes
        /// </summary>
        public float Duration { get; set; } = 10.0f;
    }

    #endregion

    #region Result Classes

    /// <summary>
    /// Generic result for multimodal tasks
    /// </summary>
    /// <typeparam name="T">Result data type</typeparam>
    public class MultimodalResult<T>
    {
        /// <summary>
        /// Indicates if the task was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// The result data
        /// </summary>
        public T Data { get; set; } = default!;

        /// <summary>
        /// Error message if task failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Task type that was executed
        /// </summary>
        public MultimodalTask TaskType { get; set; }

        /// <summary>
        /// Execution time in milliseconds
        /// </summary>
        public long ExecutionTimeMs { get; set; }

        /// <summary>
        /// Additional metadata
        /// </summary>
        public Dictionary<string, object>? Metadata { get; set; }
    }

    /// <summary>
    /// Result for image generation tasks
    /// </summary>
    public class ImageResult
    {
        /// <summary>
        /// Generated image data (base64 or file path)
        /// </summary>
        public string ImageData { get; set; } = string.Empty;

        /// <summary>
        /// Image format (png, jpg, etc.)
        /// </summary>
        public string Format { get; set; } = "png";

        /// <summary>
        /// Image width
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// Image height
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// Generation metadata
        /// </summary>
        public Dictionary<string, object>? Metadata { get; set; }
    }

    /// <summary>
    /// Result for audio generation tasks
    /// </summary>
    public class AudioResult
    {
        /// <summary>
        /// Generated audio data (base64 or file path)
        /// </summary>
        public string AudioData { get; set; } = string.Empty;

        /// <summary>
        /// Audio format (wav, mp3, etc.)
        /// </summary>
        public string Format { get; set; } = "wav";

        /// <summary>
        /// Duration in seconds
        /// </summary>
        public float Duration { get; set; }

        /// <summary>
        /// Sample rate
        /// </summary>
        public int SampleRate { get; set; }

        /// <summary>
        /// Generation metadata
        /// </summary>
        public Dictionary<string, object>? Metadata { get; set; }
    }

    /// <summary>
    /// Complete multimedia story result
    /// </summary>
    public class MultimediaStory
    {
        /// <summary>
        /// Story text content
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// Generated images for scenes
        /// </summary>
        public List<ImageResult> Images { get; set; } = new();

        /// <summary>
        /// Audio narration
        /// </summary>
        public AudioResult? Audio { get; set; }

        /// <summary>
        /// Story metadata
        /// </summary>
        public Dictionary<string, object>? Metadata { get; set; }
    }

    /// <summary>
    /// Slide content for presentations
    /// </summary>
    public class SlideContent
    {
        /// <summary>
        /// Slide title
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Slide content/text
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Slide image
        /// </summary>
        public ImageResult? Image { get; set; }

        /// <summary>
        /// Slide number
        /// </summary>
        public int SlideNumber { get; set; }
    }

    /// <summary>
    /// Complete multimedia presentation
    /// </summary>
    public class MultimediaPresentation
    {
        /// <summary>
        /// Presentation title
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// List of slides
        /// </summary>
        public List<SlideContent> Slides { get; set; } = new();

        /// <summary>
        /// Presentation narration audio
        /// </summary>
        public AudioResult? Narration { get; set; }

        /// <summary>
        /// Presentation metadata
        /// </summary>
        public Dictionary<string, object>? Metadata { get; set; }
    }

    #endregion

    #region Event Args Classes

    /// <summary>
    /// Event arguments for multimodal task events
    /// </summary>
    public class MultimodalTaskEventArgs : EventArgs
    {
        /// <summary>
        /// The multimodal task
        /// </summary>
        public MultimodalTask Task { get; set; }

        /// <summary>
        /// Task input
        /// </summary>
        public string Input { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp of the event
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Event arguments for multimodal errors
    /// </summary>
    public class MultimodalErrorEventArgs : EventArgs
    {
        /// <summary>
        /// Error message
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Exception details
        /// </summary>
        public Exception? Exception { get; set; }

        /// <summary>
        /// Timestamp of the error
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Event arguments for multimodal progress updates
    /// </summary>
    public class MultimodalProgressEventArgs : EventArgs
    {
        /// <summary>
        /// Progress message
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Current step
        /// </summary>
        public int CurrentStep { get; set; }

        /// <summary>
        /// Total steps
        /// </summary>
        public int TotalSteps { get; set; }

        /// <summary>
        /// Progress percentage (0-100)
        /// </summary>
        public int ProgressPercentage { get; set; }

        /// <summary>
        /// Timestamp of the progress update
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    #endregion
}