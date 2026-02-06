using System.Text.Json.Serialization;

namespace Sendly.Models;

/// <summary>
/// Represents a single message in a batch send request.
/// </summary>
public class BatchMessageItem
{
    /// <summary>
    /// Recipient phone number in E.164 format.
    /// </summary>
    [JsonPropertyName("to")]
    public string To { get; set; }

    /// <summary>
    /// Message content.
    /// </summary>
    [JsonPropertyName("text")]
    public string Text { get; set; }

    /// <summary>
    /// Per-message metadata (max 4KB, merged with batch metadata).
    /// </summary>
    [JsonPropertyName("metadata")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object>? Metadata { get; set; }

    /// <summary>
    /// Creates a new batch message item.
    /// </summary>
    /// <param name="to">Recipient phone number in E.164 format</param>
    /// <param name="text">Message content</param>
    /// <param name="metadata">Per-message metadata (max 4KB)</param>
    public BatchMessageItem(string to, string text, Dictionary<string, object>? metadata = null)
    {
        To = to;
        Text = text;
        Metadata = metadata;
    }
}
