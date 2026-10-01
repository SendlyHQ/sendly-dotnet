using System.Text.Json;
using Sendly.Exceptions;
using Sendly.Models;

namespace Sendly.Resources;

/// <summary>
/// Calls Resource: place phone calls handled by your AI agents, list and
/// inspect calls, end a call, and download recordings.
///
/// A workspace phone number with voice enabled can take and place phone
/// calls. Over the API a call is always handled by one of your AI agents (a
/// receptionist you create with <c>client.Voice.Agents</c> or in the dashboard
/// under Calls → Agents); the agent speaks first and follows any
/// <c>Context</c> you attach. Switch voice on for a number, choose how it
/// answers and register its emergency address with <c>client.Voice.Numbers</c>,
/// which also lists your numbers with their voice settings.
///
/// Calls are prepaid from the workspace balance per started minute: an
/// agent-handled outbound call costs 10 credits a minute ($0.10). Unanswered
/// calls cost nothing. Destinations are US and Canada.
///
/// Reads need an API key with the <c>calls:read</c> scope and writes
/// <c>calls:write</c>. Writes need a live key and accept an optional
/// <see cref="IdempotentRequestOptions"/>; POST requests get an idempotency
/// key automatically. Every call answers 404 (<see cref="NotFoundException"/>,
/// <c>voice_not_enabled</c>) until voice is enabled for your workspace.
/// </summary>
/// <example>
/// <code>
/// // Place a call
/// var call = await client.Calls.CreateAsync(new CreateCallRequest
/// {
///     To = "+15555550123",
///     AgentId = "3c4d5e6f-7081-4293-a4b5-c6d7e8f90a1b",
///     Context = "You are calling Jordan to confirm the 3pm appointment on Tuesday.",
///     Metadata = new() { ["crmId"] = "lead_8812" },
/// });
///
/// // Follow it
/// call = await client.Calls.GetAsync(call.Id);
/// Console.WriteLine($"{call.Status} {call.HangupClass}");
///
/// // Download the recording once it is ready
/// var recording = await client.Calls.RecordingAsync(call.Id);
/// if (recording.Status == CallRecordingStatus.Ready)
///     Console.WriteLine(recording.Url);
/// </code>
/// </example>
public class CallsResource
{
    private readonly SendlyClient _client;

    public CallsResource(SendlyClient client)
    {
        _client = client;
    }

    /// <summary>
    /// Place a phone call handled by one of your AI agents. Requires the
    /// <c>calls:write</c> scope and a live API key.
    ///
    /// The call is returned while it rings (<c>Status</c> <c>ringing</c>,
    /// <c>CreditsCharged</c> 0); poll <see cref="GetAsync"/> or subscribe to
    /// <c>call.started</c> / <c>call.completed</c> webhooks to follow it. At
    /// least one minute at the agent-outbound rate must be in the balance. An
    /// idempotency key is generated automatically; pass
    /// <paramref name="options"/> to supply your own.
    /// </summary>
    /// <param name="request">Who to call, which agent talks, and optionally which number to call from, call context and metadata</param>
    /// <param name="options">Optional idempotency key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The new call, ringing</returns>
    /// <exception cref="ValidationException">400 <c>agent_required</c>, <c>from_number_required</c>, <c>from_number_not_supported</c>, <c>invalid_number</c>, <c>destination_not_supported</c> or <c>invalid_metadata</c></exception>
    /// <exception cref="NotFoundException">404 <c>voice_not_enabled</c>, <c>outbound_calls_not_enabled</c>, <c>agent_not_found</c> or <c>number_not_found</c></exception>
    /// <exception cref="InsufficientCreditsException">402 <c>insufficient_credits</c> when the balance cannot cover one minute</exception>
    /// <exception cref="RateLimitException">429 <c>daily_call_limit</c> or <c>rate_limit_exceeded</c></exception>
    /// <exception cref="SendlyException">403 <c>live_key_required</c>, 409 <c>agent_disabled</c> / <c>no_voice_number</c> / <c>lines_busy</c>, 428 <c>e911_required</c>, 503 <c>voice_unavailable</c> (see <see cref="SendlyException.ApiErrorCode"/>)</exception>
    public async Task<Call> CreateAsync(
        CreateCallRequest request,
        IdempotentRequestOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(request?.To))
            throw new ValidationException("A destination 'to' is required");
        if (string.IsNullOrEmpty(request.AgentId))
            throw new ValidationException("An 'agentId' is required");

