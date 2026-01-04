using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sendly.Models;

/// <summary>
/// Response from previewing a batch (dry run).
/// </summary>
public class BatchPreviewResponse
{
    /// <summary>
    /// Whether the entire batch can be sent.
    /// </summary>
    [JsonPropertyName("canSend")]
    public bool CanSend { get; set; }

    /// <summary>
    /// Total number of messages.
    /// </summary>
    [JsonPropertyName("totalMessages")]
    public int TotalMessages { get; set; }

    /// <summary>
    /// Number of messages that will be sent.
    /// </summary>
    [JsonPropertyName("willSend")]
    public int WillSend { get; set; }

    /// <summary>
    /// Number of messages that are blocked.
    /// </summary>
    [JsonPropertyName("blocked")]
    public int Blocked { get; set; }

    /// <summary>
    /// Total credits needed.
    /// </summary>
    [JsonPropertyName("creditsNeeded")]
    public int CreditsNeeded { get; set; }

    /// <summary>
    /// Current credit balance.
    /// </summary>
    [JsonPropertyName("currentBalance")]
    public int CurrentBalance { get; set; }

    /// <summary>
    /// Whether there are enough credits.
    /// </summary>
    [JsonPropertyName("hasEnoughCredits")]
    public bool HasEnoughCredits { get; set; }

    /// <summary>
    /// Preview for each message.
    /// </summary>
    [JsonPropertyName("messages")]
    public List<BatchPreviewItem> Messages { get; set; } = new();

    /// <summary>
    /// Count of block reasons.
    /// </summary>
    [JsonPropertyName("blockReasons")]
    public Dictionary<string, int>? BlockReasons { get; set; }

    /// <summary>
    /// Creates a BatchPreviewResponse from a JSON element.
    /// </summary>
    internal static BatchPreviewResponse FromJson(JsonElement element, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<BatchPreviewResponse>(element.GetRawText(), options)
            ?? new BatchPreviewResponse();
    }
}

/// <summary>
/// A single message in a batch preview.
/// </summary>
public class BatchPreviewItem
{
    /// <summary>
    /// Recipient phone number.
    /// </summary>
    [JsonPropertyName("to")]
    public string To { get; set; } = string.Empty;

    /// <summary>
    /// Message content.
    /// </summary>
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Number of SMS segments.
    /// </summary>
    [JsonPropertyName("segments")]
    public int Segments { get; set; } = 1;

    /// <summary>
    /// Credits needed for this message.
    /// </summary>
    [JsonPropertyName("credits")]
    public int Credits { get; set; }

    /// <summary>
    /// Whether this message can be sent.
    /// </summary>
    [JsonPropertyName("canSend")]
    public bool CanSend { get; set; }

    /// <summary>
    /// Reason if message is blocked.
    /// </summary>
    [JsonPropertyName("blockReason")]
    public string? BlockReason { get; set; }

    /// <summary>
    /// Destination country code.
    /// </summary>
    [JsonPropertyName("country")]
    public string? Country { get; set; }

    /// <summary>
    /// Pricing tier for this message.
    /// </summary>
    [JsonPropertyName("pricingTier")]
    public string? PricingTier { get; set; }
}
