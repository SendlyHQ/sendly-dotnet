using System.Text.Json.Serialization;

namespace Sendly.Resources;

/// <summary>
/// Where a call is in its lifecycle. Reported as <see cref="Call.Status"/>.
/// <c>ringing</c> and <c>active</c> are live; every other value is terminal.
/// Values are plain strings so a status added later still deserializes.
/// </summary>
public static class CallStatus
{
    /// <summary>The far end is being rung; nobody has answered yet.</summary>
    public const string Ringing = "ringing";

    /// <summary>Answered and in progress.</summary>
    public const string Active = "active";

    /// <summary>Answered, then ended normally.</summary>
    public const string Completed = "completed";

    /// <summary>Rang until the deadline; nobody answered.</summary>
    public const string NoAnswer = "no_answer";

    /// <summary>The far end was busy.</summary>
    public const string Busy = "busy";

    /// <summary>Hung up by the caller before it was answered.</summary>
    public const string Cancelled = "cancelled";

    /// <summary>The far end declined the call.</summary>
    public const string Declined = "declined";

    /// <summary>The call could not be set up or was cut short by a fault.</summary>
    public const string Failed = "failed";

    /// <summary>An internal call whose media dropped and may recover.</summary>
    public const string Suspended = "suspended";
}

/// <summary>Who dialled. Reported as <see cref="Call.Direction"/>.</summary>
public static class CallDirection
{
    /// <summary>Someone called one of your numbers.</summary>
    public const string Inbound = "inbound";

    /// <summary>Your workspace placed the call.</summary>
    public const string Outbound = "outbound";
}

/// <summary>What kind of call it is. Reported as <see cref="Call.Kind"/>.</summary>
public static class CallKind
{
    /// <summary>A phone call to or from a phone number.</summary>
    public const string Pstn = "pstn";

    /// <summary>A browser-to-browser call between teammates.</summary>
    public const string Internal = "internal";
}

/// <summary>Who is on your side of the call. Reported as <see cref="Call.HandledBy"/>.</summary>
public static class CallHandledBy
{
    /// <summary>One of your AI agents.</summary>
    public const string Agent = "agent";

    /// <summary>A teammate in the dashboard.</summary>
    public const string Dashboard = "dashboard";
}

/// <summary>How the call is being charged. Reported as <see cref="Call.Billing"/>.</summary>
public static class CallBilling
{
    /// <summary>A phone call in progress, charged per started minute.</summary>
    public const string Metered = "metered";

    /// <summary>Ended; <see cref="Call.CreditsCharged"/> is final.</summary>
    public const string Settled = "settled";

    /// <summary>Never charged (internal calls, and calls from before metering).</summary>
    public const string Unbilled = "unbilled";
}

/// <summary>
/// State of a call's recording. Reported as <see cref="Call.RecordingStatus"/>
/// (null when there is no recording) and <see cref="CallRecording.Status"/>
/// (<c>none</c> when there is no recording).
/// </summary>
public static class CallRecordingStatus
{
    /// <summary>No recording for this call: recording is off, or the call was never answered.</summary>
    public const string None = "none";

    /// <summary>Still recording.</summary>
    public const string Recording = "recording";

    /// <summary>Ready to download.</summary>
    public const string Ready = "ready";

    /// <summary>The recording could not be produced.</summary>
    public const string Failed = "failed";
}

/// <summary>
/// Why a call ended. Reported as <see cref="Call.HangupClass"/> once the call
/// is terminal (null while live). Anything the API does not recognise is
/// reported as <see cref="Ended"/>.
/// </summary>
public static class CallHangupClass
{
    /// <summary>Someone hung up after talking.</summary>
    public const string Normal = "normal";

    /// <summary>The caller hung up.</summary>
    public const string CallerHungUp = "caller_hung_up";

    /// <summary>The callee hung up.</summary>
    public const string CalleeHungUp = "callee_hung_up";

    /// <summary>The caller left the call.</summary>
    public const string CallerLeft = "caller_left";

    /// <summary>The other party left the call.</summary>
    public const string PeerLeft = "peer_left";

    /// <summary>The agent ended the call.</summary>
    public const string AgentEnded = "agent_ended";

    /// <summary>The agent decided the conversation was over.</summary>
    public const string AgentAgentHangup = "agent_agent_hangup";

    /// <summary>The caller left while talking to the agent.</summary>
    public const string AgentCallerLeft = "agent_caller_left";

    /// <summary>Rang until the deadline; never connected.</summary>
    public const string RingTimeout = "ring_timeout";

    /// <summary>The callee declined; never connected.</summary>
    public const string CalleeDeclined = "callee_declined";

