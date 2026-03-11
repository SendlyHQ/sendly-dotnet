using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sendly.Models;

public class EnterpriseAccount
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("maxWorkspaces")]
    public int MaxWorkspaces { get; set; }

    [JsonPropertyName("workspaceCount")]
    public int WorkspaceCount { get; set; }

    [JsonPropertyName("workspaces")]
    public List<EnterpriseWorkspaceSummary> Workspaces { get; set; } = new();

    [JsonPropertyName("metadata")]
    public Dictionary<string, object>? Metadata { get; set; }

    internal static EnterpriseAccount FromJson(JsonElement element, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<EnterpriseAccount>(element.GetRawText(), options)
            ?? new EnterpriseAccount();
    }
}

public class EnterpriseWorkspaceSummary
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("slug")]
    public string Slug { get; set; } = string.Empty;

    [JsonPropertyName("verificationStatus")]
    public string? VerificationStatus { get; set; }

    [JsonPropertyName("verificationType")]
    public string? VerificationType { get; set; }

    [JsonPropertyName("tollFreeNumber")]
    public string? TollFreeNumber { get; set; }

    [JsonPropertyName("creditBalance")]
    public int CreditBalance { get; set; }
}

public class EnterpriseWorkspace
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("slug")]
    public string Slug { get; set; } = string.Empty;

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("verification")]
    public EnterpriseWorkspaceVerification? Verification { get; set; }

    [JsonPropertyName("credits")]
    public int Credits { get; set; }

    [JsonPropertyName("keyCount")]
    public int KeyCount { get; set; }

    internal static EnterpriseWorkspace FromJson(JsonElement element, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<EnterpriseWorkspace>(element.GetRawText(), options)
            ?? new EnterpriseWorkspace();
    }
}

public class EnterpriseWorkspaceVerification
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("tollFreeNumber")]
    public string? TollFreeNumber { get; set; }

    [JsonPropertyName("businessName")]
    public string? BusinessName { get; set; }
}

public class CreateEnterpriseWorkspaceOptions
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

public class CreateEnterpriseWorkspaceResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("slug")]
    public string Slug { get; set; } = string.Empty;

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    internal static CreateEnterpriseWorkspaceResponse FromJson(JsonElement element, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<CreateEnterpriseWorkspaceResponse>(element.GetRawText(), options)
            ?? new CreateEnterpriseWorkspaceResponse();
    }
}

public class DeleteEnterpriseWorkspaceResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("deletedId")]
    public string DeletedId { get; set; } = string.Empty;

    internal static DeleteEnterpriseWorkspaceResponse FromJson(JsonElement element, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<DeleteEnterpriseWorkspaceResponse>(element.GetRawText(), options)
            ?? new DeleteEnterpriseWorkspaceResponse();
    }
}

public class SubmitVerificationOptions
{
    [JsonPropertyName("businessName")]
    public string BusinessName { get; set; } = string.Empty;

    [JsonPropertyName("doingBusinessAs")]
    public string? DoingBusinessAs { get; set; }

    [JsonPropertyName("website")]
    public string Website { get; set; } = string.Empty;

    [JsonPropertyName("address")]
    public VerificationAddress Address { get; set; } = new();

    [JsonPropertyName("contact")]
    public VerificationContact Contact { get; set; } = new();

    [JsonPropertyName("brn")]
    public string? Brn { get; set; }

    [JsonPropertyName("brnType")]
    public string? BrnType { get; set; }

    [JsonPropertyName("brnCountry")]
    public string? BrnCountry { get; set; }

    [JsonPropertyName("useCase")]
    public string UseCase { get; set; } = string.Empty;

    [JsonPropertyName("useCaseSummary")]
    public string UseCaseSummary { get; set; } = string.Empty;

    [JsonPropertyName("sampleMessages")]
    public string SampleMessages { get; set; } = string.Empty;

    [JsonPropertyName("optInWorkflow")]
    public string OptInWorkflow { get; set; } = string.Empty;

    [JsonPropertyName("optInImageUrls")]
    public string? OptInImageUrls { get; set; }

