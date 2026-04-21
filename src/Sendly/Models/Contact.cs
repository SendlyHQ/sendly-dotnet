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

public class UpdateContactRequest
{
    public string? PhoneNumber { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
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

public class UpdateContactListRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
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
    public string? ListId { get; set; }
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
    public int SkippedDuplicates { get; set; }
    public List<ImportContactsError> Errors { get; set; } = new();
    public int TotalErrors { get; set; }
}
