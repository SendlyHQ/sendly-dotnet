using System.Net;
using System.Reflection;
using Sendly.Exceptions;
using Sendly.Models;
using Sendly.Resources;
using Sendly.Tests.Fixtures;
using Xunit;

namespace Sendly.Tests;

/// <summary>
/// Tests for WebhooksResource against the bodies the webhook endpoints send.
/// </summary>
public class WebhooksResourceTests : IDisposable
{
    private readonly MockHttpMessageHandler _mockHandler;
    private readonly HttpClient _httpClient;
    private readonly SendlyClient _client;

    public WebhooksResourceTests()
    {
        _mockHandler = new MockHttpMessageHandler();
        _httpClient = new HttpClient(_mockHandler)
        {
            BaseAddress = new Uri("https://api.test.com")
        };

        _client = new SendlyClient("test_api_key");
        var httpClientField = typeof(SendlyClient).GetField("_httpClient", BindingFlags.NonPublic | BindingFlags.Instance);
        httpClientField?.SetValue(_client, _httpClient);
    }

    public void Dispose()
    {
        _client?.Dispose();
        _httpClient?.Dispose();
        _mockHandler?.Dispose();
    }

    private static string WebhookJson(string id, string url) => $@"{{
        ""id"": ""{id}"",
        ""url"": ""{url}"",
        ""events"": [""message.delivered"", ""message.failed""],
        ""mode"": ""all"",
        ""is_active"": true,
        ""failure_count"": 0,
        ""circuit_state"": ""closed"",
        ""api_version"": ""2024-01-01"",
        ""metadata"": {{}},
        ""created_at"": ""2026-09-01T00:00:00.000Z"",
        ""updated_at"": ""2026-09-02T00:00:00.000Z"",
        ""total_deliveries"": 10,
        ""successful_deliveries"": 9,
        ""success_rate"": 90,
        ""last_delivery_at"": null
    }}";

    [Fact]
    public async Task ListAsync_ReadsTheBareArrayTheApiSends()
    {
        _mockHandler.QueueSuccessResponse(
            $"[{WebhookJson("whk_1", "https://example.com/a")},{WebhookJson("whk_2", "https://example.com/b")}]");

        var webhooks = await _client.Webhooks.ListAsync();

        Assert.Equal(2, webhooks.Data.Count);
        Assert.Equal(2, webhooks.Total);
        Assert.Equal("whk_1", webhooks.Data[0].Id);
        Assert.Equal("https://example.com/b", webhooks.Data[1].Url);
        Assert.Equal(9, webhooks.Data[0].SuccessfulDeliveries);
    }

    [Fact]
    public async Task ListAsync_ReadsMetadataThatIsNotAnObjectAsNull()
    {
        _mockHandler.QueueSuccessResponse(
            @"[{ ""id"": ""whk_1"", ""url"": ""https://example.com/a"", ""events"": [], ""metadata"": [""a"", 1] },
               { ""id"": ""whk_2"", ""url"": ""https://example.com/b"", ""events"": [], ""metadata"": ""note"" },
               { ""id"": ""whk_3"", ""url"": ""https://example.com/c"", ""events"": [], ""metadata"": { ""team"": ""ops"" } }]");

        var webhooks = await _client.Webhooks.ListAsync();

        Assert.Equal(3, webhooks.Data.Count);
        Assert.Null(webhooks.Data[0].Metadata);
        Assert.Null(webhooks.Data[1].Metadata);
        Assert.Equal("ops", webhooks.Data[2].Metadata!["team"].ToString());
    }

    [Fact]
    public async Task ListAsync_WithNoWebhooks_ReturnsAnEmptyList()
    {
        _mockHandler.QueueSuccessResponse("[]");

        var webhooks = await _client.Webhooks.ListAsync();

        Assert.Empty(webhooks.Data);
        Assert.Equal(0, webhooks.Total);
    }

    [Fact]
    public async Task UpdateAsync_WithOnlyIsActive_SendsOnlyIsActive()
    {
        _mockHandler.QueueSuccessResponse(WebhookJson("whk_1", "https://example.com/a").Replace(@"""is_active"": true", @"""is_active"": false"));

        var webhook = await _client.Webhooks.UpdateAsync("whk_1", new UpdateWebhookOptions { IsActive = false });

        Assert.Equal("{\"is_active\":false}", await _mockHandler.LastRequest!.Content!.ReadAsStringAsync());
        Assert.False(webhook.IsActive);
    }

    [Fact]
    public async Task CreateAsync_SendsDescriptionModeAndMetadata()
    {
        _mockHandler.QueueSuccessResponse(@"{
            ""id"": ""whk_1"",
            ""url"": ""https://example.com/a"",
            ""events"": [""message.delivered""],
            ""description"": ""Order updates"",
            ""mode"": ""live"",
            ""is_active"": true,
            ""metadata"": { ""team"": ""ops"" },
            ""secret"": ""whsec_abc""
        }");

        var created = await _client.Webhooks.CreateAsync(new CreateWebhookOptions
        {
            Url = "https://example.com/a",
            Events = new List<string> { "message.delivered" },
            Description = "Order updates",
            Mode = Webhook.Modes.Live,
            Metadata = new Dictionary<string, object> { ["team"] = "ops" },
        });

        using var body = System.Text.Json.JsonDocument.Parse(await _mockHandler.LastRequest!.Content!.ReadAsStringAsync());
        Assert.Equal("Order updates", body.RootElement.GetProperty("description").GetString());
        Assert.Equal("live", body.RootElement.GetProperty("mode").GetString());
        Assert.Equal("ops", body.RootElement.GetProperty("metadata").GetProperty("team").GetString());
        Assert.Equal("Order updates", created.Webhook.Description);
        Assert.Equal("ops", created.Webhook.Metadata!["team"].ToString());
        Assert.Equal("whsec_abc", created.Secret);
    }

    [Fact]
    public async Task CreateAsync_WithoutDescriptionOrMetadata_LeavesThemOut()
    {
        _mockHandler.QueueSuccessResponse(WebhookJson("whk_1", "https://example.com/a"));

        await _client.Webhooks.CreateAsync("https://example.com/a", new List<string> { "message.delivered" });

        using var body = System.Text.Json.JsonDocument.Parse(await _mockHandler.LastRequest!.Content!.ReadAsStringAsync());
        Assert.False(body.RootElement.TryGetProperty("description", out _));
        Assert.False(body.RootElement.TryGetProperty("metadata", out _));
    }

    [Fact]
    public async Task UpdateAsync_SendsDescriptionAndMetadata()
    {
        _mockHandler.QueueSuccessResponse(WebhookJson("whk_1", "https://example.com/a"));

        await _client.Webhooks.UpdateAsync("whk_1", new UpdateWebhookOptions
        {
            Description = "Billing",
            Metadata = new Dictionary<string, object> { ["team"] = "billing" },
        });

        Assert.Equal(
            "{\"description\":\"Billing\",\"metadata\":{\"team\":\"billing\"}}",
            await _mockHandler.LastRequest!.Content!.ReadAsStringAsync());
    }

    [Fact]
    public async Task UpdateAsync_WithNullDescriptionAndMetadata_SendsNullsToClearThem()
    {
        _mockHandler.QueueSuccessResponse(WebhookJson("whk_1", "https://example.com/a"));

        await _client.Webhooks.UpdateAsync("whk_1", new UpdateWebhookOptions { Description = null, Metadata = null });

        Assert.Equal(
            "{\"description\":null,\"metadata\":null}",
            await _mockHandler.LastRequest!.Content!.ReadAsStringAsync());
    }

    [Fact]
    public async Task ListEventTypesAsync_ReadsTheEventTypes()
    {
        _mockHandler.QueueSuccessResponse(@"{
            ""events"": [
                { ""type"": ""message.sent"", ""description"": ""A message was sent"" },
                { ""type"": ""message.delivered"", ""description"": ""A message was delivered"" }
            ]
        }");

        var types = await _client.Webhooks.ListEventTypesAsync();

        Assert.Equal(new[] { "message.sent", "message.delivered" }, types);
        Assert.EndsWith("webhooks/event-types", _mockHandler.LastRequest!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task ListDeliveriesAsync_ReadsTheDeliveriesEnvelope()
    {
        _mockHandler.QueueSuccessResponse(@"{
            ""deliveries"": [{
                ""id"": ""del_1"",
                ""webhook_id"": ""whk_1"",
                ""event_id"": ""evt_abc"",
                ""event_type"": ""message.delivered"",
                ""status"": ""delivered"",
                ""success"": true,
                ""response_status_code"": 200,
                ""http_status"": 200,
                ""response_time"": 12,
                ""response_time_ms"": 12,
                ""response_body"": ""ok"",
                ""error_message"": null,
                ""error_code"": null,
                ""attempt_number"": 1,
                ""max_attempts"": 5,
                ""next_retry_at"": null,
                ""created_at"": ""2026-09-25T10:00:00.000Z"",
                ""delivered_at"": ""2026-09-25T10:00:00.012Z""
            }],
            ""pagination"": { ""limit"": 50, ""offset"": 0 }
        }");

        var deliveries = await _client.Webhooks.ListDeliveriesAsync("whk_1", new ListDeliveriesOptions { Offset = 50 });

        var delivery = Assert.Single(deliveries.Data);
        Assert.Equal("message.delivered", delivery.EventType);
        Assert.Equal(1, delivery.AttemptNumber);
        Assert.Equal(200, delivery.HttpStatus);
        Assert.True(delivery.Success);
        Assert.Contains("offset=50", _mockHandler.LastRequest!.RequestUri!.Query);
    }

    [Fact]
    public async Task TestAsync_ReadsTheTestDeliveryTheApiSends()
    {
        _mockHandler.QueueSuccessResponse(@"{
            ""success"": true,
            ""message"": ""Test webhook delivered successfully in 123ms"",
            ""delivery"": {
                ""id"": ""del_1"",
                ""delivery_id"": ""del_1"",
                ""webhook_url"": ""https://example.com/a"",
                ""event_type"": ""webhook.test"",
                ""status"": ""delivered"",
                ""response_time"": 123,
                ""status_code"": 200,
                ""response_body"": ""ok"",
                ""delivered_at"": ""2026-09-25T10:00:00.000Z""
            }
        }");

        var result = await _client.Webhooks.TestAsync("whk_1");

        Assert.True(result.Success);
        Assert.Equal(200, result.StatusCode);
        Assert.Equal(123, result.ResponseTimeMs);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task TestAsync_WhenTheEndpointFails_StillThrowsValidationException()
    {
        _mockHandler.QueueResponse(HttpStatusCode.BadRequest,
            @"{""success"": false, ""message"": ""Test webhook failed: connect ECONNREFUSED""}");

        var exception = await Assert.ThrowsAsync<ValidationException>(() => _client.Webhooks.TestAsync("whk_1"));

        Assert.Equal("Test webhook failed: connect ECONNREFUSED", exception.Message);
        Assert.Single(_mockHandler.Requests);
    }

    private const string RecoveryPreviewJson = @"{
        ""circuitState"": ""open"",
        ""circuitOpenedAt"": ""2026-09-25T08:00:00.000Z"",
        ""failureCount"": 12,
        ""auditFailures"": {
            ""total"": 3,
            ""byStatus"": { ""failed"": 2, ""cancelled"": 1 },
            ""byEventType"": { ""message.delivered"": 2, ""message.opt_out"": 1 }
        },
        ""missingFromMessages"": { ""total"": 4, ""byEventType"": { ""message.sent"": 4 } },
        ""histogram"": [{ ""bucket"": ""2026-09-25T08:00:00.000Z"", ""missing"": 4, ""failed"": 3 }],
        ""samples"": [{ ""id"": ""msg_1"", ""toMasked"": ""+1555***4567"", ""eventType"": ""message.delivered"", ""occurredAt"": ""2026-09-25T08:30:00.000Z"" }],
        ""windowSuggestion"": { ""since"": ""2026-09-25T08:00:00.000Z"", ""until"": ""2026-09-25T10:00:00.000Z"" },
        ""since"": ""2026-09-24T10:00:00.000Z"",
        ""until"": ""2026-09-25T10:00:00.000Z""
    }";

    [Fact]
    public async Task RecoveryPreviewAsync_SendsTheWindowAndReadsTheCamelCasePreview()
    {
        _mockHandler.QueueSuccessResponse(RecoveryPreviewJson);

        var preview = await _client.Webhooks.RecoveryPreviewAsync("whk_1", new RecoveryPreviewOptions
        {
            Since = "2026-09-24T10:00:00.000Z",
            Until = "2026-09-25T10:00:00.000Z",
            EventTypes = new List<string> { "message.delivered", "message.failed" }
        });

        var request = _mockHandler.LastRequest!;
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal("/webhooks/whk_1/recovery-preview", request.RequestUri!.AbsolutePath);
        var query = System.Web.HttpUtility.ParseQueryString(request.RequestUri.Query);
        Assert.Equal("2026-09-24T10:00:00.000Z", query["since"]);
        Assert.Equal("2026-09-25T10:00:00.000Z", query["until"]);
        Assert.Equal("message.delivered,message.failed", query["event_types"]);

        Assert.Equal("open", preview.CircuitState);
        Assert.True(preview.IsCircuitOpen);
        Assert.Equal("2026-09-25T08:00:00.000Z", preview.CircuitOpenedAt);
        Assert.Equal(12, preview.FailureCount);
        Assert.Equal(3, preview.AuditFailures.Total);
        Assert.Equal(1, preview.AuditFailures.ByStatus["cancelled"]);
        Assert.Equal(1, preview.AuditFailures.ByEventType["message.opt_out"]);
        Assert.Equal(4, preview.MissingFromMessages.ByEventType["message.sent"]);
        Assert.Equal(3, preview.Histogram[0].Failed);
        Assert.Equal("+1555***4567", preview.Samples[0].ToMasked);
        Assert.Equal("2026-09-25T08:30:00.000Z", preview.Samples[0].OccurredAt);
        Assert.Equal("2026-09-25T08:00:00.000Z", preview.WindowSuggestion!.Since);
        Assert.Equal("2026-09-25T10:00:00.000Z", preview.Until);
    }

    [Fact]
    public async Task RecoveryPreviewAsync_WithoutOptions_SendsNoQuery()
    {
        _mockHandler.QueueSuccessResponse(RecoveryPreviewJson
            .Replace(@"""circuitOpenedAt"": ""2026-09-25T08:00:00.000Z""", @"""circuitOpenedAt"": null")
            .Replace(@"""windowSuggestion"": { ""since"": ""2026-09-25T08:00:00.000Z"", ""until"": ""2026-09-25T10:00:00.000Z"" }", @"""windowSuggestion"": null"));

        var preview = await _client.Webhooks.RecoveryPreviewAsync("whk_1");

        Assert.Equal("", _mockHandler.LastRequest!.RequestUri!.Query);
        Assert.Null(preview.CircuitOpenedAt);
        Assert.Null(preview.WindowSuggestion);
    }
}
