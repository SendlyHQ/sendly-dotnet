using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sendly.Models;

/// <summary>
/// Represents credit balance information.
/// </summary>
public class Credits
{
    /// <summary>
    /// Total credit balance.
    /// </summary>
    [JsonPropertyName("balance")]
    public int Balance { get; set; }

    /// <summary>
    /// Credits available to spend: the balance less what is reserved (plus
    /// any overage allowance on a pooled enterprise balance).
    /// </summary>
    [JsonPropertyName("availableBalance")]
    public int AvailableBalance { get; set; }

    /// <summary>
    /// Credits pending from purchases. The API does not report pending
    /// credits, so this is always 0.
    /// </summary>
    [JsonPropertyName("pending_credits")]
    public int PendingCredits { get; set; }

    /// <summary>
    /// Credits reserved for scheduled messages (the API's <c>reservedBalance</c>).
    /// </summary>
    [JsonPropertyName("reservedBalance")]
    public int ReservedCredits { get; set; }

    /// <summary>
    /// Credits reserved for scheduled messages. The same value as
    /// <see cref="ReservedCredits"/>, under the API's name.
    /// </summary>
    [JsonIgnore]
    public int ReservedBalance
    {
        get => ReservedCredits;
        set => ReservedCredits = value;
    }

    /// <summary>
    /// How the workspace pays for messages: <c>prepaid</c>, or <c>pooled</c>
    /// when it draws on an enterprise credit pool.
    /// </summary>
    [JsonPropertyName("billingMode")]
    public string? BillingMode { get; set; }

    /// <summary>
    /// Currency code.
    /// </summary>
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Whether there are credits available.
    /// </summary>
    public bool HasCredits => AvailableBalance > 0;

    /// <summary>
    /// Creates a Credits from a JSON element.
    /// </summary>
    internal static Credits FromJson(JsonElement element, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<Credits>(element.GetRawText(), options)
            ?? new Credits();
    }
}

/// <summary>
/// Represents a credit transaction.
/// </summary>
public class CreditTransaction
{
    /// <summary>
    /// Transaction type constants.
    /// </summary>
    public static class Types
    {
        /// <summary>Credits bought, including auto-recharges.</summary>
        public const string Purchase = "purchase";

        public const string Usage = "usage";
        public const string Refund = "refund";
        public const string Bonus = "bonus";

        /// <summary>Credits moved between workspaces.</summary>
        public const string Transfer = "transfer";

        /// <summary>Credits granted by Sendly.</summary>
        public const string AdminGrant = "admin_grant";

        /// <summary>Test credits added by Sendly.</summary>
        public const string AdminSeed = "admin_seed";

        /// <summary>
        /// Never recorded: a credit granted by Sendly is <see cref="AdminGrant"/>,
        /// and one taken away is <see cref="Usage"/>.
        /// </summary>
        public const string Adjustment = "adjustment";
    }

    /// <summary>
    /// Unique transaction identifier.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Transaction type.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Amount (positive for credits, negative for debits).
    /// </summary>
    [JsonPropertyName("amount")]
    public int Amount { get; set; }

    /// <summary>
    /// Balance after this transaction.
    /// </summary>
    [JsonPropertyName("balance_after")]
    public int BalanceAfter { get; set; }

    /// <summary>
    /// Transaction description.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Reference ID (e.g., message ID, order ID).
    /// </summary>
    [JsonPropertyName("reference_id")]
    public string? ReferenceId { get; set; }

    /// <summary>
    /// Transaction timestamp.
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Whether this is a credit (positive amount).
    /// </summary>
    public bool IsCredit => Amount > 0;

    /// <summary>
    /// Whether this is a debit (negative amount).
    /// </summary>
    public bool IsDebit => Amount < 0;

    /// <summary>
    /// Creates a CreditTransaction from a JSON element.
    /// </summary>
    internal static CreditTransaction FromJson(JsonElement element, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<CreditTransaction>(element.GetRawText(), options)
            ?? new CreditTransaction();
    }
}

/// <summary>
/// Paginated list of credit transactions.
/// </summary>
public class CreditTransactionList : IEnumerable<CreditTransaction>
{
    /// <summary>
    /// The transactions in this page.
    /// </summary>
    public List<CreditTransaction> Data { get; }

    /// <summary>
    /// Total number of transactions.
    /// </summary>
    public int Total { get; }

    /// <summary>
    /// Whether there are more transactions.
    /// </summary>
    public bool HasMore { get; }

