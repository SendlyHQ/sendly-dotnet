namespace Sendly.Models;

public class Verification
{
    public string Id { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string DeliveryStatus { get; set; } = string.Empty;
    public int Attempts { get; set; }
    public int MaxAttempts { get; set; } = 3;
    public string ExpiresAt { get; set; } = string.Empty;
    public string? VerifiedAt { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
    public bool Sandbox { get; set; }
    public string? AppName { get; set; }
    public string? TemplateId { get; set; }
    public string? ProfileId { get; set; }

    public bool IsPending => Status == "pending";
    public bool IsVerified => Status == "verified";
    public bool IsExpired => Status == "expired";
}

public class SendVerificationRequest
{
    public string To { get; set; } = string.Empty;
    public string? TemplateId { get; set; }
    public string? ProfileId { get; set; }
    public string? AppName { get; set; }
    public int? TimeoutSecs { get; set; }
    public int? CodeLength { get; set; }
}

public class SendVerificationResponse
{
    public string Id { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string ExpiresAt { get; set; } = string.Empty;
    public bool Sandbox { get; set; }
    public string? SandboxCode { get; set; }
    public string? Message { get; set; }
}

public class CheckVerificationResponse
{
    public string Id { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? VerifiedAt { get; set; }
    public int? RemainingAttempts { get; set; }

    public bool IsVerified => Status == "verified";
}

public class ListVerificationsOptions
{
    public int? Limit { get; set; }
    public string? Status { get; set; }
}

public class VerificationListResponse
{
    public List<Verification> Verifications { get; set; } = new();
    public PaginationInfo? Pagination { get; set; }
}

public class CreateSessionRequest
{
    public string SuccessUrl { get; set; } = string.Empty;
    public string? CancelUrl { get; set; }
    public string? BrandName { get; set; }
    public string? BrandColor { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

public class VerifySession
{
    public string Id { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string SuccessUrl { get; set; } = string.Empty;
    public string? CancelUrl { get; set; }
    public string? BrandName { get; set; }
    public string? BrandColor { get; set; }
    public string? Phone { get; set; }
    public string? VerificationId { get; set; }
    public string? Token { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
    public string ExpiresAt { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
}

public class ValidateSessionRequest
{
    public string Token { get; set; } = string.Empty;
}

public class ValidateSessionResponse
{
    public bool Valid { get; set; }
    public string? SessionId { get; set; }
    public string? Phone { get; set; }
    public string? VerifiedAt { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}
