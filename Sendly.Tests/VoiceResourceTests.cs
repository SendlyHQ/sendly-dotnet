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