    [JsonPropertyName("monthlyVolume")]
    public string? MonthlyVolume { get; set; }

    [JsonPropertyName("additionalInformation")]
    public string? AdditionalInformation { get; set; }

    [JsonPropertyName("ageGatedContent")]
    public bool? AgeGatedContent { get; set; }

    [JsonPropertyName("isvReseller")]
    public bool? IsvReseller { get; set; }

    [JsonPropertyName("privacyUrl")]
    public string? PrivacyUrl { get; set; }

    [JsonPropertyName("termsUrl")]
    public string? TermsUrl { get; set; }

    [JsonPropertyName("entityType")]
    public string? EntityType { get; set; }
}

public class VerificationAddress
{
    [JsonPropertyName("street")]
    public string Street { get; set; } = string.Empty;

    [JsonPropertyName("address2")]
    public string? Address2 { get; set; }

    [JsonPropertyName("city")]
    public string City { get; set; } = string.Empty;

    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;

    [JsonPropertyName("zip")]
    public string Zip { get; set; } = string.Empty;

    [JsonPropertyName("country")]
    public string? Country { get; set; }
}

public class VerificationContact
{
    [JsonPropertyName("firstName")]
    public string FirstName { get; set; } = string.Empty;

    [JsonPropertyName("lastName")]
    public string LastName { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("phone")]
    public string Phone { get; set; } = string.Empty;
}

public class SubmitVerificationResponse
{
    [JsonPropertyName("verificationId")]
    public string VerificationId { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("tollFreeNumber")]
    public string? TollFreeNumber { get; set; }

    [JsonPropertyName("businessName")]
    public string? BusinessName { get; set; }

    [JsonPropertyName("telnyxProfileId")]
    public string? TelnyxProfileId { get; set; }

    internal static SubmitVerificationResponse FromJson(JsonElement element, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<SubmitVerificationResponse>(element.GetRawText(), options)
            ?? new SubmitVerificationResponse();
    }
}

public class InheritVerificationOptions
{
    [JsonPropertyName("sourceWorkspaceId")]
    public string SourceWorkspaceId { get; set; } = string.Empty;
}

public class InheritVerificationResponse
{
    [JsonPropertyName("verificationId")]
    public string VerificationId { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("tollFreeNumber")]
    public string? TollFreeNumber { get; set; }

    [JsonPropertyName("inheritedFrom")]
    public string InheritedFrom { get; set; } = string.Empty;

    internal static InheritVerificationResponse FromJson(JsonElement element, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<InheritVerificationResponse>(element.GetRawText(), options)
            ?? new InheritVerificationResponse();
    }
}

public class WorkspaceVerificationStatus
{
    [JsonPropertyName("verificationId")]
    public string? VerificationId { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "none";

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("tollFreeNumber")]
    public string? TollFreeNumber { get; set; }

    [JsonPropertyName("businessName")]
    public string? BusinessName { get; set; }

    [JsonPropertyName("submittedAt")]
    public DateTime? SubmittedAt { get; set; }

    internal static WorkspaceVerificationStatus FromJson(JsonElement element, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<WorkspaceVerificationStatus>(element.GetRawText(), options)
            ?? new WorkspaceVerificationStatus();
    }
}

public class EnterpriseTransferCreditsOptions
{
    [JsonPropertyName("sourceWorkspaceId")]
    public string SourceWorkspaceId { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public int Amount { get; set; }
}

public class EnterpriseTransferCreditsResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("amount")]
    public int Amount { get; set; }

    [JsonPropertyName("sourceBalance")]
    public int SourceBalance { get; set; }

    [JsonPropertyName("targetBalance")]
    public int TargetBalance { get; set; }

    internal static EnterpriseTransferCreditsResponse FromJson(JsonElement element, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<EnterpriseTransferCreditsResponse>(element.GetRawText(), options)
            ?? new EnterpriseTransferCreditsResponse();
    }
}

public class WorkspaceCredits
{
    [JsonPropertyName("balance")]
    public int Balance { get; set; }