    /// <summary>The callee was busy; never connected.</summary>
    public const string CalleeBusy = "callee_busy";

    /// <summary>The caller cancelled before it was answered.</summary>
    public const string CallerCancelled = "caller_cancelled";

    /// <summary>The call room closed before anyone answered.</summary>
    public const string RoomClosedUnanswered = "room_closed_unanswered";

    /// <summary>The agent left before the call was answered.</summary>
    public const string AgentLeftUnanswered = "agent_left_unanswered";

    /// <summary>The caller never joined the agent's call.</summary>
    public const string AgentCallerNeverJoined = "agent_caller_never_joined";

    /// <summary>The number could not be dialled.</summary>
    public const string InvalidNumber = "invalid_number";

    /// <summary>The destination rejected the call.</summary>
    public const string DestinationRejected = "destination_rejected";

    /// <summary>Cut short at the 60-minute ceiling.</summary>
    public const string MaxDuration = "max_duration";

    /// <summary>Cut short because the workspace ran out of credits.</summary>
    public const string CreditsExhausted = "credits_exhausted";

    /// <summary>Cut short because the media path failed.</summary>
    public const string MediaAborted = "media_aborted";

    /// <summary>Cut short because a participant's connection was lost.</summary>
    public const string PeerConnectionLost = "peer_connection_lost";

    /// <summary>Cut short because the call room closed.</summary>
    public const string RoomClosed = "room_closed";

    /// <summary>Cut short because the agent left.</summary>
    public const string AgentLeft = "agent_left";

    /// <summary>The call could not be set up.</summary>
    public const string SetupFailed = "setup_failed";

    /// <summary>The agent could not be dispatched to the call.</summary>
    public const string AgentDispatchFailed = "agent_dispatch_failed";

    /// <summary>The agent could not reach the API.</summary>
    public const string AgentApiUnreachable = "agent_api_unreachable";

    /// <summary>The agent found the call already over.</summary>
    public const string AgentAlreadyEnded = "agent_already_ended";

    /// <summary>Ended for a reason the API does not classify.</summary>
    public const string Ended = "ended";
}

/// <summary>
/// The <c>error</c> codes voice endpoints answer with, as carried on
/// <see cref="Sendly.Exceptions.SendlyException.ApiErrorCode"/>.
/// <c>insufficient_credits</c>, <c>invalid_number</c>,
/// <c>rate_limit_exceeded</c> and <c>forbidden</c> are shared with the rest of
/// the API.
/// </summary>
public static class CallErrorCode
{
    /// <summary>404: voice is not enabled for the workspace yet.</summary>
    public const string VoiceNotEnabled = "voice_not_enabled";

    /// <summary>404: calls to phone numbers are not enabled for the workspace yet.</summary>
    public const string OutboundCallsNotEnabled = "outbound_calls_not_enabled";

    /// <summary>400: calls placed over the API are answered by an AI agent; pass <c>AgentId</c>.</summary>
    public const string AgentRequired = "agent_required";

    /// <summary>404: the agent does not exist in the workspace.</summary>
    public const string AgentNotFound = "agent_not_found";

    /// <summary>409: the agent is switched off.</summary>
    public const string AgentDisabled = "agent_disabled";

    /// <summary>400: <c>Metadata</c> has too many keys, or a key or value is out of range.</summary>
    public const string InvalidMetadata = "invalid_metadata";

    /// <summary>400: the workspace has several voice-enabled numbers; pass <c>From</c>.</summary>
    public const string FromNumberRequired = "from_number_required";

    /// <summary>409: no number in the workspace has voice enabled.</summary>
    public const string NoVoiceNumber = "no_voice_number";

    /// <summary>404: <c>From</c> is not a number in the workspace.</summary>
    public const string NumberNotFound = "number_not_found";

    /// <summary>400: only US and Canadian numbers can be called.</summary>
    public const string DestinationNotSupported = "destination_not_supported";

    /// <summary>428: register an emergency address for the number before placing calls.</summary>
    public const string E911Required = "e911_required";

    /// <summary>409: every line in the workspace is in use.</summary>
    public const string LinesBusy = "lines_busy";

    /// <summary>429: today's calling limit has been reached.</summary>
    public const string DailyCallLimit = "daily_call_limit";

    /// <summary>404: no call with that id is in the workspace.</summary>
    public const string CallNotFound = "call_not_found";

    /// <summary>403: placing or ending calls needs a live API key.</summary>
    public const string LiveKeyRequired = "live_key_required";

    /// <summary>500: something went wrong on Sendly's side.</summary>
    public const string VoiceInternalError = "voice_internal_error";
}

