using System.Collections;
using System.Text.Json;

namespace Sendly.Models;

/// <summary>
/// Represents a paginated list of messages.
/// </summary>
public class MessageList : IEnumerable<Message>
{
    /// <summary>
    /// Messages in this page.
    /// </summary>
    public IReadOnlyList<Message> Data { get; }

    /// <summary>
    /// Total number of messages.
    /// </summary>
    public int Total { get; }

    /// <summary>
    /// Current page limit.
    /// </summary>
    public int Limit { get; }

    /// <summary>
    /// Current offset.
    /// </summary>
    public int Offset { get; }

    /// <summary>
    /// Whether there are more pages.
    /// </summary>
    public bool HasMore { get; }

    /// <summary>
    /// Number of messages in this page.
    /// </summary>
    public int Count => Data.Count;

    /// <summary>
    /// Whether this page is empty.
    /// </summary>
    public bool IsEmpty => Data.Count == 0;

    /// <summary>
    /// Gets the first message.
    /// </summary>
    public Message? First => Data.FirstOrDefault();

    /// <summary>
    /// Gets the last message.
    /// </summary>
    public Message? Last => Data.LastOrDefault();

    internal MessageList(JsonDocument doc, JsonSerializerOptions options)
    {
        var root = doc.RootElement;
        var messages = new List<Message>();

        if (root.TryGetProperty("data", out var dataElement) &&
            dataElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in dataElement.EnumerateArray())
            {
                messages.Add(Message.FromJson(item, options));
            }
        }

        Data = messages.AsReadOnly();

        if (root.TryGetProperty("pagination", out var pagination))
        {
            Total = pagination.TryGetProperty("total", out var t) ? t.GetInt32() : messages.Count;
            Limit = pagination.TryGetProperty("limit", out var l) ? l.GetInt32() : 20;
            Offset = pagination.TryGetProperty("offset", out var o) ? o.GetInt32() : 0;
            HasMore = pagination.TryGetProperty("has_more", out var h) && h.GetBoolean();
        }
        else
        {
            Total = root.TryGetProperty("total", out var t) ? t.GetInt32() : messages.Count;
            Limit = root.TryGetProperty("limit", out var l) ? l.GetInt32() : 20;
            Offset = root.TryGetProperty("offset", out var o) ? o.GetInt32() : 0;
            HasMore = root.TryGetProperty("has_more", out var h) && h.GetBoolean();
        }
    }

    /// <summary>
    /// Gets the enumerator.
    /// </summary>
    public IEnumerator<Message> GetEnumerator() => Data.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Gets a message by index.
    /// </summary>
    public Message this[int index] => Data[index];
}
