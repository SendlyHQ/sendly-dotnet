using System.Net;
using System.Reflection;
using System.Text.Json;
using Sendly.Exceptions;
using Sendly.Models;
using Sendly.Resources;
using Sendly.Tests.Fixtures;
using Xunit;

namespace Sendly.Tests;

/// <summary>
/// Tests for VoiceResource - Numbers, Agents and Voices.
/// </summary>
public class VoiceResourceTests : IDisposable
{
    private const string AutoKeyPattern =
        @"^sendly-dotnet-retry-[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$";

    private const string NumberId = "5f0c1c2e-2a44-4d4b-9d51-0a9b0f6f4a11";
    private const string PhoneNumber = "+15555550188";
    private const string EncodedPhoneNumber = "%2B15555550188";
    private const string AgentId = "3c4d5e6f-7081-4293-a4b5-c6d7e8f90a1b";

    private const string AgentNumberJson = @"{
        ""id"": ""5f0c1c2e-2a44-4d4b-9d51-0a9b0f6f4a11"",
        ""object"": ""voice_number"",
        ""phoneNumber"": ""+15555550188"",
        ""phoneNumberType"": ""local"",
        ""countryCode"": ""US"",
        ""isDefault"": true,
        ""voiceEnabled"": true,
        ""voiceMode"": ""agent"",
        ""agentId"": ""3c4d5e6f-7081-4293-a4b5-c6d7e8f90a1b"",
        ""emergencyAddress"": {
            ""status"": ""active"",
            ""address"": { ""street"": ""500 Example Ave"", ""unit"": ""Suite 2"", ""city"": ""Austin"", ""state"": ""TX"", ""zip"": ""78701"", ""country"": ""US"" }
        },
        ""ratePerMinute"": { ""inbound"": 2, ""outbound"": 2, ""agent"": 10 }
    }";

    private const string OffNumberJson = @"{
        ""id"": ""8a9b0c1d-2e3f-4a5b-8c6d-7e8f9a0b1c2d"",
        ""object"": ""voice_number"",
        ""phoneNumber"": ""+15555550199"",
        ""phoneNumberType"": ""toll_free"",
        ""countryCode"": ""US"",
        ""isDefault"": false,
        ""voiceEnabled"": false,
        ""voiceMode"": ""none"",
        ""agentId"": null,
        ""emergencyAddress"": null,
        ""ratePerMinute"": { ""inbound"": 3, ""outbound"": 2, ""agent"": 11 }
    }";

    private const string ProvisioningNumberJson = @"{
        ""id"": ""5f0c1c2e-2a44-4d4b-9d51-0a9b0f6f4a11"",
        ""object"": ""voice_number"",
        ""phoneNumber"": ""+15555550188"",
        ""phoneNumberType"": ""local"",
        ""countryCode"": ""US"",
        ""isDefault"": true,
        ""voiceEnabled"": false,
        ""voiceMode"": ""none"",
        ""agentId"": null,
        ""emergencyAddress"": {
            ""status"": ""provisioning"",
            ""address"": { ""street"": ""500 Example Ave"", ""city"": ""Austin"", ""state"": ""TX"", ""zip"": ""78701"", ""country"": ""US"" }
        },
        ""ratePerMinute"": { ""inbound"": 2, ""outbound"": 2, ""agent"": 10 }
    }";

    private const string AgentJson = @"{
        ""id"": ""3c4d5e6f-7081-4293-a4b5-c6d7e8f90a1b"",
        ""object"": ""voice_agent"",
        ""name"": ""Front desk"",
        ""enabled"": true,
        ""voice"": ""ashley"",
        ""voiceLabel"": ""Ashley (US, warm)"",
        ""language"": ""en-US"",
        ""greeting"": ""Thanks for calling Acme, how can I help?"",
        ""instructions"": ""Answer questions about opening hours and take a message for anything else."",
        ""tools"": { ""sendSms"": true, ""transferTo"": ""+15555550142"" },
        ""canSendSms"": true,
        ""callsHandled"": 12,
        ""avgDurationSecs"": 74,
        ""createdAt"": ""2026-09-14T17:00:00.000Z"",
        ""updatedAt"": ""2026-09-14T17:05:00.000Z""
    }";

    private const string NewAgentJson = @"{
        ""id"": ""9d8c7b6a-5f4e-4d3c-8b2a-1f0e9d8c7b6a"",
        ""object"": ""voice_agent"",
        ""name"": ""After hours"",
        ""enabled"": false,
        ""voice"": ""ashley"",
        ""voiceLabel"": ""Ashley (US, warm)"",
        ""language"": ""en-US"",
        ""greeting"": """",
        ""instructions"": """",
        ""tools"": { ""sendSms"": false, ""transferTo"": null },
        ""canSendSms"": false,
        ""callsHandled"": 0,
        ""avgDurationSecs"": 0,
        ""createdAt"": ""2026-09-15T08:00:00.000Z"",
        ""updatedAt"": ""2026-09-15T08:00:00.000Z""
    }";

    private readonly MockHttpMessageHandler _mockHandler;
    private readonly HttpClient _httpClient;
    private readonly SendlyClient _client;

    public VoiceResourceTests()
    {
        _mockHandler = new MockHttpMessageHandler();
        _httpClient = new HttpClient(_mockHandler)
        {
            BaseAddress = new Uri("https://api.test.com")
        };

        _client = new SendlyClient("test_api_key", new SendlyClientOptions { MaxRetries = 0 });
        var httpClientField = typeof(SendlyClient).GetField("_httpClient", BindingFlags.NonPublic | BindingFlags.Instance);
        httpClientField?.SetValue(_client, _httpClient);
    }

    public void Dispose()
    {
        _client?.Dispose();
        _httpClient?.Dispose();
        _mockHandler?.Dispose();
    }

    private string? KeyOfLastRequest()
    {
        return _mockHandler.LastRequest!.Headers.TryGetValues("Idempotency-Key", out var values)
            ? values.FirstOrDefault()
            : null;
    }

    private async Task<JsonElement> JsonBodyOfLastRequest()
    {
        using var doc = JsonDocument.Parse(await _mockHandler.LastRequest!.Content!.ReadAsStringAsync());
        return doc.RootElement.Clone();
    }

    private static List<string> KeysOf(JsonElement element) =>
        element.EnumerateObject().Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal).ToList();

    private static EmergencyAddress Address() => new()
    {
        Street = "500 Example Ave",
        Unit = "Suite 2",
        City = "Austin",
        State = "TX",
        Zip = "78701",
    };

    [Fact]
    public void Client_ExposesVoiceNumbersAgentsAndVoices()
    {
        Assert.NotNull(_client.Voice);
        Assert.NotNull(_client.Voice.Numbers);
        Assert.NotNull(_client.Voice.Agents);
        Assert.NotNull(_client.Voice.Voices);
    }

    #region Numbers.ListAsync Tests

    [Fact]
    public async Task NumbersListAsync_GetsVoiceNumbersAndUnwrapsData()
    {
        _mockHandler.QueueSuccessResponse($@"{{ ""data"": [{AgentNumberJson}, {OffNumberJson}] }}");

        var result = await _client.Voice.Numbers.ListAsync();

        var request = _mockHandler.LastRequest!;
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal("https://api.test.com/voice/numbers", request.RequestUri!.AbsoluteUri);
        Assert.Null(KeyOfLastRequest());
        Assert.Null(request.Content);

        Assert.Equal(2, result.Data.Count);

        var agentNumber = result.Data[0];
        Assert.Equal(NumberId, agentNumber.Id);
        Assert.Equal("voice_number", agentNumber.Object);
        Assert.Equal(PhoneNumber, agentNumber.PhoneNumber);
        Assert.Equal("local", agentNumber.PhoneNumberType);
        Assert.Equal("US", agentNumber.CountryCode);
        Assert.True(agentNumber.IsDefault);
        Assert.True(agentNumber.VoiceEnabled);
        Assert.Equal(VoiceMode.Agent, agentNumber.VoiceMode);
        Assert.Equal(AgentId, agentNumber.AgentId);
        Assert.NotNull(agentNumber.EmergencyAddress);
        Assert.Equal("active", agentNumber.EmergencyAddress!.Status);
        var address = agentNumber.EmergencyAddress.Address!;
        Assert.Equal("500 Example Ave", address.Street);
        Assert.Equal("Suite 2", address.Unit);
        Assert.Equal("Austin", address.City);
        Assert.Equal("TX", address.State);
        Assert.Equal("78701", address.Zip);
        Assert.Equal("US", address.Country);
        Assert.Equal(2, agentNumber.RatePerMinute.Inbound);
        Assert.Equal(2, agentNumber.RatePerMinute.Outbound);
        Assert.Equal(10, agentNumber.RatePerMinute.Agent);

        var off = result.Data[1];
        Assert.False(off.VoiceEnabled);
        Assert.Equal(VoiceMode.None, off.VoiceMode);
        Assert.Null(off.AgentId);
        Assert.Null(off.EmergencyAddress);
        Assert.Equal(11, off.RatePerMinute.Agent);
    }

    [Fact]
    public async Task NumbersListAsync_WithNoNumbers_ReturnsEmptyData()
    {
        _mockHandler.QueueSuccessResponse(@"{ ""data"": [] }");

        var result = await _client.Voice.Numbers.ListAsync();

        Assert.Empty(result.Data);
    }

    #endregion

    #region Numbers.GetAsync Tests

    [Fact]
    public async Task NumbersGetAsync_ByE164_PercentEncodesThePlus()
    {
        _mockHandler.QueueSuccessResponse(AgentNumberJson);

        var number = await _client.Voice.Numbers.GetAsync(PhoneNumber);

        var request = _mockHandler.LastRequest!;
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal($"https://api.test.com/voice/numbers/{EncodedPhoneNumber}", request.RequestUri!.AbsoluteUri);
        Assert.Equal(PhoneNumber, number.PhoneNumber);
    }

    [Fact]
    public async Task NumbersGetAsync_ById_HitsIdPath()
    {
        _mockHandler.QueueSuccessResponse(AgentNumberJson);

        await _client.Voice.Numbers.GetAsync(NumberId);

        Assert.Equal($"https://api.test.com/voice/numbers/{NumberId}", _mockHandler.LastRequest!.RequestUri!.AbsoluteUri);
    }

    [Fact]
    public async Task NumbersGetAsync_PercentEncodesPathSeparators()
    {
        _mockHandler.QueueSuccessResponse(AgentNumberJson);

        await _client.Voice.Numbers.GetAsync("../agents");

        Assert.EndsWith("voice/numbers/..%2Fagents", _mockHandler.LastRequest!.RequestUri!.AbsoluteUri);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task NumbersGetAsync_WithEmptyNumber_ThrowsValidationExceptionBeforeAnyRequest(string? number)
    {
        await Assert.ThrowsAsync<ValidationException>(() => _client.Voice.Numbers.GetAsync(number!));
        Assert.Empty(_mockHandler.Requests);
    }

    [Fact]
    public async Task NumbersGetAsync_On404_ThrowsNotFoundExceptionWithNumberNotFound()
    {
        _mockHandler.QueueResponse(HttpStatusCode.NotFound,
            @"{""error"": ""number_not_found"", ""message"": ""This number isn't in your workspace.""}");

        var exception = await Assert.ThrowsAsync<NotFoundException>(() => _client.Voice.Numbers.GetAsync(PhoneNumber));

        Assert.Equal(CallErrorCode.NumberNotFound, exception.ApiErrorCode);
        Assert.Equal("This number isn't in your workspace.", exception.Message);
    }

    #endregion

    #region Numbers.UpdateAsync Tests

    [Fact]
    public async Task NumbersUpdateAsync_PatchesCamelCaseKeysWithAgentId()
    {
        _mockHandler.QueueSuccessResponse(AgentNumberJson);

        var number = await _client.Voice.Numbers.UpdateAsync(PhoneNumber, new UpdateVoiceNumberRequest
        {
            VoiceEnabled = true,
            VoiceMode = VoiceMode.Agent,
            AgentId = AgentId,
        });

        var request = _mockHandler.LastRequest!;
        Assert.Equal(HttpMethod.Patch, request.Method);
        Assert.Equal($"https://api.test.com/voice/numbers/{EncodedPhoneNumber}", request.RequestUri!.AbsoluteUri);

        var body = await JsonBodyOfLastRequest();
        Assert.Equal(new List<string> { "agentId", "voiceEnabled", "voiceMode" }, KeysOf(body));
        Assert.True(body.GetProperty("voiceEnabled").GetBoolean());
        Assert.Equal("agent", body.GetProperty("voiceMode").GetString());
        Assert.Equal(AgentId, body.GetProperty("agentId").GetString());
        Assert.False(body.TryGetProperty("voiceAgentId", out _));
        Assert.False(body.TryGetProperty("voice_enabled", out _));

        Assert.Equal(VoiceMode.Agent, number.VoiceMode);
        Assert.Equal(AgentId, number.AgentId);
    }

    [Fact]
    public async Task NumbersUpdateAsync_SendsOnlyTheKeysThatAreSet()
    {
        _mockHandler.QueueSuccessResponse(OffNumberJson);

        await _client.Voice.Numbers.UpdateAsync(NumberId, new UpdateVoiceNumberRequest { VoiceMode = VoiceMode.None });

        var body = await JsonBodyOfLastRequest();
        Assert.Equal(new List<string> { "voiceMode" }, KeysOf(body));
        Assert.Equal("none", body.GetProperty("voiceMode").GetString());
    }

    [Fact]
    public async Task NumbersUpdateAsync_EmptyAgentIdIsSentToClearTheAgent()
    {
        _mockHandler.QueueSuccessResponse(OffNumberJson);

        await _client.Voice.Numbers.UpdateAsync(NumberId, new UpdateVoiceNumberRequest
        {
            VoiceMode = VoiceMode.RingDashboard,
            AgentId = "",
        });

        var body = await JsonBodyOfLastRequest();
        Assert.Equal("ring_dashboard", body.GetProperty("voiceMode").GetString());
        Assert.Equal("", body.GetProperty("agentId").GetString());
    }

    [Fact]
    public async Task NumbersUpdateAsync_WithoutOptions_SendsNoIdempotencyKey()
    {
        _mockHandler.QueueSuccessResponse(AgentNumberJson);

        await _client.Voice.Numbers.UpdateAsync(PhoneNumber, new UpdateVoiceNumberRequest { VoiceEnabled = true });

        Assert.Null(KeyOfLastRequest());
    }

    [Fact]
    public async Task NumbersUpdateAsync_WithCallerKey_SendsItVerbatim()
    {
        _mockHandler.QueueSuccessResponse(AgentNumberJson);

        await _client.Voice.Numbers.UpdateAsync(PhoneNumber, new UpdateVoiceNumberRequest { VoiceEnabled = true },
            new IdempotentRequestOptions { IdempotencyKey = "front-desk-on" });

        Assert.Equal("front-desk-on", KeyOfLastRequest());
    }

    [Fact]
    public async Task NumbersUpdateAsync_WithEmptyNumber_ThrowsValidationExceptionBeforeAnyRequest()
    {
        await Assert.ThrowsAsync<ValidationException>(
            () => _client.Voice.Numbers.UpdateAsync("", new UpdateVoiceNumberRequest { VoiceEnabled = true }));
        Assert.Empty(_mockHandler.Requests);
    }

    [Fact]
    public async Task NumbersUpdateAsync_WithNullRequest_ThrowsValidationExceptionBeforeAnyRequest()
    {
        await Assert.ThrowsAsync<ValidationException>(
            () => _client.Voice.Numbers.UpdateAsync(PhoneNumber, null!));
        Assert.Empty(_mockHandler.Requests);
    }

    [Fact]
    public async Task NumbersUpdateAsync_On409AgentDisabled_ThrowsSendlyExceptionWithCode()
    {
        _mockHandler.QueueResponse(HttpStatusCode.Conflict,
            @"{""error"": ""agent_disabled"", ""message"": ""That agent is switched off. Turn it on before pointing a number at it.""}");

        var exception = await Assert.ThrowsAsync<SendlyException>(() => _client.Voice.Numbers.UpdateAsync(PhoneNumber,
            new UpdateVoiceNumberRequest { VoiceMode = VoiceMode.Agent, AgentId = AgentId }));

        Assert.Equal(409, exception.StatusCode);
        Assert.Equal(CallErrorCode.AgentDisabled, exception.ApiErrorCode);
    }

    [Fact]
    public async Task NumbersUpdateAsync_On400AgentRequired_ThrowsValidationExceptionWithCode()
    {
        _mockHandler.QueueResponse(HttpStatusCode.BadRequest,
            @"{""error"": ""agent_required"", ""message"": ""Choose an agent to answer this number.""}");

        var exception = await Assert.ThrowsAsync<ValidationException>(() => _client.Voice.Numbers.UpdateAsync(PhoneNumber,
            new UpdateVoiceNumberRequest { VoiceMode = VoiceMode.Agent }));

        Assert.Equal(CallErrorCode.AgentRequired, exception.ApiErrorCode);
    }

    [Fact]
    public async Task NumbersUpdateAsync_On502VoiceAttachFailed_ThrowsSendlyExceptionWithCode()
    {
        _mockHandler.QueueResponse(HttpStatusCode.BadGateway,
            @"{""error"": ""voice_attach_failed"", ""message"": ""Couldn't switch voice on for this number. Try again in a moment.""}");

        var exception = await Assert.ThrowsAsync<SendlyException>(() => _client.Voice.Numbers.UpdateAsync(PhoneNumber,
            new UpdateVoiceNumberRequest { VoiceEnabled = true }));

        Assert.Equal(502, exception.StatusCode);
        Assert.Equal(CallErrorCode.VoiceAttachFailed, exception.ApiErrorCode);
    }

    #endregion

    #region Numbers.RegisterEmergencyAddressAsync Tests

    [Fact]
    public async Task RegisterEmergencyAddressAsync_PostsAddressWithAutoIdempotencyKey()
    {
        _mockHandler.QueueSuccessResponse(ProvisioningNumberJson);

        var number = await _client.Voice.Numbers.RegisterEmergencyAddressAsync(PhoneNumber, Address());

        var request = _mockHandler.LastRequest!;
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal($"https://api.test.com/voice/numbers/{EncodedPhoneNumber}/emergency-address", request.RequestUri!.AbsoluteUri);
        Assert.Matches(AutoKeyPattern, KeyOfLastRequest());

        var body = await JsonBodyOfLastRequest();
        Assert.Equal(new List<string> { "city", "state", "street", "unit", "zip" }, KeysOf(body));
        Assert.Equal("500 Example Ave", body.GetProperty("street").GetString());
        Assert.Equal("Suite 2", body.GetProperty("unit").GetString());
        Assert.Equal("Austin", body.GetProperty("city").GetString());
        Assert.Equal("TX", body.GetProperty("state").GetString());
        Assert.Equal("78701", body.GetProperty("zip").GetString());

        Assert.Equal("provisioning", number.EmergencyAddress!.Status);
        Assert.Null(number.EmergencyAddress.Address!.Unit);
        Assert.Equal("US", number.EmergencyAddress.Address.Country);
    }

    [Fact]
    public async Task RegisterEmergencyAddressAsync_SendsCountryWhenSet()
    {
        _mockHandler.QueueSuccessResponse(ProvisioningNumberJson);

        await _client.Voice.Numbers.RegisterEmergencyAddressAsync(NumberId, new EmergencyAddress
        {
            Street = "100 Example St",
            City = "Toronto",
            State = "ON",
            Zip = "M5V 2T6",
            Country = "CA",
        }, new IdempotentRequestOptions { IdempotencyKey = "e911-toronto" });

        var body = await JsonBodyOfLastRequest();
        Assert.Equal(new List<string> { "city", "country", "state", "street", "zip" }, KeysOf(body));
        Assert.Equal("CA", body.GetProperty("country").GetString());
        Assert.Equal("e911-toronto", KeyOfLastRequest());
    }

    [Theory]
    [InlineData("street")]
    [InlineData("city")]
    [InlineData("state")]
    [InlineData("zip")]
    public async Task RegisterEmergencyAddressAsync_WithMissingRequiredField_ThrowsValidationExceptionBeforeAnyRequest(string field)
    {
        var address = Address();
        switch (field)
        {
            case "street": address.Street = ""; break;
            case "city": address.City = " "; break;
            case "state": address.State = null!; break;
            case "zip": address.Zip = ""; break;
        }

        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _client.Voice.Numbers.RegisterEmergencyAddressAsync(PhoneNumber, address));

        Assert.Contains($"'{field}'", exception.Message);
        Assert.Empty(_mockHandler.Requests);
    }

    [Fact]
    public async Task RegisterEmergencyAddressAsync_WithNullAddressOrEmptyNumber_ThrowsValidationException()
    {
        await Assert.ThrowsAsync<ValidationException>(
            () => _client.Voice.Numbers.RegisterEmergencyAddressAsync(PhoneNumber, null!));
        await Assert.ThrowsAsync<ValidationException>(
            () => _client.Voice.Numbers.RegisterEmergencyAddressAsync("", Address()));
        Assert.Empty(_mockHandler.Requests);
    }

    [Fact]
    public async Task RegisterEmergencyAddressAsync_On422_ExposesSuggestedAddress()
    {
        _mockHandler.QueueResponse(HttpStatusCode.UnprocessableEntity,
            @"{""error"": ""invalid_address"", ""message"": ""The address couldn't be validated."", ""suggested"": { ""street"": ""500 Example Avenue"", ""city"": ""Austin"", ""state"": ""TX"", ""zip"": ""78701"" }}");

        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _client.Voice.Numbers.RegisterEmergencyAddressAsync(PhoneNumber, Address()));

        Assert.Equal(CallErrorCode.InvalidAddress, exception.ApiErrorCode);
        Assert.NotNull(exception.ResponseBody);
        var suggested = exception.ResponseBody!.Value.GetProperty("suggested");
        Assert.Equal("500 Example Avenue", suggested.GetProperty("street").GetString());
    }

    [Fact]
    public async Task RegisterEmergencyAddressAsync_On502CarrierRefused_ThrowsSendlyExceptionWithCode()
    {
        _mockHandler.QueueResponse(HttpStatusCode.BadGateway,
            @"{""error"": ""carrier_refused"", ""message"": ""The address couldn't be registered. Try again in a moment."", ""suggested"": null}");

        var exception = await Assert.ThrowsAsync<SendlyException>(
            () => _client.Voice.Numbers.RegisterEmergencyAddressAsync(PhoneNumber, Address()));

        Assert.Equal(502, exception.StatusCode);
        Assert.Equal(CallErrorCode.CarrierRefused, exception.ApiErrorCode);
        Assert.Equal(JsonValueKind.Null, exception.ResponseBody!.Value.GetProperty("suggested").ValueKind);
    }

    #endregion

    #region Agents Tests

    [Fact]
    public async Task AgentsListAsync_GetsVoiceAgentsAndMapsEveryField()
    {
        _mockHandler.QueueSuccessResponse($@"{{ ""data"": [{AgentJson}, {NewAgentJson}] }}");

        var result = await _client.Voice.Agents.ListAsync();

        var request = _mockHandler.LastRequest!;
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal("https://api.test.com/voice/agents", request.RequestUri!.AbsoluteUri);
        Assert.Null(KeyOfLastRequest());

        Assert.Equal(2, result.Data.Count);
        var agent = result.Data[0];
        Assert.Equal(AgentId, agent.Id);
        Assert.Equal("voice_agent", agent.Object);
        Assert.Equal("Front desk", agent.Name);
        Assert.True(agent.Enabled);
        Assert.Equal("ashley", agent.Voice);
        Assert.Equal("Ashley (US, warm)", agent.VoiceLabel);
        Assert.Equal("en-US", agent.Language);
        Assert.Equal("Thanks for calling Acme, how can I help?", agent.Greeting);
        Assert.StartsWith("Answer questions", agent.Instructions);
        Assert.True(agent.Tools.SendSms);
        Assert.Equal("+15555550142", agent.Tools.TransferTo);
        Assert.True(agent.CanSendSms);
        Assert.Equal(12, agent.CallsHandled);
        Assert.Equal(74, agent.AvgDurationSecs);
        Assert.Equal(new DateTime(2026, 9, 14, 17, 0, 0, DateTimeKind.Utc), agent.CreatedAt.ToUniversalTime());
        Assert.Equal(new DateTime(2026, 9, 14, 17, 5, 0, DateTimeKind.Utc), agent.UpdatedAt.ToUniversalTime());

        var fresh = result.Data[1];
        Assert.False(fresh.Enabled);
        Assert.False(fresh.Tools.SendSms);
        Assert.Null(fresh.Tools.TransferTo);
        Assert.Equal("", fresh.Greeting);
    }

    [Fact]
    public async Task AgentsCreateAsync_PostsCamelCaseBodyWithAutoIdempotencyKey()
    {
        _mockHandler.QueueResponse(HttpStatusCode.Created, AgentJson);

        var agent = await _client.Voice.Agents.CreateAsync(new CreateVoiceAgentRequest
        {
            Name = "Front desk",
            Voice = "ashley",
            Language = "en-US",
            Greeting = "Thanks for calling Acme, how can I help?",
            Instructions = "Answer questions about opening hours and take a message for anything else.",
            Tools = new VoiceAgentToolsInput { SendSms = true, TransferTo = "+15555550142" },
        });

        var request = _mockHandler.LastRequest!;
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("https://api.test.com/voice/agents", request.RequestUri!.AbsoluteUri);
        Assert.Matches(AutoKeyPattern, KeyOfLastRequest());

        var body = await JsonBodyOfLastRequest();
        Assert.Equal(new List<string> { "greeting", "instructions", "language", "name", "tools", "voice" }, KeysOf(body));
        Assert.Equal("Front desk", body.GetProperty("name").GetString());
        var tools = body.GetProperty("tools");
        Assert.Equal(new List<string> { "sendSms", "transferTo" }, KeysOf(tools));
        Assert.True(tools.GetProperty("sendSms").GetBoolean());
        Assert.Equal("+15555550142", tools.GetProperty("transferTo").GetString());

        Assert.Equal(AgentId, agent.Id);
        Assert.True(agent.CanSendSms);
    }

    [Fact]
    public async Task AgentsCreateAsync_WithOnlyName_SendsOnlyName()
    {
        _mockHandler.QueueResponse(HttpStatusCode.Created, NewAgentJson);

        await _client.Voice.Agents.CreateAsync(new CreateVoiceAgentRequest { Name = "After hours", Enabled = false },
            new IdempotentRequestOptions { IdempotencyKey = "agent-after-hours" });

        var body = await JsonBodyOfLastRequest();
        Assert.Equal(new List<string> { "enabled", "name" }, KeysOf(body));
        Assert.False(body.GetProperty("enabled").GetBoolean());
        Assert.Equal("agent-after-hours", KeyOfLastRequest());
    }

    [Fact]
    public async Task AgentsCreateAsync_WithoutName_ThrowsValidationExceptionBeforeAnyRequest()
    {
        await Assert.ThrowsAsync<ValidationException>(
            () => _client.Voice.Agents.CreateAsync(new CreateVoiceAgentRequest { Greeting = "Hi" }));
        await Assert.ThrowsAsync<ValidationException>(
            () => _client.Voice.Agents.CreateAsync(new CreateVoiceAgentRequest { Name = "  " }));
        await Assert.ThrowsAsync<ValidationException>(
            () => _client.Voice.Agents.CreateAsync(null!));
        Assert.Empty(_mockHandler.Requests);
    }

    [Fact]
    public async Task AgentsCreateAsync_On409AgentLimit_ThrowsSendlyExceptionWithCode()
    {
        _mockHandler.QueueResponse(HttpStatusCode.Conflict,
            @"{""error"": ""agent_limit"", ""message"": ""You've reached the agent limit for this workspace.""}");

        var exception = await Assert.ThrowsAsync<SendlyException>(
            () => _client.Voice.Agents.CreateAsync(new CreateVoiceAgentRequest { Name = "One too many" }));

        Assert.Equal(409, exception.StatusCode);
        Assert.Equal(CallErrorCode.AgentLimit, exception.ApiErrorCode);
    }

    [Fact]
    public async Task AgentsGetAsync_HitsEscapedPath()
    {
        _mockHandler.QueueSuccessResponse(AgentJson);

        var agent = await _client.Voice.Agents.GetAsync(AgentId);

        var request = _mockHandler.LastRequest!;
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal($"https://api.test.com/voice/agents/{AgentId}", request.RequestUri!.AbsoluteUri);
        Assert.Equal("Front desk", agent.Name);

        _mockHandler.QueueSuccessResponse(AgentJson);
        await _client.Voice.Agents.GetAsync("agent/with space");
        Assert.EndsWith("voice/agents/agent%2Fwith%20space", _mockHandler.LastRequest!.RequestUri!.AbsoluteUri);
    }

    [Fact]
    public async Task AgentsGetAsync_WithEmptyId_ThrowsValidationException()
    {
        await Assert.ThrowsAsync<ValidationException>(() => _client.Voice.Agents.GetAsync(""));
        Assert.Empty(_mockHandler.Requests);
    }

    [Fact]
    public async Task AgentsGetAsync_On404_ThrowsNotFoundExceptionWithAgentNotFound()
    {
        _mockHandler.QueueResponse(HttpStatusCode.NotFound,
            @"{""error"": ""agent_not_found"", ""message"": ""That agent no longer exists.""}");

        var exception = await Assert.ThrowsAsync<NotFoundException>(() => _client.Voice.Agents.GetAsync(AgentId));

        Assert.Equal(CallErrorCode.AgentNotFound, exception.ApiErrorCode);
    }

    [Fact]
    public async Task AgentsUpdateAsync_PatchesOnlyTheFieldsThatAreSet()
    {
        _mockHandler.QueueSuccessResponse(AgentJson);

        await _client.Voice.Agents.UpdateAsync(AgentId, new UpdateVoiceAgentRequest
        {
            Greeting = "Thanks for calling Acme. How can I help today?",
            Tools = new VoiceAgentToolsInput { SendSms = false },
        });

        var request = _mockHandler.LastRequest!;
        Assert.Equal(HttpMethod.Patch, request.Method);
        Assert.Equal($"https://api.test.com/voice/agents/{AgentId}", request.RequestUri!.AbsoluteUri);
        Assert.Null(KeyOfLastRequest());

        var body = await JsonBodyOfLastRequest();
        Assert.Equal(new List<string> { "greeting", "tools" }, KeysOf(body));
        var tools = body.GetProperty("tools");
        Assert.Equal(new List<string> { "sendSms" }, KeysOf(tools));
        Assert.False(tools.GetProperty("sendSms").GetBoolean());
    }

    [Fact]
    public async Task AgentsUpdateAsync_EmptyTransferToIsSentToClearIt()
    {
        _mockHandler.QueueSuccessResponse(NewAgentJson);

        await _client.Voice.Agents.UpdateAsync(AgentId, new UpdateVoiceAgentRequest
        {
            Tools = new VoiceAgentToolsInput { TransferTo = "" },
        }, new IdempotentRequestOptions { IdempotencyKey = "clear-transfer" });

        var body = await JsonBodyOfLastRequest();
        Assert.Equal("", body.GetProperty("tools").GetProperty("transferTo").GetString());
        Assert.Equal("clear-transfer", KeyOfLastRequest());
    }

    [Fact]
    public async Task AgentsUpdateAsync_WithEmptyIdOrNullRequest_ThrowsValidationException()
    {
        await Assert.ThrowsAsync<ValidationException>(
            () => _client.Voice.Agents.UpdateAsync("", new UpdateVoiceAgentRequest { Name = "x" }));
        await Assert.ThrowsAsync<ValidationException>(
            () => _client.Voice.Agents.UpdateAsync(AgentId, null!));
        Assert.Empty(_mockHandler.Requests);
    }

    [Fact]
    public async Task AgentsDeleteAsync_DeletesAndReturnsConfirmation()
    {
        _mockHandler.QueueSuccessResponse($@"{{""id"": ""{AgentId}"", ""object"": ""voice_agent"", ""deleted"": true}}");

        var result = await _client.Voice.Agents.DeleteAsync(AgentId);

        var request = _mockHandler.LastRequest!;
        Assert.Equal(HttpMethod.Delete, request.Method);
        Assert.Equal($"https://api.test.com/voice/agents/{AgentId}", request.RequestUri!.AbsoluteUri);
        Assert.Null(KeyOfLastRequest());
        Assert.Null(request.Content);

        Assert.Equal(AgentId, result.Id);
        Assert.Equal("voice_agent", result.Object);
        Assert.True(result.Deleted);
    }

    [Fact]
    public async Task AgentsDeleteAsync_WithCallerKey_SendsItVerbatim()
    {
        _mockHandler.QueueSuccessResponse($@"{{""id"": ""{AgentId}"", ""object"": ""voice_agent"", ""deleted"": true}}");

        await _client.Voice.Agents.DeleteAsync(AgentId, new IdempotentRequestOptions { IdempotencyKey = "remove-front-desk" });

        Assert.Equal("remove-front-desk", KeyOfLastRequest());
    }

    [Fact]
    public async Task AgentsDeleteAsync_WithEmptyId_ThrowsValidationException()
    {
        await Assert.ThrowsAsync<ValidationException>(() => _client.Voice.Agents.DeleteAsync(""));
        Assert.Empty(_mockHandler.Requests);
    }

    [Fact]
    public async Task AgentsDeleteAsync_On409AgentInUse_ThrowsSendlyExceptionListingNumbers()
    {
        _mockHandler.QueueResponse(HttpStatusCode.Conflict,
            @"{""error"": ""agent_in_use"", ""message"": ""This agent answers 2 numbers. Point them elsewhere first."", ""numbers"": [""+15555550188"", ""+15555550199""]}");

        var exception = await Assert.ThrowsAsync<SendlyException>(() => _client.Voice.Agents.DeleteAsync(AgentId));

        Assert.Equal(409, exception.StatusCode);
        Assert.Equal(CallErrorCode.AgentInUse, exception.ApiErrorCode);
        Assert.Equal("This agent answers 2 numbers. Point them elsewhere first.", exception.Message);
        var numbers = exception.ResponseBody!.Value.GetProperty("numbers").EnumerateArray().Select(n => n.GetString()).ToList();
        Assert.Equal(new List<string?> { "+15555550188", "+15555550199" }, numbers);
    }

    #endregion

    #region Voices Tests

    [Fact]
    public async Task VoicesListAsync_GetsVoicesAndMapsIdLabelLanguage()
    {
        _mockHandler.QueueSuccessResponse(@"{ ""data"": [
            { ""id"": ""ashley"", ""label"": ""Ashley (US, warm)"", ""language"": ""en"" },
            { ""id"": ""diego"", ""label"": ""Diego (Spanish, MX)"", ""language"": ""es"" }
        ] }");

        var result = await _client.Voice.Voices.ListAsync();

        var request = _mockHandler.LastRequest!;
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal("https://api.test.com/voice/voices", request.RequestUri!.AbsoluteUri);

        Assert.Equal(2, result.Data.Count);
        Assert.Equal("ashley", result.Data[0].Id);
        Assert.Equal("Ashley (US, warm)", result.Data[0].Label);
        Assert.Equal("en", result.Data[0].Language);
        Assert.Equal("es", result.Data[1].Language);
    }

    #endregion

    #region Bring Your Own Agent Tests

    private const string WebSocketAgentJson = @"{
        ""id"": ""3c4d5e6f-7081-4293-a4b5-c6d7e8f90a1b"",
        ""object"": ""voice_agent"",
        ""name"": ""Front desk"",
        ""enabled"": true,
        ""kind"": ""websocket"",
        ""connection"": {
            ""url"": ""wss://agent.example.com/calls"",
            ""audioFormat"": ""pcm16_24000"",
            ""hasAuthToken"": true,
            ""customParameters"": { ""tenant"": ""acme"" }
        },
        ""hasSigningSecret"": true,
        ""signingSecret"": ""whsec_byo_once"",
        ""voice"": ""ashley"",
        ""voiceLabel"": ""Ashley (US, warm)"",
        ""language"": ""en-US"",
        ""greeting"": """",
        ""instructions"": """",
        ""tools"": { ""sendSms"": true, ""transferTo"": null },
        ""canSendSms"": true,
        ""callsHandled"": 0,
        ""avgDurationSecs"": 0,
        ""createdAt"": ""2026-11-13T10:00:00.000Z"",
        ""updatedAt"": ""2026-11-13T10:00:00.000Z""
    }";

    [Fact]
    public async Task AgentsCreateAsync_WithCustomLlmKind_SendsKindAndConnectionAsGiven()
    {
        _mockHandler.QueueResponse(HttpStatusCode.Created, WebSocketAgentJson);

        await _client.Voice.Agents.CreateAsync(new CreateVoiceAgentRequest
        {
            Name = "Front desk",
            Kind = VoiceAgentKind.CustomLlm,
            Connection = new VoiceAgentCustomLlmConnectionInput
            {
                BaseUrl = "https://llm.example.com/v1",
                Model = "acme-receptionist",
                ApiKey = "sk_example",
                Headers = new Dictionary<string, string> { ["X-Tenant"] = "acme" },
            },
        });

        var body = await JsonBodyOfLastRequest();
        Assert.Equal(new List<string> { "connection", "kind", "name" }, KeysOf(body));
        Assert.Equal("custom_llm", body.GetProperty("kind").GetString());
        var connection = body.GetProperty("connection");
        Assert.Equal(new List<string> { "apiKey", "baseUrl", "headers", "model" }, KeysOf(connection));
        Assert.Equal("https://llm.example.com/v1", connection.GetProperty("baseUrl").GetString());
        Assert.Equal("acme-receptionist", connection.GetProperty("model").GetString());
        Assert.Equal("sk_example", connection.GetProperty("apiKey").GetString());
        Assert.Equal("acme", connection.GetProperty("headers").GetProperty("X-Tenant").GetString());
        Assert.Matches(AutoKeyPattern, KeyOfLastRequest());
    }

    [Fact]
    public async Task AgentsCreateAsync_WithSipKind_SendsOnlyTheConnectionFieldsThatAreSet()
    {
        _mockHandler.QueueResponse(HttpStatusCode.Created, WebSocketAgentJson);

        await _client.Voice.Agents.CreateAsync(new CreateVoiceAgentRequest
        {
            Name = "Front desk",
            Kind = VoiceAgentKind.Sip,
            Connection = new VoiceAgentSipConnectionInput
            {
                Uri = "sip:+15555550100@sip.example.com",
                Transport = SipTransport.Tls,
                MediaEncryption = SipMediaEncryption.Require,
                Username = "acme",
                Password = "secret",
                Headers = new Dictionary<string, string> { ["X-Account"] = "42" },
            },
        });

        var body = await JsonBodyOfLastRequest();
        Assert.Equal("sip", body.GetProperty("kind").GetString());
        var connection = body.GetProperty("connection");
        Assert.Equal(new List<string> { "headers", "mediaEncryption", "password", "transport", "uri", "username" }, KeysOf(connection));
        Assert.Equal("sip:+15555550100@sip.example.com", connection.GetProperty("uri").GetString());
        Assert.Equal("tls", connection.GetProperty("transport").GetString());
        Assert.Equal("require", connection.GetProperty("mediaEncryption").GetString());
        Assert.Equal("acme", connection.GetProperty("username").GetString());
        Assert.Equal("secret", connection.GetProperty("password").GetString());
        Assert.Equal("42", connection.GetProperty("headers").GetProperty("X-Account").GetString());
    }

    [Fact]
    public async Task AgentsCreateAsync_ReturnsTheOneTimeSigningSecretAndConnection()
    {
        _mockHandler.QueueResponse(HttpStatusCode.Created, WebSocketAgentJson);

        var agent = await _client.Voice.Agents.CreateAsync(new CreateVoiceAgentRequest
        {
            Name = "Front desk",
            Kind = VoiceAgentKind.WebSocket,
            Connection = new VoiceAgentWebSocketConnectionInput { Url = "wss://agent.example.com/calls" },
        });

        var body = await JsonBodyOfLastRequest();
        Assert.Equal("websocket", body.GetProperty("kind").GetString());
        Assert.Equal(new List<string> { "url" }, KeysOf(body.GetProperty("connection")));

        Assert.Equal(VoiceAgentKind.WebSocket, agent.Kind);
        Assert.True(agent.HasSigningSecret);
        Assert.Equal("whsec_byo_once", agent.SigningSecret);
        Assert.NotNull(agent.Connection);
        Assert.Equal("wss://agent.example.com/calls", agent.Connection!.Url);
        Assert.Equal(WebSocketAudioFormat.Pcm16At24000, agent.Connection.AudioFormat);
        Assert.True(agent.Connection.HasAuthToken);
        Assert.Equal("acme", agent.Connection.CustomParameters!["tenant"]);
        Assert.Null(agent.Connection.BaseUrl);
        Assert.Null(agent.Connection.HasApiKey);
    }

    [Fact]
    public async Task AgentsUpdateAsync_UnsetSecretsAreLeftOutAndNullSecretsAreSentAsNull()
    {
        _mockHandler.QueueSuccessResponse(WebSocketAgentJson);

        await _client.Voice.Agents.UpdateAsync(AgentId, new UpdateVoiceAgentRequest
        {
            Connection = new VoiceAgentWebSocketConnectionInput
            {
                AudioFormat = WebSocketAudioFormat.Mulaw8000,
                AuthToken = null,
            },
        });

        var body = await JsonBodyOfLastRequest();
        Assert.Equal(new List<string> { "connection" }, KeysOf(body));
        var connection = body.GetProperty("connection");
        Assert.Equal(new List<string> { "audioFormat", "authToken" }, KeysOf(connection));
        Assert.Equal("mulaw_8000", connection.GetProperty("audioFormat").GetString());
        Assert.Equal(JsonValueKind.Null, connection.GetProperty("authToken").ValueKind);
    }

    [Fact]
    public async Task AgentsUpdateAsync_SipAndCustomLlmSecretsCanBeRemovedWithNull()
    {
        _mockHandler.QueueSuccessResponse(WebSocketAgentJson);
        await _client.Voice.Agents.UpdateAsync(AgentId, new UpdateVoiceAgentRequest
        {
            Connection = new VoiceAgentSipConnectionInput { Password = null, Headers = null },
        });
        var sip = (await JsonBodyOfLastRequest()).GetProperty("connection");
        Assert.Equal(new List<string> { "headers", "password" }, KeysOf(sip));
        Assert.Equal(JsonValueKind.Null, sip.GetProperty("password").ValueKind);
        Assert.Equal(JsonValueKind.Null, sip.GetProperty("headers").ValueKind);

        _mockHandler.QueueSuccessResponse(WebSocketAgentJson);
        await _client.Voice.Agents.UpdateAsync(AgentId, new UpdateVoiceAgentRequest
        {
            Connection = new VoiceAgentCustomLlmConnectionInput { Model = "acme-2" },
        });
        var llm = (await JsonBodyOfLastRequest()).GetProperty("connection");
        Assert.Equal(new List<string> { "model" }, KeysOf(llm));
    }

    [Fact]
    public async Task AgentsUpdateAsync_RotateSigningSecret_SendsTrueAndReadsTheNewSecret()
    {
        _mockHandler.QueueSuccessResponse(WebSocketAgentJson);

        var agent = await _client.Voice.Agents.UpdateAsync(AgentId, new UpdateVoiceAgentRequest
        {
            RotateSigningSecret = true,
        });

        var request = _mockHandler.LastRequest!;
        Assert.Equal(HttpMethod.Patch, request.Method);
        Assert.Equal($"https://api.test.com/voice/agents/{AgentId}", request.RequestUri!.AbsoluteUri);
        var body = await JsonBodyOfLastRequest();
        Assert.Equal(new List<string> { "rotateSigningSecret" }, KeysOf(body));
        Assert.True(body.GetProperty("rotateSigningSecret").GetBoolean());
        Assert.Equal("whsec_byo_once", agent.SigningSecret);
    }

    [Fact]
    public async Task AgentsUpdateAsync_RotateSigningSecretFalse_IsNeverSent()
    {
        _mockHandler.QueueSuccessResponse(AgentJson);

        await _client.Voice.Agents.UpdateAsync(AgentId, new UpdateVoiceAgentRequest
        {
            Kind = VoiceAgentKind.Sendly,
            RotateSigningSecret = false,
        });

        var body = await JsonBodyOfLastRequest();
        Assert.Equal(new List<string> { "kind" }, KeysOf(body));
        Assert.Equal("sendly", body.GetProperty("kind").GetString());
    }

    [Fact]
    public async Task AgentsGetAsync_SendlyAgent_HasNullConnectionAndNoSigningSecret()
    {
        _mockHandler.QueueSuccessResponse(AgentJson.Replace(
            @"""enabled"": true,",
            @"""enabled"": true, ""kind"": ""sendly"", ""connection"": null, ""hasSigningSecret"": false,"));

        var agent = await _client.Voice.Agents.GetAsync(AgentId);

        Assert.Equal(VoiceAgentKind.Sendly, agent.Kind);
        Assert.Null(agent.Connection);
        Assert.False(agent.HasSigningSecret);
        Assert.Null(agent.SigningSecret);
    }

    [Fact]
    public async Task AgentsGetAsync_ReadsSipAndCustomLlmConnections()
    {
        _mockHandler.QueueSuccessResponse(AgentJson.Replace(
            @"""enabled"": true,",
            @"""enabled"": true, ""kind"": ""sip"", ""hasSigningSecret"": true, ""connection"": {
                ""uri"": ""sips:front@sip.example.com"", ""transport"": ""tls"", ""mediaEncryption"": ""allow"",
                ""username"": null, ""hasPassword"": false, ""headers"": { ""X-Account"": ""42"" } },"));

        var sip = await _client.Voice.Agents.GetAsync(AgentId);

        Assert.Equal(VoiceAgentKind.Sip, sip.Kind);
        Assert.Equal("sips:front@sip.example.com", sip.Connection!.Uri);
        Assert.Equal(SipTransport.Tls, sip.Connection.Transport);
        Assert.Equal(SipMediaEncryption.Allow, sip.Connection.MediaEncryption);
        Assert.Null(sip.Connection.Username);
        Assert.False(sip.Connection.HasPassword);
        Assert.Equal("42", sip.Connection.Headers!["X-Account"]);
        Assert.Null(sip.SigningSecret);

        _mockHandler.QueueSuccessResponse(AgentJson.Replace(
            @"""enabled"": true,",
            @"""enabled"": true, ""kind"": ""custom_llm"", ""hasSigningSecret"": true, ""connection"": {
                ""baseUrl"": ""https://llm.example.com/v1"", ""model"": ""acme-receptionist"",
                ""headerNames"": [""X-Tenant""], ""hasApiKey"": true },"));

        var llm = await _client.Voice.Agents.GetAsync(AgentId);

        Assert.Equal(VoiceAgentKind.CustomLlm, llm.Kind);
        Assert.Equal("https://llm.example.com/v1", llm.Connection!.BaseUrl);
        Assert.Equal("acme-receptionist", llm.Connection.Model);
        Assert.Equal(new List<string> { "X-Tenant" }, llm.Connection.HeaderNames);
        Assert.True(llm.Connection.HasApiKey);
    }

    [Fact]
    public async Task AgentsGetAsync_KeepsAnUnknownKindAsIs()
    {
        _mockHandler.QueueSuccessResponse(AgentJson.Replace(
            @"""enabled"": true,",
            @"""enabled"": true, ""kind"": ""future_kind"", ""connection"": null, ""hasSigningSecret"": false,"));

        var agent = await _client.Voice.Agents.GetAsync(AgentId);

        Assert.Equal("future_kind", agent.Kind);
    }

    [Fact]
    public async Task AgentsCreateAsync_On403ByoAgentsNotEnabled_ThrowsSendlyExceptionWithCode()
    {
        _mockHandler.QueueResponse(HttpStatusCode.Forbidden,
            @"{""error"": ""byo_agents_not_enabled"", ""message"": ""Bring your own agent is not switched on for this workspace.""}");

        var exception = await Assert.ThrowsAsync<SendlyException>(
            () => _client.Voice.Agents.CreateAsync(new CreateVoiceAgentRequest
            {
                Name = "Front desk",
                Kind = VoiceAgentKind.WebSocket,
                Connection = new VoiceAgentWebSocketConnectionInput { Url = "wss://agent.example.com/calls" },
            }));

        Assert.Equal(403, exception.StatusCode);
        Assert.Equal(CallErrorCode.ByoAgentsNotEnabled, exception.ApiErrorCode);
    }

    private const string AgentTestJson = @"{
        ""ok"": true,
        ""outcome"": ""ok"",
        ""message"": ""Your agent's server answered in 84 ms."",
        ""status"": 200,
        ""latencyMs"": 84,
        ""signed"": false,
        ""steps"": [
            { ""step"": ""address"", ""ok"": true, ""detail"": ""sip.example.com points to a public server."" },
            { ""step"": ""connect"", ""ok"": true, ""detail"": ""Connected over TLS to sip.example.com:5061 in 41 ms."" },
            { ""step"": ""reply"", ""ok"": true, ""detail"": ""SIP 200 OK"" }
        ]
    }";

    [Fact]
    public async Task AgentsTestAsync_WithoutRequest_PostsEmptyBodyWithoutIdempotencyKey()
    {
        _mockHandler.QueueSuccessResponse(AgentTestJson);

        var result = await _client.Voice.Agents.TestAsync(AgentId);

        var request = _mockHandler.LastRequest!;
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal($"https://api.test.com/voice/agents/{AgentId}/test", request.RequestUri!.AbsoluteUri);
        Assert.Null(KeyOfLastRequest());
        var body = await JsonBodyOfLastRequest();
        Assert.Equal(JsonValueKind.Object, body.ValueKind);
        Assert.Empty(KeysOf(body));

        Assert.True(result.Ok);
        Assert.Equal(VoiceAgentTestOutcome.Ok, result.Outcome);
        Assert.Equal("Your agent's server answered in 84 ms.", result.Message);
        Assert.Equal(200, result.Status);
        Assert.Equal(84, result.LatencyMs);
        Assert.False(result.Signed);
        Assert.Equal(3, result.Steps.Count);
        Assert.Equal(VoiceAgentTestStepName.Address, result.Steps[0].Step);
        Assert.True(result.Steps[0].Ok);
        Assert.Equal("sip.example.com points to a public server.", result.Steps[0].Detail);
        Assert.Equal(VoiceAgentTestStepName.Reply, result.Steps[2].Step);
    }

    [Fact]
    public async Task AgentsTestAsync_WithSettings_SendsKindConnectionAndInstructions()
    {
        _mockHandler.QueueSuccessResponse(AgentTestJson);

        await _client.Voice.Agents.TestAsync(AgentId, new TestVoiceAgentRequest
        {
            Kind = VoiceAgentKind.Sip,
            Connection = new VoiceAgentSipConnectionInput
            {
                Uri = "sip:+15125550100@sip.example.com",
                Transport = SipTransport.Tls,
            },
            Instructions = "Answer briefly.",
        });

        Assert.Null(KeyOfLastRequest());
        var body = await JsonBodyOfLastRequest();
        Assert.Equal(new List<string> { "connection", "instructions", "kind" }, KeysOf(body));
        Assert.Equal("sip", body.GetProperty("kind").GetString());
        Assert.Equal("Answer briefly.", body.GetProperty("instructions").GetString());
        var connection = body.GetProperty("connection");
        Assert.Equal(new List<string> { "transport", "uri" }, KeysOf(connection));
        Assert.Equal("sip:+15125550100@sip.example.com", connection.GetProperty("uri").GetString());
        Assert.Equal("tls", connection.GetProperty("transport").GetString());
    }

    [Fact]
    public async Task AgentsTestAsync_FailedTest_KeepsUnknownOutcomeAndStepAndNullNumbers()
    {
        _mockHandler.QueueSuccessResponse(@"{
            ""ok"": false,
            ""outcome"": ""future_outcome"",
            ""message"": ""Your agent's server answered in a way we didn't expect."",
            ""status"": null,
            ""latencyMs"": null,
            ""signed"": true,
            ""steps"": [
                { ""step"": ""address"", ""ok"": true, ""detail"": ""agent.example.com points to a public server."" },
                { ""step"": ""future_step"", ""ok"": false, ""detail"": ""Something new happened."" }
            ]
        }");

        var result = await _client.Voice.Agents.TestAsync(AgentId, new TestVoiceAgentRequest());

        var body = await JsonBodyOfLastRequest();
        Assert.Empty(KeysOf(body));
        Assert.False(result.Ok);
        Assert.Equal("future_outcome", result.Outcome);
        Assert.Null(result.Status);
        Assert.Null(result.LatencyMs);
        Assert.True(result.Signed);
        Assert.Equal("future_step", result.Steps[1].Step);
        Assert.False(result.Steps[1].Ok);
    }

    [Fact]
    public async Task AgentsTestAsync_WithEmptyId_ThrowsValidationExceptionBeforeAnyRequest()
    {
        await Assert.ThrowsAsync<ValidationException>(() => _client.Voice.Agents.TestAsync(""));
        Assert.Null(_mockHandler.LastRequest);
    }

    [Fact]
    public async Task AgentsTestAsync_On404_ThrowsNotFoundExceptionWithAgentNotFound()
    {
        _mockHandler.QueueResponse(HttpStatusCode.NotFound,
            @"{""error"": ""agent_not_found"", ""message"": ""Agent not found""}");

        var exception = await Assert.ThrowsAsync<NotFoundException>(() => _client.Voice.Agents.TestAsync(AgentId));

        Assert.Equal(CallErrorCode.AgentNotFound, exception.ApiErrorCode);
    }

    [Fact]
    public async Task AgentsTestAsync_On429_ThrowsRateLimitException()
    {
        _mockHandler.QueueResponse(HttpStatusCode.TooManyRequests,
            @"{""error"": ""rate_limit_exceeded"", ""message"": ""Too many tests. Try again in a minute.""}");

        var exception = await Assert.ThrowsAsync<RateLimitException>(() => _client.Voice.Agents.TestAsync(AgentId));

        Assert.Equal(429, exception.StatusCode);
    }

    [Fact]
    public void AgentTestVocabulary_MatchesTheApi()
    {
        Assert.Equal("ok", VoiceAgentTestOutcome.Ok);
        Assert.Equal("blocked_destination", VoiceAgentTestOutcome.BlockedDestination);
        Assert.Equal("unreachable", VoiceAgentTestOutcome.Unreachable);
        Assert.Equal("timeout", VoiceAgentTestOutcome.Timeout);
        Assert.Equal("tls_failed", VoiceAgentTestOutcome.TlsFailed);
        Assert.Equal("auth_required", VoiceAgentTestOutcome.AuthRequired);
        Assert.Equal("auth_failed", VoiceAgentTestOutcome.AuthFailed);
        Assert.Equal("not_found", VoiceAgentTestOutcome.NotFound);
        Assert.Equal("rejected", VoiceAgentTestOutcome.Rejected);
        Assert.Equal("bad_reply", VoiceAgentTestOutcome.BadReply);
        Assert.Equal("address", VoiceAgentTestStepName.Address);
        Assert.Equal("connect", VoiceAgentTestStepName.Connect);
        Assert.Equal("login", VoiceAgentTestStepName.Login);
        Assert.Equal("reply", VoiceAgentTestStepName.Reply);
    }

    [Fact]
    public void ByoVocabulary_MatchesTheApi()
    {
        Assert.Equal("sendly", VoiceAgentKind.Sendly);
        Assert.Equal("custom_llm", VoiceAgentKind.CustomLlm);
        Assert.Equal("sip", VoiceAgentKind.Sip);
        Assert.Equal("websocket", VoiceAgentKind.WebSocket);
        Assert.Equal("auto", SipTransport.Auto);
        Assert.Equal("udp", SipTransport.Udp);
        Assert.Equal("tcp", SipTransport.Tcp);
        Assert.Equal("tls", SipTransport.Tls);
        Assert.Equal("disable", SipMediaEncryption.Disable);
        Assert.Equal("allow", SipMediaEncryption.Allow);
        Assert.Equal("require", SipMediaEncryption.Require);
        Assert.Equal("pcm16_16000", WebSocketAudioFormat.Pcm16At16000);
        Assert.Equal("pcm16_24000", WebSocketAudioFormat.Pcm16At24000);
        Assert.Equal("pcm16_8000", WebSocketAudioFormat.Pcm16At8000);
        Assert.Equal("mulaw_8000", WebSocketAudioFormat.Mulaw8000);
        Assert.Equal("byo_agents_not_enabled", CallErrorCode.ByoAgentsNotEnabled);
    }

    #endregion

    #region Errors and Constants

    [Fact]
    public async Task ResponseBody_IsNullWhenTheErrorBodyIsNotJson()
    {
        _mockHandler.QueueResponse(HttpStatusCode.Conflict, "upstream conflict");

        var exception = await Assert.ThrowsAsync<SendlyException>(() => _client.Voice.Agents.DeleteAsync(AgentId));

        Assert.Null(exception.ResponseBody);
        Assert.Null(exception.ApiErrorCode);
    }

    [Fact]
    public void VoiceModes_MatchTheApiVocabulary()
    {
        Assert.Equal("none", VoiceMode.None);
        Assert.Equal("ring_dashboard", VoiceMode.RingDashboard);
        Assert.Equal("agent", VoiceMode.Agent);
    }

    [Fact]
    public void VoiceConfigErrorCodes_MatchTheApiVocabulary()
    {
        Assert.Equal("number_not_found", CallErrorCode.NumberNotFound);
        Assert.Equal("agent_required", CallErrorCode.AgentRequired);
        Assert.Equal("agent_in_use", CallErrorCode.AgentInUse);
        Assert.Equal("agent_limit", CallErrorCode.AgentLimit);
        Assert.Equal("invalid_voice_mode", CallErrorCode.InvalidVoiceMode);
        Assert.Equal("invalid_address", CallErrorCode.InvalidAddress);
        Assert.Equal("e911_not_applicable", CallErrorCode.E911NotApplicable);
        Assert.Equal("voice_attach_failed", CallErrorCode.VoiceAttachFailed);
        Assert.Equal("carrier_refused", CallErrorCode.CarrierRefused);
        Assert.Equal("voice_unavailable", CallErrorCode.VoiceUnavailable);
    }

    #endregion
}
