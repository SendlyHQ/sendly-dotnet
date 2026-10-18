using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sendly.Models;

/// <summary>
/// A sent WhatsApp message.
/// </summary>
public class WhatsAppMessage
{
    /// <summary>
    /// Unique message identifier.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Always "whatsapp".
    /// </summary>
    [JsonPropertyName("channel")]
    public string Channel { get; set; } = string.Empty;

    /// <summary>
    /// Always "whatsapp".
    /// </summary>
    [JsonPropertyName("message_format")]
    public string MessageFormat { get; set; } = string.Empty;

    /// <summary>
    /// Destination phone number.
    /// </summary>
    [JsonPropertyName("to")]
    public string To { get; set; } = string.Empty;

    /// <summary>
    /// Sending number.
    /// </summary>
    [JsonPropertyName("from")]
    public string From { get; set; } = string.Empty;

    /// <summary>
    /// Body text for free-form text sends, or the caption for media sends
    /// (pass it as <c>Text</c> with <c>MediaUrls</c>); null for template sends
    /// and for media sent without a caption.
    /// </summary>
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    /// <summary>
    /// Current delivery status.
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Always 1 — WhatsApp has no segment concept.
    /// </summary>
    [JsonPropertyName("segments")]
    public int Segments { get; set; } = 1;

    /// <summary>
    /// Credits charged for this message. Free-form text or media inside the
    /// 24-hour window: 1 credit each for the first 1,000 per sending number per
    /// calendar month (UTC), then the destination's utility template price;
    /// countries without a listed price use the default utility price of 12
    /// credits. Templates are priced by category and destination country;
    /// countries without a listed price use 33 (marketing), 12 (utility) and
    /// 12 (authentication) credits. A failed send gives its slot back.
    /// </summary>
    [JsonPropertyName("creditsUsed")]
    public int CreditsUsed { get; set; }

    /// <summary>
    /// WhatsApp-specific details.
    /// </summary>
    [JsonPropertyName("whatsapp")]
    public WhatsAppMessageDetails WhatsApp { get; set; } = new();

    /// <summary>
    /// When the message was created (ISO 8601).
    /// </summary>
    [JsonPropertyName("createdAt")]
    public string CreatedAt { get; set; } = string.Empty;

    /// <summary>
    /// Custom metadata attached to the message.
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, object>? Metadata { get; set; }

    /// <summary>
    /// What happened to the SMS fallback, when the send asked for
    /// <see cref="SendWhatsAppMessageRequest.FallbackToSms"/>; null otherwise.
    /// When WhatsApp couldn't take the message and it went as an SMS instead,
    /// <see cref="Status"/> is <c>failed</c> and <c>Fallback.Status</c> is
    /// <c>sent</c> (see <see cref="FellBack"/>).
    /// </summary>
    [JsonPropertyName("fallback")]
    public WhatsAppSmsFallback? Fallback { get; set; }

    /// <summary>
    /// Whether the message went as an SMS instead: true when
    /// <see cref="Fallback"/> is present and its status is <c>sent</c>.
    /// </summary>
    public bool FellBack => Fallback?.IsSent ?? false;

    /// <summary>
    /// Creates a WhatsAppMessage from a JSON element.
    /// </summary>
    internal static WhatsAppMessage FromJson(JsonElement element, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<WhatsAppMessage>(element.GetRawText(), options)
            ?? new WhatsAppMessage();
    }
}

/// <summary>
/// The SMS fallback of a WhatsApp message sent with
/// <see cref="SendWhatsAppMessageRequest.FallbackToSms"/> set to true.
/// </summary>
public class WhatsAppSmsFallback
{
    /// <summary>
    /// Fallback status constants: the values of <see cref="Status"/>. Values
    /// the API adds later come through unchanged.
    /// </summary>
    public static class Statuses
    {
        /// <summary>Waiting for WhatsApp's outcome.</summary>
        public const string Pending = "pending";

        /// <summary>The SMS went.</summary>
        public const string Sent = "sent";

        /// <summary>The SMS carrier refused the SMS, or credits ran out.</summary>
        public const string Failed = "failed";

        /// <summary>The SMS could no longer be sent from this number.</summary>
        public const string Unavailable = "unavailable";

        /// <summary>The WhatsApp failure isn't one an SMS fixes, such as the recipient opting out.</summary>
        public const string Skipped = "skipped";
    }

    /// <summary>
    /// Fallback reason constants: the values of <see cref="Reason"/>. Values
    /// the API adds later come through unchanged.
    /// </summary>
    public static class Reasons
    {
        /// <summary>The 24-hour window was closed.</summary>
        public const string WindowClosed = "window_closed";

        /// <summary>WhatsApp refused the message when Sendly handed it over.</summary>
        public const string WhatsAppRefused = "whatsapp_refused";

        /// <summary>WhatsApp reported the message undeliverable later.</summary>
        public const string WhatsAppFailed = "whatsapp_failed";
    }

    /// <summary>
    /// Always "sms".
    /// </summary>
    [JsonPropertyName("channel")]
    public string Channel { get; set; } = "sms";

    /// <summary>
    /// <c>pending</c>, <c>sent</c>, <c>failed</c>, <c>unavailable</c> or
    /// <c>skipped</c> (see <see cref="Statuses"/>).
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// What made the message fall back: <c>window_closed</c>,
    /// <c>whatsapp_refused</c> or <c>whatsapp_failed</c> (see
    /// <see cref="Reasons"/>).
    /// </summary>
    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    /// <summary>
    /// The SMS message's id, once it exists. Read it with
    /// <c>client.Messages.GetAsync</c>.
    /// </summary>
    [JsonPropertyName("messageId")]
    public string? MessageId { get; set; }

    /// <summary>
    /// Credits charged for the SMS.
    /// </summary>
    [JsonPropertyName("creditsUsed")]
    public int? CreditsUsed { get; set; }

    /// <summary>
    /// Why the SMS didn't go, in plain English.
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    /// <summary>
    /// Template parts with no SMS form, such as <c>quick_replies</c> or
    /// <c>header_media</c>.
    /// </summary>
    [JsonPropertyName("contentDropped")]
    public List<string>? ContentDropped { get; set; }

    /// <summary>
    /// Whether WhatsApp's outcome is still unknown.
    /// </summary>
    public bool IsPending => Status == Statuses.Pending;

    /// <summary>
    /// Whether the SMS went.
    /// </summary>
    public bool IsSent => Status == Statuses.Sent;
}

/// <summary>
/// WhatsApp-specific details on a sent message.
/// </summary>
public class WhatsAppMessageDetails
{
    /// <summary>
    /// What was sent: "text", "media", or "template".
    /// </summary>
    [JsonPropertyName("kind")]
    public string Kind { get; set; } = string.Empty;

    /// <summary>
    /// The template that was sent (template sends only).
    /// </summary>
    [JsonPropertyName("template")]
    public WhatsAppMessageTemplate? Template { get; set; }

    /// <summary>
    /// WhatsApp message id — null until the first delivery report lands;
    /// populated on the message record afterwards.
    /// </summary>
    [JsonPropertyName("messageId")]
    public string? MessageId { get; set; }
}

/// <summary>
/// The template a WhatsApp message was sent with.
/// </summary>
public class WhatsAppMessageTemplate
{
    /// <summary>Template name.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Template language code.</summary>
    [JsonPropertyName("language")]
    public string Language { get; set; } = string.Empty;

    /// <summary>
    /// Billing category: "marketing", "utility", or "authentication". Meta
    /// reviews and may reclassify templates; the category on the send response
    /// is what was billed.
    /// </summary>
    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;
}
