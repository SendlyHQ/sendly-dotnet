using System.Text.Json;
using Sendly.Exceptions;
using Sendly.Models;

namespace Sendly.Resources;

public class TemplatesResource
{
    private readonly SendlyClient _client;

    public TemplatesResource(SendlyClient client)
    {
        _client = client;
    }

    public async Task<TemplateListResponse> ListAsync(
        ListTemplatesOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string>();
        if (options?.Limit.HasValue == true)
            queryParams["limit"] = options.Limit.Value.ToString();
        if (!string.IsNullOrEmpty(options?.Type))
            queryParams["type"] = options.Type;
        if (!string.IsNullOrEmpty(options?.Locale))
            queryParams["locale"] = options.Locale;

        var doc = await _client.GetAsync("/templates", queryParams, cancellationToken);
        return JsonSerializer.Deserialize<TemplateListResponse>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    public async Task<Template> GetAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        var doc = await _client.GetAsync($"/templates/{Uri.EscapeDataString(id)}", null, cancellationToken);
        return JsonSerializer.Deserialize<Template>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    public async Task<Template> CreateAsync(
        CreateTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        var doc = await _client.PostAsync("/templates", request, cancellationToken);
        return JsonSerializer.Deserialize<Template>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    public async Task<Template> UpdateAsync(
        string id,
        UpdateTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        var doc = await _client.PatchAsync($"/templates/{Uri.EscapeDataString(id)}", request, cancellationToken);
        return JsonSerializer.Deserialize<Template>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    public async Task<DeleteTemplateResponse> DeleteAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        var doc = await _client.DeleteAsync($"/templates/{Uri.EscapeDataString(id)}", cancellationToken);
        return JsonSerializer.Deserialize<DeleteTemplateResponse>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    public async Task<Template> PublishAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        var doc = await _client.PostAsync($"/templates/{Uri.EscapeDataString(id)}/publish", new { }, cancellationToken);
        return JsonSerializer.Deserialize<Template>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    [Obsolete("The API has no unpublish route, so this call fails with a 404. To retire a published template, create and publish a replacement, then delete this one.")]
    public async Task<Template> UnpublishAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        var doc = await _client.PostAsync($"/verify/templates/{Uri.EscapeDataString(id)}/unpublish", new { }, cancellationToken);
        return JsonSerializer.Deserialize<Template>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    public async Task<Template> CloneAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        var doc = await _client.PostAsync($"/templates/{Uri.EscapeDataString(id)}/clone", new { }, cancellationToken);
        return JsonSerializer.Deserialize<Template>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    public async Task<Template> CloneAsync(
        string id,
        string name,
        CancellationToken cancellationToken = default)
    {
        var doc = await _client.PostAsync($"/templates/{Uri.EscapeDataString(id)}/clone", new { name }, cancellationToken);
        return JsonSerializer.Deserialize<Template>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    public async Task<GeneratedTemplateResponse> GenerateAsync(
        GenerateTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(request.Description))
            throw new ValidationException("Description is required");

        var doc = await _client.PostAsync("/templates/generate", request, cancellationToken);
        return JsonSerializer.Deserialize<GeneratedTemplateResponse>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }
}
