using System.Text.Json;
using Sendly.Exceptions;
using Sendly.Models;

namespace Sendly.Resources;

/// <summary>
/// Resource for managing webhooks.
/// </summary>
public class WebhooksResource
{
    private readonly SendlyClient _client;

    internal WebhooksResource(SendlyClient client)
    {
        _client = client;
    }

    /// <summary>
    /// Creates a new webhook.
    /// </summary>
    /// <param name="url">URL to receive webhook events</param>
    /// <param name="events">List of event types to subscribe to</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created webhook with secret</returns>
    public async Task<WebhookCreatedResponse> CreateAsync(
        string url,
        List<string> events,
        CancellationToken cancellationToken = default)
    {
        return await CreateAsync(new CreateWebhookOptions { Url = url, Events = events }, cancellationToken);
    }

    /// <summary>
    /// Creates a new webhook.
    /// </summary>
    /// <param name="options">Webhook creation options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created webhook with secret</returns>
    public async Task<WebhookCreatedResponse> CreateAsync(
        CreateWebhookOptions options,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(options.Url))
            throw new ValidationException("Webhook URL is required");

        if (options.Events == null || options.Events.Count == 0)
            throw new ValidationException("At least one event type is required");

        using var response = await _client.PostAsync("/webhooks", options, cancellationToken);
        return WebhookCreatedResponse.FromJson(response.RootElement, _client.JsonOptions);
    }

    /// <summary>
    /// Lists all webhooks.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of webhooks</returns>
    public async Task<WebhookList> ListAsync(CancellationToken cancellationToken = default)
    {
        using var response = await _client.GetAsync("/webhooks", null, cancellationToken);
        return new WebhookList(response, _client.JsonOptions);
    }

    /// <summary>
    /// Gets a webhook by ID.
    /// </summary>
    /// <param name="id">Webhook ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The webhook</returns>
    public async Task<Webhook> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(id))
            throw new ValidationException("Webhook ID is required");

        using var response = await _client.GetAsync($"/webhooks/{Uri.EscapeDataString(id)}", null, cancellationToken);
        var root = response.RootElement;

        if (root.TryGetProperty("webhook", out var webhookElement) || root.TryGetProperty("data", out webhookElement))
        {
            return Webhook.FromJson(webhookElement, _client.JsonOptions);
        }

        return Webhook.FromJson(root, _client.JsonOptions);
    }

    /// <summary>
    /// Updates a webhook.
    /// </summary>
    /// <param name="id">Webhook ID</param>
    /// <param name="options">Update options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated webhook</returns>
    public async Task<Webhook> UpdateAsync(
        string id,
        UpdateWebhookOptions options,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(id))
            throw new ValidationException("Webhook ID is required");

        using var response = await _client.PatchAsync($"/webhooks/{Uri.EscapeDataString(id)}", options, cancellationToken);
        var root = response.RootElement;

        if (root.TryGetProperty("webhook", out var webhookElement) || root.TryGetProperty("data", out webhookElement))
        {
            return Webhook.FromJson(webhookElement, _client.JsonOptions);
        }

        return Webhook.FromJson(root, _client.JsonOptions);
    }

    /// <summary>
    /// Deletes a webhook.
    /// </summary>
    /// <param name="id">Webhook ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(id))
            throw new ValidationException("Webhook ID is required");

        using var _ = await _client.DeleteAsync($"/webhooks/{Uri.EscapeDataString(id)}", cancellationToken);
    }

    /// <summary>
    /// Tests a webhook endpoint.
    /// </summary>
    /// <param name="id">Webhook ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The test result</returns>
    public async Task<WebhookTestResult> TestAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(id))
            throw new ValidationException("Webhook ID is required");

        using var response = await _client.PostAsync<object>($"/webhooks/{Uri.EscapeDataString(id)}/test", new { }, cancellationToken);
        return WebhookTestResult.FromJson(response.RootElement, _client.JsonOptions);
    }

    /// <summary>
    /// Rotates a webhook's secret.
    /// </summary>
    /// <param name="id">Webhook ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The new secret</returns>
    public async Task<WebhookSecretRotation> RotateSecretAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(id))
            throw new ValidationException("Webhook ID is required");

        using var response = await _client.PostAsync<object>($"/webhooks/{Uri.EscapeDataString(id)}/rotate-secret", new { }, cancellationToken);
        return WebhookSecretRotation.FromJson(response.RootElement, _client.JsonOptions);
    }

    /// <summary>
    /// Lists delivery attempts for a webhook.
    /// </summary>
    /// <param name="id">Webhook ID</param>
    /// <param name="options">Query options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of delivery attempts</returns>
    public async Task<WebhookDeliveryList> ListDeliveriesAsync(
        string id,
        ListDeliveriesOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(id))
            throw new ValidationException("Webhook ID is required");

        var queryParams = options?.ToQueryParams();
        using var response = await _client.GetAsync($"/webhooks/{Uri.EscapeDataString(id)}/deliveries", queryParams, cancellationToken);
        return new WebhookDeliveryList(response, _client.JsonOptions);
    }

    /// <summary>
    /// Gets a specific delivery attempt.
    /// </summary>
    /// <param name="webhookId">Webhook ID</param>
    /// <param name="deliveryId">Delivery ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The delivery attempt</returns>
    public async Task<WebhookDelivery> GetDeliveryAsync(
        string webhookId,
        string deliveryId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(webhookId))
            throw new ValidationException("Webhook ID is required");
        if (string.IsNullOrEmpty(deliveryId))
            throw new ValidationException("Delivery ID is required");

        var path = $"/webhooks/{Uri.EscapeDataString(webhookId)}/deliveries/{Uri.EscapeDataString(deliveryId)}";
        using var response = await _client.GetAsync(path, null, cancellationToken);
        var root = response.RootElement;

        if (root.TryGetProperty("delivery", out var deliveryElement) || root.TryGetProperty("data", out deliveryElement))
        {
            return WebhookDelivery.FromJson(deliveryElement, _client.JsonOptions);
        }

        return WebhookDelivery.FromJson(root, _client.JsonOptions);
    }

    /// <summary>
    /// Retries a failed delivery.
    /// </summary>
    /// <param name="webhookId">Webhook ID</param>
    /// <param name="deliveryId">Delivery ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The new delivery attempt</returns>
    public async Task<WebhookDelivery> RetryDeliveryAsync(
        string webhookId,
        string deliveryId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(webhookId))
            throw new ValidationException("Webhook ID is required");
        if (string.IsNullOrEmpty(deliveryId))
            throw new ValidationException("Delivery ID is required");

        var path = $"/webhooks/{Uri.EscapeDataString(webhookId)}/deliveries/{Uri.EscapeDataString(deliveryId)}/retry";
        using var response = await _client.PostAsync<object>(path, new { }, cancellationToken);
        var root = response.RootElement;

        if (root.TryGetProperty("delivery", out var deliveryElement) || root.TryGetProperty("data", out deliveryElement))
        {
            return WebhookDelivery.FromJson(deliveryElement, _client.JsonOptions);
        }

        return WebhookDelivery.FromJson(root, _client.JsonOptions);
    }

    /// <summary>
    /// Lists available webhook event types.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of event type strings</returns>
    public async Task<List<string>> ListEventTypesAsync(CancellationToken cancellationToken = default)
    {
        using var response = await _client.GetAsync("/webhooks/event-types", null, cancellationToken);
        var root = response.RootElement;
        var eventTypes = new List<string>();

        if (root.TryGetProperty("events", out var eventsElement) && eventsElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var eventElement in eventsElement.EnumerateArray())
            {
                if (eventElement.TryGetProperty("type", out var typeElement))
                {
                    eventTypes.Add(typeElement.GetString() ?? string.Empty);
                }
            }
        }

        return eventTypes;
    }
}
