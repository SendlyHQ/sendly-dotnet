using System.Text.Json;
using Sendly.Exceptions;
using Sendly.Models;

namespace Sendly.Resources;

public class RulesResource
{
    private readonly SendlyClient _client;

    internal RulesResource(SendlyClient client)
    {
        _client = client;
    }

    /// <summary>
    /// Lists all rules.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of rules</returns>
    public async Task<RuleListResponse> ListAsync(
        CancellationToken cancellationToken = default)
    {
        using var doc = await _client.GetAsync("/rules", null, cancellationToken);
        return JsonSerializer.Deserialize<RuleListResponse>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    /// <summary>
    /// Creates a new rule.
    /// </summary>
    /// <param name="request">Create rule request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created rule</returns>
    public async Task<Rule> CreateAsync(
        CreateRuleRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(request.Name))
            throw new ValidationException("Rule name is required");

        if (request.Conditions == null || request.Conditions.Count == 0)
            throw new ValidationException("Rule conditions are required");

        if (request.Actions == null || request.Actions.Count == 0)
            throw new ValidationException("Rule actions are required");

        using var doc = await _client.PostAsync("/rules", request, cancellationToken);
        return JsonSerializer.Deserialize<Rule>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    /// <summary>
    /// Updates a rule.
    /// </summary>
    /// <param name="id">Rule ID</param>
    /// <param name="request">Update rule request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated rule</returns>
    public async Task<Rule> UpdateAsync(
        string id,
        UpdateRuleRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(id))
            throw new ValidationException("Rule ID is required");

        using var doc = await _client.PatchAsync($"/rules/{Uri.EscapeDataString(id)}", request, cancellationToken);
        return JsonSerializer.Deserialize<Rule>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    /// <summary>
    /// Deletes a rule.
    /// </summary>
    /// <param name="id">Rule ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task DeleteAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(id))
            throw new ValidationException("Rule ID is required");

        using var doc = await _client.DeleteAsync($"/rules/{Uri.EscapeDataString(id)}", cancellationToken);
    }
}
