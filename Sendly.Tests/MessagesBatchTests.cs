using System.Net;
using System.Reflection;
using Sendly.Exceptions;
using Sendly.Models;
using Sendly.Tests.Fixtures;
using Xunit;

namespace Sendly.Tests;

/// <summary>
/// Tests for batch message operations.
/// </summary>
public class MessagesBatchTests : IDisposable
{
    private readonly MockHttpMessageHandler _mockHandler;
    private readonly HttpClient _httpClient;
    private readonly SendlyClient _client;

    public MessagesBatchTests()
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

    #region SendBatchAsync Tests

    [Fact]
    public async Task SendBatchAsync_WithValidMessages_ReturnsBatchResponse()
    {
        // Arrange
        var responseJson = @"{
            ""batch_id"": ""batch_123"",
            ""total"": 2,
            ""queued"": 2,
            ""failed"": 0,
            ""credits_used"": 2,
            ""status"": ""completed"",
            ""messages"": [
                {
                    ""message_id"": ""msg_1"",
                    ""to"": ""+15551234567"",
                    ""status"": ""queued"",
                    ""credits_used"": 1,
                    ""success"": true
                },
                {
                    ""message_id"": ""msg_2"",
                    ""to"": ""+15559876543"",
                    ""status"": ""queued"",
                    ""credits_used"": 1,
                    ""success"": true
                }
            ],
            ""created_at"": ""2024-01-20T10:00:00Z""
        }";
        _mockHandler.QueueSuccessResponse(responseJson);

        var request = new SendBatchRequest
        {
            Messages = new List<BatchMessageItem>
            {
                new BatchMessageItem("+15551234567", "Message 1"),
                new BatchMessageItem("+15559876543", "Message 2")
            }
        };

