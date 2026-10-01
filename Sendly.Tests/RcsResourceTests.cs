using System.Net;
using System.Reflection;
using Sendly.Exceptions;
using Sendly.Tests.Fixtures;
using Xunit;

namespace Sendly.Tests;

/// <summary>
/// Tests for RcsResource - Agents List and recipient capability pre-flight.
/// </summary>
public class RcsResourceTests : IDisposable
{
    private readonly MockHttpMessageHandler _mockHandler;
    private readonly HttpClient _httpClient;
    private readonly SendlyClient _client;

    public RcsResourceTests()
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

    #region Agents ListAsync Tests

    [Fact]
    public async Task AgentsListAsync_ReturnsAgents()
    {
        // Arrange
        var responseJson = @"{
            ""agents"": [
                {
                    ""id"": ""agent_123"",
                    ""name"": ""Acme Inc"",
                    ""status"": ""approved"",
                    ""useCase"": ""TRANSACTIONAL"",
                    ""sendable"": true,
                    ""createdAt"": ""2026-07-01T10:00:00Z""
                },
                {
                    ""id"": ""agent_456"",
                    ""name"": ""Acme Support"",
                    ""status"": ""pending"",
                    ""useCase"": null,
                    ""sendable"": false,
                    ""createdAt"": ""2026-07-02T10:00:00Z""
                }
            ]
        }";
        _mockHandler.QueueSuccessResponse(responseJson);

        // Act
        var result = await _client.Rcs.Agents.ListAsync();

        // Assert
        Assert.Equal(2, result.Agents.Count);
        Assert.Equal("agent_123", result.Agents[0].Id);
        Assert.Equal("Acme Inc", result.Agents[0].Name);
        Assert.Equal("approved", result.Agents[0].Status);
        Assert.Equal("TRANSACTIONAL", result.Agents[0].UseCase);
        Assert.True(result.Agents[0].Sendable);
        Assert.Equal("pending", result.Agents[1].Status);
        Assert.Null(result.Agents[1].UseCase);
        Assert.False(result.Agents[1].Sendable);
    }

    [Fact]
    public async Task AgentsListAsync_WhenEmpty_ReturnsEmptyList()
    {
        // Arrange
        _mockHandler.QueueSuccessResponse(@"{""agents"": []}");

        // Act
        var result = await _client.Rcs.Agents.ListAsync();

        // Assert
        Assert.Empty(result.Agents);
    }

    [Fact]
    public async Task AgentsListAsync_HitsCorrectPath()
    {
        // Arrange
        _mockHandler.QueueSuccessResponse(@"{""agents"": []}");

        // Act
        await _client.Rcs.Agents.ListAsync();

        // Assert
        var request = _mockHandler.LastRequest;
        Assert.NotNull(request);
        Assert.Equal(HttpMethod.Get, request!.Method);
        Assert.Contains("rcs/agents", request.RequestUri?.ToString());
    }

    [Fact]
    public async Task AgentsListAsync_WhenChannelDark_ThrowsNotFoundException()
    {
        // Arrange — rcs_channel flag off for the account
        _mockHandler.QueueResponse(HttpStatusCode.NotFound, @"{""error"": ""not_found""}");

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _client.Rcs.Agents.ListAsync());
    }

    #endregion

    #region CapabilityAsync Tests

    [Fact]
    public async Task CapabilityAsync_WhenCapable_ReturnsFeatures()
    {
        // Arrange
        var responseJson = @"{
            ""to"": ""+15551234567"",
            ""agentId"": ""agent_123"",
            ""capable"": true,
            ""features"": [""RICHCARD_STANDALONE"", ""ACTION_OPEN_URL""]
        }";
        _mockHandler.QueueSuccessResponse(responseJson);

        // Act
        var result = await _client.Rcs.CapabilityAsync("+15551234567");

        // Assert
        Assert.Equal("+15551234567", result.To);
        Assert.Equal("agent_123", result.AgentId);
        Assert.True(result.Capable);
        Assert.Equal(2, result.Features.Count);
        Assert.Contains("RICHCARD_STANDALONE", result.Features);
    }

    [Fact]
    public async Task CapabilityAsync_WhenNotCapable_ReturnsNoFeatures()
    {
        // Arrange
        _mockHandler.QueueSuccessResponse(
            @"{""to"": ""+15551234567"", ""agentId"": ""agent_123"", ""capable"": false, ""features"": []}");

        // Act
        var result = await _client.Rcs.CapabilityAsync("+15551234567");

        // Assert
        Assert.False(result.Capable);
        Assert.Empty(result.Features);
    }

    [Fact]
    public async Task CapabilityAsync_HitsCorrectPathWithQuery()
    {
        // Arrange
        _mockHandler.QueueSuccessResponse(@"{""to"": ""+15551234567"", ""capable"": false, ""features"": []}");

        // Act
        await _client.Rcs.CapabilityAsync("+15551234567");

        // Assert
        var request = _mockHandler.LastRequest;
        Assert.NotNull(request);
        Assert.Equal(HttpMethod.Get, request!.Method);
        var uri = request.RequestUri?.ToString();
        Assert.Contains("rcs/capability", uri);
        Assert.Contains("to=%2B15551234567", uri);
        Assert.DoesNotContain("agentId", uri);
    }

    [Fact]
    public async Task CapabilityAsync_WithAgentId_SendsAgentIdQuery()
    {
        // Arrange
        _mockHandler.QueueSuccessResponse(@"{""to"": ""+15551234567"", ""capable"": true, ""features"": []}");

        // Act
        await _client.Rcs.CapabilityAsync("+15551234567", "agent_123");

        // Assert
        var uri = _mockHandler.LastRequest!.RequestUri?.ToString();
        Assert.Contains("to=%2B15551234567", uri);
        Assert.Contains("agentId=agent_123", uri);
    }

    [Theory]
    [InlineData("")]
    [InlineData("15551234567")]
    [InlineData("invalid")]
    public async Task CapabilityAsync_WithInvalidTo_ThrowsValidationException(string invalidTo)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _client.Rcs.CapabilityAsync(invalidTo));
    }

    [Fact]
    public async Task CapabilityAsync_WithTestKey_ThrowsSendlyException()
    {
        // Arrange — capability probes are carrier-backed, so they need a live
        // key.
        const string forbidden =
            @"{""error"": ""rcs_requires_live_key"", ""message"": ""RCS capability checks require a live API key. Test keys cannot query RCS capability.""}";
        _mockHandler.QueueResponse(HttpStatusCode.Forbidden, forbidden);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<SendlyException>(
            () => _client.Rcs.CapabilityAsync("+15551234567"));
        Assert.Contains("live API key", exception.Message);
        Assert.Equal(403, exception.StatusCode);
    }

    [Fact]
    public async Task CapabilityAsync_WithUnknownAgentId_ThrowsNotFoundException()
    {
        // Arrange
        _mockHandler.QueueResponse(HttpStatusCode.NotFound,
            @"{""error"": ""rcs_not_enabled"", ""message"": ""No RCS agent with that id is set up on this workspace.""}");

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => _client.Rcs.CapabilityAsync("+15551234567", "agent_nope"));
    }

    #endregion
}
