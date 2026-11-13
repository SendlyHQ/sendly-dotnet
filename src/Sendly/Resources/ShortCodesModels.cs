using System.Text.Json.Serialization;

namespace Sendly.Resources;

/// <summary>
/// Where a short code stands, reported as <see cref="ShortCode.Status"/> and
/// <see cref="ShortCodeApplication.Status"/>. Values are plain strings so a
/// status added later still deserializes.
/// </summary>
public static class ShortCodeStatus
{
    /// <summary>The application is in, and no code is assigned yet.</summary>
    public const string Requested = "requested";

    /// <summary>A code is being provisioned.</summary>
    public const string Provisioning = "provisioning";

    /// <summary>A code is assigned but not cleared to send.</summary>
    public const string Parked = "parked";

    /// <summary>The code can send.</summary>
    public const string Active = "active";

    /// <summary>Sending from the code is suspended.</summary>
    public const string Suspended = "suspended";

    /// <summary>The code or application was cancelled.</summary>
    public const string Cancelled = "cancelled";
}

/// <summary>
/// Where Sendly's review of a short code application stands, reported as
/// <see cref="ShortCode.ReviewStatus"/> and
/// <see cref="ShortCodeApplication.ReviewStatus"/>. More values may be added.
/// </summary>
public static class ShortCodeReviewStatus
{
    /// <summary>Being filled in.</summary>
    public const string Draft = "draft";

    /// <summary>Submitted; Sendly is reviewing it.</summary>
    public const string AwaitingReview = "awaiting_review";

    /// <summary>Sendly asked for changes. See <see cref="ShortCodeApplication.ReviewNote"/>.</summary>
    public const string ChangesRequested = "changes_requested";

    /// <summary>Approved by Sendly; the carrier forms are being prepared and signed.</summary>
    public const string ApprovedForFiling = "approved_for_filing";

    /// <summary>Filed with the carriers.</summary>
    public const string Filed = "filed";

    /// <summary>Declined by Sendly.</summary>
    public const string Rejected = "rejected";
}

/// <summary>
/// One of the workspace's short code applications, with its code once Sendly
/// assigns one. <see cref="Code"/> is null until then, and drafts, rejected
/// and cancelled applications are listed too.
/// </summary>
public class ShortCode
{
    /// <summary>The application's id.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>The code's digits, or null until Sendly assigns a code.</summary>
    [JsonPropertyName("shortCode")]
    public string? Code { get; set; }

    /// <summary>Always <c>US</c> today.</summary>
    [JsonPropertyName("countryCode")]
    public string CountryCode { get; set; } = string.Empty;

    /// <summary>Where the code stands. See <see cref="ShortCodeStatus"/>.</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>Where Sendly's review stands. See <see cref="ShortCodeReviewStatus"/>.</summary>
    [JsonPropertyName("reviewStatus")]
    public string ReviewStatus { get; set; } = string.Empty;

    /// <summary>What the code is for, as the customer wrote it.</summary>
    [JsonPropertyName("useCase")]
    public string UseCase { get; set; } = string.Empty;

    /// <summary>When the application was started.</summary>
    [JsonPropertyName("createdAt")]
    public string CreatedAt { get; set; } = string.Empty;

    /// <summary>Where the code's setup fee and lease stand.</summary>
    [JsonPropertyName("billing")]
    public ShortCodeListBilling? Billing { get; set; }
}

/// <summary>
/// The response of <see cref="ShortCodesResource.ListAsync"/>.
/// </summary>
public class ShortCodeListResponse
{
    /// <summary>Every application and code of the workspace, newest first.</summary>
    [JsonPropertyName("shortCodes")]
    public List<ShortCode> ShortCodes { get; set; } = new();
}

/// <summary>
/// The workspace's short code application. With no application yet, the
/// empty shape: <see cref="Id"/> is null, the choices show their defaults, and
/// every answer is null.
/// </summary>
public class ShortCodeApplication
{
    /// <summary>The application's id, or null before the first save.</summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>The assigned code's digits, or null until Sendly assigns one.</summary>
    [JsonPropertyName("shortCode")]
    public string? Code { get; set; }

    /// <summary><c>US</c>.</summary>
    [JsonPropertyName("countryCode")]
    public string CountryCode { get; set; } = string.Empty;

    /// <summary>Where the code stands. See <see cref="ShortCodeStatus"/>.</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>Where Sendly's review stands. See <see cref="ShortCodeReviewStatus"/>.</summary>
    [JsonPropertyName("reviewStatus")]
    public string ReviewStatus { get; set; } = string.Empty;

    /// <summary>Sendly's note from the review, such as what to change.</summary>
    [JsonPropertyName("reviewNote")]
    public string? ReviewNote { get; set; }

