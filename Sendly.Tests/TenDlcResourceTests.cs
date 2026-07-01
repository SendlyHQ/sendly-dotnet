using System.Net;
using System.Reflection;
using Sendly.Exceptions;
using Sendly.Resources;
using Sendly.Tests.Fixtures;
using Xunit;

namespace Sendly.Tests;

/// <summary>
/// Tests for TenDlcResource - ListBrands, CreateBrand, GetBrand, Qualify,
/// ListCampaigns, CreateCampaign, GetCampaign, AssignNumber, and ListAssignments.
/// </summary>
public class TenDlcResourceTests : IDisposable
{
    private readonly MockHttpMessageHandler _mockHandler;
    private readonly HttpClient _httpClient;
    private readonly SendlyClient _client;

    public TenDlcResourceTests()
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

    #region ListBrandsAsync Tests

    [Fact]
    public async Task ListBrandsAsync_ReturnsBrands()
    {
        // Arrange
        var responseJson = @"{
            ""data"": [
                {
                    ""id"": ""brd_123"",
                    ""legalName"": ""Acme Holdings LLC"",
                    ""dba"": ""Acme"",
                    ""entityType"": ""PRIVATE_PROFIT"",
                    ""ein"": ""12-3456789"",
                    ""vertical"": ""RETAIL"",
                    ""website"": ""https://acme.example"",
                    ""status"": ""verified"",
                    ""identityStatus"": ""VERIFIED"",
                    ""failureReasons"": null,
                    ""createdAt"": ""2026-06-01T10:00:00Z"",
                    ""updatedAt"": ""2026-06-02T10:00:00Z""
                }
            ]
        }";
        _mockHandler.QueueSuccessResponse(responseJson);

