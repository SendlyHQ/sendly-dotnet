using System.Text.Json.Serialization;

namespace Sendly.Models;

public class Contact
{
    public string Id { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Email { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
    public bool? OptedOut { get; set; }
    public string? LineType { get; set; }
    public string? CarrierName { get; set; }
    public string? LineTypeCheckedAt { get; set; }
    public string? InvalidReason { get; set; }
    public string? InvalidatedAt { get; set; }
    public string? UserMarkedValidAt { get; set; }
    public string? CreatedAt { get; set; }
    public string? UpdatedAt { get; set; }
}

public class CheckNumbersRequest
{
    public string? ListId { get; set; }
    public bool Force { get; set; }
}

public class CheckNumbersResponse
{
    public bool Success { get; set; }

    /// <summary>
    /// True when a lookup for the same scope was already running, so no new
    /// one started.
    /// </summary>
    [JsonPropertyName("alreadyRunning")]
    public bool AlreadyRunning { get; set; }

    public string? Message { get; set; }
}

/// <summary>
/// Source of a list-health event. Frozen enum — new values will be
/// added in minor SDK versions, never removed.
/// </summary>
public static class ListHealthEventSource
{
    public const string SendFailure = "send_failure";
    public const string CarrierLookup = "carrier_lookup";
    public const string UserAction = "user_action";
    public const string BulkMarkValid = "bulk_mark_valid";
}

/// <summary>
/// Request for <c>Contacts.BulkMarkValidAsync</c>. Pass either <see cref="Ids"/>
/// (up to 10,000 per call) OR <see cref="ListId"/> — not both. Foreign ids
/// silently no-op via the per-organization filter.
/// </summary>
public class BulkMarkValidRequest
{
    public List<string>? Ids { get; set; }
    public string? ListId { get; set; }
}

/// <summary>
/// Response from <c>Contacts.BulkMarkValidAsync</c>. Reports how many contacts
/// actually had their invalid flag cleared. Already-clean contacts and foreign
/// ids don't count.
/// </summary>
public class BulkMarkValidResponse
{
    public int Cleared { get; set; }
}

public class ContactList
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int ContactCount { get; set; }
    public string? CreatedAt { get; set; }
    public string? UpdatedAt { get; set; }
}

public class ContactListResponse
{
    public List<Contact> Contacts { get; set; } = new();
    public int Total { get; set; }
    public int Limit { get; set; }
    public int Offset { get; set; }
}

public class ContactListsResponse
{
    public List<ContactList> Lists { get; set; } = new();
    public int Total { get; set; }
    public int Limit { get; set; }
    public int Offset { get; set; }
}

public class CreateContactRequest
{
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Email { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Changes to a contact. Only the properties you set are sent, and the API
/// leaves the others as they are. Setting <see cref="Name"/>,
/// <see cref="Email"/> or <see cref="Metadata"/> to null clears it. A
/// contact always has a phone number, so a null <see cref="PhoneNumber"/> is
/// not sent.
/// </summary>
public class UpdateContactRequest : IAssignedProperties
{
    private readonly HashSet<string> _assigned = new();
    private string? _name;
    private string? _email;
    private Dictionary<string, object>? _metadata;

    public string? PhoneNumber { get; set; }

    public string? Name
    {
        get => _name;
        set { _name = value; _assigned.Add(nameof(Name)); }
    }

    public string? Email
    {
        get => _email;
        set { _email = value; _assigned.Add(nameof(Email)); }
    }

    public Dictionary<string, object>? Metadata
    {
        get => _metadata;
        set { _metadata = value; _assigned.Add(nameof(Metadata)); }
    }

    bool IAssignedProperties.IsAssigned(string propertyName) => _assigned.Contains(propertyName);
}

public class ListContactsOptions
{
    public int? Limit { get; set; }
    public int? Offset { get; set; }
    public string? Search { get; set; }
    public string? ListId { get; set; }
}

public class CreateContactListRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

/// <summary>
/// Changes to a contact list. Only the properties you set are sent, and the
/// API leaves the others as they are. Setting <see cref="Description"/> to
/// null clears it. A list always has a name, so a null <see cref="Name"/> is
/// not sent.
/// </summary>
public class UpdateContactListRequest : IAssignedProperties
{
    private readonly HashSet<string> _assigned = new();
    private string? _description;

    public string? Name { get; set; }

    public string? Description
    {
        get => _description;
        set { _description = value; _assigned.Add(nameof(Description)); }
    }

    bool IAssignedProperties.IsAssigned(string propertyName) => _assigned.Contains(propertyName);
}

public class AddContactsRequest
{
    public List<string> ContactIds { get; set; } = new();
}

public class ImportContactItem
{
    public string Phone { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? OptedInAt { get; set; }
}

public class ImportContactsRequest
{
    public List<ImportContactItem> Contacts { get; set; } = new();

    /// <summary>
    /// Contact list to add the imported contacts to, including those that
    /// already existed.
    /// </summary>
    [JsonPropertyName("listId")]
    public string? ListId { get; set; }

    /// <summary>
    /// Opt-in date (ISO 8601) for every contact that does not carry its own
    /// <see cref="ImportContactItem.OptedInAt"/>.
    /// </summary>
    [JsonPropertyName("optedInAt")]
    public string? OptedInAt { get; set; }
}

public class ImportContactsError
{
    public int Index { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
}

public class ImportContactsResponse
{
    public int Imported { get; set; }

    /// <summary>
    /// Contacts skipped because the phone number already existed.
    /// </summary>
    [JsonPropertyName("skippedDuplicates")]
    public int SkippedDuplicates { get; set; }

    /// <summary>
    /// The first 50 rows that could not be imported.
    /// </summary>
    public List<ImportContactsError> Errors { get; set; } = new();

    /// <summary>
    /// How many rows could not be imported, including any beyond the 50 in
    /// <see cref="Errors"/>.
    /// </summary>
    [JsonPropertyName("totalErrors")]
    public int TotalErrors { get; set; }
}
