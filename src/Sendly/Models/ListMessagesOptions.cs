namespace Sendly.Models;

/// <summary>
/// Options for listing messages.
/// </summary>
public class ListMessagesOptions
{
    /// <summary>
    /// Maximum messages to return (default: 50, max: 100).
    /// </summary>
    public int? Limit { get; set; }

    /// <summary>
    /// Number of messages to skip.
    /// </summary>
    public int? Offset { get; set; }

    /// <summary>
    /// Filter by status.
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Filter by recipient phone number.
    /// </summary>
    public string? To { get; set; }

    /// <summary>
    /// Converts to query parameters.
    /// </summary>
    internal Dictionary<string, string> ToQueryParams()
    {
        var @params = new Dictionary<string, string>();

        if (Limit.HasValue)
            @params["limit"] = Math.Min(Limit.Value, 100).ToString();

        if (Offset.HasValue)
            @params["offset"] = Offset.Value.ToString();

        if (!string.IsNullOrEmpty(Status))
            @params["status"] = Status;

        if (!string.IsNullOrEmpty(To))
            @params["to"] = To;

        return @params;
    }
}
