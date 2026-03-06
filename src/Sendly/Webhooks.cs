using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sendly;

/// <summary>
/// Webhook utilities for verifying and parsing Sendly webhook events.
/// </summary>
/// <example>
/// <code>
/// // In your ASP.NET Core controller
/// [HttpPost("/webhooks/sendly")]
/// public IActionResult HandleWebhook(
///     [FromBody] string payload,
///     [FromHeader(Name = "X-Sendly-Signature")] string signature,
///     [FromHeader(Name = "X-Sendly-Timestamp")] string? timestamp = null)
/// {
///     try
///     {
///         var webhookEvent = Webhooks.ParseEvent(payload, signature, _webhookSecret, timestamp);
///
///         switch (webhookEvent.Type)
///         {
///             case "message.delivered":
///                 Console.WriteLine($"Message delivered: {webhookEvent.Data.Id}");
///                 break;
///             case "message.failed":
///                 Console.WriteLine($"Message failed: {webhookEvent.Data.Error}");
///                 break;
///         }
///
///         return Ok();
///     }
///     catch (WebhookSignatureException)
///     {
///         return Unauthorized();
///     }
/// }
/// </code>
/// </example>
public static class Webhooks
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    private const int SignatureToleranceSeconds = 300;

    /// <summary>
    /// Verify webhook signature from Sendly.
    /// </summary>
    /// <param name="payload">Raw request body as string</param>
    /// <param name="signature">X-Sendly-Signature header value</param>
    /// <param name="secret">Your webhook secret from dashboard</param>
    /// <param name="timestamp">X-Sendly-Timestamp header value (null to skip timestamp check)</param>
    /// <returns>True if signature is valid, false otherwise</returns>
    public static bool VerifySignature(string payload, string signature, string secret, string? timestamp = null)
    {
        if (string.IsNullOrEmpty(payload) || string.IsNullOrEmpty(signature) || string.IsNullOrEmpty(secret))
        {
            return false;
        }

        string signedPayload;
        if (!string.IsNullOrEmpty(timestamp))
        {
            signedPayload = $"{timestamp}.{payload}";
            if (long.TryParse(timestamp, out var ts))
            {
                var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                if (Math.Abs(now - ts) > SignatureToleranceSeconds)
                {
                    return false;
                }
            }
        }
        else
        {
            signedPayload = payload;
        }

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signedPayload));
        var expected = "sha256=" + Convert.ToHexString(hash).ToLowerInvariant();

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(signature)
        );
    }

    /// <summary>
    /// Parse and validate a webhook event.
    /// </summary>
    /// <param name="payload">Raw request body as string</param>
    /// <param name="signature">X-Sendly-Signature header value</param>
    /// <param name="secret">Your webhook secret from dashboard</param>
    /// <param name="timestamp">X-Sendly-Timestamp header value (null to skip timestamp check)</param>
    /// <returns>Parsed and validated WebhookEvent</returns>
    /// <exception cref="WebhookSignatureException">If signature is invalid or payload is malformed</exception>
    public static WebhookEvent ParseEvent(string payload, string signature, string secret, string? timestamp = null)
    {
        if (!VerifySignature(payload, signature, secret, timestamp))
        {
            throw new WebhookSignatureException("Invalid webhook signature");
        }

        try
        {
            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;

            if (!root.TryGetProperty("id", out _) || !root.TryGetProperty("type", out _) ||
                !root.TryGetProperty("data", out var dataElement))
            {
                throw new WebhookSignatureException("Invalid event structure");
            }

            JsonElement msgElement;
            if (dataElement.TryGetProperty("object", out var objectElement))
            {
                msgElement = objectElement;
            }
            else
            {
                msgElement = dataElement;
            }

            var msgData = new WebhookMessageData
            {
                Id = GetStringOrDefault(msgElement, "id", GetStringOrDefault(msgElement, "message_id", "")),
                Status = GetStringOrDefault(msgElement, "status", ""),
                To = GetStringOrDefault(msgElement, "to", ""),
                From = GetStringOrDefault(msgElement, "from", ""),
                Direction = GetStringOrDefault(msgElement, "direction", "outbound"),
                OrganizationId = GetStringOrDefault(msgElement, "organization_id", null),
                Text = GetStringOrDefault(msgElement, "text", null),
                Error = GetStringOrDefault(msgElement, "error", null),
                ErrorCode = GetStringOrDefault(msgElement, "error_code", null),
                DeliveredAt = GetStringOrDefault(msgElement, "delivered_at", null),
                FailedAt = GetStringOrDefault(msgElement, "failed_at", null),
                Segments = GetIntOrDefault(msgElement, "segments", 1),
                CreditsUsed = GetIntOrDefault(msgElement, "credits_used", 0),
                MessageFormat = GetStringOrDefault(msgElement, "message_format", null)
            };

            var webhookEvent = new WebhookEvent
            {
                Id = root.GetProperty("id").GetString() ?? "",
                Type = root.GetProperty("type").GetString() ?? "",
                Data = msgData,
                ApiVersion = GetStringOrDefault(root, "api_version", "2024-01"),
                Livemode = root.TryGetProperty("livemode", out var lm) && lm.GetBoolean()
            };

            if (root.TryGetProperty("created", out var createdEl))
            {
                webhookEvent.Created = createdEl;
            }
            else if (root.TryGetProperty("created_at", out var createdAtEl))
            {
                webhookEvent.Created = createdAtEl;
            }

            return webhookEvent;
        }
        catch (JsonException ex)
        {
            throw new WebhookSignatureException($"Failed to parse webhook payload: {ex.Message}");
        }
    }

    /// <summary>
    /// Generate a webhook signature for testing purposes.
    /// </summary>
    /// <param name="payload">The payload to sign</param>
    /// <param name="secret">The secret to use for signing</param>
    /// <param name="timestamp">Optional timestamp to include in signature</param>
    /// <returns>The signature in the format "sha256=..."</returns>
    public static string GenerateSignature(string payload, string secret, string? timestamp = null)
    {
        var signedPayload = !string.IsNullOrEmpty(timestamp) ? $"{timestamp}.{payload}" : payload;
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signedPayload));
        return "sha256=" + Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string? GetStringOrDefault(JsonElement element, string property, string? defaultValue)
    {
        if (element.TryGetProperty(property, out var prop) && prop.ValueKind == JsonValueKind.String)
        {
            return prop.GetString();
        }
        return defaultValue;
    }

    private static int GetIntOrDefault(JsonElement element, string property, int defaultValue)
    {
        if (element.TryGetProperty(property, out var prop) && prop.ValueKind == JsonValueKind.Number)
        {
            return prop.GetInt32();
        }
        return defaultValue;
    }
}

