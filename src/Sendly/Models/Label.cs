namespace Sendly.Models;

public class Label
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Color { get; set; }
    public string? Description { get; set; }
    public string? CreatedAt { get; set; }
}

public class LabelListResponse
{
    public List<Label> Data { get; set; } = new();
}

public class CreateLabelRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Color { get; set; }
    public string? Description { get; set; }
}

public class AddLabelsRequest
{
    public List<string> LabelIds { get; set; } = new();
}
