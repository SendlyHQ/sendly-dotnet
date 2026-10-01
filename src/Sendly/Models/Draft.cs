using System.Text.Json.Serialization;

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

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("conversationId")]
    public string ConversationId { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("mediaUrls")]
    public List<string>? MediaUrls { get; set; }

    [JsonPropertyName("metadata")]
    public Dictionary<string, object>? Metadata { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("source")]
    public string? Source { get; set; }

    [JsonPropertyName("createdBy")]
    public string? CreatedBy { get; set; }

    [JsonPropertyName("reviewedBy")]
    public string? ReviewedBy { get; set; }

    [JsonPropertyName("reviewedAt")]
    public string? ReviewedAt { get; set; }

    [JsonPropertyName("rejectionReason")]
    public string? RejectionReason { get; set; }

    [JsonPropertyName("messageId")]
    public string? MessageId { get; set; }

    [JsonPropertyName("createdAt")]
    public string? CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public string? UpdatedAt { get; set; }
}

public class DraftListResponse
{
    [JsonPropertyName("data")]
    public List<Draft> Data { get; set; } = new();

    [JsonPropertyName("pagination")]
    public PaginationInfo Pagination { get; set; } = new();
}

public class CreateDraftRequest
{
    [JsonPropertyName("conversationId")]
    public string ConversationId { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("mediaUrls")]
    public List<string>? MediaUrls { get; set; }

    [JsonPropertyName("metadata")]
    public Dictionary<string, object>? Metadata { get; set; }

    [JsonPropertyName("source")]
    public string? Source { get; set; }
}

/// <summary>
/// Changes to a pending draft. Only the properties you set are sent, and the
/// API leaves the others as they are. Setting <see cref="MediaUrls"/> or
/// <see cref="Metadata"/> to null clears it. A draft always has text, so a
/// null <see cref="Text"/> is not sent.
/// </summary>
public class UpdateDraftRequest : IAssignedProperties
{
    private readonly HashSet<string> _assigned = new();
    private List<string>? _mediaUrls;
    private Dictionary<string, object>? _metadata;

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("mediaUrls")]
    public List<string>? MediaUrls
    {
        get => _mediaUrls;
        set { _mediaUrls = value; _assigned.Add(nameof(MediaUrls)); }
    }

    [JsonPropertyName("metadata")]
    public Dictionary<string, object>? Metadata
    {
        get => _metadata;
        set { _metadata = value; _assigned.Add(nameof(Metadata)); }
    }

    bool IAssignedProperties.IsAssigned(string propertyName) => _assigned.Contains(propertyName);
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
