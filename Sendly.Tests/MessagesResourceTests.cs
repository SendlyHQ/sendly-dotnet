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

    private static string V1Message(string id, string createdAt) => $@"{{
        ""id"": ""{id}"",
        ""to"": ""+15551234567"",
        ""from"": ""+18335550100"",
        ""text"": ""Message {id}"",
        ""status"": ""delivered"",
        ""direction"": ""outbound"",
        ""error"": null,
        ""errorCode"": null,
        ""retryCount"": 0,
        ""segments"": 1,
        ""creditsUsed"": 2,
        ""isSandbox"": false,
        ""createdAt"": ""{createdAt}"",
        ""deliveredAt"": ""{createdAt}"",
        ""message_format"": ""sms"",
        ""messageFormat"": ""sms""
    }}";

    private static string V1Page(int total, int limit, int offset, bool hasMore, params string[] items) => $@"{{
        ""data"": [{string.Join(",", items)}],
        ""pagination"": {{
            ""total"": {total},
            ""limit"": {limit},
            ""offset"": {offset},
            ""page"": {offset / limit + 1},
            ""totalPages"": {(total + limit - 1) / limit},
            ""hasMore"": {(hasMore ? "true" : "false")}
        }},
        ""count"": {items.Length}
    }}";

    #region Wire shape

    [Fact]
    public async Task GetAsync_ReadsEveryFieldOfTheMessageTheApiSends()
    {
        _mockHandler.QueueSuccessResponse(@"{
            ""id"": ""msg_9"",
            ""to"": ""+15551234567"",
            ""from"": ""+18335550100"",
            ""text"": ""Your code is 1234"",
            ""status"": ""failed"",
            ""direction"": ""inbound"",
            ""error"": ""The recipient's carrier rejected the message."",
            ""errorCode"": ""30003"",
            ""retryCount"": 1,
            ""segments"": 2,
            ""creditsUsed"": 2,
            ""isSandbox"": true,
            ""createdAt"": ""2026-09-25T10:00:00.000Z"",
            ""deliveredAt"": ""2026-09-25T10:00:05.000Z"",
            ""message_format"": ""sms"",
            ""messageFormat"": ""sms""
        }");

        var message = await _client.Messages.GetAsync("msg_9");

        Assert.Equal(2, message.CreditsUsed);
        Assert.True(message.IsSandbox);
        Assert.Equal("30003", message.ErrorCode);
        Assert.Equal(1, message.RetryCount);
        Assert.Equal("The recipient's carrier rejected the message.", message.ErrorMessage);
        Assert.Equal(new DateTime(2026, 9, 25, 10, 0, 0, DateTimeKind.Utc), message.CreatedAt.ToUniversalTime());
        Assert.NotNull(message.DeliveredAt);
        Assert.Equal(2, message.Segments);
        Assert.Equal("inbound", message.Direction);
    }

    [Fact]
    public async Task SendAsync_ReadsTheLiveSendResponseTheApiSends()
    {
        _mockHandler.QueueResponse(HttpStatusCode.Created, @"{
            ""id"": ""msg_live"",
            ""to"": ""+15551234567"",
            ""from"": ""SENDLY"",
            ""text"": ""Hello"",
            ""status"": ""queued"",
            ""direction"": ""outbound"",
            ""error"": null,
            ""segments"": 1,
            ""creditsUsed"": 2,
            ""senderType"": ""number_pool"",
            ""createdAt"": ""2026-09-25T10:00:00.000Z"",
            ""metadata"": {},
            ""senderNote"": ""Message will be sent from a toll-free number in your number pool.""
        }");

        var message = await _client.Messages.SendAsync("+15551234567", "Hello");

        Assert.Equal(2, message.CreditsUsed);
        Assert.Equal(Message.SenderTypes.NumberPool, message.SenderType);
        Assert.Equal("Message will be sent from a toll-free number in your number pool.", message.SenderNote);
        Assert.NotEqual(default, message.CreatedAt);
        Assert.Null(message.Simulated);
    }

    [Fact]
    public async Task SendAsync_ReadsASimulatedSend()
    {
        _mockHandler.QueueResponse(HttpStatusCode.Created, @"{
            ""id"": ""msg_sim"",
            ""to"": ""+15551234567"",
            ""from"": ""SENDLY-TEST"",
            ""text"": ""Hello"",
            ""status"": ""delivered"",
            ""direction"": ""outbound"",
            ""error"": null,
            ""segments"": 1,
            ""creditsUsed"": 0,
            ""createdAt"": ""2026-09-25T10:00:00.000Z"",
            ""metadata"": {},
            ""simulated"": true,
            ""simulatedReason"": ""Verification pending or not approved"",
            ""actionUrl"": ""/verify""
        }");

        var message = await _client.Messages.SendAsync("+15551234567", "Hello");

        Assert.True(message.Simulated);
        Assert.Equal("Verification pending or not approved", message.SimulatedReason);
        Assert.Null(message.SenderType);
        Assert.Equal(0, message.CreditsUsed);
    }

    [Fact]
    public async Task GetAllAsync_FollowsPaginationHasMoreToTheNextPage()
    {
        var firstPage = Enumerable.Range(1, 100)
            .Select(i => V1Message($"msg_{i}", "2026-09-25T10:00:00.000Z"))
            .ToArray();
        _mockHandler.QueueSuccessResponse(V1Page(101, 100, 0, true, firstPage));
        _mockHandler.QueueSuccessResponse(V1Page(101, 100, 100, false, V1Message("msg_101", "2026-09-25T11:00:00.000Z")));

        var ids = new List<string>();
        await foreach (var message in _client.Messages.GetAllAsync())
        {
            ids.Add(message.Id);
        }

        Assert.Equal(101, ids.Count);
        Assert.Equal("msg_101", ids[^1]);
        Assert.Equal(2, _mockHandler.Requests.Count);
        Assert.Contains("offset=100", _mockHandler.Requests[1].RequestUri!.Query);
    }

    [Fact]
    public async Task GetAllAsync_WithLimitAboveTheApiMaximum_AdvancesByThePageItReceived()
    {
        var firstPage = Enumerable.Range(1, 100)
            .Select(i => V1Message($"msg_{i}", "2026-09-25T10:00:00.000Z"))
            .ToArray();
        _mockHandler.QueueSuccessResponse(V1Page(101, 100, 0, true, firstPage));
        _mockHandler.QueueSuccessResponse(V1Page(101, 100, 100, false, V1Message("msg_101", "2026-09-25T11:00:00.000Z")));

        var count = 0;
        await foreach (var _ in _client.Messages.GetAllAsync(new ListMessagesOptions { Limit = 500 }))
        {
            count++;
        }

        Assert.Equal(101, count);
        Assert.Contains("limit=100", _mockHandler.Requests[0].RequestUri!.Query);
        Assert.Contains("offset=100", _mockHandler.Requests[1].RequestUri!.Query);
    }

    [Fact]
    public async Task GetAllAsync_StopsOnAnEmptyPageEvenWhenHasMoreIsTrue()
    {
        _mockHandler.QueueSuccessResponse(V1Page(5, 50, 0, true));

        var count = 0;
        await foreach (var _ in _client.Messages.GetAllAsync())
        {
            count++;
        }

        Assert.Equal(0, count);
        Assert.Single(_mockHandler.Requests);
    }

    [Fact]
    public async Task ListAsync_ReadsPaginationHasMore()
    {
        _mockHandler.QueueSuccessResponse(V1Page(3, 2, 0, true,
            V1Message("msg_1", "2026-09-25T10:00:00.000Z"),
            V1Message("msg_2", "2026-09-25T10:01:00.000Z")));

        var page = await _client.Messages.ListAsync(new ListMessagesOptions { Limit = 2 });

        Assert.Equal(2, page.Count);
        Assert.Equal(3, page.Total);
        Assert.True(page.HasMore);
    }

    [Fact]
    public async Task SendGroupAsync_LiveSend_ReadsRecipientObjects()
    {
        _mockHandler.QueueResponse(HttpStatusCode.Created, @"{
            ""id"": ""msg_grp"",
            ""status"": ""sent"",
            ""to"": [
                {""phoneNumber"": ""+14155551234"", ""status"": ""queued""},
                {""phoneNumber"": ""+14155555678"", ""status"": ""sent""}
            ],
            ""group_message_id"": ""grp_1""
        }");

        var result = await _client.Messages.SendGroupAsync(new SendGroupMessageRequest
        {
            To = new List<string> { "+14155551234", "+14155555678" },
            Text = "Team sync at noon"
        });

        Assert.Single(_mockHandler.Requests);
        Assert.Equal("msg_grp", result.Id);
        Assert.Equal("grp_1", result.GroupMessageId);
        Assert.Equal(new List<string> { "+14155551234", "+14155555678" }, result.To);
        Assert.NotNull(result.Recipients);
        Assert.Equal(2, result.Recipients!.Count);
        Assert.Equal("+14155551234", result.Recipients[0].PhoneNumber);
        Assert.Equal("queued", result.Recipients[0].Status);
        Assert.Equal("+14155555678", result.Recipients[1].PhoneNumber);
        Assert.Equal("sent", result.Recipients[1].Status);
    }

    [Fact]
    public async Task SendGroupAsync_SimulatedSend_ReadsPhoneNumberStrings()
    {
        _mockHandler.QueueResponse(HttpStatusCode.Created, @"{
            ""id"": ""msg_sim"",
            ""status"": ""delivered"",
            ""to"": [""+14155551234"", ""+14155555678""],
            ""simulated"": true,
            ""message"": ""Group message simulated (test key or verification pending).""
        }");

        var result = await _client.Messages.SendGroupAsync(new SendGroupMessageRequest
        {
            To = new List<string> { "+14155551234", "+14155555678" },
            Text = "Team sync at noon"
        });

        Assert.Equal(new List<string> { "+14155551234", "+14155555678" }, result.To);
        Assert.Null(result.Recipients);
        Assert.True(result.Simulated);
    }

    #endregion

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
            ""creditsUsed"": 1,
            ""createdAt"": ""2024-01-20T10:00:00Z"",
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
                ""creditsUsed"": 1,
                ""createdAt"": ""2024-01-20T10:00:00Z"",
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
    [InlineData("invalid")]
    [InlineData("")]
    public async Task SendAsync_WithInvalidPhoneNumber_ThrowsValidationException(string invalidPhone)
    {
        // Act & Assert - Client-side validation should throw immediately
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _client.Messages.SendAsync(invalidPhone, "Test message"));

        Assert.Contains("Invalid phone number format", exception.Message);
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
            ""creditsUsed"": 10,
            ""createdAt"": ""2024-01-20T10:00:00Z"",
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
            () => _client.Messages.SendAsync("+15551234567", "Test"));

        Assert.Equal("Rate limit exceeded", exception.Message);
        Assert.Equal(429, exception.StatusCode);
        Assert.NotNull(exception.RetryAfter);
        Assert.Equal(TimeSpan.FromSeconds(1), exception.RetryAfter);
    }

    [Fact]
    public async Task SendAsync_With429ResponseNoRetryAfter_ThrowsRateLimitException()
    {
        // Arrange - Queue multiple 429 responses for all retry attempts
        for (int i = 0; i < 4; i++)
        {
            _mockHandler.QueueResponse(HttpStatusCode.TooManyRequests,
                @"{""message"": ""Rate limit exceeded""}");
        }

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
            ""creditsUsed"": 1,
            ""createdAt"": ""2024-01-20T10:00:00Z"",
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
                    ""creditsUsed"": 1,
                    ""createdAt"": ""2024-01-20T10:00:00Z"",
                    ""updated_at"": ""2024-01-20T10:00:00Z""
                },
                {
                    ""id"": ""msg_2"",
                    ""to"": ""+15559876543"",
                    ""text"": ""Message 2"",
                    ""status"": ""sent"",
                    ""creditsUsed"": 1,
                    ""createdAt"": ""2024-01-20T11:00:00Z"",
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
                    ""creditsUsed"": 1,
                    ""createdAt"": ""2024-01-20T10:00:00Z"",
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
                ""creditsUsed"": 1,
                ""createdAt"": ""2024-01-20T10:00:00Z"",
                ""updated_at"": ""2024-01-20T10:00:00Z"",
                ""deliveredAt"": ""2024-01-20T10:05:00Z""
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
            ""creditsUsed"": 0,
            ""errorCode"": ""INVALID_NUMBER"",
            ""error"": ""The phone number is invalid"",
            ""createdAt"": ""2024-01-20T10:00:00Z"",
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
                    ""creditsUsed"": 1,
                    ""createdAt"": ""2024-01-20T10:00:00Z"",
                    ""updated_at"": ""2024-01-20T10:00:00Z""
                },
                {
                    ""id"": ""msg_2"",
                    ""to"": ""+15559876543"",
                    ""text"": ""Message 2"",
                    ""status"": ""sent"",
                    ""creditsUsed"": 1,
                    ""createdAt"": ""2024-01-20T11:00:00Z"",
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
                    ""creditsUsed"": 1,
                    ""createdAt"": ""2024-01-20T12:00:00Z"",
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
                    ""creditsUsed"": 1,
                    ""createdAt"": ""2024-01-20T10:00:00Z"",
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
                    ""creditsUsed"": 1,
                    ""createdAt"": ""2024-01-20T10:00:00Z"",
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
            ""creditsUsed"": 1,
            ""createdAt"": ""2024-01-20T10:00:00Z"",
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
            ""creditsUsed"": 0,
            ""createdAt"": ""2024-01-20T10:00:00Z"",
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
    [InlineData("sent")]
    public async Task Message_IsPending_ReturnsTrueForPendingStatuses(string status)
    {
        // Arrange
        var responseJson = $@"{{
            ""id"": ""msg_1"",
            ""to"": ""+15551234567"",
            ""text"": ""Test"",
            ""status"": ""{status}"",
            ""creditsUsed"": 1,
            ""createdAt"": ""2024-01-20T10:00:00Z"",
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
