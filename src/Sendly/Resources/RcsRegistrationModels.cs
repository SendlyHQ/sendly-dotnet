using System.Text.Json.Serialization;

namespace Sendly.Resources;

/// <summary>
/// Where a registration sits, in customer terms. Reported as
/// <see cref="RcsBrand.CustomerStage"/>, <see cref="RcsAgentDetail.CustomerStage"/>,
/// <see cref="RcsAgent.Stage"/>, and the top-level <c>Stage</c> of
/// <see cref="RcsRegistration"/>, <see cref="RcsAgentDetailResponse"/> and
/// <see cref="RcsAgentReviewResponse"/>. Values are plain strings so a stage
/// added later still deserializes.
/// </summary>
public static class RcsCustomerStage
{
    /// <summary>Being filled in; nothing submitted yet.</summary>
    public const string Draft = "draft";

    /// <summary>Submitted; Sendly is reviewing it.</summary>
    public const string InReview = "in_review";

    /// <summary>Sendly asked for changes; edit and resubmit. See <c>ReviewNote</c>.</summary>
    public const string ChangesRequested = "changes_requested";

    /// <summary>Sendly declined the registration. See <c>ReviewNote</c>.</summary>
    public const string Rejected = "rejected";

    /// <summary>Approved by Sendly; the carrier network is verifying the brand.</summary>
    public const string BrandVerification = "brand_verification";

    /// <summary>Brand verified; the carrier network is reviewing the agent.</summary>
    public const string AgentReview = "agent_review";

    /// <summary>Approved for invited test devices; fill in the campaign, then request launch.</summary>
    public const string Testing = "testing";

    /// <summary>Launch requested; Sendly is reviewing it.</summary>
    public const string LaunchReview = "launch_review";

    /// <summary>Sendly asked the carrier network to launch the agent.</summary>
    public const string Launching = "launching";

    /// <summary>The carrier network declined the launch. See <c>RejectionReason</c>.</summary>
    public const string LaunchRejected = "launch_rejected";

    /// <summary>Launched; the agent can reach every RCS-capable recipient.</summary>
    public const string Live = "live";

    /// <summary>Sending is currently suspended.</summary>
    public const string Suspended = "suspended";

    /// <summary>Registration failed. See <c>RejectionReason</c>.</summary>
    public const string Failed = "failed";
}

/// <summary>
/// Review status of a brand or agent, as Sendly tracks it. Reported as
/// <see cref="RcsBrand.ReviewStatus"/> and <see cref="RcsAgentDetail.ReviewStatus"/>.
/// </summary>
public static class RcsReviewStatus
{
    /// <summary>Editable; not submitted.</summary>
    public const string Draft = "draft";

    /// <summary>Submitted; locked while Sendly reviews it.</summary>
    public const string AwaitingReview = "awaiting_review";

    /// <summary>Editable again. See <c>ReviewNote</c>.</summary>
    public const string ChangesRequested = "changes_requested";

    /// <summary>Approved by Sendly and sent to the carrier network.</summary>
    public const string ApprovedForCarrier = "approved_for_carrier";

    /// <summary>Declined by Sendly. See <c>ReviewNote</c>.</summary>
    public const string Rejected = "rejected";

    /// <summary>Launch requested; locked while Sendly reviews it.</summary>
    public const string LaunchRequested = "launch_requested";

    /// <summary>Launch sent to the carrier network.</summary>
    public const string LaunchSubmitted = "launch_submitted";

    /// <summary>Launch declined by the carrier network. See <c>RejectionReason</c>.</summary>
    public const string LaunchRejected = "launch_rejected";

    /// <summary>Registration failed. See <c>RejectionReason</c>.</summary>
    public const string Failed = "failed";
}

/// <summary>
/// Error codes the RCS registration endpoints answer with, surfaced as
/// <see cref="Sendly.Exceptions.SendlyException.ApiErrorCode"/>. The HTTP
/// status decides which exception type is thrown: 404 →
/// <see cref="Sendly.Exceptions.NotFoundException"/>, 400/422 →
/// <see cref="Sendly.Exceptions.ValidationException"/>, 401 →
/// <see cref="Sendly.Exceptions.AuthenticationException"/>, 429 →
/// <see cref="Sendly.Exceptions.RateLimitException"/>, everything else
/// (403, 409, 5xx) → <see cref="Sendly.Exceptions.SendlyException"/> with
/// <c>StatusCode</c> set.
/// </summary>
public static class RcsErrorCode
{
    /// <summary>404 — RCS registration isn't enabled for this account yet.</summary>
    public const string NotEnabled = "rcs_not_enabled";

    /// <summary>404 — no brand or agent with that id in this workspace.</summary>
    public const string NotFound = "rcs_not_found";

    /// <summary>409 — the record is locked while under review, or already submitted.</summary>
    public const string FieldLocked = "rcs_field_locked";

