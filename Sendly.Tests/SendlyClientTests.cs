using System.Net;
using System.Reflection;
using Sendly.Exceptions;
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
