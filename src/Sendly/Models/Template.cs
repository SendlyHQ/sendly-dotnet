namespace Sendly.Models;

public class Template
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Type { get; set; } = "custom";
    public string? Locale { get; set; }
    public List<string> Variables { get; set; } = new();
    public bool IsDefault { get; set; }
    public bool IsPublished { get; set; }
    public string? CreatedAt { get; set; }
    public string? UpdatedAt { get; set; }

    public bool IsPreset => Type == "preset";
    public bool IsCustom => Type == "custom";
}

public class CreateTemplateRequest
{
    public string Name { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? Locale { get; set; }
    public bool? IsPublished { get; set; }
}

public class UpdateTemplateRequest
{
    public string? Name { get; set; }
    public string? Body { get; set; }
    public string? Locale { get; set; }
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
