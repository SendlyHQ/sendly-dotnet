using System.Text.Json;
using Sendly.Exceptions;
using Sendly.Models;

namespace Sendly.Resources;

/// <summary>
/// Voice Resource: configure numbers, AI agents and voices for phone calls.
///
/// Everything a call depends on, configured from code: switch voice on for a
/// number and choose how it answers (<see cref="Numbers"/>), register the
/// number's emergency address, create the AI agents that talk on calls
/// (<see cref="Agents"/>), and list the voices they can speak with
/// (<see cref="Voices"/>). Place and follow calls with <c>client.Calls</c>.
///
/// Reads need an API key with the <c>calls:read</c> scope and writes
/// <c>calls:write</c>. Writes need a live key and accept an optional
/// <see cref="IdempotentRequestOptions"/>; POST requests get an idempotency
/// key automatically, while PATCH and DELETE send one only when you supply it.
/// In a team workspace, changing a number or its emergency address also needs
/// a role that can change settings, and managing agents a role that can manage
/// API keys (each agent holds its own scoped sending key); otherwise the API
/// answers 403 <c>forbidden</c>. Every call answers 404
/// (<see cref="NotFoundException"/>, <c>voice_not_enabled</c>) until voice is
/// enabled for your workspace.
/// </summary>
/// <example>
/// <code>
/// // Create an agent with one of the available voices
/// var voices = await client.Voice.Voices.ListAsync();
/// var agent = await client.Voice.Agents.CreateAsync(new CreateVoiceAgentRequest
/// {
///     Name = "Front desk",
///     Voice = voices.Data[0].Id,
///     Greeting = "Thanks for calling Acme, how can I help?",
/// });
///
/// // Register the emergency address, then have the agent answer the number
/// await client.Voice.Numbers.RegisterEmergencyAddressAsync("+15555550188", new EmergencyAddress
/// {
///     Street = "500 Example Ave",
///     City = "Austin",
///     State = "TX",
///     Zip = "78701",
/// });
/// await client.Voice.Numbers.UpdateAsync("+15555550188", new UpdateVoiceNumberRequest
/// {
///     VoiceEnabled = true,
///     VoiceMode = VoiceMode.Agent,
///     AgentId = agent.Id,
/// });
/// </code>
/// </example>
public class VoiceResource
{
    /// <summary>
    /// Voice settings and emergency addresses for the workspace's numbers.
    /// </summary>
    public VoiceNumbersResource Numbers { get; }

    /// <summary>
    /// The AI agents that answer and place calls.
    /// </summary>
    public VoiceAgentsResource Agents { get; }

    /// <summary>
    /// The voices an agent can speak with.
    /// </summary>
    public VoiceVoicesResource Voices { get; }

    public VoiceResource(SendlyClient client)
    {
        Numbers = new VoiceNumbersResource(client);
        Agents = new VoiceAgentsResource(client);
        Voices = new VoiceVoicesResource(client);
    }
}

/// <summary>
/// Voice settings and emergency addresses for the workspace's numbers. Every
/// method that takes a <c>number</c> accepts the number's id or its E.164
/// phone number.
/// </summary>
public class VoiceNumbersResource
{
    private readonly SendlyClient _client;

    public VoiceNumbersResource(SendlyClient client)
    {
        _client = client;
    }

