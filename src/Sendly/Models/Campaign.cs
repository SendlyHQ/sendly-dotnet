namespace Sendly.Models;

public class Campaign
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string? TemplateId { get; set; }
    public List<string> ContactListIds { get; set; } = new();
    public string Status { get; set; } = string.Empty;
    public int RecipientCount { get; set; }
    public int SentCount { get; set; }
    public int DeliveredCount { get; set; }
    public int FailedCount { get; set; }
    public double? EstimatedCredits { get; set; }
    public double? CreditsUsed { get; set; }
    public string? ScheduledAt { get; set; }
    public string? Timezone { get; set; }
    public string? StartedAt { get; set; }
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
    public int RecipientCount { get; set; }
    public double EstimatedCredits { get; set; }
    public double EstimatedCost { get; set; }
    public int? BlockedCount { get; set; }
    public int? SendableCount { get; set; }
    public List<string>? Warnings { get; set; }
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
    public string ScheduledAt { get; set; } = string.Empty;
    public string? Timezone { get; set; }
}

public static class CampaignStatus
{
    public const string Draft = "draft";
    public const string Scheduled = "scheduled";
    public const string Sending = "sending";
    public const string Sent = "sent";
    public const string Paused = "paused";
    public const string Cancelled = "cancelled";
    public const string Failed = "failed";
}
