using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Sendly.Exceptions;

namespace Sendly.Resources;

/// <summary>
/// Business Upgrade Resource — Entity-Upgrade ("fork-with-new-number").
///
/// Manages the toll-free business entity upgrade flow: when a customer
/// forms a new legal entity (e.g. an LLC), this resource lets them
/// reserve a new toll-free number under the new entity, submit it for
/// carrier review, and atomically swap to it on approval — without
/// disrupting outbound SMS during the 1-2 week review window.
/// </summary>
/// <example>
/// <code>
/// // Preview validation before submitting
/// var preview = await sendly.BusinessUpgrade.PreflightAsync(new PreflightCandidate
/// {
///     BusinessName = "Acme Holdings LLC",
///     Brn = "12-3456789",
///     BrnType = "EIN",
///     BrnCountry = "US",
///     EntityType = "PRIVATE_PROFIT",
/// });
///
/// // Submit the upgrade with the IRS letter
/// var result = await sendly.BusinessUpgrade.StartAsync(
///     "ws_abc",
///     new StartUpgradeParams
///     {
///         BusinessName = "Acme Holdings LLC",
///         Brn = "12-3456789",
///         BrnType = "EIN",
///         BrnCountry = "US",
///         EntityType = "PRIVATE_PROFIT",
///     },
///     new EinDocumentInput { Path = "./CP-575.pdf" });
/// </code>
/// </example>
public class BusinessUpgradeResource
{
    private readonly SendlyClient _client;

    public BusinessUpgradeResource(SendlyClient client)
    {
        _client = client;
    }

