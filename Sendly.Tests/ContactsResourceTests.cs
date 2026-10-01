using System.Net;
using System.Reflection;
using System.Text.Json;
using Sendly.Models;
using Sendly.Tests.Fixtures;
using Xunit;

namespace Sendly.Tests;

/// <summary>
/// Tests for ContactsResource request bodies and the responses the contact
/// endpoints send.
/// </summary>
public class ContactsResourceTests : IDisposable
{
    private readonly MockHttpMessageHandler _mockHandler;
    private readonly HttpClient _httpClient;
    private readonly SendlyClient _client;

    public ContactsResourceTests()
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

    private async Task<JsonElement> JsonBodyOfLastRequest()
    {
        using var doc = JsonDocument.Parse(await BodyOfLastRequest());
        return doc.RootElement.Clone();
    }

    [Fact]
    public async Task ImportAsync_SendsTheListIdAndOptedInAtKeysTheApiReads()
    {
        _mockHandler.QueueResponse(HttpStatusCode.Created,
            @"{""imported"":1,""skippedDuplicates"":0,""errors"":[],""totalErrors"":0}");

        await _client.Contacts.ImportAsync(new ImportContactsRequest
        {
            Contacts = new List<ImportContactItem> { new() { Phone = "+15551234567", Name = "Ana" } },
            ListId = "lst_1",
            OptedInAt = "2026-01-01T00:00:00Z"
        });

        var body = await JsonBodyOfLastRequest();
        Assert.Equal("lst_1", body.GetProperty("listId").GetString());
        Assert.Equal("2026-01-01T00:00:00Z", body.GetProperty("optedInAt").GetString());
        Assert.False(body.TryGetProperty("list_id", out _));
        Assert.False(body.TryGetProperty("opted_in_at", out _));
    }

    [Fact]
    public async Task ImportAsync_ReadsTheCountsTheApiSends()
    {
        _mockHandler.QueueResponse(HttpStatusCode.Created, @"{
            ""imported"": 1,
            ""skippedDuplicates"": 2,
            ""errors"": [{ ""index"": 3, ""phone"": ""555"", ""error"": ""Invalid E.164 format"" }],
            ""totalErrors"": 1
        }");

        var result = await _client.Contacts.ImportAsync(new ImportContactsRequest
        {
            Contacts = new List<ImportContactItem> { new() { Phone = "+15551234567" } }
        });

        Assert.Equal(1, result.Imported);
        Assert.Equal(2, result.SkippedDuplicates);
        Assert.Equal(1, result.TotalErrors);
        var error = Assert.Single(result.Errors);
        Assert.Equal(3, error.Index);
        Assert.Equal("Invalid E.164 format", error.Error);
    }

    [Fact]
    public async Task BulkMarkValidAsync_ByList_SendsTheListIdKeyTheApiReads()
    {
        _mockHandler.QueueSuccessResponse(@"{""cleared"":4}");

        var result = await _client.Contacts.BulkMarkValidAsync(new BulkMarkValidRequest { ListId = "lst_1" });

        Assert.Equal("{\"listId\":\"lst_1\"}", await BodyOfLastRequest());
        Assert.Equal(4, result.Cleared);
    }

    [Fact]
    public async Task BulkMarkValidAsync_ByIds_SendsTheIds()
    {
        _mockHandler.QueueSuccessResponse(@"{""cleared"":2}");

        await _client.Contacts.BulkMarkValidAsync(new BulkMarkValidRequest { Ids = new List<string> { "ct_1", "ct_2" } });

        Assert.Equal("{\"ids\":[\"ct_1\",\"ct_2\"]}", await BodyOfLastRequest());
    }

    [Fact]
    public async Task CheckNumbersAsync_ForAList_SendsTheListIdKeyTheApiReads()
    {
        _mockHandler.QueueSuccessResponse(@"{""success"":true,""alreadyRunning"":false,""message"":""Number lookup started.""}");

        await _client.Contacts.CheckNumbersAsync(new CheckNumbersRequest { ListId = "lst_1", Force = true });

        var body = await JsonBodyOfLastRequest();
        Assert.Equal("lst_1", body.GetProperty("listId").GetString());
        Assert.True(body.GetProperty("force").GetBoolean());
        Assert.False(body.TryGetProperty("list_id", out _));
    }

    [Fact]
    public async Task CheckNumbersAsync_ForEveryContact_SendsNoListId()
    {
        _mockHandler.QueueSuccessResponse(@"{""success"":true,""alreadyRunning"":false,""message"":""Number lookup started.""}");

        await _client.Contacts.CheckNumbersAsync();

        Assert.Equal("{\"force\":false}", await BodyOfLastRequest());
    }

    [Fact]
    public async Task CheckNumbersAsync_ReadsAlreadyRunning()
    {
        _mockHandler.QueueSuccessResponse(@"{
            ""success"": true,
            ""alreadyRunning"": true,
            ""message"": ""A carrier lookup is already in progress for this scope.""
        }");

        var result = await _client.Contacts.CheckNumbersAsync(new CheckNumbersRequest { ListId = "lst_1" });

        Assert.True(result.Success);
        Assert.True(result.AlreadyRunning);
    }
}