    /// <summary>422 — the brand address is outside the US.</summary>
    public const string UsOnly = "rcs_us_only";

    /// <summary>422 — one or more fields are invalid or missing; see <c>FieldErrors</c>.</summary>
    public const string InvalidContent = "rcs_invalid_content";

    /// <summary>409 — the carrier network declined the brand, so the agent can't be submitted.</summary>
    public const string BrandNotVerified = "rcs_brand_not_verified";

    /// <summary>409 — the agent hasn't reached testing yet, so launch can't be requested.</summary>
    public const string LaunchNotReady = "rcs_launch_not_ready";

    /// <summary>403 — the API key lacks the <c>rcs:read</c> / <c>rcs:write</c> scope.</summary>
    public const string InsufficientPermissions = "insufficient_permissions";

    /// <summary>403 — the key's workspace role can't manage registrations.</summary>
    public const string Forbidden = "forbidden";

    /// <summary>500 — something went wrong on Sendly's side; retry later.</summary>
    public const string InternalError = "rcs_internal_error";

    /// <summary>
    /// 429 from any endpoint — too many requests from this address used a
    /// wrong API key, so keys are refused until <c>RetryAfter</c> has passed.
    /// The client does not retry it; check the key.
    /// </summary>
    public const string TooManyFailedKeyAttempts = "too_many_failed_key_attempts";

    /// <summary>
    /// 429 from any endpoint — too many API key checks were already running
    /// for the account from this address. Transient; the client retries it
    /// after <c>RetryAfter</c> (1 second).
    /// </summary>
    public const string TooManyConcurrentVerifications = "too_many_concurrent_verifications";
}

/// <summary>
/// Legal structure of the registering business (<see cref="RcsBrandInput.LegalEntityType"/>).
/// </summary>
public static class RcsLegalEntityType
{
    public const string LimitedLiabilityCompany = "LIMITED_LIABILITY_COMPANY";
    public const string SoleProprietorship = "SOLE_PROPRIETORSHIP";
    public const string Partnership = "PARTNERSHIP";
    public const string Corporation = "CORPORATION";
    public const string SCorporation = "S_CORPORATION";
}

/// <summary>
/// Organization type of the registering business (<see cref="RcsBrandInput.OrganizationType"/>).
/// </summary>
public static class RcsOrganizationType
{
    public const string PrivateProfit = "PRIVATE_PROFIT";
    public const string PublicProfit = "PUBLIC_PROFIT";
    public const string NonProfit = "NON_PROFIT";
    public const string Government = "GOVERNMENT";
    public const string Unknown = "UNKNOWN";
}

/// <summary>
/// Declared messaging use case of an agent (<see cref="RcsAgentBasicsInput.UseCase"/>).
/// </summary>
public static class RcsAgentUseCase
{
    public const string MultiUse = "MULTI_USE";
    public const string Promotional = "PROMOTIONAL";
    public const string Transactional = "TRANSACTIONAL";
    public const string Otp = "OTP";
}

/// <summary>
/// Kind of conversation an agent has with recipients (<see cref="RcsInteraction.InteractionType"/>).
/// </summary>
public static class RcsInteractionType
{
    public const string TransactionalUpdates = "TRANSACTIONAL_UPDATES";
    public const string CustomerSupport = "CUSTOMER_SUPPORT";
    public const string LoyaltyOrReward = "LOYALTY_OR_REWARD";
    public const string MarketingOrPromotional = "MARKETING_OR_PROMOTIONAL";
    public const string AccountAlerts = "ACCOUNT_ALERTS";
    public const string TwoWayConversation = "TWO_WAY_CONVERSATION";
    public const string Other = "OTHER";
}

/// <summary>
/// How recipients opt in to messages from an agent (<see cref="RcsOptInMethod.MethodType"/>).
/// </summary>
public static class RcsOptInMethodType
{
    public const string Sms = "SMS";
    public const string Website = "WEBSITE";
    public const string MobileApp = "MOBILE_APP";
    public const string QrCode = "QR_CODE";
    public const string SalePoint = "SALE_POINT";
    public const string Other = "OTHER";
}

/// <summary>
/// Where <see cref="RcsDossier.Brand"/> was prefilled from (<see cref="RcsDossier.Source"/>).
/// </summary>
public static class RcsDossierSource
{
    /// <summary>The workspace's newest 10DLC brand.</summary>
    public const string TenDlc = "tendlc";

    /// <summary>The workspace's active toll-free verification.</summary>
    public const string Verification = "verification";

    /// <summary>Nothing on file; <see cref="RcsDossier.Brand"/> is empty.</summary>
    public const string None = "none";
}