    /// <summary><c>new</c> or <c>migration</c>.</summary>
    [JsonPropertyName("orderType")]
    public string OrderType { get; set; } = string.Empty;

    /// <summary><c>random</c> or <c>vanity</c>.</summary>
    [JsonPropertyName("codeType")]
    public string CodeType { get; set; } = string.Empty;

    /// <summary>Whether the content provider is the brand itself.</summary>
    [JsonPropertyName("contentProviderSameAsBrand")]
    public bool ContentProviderSameAsBrand { get; set; }

    /// <summary>Example messages.</summary>
    [JsonPropertyName("sampleMessages")]
    public List<string> SampleMessages { get; set; } = new();

    /// <summary>What the code will be used for.</summary>
    [JsonPropertyName("useCase")]
    public string? UseCase { get; set; }

    /// <summary>How subscribers opt in.</summary>
    [JsonPropertyName("optInFlow")]
    public string? OptInFlow { get; set; }

    /// <summary>The message sent when someone opts in.</summary>
    [JsonPropertyName("optInConfirmation")]
    public string? OptInConfirmation { get; set; }

    /// <summary>The reply to HELP.</summary>
    [JsonPropertyName("helpResponse")]
    public string? HelpResponse { get; set; }

    /// <summary>The reply to STOP.</summary>
    [JsonPropertyName("stopConfirmation")]
    public string? StopConfirmation { get; set; }

    /// <summary>How often subscribers get messages.</summary>
    [JsonPropertyName("messageFrequency")]
    public string? MessageFrequency { get; set; }

    /// <summary>The keyword subscribers text to join.</summary>
    [JsonPropertyName("campaignKeyword")]
    public string? CampaignKeyword { get; set; }

    /// <summary>Expected messages a month.</summary>
    [JsonPropertyName("expectedMonthlyVolume")]
    public string? ExpectedMonthlyVolume { get; set; }

    /// <summary>Expected messages a day.</summary>
    [JsonPropertyName("expectedDailyVolume")]
    public string? ExpectedDailyVolume { get; set; }

    /// <summary>The privacy policy URL.</summary>
    [JsonPropertyName("privacyPolicyUrl")]
    public string? PrivacyPolicyUrl { get; set; }

    /// <summary>The terms and conditions URL.</summary>
    [JsonPropertyName("termsUrl")]
    public string? TermsUrl { get; set; }

    /// <summary>The digits asked for: a vanity code, or the code being migrated.</summary>
    [JsonPropertyName("requestedDigits")]
    public string? RequestedDigits { get; set; }

    /// <summary>The provider a migrated code is moving from.</summary>
    [JsonPropertyName("losingProvider")]
    public string? LosingProvider { get; set; }

    /// <summary>The brand's contact for the Short Code Registry.</summary>
    [JsonPropertyName("brandContactName")]
    public string? BrandContactName { get; set; }

    /// <summary>The brand contact's email.</summary>
    [JsonPropertyName("brandContactEmail")]
    public string? BrandContactEmail { get; set; }

    /// <summary>The brand contact's phone number.</summary>
    [JsonPropertyName("brandContactPhone")]
    public string? BrandContactPhone { get; set; }

    /// <summary>The content provider's legal name.</summary>
    [JsonPropertyName("contentProviderLegalName")]
    public string? ContentProviderLegalName { get; set; }

    /// <summary>The content provider's EIN.</summary>
    [JsonPropertyName("contentProviderEin")]
    public string? ContentProviderEin { get; set; }

    /// <summary>The content provider's contact.</summary>
    [JsonPropertyName("contentProviderContactName")]
    public string? ContentProviderContactName { get; set; }

    /// <summary>The content provider contact's email.</summary>
    [JsonPropertyName("contentProviderContactEmail")]
    public string? ContentProviderContactEmail { get; set; }

    /// <summary>The content provider contact's phone number.</summary>
    [JsonPropertyName("contentProviderContactPhone")]
    public string? ContentProviderContactPhone { get; set; }

    /// <summary>Where the brand's Short Code Registry registration stands.</summary>
    [JsonPropertyName("brandRegistrationStatus")]
    public string BrandRegistrationStatus { get; set; } = string.Empty;

    /// <summary>Where the content provider's Short Code Registry registration stands.</summary>
    [JsonPropertyName("contentProviderRegistrationStatus")]
    public string ContentProviderRegistrationStatus { get; set; } = string.Empty;

    /// <summary>When the application was submitted for review.</summary>
    [JsonPropertyName("submittedAt")]
    public string? SubmittedAt { get; set; }

    /// <summary>When Sendly finished reviewing it.</summary>
    [JsonPropertyName("reviewedAt")]
    public string? ReviewedAt { get; set; }

