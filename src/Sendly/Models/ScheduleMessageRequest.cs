using System.Text.Json.Serialization;

namespace Sendly.Models;

/// <summary>
/// Request object for scheduling an SMS message.
/// </summary>
public class ScheduleMessageRequest
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
    /// ISO 8601 datetime for delivery, at least 5 minutes and at most 5 days
    /// in the future; the API refuses any other time with a 400
    /// <c>invalid_scheduled_time</c>.
    /// </summary>
    [JsonPropertyName("scheduledAt")]
    public string ScheduledAt { get; set; }

    /// <summary>
    /// Optional sender ID.
    /// </summary>
    [JsonPropertyName("from")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? From { get; set; }

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
    /// Creates an empty schedule message request, for object-initializer
    /// syntax. Set <see cref="To"/>, <see cref="Text"/> and
    /// <see cref="ScheduledAt"/> before sending it.
    /// </summary>
    public ScheduleMessageRequest()
    {
        To = string.Empty;
        Text = string.Empty;
        ScheduledAt = string.Empty;
    }

    /// <summary>
    /// Creates a new schedule message request.
    /// </summary>
    /// <param name="to">Recipient phone number in E.164 format</param>
    /// <param name="text">Message content</param>
    /// <param name="scheduledAt">ISO 8601 datetime for delivery</param>
    /// <param name="from">Optional sender ID</param>
    /// <param name="messageType">Message type: "marketing" or "transactional"</param>
    /// <param name="metadata">Custom metadata to attach (max 4KB)</param>
    public ScheduleMessageRequest(string to, string text, string scheduledAt, string? from = null, string? messageType = null, Dictionary<string, object>? metadata = null)
    {
        To = to;
        Text = text;
        ScheduledAt = scheduledAt;
        From = from;
        MessageType = messageType;
        Metadata = metadata;
    }
}
