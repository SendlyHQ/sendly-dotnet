using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sendly.Models;

/// <summary>
/// Response from cancelling a scheduled message.
/// </summary>
public class CancelScheduledMessageResponse
{
    /// <summary>
    /// The cancelled message ID.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The new status (should be "cancelled").
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Credits refunded from cancellation.
    /// </summary>
    [JsonPropertyName("creditsRefunded")]
    public int CreditsRefunded { get; set; }

    /// <summary>
    /// Cancellation timestamp. The cancel response does not include it, so
    /// this is null; <c>GetScheduledAsync</c> returns the message with its
    /// <see cref="ScheduledMessage.CancelledAt"/>.
    /// </summary>
    [JsonPropertyName("cancelledAt")]
    public DateTime? CancelledAt { get; set; }

    /// <summary>
    /// Creates a CancelScheduledMessageResponse from a JSON element.
    /// </summary>
    internal static CancelScheduledMessageResponse FromJson(JsonElement element, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<CancelScheduledMessageResponse>(element.GetRawText(), options)
            ?? new CancelScheduledMessageResponse();
    }
}
