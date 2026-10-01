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
        return await SendCoreAsync(request, null, cancellationToken);
    }

    /// <summary>
    /// Sends an SMS message with per-call options such as a caller-supplied
    /// idempotency key.
    ///
    /// The SDK already generates a key per logical request automatically, so
    /// the server can dedupe the SDK's own timeout retries. Supply your own
    /// key when you need idempotency across process restarts or your own
    /// retry loops — repeating a request with the same key within 24 hours
    /// returns the original result instead of sending again.
    /// </summary>
    /// <param name="request">Send message request</param>
    /// <param name="options">Per-call options, including an idempotency key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The sent message</returns>
    public async Task<Message> SendAsync(SendMessageRequest request, IdempotentRequestOptions options, CancellationToken cancellationToken = default)
    {
        return await SendCoreAsync(request, options?.IdempotencyKey, cancellationToken);
    }

    private async Task<Message> SendCoreAsync(SendMessageRequest request, string? idempotencyKey, CancellationToken cancellationToken)
    {
        ValidatePhone(request.To);
        ValidateText(request.Text);

        using var response = await _client.PostAsync("/messages", request, idempotencyKey, true, cancellationToken);
        var root = response.RootElement;

        JsonElement data;
        if (root.TryGetProperty("message", out data) || root.TryGetProperty("data", out data))
        {
            return Message.FromJson(data, _client.JsonOptions);
        }

        return Message.FromJson(root, _client.JsonOptions);
    }

    /// <summary>
    /// Sends a WhatsApp message.
    ///
    /// Requires the <c>sms:send</c> scope (not <c>whatsapp:write</c>), a live
    /// API key and a <see cref="SendWhatsAppMessageRequest.From"/> number with
    /// an active WhatsApp connection (see <c>client.WhatsApp.Signup</c>).
    /// WhatsApp is enabled per person (the user who owns the API key, not the
    /// workspace); while it is off the API responds 403
    /// <c>whatsapp_not_enabled</c>. Free-form text and media only deliver
    /// inside an open 24-hour customer-service window — outside it, send an
    /// approved <see cref="SendWhatsAppMessageRequest.Template"/> instead
    /// (check with <c>client.WhatsApp.WindowAsync</c>).
    /// </summary>
    /// <exception cref="ValidationException">
    /// <c>whatsapp_send_failed</c> (422) when WhatsApp refused the message.
    /// It is final and not retried, and the message wasn't charged. The API
    /// caches it under the idempotency key and replays it for 24 hours.
    /// </exception>
    /// <exception cref="SendlyException">
    /// <c>whatsapp_send_failed</c> (502) when the message provably never
    /// reached the carrier, so it was not sent and is safe to send again. It
    /// is never cached, so the client retries it like any 5xx under the same
    /// idempotency key; the message wasn't charged.
    /// <c>whatsapp_send_unconfirmed</c> (409) when the outcome is unknown: the
    /// message was marked failed and refunded but may still be delivered, so
    /// check before sending it again (it could arrive twice). It is not
    /// retried automatically, and the API caches it under the idempotency
    /// key. No send returns 503 <c>whatsapp_unavailable</c>.
    /// </exception>
    /// <param name="request">WhatsApp message details (text, media with caption, or template)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created WhatsApp message</returns>
    public async Task<WhatsAppMessage> SendAsync(SendWhatsAppMessageRequest request, CancellationToken cancellationToken = default)
    {
        return await SendCoreAsync(request, null, cancellationToken);
    }

    /// <summary>
    /// Sends a WhatsApp message with per-call options such as a
    /// caller-supplied idempotency key.
    ///
    /// The SDK already generates a key per logical request automatically, so
    /// the server can dedupe the SDK's own timeout retries. Supply your own
    /// key when you need idempotency across process restarts or your own
    /// retry loops.
    /// </summary>
    /// <param name="request">WhatsApp message details (text, media with caption, or template)</param>
    /// <param name="options">Per-call options, including an idempotency key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created WhatsApp message</returns>
    public async Task<WhatsAppMessage> SendAsync(SendWhatsAppMessageRequest request, IdempotentRequestOptions options, CancellationToken cancellationToken = default)
    {
        return await SendCoreAsync(request, options?.IdempotencyKey, cancellationToken);
    }

    private async Task<WhatsAppMessage> SendCoreAsync(SendWhatsAppMessageRequest request, string? idempotencyKey, CancellationToken cancellationToken)
    {
        ValidatePhone(request.To);
        ValidatePhone(request.From);

        var hasMedia = request.MediaUrls != null && request.MediaUrls.Count > 0;
        if (string.IsNullOrEmpty(request.Text) && !hasMedia && request.Template == null)
            throw new ValidationException("Provide 'text', 'mediaUrls', or 'template'");

        using var response = await _client.PostAsync("/messages", request, idempotencyKey, true, cancellationToken);
        var root = response.RootElement;

        JsonElement data;
        if (root.TryGetProperty("message", out data) || root.TryGetProperty("data", out data))
        {
            return WhatsAppMessage.FromJson(data, _client.JsonOptions);
        }

        return WhatsAppMessage.FromJson(root, _client.JsonOptions);
    }

    /// <summary>
    /// Sends an RCS message.
    ///
    /// Requires a live API key and an RCS agent registered on your workspace
    /// (see <c>client.Rcs.Agents</c>). Provide exactly one of
    /// <see cref="SendRcsMessageRequest.Text"/> or
    /// <see cref="SendRcsMessageRequest.Card"/>.
    ///
    /// Text sends fall back to SMS (and bill as SMS) for recipients whose
    /// device doesn't support RCS — check
    /// <see cref="RcsMessage.FellBackToSms"/> on the response. Rich cards have
    /// no SMS form and never fall back.
    /// </summary>
    /// <param name="request">RCS message details (text with optional suggestions, or a rich card)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created message — native RCS, or its SMS fallback</returns>
    public async Task<RcsMessage> SendAsync(SendRcsMessageRequest request, CancellationToken cancellationToken = default)
    {
        return await SendCoreAsync(request, null, cancellationToken);
    }

    /// <summary>
    /// Sends an RCS message with per-call options such as a caller-supplied
    /// idempotency key.
    ///
    /// The SDK already generates a key per logical request automatically, so
    /// the server can dedupe the SDK's own timeout retries. Supply your own
    /// key when you need idempotency across process restarts or your own
    /// retry loops.
    /// </summary>
    /// <param name="request">RCS message details (text with optional suggestions, or a rich card)</param>
    /// <param name="options">Per-call options, including an idempotency key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created message — native RCS, or its SMS fallback</returns>
    public async Task<RcsMessage> SendAsync(SendRcsMessageRequest request, IdempotentRequestOptions options, CancellationToken cancellationToken = default)
    {
        return await SendCoreAsync(request, options?.IdempotencyKey, cancellationToken);
    }

    private async Task<RcsMessage> SendCoreAsync(SendRcsMessageRequest request, string? idempotencyKey, CancellationToken cancellationToken)
    {
        ValidatePhone(request.To);

        var hasText = !string.IsNullOrEmpty(request.Text);
        var hasCard = request.Card != null;
        if (hasText == hasCard)
            throw new ValidationException("Provide exactly one of 'text' or 'card'");

        var hasSuggestions = request.Suggestions != null && request.Suggestions.Count > 0;
        if (hasSuggestions && !hasText)
            throw new ValidationException("'suggestions' ride on text messages. Put card buttons in card.suggestions instead");

        using var response = await _client.PostAsync("/messages", request, idempotencyKey, true, cancellationToken);
        var root = response.RootElement;

        JsonElement data;
        if (root.TryGetProperty("message", out data) || root.TryGetProperty("data", out data))
        {
            return RcsMessage.FromJson(data, _client.JsonOptions);
        }

        return RcsMessage.FromJson(root, _client.JsonOptions);
    }

    /// <summary>
    /// Sends a group MMS to 2-8 recipients (US/Canada only).
    ///
    /// Creates a multi-party MMS conversation: every recipient sees the others,
    /// and replies fan out to all participants. Group messaging is an A2P 10DLC
    /// capability — the sending number must be an MMS-enabled, 10DLC-registered
    /// number you own. Omit <see cref="SendGroupMessageRequest.From"/> to use
    /// your workspace's default sender. Requires the group_mms / enable_mms flag.
    /// </summary>
    /// <param name="request">Group message details (2-8 recipients, text and/or media)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created group message, including a group_message_id on live sends</returns>
    public async Task<GroupMessageResponse> SendGroupAsync(SendGroupMessageRequest request, CancellationToken cancellationToken = default)
    {
        return await SendGroupCoreAsync(request, null, cancellationToken);
    }

    /// <summary>
    /// Sends a group MMS with per-call options such as a caller-supplied
    /// idempotency key.
    ///
    /// The SDK already generates a key per logical request automatically, so
    /// the server can dedupe the SDK's own timeout retries. Supply your own
    /// key when you need idempotency across process restarts or your own
    /// retry loops.
    /// </summary>
    /// <param name="request">Group message details (2-8 recipients, text and/or media)</param>
    /// <param name="options">Per-call options, including an idempotency key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created group message, including a group_message_id on live sends</returns>
    public async Task<GroupMessageResponse> SendGroupAsync(SendGroupMessageRequest request, IdempotentRequestOptions options, CancellationToken cancellationToken = default)
    {
        return await SendGroupCoreAsync(request, options?.IdempotencyKey, cancellationToken);
    }

    private async Task<GroupMessageResponse> SendGroupCoreAsync(SendGroupMessageRequest request, string? idempotencyKey, CancellationToken cancellationToken)
    {
        if (request.To == null || request.To.Count < 2)
            throw new ValidationException("Group messaging requires at least 2 recipients in 'to'");

        if (request.To.Count > 8)
            throw new ValidationException("Group messaging supports at most 8 recipients");

        foreach (var recipient in request.To)
            ValidatePhone(recipient);

        var hasMedia = request.MediaUrls != null && request.MediaUrls.Count > 0;
        if (string.IsNullOrEmpty(request.Text) && !hasMedia)
            throw new ValidationException("Provide 'text' or 'mediaUrls'");

        using var response = await _client.PostAsync("/messages/group", request, idempotencyKey, true, cancellationToken);
        var root = response.RootElement;

        if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Object)
        {
            return GroupMessageResponse.FromJson(data, _client.JsonOptions);
        }

        return GroupMessageResponse.FromJson(root, _client.JsonOptions);
    }

    /// <summary>
    /// AI-enhances a draft message for clarity, compliance, and send-readiness.
    ///
    /// Rewrites the supplied text into a single, polished SMS segment (≤160
    /// chars) and returns a short explanation of what changed. Pass
    /// <see cref="EnhanceMessageRequest.MessageType"/> to steer the rewrite; with
    /// no text it generates a suitable message for that type instead. At least
    /// one of <see cref="EnhanceMessageRequest.Text"/> or
    /// <see cref="EnhanceMessageRequest.MessageType"/> is required.
    ///
    /// If AI enhancement is unavailable for the account, the response falls back
    /// to the original text with an empty explanation. Requires the
    /// ai_classification flag.
    /// </summary>
    /// <param name="request">The draft text and/or a message-type hint</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The enhanced text, an explanation, and the model used</returns>
    public async Task<EnhanceMessageResponse> EnhanceAsync(EnhanceMessageRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(request.Text) && string.IsNullOrEmpty(request.MessageType))
            throw new ValidationException("Provide 'text' or 'messageType'");

        using var response = await _client.PostAsync("/ai/enhance", request, cancellationToken);
        return EnhanceMessageResponse.FromJson(response.RootElement, _client.JsonOptions);
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

        using var response = await _client.GetAsync($"/messages/{Uri.EscapeDataString(id)}", null, cancellationToken);
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

            if (!page.HasMore || page.Count == 0)
                break;

            offset += page.Count;
        }
    }

    // ==================== Scheduling Methods ====================

    /// <summary>
    /// Schedules a message for future delivery.
    /// </summary>
    /// <param name="to">Recipient phone number in E.164 format</param>
    /// <param name="text">Message content</param>
    /// <param name="scheduledAt">ISO 8601 datetime, at least 5 minutes and at most 5 days in the future</param>
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
        return await ScheduleCoreAsync(request, null, cancellationToken);
    }

    /// <summary>
    /// Schedules a message for future delivery with per-call options such as
    /// a caller-supplied idempotency key.
    ///
    /// The SDK already generates a key per logical request automatically, so
    /// the server can dedupe the SDK's own timeout retries. Supply your own
    /// key when you need idempotency across process restarts or your own
    /// retry loops.
    /// </summary>
    /// <param name="request">Schedule message request</param>
    /// <param name="options">Per-call options, including an idempotency key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The scheduled message</returns>
    public async Task<ScheduledMessage> ScheduleAsync(ScheduleMessageRequest request, IdempotentRequestOptions options, CancellationToken cancellationToken = default)
    {
        return await ScheduleCoreAsync(request, options?.IdempotencyKey, cancellationToken);
    }

    private async Task<ScheduledMessage> ScheduleCoreAsync(ScheduleMessageRequest request, string? idempotencyKey, CancellationToken cancellationToken)
    {
        ValidatePhone(request.To);
        ValidateText(request.Text);
        ValidateScheduledAt(request.ScheduledAt);

        using var response = await _client.PostAsync("/messages/schedule", request, idempotencyKey, true, cancellationToken);
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
        return await SendBatchCoreAsync(request, null, cancellationToken);
    }

    /// <summary>
    /// Sends a batch of messages with per-call options such as a
    /// caller-supplied idempotency key.
    ///
    /// The batch endpoint dedupes header-less retries server-side by hashing
    /// the request content, so the SDK does not auto-generate a key here.
    /// Supply your own key when you need idempotency across process restarts
    /// or your own retry loops.
    /// </summary>
    /// <param name="request">Batch send request</param>
    /// <param name="options">Per-call options, including an idempotency key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The batch response with results</returns>
    public async Task<BatchMessageResponse> SendBatchAsync(SendBatchRequest request, IdempotentRequestOptions options, CancellationToken cancellationToken = default)
    {
        return await SendBatchCoreAsync(request, options?.IdempotencyKey, cancellationToken);
    }

    private async Task<BatchMessageResponse> SendBatchCoreAsync(SendBatchRequest request, string? idempotencyKey, CancellationToken cancellationToken)
    {
        if (request.Messages == null || request.Messages.Count == 0)
            throw new ValidationException("At least one message is required");

        // Validate all messages
        foreach (var item in request.Messages)
        {
            ValidatePhone(item.To);
            ValidateText(item.Text);
        }

        using var response = await _client.PostAsync("/messages/batch", request, idempotencyKey, autoIdempotencyKey: false, cancellationToken);
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
