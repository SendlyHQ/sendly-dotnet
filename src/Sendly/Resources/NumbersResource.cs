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

    [JsonPropertyName("monthlyCostCents")]
    public int MonthlyCostCents { get; set; }

    /// <summary>When regulatory documents were submitted (ISO-8601), or null if the number still needs them.</summary>
    [JsonPropertyName("requirementsSubmittedAt")]
    public string? RequirementsSubmittedAt { get; set; }

    /// <summary>True if the number is scheduled for release at period end.</summary>
    [JsonPropertyName("pendingCancellation")]
    public bool PendingCancellation { get; set; }

    /// <summary>When the number is scheduled to be released (ISO-8601), or null.</summary>
    [JsonPropertyName("scheduledReleaseAt")]
    public string? ScheduledReleaseAt { get; set; }
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
