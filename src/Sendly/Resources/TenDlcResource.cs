using System.Text.Json;
using System.Text.Json.Serialization;
using Sendly.Exceptions;

namespace Sendly.Resources;

/// <summary>
/// 10DLC Resource — local-number texting registration.
///
/// Register your business for carrier review so you can text from local
/// (10-digit) US numbers. The flow has three steps:
///
/// 1. Brand — register your business identity. Starts <c>pending</c>; poll
/// <see cref="GetBrandAsync"/> until it becomes <c>verified</c> (or
/// <c>failed</c>, with <see cref="TenDlcBrand.FailureReasons"/> explaining why).
///
/// 2. Campaign — describe your messaging use case under a verified brand and
/// submit it for carrier review. Starts <c>pending</c>; poll
/// <see cref="GetCampaignAsync"/> until it becomes <c>active</c>.
/// <see cref="QualifyAsync"/> pre-checks a use case before you create the campaign.
///
/// 3. Assign — attach a number you own to the active campaign with
/// <see cref="AssignNumberAsync"/>. Once the assignment is <c>Active</c>, the
/// number can send.
///
/// Brand, campaign, and number-assignment writes require a live API key
/// (<c>sk_live_v1_xxx</c>).
/// </summary>
/// <example>
/// <code>
/// // 1. Register a brand and poll until it's verified
/// var brand = (await sendly.TenDlc.CreateBrandAsync(new CreateTenDlcBrandRequest
/// {
///     LegalName = "Acme Holdings LLC",
///     Ein = "12-3456789",
///     Website = "https://acme.example",
///     Email = "ops@acme.example",
/// })).Data;
/// // ...poll sendly.TenDlc.GetBrandAsync(brand.Id) until Status == "verified"
///
/// // 2. Pre-check the use case, then create a campaign
/// var check = (await sendly.TenDlc.QualifyAsync(brand.Id, "MIXED")).Data;
/// if (check.Qualified)
/// {
///     var campaign = (await sendly.TenDlc.CreateCampaignAsync(new CreateTenDlcCampaignRequest
///     {
///         BrandId = brand.Id,
///         UseCase = "MIXED",
///         Description = "Order updates and support replies for Acme customers",
///         MessageFlow = "Customers opt in at checkout on acme.example",
///         SampleMessages = new() { "Your order 123 has shipped" },
///     })).Data;
///     // ...poll sendly.TenDlc.GetCampaignAsync(campaign.Id) until Status == "active"
///
///     // 3. Assign a number you own
///     await sendly.TenDlc.AssignNumberAsync(campaign.Id, "+15551234567");
/// }
/// </code>
/// </example>
public class TenDlcResource
{
    private readonly SendlyClient _client;

    public TenDlcResource(SendlyClient client)
    {
        _client = client;
    }

    /// <summary>
    /// List the brands registered for carrier review.
    /// </summary>
    public async Task<TenDlcBrandListResponse> ListBrandsAsync(
        CancellationToken cancellationToken = default)
    {
        using var doc = await _client.GetAsync("/tendlc/brands", null, cancellationToken);
        return JsonSerializer.Deserialize<TenDlcBrandListResponse>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    /// <summary>
    /// Register a brand for carrier review — step 1 of enabling local-number
    /// texting. Requires a live API key. The brand starts <c>pending</c>; poll
    /// <see cref="GetBrandAsync"/> until it becomes <c>verified</c> before
    /// creating a campaign.
    /// </summary>
    /// <param name="request">Business identity details. <see cref="CreateTenDlcBrandRequest.LegalName"/> is required.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task<TenDlcBrandResponse> CreateBrandAsync(
        CreateTenDlcBrandRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(request?.LegalName))
            throw new ValidationException("Legal name is required");

        using var doc = await _client.PostAsync("/tendlc/brands", request, cancellationToken);
        return JsonSerializer.Deserialize<TenDlcBrandResponse>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    /// <summary>
    /// Fetch one brand. Also refreshes its carrier-review status, so polling
    /// this method shows progress (<c>pending</c> → <c>verified</c>/<c>failed</c>).
    /// </summary>
    /// <param name="id">Brand identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task<TenDlcBrandResponse> GetBrandAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(id))
            throw new ValidationException("Brand ID is required");

