using System.Net;
using System.Reflection;
using Sendly.Exceptions;
using Sendly.Resources;
using Sendly.Tests.Fixtures;
using Xunit;

namespace Sendly.Tests;

/// <summary>
/// Tests for NumbersResource - ListCountries, ListAvailable, List, and Buy.
/// </summary>
public class NumbersResourceTests : IDisposable
{
    private readonly MockHttpMessageHandler _mockHandler;
    private readonly HttpClient _httpClient;
    private readonly SendlyClient _client;

    public NumbersResourceTests()
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

    #region ListCountriesAsync Tests

    [Fact]
    public async Task ListCountriesAsync_ReturnsCountries()
    {
        // Arrange
        var responseJson = @"{
            ""countries"": [
                { ""code"": ""GB"", ""name"": ""United Kingdom"", ""numberTypes"": [""mobile"", ""local""] },
                { ""code"": ""US"", ""name"": ""United States"", ""numberTypes"": [""local"", ""toll_free""] }
            ]
        }";
        _mockHandler.QueueSuccessResponse(responseJson);

        // Act
        var result = await _client.Numbers.ListCountriesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Countries.Count);
        Assert.Equal("GB", result.Countries[0].Code);
        Assert.Equal("United Kingdom", result.Countries[0].Name);
        Assert.Contains("mobile", result.Countries[0].NumberTypes);
    }

    [Fact]
    public async Task ListCountriesAsync_HitsCorrectPath()
    {
        // Arrange
        _mockHandler.QueueSuccessResponse(@"{""countries"": []}");

        // Act
        await _client.Numbers.ListCountriesAsync();

        // Assert
        var request = _mockHandler.LastRequest;
        Assert.NotNull(request);
        Assert.Equal(HttpMethod.Get, request!.Method);
        Assert.Contains("numbers/countries", request.RequestUri?.ToString());
    }

    #endregion

    #region ListAvailableAsync Tests

    [Fact]
    public async Task ListAvailableAsync_ReturnsNumbers()
    {
        // Arrange
        var responseJson = @"{
            ""numbers"": [
                {
                    ""phoneNumber"": ""+447400000001"",
                    ""country"": ""GB"",
                    ""numberType"": ""mobile"",
                    ""monthlyCost"": ""2.50"",
                    ""currency"": ""GBP""
                }
            ]
        }";
        _mockHandler.QueueSuccessResponse(responseJson);

        // Act
        var result = await _client.Numbers.ListAvailableAsync(new ListAvailableNumbersOptions
        {
            Country = "GB",
            Type = "mobile"
        });

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Numbers);
        Assert.Equal("+447400000001", result.Numbers[0].PhoneNumber);
        Assert.Equal("GB", result.Numbers[0].Country);
        Assert.Equal("mobile", result.Numbers[0].NumberType);
        Assert.Equal("2.50", result.Numbers[0].MonthlyCost);
        Assert.Equal("GBP", result.Numbers[0].Currency);
    }

    [Fact]
    public async Task ListAvailableAsync_SendsCorrectQueryParameters()
    {
        // Arrange
        _mockHandler.QueueSuccessResponse(@"{""numbers"": []}");

        // Act
        await _client.Numbers.ListAvailableAsync(new ListAvailableNumbersOptions
        {
            Country = "GB",
            Type = "mobile",
            Contains = "555"
        });

        // Assert
        var request = _mockHandler.LastRequest;
        Assert.NotNull(request);
        Assert.Contains("country=GB", request!.RequestUri?.Query);
        Assert.Contains("type=mobile", request.RequestUri?.Query);
        Assert.Contains("contains=555", request.RequestUri?.Query);
    }

    [Fact]
    public async Task ListAvailableAsync_WithoutCountry_ThrowsValidationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _client.Numbers.ListAvailableAsync(new ListAvailableNumbersOptions { Type = "mobile" }));
    }

    [Fact]
    public async Task ListAvailableAsync_WithoutType_ThrowsValidationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _client.Numbers.ListAvailableAsync(new ListAvailableNumbersOptions { Country = "GB" }));
    }

    #endregion

    #region ListAsync Tests

    [Fact]
    public async Task ListAsync_ReturnsOwnedNumbers()
    {
        // Arrange
        var responseJson = @"{
            ""numbers"": [
                {
                    ""id"": ""num_123"",
                    ""phoneNumber"": ""+447400000001"",
                    ""status"": ""active"",
                    ""source"": ""purchased"",
                    ""countryCode"": ""GB"",
                    ""phoneNumberType"": ""mobile"",
                    ""monthlyCostCents"": 250
                }
            ]
        }";
        _mockHandler.QueueSuccessResponse(responseJson);

        // Act
        var result = await _client.Numbers.ListAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Numbers);
        Assert.Equal("num_123", result.Numbers[0].Id);
        Assert.Equal("+447400000001", result.Numbers[0].PhoneNumber);
        Assert.Equal("active", result.Numbers[0].Status);
        Assert.Equal("GB", result.Numbers[0].CountryCode);
        Assert.Equal("mobile", result.Numbers[0].PhoneNumberType);
        Assert.Equal(250, result.Numbers[0].MonthlyCostCents);
    }

    [Fact]
    public async Task ListAsync_With401Response_ThrowsAuthenticationException()
    {
        // Arrange
        _mockHandler.QueueResponse(HttpStatusCode.Unauthorized, @"{""message"": ""Invalid API key""}");

        // Act & Assert
        await Assert.ThrowsAsync<AuthenticationException>(() => _client.Numbers.ListAsync());
    }

    #endregion

    #region BuyAsync Tests

    [Fact]
    public async Task BuyAsync_WhenProvisioning_ReturnsNumber()
    {
        // Arrange
        var responseJson = @"{
            ""status"": ""provisioning"",
            ""number"": {
                ""id"": ""num_999"",
                ""phoneNumber"": ""+447400000001"",
                ""status"": ""provisioning"",
                ""source"": ""purchased"",
                ""countryCode"": ""GB"",
                ""phoneNumberType"": ""mobile"",
                ""monthlyCostCents"": 250
            }
        }";
        _mockHandler.QueueSuccessResponse(responseJson);

        // Act
        var result = await _client.Numbers.BuyAsync(new BuyNumberRequest
        {
            PhoneNumber = "+447400000001",
            CountryCode = "GB",
            PhoneNumberType = "mobile",
            MonthlyCost = "2.50"
        });

        // Assert
        Assert.Equal("provisioning", result.Status);
        Assert.NotNull(result.Number);
        Assert.Equal("num_999", result.Number!.Id);
        Assert.Null(result.Action);
    }

    [Fact]
    public async Task BuyAsync_WhenDocumentsRequired_ReturnsAction()
    {
        // Arrange
        var responseJson = @"{
            ""status"": ""documents_required"",
            ""requirements"": { ""docs"": [""address_proof""] },
            ""action"": {
                ""url"": ""https://sendly.live/action/abc123"",
                ""code"": ""ABC123"",
                ""expiresAt"": ""2026-06-03T10:00:00Z""
            }
        }";
        _mockHandler.QueueSuccessResponse(responseJson);

        // Act
        var result = await _client.Numbers.BuyAsync(new BuyNumberRequest
        {
            PhoneNumber = "+447400000001",
            CountryCode = "GB",
            PhoneNumberType = "mobile",
            MonthlyCost = "2.50"
        });

        // Assert
        Assert.Equal("documents_required", result.Status);
        Assert.NotNull(result.Action);
        Assert.Equal("https://sendly.live/action/abc123", result.Action!.Url);
        Assert.Equal("ABC123", result.Action.Code);
        Assert.Equal("2026-06-03T10:00:00Z", result.Action.ExpiresAt);
        Assert.NotNull(result.Requirements);
    }

    [Fact]
    public async Task BuyAsync_SendsCorrectBody()
    {
        // Arrange
        _mockHandler.QueueSuccessResponse(@"{""status"": ""provisioning""}");

        // Act
        await _client.Numbers.BuyAsync(new BuyNumberRequest
        {
            PhoneNumber = "+447400000001",
            CountryCode = "GB",
            PhoneNumberType = "mobile",
            MonthlyCost = "2.50"
        });

        // Assert
        var request = _mockHandler.LastRequest;
        Assert.NotNull(request);
        Assert.Equal(HttpMethod.Post, request!.Method);
        Assert.Contains("numbers/buy", request.RequestUri?.ToString());
        var body = await request.Content!.ReadAsStringAsync();
        // System.Text.Json escapes '+' as + by default (HTML-safe encoder),
        // matching the rest of the SDK's serialization behavior.
        Assert.Contains("\"phoneNumber\":\"\\u002B447400000001\"", body);
        Assert.Contains("\"countryCode\":\"GB\"", body);
        Assert.Contains("\"phoneNumberType\":\"mobile\"", body);
        Assert.Contains("\"monthlyCost\":\"2.50\"", body);
        // actionCode is null and should be omitted
        Assert.DoesNotContain("actionCode", body);
    }

    [Fact]
    public async Task BuyAsync_WithActionCode_IncludesItInBody()
    {
        // Arrange
        _mockHandler.QueueSuccessResponse(@"{""status"": ""provisioning""}");

        // Act
        await _client.Numbers.BuyAsync(new BuyNumberRequest
        {
            PhoneNumber = "+447400000001",
            CountryCode = "GB",
            PhoneNumberType = "mobile",
            MonthlyCost = "2.50",
            ActionCode = "ABC123"
        });

        // Assert
        var request = _mockHandler.LastRequest;
        var body = await request!.Content!.ReadAsStringAsync();
        Assert.Contains("\"actionCode\":\"ABC123\"", body);
    }

    [Fact]
    public async Task BuyAsync_WithEmptyPhoneNumber_ThrowsValidationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _client.Numbers.BuyAsync(new BuyNumberRequest { PhoneNumber = "" }));
    }

    #endregion
}