    internal CreditTransactionList(JsonDocument response, JsonSerializerOptions options)
    {
        Data = new List<CreditTransaction>();

        var root = response.RootElement;

        if (root.TryGetProperty("transactions", out var transactionsElement) && transactionsElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var element in transactionsElement.EnumerateArray())
            {
                Data.Add(CreditTransaction.FromJson(element, options));
            }
        }
        else if (root.TryGetProperty("data", out var dataElement) && dataElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var element in dataElement.EnumerateArray())
            {
                Data.Add(CreditTransaction.FromJson(element, options));
            }
        }

        if (root.TryGetProperty("total", out var totalElement))
        {
            Total = totalElement.GetInt32();
        }
        else
        {
            Total = Data.Count;
        }

        if (root.TryGetProperty("has_more", out var hasMoreElement))
        {
            HasMore = hasMoreElement.GetBoolean();
        }
    }

    public IEnumerator<CreditTransaction> GetEnumerator() => Data.GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}

public class TransferCreditsOptions
{
    [JsonPropertyName("targetOrganizationId")]
    public string TargetOrganizationId { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public int Amount { get; set; }
}

public class TransferCreditsResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("amount")]
    public int Amount { get; set; }

    [JsonPropertyName("sourceBalance")]
    public int SourceBalance { get; set; }

    [JsonPropertyName("targetBalance")]
    public int TargetBalance { get; set; }

    internal static TransferCreditsResponse FromJson(JsonElement element, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<TransferCreditsResponse>(element.GetRawText(), options)
            ?? new TransferCreditsResponse();
    }
}

/// <summary>
/// Options for listing transactions.
/// </summary>
public class ListTransactionsOptions
{
    /// <summary>
    /// Maximum number of transactions to return.
    /// </summary>
    public int? Limit { get; set; }

    /// <summary>
    /// Number of transactions to skip.
    /// </summary>
    public int? Offset { get; set; }

    /// <summary>
    /// Filter by transaction type.
    /// </summary>
    public string? Type { get; set; }

    internal Dictionary<string, string> ToQueryParams()
    {
        var result = new Dictionary<string, string>();

        if (Limit.HasValue)
            result["limit"] = Math.Min(Limit.Value, 100).ToString();
        if (Offset.HasValue)
            result["offset"] = Offset.Value.ToString();
        if (!string.IsNullOrEmpty(Type))
            result["type"] = Type;

        return result;
    }
}

/// <summary>
/// Represents an API key.
/// </summary>
public class ApiKey
{
    /// <summary>
    /// Unique API key identifier.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the API key.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Key prefix for identification.
    /// </summary>
    [JsonPropertyName("prefix")]
    public string Prefix { get; set; } = string.Empty;

    /// <summary>
    /// Key type: <c>test</c> or <c>live</c>.
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// Permission scopes granted to the key.
    /// </summary>
    [JsonPropertyName("scopes")]
    public List<string>? Scopes { get; set; }

    /// <summary>
    /// Permission scopes granted to the key, under the name the key list
    /// uses. The same values as <see cref="Scopes"/>.
    /// </summary>
    [JsonPropertyName("permissions")]
    public List<string>? Permissions { get; set; }

    /// <summary>
    /// Last time the key was used.
    /// </summary>
    [JsonPropertyName("lastUsedAt")]
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// Creation timestamp.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Expiration timestamp.
    /// </summary>
    [JsonPropertyName("expiresAt")]
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Whether the key is active. False once the key is revoked.
    /// </summary>
    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Whether the key has been revoked.
    /// </summary>
    [JsonPropertyName("isRevoked")]
    public bool? IsRevoked { get; set; }

    /// <summary>
    /// When the key was revoked. Returned by <c>GetApiKeyAsync</c>; the key
    /// list leaves it null.
    /// </summary>
    [JsonPropertyName("revokedAt")]
    public DateTime? RevokedAt { get; set; }

    /// <summary>
    /// Whether the API key is expired.
    /// </summary>
    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow;

    /// <summary>
    /// Creates an ApiKey from a JSON element.
    /// </summary>
    internal static ApiKey FromJson(JsonElement element, JsonSerializerOptions options)
    {
        var apiKey = JsonSerializer.Deserialize<ApiKey>(element.GetRawText(), options)
            ?? new ApiKey();
        if (apiKey.IsRevoked == null && element.ValueKind == JsonValueKind.Object &&
            element.TryGetProperty("isActive", out var isActive) &&
            (isActive.ValueKind == JsonValueKind.True || isActive.ValueKind == JsonValueKind.False))
            apiKey.IsRevoked = !apiKey.IsActive;
        if (apiKey.Permissions == null && apiKey.Scopes != null)
            apiKey.Permissions = new List<string>(apiKey.Scopes);
        return apiKey;
    }
}

