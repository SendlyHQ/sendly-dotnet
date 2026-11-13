using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sendly.Models;

/// <summary>
/// What a webhook recovery would do for a window, without sending anything.
/// Returned by <see cref="Resources.WebhooksResource.RecoveryPreviewAsync"/>.
/// The API sends its keys in camelCase, unlike the other webhook responses.
/// </summary>
public class WebhookRecoveryPreview
{
    /// <summary>
    /// The circuit breaker: <c>closed</c>, <c>open</c> or <c>half_open</c>
    /// (see <see cref="Webhook.CircuitStates"/>). More values may be added.
    /// </summary>
    [JsonPropertyName("circuitState")]
    public string CircuitState { get; set; } = Webhook.CircuitStates.Closed;

    /// <summary>
    /// When the circuit opened, or null.
    /// </summary>
    [JsonPropertyName("circuitOpenedAt")]
    public string? CircuitOpenedAt { get; set; }

    /// <summary>
    /// Failed deliveries in the current streak.
    /// </summary>
    [JsonPropertyName("failureCount")]
    public int FailureCount { get; set; }

    /// <summary>
    /// Failed and cancelled deliveries in the window: what
    /// <see cref="Resources.WebhooksResource.RedeliverAsync"/> would send.
    /// </summary>
    [JsonPropertyName("auditFailures")]
    public WebhookRecoveryAuditFailures AuditFailures { get; set; } = new();

    /// <summary>
    /// Message events with no delivery in the window: what
    /// <see cref="Resources.WebhooksResource.BackfillAsync"/> would send.
    /// </summary>
    [JsonPropertyName("missingFromMessages")]
    public WebhookRecoveryMissing MissingFromMessages { get; set; } = new();

    /// <summary>
    /// The window in up to 24 equal buckets, oldest first.
    /// </summary>
    [JsonPropertyName("histogram")]
    public List<WebhookRecoveryHistogramBucket> Histogram { get; set; } = new();

    /// <summary>
    /// Up to 5 affected events: failed deliveries first, then missing
    /// message events, each newest first.
    /// </summary>
    [JsonPropertyName("samples")]
    public List<WebhookRecoverySample> Samples { get; set; } = new();

    /// <summary>
    /// The outage window, when the circuit has an opening time, or null.
    /// </summary>
    [JsonPropertyName("windowSuggestion")]
    public WebhookRecoveryWindow? WindowSuggestion { get; set; }

    /// <summary>
    /// The window's start (ISO 8601).
    /// </summary>
    [JsonPropertyName("since")]
    public string Since { get; set; } = string.Empty;

    /// <summary>
    /// The window's end (ISO 8601).
    /// </summary>
    [JsonPropertyName("until")]
    public string Until { get; set; } = string.Empty;

    /// <summary>
    /// Whether the circuit breaker is open.
    /// </summary>
    [JsonIgnore]
    public bool IsCircuitOpen => CircuitState == Webhook.CircuitStates.Open;

    internal static WebhookRecoveryPreview FromJson(JsonElement element, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<WebhookRecoveryPreview>(element.GetRawText(), options)
            ?? new WebhookRecoveryPreview();
    }
}

/// <summary>
/// Failed and cancelled deliveries in a recovery preview's window.
/// </summary>
public class WebhookRecoveryAuditFailures
{
    /// <summary>How many deliveries.</summary>
    [JsonPropertyName("total")]
    public int Total { get; set; }

    /// <summary>Counts by delivery status, such as <c>failed</c> and <c>cancelled</c>.</summary>
    [JsonPropertyName("byStatus")]
    public Dictionary<string, int> ByStatus { get; set; } = new();

    /// <summary>Counts by event type, such as <c>message.delivered</c>.</summary>
    [JsonPropertyName("byEventType")]
    public Dictionary<string, int> ByEventType { get; set; } = new();
}

/// <summary>
/// Message events with no delivery in a recovery preview's window.
/// </summary>
public class WebhookRecoveryMissing
{
    /// <summary>How many events.</summary>
    [JsonPropertyName("total")]
    public int Total { get; set; }

    /// <summary>Counts by event type, such as <c>message.sent</c>.</summary>
    [JsonPropertyName("byEventType")]
    public Dictionary<string, int> ByEventType { get; set; } = new();
}

/// <summary>
/// One bucket of a recovery preview's histogram.
/// </summary>
public class WebhookRecoveryHistogramBucket
{
    /// <summary>The bucket's start (ISO 8601).</summary>
    [JsonPropertyName("bucket")]
    public string Bucket { get; set; } = string.Empty;

    /// <summary>Missing message events in the bucket.</summary>
    [JsonPropertyName("missing")]
    public int Missing { get; set; }

    /// <summary>Failed deliveries in the bucket.</summary>
    [JsonPropertyName("failed")]
    public int Failed { get; set; }
}

/// <summary>
/// One affected event in a recovery preview.
/// </summary>
public class WebhookRecoverySample
{
    /// <summary>The event's object id, such as a message id.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>The recipient, masked, or <c>***</c> when there is none.</summary>
    [JsonPropertyName("toMasked")]
    public string ToMasked { get; set; } = string.Empty;

    /// <summary>The event type.</summary>
    [JsonPropertyName("eventType")]
    public string EventType { get; set; } = string.Empty;

    /// <summary>When the delivery or message was created (ISO 8601).</summary>
    [JsonPropertyName("occurredAt")]
    public string OccurredAt { get; set; } = string.Empty;
}

/// <summary>
/// The suggested outage window in a recovery preview.
/// </summary>
public class WebhookRecoveryWindow
{
    /// <summary>When the circuit opened (ISO 8601).</summary>
    [JsonPropertyName("since")]
    public string Since { get; set; } = string.Empty;

    /// <summary>Now (ISO 8601).</summary>
    [JsonPropertyName("until")]
    public string Until { get; set; } = string.Empty;
}
