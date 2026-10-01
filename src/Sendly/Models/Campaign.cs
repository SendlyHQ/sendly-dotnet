using System.Text.Json.Serialization;

namespace Sendly.Models;

public class Campaign
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// The API does not return the template a campaign was created from, so
    /// this is null.
    /// </summary>
    public string? TemplateId { get; set; }

    public List<string> ContactListIds { get; set; } = new();
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// The message batch that sent the campaign, for
    /// <c>Messages.GetBatchAsync</c>; null until the campaign is sent.
    /// </summary>
    [JsonPropertyName("batchId")]
    public string? BatchId { get; set; }

    /// <summary>
    /// Number of recipients the campaign targets.
    /// </summary>
    [JsonPropertyName("totalRecipients")]
    public int RecipientCount { get; set; }

    [JsonPropertyName("sentCount")]
    public int SentCount { get; set; }

    [JsonPropertyName("deliveredCount")]
    public int DeliveredCount { get; set; }

    [JsonPropertyName("failedCount")]
    public int FailedCount { get; set; }

    [JsonPropertyName("estimatedCredits")]
    public double? EstimatedCredits { get; set; }

    [JsonPropertyName("creditsUsed")]
    public double? CreditsUsed { get; set; }

    [JsonPropertyName("scheduledAt")]
    public string? ScheduledAt { get; set; }

    public string? Timezone { get; set; }

    /// <summary>
    /// When sending started (the API's <c>sentAt</c>).
    /// </summary>
    [JsonPropertyName("sentAt")]
    public string? StartedAt { get; set; }

    [JsonPropertyName("completedAt")]
    public string? CompletedAt { get; set; }

    public string? CreatedAt { get; set; }
    public string? UpdatedAt { get; set; }
}

public class CampaignListResponse
{
    public List<Campaign> Campaigns { get; set; } = new();
    public int Total { get; set; }
    public int Limit { get; set; }
    public int Offset { get; set; }
}

public class CampaignPreview
{
    [JsonPropertyName("recipientCount")]
    public int RecipientCount { get; set; }

    [JsonPropertyName("estimatedCredits")]
    public double EstimatedCredits { get; set; }

    /// <summary>
    /// The API does not return a cost; this is always 0. Use
    /// <see cref="EstimatedCredits"/>.
    /// </summary>
    public double EstimatedCost { get; set; }

    [JsonPropertyName("blockedCount")]
    public int? BlockedCount { get; set; }

    [JsonPropertyName("sendableCount")]
    public int? SendableCount { get; set; }

    public List<string>? Warnings { get; set; }

    /// <summary>
    /// Recipients left out because they opted out.
    /// </summary>
    [JsonPropertyName("optedOutCount")]
    public int OptedOutCount { get; set; }

    /// <summary>
    /// Recipients left out because their number is invalid.
    /// </summary>
    [JsonPropertyName("invalidCount")]
    public int InvalidCount { get; set; }

    /// <summary>
    /// The workspace's credit balance.
    /// </summary>
    [JsonPropertyName("currentBalance")]
    public int CurrentBalance { get; set; }

    /// <summary>
    /// Whether the balance covers <see cref="EstimatedCredits"/>. Always true
    /// with a test key, whose sends are free.
    /// </summary>
    [JsonPropertyName("hasEnoughCredits")]
    public bool HasEnoughCredits { get; set; }
}

public class CreateCampaignRequest
{
    public string Name { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public List<string> ContactListIds { get; set; } = new();
    public string? TemplateId { get; set; }
}

public class UpdateCampaignRequest
{
    public string? Name { get; set; }
    public string? Text { get; set; }
    public List<string>? ContactListIds { get; set; }
    public string? TemplateId { get; set; }
}

public class ListCampaignsOptions
{
    public int? Limit { get; set; }
    public int? Offset { get; set; }
    public string? Status { get; set; }
}

public class ScheduleCampaignRequest
{
    [JsonPropertyName("scheduledAt")]
    public string ScheduledAt { get; set; } = string.Empty;

    /// <summary>
    /// Time zone for the schedule. Omitted when null, and the API then uses
    /// America/New_York.
    /// </summary>
    public string? Timezone { get; set; }
}

public static class CampaignStatus
{
    public const string Draft = "draft";
    public const string Scheduled = "scheduled";
    public const string Sending = "sending";

    /// <summary>
    /// A campaign that has been sent.
    /// </summary>
    public const string Completed = "completed";

    /// <summary>
    /// Never returned: a sent campaign's status is <see cref="Completed"/>.
    /// As a <see cref="ListCampaignsOptions.Status"/> filter it matches
    /// completed campaigns.
    /// </summary>
    public const string Sent = "sent";

    /// <summary>
    /// Never returned: campaigns cannot be paused.
    /// </summary>
    public const string Paused = "paused";

    public const string Cancelled = "cancelled";
    public const string Failed = "failed";
}
