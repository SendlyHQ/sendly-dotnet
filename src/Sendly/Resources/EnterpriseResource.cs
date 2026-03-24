using System.Text.Json;
using Sendly.Exceptions;
using Sendly.Models;

namespace Sendly.Resources;

public class EnterpriseWorkspacesResource
{
    private readonly SendlyClient _client;

    internal EnterpriseWorkspacesResource(SendlyClient client)
    {
        _client = client;
    }

    public async Task<CreateEnterpriseWorkspaceResponse> CreateAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        return await CreateAsync(new CreateEnterpriseWorkspaceOptions { Name = name }, cancellationToken);
    }

    public async Task<CreateEnterpriseWorkspaceResponse> CreateAsync(
        CreateEnterpriseWorkspaceOptions options,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(options.Name))
            throw new ValidationException("Workspace name is required");

        using var response = await _client.PostAsync("/enterprise/workspaces", options, cancellationToken);
        return CreateEnterpriseWorkspaceResponse.FromJson(response.RootElement, _client.JsonOptions);
    }

    public async Task<EnterpriseWorkspace> GetAsync(
        string workspaceId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(workspaceId))
            throw new ValidationException("Workspace ID is required");

        using var response = await _client.GetAsync(
            $"/enterprise/workspaces/{Uri.EscapeDataString(workspaceId)}", null, cancellationToken);
        return EnterpriseWorkspace.FromJson(response.RootElement, _client.JsonOptions);
    }

    public async Task<List<EnterpriseWorkspace>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await _client.GetAsync("/enterprise/account", null, cancellationToken);
        var root = response.RootElement;
        var workspaces = new List<EnterpriseWorkspace>();

        if (root.TryGetProperty("workspaces", out var workspacesElement) &&
            workspacesElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var element in workspacesElement.EnumerateArray())
            {
                workspaces.Add(EnterpriseWorkspace.FromJson(element, _client.JsonOptions));
            }
        }

        return workspaces;
    }

    public async Task<DeleteEnterpriseWorkspaceResponse> DeleteAsync(
        string workspaceId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(workspaceId))
            throw new ValidationException("Workspace ID is required");

        using var response = await _client.DeleteAsync(
            $"/enterprise/workspaces/{Uri.EscapeDataString(workspaceId)}", cancellationToken);
        return DeleteEnterpriseWorkspaceResponse.FromJson(response.RootElement, _client.JsonOptions);
    }

    public async Task<SubmitVerificationResponse> SubmitVerificationAsync(
        string workspaceId,
        SubmitVerificationOptions options,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(workspaceId))
            throw new ValidationException("Workspace ID is required");

        using var response = await _client.PostAsync(
            $"/enterprise/workspaces/{Uri.EscapeDataString(workspaceId)}/verification/submit",
            options, cancellationToken);
        return SubmitVerificationResponse.FromJson(response.RootElement, _client.JsonOptions);
    }

    public async Task<InheritVerificationResponse> InheritVerificationAsync(
        string workspaceId,
        string sourceWorkspaceId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(workspaceId))
            throw new ValidationException("Workspace ID is required");
        if (string.IsNullOrEmpty(sourceWorkspaceId))
            throw new ValidationException("Source workspace ID is required");

        var options = new InheritVerificationOptions { SourceWorkspaceId = sourceWorkspaceId };
        using var response = await _client.PostAsync(
            $"/enterprise/workspaces/{Uri.EscapeDataString(workspaceId)}/verification/inherit",
            options, cancellationToken);
        return InheritVerificationResponse.FromJson(response.RootElement, _client.JsonOptions);
    }

    public async Task<WorkspaceVerificationStatus> GetVerificationAsync(
        string workspaceId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(workspaceId))
            throw new ValidationException("Workspace ID is required");

        using var response = await _client.GetAsync(
            $"/enterprise/workspaces/{Uri.EscapeDataString(workspaceId)}/verification",
            null, cancellationToken);
        return WorkspaceVerificationStatus.FromJson(response.RootElement, _client.JsonOptions);
    }

    public async Task<EnterpriseTransferCreditsResponse> TransferCreditsAsync(
        string workspaceId,
        string sourceWorkspaceId,
        int amount,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(workspaceId))
            throw new ValidationException("Workspace ID is required");
        if (string.IsNullOrEmpty(sourceWorkspaceId))
            throw new ValidationException("Source workspace ID is required");
        if (amount <= 0)
            throw new ValidationException("Amount must be a positive integer");

        var options = new EnterpriseTransferCreditsOptions
        {
            SourceWorkspaceId = sourceWorkspaceId,
            Amount = amount
        };

        using var response = await _client.PostAsync(
            $"/enterprise/workspaces/{Uri.EscapeDataString(workspaceId)}/transfer-credits",
            options, cancellationToken);
        return EnterpriseTransferCreditsResponse.FromJson(response.RootElement, _client.JsonOptions);
    }

    public async Task<WorkspaceCredits> GetCreditsAsync(
        string workspaceId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(workspaceId))
            throw new ValidationException("Workspace ID is required");

        using var response = await _client.GetAsync(
            $"/enterprise/workspaces/{Uri.EscapeDataString(workspaceId)}/credits",
            null, cancellationToken);
        return WorkspaceCredits.FromJson(response.RootElement, _client.JsonOptions);
    }

    public async Task<CreateWorkspaceKeyResponse> CreateKeyAsync(
        string workspaceId,
        string name,
        CancellationToken cancellationToken = default)
    {
        return await CreateKeyAsync(workspaceId, new CreateWorkspaceKeyOptions { Name = name }, cancellationToken);
    }

    public async Task<CreateWorkspaceKeyResponse> CreateKeyAsync(
        string workspaceId,
        CreateWorkspaceKeyOptions options,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(workspaceId))
            throw new ValidationException("Workspace ID is required");
        if (string.IsNullOrEmpty(options.Name))
            throw new ValidationException("Key name is required");

        using var response = await _client.PostAsync(
            $"/enterprise/workspaces/{Uri.EscapeDataString(workspaceId)}/keys",
            options, cancellationToken);
        return CreateWorkspaceKeyResponse.FromJson(response.RootElement, _client.JsonOptions);
    }

    public async Task<List<WorkspaceKey>> ListKeysAsync(
        string workspaceId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(workspaceId))
            throw new ValidationException("Workspace ID is required");

        using var response = await _client.GetAsync(
            $"/enterprise/workspaces/{Uri.EscapeDataString(workspaceId)}/keys",
            null, cancellationToken);
        var root = response.RootElement;
        var keys = new List<WorkspaceKey>();

        if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var element in root.EnumerateArray())
            {
                keys.Add(WorkspaceKey.FromJson(element, _client.JsonOptions));
            }
        }
        else if (root.TryGetProperty("data", out var dataElement) &&
                 dataElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var element in dataElement.EnumerateArray())
            {
                keys.Add(WorkspaceKey.FromJson(element, _client.JsonOptions));
            }
        }

        return keys;
    }

    public async Task<RevokeWorkspaceKeyResponse> RevokeKeyAsync(
        string workspaceId,
        string keyId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(workspaceId))
            throw new ValidationException("Workspace ID is required");
        if (string.IsNullOrEmpty(keyId))
            throw new ValidationException("Key ID is required");

        using var response = await _client.DeleteAsync(
            $"/enterprise/workspaces/{Uri.EscapeDataString(workspaceId)}/keys/{Uri.EscapeDataString(keyId)}",
            cancellationToken);
        return RevokeWorkspaceKeyResponse.FromJson(response.RootElement, _client.JsonOptions);
    }

    public async Task<List<OptInPage>> ListOptInPagesAsync(
        string workspaceId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(workspaceId))
            throw new ValidationException("Workspace ID is required");

        using var response = await _client.GetAsync(
            $"/enterprise/workspaces/{Uri.EscapeDataString(workspaceId)}/opt-in-pages",
            null, cancellationToken);
        var root = response.RootElement;
        var pages = new List<OptInPage>();

        if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var element in root.EnumerateArray())
            {
                pages.Add(OptInPage.FromJson(element, _client.JsonOptions));
            }
        }
        else if (root.TryGetProperty("data", out var dataElement) &&
                 dataElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var element in dataElement.EnumerateArray())
            {
                pages.Add(OptInPage.FromJson(element, _client.JsonOptions));
            }
        }

        return pages;
    }

    public async Task<CreateOptInPageResponse> CreateOptInPageAsync(
        string workspaceId,
        CreateOptInPageOptions options,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(workspaceId))
            throw new ValidationException("Workspace ID is required");
        if (string.IsNullOrEmpty(options.BusinessName))
            throw new ValidationException("Business name is required");

        using var response = await _client.PostAsync(
            $"/enterprise/workspaces/{Uri.EscapeDataString(workspaceId)}/opt-in-pages",
            options, cancellationToken);
        return CreateOptInPageResponse.FromJson(response.RootElement, _client.JsonOptions);
    }

    public async Task<OptInPage> UpdateOptInPageAsync(
        string workspaceId,
        string pageId,
        UpdateOptInPageOptions options,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(workspaceId))
            throw new ValidationException("Workspace ID is required");
        if (string.IsNullOrEmpty(pageId))
            throw new ValidationException("Page ID is required");

        using var response = await _client.PatchAsync(
            $"/enterprise/workspaces/{Uri.EscapeDataString(workspaceId)}/opt-in-pages/{Uri.EscapeDataString(pageId)}",
            options, cancellationToken);
        return OptInPage.FromJson(response.RootElement, _client.JsonOptions);
    }

    public async Task<DeleteOptInPageResponse> DeleteOptInPageAsync(
        string workspaceId,
        string pageId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(workspaceId))
            throw new ValidationException("Workspace ID is required");
        if (string.IsNullOrEmpty(pageId))
            throw new ValidationException("Page ID is required");

        using var response = await _client.DeleteAsync(
            $"/enterprise/workspaces/{Uri.EscapeDataString(workspaceId)}/opt-in-pages/{Uri.EscapeDataString(pageId)}",
            cancellationToken);
        return DeleteOptInPageResponse.FromJson(response.RootElement, _client.JsonOptions);
    }

    public async Task<SetWorkspaceWebhookResponse> SetWebhookAsync(
        string workspaceId,
        SetWorkspaceWebhookOptions options,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(workspaceId))
            throw new ValidationException("Workspace ID is required");
        if (string.IsNullOrEmpty(options.Url))
            throw new ValidationException("Webhook URL is required");

        using var response = await _client.PutAsync(
            $"/enterprise/workspaces/{Uri.EscapeDataString(workspaceId)}/webhooks",
            options, cancellationToken);
        return SetWorkspaceWebhookResponse.FromJson(response.RootElement, _client.JsonOptions);
    }

    public async Task<List<WorkspaceWebhookConfig>> ListWebhooksAsync(
        string workspaceId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(workspaceId))
            throw new ValidationException("Workspace ID is required");

        using var response = await _client.GetAsync(
            $"/enterprise/workspaces/{Uri.EscapeDataString(workspaceId)}/webhooks",
            null, cancellationToken);
        var root = response.RootElement;
        var webhooks = new List<WorkspaceWebhookConfig>();

        if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var element in root.EnumerateArray())
            {
                webhooks.Add(WorkspaceWebhookConfig.FromJson(element, _client.JsonOptions));
            }
        }
        else if (root.TryGetProperty("data", out var dataElement) &&
                 dataElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var element in dataElement.EnumerateArray())
            {
                webhooks.Add(WorkspaceWebhookConfig.FromJson(element, _client.JsonOptions));
            }
        }

        return webhooks;
    }

    public async Task DeleteWebhooksAsync(
        string workspaceId,
        string? webhookId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(workspaceId))
            throw new ValidationException("Workspace ID is required");

        var path = $"/enterprise/workspaces/{Uri.EscapeDataString(workspaceId)}/webhooks";
        if (!string.IsNullOrEmpty(webhookId))
            path += $"?webhookId={Uri.EscapeDataString(webhookId)}";

        using var _ = await _client.DeleteAsync(path, cancellationToken);
    }

    public async Task<WorkspaceWebhookTestResult> TestWebhookAsync(
        string workspaceId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(workspaceId))
            throw new ValidationException("Workspace ID is required");

        using var response = await _client.PostAsync<object>(
            $"/enterprise/workspaces/{Uri.EscapeDataString(workspaceId)}/webhooks/test",
            new { }, cancellationToken);
        return WorkspaceWebhookTestResult.FromJson(response.RootElement, _client.JsonOptions);
    }

    public async Task<SuspendWorkspaceResponse> SuspendAsync(
        string workspaceId,
        SuspendWorkspaceOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(workspaceId))
            throw new ValidationException("Workspace ID is required");

        using var response = await _client.PostAsync(
            $"/enterprise/workspaces/{Uri.EscapeDataString(workspaceId)}/suspend",
            options ?? new SuspendWorkspaceOptions(), cancellationToken);
        return SuspendWorkspaceResponse.FromJson(response.RootElement, _client.JsonOptions);
    }

    public async Task<ResumeWorkspaceResponse> ResumeAsync(
        string workspaceId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(workspaceId))
            throw new ValidationException("Workspace ID is required");

        using var response = await _client.PostAsync<object>(
            $"/enterprise/workspaces/{Uri.EscapeDataString(workspaceId)}/resume",
            new { }, cancellationToken);
        return ResumeWorkspaceResponse.FromJson(response.RootElement, _client.JsonOptions);
    }

    public async Task<BulkProvisionResult> ProvisionBulkAsync(
        List<BulkProvisionWorkspace> workspaces,
        CancellationToken cancellationToken = default)
    {
        if (workspaces == null || workspaces.Count == 0)
            throw new ValidationException("Workspaces list is required");
        if (workspaces.Count > 50)
            throw new ValidationException("Maximum 50 workspaces per bulk provision");

        var options = new BulkProvisionOptions { Workspaces = workspaces };
        using var response = await _client.PostAsync(
            "/enterprise/workspaces/provision/bulk", options, cancellationToken);
        return BulkProvisionResult.FromJson(response.RootElement, _client.JsonOptions);
    }

    public async Task<SetCustomDomainResponse> SetCustomDomainAsync(
        string workspaceId,
        string pageId,
        string domain,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(workspaceId))
            throw new ValidationException("Workspace ID is required");
        if (string.IsNullOrEmpty(pageId))
            throw new ValidationException("Page ID is required");
        if (string.IsNullOrEmpty(domain))
            throw new ValidationException("Domain is required");

        var options = new SetCustomDomainOptions { Domain = domain };
        using var response = await _client.PutAsync(
            $"/enterprise/workspaces/{Uri.EscapeDataString(workspaceId)}/pages/{Uri.EscapeDataString(pageId)}/domain",
            options, cancellationToken);
        return SetCustomDomainResponse.FromJson(response.RootElement, _client.JsonOptions);
    }

    public async Task<Invitation> SendInvitationAsync(
        string workspaceId,
        SendInvitationOptions options,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(workspaceId))
            throw new ValidationException("Workspace ID is required");
        if (string.IsNullOrEmpty(options.Email))
            throw new ValidationException("Email is required");
        if (string.IsNullOrEmpty(options.Role))
            throw new ValidationException("Role is required");

        using var response = await _client.PostAsync(
            $"/enterprise/workspaces/{Uri.EscapeDataString(workspaceId)}/invitations",
            options, cancellationToken);
        return Invitation.FromJson(response.RootElement, _client.JsonOptions);
    }

    public async Task<List<Invitation>> ListInvitationsAsync(
        string workspaceId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(workspaceId))
            throw new ValidationException("Workspace ID is required");

        using var response = await _client.GetAsync(
            $"/enterprise/workspaces/{Uri.EscapeDataString(workspaceId)}/invitations",
            null, cancellationToken);
        var root = response.RootElement;
        var invitations = new List<Invitation>();

        if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var element in root.EnumerateArray())
            {
                invitations.Add(Invitation.FromJson(element, _client.JsonOptions));
            }
        }
        else if (root.TryGetProperty("data", out var dataElement) &&
                 dataElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var element in dataElement.EnumerateArray())
            {
                invitations.Add(Invitation.FromJson(element, _client.JsonOptions));
            }
        }

        return invitations;
    }

    public async Task<CancelInvitationResponse> CancelInvitationAsync(
        string workspaceId,
        string inviteId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(workspaceId))
            throw new ValidationException("Workspace ID is required");
        if (string.IsNullOrEmpty(inviteId))
            throw new ValidationException("Invitation ID is required");

        using var response = await _client.DeleteAsync(
            $"/enterprise/workspaces/{Uri.EscapeDataString(workspaceId)}/invitations/{Uri.EscapeDataString(inviteId)}",
            cancellationToken);
        return CancelInvitationResponse.FromJson(response.RootElement, _client.JsonOptions);
    }

    public async Task<QuotaSettings> GetQuotaAsync(
        string workspaceId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(workspaceId))
            throw new ValidationException("Workspace ID is required");

        using var response = await _client.GetAsync(
            $"/enterprise/workspaces/{Uri.EscapeDataString(workspaceId)}/quota",
            null, cancellationToken);
        return QuotaSettings.FromJson(response.RootElement, _client.JsonOptions);
    }

    public async Task<QuotaSettings> SetQuotaAsync(
        string workspaceId,
        UpdateQuotaOptions options,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(workspaceId))
            throw new ValidationException("Workspace ID is required");

        using var response = await _client.PutAsync(
            $"/enterprise/workspaces/{Uri.EscapeDataString(workspaceId)}/quota",
            options, cancellationToken);
        return QuotaSettings.FromJson(response.RootElement, _client.JsonOptions);
    }
}

