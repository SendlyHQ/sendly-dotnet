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
    /// Creates a WhatsAppMessage from a JSON element.
    /// </summary>
    internal static WhatsAppMessage FromJson(JsonElement element, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<WhatsAppMessage>(element.GetRawText(), options)
            ?? new WhatsAppMessage();
    }
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