/// <summary>
/// A phone call, or a browser-to-browser call between teammates.
/// </summary>
public class Call
{
    /// <summary>Call identifier.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Always <c>call</c>.</summary>
    [JsonPropertyName("object")]
    public string Object { get; set; } = "call";

    /// <summary><c>pstn</c> or <c>internal</c>. See <see cref="CallKind"/>.</summary>
    [JsonPropertyName("kind")]
    public string Kind { get; set; } = string.Empty;

    /// <summary><c>inbound</c> or <c>outbound</c>. See <see cref="CallDirection"/>.</summary>
    [JsonPropertyName("direction")]
    public string Direction { get; set; } = string.Empty;

    /// <summary>Lifecycle state. See <see cref="CallStatus"/>.</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary><c>agent</c> or <c>dashboard</c>. See <see cref="CallHandledBy"/>.</summary>
    [JsonPropertyName("handledBy")]
    public string HandledBy { get; set; } = string.Empty;

    /// <summary>The AI agent on the call, or null when a teammate handled it.</summary>
    [JsonPropertyName("agentId")]
    public string? AgentId { get; set; }

    /// <summary>Calling number in E.164 format, or null on internal calls.</summary>
    [JsonPropertyName("from")]
    public string? From { get; set; }

    /// <summary>Called number in E.164 format, or null on internal calls.</summary>
    [JsonPropertyName("to")]
    public string? To { get; set; }

    /// <summary>Display name of the caller, or null.</summary>
    [JsonPropertyName("callerName")]
    public string? CallerName { get; set; }

    /// <summary>Display name of the callee, or null.</summary>
    [JsonPropertyName("calleeName")]
    public string? CalleeName { get; set; }

    /// <summary>When the call started ringing.</summary>
    [JsonPropertyName("startedAt")]
    public DateTime StartedAt { get; set; }

    /// <summary>When the call was answered, or null if it never was.</summary>
    [JsonPropertyName("answeredAt")]
    public DateTime? AnsweredAt { get; set; }

    /// <summary>When the call ended, or null while live.</summary>
    [JsonPropertyName("endedAt")]
    public DateTime? EndedAt { get; set; }

    /// <summary>Answered seconds; 0 until the call has ended.</summary>
    [JsonPropertyName("durationSecs")]
    public int DurationSecs { get; set; }

    /// <summary>Credits charged so far (final once <see cref="Billing"/> is <c>settled</c>).</summary>
    [JsonPropertyName("creditsCharged")]
    public int CreditsCharged { get; set; }

    /// <summary><c>metered</c>, <c>settled</c> or <c>unbilled</c>. See <see cref="CallBilling"/>.</summary>
    [JsonPropertyName("billing")]
    public string Billing { get; set; } = string.Empty;

    /// <summary>Why the call ended, or null while live. See <see cref="CallHangupClass"/>.</summary>
    [JsonPropertyName("hangupClass")]
    public string? HangupClass { get; set; }

    /// <summary><c>recording</c>, <c>ready</c>, <c>failed</c>, or null when there is no recording. See <see cref="CallRecordingStatus"/>.</summary>
    [JsonPropertyName("recordingStatus")]
    public string? RecordingStatus { get; set; }

    /// <summary>The key/value pairs attached when the call was placed; empty when none.</summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, string> Metadata { get; set; } = new();

    /// <summary>
    /// What was said, in order. Present only on <see cref="CallsResource.GetAsync"/>
    /// and only for agent-handled calls (an empty list when nothing was said);
    /// null otherwise.
    /// </summary>
    [JsonPropertyName("transcript")]
    public List<CallTranscriptLine>? Transcript { get; set; }
}

/// <summary>One line of an agent call's transcript.</summary>
public class CallTranscriptLine
{
    /// <summary><c>caller</c> or <c>agent</c>.</summary>
    [JsonPropertyName("speaker")]
    public string Speaker { get; set; } = string.Empty;

    /// <summary>What was said.</summary>
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    /// <summary>Milliseconds from the moment the call was answered.</summary>
    [JsonPropertyName("atMs")]
    public int AtMs { get; set; }
}

/// <summary>
/// Request to place a phone call handled by one of your AI agents.
/// </summary>
public class CreateCallRequest
{
    /// <summary>Number to call, in E.164 format. US and Canada only.</summary>
    [JsonPropertyName("to")]
    public string To { get; set; } = string.Empty;

    /// <summary>The AI agent that talks on the call. Required.</summary>
    [JsonPropertyName("agentId")]
    public string AgentId { get; set; } = string.Empty;

