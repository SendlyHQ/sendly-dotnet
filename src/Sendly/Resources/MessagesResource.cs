using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;
using Sendly.Exceptions;
using Sendly.Models;

namespace Sendly.Resources;

/// <summary>
/// Resource for sending and managing SMS messages.
/// </summary>
public partial class MessagesResource
{
    private static readonly Regex PhoneRegex = MyRegex();
    private const int MaxTextLength = 1600;

    private readonly SendlyClient _client;

    internal MessagesResource(SendlyClient client)
    {
        _client = client;
    }

    /// <summary>
    /// Sends an SMS message.
    /// </summary>
    /// <param name="to">Recipient phone number in E.164 format</param>
    /// <param name="text">Message content</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The sent message</returns>
    public async Task<Message> SendAsync(string to, string text, CancellationToken cancellationToken = default)
    {
        return await SendAsync(new SendMessageRequest(to, text), cancellationToken);
    }

    /// <summary>
    /// Sends an SMS message.
    /// </summary>
    /// <param name="request">Send message request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The sent message</returns>
    public async Task<Message> SendAsync(SendMessageRequest request, CancellationToken cancellationToken = default)
    {
        ValidatePhone(request.To);
        ValidateText(request.Text);

        using var response = await _client.PostAsync("/messages", request, cancellationToken);
        var root = response.RootElement;

        JsonElement data;
        if (root.TryGetProperty("message", out data) || root.TryGetProperty("data", out data))
        {
            return Message.FromJson(data, _client.JsonOptions);
        }

        return Message.FromJson(root, _client.JsonOptions);
    }

    /// <summary>
    /// Lists messages.
    /// </summary>
    /// <param name="options">Query options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of messages</returns>
    public async Task<MessageList> ListAsync(ListMessagesOptions? options = null, CancellationToken cancellationToken = default)
    {
        var queryParams = options?.ToQueryParams();
        using var response = await _client.GetAsync("/messages", queryParams, cancellationToken);
        return new MessageList(response, _client.JsonOptions);
    }