        // Act
        var result = await _client.TenDlc.ListBrandsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Data);
        Assert.Equal("brd_123", result.Data[0].Id);
        Assert.Equal("Acme Holdings LLC", result.Data[0].LegalName);
        Assert.Equal("Acme", result.Data[0].Dba);
        Assert.Equal("PRIVATE_PROFIT", result.Data[0].EntityType);
        Assert.Equal("verified", result.Data[0].Status);
        Assert.Equal("VERIFIED", result.Data[0].IdentityStatus);
        Assert.Null(result.Data[0].FailureReasons);
    }

    [Fact]
    public async Task ListBrandsAsync_HitsCorrectPath()
    {
        // Arrange
        _mockHandler.QueueSuccessResponse(@"{""data"": []}");

        // Act
        await _client.TenDlc.ListBrandsAsync();

        // Assert
        var request = _mockHandler.LastRequest;
        Assert.NotNull(request);
        Assert.Equal(HttpMethod.Get, request!.Method);
        Assert.Contains("tendlc/brands", request.RequestUri?.ToString());
    }

    [Fact]
    public async Task ListBrandsAsync_With401Response_ThrowsAuthenticationException()
    {
        // Arrange
        _mockHandler.QueueResponse(HttpStatusCode.Unauthorized, @"{""message"": ""Invalid API key""}");

        // Act & Assert
        await Assert.ThrowsAsync<AuthenticationException>(() => _client.TenDlc.ListBrandsAsync());
    }

    #endregion

    #region CreateBrandAsync Tests

    [Fact]
    public async Task CreateBrandAsync_ReturnsBrand()
    {
        // Arrange
        var responseJson = @"{
            ""data"": {
                ""id"": ""brd_123"",
                ""legalName"": ""Acme Holdings LLC"",
                ""entityType"": ""PRIVATE_PROFIT"",
                ""status"": ""pending"",
                ""createdAt"": ""2026-06-01T10:00:00Z"",
                ""updatedAt"": ""2026-06-01T10:00:00Z""
            }
        }";
        _mockHandler.QueueSuccessResponse(responseJson);

        // Act
        var result = await _client.TenDlc.CreateBrandAsync(new CreateTenDlcBrandRequest
        {
            LegalName = "Acme Holdings LLC",
            Ein = "12-3456789"
        });

        // Assert
        Assert.NotNull(result);
        Assert.Equal("brd_123", result.Data.Id);
        Assert.Equal("Acme Holdings LLC", result.Data.LegalName);
        Assert.Equal("pending", result.Data.Status);
    }

    [Fact]
    public async Task CreateBrandAsync_SendsCorrectBody()
    {
        // Arrange
        _mockHandler.QueueSuccessResponse(@"{""data"": {""id"": ""brd_123""}}");

        // Act
        await _client.TenDlc.CreateBrandAsync(new CreateTenDlcBrandRequest
        {
            LegalName = "Acme Holdings LLC",
            Ein = "12-3456789",
            EntityType = "PRIVATE_PROFIT",
            Website = "https://acme.example",
            Email = "ops@acme.example"
        });

        // Assert
        var request = _mockHandler.LastRequest;
        Assert.NotNull(request);
        Assert.Equal(HttpMethod.Post, request!.Method);
        Assert.Contains("tendlc/brands", request.RequestUri?.ToString());
        var body = await request.Content!.ReadAsStringAsync();
        Assert.Contains("\"legalName\":\"Acme Holdings LLC\"", body);
        Assert.Contains("\"ein\":\"12-3456789\"", body);
        Assert.Contains("\"entityType\":\"PRIVATE_PROFIT\"", body);
        Assert.Contains("\"website\":\"https://acme.example\"", body);
        Assert.Contains("\"email\":\"ops@acme.example\"", body);
        // null optionals should be omitted
        Assert.DoesNotContain("dba", body);
        Assert.DoesNotContain("verificationId", body);
    }

    [Fact]
    public async Task CreateBrandAsync_WithoutLegalName_ThrowsValidationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _client.TenDlc.CreateBrandAsync(new CreateTenDlcBrandRequest { LegalName = "" }));
    }

    #endregion

    #region GetBrandAsync Tests

    [Fact]
    public async Task GetBrandAsync_ReturnsBrand()
    {
        // Arrange
        var responseJson = @"{
            ""data"": {
                ""id"": ""brd_123"",
                ""legalName"": ""Acme Holdings LLC"",
                ""entityType"": ""PRIVATE_PROFIT"",
                ""status"": ""failed"",
                ""failureReasons"": [""Business address could not be confirmed""],
                ""createdAt"": ""2026-06-01T10:00:00Z"",
                ""updatedAt"": ""2026-06-03T10:00:00Z""
            }
        }";
        _mockHandler.QueueSuccessResponse(responseJson);

        // Act
        var result = await _client.TenDlc.GetBrandAsync("brd_123");

        // Assert
        Assert.Equal("brd_123", result.Data.Id);
        Assert.Equal("failed", result.Data.Status);
        Assert.NotNull(result.Data.FailureReasons);
        Assert.Single(result.Data.FailureReasons!);
    }

    [Fact]
    public async Task GetBrandAsync_HitsCorrectPath()
    {
        // Arrange
        _mockHandler.QueueSuccessResponse(@"{""data"": {""id"": ""brd_123""}}");

        // Act
        await _client.TenDlc.GetBrandAsync("brd_123");

        // Assert
        var request = _mockHandler.LastRequest;
        Assert.NotNull(request);
        Assert.Equal(HttpMethod.Get, request!.Method);
        Assert.Contains("tendlc/brands/brd_123", request.RequestUri?.ToString());
    }

    [Fact]
    public async Task GetBrandAsync_WithEmptyId_ThrowsValidationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _client.TenDlc.GetBrandAsync(""));
    }

    #endregion

    #region QualifyAsync Tests

    [Fact]
    public async Task QualifyAsync_ReturnsQualification()
    {
        // Arrange
        var responseJson = @"{
            ""data"": {
                ""useCase"": ""MIXED"",
                ""qualified"": true,
                ""reason"": null,
                ""throughput"": { ""tier"": ""Standard"", ""carriersReady"": 3 }
            }
        }";
        _mockHandler.QueueSuccessResponse(responseJson);

        // Act
        var result = await _client.TenDlc.QualifyAsync("brd_123", "MIXED");

        // Assert
        Assert.Equal("MIXED", result.Data.UseCase);
        Assert.True(result.Data.Qualified);
        Assert.Null(result.Data.Reason);
        Assert.NotNull(result.Data.Throughput);
        Assert.Equal("Standard", result.Data.Throughput!.Tier);
        Assert.Equal(3, result.Data.Throughput.CarriersReady);
    }

    [Fact]
    public async Task QualifyAsync_WhenNotQualified_ReturnsReason()
    {
        // Arrange
        var responseJson = @"{
            ""data"": {
                ""useCase"": ""MARKETING"",
                ""qualified"": false,
                ""reason"": ""Use case not available for this brand"",
                ""throughput"": null
            }
        }";
        _mockHandler.QueueSuccessResponse(responseJson);

        // Act
        var result = await _client.TenDlc.QualifyAsync("brd_123", "MARKETING");

        // Assert
        Assert.False(result.Data.Qualified);
        Assert.Equal("Use case not available for this brand", result.Data.Reason);
        Assert.Null(result.Data.Throughput);
    }

    [Fact]
    public async Task QualifyAsync_HitsCorrectPath()
    {
        // Arrange
        _mockHandler.QueueSuccessResponse(@"{""data"": {""useCase"": ""MIXED"", ""qualified"": true}}");

        // Act
        await _client.TenDlc.QualifyAsync("brd_123", "MIXED");

        // Assert
        var request = _mockHandler.LastRequest;
        Assert.NotNull(request);
        Assert.Equal(HttpMethod.Get, request!.Method);
        Assert.Contains("tendlc/brands/brd_123/qualify/MIXED", request.RequestUri?.ToString());
    }

    [Fact]
    public async Task QualifyAsync_WithEmptyBrandId_ThrowsValidationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _client.TenDlc.QualifyAsync("", "MIXED"));
    }

    [Fact]
    public async Task QualifyAsync_WithEmptyUseCase_ThrowsValidationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _client.TenDlc.QualifyAsync("brd_123", ""));
    }

    #endregion

    #region ListCampaignsAsync Tests

    [Fact]
    public async Task ListCampaignsAsync_ReturnsCampaigns()
    {
        // Arrange
        var responseJson = @"{
            ""data"": [
                {
                    ""id"": ""cmp_123"",
                    ""brandId"": ""brd_123"",
                    ""useCase"": ""MIXED"",
                    ""subUseCases"": [""ACCOUNT_NOTIFICATION""],
                    ""description"": ""Order updates"",
                    ""status"": ""active"",
                    ""sampleMessages"": [""Your order 123 has shipped""],
                    ""throughput"": { ""tier"": ""Standard"", ""carriersReady"": 4 },
                    ""failureReasons"": null,
                    ""createdAt"": ""2026-06-01T10:00:00Z"",
                    ""updatedAt"": ""2026-06-02T10:00:00Z""
                }
            ]
        }";
        _mockHandler.QueueSuccessResponse(responseJson);

        // Act
        var result = await _client.TenDlc.ListCampaignsAsync();

        // Assert
        Assert.Single(result.Data);
        Assert.Equal("cmp_123", result.Data[0].Id);
        Assert.Equal("brd_123", result.Data[0].BrandId);
        Assert.Equal("MIXED", result.Data[0].UseCase);
        Assert.Single(result.Data[0].SubUseCases);
        Assert.Equal("active", result.Data[0].Status);
        Assert.Single(result.Data[0].SampleMessages);
        Assert.NotNull(result.Data[0].Throughput);
    }

    [Fact]
    public async Task ListCampaignsAsync_HitsCorrectPath()
    {
        // Arrange
        _mockHandler.QueueSuccessResponse(@"{""data"": []}");

        // Act
        await _client.TenDlc.ListCampaignsAsync();

        // Assert
        var request = _mockHandler.LastRequest;
        Assert.NotNull(request);
        Assert.Equal(HttpMethod.Get, request!.Method);
        Assert.Contains("tendlc/campaigns", request.RequestUri?.ToString());
    }

    #endregion

    #region CreateCampaignAsync Tests

    [Fact]
    public async Task CreateCampaignAsync_ReturnsCampaign()
    {
        // Arrange
        var responseJson = @"{
            ""data"": {
                ""id"": ""cmp_123"",
                ""brandId"": ""brd_123"",
                ""useCase"": ""MIXED"",
                ""status"": ""pending"",
                ""sampleMessages"": [""Your order 123 has shipped""],
                ""createdAt"": ""2026-06-01T10:00:00Z"",
                ""updatedAt"": ""2026-06-01T10:00:00Z""
            }
        }";
        _mockHandler.QueueSuccessResponse(responseJson);

        // Act
        var result = await _client.TenDlc.CreateCampaignAsync(new CreateTenDlcCampaignRequest
        {
            BrandId = "brd_123",
            UseCase = "MIXED",
            Description = "Order updates and support replies",
            MessageFlow = "Customers opt in at checkout",
            SampleMessages = new() { "Your order 123 has shipped" }
        });

        // Assert
        Assert.Equal("cmp_123", result.Data.Id);
        Assert.Equal("pending", result.Data.Status);
    }

    [Fact]
    public async Task CreateCampaignAsync_SendsCorrectBody()
    {
        // Arrange
        _mockHandler.QueueSuccessResponse(@"{""data"": {""id"": ""cmp_123""}}");

        // Act
        await _client.TenDlc.CreateCampaignAsync(new CreateTenDlcCampaignRequest
        {
            BrandId = "brd_123",
            UseCase = "MIXED",
            Description = "Order updates and support replies",
            MessageFlow = "Customers opt in at checkout",
            SampleMessages = new() { "Your order 123 has shipped" },
            OptOutKeywords = "STOP",
            EmbeddedLink = false
        });

        // Assert
        var request = _mockHandler.LastRequest;
        Assert.NotNull(request);
        Assert.Equal(HttpMethod.Post, request!.Method);
        Assert.Contains("tendlc/campaigns", request.RequestUri?.ToString());
        var body = await request.Content!.ReadAsStringAsync();
        Assert.Contains("\"brandId\":\"brd_123\"", body);
        Assert.Contains("\"useCase\":\"MIXED\"", body);
        Assert.Contains("\"description\":\"Order updates and support replies\"", body);
        Assert.Contains("\"messageFlow\":\"Customers opt in at checkout\"", body);
        Assert.Contains("\"sampleMessages\":[\"Your order 123 has shipped\"]", body);
        Assert.Contains("\"optOutKeywords\":\"STOP\"", body);
        Assert.Contains("\"embeddedLink\":false", body);
        // null optionals should be omitted
        Assert.DoesNotContain("optInKeywords", body);
        Assert.DoesNotContain("embeddedPhone", body);
    }

    [Fact]
    public async Task CreateCampaignAsync_WithoutBrandId_ThrowsValidationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _client.TenDlc.CreateCampaignAsync(new CreateTenDlcCampaignRequest
            {
                UseCase = "MIXED",
                Description = "Order updates",
                MessageFlow = "Checkout opt-in",
                SampleMessages = new() { "Hi" }
            }));
    }

    [Fact]
    public async Task CreateCampaignAsync_WithoutUseCase_ThrowsValidationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _client.TenDlc.CreateCampaignAsync(new CreateTenDlcCampaignRequest
            {
                BrandId = "brd_123",
                Description = "Order updates",
                MessageFlow = "Checkout opt-in",
                SampleMessages = new() { "Hi" }
            }));
    }

    [Fact]
    public async Task CreateCampaignAsync_WithoutDescription_ThrowsValidationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _client.TenDlc.CreateCampaignAsync(new CreateTenDlcCampaignRequest
            {
                BrandId = "brd_123",
                UseCase = "MIXED",
                MessageFlow = "Checkout opt-in",
                SampleMessages = new() { "Hi" }
            }));
    }

    [Fact]
    public async Task CreateCampaignAsync_WithoutMessageFlow_ThrowsValidationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _client.TenDlc.CreateCampaignAsync(new CreateTenDlcCampaignRequest
            {
                BrandId = "brd_123",
                UseCase = "MIXED",
                Description = "Order updates",
                SampleMessages = new() { "Hi" }
            }));
    }

    [Fact]
    public async Task CreateCampaignAsync_WithoutSampleMessages_ThrowsValidationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _client.TenDlc.CreateCampaignAsync(new CreateTenDlcCampaignRequest
            {
                BrandId = "brd_123",
                UseCase = "MIXED",
                Description = "Order updates",
                MessageFlow = "Checkout opt-in"
            }));
    }

    #endregion

    #region GetCampaignAsync Tests

    [Fact]
    public async Task GetCampaignAsync_ReturnsCampaign()
    {
        // Arrange
        var responseJson = @"{
            ""data"": {
                ""id"": ""cmp_123"",
                ""brandId"": ""brd_123"",
                ""useCase"": ""MIXED"",
                ""status"": ""active"",
                ""sampleMessages"": [""Your order 123 has shipped""],
                ""throughput"": { ""tier"": ""High volume"", ""carriersReady"": 5 },
                ""createdAt"": ""2026-06-01T10:00:00Z"",
                ""updatedAt"": ""2026-06-05T10:00:00Z""
            }
        }";
        _mockHandler.QueueSuccessResponse(responseJson);

        // Act
        var result = await _client.TenDlc.GetCampaignAsync("cmp_123");

        // Assert
        Assert.Equal("cmp_123", result.Data.Id);
        Assert.Equal("active", result.Data.Status);
        Assert.NotNull(result.Data.Throughput);
        Assert.Equal("High volume", result.Data.Throughput!.Tier);
        Assert.Equal(5, result.Data.Throughput.CarriersReady);
    }

    [Fact]
    public async Task GetCampaignAsync_HitsCorrectPath()
    {
        // Arrange
        _mockHandler.QueueSuccessResponse(@"{""data"": {""id"": ""cmp_123""}}");

        // Act
        await _client.TenDlc.GetCampaignAsync("cmp_123");

        // Assert
        var request = _mockHandler.LastRequest;
        Assert.NotNull(request);
        Assert.Equal(HttpMethod.Get, request!.Method);
        Assert.Contains("tendlc/campaigns/cmp_123", request.RequestUri?.ToString());
    }

    [Fact]
    public async Task GetCampaignAsync_WithEmptyId_ThrowsValidationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _client.TenDlc.GetCampaignAsync(""));
    }

    #endregion

    #region AssignNumberAsync Tests

    [Fact]
    public async Task AssignNumberAsync_ReturnsAssignment()
    {
        // Arrange
        var responseJson = @"{
            ""data"": {
                ""id"": ""asg_123"",
                ""campaignId"": ""cmp_123"",
                ""phoneNumber"": ""+15551234567"",
                ""status"": ""Under review"",
                ""assignedAt"": null
            }
        }";
        _mockHandler.QueueSuccessResponse(responseJson);

        // Act
        var result = await _client.TenDlc.AssignNumberAsync("cmp_123", "+15551234567");

        // Assert
        Assert.Equal("asg_123", result.Data.Id);
        Assert.Equal("cmp_123", result.Data.CampaignId);
        Assert.Equal("+15551234567", result.Data.PhoneNumber);
        Assert.Equal("Under review", result.Data.Status);
        Assert.Null(result.Data.AssignedAt);
    }

    [Fact]
    public async Task AssignNumberAsync_SendsCorrectBody()
    {
        // Arrange
        _mockHandler.QueueSuccessResponse(@"{""data"": {""id"": ""asg_123""}}");

        // Act
        await _client.TenDlc.AssignNumberAsync("cmp_123", "+15551234567");

        // Assert
        var request = _mockHandler.LastRequest;
        Assert.NotNull(request);
        Assert.Equal(HttpMethod.Post, request!.Method);
        Assert.Contains("tendlc/campaigns/cmp_123/assign", request.RequestUri?.ToString());
        var body = await request.Content!.ReadAsStringAsync();
        // System.Text.Json escapes '+' as + by default (HTML-safe encoder),
        // matching the rest of the SDK's serialization behavior.
        Assert.Contains("\"phoneNumber\":\"\\u002B15551234567\"", body);
    }

    [Fact]
    public async Task AssignNumberAsync_WithEmptyCampaignId_ThrowsValidationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _client.TenDlc.AssignNumberAsync("", "+15551234567"));
    }

    [Fact]
    public async Task AssignNumberAsync_WithEmptyPhoneNumber_ThrowsValidationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _client.TenDlc.AssignNumberAsync("cmp_123", ""));
    }

    #endregion

    #region ListAssignmentsAsync Tests

    [Fact]
    public async Task ListAssignmentsAsync_ReturnsAssignments()
    {
        // Arrange
        var responseJson = @"{
            ""data"": [
                {
                    ""id"": ""asg_123"",
                    ""campaignId"": ""cmp_123"",
                    ""phoneNumber"": ""+15551234567"",
                    ""status"": ""Active"",
                    ""assignedAt"": ""2026-06-05T10:00:00Z""
                }
            ]
        }";
        _mockHandler.QueueSuccessResponse(responseJson);

        // Act
        var result = await _client.TenDlc.ListAssignmentsAsync();

        // Assert
        Assert.Single(result.Data);
        Assert.Equal("asg_123", result.Data[0].Id);
        Assert.Equal("cmp_123", result.Data[0].CampaignId);
        Assert.Equal("+15551234567", result.Data[0].PhoneNumber);
        Assert.Equal("Active", result.Data[0].Status);
        Assert.Equal("2026-06-05T10:00:00Z", result.Data[0].AssignedAt);
    }

    [Fact]
    public async Task ListAssignmentsAsync_HitsCorrectPath()
    {
        // Arrange
        _mockHandler.QueueSuccessResponse(@"{""data"": []}");

        // Act
        await _client.TenDlc.ListAssignmentsAsync();

        // Assert
        var request = _mockHandler.LastRequest;
        Assert.NotNull(request);
        Assert.Equal(HttpMethod.Get, request!.Method);
        Assert.Contains("tendlc/assignments", request.RequestUri?.ToString());
    }

    #endregion
}
