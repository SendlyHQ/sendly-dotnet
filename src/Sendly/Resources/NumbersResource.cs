using System.Text.Json;
using System.Text.Json.Serialization;
using Sendly.Exceptions;

namespace Sendly.Resources;

/// <summary>
/// Numbers Resource — buy and manage phone numbers.
///
/// Browse the countries and number types Sendly can provision, search for
/// available numbers (already priced for your account), list the numbers you
/// already own, and purchase a new one.
///
/// A purchase may complete instantly (<c>provisioning</c>) or require a
/// follow-up step. When the response status is <c>documents_required</c> or
/// <c>payment_required</c>, the response carries an
/// <see cref="BuyNumberResponse.Action"/> with a hosted Sendly URL plus a
/// short code. Hand the user that URL and code, wait for them to finish, then
/// re-call <see cref="BuyAsync"/> with the SAME body plus
/// <see cref="BuyNumberRequest.ActionCode"/> set to the completed action's code.
/// </summary>
/// <example>
/// <code>
/// // Browse coverage
/// var countries = await sendly.Numbers.ListCountriesAsync();
///
/// // Find an available mobile number in the UK
/// var available = await sendly.Numbers.ListAvailableAsync(new ListAvailableNumbersOptions
/// {
///     Country = "GB",
///     Type = "mobile",
/// });
///
/// // Buy the first one
/// var first = available.Numbers[0];
/// var result = await sendly.Numbers.BuyAsync(new BuyNumberRequest
/// {
///     PhoneNumber = first.PhoneNumber,
///     CountryCode = first.Country,
///     PhoneNumberType = first.NumberType,
///     MonthlyCost = first.MonthlyCost,
/// });
///
/// if (result.Action != null)
/// {
///     // Hand result.Action.Url + result.Action.Code to the user, wait for
///     // completion, then re-call BuyAsync with ActionCode set.
/// }
/// </code>
/// </example>
public class NumbersResource
{
    private readonly SendlyClient _client;

    public NumbersResource(SendlyClient client)
    {
        _client = client;
    }

    /// <summary>
    /// List the countries Sendly can provision numbers in, along with the
    /// number types available in each.
    /// </summary>
    public async Task<NumberCountriesResponse> ListCountriesAsync(
        CancellationToken cancellationToken = default)
    {
        using var doc = await _client.GetAsync("/numbers/countries", null, cancellationToken);
        return JsonSerializer.Deserialize<NumberCountriesResponse>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    /// <summary>
    /// Search for available numbers in a country. Prices are already
    /// customer-priced for your account.
    /// </summary>
    /// <param name="options">Search filters. <see cref="ListAvailableNumbersOptions.Country"/> and <see cref="ListAvailableNumbersOptions.Type"/> are required.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task<AvailableNumbersResponse> ListAvailableAsync(
        ListAvailableNumbersOptions options,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(options?.Country))
            throw new ValidationException("Country is required");
        if (string.IsNullOrEmpty(options?.Type))
            throw new ValidationException("Number type is required");

        var queryParams = new Dictionary<string, string>
        {
            ["country"] = options.Country,
            ["type"] = options.Type,
        };
        if (!string.IsNullOrEmpty(options.Contains))
            queryParams["contains"] = options.Contains;

        using var doc = await _client.GetAsync("/numbers/available", queryParams, cancellationToken);
        return JsonSerializer.Deserialize<AvailableNumbersResponse>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    /// <summary>
    /// List the numbers you already own.
    /// </summary>
    public async Task<OwnedNumbersResponse> ListAsync(
        CancellationToken cancellationToken = default)
    {
        using var doc = await _client.GetAsync("/numbers", null, cancellationToken);
        return JsonSerializer.Deserialize<OwnedNumbersResponse>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    /// <summary>
    /// Buy a phone number. May complete instantly or return a hosted action
    /// (<c>documents_required</c> / <c>payment_required</c>) that the user must
    /// finish before re-calling with <see cref="BuyNumberRequest.ActionCode"/>.
    /// </summary>
    public async Task<BuyNumberResponse> BuyAsync(
        BuyNumberRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(request?.PhoneNumber))
            throw new ValidationException("Phone number is required");

        using var doc = await _client.PostAsync("/numbers/buy", request, cancellationToken);
        return JsonSerializer.Deserialize<BuyNumberResponse>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    /// <summary>
    /// Get a single number you own by id. Unlike <see cref="ListAsync"/>, the
    /// returned record includes <see cref="OwnedNumber.IsDefault"/>.
    /// </summary>
    /// <param name="id">The number's id</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The full owned-number record</returns>
    public async Task<OwnedNumber> GetAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(id))
            throw new ValidationException("A number 'id' is required");