/// <summary>
/// Webhook event from Sendly.
/// </summary>
public class WebhookEvent
{
    /// <summary>Unique event ID</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Event type</summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>Event data</summary>
    [JsonPropertyName("data")]
    public WebhookMessageData Data { get; set; } = new();

    /// <summary>When the event was created (unix timestamp or ISO 8601)</summary>
    [JsonIgnore]
    public JsonElement? Created { get; set; }

    /// <summary>API version</summary>
    [JsonPropertyName("api_version")]
    public string ApiVersion { get; set; } = "2024-01";

    /// <summary>Whether this is a live (production) event</summary>
    [JsonPropertyName("livemode")]
    public bool Livemode { get; set; }

    /// <summary>Backwards-compatible alias for Created</summary>
    [JsonPropertyName("created_at")]
    [Obsolete("Use Created instead")]
    public string? CreatedAt => Created?.ToString();
}

/// <summary>
/// Webhook message data.
/// </summary>
public class WebhookMessageData
{
    /// <summary>The message ID</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Backwards-compatible alias for Id</summary>
    [Obsolete("Use Id instead")]
    public string MessageId => Id;

    /// <summary>Current message status</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>Recipient phone number</summary>
    [JsonPropertyName("to")]
    public string To { get; set; } = string.Empty;

    /// <summary>Sender ID or phone number</summary>
    [JsonPropertyName("from")]
    public string From { get; set; } = string.Empty;

    /// <summary>Message direction</summary>
    [JsonPropertyName("direction")]
    public string Direction { get; set; } = "outbound";

    /// <summary>Organization ID</summary>
    [JsonPropertyName("organization_id")]
    public string? OrganizationId { get; set; }

    /// <summary>Message text</summary>
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    /// <summary>Error message if status is 'failed' or 'undelivered'</summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    /// <summary>Error code if available</summary>
    [JsonPropertyName("error_code")]
    public string? ErrorCode { get; set; }

    /// <summary>When the message was delivered</summary>
    [JsonPropertyName("delivered_at")]
    public string? DeliveredAt { get; set; }

    /// <summary>When the message failed</summary>
    [JsonPropertyName("failed_at")]
    public string? FailedAt { get; set; }

    /// <summary>Number of SMS segments</summary>
    [JsonPropertyName("segments")]
    public int Segments { get; set; } = 1;

    /// <summary>Credits charged</summary>
    [JsonPropertyName("credits_used")]
    public int CreditsUsed { get; set; }

    /// <summary>Message format (sms or mms)</summary>
    [JsonPropertyName("message_format")]
    public string? MessageFormat { get; set; }
}

/// <summary>
/// Exception thrown when webhook signature verification fails.
/// </summary>
public class WebhookSignatureException : Exception
{
    public WebhookSignatureException(string message = "Invalid webhook signature")
        : base(message)
    {
    }
}
