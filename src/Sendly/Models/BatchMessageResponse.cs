using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sendly.Models;

/// <summary>
/// Response from a batch send operation.
/// </summary>
public class BatchMessageResponse
{
    /// <summary>
    /// Batch status constants.
    /// </summary>
    public static class Statuses
    {
        public const string Processing = "processing";
        public const string Completed = "completed";
        public const string PartialFailure = "partial_failure";
        public const string Failed = "failed";
    }

    /// <summary>
    /// Unique batch identifier.
    /// </summary>
    [JsonPropertyName("batch_id")]
    public string BatchId { get; set; } = string.Empty;

    /// <summary>
    /// Batch status.
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Total number of messages in the batch.
    /// </summary>
    [JsonPropertyName("total")]
    public int Total { get; set; }

    /// <summary>
    /// Number of messages queued successfully.
    /// </summary>
    [JsonPropertyName("queued")]
    public int Queued { get; set; }

    /// <summary>
    /// Number of messages that failed.
    /// </summary>
    [JsonPropertyName("failed")]
    public int Failed { get; set; }

    /// <summary>
    /// Total credits used.
    /// </summary>
    [JsonPropertyName("credits_used")]
    public int CreditsUsed { get; set; }

    /// <summary>
    /// Individual message results.
    /// </summary>
    [JsonPropertyName("messages")]
    public List<BatchMessageResult> Messages { get; set; } = new();

    /// <summary>
    /// Creation timestamp.
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTime? CreatedAt { get; set; }

    /// <summary>
    /// Completion timestamp.
    /// </summary>
    [JsonPropertyName("completed_at")]
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Whether the batch is still processing.
    /// </summary>
    public bool IsProcessing => Status == Statuses.Processing;

    /// <summary>
    /// Whether the batch completed successfully.
    /// </summary>
    public bool IsCompleted => Status == Statuses.Completed;

    /// <summary>
    /// Whether the batch completed with some failures.
    /// </summary>
    public bool IsPartialFailure => Status == Statuses.PartialFailure;

    /// <summary>
    /// Whether all messages in the batch failed.
    /// </summary>
    public bool IsFailed => Status == Statuses.Failed;

    /// <summary>
    /// Creates a BatchMessageResponse from a JSON element.
    /// </summary>
    internal static BatchMessageResponse FromJson(JsonElement element, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<BatchMessageResponse>(element.GetRawText(), options)
            ?? new BatchMessageResponse();
    }
}