public class EnterpriseWebhooksResource
{
    private readonly SendlyClient _client;

    internal EnterpriseWebhooksResource(SendlyClient client)
    {
        _client = client;
    }

    public async Task<EnterpriseWebhookConfig> SetAsync(
        string url,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(url))
            throw new ValidationException("Webhook URL is required");

        var options = new SetEnterpriseWebhookOptions { Url = url };
        using var response = await _client.PostAsync("/enterprise/webhooks", options, cancellationToken);
        return EnterpriseWebhookConfig.FromJson(response.RootElement, _client.JsonOptions);
    }

    public async Task<EnterpriseWebhookConfig> GetAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await _client.GetAsync("/enterprise/webhooks", null, cancellationToken);
        return EnterpriseWebhookConfig.FromJson(response.RootElement, _client.JsonOptions);
    }

    public async Task DeleteAsync(CancellationToken cancellationToken = default)
    {
        using var _ = await _client.DeleteAsync("/enterprise/webhooks", cancellationToken);
    }

    public async Task<EnterpriseWebhookTestResult> TestAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await _client.PostAsync<object>("/enterprise/webhooks/test", new { }, cancellationToken);
        return EnterpriseWebhookTestResult.FromJson(response.RootElement, _client.JsonOptions);
    }

    public async Task<EnterpriseWebhookSecretRotation> RotateSecretAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await _client.PostAsync<object>("/enterprise/webhooks/rotate-secret", new { }, cancellationToken);
        return EnterpriseWebhookSecretRotation.FromJson(response.RootElement, _client.JsonOptions);
    }
}

