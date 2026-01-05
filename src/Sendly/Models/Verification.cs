namespace Sendly.Models;

public class Verification
{
    public string Id { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string DeliveryStatus { get; set; } = string.Empty;
    public int Attempts { get; set; }
    public int MaxAttempts { get; set; } = 3;
    public string Channel { get; set; } = "sms";
    public string ExpiresAt { get; set; } = string.Empty;
    public string? VerifiedAt { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
    public bool Sandbox { get; set; }
    public string? AppName { get; set; }
    public string? TemplateId { get; set; }
    public string? ProfileId { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }

    public bool IsPending => Status == "pending";
    public bool IsVerified => Status == "verified";
    public bool IsExpired => Status == "expired";
}

public class SendVerificationRequest
{
    public string Phone { get; set; } = string.Empty;
    public string? Channel { get; set; }
    public int? CodeLength { get; set; }
    public int? ExpiresIn { get; set; }
    public int? MaxAttempts { get; set; }
    public string? TemplateId { get; set; }
    public string? ProfileId { get; set; }
    public string? AppName { get; set; }
    public string? Locale { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

public class SendVerificationResponse
{
    public Verification Verification { get; set; } = new();
    public string? Code { get; set; }
}

public class CheckVerificationResponse
{
    public bool Valid { get; set; }
    public string Status { get; set; } = string.Empty;
    public Verification? Verification { get; set; }
}

public class ListVerificationsOptions
{
    public int? Limit { get; set; }
    public string? Status { get; set; }
    public string? Phone { get; set; }
}

public class VerificationListResponse
{
    public List<Verification> Verifications { get; set; } = new();
    public PaginationInfo? Pagination { get; set; }
}

public class PaginationInfo
{
    public int Limit { get; set; }
    public bool HasMore { get; set; }
}