    /// <summary>When it was filed with the carriers.</summary>
    [JsonPropertyName("filedAt")]
    public string? FiledAt { get; set; }

    /// <summary>When the code went live.</summary>
    [JsonPropertyName("activatedAt")]
    public string? ActivatedAt { get; set; }

    /// <summary>When the registry registration is next due to be re-vetted.</summary>
    [JsonPropertyName("registryRevettingDueAt")]
    public string? RegistryRevettingDueAt { get; set; }

    /// <summary>When the application was created.</summary>
    [JsonPropertyName("createdAt")]
    public string? CreatedAt { get; set; }

    /// <summary>When the application last changed.</summary>
    [JsonPropertyName("updatedAt")]
    public string? UpdatedAt { get; set; }
}

/// <summary>
/// Answers to save on the short code application. Every property is
/// optional: one left null is not sent and keeps its saved value, and an
/// empty string clears it. Strings are trimmed. Fields Sendly or the carriers
/// own are not saved and are named back in
/// <see cref="ShortCodeApplicationView.IgnoredFields"/>.
/// </summary>
public class ShortCodeApplicationInput
{
    /// <summary>What the code will be used for.</summary>
    [JsonPropertyName("useCase")]
    public string? UseCase { get; set; }

    /// <summary>How subscribers opt in.</summary>
    [JsonPropertyName("optInFlow")]
    public string? OptInFlow { get; set; }

    /// <summary>The message sent when someone opts in.</summary>
    [JsonPropertyName("optInConfirmation")]
    public string? OptInConfirmation { get; set; }

    /// <summary>
    /// The reply to HELP: the program name, a help contact,
    /// <c>Msg&amp;data rates may apply</c>, the message frequency and how to stop.
    /// </summary>
    [JsonPropertyName("helpResponse")]
    public string? HelpResponse { get; set; }

    /// <summary>The reply to STOP.</summary>
    [JsonPropertyName("stopConfirmation")]
    public string? StopConfirmation { get; set; }

    /// <summary>Specific, such as <c>4 messages per month</c>, or <c>Message frequency varies</c>.</summary>
    [JsonPropertyName("messageFrequency")]
    public string? MessageFrequency { get; set; }

    /// <summary>The join keyword: 2 to 20 letters or numbers, not a reserved word such as STOP or HELP.</summary>
    [JsonPropertyName("campaignKeyword")]
    public string? CampaignKeyword { get; set; }

    /// <summary>Expected messages a month.</summary>
    [JsonPropertyName("expectedMonthlyVolume")]
    public string? ExpectedMonthlyVolume { get; set; }

    /// <summary>Expected messages a day.</summary>
    [JsonPropertyName("expectedDailyVolume")]
    public string? ExpectedDailyVolume { get; set; }

    /// <summary>The privacy policy URL.</summary>
    [JsonPropertyName("privacyPolicyUrl")]
    public string? PrivacyPolicyUrl { get; set; }

    /// <summary>The terms and conditions URL.</summary>
    [JsonPropertyName("termsUrl")]
    public string? TermsUrl { get; set; }

    /// <summary>Only <c>US</c>.</summary>
    [JsonPropertyName("countryCode")]
    public string? CountryCode { get; set; }

    /// <summary><c>new</c> or <c>migration</c>. Any other value answers 422.</summary>
    [JsonPropertyName("orderType")]
    public string? OrderType { get; set; }

    /// <summary><c>random</c> or <c>vanity</c>. Any other value answers 422.</summary>
    [JsonPropertyName("codeType")]
    public string? CodeType { get; set; }

    /// <summary>5 or 6 digits: the vanity code, or the code being migrated.</summary>
    [JsonPropertyName("requestedDigits")]
    public string? RequestedDigits { get; set; }

    /// <summary>For a migration, the provider the code is moving from.</summary>
    [JsonPropertyName("losingProvider")]
    public string? LosingProvider { get; set; }

    /// <summary>The brand's contact for the Short Code Registry.</summary>
    [JsonPropertyName("brandContactName")]
    public string? BrandContactName { get; set; }

    /// <summary>The brand contact's email.</summary>
    [JsonPropertyName("brandContactEmail")]
    public string? BrandContactEmail { get; set; }

    /// <summary>The brand contact's phone number.</summary>
    [JsonPropertyName("brandContactPhone")]
    public string? BrandContactPhone { get; set; }

    /// <summary>Whether the content provider is the brand itself.</summary>
    [JsonPropertyName("contentProviderSameAsBrand")]
    public bool? ContentProviderSameAsBrand { get; set; }

