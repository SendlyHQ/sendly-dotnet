using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sendly.Models;

/// <summary>
/// Response from previewing a batch (dry run).
/// </summary>
public class BatchPreviewResponse
{
    private const int MaxBatchMessages = 10000;

    /// <summary>
    /// Whether nothing the preview found stops the send: at least one message
    /// passes, the batch has at most 10,000 messages, every blocked message is
    /// an opt-out (a live send skips those but rejects the whole batch for any
    /// other block), the key has sms:send, and the balance covers it or the key
    /// is a test key. The preview does not check the monthly quota or a
    /// suspended workspace, and a test send skips the destination checks the
    /// preview applies, so a test key can send a batch this reports as false.
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
    /// Never returned by the API; always empty. See BlockedMessages.
    /// </summary>
    [JsonPropertyName("messages")]
    public List<BatchPreviewItem> Messages { get; set; } = new();

    /// <summary>
    /// Count of block reasons.
    /// </summary>
    [JsonPropertyName("blockReasons")]
    public Dictionary<string, int>? BlockReasons { get; set; }

    /// <summary>
    /// Number of messages in the batch.
    /// </summary>
    [JsonPropertyName("total")]
    public int Total { get; set; }

    /// <summary>
    /// Number of messages that pass the preview's checks.
    /// </summary>
    [JsonPropertyName("sendable")]
    public int Sendable { get; set; }

    /// <summary>
    /// Number of messages to a phone number that appears earlier in the batch
    /// (compared by its digits). A live send does not drop them, and each one
    /// is charged.
    /// </summary>
    [JsonPropertyName("duplicates")]
    public int Duplicates { get; set; }

    /// <summary>
    /// Credit balance of the account the batch would be charged to.
    /// </summary>
    [JsonPropertyName("creditBalance")]
    public int CreditBalance { get; set; }

    /// <summary>
    /// Whether the balance covers the credits needed.
    /// </summary>
    [JsonPropertyName("hasSufficientCredits")]
    public bool HasSufficientCredits { get; set; }

    /// <summary>
    /// Whether the account draws on a pooled balance.
    /// </summary>
    [JsonPropertyName("pooled")]
    public bool? Pooled { get; set; }

    /// <summary>
    /// Type of the API key the preview ran with: "test" or "live".
    /// </summary>
    [JsonPropertyName("keyType")]
    public string? KeyType { get; set; }

    /// <summary>
    /// Scopes of the API key the preview ran with.
    /// </summary>
    [JsonPropertyName("keyScopes")]
    public List<string>? KeyScopes { get; set; }

    /// <summary>
    /// Whether the key has the sms:send scope a send needs.
    /// </summary>
    [JsonPropertyName("hasWriteScope")]
    public bool HasWriteScope { get; set; }

    /// <summary>
    /// Messages the preview blocked, with the reason for each.
    /// </summary>
    [JsonPropertyName("blockedMessages")]
    public List<BatchPreviewBlockedMessage> BlockedMessages { get; set; } = new();

    /// <summary>
    /// Compliance counts for the batch.
    /// </summary>
    [JsonPropertyName("compliance")]
    public BatchPreviewCompliance? Compliance { get; set; }

    /// <summary>
    /// Warnings about the batch, such as opted-out contacts.
    /// </summary>
    [JsonPropertyName("warnings")]
    public List<string>? Warnings { get; set; }

    /// <summary>
    /// Creates a BatchPreviewResponse from a JSON element.
    /// </summary>
    internal static BatchPreviewResponse FromJson(JsonElement element, JsonSerializerOptions options)
    {
        var preview = JsonSerializer.Deserialize<BatchPreviewResponse>(element.GetRawText(), options)
            ?? new BatchPreviewResponse();
        if (!element.TryGetProperty("totalMessages", out _))
            preview.TotalMessages = preview.Total;
        if (!element.TryGetProperty("willSend", out _))
            preview.WillSend = preview.Sendable;
        if (!element.TryGetProperty("currentBalance", out _))
            preview.CurrentBalance = preview.CreditBalance;
        if (!element.TryGetProperty("hasEnoughCredits", out _))
            preview.HasEnoughCredits = preview.HasSufficientCredits;
        if (!element.TryGetProperty("canSend", out _))
            preview.CanSend = preview.Sendable > 0 &&
                preview.Total <= MaxBatchMessages &&
                preview.Blocked == (preview.Compliance?.OptedOutBlocked ?? 0) &&
                preview.HasWriteScope &&
                (preview.KeyType == "test" || preview.HasSufficientCredits);
        if (preview.BlockReasons == null && preview.BlockedMessages.Count > 0)
            preview.BlockReasons = preview.BlockedMessages
                .GroupBy(m => m.Reason)
                .ToDictionary(g => g.Key, g => g.Count());
        return preview;
    }
}

/// <summary>
/// A message the batch preview blocked.
/// </summary>
public class BatchPreviewBlockedMessage
{
    /// <summary>
    /// Position of the message in the batch.
    /// </summary>
    [JsonPropertyName("index")]
    public int Index { get; set; }

    /// <summary>
    /// Recipient phone number.
    /// </summary>
    [JsonPropertyName("to")]
    public string To { get; set; } = string.Empty;

    /// <summary>
    /// Why the message was blocked.
    /// </summary>
    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Compliance counts from a batch preview.
/// </summary>
public class BatchPreviewCompliance
{
    /// <summary>
    /// "marketing" or "transactional".
    /// </summary>
    [JsonPropertyName("messageType")]
    public string? MessageType { get; set; }

    /// <summary>
    /// Messages blocked because the contact opted out. A live send skips these.
    /// </summary>
    [JsonPropertyName("optedOutBlocked")]
    public int OptedOutBlocked { get; set; }

    /// <summary>
    /// Messages blocked for restricted content.
    /// </summary>
    [JsonPropertyName("shaftBlocked")]
    public int ShaftBlocked { get; set; }

    /// <summary>
    /// Always 0: the preview counts messages that fall in the recipient's quiet
    /// hours in QuietHoursRescheduled instead.
    /// </summary>
    [JsonPropertyName("quietHoursBlocked")]
    public int QuietHoursBlocked { get; set; }

    /// <summary>
    /// Messages that would be rescheduled out of the recipient's quiet hours.
    /// </summary>
    [JsonPropertyName("quietHoursRescheduled")]
    public int QuietHoursRescheduled { get; set; }
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