        using var doc = await _client.GetAsync($"/tendlc/brands/{Uri.EscapeDataString(id)}", null, cancellationToken);
        return JsonSerializer.Deserialize<TenDlcBrandResponse>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    /// <summary>
    /// Pre-check whether a use case qualifies for a brand on the carrier
    /// network before creating a campaign.
    /// </summary>
    /// <param name="brandId">Brand identifier</param>
    /// <param name="useCase">Use-case code (e.g. "MIXED", "MARKETING", "ACCOUNT_NOTIFICATION", "2FA")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task<TenDlcQualifyResponse> QualifyAsync(
        string brandId,
        string useCase,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(brandId))
            throw new ValidationException("Brand ID is required");
        if (string.IsNullOrEmpty(useCase))
            throw new ValidationException("Use case is required");

        using var doc = await _client.GetAsync($"/tendlc/brands/{Uri.EscapeDataString(brandId)}/qualify/{Uri.EscapeDataString(useCase)}", null, cancellationToken);
        return JsonSerializer.Deserialize<TenDlcQualifyResponse>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    /// <summary>
    /// List your messaging campaigns.
    /// </summary>
    public async Task<TenDlcCampaignListResponse> ListCampaignsAsync(
        CancellationToken cancellationToken = default)
    {
        using var doc = await _client.GetAsync("/tendlc/campaigns", null, cancellationToken);
        return JsonSerializer.Deserialize<TenDlcCampaignListResponse>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    /// <summary>
    /// Create a messaging campaign under a verified brand and submit it for
    /// carrier review. Requires a live API key. The campaign starts
    /// <c>pending</c>; poll <see cref="GetCampaignAsync"/> until it becomes
    /// <c>active</c> before assigning numbers.
    /// </summary>
    /// <param name="request">Campaign details. <see cref="CreateTenDlcCampaignRequest.BrandId"/>,
    /// <see cref="CreateTenDlcCampaignRequest.UseCase"/>, <see cref="CreateTenDlcCampaignRequest.Description"/>,
    /// <see cref="CreateTenDlcCampaignRequest.MessageFlow"/>, and <see cref="CreateTenDlcCampaignRequest.SampleMessages"/> are required.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task<TenDlcCampaignResponse> CreateCampaignAsync(
        CreateTenDlcCampaignRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(request?.BrandId))
            throw new ValidationException("Brand ID is required");
        if (string.IsNullOrEmpty(request.UseCase))
            throw new ValidationException("Use case is required");
        if (string.IsNullOrEmpty(request.Description))
            throw new ValidationException("Description is required");
        if (string.IsNullOrEmpty(request.MessageFlow))
            throw new ValidationException("Message flow is required");
        if (request.SampleMessages == null || request.SampleMessages.Count == 0)
            throw new ValidationException("Sample messages are required");

        using var doc = await _client.PostAsync("/tendlc/campaigns", request, cancellationToken);
        return JsonSerializer.Deserialize<TenDlcCampaignResponse>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    /// <summary>
    /// Fetch one campaign. Also refreshes its carrier-review status, so
    /// polling this method shows progress (<c>pending</c> → <c>active</c>)
    /// including throughput once carriers approve.
    /// </summary>
    /// <param name="id">Campaign identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task<TenDlcCampaignResponse> GetCampaignAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(id))
            throw new ValidationException("Campaign ID is required");

        using var doc = await _client.GetAsync($"/tendlc/campaigns/{Uri.EscapeDataString(id)}", null, cancellationToken);
        return JsonSerializer.Deserialize<TenDlcCampaignResponse>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    /// <summary>
    /// Assign a phone number you own to an active (carrier-approved) campaign,
    /// making the number sendable. Requires a live API key. Idempotent —
    /// re-assigning the same number to the same campaign returns the existing
    /// assignment.
    /// </summary>
    /// <param name="campaignId">Campaign identifier</param>
    /// <param name="phoneNumber">E.164 number the workspace already owns</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task<TenDlcAssignmentResponse> AssignNumberAsync(
        string campaignId,
        string phoneNumber,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(campaignId))
            throw new ValidationException("Campaign ID is required");
        if (string.IsNullOrEmpty(phoneNumber))
            throw new ValidationException("Phone number is required");

        var request = new AssignTenDlcNumberRequest { PhoneNumber = phoneNumber };
        using var doc = await _client.PostAsync($"/tendlc/campaigns/{Uri.EscapeDataString(campaignId)}/assign", request, cancellationToken);
        return JsonSerializer.Deserialize<TenDlcAssignmentResponse>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    /// <summary>
    /// List your number-to-campaign assignments.
    /// </summary>
    public async Task<TenDlcAssignmentListResponse> ListAssignmentsAsync(
        CancellationToken cancellationToken = default)
    {
        using var doc = await _client.GetAsync("/tendlc/assignments", null, cancellationToken);
        return JsonSerializer.Deserialize<TenDlcAssignmentListResponse>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }
}

