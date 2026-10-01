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
        var doc = await _client.GetAsync($"/campaigns/{Uri.EscapeDataString(id)}", null, cancellationToken);
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
        var doc = await _client.PatchAsync($"/campaigns/{Uri.EscapeDataString(id)}", request, cancellationToken);
        return JsonSerializer.Deserialize<Campaign>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    public async Task DeleteAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        await _client.DeleteAsync($"/campaigns/{Uri.EscapeDataString(id)}", cancellationToken);
    }

    public async Task<CampaignPreview> PreviewAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        var doc = await _client.GetAsync($"/campaigns/{Uri.EscapeDataString(id)}/preview", null, cancellationToken);
        return JsonSerializer.Deserialize<CampaignPreview>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    /// <summary>
    /// Sends a draft or scheduled campaign now. The API answers with the
    /// message batch the send created, not the campaign, so the campaign
    /// returned carries only <see cref="Campaign.Id"/>,
    /// <see cref="Campaign.BatchId"/>, <see cref="Campaign.RecipientCount"/>,
    /// <see cref="Campaign.SentCount"/>, <see cref="Campaign.FailedCount"/>
    /// and <see cref="Campaign.CreditsUsed"/>, and its
    /// <see cref="Campaign.Status"/> is the batch's status, such as
    /// <c>completed</c>, <c>partial_failure</c>, <c>failed</c> or
    /// <c>processing</c>. The campaign itself is now <c>completed</c>; call
    /// <see cref="GetAsync"/> for its name, text and dates.
    /// </summary>
    public async Task<Campaign> SendAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        var doc = await _client.PostAsync($"/campaigns/{Uri.EscapeDataString(id)}/send", new { }, cancellationToken);
        var root = doc.RootElement;
        var campaign = JsonSerializer.Deserialize<Campaign>(root.GetRawText(), _client.JsonOptions)!;
        campaign.Id = id;
        if (root.TryGetProperty("total", out var total) && total.TryGetInt32(out var recipientCount))
            campaign.RecipientCount = recipientCount;
        if (root.TryGetProperty("sent", out var sent) && sent.TryGetInt32(out var sentCount))
            campaign.SentCount = sentCount;
        if (root.TryGetProperty("failed", out var failed) && failed.TryGetInt32(out var failedCount))
            campaign.FailedCount = failedCount;
        return campaign;
    }

    public async Task<Campaign> ScheduleAsync(
        string id,
        ScheduleCampaignRequest request,
        CancellationToken cancellationToken = default)
    {
        var doc = await _client.PostAsync($"/campaigns/{Uri.EscapeDataString(id)}/schedule", request, cancellationToken);
        return JsonSerializer.Deserialize<Campaign>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    public async Task<Campaign> CancelAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        var doc = await _client.PostAsync($"/campaigns/{Uri.EscapeDataString(id)}/cancel", new { }, cancellationToken);
        return JsonSerializer.Deserialize<Campaign>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    public async Task<Campaign> CloneAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        var doc = await _client.PostAsync($"/campaigns/{Uri.EscapeDataString(id)}/clone", new { }, cancellationToken);
        return JsonSerializer.Deserialize<Campaign>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }
}
