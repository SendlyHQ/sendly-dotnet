using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sendly.Models;

/// <summary>
/// Represents a scheduled SMS message.
/// </summary>
public class ScheduledMessage
{
    /// <summary>
    /// Scheduled message status constants.
    /// </summary>
    public static class Statuses
    {
        public const string Scheduled = "scheduled";
        public const string Sent = "sent";
        public const string Cancelled = "cancelled";
        public const string Failed = "failed";
    }

    /// <summary>
    /// Unique message identifier.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Recipient phone number in E.164 format.
    /// </summary>
    [JsonPropertyName("to")]
    public string To { get; set; } = string.Empty;

    /// <summary>
    /// Message content.
    /// </summary>
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Sender ID (optional).
    /// </summary>
    [JsonPropertyName("from")]
    public string? From { get; set; }

    /// <summary>
    /// Current status.
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Scheduled delivery time. When quiet hours moved the message, this is
    /// the new time.
    /// </summary>
    [JsonPropertyName("scheduledAt")]
    public DateTime ScheduledAt { get; set; }

    /// <summary>
    /// Time zone the message was scheduled in (for example <c>UTC</c>).
    /// </summary>
    [JsonPropertyName("timezone")]
    public string? Timezone { get; set; }

    /// <summary>
    /// Credits reserved for this message.
    /// </summary>
    [JsonPropertyName("creditsReserved")]
    public int CreditsReserved { get; set; }

    /// <summary>
    /// Number of SMS segments.
    /// </summary>
    [JsonPropertyName("segments")]
    public int? Segments { get; set; }

    /// <summary>
    /// Sender type: <c>number_pool</c> for US and Canadian recipients,
    /// <c>alphanumeric</c> for everyone else. See <see cref="Message.SenderTypes"/>.
    /// </summary>
    [JsonPropertyName("senderType")]
    public string? SenderType { get; set; }

    /// <summary>
    /// Creation timestamp.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Cancellation timestamp (if cancelled). Returned by
    /// <c>GetScheduledAsync</c>; the schedule and list responses leave it null.
    /// </summary>
    [JsonPropertyName("cancelledAt")]
    public DateTime? CancelledAt { get; set; }

    /// <summary>
    /// Sent timestamp (if sent). Returned by <c>GetScheduledAsync</c>; the
    /// schedule and list responses leave it null.
    /// </summary>
    [JsonPropertyName("sentAt")]
    public DateTime? SentAt { get; set; }

    /// <summary>
    /// Custom metadata attached when the message was scheduled. Returned by
    /// <c>ListScheduledAsync</c>; the other responses leave it null.
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, object>? Metadata { get; set; }

    /// <summary>
    /// Error message (if failed).
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    /// <summary>
    /// Whether the message is still scheduled (pending delivery).
    /// </summary>
    public bool IsScheduled => Status == Statuses.Scheduled;

    /// <summary>
    /// Whether the message was sent.
    /// </summary>
    public bool IsSent => Status == Statuses.Sent;

    /// <summary>
    /// Whether the message was cancelled.
    /// </summary>
    public bool IsCancelled => Status == Statuses.Cancelled;

    /// <summary>
    /// Whether the message failed.
    /// </summary>
    public bool IsFailed => Status == Statuses.Failed;

    /// <summary>
    /// Creates a ScheduledMessage from a JSON element.
    /// </summary>
    internal static ScheduledMessage FromJson(JsonElement element, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<ScheduledMessage>(element.GetRawText(), options)
            ?? new ScheduledMessage();
    }
}