        using var doc = await _client.PostAsync("/calls", request, options?.IdempotencyKey, true, cancellationToken);
        return JsonSerializer.Deserialize<Call>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    /// <summary>
    /// List your workspace's calls, newest first. Requires the
    /// <c>calls:read</c> scope.
    ///
    /// Live calls are brought up to date before they are returned, so a ring
    /// past its deadline shows as <c>no_answer</c>.
    /// </summary>
    /// <param name="options">Filters (status, direction, kind, agent, to, from) and pagination (limit 1-100, offset)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The calls on this page and where the page sits in the full list</returns>
    /// <exception cref="ValidationException">400 <c>invalid_request</c> when a filter value is not one the API accepts</exception>
    public async Task<CallListResponse> ListAsync(
        ListCallsOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        using var doc = await _client.GetAsync("/calls", options?.ToQueryParams(), cancellationToken);
        return JsonSerializer.Deserialize<CallListResponse>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    /// <summary>
    /// Fetch one call. Requires the <c>calls:read</c> scope.
    ///
    /// For agent-handled calls the response also carries
    /// <see cref="Call.Transcript"/>; for other calls it is null.
    /// </summary>
    /// <param name="id">Call identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The call, brought up to date if it is live</returns>
    /// <exception cref="NotFoundException">404 <c>call_not_found</c> when the call isn't in this workspace</exception>
    public async Task<Call> GetAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(id))
            throw new ValidationException("Call ID is required");

        using var doc = await _client.GetAsync($"/calls/{Uri.EscapeDataString(id)}", null, cancellationToken);
        return JsonSerializer.Deserialize<Call>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    /// <summary>
    /// End a call. Requires the <c>calls:write</c> scope and a live API key.
    ///
    /// A ringing call becomes <c>cancelled</c> (<c>HangupClass</c>
    /// <c>caller_cancelled</c>) and the callee stops ringing; an active call
    /// becomes <c>completed</c> (<c>normal</c>). A call that has already ended
    /// is returned unchanged. An idempotency key is generated automatically;
    /// pass <paramref name="options"/> to supply your own.
    /// </summary>
    /// <param name="id">Call identifier</param>
    /// <param name="options">Optional idempotency key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The call after the hangup</returns>
    /// <exception cref="NotFoundException">404 <c>call_not_found</c> when the call isn't in this workspace</exception>
    /// <exception cref="SendlyException">403 <c>live_key_required</c> with a test key</exception>
    public async Task<Call> HangupAsync(
        string id,
        IdempotentRequestOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(id))
            throw new ValidationException("Call ID is required");

        using var doc = await _client.PostAsync($"/calls/{Uri.EscapeDataString(id)}/hangup", new { }, options?.IdempotencyKey, true, cancellationToken);
        return JsonSerializer.Deserialize<Call>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    /// <summary>
    /// Fetch a call's recording. Requires the <c>calls:read</c> scope.
    ///
    /// <see cref="CallRecording.Url"/> is a signed download link valid for
    /// five minutes (<see cref="CallRecording.ExpiresAt"/>) and is null unless
    /// <see cref="CallRecording.Status"/> is <c>ready</c>. Recordings are
    /// Ogg/Opus; agent calls are dual-channel, with the agent on the left
    /// channel and the other party on the right.
    /// Recording is switched on per workspace in the dashboard under
    /// Calls → Settings.
    /// </summary>
    /// <param name="id">Call identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The recording's status and, when ready, its download URL</returns>
    /// <exception cref="NotFoundException">404 <c>call_not_found</c> when the call isn't in this workspace</exception>
    public async Task<CallRecording> RecordingAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(id))
            throw new ValidationException("Call ID is required");

        using var doc = await _client.GetAsync($"/calls/{Uri.EscapeDataString(id)}/recording", null, cancellationToken);
        return JsonSerializer.Deserialize<CallRecording>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }
}
