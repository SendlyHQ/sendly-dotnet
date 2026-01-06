using System.Text.Json;
using Sendly.Models;

namespace Sendly.Resources;

public class SessionsResource
{
    private readonly SendlyClient _client;

    public SessionsResource(SendlyClient client)
    {
        _client = client;
    }

    public async Task<VerifySession> CreateAsync(
        CreateSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        var doc = await _client.PostAsync("/verify/sessions", request, cancellationToken);
        return JsonSerializer.Deserialize<VerifySession>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    public async Task<ValidateSessionResponse> ValidateAsync(
        ValidateSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        var doc = await _client.PostAsync("/verify/sessions/validate", request, cancellationToken);
        return JsonSerializer.Deserialize<ValidateSessionResponse>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }
}

public class VerifyResource
{
    private readonly SendlyClient _client;
    public SessionsResource Sessions { get; }

    public VerifyResource(SendlyClient client)
    {
        _client = client;
        Sessions = new SessionsResource(client);
    }

    public async Task<SendVerificationResponse> SendAsync(
        SendVerificationRequest request,
        CancellationToken cancellationToken = default)
    {
        var doc = await _client.PostAsync("/verify", request, cancellationToken);
        return JsonSerializer.Deserialize<SendVerificationResponse>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    public async Task<SendVerificationResponse> ResendAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        var doc = await _client.PostAsync($"/verify/{id}/resend", new { }, cancellationToken);
        return JsonSerializer.Deserialize<SendVerificationResponse>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    public async Task<CheckVerificationResponse> CheckAsync(
        string id,
        string code,
        CancellationToken cancellationToken = default)
    {
        var doc = await _client.PostAsync($"/verify/{id}/check", new { code }, cancellationToken);
        return JsonSerializer.Deserialize<CheckVerificationResponse>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    public async Task<Verification> GetAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        var doc = await _client.GetAsync($"/verify/{id}", null, cancellationToken);
        return JsonSerializer.Deserialize<Verification>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    public async Task<VerificationListResponse> ListAsync(
        ListVerificationsOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string>();
        if (options?.Limit.HasValue == true)
            queryParams["limit"] = options.Limit.Value.ToString();
        if (!string.IsNullOrEmpty(options?.Status))
            queryParams["status"] = options.Status;
        if (!string.IsNullOrEmpty(options?.Phone))
            queryParams["phone"] = options.Phone;

        var doc = await _client.GetAsync("/verify", queryParams, cancellationToken);
        return JsonSerializer.Deserialize<VerificationListResponse>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }
}
