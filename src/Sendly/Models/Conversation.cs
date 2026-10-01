using System.Text.Json.Serialization;

namespace Sendly.Models;

public class Conversation
{
    public static class Statuses
    {
        public const string Active = "active";
        public const string Closed = "closed";
    }

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("phoneNumber")]
    public string PhoneNumber { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("unreadCount")]
    public int UnreadCount { get; set; }

    [JsonPropertyName("messageCount")]
    public int MessageCount { get; set; }

    [JsonPropertyName("lastMessageText")]
    public string? LastMessageText { get; set; }

    [JsonPropertyName("lastMessageAt")]
    public string? LastMessageAt { get; set; }

    [JsonPropertyName("lastMessageDirection")]
    public string? LastMessageDirection { get; set; }

    /// <summary>
    /// <c>sms</c> or <c>whatsapp</c>. Threads with the same contact on
    /// different channels are separate conversations.
    /// </summary>
    [JsonPropertyName("channel")]
    public string? Channel { get; set; }

    [JsonPropertyName("metadata")]
    public Dictionary<string, object>? Metadata { get; set; }

    [JsonPropertyName("tags")]
    public List<string>? Tags { get; set; }

    [JsonPropertyName("contactId")]
    public string? ContactId { get; set; }

    /// <summary>
    /// Whether this is a group MMS thread. Returned by <c>ListAsync</c> and
    /// <c>GetAsync</c>; the update, close, reopen and mark-read responses
    /// leave it null.
    /// </summary>
    [JsonPropertyName("isGroup")]
    public bool? IsGroup { get; set; }

    /// <summary>
    /// The other phone numbers in a group MMS thread; null for a one-to-one
    /// conversation.
    /// </summary>
    [JsonPropertyName("participants")]
    public List<string>? Participants { get; set; }

    [JsonPropertyName("createdAt")]
    public string? CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public string? UpdatedAt { get; set; }
}

public class ConversationWithMessages : Conversation
{
    [JsonPropertyName("messages")]
    public ConversationMessagesPage? Messages { get; set; }
}

public class ConversationMessagesPage
{
    [JsonPropertyName("data")]
    public List<Message> Data { get; set; } = new();

    [JsonPropertyName("pagination")]
    public PaginationInfo Pagination { get; set; } = new();
}

public class PaginationInfo
{
    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("limit")]
    public int Limit { get; set; }

    [JsonPropertyName("offset")]
    public int Offset { get; set; }

    [JsonPropertyName("has_more")]
    public bool HasMore { get; set; }

    [JsonInclude]
    [JsonPropertyName("hasMore")]
    private bool HasMoreCamelCase { set => HasMore = value; }
}

public class ConversationListResponse
{
    [JsonPropertyName("data")]
    public List<Conversation> Data { get; set; } = new();

    [JsonPropertyName("pagination")]
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

public class ConversationContextResponse
{
    [JsonPropertyName("context")]
    public string Context { get; set; } = string.Empty;

    [JsonPropertyName("conversation")]
    public ConversationContextInfo Conversation { get; set; } = new();

    [JsonPropertyName("tokenEstimate")]
    public int TokenEstimate { get; set; }

    [JsonPropertyName("business")]
    public ConversationContextBusiness? Business { get; set; }
}

public class ConversationContextInfo
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("phoneNumber")]
    public string PhoneNumber { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("messageCount")]
    public int MessageCount { get; set; }

    [JsonPropertyName("unreadCount")]
    public int UnreadCount { get; set; }
}

public class ConversationContextBusiness
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("useCase")]
    public string? UseCase { get; set; }
}

public class SuggestedReply
{
    public string Text { get; set; } = string.Empty;
    public string Tone { get; set; } = string.Empty;
}

public class SuggestRepliesResponse
{
    public List<SuggestedReply> Suggestions { get; set; } = new();

    [JsonPropertyName("basedOnMessageId")]
    public string? BasedOnMessageId { get; set; }

    public string? Model { get; set; }
}
