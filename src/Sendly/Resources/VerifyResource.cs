using System.Text.Json;
using Sendly.Models;

namespace Sendly.Resources;

public class VerifyResource
{
    private readonly SendlyClient _client;

    public VerifyResource(SendlyClient client)
    {
        _client = client;
    }

    public async Task<SendVerificationResponse> SendAsync(
        SendVerificationRequest request,
        CancellationToken cancellationToken = default)
    {
        var doc = await _client.PostAsync("/verify/send", request, cancellationToken);
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
