using System.Text.Json;
using System.Text.Json.Serialization;
using Sendly.Exceptions;

namespace Sendly.Resources;

/// <summary>
/// Message Templates Resource — reusable SMS message templates (<c>/templates</c>).
///
/// Manage the workspace's message templates: list presets and custom
/// templates, create a draft, update it, publish it, preview it with sample
/// variable values, clone (including from a preset), delete, and AI-generate a
/// template from a description.
///
/// <see cref="TemplatesResource"/> (available as <c>client.Templates</c>) is a
/// second, slimmer view of the same <c>/templates</c> endpoint.
/// </summary>
/// <example>
/// <code>
/// // List available templates
/// var listing = await sendly.MessageTemplates.ListAsync();
///
/// // Create a custom template
/// var template = await sendly.MessageTemplates.CreateAsync(new CreateMessageTemplateRequest
/// {
///     Name = "My OTP",
///     Text = "Your {{app_name}} code is {{code}}",
/// });
///
/// // Publish for use
/// await sendly.MessageTemplates.PublishAsync(template.Id);
/// </code>
/// </example>
public class MessageTemplatesResource
{
    private readonly SendlyClient _client;

    public MessageTemplatesResource(SendlyClient client)
    {
        _client = client;
    }

    /// <summary>
    /// List all templates (presets + custom).
    /// </summary>
    public async Task<MessageTemplateListResponse> ListAsync(CancellationToken cancellationToken = default)
    {
        using var doc = await _client.GetAsync("/templates", null, cancellationToken);
        return JsonSerializer.Deserialize<MessageTemplateListResponse>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    /// <summary>
    /// List preset templates only (otp, 2fa, login, signup, reset, generic).
    /// </summary>
    public async Task<MessageTemplateListResponse> PresetsAsync(CancellationToken cancellationToken = default)
    {
        using var doc = await _client.GetAsync("/templates/presets", null, cancellationToken);
        return JsonSerializer.Deserialize<MessageTemplateListResponse>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    /// <summary>
    /// Get a template by ID.
    /// </summary>
    public async Task<MessageTemplate> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(id))
            throw new ValidationException("Template ID is required");

        using var doc = await _client.GetAsync($"/templates/{Uri.EscapeDataString(id)}", null, cancellationToken);
        return JsonSerializer.Deserialize<MessageTemplate>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    /// <summary>
    /// Create a new template (created as a draft; publish when ready).
    /// </summary>
    public async Task<MessageTemplate> CreateAsync(CreateMessageTemplateRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(request?.Name))
            throw new ValidationException("Template name is required");
        if (string.IsNullOrEmpty(request?.Text))
            throw new ValidationException("Template text is required");

        using var doc = await _client.PostAsync("/templates", new { name = request.Name, text = request.Text }, cancellationToken);
        return JsonSerializer.Deserialize<MessageTemplate>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    /// <summary>
    /// Update a template. Updating a published template creates a new draft version.
    /// </summary>
    public async Task<MessageTemplate> UpdateAsync(string id, UpdateMessageTemplateRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(id))
            throw new ValidationException("Template ID is required");

        using var doc = await _client.PatchAsync($"/templates/{Uri.EscapeDataString(id)}", request, cancellationToken);
        return JsonSerializer.Deserialize<MessageTemplate>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    /// <summary>
    /// Publish a draft template. Published templates are locked and can be used
    /// with the Verify API.
    /// </summary>
    public async Task<MessageTemplate> PublishAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(id))
            throw new ValidationException("Template ID is required");

        using var doc = await _client.PostAsync($"/templates/{Uri.EscapeDataString(id)}/publish", new { }, cancellationToken);
        return JsonSerializer.Deserialize<MessageTemplate>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    /// <summary>
    /// Preview a template with sample variable values.
    /// </summary>
    /// <param name="id">Template ID</param>
    /// <param name="variables">Optional custom variable values</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task<MessageTemplatePreview> PreviewAsync(string id, Dictionary<string, string>? variables = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(id))
            throw new ValidationException("Template ID is required");

        var path = $"/templates/{Uri.EscapeDataString(id)}/preview";
        using var doc = variables != null
            ? await _client.PostAsync(path, new { variables }, cancellationToken)
            : await _client.PostAsync(path, new { }, cancellationToken);
        return JsonSerializer.Deserialize<MessageTemplatePreview>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    /// <summary>
    /// Delete a template. Preset templates cannot be deleted.
    /// </summary>
    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(id))
            throw new ValidationException("Template ID is required");

        using var _ = await _client.DeleteAsync($"/templates/{Uri.EscapeDataString(id)}", cancellationToken);
    }

    /// <summary>
    /// Clone a template (including presets). Creates a copy as a draft.
    /// </summary>
    /// <param name="id">Template ID to clone</param>
    /// <param name="name">Optional name for the clone</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task<MessageTemplate> CloneAsync(string id, string? name = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(id))
            throw new ValidationException("Template ID is required");

        var path = $"/templates/{Uri.EscapeDataString(id)}/clone";
        using var doc = string.IsNullOrEmpty(name)
            ? await _client.PostAsync(path, new { }, cancellationToken)
            : await _client.PostAsync(path, new { name }, cancellationToken);
        return JsonSerializer.Deserialize<MessageTemplate>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    /// <summary>
    /// AI-generate a template from a plain-language description.
    /// </summary>
    public async Task<GeneratedMessageTemplate> GenerateAsync(GenerateMessageTemplateRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(request?.Description))
            throw new ValidationException("Description is required");

        using var doc = await _client.PostAsync("/templates/generate", request, cancellationToken);
        return JsonSerializer.Deserialize<GeneratedMessageTemplate>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }
}

