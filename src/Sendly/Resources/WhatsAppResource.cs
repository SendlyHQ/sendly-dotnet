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
/// Connecting a number is a one-time $19 setup (no monthly fee) and always
/// ends with a human step: <see cref="WhatsAppSignupResource.CreateAsync"/>
/// returns a <c>ConnectUrl</c> that a person must open in a browser and log in
/// with Facebook to link their WhatsApp Business Account. Hand the URL to your
/// user — the connection cannot be completed programmatically.
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
    /// <c>ConnectUrl</c> a human must complete in a browser.
    /// </summary>
    public WhatsAppSignupResource Signup { get; }

    /// <summary>
    /// List the numbers connected (or connecting) to WhatsApp and manage their
    /// business profiles.
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
    /// message). Outside a window, send an approved template.
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
    /// Requires a live API key.
    /// </summary>
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
    /// Get the status of a WhatsApp signup.
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
/// business profiles.
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
    /// yet — start one with <see cref="WhatsAppSignupResource.CreateAsync"/>.
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
    /// responds 404 <c>whatsapp_sender_not_connected</c>.
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
    /// Requires a live API key.
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
    /// List your WhatsApp templates.
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
    /// <c>APPROVED</c>. Requires a live API key.
    /// </summary>
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
    /// goes back to <c>PENDING</c> review. Requires a live API key.
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
    /// API key.
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
/// Body for <see cref="WhatsAppSignupResource.CreateAsync"/>.
/// </summary>
public class StartWhatsAppSignupRequest
{
    /// <summary>
    /// The number to connect, in E.164 format. Must be an active number in
    /// your workspace (provisioned, purchased, or fully ported into Sendly).
    /// </summary>
    [JsonPropertyName("phoneNumber")]
    public string PhoneNumber { get; set; } = string.Empty;
}

/// <summary>
/// A newly started WhatsApp signup.
///
/// Hand <see cref="ConnectUrl"/> to a human — they open it in a browser and
/// log in with Facebook to link their WhatsApp Business Account. Poll
/// <see cref="WhatsAppSignupResource.GetAsync"/> with <see cref="Id"/> until
/// the status is <c>active</c>.
/// </summary>
public class WhatsAppSignupSession
{
    /// <summary>Unique signup identifier — use with <see cref="WhatsAppSignupResource.GetAsync"/>.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Hosted connect page URL. A person must open this in a browser.</summary>
    [JsonPropertyName("connectUrl")]
    public string ConnectUrl { get; set; } = string.Empty;

    /// <summary>
    /// Current signup status: <c>initiated</c>, <c>registering</c>,
    /// <c>active</c>, <c>failed</c>, or <c>expired</c>.
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
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
    /// complete the connect URL), <c>registering</c>, <c>active</c>,
    /// <c>failed</c> (see <see cref="FailureReasons"/>), or <c>expired</c>.
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>The number being connected, in E.164 format.</summary>
    [JsonPropertyName("phoneNumber")]
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// The customer's WhatsApp Business Account id, once linked; null before
    /// the human completes the connect step.
    /// </summary>
    [JsonPropertyName("businessAccountId")]
    public string? BusinessAccountId { get; set; }

    /// <summary>Why the signup failed, when <see cref="Status"/> is <c>failed</c>; null otherwise.</summary>
    [JsonPropertyName("failureReasons")]
    public List<string>? FailureReasons { get; set; }

    /// <summary>When the status last changed (ISO 8601).</summary>
    [JsonPropertyName("updatedAt")]
    public string UpdatedAt { get; set; } = string.Empty;
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
    /// Profile photo URL, or null when none is set. Read-only — the photo is
    /// uploaded through the WhatsApp connect flow, not this API.
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
    /// <c>AUTHENTICATION</c>, <c>UTILITY</c>, or <c>MARKETING</c>.
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

    /// <summary>Optional text header.</summary>
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

    /// <summary>Replacement text header.</summary>
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
/// <c>PAUSED</c>, or <c>DISABLED</c> (quality-suspended by Meta).
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

    /// <summary>When the window closes (ISO 8601), or null when no window is open.</summary>
    [JsonPropertyName("expiresAt")]
    public string? ExpiresAt { get; set; }
}
