using System.Text.Json;
using Sendly.Exceptions;
using Sendly.Models;

namespace Sendly.Resources;

public class LabelsResource
{
    private readonly SendlyClient _client;

    internal LabelsResource(SendlyClient client)
    {
        _client = client;
    }

    /// <summary>
    /// Creates a new label.
    /// </summary>
    /// <param name="request">Create label request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created label</returns>
    public async Task<Label> CreateAsync(
        CreateLabelRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(request.Name))
            throw new ValidationException("Label name is required");

        using var doc = await _client.PostAsync("/labels", request, cancellationToken);
        return JsonSerializer.Deserialize<Label>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    /// <summary>
    /// Lists all labels.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of labels</returns>
    public async Task<LabelListResponse> ListAsync(
        CancellationToken cancellationToken = default)
    {
        using var doc = await _client.GetAsync("/labels", null, cancellationToken);
        return JsonSerializer.Deserialize<LabelListResponse>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    /// <summary>
    /// Deletes a label.
    /// </summary>
    /// <param name="id">Label ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task DeleteAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(id))
            throw new ValidationException("Label ID is required");

        using var doc = await _client.DeleteAsync($"/labels/{Uri.EscapeDataString(id)}", cancellationToken);
    }
}