    [JsonPropertyName("lifetimeCredits")]
    public int LifetimeCredits { get; set; }

    internal static WorkspaceCredits FromJson(JsonElement element, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<WorkspaceCredits>(element.GetRawText(), options)
            ?? new WorkspaceCredits();
    }
}

public class CreateWorkspaceKeyOptions
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("scopes")]
    public List<string>? Scopes { get; set; }
}

public class WorkspaceKey
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("keyPrefix")]
    public string KeyPrefix { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("scopes")]
    public List<string> Scopes { get; set; } = new();

    [JsonPropertyName("lastUsedAt")]
    public DateTime? LastUsedAt { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    internal static WorkspaceKey FromJson(JsonElement element, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<WorkspaceKey>(element.GetRawText(), options)
            ?? new WorkspaceKey();
    }
}

public class CreateWorkspaceKeyResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("keyPrefix")]
    public string KeyPrefix { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("scopes")]
    public List<string> Scopes { get; set; } = new();

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    internal static CreateWorkspaceKeyResponse FromJson(JsonElement element, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<CreateWorkspaceKeyResponse>(element.GetRawText(), options)
            ?? new CreateWorkspaceKeyResponse();
    }
}

public class RevokeWorkspaceKeyResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("revokedId")]
    public string RevokedId { get; set; } = string.Empty;

    internal static RevokeWorkspaceKeyResponse FromJson(JsonElement element, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<RevokeWorkspaceKeyResponse>(element.GetRawText(), options)
            ?? new RevokeWorkspaceKeyResponse();
    }
}

public class ProvisionWorkspaceOptions
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("sourceWorkspaceId")]
    public string? SourceWorkspaceId { get; set; }

    [JsonPropertyName("inheritWithNewNumber")]
    public bool? InheritWithNewNumber { get; set; }

    [JsonPropertyName("verification")]
    public SubmitVerificationOptions? Verification { get; set; }

    [JsonPropertyName("creditAmount")]
    public int? CreditAmount { get; set; }

    [JsonPropertyName("creditSourceWorkspaceId")]
    public string? CreditSourceWorkspaceId { get; set; }

    [JsonPropertyName("keyName")]
    public string? KeyName { get; set; }

    [JsonPropertyName("keyType")]
    public string? KeyType { get; set; }

    [JsonPropertyName("webhookUrl")]
    public string? WebhookUrl { get; set; }

    [JsonPropertyName("generateOptInPage")]
    public bool? GenerateOptInPage { get; set; }
}

public class ProvisionWorkspaceResponse
{
    [JsonPropertyName("workspace")]
    public ProvisionedWorkspace Workspace { get; set; } = new();

    [JsonPropertyName("verification")]
    public ProvisionedVerification? Verification { get; set; }

    [JsonPropertyName("credits")]
    public ProvisionedCredits? Credits { get; set; }

    [JsonPropertyName("key")]
    public ProvisionedKey? Key { get; set; }

    [JsonPropertyName("webhook")]
    public ProvisionedWebhook? Webhook { get; set; }

    internal static ProvisionWorkspaceResponse FromJson(JsonElement element, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<ProvisionWorkspaceResponse>(element.GetRawText(), options)
            ?? new ProvisionWorkspaceResponse();
    }
}

public class ProvisionedWorkspace
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("slug")]
    public string Slug { get; set; } = string.Empty;
}

public class ProvisionedVerification
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("tollFreeNumber")]
    public string? TollFreeNumber { get; set; }

    [JsonPropertyName("inherited")]
    public bool Inherited { get; set; }
}

public class ProvisionedCredits
{
    [JsonPropertyName("transferred")]
    public int? Transferred { get; set; }

