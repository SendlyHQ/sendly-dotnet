using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sendly.Models;

/// <summary>
/// Represents an SMS message.
/// </summary>
public class Message
{
    /// <summary>
    /// Message status constants.
    /// </summary>
    public static class Statuses
    {
        public const string Queued = "queued";
        public const string Sent = "sent";
        public const string Delivered = "delivered";
        public const string Failed = "failed";
    }

    /// <summary>
    /// Message direction constants.
    /// </summary>
    public static class Directions
    {
        public const string Outbound = "outbound";
        public const string Inbound = "inbound";
    }

    /// <summary>
    /// Sender type constants.
    /// </summary>
    public static class SenderTypes
    {
        public const string User = "user";
        public const string Api = "api";
        public const string System = "system";
        public const string Campaign = "campaign";
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
    /// Sender phone number or ID.
    /// </summary>
    [JsonPropertyName("from")]
    public string? From { get; set; }

    /// <summary>
    /// Message content.
    /// </summary>
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Delivery status.
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Message direction (outbound or inbound).
    /// </summary>
    [JsonPropertyName("direction")]
    public string Direction { get; set; } = Directions.Outbound;

    /// <summary>
    /// Number of SMS segments.
    /// </summary>
    [JsonPropertyName("segments")]
    public int Segments { get; set; } = 1;

    /// <summary>
    /// Credits consumed.
    /// </summary>
    [JsonPropertyName("credits_used")]
    public int CreditsUsed { get; set; }

    /// <summary>
    /// Whether this is a sandbox message.
    /// </summary>
    [JsonPropertyName("is_sandbox")]
    public bool IsSandbox { get; set; }

    /// <summary>
    /// Type of sender (user, api, system, campaign).
    /// </summary>
    [JsonPropertyName("sender_type")]
    public string? SenderType { get; set; }

    /// <summary>
    /// Telnyx message ID for carrier tracking.
    /// </summary>
    [JsonPropertyName("telnyx_message_id")]
    public string? TelnyxMessageId { get; set; }

    /// <summary>
    /// Warning message if any.
    /// </summary>
    [JsonPropertyName("warning")]
    public string? Warning { get; set; }

    /// <summary>
    /// Optional note from the sender.
    /// </summary>
    [JsonPropertyName("sender_note")]
    public string? SenderNote { get; set; }

    /// <summary>
    /// Creation timestamp.
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last update timestamp.
    /// </summary>
    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Delivery timestamp (if delivered).
    /// </summary>
    [JsonPropertyName("delivered_at")]
    public DateTime? DeliveredAt { get; set; }

    /// <summary>
    /// Error code (if failed).
    /// </summary>
    [JsonPropertyName("error_code")]
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Error message (if failed).
    /// </summary>
    [JsonPropertyName("error_message")]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Custom metadata attached to the message.
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, object>? Metadata { get; set; }

    /// <summary>
    /// Whether the message was delivered.
    /// </summary>
    public bool IsDelivered => Status == Statuses.Delivered;

    /// <summary>
    /// Whether the message failed.
    /// </summary>
    public bool IsFailed => Status == Statuses.Failed;

    /// <summary>
    /// Whether the message is pending.
    /// </summary>
    public bool IsPending => Status is Statuses.Queued or Statuses.Sent;

    /// <summary>
    /// Creates a Message from a JSON element.
    /// </summary>
    internal static Message FromJson(JsonElement element, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<Message>(element.GetRawText(), options)
            ?? new Message();
    }
}