public class EnterpriseAnalyticsResource
{
    private readonly SendlyClient _client;

    internal EnterpriseAnalyticsResource(SendlyClient client)
    {
        _client = client;
    }

    public async Task<EnterpriseAnalyticsOverview> OverviewAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await _client.GetAsync("/enterprise/analytics/overview", null, cancellationToken);
        return EnterpriseAnalyticsOverview.FromJson(response.RootElement, _client.JsonOptions);
    }

    public async Task<EnterpriseMessagesAnalytics> MessagesAsync(
        EnterpriseAnalyticsOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var queryParams = options?.ToQueryParams();
        using var response = await _client.GetAsync("/enterprise/analytics/messages", queryParams, cancellationToken);
        return EnterpriseMessagesAnalytics.FromJson(response.RootElement, _client.JsonOptions);
    }

    public async Task<List<EnterpriseDeliveryAnalytics>> DeliveryAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await _client.GetAsync("/enterprise/analytics/delivery", null, cancellationToken);
        var root = response.RootElement;
        var results = new List<EnterpriseDeliveryAnalytics>();

        if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var element in root.EnumerateArray())
            {
                results.Add(EnterpriseDeliveryAnalytics.FromJson(element, _client.JsonOptions));
            }
        }
        else if (root.TryGetProperty("data", out var dataElement) &&
                 dataElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var element in dataElement.EnumerateArray())
            {
                results.Add(EnterpriseDeliveryAnalytics.FromJson(element, _client.JsonOptions));
            }
        }

        return results;
    }

    public async Task<EnterpriseCreditsAnalytics> CreditsAsync(
        EnterpriseAnalyticsOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var queryParams = options?.ToQueryParams();
        using var response = await _client.GetAsync("/enterprise/analytics/credits", queryParams, cancellationToken);
        return EnterpriseCreditsAnalytics.FromJson(response.RootElement, _client.JsonOptions);
    }
}

