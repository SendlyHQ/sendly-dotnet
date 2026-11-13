using System.Text.Json;
using Sendly.Models;

namespace Sendly.Resources;

/// <summary>
/// Short Codes Resource: apply for a US short code and track the application.
///
/// A short code is a 5 or 6 digit US sender. Getting one is an application,
/// not a purchase: you fill it in, Sendly reviews it, the carrier forms are
/// signed, Sendly files it, and each carrier certifies the code separately.
/// The application lives at <see cref="Application"/>; every application and
/// code is listed with <see cref="ListAsync"/>.
///
/// Reads need an API key with the <c>short_codes:read</c> scope and writes
/// <c>short_codes:write</c>. Every call answers 404 <c>short_codes_not_enabled</c>
/// (<see cref="Exceptions.NotFoundException"/>) until short codes are enabled
/// for the account.
/// </summary>
/// <example>
/// <code>
/// await client.ShortCodes.Application.UpdateAsync(new ShortCodeApplicationInput
/// {
///     UseCase = "Delivery alerts for Acme orders",
///     MessageFrequency = "4 messages per month",
/// });
/// var check = await client.ShortCodes.Application.CheckAsync();
/// if (check.Ok)
/// {
///     await client.ShortCodes.Application.SubmitAsync(new SubmitShortCodeApplicationRequest { AcceptTerms = true });
/// }
/// var codes = await client.ShortCodes.ListAsync();
/// </code>
/// </example>
public class ShortCodesResource
{
    private readonly SendlyClient _client;

    public ShortCodesResource(SendlyClient client)
    {
        _client = client;
        Application = new ShortCodeApplicationResource(client);
    }

    /// <summary>
    /// The workspace's short code application.
    /// </summary>
    public ShortCodeApplicationResource Application { get; }

    /// <summary>
    /// List every short code application of the workspace, newest first, with
    /// its code once Sendly assigns one. Drafts (with a null
    /// <see cref="ShortCode.Code"/>), rejected and cancelled applications are
    /// listed too, so filter on <see cref="ShortCode.Status"/> for the codes
    /// you can send from. Requires the <c>short_codes:read</c> scope.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Every application and code, each with its billing</returns>
    public async Task<ShortCodeListResponse> ListAsync(CancellationToken cancellationToken = default)
    {
        using var doc = await _client.GetAsync("/short_codes", null, cancellationToken);
        return JsonSerializer.Deserialize<ShortCodeListResponse>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }
}

/// <summary>
/// The workspace's short code application: read it, save answers, check it
/// against the carrier rules, and submit it.
/// </summary>
public class ShortCodeApplicationResource
{
    private readonly SendlyClient _client;

    public ShortCodeApplicationResource(SendlyClient client)
    {
        _client = client;
    }

    /// <summary>
    /// Fetch the workspace's short code application, with the documents it
    /// needs, the quote, and where the setup fee and the monthly lease stand.
    /// Requires the <c>short_codes:read</c> scope.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The application, whether it is still editable, and why not when it isn't</returns>
    public async Task<ShortCodeApplicationView> GetAsync(CancellationToken cancellationToken = default)
    {
        using var doc = await _client.GetAsync("/short_codes/application", null, cancellationToken);
        return JsonSerializer.Deserialize<ShortCodeApplicationView>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    /// <summary>
    /// Save answers on the application, creating it on the first call.
    /// Fields Sendly or the carriers own are ignored and named back in
    /// <see cref="ShortCodeApplicationView.IgnoredFields"/>. Requires the
    /// <c>short_codes:write</c> scope. Pass <paramref name="options"/> with an
    /// idempotency key so a repeat of the same save returns the first answer.
    /// </summary>
    /// <param name="application">The answers to save</param>
    /// <param name="options">Optional idempotency key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The saved application</returns>
    /// <exception cref="Exceptions.SendlyException">409 <c>short_code_locked</c> once the application is with Sendly or the carriers</exception>
    public async Task<ShortCodeApplicationView> UpdateAsync(
        ShortCodeApplicationInput application,
        IdempotentRequestOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (application == null)
            throw new Exceptions.ValidationException("Application answers are required");

        using var doc = await _client.PutAsync("/short_codes/application", application, options?.IdempotencyKey, cancellationToken);
        return JsonSerializer.Deserialize<ShortCodeApplicationView>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    /// <summary>
    /// Check the application against the carrier rules without changing
    /// anything. Unsaved answers can be passed in and are checked on top of
    /// what is saved. Requires the <c>short_codes:read</c> scope.
    /// </summary>
    /// <param name="application">Optional unsaved answers to check as well</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Whether it can be submitted, every issue found, and the documents still outstanding</returns>
    public async Task<ShortCodePreflight> CheckAsync(
        ShortCodeApplicationInput? application = null,
        CancellationToken cancellationToken = default)
    {
        using var doc = await _client.PostAsync(
            "/short_codes/application/preflight", application ?? new ShortCodeApplicationInput(), cancellationToken);
        return JsonSerializer.Deserialize<ShortCodePreflight>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    /// <summary>
    /// Submit the application to Sendly for review.
    ///
    /// Submitting charges the one-time $999 setup fee to the workspace's card
    /// on file, so set <see cref="SubmitShortCodeApplicationRequest.AcceptTerms"/>
    /// to true to accept it, the monthly lease from go-live and the 3-month
    /// minimum. The fee is refunded in full if Sendly rejects the application
    /// before filing it. Submitting twice is safe: the second call reports
    /// <see cref="ShortCodeApplicationView.AlreadySubmitted"/> and charges
    /// nothing. Requires the <c>short_codes:write</c> scope. Pass
    /// <paramref name="options"/> with an idempotency key so a repeat of the
    /// same submit returns the first answer.
    /// </summary>
    /// <param name="request"><c>AcceptTerms</c>, plus optional final answers saved before the check</param>
    /// <param name="options">Optional idempotency key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The submitted application, with <see cref="ShortCodeApplicationView.Payment"/> saying whether this call charged the fee</returns>
    /// <exception cref="Exceptions.ValidationException">422 <c>short_code_invalid_application</c> naming every field, including <c>acceptTerms</c>, or 422 <c>short_code_ai_draft_unedited</c> when the only issues are wording Sendly suggested that nobody edited</exception>
    /// <exception cref="Exceptions.SendlyException">403 <c>verification_required</c> until the workspace's business is verified (the answers sent are saved anyway)</exception>
    /// <exception cref="Exceptions.InsufficientCreditsException">402 <c>payment_method_required</c>, <c>payment_failed</c> or <c>payment_requires_authentication</c> when the fee can't be charged; the application stays a draft</exception>
    public async Task<ShortCodeApplicationView> SubmitAsync(
        SubmitShortCodeApplicationRequest request,
        IdempotentRequestOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new Exceptions.ValidationException("A submit request is required");

        using var doc = await _client.PostAsync(
            "/short_codes/application/submit", request, options?.IdempotencyKey, true, cancellationToken);
        return JsonSerializer.Deserialize<ShortCodeApplicationView>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }
}
