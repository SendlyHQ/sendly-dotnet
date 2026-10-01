using System.Text.Json;
using Sendly.Exceptions;
using Sendly.Models;

namespace Sendly.Resources;

/// <summary>
/// Resource for managing account information and credits.
/// </summary>
public class AccountResource
{
    private readonly SendlyClient _client;

    internal AccountResource(SendlyClient client)
    {
        _client = client;
    }

    /// <summary>
    /// Gets current account information.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The account information</returns>
    public async Task<Account> GetAsync(CancellationToken cancellationToken = default)
    {
        using var response = await _client.GetAsync("/account", null, cancellationToken);
        var root = response.RootElement;

        if (root.TryGetProperty("account", out var accountElement) || root.TryGetProperty("data", out accountElement))
        {
            return Account.FromJson(accountElement, _client.JsonOptions);
        }

        return Account.FromJson(root, _client.JsonOptions);
    }

    /// <summary>
    /// Gets current credit balance.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The credit balance</returns>
    public async Task<Credits> GetCreditsAsync(CancellationToken cancellationToken = default)
    {
        using var response = await _client.GetAsync("/account/credits", null, cancellationToken);
        var root = response.RootElement;

        if (root.TryGetProperty("credits", out var creditsElement) || root.TryGetProperty("data", out creditsElement))
        {
            return Credits.FromJson(creditsElement, _client.JsonOptions);
        }

        return Credits.FromJson(root, _client.JsonOptions);
    }

    /// <summary>
    /// Lists credit transactions.
    /// </summary>
    /// <param name="options">Query options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of transactions</returns>
    public async Task<CreditTransactionList> ListTransactionsAsync(
        ListTransactionsOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var queryParams = options?.ToQueryParams();
        using var response = await _client.GetAsync("/credits/transactions", queryParams, cancellationToken);
        return new CreditTransactionList(response, _client.JsonOptions);
    }

    public async Task<TransferCreditsResponse> TransferCreditsAsync(
        string targetOrganizationId,
        int amount,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(targetOrganizationId))
            throw new ValidationException("Target organization ID is required");
        if (amount <= 0)
            throw new ValidationException("Amount must be a positive integer");

        var options = new TransferCreditsOptions
        {
            TargetOrganizationId = targetOrganizationId,
            Amount = amount
        };

        using var response = await _client.PostAsync("/credits/transfer", options, cancellationToken);
        return TransferCreditsResponse.FromJson(response.RootElement, _client.JsonOptions);
    }

    /// <summary>
    /// Lists API keys.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of API keys</returns>
    public async Task<ApiKeyList> ListApiKeysAsync(CancellationToken cancellationToken = default)
    {
        using var response = await _client.GetAsync("/account/keys", null, cancellationToken);
        return new ApiKeyList(response, _client.JsonOptions);
    }

    /// <summary>
    /// Creates a new test API key. Use the <see cref="CreateApiKeyOptions"/>
    /// overload to create a live key or to choose its scopes.
    /// </summary>
    /// <param name="name">Display name for the API key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created API key with full key value</returns>
    public async Task<CreateApiKeyResponse> CreateApiKeyAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        return await CreateApiKeyAsync(new CreateApiKeyOptions { Name = name }, cancellationToken);
    }

    /// <summary>
    /// Creates a new API key: a test key unless
    /// <see cref="CreateApiKeyOptions.Type"/> is <c>live</c>.
    /// </summary>
    /// <param name="options">API key creation options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created API key with full key value</returns>
    public async Task<CreateApiKeyResponse> CreateApiKeyAsync(
        CreateApiKeyOptions options,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(options.Name))
            throw new ValidationException("API key name is required");

        using var response = await _client.PostAsync("/account/keys", options, cancellationToken);
        return CreateApiKeyResponse.FromJson(response.RootElement, _client.JsonOptions);
    }

    /// <summary>
    /// Gets a specific API key by ID.
    /// </summary>
    /// <param name="id">API key ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The API key</returns>
    public async Task<ApiKey> GetApiKeyAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(id))
            throw new ValidationException("API key ID is required");

        using var response = await _client.GetAsync($"/account/keys/{Uri.EscapeDataString(id)}", null, cancellationToken);
        var root = response.RootElement;

        if (root.TryGetProperty("api_key", out var keyElement) ||
            root.TryGetProperty("apiKey", out keyElement) ||
            root.TryGetProperty("data", out keyElement))
        {
            return ApiKey.FromJson(keyElement, _client.JsonOptions);
        }

        return ApiKey.FromJson(root, _client.JsonOptions);
    }

    /// <summary>
    /// Gets usage statistics for a specific API key.
    /// </summary>
    /// <param name="id">API key ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Usage statistics</returns>
    public async Task<ApiKeyUsage> GetApiKeyUsageAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(id))
            throw new ValidationException("API key ID is required");

        using var response = await _client.GetAsync($"/account/keys/{Uri.EscapeDataString(id)}/usage", null, cancellationToken);
        var root = response.RootElement;

        if (root.TryGetProperty("usage", out var usageElement) ||
            root.TryGetProperty("data", out usageElement))
        {
            return ApiKeyUsage.FromJson(usageElement, _client.JsonOptions);
        }

        return ApiKeyUsage.FromJson(root, _client.JsonOptions);
    }

    /// <summary>
    /// Revokes an API key. The key you are currently authenticating with cannot
    /// be revoked.
    /// </summary>
    /// <param name="id">API key ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task RevokeApiKeyAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(id))
            throw new ValidationException("API key ID is required");

        using var _ = await _client.PatchAsync(
            $"/account/keys/{Uri.EscapeDataString(id)}/revoke", new { }, cancellationToken);
    }

    /// <summary>
    /// Rotates an API key. Mints a replacement key and keeps the old one working
    /// for a grace period, so you can roll the new key out before the old one
    /// stops. The raw value of the new key is returned once, on
    /// <see cref="RotatedApiKey.Key"/> — store it securely.
    /// </summary>
    /// <param name="id">API key ID to rotate</param>
    /// <param name="gracePeriodHours">
    /// How long the old key keeps working, in hours (24-168 inclusive). Defaults
    /// to 24 when null.
    /// </param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The new key (with its one-time raw value), the old key, and a message</returns>
    public async Task<RotateApiKeyResponse> RotateApiKeyAsync(
        string id,
        int? gracePeriodHours = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(id))
            throw new ValidationException("API key ID is required");
        if (gracePeriodHours.HasValue && (gracePeriodHours.Value < 24 || gracePeriodHours.Value > 168))
            throw new ValidationException("Grace period must be between 24 and 168 hours");

        var request = new RotateApiKeyRequest { GracePeriodHours = gracePeriodHours };
        using var response = await _client.PostAsync(
            $"/account/keys/{Uri.EscapeDataString(id)}/rotate", request, cancellationToken);
        return RotateApiKeyResponse.FromJson(response.RootElement, _client.JsonOptions);
    }
}
