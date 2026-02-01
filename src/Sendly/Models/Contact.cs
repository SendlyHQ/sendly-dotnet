namespace Sendly.Models;

public class Contact
{
    public string Id { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Email { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
    public string? CreatedAt { get; set; }
    public string? UpdatedAt { get; set; }
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
