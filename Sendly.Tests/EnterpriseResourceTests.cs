using System.Net;
using System.Reflection;
using System.Text.Json;
using Sendly.Models;
using Sendly.Tests.Fixtures;
using Xunit;

namespace Sendly.Tests;

/// <summary>
/// Tests for the enterprise resources against the bodies the enterprise
/// endpoints send.
/// </summary>
public class EnterpriseResourceTests : IDisposable
{
    private readonly MockHttpMessageHandler _mockHandler;
    private readonly HttpClient _httpClient;
    private readonly SendlyClient _client;

    public EnterpriseResourceTests()
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

    private async Task<string> BodyOfLastRequest()
    {
        return await _mockHandler.LastRequest!.Content!.ReadAsStringAsync();
    }

    private const string OverviewJson = @"{
        ""totalWorkspaces"": 4,
        ""totalMessages"": 1500,
        ""totalDelivered"": 1463,
        ""totalFailed"": 37,
        ""deliveryRate"": 97.53,
        ""totalCredits"": 12000,
        ""deliveredMessages"": 1463,
        ""failedMessages"": 37,
        ""totalCreditsUsed"": 3000,
        ""activeWorkspaces"": 3,
        ""suspendedWorkspaces"": 1,
        ""totalMessagesSent"": 1500,
        ""totalMessagesDelivered"": 1463,
        ""totalCreditsRemaining"": 12000
    }";

    [Fact]
    public async Task OverviewAsync_WithAFractionalDeliveryRate_ReadsTheOverview()
    {
        _mockHandler.QueueSuccessResponse(OverviewJson);

        var overview = await _client.Enterprise.Analytics.OverviewAsync();

        Assert.Equal(1500, overview.TotalMessages);
        Assert.Equal(1463, overview.DeliveredMessages);
        Assert.Equal(37, overview.FailedMessages);
        Assert.Equal(3000, overview.TotalCreditsUsed);
        Assert.Equal(3, overview.ActiveWorkspaces);
        Assert.Equal(98, overview.DeliveryRate);
        Assert.Equal(97.53, overview.DeliveryRatePercent);
        Assert.Equal(4, overview.TotalWorkspaces);
        Assert.Equal(12000, overview.TotalCredits);
        Assert.Equal(1, overview.SuspendedWorkspaces);
    }

    [Theory]
    [InlineData("100", 100, 100.0)]
    [InlineData("0", 0, 0.0)]
    [InlineData("96.5", 97, 96.5)]
    [InlineData("33.33", 33, 33.33)]
    public async Task OverviewAsync_RoundsTheDeliveryRateForTheWholeNumberProperty(string wire, int rounded, double precise)
    {
        _mockHandler.QueueSuccessResponse(OverviewJson.Replace(@"""deliveryRate"": 97.53", $@"""deliveryRate"": {wire}"));

        var overview = await _client.Enterprise.Analytics.OverviewAsync();

        Assert.Equal(rounded, overview.DeliveryRate);
        Assert.Equal(precise, overview.DeliveryRatePercent);
    }

    [Fact]
    public void DeliveryRate_WhenSet_SetsThePercentToo()
    {
        var overview = new EnterpriseAnalyticsOverview { DeliveryRate = 90 };

        Assert.Equal(90.0, overview.DeliveryRatePercent);
        Assert.Equal(90, overview.DeliveryRate);
    }

    [Fact]
    public async Task CreditsAsync_ReadsTheTotalsTheApiSends()
    {
        _mockHandler.QueueSuccessResponse(@"{""period"":""30d"",""totalBalance"":100,""totalLifetime"":250,""totalUsed"":150,""workspaceCount"":3}");

        var credits = await _client.Enterprise.Analytics.CreditsAsync();

        Assert.Equal("30d", credits.Period);
        Assert.Equal(100, credits.TotalBalance);
        Assert.Equal(250, credits.TotalLifetime);
        Assert.Equal(150, credits.TotalUsed);
        Assert.Equal(3, credits.WorkspaceCount);
        Assert.Empty(credits.Data);
    }

    [Fact]
    public async Task ProvisionAsync_ReadsTheOptInAndLegalPages()
    {
        _mockHandler.QueueResponse(HttpStatusCode.Created, @"{
            ""workspace"": { ""id"": ""org_2"", ""name"": ""Acme"", ""slug"": ""acme"" },
            ""verification"": { ""id"": ""bv_1"", ""status"": ""pending"", ""tollFreeNumber"": ""+18335550100"", ""inherited"": false },
            ""optInPage"": { ""id"": ""p_1"", ""slug"": ""acme"", ""url"": ""https://sendly.live/opt-in/acme"" },
            ""legalPages"": {
                ""privacyUrl"": ""https://sendly.live/legal/acme-x1-privacy"",
                ""termsUrl"": ""https://sendly.live/legal/acme-x1-terms"",
                ""privacyPageId"": ""lp_1"",
                ""termsPageId"": ""lp_2""
            }
        }");

        var result = await _client.Enterprise.ProvisionAsync(new ProvisionWorkspaceOptions { Name = "Acme" });

        Assert.Equal("org_2", result.Workspace.Id);
        Assert.Equal("https://sendly.live/opt-in/acme", result.OptInPage!.Url);
        Assert.Equal("p_1", result.OptInPage.Id);
        Assert.Equal("acme", result.OptInPage.Slug);
        Assert.Null(result.OptInPage.Error);
        Assert.Equal("https://sendly.live/legal/acme-x1-privacy", result.LegalPages!.PrivacyUrl);
        Assert.Equal("https://sendly.live/legal/acme-x1-terms", result.LegalPages.TermsUrl);
        Assert.Equal("lp_1", result.LegalPages.PrivacyPageId);
        Assert.Equal("lp_2", result.LegalPages.TermsPageId);
    }

    [Fact]
    public async Task ProvisionAsync_ReadsAPageThatFailedToGenerate()
    {
        _mockHandler.QueueResponse(HttpStatusCode.Created, @"{
            ""workspace"": { ""id"": ""org_2"", ""name"": ""Acme"", ""slug"": ""acme"" },
            ""optInPage"": { ""error"": ""slug taken"" },
            ""legalPages"": { ""error"": ""storage unavailable"" }
        }");

        var result = await _client.Enterprise.ProvisionAsync(new ProvisionWorkspaceOptions { Name = "Acme" });

        Assert.Equal("slug taken", result.OptInPage!.Error);
        Assert.Null(result.OptInPage.Url);
        Assert.Equal("storage unavailable", result.LegalPages!.Error);
    }

    [Fact]
    public async Task InheritVerificationAsync_WithPurchaseNewNumber_SendsTheFlag()
    {
        _mockHandler.QueueResponse(HttpStatusCode.Created, @"{
            ""verificationId"": ""bv_2"",
            ""status"": ""pending"",
            ""type"": ""toll_free"",
            ""tollFreeNumber"": ""+18335550111"",
            ""inheritedFrom"": ""org_src"",
            ""newNumber"": true
        }");

        var result = await _client.Enterprise.Workspaces.InheritVerificationAsync("org_2",
            new InheritVerificationOptions { SourceWorkspaceId = "org_src", PurchaseNewNumber = true });

        Assert.Equal("{\"sourceWorkspaceId\":\"org_src\",\"purchaseNewNumber\":true}", await BodyOfLastRequest());
        Assert.True(result.NewNumber);
        Assert.Equal("+18335550111", result.TollFreeNumber);
    }

    [Fact]
    public async Task InheritVerificationAsync_ByDefault_SendsOnlyTheSource()
    {
        _mockHandler.QueueResponse(HttpStatusCode.Created, @"{
            ""verificationId"": ""bv_2"",
            ""status"": ""verified"",
            ""type"": ""toll_free"",
            ""tollFreeNumber"": ""+18335550100"",
            ""inheritedFrom"": ""org_src""
        }");

        var result = await _client.Enterprise.Workspaces.InheritVerificationAsync("org_2", "org_src");

        Assert.Equal("{\"sourceWorkspaceId\":\"org_src\"}", await BodyOfLastRequest());
        Assert.Null(result.NewNumber);
        Assert.Equal("org_src", result.InheritedFrom);
    }

    private static List<BulkProvisionWorkspace> Workspaces(int count) =>
        Enumerable.Range(1, count).Select(i => new BulkProvisionWorkspace { Name = $"Clinic {i}" }).ToList();

    [Fact]
    public async Task ProvisionBulkAsync_SendsUpToTheApisLimitOf100()
    {
        _mockHandler.QueueResponse(HttpStatusCode.Created, @"{
            ""results"": [],
            ""summary"": {""total"": 100, ""succeeded"": 100, ""failed"": 0}
        }");

        var result = await _client.Enterprise.Workspaces.ProvisionBulkAsync(Workspaces(100));

        Assert.Single(_mockHandler.Requests);
        Assert.Equal(100, JsonDocument.Parse(await BodyOfLastRequest()).RootElement.GetProperty("workspaces").GetArrayLength());
        Assert.Equal(100, result.Summary.Total);
    }

    [Fact]
    public async Task ProvisionBulkAsync_MoreThan100_ThrowsBeforeSending()
    {
        await Assert.ThrowsAsync<Sendly.Exceptions.ValidationException>(
            () => _client.Enterprise.Workspaces.ProvisionBulkAsync(Workspaces(101)));

        Assert.Empty(_mockHandler.Requests);
    }
}