    /// <summary>
    /// Validate a candidate entity upgrade payload before submission.
    /// Returns issues + proposed auto-fixes. No writes — purely advisory.
    /// </summary>
    public async Task<PreflightReport> PreflightAsync(
        PreflightCandidate candidate,
        CancellationToken cancellationToken = default)
    {
        var doc = await _client.PostAsync("/verification/preflight", candidate, cancellationToken);
        return JsonSerializer.Deserialize<PreflightReport>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    /// <summary>
    /// Get a "best-of" prefill across all the caller's verified workspaces.
    /// Returns most-recent non-empty values per messaging field. Use this
    /// to pre-populate the upgrade form for users whose current workspace
    /// has incomplete data.
    /// </summary>
    public async Task<BestPrefillResponse> BestPrefillAsync(
        CancellationToken cancellationToken = default)
    {
        var doc = await _client.GetAsync("/verification/best-prefill", null, cancellationToken);
        return JsonSerializer.Deserialize<BestPrefillResponse>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    /// <summary>
    /// Start an entity upgrade for the given workspace. Auto-provisions
    /// a new toll-free number + messaging profile and submits to the
    /// carrier for review. Returns the pending verification details.
    ///
    /// The current toll-free number continues sending throughout the
    /// 1-2 week carrier review; on approval, an atomic swap promotes
    /// the new number.
    /// </summary>
    public async Task<StartUpgradeResponse> StartAsync(
        string workspaceId,
        StartUpgradeParams parameters,
        EinDocumentInput? einDoc = null,
        CancellationToken cancellationToken = default)
    {
        using var form = BuildUpgradeForm(parameters, einDoc);
        using var doc = await _client.PostContentAsync(
            $"/workspaces/{Uri.EscapeDataString(workspaceId)}/upgrade",
            form,
            cancellationToken);
        return JsonSerializer.Deserialize<StartUpgradeResponse>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    /// <summary>
    /// Check whether the given workspace has a pending entity upgrade.
    /// Returns a response with <c>Pending = null</c> if no upgrade is in flight.
    /// </summary>
    public async Task<UpgradeStatusResponse> StatusAsync(
        string workspaceId,
        CancellationToken cancellationToken = default)
    {
        var doc = await _client.GetAsync(
            $"/workspaces/{Uri.EscapeDataString(workspaceId)}/upgrade/status",
            null,
            cancellationToken);
        return JsonSerializer.Deserialize<UpgradeStatusResponse>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    /// <summary>
    /// Cancel a pending entity upgrade for the given workspace. Releases
    /// the reserved toll-free number, deletes the new messaging profile,
    /// and removes the stored EIN document. Idempotent.
    /// </summary>
    public async Task<CancelUpgradeResponse> CancelAsync(
        string workspaceId,
        CancellationToken cancellationToken = default)
    {
        var doc = await _client.PostAsync(
            $"/workspaces/{Uri.EscapeDataString(workspaceId)}/upgrade/cancel",
            new { },
            cancellationToken);
        return JsonSerializer.Deserialize<CancelUpgradeResponse>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    /// <summary>
    /// Resubmit a rejected (or waiting-for-customer) entity upgrade with
    /// updated fields and optionally a new EIN document.
    /// </summary>
    public async Task<ResubmitUpgradeResponse> ResubmitAsync(
        string workspaceId,
        StartUpgradeParams parameters,
        EinDocumentInput? einDoc = null,
        CancellationToken cancellationToken = default)
    {
        using var form = BuildUpgradeForm(parameters, einDoc);
        using var doc = await _client.PostContentAsync(
            $"/workspaces/{Uri.EscapeDataString(workspaceId)}/upgrade/resubmit",
            form,
            cancellationToken);
        return JsonSerializer.Deserialize<ResubmitUpgradeResponse>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    /// <summary>
    /// After a successful entity-upgrade approval, choose what happens to
    /// the old toll-free number:
    /// <list type="bullet">
    ///   <item><description><c>moved</c>: keep it active under another workspace owned by the same user (requires <c>targetWorkspaceId</c>)</description></item>
    ///   <item><description><c>released</c>: return it to the carrier pool</description></item>
    /// </list>
    /// </summary>
    public async Task<DispositionResponse> SetDispositionAsync(
        string workspaceId,
        DispositionRequest request,
        CancellationToken cancellationToken = default)
    {
        var body = new
        {
            disposition = request.Disposition,
            targetOrgId = request.TargetWorkspaceId,
        };
        var doc = await _client.PostAsync(
            $"/workspaces/{Uri.EscapeDataString(workspaceId)}/upgrade/disposition",
            body,
            cancellationToken);
        return JsonSerializer.Deserialize<DispositionResponse>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    private MultipartFormDataContent BuildUpgradeForm(
        StartUpgradeParams parameters,
        EinDocumentInput? einDoc)
    {
        var form = new MultipartFormDataContent();

        foreach (var prop in parameters.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var value = prop.GetValue(parameters);
            if (value == null) continue;

            var name = prop.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? ToCamelCase(prop.Name);
            string stringValue = value switch
            {
                bool b => b ? "true" : "false",
                _ => value.ToString() ?? string.Empty,
            };

            form.Add(new StringContent(stringValue), name);
        }

        if (einDoc != null)
        {
            HttpContent fileContent;
            string filename = einDoc.Filename ?? "ein-doc.pdf";
            string contentType = einDoc.ContentType ?? "application/pdf";

            if (einDoc.Bytes != null)
            {
                fileContent = new ByteArrayContent(einDoc.Bytes);
            }
            else if (einDoc.Stream != null)
            {
                fileContent = new StreamContent(einDoc.Stream);
            }
            else if (!string.IsNullOrEmpty(einDoc.Path))
            {
                if (!File.Exists(einDoc.Path))
                    throw new ValidationException($"EIN document file not found: {einDoc.Path}");
                if (einDoc.Filename == null)
                    filename = System.IO.Path.GetFileName(einDoc.Path);
                fileContent = new StreamContent(File.OpenRead(einDoc.Path));
            }
            else
            {
                throw new ValidationException("EinDocumentInput requires Bytes, Stream, or Path.");
            }

            fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            form.Add(fileContent, "einDoc", filename);
        }

        return form;
    }

    private static string ToCamelCase(string s)
    {
        if (string.IsNullOrEmpty(s) || char.IsLower(s[0])) return s;
        return char.ToLowerInvariant(s[0]) + s.Substring(1);
    }
}

/// <summary>
/// Candidate payload for <see cref="BusinessUpgradeResource.PreflightAsync"/>.
/// Mirrors <see cref="StartUpgradeParams"/> but is used in a read-only
/// validation context (no carrier submission).
/// </summary>
public class PreflightCandidate
{
    [JsonPropertyName("businessName")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? BusinessName { get; set; }

    [JsonPropertyName("doingBusinessAs")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DoingBusinessAs { get; set; }

    [JsonPropertyName("brn")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Brn { get; set; }

    /// <summary>One of: EIN, SSN, DUNS, CRA, VAT, LEI, OTHER.</summary>
    [JsonPropertyName("brnType")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? BrnType { get; set; }

    [JsonPropertyName("brnCountry")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? BrnCountry { get; set; }

    /// <summary>One of: SOLE_PROPRIETOR, PRIVATE_PROFIT, PUBLIC_PROFIT, NON_PROFIT, GOVERNMENT.</summary>
    [JsonPropertyName("entityType")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? EntityType { get; set; }

    [JsonPropertyName("website")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Website { get; set; }

    [JsonPropertyName("address1")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Address1 { get; set; }

    [JsonPropertyName("address2")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Address2 { get; set; }

    [JsonPropertyName("city")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? City { get; set; }

    [JsonPropertyName("state")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? State { get; set; }

    [JsonPropertyName("zip")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Zip { get; set; }

    [JsonPropertyName("addressCountry")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AddressCountry { get; set; }

    [JsonPropertyName("contactFirstName")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ContactFirstName { get; set; }

    [JsonPropertyName("contactLastName")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ContactLastName { get; set; }

    [JsonPropertyName("contactEmail")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ContactEmail { get; set; }

    [JsonPropertyName("contactPhone")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ContactPhone { get; set; }

    [JsonPropertyName("monthlyVolume")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MonthlyVolume { get; set; }

    [JsonPropertyName("useCase")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? UseCase { get; set; }

    [JsonPropertyName("useCaseSummary")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? UseCaseSummary { get; set; }

    [JsonPropertyName("sampleMessages")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SampleMessages { get; set; }

    [JsonPropertyName("optInWorkflow")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? OptInWorkflow { get; set; }

    [JsonPropertyName("privacyUrl")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PrivacyUrl { get; set; }

    [JsonPropertyName("termsUrl")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TermsUrl { get; set; }

    [JsonPropertyName("additionalInformation")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AdditionalInformation { get; set; }

    [JsonPropertyName("ageGatedContent")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? AgeGatedContent { get; set; }
}

/// <summary>
/// Params for <see cref="BusinessUpgradeResource.StartAsync"/> /
/// <see cref="BusinessUpgradeResource.ResubmitAsync"/>. Sent as
/// multipart form fields. Null values are omitted.
/// </summary>
public class StartUpgradeParams
{
    [JsonPropertyName("businessName")]
    public string? BusinessName { get; set; }

    [JsonPropertyName("brn")]
    public string? Brn { get; set; }

    /// <summary>One of: EIN, SSN, DUNS, CRA, VAT, LEI, OTHER.</summary>
    [JsonPropertyName("brnType")]
    public string? BrnType { get; set; }

    [JsonPropertyName("brnCountry")]
    public string? BrnCountry { get; set; }

    /// <summary>One of: SOLE_PROPRIETOR, PRIVATE_PROFIT, PUBLIC_PROFIT, NON_PROFIT, GOVERNMENT.</summary>
    [JsonPropertyName("entityType")]
    public string? EntityType { get; set; }

    [JsonPropertyName("doingBusinessAs")]
    public string? DoingBusinessAs { get; set; }

    [JsonPropertyName("website")]
    public string? Website { get; set; }

    [JsonPropertyName("address1")]
    public string? Address1 { get; set; }

    [JsonPropertyName("address2")]
    public string? Address2 { get; set; }

    [JsonPropertyName("city")]
    public string? City { get; set; }

    [JsonPropertyName("state")]
    public string? State { get; set; }

    [JsonPropertyName("zip")]
    public string? Zip { get; set; }

    [JsonPropertyName("addressCountry")]
    public string? AddressCountry { get; set; }

    [JsonPropertyName("contactFirstName")]
    public string? ContactFirstName { get; set; }

    [JsonPropertyName("contactLastName")]
    public string? ContactLastName { get; set; }

    [JsonPropertyName("contactEmail")]
    public string? ContactEmail { get; set; }

    [JsonPropertyName("contactPhone")]
    public string? ContactPhone { get; set; }

    [JsonPropertyName("monthlyVolume")]
    public string? MonthlyVolume { get; set; }

    [JsonPropertyName("useCase")]
    public string? UseCase { get; set; }

    [JsonPropertyName("useCaseSummary")]
    public string? UseCaseSummary { get; set; }

    [JsonPropertyName("sampleMessages")]
    public string? SampleMessages { get; set; }

    [JsonPropertyName("optInWorkflow")]
    public string? OptInWorkflow { get; set; }

    [JsonPropertyName("privacyUrl")]
    public string? PrivacyUrl { get; set; }

    [JsonPropertyName("termsUrl")]
    public string? TermsUrl { get; set; }

    [JsonPropertyName("additionalInformation")]
    public string? AdditionalInformation { get; set; }

    [JsonPropertyName("ageGatedContent")]
    public bool? AgeGatedContent { get; set; }
}

/// <summary>
/// Input for the EIN document (IRS CP-575 letter or equivalent). Provide
/// exactly one of <see cref="Bytes"/>, <see cref="Stream"/>, or <see cref="Path"/>.
/// </summary>
public class EinDocumentInput
{
    /// <summary>Raw PDF bytes.</summary>
    public byte[]? Bytes { get; set; }

    /// <summary>Open readable stream of the document.</summary>
    public Stream? Stream { get; set; }

    /// <summary>Path to a file on disk. Filename is auto-derived if not set.</summary>
    public string? Path { get; set; }

    /// <summary>Filename used in the multipart part. Defaults to "ein-doc.pdf".</summary>
    public string? Filename { get; set; }

    /// <summary>Content-Type. Defaults to "application/pdf".</summary>
    public string? ContentType { get; set; }
}

public class PreflightIssue
{
    /// <summary>One of: blocker, warning, info.</summary>
    [JsonPropertyName("severity")]
    public string Severity { get; set; } = string.Empty;

    [JsonPropertyName("field")]
    public string Field { get; set; } = string.Empty;

    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("suggestion")]
    public string? Suggestion { get; set; }
}

public class PreflightProposedFix
{
    [JsonPropertyName("field")]
    public string Field { get; set; } = string.Empty;

    [JsonPropertyName("current")]
    public JsonElement? Current { get; set; }

    [JsonPropertyName("proposed")]
    public JsonElement? Proposed { get; set; }

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;
}

public class PreflightReport
{
    [JsonPropertyName("verificationId")]
    public string VerificationId { get; set; } = string.Empty;

    [JsonPropertyName("businessName")]
    public string? BusinessName { get; set; }

    /// <summary>One of: CA, US, OTHER, UNKNOWN.</summary>
    [JsonPropertyName("country")]
    public string Country { get; set; } = string.Empty;

    /// <summary>One of: ready, warnings, blocked.</summary>
    [JsonPropertyName("verdict")]
    public string Verdict { get; set; } = string.Empty;

    [JsonPropertyName("issues")]
    public List<PreflightIssue> Issues { get; set; } = new();

    [JsonPropertyName("proposedFixes")]
    public List<PreflightProposedFix> ProposedFixes { get; set; } = new();
}

public class StartUpgradeResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("pendingVerificationId")]
    public string PendingVerificationId { get; set; } = string.Empty;

    [JsonPropertyName("telnyxVerificationId")]
    public string TelnyxVerificationId { get; set; } = string.Empty;

    [JsonPropertyName("tollFreeNumber")]
    public string TollFreeNumber { get; set; } = string.Empty;

    [JsonPropertyName("telnyxMessagingProfileId")]
    public string TelnyxMessagingProfileId { get; set; } = string.Empty;

    [JsonPropertyName("einDocStored")]
    public bool EinDocStored { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

public class PendingUpgrade
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("businessName")]
    public string BusinessName { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("entityType")]
    public string? EntityType { get; set; }

    [JsonPropertyName("brnType")]
    public string? BrnType { get; set; }

    [JsonPropertyName("brnCountry")]
    public string? BrnCountry { get; set; }

    [JsonPropertyName("tollFreeNumber")]
    public string? TollFreeNumber { get; set; }

    [JsonPropertyName("rejectionReason")]
    public string? RejectionReason { get; set; }

    [JsonPropertyName("createdAt")]
    public string CreatedAt { get; set; } = string.Empty;

    [JsonPropertyName("updatedAt")]
    public string UpdatedAt { get; set; } = string.Empty;
}

public class UpgradeStatusResponse
{
    /// <summary>Null when no upgrade is in flight for this workspace.</summary>
    [JsonPropertyName("pending")]
    public PendingUpgrade? Pending { get; set; }
}

public class CancelUpgradeResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("cancelled")]
    public bool Cancelled { get; set; }

    [JsonPropertyName("cancelledVerificationId")]
    public string? CancelledVerificationId { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

public class ResubmitUpgradeResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("pendingVerificationId")]
    public string PendingVerificationId { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Body for <see cref="BusinessUpgradeResource.SetDispositionAsync"/>.
/// </summary>
public class DispositionRequest
{
    /// <summary>One of: moved, released.</summary>
    public string Disposition { get; set; } = string.Empty;

    /// <summary>
    /// Required when <see cref="Disposition"/> is "moved". The id of the
    /// workspace (owned by the same user) to receive the old number.
    /// </summary>
    public string? TargetWorkspaceId { get; set; }
}

public class DispositionResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>One of: moved, released.</summary>
    [JsonPropertyName("disposition")]
    public string Disposition { get; set; } = string.Empty;

    [JsonPropertyName("supersededVerificationId")]
    public string SupersededVerificationId { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

public class BestPrefillData
{
    [JsonPropertyName("monthlyVolume")]
    public string? MonthlyVolume { get; set; }

    [JsonPropertyName("useCase")]
    public string? UseCase { get; set; }

    [JsonPropertyName("useCaseSummary")]
    public string? UseCaseSummary { get; set; }

    [JsonPropertyName("sampleMessages")]
    public string? SampleMessages { get; set; }

    [JsonPropertyName("optInWorkflow")]
    public string? OptInWorkflow { get; set; }

    [JsonPropertyName("optInImageUrls")]
    public string? OptInImageUrls { get; set; }

    [JsonPropertyName("optInSource")]
    public string? OptInSource { get; set; }

    [JsonPropertyName("privacyUrl")]
    public string? PrivacyUrl { get; set; }

    [JsonPropertyName("termsUrl")]
    public string? TermsUrl { get; set; }

    [JsonPropertyName("additionalInformation")]
    public string? AdditionalInformation { get; set; }

    [JsonPropertyName("isvReseller")]
    public string? IsvReseller { get; set; }

    [JsonPropertyName("ageGatedContent")]
    public bool? AgeGatedContent { get; set; }
}

public class BestPrefillResponse
{
    [JsonPropertyName("prefill")]
    public BestPrefillData Prefill { get; set; } = new();

    [JsonPropertyName("sourceWorkspaceCount")]
    public int SourceWorkspaceCount { get; set; }
}