    [JsonPropertyName("balance")]
    public int? Balance { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

public class ProvisionedKey
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("keyPrefix")]
    public string KeyPrefix { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
}

public class ProvisionedWebhook
{
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

public class EnterpriseWebhookConfig
{
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    internal static EnterpriseWebhookConfig FromJson(JsonElement element, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<EnterpriseWebhookConfig>(element.GetRawText(), options)
            ?? new EnterpriseWebhookConfig();
    }
}

public class SetEnterpriseWebhookOptions
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
}

public class EnterpriseWebhookTestResult
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("statusCode")]
    public int? StatusCode { get; set; }

    [JsonPropertyName("statusText")]
    public string? StatusText { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    internal static EnterpriseWebhookTestResult FromJson(JsonElement element, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<EnterpriseWebhookTestResult>(element.GetRawText(), options)
            ?? new EnterpriseWebhookTestResult();
    }
}

public class EnterpriseWebhookSecretRotation
{
    [JsonPropertyName("secret")]
    public string Secret { get; set; } = string.Empty;

    [JsonPropertyName("rotatedAt")]
    public string? RotatedAt { get; set; }

    internal static EnterpriseWebhookSecretRotation FromJson(JsonElement element, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<EnterpriseWebhookSecretRotation>(element.GetRawText(), options)
            ?? new EnterpriseWebhookSecretRotation();
    }
}

public class EnterpriseAnalyticsOverview
{
    [JsonPropertyName("totalMessages")]
    public int TotalMessages { get; set; }

    [JsonPropertyName("deliveredMessages")]
    public int DeliveredMessages { get; set; }

    [JsonPropertyName("failedMessages")]
    public int FailedMessages { get; set; }

    [JsonPropertyName("deliveryRate")]
    public int DeliveryRate { get; set; }

    [JsonPropertyName("totalCreditsUsed")]
    public int TotalCreditsUsed { get; set; }

    [JsonPropertyName("activeWorkspaces")]
    public int ActiveWorkspaces { get; set; }

    internal static EnterpriseAnalyticsOverview FromJson(JsonElement element, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<EnterpriseAnalyticsOverview>(element.GetRawText(), options)
            ?? new EnterpriseAnalyticsOverview();
    }
}

public class EnterpriseMessagesAnalytics
{
    [JsonPropertyName("period")]
    public string Period { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public List<EnterpriseMessagesDailyData> Data { get; set; } = new();

    internal static EnterpriseMessagesAnalytics FromJson(JsonElement element, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<EnterpriseMessagesAnalytics>(element.GetRawText(), options)
            ?? new EnterpriseMessagesAnalytics();
    }
}

public class EnterpriseMessagesDailyData
{
    [JsonPropertyName("date")]
    public string Date { get; set; } = string.Empty;

    [JsonPropertyName("sent")]
    public int Sent { get; set; }

    [JsonPropertyName("delivered")]
    public int Delivered { get; set; }

    [JsonPropertyName("failed")]
    public int Failed { get; set; }
}

public class EnterpriseDeliveryAnalytics
{
    [JsonPropertyName("workspaceId")]
    public string WorkspaceId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("sent")]
    public int Sent { get; set; }

    [JsonPropertyName("delivered")]
    public int Delivered { get; set; }

    [JsonPropertyName("failed")]
    public int Failed { get; set; }

    [JsonPropertyName("rate")]
    public int Rate { get; set; }

    internal static EnterpriseDeliveryAnalytics FromJson(JsonElement element, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<EnterpriseDeliveryAnalytics>(element.GetRawText(), options)
            ?? new EnterpriseDeliveryAnalytics();
    }
}

public class EnterpriseCreditsAnalytics
{
    [JsonPropertyName("period")]
    public string Period { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public List<EnterpriseCreditsDailyData> Data { get; set; } = new();

    internal static EnterpriseCreditsAnalytics FromJson(JsonElement element, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<EnterpriseCreditsAnalytics>(element.GetRawText(), options)
            ?? new EnterpriseCreditsAnalytics();
    }
}

public class EnterpriseCreditsDailyData
{
    [JsonPropertyName("date")]
    public string Date { get; set; } = string.Empty;

    [JsonPropertyName("used")]
    public int Used { get; set; }

    [JsonPropertyName("transferred")]
    public int Transferred { get; set; }

    [JsonPropertyName("purchased")]
    public int Purchased { get; set; }
}

public class EnterpriseAnalyticsOptions
{
    public string? Period { get; set; }
    public string? WorkspaceId { get; set; }

