using System.Net;
using System.Reflection;
using Sendly.Exceptions;
using Sendly.Tests.Fixtures;
using Xunit;

namespace Sendly.Tests;

/// <summary>
/// Tests for how the client retries the 429s the API-key checks answer with.
/// </summary>
public class RateLimitRetryTests : IDisposable
{
    private const string CreditsJson = @"{""balance"":10,""reservedBalance"":0,""availableBalance"":10,""billingMode"":""prepaid"",""recentTransactions"":[]}";

    private const string MessageJson = @"{
        ""id"": ""msg_1"",
        ""to"": ""+15551234567"",
        ""from"": ""SENDLY"",
        ""text"": ""Hello"",
        ""status"": ""queued"",
        ""direction"": ""outbound"",
        ""error"": null,
        ""segments"": 1,
        ""creditsUsed"": 2,
        ""senderType"": ""number_pool"",
        ""createdAt"": ""2026-09-25T10:00:00.000Z"",
        ""metadata"": {}
    }";

    private readonly MockHttpMessageHandler _mockHandler;
    private readonly HttpClient _httpClient;
    private readonly SendlyClient _client;

    public RateLimitRetryTests()
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

    private static HttpResponseMessage TooManyRequests(string error, string message, int retryAfter)
    {
        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests)
        {
            Content = new StringContent(
                $@"{{""error"":""{error}"",""message"":""{message}"",""retryAfter"":{retryAfter}}}",
                System.Text.Encoding.UTF8,
                "application/json")
        };
        response.Headers.Add("Retry-After", retryAfter.ToString());
        return response;
    }

    private string? KeyOfRequest(int index)
    {
        return _mockHandler.Requests[index].Headers.TryGetValues("Idempotency-Key", out var values)
            ? values.FirstOrDefault()
            : null;
    }

    [Fact]
    public async Task TooManyFailedKeyAttempts_IsThrownOnTheFirstAttempt()
    {
        _mockHandler.QueueResponse(TooManyRequests(
            "too_many_failed_key_attempts",
            "Too many failed API key attempts. Try again in 1 seconds.",
            1));
        _mockHandler.QueueSuccessResponse(CreditsJson);

        var exception = await Assert.ThrowsAsync<RateLimitException>(() => _client.Account.GetCreditsAsync());

        Assert.Equal("too_many_failed_key_attempts", exception.ApiErrorCode);
        Assert.Equal("Too many failed API key attempts. Try again in 1 seconds.", exception.Message);
        Assert.Equal(TimeSpan.FromSeconds(1), exception.RetryAfter);
        Assert.Equal(429, exception.StatusCode);
        Assert.Single(_mockHandler.Requests);
    }

    [Fact]
    public async Task TooManyFailedKeyAttempts_OnAPost_IsThrownOnTheFirstAttempt()
    {
        _mockHandler.QueueResponse(TooManyRequests(
            "too_many_failed_key_attempts",
            "Too many failed API key attempts. Try again in 300 seconds.",
            300));
        _mockHandler.QueueResponse(HttpStatusCode.Created, MessageJson);

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        var exception = await Assert.ThrowsAsync<RateLimitException>(
            () => _client.Messages.SendAsync(new Models.SendMessageRequest("+15551234567", "Hello"), timeout.Token));

        Assert.Equal("too_many_failed_key_attempts", exception.ApiErrorCode);
        Assert.Equal(TimeSpan.FromSeconds(300), exception.RetryAfter);
        Assert.Single(_mockHandler.Requests);
    }

    [Fact]
    public async Task TooManyConcurrentVerifications_IsRetriedWithTheSameIdempotencyKey()
    {
        _mockHandler.QueueResponse(TooManyRequests(
            "too_many_concurrent_verifications",
            "Too many API key checks are already running for this account from this address. Try again in 1 second.",
            1));
        _mockHandler.QueueResponse(HttpStatusCode.Created, MessageJson);

        var message = await _client.Messages.SendAsync("+15551234567", "Hello");

        Assert.Equal("msg_1", message.Id);
        Assert.Equal(2, _mockHandler.Requests.Count);
        Assert.NotNull(KeyOfRequest(0));
        Assert.Equal(KeyOfRequest(0), KeyOfRequest(1));
    }

    private static HttpResponseMessage TooManyRequestsWithoutHeader(string body)
    {
        return new HttpResponseMessage(HttpStatusCode.TooManyRequests)
        {
            Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
        };
    }

    [Fact]
    public async Task RateLimitOverAMinute_IsThrownAtOnce_WithTheBodysRetryAfter()
    {
        for (var i = 0; i < 4; i++)
        {
            _mockHandler.QueueResponse(TooManyRequestsWithoutHeader(
                @"{""error"":""rate_limit_exceeded"",""message"":""Too many OTPs sent to this phone number. Max 5 per 10 minutes."",""retryAfter"":600}"));
        }

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        var exception = await Assert.ThrowsAsync<RateLimitException>(() => _client.Account.GetCreditsAsync(timeout.Token));

        Assert.Equal(TimeSpan.FromSeconds(600), exception.RetryAfter);
        Assert.Single(_mockHandler.Requests);
    }

    [Fact]
    public async Task AnotherFinal429_IsThrownAtOnce()
    {
        for (var i = 0; i < 4; i++)
        {
            _mockHandler.QueueResponse(TooManyRequestsWithoutHeader(
                @"{""error"":""max_attempts_exceeded"",""message"":""Maximum verification attempts exceeded""}"));
        }

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        var exception = await Assert.ThrowsAsync<RateLimitException>(() => _client.Account.GetCreditsAsync(timeout.Token));

        Assert.Equal("max_attempts_exceeded", exception.ApiErrorCode);
        Assert.Single(_mockHandler.Requests);
    }

    [Fact]
    public async Task BusyOverAMinute_IsThrownAtOnce()
    {
        _mockHandler.QueueResponse(TooManyRequests(
            "too_many_concurrent_verifications",
            "Too many API key checks are already running for this account from this address. Try again in 1 second.",
            120));
        _mockHandler.QueueSuccessResponse(CreditsJson);

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var exception = await Assert.ThrowsAsync<RateLimitException>(() => _client.Account.GetCreditsAsync(timeout.Token));

        Assert.Equal("too_many_concurrent_verifications", exception.ApiErrorCode);
        Assert.Single(_mockHandler.Requests);
    }

    [Fact]
    public async Task AWaitedOutRateLimit_WaitsExactlyRetryAfter()
    {
        _mockHandler.QueueResponse(TooManyRequests(
            "rate_limit_exceeded",
            "Rate limit exceeded. Limit: 60 requests per minute.",
            2));
        _mockHandler.QueueSuccessResponse(CreditsJson);

        var started = DateTime.UtcNow;
        await _client.Account.GetCreditsAsync();
        var elapsed = DateTime.UtcNow - started;

        Assert.Equal(2, _mockHandler.Requests.Count);
        Assert.InRange(elapsed, TimeSpan.FromSeconds(1.9), TimeSpan.FromSeconds(2.9));
    }

    [Fact]
    public async Task TheLastAttempt_DoesNotWaitBeforeThrowing()
    {
        var client = new SendlyClient("test_api_key", new SendlyClientOptions { MaxRetries = 0 });
        typeof(SendlyClient).GetField("_httpClient", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(client, _httpClient);
        _mockHandler.QueueResponse(TooManyRequests(
            "rate_limit_exceeded",
            "Rate limit exceeded. Limit: 60 requests per minute.",
            2));

        var started = DateTime.UtcNow;
        await Assert.ThrowsAsync<RateLimitException>(() => client.Account.GetCreditsAsync());
        var elapsed = DateTime.UtcNow - started;

        Assert.Single(_mockHandler.Requests);
        Assert.True(elapsed < TimeSpan.FromSeconds(1), $"waited {elapsed} after the last attempt");
    }

    [Fact]
    public async Task ARateLimitOfExactlyAMinute_IsStillWaitedOut()
    {
        var client = new SendlyClient("test_api_key", new SendlyClientOptions { MaxRetries = 1 });
        typeof(SendlyClient).GetField("_httpClient", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(client, _httpClient);
        _mockHandler.QueueResponse(TooManyRequests(
            "too_many_concurrent_verifications",
            "Too many API key checks are already running for this account from this address. Try again in 1 second.",
            60));
        _mockHandler.QueueSuccessResponse(CreditsJson);

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.Account.GetCreditsAsync(timeout.Token));

        Assert.Single(_mockHandler.Requests);
    }

    [Fact]
    public async Task ACodeless429_IsRetried()
    {
        _mockHandler.QueueResponse(TooManyRequestsWithoutHeader(@"{""message"":""Too many requests""}"));
        _mockHandler.QueueSuccessResponse(CreditsJson);

        var credits = await _client.Account.GetCreditsAsync();

        Assert.Equal(10, credits.Balance);
        Assert.Equal(2, _mockHandler.Requests.Count);
    }

    private static IEnumerable<string?> ValuesOf(Type constants) =>
        constants.GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral)
            .Select(f => (string?)f.GetValue(null));

    [Theory]
    [InlineData(typeof(Resources.CallErrorCode), "too_many_failed_key_attempts")]
    [InlineData(typeof(Resources.CallErrorCode), "too_many_concurrent_verifications")]
    [InlineData(typeof(Resources.CallErrorCode), "from_number_not_supported")]
    [InlineData(typeof(Resources.RcsErrorCode), "too_many_failed_key_attempts")]
    [InlineData(typeof(Resources.RcsErrorCode), "too_many_concurrent_verifications")]
    public void ErrorCodeConstants_ListTheCodesTheApiAnswersWith(Type constants, string code)
    {
        Assert.Contains(code, ValuesOf(constants));
    }

    [Fact]
    public async Task RateLimitExceeded_IsStillRetried()
    {
        _mockHandler.QueueResponse(TooManyRequests(
            "rate_limit_exceeded",
            "Too many failed API key attempts. Try again in 1 seconds.",
            1));
        _mockHandler.QueueSuccessResponse(CreditsJson);

        var credits = await _client.Account.GetCreditsAsync();

        Assert.Equal(10, credits.Balance);
        Assert.Equal(2, _mockHandler.Requests.Count);
    }

    private const string ProvisionedJson = @"{""workspace"": { ""id"": ""org_2"", ""name"": ""Acme"", ""slug"": ""acme"" }}";

    [Fact]
    public async Task PerMinuteProvisioningLimit_IsWaitedOutWithTheSameIdempotencyKey()
    {
        _mockHandler.QueueResponse(TooManyRequestsWithoutHeader(
            @"{""error"":""provision_rate_limit"",""message"":""Max 120 provisions per minute."",""retryAfter"":1}"));
        _mockHandler.QueueResponse(HttpStatusCode.Created, ProvisionedJson);

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        var result = await _client.Enterprise.ProvisionAsync(new Models.ProvisionWorkspaceOptions { Name = "Acme" }, timeout.Token);

        Assert.Equal("org_2", result.Workspace.Id);
        Assert.Equal(2, _mockHandler.Requests.Count);
        Assert.NotNull(KeyOfRequest(0));
        Assert.Equal(KeyOfRequest(0), KeyOfRequest(1));
    }

    [Fact]
    public async Task HourlyProvisioningLimit_IsThrownAtOnce()
    {
        _mockHandler.QueueResponse(TooManyRequestsWithoutHeader(
            @"{""error"":""provision_rate_limit"",""message"":""Max 1000 provisions per hour."",""retryAfter"":3100}"));
        _mockHandler.QueueResponse(HttpStatusCode.Created, ProvisionedJson);

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var exception = await Assert.ThrowsAsync<RateLimitException>(
            () => _client.Enterprise.ProvisionAsync(new Models.ProvisionWorkspaceOptions { Name = "Acme" }, timeout.Token));

        Assert.Equal("provision_rate_limit", exception.ApiErrorCode);
        Assert.Equal(TimeSpan.FromSeconds(3100), exception.RetryAfter);
        Assert.Single(_mockHandler.Requests);
    }
}