    /// <summary>The content provider's legal name, when it is not the brand.</summary>
    [JsonPropertyName("contentProviderLegalName")]
    public string? ContentProviderLegalName { get; set; }

    /// <summary>The content provider's EIN.</summary>
    [JsonPropertyName("contentProviderEin")]
    public string? ContentProviderEin { get; set; }

    /// <summary>The content provider's contact.</summary>
    [JsonPropertyName("contentProviderContactName")]
    public string? ContentProviderContactName { get; set; }

    /// <summary>The content provider contact's email.</summary>
    [JsonPropertyName("contentProviderContactEmail")]
    public string? ContentProviderContactEmail { get; set; }

    /// <summary>The content provider contact's phone number.</summary>
    [JsonPropertyName("contentProviderContactPhone")]
    public string? ContentProviderContactPhone { get; set; }

    /// <summary>Real examples of the messages. An empty list saves none.</summary>
    [JsonPropertyName("sampleMessages")]
    public List<string>? SampleMessages { get; set; }
}

/// <summary>
/// The body of <see cref="ShortCodeApplicationResource.SubmitAsync"/>: final
/// answers, plus acceptance of the price.
/// </summary>
public class SubmitShortCodeApplicationRequest : ShortCodeApplicationInput
{
    /// <summary>
    /// Accepts the $999 setup fee, charged to the workspace's card when you
    /// submit, the monthly lease from go-live and the 3-month minimum. Submit
    /// is refused with <c>short_code_invalid_application</c> until this is true.
    /// </summary>
    [JsonPropertyName("acceptTerms")]
    public bool? AcceptTerms { get; set; }
}

/// <summary>A compliance problem with a short code application, by field.</summary>
public class ShortCodeIssue
{
    /// <summary>The field the issue is about, such as <c>helpResponse</c> or <c>documents.loa</c>.</summary>
    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    /// <summary>What to change, for people.</summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

/// <summary>A carrier form the application needs, and where it stands.</summary>
public class ShortCodeDocument
{
    /// <summary>The carrier form, such as <c>loa</c> or <c>order_brief</c>.</summary>
    [JsonPropertyName("kind")]
    public string Kind { get; set; } = string.Empty;

    /// <summary>Whether Sendly has prepared the form for signing.</summary>
    [JsonPropertyName("templateReady")]
    public bool TemplateReady { get; set; }

    /// <summary>When the form was signed.</summary>
    [JsonPropertyName("signedAt")]
    public string? SignedAt { get; set; }

    /// <summary>Who signed it.</summary>
    [JsonPropertyName("signedByName")]
    public string? SignedByName { get; set; }

    /// <summary>Whether a signing link is out and has not expired.</summary>
    [JsonPropertyName("awaitingSignature")]
    public bool AwaitingSignature { get; set; }
}

/// <summary>
/// Suggestions from what the workspace has already told Sendly: its 10DLC
/// brand or its toll-free verification. Never saved on the application
/// unless sent back with a save. Fields with no value are null.
/// </summary>
public class ShortCodePrefill
{
    /// <summary><c>tendlc</c>, <c>verification</c> or <c>none</c>.</summary>
    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;

    /// <summary>The business's legal name.</summary>
    [JsonPropertyName("legalName")]
    public string? LegalName { get; set; }

    /// <summary>The name the business trades as.</summary>
    [JsonPropertyName("dba")]
    public string? Dba { get; set; }

    /// <summary>The business's EIN.</summary>
    [JsonPropertyName("ein")]
    public string? Ein { get; set; }

    /// <summary>The business's website.</summary>
    [JsonPropertyName("websiteUrl")]
    public string? WebsiteUrl { get; set; }

    /// <summary>The business address. Only the parts that are known.</summary>
    [JsonPropertyName("address")]
    public ShortCodePrefillAddress? Address { get; set; }

    /// <summary>The business contact. Only the parts that are known.</summary>
    [JsonPropertyName("contact")]
    public ShortCodePrefillContact? Contact { get; set; }

    /// <summary>The privacy policy URL from the toll-free verification.</summary>
    [JsonPropertyName("privacyPolicyUrl")]
    public string? PrivacyPolicyUrl { get; set; }

    /// <summary>The terms URL from the toll-free verification.</summary>
    [JsonPropertyName("termsUrl")]
    public string? TermsUrl { get; set; }

    /// <summary>Whether the toll-free verification uses Sendly's hosted legal pages.</summary>
    [JsonPropertyName("usesHostedLegalPages")]
    public bool? UsesHostedLegalPages { get; set; }
}

/// <summary>A prefilled business address.</summary>
public class ShortCodePrefillAddress
{
    /// <summary>Street line 1.</summary>
    [JsonPropertyName("line1")]
    public string? Line1 { get; set; }