/// <summary>
/// A business identity registered for carrier review.
/// <see cref="Status"/> is one of <c>pending</c>, <c>verified</c>, or
/// <c>failed</c> (see <see cref="FailureReasons"/>).
/// </summary>
public class TenDlcBrand
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Legal business name.</summary>
    [JsonPropertyName("legalName")]
    public string LegalName { get; set; } = string.Empty;

    /// <summary>"Doing business as" name, if different from the legal name.</summary>
    [JsonPropertyName("dba")]
    public string? Dba { get; set; }

    /// <summary>Business entity type (e.g. "PRIVATE_PROFIT", "SOLE_PROPRIETOR").</summary>
    [JsonPropertyName("entityType")]
    public string EntityType { get; set; } = string.Empty;

    /// <summary>Business registration number (e.g. EIN).</summary>
    [JsonPropertyName("ein")]
    public string? Ein { get; set; }

    /// <summary>Industry vertical.</summary>
    [JsonPropertyName("vertical")]
    public string? Vertical { get; set; }

    /// <summary>Business website URL.</summary>
    [JsonPropertyName("website")]
    public string? Website { get; set; }

    /// <summary>Carrier-review status: pending, verified, or failed.</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>Identity-verification detail from the carrier review, when available.</summary>
    [JsonPropertyName("identityStatus")]
    public string? IdentityStatus { get; set; }

    /// <summary>Why the review failed, when <see cref="Status"/> is "failed".</summary>
    [JsonPropertyName("failureReasons")]
    public List<string>? FailureReasons { get; set; }

    /// <summary>When the brand was created (ISO 8601).</summary>
    [JsonPropertyName("createdAt")]
    public string CreatedAt { get; set; } = string.Empty;

    /// <summary>When the brand was last updated (ISO 8601).</summary>
    [JsonPropertyName("updatedAt")]
    public string UpdatedAt { get; set; } = string.Empty;
}

public class TenDlcBrandListResponse
{
    [JsonPropertyName("data")]
    public List<TenDlcBrand> Data { get; set; } = new();
}

public class TenDlcBrandResponse
{
    [JsonPropertyName("data")]
    public TenDlcBrand Data { get; set; } = new();
}

/// <summary>
/// Body for <see cref="TenDlcResource.CreateBrandAsync"/>. Null values are omitted.
/// </summary>
public class CreateTenDlcBrandRequest
{
    /// <summary>Legal business name. Required.</summary>
    [JsonPropertyName("legalName")]
    public string LegalName { get; set; } = string.Empty;