/// <summary>
/// Registered business address on a brand draft. <see cref="CountryCode"/>
/// must be <c>US</c> — RCS registration is available to US businesses for now.
/// Null values are omitted from the request.
/// </summary>
public class RcsBrandAddressInput
{
    /// <summary>Street address, first line.</summary>
    [JsonPropertyName("line1")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Line1 { get; set; }

    /// <summary>Street address, second line.</summary>
    [JsonPropertyName("line2")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Line2 { get; set; }

    /// <summary>City.</summary>
    [JsonPropertyName("city")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? City { get; set; }

    /// <summary>State (two-letter code).</summary>
    [JsonPropertyName("state")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? State { get; set; }

    /// <summary>ZIP / postal code.</summary>
    [JsonPropertyName("postalCode")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PostalCode { get; set; }

    /// <summary>ISO 3166-1 alpha-2 country code; must be "US".</summary>
    [JsonPropertyName("countryCode")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CountryCode { get; set; }
}

/// <summary>
/// Business contact on a brand draft — who the carrier network can reach
/// about the registration. Null values are omitted from the request.
/// </summary>
public class RcsBrandContactInput
{
    /// <summary>Contact's first name.</summary>
    [JsonPropertyName("firstName")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FirstName { get; set; }

    /// <summary>Contact's last name.</summary>
    [JsonPropertyName("lastName")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? LastName { get; set; }

    /// <summary>Contact's job title.</summary>
    [JsonPropertyName("title")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Title { get; set; }

    /// <summary>Contact's email address.</summary>
    [JsonPropertyName("email")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Email { get; set; }

    /// <summary>Contact's phone number in E.164 format.</summary>
    [JsonPropertyName("phoneNumber")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PhoneNumber { get; set; }
}

/// <summary>
/// Brand fields for <see cref="RcsBrandsResource.CreateAsync"/> and
/// <see cref="RcsBrandsResource.UpdateAsync"/>, and the shape
/// <see cref="RcsDossier.Brand"/> prefills.
///
/// Every field is optional while drafting — required-field checks run at
/// <see cref="RcsAgentsResource.SubmitAsync"/>, which reports each gap as a
/// <c>brand.&lt;field&gt;</c> entry in
/// <see cref="Sendly.Exceptions.SendlyException.FieldErrors"/>. On update,
/// only the fields you set are changed; leave a field null to keep its
/// current value, or send an empty string to clear it. <see cref="Address"/>
/// and <see cref="Contact"/> may be partial.
/// </summary>
public class RcsBrandInput
{
    /// <summary>The brand name recipients see.</summary>
    [JsonPropertyName("displayName")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DisplayName { get; set; }

    /// <summary>Legal business name.</summary>
    [JsonPropertyName("legalName")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? LegalName { get; set; }

    /// <summary>Legal structure of the business; see <see cref="RcsLegalEntityType"/>.</summary>
    [JsonPropertyName("legalEntityType")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? LegalEntityType { get; set; }

    /// <summary>Organization type; see <see cref="RcsOrganizationType"/>.</summary>
    [JsonPropertyName("organizationType")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? OrganizationType { get; set; }

    /// <summary>Business website (https).</summary>
    [JsonPropertyName("websiteUrl")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? WebsiteUrl { get; set; }

    /// <summary>Employer Identification Number ("123456789" or "12-3456789").</summary>
    [JsonPropertyName("ein")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Ein { get; set; }

    /// <summary>Stock symbol as "EXCHANGE:TICKER", for publicly traded businesses.</summary>
    [JsonPropertyName("stockSymbol")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? StockSymbol { get; set; }

    /// <summary>Registered business address; <see cref="RcsBrandAddressInput.CountryCode"/> must be "US".</summary>
    [JsonPropertyName("address")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public RcsBrandAddressInput? Address { get; set; }

    /// <summary>Business contact.</summary>
    [JsonPropertyName("contact")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public RcsBrandContactInput? Contact { get; set; }
}

/// <summary>
/// Phone contact shown on the agent's info sheet.
/// </summary>
public class RcsAgentPhoneContact
{
    /// <summary>Phone number in E.164 format.</summary>
    [JsonPropertyName("number")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Number { get; set; }

    /// <summary>Label recipients see, e.g. "Call support".</summary>
    [JsonPropertyName("label")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Label { get; set; }
}

/// <summary>
/// Website link shown on the agent's info sheet.
/// </summary>
public class RcsAgentWebsiteContact
{
    /// <summary>Website URL (https).</summary>
    [JsonPropertyName("url")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Url { get; set; }

    /// <summary>Label recipients see, e.g. "Visit our site".</summary>
    [JsonPropertyName("label")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Label { get; set; }
}

/// <summary>
/// Email contact shown on the agent's info sheet.
/// </summary>
public class RcsAgentEmailContact
{
    /// <summary>Email address.</summary>
    [JsonPropertyName("address")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Address { get; set; }

    /// <summary>Label recipients see, e.g. "Email us".</summary>
    [JsonPropertyName("label")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Label { get; set; }
}

/// <summary>
/// Agent identity — what recipients see when they open the agent.
///
/// <see cref="LogoUrl"/> and <see cref="HeroUrl"/> must be public
/// <c>https://</c> URLs; uploading assets is dashboard-only. Null values are
/// omitted from the request, so on update only the fields you set change.
/// </summary>
public class RcsAgentBasicsInput
{
    /// <summary>The agent name recipients see.</summary>
    [JsonPropertyName("displayName")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DisplayName { get; set; }

    /// <summary>Declared messaging use case; see <see cref="RcsAgentUseCase"/>.</summary>
    [JsonPropertyName("useCase")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? UseCase { get; set; }

    /// <summary>What the agent is for, shown on its info sheet.</summary>
    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; }

    /// <summary>Public https:// URL of the agent's logo.</summary>
    [JsonPropertyName("logoUrl")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? LogoUrl { get; set; }

    /// <summary>Public https:// URL of the agent's hero image.</summary>
    [JsonPropertyName("heroUrl")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? HeroUrl { get; set; }

    /// <summary>Brand colour as "#RGB" or "#RRGGBB".</summary>
    [JsonPropertyName("brandColor")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? BrandColor { get; set; }

    /// <summary>Privacy policy URL (https).</summary>
    [JsonPropertyName("privacyPolicyUrl")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PrivacyPolicyUrl { get; set; }

    /// <summary>Terms and conditions URL (https).</summary>
    [JsonPropertyName("termsAndConditionsUrl")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TermsAndConditionsUrl { get; set; }

    /// <summary>Phone contact on the info sheet.</summary>
    [JsonPropertyName("phoneNumber")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public RcsAgentPhoneContact? PhoneNumber { get; set; }

    /// <summary>Website link on the info sheet.</summary>
    [JsonPropertyName("website")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public RcsAgentWebsiteContact? Website { get; set; }

    /// <summary>Email contact on the info sheet.</summary>
    [JsonPropertyName("email")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public RcsAgentEmailContact? Email { get; set; }
}

/// <summary>
/// Agent identity as stored — <see cref="RcsAgentBasicsInput"/> with the
/// server-owned fields filled in. Optional fields are null until set.
/// </summary>
public class RcsAgentBasics : RcsAgentBasicsInput
{
    /// <summary>Hosting region chosen by Sendly, or null until provisioned.</summary>
    [JsonPropertyName("hostingRegion")]
    public string? HostingRegion { get; set; }
}

/// <summary>
/// One kind of conversation the agent has with recipients.
/// </summary>
public class RcsInteraction
{
    /// <summary>Kind of interaction; see <see cref="RcsInteractionType"/>.</summary>
    [JsonPropertyName("interactionType")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? InteractionType { get; set; }

    /// <summary>What that interaction looks like for your recipients.</summary>
    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; }
}

/// <summary>
/// One way recipients opt in to the agent's messages.
/// </summary>
public class RcsOptInMethod
{
    /// <summary>Opt-in channel; see <see cref="RcsOptInMethodType"/>.</summary>
    [JsonPropertyName("methodType")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MethodType { get; set; }

    /// <summary>How the opt-in works on that channel.</summary>
    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; }
}

/// <summary>
/// How recipients consent to messages, and the standard replies.
///
/// <see cref="CallToActionMediaUrl"/> must be a public <c>https://</c> URL;
/// uploading assets is dashboard-only.
/// </summary>
public class RcsConsentSettings
{
    /// <summary>Ways recipients opt in.</summary>
    [JsonPropertyName("optInMethods")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<RcsOptInMethod>? OptInMethods { get; set; }

    /// <summary>The call to action recipients see when opting in.</summary>
    [JsonPropertyName("callToAction")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CallToAction { get; set; }

    /// <summary>Where the opt-in call to action lives (https).</summary>
    [JsonPropertyName("callToActionUrl")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CallToActionUrl { get; set; }

    /// <summary>Public https:// URL of a screenshot or image of the opt-in.</summary>
    [JsonPropertyName("callToActionMediaUrl")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CallToActionMediaUrl { get; set; }

    /// <summary>Whether recipients confirm their opt-in a second time.</summary>
    [JsonPropertyName("doubleOptIn")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? DoubleOptIn { get; set; }

    /// <summary>The confirmation message, when <see cref="DoubleOptIn"/> is true.</summary>
    [JsonPropertyName("doubleOptInMessage")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DoubleOptInMessage { get; set; }

    /// <summary>Message sent when a recipient opts in.</summary>
    [JsonPropertyName("optInMessage")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? OptInMessage { get; set; }

    /// <summary>Reply to a HELP request.</summary>
    [JsonPropertyName("helpResponse")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? HelpResponse { get; set; }

    /// <summary>Reply to a STOP request.</summary>
    [JsonPropertyName("optOutResponse")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? OptOutResponse { get; set; }
}

/// <summary>
/// Campaign section of an agent — what it sends and how recipients agreed
/// to it. Optional while drafting; required before
/// <see cref="RcsAgentsResource.RequestLaunchAsync"/> (an overview, at least
/// one interaction, at least three message examples, and consent settings).
/// On update the fields you set are merged into the stored section.
/// </summary>
public class RcsCampaign
{
    /// <summary>What your business does.</summary>
    [JsonPropertyName("companyOverview")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CompanyOverview { get; set; }

    /// <summary>What the agent sends and why.</summary>
    [JsonPropertyName("agentOverview")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AgentOverview { get; set; }

    /// <summary>Anything else reviewers should know.</summary>
    [JsonPropertyName("additionalInformation")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AdditionalInformation { get; set; }

    /// <summary>Kinds of conversations the agent has.</summary>
    [JsonPropertyName("interactions")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<RcsInteraction>? Interactions { get; set; }

    /// <summary>Example messages the agent sends (at least three at launch).</summary>
    [JsonPropertyName("messageExamples")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? MessageExamples { get; set; }

    /// <summary>How recipients consent, and the standard replies.</summary>
    [JsonPropertyName("consentSettings")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public RcsConsentSettings? ConsentSettings { get; set; }
}

/// <summary>
/// Testing section of an agent — how reviewers can see the agent in action
/// before launch. On update the fields you set are merged into the stored
/// section.
/// </summary>
public class RcsTesting
{
    /// <summary>URL where reviewers can trigger a test message (required at launch).</summary>
    [JsonPropertyName("testUrl")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TestUrl { get; set; }

    /// <summary>Identifier of a message sent to an invited test device.</summary>
    [JsonPropertyName("messageId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MessageId { get; set; }

    /// <summary>Anything else reviewers should know about testing.</summary>
    [JsonPropertyName("additionalInformation")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AdditionalInformation { get; set; }
}

/// <summary>
/// Body for <see cref="RcsAgentsResource.CreateAsync"/>. Null values are omitted.
/// </summary>
public class CreateRcsAgentRequest
{
    /// <summary>The brand this agent belongs to. Required.</summary>
    [JsonPropertyName("brandId")]
    public string BrandId { get; set; } = string.Empty;

    /// <summary>The agent name recipients see; overrides <c>Basics.DisplayName</c>.</summary>
    [JsonPropertyName("displayName")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DisplayName { get; set; }

    /// <summary>Declared messaging use case; overrides <c>Basics.UseCase</c>. See <see cref="RcsAgentUseCase"/>.</summary>
    [JsonPropertyName("useCase")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? UseCase { get; set; }

    /// <summary>Agent identity.</summary>
    [JsonPropertyName("basics")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public RcsAgentBasicsInput? Basics { get; set; }

    /// <summary>Campaign section; can be filled in later with <see cref="RcsAgentsResource.UpdateAsync"/>.</summary>
    [JsonPropertyName("campaign")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public RcsCampaign? Campaign { get; set; }

    /// <summary>Testing section; can be filled in later with <see cref="RcsAgentsResource.UpdateAsync"/>.</summary>
    [JsonPropertyName("testing")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public RcsTesting? Testing { get; set; }
}

/// <summary>
/// Body for <see cref="RcsAgentsResource.UpdateAsync"/>. Only the sections
/// you set are changed: <see cref="DisplayName"/>, <see cref="UseCase"/> and
/// <see cref="Basics"/> merge into the agent identity; <see cref="Campaign"/>
/// and <see cref="Testing"/> merge field-wise into their stored sections.
/// Set <see cref="ClearCampaign"/> / <see cref="ClearTesting"/> to remove a
/// section entirely.
/// </summary>
public class UpdateRcsAgentRequest
{
    /// <summary>The agent name recipients see.</summary>
    [JsonPropertyName("displayName")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DisplayName { get; set; }

    /// <summary>Declared messaging use case; see <see cref="RcsAgentUseCase"/>.</summary>
    [JsonPropertyName("useCase")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? UseCase { get; set; }

    /// <summary>Agent identity fields to merge.</summary>
    [JsonPropertyName("basics")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public RcsAgentBasicsInput? Basics { get; set; }

    /// <summary>Campaign fields to merge.</summary>
    [JsonPropertyName("campaign")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public RcsCampaign? Campaign { get; set; }

    /// <summary>Testing fields to merge.</summary>
    [JsonPropertyName("testing")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public RcsTesting? Testing { get; set; }

    /// <summary>True to remove the campaign section entirely (sends <c>campaign: null</c>). Takes precedence over <see cref="Campaign"/>.</summary>
    [JsonIgnore]
    public bool ClearCampaign { get; set; }

    /// <summary>True to remove the testing section entirely (sends <c>testing: null</c>). Takes precedence over <see cref="Testing"/>.</summary>
    [JsonIgnore]
    public bool ClearTesting { get; set; }
}

/// <summary>
/// A device to invite for <see cref="RcsAgentsResource.SetTestDevicesAsync"/>.
/// </summary>
public class RcsTestDeviceInput
{
    public RcsTestDeviceInput()
    {
    }

    /// <param name="phoneNumber">Device phone number in E.164 format (a formatted 10-digit US number is also accepted)</param>
    /// <param name="label">Friendly label, e.g. "Sam's Pixel"</param>
    public RcsTestDeviceInput(string phoneNumber, string? label = null)
    {
        PhoneNumber = phoneNumber;
        Label = label;
    }

    /// <summary>Device phone number in E.164 format (a formatted 10-digit US number is also accepted). Required.</summary>
    [JsonPropertyName("phoneNumber")]
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>Friendly label, e.g. "Sam's Pixel".</summary>
    [JsonPropertyName("label")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Label { get; set; }
}

/// <summary>
/// Body for <see cref="RcsAgentsResource.RequestLaunchAsync"/>. Both fields
/// are optional; when set they are saved to the agent's testing section
/// before the launch request is recorded.
/// </summary>
public class RcsRequestLaunchRequest
{
    /// <summary>URL where reviewers can trigger a test message.</summary>
    [JsonPropertyName("testUrl")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TestUrl { get; set; }

    /// <summary>Anything else reviewers should know about testing.</summary>
    [JsonPropertyName("testingAdditionalInformation")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TestingAdditionalInformation { get; set; }
}

/// <summary>
/// Registered business address on a brand.
/// </summary>
public class RcsBrandAddress
{
    /// <summary>Street address, first line ("" while unset).</summary>
    [JsonPropertyName("line1")]
    public string Line1 { get; set; } = string.Empty;

    /// <summary>Street address, second line.</summary>
    [JsonPropertyName("line2")]
    public string? Line2 { get; set; }

    /// <summary>City ("" while unset).</summary>
    [JsonPropertyName("city")]
    public string City { get; set; } = string.Empty;

    /// <summary>State ("" while unset).</summary>
    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;

    /// <summary>ZIP / postal code ("" while unset).</summary>
    [JsonPropertyName("postalCode")]
    public string PostalCode { get; set; } = string.Empty;

    /// <summary>ISO 3166-1 alpha-2 country code ("US").</summary>
    [JsonPropertyName("countryCode")]
    public string CountryCode { get; set; } = string.Empty;
}

/// <summary>
/// Business contact on a brand.
/// </summary>
public class RcsBrandContact
{
    /// <summary>Contact's first name ("" while unset).</summary>
    [JsonPropertyName("firstName")]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>Contact's last name ("" while unset).</summary>
    [JsonPropertyName("lastName")]
    public string LastName { get; set; } = string.Empty;

    /// <summary>Contact's job title.</summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>Contact's email address ("" while unset).</summary>
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    /// <summary>Contact's phone number in E.164 format ("" while unset).</summary>
    [JsonPropertyName("phoneNumber")]
    public string PhoneNumber { get; set; } = string.Empty;
}

/// <summary>
/// A brand — the business identity an agent is registered under.
/// </summary>
public class RcsBrand
{
    /// <summary>Unique brand identifier.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Review status, as Sendly tracks it; see <see cref="RcsReviewStatus"/>.</summary>
    [JsonPropertyName("reviewStatus")]
    public string ReviewStatus { get; set; } = string.Empty;

    /// <summary>Where the brand sits, in customer terms; see <see cref="RcsCustomerStage"/>.</summary>
    [JsonPropertyName("customerStage")]
    public string CustomerStage { get; set; } = string.Empty;

    /// <summary>The brand name recipients see ("" while unset).</summary>
    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Legal business name ("" while unset).</summary>
    [JsonPropertyName("legalName")]
    public string LegalName { get; set; } = string.Empty;

    /// <summary>Legal structure of the business ("" while unset); see <see cref="RcsLegalEntityType"/>.</summary>
    [JsonPropertyName("legalEntityType")]
    public string LegalEntityType { get; set; } = string.Empty;

    /// <summary>Organization type ("" while unset); see <see cref="RcsOrganizationType"/>.</summary>
    [JsonPropertyName("organizationType")]
    public string OrganizationType { get; set; } = string.Empty;

    /// <summary>Stock symbol as "EXCHANGE:TICKER", for publicly traded businesses.</summary>
    [JsonPropertyName("stockSymbol")]
    public string? StockSymbol { get; set; }

    /// <summary>Business website ("" while unset).</summary>
    [JsonPropertyName("websiteUrl")]
    public string WebsiteUrl { get; set; } = string.Empty;

    /// <summary>Employer Identification Number ("" while unset).</summary>
    [JsonPropertyName("ein")]
    public string Ein { get; set; } = string.Empty;

    /// <summary>Registered business address.</summary>
    [JsonPropertyName("address")]
    public RcsBrandAddress Address { get; set; } = new();

    /// <summary>Business contact.</summary>
    [JsonPropertyName("contact")]
    public RcsBrandContact Contact { get; set; } = new();

    /// <summary>Sendly's note from review, when changes were requested or the brand was declined.</summary>
    [JsonPropertyName("reviewNote")]
    public string? ReviewNote { get; set; }

    /// <summary>Why the carrier network declined the brand, when it did.</summary>
    [JsonPropertyName("rejectionReason")]
    public string? RejectionReason { get; set; }

    /// <summary>When the brand was submitted for review (ISO 8601), or null.</summary>
    [JsonPropertyName("submittedForReviewAt")]
    public string? SubmittedForReviewAt { get; set; }

    /// <summary>When the brand was sent to the carrier network (ISO 8601), or null.</summary>
    [JsonPropertyName("sentToCarrierAt")]
    public string? SentToCarrierAt { get; set; }

    /// <summary>When the carrier network verified the brand (ISO 8601), or null.</summary>
    [JsonPropertyName("verifiedAt")]
    public string? VerifiedAt { get; set; }

    /// <summary>When the brand was created (ISO 8601).</summary>
    [JsonPropertyName("createdAt")]
    public string CreatedAt { get; set; } = string.Empty;

    /// <summary>When the brand was last updated (ISO 8601).</summary>
    [JsonPropertyName("updatedAt")]
    public string UpdatedAt { get; set; } = string.Empty;
}

/// <summary>
/// A device invited to test an agent before launch.
/// </summary>
public class RcsTestDevice
{
    /// <summary>Unique device identifier.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Device phone number in E.164 format.</summary>
    [JsonPropertyName("phoneNumber")]
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>Friendly label.</summary>
    [JsonPropertyName("label")]
    public string? Label { get; set; }

    /// <summary>Invite state reported by the carrier network (e.g. "PENDING"), or null until invited.</summary>
    [JsonPropertyName("inviteStatus")]
    public string? InviteStatus { get; set; }

    /// <summary>When the device was added (ISO 8601).</summary>
    [JsonPropertyName("createdAt")]
    public string CreatedAt { get; set; } = string.Empty;
}

/// <summary>
/// The full agent record — identity, campaign, testing, review state, and
/// invited devices. <see cref="RcsAgent"/> is the lighter shape returned by
/// <see cref="RcsAgentsResource.ListAsync"/>.
/// </summary>
public class RcsAgentDetail
{
    /// <summary>Unique agent identifier — pass as <c>AgentId</c> on sends.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>The brand this agent belongs to.</summary>
    [JsonPropertyName("brandId")]
    public string? BrandId { get; set; }

    /// <summary>
    /// Lifecycle (send) status: <c>draft</c>, <c>submitted</c>,
    /// <c>testing</c> (reaches invited test devices), <c>approved</c>
    /// (reaches everyone), or <c>suspended</c>.
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>Review status, as Sendly tracks it; see <see cref="RcsReviewStatus"/>.</summary>
    [JsonPropertyName("reviewStatus")]
    public string ReviewStatus { get; set; } = string.Empty;

    /// <summary>Where the registration sits, in customer terms; see <see cref="RcsCustomerStage"/>.</summary>
    [JsonPropertyName("customerStage")]
    public string CustomerStage { get; set; } = string.Empty;

    /// <summary>The agent name recipients see ("" while unset).</summary>
    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Declared messaging use case, or null when not set.</summary>
    [JsonPropertyName("useCase")]
    public string? UseCase { get; set; }

    /// <summary>Hosting region chosen by Sendly, or null until provisioned.</summary>
    [JsonPropertyName("hostingRegion")]
    public string? HostingRegion { get; set; }

    /// <summary>Agent identity.</summary>
    [JsonPropertyName("basics")]
    public RcsAgentBasics Basics { get; set; } = new();

    /// <summary>Campaign section, or null until filled in.</summary>
    [JsonPropertyName("campaign")]
    public RcsCampaign? Campaign { get; set; }

    /// <summary>Testing section, or null until filled in.</summary>
    [JsonPropertyName("testing")]
    public RcsTesting? Testing { get; set; }

    /// <summary>Sendly's note from review, when changes were requested or the agent was declined.</summary>
    [JsonPropertyName("reviewNote")]
    public string? ReviewNote { get; set; }

    /// <summary>Why the carrier network declined the agent or its launch, when it did.</summary>
    [JsonPropertyName("rejectionReason")]
    public string? RejectionReason { get; set; }

    /// <summary>Devices invited to test the agent.</summary>
    [JsonPropertyName("testDevices")]
    public List<RcsTestDevice> TestDevices { get; set; } = new();

    /// <summary>When the agent was submitted for review (ISO 8601), or null.</summary>
    [JsonPropertyName("submittedForReviewAt")]
    public string? SubmittedForReviewAt { get; set; }

    /// <summary>When the agent identity was sent to the carrier network (ISO 8601), or null.</summary>
    [JsonPropertyName("basicsSubmittedAt")]
    public string? BasicsSubmittedAt { get; set; }

    /// <summary>When the launch was sent to the carrier network (ISO 8601), or null.</summary>
    [JsonPropertyName("launchSubmittedAt")]
    public string? LaunchSubmittedAt { get; set; }

    /// <summary>When the agent went live (ISO 8601), or null.</summary>
    [JsonPropertyName("liveAt")]
    public string? LiveAt { get; set; }

    /// <summary>When the agent was created (ISO 8601).</summary>
    [JsonPropertyName("createdAt")]
    public string CreatedAt { get; set; } = string.Empty;

    /// <summary>When the agent was last updated (ISO 8601).</summary>
    [JsonPropertyName("updatedAt")]
    public string UpdatedAt { get; set; } = string.Empty;
}

/// <summary>
/// Response from <see cref="RcsRegistrationResource.GetAsync"/> — the
/// workspace's current registration at a glance.
/// </summary>
public class RcsRegistration
{
    /// <summary>The newest agent's brand, else the newest brand, or null when none exists.</summary>
    [JsonPropertyName("brand")]
    public RcsBrand? Brand { get; set; }

    /// <summary>The newest agent, or null when none exists.</summary>
    [JsonPropertyName("agent")]
    public RcsAgentDetail? Agent { get; set; }

    /// <summary>Devices invited to test that agent (empty when there is no agent).</summary>
    [JsonPropertyName("devices")]
    public List<RcsTestDevice> Devices { get; set; } = new();

    /// <summary>Where the registration sits, in customer terms (<c>draft</c> when nothing exists); see <see cref="RcsCustomerStage"/>.</summary>
    [JsonPropertyName("stage")]
    public string Stage { get; set; } = string.Empty;

    /// <summary>False when something on file names a non-US country.</summary>
    [JsonPropertyName("usEligible")]
    public bool UsEligible { get; set; }
}

/// <summary>
/// Response from <see cref="RcsDossierResource.GetAsync"/> — business details
/// already on file, shaped as an <see cref="RcsBrandInput"/> you can pass
/// straight to <see cref="RcsBrandsResource.CreateAsync"/>.
/// </summary>
public class RcsDossier
{
    /// <summary>Prefilled brand fields (only the ones that have a value).</summary>
    [JsonPropertyName("brand")]
    public RcsBrandInput Brand { get; set; } = new();

    /// <summary>False when something on file names a non-US country.</summary>
    [JsonPropertyName("usEligible")]
    public bool UsEligible { get; set; }

    /// <summary>Where the details came from; see <see cref="RcsDossierSource"/>.</summary>
    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;
}

/// <summary>
/// Response from <see cref="RcsBrandsResource.CreateAsync"/> and
/// <see cref="RcsBrandsResource.UpdateAsync"/>.
/// </summary>
public class RcsBrandResponse
{
    [JsonPropertyName("brand")]
    public RcsBrand Brand { get; set; } = new();
}

/// <summary>
/// Response from <see cref="RcsAgentsResource.CreateAsync"/> and
/// <see cref="RcsAgentsResource.UpdateAsync"/>.
/// </summary>
public class RcsAgentResponse
{
    [JsonPropertyName("agent")]
    public RcsAgentDetail Agent { get; set; } = new();
}

/// <summary>
/// Response from <see cref="RcsAgentsResource.GetAsync"/>.
/// </summary>
public class RcsAgentDetailResponse
{
    /// <summary>The agent.</summary>
    [JsonPropertyName("agent")]
    public RcsAgentDetail Agent { get; set; } = new();

    /// <summary>Devices invited to test the agent (same as <c>Agent.TestDevices</c>).</summary>
    [JsonPropertyName("devices")]
    public List<RcsTestDevice> Devices { get; set; } = new();

    /// <summary>Where the registration sits (same as <c>Agent.CustomerStage</c>); see <see cref="RcsCustomerStage"/>.</summary>
    [JsonPropertyName("stage")]
    public string Stage { get; set; } = string.Empty;
}

/// <summary>
/// Response from <see cref="RcsAgentsResource.SetTestDevicesAsync"/>.
/// </summary>
public class RcsTestDeviceListResponse
{
    /// <summary>The full device list after the change.</summary>
    [JsonPropertyName("devices")]
    public List<RcsTestDevice> Devices { get; set; } = new();
}

/// <summary>
/// Response from <see cref="RcsAgentsResource.SubmitAsync"/> and
/// <see cref="RcsAgentsResource.RequestLaunchAsync"/>.
/// </summary>
public class RcsAgentReviewResponse
{
    /// <summary>The agent, with its new review status.</summary>
    [JsonPropertyName("agent")]
    public RcsAgentDetail Agent { get; set; } = new();

    /// <summary>Where the registration sits now; see <see cref="RcsCustomerStage"/>.</summary>
    [JsonPropertyName("stage")]
    public string Stage { get; set; } = string.Empty;
}
