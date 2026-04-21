using System.Text.Json;
using Sendly.Models;

namespace Sendly.Resources;

public class ContactsResource
{
    private readonly SendlyClient _client;

    public ContactsResource(SendlyClient client)
    {
        _client = client;
        Lists = new ContactListsResource(client);
    }

    public ContactListsResource Lists { get; }

    public async Task<ContactListResponse> ListAsync(
        ListContactsOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string>();
        if (options?.Limit.HasValue == true)
            queryParams["limit"] = Math.Min(options.Limit.Value, 100).ToString();
        if (options?.Offset.HasValue == true)
            queryParams["offset"] = options.Offset.Value.ToString();
        if (!string.IsNullOrEmpty(options?.Search))
            queryParams["search"] = options.Search;
        if (!string.IsNullOrEmpty(options?.ListId))
            queryParams["list_id"] = options.ListId;

        var doc = await _client.GetAsync("/contacts", queryParams, cancellationToken);
        return JsonSerializer.Deserialize<ContactListResponse>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    public async Task<Contact> GetAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        var doc = await _client.GetAsync($"/contacts/{id}", null, cancellationToken);
        return JsonSerializer.Deserialize<Contact>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    public async Task<Contact> CreateAsync(
        CreateContactRequest request,
        CancellationToken cancellationToken = default)
    {
        var doc = await _client.PostAsync("/contacts", request, cancellationToken);
        return JsonSerializer.Deserialize<Contact>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    public async Task<Contact> UpdateAsync(
        string id,
        UpdateContactRequest request,
        CancellationToken cancellationToken = default)
    {
        var doc = await _client.PatchAsync($"/contacts/{id}", request, cancellationToken);
        return JsonSerializer.Deserialize<Contact>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    public async Task DeleteAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        await _client.DeleteAsync($"/contacts/{id}", cancellationToken);
    }

    /// <summary>
    /// Clear the invalid flag on a contact so future campaigns include it again.
    /// Contacts get auto-flagged when a send fails with a terminal bad-number
    /// error (landline, invalid number) or when a carrier lookup reports they
    /// can't receive SMS.
    /// </summary>
    public async Task<Contact> MarkValidAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        var doc = await _client.PostAsync($"/contacts/{id}/mark-valid", new { }, cancellationToken);
        return JsonSerializer.Deserialize<Contact>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    /// <summary>
    /// Clear the invalid flag on many contacts at once — the escape hatch for
    /// when auto-flag misclassifies at scale. Pass either <see cref="BulkMarkValidRequest.Ids"/>
    /// (up to 10,000 per call) OR <see cref="BulkMarkValidRequest.ListId"/>, not both.
    /// Foreign ids silently no-op via the per-organization filter.
    /// </summary>
    /// <returns>Count of contacts whose flag was actually cleared.</returns>
    public async Task<BulkMarkValidResponse> BulkMarkValidAsync(
        BulkMarkValidRequest request,
        CancellationToken cancellationToken = default)
    {
        var hasIds = request.Ids != null && request.Ids.Count > 0;
        var hasListId = !string.IsNullOrEmpty(request.ListId);

        if (!hasIds && !hasListId)
            throw new ArgumentException("BulkMarkValid requires either Ids or ListId.", nameof(request));
        if (hasIds && hasListId)
            throw new ArgumentException("BulkMarkValid accepts Ids OR ListId, not both.", nameof(request));

        object body = hasIds
            ? new { ids = request.Ids }
            : new { listId = request.ListId };

        var doc = await _client.PostAsync("/contacts/bulk-mark-valid", body, cancellationToken);
        return JsonSerializer.Deserialize<BulkMarkValidResponse>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    /// <summary>
    /// Trigger a background carrier lookup across your contacts. Landlines and
    /// other non-SMS-capable numbers are auto-excluded from future campaigns.
    /// Runs asynchronously (1-5 minutes). Idempotent: re-triggering while a
    /// lookup is already running for the same scope is a no-op.
    /// </summary>
    public async Task<CheckNumbersResponse> CheckNumbersAsync(
        CheckNumbersRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var body = new
        {
            listId = request?.ListId,
            force = request?.Force ?? false,
        };
        var doc = await _client.PostAsync("/contacts/lookup", body, cancellationToken);
        return JsonSerializer.Deserialize<CheckNumbersResponse>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    public async Task<ImportContactsResponse> ImportAsync(
        ImportContactsRequest request,
        CancellationToken cancellationToken = default)
    {
        var doc = await _client.PostAsync("/contacts/import", request, cancellationToken);
        return JsonSerializer.Deserialize<ImportContactsResponse>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }
}

public class ContactListsResource
{
    private readonly SendlyClient _client;

    public ContactListsResource(SendlyClient client)
    {
        _client = client;
    }

    public async Task<ContactListsResponse> ListAsync(
        CancellationToken cancellationToken = default)
    {
        var doc = await _client.GetAsync("/contact-lists", null, cancellationToken);
        return JsonSerializer.Deserialize<ContactListsResponse>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    public async Task<ContactList> GetAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        var doc = await _client.GetAsync($"/contact-lists/{id}", null, cancellationToken);
        return JsonSerializer.Deserialize<ContactList>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    public async Task<ContactList> CreateAsync(
        CreateContactListRequest request,
        CancellationToken cancellationToken = default)
    {
        var doc = await _client.PostAsync("/contact-lists", request, cancellationToken);
        return JsonSerializer.Deserialize<ContactList>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    public async Task<ContactList> UpdateAsync(
        string id,
        UpdateContactListRequest request,
        CancellationToken cancellationToken = default)
    {
        var doc = await _client.PatchAsync($"/contact-lists/{id}", request, cancellationToken);
        return JsonSerializer.Deserialize<ContactList>(doc.RootElement.GetRawText(), _client.JsonOptions)!;
    }

    public async Task DeleteAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        await _client.DeleteAsync($"/contact-lists/{id}", cancellationToken);
    }

    public async Task AddContactsAsync(
        string listId,
        List<string> contactIds,
        CancellationToken cancellationToken = default)
    {
        var request = new AddContactsRequest { ContactIds = contactIds };
        await _client.PostAsync($"/contact-lists/{listId}/contacts", request, cancellationToken);
    }

    public async Task RemoveContactAsync(
        string listId,
        string contactId,
        CancellationToken cancellationToken = default)
    {
        await _client.DeleteAsync($"/contact-lists/{listId}/contacts/{contactId}", cancellationToken);
    }
}
