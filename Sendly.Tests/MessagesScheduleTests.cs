using System.Net;
using System.Reflection;
using Sendly.Exceptions;
using Sendly.Models;
using Sendly.Tests.Fixtures;
using Xunit;

namespace Sendly.Tests;

/// <summary>
/// Tests for scheduled message operations.
/// </summary>
public class MessagesScheduleTests : IDisposable
{
    private readonly MockHttpMessageHandler _mockHandler;
    private readonly HttpClient _httpClient;
    private readonly SendlyClient _client;

    public MessagesScheduleTests()
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

    #region ScheduleAsync Tests

    [Fact]
    public async Task ScheduleAsync_WithValidParameters_ReturnsScheduledMessage()
    {
        // Arrange
        var responseJson = @"{
            ""data"": {
                ""id"": ""sched_123"",
                ""to"": ""+15551234567"",
                ""text"": ""Scheduled message"",
                ""scheduled_at"": ""2025-01-20T15:00:00Z"",
                ""status"": ""scheduled"",
                ""credits_reserved"": 1,
                ""created_at"": ""2024-01-20T10:00:00Z""
            }
        }";
        _mockHandler.QueueSuccessResponse(responseJson);

        // Act
        var message = await _client.Messages.ScheduleAsync(
            "+15551234567",
            "Scheduled message",
            "2025-01-20T15:00:00Z");

