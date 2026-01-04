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
    /// Available credits for use.
    /// </summary>
    [JsonPropertyName("available_balance")]
    public int AvailableBalance { get; set; }

    /// <summary>
    /// Credits pending from purchases.
    /// </summary>
    [JsonPropertyName("pending_credits")]
    public int PendingCredits { get; set; }

    /// <summary>
    /// Credits reserved for scheduled messages.
    /// </summary>
    [JsonPropertyName("reserved_credits")]
    public int ReservedCredits { get; set; }

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
        public const string Purchase = "purchase";
        public const string Usage = "usage";
        public const string Refund = "refund";
        public const string Bonus = "bonus";
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
    /// Last time the key was used.
    /// </summary>
    [JsonPropertyName("last_used_at")]
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// Creation timestamp.
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Expiration timestamp.
    /// </summary>
    [JsonPropertyName("expires_at")]
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Whether the key is active.
    /// </summary>
    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Whether the API key is expired.
    /// </summary>
    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow;

    /// <summary>
    /// Creates an ApiKey from a JSON element.
    /// </summary>
    internal static ApiKey FromJson(JsonElement element, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<ApiKey>(element.GetRawText(), options)
            ?? new ApiKey();
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

        if (root.TryGetProperty("api_keys", out var keysElement) && keysElement.ValueKind == JsonValueKind.Array)
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

        if (element.TryGetProperty("api_key", out var apiKeyElement))
        {
            response.ApiKey = ApiKey.FromJson(apiKeyElement, options);
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
    /// Optional expiration date.
    /// </summary>
    [JsonPropertyName("expires_at")]
    public string? ExpiresAt { get; set; }
}

/// <summary>
/// Usage statistics for an API key.
/// </summary>
public class ApiKeyUsage
{
    /// <summary>
    /// Total number of requests made with this key.
    /// </summary>
    [JsonPropertyName("totalRequests")]
    public long TotalRequests { get; set; }

    /// <summary>
    /// Number of successful requests.
    /// </summary>
    [JsonPropertyName("successfulRequests")]
    public long SuccessfulRequests { get; set; }

    /// <summary>
    /// Number of failed requests.
    /// </summary>
    [JsonPropertyName("failedRequests")]
    public long FailedRequests { get; set; }

    /// <summary>
    /// Last request timestamp.
    /// </summary>
    [JsonPropertyName("lastRequestAt")]
    public DateTime? LastRequestAt { get; set; }

    /// <summary>
    /// Credits used by this key.
    /// </summary>
    [JsonPropertyName("creditsUsed")]
    public long CreditsUsed { get; set; }

    /// <summary>
    /// Creates an ApiKeyUsage from a JSON element.
    /// </summary>
    internal static ApiKeyUsage FromJson(JsonElement element, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<ApiKeyUsage>(element.GetRawText(), options)
            ?? new ApiKeyUsage();
    }
}

/// <summary>
/// Account verification status.
/// </summary>
public class AccountVerification
{
    /// <summary>
    /// Whether email is verified.
    /// </summary>
    [JsonPropertyName("email_verified")]
    public bool EmailVerified { get; set; }

    /// <summary>
    /// Whether phone is verified.
    /// </summary>
    [JsonPropertyName("phone_verified")]
    public bool PhoneVerified { get; set; }

    /// <summary>
    /// Whether identity is verified.
    /// </summary>
    [JsonPropertyName("identity_verified")]
    public bool IdentityVerified { get; set; }

    /// <summary>
    /// Whether fully verified.
    /// </summary>
    public bool IsFullyVerified => EmailVerified && PhoneVerified && IdentityVerified;
}

/// <summary>
/// Account rate limits.
/// </summary>
public class AccountLimits
{
    /// <summary>
    /// Maximum messages per second.
    /// </summary>
    [JsonPropertyName("messages_per_second")]
    public int MessagesPerSecond { get; set; } = 10;

    /// <summary>
    /// Maximum messages per day.
    /// </summary>
    [JsonPropertyName("messages_per_day")]
    public int MessagesPerDay { get; set; } = 10000;

    /// <summary>
    /// Maximum batch size.
    /// </summary>
    [JsonPropertyName("max_batch_size")]
    public int MaxBatchSize { get; set; } = 1000;
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
    /// Account holder name.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Company name.
    /// </summary>
    [JsonPropertyName("company_name")]
    public string? CompanyName { get; set; }

    /// <summary>
    /// Verification status.
    /// </summary>
    [JsonPropertyName("verification")]
    public AccountVerification Verification { get; set; } = new();

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
        return JsonSerializer.Deserialize<Account>(element.GetRawText(), options)
            ?? new Account();
    }
}
