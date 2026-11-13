using System.Text.Json.Serialization;

namespace Sendly.Resources;

/// <summary>
/// How a number answers phone calls. Reported as <see cref="VoiceNumber.VoiceMode"/>
/// and sent as <see cref="UpdateVoiceNumberRequest.VoiceMode"/>. Values are
/// plain strings so a mode added later still deserializes.
/// </summary>
public static class VoiceMode
{
    /// <summary>
    /// Voice is off; the number does not take or place calls. Sent without
    /// <see cref="UpdateVoiceNumberRequest.VoiceEnabled"/> it switches voice
    /// off; with <c>VoiceEnabled = true</c> it becomes <c>ring_dashboard</c>.
    /// </summary>
    public const string None = "none";

    /// <summary>Calls ring the team in the dashboard.</summary>
    public const string RingDashboard = "ring_dashboard";

    /// <summary>An AI agent answers.</summary>
    public const string Agent = "agent";
}

/// <summary>
/// A street address emergency services are sent to. Returned inside
/// <see cref="VoiceNumberEmergencyAddress.Address"/> and passed to
/// <see cref="VoiceNumbersResource.RegisterEmergencyAddressAsync"/>.
/// </summary>
public class EmergencyAddress
{
    /// <summary>Street address. Required when registering.</summary>
    [JsonPropertyName("street")]
    public string Street { get; set; } = string.Empty;

