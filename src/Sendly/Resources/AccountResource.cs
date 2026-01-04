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
        using var response = await _client.GetAsync("/account/transactions", queryParams, cancellationToken);
        return new CreditTransactionList(response, _client.JsonOptions);
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
    /// Creates a new API key.
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
    /// Creates a new API key.
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
    /// Revokes an API key.
    /// </summary>
    /// <param name="id">API key ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task RevokeApiKeyAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(id))
            throw new ValidationException("API key ID is required");

        using var _ = await _client.DeleteAsync($"/account/keys/{Uri.EscapeDataString(id)}", cancellationToken);
    }
}
