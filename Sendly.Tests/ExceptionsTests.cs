using Sendly.Exceptions;
using Xunit;

namespace Sendly.Tests;

/// <summary>
/// Tests for all Sendly exception classes.
/// </summary>
public class ExceptionsTests
{
    #region SendlyException Tests

    [Fact]
    public void SendlyException_WithMessage_SetsMessage()
    {
        // Arrange & Act
        var exception = new SendlyException("Test error");

        // Assert
        Assert.Equal("Test error", exception.Message);
        Assert.Equal(0, exception.StatusCode);
        Assert.Null(exception.ErrorCode);
    }

    [Fact]
    public void SendlyException_WithMessageAndStatusCode_SetsProperties()
    {
        // Arrange & Act
        var exception = new SendlyException("Server error", 500);

        // Assert
        Assert.Equal("Server error", exception.Message);
        Assert.Equal(500, exception.StatusCode);
        Assert.Null(exception.ErrorCode);
    }

    [Fact]
    public void SendlyException_WithAllParameters_SetsAllProperties()
    {
        // Arrange & Act
        var exception = new SendlyException("Custom error", 418, "TEAPOT_ERROR");

        // Assert
        Assert.Equal("Custom error", exception.Message);
        Assert.Equal(418, exception.StatusCode);
        Assert.Equal("TEAPOT_ERROR", exception.ErrorCode);
        Assert.Null(exception.InnerException);
    }

    [Fact]
    public void SendlyException_WithInnerException_SetsInnerException()
    {
        // Arrange
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new SendlyException("Outer error", 500, "ERROR", innerException);

        // Assert
        Assert.Equal("Outer error", exception.Message);
        Assert.NotNull(exception.InnerException);
        Assert.Equal("Inner error", exception.InnerException.Message);
        Assert.IsType<InvalidOperationException>(exception.InnerException);
    }

    [Fact]
    public void SendlyException_IsException()
    {
        // Assert
        Assert.True(typeof(Exception).IsAssignableFrom(typeof(SendlyException)));
    }

    #endregion

    #region AuthenticationException Tests

    [Fact]
    public void AuthenticationException_WithDefaultConstructor_SetsDefaultMessage()
    {
        // Arrange & Act
        var exception = new AuthenticationException();

        // Assert
        Assert.Equal("Invalid or missing API key", exception.Message);
        Assert.Equal(401, exception.StatusCode);
        Assert.Equal("AUTHENTICATION_ERROR", exception.ErrorCode);
    }

    [Fact]
    public void AuthenticationException_WithCustomMessage_SetsCustomMessage()
    {
        // Arrange & Act
        var exception = new AuthenticationException("Custom auth error");

        // Assert
        Assert.Equal("Custom auth error", exception.Message);
        Assert.Equal(401, exception.StatusCode);
        Assert.Equal("AUTHENTICATION_ERROR", exception.ErrorCode);
    }

    [Fact]
    public void AuthenticationException_InheritsFromSendlyException()
    {
        // Assert
        Assert.True(typeof(SendlyException).IsAssignableFrom(typeof(AuthenticationException)));
    }

    [Fact]
    public void AuthenticationException_CanBeCaughtAsSendlyException()
    {
        // Arrange & Act
        SendlyException caughtException = null!;

        try
        {
            throw new AuthenticationException("Test auth error");
        }
        catch (SendlyException ex)
        {
            caughtException = ex;
        }

        // Assert
        Assert.NotNull(caughtException);
        Assert.IsType<AuthenticationException>(caughtException);
        Assert.Equal("Test auth error", caughtException.Message);
    }

    #endregion

    #region ValidationException Tests

    [Fact]
    public void ValidationException_WithDefaultConstructor_SetsDefaultMessage()
    {
        // Arrange & Act
        var exception = new ValidationException();

        // Assert
        Assert.Equal("Validation failed", exception.Message);
        Assert.Equal(400, exception.StatusCode);
        Assert.Equal("VALIDATION_ERROR", exception.ErrorCode);
    }

