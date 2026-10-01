using System.Net;
using System.Reflection;
using System.Text.Json;
using Sendly.Models;
using Sendly.Tests.Fixtures;
using Xunit;

namespace Sendly.Tests;

/// <summary>
/// Tests for RulesResource against the rule rows the API stores and returns.
/// </summary>
public class RulesResourceTests : IDisposable
{
    private readonly MockHttpMessageHandler _mockHandler;
    private readonly HttpClient _httpClient;
    private readonly SendlyClient _client;

    public RulesResourceTests()
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

    private const string RuleRow = @"{
        ""id"": ""r_1"",
        ""userId"": ""user_1"",
        ""organizationId"": ""org_1"",
        ""name"": ""Complaints"",
        ""conditions"": { ""intent"": ""complaint"", ""intentConfidenceMin"": 0.8 },
        ""actions"": { ""addLabels"": [""lbl_1""], ""closeConversation"": false },
        ""enabled"": true,
        ""priority"": 0,
        ""createdAt"": ""2026-09-01T00:00:00.000Z"",
        ""updatedAt"": ""2026-09-02T00:00:00.000Z""
    }";

    [Fact]
    public async Task ListAsync_ReadsTheObjectShapedRulesTheApiStores()
    {
        _mockHandler.QueueSuccessResponse($@"{{ ""data"": [{RuleRow}] }}");

        var rules = await _client.Rules.ListAsync();

        var rule = Assert.Single(rules.Data);
        var conditions = Assert.Single(rule.Conditions);
        Assert.Equal("complaint", conditions["intent"].ToString());
        var actions = Assert.Single(rule.Actions);
        Assert.Equal("lbl_1", ((JsonElement)actions["addLabels"])[0].GetString());
        Assert.Equal("2026-09-01T00:00:00.000Z", rule.CreatedAt);
    }

    [Fact]
    public async Task CreateAsync_SendsConditionsAndActionsAsObjects()
    {
        _mockHandler.QueueResponse(HttpStatusCode.Created, RuleRow);

        await _client.Rules.CreateAsync(new CreateRuleRequest
        {
            Name = "Complaints",
            Conditions = new List<Dictionary<string, object>>
            {
                new() { ["intent"] = "complaint" },
                new() { ["intentConfidenceMin"] = 0.8 }
            },
            Actions = new List<Dictionary<string, object>>
            {
                new() { ["addLabels"] = new[] { "lbl_1" } }
            }
        });

        var body = await JsonBodyOfLastRequest();
        Assert.Equal(JsonValueKind.Object, body.GetProperty("conditions").ValueKind);
        Assert.Equal("complaint", body.GetProperty("conditions").GetProperty("intent").GetString());
        Assert.Equal(0.8, body.GetProperty("conditions").GetProperty("intentConfidenceMin").GetDouble());
        Assert.Equal(JsonValueKind.Object, body.GetProperty("actions").ValueKind);
        Assert.Equal("lbl_1", body.GetProperty("actions").GetProperty("addLabels")[0].GetString());
    }

    [Fact]
    public async Task UpdateAsync_SendsConditionsAsAnObject()
    {
        _mockHandler.QueueSuccessResponse(RuleRow);

        await _client.Rules.UpdateAsync("r_1", new UpdateRuleRequest
        {
            Conditions = new List<Dictionary<string, object>>
            {
                new() { ["sentiment"] = new[] { "negative" } }
            }
        });

        var body = await JsonBodyOfLastRequest();
        Assert.Equal(JsonValueKind.Object, body.GetProperty("conditions").ValueKind);
        Assert.Equal("negative", body.GetProperty("conditions").GetProperty("sentiment")[0].GetString());
    }

    [Fact]
    public async Task ListAsync_StillReadsAnArrayShapedRule()
    {
        _mockHandler.QueueSuccessResponse(@"{ ""data"": [{
            ""id"": ""r_old"",
            ""name"": ""Old"",
            ""conditions"": [{ ""field"": ""intent"", ""value"": ""complaint"" }],
            ""actions"": [{ ""type"": ""add_label"", ""labelId"": ""lbl_1"" }],
            ""enabled"": true,
            ""priority"": 1,
            ""createdAt"": ""2026-09-01T00:00:00.000Z"",
            ""updatedAt"": ""2026-09-01T00:00:00.000Z""
        }] }");

        var rules = await _client.Rules.ListAsync();

        var rule = Assert.Single(rules.Data);
        Assert.Equal("intent", Assert.Single(rule.Conditions)["field"].ToString());
        Assert.Equal("add_label", Assert.Single(rule.Actions)["type"].ToString());
        Assert.Equal(1, rule.Priority);
    }

    [Fact]
    public async Task ListAsync_ToleratesARuleWhosePriorityWasClearedToNull()
    {
        _mockHandler.QueueSuccessResponse($@"{{ ""data"": [{RuleRow.Replace(@"""priority"": 0", @"""priority"": null")}] }}");

        var rules = await _client.Rules.ListAsync();

        Assert.Equal(0, Assert.Single(rules.Data).Priority);
    }
}
