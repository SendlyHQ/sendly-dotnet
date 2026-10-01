using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sendly.Models;

/// <summary>
/// Represents a webhook configuration.
/// </summary>
public class Webhook
{
    /// <summary>
    /// Webhook event type constants.
    /// </summary>
    public static class EventTypes
    {
        public const string MessageSent = "message.sent";
        public const string MessageDelivered = "message.delivered";
        public const string MessageRead = "message.read";
        public const string MessageFailed = "message.failed";
        public const string MessageBounced = "message.bounced";
        public const string MessageRetrying = "message.retrying";
        public const string MessageReceived = "message.received";
        public const string MessageOptOut = "message.opt_out";
        public const string MessageOptIn = "message.opt_in";
        public const string VerificationCreated = "verification.created";
        public const string VerificationDelivered = "verification.delivered";
        public const string VerificationVerified = "verification.verified";
        public const string VerificationExpired = "verification.expired";
        public const string VerificationFailed = "verification.failed";
        public const string VerificationResent = "verification.resent";
        public const string VerificationDeliveryFailed = "verification.delivery_failed";
        public const string ConversationCreated = "conversation.created";
        public const string ConversationUpdated = "conversation.updated";
        public const string DraftCreated = "draft.created";
        public const string DraftApproved = "draft.approved";
        public const string DraftRejected = "draft.rejected";
        public const string ContactAutoFlagged = "contact.auto_flagged";
        public const string ContactMarkedValid = "contact.marked_valid";
        public const string ContactsLookupCompleted = "contacts.lookup_completed";
        public const string ContactsBulkMarkedValid = "contacts.bulk_marked_valid";
        public const string BrandVerified = "brand.verified";
        public const string BrandFailed = "brand.failed";
        public const string CampaignApproved = "campaign.approved";
        public const string CampaignRejected = "campaign.rejected";
        public const string CampaignSuspended = "campaign.suspended";
        public const string AssignmentConfirmed = "assignment.confirmed";
        public const string AssignmentFailed = "assignment.failed";
        public const string RcsBrandVerified = "rcs_brand.verified";
        public const string RcsBrandFailed = "rcs_brand.failed";
        public const string RcsAgentTesting = "rcs_agent.testing";
        public const string RcsAgentLive = "rcs_agent.live";
        public const string RcsAgentRejected = "rcs_agent.rejected";
        public const string RcsAgentActionRequired = "rcs_agent.action_required";
        public const string PortCompleted = "port.completed";
        public const string PortOutRequested = "port_out.requested";
        public const string PortOutCompleted = "port_out.completed";
        public const string PortOutRejected = "port_out.rejected";
        public const string PortOutCancelled = "port_out.cancelled";
        public const string NumberActivated = "number.activated";
        public const string NumberFailed = "number.failed";
        public const string NumberRequirementsRequired = "number.requirements_required";
        public const string NumberReleased = "number.released";
        public const string WhatsappAccountConnected = "whatsapp_account.connected";
        public const string WhatsappAccountFailed = "whatsapp_account.failed";
        public const string WhatsappTemplateApproved = "whatsapp_template.approved";
        public const string WhatsappTemplateRejected = "whatsapp_template.rejected";
        public const string WhatsappTemplatePaused = "whatsapp_template.paused";
        public const string CallStarted = "call.started";
        public const string CallCompleted = "call.completed";
        public const string CallRecordingReady = "call.recording.ready";
        public const string ShortCodeActionRequired = "short_code.action_required";
        public const string ShortCodeRejected = "short_code.rejected";
        public const string ShortCodeFiled = "short_code.filed";
        public const string ShortCodeLive = "short_code.live";
    }

    /// <summary>
    /// Circuit breaker state constants.
    /// </summary>
    public static class CircuitStates
    {
        public const string Closed = "closed";
        public const string Open = "open";
        public const string HalfOpen = "half_open";
    }

    /// <summary>
    /// Webhook mode constants for event filtering.
    /// </summary>
    public static class Modes
    {
        public const string All = "all";
        public const string Test = "test";
        public const string Live = "live";
    }

    /// <summary>
    /// Unique webhook identifier.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// URL to receive webhook events.
    /// </summary>
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// List of subscribed event types.
    /// </summary>
    [JsonPropertyName("events")]
    public List<string> Events { get; set; } = new();

    /// <summary>
    /// Event mode filter (all, test, live).
    /// </summary>
    [JsonPropertyName("mode")]
    public string Mode { get; set; } = Modes.All;

    /// <summary>
    /// Whether the webhook is active.
    /// </summary>
    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Number of consecutive failures.
    /// </summary>
    [JsonPropertyName("failure_count")]
    public int FailureCount { get; set; }

    /// <summary>
    /// Circuit breaker state.
    /// </summary>
    [JsonPropertyName("circuit_state")]
    public string CircuitState { get; set; } = CircuitStates.Closed;

    /// <summary>
    /// API version for webhook payloads.
    /// </summary>
    [JsonPropertyName("api_version")]
    public string? ApiVersion { get; set; }

