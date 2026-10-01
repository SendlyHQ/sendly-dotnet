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

        /// <summary>Recipient read the message. RCS and WhatsApp only — SMS never reports one.</summary>
        public const string Read = "read";

        public const string Failed = "failed";
        public const string Bounced = "bounced";
        public const string Retrying = "retrying";
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
    /// Sender type constants: the values of <see cref="SenderType"/>.
    /// </summary>
    public static class SenderTypes
    {
        /// <summary>Sent from a toll-free number in your number pool (US and Canadian recipients).</summary>
        public const string NumberPool = "number_pool";

        /// <summary>Sent from your alphanumeric sender ID (recipients outside the US and Canada).</summary>
        public const string Alphanumeric = "alphanumeric";

        /// <summary>Sent from a number of yours: the <c>From</c> you passed, or the number your workspace sends from.</summary>
        public const string Explicit = "explicit";

        [Obsolete("The API never sends this value. SenderType is number_pool, alphanumeric or explicit (see NumberPool, Alphanumeric and Explicit).")]
        public const string User = "user";

        [Obsolete("The API never sends this value. SenderType is number_pool, alphanumeric or explicit (see NumberPool, Alphanumeric and Explicit).")]
        public const string Api = "api";

        [Obsolete("The API never sends this value. SenderType is number_pool, alphanumeric or explicit (see NumberPool, Alphanumeric and Explicit).")]
        public const string System = "system";

        [Obsolete("The API never sends this value. SenderType is number_pool, alphanumeric or explicit (see NumberPool, Alphanumeric and Explicit).")]
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
    [JsonPropertyName("creditsUsed")]
    public int CreditsUsed { get; set; }

    /// <summary>
    /// Whether this is a sandbox message. Returned by <c>GetAsync</c> and
    /// <c>ListAsync</c>; a send response reports a simulated send through
    /// <see cref="Simulated"/> instead.
    /// </summary>
    [JsonPropertyName("isSandbox")]
    public bool IsSandbox { get; set; }

    /// <summary>
    /// Which sender the message went out from: <c>number_pool</c>,
    /// <c>alphanumeric</c> or <c>explicit</c> (see <see cref="SenderTypes"/>).
    /// Returned by a live send; null on a simulated send and on messages read
    /// back with <c>GetAsync</c> or <c>ListAsync</c>.
    /// </summary>
    [JsonPropertyName("senderType")]
    public string? SenderType { get; set; }

    /// <summary>
    /// Carrier message ID for tracking.
    /// </summary>
    [JsonPropertyName("telnyx_message_id")]
    public string? TelnyxMessageId { get; set; }

    /// <summary>
    /// Warning message if any.
    /// </summary>
    [JsonPropertyName("warning")]
    public string? Warning { get; set; }

    /// <summary>
    /// A note on which sender a live send used, when there is one to add.
    /// </summary>
    [JsonPropertyName("senderNote")]
    public string? SenderNote { get; set; }

    /// <summary>
    /// True when a send was simulated rather than delivered to a handset:
    /// with a test key or to a sandbox number, and with a live key when the
    /// account is not set up to send to the destination yet. Null on a real
    /// send and on messages read back with <c>GetAsync</c> or <c>ListAsync</c>
    /// (see <see cref="IsSandbox"/>).
    /// </summary>
    [JsonPropertyName("simulated")]
    public bool? Simulated { get; set; }

    /// <summary>
    /// Why a live-key send was simulated instead of delivered, when it was.
    /// </summary>
    [JsonPropertyName("simulatedReason")]
    public string? SimulatedReason { get; set; }

    /// <summary>
    /// Creation timestamp.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last update timestamp. The API does not send one, so this stays at
    /// its default.
    /// </summary>
    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Delivery timestamp (if delivered).
    /// </summary>
    [JsonPropertyName("deliveredAt")]
    public DateTime? DeliveredAt { get; set; }

    /// <summary>
    /// Error code (if failed).
    /// </summary>
    [JsonPropertyName("errorCode")]
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Error message (if failed): the API's <c>error</c> field.
    /// </summary>
    [JsonPropertyName("error")]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Number of delivery retry attempts.
    /// </summary>
    [JsonPropertyName("retryCount")]
    public int RetryCount { get; set; }

    /// <summary>
    /// Custom metadata attached to the message.
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, object>? Metadata { get; set; }

    /// <summary>
    /// AI classification metadata for inbound messages.
    /// </summary>
    [JsonPropertyName("aiMetadata")]
    public AiMetadata? AiMetadata { get; set; }

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

/// <summary>
/// AI classification metadata for inbound messages.
/// </summary>
public class AiMetadata
{
    [JsonPropertyName("intent")]
    public string? Intent { get; set; }

    [JsonPropertyName("intentConfidence")]
    public double? IntentConfidence { get; set; }

    [JsonPropertyName("sentiment")]
    public string? Sentiment { get; set; }

    [JsonPropertyName("sentimentConfidence")]
    public double? SentimentConfidence { get; set; }

    [JsonPropertyName("classifiedAt")]
    public string? ClassifiedAt { get; set; }

    [JsonPropertyName("model")]
    public string? Model { get; set; }
}