        using var doc = await _client.GetAsync($"/numbers/{Uri.EscapeDataString(id)}", null, cancellationToken);
        return JsonSerializer.Deserialize<OwnedNumber>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    /// <summary>
    /// Update a number you own. Supply at least one supported mutation:
    /// <list type="bullet">
    /// <item><description><see cref="UpdateNumberRequest.IsDefault"/> = true — make this the
    /// workspace's default sending number (the number must be <c>active</c>).</description></item>
    /// <item><description><see cref="UpdateNumberRequest.PendingCancellation"/> = false — cancel a
    /// previously scheduled release and keep the number.</description></item>
    /// </list>
    /// </summary>
    /// <param name="id">The number's id</param>
    /// <param name="request">The mutation(s) to apply</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated owned-number record (including <see cref="OwnedNumber.IsDefault"/>)</returns>
    public async Task<OwnedNumber> UpdateAsync(
        string id,
        UpdateNumberRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(id))
            throw new ValidationException("A number 'id' is required");
        if (request == null || (request.IsDefault == null && request.PendingCancellation == null))
            throw new ValidationException(
                "Provide at least one of { IsDefault = true } or { PendingCancellation = false }");

        using var doc = await _client.PatchAsync($"/numbers/{Uri.EscapeDataString(id)}", request, cancellationToken);
        return JsonSerializer.Deserialize<OwnedNumber>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    /// <summary>
    /// Make a number the workspace's default sending number. The number must be
    /// <c>active</c>. Convenience wrapper over <see cref="UpdateAsync"/>.
    /// </summary>
    public async Task<OwnedNumber> SetDefaultAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        return await UpdateAsync(id, new UpdateNumberRequest { IsDefault = true }, cancellationToken);
    }

    /// <summary>
    /// Cancel a previously scheduled release and keep the number. Convenience
    /// wrapper over <see cref="UpdateAsync"/>.
    /// </summary>
    public async Task<OwnedNumber> KeepAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        return await UpdateAsync(id, new UpdateNumberRequest { PendingCancellation = false }, cancellationToken);
    }

    /// <summary>
    /// Release a number you own. A live paid purchase is cancelled at the end of
    /// the paid period (the response then carries
    /// <see cref="ReleaseNumberResponse.Scheduled"/> and a
    /// <see cref="ReleaseNumberResponse.ScheduledReleaseAt"/>); everything else
    /// is released immediately.
    /// </summary>
    /// <param name="id">The number's id</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>An immediate or scheduled release result</returns>
    public async Task<ReleaseNumberResponse> ReleaseAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(id))
            throw new ValidationException("A number 'id' is required");

        using var doc = await _client.DeleteAsync($"/numbers/{Uri.EscapeDataString(id)}", cancellationToken);
        return JsonSerializer.Deserialize<ReleaseNumberResponse>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }
}

/// <summary>
/// A country Sendly can provision numbers in.
/// </summary>
public class NumberCountry
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("numberTypes")]
    public List<string> NumberTypes { get; set; } = new();
}

public class NumberCountriesResponse
{
    [JsonPropertyName("countries")]
    public List<NumberCountry> Countries { get; set; } = new();
}

/// <summary>
/// Search filters for <see cref="NumbersResource.ListAvailableAsync"/>.
/// </summary>
public class ListAvailableNumbersOptions
{
    /// <summary>ISO country code, e.g. "GB". Required.</summary>
    public string Country { get; set; } = string.Empty;

    /// <summary>Number type, e.g. "mobile". Required.</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Optional digit/substring the number should contain.</summary>
    public string? Contains { get; set; }
}