    /// <summary>"Doing business as" name, if different from the legal name.</summary>
    [JsonPropertyName("dba")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Dba { get; set; }

    /// <summary>Business registration number (e.g. EIN).</summary>
    [JsonPropertyName("ein")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Ein { get; set; }

    /// <summary>Business entity type (e.g. "PRIVATE_PROFIT", "SOLE_PROPRIETOR"). Defaults to "PRIVATE_PROFIT".</summary>
    [JsonPropertyName("entityType")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? EntityType { get; set; }

    /// <summary>Industry vertical.</summary>
    [JsonPropertyName("vertical")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Vertical { get; set; }

    /// <summary>Business website URL.</summary>
    [JsonPropertyName("website")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Website { get; set; }

    /// <summary>Business contact email.</summary>
    [JsonPropertyName("email")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Email { get; set; }

    /// <summary>Business phone number.</summary>
    [JsonPropertyName("phone")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Phone { get; set; }

    /// <summary>Business mobile phone number.</summary>
    [JsonPropertyName("mobilePhone")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MobilePhone { get; set; }

    /// <summary>Street address.</summary>
    [JsonPropertyName("street")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Street { get; set; }

    /// <summary>City.</summary>
    [JsonPropertyName("city")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? City { get; set; }

    /// <summary>State or region.</summary>
    [JsonPropertyName("state")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? State { get; set; }

    /// <summary>Postal code.</summary>
    [JsonPropertyName("postalCode")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PostalCode { get; set; }

    /// <summary>ISO 3166-1 alpha-2 country code. Defaults to "US".</summary>
    [JsonPropertyName("country")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Country { get; set; }

    /// <summary>Existing Sendly verification to prefill business details from.</summary>
    [JsonPropertyName("verificationId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? VerificationId { get; set; }
}

/// <summary>
/// Throughput detail for a campaign or use-case qualification.
/// </summary>
public class TenDlcThroughput
{
    /// <summary>Throughput tier granted by the carrier network: "High volume", "Standard", or "Low volume".</summary>
    [JsonPropertyName("tier")]
    public string Tier { get; set; } = string.Empty;

    /// <summary>How many carriers have accepted the campaign so far.</summary>
    [JsonPropertyName("carriersReady")]
    public int CarriersReady { get; set; }
}

/// <summary>
/// Result of a use-case qualification pre-check.
/// </summary>
public class TenDlcQualifyResult
{
    /// <summary>The use-case code that was checked (e.g. "MIXED", "MARKETING").</summary>
    [JsonPropertyName("useCase")]
    public string UseCase { get; set; } = string.Empty;

    /// <summary>Whether the use case qualifies for this brand.</summary>
    [JsonPropertyName("qualified")]
    public bool Qualified { get; set; }

    /// <summary>Why the use case does not qualify, when <see cref="Qualified"/> is false.</summary>
    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    /// <summary>Expected throughput, when the carrier network reports it.</summary>
    [JsonPropertyName("throughput")]
    public TenDlcThroughput? Throughput { get; set; }
}

public class TenDlcQualifyResponse
{
    [JsonPropertyName("data")]
    public TenDlcQualifyResult Data { get; set; } = new();
}

/// <summary>
/// A messaging campaign registered for carrier review.
/// <see cref="Status"/> is one of <c>pending</c>, <c>active</c>, <c>failed</c>,
/// <c>suspended</c>, or <c>expired</c>.
/// </summary>
public class TenDlcCampaign
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>The brand this campaign belongs to.</summary>
    [JsonPropertyName("brandId")]
    public string BrandId { get; set; } = string.Empty;

    /// <summary>Primary use-case code (e.g. "MIXED", "MARKETING").</summary>
    [JsonPropertyName("useCase")]
    public string UseCase { get; set; } = string.Empty;

    /// <summary>Sub-use-case codes.</summary>
    [JsonPropertyName("subUseCases")]
    public List<string> SubUseCases { get; set; } = new();

    /// <summary>What the campaign sends and why.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>Carrier-review status: pending, active, failed, suspended, or expired.</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>Example messages the campaign sends.</summary>
    [JsonPropertyName("sampleMessages")]
    public List<string> SampleMessages { get; set; } = new();

    /// <summary>Granted throughput, once carriers approve.</summary>
    [JsonPropertyName("throughput")]
    public TenDlcThroughput? Throughput { get; set; }

    /// <summary>Why the review failed, when <see cref="Status"/> is "failed".</summary>
    [JsonPropertyName("failureReasons")]
    public List<string>? FailureReasons { get; set; }

    /// <summary>When the campaign was created (ISO 8601).</summary>
    [JsonPropertyName("createdAt")]
    public string CreatedAt { get; set; } = string.Empty;

    /// <summary>When the campaign was last updated (ISO 8601).</summary>
    [JsonPropertyName("updatedAt")]
    public string UpdatedAt { get; set; } = string.Empty;
}

public class TenDlcCampaignListResponse
{
    [JsonPropertyName("data")]
    public List<TenDlcCampaign> Data { get; set; } = new();
}

public class TenDlcCampaignResponse
{
    [JsonPropertyName("data")]
    public TenDlcCampaign Data { get; set; } = new();
}

/// <summary>
/// Body for <see cref="TenDlcResource.CreateCampaignAsync"/>. Null values are omitted.
/// </summary>
public class CreateTenDlcCampaignRequest
{
    /// <summary>The verified brand to create the campaign under. Required.</summary>
    [JsonPropertyName("brandId")]
    public string BrandId { get; set; } = string.Empty;

    /// <summary>Primary use-case code (e.g. "MIXED", "MARKETING"). Required.</summary>
    [JsonPropertyName("useCase")]
    public string UseCase { get; set; } = string.Empty;

    /// <summary>What the campaign sends and why. Required.</summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>How recipients opt in to receive messages. Required.</summary>
    [JsonPropertyName("messageFlow")]
    public string MessageFlow { get; set; } = string.Empty;

    /// <summary>Example messages the campaign sends. Required; the first 5 are used.</summary>
    [JsonPropertyName("sampleMessages")]
    public List<string> SampleMessages { get; set; } = new();

    /// <summary>Sub-use-case codes.</summary>
    [JsonPropertyName("subUseCases")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? SubUseCases { get; set; }

    /// <summary>Comma-separated keywords that opt a recipient in.</summary>
    [JsonPropertyName("optInKeywords")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? OptInKeywords { get; set; }

    /// <summary>Comma-separated keywords that opt a recipient out.</summary>
    [JsonPropertyName("optOutKeywords")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? OptOutKeywords { get; set; }

    /// <summary>Comma-separated keywords that request help.</summary>
    [JsonPropertyName("helpKeywords")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? HelpKeywords { get; set; }

    /// <summary>Auto-reply sent on opt-in.</summary>
    [JsonPropertyName("optInMessage")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? OptInMessage { get; set; }

    /// <summary>Auto-reply sent on opt-out.</summary>
    [JsonPropertyName("optOutMessage")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? OptOutMessage { get; set; }

    /// <summary>Auto-reply sent on a help request.</summary>
    [JsonPropertyName("helpMessage")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? HelpMessage { get; set; }

    /// <summary>Whether messages may contain links. Defaults to true.</summary>
    [JsonPropertyName("embeddedLink")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? EmbeddedLink { get; set; }

    /// <summary>Whether messages may contain phone numbers. Defaults to false.</summary>
    [JsonPropertyName("embeddedPhone")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? EmbeddedPhone { get; set; }
}

/// <summary>
/// Body for <see cref="TenDlcResource.AssignNumberAsync"/>.
/// </summary>
public class AssignTenDlcNumberRequest
{
    [JsonPropertyName("phoneNumber")]
    public string PhoneNumber { get; set; } = string.Empty;
}

/// <summary>
/// A phone number assigned to a campaign.
/// <see cref="Status"/> is one of <c>Active</c>, <c>Under review</c>, or
/// <c>Action needed</c>; the number can send once <c>Active</c>.
/// </summary>
public class TenDlcAssignment
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>The campaign the number is assigned to.</summary>
    [JsonPropertyName("campaignId")]
    public string CampaignId { get; set; } = string.Empty;

    /// <summary>The assigned phone number in E.164 format.</summary>
    [JsonPropertyName("phoneNumber")]
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>Assignment status; the number can send once "Active".</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>When the assignment completed (ISO 8601), or null while in progress.</summary>
    [JsonPropertyName("assignedAt")]
    public string? AssignedAt { get; set; }
}

public class TenDlcAssignmentResponse
{
    [JsonPropertyName("data")]
    public TenDlcAssignment Data { get; set; } = new();
}

public class TenDlcAssignmentListResponse
{
    [JsonPropertyName("data")]
    public List<TenDlcAssignment> Data { get; set; } = new();
}