    /// <summary>
    /// A voice-enabled number in your workspace to call from. Required when
    /// the workspace has more than one voice-enabled number; with exactly one,
    /// that number is used.
    /// </summary>
    [JsonPropertyName("from")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? From { get; set; }

    /// <summary>
    /// Up to 2000 characters appended to the agent's instructions for this
    /// call only. Not echoed back.
    /// </summary>
    [JsonPropertyName("context")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Context { get; set; }

    /// <summary>
    /// Up to 20 key/value pairs stored with the call and echoed on every read
    /// and in every <c>call.*</c> webhook. Keys are 1-40 characters of
    /// letters, digits, <c>_ . : -</c>; values are strings up to 500 characters.
    /// </summary>
    [JsonPropertyName("metadata")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string>? Metadata { get; set; }
}

/// <summary>
/// Filters and pagination for <see cref="CallsResource.ListAsync"/>.
/// </summary>
public class ListCallsOptions
{
    /// <summary>Maximum number of calls to return (1-100). Defaults to 50.</summary>
    public int? Limit { get; set; }

    /// <summary>Number of calls to skip for pagination. Defaults to 0.</summary>
    public int? Offset { get; set; }

    /// <summary>Only calls with this status. See <see cref="CallStatus"/>.</summary>
    public string? Status { get; set; }

    /// <summary>Only calls in this direction. See <see cref="CallDirection"/>.</summary>
    public string? Direction { get; set; }

    /// <summary>Only calls of this kind. See <see cref="CallKind"/>.</summary>
    public string? Kind { get; set; }

    /// <summary>Only calls handled by this agent.</summary>
    public string? AgentId { get; set; }

    /// <summary>Only calls to this number (E.164, exact match).</summary>
    public string? To { get; set; }

    /// <summary>Only calls from this number (E.164, exact match).</summary>
    public string? From { get; set; }

    internal Dictionary<string, string> ToQueryParams()
    {
        var queryParams = new Dictionary<string, string>();
        if (Limit.HasValue)
            queryParams["limit"] = Limit.Value.ToString();
        if (Offset.HasValue)
            queryParams["offset"] = Offset.Value.ToString();
        if (!string.IsNullOrEmpty(Status))
            queryParams["status"] = Status;
        if (!string.IsNullOrEmpty(Direction))
            queryParams["direction"] = Direction;
        if (!string.IsNullOrEmpty(Kind))
            queryParams["kind"] = Kind;
        if (!string.IsNullOrEmpty(AgentId))
            queryParams["agentId"] = AgentId;
        if (!string.IsNullOrEmpty(To))
            queryParams["to"] = To;
        if (!string.IsNullOrEmpty(From))
            queryParams["from"] = From;
        return queryParams;
    }
}

/// <summary>Position within the full list of calls.</summary>
public class CallListPagination
{
    /// <summary>Total number of calls matching the filters.</summary>
    [JsonPropertyName("total")]
    public int Total { get; set; }

    /// <summary>The limit that was applied.</summary>
    [JsonPropertyName("limit")]
    public int Limit { get; set; }

    /// <summary>The offset that was applied.</summary>
    [JsonPropertyName("offset")]
    public int Offset { get; set; }

    /// <summary>Whether more calls follow this page.</summary>
    [JsonPropertyName("hasMore")]
    public bool HasMore { get; set; }
}

/// <summary>Response from <see cref="CallsResource.ListAsync"/>.</summary>
public class CallListResponse
{
    /// <summary>The calls, newest first.</summary>
    [JsonPropertyName("data")]
    public List<Call> Data { get; set; } = new();

    /// <summary>Where this page sits in the full list.</summary>
    [JsonPropertyName("pagination")]
    public CallListPagination Pagination { get; set; } = new();
}

/// <summary>Response from <see cref="CallsResource.RecordingAsync"/>.</summary>
public class CallRecording
{
    /// <summary>The call the recording belongs to.</summary>
    [JsonPropertyName("callId")]
    public string CallId { get; set; } = string.Empty;

    /// <summary><c>none</c>, <c>recording</c>, <c>ready</c> or <c>failed</c>. See <see cref="CallRecordingStatus"/>.</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>Signed download URL, valid for five minutes. Null unless <see cref="Status"/> is <c>ready</c>.</summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>When <see cref="Url"/> stops working. Null unless <see cref="Status"/> is <c>ready</c>.</summary>
    [JsonPropertyName("expiresAt")]
    public DateTime? ExpiresAt { get; set; }

    /// <summary><c>audio/ogg</c> when ready, else null. Agent calls are recorded dual-channel: caller left, agent right.</summary>
    [JsonPropertyName("contentType")]
    public string? ContentType { get; set; }
}