    /// <summary>
    /// List the workspace's active numbers with their voice settings, in the
    /// same order as the dashboard. Requires the <c>calls:read</c> scope.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The numbers</returns>
    public async Task<VoiceNumberListResponse> ListAsync(
        CancellationToken cancellationToken = default)
    {
        using var doc = await _client.GetAsync("/voice/numbers", null, cancellationToken);
        return JsonSerializer.Deserialize<VoiceNumberListResponse>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    /// <summary>
    /// Fetch a number's voice settings. Requires the <c>calls:read</c> scope.
    /// </summary>
    /// <param name="number">The number's id or its E.164 phone number</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The number</returns>
    /// <exception cref="ValidationException">When <paramref name="number"/> is empty</exception>
    /// <exception cref="NotFoundException">404 <c>number_not_found</c> when the number isn't active in this workspace</exception>
    public async Task<VoiceNumber> GetAsync(
        string number,
        CancellationToken cancellationToken = default)
    {
        using var doc = await _client.GetAsync(NumberPath(number), null, cancellationToken);
        return JsonSerializer.Deserialize<VoiceNumber>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    /// <summary>
    /// Change how a number answers phone calls. Requires the
    /// <c>calls:write</c> scope and a live API key.
    ///
    /// This changes what happens when real people call the number. Turning
    /// voice on connects the number for calls before the change is saved.
    /// A mode alone is enough: <c>VoiceMode</c> <c>ring_dashboard</c> or
    /// <c>agent</c> switches voice on and <c>none</c> switches it off.
    /// <c>VoiceEnabled</c> false wins over any mode, and <c>none</c> with
    /// <c>VoiceEnabled</c> true becomes <c>ring_dashboard</c>. No idempotency
    /// key is sent unless you pass <paramref name="options"/>.
    /// </summary>
    /// <param name="number">The number's id or its E.164 phone number</param>
    /// <param name="request">The settings to change; properties left null are not sent</param>
    /// <param name="options">Optional idempotency key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The number after the change</returns>
    /// <exception cref="ValidationException">When <paramref name="number"/> is empty or <paramref name="request"/> is null; 400 <c>invalid_request</c>, <c>invalid_voice_mode</c> or <c>agent_required</c> (agent mode with no agent)</exception>
    /// <exception cref="NotFoundException">404 <c>number_not_found</c> or <c>agent_not_found</c></exception>
    /// <exception cref="SendlyException">403 <c>forbidden</c> / <c>live_key_required</c>, 409 <c>agent_disabled</c>, 502 <c>voice_attach_failed</c> (try again), 503 <c>voice_unavailable</c> (see <see cref="SendlyException.ApiErrorCode"/>). The 502 and 503 can follow a mode alone on a number whose voice is off.</exception>
    public async Task<VoiceNumber> UpdateAsync(
        string number,
        UpdateVoiceNumberRequest request,
        IdempotentRequestOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var path = NumberPath(number);
        if (request == null)
            throw new ValidationException("Voice settings are required");

        using var doc = await _client.PatchAsync(path, request, options?.IdempotencyKey, cancellationToken);
        return JsonSerializer.Deserialize<VoiceNumber>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    /// <summary>
    /// Register the street address emergency services are sent to when
    /// someone calls them from this number. Requires the <c>calls:write</c>
    /// scope and a live API key.
    ///
    /// A US or Canadian number needs one before it can place calls. The first
    /// registration adds $1.50 a month to the number; registering again
    /// replaces the address without adding the charge a second time. An
    /// idempotency key is generated automatically; pass
    /// <paramref name="options"/> to supply your own.
    /// </summary>
    /// <param name="number">The number's id or its E.164 phone number</param>
    /// <param name="address">The address; <c>Country</c> defaults to <c>US</c></param>
    /// <param name="options">Optional idempotency key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The number with its <see cref="VoiceNumber.EmergencyAddress"/></returns>
    /// <exception cref="ValidationException">When <paramref name="number"/>, <c>Street</c>, <c>City</c>, <c>State</c> or <c>Zip</c> is empty; 400 <c>invalid_address</c> for a malformed field or <c>e911_not_applicable</c> for a number outside the US and Canada; 422 <c>invalid_address</c> when the address couldn't be validated, with a corrected address (or null) under <c>suggested</c> in <see cref="SendlyException.ResponseBody"/></exception>
    /// <exception cref="NotFoundException">404 <c>number_not_found</c> when the number isn't active in this workspace</exception>
    /// <exception cref="SendlyException">403 <c>forbidden</c> / <c>live_key_required</c>; 502 <c>carrier_refused</c> when the registration was refused, thrown after the client has already retried the 5xx on its own. When the message says the number couldn't be found for emergency registration, retrying won't help: contact support. When it says the address couldn't be registered or emergency calling couldn't be switched on, try again later.</exception>
    public async Task<VoiceNumber> RegisterEmergencyAddressAsync(
        string number,
        EmergencyAddress address,
        IdempotentRequestOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var path = $"{NumberPath(number)}/emergency-address";
        if (address == null)
            throw new ValidationException("An emergency address is required");
        if (string.IsNullOrWhiteSpace(address.Street))
            throw new ValidationException("An emergency address 'street' is required");
        if (string.IsNullOrWhiteSpace(address.City))
            throw new ValidationException("An emergency address 'city' is required");
        if (string.IsNullOrWhiteSpace(address.State))
            throw new ValidationException("An emergency address 'state' is required");
        if (string.IsNullOrWhiteSpace(address.Zip))
            throw new ValidationException("An emergency address 'zip' is required");

        using var doc = await _client.PostAsync(path, address, options?.IdempotencyKey, true, cancellationToken);
        return JsonSerializer.Deserialize<VoiceNumber>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    private static string NumberPath(string number)
    {
        if (string.IsNullOrWhiteSpace(number))
            throw new ValidationException("A number is required: the number's id or its E.164 phone number");
        return $"/voice/numbers/{Uri.EscapeDataString(number)}";
    }
}

/// <summary>
/// The AI agents that answer and place calls: list, create, update and delete.
/// </summary>
public class VoiceAgentsResource
{
    private readonly SendlyClient _client;

    public VoiceAgentsResource(SendlyClient client)
    {
        _client = client;
    }

    /// <summary>
    /// List the workspace's AI agents with their call stats. Requires the
    /// <c>calls:read</c> scope.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The agents</returns>
    public async Task<VoiceAgentListResponse> ListAsync(
        CancellationToken cancellationToken = default)
    {
        using var doc = await _client.GetAsync("/voice/agents", null, cancellationToken);
        return JsonSerializer.Deserialize<VoiceAgentListResponse>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    /// <summary>
    /// Create an AI agent. Requires the <c>calls:write</c> scope and a live
    /// API key.
    ///
    /// The agent answers real callers on any number pointed at it and talks on
    /// the calls you place with it. Each agent gets its own scoped sending key
    /// so it can text callers; <see cref="VoiceAgent.CanSendSms"/> says whether
    /// it has one. A workspace can have up to 20 agents. An idempotency key is
    /// generated automatically; pass <paramref name="options"/> to supply your
    /// own.
    /// </summary>
    /// <param name="request">The agent's name, voice, greeting, instructions and tools</param>
    /// <param name="options">Optional idempotency key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The new agent</returns>
    /// <exception cref="ValidationException">When <c>Name</c> is empty; 400 <c>invalid_request</c> naming the field the API rejected</exception>
    /// <exception cref="SendlyException">403 <c>forbidden</c> / <c>live_key_required</c>, 409 <c>agent_limit</c> when the workspace already has 20 agents</exception>
    public async Task<VoiceAgent> CreateAsync(
        CreateVoiceAgentRequest request,
        IdempotentRequestOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request?.Name))
            throw new ValidationException("An agent 'name' is required");

        using var doc = await _client.PostAsync("/voice/agents", request, options?.IdempotencyKey, true, cancellationToken);
        return JsonSerializer.Deserialize<VoiceAgent>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    /// <summary>
    /// Fetch one agent. Requires the <c>calls:read</c> scope.
    /// </summary>
    /// <param name="id">Agent identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The agent</returns>
    /// <exception cref="ValidationException">When <paramref name="id"/> is empty</exception>
    /// <exception cref="NotFoundException">404 <c>agent_not_found</c> when the agent isn't in this workspace</exception>
    public async Task<VoiceAgent> GetAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        using var doc = await _client.GetAsync(AgentPath(id), null, cancellationToken);
        return JsonSerializer.Deserialize<VoiceAgent>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    /// <summary>
    /// Update an agent. Requires the <c>calls:write</c> scope and a live API
    /// key.
    ///
    /// Only the properties you set are sent; tools you leave null keep their
    /// current values. Changes apply to the next call the agent takes. No
    /// idempotency key is sent unless you pass <paramref name="options"/>.
    /// </summary>
    /// <param name="id">Agent identifier</param>
    /// <param name="request">Any subset of the create fields</param>
    /// <param name="options">Optional idempotency key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The agent after the change</returns>
    /// <exception cref="ValidationException">When <paramref name="id"/> is empty or <paramref name="request"/> is null; 400 <c>invalid_request</c> naming the field the API rejected</exception>
    /// <exception cref="NotFoundException">404 <c>agent_not_found</c> when the agent isn't in this workspace</exception>
    /// <exception cref="SendlyException">403 <c>forbidden</c> / <c>live_key_required</c></exception>
    public async Task<VoiceAgent> UpdateAsync(
        string id,
        UpdateVoiceAgentRequest request,
        IdempotentRequestOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var path = AgentPath(id);
        if (request == null)
            throw new ValidationException("Agent details are required");

        using var doc = await _client.PatchAsync(path, request, options?.IdempotencyKey, cancellationToken);
        return JsonSerializer.Deserialize<VoiceAgent>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    /// <summary>
    /// Delete an agent and revoke its sending key. Requires the
    /// <c>calls:write</c> scope and a live API key.
    ///
    /// An agent that still answers a number can't be deleted: point those
    /// numbers at another agent or back to the team first with
    /// <see cref="VoiceNumbersResource.UpdateAsync"/>. No idempotency key is
    /// sent unless you pass <paramref name="options"/>.
    /// </summary>
    /// <param name="id">Agent identifier</param>
    /// <param name="options">Optional idempotency key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Deletion confirmation</returns>
    /// <exception cref="ValidationException">When <paramref name="id"/> is empty</exception>
    /// <exception cref="NotFoundException">404 <c>agent_not_found</c> when the agent isn't in this workspace</exception>
    /// <exception cref="SendlyException">403 <c>forbidden</c> / <c>live_key_required</c>, 409 <c>agent_in_use</c> while a number answers with the agent; the numbers are listed under <c>numbers</c> in <see cref="SendlyException.ResponseBody"/></exception>
    public async Task<DeletedVoiceAgent> DeleteAsync(
        string id,
        IdempotentRequestOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        using var doc = await _client.DeleteAsync(AgentPath(id), options?.IdempotencyKey, cancellationToken);
        return JsonSerializer.Deserialize<DeletedVoiceAgent>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    private static string AgentPath(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ValidationException("Agent ID is required");
        return $"/voice/agents/{Uri.EscapeDataString(id)}";
    }
}

/// <summary>
/// The voices an agent can speak with.
/// </summary>
public class VoiceVoicesResource
{
    private readonly SendlyClient _client;

    public VoiceVoicesResource(SendlyClient client)
    {
        _client = client;
    }

    /// <summary>
    /// List the voices an agent can speak with. Pass a voice's
    /// <see cref="Voice.Id"/> as <c>Voice</c> when creating or updating an
    /// agent. Requires the <c>calls:read</c> scope.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The voices</returns>
    public async Task<VoiceListResponse> ListAsync(
        CancellationToken cancellationToken = default)
    {
        using var doc = await _client.GetAsync("/voice/voices", null, cancellationToken);
        return JsonSerializer.Deserialize<VoiceListResponse>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }
}