    /// <summary>
    /// Gets a message by ID.
    /// </summary>
    /// <param name="id">Message ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The message</returns>
    public async Task<Message> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(id))
            throw new ValidationException("Message ID is required");

        using var response = await _client.GetAsync($"/messages/{id}", null, cancellationToken);
        var root = response.RootElement;

        JsonElement data;
        if (root.TryGetProperty("data", out data) || root.TryGetProperty("message", out data))
        {
            return Message.FromJson(data, _client.JsonOptions);
        }

        return Message.FromJson(root, _client.JsonOptions);
    }

    /// <summary>
    /// Iterates over all messages with automatic pagination.
    /// </summary>
    /// <param name="options">Query options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Async enumerable of messages</returns>
    public async IAsyncEnumerable<Message> GetAllAsync(
        ListMessagesOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var offset = options?.Offset ?? 0;
        var batchSize = options?.Limit ?? 100;

        while (true)
        {
            var page = await ListAsync(new ListMessagesOptions
            {
                Limit = batchSize,
                Offset = offset,
                Status = options?.Status,
                To = options?.To
            }, cancellationToken);

            foreach (var message in page)
            {
                yield return message;
            }

            if (!page.HasMore)
                break;

            offset += batchSize;
        }
    }

    // ==================== Scheduling Methods ====================

    /// <summary>
    /// Schedules a message for future delivery.
    /// </summary>
    /// <param name="to">Recipient phone number in E.164 format</param>
    /// <param name="text">Message content</param>
    /// <param name="scheduledAt">ISO 8601 datetime (must be at least 1 minute in the future)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The scheduled message</returns>
    public async Task<ScheduledMessage> ScheduleAsync(string to, string text, string scheduledAt, CancellationToken cancellationToken = default)
    {
        return await ScheduleAsync(new ScheduleMessageRequest(to, text, scheduledAt), cancellationToken);
    }

    /// <summary>
    /// Schedules a message for future delivery.
    /// </summary>
    /// <param name="request">Schedule message request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The scheduled message</returns>
    public async Task<ScheduledMessage> ScheduleAsync(ScheduleMessageRequest request, CancellationToken cancellationToken = default)
    {
        ValidatePhone(request.To);
        ValidateText(request.Text);
        ValidateScheduledAt(request.ScheduledAt);

        using var response = await _client.PostAsync("/messages/schedule", request, cancellationToken);
        var root = response.RootElement;

        JsonElement data;
        if (root.TryGetProperty("data", out data))
        {
            return ScheduledMessage.FromJson(data, _client.JsonOptions);
        }

        return ScheduledMessage.FromJson(root, _client.JsonOptions);
    }

    /// <summary>
    /// Lists scheduled messages.
    /// </summary>
    /// <param name="options">Query options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of scheduled messages</returns>
    public async Task<ScheduledMessageList> ListScheduledAsync(ListScheduledMessagesOptions? options = null, CancellationToken cancellationToken = default)
    {
        var queryParams = options?.ToQueryParams();
        using var response = await _client.GetAsync("/messages/scheduled", queryParams, cancellationToken);
        return new ScheduledMessageList(response, _client.JsonOptions);
    }

    /// <summary>
    /// Gets a scheduled message by ID.
    /// </summary>
    /// <param name="id">Scheduled message ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The scheduled message</returns>
    public async Task<ScheduledMessage> GetScheduledAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(id))
            throw new ValidationException("Scheduled message ID is required");

        var encodedId = Uri.EscapeDataString(id);
        using var response = await _client.GetAsync($"/messages/scheduled/{encodedId}", null, cancellationToken);
        var root = response.RootElement;

        JsonElement data;
        if (root.TryGetProperty("data", out data))
        {
            return ScheduledMessage.FromJson(data, _client.JsonOptions);
        }

        return ScheduledMessage.FromJson(root, _client.JsonOptions);
    }

    /// <summary>
    /// Cancels a scheduled message.
    /// </summary>
    /// <param name="id">Scheduled message ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The cancellation response with refunded credits</returns>
    public async Task<CancelScheduledMessageResponse> CancelScheduledAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(id))
            throw new ValidationException("Scheduled message ID is required");

        var encodedId = Uri.EscapeDataString(id);
        using var response = await _client.DeleteAsync($"/messages/scheduled/{encodedId}", cancellationToken);
        return CancelScheduledMessageResponse.FromJson(response.RootElement, _client.JsonOptions);
    }

    // ==================== Batch Methods ====================

    /// <summary>
    /// Sends a batch of messages.
    /// </summary>
    /// <param name="request">Batch send request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The batch response with results</returns>
    public async Task<BatchMessageResponse> SendBatchAsync(SendBatchRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Messages == null || request.Messages.Count == 0)
            throw new ValidationException("At least one message is required");

        // Validate all messages
        foreach (var item in request.Messages)
        {
            ValidatePhone(item.To);
            ValidateText(item.Text);
        }

        using var response = await _client.PostAsync("/messages/batch", request, cancellationToken);
        return BatchMessageResponse.FromJson(response.RootElement, _client.JsonOptions);
    }

    /// <summary>
    /// Gets a batch by ID.
    /// </summary>
    /// <param name="batchId">Batch ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The batch response</returns>
    public async Task<BatchMessageResponse> GetBatchAsync(string batchId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(batchId))
            throw new ValidationException("Batch ID is required");

        var encodedId = Uri.EscapeDataString(batchId);
        using var response = await _client.GetAsync($"/messages/batch/{encodedId}", null, cancellationToken);
        return BatchMessageResponse.FromJson(response.RootElement, _client.JsonOptions);
    }

    /// <summary>
    /// Lists all batches.
    /// </summary>
    /// <param name="options">Query options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of batches</returns>
    public async Task<BatchList> ListBatchesAsync(ListBatchesOptions? options = null, CancellationToken cancellationToken = default)
    {
        var queryParams = options?.ToQueryParams();
        using var response = await _client.GetAsync("/messages/batches", queryParams, cancellationToken);
        return new BatchList(response, _client.JsonOptions);
    }

    /// <summary>
    /// Previews a batch without sending (dry run).
    /// </summary>
    /// <param name="request">Batch send request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Preview showing what would happen if batch was sent</returns>
    public async Task<BatchPreviewResponse> PreviewBatchAsync(SendBatchRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Messages == null || request.Messages.Count == 0)
            throw new ValidationException("At least one message is required");

        // Validate all messages
        foreach (var item in request.Messages)
        {
            ValidatePhone(item.To);
            ValidateText(item.Text);
        }

        using var response = await _client.PostAsync("/messages/batch/preview", request, cancellationToken);
        return BatchPreviewResponse.FromJson(response.RootElement, _client.JsonOptions);
    }

    // ==================== Validation Helpers ====================

    private static void ValidatePhone(string phone)
    {
        if (string.IsNullOrEmpty(phone) || !PhoneRegex.IsMatch(phone))
        {
            throw new ValidationException(
                "Invalid phone number format. Use E.164 format (e.g., +15551234567)");
        }
    }

    private static void ValidateText(string text)
    {
        if (string.IsNullOrEmpty(text))
            throw new ValidationException("Message text is required");

        if (text.Length > MaxTextLength)
            throw new ValidationException($"Message text exceeds maximum length ({MaxTextLength} characters)");
    }

    private static void ValidateScheduledAt(string scheduledAt)
    {
        if (string.IsNullOrEmpty(scheduledAt))
            throw new ValidationException("Scheduled time is required");

        // Basic ISO 8601 format validation
        if (!Regex.IsMatch(scheduledAt, @"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}"))
        {
            throw new ValidationException(
                "Invalid scheduled time format. Use ISO 8601 format (e.g., 2025-01-20T10:00:00Z)");
        }
    }

    [GeneratedRegex(@"^\+[1-9]\d{1,14}$")]
    private static partial Regex MyRegex();
}
