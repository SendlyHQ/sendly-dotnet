using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sendly.Models;

public class Rule
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// What a message must match. The API stores one conditions object, such
    /// as <c>{ "intent": "complaint", "intentConfidenceMin": 0.8 }</c>, which
    /// is read as a one-element list.
    /// </summary>
    [JsonPropertyName("conditions")]
    [JsonConverter(typeof(RuleClauseListConverter))]
    public List<Dictionary<string, object>> Conditions { get; set; } = new();

    /// <summary>
    /// What the rule does. The API stores one actions object, such as
    /// <c>{ "addLabels": ["lbl_1"] }</c>, which is read as a one-element list.
    /// </summary>
    [JsonPropertyName("actions")]
    [JsonConverter(typeof(RuleClauseListConverter))]
    public List<Dictionary<string, object>> Actions { get; set; } = new();

    [JsonPropertyName("priority")]
    [JsonConverter(typeof(NullAsZeroInt32Converter))]
    public int Priority { get; set; }

    /// <summary>
    /// Whether the rule runs. Null when the response does not say.
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool? Enabled { get; set; }

    [JsonPropertyName("createdAt")]
    public string? CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public string? UpdatedAt { get; set; }
}

public class RuleListResponse
{
    [JsonPropertyName("data")]
    public List<Rule> Data { get; set; } = new();
}

public class CreateRuleRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// What a message must match. The API evaluates one conditions object,
    /// so the dictionaries are merged into one when sent. Keys:
    /// <c>intent</c> and <c>sentiment</c> (a string or a list of strings),
    /// <c>intentConfidenceMin</c> and <c>sentimentConfidenceMin</c> (0 to 1).
    /// </summary>
    [JsonPropertyName("conditions")]
    [JsonConverter(typeof(RuleClauseListConverter))]
    public List<Dictionary<string, object>> Conditions { get; set; } = new();

    /// <summary>
    /// What the rule does. The dictionaries are merged into one actions
    /// object when sent. Keys: <c>addLabels</c> (a list of label ids) and
    /// <c>closeConversation</c> (true or false).
    /// </summary>
    [JsonPropertyName("actions")]
    [JsonConverter(typeof(RuleClauseListConverter))]
    public List<Dictionary<string, object>> Actions { get; set; } = new();

    [JsonPropertyName("priority")]
    public int? Priority { get; set; }
}

public class UpdateRuleRequest
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Replaces the rule's conditions. The dictionaries are merged into one
    /// conditions object when sent; see <see cref="CreateRuleRequest.Conditions"/>.
    /// </summary>
    [JsonPropertyName("conditions")]
    [JsonConverter(typeof(RuleClauseListConverter))]
    public List<Dictionary<string, object>>? Conditions { get; set; }

    /// <summary>
    /// Replaces the rule's actions. The dictionaries are merged into one
    /// actions object when sent; see <see cref="CreateRuleRequest.Actions"/>.
    /// </summary>
    [JsonPropertyName("actions")]
    [JsonConverter(typeof(RuleClauseListConverter))]
    public List<Dictionary<string, object>>? Actions { get; set; }

    [JsonPropertyName("priority")]
    public int? Priority { get; set; }

    /// <summary>
    /// Set to false to switch the rule off, or true to switch it back on.
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool? Enabled { get; set; }
}

internal sealed class RuleClauseListConverter : JsonConverter<List<Dictionary<string, object>>>
{
    public override List<Dictionary<string, object>> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;
        var clauses = new List<Dictionary<string, object>>();

        if (root.ValueKind == JsonValueKind.Object)
        {
            clauses.Add(ReadClause(root, options));
        }
        else if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in root.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.Object)
                    clauses.Add(ReadClause(item, options));
            }
        }

        return clauses;
    }

    public override void Write(Utf8JsonWriter writer, List<Dictionary<string, object>> value, JsonSerializerOptions options)
    {
        var merged = new Dictionary<string, object>();
        foreach (var clause in value)
        {
            if (clause == null) continue;
            foreach (var entry in clause)
                merged[entry.Key] = entry.Value;
        }

        JsonSerializer.Serialize(writer, merged, options);
    }

    private static Dictionary<string, object> ReadClause(JsonElement element, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<Dictionary<string, object>>(element.GetRawText(), options)
            ?? new Dictionary<string, object>();
    }
}

internal sealed class NullAsZeroInt32Converter : JsonConverter<int>
{
    public override bool HandleNull => true;

    public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType == JsonTokenType.Null ? 0 : reader.GetInt32();
    }

    public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value);
    }
}