/// <summary>
/// List of API keys.
/// </summary>
public class ApiKeyList : IEnumerable<ApiKey>
{
    /// <summary>
    /// The API keys.
    /// </summary>
    public List<ApiKey> Data { get; }

    internal ApiKeyList(JsonDocument response, JsonSerializerOptions options)
    {
        Data = new List<ApiKey>();

        var root = response.RootElement;

        if ((root.TryGetProperty("keys", out var keysElement) ||
             root.TryGetProperty("api_keys", out keysElement)) &&
            keysElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var element in keysElement.EnumerateArray())
            {
                Data.Add(ApiKey.FromJson(element, options));
            }
        }
        else if (root.TryGetProperty("data", out var dataElement) && dataElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var element in dataElement.EnumerateArray())
            {
                Data.Add(ApiKey.FromJson(element, options));
            }
        }
        else if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var element in root.EnumerateArray())
            {
                Data.Add(ApiKey.FromJson(element, options));
            }
        }
    }

    public IEnumerator<ApiKey> GetEnumerator() => Data.GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}

/// <summary>
/// Response from creating an API key.
/// </summary>
public class CreateApiKeyResponse
{
    /// <summary>
    /// The created API key.
    /// </summary>
    public ApiKey ApiKey { get; set; } = new();

    /// <summary>
    /// The full API key value (only shown once).
    /// </summary>
    public string Key { get; set; } = string.Empty;

    internal static CreateApiKeyResponse FromJson(JsonElement element, JsonSerializerOptions options)
    {
        var response = new CreateApiKeyResponse();

        if ((element.TryGetProperty("apiKey", out var apiKeyElement) || element.TryGetProperty("api_key", out apiKeyElement)) &&
            apiKeyElement.ValueKind == JsonValueKind.Object)
        {
            response.ApiKey = ApiKey.FromJson(apiKeyElement, options);
        }
        else if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty("id", out _))
        {
            response.ApiKey = ApiKey.FromJson(element, options);
        }

        if (element.TryGetProperty("key", out var keyElement))
        {
            response.Key = keyElement.GetString() ?? string.Empty;
        }

        return response;
    }
}

/// <summary>
/// Options for creating an API key.
/// </summary>
public class CreateApiKeyOptions
{
    /// <summary>
    /// Display name for the API key.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Key type: <c>test</c> (the default) or <c>live</c>. A live key needs a
    /// verified business and a credit balance; the API answers 403
    /// <c>verification_required</c> or 402 <c>credits_required</c> otherwise.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "test";

    /// <summary>
    /// Permission scopes to grant, such as <c>sms:send</c>. The key you call
    /// with must hold every scope you grant. Omitted when null, and the new
    /// key then gets the calling key's scopes.
    /// </summary>
    [JsonPropertyName("scopes")]
    public List<string>? Scopes { get; set; }

    /// <summary>
    /// Optional expiration date (ISO 8601).
    /// </summary>
    [JsonPropertyName("expires_at")]
    public string? ExpiresAt { get; set; }
}

/// <summary>
/// Options for <see cref="Sendly.Resources.AccountResource.RotateApiKeyAsync(string, int?, System.Threading.CancellationToken)"/>.
/// </summary>
public class RotateApiKeyRequest
{
    /// <summary>
    /// How long the old key keeps working after rotation, in hours (24-168
    /// inclusive; the server defaults to 24 when omitted). Omitted from the wire
    /// when null.
    /// </summary>
    [JsonPropertyName("gracePeriodHours")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? GracePeriodHours { get; set; }
}

/// <summary>
/// An API key as returned in a rotation response. The <c>newKey</c> additionally
/// carries the one-time raw <see cref="Key"/> and a <see cref="Warning"/>; the
/// <c>oldKey</c> leaves both null.
/// </summary>
public class RotatedApiKey
{
    /// <summary>Unique API key identifier.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Display name for the API key.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Public reference id (key_xxx), when present.</summary>
    [JsonPropertyName("keyId")]
    public string? KeyId { get; set; }

    /// <summary>Key prefix for identification, when present.</summary>
    [JsonPropertyName("keyPrefix")]
    public string? KeyPrefix { get; set; }

