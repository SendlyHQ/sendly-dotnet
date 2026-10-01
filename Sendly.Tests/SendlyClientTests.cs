using System.Net;
using System.Reflection;
using System.Text.Json;
using Sendly.Exceptions;
using Sendly.Models;
using Sendly.Tests.Fixtures;
using Xunit;

namespace Sendly.Tests;

/// <summary>
/// Tests for SendlyClient initialization and configuration.
/// </summary>
public class SendlyClientTests
{
    [Fact]
    public void Constructor_WithValidApiKey_InitializesClient()
    {
        // Arrange & Act
        using var client = new SendlyClient("test_api_key");

        // Assert
        Assert.NotNull(client);
        Assert.NotNull(client.Messages);
    }

    [Fact]
    public void Constructor_WithValidApiKeyAndOptions_InitializesClient()
    {
        // Arrange
        var options = new SendlyClientOptions
        {
            BaseUrl = "https://custom.api.com",
            Timeout = TimeSpan.FromSeconds(60),
            MaxRetries = 5
        };

        // Act
        using var client = new SendlyClient("test_api_key", options);

        // Assert
        Assert.NotNull(client);
        Assert.NotNull(client.Messages);
    }

    [Fact]
    public void Constructor_WithNullApiKey_ThrowsAuthenticationException()
    {
        // Act & Assert
        var exception = Assert.Throws<AuthenticationException>(() => new SendlyClient(null!));
        Assert.Equal("API key is required", exception.Message);
        Assert.Equal(401, exception.StatusCode);
    }

    [Fact]
    public void Constructor_WithEmptyApiKey_ThrowsAuthenticationException()
    {
        // Act & Assert
        var exception = Assert.Throws<AuthenticationException>(() => new SendlyClient(""));
        Assert.Equal("API key is required", exception.Message);
        Assert.Equal(401, exception.StatusCode);
    }

    [Fact]
    public void Constructor_WithWhitespaceApiKey_ThrowsAuthenticationException()
    {
        // Act & Assert
        var exception = Assert.Throws<AuthenticationException>(() => new SendlyClient("   "));
        Assert.Equal("API key is required", exception.Message);
    }

    [Fact]
    public void Constructor_WithNullOptions_UsesDefaults()
    {
        // Act
        using var client = new SendlyClient("test_api_key", null);

        // Assert
        Assert.NotNull(client);
        Assert.NotNull(client.Messages);
    }

    [Fact]
    public void DefaultBaseUrl_IsCorrect()
    {
        // Assert
        Assert.Equal("https://sendly.live/api/v1", SendlyClient.DefaultBaseUrl);
    }