public class EnterpriseSettingsResource
{
    private readonly SendlyClient _client;

    internal EnterpriseSettingsResource(SendlyClient client)
    {
        _client = client;
    }

    public async Task<AutoTopUpSettings> GetAutoTopUpAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await _client.GetAsync("/enterprise/settings/auto-top-up", null, cancellationToken);
        return AutoTopUpSettings.FromJson(response.RootElement, _client.JsonOptions);
    }

    public async Task<AutoTopUpSettings> UpdateAutoTopUpAsync(
        UpdateAutoTopUpOptions options,
        CancellationToken cancellationToken = default)
    {
        using var response = await _client.PutAsync("/enterprise/settings/auto-top-up", options, cancellationToken);
        return AutoTopUpSettings.FromJson(response.RootElement, _client.JsonOptions);
    }
}

public class EnterpriseBillingResource
{
    private readonly SendlyClient _client;

    internal EnterpriseBillingResource(SendlyClient client)
    {
        _client = client;
    }

    public async Task<BillingBreakdown> GetBreakdownAsync(
        BillingBreakdownOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var queryParams = options?.ToQueryParams();
        using var response = await _client.GetAsync("/enterprise/billing/workspace-breakdown", queryParams, cancellationToken);
        return BillingBreakdown.FromJson(response.RootElement, _client.JsonOptions);
    }
}