/// <summary>
/// An available number returned by <see cref="NumbersResource.ListAvailableAsync"/>.
/// <see cref="MonthlyCost"/> is already customer-priced.
/// </summary>
public class AvailableNumber
{
    [JsonPropertyName("phoneNumber")]
    public string PhoneNumber { get; set; } = string.Empty;

    [JsonPropertyName("country")]
    public string Country { get; set; } = string.Empty;

    [JsonPropertyName("numberType")]
    public string NumberType { get; set; } = string.Empty;

    /// <summary>Customer-priced monthly cost, as a string (e.g. "2.50").</summary>
    [JsonPropertyName("monthlyCost")]
    public string MonthlyCost { get; set; } = string.Empty;

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = string.Empty;
}

public class AvailableNumbersResponse
{
    [JsonPropertyName("numbers")]
    public List<AvailableNumber> Numbers { get; set; } = new();
}

/// <summary>
/// A number you own, returned by <see cref="NumbersResource.ListAsync"/>.
/// </summary>
public class OwnedNumber
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("phoneNumber")]
    public string PhoneNumber { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;

    [JsonPropertyName("countryCode")]
    public string CountryCode { get; set; } = string.Empty;

    [JsonPropertyName("phoneNumberType")]
    public string PhoneNumberType { get; set; } = string.Empty;

    /// <summary>
    /// Monthly cost in cents, or 0 when the number has no recorded price
    /// (such as the toll-free number provisioned with a verification).
    /// <see cref="MonthlyCostCentsOrNull"/> tells the two apart.
    /// </summary>
    [JsonIgnore]
    public int MonthlyCostCents
    {
        get => MonthlyCostCentsOrNull ?? 0;
        set => MonthlyCostCentsOrNull = value;
    }

    /// <summary>
    /// Monthly cost in cents as the API reported it, or null when the number
    /// has no recorded price.
    /// </summary>
    [JsonPropertyName("monthlyCostCents")]
    public int? MonthlyCostCentsOrNull { get; set; }

    /// <summary>
    /// True if this is the workspace's default sending number. Included by
    /// <see cref="NumbersResource.GetAsync"/> and <see cref="NumbersResource.UpdateAsync"/>;
    /// omitted (null) by the list endpoint.
    /// </summary>
    [JsonPropertyName("isDefault")]
    public bool? IsDefault { get; set; }

    /// <summary>When regulatory documents were submitted (ISO-8601), or null if the number still needs them.</summary>
    [JsonPropertyName("requirementsSubmittedAt")]
    public string? RequirementsSubmittedAt { get; set; }

    /// <summary>True if the number is scheduled for release at period end.</summary>
    [JsonPropertyName("pendingCancellation")]
    public bool PendingCancellation { get; set; }

    /// <summary>When the number is scheduled to be released (ISO-8601), or null.</summary>
    [JsonPropertyName("scheduledReleaseAt")]
    public string? ScheduledReleaseAt { get; set; }

    /// <summary>
    /// True if the number can take and place phone calls, so it can be the
    /// <c>From</c> of <see cref="CallsResource.CreateAsync"/>. Included by the
    /// list endpoint; null where the API omits it.
    /// </summary>
    [JsonPropertyName("voiceEnabled")]
    public bool? VoiceEnabled { get; set; }

    /// <summary>
    /// How the number answers: <c>none</c>, <c>ring_dashboard</c> (the team
    /// answers in the dashboard) or <c>agent</c> (an AI agent answers). Null
    /// where the API omits it.
    /// </summary>
    [JsonPropertyName("voiceMode")]
    public string? VoiceMode { get; set; }
}

public class OwnedNumbersResponse
{
    [JsonPropertyName("numbers")]
    public List<OwnedNumber> Numbers { get; set; } = new();
}

/// <summary>
/// Body for <see cref="NumbersResource.BuyAsync"/>. Re-call with the SAME body
/// plus <see cref="ActionCode"/> after a hosted action (documents/payment)
/// completes. Null values are omitted.
/// </summary>
public class BuyNumberRequest
{
    [JsonPropertyName("phoneNumber")]
    public string PhoneNumber { get; set; } = string.Empty;