    /// <summary>Apartment, suite or floor, or null when there is none.</summary>
    [JsonPropertyName("unit")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Unit { get; set; }

    /// <summary>City. Required when registering.</summary>
    [JsonPropertyName("city")]
    public string City { get; set; } = string.Empty;

    /// <summary>Two-letter state or province code, e.g. <c>TX</c>. Required when registering.</summary>
    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;

    /// <summary>
    /// Five-digit ZIP (or ZIP+4) in the US, <c>A1A 1A1</c> in Canada. Required
    /// when registering.
    /// </summary>
    [JsonPropertyName("zip")]
    public string Zip { get; set; } = string.Empty;

    /// <summary>
    /// <c>US</c> or <c>CA</c>. Always set on a registered address; leave it
    /// null when registering to use <c>US</c>.
    /// </summary>
    [JsonPropertyName("country")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Country { get; set; }
}

/// <summary>A number's emergency address registration.</summary>
public class VoiceNumberEmergencyAddress
{
    /// <summary>
    /// <c>provisioning</c> while the registration is being switched on,
    /// <c>active</c> once it is in place, otherwise the failure status as
    /// recorded.
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>The registered address, or null when none is on file.</summary>
    [JsonPropertyName("address")]
    public EmergencyAddress? Address { get; set; }
}

/// <summary>Credits charged per started minute on a number.</summary>
public class VoiceNumberRates
{
    /// <summary>An inbound call the team answers in the dashboard.</summary>
    [JsonPropertyName("inbound")]
    public int Inbound { get; set; }

    /// <summary>An outbound call (an agent on the call adds its own per-minute charge).</summary>
    [JsonPropertyName("outbound")]
    public int Outbound { get; set; }

    /// <summary>An inbound call an AI agent answers, agent included.</summary>
    [JsonPropertyName("agent")]
    public int Agent { get; set; }
}

/// <summary>A number in the workspace with its voice settings.</summary>
public class VoiceNumber
{
    /// <summary>Number identifier.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Always <c>voice_number</c>.</summary>
    [JsonPropertyName("object")]
    public string Object { get; set; } = "voice_number";

    /// <summary>The phone number in E.164 format.</summary>
    [JsonPropertyName("phoneNumber")]
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>The number's type, for example <c>local</c> or <c>toll_free</c>, or null.</summary>
    [JsonPropertyName("phoneNumberType")]
    public string? PhoneNumberType { get; set; }

    /// <summary>ISO 3166-1 alpha-2 country code, or null.</summary>
    [JsonPropertyName("countryCode")]
    public string? CountryCode { get; set; }

    /// <summary>True for the workspace's default sending number.</summary>
    [JsonPropertyName("isDefault")]
    public bool IsDefault { get; set; }

    /// <summary>True when the number takes and places phone calls.</summary>
    [JsonPropertyName("voiceEnabled")]
    public bool VoiceEnabled { get; set; }

    /// <summary>
    /// How inbound calls are answered: <c>none</c>, <c>ring_dashboard</c> or
    /// <c>agent</c>, and always <c>none</c> when <see cref="VoiceEnabled"/> is
    /// false. See <see cref="Sendly.Resources.VoiceMode"/>.
    /// </summary>
    [JsonPropertyName("voiceMode")]
    public string VoiceMode { get; set; } = string.Empty;

    /// <summary>
    /// The agent that answers when <see cref="VoiceMode"/> is <c>agent</c>. In
    /// other modes, whichever agent was last stored, or null.
    /// </summary>
    [JsonPropertyName("agentId")]
    public string? AgentId { get; set; }

    /// <summary>The emergency address registration, or null if one was never registered.</summary>
    [JsonPropertyName("emergencyAddress")]
    public VoiceNumberEmergencyAddress? EmergencyAddress { get; set; }

    /// <summary>Credits per started minute on this number.</summary>
    [JsonPropertyName("ratePerMinute")]
    public VoiceNumberRates RatePerMinute { get; set; } = new();
}

/// <summary>Response from <see cref="VoiceNumbersResource.ListAsync"/>.</summary>
public class VoiceNumberListResponse
{
    /// <summary>Active numbers in the workspace, in the same order as the dashboard.</summary>
    [JsonPropertyName("data")]
    public List<VoiceNumber> Data { get; set; } = new();
}

/// <summary>
/// Request for <see cref="VoiceNumbersResource.UpdateAsync"/>. Set only what
/// changes; properties left null are not sent.
/// </summary>
public class UpdateVoiceNumberRequest
{
    /// <summary>
    /// Switch voice on or off. Turning it on connects the number for phone
    /// calls and answers in <c>ring_dashboard</c> mode unless
    /// <see cref="VoiceMode"/> is <c>agent</c>; false switches voice off and
    /// sets the mode to <c>none</c> whatever <see cref="VoiceMode"/> says.
    /// </summary>
    [JsonPropertyName("voiceEnabled")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? VoiceEnabled { get; set; }

    /// <summary>
    /// How the number answers. On its own, <c>ring_dashboard</c> or
    /// <c>agent</c> switches voice on, so it can fail the way switching on
    /// does, and <c>none</c> switches it off. <see cref="VoiceEnabled"/> false
    /// wins over any mode, and <c>none</c> with <see cref="VoiceEnabled"/> true
    /// becomes <c>ring_dashboard</c>. See <see cref="Sendly.Resources.VoiceMode"/>.
    /// </summary>
    [JsonPropertyName("voiceMode")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? VoiceMode { get; set; }

    /// <summary>
    /// The agent that answers in <c>agent</c> mode. Required (here or already
    /// stored) when the mode is <c>agent</c>, and the agent must be switched
    /// on. An empty string clears the stored agent.
    /// </summary>
    [JsonPropertyName("agentId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AgentId { get; set; }
}

/// <summary>What an agent may do on a call.</summary>
public class VoiceAgentTools
{
    /// <summary>
    /// True when the agent may text the caller during the call. It reads the
    /// number back to the caller before sending.
    /// </summary>
    [JsonPropertyName("sendSms")]
    public bool SendSms { get; set; }

    /// <summary>
    /// A number in E.164 format the agent transfers a caller to when they ask
    /// for a person, or null when the agent cannot transfer. It is also where
    /// <see cref="CallsResource.TransferAsync"/> sends the caller when no
    /// number is given.
    /// </summary>
    [JsonPropertyName("transferTo")]
    public string? TransferTo { get; set; }
}

/// <summary>
/// Who answers an agent's calls. Reported as <see cref="VoiceAgent.Kind"/> and
/// <see cref="Call.AgentKind"/>, and sent as
/// <see cref="CreateVoiceAgentRequest.Kind"/> and
/// <see cref="UpdateVoiceAgentRequest.Kind"/>. Values are plain strings so a
/// kind added later still deserializes.
/// </summary>
public static class VoiceAgentKind
{
    /// <summary>Sendly writes the replies from the agent's instructions. The default.</summary>
    public const string Sendly = "sendly";

    /// <summary>
    /// Sendly listens and speaks, and your chat completions endpoint writes
    /// each reply. See <see cref="VoiceAgentCustomLlmConnectionInput"/>.
    /// </summary>
    public const string CustomLlm = "custom_llm";

    /// <summary>
    /// Sendly connects the caller to your agent's SIP address. Answers
    /// incoming calls only. See <see cref="VoiceAgentSipConnectionInput"/>.
    /// </summary>
    public const string Sip = "sip";

    /// <summary>
    /// Sendly streams the call audio to your <c>wss://</c> server and plays
    /// back what it sends. Answers incoming calls only. See
    /// <see cref="VoiceAgentWebSocketConnectionInput"/>.
    /// </summary>
    public const string WebSocket = "websocket";
}

/// <summary>
/// How a <c>sip</c> agent's call is carried. Reported as
/// <see cref="VoiceAgentConnection.Transport"/> and sent as
/// <see cref="VoiceAgentSipConnectionInput.Transport"/>.
/// </summary>
public static class SipTransport
{
    /// <summary>Chosen automatically.</summary>
    public const string Auto = "auto";

    /// <summary>UDP.</summary>
    public const string Udp = "udp";

    /// <summary>TCP. The default.</summary>
    public const string Tcp = "tcp";

    /// <summary>TLS. A <c>sips:</c> address always uses it.</summary>
    public const string Tls = "tls";
}

/// <summary>
/// Whether a <c>sip</c> agent's call audio is encrypted. Reported as
/// <see cref="VoiceAgentConnection.MediaEncryption"/> and sent as
/// <see cref="VoiceAgentSipConnectionInput.MediaEncryption"/>.
/// </summary>
public static class SipMediaEncryption
{
    /// <summary>Never encrypted.</summary>
    public const string Disable = "disable";

    /// <summary>Encrypted when the far end offers it. The default.</summary>
    public const string Allow = "allow";

    /// <summary>Always encrypted; needs the <c>tls</c> transport.</summary>
    public const string Require = "require";
}

/// <summary>
/// The audio format a <c>websocket</c> agent sends and receives. Reported as
/// <see cref="VoiceAgentConnection.AudioFormat"/> and sent as
/// <see cref="VoiceAgentWebSocketConnectionInput.AudioFormat"/>.
/// </summary>
public static class WebSocketAudioFormat
{
    /// <summary>16-bit PCM at 16 kHz. The default.</summary>
    public const string Pcm16At16000 = "pcm16_16000";

    /// <summary>16-bit PCM at 24 kHz.</summary>
    public const string Pcm16At24000 = "pcm16_24000";

    /// <summary>16-bit PCM at 8 kHz.</summary>
    public const string Pcm16At8000 = "pcm16_8000";

    /// <summary>Mu-law at 8 kHz.</summary>
    public const string Mulaw8000 = "mulaw_8000";
}

/// <summary>
/// How Sendly reaches your own agent, as returned on
/// <see cref="VoiceAgent.Connection"/>. Which properties are set depends on
/// <see cref="VoiceAgent.Kind"/>; the others are null. Secrets (the API key,
/// header values for <c>custom_llm</c>, the SIP password and the auth token)
/// are never returned, only whether one is saved.
/// </summary>
public class VoiceAgentConnection
{
    /// <summary><c>custom_llm</c>: your endpoint. Sendly sends <c>POST {BaseUrl}/chat/completions</c> with streaming on.</summary>
    [JsonPropertyName("baseUrl")]
    public string? BaseUrl { get; set; }

    /// <summary><c>custom_llm</c>: the model name sent in each request.</summary>
    [JsonPropertyName("model")]
    public string? Model { get; set; }

    /// <summary><c>custom_llm</c>: the names of the extra headers sent with each request. Their values are never returned.</summary>
    [JsonPropertyName("headerNames")]
    public List<string>? HeaderNames { get; set; }

    /// <summary><c>custom_llm</c>: whether an API key is saved.</summary>
    [JsonPropertyName("hasApiKey")]
    public bool? HasApiKey { get; set; }

    /// <summary><c>sip</c>: your agent's SIP address, such as <c>sip:+15555550100@sip.example.com</c>.</summary>
    [JsonPropertyName("uri")]
    public string? Uri { get; set; }

    /// <summary><c>sip</c>: how the call is carried. See <see cref="SipTransport"/>.</summary>
    [JsonPropertyName("transport")]
    public string? Transport { get; set; }

    /// <summary><c>sip</c>: whether the call audio is encrypted. See <see cref="SipMediaEncryption"/>.</summary>
    [JsonPropertyName("mediaEncryption")]
    public string? MediaEncryption { get; set; }

    /// <summary><c>sip</c>: the digest auth username, or null.</summary>
    [JsonPropertyName("username")]
    public string? Username { get; set; }

    /// <summary><c>sip</c>: whether a digest auth password is saved.</summary>
    [JsonPropertyName("hasPassword")]
    public bool? HasPassword { get; set; }

    /// <summary><c>sip</c>: the extra <c>X-</c> headers sent on the call.</summary>
    [JsonPropertyName("headers")]
    public Dictionary<string, string>? Headers { get; set; }

    /// <summary><c>websocket</c>: your <c>wss://</c> server.</summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary><c>websocket</c>: the audio format both ways. See <see cref="WebSocketAudioFormat"/>.</summary>
    [JsonPropertyName("audioFormat")]
    public string? AudioFormat { get; set; }

    /// <summary><c>websocket</c>: whether an auth token is saved. It is sent as <c>Authorization: Bearer</c>.</summary>
    [JsonPropertyName("hasAuthToken")]
    public bool? HasAuthToken { get; set; }

    /// <summary><c>websocket</c>: the values sent to your server in the <c>start</c> message.</summary>
    [JsonPropertyName("customParameters")]
    public Dictionary<string, string>? CustomParameters { get; set; }
}

/// <summary>
/// How Sendly reaches your own agent, sent as
/// <see cref="CreateVoiceAgentRequest.Connection"/>,
/// <see cref="UpdateVoiceAgentRequest.Connection"/> and
/// <see cref="TestVoiceAgentRequest.Connection"/>. Use the subclass that
/// matches the agent's kind: <see cref="VoiceAgentCustomLlmConnectionInput"/>,
/// <see cref="VoiceAgentSipConnectionInput"/> or
/// <see cref="VoiceAgentWebSocketConnectionInput"/>. Properties left unset are
/// not sent; on an update that keeps the kind, they keep their saved values.
/// Hosts must resolve to public internet addresses.
/// </summary>
[JsonDerivedType(typeof(VoiceAgentCustomLlmConnectionInput))]
[JsonDerivedType(typeof(VoiceAgentSipConnectionInput))]
[JsonDerivedType(typeof(VoiceAgentWebSocketConnectionInput))]
public abstract class VoiceAgentConnectionInput
{
}

/// <summary>
/// Connection for a <c>custom_llm</c> agent: your chat completions endpoint
/// writes each reply. Setting <see cref="ApiKey"/> or <see cref="Headers"/> to
/// null sends null, which removes the saved value; leaving them unset keeps it.
/// </summary>
public class VoiceAgentCustomLlmConnectionInput : VoiceAgentConnectionInput, IAssignedProperties
{
    private readonly HashSet<string> _assigned = new();
    private string? _apiKey;
    private Dictionary<string, string>? _headers;

    /// <summary>
    /// An <c>https://</c> URL, up to 2048 characters, with no credentials or
    /// <c>#</c>; a trailing <c>/</c> is dropped. Sendly sends
    /// <c>POST {BaseUrl}/chat/completions</c> with streaming on. Required when
    /// the agent becomes <c>custom_llm</c>.
    /// </summary>
    [JsonPropertyName("baseUrl")]
    public string? BaseUrl { get; set; }

    /// <summary>The model name sent in each request: 1-200 characters of letters, digits and <c>_ . : / @ + -</c>.</summary>
    [JsonPropertyName("model")]
    public string? Model { get; set; }

    /// <summary>
    /// Sent as <c>Authorization: Bearer</c>, 1-2048 printable ASCII
    /// characters. Unset keeps the saved key; null removes it.
    /// </summary>
    [JsonPropertyName("apiKey")]
    public string? ApiKey
    {
        get => _apiKey;
        set { _apiKey = value; _assigned.Add(nameof(ApiKey)); }
    }

    /// <summary>
    /// Up to 10 extra headers with secret values. Replaces the saved set; null
    /// removes them. <c>Authorization</c>, <c>Content-Type</c>,
    /// <c>Content-Length</c>, <c>Host</c>, <c>Connection</c>,
    /// <c>Transfer-Encoding</c>, <c>User-Agent</c> and <c>X-Sendly-*</c> can't
    /// be set.
    /// </summary>
    [JsonPropertyName("headers")]
    public Dictionary<string, string>? Headers
    {
        get => _headers;
        set { _headers = value; _assigned.Add(nameof(Headers)); }
    }

    bool IAssignedProperties.IsAssigned(string propertyName) => _assigned.Contains(propertyName);
}

/// <summary>
/// Connection for a <c>sip</c> agent: Sendly connects the caller to your SIP
/// address. Setting <see cref="Username"/>, <see cref="Password"/> or
/// <see cref="Headers"/> to null sends null, which removes the saved value;
/// leaving them unset keeps it.
/// </summary>
public class VoiceAgentSipConnectionInput : VoiceAgentConnectionInput, IAssignedProperties
{
    private readonly HashSet<string> _assigned = new();
    private string? _username;
    private string? _password;
    private Dictionary<string, string>? _headers;

    /// <summary>
    /// <c>sip:</c> or <c>sips:</c> with a user part and a hostname (not an IP
    /// address), such as <c>sip:+15555550100@sip.example.com</c>. Only
    /// <c>;transport=</c> may follow. Required when the agent becomes
    /// <c>sip</c>.
    /// </summary>
    [JsonPropertyName("uri")]
    public string? Uri { get; set; }

    /// <summary>Defaults to <c>tcp</c>; a <c>sips:</c> address always uses <c>tls</c>. See <see cref="SipTransport"/>.</summary>
    [JsonPropertyName("transport")]
    public string? Transport { get; set; }

    /// <summary>Defaults to <c>allow</c>; <c>require</c> needs the <c>tls</c> transport. See <see cref="SipMediaEncryption"/>.</summary>
    [JsonPropertyName("mediaEncryption")]
    public string? MediaEncryption { get; set; }

    /// <summary>Digest auth username. Unset keeps the saved one; null removes it.</summary>
    [JsonPropertyName("username")]
    public string? Username
    {
        get => _username;
        set { _username = value; _assigned.Add(nameof(Username)); }
    }

    /// <summary>Digest auth password. Unset keeps the saved one; null removes it.</summary>
    [JsonPropertyName("password")]
    public string? Password
    {
        get => _password;
        set { _password = value; _assigned.Add(nameof(Password)); }
    }

    /// <summary>
    /// Up to 10 headers named <c>X-...</c> (not <c>X-Sendly-...</c>) sent on
    /// the call. Replaces the saved set; null removes them.
    /// </summary>
    [JsonPropertyName("headers")]
    public Dictionary<string, string>? Headers
    {
        get => _headers;
        set { _headers = value; _assigned.Add(nameof(Headers)); }
    }

    bool IAssignedProperties.IsAssigned(string propertyName) => _assigned.Contains(propertyName);
}

/// <summary>
/// Connection for a <c>websocket</c> agent: Sendly streams the call audio to
/// your server. Setting <see cref="AuthToken"/> or
/// <see cref="CustomParameters"/> to null sends null, which removes the saved
/// value; leaving them unset keeps it.
/// </summary>
public class VoiceAgentWebSocketConnectionInput : VoiceAgentConnectionInput, IAssignedProperties
{
    private readonly HashSet<string> _assigned = new();
    private string? _authToken;
    private Dictionary<string, string>? _customParameters;

    /// <summary>
    /// A <c>wss://</c> URL, up to 2048 characters, with no credentials or
    /// <c>#</c>. Required when the agent becomes <c>websocket</c>.
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>Defaults to <c>pcm16_16000</c>. See <see cref="WebSocketAudioFormat"/>.</summary>
    [JsonPropertyName("audioFormat")]
    public string? AudioFormat { get; set; }

    /// <summary>Sent as <c>Authorization: Bearer</c>. Unset keeps the saved token; null removes it.</summary>
    [JsonPropertyName("authToken")]
    public string? AuthToken
    {
        get => _authToken;
        set { _authToken = value; _assigned.Add(nameof(AuthToken)); }
    }

    /// <summary>
    /// Up to 20 string values sent to your server in the <c>start</c> message.
    /// Keys are 1-40 characters of letters, digits and <c>_ . : -</c>; values
    /// are up to 500 characters. Replaces the saved set; null removes them.
    /// </summary>
    [JsonPropertyName("customParameters")]
    public Dictionary<string, string>? CustomParameters
    {
        get => _customParameters;
        set { _customParameters = value; _assigned.Add(nameof(CustomParameters)); }
    }

    bool IAssignedProperties.IsAssigned(string propertyName) => _assigned.Contains(propertyName);
}

/// <summary>
/// Tool settings sent with <see cref="CreateVoiceAgentRequest"/> and
/// <see cref="UpdateVoiceAgentRequest"/>. Properties left null are not sent,
/// so an update keeps their current values.
/// </summary>
public class VoiceAgentToolsInput
{
    /// <summary>Whether the agent may text the caller. Defaults to true on create.</summary>
    [JsonPropertyName("sendSms")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? SendSms { get; set; }

    /// <summary>
    /// A number in E.164 format for callers who need a person (see
    /// <see cref="VoiceAgentTools.TransferTo"/>). An empty string clears it.
    /// </summary>
    [JsonPropertyName("transferTo")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TransferTo { get; set; }
}

/// <summary>An AI agent that answers and places phone calls.</summary>
public class VoiceAgent
{
    /// <summary>Agent identifier.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Always <c>voice_agent</c>.</summary>
    [JsonPropertyName("object")]
    public string Object { get; set; } = "voice_agent";

    /// <summary>The agent's name.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// False when the agent is switched off; a switched-off agent can't be
    /// pointed at a number or put on a call.
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    /// <summary>
    /// Who answers the agent's calls: <c>sendly</c>, or one of your own
    /// (<c>custom_llm</c>, <c>sip</c>, <c>websocket</c>). See
    /// <see cref="VoiceAgentKind"/>; a value not listed there comes through
    /// unchanged.
    /// </summary>
    [JsonPropertyName("kind")]
    public string Kind { get; set; } = VoiceAgentKind.Sendly;

    /// <summary>
    /// How Sendly reaches your own agent, or null for a <c>sendly</c> agent.
    /// Secrets are never returned, only whether one is saved.
    /// </summary>
    [JsonPropertyName("connection")]
    public VoiceAgentConnection? Connection { get; set; }

    /// <summary>
    /// True when the agent has a signing secret. Your own agents do: Sendly
    /// signs every request to them with it in the <c>X-Sendly-Signature</c>
    /// header (<c>t=&lt;unix&gt;,v1=&lt;hex HMAC-SHA256&gt;</c>).
    /// </summary>
    [JsonPropertyName("hasSigningSecret")]
    public bool HasSigningSecret { get; set; }

    /// <summary>
    /// The agent's signing secret, set only when it was just made: on the
    /// response to creating your own agent, to switching a <c>sendly</c>
    /// agent to one of your own, and to
    /// <see cref="UpdateVoiceAgentRequest.RotateSigningSecret"/>. Null
    /// everywhere else. Save it now; it is never shown again.
    /// </summary>
    [JsonPropertyName("signingSecret")]
    public string? SigningSecret { get; set; }

    /// <summary>Voice id, one of <see cref="VoiceVoicesResource.ListAsync"/>.</summary>
    [JsonPropertyName("voice")]
    public string Voice { get; set; } = string.Empty;

    /// <summary>Human-readable voice name, e.g. <c>Ashley (US, warm)</c>.</summary>
    [JsonPropertyName("voiceLabel")]
    public string VoiceLabel { get; set; } = string.Empty;

    /// <summary>Language tag, e.g. <c>en-US</c>.</summary>
    [JsonPropertyName("language")]
    public string Language { get; set; } = string.Empty;

    /// <summary>What the agent says when it picks up (empty when unset).</summary>
    [JsonPropertyName("greeting")]
    public string Greeting { get; set; } = string.Empty;

    /// <summary>Business instructions the agent follows (empty when unset).</summary>
    [JsonPropertyName("instructions")]
    public string Instructions { get; set; } = string.Empty;

    /// <summary>What the agent may do on a call.</summary>
    [JsonPropertyName("tools")]
    public VoiceAgentTools Tools { get; set; } = new();

    /// <summary>True when the agent holds its own scoped sending key, so <see cref="VoiceAgentTools.SendSms"/> can send.</summary>
    [JsonPropertyName("canSendSms")]
    public bool CanSendSms { get; set; }

    /// <summary>Calls this agent has handled.</summary>
    [JsonPropertyName("callsHandled")]
    public int CallsHandled { get; set; }

    /// <summary>Average answered duration of those calls, in seconds.</summary>
    [JsonPropertyName("avgDurationSecs")]
    public int AvgDurationSecs { get; set; }

    /// <summary>When the agent was created.</summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    /// <summary>When the agent last changed.</summary>
    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Request for <see cref="VoiceAgentsResource.CreateAsync"/>. Only
/// <see cref="Name"/> is required; properties left null are not sent.
/// </summary>
public class CreateVoiceAgentRequest
{
    /// <summary>1-80 characters. Required.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Whether the agent is switched on. Defaults to true.</summary>
    [JsonPropertyName("enabled")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Enabled { get; set; }

    /// <summary>A voice id from <see cref="VoiceVoicesResource.ListAsync"/>. An unknown id falls back to the default voice.</summary>
    [JsonPropertyName("voice")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Voice { get; set; }

    /// <summary>Language tag, up to 16 characters. Defaults to <c>en-US</c>.</summary>
    [JsonPropertyName("language")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Language { get; set; }

    /// <summary>What the agent says when it picks up, up to 500 characters.</summary>
    [JsonPropertyName("greeting")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Greeting { get; set; }

    /// <summary>Business instructions the agent follows, up to 4000 characters.</summary>
    [JsonPropertyName("instructions")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Instructions { get; set; }

    /// <summary>What the agent may do on a call. <c>SendSms</c> defaults to true, <c>TransferTo</c> to null.</summary>
    [JsonPropertyName("tools")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public VoiceAgentToolsInput? Tools { get; set; }

    /// <summary>
    /// <c>sendly</c> (the default), or one of your own: <c>custom_llm</c>,
    /// <c>sip</c> or <c>websocket</c>. See <see cref="VoiceAgentKind"/>. Your
    /// own agents need the workspace's bring-your-own-agent feature.
    /// </summary>
    [JsonPropertyName("kind")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Kind { get; set; }

    /// <summary>
    /// How Sendly reaches your own agent, matching <see cref="Kind"/>.
    /// Required for <c>custom_llm</c>, <c>sip</c> and <c>websocket</c>.
    /// </summary>
    [JsonPropertyName("connection")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public VoiceAgentConnectionInput? Connection { get; set; }
}

/// <summary>
/// Request for <see cref="VoiceAgentsResource.UpdateAsync"/>. Set only what
/// changes; properties left null are not sent and keep their current values.
/// </summary>
public class UpdateVoiceAgentRequest
{
    /// <summary>1-80 characters.</summary>
    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; set; }

    /// <summary>Switch the agent on or off.</summary>
    [JsonPropertyName("enabled")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Enabled { get; set; }

    /// <summary>A voice id from <see cref="VoiceVoicesResource.ListAsync"/>. An unknown id falls back to the default voice.</summary>
    [JsonPropertyName("voice")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Voice { get; set; }

    /// <summary>Language tag, up to 16 characters; an empty string resets it to <c>en-US</c>.</summary>
    [JsonPropertyName("language")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Language { get; set; }

    /// <summary>Up to 500 characters; an empty string clears it.</summary>
    [JsonPropertyName("greeting")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Greeting { get; set; }

    /// <summary>Up to 4000 characters; an empty string clears it.</summary>
    [JsonPropertyName("instructions")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Instructions { get; set; }

    /// <summary>Tool settings to change; tools left null keep their current values.</summary>
    [JsonPropertyName("tools")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public VoiceAgentToolsInput? Tools { get; set; }

    /// <summary>
    /// Switch who answers the agent's calls. See <see cref="VoiceAgentKind"/>.
    /// Switching to one of your own needs <see cref="Connection"/>; a <c>sendly</c> agent gets a
    /// signing secret (switching between your own kinds keeps it), returned once on <see cref="VoiceAgent.SigningSecret"/>;
    /// switching to <c>sendly</c> removes the connection and the signing
    /// secret.
    /// </summary>
    [JsonPropertyName("kind")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Kind { get; set; }

    /// <summary>
    /// How Sendly reaches your own agent, matching the agent's kind. With the
    /// same kind it is merged into the saved connection: properties left unset
    /// keep their values. Required when <see cref="Kind"/> changes to one of
    /// your own.
    /// </summary>
    [JsonPropertyName("connection")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public VoiceAgentConnectionInput? Connection { get; set; }

    /// <summary>
    /// True makes a new signing secret for your own agent; the response
    /// carries it once on <see cref="VoiceAgent.SigningSecret"/>, and the old
    /// secret stops working at once. Sent only when true. Refused with 400
    /// <c>invalid_request</c> on a <c>sendly</c> agent.
    /// </summary>
    [JsonPropertyName("rotateSigningSecret")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool RotateSigningSecret { get; set; }
}

/// <summary>
/// Settings to test with <see cref="VoiceAgentsResource.TestAsync"/>. Leave it
/// out to test the saved connection; properties left null are not sent.
/// </summary>
public class TestVoiceAgentRequest
{
    /// <summary>
    /// The kind of the settings in <see cref="Connection"/>: <c>custom_llm</c>,
    /// <c>sip</c> or <c>websocket</c> (see <see cref="VoiceAgentKind"/>).
    /// Defaults to the agent's own kind. Needed to test settings of another
    /// kind, such as on a <c>sendly</c> agent before switching it.
    /// </summary>
    [JsonPropertyName("kind")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Kind { get; set; }

    /// <summary>
    /// Settings to test instead of the saved ones. Nothing is saved. For the
    /// agent's own kind, a secret left out is taken from the saved connection, while the host stays the same.
    /// </summary>
    [JsonPropertyName("connection")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public VoiceAgentConnectionInput? Connection { get; set; }

    /// <summary>
    /// For <c>custom_llm</c>: the system prompt to send, up to 4000
    /// characters. Defaults to the agent's instructions.
    /// </summary>
    [JsonPropertyName("instructions")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Instructions { get; set; }
}

/// <summary>
/// What a connection test found. Reported as
/// <see cref="VoiceAgentTest.Outcome"/>. Values are plain strings so an
/// outcome added later still deserializes.
/// </summary>
public static class VoiceAgentTestOutcome
{
    /// <summary>The agent answered the way a call needs.</summary>
    public const string Ok = "ok";

    /// <summary>The host isn't a public address.</summary>
    public const string BlockedDestination = "blocked_destination";

    /// <summary>The agent couldn't be reached.</summary>
    public const string Unreachable = "unreachable";

    /// <summary>The agent didn't answer in time.</summary>
    public const string Timeout = "timeout";

    /// <summary>The secure connection couldn't be set up.</summary>
    public const string TlsFailed = "tls_failed";

    /// <summary>The SIP server wants credentials the agent doesn't have.</summary>
    public const string AuthRequired = "auth_required";

    /// <summary>The key, token or credentials were refused.</summary>
    public const string AuthFailed = "auth_failed";

    /// <summary>Wrong path or SIP user.</summary>
    public const string NotFound = "not_found";

    /// <summary>Any other refusal.</summary>
    public const string Rejected = "rejected";

    /// <summary>An answer outside the protocol.</summary>
    public const string BadReply = "bad_reply";
}

/// <summary>
/// A step of a connection test. Reported as
/// <see cref="VoiceAgentTestStep.Step"/>. Values are plain strings so a step
/// added later still deserializes.
/// </summary>
public static class VoiceAgentTestStepName
{
    /// <summary>The host resolved to a public address.</summary>
    public const string Address = "address";

    /// <summary>A connection opened.</summary>
    public const string Connect = "connect";

    /// <summary>The SIP server accepted the credentials.</summary>
    public const string Login = "login";

    /// <summary>The agent answered as it should.</summary>
    public const string Reply = "reply";
}

/// <summary>One step of a connection test.</summary>
public class VoiceAgentTestStep
{
    /// <summary>
    /// <c>address</c>, <c>connect</c>, <c>login</c> or <c>reply</c>. See
    /// <see cref="VoiceAgentTestStepName"/>; a value not listed there comes
    /// through as is.
    /// </summary>
    [JsonPropertyName("step")]
    public string Step { get; set; } = string.Empty;

    /// <summary>Whether this step worked.</summary>
    [JsonPropertyName("ok")]
    public bool Ok { get; set; }

    /// <summary>What happened at this step, in plain words.</summary>
    [JsonPropertyName("detail")]
    public string Detail { get; set; } = string.Empty;
}

/// <summary>
/// Response from <see cref="VoiceAgentsResource.TestAsync"/>. A failed test is
/// still a successful request: check <see cref="Ok"/>.
/// </summary>
public class VoiceAgentTest
{
    /// <summary>Whether the agent answered the way a call needs.</summary>
    [JsonPropertyName("ok")]
    public bool Ok { get; set; }

    /// <summary>
    /// <c>ok</c>, or why not. See <see cref="VoiceAgentTestOutcome"/>; a value
    /// not listed there comes through as is.
    /// </summary>
    [JsonPropertyName("outcome")]
    public string Outcome { get; set; } = string.Empty;

    /// <summary>One or two sentences to show a person: what happened and what to change.</summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>The HTTP or SIP status the agent answered with, or null.</summary>
    [JsonPropertyName("status")]
    public int? Status { get; set; }

    /// <summary>How long the agent took to answer, or null.</summary>
    [JsonPropertyName("latencyMs")]
    public int? LatencyMs { get; set; }

    /// <summary>
    /// Whether the request carried <c>X-Sendly-Signature</c>, as calls do.
    /// Always false for <c>sip</c>.
    /// </summary>
    [JsonPropertyName("signed")]
    public bool Signed { get; set; }

    /// <summary>Each step tried, in order, up to the first that failed.</summary>
    [JsonPropertyName("steps")]
    public List<VoiceAgentTestStep> Steps { get; set; } = new();
}

/// <summary>Response from <see cref="VoiceAgentsResource.ListAsync"/>.</summary>
public class VoiceAgentListResponse
{
    /// <summary>The workspace's agents.</summary>
    [JsonPropertyName("data")]
    public List<VoiceAgent> Data { get; set; } = new();
}

/// <summary>Response from <see cref="VoiceAgentsResource.DeleteAsync"/>.</summary>
public class DeletedVoiceAgent
{
    /// <summary>The deleted agent's id.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Always <c>voice_agent</c>.</summary>
    [JsonPropertyName("object")]
    public string Object { get; set; } = "voice_agent";

    /// <summary>Always true.</summary>
    [JsonPropertyName("deleted")]
    public bool Deleted { get; set; }
}

/// <summary>A voice an agent can speak with.</summary>
public class Voice
{
    /// <summary>Voice id to pass as <c>Voice</c> when creating or updating an agent.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Human-readable name, e.g. <c>Ashley (US, warm)</c>.</summary>
    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    /// <summary>Language the voice speaks, e.g. <c>en</c>.</summary>
    [JsonPropertyName("language")]
    public string Language { get; set; } = string.Empty;
}

/// <summary>Response from <see cref="VoiceVoicesResource.ListAsync"/>.</summary>
public class VoiceListResponse
{
    /// <summary>Every voice an agent can use.</summary>
    [JsonPropertyName("data")]
    public List<Voice> Data { get; set; } = new();
}
