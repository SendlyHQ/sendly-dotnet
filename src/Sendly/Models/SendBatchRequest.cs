using System.Text.Json.Serialization;

namespace Sendly.Models;

/// <summary>
/// Request object for sending a batch of SMS messages.
/// </summary>
public class SendBatchRequest
{
    /// <summary>
    /// List of messages to send.
    /// </summary>
    [JsonPropertyName("messages")]
    public List<BatchMessageItem> Messages { get; set; }

    /// <summary>
    /// Optional sender ID (applies to all messages).
    /// </summary>
    [JsonPropertyName("from")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? From { get; set; }

    /// <summary>
    /// Message type: "marketing" (default, subject to quiet hours) or "transactional" (24/7).
    /// </summary>
    [JsonPropertyName("messageType")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MessageType { get; set; }

    /// <summary>
    /// Shared metadata for all messages in the batch (max 4KB).
    /// </summary>
    [JsonPropertyName("metadata")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object>? Metadata { get; set; }

    /// <summary>
    /// Creates a new send batch request.
    /// </summary>
    /// <param name="messages">List of messages to send</param>
    /// <param name="from">Optional sender ID</param>
    /// <param name="messageType">Message type: "marketing" or "transactional"</param>
    /// <param name="metadata">Shared metadata for all messages (max 4KB)</param>
    public SendBatchRequest(List<BatchMessageItem> messages, string? from = null, string? messageType = null, Dictionary<string, object>? metadata = null)
    {
        Messages = messages;
        From = from;
        MessageType = messageType;
        Metadata = metadata;
    }

    /// <summary>
    /// Creates a new send batch request.
    /// </summary>
    public SendBatchRequest()
    {
        Messages = new List<BatchMessageItem>();
    }

    /// <summary>
    /// Adds a message to the batch.
    /// </summary>
    /// <param name="to">Recipient phone number in E.164 format</param>
    /// <param name="text">Message content</param>
    /// <returns>This request for chaining</returns>
    public SendBatchRequest AddMessage(string to, string text)
    {
        Messages.Add(new BatchMessageItem(to, text));
        return this;
    }
}
