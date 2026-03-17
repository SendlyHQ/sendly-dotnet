using System.Text.Json;
using Sendly.Exceptions;
using Sendly.Models;

namespace Sendly.Resources;

public class ConversationsResource
{
    private readonly SendlyClient _client;

    internal ConversationsResource(SendlyClient client)
    {
        _client = client;
    }

    /// <summary>
    /// Lists conversations.
    /// </summary>
    /// <param name="options">Query options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of conversations</returns>
    public async Task<ConversationListResponse> ListAsync(
        ListConversationsOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var queryParams = options?.ToQueryParams();
        using var doc = await _client.GetAsync("/conversations", queryParams, cancellationToken);
        return JsonSerializer.Deserialize<ConversationListResponse>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    /// <summary>
    /// Gets a conversation by ID.
    /// </summary>
    /// <param name="id">Conversation ID</param>
    /// <param name="options">Query options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The conversation</returns>
    public async Task<ConversationWithMessages> GetAsync(
        string id,
        GetConversationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(id))
            throw new ValidationException("Conversation ID is required");

        var queryParams = options?.ToQueryParams();
        using var doc = await _client.GetAsync($"/conversations/{Uri.EscapeDataString(id)}", queryParams, cancellationToken);
        return JsonSerializer.Deserialize<ConversationWithMessages>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    /// <summary>
    /// Replies to a conversation.
    /// </summary>
    /// <param name="id">Conversation ID</param>
    /// <param name="request">Reply request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The sent message</returns>
    public async Task<Message> ReplyAsync(
        string id,
        ReplyToConversationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(id))
            throw new ValidationException("Conversation ID is required");

        if (string.IsNullOrEmpty(request.Text))
            throw new ValidationException("Message text is required");

        using var doc = await _client.PostAsync($"/conversations/{Uri.EscapeDataString(id)}/messages", request, cancellationToken);
        return JsonSerializer.Deserialize<Message>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    /// <summary>
    /// Updates a conversation.
    /// </summary>
    /// <param name="id">Conversation ID</param>
    /// <param name="request">Update request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated conversation</returns>
    public async Task<Conversation> UpdateAsync(
        string id,
        UpdateConversationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(id))
            throw new ValidationException("Conversation ID is required");

        using var doc = await _client.PatchAsync($"/conversations/{Uri.EscapeDataString(id)}", request, cancellationToken);
        return JsonSerializer.Deserialize<Conversation>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    /// <summary>
    /// Closes a conversation.
    /// </summary>
    /// <param name="id">Conversation ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The closed conversation</returns>
    public async Task<Conversation> CloseAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(id))
            throw new ValidationException("Conversation ID is required");

        using var doc = await _client.PostAsync($"/conversations/{Uri.EscapeDataString(id)}/close", new { }, cancellationToken);
        return JsonSerializer.Deserialize<Conversation>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    /// <summary>
    /// Reopens a conversation.
    /// </summary>
    /// <param name="id">Conversation ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The reopened conversation</returns>
    public async Task<Conversation> ReopenAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(id))
            throw new ValidationException("Conversation ID is required");

        using var doc = await _client.PostAsync($"/conversations/{Uri.EscapeDataString(id)}/reopen", new { }, cancellationToken);
        return JsonSerializer.Deserialize<Conversation>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    /// <summary>
    /// Marks a conversation as read.
    /// </summary>
    /// <param name="id">Conversation ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated conversation</returns>
    public async Task<Conversation> MarkReadAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(id))
            throw new ValidationException("Conversation ID is required");

        using var doc = await _client.PostAsync($"/conversations/{Uri.EscapeDataString(id)}/mark-read", new { }, cancellationToken);
        return JsonSerializer.Deserialize<Conversation>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    /// <summary>
    /// Adds labels to a conversation.
    /// </summary>
    /// <param name="id">Conversation ID</param>
    /// <param name="request">Add labels request containing label IDs</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated label list</returns>
    public async Task<LabelListResponse> AddLabelsAsync(
        string id,
        AddLabelsRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(id))
            throw new ValidationException("Conversation ID is required");

        if (request.LabelIds == null || request.LabelIds.Count == 0)
            throw new ValidationException("Label IDs are required");

        using var doc = await _client.PostAsync($"/conversations/{Uri.EscapeDataString(id)}/labels", request, cancellationToken);
        return JsonSerializer.Deserialize<LabelListResponse>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    /// <summary>
    /// Removes a label from a conversation.
    /// </summary>
    /// <param name="id">Conversation ID</param>
    /// <param name="labelId">Label ID to remove</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task RemoveLabelAsync(
        string id,
        string labelId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(id))
            throw new ValidationException("Conversation ID is required");

        if (string.IsNullOrEmpty(labelId))
            throw new ValidationException("Label ID is required");

        using var doc = await _client.DeleteAsync($"/conversations/{Uri.EscapeDataString(id)}/labels/{Uri.EscapeDataString(labelId)}", cancellationToken);
    }
}
