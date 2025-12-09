using System.Collections.Generic;

namespace Beep.Python.RuntimeEngine.Infrastructure;

/// <summary>
/// Represents a chat message for conversation history
/// </summary>
public class ChatMessage
{
    /// <summary>
    /// Role of the message sender (e.g., "system", "user", "assistant")
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Content of the message
    /// </summary>
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// Options for text generation
/// </summary>
public class GenerationOptions
{
    /// <summary>
    /// Maximum number of tokens to generate
    /// </summary>
    public int MaxTokens { get; set; } = 10;  // Temporarily reduced for testing

    /// <summary>
    /// Temperature for sampling (0.0 to 2.0)
    /// </summary>
    public double Temperature { get; set; } = 0.7;

    /// <summary>
    /// Top-p (nucleus) sampling parameter
    /// </summary>
    public double TopP { get; set; } = 0.9;

    /// <summary>
    /// Top-k sampling parameter
    /// </summary>
    public int TopK { get; set; } = 50;

    /// <summary>
    /// Repetition penalty
    /// </summary>
    public double RepetitionPenalty { get; set; } = 1.0;

    /// <summary>
    /// Stop sequences to end generation
    /// </summary>
    public List<string>? StopSequences { get; set; }

    /// <summary>
    /// Whether to stream the response
    /// </summary>
    public bool Stream { get; set; }

    /// <summary>
    /// System prompt (if supported by model)
    /// </summary>
    public string? SystemPrompt { get; set; }

    /// <summary>
    /// Conversation history for multi-turn chat
    /// </summary>
    public List<ChatMessage>? ConversationHistory { get; set; }

    /// <summary>
    /// Additional provider-specific options
    /// </summary>
    public Dictionary<string, object>? AdditionalOptions { get; set; }
}