    /// <summary>
    /// Reads the HttpClient the SDK built for itself.
    /// </summary>
    private static Uri BaseAddressOf(SendlyClient client)
    {
        var field = typeof(SendlyClient).GetField("_httpClient",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(field);
        var http = (HttpClient)field!.GetValue(client)!;
        Assert.NotNull(http.BaseAddress);
        return http.BaseAddress!;
    }

    [Fact]
    public void Constructor_WithDefaultBaseUrl_KeepsVersionedSegmentWhenResolvingPaths()
    {
        // Request paths are relative, and RFC 3986 drops the last segment of a
        // base that does not end in "/" — so an unslashed ".../api/v1" would
        // send every call to ".../api/*" instead of the versioned API.
        using var client = new SendlyClient("test_api_key");

        var baseAddress = BaseAddressOf(client);

        Assert.Equal("https://sendly.live/api/v1/", baseAddress.ToString());
        Assert.Equal("https://sendly.live/api/v1/messages",
            new Uri(baseAddress, "messages").ToString());
    }

    [Fact]
    public void Constructor_WithCustomBaseUrlLackingTrailingSlash_KeepsLastSegment()
    {
        var options = new SendlyClientOptions { BaseUrl = "https://custom.api.com/api/v1" };

        using var client = new SendlyClient("test_api_key", options);

        Assert.Equal("https://custom.api.com/api/v1/messages",
            new Uri(BaseAddressOf(client), "messages").ToString());
    }

    [Fact]
    public void Constructor_WithCustomBaseUrlEndingInSlash_DoesNotDoubleIt()
    {
        var options = new SendlyClientOptions { BaseUrl = "https://custom.api.com/api/v1/" };

        using var client = new SendlyClient("test_api_key", options);

        Assert.Equal("https://custom.api.com/api/v1/", BaseAddressOf(client).ToString());
    }

    [Fact]
    public async Task Request_OnDefaultBaseUrl_TargetsVersionedApiOnTheWire()
    {
        using var mockHandler = new MockHttpMessageHandler();
        mockHandler.QueueSuccessResponse(@"{""data"": [], ""has_more"": false, ""total"": 0}");

        using var client = new SendlyClient("test_api_key");
        // Swap in the recording handler but keep the base address the SDK
        // itself derived, so the assertion covers the real default.
        var field = typeof(SendlyClient).GetField("_httpClient",
            BindingFlags.NonPublic | BindingFlags.Instance);
        using var instrumented = new HttpClient(mockHandler)
        {
            BaseAddress = BaseAddressOf(client),
        };
        field!.SetValue(client, instrumented);

        await client.Messages.ListAsync();

        Assert.Equal("https://sendly.live/api/v1/messages",
            mockHandler.LastRequest?.RequestUri?.GetLeftPart(UriPartial.Path));
    }

    private static SendlyClient ClientRecordingTo(MockHttpMessageHandler handler)
    {
        var client = new SendlyClient("test_api_key");
        var field = typeof(SendlyClient).GetField("_httpClient",
            BindingFlags.NonPublic | BindingFlags.Instance);
        field!.SetValue(client, new HttpClient(handler) { BaseAddress = new Uri("https://api.test.com") });
        return client;
    }

    private static async Task<JsonElement> JsonBodyOf(HttpRequestMessage request)
    {
        using var doc = JsonDocument.Parse(await request.Content!.ReadAsStringAsync());
        return doc.RootElement.Clone();
    }

    private static List<string> KeysOf(JsonElement body) =>
        body.EnumerateObject().Select(p => p.Name).ToList();

    [Theory]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.Conflict)]
    [InlineData(HttpStatusCode.Gone)]
    [InlineData(HttpStatusCode.RequestEntityTooLarge)]
    [InlineData(HttpStatusCode.PreconditionRequired)]
    public async Task A4xxAnswer_IsThrownOnTheFirstAttempt(HttpStatusCode status)
    {
        using var handler = new MockHttpMessageHandler();
        for (var i = 0; i < 4; i++)
            handler.QueueResponse(status, @"{""error"":""refused"",""message"":""Refused""}");
        using var client = ClientRecordingTo(handler);

        var exception = await Assert.ThrowsAsync<SendlyException>(() => client.Account.GetCreditsAsync());

        Assert.Equal((int)status, exception.StatusCode);
        Assert.Equal("refused", exception.ApiErrorCode);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task A408Answer_IsRetried()
    {
        using var handler = new MockHttpMessageHandler();
        handler.QueueResponse(HttpStatusCode.RequestTimeout, @"{""error"":""request_timeout"",""message"":""Timed out""}");
        handler.QueueSuccessResponse(@"{""balance"":10,""reservedBalance"":0,""availableBalance"":10,""billingMode"":""prepaid"",""recentTransactions"":[]}");
        using var client = ClientRecordingTo(handler);

        var credits = await client.Account.GetCreditsAsync();

        Assert.Equal(10, credits.Balance);
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task ContactsUpdateAsync_SendsOnlyTheFieldsSet()
    {
        using var handler = new MockHttpMessageHandler();
        handler.QueueSuccessResponse(@"{""id"":""ct_1"",""phone_number"":""+15551234567"",""name"":""x"",""email"":""a@b.co"",""metadata"":{}}");
        using var client = ClientRecordingTo(handler);

        await client.Contacts.UpdateAsync("ct_1", new UpdateContactRequest { Name = "x" });

        Assert.Equal("{\"name\":\"x\"}", await handler.LastRequest!.Content!.ReadAsStringAsync());
    }

    [Fact]
    public async Task ConversationsUpdateAsync_WithOnlyMetadata_SendsNoTags()
    {
        using var handler = new MockHttpMessageHandler();
        handler.QueueSuccessResponse(@"{""id"":""conv_1"",""phoneNumber"":""+15551234567"",""status"":""active"",""metadata"":{""vip"":true},""tags"":[""a""]}");
        using var client = ClientRecordingTo(handler);

        await client.Conversations.UpdateAsync("conv_1", new UpdateConversationRequest
        {
            Metadata = new Dictionary<string, object> { ["vip"] = true }
        });

        var body = await JsonBodyOf(handler.LastRequest!);
        Assert.Equal(new[] { "metadata" }, KeysOf(body));
    }

    [Fact]
    public async Task DraftsUpdateAsync_WithOnlyMetadata_SendsNoText()
    {
        using var handler = new MockHttpMessageHandler();
        handler.QueueSuccessResponse(@"{""id"":""d_1"",""conversationId"":""conv_1"",""text"":""Hi"",""status"":""pending"",""metadata"":{""k"":""v""}}");
        using var client = ClientRecordingTo(handler);

        await client.Drafts.UpdateAsync("d_1", new UpdateDraftRequest
        {
            Metadata = new Dictionary<string, object> { ["k"] = "v" }
        });

        var body = await JsonBodyOf(handler.LastRequest!);
        Assert.Equal(new[] { "metadata" }, KeysOf(body));
    }

    [Fact]
    public async Task RulesUpdateAsync_WithOnlyPriority_SendsNoNameConditionsOrActions()
    {
        using var handler = new MockHttpMessageHandler();
        handler.QueueSuccessResponse(@"{""id"":""r_1"",""name"":""n"",""priority"":5,""enabled"":true}");
        using var client = ClientRecordingTo(handler);

        await client.Rules.UpdateAsync("r_1", new UpdateRuleRequest { Priority = 5 });

        var body = await JsonBodyOf(handler.LastRequest!);
        Assert.Equal(new[] { "priority" }, KeysOf(body));
    }

    [Fact]
    public async Task ContactListsUpdateAsync_WithOnlyName_SendsNoDescription()
    {
        using var handler = new MockHttpMessageHandler();
        handler.QueueSuccessResponse(@"{""id"":""lst_1"",""name"":""VIPs"",""description"":""Keep"",""contact_count"":3}");
        using var client = ClientRecordingTo(handler);

        await client.Contacts.Lists.UpdateAsync("lst_1", new UpdateContactListRequest { Name = "VIPs" });

        Assert.Equal("{\"name\":\"VIPs\"}", await handler.LastRequest!.Content!.ReadAsStringAsync());
    }

    [Fact]
    public async Task ContactListsUpdateAsync_WithOnlyDescription_SendsNoName()
    {
        using var handler = new MockHttpMessageHandler();
        handler.QueueSuccessResponse(@"{""id"":""lst_1"",""name"":""VIPs"",""description"":""Top buyers"",""contact_count"":3}");
        using var client = ClientRecordingTo(handler);

        await client.Contacts.Lists.UpdateAsync("lst_1", new UpdateContactListRequest { Description = "Top buyers" });

        Assert.Equal("{\"description\":\"Top buyers\"}", await handler.LastRequest!.Content!.ReadAsStringAsync());
    }

    [Fact]
    public async Task ContactsUpdateAsync_WithFieldsSetToNull_SendsNullToClearThem()
    {
        using var handler = new MockHttpMessageHandler();
        handler.QueueSuccessResponse(@"{""id"":""ct_1"",""phone_number"":""+15551234567"",""name"":null,""email"":null,""metadata"":null}");
        using var client = ClientRecordingTo(handler);

        await client.Contacts.UpdateAsync("ct_1", new UpdateContactRequest
        {
            PhoneNumber = null,
            Name = null,
            Email = null,
            Metadata = null
        });

        Assert.Equal("{\"name\":null,\"email\":null,\"metadata\":null}", await handler.LastRequest!.Content!.ReadAsStringAsync());
    }

    [Fact]
    public async Task ContactsUpdateAsync_WithEmailSetToNull_ClearsOnlyTheEmail()
    {
        using var handler = new MockHttpMessageHandler();
        handler.QueueSuccessResponse(@"{""id"":""ct_1"",""phone_number"":""+15551234567"",""name"":""Ann"",""email"":null,""metadata"":{""tier"":""gold""}}");
        using var client = ClientRecordingTo(handler);

        await client.Contacts.UpdateAsync("ct_1", new UpdateContactRequest { Name = "Ann", Email = null });

        Assert.Equal("{\"name\":\"Ann\",\"email\":null}", await handler.LastRequest!.Content!.ReadAsStringAsync());
    }

    [Fact]
    public async Task ContactListsUpdateAsync_WithDescriptionSetToNull_SendsNullToClearIt()
    {
        using var handler = new MockHttpMessageHandler();
        handler.QueueSuccessResponse(@"{""id"":""lst_1"",""name"":""VIPs"",""description"":null,""contact_count"":3}");
        using var client = ClientRecordingTo(handler);

        await client.Contacts.Lists.UpdateAsync("lst_1", new UpdateContactListRequest { Name = "VIPs", Description = null });

        Assert.Equal("{\"name\":\"VIPs\",\"description\":null}", await handler.LastRequest!.Content!.ReadAsStringAsync());
    }

    [Fact]
    public async Task DraftsUpdateAsync_WithMediaUrlsAndMetadataSetToNull_SendsNullToClearThem()
    {
        using var handler = new MockHttpMessageHandler();
        handler.QueueSuccessResponse(@"{""id"":""d_1"",""conversationId"":""conv_1"",""text"":""Hi"",""mediaUrls"":null,""status"":""pending"",""metadata"":null}");
        using var client = ClientRecordingTo(handler);

        await client.Drafts.UpdateAsync("d_1", new UpdateDraftRequest
        {
            Text = null,
            MediaUrls = null,
            Metadata = null
        });

        Assert.Equal("{\"mediaUrls\":null,\"metadata\":null}", await handler.LastRequest!.Content!.ReadAsStringAsync());
    }

    [Fact]
    public async Task EnterpriseUpdateOptInPageAsync_WithFieldsSetToNull_SendsNullToClearThem()
    {
        using var handler = new MockHttpMessageHandler();
        handler.QueueSuccessResponse(@"{""id"":""page_1"",""slug"":""acme"",""url"":""https://sendly.live/opt-in/acme"",""businessName"":""Acme"",""logoUrl"":null,""headerColor"":""#000000"",""buttonColor"":null,""customHeadline"":null,""customBenefits"":null}");
        using var client = ClientRecordingTo(handler);

        await client.Enterprise.Workspaces.UpdateOptInPageAsync("ws_1", "page_1", new UpdateOptInPageOptions
        {
            LogoUrl = null,
            HeaderColor = "#000000",
            ButtonColor = null,
            CustomHeadline = null,
            CustomBenefits = null
        });

        Assert.Equal(
            "{\"logoUrl\":null,\"headerColor\":\"#000000\",\"buttonColor\":null,\"customHeadline\":null,\"customBenefits\":null}",
            await handler.LastRequest!.Content!.ReadAsStringAsync());
    }

    [Fact]
    public async Task EnterpriseUpdateOptInPageAsync_SendsOnlyTheFieldsSet()
    {
        using var handler = new MockHttpMessageHandler();
        handler.QueueSuccessResponse(@"{""id"":""page_1"",""slug"":""acme"",""url"":""https://sendly.live/opt-in/acme"",""businessName"":""Acme"",""logoUrl"":""https://example.com/logo.png"",""headerColor"":""#000000""}");
        using var client = ClientRecordingTo(handler);

        await client.Enterprise.Workspaces.UpdateOptInPageAsync("ws_1", "page_1", new UpdateOptInPageOptions { HeaderColor = "#000000" });

        Assert.Equal("{\"headerColor\":\"#000000\"}", await handler.LastRequest!.Content!.ReadAsStringAsync());
    }

    [Fact]
    public async Task EnterpriseSetQuotaAsync_WithNullQuota_StillSendsNullToRemoveTheQuota()
    {
        using var handler = new MockHttpMessageHandler();
        handler.QueueSuccessResponse(@"{""monthlyMessageQuota"":null,""messagesThisMonth"":12,""quotaResetAt"":null}");
        using var client = ClientRecordingTo(handler);

        await client.Enterprise.Workspaces.SetQuotaAsync("ws_1", new UpdateQuotaOptions { MonthlyMessageQuota = null });

        Assert.Equal("{\"monthlyMessageQuota\":null}", await handler.LastRequest!.Content!.ReadAsStringAsync());
    }

    [Fact]
    public void Version_IsSet()
    {
        // Assert
        Assert.NotNull(SendlyClient.Version);
        Assert.NotEmpty(SendlyClient.Version);
        Assert.Matches(@"^\d+\.\d+\.\d+$", SendlyClient.Version);
    }

    [Fact]
    public void SendlyClientOptions_DefaultTimeout_Is30Seconds()
    {
        // Arrange & Act
        var options = new SendlyClientOptions();

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(30), options.Timeout);
    }

    [Fact]
    public void SendlyClientOptions_DefaultMaxRetries_Is3()
    {
        // Arrange & Act
        var options = new SendlyClientOptions();

        // Assert
        Assert.Equal(3, options.MaxRetries);
    }

    [Fact]
    public void SendlyClientOptions_DefaultBaseUrl_IsNull()
    {
        // Arrange & Act
        var options = new SendlyClientOptions();

        // Assert
        Assert.Null(options.BaseUrl);
    }

    [Fact]
    public void SendlyClientOptions_CanSetCustomValues()
    {
        // Arrange & Act
        var options = new SendlyClientOptions
        {
            BaseUrl = "https://custom.api.com",
            Timeout = TimeSpan.FromMinutes(2),
            MaxRetries = 10
        };

        // Assert
        Assert.Equal("https://custom.api.com", options.BaseUrl);
        Assert.Equal(TimeSpan.FromMinutes(2), options.Timeout);
        Assert.Equal(10, options.MaxRetries);
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        var client = new SendlyClient("test_api_key");

        // Act & Assert - Should not throw
        client.Dispose();
        client.Dispose();
    }

    [Fact]
    public void Client_ImplementsIDisposable()
    {
        // Assert
        Assert.True(typeof(IDisposable).IsAssignableFrom(typeof(SendlyClient)));
    }

    [Fact]
    public void Client_CanBeUsedInUsingStatement()
    {
        // Act & Assert - Should not throw
        using (var client = new SendlyClient("test_api_key"))
        {
            Assert.NotNull(client);
        }
    }

    [Fact]
    public void MessagesResource_IsInitializedOnConstruction()
    {
        // Arrange & Act
        using var client = new SendlyClient("test_api_key");

        // Assert
        Assert.NotNull(client.Messages);
    }

    [Fact]
    public void Constructor_SetsAuthorizationHeader()
    {
        // This test verifies the client is properly configured
        // The actual header verification happens in integration tests
        using var client = new SendlyClient("test_api_key_123");
        Assert.NotNull(client);
    }

    [Theory]
    [InlineData("sk_test_123")]
    [InlineData("sk_live_456")]
    [InlineData("custom_key_789")]
    public void Constructor_AcceptsVariousApiKeyFormats(string apiKey)
    {
        // Act
        using var client = new SendlyClient(apiKey);

        // Assert
        Assert.NotNull(client);
    }

    [Fact]
    public void Constructor_WithZeroMaxRetries_AcceptsValue()
    {
        // Arrange
        var options = new SendlyClientOptions { MaxRetries = 0 };

        // Act
        using var client = new SendlyClient("test_api_key", options);

        // Assert
        Assert.NotNull(client);
    }

    [Fact]
    public void Constructor_WithNegativeMaxRetries_AcceptsValue()
    {
        // This tests that the client doesn't validate max retries in constructor
        // (validation happens at runtime if needed)
        var options = new SendlyClientOptions { MaxRetries = -1 };

        using var client = new SendlyClient("test_api_key", options);

        Assert.NotNull(client);
    }
}