    /// <summary>Street line 2.</summary>
    [JsonPropertyName("line2")]
    public string? Line2 { get; set; }

    /// <summary>City.</summary>
    [JsonPropertyName("city")]
    public string? City { get; set; }

    /// <summary>State.</summary>
    [JsonPropertyName("state")]
    public string? State { get; set; }

    /// <summary>Postal code.</summary>
    [JsonPropertyName("postalCode")]
    public string? PostalCode { get; set; }

    /// <summary>Country code.</summary>
    [JsonPropertyName("countryCode")]
    public string? CountryCode { get; set; }
}

/// <summary>A prefilled business contact.</summary>
public class ShortCodePrefillContact
{
    /// <summary>Name.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>Email.</summary>
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    /// <summary>Phone number.</summary>
    [JsonPropertyName("phone")]
    public string? Phone { get; set; }
}

/// <summary>Wording Sendly suggested for an application.</summary>
public class ShortCodeGeneratedDefaults
{
    /// <summary>The suggested use case.</summary>
    [JsonPropertyName("useCase")]
    public string? UseCase { get; set; }

    /// <summary>The suggested opt-in flow.</summary>
    [JsonPropertyName("optInFlow")]
    public string? OptInFlow { get; set; }
}

/// <summary>
/// The price: the published one for this code type, or once submitted the
/// one fixed at submit.
/// </summary>
public class ShortCodeQuote
{
    /// <summary><c>random</c> or <c>vanity</c>.</summary>
    [JsonPropertyName("codeType")]
    public string CodeType { get; set; } = string.Empty;

    /// <summary>The monthly lease, in US dollars.</summary>
    [JsonPropertyName("monthlyUsd")]
    public decimal MonthlyUsd { get; set; }

    /// <summary>The one-time setup fee, in US dollars.</summary>
    [JsonPropertyName("setupUsd")]
    public decimal SetupUsd { get; set; }

    /// <summary>Always <c>USD</c>.</summary>
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = string.Empty;

    /// <summary>Always false, kept for older clients: <see cref="ShortCodeApplicationView.Billing"/> says what is charged.</summary>
    [JsonPropertyName("autoBilled")]
    public bool AutoBilled { get; set; }

    /// <summary>The one-time setup fee, in US cents, charged at submit.</summary>
    [JsonPropertyName("setupCents")]
    public long SetupCents { get; set; }

    /// <summary>The monthly lease, in US cents, fixed when the application is submitted.</summary>
    [JsonPropertyName("monthlyCents")]
    public long MonthlyCents { get; set; }

    /// <summary>The lease runs at least this many months from go-live.</summary>
    [JsonPropertyName("minimumTermMonths")]
    public int MinimumTermMonths { get; set; }

    /// <summary>When the setup fee is charged: <c>submit</c>.</summary>
    [JsonPropertyName("setupChargedAt")]
    public string? SetupChargedAt { get; set; }

    /// <summary>When the first lease month is charged: <c>go_live</c>.</summary>
    [JsonPropertyName("leaseStartsAt")]
    public string? LeaseStartsAt { get; set; }
}

/// <summary>The oldest unpaid lease charge.</summary>
public class ShortCodeLeasePastDue
{
    /// <summary>The unpaid lease charge.</summary>
    [JsonPropertyName("chargeId")]
    public string ChargeId { get; set; } = string.Empty;

    /// <summary>The amount, in US cents.</summary>
    [JsonPropertyName("amountCents")]
    public long AmountCents { get; set; }

    /// <summary>The charge's status.</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>The start of the month it pays for.</summary>
    [JsonPropertyName("periodStart")]
    public string? PeriodStart { get; set; }

    /// <summary>The end of the month it pays for.</summary>
    [JsonPropertyName("periodEnd")]
    public string? PeriodEnd { get; set; }

    /// <summary>When charging it first failed.</summary>
    [JsonPropertyName("firstFailedAt")]
    public string? FirstFailedAt { get; set; }

    /// <summary>When sending from the code pauses if the month is still unpaid.</summary>
    [JsonPropertyName("pauseAt")]
    public string? PauseAt { get; set; }

    /// <summary>The last payment notice sent.</summary>
    [JsonPropertyName("noticeStage")]
    public string? NoticeStage { get; set; }
}

/// <summary>The monthly lease of a short code.</summary>
public class ShortCodeLease
{
    /// <summary>The monthly lease, in US cents.</summary>
    [JsonPropertyName("monthlyCents")]
    public long MonthlyCents { get; set; }

    /// <summary>The minimum lease term, in months.</summary>
    [JsonPropertyName("minimumTermMonths")]
    public int MinimumTermMonths { get; set; }

