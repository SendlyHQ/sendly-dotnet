namespace Sendly.Models;

public class Draft
{
    public static class Statuses
    {
        public const string Pending = "pending";
        public const string Approved = "approved";
        public const string Rejected = "rejected";
        public const string Sent = "sent";
        public const string Failed = "failed";
    }

    public string Id { get; set; } = string.Empty;
    public string ConversationId { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public List<string>? MediaUrls { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Source { get; set; }
    public string? CreatedBy { get; set; }
    public string? ReviewedBy { get; set; }
    public string? ReviewedAt { get; set; }
    public string? RejectionReason { get; set; }
    public string? MessageId { get; set; }
    public string? CreatedAt { get; set; }
    public string? UpdatedAt { get; set; }
}

public class DraftListResponse
{
    public List<Draft> Data { get; set; } = new();
    public PaginationInfo Pagination { get; set; } = new();
}

public class CreateDraftRequest
{
    public string ConversationId { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public List<string>? MediaUrls { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
    public string? Source { get; set; }
}

public class UpdateDraftRequest
{
    public string? Text { get; set; }
    public List<string>? MediaUrls { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

public class RejectDraftRequest
{
    public string? Reason { get; set; }
}

public class ListDraftsOptions
{
    public string? ConversationId { get; set; }
    public string? Status { get; set; }
    public int? Limit { get; set; }
    public int? Offset { get; set; }

    internal Dictionary<string, string> ToQueryParams()
    {
        var @params = new Dictionary<string, string>();

        if (!string.IsNullOrEmpty(ConversationId))
            @params["conversation_id"] = ConversationId;

        if (!string.IsNullOrEmpty(Status))
            @params["status"] = Status;

        if (Limit.HasValue)
            @params["limit"] = Limit.Value.ToString();

        if (Offset.HasValue)
            @params["offset"] = Offset.Value.ToString();

        return @params;
    }
}
