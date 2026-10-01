using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Sendly.Exceptions;

namespace Sendly.Resources;

/// <summary>
/// WhatsApp Resource — connect senders, manage templates, check windows.
///
/// WhatsApp is a first-class Sendly channel: connect a number you own, create
/// Meta-reviewed message templates, and send with
/// <c>client.Messages.SendAsync(new SendWhatsAppMessageRequest(...))</c>.
///
/// Connecting a number is a one-time $19 setup (no monthly fee). The first
/// number ends with a human step:
/// <see cref="WhatsAppSignupResource.CreateAsync(string, CancellationToken)"/>
/// returns a <c>ConnectUrl</c> that a person must open in a browser and log in
/// with Facebook to link their WhatsApp Business Account. Hand the URL to your
/// user: that step cannot be completed programmatically. Once an account is
/// connected, more numbers can be added to it from code: pass its
/// <c>BusinessAccountId</c> to
/// <see cref="WhatsAppSignupResource.CreateAsync(StartWhatsAppSignupRequest, CancellationToken)"/>,
/// and submit the code Meta sends the number with
/// <see cref="WhatsAppSignupResource.VerifyAsync"/>.
///
/// Two ways to reach a recipient:
///
/// - Inside a 24-hour window (the recipient messaged you in the last 24h):
/// free-form text and media are allowed. Check with <see cref="WindowAsync"/>.
///
/// - Anytime: an approved template. Templates are reviewed by Meta (typically
/// 24-48h) and categorized as authentication, utility, or marketing — pricing
/// follows the category and destination country. Note: Meta has paused
/// marketing template delivery to US (+1) numbers.
///
/// Pricing: free-form text or media inside the 24-hour window costs 1 credit
/// each for the first 1,000 per sending number per calendar month (UTC), then
/// the destination's utility template price; countries without a listed
/// price use the default utility price of 12 credits. Templates are priced by
/// category and destination country; countries without a listed price use 33
/// (marketing), 12 (utility) and 12 (authentication) credits. A failed send
/// gives its slot back.
///
/// Scopes and keys: sends go through <c>Messages.SendAsync</c> and need
/// <c>sms:send</c>, not <c>whatsapp:write</c>, and a live key. Reads (signup
/// status, templates, the window, senders, sender profiles and
/// conversational components) need <c>whatsapp:read</c> and accept test
/// keys. Signup (including submitting and resending a code), template
/// create/edit/delete and sender edits (profile, photo, conversational
/// components and calling) need <c>whatsapp:write</c> and a live key
/// (otherwise 403 <c>whatsapp_requires_live_key</c>). In a team workspace,
/// connecting and sender edits need an owner or admin
/// (<c>settings:write</c>), and template writes need an owner, admin or member
/// (<c>templates:write</c>); a missing role returns 403
/// <c>insufficient_permissions</c>.
///
/// WhatsApp is enabled per person: the user who owns the API key, not the
/// workspace. While it is off, sends return 403 <c>whatsapp_not_enabled</c>
/// and every method on this resource gets 404 <c>not_found</c>.
/// </summary>
/// <example>
/// <code>
/// // 1. Connect a number ($19 one-time, no monthly fee). The connect URL
/// //    must be opened by a human — they log in with Facebook in a browser
/// //    to link their WhatsApp Business Account.
/// var signup = await sendly.WhatsApp.Signup.CreateAsync("+15559876543");
/// Console.WriteLine($"Have your user open: {signup.ConnectUrl}");
///
/// // 2. Poll until active
/// var status = await sendly.WhatsApp.Signup.GetAsync(signup.Id);
///
/// // 3. Create a template (Meta reviews it, usually 24-48h)
/// await sendly.WhatsApp.Templates.CreateAsync(new CreateWhatsAppTemplateRequest
/// {
///     Sender = "+15559876543",
///     Name = "order_shipped",
///     Language = "en_US",
///     Category = "UTILITY",
///     Body = "Hi {{1}}, your order {{2}} has shipped!",
///     Examples = new() { ["1"] = "Sam", ["2"] = "#4821" },
/// });
///
/// // 4. Send — free-form inside an open 24h window, template anytime
/// var window = await sendly.WhatsApp.WindowAsync("+15559876543", "+15551234567");
/// </code>
/// </example>
public partial class WhatsAppResource
{
    private static readonly Regex PhoneRegex = MyRegex();

    private readonly SendlyClient _client;

    /// <summary>
    /// Connect numbers to WhatsApp. Starting a signup returns a
    /// <c>ConnectUrl</c> a human must complete in a browser, or, for a number
    /// added to an account already connected, a code to verify.
    /// </summary>
    public WhatsAppSignupResource Signup { get; }

    /// <summary>
    /// List the numbers connected (or connecting) to WhatsApp and manage their
    /// business profiles, profile photos, conversational components and
    /// calling.
    /// </summary>
    public WhatsAppSendersResource Senders { get; }

    /// <summary>
    /// Manage Meta-reviewed message templates.
    /// </summary>
    public WhatsAppTemplatesResource Templates { get; }

    public WhatsAppResource(SendlyClient client)
    {
        _client = client;
        Signup = new WhatsAppSignupResource(client);
        Senders = new WhatsAppSendersResource(client);
        Templates = new WhatsAppTemplatesResource(client);
    }

    /// <summary>
    /// Check whether a 24-hour customer-service window is open between one of
    /// your WhatsApp senders and a recipient.
    ///
    /// Free-form text and media only deliver while a window is open (it opens
    /// when the recipient messages you and lasts 24h from their last inbound
    /// message). Outside a window, send an approved template. Needs the
    /// <c>whatsapp:read</c> scope; test keys work.
    ///
    /// The response is exactly <c>{ open, expiresAt }</c>: with no window on
    /// record <c>Open</c> is false and <c>ExpiresAt</c> is null; after a window
    /// has expired <c>Open</c> is false and <c>ExpiresAt</c> is the past expiry.
    /// </summary>
    /// <param name="from">Your WhatsApp-connected sending number, in E.164 format</param>
    /// <param name="to">The recipient's number, in E.164 format</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Whether the window is open and when it closes</returns>
    public async Task<WhatsAppWindow> WindowAsync(
        string from,
        string to,
        CancellationToken cancellationToken = default)
    {
        ValidatePhone(from);
        ValidatePhone(to);

        var queryParams = new Dictionary<string, string>
        {
            ["from"] = from,
            ["to"] = to
        };

        using var doc = await _client.GetAsync("/whatsapp/window", queryParams, cancellationToken);
        return JsonSerializer.Deserialize<WhatsAppWindow>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    internal static void ValidatePhone(string? phone)
    {
        if (string.IsNullOrEmpty(phone) || !PhoneRegex.IsMatch(phone))
        {
            throw new ValidationException(
                "Invalid phone number format. Use E.164 format (e.g., +15551234567)");
        }
    }

    [GeneratedRegex(@"^\+[1-9]\d{1,14}$")]
    private static partial Regex MyRegex();
}

/// <summary>
/// Connect numbers to WhatsApp.
/// </summary>
public class WhatsAppSignupResource
{
    private readonly SendlyClient _client;

    public WhatsAppSignupResource(SendlyClient client)
    {
        _client = client;
    }