    /// <summary>Key type: "test" or "live".</summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>Permission scopes granted to the key.</summary>
    [JsonPropertyName("scopes")]
    public List<string>? Scopes { get; set; }

    /// <summary>Whether the key is active.</summary>
    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }

    /// <summary>Creation timestamp.</summary>
    [JsonPropertyName("createdAt")]
    public DateTime? CreatedAt { get; set; }

    /// <summary>Last time the key was used.</summary>
    [JsonPropertyName("lastUsedAt")]
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// Expiration timestamp. On the rotated old key this is the end of the grace
    /// period, after which it stops working.
    /// </summary>
    [JsonPropertyName("expiresAt")]
    public DateTime? ExpiresAt { get; set; }

    /// <summary>Revocation timestamp, when revoked.</summary>
    [JsonPropertyName("revokedAt")]
    public DateTime? RevokedAt { get; set; }

    /// <summary>Id of the key this one was rotated from, when applicable.</summary>
    [JsonPropertyName("rotatedFromId")]
    public string? RotatedFromId { get; set; }

    /// <summary>
    /// The full raw key value (sk_...), present only on the <c>newKey</c> and
    /// shown only once — store it securely.
    /// </summary>
    [JsonPropertyName("key")]
    public string? Key { get; set; }

    /// <summary>Human-readable warning, present only on the <c>newKey</c>.</summary>
    [JsonPropertyName("warning")]
    public string? Warning { get; set; }
}

/// <summary>
/// Response from rotating an API key. The new key supersedes the old one, which
/// keeps working until the end of its grace period.
/// </summary>
public class RotateApiKeyResponse
{
    /// <summary>The newly created key, carrying the one-time raw <see cref="RotatedApiKey.Key"/>.</summary>
    [JsonPropertyName("newKey")]
    public RotatedApiKey NewKey { get; set; } = new();

    /// <summary>The prior key, now expiring at the end of its grace period.</summary>
    [JsonPropertyName("oldKey")]
    public RotatedApiKey OldKey { get; set; } = new();

    /// <summary>Human-readable summary (e.g. "Old key will expire in 24 hours").</summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Creates a RotateApiKeyResponse from a JSON element.
    /// </summary>
    internal static RotateApiKeyResponse FromJson(JsonElement element, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<RotateApiKeyResponse>(element.GetRawText(), options)
            ?? new RotateApiKeyResponse();
    }
}

/// <summary>
/// Usage statistics for an API key, covering its most recent 100 requests.
/// </summary>
public class ApiKeyUsage
{
    /// <summary>
    /// The key's identifier.
    /// </summary>
    [JsonPropertyName("keyId")]
    public string? KeyId { get; set; }

    /// <summary>
    /// The key's display name.
    /// </summary>
    [JsonPropertyName("keyName")]
    public string? KeyName { get; set; }

    /// <summary>
    /// Number of requests made with this key (the API counts at most the
    /// last 100).
    /// </summary>
    [JsonPropertyName("totalRequests")]
    public long TotalRequests { get; set; }

    /// <summary>
    /// Number of successful requests. The API does not report this, so it is
    /// always 0; count <see cref="RecentRequests"/> by status code instead.
    /// </summary>
    [JsonPropertyName("successfulRequests")]
    public long SuccessfulRequests { get; set; }

    /// <summary>
    /// Number of failed requests. The API does not report this, so it is
    /// always 0; count <see cref="RecentRequests"/> by status code instead.
    /// </summary>
    [JsonPropertyName("failedRequests")]
    public long FailedRequests { get; set; }

    /// <summary>
    /// Last request timestamp.
    /// </summary>
    [JsonPropertyName("lastRequestAt")]
    public DateTime? LastRequestAt { get; set; }

    /// <summary>
    /// Credits used by those requests.
    /// </summary>
    [JsonPropertyName("creditsUsed")]
    public long CreditsUsed { get; set; }

    /// <summary>
    /// The key's most recent requests, newest first (at most 20).
    /// </summary>
    [JsonPropertyName("recentRequests")]
    public List<ApiKeyUsageRequest> RecentRequests { get; set; } = new();

    /// <summary>
    /// How many requests went to each endpoint, busiest first.
    /// </summary>
    [JsonPropertyName("endpointBreakdown")]
    public List<ApiKeyUsageEndpoint> EndpointBreakdown { get; set; } = new();

