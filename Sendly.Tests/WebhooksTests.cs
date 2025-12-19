using Sendly;
using Xunit;

namespace Sendly.Tests;

/// <summary>
/// Tests for Webhook signature verification and event parsing.
/// </summary>
public class WebhooksTests
{
    private const string TestSecret = "test_webhook_secret_12345";

    #region VerifySignature Tests

    [Fact]
    public void VerifySignature_WithValidSignature_ReturnsTrue()
    {
        // Arrange
        var payload = @"{""id"":""evt_123"",""type"":""message.delivered""}";
        var signature = Webhooks.GenerateSignature(payload, TestSecret);

        // Act
        var isValid = Webhooks.VerifySignature(payload, signature, TestSecret);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void VerifySignature_WithInvalidSignature_ReturnsFalse()
    {
        // Arrange
        var payload = @"{""id"":""evt_123"",""type"":""message.delivered""}";
        var invalidSignature = "sha256=invalid_signature_hash";

        // Act
        var isValid = Webhooks.VerifySignature(payload, invalidSignature, TestSecret);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void VerifySignature_WithWrongSecret_ReturnsFalse()
    {
        // Arrange
        var payload = @"{""id"":""evt_123"",""type"":""message.delivered""}";
        var signature = Webhooks.GenerateSignature(payload, TestSecret);
        var wrongSecret = "wrong_secret";

        // Act
        var isValid = Webhooks.VerifySignature(payload, signature, wrongSecret);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void VerifySignature_WithModifiedPayload_ReturnsFalse()
    {
        // Arrange
        var originalPayload = @"{""id"":""evt_123"",""type"":""message.delivered""}";
        var signature = Webhooks.GenerateSignature(originalPayload, TestSecret);
        var modifiedPayload = @"{""id"":""evt_456"",""type"":""message.delivered""}";

        // Act
        var isValid = Webhooks.VerifySignature(modifiedPayload, signature, TestSecret);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void VerifySignature_WithEmptyPayload_ReturnsFalse()
    {
        // Arrange
        var signature = "sha256=somehash";

        // Act
        var isValid = Webhooks.VerifySignature("", signature, TestSecret);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void VerifySignature_WithNullPayload_ReturnsFalse()
    {
        // Arrange
        var signature = "sha256=somehash";

        // Act
        var isValid = Webhooks.VerifySignature(null!, signature, TestSecret);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void VerifySignature_WithEmptySignature_ReturnsFalse()
    {
        // Arrange
        var payload = @"{""id"":""evt_123""}";

        // Act
        var isValid = Webhooks.VerifySignature(payload, "", TestSecret);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void VerifySignature_WithNullSignature_ReturnsFalse()
    {
        // Arrange
        var payload = @"{""id"":""evt_123""}";

        // Act
        var isValid = Webhooks.VerifySignature(payload, null!, TestSecret);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void VerifySignature_WithEmptySecret_ReturnsFalse()
    {
        // Arrange
        var payload = @"{""id"":""evt_123""}";
        var signature = "sha256=somehash";

        // Act
        var isValid = Webhooks.VerifySignature(payload, signature, "");

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void VerifySignature_WithNullSecret_ReturnsFalse()
    {
        // Arrange
        var payload = @"{""id"":""evt_123""}";
        var signature = "sha256=somehash";

        // Act
        var isValid = Webhooks.VerifySignature(payload, signature, null!);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void VerifySignature_WithWhitespaceInPayload_MatchesExactly()
    {
        // Arrange
        var payloadWithSpaces = @"{""id"": ""evt_123"", ""type"": ""message.delivered""}";
        var payloadNoSpaces = @"{""id"":""evt_123"",""type"":""message.delivered""}";
        var signature = Webhooks.GenerateSignature(payloadWithSpaces, TestSecret);

        // Act
        var validForOriginal = Webhooks.VerifySignature(payloadWithSpaces, signature, TestSecret);
        var invalidForModified = Webhooks.VerifySignature(payloadNoSpaces, signature, TestSecret);

        // Assert
        Assert.True(validForOriginal);
        Assert.False(invalidForModified);
    }

    [Fact]
    public void VerifySignature_IsCaseSensitive()
    {
        // Arrange
        var payload = @"{""id"":""evt_123""}";
        var signature = Webhooks.GenerateSignature(payload, TestSecret);
        var upperSignature = signature.ToUpperInvariant();

        // Act
        var isValid = Webhooks.VerifySignature(payload, upperSignature, TestSecret);

        // Assert
        Assert.False(isValid);
    }

    #endregion

    #region ParseEvent Tests

    [Fact]
    public void ParseEvent_WithValidSignatureAndPayload_ReturnsWebhookEvent()
    {
        // Arrange
        var payload = @"{
            ""id"": ""evt_123"",
            ""type"": ""message.delivered"",
            ""data"": {
                ""message_id"": ""msg_456"",
                ""status"": ""delivered"",
                ""to"": ""+15551234567"",
                ""from"": ""Sendly"",
                ""segments"": 1,
                ""credits_used"": 1,
                ""delivered_at"": ""2024-01-20T10:05:00Z""
            },
            ""created_at"": ""2024-01-20T10:00:00Z"",
            ""api_version"": ""2024-01-01""
        }";
        var signature = Webhooks.GenerateSignature(payload, TestSecret);

        // Act
        var webhookEvent = Webhooks.ParseEvent(payload, signature, TestSecret);

        // Assert
        Assert.NotNull(webhookEvent);
        Assert.Equal("evt_123", webhookEvent.Id);
        Assert.Equal("message.delivered", webhookEvent.Type);
        Assert.Equal("msg_456", webhookEvent.Data.MessageId);
        Assert.Equal("delivered", webhookEvent.Data.Status);
        Assert.Equal("+15551234567", webhookEvent.Data.To);
        Assert.Equal("2024-01-20T10:00:00Z", webhookEvent.CreatedAt);
    }

    [Fact]
    public void ParseEvent_WithInvalidSignature_ThrowsWebhookSignatureException()
    {
        // Arrange
        var payload = @"{""id"":""evt_123"",""type"":""message.delivered""}";
        var invalidSignature = "sha256=invalid";

        // Act & Assert
        var exception = Assert.Throws<WebhookSignatureException>(
            () => Webhooks.ParseEvent(payload, invalidSignature, TestSecret));

        Assert.Equal("Invalid webhook signature", exception.Message);
    }

    [Fact]
    public void ParseEvent_WithMalformedJson_ThrowsWebhookSignatureException()
    {
        // Arrange
        var payload = @"{invalid json}";
        var signature = Webhooks.GenerateSignature(payload, TestSecret);

        // Act & Assert
        var exception = Assert.Throws<WebhookSignatureException>(
            () => Webhooks.ParseEvent(payload, signature, TestSecret));

        Assert.Contains("Failed to parse webhook payload", exception.Message);
    }

    [Fact]
    public void ParseEvent_WithMissingId_ThrowsWebhookSignatureException()
    {
        // Arrange
        var payload = @"{
            ""type"": ""message.delivered"",
            ""data"": {},
            ""created_at"": ""2024-01-20T10:00:00Z""
        }";
        var signature = Webhooks.GenerateSignature(payload, TestSecret);

        // Act & Assert
        Assert.Throws<WebhookSignatureException>(
            () => Webhooks.ParseEvent(payload, signature, TestSecret));
    }

    [Fact]
    public void ParseEvent_WithMissingType_ThrowsWebhookSignatureException()
    {
        // Arrange
        var payload = @"{
            ""id"": ""evt_123"",
            ""data"": {},
            ""created_at"": ""2024-01-20T10:00:00Z""
        }";
        var signature = Webhooks.GenerateSignature(payload, TestSecret);

        // Act & Assert
        Assert.Throws<WebhookSignatureException>(
            () => Webhooks.ParseEvent(payload, signature, TestSecret));
    }

    [Fact]
    public void ParseEvent_WithMissingCreatedAt_ThrowsWebhookSignatureException()
    {
        // Arrange
        var payload = @"{
            ""id"": ""evt_123"",
            ""type"": ""message.delivered"",
            ""data"": {}
        }";
        var signature = Webhooks.GenerateSignature(payload, TestSecret);

        // Act & Assert
        Assert.Throws<WebhookSignatureException>(
            () => Webhooks.ParseEvent(payload, signature, TestSecret));
    }

    [Fact]
    public void ParseEvent_MessageDeliveredEvent_ParsesCorrectly()
    {
        // Arrange
        var payload = @"{
            ""id"": ""evt_delivered"",
            ""type"": ""message.delivered"",
            ""data"": {
                ""message_id"": ""msg_123"",
                ""status"": ""delivered"",
                ""to"": ""+15551234567"",
                ""from"": ""Sendly"",
                ""delivered_at"": ""2024-01-20T10:05:00Z"",
                ""segments"": 2,
                ""credits_used"": 2
            },
            ""created_at"": ""2024-01-20T10:00:00Z""
        }";
        var signature = Webhooks.GenerateSignature(payload, TestSecret);

        // Act
        var webhookEvent = Webhooks.ParseEvent(payload, signature, TestSecret);

        // Assert
        Assert.Equal("message.delivered", webhookEvent.Type);
        Assert.Equal("delivered", webhookEvent.Data.Status);
        Assert.Equal("2024-01-20T10:05:00Z", webhookEvent.Data.DeliveredAt);
        Assert.Null(webhookEvent.Data.Error);
    }

    [Fact]
    public void ParseEvent_MessageFailedEvent_ParsesCorrectly()
    {
        // Arrange
        var payload = @"{
            ""id"": ""evt_failed"",
            ""type"": ""message.failed"",
            ""data"": {
                ""message_id"": ""msg_456"",
                ""status"": ""failed"",
                ""to"": ""+15551234567"",
                ""from"": ""Sendly"",
                ""error"": ""Invalid phone number"",
                ""error_code"": ""INVALID_NUMBER"",
                ""failed_at"": ""2024-01-20T10:05:00Z"",
                ""segments"": 1,
                ""credits_used"": 0
            },
            ""created_at"": ""2024-01-20T10:00:00Z""
        }";
        var signature = Webhooks.GenerateSignature(payload, TestSecret);

        // Act
        var webhookEvent = Webhooks.ParseEvent(payload, signature, TestSecret);

        // Assert
        Assert.Equal("message.failed", webhookEvent.Type);
        Assert.Equal("failed", webhookEvent.Data.Status);
        Assert.Equal("Invalid phone number", webhookEvent.Data.Error);
        Assert.Equal("INVALID_NUMBER", webhookEvent.Data.ErrorCode);
        Assert.Equal("2024-01-20T10:05:00Z", webhookEvent.Data.FailedAt);
    }

    [Fact]
    public void ParseEvent_WithApiVersion_ParsesApiVersion()
    {
        // Arrange
        var payload = @"{
            ""id"": ""evt_123"",
            ""type"": ""message.delivered"",
            ""data"": {
                ""message_id"": ""msg_123"",
                ""status"": ""delivered"",
                ""to"": ""+15551234567"",
                ""from"": ""Sendly"",
                ""segments"": 1,
                ""credits_used"": 1
            },
            ""created_at"": ""2024-01-20T10:00:00Z"",
            ""api_version"": ""2025-01-01""
        }";
        var signature = Webhooks.GenerateSignature(payload, TestSecret);

        // Act
        var webhookEvent = Webhooks.ParseEvent(payload, signature, TestSecret);

        // Assert
        Assert.Equal("2025-01-01", webhookEvent.ApiVersion);
    }

    [Fact]
    public void ParseEvent_WithoutApiVersion_UsesDefault()
    {
        // Arrange
        var payload = @"{
            ""id"": ""evt_123"",
            ""type"": ""message.delivered"",
            ""data"": {
                ""message_id"": ""msg_123"",
                ""status"": ""delivered"",
                ""to"": ""+15551234567"",
                ""from"": ""Sendly"",
                ""segments"": 1,
                ""credits_used"": 1
            },
            ""created_at"": ""2024-01-20T10:00:00Z""
        }";
        var signature = Webhooks.GenerateSignature(payload, TestSecret);

        // Act
        var webhookEvent = Webhooks.ParseEvent(payload, signature, TestSecret);

        // Assert
        Assert.Equal("2024-01-01", webhookEvent.ApiVersion);
    }

    #endregion

    #region GenerateSignature Tests

    [Fact]
    public void GenerateSignature_WithValidPayload_ReturnsSignature()
    {
        // Arrange
        var payload = @"{""test"":""data""}";

        // Act
        var signature = Webhooks.GenerateSignature(payload, TestSecret);

        // Assert
        Assert.NotNull(signature);
        Assert.StartsWith("sha256=", signature);
        Assert.Equal(71, signature.Length); // "sha256=" (7) + 64 hex chars
    }

    [Fact]
    public void GenerateSignature_WithSameInput_ReturnsSameSignature()
    {
        // Arrange
        var payload = @"{""test"":""data""}";

        // Act
        var signature1 = Webhooks.GenerateSignature(payload, TestSecret);
        var signature2 = Webhooks.GenerateSignature(payload, TestSecret);

        // Assert
        Assert.Equal(signature1, signature2);
    }

    [Fact]
    public void GenerateSignature_WithDifferentPayload_ReturnsDifferentSignature()
    {
        // Arrange
        var payload1 = @"{""test"":""data1""}";
        var payload2 = @"{""test"":""data2""}";

        // Act
        var signature1 = Webhooks.GenerateSignature(payload1, TestSecret);
        var signature2 = Webhooks.GenerateSignature(payload2, TestSecret);

        // Assert
        Assert.NotEqual(signature1, signature2);
    }

    [Fact]
    public void GenerateSignature_WithDifferentSecret_ReturnsDifferentSignature()
    {
        // Arrange
        var payload = @"{""test"":""data""}";
        var secret1 = "secret1";
        var secret2 = "secret2";

        // Act
        var signature1 = Webhooks.GenerateSignature(payload, secret1);
        var signature2 = Webhooks.GenerateSignature(payload, secret2);

        // Assert
        Assert.NotEqual(signature1, signature2);
    }

    [Fact]
    public void GenerateSignature_ReturnsLowercaseHex()
    {
        // Arrange
        var payload = @"{""test"":""data""}";

        // Act
        var signature = Webhooks.GenerateSignature(payload, TestSecret);
        var hexPart = signature.Substring(7); // Remove "sha256=" prefix

        // Assert
        Assert.Equal(hexPart.ToLowerInvariant(), hexPart);
    }

    #endregion

    #region WebhookEvent Model Tests

    [Fact]
    public void WebhookEvent_DefaultConstructor_InitializesProperties()
    {
        // Arrange & Act
        var webhookEvent = new WebhookEvent();

        // Assert
        Assert.NotNull(webhookEvent.Id);
        Assert.NotNull(webhookEvent.Type);
        Assert.NotNull(webhookEvent.Data);
        Assert.NotNull(webhookEvent.CreatedAt);
        Assert.NotNull(webhookEvent.ApiVersion);
    }

    [Fact]
    public void WebhookEvent_CanSetProperties()
    {
        // Arrange & Act
        var webhookEvent = new WebhookEvent
        {
            Id = "evt_test",
            Type = "test.event",
            CreatedAt = "2024-01-20T10:00:00Z",
            ApiVersion = "2024-01-01"
        };

        // Assert
        Assert.Equal("evt_test", webhookEvent.Id);
        Assert.Equal("test.event", webhookEvent.Type);
        Assert.Equal("2024-01-20T10:00:00Z", webhookEvent.CreatedAt);
        Assert.Equal("2024-01-01", webhookEvent.ApiVersion);
    }

    #endregion

    #region WebhookMessageData Model Tests

    [Fact]
    public void WebhookMessageData_DefaultConstructor_InitializesProperties()
    {
        // Arrange & Act
        var data = new WebhookMessageData();

        // Assert
        Assert.NotNull(data.MessageId);
        Assert.NotNull(data.Status);
        Assert.NotNull(data.To);
        Assert.NotNull(data.From);
        Assert.Equal(1, data.Segments);
        Assert.Equal(0, data.CreditsUsed);
    }

    [Fact]
    public void WebhookMessageData_CanSetAllProperties()
    {
        // Arrange & Act
        var data = new WebhookMessageData
        {
            MessageId = "msg_123",
            Status = "delivered",
            To = "+15551234567",
            From = "Sendly",
            Error = "Test error",
            ErrorCode = "TEST_ERROR",
            DeliveredAt = "2024-01-20T10:05:00Z",
            FailedAt = "2024-01-20T10:05:00Z",
            Segments = 3,
            CreditsUsed = 3
        };

        // Assert
        Assert.Equal("msg_123", data.MessageId);
        Assert.Equal("delivered", data.Status);
        Assert.Equal("+15551234567", data.To);
        Assert.Equal("Sendly", data.From);
        Assert.Equal("Test error", data.Error);
        Assert.Equal("TEST_ERROR", data.ErrorCode);
        Assert.Equal("2024-01-20T10:05:00Z", data.DeliveredAt);
        Assert.Equal("2024-01-20T10:05:00Z", data.FailedAt);
        Assert.Equal(3, data.Segments);
        Assert.Equal(3, data.CreditsUsed);
    }

    #endregion

    #region WebhookSignatureException Tests

    [Fact]
    public void WebhookSignatureException_DefaultConstructor_SetsDefaultMessage()
    {
        // Arrange & Act
        var exception = new WebhookSignatureException();

        // Assert
        Assert.Equal("Invalid webhook signature", exception.Message);
    }

    [Fact]
    public void WebhookSignatureException_WithCustomMessage_SetsMessage()
    {
        // Arrange & Act
        var exception = new WebhookSignatureException("Custom error message");

        // Assert
        Assert.Equal("Custom error message", exception.Message);
    }

    [Fact]
    public void WebhookSignatureException_InheritsFromException()
    {
        // Assert
        Assert.True(typeof(Exception).IsAssignableFrom(typeof(WebhookSignatureException)));
    }

    #endregion

    #region Integration-like Tests

    [Fact]
    public void WebhookWorkflow_CompleteFlow_WorksCorrectly()
    {
        // Arrange - Simulate receiving a webhook
        var payload = @"{
            ""id"": ""evt_integration"",
            ""type"": ""message.delivered"",
            ""data"": {
                ""message_id"": ""msg_integration"",
                ""status"": ""delivered"",
                ""to"": ""+15551234567"",
                ""from"": ""Sendly"",
                ""segments"": 1,
                ""credits_used"": 1,
                ""delivered_at"": ""2024-01-20T10:05:00Z""
            },
            ""created_at"": ""2024-01-20T10:00:00Z"",
            ""api_version"": ""2024-01-01""
        }";

        // Generate signature as if it came from Sendly
        var signature = Webhooks.GenerateSignature(payload, TestSecret);

        // Act - Verify and parse
        var isValid = Webhooks.VerifySignature(payload, signature, TestSecret);
        var webhookEvent = Webhooks.ParseEvent(payload, signature, TestSecret);

        // Assert
        Assert.True(isValid);
        Assert.NotNull(webhookEvent);
        Assert.Equal("evt_integration", webhookEvent.Id);
        Assert.Equal("message.delivered", webhookEvent.Type);
        Assert.Equal("msg_integration", webhookEvent.Data.MessageId);
        Assert.Equal("delivered", webhookEvent.Data.Status);
    }

    [Theory]
    [InlineData("message.delivered")]
    [InlineData("message.failed")]
    [InlineData("message.sent")]
    [InlineData("message.queued")]
    public void WebhookWorkflow_WithVariousEventTypes_ParsesCorrectly(string eventType)
    {
        // Arrange
        var payload = $@"{{
            ""id"": ""evt_{eventType}"",
            ""type"": ""{eventType}"",
            ""data"": {{
                ""message_id"": ""msg_123"",
                ""status"": ""{eventType.Split('.')[1]}"",
                ""to"": ""+15551234567"",
                ""from"": ""Sendly"",
                ""segments"": 1,
                ""credits_used"": 1
            }},
            ""created_at"": ""2024-01-20T10:00:00Z""
        }}";
        var signature = Webhooks.GenerateSignature(payload, TestSecret);

        // Act
        var webhookEvent = Webhooks.ParseEvent(payload, signature, TestSecret);

        // Assert
        Assert.Equal(eventType, webhookEvent.Type);
    }

    #endregion
}