    [Fact]
    public void ValidationException_WithCustomMessage_SetsCustomMessage()
    {
        // Arrange & Act
        var exception = new ValidationException("Invalid phone number format");

        // Assert
        Assert.Equal("Invalid phone number format", exception.Message);
        Assert.Equal(400, exception.StatusCode);
        Assert.Equal("VALIDATION_ERROR", exception.ErrorCode);
    }

    [Fact]
    public void ValidationException_InheritsFromSendlyException()
    {
        // Assert
        Assert.True(typeof(SendlyException).IsAssignableFrom(typeof(ValidationException)));
    }

    [Theory]
    [InlineData("Phone number is required")]
    [InlineData("Message text is too long")]
    [InlineData("Invalid format")]
    public void ValidationException_CanStoreVariousValidationMessages(string message)
    {
        // Arrange & Act
        var exception = new ValidationException(message);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Equal(400, exception.StatusCode);
    }

    #endregion

    #region InsufficientCreditsException Tests

    [Fact]
    public void InsufficientCreditsException_WithDefaultConstructor_SetsDefaultMessage()
    {
        // Arrange & Act
        var exception = new InsufficientCreditsException();

        // Assert
        Assert.Equal("Insufficient credits", exception.Message);
        Assert.Equal(402, exception.StatusCode);
        Assert.Equal("INSUFFICIENT_CREDITS", exception.ErrorCode);
    }

    [Fact]
    public void InsufficientCreditsException_WithCustomMessage_SetsCustomMessage()
    {
        // Arrange & Act
        var exception = new InsufficientCreditsException("You need 10 more credits");

        // Assert
        Assert.Equal("You need 10 more credits", exception.Message);
        Assert.Equal(402, exception.StatusCode);
        Assert.Equal("INSUFFICIENT_CREDITS", exception.ErrorCode);
    }

    [Fact]
    public void InsufficientCreditsException_InheritsFromSendlyException()
    {
        // Assert
        Assert.True(typeof(SendlyException).IsAssignableFrom(typeof(InsufficientCreditsException)));
    }

    #endregion

    #region NotFoundException Tests

    [Fact]
    public void NotFoundException_WithDefaultConstructor_SetsDefaultMessage()
    {
        // Arrange & Act
        var exception = new NotFoundException();

        // Assert
        Assert.Equal("Resource not found", exception.Message);
        Assert.Equal(404, exception.StatusCode);
        Assert.Equal("NOT_FOUND", exception.ErrorCode);
    }

    [Fact]
    public void NotFoundException_WithCustomMessage_SetsCustomMessage()
    {
        // Arrange & Act
        var exception = new NotFoundException("Message msg_123 not found");

        // Assert
        Assert.Equal("Message msg_123 not found", exception.Message);
        Assert.Equal(404, exception.StatusCode);
        Assert.Equal("NOT_FOUND", exception.ErrorCode);
    }

    [Fact]
    public void NotFoundException_InheritsFromSendlyException()
    {
        // Assert
        Assert.True(typeof(SendlyException).IsAssignableFrom(typeof(NotFoundException)));
    }

    #endregion

    #region RateLimitException Tests

    [Fact]
    public void RateLimitException_WithDefaultConstructor_SetsDefaultMessage()
    {
        // Arrange & Act
        var exception = new RateLimitException();

        // Assert
        Assert.Equal("Rate limit exceeded", exception.Message);
        Assert.Equal(429, exception.StatusCode);
        Assert.Equal("RATE_LIMIT_EXCEEDED", exception.ErrorCode);
        Assert.Null(exception.RetryAfter);
    }

    [Fact]
    public void RateLimitException_WithCustomMessage_SetsCustomMessage()
    {
        // Arrange & Act
        var exception = new RateLimitException("Too many requests");

        // Assert
        Assert.Equal("Too many requests", exception.Message);
        Assert.Equal(429, exception.StatusCode);
        Assert.Null(exception.RetryAfter);
    }