        // Act
        var response = await _client.Messages.SendBatchAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.Equal("batch_123", response.BatchId);
        Assert.Equal(2, response.Total);
        Assert.Equal(2, response.Queued);
        Assert.Equal(0, response.Failed);
        Assert.Equal(2, response.CreditsUsed);
        Assert.Equal("completed", response.Status);
        Assert.Equal(2, response.Messages.Count);
    }

    [Fact]
    public async Task SendBatchAsync_WithPartialFailures_ReturnsCorrectCounts()
    {
        // Arrange
        var responseJson = @"{
            ""batch_id"": ""batch_456"",
            ""total"": 3,
            ""queued"": 2,
            ""failed"": 1,
            ""credits_used"": 2,
            ""status"": ""completed"",
            ""messages"": [
                {
                    ""message_id"": ""msg_1"",
                    ""to"": ""+15551234567"",
                    ""status"": ""queued"",
                    ""credits_used"": 1,
                    ""success"": true
                },
                {
                    ""to"": ""+15559999999"",
                    ""status"": ""failed"",
                    ""credits_used"": 0,
                    ""success"": false,
                    ""error"": ""Invalid phone number"",
                    ""error_code"": ""INVALID_NUMBER""
                },
                {
                    ""message_id"": ""msg_3"",
                    ""to"": ""+15558888888"",
                    ""status"": ""queued"",
                    ""credits_used"": 1,
                    ""success"": true
                }
            ],
            ""created_at"": ""2024-01-20T10:00:00Z""
        }";
        _mockHandler.QueueSuccessResponse(responseJson);

        var request = new SendBatchRequest
        {
            Messages = new List<BatchMessageItem>
            {
                new BatchMessageItem("+15551234567", "Message 1"),
                new BatchMessageItem("+15559999999", "Message 2"),
                new BatchMessageItem("+15558888888", "Message 3")
            }
        };

        // Act
        var response = await _client.Messages.SendBatchAsync(request);

        // Assert
        Assert.Equal(3, response.Total);
        Assert.Equal(2, response.Queued);
        Assert.Equal(1, response.Failed);
        Assert.Equal(2, response.CreditsUsed);

        // Verify failed result has error details
        var failedResult = response.Messages[1];
        Assert.True(failedResult.IsFailed);
        Assert.Equal("Invalid phone number", failedResult.Error);
    }

    [Fact]
    public async Task SendBatchAsync_WithEmptyMessageList_ThrowsValidationException()
    {
        // Arrange
        var request = new SendBatchRequest
        {
            Messages = new List<BatchMessageItem>()
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _client.Messages.SendBatchAsync(request));

        Assert.Contains("At least one message is required", exception.Message);
    }

    [Fact]
    public async Task SendBatchAsync_WithNullMessageList_ThrowsValidationException()
    {
        // Arrange
        var request = new SendBatchRequest
        {
            Messages = null!
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _client.Messages.SendBatchAsync(request));
    }

    [Fact]
    public async Task SendBatchAsync_WithInvalidPhoneInBatch_ThrowsValidationException()
    {
        // Arrange
        var request = new SendBatchRequest
        {
            Messages = new List<BatchMessageItem>
            {
                new BatchMessageItem("+15551234567", "Valid"),
                new BatchMessageItem("invalid", "Invalid phone")
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _client.Messages.SendBatchAsync(request));
    }

    [Fact]
    public async Task SendBatchAsync_WithEmptyTextInBatch_ThrowsValidationException()
    {
        // Arrange
        var request = new SendBatchRequest
        {
            Messages = new List<BatchMessageItem>
            {
                new BatchMessageItem("+15551234567", "Valid"),
                new BatchMessageItem("+15559876543", "")
            }
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _client.Messages.SendBatchAsync(request));

        Assert.Contains("Message text is required", exception.Message);
    }

    [Fact]
    public async Task SendBatchAsync_WithTooLongTextInBatch_ThrowsValidationException()
    {
        // Arrange
        var longText = new string('a', 1601);
        var request = new SendBatchRequest
        {
            Messages = new List<BatchMessageItem>
            {
                new BatchMessageItem("+15551234567", longText)
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _client.Messages.SendBatchAsync(request));
    }

    [Fact]
    public async Task SendBatchAsync_WithSingleMessage_Succeeds()
    {
        // Arrange
        var responseJson = @"{
            ""batch_id"": ""batch_single"",
            ""total"": 1,
            ""queued"": 1,
            ""failed"": 0,
            ""credits_used"": 1,
            ""status"": ""completed"",
            ""messages"": [
                {
                    ""message_id"": ""msg_1"",
                    ""to"": ""+15551234567"",
                    ""status"": ""queued"",
                    ""credits_used"": 1,
                    ""success"": true
                }
            ],
            ""created_at"": ""2024-01-20T10:00:00Z""
        }";
        _mockHandler.QueueSuccessResponse(responseJson);

        var request = new SendBatchRequest
        {
            Messages = new List<BatchMessageItem>
            {
                new BatchMessageItem("+15551234567", "Single message")
            }
        };

        // Act
        var response = await _client.Messages.SendBatchAsync(request);

        // Assert
        Assert.Equal(1, response.Total);
        Assert.Single(response.Messages);
    }

    [Fact]
    public async Task SendBatchAsync_WithLargeBatch_Succeeds()
    {
        // Arrange
        var messages = new List<BatchMessageItem>();
        var results = new System.Text.StringBuilder("[");

        for (int i = 0; i < 100; i++)
        {
            messages.Add(new BatchMessageItem($"+1555123{i:D4}", $"Message {i}"));

            if (i > 0) results.Append(",");
            results.Append($@"{{
                ""message_id"": ""msg_{i}"",
                ""to"": ""+1555123{i:D4}"",
                ""status"": ""queued"",
                ""credits_used"": 1,
                ""success"": true
            }}");
        }
        results.Append("]");

        var responseJson = $@"{{
            ""batch_id"": ""batch_large"",
            ""total"": 100,
            ""queued"": 100,
            ""failed"": 0,
            ""credits_used"": 100,
            ""status"": ""completed"",
            ""messages"": {results},
            ""created_at"": ""2024-01-20T10:00:00Z""
        }}";
        _mockHandler.QueueSuccessResponse(responseJson);

        var request = new SendBatchRequest { Messages = messages };

        // Act
        var response = await _client.Messages.SendBatchAsync(request);

        // Assert
        Assert.Equal(100, response.Total);
        Assert.Equal(100, response.Messages.Count);
    }

    [Fact]
    public async Task SendBatchAsync_With401Response_ThrowsAuthenticationException()
    {
        // Arrange
        _mockHandler.QueueResponse(HttpStatusCode.Unauthorized,
            @"{""message"": ""Invalid API key""}");

        var request = new SendBatchRequest
        {
            Messages = new List<BatchMessageItem>
            {
                new BatchMessageItem("+15551234567", "Test")
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<AuthenticationException>(
            () => _client.Messages.SendBatchAsync(request));
    }

    [Fact]
    public async Task SendBatchAsync_With402Response_ThrowsInsufficientCreditsException()
    {
        // Arrange
        _mockHandler.QueueResponse(HttpStatusCode.PaymentRequired,
            @"{""error"": ""Insufficient credits for batch""}");

        var request = new SendBatchRequest
        {
            Messages = new List<BatchMessageItem>
            {
                new BatchMessageItem("+15551234567", "Test")
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<InsufficientCreditsException>(
            () => _client.Messages.SendBatchAsync(request));
    }

    [Fact]
    public async Task SendBatchAsync_With429Response_ThrowsRateLimitException()
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

        var request = new SendBatchRequest
        {
            Messages = new List<BatchMessageItem>
            {
                new BatchMessageItem("+15551234567", "Test")
            }
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<RateLimitException>(
            () => _client.Messages.SendBatchAsync(request));

        Assert.Equal(TimeSpan.FromSeconds(1), exception.RetryAfter);
    }

    [Fact]
    public async Task SendBatchAsync_With500Response_RetriesAndThrows()
    {
        // Arrange
        for (int i = 0; i < 4; i++)
        {
            _mockHandler.QueueResponse(HttpStatusCode.InternalServerError,
                @"{""error"": ""Server error""}");
        }

        var request = new SendBatchRequest
        {
            Messages = new List<BatchMessageItem>
            {
                new BatchMessageItem("+15551234567", "Test")
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<SendlyException>(
            () => _client.Messages.SendBatchAsync(request));

        Assert.Equal(4, _mockHandler.Requests.Count);
    }

    #endregion

    #region GetBatchAsync Tests

    [Fact]
    public async Task GetBatchAsync_WithValidId_ReturnsBatchResponse()
    {
        // Arrange
        var responseJson = @"{
            ""batch_id"": ""batch_get_123"",
            ""total"": 2,
            ""queued"": 2,
            ""failed"": 0,
            ""credits_used"": 2,
            ""status"": ""completed"",
            ""messages"": [
                {
                    ""message_id"": ""msg_1"",
                    ""to"": ""+15551234567"",
                    ""status"": ""delivered"",
                    ""credits_used"": 1,
                    ""success"": true
                },
                {
                    ""message_id"": ""msg_2"",
                    ""to"": ""+15559876543"",
                    ""status"": ""delivered"",
                    ""credits_used"": 1,
                    ""success"": true
                }
            ],
            ""created_at"": ""2024-01-20T10:00:00Z"",
            ""completed_at"": ""2024-01-20T10:05:00Z""
        }";
        _mockHandler.QueueSuccessResponse(responseJson);

        // Act
        var response = await _client.Messages.GetBatchAsync("batch_get_123");

        // Assert
        Assert.NotNull(response);
        Assert.Equal("batch_get_123", response.BatchId);
        Assert.Equal("completed", response.Status);
        Assert.Equal(2, response.Messages.Count);
    }

    [Fact]
    public async Task GetBatchAsync_WithEmptyId_ThrowsValidationException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _client.Messages.GetBatchAsync(""));

        Assert.Contains("Batch ID is required", exception.Message);
    }

    [Fact]
    public async Task GetBatchAsync_WithNullId_ThrowsValidationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _client.Messages.GetBatchAsync(null!));
    }

    [Fact]
    public async Task GetBatchAsync_With404Response_ThrowsNotFoundException()
    {
        // Arrange
        _mockHandler.QueueResponse(HttpStatusCode.NotFound,
            @"{""message"": ""Batch not found""}");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => _client.Messages.GetBatchAsync("batch_nonexistent"));

        Assert.Equal("Batch not found", exception.Message);
        Assert.Equal(404, exception.StatusCode);
    }

    [Fact]
    public async Task GetBatchAsync_With401Response_ThrowsAuthenticationException()
    {
        // Arrange
        _mockHandler.QueueResponse(HttpStatusCode.Unauthorized,
            @"{""message"": ""Invalid API key""}");

        // Act & Assert
        await Assert.ThrowsAsync<AuthenticationException>(
            () => _client.Messages.GetBatchAsync("batch_123"));
    }

    [Fact]
    public async Task GetBatchAsync_WithSpecialCharactersInId_EncodesCorrectly()
    {
        // Arrange
        var responseJson = @"{
            ""batch_id"": ""batch/special+id"",
            ""total"": 1,
            ""queued"": 1,
            ""failed"": 0,
            ""credits_used"": 1,
            ""status"": ""completed"",
            ""messages"": [],
            ""created_at"": ""2024-01-20T10:00:00Z""
        }";
        _mockHandler.QueueSuccessResponse(responseJson);

        // Act
        await _client.Messages.GetBatchAsync("batch/special+id");

        // Assert
        var request = _mockHandler.LastRequest;
        Assert.NotNull(request);
        Assert.Contains("batch%2Fspecial%2Bid", request.RequestUri?.ToString());
    }

    [Fact]
    public async Task GetBatchAsync_WithPendingStatus_ReturnsInProgressBatch()
    {
        // Arrange
        var responseJson = @"{
            ""batch_id"": ""batch_pending"",
            ""total"": 100,
            ""queued"": 50,
            ""failed"": 0,
            ""credits_used"": 50,
            ""status"": ""processing"",
            ""messages"": [],
            ""created_at"": ""2024-01-20T10:00:00Z""
        }";
        _mockHandler.QueueSuccessResponse(responseJson);

        // Act
        var response = await _client.Messages.GetBatchAsync("batch_pending");

        // Assert
        Assert.Equal("processing", response.Status);
        Assert.Equal(50, response.Queued);
        Assert.Equal(100, response.Total);
    }

    #endregion

    #region ListBatchesAsync Tests

    [Fact]
    public async Task ListBatchesAsync_WithoutOptions_ReturnsBatchList()
    {
        // Arrange
        var responseJson = @"{
            ""data"": [
                {
                    ""batch_id"": ""batch_1"",
                    ""total"": 10,
                    ""queued"": 10,
                    ""failed"": 0,
                    ""credits_used"": 10,
                    ""status"": ""completed"",
                    ""created_at"": ""2024-01-20T10:00:00Z""
                },
                {
                    ""batch_id"": ""batch_2"",
                    ""total"": 5,
                    ""queued"": 5,
                    ""failed"": 0,
                    ""credits_used"": 5,
                    ""status"": ""completed"",
                    ""created_at"": ""2024-01-20T11:00:00Z""
                }
            ],
            ""has_more"": false,
            ""total"": 2
        }";
        _mockHandler.QueueSuccessResponse(responseJson);

        // Act
        var result = await _client.Messages.ListBatchesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.False(result.HasMore);
        Assert.Equal(2, result.Total);
    }

    [Fact]
    public async Task ListBatchesAsync_WithOptions_SendsCorrectQueryParameters()
    {
        // Arrange
        var responseJson = @"{""data"": [], ""has_more"": false, ""total"": 0}";
        _mockHandler.QueueSuccessResponse(responseJson);

        var options = new ListBatchesOptions
        {
            Limit = 20,
            Offset = 40,
            Status = "completed"
        };

        // Act
        await _client.Messages.ListBatchesAsync(options);

        // Assert
        var request = _mockHandler.LastRequest;
        Assert.NotNull(request);
        Assert.Contains("limit=20", request.RequestUri?.Query);
        Assert.Contains("offset=40", request.RequestUri?.Query);
        Assert.Contains("status=completed", request.RequestUri?.Query);
    }

    [Fact]
    public async Task ListBatchesAsync_WithPagination_ReturnsCorrectData()
    {
        // Arrange
        var responseJson = @"{
            ""data"": [
                {
                    ""batch_id"": ""batch_page1"",
                    ""total"": 10,
                    ""queued"": 10,
                    ""failed"": 0,
                    ""credits_used"": 10,
                    ""status"": ""completed"",
                    ""created_at"": ""2024-01-20T10:00:00Z""
                }
            ],
            ""has_more"": true,
            ""total"": 50
        }";
        _mockHandler.QueueSuccessResponse(responseJson);

        // Act
        var result = await _client.Messages.ListBatchesAsync(new ListBatchesOptions { Limit = 1 });

        // Assert
        Assert.True(result.HasMore);
        Assert.Equal(50, result.Total);
        Assert.Single(result);
    }

    [Fact]
    public async Task ListBatchesAsync_With401Response_ThrowsAuthenticationException()
    {
        // Arrange
        _mockHandler.QueueResponse(HttpStatusCode.Unauthorized,
            @"{""message"": ""Invalid API key""}");

        // Act & Assert
        await Assert.ThrowsAsync<AuthenticationException>(
            () => _client.Messages.ListBatchesAsync());
    }

    [Fact]
    public async Task ListBatchesAsync_With500Response_ThrowsSendlyException()
    {
        // Arrange
        for (int i = 0; i < 4; i++)
        {
            _mockHandler.QueueResponse(HttpStatusCode.InternalServerError,
                @"{""error"": ""Server error""}");
        }

        // Act & Assert
        await Assert.ThrowsAsync<SendlyException>(
            () => _client.Messages.ListBatchesAsync());
    }

    [Fact]
    public async Task ListBatchesAsync_WithEmptyResult_ReturnsEmptyList()
    {
        // Arrange
        var responseJson = @"{""data"": [], ""has_more"": false, ""total"": 0}";
        _mockHandler.QueueSuccessResponse(responseJson);

        // Act
        var result = await _client.Messages.ListBatchesAsync();

        // Assert
        Assert.Empty(result);
        Assert.False(result.HasMore);
        Assert.Equal(0, result.Total);
    }

    #endregion
}
