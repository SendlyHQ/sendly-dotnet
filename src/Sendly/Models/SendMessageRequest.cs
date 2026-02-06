using System.Text.Json.Serialization;

namespace Sendly.Models;

/// <summary>
/// Request to send an SMS message.
/// </summary>
public class SendMessageRequest
{
    /// <summary>
    /// Recipient phone number in E.164 format.
    /// </summary>
    [JsonPropertyName("to")]
    public string To { get; set; }

    /// <summary>
    /// Message content (max 1600 characters).
    /// </summary>
    [JsonPropertyName("text")]
    public string Text { get; set; }

    /// <summary>
    /// Message type: "marketing" (default, subject to quiet hours) or "transactional" (24/7).
    /// </summary>
    [JsonPropertyName("messageType")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MessageType { get; set; }

    /// <summary>
    /// Custom metadata to attach to the message (max 4KB).
    /// </summary>
    [JsonPropertyName("metadata")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object>? Metadata { get; set; }

    /// <summary>
    /// Creates a new send message request.
    /// </summary>
    public SendMessageRequest(string to, string text, string? messageType = null, Dictionary<string, object>? metadata = null)
    {
        To = to;
        Text = text;
        MessageType = messageType;
        Metadata = metadata;
    }
}