    /// <summary>
    /// Total number of delivery attempts.
    /// </summary>
    [JsonPropertyName("total_deliveries")]
    public int TotalDeliveries { get; set; }

    /// <summary>
    /// Number of successful deliveries.
    /// </summary>
    [JsonPropertyName("successful_deliveries")]
    public int SuccessfulDeliveries { get; set; }

    /// <summary>
    /// Success rate percentage.
    /// </summary>
    [JsonPropertyName("success_rate")]
    public double SuccessRate { get; set; }

    /// <summary>
    /// Timestamp of last delivery attempt.
    /// </summary>
    [JsonPropertyName("last_delivery_at")]
    public DateTime? LastDeliveryAt { get; set; }

    /// <summary>
    /// Creation timestamp.
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last update timestamp.
    /// </summary>
    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Whether the webhook is healthy (active and circuit closed).
    /// </summary>
    public bool IsHealthy => IsActive && CircuitState == CircuitStates.Closed;

    /// <summary>
    /// Whether the circuit breaker is open.
    /// </summary>
    public bool IsCircuitOpen => CircuitState == CircuitStates.Open;

    /// <summary>
    /// Creates a Webhook from a JSON element.
    /// </summary>
    internal static Webhook FromJson(JsonElement element, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<Webhook>(element.GetRawText(), options)
            ?? new Webhook();
    }
}

/// <summary>
/// Response from creating a webhook (includes secret).
/// </summary>
public class WebhookCreatedResponse
{
    /// <summary>
    /// The created webhook.
    /// </summary>
    [JsonPropertyName("webhook")]
    public Webhook Webhook { get; set; } = new();

    /// <summary>
    /// The webhook secret for signature verification.
    /// </summary>
    [JsonPropertyName("secret")]
    public string Secret { get; set; } = string.Empty;

    /// <summary>
    /// Creates a WebhookCreatedResponse from a JSON element.
    /// </summary>
    internal static WebhookCreatedResponse FromJson(JsonElement element, JsonSerializerOptions options)
    {
        var response = new WebhookCreatedResponse();

        if (element.TryGetProperty("webhook", out var webhookElement))
        {
            response.Webhook = Webhook.FromJson(webhookElement, options);
        }
        else
        {
            response.Webhook = Webhook.FromJson(element, options);
        }

        if (element.TryGetProperty("secret", out var secretElement))
        {
            response.Secret = secretElement.GetString() ?? string.Empty;
        }

        return response;
    }
}

/// <summary>
/// Options for creating a webhook.
/// </summary>
public class CreateWebhookOptions
{
    /// <summary>
    /// URL to receive webhook events.
    /// </summary>
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// List of event types to subscribe to.
    /// </summary>
    [JsonPropertyName("events")]
    public List<string> Events { get; set; } = new();

    /// <summary>
    /// Event mode filter (all, test, live). Live requires verification.
    /// </summary>
    [JsonPropertyName("mode")]
    public string? Mode { get; set; }

    /// <summary>
    /// API version for webhook payloads.
    /// </summary>
    [JsonPropertyName("api_version")]
    public string? ApiVersion { get; set; }
}

/// <summary>
/// Options for updating a webhook.
/// </summary>
public class UpdateWebhookOptions
{
    /// <summary>
    /// New URL to receive webhook events.
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>
    /// New list of event types to subscribe to.
    /// </summary>
    [JsonPropertyName("events")]
    public List<string>? Events { get; set; }

    /// <summary>
    /// Whether the webhook is active.
    /// </summary>
    [JsonPropertyName("is_active")]
    public bool? IsActive { get; set; }

    /// <summary>
    /// Event mode filter (all, test, live).
    /// </summary>
    [JsonPropertyName("mode")]
    public string? Mode { get; set; }
}

/// <summary>
/// Paginated list of webhooks.
/// </summary>
public class WebhookList : IEnumerable<Webhook>
{
    /// <summary>
    /// The webhooks in this page.
    /// </summary>
    public List<Webhook> Data { get; }

    /// <summary>
    /// Total number of webhooks.
    /// </summary>
    public int Total { get; }

    internal WebhookList(JsonDocument response, JsonSerializerOptions options)
    {
        Data = new List<Webhook>();

        var root = response.RootElement;

        if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var element in root.EnumerateArray())
            {
                Data.Add(Webhook.FromJson(element, options));
            }
        }
        else if (root.ValueKind == JsonValueKind.Object)
        {
            if (root.TryGetProperty("webhooks", out var webhooksElement) && webhooksElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var element in webhooksElement.EnumerateArray())
                {
                    Data.Add(Webhook.FromJson(element, options));
                }
            }
            else if (root.TryGetProperty("data", out var dataElement) && dataElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var element in dataElement.EnumerateArray())
                {
                    Data.Add(Webhook.FromJson(element, options));
                }
            }
        }

        if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("total", out var totalElement))
        {
            Total = totalElement.GetInt32();
        }
        else
        {
            Total = Data.Count;
        }
    }

    public IEnumerator<Webhook> GetEnumerator() => Data.GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}