public class EnterpriseCreditsResource
{
    private readonly SendlyClient _client;

    internal EnterpriseCreditsResource(SendlyClient client)
    {
        _client = client;
    }

    public async Task<PoolCredits> GetAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await _client.GetAsync("/enterprise/credits", null, cancellationToken);
        return PoolCredits.FromJson(response.RootElement, _client.JsonOptions);
    }

    public async Task<PoolCredits> DepositAsync(
        int amount,
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        if (amount <= 0)
            throw new ValidationException("Amount must be a positive integer");

        var options = new DepositCreditsOptions { Amount = amount, Description = description };
        using var response = await _client.PostAsync("/enterprise/credits/deposit", options, cancellationToken);
        return PoolCredits.FromJson(response.RootElement, _client.JsonOptions);
    }
}

public class EnterpriseResource
{
    private readonly SendlyClient _client;

    public EnterpriseWorkspacesResource Workspaces { get; }
    public EnterpriseWebhooksResource Webhooks { get; }
    public EnterpriseAnalyticsResource Analytics { get; }
    public EnterpriseSettingsResource Settings { get; }
    public EnterpriseBillingResource Billing { get; }
    public EnterpriseCreditsResource Credits { get; }

    internal EnterpriseResource(SendlyClient client)
    {
        _client = client;
        Workspaces = new EnterpriseWorkspacesResource(client);
        Webhooks = new EnterpriseWebhooksResource(client);
        Analytics = new EnterpriseAnalyticsResource(client);
        Settings = new EnterpriseSettingsResource(client);
        Billing = new EnterpriseBillingResource(client);
        Credits = new EnterpriseCreditsResource(client);
    }

