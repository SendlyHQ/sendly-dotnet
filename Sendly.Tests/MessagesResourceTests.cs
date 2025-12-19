using System.Net;
using System.Reflection;
using Sendly.Exceptions;
using Sendly.Models;
using Sendly.Resources;
using Sendly.Tests.Fixtures;
using Xunit;

namespace Sendly.Tests;

/// <summary>
/// Tests for MessagesResource - Send, List, Get, and GetAll methods.
/// </summary>
public class MessagesResourceTests : IDisposable
{
    private readonly MockHttpMessageHandler _mockHandler;
    private readonly HttpClient _httpClient;
    private readonly SendlyClient _client;

    public MessagesResourceTests()
    {
        _mockHandler = new MockHttpMessageHandler();
        _httpClient = new HttpClient(_mockHandler)
        {
            BaseAddress = new Uri("https://api.test.com")
        };

        // Use reflection to inject the mock HttpClient
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

    #region SendAsync Tests

    [Fact]
    public async Task SendAsync_WithValidParameters_ReturnsMessage()
    {
        // Arrange
        var responseJson = @"{
            ""id"": ""msg_123"",
            ""to"": ""+15551234567"",
            ""text"": ""Hello World"",
            ""status"": ""queued"",
            ""credits_used"": 1,
            ""created_at"": ""2024-01-20T10:00:00Z"",
            ""updated_at"": ""2024-01-20T10:00:00Z""
        }";
        _mockHandler.QueueSuccessResponse(responseJson);

        // Act
        var message = await _client.Messages.SendAsync("+15551234567", "Hello World");

        // Assert
        Assert.NotNull(message);
        Assert.Equal("msg_123", message.Id);
        Assert.Equal("+15551234567", message.To);
        Assert.Equal("Hello World", message.Text);
        Assert.Equal("queued", message.Status);
        Assert.Equal(1, message.CreditsUsed);
    }

    [Fact]
    public async Task SendAsync_WithRequestObject_ReturnsMessage()
    {
        // Arrange
        var responseJson = @"{
            ""message"": {
                ""id"": ""msg_456"",
                ""to"": ""+15551234567"",
                ""text"": ""Test message"",
                ""status"": ""sent"",
                ""credits_used"": 1,
                ""created_at"": ""2024-01-20T10:00:00Z"",
                ""updated_at"": ""2024-01-20T10:00:00Z""
            }
        }";
        _mockHandler.QueueSuccessResponse(responseJson);

        var request = new SendMessageRequest("+15551234567", "Test message");

        // Act
        var message = await _client.Messages.SendAsync(request);

