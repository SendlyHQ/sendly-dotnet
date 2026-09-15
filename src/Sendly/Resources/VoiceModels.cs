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
    /// A number in E.164 format for callers who need a person, or null.
    /// Agents cannot transfer calls yet and never dial or read out this
    /// number: while it is set, a caller who asks for a person is told the
    /// message will be passed on, and the agent takes their name and number.
    /// </summary>
    [JsonPropertyName("transferTo")]
    public string? TransferTo { get; set; }
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
