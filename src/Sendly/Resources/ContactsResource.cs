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
