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
/// Tests for CallsResource - Create, List, Get, Hangup and Recording.
/// </summary>
public class CallsResourceTests : IDisposable
{
    private const string AutoKeyPattern =
        @"^sendly-dotnet-retry-[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$";

    private const string CallId = "6f1c2d3e-4a5b-4c6d-8e9f-0a1b2c3d4e5f";
    private const string AgentId = "3c4d5e6f-7081-4293-a4b5-c6d7e8f90a1b";

    private const string RingingCallJson = @"{
        ""id"": ""6f1c2d3e-4a5b-4c6d-8e9f-0a1b2c3d4e5f"",
        ""object"": ""call"",
        ""kind"": ""pstn"",
        ""direction"": ""outbound"",
        ""status"": ""ringing"",
        ""handledBy"": ""agent"",
        ""agentId"": ""3c4d5e6f-7081-4293-a4b5-c6d7e8f90a1b"",
        ""from"": ""+15555550188"",
        ""to"": ""+15555550123"",
        ""callerName"": ""Front Desk"",
        ""calleeName"": ""+15555550123"",
        ""startedAt"": ""2026-09-12T14:03:11.000Z"",
        ""answeredAt"": null,
        ""endedAt"": null,
        ""durationSecs"": 0,
        ""creditsCharged"": 0,
        ""billing"": ""metered"",
        ""hangupClass"": null,
        ""recordingStatus"": null,
        ""metadata"": { ""crmId"": ""lead_8812"" }
    }";

    private const string CompletedAgentCallJson = @"{
        ""id"": ""6f1c2d3e-4a5b-4c6d-8e9f-0a1b2c3d4e5f"",
        ""object"": ""call"",
        ""kind"": ""pstn"",
        ""direction"": ""outbound"",
        ""status"": ""completed"",
        ""handledBy"": ""agent"",
        ""agentId"": ""3c4d5e6f-7081-4293-a4b5-c6d7e8f90a1b"",
        ""from"": ""+15555550188"",
        ""to"": ""+15555550123"",
        ""callerName"": ""Front Desk"",
        ""calleeName"": ""+15555550123"",
        ""startedAt"": ""2026-09-12T14:03:11.000Z"",
        ""answeredAt"": ""2026-09-12T14:03:19.000Z"",
        ""endedAt"": ""2026-09-12T14:05:02.000Z"",
        ""durationSecs"": 103,
        ""creditsCharged"": 20,
        ""billing"": ""settled"",
        ""hangupClass"": ""agent_agent_hangup"",
        ""recordingStatus"": ""ready"",
        ""metadata"": { ""crmId"": ""lead_8812"" },
        ""transcript"": [
            { ""speaker"": ""agent"", ""text"": ""Hi Jordan, this is the front desk confirming Tuesday at 3pm."", ""atMs"": 1200 },
            { ""speaker"": ""caller"", ""text"": ""Yes, that works."", ""atMs"": 6400 }
        ]
    }";

    private const string InboundDashboardCallJson = @"{
        ""id"": ""0a1b2c3d-4e5f-4a6b-8c7d-9e0f1a2b3c4d"",
        ""object"": ""call"",
        ""kind"": ""pstn"",
        ""direction"": ""inbound"",
        ""status"": ""completed"",
        ""handledBy"": ""dashboard"",
        ""agentId"": null,
        ""from"": ""+15555550177"",
        ""to"": ""+15555550188"",
        ""callerName"": null,
        ""calleeName"": ""Front Desk"",
        ""startedAt"": ""2026-09-12T09:00:00.000Z"",
        ""answeredAt"": ""2026-09-12T09:00:06.000Z"",
        ""endedAt"": ""2026-09-12T09:01:30.000Z"",
        ""durationSecs"": 84,
        ""creditsCharged"": 4,
        ""billing"": ""settled"",
        ""hangupClass"": ""caller_hung_up"",
        ""recordingStatus"": null,
        ""metadata"": {}
    }";

    private readonly MockHttpMessageHandler _mockHandler;
    private readonly HttpClient _httpClient;
    private readonly SendlyClient _client;

    public CallsResourceTests()
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

    private static CreateCallRequest MinimalCreate() => new()
    {
        To = "+15555550123",
        AgentId = AgentId,
    };

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_PostsToCallsWithAutoIdempotencyKey()
    {
        _mockHandler.QueueResponse(HttpStatusCode.Created, RingingCallJson);

        var call = await _client.Calls.CreateAsync(MinimalCreate());

        var request = _mockHandler.LastRequest!;
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("https://api.test.com/calls", request.RequestUri!.ToString());
        Assert.Matches(AutoKeyPattern, KeyOfLastRequest());

        Assert.Equal(CallId, call.Id);
        Assert.Equal("call", call.Object);
        Assert.Equal(CallStatus.Ringing, call.Status);
        Assert.Equal(CallHandledBy.Agent, call.HandledBy);
        Assert.Equal(CallBilling.Metered, call.Billing);
        Assert.Equal(0, call.CreditsCharged);
        Assert.Equal(AgentId, call.AgentId);
        Assert.Equal("+15555550188", call.From);
        Assert.Equal("+15555550123", call.To);
        Assert.Null(call.AnsweredAt);
        Assert.Null(call.HangupClass);
        Assert.Null(call.RecordingStatus);
        Assert.Null(call.Transcript);
        Assert.Equal("lead_8812", call.Metadata["crmId"]);
    }

    [Fact]
    public async Task CreateAsync_SendsOnlyRequiredKeysWhenOptionalsAreUnset()
    {
        _mockHandler.QueueResponse(HttpStatusCode.Created, RingingCallJson);

        await _client.Calls.CreateAsync(MinimalCreate());

        var body = await JsonBodyOfLastRequest();
        Assert.Equal("+15555550123", body.GetProperty("to").GetString());
        Assert.Equal(AgentId, body.GetProperty("agentId").GetString());
        Assert.False(body.TryGetProperty("from", out _));
        Assert.False(body.TryGetProperty("context", out _));
        Assert.False(body.TryGetProperty("metadata", out _));
        Assert.False(body.TryGetProperty("agent_id", out _));
    }

    [Fact]
    public async Task CreateAsync_SendsFromContextAndMetadataUnderWireKeys()
    {
        _mockHandler.QueueResponse(HttpStatusCode.Created, RingingCallJson);

        await _client.Calls.CreateAsync(new CreateCallRequest
        {
            To = "+15555550123",
            AgentId = AgentId,
            From = "+15555550188",
            Context = "You are calling Jordan to confirm the 3pm appointment on Tuesday.",
            Metadata = new Dictionary<string, string> { ["crmId"] = "lead_8812", ["source.ref"] = "q3" },
        });

        var body = await JsonBodyOfLastRequest();
        Assert.Equal("+15555550188", body.GetProperty("from").GetString());
        Assert.Equal("You are calling Jordan to confirm the 3pm appointment on Tuesday.", body.GetProperty("context").GetString());
        var metadata = body.GetProperty("metadata");
        Assert.Equal("lead_8812", metadata.GetProperty("crmId").GetString());
        Assert.Equal("q3", metadata.GetProperty("source.ref").GetString());
        Assert.Equal(2, metadata.EnumerateObject().Count());
    }

    [Fact]
    public async Task CreateAsync_WithCallerKey_SendsItVerbatim()
    {
        _mockHandler.QueueResponse(HttpStatusCode.Created, RingingCallJson);

        await _client.Calls.CreateAsync(MinimalCreate(), new IdempotentRequestOptions { IdempotencyKey = "confirm-jordan-tue" });

        Assert.Equal("confirm-jordan-tue", KeyOfLastRequest());
    }

    [Fact]
    public async Task CreateAsync_WithoutTo_ThrowsValidationExceptionBeforeAnyRequest()
    {
        await Assert.ThrowsAsync<ValidationException>(
            () => _client.Calls.CreateAsync(new CreateCallRequest { AgentId = AgentId }));
        Assert.Empty(_mockHandler.Requests);
    }

    [Fact]
    public async Task CreateAsync_WithoutAgentId_ThrowsValidationExceptionBeforeAnyRequest()
    {
        await Assert.ThrowsAsync<ValidationException>(
            () => _client.Calls.CreateAsync(new CreateCallRequest { To = "+15555550123" }));
        Assert.Empty(_mockHandler.Requests);
    }

    [Fact]
    public async Task CreateAsync_WithNullRequest_ThrowsValidationException()
    {
        await Assert.ThrowsAsync<ValidationException>(
            () => _client.Calls.CreateAsync(null!));
    }

    [Fact]
    public async Task CreateAsync_On402_ThrowsInsufficientCreditsExceptionWithApiErrorCode()
    {
        _mockHandler.QueueResponse(HttpStatusCode.PaymentRequired,
            @"{""error"": ""insufficient_credits"", ""message"": ""Calls cost 10 credits a minute. Current balance: 4."", ""creditsNeeded"": 10, ""currentBalance"": 4}");

        var exception = await Assert.ThrowsAsync<InsufficientCreditsException>(
            () => _client.Calls.CreateAsync(MinimalCreate()));

        Assert.Equal("insufficient_credits", exception.ApiErrorCode);
        Assert.Equal("Calls cost 10 credits a minute. Current balance: 4.", exception.Message);
    }

    [Fact]
    public async Task CreateAsync_On428_ThrowsSendlyExceptionWithE911Code()
    {
        _mockHandler.QueueResponse((HttpStatusCode)428,
            @"{""error"": ""e911_required"", ""message"": ""Register an emergency address for this number before placing calls. It's required by US law.""}");

        var exception = await Assert.ThrowsAsync<SendlyException>(
            () => _client.Calls.CreateAsync(MinimalCreate()));

        Assert.Equal(428, exception.StatusCode);
        Assert.Equal(CallErrorCode.E911Required, exception.ApiErrorCode);
    }

    [Fact]
    public async Task CreateAsync_On409LinesBusy_ThrowsSendlyExceptionWithCode()
    {
        _mockHandler.QueueResponse(HttpStatusCode.Conflict,
            @"{""error"": ""lines_busy"", ""message"": ""Your workspace's lines are all in use. Try again in a moment.""}");

        var exception = await Assert.ThrowsAsync<SendlyException>(
            () => _client.Calls.CreateAsync(MinimalCreate()));

        Assert.Equal(409, exception.StatusCode);
        Assert.Equal(CallErrorCode.LinesBusy, exception.ApiErrorCode);
    }

    [Fact]
    public async Task CreateAsync_On400AgentRequired_ThrowsValidationExceptionWithCode()
    {
        _mockHandler.QueueResponse(HttpStatusCode.BadRequest,
            @"{""error"": ""agent_required"", ""message"": ""Calls placed over the API are answered by an AI agent. Pass agentId.""}");

        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _client.Calls.CreateAsync(MinimalCreate()));

        Assert.Equal(CallErrorCode.AgentRequired, exception.ApiErrorCode);
    }

    [Fact]
    public async Task CreateAsync_On404VoiceNotEnabled_ThrowsNotFoundExceptionWithCode()
    {
        _mockHandler.QueueResponse(HttpStatusCode.NotFound,
            @"{""error"": ""voice_not_enabled"", ""message"": ""Voice is not enabled for your account.""}");

        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => _client.Calls.CreateAsync(MinimalCreate()));

        Assert.Equal(CallErrorCode.VoiceNotEnabled, exception.ApiErrorCode);
    }

    [Fact]
    public async Task CreateAsync_On403LiveKeyRequired_ThrowsSendlyExceptionWithCode()
    {
        _mockHandler.QueueResponse(HttpStatusCode.Forbidden,
            @"{""error"": ""live_key_required"", ""message"": ""Phone calls need a live API key.""}");

        var exception = await Assert.ThrowsAsync<SendlyException>(
            () => _client.Calls.CreateAsync(MinimalCreate()));

        Assert.Equal(403, exception.StatusCode);
        Assert.Equal(CallErrorCode.LiveKeyRequired, exception.ApiErrorCode);
    }

    #endregion

    #region ListAsync Tests

    [Fact]
    public async Task ListAsync_WithoutOptions_HitsCallsWithNoQuery()
    {
        _mockHandler.QueueSuccessResponse(@"{""data"": [], ""pagination"": {""total"": 0, ""limit"": 50, ""offset"": 0, ""hasMore"": false}}");

        var result = await _client.Calls.ListAsync();

        var request = _mockHandler.LastRequest!;
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal("https://api.test.com/calls", request.RequestUri!.ToString());
        Assert.Null(KeyOfLastRequest());
        Assert.Empty(result.Data);
        Assert.False(result.Pagination.HasMore);
    }

    [Fact]
    public async Task ListAsync_EncodesEveryFilterUnderItsWireKey()
    {
        _mockHandler.QueueSuccessResponse(@"{""data"": [], ""pagination"": {""total"": 0, ""limit"": 10, ""offset"": 20, ""hasMore"": false}}");

        await _client.Calls.ListAsync(new ListCallsOptions
        {
            Limit = 10,
            Offset = 20,
            Status = CallStatus.Completed,
            Direction = CallDirection.Outbound,
            Kind = CallKind.Pstn,
            AgentId = AgentId,
            To = "+15555550123",
            From = "+15555550188",
        });

        var query = _mockHandler.LastRequest!.RequestUri!.Query;
        Assert.Contains("limit=10", query);
        Assert.Contains("offset=20", query);
        Assert.Contains("status=completed", query);
        Assert.Contains("direction=outbound", query);
        Assert.Contains("kind=pstn", query);
        Assert.Contains($"agentId={AgentId}", query);
        Assert.Contains("to=%2B15555550123", query);
        Assert.Contains("from=%2B15555550188", query);
        Assert.DoesNotContain("agent_id", query);
    }

    [Fact]
    public async Task ListAsync_MapsCallsAndPagination()
    {
        _mockHandler.QueueSuccessResponse($@"{{
            ""data"": [{CompletedAgentCallJson}, {InboundDashboardCallJson}],
            ""pagination"": {{ ""total"": 132, ""limit"": 2, ""offset"": 0, ""hasMore"": true }}
        }}");

        var result = await _client.Calls.ListAsync(new ListCallsOptions { Limit = 2 });

        Assert.Equal(2, result.Data.Count);
        Assert.Equal(132, result.Pagination.Total);
        Assert.Equal(2, result.Pagination.Limit);
        Assert.Equal(0, result.Pagination.Offset);
        Assert.True(result.Pagination.HasMore);

        var agentCall = result.Data[0];
        Assert.Equal(CallStatus.Completed, agentCall.Status);
        Assert.Equal(CallHangupClass.AgentAgentHangup, agentCall.HangupClass);
        Assert.Equal(103, agentCall.DurationSecs);
        Assert.Equal(20, agentCall.CreditsCharged);
        Assert.Equal(CallBilling.Settled, agentCall.Billing);
        Assert.Equal(CallRecordingStatus.Ready, agentCall.RecordingStatus);
        Assert.Equal(new DateTime(2026, 9, 12, 14, 3, 19, DateTimeKind.Utc), agentCall.AnsweredAt!.Value.ToUniversalTime());

        var inbound = result.Data[1];
        Assert.Equal(CallDirection.Inbound, inbound.Direction);
        Assert.Equal(CallHandledBy.Dashboard, inbound.HandledBy);
        Assert.Null(inbound.AgentId);
        Assert.Null(inbound.CallerName);
        Assert.Empty(inbound.Metadata);
        Assert.Null(inbound.Transcript);
    }

    [Fact]
    public async Task ListAsync_On400InvalidRequest_ThrowsValidationException()
    {
        _mockHandler.QueueResponse(HttpStatusCode.BadRequest,
            @"{""error"": ""invalid_request"", ""message"": ""status must be one of ringing, active, completed, no_answer, busy, cancelled, declined, failed""}");

        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _client.Calls.ListAsync(new ListCallsOptions { Status = "bogus" }));

        Assert.Equal("invalid_request", exception.ApiErrorCode);
    }

    #endregion

    #region GetAsync Tests

    [Fact]
    public async Task GetAsync_HitsEscapedPathAndMapsTranscriptForAgentCall()
    {
        _mockHandler.QueueSuccessResponse(CompletedAgentCallJson);

        var call = await _client.Calls.GetAsync(CallId);

        var request = _mockHandler.LastRequest!;
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal($"https://api.test.com/calls/{CallId}", request.RequestUri!.ToString());

        Assert.NotNull(call.Transcript);
        Assert.Equal(2, call.Transcript!.Count);
        Assert.Equal("agent", call.Transcript[0].Speaker);
        Assert.Equal(1200, call.Transcript[0].AtMs);
        Assert.Equal("caller", call.Transcript[1].Speaker);
        Assert.Equal("Yes, that works.", call.Transcript[1].Text);
        Assert.Equal(new DateTime(2026, 9, 12, 14, 5, 2, DateTimeKind.Utc), call.EndedAt!.Value.ToUniversalTime());
    }

    [Fact]
    public async Task GetAsync_ForDashboardCall_LeavesTranscriptNull()
    {
        _mockHandler.QueueSuccessResponse(InboundDashboardCallJson);

        var call = await _client.Calls.GetAsync("0a1b2c3d-4e5f-4a6b-8c7d-9e0f1a2b3c4d");

        Assert.Null(call.Transcript);
        Assert.Equal(CallHangupClass.CallerHungUp, call.HangupClass);
    }

    [Fact]
    public async Task GetAsync_PercentEncodesTheId()
    {
        _mockHandler.QueueSuccessResponse(CompletedAgentCallJson);

        await _client.Calls.GetAsync("call/with space");

        Assert.EndsWith("calls/call%2Fwith%20space", _mockHandler.LastRequest!.RequestUri!.AbsoluteUri);
    }

    [Fact]
    public async Task GetAsync_WithEmptyId_ThrowsValidationException()
    {
        await Assert.ThrowsAsync<ValidationException>(() => _client.Calls.GetAsync(""));
        Assert.Empty(_mockHandler.Requests);
    }

    [Fact]
    public async Task GetAsync_On404_ThrowsNotFoundExceptionWithCallNotFound()
    {
        _mockHandler.QueueResponse(HttpStatusCode.NotFound,
            @"{""error"": ""call_not_found"", ""message"": ""No call with that id is in this workspace.""}");

        var exception = await Assert.ThrowsAsync<NotFoundException>(() => _client.Calls.GetAsync(CallId));

        Assert.Equal(CallErrorCode.CallNotFound, exception.ApiErrorCode);
        Assert.Equal("No call with that id is in this workspace.", exception.Message);
    }

    #endregion

    #region HangupAsync Tests

    [Fact]
    public async Task HangupAsync_PostsEmptyObjectWithAutoIdempotencyKey()
    {
        _mockHandler.QueueSuccessResponse(RingingCallJson.Replace(@"""status"": ""ringing""", @"""status"": ""cancelled""")
            .Replace(@"""hangupClass"": null", @"""hangupClass"": ""caller_cancelled"""));

        var call = await _client.Calls.HangupAsync(CallId);

        var request = _mockHandler.LastRequest!;
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal($"https://api.test.com/calls/{CallId}/hangup", request.RequestUri!.ToString());
        Assert.Matches(AutoKeyPattern, KeyOfLastRequest());
        var body = await JsonBodyOfLastRequest();
        Assert.Equal(JsonValueKind.Object, body.ValueKind);
        Assert.Empty(body.EnumerateObject());

        Assert.Equal(CallStatus.Cancelled, call.Status);
        Assert.Equal(CallHangupClass.CallerCancelled, call.HangupClass);
    }

    [Fact]
    public async Task HangupAsync_OnActiveCall_ReturnsCompletedNormal()
    {
        _mockHandler.QueueSuccessResponse(CompletedAgentCallJson.Replace(@"""hangupClass"": ""agent_agent_hangup""", @"""hangupClass"": ""normal"""));

        var call = await _client.Calls.HangupAsync(CallId, new IdempotentRequestOptions { IdempotencyKey = "hangup-once" });

        Assert.Equal("hangup-once", KeyOfLastRequest());
        Assert.Equal(CallStatus.Completed, call.Status);
        Assert.Equal(CallHangupClass.Normal, call.HangupClass);
    }

    [Fact]
    public async Task HangupAsync_WithEmptyId_ThrowsValidationException()
    {
        await Assert.ThrowsAsync<ValidationException>(() => _client.Calls.HangupAsync(""));
        Assert.Empty(_mockHandler.Requests);
    }

    [Fact]
    public async Task HangupAsync_On404_ThrowsNotFoundException()
    {
        _mockHandler.QueueResponse(HttpStatusCode.NotFound,
            @"{""error"": ""call_not_found"", ""message"": ""No call with that id is in this workspace.""}");

        var exception = await Assert.ThrowsAsync<NotFoundException>(() => _client.Calls.HangupAsync(CallId));

        Assert.Equal(CallErrorCode.CallNotFound, exception.ApiErrorCode);
    }

    #endregion

    #region RecordingAsync Tests

    [Fact]
    public async Task RecordingAsync_WhenReady_MapsSignedUrlAndExpiry()
    {
        _mockHandler.QueueSuccessResponse($@"{{
            ""callId"": ""{CallId}"",
            ""status"": ""ready"",
            ""url"": ""https://sendly.live/api/v1/recordings/signed/abc?sig=def"",
            ""expiresAt"": ""2026-09-12T14:10:00.000Z"",
            ""contentType"": ""audio/ogg""
        }}");

        var recording = await _client.Calls.RecordingAsync(CallId);

        var request = _mockHandler.LastRequest!;
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal($"https://api.test.com/calls/{CallId}/recording", request.RequestUri!.ToString());
        Assert.Null(KeyOfLastRequest());

        Assert.Equal(CallId, recording.CallId);
        Assert.Equal(CallRecordingStatus.Ready, recording.Status);
        Assert.Equal("https://sendly.live/api/v1/recordings/signed/abc?sig=def", recording.Url);
        Assert.Equal(new DateTime(2026, 9, 12, 14, 10, 0, DateTimeKind.Utc), recording.ExpiresAt!.Value.ToUniversalTime());
        Assert.Equal("audio/ogg", recording.ContentType);
    }

    [Fact]
    public async Task RecordingAsync_WhenNone_LeavesUrlExpiryAndContentTypeNull()
    {
        _mockHandler.QueueSuccessResponse($@"{{
            ""callId"": ""{CallId}"",
            ""status"": ""none"",
            ""url"": null,
            ""expiresAt"": null,
            ""contentType"": null
        }}");

        var recording = await _client.Calls.RecordingAsync(CallId);

        Assert.Equal(CallRecordingStatus.None, recording.Status);
        Assert.Null(recording.Url);
        Assert.Null(recording.ExpiresAt);
        Assert.Null(recording.ContentType);
    }

    [Fact]
    public async Task RecordingAsync_WhenStillRecording_LeavesUrlNull()
    {
        _mockHandler.QueueSuccessResponse($@"{{""callId"": ""{CallId}"", ""status"": ""recording"", ""url"": null, ""expiresAt"": null, ""contentType"": null}}");

        var recording = await _client.Calls.RecordingAsync(CallId);

        Assert.Equal(CallRecordingStatus.Recording, recording.Status);
        Assert.Null(recording.Url);
    }

    [Fact]
    public async Task RecordingAsync_WithEmptyId_ThrowsValidationException()
    {
        await Assert.ThrowsAsync<ValidationException>(() => _client.Calls.RecordingAsync(""));
        Assert.Empty(_mockHandler.Requests);
    }

    [Fact]
    public async Task RecordingAsync_On404_ThrowsNotFoundException()
    {
        _mockHandler.QueueResponse(HttpStatusCode.NotFound,
            @"{""error"": ""call_not_found"", ""message"": ""No call with that id is in this workspace.""}");

        var exception = await Assert.ThrowsAsync<NotFoundException>(() => _client.Calls.RecordingAsync(CallId));

        Assert.Equal(CallErrorCode.CallNotFound, exception.ApiErrorCode);
    }

    #endregion

    #region Constants

    [Fact]
    public void ErrorCodes_MatchTheApiVocabulary()
    {
        Assert.Equal("voice_not_enabled", CallErrorCode.VoiceNotEnabled);
        Assert.Equal("outbound_calls_not_enabled", CallErrorCode.OutboundCallsNotEnabled);
        Assert.Equal("agent_required", CallErrorCode.AgentRequired);
        Assert.Equal("agent_not_found", CallErrorCode.AgentNotFound);
        Assert.Equal("agent_disabled", CallErrorCode.AgentDisabled);
        Assert.Equal("invalid_metadata", CallErrorCode.InvalidMetadata);
        Assert.Equal("from_number_required", CallErrorCode.FromNumberRequired);
        Assert.Equal("no_voice_number", CallErrorCode.NoVoiceNumber);
        Assert.Equal("number_not_found", CallErrorCode.NumberNotFound);
        Assert.Equal("destination_not_supported", CallErrorCode.DestinationNotSupported);
        Assert.Equal("e911_required", CallErrorCode.E911Required);
        Assert.Equal("lines_busy", CallErrorCode.LinesBusy);
        Assert.Equal("daily_call_limit", CallErrorCode.DailyCallLimit);
        Assert.Equal("call_not_found", CallErrorCode.CallNotFound);
        Assert.Equal("live_key_required", CallErrorCode.LiveKeyRequired);
        Assert.Equal("voice_internal_error", CallErrorCode.VoiceInternalError);
    }

    #endregion
}
