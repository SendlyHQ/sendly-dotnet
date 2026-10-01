using System.Net;
using System.Reflection;
using Sendly.Exceptions;
using Sendly.Tests.Fixtures;
using Xunit;

namespace Sendly.Tests;

public class PathIdTests : IDisposable
{
    private readonly MockHttpMessageHandler _mockHandler;
    private readonly HttpClient _httpClient;
    private readonly SendlyClient _client;

    public PathIdTests()
    {
        _mockHandler = new MockHttpMessageHandler();
        _httpClient = new HttpClient(_mockHandler)
        {
            BaseAddress = new Uri("https://api.test.com/api/v1/")
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

    [Theory]
    [InlineData("..")]
    [InlineData(".")]
    public async Task RevokeKeyAsync_DotSegmentKeyId_ThrowsBeforeSending(string keyId)
    {
        _mockHandler.QueueSuccessResponse(@"{""success"": true}");

        await Assert.ThrowsAsync<ValidationException>(
            () => _client.Enterprise.Workspaces.RevokeKeyAsync("ws_1", keyId));

        Assert.Empty(_mockHandler.Requests);
    }

    [Theory]
    [InlineData("..")]
    [InlineData(".")]
    [InlineData("")]
    public async Task RemoveContactAsync_DotSegmentOrEmptyContactId_ThrowsBeforeSending(string contactId)
    {
        _mockHandler.QueueSuccessResponse(@"{""success"": true}");

        await Assert.ThrowsAsync<ValidationException>(
            () => _client.Contacts.Lists.RemoveContactAsync("list_1", contactId));

        Assert.Empty(_mockHandler.Requests);
    }

    [Fact]
    public async Task GetAsync_DotSegmentWebhookId_ThrowsBeforeSending()
    {
        _mockHandler.QueueSuccessResponse(@"[]");

        await Assert.ThrowsAsync<ValidationException>(
            () => _client.Webhooks.GetAsync(".."));

        Assert.Empty(_mockHandler.Requests);
    }

    [Theory]
    [InlineData("key_1")]
    [InlineData("key.v1")]
    [InlineData("...")]
    public async Task RevokeKeyAsync_OrdinaryKeyId_IsSentInThePath(string keyId)
    {
        _mockHandler.QueueSuccessResponse(@"{""success"": true}");

        await _client.Enterprise.Workspaces.RevokeKeyAsync("ws_1", keyId);

        Assert.Single(_mockHandler.Requests);
        Assert.Equal(HttpMethod.Delete, _mockHandler.LastRequest!.Method);
        Assert.Equal($"/api/v1/enterprise/workspaces/ws_1/keys/{keyId}", _mockHandler.LastRequest.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task RemoveContactAsync_OrdinaryIds_IsSentInThePath()
    {
        _mockHandler.QueueSuccessResponse(@"{""success"": true}");

        await _client.Contacts.Lists.RemoveContactAsync("list_1", "contact_1");

        Assert.Single(_mockHandler.Requests);
        Assert.Equal("/api/v1/contact-lists/list_1/contacts/contact_1", _mockHandler.LastRequest!.RequestUri!.AbsolutePath);
    }
}
