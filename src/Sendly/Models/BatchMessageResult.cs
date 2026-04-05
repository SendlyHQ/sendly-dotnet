using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sendly.Models;

/// <summary>
/// Represents the result of a single message in a batch.
/// </summary>
public class BatchMessageResult
{
    /// <summary>
    /// Message ID (if successfully queued).
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Recipient phone number.
    /// </summary>
    [JsonPropertyName("to")]
    public string To { get; set; } = string.Empty;

    /// <summary>
    /// Status of this message in the batch.
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Error message (if failed).
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    /// <summary>
    /// When the message was created.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public string? CreatedAt { get; set; }

    /// <summary>
    /// When the message was delivered (if applicable).
    /// </summary>
    [JsonPropertyName("deliveredAt")]
    public string? DeliveredAt { get; set; }

    /// <summary>
    /// Whether this message was successful.
    /// </summary>
    public bool IsSuccess => Status == "queued" || Status == "sent" || Status == "delivered";

    /// <summary>
    /// Whether this message failed.
    /// </summary>
    public bool IsFailed => Status == "failed" || Status == "bounced";

    /// <summary>
    /// Creates a BatchMessageResult from a JSON element.
    /// </summary>
    internal static BatchMessageResult FromJson(JsonElement element, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<BatchMessageResult>(element.GetRawText(), options)
            ?? new BatchMessageResult();
    }
}
