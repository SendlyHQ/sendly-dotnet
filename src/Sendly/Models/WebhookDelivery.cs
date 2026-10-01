using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sendly.Models;

/// <summary>
/// Represents a webhook delivery attempt.
/// </summary>
public class WebhookDelivery
{
    /// <summary>
    /// Unique delivery identifier.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Webhook ID this delivery belongs to.
    /// </summary>
    [JsonPropertyName("webhook_id")]
    public string WebhookId { get; set; } = string.Empty;

    /// <summary>
    /// Event type that triggered this delivery.
    /// </summary>
    [JsonPropertyName("event_type")]
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// HTTP status code from the endpoint.
    /// </summary>
    [JsonPropertyName("http_status")]
    public int HttpStatus { get; set; }

    /// <summary>
    /// Whether the delivery was successful.
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>
    /// Attempt number (1-based).
    /// </summary>
    [JsonPropertyName("attempt_number")]
    public int AttemptNumber { get; set; } = 1;

    /// <summary>
    /// Error message if the delivery failed.
    /// </summary>
    [JsonPropertyName("error_message")]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Response time in milliseconds.
    /// </summary>
    [JsonPropertyName("response_time_ms")]
    public int ResponseTimeMs { get; set; }

    /// <summary>
    /// Timestamp of the delivery attempt.
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Creates a WebhookDelivery from a JSON element.
    /// </summary>
    internal static WebhookDelivery FromJson(JsonElement element, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<WebhookDelivery>(element.GetRawText(), options)
            ?? new WebhookDelivery();
    }
}

/// <summary>
/// Paginated list of webhook deliveries.
/// </summary>
public class WebhookDeliveryList : IEnumerable<WebhookDelivery>
{
    /// <summary>
    /// The deliveries in this page.
    /// </summary>
    public List<WebhookDelivery> Data { get; }

    /// <summary>
    /// Total number of deliveries.
    /// </summary>
    public int Total { get; }

    /// <summary>
    /// Whether there are more deliveries.
    /// </summary>
    public bool HasMore { get; }

    internal WebhookDeliveryList(JsonDocument response, JsonSerializerOptions options)
    {
        Data = new List<WebhookDelivery>();

        var root = response.RootElement;

        if (root.TryGetProperty("deliveries", out var deliveriesElement) && deliveriesElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var element in deliveriesElement.EnumerateArray())
            {
                Data.Add(WebhookDelivery.FromJson(element, options));
            }
        }
        else if (root.TryGetProperty("data", out var dataElement) && dataElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var element in dataElement.EnumerateArray())
            {
                Data.Add(WebhookDelivery.FromJson(element, options));
            }
        }
        else if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var element in root.EnumerateArray())
            {
                Data.Add(WebhookDelivery.FromJson(element, options));
            }
        }

        if (root.TryGetProperty("total", out var totalElement))
        {
            Total = totalElement.GetInt32();
        }
        else
        {
            Total = Data.Count;
        }

        if (root.TryGetProperty("has_more", out var hasMoreElement))
        {
            HasMore = hasMoreElement.GetBoolean();
        }
    }

    public IEnumerator<WebhookDelivery> GetEnumerator() => Data.GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}

/// <summary>
/// Options for listing webhook deliveries.
/// </summary>
public class ListDeliveriesOptions
{
    /// <summary>
    /// Maximum number of deliveries to return.
    /// </summary>
    public int? Limit { get; set; }

    /// <summary>
    /// Number of deliveries to skip.
    /// </summary>
    public int? Offset { get; set; }

    internal Dictionary<string, string> ToQueryParams()
    {
        var result = new Dictionary<string, string>();

        if (Limit.HasValue)
            result["limit"] = Math.Min(Limit.Value, 100).ToString();
        if (Offset.HasValue)
            result["offset"] = Offset.Value.ToString();

        return result;
    }
}

/// <summary>
/// Result from testing a webhook.
/// </summary>
public class WebhookTestResult
{
    /// <summary>
    /// Whether the test was successful.
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>
    /// What happened, such as <c>Test webhook delivered successfully in 123ms</c>.
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    /// <summary>
    /// HTTP status code from the endpoint.
    /// </summary>
    [JsonPropertyName("status_code")]
    public int StatusCode { get; set; }

    /// <summary>
    /// Response time in milliseconds.
    /// </summary>
    [JsonPropertyName("response_time_ms")]
    public int ResponseTimeMs { get; set; }

    /// <summary>
    /// Error message if the test failed.
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    /// <summary>
    /// Creates a WebhookTestResult from a JSON element.
    /// </summary>
    internal static WebhookTestResult FromJson(JsonElement element, JsonSerializerOptions options)
    {
        var result = JsonSerializer.Deserialize<WebhookTestResult>(element.GetRawText(), options)
            ?? new WebhookTestResult();

        if (element.ValueKind == JsonValueKind.Object &&
            element.TryGetProperty("delivery", out var delivery) &&
            delivery.ValueKind == JsonValueKind.Object)
        {
            if (delivery.TryGetProperty("status_code", out var statusCode) &&
                statusCode.ValueKind == JsonValueKind.Number &&
                statusCode.TryGetInt32(out var code))
                result.StatusCode = code;
            if (delivery.TryGetProperty("response_time", out var responseTime) &&
                responseTime.ValueKind == JsonValueKind.Number)
                result.ResponseTimeMs = (int)Math.Round(responseTime.GetDouble());
            if (delivery.TryGetProperty("error", out var error) && error.ValueKind == JsonValueKind.String)
                result.Error = error.GetString();
        }

        return result;
    }
}

/// <summary>
/// Response from rotating a webhook secret.
/// </summary>
public class WebhookSecretRotation
{
    /// <summary>
    /// The new webhook secret.
    /// </summary>
    [JsonPropertyName("secret")]
    public string Secret { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp of the rotation.
    /// </summary>
    [JsonPropertyName("rotated_at")]
    public DateTime RotatedAt { get; set; }

    /// <summary>
    /// Creates a WebhookSecretRotation from a JSON element.
    /// </summary>
    internal static WebhookSecretRotation FromJson(JsonElement element, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<WebhookSecretRotation>(element.GetRawText(), options)
            ?? new WebhookSecretRotation();
    }
}
