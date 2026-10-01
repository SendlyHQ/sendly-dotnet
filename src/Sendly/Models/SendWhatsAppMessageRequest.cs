using System.Text.Json.Serialization;

namespace Sendly.Models;

/// <summary>
/// Request to send a WhatsApp message.
///
/// Provide exactly one of:
///
/// - <see cref="Text"/> — free-form text; only deliverable inside an open
/// 24-hour customer-service window (the recipient messaged you in the last 24h)
///
/// - <see cref="MediaUrls"/> — a single media attachment (optional
/// <see cref="Text"/> becomes its caption); also window-bound
///
/// - <see cref="Template"/> — an approved template; works regardless of the
/// window
///
/// WhatsApp sends require the <c>sms:send</c> scope, a live API key and a
/// <see cref="From"/> number with an active WhatsApp connection (see
/// <c>client.WhatsApp.Signup</c>).
/// </summary>
public class SendWhatsAppMessageRequest
{
    /// <summary>
    /// Selects the WhatsApp channel. Always "whatsapp".
    /// </summary>
    [JsonPropertyName("channel")]
    public string Channel { get; } = "whatsapp";

    /// <summary>
    /// Destination phone number in E.164 format (e.g., +15551234567).
    /// </summary>
    [JsonPropertyName("to")]
    public string To { get; set; }

    /// <summary>
    /// Sending number in E.164 format. Required — must be one of your numbers
    /// with an active WhatsApp connection.
    /// </summary>
    [JsonPropertyName("from")]
    public string From { get; set; }

    /// <summary>
    /// Free-form message text (max 4096 bytes), or the caption when
    /// <see cref="MediaUrls"/> is provided (max 1024 bytes). Requires an open
    /// 24-hour window — outside it the API responds 422
    /// <c>whatsapp_window_closed</c>; send a <see cref="Template"/> instead.
    /// </summary>
    [JsonPropertyName("text")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Text { get; set; }

    /// <summary>
    /// Media attachment URL. WhatsApp accepts exactly one per message.
    /// Must be a publicly accessible HTTPS URL.
    /// </summary>
    [JsonPropertyName("mediaUrls")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? MediaUrls { get; set; }

    /// <summary>
    /// Approved template to send. Works regardless of the 24-hour window.
    /// </summary>
    [JsonPropertyName("template")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public WhatsAppTemplateSendParams? Template { get; set; }

    /// <summary>
    /// Custom metadata to attach to the message (max 4KB).
    /// </summary>
    [JsonPropertyName("metadata")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object>? Metadata { get; set; }

    /// <summary>
    /// Creates a new WhatsApp send request.
    /// </summary>
    public SendWhatsAppMessageRequest(string to, string from, string? text = null, List<string>? mediaUrls = null, WhatsAppTemplateSendParams? template = null, Dictionary<string, object>? metadata = null)
    {
        To = to;
        From = from;
        Text = text;
        MediaUrls = mediaUrls;
        Template = template;
        Metadata = metadata;
    }
}

/// <summary>
/// The approved WhatsApp template to send, with its variable values.
/// </summary>
public class WhatsAppTemplateSendParams
{
    /// <summary>Template name as approved (e.g. "order_shipped").</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Template language code (e.g. "en_US") — must match the approved
    /// template's language exactly.
    /// </summary>
    [JsonPropertyName("language")]
    public string Language { get; set; } = string.Empty;

    /// <summary>
    /// Body variable values keyed by placeholder number:
    /// <c>{ ["1"] = "Acme Inc", ["2"] = "#4821" }</c>.
    /// </summary>
    [JsonPropertyName("variables")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string>? Variables { get; set; }

    /// <summary>Variable values for dynamic-URL buttons.</summary>
    [JsonPropertyName("buttons")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<WhatsAppTemplateButtonVariables>? Buttons { get; set; }
}

/// <summary>
/// Variable values for one dynamic-URL button on an approved WhatsApp
/// template.
/// </summary>
public class WhatsAppTemplateButtonVariables
{
    /// <summary>Zero-based index of the button on the approved template.</summary>
    [JsonPropertyName("index")]
    public int Index { get; set; }

    /// <summary>
    /// Values for the button's URL placeholders, keyed by placeholder number:
    /// <c>{ ["1"] = "4821" }</c>.
    /// </summary>
    [JsonPropertyName("variables")]
    public Dictionary<string, string> Variables { get; set; } = new();
}