    /// <summary>Present on the application: always <c>go_live</c>.</summary>
    [JsonPropertyName("startsAt")]
    public string? StartsAt { get; set; }

    /// <summary>
    /// <c>not_started</c>, <c>running</c>, <c>past_due</c>, <c>paused</c>,
    /// <c>ending</c> or <c>ended</c>. More values may be added.
    /// </summary>
    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;

    /// <summary>Whether Sendly bills the lease yet.</summary>
    [JsonPropertyName("billingEnabled")]
    public bool BillingEnabled { get; set; }

    /// <summary>When the lease started: when the code went live.</summary>
    [JsonPropertyName("startedAt")]
    public string? StartedAt { get; set; }

    /// <summary>When the next month is charged, or null when nothing more is billed.</summary>
    [JsonPropertyName("nextChargeAt")]
    public string? NextChargeAt { get; set; }

    /// <summary>The end of the last month paid for, or waived.</summary>
    [JsonPropertyName("paidThrough")]
    public string? PaidThrough { get; set; }

    /// <summary>When the minimum term ends.</summary>
    [JsonPropertyName("termEndsAt")]
    public string? TermEndsAt { get; set; }

    /// <summary>When a cancelled lease ends.</summary>
    [JsonPropertyName("endsAt")]
    public string? EndsAt { get; set; }

    /// <summary>When the lease was cancelled.</summary>
    [JsonPropertyName("cancelRequestedAt")]
    public string? CancelRequestedAt { get; set; }

    /// <summary>The oldest unpaid month, or null when nothing is unpaid.</summary>
    [JsonPropertyName("pastDue")]
    public ShortCodeLeasePastDue? PastDue { get; set; }

    /// <summary>Months used but not yet charged.</summary>
    [JsonPropertyName("dueMonths")]
    public int DueMonths { get; set; }

    /// <summary>What those months cost, in US cents.</summary>
    [JsonPropertyName("dueCents")]
    public long DueCents { get; set; }
}

/// <summary>One short code charge: the setup fee or a lease month.</summary>
public class ShortCodeCharge
{
    /// <summary>The charge's id.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary><c>setup</c> or <c>lease</c>.</summary>
    [JsonPropertyName("kind")]
    public string Kind { get; set; } = string.Empty;

    /// <summary>The amount, in US cents.</summary>
    [JsonPropertyName("amountCents")]
    public long AmountCents { get; set; }

    /// <summary>
    /// Where the charge stands, such as <c>paid</c>, <c>processing</c>,
    /// <c>failed</c>, <c>waived</c>, <c>refunded</c> or <c>not_charged</c>.
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>When it was paid.</summary>
    [JsonPropertyName("paidAt")]
    public string? PaidAt { get; set; }

    /// <summary>How much was refunded, in US cents.</summary>
    [JsonPropertyName("refundedCents")]
    public long RefundedCents { get; set; }

    /// <summary>When it was refunded.</summary>
    [JsonPropertyName("refundedAt")]
    public string? RefundedAt { get; set; }

    /// <summary>For a lease charge, the start of the month it pays for.</summary>
    [JsonPropertyName("periodStart")]
    public string? PeriodStart { get; set; }

    /// <summary>For a lease charge, the end of the month it pays for.</summary>
    [JsonPropertyName("periodEnd")]
    public string? PeriodEnd { get; set; }

    /// <summary>When the charge was recorded.</summary>
    [JsonPropertyName("createdAt")]
    public string? CreatedAt { get; set; }
}

/// <summary>The one-time setup fee of an application.</summary>
public class ShortCodeSetupFee
{
    /// <summary>The setup fee, in US cents.</summary>
    [JsonPropertyName("amountCents")]
    public long AmountCents { get; set; }

    /// <summary>
    /// <c>unpaid</c>, <c>processing</c>, <c>requires_action</c>, <c>failed</c>,
    /// <c>no_payment_method</c>, <c>paid</c>, <c>waived</c>,
    /// <c>refund_pending</c> or <c>refunded</c>. More values may be added.
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>When the fee is charged: <c>submit</c>. Null on a listed code.</summary>
    [JsonPropertyName("chargedAt")]
    public string? ChargedAt { get; set; }

    /// <summary>When the fee was paid.</summary>
    [JsonPropertyName("paidAt")]
    public string? PaidAt { get; set; }

    /// <summary>How much was refunded, in US cents.</summary>
    [JsonPropertyName("refundedCents")]
    public long RefundedCents { get; set; }

