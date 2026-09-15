using System.Text.Json.Serialization;

namespace Sendly.Models;

public class TemplateVariable
{
    public string Key { get; set; } = string.Empty;
    public string Type { get; set; } = "string";
    public string? Fallback { get; set; }
}

public class Template
{
    private bool? _isPublished;

    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// Templates are not locale-scoped, so this is never populated from a
    /// response and holds only what you assign. Create one template per locale
    /// instead.
    /// </summary>
    public string? Locale { get; set; }

    /// <summary>
    /// Variables declared by the template, each with its type and fallback.
    /// </summary>
    [JsonPropertyName("variables")]
    public List<TemplateVariable> VariableDefinitions { get; set; } = new();

    public string Status { get; set; } = string.Empty;
    public int Version { get; set; }
    public string? PresetSlug { get; set; }
    public string? PublishedAt { get; set; }
    public string? CreatedAt { get; set; }
    public string? UpdatedAt { get; set; }

    public bool IsPreset { get; set; }
    public bool IsCustom => !IsPreset;

    /// <summary>
    /// Variable keys only, without their type or fallback. Every read returns a
    /// fresh copy, so changes made to the returned list are not kept.
    /// </summary>
    [Obsolete("Use VariableDefinitions, which also carries each variable's type and fallback. Reading this property returns a fresh copy of the keys of VariableDefinitions, so in-place changes such as Variables.Add(...) are discarded; assign a whole list to this property, or edit VariableDefinitions directly.")]
    [JsonIgnore]
    public List<string> Variables
    {
        get => VariableDefinitions.Select(v => v.Key).ToList();
        set => VariableDefinitions = (value ?? new List<string>())
            .Select(key => new TemplateVariable { Key = key })
            .ToList();
    }

    /// <summary>
    /// "preset" or "custom".
    /// </summary>
    [Obsolete("Use IsPreset (or PresetSlug for which preset a template came from). This property is derived from IsPreset.")]
    [JsonIgnore]
    public string Type
    {
        get => IsPreset ? "preset" : "custom";
        set => IsPreset = value == "preset";
    }

    /// <summary>
    /// Templates have no default flag, so this is never populated from a
    /// response and never sent; it holds only what you assign, and defaults to
    /// false.
    /// </summary>
    [Obsolete("Templates have no default flag. This property is never populated from a response and never sent; it holds only what you assign. Use IsPreset to tell built-in templates from your own.")]
    [JsonIgnore]
    public bool IsDefault { get; set; }

    [Obsolete("Use Status (\"draft\" or \"published\") or PublishedAt. Reading this property returns Status == \"published\" unless you have assigned it a value, which it then returns unchanged.")]
    [JsonIgnore]
    public bool IsPublished
    {
        get => _isPublished ?? Status == "published";
        set => _isPublished = value;
    }
}

public class CreateTemplateRequest
{
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public string Body { get; set; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<TemplateVariable>? Variables { get; set; }

    /// <summary>
    /// Not sent. Templates are not locale-scoped.
    /// </summary>
    [Obsolete("Templates are not locale-scoped, so this value is never sent. Create one template per locale instead.")]
    [JsonIgnore]
    public string? Locale { get; set; }

    /// <summary>
    /// Not sent. Templates are always created as drafts.
    /// </summary>
    [Obsolete("Templates are always created as drafts, so this value is never sent. Call TemplatesResource.PublishAsync afterwards to publish.")]
    [JsonIgnore]
    public bool? IsPublished { get; set; }
}

public class UpdateTemplateRequest
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; set; }

    [JsonPropertyName("text")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Body { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<TemplateVariable>? Variables { get; set; }

    /// <summary>
    /// Not sent. Templates are not locale-scoped.
    /// </summary>
    [Obsolete("Templates are not locale-scoped, so this value is never sent. Create one template per locale instead.")]
    [JsonIgnore]
    public string? Locale { get; set; }

    /// <summary>
    /// Not sent. Publishing is a separate call.
    /// </summary>
    [Obsolete("Publishing is a separate call, so this value is never sent. Use TemplatesResource.PublishAsync.")]
    [JsonIgnore]
    public bool? IsPublished { get; set; }
}

public class ListTemplatesOptions
{
    public int? Limit { get; set; }
    public string? Type { get; set; }
    public string? Locale { get; set; }
}

public class TemplateListResponse
{
    public List<Template> Templates { get; set; } = new();
    public PaginationInfo? Pagination { get; set; }
}

public class DeleteTemplateResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
}

public class GenerateTemplateRequest
{
    public string Description { get; set; } = string.Empty;
    public string? Category { get; set; }
}

public class GeneratedTemplateResponse
{
    public string Name { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public List<string> Variables { get; set; } = new();
    public string Category { get; set; } = string.Empty;
}
