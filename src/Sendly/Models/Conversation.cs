namespace Sendly.Models;

public class Conversation
{
    public static class Statuses
    {
        public const string Active = "active";
        public const string Closed = "closed";
    }

    public string Id { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int UnreadCount { get; set; }
    public int MessageCount { get; set; }
    public string? LastMessageText { get; set; }
    public string? LastMessageAt { get; set; }
    public string? LastMessageDirection { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
    public List<string>? Tags { get; set; }
    public string? ContactId { get; set; }
    public string? CreatedAt { get; set; }
    public string? UpdatedAt { get; set; }
}

public class ConversationWithMessages : Conversation
{
    public ConversationMessagesPage? Messages { get; set; }
}

public class ConversationMessagesPage
{
    public List<Message> Data { get; set; } = new();
    public PaginationInfo Pagination { get; set; } = new();
}

public class PaginationInfo
{
    public int Total { get; set; }
    public int Limit { get; set; }
    public int Offset { get; set; }
    public bool HasMore { get; set; }
}

public class ConversationListResponse
{
    public List<Conversation> Data { get; set; } = new();
    public PaginationInfo Pagination { get; set; } = new();
}

public class ListConversationsOptions
{
    public int? Limit { get; set; }
    public int? Offset { get; set; }
    public string? Status { get; set; }

    internal Dictionary<string, string> ToQueryParams()
    {
        var @params = new Dictionary<string, string>();

        if (Limit.HasValue)
            @params["limit"] = Math.Min(Limit.Value, 100).ToString();

        if (Offset.HasValue)
            @params["offset"] = Offset.Value.ToString();

        if (!string.IsNullOrEmpty(Status))
            @params["status"] = Status;

        return @params;
    }
}

public class GetConversationOptions
{
    public bool? IncludeMessages { get; set; }
    public int? MessageLimit { get; set; }
    public int? MessageOffset { get; set; }

    internal Dictionary<string, string> ToQueryParams()
    {
        var @params = new Dictionary<string, string>();

        if (IncludeMessages == true)
            @params["include_messages"] = "true";

        if (MessageLimit.HasValue)
            @params["message_limit"] = MessageLimit.Value.ToString();

        if (MessageOffset.HasValue)
            @params["message_offset"] = MessageOffset.Value.ToString();

        return @params;
    }
}

public class UpdateConversationRequest
{
    public Dictionary<string, object>? Metadata { get; set; }
    public List<string>? Tags { get; set; }
}

public class ReplyToConversationRequest
{
    public string Text { get; set; } = string.Empty;
    public string? MessageType { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
    public List<string>? MediaUrls { get; set; }
}