    internal Dictionary<string, string> ToQueryParams()
    {
        var result = new Dictionary<string, string>();

        if (!string.IsNullOrEmpty(Period))
            result["period"] = Period;
        if (!string.IsNullOrEmpty(WorkspaceId))
            result["workspaceId"] = WorkspaceId;

        return result;
    }
}

public class OptInPage
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("slug")]
    public string Slug { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("businessName")]
    public string BusinessName { get; set; } = string.Empty;

    [JsonPropertyName("useCase")]
    public string? UseCase { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }

    [JsonPropertyName("viewCount")]
    public int ViewCount { get; set; }

    [JsonPropertyName("logoUrl")]
    public string? LogoUrl { get; set; }

    [JsonPropertyName("headerColor")]
    public string? HeaderColor { get; set; }

    [JsonPropertyName("buttonColor")]
    public string? ButtonColor { get; set; }

    [JsonPropertyName("customHeadline")]
    public string? CustomHeadline { get; set; }

    [JsonPropertyName("createdAt")]
    public string CreatedAt { get; set; } = string.Empty;

    internal static OptInPage FromJson(JsonElement element, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<OptInPage>(element.GetRawText(), options)
            ?? new OptInPage();
    }
}

public class CreateOptInPageOptions
{
    [JsonPropertyName("businessName")]
    public string BusinessName { get; set; } = string.Empty;

    [JsonPropertyName("useCase")]
    public string? UseCase { get; set; }

    [JsonPropertyName("useCaseSummary")]
    public string? UseCaseSummary { get; set; }

    [JsonPropertyName("sampleMessages")]
    public string? SampleMessages { get; set; }
}

public class CreateOptInPageResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("slug")]
    public string Slug { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("businessName")]
    public string BusinessName { get; set; } = string.Empty;

    internal static CreateOptInPageResponse FromJson(JsonElement element, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<CreateOptInPageResponse>(element.GetRawText(), options)
            ?? new CreateOptInPageResponse();
    }
}

public class UpdateOptInPageOptions
{
    [JsonPropertyName("logoUrl")]
    public string? LogoUrl { get; set; }

    [JsonPropertyName("headerColor")]
    public string? HeaderColor { get; set; }

    [JsonPropertyName("buttonColor")]
    public string? ButtonColor { get; set; }

    [JsonPropertyName("customHeadline")]
    public string? CustomHeadline { get; set; }

    [JsonPropertyName("customBenefits")]
    public List<string>? CustomBenefits { get; set; }
}

public class DeleteOptInPageResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    internal static DeleteOptInPageResponse FromJson(JsonElement element, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<DeleteOptInPageResponse>(element.GetRawText(), options)
            ?? new DeleteOptInPageResponse();
    }
}

public class WorkspaceWebhookConfig
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("events")]
    public List<string> Events { get; set; } = new();

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }

    [JsonPropertyName("createdAt")]
    public string CreatedAt { get; set; } = string.Empty;

    internal static WorkspaceWebhookConfig FromJson(JsonElement element, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<WorkspaceWebhookConfig>(element.GetRawText(), options)
            ?? new WorkspaceWebhookConfig();
    }
}

public class SetWorkspaceWebhookOptions
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("events")]
    public List<string>? Events { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

public class SetWorkspaceWebhookResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("events")]
    public List<string> Events { get; set; } = new();

    [JsonPropertyName("secret")]
    public string? Secret { get; set; }

    [JsonPropertyName("created")]
    public bool? Created { get; set; }

    [JsonPropertyName("updated")]
    public bool? Updated { get; set; }

    internal static SetWorkspaceWebhookResponse FromJson(JsonElement element, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<SetWorkspaceWebhookResponse>(element.GetRawText(), options)
            ?? new SetWorkspaceWebhookResponse();
    }
}

public class WorkspaceWebhookTestResult
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("statusCode")]
    public int? StatusCode { get; set; }

    [JsonPropertyName("statusText")]
    public string? StatusText { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    internal static WorkspaceWebhookTestResult FromJson(JsonElement element, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<WorkspaceWebhookTestResult>(element.GetRawText(), options)
            ?? new WorkspaceWebhookTestResult();
    }
}