    [Fact]
    public void RateLimitException_WithRetryAfter_SetsRetryAfter()
    {
        // Arrange
        var retryAfter = TimeSpan.FromSeconds(60);

        // Act
        var exception = new RateLimitException("Rate limited", retryAfter);

        // Assert
        Assert.Equal("Rate limited", exception.Message);
        Assert.Equal(429, exception.StatusCode);
        Assert.NotNull(exception.RetryAfter);
        Assert.Equal(TimeSpan.FromSeconds(60), exception.RetryAfter);
    }

    [Fact]
    public void RateLimitException_WithZeroRetryAfter_StoresZero()
    {
        // Arrange & Act
        var exception = new RateLimitException("Rate limited", TimeSpan.Zero);

        // Assert
        Assert.NotNull(exception.RetryAfter);
        Assert.Equal(TimeSpan.Zero, exception.RetryAfter);
    }

    [Theory]
    [InlineData(30)]
    [InlineData(60)]
    [InlineData(120)]
    [InlineData(300)]
    public void RateLimitException_WithVariousRetryAfterValues_StoresCorrectly(int seconds)
    {
        // Arrange & Act
        var exception = new RateLimitException("Rate limited", TimeSpan.FromSeconds(seconds));

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(seconds), exception.RetryAfter);
    }

    [Fact]
    public void RateLimitException_InheritsFromSendlyException()
    {
        // Assert
        Assert.True(typeof(SendlyException).IsAssignableFrom(typeof(RateLimitException)));
    }

    #endregion

    #region NetworkException Tests

    [Fact]
    public void NetworkException_WithDefaultConstructor_SetsDefaultMessage()
    {
        // Arrange & Act
        var exception = new NetworkException();

        // Assert
        Assert.Equal("Network error occurred", exception.Message);
        Assert.Equal(0, exception.StatusCode);
        Assert.Equal("NETWORK_ERROR", exception.ErrorCode);
        Assert.Null(exception.InnerException);
    }

    [Fact]
    public void NetworkException_WithCustomMessage_SetsCustomMessage()
    {
        // Arrange & Act
        var exception = new NetworkException("Connection timeout");

        // Assert
        Assert.Equal("Connection timeout", exception.Message);
        Assert.Equal(0, exception.StatusCode);
        Assert.Equal("NETWORK_ERROR", exception.ErrorCode);
    }

    [Fact]
    public void NetworkException_WithInnerException_SetsInnerException()
    {
        // Arrange
        var innerException = new HttpRequestException("Connection refused");

        // Act
        var exception = new NetworkException("Network error", innerException);

        // Assert
        Assert.Equal("Network error", exception.Message);
        Assert.NotNull(exception.InnerException);
        Assert.Equal("Connection refused", exception.InnerException.Message);
        Assert.IsType<HttpRequestException>(exception.InnerException);
    }

    [Fact]
    public void NetworkException_InheritsFromSendlyException()
    {
        // Assert
        Assert.True(typeof(SendlyException).IsAssignableFrom(typeof(NetworkException)));
    }

    [Fact]
    public void NetworkException_HasZeroStatusCode()
    {
        // Arrange & Act
        var exception = new NetworkException("Test");

        // Assert
        Assert.Equal(0, exception.StatusCode);
    }

    #endregion

    #region Exception Hierarchy Tests

    [Fact]
    public void AllExceptions_InheritFromSendlyException()
    {
        // Assert
        Assert.True(typeof(SendlyException).IsAssignableFrom(typeof(AuthenticationException)));
        Assert.True(typeof(SendlyException).IsAssignableFrom(typeof(ValidationException)));
        Assert.True(typeof(SendlyException).IsAssignableFrom(typeof(InsufficientCreditsException)));
        Assert.True(typeof(SendlyException).IsAssignableFrom(typeof(NotFoundException)));
        Assert.True(typeof(SendlyException).IsAssignableFrom(typeof(RateLimitException)));
        Assert.True(typeof(SendlyException).IsAssignableFrom(typeof(NetworkException)));
    }

    [Fact]
    public void AllExceptions_CanBeCaughtAsException()
    {
        // Arrange
        var exceptions = new Exception[]
        {
            new SendlyException("Test"),
            new AuthenticationException("Test"),
            new ValidationException("Test"),
            new InsufficientCreditsException("Test"),
            new NotFoundException("Test"),
            new RateLimitException("Test"),
            new NetworkException("Test")
        };

        // Act & Assert
        foreach (var ex in exceptions)
        {
            Assert.True(ex is Exception);
        }
    }

    [Fact]
    public void ExceptionStatusCodes_AreCorrect()
    {
        // Assert
        Assert.Equal(0, new SendlyException("Test").StatusCode);
        Assert.Equal(401, new AuthenticationException().StatusCode);
        Assert.Equal(400, new ValidationException().StatusCode);
        Assert.Equal(402, new InsufficientCreditsException().StatusCode);
        Assert.Equal(404, new NotFoundException().StatusCode);
        Assert.Equal(429, new RateLimitException().StatusCode);
        Assert.Equal(0, new NetworkException().StatusCode);
    }

    [Fact]
    public void ExceptionErrorCodes_AreCorrect()
    {
        // Assert
        Assert.Null(new SendlyException("Test").ErrorCode);
        Assert.Equal("AUTHENTICATION_ERROR", new AuthenticationException().ErrorCode);
        Assert.Equal("VALIDATION_ERROR", new ValidationException().ErrorCode);
        Assert.Equal("INSUFFICIENT_CREDITS", new InsufficientCreditsException().ErrorCode);
        Assert.Equal("NOT_FOUND", new NotFoundException().ErrorCode);
        Assert.Equal("RATE_LIMIT_EXCEEDED", new RateLimitException().ErrorCode);
        Assert.Equal("NETWORK_ERROR", new NetworkException().ErrorCode);
    }

    #endregion

    #region Exception Serialization Tests

    [Fact]
    public void SendlyException_ToString_ContainsMessage()
    {
        // Arrange
        var exception = new SendlyException("Test error message");

        // Act
        var toString = exception.ToString();

        // Assert
        Assert.Contains("Test error message", toString);
    }

    [Fact]
    public void AuthenticationException_ToString_ContainsMessage()
    {
        // Arrange
        var exception = new AuthenticationException("Invalid API key");

        // Act
        var toString = exception.ToString();

        // Assert
        Assert.Contains("Invalid API key", toString);
    }

    [Fact]
    public void ExceptionWithInnerException_ToString_ContainsInnerException()
    {
        // Arrange
        var inner = new InvalidOperationException("Inner error");
        var exception = new NetworkException("Outer error", inner);

        // Act
        var toString = exception.ToString();

        // Assert
        Assert.Contains("Outer error", toString);
        Assert.Contains("Inner error", toString);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void SendlyException_WithNullMessage_HandlesGracefully()
    {
        // Arrange & Act
        var exception = new SendlyException(null!);

        // Assert
        Assert.NotNull(exception);
    }

    [Fact]
    public void SendlyException_WithEmptyMessage_HandlesGracefully()
    {
        // Arrange & Act
        var exception = new SendlyException("");

        // Assert
        Assert.Equal("", exception.Message);
    }

    [Fact]
    public void RateLimitException_WithNullRetryAfter_IsNull()
    {
        // Arrange & Act
        var exception = new RateLimitException("Test", null);

        // Assert
        Assert.Null(exception.RetryAfter);
    }

    [Fact]
    public void SendlyException_WithNegativeStatusCode_AcceptsValue()
    {
        // Arrange & Act
        var exception = new SendlyException("Test", -1);

        // Assert
        Assert.Equal(-1, exception.StatusCode);
    }

    [Fact]
    public void SendlyException_WithLargeStatusCode_AcceptsValue()
    {
        // Arrange & Act
        var exception = new SendlyException("Test", 999);

        // Assert
        Assert.Equal(999, exception.StatusCode);
    }

    #endregion
}
