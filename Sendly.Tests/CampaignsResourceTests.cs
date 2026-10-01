using System.Net;
using System.Reflection;
using System.Text.Json;
using Sendly.Models;
using Sendly.Tests.Fixtures;
using Xunit;

namespace Sendly.Tests;

/// <summary>
/// Tests for CampaignsResource against the campaign rows the API returns.
/// </summary>
public class CampaignsResourceTests : IDisposable
{
    private readonly MockHttpMessageHandler _mockHandler;
    private readonly HttpClient _httpClient;
    private readonly SendlyClient _client;

    public CampaignsResourceTests()
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

    private const string CampaignJson = @"{
        ""id"": ""camp_1"",
        ""userId"": ""user_1"",
        ""organizationId"": ""org_1"",
        ""name"": ""Spring sale"",
        ""status"": ""completed"",
        ""messageText"": ""20% off this weekend"",
        ""fromSender"": null,
        ""targetType"": ""contact_list"",
        ""targetListId"": ""lst_1"",
        ""manualRecipients"": null,
        ""excludeOptedOut"": true,
        ""sendNow"": false,
        ""scheduledAt"": ""2026-09-20T15:00:00.000Z"",
        ""timezone"": ""America/New_York"",
        ""batchId"": ""batch_1"",
        ""totalRecipients"": 10,
        ""estimatedCredits"": 20,
        ""sentCount"": 10,
        ""deliveredCount"": 9,
        ""failedCount"": 1,
        ""creditsUsed"": 20,
        ""creditsRefunded"": 0,
        ""createdAt"": ""2026-09-19T12:00:00.000Z"",
        ""updatedAt"": ""2026-09-20T15:05:00.000Z"",
        ""sentAt"": ""2026-09-20T15:00:01.000Z"",
        ""completedAt"": ""2026-09-20T15:05:00.000Z"",
        ""text"": ""20% off this weekend"",
        ""contact_list_ids"": [""lst_1""],
        ""created_at"": ""2026-09-19T12:00:00.000Z"",
        ""updated_at"": ""2026-09-20T15:05:00.000Z""
    }";

    [Fact]
    public async Task GetAsync_ReadsTheCountsAndDatesTheApiSends()
    {
        _mockHandler.QueueSuccessResponse(CampaignJson);

        var campaign = await _client.Campaigns.GetAsync("camp_1");

        Assert.Equal(10, campaign.SentCount);
        Assert.Equal(9, campaign.DeliveredCount);
        Assert.Equal(1, campaign.FailedCount);
        Assert.Equal(10, campaign.RecipientCount);
        Assert.Equal(20, campaign.EstimatedCredits);
        Assert.Equal(20, campaign.CreditsUsed);
        Assert.Equal("2026-09-20T15:00:00.000Z", campaign.ScheduledAt);
        Assert.Equal("2026-09-20T15:00:01.000Z", campaign.StartedAt);
        Assert.Equal("2026-09-20T15:05:00.000Z", campaign.CompletedAt);
        Assert.Equal("20% off this weekend", campaign.Text);
        Assert.Equal(new[] { "lst_1" }, campaign.ContactListIds);
        Assert.Equal("2026-09-19T12:00:00.000Z", campaign.CreatedAt);
        Assert.Equal("America/New_York", campaign.Timezone);
        Assert.Equal("batch_1", campaign.BatchId);
    }

    [Fact]
    public async Task ListAsync_ReadsTheCountsOfEachCampaign()
    {
        _mockHandler.QueueSuccessResponse($@"{{ ""campaigns"": [{CampaignJson}], ""total"": 1, ""limit"": 50, ""offset"": 0 }}");

        var list = await _client.Campaigns.ListAsync();

        var campaign = Assert.Single(list.Campaigns);
        Assert.Equal(10, campaign.SentCount);
        Assert.Equal(1, list.Total);
    }

    private const string CampaignSendJson = @"{
        ""batchId"": ""batch_1"",
        ""status"": ""partial_failure"",
        ""total"": 10,
        ""sent"": 8,
        ""failed"": 1,
        ""retrying"": 0,
        ""optedOutSkipped"": 1,
        ""invalidSkipped"": 0,
        ""creditsUsed"": 16,
        ""creditsRefunded"": 2,
        ""messages"": [
            { ""index"": 0, ""id"": ""msg_1"", ""to"": ""+15551234567"", ""status"": ""queued"" },
            { ""index"": 1, ""id"": ""msg_2"", ""to"": ""+15551234568"", ""status"": ""failed"", ""error"": ""Invalid number"" }
        ]
    }";

    [Fact]
    public async Task SendAsync_ReadsTheCountsOfTheBatchTheApiAnswersWith()
    {
        _mockHandler.QueueSuccessResponse(CampaignSendJson);

        var campaign = await _client.Campaigns.SendAsync("camp_1");

        Assert.Equal("camp_1", campaign.Id);
        Assert.Equal("batch_1", campaign.BatchId);
        Assert.Equal(10, campaign.RecipientCount);
        Assert.Equal(8, campaign.SentCount);
        Assert.Equal(1, campaign.FailedCount);
        Assert.Equal(16, campaign.CreditsUsed);
        Assert.Equal("partial_failure", campaign.Status);
        Assert.EndsWith("/campaigns/camp_1/send", _mockHandler.LastRequest!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task ScheduleAsync_SendsTheScheduledAtKeyTheApiReads()
    {
        _mockHandler.QueueSuccessResponse(CampaignJson.Replace(@"""status"": ""completed""", @"""status"": ""scheduled"""));

        await _client.Campaigns.ScheduleAsync("camp_1", new ScheduleCampaignRequest { ScheduledAt = "2030-01-01T10:00:00Z" });

        var body = await JsonBodyOfLastRequest();
        Assert.Equal("2030-01-01T10:00:00Z", body.GetProperty("scheduledAt").GetString());
        Assert.False(body.TryGetProperty("scheduled_at", out _));
        Assert.False(body.TryGetProperty("timezone", out _));
    }

    [Fact]
    public async Task ScheduleAsync_WithATimezone_SendsIt()
    {
        _mockHandler.QueueSuccessResponse(CampaignJson.Replace(@"""status"": ""completed""", @"""status"": ""scheduled"""));

        await _client.Campaigns.ScheduleAsync("camp_1", new ScheduleCampaignRequest
        {
            ScheduledAt = "2030-01-01T10:00:00Z",
            Timezone = "Europe/London"
        });

        var body = await JsonBodyOfLastRequest();
        Assert.Equal("Europe/London", body.GetProperty("timezone").GetString());
    }

    [Fact]
    public async Task PreviewAsync_ReadsThePreviewTheApiSends()
    {
        _mockHandler.QueueSuccessResponse(@"{
            ""totalRecipients"": 10,
            ""estimatedCredits"": 20,
            ""optedOutCount"": 2,
            ""invalidCount"": 1,
            ""sampleRecipients"": [{ ""phone"": ""+15551234567"", ""name"": ""Ana"" }],
            ""blockedCount"": 1,
            ""sendableCount"": 9,
            ""warnings"": [""1 recipient is in a blocked country""],
            ""recipientCount"": 10,
            ""currentBalance"": 500,
            ""hasEnoughCredits"": true
        }");

        var preview = await _client.Campaigns.PreviewAsync("camp_1");

        Assert.Equal(10, preview.RecipientCount);
        Assert.Equal(20, preview.EstimatedCredits);
        Assert.Equal(9, preview.SendableCount);
        Assert.Equal(1, preview.BlockedCount);
        Assert.Equal("1 recipient is in a blocked country", Assert.Single(preview.Warnings!));
        Assert.Equal(2, preview.OptedOutCount);
        Assert.Equal(1, preview.InvalidCount);
        Assert.Equal(500, preview.CurrentBalance);
        Assert.True(preview.HasEnoughCredits);
    }

    [Fact]
    public void CampaignStatus_ListsTheStatusASentCampaignGets()
    {
        var values = typeof(CampaignStatus)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Select(f => (string?)f.GetValue(null));

        Assert.Contains("completed", values);
    }
}