public class SuspendWorkspaceOptions
{
    [JsonPropertyName("reason")]
    public string? Reason { get; set; }
}

public class SuspendWorkspaceResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("suspendedAt")]
    public string SuspendedAt { get; set; } = string.Empty;

    internal static SuspendWorkspaceResponse FromJson(JsonElement element, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<SuspendWorkspaceResponse>(element.GetRawText(), options)
            ?? new SuspendWorkspaceResponse();
    }
}

public class ResumeWorkspaceResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    internal static ResumeWorkspaceResponse FromJson(JsonElement element, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<ResumeWorkspaceResponse>(element.GetRawText(), options)
            ?? new ResumeWorkspaceResponse();
    }
}

public class BulkProvisionWorkspace
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("sourceWorkspaceId")]
    public string? SourceWorkspaceId { get; set; }

    [JsonPropertyName("creditAmount")]
    public int? CreditAmount { get; set; }

    [JsonPropertyName("creditSourceWorkspaceId")]
    public string? CreditSourceWorkspaceId { get; set; }
}

public class BulkProvisionOptions
{
    [JsonPropertyName("workspaces")]
    public List<BulkProvisionWorkspace> Workspaces { get; set; } = new();
}

public class BulkProvisionResultItem
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("workspaceId")]
    public string? WorkspaceId { get; set; }

    [JsonPropertyName("slug")]
    public string? Slug { get; set; }

    [JsonPropertyName("warning")]
    public string? Warning { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

public class BulkProvisionSummary
{
    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("succeeded")]
    public int Succeeded { get; set; }

    [JsonPropertyName("failed")]
    public int Failed { get; set; }
}

public class BulkProvisionResult
{
    [JsonPropertyName("results")]
    public List<BulkProvisionResultItem> Results { get; set; } = new();

    [JsonPropertyName("summary")]
    public BulkProvisionSummary Summary { get; set; } = new();

    internal static BulkProvisionResult FromJson(JsonElement element, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<BulkProvisionResult>(element.GetRawText(), options)
            ?? new BulkProvisionResult();
    }
}

public class SetCustomDomainOptions
{
    [JsonPropertyName("domain")]
    public string Domain { get; set; } = string.Empty;
}

public class DnsRecord
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
}

public class DnsInstructions
{
    [JsonPropertyName("cname")]
    public DnsRecord Cname { get; set; } = new();

    [JsonPropertyName("txt")]
    public DnsRecord Txt { get; set; } = new();
}

public class SetCustomDomainResponse
{
    [JsonPropertyName("domain")]
    public string Domain { get; set; } = string.Empty;

    [JsonPropertyName("verified")]
    public bool Verified { get; set; }

    [JsonPropertyName("dnsInstructions")]
    public DnsInstructions DnsInstructions { get; set; } = new();

    internal static SetCustomDomainResponse FromJson(JsonElement element, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<SetCustomDomainResponse>(element.GetRawText(), options)
            ?? new SetCustomDomainResponse();
    }
}

public class SendInvitationOptions
{
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;
}

public class Invitation
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("expiresAt")]
    public string ExpiresAt { get; set; } = string.Empty;

    internal static Invitation FromJson(JsonElement element, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<Invitation>(element.GetRawText(), options)
            ?? new Invitation();
    }
}

public class CancelInvitationResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    internal static CancelInvitationResponse FromJson(JsonElement element, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<CancelInvitationResponse>(element.GetRawText(), options)
            ?? new CancelInvitationResponse();
    }
}

public class QuotaSettings
{
    [JsonPropertyName("monthlyMessageQuota")]
    public int? MonthlyMessageQuota { get; set; }

    [JsonPropertyName("messagesThisMonth")]
    public int MessagesThisMonth { get; set; }

    [JsonPropertyName("quotaResetAt")]
    public string? QuotaResetAt { get; set; }