    /// <summary>
    /// Creates an ApiKeyUsage from a JSON element.
    /// </summary>
    internal static ApiKeyUsage FromJson(JsonElement element, JsonSerializerOptions options)
    {
        var usage = JsonSerializer.Deserialize<ApiKeyUsage>(element.GetRawText(), options)
            ?? new ApiKeyUsage();
        usage.RecentRequests ??= new List<ApiKeyUsageRequest>();
        usage.EndpointBreakdown ??= new List<ApiKeyUsageEndpoint>();

        if (element.ValueKind == JsonValueKind.Object &&
            element.TryGetProperty("summary", out var summary) &&
            summary.ValueKind == JsonValueKind.Object)
        {
            if (summary.TryGetProperty("totalRequests", out var total) && total.TryGetInt64(out var totalRequests))
                usage.TotalRequests = totalRequests;
            if (summary.TryGetProperty("totalCredits", out var credits) && credits.TryGetInt64(out var totalCredits))
                usage.CreditsUsed = totalCredits;
            if (summary.TryGetProperty("lastUsed", out var lastUsed) &&
                lastUsed.ValueKind == JsonValueKind.String &&
                lastUsed.TryGetDateTime(out var lastUsedAt))
                usage.LastRequestAt = lastUsedAt;
        }

        return usage;
    }
}

/// <summary>
/// One request made with an API key.
/// </summary>
public class ApiKeyUsageRequest
{
    /// <summary>Request path, such as <c>/api/v1/messages</c>.</summary>
    [JsonPropertyName("endpoint")]
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>HTTP method.</summary>
    [JsonPropertyName("method")]
    public string Method { get; set; } = string.Empty;

    /// <summary>HTTP status code the API answered with.</summary>
    [JsonPropertyName("statusCode")]
    public int? StatusCode { get; set; }

    /// <summary>Credits the request used.</summary>
    [JsonPropertyName("creditsUsed")]
    public int CreditsUsed { get; set; }

    /// <summary>When the request was made.</summary>
    [JsonPropertyName("createdAt")]
    public DateTime? CreatedAt { get; set; }
}

/// <summary>
/// How many of an API key's requests went to one endpoint.
/// </summary>
public class ApiKeyUsageEndpoint
{
    /// <summary>Method and path, such as <c>POST /api/v1/messages</c>.</summary>
    [JsonPropertyName("endpoint")]
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>Number of requests.</summary>
    [JsonPropertyName("count")]
    public int Count { get; set; }
}

/// <summary>
/// The business verification of the account's workspace. Every value is
/// empty when the workspace has not submitted one.
/// </summary>
public class AccountVerification
{
    /// <summary>
    /// Verification status, such as <c>pending</c>, <c>processing</c>,
    /// <c>action_required</c>, <c>verified</c>, <c>approved</c> (a business
    /// verified for international sending) or <c>rejected</c>; null when the
    /// workspace has no verification.
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>
    /// What was verified: <c>toll_free</c>, <c>international</c> or <c>both</c>.
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// Where the business sends: <c>us</c>, <c>intl</c> or <c>both</c>.
    /// </summary>
    [JsonPropertyName("region")]
    public string? Region { get; set; }

    /// <summary>
    /// When the verification was submitted.
    /// </summary>
    [JsonPropertyName("submittedAt")]
    public DateTime? SubmittedAt { get; set; }

    /// <summary>
    /// When the verification last changed.
    /// </summary>
    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Whether email is verified. The API does not report this, so it is
    /// false unless you set it.
    /// </summary>
    [JsonPropertyName("email_verified")]
    public bool EmailVerified { get; set; }

    /// <summary>
    /// Whether phone is verified. The API does not report this, so it is
    /// false unless you set it.
    /// </summary>
    [JsonPropertyName("phone_verified")]
    public bool PhoneVerified { get; set; }

    /// <summary>
    /// Whether identity is verified. The API does not report this, so it is
    /// false unless you set it.
    /// </summary>
    [JsonPropertyName("identity_verified")]
    public bool IdentityVerified { get; set; }

    /// <summary>
    /// Whether the business is verified: <see cref="Status"/> is
    /// <c>verified</c> or <c>approved</c>.
    /// </summary>
    public bool IsFullyVerified => Status is "verified" or "approved" || (EmailVerified && PhoneVerified && IdentityVerified);
}

/// <summary>
/// Account rate limits.
/// </summary>
public class AccountLimits
{
    /// <summary>
    /// Maximum messages per minute.
    /// </summary>
    [JsonPropertyName("messagesPerMinute")]
    public int MessagesPerMinute { get; set; }

