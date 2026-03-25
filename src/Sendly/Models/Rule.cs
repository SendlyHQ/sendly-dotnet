namespace Sendly.Models;

public class Rule
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<Dictionary<string, object>> Conditions { get; set; } = new();
    public List<Dictionary<string, object>> Actions { get; set; } = new();
    public int Priority { get; set; }
    public string? CreatedAt { get; set; }
    public string? UpdatedAt { get; set; }
}

public class RuleListResponse
{
    public List<Rule> Data { get; set; } = new();
}

public class CreateRuleRequest
{
    public string Name { get; set; } = string.Empty;
    public List<Dictionary<string, object>> Conditions { get; set; } = new();
    public List<Dictionary<string, object>> Actions { get; set; } = new();
    public int? Priority { get; set; }
}

public class UpdateRuleRequest
{
    public string? Name { get; set; }
    public List<Dictionary<string, object>>? Conditions { get; set; }
    public List<Dictionary<string, object>>? Actions { get; set; }
    public int? Priority { get; set; }
}