    /// <summary>
    /// Start connecting a number to WhatsApp.
    ///
    /// Charges a one-time $19 setup fee (no monthly fee) and returns a
    /// <c>ConnectUrl</c>. Completing the connection requires a human: hand the
    /// URL to your user — they open it in a browser and log in with Facebook
    /// to link their WhatsApp Business Account. Then poll
    /// <see cref="GetAsync"/> until the status is <c>active</c>.
    ///
    /// Calling again for a number with an in-flight signup returns the
    /// existing signup (same <c>ConnectUrl</c>) without charging again.
    /// Requires a live API key with the <c>whatsapp:write</c> scope (a test key
    /// gets 403 <c>whatsapp_requires_live_key</c>) and, in a team workspace, an
    /// owner or admin (<c>settings:write</c>). After the Facebook step the
    /// signup stays <c>registering</c> while WhatsApp activates the number.
    /// Activation usually takes a few minutes but can take hours. If it hasn't
    /// finished about 6 hours after the session began, the session fails with
    /// <c>registration_timeout</c> and the fee is refunded. If the connection
    /// fails, the $19 fee is refunded automatically; once the number has
    /// connected there is no refund, and a later disconnect gets nothing back.
    /// To add a number to a WhatsApp Business account already connected,
    /// without the Facebook step, use
    /// <see cref="CreateAsync(StartWhatsAppSignupRequest, CancellationToken)"/>.
    /// </summary>
    /// <exception cref="SendlyException">
    /// <c>whatsapp_verification_in_progress</c> (409) when the number is
    /// being added to a connected account by code; finish that with
    /// <see cref="VerifyAsync"/>, or wait for it to expire. The body's
    /// <c>id</c> is that signup. <c>whatsapp_unavailable</c> (503) while
    /// WhatsApp connections are unavailable; nothing is charged. Only signup
    /// returns it, with <c>retryAfter</c> 3600 in the body and a
    /// <c>Retry-After: 3600</c> header. The client retries it like any 5xx before throwing.
    /// </exception>
    /// <exception cref="RateLimitException">
    /// <c>whatsapp_signup_limit_reached</c> (429) after 5 failed, charged
    /// signups in 24 hours. Not retried; try again the next day.
    /// </exception>
    /// <param name="phoneNumber">The number to connect, in E.164 format. Must be an
    /// active number in your workspace (provisioned, purchased, or fully ported into Sendly).</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The signup with its <c>ConnectUrl</c></returns>
    public async Task<WhatsAppSignupSession> CreateAsync(
        string phoneNumber,
        CancellationToken cancellationToken = default)
    {
        WhatsAppResource.ValidatePhone(phoneNumber);

        var request = new StartWhatsAppSignupRequest { PhoneNumber = phoneNumber };
        using var doc = await _client.PostAsync("/whatsapp/signup", request, cancellationToken);
        return JsonSerializer.Deserialize<WhatsAppSignupSession>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    /// <summary>
    /// Start connecting a number to WhatsApp, or add a number to a WhatsApp
    /// Business account this workspace already connected.
    ///
    /// Without <see cref="StartWhatsAppSignupRequest.BusinessAccountId"/> this
    /// is the Facebook connection that <see cref="CreateAsync(string, CancellationToken)"/>
    /// starts, and the result carries a <c>ConnectUrl</c>.
    ///
    /// With <see cref="StartWhatsAppSignupRequest.BusinessAccountId"/> (as
    /// shown on <see cref="WhatsAppSender.BusinessAccountId"/>) there is no
    /// Facebook step: Meta sends the number a 6-digit code by text
    /// (<c>sms</c>, the default) or voice call, and the result is the signup
    /// with status <c>verifying</c> and an empty <c>ConnectUrl</c>. Submit the
    /// code with <see cref="VerifyAsync"/>; while verifying,
    /// <see cref="GetAsync"/> returns the code as
    /// <see cref="WhatsAppSignup.VerificationCode"/> once Meta's text has
    /// arrived on the number. The account must be connected in this
    /// workspace with at least one active number, otherwise the API answers
    /// 404 <c>whatsapp_business_account_not_found</c>. The number must meet
    /// the same rules as a Facebook connection, and the same one-time $19 fee
    /// applies, charged before the code is requested and refunded
    /// automatically if the connection fails. Calling again for a number
    /// that is already verifying returns that session without charging again
    /// or sending another code. Requires a live API key with the
    /// <c>whatsapp:write</c> scope and, in a team workspace, an owner or
    /// admin (<c>settings:write</c>).
    /// </summary>
    /// <exception cref="ValidationException">
    /// 400 <c>display_name_required</c> when no display name is given and the
    /// account has none to reuse. 400 <c>invalid_request</c> when
    /// <c>VerificationMethod</c> isn't <c>sms</c> or <c>voice</c>, or
    /// <c>DisplayName</c> is longer than 512 characters. 422 <c>whatsapp_verification_start_failed</c>
    /// when WhatsApp refused to verify the number; the session failed and
    /// the fee is refunded.
    /// </exception>
    /// <exception cref="NotFoundException">
    /// 404 <c>whatsapp_business_account_not_found</c>.
    /// </exception>
    /// <exception cref="SendlyException">
    /// 409 <c>whatsapp_signup_in_progress</c> (a Facebook connection for the
    /// number is in flight), 409 <c>whatsapp_already_enabled</c>, 409
    /// <c>whatsapp_verification_in_progress</c> (an expired attempt for the
    /// number is still being cleared; try again in a moment), or 502
    /// <c>whatsapp_verification_start_failed</c> when WhatsApp couldn't be
    /// reached; that session failed and its fee is refunded, so start again.
    /// With <c>BusinessAccountId</c> set, a 5xx, a 408, a timeout or a
    /// network failure (a <see cref="NetworkException"/>) is thrown on the
    /// first attempt and never retried automatically, because a retry could
    /// start a new session that is charged and, when it fails, refunded. Only
    /// a 429 the client waits out is retried. Without
    /// <c>BusinessAccountId</c>, the errors and retries of
    /// <see cref="CreateAsync(string, CancellationToken)"/> apply.
    /// </exception>
    /// <exception cref="InsufficientCreditsException">
    /// 402 <c>payment_method_required</c> or <c>payment_failed</c> when the
    /// $19 fee couldn't be charged.
    /// </exception>
    /// <exception cref="RateLimitException">
    /// <c>whatsapp_signup_limit_reached</c> (429) after 5 failed, charged
    /// signups in 24 hours. Not retried; try again the next day.
    /// </exception>
    /// <param name="request">The number, and for adding it by code the business account, delivery method and display name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The signup: with a <c>ConnectUrl</c> for a Facebook connection, or <c>verifying</c> for a number added by code</returns>
    public async Task<WhatsAppSignupSession> CreateAsync(
        StartWhatsAppSignupRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ValidationException("A signup 'request' is required");
        WhatsAppResource.ValidatePhone(request.PhoneNumber);
        if (request.BusinessAccountId != null && string.IsNullOrWhiteSpace(request.BusinessAccountId))
            throw new ValidationException("businessAccountId must be a non-empty string");

        var addingByCode = !string.IsNullOrEmpty(request.BusinessAccountId);
        using var doc = await _client.PostAsync("/whatsapp/signup", request, null, true, cancellationToken, retryUnknownOutcomes: !addingByCode);
        return JsonSerializer.Deserialize<WhatsAppSignupSession>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    /// <summary>
    /// Submit the 6-digit code Meta sent to a number being added by code.
    ///
    /// Spaces and dashes in <paramref name="code"/> are ignored. A correct
    /// code connects the number and returns the signup with status
    /// <c>active</c> (and fires <c>whatsapp_account.connected</c>). A signup
    /// that is already active is returned as it is. Requires a live API key
    /// with the <c>whatsapp:write</c> scope and, in a team workspace, an
    /// owner or admin (<c>settings:write</c>).
    /// </summary>
    /// <exception cref="ValidationException">
    /// 400 <c>invalid_verification_code</c> when the code isn't 6 digits. 422
    /// <c>whatsapp_verification_code_invalid</c> for a wrong code; the
    /// response body's <c>attemptsRemaining</c> (read it from
    /// <see cref="SendlyException.ResponseBody"/>) says how many tries are left.
    /// </exception>
    /// <exception cref="NotFoundException">404 <c>signup_not_found</c>.</exception>
    /// <exception cref="SendlyException">
    /// 409 <c>whatsapp_verification_failed</c> after 5 wrong codes (the
    /// session failed and the fee is refunded), 409
    /// <c>whatsapp_verification_busy</c> while another code for the number is
    /// being checked (try again), 409 <c>signup_not_active</c> when the signup
    /// isn't waiting for a code or is more than 3 hours old, 502
    /// <c>whatsapp_verification_unavailable</c> when WhatsApp couldn't check
    /// the code (the attempt isn't counted), or 502
    /// <c>whatsapp_activation_pending</c> when the code was accepted but the
    /// connection couldn't be finished; Sendly is alerted, so check back with
    /// <see cref="GetAsync"/>. A 5xx, a 408, a timeout or a network failure
    /// (a <see cref="NetworkException"/>) is thrown on the first attempt and
    /// never retried automatically: every submission uses up one of the 5
    /// attempts, and after <c>whatsapp_activation_pending</c> WhatsApp has
    /// already accepted the code. Check <see cref="GetAsync"/> before
    /// submitting again. Only a 429 the client waits out is retried.
    /// </exception>
    /// <param name="id">The signup's id</param>
    /// <param name="code">The 6-digit code</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The signup, <c>active</c> once the code is accepted</returns>
    public async Task<WhatsAppSignup> VerifyAsync(
        string id,
        string code,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(id))
            throw new ValidationException("A signup 'id' is required");
        if (string.IsNullOrEmpty(code))
            throw new ValidationException("A verification 'code' is required");

        var request = new VerifyWhatsAppSignupRequest { Code = code };
        using var doc = await _client.PostAsync($"/whatsapp/signup/{Uri.EscapeDataString(id)}/verify", request, null, true, cancellationToken, retryUnknownOutcomes: false);
        return JsonSerializer.Deserialize<WhatsAppSignup>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    /// <summary>
    /// Ask Meta to send a new code to a number being added by code,
    /// optionally switching between text (<c>sms</c>) and voice call
    /// (<c>voice</c>); leave <paramref name="verificationMethod"/> null for a
    /// text. Codes can be requested at most every 30 seconds, counted from
    /// the signup's last change, including a code submission. A signup that
    /// is already active is returned as it is. Requires a live API key with
    /// the <c>whatsapp:write</c> scope and, in a team workspace, an owner or
    /// admin (<c>settings:write</c>).
    /// </summary>
    /// <exception cref="RateLimitException">
    /// 429 <c>whatsapp_verification_resend_too_soon</c>, thrown at once with
    /// <see cref="RateLimitException.RetryAfter"/> set to the seconds left.
    /// </exception>
    /// <exception cref="ValidationException">
    /// 422 <c>whatsapp_verification_resend_failed</c> when WhatsApp wouldn't
    /// send another code yet; wait a few minutes.
    /// </exception>
    /// <exception cref="NotFoundException">404 <c>signup_not_found</c>.</exception>
    /// <exception cref="SendlyException">
    /// 409 <c>signup_not_active</c>, or 502
    /// <c>whatsapp_verification_resend_failed</c> when WhatsApp couldn't be
    /// reached, which the client retries like any 5xx before throwing.
    /// </exception>
    /// <param name="id">The signup's id</param>
    /// <param name="verificationMethod"><c>sms</c> or <c>voice</c> (see <see cref="WhatsAppVerificationMethod"/>); null sends a text</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The signup, still <c>verifying</c></returns>
    public async Task<WhatsAppSignup> ResendAsync(
        string id,
        string? verificationMethod = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(id))
            throw new ValidationException("A signup 'id' is required");

        var request = new ResendWhatsAppSignupCodeRequest { VerificationMethod = verificationMethod };
        using var doc = await _client.PostAsync($"/whatsapp/signup/{Uri.EscapeDataString(id)}/resend", request, cancellationToken);
        return JsonSerializer.Deserialize<WhatsAppSignup>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    /// <summary>
    /// Get the status of a WhatsApp signup. Needs the <c>whatsapp:read</c>
    /// scope; test keys work.
    /// </summary>
    /// <param name="id">The signup's id</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The signup status</returns>
    public async Task<WhatsAppSignup> GetAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(id))
            throw new ValidationException("A signup 'id' is required");

        using var doc = await _client.GetAsync($"/whatsapp/signup/{Uri.EscapeDataString(id)}", null, cancellationToken);
        return JsonSerializer.Deserialize<WhatsAppSignup>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }
}

/// <summary>
/// List the numbers connected (or connecting) to WhatsApp and manage their
/// business profiles, profile photos, conversational components and
/// calling.
/// </summary>
public class WhatsAppSendersResource
{
    private readonly SendlyClient _client;