    /// <summary>When the fee was refunded.</summary>
    [JsonPropertyName("refundedAt")]
    public string? RefundedAt { get; set; }
}

/// <summary>The short code terms and their acceptance.</summary>
public class ShortCodeTerms
{
    /// <summary>The current terms.</summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    /// <summary>The terms accepted at submit.</summary>
    [JsonPropertyName("acceptedVersion")]
    public string? AcceptedVersion { get; set; }

    /// <summary>When the terms were accepted.</summary>
    [JsonPropertyName("acceptedAt")]
    public string? AcceptedAt { get; set; }
}

/// <summary>Where the money stands on a short code application.</summary>
public class ShortCodeBilling
{
    /// <summary>The one-time setup fee.</summary>
    [JsonPropertyName("setupFee")]
    public ShortCodeSetupFee SetupFee { get; set; } = new();

    /// <summary>The monthly lease.</summary>
    [JsonPropertyName("lease")]
    public ShortCodeLease Lease { get; set; } = new();

    /// <summary>The terms and their acceptance.</summary>
    [JsonPropertyName("terms")]
    public ShortCodeTerms Terms { get; set; } = new();

    /// <summary>Always <c>full_refund_before_filing</c>.</summary>
    [JsonPropertyName("refundPolicy")]
    public string RefundPolicy { get; set; } = string.Empty;

    /// <summary>
    /// Why the workspace pays no fee, if it does not: <c>mock</c> or
    /// <c>enterprise</c>. Null when it is charged by card.
    /// </summary>
    [JsonPropertyName("exempt")]
    public string? Exempt { get; set; }

    /// <summary>The application's charges.</summary>
    [JsonPropertyName("charges")]
    public List<ShortCodeCharge> Charges { get; set; } = new();
}

/// <summary>Where a listed short code's setup fee and lease stand.</summary>
public class ShortCodeListBilling
{
    /// <summary>The setup fee's status and amount.</summary>
    [JsonPropertyName("setupFee")]
    public ShortCodeSetupFee SetupFee { get; set; } = new();

    /// <summary>The monthly lease.</summary>
    [JsonPropertyName("lease")]
    public ShortCodeLease Lease { get; set; } = new();
}

/// <summary>
/// Whether the workspace's business is verified, which submitting needs.
/// </summary>
public class ShortCodeVerification
{
    /// <summary>Whether the business is verified.</summary>
    [JsonPropertyName("verified")]
    public bool Verified { get; set; }

    /// <summary><c>verified</c>, <c>in_review</c>, <c>needs_attention</c> or <c>not_started</c>.</summary>
    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;

    /// <summary>The dashboard page for the verification.</summary>
    [JsonPropertyName("href")]
    public string Href { get; set; } = string.Empty;
}

/// <summary>One carrier's certification of the code.</summary>
public class ShortCodeCarrierApproval
{
    /// <summary><c>ATT</c>, <c>TMOBILE</c>, <c>VERIZON</c> or <c>US_CELLULAR</c>.</summary>
    [JsonPropertyName("carrier")]
    public string Carrier { get; set; } = string.Empty;

    /// <summary>The carrier's name, such as <c>T-Mobile</c>.</summary>
    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// <c>not_submitted</c>, <c>submitted</c>, <c>in_review</c>,
    /// <c>approved</c> or <c>rejected</c>, as Sendly last recorded it.
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>When the carrier's status was last recorded.</summary>
    [JsonPropertyName("updatedAt")]
    public string? UpdatedAt { get; set; }
}

/// <summary>Where each carrier's certification stands.</summary>
public class ShortCodeCarriers
{
    /// <summary>How many carriers have approved the code.</summary>
    [JsonPropertyName("approved")]
    public int Approved { get; set; }

    /// <summary>How many carriers certify it.</summary>
    [JsonPropertyName("total")]
    public int Total { get; set; }

    /// <summary><c>not_submitted</c>, <c>in_progress</c>, <c>approved</c> or <c>rejected</c>.</summary>
    [JsonPropertyName("overall")]
    public string Overall { get; set; } = string.Empty;

    /// <summary>Every carrier, one entry each.</summary>
    [JsonPropertyName("byCarrier")]
    public List<ShortCodeCarrierApproval> ByCarrier { get; set; } = new();
}

/// <summary>The setup fee a submit collected.</summary>
public class ShortCodeSubmitPayment
{
    /// <summary>The setup fee's status. See <see cref="ShortCodeSetupFee.Status"/>.</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>Whether this call charged the card.</summary>
    [JsonPropertyName("charged")]
    public bool Charged { get; set; }
}

/// <summary>
/// The short code application plus everything needed to act on it. A save
/// adds <see cref="IgnoredFields"/>; a submit adds <see cref="Submitted"/>,
/// <see cref="AlreadySubmitted"/>, <see cref="Notified"/> and
/// <see cref="Payment"/>.
/// </summary>
public class ShortCodeApplicationView
{
    /// <summary>Always true.</summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    /// <summary>The application.</summary>
    [JsonPropertyName("application")]
    public ShortCodeApplication Application { get; set; } = new();