    /// <summary>
    /// Maximum messages per second. The API does not report this, so it is
    /// always 10.
    /// </summary>
    [JsonPropertyName("messages_per_second")]
    public int MessagesPerSecond { get; set; } = 10;

    /// <summary>
    /// Maximum messages per day: 100 for a test key, 10,000 for a live key.
    /// </summary>
    [JsonPropertyName("messagesPerDay")]
    public int MessagesPerDay { get; set; } = 10000;

    /// <summary>
    /// Maximum batch size. The API does not report this, so it is always
    /// 1000.
    /// </summary>
    [JsonPropertyName("max_batch_size")]
    public int MaxBatchSize { get; set; } = 1000;
}

/// <summary>
/// The workspace an API key belongs to.
/// </summary>
public class AccountOrganization
{
    /// <summary>Workspace identifier (the <c>organization_id</c> in webhook payloads).</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Workspace name.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>Whether this is the account's personal workspace.</summary>
    [JsonPropertyName("isPersonal")]
    public bool IsPersonal { get; set; }
}

/// <summary>
/// The credit balance reported with the account.
/// </summary>
public class AccountCredits
{
    /// <summary>Credit balance.</summary>
    [JsonPropertyName("balance")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public int Balance { get; set; }

    /// <summary>Credits reserved for scheduled messages.</summary>
    [JsonPropertyName("reservedBalance")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public int ReservedBalance { get; set; }
}

/// <summary>
/// The API key the request was made with.
/// </summary>
public class AccountApiKey
{
    /// <summary>Key identifier.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Key display name.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>Key type: <c>test</c> or <c>live</c>.</summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>Permission scopes the key holds.</summary>
    [JsonPropertyName("scopes")]
    public List<string>? Scopes { get; set; }

    /// <summary>When the key was created.</summary>
    [JsonPropertyName("createdAt")]
    public DateTime? CreatedAt { get; set; }

    /// <summary>When the key was last used.</summary>
    [JsonPropertyName("lastUsedAt")]
    public DateTime? LastUsedAt { get; set; }
}

/// <summary>
/// Represents account information.
/// </summary>
public class Account
{
    /// <summary>
    /// Unique account identifier.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Account email address.
    /// </summary>
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Account holder name. The API does not report it, so it is null.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Company name. The API does not report it, so it is null; see
    /// <see cref="Organization"/> for the workspace name.
    /// </summary>
    [JsonPropertyName("company_name")]
    public string? CompanyName { get; set; }

    /// <summary>
    /// The workspace the API key belongs to, or null for a key without one.
    /// </summary>
    [JsonPropertyName("organization")]
    public AccountOrganization? Organization { get; set; }

    /// <summary>
    /// Credit balance. <see cref="Sendly.Resources.AccountResource.GetCreditsAsync"/>
    /// also reports what is available to spend.
    /// </summary>
    [JsonPropertyName("credits")]
    public AccountCredits? Credits { get; set; }

    /// <summary>
    /// Business verification. Never null: its values are empty when the
    /// workspace has no verification.
    /// </summary>
    [JsonPropertyName("verification")]
    public AccountVerification Verification { get; set; } = new();

    /// <summary>
    /// The API key the request was made with.
    /// </summary>
    [JsonPropertyName("apiKey")]
    public AccountApiKey? ApiKey { get; set; }

    /// <summary>
    /// Rate limits.
    /// </summary>
    [JsonPropertyName("limits")]
    public AccountLimits Limits { get; set; } = new();

    /// <summary>
    /// Account creation timestamp.
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Creates an Account from a JSON element.
    /// </summary>
    internal static Account FromJson(JsonElement element, JsonSerializerOptions options)
    {
        var account = JsonSerializer.Deserialize<Account>(element.GetRawText(), options)
            ?? new Account();

        if (element.ValueKind == JsonValueKind.Object &&
            element.TryGetProperty("user", out var user) &&
            user.ValueKind == JsonValueKind.Object)
        {
            if (user.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.String)
                account.Id = id.GetString() ?? string.Empty;
            if (user.TryGetProperty("email", out var email) && email.ValueKind == JsonValueKind.String)
                account.Email = email.GetString() ?? string.Empty;
            if (user.TryGetProperty("createdAt", out var createdAt) &&
                createdAt.ValueKind == JsonValueKind.String &&
                createdAt.TryGetDateTime(out var created))
                account.CreatedAt = created;
        }

        account.Verification ??= new AccountVerification();
        account.Limits ??= new AccountLimits();
        return account;
    }
}
