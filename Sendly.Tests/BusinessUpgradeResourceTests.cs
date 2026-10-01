using System.Reflection;
using Sendly.Resources;
using Sendly.Tests.Fixtures;
using Xunit;

namespace Sendly.Tests;

/// <summary>
/// Tests for BusinessUpgradeResource request bodies.
/// </summary>
public class BusinessUpgradeResourceTests : IDisposable
{
    private readonly MockHttpMessageHandler _mockHandler;
    private readonly HttpClient _httpClient;
    private readonly SendlyClient _client;

    public BusinessUpgradeResourceTests()
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

    private const string DispositionResponseJson = @"{
        ""success"": true,
        ""disposition"": ""moved"",
        ""supersededVerificationId"": ""bv_old"",
        ""message"": ""The old number now belongs to the target workspace.""
    }";

    [Fact]
    public async Task SetDispositionAsync_Moved_SendsTheTargetOrgIdKeyTheApiReads()
    {
        _mockHandler.QueueSuccessResponse(DispositionResponseJson);

        var result = await _client.BusinessUpgrade.SetDispositionAsync("org_1", new DispositionRequest
        {
            Disposition = "moved",
            TargetWorkspaceId = "org_2"
        });

        Assert.Equal("{\"disposition\":\"moved\",\"targetOrgId\":\"org_2\"}",
            await _mockHandler.LastRequest!.Content!.ReadAsStringAsync());
        Assert.Equal("moved", result.Disposition);
    }

    [Fact]
    public async Task SetDispositionAsync_Released_SendsOnlyTheDisposition()
    {
        _mockHandler.QueueSuccessResponse(DispositionResponseJson.Replace(@"""moved""", @"""released"""));

        await _client.BusinessUpgrade.SetDispositionAsync("org_1", new DispositionRequest { Disposition = "released" });

        Assert.Equal("{\"disposition\":\"released\"}",
            await _mockHandler.LastRequest!.Content!.ReadAsStringAsync());
    }
}
