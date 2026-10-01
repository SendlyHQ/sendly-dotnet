using System.Reflection;
using Sendly.Tests.Fixtures;
using Xunit;

namespace Sendly.Tests;

/// <summary>
/// Tests for MessageTemplatesResource against the bodies the template
/// endpoints send.
/// </summary>
public class MessageTemplatesResourceTests : IDisposable
{
    private readonly MockHttpMessageHandler _mockHandler;
    private readonly HttpClient _httpClient;
    private readonly SendlyClient _client;

    public MessageTemplatesResourceTests()
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

    [Fact]
    public async Task PreviewAsync_ReadsTheRenderedTextTheApiSends()
    {
        _mockHandler.QueueSuccessResponse(@"{
            ""template_id"": ""tmpl_1"",
            ""original_text"": ""Your code is {{code}}"",
            ""rendered_text"": ""Your code is 1234"",
            ""character_count"": 17,
            ""segment_count"": 1
        }");

        var preview = await _client.MessageTemplates.PreviewAsync("tmpl_1",
            new Dictionary<string, string> { ["code"] = "1234" });

        Assert.Equal("Your code is 1234", preview.PreviewText);
        Assert.Equal("Your code is {{code}}", preview.OriginalText);
        Assert.Equal("tmpl_1", preview.TemplateId);
        Assert.Equal(17, preview.CharacterCount);
        Assert.Equal(1, preview.SegmentCount);
        Assert.Equal("{\"variables\":{\"code\":\"1234\"}}", await _mockHandler.LastRequest!.Content!.ReadAsStringAsync());
    }
}
