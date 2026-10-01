using System.Net;
using System.Reflection;
using Sendly.Exceptions;
using Sendly.Models;
using Sendly.Tests.Fixtures;
using Xunit;

namespace Sendly.Tests;

/// <summary>
/// Tests for reading the batch preview the API returns, and for the status of a 422.
/// </summary>
public class BatchPreviewWireTests : IDisposable
{
    private readonly MockHttpMessageHandler _mockHandler;
    private readonly HttpClient _httpClient;
    private readonly SendlyClient _client;

    public BatchPreviewWireTests()
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

    private static string PreviewJson(string keyType, bool hasSufficientCredits, string blockedReason, int optedOutBlocked) => $@"{{
        ""total"": 3,
        ""sendable"": 2,
        ""blocked"": 1,
        ""duplicates"": 0,
        ""creditsNeeded"": 4,
        ""creditBalance"": 100,
        ""hasSufficientCredits"": {(hasSufficientCredits ? "true" : "false")},
        ""pooled"": false,
        ""keyType"": ""{keyType}"",
        ""keyScopes"": [""sms:send"", ""sms:read""],
        ""hasWriteScope"": true,
        ""messagingProfile"": {{
            ""id"": ""mp_1"",
            ""canSendDomestic"": true,
            ""canSendInternational"": false,
            ""verificationStatus"": ""verified"",
            ""verificationType"": ""toll_free""
        }},
        ""byCountry"": {{ ""US"": {{ ""count"": 2, ""credits"": 4, ""allowed"": true }} }},
        ""blockedMessages"": [{{ ""index"": 2, ""to"": ""+15551230003"", ""reason"": ""{blockedReason}"" }}],
        ""compliance"": {{
            ""messageType"": ""marketing"",
            ""optedOutBlocked"": {optedOutBlocked},
            ""shaftBlocked"": 0,
            ""quietHoursBlocked"": 0,
            ""quietHoursRescheduled"": 0,
            ""shaftBlockedMessages"": [],
            ""quietHoursBlockedMessages"": []
        }},
        ""warnings"": [""1 message blocked - contacts opted out (texted STOP)""]
    }}";

    private static SendBatchRequest ThreeMessages() => new(new List<BatchMessageItem>
    {
        new("+15551230001", "Hello"),
        new("+15551230002", "Hello"),
        new("+15551230003", "Hello"),
    });

    [Fact]
    public async Task PreviewBatchAsync_ReadsThePreviewTheApiReturns()
    {
        _mockHandler.QueueSuccessResponse(PreviewJson("live", true, "Contact has opted out (texted STOP)", 1));

        var preview = await _client.Messages.PreviewBatchAsync(ThreeMessages());

        Assert.Equal(3, preview.Total);
        Assert.Equal(2, preview.Sendable);
        Assert.Equal(100, preview.CreditBalance);
        Assert.True(preview.HasSufficientCredits);
        Assert.Equal("live", preview.KeyType);
        Assert.True(preview.HasWriteScope);
        Assert.Single(preview.BlockedMessages);
        Assert.Equal(1, preview.Compliance?.OptedOutBlocked);
        Assert.Equal(3, preview.TotalMessages);
        Assert.Equal(2, preview.WillSend);
        Assert.Equal(100, preview.CurrentBalance);
        Assert.True(preview.HasEnoughCredits);
        Assert.True(preview.CanSend);
        Assert.Equal(1, preview.BlockReasons?["Contact has opted out (texted STOP)"]);
    }

    [Fact]
    public async Task PreviewBatchAsync_CannotSendWhenAMessageIsBlockedForAnotherReason()
    {
        _mockHandler.QueueSuccessResponse(PreviewJson("live", true, "Country GB is not supported", 0));

        var preview = await _client.Messages.PreviewBatchAsync(ThreeMessages());

        Assert.False(preview.CanSend);
    }

    [Fact]
    public async Task PreviewBatchAsync_ATestKeyCanSendWithoutABalance()
    {
        _mockHandler.QueueSuccessResponse(PreviewJson("test", false, "Contact has opted out (texted STOP)", 1));

        var preview = await _client.Messages.PreviewBatchAsync(ThreeMessages());

        Assert.True(preview.CanSend);
    }

    private static string CountsJson(int total, int sendable, int blocked, int optedOutBlocked, bool hasWriteScope, string extra = "") => $@"{{
        ""total"": {total},
        ""sendable"": {sendable},
        ""blocked"": {blocked},
        ""duplicates"": 0,
        ""creditsNeeded"": {sendable * 2},
        ""creditBalance"": 100000,
        ""hasSufficientCredits"": true,
        ""keyType"": ""live"",
        ""keyScopes"": [""sms:send""],
        ""hasWriteScope"": {(hasWriteScope ? "true" : "false")},
        ""blockedMessages"": [],
        ""compliance"": {{ ""messageType"": ""marketing"", ""optedOutBlocked"": {optedOutBlocked}, ""shaftBlocked"": 0, ""quietHoursBlocked"": 0, ""quietHoursRescheduled"": 0 }},
        ""warnings"": []{extra}
    }}";

    [Fact]
    public async Task PreviewBatchAsync_CannotSendABatchOverTheLimit()
    {
        _mockHandler.QueueSuccessResponse(CountsJson(10001, 10001, 0, 0, true));

        var preview = await _client.Messages.PreviewBatchAsync(ThreeMessages());

        Assert.False(preview.CanSend);
    }

    [Fact]
    public async Task PreviewBatchAsync_CannotSendWhenEveryMessageIsOptedOut()
    {
        _mockHandler.QueueSuccessResponse(CountsJson(3, 0, 3, 3, true));

        var preview = await _client.Messages.PreviewBatchAsync(ThreeMessages());

        Assert.False(preview.CanSend);
    }

    [Fact]
    public async Task PreviewBatchAsync_CannotSendWithoutTheSendScope()
    {
        _mockHandler.QueueSuccessResponse(CountsJson(3, 3, 0, 0, false));

        var preview = await _client.Messages.PreviewBatchAsync(ThreeMessages());

        Assert.False(preview.CanSend);
    }

    [Fact]
    public async Task PreviewBatchAsync_KeepsValuesTheApiSendsItself()
    {
        _mockHandler.QueueSuccessResponse(CountsJson(3, 3, 0, 0, true, @", ""canSend"": false, ""totalMessages"": 5"));

        var preview = await _client.Messages.PreviewBatchAsync(ThreeMessages());

        Assert.False(preview.CanSend);
        Assert.Equal(5, preview.TotalMessages);
    }

    [Fact]
    public async Task A422_KeepsItsStatus()
    {
        _mockHandler.QueueResponse(new HttpResponseMessage(HttpStatusCode.UnprocessableEntity)
        {
            Content = new StringContent(
                @"{""error"":""idempotency_key_mismatch"",""message"":""This idempotency key was already used with a different request body. Use a new key for different requests.""}",
                System.Text.Encoding.UTF8,
                "application/json")
        });

        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _client.Messages.SendAsync("+15551234567", "Hello"));

        Assert.Equal(422, exception.StatusCode);
        Assert.Equal("idempotency_key_mismatch", exception.ApiErrorCode);
    }
}
