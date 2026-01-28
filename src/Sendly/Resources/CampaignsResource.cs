using System.Text.Json;
using Sendly.Models;

namespace Sendly.Resources;

public class CampaignsResource
{
    private readonly SendlyClient _client;

    public CampaignsResource(SendlyClient client)
    {
        _client = client;
    }

    public async Task<CampaignListResponse> ListAsync(
        ListCampaignsOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string>();
        if (options?.Limit.HasValue == true)
            queryParams["limit"] = Math.Min(options.Limit.Value, 100).ToString();
        if (options?.Offset.HasValue == true)
            queryParams["offset"] = options.Offset.Value.ToString();
        if (!string.IsNullOrEmpty(options?.Status))
            queryParams["status"] = options.Status;

        var doc = await _client.GetAsync("/campaigns", queryParams, cancellationToken);
        return JsonSerializer.Deserialize<CampaignListResponse>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    public async Task<Campaign> GetAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        var doc = await _client.GetAsync($"/campaigns/{id}", null, cancellationToken);
        return JsonSerializer.Deserialize<Campaign>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    public async Task<Campaign> CreateAsync(
        CreateCampaignRequest request,
        CancellationToken cancellationToken = default)
    {
        var doc = await _client.PostAsync("/campaigns", request, cancellationToken);
        return JsonSerializer.Deserialize<Campaign>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    public async Task<Campaign> UpdateAsync(
        string id,
        UpdateCampaignRequest request,
        CancellationToken cancellationToken = default)
    {
        var doc = await _client.PatchAsync($"/campaigns/{id}", request, cancellationToken);
        return JsonSerializer.Deserialize<Campaign>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    public async Task DeleteAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        await _client.DeleteAsync($"/campaigns/{id}", cancellationToken);
    }

    public async Task<CampaignPreview> PreviewAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        var doc = await _client.GetAsync($"/campaigns/{id}/preview", null, cancellationToken);
        return JsonSerializer.Deserialize<CampaignPreview>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    public async Task<Campaign> SendAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        var doc = await _client.PostAsync($"/campaigns/{id}/send", new { }, cancellationToken);
        return JsonSerializer.Deserialize<Campaign>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    public async Task<Campaign> ScheduleAsync(
        string id,
        ScheduleCampaignRequest request,
        CancellationToken cancellationToken = default)
    {
        var doc = await _client.PostAsync($"/campaigns/{id}/schedule", request, cancellationToken);
        return JsonSerializer.Deserialize<Campaign>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    public async Task<Campaign> CancelAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        var doc = await _client.PostAsync($"/campaigns/{id}/cancel", new { }, cancellationToken);
        return JsonSerializer.Deserialize<Campaign>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    public async Task<Campaign> CloneAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        var doc = await _client.PostAsync($"/campaigns/{id}/clone", new { }, cancellationToken);
        return JsonSerializer.Deserialize<Campaign>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }
}
