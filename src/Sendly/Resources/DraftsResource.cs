using System.Text.Json;
using Sendly.Exceptions;
using Sendly.Models;

namespace Sendly.Resources;

public class DraftsResource
{
    private readonly SendlyClient _client;

    internal DraftsResource(SendlyClient client)
    {
        _client = client;
    }

    /// <summary>
    /// Creates a new draft.
    /// </summary>
    /// <param name="request">Create draft request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created draft</returns>
    public async Task<Draft> CreateAsync(
        CreateDraftRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(request.ConversationId))
            throw new ValidationException("Conversation ID is required");

        if (string.IsNullOrEmpty(request.Text))
            throw new ValidationException("Draft text is required");

        using var doc = await _client.PostAsync("/drafts", request, cancellationToken);
        return JsonSerializer.Deserialize<Draft>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    /// <summary>
    /// Lists drafts.
    /// </summary>
    /// <param name="options">Query options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of drafts</returns>
    public async Task<DraftListResponse> ListAsync(
        ListDraftsOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var queryParams = options?.ToQueryParams();
        using var doc = await _client.GetAsync("/drafts", queryParams, cancellationToken);
        return JsonSerializer.Deserialize<DraftListResponse>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    /// <summary>
    /// Gets a draft by ID.
    /// </summary>
    /// <param name="id">Draft ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The draft</returns>
    public async Task<Draft> GetAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(id))
            throw new ValidationException("Draft ID is required");

        using var doc = await _client.GetAsync($"/drafts/{Uri.EscapeDataString(id)}", null, cancellationToken);
        return JsonSerializer.Deserialize<Draft>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    /// <summary>
    /// Updates a draft.
    /// </summary>
    /// <param name="id">Draft ID</param>
    /// <param name="request">Update request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated draft</returns>
    public async Task<Draft> UpdateAsync(
        string id,
        UpdateDraftRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(id))
            throw new ValidationException("Draft ID is required");

        using var doc = await _client.PatchAsync($"/drafts/{Uri.EscapeDataString(id)}", request, cancellationToken);
        return JsonSerializer.Deserialize<Draft>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    /// <summary>
    /// Approves a draft.
    /// </summary>
    /// <param name="id">Draft ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The approved draft</returns>
    public async Task<Draft> ApproveAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(id))
            throw new ValidationException("Draft ID is required");

        using var doc = await _client.PostAsync($"/drafts/{Uri.EscapeDataString(id)}/approve", new { }, cancellationToken);
        return JsonSerializer.Deserialize<Draft>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    /// <summary>
    /// Rejects a draft.
    /// </summary>
    /// <param name="id">Draft ID</param>
    /// <param name="reason">Optional rejection reason</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The rejected draft</returns>
    public async Task<Draft> RejectAsync(
        string id,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(id))
            throw new ValidationException("Draft ID is required");

        var body = reason != null ? new RejectDraftRequest { Reason = reason } : new RejectDraftRequest();
        using var doc = await _client.PostAsync($"/drafts/{Uri.EscapeDataString(id)}/reject", body, cancellationToken);
        return JsonSerializer.Deserialize<Draft>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }
}
