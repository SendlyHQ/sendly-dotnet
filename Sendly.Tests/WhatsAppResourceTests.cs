using System.Net;
using System.Reflection;
using Sendly.Exceptions;
using Sendly.Resources;
using Sendly.Tests.Fixtures;
using Xunit;

namespace Sendly.Tests;

/// <summary>
/// Tests for WhatsAppResource - Signup Create/Get, Senders List,
/// Templates List/Create/Update/Delete, and Window.
/// </summary>
public class WhatsAppResourceTests : IDisposable
{
    private readonly MockHttpMessageHandler _mockHandler;
    private readonly HttpClient _httpClient;
    private readonly SendlyClient _client;

    public WhatsAppResourceTests()
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

    #region Signup CreateAsync Tests

    [Fact]
    public async Task SignupCreateAsync_ReturnsSession()
    {
        // Arrange
        var responseJson = @"{
            ""id"": ""was_123"",
            ""connectUrl"": ""https://sendly.live/whatsapp/connect/tok_abc"",
            ""status"": ""initiated""
        }";
        _mockHandler.QueueSuccessResponse(responseJson);

        // Act
        var result = await _client.WhatsApp.Signup.CreateAsync("+15559876543");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("was_123", result.Id);
        Assert.Equal("https://sendly.live/whatsapp/connect/tok_abc", result.ConnectUrl);
        Assert.Equal("initiated", result.Status);
    }

    [Fact]
    public async Task SignupCreateAsync_SendsCorrectBody()
    {
        // Arrange
        _mockHandler.QueueSuccessResponse(@"{""id"": ""was_123"", ""connectUrl"": ""https://x"", ""status"": ""initiated""}");

        // Act
        await _client.WhatsApp.Signup.CreateAsync("+15559876543");

        // Assert
        var request = _mockHandler.LastRequest;
        Assert.NotNull(request);
        Assert.Equal(HttpMethod.Post, request!.Method);
        Assert.Contains("whatsapp/signup", request.RequestUri?.ToString());
        var body = await request.Content!.ReadAsStringAsync();
        Assert.Contains("\"phoneNumber\":\"\\u002B15559876543\"", body);
    }

    [Theory]
    [InlineData("")]
    [InlineData("15559876543")]
    [InlineData("invalid")]
    public async Task SignupCreateAsync_WithInvalidPhoneNumber_ThrowsValidationException(string invalidPhone)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _client.WhatsApp.Signup.CreateAsync(invalidPhone));
    }

    #endregion

    #region Signup GetAsync Tests

    [Fact]
    public async Task SignupGetAsync_ReturnsSignup()
    {
        // Arrange
        var responseJson = @"{
            ""id"": ""was_123"",
            ""status"": ""active"",
            ""phoneNumber"": ""+15559876543"",
            ""businessAccountId"": ""waba_456"",
            ""failureReasons"": null,
            ""updatedAt"": ""2026-07-01T10:00:00Z""
        }";
        _mockHandler.QueueSuccessResponse(responseJson);

        // Act
        var result = await _client.WhatsApp.Signup.GetAsync("was_123");

        // Assert
        Assert.Equal("was_123", result.Id);
        Assert.Equal("active", result.Status);
        Assert.Equal("+15559876543", result.PhoneNumber);
        Assert.Equal("waba_456", result.BusinessAccountId);
        Assert.Null(result.FailureReasons);
        Assert.Equal("2026-07-01T10:00:00Z", result.UpdatedAt);
    }

    [Fact]
    public async Task SignupGetAsync_WhenFailed_ReturnsFailureReasons()
    {
        // Arrange
        var responseJson = @"{
            ""id"": ""was_123"",
            ""status"": ""failed"",
            ""phoneNumber"": ""+15559876543"",
            ""businessAccountId"": null,
            ""failureReasons"": [""Number is registered to another WhatsApp account""],
            ""updatedAt"": ""2026-07-01T10:00:00Z""
        }";
        _mockHandler.QueueSuccessResponse(responseJson);

        // Act
        var result = await _client.WhatsApp.Signup.GetAsync("was_123");

        // Assert
        Assert.Equal("failed", result.Status);
        Assert.Null(result.BusinessAccountId);
        Assert.NotNull(result.FailureReasons);
        Assert.Single(result.FailureReasons!);
    }

    [Fact]
    public async Task SignupGetAsync_HitsCorrectPath()
    {
        // Arrange
        _mockHandler.QueueSuccessResponse(@"{""id"": ""was_123"", ""status"": ""initiated""}");

        // Act
        await _client.WhatsApp.Signup.GetAsync("was_123");

        // Assert
        var request = _mockHandler.LastRequest;
        Assert.NotNull(request);
        Assert.Equal(HttpMethod.Get, request!.Method);
        Assert.Contains("whatsapp/signup/was_123", request.RequestUri?.ToString());
    }

    [Fact]
    public async Task SignupGetAsync_WithEmptyId_ThrowsValidationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _client.WhatsApp.Signup.GetAsync(""));
    }

    #endregion

    #region Senders ListAsync Tests

    [Fact]
    public async Task SendersListAsync_ReturnsSenders()
    {
        // Arrange
        var responseJson = @"{
            ""senders"": [
                {
                    ""phoneNumber"": ""+15559876543"",
                    ""displayName"": ""Acme"",
                    ""status"": ""active"",
                    ""qualityRating"": ""GREEN"",
                    ""createdAt"": ""2026-07-01T10:00:00Z""
                },
                {
                    ""phoneNumber"": ""+15551112222"",
                    ""displayName"": null,
                    ""status"": ""pending"",
                    ""qualityRating"": null,
                    ""createdAt"": ""2026-07-02T10:00:00Z""
                }
            ]
        }";
        _mockHandler.QueueSuccessResponse(responseJson);

        // Act
        var result = await _client.WhatsApp.Senders.ListAsync();

        // Assert
        Assert.Equal(2, result.Senders.Count);
        Assert.Equal("+15559876543", result.Senders[0].PhoneNumber);
        Assert.Equal("Acme", result.Senders[0].DisplayName);
        Assert.Equal("active", result.Senders[0].Status);
        Assert.Equal("GREEN", result.Senders[0].QualityRating);
        Assert.Null(result.Senders[1].DisplayName);
        Assert.Equal("pending", result.Senders[1].Status);
        Assert.Null(result.Senders[1].QualityRating);
    }

    [Fact]
    public async Task SendersListAsync_WhenEmpty_ReturnsEmptyList()
    {
        // Arrange
        _mockHandler.QueueSuccessResponse(@"{""senders"": []}");

        // Act
        var result = await _client.WhatsApp.Senders.ListAsync();

        // Assert
        Assert.Empty(result.Senders);
    }

    [Fact]
    public async Task SendersListAsync_HitsCorrectPath()
    {
        // Arrange
        _mockHandler.QueueSuccessResponse(@"{""senders"": []}");

        // Act
        await _client.WhatsApp.Senders.ListAsync();

        // Assert
        var request = _mockHandler.LastRequest;
        Assert.NotNull(request);
        Assert.Equal(HttpMethod.Get, request!.Method);
        Assert.Contains("whatsapp/senders", request.RequestUri?.ToString());
    }

    #endregion

    #region Senders GetProfileAsync Tests

    [Fact]
    public async Task SendersGetProfileAsync_ReturnsProfile()
    {
        // Arrange
        var responseJson = @"{
            ""phoneNumber"": ""+15559876543"",
            ""displayName"": ""Acme Inc"",
            ""profilePhotoUrl"": ""https://example.com/logo.png"",
            ""category"": ""Retail"",
            ""about"": ""Fast delivery, friendly service"",
            ""description"": ""Acme sells everything."",
            ""email"": ""help@example.com"",
            ""website"": ""https://example.com"",
            ""address"": ""123 Main St, Springfield""
        }";
        _mockHandler.QueueSuccessResponse(responseJson);

        // Act
        var profile = await _client.WhatsApp.Senders.GetProfileAsync("+15559876543");

        // Assert
        Assert.Equal("+15559876543", profile.PhoneNumber);
        Assert.Equal("Acme Inc", profile.DisplayName);
        Assert.Equal("https://example.com/logo.png", profile.ProfilePhotoUrl);
        Assert.Equal("Retail", profile.Category);
        Assert.Equal("Fast delivery, friendly service", profile.About);
        Assert.Equal("Acme sells everything.", profile.Description);
        Assert.Equal("help@example.com", profile.Email);
        Assert.Equal("https://example.com", profile.Website);
        Assert.Equal("123 Main St, Springfield", profile.Address);
    }

    [Fact]
    public async Task SendersGetProfileAsync_WhenUnset_ReturnsNullFields()
    {
        // Arrange
        _mockHandler.QueueSuccessResponse(@"{
            ""phoneNumber"": ""+15559876543"",
            ""displayName"": null,
            ""profilePhotoUrl"": null,
            ""category"": null,
            ""about"": null,
            ""description"": null,
            ""email"": null,
            ""website"": null,
            ""address"": null
        }");

        // Act
        var profile = await _client.WhatsApp.Senders.GetProfileAsync("+15559876543");

        // Assert
        Assert.Equal("+15559876543", profile.PhoneNumber);
        Assert.Null(profile.DisplayName);
        Assert.Null(profile.ProfilePhotoUrl);
        Assert.Null(profile.About);
        Assert.Null(profile.Description);
    }

    [Fact]
    public async Task SendersGetProfileAsync_HitsCorrectPath()
    {
        // Arrange
        _mockHandler.QueueSuccessResponse(@"{""phoneNumber"": ""+15559876543""}");

        // Act
        await _client.WhatsApp.Senders.GetProfileAsync("+15559876543");

        // Assert
        var request = _mockHandler.LastRequest;
        Assert.NotNull(request);
        Assert.Equal(HttpMethod.Get, request!.Method);
        Assert.Contains("whatsapp/senders/%2B15559876543/profile", request.RequestUri?.ToString());
    }

    [Fact]
    public async Task SendersGetProfileAsync_WithInvalidPhone_ThrowsValidationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _client.WhatsApp.Senders.GetProfileAsync("invalid"));
    }

    [Fact]
    public async Task SendersGetProfileAsync_WhenNotConnected_ThrowsNotFoundException()
    {
        // Arrange
        _mockHandler.QueueResponse(HttpStatusCode.NotFound,
            @"{""error"": ""whatsapp_sender_not_connected"", ""message"": ""This number isn't connected to WhatsApp yet.""}");

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => _client.WhatsApp.Senders.GetProfileAsync("+15559876543"));
    }

    #endregion

    #region Senders UpdateProfileAsync Tests

    [Fact]
    public async Task SendersUpdateProfileAsync_ReturnsUpdatedProfile()
    {
        // Arrange
        _mockHandler.QueueSuccessResponse(@"{
            ""phoneNumber"": ""+15559876543"",
            ""displayName"": ""Acme Inc"",
            ""profilePhotoUrl"": null,
            ""category"": ""Retail"",
            ""about"": ""Fast delivery, friendly service"",
            ""description"": null,
            ""email"": null,
            ""website"": ""https://example.com"",
            ""address"": null
        }");

        // Act
        var profile = await _client.WhatsApp.Senders.UpdateProfileAsync("+15559876543",
            new UpdateWhatsAppSenderProfileRequest
            {
                About = "Fast delivery, friendly service",
                Website = "https://example.com"
            });

        // Assert
        Assert.Equal("Fast delivery, friendly service", profile.About);
        Assert.Equal("https://example.com", profile.Website);
        Assert.Null(profile.Description);
    }

    [Fact]
    public async Task SendersUpdateProfileAsync_SendsOnlySuppliedFields()
    {
        // Arrange
        _mockHandler.QueueSuccessResponse(@"{""phoneNumber"": ""+15559876543""}");

        // Act
        await _client.WhatsApp.Senders.UpdateProfileAsync("+15559876543",
            new UpdateWhatsAppSenderProfileRequest
            {
                About = "Fast delivery, friendly service",
                Website = "https://example.com"
            });

        // Assert
        var request = _mockHandler.LastRequest;
        Assert.NotNull(request);
        Assert.Equal(HttpMethod.Patch, request!.Method);
        Assert.Contains("whatsapp/senders/%2B15559876543/profile", request.RequestUri?.ToString());
        var body = await request.Content!.ReadAsStringAsync();
        Assert.Contains("\"about\":\"Fast delivery, friendly service\"", body);
        Assert.Contains("\"website\":\"https://example.com\"", body);
        // omitted fields keep their current value, so they must not be sent
        Assert.DoesNotContain("displayName", body);
        Assert.DoesNotContain("description", body);
        Assert.DoesNotContain("category", body);
        Assert.DoesNotContain("email", body);
        Assert.DoesNotContain("address", body);
    }

    [Fact]
    public async Task SendersUpdateProfileAsync_WithInvalidPhone_ThrowsValidationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _client.WhatsApp.Senders.UpdateProfileAsync("invalid",
                new UpdateWhatsAppSenderProfileRequest { About = "Hi" }));
    }

    [Fact]
    public async Task SendersUpdateProfileAsync_WithNoFields_ThrowsFromServer()
    {
        // Arrange — the server requires at least one field
        _mockHandler.QueueResponse(HttpStatusCode.BadRequest,
            @"{""error"": ""invalid_request"", ""message"": ""Provide at least one profile field to update.""}");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _client.WhatsApp.Senders.UpdateProfileAsync("+15559876543",
                new UpdateWhatsAppSenderProfileRequest()));
        Assert.Contains("at least one profile field", exception.Message);
    }

    [Fact]
    public async Task SendersUpdateProfileAsync_WithTestKey_ThrowsSendlyException()
    {
        // Arrange
        const string forbidden =
            @"{""error"": ""whatsapp_requires_live_key"", ""message"": ""WhatsApp requires a live API key. Test keys cannot update sender profiles.""}";
        _mockHandler.QueueResponse(HttpStatusCode.Forbidden, forbidden);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<SendlyException>(
            () => _client.WhatsApp.Senders.UpdateProfileAsync("+15559876543",
                new UpdateWhatsAppSenderProfileRequest { About = "Hi" }));
        Assert.Contains("live API key", exception.Message);
        Assert.Equal(403, exception.StatusCode);
    }

    #endregion

    #region Templates ListAsync Tests

    [Fact]
    public async Task TemplatesListAsync_ReturnsTemplates()
    {
        // Arrange
        var responseJson = @"{
            ""templates"": [
                {
                    ""id"": ""wat_123"",
                    ""name"": ""order_shipped"",
                    ""language"": ""en_US"",
                    ""category"": ""UTILITY"",
                    ""status"": ""APPROVED"",
                    ""qualityRating"": ""GREEN"",
                    ""rejectionReason"": null,
                    ""createdAt"": ""2026-07-01T10:00:00Z"",
                    ""updatedAt"": ""2026-07-02T10:00:00Z""
                }
            ]
        }";
        _mockHandler.QueueSuccessResponse(responseJson);

        // Act
        var result = await _client.WhatsApp.Templates.ListAsync();

        // Assert
        Assert.Single(result.Templates);
        Assert.Equal("wat_123", result.Templates[0].Id);
        Assert.Equal("order_shipped", result.Templates[0].Name);
        Assert.Equal("en_US", result.Templates[0].Language);
        Assert.Equal("UTILITY", result.Templates[0].Category);
        Assert.Equal("APPROVED", result.Templates[0].Status);
        Assert.Equal("GREEN", result.Templates[0].QualityRating);
        Assert.Null(result.Templates[0].RejectionReason);
    }

    [Fact]
    public async Task TemplatesListAsync_HitsCorrectPath()
    {
        // Arrange
        _mockHandler.QueueSuccessResponse(@"{""templates"": []}");

        // Act
        await _client.WhatsApp.Templates.ListAsync();

        // Assert
        var request = _mockHandler.LastRequest;
        Assert.NotNull(request);
        Assert.Equal(HttpMethod.Get, request!.Method);
        Assert.Contains("whatsapp/templates", request.RequestUri?.ToString());
    }

    #endregion

    #region Templates CreateAsync Tests

    [Fact]
    public async Task TemplatesCreateAsync_ReturnsTemplate()
    {
        // Arrange
        var responseJson = @"{
            ""id"": ""wat_123"",
            ""name"": ""order_shipped"",
            ""language"": ""en_US"",
            ""category"": ""UTILITY"",
            ""status"": ""PENDING"",
            ""qualityRating"": null,
            ""rejectionReason"": null,
            ""createdAt"": ""2026-07-01T10:00:00Z"",
            ""updatedAt"": ""2026-07-01T10:00:00Z"",
            ""warnings"": [""Display name is not approved yet""]
        }";
        _mockHandler.QueueSuccessResponse(responseJson);

        // Act
        var result = await _client.WhatsApp.Templates.CreateAsync(new CreateWhatsAppTemplateRequest
        {
            Sender = "+15559876543",
            Name = "order_shipped",
            Language = "en_US",
            Category = "UTILITY",
            Body = "Hi {{1}}, your order {{2}} has shipped!",
            Examples = new() { ["1"] = "Sam", ["2"] = "#4821" }
        });

        // Assert
        Assert.Equal("wat_123", result.Id);
        Assert.Equal("PENDING", result.Status);
        Assert.NotNull(result.Warnings);
        Assert.Single(result.Warnings!);
    }

    [Fact]
    public async Task TemplatesCreateAsync_SendsCorrectBody()
    {
        // Arrange
        _mockHandler.QueueSuccessResponse(@"{""id"": ""wat_123"", ""status"": ""PENDING""}");

        // Act
        await _client.WhatsApp.Templates.CreateAsync(new CreateWhatsAppTemplateRequest
        {
            Sender = "+15559876543",
            Name = "order_shipped",
            Language = "en_US",
            Category = "UTILITY",
            Body = "Hi {{1}}, your order {{2}} has shipped!",
            Examples = new() { ["1"] = "Sam", ["2"] = "#4821" }
        });

        // Assert
        var request = _mockHandler.LastRequest;
        Assert.NotNull(request);
        Assert.Equal(HttpMethod.Post, request!.Method);
        Assert.Contains("whatsapp/templates", request.RequestUri?.ToString());
        var body = await request.Content!.ReadAsStringAsync();
        Assert.Contains("\"sender\":\"\\u002B15559876543\"", body);
        Assert.Contains("\"name\":\"order_shipped\"", body);
        Assert.Contains("\"language\":\"en_US\"", body);
        Assert.Contains("\"category\":\"UTILITY\"", body);
        Assert.Contains("\"examples\":{\"1\":\"Sam\",\"2\":\"#4821\"}", body);
        // null optionals should be omitted
        Assert.DoesNotContain("footer", body);
        Assert.DoesNotContain("header", body);
        Assert.DoesNotContain("buttons", body);
    }

    [Fact]
    public async Task TemplatesCreateAsync_WithButtons_SendsButtons()
    {
        // Arrange
        _mockHandler.QueueSuccessResponse(@"{""id"": ""wat_123"", ""status"": ""PENDING""}");

        // Act
        await _client.WhatsApp.Templates.CreateAsync(new CreateWhatsAppTemplateRequest
        {
            Sender = "+15559876543",
            Name = "spring_sale",
            Language = "en_US",
            Category = "MARKETING",
            Body = "Our spring sale is on!",
            Buttons = new()
            {
                new WhatsAppTemplateButton { Type = "quick_reply", Text = "Stop promotions" }
            }
        });

        // Assert
        var body = await _mockHandler.LastRequest!.Content!.ReadAsStringAsync();
        Assert.Contains("\"buttons\":[{\"type\":\"quick_reply\",\"text\":\"Stop promotions\"}]", body);
    }

    [Fact]
    public async Task TemplatesCreateAsync_WithInvalidSender_ThrowsValidationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _client.WhatsApp.Templates.CreateAsync(new CreateWhatsAppTemplateRequest
            {
                Sender = "not-a-number",
                Name = "order_shipped",
                Language = "en_US",
                Category = "UTILITY",
                Body = "Hi!"
            }));
    }

    #endregion

    #region Templates UpdateAsync Tests

    [Fact]
    public async Task TemplatesUpdateAsync_ReturnsTemplate()
    {
        // Arrange
        var responseJson = @"{
            ""id"": ""wat_123"",
            ""name"": ""order_shipped"",
            ""language"": ""en_US"",
            ""category"": ""UTILITY"",
            ""status"": ""PENDING"",
            ""qualityRating"": null,
            ""rejectionReason"": null,
            ""createdAt"": ""2026-07-01T10:00:00Z"",
            ""updatedAt"": ""2026-07-03T10:00:00Z""
        }";
        _mockHandler.QueueSuccessResponse(responseJson);

        // Act
        var result = await _client.WhatsApp.Templates.UpdateAsync("wat_123", new UpdateWhatsAppTemplateRequest
        {
            Body = "Hi {{1}}, your order {{2}} is on its way!",
            Examples = new() { ["1"] = "Sam", ["2"] = "#4821" }
        });

        // Assert
        Assert.Equal("wat_123", result.Id);
        Assert.Equal("PENDING", result.Status);
    }

    [Fact]
    public async Task TemplatesUpdateAsync_SendsOnlyChangedFields()
    {
        // Arrange
        _mockHandler.QueueSuccessResponse(@"{""id"": ""wat_123"", ""status"": ""PENDING""}");

        // Act
        await _client.WhatsApp.Templates.UpdateAsync("wat_123", new UpdateWhatsAppTemplateRequest
        {
            Body = "Hi {{1}}, your order {{2}} is on its way!"
        });

        // Assert
        var request = _mockHandler.LastRequest;
        Assert.NotNull(request);
        Assert.Equal(HttpMethod.Patch, request!.Method);
        Assert.Contains("whatsapp/templates/wat_123", request.RequestUri?.ToString());
        var body = await request.Content!.ReadAsStringAsync();
        Assert.Contains("\"body\":", body);
        Assert.DoesNotContain("footer", body);
        Assert.DoesNotContain("header", body);
        Assert.DoesNotContain("buttons", body);
        Assert.DoesNotContain("examples", body);
    }

    [Fact]
    public async Task TemplatesUpdateAsync_WithEmptyId_ThrowsValidationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _client.WhatsApp.Templates.UpdateAsync("", new UpdateWhatsAppTemplateRequest { Body = "Hi" }));
    }

    #endregion

    #region Templates DeleteAsync Tests

    [Fact]
    public async Task TemplatesDeleteAsync_ReturnsConfirmation()
    {
        // Arrange
        _mockHandler.QueueSuccessResponse(@"{""id"": ""wat_123"", ""deleted"": true}");

        // Act
        var result = await _client.WhatsApp.Templates.DeleteAsync("wat_123");

        // Assert
        Assert.Equal("wat_123", result.Id);
        Assert.True(result.Deleted);
    }

    [Fact]
    public async Task TemplatesDeleteAsync_HitsCorrectPath()
    {
        // Arrange
        _mockHandler.QueueSuccessResponse(@"{""id"": ""wat_123"", ""deleted"": true}");

        // Act
        await _client.WhatsApp.Templates.DeleteAsync("wat_123");

        // Assert
        var request = _mockHandler.LastRequest;
        Assert.NotNull(request);
        Assert.Equal(HttpMethod.Delete, request!.Method);
        Assert.Contains("whatsapp/templates/wat_123", request.RequestUri?.ToString());
    }

    [Fact]
    public async Task TemplatesDeleteAsync_WithEmptyId_ThrowsValidationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _client.WhatsApp.Templates.DeleteAsync(""));
    }

    #endregion

    #region WindowAsync Tests

    [Fact]
    public async Task WindowAsync_WhenOpen_ReturnsExpiry()
    {
        // Arrange
        _mockHandler.QueueSuccessResponse(@"{""open"": true, ""expiresAt"": ""2026-07-01T18:30:00Z""}");

        // Act
        var result = await _client.WhatsApp.WindowAsync("+15559876543", "+15551234567");

        // Assert
        Assert.True(result.Open);
        Assert.Equal("2026-07-01T18:30:00Z", result.ExpiresAt);
    }

    [Fact]
    public async Task WindowAsync_WhenClosed_ReturnsNullExpiry()
    {
        // Arrange
        _mockHandler.QueueSuccessResponse(@"{""open"": false, ""expiresAt"": null}");

        // Act
        var result = await _client.WhatsApp.WindowAsync("+15559876543", "+15551234567");

        // Assert
        Assert.False(result.Open);
        Assert.Null(result.ExpiresAt);
    }

    [Fact]
    public async Task WindowAsync_HitsCorrectPathWithQuery()
    {
        // Arrange
        _mockHandler.QueueSuccessResponse(@"{""open"": false, ""expiresAt"": null}");

        // Act
        await _client.WhatsApp.WindowAsync("+15559876543", "+15551234567");

        // Assert
        var request = _mockHandler.LastRequest;
        Assert.NotNull(request);
        Assert.Equal(HttpMethod.Get, request!.Method);
        var uri = request.RequestUri?.ToString();
        Assert.Contains("whatsapp/window", uri);
        Assert.Contains("from=%2B15559876543", uri);
        Assert.Contains("to=%2B15551234567", uri);
    }

    [Fact]
    public async Task WindowAsync_WithInvalidFrom_ThrowsValidationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _client.WhatsApp.WindowAsync("invalid", "+15551234567"));
    }

    [Fact]
    public async Task WindowAsync_WithInvalidTo_ThrowsValidationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _client.WhatsApp.WindowAsync("+15559876543", ""));
    }

    #endregion
}