        // Assert
        Assert.NotNull(message);
        Assert.Equal("sched_123", message.Id);
        Assert.Equal("+15551234567", message.To);
        Assert.Equal("Scheduled message", message.Text);
        Assert.NotEqual(default(DateTime), message.ScheduledAt);
        Assert.Equal("scheduled", message.Status);
    }

    [Fact]
    public async Task ScheduleAsync_WithRequestObject_ReturnsScheduledMessage()
    {
        // Arrange
        var responseJson = @"{
            ""data"": {
                ""id"": ""sched_456"",
                ""to"": ""+15559876543"",
                ""text"": ""Test scheduled"",
                ""scheduled_at"": ""2025-02-01T10:00:00Z"",
                ""status"": ""scheduled"",
                ""credits_reserved"": 1,
                ""created_at"": ""2024-01-20T10:00:00Z""
            }
        }";
        _mockHandler.QueueSuccessResponse(responseJson);

        var request = new ScheduleMessageRequest(
            "+15559876543",
            "Test scheduled",
            "2025-02-01T10:00:00Z");

        // Act
        var message = await _client.Messages.ScheduleAsync(request);

        // Assert
        Assert.NotNull(message);
        Assert.Equal("sched_456", message.Id);
    }

    [Theory]
    [InlineData("1234567890")]
    [InlineData("invalid")]
    [InlineData("")]
    public async Task ScheduleAsync_WithInvalidPhoneNumber_ThrowsValidationException(string invalidPhone)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _client.Messages.ScheduleAsync(invalidPhone, "Test", "2025-01-20T10:00:00Z"));
    }

    [Fact]
    public async Task ScheduleAsync_WithNullPhoneNumber_ThrowsValidationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _client.Messages.ScheduleAsync(null!, "Test", "2025-01-20T10:00:00Z"));
    }

    [Fact]
    public async Task ScheduleAsync_WithEmptyText_ThrowsValidationException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _client.Messages.ScheduleAsync("+15551234567", "", "2025-01-20T10:00:00Z"));

        Assert.Contains("Message text is required", exception.Message);
    }

    [Fact]
    public async Task ScheduleAsync_WithNullText_ThrowsValidationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _client.Messages.ScheduleAsync("+15551234567", null!, "2025-01-20T10:00:00Z"));
    }

    [Fact]
    public async Task ScheduleAsync_WithTooLongText_ThrowsValidationException()
    {
        // Arrange
        var longText = new string('a', 1601);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _client.Messages.ScheduleAsync("+15551234567", longText, "2025-01-20T10:00:00Z"));

        Assert.Contains("exceeds maximum length", exception.Message);
    }

    [Fact]
    public async Task ScheduleAsync_WithEmptyScheduledAt_ThrowsValidationException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _client.Messages.ScheduleAsync("+15551234567", "Test", ""));

        Assert.Contains("Scheduled time is required", exception.Message);
    }

    [Fact]
    public async Task ScheduleAsync_WithNullScheduledAt_ThrowsValidationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _client.Messages.ScheduleAsync("+15551234567", "Test", null!));
    }

    [Theory]
    [InlineData("invalid-date")]
    [InlineData("2025-01-20")]
    [InlineData("10:00:00")]
    [InlineData("not-a-date")]
    public async Task ScheduleAsync_WithInvalidDateFormat_ThrowsValidationException(string invalidDate)
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _client.Messages.ScheduleAsync("+15551234567", "Test", invalidDate));

        Assert.Contains("Invalid scheduled time format", exception.Message);
    }

    [Theory]
    [InlineData("2025-01-20T10:00:00Z")]
    [InlineData("2025-12-31T23:59:59Z")]
    [InlineData("2026-06-15T12:30:00Z")]
    public async Task ScheduleAsync_WithValidISO8601Formats_Succeeds(string validDate)
    {
        // Arrange
        var responseJson = $@"{{
            ""data"": {{
                ""id"": ""sched_test"",
                ""to"": ""+15551234567"",
                ""text"": ""Test"",
                ""scheduled_at"": ""{validDate}"",
                ""status"": ""scheduled"",
                ""credits_reserved"": 1,
                ""created_at"": ""2024-01-20T10:00:00Z""
            }}
        }}";
        _mockHandler.QueueSuccessResponse(responseJson);

        // Act
        var message = await _client.Messages.ScheduleAsync("+15551234567", "Test", validDate);

        // Assert
        Assert.NotNull(message);
    }

    [Fact]
    public async Task ScheduleAsync_With401Response_ThrowsAuthenticationException()
    {
        // Arrange
        _mockHandler.QueueResponse(HttpStatusCode.Unauthorized,
            @"{""message"": ""Invalid API key""}");

        // Act & Assert
        await Assert.ThrowsAsync<AuthenticationException>(
            () => _client.Messages.ScheduleAsync("+15551234567", "Test", "2025-01-20T10:00:00Z"));
    }

    [Fact]
    public async Task ScheduleAsync_With402Response_ThrowsInsufficientCreditsException()
    {
        // Arrange
        _mockHandler.QueueResponse(HttpStatusCode.PaymentRequired,
            @"{""error"": ""Insufficient credits""}");

        // Act & Assert
        await Assert.ThrowsAsync<InsufficientCreditsException>(
            () => _client.Messages.ScheduleAsync("+15551234567", "Test", "2025-01-20T10:00:00Z"));
    }

    [Fact]
    public async Task ScheduleAsync_With429Response_ThrowsRateLimitException()
    {
        // Arrange - Queue multiple 429 responses for all retry attempts
        for (int i = 0; i < 4; i++)
        {
            var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests)
            {
                Content = new StringContent(@"{""message"": ""Rate limit exceeded""}",
                    System.Text.Encoding.UTF8, "application/json")
            };
            response.Headers.Add("Retry-After", "1");
            _mockHandler.QueueResponse(response);
        }

        // Act & Assert
        var exception = await Assert.ThrowsAsync<RateLimitException>(
            () => _client.Messages.ScheduleAsync("+15551234567", "Test", "2025-01-20T10:00:00Z"));

        Assert.Equal(TimeSpan.FromSeconds(1), exception.RetryAfter);
    }

    [Fact]
    public async Task ScheduleAsync_With500Response_RetriesAndThrows()
    {
        // Arrange
        for (int i = 0; i < 4; i++)
        {
            _mockHandler.QueueResponse(HttpStatusCode.InternalServerError,
                @"{""error"": ""Server error""}");
        }

        // Act & Assert
        await Assert.ThrowsAsync<SendlyException>(
            () => _client.Messages.ScheduleAsync("+15551234567", "Test", "2025-01-20T10:00:00Z"));

        // Verify retries occurred
        Assert.Equal(4, _mockHandler.Requests.Count);
    }

    #endregion

    #region ListScheduledAsync Tests

    [Fact]
    public async Task ListScheduledAsync_WithoutOptions_ReturnsScheduledMessageList()
    {
        // Arrange
        var responseJson = @"{
            ""data"": [
                {
                    ""id"": ""sched_1"",
                    ""to"": ""+15551234567"",
                    ""text"": ""Scheduled 1"",
                    ""scheduled_at"": ""2025-01-20T10:00:00Z"",
                    ""status"": ""scheduled"",
                    ""credits_reserved"": 1,
                    ""created_at"": ""2024-01-20T09:00:00Z""
                },
                {
                    ""id"": ""sched_2"",
                    ""to"": ""+15559876543"",
                    ""text"": ""Scheduled 2"",
                    ""scheduled_at"": ""2025-01-21T10:00:00Z"",
                    ""status"": ""scheduled"",
                    ""credits_reserved"": 1,
                    ""created_at"": ""2024-01-20T09:30:00Z""
                }
            ],
            ""has_more"": false,
            ""total"": 2
        }";
        _mockHandler.QueueSuccessResponse(responseJson);

        // Act
        var result = await _client.Messages.ListScheduledAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.False(result.HasMore);
        Assert.Equal(2, result.Total);
    }

    [Fact]
    public async Task ListScheduledAsync_WithOptions_SendsCorrectQueryParameters()
    {
        // Arrange
        var responseJson = @"{""data"": [], ""has_more"": false, ""total"": 0}";
        _mockHandler.QueueSuccessResponse(responseJson);

        var options = new ListScheduledMessagesOptions
        {
            Limit = 25,
            Offset = 50,
            Status = "scheduled"
        };

        // Act
        await _client.Messages.ListScheduledAsync(options);

        // Assert
        var request = _mockHandler.LastRequest;
        Assert.NotNull(request);
        Assert.Contains("limit=25", request.RequestUri?.Query);
        Assert.Contains("offset=50", request.RequestUri?.Query);
        Assert.Contains("status=scheduled", request.RequestUri?.Query);
    }

    [Fact]
    public async Task ListScheduledAsync_With401Response_ThrowsAuthenticationException()
    {
        // Arrange
        _mockHandler.QueueResponse(HttpStatusCode.Unauthorized,
            @"{""message"": ""Invalid API key""}");

        // Act & Assert
        await Assert.ThrowsAsync<AuthenticationException>(
            () => _client.Messages.ListScheduledAsync());
    }

    [Fact]
    public async Task ListScheduledAsync_WithPagination_ReturnsCorrectData()
    {
        // Arrange
        var responseJson = @"{
            ""data"": [
                {
                    ""id"": ""sched_page1"",
                    ""to"": ""+15551234567"",
                    ""text"": ""Page 1"",
                    ""scheduled_at"": ""2025-01-20T10:00:00Z"",
                    ""status"": ""scheduled"",
                    ""credits_reserved"": 1,
                    ""created_at"": ""2024-01-20T09:00:00Z""
                }
            ],
            ""has_more"": true,
            ""total"": 100
        }";
        _mockHandler.QueueSuccessResponse(responseJson);

        // Act
        var result = await _client.Messages.ListScheduledAsync(new ListScheduledMessagesOptions { Limit = 1 });

        // Assert
        Assert.True(result.HasMore);
        Assert.Equal(100, result.Total);
        Assert.Single(result);
    }

    #endregion

    #region GetScheduledAsync Tests

    [Fact]
    public async Task GetScheduledAsync_WithValidId_ReturnsScheduledMessage()
    {
        // Arrange
        var responseJson = @"{
            ""data"": {
                ""id"": ""sched_xyz"",
                ""to"": ""+15551234567"",
                ""text"": ""Retrieved scheduled message"",
                ""scheduled_at"": ""2025-01-20T15:00:00Z"",
                ""status"": ""scheduled"",
                ""credits_reserved"": 1,
                ""created_at"": ""2024-01-20T10:00:00Z""
            }
        }";
        _mockHandler.QueueSuccessResponse(responseJson);

        // Act
        var message = await _client.Messages.GetScheduledAsync("sched_xyz");

        // Assert
        Assert.NotNull(message);
        Assert.Equal("sched_xyz", message.Id);
        Assert.Equal("Retrieved scheduled message", message.Text);
        Assert.Equal("scheduled", message.Status);
    }

    [Fact]
    public async Task GetScheduledAsync_WithEmptyId_ThrowsValidationException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _client.Messages.GetScheduledAsync(""));

        Assert.Contains("Scheduled message ID is required", exception.Message);
    }

    [Fact]
    public async Task GetScheduledAsync_WithNullId_ThrowsValidationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _client.Messages.GetScheduledAsync(null!));
    }

    [Fact]
    public async Task GetScheduledAsync_With404Response_ThrowsNotFoundException()
    {
        // Arrange
        _mockHandler.QueueResponse(HttpStatusCode.NotFound,
            @"{""message"": ""Scheduled message not found""}");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => _client.Messages.GetScheduledAsync("sched_nonexistent"));

        Assert.Equal("Scheduled message not found", exception.Message);
        Assert.Equal(404, exception.StatusCode);
    }

    [Fact]
    public async Task GetScheduledAsync_With401Response_ThrowsAuthenticationException()
    {
        // Arrange
        _mockHandler.QueueResponse(HttpStatusCode.Unauthorized,
            @"{""message"": ""Invalid API key""}");

        // Act & Assert
        await Assert.ThrowsAsync<AuthenticationException>(
            () => _client.Messages.GetScheduledAsync("sched_123"));
    }

    [Fact]
    public async Task GetScheduledAsync_WithSpecialCharactersInId_EncodesCorrectly()
    {
        // Arrange
        var responseJson = @"{
            ""data"": {
                ""id"": ""sched/special+id"",
                ""to"": ""+15551234567"",
                ""text"": ""Test"",
                ""scheduled_at"": ""2025-01-20T15:00:00Z"",
                ""status"": ""scheduled"",
                ""credits_reserved"": 1,
                ""created_at"": ""2024-01-20T10:00:00Z""
            }
        }";
        _mockHandler.QueueSuccessResponse(responseJson);

        // Act
        var message = await _client.Messages.GetScheduledAsync("sched/special+id");

        // Assert
        var request = _mockHandler.LastRequest;
        Assert.NotNull(request);
        Assert.Contains("sched%2Fspecial%2Bid", request.RequestUri?.ToString());
    }

    #endregion

    #region CancelScheduledAsync Tests

    [Fact]
    public async Task CancelScheduledAsync_WithValidId_ReturnsCancellationResponse()
    {
        // Arrange
        var responseJson = @"{
            ""id"": ""sched_123"",
            ""status"": ""cancelled"",
            ""credits_refunded"": 1,
            ""cancelled_at"": ""2024-01-20T12:00:00Z""
        }";
        _mockHandler.QueueSuccessResponse(responseJson);

        // Act
        var response = await _client.Messages.CancelScheduledAsync("sched_123");

        // Assert
        Assert.NotNull(response);
        Assert.Equal("sched_123", response.Id);
        Assert.Equal(1, response.CreditsRefunded);
        Assert.NotNull(response.CancelledAt);
    }

    [Fact]
    public async Task CancelScheduledAsync_WithEmptyId_ThrowsValidationException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _client.Messages.CancelScheduledAsync(""));

        Assert.Contains("Scheduled message ID is required", exception.Message);
    }

    [Fact]
    public async Task CancelScheduledAsync_WithNullId_ThrowsValidationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _client.Messages.CancelScheduledAsync(null!));
    }

    [Fact]
    public async Task CancelScheduledAsync_With404Response_ThrowsNotFoundException()
    {
        // Arrange
        _mockHandler.QueueResponse(HttpStatusCode.NotFound,
            @"{""message"": ""Scheduled message not found""}");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => _client.Messages.CancelScheduledAsync("sched_nonexistent"));

        Assert.Equal("Scheduled message not found", exception.Message);
    }

    [Fact]
    public async Task CancelScheduledAsync_With401Response_ThrowsAuthenticationException()
    {
        // Arrange
        _mockHandler.QueueResponse(HttpStatusCode.Unauthorized,
            @"{""message"": ""Invalid API key""}");

        // Act & Assert
        await Assert.ThrowsAsync<AuthenticationException>(
            () => _client.Messages.CancelScheduledAsync("sched_123"));
    }

    [Fact]
    public async Task CancelScheduledAsync_With400Response_ThrowsValidationException()
    {
        // Arrange
        _mockHandler.QueueResponse(HttpStatusCode.BadRequest,
            @"{""message"": ""Message has already been sent""}");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _client.Messages.CancelScheduledAsync("sched_sent"));

        Assert.Equal("Message has already been sent", exception.Message);
    }

    [Fact]
    public async Task CancelScheduledAsync_With429Response_ThrowsRateLimitException()
    {
        // Arrange - Queue multiple 429 responses for all retry attempts
        for (int i = 0; i < 4; i++)
        {
            var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests)
            {
                Content = new StringContent(@"{""message"": ""Rate limit exceeded""}",
                    System.Text.Encoding.UTF8, "application/json")
            };
            response.Headers.Add("Retry-After", "1");
            _mockHandler.QueueResponse(response);
        }

        // Act & Assert
        var exception = await Assert.ThrowsAsync<RateLimitException>(
            () => _client.Messages.CancelScheduledAsync("sched_123"));

        Assert.Equal(TimeSpan.FromSeconds(1), exception.RetryAfter);
    }

    [Fact]
    public async Task CancelScheduledAsync_WithSpecialCharactersInId_EncodesCorrectly()
    {
        // Arrange
        var responseJson = @"{
            ""id"": ""sched/special+id"",
            ""status"": ""cancelled"",
            ""credits_refunded"": 1,
            ""cancelled_at"": ""2024-01-20T12:00:00Z""
        }";
        _mockHandler.QueueSuccessResponse(responseJson);

        // Act
        await _client.Messages.CancelScheduledAsync("sched/special+id");

        // Assert
        var request = _mockHandler.LastRequest;
        Assert.NotNull(request);
        Assert.Contains("sched%2Fspecial%2Bid", request.RequestUri?.ToString());
    }

    [Fact]
    public async Task CancelScheduledAsync_With500Response_RetriesAndThrows()
    {
        // Arrange
        for (int i = 0; i < 4; i++)
        {
            _mockHandler.QueueResponse(HttpStatusCode.InternalServerError,
                @"{""error"": ""Server error""}");
        }

        // Act & Assert
        await Assert.ThrowsAsync<SendlyException>(
            () => _client.Messages.CancelScheduledAsync("sched_123"));

        // Verify retries occurred
        Assert.Equal(4, _mockHandler.Requests.Count);
    }

    #endregion
}