    public WhatsAppSendersResource(SendlyClient client)
    {
        _client = client;
    }

    /// <summary>
    /// List your WhatsApp senders.
    ///
    /// Returns the numbers connected (or connecting) to WhatsApp on your
    /// workspace, newest first. An empty list means no number is connected
    /// yet — start one with <see cref="WhatsAppSignupResource.CreateAsync(string, CancellationToken)"/>.
    /// Needs the <c>whatsapp:read</c> scope; test keys work.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Your senders with connection status and quality rating</returns>
    public async Task<WhatsAppSendersListResponse> ListAsync(
        CancellationToken cancellationToken = default)
    {
        using var doc = await _client.GetAsync("/whatsapp/senders", null, cancellationToken);
        return JsonSerializer.Deserialize<WhatsAppSendersListResponse>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    /// <summary>
    /// Get a sender's WhatsApp business profile.
    ///
    /// Returns what recipients see on the sender's contact card: display name,
    /// photo, category, about line, description, and contact details. The
    /// sender must be actively connected to WhatsApp — otherwise the API
    /// responds 404 <c>whatsapp_sender_not_connected</c>. Needs the
    /// <c>whatsapp:read</c> scope; test keys work.
    /// </summary>
    /// <param name="phoneNumber">The WhatsApp-connected sender, in E.164 format</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The sender's business profile</returns>
    public async Task<WhatsAppSenderProfile> GetProfileAsync(
        string phoneNumber,
        CancellationToken cancellationToken = default)
    {
        WhatsAppResource.ValidatePhone(phoneNumber);

        using var doc = await _client.GetAsync(
            $"/whatsapp/senders/{Uri.EscapeDataString(phoneNumber)}/profile", null, cancellationToken);
        return JsonSerializer.Deserialize<WhatsAppSenderProfile>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    /// <summary>
    /// Update a sender's WhatsApp business profile.
    ///
    /// Supply only the fields to change (at least one); omitted fields keep
    /// their current value. <see cref="UpdateWhatsAppSenderProfileRequest.About"/>
    /// is capped at 139 characters and
    /// <see cref="UpdateWhatsAppSenderProfileRequest.Description"/> at 512.
    /// Requires a live API key with the <c>whatsapp:write</c> scope and, in a
    /// team workspace, an owner or admin (<c>settings:write</c>).
    /// </summary>
    /// <param name="phoneNumber">The WhatsApp-connected sender, in E.164 format</param>
    /// <param name="request">The profile fields to change</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated business profile</returns>
    public async Task<WhatsAppSenderProfile> UpdateProfileAsync(
        string phoneNumber,
        UpdateWhatsAppSenderProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        WhatsAppResource.ValidatePhone(phoneNumber);
        if (request == null)
            throw new ValidationException("A profile update 'request' is required");

        using var doc = await _client.PatchAsync(
            $"/whatsapp/senders/{Uri.EscapeDataString(phoneNumber)}/profile", request, cancellationToken);
        return JsonSerializer.Deserialize<WhatsAppSenderProfile>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    /// <summary>
    /// Upload a sender's WhatsApp profile photo from a local file, replacing
    /// the current one. The content type is taken from the file extension.
    /// See <see cref="UploadProfilePhotoAsync(string, Stream, string, string, CancellationToken)"/>.
    /// </summary>
    /// <param name="phoneNumber">The WhatsApp-connected sender, in E.164 format</param>
    /// <param name="filePath">Path to a JPEG or PNG on disk</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The sender's business profile with the new photo</returns>
    public async Task<WhatsAppSenderProfile> UploadProfilePhotoAsync(
        string phoneNumber,
        string filePath,
        CancellationToken cancellationToken = default)
    {
        WhatsAppResource.ValidatePhone(phoneNumber);
        if (string.IsNullOrEmpty(filePath))
            throw new ValidationException("File path is required");

        if (!File.Exists(filePath))
            throw new ValidationException($"File not found: {filePath}");

        var fileName = Path.GetFileName(filePath);
        using var stream = File.OpenRead(filePath);
        return await UploadProfilePhotoAsync(phoneNumber, stream, fileName, MediaResource.GetContentType(fileName), cancellationToken);
    }

    /// <summary>
    /// Upload a sender's WhatsApp profile photo, replacing the current one.
    ///
    /// The photo is sent as the multipart field <c>file</c>. It must be a
    /// JPEG or PNG (the API checks the file's bytes, not its name or content
    /// type) of at most 5 MB. WhatsApp wants it square and at least 192
    /// pixels wide (640 recommended). Requires a live API key with the
    /// <c>whatsapp:write</c> scope and, in a team workspace, an owner or
    /// admin (<c>settings:write</c>). The sender must be actively connected,
    /// otherwise the API answers 404 <c>whatsapp_sender_not_connected</c>.
    /// </summary>
    /// <exception cref="ValidationException">
    /// 400 <c>file_required</c> (an empty file),
    /// <c>whatsapp_profile_photo_invalid</c> (not a JPEG or PNG) or
    /// <c>invalid_request</c> (a malformed upload).
    /// </exception>
    /// <exception cref="NotFoundException">404 <c>whatsapp_sender_not_connected</c>.</exception>
    /// <exception cref="SendlyException">
    /// 413 <c>whatsapp_profile_photo_too_large</c> (over 5 MB), or 502
    /// <c>whatsapp_profile_update_failed</c> when WhatsApp refused the photo
    /// or couldn't be reached; check the photo is square and at least 192
    /// pixels wide, then try again. A 5xx, a 408, a timeout or a network
    /// failure (a <see cref="NetworkException"/>) is thrown on the first
    /// attempt and never retried automatically. Only a 429 the client waits
    /// out is retried.
    /// </exception>
    /// <param name="phoneNumber">The WhatsApp-connected sender, in E.164 format</param>
    /// <param name="stream">The photo's bytes. A stream that can't seek is read into memory first, so an upload retried after a 429 sends the whole photo again.</param>
    /// <param name="fileName">File name with extension</param>
    /// <param name="contentType">MIME content type, <c>image/jpeg</c> or <c>image/png</c></param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The sender's business profile with the new photo</returns>
    public async Task<WhatsAppSenderProfile> UploadProfilePhotoAsync(
        string phoneNumber,
        Stream stream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        WhatsAppResource.ValidatePhone(phoneNumber);
        if (stream == null)
            throw new ValidationException("Stream is required");

        if (string.IsNullOrEmpty(fileName))
            throw new ValidationException("File name is required");

        if (string.IsNullOrEmpty(contentType))
            throw new ValidationException("Content type is required");

        var photo = stream;
        if (!stream.CanSeek)
        {
            photo = new MemoryStream();
            await stream.CopyToAsync(photo, cancellationToken);
            photo.Position = 0;
        }

        using var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(photo);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(streamContent, "file", fileName);

        using var doc = await _client.PostContentAsync(
            $"/whatsapp/senders/{Uri.EscapeDataString(phoneNumber)}/profile/photo", content, cancellationToken, retryUnknownOutcomes: false);
        return JsonSerializer.Deserialize<WhatsAppSenderProfile>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    /// <summary>
    /// Remove a sender's WhatsApp profile photo. Requires a live API key with
    /// the <c>whatsapp:write</c> scope and, in a team workspace, an owner or
    /// admin (<c>settings:write</c>).
    /// </summary>
    /// <exception cref="NotFoundException">404 <c>whatsapp_sender_not_connected</c>.</exception>
    /// <exception cref="SendlyException">
    /// 502 <c>whatsapp_profile_update_failed</c> when the photo couldn't be
    /// removed, which the client retries like any 5xx before throwing.
    /// </exception>
    /// <param name="phoneNumber">The WhatsApp-connected sender, in E.164 format</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The sender's business profile as WhatsApp now reports it, normally with <c>ProfilePhotoUrl</c> null</returns>
    public async Task<WhatsAppSenderProfile> DeleteProfilePhotoAsync(
        string phoneNumber,
        CancellationToken cancellationToken = default)
    {
        WhatsAppResource.ValidatePhone(phoneNumber);

        using var doc = await _client.DeleteAsync(
            $"/whatsapp/senders/{Uri.EscapeDataString(phoneNumber)}/profile/photo", cancellationToken);
        return JsonSerializer.Deserialize<WhatsAppSenderProfile>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    /// <summary>
    /// Get a sender's conversational components: the ice breakers shown as
    /// tappable suggestions when someone opens a chat with the business for
    /// the first time, and the commands shown when they type "/". Needs the
    /// <c>whatsapp:read</c> scope; test keys work.
    /// </summary>
    /// <exception cref="NotFoundException">404 <c>whatsapp_sender_not_connected</c>.</exception>
    /// <exception cref="SendlyException">
    /// 502 <c>whatsapp_conversational_components_fetch_failed</c>, which the
    /// client retries like any 5xx before throwing.
    /// </exception>
    /// <param name="phoneNumber">The WhatsApp-connected sender, in E.164 format</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The sender's ice breakers and commands</returns>
    public async Task<WhatsAppConversationalComponents> GetConversationalComponentsAsync(
        string phoneNumber,
        CancellationToken cancellationToken = default)
    {
        WhatsAppResource.ValidatePhone(phoneNumber);

        using var doc = await _client.GetAsync(
            $"/whatsapp/senders/{Uri.EscapeDataString(phoneNumber)}/conversational_components", null, cancellationToken);
        return JsonSerializer.Deserialize<WhatsAppConversationalComponents>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    /// <summary>
    /// Replace a sender's ice breakers, commands, or both.
    ///
    /// Each list you set replaces the stored one, an empty list clears it,
    /// and a list left null is kept. Set at least one. Up to 4 ice breakers
    /// of 1-80 characters each, with no duplicates (ignoring case). Up to 30
    /// commands; a command is 1-32 letters, digits or underscores (a leading
    /// "/" is dropped) with a 1-256 character description, and no command
    /// may appear twice. Requires a live API key with the
    /// <c>whatsapp:write</c> scope and, in a team workspace, an owner or
    /// admin (<c>settings:write</c>).
    /// </summary>
    /// <exception cref="ValidationException">
    /// 400 <c>invalid_request</c>, whose message says which rule failed.
    /// </exception>
    /// <exception cref="NotFoundException">404 <c>whatsapp_sender_not_connected</c>.</exception>
    /// <exception cref="SendlyException">
    /// 502 <c>whatsapp_conversational_components_update_failed</c>, which the
    /// client retries like any 5xx before throwing.
    /// </exception>
    /// <param name="phoneNumber">The WhatsApp-connected sender, in E.164 format</param>
    /// <param name="request">The lists to replace</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The sender's ice breakers and commands as stored</returns>
    public async Task<WhatsAppConversationalComponents> UpdateConversationalComponentsAsync(
        string phoneNumber,
        UpdateWhatsAppConversationalComponentsRequest request,
        CancellationToken cancellationToken = default)
    {
        WhatsAppResource.ValidatePhone(phoneNumber);
        if (request == null)
            throw new ValidationException("A conversational components 'request' is required");

        using var doc = await _client.PatchAsync(
            $"/whatsapp/senders/{Uri.EscapeDataString(phoneNumber)}/conversational_components", request, cancellationToken);
        return JsonSerializer.Deserialize<WhatsAppConversationalComponents>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    /// <summary>
    /// Switch WhatsApp calling on or off for a sender.
    ///
    /// Once calling is on, a WhatsApp user calling the number rings exactly
    /// like a phone call, in the dashboard or to an AI agent according to the
    /// number's voice settings, and is billed at the normal inbound rate.
    /// Turning it on needs voice switched on for the number first (see
    /// <c>client.Voice.Numbers</c>). There is no API for placing WhatsApp
    /// calls. Requires a live API key with the <c>whatsapp:write</c> scope
    /// and, in a team workspace, an owner or admin (<c>settings:write</c>).
    /// </summary>
    /// <exception cref="ValidationException">
    /// 422 <c>whatsapp_calling_unavailable</c> when Meta refused: calling
    /// needs the account at the 2,000-recipients-a-day messaging limit and an
    /// approved display name.
    /// </exception>
    /// <exception cref="NotFoundException">404 <c>whatsapp_sender_not_connected</c>.</exception>
    /// <exception cref="SendlyException">
    /// 409 <c>voice_not_enabled</c> when turning it on for a number whose
    /// voice is off, or 502 <c>whatsapp_calling_update_failed</c>, which the
    /// client retries like any 5xx before throwing.
    /// </exception>
    /// <param name="phoneNumber">The WhatsApp-connected sender, in E.164 format</param>
    /// <param name="enabled">True to switch calling on, false to switch it off</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The sender's calling settings</returns>
    public async Task<WhatsAppCallingSettings> SetCallingAsync(
        string phoneNumber,
        bool enabled,
        CancellationToken cancellationToken = default)
    {
        WhatsAppResource.ValidatePhone(phoneNumber);

        var request = new SetWhatsAppCallingRequest { Enabled = enabled };
        using var doc = await _client.PatchAsync(
            $"/whatsapp/senders/{Uri.EscapeDataString(phoneNumber)}/calling", request, cancellationToken);
        return JsonSerializer.Deserialize<WhatsAppCallingSettings>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }
}

/// <summary>
/// Manage Meta-reviewed WhatsApp message templates.
/// </summary>
public class WhatsAppTemplatesResource
{
    private readonly SendlyClient _client;

    public WhatsAppTemplatesResource(SendlyClient client)
    {
        _client = client;
    }

    /// <summary>
    /// List your WhatsApp templates. Needs the <c>whatsapp:read</c> scope; test
    /// keys work.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Your templates with review status and quality rating</returns>
    public async Task<WhatsAppTemplateListResponse> ListAsync(
        CancellationToken cancellationToken = default)
    {
        using var doc = await _client.GetAsync("/whatsapp/templates", null, cancellationToken);
        return JsonSerializer.Deserialize<WhatsAppTemplateListResponse>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    /// <summary>
    /// Create a template and submit it to Meta for review.
    ///
    /// Review usually takes 24-48h; the template is usable once its status is
    /// <c>APPROVED</c>. Requires a live API key with the <c>whatsapp:write</c>
    /// scope and, in a team workspace, an owner, admin or member
    /// (<c>templates:write</c>). The category is required, with no default. A
    /// marketing template without an opt-out button is still accepted, with a
    /// warning.
    /// </summary>
    /// <exception cref="NotFoundException">
    /// 404 <c>whatsapp_sender_not_connected</c> when the sender isn't connected
    /// to WhatsApp; this is checked first.
    /// </exception>
    /// <exception cref="ValidationException">
    /// A 400 whose <c>ApiErrorCode</c> is the <c>template_*</c> reason and
    /// whose message says what to fix: <c>template_category_invalid</c>
    /// (category missing or not one of the three),
    /// <c>template_authentication_otp_button_required</c>,
    /// <c>template_authentication_no_links</c> (a link in the body or a URL
    /// button on an authentication template) or
    /// <c>template_header_variable_unsupported</c> for a header containing
    /// <c>{{n}}</c>.
    /// </exception>
    /// <param name="request">The template definition</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created template (status <c>PENDING</c>), with any submission warnings</returns>
    public async Task<WhatsAppTemplate> CreateAsync(
        CreateWhatsAppTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        WhatsAppResource.ValidatePhone(request?.Sender);

        using var doc = await _client.PostAsync("/whatsapp/templates", request, cancellationToken);
        return JsonSerializer.Deserialize<WhatsAppTemplate>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    /// <summary>
    /// Edit an APPROVED or REJECTED template and resubmit it for review.
    ///
    /// This is the recovery path for rejections: template names are locked for
    /// ~30 days after deletion, so editing a rejected template (rather than
    /// deleting and re-creating it) is the way to fix it. The updated template
    /// goes back to <c>PENDING</c> review. The category can't be changed.
    /// Requires a live API key with the <c>whatsapp:write</c> scope and, in a
    /// team workspace, an owner, admin or member (<c>templates:write</c>).
    /// </summary>
    /// <param name="id">The template's id</param>
    /// <param name="request">The fields to change (omitted fields are kept)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated template (status <c>PENDING</c>)</returns>
    public async Task<WhatsAppTemplate> UpdateAsync(
        string id,
        UpdateWhatsAppTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(id))
            throw new ValidationException("A template 'id' is required");

        using var doc = await _client.PatchAsync($"/whatsapp/templates/{Uri.EscapeDataString(id)}", request, cancellationToken);
        return JsonSerializer.Deserialize<WhatsAppTemplate>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    /// <summary>
    /// Delete a template.
    ///
    /// Meta locks a deleted template's name for ~30 days — re-creating it
    /// fails with <c>template_name_locked</c> until the lock lifts. To fix a
    /// rejected template, prefer <see cref="UpdateAsync"/>. Requires a live
    /// API key with the <c>whatsapp:write</c> scope and, in a team workspace,
    /// an owner, admin or member (<c>templates:write</c>).
    /// </summary>
    /// <param name="id">The template's id</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Deletion confirmation</returns>
    public async Task<WhatsAppTemplateDeletedResponse> DeleteAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(id))
            throw new ValidationException("A template 'id' is required");

        using var doc = await _client.DeleteAsync($"/whatsapp/templates/{Uri.EscapeDataString(id)}", cancellationToken);
        return JsonSerializer.Deserialize<WhatsAppTemplateDeletedResponse>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }
}

/// <summary>
/// Body for <see cref="WhatsAppSignupResource.CreateAsync(StartWhatsAppSignupRequest, CancellationToken)"/>.
/// </summary>
public class StartWhatsAppSignupRequest
{
    /// <summary>
    /// The number to connect, in E.164 format. Must be an active number in
    /// your workspace (provisioned, purchased, or fully ported into Sendly).
    /// </summary>
    [JsonPropertyName("phoneNumber")]
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// The WhatsApp Business account to add the number to, as shown on
    /// <see cref="WhatsAppSender.BusinessAccountId"/>. Set it to add a number
    /// to an account this workspace already connected, verified by a code
    /// instead of the Facebook step. Leave it null for a Facebook connection.
    /// </summary>
    [JsonPropertyName("businessAccountId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? BusinessAccountId { get; set; }

    /// <summary>
    /// How Meta delivers the code when adding a number by code: <c>sms</c>
    /// (the default) or <c>voice</c>. See <see cref="WhatsAppVerificationMethod"/>.
    /// </summary>
    [JsonPropertyName("verificationMethod")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? VerificationMethod { get; set; }

    /// <summary>
    /// The name WhatsApp shows for the number when adding it by code (at
    /// most 512 characters). Defaults to the display name of the account's
    /// existing sender, else its business name.
    /// </summary>
    [JsonPropertyName("displayName")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DisplayName { get; set; }
}

/// <summary>
/// How Meta delivers the code for a number added by code. Values are plain
/// strings.
/// </summary>
public static class WhatsAppVerificationMethod
{
    /// <summary>A text message to the number.</summary>
    public const string Sms = "sms";

    /// <summary>A voice call to the number.</summary>
    public const string Voice = "voice";
}

/// <summary>
/// Body for <see cref="WhatsAppSignupResource.VerifyAsync"/>.
/// </summary>
public class VerifyWhatsAppSignupRequest
{
    /// <summary>The 6-digit code; spaces and dashes are ignored.</summary>
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;
}

/// <summary>
/// Body for <see cref="WhatsAppSignupResource.ResendAsync"/>.
/// </summary>
public class ResendWhatsAppSignupCodeRequest
{
    /// <summary><c>sms</c> or <c>voice</c>; null sends a text.</summary>
    [JsonPropertyName("verificationMethod")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? VerificationMethod { get; set; }
}

/// <summary>
/// A newly started WhatsApp signup.
///
/// Hand <see cref="ConnectUrl"/> to a human — they open it in a browser and
/// log in with Facebook to link their WhatsApp Business Account. Poll
/// <see cref="WhatsAppSignupResource.GetAsync"/> with <see cref="Id"/> until
/// the status is <c>active</c>. A number added by code has no
/// <see cref="ConnectUrl"/> (it is empty) and starts as <c>verifying</c>:
/// submit the code with <see cref="WhatsAppSignupResource.VerifyAsync"/>.
/// </summary>
public class WhatsAppSignupSession
{
    /// <summary>Unique signup identifier — use with <see cref="WhatsAppSignupResource.GetAsync"/>.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Hosted connect page URL. A person must open this in a browser. Empty
    /// for a number added by code.
    /// </summary>
    [JsonPropertyName("connectUrl")]
    public string ConnectUrl { get; set; } = string.Empty;

    /// <summary>
    /// Current signup status: <c>initiated</c>, <c>registering</c>,
    /// <c>verifying</c> (a number added by code, waiting for its code),
    /// <c>active</c>, or <c>failed</c>. The API does not send <c>expired</c>.
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>The number being connected, for a number added by code; null for a Facebook connection.</summary>
    [JsonPropertyName("phoneNumber")]
    public string? PhoneNumber { get; set; }

    /// <summary>The WhatsApp Business account the number is being added to, for a number added by code; null otherwise.</summary>
    [JsonPropertyName("businessAccountId")]
    public string? BusinessAccountId { get; set; }

    /// <summary>Why the signup failed, for a number added by code; null otherwise. See <see cref="WhatsAppSignup.FailureReasons"/>.</summary>
    [JsonPropertyName("failureReasons")]
    public List<string>? FailureReasons { get; set; }

    /// <summary>How the code is delivered (<c>sms</c> or <c>voice</c>) while <c>verifying</c>; null otherwise.</summary>
    [JsonPropertyName("verificationMethod")]
    public string? VerificationMethod { get; set; }

    /// <summary>How many wrong codes may still be tried while <c>verifying</c> (5 at the start); null otherwise.</summary>
    [JsonPropertyName("verificationAttemptsRemaining")]
    public int? VerificationAttemptsRemaining { get; set; }

    /// <summary>When the status last changed (ISO 8601), for a number added by code; null for a Facebook connection.</summary>
    [JsonPropertyName("updatedAt")]
    public string? UpdatedAt { get; set; }
}

/// <summary>
/// A WhatsApp signup's current state, from
/// <see cref="WhatsAppSignupResource.GetAsync"/>.
/// </summary>
public class WhatsAppSignup
{
    /// <summary>Unique signup identifier.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Current signup status: <c>initiated</c> (waiting for a human to
    /// complete the connect URL), <c>registering</c> (WhatsApp is activating
    /// the number; activation usually takes a few minutes but can take hours,
    /// and a session that hasn't finished about 6 hours after it began fails
    /// with <c>registration_timeout</c> and the fee is refunded),
    /// <c>verifying</c> (a number added by code, waiting for its code),
    /// <c>active</c>, or <c>failed</c> (see <see cref="FailureReasons"/>).
    /// The API does not send <c>expired</c>.
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>The number being connected, in E.164 format.</summary>
    [JsonPropertyName("phoneNumber")]
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// The customer's WhatsApp Business Account id while the signup is
    /// <c>verifying</c> or <c>active</c>; null otherwise, including before
    /// the human completes the connect step.
    /// </summary>
    [JsonPropertyName("businessAccountId")]
    public string? BusinessAccountId { get; set; }

    /// <summary>
    /// Why the signup failed, when <see cref="Status"/> is <c>failed</c>; null
    /// otherwise. Holds one code: <c>setup_fee_payment_failed</c>,
    /// <c>signup_abandoned</c>, <c>meta_exchange_failed</c>,
    /// <c>registration_failed</c>, <c>waba_already_connected</c>,
    /// <c>waba_mismatch</c> (the WhatsApp Business Account chosen in the
    /// Facebook step doesn't hold the verified number),
    /// <c>registration_timeout</c> (activation hadn't finished about 6 hours
    /// after the session began), <c>phone_number_mismatch</c>, or, for a
    /// number added by code, <c>verification_start_failed</c> (WhatsApp
    /// couldn't start verifying the number), <c>verification_failed</c> (too
    /// many wrong codes) or <c>verification_expired</c> (the signup was
    /// left untouched for an hour, or was more than 3 hours old when the
    /// number was added again). If
    /// the connection fails, the $19 fee is refunded automatically.
    /// </summary>
    [JsonPropertyName("failureReasons")]
    public List<string>? FailureReasons { get; set; }

    /// <summary>When the status last changed (ISO 8601).</summary>
    [JsonPropertyName("updatedAt")]
    public string UpdatedAt { get; set; } = string.Empty;

    /// <summary>How the code is delivered (<c>sms</c> or <c>voice</c>) while <c>verifying</c>; null otherwise.</summary>
    [JsonPropertyName("verificationMethod")]
    public string? VerificationMethod { get; set; }

    /// <summary>How many wrong codes may still be tried while <c>verifying</c>; null otherwise.</summary>
    [JsonPropertyName("verificationAttemptsRemaining")]
    public int? VerificationAttemptsRemaining { get; set; }

    /// <summary>
    /// While <c>verifying</c>, the code from Meta's text once it has arrived
    /// on the number; null until then, and always null from
    /// <see cref="WhatsAppSignupResource.VerifyAsync"/> and
    /// <see cref="WhatsAppSignupResource.ResendAsync"/>. Until a code has been
    /// submitted, it is the newest code that has arrived since the signup
    /// started, so after a resend it is still the earlier code until the new
    /// one arrives. Once WhatsApp has checked a code, only a code that arrived
    /// after the last submission or resend is returned. A submission answered
    /// with 502 <c>whatsapp_verification_unavailable</c> is not counted, so
    /// the same unchecked code can come back, and submitting it again is safe.
    /// </summary>
    [JsonPropertyName("verificationCode")]
    public string? VerificationCode { get; set; }
}

/// <summary>
/// A number connected (or connecting) to WhatsApp.
/// <see cref="Status"/> is one of <c>pending</c> (connection in progress; not
/// sendable yet), <c>active</c>, or <c>suspended</c>.
/// </summary>
public class WhatsAppSender
{
    /// <summary>The sender, in E.164 format.</summary>
    [JsonPropertyName("phoneNumber")]
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// The name recipients see — chosen during the connect flow and reviewed
    /// by Meta; null until set.
    /// </summary>
    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    /// <summary>Connection status: <c>pending</c>, <c>active</c>, or <c>suspended</c>.</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>Meta quality rating (e.g. "GREEN"), or null before first rating.</summary>
    [JsonPropertyName("qualityRating")]
    public string? QualityRating { get; set; }

    /// <summary>
    /// The WhatsApp Business account the number belongs to; null while
    /// <c>pending</c>. Pass it as
    /// <see cref="StartWhatsAppSignupRequest.BusinessAccountId"/> to add
    /// another number to the same account.
    /// </summary>
    [JsonPropertyName("businessAccountId")]
    public string? BusinessAccountId { get; set; }

    /// <summary>
    /// The business name on the WhatsApp Business account; null while
    /// <c>pending</c>, and when the account has no business name on record.
    /// </summary>
    [JsonPropertyName("businessName")]
    public string? BusinessName { get; set; }

    /// <summary>
    /// Whether WhatsApp calling is switched on for the number. Change it with
    /// <see cref="WhatsAppSendersResource.SetCallingAsync"/>.
    /// </summary>
    [JsonPropertyName("callingEnabled")]
    public bool CallingEnabled { get; set; }

    /// <summary>
    /// Whether Meta allows business-initiated WhatsApp calls from the
    /// number: false for every +1 number (the US, Canada and the rest of the
    /// North American numbering plan), +20 (Egypt), +84 (Vietnam) and +234
    /// (Nigeria) numbers.
    /// </summary>
    [JsonPropertyName("outboundCallingAllowed")]
    public bool OutboundCallingAllowed { get; set; }

    /// <summary>When the sender was connected (ISO 8601).</summary>
    [JsonPropertyName("createdAt")]
    public string CreatedAt { get; set; } = string.Empty;
}

/// <summary>
/// Response from <see cref="WhatsAppSendersResource.ListAsync"/>.
/// </summary>
public class WhatsAppSendersListResponse
{
    [JsonPropertyName("senders")]
    public List<WhatsAppSender> Senders { get; set; } = new();
}

/// <summary>
/// A WhatsApp sender's business profile — what recipients see when they open
/// the sender's contact card.
/// </summary>
public class WhatsAppSenderProfile
{
    /// <summary>The sender, in E.164 format.</summary>
    [JsonPropertyName("phoneNumber")]
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>The name recipients see; null until set.</summary>
    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    /// <summary>
    /// Profile photo URL, or null when none is set. Change it with
    /// <see cref="WhatsAppSendersResource.UploadProfilePhotoAsync(string, Stream, string, string, CancellationToken)"/>
    /// and remove it with <see cref="WhatsAppSendersResource.DeleteProfilePhotoAsync"/>.
    /// </summary>
    [JsonPropertyName("profilePhotoUrl")]
    public string? ProfilePhotoUrl { get; set; }

    /// <summary>Business category (e.g. "Retail"), or null when not set.</summary>
    [JsonPropertyName("category")]
    public string? Category { get; set; }

    /// <summary>Short about line (max 139 characters), or null when not set.</summary>
    [JsonPropertyName("about")]
    public string? About { get; set; }

    /// <summary>Longer business description (max 512 characters), or null when not set.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>Contact email, or null when not set.</summary>
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    /// <summary>Website URL, or null when not set.</summary>
    [JsonPropertyName("website")]
    public string? Website { get; set; }

    /// <summary>Street address, or null when not set.</summary>
    [JsonPropertyName("address")]
    public string? Address { get; set; }
}

/// <summary>
/// Body for <see cref="WhatsAppSendersResource.UpdateProfileAsync"/>. Supply
/// only the fields to change; omitted (null) fields keep their current value.
/// At least one field is required.
/// </summary>
public class UpdateWhatsAppSenderProfileRequest
{
    /// <summary>Replacement display name.</summary>
    [JsonPropertyName("displayName")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DisplayName { get; set; }

    /// <summary>Replacement about line (max 139 characters).</summary>
    [JsonPropertyName("about")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? About { get; set; }

    /// <summary>Replacement business description (max 512 characters).</summary>
    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; }

    /// <summary>Replacement business category.</summary>
    [JsonPropertyName("category")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Category { get; set; }

    /// <summary>Replacement contact email.</summary>
    [JsonPropertyName("email")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Email { get; set; }

    /// <summary>Replacement website URL.</summary>
    [JsonPropertyName("website")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Website { get; set; }

    /// <summary>Replacement street address.</summary>
    [JsonPropertyName("address")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Address { get; set; }
}

/// <summary>
/// A sender's conversational components, from
/// <see cref="WhatsAppSendersResource.GetConversationalComponentsAsync"/>.
/// </summary>
public class WhatsAppConversationalComponents
{
    /// <summary>The sender, in E.164 format.</summary>
    [JsonPropertyName("phoneNumber")]
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// Tappable suggestions shown when someone opens a chat with the
    /// business for the first time; empty when none are set.
    /// </summary>
    [JsonPropertyName("iceBreakers")]
    public List<string> IceBreakers { get; set; } = new();

    /// <summary>Commands shown when the customer types "/"; empty when none are set.</summary>
    [JsonPropertyName("commands")]
    public List<WhatsAppCommand> Commands { get; set; } = new();
}

/// <summary>
/// A command shown when the customer types "/".
/// </summary>
public class WhatsAppCommand
{
    /// <summary>The command, without the "/": 1-32 letters, digits or underscores.</summary>
    [JsonPropertyName("command")]
    public string Command { get; set; } = string.Empty;

    /// <summary>What the command does, 1-256 characters.</summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Body for <see cref="WhatsAppSendersResource.UpdateConversationalComponentsAsync"/>.
/// Each list you set replaces the stored one and an empty list clears it; a
/// list left null is not sent and keeps its value. Set at least one.
/// </summary>
public class UpdateWhatsAppConversationalComponentsRequest
{
    /// <summary>Up to 4 ice breakers, 1-80 characters each, with no duplicates.</summary>
    [JsonPropertyName("iceBreakers")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? IceBreakers { get; set; }

    /// <summary>Up to 30 commands, with no command listed twice.</summary>
    [JsonPropertyName("commands")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<WhatsAppCommand>? Commands { get; set; }
}

/// <summary>
/// Body for <see cref="WhatsAppSendersResource.SetCallingAsync"/>.
/// </summary>
public class SetWhatsAppCallingRequest
{
    /// <summary>True to switch WhatsApp calling on, false to switch it off.</summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }
}

/// <summary>
/// A sender's WhatsApp calling settings, from
/// <see cref="WhatsAppSendersResource.SetCallingAsync"/>.
/// </summary>
public class WhatsAppCallingSettings
{
    /// <summary>The sender, in E.164 format.</summary>
    [JsonPropertyName("phoneNumber")]
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>Whether WhatsApp calling is switched on.</summary>
    [JsonPropertyName("callingEnabled")]
    public bool CallingEnabled { get; set; }

    /// <summary>
    /// Whether Meta allows business-initiated WhatsApp calls from the
    /// number: false for every +1 number (the US, Canada and the rest of the
    /// North American numbering plan), +20 (Egypt), +84 (Vietnam) and +234
    /// (Nigeria) numbers.
    /// </summary>
    [JsonPropertyName("outboundCallingAllowed")]
    public bool OutboundCallingAllowed { get; set; }
}

/// <summary>
/// A button on a template.
///
/// - <c>url</c> — link button; <see cref="Url"/> is required and may contain a
/// <c>{{1}}</c> placeholder (supply <see cref="Example"/> values for review)
///
/// - <c>quick_reply</c> — tap-to-reply button (e.g. a "Stop promotions"
/// opt-out, recommended on marketing templates)
///
/// - <c>otp</c> — copy-code button; required on AUTHENTICATION templates
/// </summary>
public class WhatsAppTemplateButton
{
    /// <summary>Button type: <c>url</c>, <c>quick_reply</c>, or <c>otp</c>.</summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>Button label.</summary>
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    /// <summary>Link target (url buttons only); may contain a <c>{{1}}</c> placeholder.</summary>
    [JsonPropertyName("url")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Url { get; set; }

    /// <summary>Example values for a url placeholder, for Meta review.</summary>
    [JsonPropertyName("example")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Example { get; set; }
}

/// <summary>
/// Body for <see cref="WhatsAppTemplatesResource.CreateAsync"/>. Null values
/// are omitted.
/// </summary>
public class CreateWhatsAppTemplateRequest
{
    /// <summary>The WhatsApp-connected sending number this template belongs to, in E.164 format.</summary>
    [JsonPropertyName("sender")]
    public string Sender { get; set; } = string.Empty;

    /// <summary>Template name: lowercase letters, digits, and underscores (e.g. "order_shipped").</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Template language code (e.g. "en_US").</summary>
    [JsonPropertyName("language")]
    public string Language { get; set; } = string.Empty;

    /// <summary>
    /// Template category — drives Meta review rules and pricing:
    /// <c>AUTHENTICATION</c>, <c>UTILITY</c>, or <c>MARKETING</c> (the server
    /// uppercases it). Required, with no default: leaving it empty returns 400
    /// <c>template_category_invalid</c>. An update can't change it.
    /// </summary>
    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Body text. Use <c>{{1}}</c>, <c>{{2}}</c>, … for variables; every
    /// placeholder needs an example value in <see cref="Examples"/>.
    /// </summary>
    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;

    /// <summary>Optional footer line.</summary>
    [JsonPropertyName("footer")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Footer { get; set; }

    /// <summary>
    /// Optional text header. It is fixed text: a header containing <c>{{n}}</c>
    /// is refused with <c>template_header_variable_unsupported</c>, because
    /// sends fill only body and button variables.
    /// </summary>
    [JsonPropertyName("header")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Header { get; set; }

    /// <summary>Optional buttons.</summary>
    [JsonPropertyName("buttons")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<WhatsAppTemplateButton>? Buttons { get; set; }

    /// <summary>
    /// Example values for body placeholders, keyed by placeholder number:
    /// <c>{ ["1"] = "Acme Inc", ["2"] = "#4821" }</c>. Required when the body
    /// has variables — Meta reviews templates with these examples filled in.
    /// </summary>
    [JsonPropertyName("examples")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string>? Examples { get; set; }
}

/// <summary>
/// Body for <see cref="WhatsAppTemplatesResource.UpdateAsync"/>. Supply only
/// the fields to change; omitted (null) fields keep their current value.
/// </summary>
public class UpdateWhatsAppTemplateRequest
{
    /// <summary>Replacement body text.</summary>
    [JsonPropertyName("body")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Body { get; set; }

    /// <summary>Replacement footer.</summary>
    [JsonPropertyName("footer")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Footer { get; set; }

    /// <summary>
    /// Replacement text header. It can't contain <c>{{n}}</c> variables
    /// (<c>template_header_variable_unsupported</c>).
    /// </summary>
    [JsonPropertyName("header")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Header { get; set; }

    /// <summary>Replacement buttons.</summary>
    [JsonPropertyName("buttons")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<WhatsAppTemplateButton>? Buttons { get; set; }

    /// <summary>Replacement example values for body placeholders.</summary>
    [JsonPropertyName("examples")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string>? Examples { get; set; }
}

/// <summary>
/// A WhatsApp message template.
///
/// <see cref="Status"/> is one of <c>PENDING</c> (Meta review usually takes
/// 24-48h), <c>APPROVED</c>, <c>REJECTED</c> (edit it with
/// <see cref="WhatsAppTemplatesResource.UpdateAsync"/> to resubmit — template
/// names are locked for ~30 days after deletion, so editing is the way out),
/// <c>PAUSED</c>, or <c>DISABLED</c> (quality-suspended by Meta). Meta may
/// report other statuses; they come through in uppercase.
/// </summary>
public class WhatsAppTemplate
{
    /// <summary>Unique template identifier.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Template name.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Template language code.</summary>
    [JsonPropertyName("language")]
    public string Language { get; set; } = string.Empty;

    /// <summary>
    /// Category: <c>AUTHENTICATION</c>, <c>UTILITY</c>, or <c>MARKETING</c>.
    /// Meta may reclassify; this value drives pricing.
    /// </summary>
    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    /// <summary>Review status.</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>Meta quality rating (e.g. "GREEN"), or null before first rating.</summary>
    [JsonPropertyName("qualityRating")]
    public string? QualityRating { get; set; }

    /// <summary>Why Meta rejected the template, when <see cref="Status"/> is <c>REJECTED</c>.</summary>
    [JsonPropertyName("rejectionReason")]
    public string? RejectionReason { get; set; }

    /// <summary>When the template was created (ISO 8601).</summary>
    [JsonPropertyName("createdAt")]
    public string CreatedAt { get; set; } = string.Empty;

    /// <summary>When the template was last updated (ISO 8601).</summary>
    [JsonPropertyName("updatedAt")]
    public string UpdatedAt { get; set; } = string.Empty;

    /// <summary>
    /// Non-blocking submission warnings (e.g. an unapproved display name, or a
    /// marketing template without an opt-out button). Present on create
    /// responses when applicable.
    /// </summary>
    [JsonPropertyName("warnings")]
    public List<string>? Warnings { get; set; }
}

/// <summary>
/// Response from <see cref="WhatsAppTemplatesResource.ListAsync"/>.
/// </summary>
public class WhatsAppTemplateListResponse
{
    [JsonPropertyName("templates")]
    public List<WhatsAppTemplate> Templates { get; set; } = new();
}

/// <summary>
/// Response from <see cref="WhatsAppTemplatesResource.DeleteAsync"/>.
/// </summary>
public class WhatsAppTemplateDeletedResponse
{
    /// <summary>The deleted template's id.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Always true.</summary>
    [JsonPropertyName("deleted")]
    public bool Deleted { get; set; }
}

/// <summary>
/// Response from <see cref="WhatsAppResource.WindowAsync"/>.
/// </summary>
public class WhatsAppWindow
{
    /// <summary>True when a 24-hour customer-service window is currently open.</summary>
    [JsonPropertyName("open")]
    public bool Open { get; set; }

    /// <summary>
    /// When the window closes (ISO 8601). After it closes this is the past
    /// expiry, with <see cref="Open"/> false. Null when Sendly has no window
    /// on record for the pair; a free-form send may still go through then if
    /// WhatsApp reports an open window, and otherwise fails with
    /// <c>whatsapp_window_closed</c>.
    /// </summary>
    [JsonPropertyName("expiresAt")]
    public string? ExpiresAt { get; set; }
}