    internal static QuotaSettings FromJson(JsonElement element, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<QuotaSettings>(element.GetRawText(), options)
            ?? new QuotaSettings();
    }
}

public class UpdateQuotaOptions
{
    [JsonPropertyName("monthlyMessageQuota")]
    public int? MonthlyMessageQuota { get; set; }
}

public class PoolCredits
{
    [JsonPropertyName("balance")]
    public int Balance { get; set; }

    [JsonPropertyName("lifetimeCredits")]
    public int LifetimeCredits { get; set; }

    [JsonPropertyName("reservedBalance")]
    public int ReservedBalance { get; set; }

    internal static PoolCredits FromJson(JsonElement element, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<PoolCredits>(element.GetRawText(), options)
            ?? new PoolCredits();
    }
}

public class DepositCreditsOptions
{
    [JsonPropertyName("amount")]
    public int Amount { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

public class AutoTopUpSettings
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    [JsonPropertyName("threshold")]
    public int Threshold { get; set; }

    [JsonPropertyName("amount")]
    public int Amount { get; set; }

    [JsonPropertyName("sourceWorkspaceId")]
    public string? SourceWorkspaceId { get; set; }

    internal static AutoTopUpSettings FromJson(JsonElement element, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<AutoTopUpSettings>(element.GetRawText(), options)
            ?? new AutoTopUpSettings();
    }
}

public class UpdateAutoTopUpOptions
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    [JsonPropertyName("threshold")]
    public int Threshold { get; set; }

    [JsonPropertyName("amount")]
    public int Amount { get; set; }

    [JsonPropertyName("sourceWorkspaceId")]
    public string? SourceWorkspaceId { get; set; }
}

public class WorkspaceBillingItem
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("creditsUsed")]
    public int CreditsUsed { get; set; }

    [JsonPropertyName("creditsPurchased")]
    public int CreditsPurchased { get; set; }

    [JsonPropertyName("creditsTransferredIn")]
    public int CreditsTransferredIn { get; set; }

    [JsonPropertyName("creditsTransferredOut")]
    public int CreditsTransferredOut { get; set; }

    [JsonPropertyName("messagesSent")]
    public int MessagesSent { get; set; }

    [JsonPropertyName("messagesDelivered")]
    public int MessagesDelivered { get; set; }

    [JsonPropertyName("workspaceFee")]
    public decimal WorkspaceFee { get; set; }

    [JsonPropertyName("allocatedPlatformFee")]
    public decimal AllocatedPlatformFee { get; set; }

    [JsonPropertyName("totalCost")]
    public decimal TotalCost { get; set; }
}

public class BillingBreakdownSummary
{
    [JsonPropertyName("platformFee")]
    public decimal PlatformFee { get; set; }

    [JsonPropertyName("totalWorkspaceFees")]
    public decimal TotalWorkspaceFees { get; set; }

    [JsonPropertyName("totalCreditsUsed")]
    public int TotalCreditsUsed { get; set; }

    [JsonPropertyName("totalCost")]
    public decimal TotalCost { get; set; }
}

public class BillingBreakdown
{
    [JsonPropertyName("period")]
    public string Period { get; set; } = string.Empty;

    [JsonPropertyName("summary")]
    public BillingBreakdownSummary Summary { get; set; } = new();

    [JsonPropertyName("workspaces")]
    public List<WorkspaceBillingItem> Workspaces { get; set; } = new();

    internal static BillingBreakdown FromJson(JsonElement element, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<BillingBreakdown>(element.GetRawText(), options)
            ?? new BillingBreakdown();
    }
}

public class BillingBreakdownOptions
{
    public string? Period { get; set; }
    public int? Page { get; set; }
    public int? Limit { get; set; }

    internal Dictionary<string, string> ToQueryParams()
    {
        var result = new Dictionary<string, string>();

        if (!string.IsNullOrEmpty(Period))
            result["period"] = Period;
        if (Page.HasValue)
            result["page"] = Page.Value.ToString();
        if (Limit.HasValue)
            result["limit"] = Limit.Value.ToString();

        return result;
    }
}