    [JsonPropertyName("countryCode")]
    public string CountryCode { get; set; } = string.Empty;

    [JsonPropertyName("phoneNumberType")]
    public string PhoneNumberType { get; set; } = string.Empty;

    /// <summary>Customer-priced monthly cost, as a string (echo the value from the available-number result).</summary>
    [JsonPropertyName("monthlyCost")]
    public string MonthlyCost { get; set; } = string.Empty;

    /// <summary>
    /// Set on a re-call after a hosted action completes. Use the code from a
    /// COMPLETED action for this number.
    /// </summary>
    [JsonPropertyName("actionCode")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ActionCode { get; set; }
}

/// <summary>
/// A hosted-action hand-off returned when a purchase needs the user to finish
/// a step (upload documents or provide payment) on a Sendly-hosted page.
/// </summary>
public class NumberBuyAction
{
    /// <summary>Hosted Sendly page URL to send the user to.</summary>
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// The action identifier (32-hex string). Poll
    /// <c>GET /api/cli/action/{ActionCode}</c> with this, and pass it back as the
    /// <see cref="BuyNumberRequest.ActionCode"/> on a re-buy. Distinct from
    /// <see cref="Code"/>.
    /// </summary>
    [JsonPropertyName("actionCode")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ActionCode { get; set; }

    /// <summary>
    /// Short user code (8-char, alphabet A-HJ-NP-Z2-9) shown to the user to type
    /// on the hosted page to prove terminal access. Display only — do not use
    /// where <see cref="ActionCode"/> is required.
    /// </summary>
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    /// <summary>When the action expires, as epoch milliseconds.</summary>
    [JsonPropertyName("expiresAt")]
    public long? ExpiresAt { get; set; }
}

/// <summary>
/// Response from <see cref="NumbersResource.BuyAsync"/>.
/// <see cref="Status"/> is one of <c>provisioning</c>, <c>documents_required</c>,
/// or <c>payment_required</c>. When a follow-up step is needed,
/// <see cref="Action"/> is populated; otherwise it is null.
/// </summary>
public class BuyNumberResponse
{
    /// <summary>One of: provisioning, documents_required, payment_required.</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>The provisioned number, present when the purchase completes.</summary>
    [JsonPropertyName("number")]
    public OwnedNumber? Number { get; set; }

    /// <summary>Outstanding requirements when status is documents_required.</summary>
    [JsonPropertyName("requirements")]
    public JsonElement? Requirements { get; set; }

    /// <summary>Hosted-action hand-off when status is documents_required or payment_required.</summary>
    [JsonPropertyName("action")]
    public NumberBuyAction? Action { get; set; }
}

/// <summary>
/// Body for <see cref="NumbersResource.UpdateAsync"/>. Supply at least one field.
/// Only these two mutations are supported; null values are omitted.
/// </summary>
public class UpdateNumberRequest
{
    /// <summary>
    /// Set to <c>true</c> to make this the workspace's default sending number.
    /// The number must be <c>active</c>, or the call fails with <c>invalid_state</c>.
    /// </summary>
    [JsonPropertyName("isDefault")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsDefault { get; set; }

    /// <summary>
    /// Set to <c>false</c> to cancel a previously scheduled release and keep the
    /// number.
    /// </summary>
    [JsonPropertyName("pendingCancellation")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? PendingCancellation { get; set; }
}

/// <summary>
/// Response from <see cref="NumbersResource.ReleaseAsync"/>. An immediate release
/// is <c>{ success: true }</c>; a live paid purchase cancelled at period end is
/// <c>{ success: true, scheduled: true, scheduledReleaseAt }</c>.
/// </summary>
public class ReleaseNumberResponse
{
    /// <summary>Always true on success.</summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>True when the release was scheduled for the end of the paid period.</summary>
    [JsonPropertyName("scheduled")]
    public bool? Scheduled { get; set; }

    /// <summary>When the scheduled release takes effect (ISO-8601), when <see cref="Scheduled"/>.</summary>
    [JsonPropertyName("scheduledReleaseAt")]
    public string? ScheduledReleaseAt { get; set; }
}