/// <summary>
/// A variable detected in a message template.
/// </summary>
public class MessageTemplateVariable
{
    /// <summary>Variable key (e.g. "code", "app_name").</summary>
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    /// <summary>Variable type ("string" or "number").</summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "string";

    /// <summary>Default fallback value.</summary>
    [JsonPropertyName("fallback")]
    public string? Fallback { get; set; }
}

/// <summary>
/// A reusable SMS message template.
/// </summary>
public class MessageTemplate
{
    /// <summary>Template ID.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Template name.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Message text with {{variables}}.</summary>
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    /// <summary>Variables detected in the template.</summary>
    [JsonPropertyName("variables")]
    public List<MessageTemplateVariable> Variables { get; set; } = new();

    /// <summary>Whether this is a preset template.</summary>
    [JsonPropertyName("is_preset")]
    public bool IsPreset { get; set; }

    /// <summary>Preset slug (e.g. "otp", "2fa"), or null.</summary>
    [JsonPropertyName("preset_slug")]
    public string? PresetSlug { get; set; }

    /// <summary>Template status ("draft" or "published").</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>Version number.</summary>
    [JsonPropertyName("version")]
    public int Version { get; set; }

    /// <summary>When published (ISO 8601), or null.</summary>
    [JsonPropertyName("published_at")]
    public string? PublishedAt { get; set; }

    /// <summary>When created (ISO 8601).</summary>
    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }

    /// <summary>When last updated (ISO 8601).</summary>
    [JsonPropertyName("updated_at")]
    public string? UpdatedAt { get; set; }
}

/// <summary>
/// Response from listing message templates.
/// </summary>
public class MessageTemplateListResponse
{
    /// <summary>Array of templates.</summary>
    [JsonPropertyName("templates")]
    public List<MessageTemplate> Templates { get; set; } = new();
}

/// <summary>
/// Request to create a message template.
/// </summary>
public class CreateMessageTemplateRequest
{
    /// <summary>Template name.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Message text (use {{code}} and {{app_name}} variables).</summary>
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}

/// <summary>
/// Request to update a message template. Null fields are omitted.
/// </summary>
public class UpdateMessageTemplateRequest
{
    /// <summary>New template name.</summary>
    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; set; }

    /// <summary>New message text.</summary>
    [JsonPropertyName("text")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Text { get; set; }
}

/// <summary>
/// A message template preview with interpolated text.
/// </summary>
public class MessageTemplatePreview
{
    /// <summary>
    /// Template ID. The preview response does not include it, so this is
    /// empty; see <see cref="TemplateId"/>.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// ID of the previewed template.
    /// </summary>
    [JsonPropertyName("template_id")]
    public string TemplateId { get; set; } = string.Empty;

    /// <summary>
    /// Template name. The preview response does not include it, so this is
    /// empty.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Original text with variables.</summary>
    [JsonPropertyName("original_text")]
    public string OriginalText { get; set; } = string.Empty;

    /// <summary>The text with the variables filled in.</summary>
    [JsonPropertyName("rendered_text")]
    public string PreviewText { get; set; } = string.Empty;

    /// <summary>Length of the filled-in text, in characters.</summary>
    [JsonPropertyName("character_count")]
    public int CharacterCount { get; set; }

    /// <summary>Number of SMS segments the filled-in text takes.</summary>
    [JsonPropertyName("segment_count")]
    public int SegmentCount { get; set; }

    /// <summary>
    /// Variables detected. The preview response does not include them, so
    /// this is empty.
    /// </summary>
    [JsonPropertyName("variables")]
    public List<MessageTemplateVariable> Variables { get; set; } = new();
}

/// <summary>
/// Request to AI-generate a message template.
/// </summary>
public class GenerateMessageTemplateRequest
{
    /// <summary>Plain-language description of the template to generate.</summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>Optional category hint.</summary>
    [JsonPropertyName("category")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Category { get; set; }
}

/// <summary>
/// An AI-generated message template.
/// </summary>
public class GeneratedMessageTemplate
{
    /// <summary>Suggested template name.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Generated message text with {{variables}}.</summary>
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    /// <summary>Variable keys detected in the generated text.</summary>
    [JsonPropertyName("variables")]
    public List<string> Variables { get; set; } = new();

    /// <summary>Category of the generated template.</summary>
    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;
}
