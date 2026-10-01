using System.Text.Json.Serialization;

namespace Sendly.Models;

public class Label
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("color")]
    public string? Color { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("createdAt")]
    public string? CreatedAt { get; set; }
}

public class LabelListResponse
{
    [JsonPropertyName("data")]
    public List<Label> Data { get; set; } = new();
}

public class CreateLabelRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("color")]
    public string? Color { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

public class AddLabelsRequest
{
    [JsonPropertyName("labelIds")]
    public List<string> LabelIds { get; set; } = new();
}