        // Assert
        Assert.NotNull(message);
        Assert.Equal("msg_456", message.Id);
        Assert.Equal("Test message", message.Text);
    }

    [Theory]
    [InlineData("1234567890")]
    [InlineData("15551234567")]
    [InlineData("+1555")]
    [InlineData("invalid")]
    [InlineData("")]
    public async Task SendAsync_WithInvalidPhoneNumber_ThrowsValidationException(string invalidPhone)
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _client.Messages.SendAsync(invalidPhone, "Test message"));

        Assert.Contains("Invalid phone number", exception.Message);
        Assert.Equal(400, exception.StatusCode);
    }

    [Fact]
    public async Task SendAsync_WithNullPhoneNumber_ThrowsValidationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _client.Messages.SendAsync(null!, "Test message"));
    }

    [Fact]
    public async Task SendAsync_WithEmptyText_ThrowsValidationException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _client.Messages.SendAsync("+15551234567", ""));

        Assert.Contains("Message text is required", exception.Message);
    }

    [Fact]
    public async Task SendAsync_WithNullText_ThrowsValidationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _client.Messages.SendAsync("+15551234567", null!));
    }

    [Fact]
    public async Task SendAsync_WithTooLongText_ThrowsValidationException()
    {
        // Arrange
        var longText = new string('a', 1601); // Max is 1600

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _client.Messages.SendAsync("+15551234567", longText));

        Assert.Contains("exceeds maximum length", exception.Message);
    }

    [Fact]
    public async Task SendAsync_WithMaxLengthText_Succeeds()
    {
        // Arrange
        var maxText = new string('a', 1600);
        var responseJson = @"{
            ""id"": ""msg_789"",
            ""to"": ""+15551234567"",
            ""text"": """ + maxText + @""",
            ""status"": ""queued"",
            ""credits_used"": 10,
            ""created_at"": ""2024-01-20T10:00:00Z"",
            ""updated_at"": ""2024-01-20T10:00:00Z""
        }";
        _mockHandler.QueueSuccessResponse(responseJson);

        // Act
        var message = await _client.Messages.SendAsync("+15551234567", maxText);

        // Assert
        Assert.NotNull(message);
    }

    [Fact]
    public async Task SendAsync_With401Response_ThrowsAuthenticationException()
    {
        // Arrange
        _mockHandler.QueueResponse(HttpStatusCode.Unauthorized, @"{""message"": ""Invalid API key""}");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AuthenticationException>(
            () => _client.Messages.SendAsync("+15551234567", "Test"));

        Assert.Equal("Invalid API key", exception.Message);
        Assert.Equal(401, exception.StatusCode);
    }

    [Fact]
    public async Task SendAsync_With402Response_ThrowsInsufficientCreditsException()
    {
        // Arrange
        _mockHandler.QueueResponse(HttpStatusCode.PaymentRequired,
            @"{""error"": ""Insufficient credits to send message""}");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InsufficientCreditsException>(
            () => _client.Messages.SendAsync("+15551234567", "Test"));

        Assert.Equal("Insufficient credits to send message", exception.Message);
        Assert.Equal(402, exception.StatusCode);
    }

    [Fact]
    public async Task SendAsync_With404Response_ThrowsNotFoundException()
    {
        // Arrange
        _mockHandler.QueueResponse(HttpStatusCode.NotFound,
            @"{""message"": ""Resource not found""}");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => _client.Messages.SendAsync("+15551234567", "Test"));

        Assert.Equal(404, exception.StatusCode);
    }

    [Fact]
    public async Task SendAsync_With429Response_ThrowsRateLimitException()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests)
        {
            Content = new StringContent(@"{""message"": ""Rate limit exceeded""}",
                System.Text.Encoding.UTF8, "application/json")
        };
        response.Headers.Add("Retry-After", "60");
        _mockHandler.QueueResponse(response);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<RateLimitException>(
            () => _client.Messages.SendAsync("+15551234567", "Test"));

        Assert.Equal("Rate limit exceeded", exception.Message);
        Assert.Equal(429, exception.StatusCode);
        Assert.NotNull(exception.RetryAfter);
        Assert.Equal(TimeSpan.FromSeconds(60), exception.RetryAfter);
    }

    [Fact]
    public async Task SendAsync_With429ResponseNoRetryAfter_ThrowsRateLimitException()
    {
        // Arrange
        _mockHandler.QueueResponse(HttpStatusCode.TooManyRequests,
            @"{""message"": ""Rate limit exceeded""}");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<RateLimitException>(
            () => _client.Messages.SendAsync("+15551234567", "Test"));

        Assert.Null(exception.RetryAfter);
    }

    [Fact]
    public async Task SendAsync_With500Response_RetriesAndEventuallyThrows()
    {
        // Arrange - Queue multiple 500 responses to simulate retries
        _mockHandler.QueueResponse(HttpStatusCode.InternalServerError, @"{""error"": ""Server error""}");
        _mockHandler.QueueResponse(HttpStatusCode.InternalServerError, @"{""error"": ""Server error""}");
        _mockHandler.QueueResponse(HttpStatusCode.InternalServerError, @"{""error"": ""Server error""}");
        _mockHandler.QueueResponse(HttpStatusCode.InternalServerError, @"{""error"": ""Server error""}");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<SendlyException>(
            () => _client.Messages.SendAsync("+15551234567", "Test"));

        Assert.Equal("Server error", exception.Message);
        Assert.Equal(500, exception.StatusCode);

        // Verify retries occurred (1 initial + 3 retries = 4 total)
        Assert.Equal(4, _mockHandler.Requests.Count);
    }

    [Theory]
    [InlineData("+15551234567")]
    [InlineData("+442071234567")]
    [InlineData("+61412345678")]
    [InlineData("+919876543210")]
    public async Task SendAsync_WithValidE164PhoneNumbers_Succeeds(string phoneNumber)
    {
        // Arrange
        var responseJson = $@"{{
            ""id"": ""msg_123"",
            ""to"": ""{phoneNumber}"",
            ""text"": ""Test"",
            ""status"": ""queued"",
            ""credits_used"": 1,
            ""created_at"": ""2024-01-20T10:00:00Z"",
            ""updated_at"": ""2024-01-20T10:00:00Z""
        }}";
        _mockHandler.QueueSuccessResponse(responseJson);

        // Act
        var message = await _client.Messages.SendAsync(phoneNumber, "Test");

        // Assert
        Assert.NotNull(message);
        Assert.Equal(phoneNumber, message.To);
    }

    #endregion

    #region ListAsync Tests

    [Fact]
    public async Task ListAsync_WithoutOptions_ReturnsMessageList()
    {
        // Arrange
        var responseJson = @"{
            ""data"": [
                {
                    ""id"": ""msg_1"",
                    ""to"": ""+15551234567"",
                    ""text"": ""Message 1"",
                    ""status"": ""delivered"",
                    ""credits_used"": 1,
                    ""created_at"": ""2024-01-20T10:00:00Z"",
                    ""updated_at"": ""2024-01-20T10:00:00Z""
                },
                {
                    ""id"": ""msg_2"",
                    ""to"": ""+15559876543"",
                    ""text"": ""Message 2"",
                    ""status"": ""sent"",
                    ""credits_used"": 1,
                    ""created_at"": ""2024-01-20T11:00:00Z"",
                    ""updated_at"": ""2024-01-20T11:00:00Z""
                }
            ],
            ""has_more"": false,
            ""total"": 2
        }";
        _mockHandler.QueueSuccessResponse(responseJson);

        // Act
        var result = await _client.Messages.ListAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.False(result.HasMore);
        Assert.Equal(2, result.Total);
    }

    [Fact]
    public async Task ListAsync_WithOptions_SendsCorrectQueryParameters()
    {
        // Arrange
        var responseJson = @"{""data"": [], ""has_more"": false, ""total"": 0}";
        _mockHandler.QueueSuccessResponse(responseJson);

        var options = new ListMessagesOptions
        {
            Limit = 50,
            Offset = 100,
            Status = "delivered",
            To = "+15551234567"
        };

        // Act
        await _client.Messages.ListAsync(options);

        // Assert
        var request = _mockHandler.LastRequest;
        Assert.NotNull(request);
        Assert.Contains("limit=50", request.RequestUri?.Query);
        Assert.Contains("offset=100", request.RequestUri?.Query);
        Assert.Contains("status=delivered", request.RequestUri?.Query);
        Assert.Contains("to=%2B15551234567", request.RequestUri?.Query);
    }

    [Fact]
    public async Task ListAsync_WithPagination_ReturnsCorrectData()
    {
        // Arrange
        var responseJson = @"{
            ""data"": [
                {
                    ""id"": ""msg_101"",
                    ""to"": ""+15551234567"",
                    ""text"": ""Page 1"",
                    ""status"": ""delivered"",
                    ""credits_used"": 1,
                    ""created_at"": ""2024-01-20T10:00:00Z"",
                    ""updated_at"": ""2024-01-20T10:00:00Z""
                }
            ],
            ""has_more"": true,
            ""total"": 200
        }";
        _mockHandler.QueueSuccessResponse(responseJson);

        // Act
        var result = await _client.Messages.ListAsync(new ListMessagesOptions { Limit = 1 });

        // Assert
        Assert.True(result.HasMore);
        Assert.Equal(200, result.Total);
        Assert.Single(result);
    }

    [Fact]
    public async Task ListAsync_With401Response_ThrowsAuthenticationException()
    {
        // Arrange
        _mockHandler.QueueResponse(HttpStatusCode.Unauthorized,
            @"{""message"": ""Invalid API key""}");

        // Act & Assert
        await Assert.ThrowsAsync<AuthenticationException>(
            () => _client.Messages.ListAsync());
    }

    [Fact]
    public async Task ListAsync_With500Response_ThrowsSendlyException()
    {
        // Arrange - Queue responses for retries
        for (int i = 0; i < 4; i++)
        {
            _mockHandler.QueueResponse(HttpStatusCode.InternalServerError,
                @"{""error"": ""Server error""}");
        }

        // Act & Assert
        await Assert.ThrowsAsync<SendlyException>(
            () => _client.Messages.ListAsync());
    }

    #endregion

    #region GetAsync Tests

    [Fact]
    public async Task GetAsync_WithValidId_ReturnsMessage()
    {
        // Arrange
        var responseJson = @"{
            ""data"": {
                ""id"": ""msg_xyz"",
                ""to"": ""+15551234567"",
                ""text"": ""Retrieved message"",
                ""status"": ""delivered"",
                ""credits_used"": 1,
                ""created_at"": ""2024-01-20T10:00:00Z"",
                ""updated_at"": ""2024-01-20T10:00:00Z"",
                ""delivered_at"": ""2024-01-20T10:05:00Z""
            }
        }";
        _mockHandler.QueueSuccessResponse(responseJson);

        // Act
        var message = await _client.Messages.GetAsync("msg_xyz");

        // Assert
        Assert.NotNull(message);
        Assert.Equal("msg_xyz", message.Id);
        Assert.Equal("Retrieved message", message.Text);
        Assert.Equal("delivered", message.Status);
        Assert.NotNull(message.DeliveredAt);
    }

    [Fact]
    public async Task GetAsync_WithEmptyId_ThrowsValidationException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _client.Messages.GetAsync(""));

        Assert.Contains("Message ID is required", exception.Message);
    }

    [Fact]
    public async Task GetAsync_WithNullId_ThrowsValidationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _client.Messages.GetAsync(null!));
    }

    [Fact]
    public async Task GetAsync_With404Response_ThrowsNotFoundException()
    {
        // Arrange
        _mockHandler.QueueResponse(HttpStatusCode.NotFound,
            @"{""message"": ""Message not found""}");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => _client.Messages.GetAsync("msg_nonexistent"));

        Assert.Equal("Message not found", exception.Message);
        Assert.Equal(404, exception.StatusCode);
    }

    [Fact]
    public async Task GetAsync_With401Response_ThrowsAuthenticationException()
    {
        // Arrange
        _mockHandler.QueueResponse(HttpStatusCode.Unauthorized,
            @"{""message"": ""Invalid API key""}");

        // Act & Assert
        await Assert.ThrowsAsync<AuthenticationException>(
            () => _client.Messages.GetAsync("msg_123"));
    }

    [Fact]
    public async Task GetAsync_WithFailedMessage_IncludesErrorDetails()
    {
        // Arrange
        var responseJson = @"{
            ""id"": ""msg_failed"",
            ""to"": ""+15551234567"",
            ""text"": ""Failed message"",
            ""status"": ""failed"",
            ""credits_used"": 0,
            ""error_code"": ""INVALID_NUMBER"",
            ""error_message"": ""The phone number is invalid"",
            ""created_at"": ""2024-01-20T10:00:00Z"",
            ""updated_at"": ""2024-01-20T10:00:00Z""
        }";
        _mockHandler.QueueSuccessResponse(responseJson);

        // Act
        var message = await _client.Messages.GetAsync("msg_failed");

        // Assert
        Assert.Equal("failed", message.Status);
        Assert.Equal("INVALID_NUMBER", message.ErrorCode);
        Assert.Equal("The phone number is invalid", message.ErrorMessage);
        Assert.True(message.IsFailed);
        Assert.False(message.IsDelivered);
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_WithoutOptions_IteratesAllMessages()
    {
        // Arrange - Set up two pages
        var page1Json = @"{
            ""data"": [
                {
                    ""id"": ""msg_1"",
                    ""to"": ""+15551234567"",
                    ""text"": ""Message 1"",
                    ""status"": ""delivered"",
                    ""credits_used"": 1,
                    ""created_at"": ""2024-01-20T10:00:00Z"",
                    ""updated_at"": ""2024-01-20T10:00:00Z""
                },
                {
                    ""id"": ""msg_2"",
                    ""to"": ""+15559876543"",
                    ""text"": ""Message 2"",
                    ""status"": ""sent"",
                    ""credits_used"": 1,
                    ""created_at"": ""2024-01-20T11:00:00Z"",
                    ""updated_at"": ""2024-01-20T11:00:00Z""
                }
            ],
            ""has_more"": true,
            ""total"": 3
        }";

        var page2Json = @"{
            ""data"": [
                {
                    ""id"": ""msg_3"",
                    ""to"": ""+15552223333"",
                    ""text"": ""Message 3"",
                    ""status"": ""delivered"",
                    ""credits_used"": 1,
                    ""created_at"": ""2024-01-20T12:00:00Z"",
                    ""updated_at"": ""2024-01-20T12:00:00Z""
                }
            ],
            ""has_more"": false,
            ""total"": 3
        }";

        _mockHandler.QueueSuccessResponse(page1Json);
        _mockHandler.QueueSuccessResponse(page2Json);

        // Act
        var messages = new List<Message>();
        await foreach (var message in _client.Messages.GetAllAsync())
        {
            messages.Add(message);
        }

        // Assert
        Assert.Equal(3, messages.Count);
        Assert.Equal("msg_1", messages[0].Id);
        Assert.Equal("msg_2", messages[1].Id);
        Assert.Equal("msg_3", messages[2].Id);

        // Verify pagination occurred
        Assert.Equal(2, _mockHandler.Requests.Count);
    }

    [Fact]
    public async Task GetAllAsync_WithOptions_UsesProvidedOptions()
    {
        // Arrange
        var responseJson = @"{
            ""data"": [
                {
                    ""id"": ""msg_1"",
                    ""to"": ""+15551234567"",
                    ""text"": ""Message 1"",
                    ""status"": ""delivered"",
                    ""credits_used"": 1,
                    ""created_at"": ""2024-01-20T10:00:00Z"",
                    ""updated_at"": ""2024-01-20T10:00:00Z""
                }
            ],
            ""has_more"": false,
            ""total"": 1
        }";
        _mockHandler.QueueSuccessResponse(responseJson);

        var options = new ListMessagesOptions
        {
            Status = "delivered",
            Limit = 10
        };

        // Act
        var messages = new List<Message>();
        await foreach (var message in _client.Messages.GetAllAsync(options))
        {
            messages.Add(message);
        }

        // Assert
        Assert.Single(messages);
        var request = _mockHandler.LastRequest;
        Assert.Contains("status=delivered", request?.RequestUri?.Query);
    }

    [Fact]
    public async Task GetAllAsync_WithEmptyResult_ReturnsEmpty()
    {
        // Arrange
        var responseJson = @"{""data"": [], ""has_more"": false, ""total"": 0}";
        _mockHandler.QueueSuccessResponse(responseJson);

        // Act
        var messages = new List<Message>();
        await foreach (var message in _client.Messages.GetAllAsync())
        {
            messages.Add(message);
        }

        // Assert
        Assert.Empty(messages);
    }

    [Fact]
    public async Task GetAllAsync_WithCancellation_StopsIteration()
    {
        // Arrange
        var responseJson = @"{
            ""data"": [
                {
                    ""id"": ""msg_1"",
                    ""to"": ""+15551234567"",
                    ""text"": ""Message 1"",
                    ""status"": ""delivered"",
                    ""credits_used"": 1,
                    ""created_at"": ""2024-01-20T10:00:00Z"",
                    ""updated_at"": ""2024-01-20T10:00:00Z""
                }
            ],
            ""has_more"": true,
            ""total"": 100
        }";
        _mockHandler.QueueSuccessResponse(responseJson);

        var cts = new CancellationTokenSource();

        // Act
        var messages = new List<Message>();
        await foreach (var message in _client.Messages.GetAllAsync(cancellationToken: cts.Token))
        {
            messages.Add(message);
            cts.Cancel(); // Cancel after first message
            break;
        }

        // Assert
        Assert.Single(messages);
    }

    #endregion

    #region Message Model Property Tests

    [Fact]
    public async Task Message_IsDelivered_ReturnsTrueForDeliveredStatus()
    {
        // Arrange
        var responseJson = @"{
            ""id"": ""msg_1"",
            ""to"": ""+15551234567"",
            ""text"": ""Test"",
            ""status"": ""delivered"",
            ""credits_used"": 1,
            ""created_at"": ""2024-01-20T10:00:00Z"",
            ""updated_at"": ""2024-01-20T10:00:00Z""
        }";
        _mockHandler.QueueSuccessResponse(responseJson);

        // Act
        var message = await _client.Messages.SendAsync("+15551234567", "Test");

        // Assert
        Assert.True(message.IsDelivered);
        Assert.False(message.IsFailed);
        Assert.False(message.IsPending);
    }

    [Fact]
    public async Task Message_IsFailed_ReturnsTrueForFailedStatus()
    {
        // Arrange
        var responseJson = @"{
            ""id"": ""msg_1"",
            ""to"": ""+15551234567"",
            ""text"": ""Test"",
            ""status"": ""failed"",
            ""credits_used"": 0,
            ""created_at"": ""2024-01-20T10:00:00Z"",
            ""updated_at"": ""2024-01-20T10:00:00Z""
        }";
        _mockHandler.QueueSuccessResponse(responseJson);

        // Act
        var message = await _client.Messages.SendAsync("+15551234567", "Test");

        // Assert
        Assert.True(message.IsFailed);
        Assert.False(message.IsDelivered);
        Assert.False(message.IsPending);
    }

    [Theory]
    [InlineData("queued")]
    [InlineData("sending")]
    [InlineData("sent")]
    public async Task Message_IsPending_ReturnsTrueForPendingStatuses(string status)
    {
        // Arrange
        var responseJson = $@"{{
            ""id"": ""msg_1"",
            ""to"": ""+15551234567"",
            ""text"": ""Test"",
            ""status"": ""{status}"",
            ""credits_used"": 1,
            ""created_at"": ""2024-01-20T10:00:00Z"",
            ""updated_at"": ""2024-01-20T10:00:00Z""
        }}";
        _mockHandler.QueueSuccessResponse(responseJson);

        // Act
        var message = await _client.Messages.SendAsync("+15551234567", "Test");

        // Assert
        Assert.True(message.IsPending);
        Assert.False(message.IsDelivered);
        Assert.False(message.IsFailed);
    }

    #endregion
}
