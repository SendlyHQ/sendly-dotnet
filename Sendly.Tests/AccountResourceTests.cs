using System.Net;
using System.Reflection;
using System.Text.Json;
using Sendly.Exceptions;
using Sendly.Models;
using Sendly.Tests.Fixtures;
using Xunit;

namespace Sendly.Tests;

/// <summary>
/// Tests for AccountResource against the bodies the account endpoints send.
/// </summary>
public class AccountResourceTests : IDisposable
{
    private readonly MockHttpMessageHandler _mockHandler;
    private readonly HttpClient _httpClient;
    private readonly SendlyClient _client;

    public AccountResourceTests()
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

    private async Task<JsonElement> JsonBodyOfLastRequest()
    {
        using var doc = JsonDocument.Parse(await _mockHandler.LastRequest!.Content!.ReadAsStringAsync());
        return doc.RootElement.Clone();
    }

    private const string AccountJson = @"{
        ""user"": { ""id"": ""user_1"", ""email"": ""ops@example.com"", ""createdAt"": ""2026-01-02T03:04:05.000Z"" },
        ""organization"": { ""id"": ""org_1"", ""name"": ""Acme"", ""isPersonal"": false },
        ""credits"": { ""balance"": 500, ""reservedBalance"": ""0"" },
        ""verification"": null,
        ""apiKey"": {
            ""id"": ""key_1"",
            ""name"": ""CI"",
            ""type"": ""test"",
            ""scopes"": [""sms:send"", ""sms:read""],
            ""createdAt"": ""2026-01-03T00:00:00.000Z"",
            ""lastUsedAt"": null
        },
        ""limits"": { ""messagesPerMinute"": 60, ""messagesPerDay"": 100 }
    }";

    private const string CreatedKeyJson = @"{
        ""id"": ""key_1"",
        ""name"": ""ci"",
        ""key"": ""sk_test_v1_abc"",
        ""keyPrefix"": ""sk_test_v1_a"",
        ""type"": ""test"",
        ""createdAt"": ""2026-09-25T10:00:00.000Z"",
        ""expiresAt"": null,
        ""apiKey"": {
            ""id"": ""key_1"",
            ""name"": ""ci"",
            ""type"": ""test"",
            ""prefix"": ""sk_test_v1_a..."",
            ""scopes"": [""sms:send""],
            ""permissions"": [""sms:send""],
            ""isActive"": true,
            ""isRevoked"": false,
            ""createdAt"": ""2026-09-25T10:00:00.000Z"",
            ""lastUsedAt"": null,
            ""expiresAt"": null
        }
    }";

    [Theory]
    [InlineData("purchase")]
    [InlineData("usage")]
    [InlineData("refund")]
    [InlineData("bonus")]
    [InlineData("transfer")]
    [InlineData("admin_grant")]
    [InlineData("admin_seed")]
    public void CreditTransactionTypes_ListEveryTypeTheLedgerRecords(string type)
    {
        var values = typeof(CreditTransaction.Types)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral)
            .Select(f => (string?)f.GetValue(null));

        Assert.Contains(type, values);
    }

    [Fact]
    public async Task ListTransactionsAsync_ReadsATransferRow()
    {
        _mockHandler.QueueSuccessResponse(@"{
            ""transactions"": [{
                ""id"": ""tx_1"",
                ""amount"": -500,
                ""balance_after"": 1500,
                ""type"": ""transfer"",
                ""description"": ""Transfer to Acme"",
                ""created_at"": ""2026-09-25T10:00:00.000Z""
            }]
        }");

        var list = await _client.Account.ListTransactionsAsync(new ListTransactionsOptions { Type = CreditTransaction.Types.Transfer });

        var transaction = Assert.Single(list);
        Assert.Equal(CreditTransaction.Types.Transfer, transaction.Type);
        Assert.Equal(1500, transaction.BalanceAfter);
        Assert.True(transaction.IsDebit);
        Assert.Contains("type=transfer", _mockHandler.LastRequest!.RequestUri!.Query);
    }

    [Fact]
    public async Task GetCreditsAsync_ReadsTheBalancesTheApiSends()
    {
        _mockHandler.QueueSuccessResponse(@"{
            ""balance"": 500,
            ""reservedBalance"": 20,
            ""availableBalance"": 480,
            ""billingMode"": ""prepaid"",
            ""recentTransactions"": []
        }");

        var credits = await _client.Account.GetCreditsAsync();

        Assert.Equal(500, credits.Balance);
        Assert.Equal(480, credits.AvailableBalance);
        Assert.Equal(20, credits.ReservedCredits);
        Assert.True(credits.HasCredits);
        Assert.Equal(20, credits.ReservedBalance);
        Assert.Equal("prepaid", credits.BillingMode);
    }

    [Fact]
    public async Task GetAsync_ReadsTheUserTheApiNestsAndSurvivesANullVerification()
    {
        _mockHandler.QueueSuccessResponse(AccountJson);

        var account = await _client.Account.GetAsync();

        Assert.Equal("user_1", account.Id);
        Assert.Equal("ops@example.com", account.Email);
        Assert.Equal(new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc), account.CreatedAt.ToUniversalTime());
        Assert.NotNull(account.Verification);
        Assert.False(account.Verification.IsFullyVerified);
        Assert.Null(account.Verification.Status);
        Assert.Equal(100, account.Limits.MessagesPerDay);
        Assert.Equal(60, account.Limits.MessagesPerMinute);
        Assert.Equal("org_1", account.Organization!.Id);
        Assert.Equal("Acme", account.Organization.Name);
        Assert.False(account.Organization.IsPersonal);
        Assert.Equal(500, account.Credits!.Balance);
        Assert.Equal(0, account.Credits.ReservedBalance);
        Assert.Equal("key_1", account.ApiKey!.Id);
        Assert.Equal("test", account.ApiKey.Type);
        Assert.Equal(new[] { "sms:send", "sms:read" }, account.ApiKey.Scopes);
        Assert.Null(account.ApiKey.LastUsedAt);
    }

    [Fact]
    public async Task GetAsync_WithoutAVerification_StillHasAVerificationObject()
    {
        _mockHandler.QueueSuccessResponse(AccountJson);

        var account = await _client.Account.GetAsync();

        Assert.NotNull(account.Verification);
        Assert.False(account.Verification.IsFullyVerified);
    }

    [Fact]
    public async Task GetAsync_WithAVerifiedBusiness_IsFullyVerified()
    {
        _mockHandler.QueueSuccessResponse(AccountJson.Replace(
            @"""verification"": null",
            @"""verification"": { ""status"": ""verified"", ""type"": ""toll_free"", ""region"": ""us"", ""submittedAt"": ""2026-02-01T00:00:00.000Z"", ""updatedAt"": ""2026-02-05T00:00:00.000Z"" }"));

        var account = await _client.Account.GetAsync();

        Assert.True(account.Verification.IsFullyVerified);
        Assert.Equal("verified", account.Verification.Status);
        Assert.Equal("toll_free", account.Verification.Type);
        Assert.Equal("us", account.Verification.Region);
        Assert.NotNull(account.Verification.SubmittedAt);
        Assert.NotNull(account.Verification.UpdatedAt);
    }

    [Fact]
    public async Task GetAsync_WithAnApprovedInternationalVerification_IsFullyVerified()
    {
        _mockHandler.QueueSuccessResponse(AccountJson.Replace(
            @"""verification"": null",
            @"""verification"": { ""status"": ""approved"", ""type"": ""international"", ""region"": ""intl"", ""submittedAt"": ""2026-02-01T00:00:00.000Z"", ""updatedAt"": ""2026-02-01T00:00:00.000Z"" }"));

        var account = await _client.Account.GetAsync();

        Assert.Equal("approved", account.Verification.Status);
        Assert.Equal("international", account.Verification.Type);
        Assert.True(account.Verification.IsFullyVerified);
    }

    [Fact]
    public async Task GetAsync_WithAPendingVerification_IsNotFullyVerified()
    {
        _mockHandler.QueueSuccessResponse(AccountJson.Replace(
            @"""verification"": null",
            @"""verification"": { ""status"": ""processing"", ""type"": ""toll_free"", ""region"": null, ""submittedAt"": ""2026-02-01T00:00:00.000Z"", ""updatedAt"": ""2026-02-05T00:00:00.000Z"" }"));

        var account = await _client.Account.GetAsync();

        Assert.False(account.Verification.IsFullyVerified);
        Assert.Equal("processing", account.Verification.Status);
    }

    [Fact]
    public async Task CreateApiKeyAsync_WithLiveTypeAndScopes_SendsThem()
    {
        _mockHandler.QueueSuccessResponse(CreatedKeyJson);

        await _client.Account.CreateApiKeyAsync(new CreateApiKeyOptions
        {
            Name = "prod",
            Type = "live",
            Scopes = new List<string> { "sms:send" },
            ExpiresAt = "2030-01-01T00:00:00Z",
        });

        var body = await JsonBodyOfLastRequest();
        Assert.Equal("live", body.GetProperty("type").GetString());
        Assert.Equal("sms:send", body.GetProperty("scopes")[0].GetString());
        Assert.Equal("2030-01-01T00:00:00Z", body.GetProperty("expires_at").GetString());
    }

    [Fact]
    public async Task CreateApiKeyAsync_ForALiveKeyWithoutVerification_ThrowsTheRefusalWithoutRetrying()
    {
        for (var i = 0; i < 4; i++)
            _mockHandler.QueueResponse(HttpStatusCode.Forbidden,
                @"{""error"":""verification_required"",""message"":""Verification required to create live API keys""}");

        var exception = await Assert.ThrowsAsync<SendlyException>(() =>
            _client.Account.CreateApiKeyAsync(new CreateApiKeyOptions { Name = "prod", Type = "live" }));

        Assert.Equal(403, exception.StatusCode);
        Assert.Equal("verification_required", exception.ApiErrorCode);
        Assert.Single(_mockHandler.Requests);
    }

    [Fact]
    public async Task CreateApiKeyAsync_WithoutScopesOrExpiry_SendsNeither()
    {
        _mockHandler.QueueSuccessResponse(CreatedKeyJson);

        await _client.Account.CreateApiKeyAsync("ci");

        Assert.Equal("{\"name\":\"ci\",\"type\":\"test\"}", await _mockHandler.LastRequest!.Content!.ReadAsStringAsync());
    }

    [Fact]
    public async Task CreateApiKeyAsync_SendsTheTypeTheApiReads()
    {
        _mockHandler.QueueSuccessResponse(CreatedKeyJson);

        await _client.Account.CreateApiKeyAsync("ci");

        var body = await JsonBodyOfLastRequest();
        Assert.Equal("ci", body.GetProperty("name").GetString());
        Assert.True(body.TryGetProperty("type", out var type));
        Assert.Equal("test", type.GetString());
    }

    [Fact]
    public async Task CreateApiKeyAsync_ReadsTheCreatedKeyTheApiReturns()
    {
        _mockHandler.QueueSuccessResponse(CreatedKeyJson);

        var result = await _client.Account.CreateApiKeyAsync("ci");

        Assert.Equal("sk_test_v1_abc", result.Key);
        Assert.Equal("key_1", result.ApiKey.Id);
        Assert.Equal("ci", result.ApiKey.Name);
        Assert.Equal("sk_test_v1_a...", result.ApiKey.Prefix);
        Assert.NotEqual(default, result.ApiKey.CreatedAt);
        Assert.Equal("test", result.ApiKey.Type);
        Assert.True(result.ApiKey.IsActive);
        Assert.False(result.ApiKey.IsRevoked);
        Assert.Equal(new[] { "sms:send" }, result.ApiKey.Scopes);
        Assert.Null(result.ApiKey.ExpiresAt);
    }

    [Fact]
    public async Task CreateApiKeyAsync_ReadsAFlatCreatedKey()
    {
        _mockHandler.QueueSuccessResponse(@"{
            ""id"": ""key_2"",
            ""name"": ""ci"",
            ""key"": ""sk_test_v1_def"",
            ""keyPrefix"": ""sk_test_v1_d"",
            ""type"": ""test"",
            ""createdAt"": ""2026-09-25T10:00:00.000Z""
        }");

        var result = await _client.Account.CreateApiKeyAsync("ci");

        Assert.Equal("sk_test_v1_def", result.Key);
        Assert.Equal("key_2", result.ApiKey.Id);
        Assert.Equal("ci", result.ApiKey.Name);
    }

    [Fact]
    public async Task ListApiKeysAsync_ReadsTheKeysTheApiSends()
    {
        _mockHandler.QueueSuccessResponse(@"{
            ""keys"": [{
                ""id"": ""key_1"",
                ""name"": ""Old"",
                ""type"": ""live"",
                ""prefix"": ""sk_live_v1_a..."",
                ""scopes"": [""sms:send""],
                ""permissions"": [""sms:send""],
                ""isActive"": false,
                ""isRevoked"": true,
                ""createdAt"": ""2026-01-01T00:00:00.000Z"",
                ""lastUsedAt"": ""2026-02-01T00:00:00.000Z"",
                ""expiresAt"": ""2027-01-01T00:00:00.000Z""
            }]
        }");

        var keys = await _client.Account.ListApiKeysAsync();

        var key = Assert.Single(keys);
        Assert.False(key.IsActive);
        Assert.NotEqual(default, key.CreatedAt);
        Assert.NotNull(key.LastUsedAt);
        Assert.NotNull(key.ExpiresAt);
        Assert.Equal("live", key.Type);
        Assert.True(key.IsRevoked);
        Assert.Equal(new[] { "sms:send" }, key.Scopes);
        Assert.Equal(new[] { "sms:send" }, key.Permissions);
    }

    [Fact]
    public async Task GetApiKeyAsync_ReadsARevokedKey()
    {
        _mockHandler.QueueSuccessResponse(@"{
            ""id"": ""key_1"",
            ""name"": ""Old"",
            ""type"": ""live"",
            ""prefix"": ""sk_live_v1_a..."",
            ""scopes"": [""sms:send""],
            ""isActive"": false,
            ""createdAt"": ""2026-01-01T00:00:00.000Z"",
            ""lastUsedAt"": null,
            ""expiresAt"": null,
            ""revokedAt"": ""2026-03-01T00:00:00.000Z""
        }");

        var key = await _client.Account.GetApiKeyAsync("key_1");

        Assert.False(key.IsActive);
        Assert.NotEqual(default, key.CreatedAt);
        Assert.True(key.IsRevoked);
        Assert.NotNull(key.RevokedAt);
        Assert.Equal(new[] { "sms:send" }, key.Permissions);
    }

    [Fact]
    public async Task GetApiKeyUsageAsync_ReadsTheSummaryTheApiSends()
    {
        _mockHandler.QueueSuccessResponse(@"{
            ""keyId"": ""key_1"",
            ""keyName"": ""CI"",
            ""summary"": { ""totalRequests"": 3, ""totalCredits"": 4, ""lastUsed"": ""2026-09-25T10:00:00.000Z"" },
            ""recentRequests"": [
                { ""endpoint"": ""/api/v1/messages"", ""method"": ""POST"", ""statusCode"": 201, ""creditsUsed"": 2, ""createdAt"": ""2026-09-25T10:00:00.000Z"" },
                { ""endpoint"": ""/api/v1/messages"", ""method"": ""POST"", ""statusCode"": 201, ""creditsUsed"": 2, ""createdAt"": ""2026-09-25T09:00:00.000Z"" },
                { ""endpoint"": ""/api/v1/account"", ""method"": ""GET"", ""statusCode"": 200, ""creditsUsed"": 0, ""createdAt"": ""2026-09-25T08:00:00.000Z"" }
            ],
            ""endpointBreakdown"": [
                { ""endpoint"": ""POST /api/v1/messages"", ""count"": 2 },
                { ""endpoint"": ""GET /api/v1/account"", ""count"": 1 }
            ]
        }");

        var usage = await _client.Account.GetApiKeyUsageAsync("key_1");

        Assert.Equal(3, usage.TotalRequests);
        Assert.Equal(4, usage.CreditsUsed);
        Assert.NotNull(usage.LastRequestAt);
        Assert.Equal("key_1", usage.KeyId);
        Assert.Equal("CI", usage.KeyName);
        Assert.Equal(3, usage.RecentRequests.Count);
        Assert.Equal(201, usage.RecentRequests[0].StatusCode);
        Assert.Equal("POST", usage.RecentRequests[0].Method);
        Assert.Equal(2, usage.EndpointBreakdown[0].Count);
        Assert.Equal("POST /api/v1/messages", usage.EndpointBreakdown[0].Endpoint);
    }

    [Fact]
    public async Task GetApiKeyUsageAsync_ForAnUnusedKey_ReadsZeroes()
    {
        _mockHandler.QueueSuccessResponse(@"{
            ""keyId"": ""key_1"",
            ""keyName"": ""CI"",
            ""summary"": { ""totalRequests"": 0, ""totalCredits"": 0, ""lastUsed"": null },
            ""recentRequests"": [],
            ""endpointBreakdown"": []
        }");

        var usage = await _client.Account.GetApiKeyUsageAsync("key_1");

        Assert.Equal(0, usage.TotalRequests);
        Assert.Null(usage.LastRequestAt);
        Assert.Empty(usage.RecentRequests);
    }
}