    public async Task<EnterpriseAccount> GetAccountAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await _client.GetAsync("/enterprise/account", null, cancellationToken);
        return EnterpriseAccount.FromJson(response.RootElement, _client.JsonOptions);
    }

    public async Task<ProvisionWorkspaceResponse> ProvisionAsync(
        ProvisionWorkspaceOptions options,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(options.Name))
            throw new ValidationException("Workspace name is required");

        using var response = await _client.PostAsync("/enterprise/workspaces/provision", options, cancellationToken);
        return ProvisionWorkspaceResponse.FromJson(response.RootElement, _client.JsonOptions);
    }

    public async Task<GenerateBusinessPageResponse> GenerateBusinessPageAsync(
        GenerateBusinessPageOptions options,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(options.BusinessName))
            throw new ValidationException("Business name is required");

        using var response = await _client.PostAsync("/enterprise/business-page/generate", options, cancellationToken);
        return GenerateBusinessPageResponse.FromJson(response.RootElement, _client.JsonOptions);
    }

    public async Task<VerificationDocumentUploadResponse> UploadVerificationDocumentAsync(
        string filePath,
        string? workspaceId = null,
        string? verificationId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(filePath))
            throw new ValidationException("File path is required");

        if (!File.Exists(filePath))
            throw new ValidationException($"File not found: {filePath}");

        var fileName = Path.GetFileName(filePath);
        var contentType = Path.GetExtension(filePath)?.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".pdf" => "application/pdf",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };

        using var stream = File.OpenRead(filePath);
        return await UploadVerificationDocumentAsync(stream, fileName, contentType, workspaceId, verificationId, cancellationToken);
    }

    public async Task<VerificationDocumentUploadResponse> UploadVerificationDocumentAsync(
        Stream stream,
        string fileName,
        string contentType,
        string? workspaceId = null,
        string? verificationId = null,
        CancellationToken cancellationToken = default)
    {
        if (stream == null)
            throw new ValidationException("Stream is required");

        if (string.IsNullOrEmpty(fileName))
            throw new ValidationException("File name is required");

        using var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(stream);
        streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        content.Add(streamContent, "file", fileName);

        if (!string.IsNullOrEmpty(workspaceId))
            content.Add(new StringContent(workspaceId), "workspaceId");

        if (!string.IsNullOrEmpty(verificationId))
            content.Add(new StringContent(verificationId), "verificationId");

        using var response = await _client.PostContentAsync("/enterprise/verification-document/upload", content, cancellationToken);
        return VerificationDocumentUploadResponse.FromJson(response.RootElement, _client.JsonOptions);
    }
}