    /// <summary>Suggestions from the workspace's business details.</summary>
    [JsonPropertyName("prefill")]
    public ShortCodePrefill Prefill { get; set; } = new();

    /// <summary>Whether a save can change the application.</summary>
    [JsonPropertyName("editable")]
    public bool Editable { get; set; }

    /// <summary>Why the application cannot be edited, or null while it can.</summary>
    [JsonPropertyName("lockMessage")]
    public string? LockMessage { get; set; }

    /// <summary>
    /// Whether the application is finished (rejected or cancelled), so the
    /// next save starts a new one.
    /// </summary>
    [JsonPropertyName("canStartNewApplication")]
    public bool CanStartNewApplication { get; set; }

    /// <summary>
    /// Wording Sendly suggested for this application. Submit refuses it with
    /// <c>short_code_ai_draft_unedited</c> until a person has edited it. Null
    /// when nothing was suggested.
    /// </summary>
    [JsonPropertyName("generatedDefaults")]
    public ShortCodeGeneratedDefaults? GeneratedDefaults { get; set; }

    /// <summary>The forms this order type needs.</summary>
    [JsonPropertyName("requiredDocuments")]
    public List<string> RequiredDocuments { get; set; } = new();

    /// <summary>The required forms not signed yet.</summary>
    [JsonPropertyName("missingDocuments")]
    public List<string> MissingDocuments { get; set; } = new();

    /// <summary>The forms Sendly has prepared for this application.</summary>
    [JsonPropertyName("documents")]
    public List<ShortCodeDocument> Documents { get; set; } = new();

    /// <summary>The price.</summary>
    [JsonPropertyName("quote")]
    public ShortCodeQuote Quote { get; set; } = new();

    /// <summary>Where the setup fee and the lease stand, and every charge.</summary>
    [JsonPropertyName("billing")]
    public ShortCodeBilling? Billing { get; set; }

    /// <summary>
    /// Whether the workspace's business is verified. Submit is refused with
    /// <c>verification_required</c> (403) until it is.
    /// </summary>
    [JsonPropertyName("verification")]
    public ShortCodeVerification Verification { get; set; } = new();

    /// <summary>Where each carrier's certification stands.</summary>
    [JsonPropertyName("carriers")]
    public ShortCodeCarriers Carriers { get; set; } = new();

    /// <summary>On a submit: always true. Null otherwise.</summary>
    [JsonPropertyName("submitted")]
    public bool? Submitted { get; set; }

    /// <summary>On a submit: true when it was already with Sendly, and nothing was charged.</summary>
    [JsonPropertyName("alreadySubmitted")]
    public bool? AlreadySubmitted { get; set; }

    /// <summary>
    /// On a submit: whether Sendly's review team was told (false when it was
    /// already submitted, or the notice failed).
    /// </summary>
    [JsonPropertyName("notified")]
    public bool? Notified { get; set; }

    /// <summary>On a submit: the setup fee's status and whether this call charged it.</summary>
    [JsonPropertyName("payment")]
    public ShortCodeSubmitPayment? Payment { get; set; }

    /// <summary>On a save: fields Sendly or the carriers own, which were not saved.</summary>
    [JsonPropertyName("ignoredFields")]
    public List<string>? IgnoredFields { get; set; }
}

/// <summary>
/// A dry run of submitting the application, from
/// <see cref="ShortCodeApplicationResource.CheckAsync"/>. Nothing is saved.
/// </summary>
public class ShortCodePreflight
{
    /// <summary>Whether the application can be submitted as it is.</summary>
    [JsonPropertyName("ok")]
    public bool Ok { get; set; }

    /// <summary>Every issue found, all at once.</summary>
    [JsonPropertyName("issues")]
    public List<ShortCodeIssue> Issues { get; set; } = new();

    /// <summary>The forms this order type needs.</summary>
    [JsonPropertyName("requiredDocuments")]
    public List<string> RequiredDocuments { get; set; } = new();

    /// <summary>The required forms not signed yet.</summary>
    [JsonPropertyName("missingDocuments")]
    public List<string> MissingDocuments { get; set; } = new();

    /// <summary>The forms Sendly has prepared for this application.</summary>
    [JsonPropertyName("documents")]
    public List<ShortCodeDocument> Documents { get; set; } = new();

    /// <summary>Fields the body sent that Sendly or the carriers own, which were ignored.</summary>
    [JsonPropertyName("ignoredFields")]
    public List<string>? IgnoredFields { get; set; }
}
